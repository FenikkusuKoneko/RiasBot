using Disqord.Bot.Commands.Application;
using Qmmands;
using Rias.Services.Commands;

namespace Rias.ApplicationCommands.Modules.Utility;

public class UtilityModule : RiasApplicationGuildModule<UtilityService>
{
    [SlashCommand("prefix")]
    public async Task<IResult> PrefixAsync()
    {
        var prefix = await Service.GetGuildPrefixAsync(Context.GuildId);
        
        return ConfirmationResponse(!string.IsNullOrEmpty(prefix)
            ? $"The prefix on this server is `{prefix}`."
            : "No prefix set on this server.");
    }
}