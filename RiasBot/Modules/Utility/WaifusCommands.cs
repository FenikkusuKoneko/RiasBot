using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Searches.Services;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RiasBot.Modules.Utility
{
    public partial class Utility
    {
        public class WaifusCommands : RiasSubmodule<AnimeService>
        {
            private readonly CommandHandler _ch;
            private readonly DbService _db;
            public WaifusCommands(CommandHandler ch, DbService db)
            {
                _ch = ch;
                _db = db;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(1)]
            public async Task ClaimWaifu([Remainder]int character)
            {
                var obj = await _service.CharacterSearch(character);

                if (obj is null)
                    await ReplyAsync("Sorry I couldn't find the character.");
                else
                {
                    int waifuId = (int)obj.id;
                    string waifuName = null;

                    if (String.IsNullOrEmpty((string)obj.name.first))
                        waifuName = (string)obj.name.last;
                    else if (String.IsNullOrEmpty((string)obj.name.last))
                        waifuName = (string)obj.name.first;
                    else
                        waifuName = $"{(string)obj.name.first} {(string)obj.name.last}";

                    string waifuUrl = (string)obj.siteUrl;
                    string waifuPicture = (string)obj.image.large;
                    var random = new Random((int)DateTime.UtcNow.Ticks);
                    int price = random.Next(5000, 10001);

                    using (var db = _db.GetDbContext())
                    {
                        var userDb = db.Users.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                        var waifuDb = db.Waifus.Where(x => x.UserId == Context.User.Id);
                        try
                        {
                            if (userDb.Currency < 10000)
                            {
                                await ReplyAsync($"You need to have at least 10000 {RiasBot.currency} to claim a waifu.");
                                return;
                            }
                            if (waifuDb.Any(x => x.WaifuId == waifuId))
                            {
                                await ReplyAsync($"{Context.User.Mention} you already claimed this waifu.");
                                return;
                            }

                            userDb.Currency -= price;

                            var waifus = new Waifus { UserId = Context.User.Id, WaifuId = waifuId, WaifuName = waifuName,
                                WaifuUrl = waifuUrl, WaifuPicture = waifuPicture, WaifuPrice = price };
                            await db.AddAsync(waifus).ConfigureAwait(false);
                            await db.SaveChangesAsync().ConfigureAwait(false);

                            var embed = new EmbedBuilder().WithColor(RiasBot.color);
                            embed.WithDescription($"Congratulations!\nYou successfully claimed {Format.Bold(waifuName)} for {Format.Bold(price.ToString())} {RiasBot.currency}");
                            embed.WithThumbnailUrl(waifuPicture);
                            await ReplyAsync("", embed: embed.Build());
                        }
                        catch
                        {

                        }
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(0)]
            public async Task ClaimWaifu([Remainder]string character)
            {
                var obj = await _service.CharacterSearch(character);

                var characters = (JArray)obj.characters;
                if (characters.Count == 0)
                    await ReplyAsync("Sorry I couldn't find the character.");
                else
                {
                    if (characters.Count <= 1)
                    {
                        int waifuId = (int)obj.characters[0].id;
                        string waifuName = null;

                        if (String.IsNullOrEmpty((string)obj.characters[0].name.first))
                            waifuName = (string)obj.characters[0].name.last;
                        else if (String.IsNullOrEmpty((string)obj.characters[0].name.last))
                            waifuName = (string)obj.characters[0].name.first;
                        else
                            waifuName = $"{(string)obj.characters[0].name.first} {(string)obj.characters[0].name.last}";

                        string waifuUrl = (string)obj.characters[0].siteUrl;
                        string waifuPicture = (string)obj.characters[0].image.large;
                        var random = new Random((int)DateTime.UtcNow.Ticks);
                        int price = random.Next(5000, 10001);

                        using (var db = _db.GetDbContext())
                        {
                            var userDb = db.Users.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                            var waifuDb = db.Waifus.Where(x => x.UserId == Context.User.Id);
                            try
                            {
                                if (userDb.Currency < 10000)
                                {
                                    await ReplyAsync($"You need to have at least 10000 {RiasBot.currency} to claim a waifu.");
                                    return;
                                }
                                if (waifuDb.Any(x => x.WaifuId == waifuId))
                                {
                                    await ReplyAsync($"{Context.User.Mention} you already claimed this waifu.");
                                    return;
                                }

                                userDb.Currency -= price;

                                var waifus = new Waifus { UserId = Context.User.Id, WaifuId = waifuId, WaifuName = waifuName,
                                    WaifuUrl = waifuUrl, WaifuPicture = waifuPicture, WaifuPrice = price };
                                await db.AddAsync(waifus).ConfigureAwait(false);
                                await db.SaveChangesAsync().ConfigureAwait(false);

                                var embed = new EmbedBuilder().WithColor(RiasBot.color);
                                embed.WithDescription($"Congratulations!\nYou successfully claimed {Format.Bold(waifuName)} for {Format.Bold(price.ToString())} {RiasBot.currency}");
                                embed.WithThumbnailUrl(waifuPicture);
                                await ReplyAsync("", embed: embed.Build());
                            }
                            catch
                            {

                            }
                        }
                    }
                    else
                    {
                        string[] listCharacters = new string[characters.Count];
                        for (int i = 0; i < characters.Count(); i++)
                        {
                            string waifuName1 = $"{(string)obj.characters[i].name.first} { (string)obj.characters[i].name.last}";
                            listCharacters[i] = $"{waifuName1}\tId: {obj.characters[i].id}\n";
                        }
                        await Context.Channel.SendPaginated((DiscordSocketClient)Context.Client, $"I've found {characters.Count()} characters for {character}. Claim a waifu by id",
                            listCharacters, 10);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task PrimaryWaifu([Remainder] string waifu)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                    if (userDb != null)
                    {
                        if (userDb.Currency < 10000)
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you don't have enough {RiasBot.currency}.");
                            return;
                        }
                    }
                    var waifuDb = db.Waifus.Where(x => x.UserId == Context.User.Id).ToList();
                    if (waifuDb.Count != 0)
                    {
                        var getWaifu = waifuDb.Where(x => x.WaifuName.ToLowerInvariant() == waifu.ToLowerInvariant()).FirstOrDefault();
                        if (getWaifu != null)
                        {
                            var lastPrimaryWaifu = waifuDb.Where(x => x.IsPrimary == true).FirstOrDefault();
                            if (lastPrimaryWaifu != null)
                            {
                                lastPrimaryWaifu.IsPrimary = false;
                                getWaifu.IsPrimary = true;
                            }
                            else
                            {
                                getWaifu.IsPrimary = true;
                            }
                            userDb.Currency -= 10000;
                            await db.SaveChangesAsync().ConfigureAwait(false);
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} {getWaifu.WaifuName} is not your beloved waifu :heart:.");
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the user.").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you don't have any waifu.").ConfigureAwait(false);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Divorce([Remainder]string character)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                    var waifusDb = db.Waifus.Where(x => x.UserId == Context.User.Id).ToList();

                    if (waifusDb.Count() > 0)
                    {
                        var waifu = waifusDb.Where(x => x.WaifuName.ToLowerInvariant() == character.ToLowerInvariant()).FirstOrDefault();
                        if (waifu != null)
                        {
                            int waifuCashback = waifu.WaifuPrice * 90 / 100;
                            userDb.Currency += waifuCashback;

                            db.Remove(waifu);
                            await db.SaveChangesAsync().ConfigureAwait(false);

                            var embed = new EmbedBuilder().WithColor(RiasBot.color);
                            embed.WithTitle("Divorce!");
                            embed.WithDescription($"You successfully divorced from {waifu.WaifuName}. You received {waifuCashback} {RiasBot.currency} back.");
                            embed.WithThumbnailUrl(waifu.WaifuPicture);

                            await ReplyAsync("", embed: embed.Build());
                        }
                        else
                        {
                            await ReplyAsync($"{Context.User.Mention} I couldn't find your waifu.");
                        }
                    }
                    else
                    {
                        await ReplyAsync($"{Context.User.Mention} you don't have any waifu.");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Waifus([Remainder]IUser user = null)
            {
                user = user ?? Context.User;

                using (var db = _db.GetDbContext())
                {
                    var waifusDb = db.Waifus.Where(x => x.UserId == user.Id).ToList();

                    try
                    {
                        string[] waifus = new string[waifusDb.Count()];
                        for (int i = 0; i < waifusDb.Count(); i++)
                        {
                            if (waifusDb[i].IsPrimary)
                                waifus[i] = $"#{i+1} ❤️ [{waifusDb[i].WaifuName}]({waifusDb[i].WaifuUrl})\tId: {waifusDb[i].WaifuId}\tPrice: {waifusDb[i].WaifuPrice} {RiasBot.currency}";
                            else
                                waifus[i] = $"#{i+1}[{waifusDb[i].WaifuName}]({waifusDb[i].WaifuUrl})\tId: {waifusDb[i].WaifuId}\tPrice: {waifusDb[i].WaifuPrice} {RiasBot.currency}";
                        }

                        if (waifusDb.Count() > 0)
                            await Context.Channel.SendPaginated((DiscordSocketClient)Context.Client, $"All waifus for {user}", waifus, 10);
                        else if (user == Context.Message.Author)
                            await ReplyAsync($"{user.Mention} you don't have any waifu.");
                        else
                            await ReplyAsync($"{Context.Message.Author.Mention} {user} doesn't have have any waifu.");
                    }
                    catch
                    {
                        
                    }
                }
            }
        }
    }
}
