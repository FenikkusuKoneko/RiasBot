using Disqord;
using Disqord.Bot.Commands.Text;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Common;
using Rias.Services;

namespace Rias.TextCommands;

public abstract class RiasTextModule : DiscordTextModuleBase
{
    protected LocalisationService Localisation => _localizationService.Value;
    private readonly Lazy<LocalisationService> _localizationService;
    
    public RiasTextModule()
    {
        _localizationService = new Lazy<LocalisationService>(() => Context.Services.GetRequiredService<LocalisationService>());
    }

    protected IResult ReplyConfirmationResponse(string key)
        => Reply(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localisation.GetText(null, key)));

    protected IResult ReplyConfirmationResponse(string key, object arg0)
        => Reply(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localisation.GetText(null, key, arg0)));
    
    protected IResult ReplyConfirmationResponse(string key, object arg0, object arg1)
        => Reply(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localisation.GetText(null, key, arg0, arg1)));
    
    protected IResult ReplyConfirmationResponse(string key, object arg0, object arg1, object arg2)
        => Reply(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localisation.GetText(null, key, arg0, arg1, arg2)));
    
    protected IResult ReplyConfirmationResponse(string key, params object[] args)
        => Reply(new LocalEmbed().WithColor(Utils.ConfirmationColor).WithDescription(Localisation.GetText(null, key, args)));
    
    protected IResult ReplyErrorResponse(string key)
        => Reply(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localisation.GetText(null, key)));
    
    protected IResult ReplyErrorResponse(string key, object arg0)
        => Reply(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localisation.GetText(null, key, arg0)));
    
    protected IResult ReplyErrorResponse(string key, object arg0, object arg1)
        => Reply(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localisation.GetText(null, key, arg0, arg1)));
    
    protected IResult ReplyErrorResponse(string key, object arg0, object arg1, object arg2)
        => Reply(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localisation.GetText(null, key, arg0, arg1, arg2)));
    
    protected IResult ReplyErrorResponse(string key, params object[] args)
        => Reply(new LocalEmbed().WithColor(Utils.ErrorColor).WithDescription(Localisation.GetText(null, key, args)));
    
    protected string GetText(string key)
        => Localisation.GetText(Context.GuildId, key);
    
    protected string GetText(string key, object arg0)
        => Localisation.GetText(Context.GuildId, key, arg0);
    
    protected string GetText(string key, object arg0, object arg1)
        => Localisation.GetText(Context.GuildId, key, arg0, arg1);
    
    protected string GetText(string key, object arg0, object arg1, object arg2)
        => Localisation.GetText(Context.GuildId, key, arg0, arg1, arg2);
    
    protected string GetText(string key, params object[] args)
        => Localisation.GetText(Context.GuildId, key, args);
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