using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RiasBot.Modules.Administration.Services;
using RiasBot.Modules.Music.Services;
using Victoria;

namespace RiasBot.Services
{
    public class BotService : IRService
    {
        private readonly DiscordShardedClient _discord;
        private readonly DbService _db;
        private readonly MuteService _muteService;
        private readonly IBotCredentials _creds;
        private readonly LoggingService _loggingService;
        private readonly MusicService _musicService;
        private readonly Lavalink _lavalink;

        public Timer Status;

        public string[] Statuses;
        private int _statusCount;
        
        private bool _allShardsDoneConnection;
        private int _shardsConnected;
        private int _recommendedShardCount;

        public BotService(DiscordShardedClient discord, DbService db, MuteService muteService, 
            IBotCredentials creds, LoggingService loggingService, MusicService musicService, Lavalink lavalink)
        {
            _discord = discord;
            _db = db;
            _muteService = muteService;
            _creds = creds;
            _loggingService = loggingService;
            _musicService = musicService;
            _lavalink = lavalink;

            _discord.UserJoined += UserJoined;
            _discord.UserLeft += UserLeft;
            _discord.ChannelDestroyed += ChannelDestroyed;
            _discord.ShardConnected += ShardConnected;
            _discord.ShardDisconnected += ShardDisconnected;
            _discord.UserVoiceStateUpdated += _musicService.UpdateVoiceState;
            

            if (!string.IsNullOrEmpty(_creds.DiscordBotsListApiKey))
            {
                if(!_creds.IsBeta)
                {
                    var unused = new Timer(async _ => await DblStats(), null, new TimeSpan(0, 0, 30), new TimeSpan(0, 0, 30));
                }
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.FirstOrDefault(g => g.GuildId == user.Guild.Id);
                var userGuildDb = db.UserGuilds.Where(x => x.GuildId == user.Guild.Id).FirstOrDefault(x => x.UserId == user.Id);
                if (guildDb != null)
                {
                    if (guildDb.Greet)
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(guildDb.GreetMessage))
                            {
                                var greetMsg = guildDb.GreetMessage;
                                var channel = _discord.GetGuild(user.Guild.Id).GetTextChannel(guildDb.GreetChannel);
                                greetMsg = greetMsg.Replace("%user%", user.Mention);
                                greetMsg = greetMsg.Replace("%guild%", Format.Bold(user.Guild.Name));
                                greetMsg = greetMsg.Replace("%server%", Format.Bold(user.Guild.Name));

                                var embed = Extensions.Extensions.EmbedFromJson(greetMsg);
                                if (embed is null)
                                    await channel.SendMessageAsync(greetMsg).ConfigureAwait(false);
                                else
                                    await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                            //channel deleted
                        }
                    }

                    if (guildDb.AutoAssignableRole > 0)
                    {
                        try
                        {
                            var aar = _discord.GetGuild(user.Guild.Id).GetRole(guildDb.AutoAssignableRole);
                            await user.AddRoleAsync(aar).ConfigureAwait(false);
                        }
                        catch
                        {
                            //ignored
                        }
                    }
                    if (userGuildDb != null)
                    {
                        if (userGuildDb.IsMuted)
                        {
                            var role = user.Guild.GetRole(guildDb.MuteRole) ?? (user.Guild.Roles.FirstOrDefault(x => x.Name == "rias-mute"));
                            if (role != null)
                            {
                                await user.AddRoleAsync(role).ConfigureAwait(false);
                                await user.ModifyAsync(x => x.Mute = true).ConfigureAwait(false);
                            }
                            else
                            {
                                userGuildDb.IsMuted = false;
                                await db.SaveChangesAsync().ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }

        private async Task UserLeft(SocketGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.FirstOrDefault(g => g.GuildId == user.Guild.Id);
                var userGuildDb = db.UserGuilds.Where(x => x.GuildId == user.Guild.Id).FirstOrDefault(x => x.UserId == user.Id);
                if (guildDb != null)
                {
                    if (guildDb.Bye)
                    {
                        try
                        {
                            if (!String.IsNullOrEmpty(guildDb.ByeMessage))
                            {
                                var byeMsg = guildDb.ByeMessage;
                                var channel = _discord.GetGuild(user.Guild.Id).GetTextChannel(guildDb.ByeChannel);
                                byeMsg = byeMsg.Replace("%user%", user.ToString());
                                byeMsg = byeMsg.Replace("%guild%", Format.Bold(user.Guild.Name));
                                byeMsg = byeMsg.Replace("%server%", Format.Bold(user.Guild.Name));

                                var embed = Extensions.Extensions.EmbedFromJson(byeMsg);
                                if (embed is null)
                                    await channel.SendMessageAsync(byeMsg).ConfigureAwait(false);
                                else
                                    await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                            }
                        }
                        catch
                        {
                            //channel deleted
                        }
                    }
                    if (userGuildDb != null)
                    {
                        if (userGuildDb.IsMuted)
                        {
                            var role = user.Guild.GetRole(guildDb.MuteRole) ?? (user.Guild.Roles.FirstOrDefault(x => x.Name == "rias-mute"));
                            if (role != null)
                            {
                                if (user.Roles.All(r => r.Id != role.Id))
                                {
                                    userGuildDb.IsMuted = false;
                                    await _muteService.RemoveMuteTimer(user.Guild, user);
                                }
                            }
                            else
                            {
                                userGuildDb.IsMuted = false;
                                await _muteService.RemoveMuteTimer(user.Guild, user);
                            }
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        public async Task AddAssignableRole(IGuild guild, IGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.FirstOrDefault(g => g.GuildId == guild.Id);
                if (guildDb != null)
                {
                    if (guildDb.AutoAssignableRole > 0)
                    {
                        var aar = guild.GetRole(guildDb.AutoAssignableRole);
                        if (aar != null)
                        {
                            if (user.RoleIds.All(x => x != aar.Id) && user.RoleIds.All(x => x == user.Guild.EveryoneRole.Id))
                                await user.AddRoleAsync(aar).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        public async Task StatusRotate()
        {
            var sts = Statuses[_statusCount];
            sts = sts.Trim();
            var type = sts.Substring(0, sts.IndexOf(" ", StringComparison.Ordinal)).Trim().ToLowerInvariant();
            var statusName = sts.Remove(0, sts.IndexOf(" ", StringComparison.Ordinal)).Trim();
            statusName = statusName.Replace("%guilds%", _discord.Guilds.Count.ToString());

            if (statusName.Contains("%users%"))
            {
                var users = _discord.Guilds.Sum(guild => guild.MemberCount);
                statusName = statusName.Replace("%users%", users.ToString());
            }
            switch (type)
            {
                case "playing":
                    await _discord.SetActivityAsync(new Game(statusName)).ConfigureAwait(false);
                    break;
                case "listening":
                    await _discord.SetActivityAsync(new Game(statusName, ActivityType.Listening)).ConfigureAwait(false);
                    break;
                case "watching":
                    await _discord.SetActivityAsync(new Game(statusName, ActivityType.Watching)).ConfigureAwait(false);
                    break;
            }

            if (_statusCount > Statuses.Length - 2)
                _statusCount = 0;
            else
                _statusCount++;
        }

        public async Task DblStats()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_creds.DiscordBotsListApiKey))
                    return;
                using (var http = new HttpClient())
                {
                    using (var content = new FormUrlEncodedContent(
                        new Dictionary<string, string> {
                                    { "shard_count",  _discord.Shards.Count.ToString()},
                                    //{ "shard_id", _discord.ShardId.ToString() },
                                    { "server_count", _discord.Guilds.Count.ToString() }
                        }))
                    {
                        content.Headers.Clear();
                        content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        http.DefaultRequestHeaders.Add("Authorization", _creds.DiscordBotsListApiKey);

                        await http.PostAsync($"https://discordbots.org/api/bots/{_discord.CurrentUser.Id}/stats", content).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private Task ChannelDestroyed(SocketChannel channel)
        {
            if (channel is SocketGuildChannel guildChannel)
            {
                var mpPlayer = _musicService.GetMusicPlayer(guildChannel.Guild);
                if (mpPlayer != null)
                {
                    mpPlayer.Channel = null;
                }
            }

            return Task.CompletedTask;
        }

        private async Task ShardConnected(DiscordSocketClient client)
        {
            _loggingService.Ready = true;
            if (!_allShardsDoneConnection)
                _shardsConnected++;
            
            if (_recommendedShardCount == 0)
                _recommendedShardCount = await _discord.GetRecommendedShardCountAsync().ConfigureAwait(false);

            if (_shardsConnected == _recommendedShardCount && !_allShardsDoneConnection)
            {
                await _discord.GetGuild(RiasBot.SupportServer).DownloadUsersAsync().ConfigureAwait(false);

                var lavaNode = await _lavalink.AddNodeAsync(_discord, new Configuration
                {
                    ReconnectAttempts = 10,
                    ReconnectInterval = TimeSpan.FromSeconds(3.0),
                    Host = _creds.LavalinkConfig.Host,
                    Port = _creds.LavalinkConfig.Port,
                    Authorization = _creds.LavalinkConfig.Authorization,
                    Severity = LogSeverity.Info,
                    NodePrefix = "LavaNode_",
                    BufferSize = 1024
                });
                
                _musicService.InitializeLavaNode(lavaNode);
                Console.WriteLine($"{DateTime.UtcNow:MMM dd hh:mm:ss} Lavalink started!");
                _allShardsDoneConnection = true;
            }
        }
        
        private async Task ShardDisconnected(Exception exception, DiscordSocketClient client)
        {
            foreach (var guild in client.Guilds)
            {
                if (_musicService.MPlayer.TryGetValue(guild.Id, out var musicPlayer))
                {
                    //remove the music player from MusicService
                    await musicPlayer.ShardDisconnected(guild);
                }
            }
        }
    }
}
