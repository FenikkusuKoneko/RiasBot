using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RiasBot.Extensions;
using RiasBot.Modules.Music.Services;
using Victoria;
using Victoria.Objects;

namespace RiasBot.Modules.Music.Commons
{
    public class MusicPlayer
    {
        private readonly MusicService _service;
        
        public MusicPlayer(MusicService service)
        {
            _service = service;
        }

        private IGuild _guild;
        private IMessageChannel _channel;
        public IVoiceChannel VoiceChannel;

        public LavaPlayer Player;
        private Song _currentTrack;
        private readonly List<Song> _queue = new List<Song>();
        
        private readonly Stopwatch _elapsedTime = new Stopwatch();
        private TimeSpan _offsetElapsedTime = TimeSpan.Zero;
        
        public Timer Timeout;
        
        //Patreon features
        //For self-hosters they will be unlocked
        public bool UnlockVolume;
        public bool UnlockLongTracks;
        public bool UnlockLivestreams;

        public bool IsPaused;
        public bool RegisteringPlaylist;
        public bool Repeat;

        private class Song
        {
            public LavaTrack Track { get; set; }
            public IGuildUser User { get; set; }
            public string Source { get; set; }
            public string Thumbnail { get; set; }
            public string TrackId { get; set; }
        }
        
        private async Task JoinAsync(IGuild guild, IMessageChannel channel, IVoiceChannel voiceChannel)
        {
            _guild = guild;
            VoiceChannel = voiceChannel;
            if (Player is null)
            {
                _channel = channel;
                Player = await _service.LavaNode.JoinAsync(voiceChannel, channel);
                await SendMessageAsync(MessageType.Confirmation, $"Connected to {Format.Bold(voiceChannel.ToString())}!").ConfigureAwait(false);
            }
        }
        
        public async Task PlayAsync(IGuild guild, IGuildUser user, IMessageChannel channel, IVoiceChannel voiceChannel,
            string source, LavaTrack track)
        {
            await JoinAsync(guild, channel, voiceChannel);
            
            if (_queue.Count > 5000)    //5000 tracks are enough, you don't keep the music player online for years
            {
                await SendMessageAsync(MessageType.Error, "The queue is too heavy, please remove some tracks or clear it!").ConfigureAwait(false);
                return;
            }

            if (!UnlockLongTracks)
            {
                if (track.IsSeekable)
                {
                    if (TimeSpan.Compare(track.Length, new TimeSpan(3, 5, 0)) > 0)    //a little exception of 5 minutes
                    {
                        await SendMessageAsync(MessageType.Error, "I cannot play tracks over 3 hours!").ConfigureAwait(false);
                        return;
                    }
                }
            }

            if (!UnlockLivestreams)
            {
                if (track.IsStream)
                {
                    await SendMessageAsync(MessageType.Error, "I cannot play livestreams!").ConfigureAwait(false);
                    return;
                }
            }
            
            var song = new Song
            {
                Track = track,
                User = user,
                Source = source,
                TrackId = _service.GetYouTubeTrackId(track.Uri)
            };

            if (source.Equals("youtube"))
            {
                song.Thumbnail = $"https://img.youtube.com/vi/{_service.GetYouTubeTrackId(track.Uri)}/maxresdefault.jpg";
            }
            else if (source.Equals("soundcloud"))
            {
                song.Thumbnail = await _service.GetTrackThumbnail(track.Uri);
            }

            if (_queue.Count > 0)
            {
                await AddToQueueAsync(song, user);
            }
            else
            {
                if (_currentTrack != null)
                {
                    await AddToQueueAsync(song, user);
                }
                else
                {
                    _currentTrack = song;
                    await UpdateQueueAsync();
                }
            }
        }
        
        public async Task AddPlaylistAsync(IGuild guild, IGuildUser user, IMessageChannel channel, IVoiceChannel voiceChannel,
            string source, LavaResult tracks)
        {
            await JoinAsync(guild, channel, voiceChannel);
            
            if (_queue.Count > 10000)    //10000 tracks are enough, you don't keep the music player online for years
            {
                await channel.SendErrorMessageAsync("The queue is too heavy, please remove some tracks or clear it!").ConfigureAwait(false);
                return;
            }

            if (_channel is null)
                _channel = channel;
            
            var message = await SendMessageAsync(MessageType.Confirmation, "Adding tracks to queue, please wait!").ConfigureAwait(false);
            RegisteringPlaylist = true;
            
            var count = 0;
            foreach (var track in tracks.Tracks)
            {
                if (!UnlockLongTracks)
                {
                    if (track.IsSeekable)
                    {
                        if (TimeSpan.Compare(track.Length, new TimeSpan(3, 5, 0)) > 0)    //a little exception of 5 minutes
                        {
                            continue;
                        }
                    }
                }
                
                if (!UnlockLivestreams)
                {
                    if (track.IsStream)
                    {
                        continue;
                    }
                }
                
                var song = new Song
                {
                    Track = track,
                    User = user,
                    Source = source,
                    TrackId = _service.GetYouTubeTrackId(track.Uri)
                };
                
                if (source.Equals("youtube"))
                {
                    song.Thumbnail = $"https://img.youtube.com/vi/{_service.GetYouTubeTrackId(track.Uri)}/maxresdefault.jpg";
                }
                _queue.Add(song);
                count++;
            }
            
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithDescription($"Added to queue {count} tracks");
            if (message != null)
                await message.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
            
            if (tracks.PlaylistInfo.SelectedTrack > 0)
            {
                var track = _queue[tracks.PlaylistInfo.SelectedTrack];
                _queue.RemoveAt(tracks.PlaylistInfo.SelectedTrack);
                _queue.Insert(0, track);
            }
            
            if (_currentTrack is null)
            {
                await UpdateQueueAsync(0).ConfigureAwait(false);
            }
            else
            {
                if (!Player.IsConnected)
                {
                    await UpdateQueueAsync(0).ConfigureAwait(false);
                }
            }
            RegisteringPlaylist = false;
        }
        
        public async Task UpdateQueueAsync(int index = -1)
        {
            if (index < _queue.Count)
            {
                if (index >= 0)
                {
                    _currentTrack = _queue[index];
                    _queue.RemoveRange(0, index + 1);
                }
                
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithTitle("Now Playing");
                embed.WithDescription($"[{_currentTrack.Track.Title}]({_currentTrack.Track.Uri})");
                embed.AddField("Channel", _currentTrack.Track.Author, true);
                embed.AddField("Length", _currentTrack.Track.IsStream ?
                    "Livestream" : _currentTrack.Track.Length.GetTimeString() , true);
                embed.AddField("Requested by", $"{_currentTrack.User}", true);
                if (Repeat)
                    embed.AddField("Repeat", "Enabled", true);
                embed.WithThumbnailUrl(_currentTrack.Thumbnail);
                
                Player.Play(_currentTrack.Track);
                _elapsedTime.Restart();
                _offsetElapsedTime = TimeSpan.Zero;
                IsPaused = false;
                await SendMessageEmbedAsync(embed).ConfigureAwait(false);
            }
            else
            {
                IsPaused = true;
                _currentTrack = null;
            }
        }
        
        private async Task AddToQueueAsync(Song song, IGuildUser user)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithAuthor(user);
            embed.WithTitle("Added to queue");
            embed.WithDescription($"[{song.Track.Title}]({song.Track.Uri})");
            embed.AddField("Channel", song.Track.Author, true);

            embed.AddField("Length", song.Track.IsStream ? "Livestream" : song.Track.Length.GetTimeString(), true);

            var eta = TimeSpan.Zero;
            var endless = false;
            if (!_currentTrack.Track.IsStream)
            {
                eta = _currentTrack.Track.Length.Subtract(_offsetElapsedTime.Add(_elapsedTime.Elapsed));
                foreach (var track in _queue)
                {
                    if (!track.Track.IsStream)
                        eta = eta.Add(track.Track.Length);
                    else
                    {
                        endless = true;
                        break;
                    }
                }
            }
            else
            {
                endless = true;
            }
            
            if (!Repeat)
                embed.AddField("Estimated time until playing", endless ?
                    "∞" : eta.GetTimeString(), true).AddField("Position", _queue.Count + 1, true);
            else
                embed.AddField("Estimated time until playing", "Repeat enabled", true);

            embed.WithThumbnailUrl(song.Thumbnail);

            _queue.Add(song);
            await SendMessageEmbedAsync(embed).ConfigureAwait(false);
        }
        
        public async Task PauseAsync(string message)
        {
            if (IsPaused)
                return;
            
            Player.Pause();
            IsPaused = true;
            _elapsedTime.Stop();
            if (!string.IsNullOrEmpty(message))
                await SendMessageAsync(MessageType.Confirmation, message).ConfigureAwait(false);
        }
        
        public async Task ResumeAsync(string message)
        {
            if (!IsPaused)
                return;

            Player.Resume();
            IsPaused = false;
            _elapsedTime.Start();
            if (!string.IsNullOrEmpty(message))
                await SendMessageAsync(MessageType.Confirmation, message).ConfigureAwait(false);
        }
        
        public async Task NowPlayingAsync()
        {
            if (_currentTrack != null && !IsPaused)
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithTitle("Now Playing");
                var description = $"[{_currentTrack.Track.Title}]({_currentTrack.Track.Uri})\n\n";
                
                var elapsedTime = _offsetElapsedTime.Add(_elapsedTime.Elapsed);
                if (!_currentTrack.Track.IsStream)
                {
                    var currentTrackLength = _currentTrack.Track.Length;
                    var timerBar = "";
                    var timerPos = (elapsedTime.TotalMilliseconds / currentTrackLength.TotalMilliseconds) * 30;
                    for (var i = 0; i < 30; i++)
                    {
                        if (i == (int)timerPos)
                            timerBar += "⚫";
                        else
                        {
                            timerBar += "▬";
                        }
                    }
                
                    description += $"{Format.Code(timerBar)}\n" + $"{Format.Code($"{elapsedTime.GetTimeString()}/{currentTrackLength.GetTimeString()}")}";
                }
                else
                {
                    description += Format.Code(elapsedTime.GetTimeString());
                }
                
                embed.AddField("Requested by", _currentTrack.User, true);
                if (Repeat)
                    embed.AddField("Repeat", "Enabled", true);
                embed.WithDescription(description);
                                     
                if (_currentTrack.Source.Equals("youtube"))
                    embed.WithThumbnailUrl(_currentTrack.Thumbnail);

                await SendMessageEmbedAsync(embed).ConfigureAwait(false);
            }
            else
            {
                await SendMessageAsync(MessageType.Error, "No track is running!").ConfigureAwait(false);
            }
        }
        
        public async Task SetVolumeAsync(string volume)
        {
            if (UnlockVolume)
            {
                volume = volume.Replace("%", "");

                if (int.TryParse(volume, out var vol))
                {
                    if (vol >= 0 && vol <= 100)
                    {
                        Player.Volume(vol);
                        await SendMessageAsync(MessageType.Confirmation, $"Volume set to {volume}%").ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await SendMessageAsync(MessageType.Error, "Changing the volume is a Patreon feature!").ConfigureAwait(false);
            }
        }
        
        public async Task SeekAsync(string inputTime, IGuildUser user)
        {
            var time = Extensions.Extensions.ConvertToTimeSpan(inputTime);
            if (_currentTrack.Track.IsSeekable)
            {
                if (TimeSpan.Compare(time, _currentTrack.Track.Length) <= 0)
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithAuthor("Seek", user.GetRealAvatarUrl());
                    embed.WithDescription($"[{_currentTrack.Track.Title}]({_currentTrack.Track.Uri})");
                    
                    Player.Seek(time);

                    embed.AddField("From", _offsetElapsedTime.Add(_elapsedTime.Elapsed).GetTimeString(), true)
                        .AddField("To", time.GetTimeString(), true);

                    await SendMessageEmbedAsync(embed).ConfigureAwait(false);
                    
                    _offsetElapsedTime = time;
                    _elapsedTime.Restart();
                }
                else
                {
                    await SendMessageAsync(MessageType.Error, "The time is over the track's length!").ConfigureAwait(false);
                }
            }
            else
            {
                await SendMessageAsync(MessageType.Error, "The current track is not seekable!").ConfigureAwait(false);
            }
        }
        
        public async Task PlaylistAsync(int index)
        {
            if (_currentTrack is null)
            {
                await SendMessageAsync(MessageType.Error, "The queue is empty!").ConfigureAwait(false);
                return;
            }
            
            var playlist = new List<string>();
            
            string status;
            if (Player.IsConnected && !IsPaused)
                status = "▶";
            else if (Player.IsConnected && IsPaused)
                status = "⏸";
            else
                status = "⏹";

            if (!_currentTrack.Track.IsStream)
            {
                playlist.Add($"{status} {_currentTrack.Track.Title} {Format.Code($"({_currentTrack.Track.Length.GetTimeString()})")}\n" +
                             "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            }
            else
            {
                playlist.Add($"♾ {_currentTrack.Track.Title} {Format.Code("Livestream")}\n" +
                             "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            }
            var totalLength = _currentTrack.Track.IsStream ? TimeSpan.Zero : _currentTrack.Track.Length;
            
            for (var i = 0; i < _queue.Count; i++)
            {
                if (!_queue[i].Track.IsStream)
                {
                    playlist.Add($"#{i+1} {_queue[i].Track.Title} {Format.Code($"({_queue[i].Track.Length.GetTimeString()})")}");
                    totalLength = totalLength.Add(_queue[i].Track.Length);
                }
                else
                {
                    playlist.Add($"#{i+1} {_queue[i].Track.Title} {Format.Code("Livestream")}");
                }
            }

            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithTitle("Current queue");
            embed.WithDescription(string.Join("\n", playlist.Skip(index * 16).Take(16)));

            var totalPages = (playlist.Count % 16 == 0) ? playlist.Count / 16 : playlist.Count / 16 + 1;
            embed.WithFooter($"{index+1}/{totalPages} | Total length: {totalLength.GetTimeString()}");
            await SendMessageEmbedAsync(embed).ConfigureAwait(false);
        }
        
        public async Task SkipAsync()
        {
            if (_queue.Count > 0)
            {
                await UpdateQueueAsync(0);
            }
            else
            {
                await SendMessageAsync(MessageType.Error, "No next track in the queue!").ConfigureAwait(false);
            }
        }
        
        public async Task SkipToAsync(int index)
        {
            if (index >= 0 && index < _queue.Count)
            {
                await UpdateQueueAsync(index);
            }
            else
            {
                await SendMessageAsync(MessageType.Error, "The index is outside the queue's size").ConfigureAwait(false);
            }
        }
        
        public async Task SkipToAsync(string title)
        {
            var message = await SendMessageAsync(MessageType.Confirmation, "Searching the track, please wait!").ConfigureAwait(false);
            var index = _queue.FindIndex(x => x.Track.Title.Contains(title, StringComparison.InvariantCultureIgnoreCase));
            if (index > 0)
            {
                if (message != null)
                    await message.DeleteAsync().ConfigureAwait(false);
                await UpdateQueueAsync(index);
            }
            else
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                embed.WithDescription("I couldn't find the track!");
                if (message != null)
                    await message.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
            }
        }
        
        public async Task ReplayAsync()
        {
            if (_currentTrack != null)
            {
                await UpdateQueueAsync().ConfigureAwait(false);
            }
        }
        
        public async Task ClearAsync()
        {
            if (_currentTrack != null)
            {
                _queue.Clear();
                await SendMessageAsync(MessageType.Confirmation, "Queue cleared!").ConfigureAwait(false);
            }
        }
        
        public async Task ShuffleAsync()
        {
            if (_currentTrack != null)
            {
                _queue.Shuffle();
                await SendMessageAsync(MessageType.Confirmation, "Queue shuffled!").ConfigureAwait(false);
            }
        }
        
        public async Task RemoveAsync(int index)
        {
            if (index >= 0 && index < _queue.Count)
            {
                var song = _queue[index];
                _queue.RemoveAt(index);
                await SendMessageAsync(MessageType.Confirmation, $"{Format.Bold(song.Track.Title)} was removed from the queue").ConfigureAwait(false);
            }
            else
            {
                await SendMessageAsync(MessageType.Error, "The index is outside the queue's size").ConfigureAwait(false);
            }
        }
        
        public async Task RemoveAsync(string title)
        {
            var message = await SendMessageAsync(MessageType.Confirmation, "Searching the track, please wait!").ConfigureAwait(false);
            var song = _queue.Find(x => x.Track.Title.Contains(title, StringComparison.InvariantCultureIgnoreCase));
            if (song != null)
            {
                _queue.Remove(song);
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithDescription($"{Format.Bold(song.Track.Title)} was removed from the queue");
                if (message != null)
                    await message.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                embed.WithDescription("I couldn't find the track!");
                if (message != null)
                    await message.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
            }
        }
        
        public async Task LeaveAsync(IGuild guild, string message)
        {
            if (Timeout != null)
            {
                Timeout.Dispose();
                Timeout = null;
            }

            try
            {
                if (Player != null)
                {
                    await _service.LavaNode.LeaveAsync(guild.Id);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _service.RemoveMusicPlayer(guild);
                if (!string.IsNullOrEmpty(message))
                    await SendMessageAsync(MessageType.Confirmation, message).ConfigureAwait(false);
            }
        }
        public Task ShardDisconnected(IGuild guild)
        {
            if (Timeout != null)
            {
                Timeout.Dispose();
                Timeout = null;
            }

            // the LavaLink library takes care to disconnect all players from the shard
            
            _service.RemoveMusicPlayer(guild);
            return Task.CompletedTask;
        }

        private async Task<IUserMessage> SendMessageAsync(MessageType messageType, string message)
        {
            if (_guild != null && _channel != null)
            {
                var socketGuildUser = await _guild.GetCurrentUserAsync().ConfigureAwait(false);
                var preconditions = ((SocketGuildUser) socketGuildUser).GetPermissions((IGuildChannel) _channel);
                if (preconditions.ViewChannel && preconditions.SendMessages)
                {
                    switch (messageType)
                    {
                        case MessageType.Confirmation:
                            return await _channel.SendConfirmationMessageAsync(message).ConfigureAwait(false);
                        case MessageType.Error:
                            return await _channel.SendErrorMessageAsync(message).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }
        
        private async Task<IUserMessage> SendMessageEmbedAsync(EmbedBuilder embed)
        {
            if (_guild != null && _channel != null)
            {
                var socketGuildUser = await _guild.GetCurrentUserAsync().ConfigureAwait(false);
                var preconditions = ((SocketGuildUser)socketGuildUser).GetPermissions((IGuildChannel)_channel);
                if (preconditions.ViewChannel)
                {
                    if (preconditions.SendMessages)
                    {
                        return await _channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        private enum MessageType
        {
            Confirmation = 0,
            Error = 1
        }
    }
}