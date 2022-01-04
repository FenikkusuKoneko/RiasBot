using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Extensions;
using Rias.Implementation;
using Rias.Services;

namespace Rias.Modules.Utility
{
    public partial class UtilityModule
    {
        [Name("Info")]
        public class InfoSubmodule : RiasModule
        {
            private readonly CommandHandlerService _commandHandlerService;
            
            public InfoSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
                _commandHandlerService = serviceProvider.GetRequiredService<CommandHandlerService>();
            }
            
            [Command("stats")]
            public async Task StatsAsync()
            {
                var uptime = RiasBot.UpTime.Elapsed.Humanize(5, new CultureInfo(Localization.GetGuildLocale(Context.Guild?.Id)), TimeUnit.Month, TimeUnit.Second);
                
                var commandsStatistics = _commandHandlerService.CommandStatistics;
                var commandsStatisticsString = $"{GetText(Localization.UtilityExecutedCommands, commandsStatistics.ExecutedCommands)}\n" +
                                               $"{GetText(Localization.UtilityAttemptedCommands, commandsStatistics.AttemptedCommands)}\n" +
                                               $"{GetText(Localization.UtilityCommandsPerSecond, commandsStatistics.CommandsPerSecondAverage.ToString("F2"))}\n" +
                                               $"{GetText(Localization.UtilityCommandsPerMinute, commandsStatistics.CommandsPerMinuteAverage.ToString("F2"))}\n" +
                                               $"{GetText(Localization.UtilityCommandsPerHour, commandsStatistics.CommandsPerHourAverage.ToString("F2"))}\n" +
                                               GetText(Localization.UtilityCommandsPerDay, commandsStatistics.CommandsPerDayAverage.ToString("F2"));

                var presence = GetText(Localization.UtilityPresenceInfo,
                    RiasBot.Client.ShardClients.Sum(x => x.Value.Guilds.Count),
                    RiasBot.Client.ShardClients.Sum(x => x.Value.Guilds.Sum(y => y.Value.MemberCount)),
                    RiasBot.Members.Count);

                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Author = new DiscordEmbedBuilder.EmbedAuthor
                        {
                            Name = GetText(Localization.UtilityStats, RiasBot.CurrentUser!.Username, RiasBot.Version),
                            IconUrl = RiasBot.CurrentUser.GetAvatarUrl(ImageFormat.Auto)
                        },
                        Footer = new DiscordEmbedBuilder.EmbedFooter
                        {
                            Text = GetText(Localization.UtilityStatsFooter)
                        }
                    }.WithThumbnail(RiasBot.CurrentUser.GetAvatarUrl(ImageFormat.Auto))
                    .AddField(GetText(Localization.UtilityAuthor), RiasBot.Author, true)
                    .AddField(GetText(Localization.UtilityBotId), RiasBot.CurrentUser.Id.ToString(), true)
                    .AddField(GetText(Localization.UtilityMasterId), Configuration.MasterId.ToString(), true)
                    .AddField(GetText(Localization.UtilityShard), $"{RiasBot.GetShardId(Context.Guild) + 1}/{RiasBot.Client.ShardClients.Count}", true)
                    .AddField(GetText(Localization.UtilityInServer), Context.Guild?.Name ?? "-", true)
                    .AddField(GetText(Localization.UtilityUptime), uptime, true)
                    .AddField(GetText(Localization.UtilityCommandsStatistics), commandsStatisticsString, true)
                    .AddField(GetText(Localization.UtilityPresence), presence, true);

                var links = new StringBuilder();
                const string delimiter = " • ";

                if (!string.IsNullOrEmpty(Configuration.OwnerServerInvite))
                {
                    var ownerServer = RiasBot.GetGuild(Configuration.OwnerServerId);
                    links.Append(delimiter)
                        .Append(GetText(Localization.HelpSupportServer, ownerServer!.Name, Configuration.OwnerServerInvite))
                        .AppendLine();
                }

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Configuration.Invite))
                    links.Append(GetText(Localization.HelpInviteMe, Configuration.Invite)).AppendLine();

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Configuration.Website))
                    links.Append(GetText(Localization.HelpWebsite, Configuration.Website)).AppendLine();

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Configuration.Patreon))
                    links.Append(GetText(Localization.HelpDonate, Configuration.Patreon)).AppendLine();

                embed.AddField(GetText(Localization.HelpLinks), links.ToString());

                await ReplyAsync(embed);
            }

            [Command("memberinfo", "minfo", "userinfo", "uinfo")]
            [Context(ContextType.Guild)]
            public async Task UserInfoAsync([Remainder] DiscordMember? member = null)
            {
                member ??= (DiscordMember) Context.User;

                var userRoles = member.Roles.OrderByDescending(x => x.Position).ToList();
                
                var sbRoles = new StringBuilder();
                foreach (var mention in userRoles.Select(x => x.Mention).TakeWhile(y => sbRoles.Length + y.Length <= 1024))
                    sbRoles.Append(mention).Append(' ');

                var locale = Localization.GetGuildLocale(Context.Guild!.Id);
                var joinedAtDateTime = member.JoinedAt.UtcDateTime;
                var joinedAt = $"{joinedAtDateTime:yyyy-MM-dd HH:mm:ss}\n" +
                               $"`{GetText(Localization.UtilityDateTimeAgo, (DateTime.UtcNow - joinedAtDateTime).Humanize(6, new CultureInfo(locale), TimeUnit.Year, TimeUnit.Second))}`";
                
                var creationTimestampDateTime = member.CreationTimestamp.UtcDateTime;
                var creationTimestamp = $"{creationTimestampDateTime:yyyy-MM-dd HH:mm:ss}\n" +
                                        $"`{GetText(Localization.UtilityDateTimeAgo, (DateTime.UtcNow - creationTimestampDateTime).Humanize(6, new CultureInfo(locale), TimeUnit.Year, TimeUnit.Second))}`";

                var embed = new DiscordEmbedBuilder()
                    .WithColor(RiasUtilities.ConfirmColor)
                    .WithThumbnail(string.IsNullOrEmpty(member.GuildAvatarHash) ? member.AvatarUrl : member.GuildAvatarUrl)
                    .AddField(GetText(Localization.UtilityUsername), member.FullName(), true)
                    .AddField(GetText(Localization.UtilityNickname), member.Nickname ?? "-", true)
                    .AddField(GetText(Localization.CommonId), member.Id.ToString(), true)
                    .AddField(GetText(Localization.UtilityJoinedServer), joinedAt, true)
                    .AddField(GetText(Localization.UtilityJoinedDiscord), creationTimestamp, true)
                    .AddField($"{GetText(Localization.UtilityRoles)} ({userRoles.Count})", userRoles.Count != 0 ? sbRoles.ToString() : "-");

                await ReplyAsync(embed);
            }

            [Command("serverinfo", "sinfo")]
            [Context(ContextType.Guild)]
            public async Task ServerInfo()
            {
                var locale = Localization.GetGuildLocale(Context.Guild!.Id);
                var creationTimestampDateTime = Context.Guild!.CreationTimestamp.UtcDateTime;
                var creationTimestamp = $"{creationTimestampDateTime:yyyy-MM-dd HH:mm:ss}\n" +
                                        $"`{GetText(Localization.UtilityDateTimeAgo, (DateTime.UtcNow - creationTimestampDateTime).Humanize(6, new CultureInfo(locale), TimeUnit.Year, TimeUnit.Second))}`";
                
                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Title = Context.Guild.Name
                    }.WithThumbnail(Context.Guild.GetIconUrl())
                    .AddField(GetText(Localization.CommonId), Context.Guild.Id.ToString(), true)
                    .AddField(GetText(Localization.UtilityOwner), Context.Guild.Owner.FullName(), true)
                    .AddField(GetText(Localization.CommonMembers), Context.Guild.MemberCount.ToString(), true)
                    .AddField(GetText(Localization.UtilityBots), Context.Guild.Members.Count(x => x.Value.IsBot).ToString(), true)
                    .AddField(GetText(Localization.UtilityCreatedAt), creationTimestamp, true)
                    .AddField(GetText(Localization.UtilityTextChannels), Context.Guild.Channels.Count(x => x.Value.Type == ChannelType.Text).ToString(), true)
                    .AddField(GetText(Localization.UtilityVoiceChannels), Context.Guild.Channels.Count(x => x.Value.Type == ChannelType.Voice).ToString(), true)
                    .AddField(GetText(Localization.UtilitySystemChannel), Context.Guild.SystemChannel?.Mention ?? "-", true)
                    .AddField(GetText(Localization.UtilityAfkChannel), Context.Guild.AfkChannel?.Name ?? "-", true)
                    .AddField(GetText(Localization.UtilityVerificationLevel), Context.Guild.VerificationLevel.ToString(), true)
                    .AddField(GetText(Localization.UtilityBoostTier), Context.Guild.PremiumTier.Humanize(), true)
                    .AddField(GetText(Localization.UtilityBoosts), Context.Guild.PremiumSubscriptionCount?.ToString() ?? "0", true);

                if (!string.IsNullOrEmpty(Context.Guild.VanityUrlCode))
                    embed.AddField(GetText(Localization.UtilityVanityUrl), (await Context.Guild.GetVanityInviteAsync()).ToString());

                if (Context.Guild.Features.Count != 0)
                    embed.AddField(GetText(Localization.UtilityFeatures, Context.Guild.Features.Count),
                        string.Join(" • ", Context.Guild.Features.OrderBy(f => f).Select(x => x.ToLower().Humanize(LetterCasing.Sentence))));

                var emotes = new StringBuilder();
                foreach (var (_, emote) in Context.Guild.Emojis)
                {
                    var emoteString = emote.ToString();
                    if (emotes.Length + emoteString.Length > 1024)
                        break;

                    emotes.Append(emoteString);
                }

                embed.AddField(GetText(Localization.UtilityEmojis, Context.Guild.Emojis.Count), emotes.Length != 0 ? emotes.ToString() : "-");

                if (!string.IsNullOrEmpty(Context.Guild.Banner))
                    embed.WithImageUrl($"{Context.Guild.BannerUrl}?size=2048");
                else if (!string.IsNullOrEmpty(Context.Guild.SplashHash))
                    embed.WithImageUrl($"{Context.Guild.SplashUrl}?size=2048");

                await ReplyAsync(embed);
            }

            [Command("avatar", "av")]
            [Context(ContextType.Guild)]
            public async Task GuildAvatarAsync([Remainder] DiscordMember? member = null)
            {
                member ??= (DiscordMember) Context.User;
                var avatarUrl = string.IsNullOrEmpty(member.GuildAvatarHash)
                    ? member.AvatarUrl
                    : member.GuildAvatarUrl;

                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = member.FullName(),
                        IconUrl = avatarUrl,
                        Url = avatarUrl
                    },
                    ImageUrl = avatarUrl
                };

                await ReplyAsync(embed);
            }
            
            [Command("useravatar", "uav")]
            [Context(ContextType.Guild)]
            public async Task GlobalAvatarAsync([Remainder] DiscordMember? member = null)
            {
                member ??= (DiscordMember) Context.User;
                var avatarUrl = member.AvatarUrl;

                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = member.FullName(),
                        IconUrl = avatarUrl,
                        Url = avatarUrl
                    },
                    ImageUrl = avatarUrl
                };

                await ReplyAsync(embed);
            }

            [Command("servericon", "sic")]
            [Context(ContextType.Guild)]
            public async Task ServerIconAsync()
            {
                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = Context.Guild!.Name,
                        IconUrl = Context.Guild.GetIconUrl(),
                        Url = Context.Guild.GetIconUrl()
                    },
                    ImageUrl = Context.Guild.GetIconUrl()
                };

                await ReplyAsync(embed);
            }
        }
    }
}