using Disqord.Bot.Commands.Text;
using Qmmands;
using Qmmands.Text;
using Rias.Common;
using Rias.Services;
using Rias.Services.Commands;

namespace Rias.TextCommands.Modules;

[Name("Utility")]
public class UtilityModule : RiasTextGuildModule<UtilityService>
{
    private readonly RiasPrefixProvider _prefixProvider;
    
    public UtilityModule(IPrefixProvider prefixProvider)
    {
        _prefixProvider = (RiasPrefixProvider) prefixProvider;
    }
    
    [TextCommand("prefix")]
    public IResult Prefix()
    {
        var prefix = _prefixProvider.GetPrefix(Context.GuildId);

        return !string.IsNullOrEmpty(prefix) 
            ? ReplyConfirmationResponse(Strings.Utility.PrefixIs, prefix) 
            : ReplyConfirmationResponse(Strings.Utility.PrefixNameOrMention);
    }
}