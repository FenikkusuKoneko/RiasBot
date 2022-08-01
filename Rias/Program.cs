using System.Reflection;
using Disqord.Bot;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rias;
using Rias.Common;
using Rias.Database;
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
        configBuilder.AddJsonFile("appsettings.json", true, true);

        if (File.Exists($"appsettings.{env.EnvironmentName}.json"))
            configBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);
        
        var config = configBuilder.Build();
        var confirmationColor = config["ConfirmationColor"];
        if (!string.IsNullOrEmpty(confirmationColor))
            Utils.ConfirmationColor = Helpers.HexToInt(confirmationColor) ?? Utils.ConfirmationColor;
        
        var errorColor = config["ErrorColor"];
        if (!string.IsNullOrEmpty(errorColor))
            Utils.ErrorColor = Helpers.HexToInt(errorColor) ?? Utils.ErrorColor;
        
        var intermediateColor = config["IntermediateColor"];
        if (!string.IsNullOrEmpty(intermediateColor))
            Utils.IntermediateColor = Helpers.HexToInt(intermediateColor) ?? Utils.IntermediateColor;
    })
    .ConfigureLogging((context, builder) =>
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}", theme: SystemConsoleTheme.Literate);

        if (context.HostingEnvironment.IsDevelopment())
            loggerConfig.MinimumLevel.Debug();

        var logger = loggerConfig.CreateLogger();
        builder.AddSerilog(logger, true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<UtilityService>();
        
        var dbConnectionString = context.Configuration.GetConnectionString("Database") ?? throw new NullReferenceException("Missing database configuration.");
        services.AddDbContext<RiasDbContext>(options =>
            options.UseNpgsql(dbConnectionString, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()).UseSnakeCaseNamingConvention());

        services.Configure<RiasOptions>(context.Configuration);
        services.AddPrefixProvider<RiasPrefixProvider>();
    })
    .ConfigureDiscordBot<RiasBot>((context, bot) =>
    {
        bot.Token = context.Configuration["Token"];
        bot.Intents = GatewayIntents.All & ~GatewayIntents.Presences;
        bot.Activities = new[] { LocalActivity.Playing("rias help | rias.gg") };
    })
    .UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    })
    .UseConsoleLifetime();

var host = builder.Build();

using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
await db.Database.MigrateAsync();

await host.RunAsync();