using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord;
using Humanizer;
using Humanizer.Localisation;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;

namespace Rias.Core.Modules.Utility
{
    public partial class UtilityModule
    {
        [Name("Info")]
        public class InfoSubmodule : RiasModule
        {
            public InfoSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("stats")]
            public async Task StatsAsync()
            {
                var embed = new LocalEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Author = new LocalEmbedAuthorBuilder
                        {
                            Name = GetText(Localization.UtilityStats, RiasBot.CurrentUser.Name, Rias.Version),
                            IconUrl = RiasBot.CurrentUser.GetAvatarUrl()
                        },
                        ThumbnailUrl = RiasBot.CurrentUser.GetAvatarUrl(),
                        Footer = new LocalEmbedFooterBuilder().WithText("© 2018-2020 Copyright: Koneko#0001")
                    }.AddField(GetText(Localization.UtilityAuthor), Rias.Author, true)
                    .AddField(GetText(Localization.UtilityBotId), RiasBot.CurrentUser.Id, true)
                    .AddField(GetText(Localization.UtilityMasterId), Credentials.MasterId, true)
                    .AddField(GetText(Localization.UtilityShard), $"{RiasBot.GetShardId(Context.Guild?.Id ?? 0) + 1}/{RiasBot.Shards.Count}", true)
                    .AddField(GetText(Localization.UtilityInServer), Context.Guild?.Name ?? "-", true)
                    .AddField(GetText(Localization.UtilityCommandsAttempted), CommandHandlerService.CommandsAttempted, true)
                    .AddField(GetText(Localization.UtilityCommandsExecuted), CommandHandlerService.CommandsExecuted, true)
                    .AddField(GetText(Localization.UtilityUptime), Rias.UpTime.Elapsed.Humanize(5, new CultureInfo(Localization.GetGuildLocale(Context.Guild?.Id)),
                        TimeUnit.Month, TimeUnit.Second), true)
                    .AddField(GetText(Localization.UtilityPresence), $"{RiasBot.Guilds.Count} {GetText(Localization.UtilityServers)}\n" +
                                                   $"{RiasBot.Guilds.Sum(x => x.Value.TextChannels.Count)} {GetText(Localization.UtilityTextChannels)}\n" +
                                                   $"{RiasBot.Guilds.Sum(x => x.Value.TextChannels.Count)} {GetText(Localization.UtilityVoiceChannels)}\n" +
                                                   $"{RiasBot.Users.Count} {GetText(Localization.CommonUsers)}\n", true);

                var links = new StringBuilder();
                const string delimiter = " • ";

                if (!string.IsNullOrEmpty(Credentials.OwnerServerInvite))
                {
                    var ownerServer = RiasBot.GetGuild(Credentials.OwnerServerId);
                    links.Append(delimiter)
                        .Append(GetText(Localization.HelpSupportServer, ownerServer.Name, Credentials.OwnerServerInvite))
                        .Append("\n");
                }

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Credentials.Invite))
                    links.Append(GetText(Localization.HelpInviteMe, Credentials.Invite)).Append("\n");

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Credentials.Website))
                    links.Append(GetText(Localization.HelpWebsite, Credentials.Website)).Append("\n");

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Credentials.Patreon))
                    links.Append(GetText(Localization.HelpDonate, Credentials.Patreon)).Append("\n");

                embed.AddField(GetText(Localization.HelpLinks), links.ToString());

                await ReplyAsync(embed);
            }
            
            [Command("userinfo"), Context(ContextType.Guild)]
            public async Task UserInfoAsync([Remainder] CachedMember? member = null)
            {
                member ??= (CachedMember) Context.User;

                var userRoles = member.Roles.Select(x => x.Value).Where(x => !x.IsDefault)
                    .OrderByDescending(x => x.Position)
                    .Take(10)
                    .Select(x => x.Mention)
                    .ToList();

                var activity = "-";
                if (member.Presence != null && member.Presence.Activities.Count != 0)
                {
                    activity = string.Join("\n", member.Presence.Activities.Select(x => x switch
                    {
                        CustomActivity customActivity => $"{customActivity.Emoji} {customActivity.Text}",
                        RichActivity richActivity => GetText(Localization.BotActivity(richActivity.Type.ToString().ToLower()), richActivity.Name),
                        StreamingActivity streamingActivity => GetText(Localization.BotActivity(streamingActivity.Type.ToString().ToLower()), streamingActivity.Name),
                        SpotifyActivity spotifyActivity => $"{GetText(Localization.BotActivity(spotifyActivity.Type.ToString().ToLower()), spotifyActivity.Name)}\n" +
                                                           $"{spotifyActivity.Artists.Humanize()} - {spotifyActivity.TrackTitle}",
                        _ => GetText(Localization.BotActivity(x.Type.ToString().ToLower()), x.Name)
                    }));
                }

                var embed = new LocalEmbedBuilder()
                    .WithColor(RiasUtilities.ConfirmColor)
                    .WithThumbnailUrl(member.GetAvatarUrl())
                    .AddField(GetText(Localization.UtilityUsername), member, true)
                    .AddField(GetText(Localization.UtilityNickname), member.Nick ?? "-", true)
                    .AddField(GetText(Localization.CommonId), member.Id, true)
                    .AddField(GetText(Localization.UtilityActivity), activity, true)
                    .AddField(GetText(Localization.UtilityStatus), member.Presence?.Status.ToString() ?? "-", true)
                    .AddField(GetText(Localization.UtilityJoinedServer), member.JoinedAt.ToString("yyyy-MM-dd hh:mm:ss tt") ?? "-", true)
                    .AddField(GetText(Localization.UtilityJoinedDiscord), member.CreatedAt.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                    .AddField($"{GetText(Localization.UtilityRoles)} ({userRoles.Count})", userRoles.Count != 0 ? string.Join("\n", userRoles) : "-", true);

                await ReplyAsync(embed);
            }
            
            [Command("serverinfo"), Context(ContextType.Guild)]
            public async Task ServerInfo()
            {
                var embed = new LocalEmbedBuilder
                    {
                        Color = RiasUtilities.ConfirmColor,
                        Title = Context.Guild!.Name,
                        ThumbnailUrl = Context.Guild.GetRealIconUrl()
                    }.AddField(GetText(Localization.CommonId), Context.Guild.Id, true)
                    .AddField(GetText(Localization.UtilityOwner), Context.Guild.Owner, true)
                    .AddField(GetText(Localization.CommonUsers), Context.Guild.MemberCount, true)
                    .AddField(GetText(Localization.UtilityCurrentlyOnline),
                        Context.Guild.Members.Count(x => x.Value.Presence?.Status == UserStatus.Online ||
                                                         x.Value.Presence?.Status == UserStatus.Idle || x.Value.Presence?.Status == UserStatus.DoNotDisturb), true)
                    .AddField(GetText(Localization.UtilityBots), Context.Guild.Members.Count(x => x.Value.IsBot), true)
                    .AddField(GetText(Localization.UtilityCreatedAt), Context.Guild.CreatedAt.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                    .AddField(GetText(Localization.UtilityTextChannels), Context.Guild.TextChannels.Count, true)
                    .AddField(GetText(Localization.UtilityVoiceChannels), Context.Guild.VoiceChannels.Count, true)
                    .AddField(GetText(Localization.UtilitySystemChannel), Context.Guild.SystemChannel?.ToString() ?? "-", true)
                    .AddField(GetText(Localization.UtilityAfkChannel), Context.Guild.GetChannel(Context.Guild.AfkChannelId ?? 0)?.Name ?? "-", true)
                    .AddField(GetText(Localization.UtilityRegion), Context.Guild.VoiceRegionId, true)
                    .AddField(GetText(Localization.UtilityVerificationLevel), Context.Guild.VerificationLevel, true)
                    .AddField(GetText(Localization.UtilityBoostTier), Context.Guild.BoostTier, true)
                    .AddField(GetText(Localization.UtilityBoosts), Context.Guild.BoostingMemberCount, true);

                if (!string.IsNullOrEmpty(Context.Guild.VanityUrlCode))
                    embed.AddField(GetText(Localization.UtilityVanityUrl), Context.Guild.GetVanityInviteAsync());

                if (Context.Guild.Features.Count != 0)
                    embed.AddField(GetText(Localization.UtilityFeatures, Context.Guild.Features.Count), string.Join("\n", Context.Guild.Features), true);

                var emotes = new StringBuilder();
                foreach (var (_, emote) in Context.Guild.Emojis)
                {
                    var emoteString = emote.MessageFormat;
                    if (emotes.Length + emoteString.Length > 1024)
                        break;

                    emotes.Append(emoteString);
                }

                embed.AddField(GetText(Localization.UtilityEmojis, Context.Guild.Emojis.Count), emotes.Length != 0 ? emotes.ToString() : "-", true);

                if (!string.IsNullOrEmpty(Context.Guild.BannerHash))
                    embed.WithImageUrl(Context.Guild.GetBannerUrl());
                else if (!string.IsNullOrEmpty(Context.Guild.SplashHash))
                    embed.WithImageUrl(Context.Guild.GetSplashUrl());

                await ReplyAsync(embed);
            }
            
            [Command("avatar"), Context(ContextType.Guild)]
            public async Task AvatarAsync([Remainder] CachedMember? member = null)
            {
                member ??= (CachedMember) Context.User;

                var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Author = new LocalEmbedAuthorBuilder
                    {
                        Name = member.ToString(),
                        IconUrl = member.GetAvatarUrl(),
                        Url = member.GetAvatarUrl()
                    },
                    ImageUrl = member.GetAvatarUrl()
                };

                await ReplyAsync(embed);
            }
            
            [Command("servericon"), Context(ContextType.Guild)]
            public async Task ServerIconAsync()
            {
                var embed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Author = new LocalEmbedAuthorBuilder
                    {
                        Name = Context.Guild!.Name,
                        IconUrl = Context.Guild.GetRealIconUrl(),
                        Url = Context.Guild.GetRealIconUrl()
                    },
                    ImageUrl = Context.Guild.GetRealIconUrl()
                };

                await ReplyAsync(embed);
            }
            
            [Command("whoisplaying"), Context(ContextType.Guild)]
            public async Task WhoIsPlayingAsync([Remainder] string game)
            {
                var usersActivities = Context.Guild!.Members
                    .Select(x => x.Value)
                    .Where(x => x.Presence?.Activity != null && x.Presence.Activity.Name.StartsWith(game, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

                if (usersActivities.Count == 0)
                {
                    await ReplyErrorAsync(Localization.UtilityNoUserIsPlaying, game);
                    return;
                }

                var usersActivitiesList = new List<string>();
                var usersActivitiesGroup = usersActivities.OrderBy(x => x.Name)
                    .GroupBy(x => x.Presence.Activity.Name);

                foreach (var userActivity in usersActivitiesGroup)
                {
                    usersActivitiesList.Add($"•**{userActivity.Key}**");
                    usersActivitiesList.AddRange(userActivity.Select(x => $"\t~>{x.Name}"));
                }

                await SendPaginatedMessageAsync(usersActivitiesList, 15, (items, index) => new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ConfirmColor,
                    Title = GetText(Localization.UtilityUsersPlay, game),
                    Description = string.Join("\n", items)
                });
            }
        }
    }
}