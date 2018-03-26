using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotsList.Api;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration
{
    public partial class Administration
    {
        public class BotCommands : RiasSubmodule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            private readonly IServiceProvider _provider;
            private readonly DbService _db;
            private readonly DiscordSocketClient _client;
            private readonly BotService _botService;
            private readonly IBotCredentials _creds;

            public BotCommands(CommandHandler ch, CommandService service, IServiceProvider provider, DbService db, DiscordSocketClient client, BotService botService, IBotCredentials creds)
            {
                _ch = ch;
                _service = service;
                _provider = provider;
                _db = db;
                _client = client;
                _botService = botService;
                _creds = creds;
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task LeaveGuild(ulong id)
            {
                var guild = await Context.Client.GetGuildAsync(id).ConfigureAwait(false);
                if (guild != null)
                {
                    var usersGuild = await guild.GetUsersAsync();
                    var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                    embed.WithDescription($"Leabing {Format.Bold(guild.Name)}");
                    embed.AddField("Id", guild.Id, true).AddField("Users", usersGuild.Count, true);

                    await ReplyAsync("", embed: embed.Build()).ConfigureAwait(false);

                    await guild.LeaveAsync().ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Activity(string type = null, [Remainder]string name = null)
            {
                try
                {
                    _botService.status.Dispose();
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

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task ActivityRotate(int time, [Remainder]string status)
            {
                var statuses = status.Split('\n');

                _botService.statuses = statuses;
                _botService.status = new Timer(async _ => await _botService.StatusRotate(), null, 0, time * 1000);

                await Context.Channel.SendConfirmationEmbed($"Activity status rotation set: {time} seconds\n{String.Join("\n", statuses)}");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Status(string status)
            {
                status = status.ToLowerInvariant();
                switch (status)
                {
                    case "online":
                        await ((DiscordSocketClient)Context.Client).SetStatusAsync(UserStatus.Online);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("Online")}");
                        break;
                    case "idle":
                        await ((DiscordSocketClient)Context.Client).SetStatusAsync(UserStatus.Idle);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("Idle")}");
                        break;
                    case "afk":
                        await ((DiscordSocketClient)Context.Client).SetStatusAsync(UserStatus.AFK);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("AFK")}");
                        break;
                    case "donotdisturb":
                        await ((DiscordSocketClient)Context.Client).SetStatusAsync(UserStatus.DoNotDisturb);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("DoNotDisturb")}");
                        break;
                    case "dnd":
                        await ((DiscordSocketClient)Context.Client).SetStatusAsync(UserStatus.DoNotDisturb);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("DoNotDisturb")}");
                        break;
                    case "invisible":
                        await ((DiscordSocketClient)Context.Client).SetStatusAsync(UserStatus.Invisible);
                        await Context.Channel.SendConfirmationEmbed($"Status set to {Format.Code("Invisible")}");
                        break;
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Streaming(string url = null, [Remainder]string name = null)
            {
                try
                {
                    _botService.status.Dispose();
                }
                catch
                {

                }

                name = name ?? "";
                url = url ?? "";

                var game = new Game(name, ActivityType.Streaming);
                game = new StreamingGame(name, url);

                await _client.SetActivityAsync(game).ConfigureAwait(false);
                await Context.Channel.SendConfirmationEmbed($"Activity status set to {Format.Bold($"Streaming {name}")}");
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Die()
            {
                await Context.Channel.SendConfirmationEmbed("Shutting down...").ConfigureAwait(false);
                await Context.Client.StopAsync().ConfigureAwait(false);
                Environment.Exit(0);
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Send(string id, [Remainder]string message)
            {
                IGuild guild;
                IUser user;
                ITextChannel channel;

                var embed = Extensions.Extensions.EmbedFromJson(message);

                if (id.Contains("|"))
                {
                    try
                    {
                        var ids = id.Split('|');
                        string guildId = ids[0];
                        string channelId = ids[1];

                        guild = await Context.Client.GetGuildAsync(Convert.ToUInt64(guildId)).ConfigureAwait(false);
                        channel = await guild.GetTextChannelAsync(Convert.ToUInt64(channelId)).ConfigureAwait(false);
                        if (embed is null)
                            await channel.SendMessageAsync(message).ConfigureAwait(false);
                        else
                            await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed("Message sent!").ConfigureAwait(false);
                    }
                    catch
                    {
                        await Context.Channel.SendErrorEmbed("I couldn't find the guild or the channel");
                    }
                }
                else
                {
                    try
                    {
                        user = await Context.Client.GetUserAsync(Convert.ToUInt64(id));
                        if (embed is null)
                            await user.SendMessageAsync(message).ConfigureAwait(false);
                        else
                            await user.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                        await Context.Channel.SendConfirmationEmbed("Message sent!").ConfigureAwait(false);
                    }
                    catch
                    {
                        await Context.Channel.SendErrorEmbed("I couldn't find the user").ConfigureAwait(false);
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Name([Remainder]string name)
            {
                try
                {
                    await Context.Client.CurrentUser.ModifyAsync(u => u.Username = name);
                    await ReplyAsync("New name " + name);
                }
                catch
                {
                    await ReplyAsync("You need to wait 2 hours to change your name again.");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Avatar(string url)
            {
                try
                {
                    var http = new HttpClient();
                    var res = await http.GetStreamAsync(new Uri(url));
                    var ms = new MemoryStream();
                    res.CopyTo(ms);
                    ms.Position = 0;
                    await Context.Client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(ms));
                }
                catch
                {

                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Edit(string channelMessage, [Remainder]string message)
            {
                try
                {
                    var ids = channelMessage.Split("|");
                    UInt64.TryParse(ids[0], out ulong channelId);
                    UInt64.TryParse(ids[1], out ulong messageId);

                    var channel = await Context.Client.GetChannelAsync(channelId).ConfigureAwait(false);
                    var msg = await ((ITextChannel)channel).GetMessageAsync(messageId).ConfigureAwait(false);
                    await ((IUserMessage)msg).ModifyAsync(x => x.Content = message).ConfigureAwait(false);
                    await Context.Channel.SendConfirmationEmbed("Message edited!");
                }
                catch
                {
                    await Context.Channel.SendConfirmationEmbed("I couldn't find the channel/message!");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Dbl(int page = 1)
            {
                AuthDiscordBotListApi dblApi = new AuthDiscordBotListApi(_creds.ClientId, _creds.DiscordBotsListApiKey);
                var dblSelfBot = await dblApi.GetMeAsync().ConfigureAwait(false);
                var dbls = await dblSelfBot.GetVotersAsync(1).ConfigureAwait(false);

                string[] voters = new string[dbls.Count];
                int index = 0;
                foreach (var dbl in dbls)
                {
                    voters[index] = $"#{index+1} {dbl.Username}#{dbl.Discriminator} ({dbl.Id})";
                    index++;
                }
                if (voters.Count() > 0)
                    await Context.Channel.SendPaginated((DiscordSocketClient)Context.Client, "List of voters today", voters, 10, page - 1).ConfigureAwait(false);
                else
                    await Context.Channel.SendErrorEmbed("No voters today.").ConfigureAwait(false);
            }
        }
    }
}
