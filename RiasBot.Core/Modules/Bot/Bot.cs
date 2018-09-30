using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RiasBot.Modules.Music.Services;

namespace RiasBot.Modules.Bot
{
    public partial class Bot : RiasModule
    {
        private readonly CommandHandler _ch;
        private readonly CommandService _service;
        private readonly DbService _db;
        private readonly IBotCredentials _creds;
        private readonly DiscordShardedClient _client;
        private readonly DiscordRestClient _restClient;
        private readonly BotService _botService;
        private readonly InteractiveService _is;
        private readonly MusicService _musicService;
        private readonly VotesService _votesService;

        public Bot(CommandHandler ch, CommandService service, DbService db, IBotCredentials creds, DiscordShardedClient client, DiscordRestClient restClient,
            BotService botService, InteractiveService interactiveService, MusicService musicService, VotesService votesService)
        {
            _ch = ch;
            _service = service;
            _db = db;
            _creds = creds;
            _client = client;
            _restClient = restClient;
            _botService = botService;
            _is = interactiveService;
            _musicService = musicService;
            _votesService = votesService;
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
            await Context.Channel.SendConfirmationMessageAsync("Shutting down...").ConfigureAwait(false);

            foreach (var musicPlayer in _musicService.MPlayer)
            {
                await musicPlayer.Value.Leave(Context.Guild, null);
            }
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
                    var guildId = ids[0];
                    var channelId = ids[1];

                    guild = await Context.Client.GetGuildAsync(Convert.ToUInt64(guildId)).ConfigureAwait(false);
                    channel = await guild.GetTextChannelAsync(Convert.ToUInt64(channelId)).ConfigureAwait(false);
                    if (embed is null)
                        await channel.SendMessageAsync(message).ConfigureAwait(false);
                    else
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    await Context.Channel.SendConfirmationMessageAsync("Message sent!").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the guild or the channel");
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
                    await Context.Channel.SendConfirmationMessageAsync("Message sent!").ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorMessageAsync("I couldn't find the user").ConfigureAwait(false);
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
                UInt64.TryParse(ids[0], out var channelId);
                UInt64.TryParse(ids[1], out var messageId);


                var channel = await Context.Client.GetChannelAsync(channelId).ConfigureAwait(false);
                var msg = await ((ITextChannel)channel).GetMessageAsync(messageId).ConfigureAwait(false);

                if (embed is null)
                    await ((IUserMessage)msg).ModifyAsync(x => x.Content = message).ConfigureAwait(false);
                else
                    await ((IUserMessage)msg).ModifyAsync(x => x.Embed = embed.Build()).ConfigureAwait(false);
                await Context.Channel.SendConfirmationMessageAsync("Message edited!");
            }
            catch
            {
                await Context.Channel.SendConfirmationMessageAsync("I couldn't find the channel/message!");
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task Votes()
        {
            var votes = new List<string>();
            var index = 0;
            if (_votesService.VotesList != null)
            {
                foreach (var vote in _votesService.VotesList)
                {
                    var user = await Context.Client.GetUserAsync(vote.User);
                    votes.Add($"#{index+1} {user?.ToString()} ({vote.User})");
                    index++;
                }
                var pager = new PaginatedMessage
                {
                    Title = "List of voters in the past 12 hours",
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
            else
            {
                await Context.Channel.SendErrorMessageAsync("The votes manager is not configured!").ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        [Priority(1)]
        public async Task FindUser([Remainder]string user)
        {
            IUser getUser;
            var mutualServers = false;
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
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't find the user.").ConfigureAwait(false);
                return;
            }

            var guilds = await Context.Client.GetGuildsAsync().ConfigureAwait(false);
            mutualServers = guilds.Any(x => x.GetUserAsync(getUser.Id).GetAwaiter().GetResult() != null);

            var accountCreated = getUser.CreatedAt.UtcDateTime.ToUniversalTime().ToString("dd MMM yyyy hh:mm tt");

            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.AddField("Name", getUser, true).AddField("ID", getUser.Id, true);
            embed.AddField("Joined Discord", accountCreated, true).AddField("Mutual servers (probable)", (mutualServers) ? "true" : "false", true);
            embed.WithImageUrl(getUser.GetRealAvatarUrl());
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
            try
            {
                result = await CSharpScript.EvaluateAsync(expression,
                ScriptOptions.Default.WithReferences(typeof(RiasBot).Assembly).WithImports(new[] { "System", "System.Collections.Generic",
                    "System.Linq", "Discord", "System.Threading.Tasks" }), globals);

                embed.WithAuthor("Success", Context.User.GetRealAvatarUrl());
                embed.AddField("Code", Format.Code(expression, "csharp"));
                if (result != null)
                {
                    embed.AddField("Result", Format.Code(result.ToString(), "csharp"));
                    await Context.Channel.SendMessageAsync(embed: embed.Build());
                }
            }
            catch (Exception e)
            {
                embed.WithAuthor("Failed", Context.User.GetRealAvatarUrl());
                embed.AddField("CompilationErrorException", Format.Code(e.Message, "csharp"));
                await Context.Channel.SendMessageAsync(embed: embed.Build());
            }
            finally
            {
                if (result != null)
                    GC.Collect(GC.GetGeneration(result), GCCollectionMode.Optimized);
            }
        }

        [RiasCommand]
        [@Alias]
        [Description]
        [@Remarks]
        [RequireOwner]
        public async Task ExecuteSqlAsync([Remainder] string query)
        {
            var message = await Context.Channel.SendConfirmationMessageAsync("Do you want to execute this SQL query?").ConfigureAwait(false);
            var input = await _is.NextMessageAsync((ShardedCommandContext)Context, timeout: TimeSpan.FromMinutes(1));
            if (input != null)
            {
                if (input.Content.Equals("yes", StringComparison.InvariantCultureIgnoreCase))
                {
                    using (var db = _db.GetDbContext())
                    {
                        var transaction = await db.Database.BeginTransactionAsync().ConfigureAwait(false);
                        var rows = await db.Database.ExecuteSqlCommandAsync(query).ConfigureAwait(false);
                        transaction.Commit();
                        
                        var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                        embed.WithAuthor("Execute SQL Query", Context.User.GetRealAvatarUrl());
                        embed.WithDescription(Format.Code(query));
                        embed.AddField("Rows affected ", Format.Code(rows.ToString()));

                        await message.DeleteAsync().ConfigureAwait(false);
                        await Context.Message.DeleteAsync().ConfigureAwait(false);
                        await Context.Channel.SendMessageAsync(embed: embed.Build()).ConfigureAwait(false);
                    }
                }
                else
                {
                    await message.DeleteAsync().ConfigureAwait(false);
                    await Context.Channel.SendErrorMessageAsync("Execution aborted!").ConfigureAwait(false);
                }
            }
            else
            {
                await message.DeleteAsync().ConfigureAwait(false);
                await Context.Channel.SendErrorMessageAsync("Execution aborted!").ConfigureAwait(false);
            }
        }
        
        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task DownloadUsers()
        {
            await Context.Guild.DownloadUsersAsync();
            await Context.Channel.SendConfirmationMessageAsync($"All users downloaded from {Context.Guild.Name}");
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [RequireOwner]
        public async Task DownloadUsers(ulong guildId)
        {
            var guild = await Context.Client.GetGuildAsync(guildId).ConfigureAwait(false);
            await guild.DownloadUsersAsync();
            await Context.Channel.SendConfirmationMessageAsync($"All users downloaded from {guild.Name}");
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
