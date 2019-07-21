using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using Qmmands;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using RiasBot.Extensions;

namespace Rias.Core.Modules.Help
{
    [Name("Help")]
    public class Help : RiasModule
    {
        public CommandService CommandService { get; set; }

        [Command("help")]
        public async Task HelpAsync()
        {
            var embed = new EmbedBuilder()
                .WithColor(RiasUtils.ConfirmColor)
                .WithAuthor(GetText("title", Context.Client.CurrentUser.Username, Rias.Version), Context.Client.CurrentUser.GetRealAvatarUrl());

            var prefix = GetPrefix();
            if (string.IsNullOrWhiteSpace(prefix))
                prefix = Creds.Prefix;

            embed.WithDescription(GetText("info_1") +
                                  GetText("info_2", prefix) +
                                  GetText("info_3", prefix) +
                                  GetText("info_4", prefix) +
                                  GetText("info_5", prefix));

            var links = new StringBuilder();
            const string delimiter = " • ";

            if (!string.IsNullOrWhiteSpace(Creds.OwnerServerInvite))
            {
                var ownerServer = Context.Client.GetGuild(Creds.OwnerServerId);
                links.Append(GetText("support_server", ownerServer.Name, Creds.OwnerServerInvite));
            }

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrWhiteSpace(Creds.Invite))
                links.Append(GetText("invite_me", Creds.Invite));

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrWhiteSpace(Creds.Website))
                links.Append(GetText("website", Creds.Website));

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrWhiteSpace(Creds.Patreon))
                links.Append(GetText("donate", Creds.Patreon));

            embed.AddField(GetText("links"), links.ToString());
            embed.WithFooter("© 2018-2019 Copyright: Koneko#0001");
            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("help")]
        public async Task HelpAsync(string name)
        {
            name = name.Trim();
            var command = CommandService.GetAllCommands().FirstOrDefault(x => x.Aliases.Any(y => y.Equals(name, StringComparison.InvariantCultureIgnoreCase)));

            var prefix = GetPrefix();
            if (command is null)
            {
                await ReplyErrorAsync("command_not_found", prefix);
                return;
            }

            var embed = new EmbedBuilder()
                .WithColor(RiasUtils.ConfirmColor)
                .WithTitle(string.Join("/ ", command.Aliases.Select(a => prefix + a)));

            var description = command.Description;
            description = description.Replace("[prefix]", prefix);
            description = description.Replace("[currency]", Creds.Currency);
            embed.WithDescription(description);

            var moduleName = command.Module.Name;
            if (command.Module.Parent != null)
                moduleName = $"{command.Module.Parent.Name} -> {moduleName}";
            embed.AddField(GetText("module"), moduleName, true);

            var commandCooldown = command.Cooldowns.FirstOrDefault();
            if (commandCooldown != null)
            {
                var culture = CultureInfo.GetCultureInfo(Translations.GetGuildLocale(Context.Guild.Id));
                embed.AddField(GetText("#common_cooldown"), $"{GetText("#common_amount")}: **{commandCooldown.Amount}**\n" +
                                                            $"{GetText("#common_period")}: **{commandCooldown.Per.Humanize(culture: culture)}**\n" +
                                                            $"{GetText("#common_per")}: **{GetText($"#common_{commandCooldown.BucketType.ToString().ToLower()}").Titleize()}**", true);
            }

            embed.AddField(GetText("example"), string.Format(command.Remarks, prefix), true);
            embed.WithCurrentTimestamp();

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("modules")]
        public async Task ModulesAsync()
        {
            var modules = CommandService.GetAllModules()
                .GroupBy(m => m.Parent ?? m)
                .Select(m => m.Key)
                .OrderBy(m => m.Name)
                .ToArray();

            var modulesString = new StringBuilder();
            foreach (var module in modules)
            {
                modulesString.Append(module.Name).Append(" -> ");
                var length = module.Name.Length + 4;

                var firstSubmoduleAdded = false;
                foreach (var submodule in module.Submodules)
                {
                    if (!firstSubmoduleAdded)
                    {
                        modulesString.Append(submodule.Name);
                        firstSubmoduleAdded = true;
                    }
                    else
                        modulesString.Append(' ', length).Append(submodule.Name).Append("\n");
                }

                modulesString.Append("\n");
            }

            var embed = new EmbedBuilder()
                .WithColor(RiasUtils.ConfirmColor)
                .WithTitle(GetText("modules_list"))
                .WithDescription(modulesString.ToString())
                .WithFooter(GetText("modules_info"));

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("commands")]
        public async Task CommandsAsync([Remainder] string name)
        {
            var prefix = GetPrefix();
            var module = CommandService.GetAllModules().FirstOrDefault(m => m.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));
            if (module is null)
            {
                await ReplyErrorAsync("module_not_found", prefix);
                return;
            }

            var moduleCommands = GetModuleCommands(module);
            var commandsAliases = GetCommandsAliases(moduleCommands, prefix);
            var isSubmodule = module.Parent != null;

            var embed = new EmbedBuilder()
                .WithColor(RiasUtils.ConfirmColor)
                .WithTitle(GetText(isSubmodule ? "all_commands_for_submodule" : "all_commands_for_module", module.Name))
                .AddField(module.Name, string.Join("\n", commandsAliases), true);
            
            if (!isSubmodule)
            {
                foreach (var submodule in module.Submodules)
                {
                    var submoduleCommands = GetModuleCommands(submodule);
                    var submoduleCommandsAliases = GetCommandsAliases(submoduleCommands, prefix);

                    embed.AddField(submodule.Name, string.Join("\n", submoduleCommandsAliases), true);
                }
            }
            
            embed.WithFooter(GetText("command_info", prefix));
            embed.WithCurrentTimestamp();
            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [Command("allcommands"), Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task AllCommandsAsync()
        {
            var modules = CommandService.GetAllModules().Where(m => m.Parent is null).OrderBy(mo => mo.Name);

            var prefix = GetPrefix();
            var embed = new EmbedBuilder()
                .WithColor(RiasUtils.ConfirmColor)
                .WithTitle(GetText("all_commands"));
            
            foreach (var module in modules)
            {
                var moduleCommands = GetModuleCommands(module);
                var commandsAliases = GetCommandsAliases(moduleCommands, prefix);
                
                if (commandsAliases.Any())
                    embed.AddField(module.Name, string.Join("\n", commandsAliases), true);

                foreach (var submodule in module.Submodules)
                {
                    var submoduleCommands = GetModuleCommands(submodule);
                    var submoduleCommandsAliases = GetCommandsAliases(submoduleCommands, prefix);
                    if (submoduleCommandsAliases.Any())
                        embed.AddField(module.Name, string.Join("\n", submoduleCommandsAliases), true);
                }
                
                if (embed.Fields.Count <= 20) continue;
                
                var received = await SendAllCommandsMessageAsync(embed, prefix);
                if (!received) return;

                embed = new EmbedBuilder()
                    .WithColor(RiasUtils.ConfirmColor)
                    .WithTitle(GetText("all_commands"));
            }
            
            await SendAllCommandsMessageAsync(embed, prefix);
        }

        /// <summary>
        /// Gets a list with all commands from a module or submodule sorted by name without without duplicates.
        /// </summary>
        private IReadOnlyList<Command> GetModuleCommands(Module module) =>
            module.Commands.GroupBy(c => c.Name).Select(x => x.First()).OrderBy(n => n.Name).ToImmutableList();

        /// <summary>
        /// Gets a list with all aliases from a collection with commands including the <see cref="prefix"/>: [prefix]name [[prefix]alias1, [prefix]alias2...]
        /// </summary>
        private IReadOnlyList<string> GetCommandsAliases(IEnumerable<Command> commands, string prefix)
            => commands.Select(x =>
            {
                var nextAlias = string.Join(", ", x.Aliases.Skip(1).Select(a => prefix + a));
                if (!string.IsNullOrWhiteSpace(nextAlias))
                    nextAlias = $"[{nextAlias}]";

                return $"{prefix + x.Aliases.First()} {nextAlias}";
            }).ToImmutableList();

        private async Task<bool> SendAllCommandsMessageAsync(EmbedBuilder embed, string prefix)
        {
            embed.WithFooter(GetText("command_info", prefix));
            embed.WithCurrentTimestamp();
            try
            {
                await Context.User.SendMessageAsync(embed: embed.Build());
            }
            catch
            {
                await ReplyErrorAsync("commands_not_sent");
                return false;
            }

            return true;
        }
    }
}