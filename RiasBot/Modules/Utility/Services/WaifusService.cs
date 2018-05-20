using Discord;
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
        private readonly DbService _db;

        public WaifusService(DbService db)
        {
            _db = db;
        }

        public async Task ClaimWaifu(IGuildUser user, IMessageChannel channel, dynamic obj, WaifusCommands baseModule)
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

            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(x => x.UserId == user.Id).FirstOrDefault();
                var waifuDb = db.Waifus.Where(x => x.UserId == user.Id);
                var waifus = db.Waifus.Where(x => x.WaifuId == waifuId);
                try
                {
                    int waifuPrice = 1000;
                    if (waifus != null)
                    {
                        waifuPrice += waifus.Count() * 10;
                    }
                    if (waifuPrice > 10000)
                        waifuPrice = 10000;

                    var waifuEmbed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                    waifuEmbed.WithTitle(waifuName);
                    waifuEmbed.WithDescription("Do you want to claim thim waifu? Type `confirm` or `cancel`");
                    waifuEmbed.AddField("Claimed by", $"{waifus?.Count() ?? 0} users", true).AddField("Price", waifuPrice, true);
                    waifuEmbed.WithThumbnailUrl(waifuPicture);
                    var waifuClaimMsg = await channel.SendMessageAsync("", embed: waifuEmbed.Build());

                    string input = await baseModule.GetUserInputAsync(user.Id, channel.Id, 30 * 1000);
                    if (!String.IsNullOrEmpty(input))
                    {
                        if (input != "confirm")
                        {
                            await waifuClaimMsg.DeleteAsync(new RequestOptions
                            {
                                Timeout = 1000
                            });
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }

                    if (userDb.Currency < waifuPrice)
                    {
                        await channel.SendErrorEmbed($"{user.Mention} you don't have enough {RiasBot.currency}.");
                        return;
                    }
                    if (waifuDb.Any(x => x.WaifuId == waifuId))
                    {
                        await channel.SendErrorEmbed($"{user.Mention} you already claimed this waifu.");
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

                    var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                    embed.WithDescription($"Congratulations!\nYou successfully claimed {Format.Bold(waifuName)} for {Format.Bold(waifuPrice.ToString())} {RiasBot.currency}");
                    embed.WithThumbnailUrl(waifuPicture);
                    await channel.SendMessageAsync("", embed: embed.Build());
                }
                catch
                {

                }
            }
        }
    }
}
