using System;
using System.Threading.Tasks;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Database.Entities;
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
            public async Task DeleteAsync([Remainder] IUser user)
            {
                await ReplyConfirmationAsync(Localization.BotDeleteDialog, user);
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived?.Message.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
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
                await ReplyConfirmationAsync(Localization.BotUserDeleted, user);
            }
            
            [Command("database"), OwnerOnly]
            public async Task DatabaseAsync([Remainder] IUser user)
            {
                var userDb = await DbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (userDb is null)
                {
                    await ReplyErrorAsync(Localization.BotUserNotInDatabase);
                    return;
                }

                var mutualGuilds = user is CachedUser cachedUser ? cachedUser.MutualGuilds.Count : 0;
                
                var embed = new LocalEmbedBuilder()
                    .WithColor(RiasUtilities.ConfirmColor)
                    .WithAuthor(user)
                    .AddField(GetText(Localization.CommonId), user.Id, true)
                    .AddField(GetText(Localization.GamblingCurrency), $"{userDb.Currency} {Credentials.Currency}", true)
                    .AddField(GetText(Localization.XpGlobalLevel), RiasUtilities.XpToLevel(userDb.Xp, 30), true)
                    .AddField(GetText(Localization.XpGlobalXp), userDb.Xp, true)
                    .AddField(GetText(Localization.BotIsBlacklisted), userDb.IsBlacklisted, true)
                    .AddField(GetText(Localization.BotIsBanned), userDb.IsBanned, true)
                    .AddField(GetText(Localization.BotMutualGuilds), mutualGuilds, true)
                    .WithImageUrl(user.GetAvatarUrl());

                await ReplyAsync(embed);
            }
            
            [Command("blacklist"), OwnerOnly]
            public async Task BlacklistAsync([Remainder] IUser user)
            {
                await ReplyConfirmationAsync(Localization.BotBlacklistDialog);
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived?.Message.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.BotBlacklistCanceled);
                    return;
                }

                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                userDb.IsBlacklisted = true;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserBlacklisted, user);
            }
            
            [Command("removeblacklist"), OwnerOnly]
            public async Task RemoveBlacklistAsync([Remainder] IUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                userDb.IsBlacklisted = false;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserBlacklistRemoved, user);
            }
            
            [Command("botban"), OwnerOnly]
            public async Task BotBanAsync([Remainder] IUser user)
            {
                await ReplyConfirmationAsync(Localization.BotBotBanDialog);
                var messageReceived = await NextMessageAsync();
                if (!string.Equals(messageReceived?.Message.Content, GetText(Localization.CommonYes), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync(Localization.BotBotBanCanceled);
                    return;
                }
                
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                userDb.IsBanned = true;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserBotBanned, user);
            }
            
            [Command("removebotban"), OwnerOnly]
            public async Task RemoveBotBanAsync([Remainder] IUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UsersEntity {UserId = user.Id});
                userDb.IsBanned = false;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserBotBanRemoved, user);
            }
        }
    }
}