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
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        private string _logDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
        private string _logFile => Path.Combine(_logDirectory, $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}.txt");
        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            _discord = discord;
            _commands = commands;
            
            _discord.Log += DiscordLogAsync;
            _commands.Log += DiscordLogAsync;
        }

        private Task DiscordLogAsync(LogMessage msg)
        {
            if (!Directory.Exists(_logDirectory))     // Create the log directory if it doesn't exist
                Directory.CreateDirectory(_logDirectory);
            if (!File.Exists(_logFile))               // Create today's log file if it doesn't exist
                File.Create(_logFile).Dispose();

            var logText = new List<string>()
            {
                $"{DateTime.UtcNow.ToString("hh:mm:ss")} [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}"
            };
            try
            {
                File.AppendAllLinesAsync(_logFile, logText);     // Write the log text to a file
            }
            catch
            {

            }
            return Console.Out.WriteLineAsync(logText[0]);
        }
    }
}