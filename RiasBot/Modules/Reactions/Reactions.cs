using Discord;
using Discord.Commands;
using RiasBot.Commons.Attributes;
using RiasBot.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RiasBot.Modules.Reactions
{
    public partial class Reactions : RiasModule
    {
        private readonly CommandHandler _ch;
        private readonly CommandService _service;

        public string path = "assets/reactions/";

        public Reactions(CommandHandler ch, CommandService service)
        {
            _ch = ch;
            _service = service;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Pat([Remainder]IGuildUser user)
        {
            var gifs = Directory.GetFiles(path + "pat", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                $"{user.Mention} you have been patted by {Format.Bold(Context.User.ToString())} <3");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Pat([Remainder]string user = null)
        {
            var gifs = Directory.GetFiles(path + "pat", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            if (user is null)
            {
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                $"{Format.Italics("Pat pat")} {Context.User.Mention} <3").ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{String.Join(" ", users)} you have been patted by {Format.Bold(Context.User.ToString())} <3").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Hug([Remainder]IGuildUser user)
        {
            var gifs = Directory.GetFiles(path + "hug", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                $"{user.Mention} you have been hugged by {Format.Bold(Context.User.ToString())} <3").ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Hug([Remainder]string user = null)
        {
            var gifs = Directory.GetFiles(path + "hug", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            if (user is null)
            {
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{Format.Italics("Hugs")} {Context.User.Mention} <3").ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{String.Join(" ", users)} you have been hugged by {Format.Bold(Context.User.ToString())} <3").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Kiss([Remainder]IGuildUser user)
        {
            var gifs = Directory.GetFiles(path + "kiss", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                $"{user.Mention} you have been kissed by {Format.Bold(Context.User.ToString())} ❤️").ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Kiss([Remainder]string user = null)
        {
            var gifs = Directory.GetFiles(path + "kiss", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            if (user is null)
            {
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{Format.Italics("Kiss")} {Context.User.Mention} ❤️").ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{String.Join(" ", users)} you have been kissed by {Format.Bold(Context.User.ToString())} ❤️").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Bite([Remainder]IGuildUser user)
        {
            var gifs = Directory.GetFiles(path + "bite", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                $"{user.Mention} you have been bitten by {Format.Bold(Context.User.ToString())}").ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Bite([Remainder]string user = null)
        {
            var gifs = Directory.GetFiles(path + "bite", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            if (user is null)
            {
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{Format.Italics("Bite")} {Context.User.Mention}").ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{String.Join(" ", users)} you have been bitten by {Format.Bold(Context.User.ToString())}").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Lick([Remainder]IGuildUser user)
        {
            var gifs = Directory.GetFiles(path + "lick", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                $"{user.Mention} you have been licked by {Format.Bold(Context.User.ToString())}, lewd").ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Lick([Remainder]string user = null)
        {
            var gifs = Directory.GetFiles(path + "lick", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            if (user is null)
            {
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{Format.Italics("Licks")} {Context.User.Mention} lewd").ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{String.Join(" ", users)} you have been licked by {Format.Bold(Context.User.ToString())} lewd").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Slap([Remainder]IGuildUser user)
        {
            var gifs = Directory.GetFiles(path + "slap", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                $"{user.Mention} you have been slapped by {Format.Bold(Context.User.ToString())}").ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Slap([Remainder]string user = null)
        {
            var gifs = Directory.GetFiles(path + "slap", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            if (user is null)
            {
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{Format.Italics("Slaps")} {Context.User.Mention}").ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{String.Join(" ", users)} you have been slapped by {Format.Bold(Context.User.ToString())}").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Cry()
        {
            var gifs = Directory.GetFiles(path + "cry", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                $"Aww don't cry {Context.Message.Author.Mention}. I will {Format.Italics("pat")} and {Format.Italics("hug")} you <3").ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(1)]
        public async Task Grope([Remainder]IGuildUser user)
        {
            var gifs = Directory.GetFiles(path + "grope", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                $"{user.Mention} you have been groped by {Format.Bold(Context.User.ToString())}").ConfigureAwait(false);
        }

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireContext(ContextType.Guild)]
        [Priority(0)]
        public async Task Grope([Remainder]string user = null)
        {
            var gifs = Directory.GetFiles(path + "grope", "*.gif");

            Random rnd = new Random((int)DateTime.UtcNow.Ticks);

            if (user is null)
            {
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{Format.Italics("Gropes")} {Context.User.Mention}").ConfigureAwait(false);
            }
            else
            {
                var users = user.Split(" ");
                await Context.Channel.SendFileAsync(gifs[rnd.Next(gifs.Length)],
                    $"{String.Join(" ", users)} you have been groped by {Format.Bold(Context.User.ToString())}").ConfigureAwait(false);
            }
        }
    }
}
