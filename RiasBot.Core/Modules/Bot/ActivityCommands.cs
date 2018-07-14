using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RiasBot.Modules.Bot
{
    public partial class Bot
    {
        public class ActivityCommands : RiasSubmodule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            private readonly DbService _db;
            private readonly DiscordShardedClient _client;
            private readonly BotService _botService;

            public ActivityCommands(CommandHandler ch, CommandService service, DbService db, DiscordShardedClient client, BotService botService)
            {
                _ch = ch;
                _service = service;
                _db = db;
                _client = client;
                _botService = botService;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Activity(string type = null, [Remainder]string name = null)
            {
                try
                {
                    _botService.Status.Dispose();
                }
                catch
                {

                }

                name = name ?? "";
                type = type?.ToLower();
                if (type is null)
                    await _client.SetActivityAsync(new Game("", ActivityType.Playing)).ConfigureAwait(false);

                switch (type)
                {
                    case "playing":
                        await _client.SetActivityAsync(new Game(name, ActivityType.Playing)).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"Activity status set to {Format.Bold($"Playing {name}")}");
                        break;
                    case "listening":
                        await _client.SetActivityAsync(new Game(name, ActivityType.Listening)).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"Activity status set to {Format.Bold($"Listening to {name}")}");
                        break;
                    case "watching":
                        await _client.SetActivityAsync(new Game(name, ActivityType.Watching)).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed($"Activity status set to {Format.Bold($"Watching {name}")}");
                        break;
                }
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireOwner]
            public async Task ActivityRotate(int time, [Remainder]string status)
            {
                var statuses = status.Split('\n');

                _botService.Statuses = statuses;
                _botService.Status = new Timer(async _ => await _botService.StatusRotate(), null, 0, time * 1000);

                await Context.Channel.SendConfirmationEmbed($"Activity status rotation set: {time} seconds\n{String.Join("\n", statuses)}");
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireOwner]
            public async Task Status(string status)
            {
                status = status.ToLowerInvariant();
                switch (status)
                {
                    case "online":
                        await ((DiscordShardedClient)Context.Client).SetStatusAsync(UserStatus.Online);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("Online")}");
                        break;
                    case "idle":
                        await ((DiscordShardedClient)Context.Client).SetStatusAsync(UserStatus.Idle);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("Idle")}");
                        break;
                    case "afk":
                        await ((DiscordShardedClient)Context.Client).SetStatusAsync(UserStatus.AFK);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("AFK")}");
                        break;
                    case "donotdisturb":
                        await ((DiscordShardedClient)Context.Client).SetStatusAsync(UserStatus.DoNotDisturb);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("DoNotDisturb")}");
                        break;
                    case "dnd":
                        await ((DiscordShardedClient)Context.Client).SetStatusAsync(UserStatus.DoNotDisturb);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("DoNotDisturb")}");
                        break;
                    case "invisible":
                        await ((DiscordShardedClient)Context.Client).SetStatusAsync(UserStatus.Invisible);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("Invisible")}");
                        break;
                }
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireOwner]
            public async Task Streaming(string url = null, [Remainder]string name = null)
            {
                _botService.Status?.Dispose();
                var game = new StreamingGame(name, url);
                await _client.SetActivityAsync(game).ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed($"Activity status set to {Format.Bold($"Streaming {name}")}");
            }
        }
    }
}
