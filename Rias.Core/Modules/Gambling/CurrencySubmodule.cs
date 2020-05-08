using System;
using System.Globalization;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Humanizer.Localisation;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Gambling
{
    public partial class GamblingModule
    {
        [Name("Currency")]
        public class CurrencySubmodule : RiasModule
        {
            public CurrencySubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("currency"), Context(ContextType.Guild)]
            public async Task CurrencyAsync([Remainder] CachedMember? user = null)
            {
                user ??= (CachedMember) Context.User;
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                if (user is null || user.Id == Context.User.Id)
                {
                    if (string.IsNullOrEmpty(Credentials.DiscordBotList))
                        await ReplyConfirmationAsync(Localization.GamblingCurrencyYou, userDb.Currency, Credentials.Currency);
                    else
                        await ReplyConfirmationAsync(Localization.GamblingCurrencyYouVote, userDb.Currency, Credentials.Currency, $"{Credentials.DiscordBotList}/vote");
                }
                else
                {
                    await ReplyConfirmationAsync(Localization.GamblingCurrencyYouVote, user, userDb.Currency, Credentials.Currency);
                }
            }
            
            [Command("reward"), OwnerOnly]
            public async Task RewardAsync(int amount, [Remainder] IUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                var currency = userDb.Currency += amount;

                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.GamblingUserRewarded, amount, Credentials.Currency, user, currency);
            }
            
            [Command("take"), OwnerOnly]
            public async Task TakeAsync(int amount, [Remainder] IUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                amount = Math.Min(amount, userDb.Currency);
                userDb.Currency -= amount;

                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.GamblingUserTook, amount, Credentials.Currency, user);
            }
            
            [Command("leaderboard"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task LeaderboardAsync(int page = 1)
            {
                page--;
                if (page < 0) page = 0;
                
                var usersCurrency = await DbContext.GetOrderedListAsync<UsersEntity, int>(x => x.Currency, true, (page * 15)..((page + 1) * 15));
                if (usersCurrency.Count == 0)
                {
                    await ReplyErrorAsync(Localization.GamblingLeaderboardNoUsers);
                    return;
                }

                var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.GamblingCurrencyLeaderboard, Credentials.Currency)
                };

                var index = 0;
                foreach (var userCurrency in usersCurrency)
                {
                    var user = (IUser) RiasBot.GetUser(userCurrency.UserId) ?? await RiasBot.GetUserAsync(userCurrency.UserId);
                    embed.AddField($"#{++index + page * 15} {(user != null ? user.ToString() : userCurrency.UserId.ToString())}",
                        $"{userCurrency.Currency} {Credentials.Currency}", true);
                }

                await ReplyAsync(embed);
            }
            
            [Command("daily"), Context(ContextType.Guild)]
            public async Task DailyAsync()
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new UsersEntity {UserId = Context.User.Id});
                var timeNow = DateTime.UtcNow;
                var nextDaily = userDb.DailyTaken.AddDays(1);
                if (nextDaily > timeNow)
                {
                    var timeLeftHumanized = (nextDaily - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), minUnit: TimeUnit.Second);
                    if (string.IsNullOrEmpty(Credentials.DiscordBotList))
                        await ReplyErrorAsync(Localization.GamblingDailyWait, timeLeftHumanized);
                    else
                        await ReplyErrorAsync(Localization.GamblingDailyWaitVote, timeLeftHumanized, $"{Credentials.DiscordBotList}/vote", Credentials.Currency);
                    return;
                }

                userDb.Currency += 100;
                userDb.DailyTaken = timeNow;

                await DbContext.SaveChangesAsync();
                if (string.IsNullOrEmpty(Credentials.DiscordBotList))
                    await ReplyConfirmationAsync(Localization.GamblingDailyReceived, 100, Credentials.Currency, userDb.Currency);
                else
                    await ReplyConfirmationAsync(Localization.GamblingDailyReceivedVote, 100, Credentials.Currency, userDb.Currency, $"{Credentials.DiscordBotList}/vote");
            }
        }
    }
}