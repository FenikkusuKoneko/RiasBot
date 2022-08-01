using Qmmands;
using Qmmands.Text;
using Rias.Services.Commands;

namespace Rias.TextCommands.Modules;

public class UtilityModule : RiasTextGuildModule<UtilityService>
{
    [TextCommand("prefix")]
    public async Task<IResult> PrefixAsync()
    {
        var prefix = await Service.GetGuildPrefixAsync(Context.GuildId);
        
        return ConfirmationResponse(!string.IsNullOrEmpty(prefix)
            ? $"The prefix on this server is `{prefix}`."
            : "No prefix set on this server.");
    }
}