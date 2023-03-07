using System.Diagnostics;
using System.Globalization;
using System.Text;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Humanizer;
using Microsoft.Extensions.Options;
using Qmmands;
using Qmmands.Text;
using Rias.Common;
using Rias.Database;
using Rias.Services.Attributes;

namespace Rias.Services.Commands;

public class HelpService : RiasCommandService
{
    private readonly RiasConfiguration _configuration;
    
    public HelpService(
        RiasDbContext db,
        LocalisationService localisation,
        IOptions<RiasConfiguration> options)
        : base(db, localisation)
    {
        _configuration = options.Value;
    }

    public LocalEmbed GenerateHelpEmbed(IUser user, CachedGuild? guild, IList<ITextCommand> commands, IPrefix prefix)
    {
        var command = commands[0];
        var prefixString = prefix.Stringify();
        
        var moduleName = command.Module.Name;
        if (command.Module.Parent != null && !string.Equals(command.Module.Name, command.Module.Parent.Name, StringComparison.OrdinalIgnoreCase))
            moduleName = $"{command.Module.Parent.Name} -> {moduleName}";
        
        var moduleAlias = command.Module.Aliases.Count != 0 ? $"{command.Module.Aliases[0]} " : string.Empty;
        var title = string.Join(" / ", command.Aliases.Select(a => $"{moduleAlias}{a}"));
        
        if (command.Checks.Any(c => c is RequireBotOwnerAttribute))
            title += $" [{Localisation.GetText(guild?.Id, Strings.Help.OwnerOnly).ToLowerInvariant()}]";
        
        var commandInfoKey = $"{command.Module.Name.Replace(' ', '_').ToLower()}_{command.Name.Replace(' ', '_')}";
        var description = Localisation.GetCommandText(guild?.Id, commandInfoKey);

        if (!string.IsNullOrEmpty(description))
        {
            description = description.Replace("{prefix}", prefixString).Replace("{currency}", _configuration.CurrencyEmoji)
                          + $"\n\n{Localisation.GetText(guild?.Id, Strings.Help.Module, Markdown.Bold(moduleName))}";
        }
        else
        {
            description = Localisation.GetText(guild?.Id, Strings.NoDescription)
                          + $"\n\n{Localisation.GetText(guild?.Id, Strings.Help.Module, Markdown.Bold(moduleName))}";
        }

        var embed = new LocalEmbed()
            .WithColor(Utils.SuccessColor)
            .WithTitle(title)
            .WithDescription(description)
            .WithFooter(Localisation.GetText(guild?.Id, Strings.Help.CommandInfoFooter, user.Tag), user.GetAvatarUrl(CdnAssetFormat.Automatic, 128));

        string? requiredAuthorPermissions = null;
        string? requiredBotPermissions = null;
        
        foreach (var attribute in command.Checks)
        {
            switch (attribute)
            {
                case AuthorPermissionsAttribute authorPermissionsAttribute:
                {
                    var permissionsList = Enum.GetValues<Permissions>()
                        .Where(p => p is not Permissions.None && authorPermissionsAttribute.Permissions.HasFlag(p))
                        .ToList();

                    requiredAuthorPermissions = string.Join(" ", permissionsList.Select(p => Markdown.Code(p.Humanize(LetterCasing.Title))));
                    break;
                }
                case BotPermissionsAttribute botPermissionsAttribute:
                {
                    var permissionsList = Enum.GetValues<Permissions>()
                        .Where(p => p is not Permissions.None && botPermissionsAttribute.Permissions.HasFlag(p))
                        .ToList();

                    requiredBotPermissions = string.Join(" ", permissionsList.Select(p => Markdown.Code(p.Humanize(LetterCasing.Title))));
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(requiredAuthorPermissions) || !string.IsNullOrEmpty(requiredBotPermissions))
        {
            embed.AddField(Localisation.GetText(guild?.Id, Strings.Help.RequiredPermissions),
                $"{(string.IsNullOrEmpty(requiredAuthorPermissions) ? string.Empty : Localisation.GetText(guild?.Id, Strings.Help.RequiredPermissionsYou, requiredAuthorPermissions))}\n" +
                $"{(string.IsNullOrEmpty(requiredBotPermissions) ? string.Empty : Localisation.GetText(guild?.Id, Strings.Help.RequiredPermissionsMe, requiredBotPermissions))}",
                true);
        }
        
        var rateLimitAttribute = command.CustomAttributes.OfType<RateLimitAttribute>().FirstOrDefault();
        if (rateLimitAttribute is not null)
        {
            var cooldownWindow = rateLimitAttribute.Window.Humanize(1, new CultureInfo(Localisation.GetGuildLocale(guild?.Id)));
            var cooldownScope = Localisation.GetText(guild?.Id, rateLimitAttribute.BucketType switch
            {
                RateLimitBucketType.User => Strings.User,
                RateLimitBucketType.Member => Strings.Member,
                RateLimitBucketType.Guild => Strings.Server,
                RateLimitBucketType.Channel => Strings.Channel,
                _ => throw new UnreachableException()
            }).ToLowerInvariant();

            var cooldownValue = $"{Localisation.GetText(guild?.Id, Strings.Help.HelpCooldownUses, rateLimitAttribute.Uses)}\n" +
                                $"{Localisation.GetText(guild?.Id, Strings.Help.HelpCooldownWindow, cooldownWindow)}\n" +
                                $"{Localisation.GetText(guild?.Id, Strings.Help.HelpCooldownScope, cooldownScope)}";
            
            embed.AddField(Localisation.GetText(guild?.Id, Strings.Help.HelpCooldown), cooldownValue, true);
        }
        
        var examples = Localisation.GetCommandText(guild?.Id, $"{commandInfoKey}_examples");
        if (!string.IsNullOrEmpty(examples))
        {
            examples = string.Join('\n', examples.Split('\n').Select(ex => $"{prefixString}{command.Aliases[0]} {ex}"));
            embed.AddField(Localisation.GetText(guild?.Id, Strings.Examples), examples);
        }

        var usages = new StringBuilder();
        foreach (var cmd in commands)
        {
            usages.AppendLine()
                .Append($"{prefixString}{cmd.Aliases[0]} ")
                .AppendJoin(' ', cmd.Parameters.Select(p => p.ParameterInfo is not null && p.ParameterInfo.IsOptional ? $"[{p.Name}]" : $"<{p.Name}>"));
        }
        
        embed.AddField(Localisation.GetText(guild?.Id, Strings.Usages), usages.ToString());
        return embed;
    }
}