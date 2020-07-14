using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Disqord;
using Disqord.Extensions.Interactivity;
using Disqord.Sharding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Serilog;
using StackExchange.Redis;

namespace Rias.Core
{
    public class Rias : DiscordSharder, IServiceProvider
    {
        public const string Author = "Koneko#0001";
        public const string Version = "3.0.12";
        public static readonly Stopwatch UpTime = new Stopwatch();

        private readonly IServiceProvider _serviceProvider;
        private static Credentials? _credentials;
        
        public Rias(Credentials credentials, string databaseConnection) : base(TokenType.Bot, credentials.Token)
        {
            _credentials = credentials;
            var commandService = new CommandService(new CommandServiceConfiguration
            {
                DefaultRunMode = RunMode.Parallel,
                StringComparison = StringComparison.InvariantCultureIgnoreCase,
                CooldownBucketKeyGenerator = CooldownBucketKeyGenerator
            });

            var interactivity = new InteractivityExtension();
            AddExtensionAsync(interactivity).GetAwaiter().GetResult();

            var redis = ConnectionMultiplexer.Connect("localhost");
            Log.Information("Redis connected");
            
            _serviceProvider = InitializeServices()
                .AddSingleton(this)
                .AddSingleton(credentials)
                .AddSingleton(commandService)
                .AddSingleton(interactivity)
                .AddSingleton(redis)
                .AddSingleton<Localization>()
                .AddSingleton<HttpClient>()
                .AddDbContext<RiasDbContext>(x => x.UseNpgsql(databaseConnection).UseSnakeCaseNamingConvention())
                .BuildServiceProvider();
            
            ApplyDatabaseMigrations();

            _serviceProvider.GetRequiredService<Localization>();
            var autoStartServices = typeof(Rias).Assembly.GetTypes()
                .Where(x => typeof(RiasService).IsAssignableFrom(x)
                            && x.GetCustomAttribute<AutoStartAttribute>() != null
                            && !x.GetTypeInfo().IsInterface
                            && !x.GetTypeInfo().IsAbstract)
                .OrderByDescending(x => x.GetCustomAttribute<AutoStartAttribute>()!.Priority);
            
            foreach (var serviceType in autoStartServices)
                _serviceProvider.GetRequiredService(serviceType);
            
            UpTime.Start();
        }

        private static object? CooldownBucketKeyGenerator(object bucketType, CommandContext context)
        {
            var riasContext = (RiasCommandContext) context;
            
            // owner doesn't have cooldown
            if (_credentials != null && _credentials.MasterId != 0 && riasContext.User.Id == _credentials.MasterId)
                return null;
            
            return (BucketType) bucketType switch
            {
                BucketType.Guild => riasContext.Guild!.Id.ToString(),
                BucketType.User => riasContext.User.Id.ToString(),
                BucketType.Member => $"{riasContext.Guild!.Id}_{riasContext.User.Id}",
                _ => null
            };
        }
        
        private static IServiceCollection InitializeServices()
        {
            var riasServices = typeof(Rias).Assembly.GetTypes()
                .Where(x => typeof(RiasService).IsAssignableFrom(x)
                            && !x.GetTypeInfo().IsInterface
                            && !x.GetTypeInfo().IsAbstract);
            
            var services = new ServiceCollection();
            foreach (var serviceType in riasServices)
            {
                services.AddSingleton(serviceType);
            }

            return services;
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

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(Rias) || serviceType == GetType())
                return this;

            return _serviceProvider?.GetService(serviceType);
        }
    }
}