using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Music.Services;
using System.Threading.Tasks;

namespace RiasBot.Modules.Music
{
    public class Music : RiasModule<MusicService>
    {
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Play([Remainder] string keywords)
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

            await _service.SearchTrack((ShardedCommandContext) Context, Context.Guild, Context.Channel,
                (IGuildUser) Context.User, voiceChannel, keywords);
        }

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Pause()
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
                await mp.Pause("The music player has been paused!").ConfigureAwait(false);
        }
        
        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Resume()
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
                await mp.Resume("The music player has been resumed!").ConfigureAwait(false);
        }
        
        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task NowPlaying()
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
                await mp.NowPlaying().ConfigureAwait(false);
        }
        
        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Volume(string volume)
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
                await mp.SetVolume(volume).ConfigureAwait(false);
        }
        
        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Seek(string time)
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
                await mp.Seek(time, (IGuildUser)Context.User).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Leave()
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
                    await mp.Leave(Context.Guild, $"Left {Format.Bold(mp.VoiceChannel.ToString())}").ConfigureAwait(false);
                else
                    await mp.Leave(Context.Guild, null).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Queue(int index = 1)
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
                await mp.Playlist(index - 1).ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Skip()
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
                await mp.Skip().ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task SkipTo(int index)
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
                await mp.SkipTo(index - 1).ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task SkipTo([Remainder]string title)
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
                await mp.SkipTo(title).ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Repeat()
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
        public async Task Replay()
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
                await mp.Replay().ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Clear()
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
                await mp.Clear().ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Shuffle()
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
                await mp.Shuffle().ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Remove(int index)
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
                await mp.Remove(index - 1).ConfigureAwait(false);
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Remove([Remainder]string title)
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
                await mp.Remove(title).ConfigureAwait(false);
        }
    }
}
