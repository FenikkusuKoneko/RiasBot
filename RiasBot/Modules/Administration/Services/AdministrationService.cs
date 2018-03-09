using Discord;
using Discord.Commands;
using Discord.WebSocket;
using RiasBot.Extensions;
using RiasBot.Services;
using RiasBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Administration.Services
{
    public class AdministrationService : IRService
    {
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;
        public AdministrationService(DiscordSocketClient client, DbService db)
        {
            _client = client;
            _db = db;
        }

        public async Task MuteUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                try
                {
                    IRole role = null;
                    try
                    {
                        role = guild.GetRole(guildDb.MuteRole);
                        if (role is null)
                        {
                            role = await guild.CreateRoleAsync("rias-mute").ConfigureAwait(false);
                            guildDb.MuteRole = role.Id;
                        }
                    }
                    catch
                    {
                        role = await guild.CreateRoleAsync("rias-mute").ConfigureAwait(false);
                        var newRole = new GuildConfig { GuildId = guild.Id, MuteRole = role.Id };
                        await db.AddAsync(newRole).ConfigureAwait(false);
                    }
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    if (user.RoleIds.Any(r => r == role.Id))
                    {
                        await channel.SendErrorEmbed($"{moderator.Mention} {Format.Bold(user.ToString())} is already muted from text and voice channels!");
                    }
                    else
                    {
                        await Task.Factory.StartNew(() => MuteService(role, guild));
                        await user.AddRoleAsync(role).ConfigureAwait(false);

                        var embed = new EmbedBuilder().WithColor(0xffff00);
                        embed.WithDescription("Mute");
                        embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                        embed.AddField("Moderator", moderator, true);
                        if (!String.IsNullOrEmpty(reason))
                            embed.AddField("Reason", reason, true);
                        else
                            embed.AddField("Moderator", moderator);

                        embed.WithCurrentTimestamp();

                        var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                        if (modlog != null)
                            await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                        else
                            await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                }
                catch
                {
                }
            }
        }

        public async Task UnmuteUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                try
                {
                    IRole role = null;
                    try
                    {
                        role = guild.GetRole(guildDb.MuteRole);
                    }
                    catch
                    {
                        role = await guild.CreateRoleAsync("rias-mute").ConfigureAwait(false);
                        var newRole = new GuildConfig { GuildId = guild.Id, MuteRole = role.Id };
                        await db.AddAsync(newRole).ConfigureAwait(false);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                    if (user.RoleIds.Any(r => r == role.Id))
                    {
                        await user.RemoveRoleAsync(role).ConfigureAwait(false);

                        var embed = new EmbedBuilder().WithColor(RiasBot.goodColor);
                        embed.WithDescription("Unmute");
                        embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                        embed.AddField("Moderator", moderator, true);
                        if (!String.IsNullOrEmpty(reason))
                            embed.AddField("Reason", reason, true);

                        embed.WithCurrentTimestamp();

                        var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                        if (modlog != null)
                            await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                        else
                            await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                    else
                    {
                        await channel.SendConfirmationEmbed($"{moderator.Mention} {Format.Bold(user.ToString())} is not muted from text and voice channels!");
                    }
                }
                catch
                {
                    
                }
            }
        }

        public async Task KickUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();

                var embed = new EmbedBuilder().WithColor(RiasBot.badColor);
                embed.WithDescription("Kick");
                embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                embed.AddField("Moderator", moderator, true);
                if (!String.IsNullOrEmpty(reason))
                    embed.AddField("Reason", reason);

                try
                {
                    var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                    if (modlog != null)
                        await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    else
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }

                var reasonEmbed = new EmbedBuilder().WithColor(RiasBot.badColor);
                reasonEmbed.WithDescription($"You have been kicked from {Format.Bold(guild.Name)} server!");
                if (reason != null)
                    reasonEmbed.AddField("Reason", reason);

                if (!user.IsBot)
                    await user.SendMessageAsync("", embed: reasonEmbed.Build()).ConfigureAwait(false);

                await user.KickAsync(reason).ConfigureAwait(false);
            }
        }

        public async Task BanUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();

                var embed = new EmbedBuilder().WithColor(RiasBot.badColor);
                embed.WithDescription("Ban");
                embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                embed.AddField("Moderator", moderator, true);
                if (reason != null)
                    embed.AddField("Reason", reason);

                try
                {
                    var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                    if (modlog != null)
                        await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    else
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }

                var reasonEmbed = new EmbedBuilder().WithColor(RiasBot.badColor);
                reasonEmbed.WithDescription($"You have been banned from {Format.Bold(guild.Name)} server!");
                if (reason != null)
                    reasonEmbed.AddField("Reason", reason);

                if (!user.IsBot)
                    await user.SendMessageAsync("", embed: reasonEmbed.Build()).ConfigureAwait(false);

                await guild.AddBanAsync(user).ConfigureAwait(false);
            }
        }

        public async Task SoftbanUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();

                var embed = new EmbedBuilder().WithColor(RiasBot.badColor);
                embed.WithDescription("Softban");
                embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                embed.AddField("Moderator", moderator, true);
                if (reason != null)
                    embed.AddField("Reason", reason);

                try
                {
                    var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                    if (modlog != null)
                        await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    else
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }
                catch
                {
                    await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }

                var reasonEmbed = new EmbedBuilder().WithColor(RiasBot.badColor);
                reasonEmbed.WithDescription($"You have been kicked from {Format.Bold(guild.Name)} server!");
                if (reason != null)
                    reasonEmbed.AddField("Reason", reason);

                if (!user.IsBot)
                    await user.SendMessageAsync("", embed: reasonEmbed.Build()).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
                await guild.AddBanAsync(user, 7).ConfigureAwait(false);
                await guild.RemoveBanAsync(user).ConfigureAwait(false);
            }
        }

        public async Task MuteService(IRole role, IGuild guild)
        {
            OverwritePermissions permissions = new OverwritePermissions()
                .Modify(PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Deny,
                PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit,
                PermValue.Inherit, PermValue.Inherit, PermValue.Deny, PermValue.Deny, PermValue.Inherit,
                PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit, PermValue.Inherit);

            var channels = await guild.GetChannelsAsync();
            foreach (var c in channels)
            {
                await c.AddPermissionOverwriteAsync(role, permissions).ConfigureAwait(false);
            }
        }

        public bool CheckHierarchyRole(IGuild guild, IGuildUser user, IGuildUser bot)
        {
            var userRoles = new List<IRole>();
            var botRoles = new List<IRole>();

            foreach (var userRole in user.RoleIds)
                userRoles.Add(guild.GetRole(userRole));
            foreach (var botRole in bot.RoleIds)
                botRoles.Add(guild.GetRole(botRole));

            return botRoles.Any(x => userRoles.All(y => x.Position > y.Position));
        }
    }
}
