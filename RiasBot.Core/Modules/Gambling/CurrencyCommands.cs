using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using RiasBot.Extensions;

namespace RiasBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class CurrencyCommands : RiasSubmodule
        {
            private readonly DbService _db;

            private readonly string _dblVote;

            public CurrencyCommands(DbService db)
            {
                _db = db;
                
                _dblVote = $"https://discordbots.org/bot/{RiasBot.BotId}/vote";
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            [Priority(2)]
            public async Task Award(int amount, [Remainder]IUser user)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
                    if (userDb != null)
                    {
                        userDb.Currency += amount;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        var currency = new UserConfig { UserId = user.Id, Currency = amount };
                        await db.Users.AddAsync(currency).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                await Context.Channel.SendConfirmationMessageAsync($"{Format.Bold(user.ToString())} has been awarded with {amount} {RiasBot.Currency}");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            [Priority(1)]
            public async Task Award(int amount, ulong id)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == id);
                    if (userDb != null)
                    {
                        userDb.Currency += amount;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        var currency = new UserConfig { UserId = id, Currency = amount };
                        await db.Users.AddAsync(currency).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                var user = await Context.Client.GetUserAsync(id).ConfigureAwait(false);
                await Context.Channel.SendConfirmationMessageAsync($"{Format.Bold(user?.ToString() ?? id.ToString())} has been awarded with {amount} {RiasBot.Currency}");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            [Priority(0)]
            public async Task Award(int amount, string user)
            {
                var userSplit = user.Split("#");
                var getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == getUser.Id);
                    if (userDb != null)
                    {
                        userDb.Currency += amount;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        var currency = new UserConfig { UserId = getUser.Id, Currency = amount };
                        await db.Users.AddAsync(currency).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                await Context.Channel.SendConfirmationMessageAsync($"{Format.Bold(user)} has been awarded with {amount} {RiasBot.Currency}");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            [Priority(2)]
            public async Task Take(int amount, [Remainder]IUser user)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
                    if (userDb != null)
                    {
                        if (userDb.Currency - amount < 0)
                        {
                            return;
                        }
                        else
                        {
                            userDb.Currency -= amount;
                        }
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync($"Took {amount} {RiasBot.Currency} from {Format.Bold(user.ToString())}").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("The user doesn't exists in the database");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            [Priority(1)]
            public async Task Take(int amount, ulong id)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == id);
                    if (userDb != null)
                    {
                        try
                        {
                            if (userDb.Currency - amount < 0)
                            {
                                return;
                            }
                            else
                            {
                                userDb.Currency -= amount;
                            }
                            await db.SaveChangesAsync().ConfigureAwait(false);

                            var user = await Context.Client.GetUserAsync(id).ConfigureAwait(false);
                            await Context.Channel.SendConfirmationMessageAsync($"Took {amount} {RiasBot.Currency} from {Format.Bold(user.ToString())}").ConfigureAwait(false);
                        }
                        catch
                        {
                            await Context.Channel.SendConfirmationMessageAsync($"Took {amount} {RiasBot.Currency} from {Format.Bold(id.ToString())}").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("User doesn't exists in the database").ConfigureAwait(false);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            [Priority(0)]
            public async Task Take(int amount, string user)
            {
                var userSplit = user.Split("#");
                var getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == getUser.Id);
                    if (userDb != null)
                    {
                        if (userDb.Currency - amount < 0)
                        {
                            return;
                        }
                        else
                        {
                            userDb.Currency -= amount;
                        }
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        await Context.Channel.SendConfirmationMessageAsync($"Took {amount} {RiasBot.Currency} from {Format.Bold(user)}").ConfigureAwait(false);
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("The user doesn't exists in the database");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(0)]
            public async Task Currency([Remainder]IUser user = null)
            {
                user = user ?? Context.User;

                var voteInfo = $"Do you want more? You can vote me [here]({_dblVote}) to get extra {RiasBot.Currency}";
                
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
                    if (userDb != null)
                    {
                        if (user == Context.User)
                            await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} you have {userDb.Currency} {RiasBot.Currency}\n{voteInfo}").ConfigureAwait(false);
                        else
                            await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} {user} has {userDb.Currency} {RiasBot.Currency}").ConfigureAwait(false);
                    }
                    else
                    {
                        var currency = new UserConfig { UserId = user.Id, Currency = 0 };
                        await db.Users.AddAsync(currency);
                        await db.SaveChangesAsync();
                        if (user == Context.User)
                            await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} you have 0 {RiasBot.Currency}\n{voteInfo}").ConfigureAwait(false);
                        else
                            await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} {user} has 0 {RiasBot.Currency}").ConfigureAwait(false);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Leaderboard(int page = 1)
            {
                if (page < 1)
                    page = 1;
                page--;
                Emote.TryParse(RiasBot.Currency, out var heartDiamond);
                using (var db = _db.GetDbContext())
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithTitle($"{heartDiamond} Leaderboard");

                    var userDb = db.Users.OrderByDescending(x => x.Currency).Skip(page * 9).Take(9).ToList();

                    for (var i = 0; i < userDb.Count; i++)
                    {
                        var user = await Context.Client.GetUserAsync(userDb[i].UserId);
                        embed.AddField($"#{i+1 + (page * 9)} {user?.ToString() ?? userDb[i].UserId.ToString()}", $"{userDb[i].Currency} {heartDiamond}", true);
                    }
                    if (userDb.Count == 0)
                        embed.WithDescription("No users on this page");

                    await Context.Channel.SendMessageAsync("", embed: embed.Build());
                }
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task DailyAsync()
            {
                var voteInfo = $"Do you want more? You can vote me [here]({_dblVote}) to get extra {RiasBot.Currency}";
                var voteInfoWait = $"In the mean time you can vote me [here]({_dblVote}), if you haven't already, to get extra {RiasBot.Currency}";
                
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == Context.User.Id);
                    var dailyDb = db.Dailies.FirstOrDefault(x => x.UserId == Context.User.Id);
                    
                    var nextDateTimeDaily = DateTime.UtcNow.AddHours(23);
                    
                    if (dailyDb != null)
                    {
                        if (DateTime.Compare(DateTime.UtcNow, dailyDb.NextDaily) >= 0)
                        {
                            dailyDb.NextDaily = nextDateTimeDaily;
                        }
                        else
                        {
                            var timeLeft = dailyDb.NextDaily.Subtract(DateTime.UtcNow);
                            await Context.Channel.SendErrorMessageAsync($"You can get your next daily in {timeLeft.StringTimeSpan()}.\n{voteInfoWait}").ConfigureAwait(false);
                            return;
                        }
                    }
                    else
                    {
                        var newDailyDb = new Dailies { UserId = Context.User.Id, NextDaily = nextDateTimeDaily};
                        await db.AddAsync(newDailyDb).ConfigureAwait(false);
                    }

                    if (userDb != null)
                    {
                        userDb.Currency += 100;
                    }
                    else
                    {
                        var newUserDb = new UserConfig {UserId = Context.User.Id, Currency = 100};
                        await db.AddAsync(newUserDb).ConfigureAwait(false);
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await Context.Channel.SendConfirmationMessageAsync($"You received your daily 100 {RiasBot.Currency}.\n{voteInfo}").ConfigureAwait(false);
                }
            }
        }
    }
}
