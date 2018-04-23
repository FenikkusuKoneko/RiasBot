using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Modules.Music.MusicServices;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RiasBot.Services
{
    public class BotService : IRService
    {
        private readonly DiscordShardedClient _discord;
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        private readonly MusicService _musicService;

        private Timer dblTimer;
        private Stopwatch voteTimer;
        public Timer status;

        public string[] statuses;
        private int statusCount = 0;

        public BotService(DiscordShardedClient discord, DbService db, IBotCredentials creds, MusicService musicService)
        {
            _discord = discord;
            _db = db;
            _creds = creds;
            _musicService = musicService;

            _discord.UserJoined += UserJoined;
            _discord.UserLeft += UserLeft;
            _discord.ShardDisconnected += Disconnected;
            _discord.UserVoiceStateUpdated += _musicService.CheckIfAlone;

            if(!RiasBot.isBeta && !String.IsNullOrEmpty(_creds.DiscordBotsListApiKey))
            {
                dblTimer = new Timer(new TimerCallback(async _ => await DblStats()), null, new TimeSpan(0, 0, 30), new TimeSpan(0, 0, 30));
                voteTimer = new Stopwatch();
                voteTimer.Start();
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(g => g.GuildId == user.Guild.Id).FirstOrDefault();
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
                        catch { }
                    }
                }
            }
        }

        private async Task UserLeft(SocketGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(g => g.GuildId == user.Guild.Id).FirstOrDefault();
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
                }
            }
        }

        public async Task Disconnected(Exception ex, DiscordSocketClient client)
        {
            foreach (var guild in client.Guilds)
            {
                var mp = _musicService.GetMusicPlayer(guild);
                await mp.Destroy("", true).ConfigureAwait(false);
            }
        }

        public async Task StatusRotate()
        {
            string sts = statuses[statusCount];
            sts = sts.Trim();
            var type = sts.Substring(0, sts.IndexOf(" "));
            var statusName = sts.Remove(0, sts.IndexOf(" ") + 1);
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
            if (statusCount > statuses.Length - 2)
                statusCount = 0;
            else
                statusCount++;
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
                                    { "server_count", _discord.Guilds.Count().ToString() }
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
    }
}
