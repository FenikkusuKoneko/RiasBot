using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotsList.Api;
using RiasBot.Modules.Music.MusicServices;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RiasBot.Services
{
    public class BotService : IRService
    {
        private readonly DiscordSocketClient _discord;
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        private readonly MusicService _musicService;

        private Timer dblTimer;
        private Stopwatch voteTimer;
        public Timer status;

        public string[] statuses;
        private int statusCount = 0;

        public BotService(DiscordSocketClient discord, DbService db, IBotCredentials creds, MusicService musicService)
        {
            _discord = discord;
            _db = db;
            _creds = creds;
            _musicService = musicService;

            _discord.UserJoined += UserJoined;
            _discord.UserLeft += UserLeft;
            _discord.UserVoiceStateUpdated += _musicService.CheckIfAlone;

            if(!RiasBot.isBeta && !String.IsNullOrEmpty(_creds.DiscordBotsListApiKey))
            {
                dblTimer = new Timer(new TimerCallback(async _ => await DblStats()), null, TimeSpan.Zero, new TimeSpan(0, 0, 30));
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
                AuthDiscordBotListApi dblApi = new AuthDiscordBotListApi(_creds.ClientId, _creds.DiscordBotsListApiKey);
                var dblSelfBot = await dblApi.GetMeAsync().ConfigureAwait(false);
                await dblSelfBot.UpdateStatsAsync(_discord.Guilds.Count).ConfigureAwait(false);

                if (TimeSpan.Compare(voteTimer.Elapsed, new TimeSpan(1, 0, 0)) >= 0)
                {
                    voteTimer.Restart();
                    using (var db = _db.GetDbContext())
                    {
                        var voters = (await dblSelfBot.GetVotersAsync(1).ConfigureAwait(false)).GroupBy(x => x.Id).Select(y => y.FirstOrDefault()).ToList();
                        foreach (var voter in voters)
                        {
                            try
                            {
                                var userGuild = _discord.GetGuild(RiasBot.supportServer).GetUser(voter.Id);
                                if (userGuild != null)
                                {
                                    try
                                    {
                                        var userDb = db.Users.Where(x => x.UserId == voter.Id).FirstOrDefault();
                                        userDb.Currency += 10;
                                    }
                                    catch
                                    {
                                        var dblVote = new UserConfig { UserId = voter.Id, Currency = 10 };
                                        await db.AddAsync(dblVote).ConfigureAwait(false);
                                    }
                                }
                                
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            
                        }
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
