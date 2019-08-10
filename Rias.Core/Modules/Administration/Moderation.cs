using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Moderation")]
        public class Moderation : RiasModule
        {
            [Command("kick"), Context(ContextType.Guild),
             UserPermission(GuildPermission.KickMembers), BotPermission(GuildPermission.KickMembers)]
            public async Task KickAsync(SocketGuildUser user, [Remainder] string reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;

                if (user.Id == Context.Guild.OwnerId)
                {
                    await ReplyErrorAsync("cannot_kick_owner");
                    return;
                }

                if (Context.CurrentGuildUser.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("user_above");
                    return;
                }

                await SendMessageAsync(user, "user_kicked", "kicked_from", reason);
                await user.KickAsync();
            }

            [Command("ban"), Context(ContextType.Guild),
             UserPermission(GuildPermission.BanMembers), BotPermission(GuildPermission.BanMembers)]
            public async Task BanAsync(SocketGuildUser user, [Discord.Commands.Remainder] string reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;

                if (user.Id == Context.Guild.OwnerId)
                {
                    await ReplyErrorAsync("cannot_ban_owner");
                    return;
                }

                if (Context.CurrentGuildUser.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("user_above");
                    return;
                }

                await SendMessageAsync(user, "user_banned", "banned_from", reason);
                await Context.Guild.AddBanAsync(user);
            }

            [Command("softban"), Context(ContextType.Guild),
             UserPermission(GuildPermission.KickMembers), BotPermission(GuildPermission.KickMembers | GuildPermission.BanMembers)]
            public async Task SoftBanAsync(SocketGuildUser user, [Discord.Commands.Remainder] string reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;

                if (user.Id == Context.Guild.OwnerId)
                {
                    await ReplyErrorAsync("cannot_softban_owner");
                    return;
                }

                if (Context.CurrentGuildUser.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("user_above");
                    return;
                }

                await SendMessageAsync(user, "user_soft_banned", "kicked_from", reason);
                await Context.Guild.AddBanAsync(user, 7);
                await Context.Guild.RemoveBanAsync(user);
            }

            [Command("pruneban"), Context(ContextType.Guild),
             UserPermission(GuildPermission.BanMembers), BotPermission(GuildPermission.BanMembers)]
            public async Task PruneBanAsync(SocketGuildUser user, [Discord.Commands.Remainder] string reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;

                if (user.Id == Context.Guild.OwnerId)
                {
                    await ReplyErrorAsync("cannot_pruneban_owner");
                    return;
                }

                if (Context.CurrentGuildUser.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("user_above");
                    return;
                }

                await SendMessageAsync(user, "user_banned", "banned_from", reason);
                await Context.Guild.AddBanAsync(user, 7);
            }

            [Command("prune"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageMessages), BotPermission(GuildPermission.ManageMessages),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            [Priority(2)]
            public async Task PruneAsync(int amount = 100)
            {
                var channel = (ITextChannel) Context.Channel;

                if (amount < 1)
                    return;
                if (amount > 100)
                    amount = 100;

                var messages = (await channel.GetMessagesAsync(amount).FlattenAsync())
                    .Where(m => DateTimeOffset.UtcNow.Subtract(m.CreatedAt.ToUniversalTime()).Days < 14)
                    .ToList();

                if (messages.Any())
                    await channel.DeleteMessagesAsync(messages);
            }

            [Command("prune"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageMessages), BotPermission(GuildPermission.ManageMessages),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            [Priority(1)]
            public async Task PruneAsync(int amount, SocketGuildUser user)
                => await PruneUserMessagesAsync(user, amount);

            [Command("prune"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageMessages), BotPermission(GuildPermission.ManageMessages),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            [Priority(0)]
            public async Task PruneAsync(SocketGuildUser user, int amount = 100)
                => await PruneUserMessagesAsync(user, amount);

            private async Task PruneUserMessagesAsync(SocketGuildUser user, int amount)
            {
                var channel = (ITextChannel) Context.Channel;

                if (amount < 1)
                    return;
                if (amount > 100)
                    amount = 100;

                var messages = (await channel.GetMessagesAsync().FlattenAsync())
                    .Where(m => m.Author.Id == user.Id && DateTimeOffset.UtcNow.Subtract(m.CreatedAt.ToUniversalTime()).Days < 14)
                    .Take(amount)
                    .ToList();

                if (messages.Any())
                {
                    if (Context.User.Id != user.Id)
                        messages.Add(Context.Message);
                    await channel.DeleteMessagesAsync(messages);
                }
                else
                {
                    await ReplyErrorAsync("prune_limit");
                }
            }

            private async Task SendMessageAsync(SocketGuildUser user, string moderationType, string fromWhere, string reason)
            {
                var guildDb = Db.Guilds.FirstOrDefault(x => x.GuildId == Context.Guild.Id);

                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ErrorColor,
                        Title = GetText(moderationType),
                        ThumbnailUrl = user.GetRealAvatarUrl()
                    }.AddField(GetText("#common_user"), user, true)
                    .AddField(GetText("#common_id"), user.Id.ToString(), true)
                    .AddField(GetText("moderator"), Context.User, true);

                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText("#common_reason"), reason);

                var channel = Context.Channel;
                var modLogChannel = Context.Guild.GetTextChannel(guildDb?.ModLogChannel ?? 0);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentGuildUser.GetPermissions(modLogChannel);
                    if (preconditions.ViewChannel && preconditions.SendMessages)
                        channel = modLogChannel;
                }

                if (channel.Id != Context.Channel.Id)
                    await Context.Message.AddReactionAsync(new Emoji("✅"));

                await channel.SendMessageAsync(embed);

                var reasonEmbed = new EmbedBuilder
                {
                    Color = RiasUtils.ErrorColor,
                    Description = GetText(fromWhere, Context.Guild.Name)
                };

                if (!string.IsNullOrEmpty(reason))
                    reasonEmbed.AddField(GetText("#common_reason"), reason);

                try
                {
                    if (!user.IsBot)
                        await user.SendMessageAsync(reasonEmbed);
                }
                catch
                {
                    // the user blocked the messages from the guild users
                }
            }
        }
    }
}