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

namespace RiasBot.Modules.Utility
{
    public partial class Utility
    {
        public class InfoCommands : RiasModule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;

            public InfoCommands(CommandHandler ch, CommandService service)
            {
                _ch = ch;
                _service = service;
            }

            [RiasCommand]
            [@Alias]
            [@Remarks]
            [Description]
            public async Task Stats()
            {
                var author = await Context.Client.GetUserAsync(RiasBot.fenikkusuId).ConfigureAwait(false);
                var guilds = await Context.Client.GetGuildsAsync().ConfigureAwait(false);

                int textChannels = 0;
                int voiceChannels = 0;
                int users = 0;

                foreach (var guild in guilds)
                {
                    textChannels += (await guild.GetTextChannelsAsync().ConfigureAwait(false)).Count;
                    voiceChannels += (await guild.GetVoiceChannelsAsync().ConfigureAwait(false)).Count;
                    users += (await guild.GetUsersAsync().ConfigureAwait(false)).Count;
                }

                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);

                embed.WithAuthor("Rias Bot " + RiasBot.version, Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto));
                embed.AddField("Author", author.Username + "#" + author.Discriminator, true).AddField("Bot ID", Context.Client.CurrentUser.Id.ToString(), true);
                embed.AddField("Master ID", author.Id, true).AddField("In server", Context.Guild?.Name ?? "-", true);
                embed.AddField("Uptime", GetTimeString(RiasBot.upTime.Elapsed), true).AddField("Commands Run", RiasBot.commandsRun, true);
                embed.AddField("Memory", Math.Round((double)GC.GetTotalMemory(false) / 1024 / 1024, 2) + " Mb", true)
                    .AddField("Presence", $"{guilds.Count} Servers\n{textChannels} Text Channels\n{voiceChannels} Voice Channels\n{users} Users", true);
                embed.WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Auto));

                //continue
                await ReplyAsync("", false, embed.Build()).ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task UserInfo([Remainder] IGuildUser user = null)
            {
                if (user is null) user = (IGuildUser)Context.User;

                try
                {
                    string joinedServer = user.JoinedAt.Value.UtcDateTime.ToShortDateString()
                    + " " + user.JoinedAt.Value.UtcDateTime.ToShortTimeString()
                    .Replace("/", ".");

                    string accountCreated = user.CreatedAt.UtcDateTime.ToShortDateString()
                        + " " + user.JoinedAt.Value.UtcDateTime.ToShortTimeString()
                        .Replace("/", ".");

                    int roleIndex = 0;
                    var getUserRoles = user.RoleIds;
                    string[] userRoles = new string[getUserRoles.Count];
                    int[] userRolesPositions = new int[getUserRoles.Count];

                    foreach (var role in getUserRoles)
                    {
                        var r = Context.Guild.GetRole(role);
                        if (roleIndex < 10)
                        {
                            userRoles[roleIndex] = r.Name;
                            userRolesPositions[roleIndex] = r.Position;
                            roleIndex++;
                        }
                    }

                    Array.Sort(userRolesPositions, userRoles);
                    Array.Reverse(userRoles);

                    var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                    embed.AddField("Name", user, true).AddField("Nickname", user.Nickname ?? "-", true);
                    embed.AddField("ID", user.Id, true).AddField("Status", user.Status, true);
                    embed.AddField("Joined Server", joinedServer, true).AddField("Joined Discord", accountCreated, true);
                    embed.AddField($"Roles ({roleIndex})",
                        (roleIndex == 0) ? "-" : String.Join("\n", userRoles));
                    embed.WithThumbnailUrl(user.RealAvatarUrl() ?? user.DefaultAvatarUrl());

                    await ReplyAsync("", false, embed.Build()).ConfigureAwait(false);
                }
                catch
                {
                    await Context.Channel.SendErrorEmbed("I couldn't find the user.");
                }
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ServerInfo()
            {
                var users = await Context.Guild.GetUsersAsync();
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
                string serverCreated = Context.Guild.CreatedAt.UtcDateTime.ToShortDateString()
                    + " " + Context.Guild.CreatedAt.UtcDateTime.ToShortTimeString()
                    .Replace("/", ".");

                var guildEmotes = Context.Guild.Emotes;
                string[] emotes = new string[guildEmotes.Count];
                int emoteIndex = 0;

                foreach (var emote in guildEmotes)
                {
                    if (emote.Animated && emoteIndex < 20)
                    {
                        emotes[emoteIndex] = $"{emote.Name} <a:{emote.Name}:{emote.Id}>";
                    }
                    else
                    {
                        emotes[emoteIndex] = $"{emote.Name} <:{emote.Name}:{emote.Id}>";
                    }
                    emoteIndex++;
                }

                var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                embed.WithTitle(Context.Guild.Name);
                embed.AddField("ID", Context.Guild.Id.ToString(), true).AddField("Owner", $"{owner.Username}#{owner.Discriminator}", true).AddField("Members", users.Count, true);
                embed.AddField("Currently online", onlineUsers, true).AddField("Bots", bots, true).AddField("Created at", serverCreated, true);
                embed.AddField("Text channels", textChannels.Count, true).AddField("Voice channels", voiceChannels.Count, true).AddField("Region", Context.Guild.VoiceRegionId, true);
                embed.AddField($"Custom Emojis ({Context.Guild.Emotes.Count})", String.Join(" ", emotes));
                embed.WithImageUrl(Context.Guild.IconUrl);

                await ReplyAsync("", false, embed.Build()).ConfigureAwait(false);
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

                await ReplyAsync($"{user.Mention} the ID of {user} is {Format.Code(user.Id.ToString())}").ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ChannelId()
            {
                await ReplyAsync($"{Context.Message.Author.Mention} the ID of this channel is {Format.Code(Context.Channel.Id.ToString())}").ConfigureAwait(false);
            }

            [RiasCommand]
            [@Alias]
            [Description]
            [@Remarks]
            [RequireContext(ContextType.Guild)]
            public async Task ServerId()
            {

                await ReplyAsync($"{Context.Message.Author.Mention} the ID of this server is {Format.Code(Context.Guild.Id.ToString())}").ConfigureAwait(false);
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
                try
                {
                    embed.WithAuthor($"{user}", null, user.RealAvatarUrl(1024));
                    embed.WithImageUrl(user.RealAvatarUrl(1024));
                }
                catch
                {
                    embed.WithAuthor($"{user}", null, user.DefaultAvatarUrl());
                    embed.WithImageUrl(user.DefaultAvatarUrl());
                }

                await ReplyAsync("", false, embed.Build()).ConfigureAwait(false);
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

                await ReplyAsync("", false, embed.Build()).ConfigureAwait(false);
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
    }
}
