using RiasBot.Commons.Attributes;
using RiasBot.Services;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RiasBot.Extensions;

namespace RiasBot.Modules.Help
{
    public partial class Help : RiasModule
    {
        private readonly CommandHandler _ch;
        private readonly CommandService _service;
        private readonly IBotCredentials _creds;

        public Help(CommandHandler ch, CommandService service, IBotCredentials creds)
        {
            _ch = ch;
            _service = service;
            _creds = creds;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Priority(1)]
        public async Task HelpAsync()
        {
            //Ignore the raw string coding
            
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            embed.WithAuthor($"{Context.Client.CurrentUser.Username} Bot v{RiasBot.Version} help page", Context.Client.CurrentUser.GetRealAvatarUrl());
            embed.WithDescription("I'm based on modules and submodules. Each module has submodules and each submodule has commands.\n\n" +
                                  $"Type `{_ch.GetPrefix(Context.Guild)}modules` to get a list with all modules and submodules.\n\n" +
                                  $"Type `{_ch.GetPrefix(Context.Guild)}commands <moduleName>` or `{_ch.GetPrefix(Context.Guild)}commands <submoduleName>` to get a list with all commands from the module or submodule.\n" +
                                  $"Example: `{_ch.GetPrefix(Context.Guild)}commands Administration`, `{_ch.GetPrefix(Context.Guild)}commands Server`\n\n" +
                                  $"Type `{_ch.GetPrefix(Context.Guild)}allcommands` to get a list with all commands.");
            embed.AddField("Links", $"[Invite me]({RiasBot.Invite}) • [Support server]({RiasBot.CreatorServer})\n" +
                                    $"[Website]({RiasBot.Website}) • [Support me]({RiasBot.Patreon})\n" +
                                    $"[Vote on DBL](https://discordbots.org/bot/{Context.Client.CurrentUser.Id})");
            embed.WithFooter("© 2018-2019 Copyright: Koneko#0001");
            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Priority(0)]
        public async Task HelpAsync(string name)
        {
            name = name?.Trim();
            var command = _service.Commands.FirstOrDefault(x => x.Aliases.Any(y => y.Equals(name, StringComparison.InvariantCultureIgnoreCase)));

            if (command is null)
            {
                await Context.Channel.SendErrorMessageAsync($"I couldn't find that command. For help type `{_ch.GetPrefix(Context.Guild)}help`").ConfigureAwait(false);
                return;
            }

            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            
            var summary = command.Summary;

            var index = 0;
            var aliases = new string[command.Aliases.Count];

            foreach (var alias in command.Aliases)
            {
                aliases[index] = _ch.GetPrefix(Context.Guild) + alias;
                index++;
            }

            var requires = "";
            if (GetCommandRequirements(command) != requires)
            {
                requires = $"{GetCommandRequirements(command)}";
            }

            embed.WithTitle(string.Join("/ ", aliases));

            summary = summary.Replace("[prefix]", _ch.GetPrefix(Context.Guild));
            summary = summary.Replace("[currency]", RiasBot.Currency);

            embed.WithDescription(summary);

            var module = command.Module.IsSubmodule ? $"{command.Module.Parent.Name} -> {command.Module.Name}" : $"{command.Module.Name}";
            embed.AddField("Module", module, true);
            
            if (!string.IsNullOrEmpty(requires))
                embed.AddField("Requires", requires, true);
            
            embed.AddField("Example", command.Remarks.Replace("[prefix]", _ch.GetPrefix(Context.Guild)));
            embed.WithCurrentTimestamp();
            
            await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task ModulesAsync()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(RiasBot.GoodColor);
            embed.WithTitle("List of all modules and submodules");

            var modules = _service.Modules.GroupBy(m => m.GetModule()).Select(m => m.Key).OrderBy(m => m.Name).ToList();

            var modulesDescription = new List<string>();
            foreach (var module in modules)
            {
                modulesDescription.Add(Format.Bold($"•{module.Name}"));
                var submodules = module.Submodules;
                foreach (var submodule in submodules)
                {
                    if (submodule.Name == "CommandsCommands")
                        modulesDescription.Add("\t~>" + "Commands");
                    else
                        modulesDescription.Add("\t~>" + submodule.Name.Replace("Commands", ""));
                }
            }
            embed.WithDescription(string.Join("\n", modulesDescription));
            embed.WithFooter($"To get all commands for a module or submodule, type {_ch.GetPrefix(Context.Guild)}cmds <module> or {_ch.GetPrefix(Context.Guild)}cmds <submodule>");
            embed.WithCurrentTimestamp();
            await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task CommandsAsync([Remainder]string name)
        {
            var module = _service.Modules.FirstOrDefault(m => m.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));

            if (module is null)
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't find the module or submodule. Type {Format.Code(_ch.GetPrefix(Context.Guild) + "modules")} to see all modules and submodules.").ConfigureAwait(false);
                return;
            }
            
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            if (!module.IsSubmodule)
            {
                var moduleCommands = module.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z?.Aliases.First());

                var commands = moduleCommands.Select(x =>
                {
                    string nextAlias = null;
                    if (x?.Aliases.Skip(1).FirstOrDefault() != null)
                        nextAlias = $"[{_ch.GetPrefix(Context.Guild)}{x.Aliases.Skip(1).FirstOrDefault()}]";

                    return $"{_ch.GetPrefix(Context.Guild) + x?.Aliases.First()} {nextAlias}";
                });
                embed.WithTitle($"All commands for module {module.Name}");
                embed.AddField(module.Name, string.Join("\n", commands), true);

                foreach (var command in module.Submodules.OrderBy(sb => sb.Name))
                {
                    var submoduleCommands = command.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z?.Aliases.First());
                    var commandsSb = submoduleCommands.Select(x =>
                    {
                        string nextAlias = null;
                        if (x?.Aliases.Skip(1).FirstOrDefault() != null)
                            nextAlias = $"[{_ch.GetPrefix(Context.Guild)}{x.Aliases.Skip(1).FirstOrDefault()}]";

                        return $"{_ch.GetPrefix(Context.Guild) + x?.Aliases.First()} {nextAlias}";
                    });
                    embed.AddField(command.Name.Equals("CommandsCommands") ? "Commands" : command.Name.Replace("Commands", ""), string.Join("\n", commandsSb), true);
                }
                embed.WithFooter($"For a specific command info type {_ch.GetPrefix(Context.Guild) + "h <command>"}");
                embed.WithCurrentTimestamp();
                await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                var submoduleCommands = module.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z?.Aliases.First());

                var transformed = submoduleCommands.Select(x =>
                {
                    string nextAlias = null;
                    if (x?.Aliases.Skip(1).FirstOrDefault() != null)
                        nextAlias = $"[{_ch.GetPrefix(Context.Guild)}{x.Aliases.Skip(1).FirstOrDefault()}]";

                    return $"{_ch.GetPrefix(Context.Guild) + x?.Aliases.First()} {nextAlias}";
                });
                embed.WithTitle($"All commands for submodule {module.Name.Replace("Commands", "")}");
                embed.WithDescription(string.Join("\n", transformed));
                embed.WithFooter($"For a specific command info type {_ch.GetPrefix(Context.Guild) + "h <command>"}");
                embed.WithCurrentTimestamp();
                await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Ratelimit(1, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task AllCommandsAsync()
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);

            foreach (var module in _service.Modules.GroupBy(m => m.GetModule()).Select(m => m.Key).OrderBy(m => m.Name))
            {
                var moduleCommands = module.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z?.Aliases.First());

                var commands = moduleCommands.Select(x =>
                {
                    string nextAlias = null;
                    if (x?.Aliases.Skip(1).FirstOrDefault() != null)
                        nextAlias = $"[{_ch.GetPrefix(Context.Guild)}{x.Aliases.Skip(1).FirstOrDefault()}]";

                    return $"{_ch.GetPrefix(Context.Guild) + x?.Aliases.First()} {nextAlias}";
                }).ToList();
                embed.WithTitle($"All commands");
                if (commands.Any())
                    embed.AddField(module.Name, string.Join("\n", commands), true);

                foreach (var submodule in module.Submodules.OrderBy(sb => sb.Name))
                {
                    var submoduleCommands = submodule.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z?.Aliases.First());
                    var commandsSb = submoduleCommands.Select(x =>
                    {
                        string nextAlias = null;
                        if (x?.Aliases.Skip(1).FirstOrDefault() != null)
                            nextAlias = $"[{_ch.GetPrefix(Context.Guild)}{x.Aliases.Skip(1).FirstOrDefault()}]";

                        return $"{_ch.GetPrefix(Context.Guild) + x?.Aliases.First()} {nextAlias}";
                    });
                    embed.AddField(submodule.Name == "CommandsCommands" ? "Commands" : submodule.Name.Replace("Commands", ""), string.Join("\n", commandsSb), true);
                }
                if (embed.Fields.Count > 20)
                {
                    embed.WithFooter($"For a specific command info type {_ch.GetPrefix(Context.Guild) + "h <command>"}");
                    embed.WithCurrentTimestamp();
                    try
                    {
                        await Context.User.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                    catch
                    {
                        await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't send you the help DM. Please verify if you disabled receiving DM from this server.");
                        return;
                    }
                    embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                }
            }
            embed.WithFooter($"For a specific command info type {_ch.GetPrefix(Context.Guild) + "h <command>"}");
            embed.WithCurrentTimestamp();
            try
            {
                await Context.Message.AddReactionAsync(new Emoji("✅")).ConfigureAwait(false);
                await Context.User.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't send you the help DM. Please verify if you disabled receiving DM from this server.");
            }
        }

        private string GetCommandRequirements(CommandInfo cmd)
        {

            return string.Join(", ", cmd.Preconditions.Where(precondition => precondition is RequireOwnerAttribute || precondition is RequireUserPermissionAttribute)
                .Select(precondition =>
                {
                    if (precondition is RequireOwnerAttribute)
                        return "Bot Owner";

                    var preconditionAtribute = (RequireUserPermissionAttribute) precondition;
                    if (preconditionAtribute.GuildPermission != null)
                        return preconditionAtribute.GuildPermission.ToString();

                    return preconditionAtribute.ChannelPermission.ToString();
                }));
        }
    }
}
