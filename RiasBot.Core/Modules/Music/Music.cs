﻿using Discord;
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
using System.Web;
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
        [Priority(0)]
        public async Task Play([Remainder] string keywords)
        {
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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

            var socketGuildUser = await Context.Guild.GetCurrentUserAsync();
            var preconditions = socketGuildUser.GetPermissions(voiceChannel);
            if (!preconditions.Connect)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I don't have permission to connect in the channel {Format.Bold(voiceChannel.Name)}!");
                return;
            }
            try
            {
                var mp = await _service.GetOrAddMusicPlayer(Context.Guild);
                await mp.JoinAudio(Context.Guild, Context.Channel, voiceChannel).ConfigureAwait(false);

                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = _creds.GoogleApiKey,
                    ApplicationName = "Rias Bot"
                });

                if (Uri.IsWellFormedUriString(keywords, UriKind.Absolute))
                {
                    var regex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]*)(?:.*list=|(?:.*/)?)([a-zA-Z0-9-_]*)");
                    var matches = regex.Match(keywords);
                    string videoId = matches.Groups[1].Value;
                    string listId = matches.Groups[2].Value;

                    if (!String.IsNullOrEmpty(listId))
                    {
                        if (videoId == "playlist")
                        {
                            await PlayList(mp, youtubeService, listId, index: 0);
                        }
                        else
                        {
                            await PlayList(mp, youtubeService, listId, videoId);
                        }
                    }
                    else
                    {
                        await PlayVideoURL(mp, youtubeService, videoId);
                    }
                }
                else
                {
                    await PlayVideo(mp, youtubeService, keywords);
                }
            }
            catch
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} something went wrong. I can't connect to the voice channel, speak or to see the voice channel.").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Play(int index)
        {
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
                await mp.PlayByIndex(index - 1);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(2)]
        public async Task Play()
        {
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
        public async Task Replay()
        {
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
        public async Task Playlist(int currentPage = 1)
        {
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
                await mp.Playlist((ShardedCommandContext)Context, _is).ConfigureAwait(false);
            else
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I'm not in a voice channel");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        public async Task Volume(int volume)
        {
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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
            if (Context.Guild.Id != RiasBot.SupportServer)
                return;
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

        private async Task PlayList(MusicPlayer mp, YouTubeService youtubeService, string playlist, string videoId = null, int index = -1)
        {
            if (mp.waited)
                return;

            mp.waited = true;

            string title = null;
            string url = null;
            string thumbnail = null;
            TimeSpan duration = new TimeSpan();
            var user = (IGuildUser)Context.User;

            await mp.Clear().ConfigureAwait(false);

            await Context.Channel.SendConfirmationEmbed("Adding songs to the playlist, please wait!").ConfigureAwait(false);
            int items = 0;
            var nextPageToken = "";
            while (nextPageToken != null)
            {
                var playlistItemsListRequest = youtubeService.PlaylistItems.List("snippet");
                var videoListRequest = youtubeService.Videos.List("contentDetails");
                playlistItemsListRequest.PlaylistId = playlist;
                playlistItemsListRequest.MaxResults = 50;
                playlistItemsListRequest.PageToken = nextPageToken;

                try
                {
                    var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync().ConfigureAwait(false);

                    foreach (var playlistItem in playlistItemsListResponse.Items)
                    {
                        if (items < mp.queueLimit)
                        {
                            try
                            {
                                string id = playlistItem.Snippet.ResourceId.VideoId;
                                if (id == videoId)
                                    index = (int?)playlistItem.Snippet.Position ?? -1;

                                videoListRequest.Id = playlistItem.Snippet.ResourceId.VideoId;
                                var videoListResponse = await videoListRequest.ExecuteAsync().ConfigureAwait(false);
                                title = playlistItem.Snippet.Title;
                                url = "https://youtu.be/" + playlistItem.Snippet.ResourceId.VideoId;
                                duration = System.Xml.XmlConvert.ToTimeSpan(videoListResponse.Items.FirstOrDefault().ContentDetails.Duration);
                                thumbnail = playlistItem.Snippet.Thumbnails.High.Url;

                                if (title != null && url != null && duration != new TimeSpan(0, 0, 0))
                                {
                                    await mp.Playlist((IGuildUser)Context.User, youtubeService, videoListRequest, playlistItem, index);
                                    if (mp.destroyed)
                                        return;
                                    items++;
                                }
                            }
                            catch
                            {

                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    nextPageToken = playlistItemsListResponse.NextPageToken;
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed("Please provide a direct and unlisted or public YouTube playlist URL!");
                    mp.waited = false;
                    return;
                }
            }
            await Context.Channel.SendConfirmationEmbed($"Added to playlist {items} songs").ConfigureAwait(false);
            mp.registeringPlaylist = false;
            if (index > -1)
                mp.waited = false;
            else
                await Task.Factory.StartNew(() => mp.UpdateQueue(0));
        }

        private async Task PlayVideoURL(MusicPlayer mp, YouTubeService youtubeService, string videoId)
        {
            string title = null;
            string url = null;
            string channel = null;
            string thumbnail = null;
            TimeSpan duration = new TimeSpan();
            var user = (IGuildUser)Context.User;

            var videoListRequestSnippet = youtubeService.Videos.List("snippet");
            var videoListRequestContentDetails = youtubeService.Videos.List("contentDetails");
            videoListRequestSnippet.Id = videoId;
            videoListRequestContentDetails.Id = videoId;

            try
            {
                var videoListResponseSnippet = await videoListRequestSnippet.ExecuteAsync().ConfigureAwait(false);
                var videoListResponseContentDetails = await videoListRequestContentDetails.ExecuteAsync().ConfigureAwait(false);

                url = "https://youtu.be/" + videoId;
                title = videoListResponseSnippet.Items.FirstOrDefault().Snippet.Title;
                channel = videoListResponseSnippet.Items.FirstOrDefault().Snippet.ChannelTitle;
                thumbnail = videoListResponseSnippet.Items.FirstOrDefault().Snippet.Thumbnails.High.Url;

                duration = System.Xml.XmlConvert.ToTimeSpan(videoListResponseContentDetails.Items.FirstOrDefault().ContentDetails.Duration);

                if (title != null && url != null && thumbnail != null)
                {
                    if (duration == new TimeSpan(0, 0, 0))
                    {
                        await Context.Channel.SendErrorEmbed("I can't play live YouTube videos");
                        return;
                    }
                    else
                    {
                        await mp.Play(title, url, channel, duration, thumbnail, user).ConfigureAwait(false);
                    }
                }
                else
                {
                    await Context.Channel.SendErrorEmbed("Please provide a direct YouTube video URL!");
                    return;
                }
            }
            catch
            {

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
            string description = null;
            int index = 0;
            foreach (var searchResult in searchListResponse.Items)
            {
                if (searchResult.Id.VideoId != null && index < 5)
                {
                    var videoDetails = new VideoDetails();
                    videoDetails.url = "https://youtu.be/" + searchResult.Id.VideoId;
                    videoDetails.title = searchResult.Snippet.Title;
                    videoDetails.channel = searchResult.Snippet.ChannelTitle;
                    videoDetails.thumbnail = searchResult.Snippet.Thumbnails.High.Url;

                    videoListRequest.Id = searchResult.Id.VideoId;
                    var videoResponse = await videoListRequest.ExecuteAsync().ConfigureAwait(false);
                    videoDetails.duration = System.Xml.XmlConvert.ToTimeSpan(videoResponse.Items.FirstOrDefault().ContentDetails.Duration);

                    if (videoDetails.duration != new TimeSpan(0, 0, 0))
                    {
                        description += $"#{index+1} {videoDetails.title} {Format.Code($"({videoDetails.duration})")}\n";
                        videosList.Add(videoDetails);
                        index++;
                    }
                }
            }
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithTitle("Choose a song by typing the index. You have 30 seconds");
            embed.WithDescription(description);
            var choose = await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);

            string userInput = null;
            var getUserInput = await _is.NextMessageAsync((ShardedCommandContext)Context, timeout: TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            if (getUserInput != null)
                userInput = getUserInput.Content.Replace("#", "");
            if (Int32.TryParse(userInput, out int input))
            {
                input--;
                if (input >= 0 && input < 5)
                {
                    await choose.DeleteAsync().ConfigureAwait(false);
                    if (videosList[input].title != null && videosList[input].url != null && videosList[input].thumbnail != null)
                    {
                        if (videosList[input].duration == new TimeSpan(0, 0, 0))
                        {
                            await Context.Channel.SendErrorEmbed("I can't play live YouTube videos");
                            return;
                        }
                        else
                        {
                            await mp.Play(videosList[input].title, videosList[input].url,
                                videosList[input].channel, videosList[input].duration, videosList[input].thumbnail, user).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("Please provide a direct YouTube video URL!");
                        return;
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
        public string title = null;
        public string url = null;
        public string channel = null;
        public string thumbnail = null;
        public TimeSpan duration = new TimeSpan();
    }
}
