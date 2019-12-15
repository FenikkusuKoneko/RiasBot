using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MoreLinq.Extensions;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Interactive;
using Rias.Interactive.Paginator;
using Socks;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Rest;

namespace Rias.Core.Services.Commons
{
    public class MusicPlayer : LavaPlayer
    {
#nullable disable
        private MusicService _service;
#nullable enable

        private readonly List<(LavaTrack, SocketUser)> _queue = new List<(LavaTrack, SocketUser)>();
        private readonly List<string> _queueString = new List<string>();
        private (LavaTrack LavaTrack, SocketUser User) _currentTrack;
        private TimeSpan _totalDuration = TimeSpan.Zero;
        private PlayerPatreonFeatures _features;
        private bool _repeat;
        private readonly TimeSpan _maximumTrackDuration = TimeSpan.FromHours(3);

        public ulong GuildId => VoiceChannel.GuildId;

        public readonly TrackTime CurrentTime = new TrackTime();
        public Timer? AutoDisconnectTimer;
        
        public MusicPlayer(ClientSock sock, IVoiceChannel voiceChannel, ITextChannel textChannel) : base(sock, voiceChannel, textChannel) {}

        public void Initialize(MusicService service, PlayerPatreonFeatures features)
        {
            _service = service;
            _features = features;
        }

        public async Task PlayAsync(SocketUserMessage message, string query)
        {
            var searchResponse = await LoadTracksAsync(query);
            if (!searchResponse.HasValue)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "NoYouTubeUrl");
                return;
            }

            (LavaTrack, SocketUser)? track = null;
            switch (searchResponse.Value.LoadType)
            {
                case LoadType.NoMatches:
                    await _service.ReplyErrorAsync(TextChannel, GuildId, "TrackLoadNoMatches");
                    return;
                case LoadType.LoadFailed:
                    await _service.ReplyErrorAsync(TextChannel, GuildId, "TrackLoadFailed", _service.Creds.OwnerServerInvite);
                    return;
                case LoadType.SearchResult:
                    track = await ChooseTrackAsync(message, searchResponse.Value.Tracks);
                    break;
                case LoadType.TrackLoaded:
                    track = (searchResponse.Value.Tracks.FirstOrDefault(), message.Author);
                    break;
                case LoadType.PlaylistLoaded:
                    await AddToQueueAsync(searchResponse.Value, message.Author);
                    break;
            }

            if (!track.HasValue && _queue.Count > 0)
                track = _queue[0];
            
            if (!track.HasValue)
                return;
            
            if (track.Value.Item1.IsStream && !_features.HasFlag(PlayerPatreonFeatures.Livestream))
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerFeatureLivestream", _service.Creds.Patreon);
                return;
            }
            
            if (track.Value.Item1.Duration > _maximumTrackDuration && !_features.HasFlag(PlayerPatreonFeatures.LongTracks))
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerFeatureLongTracks", _maximumTrackDuration.Hours, _service.Creds.Patreon);
                return;
            }
            
            switch (PlayerState)
            {
                case PlayerState.Connected:
                case PlayerState.Stopped:
                    await PlayNextTrackAsync(track.Value);
                    break;
                case PlayerState.Playing:
                case PlayerState.Paused:
                    await AddToQueueAsync(track.Value);
                    break;
            }
        }
        
        public async Task PlayNextTrackAsync((LavaTrack LavaTrack, SocketUser User)? track = null, bool bypassRepeat = false)
        {
            if (!track.HasValue)
            {
                if (_repeat && !bypassRepeat)
                {
                    track = _currentTrack;
                }
                else
                {
                    if (_queue.Count == 0)
                        return;

                    track = _queue[0];
                    _queue.RemoveAt(0);
                    _queueString.RemoveAt(0);
                    if (!track.Value.LavaTrack.IsStream)
                        _totalDuration -= track.Value.LavaTrack.Duration;
                }
            }
            else
            {
                var rangeDuration = TimeSpan.Zero;
                var index = 0;
                foreach (var (lavaTrack, _) in _queue)
                {
                    if (!lavaTrack.IsStream)
                        rangeDuration += lavaTrack.Duration;
                    if (lavaTrack == track.Value.LavaTrack)
                    {
                        _queue.RemoveRange(0, index + 1);
                        _queueString.RemoveRange(0, index + 1);
                        _totalDuration -= rangeDuration;
                        break;
                    }

                    index++;
                }
            }

            await PlayAsync(track.Value.LavaTrack);
            CurrentTime.Restart();
            
            if (!_repeat) 
                _currentTrack = track.Value;
            
            var outputChannelState = _service.CheckOutputChannel(GuildId, TextChannel);
            if (outputChannelState != OutputChannelState.Available)
                return;

            var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Author = new EmbedAuthorBuilder
                    {
                        Name = _service.GetText(GuildId, "NowPlaying"),
                        IconUrl = track.Value.User!.GetRealAvatarUrl()
                    },
                    Description = $"[{track.Value.LavaTrack.Title}]({track.Value.LavaTrack.Url})"
                }.AddField(_service.GetText(GuildId, "TrackChannel"), track.Value.LavaTrack.Author, true)
                .AddField(_service.GetText(GuildId, "TrackDuration"), track.Value.LavaTrack.IsStream
                    ? _service.GetText(GuildId, "Livestream")
                    : track.Value.LavaTrack.Duration.DigitalTimeSpanString(), true)
                .AddField(_service.GetText(GuildId, "RequestedBy"), track.Value.User, true);

            if (_repeat)
                embed.AddField(_service.GetText(GuildId, "Repeat"), _service.GetText(GuildId, "#Utility_Enabled"), true);

            await TextChannel.SendMessageAsync(embed);
        }

        public async Task LeaveAndDisposeAsync(bool sendMessage = true)
        {
            var guildId = GuildId;
            var voiceChannelName = VoiceChannel.Name;
            await _service.Lavalink.LeaveAsync(VoiceChannel);
            if (!sendMessage) return;
            await _service.ReplyConfirmationAsync(TextChannel, guildId, "ChannelDisconnected", voiceChannelName);
        }

        public async Task PauseAsync(bool sendMessage = true)
        {
            switch (PlayerState)
            {
                case PlayerState.Stopped:
                    await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerNotPlaying");
                    return;
                case PlayerState.Paused:
                    await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerIsPaused");
                    return;
            }

            await base.PauseAsync();
            CurrentTime.Stop();
            if (sendMessage)
                await _service.ReplyConfirmationAsync(TextChannel, GuildId, "PlayerPaused");
        }
        
        public async Task ResumeAsync(bool sendMessage = true)
        {
            switch (PlayerState)
            {
                case PlayerState.Stopped:
                    await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerNotPlaying");
                    return;
                case PlayerState.Playing:
                    await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerIsPlaying");
                    return;
            }

            await base.ResumeAsync();
            CurrentTime.Start();
            if (sendMessage)
                await _service.ReplyConfirmationAsync(TextChannel, GuildId, "PlayerResumed");
        }

        public async Task QueueAsync(SocketUserMessage message)
        {
            if (_queue.Count == 0)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "QueueEmpty");
                return;
            }

            var status = PlayerState switch
            {
                PlayerState.Playing => "⏸",
                PlayerState.Paused => "▶",
                _ => "⏹"
            };

            var index = 0;
            var pages = _queueString.Batch(15, x =>
            {
                var duration = !_currentTrack.LavaTrack.IsStream ? _currentTrack.LavaTrack.Duration.DigitalTimeSpanString() : _service.GetText(GuildId, "Livestream");
                var currentTrackString = $"{status} [{_currentTrack.LavaTrack.Title}]({_currentTrack.LavaTrack.Url}) `{duration}`\n" + 
                                         "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬\n";
                
                return new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Title = _service.GetText(GuildId, "Queue"),
                        Color = RiasUtils.ConfirmColor,
                        Description = currentTrackString + string.Join("\n", x.Select(track => $"#{++index} {track}")),
                        Footer = new EmbedFooterBuilder().WithText(_service.GetText(GuildId, "TotalDuration", _totalDuration.DigitalTimeSpanString()))
                    }
                );
            });

            await _service.Interactive.SendPaginatedMessageAsync(message, new PaginatedMessage(pages));
        }

        public async Task NowPlayingAsync()
        {
            if (!(PlayerState == PlayerState.Playing || PlayerState == PlayerState.Paused))
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerNotPlaying");
                return;
            }
            
            var currentPositionElapsed = CurrentTime.Elapsed;
            var elapsedTime = currentPositionElapsed.DigitalTimeSpanString();
            
            if (!_currentTrack.LavaTrack.IsStream)
            {
                var timerBar = new StringBuilder();
                var position = currentPositionElapsed.TotalMilliseconds / _currentTrack.LavaTrack.Duration.TotalMilliseconds * 30;
                for (var i = 0; i < 30; i++)
                {
                    timerBar.Append(i == (int) position ? "⚫" : "▬");
                }

                elapsedTime = $"`{timerBar}`\n`{currentPositionElapsed.DigitalTimeSpanString()}/{_currentTrack.LavaTrack.Duration.DigitalTimeSpanString()}`";
            }

            var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = _service.GetText(GuildId, "NowPlaying"),
                    Description = $"[{_currentTrack.LavaTrack.Title}]({_currentTrack.LavaTrack.Url})\n\n{elapsedTime}"
                }.AddField(_service.GetText(GuildId, "TrackChannel"), _currentTrack.LavaTrack.Author, true)
                .AddField(_service.GetText(GuildId, "TrackDuration"), _currentTrack.LavaTrack.IsStream
                    ? _service.GetText(GuildId, "Livestream")
                    : _currentTrack.LavaTrack.Duration.DigitalTimeSpanString(), true)
                .AddField(_service.GetText(GuildId, "RequestedBy"), _currentTrack.User, true);
            
            if (_repeat)
                embed.AddField(_service.GetText(GuildId, "Repeat"), _service.GetText(GuildId, "#Utility_Enabled"), true);

            await TextChannel.SendMessageAsync(embed);
        }

        public async Task SkipAsync()
        {
            if (_queue.Count == 0)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "QueueEmpty");
                return;
            }
            
            await PlayNextTrackAsync(bypassRepeat: true);
        }
        
        public async Task SkipToAsync(string title)
        {
            if (_queue.Count == 0)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "QueueEmpty");
                return;
            }

            var trackIndex = GetTrackIndex(title);
            if (trackIndex < 0)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "TrackIndexLessThan", 1);
                return;
            }
            
            if (trackIndex >= _queue.Count)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "TrackIndexAbove");
                return;
            }

            if (!trackIndex.HasValue)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "TrackNotFound");
                return;
            }

            await PlayNextTrackAsync(_queue[trackIndex.Value]);
        }

        public async Task SeekAsync(TimeSpan position)
        {
            if (!(PlayerState == PlayerState.Playing || PlayerState == PlayerState.Paused))
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerNotPlaying");
                return;
            }

            if (position > _currentTrack.LavaTrack.Duration)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "SeekPositionAbove");
                return;
            }
            
            var currentPosition = CurrentTime.Elapsed;
            await base.SeekAsync(position);
            CurrentTime.Update(position);

            var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = _service.GetText(GuildId, "Seek")
                }.AddField(_service.GetText(GuildId, "SeekFrom"), currentPosition.DigitalTimeSpanString(), true)
                .AddField(_service.GetText(GuildId, "SeekTo"), CurrentTime.Elapsed.DigitalTimeSpanString(), true);

            await TextChannel.SendMessageAsync(embed);
        }

        public async Task ReplayAsync()
        {
            if (!(PlayerState == PlayerState.Playing || PlayerState == PlayerState.Paused))
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerNotPlaying");
                return;
            }
            
            await PlayNextTrackAsync(_currentTrack);
        }

        public async Task SetVolumeAsync(int? volume)
        {
            if (!volume.HasValue)
            {
                await _service.ReplyConfirmationAsync(TextChannel, GuildId, "CurrentVolume", Volume);
                return;
            }
            
            if (!_features.HasFlag(PlayerPatreonFeatures.Volume))
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerFeatureVolume", _service.Creds.Patreon);
                return;
            }
            
            if (volume < 0 || volume > 100)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "VolumeBetween", 0, 100);
                return;
            }

            if (!(PlayerState == PlayerState.Playing || PlayerState == PlayerState.Paused))
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "PlayerNotPlaying");
                return;
            }

            await UpdateVolumeAsync((ushort) volume);
            await _service.ReplyConfirmationAsync(TextChannel, GuildId, "VolumeSet", volume);
        }

        public async Task ShuffleAsync()
        {
            if (_queue.Count == 0)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "QueueEmpty");
                return;
            }
            
            var seed = new Random().Next();
            _queue.Shuffle(new Random(seed));
            _queueString.Shuffle(new Random(seed));
            await _service.ReplyConfirmationAsync(TextChannel, GuildId, "QueueShuffled");
        }
        
        public async Task ClearAsync()
        {
            if (_queue.Count == 0)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "QueueEmpty");
                return;
            }
            
            _queue.Clear();
            _queueString.Clear();
            await _service.ReplyConfirmationAsync(TextChannel, GuildId, "QueueCleared");
        }

        public async Task RemoveAsync(string title)
        {
            if (_queue.Count == 0)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "QueueEmpty");
                return;
            }

            var trackIndex = GetTrackIndex(title);
            if (trackIndex < 0)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "TrackIndexLessThan", 1);
                return;
            }
            
            if (trackIndex >= _queue.Count)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "TrackIndexAbove");
                return;
            }

            if (!trackIndex.HasValue)
            {
                await _service.ReplyErrorAsync(TextChannel, GuildId, "TrackNotFound");
                return;
            }

            var (lavaTrack, _) = _queue[trackIndex.Value];
            _queue.RemoveAt(trackIndex.Value);
            _queueString.RemoveAt(trackIndex.Value);

            await _service.ReplyConfirmationAsync(TextChannel, GuildId, "TrackRemoved", lavaTrack.Title);
        }

        public async Task ToggleRepeatAsync()
        {
            _repeat = !_repeat;
            var repeatKey = _repeat ? "RepeatEnabled" : "RepeatDisabled";
            await _service.ReplyConfirmationAsync(TextChannel, GuildId, repeatKey);
        }

        private async Task<SearchResponse?> LoadTracksAsync(string query)
        {
            if (Uri.IsWellFormedUriString(query, UriKind.Absolute))
            {
                if (!(query.Contains("youtube.com") || query.Contains("youtu.be")))
                    return null;
                
                YoutubeUrl? youtubeUrl = null;
                if (!query.Contains("playlist"))
                {
                    youtubeUrl = SanitizeYoutubeUrl(query);
                    if (youtubeUrl != null)
                        query = string.Format(_service.YoutubeUrl, youtubeUrl.VideoId, youtubeUrl.ListId);
                }
                
                if (!string.IsNullOrEmpty(youtubeUrl?.ListId))
                    await _service.ReplyConfirmationAsync(TextChannel, GuildId, "EnqueuingTracks");
                
                return await _service.Lavalink.SearchAsync(query);
            }

            return await _service.Lavalink.SearchYouTubeAsync(query);
        }
        
        private static YoutubeUrl? SanitizeYoutubeUrl(string url)
        {
            var regex = new Regex(@"(?:(?:youtube\.com/watch\?v=)|(?:youtu.be/))(?<videoId>[a-zA-Z0-9-_]+)(?:(?:.*list=)(?<listId>[a-zA-Z0-9-_]+))?",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

            var match = regex.Match(url);

            if (match.Length == 0)
                return null;


            var groups = match.Groups;
            if (groups.Count > 2)
            {
                return new YoutubeUrl
                {
                    VideoId = match.Groups["videoId"].Value,
                    ListId = match.Groups["listId"].Value
                };
            }

            return null;
        }
        
        private async Task<(LavaTrack, SocketUser)?> ChooseTrackAsync(SocketUserMessage message, ICollection<LavaTrack> tracks)
        {
            var description = new StringBuilder();
            var length = tracks.Count;
            if (length > 10)
                length = 10;

            var index = 0;
            foreach (var track in tracks)
            {
                if (index > length)
                    break;
                
                description.Append($"#{++index} [{track.Title}]({track.Url}) `{track.Duration.DigitalTimeSpanString()}`").Append("\n");
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = _service.GetText(GuildId, "ChooseTrack"),
                Description = description.ToString()
            };

            await TextChannel.SendMessageAsync(embed);
            var userInput = await _service.Interactive.NextMessageAsync(message);
            if (userInput == null) return null;
            
            var content = userInput.Content.Replace("#", "");
            if (!int.TryParse(content, out index)) return null;
            
            if (index > 0 && index <= tracks.Count)
            {
                return (tracks.ElementAt(index - 1), message.Author);
            }

            return null;
        }

        private async Task AddToQueueAsync(SearchResponse searchResponse, SocketUser user)
        {
            var tracks = searchResponse.Tracks.Where(x =>
            {
                if (x.IsStream && !_features.HasFlag(PlayerPatreonFeatures.Livestream))
                    return false;
                return x.Duration <= _maximumTrackDuration || _features.HasFlag(PlayerPatreonFeatures.LongTracks);
            }).ToList();
            
            _queue.AddRange(tracks.Select(x => (x, user)));
            _queueString.AddRange(tracks.Select(x =>
            {
                var duration = !x.IsStream ? x.Duration.DigitalTimeSpanString() : _service.GetText(GuildId, "Livestream");
                return $"[{x.Title}]({x.Url}) `{duration}`";
            }));
            
            var trackListPosition = searchResponse.Playlist.SelectedTrack;
            if (trackListPosition > 0)
            {
                var track = _queue[trackListPosition];
                _queue.RemoveAt(trackListPosition);
                _queue.Insert(0, track);
                
                _queueString.RemoveAt(trackListPosition);
                var duration = !track.Item1.IsStream ? track.Item1.Duration.DigitalTimeSpanString() : _service.GetText(GuildId, "Livestream");
                _queueString.Insert(0, $"[{track.Item1.Title}]({track.Item1.Url}) `{duration}`");
            }

            await _service.ReplyConfirmationAsync(TextChannel, GuildId, "TracksEnqueued",
                searchResponse.Tracks.Count, searchResponse.Playlist.Name);
        }

        private async Task AddToQueueAsync((LavaTrack LavaTrack, SocketUser User) track)
        {
            _queue.Add(track);
            var duration = !track.LavaTrack.IsStream ? track.LavaTrack.Duration.DigitalTimeSpanString() : _service.GetText(GuildId, "Livestream");
            _queueString.Add($"[{track.LavaTrack.Title}]({track.LavaTrack.Url}) `{duration}`");
            if (!track.LavaTrack.IsStream)
                _totalDuration += track.LavaTrack.Duration;
            
            var currentTrackDuration = _currentTrack.LavaTrack.IsStream ? TimeSpan.Zero : _currentTrack.LavaTrack.Duration;
            var etp = _totalDuration + (currentTrackDuration - CurrentTime.Elapsed);

            var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Author = new EmbedAuthorBuilder
                    {
                        Name = _service.GetText(GuildId, "TrackEnqueued"),
                        IconUrl = track.User.GetRealAvatarUrl()
                    },
                    Description = $"[{track.LavaTrack.Title}]({track.LavaTrack.Url})"
                }.AddField(_service.GetText(GuildId, "TrackChannel"), track.LavaTrack.Author, true)
                .AddField(_service.GetText(GuildId, "TrackDuration"), track.LavaTrack.IsStream
                    ? _service.GetText(GuildId, "Livestream")
                    : track.LavaTrack.Duration.DigitalTimeSpanString(), true)
                .AddField(_service.GetText(GuildId, "TrackEtp"), etp.DigitalTimeSpanString(), true);

            if (_repeat)
                embed.AddField(_service.GetText(GuildId, "Repeat"), _service.GetText(GuildId, "#Utility_Enabled"), true);

            await TextChannel.SendMessageAsync(embed);
        }

        private int? GetTrackIndex(string title)
        {
            int? index = null;
            var indexTitle = title;
            if (title.StartsWith("#"))
                indexTitle = title[1..];
            
            if (int.TryParse(indexTitle, out var ind))
                index = ind;
            
            if (index.HasValue)
            {
                index -= 1;
            }
            else
            {
                ind = _queue.FindIndex(x => x.Item1.Title.Contains(title, StringComparison.InvariantCultureIgnoreCase));
                if (ind != -1) index = ind;
            }

            return index;
        }

        public class TrackTime
        {
            private readonly Stopwatch _stopwatch;
            private TimeSpan _offset;

            public TimeSpan Elapsed => _stopwatch.Elapsed + _offset;

            public TrackTime()
            {
                _stopwatch = new Stopwatch();
                _offset = TimeSpan.Zero;
            }

            public void Start()
            {
                _stopwatch.Start();
            }

            public void Stop()
            {
                _stopwatch.Stop();
                _offset = TimeSpan.Zero;
            }

            public void Restart()
            {
                _stopwatch.Restart();
                _offset = TimeSpan.Zero;
            }

            public void Update(TimeSpan offset)
            {
                _offset = offset;
                _stopwatch.Restart();
            }
        }
    }
}