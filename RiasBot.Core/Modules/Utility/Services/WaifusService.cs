using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RiasBot.Modules.Utility.Utility;

namespace RiasBot.Modules.Utility.Services
{
    public class WaifusService : IRService
    {
        private readonly InteractiveService _is;
        private readonly DbService _db;

        public WaifusService(InteractiveService interactiveService, DbService db)
        {
            _is = interactiveService;
            _db = db;
        }

        public async Task ClaimWaifu(ShardedCommandContext context, IGuildUser user, IMessageChannel channel, dynamic obj)
        {
            var waifuId = (int)obj.id;

            var firstName = (string) obj.name.first;
            var lastName = (string) obj.name.last;
            var waifuName = "";

            if (string.IsNullOrEmpty(firstName))
                waifuName = lastName.Trim();
            else if (string.IsNullOrEmpty(lastName))
                waifuName = firstName.Trim();
            else
                waifuName = $"{firstName.Trim()} {lastName.Trim()}";

            var waifuUrl = (string)obj.siteUrl;
            var waifuPicture = (string)obj.image.large;

            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
                var waifuDb = db.Waifus.Where(x => x.UserId == user.Id);
                var waifus = db.Waifus.Where(x => x.WaifuId == waifuId);
                try
                {
                    var waifuPrice = 1000;
                    if (waifus != null)
                    {
                        waifuPrice += waifus.Count() * 10;
                    }
                    if (waifuPrice > 10000)
                        waifuPrice = 10000;

                    var waifuEmbed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    waifuEmbed.WithTitle(waifuName);
                    waifuEmbed.WithDescription("Do you want to claim this waifu? Type `confirm` or `cancel`");
                    waifuEmbed.AddField("Claimed by", $"{waifus?.Count() ?? 0} users", true).AddField("Price", waifuPrice, true);
                    waifuEmbed.WithThumbnailUrl(waifuPicture);
                    var waifuClaimMsg = await channel.SendMessageAsync("", embed: waifuEmbed.Build());

                    var input = await _is.NextMessageAsync(context, timeout: TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                    if (input != null)
                    {
                        if (input.Content != "confirm")
                        {
                            await channel.SendErrorMessageAsync("Claim waifu canceled!").ConfigureAwait(false);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }

                    if (userDb.Currency < waifuPrice)
                    {
                        await channel.SendErrorMessageAsync($"{user.Mention} you don't have enough {RiasBot.Currency}.");
                        return;
                    }
                    if (waifuDb.Any(x => x.WaifuId == waifuId))
                    {
                        await channel.SendErrorMessageAsync($"{user.Mention} you already claimed this waifu.");
                        return;
                    }

                    userDb.Currency -= waifuPrice;

                    var waifu = new Waifus
                    {
                        UserId = user.Id,
                        WaifuId = waifuId,
                        WaifuName = waifuName,
                        WaifuUrl = waifuUrl,
                        WaifuPicture = waifuPicture,
                        WaifuPrice = waifuPrice
                    };
                    await db.AddAsync(waifu).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);

                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithDescription($"Congratulations!\nYou successfully claimed {Format.Bold(waifuName)} for {Format.Bold(waifuPrice.ToString())} {RiasBot.Currency}");
                    embed.WithThumbnailUrl(waifuPicture);
                    await channel.SendMessageAsync("", embed: embed.Build());
                }
                catch
                {
                    //ignored
                }
            }
        }
    }
}
