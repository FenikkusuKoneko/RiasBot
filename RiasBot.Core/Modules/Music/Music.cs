using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Music.Services;
using Victoria;

namespace RiasBot.Modules.Music
{
    public class Music : RiasModule<MusicService>
    {
        private readonly Lavalink _lavalink;

        public Music(Lavalink lavalink)
        {
            _lavalink = lavalink;
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task PlayAsync([Remainder] string keywords)
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync("You are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel.Id != botVoiceChannel.Id)
                {
                    await Context.Channel.SendErrorMessageAsync("You are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var socketGuildUser = await Context.Guild.GetCurrentUserAsync();
            var preconditions = socketGuildUser.GetPermissions(voiceChannel);
            if (!preconditions.Connect)
            {
                await Context.Channel.SendErrorMessageAsync($"I don't have permission to connect in the channel {Format.Bold(voiceChannel.Name)}!");
                return;
            }

            if (_service.LavaNode != null)
            {
                await _service.SearchTrackAsync((ShardedCommandContext) Context, Context.Guild, Context.Channel,
                    voiceChannel, (IGuildUser) Context.User, keywords);
            }
            else
            {
                await Context.Channel.SendErrorMessageAsync("Lavalink has not started yet! Please wait few seconds!").ConfigureAwait(false);
            }
        }
        
        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task PauseAsync()
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.PauseAsync("The music player has been paused!").ConfigureAwait(false);
        }
        
        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task ResumeAsync()
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.ResumeAsync("The music player has been resumed!").ConfigureAwait(false);
        }
        
        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task NowPlayingAsync()
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.NowPlayingAsync().ConfigureAwait(false);
        }
        
        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task VolumeAsync(string volume)
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.SetVolumeAsync(volume).ConfigureAwait(false);
        }
        
        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task SeekAsync(string time)
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.SeekAsync(time, (IGuildUser)Context.User).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task LeaveAsync()
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                if (mp.VoiceChannel != null)
                    await mp.LeaveAsync(Context.Guild, $"Left {Format.Bold(mp.VoiceChannel.ToString())}").ConfigureAwait(false);
                else
                    await mp.LeaveAsync(Context.Guild, null).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task QueueAsync(int index = 1)
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.PlaylistAsync(index - 1).ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task SkipAsync()
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.SkipAsync().ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task SkipToAsync(int index)
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.SkipToAsync(index - 1).ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task SkipToAsync([Remainder]string title)
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.SkipToAsync(title).ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task RepeatAsync()
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
            {
                if (!mp.Repeat)
                {
                    mp.Repeat = true;
                    await Context.Channel.SendConfirmationMessageAsync("Repeat enabled!").ConfigureAwait(false);
                }
                else
                {
                    mp.Repeat = false;
                    await Context.Channel.SendConfirmationMessageAsync("Repeat disabled!").ConfigureAwait(false);
                }
            }
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task ReplayAsync()
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.ReplayAsync().ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task ClearAsync()
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.ClearAsync().ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task ShuffleAsync()
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.ShuffleAsync().ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task RemoveAsync(int index)
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.RemoveAsync(index - 1).ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task RemoveAsync([Remainder]string title)
        {
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }
            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.RemoveAsync(title).ConfigureAwait(false);
        }
    }
}