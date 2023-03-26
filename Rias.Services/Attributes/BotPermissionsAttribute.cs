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
    public readonly Permissions Permissions;

    public BotPermissionsAttribute(Permissions permissions)
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
            var permissions = interactionContext.ApplicationPermissions;

            if (!permissions.HasFlag(Permissions))
                return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingBotPermissions,
                    Permissions & ~permissions));
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

            var hasGuildPermissions = guildPermissions.HasFlag(Permissions);
            var hasChannelPermissions = channelPermissions.HasFlag(Permissions);

            switch (hasGuildPermissions)
            {
                case false when !hasChannelPermissions:
                    return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingBotGuildPermissions,
                        HumanizePermissions(Permissions & ~guildPermissions, context.GuildId, localisation)));
                case true when !hasChannelPermissions:
                    return Results.Failure(localisation.GetText(context.GuildId, Strings.Attribute.MissingBotChannelPermissions,
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