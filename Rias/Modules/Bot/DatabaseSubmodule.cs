using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Rias.Attributes;
using Rias.Database.Entities;
using Rias.Extensions;
using Rias.Implementation;

namespace Rias.Modules.Bot
{
    public partial class BotModule
    {
        [Name("Database")]
        public class DatabaseSubmodule : RiasModule
        {
            public DatabaseSubmodule(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            [Command("delete")]
            [OwnerOnly]
            public async Task DeleteAsync([Remainder] DiscordUser user)
            {
                var componentInteractionArgs = await SendConfirmationButtonsAsync(Localization.BotDeleteDialog, user.FullName());
                if (componentInteractionArgs is null)
                    return;

                var userDb = await DbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (userDb != null)
                    DbContext.Remove(userDb);
                
                var waifusDbList = await DbContext.GetListAsync<WaifuEntity>(x => x.UserId == user.Id);
                if (waifusDbList.Count != 0)
                    DbContext.RemoveRange(waifusDbList);
                
                var profileDb = await DbContext.Profile.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (profileDb != null)
                    DbContext.Remove(profileDb);
                
                await DbContext.SaveChangesAsync();
                await ButtonsActionModifyDescriptionAsync(componentInteractionArgs.Value.Result.Message, Localization.BotUserDeleted, user.FullName());
            }

            [Command("database", "db")]
            [OwnerOnly]
            public async Task DatabaseAsync([Remainder] DiscordUser user)
            {
                var userDb = await DbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (userDb is null)
                {
                    await ReplyErrorAsync(Localization.BotUserNotInDatabase, user.FullName());
                    return;
                }

                var mutualGuilds = user is DiscordMember cachedUser ? cachedUser.GetMutualGuilds(RiasBot).Count() : 0;

#if RELEASE
                var currency = GetText(Localization.GamblingCurrency);
#else
                var currency = GetText(Localization.GamblingHearts);
#endif
                
                var embed = new DiscordEmbedBuilder()
                    .WithColor(RiasUtilities.ConfirmColor)
                    .WithAuthor(user.FullName(), user.GetAvatarUrl(ImageFormat.Auto))
                    .AddField(GetText(Localization.CommonId), user.Id.ToString(), true)
                    .AddField(currency, $"{userDb.Currency} {Configuration.Currency}", true)
                    .AddField(GetText(Localization.XpGlobalLevel), RiasUtilities.XpToLevel(userDb.Xp, 30).ToString(), true)
                    .AddField(GetText(Localization.XpGlobalXp), userDb.Xp.ToString(), true)
                    .AddField(GetText(Localization.BotIsBanned), userDb.IsBanned.ToString(), true)
                    .AddField(GetText(Localization.BotMutualGuilds), mutualGuilds.ToString(), true)
                    .WithImageUrl(user.GetAvatarUrl(ImageFormat.Auto));

                await ReplyAsync(embed);
            }

            [Command("botban")]
            [OwnerOnly]
            public async Task BotBanAsync([Remainder] DiscordUser user)
            {
                var componentInteractionArgs = await SendConfirmationButtonsAsync(Localization.BotBotBanDialog, user.FullName());
                if (componentInteractionArgs is null)
                    return;

                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UserEntity { UserId = user.Id });
                userDb.IsBanned = true;
                
                await DbContext.SaveChangesAsync();
                await ButtonsActionModifyDescriptionAsync(componentInteractionArgs.Value.Result.Message, Localization.BotUserBotBanned, user.FullName());
            }

            [Command("removebotban")]
            [OwnerOnly]
            public async Task RemoveBotBanAsync([Remainder] DiscordUser user)
            {
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new UserEntity { UserId = user.Id });
                userDb.IsBanned = false;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync(Localization.BotUserBotBanRemoved, user.FullName());
            }
        }
    }
}