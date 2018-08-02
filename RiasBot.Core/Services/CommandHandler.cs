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
using System.ComponentModel;
using RiasBot.Extensions;
using RiasBot.Services.Database.Models;

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

        public string Prefix;

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
            if (!(s is SocketUserMessage msg)) return;      // Ensure the message is from a user/bot
            if (msg.Author.Id == _discord.CurrentUser.Id) return;     // Ignore self when checking commands
            if (msg.Author.IsBot) return;       // Ignore other bots

            var context = new ShardedCommandContext(_discord, msg);     // Create the command context

            await _botService.AddAssignableRole(context.Guild, (IGuildUser)context.User);
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == context.Guild.Id);
                var userDb = db.Users.FirstOrDefault(x => x.UserId == msg.Author.Id);

                Prefix = GetPrefix(context, guildDb);
                await GiveXp(context, msg, userDb);
                
                if (userDb != null)
                    if (userDb.IsBanned)
                        return;     //banned users will cannot use the commands

                var argPos = 0;     // Check if the message has a valid command prefix

                if (msg.HasStringPrefix(Prefix, ref argPos) || msg.HasStringPrefix("rias ", ref argPos)
                    || msg.HasStringPrefix("Rias ", ref argPos)
                    || (msg.HasMentionPrefix(context.Client.CurrentUser, ref argPos)))
                {
                    var result = await _commands.ExecuteAsync(context, argPos, _provider).ConfigureAwait(false);

                    if (guildDb != null)
                        if (guildDb.DeleteCommandMessage)
                            await msg.DeleteAsync().ConfigureAwait(false);

                    if (result.IsSuccess)
                        RiasBot.CommandsRun++;
                    else if (result.Error == CommandError.UnmetPrecondition ||
                             result.Error == CommandError.Exception)
                    {
                        await Task.Factory.StartNew(() => SendErrorResult(msg, result)).ConfigureAwait(false);
                    }
                }
                await db.SaveChangesAsync();
            }
        }

        private string GetPrefix(SocketCommandContext context, GuildConfig guildDb)
        {
            if (!context.IsPrivate)
            {
                if (guildDb != null)
                {
                    return !string.IsNullOrEmpty(guildDb.Prefix) ? guildDb.Prefix : _creds.Prefix;
                }
                else
                {
                    return _creds.Prefix;
                }
            }
            else
            {
                return _creds.Prefix;
            }
        }

        private async Task GiveXp(ShardedCommandContext context, SocketUserMessage msg, UserConfig userDb)
        {
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
                    // ignored
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
