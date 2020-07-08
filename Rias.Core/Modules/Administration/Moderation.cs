using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    public partial class Administration
    {
        [Name("Moderation")]
        public class Moderation : RiasModule
        {
            public Moderation(IServiceProvider services) : base(services)
            {
            }

            [Command("kick"), Context(ContextType.Guild),
             UserPermission(GuildPermission.KickMembers), BotPermission(GuildPermission.KickMembers)]
            public async Task KickAsync(SocketGuildUser user, [Remainder] string? reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;

                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync("CannotKickOwner");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAboveMe");
                    return;
                }
                
                if (((SocketGuildUser) Context.User).CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                await SendMessageAsync(user, "UserKicked", "KickedFrom", reason);
                await user.KickAsync();
            }

            [Command("ban"), Context(ContextType.Guild),
             UserPermission(GuildPermission.BanMembers), BotPermission(GuildPermission.BanMembers)]
            public async Task BanAsync(SocketGuildUser user, [Remainder] string? reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;

                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync("CannotBanOwner");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAboveMe");
                    return;
                }
                
                if (((SocketGuildUser) Context.User).CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                await SendMessageAsync(user, "UserBanned", "BannedFrom", reason);
                await Context.Guild.AddBanAsync(user);
            }

            [Command("softban"), Context(ContextType.Guild),
             UserPermission(GuildPermission.KickMembers), BotPermission(GuildPermission.KickMembers | GuildPermission.BanMembers)]
            public async Task SoftBanAsync(SocketGuildUser user, [Remainder] string? reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;

                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync("CannotSoftbanOwner");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAboveMe");
                    return;
                }
                
                if (((SocketGuildUser) Context.User).CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                await SendMessageAsync(user, "UserSoftBanned", "KickedFrom", reason);
                await Context.Guild.AddBanAsync(user, 7);
                await Context.Guild.RemoveBanAsync(user);
            }

            [Command("pruneban"), Context(ContextType.Guild),
             UserPermission(GuildPermission.BanMembers), BotPermission(GuildPermission.BanMembers)]
            public async Task PruneBanAsync(SocketGuildUser user, [Remainder] string? reason = null)
            {
                if (user.Id == Context.User.Id)
                    return;

                if (user.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync("CannotPrunebanOwner");
                    return;
                }

                if (Context.CurrentGuildUser!.CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAboveMe");
                    return;
                }
                
                if (((SocketGuildUser) Context.User).CheckHierarchy(user) <= 0)
                {
                    await ReplyErrorAsync("UserAbove");
                    return;
                }

                await SendMessageAsync(user, "UserBanned", "BannedFrom", reason);
                await Context.Guild.AddBanAsync(user, 7);
            }

            [Command("prune"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageMessages), BotPermission(GuildPermission.ManageMessages),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(2)]
            public async Task PruneAsync(int amount = 100)
            {
                var channel = (SocketTextChannel) Context.Channel;

                if (amount < 1)
                    return;
                if (amount < 100)
                    amount++;
                else
                    amount = 100;

                var messages = (await channel.GetMessagesAsync(amount).FlattenAsync())
                    .Where(m => DateTimeOffset.UtcNow.Subtract(m.CreatedAt.ToUniversalTime()).Days < 14)
                    .ToList();

                if (messages.Count != 0)
                {
                    await channel.DeleteMessagesAsync(messages);
                }
                else
                    await ReplyErrorAsync("PruneLimit");
            }

            [Command("prune"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageMessages), BotPermission(GuildPermission.ManageMessages),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(1)]
            public async Task PruneAsync(int amount, SocketGuildUser user)
                => await PruneUserMessagesAsync(user, amount);

            [Command("prune"), Context(ContextType.Guild),
             UserPermission(GuildPermission.ManageMessages), BotPermission(GuildPermission.ManageMessages),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(0)]
            public async Task PruneAsync(SocketGuildUser user, int amount = 100)
                => await PruneUserMessagesAsync(user, amount);

            private async Task PruneUserMessagesAsync(SocketGuildUser user, int amount)
            {
                var channel = (SocketTextChannel) Context.Channel;

                if (amount < 1)
                    return;
                if (amount < 100)
                    amount++;
                else
                    amount = 100;

                var messages = (await channel.GetMessagesAsync().FlattenAsync())
                    .Where(m => m.Author.Id == user.Id && DateTimeOffset.UtcNow.Subtract(m.CreatedAt.ToUniversalTime()).Days < 14)
                    .Take(amount)
                    .ToList();

                if (messages.Count != 0)
                {
                    if (Context.User.Id != user.Id)
                        messages.Add(Context.Message);
                    await channel.DeleteMessagesAsync(messages);
                }
                else
                {
                    await ReplyErrorAsync("PruneLimit");
                }
            }

            private async Task SendMessageAsync(SocketGuildUser user, string moderationType, string fromWhere, string? reason)
            {
                var embed = new EmbedBuilder
                    {
                        Color = RiasUtils.ErrorColor,
                        Title = GetText(moderationType),
                        ThumbnailUrl = user.GetRealAvatarUrl()
                    }.AddField(GetText("#Common_User"), user, true)
                    .AddField(GetText("#Common_Id"), user.Id.ToString(), true)
                    .AddField(GetText("Moderator"), Context.User, true);

                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText("#Common_Reason"), reason, true);

                var channel = Context.Channel;
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                var modLogChannel = Context.Guild!.GetTextChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentGuildUser!.GetPermissions(modLogChannel);
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
                    reasonEmbed.AddField(GetText("#Common_Reason"), reason, true);

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