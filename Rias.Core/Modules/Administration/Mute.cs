using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Services;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Mute")]
        public class Mute : RiasModule<MuteService>
        {
            public Mute(IServiceProvider services) : base(services) {}

            [Command("mute"), Context(ContextType.Guild),
             UserPermission(GuildPermission.MuteMembers),
             BotPermission(GuildPermission.MuteMembers | GuildPermission.ManageRoles | GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task MuteAsync(SocketGuildUser user, [Remainder] string? reason = null)
            {
                if (user.Id == Context.User.Id)
                {
                    Context.Command.ResetCooldowns();
                    return;
                }

                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync("CannotMuteOwner");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAboveMe");
                    return;
                }
                
                if (((SocketGuildUser) Context.User).CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                await Service.MuteUserAsync(Context.Channel, (SocketGuildUser) Context.User, user, reason);
            }

            [Command("mute"), Context(ContextType.Guild),
             UserPermission(GuildPermission.MuteMembers),
             BotPermission(GuildPermission.MuteMembers | GuildPermission.ManageRoles | GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task MuteAsync(SocketGuildUser user, TimeSpan timeout, [Remainder] string? reason = null)
            {
                if (user.Id == Context.User.Id)
                {
                    Context.Command.ResetCooldowns();
                    return;
                }

                var culture = Resources.GetGuildCulture(Context.Guild!.Id);

                var lowestTimeout = TimeSpan.FromMinutes(1);
                if (timeout < lowestTimeout)
                {
                    await ReplyErrorAsync("MuteTimeoutLowest", lowestTimeout.Humanize(1, culture));
                    return;
                }

                var now = DateTime.UtcNow;
                var highestTimeout = now.AddYears(1) - now;
                if (timeout > highestTimeout)
                {
                    await ReplyErrorAsync("MuteTimeoutHighest", highestTimeout.Humanize(1, culture, TimeUnit.Year));
                    return;
                }

                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync("CannotMuteOwner");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAboveMe");
                    return;
                }
                
                if (((SocketGuildUser) Context.User).CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                await Service.MuteUserAsync(Context.Channel, (SocketGuildUser) Context.User, user, reason, timeout);
            }

            [Command("unmute"), Context(ContextType.Guild),
             UserPermission(GuildPermission.MuteMembers),
             BotPermission(GuildPermission.MuteMembers | GuildPermission.ManageRoles | GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task UnmuteAsync(SocketGuildUser user, [Remainder] string? reason = null)
            {
                if (user.Id == Context.User.Id)
                {
                    Context.Command.ResetCooldowns();
                    return;
                }

                if (user.Id == Context.Guild!.OwnerId)
                {
                    Context.Command.ResetCooldowns();
                    return;
                }

                var muteContext = new MuteService.MuteContext(Context.Guild!, (SocketGuildUser) Context.User,
                    user, Context.Channel, reason);
                await Service.UnmuteUserAsync(muteContext);
            }

            [Command("setmute"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles | GuildPermission.ManageChannels),
             BotPermission(GuildPermission.ManageRoles | GuildPermission.ManageChannels),
             Cooldown(1, 30, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(1)]
            public async Task SetMuteAsync([Remainder] SocketRole role)
            {
                if (role.IsEveryone) return;
                if (Context.CurrentGuildUser!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync("RoleAboveMe");
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync("MuteRoleNotSet");
                    return;
                }

                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                guildDb.MuteRoleId = role.Id;
                await DbContext.SaveChangesAsync();
                
                await RunTaskAsync(Service.AddMuteRoleToChannelsAsync(role, Context.Guild!));
                await ReplyConfirmationAsync("NewMuteRoleSet");
            }

            [Command("setmute"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageRoles | GuildPermission.ManageChannels),
             BotPermission(GuildPermission.ManageRoles | GuildPermission.ManageChannels),
             Cooldown(1, 30, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(0)]
            public async Task SetMuteAsync([Remainder] string name)
            {
                var role = await Context.Guild!.CreateRoleAsync(name, isMentionable: false);
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                guildDb.MuteRoleId = role.Id;
                await DbContext.SaveChangesAsync();
                
                await RunTaskAsync(Service.AddMuteRoleToChannelsAsync(role, Context.Guild!));
                await ReplyConfirmationAsync("NewMuteRoleSet");
            }
        }
    }
}