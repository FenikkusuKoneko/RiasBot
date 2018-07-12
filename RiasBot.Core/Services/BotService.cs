using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        private Timer dblTimer;
        private Timer dblVotesTimer;
        public Timer status;

        public List<Votes> votesList = new List<Votes>();
        private bool populateVotesList = true;

        public string[] statuses;
        private int statusCount = 0;

        public BotService(DiscordShardedClient discord, DbService db, IBotCredentials creds)
        {
            _discord = discord;
            _db = db;
            _creds = creds;

            _discord.UserJoined += UserJoined;
            _discord.UserLeft += UserLeft;

            if(!RiasBot.isBeta && !String.IsNullOrEmpty(_creds.DiscordBotsListApiKey))
            {
                dblTimer = new Timer(new TimerCallback(async _ => await DblStats()), null, new TimeSpan(0, 0, 30), new TimeSpan(0, 0, 30));
                dblVotesTimer = new Timer(new TimerCallback(async _ => await DblVotes()), null, TimeSpan.Zero, new TimeSpan(1, 0, 0));
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(g => g.GuildId == user.Guild.Id).FirstOrDefault();
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
                        catch { }
                    }

                    if (userGuildDb != null)
                    {
                        if (userGuildDb.IsMuted)
                        {
                            var role = user.Guild.GetRole(guildDb.MuteRole);
                            if (role != null)
                            {
                                await user.AddRoleAsync(role).ConfigureAwait(false);
                                await user.ModifyAsync(x => x.Mute = true).ConfigureAwait(false);
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

        public async Task AddAssignableRole(IGuild guild, IGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(g => g.GuildId == guild.Id).FirstOrDefault();
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

        public async Task DblVotes()
        {
            try
            {
                using (var db = _db.GetDbContext())
                using (var http = new HttpClient())
                {
                    string votesApi = await http.GetStringAsync(RiasBot.website + "api/votes.json");
                    var dblVotes = JsonConvert.DeserializeObject<DBL>(votesApi);
                    var votes = dblVotes.data.votes.Where(x => x.type == "upvote");
                    votesList = new List<Votes>();
                    foreach (var vote in votes)
                    {
                        if (DateTime.TryParseExact(vote.date, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.CurrentCulture, DateTimeStyles.None, out var date))
                        {
                            date = date.AddDays(1);
                            if (DateTime.Compare(date.ToUniversalTime(), DateTime.UtcNow) >= 1)
                            {
                                votesList.Add(vote);
                                if (!populateVotesList)
                                {
                                    var getGuildUser = _discord.GetGuild(RiasBot.supportServer).GetUser(vote.user);
                                    if (getGuildUser != null)
                                    {
                                        var userDb = db.Users.Where(x => x.UserId == vote.user).FirstOrDefault();
                                        if (userDb != null)
                                        {
                                            if (!userDb.IsBlacklisted)
                                                userDb.Currency += 10;
                                        }
                                        else
                                        {
                                            var currency = new UserConfig { UserId = vote.user, Currency = 10 };
                                        }
                                        await db.SaveChangesAsync().ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        //User not in the support server!
                                    }
                                }
                            }
                        }
                    }
                    populateVotesList = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    public class DBL
    {
        public Data data { get; set; }
    }
    public class Data
    {
        public Votes[] votes { get; set; }
        public string date { get; set; }
    }
    public class Votes
    {
        public ulong bot { get; set; }
        public ulong user { get; set; }
        public string type { get; set; }
        public string query { get; set; }
        public string date { get; set; }
    }
}
