using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Text;
using Qmmands;
using Rias.Common;
using Rias.Services;
using Rias.Services.Commands;

namespace Rias.ApplicationCommands.Modules.Utility;

public class UtilityModule : RiasApplicationGuildModule<UtilityService>
{
    private readonly RiasPrefixProvider _prefixProvider;
    
    public UtilityModule(IPrefixProvider prefixProvider)
    {
        _prefixProvider = (RiasPrefixProvider) prefixProvider;
    }
    
    [SlashCommand("prefix")]
    [Description("Shows the prefix for this server.")]
    public IResult Prefix()
    {
        var prefix = _prefixProvider.GetPrefix(Context.GuildId);

        return !string.IsNullOrEmpty(prefix)
            ? ConfirmationResponse(Strings.UtilityPrefixIs, prefix) 
            : ConfirmationResponse(Strings.UtilityPrefixNameOrMention);
    }
}