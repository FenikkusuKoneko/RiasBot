﻿using Disqord;
using Disqord.Bot.Commands.Text;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Common;
using Rias.Services;
using Rias.Services.Responses;

namespace Rias.TextCommands;

public abstract class RiasTextGuildModule : DiscordTextGuildModuleBase
{
    protected LocalisationService Localisation => _localizationService.Value;
    private readonly Lazy<LocalisationService> _localizationService;

    public RiasTextGuildModule()
    {
        _localizationService = new Lazy<LocalisationService>(() => Context.Services.GetRequiredService<LocalisationService>());
    }

    protected static LocalEmbed SuccessEmbed => new LocalEmbed().WithColor(Utils.SuccessColor);

    protected IResult SuccessReply(string key)
        => Reply(GetEmbed(Utils.SuccessColor, Localisation.GetText(Context.GuildId, key)));

    protected IResult SuccessReply(string key, object arg0)
        => Reply(GetEmbed(Utils.SuccessColor, Localisation.GetText(Context.GuildId, key, arg0)));

    protected IResult SuccessReply(string key, object arg0, object arg1)
        => Reply(GetEmbed(Utils.SuccessColor, Localisation.GetText(Context.GuildId, key, arg0, arg1)));

    protected IResult ErrorReply(string key)
        => Reply(GetEmbed(Utils.ErrorColor, Localisation.GetText(Context.GuildId, key)));

    protected IResult ErrorReply(string key, object arg0)
        => Reply(GetEmbed(Utils.ErrorColor, Localisation.GetText(Context.GuildId, key, arg0)));

    protected IResult ErrorReply(string key, object arg0, object arg1)
        => Reply(GetEmbed(Utils.ErrorColor, Localisation.GetText(Context.GuildId, key, arg0, arg1)));

    protected IResult ErrorReply<T>(RiasResult<T> result)
        => Reply(GetEmbed(Utils.ErrorColor, result.ErrorReason));

    protected string GetText(string key)
        => Localisation.GetText(Context.GuildId, key);

    protected string GetText(string key, object arg0)
        => Localisation.GetText(Context.GuildId, key, arg0);

    protected string GetText(string key, object arg0, object arg1)
        => Localisation.GetText(Context.GuildId, key, arg0, arg1);

    protected string GetText(string key, object arg0, object arg1, object arg2)
        => Localisation.GetText(Context.GuildId, key, arg0, arg1, arg2);

    private LocalEmbed GetEmbed(Color color, string description) => new LocalEmbed()
        .WithColor(color)
        .WithDescription(description)
        .WithFooter(Context.Author.Tag, Context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 128));
}

public abstract class RiasTextGuildModule<TService> : RiasTextGuildModule
    where TService : RiasCommandService
{
    protected TService Service => _service.Value;

    // The Context is created when the command begins the execution. It's not available in the constructor.
    // The Service must be called only in command methods.
    private readonly Lazy<TService> _service;

    protected RiasTextGuildModule()
    {
        _service = new Lazy<TService>(() => Context.Services.GetRequiredService<TService>());
    }
}