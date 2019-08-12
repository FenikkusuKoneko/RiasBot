using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Commons;
using Rias.Core.Database;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Interactive;
using Serilog;

namespace Rias.Core
{
    public class Rias
    {
        public const string Author = "Koneko#0001";
        public const string Version = "2.0.0";

        private DiscordShardedClient _client;
        private CommandService _commandService;
        private Credentials _creds;

        public async Task InitializeAsync()
        {
            _creds = new Credentials();

            _client = new DiscordShardedClient(new DiscordSocketConfig
            {
                LogLevel = _creds.IsBeta ? LogSeverity.Verbose : LogSeverity.Info,
                ExclusiveBulkDelete = true
            });
            _commandService = new CommandService(new CommandServiceConfiguration
            {
                DefaultRunMode = RunMode.Parallel,
                StringComparison = StringComparison.InvariantCultureIgnoreCase,
                CooldownBucketKeyGenerator = CooldownBucketKeyGenerator
            });

            await StartAsync();

            var provider = InitializeServices();
            ApplyDatabaseMigrations(provider.GetRequiredService<RiasDbContext>());
            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<CommandHandlerService>();

            await Task.Delay(-1);
        }

        private object CooldownBucketKeyGenerator(object bucketType, CommandContext context, IServiceProvider provider)
        {
            var riasContext = (RiasCommandContext) context;
            return (BucketType) bucketType switch
            {
                BucketType.Guild => (object) riasContext.Guild.Id,
                BucketType.User => riasContext.User.Id,
                BucketType.GuildUser => riasContext.Guild.Id + "_" + riasContext.User.Id,
                _ => riasContext.User.Id
            };
        }

        private IServiceProvider InitializeServices()
        {
            var services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commandService)
                .AddSingleton(_creds)
                .AddSingleton<Translations>()
                .AddSingleton(new InteractiveService(_client))
                .AddTransient<RiasDbContext>();

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

        private static void ApplyDatabaseMigrations(DbContext dbContext)
        {
            if (!dbContext.Database.GetPendingMigrations().Any()) return;
            dbContext.Database.Migrate();
            dbContext.SaveChanges();
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

            if (!string.IsNullOrEmpty(_creds.Prefix)) return true;

            Log.Error("You must set the default prefix in credentials.json!");
            return false;
        }
    }
}