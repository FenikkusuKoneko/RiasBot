using System.Reflection;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Rias;
using Rias.Common;
using Rias.Database;
using Rias.Database.Enums;
using Rias.Services;
using Rias.Services.Commands;
using Rias.Services.Hosted;
using Rias.Services.Providers;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

Assembly.Load("Rias.ApplicationCommands");
Assembly.Load("Rias.TextCommands");

Logger? logger;

var builder = new HostBuilder()
    .ConfigureHostConfiguration(config =>
    {
        config.AddEnvironmentVariables("RIAS_");
    })
    .ConfigureAppConfiguration((context, configBuilder) =>
    {
        var env = context.HostingEnvironment;
        configBuilder.AddJsonFile("appsettings.json", false, true);

        if (File.Exists($"appsettings.{env.EnvironmentName}.json"))
            configBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);
        
        var config = configBuilder.Build();
        var successColor = config["SuccessColor"];
        if (!string.IsNullOrEmpty(successColor))
            Utils.SuccessColor = Helpers.HexToInt(successColor) ?? default;
        
        var errorColor = config["ErrorColor"];
        if (!string.IsNullOrEmpty(errorColor))
            Utils.ErrorColor = Helpers.HexToInt(errorColor) ?? default;
        
        var intermediateColor = config["IntermediateColor"];
        if (!string.IsNullOrEmpty(intermediateColor))
            Utils.IntermediateColor = Helpers.HexToInt(intermediateColor) ?? default;
    })
    .ConfigureLogging((context, loggingBuilder) =>
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}", theme: SystemConsoleTheme.Literate);
        
        if (context.HostingEnvironment.IsDevelopment())
            loggerConfig.MinimumLevel.Debug();

        logger = loggerConfig.CreateLogger();
        Log.Logger = logger;
        loggingBuilder.AddSerilog(logger, true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<LocalisationService>()
            .AddHostedService<LocalisationBackgroundService>()
            .AddHostedService<PrefixesBackgroundService>()
            .AddPrefixProvider<RiasPrefixProvider>()
            .Configure<RiasConfiguration>(context.Configuration);

        var dbConnectionString = context.Configuration.GetConnectionString("Database") ?? throw new Exception("Database connection string is not set.");
        var dbDataSource = new NpgsqlDataSourceBuilder(dbConnectionString);
        dbDataSource.MapEnum<LastChargeStatus>();
        dbDataSource.MapEnum<PatronStatus>();
        
        services.AddDbContext<RiasDbContext>(options =>
            options.UseNpgsql(dbDataSource.Build(), npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()).UseSnakeCaseNamingConvention());

        var commandServices = typeof(RiasCommandService).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(RiasCommandService)) && t is { IsAbstract: false, IsInterface: false });
        
        foreach (var commandService in commandServices)
            services.AddScoped(commandService);
        
        services.AddHttpClient<AdministrationService>(httpClient => httpClient.Timeout = 15.Seconds());
        services.AddHttpClient<EmojisService>(httpClient =>
        {
            httpClient.MaxResponseContentBufferSize = Limits.Guild.Emoji.SizeLimit;
            httpClient.Timeout = 15.Seconds();
        });
    })
    .ConfigureDiscordBot<RiasBot>((context, bot) =>
    {
        bot.Token = context.Configuration["Token"];
        bot.Intents = GatewayIntents.All & ~GatewayIntents.Presences;
        bot.ServiceAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        var activity = context.Configuration["Activity"];
        if (!string.IsNullOrEmpty(activity))
            bot.Activities = new[] { LocalActivity.Playing(activity) };
    })
    .UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    })
    .UseConsoleLifetime();


TaskScheduler.UnobservedTaskException += (sender, args) =>
{
    args.SetObserved();
    Log.ForContext("SourceContext", sender).Error(args.Exception, "Unobserved task exception");
};

AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    Log.ForContext("SourceContext", sender).Fatal(args.ExceptionObject as Exception, "Unhandled exception. The process is {ProcessState}", args.IsTerminating ? "terminating" : "continuing");
    
    if (args.IsTerminating)
        Log.CloseAndFlush();
};

var host = builder.Build();

await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
await db.Database.MigrateAsync();

await host.RunAsync();