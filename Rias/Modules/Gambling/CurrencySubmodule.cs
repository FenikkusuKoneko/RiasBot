using System;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Humanizer;
using Humanizer.Localisation;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Modules.Gambling
{
    public partial class GamblingModule
    {
        [Name("Currency")]
        public class CurrencySubmodule : RiasModule
        {
            public CurrencySubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            [Command("currency", "$", "hearts")]
            [Context(ContextType.Guild)]
            public async Task CurrencyAsync([Remainder] DiscordMember? member = null)
            {
                member ??= (DiscordMember) Context.User;
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == member.Id, () => new UsersEntity { UserId = member.Id });
                if (member.Id == Context.User.Id)
                {
                    var userVotesDb = await DbContext.GetOrderedListAsync<VotesEntity, DateTime>(x => x.UserId == member.Id, x => x.DateAdded, true);

                    if (string.IsNullOrEmpty(Credentials.DiscordBotList))
                    {
                        await ReplyConfirmationAsync(Localization.GamblingCurrencyYou, userDb.Currency, Credentials.Currency);
                    }
                    else
                    {
                        var timeNow = DateTime.UtcNow;
                        if (userVotesDb.Count == 0 || userVotesDb[0].DateAdded.AddHours(12) < timeNow)
                        {
                            await ReplyConfirmationAsync(Localization.GamblingCurrencyYouVote, userDb.Currency, Credentials.Currency, $"{Credentials.DiscordBotList}/vote", Credentials.Patreon);
                        }
                        else
                        {
                            var nextVoteHumanized = (userVotesDb[0].DateAdded.AddHours(12) - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), TimeUnit.Hour, TimeUnit.Second);
                            await ReplyConfirmationAsync(Localization.GamblingCurrencyYouVoted, userDb.Currency, Credentials.Currency, $"{Credentials.DiscordBotList}/vote", nextVoteHumanized, Credentials.Patreon);
                        }
                    }
                }
                else
                {
                    await ReplyConfirmationAsync(Localization.GamblingCurrencyMember, member.FullName(), userDb.Currency, Credentials.Currency);
                }
            }

            [Command("reward")]
            [OwnerOnly]
            public async Task RewardAsync(int amount, [Remainder] DiscordUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity { UserId = user.Id });
                var currency = userDb.Currency += amount;

                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.GamblingUserRewarded, amount, Credentials.Currency, user.FullName(), currency);
            }

            [Command("take")]
            [OwnerOnly]
            public async Task TakeAsync(int amount, [Remainder] DiscordUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity { UserId = user.Id });
                amount = Math.Min(amount, userDb.Currency);
                userDb.Currency -= amount;

                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.GamblingUserTook, amount, Credentials.Currency, user.FullName());
            }

            [Command("leaderboard", "lb")]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
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

                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.GamblingCurrencyLeaderboard, Credentials.Currency)
                };

                var index = 0;
                foreach (var userCurrency in usersCurrency)
                {
                    var user = await RiasBot.GetUserAsync(userCurrency.UserId);
                    embed.AddField($"#{++index + page * 15} {user?.FullName()}", $"{userCurrency.Currency} {Credentials.Currency}", true);
                }

                await ReplyAsync(embed);
            }

            [Command("daily", "dailies")]
            [Context(ContextType.Guild)]
            public async Task DailyAsync()
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new UsersEntity { UserId = Context.User.Id });
                var userVotesDb = await DbContext.GetOrderedListAsync<VotesEntity, DateTime>(x => x.UserId == Context.User.Id, x => x.DateAdded, true);
                
                var timeNow = DateTime.UtcNow;
                var nextDaily = userDb.DailyTaken.AddDays(1);
                if (nextDaily > timeNow)
                {
                    var timeLeftHumanized = (nextDaily - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), minUnit: TimeUnit.Second);
                    if (string.IsNullOrEmpty(Credentials.DiscordBotList))
                    {
                        await ReplyErrorAsync(Localization.GamblingDailyWait, timeLeftHumanized);
                    }
                    else
                    {
                        if (userVotesDb.Count == 0 || userVotesDb[0].DateAdded.AddHours(12) < timeNow)
                        {
                            await ReplyErrorAsync(Localization.GamblingDailyWaitVote, timeLeftHumanized, $"{Credentials.DiscordBotList}/vote", Credentials.Currency, Credentials.Patreon);
                        }
                        else
                        {
                            var nextVoteHumanized = (userVotesDb[0].DateAdded.AddHours(12) - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), TimeUnit.Hour, TimeUnit.Second);
                            await ReplyErrorAsync(Localization.GamblingDailyWaitVoted, timeLeftHumanized, $"{Credentials.DiscordBotList}/vote", nextVoteHumanized, Credentials.Patreon);
                        }
                    }
                    
                    return;
                }

                userDb.Currency += 100;
                userDb.DailyTaken = timeNow;

                await DbContext.SaveChangesAsync();
                if (string.IsNullOrEmpty(Credentials.DiscordBotList))
                {
                    await ReplyConfirmationAsync(Localization.GamblingDailyReceived, 100, Credentials.Currency, userDb.Currency);
                }
                else
                {
                    if (userVotesDb.Count == 0 || userVotesDb[0].DateAdded.AddHours(12) < timeNow)
                    {
                        await ReplyConfirmationAsync(Localization.GamblingDailyReceivedVote, 100, Credentials.Currency, userDb.Currency, $"{Credentials.DiscordBotList}/vote", Credentials.Patreon);
                    }
                    else
                    {
                        var nextVoteHumanized = (userVotesDb[0].DateAdded.AddHours(12) - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), TimeUnit.Hour, TimeUnit.Second);
                        await ReplyConfirmationAsync(Localization.GamblingDailyReceivedVoted, 100, Credentials.Currency, userDb.Currency, $"{Credentials.DiscordBotList}/vote", nextVoteHumanized, Credentials.Patreon);
                    }
                }
            }
        }
    }
}