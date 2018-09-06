using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RiasBot.Services
{
    public class LoggingService : IRService
    {
        private readonly DiscordShardedClient _discord;
        private readonly CommandService _commands;

        public bool Ready;
        public string CommandArguments;
        public LoggingService(DiscordShardedClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;

            _discord.Log += DiscordLogAsync;
            _commands.Log += DiscordLogAsync;
            _commands.CommandExecuted += CommandLogAsync;
        }

        private Task DiscordLogAsync(LogMessage msg)
        {
            if (Ready)
            {
                if (msg.Severity != LogSeverity.Verbose && msg.Severity != LogSeverity.Warning)
                {
                    var log = $"{DateTime.UtcNow:MMM dd hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
                    
                    return Console.Out.WriteLineAsync(log);
                }
            }
            else
            {
                var log = $"{DateTime.UtcNow:MMM dd hh:mm:ss} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
                
                return Console.Out.WriteLineAsync(log);
            }
            return null;
        }

        private Task CommandLogAsync(CommandInfo commandInfo, ICommandContext context, IResult result)
        {
            var log = new List<string>()
            {
                $"{DateTime.UtcNow:MMM dd hh:mm:ss} [Command] \"{commandInfo.Name}\"",
                $"\t[Arguments] \"{CommandArguments}\"",
                $"\t[User] \"{context.User}\" ({context.User.Id})",
                $"\t[Channel] \"{context.Channel.Name}\" ({context.Channel.Id})",
                $"\t[Guild] \"{context.Guild?.Name ?? "DM"}\" ({context.Guild?.Id ?? 0})"
            };
            
            return Console.Out.WriteLineAsync(String.Join("\n", log));
        }
    }
}
