using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Text;
using Disqord.Gateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rias.Common;
using Rias.Database;

namespace Rias.Services;

public class RiasPrefixProvider : IPrefixProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RiasOptions _riasOptions;
    
    public RiasPrefixProvider(IServiceProvider serviceProvider, IOptions<RiasOptions> riasConfiguration)
    {
        _serviceProvider = serviceProvider;
        _riasOptions = riasConfiguration.Value;
    }
    
    public async ValueTask<IEnumerable<IPrefix>?> GetPrefixesAsync(IGatewayUserMessage message)
    {
        var prefixes = new HashSet<IPrefix>();

        var prefix = _riasOptions.Prefix;
        if (message.GuildId.HasValue)
        {
            var guildPrefix = await GetGuildPrefixAsync(message.GuildId.Value);
            if (!string.IsNullOrEmpty(guildPrefix))
                prefix = guildPrefix;
        }

        if (!string.IsNullOrEmpty(prefix))
            prefixes.Add(new StringPrefix(prefix));
        
        if (message.Client.CurrentUser is not null)
            prefixes.Add(new StringPrefix(message.Client.CurrentUser.Name));
        
        return prefixes;
    }

    public async Task<string?> GetGuildPrefixAsync(Snowflake guildId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();

        var guild = await db.Guilds.FirstOrDefaultAsync(g => g.GuildId == guildId.RawValue);
        return guild?.Prefix;
    }
}