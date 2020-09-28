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
        
        [Command("pat")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task PatAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pat"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPatYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPattedBy,
                    member.Mention, ((DiscordMember)Context.User).DisplayName), embed: embed);
        }

        [Command("pat")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task PatAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pat"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPatYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPattedBy, value, ((DiscordMember)Context.User).DisplayName),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("hug")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task HugAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("hug"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHugYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHuggedBy,
                    member.Mention, ((DiscordMember)Context.User).DisplayName), embed: embed);
        }

        [Command("hug")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task HugAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("hug"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHugYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHuggedBy, value, ((DiscordMember)Context.User).DisplayName),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("kiss")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task KissAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("kiss"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissedBy,
                    member.Mention, ((DiscordMember)Context.User).DisplayName), embed: embed);
        }

        [Command("kiss")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task KissAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("kiss"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissedBy, value, ((DiscordMember)Context.User).DisplayName),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("lick")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task LickAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("lick"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickedBy,
                    member.Mention, ((DiscordMember)Context.User).DisplayName), embed: embed);
        }

        [Command("lick")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task LickAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("lick"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickedBy, value, ((DiscordMember)Context.User).DisplayName),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("cuddle")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task CuddleAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("cuddle"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddleYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddledBy,
                    member.Mention, ((DiscordMember)Context.User).DisplayName), embed: embed);
        }

        [Command("cuddle")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task CuddleAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("cuddle"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddleYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddledBy, value, ((DiscordMember)Context.User).DisplayName),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("bite")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task BiteAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("bite"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBiteYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBittenBy,
                    member.Mention, ((DiscordMember)Context.User).DisplayName), embed: embed);
        }

        [Command("bite")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task BiteAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("bite"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBiteYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBittenBy, value, ((DiscordMember)Context.User).DisplayName),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("slap")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task SlapAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("slap"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlapYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlappedBy,
                    member.Mention, ((DiscordMember)Context.User).DisplayName), embed: embed);
        }

        [Command("slap")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task SlapAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("slap"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlapYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlappedBy, value, ((DiscordMember)Context.User).DisplayName),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("cry")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task CryAsync()
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("cry"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };
            
            await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDontCry, Context.User.Mention), embed: embed);
        }

        [Command("grope")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task GropeAsync([Remainder] DiscordMember member)
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetGropeUrlAsync(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} riasbot.me"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropeYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropedBy,
                    member.Mention, ((DiscordMember)Context.User).DisplayName), embed: embed);
        }

        [Command("grope")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task GropeAsync([Remainder] string? value = null)
        {
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }
            
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetGropeUrlAsync(),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} riasbot.me"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropeYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropedBy, value, ((DiscordMember)Context.User).DisplayName),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("blush")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task BlushAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("blush"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlush, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlushAt,
                    ((DiscordMember)Context.User).DisplayName, member.Mention), embed: embed);
        }

        [Command("blush")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task BlushAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("blush"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlush, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlushAt, ((DiscordMember)Context.User).DisplayName, value),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("dance")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task DanceAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("dance"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDance, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDanceTogether, Context.User.Mention, member.Mention), embed: embed);
        }

        [Command("dance")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task DanceAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("dance"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDance, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDanceTogether, Context.User.Mention, value),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("poke")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task PokeAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("poke"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokeYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokedBy,
                    member.Mention, ((DiscordMember)Context.User).DisplayName), embed: embed);
        }

        [Command("poke")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task PokeAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("poke"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokeYou, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokedBy, value, ((DiscordMember)Context.User).DisplayName),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("pout")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task PoutAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pout"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPout, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPoutAt,
                    ((DiscordMember)Context.User).DisplayName, member.Mention), embed: embed);
        }

        [Command("pout")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task PoutAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }
            
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pout"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPout, Context.User.Mention), embed: embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPoutAt, ((DiscordMember)Context.User).DisplayName, value),
                    embed: embed, mentions: Context.Message.MentionedUsers.Select(x => (IMention)new UserMention(x)));
        }

        [Command("sleepy")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task SleepyAsync()
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("sleepy"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };
            
            await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSleepy, Context.User.Mention), embed: embed);
        }
    }
}