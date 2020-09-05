using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Configuration;
using Rias.Database;
using Rias.Implementation;
using Rias.Services;
using Serilog;
using Serilog.Extensions.Logging;
using StackExchange.Redis;

namespace Rias
{
    public class RiasBot : IServiceProvider
    {
        public const string Author = "Koneko#0001";
        public const string Version = "3.3.0";
        public static readonly Stopwatch UpTime = new Stopwatch();

        public readonly DiscordShardedClient Client;
        public DiscordUser? CurrentUser => Client.CurrentUser;
        public IReadOnlyDictionary<ulong, DiscordGuild> Guilds => Client.ShardClients
            .SelectMany(x => x.Value.Guilds)
            .Select(x => x.Value)
            .ToImmutableDictionary(x => x.Id);

        public ConcurrentDictionary<ulong, DiscordUser> Members = new ConcurrentDictionary<ulong, DiscordUser>();

        public IReadOnlyDictionary<ulong, DiscordChannel> Channels => Client.ShardClients
            .SelectMany(x => x.Value.Guilds)
            .Select(x => x.Value)
            .SelectMany(x => x.Channels)
            .Select(x => x.Value)
            .ToImmutableDictionary(x => x.Id);

        public double Latency => Client.ShardClients.Average(x => x.Value.Ping);

        private readonly Credentials _credentials;
        private readonly IServiceProvider _serviceProvider;
        
        public RiasBot()
        {
            _credentials = new Credentials();
            Log.Information(!_credentials.IsDevelopment ? $"Initializing RiasBot version {Version}" : $"Initializing development RiasBot version {Version}");
            VerifyCredentials();
            
            Client = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = _credentials.Token,
                MessageCacheSize = 4096,
                Intents = DiscordIntents.All,
                LoggerFactory = new SerilogLoggerFactory(Log.Logger)
            });
            
            var commandService = new CommandService(new CommandServiceConfiguration
            {
                DefaultRunMode = RunMode.Parallel,
                StringComparison = StringComparison.InvariantCultureIgnoreCase,
                CooldownBucketKeyGenerator = CooldownBucketKeyGenerator
            });
            
            var databaseConnection = GetDatabaseConnection();
            if (databaseConnection is null)
                throw new NullReferenceException("The database connection is not set in credentials.json");

            var redis = ConnectionMultiplexer.Connect("localhost");
            Log.Information("Redis connected");
            
            var riasServices = typeof(RiasBot).Assembly.GetTypes()
                .Where(x => typeof(RiasService).IsAssignableFrom(x)
                            && !x.GetTypeInfo().IsInterface
                            && !x.GetTypeInfo().IsAbstract);
            
            var services = new ServiceCollection();
            foreach (var serviceType in riasServices)
                services.AddSingleton(serviceType);

            _serviceProvider = services
                .AddSingleton(this)
                .AddSingleton(_credentials)
                .AddSingleton(commandService)
                .AddSingleton(redis)
                .AddSingleton<Localization>()
                .AddSingleton<HttpClient>()
                .AddDbContext<RiasDbContext>(x => x.UseNpgsql(databaseConnection).UseSnakeCaseNamingConvention())
                .BuildServiceProvider();
            
            ApplyDatabaseMigrations();

            _serviceProvider.GetRequiredService<Localization>();
            var autoStartServices = typeof(RiasBot).Assembly.GetTypes()
                .Where(x => typeof(RiasService).IsAssignableFrom(x)
                            && x.GetCustomAttribute<AutoStartAttribute>() != null
                            && !x.GetTypeInfo().IsInterface
                            && !x.GetTypeInfo().IsAbstract);
            
            foreach (var serviceType in autoStartServices)
                _serviceProvider.GetRequiredService(serviceType);
            
            UpTime.Start();
        }

        public Task StartAsync()
            => Client.StartAsync();

        public int GetShardId(DiscordGuild? guild)
            => guild != null ? Client.ShardClients.First(x => x.Value.Guilds.ContainsKey(guild.Id)).Value.ShardId : 0;

        public DiscordGuild? GetGuild(ulong id)
            => Guilds.TryGetValue(id, out var guild) ? guild : null;

        public DiscordUser? GetMember(ulong id)
            => Members.TryGetValue(id, out var user) ? user : null;

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(RiasBot) || serviceType == GetType())
                return this;

            return _serviceProvider.GetService(serviceType);
        }

        private void VerifyCredentials()
        {
            if (string.IsNullOrEmpty(_credentials.Token))
                throw new NullReferenceException("You must set the token in credentials.json!");

            if (string.IsNullOrEmpty(_credentials.Prefix))
                throw new NullReferenceException("You must set the default prefix in credentials.json!");
        }
        
        private string? GetDatabaseConnection()
        {
            if (_credentials.DatabaseConfig is null)
            {
                return null;
            }

            var connectionString = new StringBuilder();
            connectionString.Append("Host=").Append(_credentials.DatabaseConfig.Host).Append(";");

            if (_credentials.DatabaseConfig.Port > 0)
                connectionString.Append("Port=").Append(_credentials.DatabaseConfig.Port).Append(";");

            connectionString.Append("Username=").Append(_credentials.DatabaseConfig.Username).Append(";")
                .Append("Password=").Append(_credentials.DatabaseConfig.Password).Append(";")
                .Append("Database=").Append(_credentials.DatabaseConfig.Database).Append(";")
                .Append("ApplicationName=").Append(_credentials.DatabaseConfig.ApplicationName);

            return connectionString.ToString();
        }
        
        private object? CooldownBucketKeyGenerator(object bucketType, CommandContext context)
        {
            var riasContext = (RiasCommandContext) context;
            
            // owner doesn't have cooldown
            if (_credentials.MasterId != 0 && riasContext.User.Id == _credentials.MasterId)
                return null;
            
            return (BucketType) bucketType switch
            {
                BucketType.Guild => riasContext.Guild!.Id.ToString(),
                BucketType.User => riasContext.User.Id.ToString(),
                BucketType.Member => $"{riasContext.Guild!.Id}_{riasContext.User.Id}",
                BucketType.Channel => riasContext.Channel.Id.ToString(),
                _ => null
            };
        }

        private void ApplyDatabaseMigrations()
        {
            using var scope = this.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            if (!dbContext.Database.GetPendingMigrations().Any())
            {
                return;
            }

            dbContext.Database.Migrate();
            dbContext.SaveChanges();
        }
    }
}