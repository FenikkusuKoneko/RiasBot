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
        public async Task PatAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("pat"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPatYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPattedBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("pat", "pet")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task PatAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("pat"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPatYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsPattedBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("hug")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task HugAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("hug"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHugYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHuggedBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("hug")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task HugAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("hug"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHugYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsHuggedBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("kiss")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task KissAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("kiss"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissedBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("kiss")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task KissAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("kiss"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsKissedBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("lick")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task LickAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("lick"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickedBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("lick")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task LickAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("lick"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsLickedBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("cuddle")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task CuddleAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("cuddle"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddleYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddledBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("cuddle")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task CuddleAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("cuddle"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddleYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsCuddledBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("bite")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task BiteAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("bite"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBiteYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBittenBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("bite")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task BiteAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("bite"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBiteYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsBittenBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("slap")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task SlapAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("slap"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlapYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlappedBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("slap")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task SlapAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("slap"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlapYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsSlappedBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("cry", "crying")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task CryAsync()
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("cry"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };
            
            await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDontCry, Context.User.Mention), embed);
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
                ImageUrl = await Service.GetImageAsync("grope"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} rias.gg"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropeYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropedBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
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
                ImageUrl = await Service.GetImageAsync("grope"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} rias.gg"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropeYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsGropedBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("blush")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task BlushAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("blush"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlush, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlushAt,
                    ((DiscordMember) Context.User).DisplayName, member.Mention), embed);
        }

        [Command("blush")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task BlushAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("blush"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlush, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsBlushAt, ((DiscordMember) Context.User).DisplayName, value))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("dance", "dancing")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task DanceAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("dance"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDance, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDanceTogether, Context.User.Mention, member.Mention), embed);
        }

        [Command("dance")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task DanceAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("dance"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDance, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsDanceTogether, Context.User.Mention, value))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("poke")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task PokeAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("poke"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokeYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokedBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("poke")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task PokeAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("poke"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokeYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsPokedBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }

        [Command("pout")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task PoutAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("pout"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPout, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPoutAt,
                    ((DiscordMember) Context.User).DisplayName, member.Mention), embed);
        }

        [Command("pout")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task PoutAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("pout"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPout, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsPoutAt, ((DiscordMember) Context.User).DisplayName, value))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }
        
        [Command("goodmorning", "morning")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task GoodMorningAsync()
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetImageAsync("good_morning"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} rias.gg"
                }
            };
            
            await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGoodMorning, Context.User.Mention), embed);
        }

        [Command("sleepy", "sleep", "goodnight")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task SleepyAsync()
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("sleepy"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };
            
            await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSleepy, Context.User.Mention), embed);
        }
        
        [Command("baka", "idiot")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task BakaAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("baka"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBaka, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBakaMember,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("baka")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task BakaAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("baka"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBaka, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsBakaMember, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }
        
        [Command("bang")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task BangAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("bang"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBangYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBangedBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("bang")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task BangAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("bang"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBangYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsBangedBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }
        
        [Command("punch")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(1)]
        public async Task PunchAsync([Remainder] DiscordMember member)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("punch"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPunchYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPunchedBy,
                    member.Mention, ((DiscordMember) Context.User).DisplayName), embed);
        }

        [Command("punch")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        [Priority(0)]
        public async Task PunchAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
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
                ImageUrl = await Service.GetReactionAsync("punch"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPunchYou, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent(GetText(Localization.ReactionsPunchedBy, value, ((DiscordMember) Context.User).DisplayName))
                    .WithEmbed(embed)
                    .WithAllowedMentions(Context.Message.MentionedUsers.Select(x => (IMention) new UserMention(x))));
        }
        
        [Command("shrug")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task ShrugAsync()
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("shrug"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };
            
            await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsShrug, Context.User.Mention), embed);
        }
        
        [Command("handholding")]
        [Context(ContextType.Guild)]
        [Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task HandholdingAsync([Remainder] DiscordMember? member = null)
        {
            if (string.IsNullOrEmpty(Configuration.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionAsync("handholding"),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = $"{GetText(Localization.ReactionsPoweredBy)} weeb.sh"
                }
            };

            if (member is null || member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHandholding, Context.User.Mention), embed);
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHandholdingMember,
                    ((DiscordMember) Context.User).DisplayName, member.Mention), embed);
        }

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
    }
}