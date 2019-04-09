using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using RiasBot.Services;
using RiasBot.Services.Implementation;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Victoria;

namespace RiasBot
{
    public class RiasBot
    {
        public const string Version = "1.21.6";
        public const uint GoodColor = 0x009688;
        public const uint BadColor = 0xff0000;
        public const string Currency = "<a:diamond_heart:546102072861392902>";
        public const string Invite = "https://discordapp.com/oauth2/authorize?client_id=381387277764395008&scope=bot&permissions=1609952503";
        public const string Author = "Koneko#0001";
        public const ulong KonekoId = 327927038360944640;
        public const ulong BotId = 381387277764395008;
        public const string CreatorServer = "https://discord.gg/VPfBvBt";
        public const ulong SupportServer = 416492045859946507;
        public const string Patreon = "https://www.patreon.com/riasbot";
        public const string Website = "https://riasbot.me/";
        public const string WeebApi = "https://api-v2.weeb.sh/";
        public static readonly Stopwatch UpTime = new Stopwatch();
        public static int CommandsRun = 0;

        private BotCredentials Credentials { get; set; }

        public async Task StartAsync()
        {
            Credentials = new BotCredentials();

            var services = new ServiceCollection()      // Begin building the service provider
                .AddSingleton(new DiscordShardedClient(new DiscordSocketConfig     // Add the discord client to the service provider
                {
                    LogLevel = LogSeverity.Info,
                    MessageCacheSize = 0,
                    AlwaysDownloadUsers = false
                }))
                .AddSingleton(new DiscordRestClient())
                .AddSingleton(new CommandService(new CommandServiceConfig     // Add the command service to the service provider
                {
                    DefaultRunMode = RunMode.Async,     // Force all commands to run async
                    LogLevel = LogSeverity.Verbose
                }))
                .AddSingleton<IBotCredentials>(Credentials)
                .AddSingleton<Lavalink>();
            var assembly = Assembly.GetAssembly(typeof(RiasBot));

            var iRServices = assembly.GetTypes()
                    .Where(x => x.GetInterfaces().Contains(typeof(IRService))
                        && !x.GetTypeInfo().IsInterface && !x.GetTypeInfo().IsAbstract);

            foreach (var type in iRServices)
            {
                services.AddSingleton(type);
            }

            var provider = services.BuildServiceProvider();     // Create the service provider

            provider.GetRequiredService<LoggingService>();      // Initialize the logging service, startup service, and command handler
            await provider.GetRequiredService<StartupService>().StartAsync().ConfigureAwait(false);
            provider.GetRequiredService<CommandHandler>();
            provider.GetRequiredService<BotService>();
            provider.GetRequiredService<DbService>();
            provider.GetRequiredService<PatreonService>();
            await provider.GetRequiredService<VotesService>().ConfigureVotesWebSocket();

            await Task.Delay(-1).ConfigureAwait(false);     // Prevent the application from closing
        }

        //public void SetEnvironmentCurrentDirectory()
        //{
        //    //If your Visual Studio has some issues with the Environment#CurrentDirectory, call this function
        //    //Usually the path should be the project's path. But there is the "bin" folder
        //    //This is happening just in Visual Studio, works if you build using a command line and dotnet
        //    var path = Environment.CurrentDirectory;
        //    if (path.Contains("bin"))
        //        Environment.CurrentDirectory = Directory.GetParent(path).Parent.Parent.FullName;
        //}
    }
}
