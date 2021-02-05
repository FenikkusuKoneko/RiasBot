using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Waifu
{
    public partial class WaifuModule
    {
        [Name("Waifu")]
        [Group("waifu")]
        public class WaifuGroupModule : RiasModule
        {
            private const int WaifuStartingPrice = 1000;
            private const int SpecialWaifuPrice = 5000;
            private const int WaifuCreationPrice = 10000; 

            private const int WaifuPositionLimit = 1000;
            
            private readonly AnimeService _animeService;
            private readonly HttpClient _httpClient;
        
            public WaifuGroupModule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
                _animeService = serviceProvider.GetRequiredService<AnimeService>();
                _httpClient = serviceProvider.GetRequiredService<HttpClient>();
            }
            
            [Command("claim")]
            [Context(ContextType.Guild)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task ClaimAsync([Remainder] string name)
            {
                var character = await _animeService.GetOrAddCharacterAsync(name);
                if (character is null)
                {
                    await ReplyErrorAsync(Localization.SearchesCharacterNotFound);
                    return;
                }

                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Url = character is CharacterEntity animeCharacter ? animeCharacter.Url : default,
                    Title = character.Name,
                }.WithThumbnail(character.ImageUrl);

                var claimCanceled = false;

                var waifus = (await DbContext.GetListAsync<WaifuEntity, CharacterEntity, CustomCharacterEntity>(
                        x => x.UserId == Context.User.Id,
                        x => x.Character!,
                        x => x.CustomCharacter!))
                    .ToList<IWaifuEntity>();
            
                waifus.AddRange(await DbContext.GetListAsync<CustomWaifuEntity>(x => x.UserId == Context.User.Id));
            
                var waifu = character switch
                {
                    CustomCharacterEntity => waifus.FirstOrDefault(x => x is WaifuEntity normalWaifu && normalWaifu.CustomCharacterId == character.CharacterId),
                    _ => waifus.FirstOrDefault(x => x is WaifuEntity normalWaifu && normalWaifu.CharacterId == character.CharacterId)
                };
            
                if (waifu != null)
                {
                    embed.WithDescription(GetText(Localization.WaifuHasWaifu));
                    claimCanceled = true;
                }
            
                var waifuUsers = character is CustomCharacterEntity
                    ? DbContext.Waifus.Where(x => x.CustomCharacterId == character.CharacterId).ToList()
                    : DbContext.Waifus.Where(x => x.CharacterId == character.CharacterId).ToList();
            
                var waifuPrice = WaifuStartingPrice + waifuUsers.Count * 10;
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new UserEntity { UserId = Context.User.Id });
                if (!claimCanceled && userDb.Currency < waifuPrice)
                {
                    embed.WithDescription(GetText(Localization.WaifuClaimCurrencyNotEnough, Configuration.Currency));
                    claimCanceled = true;
                }

                if (!claimCanceled)
                {
                    embed.WithDescription(GetText(Localization.WaifuClaimConfirmation));
                }

                embed.AddField(GetText(Localization.WaifuClaimedBy), $"{waifuUsers.Count} {GetText(Localization.CommonUsers).ToLowerInvariant()}", true)
                    .AddField(GetText(Localization.UtilityPrice), waifuPrice.ToString(), true);
            
                await Context.Channel.SendMessageAsync($"**{GetText(Localization.WaifuClaimNote, Context.Prefix)}**", embed);

                if (claimCanceled)
                    return;

                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived.Result?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.WaifuClaimCanceled);
                    return;
                }
            
                userDb.Currency -= waifuPrice;
                var waifuDb = new WaifuEntity
                {
                    UserId = Context.User.Id,
                    Price = waifuPrice,
                    Position = waifus.Count != 0 ? waifus.Max(x => x.Position) + 1 : 1
                };

                if (character is CustomCharacterEntity)
                    waifuDb.CustomCharacterId = character.CharacterId;
                else
                    waifuDb.CharacterId = character.CharacterId;

                await DbContext.AddAsync(waifuDb);
                await DbContext.SaveChangesAsync();

                embed.WithDescription(GetText(Localization.WaifuWaifuClaimed, character.Name!, waifuPrice, Configuration.Currency));
                embed.ClearFields();

                await ReplyAsync(embed);
            }

            [Command("divorce")]
            [Context(ContextType.Guild)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task DivorceAsync([Remainder] string name)
            {
                var waifu = await GetWaifuAsync(name);
                if (waifu is null)
                {
                    await ReplyErrorAsync(Localization.WaifuNotFound);
                    return;
                }

                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = waifu.Name,
                    Description = GetText(Localization.WaifuDivorceConfirmation, waifu.Name!),
                }.WithThumbnail(waifu.ImageUrl);

                await ReplyAsync(embed);
            
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived.Result?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.WaifuDivorceCanceled);
                    return;
                }

                if (waifu is CustomWaifuEntity customWaifu)
                    DbContext.Remove(customWaifu);
                else
                    DbContext.Remove((WaifuEntity) waifu);
            
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.WaifuDivorced, waifu.Name!);
            }

            [Command("special", "beloved")]
            [Context(ContextType.Guild)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task SpecialWaifuAsync([Remainder] string name)
            {
                var waifu = await GetWaifuAsync(name);
                if (waifu is null)
                {
                    await ReplyErrorAsync(Localization.WaifuNotFound);
                    return;
                }

                if (waifu.IsSpecial)
                {
                    await ReplyErrorAsync(Localization.WaifuAlreadySpecial, waifu.Name!);
                    return;
                }
            
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new UserEntity { UserId = Context.User.Id });
                if (userDb.Currency < SpecialWaifuPrice)
                {
                    await ReplyErrorAsync(Localization.GamblingCurrencyNotEnough, Configuration.Currency);
                    return;
                }
            
                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = waifu.Name,
                    Description = GetText(Localization.WaifuSpecialConfirmation, waifu.Name!),
                }.WithThumbnail(waifu.ImageUrl);

                await ReplyAsync(embed);

                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived.Result?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.WaifuSpecialCanceled);
                    return;
                }
            
                userDb.Currency -= SpecialWaifuPrice;
                var currentSpecialWaifu = (IWaifuEntity) await DbContext.Waifus.FirstOrDefaultAsync(x => x.UserId == Context.User.Id && x.IsSpecial)
                                          ?? await DbContext.CustomWaifus.FirstOrDefaultAsync(x => x.UserId == Context.User.Id && x.IsSpecial);

                if (currentSpecialWaifu != null)
                {
                    currentSpecialWaifu.IsSpecial = false;
                    if (currentSpecialWaifu is WaifuEntity currentWaifuDb)
                        currentWaifuDb.CustomImageUrl = null;
                }

                waifu.IsSpecial = true;
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.WaifuSpecial, waifu.Name!);
            }

            [Command("image", "avatar")]
            [Context(ContextType.Guild)]
            [Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.User)]
            public async Task WaifuImageAsync(string url, [Remainder] string name)
            {
                var waifu = await GetWaifuAsync(name);
                if (waifu is null)
                {
                    await ReplyErrorAsync(Localization.WaifuNotFound);
                    Context.Command.ResetCooldowns();
                    return;
                }

                if (!(waifu.IsSpecial || waifu is CustomWaifuEntity))
                {
                    await ReplyErrorAsync(Localization.WaifuImageSetError);
                    Context.Command.ResetCooldowns();
                    return;
                }
            
                if (!Uri.TryCreate(url, UriKind.Absolute, out var imageUri))
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotValid);
                    Context.Command.ResetCooldowns();
                    return;
                }

                if (imageUri.Scheme != Uri.UriSchemeHttps)
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotHttps);
                    Context.Command.ResetCooldowns();
                    return;
                }

                using var result = await _httpClient.GetAsync(imageUri);
                if (!result.IsSuccessStatusCode)
                {
                    await ReplyErrorAsync(Localization.UtilityImageOrUrlNotGood);
                    Context.Command.ResetCooldowns();
                    return;
                }

                await using var imageStream = await result.Content.ReadAsStreamAsync();

                if (!(RiasUtilities.IsPng(imageStream) || RiasUtilities.IsJpg(imageStream)))
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotPngJpg);
                    return;
                }

                if (waifu is WaifuEntity normalWaifuDb)
                    normalWaifuDb.CustomImageUrl = url;
                else
                    ((CustomWaifuEntity) waifu).ImageUrl = url;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.WaifuImageSet, waifu.Name!);
            }

            [Command("position")]
            [Context(ContextType.Guild)]
            public async Task WaifuPositionAsync(int position, [Remainder] string name)
            {
                var waifu = await GetWaifuAsync(name);
                if (waifu is null)
                {
                    await ReplyErrorAsync(Localization.WaifuNotFound);
                    Context.Command.ResetCooldowns();
                    return;
                }

                if (position < 1)
                {
                    await ReplyErrorAsync(Localization.WaifuPositionLowerLimit);
                    return;
                }
            
                if (position > WaifuPositionLimit)
                {
                    await ReplyErrorAsync(Localization.WaifuPositionHigherLimit, WaifuPositionLimit);
                    return;
                }

                if (waifu.Position == position)
                {
                    await ReplyErrorAsync(Localization.WaifuHasPosition, waifu.Name!, position);
                    return;
                }
            
                var waifus = (await DbContext.GetListAsync<WaifuEntity, CharacterEntity, CustomCharacterEntity>(
                        x => x.UserId == Context.User.Id,
                        x => x.Character!,
                        x => x.CustomCharacter!))
                    .ToList<IWaifuEntity>();
            
                waifus.AddRange(await DbContext.GetListAsync<CustomWaifuEntity>(x => x.UserId == Context.User.Id));
            
                waifus = waifus.Where(x => x.Position != 0)
                    .OrderBy(x => x.Position)
                    .Concat(waifus.Where(x => x.Position == 0))
                    .ToList();

                var currentWaifu = waifus.FirstOrDefault(x => x.GetType().IsInstanceOfType(waifu) && x.Id == waifu.Id);
                if (currentWaifu is not null)
                    waifus.Remove(currentWaifu);
            
                position = Math.Min(position, waifus.Count + 1);
                if (currentWaifu is not null)
                    waifus.Insert(position - 1, currentWaifu);
            
                for (var i = 0; i < waifus.Count; i++)
                    waifus[i].Position = i + 1;
            
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.WaifuPositionSet, waifu.Name!, position);
            }

            [Command("create")]
            [Context(ContextType.Guild)]
            [Cooldown(1, 60, CooldownMeasure.Seconds, BucketType.User)]
            public async Task CreateWaifusAsync(string url, [Remainder] string name)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new UserEntity { UserId = Context.User.Id });
                if (userDb.Currency < WaifuCreationPrice)
                {
                    await ReplyErrorAsync(Localization.GamblingCurrencyNotEnough, Configuration.Currency);
                    return;
                }
            
                if (!Uri.TryCreate(url, UriKind.Absolute, out var imageUri))
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotValid);
                    Context.Command.ResetCooldowns();
                    return;
                }

                if (imageUri.Scheme != Uri.UriSchemeHttps)
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotHttps);
                    Context.Command.ResetCooldowns();
                    return;
                }

                using var result = await _httpClient.GetAsync(imageUri);
                if (!result.IsSuccessStatusCode)
                {
                    await ReplyErrorAsync(Localization.UtilityImageOrUrlNotGood);
                    Context.Command.ResetCooldowns();
                    return;
                }

                await using var imageStream = await result.Content.ReadAsStreamAsync();

                if (!(RiasUtilities.IsPng(imageStream) || RiasUtilities.IsJpg(imageStream)))
                {
                    await ReplyErrorAsync(Localization.UtilityUrlNotPngJpg);
                    return;
                }
            
                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = name,
                    Description = GetText(Localization.WaifuCreationConfirmation, name),
                }.WithThumbnail(url);

                await ReplyAsync(embed);

                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived.Result?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.WaifuCreationCanceled);
                    return;
                }
            
                userDb.Currency -= WaifuCreationPrice;
                var currentSpecialWaifu = (IWaifuEntity) await DbContext.Waifus.FirstOrDefaultAsync(x => x.UserId == Context.User.Id && x.IsSpecial)
                                          ?? await DbContext.CustomWaifus.FirstOrDefaultAsync(x => x.UserId == Context.User.Id && x.IsSpecial);

                if (currentSpecialWaifu != null)
                {
                    currentSpecialWaifu.IsSpecial = false;
                    if (currentSpecialWaifu is WaifuEntity currentWaifuDb)
                        currentWaifuDb.CustomImageUrl = null;
                }
            
                var waifus = (await DbContext.GetListAsync<WaifuEntity, CharacterEntity, CustomCharacterEntity>(
                        x => x.UserId == Context.User.Id,
                        x => x.Character!,
                        x => x.CustomCharacter!))
                    .ToList<IWaifuEntity>();
            
                waifus.AddRange(await DbContext.GetListAsync<CustomWaifuEntity>(x => x.UserId == Context.User.Id));

                await DbContext.AddAsync(new CustomWaifuEntity
                {
                    UserId = Context.User.Id,
                    Name = name,
                    ImageUrl = url,
                    IsSpecial = true,
                    Position = waifus.Count != 0 ? waifus.Max(x => x.Position) + 1 : 1
                });

                await DbContext.SaveChangesAsync();
                embed.Description = GetText(Localization.WaifuCreated, name);
                await ReplyAsync(embed);
            }
            
            private async Task<IWaifuEntity?> GetWaifuAsync(string name)
            {
                var waifus = (await DbContext.GetListAsync<WaifuEntity, CharacterEntity, CustomCharacterEntity>(
                        x => x.UserId == Context.User.Id,
                        x => x.Character!,
                        x => x.CustomCharacter!))
                    .ToList<IWaifuEntity>();
            
                waifus.AddRange(await DbContext.GetListAsync<CustomWaifuEntity>(x => x.UserId == Context.User.Id));
            
                IWaifuEntity? waifu;
                if (name.StartsWith("w", StringComparison.OrdinalIgnoreCase) && int.TryParse(name[1..], out var id))
                {
                    waifu = waifus.FirstOrDefault(x => x is WaifuEntity normalWaifu && normalWaifu.CustomCharacterId == id);
                    if (waifu != null)
                        return waifu;
                }

                if (int.TryParse(name, out id))
                {
                    waifu = waifus.FirstOrDefault(x => x is WaifuEntity normalWaifu && normalWaifu.CharacterId == id);
                    if (waifu != null)
                        return waifu;
                }
            
                waifu = waifus.FirstOrDefault(x =>
                    name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .All(y => x.Name!.Contains(y, StringComparison.InvariantCultureIgnoreCase)));
            
                return waifu;
            }
        }
    }
}