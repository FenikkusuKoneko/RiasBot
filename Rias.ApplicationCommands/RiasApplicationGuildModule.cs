using Disqord;
using Disqord.Bot.Commands.Application;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Common;
using Rias.Services;

namespace Rias.ApplicationCommands;

public abstract class RiasApplicationGuildModule : DiscordApplicationGuildModuleBase
{
    protected LocalisationService Localisation => _localizationService.Value;

    private readonly Lazy<LocalisationService> _localizationService;

    public RiasApplicationGuildModule()
    {
        _localizationService = new Lazy<LocalisationService>(() => Context.Services.GetRequiredService<LocalisationService>());
    }

    protected IResult SuccessResponse(string key)
        => Response(new LocalEmbed().WithColor(Utils.SuccessColor).WithDescription(Localisation.GetText(Context.GuildId, key)));

    protected IResult SuccessResponse(string key, object arg0)
        => Response(new LocalEmbed().WithColor(Utils.SuccessColor).WithDescription(Localisation.GetText(Context.GuildId, key, arg0)));
}

public abstract class RiasApplicationGuildModule<TService> : RiasApplicationGuildModule
    where TService : RiasCommandService
{
    // The Context is created when the command begins execution. This is no available in the constructor.
    // The Service must be called only in command methods.
    private readonly Lazy<TService> _serviceLazy;
    protected TService Service => _serviceLazy.Value;

    protected RiasApplicationGuildModule()
    {
        _serviceLazy = new Lazy<TService>(() => Context.Services.GetRequiredService<TService>());
    }
}