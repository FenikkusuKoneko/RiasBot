using Disqord;
using Disqord.Bot.Commands.Text;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Common;
using Rias.Services;

namespace Rias.TextCommands;

public abstract class RiasTextGuildModule : DiscordTextGuildModuleBase
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

public abstract class RiasTextGuildModule<TService> : RiasTextGuildModule
    where TService : RiasCommandService
{
    // The Context is created when the command begins execution. This is no available in the constructor.
    // The Service must be called only in command methods.
    private readonly Lazy<TService> _serviceLazy;
    protected TService Service => _serviceLazy.Value;

    protected RiasTextGuildModule()
    {
        _serviceLazy = new Lazy<TService>(() => Context.Services.GetRequiredService<TService>());
    }
}