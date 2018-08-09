using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Music.Common;
using RiasBot.Modules.Music.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RiasBot.Services;

namespace RiasBot.Modules.Music
{
    public class Music : RiasModule<MusicService>
    {
        private readonly IBotCredentials _creds;
        private readonly InteractiveService _is;

        public Music(IBotCredentials creds, InteractiveService interactiveService)
        {
            _creds = creds;
            _is = interactiveService;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Play([Remainder] string keywords)
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed("You are not in a voice channel");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel.Id != botVoiceChannel.Id)
                {
                    await Context.Channel.SendErrorEmbed("You are not in the same voice channel with me").ConfigureAwait(false);
                    return;
                }

            var socketGuildUser = await Context.Guild.GetCurrentUserAsync();
            var preconditions = socketGuildUser.GetPermissions(voiceChannel);
            if (!preconditions.Connect)
            {
                await Context.Channel.SendErrorEmbed($"I don't have permission to connect in the channel {Format.Bold(voiceChannel.Name)}");
                return;
            }
            var mp = _service.GetOrAddMusicPlayer(Context.Guild);
            try
            {
                await mp.JoinAudio(Context.Guild, Context.Channel, voiceChannel).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = _creds.GoogleApiKey,
                ApplicationName = "Rias Bot"
            });
            mp.YouTubeService = youtubeService;
            if (Uri.IsWellFormedUriString(keywords, UriKind.Absolute))
            {
                var regex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]*)(?:.*list=|(?:.*/)?)([a-zA-Z0-9-_]*)");
                var matches = regex.Match(keywords);
                var videoId = matches.Groups[1].Value;
                var listId = matches.Groups[2].Value;

                if (!string.IsNullOrEmpty(listId))
                {
                    if (videoId == "playlist")
                    {
                        await PlayList(mp, youtubeService, listId);
                    }
                    else
                    {
                        await PlayList(mp, youtubeService, listId, videoId);
                    }
                }
                else
                {
                    await PlayVideoUrl(mp, youtubeService, videoId);
                }
            }
            else
            {
                await PlayVideo(mp, youtubeService, keywords);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Play()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.TogglePause(false, true).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Pause()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.TogglePause(true, true).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Resume()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.TogglePause(false, true).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task NowPlaying()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.NowPlaying().ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Destroy()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.Destroy("Stopping music playback!").ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Skip()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.Skip().ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task SkipTo(int index)
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.SkipTo(index);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task SkipTo([Remainder]string title)
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.SkipTo(title);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Replay()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.Replay().ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Playlist(int index = 1)
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.Playlist(index - 1).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Volume(int volume)
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.SetVolume(volume).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Shuffle()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.Shuffle().ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Clear()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.Clear().ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Remove(int index)
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.Remove(index - 1).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Remove([Remainder]string title)
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.Remove(title).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Repeat()
        {
            if (!_service.UnlockMusic(Context.Guild.OwnerId))
            {
                await Context.Channel.SendErrorEmbed($"The Music module is for Patreon supporters only. You can support [here]({RiasBot.Patreon}) " +
                                                     "to unlock this feature.");
                return;
            }
            var voiceChannel = ((IVoiceState)Context.User).VoiceChannel;
            if (voiceChannel is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in a voice channel!");
                return;
            }

            var botVoiceChannel = (await Context.Guild.GetCurrentUserAsync()).VoiceChannel;
            if (botVoiceChannel != null)
                if (voiceChannel != botVoiceChannel)
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you are not in the same voice channel with me!").ConfigureAwait(false);
                    return;
                }

            var mp = _service.GetMusicPlayer(Context.Guild);
            if (mp != null)
                await mp.ToggleRepeat().ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        private async Task PlayList(MusicPlayer mp, YouTubeService youtubeService, string playlist, string videoId = null, int index = - 1)
        {
            if (mp.Wait)
                return;

            mp.Wait = true;

            await Context.Channel.SendConfirmationEmbed("Adding songs to the playlist, please wait!").ConfigureAwait(false);
            var items = 0;
            var nextPageToken = "";
            
            while (nextPageToken != null)
            {
                var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
                playlistItemsListRequest.PlaylistId = playlist;
                playlistItemsListRequest.MaxResults = 50;
                playlistItemsListRequest.PageToken = nextPageToken;

                var startPosition = mp.Queue.Count;
                var ids = "";
                try
                {
                    var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync().ConfigureAwait(false);

                    foreach (var playlistItem in playlistItemsListResponse.Items)
                    {
                        if (!string.IsNullOrEmpty(videoId))
                        {
                            var id = playlistItem.Snippet.ResourceId.VideoId;
                            if (id.Equals(videoId))
                                if (playlistItem.Snippet.Position != null)
                                    index = (int) playlistItem.Snippet.Position;
                        }
                        else
                        {
                            index = 0;
                        }
                        
                        var title = playlistItem.Snippet.Title;

                        if (title.Equals("Deleted video"))
                            continue;
                        
                        var itemVideoId = playlistItem.Snippet.ResourceId.VideoId;
                        var url = "https://youtu.be/" + itemVideoId;
                        var channel = playlistItem.Snippet.ChannelTitle;
                        var thumbnail = playlistItem.Snippet.Thumbnails?.Maxres?.Url;
                        if (string.IsNullOrEmpty(thumbnail))
                            thumbnail = playlistItem.Snippet.Thumbnails?.Standard?.Url;
                        if (string.IsNullOrEmpty(thumbnail))
                            thumbnail = playlistItem.Snippet.Thumbnails?.Default__?.Url;

                        await mp.Playlist(title, itemVideoId, url, channel, thumbnail, (IGuildUser)Context.User, index);
                        ids += itemVideoId + ",";
                        if (mp.Destroyed)
                            return;
                        items++;
                    }

                    await mp.LoadSongsLength(ids, startPosition, mp.Queue.Count);
                    nextPageToken = playlistItemsListResponse.NextPageToken;
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed("Something went wrong! Please check if the link of the playlist is available or the link of the video is available!");
                    mp.RegisteringPlaylist = false;
                    mp.Wait = false;
                    return;
                }
            }
            await Context.Channel.SendConfirmationEmbed($"Added to playlist {items} songs").ConfigureAwait(false);
            mp.RegisteringPlaylist = false;
            mp.Wait = false;
        }

        private async Task PlayVideoUrl(MusicPlayer mp, YouTubeService youtubeService, string videoId)
        {
            var user = (IGuildUser)Context.User;

            var videoListRequest = youtubeService.Videos.List("snippet,contentDetails");
            videoListRequest.Id = videoId;

            var videoListResponse = await videoListRequest.ExecuteAsync().ConfigureAwait(false);

            var url = "https://youtu.be/" + videoId;
            var title = videoListResponse.Items.FirstOrDefault()?.Snippet.Title;

            if (!string.IsNullOrEmpty(title))
                if (title.Equals("Deleted video"))
                {
                    await Context.Channel.SendErrorEmbed("The video is not available!");
                }
            
            var channel = videoListResponse.Items.FirstOrDefault()?.Snippet.ChannelTitle;
            var thumbnail = videoListResponse.Items.FirstOrDefault()?.Snippet.Thumbnails.High.Url;

            var duration = TimeSpan.Zero;
            var durationString = videoListResponse.Items.FirstOrDefault()?.ContentDetails.Duration;
            if (!string.IsNullOrEmpty(durationString))
                duration = System.Xml.XmlConvert.ToTimeSpan(durationString);

            if (title != null && thumbnail != null)
            {
                if (duration == TimeSpan.Zero)
                {
                    await Context.Channel.SendErrorEmbed("I can't play live YouTube videos");
                }
                else
                {
                    await mp.Play(title, videoId, url, channel, duration, thumbnail, user).ConfigureAwait(false);
                }
            }
            else
            {
                await Context.Channel.SendErrorEmbed("Please provide a direct and valid YouTube video URL!");
            }
        }

        private async Task PlayVideo(MusicPlayer mp, YouTubeService youtubeService, string keywords)
        {
            var user = (IGuildUser)Context.User;

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = keywords;
            searchListRequest.MaxResults = 15;

            var searchListResponse = await searchListRequest.ExecuteAsync().ConfigureAwait(false);
            var videoListRequest = youtubeService.Videos.List("contentDetails");

            var videosList = new List<VideoDetails>();
            var videosIds = "";
            var description = "";
            var index = 0;
            foreach (var searchResult in searchListResponse.Items)
            {
                if (searchResult.Id.VideoId != null && index < 5)
                {
                    var videoDetails = new VideoDetails
                    {
                        Id = searchResult.Id.VideoId,
                        Url = "https://youtu.be/" + searchResult.Id.VideoId,
                        Title = searchResult.Snippet.Title,
                        Channel = searchResult.Snippet.ChannelTitle,
                        Thumbnail = searchResult.Snippet.Thumbnails.High.Url
                    };

                    videosIds += searchResult.Id.VideoId + ",";
                    videosList.Add(videoDetails);
                    index++;
                }
                else
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(videosIds))
            {
                await Context.Channel.SendErrorEmbed("I couldn't find anything!").ConfigureAwait(false);
                return;
            }
            videosIds = videosIds.Remove(videosIds.Length - 1);
            videoListRequest.Id = videosIds;
            var videoResponse = await videoListRequest.ExecuteAsync().ConfigureAwait(false);
            
            for (var i = 0; i < videosList.Count; i++)
            {
                videosList[i].Duration = System.Xml.XmlConvert.ToTimeSpan(videoResponse.Items[i].ContentDetails.Duration);
                description += $"#{i+1} {videosList[i].Title} {Format.Code($"({videosList[i].Duration})")}\n";
            }
            
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithTitle("Choose a song by typing the index. You have 1 minute");
            embed.WithDescription(description);
            var choose = await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);

            string userInput = null;
            var getUserInput = await _is.NextMessageAsync((ShardedCommandContext)Context, timeout: TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            if (getUserInput != null)
                userInput = getUserInput.Content.Replace("#", "");
            if (int.TryParse(userInput, out var input))
            {
                input--;
                if (input >= 0 && input < 5)
                {
                    await choose.DeleteAsync().ConfigureAwait(false);
                    if (videosList[input].Title != null && videosList[input].Url != null && videosList[input].Thumbnail != null)
                    {
                        if (videosList[input].Duration == new TimeSpan(0, 0, 0))
                        {
                            await Context.Channel.SendErrorEmbed("I can't play live YouTube videos");
                        }
                        else
                        {
                            await mp.Play(videosList[input].Title, videosList[input].Id, videosList[input].Url,
                                videosList[input].Channel, videosList[input].Duration, videosList[input].Thumbnail, user).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("Please provide a direct YouTube video URL");
                    }
                }
                else
                {
                    await choose.DeleteAsync().ConfigureAwait(false);
                }
            }
            else
            {
                await choose.DeleteAsync().ConfigureAwait(false);
            }
        }
    }

    public class VideoDetails
    {
        public string Title;
        public string Id;
        public string Url;
        public string Channel;
        public string Thumbnail;
        public TimeSpan Duration;
    }
}
