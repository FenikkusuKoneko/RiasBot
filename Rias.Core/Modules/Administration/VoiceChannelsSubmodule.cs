using System;
using System.Threading.Tasks;
using Disqord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Voice Channels")]
        public class VoiceChannelsSubmodule : RiasModule
        {
            public VoiceChannelsSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("createvoicechannel"), Context(ContextType.Guild),
             UserPermission(Permission.ManageChannels), BotPermission(Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateVoiceChannelAsync([Remainder] string name)
            {
                if (name.Length < 1 || name.Length > 100)
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelNameLengthLimit, 1, 100);
                    return;
                }

                await Context.Guild!.CreateVoiceChannelAsync(name);
                await ReplyConfirmationAsync(Localization.AdministrationVoiceChannelCreated, name);
            }

            [Command("deletevoicechannel"), Context(ContextType.Guild),
             UserPermission(Permission.ManageChannels), BotPermission(Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteVoiceChannelAsync([Remainder] CachedVoiceChannel channel)
            {
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, channel))
                {
                    await ReplyErrorAsync(Localization.AdministrationVoiceChannelNoViewPermission);
                    return;
                }

                await channel.DeleteAsync();
                await ReplyConfirmationAsync(Localization.AdministrationVoiceChannelDeleted, channel.Name);
            }

            [Command("renamevoicechannel"), Context(ContextType.Guild),
             UserPermission(Permission.ManageChannels), BotPermission(Permission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task RenameVoiceChannelAsync([Remainder] string names)
            {
                var namesSplit = names.Split("->");
                if (namesSplit.Length < 2)
                    return;

                var oldName = namesSplit[0].TrimEnd();
                var newName = namesSplit[1].TrimStart();

                var channel = Context.Guild!.GetVoiceChannel(oldName);

                if (channel is null)
                {
                    await ReplyErrorAsync(Localization.AdministrationVoiceChannelNotFound);
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentMember!, channel))
                {
                    await ReplyErrorAsync(Localization.AdministrationVoiceChannelNoViewPermission);
                    return;
                }

                if (newName.Length < 1 || newName.Length > 100)
                {
                    await ReplyErrorAsync(Localization.AdministrationChannelNameLengthLimit, 1, 100);
                    return;
                }

                oldName = channel.Name;
                await channel.ModifyAsync(x => x.Name = newName);
                await ReplyConfirmationAsync(Localization.AdministrationVoiceChannelRenamed, oldName, newName);
            }
        }
    }
}