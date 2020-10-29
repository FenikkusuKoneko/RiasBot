using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Humanizer.Localisation;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using NCalc;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Models;
using Rias.Services;

namespace Rias.Modules.Utility
{
    [Name("Utility")]
    public partial class UtilityModule : RiasModule
    {
        private readonly UnitsService _unitsService;
        
        public UtilityModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _unitsService = serviceProvider.GetRequiredService<UnitsService>();
        }

        [Command("prefix")]
        [Context(ContextType.Guild)]
        public Task PrefixAsync()
        => ReplyConfirmationAsync(Localization.UtilityPrefixIs, Context.Prefix);

        [Command("setprefix")]
        [Context(ContextType.Guild)]
        [UserPermission(Permissions.Administrator)]
        public async Task SetPrefixAsync([Remainder] string prefix)
        {
            if (prefix.Length > 15)
            {
                await ReplyErrorAsync(Localization.UtilityPrefixLimit, 15);
                return;
            }
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity { GuildId = Context.Guild!.Id });
            guildDb.Prefix = prefix;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.UtilityPrefixChanged, Context.Prefix, prefix);
        }

        [Command("languages")]
        [Context(ContextType.Guild)]
        public async Task LanguagesAsync()
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityLanguages),
                Description = string.Join("\n", Localization.Locales.Select(x => $"{x.Language} ({x.Locale})")),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = GetText(Localization.UtilityLanguagesFooter, Credentials.Prefix)
                }
            };

            await ReplyAsync(embed);
        }

        [Command("setlanguage")]
        [Context(ContextType.Guild)]
        public async Task SetLanguageAsync(string language)
        {
            var (locale, lang) = Localization.Locales.FirstOrDefault(x =>
                string.Equals(x.Locale, language, StringComparison.OrdinalIgnoreCase) || x.Language.StartsWith(language, StringComparison.OrdinalIgnoreCase))!;
            
            if (string.IsNullOrEmpty(locale))
            {
                await ReplyErrorAsync(Localization.UtilityLanguageNotFound);
                return;
            }

            if (string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase))
            {
                Localization.RemoveGuildLocale(Context.Guild!.Id);
                
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild.Id, () => new GuildsEntity { GuildId = Context.Guild.Id });
                guildDb.Locale = null;
            }
            else
            {
                Localization.SetGuildLocale(Context.Guild!.Id, locale.ToLowerInvariant());
                
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild.Id, () => new GuildsEntity { GuildId = Context.Guild.Id });
                guildDb.Locale = locale.ToLower();
            }
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.UtilityLanguageSet, $"{lang} ({locale})");
        }
        
        [Command("invite")]
        public async Task InviteAsync()
        {
            if (!string.IsNullOrEmpty(Credentials.Invite))
                await ReplyConfirmationAsync(Localization.UtilityInviteInfo, Credentials.Invite);
        }
        
        [Command("patreon")]
        public async Task DonateAsync()
        {
            if (!string.IsNullOrEmpty(Credentials.Patreon))
                await ReplyConfirmationAsync(Localization.UtilityPatreonInfo, Credentials.Patreon, Credentials.Currency);
        }
        
        [Command("patrons")]
        public async Task PatronsAsync()
        {
            var patrons = await DbContext.GetOrderedListAsync<PatreonEntity, int>(
                x => x.PatronStatus == PatronStatus.ActivePatron && x.Tier > 0,
                y => y.AmountCents,
                true);
            
            if (patrons.Count == 0)
            {
                await ReplyErrorAsync(Localization.UtilityNoPatrons, Credentials.Patreon, Credentials.Currency);
                return;
            }

            var users = (await Task.WhenAll(patrons.Select(x => RiasBot.GetUserAsync(x.UserId))))
                .ToDictionary(x => x!.Id);

            await SendPaginatedMessageAsync(patrons, 15, (items, index) => new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityAllPatrons),
                Description = string.Join("\n", items.Select(p =>
                    $"{++index}. **{(users.TryGetValue(p.UserId, out var user) ? user?.FullName() : p.UserId.ToString())}**"))
            });
        }
        
        [Command("vote")]
        public async Task VoteAsync()
        {
            var timeNow = DateTime.UtcNow;
            var userVotesDb = await DbContext.GetOrderedListAsync<VotesEntity, DateTime>(x => x.UserId == Context.User.Id, x => x.DateAdded, true);
            if (userVotesDb.Count == 0 || userVotesDb[0].DateAdded.AddHours(12) < timeNow)
            {
                await ReplyConfirmationAsync(Localization.UtilityVoteInfo, $"{Credentials.DiscordBotList}/vote", Credentials.Currency);
            }
            else
            {
                var nextVoteHumanized = (userVotesDb[0].DateAdded.AddHours(12) - timeNow).Humanize(3, new CultureInfo(Localization.GetGuildLocale(Context.Guild!.Id)), TimeUnit.Hour, TimeUnit.Second);
                await ReplyConfirmationAsync(Localization.UtilityVotedInfo, $"{Credentials.DiscordBotList}/vote", nextVoteHumanized);
            }
        }
        
        [Command("votes")]
        [Cooldown(1, 10, CooldownMeasure.Seconds, BucketType.Guild)]
        public async Task VotesAsync(TimeSpan? timeSpan = null)
        {
            timeSpan ??= TimeSpan.FromHours(12);

            var locale = Localization.GetGuildLocale(Context.Guild?.Id);

            var lowestTime = TimeSpan.FromMinutes(1);
            if (timeSpan < lowestTime)
            {
                await ReplyErrorAsync(Localization.UtilityTimeLowest, lowestTime.Humanize(1, new CultureInfo(locale)));
                return;
            }
            
            var now = DateTime.UtcNow;
            var highestTime = now.AddMonths(1) - now;
            if (timeSpan > highestTime)
            {
                await ReplyErrorAsync(Localization.UtilityTimeHighest, highestTime.Humanize(1, new CultureInfo(locale), maxUnit: TimeUnit.Month));
                return;
            }
            
            var dateAdded = DateTime.UtcNow - timeSpan;
            var votesGroup = (await DbContext.GetListAsync<VotesEntity>(x => x.DateAdded >= dateAdded))
                .GroupBy(x => x.UserId)
                .ToList();

            var users = (await Task.WhenAll(votesGroup.Select(x => RiasBot.GetUserAsync(x.Key))))
                .ToDictionary(x => x!.Id);

            var index = 0;
            var votesList = votesGroup.Select(x =>
                $"{++index}. {(users.TryGetValue(x.Key, out var user) ? user?.FullName() : x.Key.ToString())} | {GetText(Localization.UtilityVotes)}: {x.Count()}")
                .ToList();

            if (votesList.Count == 0)
            {
                await ReplyErrorAsync(Localization.UtilityNoVotes);
                return;
            }

            var timeSpanHumanized = timeSpan.Value.Humanize(5, new CultureInfo(Localization.GetGuildLocale(Context.Guild?.Id)), TimeUnit.Month);
            await SendPaginatedMessageAsync(votesList, 15, (items, _) => new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityAllVotes, timeSpanHumanized),
                Description = string.Join("\n", items)
            });
        }
        
        [Command("ping")]
        [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Channel)]
        public async Task PingAsync()
        {
            var sw = Stopwatch.StartNew();
            await Context.Channel.TriggerTypingAsync();
            sw.Stop();
            var timeOne = sw.ElapsedMilliseconds;
            
            sw.Restart();
            var msg = await Context.Channel.SendMessageAsync(":ping_pong:");
            sw.Stop();
            var timeTwo = sw.ElapsedMilliseconds;
            
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Description = GetText(Localization.UtilityPingInfo, RiasBot.Latency, timeOne, timeTwo, (timeOne + timeTwo) / 2)
            };

            await msg.ModifyAsync(null, embed.Build());
        }

        [Command("choose")]
        public async Task ChooseAsync([Remainder] string list)
        {
            var choices = list.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);

            var choice = new Random().Next(choices.Length);
            await ReplyConfirmationAsync(Localization.UtilityChose, choices[choice].Trim());
        }

        [Command("color")]
        [Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.User)]
        public async Task ColorAsync([Remainder] DiscordColor color)
        {
            var serverAttachFilesPerm = Context.Guild!.CurrentMember.GetPermissions().HasPermission(Permissions.AttachFiles);
            var channelAttachFilesPerm = Context.Guild!.CurrentMember.PermissionsIn(Context.Channel).HasPermission(Permissions.AttachFiles);
            if (!serverAttachFilesPerm && !channelAttachFilesPerm)
            {
                await ReplyErrorAsync(Localization.UtilityColorNoAttachFilesPermission);
                return;
            }

            if (serverAttachFilesPerm && !channelAttachFilesPerm)
            {
                await ReplyErrorAsync(Localization.UtilityColorNoAttachFilesChannelPermission);
                return;
            }

            var hexColor = color.ToString();
            var magickColor = new MagickColor(hexColor);
            var hsl = ColorHSL.FromMagickColor(magickColor);
            var yuv = ColorYUV.FromMagickColor(magickColor);
            var cmyk = ColorCMYK.FromMagickColor(magickColor);

            var ushortMax = (double)ushort.MaxValue;
            var byteMax = byte.MaxValue;
            var colorDetails = new StringBuilder()
                .Append($"**Hex:** {hexColor}").AppendLine()
                .Append($"**Rgb:** {magickColor.R / ushortMax * byteMax} {magickColor.G / ushortMax * byteMax} {magickColor.B / ushortMax * byteMax}").AppendLine()
                .Append($"**Hsl:** {hsl.Hue:F2}% {hsl.Saturation:F2}% {hsl.Lightness:F2}%").AppendLine()
                .Append($"**Yuv:** {yuv.Y:F2} {yuv.U:F2} {yuv.V:F2}").AppendLine()
                .Append($"**Cmyk:** {cmyk.C / ushortMax * byteMax} {cmyk.M / ushortMax * byteMax} {cmyk.Y / ushortMax * byteMax} {cmyk.K / ushortMax * byteMax}");

            var fileName = $"{color.Value.ToString()}.png";
            var embed = new DiscordEmbedBuilder()
                .WithColor(color)
                .WithDescription(colorDetails.ToString())
                .WithImageUrl($"attachment://{fileName}");
            
            using var magickImage = new MagickImage(MagickColor.FromRgb(color.R, color.G, color.B), 300, 300);
            var image = new MemoryStream();
            magickImage.Write(image, MagickFormat.Png);
            image.Position = 0;
            await Context.Channel.SendFileAsync(fileName, image, embed: embed);
        }
        
        [Command("calculator")]
        public async Task CalculatorAsync([Remainder] string expression)
        {
            var expr = new Expression(expression, EvaluateOptions.IgnoreCase);
            expr.EvaluateParameter += ExpressionEvaluateParameter;

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityCalculator)
            }.AddField(GetText(Localization.UtilityExpression), expression);

            try
            {
                var result = expr.Evaluate();
                embed.AddField(GetText(Localization.UtilityResult), result.ToString());
            }
            catch
            {
                embed.AddField(GetText(Localization.UtilityResult), !string.IsNullOrEmpty(expr.Error) ? expr.Error : GetText(Localization.UtilityExpressionFailed));
            }

            await ReplyAsync(embed);
        }

        [Command("converter")]
        [Priority(3)]
        public async Task ConverterAsync(double value, string unit1, string unit2)
            => await ConverterAsync(unit1, unit2, value);

        [Command("converter")]
        [Priority(2)]
        public async Task ConverterAsync(string unitOneName, string unitTwoName, double value)
        {
            var unitsIndex = _unitsService.GetUnits(ref unitOneName, ref unitTwoName, out var unitOne, out var unitTwo);
            switch (unitsIndex)
            {
                case -1:
                    await ReplyErrorAsync(Localization.UtilityUnitNotFound, unitOneName);
                    return;
                case -2:
                    await ReplyErrorAsync(Localization.UtilityUnitNotFound, unitTwoName);
                    return;
                case 0:
                    await ReplyErrorAsync(
                        Localization.UtilityUnitsNotCompatible,
                        $"{unitOne!.Name.Singular} ({unitOne.Category.Name})",
                        $"{unitTwo!.Name.Singular} ({unitTwo.Category.Name})");
                    return;
            }
            
            var result = _unitsService.Convert(unitOne!, unitTwo!, value);

            unitOneName = value == 1 ? unitOne!.Name.Singular! : unitOne!.Name.Plural!;
            unitTwoName = result == 1 ? unitTwo!.Name.Singular! : unitTwo!.Name.Plural!;
            
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityConverter),
                Description = $"**[{unitOne.Category.Name}]**\n" +
                              $"{value} {unitOneName} = {Format(result)} {unitTwoName}"
            };

            await ReplyAsync(embed);
        }

        [Command("converter")]
        [Priority(1)]
        public async Task ConverterAsync(string category, double value, string unit1, string unit2)
            => await ConverterAsync(category, unit1, unit2, value);
        
        [Command("converter")]
        [Priority(0)]
        public async Task ConverterAsync(string category, string unitOneName, string unitTwoName, double value)
        {
            var unitsCategory = _unitsService.GetUnitsByCategory(category);
            if (unitsCategory is null)
            {
                await ReplyErrorAsync(Localization.UtilityUnitsCategoryNotFound, category);
                return;
            }
            
            var unitsIndex = _unitsService.GetUnits(ref unitOneName, ref unitTwoName, out var unitOne, out var unitTwo, unitsCategory);
            switch (unitsIndex)
            {
                case -1:
                    await ReplyErrorAsync(Localization.UtilityUnitNotFoundInCategory, unitOneName, unitsCategory.Name);
                    return;
                case -2:
                    await ReplyErrorAsync(Localization.UtilityUnitNotFoundInCategory, unitTwoName, unitsCategory.Name);
                    return;
                case 0:
                    await ReplyErrorAsync(
                        Localization.UtilityUnitsNotCompatible,
                        $"{unitOne!.Name.Singular} ({unitOne.Category.Name})",
                        $"{unitTwo!.Name.Singular} ({unitTwo.Category.Name})");
                    return;
            }
            
            var result = _unitsService.Convert(unitOne!, unitTwo!, value);
        
            unitOneName = value == 1 ? unitOne!.Name.Singular! : unitOne!.Name.Plural!;
            unitTwoName = result == 1 ? unitTwo!.Name.Singular! : unitTwo!.Name.Plural!;
        
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityConverter),
                Description = $"**[{unitOne.Category.Name}]**\n" +
                              $"{value} {unitOneName} = {Format(result)} {unitTwoName}"
            };
        
            await ReplyAsync(embed);
        }
        
        [Command("converterlist")]
        public async Task ConverterList(string? category = null)
        {
            if (category is null)
            {
                await SendPaginatedMessageAsync(
                    _unitsService.GetAllUnits().OrderBy(x => x.Name).ToList(),
                    15,
                    (items, index) => new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Title = GetText(Localization.UtilityAllUnitsCategories),
                        Description = string.Join("\n", items.Select(x => $"{++index}. {x.Name}")),
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = GetText(Localization.UtilityConvertListFooter, Context.Prefix)
                        }
                    });
                
                return;
            }

            var units = _unitsService.GetUnitsByCategory(category);
            if (units is null)
            {
                await ReplyErrorAsync(Localization.UtilityUnitsCategoryNotFound, category);
                return;
            }

            await SendPaginatedMessageAsync(units.Units.ToList(), 15, (items, index) => new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityCategoryAllUnits, category),
                Description = string.Join("\n", items.Select(x =>
                {
                    var abbreviations = x.Name.Abbreviations?.ToList();
                    var abbreviationsString = string.Empty;
                    if (abbreviations != null && abbreviations.Count != 0)
                    {
                        abbreviationsString = $" [{string.Join(", ", abbreviations)}]";
                    }
                    
                    return $"{++index}. {x.Name.Singular}{abbreviationsString}";
                }))
            });
        }
        
        private static void ExpressionEvaluateParameter(string name, ParameterArgs args)
        {
            args.Result = name.ToLowerInvariant() switch
            {
                "pi" => Math.PI,
                "e" => Math.E,
                _ => default
            };
        }

        /// <summary>
        /// If the number is higher than 1 or lower than -1 then it rounds on the first 2 digits.
        /// Otherwise it rounds after the number of leading zeros.<br/>
        /// Ex: 0.00234 = 0.0023, 0.0000234 = 2.3E-5, 1.23 = 1.23, 1E20 = 1E+20.
        /// </summary>
        private string Format(double d)
        {
            if (Math.Abs(d) >= 1)
            {
                d = Math.Round(d, 2);
                return d < 1E9 ? d.ToString(CultureInfo.InvariantCulture) : d.ToString("0.##E0");
            }

            var fractionPart = d % 1.0;
            if (fractionPart == 0)
                return d.ToString(CultureInfo.InvariantCulture);
            
            var count = -2;
            while (fractionPart < 10 && count < 7)
            {
                fractionPart *= 10;
                count++;
            }
            
            return count < 7 ? Math.Round(d, count + 2).ToString(CultureInfo.InvariantCulture) : d.ToString("0.##E0");
        }
    }
}