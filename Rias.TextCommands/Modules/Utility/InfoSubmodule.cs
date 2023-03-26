using System.Diagnostics;
using System.Globalization;
using System.Text;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.Options;
using Qmmands;
using Qmmands.Text;
using Qommon;
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

            var embed = SuccessEmbed
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

        [TextCommand("memberinfo", "minfo", "userinfo", "uinfo")]
        public IResult MemberInfo([Remainder] IMember? member = null)
        {
            member ??= Context.Author;
            var locale = Localisation.GetGuildLocale(Context.GuildId);
            
            var joinedAt = member.JoinedAt.GetValueOrDefault();
            var joinedAtString = $"{joinedAt:yyyy-MM-dd HH:mm:ss}\n" +
                                 $"`{GetText(Strings.Utility.DateTimeAgo, (DateTime.UtcNow - joinedAt).Humanize(6, new CultureInfo(locale), TimeUnit.Year, TimeUnit.Second))}`";

            var createdAt = member.CreatedAt();
            var createdAtString = $"{createdAt:yyyy-MM-dd HH:mm:ss}\n" +
                                  $"`{GetText(Strings.Utility.DateTimeAgo, (DateTime.UtcNow - createdAt).Humanize(6, new CultureInfo(locale), TimeUnit.Year, TimeUnit.Second))}`";

            var rolesString = new StringBuilder();
            var roleMentions = member
                .GetRoles()
                .Where(r => r.Value.Id != r.Value.GuildId)
                .Select(r => r.Value.Mention)
                .TakeWhile(rm => rolesString.Length + rm.Length <= 1024);
            
            foreach (var roleMention in roleMentions)
                rolesString.Append(roleMention).Append(' ');

            var embed = SuccessEmbed
                .WithThumbnailUrl(member.GetGuildAvatarUrl(CdnAssetFormat.Automatic, 1024))
                .AddField(GetText(Strings.Utility.Username), member.Tag, true)
                .AddField(GetText(Strings.Utility.Nickname), member.Nick ?? "-", true)
                .AddField(GetText(Strings.Id), member.Id.ToString(), true)
                .AddField(GetText(Strings.Utility.JoinedServer), joinedAtString, true)
                .AddField(GetText(Strings.Utility.JoinedDiscord), createdAtString, true)
                .AddField($"{GetText(Strings.Utility.Roles)} ({member.RoleIds.Count})", member.RoleIds.Count > 0 ? rolesString : '-');

            return Reply(embed);
        }

        [TextCommand("serverinfo", "sinfo")]
        public IResult ServerInfo()
        {
            var guild = Context.GetGuild();
            var locale = Localisation.GetGuildLocale(Context.GuildId);

            var createdAt = guild.CreatedAt();
            var createdAtString = $"{createdAt:yyyy-MM-dd HH:mm:ss}\n" +
                                  $"`{GetText(Strings.Utility.DateTimeAgo, (DateTime.UtcNow - createdAt).Humanize(6, new CultureInfo(locale), TimeUnit.Year, TimeUnit.Second))}`";

            var channels = guild.GetChannels();
            var textChannelsCount = channels.Count(ch => ch.Value.Type is ChannelType.Text or ChannelType.News or ChannelType.Forum);
            var voiceChannelsCount = channels.Count(ch => ch.Value.Type is ChannelType.Voice or ChannelType.Stage);

            var embed = SuccessEmbed
                .WithTitle(guild.Name)
                .AddField(GetText(Strings.Id), guild.Id, true)
                .AddField(GetText(Strings.Utility.Owner), guild.GetMember(guild.OwnerId)?.Tag ?? "-", true)
                .AddField(GetText(Strings.Members), guild.MemberCount, true)
                .AddField(GetText(Strings.Utility.Bots), guild.Members.Count(x => x.Value.IsBot), true)
                .AddField(GetText(Strings.Utility.CreatedAt), createdAtString, true)
                .AddField(GetText(Strings.Utility.TextChannels), textChannelsCount, true)
                .AddField(GetText(Strings.Utility.VoiceChannels), voiceChannelsCount, true);

            if (guild.SystemChannelId is not null)
            {
                var systemChannel = guild.GetChannel(guild.SystemChannelId.Value);
                if (systemChannel is not null)
                    embed.AddField(GetText(Strings.Utility.SystemChannel), systemChannel.Name, true);
            }

            if (guild.AfkChannelId is not null)
            {
                var afkChannel = guild.GetChannel(guild.AfkChannelId.Value);
                if (afkChannel is not null)
                    embed.AddField(GetText(Strings.Utility.AfkChannel), afkChannel.Name, true);
            }
            
            embed.AddField(GetText(Strings.Utility.VerificationLevel), guild.VerificationLevel, true)
                .AddField(GetText(Strings.Utility.BoostTier), guild.BoostTier, true)
                .AddField(GetText(Strings.Utility.Boosts), guild.BoostingMemberCount ?? 0, true);
            
            if (!string.IsNullOrEmpty(guild.VanityUrlCode))
                embed.AddField(GetText(Strings.Utility.VanityUrl), $"https://discord.gg/{guild.VanityUrlCode}");

            if (guild.Features.Count > 0)
            {
                embed.AddField(GetText(Strings.Utility.Features, guild.Features.Count),
                    string.Join(" • ", guild.Features.OrderBy(f => f).Select(x => x.ToLower().Humanize(LetterCasing.Sentence))));
            }
            
            var emojisString = new StringBuilder();
            foreach (var (_, emoji) in guild.Emojis)
            {
                if (emojisString.Length + emoji.Tag.Length > 1024)
                    break;

                emojisString.Append(emoji.Tag);
            }

            embed.AddField(GetText(Strings.Utility.Emojis, guild.Emojis.Count), guild.Emojis.Count > 0 ? emojisString : '-');
            
            var guildIconUrl = guild.GetIconUrl(CdnAssetFormat.Automatic, 1024);
            if (!string.IsNullOrEmpty(guildIconUrl))
                embed.WithThumbnailUrl(guildIconUrl);

            var imageUrl = guild.GetBannerUrl(CdnAssetFormat.Automatic, 2048)
                           ?? guild.GetDiscoverySplashUrl(CdnAssetFormat.Automatic, 2048)
                           ?? guild.GetSplashUrl(CdnAssetFormat.Automatic, 2048);
            
            if (!string.IsNullOrEmpty(imageUrl))
                embed.WithImageUrl(imageUrl);

            return Reply(embed);
        }

        [TextCommand("avatar", "av")]
        public IResult Avatar([Remainder] IMember? member = null)
        {
            member ??= Context.Author;
            var memberAvatarUrl = member.GetGuildAvatarUrl(CdnAssetFormat.Automatic, 2048);

            var embed = SuccessEmbed
                .WithAuthor(member.Tag, member.GetGuildAvatarUrl(CdnAssetFormat.Automatic, 128), memberAvatarUrl)
                .WithImageUrl(memberAvatarUrl);

            return Reply(embed);
        }
        
        [TextCommand("useravatar", "uav")]
        public IResult UserAvatar([Remainder] IMember? member = null)
        {
            member ??= Context.Author;
            var memberAvatarUrl = member.GetAvatarUrl(CdnAssetFormat.Automatic, 2048);

            var embed = SuccessEmbed
                .WithAuthor(member.Tag, member.GetAvatarUrl(CdnAssetFormat.Automatic, 128), memberAvatarUrl)
                .WithImageUrl(memberAvatarUrl);

            return Reply(embed);
        }
        
        [TextCommand("servericon", "sic")]
        public IResult ServerIcon()
        {
            var guild = Context.GetGuild();
            var guildIconUrl = guild.GetIconUrl(CdnAssetFormat.Automatic, 2048);

            if (!string.IsNullOrEmpty(guildIconUrl))
            {
                var embed = SuccessEmbed
                    .WithAuthor(guild.Name, guild.GetIconUrl(CdnAssetFormat.Automatic, 128), guildIconUrl)
                    .WithImageUrl(guildIconUrl);

                return Reply(embed);
            }

            return ErrorReply(Strings.Utility.NoGuildIcon);
        }
    }
}