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

namespace RiasBot.Modules.Utility
{
    public partial class Utility
    {
        public class InfoCommands : RiasModule
        {
            private readonly DiscordShardedClient _client;
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            private readonly InteractiveService _is;

            public InfoCommands(DiscordShardedClient client, CommandHandler ch, CommandService service, InteractiveService interactiveService)
            {
                _client = client;
                _ch = ch;
                _service = service;
                _is = interactiveService;
            }

            [RiasCommand]
            [@Alias]
            [@Remarks]
            [Description]
            public async Task Stats()
            {
                var author = _client.GetUser(RiasBot.konekoID);
                var guilds = await Context.Client.GetGuildsAsync().ConfigureAwait(false);
                int shard = 0;
                if (Context.Guild != null)
                    shard = _client.GetShardIdFor(Context.Guild) + 1;

                int textChannels = 0;
                int voiceChannels = 0;
                int users = 0;

                foreach (SocketGuild guild in guilds)
                {
                    textChannels += guild.TextChannels.Count;
                    voiceChannels += guild.VoiceChannels.Count;
                    users += guild.MemberCount;
                }

                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);

                embed.WithAuthor("Rias Bot " + RiasBot.version, Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto));
                embed.AddField("Author", author?.ToString() ?? RiasBot.author, true).AddField("Bot ID", Context.Client.CurrentUser.Id, true);
                embed.AddField("Master ID", RiasBot.konekoID, true).AddField("Shard", $"#{shard}/{_client.Shards.Count()}", true);
                embed.AddField("In server", Context.Guild?.Name ?? "-", true).AddField("Commands Run", RiasBot.commandsRun, true);
                embed.AddField("Uptime", GetTimeString(RiasBot.upTime.Elapsed), true).AddField("Presence", $"{guilds.Count} Servers\n{textChannels} " +
                    $"Text Channels\n{voiceChannels} Voice Channels\n{users} Users", true);
                //embed.AddField("Playing Music", $"Running {musicRunning} Channels\nAFK {musicAfk} Channels", true);
                embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto));

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

                string activity = user.Activity?.Name;
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

                string joinedServer = user.JoinedAt.Value.UtcDateTime.ToUniversalTime().ToString("dd MMM yyyy hh:mm tt");
                string accountCreated = user.CreatedAt.UtcDateTime.ToUniversalTime().ToString("dd MMM yyyy hh:mm tt");

                int roleIndex = 0;
                var getUserRoles = user.RoleIds;
                string[] userRoles = new string[getUserRoles.Count - 1];
                int[] userRolesPositions = new int[getUserRoles.Count - 1];

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

                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.AddField("Name", user, true).AddField("Nickname", user.Nickname ?? "-", true);
                embed.AddField("Activity", activity ?? "-", true).AddField("ID", user.Id, true);
                embed.AddField("Status", user.Status, true).AddField("Joined Server", joinedServer, true);
                embed.AddField("Joined Discord", accountCreated, true).AddField($"Roles ({roleIndex})",
                    (roleIndex == 0) ? "-" : String.Join("\n", userRoles), true);
                embed.WithThumbnailUrl(user.RealAvatarUrl(1024));

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
                int onlineUsers = 0;
                int bots = 0;

                foreach (var getUser in users)
                {
                    if (getUser.IsBot) bots++;
                    if (getUser.Status.ToString() == "Online" || getUser.Status.ToString() == "Idle" || getUser.Status.ToString() == "DoNotDisturb")
                        onlineUsers++;
                }
                string serverCreated = Context.Guild.CreatedAt.UtcDateTime.ToUniversalTime().ToString("dd MMM yyyy hh:mm tt");

                var guildEmotes = Context.Guild.Emotes;
                string emotes = null;

                foreach (var emote in guildEmotes)
                {
                    if ((emotes + guildEmotes).Length <= 1024)
                    {
                        emotes += emote.ToString();
                    }
                }
                if (String.IsNullOrEmpty(emotes))
                    emotes = "-";

                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle(Context.Guild.Name);
                embed.AddField("ID", Context.Guild.Id.ToString(), true).AddField("Owner", $"{owner?.Username}#{owner?.Discriminator}", true).AddField("Members", guild.MemberCount, true);
                embed.AddField("Currently online", onlineUsers, true).AddField("Bots", bots, true).AddField("Created at", serverCreated, true);
                embed.AddField("Text channels", textChannels.Count, true).AddField("Voice channels", voiceChannels.Count, true).AddField("Region", Context.Guild.VoiceRegionId, true);
                embed.AddField($"Custom Emotes ({Context.Guild.Emotes.Count})", emotes);
                embed.WithImageUrl(Context.Guild.IconUrl);

                await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
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

                await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the ID of {user} is {Format.Code(user.Id.ToString())}").ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ChannelId()
            {
                await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the ID of this channel is {Format.Code(Context.Channel.Id.ToString())}").ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ServerId()
            {

                await Context.Channel.SendConfirmationEmbed($"{Context.User.Mention} the ID of this server is {Format.Code(Context.Guild.Id.ToString())}").ConfigureAwait(false);
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
                embed.WithColor(RiasBot.goodColor);
                embed.WithAuthor($"{user}", null, user.RealAvatarUrl(1024));
                embed.WithImageUrl(user.RealAvatarUrl(1024));

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
                embed.WithColor(RiasBot.goodColor);
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
                        Color = new Color(RiasBot.goodColor),
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
                    await Context.Channel.SendErrorEmbed($"No users are playing {game}.").ConfigureAwait(false);
                }
            }
        }

        public static string GetTimeString(TimeSpan timeSpan)
        {
            var days = timeSpan.Days;
            var hoursInt = timeSpan.Hours;
            var minutesInt = timeSpan.Minutes;
            var secondsInt = timeSpan.Seconds;

            string hours = hoursInt.ToString();
            string minutes = minutesInt.ToString();
            string seconds = secondsInt.ToString();

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
