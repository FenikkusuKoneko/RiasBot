using Disqord;
using Disqord.Bot.Commands.Application;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Common;
using Rias.Services;

namespace Rias.ApplicationCommands;

public abstract class RiasApplicationGuildModule : DiscordApplicationGuildModuleBase
{
    protected LocalizationService Localization => _localizationService.Value;
    
    private readonly Lazy<LocalizationService> _localizationService;
    
    public RiasApplicationGuildModule()
    {
        _localizationService = new Lazy<LocalizationService>(() => Context.Services.GetRequiredService<LocalizationService>());
    }

    protected IResult ConfirmationResponse(string key)
        => Response(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localization.GetText(Context.GuildId, key)));
    
    protected IResult ConfirmationResponse(string key, object arg0)
        => Response(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localization.GetText(Context.GuildId, key, arg0)));
    
    protected IResult ConfirmationResponse(string key, object arg0, object arg1)
        => Response(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localization.GetText(Context.GuildId, key, arg0, arg1)));
    
    protected IResult ConfirmationResponse(string key, object arg0, object arg1, object arg2)
        => Response(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localization.GetText(Context.GuildId, key, arg0, arg1, arg2)));
    
    protected IResult ConfirmationResponse(string key, params object[] args)
        => Response(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localization.GetText(Context.GuildId, key, args)));
    
    protected IResult ErrorResponse(string key)
        => Response(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localization.GetText(Context.GuildId, key)));
    
    protected IResult ErrorResponse(string key, object arg0)
        => Response(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localization.GetText(Context.GuildId, key, arg0)));
    
    protected IResult ErrorResponse(string key, object arg0, object arg1)
        => Response(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localization.GetText(Context.GuildId, key, arg0, arg1)));
    
    protected IResult ErrorResponse(string key, object arg0, object arg1, object arg2)
        => Response(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localization.GetText(Context.GuildId, key, arg0, arg1, arg2)));

    protected IResult ErrorResponse(string key, params object[] args)
        => Response(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localization.GetText(Context.GuildId, key, args)));
    
    protected string GetText(string key)
        => Localization.GetText(Context.GuildId, key);
    
    protected string GetText(string key, object arg0)
        => Localization.GetText(Context.GuildId, key, arg0);
    
    protected string GetText(string key, object arg0, object arg1)
        => Localization.GetText(Context.GuildId, key, arg0, arg1);
    
    protected string GetText(string key, object arg0, object arg1, object arg2)
        => Localization.GetText(Context.GuildId, key, arg0, arg1, arg2);
    
    protected string GetText(string key, params object[] args)
        => Localization.GetText(Context.GuildId, key, args);
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