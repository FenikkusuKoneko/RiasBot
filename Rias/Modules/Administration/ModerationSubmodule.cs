using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Qmmands;
using Rias.Attributes;
using Rias.Commons;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Modules.Administration
{
    public partial class AdministrationModule
    {
        [Name("Moderation")]
        public class ModerationSubmodule : RiasModule
        {
            public ModerationSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
            
            [Command("kick")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.KickMembers)]
            [BotPermission(Permissions.KickMembers)]
            public async Task KickAsync(DiscordMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotKickOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAboveMe);
                    return;
                }
                
                if (((DiscordMember)Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAbove);
                    return;
                }

                await SendMessageAsync(member, Localization.AdministrationMemberKicked, Localization.AdministrationKickedFrom, Localization.AdministrationMemberWasKicked, reason);
                await member.RemoveAsync(reason);
            }
            
            [Command("ban")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.BanMembers)]
            [BotPermission(Permissions.BanMembers)]
            public async Task BanAsync(DiscordMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotBanOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAboveMe);
                    return;
                }
                
                if (((DiscordMember)Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAbove);
                    return;
                }

                await SendMessageAsync(member, Localization.AdministrationMemberBanned, Localization.AdministrationBannedFrom, Localization.AdministrationMemberWasBanned, reason);
                await member.BanAsync(reason: reason);
            }
            
            [Command("softban")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.KickMembers)]
            [BotPermission(Permissions.KickMembers | Permissions.BanMembers)]
            public async Task SoftBanAsync(DiscordMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotSoftbanOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAboveMe);
                    return;
                }
                
                if (((DiscordMember)Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAbove);
                    return;
                }

                await SendMessageAsync(member, Localization.AdministrationMemberSoftBanned, Localization.AdministrationKickedFrom, Localization.AdministrationMemberWasSoftBanned, reason);
                await member.BanAsync(7, reason);
                await member.UnbanAsync();
            }
            
            [Command("pruneban")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.BanMembers)]
            [BotPermission(Permissions.BanMembers)]
            public async Task PruneBanAsync(DiscordMember member, [Remainder] string? reason = null)
            {
                if (member.Id == Context.User.Id)
                    return;

                if (member.Id == Context.Guild!.Owner.Id)
                {
                    await ReplyErrorAsync(Localization.AdministrationCannotPrunebanOwner);
                    return;
                }

                if (Context.CurrentMember!.CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAboveMe);
                    return;
                }
                
                if (((DiscordMember)Context.User).CheckHierarchy(member) <= 0)
                {
                    await ReplyErrorAsync(Localization.AdministrationMemberAbove);
                    return;
                }

                await SendMessageAsync(member, Localization.AdministrationMemberBanned, Localization.AdministrationBannedFrom, Localization.AdministrationMemberWasBanned, reason);
                await member.BanAsync(7, reason);
            }
            
            [Command("prune")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.ManageMessages)]
            [BotPermission(Permissions.ManageMessages)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            [Priority(2)]
            public async Task PruneAsync(int amount = 100)
            {
                if (amount < 1)
                    return;
                if (amount < 100)
                    amount++;
                else
                    amount = 100;

                var messages = (await Context.Channel.GetMessagesAsync(amount))
                    .Where(m => DateTime.UtcNow.Subtract(m.CreationTimestamp.UtcDateTime).Days < 14)
                    .ToList();

                if (messages.Count != 0)
                    await Context.Channel.DeleteMessagesAsync(messages);
                else
                    await ReplyErrorAsync(Localization.AdministrationPruneLimit);
            }

            [Command("prune")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.ManageMessages)]
            [BotPermission(Permissions.ManageMessages)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            [Priority(1)]
            public async Task PruneAsync(int amount, DiscordMember member)
                => await PruneUserMessagesAsync(member, amount);

            [Command("prune")]
            [Context(ContextType.Guild)]
            [MemberPermission(Permissions.ManageMessages)]
            [BotPermission(Permissions.ManageMessages)]
            [Cooldown(1, 5, CooldownMeasure.Seconds, BucketType.Guild)]
            [Priority(0)]
            public async Task PruneAsync(DiscordMember member, int amount = 100)
                => await PruneUserMessagesAsync(member, amount);

            private async Task PruneUserMessagesAsync(DiscordMember member, int amount)
            {
                if (amount < 1)
                    return;
                if (member.Id == Context.User.Id && amount < 100)
                    amount++;
                else if (amount > 100)
                    amount = 100;

                var messages = (await Context.Channel.GetMessagesAsync())
                    .Where(m => m.Author.Id == member.Id && DateTime.UtcNow.Subtract(m.CreationTimestamp.UtcDateTime).Days < 14)
                    .Take(amount)
                    .ToList();

                if (messages.Count != 0)
                {
                    if (Context.User.Id != member.Id)
                        messages.Add(Context.Message);
                    await Context.Channel.DeleteMessagesAsync(messages);
                }
                else
                {
                    await ReplyErrorAsync(Localization.AdministrationPruneLimit);
                }
            }
            
            private async Task SendMessageAsync(DiscordMember member, string moderationType, string fromWhere, string confirmation, string? reason)
            {
                var embed = new DiscordEmbedBuilder
                    {
                        Color = RiasUtilities.ErrorColor,
                        Title = GetText(moderationType)
                    }.WithThumbnail(member.GetAvatarUrl(ImageFormat.Auto))
                    .AddField(GetText(Localization.CommonMember), member.FullName(), true)
                    .AddField(GetText(Localization.CommonId), member.Id.ToString(), true)
                    .AddField(GetText(Localization.AdministrationModerator), Context.User.FullName(), true);

                if (!string.IsNullOrEmpty(reason))
                    embed.AddField(GetText(Localization.CommonReason), reason, true);

                var channel = Context.Channel;
                var guildDb = await DbContext.GetOrAddAsync(x => x.GuildId == Context.Guild!.Id, () => new GuildsEntity { GuildId = Context.Guild!.Id });
                var modLogChannel = Context.Guild!.GetChannel(guildDb.ModLogChannelId);
                if (modLogChannel != null)
                {
                    var preconditions = Context.CurrentMember!.PermissionsIn(modLogChannel);
                    if (preconditions.HasPermission(Permissions.AccessChannels) && preconditions.HasPermission(Permissions.SendMessages))
                    {
                        await ReplyConfirmationAsync(confirmation, member.FullName(), modLogChannel.Mention);
                        channel = modLogChannel;
                    }
                }

                await channel.SendMessageAsync(embed);

                var reasonEmbed = new DiscordEmbedBuilder
                {
                    Color = RiasUtilities.ErrorColor,
                    Description = GetText(fromWhere, Context.Guild.Name)
                };

                if (!string.IsNullOrEmpty(reason))
                    reasonEmbed.AddField(GetText(Localization.CommonReason), reason, true);

                try
                {
                    if (!member.IsBot)
                        await member.SendMessageAsync(embed: reasonEmbed);
                }
                catch
                {
                    // the user blocked the messages from the guild users
                }
            }
        }
    }
}