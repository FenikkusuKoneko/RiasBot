using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;

namespace RiasBot.Modules.Administration.Services
{
    public class MuteService : IRService
    {
        private readonly DiscordShardedClient _client;
        private readonly DbService _db;
        
        public MuteService(DiscordShardedClient client, DbService db)
        {
            _client = client;
            _db = db;
            
            LoadMuteTimers().GetAwaiter().GetResult();
        }
        
        private readonly ConcurrentDictionary<ulong, List<UserMute>> _muteTimers = new ConcurrentDictionary<ulong, List<UserMute>>();
        
        public async Task MuteUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel,
            TimeSpan untilTime, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
                var userGuildDb = db.UserGuilds.Where(x => x.GuildId == guild.Id);
                
                IRole role;
                if (guildDb != null)
                {
                    role = guild.GetRole(guildDb.MuteRole) ?? (guild.Roles.FirstOrDefault(x => x.Name == "rias-mute") ?? await guild.CreateRoleAsync("rias-mute").ConfigureAwait(false));
                }
                else
                {
                    role = guild.Roles.FirstOrDefault(x => x.Name == "rias-mute") ?? await guild.CreateRoleAsync("rias-mute").ConfigureAwait(false);
                }
                
                if (user.RoleIds.Any(r => r == role.Id))
                {
                    await channel.SendErrorMessageAsync($"{moderator.Mention} {Format.Bold(user.ToString())} is already muted from text and voice channels!");
                }
                else
                {
                    await Task.Factory.StartNew(async () => await AddMuteRoleToChannels(role, guild));
                    await user.AddRoleAsync(role).ConfigureAwait(false);
                    await user.ModifyAsync(x => x.Mute = true).ConfigureAwait(false);
                    
                    var muteUser = userGuildDb.FirstOrDefault(x => x.UserId == user.Id);
                    if (muteUser != null)
                    {
                        muteUser.IsMuted = true;
                    }
                    else
                    {
                        var muteUserGuild = new UserGuildConfig { GuildId = guild.Id, UserId = user.Id, IsMuted = true };
                        await db.AddAsync(muteUserGuild).ConfigureAwait(false);
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    
                    var embed = new EmbedBuilder().WithColor(0xffff00);
                    embed.WithDescription("Mute");
                    embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                    embed.AddField("Moderator", moderator, true);

                    if (untilTime.CompareTo(TimeSpan.Zero) != 0)
                    {
                        await Task.Factory.StartNew(() => AddMuteTimer(guild, moderator, user,
                            channel, untilTime));
                        embed.AddField("For", untilTime.StringTimeSpan(), true);
                    }
                    
                    if (!string.IsNullOrEmpty(reason))
                        embed.AddField("Reason", reason, true);
                    
                    embed.WithCurrentTimestamp();

                    if (guildDb != null)
                    {
                        var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                        if (modlog != null)
                            await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                        else
                            await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                    else
                    {
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task UnmuteUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel,
            string reason, bool showIsNotMutedMessage = true)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
                var userGuildDb = db.UserGuilds.Where(x => x.GuildId == guild.Id);
                var muteUser = userGuildDb.FirstOrDefault(x => x.UserId == user.Id);

                IRole role;
                if (guildDb != null)
                {
                    role = guild.GetRole(guildDb.MuteRole);
                    if (role is null)
                    {
                        role = guild.Roles.FirstOrDefault(x => x.Name == "rias-mute");
                        if (role is null)
                        {
                            if (channel != null)
                                await channel.SendErrorMessageAsync($"I couldn't unmute {user} because I couldn't find the mute role. Set a new mute role!");
                            return;
                        }
                    }
                }
                else
                {
                    role = guild.Roles.FirstOrDefault(x => x.Name == "rias-mute");
                    if (role is null)
                    {
                        if (channel != null)
                            await channel.SendErrorMessageAsync($"I couldn't unmute {user} because I couldn't find the mute role. Set a new mute role!");
                        return;
                    }
                }
                
                if (user.RoleIds.Any(r => r == role.Id))
                {
                    await user.RemoveRoleAsync(role).ConfigureAwait(false);
                    await user.ModifyAsync(x => x.Mute = false).ConfigureAwait(false);
                    
                    if (muteUser != null)
                    {
                        muteUser.IsMuted = false;
                    }
                    else
                    {
                        var muteUserGuild = new UserGuildConfig { GuildId = guild.Id, UserId = user.Id, IsMuted = false};
                        await db.AddAsync(muteUserGuild).ConfigureAwait(false);
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await Task.Factory.StartNew(async () => await RemoveMuteTimer(guild, user));
                    
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithDescription("Unmute");
                    embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                    embed.AddField("Moderator", moderator, true);
                    
                    if (!string.IsNullOrEmpty(reason))
                        embed.AddField("Reason", reason, true);
                    
                    embed.WithCurrentTimestamp();
                    
                    if (guildDb != null)
                    {
                        var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                        if (modlog != null)
                            await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                        else if (channel != null)
                            await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                    else if (channel != null)
                    {
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (showIsNotMutedMessage)
                        if (channel != null)
                            await channel.SendErrorMessageAsync($"{moderator.Mention} {Format.Bold(user.ToString())} is not muted from text and voice channels!");
                }
            }
        }
        
        private async Task UnmuteTimerUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel,
            string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
                var userGuildDb = db.UserGuilds.Where(x => x.GuildId == guild.Id);
                var muteUser = userGuildDb.FirstOrDefault(x => x.UserId == user.Id);

                IRole role;
                if (guildDb != null)
                {
                    role = guild.GetRole(guildDb.MuteRole);
                    if (role is null)
                    {
                        role = guild.Roles.FirstOrDefault(x => x.Name == "rias-mute");
                        if (role is null)
                        {
                            return;
                        }
                    }
                }
                else
                {
                    role = guild.Roles.FirstOrDefault(x => x.Name == "rias-mute");
                    if (role is null)
                    {
                        return;
                    }
                }
                
                if (user.RoleIds.Any(r => r == role.Id))
                {
                    if (muteUser != null)
                    {
                        muteUser.IsMuted = false;
                    }
                    else
                    {
                        var muteUserGuild = new UserGuildConfig { GuildId = guild.Id, UserId = user.Id, IsMuted = false};
                        await db.AddAsync(muteUserGuild).ConfigureAwait(false);
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await Task.Factory.StartNew(() => RemoveMuteTimer(guild, user));
                    
                    user = await guild.GetUserAsync(user.Id);
                    if (user is null)
                    {
                        return;
                    }
                    
                    await user.RemoveRoleAsync(role).ConfigureAwait(false);
                    await user.ModifyAsync(x => x.Mute = false).ConfigureAwait(false);
                    
                    var embed = new EmbedBuilder().WithColor(RiasBot.GoodColor);
                    embed.WithDescription("Unmute");
                    embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                    embed.AddField("Moderator", moderator.ToString() ?? "-", true);
                    
                    if (!string.IsNullOrEmpty(reason))
                        embed.AddField("Reason", reason, true);
                    
                    embed.WithCurrentTimestamp();
                    
                    if (guildDb != null)
                    {
                        var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                        if (modlog != null)
                            await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                        else if (channel != null)
                            await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                    else if (channel != null)
                    {
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task AddMuteTimer(IGuild guild, IGuildUser moderator, IGuildUser user,
            IMessageChannel channel, TimeSpan untilTime, bool addToDb = true)
        {
            using (var db = _db.GetDbContext())
            {
                var muteTimer = new Timer(async _ => await UnmuteTimerUser(guild, moderator, user,
                    channel, "Time's up!"), null, untilTime, TimeSpan.Zero);
                var userMute = new UserMute()
                {
                    UserId = user.Id,
                    MuteTimer = muteTimer
                };
                
                if (_muteTimers.TryGetValue(guild.Id, out var muteUser))
                {
                    _muteTimers.AddOrUpdate(guild.Id, muteUser, (id, timer) =>
                    {
                        timer.Add(userMute);
                        return timer;
                    });
                }
                else
                {
                    _muteTimers.TryAdd(guild.Id, new List<UserMute> {userMute});
                }
                
                if (addToDb)
                {
                    var muteTimerDb = db.MuteTimers.Where(x => x.GuildId == guild.Id).FirstOrDefault(x => x.UserId == user.Id);
                    if (muteTimerDb != null)
                    {
                        muteTimerDb.Moderator = moderator.Id;
                        muteTimerDb.MuteChannelSource = channel.Id;
                        muteTimerDb.MutedUntil = DateTime.UtcNow.Add(untilTime);
                    }
                    else
                    {
                        var newMuteTimerDb = new MuteTimers
                        {
                            GuildId = guild.Id,
                            UserId = user.Id,
                            Moderator = moderator.Id,
                            MuteChannelSource = channel.Id,
                            MutedUntil = DateTime.UtcNow.Add(untilTime)
                        };
                        await db.AddAsync(newMuteTimerDb).ConfigureAwait(false);
                    }

                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }
        
        public async Task RemoveMuteTimer(IGuild guild, IGuildUser user)
        {
            using (var db = _db.GetDbContext())
            {
                if (_muteTimers.TryGetValue(guild.Id, out var muteUser))
                {
                    var userMute = muteUser.Find(x => x.UserId == user.Id);
                    if (userMute != null)
                    {
                        userMute.MuteTimer.Dispose();
                        _muteTimers.AddOrUpdate(guild.Id, muteUser, (id, timer) =>
                        {
                            timer.Remove(userMute);
                            return timer;
                        });
                    }
                }
                var muteTimerDb = db.MuteTimers.Where(x => x.GuildId == guild.Id).FirstOrDefault(x => x.UserId == user.Id);
                if (muteTimerDb != null)
                {
                    db.Remove(muteTimerDb);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task LoadMuteTimers()
        {
            using (var db = _db.GetDbContext())
            {
                foreach (var muteTimer in db.MuteTimers)
                {
                    var guild = _client.GetGuild(muteTimer.GuildId);
                    if (guild is null) continue;

                    var user = await ((IGuild) guild).GetUserAsync(muteTimer.UserId);
                    var moderator = await ((IGuild) guild).GetUserAsync(muteTimer.Moderator);
                    var channel = await ((IGuild) guild).GetTextChannelAsync(muteTimer.MuteChannelSource);
                    var timeSpan = muteTimer.MutedUntil.Subtract(DateTime.UtcNow);
                    
                    if (muteTimer.MutedUntil.CompareTo(DateTime.UtcNow) > 0)
                    {
                        await AddMuteTimer(guild, moderator, user, channel, timeSpan, false);
                    }
                    else
                    {
                        await UnmuteUser(guild, moderator, user, channel, "Time's Up!", false);
                    }
                }
                await db.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        
        public async Task AddMuteRoleToChannels(IRole role, IGuild guild)
        {
            var permissions = new OverwritePermissions().Modify(addReactions: PermValue.Deny, sendMessages: PermValue.Deny);

            var textChannels = await guild.GetTextChannelsAsync();
            foreach (var c in textChannels)
            {
                await c.AddPermissionOverwriteAsync(role, permissions).ConfigureAwait(false);
            }
            var categories = await guild.GetCategoriesAsync();
            foreach (var cat in categories)
            {
                await cat.AddPermissionOverwriteAsync(role, permissions).ConfigureAwait(false);
            }
        }
    }

    public class UserMute
    {
        public ulong UserId { get; set; }
        public Timer MuteTimer { get; set; }
    }
}