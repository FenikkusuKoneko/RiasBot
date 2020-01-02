using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;
using Rias.Core.Attributes;
using Rias.Core.Database.Models;
using Rias.Core.Extensions;
using Rias.Core.Implementation;
using Rias.Interactive;

namespace Rias.Core.Modules.Bot
{
    public partial class Bot
    {
        [Name("Database")]
        public class Database : RiasModule
        {
            private readonly InteractiveService _interactive;

            public Database(IServiceProvider services) : base(services)
            {
                _interactive = services.GetRequiredService<InteractiveService>();
            }

            [Command("delete"), OwnerOnly]
            public async Task DeleteAsync([Remainder] string value)
            {
                var user = await RiasUtils.GetUserFromSocketOrRestAsync(Context.Client, value);
                if (user is null)
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }
                
                await ReplyConfirmationAsync("DeleteDialog");
                var message = await _interactive.NextMessageAsync(Context.Message);
                if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync("DeleteCanceled");
                    return;
                }

                var userDb = await DbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (userDb != null)
                    DbContext.Remove(userDb);
                
                var waifusDbList = await DbContext.GetListAsync<Waifus>(x => x.UserId == user.Id);
                if (waifusDbList.Count != 0)
                    DbContext.RemoveRange(waifusDbList);
                
                var profileDb = await DbContext.Profile.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (profileDb != null)
                    DbContext.Remove(profileDb);
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync("UserDeleted", user);
            }

            [Command("database"), OwnerOnly]
            public async Task DatabaseAsync([Remainder] string value)
            {
                var user = await RiasUtils.GetUserFromSocketOrRestAsync(Context.Client, value);
                if (user is null)
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }

                var userDb = await DbContext.Users.FirstOrDefaultAsync(x => x.UserId == user.Id);
                if (userDb is null)
                {
                    await ReplyErrorAsync("UserNotInDatabase");
                    return;
                }

                var mutualGuilds = 0;
                if (user is SocketGuildUser guildUser)
                    mutualGuilds = guildUser.MutualGuilds.Count;
                
                var embed = new EmbedBuilder()
                    .WithColor(RiasUtils.ConfirmColor)
                    .WithAuthor(user.ToString())
                    .AddField(GetText("#Common_Id"), user.Id, true)
                    .AddField(GetText("#Gambling_Currency"), $"{userDb.Currency} {Credentials.Currency}", true)
                    .AddField(GetText("#Xp_GlobalLevel"), RiasUtils.XpToLevel(userDb.Xp, 30), true)
                    .AddField(GetText("#Xp_GlobalXp"), userDb.Xp, true)
                    .AddField(GetText("IsBlacklisted"), userDb.IsBlacklisted, true)
                    .AddField(GetText("IsBanned"), userDb.IsBanned, true)
                    .AddField(GetText("MutualGuilds"), mutualGuilds, true)
                    .WithImageUrl(user.GetRealAvatarUrl());

                await ReplyAsync(embed);
            }

            [Command("blacklist"), OwnerOnly]
            public async Task BlacklistAsync([Remainder] string value)
            {
                var user = await RiasUtils.GetUserFromSocketOrRestAsync(Context.Client, value);
                if (user is null)
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }
                
                await ReplyConfirmationAsync("BlacklistDialog");
                var message = await _interactive.NextMessageAsync(Context.Message);
                if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync("BlacklistCanceled");
                    return;
                }

                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new Users {UserId = user.Id});
                userDb.IsBlacklisted = true;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync("UserBlacklisted", user);
            }
            
            [Command("removeblacklist"), OwnerOnly]
            public async Task RemoveBlacklistAsync([Remainder] string value)
            {
                var user = await RiasUtils.GetUserFromSocketOrRestAsync(Context.Client, value);
                if (user is null)
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }
                
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new Users {UserId = user.Id});
                userDb.IsBlacklisted = false;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync("UserBlacklistRemoved", user);
            }

            [Command("botban"), OwnerOnly]
            public async Task BotBanAsync([Remainder] string value)
            {
                var user = await RiasUtils.GetUserFromSocketOrRestAsync(Context.Client, value);
                if (user is null)
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }
                
                await ReplyConfirmationAsync("BotBanDialog");
                var message = await _interactive.NextMessageAsync(Context.Message);
                if (!string.Equals(message?.Content, GetText("#Common_Yes"), StringComparison.InvariantCultureIgnoreCase))
                {
                    await ReplyErrorAsync("BotBanCanceled");
                    return;
                }
                
                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new Users {UserId = user.Id});
                userDb.IsBanned = true;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync("UserBotBanned", user);
            }
            
            [Command("removebotban"), OwnerOnly]
            public async Task RemoveBotBanAsync([Remainder] string value)
            {
                var user = await RiasUtils.GetUserFromSocketOrRestAsync(Context.Client, value);
                if (user is null)
                {
                    await ReplyErrorAsync("#Administration_UserNotFound");
                    return;
                }

                var userDb = await DbContext.GetOrAddAsync(x => x.UserId == user.Id, () => new Users {UserId = user.Id});
                userDb.IsBanned = false;
                
                await DbContext.SaveChangesAsync();
                await ReplyConfirmationAsync("UserBotBanRemoved", user);
            }
        }
    }
}