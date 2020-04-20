using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Serilog;

namespace Rias.Core.Services
{
    public class MuteService : RiasService
    {
        public MuteService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            LoadTimers = new Timer(_ => LoadTimersAsync(), null, TimeSpan.Zero, TimeSpan.FromDays(7));
        }
        
        private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), Timer> _timers = new ConcurrentDictionary<(ulong, ulong), Timer>();

#if DEBUG
        public const string MuteRole = "rias-mute-dev";
#elif RELEASE
        public const string MuteRole = "rias-mute";
#endif
        
        private Timer LoadTimers { get; }
        
        private void LoadTimersAsync()
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var muteTimersDb = db.MuteTimers.ToList();

            var dateTime = DateTime.UtcNow.AddDays(7);
            foreach (var muteTimerDb in muteTimersDb)
            {
                if (dateTime <= muteTimerDb.Expiration)
                    continue;

                var context = new MuteContext(RiasBot, muteTimerDb.GuildId, muteTimerDb.ModeratorId, muteTimerDb.UserId, muteTimerDb.MuteChannelSourceId);
                var dueTime = muteTimerDb.Expiration - DateTime.UtcNow;
                if (dueTime < TimeSpan.Zero)
                    dueTime = TimeSpan.Zero;
                var muteTimer = new Timer(async _ => await UnmuteUserAsync(context), null, dueTime, TimeSpan.Zero);
                _timers.TryAdd((muteTimerDb.GuildId, muteTimerDb.UserId), muteTimer);
            }

            Log.Debug("Mute timers loaded");
        }
        
        public async Task MuteUserAsync(IMessageChannel channel, CachedMember moderator, CachedMember member,
            string? reason, TimeSpan? timeout = null)
        {
            var guild = member.Guild;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);

            var role = (guild.GetRole(guildDb?.MuteRoleId ?? 0)
                        ?? guild.Roles.FirstOrDefault(x => string.Equals(x.Value.Name, MuteRole) && !x.Value.IsManaged).Value)
                       ?? (IRole) await guild.CreateRoleAsync(x => x.Name = MuteRole);

            var currentMember = guild.CurrentMember;
            if (currentMember.CheckRoleHierarchy(role) <= 0)
            {
                await ReplyErrorAsync(channel, guild.Id, Localization.AdministrationMuteRoleAbove, role.Name);
                return;
            }

            if (member.Roles.Any(r => r.Key == role.Id))
            {
                await ReplyErrorAsync(channel, guild.Id, Localization.AdministrationUserAlreadyMuted, member);
                return;
            }

            await RunTaskAsync(AddMuteRoleToChannelsAsync(role, guild));
            await member.GrantRoleAsync(role.Id);

            await RunTaskAsync(AddMuteAsync(channel, moderator, member, timeout));

            var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.Yellow,
                    Description = GetText(guild.Id, Localization.AdministrationUserMuted)
                }
                .AddField(GetText(guild.Id, Localization.CommonUser), member, true)
                .AddField(GetText(guild.Id, Localization.CommonId), member.Id, true)
                .AddField(GetText(guild.Id, Localization.AdministrationModerator), moderator, true);

            if (!string.IsNullOrEmpty(reason))
                embed.AddField(GetText(guild.Id, Localization.CommonReason), reason, true);

            embed.WithThumbnailUrl(member.GetAvatarUrl());
            embed.WithCurrentTimestamp();

            if (timeout.HasValue)
            {
                var locale = Localization.GetGuildLocale(guild.Id);
                embed.AddField(GetText(guild.Id, Localization.AdministrationMutedFor),
                    timeout.Value.Humanize(5, new CultureInfo(locale), TimeUnit.Year), true);
            }

            var modLogChannel = guild.GetTextChannel(guildDb?.ModLogChannelId ?? 0);
            if (modLogChannel != null && channel.Id != modLogChannel.Id)
            {
                var preconditions = currentMember.GetPermissionsFor(modLogChannel);
                if (preconditions.ViewChannel && preconditions.SendMessages)
                    channel = modLogChannel;
            }

            await channel.SendMessageAsync(embed);
        }
        
        private async Task AddMuteAsync(IMessageChannel channel, CachedMember moderator, CachedMember member, TimeSpan? timeout)
        {
            var guild = member.Guild;
            
            if (_timers.TryRemove((guild.Id, member.Id), out var muteTimer))
            {
                await muteTimer.DisposeAsync();
                Log.Debug("Mute timer removed");
            }

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userGuildDb = await db.GuildUsers.FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.UserId == member.Id);
            if (userGuildDb != null)
            {
                userGuildDb.IsMuted = true;
            }
            else
            {
                var muteUserGuild = new GuildUsersEntity
                {
                    GuildId = guild.Id,
                    UserId = member.Id,
                    IsMuted = true
                };
                await db.AddAsync(muteUserGuild);
            }

            if (!timeout.HasValue)
            {
                await db.SaveChangesAsync();
                return;
            }

            var muteTimerDb = await db.MuteTimers.FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.UserId == member.Id);
            if (muteTimerDb != null)
            {
                muteTimerDb.ModeratorId = moderator.Id;
                muteTimerDb.MuteChannelSourceId = channel.Id;
                muteTimerDb.Expiration = DateTime.UtcNow + timeout.Value;
            }
            else
            {
                var newMuteTimerDb = new MuteTimersEntity
                {
                    GuildId = guild.Id,
                    UserId = member.Id,
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

            var muteContext = new MuteContext(RiasBot, guild.Id, moderator.Id, member.Id, channel.Id);
            var timer = new Timer(async _ => await UnmuteUserAsync(muteContext), null, timeout.Value, TimeSpan.Zero);
            _timers.TryAdd((guild.Id, member.Id), timer);
            {
                Log.Debug("Mute timer added");
            }
        }
        
        public async Task UnmuteUserAsync(MuteContext context)
        {
            if (context.SentByTimer) context.Update();

            if (context.Guild is null || context.Member is null)
            {
                await RemoveMuteAsync(context);
                return;
            }

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == context.Guild.Id);

            var role = context.Guild.GetRole(guildDb?.MuteRoleId ?? 0)
                       ?? context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Value.Name, MuteRole) && !x.Value.IsManaged).Value;

            if (role is null)
            {
                if (!context.SentByTimer)
                    await ReplyErrorAsync(context.SourceChannel!, context.Guild.Id, Localization.AdministrationMuteRoleNotFound, guildDb?.Prefix ?? Credentials.Prefix);
                await RemoveMuteAsync(context);
                return;
            }

            var currentMember = context.Guild.CurrentMember;
            if (currentMember.CheckRoleHierarchy(role) <= 0)
            {
                if (!context.SentByTimer)
                    await ReplyErrorAsync(context.SourceChannel!, context.Guild.Id, Localization.AdministrationMuteRoleAbove, role.Name);
                await RemoveMuteAsync(context);
                return;
            }

            if (currentMember.CheckHierarchy(context.Member) <= 0)
            {
                if (!context.SentByTimer)
                    await ReplyErrorAsync(context.SourceChannel!, context.Guild.Id, Localization.AdministrationUserAboveMe);
                await RemoveMuteAsync(context);
                return;
            }

            if (context.Member.Roles.All(r => r.Key != role.Id))
            {
                if (!context.SentByTimer)
                    await ReplyErrorAsync(context.SourceChannel!, context.Guild.Id, Localization.AdministrationUserNotMuted, context.Member);
                await RemoveMuteAsync(context);
                return;
            }

            await RunTaskAsync(RemoveMuteAsync(context));
            await context.Member.RevokeRoleAsync(role.Id);

            var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.Yellow,
                    Description = GetText(context.Guild.Id, Localization.AdministrationUserUnmuted)
                }
                .AddField(GetText(context.Guild.Id, Localization.CommonUser), context.Member, true)
                .AddField(GetText(context.Guild.Id, Localization.CommonId), context.UserId, true)
                .AddField(GetText(context.Guild.Id, Localization.AdministrationModerator),
                    context.SentByTimer ? currentMember : context.Moderator, true);

            if (!string.IsNullOrEmpty(context.Reason))
                embed.AddField(GetText(context.Guild.Id, Localization.CommonReason), context.Reason, true);

            if (context.SentByTimer)
                embed.AddField(GetText(context.Guild.Id, Localization.CommonReason),
                    GetText(context.Guild.Id, Localization.CommonTimesUp), true);

            embed.WithThumbnailUrl(context.Member.GetAvatarUrl());
            embed.WithCurrentTimestamp();

            var channel = context.SourceChannel;
            var modLogChannel = context.Guild.GetTextChannel(guildDb?.ModLogChannelId ?? 0);
            if (modLogChannel != null)
            {
                var preconditionsModLog = currentMember.GetPermissionsFor(modLogChannel);
                if (preconditionsModLog.ViewChannel && preconditionsModLog.SendMessages)
                    channel = modLogChannel;
            }

            if (channel is null)
                return;

            var preconditionsChannel = currentMember.GetPermissionsFor((CachedTextChannel) channel);
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

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userGuildDb = await db.GuildUsers.FirstOrDefaultAsync(x => x.GuildId == context.GuildId && x.UserId == context.UserId);
            if (userGuildDb != null)
                userGuildDb.IsMuted = false;

            var muteTimerDb = await db.MuteTimers.FirstOrDefaultAsync(x => x.GuildId == context.GuildId && x.UserId == context.UserId);
            if (muteTimerDb != null)
                db.Remove(muteTimerDb);

            await db.SaveChangesAsync();
        }

        public async Task AddMuteRoleToChannelsAsync(IRole role, CachedGuild guild)
        {
            var categories = guild.CategoryChannels;
            foreach (var (_, category) in categories)
            {
                if (guild.CurrentMember.GetPermissionsFor(category).ViewChannel)
                    await AddPermissionOverwriteAsync(category, role);
            }

            var channels = guild.Channels;
            foreach (var (_, channel) in channels)
            {
                if (guild.CurrentMember.GetPermissionsFor(channel).ViewChannel)
                    await AddPermissionOverwriteAsync(channel, role);
            }
        }

        private static async Task AddPermissionOverwriteAsync(CachedGuildChannel channel, IRole role)
        {
            var roleOverwrites = channel.Overwrites
                .FirstOrDefault(x => x.TargetType == OverwriteTargetType.Role && x.TargetId == role.Id);

            if (roleOverwrites is null)
            {
                await channel.AddOrModifyOverwriteAsync(new LocalOverwrite(role, new OverwritePermissions()
                - Permission.SendMessages
                - Permission.AddReactions
                - Permission.Speak));
                return;
            }
            
            var permissions = roleOverwrites.Permissions;
            var addPermissionOverwrite = false;
            
            if (!roleOverwrites.Permissions.Denied.SendMessages)
            {
                permissions -= Permission.SendMessages;
                addPermissionOverwrite = true;
            }
            
            if (!roleOverwrites.Permissions.Denied.Speak)
            {
                permissions -= Permission.Speak;
                addPermissionOverwrite = true;
            }

            if (!roleOverwrites.Permissions.Denied.AddReactions)
            {
                permissions -= Permission.AddReactions;
                addPermissionOverwrite = true;
            }
            
            if (addPermissionOverwrite)
                await channel.AddOrModifyOverwriteAsync(new LocalOverwrite(role, permissions));
        }
        
        public class MuteContext
        {
            public CachedGuild? Guild { get; private set; }
            public ulong GuildId { get; }
            public CachedMember? Moderator { get; private set; }
            public CachedMember? Member { get; private set; }
            public ulong UserId { get; }
            public IMessageChannel? SourceChannel { get; private set; }
            public string? Reason { get; }
            public bool SentByTimer { get; }

            private readonly Rias? _riasBot;

            public MuteContext(CachedGuild guild, CachedMember moderator, CachedMember member, IMessageChannel channel, string? reason)
            {
                Guild = guild;
                GuildId = guild.Id;
                Moderator = moderator;
                Member = member;
                UserId = member.Id;
                SourceChannel = channel;
                Reason = reason;
            }

            public MuteContext(Rias riasBot, ulong guildId, ulong moderatorId, ulong userId, ulong channelId)
            {
                _riasBot = riasBot;

                Guild = riasBot.GetGuild(guildId);
                GuildId = guildId;
                Moderator = Guild?.GetMember(moderatorId);
                Member = Guild?.GetMember(userId);
                UserId = userId;
                SourceChannel = Guild?.GetTextChannel(channelId);
                SentByTimer = true;
            }

            public void Update()
            {
                Guild = _riasBot?.GetGuild(GuildId);
                Moderator = Guild?.GetMember(Moderator?.Id ?? 0);
                Member = Guild?.GetMember(UserId);
                SourceChannel = Guild?.GetTextChannel(SourceChannel?.Id ?? 0);
            }
        }
    }
}