using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Reactions
{
    [Name("Reactions")]
    public class Reactions : RiasModule<ReactionsService>
    {
        public Reactions(IServiceProvider services) : base(services)
        {
        }

        [Command("pat"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task PatAsync([Remainder] SocketGuildUser user)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pat"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("PatYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("PattedBy", user.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("pat"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task PatAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("pat"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("PatYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("PattedBy", value, Context.User), embed: embed.Build());
        }
        
        [Command("hug"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task HugAsync([Remainder] SocketGuildUser user)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("hug"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("HugYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("HuggedBy", user.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("hug"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task HugAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("hug"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("HugYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("HuggedBy", value, Context.User), embed: embed.Build());
        }
        
        [Command("kiss"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task KissAsync([Remainder] SocketGuildUser user)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("kiss"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("KissYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("KissedBy", user.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("kiss"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task KissAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("kiss"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("KissYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("KissedBy", value, Context.User), embed: embed.Build());
        }
        
        [Command("lick"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task LickAsync([Remainder] SocketGuildUser user)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("lick"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("LickYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("LickedBy", user.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("lick"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task LickAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("lick"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("LickYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("LickedBy", value, Context.User), embed: embed.Build());
        }
        
        [Command("cuddle"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task CuddleAsync([Remainder] SocketGuildUser user)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("cuddle"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("CuddleYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("CuddledBy", user.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("cuddle"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task CuddleAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("cuddle"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("CuddleYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("CuddledBy", value, Context.User), embed: embed.Build());
        }
        
        [Command("bite"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task BiteAsync([Remainder] SocketGuildUser user)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("bite"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("BiteYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("BittenBy", user.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("bite"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task BiteAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("bite"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("BiteYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("BittenBy", value, Context.User), embed: embed.Build());
        }
        
        [Command("slap"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task SlapAsync([Remainder] SocketGuildUser user)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("slap"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("SlapYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("SlappedBy", user.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("slap"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task SlapAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("slap"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("SlapYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("SlappedBy", value, Context.User), embed: embed.Build());
        }
        
        [Command("cry"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
        public async Task CryAsync()
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("cry"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            await Context.Channel.SendMessageAsync(GetText("DontCry", Context.User.Mention), embed: embed.Build());
        }
        
        [Command("grope"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task GropeAsync([Remainder] SocketGuildUser user)
        {
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetGropeUrlAsync(),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} riasbot.me"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("GropeYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("GropedBy", user.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("grope"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task GropeAsync([Remainder] string? value = null)
        {
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetGropeUrlAsync(),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} riasbot.me"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("GropeYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("GropedBy", value, Context.User), embed: embed.Build());
        }
        
        [Command("blush"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser)]
        public async Task BlushAsync()
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("blush"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            await Context.Channel.SendMessageAsync(GetText("Blush", Context.User.Mention), embed: embed.Build());
        }
        
        [Command("dance"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task DanceAsync([Remainder] SocketGuildUser user)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("dance"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("Dance", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("DanceTogether", Context.User.Mention, user.Mention), embed: embed.Build());
        }
        
        [Command("dance"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task DanceAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("dance"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("Dance", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("DanceTogether", Context.User.Mention, value), embed: embed.Build());
        }
        
        [Command("poke"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
        Priority(1)]
        public async Task PokeAsync([Remainder] SocketGuildUser user)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("poke"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };

            if (user.Id == Context.User.Id)
                await Context.Channel.SendMessageAsync(GetText("PokeYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("PokedBy", user.Mention, Context.User), embed: embed.Build());
        }
        
        [Command("poke"), Context(ContextType.Guild),
         Cooldown(2, 5, CooldownMeasure.Seconds, BucketType.GuildUser),
         Priority(0)]
        public async Task PokeAsync([Remainder] string? value = null)
        {
            if (string.IsNullOrEmpty(Credentials.WeebServicesToken))
            {
                await ReplyErrorAsync("NoWeebApi");
                return;
            }

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                ImageUrl = await Service.GetReactionUrlAsync("poke"),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"{GetText("PoweredBy")} weeb.sh"
                }
            };
            
            if (value is null)
                await Context.Channel.SendMessageAsync(GetText("PokeYou", Context.User.Mention), embed: embed.Build());
            else
                await Context.Channel.SendMessageAsync(GetText("PokedBy", value, Context.User), embed: embed.Build());
        }
    }
}