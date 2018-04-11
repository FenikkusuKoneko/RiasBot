using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Modules.Music.MusicServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RiasBot.Services
{
    public class LoggingService : IRService
    {
        private readonly CommandHandler _ch;
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        private string _logDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
        private string _logFile => Path.Combine(_logDirectory, $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}.txt");

        private bool ready;
        public LoggingService(CommandHandler ch, DiscordSocketClient discord, CommandService commands)
        {
            _ch = ch;
            _discord = discord;
            _commands = commands;

            _discord.Log += DiscordLogAsync;
            _commands.Log += DiscordLogAsync;
            _commands.CommandExecuted += CommandLogAsync;
            _discord.Ready += Ready;
        }

        private Task DiscordLogAsync(LogMessage msg)
        {
            if (!Directory.Exists(_logDirectory))     // Create the log directory if it doesn't exist
                Directory.CreateDirectory(_logDirectory);
            if (!File.Exists(_logFile))               // Create today's log file if it doesn't exist
                File.Create(_logFile).Dispose();

            if (ready)
            {
                if (msg.Severity != LogSeverity.Verbose && msg.Severity != LogSeverity.Warning)
                {
                    string log = $"{DateTime.UtcNow.ToString("hh:mm:ss")} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
                    
                    return Console.Out.WriteLineAsync(log);
                }
            }
            else
            {
                string log = $"{DateTime.UtcNow.ToString("hh:mm:ss")} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
                
                return Console.Out.WriteLineAsync(log);
            }
            return null;
        }

        private Task CommandLogAsync(CommandInfo commandInfo, ICommandContext context, IResult result)
        {
            var log = new List<string>()
            {
                $"{DateTime.UtcNow.ToString("hh:mm:ss")} [Command] \"{commandInfo.Name}\"",
                $"\t[User] \"{context.User}\" ({context.User.Id})",
                $"\t[Channel] \"{context.Channel.Name}\" ({context.Channel.Id})",
                $"\t[Guild] \"{context.Guild?.Name ?? "DM"}\" ({context.Guild?.Id ?? 0})"
            };
            
            return Console.Out.WriteLineAsync(String.Join("\n", log));
        }

        private Task Ready()
        {
            ready = true;
            return null;
        }
    }
}
