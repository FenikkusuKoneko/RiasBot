using Disqord;
using Disqord.Bot.Commands.Application;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Common;
using Rias.Services;

namespace Rias.ApplicationCommands;

public abstract class RiasApplicationGuildModule : DiscordApplicationGuildModuleBase
{
    protected IResult ConfirmationResponse(string message)
    {
        return Response(new LocalEmbed
        {
            Color = Utils.ConfirmationColor,
            Description = message
        });
    }
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