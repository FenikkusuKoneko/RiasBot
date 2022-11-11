using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Rias.Configurations;
using Rias.Implementation;
using Localization = Rias.Implementation.Localization;

namespace Rias.SlashModules.Help
{
    public class HelpSlashModule : ApplicationCommandModule
    {
        private readonly RiasBot _riasBot;
        private readonly Configuration _configuration;
        private readonly Localization _localization;
        
        public HelpSlashModule(RiasBot riasBot, Configuration configuration, Localization localization)
        {
            _riasBot = riasBot;
            _configuration = configuration;
            _localization = localization;
        }
        
        [SlashCommand("help", "Shows the help page.")]
        public async Task HelpAsync(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithColor(RiasUtilities.ConfirmColor)
                .WithAuthor(_localization.GetText(ctx.Guild.Id, Localization.HelpTitle, _riasBot.CurrentUser!.Username, RiasBot.Version), _riasBot.CurrentUser.GetAvatarUrl(ImageFormat.Auto))
                .WithFooter(_localization.GetText(ctx.Guild.Id, Localization.HelpFooter))
                .WithDescription(_localization.GetText(ctx.Guild.Id, Localization.HelpInfo, "rias "));

            var links = new StringBuilder();
            const string delimiter = " • ";

            if (!string.IsNullOrEmpty(_configuration.OwnerServerInvite))
            {
                var ownerServer = _riasBot.GetGuild(_configuration.OwnerServerId);
                links.Append(delimiter)
                    .Append(_localization.GetText(ctx.Guild.Id, Localization.HelpSupportServer, ownerServer!.Name, _configuration.OwnerServerInvite))
                    .AppendLine();
            }

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(_configuration.Invite))
                links.Append(_localization.GetText(ctx.Guild.Id, Localization.HelpInviteMe, _configuration.Invite)).AppendLine();

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(_configuration.Website))
                links.Append(_localization.GetText(ctx.Guild.Id, Localization.HelpWebsite, _configuration.Website)).AppendLine();

            if (links.Length > 0) links.Append(delimiter);
            if (!string.IsNullOrEmpty(_configuration.Patreon))
                links.Append(_localization.GetText(ctx.Guild.Id, Localization.HelpDonate, _configuration.Patreon)).AppendLine();

            embed.AddField(_localization.GetText(ctx.Guild.Id, Localization.HelpLinks), links.ToString());
            await ctx.CreateResponseAsync(embed);
        }
    }
}