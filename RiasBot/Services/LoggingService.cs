using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Modules.Music.MusicServices;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RiasBot.Services
{
    public class LoggingService : IRService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            
            _discord.Log += DiscordLogAsync;
            _commands.Log += DiscordLogAsync;
        }
        
        private Task DiscordLogAsync(LogMessage msg)
        {
            string logText = $"{DateTime.UtcNow.ToString("hh:mm:ss")} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
            return Console.Out.WriteLineAsync(logText);
        }
    }
}