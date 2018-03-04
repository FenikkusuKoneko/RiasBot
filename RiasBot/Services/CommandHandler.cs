using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Linq;
using Cleverbot.Net;
using Discord;
using RiasBot.Modules.Xp.Services;

namespace RiasBot.Services
{
    public class CommandHandler : IKService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IBotCredentials _creds;
        private readonly IServiceProvider _provider;
        private readonly DbService _db;
        private readonly XpService _xpService;

        public string _prefix;

        public CommandHandler(DiscordSocketClient discord, CommandService commands, IBotCredentials creds, IServiceProvider provider, DbService db, XpService xpService)
        {
            _discord = discord;
            _commands = commands;
            _creds = creds;
            _provider = provider;
            _db = db;
            _xpService = xpService;

            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;     // Ensure the message is from a user/bot
            if (msg == null) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;     // Ignore self when checking commands
            if (msg.Author.IsBot) return;       // Ignore other bots

            var context = new SocketCommandContext(_discord, msg);     // Create the command context

            try
            {
                var socketGuildUser = context.Guild.GetUser(_discord.CurrentUser.Id);
                var preconditions = socketGuildUser.GetPermissions((IGuildChannel)context.Channel);
                if (!preconditions.SendMessages)
                    return;
            }
            catch
            {

            }

            if (!context.IsPrivate)
            {
                using (var db = _db.GetDbContext())
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
            }
            else
            {
                _prefix = _creds.Prefix;
            }

            int argPos = 0;     // Check if the message has a valid command prefix

            if (msg.HasStringPrefix(_prefix, ref argPos) || msg.HasStringPrefix("rias ", ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider).ConfigureAwait(false);

                if (result.IsSuccess)
                    RiasBot.commandsRun++;
            }

            /*if (msg.HasMentionPrefix(context.Client.CurrentUser, ref argPos))
            {
                await Task.Factory.StartNew(() => Cleverbot(msg, context, context.Channel)).ConfigureAwait(false);
            }*/

            if (!context.IsPrivate)
            {
                await _xpService.XpUserMessage((IGuildUser)msg.Author, (ITextChannel)context.Channel);
                await _xpService.XpUserGuildMessage(context.Guild, (IGuildUser)msg.Author, (ITextChannel)context.Channel);
            }
        }
        
        private async Task Cleverbot (SocketUserMessage msg, SocketCommandContext context, IMessageChannel channel)
        {
            
            string _message = msg.ToString();
        
            await context.Channel.TriggerTypingAsync().ConfigureAwait(false);
        
            var _cleverbot = await CleverbotSession.NewSessionAsync("fxssTYrQznLfGLi2", "WJSrkkq8mGmIyr5CaZncuDTX49gk4jBM");
            var cleverbotResponse = await _cleverbot.SendAsync(_message.Remove(0, context.Client.CurrentUser.Mention.Length));
            await channel.SendMessageAsync(cleverbotResponse).ConfigureAwait(false);
        }
    }
}
