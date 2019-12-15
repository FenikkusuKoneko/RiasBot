using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Humanizer.Localisation;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using NCalc;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Interactive;
using Rias.Interactive.Paginator;
using Color = Discord.Color;

namespace Rias.Core.Modules.Utility
{
    [Name("Utility")]
    public partial class Utility : RiasModule<UtilityService>
    {
        private readonly InteractiveService _interactive;
        private readonly VotesService _votesService;
        private readonly PatreonService _patreonService;
        
        public Utility(IServiceProvider services) : base(services)
        {
            _interactive = services.GetRequiredService<InteractiveService>();
            _votesService = services.GetRequiredService<VotesService>();
            _patreonService = services.GetRequiredService<PatreonService>();
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
            await Service.SetPrefixAsync(Context.Guild!, prefix);
            await ReplyConfirmationAsync("PrefixChanged", currentPrefix, prefix);
        }

        [Command("invite")]
        public async Task InviteAsync()
        {
            if (!string.IsNullOrEmpty(Creds.Invite))
                await ReplyConfirmationAsync("InviteInfo", Creds.Invite);
        }
        
        [Command("patreon")]
        public async Task DonateAsync()
        {
            if (!string.IsNullOrEmpty(Creds.Patreon))
                await ReplyConfirmationAsync("PatreonInfo", Creds.Patreon, Creds.Currency);
        }
        
        [Command("patrons")]
        public async Task PatronsAsync()
        {
            var patrons = _patreonService.GetPatrons();

            
            if (patrons.Count == 0)
            {
                await ReplyErrorAsync("NoPatrons", Creds.Patreon, Creds.Currency);
                return;
            }
            
            var index = 0;
            var pages = patrons.Batch(15).Select(x => new InteractiveMessage
            (
                new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Title = GetText("AllPatrons"),
                    Description = string.Join('\n', x.Select(p => $"{++index}. {Context.Client.GetUser(p.UserId)?.ToString() ?? p.UserId.ToString()}"))
                }
            ));

            await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
        }
        
        [Command("vote")]
        public async Task VoteAsync()
        {
            await ReplyConfirmationAsync("VoteInfo", $"https://top.gg/bot/{Context.Client.CurrentUser.Id}", Creds.Currency);
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

            var votesGroup = _votesService.GetVotes(timeSpan)
                .GroupBy(x => x.UserId)
                .ToList();

            var index = 0;
            var votesList = (from votes in votesGroup
                let user = Context.Client.GetUser(votes.Key)
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
            await using var image = Service.GenerateColorImage(color);

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