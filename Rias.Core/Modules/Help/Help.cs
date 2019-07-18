using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Qmmands;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

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

            if (!string.IsNullOrWhiteSpace(Creds.Invite))
                links.Append(GetText("invite_me", Creds.Invite));

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrWhiteSpace(Creds.OwnerServerInvite))
            {
                var ownerServer = Context.Client.GetGuild(Creds.OwnerServerId);
                links.Append(GetText("support_server", ownerServer.Name, Creds.OwnerServerInvite));
            }

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
                .WithTitle(string.Join("/ ", command.Aliases.Select(x => prefix)));
            
            var description = command.Description;
            description = description.Replace("[prefix]", prefix);
            description = description.Replace("[currency]", Creds.Currency);
            embed.WithDescription(description);

            var moduleName = command.Module.Name;
            if (command.Module.Parent != null)
                moduleName = $"{command.Module.Parent.Name} -> {moduleName}";
            embed.AddField(GetText("module"), moduleName, true);

            embed.AddField(GetText("example"), string.Format(command.Remarks, prefix), true);
            embed.WithCurrentTimestamp();

            await Context.Channel.SendMessageAsync(embed: embed.Build());
        }
    }
}