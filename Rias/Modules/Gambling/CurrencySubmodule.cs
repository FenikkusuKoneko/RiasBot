using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
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
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == member.Id, () => new UserEntity { UserId = member.Id });
                if (member.Id == Context.User.Id)
                {
                    var userVotesDb = await DbContext.GetOrderedListAsync<VoteEntity, DateTime>(x => x.UserId == member.Id, x => x.CreatedAt, true);

                    if (string.IsNullOrEmpty(Configuration.DiscordBotList))
                    {
                        await ReplyConfirmationAsync(Localization.GamblingCurrencyYou, userDb.Currency, Configuration.Currency);
                    }
                    else
                    {
                        var timeNow = DateTime.UtcNow;
                        if (userVotesDb.Count == 0 || userVotesDb[0].CreatedAt.AddHours(12) < timeNow)
                        {
                            await ReplyConfirmationAsync(Localization.GamblingCurrencyYouVote, userDb.Currency, Configuration.Currency, $"{Configuration.DiscordBotList}/vote", Configuration.Patreon);
                        }
                        else
                        {
                            var nextVoteHumanized = (userVotesDb[0].CreatedAt.AddHours(12) - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), TimeUnit.Hour, TimeUnit.Second);
                            await ReplyConfirmationAsync(Localization.GamblingCurrencyYouVoted, userDb.Currency, Configuration.Currency, $"{Configuration.DiscordBotList}/vote", nextVoteHumanized, Configuration.Patreon);
                        }
                    }
                }
                else
                {
                    await ReplyConfirmationAsync(Localization.GamblingCurrencyMember, member.FullName(), userDb.Currency, Configuration.Currency);
                }
            }

            [Command("reward")]
            [MasterOnly]
            public async Task RewardAsync(int amount, [Remainder] DiscordUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UserEntity { UserId = user.Id });
                var currency = userDb.Currency += amount;

                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.GamblingUserRewarded, amount, Configuration.Currency, user.FullName(), currency);
            }

            [Command("take")]
            [MasterOnly]
            public async Task TakeAsync(int amount, [Remainder] DiscordUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UserEntity { UserId = user.Id });
                amount = Math.Min(amount, userDb.Currency);
                userDb.Currency -= amount;

                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.GamblingUserTook, amount, Configuration.Currency, user.FullName());
            }

            [Command("leaderboard", "lb")]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
            public async Task LeaderboardAsync(int page = 1)
            {
                page--;
                if (page < 0) page = 0;
                
                var usersCurrency = await DbContext.GetOrderedListAsync<UserEntity, int>(x => x.Currency, true);
                var currencyLeaderboard = usersCurrency.Skip(page * 15)
                    .Take(15)
                    .ToList();
                
                if (currencyLeaderboard.Count == 0)
                {
                    await ReplyErrorAsync(Localization.GamblingLeaderboardNoUsers);
                    return;
                }

                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.GamblingCurrencyLeaderboard, Configuration.Currency),
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        IconUrl = Context.User.GetAvatarUrl(ImageFormat.Auto),
                        Text = $"{Context.User.FullName()} • #{usersCurrency.FindIndex(x => x.UserId == Context.User.Id) + 1}"
                    }
                };

                var index = 0;
                foreach (var userCurrency in currencyLeaderboard)
                {
                    var user = await RiasBot.GetUserAsync(userCurrency.UserId);
                    embed.AddField($"#{++index + page * 15} {user?.FullName()}", $"{userCurrency.Currency} {Configuration.Currency}", true);
                }

                await ReplyAsync(embed);
            }

            [Command("daily", "dailies")]
            [Context(ContextType.Guild)]
            public async Task DailyAsync()
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == Context.User.Id, () => new UserEntity { UserId = Context.User.Id });
                var userVotesDb = await DbContext.GetOrderedListAsync<VoteEntity, DateTime>(x => x.UserId == Context.User.Id, x => x.CreatedAt, true);
                
                var timeNow = DateTime.UtcNow;
                var nextDaily = userDb.DailyTaken.AddDays(1);
                if (nextDaily > timeNow)
                {
                    var timeLeftHumanized = (nextDaily - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), minUnit: TimeUnit.Second);
                    if (string.IsNullOrEmpty(Configuration.DiscordBotList))
                    {
                        await ReplyErrorAsync(Localization.GamblingDailyWait, timeLeftHumanized);
                    }
                    else
                    {
                        if (userVotesDb.Count == 0 || userVotesDb[0].CreatedAt.AddHours(12) < timeNow)
                        {
                            await ReplyErrorAsync(Localization.GamblingDailyWaitVote, timeLeftHumanized, $"{Configuration.DiscordBotList}/vote", Configuration.Currency, Configuration.Patreon);
                        }
                        else
                        {
                            var nextVoteHumanized = (userVotesDb[0].CreatedAt.AddHours(12) - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), TimeUnit.Hour, TimeUnit.Second);
                            await ReplyErrorAsync(Localization.GamblingDailyWaitVoted, timeLeftHumanized, $"{Configuration.DiscordBotList}/vote", nextVoteHumanized, Configuration.Patreon);
                        }
                    }
                    
                    return;
                }

                userDb.Currency += 100;
                userDb.DailyTaken = timeNow;

                await DbContext.SaveChangesAsync();
                if (string.IsNullOrEmpty(Configuration.DiscordBotList))
                {
                    await ReplyConfirmationAsync(Localization.GamblingDailyReceived, 100, Configuration.Currency, userDb.Currency);
                }
                else
                {
                    if (userVotesDb.Count == 0 || userVotesDb[0].CreatedAt.AddHours(12) < timeNow)
                    {
                        await ReplyConfirmationAsync(Localization.GamblingDailyReceivedVote, 100, Configuration.Currency, userDb.Currency, $"{Configuration.DiscordBotList}/vote", Configuration.Patreon);
                    }
                    else
                    {
                        var nextVoteHumanized = (userVotesDb[0].CreatedAt.AddHours(12) - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), TimeUnit.Hour, TimeUnit.Second);
                        await ReplyConfirmationAsync(Localization.GamblingDailyReceivedVoted, 100, Configuration.Currency, userDb.Currency, $"{Configuration.DiscordBotList}/vote", nextVoteHumanized, Configuration.Patreon);
                    }
                }
            }
        }
    }
}