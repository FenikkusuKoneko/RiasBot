using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Modules.Waifu
{
    [Name("Waifu")]
    public partial class WaifuModule : RiasModule
    {
        public WaifuModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        [Command("waifus")]
        [Context(ContextType.Guild)]
        [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task AllWaifusAsync([Remainder] DiscordMember? member = null)
        {
            member ??= (DiscordMember) Context.User;

            var allWaifus = (await DbContext.GetListAsync<WaifuEntity, CharacterEntity, CustomCharacterEntity>(
                    x => x.UserId == member.Id,
                    x => x.Character!,
                    x => x.CustomCharacter!))
                .ToList<IWaifuEntity>();
            
            allWaifus.AddRange(await DbContext.GetListAsync<CustomWaifuEntity>(x => x.UserId == member.Id));
            
            if (allWaifus.Count == 0)
            {
                if (member.Id == Context.User.Id)
                    await ReplyErrorAsync(Localization.WaifuNoWaifus);
                else
                    await ReplyErrorAsync(Localization.WaifuMemberNoWaifus, member.FullName());
                
                return;
            }

            var specialWaifu = allWaifus.FirstOrDefault(x => x.IsSpecial);
            var specialWaifuString = string.Empty;
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

            var specialWaifuImage = specialWaifu is WaifuEntity waifuEntity && !string.IsNullOrEmpty(waifuEntity.CustomImageUrl)
                ? waifuEntity.CustomImageUrl
                : specialWaifu?.ImageUrl;

            if (waifus.Count == 0)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = member.Id == Context.User.Id ? GetText(Localization.WaifuAllWaifus) : GetText(Localization.WaifuAllMemberWaifus, member.FullName()),
                    Description = specialWaifuString,
                }.WithThumbnail(specialWaifuImage);

                await ReplyAsync(embed);
                return;
            }

            await SendPaginatedMessageAsync(waifus, 10, (items, _) => new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = member.Id == Context.User.Id ? GetText(Localization.WaifuAllWaifus) : GetText(Localization.WaifuAllMemberWaifus, member.FullName()),
                Description = $"{specialWaifuString}\n\n{string.Join("\n\n", items.Select(StringifyWaifu))}",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = GetText(Localization.WaifuWaifusNumber, specialWaifu != null ? waifus.Count + 1 : waifus.Count)
                }
            }.WithThumbnail(specialWaifuImage));
        }

        private string StringifyWaifu(IWaifuEntity waifu)
        {
            if (waifu is not WaifuEntity normalWaifu)
                return $"[{waifu.Name}]({waifu.ImageUrl})\n" +
                       $"{GetText(Localization.WaifuPosition)}: {waifu.Position}";
                
            return $"[{normalWaifu.Name}]({normalWaifu.Character?.Url ?? normalWaifu.ImageUrl})\n" +
                   $"{GetText(Localization.CommonId)}: {(normalWaifu.CharacterId != null ? normalWaifu.CharacterId.ToString() : $"w{normalWaifu.CustomCharacterId}")}" +
                   $" | {GetText(Localization.UtilityPrice)}: {normalWaifu.Price} {Configuration.Currency}" +
                   $" | {GetText(Localization.WaifuPosition)}: {normalWaifu.Position}";
        }
    }
}