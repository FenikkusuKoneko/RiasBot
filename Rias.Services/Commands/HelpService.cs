using System.Text;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Microsoft.Extensions.Options;
using Qmmands.Text;
using Rias.Common;
using Rias.Database;

namespace Rias.Services.Commands;

public class HelpService : RiasCommandService
{
    private readonly LocalisationService _localisation;
    private readonly RiasOptions _options;
    
    public HelpService(RiasDbContext db, LocalisationService localisation, IOptions<RiasOptions> options)
        : base(db)
    {
        _localisation = localisation;
        _options = options.Value;
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
            title += $" [{_localisation.GetText(guild?.Id, Strings.Help.OwnerOnly).ToLowerInvariant()}]";
        
        var commandInfoKey = $"{command.Module.Name.Replace(' ', '_').ToLower()}_{command.Name.Replace(' ', '_')}";
        var description = _localisation.GetCommandText(guild?.Id, commandInfoKey);

        if (!string.IsNullOrEmpty(description))
        {
            description = description.Replace("{prefix}", prefixString).Replace("{currency}", _options.CurrencyEmoji)
                          + $"\n\n{_localisation.GetText(guild?.Id, Strings.Help.Module, Markdown.Bold(moduleName))}";
        }
        else
        {
            description = _localisation.GetText(guild?.Id, Strings.NoDescription);
        }

        var embed = new LocalEmbed()
            .WithColor(Utils.ConfirmationColor)
            .WithTitle(title)
            .WithDescription(description)
            .WithFooter($"{user.Tag} | <> - mandatory; [] - optional", user.GetAvatarUrl(CdnAssetFormat.Automatic, 128));
        
        var examples = _localisation.GetCommandText(guild?.Id, $"{commandInfoKey}_examples");
        if (!string.IsNullOrEmpty(examples))
        {
            examples = string.Join('\n', examples.Split('\n').Select(ex => $"{prefixString}{command.Aliases[0]} {ex}"));
            embed.AddField(_localisation.GetText(guild?.Id, Strings.Examples), examples);
        }

        var usages = new StringBuilder();
        foreach (var cmd in commands.OrderBy(c => c.Parameters.Count))
        {
            usages.AppendLine()
                .Append($"{prefixString}{cmd.Aliases[0]} ")
                .AppendJoin(' ', cmd.Parameters.Select(p => p.ParameterInfo is not null && p.ParameterInfo.IsOptional ? $"[{p.Name}]" : $"<{p.Name}>"));
        }
        
        embed.AddField(_localisation.GetText(guild?.Id, Strings.Usages), usages.ToString());
        return embed;
    }
}