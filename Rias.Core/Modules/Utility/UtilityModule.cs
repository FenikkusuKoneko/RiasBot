using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Humanizer.Localisation;
using ImageMagick;
using NCalc;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Implementation;
using Rias.Core.Models;

namespace Rias.Core.Modules.Utility
{
    [Name("Utility")]
    public class UtilityModule : RiasModule
    {
        public UtilityModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        
        [Command("prefix"), Context(ContextType.Guild)]
        public async Task PrefixAsync()
        => await ReplyConfirmationAsync(Localization.UtilityPrefixIs, Context.Prefix);
        
        [Command("setprefix"), Context(ContextType.Guild),
         UserPermission(Permission.Administrator)]
        public async Task SetPrefixAsync([Remainder] string prefix)
        {
            if (prefix.Length > 15)
            {
                await ReplyErrorAsync(Localization.UtilityPrefixLimit, 15);
                return;
            }
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
            guildDb.Prefix = prefix;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync(Localization.UtilityPrefixChanged, Context.Prefix, prefix);
        }
        
        [Command("languages"), Context(ContextType.Guild)]
        public async Task LanguagesAsync()
        {
            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityLanguages),
                Description = string.Join('\n', Localization.Locales.Select(x => $"{x.Language} ({x.Locale})")),
                Footer = new LocalEmbedFooterBuilder().WithText(GetText(Localization.UtilityLanguagesFooter, Credentials.Prefix))
            };

            await ReplyAsync(embed);
        }
        
        [Command("setlanguage"), Context(ContextType.Guild)]
        public async Task SetLanguageAsync(string language)
        {
            var (locale, lang) = Localization.Locales.FirstOrDefault(x =>
                string.Equals(x.Locale, language, StringComparison.OrdinalIgnoreCase) || x.Language.StartsWith(language, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(locale))
            {
                await ReplyErrorAsync(Localization.UtilityLanguageNotFound);
                return;
            }
            
            Localization.SetGuildLocale(Context.Guild!.Id, locale.ToLowerInvariant());
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild.Id, () => new GuildsEntity() {GuildId = Context.Guild.Id});
            guildDb.Locale = locale.ToLower();
            
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
                y => y.AmountCents, true);
            
            if (patrons.Count == 0)
            {
                await ReplyErrorAsync("NoPatrons", Credentials.Patreon, Credentials.Currency);
                return;
            }

            await SendPaginatedMessageAsync(patrons, 15, (items, index) => new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityAllPatrons),
                Description = string.Join('\n', items.Select(p => $"{++index}. {RiasBot.GetUser(p.UserId)?.ToString() ?? p.UserId.ToString()}"))
            });
        }
        
        [Command("vote")]
        public async Task VoteAsync()
        {
            await ReplyConfirmationAsync(Localization.UtilityVoteInfo, $"{Credentials.DiscordBotList}/vote", Credentials.Currency);
        }
        
        [Command("votes")]
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

            var index = 0;
            var votesList = (from votes in votesGroup
                let user = RiasBot.GetUser(votes.Key)
                select $"{++index}. {(user != null ? user.ToString() : votes.Key.ToString())} | {GetText(Localization.UtilityVotes)}: {votes.Count()}").ToList();

            if (votesList.Count == 0)
            {
                await ReplyErrorAsync(Localization.UtilityNoVotes);
                return;
            }

            var timeSpanHumanized = timeSpan.Value.Humanize(5, new CultureInfo(Localization.GetGuildLocale(Context.Guild?.Id)), TimeUnit.Month);
            await SendPaginatedMessageAsync(votesList, 15, (items, _) => new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityAllVotes, timeSpanHumanized),
                Description = string.Join('\n', items)
            });
        }
        
        [Command("ping")]
        public async Task PingAsync()
        {
            var sw = Stopwatch.StartNew();
            await Context.Channel.TriggerTypingAsync();
            sw.Stop();

            await ReplyConfirmationAsync(Localization.UtilityPingInfo, RiasBot.Latency.GetValueOrDefault().TotalMilliseconds.ToString("F3"), sw.ElapsedMilliseconds);
        }

        [Command("choose")]
        public async Task ChooseAsync([Remainder] string list)
        {
            var choices = list.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);

            var choice = new Random().Next(choices.Length);
            await ReplyConfirmationAsync(Localization.UtilityChose, choices[choice].Trim());
        }
        
        [Command("color"),
         Cooldown(1, 3, CooldownMeasure.Seconds, BucketType.User)]
        public async Task ColorAsync([Remainder] Color color)
        {
            var hexColor = color.ToString();
            var magickColor = new MagickColor(hexColor);
            var hsl = ColorHSL.FromMagickColor(magickColor);
            var yuv = ColorYUV.FromMagickColor(magickColor);
            var cmyk = ColorCMYK.FromMagickColor(magickColor);

            var ushortMax = (double) ushort.MaxValue;
            var byteMax = byte.MaxValue;
            var colorDetails = new StringBuilder()
                .Append($"**Hex:** {hexColor}").AppendLine()
                .Append($"**Rgb:** {magickColor.R / ushortMax * byteMax} {magickColor.G / ushortMax * byteMax} {magickColor.B / ushortMax * byteMax}").AppendLine()
                .Append($"**Hsl:** {hsl.Hue:F2}% {hsl.Saturation:F2}% {hsl.Lightness:F2}%").AppendLine()
                .Append($"**Yuv:** {yuv.Y:F2} {yuv.U:F2} {yuv.V:F2}").AppendLine()
                .Append($"**Cmyk:** {cmyk.C / ushortMax * byteMax} {cmyk.M / ushortMax * byteMax} {cmyk.Y / ushortMax * byteMax} {cmyk.K / ushortMax * byteMax}");

            var fileName = $"{color.RawValue.ToString()}.png";
            var embed = new LocalEmbedBuilder()
                .WithColor(color)
                .WithDescription(colorDetails.ToString())
                .WithImageUrl($"attachment://{fileName}");
            
            using var magickImage = new MagickImage(MagickColor.FromRgb(color.R, color.G, color.B), 300, 300);
            var image = new MemoryStream();
            magickImage.Write(image, MagickFormat.Png);
            image.Position = 0;
            await Context.Channel.SendMessageAsync(new LocalAttachment(image, fileName), embed: embed.Build());
        }
        
        [Command("calculator")]
        public async Task CalculatorAsync([Remainder] string expression)
        {
            var expr = new Expression(expression, EvaluateOptions.IgnoreCase);
            expr.EvaluateParameter += ExpressionEvaluateParameter;

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.UtilityCalculator)
            }.AddField(GetText(Localization.UtilityExpression), expression);

            try
            {
                var result = expr.Evaluate();
                embed.AddField(GetText(Localization.UtilityResult), result);
            }
            catch
            {
                embed.AddField(GetText(Localization.UtilityResult), !string.IsNullOrEmpty(expr.Error) ? expr.Error : GetText(Localization.UtilityExpressionFailed));
            }

            await ReplyAsync(embed);
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
    }
}