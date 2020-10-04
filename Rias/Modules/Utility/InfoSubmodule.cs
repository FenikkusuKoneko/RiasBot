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
                var commandsStatistics = _commandHandlerService.CommandStatistics;
                var commandsStatisticsString = $"{GetText(Localization.UtilityExecutedCommands, commandsStatistics.ExecutedCommands)}\n" +
                                               $"{GetText(Localization.UtilityAttemptedCommands, commandsStatistics.AttemptedCommands)}\n" +
                                               $"{GetText(Localization.UtilityCommandsPerSecond, commandsStatistics.CommandsPerSecondAverage.ToString("F2"))}\n" +
                                               $"{GetText(Localization.UtilityCommandsPerMinute, commandsStatistics.CommandsPerMinuteAverage.ToString("F2"))}\n" +
                                               $"{GetText(Localization.UtilityCommandsPerHour, commandsStatistics.CommandsPerHourAverage.ToString("F2"))}\n" +
                                               GetText(Localization.UtilityCommandsPerDay, commandsStatistics.CommandsPerDayAverage.ToString("F2"));

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
                            Text = "© 2018-2020 Copyright: Koneko#0001"
                        }
                    }.WithThumbnail(RiasBot.CurrentUser.GetAvatarUrl(ImageFormat.Auto))
                    .AddField(GetText(Localization.UtilityAuthor), RiasBot.Author, true)
                    .AddField(GetText(Localization.UtilityBotId), RiasBot.CurrentUser.Id.ToString(), true)
                    .AddField(GetText(Localization.UtilityMasterId), Credentials.MasterId.ToString(), true)
                    .AddField(GetText(Localization.UtilityShard), $"{RiasBot.GetShardId(Context.Guild) + 1}/{RiasBot.Client.ShardClients.Count}", true)
                    .AddField(GetText(Localization.UtilityInServer), Context.Guild?.Name ?? "-", true)
                    .AddField(GetText(Localization.UtilityUptime),
                        RiasBot.UpTime.Elapsed.Humanize(5, new CultureInfo(Localization.GetGuildLocale(Context.Guild?.Id)), TimeUnit.Month, TimeUnit.Second),
                        true)
                    .AddField(GetText(Localization.UtilityCommandsStatistics), commandsStatisticsString, true)
                    .AddField(GetText(Localization.UtilityPresence),
                        $"{RiasBot.Client.ShardClients.Sum(x => x.Value.Guilds.Count)} {GetText(Localization.UtilityServers)}\n" +
                        $"{RiasBot.Members.Count} {GetText(Localization.CommonUsers)}\n",
                        true);

                var links = new StringBuilder();
                const string delimiter = " • ";

                if (!string.IsNullOrEmpty(Credentials.OwnerServerInvite))
                {
                    var ownerServer = RiasBot.GetGuild(Credentials.OwnerServerId);
                    links.Append(delimiter)
                        .Append(GetText(Localization.HelpSupportServer, ownerServer!.Name, Credentials.OwnerServerInvite))
                        .AppendLine();
                }

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Credentials.Invite))
                    links.Append(GetText(Localization.HelpInviteMe, Credentials.Invite)).AppendLine();

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Credentials.Website))
                    links.Append(GetText(Localization.HelpWebsite, Credentials.Website)).AppendLine();

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Credentials.Patreon))
                    links.Append(GetText(Localization.HelpDonate, Credentials.Patreon)).AppendLine();

                embed.AddField(GetText(Localization.HelpLinks), links.ToString());

                await ReplyAsync(embed);
            }

            [Command("userinfo")]
            [Context(ContextType.Guild)]
            public async Task UserInfoAsync([Remainder] DiscordMember? member = null)
            {
                member ??= (DiscordMember)Context.User;

                var userRoles = member.Roles.Where(x => x.Id != Context.Guild!.EveryoneRole.Id)
                    .OrderByDescending(x => x.Position)
                    .Take(10)
                    .Select(x => x.Mention)
                    .ToList();

                var embed = new DiscordEmbedBuilder()
                    .WithColor(RiasUtilities.ConfirmColor)
                    .WithThumbnail(member.GetAvatarUrl(ImageFormat.Auto))
                    .AddField(GetText(Localization.UtilityUsername), member.FullName(), true)
                    .AddField(GetText(Localization.UtilityNickname), member.Nickname ?? "-", true)
                    .AddField(GetText(Localization.CommonId), member.Id.ToString(), true)
                    .AddField(GetText(Localization.UtilityJoinedServer), member.JoinedAt.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                    .AddField(GetText(Localization.UtilityJoinedDiscord), member.CreationTimestamp.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                    .AddField($"{GetText(Localization.UtilityRoles)} ({userRoles.Count})", userRoles.Count != 0 ? string.Join("\n", userRoles) : "-", true);

                await ReplyAsync(embed);
            }

            [Command("serverinfo")]
            [Context(ContextType.Guild)]
            public async Task ServerInfo()
            {
                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Title = Context.Guild!.Name
                    }.WithThumbnail(Context.Guild.GetIconUrl())
                    .AddField(GetText(Localization.CommonId), Context.Guild.Id.ToString(), true)
                    .AddField(GetText(Localization.UtilityOwner), Context.Guild.Owner.FullName(), true)
                    .AddField(GetText(Localization.CommonUsers), Context.Guild.MemberCount.ToString(), true)
                    .AddField(GetText(Localization.UtilityBots), Context.Guild.Members.Count(x => x.Value.IsBot).ToString(), true)
                    .AddField(GetText(Localization.UtilityCreatedAt), Context.Guild.CreationTimestamp.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                    .AddField(GetText(Localization.UtilityTextChannels), Context.Guild.Channels.Count(x => x.Value.Type == ChannelType.Text).ToString(), true)
                    .AddField(GetText(Localization.UtilityVoiceChannels), Context.Guild.Channels.Count(x => x.Value.Type == ChannelType.Voice).ToString(), true)
                    .AddField(GetText(Localization.UtilitySystemChannel), Context.Guild.SystemChannel?.Mention ?? "-", true)
                    .AddField(GetText(Localization.UtilityAfkChannel), Context.Guild.AfkChannel?.Name ?? "-", true)
                    .AddField(GetText(Localization.UtilityRegion), Context.Guild.VoiceRegion.Id, true)
                    .AddField(GetText(Localization.UtilityVerificationLevel), Context.Guild.VerificationLevel.ToString(), true)
                    .AddField(GetText(Localization.UtilityBoostTier), Context.Guild.PremiumTier.ToString(), true)
                    .AddField(GetText(Localization.UtilityBoosts), Context.Guild.PremiumSubscriptionCount?.ToString() ?? "0", true);

                if (!string.IsNullOrEmpty(Context.Guild.VanityUrlCode))
                    embed.AddField(GetText(Localization.UtilityVanityUrl), (await Context.Guild.GetVanityInviteAsync()).ToString());

                if (Context.Guild.Features.Count != 0)
                    embed.AddField(GetText(Localization.UtilityFeatures, Context.Guild.Features.Count),
                        string.Join("\n", Context.Guild.Features.Select(x => x.ToLower().Humanize(LetterCasing.Sentence))), true);

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

            [Command("avatar")]
            [Context(ContextType.Guild)]
            public async Task AvatarAsync([Remainder] DiscordMember? member = null)
            {
                member ??= (DiscordMember)Context.User;

                var embed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = member.FullName(),
                        IconUrl = member.GetAvatarUrl(ImageFormat.Auto),
                        Url = member.GetAvatarUrl(ImageFormat.Auto)
                    },
                    ImageUrl = member.GetAvatarUrl(ImageFormat.Auto)
                };

                await ReplyAsync(embed);
            }

            [Command("servericon")]
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