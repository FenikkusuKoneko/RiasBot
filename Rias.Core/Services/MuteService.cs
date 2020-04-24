using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Serilog;

namespace Rias.Core.Services
{
    public class MuteService : RiasService
    {
        private readonly DiscordShardedClient _client;

        public MuteService(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            LoadTimers = new Timer(_ => LoadTimersAsync(), null, TimeSpan.Zero, TimeSpan.FromDays(7));
        }

        private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), Timer> _timers = new ConcurrentDictionary<(ulong, ulong), Timer>();

        public const string MuteRole = "rias-mute";
        private const string ModuleName = "Administration";

        private Timer LoadTimers { get; }

        private void LoadTimersAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var muteTimersDb = db.MuteTimers.ToList();

            var dateTime = DateTime.UtcNow.AddDays(7);
            foreach (var muteTimerDb in muteTimersDb)
            {
                if (dateTime <= muteTimerDb.Expiration)
                    continue;

                var context = new MuteContext(_client, muteTimerDb.GuildId, muteTimerDb.ModeratorId, muteTimerDb.UserId, muteTimerDb.MuteChannelSourceId);
                var dueTime = muteTimerDb.Expiration - DateTime.UtcNow;
                if (dueTime < TimeSpan.Zero)
                    dueTime = TimeSpan.Zero;
                var muteTimer = new Timer(async _ => await UnmuteUserAsync(context), null, dueTime, TimeSpan.Zero);
                _timers.TryAdd((muteTimerDb.GuildId, muteTimerDb.UserId), muteTimer);
            }

            Log.Debug("Mute timers loaded");
        }

        public async Task MuteUserAsync(IMessageChannel channel, SocketGuildUser moderator, SocketGuildUser user,
            string? reason, TimeSpan? timeout = null)
        {
            var guild = user.Guild;

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);

            var role = (guild.GetRole(guildDb?.MuteRoleId ?? 0)
                        ?? guild.Roles.FirstOrDefault(x => string.Equals(x.Name, MuteRole) && !x.IsManaged))
                       ?? (IRole) await guild.CreateRoleAsync(MuteRole, isMentionable: false);

            var currentUser = guild.CurrentUser;
            if (currentUser.CheckRoleHierarchy(role) <= 0)
            {
                await ReplyErrorAsync(channel, guild.Id, ModuleName, "MuteRoleAbove", role.Name);
                return;
            }

            if (user.Roles.Any(r => r.Id == role.Id))
            {
                await ReplyErrorAsync(channel, guild.Id, ModuleName, "UserAlreadyMuted", user);
                return;
            }

            await RunTaskAsync(AddMuteRoleToChannelsAsync(role, guild));
            await user.AddRoleAsync(role);

            await RunTaskAsync(AddMuteAsync(channel, moderator, user, timeout));

            var embed = new EmbedBuilder
                {
                    Color = RiasUtils.Yellow,
                    Description = GetText(guild.Id, ModuleName, "UserMuted")
                }
                .AddField(GetText(guild.Id, null, "#Common_User"), user, true)
                .AddField(GetText(guild.Id, null, "#Common_Id"), user.Id, true)
                .AddField(GetText(guild.Id, ModuleName, "Moderator"), moderator, true);

            if (!string.IsNullOrEmpty(reason))
                embed.AddField(GetText(guild.Id, null, "#Common_Reason"), reason, true);

            embed.WithThumbnailUrl(user.GetRealAvatarUrl());
            embed.WithCurrentTimestamp();

            if (timeout.HasValue)
            {
                var culture = Resources.GetGuildCulture(guild.Id);
                embed.AddField(GetText(guild.Id, ModuleName, "MutedFor"),
                    timeout.Value.Humanize(5, culture, TimeUnit.Year), true);
            }

            var modLogChannel = guild.GetTextChannel(guildDb?.ModLogChannelId ?? 0);
            if (modLogChannel != null && channel.Id != modLogChannel.Id)
            {
                var preconditions = currentUser.GetPermissions(modLogChannel);
                if (preconditions.ViewChannel && preconditions.SendMessages)
                    channel = modLogChannel;
            }

            await channel.SendMessageAsync(embed);
        }

        private async Task AddMuteAsync(IMessageChannel channel, SocketGuildUser moderator, SocketGuildUser user, TimeSpan? timeout)
        {
            var guild = user.Guild;
            
            if (_timers.TryRemove((guild.Id, user.Id), out var muteTimer))
            {
                await muteTimer.DisposeAsync();
                Log.Debug("Mute timer removed");
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userGuildDb = await db.GuildUsers.FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.UserId == user.Id);
            if (userGuildDb != null)
            {
                userGuildDb.IsMuted = true;
            }
            else
            {
                var muteUserGuild = new GuildUsers
                {
                    GuildId = guild.Id,
                    UserId = user.Id,
                    IsMuted = true
                };
                await db.AddAsync(muteUserGuild);
            }

            if (!timeout.HasValue)
            {
                await db.SaveChangesAsync();
                return;
            }

            var muteTimerDb = await db.MuteTimers.FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.UserId == user.Id);
            if (muteTimerDb != null)
            {
                muteTimerDb.ModeratorId = moderator.Id;
                muteTimerDb.MuteChannelSourceId = channel.Id;
                muteTimerDb.Expiration = DateTime.UtcNow + timeout.Value;
            }
            else
            {
                var newMuteTimerDb = new MuteTimers
                {
                    GuildId = guild.Id,
                    UserId = user.Id,
                    ModeratorId = moderator.Id,
                    MuteChannelSourceId = channel.Id,
                    Expiration = DateTime.UtcNow + timeout.Value
                };

                await db.AddAsync(newMuteTimerDb);
            }

            await db.SaveChangesAsync();

            if (timeout.Value.Days >= 7)
            {
                return;
            }

            var muteContext = new MuteContext(_client, guild.Id, moderator.Id, user.Id, channel.Id);
            var timer = new Timer(async _ => await UnmuteUserAsync(muteContext), null, timeout.Value, TimeSpan.Zero);
            _timers.TryAdd((guild.Id, user.Id), timer);
            {
                Log.Debug("Mute timer added");
            }
        }

        public async Task UnmuteUserAsync(MuteContext context)
        {
            if (context.SentByTimer) context.Update();

            if (context.Guild is null || context.User is null)
            {
                await RemoveMuteAsync(context);
                return;
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == context.Guild.Id);

            var role = context.Guild.GetRole(guildDb?.MuteRoleId ?? 0)
                       ?? context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Name, MuteRole) && !x.IsManaged);

            if (role is null)
            {
                if (!context.SentByTimer)
                    await ReplyErrorAsync(context.SourceChannel!, context.Guild.Id, ModuleName, "MuteRoleNotFound", guildDb?.Prefix ?? Creds.Prefix);
                await RemoveMuteAsync(context);
                return;
            }

            var currentUser = context.Guild.CurrentUser;
            if (currentUser.CheckRoleHierarchy(role) <= 0)
            {
                if (!context.SentByTimer)
                    await ReplyErrorAsync(context.SourceChannel!, context.Guild.Id, ModuleName, "MuteRoleAbove", role.Name);
                await RemoveMuteAsync(context);
                return;
            }

            if (currentUser.CheckHierarchy(context.User) <= 0)
            {
                if (!context.SentByTimer)
                    await ReplyErrorAsync(context.SourceChannel!, context.Guild.Id, ModuleName, "UserAbove");
                await RemoveMuteAsync(context);
                return;
            }

            if (context.User.Roles.FirstOrDefault(r => r.Id == role.Id) is null)
            {
                if (!context.SentByTimer)
                    await ReplyErrorAsync(context.SourceChannel!, context.Guild.Id, ModuleName, "UserNotMuted", context.User);
                await RemoveMuteAsync(context);
                return;
            }

            await RunTaskAsync(RemoveMuteAsync(context));
            await context.User.RemoveRoleAsync(role);

            var embed = new EmbedBuilder
                {
                    Color = RiasUtils.Yellow,
                    Description = GetText(context.Guild.Id, ModuleName, "UserUnmuted")
                }
                .AddField(GetText(context.Guild.Id, null, "#Common_User"), context.User, true)
                .AddField(GetText(context.Guild.Id, null, "#Common_Id"), context.UserId, true)
                .AddField(GetText(context.Guild.Id, ModuleName, "Moderator"),
                    context.SentByTimer ? currentUser : context.Moderator, true);

            if (!string.IsNullOrEmpty(context.Reason))
                embed.AddField(GetText(context.Guild.Id, null, "#Common_Reason"), context.Reason, true);

            if (context.SentByTimer)
                embed.AddField(GetText(context.Guild.Id, null, "#Common_Reason"),
                    GetText(context.Guild.Id, null, "#Common_TimesUp"), true);

            embed.WithThumbnailUrl(context.User.GetRealAvatarUrl());
            embed.WithCurrentTimestamp();

            var channel = context.SourceChannel;
            var modLogChannel = context.Guild.GetTextChannel(guildDb?.ModLogChannelId ?? 0);
            if (modLogChannel != null)
            {
                var preconditionsModLog = currentUser.GetPermissions(modLogChannel);
                if (preconditionsModLog.ViewChannel && preconditionsModLog.SendMessages)
                    channel = modLogChannel;
            }

            if (channel is null)
                return;

            var preconditionsChannel = currentUser.GetPermissions((SocketTextChannel) channel);
            if (preconditionsChannel.ViewChannel && preconditionsChannel.SendMessages)
                await channel.SendMessageAsync(embed);
        }

        private async Task RemoveMuteAsync(MuteContext context)
        {
            if (_timers.TryRemove((context.GuildId, context.UserId), out var muteTimer))
            {
                await muteTimer.DisposeAsync();
                Log.Debug("Mute timer removed");
            }

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userGuildDb = await db.GuildUsers.FirstOrDefaultAsync(x => x.GuildId == context.GuildId && x.UserId == context.UserId);
            if (userGuildDb != null)
                userGuildDb.IsMuted = false;

            var muteTimerDb = await db.MuteTimers.FirstOrDefaultAsync(x => x.GuildId == context.GuildId && x.UserId == context.UserId);
            if (muteTimerDb != null)
                db.Remove(muteTimerDb);

            await db.SaveChangesAsync();
        }

        public async Task AddMuteRoleToChannelsAsync(IRole role, SocketGuild guild)
        {
            var permissions = new OverwritePermissions().Modify(addReactions: PermValue.Deny, sendMessages: PermValue.Deny, speak: PermValue.Deny);

            var categories = guild.CategoryChannels;
            foreach (var category in categories)
            {
                if (guild.CurrentUser.GetPermissions(category).ViewChannel)
                    await AddPermissionOverwriteAsync(category, role, permissions);
            }

            var channels = guild.Channels;
            foreach (var channel in channels)
            {
                if (guild.CurrentUser.GetPermissions(channel).ViewChannel)
                    await AddPermissionOverwriteAsync(channel, role, permissions);
            }
        }

        private static async Task AddPermissionOverwriteAsync(SocketGuildChannel channel, IRole role, OverwritePermissions permissions)
        {
            var rolePermissions = channel.GetPermissionOverwrite(role);

            if (!rolePermissions.HasValue)
            {
                await channel.AddPermissionOverwriteAsync(role, permissions);
                return;
            }

            var sendMessages = rolePermissions.Value.SendMessages;
            var addReactions = rolePermissions.Value.AddReactions;
            var speak = rolePermissions.Value.Speak;

            var addPermissionOverwrite = false;

            if (sendMessages != PermValue.Deny)
            {
                sendMessages = PermValue.Deny;
                addPermissionOverwrite = true;
            }

            if (addReactions != PermValue.Deny)
            {
                addReactions = PermValue.Deny;
                addPermissionOverwrite = true;
            }

            if (speak != PermValue.Deny)
            {
                speak = PermValue.Deny;
                addPermissionOverwrite = true;
            }

            rolePermissions = rolePermissions.Value.Modify(addReactions: addReactions, sendMessages: sendMessages, speak: speak);

            if (addPermissionOverwrite)
                await channel.AddPermissionOverwriteAsync(role, rolePermissions.Value);
        }

        public class MuteContext
        {
            public SocketGuild? Guild { get; private set; }
            public ulong GuildId { get; private set; }
            public SocketGuildUser? Moderator { get; private set; }
            public SocketGuildUser? User { get; private set; }
            public ulong UserId { get; }
            public IMessageChannel? SourceChannel { get; private set; }
            public string? Reason { get; }
            public bool SentByTimer { get; }

            private readonly DiscordShardedClient? _client;

            public MuteContext(SocketGuild guild, SocketGuildUser moderator, SocketGuildUser user, IMessageChannel channel, string? reason)
            {
                Guild = guild;
                GuildId = guild.Id;
                Moderator = moderator;
                User = user;
                UserId = user.Id;
                SourceChannel = channel;
                Reason = reason;
            }

            public MuteContext(DiscordShardedClient client, ulong guildId, ulong moderatorId, ulong userId, ulong channelId)
            {
                _client = client;

                Guild = client.GetGuild(guildId);
                GuildId = guildId;
                Moderator = Guild?.GetUser(moderatorId);
                User = Guild?.GetUser(userId);
                UserId = userId;
                SourceChannel = Guild?.GetTextChannel(channelId);
                SentByTimer = true;
            }

            public void Update()
            {
                Guild = _client?.GetGuild(GuildId);
                Moderator = Guild?.GetUser(Moderator?.Id ?? 0);
                User = Guild?.GetUser(UserId);
                SourceChannel = Guild?.GetTextChannel(SourceChannel?.Id ?? 0);
            }
        }
    }
}