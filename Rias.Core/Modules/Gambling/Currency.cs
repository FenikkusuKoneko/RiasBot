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
using Rias.Core.Database.Entities;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Gambling
{
    public partial class Gambling
    {
        [Name("Currency")]
        public class Currency : RiasModule
        {
            private readonly DiscordShardedClient _client;

            public Currency(IServiceProvider services) : base(services)
            {
                _client = services.GetRequiredService<DiscordShardedClient>();
            }

            [Command("currency"), Context(ContextType.Guild)]
            public async Task CurrencyAsync([Remainder] SocketGuildUser? user = null)
            {
                user ??= (SocketGuildUser) Context.User;
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                if (user is null || user.Id == Context.User.Id)
                {
                    if (string.IsNullOrEmpty(Credentials.DiscordBotList))
                        await ReplyConfirmationAsync("CurrencyYou", userDb.Currency, Credentials.Currency);
                    else
                        await ReplyConfirmationAsync("CurrencyYouVote", userDb.Currency, Credentials.Currency, $"{Credentials.DiscordBotList}/vote");
                }
                else
                {
                    await ReplyConfirmationAsync("CurrencyUser", user, userDb.Currency, Credentials.Currency);
                }
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

                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                var currency = userDb.Currency += amount;

                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync("UserRewarded", amount, Credentials.Currency, user, currency);
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
                
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                amount = Math.Min(amount, userDb.Currency);
                userDb.Currency -= amount;

                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync("UserTook", amount, Credentials.Currency, user);
            }

            [Command("leaderboard"),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task LeaderboardAsync(int page = 1)
            {
                page--;
                if (page < 0) page = 0;
                
                var usersCurrency = await DbContext.GetOrderedListAsync<UsersEntity, int>(x => x.Currency, true, (page * 9)..((page + 1) * 9));
                if (usersCurrency.Count == 0)
                {
                    await ReplyErrorAsync("LeaderboardNoUsers");
                    return;
                }

                var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = GetText("CurrencyLeaderboard", Credentials.Currency)
                };

                var index = 0;
                foreach (var userCurrency in usersCurrency)
                {
                    var user = _client.GetUser(userCurrency.UserId);
                    embed.AddField($"#{++index + page * 9} {(user != null ? user.ToString() : userCurrency.UserId.ToString())}",
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
                    var timeLeftHumanized = (nextDaily - timeNow).Humanize(3, Resources.GetGuildCulture(Context.Guild!.Id), minUnit: TimeUnit.Second);
                    if (string.IsNullOrEmpty(Credentials.DiscordBotList))
                        await ReplyErrorAsync("DailyWait", timeLeftHumanized);
                    else
                        await ReplyErrorAsync("DailyWaitVote", timeLeftHumanized, $"{Credentials.DiscordBotList}/vote", Credentials.Currency);
                    return;
                }

                userDb.Currency += 100;
                userDb.DailyTaken = timeNow;

                await DbContext.SaveChangesAsync();
                if (string.IsNullOrEmpty(Credentials.DiscordBotList))
                    await ReplyConfirmationAsync("DailyReceived", 100, Credentials.Currency, userDb.Currency);
                else
                    await ReplyConfirmationAsync("DailyReceivedVote", 100, Credentials.Currency, userDb.Currency, $"{Credentials.DiscordBotList}/vote");
            }
        }
    }
}