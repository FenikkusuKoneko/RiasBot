using System.Reflection;
using Disqord.Bot;
using Disqord.Bot.Commands.Text;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
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
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Assembly.Load("Rias.ApplicationCommands");
Assembly.Load("Rias.TextCommands");

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

        var logger = loggerConfig.CreateLogger();
        loggingBuilder.AddSerilog(logger, true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<LocalisationService>();
        
        services.AddPrefixProvider<RiasPrefixProvider>();
        services.Configure<RiasOptions>(context.Configuration);
        
        services.AddHttpClient<AdministrationService>();

        var dbConnectionString = context.Configuration.GetConnectionString("Database") ?? throw new Exception("Database connection string is not set.");
        var dbDataSource = new NpgsqlDataSourceBuilder(dbConnectionString);
        dbDataSource.MapEnum<LastChargeStatus>();
        dbDataSource.MapEnum<PatronStatus>();
        
        services.AddDbContext<RiasDbContext>(options =>
            options.UseNpgsql(dbDataSource.Build(), npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention()
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        var commandServices = typeof(RiasCommandService).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(RiasCommandService)) && t is { IsAbstract: false, IsInterface: false });
        
        foreach (var commandService in commandServices)
            services.AddScoped(commandService);
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

var host = builder.Build();

await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
await db.Database.MigrateAsync();

var localization = host.Services.GetRequiredService<LocalisationService>();
await localization.LoadAsync();

var prefixProvider = (RiasPrefixProvider) host.Services.GetRequiredService<IPrefixProvider>();
await prefixProvider.LoadGuildPrefixesAsync();

await host.RunAsync();