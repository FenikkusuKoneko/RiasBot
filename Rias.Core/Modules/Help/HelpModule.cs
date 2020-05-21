using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Help
{
    [Name("Help")]
    public class HelpModule : RiasModule
    {
        private readonly CommandService _commandService;
        
        public HelpModule(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _commandService = serviceProvider.GetRequiredService<CommandService>();
        }
        
        [Command("help")]
        public async Task HelpAsync()
        {
            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Author = new LocalEmbedAuthorBuilder
                {
                    Name = GetText(Localization.HelpTitle, RiasBot.CurrentUser.Name, Rias.Version),
                    IconUrl = RiasBot.CurrentUser.GetAvatarUrl()
                },
                Footer = new LocalEmbedFooterBuilder().WithText("© 2018-2020 Copyright: Koneko#0001")
            };
            
            embed.WithDescription(GetText(Localization.HelpInfo, Context.Prefix));

            var links = new StringBuilder();
            const string delimiter = " • ";

            if (!string.IsNullOrEmpty(Credentials.OwnerServerInvite))
            {
                var ownerServer = RiasBot.GetGuild(Credentials.OwnerServerId);
                links.Append(delimiter)
                    .Append(GetText(Localization.HelpSupportServer, ownerServer.Name, Credentials.OwnerServerInvite))
                    .Append("\n");
            }

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Credentials.Invite))
                links.Append(GetText(Localization.HelpInviteMe, Credentials.Invite)).Append("\n");

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Credentials.Website))
                links.Append(GetText(Localization.HelpWebsite, Credentials.Website)).Append("\n");

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Credentials.Patreon))
                links.Append(GetText(Localization.HelpDonate, Credentials.Patreon)).Append("\n");

            embed.AddField(GetText(Localization.HelpLinks), links.ToString());
            await ReplyAsync(embed);
        }
        
        [Command("help")]
        public async Task HelpAsync(string alias1, string? alias2 = null)
        {
            var module = GetModuleByAlias(alias1);
            var command = GetCommand(module, module is null ? alias1 : alias2);
            
            if (command is null)
            {
                await ReplyErrorAsync(Localization.HelpCommandNotFound, Context.Prefix);
                return;
            }

            var moduleAlias = module != null ? $"{module.Aliases[0]} " : string.Empty;
            var title = string.Join(" / ", command.Aliases.Select(a => $"{Context.Prefix}{moduleAlias}{a}"));
            if (string.IsNullOrEmpty(title))
            {
                title = $"{Context.Prefix}{moduleAlias}";
            } 
            
            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = title
            };

            var moduleName = command.Module.Name;
            if (command.Module.Parent != null)
                moduleName = $"{command.Module.Parent.Name} -> {moduleName}";

            var description = new StringBuilder(command.Description)
                .Append($"\n\n**{GetText(Localization.HelpModule)}**\n{moduleName}")
                .Replace("[prefix]", Context.Prefix)
                .Replace("[currency]", Credentials.Currency);

            embed.WithDescription(description.ToString());

            foreach (var attribute in command.Checks)
            {
                switch (attribute)
                {
                    case UserPermissionAttribute userPermissionAttribute:
                        var userPermissions = userPermissionAttribute.GuildPermissions
                            .GetValueOrDefault()
                            .ToString()
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Humanize(LetterCasing.Title))
                            .ToArray();
                        embed.AddField(GetText(Localization.HelpRequiresUserPermission), string.Join("\n", userPermissions), true);
                        break;
                    case BotPermissionAttribute botPermissionAttribute:
                        var botPermissions = botPermissionAttribute.GuildPermissions
                            .GetValueOrDefault()
                            .ToString()
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Humanize(LetterCasing.Title))
                            .ToArray();
                        embed.AddField(GetText(Localization.HelpRequiresBotPermission), string.Join("\n", botPermissions), true);
                        break;
                    case OwnerOnlyAttribute _:
                        embed.AddField(GetText(Localization.HelpRequiresOwner), GetText(Localization.CommonYes), true);
                        break;
                }
            }

            var commandCooldown = command.Cooldowns.FirstOrDefault();
            if (commandCooldown != null)
            {
                var locale = Localization.GetGuildLocale(Context.Guild!.Id);
                embed.AddField(GetText(Localization.CommonCooldown),
                    $"{GetText(Localization.CommonAmount)}: **{commandCooldown.Amount}**\n" +
                    $"{GetText(Localization.CommonPeriod)}: **{commandCooldown.Per.Humanize(culture: new CultureInfo(locale))}**\n" +
                    $"{GetText(Localization.CommonPer)}: **{GetText(Localization.CommonCooldownBucketType(commandCooldown.BucketType.Humanize(LetterCasing.LowerCase).Underscore()))}**",
                    true);
            }

            embed.AddField(GetText(Localization.CommonExample), string.Format(command.Remarks, Context.Prefix));
            embed.WithCurrentTimestamp();

            await ReplyAsync(embed);
        }
        
        [Command("modules")]
        public async Task ModulesAsync()
        {
            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.HelpModulesListTitle),
                Footer = new LocalEmbedFooterBuilder().WithText(GetText(Localization.HelpModulesListFooter, Context.Prefix))
            };

            var modules = _commandService.GetAllModules()
                .Where(m => m.Parent is null)
                .OrderBy(m => m.Name)
                .ToArray();
            
            foreach (var module in modules)
            {
                var fieldValue = "\u200B";
                if (module.Submodules.Count != 0)
                    fieldValue = string.Join("\n", module.Submodules.OrderBy(m => m.Name).Select(x => x.Name));

                embed.AddField(module.Name, fieldValue, true);
            }

            await ReplyAsync(embed);
        }
        
        private Module? GetModuleByAlias(string alias)
        {
            if (string.IsNullOrEmpty(alias))
                return null;

            return _commandService.GetAllModules().FirstOrDefault(x =>
                x.Aliases.Any(y => string.Equals(y, alias, StringComparison.InvariantCultureIgnoreCase)));
        }
        
        [Command("commands")]
        public async Task CommandsAsync([Remainder] string name)
        {
            var module = _commandService.GetAllModules().FirstOrDefault(m => m.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));
            if (module is null)
            {
                await ReplyErrorAsync(Localization.HelpModuleNotFound, Context.Prefix);
                return;
            }

            var modulesCommands = GetModuleCommands(module);
            var commandsAliases = GetCommandsAliases(modulesCommands, Context.Prefix);

            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(module.Parent != null ? Localization.HelpAllCommandsForSubmodule : Localization.HelpAllCommandsForModule, module.Name)
            }.AddField(module.Name, string.Join("\n", commandsAliases), true);

            foreach (var submodule in module.Submodules)
            {
                var submoduleCommands = GetModuleCommands(submodule);
                var submoduleCommandsAliases = GetCommandsAliases(submoduleCommands, Context.Prefix);

                embed.AddField(submodule.Name, string.Join("\n", submoduleCommandsAliases), true);
            }

            embed.WithFooter(GetText(Localization.HelpCommandInfo, Context.Prefix));
            embed.WithCurrentTimestamp();
            await ReplyAsync(embed);
        }
        
        [Command("allcommands"),
         Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task AllCommandsAsync()
        {
            var embed = new LocalEmbedBuilder
            {
                Color = RiasUtilities.ConfirmColor,
                Title = GetText(Localization.HelpAllCommands),
                Footer = new LocalEmbedFooterBuilder().WithText(GetText(Localization.HelpCommandInfo, Context.Prefix)),
                Timestamp = DateTimeOffset.UtcNow
            };

            var modules = _commandService.GetAllModules()
                .Where(m => m.Parent is null)
                .OrderBy(m => m.Name)
                .ToArray();
            
            foreach (var module in modules)
            {
                var moduleCommands = GetModuleCommands(module);
                var commandsAliases = GetCommandsAliases(moduleCommands, Context.Prefix);

                if (commandsAliases.Count != 0)
                    embed.AddField(module.Name, string.Join("\n", commandsAliases), true);

                foreach (var submodule in module.Submodules.OrderBy(m => m.Name))
                {
                    var submoduleCommands = GetModuleCommands(submodule);
                    var submoduleCommandsAliases = GetCommandsAliases(submoduleCommands, Context.Prefix);
                    if (submoduleCommandsAliases.Count != 0)
                        embed.AddField(submodule.Name, string.Join("\n", submoduleCommandsAliases), true);
                }

                if (embed.Fields.Count <= 20) continue;

                var received = await SendAllCommandsMessageAsync(embed.Build());
                if (!received) return;
                
                embed.Fields.Clear();
            }

            await SendAllCommandsMessageAsync(embed.Build());
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
        
        public IReadOnlyList<Command> GetModuleCommands(Module module) =>
            module.Commands.GroupBy(x => x.Name).Select(x => x.First()).OrderBy(x => x.Name).ToImmutableList();
        
        public IReadOnlyList<string> GetCommandsAliases(IEnumerable<Command> commands, string prefix)
            => commands.Select(x =>
            {
                var nextAliases = string.Join(", ", x.Aliases.Skip(1));
                if (!string.IsNullOrEmpty(nextAliases))
                    nextAliases = $"[{nextAliases}]";

                var moduleAlias = x.Module.Aliases.Count != 0 ? $"{x.Module.Aliases[0]} " : null;
                return $"{prefix}{moduleAlias}{x.Aliases.FirstOrDefault()} {nextAliases}";
            }).ToImmutableList();
        
        private async Task<bool> SendAllCommandsMessageAsync(LocalEmbed embed)
        {
            try
            {
                await Context.User.SendMessageAsync(embed: embed);
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