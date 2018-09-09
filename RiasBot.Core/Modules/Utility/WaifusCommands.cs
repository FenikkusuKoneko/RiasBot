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
using System.Collections.Generic;
using Discord.Addons.Interactive;

namespace RiasBot.Modules.Utility
{
    public partial class Utility
    {
        public class WaifusCommands : RiasSubmodule<WaifusService>
        {
            private readonly CommandHandler _ch;
            private readonly DbService _db;
            private readonly AnimeService _animeService;
            private readonly InteractiveService _is;
            public WaifusCommands(CommandHandler ch, DbService db, AnimeService animeService, InteractiveService interactiveService)
            {
                _ch = ch;
                _db = db;
                _animeService = animeService;
                _is = interactiveService;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [Priority(1)]
            public async Task ClaimWaifu([Remainder]int character)
            {
                var obj = await _animeService.CharacterSearch(character);

                if (obj is null)
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the character.");
                else
                {
                    await _service.ClaimWaifu((ShardedCommandContext)Context, (IGuildUser)Context.User, Context.Channel, obj);
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
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the character.");
                else
                {
                    if (characters.Count <= 1)
                    {
                        await _service.ClaimWaifu((ShardedCommandContext)Context, (IGuildUser)Context.User, Context.Channel, obj.characters[0]);
                    }
                    else
                    {
                        var listCharacters = new List<string>();
                        for (var i = 0; i < characters.Count(); i++)
                        {
                            var waifuName1 = $"{(string)obj.characters[i].name.first} { (string)obj.characters[i].name.last}";
                            listCharacters.Add($"{waifuName1}\tId: {obj.characters[i].id}\n");
                        }
                        var pager = new PaginatedMessage
                        {
                            Title = $"I've found {characters.Count()} characters for {character}. Claim a waifu by id",
                            Color = new Color(RiasBot.GoodColor),
                            Pages = listCharacters,
                            Options = new PaginatedAppearanceOptions
                            {
                                ItemsPerPage = 10,
                                Timeout = TimeSpan.FromMinutes(1),
                                DisplayInformationIcon = false,
                                JumpDisplayOptions = JumpDisplayOptions.Never
                            }

                        };
                        await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task BelovedWaifu([Remainder] string waifu)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == Context.User.Id);
                    if (userDb != null)
                    {
                        if (userDb.Currency < 5000)
                        {
                            await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have enough {RiasBot.Currency}. 5000 {RiasBot.Currency} required.");
                            return;
                        }
                    }
                    var waifuDb = db.Waifus.Where(x => x.UserId == Context.User.Id).ToList();
                    if (waifuDb.Count != 0)
                    {
                        var getWaifu = waifuDb.FirstOrDefault(x => x.WaifuName.Equals(waifu, StringComparison.InvariantCultureIgnoreCase));
                        if (getWaifu is null)
                        {
                            waifu = string.Join(" ", waifu.Split(" ", StringSplitOptions.RemoveEmptyEntries).Reverse());
                            getWaifu = waifuDb.FirstOrDefault(x => x.WaifuName.Equals(waifu, StringComparison.InvariantCultureIgnoreCase));
                        }
                        
                        if (getWaifu != null)
                        {
                            var lastPrimaryWaifu = waifuDb.FirstOrDefault(x => x.IsPrimary == true);
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
                            await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} {getWaifu.WaifuName} is now your beloved waifu :heart:.");
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't find the waifu.").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have any waifu.").ConfigureAwait(false);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task Divorce([Remainder]string character)
            {
                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.FirstOrDefault(x => x.UserId == Context.User.Id);
                    var waifusDb = db.Waifus.Where(x => x.UserId == Context.User.Id).ToList();

                    if (waifusDb.Any())
                    {
                        var waifu = waifusDb.FirstOrDefault(x => string.Equals(x.WaifuName, character, StringComparison.InvariantCultureIgnoreCase));
                        if (waifu is null)
                        {
                            character = string.Join(" ", character.Split(" ", StringSplitOptions.RemoveEmptyEntries).Reverse());
                            waifu = waifusDb.FirstOrDefault(x => x.WaifuName.Equals(character, StringComparison.InvariantCultureIgnoreCase));
                        }
                        if (waifu != null)
                        {
                            var waifuCashback = waifu.WaifuPrice * 90 / 100;
                            userDb.Currency += waifuCashback;

                            db.Remove(waifu);
                            await db.SaveChangesAsync().ConfigureAwait(false);

                            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                            embed.WithTitle("Divorce!");
                            embed.WithDescription($"You successfully divorced from {waifu.WaifuName}. You received {waifuCashback} {RiasBot.Currency} back.");
                            embed.WithThumbnailUrl(!string.IsNullOrEmpty(waifu.BelovedWaifuPicture) ? waifu.BelovedWaifuPicture : waifu.WaifuPicture);

                            await Context.Channel.SendMessageAsync("", embed: embed.Build());
                        }
                        else
                        {
                            await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't find your waifu.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have any waifu.");
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
                        var waifus = new List<string>();
                        for (var i = 0; i < waifusDb.Count(); i++)
                        {
                            if (waifusDb[i].IsPrimary)
                                waifus.Add($"#{i+1} ❤️ [{waifusDb[i].WaifuName}]({waifusDb[i].WaifuUrl})\tId: {waifusDb[i].WaifuId}\tPrice: {waifusDb[i].WaifuPrice} {RiasBot.Currency}");
                            else
                                waifus.Add($"#{i+1}[{waifusDb[i].WaifuName}]({waifusDb[i].WaifuUrl})\tId: {waifusDb[i].WaifuId}\tPrice: {waifusDb[i].WaifuPrice} {RiasBot.Currency}");
                        }

                        if (waifusDb.Count() > 0)
                        {
                            var pager = new PaginatedMessage
                            {
                                Title = $"All waifus for {user}",
                                Color = new Color(RiasBot.GoodColor),
                                Pages = waifus,
                                Options = new PaginatedAppearanceOptions
                                {
                                    ItemsPerPage = 5,
                                    Timeout = TimeSpan.FromMinutes(1),
                                    DisplayInformationIcon = false,
                                    JumpDisplayOptions = JumpDisplayOptions.Never
                                }

                            };
                            await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
                        }
                        else if (user == Context.User)
                            await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have any waifu.");
                        else
                            await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} {user} doesn't have any waifu.");
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
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the url is not a well formed uri string.").ConfigureAwait(false);
                    return;
                }
                if (!url.Contains("https"))
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the url must be https").ConfigureAwait(false);
                    return;

                }
                if (!url.Contains(".png") && !url.Contains(".jpg") && !url.Contains(".jpeg"))
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the url is not a direct link for a png, jpg or jpeg image.").ConfigureAwait(false);
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
                            await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} new avatar set for {Format.Bold(belovedWaifu.WaifuName)}.");
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have a beloved waifu.").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have any waifu.").ConfigureAwait(false);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            public async Task CreateWaifu(string url, [Remainder]string name)
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the url is not a well formed uri string.").ConfigureAwait(false);
                    return;
                }
                if (!url.Contains(".png") && !url.Contains(".jpg") && !url.Contains(".jpeg"))
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the url is not a direct link for a png, jpg or jpeg image.").ConfigureAwait(false);
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

                            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                            embed.WithDescription($"Congratulations!\nYou successfully created {Format.Bold(name)}.");
                            embed.WithThumbnailUrl(url);
                            await Context.Channel.SendMessageAsync("", embed: embed.Build());

                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have enough {RiasBot.Currency}.");
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} you don't have enough {RiasBot.Currency}.");
                    }
                }
            }
        }
    }
}
