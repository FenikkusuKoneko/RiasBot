using System.Reflection;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Rias;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Assembly.Load("Rias.ApplicationCommands");
Assembly.Load("Rias.TextCommands");

await new HostBuilder()
    .ConfigureHostConfiguration(config =>
    {
        config.AddEnvironmentVariables("RIAS_");
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        var env = context.HostingEnvironment;
        var reloadOnChange = context.Configuration.GetValue("hostBuilder:reloadConfigOnChange", true);

        config.AddJsonFile("appsettings.json", true, reloadOnChange);

        if (File.Exists($"appsettings.{env.EnvironmentName}.json"))
            config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, reloadOnChange);
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
    .ConfigureDiscordBot<RiasBot>((context, bot) =>
    {
        bot.Token = context.Configuration["Token"];
        bot.Intents = GatewayIntents.All & ~GatewayIntents.Presences;
        bot.Activities = new[] { LocalActivity.Playing("rias help | rias.gg") };
    })
    .RunConsoleAsync();