using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
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
    public class Waifu : RiasModule<WaifuService>
    {
        private readonly AnimeService _animeService;
        private readonly GamblingService _gamblingService;
        private readonly InteractiveService _interactive;
        private readonly HttpClient _httpClient;
        
        public Waifu(IServiceProvider services) : base(services)
        {
            _animeService = services.GetRequiredService<AnimeService>();
            _gamblingService = services.GetRequiredService<GamblingService>();
            _interactive = services.GetRequiredService<InteractiveService>();
            _httpClient = services.GetRequiredService<HttpClient>();
        }

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

            var waifus = Service.GetUserWaifus(Context.User);
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
            
            var waifuUsers = Service.GetWaifuUsers(character.CharacterId, character is CustomCharacters);
            var waifuPrice = WaifuService.WaifuStartingPrice + waifuUsers.Count * 10;
            
            if (!claimCanceled && _gamblingService.GetUserCurrency(Context.User) < waifuPrice)
            {
                embed.WithDescription(GetText("ClaimCurrencyNotEnough", Creds.Currency));
                claimCanceled = true;
            }

            if (!claimCanceled)
            {
                embed.WithDescription(GetText("ClaimConfirmation"));
            }

            embed.AddField(GetText("ClaimedBy"), $"{waifuUsers.Count} {GetText("#Common_Users").ToLowerInvariant()}", true)
                .AddField(GetText("#Utility_Price"), waifuPrice, true);
            
            await Context.Channel.SendMessageAsync(Format.Bold(GetText("ClaimNote", GetPrefix())), embed: embed.Build());

            if (claimCanceled)
                return;

            var message = await _interactive.NextMessageAsync(Context.Message);
            if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
            {
                await ReplyErrorAsync("ClaimCanceled");
                return;
            }

            await _gamblingService.RemoveUserCurrencyAsync(Context.User, waifuPrice);
            await Service.AddWaifuAsync(Context.User, character, waifuPrice, waifus.Max(x => x.Position) + 1);

            embed.WithDescription(GetText("WaifuClaimed", character.Name!, waifuPrice, Creds.Currency));
            embed.Fields.Clear();

            await ReplyAsync(embed);
        }

        [Command("divorce"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task DivorceAsync([Remainder] string name)
        {
            var waifu = Service.GetWaifu(Context.User, name);
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

            await Service.RemoveWaifuAsync(Context.User, waifu);
            await ReplyConfirmationAsync("Divorced", waifu.Name!);
        }

        [Command("all"), Context(ContextType.Guild),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task AllWaifusAsync([Remainder] SocketGuildUser? user = null)
        {
            user ??= (SocketGuildUser) Context.User;

            var allWaifus = Service.GetUserWaifus(Context.User);
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

            var waifus = allWaifus.Where(x => !x.IsSpecial && x.Position != 0)
                .OrderBy(x => x.Position)
                .Concat(allWaifus.Where(x => x.Position == 0))
                .ToList();

            if (waifus.Count == 0)
            {
                var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = user.Id == Context.User.Id ? GetText("AllWaifus") : GetText("AllUserWaifus"),
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
                    Title = user.Id == Context.User.Id ? GetText("AllWaifus") : GetText("AllUserWaifus"),
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
            var waifu = Service.GetWaifu(Context.User, name);
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
            
            if (_gamblingService.GetUserCurrency(Context.User) < WaifuService.SpecialWaifuPrice)
            {
                await ReplyErrorAsync("#Gambling_CurrencyNotEnough", Creds.Currency);
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
            
            await _gamblingService.RemoveUserCurrencyAsync(Context.User, WaifuService.SpecialWaifuPrice);
            await Service.SetSpecialWaifuAsync(Context.User, waifu);
            await ReplyConfirmationAsync("Special", waifu.Name!);
        }

        [Command("image"), Context(ContextType.Guild),
         Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
        public async Task WaifuImageAsync(string url, [Remainder] string name)
        {
            var waifu = Service.GetWaifu(Context.User, name);
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

            await Service.SetWaifuImageAsync(Context.User, waifu, url);
            await ReplyConfirmationAsync("ImageSet", waifu.Name!);
        }

        [Command("position"), Context(ContextType.Guild)]
        public async Task WaifuPositionAsync(int position, [Remainder] string name)
        {
            var waifu = Service.GetWaifu(Context.User, name);
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
            
            if (position > WaifuService.WaifuPositionLimit)
            {
                await ReplyErrorAsync("PositionHigherLimit", WaifuService.WaifuPositionLimit);
                return;
            }

            if (waifu.Position == position)
            {
                await ReplyErrorAsync("HasPosition", waifu.Name!, position);
                return;
            }

            position = await Service.SetWaifuPositionAsync(Context.User, waifu, position);
            await ReplyConfirmationAsync("PositionSet", waifu.Name!, position);
        }

        [Command("create"), Context(ContextType.Guild),
         Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.User)]
        public async Task CreateWaifusAsync(string url, [Remainder] string name)
        {
            if (_gamblingService.GetUserCurrency(Context.User) < WaifuService.WaifuCreationPrice)
            {
                await ReplyErrorAsync("#Gambling_CurrencyNotEnough", Creds.Currency);
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
            
            await _gamblingService.RemoveUserCurrencyAsync(Context.User, WaifuService.WaifuCreationPrice);
            await Service.CreateWaifuAsync(Context.User, name, url);

            embed.Description = GetText("Created", name);
            await ReplyAsync(embed);
        }

        private string StringifyWaifu(IWaifus waifu)
        {
            if (!(waifu is Waifus normalWaifu))
                return $"[{waifu.Name}]({waifu.ImageUrl})\n" +
                       $"{GetText("Position")}: {waifu.Position}";
                
            return $"[{normalWaifu.Name}]({normalWaifu.Character?.Url ?? normalWaifu.ImageUrl})\n" +
                   $"{GetText("#Common_Id")}: {(normalWaifu.CharacterId != null ? normalWaifu.CharacterId.ToString() : $"@{normalWaifu.CustomCharacterId}")}" +
                   $" | {GetText("#Utility_Price")}: {normalWaifu.Price} {Creds.Currency}" +
                   $" | {GetText("Position")}: {normalWaifu.Position}";
        }
    }
}