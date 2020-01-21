using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Models;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Interactive;
using Rias.Interactive.Paginator;

namespace Rias.Core.Modules.Waifu
{
    [Name("Waifu")]
    public class Waifu : RiasModule
    {
        private readonly AnimeService _animeService;
        private readonly InteractiveService _interactive;
        private readonly HttpClient _httpClient;
        
        public Waifu(IServiceProvider services) : base(services)
        {
            _animeService = services.GetRequiredService<AnimeService>();
            _interactive = services.GetRequiredService<InteractiveService>();
            _httpClient = services.GetRequiredService<HttpClient>();
        }

        private const int WaifuStartingPrice = 1000;
        private const int SpecialWaifuPrice = 5000;
        private const int WaifuCreationPrice = 10000; 

        private const int WaifuPositionLimit = 1000;

        [Command("claim"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task ClaimAsync([Remainder] string name)
        {
            var character = await _animeService.GetOrAddCharacterAsync(name);
            if (character is null)
            {
                await ReplyErrorAsync("#Searches_CharacterNotFound");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Url = character is Characters animeCharacter? animeCharacter.Url : default,
                Title = character.Name,
                ThumbnailUrl = character.ImageUrl
            };

            var claimCanceled = false;

            var waifus = (await DbContext.GetListAsync<Waifus, Characters, CustomCharacters>
            (
                x => x.UserId == Context.User.Id,
                x => x.Character!,
                x => x.CustomCharacter!
            )).Cast<IWaifus>().ToList();
            waifus.AddRange(await DbContext.GetListAsync<CustomWaifus>(x => x.UserId == Context.User.Id));
            
            var waifu = character switch
            {
                CustomCharacters _ => waifus.FirstOrDefault(x => x is Waifus normalWaifu && normalWaifu.CustomCharacterId == character.CharacterId),
                _ => waifus.FirstOrDefault(x => x is Waifus normalWaifu && normalWaifu.CharacterId == character.CharacterId)
            };
            
            if (waifu != null)
            {
                embed.WithDescription(GetText("HasWaifu"));
                claimCanceled = true;
            }
            
            var waifuUsers = character is CustomCharacters
                ? DbContext.Waifus.Where(x => x.CustomCharacterId == character.CharacterId).ToList()
                : DbContext.Waifus.Where(x => x.CharacterId == character.CharacterId).ToList();
            
            var waifuPrice = WaifuStartingPrice + waifuUsers.Count * 10;
            var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new Users {UserId = Context.User.Id});
            if (!claimCanceled && userDb.Currency < waifuPrice)
            {
                embed.WithDescription(GetText("ClaimCurrencyNotEnough", Credentials.Currency));
                claimCanceled = true;
            }

            if (!claimCanceled)
            {
                embed.WithDescription(GetText("ClaimConfirmation"));
            }

            embed.AddField(GetText("ClaimedBy"), $"{waifuUsers.Count} {GetText("#Common_Users").ToLowerInvariant()}", true)
                .AddField(GetText("#Utility_Price"), waifuPrice, true);
            
            await Context.Channel.SendMessageAsync(Format.Bold(GetText("ClaimNote", Context.Prefix)), embed: embed.Build());

            if (claimCanceled)
                return;

            var message = await _interactive.NextMessageAsync(Context.Message);
            if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync("ClaimCanceled");
                return;
            }
            
            userDb.Currency -= waifuPrice;
            var waifuDb = new Waifus
            {
                UserId = Context.User.Id,
                Price = waifuPrice,
                Position = waifus.Count != 0 ? waifus.Max(x => x.Position) + 1 : 1
            };

            if (character is CustomCharacters)
                waifuDb.CustomCharacterId = character.CharacterId;
            else
                waifuDb.CharacterId = character.CharacterId;

            await DbContext.AddAsync(waifuDb);
            await DbContext.SaveChangesAsync();

            embed.WithDescription(GetText("WaifuClaimed", character.Name!, waifuPrice, Credentials.Currency));
            embed.Fields.Clear();

            await ReplyAsync(embed);
        }

        [Command("divorce"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task DivorceAsync([Remainder] string name)
        {
            var waifu = await GetWaifuAsync(name);
            if (waifu is null)
            {
                await ReplyErrorAsync("NotFound");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = waifu.Name,
                Description = GetText("DivorceConfirmation", waifu.Name!),
                ThumbnailUrl = waifu.ImageUrl
            };

            await ReplyAsync(embed);
            
            var message = await _interactive.NextMessageAsync(Context.Message);
            if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync("DivorceCanceled");
                return;
            }

            if (waifu is CustomWaifus customWaifu)
                DbContext.Remove(customWaifu);
            else
                DbContext.Remove((Waifus) waifu);
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("Divorced", waifu.Name!);
        }

        [Command("all"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task AllWaifusAsync([Remainder] SocketGuildUser? user = null)
        {
            user ??= (SocketGuildUser) Context.User;

            var allWaifus = (await DbContext.GetListAsync<Waifus, Characters, CustomCharacters>
            (
                x => x.UserId == user.Id,
                x => x.Character!,
                x => x.CustomCharacter!
            )).Cast<IWaifus>().ToList();
            allWaifus.AddRange(await DbContext.GetListAsync<CustomWaifus>(x => x.UserId == user.Id));
            
            if (allWaifus.Count == 0)
            {
                if (user.Id == Context.User.Id)
                    await ReplyErrorAsync("NoWaifus");
                else
                    await ReplyErrorAsync("UserNoWaifus", user);
                
                return;
            }

            var specialWaifu = allWaifus.FirstOrDefault(x => x.IsSpecial);
            var specialWaifuString = "";
            if (specialWaifu != null)
            {
                var specialWaifuStringify = StringifyWaifu(specialWaifu);
                specialWaifuString = $"❤ {specialWaifuStringify[..specialWaifuStringify.IndexOf('\n')]}";
            }

            if (specialWaifu != null)
                allWaifus.Remove(specialWaifu);

            var waifus = allWaifus.Where(x => x.Position != 0)
                .OrderBy(x => x.Position)
                .Concat(allWaifus.Where(x => x.Position == 0))
                .ToList();

            if (waifus.Count == 0)
            {
                var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = user.Id == Context.User.Id ? GetText("AllWaifus") : GetText("AllUserWaifus", user),
                    Description = specialWaifuString
                };

                await ReplyAsync(embed);
                return;
            }

            var pages = waifus.Batch(10, x => new InteractiveMessage
            (
                new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = user.Id == Context.User.Id ? GetText("AllWaifus") : GetText("AllUserWaifus", user),
                    Description = $"{specialWaifuString}\n\n{string.Join("\n\n", x.Select(StringifyWaifu))}",
                    ThumbnailUrl = specialWaifu?.ImageUrl
                }
            ));

            await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
        }

        [Command("special"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task SpecialWaifuAsync([Remainder] string name)
        {
            var waifu = await GetWaifuAsync(name);
            if (waifu is null)
            {
                await ReplyErrorAsync("NotFound");
                return;
            }

            if (waifu.IsSpecial)
            {
                await ReplyErrorAsync("AlreadySpecial", waifu.Name!);
                return;
            }
            
            var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new Users {UserId = Context.User.Id});
            if (userDb.Currency < SpecialWaifuPrice)
            {
                await ReplyErrorAsync("#Gambling_CurrencyNotEnough", Credentials.Currency);
                return;
            }
            
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = waifu.Name,
                Description = GetText("SpecialConfirmation", waifu.Name!),
                ThumbnailUrl = waifu.ImageUrl
            };

            await ReplyAsync(embed);

            var message = await _interactive.NextMessageAsync(Context.Message);
            if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync("SpecialCanceled");
                return;
            }
            
            userDb.Currency -= SpecialWaifuPrice;
            var currentSpecialWaifu = (IWaifus) await DbContext.Waifus.FirstOrDefaultAsync(x => x.UserId == Context.User.Id && x.IsSpecial)
                                      ?? await DbContext.CustomWaifus.FirstOrDefaultAsync(x => x.UserId == Context.User.Id && x.IsSpecial);

            if (currentSpecialWaifu != null)
            {
                currentSpecialWaifu.IsSpecial = false;
                if (currentSpecialWaifu is Waifus currentWaifuDb)
                    currentWaifuDb.CustomImageUrl = null;
            }

            var waifuDb = waifu is CustomWaifus
                ? await DbContext.CustomWaifus.FirstOrDefaultAsync(x => x.Id == waifu.Id)
                : (IWaifus) await DbContext.Waifus.FirstOrDefaultAsync(x => x.Id == waifu.Id);
            
            if (waifuDb != null)
                waifuDb.IsSpecial = true;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("Special", waifu.Name!);
        }

        [Command("image"), Context(ContextType.Guild),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
        public async Task WaifuImageAsync(string url, [Remainder] string name)
        {
            var waifu = await GetWaifuAsync(name);
            if (waifu is null)
            {
                await ReplyErrorAsync("NotFound");
                Context.Command.ResetCooldowns();
                return;
            }

            if (!(waifu.IsSpecial || waifu is CustomWaifus))
            {
                await ReplyErrorAsync("ImageSetError");
                Context.Command.ResetCooldowns();
                return;
            }
            
            if (!Uri.TryCreate(url, UriKind.Absolute, out var imageUri))
            {
                await ReplyErrorAsync("#Utility_UrlNotValid");
                Context.Command.ResetCooldowns();
                return;
            }

            if (imageUri.Scheme != Uri.UriSchemeHttps)
            {
                await ReplyErrorAsync("#Utility_UrlNotHttps");
                Context.Command.ResetCooldowns();
                return;
            }

            using var result = await _httpClient.GetAsync(imageUri);
            if (!result.IsSuccessStatusCode)
            {
                await ReplyErrorAsync("#Utility_ImageOrUrlNotGood");
                Context.Command.ResetCooldowns();
                return;
            }

            await using var imageStream = await result.Content.ReadAsStreamAsync();

            if (!(RiasUtils.IsPng(imageStream) || RiasUtils.IsJpg(imageStream)))
            {
                await ReplyErrorAsync("#Utility_UrlNotPngJpgGif");
                return;
            }

            if (waifu is Waifus normalWaifuDb)
                normalWaifuDb.CustomImageUrl = url;
            else
                ((CustomWaifus) waifu).ImageUrl = url;
                
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("ImageSet", waifu.Name!);
        }

        [Command("position"), Context(ContextType.Guild)]
        public async Task WaifuPositionAsync(int position, [Remainder] string name)
        {
            var waifu = await GetWaifuAsync(name);
            if (waifu is null)
            {
                await ReplyErrorAsync("NotFound");
                Context.Command.ResetCooldowns();
                return;
            }

            if (position < 1)
            {
                await ReplyErrorAsync("PositionLowerLimit");
                return;
            }
            
            if (position > WaifuPositionLimit)
            {
                await ReplyErrorAsync("PositionHigherLimit", WaifuPositionLimit);
                return;
            }

            if (waifu.Position == position)
            {
                await ReplyErrorAsync("HasPosition", waifu.Name!, position);
                return;
            }
            
            var waifus = (await DbContext.GetListAsync<Waifus, Characters, CustomCharacters>
            (
                x => x.UserId == Context.User.Id,
                x => x.Character!,
                x => x.CustomCharacter!
            )).Cast<IWaifus>().ToList();
            waifus.AddRange(await DbContext.GetListAsync<CustomWaifus>(x => x.UserId == Context.User.Id));
            
            waifus = waifus.Where(x => x.Position != 0)
                .OrderBy(x => x.Position)
                .Concat(waifus.Where(x => x.Position == 0))
                .ToList();

            var currentWaifu = waifus.FirstOrDefault(x => x.GetType().IsInstanceOfType(waifu) && x.Id == waifu.Id);
            waifus.Remove(currentWaifu);
            
            position = Math.Min(position, waifus.Count);
            waifus.Insert(position - 1, currentWaifu);
            
            for (var i = 0; i < waifus.Count; i++)
                waifus[i].Position = i + 1;
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("PositionSet", waifu.Name!, position);
        }

        [Command("create"), Context(ContextType.Guild),
         Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.User)]
        public async Task CreateWaifusAsync(string url, [Remainder] string name)
        {
            var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new Users {UserId = Context.User.Id});
            if (userDb.Currency < WaifuCreationPrice)
            {
                await ReplyErrorAsync("#Gambling_CurrencyNotEnough", Credentials.Currency);
                return;
            }
            
            if (!Uri.TryCreate(url, UriKind.Absolute, out var imageUri))
            {
                await ReplyErrorAsync("#Utility_UrlNotValid");
                Context.Command.ResetCooldowns();
                return;
            }

            if (imageUri.Scheme != Uri.UriSchemeHttps)
            {
                await ReplyErrorAsync("#Utility_UrlNotHttps");
                Context.Command.ResetCooldowns();
                return;
            }

            using var result = await _httpClient.GetAsync(imageUri);
            if (!result.IsSuccessStatusCode)
            {
                await ReplyErrorAsync("#Utility_ImageOrUrlNotGood");
                Context.Command.ResetCooldowns();
                return;
            }

            await using var imageStream = await result.Content.ReadAsStreamAsync();

            if (!(RiasUtils.IsPng(imageStream) || RiasUtils.IsJpg(imageStream)))
            {
                await ReplyErrorAsync("#Utility_UrlNotPngJpgGif");
                return;
            }
            
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = name,
                Description = GetText("CreationConfirmation", name),
                ThumbnailUrl = url
            };

            await ReplyAsync(embed);

            var message = await _interactive.NextMessageAsync(Context.Message);
            if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync("CreationCanceled");
                return;
            }
            
            userDb.Currency -= WaifuCreationPrice;
            var waifus = (await DbContext.GetListAsync<Waifus, Characters, CustomCharacters>
            (
                x => x.UserId == Context.User.Id,
                x => x.Character!,
                x => x.CustomCharacter!
            )).Cast<IWaifus>().ToList();
            waifus.AddRange(await DbContext.GetListAsync<CustomWaifus>(x => x.UserId == Context.User.Id));

            await DbContext.AddAsync(new CustomWaifus
            {
                UserId = Context.User.Id,
                Name = name,
                ImageUrl = url,
                IsSpecial = true,
                Position = waifus.Count != 0 ? waifus.Max(x => x.Position) + 1 : 1
            });

            await DbContext.SaveChangesAsync();
            embed.Description = GetText("Created", name);
            await ReplyAsync(embed);
        }
        
        private async Task<IWaifus?> GetWaifuAsync(string name)
        {
            var waifus = (await DbContext.GetListAsync<Waifus, Characters, CustomCharacters>
            (
                x => x.UserId == Context.User.Id,
                x => x.Character!,
                x => x.CustomCharacter!
            )).Cast<IWaifus>().ToList();
            waifus.AddRange(await DbContext.GetListAsync<CustomWaifus>(x => x.UserId == Context.User.Id));
            
            IWaifus? waifu;
            if (name.StartsWith("@") && int.TryParse(name[1..], out var id))
            {
                waifu = waifus.FirstOrDefault(x => x is Waifus normalWaifu && normalWaifu.CustomCharacterId == id);
                if (waifu != null)
                    return waifu;
            }

            if (int.TryParse(name, out id))
            {
                waifu = waifus.FirstOrDefault(x => x is Waifus normalWaifu && normalWaifu.CharacterId == id);
                if (waifu != null)
                    return waifu;
            }
            
            waifu = waifus.FirstOrDefault(x =>
                name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .All(y => x.Name!.Contains(y, StringComparison.InvariantCultureIgnoreCase)));
            
            return waifu;
        }

        private string StringifyWaifu(IWaifus waifu)
        {
            if (!(waifu is Waifus normalWaifu))
                return $"[{waifu.Name}]({waifu.ImageUrl})\n" +
                       $"{GetText("Position")}: {waifu.Position}";
                
            return $"[{normalWaifu.Name}]({normalWaifu.Character?.Url ?? normalWaifu.ImageUrl})\n" +
                   $"{GetText("#Common_Id")}: {(normalWaifu.CharacterId != null ? normalWaifu.CharacterId.ToString() : $"@{normalWaifu.CustomCharacterId}")}" +
                   $" | {GetText("#Utility_Price")}: {normalWaifu.Price} {Credentials.Currency}" +
                   $" | {GetText("Position")}: {normalWaifu.Position}";
        }
    }
}