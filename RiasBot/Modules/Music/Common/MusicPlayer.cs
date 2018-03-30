﻿using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using RiasBot.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RiasBot.Modules.Music.Common
{
    public class MusicPlayer
    {
        public DiscordSocketClient _client;
        public SongProcessing _sp;
        public MusicPlayer(DiscordSocketClient client)
        {
            _client = client;
            _sp = new SongProcessing(this);
        }

        public IAudioClient audioClient;
        public bool Paused => pauseTaskSource != null;
        public TaskCompletionSource<bool> pauseTaskSource { get; set; } = null;
        public event Action<MusicPlayer, bool> OnPauseChanged;

        public Object locker = new Object();
        public SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public Process p;
        public Stream _outStream;
        public AudioOutStream audioStream;
        public CancellationTokenSource tokenSource;
        public CancellationToken token;
        public IMessageChannel _channel;

        public List<Song> Queue = new List<Song>();

        public int position;
        public float volume = 1.0f;
        public bool isRunning;
        public bool waited;     //for not spamming
        public bool repeat;     //repeat the current song
        public Stopwatch timer;

        public struct Song
        {
            public string title;
            public string url;
            public string channel;
            public TimeSpan duration;
            public string thumbnail;
            public IGuildUser user;
            public string dlUrl;
        }

        public async Task JoinAudio(IGuild guild, IMessageChannel channel, IVoiceChannel target)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                if (audioClient != null)
                {
                    return;
                }

                if (target.Guild.Id != guild.Id)
                {
                    return;
                }

                audioClient = await target.ConnectAsync().ConfigureAwait(false);
                _channel = channel;
                await _channel.SendConfirmationEmbed($"Joining to {Format.Bold(target.Name)}!");
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task Play(string title, string url, string channel, TimeSpan duration, string thumbnail, IGuildUser user)
        {
            try
            {
                if (waited)
                    return;

                waited = true;
                Song song = new Song
                {
                    title = title,
                    url = url,
                    channel = channel,
                    duration = duration,
                    thumbnail = thumbnail,
                    user = user
                };

                if (duration <= new TimeSpan(2, 0, 0))
                {
                    if (!isRunning)
                    {
                        Queue.Add(song);
                        await UpdateQueue(position).ConfigureAwait(false);
                        isRunning = true;
                    }
                    else
                    {
                        if (Queue.Count < 50)
                        {
                            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                            lock (locker)
                            {
                                var eta = Queue[position].duration;
                                eta = eta.Subtract(timer.Elapsed);

                                for (int i = position + 1; i < Queue.Count; i++)
                                {
                                    var songQueue = Queue[i];
                                    eta = eta.Add(songQueue.duration);
                                }
                                var timeETA = GetTimeString(eta);

                                Queue.Add(song);
                                embed.WithAuthor("Added to queue", song.user.GetAvatarUrl() ?? song.user.DefaultAvatarUrl());
                                embed.WithDescription($"[{song.title}]({song.url})");
                                embed.AddField("Channel", song.channel, true).AddField("Length", song.duration, true);
                                embed.AddField("ETA", timeETA, true).AddField("Position", Queue.Count, true);
                                embed.WithThumbnailUrl(song.thumbnail);
                            }

                            await _channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                            await Task.Factory.StartNew(() => _sp.DownloadNextSong());
                            if (!isRunning)
                            {
                                await UpdateQueue(position).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await _channel.SendErrorEmbed("The current playlist has 50 songs. Clear the playlist if you want to add more.").ConfigureAwait(false);
                        }
                        waited = false;
                    }
                }
                else
                {
                    await _channel.SendErrorEmbed("I can't play songs longer than 2 hours!");
                    waited = false;
                }
            }
            catch
            {

            }
        }

        public async Task Playlist(IGuildUser user, YouTubeService youtubeService, VideosResource.ListRequest videoListRequest, PlaylistItem playlistItem, int index)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                videoListRequest.Id = playlistItem.Snippet.ResourceId.VideoId;
                var videoListResponse = await videoListRequest.ExecuteAsync().ConfigureAwait(false);

                var videoListRequestSnippet = youtubeService.Videos.List("snippet");
                videoListRequestSnippet.Id = playlistItem.Snippet.ResourceId.VideoId;
                var videoListResponseSnippet = await videoListRequestSnippet.ExecuteAsync().ConfigureAwait(false);

                lock (locker)
                {
                    string title = playlistItem.Snippet.Title;
                    string url = "https://youtu.be/" + playlistItem.Snippet.ResourceId.VideoId;
                    var channel = videoListResponseSnippet.Items.FirstOrDefault().Snippet.ChannelTitle;
                    TimeSpan duration = System.Xml.XmlConvert.ToTimeSpan(videoListResponse.Items.FirstOrDefault().ContentDetails.Duration);
                    string thumbnail = playlistItem.Snippet.Thumbnails.High.Url;

                    if (title is null || url is null || duration == new TimeSpan(0, 0, 0) || thumbnail is null)
                        return;

                    Song song = new Song()
                    {
                        title = title,
                        url = url,
                        channel = channel,
                        duration = duration,
                        thumbnail = thumbnail,
                        user = user,
                        dlUrl = null
                    };
                    isRunning = true;
                    Queue.Add(song);
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task PlayByIndex(int index)
        {
            if (!waited)
            {
                Dispose();
                waited = true;
                await UpdateQueue(index).ConfigureAwait(false);
                position = index;
                await Task.Factory.StartNew(() => _sp.DownloadNextSong());
            }
        }

        public async Task UpdateQueue(int index)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                var song = Queue[index];

                while (song.duration > new TimeSpan(2, 0, 0))
                {
                    await _channel.SendConfirmationEmbed("I can't play songs longer than 2 hours. Playing next song!").ConfigureAwait(false);
                    song = Queue[++index];
                }

                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle("Now Playing");
                embed.WithDescription($"[{song.title}]({song.url})");
                embed.AddField("Channel", song.channel, true).AddField("Length", song.duration, true);
                embed.AddField("Requested by", $"{song.user}");
                embed.WithThumbnailUrl(song.thumbnail);

                if (!String.IsNullOrEmpty(song.dlUrl))
                    await Task.Factory.StartNew(() => PlayMusic(song.dlUrl, index, embed.Build())).ConfigureAwait(false);
                else
                {
                    var audioURL = await _sp.GetAudioURL(song.url).ConfigureAwait(false);
                    await Task.Factory.StartNew(() => PlayMusic(audioURL, index, embed.Build())).ConfigureAwait(false);
                    song.dlUrl = audioURL;
                    Queue[index] = song;
                }
                await Task.Factory.StartNew(() => _sp.DownloadNextSong());
            }
            catch
            {
                isRunning = false;
                waited = false;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task PlayMusic(string path, int index, Embed embed)
        {
            try
            {
                if (audioStream != null)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    tokenSource = new CancellationTokenSource();
                    token = tokenSource.Token;
                }
                else
                {
                    tokenSource = new CancellationTokenSource();
                    token = tokenSource.Token;
                    audioStream = audioClient.CreatePCMStream(AudioApplication.Music, bufferMillis: 1920);
                }
                if (timer != null)
                    timer.Restart();
                else
                {
                    timer = new Stopwatch();
                    timer.Start();
                }

                byte[] buffer = new byte[3840];
                int bytesRead = 0;

                if (!String.IsNullOrEmpty(path))
                {
                    p = _sp.CreateStream(path);
                    _outStream = p.StandardOutput.BaseStream;

                    await _channel.SendMessageAsync("", embed: embed).ConfigureAwait(false);
                    waited = false;

                    while ((bytesRead = _outStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        AdjustVolume(buffer, volume);
                        await audioStream.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);

                        await (pauseTaskSource?.Task ?? Task.CompletedTask);
                    }
                }
                else
                {
                    waited = false;
                }
                await audioStream.FlushAsync(token).ConfigureAwait(false);

                timer.Stop();
                Dispose();

                index += (repeat) ? 0 : 1;
                position = index;

                await UpdateQueue(index).ConfigureAwait(false);
            }
            catch
            {

            }
        }

        public async Task NowPlaying()
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                lock (locker)
                {
                    string timerBar = null;
                    double timerPos = (timer.ElapsedMilliseconds / Queue[position].duration.TotalMilliseconds) * 30;
                    for (int i = 0; i < 30; i++)
                    {
                        if (i == (int)timerPos)
                            timerBar += "⚫";
                        else
                        {
                            timerBar += "▬";
                        }
                    }

                    var song = Queue[position];

                    embed.WithTitle("Now Playing");
                    embed.WithDescription($"[{song.title}]({song.url})\n\n{Format.Code(timerBar)}\n" +
                        $"{Format.Code($"{GetTimeString(timer.Elapsed)}/{GetTimeString(song.duration)}")}\n\n" +
                        $"{Format.Bold("Requested by:")} {song.user}");
                    embed.WithThumbnailUrl(song.thumbnail);
                }

                await _channel.SendMessageAsync("", embed: embed.Build());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task Skip()
        {
            if (!waited)
            {
                if (position + 1 < Queue.Count)
                {
                    lock (locker)
                    {
                        Dispose();
                        waited = true;
                    }
                    await _channel.SendConfirmationEmbed("Skipping current song!");
                    await UpdateQueue(++position).ConfigureAwait(false);
                }
                else
                {
                    await _channel.SendErrorEmbed("No next song in the playlist!");
                }
            }
        }

        public async Task Replay()
        {
            if (!waited)
            {
                lock (locker)
                {
                    Dispose();
                    waited = true;
                }
                await _channel.SendConfirmationEmbed("Replay current song!");
                await UpdateQueue(position).ConfigureAwait(false);
            }
        }

        public async Task Playlist()
        {
            string[] playlist = new string[Queue.Count];
            lock (locker)
            {
                for (int i = 0; i < Queue.Count; i++)
                {
                    if (position == i)
                        playlist[i] = $"➡ #{i + 1} {Queue[i].title}";
                    else
                        playlist[i] = $"#{i + 1} {Queue[i].title}";
                }
            }
            await _channel.SendPaginated(_client, "Current playlist", playlist, 10);
        }

        public async Task Shuffle()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                lock(locker)
                {
                    Song song = Queue[position];
                    Queue.Shuffle();
                    var pos = Queue.IndexOf(song);
                    position = pos;
                }

                if (Queue.Count == 1)
                    await _channel.SendConfirmationEmbed("The playlist has only one song!");
                else
                    await _channel.SendConfirmationEmbed($"{Queue.Count} songs have been shuffled!");
            }
            catch
            {

            }
            semaphoreSlim.Release();
        }

        public async Task Clear()
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                lock(locker)
                {
                    Queue.Clear();
                    position = 0;
                    isRunning = false;

                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    tokenSource = new CancellationTokenSource();
                    token = tokenSource.Token;

                    Dispose();
                }
                await _channel.SendConfirmationEmbed("Current playlist cleared!").ConfigureAwait(false);
            }
            catch
            {
                // Playlist already cleared or is not created yet
            }
            semaphoreSlim.Release();
        }

        public async Task Remove(int index)
        {
            await semaphoreSlim.WaitAsync();
            bool current = false;
            try
            {
                var song = Queue[index];
                lock (locker)
                {
                    if (index != position)
                    {
                        if (index < position)
                        {
                            Queue.Remove(song);
                            position--;
                        }
                        else
                        {
                            Queue.Remove(song);
                        }
                    }
                    else
                    {
                        current = true;
                    }
                }
                if (!current)
                    await _channel.SendConfirmationEmbed($"{Format.Bold(song.title)} was removed from the playlist!").ConfigureAwait(false);
                else
                    await _channel.SendErrorEmbed($"{Format.Bold(song.title)} couldn't be removed from the playlist because is running!");
            }
            catch
            {
                // Playlist already cleared or is not created yet
            }
            semaphoreSlim.Release();
        }

        public async Task Remove(string title)
        {
            await semaphoreSlim.WaitAsync();
            bool current = false;
            try
            {
                var msg = await _channel.SendConfirmationEmbed("Searching the song... Please wait!").ConfigureAwait(false);
                var titles = Queue.Where(x => x.title.ToLowerInvariant().Contains(title.ToLowerInvariant()));
                await msg.DeleteAsync().ConfigureAwait(false);
                if (titles.Count() <= 0)
                {
                    await _channel.SendErrorEmbed($"I couldn't find the song!");
                    semaphoreSlim.Release();
                    return;
                }

                var song = Queue.Find(x => x.title == titles.FirstOrDefault().title);
                var index = Queue.IndexOf(song);
                lock (locker)
                {
                    if (song.title != Queue[position].title)
                    {
                        if (index < position)
                        {
                            Queue.Remove(song);
                            position--;
                        }
                        else
                        {
                            Queue.Remove(song);
                        }
                    }
                    else
                    {
                        current = true;
                    }
                }
                if (!current)
                    await _channel.SendConfirmationEmbed($"{Format.Bold(song.title)} was removed from the playlist!").ConfigureAwait(false);
                else
                    await _channel.SendErrorEmbed($"{Format.Bold(song.title)} couldn't be removed from the playlist because is running!");
            }
            catch
            {
                // Playlist already cleared or is not created yet
            }
            semaphoreSlim.Release();
        }

        public async Task Destroy(string message)
        {
            try
            {
                lock(locker)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                    tokenSource = new CancellationTokenSource();
                    token = tokenSource.Token;

                    Dispose();
                }
                await audioClient.StopAsync().ConfigureAwait(false);
                await _channel.SendConfirmationEmbed(message).ConfigureAwait(false);
            }
            catch { }
        }

        public async Task SetVolume(int volume)
        {
            if (volume >= 0 && volume <= 100)
            {
                lock(locker)
                {
                    this.volume = ((float)volume) / 100;
                }
                await _channel.SendConfirmationEmbed($"Volume set {volume}%");
            }
        }

        private unsafe static byte[] AdjustVolume(byte[] audioSamples, float volume)
        {
            if (Math.Abs(volume - 1f) < 0.0001f) return audioSamples;

            // 16-bit precision for the multiplication
            int volumeFixed = (int)Math.Round(volume * 65536d);

            int count = audioSamples.Length >> 1;

            fixed (byte* srcBytes = audioSamples)
            {
                short* src = (short*)srcBytes;

                for (int i = count; i != 0; i--, src++)
                    *src = (short)(((*src) * volumeFixed) >> 16);
            }

            return audioSamples;
        }

        public void Unpause()
        {
            lock(locker)
            {
                if (pauseTaskSource != null)
                {
                    pauseTaskSource.TrySetResult(true);
                    pauseTaskSource = null;
                }
            }
        }

        public async Task TogglePause()
        {
            bool pause;
            lock(locker)
            {
                if (pauseTaskSource == null)
                {
                    pauseTaskSource = new TaskCompletionSource<bool>();
                    pause = true;
                }
                else
                {
                    Unpause();
                    pause = false;
                }
            }
            if (pause)
                await _channel.SendConfirmationEmbed("Music playback paused!");
            else
                await _channel.SendConfirmationEmbed("Music playback resumed!");

            OnPauseChanged?.Invoke(this, pauseTaskSource != null);
        }

        public async Task ToggleRepeat()
        {
            if (repeat)
            {
                repeat = false;
                await _channel.SendConfirmationEmbed("Repeating the current song disabled!");
            }
            else
            {
                repeat = true;
                await _channel.SendConfirmationEmbed("Repeating the current song enabled!");
            }
        }

        private void Dispose()
        {
            GC.Collect();
            try
            {
                p.StandardOutput.Dispose();
            }
            catch
            {
            }
            try
            {
                if (!p.HasExited)
                    p.Kill();
            }
            catch
            {
            }
            try
            {
                _outStream.Dispose();
                p.Dispose();
            }
            catch
            {

            }
        }

        public Task<bool> CheckMusicChannel(IMessageChannel channel) => Task<bool>.Factory.StartNew(() =>
        {
            return _channel == channel;
        });

        public static string GetTimeString(TimeSpan timeSpan)
        {
            var hoursInt = timeSpan.Hours;
            var minutesInt = timeSpan.Minutes;
            var secondsInt = timeSpan.Seconds;

            string hours = hoursInt.ToString();
            string minutes = minutesInt.ToString();
            string seconds = secondsInt.ToString();

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
