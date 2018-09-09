using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using RiasBot.Commons.Attributes;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Bot
{
    public partial class Bot
    {
        public class DatabaseCommands : RiasSubmodule
        {
            private readonly CommandHandler _ch;
            private readonly CommandService _service;
            private readonly DbService _db;
            private readonly DiscordShardedClient _client;
            private readonly DiscordRestClient _restClient;
            private readonly BotService _botService;
            private readonly InteractiveService _is;

            public DatabaseCommands(CommandHandler ch, CommandService service, DbService db, DiscordShardedClient client,
                DiscordRestClient restClient, BotService botService, InteractiveService interactiveService)
            {
                _ch = ch;
                _service = service;
                _db = db;
                _client = client;
                _restClient = restClient;
                _botService = botService;
                _is = interactiveService;
            }
            
            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Delete([Remainder]string user)
            {
                IUser getUser;
                if (UInt64.TryParse(user, out var id))
                {
                    getUser = await _restClient.GetUserAsync(id).ConfigureAwait(false);
                }
                else
                {
                    var userSplit = user.Split("#");
                    if (userSplit.Length == 2)
                        getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
                    else
                        getUser = null;
                }
                if (getUser is null)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't find the user.").ConfigureAwait(false);
                    return;
                }
                if (getUser is null)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} the user couldn't be found");
                    return;
                }
                if (getUser.Id == RiasBot.KonekoId)
                {
                    await Context.Channel.SendErrorMessageAsync("I will not delete you from the database, Master!");
                    return;
                }
                var confirm = await Context.Channel.SendConfirmationMessageAsync($"Are you sure you want to delete the user? Type {Format.Code("confirm")}");
                var input = await _is.NextMessageAsync((ShardedCommandContext)Context, timeout: TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                if (input != null)
                {
                    if (input.Content == "confirm")
                    {
                        using (var db = _db.GetDbContext())
                        {
                            var userDb = db.Users.Where(x => x.UserId == getUser.Id).FirstOrDefault();
                            if (userDb != null)
                            {
                                db.Remove(userDb);
                            }
                            var waifusDb = db.Waifus.Where(x => x.UserId == getUser.Id);
                            if (waifusDb != null)
                            {
                                db.RemoveRange(waifusDb);
                            }
                            var profileDb = db.Profile.Where(x => x.UserId == getUser.Id).FirstOrDefault();
                            if (profileDb != null)
                            {
                                db.Remove(profileDb);
                            }
                            if (getUser != null)
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {getUser} has been deleted from the database").ConfigureAwait(false);
                            }
                            else
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {id} has been added to the blacklist.").ConfigureAwait(false);
                            }
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    await Context.Channel.SendErrorMessageAsync("Canceled!");
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Db([Remainder]string user)
            {
                IUser getUser;
                var mutualServers = false;
                if (UInt64.TryParse(user, out var id))
                {
                    getUser = await Context.Client.GetUserAsync(id).ConfigureAwait(false);
                }
                else
                {
                    var userSplit = user.Split("#");
                    if (userSplit.Length == 2)
                        getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
                    else
                        getUser = null;
                }
                if (getUser is null)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't find the user.").ConfigureAwait(false);
                    return;
                }

                var guilds = await Context.Client.GetGuildsAsync().ConfigureAwait(false);
                mutualServers = guilds.Any(x => x.GetUserAsync(getUser.Id).GetAwaiter().GetResult() != null);

                using (var db = _db.GetDbContext())
                {
                    var userDb = db.Users.Where(x => x.UserId == getUser.Id).FirstOrDefault();
                    var xpDb = db.XpSystem.Where(x => x.UserId == getUser.Id);
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithAuthor(getUser.ToString());
                    embed.AddField("ID", getUser.Id, true).AddField("Currency", $"{userDb?.Currency} {RiasBot.Currency}", true);
                    embed.AddField("Global level", userDb?.Level, true).AddField("Global XP", userDb?.Xp, true);
                    embed.AddField("Is blacklisted", (userDb.IsBlacklisted) ? "true" : "false", true).AddField("Is banned", (userDb.IsBanned) ? "true" : "false", true);
                    embed.AddField("Mutual servers (probable)", (mutualServers) ? "true" : "false", true);
                    embed.WithImageUrl(getUser.GetRealAvatarUrl(1024));

                    await Context.Channel.SendMessageAsync("", false, embed.Build()).ConfigureAwait(false);
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task Blacklist([Remainder]string user)
            {
                IUser getUser;
                if (UInt64.TryParse(user, out var id))
                {
                    getUser = await _restClient.GetUserAsync(id).ConfigureAwait(false);
                }
                else
                {
                    var userSplit = user.Split("#");
                    if (userSplit.Length == 2)
                        getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
                    else
                        getUser = null;
                }
                if (getUser is null)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't find the user.").ConfigureAwait(false);
                    return;
                }
                if (getUser.Id == RiasBot.KonekoId)
                {
                    await Context.Channel.SendErrorMessageAsync("I will not add you on the blacklist, Master!");
                    return;
                }

                var confirm = await Context.Channel.SendConfirmationMessageAsync($"Are you sure you want to add this user to the blacklist? Type {Format.Code("confirm")}");
                var input = await _is.NextMessageAsync((ShardedCommandContext)Context, timeout: TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                if (input != null)
                {
                    if (input.Content == "confirm")
                    {
                        using (var db = _db.GetDbContext())
                        {
                            var userDb = db.Users.Where(x => x.UserId == getUser.Id).FirstOrDefault();
                            if (userDb != null)
                            {
                                userDb.IsBlacklisted = true;
                            }
                            else
                            {
                                var userConfig = new UserConfig { UserId = getUser.Id, IsBlacklisted = true };
                                await db.AddAsync(userConfig).ConfigureAwait(false);
                            }
                            if (getUser != null)
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {getUser} has been added to the blacklist.").ConfigureAwait(false);
                            }
                            else
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {id} has been added to the blacklist.").ConfigureAwait(false);
                            }
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("Canceled!");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task RemoveBlacklist([Remainder]string user)
            {
                IUser getUser;
                if (UInt64.TryParse(user, out var id))
                {
                    getUser = await _restClient.GetUserAsync(id).ConfigureAwait(false);
                }
                else
                {
                    var userSplit = user.Split("#");
                    if (userSplit.Length == 2)
                        getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
                    else
                        getUser = null;
                }
                if (getUser.Id == RiasBot.KonekoId)
                {
                    await Context.Channel.SendErrorMessageAsync("You are not in the blacklist, Master!");
                    return;
                }

                var confirm = await Context.Channel.SendConfirmationMessageAsync($"Are you sure you want to remove this user from the blacklist? Type {Format.Code("confirm")}");
                var input = await _is.NextMessageAsync((ShardedCommandContext)Context, timeout: TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                if (input != null)
                {
                    if (input.Content == "confirm")
                    {
                        using (var db = _db.GetDbContext())
                        {
                            var userDb = db.Users.Where(x => x.UserId == id).FirstOrDefault();
                            if (userDb != null)
                            {
                                userDb.IsBlacklisted = false;
                            }
                            else
                            {
                                var userConfig = new UserConfig { UserId = getUser.Id, IsBlacklisted = false };
                                await db.AddAsync(userConfig).ConfigureAwait(false);
                            }
                            if (getUser != null)
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {getUser} has been removed from the blacklist.").ConfigureAwait(false);
                            }
                            else
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {id} has been removed from the blacklist.").ConfigureAwait(false);
                            }
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task BotBan([Remainder]string user)
            {
                IUser getUser;
                if (UInt64.TryParse(user, out var id))
                {
                    getUser = await _restClient.GetUserAsync(id).ConfigureAwait(false);
                }
                else
                {
                    var userSplit = user.Split("#");
                    if (userSplit.Length == 2)
                        getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
                    else
                        getUser = null;
                }
                if (getUser is null)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't find the user.").ConfigureAwait(false);
                    return;
                }
                if (getUser.Id == RiasBot.KonekoId)
                {
                    await Context.Channel.SendErrorMessageAsync("I will not ban you from using me, Master!");
                    return;
                }

                var confirm = await Context.Channel.SendConfirmationMessageAsync($"Are you sure you want to ban this user from using the commands? Type {Format.Code("confirm")}");
                var input = await _is.NextMessageAsync((ShardedCommandContext)Context, timeout: TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                if (input != null)
                {
                    if (input.Content == "confirm")
                    {
                        using (var db = _db.GetDbContext())
                        {
                            var userDb = db.Users.Where(x => x.UserId == getUser.Id).FirstOrDefault();
                            if (userDb != null)
                            {
                                userDb.IsBlacklisted = true;
                                userDb.IsBanned = true;
                            }
                            else
                            {
                                var userConfig = new UserConfig { UserId = getUser.Id, IsBlacklisted = true, IsBanned = true };
                                await db.AddAsync(userConfig).ConfigureAwait(false);
                            }
                            if (getUser != null)
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {getUser} has been banned from using the bot.").ConfigureAwait(false);
                            }
                            else
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {id} has been banned from using the bot.").ConfigureAwait(false);
                            }
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("Canceled!");
                    }
                }
            }

            [RiasCommand][@Alias]
            [Description][@Remarks]
            [RequireOwner]
            public async Task RemoveBotBan([Remainder]string user)
            {
                IUser getUser;
                if (UInt64.TryParse(user, out var id))
                {
                    getUser = await _restClient.GetUserAsync(id).ConfigureAwait(false);
                }
                else
                {
                    var userSplit = user.Split("#");
                    if (userSplit.Length == 2)
                        getUser = await Context.Client.GetUserAsync(userSplit[0], userSplit[1]).ConfigureAwait(false);
                    else
                        getUser = null;
                }
                if (getUser is null)
                {
                    await Context.Channel.SendErrorMessageAsync($"{Context.User.Mention} I couldn't find the user.").ConfigureAwait(false);
                    return;
                }
                if (getUser.Id == RiasBot.KonekoId)
                {
                    await Context.Channel.SendErrorMessageAsync("You are not banned from using me, Master!");
                    return;
                }

                var confirm = await Context.Channel.SendConfirmationMessageAsync($"Are you sure you want to unban this user from using the commands? Type {Format.Code("confirm")}");
                var input = await _is.NextMessageAsync((ShardedCommandContext)Context, timeout: TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                if (input != null)
                {
                    if (input.Content == "confirm")
                    {
                        using (var db = _db.GetDbContext())
                        {
                            var userDb = db.Users.Where(x => x.UserId == getUser.Id).FirstOrDefault();
                            if (userDb != null)
                            {
                                userDb.IsBanned = false;
                            }
                            else
                            {
                                var userConfig = new UserConfig { UserId = getUser.Id, IsBanned = false };
                                await db.AddAsync(userConfig).ConfigureAwait(false);
                            }
                            if (getUser != null)
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {getUser} has been unbanned from using the bot.").ConfigureAwait(false);
                            }
                            else
                            {
                                await Context.Channel.SendConfirmationMessageAsync($"{Context.User.Mention} user {id} has been unbanned from using the bot.").ConfigureAwait(false);
                            }
                            await db.SaveChangesAsync().ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await Context.Channel.SendErrorMessageAsync("Canceled!");
                    }
                }
            }
        }
    }
}
