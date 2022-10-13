using Disqord.Gateway;
using Qmmands;
using Qmmands.Text;
using Rias.Common;
using Rias.Services.Commands;

namespace Rias.TextCommands.Modules;

[Name("Help")]
public class HelpModule : RiasTextModule<HelpService>
{
    private readonly ICommandService _commandService;
    
    public HelpModule(ICommandService commandService)
    {
        _commandService = commandService;
    }
    
    [TextCommand("help", "h")]
    public IResult HelpAsync()
    {
        return Reply("No help bro.");
    }

    [TextCommand("help", "h")]
    public IResult HelpAsync(string command, string? subcommand = null)
    {
        var module = _commandService.EnumerateTextModules().FirstOrDefault(m =>
            m.Aliases.Any(a => string.Equals(a, command, StringComparison.OrdinalIgnoreCase)));
        
        var commands = GetCommands(module, module is null ? command : subcommand).ToList();

        if (commands.Count == 0)
            return ReplyErrorResponse(Strings.HelpCommandNotFound, Context.Prefix.Stringify());

        var guild = Context.GuildId.HasValue ? Context.Bot.GetGuild(Context.GuildId.Value) : null;
        var embed = Service.GenerateHelpEmbed(Context.Author, guild, commands, Context.Prefix);
        return Reply(embed);
    }

    private IEnumerable<ITextCommand> GetCommands(ITextModule? module, string? alias)
    {
        if (module is null && !string.IsNullOrEmpty(alias))
            return GetCommands(alias);
            
        if (string.IsNullOrEmpty(alias))
            return module?.Commands.Where(x => x.Aliases.Count == 0) ?? Enumerable.Empty<ITextCommand>();

        return module?.Commands.Where(c =>
                   c.Aliases.Any(a => string.Equals(a, alias, StringComparison.OrdinalIgnoreCase)))
               ?? Enumerable.Empty<ITextCommand>();
    }
    
    private IEnumerable<ITextCommand> GetCommands(string alias) => _commandService.EnumerateTextModules().SelectMany(m => m.Commands).Where(c =>
    {
        if (c.Aliases.Count == 0)
            return false;

        return c.Module.Aliases.Count == 0 && c.Aliases.Any(y => string.Equals(y, alias, StringComparison.OrdinalIgnoreCase));
    });
}