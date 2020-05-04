using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Core.Services;
using Rias.Interactive;
using Rias.Interactive.Paginator;

namespace Rias.Core.Modules.Utility
{
    public partial class Utility
    {
        [Name("Info")]
        public class Info : RiasModule
        {
            private readonly DiscordShardedClient _client;
            private readonly InteractiveService _interactive;

            public Info(IServiceProvider services) : base(services)
            {
                _client = services.GetRequiredService<DiscordShardedClient>();
                _interactive = services.GetRequiredService<InteractiveService>();
            }

            [Command("stats")]
            public async Task StatsAsync()
            {
                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Author = new EmbedAuthorBuilder
                        {
                            Name = GetText("Stats", Context.Client.CurrentUser.Username, Rias.Version),
                            IconUrl = Context.Client.CurrentUser.GetRealAvatarUrl()
                        },
                        ThumbnailUrl = Context.Client.CurrentUser.GetRealAvatarUrl(),
                        Footer = new EmbedFooterBuilder().WithText("© 2018-2020 Copyright: Koneko#0001")
                    }.AddField(GetText("Author"), Rias.Author, true)
                    .AddField(GetText("BotId"), Context.Client.CurrentUser.Id, true)
                    .AddField(GetText("MasterId"), Credentials.MasterId, true)
                    .AddField(GetText("Shard"), $"{_client.GetShardIdFor(Context.Guild) + 1}/{_client.Shards.Count}", true)
                    .AddField(GetText("InServer"), Context.Guild?.Name ?? "-", true)
                    .AddField(GetText("CommandsAttempted"), CommandHandlerService.CommandsAttempted, true)
                    .AddField(GetText("CommandsExecuted"), CommandHandlerService.CommandsExecuted, true)
                    .AddField(GetText("Uptime"), Rias.UpTime.Elapsed.Humanize(5, Resources.GetGuildCulture(Context.Guild?.Id), TimeUnit.Month, TimeUnit.Second), true)
                    .AddField(GetText("Presence"), $"{_client.Guilds.Count} {GetText("Servers")}\n" +
                                                   $"{_client.Guilds.Sum(x => x.TextChannels.Count)} {GetText("TextChannels")}\n" +
                                                   $"{_client.Guilds.Sum(x => x.VoiceChannels.Count)} {GetText("VoiceChannels")}\n" +
                                                   $"{_client.Guilds.Sum(x => x.MemberCount)} {GetText("#Common_Users")}\n", true);

                var links = new StringBuilder();
                const string delimiter = " • ";

                if (!string.IsNullOrEmpty(Credentials.OwnerServerInvite))
                {
                    var ownerServer = _client.GetGuild(Credentials.OwnerServerId);
                    links.Append(delimiter)
                        .Append(GetText("#Help_SupportServer", ownerServer.Name, Credentials.OwnerServerInvite))
                        .Append("\n");
                }

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Credentials.Invite))
                    links.Append(GetText("#Help_InviteMe", Credentials.Invite)).Append("\n");

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Credentials.Website))
                    links.Append(GetText("#Help_Website", Credentials.Website)).Append("\n");

                if (links.Length > 0) links.Append(delimiter);
                if (!string.IsNullOrEmpty(Credentials.Patreon))
                    links.Append(GetText("#Help_Donate", Credentials.Patreon)).Append("\n");

                embed.AddField(GetText("#Help_Links"), links.ToString());

                await ReplyAsync(embed);
            }

            [Command("userinfo"), Context(ContextType.Guild)]
            public async Task UserInfoAsync([Remainder] SocketGuildUser? user = null)
            {
                user ??= (SocketGuildUser) Context.User;

                var userRoles = user.Roles.Where(role => role.Id != Context.Guild!.EveryoneRole.Id)
                    .OrderByDescending(r => r.Position)
                    .Take(10)
                    .Select(x => x.Mention)
                    .ToList();

                var activity = user.Activity switch
                {
                    null => "-",
                    CustomStatusGame customStatusGame => customStatusGame.ToString(),
                    _ => GetText($"#Bot_Activity{user.Activity.Type}", user.Activity.Name)
                };

                var embed = new EmbedBuilder()
                    .WithColor(RiasUtils.ConfirmColor)
                    .WithThumbnailUrl(user.GetRealAvatarUrl())
                    .AddField(GetText("Username"), user, true)
                    .AddField(GetText("Nickname"), user.Nickname ?? "-", true)
                    .AddField(GetText("#Common_Id"), user.Id, true)
                    .AddField(GetText("Activity"), !string.IsNullOrWhiteSpace(activity) ? activity : "-", true)
                    .AddField(GetText("Status"), user.Status, true)
                    .AddField(GetText("JoinedServer"), user.JoinedAt?.ToString("yyyy-MM-dd hh:mm:ss tt") ?? "-", true)
                    .AddField(GetText("JoinedDiscord"), user.CreatedAt.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                    .AddField($"{GetText("Roles")} ({userRoles.Count})", userRoles.Count != 0 ? string.Join("\n", userRoles) : "-", true);

                await ReplyAsync(embed);
            }

            [Command("serverinfo"), Context(ContextType.Guild)]
            public async Task ServerInfo()
            {
                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Title = Context.Guild!.Name,
                        ThumbnailUrl = Context.Guild.GetRealIconUrl()
                    }.AddField(GetText("#Common_Id"), Context.Guild.Id, true)
                    .AddField(GetText("Owner"), Context.Guild.Owner, true)
                    .AddField(GetText("#Common_Users"), Context.Guild.MemberCount, true)
                    .AddField(GetText("CurrentlyOnline"), Context.Guild.Users.Count(x => x.Status != UserStatus.Offline && x.Status != UserStatus.Invisible), true)
                    .AddField(GetText("Bots"), Context.Guild.Users.Count(x => x.IsBot), true)
                    .AddField(GetText("CreatedAt"), Context.Guild.CreatedAt.ToString("yyyy-MM-dd hh:mm:ss tt"), true)
                    .AddField(GetText("TextChannels"), Context.Guild.TextChannels.Count, true)
                    .AddField(GetText("VoiceChannels"), Context.Guild.VoiceChannels.Count, true)
                    .AddField(GetText("SystemChannel"), Context.Guild.SystemChannel?.ToString() ?? "-", true)
                    .AddField(GetText("AFKChannel"), Context.Guild.AFKChannel?.ToString() ?? "-", true)
                    .AddField(GetText("Region"), Context.Guild.VoiceRegionId, true)
                    .AddField(GetText("VerificationLevel"), Context.Guild.VerificationLevel, true)
                    .AddField(GetText("PremiumTier"), Context.Guild.PremiumTier, true)
                    .AddField(GetText("Boosts"), Context.Guild.PremiumSubscriptionCount, true);

                if (!string.IsNullOrEmpty(Context.Guild.VanityURLCode))
                    embed.AddField(GetText("VanityUrl"), Context.Guild.GetVanityInviteAsync());

                if (Context.Guild.Features.Count != 0)
                    embed.AddField(GetText("Features", Context.Guild.Features.Count), string.Join("\n", Context.Guild.Features), true);

                var emotes = new StringBuilder();
                foreach (var emote in Context.Guild.Emotes)
                {
                    var emoteString = emote.ToString();
                    if (emotes.Length + emoteString.Length > 1024)
                        break;

                    emotes.Append(emoteString);
                }

                embed.AddField(GetText("Emotes", Context.Guild.Emotes.Count), emotes.Length != 0 ? emotes.ToString() : "-", true);

                if (!string.IsNullOrEmpty(Context.Guild.BannerId))
                    embed.WithImageUrl(Context.Guild.GetBannerUrl());
                else if (!string.IsNullOrEmpty(Context.Guild.SplashId))
                    embed.WithImageUrl(Context.Guild.GetSplashUrl());

                await ReplyAsync(embed);
            }

            [Command("shardsinfo")]
            public async Task ShardsInfo()
            {
                var shards = _client.Shards;
                var connectedShards = shards.Count(x => x.ConnectionState == ConnectionState.Connected);

                var index = 1;
                var pages = _client.Shards.Batch(15, x => new InteractiveMessage
                (
                    new EmbedBuilder
                    {
                        Color = RiasUtils.ConfirmColor,
                        Title = GetText("ShardsInfo", connectedShards, shards.Count),
                        Description = string.Join("\n", x.Select(shard => GetText("ShardState", index++, shard.ConnectionState)))
                    }
                ));

                await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
            }

            [Command("avatar"), Context(ContextType.Guild)]
            public async Task AvatarAsync([Remainder] SocketGuildUser? user = null)
            {
                user ??= (SocketGuildUser) Context.User;

                var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Author = new EmbedAuthorBuilder
                    {
                        Name = user.ToString(),
                        IconUrl = user.GetRealAvatarUrl(),
                        Url = user.GetRealAvatarUrl()
                    },
                    ImageUrl = user.GetRealAvatarUrl()
                };

                await ReplyAsync(embed);
            }
            
            [Command("servericon"), Context(ContextType.Guild)]
            public async Task ServerIconAsync()
            {
                var embed = new EmbedBuilder
                {
                    Color = RiasUtils.ConfirmColor,
                    Author = new EmbedAuthorBuilder
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
                var usersActivities = Context.Guild!.Users
                    .Where(x => x.Activity != null && x.Activity.Name.StartsWith(game, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

                if (usersActivities.Count == 0)
                {
                    await ReplyErrorAsync("NoUserIsPlaying", game);
                    return;
                }

                var usersActivitiesList = new List<string>();
                var usersActivitiesGroup = usersActivities.OrderBy(x => x.Username)
                    .GroupBy(x => x.Activity.Name);

                foreach (var userActivity in usersActivitiesGroup)
                {
                    usersActivitiesList.Add($"•{Format.Bold(userActivity.Key)}");
                    usersActivitiesList.AddRange(userActivity.Select(x => $"\t~>{x.Username}"));
                }

                var pages = usersActivitiesList.Batch(15, x => new InteractiveMessage
                    (
                        new EmbedBuilder
                        {
                            Color = RiasUtils.ConfirmColor,
                            Title = GetText("UsersPlay", game),
                            Description = string.Join("\n", x)
                        }
                    ));

                await _interactive.SendPaginatedMessageAsync(Context.Message, new PaginatedMessage(pages));
            }
        }
    }
}