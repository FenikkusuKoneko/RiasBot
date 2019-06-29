using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Database;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Serilog;

namespace Rias.Core
{
    public class Rias
    {
        public const string Author = "Koneko#0001";
        public const string Version = "2.0.0-alpha";

        private DiscordShardedClient _client;
        private CommandService _commandService;
        private Credentials _creds;

        public async Task InitializeAsync()
        {
            _creds = new Credentials();
            
            _client = new DiscordShardedClient(new DiscordSocketConfig
            {
                LogLevel = _creds.IsBeta ? LogSeverity.Verbose : LogSeverity.Info
            });
            _commandService = new CommandService(new CommandServiceConfiguration
            {
                DefaultRunMode = RunMode.Parallel
            });

            await StartAsync();

            var provider = InitializeServices();
            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<CommandHandlerService>();

            await Task.Delay(-1);
        }

        private IServiceProvider InitializeServices()
        {
            var services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commandService)
                .AddSingleton(_creds)
                .AddSingleton<Translations>();

            var assembly = Assembly.GetAssembly(typeof(Rias));

            var attributeServices = assembly.GetTypes()
                .Where(x => typeof(RiasService).IsAssignableFrom(x)
                            && !x.GetTypeInfo().IsInterface
                            && !x.GetTypeInfo().IsAbstract)
                .ToArray();

            foreach (var serviceType in attributeServices)
            {
                services.AddSingleton(serviceType);
            }
            
            return services.BuildServiceProvider();
        }

        private async Task StartAsync()
        {
            if (!VerifyCredentials()) return;
            
            await _client.LoginAsync(TokenType.Bot, _creds.Token);
            await _client.StartAsync();
        }
        
        private bool VerifyCredentials()
        {
            if (string.IsNullOrEmpty(_creds.Token))
            {
                Log.Error("You must set the token in credentials.json!");
                return false;
            }

            if (string.IsNullOrEmpty(_creds.Prefix))
            {
                Log.Error("You must set the default prefix in credentials.json!");
                return false;
            }

            return true;
        }
    }
}