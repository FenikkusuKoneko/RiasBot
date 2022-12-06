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
///     Specifies that the module or command can only be executed by authors with the given permissions.
/// </summary>
public class AuthorPermissionsAttribute : DiscordCheckAttribute
{
    private readonly Permissions _permissions;
    
    public AuthorPermissionsAttribute(Permissions permissions)
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
            var permissions = interactionContext.AuthorPermissions;

            if (!permissions.HasFlag(_permissions))
                return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingAuthorPermissions, 
                    _permissions & ~permissions));
        }
        else
        {
            if (guildContext.Bot.GetChannel(guildContext.GuildId, guildContext.ChannelId) is not IGuildChannel channel)
                throw new InvalidOperationException($"{nameof(AuthorPermissionsAttribute)} requires the context channel.");

            var guildPermissions = guildContext.Author.CalculateGuildPermissions();
            var channelPermissions = guildContext.Author.CalculateChannelPermissions(channel);
            
            var hasGuildPermissions = guildPermissions.HasFlag(_permissions);
            var hasChannelPermissions = channelPermissions.HasFlag(_permissions);

            switch (hasGuildPermissions)
            {
                case false when !hasChannelPermissions:
                    return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingAuthorGuildPermissions,
                        HumanizePermissions(_permissions & ~guildPermissions, context.GuildId, localisation)));
                case true when !hasChannelPermissions:
                    return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingAuthorChannelPermissions,
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