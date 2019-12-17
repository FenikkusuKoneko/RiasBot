using System;
using System.Threading.Tasks;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Services;

namespace Rias.Core.Modules.Music
{
    [Name("Music")]
    public class Music : RiasModule<MusicService>
    {
        public Music(IServiceProvider services) : base(services) {}

        [Command("play"), Context(ContextType.Guild)]
        public async Task PlayAsync([Remainder] string query)
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (Creds.LavalinkConfig is null)
            {
                await ReplyErrorAsync("LavalinkNotConfigured");
                return;
            }

            if (!Service.Lavalink.IsConnected)
            {
                await ReplyErrorAsync("LavalinkNotConnected");
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;
            
            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
            {
                player = await Service.InitializePlayerAsync(voiceChannel, (SocketTextChannel) Context.Channel, (SocketGuildUser) Context.User);
            }
            
            await ValidateOutputChannelAsync(player);
            await player.PlayAsync(Context.Message, query);
            */
        }

        [Command("leave"), Context(ContextType.Guild)]
        public async Task LeaveAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (Creds.LavalinkConfig is null)
            {
                await ReplyErrorAsync("LavalinkNotConfigured");
                return;
            }

            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;
            
            if (Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
            {
                await player.LeaveAndDisposeAsync();
            }
            */
        }

        [Command("pause"), Context(ContextType.Guild)]
        public async Task PauseAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }

            await player.PauseAsync();
            */
        }
        
        [Command("resume"), Context(ContextType.Guild)]
        public async Task ResumeAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }

            await player.ResumeAsync();
            */
        }
        
        [Command("queue"), Context(ContextType.Guild),
        Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
        public async Task QueueAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }

            await player.QueueAsync(Context.Message);
            */
        }
        
        [Command("nowplaying"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
        public async Task NowPlayingAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }

            await player.NowPlayingAsync();
            */
        }
        
        [Command("skip"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
        public async Task SkipAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }

            await player.SkipAsync();
            */
        }

        [Command("skipto"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
        public async Task SkipToAsync([Remainder] string title)
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }

            await player.SkipToAsync(title);
            */
        }
        
        [Command("seek"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
        public async Task SeekAsync(TimeSpan position)
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }

            await player.SeekAsync(position);
            */
        }
        
        [Command("replay"), Context(ContextType.Guild)]
        public async Task ReplayAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }
            
            await player.ReplayAsync();
            */
        }
        
        [Command("volume"), Context(ContextType.Guild)]
        public async Task VolumeAsync(int? volume = null)
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }
            
            await player.SetVolumeAsync(volume);
            */
        }
        
        [Command("shuffle"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
        public async Task ShuffleAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }
            
            await player.ShuffleAsync();
            */
        }
        
        [Command("clear"), Context(ContextType.Guild)]
        public async Task ClearAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }
            
            await player.ClearAsync();
            */
        }
        
        [Command("remove"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
        public async Task RemoveAsync([Remainder] string title)
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }

            await player.RemoveAsync(title);
            */
        }
        
        [Command("repeat"), Context(ContextType.Guild)]
        public async Task RepeatAsync()
        {
            await ReplyErrorAsync("Disabled");

            /*
            if (!Service.Lavalink.IsConnected)
            {
                return;
            }
            
            var voiceChannel = ((SocketGuildUser) Context.User).VoiceChannel;
            if (!await CheckAsync(voiceChannel))
                return;

            if (!Service.Lavalink.TryGetPlayer(Context.Guild, out var player))
                return;

            await ValidateOutputChannelAsync(player);
            if (player.TextChannel.Id != Context.Channel.Id)
            {
                await ReplyErrorAsync("OutputChannelCommands", player.TextChannel);
                return;
            }
            
            await player.ToggleRepeatAsync();
            */
        }

        // private async Task<bool> CheckAsync(SocketVoiceChannel voiceChannel)
        // {
        //     if (voiceChannel is null)
        //     {
        //         await ReplyErrorAsync("UserNotInVoiceChannel");
        //         return false;
        //     }
        //
        //     var botVoiceChannel = Context.CurrentGuildUser!.VoiceChannel;
        //     if (botVoiceChannel != null && voiceChannel.Id != botVoiceChannel.Id)
        //     {
        //         await ReplyErrorAsync("NotSameVoiceChannel");
        //         return false;
        //     }
        //
        //     var preconditions = Context.CurrentGuildUser.GetPermissions(voiceChannel);
        //     if (!preconditions.Connect)
        //     {
        //         await ReplyErrorAsync("NoConnectPermission", voiceChannel.Name);
        //         return false;
        //     }
        //
        //     return true;
        // }
        //
        // private async Task ValidateOutputChannelAsync(MusicPlayer player)
        // {
        //     var outputChannelState = Service.CheckOutputChannel(Context.Guild!.Id, player.TextChannel);
        //     switch (outputChannelState)
        //     {
        //         case OutputChannelState.Null:
        //             Service.Lavalink.UpdateTextChannel(Context.Guild, (SocketTextChannel) Context.Channel);
        //             await Context.Channel.SendErrorMessageAsync($"{GetText("NullOutputChannel")}\n" +
        //                                                         $"{GetText("NewOutputChannel")}");
        //             break;
        //         case OutputChannelState.NoViewPermission:
        //             Service.Lavalink.UpdateTextChannel(Context.Guild, (SocketTextChannel) Context.Channel);
        //             await Context.Channel.SendErrorMessageAsync($"{GetText("OutputChannelNoViewPermission", player.TextChannel.Name)}\n" +
        //                                                         $"{GetText("NewOutputChannel")}");
        //             break;
        //         case OutputChannelState.NoSendPermission:
        //             Service.Lavalink.UpdateTextChannel(Context.Guild, (SocketTextChannel) Context.Channel);
        //             await Context.Channel.SendErrorMessageAsync($"{GetText("OutputChannelNoSendPermission", player.TextChannel.Name)}\n" +
        //                                                         $"{GetText("NewOutputChannel")}");
        //             break;
        //         default: return;
        //     }
        // }
    }
}