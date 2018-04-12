using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RiasBot.Services;
using RiasBot.Services.Implementation;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace RiasBot
{
    public class RiasBot
    {
        public static void Main(string[] args)
            =>new RiasBot().StartAsync().GetAwaiter().GetResult();

        public static string version = "1.3.37";
        public static uint goodColor = 0x009688;
        public static uint badColor = 0xff0000;
        public static string currency = "<:heart_diamond:416513090549448724>";
        public static string invite = "https://discordapp.com/oauth2/authorize?client_id=381387277764395008&scope=bot&permissions=1610083455";
        public static ulong fenikkusuId = 327927038360944640;
        public static string creatorServer = "https://discord.gg/VPfBvBt";
        public static ulong supportServer = 416492045859946507;
        public static Stopwatch upTime = new Stopwatch();
        public static int commandsRun = 0;

        public static bool isBeta = false;

        public BotCredentials Credentials { get; private set; }

        public async Task StartAsync()
        {
            SetEnvironmentCurrentDirectory(); //Set Environment#CurrentDirectory with the project's path. Call it for the first time.
            Credentials = new BotCredentials();

            var services = new ServiceCollection()      // Begin building the service provider
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig     // Add the discord client to the service provider
                {
                    LogLevel = LogSeverity.Info,
                    MessageCacheSize = 1000,     // Tell Discord.Net to cache 1000 messages per channel
                    AlwaysDownloadUsers = true,
                }))
                .AddSingleton(new CommandService(new CommandServiceConfig     // Add the command service to the service provider
                {
                    DefaultRunMode = RunMode.Async,     // Force all commands to run async
                    LogLevel = LogSeverity.Verbose
                }))
                .AddSingleton<IBotCredentials>(Credentials);

            var assembly = Assembly.GetAssembly(typeof(RiasBot));

            var IKServices = assembly.GetTypes()
                    .Where(x => x.GetInterfaces().Contains(typeof(IRService))
                        && !x.GetTypeInfo().IsInterface && !x.GetTypeInfo().IsAbstract).ToArray();

            for (int i = 0; i < IKServices.Length; i++)
            {
                Type type = IKServices[i];
                services.AddSingleton(type);
            }

            var provider = services.BuildServiceProvider();     // Create the service provider

            provider.GetRequiredService<LoggingService>();      // Initialize the logging service, startup service, and command handler
            await provider.GetRequiredService<StartupService>().StartAsync().ConfigureAwait(false);
            provider.GetRequiredService<CommandHandler>();
            provider.GetRequiredService<BotService>();
            provider.GetRequiredService<DbService>();
            provider.GetRequiredService<PatreonService>();

            await Task.Delay(-1).ConfigureAwait(false);     // Prevent the application from closing
        }

        public void SetEnvironmentCurrentDirectory()
        {
            //If your Visual Studio has some issues with the Environment#CurrentDirectory, call this function
            //Usually the path should be the project's path. But there is the "bin" folder
            //This is happening just in Visual Studio, works if you build using a command line and dotnet
            string path = Environment.CurrentDirectory;
            if (path.Contains("bin"))
                Environment.CurrentDirectory = Directory.GetParent(path).Parent.Parent.FullName;
        }
    }
}
