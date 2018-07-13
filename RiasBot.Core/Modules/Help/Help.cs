using RiasBot.Commons.Attributes;
using RiasBot.Services;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using RiasBot.Extensions;

namespace RiasBot.Modules.Help
{
    public partial class Help : RiasModule
    {
        public readonly CommandHandler _ch;
        public readonly CommandService _service;
        public readonly IBotCredentials _creds;

        public Help(CommandHandler ch, CommandService service, IBotCredentials creds)
        {
            _ch = ch;
            _service = service;
            _creds = creds;
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Priority(1)]
        public async Task H()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(RiasBot.GoodColor);
            embed.WithDescription(_creds.HelpDM.Replace("%invite%", RiasBot.Invite).Replace("%creatorServer%", RiasBot.CreatorServer));

            try
            {
                await Context.Message.Author.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
            }
            catch
            {
                await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't send you the help DM. Please verify if you disabled receiving DM from this server.");
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Priority(0)]
        public async Task H(string command)
        {
            command = command?.Trim();
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await Context.Channel.SendErrorEmbed($"I couldn't find that command. For help type `{_ch.Prefix}h` to send you a DM").ConfigureAwait(false);
                return;
            }

            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);

            bool single = true;

            foreach (var match in result.Commands)
            {
                if (single)
                {
                    single = false;

                    var cmd = match.Command;
                    string summary = cmd.Summary;

                    int index = 0;
                    string[] aliases = new String[cmd.Aliases.Count];

                    foreach (string alias in cmd.Aliases)
                    {
                        aliases[index] = _ch.Prefix + alias;
                        index++;
                    }

                    string require = "";
                    if (GetCommandRequirements(cmd) != require)
                    {
                        require = $" Requires {GetCommandRequirements(cmd)}";
                    }

                    embed.WithTitle(string.Join("/ ", aliases));

                    //Replacements
                    //if (summary.Contains("[prefix]"))
                        summary = summary.Replace("[prefix]", _ch.Prefix);
                    //if (summary.Contains("[currency]"))
                        summary = summary.Replace("[currency]", RiasBot.Currency);

                    embed.WithDescription(summary + require);
                    embed.AddField("Example", cmd.Remarks.Replace("[prefix]", _ch.Prefix));
                }
            }
            embed.WithCurrentTimestamp();
            await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Modules()
        {
            var embed = new EmbedBuilder();
            embed.WithColor(RiasBot.GoodColor);
            embed.WithTitle("List of all modules and submodules");

            var modules = _service.Modules.GroupBy(m => m.GetModule()).Select(m => m.Key).OrderBy(m => m.Name);

            string[] modulesDescription = new string[modules.Count()];
            int index = 0;
            foreach (var module in modules)
            {
                if (module.Name == "Music") //don't show the Music module, is for tests
                    continue;
                modulesDescription[index] += Format.Bold($"•{module.Name}") + "\n";
                var submodules = module.Submodules;
                foreach (var submodule in submodules)
                {
                    if (submodule.Name == "CommandsCommands")
                        modulesDescription[index] += "\t~>" + "Commands" + "\n";
                    else
                        modulesDescription[index] += "\t~>" + submodule.Name.Replace("Commands", "") + "\n";
                }
            }
            embed.WithDescription(String.Join("\n\n", modulesDescription));
            embed.WithFooter($"To see all commands for a module or submodule, type {_ch.Prefix + "cmds <module>"} or {_ch.Prefix + "cmds <submodule>"}");
            embed.WithCurrentTimestamp();
            await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        public async Task Commands([Remainder]string module)
        {
            module = module?.Trim().ToUpperInvariant();
            var getModule = _service.Modules.Where(m => m.GetModule().Name.ToUpperInvariant().StartsWith(module)).FirstOrDefault();

            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            int index = 0;

            if (getModule != null)
            {
                var moduleCommands = getModule.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z.Aliases.First());

                var transformed = moduleCommands.Select(x =>
                {
                    string nextAlias = null;
                    if (x.Aliases.Skip(1).FirstOrDefault() != null)
                        nextAlias = $"[{_ch.Prefix}{x.Aliases.Skip(1).FirstOrDefault()}]";

                    return $"{_ch.Prefix + x.Aliases.First()} {nextAlias}";
                });
                embed.WithTitle($"All commands for module {getModule.Name}");
                embed.AddField(getModule.Name, String.Join("\n", transformed), true);

                foreach (var command in getModule.Submodules.OrderBy(sb => sb.Name))
                {
                    var submoduleCommands = command.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z.Aliases.First());
                    var transformedSb = submoduleCommands.Select(x =>
                    {
                        string nextAlias = null;
                        if (x.Aliases.Skip(1).FirstOrDefault() != null)
                            nextAlias = $"[{_ch.Prefix}{x.Aliases.Skip(1).FirstOrDefault()}]";

                        return $"{_ch.Prefix + x.Aliases.First()} {nextAlias}";
                    });
                    if (command.Name == "CommandsCommands") // I just want the Commands submodule
                        embed.AddField("Commands", String.Join("\n", transformedSb), true);
                    else
                        embed.AddField(command.Name.Replace("Commands", ""), String.Join("\n", transformedSb), true);
                    index++;
                }
                embed.WithFooter($"For a specific command info type {_ch.Prefix + "h <command>"}");
                embed.WithCurrentTimestamp();
                await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
            }
            else
            {
                try
                {
                    ModuleInfo submodule = null;
                    foreach (var mod in _service.Modules)
                    {
                        bool sbFound = false;
                        foreach (var submod in mod.Submodules)
                        {
                            if (submod.Name.ToUpperInvariant().StartsWith(module))
                            {
                                submodule = submod;
                                sbFound = true;
                                break;
                            }

                        }
                        if (sbFound)
                            break;
                    }
                    var submoduleCommands = submodule.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z.Aliases.First());

                    var transformed = submoduleCommands.Select(x =>
                    {
                        string nextAlias = null;
                        if (x.Aliases.Skip(1).FirstOrDefault() != null)
                            nextAlias = $"[{_ch.Prefix}{x.Aliases.Skip(1).FirstOrDefault()}]";

                        return $"{_ch.Prefix + x.Aliases.First()} {nextAlias}";
                    });
                    embed.WithTitle($"All commands for submodule {submodule.Name.Replace("Commands", "")}");
                    embed.WithDescription(String.Join("\n", transformed));
                    embed.WithFooter($"For a specific command info type {_ch.Prefix + "h <command>"}");
                    embed.WithCurrentTimestamp();
                    await Context.Channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed($"{Context.User.Mention} I couldn't find the module or submodule. Type {Format.Code(_ch.Prefix + "modules")} to see all modules and submodules.").ConfigureAwait(false);
                }
            }
        }

        [RiasCommand][@Alias]
        [Description][@Remarks]
        [Ratelimit(1, 5, Measure.Seconds, applyPerGuild: true)]
        public async Task AllCommands()
        {
            var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
            int index = 0;

            foreach (var getModule in _service.Modules.GroupBy(m => m.GetModule()).Select(m => m.Key).OrderBy(m => m.Name))
            {
                var moduleCommands = getModule.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z.Aliases.First());

                var transformed = moduleCommands.Select(x =>
                {
                    string nextAlias = null;
                    if (x.Aliases.Skip(1).FirstOrDefault() != null)
                        nextAlias = $"[{_ch.Prefix}{x.Aliases.Skip(1).FirstOrDefault()}]";

                    return $"{_ch.Prefix + x.Aliases.First()} {nextAlias}";
                });
                embed.WithTitle($"All commands");
                embed.AddField(getModule.Name, String.Join("\n", transformed), true);

                foreach (var command in getModule.Submodules.OrderBy(sb => sb.Name))
                {
                    var submoduleCommands = command.Commands.GroupBy(c => c.Aliases.First()).Select(y => y.FirstOrDefault()).OrderBy(z => z.Aliases.First());
                    var transformedSb = submoduleCommands.Select(x =>
                    {
                        string nextAlias = null;
                        if (x.Aliases.Skip(1).FirstOrDefault() != null)
                            nextAlias = $"[{_ch.Prefix}{x.Aliases.Skip(1).FirstOrDefault()}]";

                        return $"{_ch.Prefix + x.Aliases.First()} {nextAlias}";
                    });
                    embed.AddField(command.Name.Replace("Commands", ""), String.Join("\n", transformedSb), true);
                    index++;
                }
                if (embed.Fields.Count > 20)
                {
                    embed.WithFooter($"For a specific command info type {_ch.Prefix + "h <command>"}");
                    embed.WithCurrentTimestamp();
                    await Context.User.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                }
            }
            embed.WithFooter($"For a specific command info type {_ch.Prefix + "h <command>"}");
            embed.WithCurrentTimestamp();
            await Context.User.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
        }

        public string GetCommandRequirements(CommandInfo cmd) =>
            string.Join(", ", cmd.Preconditions
                  .Where(ca => ca is RequireOwnerAttribute || ca is RequireUserPermissionAttribute)
                  .Select(ca =>
                  {
                      if (ca is RequireOwnerAttribute)
                          return Format.Bold("Bot Owner");

                      var cau = (RequireUserPermissionAttribute)ca;
                      if (cau.GuildPermission != null)
                          return Format.Bold(cau.GuildPermission.ToString());

                      return Format.Bold(cau.ChannelPermission.ToString());
                  }));
    }
}
