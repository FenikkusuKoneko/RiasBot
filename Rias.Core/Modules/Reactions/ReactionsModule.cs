using System;
using System.Threading.Tasks;
using Disqord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Reactions
{
    [Name("Reactions")]
    public class ReactionsModule : RiasModule<ReactionsService>
    {
        public ReactionsModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        private const int ReactionLimit = 1500;
        
        [Command("pat"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
        Priority(1)]
        public async Task PatAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pat"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPatYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPattedBy, member.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("pat"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pat"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPatYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPattedBy, value.Replace("@everyone", "everyone"), Context.User), embed: embed.Build());
        }
        
        [Command("hug"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
        Priority(1)]
        public async Task HugAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("hug"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHugYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHuggedBy, member.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("hug"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("hug"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHugYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsHuggedBy, value.Replace("@everyone", "everyone"), Context.User), embed: embed.Build());
        }
        
        [Command("kiss"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
        Priority(1)]
        public async Task KissAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("kiss"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissedBy, member.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("kiss"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("kiss"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsKissedBy, value.Replace("@everyone", "everyone"), Context.User), embed: embed.Build());
        }
        
        [Command("lick"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
        Priority(1)]
        public async Task LickAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("lick"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickedBy, member.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("lick"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("lick"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsLickedBy, value.Replace("@everyone", "everyone"), Context.User), embed: embed.Build());
        }
        
        [Command("cuddle"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
        Priority(1)]
        public async Task CuddleAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("cuddle"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddleYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddledBy, member.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("cuddle"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("cuddle"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddleYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsCuddledBy, value.Replace("@everyone", "everyone"), Context.User), embed: embed.Build());
        }
        
        [Command("bite"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
        Priority(1)]
        public async Task BiteAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("bite"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBiteYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBittenBy, member.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("bite"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("bite"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBiteYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBittenBy, value.Replace("@everyone", "everyone"), Context.User), embed: embed.Build());
        }
        
        [Command("slap"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
        Priority(1)]
        public async Task SlapAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("slap"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlapYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlappedBy, member.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("slap"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("slap"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlapYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSlappedBy, value.Replace("@everyone", "everyone"), Context.User), embed: embed.Build());
        }
        
        [Command("cry"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task CryAsync()
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("cry"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDontCry, Context.User.Mention), embed: embed.Build());
        }
        
        [Command("grope"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(1)]
        public async Task GropeAsync([Remainder] CachedMember member)
        {
            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetGropeUrlAsync(),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} riasbot.me")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropeYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropedBy, member.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("grope"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
        public async Task GropeAsync([Remainder] string? value = null)
        {
            if (value != null && value.Length > ReactionLimit)
            {
                await ReplyErrorAsync(Localization.ReactionsLimit);
                return;
            }
            
            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetGropeUrlAsync(),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} riasbot.me")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropeYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsGropedBy, value.Replace("@everyone", "everyone"), Context.User), embed: embed.Build());
        }
        
        [Command("blush"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task BlushAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("blush"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlush, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlushAt, Context.User, member.Mention), embed: embed.Build());
        }
        
        [Command("blush"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("blush"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlush, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsBlushAt, Context.User, value.Replace("@everyone", "everyone")), embed: embed.Build());
        }
        
        [Command("dance"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
        Priority(1)]
        public async Task DanceAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("dance"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDance, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDanceTogether, Context.User.Mention, member.Mention), embed: embed.Build());
        }
        
        [Command("dance"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("dance"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDance, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsDanceTogether, Context.User.Mention, value.Replace("@everyone", "everyone")), embed: embed.Build());
        }
        
        [Command("poke"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
        Priority(1)]
        public async Task PokeAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("poke"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };

            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokeYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokedBy, member.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("poke"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member),
         Priority(0)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("poke"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokeYou, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPokedBy, value.Replace("@everyone", "everyone"), Context.User), embed: embed.Build());
        }
        
        [Command("pout"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task PoutAsync([Remainder] CachedMember member)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pout"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (member.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPout, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPoutAt, Context.User, member.Mention), embed: embed.Build());
        }
        
        [Command("pout"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
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

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pout"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPout, Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsPoutAt, Context.User, value.Replace("@everyone", "everyone")), embed: embed.Build());
        }
        
        [Command("sleepy"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.Member)]
        public async Task SleepyAsync()
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync(Localization.ReactionsNoWeebApi);
                return;
            }

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("sleepy"),
                Footer = new LocalEmbedFooterBuilder().WithText($"{GetText(Localization.ReactionsPoweredBy)} weeb.sh")
            };
            
            await Context.Channel.SendMessageAsync(GetText(Localization.ReactionsSleepy, Context.User.Mention), embed: embed.Build());
        }
    }
}