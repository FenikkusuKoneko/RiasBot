using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Help
{
    [Name("Help")]
    public class HelpModule : RiasModule
    {
        private readonly CommandHandlerService _commandHandlerService;
        private readonly CommandService _commandService;
        
        public HelpModule(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _commandHandlerService = serviceProvider.GetRequiredService<CommandHandlerService>();
            _commandService = serviceProvider.GetRequiredService<CommandService>();
        }
        
        [Command("help", "h")]
        public async Task HelpAsync()
        {
            var embed = new DiscordEmbedBuilder()
                .WithColor(RiasUtilities.ConfirmColor)
                .WithAuthor(GetText(Localization.HelpTitle, RiasBot.CurrentUser!.Username, RiasBot.Version), RiasBot.CurrentUser.GetAvatarUrl(ImageFormat.Auto))
                .WithFooter("© 2018-2020 Copyright: Koneko#0001")
                .WithDescription(GetText(Localization.HelpInfo, Context.Prefix));

            var links = new StringBuilder();
            const string delimiter = " • ";

            if (!string.IsNullOrEmpty(Configuration.OwnerServerInvite))
            {
                var ownerServer = RiasBot.GetGuild(Configuration.OwnerServerId);
                links.Append(delimiter)
                    .Append(GetText(Localization.HelpSupportServer, ownerServer!.Name, Configuration.OwnerServerInvite))
                    .AppendLine();
            }

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Configuration.Invite))
                links.Append(GetText(Localization.HelpInviteMe, Configuration.Invite)).AppendLine();

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Configuration.Website))
                links.Append(GetText(Localization.HelpWebsite, Configuration.Website)).AppendLine();

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Configuration.Patreon))
                links.Append(GetText(Localization.HelpDonate, Configuration.Patreon)).AppendLine();

            embed.AddField(GetText(Localization.HelpLinks), links.ToString());
            await ReplyAsync(embed);
        }
        
        [Command("help", "h")]
        public async Task HelpAsync(string alias1, string? alias2 = null)
        {
            var module = GetModuleByAlias(alias1);
            var command = GetCommand(module, module is null ? alias1 : alias2);
            
            if (command is null)
            {
                await ReplyErrorAsync(Localization.HelpCommandNotFound, Context.Prefix);
                return;
            }
            
            var embed = _commandHandlerService.GenerateHelpEmbedAsync(Context.Guild!, command, Context.Prefix);
            await ReplyAsync(embed);
        }
        
        [Command("modules", "mdls")]
        public async Task ModulesAsync()
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.HelpModulesListTitle)
            }.WithFooter(GetText(Localization.HelpModulesListFooter, Context.Prefix));

            var isOwner = Context.User.Id == Configuration.MasterId;

            Func<Module, bool> modulesOwnerPredicate;
            if (isOwner)
                modulesOwnerPredicate = m => m.Parent is null;
            else
                modulesOwnerPredicate = m => m.Parent is null && m.Commands.All(c => !c.Checks.Any(x => x is OwnerOnlyAttribute));

            var modules = _commandService.GetAllModules()
                .Where(modulesOwnerPredicate)
                .OrderBy(m => m.Name)
                .ToList();

            Func<Module, bool> submodulesGroupOwnerPredicate;
            if (isOwner)
                submodulesGroupOwnerPredicate = m => !string.Equals(m.Parent.Name, m.Name, StringComparison.OrdinalIgnoreCase);
            else
                submodulesGroupOwnerPredicate = m => !string.Equals(m.Parent.Name, m.Name, StringComparison.OrdinalIgnoreCase)
                                                     && m.Commands.All(c => !c.Checks.Any(x => x is OwnerOnlyAttribute));
            
            foreach (var module in modules)
            {
                var fieldValue = "\u200B";
                if (module.Submodules.Count != 0)
                {
                    var innerFieldValue = string.Join("\n", module.Submodules
                        .Where(submodulesGroupOwnerPredicate)
                        .OrderBy(m => m.Name)
                        .Select(x => x.Name));
                    
                    if (!string.IsNullOrEmpty(innerFieldValue))
                        fieldValue = innerFieldValue;
                }

                embed.AddField(module.Name, fieldValue, true);
            }

            await ReplyAsync(embed);
        }

        [Command("commands", "cmds")]
        public async Task CommandsAsync([Remainder] string name)
        {
            var module = _commandService.GetAllModules().FirstOrDefault(m => m.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));
            if (module is null)
            {
                await ReplyErrorAsync(Localization.HelpModuleNotFound, Context.Prefix);
                return;
            }

            var isOwner = Context.User.Id == Configuration.MasterId;

            var modulesCommands = GetModuleCommands(module, isOwner);
            if (modulesCommands.Count == 0)
            {
                await ReplyErrorAsync(Localization.HelpModuleNotFound, Context.Prefix);
                return;
            }
            
            var commandsAliases = GetCommandsAliases(modulesCommands, Context.Prefix);

            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(module.Parent != null ? Localization.HelpAllCommandsForSubmodule : Localization.HelpAllCommandsForModule, module.Name)
            }.AddField(module.Name, string.Join("\n", commandsAliases), true);

            foreach (var submodule in module.Submodules.Where(x => !string.Equals(x.Name, x.Parent.Name, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Name))
            {
                var submoduleCommands = GetModuleCommands(submodule, isOwner);
                if (submoduleCommands.Count == 0)
                    continue;
                
                var submoduleCommandsAliases = GetCommandsAliases(submoduleCommands, Context.Prefix);

                embed.AddField(submodule.Name, string.Join("\n", submoduleCommandsAliases), true);
            }

            embed.WithFooter(GetText(Localization.HelpCommandInfo, Context.Prefix));
            embed.WithCurrentTimestamp();
            await ReplyAsync(embed);
        }

        [Command("allcommands", "allcmds")]
        [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task AllCommandsAsync()
        {
            var embed = new DiscordEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.HelpAllCommands),
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = GetText(Localization.HelpCommandInfo, Context.Prefix)
                },
                Timestamp = DateTimeOffset.UtcNow
            };

            var modules = _commandService.GetAllModules()
                .Where(m => m.Parent is null)
                .OrderBy(m => m.Name)
                .ToArray();
            
            var isOwner = Context.User.Id == Configuration.MasterId;
            
            foreach (var module in modules)
            {
                var moduleCommands = GetModuleCommands(module, isOwner);
                if (moduleCommands.Count == 0)
                    continue;
                
                var commandsAliases = GetCommandsAliases(moduleCommands, Context.Prefix);

                if (commandsAliases.Count != 0)
                    embed.AddField(module.Name, string.Join("\n", commandsAliases), true);

                foreach (var submodule in module.Submodules.Where(x => !string.Equals(x.Name, x.Parent.Name, StringComparison.OrdinalIgnoreCase)).OrderBy(m => m.Name))
                {
                    var submoduleCommands = GetModuleCommands(submodule, isOwner);
                    if (submoduleCommands.Count == 0)
                        continue;
                    
                    var submoduleCommandsAliases = GetCommandsAliases(submoduleCommands, Context.Prefix);
                    if (submoduleCommandsAliases.Count != 0)
                        embed.AddField(submodule.Name, string.Join("\n", submoduleCommandsAliases), true);
                }

                if (embed.Fields.Count <= 20) continue;

                var received = await SendAllCommandsMessageAsync(embed);
                if (!received) return;
                
                embed.ClearFields();
            }

            await SendAllCommandsMessageAsync(embed);
        }
        
        private Module? GetModuleByAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                return null;

            return _commandService.GetAllModules().FirstOrDefault(x =>
                x.Aliases.Any(y => string.Equals(y, alias, StringComparison.InvariantCultureIgnoreCase)));
        }
        
        private Command? GetCommand(Module? module, string? alias)
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

        private IReadOnlyList<Command> GetModuleCommands(Module module, bool isOwner)
        {
            var commands = module.Commands.AsEnumerable();
            var groupCommands = module.Submodules.FirstOrDefault(x => string.Equals(x.Name, x.Parent.Name, StringComparison.OrdinalIgnoreCase))?.Commands;
            if (groupCommands != null)
                commands = commands.Concat(groupCommands);

            return (isOwner
                ? commands
                : commands.Where(x => !x.Checks.Any(c => c is OwnerOnlyAttribute)))
                .GroupBy(x => x.Name)
                .Select(x => x.First())
                .OrderBy(x => x.Name)
                .ToImmutableList();
        }

        private IReadOnlyList<string> GetCommandsAliases(IEnumerable<Command> commands, string prefix)
            => commands.Select(x =>
            {
                var nextAliases = string.Join(", ", x.Aliases.Skip(1));
                if (!string.IsNullOrEmpty(nextAliases))
                    nextAliases = $" [{nextAliases}]";

                var moduleAlias = x.Module.Aliases.Count != 0 ? $"{x.Module.Aliases[0]} " : null;
                var isOwnerString = x.Checks.Any(c => c is OwnerOnlyAttribute) ? $" **({GetText(Localization.HelpOwnerOnly).ToLowerInvariant()})**" : null;
                return $"{prefix}{moduleAlias}{x.Aliases.FirstOrDefault()}{nextAliases}{isOwnerString}";
            }).ToImmutableList();
        
        private async Task<bool> SendAllCommandsMessageAsync(DiscordEmbed embed)
        {
            try
            {
                await ((DiscordMember) Context.User).SendMessageAsync(embed: embed);
            }
            catch
            {
                await ReplyErrorAsync(Localization.HelpCommandsNotSent);
                return false;
            }

            return true;
        }
    }
}