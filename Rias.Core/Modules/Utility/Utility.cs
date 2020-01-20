using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using NCalc;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Models;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Interactive;
using Rias.Interactive.Paginator;

namespace Rias.Core.Modules.Utility
{
    [Name("Utility")]
    public partial class Utility : RiasModule
    {
        private readonly DiscordShardedClient _client;
        private readonly InteractiveService _interactive;
        private readonly CommandHandlerService _commandHandlerService;
        
        public Utility(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _interactive = services.GetRequiredService<InteractiveService>();
            _commandHandlerService = services.GetRequiredService<CommandHandlerService>();
        }

        [Command("prefix"), Context(ContextType.Guild)]
        public async Task PrefixAsync()
        {
            await ReplyConfirmationAsync("PrefixIs", GetPrefix());
        }

        [Command("setprefix"), Context(ContextType.Guild),
         UserPermission(GuildPermission.Administrator)]
        public async Task SetPrefixAsync([Remainder] string prefix)
        {
            if (prefix.Length > 15)
            {
                await ReplyErrorAsync("PrefixLimit", 15);
                return;
            }

            var currentPrefix = GetPrefix();
            _commandHandlerService.GuildPrefixes[Context.Guild!.Id] = prefix;

            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new Guilds {GuildId = Context.Guild!.Id});
            guildDb.Prefix = prefix;

            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("PrefixChanged", currentPrefix, prefix);
        }
        
        [Command("languages"), Context(ContextType.Guild)]
        public async Task LanguagesAsync()
        {
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = GetText("Languages"),
                Description = string.Join('\n', Resources.Languages.Select(x => $"{x.Language} ({x.Locale})"))
            };

            await ReplyAsync(embed);
        }

        [Command("setlanguage"), Context(ContextType.Guild)]
        public async Task SetLanguageAsync(string language)
        {
            var (locale, lang) = Resources.Languages.FirstOrDefault(x =>
                string.Equals(x.Locale, language, StringComparison.OrdinalIgnoreCase) || x.Language.StartsWith(language, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(locale))
            {
                await ReplyErrorAsync("LanguageNotFound");
                return;
            }
            
            Resources.SetGuildCulture(Context.Guild!.Id, new CultureInfo(locale));
            
            var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild.Id, () => new Guilds {GuildId = Context.Guild.Id});
            guildDb.Locale = locale.ToLower();
            
            await DbContext.SaveChangesAsync();
            await ReplyConfirmationAsync("LanguageSet", $"{lang} ({locale})");
        }

        [Command("invite")]
        public async Task InviteAsync()
        {
            if (!string.IsNullOrEmpty(Credentials.Invite))
                await ReplyConfirmationAsync("InviteInfo", Credentials.Invite);
        }
        
        [Command("patreon")]
        public async Task DonateAsync()
        {
            if (!string.IsNullOrEmpty(Credentials.Patreon))
                await ReplyConfirmationAsync("PatreonInfo", Credentials.Patreon, Credentials.Currency);
        }
        
        [Command("patrons")]
        public async Task PatronsAsync()
        {
            var patrons = await DbContext.GetOrderedListAsync<Patreon, int>(x => x.PatronStatus == PatronStatus.ActivePatron, y => y.AmountCents, true);
            if (patrons.Count == 0)
            {
                await ReplyErrorAsync("NoPatrons", Credentials.Patreon, Credentials.Currency);
                return;
            }
            
            var index = 0;
            var pages = patrons.Batch(15).Select(x => new InteractiveMessage
            (
                new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = GetText("AllPatrons"),
                    Description = string.Join('\n', x.Select(p => $"{++index}. {_client.GetUser(p.UserId)?.ToString() ?? p.UserId.ToString()}"))
                }
            ));

            await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
        }
        
        [Command("vote")]
        public async Task VoteAsync()
        {
            await ReplyConfirmationAsync("VoteInfo", $"{Credentials.DiscordBotList}/vote", Credentials.Currency);
        }
        
        [Command("votes")]
        public async Task VotesAsync(string? time = null)
        {
            var timeSpan = time switch
            {
                "24h" => TimeSpan.FromHours(24),
                "1d" => TimeSpan.FromDays(1),
                "7d" => TimeSpan.FromDays(7),
                "30d" => TimeSpan.FromDays(30),
                "1mo" => TimeSpan.FromDays(30),
                _ => TimeSpan.FromHours(12)
            };

            var dateAdded = DateTime.UtcNow - timeSpan;
            var votesGroup = (await DbContext.GetListAsync<Votes>(x => x.DateAdded >= dateAdded))
                .GroupBy(x => x.UserId)
                .ToList();

            var index = 0;
            var votesList = (from votes in votesGroup
                let user = _client.GetUser(votes.Key)
                select $"{++index}. {(user != null ? user.ToString() : votes.Key.ToString())} | {GetText("Votes")}: {votes.Count()}").ToList();

            if (votesList.Count == 0)
            {
                await ReplyErrorAsync("NoVotes");
                return;
            }

            var timeSpanHumanized = timeSpan.Humanize(culture: Resources.GetGuildCulture(Context.Guild?.Id), maxUnit: TimeUnit.Month);

            var pages = votesList.Batch(15).Select(x => new InteractiveMessage
            (
                new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = GetText("AllVotes", timeSpanHumanized),
                    Description = string.Join('\n', x)
                }
            ));

            await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
        }

        [Command("ping")]
        public async Task PingAsync()
        {
            var sw = Stopwatch.StartNew();
            await Context.Channel.TriggerTypingAsync();
            sw.Stop();

            await ReplyConfirmationAsync("PingInfo", Context.Client.Latency, sw.ElapsedMilliseconds);
        }

        [Command("choose")]
        public async Task ChooseAsync([Remainder] string list)
        {
            var choices = list.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);

            var choice = new Random().Next(choices.Length);
            await ReplyConfirmationAsync("Chose", choices[choice].Trim());
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
            
            var colorDetails = new StringBuilder()
                .Append($"**Hex:** {hexColor}").AppendLine()
                .Append($"**Hsl:** {hsl.Hue:F2}% {hsl.Saturation:F2}% {hsl.Lightness:F2}%").AppendLine()
                .Append($"**Yuv:** {yuv.Y:F2} {yuv.U:F2} {yuv.V:F2}").AppendLine()
                .Append($"**Cmyk:** {cmyk.C} {cmyk.M} {cmyk.Y} {cmyk.K}");

            var fileName = $"{color.RawValue.ToString()}.png";
            var embed = new EmbedBuilder()
                .WithColor(color)
                .WithDescription(colorDetails.ToString())
                .WithImageUrl($"attachment://{fileName}");
            
            using var magickImage = new MagickImage(MagickColor.FromRgb(color.R, color.G, color.B), 300, 300);
            var image = new MemoryStream();
            magickImage.Write(image, MagickFormat.Png);
            image.Position = 0;
            await Context.Channel.SendFileAsync(image, fileName, embed: embed.Build());
        }

        [Command("calculator")]
        public async Task CalculatorAsync([Remainder] string expression)
        {
            var expr = new Expression(expression, EvaluateOptions.IgnoreCase);
            expr.EvaluateParameter += ExpressionEvaluateParameter;

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = GetText("Calculator")
            }.AddField(GetText("Expression"), expression);

            try
            {
                var result = expr.Evaluate();
                embed.AddField(GetText("Result"), result);
            }
            catch
            {
                embed.AddField(GetText("Result"), !string.IsNullOrEmpty(expr.Error) ? expr.Error : GetText("ExpressionFailed"));
            }

            await ReplyAsync(embed);
        }
        
        private static void ExpressionEvaluateParameter(string name, ParameterArgs args)
        {
            switch (name.ToLowerInvariant())
            {
                case "pi":
                    args.Result = Math.PI;
                    break;
                case "e":
                    args.Result = Math.E;
                    break;
                default:
                    args.Result = default;
                    break;
            }
        }
    }
}