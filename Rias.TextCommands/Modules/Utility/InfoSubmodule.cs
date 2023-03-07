using System.Diagnostics;
using System.Globalization;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.Options;
using Qmmands;
using Qmmands.Text;
using Rias.Common;
using Rias.Services;
using Rias.Services.Extensions;

namespace Rias.TextCommands.Modules.Utility;

public partial class UtilityModule
{
    [Name("Info")]
    public class InfoSubmodule : RiasTextGuildModule
    {
        private readonly RiasConfiguration _configuration;

        public InfoSubmodule(IOptions<RiasConfiguration> options)
        {
            _configuration = options.Value;
        }
        
        [TextCommand("stats")]
        public async Task StatsAsync()
        {
            var riasBot = (IRiasBot) Context.Bot;
            var uptime = riasBot.ElapsedTime.Humanize(5, new CultureInfo(Localisation.GetGuildLocale(Context.GuildId)), TimeUnit.Month, TimeUnit.Second);

            var guilds = Context.Bot.GetGuilds();
            var membersCount = guilds.Sum(g => g.Value.MemberCount);
            var usersCount = Context.Bot.CacheProvider.TryGetUsers(out var cachedUsers) ? cachedUsers.Count : 0;
            
            var shardId = Context.Bot.ApiClient.GetShardId(Context.GuildId);
            var latency = (int) Context.Bot.ApiClient.Shards.Average(s => s.Value.Heartbeater.Latency?.TotalMilliseconds ?? 0);

            var sw = Stopwatch.StartNew();
            var message = await Context.GetChannel().SendMessageAsync(new LocalMessage().WithContent("Pinging..."));
            sw.Stop();
            var messageLatency = sw.ElapsedMilliseconds;
            
            if (Context.Bot.OwnerIds.Count == 0)
                await Context.Bot.IsOwnerAsync(0);  // Calling IsOwnerAsync just for OwnersIds to be populated

            var embed = new LocalEmbed()
                .WithColor(Utils.SuccessColor)
                .WithAuthor($"{Context.Bot.CurrentUser.Name} v{riasBot.Version}", Context.Bot.CurrentUser.GetAvatarUrl(CdnAssetFormat.Automatic, 128))
                .WithThumbnailUrl(Context.Bot.CurrentUser.GetAvatarUrl(CdnAssetFormat.Automatic, 1024))
                .WithFooter(GetText(Strings.Utility.StatsFooter));

            var ownerIds = Context.Bot.OwnerIds;
            if (ownerIds.Count == 1 && ownerIds[0] == riasBot.AuthorId)
            {
                embed.AddField(GetText(Strings.Utility.Author), riasBot.Author, true);
            }
            else
            {
                var owners = new List<IUser>();
                foreach (var ownerId in ownerIds)
                {
                    var owner = (IUser?) Context.Bot.GetUser(ownerId) ?? await Context.Bot.FetchUserAsync(ownerId);
                    if (owner is not null)
                        owners.Add(owner);
                }
                
                embed.AddField(GetText(owners.Count == 1 ? Strings.Utility.Owner : Strings.Utility.Owners), 
                    owners.Count > 0 ? string.Join('\n', owners) : "-", true);
            }
            
            embed.AddField(GetText(Strings.Utility.Shard), $"{shardId.Index + 1}/{shardId.Count}", true)
                .AddField(GetText(Strings.Utility.InServer), Context.GetGuild().Name, true)
                .AddField(GetText(Strings.Utility.Uptime), uptime, true);

            embed.AddField(GetText(Strings.Utility.Ping), GetText(Strings.Utility.PingInfo, latency, messageLatency), true)
                .AddField(GetText(Strings.Utility.Presence), GetText(Strings.Utility.PresenceInfo, guilds.Count, usersCount, membersCount), true);

            var links = new List<string>();
            const string delimiter = " • ";

            var ownerServer = Context.Bot.GetGuild(_configuration.OwnerServerId);
            if (ownerServer is not null && !string.IsNullOrEmpty(_configuration.OwnerServerInviteLink))
                links.Add(delimiter + GetText(Strings.Help.SupportServer, ownerServer.Name, _configuration.OwnerServerInviteLink));

            if (!string.IsNullOrEmpty(_configuration.InviteLink))
                links.Add(delimiter + GetText(Strings.Help.InviteMe, _configuration.InviteLink));

            if (!string.IsNullOrEmpty(_configuration.WebsiteUrl))
                links.Add(delimiter + GetText(Strings.Help.Website, _configuration.WebsiteUrl));

            if (!string.IsNullOrEmpty(_configuration.PatreonUrl))
                links.Add(delimiter + GetText(Strings.Help.Donate, _configuration.PatreonUrl));

            embed.AddField(GetText(Strings.Links), string.Join('\n', links));
            
            
            await message.ModifyAsync(props =>
            {
                props.Content = null;
                props.Embeds = new[] { embed };
            });
        }
    }
}