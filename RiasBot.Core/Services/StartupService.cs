using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Rest;
using RiasBot.Commons.TypeReaders;
using RiasBot.Modules.Music;

namespace RiasBot.Services
{
    public class StartupService : IRService
    {
        private readonly DiscordShardedClient _discord;
        private readonly DiscordRestClient _restDiscord;
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly IBotCredentials _creds;

        public StartupService(DiscordShardedClient discord, DiscordRestClient restDiscord, CommandService commands,
            IServiceProvider provider, IBotCredentials creds)
        {
            _creds = creds;
            _discord = discord;
            _restDiscord = restDiscord;
            _provider = provider;
            _commands = commands;
        }

        public async Task StartAsync()
        {
            if (string.IsNullOrEmpty(_creds.Token) || string.IsNullOrEmpty(_creds.Prefix))
            {
                Console.WriteLine("You must set the token and the prefix in credentials.json");
                Console.ReadKey();
                return;
            }

            var discordToken = _creds.Token;
            await _discord.LoginAsync(TokenType.Bot, discordToken).ConfigureAwait(false);
            await _restDiscord.LoginAsync(TokenType.Bot, discordToken).ConfigureAwait(false);
            await _discord.StartAsync().ConfigureAwait(false);
            
            var assembly = Assembly.GetAssembly(typeof(RiasBot));
            var typeReaders = assembly.GetTypes()
                .Where(x => x.IsSubclassOf(typeof(TypeReader))
                            && x.BaseType.GetGenericArguments().Length > 0
                            && !x.IsAbstract);

            foreach (var type in typeReaders)
            {
                var typeReader = (TypeReader) Activator.CreateInstance(type, _discord, _commands);
                var baseType = type.BaseType;
                var typeArgs = baseType.GetGenericArguments();
                
                _commands.AddTypeReader(typeArgs[0], typeReader);
            }
            await _commands.AddModulesAsync(Assembly.GetAssembly(typeof(RiasBot)), _provider).ConfigureAwait(false);
            
            RiasBot.UpTime.Start();
        }
    }
}