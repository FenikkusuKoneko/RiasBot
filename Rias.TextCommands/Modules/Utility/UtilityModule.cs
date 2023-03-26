using Disqord.Bot.Commands.Text;
using Qmmands;
using Qmmands.Text;
using Rias.Common;
using Rias.Services.Commands;
using Rias.Services.Providers;

namespace Rias.TextCommands.Modules.Utility;

[Name("Utility")]
public partial class UtilityModule : RiasTextGuildModule<UtilityService>
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
            ? SuccessReply(Strings.Utility.PrefixIs, prefix)
            : SuccessReply(Strings.Utility.PrefixNameOrMention);
    }
}