using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Modules.Reactions.Services;
using RiasBot.Modules.Searches.Services;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RiasBot.Modules.Bot
{
    public partial class Bot : RiasModule
    {
        private readonly CommandHandler _ch;
        private readonly CommandService _service;
        private readonly IServiceProvider _provider;
        private readonly DbService _db;
        private readonly DiscordShardedClient _client;
        private readonly DiscordRestClient _restClient;
        private readonly BotService _botService;
        private readonly InteractiveService _is;
        private readonly IBotCredentials _creds;
        private readonly ReactionsService _reactionsService;

        public Bot(CommandHandler ch, CommandService service, IServiceProvider provider, DbService db, DiscordShardedClient client, DiscordRestClient restClient,
            BotService botService, InteractiveService interactiveService, IBotCredentials creds, ReactionsService reactionsService)
        {
            _ch = ch;
            _service = service;
            _provider = provider;
            _db = db;
            _client = client;
            _restClient = restClient;
            _botService = botService;
            _is = interactiveService;
            _creds = creds;
            _reactionsService = reactionsService;
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
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithDescription($"Leaving {Format.Bold(guild.Name)}");
                embed.AddField("Id", guild.Id, true).AddField("Users", usersGuild.Count, true);

                await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);

                await guild.LeaveAsync().ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task Update()
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

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireOwner]
        public async Task Edit(string channelMessage, [Remainder]string message)
        {
            try
            {
                var embed = Extensions.Extensions.EmbedFromJson(message);
                var ids = channelMessage.Split("|");
                UInt64.TryParse(ids[0], out ulong channelId);
                UInt64.TryParse(ids[1], out ulong messageId);


                var channel = await Context.Client.GetChannelAsync(channelId).ConfigureAwait(false);
                var msg = await ((ITextChannel)channel).GetMessageAsync(messageId).ConfigureAwait(false);

                if (embed is null)
                    await ((IUserMessage)msg).ModifyAsync(x => x.Content = message).ConfigureAwait(false);
                else
                    await ((IUserMessage)msg).ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
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
        public async Task Dbl()
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            var votes = new List<string>();
            int index = 0;
            foreach (var vote in _botService.votesList)
            {
                var user = await Context.Client.GetUserAsync(vote.user);
                votes.Add($"#{index+1} {user?.ToString()} ({vote.user})");
                index++;
            }
            var pager = new PaginatedMessage
            {
                Title = "List of voters today",
                Color = new Color(RiasBot.GoodColor),
                Pages = votes,
                Options = new PaginatedAppearanceOptions
                {
                    ItemsPerPage = 15,
                    Timeout = TimeSpan.FromMinutes(1),
                    DisplayInformationIcon = false,
                    JumpDisplayOptions = JumpDisplayOptions.Never
                }

            };
            await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task UpdateImages()
        {
            await _reactionsService.UpdateImages("H1Pqa", _reactionsService.biteList);
            await _reactionsService.UpdateImages("woGOn", _reactionsService.cryList);
            await _reactionsService.UpdateImages("GdiXR", _reactionsService.gropeList);
            await _reactionsService.UpdateImages("KTkPe", _reactionsService.hugList);
            await _reactionsService.UpdateImages("CotHR", _reactionsService.kissList);
            await _reactionsService.UpdateImages("5cMDN", _reactionsService.lickList);
            await _reactionsService.UpdateImages("OQjWy", _reactionsService.patList);
            await _reactionsService.UpdateImages("AQoU8", _reactionsService.slapList);
            await _reactionsService.UpdateImages("Xqjh9UM", _reactionsService.cuddleList);

            await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} reactions images, updated");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        [Priority(1)]
        public async Task FindUser([Remainder]string user)
        {
            IUser getUser;
            bool mutualServers = false;
            if (UInt64.TryParse(user, out var id))
            {
                getUser = await _restClient.GetUserAsync(id).ConfigureAwait(false);
            }
            else
            {
                var userSplit = user.Split("#");
                if (userSplit.Length == 2)
                    getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
                else
                    getUser = null;
            }
            if (getUser is null)
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the user.").ConfigureAwait(false);
                return;
            }

            var guilds = await Context.Client.GetGuildsAsync().ConfigureAwait(false);
            mutualServers = guilds.Any(x => x.GetUserAsync(getUser.Id).GetAwaiter().GetResult() != null);

            string accountCreated = getUser.CreatedAt.UtcDateTime.ToUniversalTime().ToString("dd MMM yyyy hh:mm tt");

            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.AddField("Name", getUser, true).AddField("ID", getUser.Id, true);
            embed.AddField("Joined Discord", accountCreated, true).AddField("Mutual servers (probable)", (mutualServers) ? "true" : "false", true);
            try
            {
                embed.WithImageUrl(getUser.RealAvatarUrl(1024));
            }
            catch
            {
                embed.WithImageUrl(getUser.DefaultAvatarUrl());
            }
            await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task Evaluate([Remainder]string expression)
        {
            var globals = new Globals()
            {
                Context = Context,
                Client = _client,
                Handler = _ch,
                Service = _service,
                Database = _db
            };
            object result = null;
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithAuthor(Context.User);
            try
            {
                result = await CSharpScript.EvaluateAsync(expression,
                ScriptOptions.Default.WithReferences(typeof(RiasBot).Assembly).WithImports(new[] { "System", "System.Collections.Generic",
                    "System.Linq", "Discord" }), globals);

                embed.WithDescription("Success");
                embed.AddField("Code", Format.Code(expression, "csharp"));
                if (result != null)
                {
                    embed.AddField("Result", Format.Code(result.ToString(), "csharp"));
                    await Context.Channel.SendMessageAsync(embed: embed.Build());
                }
            }
            catch (Exception e)
            {
                embed.WithDescription("Failed");
                embed.AddField("CompilationErrorException", Format.Code(e.Message, "csharp"));
                await Context.Channel.SendMessageAsync(embed: embed.Build());
            }
            finally
            {
                if (result != null)
                    GC.Collect(GC.GetGeneration(result), GCCollectionMode.Optimized);
            }
        }

        public class Globals
        {
            public ICommandContext Context { get; set; }
            public DiscordShardedClient Client { get; set; }
            public CommandHandler Handler { get; set; }
            public CommandService Service { get; set; }
            public DbService Database;
        }
    }
}
