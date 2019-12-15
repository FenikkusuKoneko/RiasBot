using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Voice Channels")]
        public class VoiceChannels : RiasModule
        {
            public VoiceChannels(IServiceProvider services) : base(services)
            {
            }

            [Command("createvoicechannel"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task CreateVoiceChannelAsync([Remainder] string name)
            {
                if (name.Length < 1 || name.Length > 100)
                {
                    await ReplyErrorAsync("ChannelNameLengthLimit", 1, 100);
                    return;
                }

                await Context.Guild!.CreateVoiceChannelAsync(name);
                await ReplyConfirmationAsync("VoiceChannelCreated", name);
            }

            [Command("deletevoicechannel"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            public async Task DeleteVoiceChannelAsync([Remainder] SocketVoiceChannel channel)
            {
                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, channel))
                {
                    await ReplyErrorAsync("VoiceChannelNoViewPermission");
                    return;
                }

                await channel.DeleteAsync();
                await ReplyConfirmationAsync("VoiceChannelDeleted", channel.Name);
            }

            [Command("renamevoicechannel"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageChannels), BotPermission(GuildPermission.ManageChannels),
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
                    await ReplyErrorAsync("VoiceChannelNotFound");
                    return;
                }

                if (!ChannelExtensions.CheckViewChannelPermission(Context.CurrentGuildUser!, channel))
                {
                    await ReplyErrorAsync("VoiceChannelNoViewPermission");
                    return;
                }

                if (newName.Length < 1 || newName.Length > 100)
                {
                    await ReplyErrorAsync("ChannelNameLengthLimit", 1, 100);
                    return;
                }

                oldName = channel.Name;
                await channel.ModifyAsync(x => x.Name = newName);
                await ReplyConfirmationAsync("VoiceChannelRenamed", oldName, newName);
            }
        }
    }
}