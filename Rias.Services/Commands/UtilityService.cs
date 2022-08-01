using Disqord;
using Disqord.Bot.Commands.Text;
using Rias.Database;

namespace Rias.Services.Commands;

public class UtilityService : RiasCommandService
{
    private readonly RiasPrefixProvider _prefixProvider;
    
    public UtilityService(RiasDbContext db, IPrefixProvider prefixProvider) 
        : base(db)
    {
        _prefixProvider = (RiasPrefixProvider) prefixProvider;
    }
    
    public async Task<string?> GetGuildPrefixAsync(Snowflake guildId)
    {
        var prefix = await _prefixProvider.GetGuildPrefixAsync(guildId);
        return prefix;
    }
}