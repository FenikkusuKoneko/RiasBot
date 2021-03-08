using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Reactions
{
    [Name("Reactions")]
    public class ReactionsModule : RiasModule<ReactionsService>
    {
        private const int ReactionLimit = 1500;
        
        public ReactionsModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        
        [Command("pat", "pet")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task PatAsync([Remainder] DiscordMember member)
            => SendReactionAsync("pat", member, Localization.ReactionsPatYou, Localization.ReactionsPattedBy);

        [Command("pat", "pet")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task PatAsync([Remainder] string? value = null)
            => SendReactionAsync("pat", value, Localization.ReactionsPatYou, Localization.ReactionsPattedBy);
        
        [Command("hug")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task HugAsync([Remainder] DiscordMember member)
            => SendReactionAsync("hug", member, Localization.ReactionsHugYou, Localization.ReactionsHuggedBy);

        [Command("hug")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task HugAsync([Remainder] string? value = null)
            => SendReactionAsync("hug", value, Localization.ReactionsHugYou, Localization.ReactionsHuggedBy);
        
        [Command("kiss")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task KissAsync([Remainder] DiscordMember member)
            => SendReactionAsync("kiss", member, Localization.ReactionsKissYou, Localization.ReactionsKissedBy);

        [Command("kiss")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task KissAsync([Remainder] string? value = null)
            => SendReactionAsync("kiss", value, Localization.ReactionsKissYou, Localization.ReactionsKissedBy);
        
        [Command("lick")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task LickAsync([Remainder] DiscordMember member)
            => SendReactionAsync("lick", member, Localization.ReactionsLickYou, Localization.ReactionsLickedBy);

        [Command("lick")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task LickAsync([Remainder] string? value = null)
            => SendReactionAsync("lick", value, Localization.ReactionsLickYou, Localization.ReactionsLickedBy);

        [Command("cuddle")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task CuddleAsync([Remainder] DiscordMember member)
            => SendReactionAsync("cuddle", member, Localization.ReactionsCuddleYou, Localization.ReactionsCuddledBy);

        [Command("cuddle")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task CuddleAsync([Remainder] string? value = null)
            => SendReactionAsync("cuddle", value, Localization.ReactionsCuddleYou, Localization.ReactionsCuddledBy);

        [Command("bite")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task BiteAsync([Remainder] DiscordMember member)
            => SendReactionAsync("bite", member, Localization.ReactionsBiteYou, Localization.ReactionsBittenBy);

        [Command("bite")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task BiteAsync([Remainder] string? value = null)
            => SendReactionAsync("bite", value, Localization.ReactionsBiteYou, Localization.ReactionsBittenBy);

        [Command("slap")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task SlapAsync([Remainder] DiscordMember member)
            => SendReactionAsync("slap", member, Localization.ReactionsSlapYou, Localization.ReactionsSlappedBy);

        [Command("slap")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task SlapAsync([Remainder] string? value = null)
            => SendReactionAsync("slap", value, Localization.ReactionsSlapYou, Localization.ReactionsSlappedBy);

        [Command("cry", "crying")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task CryAsync()
            => SendReactionAsync("cry", value: null, Localization.ReactionsDontCry, string.Empty);

        [Command("grope")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task GropeAsync([Remainder] DiscordMember member)
        {
            if (!Context.Channel.IsNSFW)
            {
                await ReplyErrorAsync(Localization.NsfwChannelNotNsfw);
                return;
            }
            
            await SendReactionAsync("grope", member, Localization.ReactionsGropeYou, Localization.ReactionsGropedBy, false);
        }

        [Command("grope")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task GropeAsync([Remainder] string? value = null)
        {
            if (!Context.Channel.IsNSFW)
            {
                await ReplyErrorAsync(Localization.NsfwChannelNotNsfw);
                return;
            }
            
            await SendReactionAsync("grope", value, Localization.ReactionsGropeYou, Localization.ReactionsGropedBy, false);
        }

        [Command("blush")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task BlushAsync([Remainder] DiscordMember member)
            => SendReactionAsync("blush", member, Localization.ReactionsBlush, Localization.ReactionsBlushAt);

        [Command("blush")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task BlushAsync([Remainder] string? value = null)
            => SendReactionAsync("blush", value, Localization.ReactionsBlush, Localization.ReactionsBlushAt);

        [Command("dance", "dancing")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task DanceAsync([Remainder] DiscordMember member)
            => SendReactionAsync("dance", member, Localization.ReactionsDance, Localization.ReactionsDanceTogether);

        [Command("dance")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task DanceAsync([Remainder] string? value = null)
            => SendReactionAsync("dance", value, Localization.ReactionsDance, Localization.ReactionsDanceTogether);

        [Command("poke")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task PokeAsync([Remainder] DiscordMember member)
            => SendReactionAsync("poke", member, Localization.ReactionsPokeYou, Localization.ReactionsPokedBy);

        [Command("poke")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task PokeAsync([Remainder] string? value = null)
            => SendReactionAsync("poke", value, Localization.ReactionsPokeYou, Localization.ReactionsPokedBy);

        [Command("pout")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task PoutAsync([Remainder] DiscordMember member)
            => SendReactionAsync("pout", member, Localization.ReactionsPout, Localization.ReactionsPoutAt);

        [Command("pout")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task PoutAsync([Remainder] string? value = null)
            => SendReactionAsync("pout", value, Localization.ReactionsPout, Localization.ReactionsPoutAt);

        [Command("goodmorning", "morning")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task GoodMorningAsync()
            => SendReactionAsync("good_morning", value: null, Localization.ReactionsGoodMorning, string.Empty, false);

        [Command("sleepy", "sleep", "goodnight")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task SleepyAsync()
            => SendReactionAsync("sleepy", value: null, Localization.ReactionsSleepy, string.Empty);

        [Command("baka", "idiot")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task BakaAsync([Remainder] DiscordMember member)
            => SendReactionAsync("baka", member, Localization.ReactionsBaka, Localization.ReactionsBakaMember);

        [Command("baka")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task BakaAsync([Remainder] string? value = null)
            => SendReactionAsync("baka", value, Localization.ReactionsBaka, Localization.ReactionsBakaMember);

        [Command("bang")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task BangAsync([Remainder] DiscordMember member)
            => SendReactionAsync("bang", member, Localization.ReactionsBangYou, Localization.ReactionsBangedBy);

        [Command("bang")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task BangAsync([Remainder] string? value = null)
            => SendReactionAsync("bang", value, Localization.ReactionsBangYou, Localization.ReactionsBangedBy);

        [Command("punch")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public Task PunchAsync([Remainder] DiscordMember member)
            => SendReactionAsync("punch", member, Localization.ReactionsPunchYou, Localization.ReactionsPunchedBy);

        [Command("punch")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public Task PunchAsync([Remainder] string? value = null)
            => SendReactionAsync("punch", value, Localization.ReactionsPunchYou, Localization.ReactionsPunchedBy);

        [Command("shrug")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task ShrugAsync()
            => SendReactionAsync("shrug", value: null, Localization.ReactionsShrug, string.Empty);
        
        [Command("handholding")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public Task HandholdingAsync([Remainder] DiscordMember? member = null)
        => SendReactionAsync("handholding", member, Localization.ReactionsHandholding, Localization.ReactionsHandholdingMember);

        [Command("waifuinsult")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task WaifuInsultAsync(DiscordMember member, [Remainder] string? waifu = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("waifu_insult"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                return;
            
            if (!string.IsNullOrWhiteSpace(waifu))
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsWaifuInsult,
                    member.Mention, ((DiscordMember) Context.User).DisplayName, waifu), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsWaifuInsultGenerally,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        private async Task SendReactionAsync(string reaction, DiscordMember? member, string localeYou, string localeBy, bool isWeeb = true)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = isWeeb ? await Service.GetReactionAsync(reaction) : await Service.GetImageAsync(reaction),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} {(isWeeb ? "weeb.sh" : "rias.gg")}"
                }
            };

            if (member is null || member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(localeYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(localeBy, member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        private async Task SendReactionAsync(string reaction, string? value, string localeYou, string localeBy, bool isWeeb = true)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            if (value is { Length: > ReactionLimit })
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = isWeeb ? await Service.GetReactionAsync(reaction) : await Service.GetImageAsync(reaction),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} {(isWeeb ? "weeb.sh" : "rias.gg")}"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(localeYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(localeBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }
    }
}