using Discord;
using Discord.Audio;
using Google.Apis.YouTube.v3;
using RiasBot.Extensions;
using RiasBot.Modules.Music.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RiasBot.Modules.Music.Common
{
    public class MusicPlayer
    {
        private readonly SongProcessing _sp;
        private readonly MusicService _ms;
        public MusicPlayer(MusicService ms)
        {
            _sp = new SongProcessing(this);
            _ms = ms;
        }

        private IAudioClient _audioClient;
        public IVoiceChannel VoiceChannel;
        public bool Paused => PauseTaskSource != null;
        private TaskCompletionSource<bool> PauseTaskSource { get; set; }
        //private event Action<MusicPlayer, bool> OnPauseChanged;

        private Process _p;
        private Stream _outStream;
        private AudioOutStream _audioStream;
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;
        public IMessageChannel Channel;
        private IGuild _guild;

        public readonly List<Song> Queue = new List<Song>();
        
        private float _volume = 1.0f;
        private bool _isRunning;
        private bool _repeat;     //repeat the current song
        private bool _isPaused;
        private bool _isConnected;
        public bool Wait;     //for not spamming
        public bool IsDownloading; //downloading the next song
        public bool RegisteringPlaylist;
        public bool Destroyed;
        
        private Stopwatch _timer;
        public Timer Timeout;
        public YouTubeService YouTubeService;

        public TimeSpan DurationLimit = new TimeSpan(2, 5, 0); // I'll make a little exception of 5 minutes

        public class Song
        {
            public string Title { get; set; }
            public string Id { get; set; }
            public string Url { get; set; }
            public string Channel { get; set; }
            public TimeSpan Duration { get; set; }
            public string Thumbnail { get; set; }
            public IGuildUser User { get; set; }
            public string DlUrl { get; set; }
        }

        public async Task JoinAudio(IGuild guild, IMessageChannel channel, IVoiceChannel target)
        {
            if (_audioClient != null)
            {
                return;
            }

            if (target.Guild.Id != guild.Id)
            {
                return;
            }

            if (!_isConnected)
            {
                VoiceChannel = target;
                _audioClient = await target.ConnectAsync().ConfigureAwait(false);
                Channel = channel;
                _guild = guild;
                await Channel.SendConfirmationEmbed($"Joining to {Format.Bold(target.Name)}!");
                _isConnected = true;
            }
        }

        public async Task Play(string title, string id, string url, string channel, TimeSpan duration, string thumbnail, IGuildUser user)
        {
            try
            {
                if (RegisteringPlaylist)
                {
                    await Channel.SendErrorEmbed("I still add the songs to the playlist. Please wait!");
                    return;
                }
                if (Wait)
                    return;

                Wait = true;
                var song = new Song
                {
                    Title = title,
                    Id = id,
                    Url = url,
                    Channel = channel,
                    Duration = duration,
                    Thumbnail = thumbnail,
                    User = user
                };

                if (duration <= DurationLimit)
                {
                    if (!_isRunning)
                    {
                        await TogglePause(false, false).ConfigureAwait(false);
                        Queue.Add(song);
                        if (_repeat)
                            await UpdateQueue(1).ConfigureAwait(false);
                        else
                            await UpdateQueue(0).ConfigureAwait(false);
                    }
                    else
                    {
                        var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                        var eta = Queue[0].Duration;
                        eta = eta.Subtract(_timer.Elapsed);

                        for (var i = 1; i < Queue.Count; i++)
                        {
                            eta = eta.Add(Queue[i].Duration);
                        }
                        var timeEta = GetTimeString(eta);

                        Queue.Add(song);
                        embed.WithAuthor("Added to queue", song.User.GetAvatarUrl() ?? song.User.DefaultAvatarUrl());
                        embed.WithDescription($"[{song.Title}]({song.Url})");
                        embed.AddField("Channel", song.Channel, true).AddField("Length", song.Duration, true);
                        embed.AddField("ETA", timeEta, true).AddField("Position", Queue.Count, true);
                        embed.WithThumbnailUrl(song.Thumbnail);

                        await Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                        IsDownloading = true;
                        await Task.Factory.StartNew(() => _sp.DownloadNextSong());
                        
                        Wait = false;
                    }
                }
                else
                {
                    await Channel.SendErrorEmbed($"I can't play songs longer than {DurationLimit.Hours} hours.");
                    Wait = false;
                }
            }
            catch
            {
                //ignored
            }
        }

        public async Task Playlist(string title, string id, string url, string channel, string thumbnail, IGuildUser user, int index)
        {
            var song = new Song()
            {
                Title = title,
                Id = id,
                Url = url,
                Channel = channel,
                Duration = TimeSpan.Zero,
                Thumbnail = thumbnail,
                User = user,
                DlUrl = null
            };
            if (!RegisteringPlaylist)
            {
                if (index > -1 && (index - 1) < Queue.Count)
                {
                    if (!_isRunning)
                    {
                        Queue.Insert(0, song);
                        await Task.Factory.StartNew(() => UpdateQueue(0));
                        RegisteringPlaylist = true;
                    }
                    else
                    {
                        Queue.Add(song);
                    }
                }
                else
                {
                    Queue.Add(song);
                }
            }
            else
            {
                Queue.Add(song);
            }
        }

        public async Task LoadSongsLength(string ids, int startPosition, int endPosition)
        {
            ids = ids.Remove(ids.Length - 1);
            var videoListRequest = YouTubeService.Videos.List("contentDetails");
            videoListRequest.Id = ids;
            var videoListResponse = await videoListRequest.ExecuteAsync().ConfigureAwait(false);

            var itemsToRemove = new List<int>();
            
            var index = 0;
            for (var i = startPosition; i < endPosition; i++)
            {
                if (Queue[i].Id.Equals(videoListResponse.Items[index].Id))
                {
                    Queue[i].Duration = System.Xml.XmlConvert.ToTimeSpan(videoListResponse.Items[index].ContentDetails.Duration);
                    index++;
                }
                else
                {
                    itemsToRemove.Add(i);
                }
            }
            foreach (var item in itemsToRemove)
                Queue.RemoveAt(item);
        }

        public async Task SkipTo(int index)
        {
            if (!Wait && !IsDownloading)
            {
                if (index > 0 && index < Queue.Count)
                {
                    await Channel.SendConfirmationEmbed("Searching the song... Please wait!").ConfigureAwait(false);
                    await TogglePause(false, false).ConfigureAwait(false);
                    Dispose();
                    _isRunning = false;
                    Wait = true;
                    
                    await UpdateQueue(index).ConfigureAwait(false);
                    await Task.Factory.StartNew(() => _sp.DownloadNextSong());
                }
                else
                {
                    await Channel.SendErrorEmbed("I couldn't find the song").ConfigureAwait(false);
                }
            }
        }
        
        public async Task SkipTo(string title)
        {
            if (!Wait && !IsDownloading)
            {
                await Channel.SendConfirmationEmbed("Searching the song... Please wait!").ConfigureAwait(false);
                var titles = Queue.Where(x => x.Title.Contains(title, StringComparison.InvariantCultureIgnoreCase)).ToList();
                if (!titles.Any())
                {
                    await Channel.SendErrorEmbed("I couldn't find the song");
                }
                else
                {
                    var index = Queue.FindIndex(x => x.Title.Equals(titles.FirstOrDefault()?.Title));
                    if (index > 0 && index < Queue.Count)
                    {
                        await TogglePause(false, false).ConfigureAwait(false);
                        Dispose();
                        _isRunning = false;
                        Wait = true;
                        await UpdateQueue(index).ConfigureAwait(false);
                        await Task.Factory.StartNew(() => _sp.DownloadNextSong());
                    }
                    else
                    {
                        await Channel.SendErrorEmbed("I couldn't find the song").ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task UpdateQueue(int index)
        {
            if (index < Queue.Count)
            {
                var song = Queue[index];
                if (index > 0)
                    Queue.RemoveRange(0, index);
                
                while (song.Duration > DurationLimit)
                {
                    await Channel.SendErrorEmbed($"I can't play songs longer than {DurationLimit} hours. Playing next song.").ConfigureAwait(false);
                    song = Queue[++index];
                }
                
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithTitle("Now Playing");
                embed.WithDescription($"[{song.Title}]({song.Url})");
                embed.AddField("Channel", song.Channel, true).AddField("Length", song.Duration, true);
                embed.AddField("Requested by", $"{song.User}", true);
                if (_repeat)
                    embed.AddField("Repeat", "Enabled", true);
                embed.WithThumbnailUrl(song.Thumbnail);

                if (!string.IsNullOrEmpty(song.DlUrl))
                    await Task.Factory.StartNew(() => PlayMusic(song.DlUrl, embed.Build())).ConfigureAwait(false);
                else
                {
                    var audioUrl = await _sp.GetAudioUrl(song.Url).ConfigureAwait(false);
                    await Task.Factory.StartNew(() => PlayMusic(audioUrl, embed.Build())).ConfigureAwait(false);
                    Queue[0].DlUrl = audioUrl;
                }
                await Task.Factory.StartNew(() => _sp.DownloadNextSong());
                Wait = false;
            }
        }

        private async Task PlayMusic(string path, Embed embed)
        {
            if (_isRunning) return;
            _isRunning = true;
            if (_audioStream != null)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _tokenSource = new CancellationTokenSource();
                _token = _tokenSource.Token;
            }
            else
            {
                _tokenSource = new CancellationTokenSource();
                _token = _tokenSource.Token;
                _audioStream = _audioClient.CreatePCMStream(AudioApplication.Music, bufferMillis: 1920);
            }

            if (_timer != null)
                _timer.Restart();
            else
            {
                _timer = new Stopwatch();
                _timer.Start();
            }
            var buffer = new byte[3840];

            if (!string.IsNullOrEmpty(path))
            {
                _p = _sp.CreateStream(path);
                _outStream = _p.StandardOutput.BaseStream;

                await Channel.SendMessageAsync(embed: embed).ConfigureAwait(false);
                if (!RegisteringPlaylist)
                    Wait = false;

                int bytesRead;
                while ((bytesRead = await _outStream.ReadAsync(buffer, 0, buffer.Length, _token)) > 0)
                {
                    AdjustVolume(buffer, _volume);
                    await _audioStream.WriteAsync(buffer, 0, bytesRead, _token).ConfigureAwait(false);
                    await (PauseTaskSource?.Task ?? Task.CompletedTask);
                }
                await _audioStream.FlushAsync(_token).ConfigureAwait(false);
                Dispose();
            }

            _timer.Stop();
            _isRunning = false;
            if (Queue.Count > 0)
                await UpdateQueue((_repeat) ? 0 : 1).ConfigureAwait(false);
                
        }

        public async Task NowPlaying()
        {
            if (_isRunning)
            {
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                string timerBar = null;
                var timerPos = (_timer.ElapsedMilliseconds / Queue[0].Duration.TotalMilliseconds) * 30;
                for (var i = 0; i < 30; i++)
                {
                    if (i == (int)timerPos)
                        timerBar += "⚫";
                    else
                    {
                        timerBar += "▬";
                    }
                }

                var song = Queue[0];

                string description = $"[{song.Title}]({song.Url})\n\n{Format.Code(timerBar)}\n" +
                                     $"{Format.Code($"{GetTimeString(_timer.Elapsed)}/{GetTimeString(song.Duration)}")}\n\n";
                if (_repeat)
                    description += $"{Format.Bold("Repeat:")} Enabled\n";
                description += $"{Format.Bold("Requested by:")} {song.User}";
                embed.WithTitle("Now Playing");
                embed.WithDescription(description);
                                     
                embed.WithThumbnailUrl(song.Thumbnail);

                await Channel.SendMessageAsync("", embed: embed.Build());
            }
            else
            {
                await Channel.SendErrorEmbed("No song is running!");
            }
        }

        public async Task Skip()
        {
            if (!IsDownloading)
            {
                if (Queue.Count > 1)
                {
                    await TogglePause(false, false).ConfigureAwait(false);
                    Dispose();
                    _isRunning = false;
                    Wait = true;

                    await UpdateQueue(1).ConfigureAwait(false);
                }
                else
                {
                    await Channel.SendErrorEmbed("No next song in the playlist!");
                }
            }
        }

        public async Task Replay()
        {
            if (!Wait)
            {
                Wait = true;

                await Channel.SendConfirmationEmbed("Replay current song!");
                await UpdateQueue(0).ConfigureAwait(false);
            }
        }

        public async Task Playlist(int index)
        {
            var playlist = new List<string>();
            for (var i = 0; i < Queue.Count; i++)
            {
                if (!_isRunning)
                {
                    playlist.Add(i == 0 ? $"⏹ {Queue[i].Title} {Format.Code($"({Queue[i].Duration})")}\n" +
                                          "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬" :
                        $"#{i} {Queue[i].Title} {Format.Code($"({Queue[i].Duration})")}");
                }
                else if (_isPaused)
                {
                    playlist.Add(i == 0 ? $"⏸ {Queue[i].Title} {Format.Code($"({Queue[i].Duration})")}\n" +
                                          "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬" :
                        $"#{i} {Queue[i].Title} {Format.Code($"({Queue[i].Duration})")}");
                }
                else
                {
                    playlist.Add(i == 0 ? $"▶ {Queue[i].Title} {Format.Code($"({Queue[i].Duration})")}\n" +
                                          "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬" :
                        $"#{i} {Queue[i].Title} {Format.Code($"({Queue[i].Duration})")}");
                }
            }
            
            var eta = Queue[0].Duration;
            eta = eta.Subtract(_timer.Elapsed);

            for (var i = 1; i < Queue.Count; i++)
            {
                eta = eta.Add(Queue[i].Duration);
            }
            var timeEta = GetTimeString(eta);

            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithTitle("Current playlist");
            embed.WithDescription(string.Join("\n", playlist.Skip(index * 16).Take(16)));

            var totalPages = (playlist.Count % 16 == 0) ? playlist.Count / 16 : playlist.Count / 16 + 1;
            
            embed.WithFooter($"Page {index + 1}/{totalPages} | Total length: {timeEta}");
            
            await Channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
        }

        public async Task Shuffle()
        {
            if (RegisteringPlaylist)
            {
                await Channel.SendErrorEmbed("I still add the songs to the playlist. Please wait!");
                return;
            }
            try
            {
                var song = Queue[0];
                Queue.Shuffle();
                var pos = Queue.IndexOf(song);
                var firstSong = Queue[0];
                Queue[0] = song;
                Queue[pos] = firstSong;

                if (Queue.Count == 1)
                    await Channel.SendErrorEmbed("The playlist has only one song!");
                else
                    await Channel.SendConfirmationEmbed($"{Queue.Count - 1} songs have been shuffled!");
            }
            catch
            {
                //ignored
            }
        }

        public async Task Clear()
        {
            if (RegisteringPlaylist)
            {
                await Channel.SendErrorEmbed("I still add the songs to the playlist. Please wait!");
                return;
            }
            try
            {
                Queue.RemoveRange(1, Queue.Count - 1);
                RegisteringPlaylist = false;

                await Channel.SendConfirmationEmbed("Playlist cleared");
            }
            catch
            {
                // Playlist already cleared or is not created yet
            }
        }

        public async Task Remove(int index)
        {
            var current = false;
            try
            {
                var song = Queue[index];
                if (index > 0)
                {
                    Queue.RemoveAt(index);
                }
                else
                {
                    current = true;
                }

                if (!current)
                    await Channel.SendConfirmationEmbed($"{Format.Bold(song.Title)} was removed from the playlist!").ConfigureAwait(false);
                else
                    await Channel.SendErrorEmbed($"{Format.Bold(song.Title)} couldn't be removed from the playlist because is running!");
            }
            catch
            {
                // Playlist already cleared or is not created yet
            }
        }

        public async Task Remove(string title)
        {
            var current = false;
            try
            {
                var msg = await Channel.SendConfirmationEmbed("Searching the song... Please wait!").ConfigureAwait(false);
                var titles = Queue.Where(x => x.Title.ToLowerInvariant().Contains(title.ToLowerInvariant())).ToList();
                await msg.DeleteAsync().ConfigureAwait(false);
                if (!titles.Any())
                {
                    await Channel.SendErrorEmbed("I couldn't find the song!");
                    return;
                }

                var song = Queue.Find(x => x.Title == titles.FirstOrDefault()?.Title);
                if (song.Title != Queue[0].Title)
                {
                    Queue.Remove(song);
                }
                else
                {
                    current = true;
                }

                if (!current)
                    await Channel.SendConfirmationEmbed($"{Format.Bold(song.Title)} was removed from the playlist!").ConfigureAwait(false);
                else
                    await Channel.SendErrorEmbed($"{Format.Bold(song.Title)} couldn't be removed from the playlist because is running!");
            }
            catch
            {
                // Playlist already cleared or is not created yet
            }
        }

        public async Task Destroy(string message, bool forced = false, bool noUsers = false)
        {
            if (!Wait || forced)
            {
                try
                {
                    _tokenSource.Cancel();
                    _tokenSource.Dispose();
                    _tokenSource = new CancellationTokenSource();
                    _token = _tokenSource.Token;
                }
                catch
                {
                    //ignored
                }
                Dispose();
                if (_audioClient != null)
                    if (_audioClient.ConnectionState == ConnectionState.Connected)
                    {
                        await _audioClient.StopAsync().ConfigureAwait(false);
                        _isConnected = false;
                    }
                try
                {
                    if (!forced)
                        await Channel.SendConfirmationEmbed(message).ConfigureAwait(false);
                    if (noUsers)
                        await Channel.SendConfirmationEmbed(message).ConfigureAwait(false);
                    Destroyed = true;
                }
                catch
                {
                    //ignored
                }
                _ms.RemoveMusicPlayer(_guild);
            }
        }

        public async Task SetVolume(int volume)
        {
            if (volume >= 0 && volume <= 100)
            {
                _volume = ((float)volume) / 100;
                await Channel.SendConfirmationEmbed($"Volume set to {volume}%");
            }
        }

        private unsafe void AdjustVolume(byte[] audioSamples, float volume)
        {
            if (Math.Abs(volume - 1f) < 0.0001f) return;

            // 16-bit precision for the multiplication
            var volumeFixed = (int)Math.Round(volume * 65536d);

            var count = audioSamples.Length >> 1;

            fixed (byte* srcBytes = audioSamples)
            {
                var src = (short*)srcBytes;

                for (var i = count; i != 0; i--, src++)
                    *src = (short)(((*src) * volumeFixed) >> 16);
            }
        }

        public async Task TogglePause(bool pause, bool message)
        {
            if (Wait)
                return;

            if (pause != _isPaused && _isRunning)
            {
                if (PauseTaskSource == null)
                {
                    PauseTaskSource = new TaskCompletionSource<bool>();
                    _timer.Stop();
                    _isPaused = pause;
                }
                else
                {
                    PauseTaskSource.TrySetResult(true);
                    PauseTaskSource = null;
                    _timer.Start();
                    _isPaused = pause;
                }

                if (_isPaused && message)
                    await Channel.SendConfirmationEmbed("Music playback paused!");
                else if (message)
                    await Channel.SendConfirmationEmbed("Music playback resumed!");

                //OnPauseChanged?.Invoke(this, PauseTaskSource != null);
            }
        }

        public async Task ToggleRepeat()
        {
            if (_repeat)
            {
                _repeat = false;
                await Channel.SendConfirmationEmbed("Repeating the current song disabled!");
            }
            else
            {
                _repeat = true;
                await Channel.SendConfirmationEmbed("Repeating the current song enabled!");
            }
        }

        private void Dispose()
        {
            try
            {
                _p.StandardOutput.Dispose();
            }
            catch
            {
                //ignored
            }
            try
            {
                if (!_p.HasExited)
                    _p.Kill();
            }
            catch
            {
                //ignored
            }
            _outStream?.Dispose();
        }

        private static string GetTimeString(TimeSpan timeSpan)
        {
            var hoursInt = timeSpan.Hours;
            var minutesInt = timeSpan.Minutes;
            var secondsInt = timeSpan.Seconds;

            var hours = hoursInt.ToString();
            var minutes = minutesInt.ToString();
            var seconds = secondsInt.ToString();

            if (hoursInt < 10)
                hours = "0" + hours;
            if (minutesInt < 10)
                minutes = "0" + minutes;
            if (secondsInt < 10)
                seconds = "0" + seconds;

            return hours + ":" + minutes + ":" + seconds;
        }
    }
}