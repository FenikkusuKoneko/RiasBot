using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Database.Entities;
using Rias.Core.Extensions;
using Rias.Core.Implementation;

namespace Rias.Core.Modules.Bot
{
    public partial class BotModule
    {
        [Name("Database")]
        public class DatabaseSubmodule : RiasModule
        {
            public DatabaseSubmodule(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }
            
            [Command("delete"), OwnerOnly]
            public async Task DeleteAsync([Remainder] DiscordUser user)
            {
                await ReplyConfirmationAsync(Localization.BotDeleteDialog, user.FullName());
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.BotDeleteCanceled);
                    return;
                }

                var userDb = await DbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (userDb != null)
                    DbContext.Remove(userDb);
                
                var waifusDbList = await DbContext.GetListAsync<WaifusEntity>(x => x.UserId == user.Id);
                if (waifusDbList.Count != 0)
                    DbContext.RemoveRange(waifusDbList);
                
                var profileDb = await DbContext.Profile.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (profileDb != null)
                    DbContext.Remove(profileDb);
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserDeleted, user.FullName());
            }
            
            [Command("database"), OwnerOnly]
            public async Task DatabaseAsync([Remainder] DiscordUser user)
            {
                var userDb = await DbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (userDb is null)
                {
                    await ReplyErrorAsync(Localization.BotUserNotInDatabase);
                    return;
                }

                var mutualGuilds = user is DiscordMember cachedUser ? cachedUser.GetMutualGuilds(RiasBot).Count : 0;
                
                var embed = new DiscordEmbedBuilder()
                    .WithColor(RiasUtilities.ConfirmColor)
                    .WithAuthor(user.FullName(), user.GetAvatarUrl(ImageFormat.Auto))
                    .AddField(GetText(Localization.CommonId), user.Id.ToString(), true)
                    .AddField(GetText(Localization.GamblingHearts), $"{userDb.Currency} {Credentials.Currency}", true)
                    .AddField(GetText(Localization.XpGlobalLevel), RiasUtilities.XpToLevel(userDb.Xp, 30).ToString(), true)
                    .AddField(GetText(Localization.XpGlobalXp), userDb.Xp.ToString(), true)
                    .AddField(GetText(Localization.BotIsBlacklisted), userDb.IsBlacklisted.ToString(), true)
                    .AddField(GetText(Localization.BotIsBanned), userDb.IsBanned.ToString(), true)
                    .AddField(GetText(Localization.BotMutualGuilds), mutualGuilds.ToString(), true)
                    .WithImageUrl(user.GetAvatarUrl(ImageFormat.Auto));

                await ReplyAsync(embed);
            }
            
            [Command("blacklist"), OwnerOnly]
            public async Task BlacklistAsync([Remainder] DiscordUser user)
            {
                await ReplyConfirmationAsync(Localization.BotBlacklistDialog);
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.BotBlacklistCanceled);
                    return;
                }

                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                userDb.IsBlacklisted = true;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserBlacklisted, user.FullName());
            }
            
            [Command("removeblacklist"), OwnerOnly]
            public async Task RemoveBlacklistAsync([Remainder] DiscordUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                userDb.IsBlacklisted = false;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserBlacklistRemoved, user.FullName());
            }
            
            [Command("botban"), OwnerOnly]
            public async Task BotBanAsync([Remainder] DiscordUser user)
            {
                await ReplyConfirmationAsync(Localization.BotBotBanDialog);
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived?.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.BotBotBanCanceled);
                    return;
                }
                
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                userDb.IsBanned = true;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserBotBanned, user.FullName());
            }
            
            [Command("removebotban"), OwnerOnly]
            public async Task RemoveBotBanAsync([Remainder] DiscordUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                userDb.IsBanned = false;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserBotBanRemoved, user.FullName());
            }
        }
    }
}