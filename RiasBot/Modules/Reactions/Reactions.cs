using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using RiasBot.Modules.Reactions.Services;

namespace RiasBot.Modules.Reactions
{
    public partial class Reactions : RiasModule<ReactionsService>
    {
        public Reactions()
        {
            
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Pat([Remainder]IGuildUser user)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.patList));

            await Context.Channel.SendMessageAsync($"{user.Mention} you have been patted by {Format.Bold(Context.User.ToString())} <3",
                embed: embed.Build());
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Pat([Remainder]string user = null)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.patList));

            if (user is null)
            {
                await Context.Channel.SendMessageAsync($"{Format.Italics("Pat pat")} {Context.User.Mention} <3",
                    embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendMessageAsync($"{String.Join(" ", users)} you have been patted by {Format.Bold(Context.User.ToString())} <3",
                    embed: embed.Build()).ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Hug([Remainder]IGuildUser user)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.hugList));

            await Context.Channel.SendMessageAsync($"{user.Mention} you have been hugged by {Format.Bold(Context.User.ToString())} <3",
                embed: embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Hug([Remainder]string user = null)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.hugList));

            if (user is null)
            {
                await Context.Channel.SendMessageAsync($"{Format.Italics("Hugs")} {Context.User.Mention} <3",
                    embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendMessageAsync($"{String.Join(" ", users)} you have been hugged by {Format.Bold(Context.User.ToString())} <3",
                    embed: embed.Build()).ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Kiss([Remainder]IGuildUser user)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.kissList));

            await Context.Channel.SendMessageAsync($"{user.Mention} you have been kissed by {Format.Bold(Context.User.ToString())} ❤️",
                embed: embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Kiss([Remainder]string user = null)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.kissList));

            if (user is null)
            {
                await Context.Channel.SendMessageAsync($"{Format.Italics("Kiss")} {Context.User.Mention} ❤️",
                    embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendMessageAsync($"{String.Join(" ", users)} you have been kissed by {Format.Bold(Context.User.ToString())} ❤️",
                    embed: embed.Build()).ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Bite([Remainder]IGuildUser user)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.biteList));

            await Context.Channel.SendMessageAsync($"{user.Mention} you have been bitten by {Format.Bold(Context.User.ToString())}",
                embed: embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Bite([Remainder]string user = null)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.biteList));

            if (user is null)
            {
                await Context.Channel.SendMessageAsync($"{Format.Italics("Bite")} {Context.User.Mention}",
                    embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendMessageAsync($"{String.Join(" ", users)} you have been bitten by {Format.Bold(Context.User.ToString())}",
                    embed: embed.Build()).ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Lick([Remainder]IGuildUser user)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.lickList));

            await Context.Channel.SendMessageAsync($"{user.Mention} you have been licked by {Format.Bold(Context.User.ToString())}, lewd",
                embed: embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Lick([Remainder]string user = null)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.lickList));

            if (user is null)
            {
                await Context.Channel.SendMessageAsync($"{Format.Italics("Licks")} {Context.User.Mention} lewd",
                    embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendMessageAsync($"{String.Join(" ", users)} you have been licked by {Format.Bold(Context.User.ToString())} lewd",
                    embed: embed.Build()).ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Slap([Remainder]IGuildUser user)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.slapList));

            await Context.Channel.SendMessageAsync($"{user.Mention} you have been slapped by {Format.Bold(Context.User.ToString())}",
                embed: embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Slap([Remainder]string user = null)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.slapList));

            if (user is null)
            {
                await Context.Channel.SendMessageAsync($"{Format.Italics("Slaps")} {Context.User.Mention}",
                    embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendMessageAsync($"{String.Join(" ", users)} you have been slapped by {Format.Bold(Context.User.ToString())}",
                    embed: embed.Build()).ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Cry()
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.cryList));

            await Context.Channel.SendMessageAsync($"Aww don't cry {Context.Message.Author.Mention}. I will {Format.Italics("pat")} and {Format.Italics("hug")} you <3",
                embed: embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Grope([Remainder]IGuildUser user)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.gropeList));

            await Context.Channel.SendMessageAsync($"{user.Mention} you have been groped by {Format.Bold(Context.User.ToString())}",
                embed: embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        [Ratelimit(2, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task Grope([Remainder]string user = null)
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
            embed.WithImageUrl(_service.GetImage(_service.gropeList));

            if (user is null)
            {
                await Context.Channel.SendMessageAsync($"{Format.Italics("Gropes")} {Context.User.Mention}",
                    embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendMessageAsync($"{String.Join(" ", users)} you have been groped by {Format.Bold(Context.User.ToString())}",
                    embed: embed.Build()).ConfigureAwait(false);
            }
        }
    }
}
