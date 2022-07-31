using Disqord.Bot;
using Disqord.Bot.Commands.Text;
using Disqord.Gateway;
using Microsoft.Extensions.Options;
using Rias.Common;

namespace Rias.Services;

public class RiasPrefixProvider : IPrefixProvider
{
    private readonly RiasOptions _riasOptions;
    
    public RiasPrefixProvider(IOptions<RiasOptions> riasConfiguration)
    {
        _riasOptions = riasConfiguration.Value;
    }
    
    public ValueTask<IEnumerable<IPrefix>?> GetPrefixesAsync(IGatewayUserMessage message)
    {
        var prefixes = new HashSet<IPrefix>();

        if (_riasOptions.Prefixes is not null)
        {
            foreach (var prefix in _riasOptions.Prefixes)
                prefixes.Add(new StringPrefix(prefix));
        }
        
        if (message.Client.CurrentUser is not null)
            prefixes.Add(new StringPrefix(message.Client.CurrentUser.Name));
        
        return new ValueTask<IEnumerable<IPrefix>?>(prefixes);
    }
}