using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Extensions;
using RiasBot.Modules.Music.Commons;
using RiasBot.Services;
using Victoria;
using Victoria.Objects;
using Victoria.Objects.Enums;

namespace RiasBot.Modules.Music.Services
{
    public class MusicService : IRService
    {
        private readonly DbService _db;
        private readonly IBotCredentials _creds;
        private readonly InteractiveService _is;

        public MusicService(DbService db, IBotCredentials creds, InteractiveService iss)
        {
            _db = db;
            _creds = creds;
            _is = iss;
        }
        
        public readonly ConcurrentDictionary<ulong, MusicPlayer> MPlayer = new ConcurrentDictionary<ulong, MusicPlayer>();
        public LavaNode LavaNode;
        
        public async Task UpdateVoiceState(SocketUser user, SocketVoiceState stateOld, SocketVoiceState stateNew)
        {
            var wasMoved = false;
            
            var userG = (SocketGuildUser)user;
            var mp = GetMusicPlayer(userG.Guild);
            if (user.IsBot)
                if (user.Id == userG.Guild.CurrentUser.Id)
                {
                    if (mp != null)
                    {
                        if (mp.VoiceChannel.Id != stateNew.VoiceChannel.Id)
                        {
                            mp.VoiceChannel = stateNew.VoiceChannel;
                            wasMoved = true;
                        }
                    }
                }
                else
                {
                    return;
                }

            if (mp != null)
            {
                if (stateNew.VoiceChannel != null)
                {
                    if (stateNew.VoiceChannel.Id == mp.VoiceChannel.Id)
                    {
                        if (stateNew.VoiceChannel.Users.Contains(((SocketGuildUser) user).Guild.CurrentUser) && stateNew.VoiceChannel.Users.Count <= 2)
                        {
                            if (mp.Timeout != null)
                            {
                                await mp.ResumeAsync("The music player has been resumed!");
                                mp.Timeout.Dispose();
                                mp.Timeout = null;
                            }
                        }
                    }
                }
            }
                
                if (stateOld.VoiceChannel == null)
                    return;
                if (!stateOld.VoiceChannel.Users.Contains(((SocketGuildUser)user).Guild.CurrentUser) && !wasMoved)
                    return;
                if (stateOld.VoiceChannel == stateNew.VoiceChannel)
                    return;

            var users = wasMoved ? stateNew.VoiceChannel.Users.Count(u => !u.IsBot) : stateOld.VoiceChannel.Users.Count(u => !u.IsBot);
                
            if (users < 1)
            {
                if (mp != null)
                {
                    await mp.PauseAsync("All users left the voice channel! The music player has been paused and I will leave in two minutes " +
                                   "if you don't join back!");
                    mp.Timeout = new Timer(async _ => await mp.LeaveAsync(userG.Guild, $"I left {Format.Bold(mp.VoiceChannel.ToString())} due to inactivity!"), null,
                        TimeSpan.FromMinutes(2), TimeSpan.Zero);
                }
            } 
        }

        public void InitializeLavaNode(LavaNode node)
        {
            LavaNode = node;
            LavaNode.Finished += OnFinished;
        }

        private async Task<MusicPlayer> GetOrCreateMusicPlayerAsync(IGuild guild)
        {
            var mp = MPlayer.GetOrAdd(guild.Id, new MusicPlayer(this));
            UnlockFeatures(mp, await guild.GetOwnerAsync());
            return mp;
        }

        public MusicPlayer GetMusicPlayer(IGuild guild)
        {
            MPlayer.TryGetValue(guild.Id, out var mp);
            return mp;
        }

        public MusicPlayer RemoveMusicPlayer(IGuild guild)
        {
            return MPlayer.TryRemove(guild.Id, out var mp) ? mp : null;
        }

        public async Task SearchTrackAsync(ShardedCommandContext context, IGuild guild, IMessageChannel channel,
            IVoiceChannel voiceChannel, IGuildUser user, string keywords)
        {
            var mp = await GetOrCreateMusicPlayerAsync(guild);
            if (Uri.IsWellFormedUriString(keywords, UriKind.Absolute))
            {
                if (keywords.Contains("youtube") || keywords.Contains("youtu.be"))
                {
                    var youtubeTrackInfo = GetYouTubeTrackInfo(keywords);
                    if (youtubeTrackInfo.VideoId.Equals("playlist"))
                    {
                        await AddYouTubePlaylistAsync(mp, guild, channel, voiceChannel, user, keywords).ConfigureAwait(false);
                    }
                    else
                    {
                        await AddYouTubeTrackAsync(mp, guild, channel, voiceChannel, user, youtubeTrackInfo).ConfigureAwait(false);
                    }
                }
                else if (keywords.Contains("soundcloud"))
                {
                    await AddSoundcloudTrackAsync(mp, guild, channel, voiceChannel, user, keywords).ConfigureAwait(false);
                }
                else
                {
                    await channel.SendErrorMessageAsync("The URL is not valid!").ConfigureAwait(false);
                }
            }
            else
            {
                await SearchTrackOnYouTubeAsync(context, mp, guild, channel, user, voiceChannel, keywords).ConfigureAwait(false);
            }
        }

        private async Task AddYouTubePlaylistAsync(MusicPlayer mp, IGuild guild, IMessageChannel channel,
            IVoiceChannel voiceChannel, IGuildUser user, string keywords)
        {
            if (mp.RegisteringPlaylist)
                return;    //don't let people to spam playlists

            LavaResult tracks;
            try
            {
                tracks = await LavaNode.GetTracksAsync(new Uri(keywords)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await channel.SendErrorMessageAsync("Something went wrong when trying to get the tracks. If this still continue, please report in the " +
                                                    $"[Support Server]({RiasBot.CreatorServer})!").ConfigureAwait(false);
                await mp.LeaveAsync(guild, null).ConfigureAwait(false);
                Console.WriteLine(e);
                return;
            }
            if (tracks.Tracks != null)
            {
                if (tracks.Tracks.Any())
                {
                    await mp.AddPlaylistAsync(guild, user, channel, voiceChannel, "youtube", tracks).ConfigureAwait(false);
                }
                else
                {
                    await channel.SendErrorMessageAsync("The URL is not valid! Or check if the playlist is available").ConfigureAwait(false);
                }
            }
            else
            {
                await channel.SendErrorMessageAsync("The URL is not valid! Or check if the playlist is available").ConfigureAwait(false);
            }
        }

        private async Task AddYouTubeTrackAsync(MusicPlayer mp, IGuild guild, IMessageChannel channel,
            IVoiceChannel voiceChannel, IGuildUser user, YouTubeTrackInfo youtubeTrackInfo)
        {
            if (mp.RegisteringPlaylist)
                return;    //don't let people to spam playlists
            
            var url = $"https://youtu.be/{youtubeTrackInfo.VideoId}?list={youtubeTrackInfo.PlaylistId}";
            LavaResult tracks;
            try
            {
                tracks = await LavaNode.GetTracksAsync(new Uri(url)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await channel.SendErrorMessageAsync("Something went wrong when trying to get the tracks. If this still continue, please report in the " +
                                                    $"[Support Server]({RiasBot.CreatorServer})!").ConfigureAwait(false);
                await mp.LeaveAsync(guild, null).ConfigureAwait(false);
                Console.WriteLine(e);
                return;
            }
            if (tracks.Tracks != null)
            {
                if (tracks.Tracks.Any())
                {
                    if (!string.IsNullOrEmpty(youtubeTrackInfo.PlaylistId))
                    {
                        await mp.AddPlaylistAsync(guild, user, channel, voiceChannel, "youtube", tracks).ConfigureAwait(false);
                    }
                    else
                    {
                        await mp.PlayAsync(guild, user, channel, voiceChannel, "youtube", tracks.Tracks.FirstOrDefault()).ConfigureAwait(false);
                    }
                }
                else
                {
                    await channel.SendErrorMessageAsync("The URL is not valid! Or check if the track is available").ConfigureAwait(false);
                }
            }
            else
            {
                await channel.SendErrorMessageAsync("The URL is not valid! Or check if the track is available").ConfigureAwait(false);
            }
        }

        private async Task AddSoundcloudTrackAsync(MusicPlayer mp, IGuild guild, IMessageChannel channel,
            IVoiceChannel voiceChannel, IGuildUser user, string keywords)
        {
            LavaResult tracks;
            try
            {
                tracks = await LavaNode.GetTracksAsync(new Uri(keywords)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await channel.SendErrorMessageAsync("Something went wrong when trying to get the tracks. If this still continue, please report in the " +
                                                    $"[Support Server]({RiasBot.CreatorServer})!").ConfigureAwait(false);
                await mp.LeaveAsync(guild, null).ConfigureAwait(false);
                Console.WriteLine(e);
                return;
            }
            if (tracks.Tracks != null)
            {
                if (tracks.Tracks.Any())
                {
                    await mp.PlayAsync(guild, user, channel, voiceChannel, "soundcloud", tracks.Tracks.FirstOrDefault()).ConfigureAwait(false);
                }
                else
                {
                    await channel.SendErrorMessageAsync("The URL is not valid! Or check if the track is available").ConfigureAwait(false);
                }
            }
            else
            {
                await channel.SendErrorMessageAsync("The URL is not valid! Or check if the track is available").ConfigureAwait(false);
            }
        }
        
        private async Task SearchTrackOnYouTubeAsync(ShardedCommandContext context, MusicPlayer mp, IGuild guild, IMessageChannel channel,
            IGuildUser user, IVoiceChannel voiceChannel, string keywords)
        {
            LavaResult tracks;
            try
            {
                tracks = await LavaNode.SearchYouTubeAsync(keywords);
            }
            catch (Exception e)
            {
                await channel.SendErrorMessageAsync("Something went wrong when trying to get the tracks. If this still continue, please report in the " +
                                       $"[Support Server]({RiasBot.CreatorServer})!").ConfigureAwait(false);
                await mp.LeaveAsync(guild, null).ConfigureAwait(false);
                Console.WriteLine(e);
                return;
            }

            var description = "";
            var index = 1;
            foreach (var track in tracks.Tracks)
            {
                if (index < 6)
                {
                    description += $"#{index} {track.Title} `{track.Length.GetTimeString()}`\n";
                    index++;
                }
                else
                {
                    break;
                }
            }
            
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithTitle("Choose a song by typing the index. You have 1 minute");
            embed.WithDescription(description);
            var choose = await channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
            string userInput = null;
            var getUserInput = await _is.NextMessageAsync(context, timeout: TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            if (getUserInput != null)
                userInput = getUserInput.Content.Replace("#", "");
            if (int.TryParse(userInput, out var input))
            {
                input--;
                if (input >= 0 && input < tracks.Tracks.Count())
                {
                    var track = tracks.Tracks.ElementAt(input);
                    await mp.PlayAsync(guild, user, channel, voiceChannel, "youtube", track).ConfigureAwait(false);
                }
            }
            
            await choose.DeleteAsync().ConfigureAwait(false);
        }
        
        private YouTubeTrackInfo GetYouTubeTrackInfo(string url)
        {
            var regex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]*)(?:.*list=|(?:.*/)?)([a-zA-Z0-9-_]*)");
            var match = regex.Match(url);
            if (match.Groups.Count > 0)
            {
                return new YouTubeTrackInfo
                {
                    VideoId = match.Groups[1].Value,
                    PlaylistId = match.Groups[2].Value
                    
                };
            }

            return null;
        }
        
        public string GetYouTubeTrackId(Uri uri)
        {
            var regex = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]*)");
            var match = regex.Match(uri.ToString());
            return match.Groups.Count > 0 ? match.Groups[1].Value : null;
        }
        
        public async Task<string> GetTrackThumbnail(Uri uri)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    {
                        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
                        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
                        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
                        request.Headers.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

                        using (var response = await client.SendAsync(request).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();
                            using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            using (var decompressedStream = new GZipStream(responseStream, CompressionMode.Decompress))
                            using (var streamReader = new StreamReader(decompressedStream))
                            {
                                var html = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                                var startIndex = html.IndexOf("og:image", StringComparison.InvariantCulture);
                                html = html.Substring(startIndex);
                                startIndex = html.IndexOf("https", StringComparison.InvariantCulture);
                                var lastIndex = html.IndexOf("\">", StringComparison.InvariantCulture);
                                var thumbnail = html.Substring(startIndex, lastIndex - startIndex);
                                return thumbnail;
                            }
                        }
                    }
                }
                
            }
            catch
            {
                return null;
            }
        }
        
        private class YouTubeTrackInfo
        {
            public string VideoId { get; set; }
            public string PlaylistId { get; set; }
        }
        
        private void UnlockFeatures(MusicPlayer player, IGuildUser user)
        {
            if (!string.IsNullOrEmpty(_creds.PatreonAccessToken) && user.Id != RiasBot.KonekoId)
            {
                using (var db = _db.GetDbContext())
                {
                    var isBanned = false;    //if an user is banned then it doesn't matter the pledge
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
                    if (userDb != null)
                        isBanned = userDb.IsBanned;

                    var patreonDb = db.Patreon.FirstOrDefault(x => x.UserId == user.Id);
                    if (!isBanned)
                    {
                        if (patreonDb != null)
                        {
                            player.UnlockVolume = patreonDb.Reward >= 5000;
                            player.UnlockLongTracks = patreonDb.Reward >= 10000;
                            player.UnlockLivestreams = patreonDb.Reward >= 15000;
                        }
                    }
                }
            }
            else
            {
                player.UnlockVolume = true;
                player.UnlockLongTracks = true;
                player.UnlockLivestreams = true;
            }
        }
        
        private async Task OnFinished(LavaPlayer player, LavaTrack track, TrackReason reason)
        {
            if (reason == TrackReason.Finished)
            {
                var mp = GetMusicPlayer(player.VoiceChannel.Guild);
                if (mp != null)
                {
                    await mp.UpdateQueueAsync(mp.Repeat ? -1 : 0).ConfigureAwait(false);
                }
            }
        }
    }
}