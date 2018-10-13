using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using RiasBot.Services;
using Discord.WebSocket;
using System.Globalization;
using System.Diagnostics;
using Discord.Addons.Interactive;
using Microsoft.EntityFrameworkCore;
using RiasBot.Modules.Music.Services;

namespace RiasBot.Modules.Utility
{
    public partial class Utility
    {
        public class InfoCommands : RiasModule
        {
            private readonly DiscordShardedClient _client;
            private readonly InteractiveService _is;
            private readonly MusicService _musicService;

            public InfoCommands(DiscordShardedClient client, InteractiveService interactiveService, MusicService musicService)
            {
                _client = client;
                _is = interactiveService;
                _musicService = musicService;
            }

            [RiasCommand]
            [@Alias]
            [@Remarks]
            [Description]
            public async Task Stats()
            {
                var author = _client.GetUser(RiasBot.KonekoId);
                var guilds = await Context.Client.GetGuildsAsync().ConfigureAwait(false);
                var shard = 0;
                if (Context.Guild != null)
                    shard = _client.GetShardIdFor(Context.Guild) + 1;

                var textChannels = 0;
                var voiceChannels = 0;
                var users = 0;

                foreach (SocketGuild guild in guilds)
                {
                    textChannels += guild.TextChannels.Count;
                    voiceChannels += guild.VoiceChannels.Count;
                    users += guild.MemberCount;
                }
                
                var musicPlaying = _musicService.MPlayer.Count(m => m.Value.Player != null && m.Value.Player.Playing);
                var musicAfk = _musicService.MPlayer.Count(m => m.Value.Player != null && !m.Value.Player.Playing);

                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);

                embed.WithAuthor($"{Context.Client.CurrentUser.Username} Bot v{RiasBot.Version}", Context.Client.CurrentUser.GetRealAvatarUrl());
                embed.AddField("Author", author?.ToString() ?? RiasBot.Author, true).AddField("Bot ID", Context.Client.CurrentUser.Id, true);
                embed.AddField("Master ID", RiasBot.KonekoId, true).AddField("Shard", $"#{shard}/{_client.Shards.Count()}", true);
                embed.AddField("In server", Context.Guild?.Name ?? "-", true).AddField("Commands Run", RiasBot.CommandsRun, true);
                embed.AddField("Uptime", GetTimeString(RiasBot.UpTime.Elapsed), true).AddField("Presence", $"{guilds.Count} Servers\n{textChannels} " +
                    $"Text Channels\n{voiceChannels} Voice Channels\n{users} Users", true);
                embed.AddField("Music", $"Playing in {musicPlaying} voice channels\nAFK in {musicAfk} voice channels", true)
                    .AddField("Links", $"[Invite me]({RiasBot.Invite}) • [Support server]({RiasBot.CreatorServer})\n" +
                                        $"[Website]({RiasBot.Website}) • [Support me]({RiasBot.Patreon})\n" +
                                        $"[Vote on DBL](https://discordbots.org/bot/{Context.Client.CurrentUser.Id})", true);
                embed.WithThumbnailUrl(Context.Client.CurrentUser.GetRealAvatarUrl());

                Context.Client.CurrentUser.GetAvatarUrl();
                
                embed.WithFooter("© 2018 Copyright: Koneko");

                await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task UserInfo([Remainder] IGuildUser user = null)
            {
                user = user ?? (IGuildUser)Context.User;

                var activity = user.Activity?.Name;
                var activityType = user.Activity?.Type;

                switch (activityType)
                {
                    case ActivityType.Playing:
                        activity = "Playing " + activity;
                        break;
                    case ActivityType.Streaming:
                        activity = "Streaming " + activity;
                        break;
                    case ActivityType.Listening:
                        activity = "Listening to " + activity;
                        break;
                    case ActivityType.Watching:
                        activity = "Watching " + activity;
                        break;
                }
                var joinedServer = user.JoinedAt?.UtcDateTime.ToUniversalTime().ToString("dd MMM yyyy hh:mm tt");
                var accountCreated = user.CreatedAt.UtcDateTime.ToUniversalTime().ToString("dd MMM yyyy hh:mm tt");
                
                var roleIndex = 0;
                var getUserRoles = user.RoleIds;
                var userRoles = new string[getUserRoles.Count - 1];
                var userRolesPositions = new int[getUserRoles.Count - 1];

                foreach (var role in getUserRoles)
                {
                    var r = Context.Guild.GetRole(role);
                    if (roleIndex < 10)
                    {
                        if (r.Id != Context.Guild.EveryoneRole.Id)
                        {
                            userRoles[roleIndex] = r.Name;
                            userRolesPositions[roleIndex] = r.Position;
                            roleIndex++;
                        }
                    }
                }
                Array.Sort(userRolesPositions, userRoles);
                Array.Reverse(userRoles);

                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.AddField("Name", user, true).AddField("Nickname", user.Nickname ?? "-", true);
                embed.AddField("Activity", activity ?? "-", true).AddField("ID", user.Id, true);
                embed.AddField("Status", user.Status, true).AddField("Joined Server", joinedServer, true);
                embed.AddField("Joined Discord", accountCreated, true).AddField($"Roles ({roleIndex})",
                    (roleIndex == 0) ? "-" : String.Join("\n", userRoles), true);
                embed.WithThumbnailUrl(user.GetRealAvatarUrl(1024));

                await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ServerInfo()
            {
                var guild = (SocketGuild)Context.Guild;
                var users = await Context.Guild.GetUsersAsync().ConfigureAwait(false);
                var owner = await Context.Guild.GetOwnerAsync().ConfigureAwait(false);
                var textChannels = await Context.Guild.GetTextChannelsAsync().ConfigureAwait(false);
                var voiceChannels = await Context.Guild.GetVoiceChannelsAsync().ConfigureAwait(false);
                var onlineUsers = 0;
                var bots = 0;
                var emotes = "";
                var features = string.Join(", ", guild.Features);

                foreach (var getUser in users)
                {
                    if (getUser.IsBot) bots++;
                    if (getUser.Status.ToString() == "Online" || getUser.Status.ToString() == "Idle" || getUser.Status.ToString() == "DoNotDisturb")
                        onlineUsers++;
                }
                var serverCreated = Context.Guild.CreatedAt.UtcDateTime.ToUniversalTime().ToString("dd MMM yyyy hh:mm tt");
                var guildEmotes = Context.Guild.Emotes;
                foreach (var emote in guildEmotes)
                {
                    if ((emotes + guildEmotes).Length <= 1024)
                    {
                        emotes += emote.ToString();
                    }
                }
                if (string.IsNullOrEmpty(emotes))
                    emotes = "-";
                if (string.IsNullOrEmpty(features))
                    features = "-";
                
                var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                embed.WithTitle(Context.Guild.Name);
                embed.AddField("ID", Context.Guild.Id.ToString(), true).AddField("Owner", $"{owner?.Username}#{owner?.Discriminator}", true);
                embed.AddField("Members", guild.MemberCount, true).AddField("Currently online", onlineUsers, true);
                embed.AddField("Bots", bots, true).AddField("Created at", serverCreated, true);
                embed.AddField("Text channels", textChannels.Count, true).AddField("Voice channels", voiceChannels.Count, true);
                embed.AddField("AFK channel", guild.AFKChannel?.Name ?? "-", true).AddField("Region", Context.Guild.VoiceRegionId, true);
                embed.AddField("Verification level", guild.VerificationLevel.ToString(), true).AddField($"Features ({guild.Features.Count})", features, true);
                embed.AddField($"Custom Emotes ({Context.Guild.Emotes.Count})", emotes);
                embed.WithThumbnailUrl(Context.Guild.IconUrl);
                if (!string.IsNullOrEmpty(guild.SplashUrl))
                    embed.WithImageUrl(guild.SplashUrl);

                await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            public async Task ShardsInfo()
            {
                var shards = _client.Shards;
                var shardsConnected = shards.Count(x => x.ConnectionState == ConnectionState.Connected);
                var shardsConnectionState = new List<string>();

                foreach (var shard in shards)
                {
                    switch (shard.ConnectionState)
                    {
                        case ConnectionState.Connected:
                            shardsConnectionState.Add($"Shard #{shard.ShardId} is connected");
                            break;
                        case ConnectionState.Connecting:
                            shardsConnectionState.Add($"Shard #{shard.ShardId} is connecting");
                            break;
                        case ConnectionState.Disconnecting:
                            shardsConnectionState.Add($"Shard #{shard.ShardId} is disconnecting");
                            break;
                        case ConnectionState.Disconnected:
                            shardsConnectionState.Add($"Shard #{shard.ShardId} is disconnected");
                            break;
                    }
                }
                var pager = new PaginatedMessage
                {
                    Title = $"Shards info: {shardsConnected} shards connected from {shards.Count} shards",
                    Color = new Color(RiasBot.GoodColor),
                    Pages = shardsConnectionState,
                    Options = new PaginatedAppearanceOptions
                    {
                        ItemsPerPage = 15,
                        Timeout = TimeSpan.FromMinutes(1),
                        DisplayInformationIcon = false,
                        JumpDisplayOptions = JumpDisplayOptions.Never
                    }

                };
                await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task UserId([Remainder] IGuildUser user = null)
            {
                if (user is null)
                    user = (IGuildUser)Context.User;

                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} the ID of {user} is {Format.Code(user.Id.ToString())}").ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ChannelId()
            {
                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} the ID of this channel is {Format.Code(Context.Channel.Id.ToString())}").ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ServerId()
            {

                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} the ID of this server is {Format.Code(Context.Guild.Id.ToString())}").ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task UserAvatar([Remainder] IGuildUser user = null)
            {
                if (user == null)
                    user = (IGuildUser)Context.User;

                var embed = new EmbedBuilder();
                embed.WithColor(RiasBot.GoodColor);
                embed.WithAuthor($"{user}", null, user.GetRealAvatarUrl(1024));
                embed.WithImageUrl(user.GetRealAvatarUrl(1024));

                await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ServerIcon()
            {
                var embed = new EmbedBuilder();
                embed.WithColor(RiasBot.GoodColor);
                embed.WithImageUrl(Context.Guild.IconUrl + "?size=1024");

                await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task WhoIsPlaying([Remainder]string game)
            {
                game = game.ToLowerInvariant();
                var playingUsers = new List<UserActivity>();
                var guildUsers = await Context.Guild.GetUsersAsync().ConfigureAwait(false);
                foreach (var guildUser in guildUsers)
                {
                    var activityName = guildUser.Activity?.Name;
                    if (!String.IsNullOrEmpty(activityName))
                        if (activityName.ToLowerInvariant().StartsWith(game))
                        {
                            var userActivity = new UserActivity
                            {
                                Username = guildUser.ToString(),
                                ActivityName = activityName
                            };
                            playingUsers.Add(userActivity);
                        }
                }
                if (playingUsers.Count != 0)
                {
                    var playingUsersList = new List<string>();

                    var groupPlayingUsers = playingUsers.OrderBy(x => x.Username).GroupBy(y => y.ActivityName);
                    foreach (var group in groupPlayingUsers)
                    {
                        playingUsersList.Add($"•{Format.Bold(group.Key)}");
                        foreach (var subGroup in group)
                        {
                            playingUsersList.Add($"\t~>{subGroup.Username}");
                        }
                    }
                    var pager = new PaginatedMessage
                    {
                        Title = $"Users who play {game}",
                        Color = new Color(RiasBot.GoodColor),
                        Pages = playingUsersList,
                        Options = new PaginatedAppearanceOptions
                        {
                            ItemsPerPage = 15,
                            Timeout = TimeSpan.FromMinutes(1),
                            DisplayInformationIcon = false,
                            JumpDisplayOptions = JumpDisplayOptions.Never
                        }

                    };
                    await _is.SendPaginatedMessageAsync((ShardedCommandContext)Context, pager);
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync($"No users are playing {game}.").ConfigureAwait(false);
                }
            }
        }

        public static string GetTimeString(TimeSpan timeSpan)
        {
            var days = timeSpan.Days;
            var hoursInt = timeSpan.Hours;
            var minutesInt = timeSpan.Minutes;
            var secondsInt = timeSpan.Seconds;

            var hours = hoursInt.ToString();
            var minutes = minutesInt.ToString();
            var seconds = secondsInt.ToString();

            if (hoursInt < 10)
                hours = "0" + hours;
            if (minutesInt < 10)
                minutes = "0" + minutes;
            if (secondsInt < 10)
                seconds = "0" + seconds;

            return $"{days} days {hours}:{minutes}:{seconds}";
        }

        public class UserActivity
        {
            public string Username { get; set; }
            public string ActivityName { get; set; }
        }
    }
}
