using Disqord;
using Disqord.Bot.Commands.Text;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Common;
using Rias.Services;

namespace Rias.TextCommands;

public abstract class RiasTextModule : DiscordTextModuleBase
{
    protected LocalizationService Localization => _localizationService.Value;
    private readonly Lazy<LocalizationService> _localizationService;
    
    public RiasTextModule()
    {
        _localizationService = new Lazy<LocalizationService>(() => Context.Services.GetRequiredService<LocalizationService>());
    }

    protected IResult ReplyConfirmationResponse(string key)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ConfirmationColor,
            Description = Localization.GetText(null, key)
        });
    }
    
    protected IResult ReplyConfirmationResponse(string key, object arg0)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ConfirmationColor,
            Description = Localization.GetText(null, key, arg0)
        });
    }
    
    protected IResult ReplyConfirmationResponse(string key, object arg0, object arg1)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ConfirmationColor,
            Description = Localization.GetText(null, key, arg0, arg1)
        });
    }
    
    protected IResult ReplyConfirmationResponse(string key, object arg0, object arg1, object arg2)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ConfirmationColor,
            Description = Localization.GetText(null, key, arg0, arg1, arg2)
        });
    }
    
    protected IResult ReplyConfirmationResponse(string key, params object[] args)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ConfirmationColor,
            Description = Localization.GetText(null, key, args)
        });
    }
    
    protected IResult ReplyErrorResponse(string key)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ErrorColor,
            Description = Localization.GetText(null, key)
        });
    }
    
    protected IResult ReplyErrorResponse(string key, object arg0)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ErrorColor,
            Description = Localization.GetText(null, key, arg0)
        });
    }
    
    protected IResult ReplyErrorResponse(string key, object arg0, object arg1)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ErrorColor,
            Description = Localization.GetText(null, key, arg0, arg1)
        });
    }
    
    protected IResult ReplyErrorResponse(string key, object arg0, object arg1, object arg2)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ErrorColor,
            Description = Localization.GetText(null, key, arg0, arg1, arg2)
        });
    }
    
    protected IResult ReplyErrorResponse(string key, params object[] args)
    {
        return Reply(new LocalEmbed
        {
            Color = Utils.ErrorColor,
            Description = Localization.GetText(null, key, args)
        });
    }
    
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

public abstract class RiasTextModule<TService> : RiasTextModule
    where TService : RiasCommandService
{
    protected TService Service => _service.Value;
    
    // The Context is created when the command begins the execution. It's not available in the constructor.
    // The Service must be called only in command methods.
    private readonly Lazy<TService> _service;

    protected RiasTextModule()
    {
        _service = new Lazy<TService>(() => Context.Services.GetRequiredService<TService>());
    }
}