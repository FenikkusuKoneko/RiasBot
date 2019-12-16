using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Help
{
    [Name("Help")]
    public class Help : RiasModule<HelpService>
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
                Footer = new EmbedFooterBuilder().WithText("© 2018-2019 Copyright: Koneko#0001")
            };

            var prefix = GetPrefix();

            embed.WithDescription(GetText("Info", prefix));

            var links = new StringBuilder();
            const string delimiter = " • ";

            if (!string.IsNullOrEmpty(Creds.OwnerServerInvite))
            {
                var ownerServer = _client.GetGuild(Creds.OwnerServerId);
                links.Append(delimiter)
                    .Append(GetText("SupportServer", ownerServer.Name, Creds.OwnerServerInvite))
                    .Append("\n");
            }

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Creds.Invite))
                links.Append(GetText("InviteMe", Creds.Invite)).Append("\n");

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Creds.Website))
                links.Append(GetText("Website", Creds.Website)).Append("\n");

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(Creds.Patreon))
                links.Append(GetText("Donate", Creds.Patreon)).Append("\n");

            embed.AddField(GetText("Links"), links.ToString());
            await ReplyAsync(embed);
        }

        [Command("help")]
        public async Task HelpAsync(string alias1, string? alias2 = null)
        {
            var module = Service.GetModuleByAlias(alias1);
            var command = Service.GetCommand(module, module is null ? alias1 : alias2);
            
            var prefix = GetPrefix();
            if (command is null)
            {
                await ReplyErrorAsync("CommandNotFound", prefix);
                return;
            }

            var moduleAlias = module != null ? $"{module.Aliases[0]} " : string.Empty;
            var title = string.Join(" / ", command.Aliases.Select(a => $"{prefix}{moduleAlias}{a}"));
            if (string.IsNullOrEmpty(title))
            {
                title = $"{prefix}{moduleAlias}";
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
                .Replace("[prefix]", prefix)
                .Replace("[currency]", Creds.Currency);

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

            embed.AddField(GetText("#Common_Example"), string.Format(command.Remarks, prefix), true);
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
                    Text = GetText("ModulesListFooter", GetPrefix())
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
            var prefix = GetPrefix();
            var module = _commandService.GetAllModules().FirstOrDefault(m => m.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));
            if (module is null)
            {
                await ReplyErrorAsync("ModuleNotFound", prefix);
                return;
            }

            var modulesCommands = Service.GetModuleCommands(module);
            var commandsAliases = Service.GetCommandsAliases(modulesCommands, prefix);

            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = GetText(module.Parent != null ? "AllCommandsForSubmodule" : "AllCommandsForModule", module.Name)
            }.AddField(module.Name, string.Join("\n", commandsAliases), true);

            foreach (var submodule in module.Submodules)
            {
                var submoduleCommands = Service.GetModuleCommands(submodule);
                var submoduleCommandsAliases = Service.GetCommandsAliases(submoduleCommands, prefix);

                embed.AddField(submodule.Name, string.Join("\n", submoduleCommandsAliases), true);
            }

            embed.WithFooter(GetText("CommandInfo", prefix));
            embed.WithCurrentTimestamp();
            await ReplyAsync(embed);
        }

        [Command("allcommands"), Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.User)]
        public async Task AllCommandsAsync()
        {
            var prefix = GetPrefix();
            var embed = new EmbedBuilder
            {
                Color = RiasUtils.ConfirmColor,
                Title = GetText("AllCommands"),
                Footer = new EmbedFooterBuilder
                {
                    Text = GetText("CommandInfo", prefix)
                },
                Timestamp = DateTimeOffset.UtcNow
            };

            var modules = _commandService.GetAllModules()
                .Where(m => m.Parent is null)
                .OrderBy(m => m.Name)
                .ToArray();
            
            foreach (var module in modules)
            {
                var moduleCommands = Service.GetModuleCommands(module);
                var commandsAliases = Service.GetCommandsAliases(moduleCommands, prefix);

                if (commandsAliases.Count != 0)
                    embed.AddField(module.Name, string.Join("\n", commandsAliases), true);

                foreach (var submodule in module.Submodules.OrderBy(m => m.Name))
                {
                    var submoduleCommands = Service.GetModuleCommands(submodule);
                    var submoduleCommandsAliases = Service.GetCommandsAliases(submoduleCommands, prefix);
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