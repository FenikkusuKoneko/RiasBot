using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Gambling
{
    public partial class Gambling
    {
        [Name("Currency")]
        public class Currency : RiasModule<GamblingService>
        {
            private readonly DiscordShardedClient _client;

            public Currency(IServiceProvider services) : base(services)
            {
                _client = services.GetRequiredService<DiscordShardedClient>();
            }

            [Command("currency"), Context(ContextType.Guild)]
            public async Task CurrencyAsync([Remainder] SocketGuildUser? user = null)
            {
                var currency = Service.GetUserCurrency(user ?? Context.User);
                if (user is null || user == Context.User)
                    await ReplyConfirmationAsync("CurrencyYou", currency, Creds.Currency);
                else
                    await ReplyConfirmationAsync("CurrencyUser", user, currency, Creds.Currency);
            }

            [Command("reward"), OwnerOnly]
            public async Task RewardAsync(int amount, [Remainder] string value)
            {
                var user = await RiasUtils.GetUserFromSocketOrRestAsync(Context.Client, value);
                if (user is null)
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }

                var currency = await Service.AddUserCurrencyAsync(user.Id, amount);
                await ReplyConfirmationAsync("UserRewarded", amount, Creds.Currency, user, currency);
            }
            
            [Command("take"), OwnerOnly]
            public async Task TakeAsync(int amount, [Remainder] string value)
            {
                var user = await RiasUtils.GetUserFromSocketOrRestAsync(Context.Client, value);
                if (user is null)
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }

                var currency = await Service.RemoveUserCurrencyAsync(user, amount);
                await ReplyConfirmationAsync("UserTook", currency, Creds.Currency, user);
            }

            [Command("leaderboard"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task LeaderboardAsync(int page = 1)
            {
                page--;
                if (page < 0) page = 0;

                var usersCurrency = Service.GetUsersCurrency(page, 9);
                if (usersCurrency.Count == 0)
                {
                    await ReplyErrorAsync("LeaderboardNoUsers");
                    return;
                }

                var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = GetText("CurrencyLeaderboard", Creds.Currency)
                };

                var index = 0;
                foreach (var userCurrency in usersCurrency)
                {
                    var user = _client.GetUser(userCurrency.UserId);
                    embed.AddField($"#{++index + page * 9} {(user != null ? user.ToString() : userCurrency.UserId.ToString())}",
                        $"{userCurrency.Currency} {Creds.Currency}", true);
                }

                await ReplyAsync(embed);
            }

            [Command("daily"), Context(ContextType.Guild)]
            public async Task DailyAsync()
            {
                var userDb = Service.GetUser(Context.User);
                var timeNow = DateTime.UtcNow;
                if (userDb != null)
                {
                    var nextDaily = userDb.DailyTaken.AddDays(1);
                    if (nextDaily > timeNow)
                    {
                        var timeLeftHumanized = (nextDaily - timeNow).Humanize(3, Resources.GetGuildCulture(Context.Guild!.Id), minUnit: TimeUnit.Second);
                        await ReplyErrorAsync("DailyWait", timeLeftHumanized);
                        return;
                    }
                }

                await Service.AddUserCurrencyAsync(Context.User.Id, 100);
                await Service.UpdateDailyAsync(Context.User, timeNow);

                await ReplyConfirmationAsync("DailyReceived", 100, Creds.Currency);
            }
        }
    }
}