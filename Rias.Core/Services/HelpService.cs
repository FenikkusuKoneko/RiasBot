using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Qmmands;

namespace Rias.Core.Services
{
    public class HelpService : RiasService
    {
        private readonly CommandService _commandService;

        public HelpService(IServiceProvider services) : base(services)
        {
            _commandService = services.GetRequiredService<CommandService>();
        }

        public Module? GetModuleByAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                return null;

            return _commandService.GetAllModules().FirstOrDefault(x =>
                x.Aliases.Any(y => string.Equals(y, alias, StringComparison.InvariantCultureIgnoreCase)));
        }

        public Command? GetCommand(Module? module, string? alias)
        {
            if (module is null && !string.IsNullOrEmpty(alias))
                return GetCommand(alias);
            
            if (string.IsNullOrEmpty(alias))
                return module?.Commands.FirstOrDefault(x => x.Aliases.Count == 0);

            return module?.Commands.FirstOrDefault(x =>
                x.Aliases.Any(y => string.Equals(y, alias, StringComparison.InvariantCultureIgnoreCase)));
        }
        
        private Command? GetCommand(string alias) => _commandService.GetAllCommands().FirstOrDefault(x =>
        {
            if (x.Aliases is null)
                return false;

            return x.Module.Aliases.Count == 0 && x.Aliases.Any(y => string.Equals(y, alias, StringComparison.InvariantCultureIgnoreCase));
        });

        public IReadOnlyList<Command> GetModuleCommands(Module module) =>
            module.Commands.DistinctBy(c => c.Name).OrderBy(x => x.Name).ToImmutableList();
        
        public IReadOnlyList<string> GetCommandsAliases(IEnumerable<Command> commands, string prefix)
            => commands.Select(x =>
            {
                var nextAliases = string.Join(", ", x.Aliases.Skip(1));
                if (!string.IsNullOrEmpty(nextAliases))
                    nextAliases = $"[{nextAliases}]";

                var moduleAlias = x.Module.Aliases.Count != 0 ? $"{x.Module.Aliases[0]} " : null;
                return $"{prefix}{moduleAlias}{x.Aliases.FirstOrDefault()} {nextAliases}";
            }).ToImmutableList();
    }
}