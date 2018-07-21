using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;

namespace RiasBot.Modules.Administration.Services
{
    public class MuteService : IRService
    {
        private readonly DbService _db;
        public MuteService(DbService db)
        {
            _db = db;
        }
        
        public async Task MuteUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, TimeSpan time, string reason)
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
                    await channel.SendErrorEmbed($"{moderator.Mention} {Format.Bold(user.ToString())} is already muted from text and voice channels!");
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
                    if (!string.IsNullOrEmpty(reason))
                        embed.AddField("Reason", reason, true);
                    else
                        embed.AddField("Moderator", moderator);
                    
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

        public async Task UnmuteUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, string reason, bool showIsNotMutedMessage = true)
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
                            await channel.SendErrorEmbed($"I couldn't unmute {user} because I couldn't find the mute role. Set a new mute role!");
                            return;
                        }
                    }
                }
                else
                {
                    role = guild.Roles.FirstOrDefault(x => x.Name == "rias-mute");
                    if (role is null)
                    {
                        await channel.SendErrorEmbed($"I couldn't unmute {user} because I couldn't find the mute role. Set a new mute role!");
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
                        await channel.SendErrorEmbed($"{moderator.Mention} {Format.Bold(user.ToString())} is not muted from text and voice channels!");
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
                }
            }
        }
        
        public async Task AddMuteRoleToChannels(IRole role, IGuild guild)
        {
            var permissions = new OverwritePermissions().Modify(addReactions: PermValue.Deny, sendMessages: PermValue.Deny);

            var channels = await guild.GetTextChannelsAsync();
            foreach (var c in channels)
            {
                await c.AddPermissionOverwriteAsync(role, permissions).ConfigureAwait(false);
            }
        }
    }
}