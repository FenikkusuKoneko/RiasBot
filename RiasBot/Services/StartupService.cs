using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Modules.Music;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;
using RiasBot.Modules.NSFW;

namespace RiasBot.Services
{
    public class StartupService : IRService
    {
        private readonly DiscordShardedClient _discord;
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly IBotCredentials _creds;

        public StartupService(
            DiscordShardedClient discord, CommandService commands, IServiceProvider provider, IBotCredentials creds)
        {
            _creds = creds;
            _discord = discord;
            _provider = provider;
            _commands = commands;
        }

        public async Task StartAsync()
        {
            if (_creds.ClientId <= 0 || String.IsNullOrEmpty(_creds.Token) || String.IsNullOrEmpty(_creds.Prefix))
            {
                Console.WriteLine("You must set the the client ID, the token and the prefix in credentials.json");
                Console.ReadKey();
                return;
            }

            string discordToken = _creds.Token;
            await _discord.LoginAsync(TokenType.Bot, discordToken).ConfigureAwait(false);
            await _discord.StartAsync().ConfigureAwait(false);
            
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider).ConfigureAwait(false);
            //await _commands.RemoveModuleAsync<Music>().ConfigureAwait(false);
            RiasBot.upTime.Start();
        }
    }
}