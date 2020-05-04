using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Help
{
    [Name("Help")]
    public class Help : RiasModule
    {
        private readonly DiscordShardedClient _client;
        private readonly CommandService _commandService;

        public Help(IServiceProvider services) : base(services)
        {
            _client = services.GetRequiredService<DiscordShardedClient>();
            _commandService = services.GetRequiredService<CommandService>();
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Author = new EmbedAuthorBuilder
                {
                    Name = GetText("Title", Context.Client.CurrentUser.Username, Rias.Version),
                    IconUrl = Context.Client.CurrentUser.GetRealAvatarUrl()
                },
                Footer = new EmbedFooterBuilder().WithText("© 2018-2020 Copyright: Koneko#0001")
            };
            
            embed.WithDescription(GetText("Info", Context.Prefix));

            var links = new StringBuilder();
            const string delimiter = " • ";

            if (!string.IsNullOrEmpty(Credentials.OwnerServerInvite))
            {
                var ownerServer = _client.GetGuild(Credentials.OwnerServerId);
                links.Append(delimiter)
                    .Append(GetText("SupportServer", ownerServer.Name, Credentials.OwnerServerInvite))
                    .Append("\n");
            }

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Credentials.Invite))
                links.Append(GetText("InviteMe", Credentials.Invite)).Append("\n");

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Credentials.Website))
                links.Append(GetText("Website", Credentials.Website)).Append("\n");

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Credentials.Patreon))
                links.Append(GetText("Donate", Credentials.Patreon)).Append("\n");

            embed.AddField(GetText("Links"), links.ToString());
            await ReplyAsync(embed);
        }

        [Command("help")]
        public async Task HelpAsync(string alias1, string? alias2 = null)
        {
            var module = GetModuleByAlias(alias1);
            var command = GetCommand(module, module is null ? alias1 : alias2);
            
            if (command is null)
            {
                await ReplyErrorAsync("CommandNotFound", Context.Prefix);
                return;
            }

            var moduleAlias = module != null ? $"{module.Aliases[0]} " : string.Empty;
            var title = string.Join(" / ", command.Aliases.Select(a => $"{Context.Prefix}{moduleAlias}{a}"));
            if (string.IsNullOrEmpty(title))
            {
                title = $"{Context.Prefix}{moduleAlias}";
            } 
            
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = title
            };

            var moduleName = command.Module.Name;
            if (command.Module.Parent != null)
                moduleName = $"{command.Module.Parent.Name} -> {moduleName}";

            var description = new StringBuilder(command.Description)
                .Append($"\n\n**{GetText("Module")}**\n{moduleName}")
                .Replace("[prefix]", Context.Prefix)
                .Replace("[currency]", Credentials.Currency);

            embed.WithDescription(description.ToString());

            foreach (var attribute in command.Checks)
            {
                switch (attribute)
                {
                    case UserPermissionAttribute userPermissionAttribute:
                        var userPermissions = userPermissionAttribute.GuildPermission
                            .GetValueOrDefault()
                            .ToString()
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Humanize(LetterCasing.Title))
                            .ToArray();
                        embed.AddField(GetText("RequiresUserPermission"), string.Join("\n", userPermissions), true);
                        break;
                    case BotPermissionAttribute botPermissionAttribute:
                        var botPermissions = botPermissionAttribute.GuildPermission
                            .GetValueOrDefault()
                            .ToString()
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Humanize(LetterCasing.Title))
                            .ToArray();
                        embed.AddField(GetText("RequiresBotPermission"), string.Join("\n", botPermissions), true);
                        break;
                    case OwnerOnlyAttribute _:
                        embed.AddField(GetText("RequiresOwner"), GetText("#Common_Yes"), true);
                        break;
                }
            }

            var commandCooldown = command.Cooldowns.FirstOrDefault();
            if (commandCooldown != null)
            {
                var culture = Resources.GetGuildCulture(Context.Guild!.Id);
                embed.AddField(GetText("#Common_Cooldown"),$"{GetText("#Common_Amount")}: **{commandCooldown.Amount}**\n" +
                                                            $"{GetText("#Common_Period")}: **{commandCooldown.Per.Humanize(culture: culture)}**\n" +
                                                            $"{GetText("#Common_Per")}: **{GetText($"#Common_{commandCooldown.BucketType}")}**", true);
            }

            embed.AddField(GetText("#Common_Example"), string.Format(command.Remarks, Context.Prefix), true);
            embed.WithCurrentTimestamp();

            await ReplyAsync(embed);
        }

        [Command("modules")]
        public async Task ModulesAsync()
        {
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = GetText("ModulesListTitle"),
                Footer = new EmbedFooterBuilder
                {
                    Text = GetText("ModulesListFooter", Context.Prefix)
                }
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

        [Command("commands")]
        public async Task CommandsAsync([Remainder] string name)
        {
            var module = _commandService.GetAllModules().FirstOrDefault(m => m.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));
            if (module is null)
            {
                await ReplyErrorAsync("ModuleNotFound", Context.Prefix);
                return;
            }

            var modulesCommands = GetModuleCommands(module);
            var commandsAliases = GetCommandsAliases(modulesCommands, Context.Prefix);

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = GetText(module.Parent != null ? "AllCommandsForSubmodule" : "AllCommandsForModule", module.Name)
            }.AddField(module.Name, string.Join("\n", commandsAliases), true);

            foreach (var submodule in module.Submodules)
            {
                var submoduleCommands = GetModuleCommands(submodule);
                var submoduleCommandsAliases = GetCommandsAliases(submoduleCommands, Context.Prefix);

                embed.AddField(submodule.Name, string.Join("\n", submoduleCommandsAliases), true);
            }

            embed.WithFooter(GetText("CommandInfo", Context.Prefix));
            embed.WithCurrentTimestamp();
            await ReplyAsync(embed);
        }

        [Command("allcommands"), Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task AllCommandsAsync()
        {
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = GetText("AllCommands"),
                Footer = new EmbedFooterBuilder
                {
                    Text = GetText("CommandInfo", Context.Prefix)
                },
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

                embed.Fields = new List<EmbedFieldBuilder>();
            }

            await SendAllCommandsMessageAsync(embed.Build());
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
        
        private async Task<bool> SendAllCommandsMessageAsync(Embed embed)
        {
            try
            {
                await Context.User.SendMessageAsync(embed: embed);
            }
            catch
            {
                await ReplyErrorAsync("CommandsNotSent");
                return false;
            }

            return true;
        }
    }
}