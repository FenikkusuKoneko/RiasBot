using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Database;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;
using Serilog;

namespace Rias.Services
{
    public class MuteService : RiasService
    {
#if DEBUG
        public const string MuteRole = "rias-mute-dev";
#elif RELEASE
        public const string MuteRole = "rias-mute";
#endif
        
        private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), Timer> _timers = new ConcurrentDictionary<(ulong, ulong), Timer>();
        
        public MuteService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            LoadTimers = new Timer(_ => RunTaskAsync(LoadTimersAsync), null, TimeSpan.Zero, TimeSpan.FromDays(7));
        }
        
        private Timer LoadTimers { get; }

        public async Task MuteUserAsync(
            DiscordChannel channel,
            DiscordMember moderator,
            DiscordMember member,
            string? reason,
            TimeSpan? timeout = null,
            bool sentByWarning = false)
        {
            var guild = member.Guild;

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = await db.Guilds.FirstOrDefaultAsync(x => x.GuildId == guild.Id);

            var role = (guild.GetRole(guildDb?.MuteRoleId ?? 0)
                        ?? guild.Roles.FirstOrDefault(x => string.Equals(x.Value.Name, MuteRole) && !x.Value.IsManaged).Value)
                       ?? await guild.CreateRoleAsync(MuteRole);

            var currentMember = guild.CurrentMember;
            if (currentMember.CheckRoleHierarchy(role) <= 0)
            {
                await ReplyErrorAsync(channel, guild.Id, Localization.AdministrationMuteRoleAbove, role.Name);
                return;
            }

            if (member.Roles.Any(r => r.Id == role.Id) && !sentByWarning)
            {
                await ReplyErrorAsync(channel, guild.Id, Localization.AdministrationUserAlreadyMuted, member.FullName());
                return;
            }

            await RunTaskAsync(AddMuteRoleToChannelsAsync(role, guild));
            await member.GrantRoleAsync(role);

            await RunTaskAsync(AddMuteAsync(channel, moderator, member, timeout));

            var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.Yellow,
                    Description = GetText(guild.Id, Localization.AdministrationUserMuted)
                }
                .AddField(GetText(guild.Id, Localization.CommonUser), member.FullName(), true)
                .AddField(GetText(guild.Id, Localization.CommonId), member.Id.ToString(), true)
                .AddField(GetText(guild.Id, Localization.AdministrationModerator), moderator.FullName(), true);

            if (!string.IsNullOrEmpty(reason))
                embed.AddField(GetText(guild.Id, Localization.CommonReason), reason, true);

            embed.WithThumbnail(member.GetAvatarUrl(ImageFormat.Auto));
            embed.WithCurrentTimestamp();

            if (timeout.HasValue)
            {
                var locale = Localization.GetGuildLocale(guild.Id);
                embed.AddField(GetText(guild.Id, Localization.AdministrationMutedFor), timeout.Value.Humanize(5, new CultureInfo(locale), TimeUnit.Year), true);
            }

            var modLogChannel = guild.GetChannel(guildDb?.ModLogChannelId ?? 0);
            if (modLogChannel != null && channel.Id != modLogChannel.Id)
            {
                var preconditions = currentMember.PermissionsIn(modLogChannel);
                if (preconditions.HasPermission(Permissions.AccessChannels) && preconditions.HasPermission(Permissions.SendMessages))
                {
                    if (!sentByWarning)
                        await channel.SendConfirmationMessageAsync(GetText(guild.Id, Localization.AdministrationUserWasMuted, member.FullName(), modLogChannel.Mention));
                    else
                        await channel.SendConfirmationMessageAsync(GetText(guild.Id, Localization.AdministrationUserWasWarned, member.FullName(), modLogChannel.Mention));
                    
                    channel = modLogChannel;
                }
            }

            await channel.SendMessageAsync(embed);
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

            if (context.Member.Roles.All(r => r.Id != role.Id))
            {
                if (!context.SentByTimer)
                    await ReplyErrorAsync(context.SourceChannel!, context.Guild.Id, Localization.AdministrationUserNotMuted, context.Member);
                await RemoveMuteAsync(context);
                return;
            }

            await RunTaskAsync(RemoveMuteAsync(context));
            await context.Member.RevokeRoleAsync(role);

            var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.Yellow,
                    Description = GetText(context.Guild.Id, Localization.AdministrationUserUnmuted)
                }
                .AddField(GetText(context.Guild.Id, Localization.CommonUser), context.Member.FullName(), true)
                .AddField(GetText(context.Guild.Id, Localization.CommonId), context.MemberId.ToString(), true)
                .AddField(GetText(context.Guild.Id, Localization.AdministrationModerator), context.SentByTimer ? currentMember.FullName() : context.Moderator?.FullName() ?? "-", true);

            if (!string.IsNullOrEmpty(context.Reason))
                embed.AddField(GetText(context.Guild.Id, Localization.CommonReason), context.Reason, true);

            if (context.SentByTimer)
                embed.AddField(GetText(context.Guild.Id, Localization.CommonReason), GetText(context.Guild.Id, Localization.CommonTimesUp), true);

            embed.WithThumbnail(context.Member.GetAvatarUrl(ImageFormat.Auto));
            embed.WithCurrentTimestamp();

            var channel = context.SourceChannel;
            var modLogChannel = context.Guild.GetChannel(guildDb?.ModLogChannelId ?? 0);
            if (modLogChannel != null)
            {
                var preconditionsModLog = currentMember.PermissionsIn(modLogChannel);
                if (preconditionsModLog.HasPermission(Permissions.AccessChannels) && preconditionsModLog.HasPermission(Permissions.SendMessages))
                {
                    if (!context.SentByTimer && channel != null)
                        await channel.SendConfirmationMessageAsync(GetText(context.Guild.Id, Localization.AdministrationUserWasUnmuted, context.Member.FullName(), modLogChannel.Mention));
                    channel = modLogChannel;
                }
            }

            if (channel is null)
                return;

            var preconditionsChannel = currentMember.PermissionsIn(channel);
            if (preconditionsChannel.HasPermission(Permissions.AccessChannels) && preconditionsChannel.HasPermission(Permissions.SendMessages))
                await channel.SendMessageAsync(embed);
        }
        
        public async Task AddMuteRoleToChannelsAsync(DiscordRole role, DiscordGuild guild)
        {
            var categories = guild.Channels.Where(x => x.Value.Type == ChannelType.Category);
            foreach (var (_, category) in categories)
            {
                if (guild.CurrentMember.PermissionsIn(category).HasPermission(Permissions.AccessChannels))
                    await AddPermissionOverwriteAsync(category, role);
            }

            var channels = guild.Channels;
            foreach (var (_, channel) in channels)
            {
                if (guild.CurrentMember.PermissionsIn(channel).HasPermission(Permissions.AccessChannels))
                    await AddPermissionOverwriteAsync(channel, role);
            }
        }
        
        private async Task LoadTimersAsync()
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var muteTimersDb = await db.MuteTimers.ToListAsync();

            var dateTime = DateTime.UtcNow.AddDays(7);
            foreach (var muteTimerDb in muteTimersDb)
            {
                if (dateTime <= muteTimerDb.Expiration)
                    continue;

                var context = new MuteContext(RiasBot, muteTimerDb.GuildId, muteTimerDb.ModeratorId, muteTimerDb.UserId, muteTimerDb.MuteChannelSourceId);
                var dueTime = muteTimerDb.Expiration - DateTime.UtcNow;
                if (dueTime < TimeSpan.Zero)
                    dueTime = TimeSpan.Zero;
                var muteTimer = new Timer(_ => RunTaskAsync(UnmuteUserAsync(context)), null, dueTime, TimeSpan.Zero);
                _timers.TryAdd((muteTimerDb.GuildId, muteTimerDb.UserId), muteTimer);
            }

            Log.Debug("Mute timers loaded");
        }
        
        private async Task AddMuteAsync(DiscordChannel channel, DiscordMember moderator, DiscordMember member, TimeSpan? timeout)
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
            var timer = new Timer(_ => RunTaskAsync(UnmuteUserAsync(muteContext)), null, timeout.Value, TimeSpan.Zero);
            _timers.TryAdd((guild.Id, member.Id), timer);
            {
                Log.Debug("Mute timer added");
            }
        }
        
        private async Task RemoveMuteAsync(MuteContext context)
        {
            if (_timers.TryRemove((context.GuildId, context.MemberId), out var muteTimer))
            {
                await muteTimer.DisposeAsync();
                Log.Debug("Mute timer removed");
            }

            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userGuildDb = await db.GuildUsers.FirstOrDefaultAsync(x => x.GuildId == context.GuildId && x.UserId == context.MemberId);
            if (userGuildDb != null)
                userGuildDb.IsMuted = false;

            var muteTimerDb = await db.MuteTimers.FirstOrDefaultAsync(x => x.GuildId == context.GuildId && x.UserId == context.MemberId);
            if (muteTimerDb != null)
                db.Remove(muteTimerDb);

            await db.SaveChangesAsync();
        }

        private async Task AddPermissionOverwriteAsync(DiscordChannel channel, DiscordRole role)
        {
            var roleOverwrites = channel.PermissionOverwrites.FirstOrDefault(x => x.Type == OverwriteType.Role && x.Id == role.Id);
            if (roleOverwrites is null)
            {
                await channel.AddOverwriteAsync(role, deny: Permissions.SendMessages | Permissions.AddReactions | Permissions.Speak);
                return;
            }

            var permissions = roleOverwrites.Denied;

            if (!permissions.HasPermission(Permissions.SendMessages))
                permissions |= Permissions.SendMessages;
            
            if (!permissions.HasPermission(Permissions.Speak))
                permissions |= Permissions.Speak;

            if (!permissions.HasPermission(Permissions.AddReactions))
                permissions |= Permissions.AddReactions;
            
            if (permissions > roleOverwrites.Denied)
                await channel.AddOverwriteAsync(role, deny: permissions);
        }
        
        public class MuteContext
        {
            public readonly DiscordGuild? Guild;
            public readonly ulong GuildId;
            public DiscordMember? Moderator;
            public readonly ulong ModeratorId;
            public DiscordMember? Member;
            public readonly ulong MemberId;
            public readonly DiscordChannel? SourceChannel;
            public readonly string? Reason;
            public readonly bool SentByTimer;

            public MuteContext(DiscordGuild guild, DiscordMember moderator, DiscordMember member, DiscordChannel channel, string? reason)
            {
                Guild = guild;
                GuildId = guild.Id;
                Moderator = moderator;
                ModeratorId = moderator.Id;
                Member = member;
                MemberId = member.Id;
                SourceChannel = channel;
                Reason = reason;
            }

            public MuteContext(RiasBot riasBot, ulong guildId, ulong moderatorId, ulong memberId, ulong channelId)
            {
                Guild = riasBot.GetGuild(guildId);
                GuildId = guildId;
                MemberId = memberId;

                if (Guild != null)
                {
                    if (Guild.Members.TryGetValue(moderatorId, out var moderator))
                        Moderator = moderator;
                    
                    if (Guild.Members.TryGetValue(memberId, out var member))
                        Member = member;
                    
                    SourceChannel = Guild.GetChannel(channelId);
                }

                SentByTimer = true;
            }

            public void Update()
            {
                if (Guild == null)
                {
                    Moderator = null;
                    Member = null;
                    return;
                }

                Moderator = Guild.Members.TryGetValue(ModeratorId, out var moderator)
                    ? moderator
                    : null;

                Member = Guild.Members.TryGetValue(MemberId, out var member)
                    ? member
                    : null;
            }
        }
    }
}