using System.Reflection;
using Disqord;
using Disqord.Bot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Rias;

public class RiasBot : DiscordBot
{
    protected override IEnumerable<Assembly> GetModuleAssemblies() => AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => a.FullName?.StartsWith("Rias") == true);

    public RiasBot(IOptions<DiscordBotConfiguration> options, ILogger<DiscordBot> logger, IServiceProvider services, DiscordClient client) : base(options, logger, services, client)
    {
    }
}