using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Linq;
using Cleverbot.Net;
using Discord;
using RiasBot.Modules.Xp.Services;
using System.Collections.Concurrent;
using RiasBot.Extensions;

namespace RiasBot.Services
{
    public class CommandHandler : IRService
    {
        private readonly DiscordShardedClient _discord;
        private readonly CommandService _commands;
        private readonly IBotCredentials _creds;
        private readonly IServiceProvider _provider;
        private readonly DbService _db;
        private readonly XpService _xpService;
        private readonly BotService _botService;

        public string _prefix;

        public CommandHandler(DiscordShardedClient discord, CommandService commands, IBotCredentials creds, IServiceProvider provider, DbService db, XpService xpService, BotService botService)
        {
            _discord = discord;
            _commands = commands;
            _creds = creds;
            _provider = provider;
            _db = db;
            _xpService = xpService;
            _botService = botService;

            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;     // Ensure the message is from a user/bot
            if (msg == null) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;     // Ignore self when checking commands
            if (msg.Author.IsBot) return;       // Ignore other bots

            var context = new ShardedCommandContext(_discord, msg);     // Create the command context

            await _botService.AddAssignableRole(context.Guild, (IGuildUser)context.User);
            await GiveXp(context, msg);
            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(x => x.UserId == msg.Author.Id).FirstOrDefault();
                if (!context.IsPrivate)
                {
                    var guild = db.Guilds.Where(x => x.GuildId == context.Guild.Id).FirstOrDefault();
                    try
                    {
                        if (!String.IsNullOrEmpty(guild.Prefix))
                            _prefix = guild.Prefix;
                        else
                            _prefix = _creds.Prefix;
                    }
                    catch
                    {
                        _prefix = _creds.Prefix;
                    }
                }
                else
                {
                    _prefix = _creds.Prefix;
                }
                if (userDb != null)
                    if (userDb.IsBanned)
                        return;     //banned users will cannot use the commands
            }

            int argPos = 0;     // Check if the message has a valid command prefix

            if (msg.HasStringPrefix(_prefix, ref argPos) || msg.HasStringPrefix("rias ", ref argPos)
                || msg.HasStringPrefix("Rias ", ref argPos)
                || (msg.HasMentionPrefix(context.Client.CurrentUser, ref argPos)))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider).ConfigureAwait(false);

                if (result.IsSuccess)
                    RiasBot.commandsRun++;
                else if (result.Error == CommandError.UnmetPrecondition)
                {
                    await Task.Factory.StartNew(() => SendErrorResult(msg, result)).ConfigureAwait(false);
                }
            }
        }

        private async Task GiveXp(ShardedCommandContext context, SocketUserMessage msg)
        {
            using (var db = _db.GetDbContext())
            {
                var userDb = db.Users.Where(x => x.UserId == msg.Author.Id).FirstOrDefault();
                if (!context.IsPrivate)
                {
                    try
                    {
                        var socketGuildUser = context.Guild.GetUser(_discord.CurrentUser.Id);
                        var preconditions = socketGuildUser.GetPermissions((IGuildChannel)context.Channel);
                        if (preconditions.SendMessages)
                        {
                            if (userDb != null)
                            {
                                if (!userDb.IsBlacklisted)
                                    await _xpService.XpUserMessage((IGuildUser)msg.Author, (ITextChannel)context.Channel);
                            }
                            else
                                await _xpService.XpUserMessage((IGuildUser)msg.Author, (ITextChannel)context.Channel);
                            await _xpService.XpUserGuildMessage(context.Guild, (IGuildUser)msg.Author, (ITextChannel)context.Channel, true);
                        }
                        else
                        {
                            if (userDb != null)
                            {
                                if (!userDb.IsBlacklisted)
                                    await _xpService.XpUserMessage((IGuildUser)msg.Author, (ITextChannel)context.Channel);
                            }
                            else
                                await _xpService.XpUserMessage((IGuildUser)msg.Author, (ITextChannel)context.Channel);
                            await _xpService.XpUserGuildMessage(context.Guild, (IGuildUser)msg.Author, (ITextChannel)context.Channel);
                            return;
                        }
                    }
                    catch
                    {

                    }
                }
            }
        }

        private async Task SendErrorResult(SocketUserMessage msg, IResult result)
        {
            var timeoutMsg = await msg.Channel.SendErrorEmbed(result.ErrorReason).ConfigureAwait(false);
            await Task.Delay(10000);
            await timeoutMsg.DeleteAsync().ConfigureAwait(false);
        }
    }
}
