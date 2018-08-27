using Discord;
using RiasBot.Extensions;
using RiasBot.Modules.Music.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using SharpLink;

namespace RiasBot.Modules.Music.Common
{
    public class MusicPlayer
    {
        private readonly MusicService _service;
        public MusicPlayer(MusicService service)
        {
            _service = service;
        }

        public IGuild Guild;
        public IMessageChannel Channel;
        public IVoiceChannel VoiceChannel;

        private LavalinkPlayer _player;
        public Song CurrentTrack;
        private readonly List<Song> _queue = new List<Song>();

        private readonly Stopwatch _elapsedTime = new Stopwatch();
        private TimeSpan _offsetElapsedTime = TimeSpan.Zero;

        public Timer Timeout;

        //Patreon features
        //For self-hosters they will be unlocked
        public bool UnlockVolume;
        public bool UnlockLongTracks;
        public bool UnlockLivestreams;

        private bool _isPaused;
        public bool RegisteringPlaylist;
        public bool Repeat;

        public class Song
        {
            public LavalinkTrack Track { get; set; }
            public IGuildUser User { get; set; }
            public string Source { get; set; }
            public string Thumbnail { get; set; }
            public string TrackId { get; set; }
        }
        
        public async Task Join(IGuild guild, IGuildUser user, IMessageChannel channel, IVoiceChannel voiceChannel, bool check = true)
        {
            if (_player is null)
            {
                _player = await RiasBot.Lavalink.JoinAsync(voiceChannel);
                Guild = guild;
                Channel = channel;
                VoiceChannel = voiceChannel;
                await SendMessage(MessageType.Confirmation, $"Connected to {Format.Bold(voiceChannel.ToString())}!").ConfigureAwait(false);
            }
            else
            {
                if (check)
                    await SendMessage(MessageType.Error, "I'm already connected to a voice channel!").ConfigureAwait(false);
            }
        }

        public async Task Play(IGuild guild, IGuildUser user, IMessageChannel channel, IVoiceChannel voiceChannel,
            string source, LavalinkTrack track)
        {
            await Join(guild, user, channel, voiceChannel, false);
            
            if (_queue.Count > 5000)    //5000 tracks are enough, you don't keep the music player online for years
            {
                await SendMessage(MessageType.Error, "The queue is too heavy, please remove some tracks or clear it!").ConfigureAwait(false);
                return;
            }

            if (!UnlockLongTracks)
            {
                if (track.IsSeekable)
                {
                    if (TimeSpan.Compare(track.Length, new TimeSpan(3, 5, 0)) > 0)    //a little exception of 5 minutes
                    {
                        await SendMessage(MessageType.Error, "I cannot play tracks over 3 hours!").ConfigureAwait(false);
                        return;
                    }
                }
            }

            if (!UnlockLivestreams)
            {
                if (track.IsStream)
                {
                    await SendMessage(MessageType.Error, "I cannot play livestreams!").ConfigureAwait(false);
                    return;
                }
            }
            
            var song = new Song
            {
                Track = track,
                User = user,
                Source = source,
                TrackId = _service.GetYouTubeTrackId(track.Url)
            };

            if (source.Equals("youtube"))
            {
                song.Thumbnail = $"http://i3.ytimg.com/vi/{_service.GetYouTubeTrackId(track.Url)}/maxresdefault.jpg";
            }
            else if (source.Equals("soundcloud"))
            {
                song.Thumbnail = await _service.GetTrackThumbnail(track.Url);
            }

            if (_queue.Count > 0)
            {
                await AddToQueue(song, user);
            }
            else
            {
                if (_player.Playing)
                {
                    await AddToQueue(song, user);
                }
                else
                {
                    CurrentTrack = song;
                    await UpdateQueue();
                }
            }
        }

        public async Task AddPlaylist(IGuild guild, IGuildUser user, IMessageChannel channel, IVoiceChannel voiceChannel,
            string source, LoadTracksResponse tracks)
        {
            await Join(guild, user, channel, voiceChannel, false);
            
            if (_queue.Count > 10000)    //10000 tracks are enough, you don't keep the music player online for years
            {
                await channel.SendErrorEmbed("The queue is too heavy, please remove some tracks or clear it!").ConfigureAwait(false);
                return;
            }

            if (Channel is null)
                Channel = channel;
            
            var message = await SendMessage(MessageType.Confirmation, "Adding tracks to queue, please wait!").ConfigureAwait(false);
            RegisteringPlaylist = true;
            
            var count = 0;
            if (tracks.Tracks.Any())
            {
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
                        TrackId = _service.GetYouTubeTrackId(track.Url)
                    };

                    if (source.Equals("youtube"))
                    {
                        song.Thumbnail = $"http://i3.ytimg.com/vi/{_service.GetYouTubeTrackId(track.Url)}/maxresdefault.jpg";
                    }
                    _queue.Add(song);
                    count++;
                }

                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithDescription($"Added to queue {count} tracks");
                if (message != null)
                    await message.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
                if (tracks.PlaylistInfo?.SelectedTrack != null)
                {
                    if (tracks.PlaylistInfo.SelectedTrack > 0)
                    {
                        var track = _queue[tracks.PlaylistInfo.SelectedTrack];
                        _queue.RemoveAt(tracks.PlaylistInfo.SelectedTrack);
                        _queue.Insert(0, track);
                    }
                }
                
                if (CurrentTrack is null)
                {
                    if (_player.VoiceChannel != null)
                        await UpdateQueue(0).ConfigureAwait(false);
                }
            }
            else
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                embed.WithDescription("I couldn't load any track, check if the playlist link is available!");
                if (message != null)
                    await message.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
            }
            RegisteringPlaylist = false;
        }

        public async Task UpdateQueue(int index = -1)
        {
            if (index < _queue.Count)
            {
                if (index >= 0)
                {
                    CurrentTrack = _queue[index];
                    _queue.RemoveRange(0, index + 1);
                }
                
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithTitle("Now Playing");
                embed.WithDescription($"[{CurrentTrack.Track.Title}]({CurrentTrack.Track.Url})");
                embed.AddField("Channel", CurrentTrack.Track.Author, true);
                embed.AddField("Length", CurrentTrack.Track.IsStream ?
                    "Livestream" : CurrentTrack.Track.Length.GetTimeString() , true);
                embed.AddField("Requested by", $"{CurrentTrack.User}", true);
                if (Repeat)
                    embed.AddField("Repeat", "Enabled", true);
                embed.WithThumbnailUrl(CurrentTrack.Thumbnail);
                
                await _player.PlayAsync(CurrentTrack.Track);
                _elapsedTime.Restart();
                _offsetElapsedTime = TimeSpan.Zero;
                await SendMessageEmbed(embed).ConfigureAwait(false);
            }
        }

        public async Task Pause(string message)
        {
            if (!_player.Playing)
                return;
            
            await _player.PauseAsync().ConfigureAwait(false);
            _isPaused = true;
            _elapsedTime.Stop();
            if (!string.IsNullOrEmpty(message))
                await SendMessage(MessageType.Confirmation, message).ConfigureAwait(false);
        }
        
        public async Task Resume(string message)
        {
            if (_player.Playing)
                return;
            
            await _player.ResumeAsync().ConfigureAwait(false);
            _isPaused = false;
            _elapsedTime.Start();
            if (!string.IsNullOrEmpty(message))
                await SendMessage(MessageType.Confirmation, message).ConfigureAwait(false);
        }

        public async Task NowPlaying()
        {
            if (_player.Playing)
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithTitle("Now Playing");
                var description = $"[{CurrentTrack.Track.Title}]({CurrentTrack.Track.Url})\n\n";
                
                var elapsedTime = _offsetElapsedTime.Add(_elapsedTime.Elapsed);
                if (!CurrentTrack.Track.IsStream)
                {
                    var currentTrackLength = CurrentTrack.Track.Length;
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
                
                embed.AddField("Requested by", CurrentTrack.User, true);
                if (Repeat)
                    embed.AddField("Repeat", "Enabled", true);
                embed.WithDescription(description);
                                     
                if (CurrentTrack.Source.Equals("youtube"))
                    embed.WithThumbnailUrl(CurrentTrack.Thumbnail);

                await SendMessageEmbed(embed).ConfigureAwait(false);
            }
            else
            {
                await SendMessage(MessageType.Error, "No track is running!").ConfigureAwait(false);
            }
        }

        public async Task SetVolume(string volume)
        {
            if (UnlockVolume)
            {
                volume = volume.Replace("%", "");

                if (uint.TryParse(volume, out var vol))
                {
                    if (vol <= 100)
                    {
                        await _player.SetVolumeAsync(vol).ConfigureAwait(false);
                        await SendMessage(MessageType.Confirmation, $"Volume set to {volume}%").ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await SendMessage(MessageType.Error, "The volume is a Patreon feature!").ConfigureAwait(false);
            }
        }

        public async Task Seek(string inputTime, IGuildUser user)
        {
            var time = Extensions.Extensions.ConvertToTimeSpan(inputTime);
            if (CurrentTrack.Track.IsSeekable)
            {
                if (TimeSpan.Compare(time, CurrentTrack.Track.Length) <= 0)
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithAuthor("Seek", user.RealAvatarUrl());
                    embed.WithDescription($"[{CurrentTrack.Track.Title}]({CurrentTrack.Track.Url})");
                    
                    await _player.SeekAsync((int)time.TotalMilliseconds);

                    embed.AddField("From", _offsetElapsedTime.Add(_elapsedTime.Elapsed).GetTimeString(), true)
                        .AddField("To", time.GetTimeString(), true);

                    await SendMessageEmbed(embed).ConfigureAwait(false);
                    
                    _offsetElapsedTime = time;
                    _elapsedTime.Restart();
                }
                else
                {
                    await SendMessage(MessageType.Error, "The time is over the track's length!").ConfigureAwait(false);
                }
            }
            else
            {
                await SendMessage(MessageType.Error, "The current track is not seekable!").ConfigureAwait(false);
            }
        }
        
        public async Task Playlist(int index)
        {
            if (CurrentTrack is null)
            {
                return;
            }
            
            var playlist = new List<string>();
            
            string status;
            if (_player.Playing)
                status = "▶";
            else if (_isPaused)
                status = "⏸";
            else
                status = "⏹";

            if (!CurrentTrack.Track.IsStream)
            {
                playlist.Add($"{status} {CurrentTrack.Track.Title} {Format.Code($"({CurrentTrack.Track.Length.GetTimeString()})")}\n" +
                             "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            }
            else
            {
                playlist.Add($"♾ {CurrentTrack.Track.Title} {Format.Code("Livestream")}\n" +
                             "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬");
            }
            var totalLength = TimeSpan.Zero;
            
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
            await SendMessageEmbed(embed).ConfigureAwait(false);
        }

        public async Task Skip()
        {
            if (_queue.Count > 0)
            {
                await UpdateQueue(0);
            }
            else
            {
                await SendMessage(MessageType.Error, "No next track in the queue!").ConfigureAwait(false);
            }
        }
        
        public async Task SkipTo(int index)
        {
            if (index >= 0 && index < _queue.Count)
            {
                await UpdateQueue(index);
            }
            else
            {
                await SendMessage(MessageType.Error, "The index is outside the queue's size").ConfigureAwait(false);
            }
        }
        
        public async Task SkipTo(string title)
        {
            var message = await SendMessage(MessageType.Confirmation, "Searching the track, please wait!").ConfigureAwait(false);
            var index = _queue.FindIndex(x => x.Track.Title.Contains(title, StringComparison.InvariantCultureIgnoreCase));
            if (index > 0)
            {
                if (message != null)
                    await message.DeleteAsync().ConfigureAwait(false);
                await UpdateQueue(index);
            }
            else
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                embed.WithDescription("I couldn't find the track!");
                if (message != null)
                    await message.ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
            }
        }

        public async Task Replay()
        {
            if (CurrentTrack != null)
            {
                await UpdateQueue().ConfigureAwait(false);
            }
        }
        
        public async Task Clear()
        {
            if (CurrentTrack != null)
            {
                _queue.Clear();
                await SendMessage(MessageType.Confirmation, "Queue cleared!").ConfigureAwait(false);
            }
        }
        
        public async Task Shuffle()
        {
            if (CurrentTrack != null)
            {
                _queue.Shuffle();
                await SendMessage(MessageType.Confirmation, "Queue shuffled!").ConfigureAwait(false);
            }
        }
        
        public async Task Remove(int index)
        {
            if (index >= 0 && index < _queue.Count)
            {
                var song = _queue[index];
                _queue.RemoveAt(index);
                await SendMessage(MessageType.Confirmation, $"{Format.Bold(song.Track.Title)} was removed from the queue").ConfigureAwait(false);
            }
            else
            {
                await SendMessage(MessageType.Error, "The index is outside the queue's size").ConfigureAwait(false);
            }
        }
        
        public async Task Remove(string title)
        {
            var message = await SendMessage(MessageType.Confirmation, "Searching the track, please wait!").ConfigureAwait(false);
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

        public async Task Leave(IGuild guild, string message)
        {
            if (Timeout != null)
            {
                Timeout.Dispose();
                Timeout = null;
            }
            
            await _player.StopAsync();
            await _player.DisconnectAsync();
            _service.RemoveMusicPlayer(guild);
            if (!string.IsNullOrEmpty(message))
                await SendMessage(MessageType.Confirmation, message).ConfigureAwait(false);
        }

        private async Task AddToQueue(Song song, IGuildUser user)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithAuthor(user);
            embed.WithTitle("Added to queue");
            embed.WithDescription($"[{song.Track.Title}]({song.Track.Url})");
            embed.AddField("Channel", song.Track.Author, true);

            embed.AddField("Length", song.Track.IsStream ? "Livestream" : song.Track.Length.GetTimeString(), true);

            var eta = TimeSpan.Zero;
            var endless = false;
            if (!CurrentTrack.Track.IsStream)
            {
                eta = CurrentTrack.Track.Length.Subtract(_offsetElapsedTime.Add(_elapsedTime.Elapsed));
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
            await SendMessageEmbed(embed).ConfigureAwait(false);
        }

        private async Task<IUserMessage> SendMessage(MessageType messageType, string message)
        {
            if (Guild != null && Channel != null)
            {
                var socketGuildUser = await Guild.GetCurrentUserAsync().ConfigureAwait(false);
                var preconditions = ((SocketGuildUser)socketGuildUser).GetPermissions((IGuildChannel)Channel);
                if (preconditions.ViewChannel)
                {
                    if (preconditions.SendMessages)
                    {
                        switch (messageType)
                        {
                            case MessageType.Confirmation:
                                return await Channel.SendConfirmationEmbed(message).ConfigureAwait(false);
                            case MessageType.Error:
                                return await Channel.SendConfirmationEmbed(message).ConfigureAwait(false);
                        }
                    }
                }
            }

            return null;
        }
        
        private async Task<IUserMessage> SendMessageEmbed(EmbedBuilder embed)
        {
            if (Guild != null && Channel != null)
            {
                var socketGuildUser = await Guild.GetCurrentUserAsync().ConfigureAwait(false);
                var preconditions = ((SocketGuildUser)socketGuildUser).GetPermissions((IGuildChannel)Channel);
                if (preconditions.ViewChannel)
                {
                    if (preconditions.SendMessages)
                    {
                        return await Channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
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