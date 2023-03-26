using Disqord.Bot.Commands.Application;
using Disqord.Bot.Commands.Text;
using Qmmands;
using Rias.Common;
using Rias.Services.Commands;
using Rias.Services.Providers;

namespace Rias.ApplicationCommands.Modules.Utility;

public class UtilityModule : RiasApplicationGuildModule<UtilityService>
{
    private readonly RiasPrefixProvider _prefixProvider;

    public UtilityModule(IPrefixProvider prefixProvider)
    {
        _prefixProvider = (RiasPrefixProvider) prefixProvider;
    }

    [SlashCommand("prefix")]
    [Description("Shows my prefix in this server.")]
    public IResult Prefix()
    {
        var prefix = _prefixProvider.GetPrefix(Context.GuildId);

        return !string.IsNullOrEmpty(prefix)
            ? SuccessResponse(Strings.Utility.PrefixIs, prefix)
            : SuccessResponse(Strings.Utility.PrefixNameOrMention);
    }
}