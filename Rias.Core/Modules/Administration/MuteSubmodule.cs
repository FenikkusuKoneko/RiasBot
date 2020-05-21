using System;
using System.Globalization;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Humanizer.Localisation;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Mute")]
        public class MuteSubmodule : RiasModule<MuteService>
        {
            public MuteSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("mute"), Context(ContextType.Guild),
             UserPermission( Permission.MuteMembers),
             BotPermission(Permission.MuteMembers | Permission.ManageRoles | Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(0)]
            public async Task MuteAsync(CachedMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                {
                    Context.Command.ResetCooldowns();
                    return;
                }

                if (member.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotMuteOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAbove);
                    return;
                }

                await Service.MuteUserAsync(Context.Channel, (CachedMember) Context.User, member, reason);
            }
            
            [Command("mute"), Context(ContextType.Guild),
             UserPermission(Permission.MuteMembers),
             BotPermission(Permission.MuteMembers | Permission.ManageRoles | Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild),
            Priority(1)]
            public async Task MuteAsync(CachedMember member, TimeSpan timeout, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                {
                    Context.Command.ResetCooldowns();
                    return;
                }

                var locale = Localization.GetGuildLocale(Context.Guild!.Id);

                var lowestTimeout = TimeSpan.FromMinutes(1);
                if (timeout < lowestTimeout)
                {
                    await ReplyErrorAsync(Localization.AdministrationMuteTimeoutLowest, lowestTimeout.Humanize(1, new CultureInfo(locale)));
                    return;
                }

                var now = DateTime.UtcNow;
                var highestTimeout = now.AddYears(1) - now;
                if (timeout > highestTimeout)
                {
                    await ReplyErrorAsync(Localization.AdministrationMuteTimeoutHighest, highestTimeout.Humanize(1, new CultureInfo(locale), TimeUnit.Year));
                    return;
                }

                if (member.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotMuteOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAbove);
                    return;
                }

                await Service.MuteUserAsync(Context.Channel, (CachedMember) Context.User, member, reason, timeout);
            }
            
            [Command("unmute"), Context(ContextType.Guild),
             UserPermission(Permission.MuteMembers),
             BotPermission(Permission.MuteMembers | Permission.ManageRoles | Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task UnmuteAsync(CachedMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                {
                    Context.Command.ResetCooldowns();
                    return;
                }

                if (member.Id == Context.Guild!.OwnerId)
                {
                    Context.Command.ResetCooldowns();
                    return;
                }

                var muteContext = new MuteService.MuteContext(Context.Guild!, (CachedMember) Context.User,
                    member, Context.Channel, reason);
                await Service.UnmuteUserAsync(muteContext);
            }
            
            [Command("setmute"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles | Permission.ManageChannels),
             BotPermission(Permission.ManageRoles | Permission.ManageChannels),
             Cooldown(1, 30, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(1)]
            public async Task SetMuteAsync([Remainder] CachedRole role)
            {
                if (role.IsDefault) return;
                if (Context.CurrentMember!.CheckRoleHierarchy(role) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationRoleAboveMe);
                    return;
                }

                if (role.IsManaged)
                {
                    await ReplyErrorAsync(Localization.AdministrationMuteRoleNotSet);
                    return;
                }

                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                guildDb.MuteRoleId = role.Id;
                await DbContext.SaveChangesAsync();
                
                await RunTaskAsync(Service.AddMuteRoleToChannelsAsync(role, Context.Guild!));
                await ReplyConfirmationAsync(Localization.AdministrationNewMuteRoleSet);
            }

            [Command("setmute"), Context(ContextType.Guild),
             UserPermission(Permission.ManageRoles | Permission.ManageChannels),
             BotPermission(Permission.ManageRoles | Permission.ManageChannels),
             Cooldown(1, 30, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(0)]
            public async Task SetMuteAsync([Remainder] string name)
            {
                var role = await Context.Guild!.CreateRoleAsync(x => x.Name = name);
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                guildDb.MuteRoleId = role.Id;
                await DbContext.SaveChangesAsync();
                
                await RunTaskAsync(Service.AddMuteRoleToChannelsAsync(role, Context.Guild!));
                await ReplyConfirmationAsync(Localization.AdministrationNewMuteRoleSet);
            }
        }
    }
}