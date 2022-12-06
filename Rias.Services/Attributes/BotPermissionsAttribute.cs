using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Interaction;
using Disqord.Gateway;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Common;

namespace Rias.Services.Attributes;

/// <summary>
///     Specifies that the module or command can only be executed if the bot has given guild permissions.
/// </summary>
public class BotPermissionsAttribute : DiscordCheckAttribute
{
    private readonly Permissions _permissions;
    
    public BotPermissionsAttribute(Permissions permissions)
    {
        _permissions = permissions;
    }

    public override ValueTask<IResult> CheckAsync(IDiscordCommandContext context)
    {
        if (context is not IDiscordGuildCommandContext guildContext)
            return Results.Success;
        
        var localisation = context.Services.GetRequiredService<LocalisationService>();
        
        if (context is IDiscordInteractionCommandContext interactionContext)
        {
            var permissions = interactionContext.ApplicationPermissions;

            if (!permissions.HasFlag(_permissions))
                return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingBotPermissions, 
                    _permissions & ~permissions));
        }
        else
        {
            if (guildContext.Bot.GetChannel(guildContext.GuildId, guildContext.ChannelId) is not IGuildChannel channel)
                throw new InvalidOperationException($"{nameof(AuthorPermissionsAttribute)} requires the context channel.");
            
            var currentMember = guildContext.Bot.GetCurrentMember(guildContext.GuildId);
            if (currentMember == null)
                throw new InvalidOperationException($"{nameof(BotPermissionsAttribute)} requires the current member cached.");

            var guildPermissions = currentMember.CalculateGuildPermissions();
            var channelPermissions = currentMember.CalculateChannelPermissions(channel);
            
            var hasGuildPermissions = guildPermissions.HasFlag(_permissions);
            var hasChannelPermissions = channelPermissions.HasFlag(_permissions);

            switch (hasGuildPermissions)
            {
                case false when !hasChannelPermissions:
                    return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingBotGuildPermissions,
                        HumanizePermissions(_permissions & ~guildPermissions, context.GuildId, localisation)));
                case true when !hasChannelPermissions:
                    return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingBotChannelPermissions,
                        HumanizePermissions(_permissions & ~channelPermissions, context.GuildId, localisation)));
            }
        }
        
        return Results.Success;
    }
    
    private static string HumanizePermissions(Permissions permissions, Snowflake? guildId, LocalisationService localisation)
    {
        var permissionsList = Enum.GetValues<Permissions>()
            .Where(p => p is not Permissions.None && permissions.HasFlag(p))
            .ToList();

        return permissionsList.Humanize(p => Markdown.Code(p.Humanize(LetterCasing.Title)),
            localisation.GetText(guildId, Strings.And));
    }
}