using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RiasBot.Modules.Administration.Services;
using RiasBot.Modules.Music.Services;
using SharpLink;

namespace RiasBot.Services
{
    public class BotService : IRService
    {
        private readonly DiscordShardedClient _discord;
        private readonly DbService _db;
        private readonly MusicService _musicService;
        private readonly MuteService _muteService;
        private readonly IBotCredentials _creds;
        private readonly LoggingService _loggingService;

        private Timer _dblTimer;
        public Timer Status;

        public string[] Statuses;
        private int _statusCount = 0;

        private bool _allShardsDoneConnection;
        private int _shardsConnected = 0;
        private int _recommendedShardCount = 0;

        public BotService(DiscordShardedClient discord, DbService db, MusicService musicService, MuteService muteService, IBotCredentials creds, LoggingService loggingService)
        {
            _discord = discord;
            _db = db;
            _musicService = musicService;
            _muteService = muteService;
            _creds = creds;
            _loggingService = loggingService;

            _discord.UserJoined += UserJoined;
            _discord.UserLeft += UserLeft;
            _discord.ShardReady += ShardReady;
            _discord.GuildUnavailable += GuildUnavailable;
            
            _discord.UserVoiceStateUpdated += _musicService.UpdateVoiceState;

            if (!string.IsNullOrEmpty(_creds.DiscordBotsListApiKey))
            {
                if(!_creds.IsBeta)
                {
                    _dblTimer = new Timer(async _ => await DblStats(), null, new TimeSpan(0, 0, 30), new TimeSpan(0, 0, 30));
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

        private async Task GuildUnavailable(SocketGuild guild)
        {
            if (_musicService.MPlayer.TryGetValue(guild.Id, out var musicPlayer))
            {
                await musicPlayer.Leave(guild, null);
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
                    await _discord.SetActivityAsync(new Game(statusName, ActivityType.Playing)).ConfigureAwait(false);
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

        private async Task ShardReady(DiscordSocketClient client)
        {
            _loggingService.Ready = true;
            if (!_allShardsDoneConnection)
                _shardsConnected++;
            
            if (_recommendedShardCount == 0)
                _recommendedShardCount = await _discord.GetRecommendedShardCountAsync().ConfigureAwait(false);

            if (_shardsConnected == _recommendedShardCount && !_allShardsDoneConnection)
            {
                await _discord.GetGuild(RiasBot.SupportServer).DownloadUsersAsync();

                RiasBot.Lavalink = new LavalinkManager(_discord, new LavalinkManagerConfig
                {
                    RESTHost = _creds.LavalinkConfig.RestHost,
                    RESTPort = _creds.LavalinkConfig.RestPort,
                    WebSocketHost = _creds.LavalinkConfig.WebSocketHost,
                    WebSocketPort = _creds.LavalinkConfig.WebSocketPort,
                    Authorization = _creds.LavalinkConfig.Authorization,
                    TotalShards = _recommendedShardCount
                });
                await RiasBot.Lavalink.StartAsync().ConfigureAwait(false);
                RiasBot.Lavalink.TrackEnd += _musicService.TrackEnd;
                Console.WriteLine($"{DateTime.UtcNow:MMM dd hh:mm:ss} Lavalink started!");
                _allShardsDoneConnection = true;
            }
        }
    }
}
