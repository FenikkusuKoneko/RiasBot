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
using RiasBot.Modules.Utility.Services;

namespace RiasBot.Modules.Utility
{
    public partial class Utility
    {
        public class WaifusCommands : RiasSubmodule<WaifusService>
        {
            private readonly CommandHandler _ch;
            private readonly DbService _db;
            private readonly AnimeService _animeService;
            public WaifusCommands(CommandHandler ch, DbService db, AnimeService animeService)
            {
                _ch = ch;
                _db = db;
                _animeService = animeService;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(1)]
            public async Task ClaimWaifu([Remainder]int character)
            {
                var obj = await _animeService.CharacterSearch(character);

                if (obj is null)
                    await Context.Channel.SendErrorEmbed("I couldn't find the character.");
                else
                {
                    await _service.ClaimWaifu((IGuildUser)Context.User, Context.Channel, obj, this);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(0)]
            public async Task ClaimWaifu([Remainder]string character)
            {
                var obj = await _animeService.CharacterSearch(character);

                var characters = (JArray)obj.characters;
                if (characters.Count == 0)
                    await Context.Channel.SendErrorEmbed("I couldn't find the character.");
                else
                {
                    if (characters.Count <= 1)
                    {
                        await _service.ClaimWaifu((IGuildUser)Context.User, Context.Channel, obj.characters[0], this);
                    }
                    else
                    {
                        string[] listCharacters = new string[characters.Count];
                        for (int i = 0; i < characters.Count(); i++)
                        {
                            string waifuName1 = $"{(string)obj.characters[i].name.first} { (string)obj.characters[i].name.last}";
                            listCharacters[i] = $"{waifuName1}\tId: {obj.characters[i].id}\n";
                        }
                        await Context.Channel.SendPaginated((DiscordShardedClient)Context.Client, $"I've found {characters.Count()} characters for {character}. Claim a waifu by id",
                            listCharacters, 10);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task BelovedWaifu([Remainder] string waifu)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                    if (userDb != null)
                    {
                        if (userDb.Currency < 5000)
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
                                lastPrimaryWaifu.BelovedWaifuPicture = null;
                                getWaifu.IsPrimary = true;
                            }
                            else
                            {
                                getWaifu.IsPrimary = true;
                            }
                            userDb.Currency -= 5000;
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} {getWaifu.WaifuName} is now your beloved waifu :heart:.");
                            await db.SaveChangesAsync().ConfigureAwait(false);
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

                            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                            embed.WithTitle("Divorce!");
                            embed.WithDescription($"You successfully divorced from {waifu.WaifuName}. You received {waifuCashback} {RiasBot.currency} back.");
                            if (!String.IsNullOrEmpty(waifu.BelovedWaifuPicture))
                                embed.WithThumbnailUrl(waifu.BelovedWaifuPicture);
                            else
                                embed.WithThumbnailUrl(waifu.WaifuPicture);

                            await Context.Channel.SendMessageAsync("", embed: embed.Build());
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find your waifu.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you don't have any waifu.");
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
                            await Context.Channel.SendPaginated((DiscordShardedClient)Context.Client, $"All waifus for {user}", waifus, 10);
                        else if (user == Context.User)
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you don't have any waifu.");
                        else
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} {user} doesn't have have any waifu.");
                    }
                    catch
                    {
                        
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task BelovedWaifuAvatar(string url)
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the url is not a well formed uri string.").ConfigureAwait(false);
                    return;
                }
                if (!url.Contains(".png") && !url.Contains(".jpg") && !url.Contains(".jpeg"))
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the url is not a direck link for a png, jpg or jpeg image.").ConfigureAwait(false);
                    return;
                }
                using (var db = _db.GetDbContext())
                {
                    var waifus = db.Waifus.Where(x => x.UserId == Context.User.Id);
                    if (waifus != null)
                    {
                        var belovedWaifu = waifus.Where(x => x.IsPrimary == true).FirstOrDefault();
                        if (belovedWaifu != null)
                        {
                            belovedWaifu.BelovedWaifuPicture = url;
                            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} new avatar set for {Format.Bold(belovedWaifu.WaifuName)}.");
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you don't have a beloved waifu.").ConfigureAwait(false);
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
            public async Task CreateWaifu(string url, [Remainder]string name)
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the url is not a well formed uri string.").ConfigureAwait(false);
                    return;
                }
                if (!url.Contains(".png") && !url.Contains(".jpg") && !url.Contains(".jpeg"))
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} the url is not a direck link for a png, jpg or jpeg image.").ConfigureAwait(false);
                    return;
                }
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.Where(x => x.UserId == Context.User.Id).FirstOrDefault();
                    var waifuDb = db.Waifus.Where(x => x.UserId == Context.User.Id);
                    if (userDb != null)
                    {
                        if (userDb.Currency >= 10000)
                        {
                            if (waifuDb != null)
                            {
                                var lastPrimaryWaifu = waifuDb.Where(x => x.IsPrimary == true).FirstOrDefault();
                                if (lastPrimaryWaifu != null)
                                {
                                    lastPrimaryWaifu.IsPrimary = false;
                                    lastPrimaryWaifu.BelovedWaifuPicture = null;
                                }
                            }
                            var waifu = new Waifus { UserId = Context.User.Id, WaifuName = name, WaifuPicture = url, WaifuPrice = 10000, IsPrimary = true };
                            await db.AddAsync(waifu).ConfigureAwait(false);
                            userDb.Currency -= 10000;

                            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                            embed.WithDescription($"Congratulations!\nYou successfully created {Format.Bold(name)}.");
                            embed.WithThumbnailUrl(url);
                            await Context.Channel.SendMessageAsync("", embed: embed.Build());

                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you don't have enough {RiasBot.currency}.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorEmbed($"{Context.User.Mention} you don't have enough {RiasBot.currency}.");
                    }
                }
            }
        }
    }
}
