using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Commons;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Moderation")]
        public class ModerationSubmodule : RiasModule
        {
            public ModerationSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("kick"), Context(ContextType.Guild),
             UserPermission(Permission.KickMembers), BotPermission(Permission.KickMembers)]
            public async Task KickAsync(CachedMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotKickOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAbove);
                    return;
                }

                await SendMessageAsync(member, Localization.AdministrationUserKicked, Localization.AdministrationKickedFrom, reason);
                await member.KickAsync();
            }
            
            [Command("ban"), Context(ContextType.Guild),
             UserPermission(Permission.BanMembers), BotPermission(Permission.BanMembers)]
            public async Task BanAsync(CachedMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotBanOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAbove);
                    return;
                }

                await SendMessageAsync(member, Localization.AdministrationUserBanned, Localization.AdministrationBannedFrom, reason);
                await member.BanAsync(reason);
            }
            
            [Command("softban"), Context(ContextType.Guild),
             UserPermission(Permission.KickMembers), BotPermission(Permission.KickMembers | Permission.BanMembers)]
            public async Task SoftBanAsync(CachedMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotSoftbanOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAbove);
                    return;
                }

                await SendMessageAsync(member, Localization.AdministrationUserSoftBanned, Localization.AdministrationKickedFrom, reason);
                await member.BanAsync(reason, 7);
                await member.UnbanAsync();
            }
            
            [Command("pruneban"), Context(ContextType.Guild),
             UserPermission(Permission.BanMembers), BotPermission(Permission.BanMembers)]
            public async Task PruneBanAsync(CachedMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.OwnerId)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotPrunebanOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAboveMe);
                    return;
                }
                
                if (((CachedMember) Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationUserAbove);
                    return;
                }

                await SendMessageAsync(member, Localization.AdministrationUserBanned, Localization.AdministrationBannedFrom, reason);
                await member.BanAsync(reason, 7);
            }
            
            [Command("prune"), Context(ContextType.Guild),
             UserPermission(Permission.ManageMessages), BotPermission(Permission.ManageMessages),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(2)]
            public async Task PruneAsync(int amount = 100)
            {
                var channel = (CachedTextChannel) Context.Channel;

                if (amount < 1)
                    return;
                if (amount < 100)
                    amount++;
                else
                    amount = 100;

                var messages = (await channel.GetMessagesAsync(amount))
                    .Where(m => DateTimeOffset.UtcNow.Subtract(m.CreatedAt.ToUniversalTime()).Days < 14)
                    .Select(x => x.Id)
                    .ToList();

                if (messages.Count != 0)
                {
                    await channel.DeleteMessagesAsync(messages);
                }
                else
                    await ReplyErrorAsync(Localization.AdministrationPruneLimit);
            }

            [Command("prune"), Context(ContextType.Guild),
             UserPermission(Permission.ManageMessages), BotPermission(Permission.ManageMessages),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(1)]
            public async Task PruneAsync(int amount, CachedMember member)
                => await PruneUserMessagesAsync(member, amount);

            [Command("prune"), Context(ContextType.Guild),
             UserPermission(Permission.ManageMessages), BotPermission(Permission.ManageMessages),
             Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild),
             Priority(0)]
            public async Task PruneAsync(CachedMember member, int amount = 100)
                => await PruneUserMessagesAsync(member, amount);

            private async Task PruneUserMessagesAsync(CachedMember member, int amount)
            {
                var channel = (CachedTextChannel) Context.Channel;

                if (amount < 1)
                    return;
                if (amount < 100)
                    amount++;
                else
                    amount = 100;

                var messages = (await channel.GetMessagesAsync())
                    .Where(m => m.Author.Id == member.Id && DateTimeOffset.UtcNow.Subtract(m.CreatedAt.ToUniversalTime()).Days < 14)
                    .Take(amount)
                    .Select(x => x.Id)
                    .ToList();

                if (messages.Count != 0)
                {
                    if (Context.User.Id != member.Id)
                        messages.Add(Context.Message.Id);
                    await channel.DeleteMessagesAsync(messages);
                }
                else
                {
                    await ReplyErrorAsync(Localization.AdministrationPruneLimit);
                }
            }
            
            private async Task SendMessageAsync(CachedMember member, string moderationType, string fromWhere, string? reason)
            {
                var embed = new LocalEmbedBuilder
                    {
                        Color = RiasUtilities.ErrorColor,
                        Title = GetText(moderationType),
                        ThumbnailUrl = member.GetAvatarUrl()
                    }.AddField(GetText(Localization.CommonUser), member, true)
                    .AddField(GetText(Localization.CommonId), member.Id, true)
                    .AddField(GetText(Localization.AdministrationModerator), Context.User, true);

                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText(Localization.CommonReason), reason, true);

                var channel = Context.Channel;
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity {GuildId = Context.Guild!.Id});
                var modLogChannel = Context.Guild!.GetTextChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentMember!.GetPermissionsFor(modLogChannel);
                    if (preconditions.ViewChannel && preconditions.SendMessages)
                        channel = modLogChannel;
                }

                if (channel.Id != Context.Channel.Id)
                    await Context.Message.AddReactionAsync(new LocalEmoji("✅"));

                await channel.SendMessageAsync(embed);

                var reasonEmbed = new LocalEmbedBuilder
                {
                    Color = RiasUtilities.ErrorColor,
                    Description = GetText(fromWhere, Context.Guild.Name)
                };

                if (!string.IsNullOrEmpty(reason))
                    reasonEmbed.AddField(GetText(Localization.CommonReason), reason, true);

                try
                {
                    if (!member.IsBot)
                        await member.SendMessageAsync(reasonEmbed);
                }
                catch
                {
                    // the user blocked the messages from the guild users
                }
            }
        }
    }
}