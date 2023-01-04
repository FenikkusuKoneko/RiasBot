using System.Text;
using Disqord;
using Disqord.Bot.Commands;
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
    public IResult Help()
    {
        return Reply("No help bro.");
    }

    [TextCommand("help", "h")]
    public IResult Help([Remainder] string command)
    {
        var commands = _commandService
            .GetCommandMapProvider()
            .GetRequiredMap<ITextCommandMap>()
            .FindMatches(command.ToCharArray())
            .Select(m => m.Command)
            .ToList();

        if (commands.Count == 0)
            return ReplyErrorResponse(Strings.Help.CommandNotFound, Context.Prefix.Stringify());

        var guild = Context.GuildId.HasValue ? Context.Bot.GetGuild(Context.GuildId.Value) : null;
        var embed = Service.GenerateHelpEmbed(Context.Author, guild, commands, Context.Prefix);
        return Reply(embed);
    }

    [TextCommand("modules", "mdls")]
    public async Task<IResult> ModulesAsync()
    {
        var isOwner = await Context.Bot.IsOwnerAsync(Context.AuthorId);
        
        Func<ITextModule, bool> modulesPredicate;
        if (isOwner)
            modulesPredicate = m => m.Parent is null;
        else
            modulesPredicate = m => m.Parent is null && m.Commands.All(c => !c.Checks.Any(ch => ch is RequireBotOwnerAttribute));
        
        var modules = _commandService.EnumerateTextModules()
            .Where(modulesPredicate)
            .OrderBy(m => m.Name)
            .ToList();
        
        Func<ITextModule, bool> submodulesOwnerPredicate;
        
        if (isOwner)
            submodulesOwnerPredicate = m => !string.Equals(m.Parent?.Name, m.Name, StringComparison.OrdinalIgnoreCase);
        else
            submodulesOwnerPredicate = m => !string.Equals(m.Parent?.Name, m.Name, StringComparison.OrdinalIgnoreCase)
                                            && m.Commands.All(c => !c.Checks.Any(ch => ch is RequireBotOwnerAttribute));

        var description = new StringBuilder()
            .AppendLine(GetText(Strings.Help.ModulesListFooter, Context.Prefix.Stringify()))
            .AppendLine();
        
        foreach (var module in modules)
        {
            description.Append(Markdown.Code(module.Name));
            
            if (module.Submodules.Count != 0)
            {
                var submodules = module.Submodules
                    .Where(submodulesOwnerPredicate)
                    .OrderBy(m => m.Name)
                    .Select(m => Markdown.Code(m.Name))
                    .ToList();

                if (submodules.Count != 0)
                    description.Append(": ").AppendJoin(", ", submodules);
            }

            description.AppendLine();
        }

        var embed = new LocalEmbed()
            .WithColor(Utils.SuccessColor)
            .WithTitle(GetText(Strings.Help.ModulesListTitle))
            .WithDescription(description.ToString())
            .WithFooter(Context.Author.Tag, Context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 128));
        
        return Reply(embed);
    }

    [TextCommand("commands", "cmds")]
    public async Task<IResult> CommandsAsync([Remainder, Name("module")] string? moduleName = null)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
            return await AllCommandsAsync();
        
        var module = _commandService
            .EnumerateTextModules()
            .SelectMany(m => m.Submodules.Prepend(m))
            .FirstOrDefault(m => m.Name.StartsWith(moduleName, StringComparison.OrdinalIgnoreCase));
        
        if (module is null)
            return ReplyErrorResponse(Strings.Help.ModuleNotFound, Context.Prefix.Stringify());
        
        var isOwner = await Context.Bot.IsOwnerAsync(Context.AuthorId);
        var commands = GetModuleCommands(module, isOwner).ToList();
        
        if (commands.Count == 0)
            return ReplyErrorResponse(Strings.Help.ModuleNotFound, Context.Prefix.Stringify());

        var commandAliases = GetAliases(commands).ToList();
        var description = new StringBuilder()
            .AppendLine(GetText(Strings.Help.CommandInfo, Context.Prefix.Stringify()))
            .AppendLine()
            .AppendLine($"**{module.Name}:** {string.Join(" ", commandAliases.Select(Markdown.Code))}");
        
        foreach (var submodule in module.Submodules.OrderBy(sm => sm.Name))
        {
            var groupModuleCommands = GetModuleCommands(submodule, isOwner).ToList();

            if (groupModuleCommands.Count != 0)
            {
                var groupCommandAliases = GetAliases(groupModuleCommands).ToList();
                description.AppendLine($"**{submodule.Name}:** {string.Join(" ", groupCommandAliases.Select(Markdown.Code))}");
            }
        }

        var embed = new LocalEmbed()
            .WithColor(Utils.SuccessColor)
            .WithTitle(GetText(module.Parent is null ? Strings.Help.AllModuleCommands : Strings.Help.AllSubmoduleCommands, module.Name))
            .WithDescription(description.ToString())
            .WithFooter(Context.Author.Tag, Context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 128));

        return Reply(embed);
    }

    [TextCommand("allcommands", "allcmds")]
    public async Task<IResult> AllCommandsAsync()
    {
        var isOwner = await Context.Bot.IsOwnerAsync(Context.AuthorId);
        
        Func<ITextModule, bool> modulesPredicate;
        if (isOwner)
            modulesPredicate = m => m.Parent is null;
        else
            modulesPredicate = m => m.Parent is null && m.Commands.All(c => !c.Checks.Any(ch => ch is RequireBotOwnerAttribute));
        
        var modules = _commandService.EnumerateTextModules()
            .Where(modulesPredicate)
            .OrderBy(m => m.Name)
            .ToList();
        
        Func<ITextModule, bool> submodulesOwnerPredicate;
        
        if (isOwner)
            submodulesOwnerPredicate = m => !string.Equals(m.Parent?.Name, m.Name, StringComparison.OrdinalIgnoreCase);
        else
            submodulesOwnerPredicate = m => !string.Equals(m.Parent?.Name, m.Name, StringComparison.OrdinalIgnoreCase)
                                            && m.Commands.All(c => !c.Checks.Any(ch => ch is RequireBotOwnerAttribute));

        var description = new StringBuilder()
            .AppendLine(GetText(Strings.Help.CommandInfo, Context.Prefix.Stringify()))
            .AppendLine();
        
        foreach (var module in modules)
        {
            var moduleCommands = GetModuleCommands(module, isOwner).ToList();
            if (moduleCommands.Count == 0)
                continue;
            
            var commandAliases = GetAliases(moduleCommands).ToList();
            description.Append($"**{module.Name}:** {string.Join(" ", commandAliases.Select(Markdown.Code))}");
            
            if (module.Submodules.Count != 0)
            {
                var submodules = module.Submodules
                    .Where(submodulesOwnerPredicate)
                    .OrderBy(m => m.Name)
                    .ToList();

                if (submodules.Count != 0)
                {
                    foreach (var submodule in submodules)
                    {
                        var groupModuleCommands = GetModuleCommands(submodule, isOwner).ToList();
                        if (groupModuleCommands.Count != 0)
                        {
                            var groupCommandAliases = GetAliases(groupModuleCommands).ToList();
                            description.AppendLine().Append($"**{submodule.Name}:** {string.Join(" ", groupCommandAliases.Select(Markdown.Code))}");
                        }
                    }
                }
            }

            description.AppendLine();
        }
        
        var embed = new LocalEmbed()
            .WithColor(Utils.SuccessColor)
            .WithTitle(GetText(Strings.Help.AllCommands))
            .WithDescription(description.ToString())
            .WithFooter(Context.Author.Tag, Context.Author.GetAvatarUrl(CdnAssetFormat.Automatic, 128));
        
        return Reply(embed);
    }

    private static IEnumerable<ITextCommand> GetModuleCommands(ITextModule module, bool isOwner)
    {
        var commands = module.Commands.AsEnumerable();
        var submoduleCommands = module.Submodules.FirstOrDefault(sm => string.Equals(sm.Name, sm.Parent?.Name, StringComparison.OrdinalIgnoreCase))?.Commands;
        
        if (submoduleCommands is not null)
            commands = commands.Concat(submoduleCommands);

        if (!isOwner)
            commands = commands.Where(c => !c.Checks.Any(ch => ch is RequireBotOwnerAttribute));

        return commands.GroupBy(c => c.Name)
            .Select(cg => cg.First())
            .OrderBy(c => c.Name);
    }

    private static IEnumerable<string> GetAliases(IEnumerable<ITextCommand> commands)
        => commands.Select(command =>
        {
            var aliases = string.Join('/', command.Aliases);
            var moduleAlias = command.Module.Aliases.Count != 0 ? $"{command.Module.Aliases[0]} " : null;
            var isMasterOnly = command.Checks.Any(c => c is RequireBotOwnerAttribute);
            aliases = $"{moduleAlias}{aliases}";
            
            return isMasterOnly ? $"{aliases}*" : aliases;
        });
}