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
    public readonly Permissions Permissions;

    public AuthorPermissionsAttribute(Permissions permissions)
    {
        Permissions = permissions;
    }

    public override ValueTask<IResult> CheckAsync(IDiscordCommandContext context)
    {
        if (context is not IDiscordGuildCommandContext guildContext)
            return Results.Success;

        var localisation = context.Services.GetRequiredService<LocalisationService>();

        if (context is IDiscordInteractionCommandContext interactionContext)
        {
            var permissions = interactionContext.AuthorPermissions;

            if (!permissions.HasFlag(Permissions))
                return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingAuthorPermissions,
                    Permissions & ~permissions));
        }
        else
        {
            if (guildContext.Bot.GetChannel(guildContext.GuildId, guildContext.ChannelId) is not IGuildChannel channel)
                throw new InvalidOperationException($"{nameof(AuthorPermissionsAttribute)} requires the context channel.");

            var guildPermissions = guildContext.Author.CalculateGuildPermissions();
            var channelPermissions = guildContext.Author.CalculateChannelPermissions(channel);

            var hasGuildPermissions = guildPermissions.HasFlag(Permissions);
            var hasChannelPermissions = channelPermissions.HasFlag(Permissions);

            switch (hasGuildPermissions)
            {
                case false when !hasChannelPermissions:
                    return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingAuthorGuildPermissions,
                        HumanizePermissions(Permissions & ~guildPermissions, context.GuildId, localisation)));
                case true when !hasChannelPermissions:
                    return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingAuthorChannelPermissions,
                        HumanizePermissions(Permissions & ~channelPermissions, context.GuildId, localisation)));
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