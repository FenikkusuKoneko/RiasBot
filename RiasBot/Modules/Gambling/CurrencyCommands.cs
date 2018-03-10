using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RiasBot.Extensions;

namespace RiasBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class CurrencyCommands : RiasSubmodule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            private readonly DbService _db;

            public CurrencyCommands(CommandHandler ch, CommandService service, DbService db)
            {
                _ch = ch;
                _service = service;
                _db = db;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            [Priority(2)]
            public async Task Award(int amount, [Remainder]IUser user)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.Where(x => x.UserId == user.Id).FirstOrDefault();
                    try
                    {
                        userDb.Currency += amount;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        var currency = new UserConfig { UserId = user.Id, Currency = amount };
                        await db.Users.AddAsync(currency).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                await ReplyAsync($"{Format.Bold(user.ToString())} has been awarded with {amount} {RiasBot.currency}");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            [Priority(1)]
            public async Task Award(int amount, ulong id)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.Where(x => x.UserId == id).FirstOrDefault();
                    try
                    {
                        userDb.Currency += amount;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        var currency = new UserConfig { UserId = id, Currency = amount };
                        await db.Users.AddAsync(currency).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                var user = await Context.Client.GetUserAsync(id).ConfigureAwait(false);
                await ReplyAsync($"{Format.Bold(user?.ToString() ?? id.ToString())} has been awarded with {amount} {RiasBot.currency}");
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
                    var userDb = db.Users.Where(x => x.UserId == getUser.Id).FirstOrDefault();
                    try
                    {
                        userDb.Currency += amount;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        var currency = new UserConfig { UserId = getUser.Id, Currency = amount };
                        await db.Users.AddAsync(currency).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                await ReplyAsync($"{Format.Bold(user.ToString())} has been awarded with {amount} {RiasBot.currency}");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            [Priority(2)]
            public async Task Take(int amount, [Remainder]IUser user)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.Where(x => x.UserId == user.Id).FirstOrDefault();
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
                        await ReplyAsync($"Took {amount} {RiasBot.currency} from {Format.Bold(user.ToString())}").ConfigureAwait(false);
                    }
                    catch
                    {
                        await ReplyAsync("The user doesn't exists in the database");
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
                    var userDb = db.Users.Where(x => x.UserId == id).FirstOrDefault();
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
                            await ReplyAsync($"Took {amount} {RiasBot.currency} from {Format.Bold(user.ToString())}").ConfigureAwait(false);
                        }
                        catch
                        {
                            await ReplyAsync($"Took {amount} {RiasBot.currency} from {Format.Bold(id.ToString())}").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed("User doesn't exists in the database").ConfigureAwait(false);
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
                    var userDb = db.Users.Where(x => x.UserId == getUser.Id).FirstOrDefault();
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
                        await ReplyAsync($"Took {amount} {RiasBot.currency} from {Format.Bold(user.ToString())}").ConfigureAwait(false);
                    }
                    catch
                    {
                        await ReplyAsync("The user doesn't exists in the database");
                    }
                }
            }

            [RiasCommand][Discord.Commands.Alias]
            [Description][@Remarks]
            public async Task Give(int amount, [Remainder]IGuildUser user)
            {
                using (var db = _db.GetDbContext())
                {
                    var firstUser = db.Users.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                    var secondUser = db.Users.Where(x => x.UserId == user.Id).FirstOrDefault();
                    try
                    {
                        if (firstUser.Currency > 0)
                        {
                            if (firstUser.Currency - amount >= 0)
                            {
                                firstUser.Currency -= amount;
                                secondUser.Currency += amount;
                                await db.SaveChangesAsync().ConfigureAwait(false);
                                await ReplyAsync($"{user.Mention} you received {amount} {RiasBot.currency} from {Format.Bold(Context.User.ToString())}");
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(0)]
            public async Task Currency([Remainder]IUser user = null)
            {
                user = user ?? Context.User;

                using (var db = _db.GetDbContext())
                {
                    int currencyAmount = 0;
                    var userDb = db.Users.Where(x => x.UserId == user.Id).FirstOrDefault();
                    try
                    {
                        currencyAmount = userDb.Currency;
                        if (user == Context.User)
                            await ReplyAsync($"{Context.User.Mention} {user} you have {userDb.Currency} {RiasBot.currency}").ConfigureAwait(false);
                        else
                            await ReplyAsync($"{Context.User.Mention} {user} has {userDb.Currency} {RiasBot.currency}");
                    }
                    catch
                    {
                        var currency = new UserConfig { UserId = user.Id, Currency = 0 };
                        await db.Users.AddAsync(currency);
                        await db.SaveChangesAsync();
                        if (user == Context.User)
                            await ReplyAsync($"{Context.User.Mention} {user} you have 0 {RiasBot.currency}").ConfigureAwait(false);
                        else
                            await ReplyAsync($"{Context.User.Mention} {user} has 0 {RiasBot.currency}");
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
                Emote.TryParse(RiasBot.currency, out var heartDiamond);
                using (var db = _db.GetDbContext())
                {
                    var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                    embed.WithTitle($"{heartDiamond} Leaderboard");

                    var userDb = db.Users.OrderByDescending(x => x.Currency).Skip(page * 9).Take(9).ToList();

                    for (int i = 0; i < userDb.Count; i++)
                    {
                        var user = await Context.Client.GetUserAsync(userDb[i].UserId);
                        embed.AddField($"#{i+1 + (page * 9)} {user?.ToString() ?? userDb[i].UserId.ToString()}", $"{userDb[i].Currency} {heartDiamond}", true);
                    }
                    if (userDb.Count == 0)
                        embed.WithDescription("No users on this page");

                    await ReplyAsync("", embed: embed.Build());
                }
            }
        }
    }
}
