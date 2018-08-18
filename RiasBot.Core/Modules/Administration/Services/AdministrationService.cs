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
        private readonly DiscordShardedClient _client;
        private readonly DbService _db;
        public AdministrationService(DiscordShardedClient client, DbService db)
        {
            _client = client;
            _db = db;
        }

        public async Task KickUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, IUserMessage message, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();

                var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                embed.WithDescription("Kick");
                embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                embed.AddField("Moderator", moderator, true);
                embed.WithThumbnailUrl(user.RealAvatarUrl());
                if (!String.IsNullOrEmpty(reason))
                    embed.AddField("Reason", reason);

                if (guildDb != null)
                {
                    var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                    if (modlog != null)
                    {
                        await message.AddReactionAsync(new Emoji("✅")).ConfigureAwait(false);
                        await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                    else
                    {
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                }
                else
                {
                    await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }

                var reasonEmbed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                reasonEmbed.WithDescription($"You have been kicked from {Format.Bold(guild.Name)} server!");
                if (reason != null)
                    reasonEmbed.AddField("Reason", reason);

                if (!user.IsBot)
                    await user.SendMessageAsync("", embed: reasonEmbed.Build()).ConfigureAwait(false);

                await user.KickAsync(reason).ConfigureAwait(false);
            }
        }

        public async Task BanUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, IUserMessage message, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();

                var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                embed.WithDescription("Ban");
                embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                embed.AddField("Moderator", moderator, true);
                embed.WithThumbnailUrl(user.RealAvatarUrl());
                if (reason != null)
                    embed.AddField("Reason", reason);

                if (guildDb != null)
                {
                    var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                    if (modlog != null)
                    {
                        await message.AddReactionAsync(new Emoji("✅")).ConfigureAwait(false);
                        await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                    else
                    {
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                }
                else
                {
                    await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }

                var reasonEmbed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                reasonEmbed.WithDescription($"You have been banned from {Format.Bold(guild.Name)} server!");
                if (reason != null)
                    reasonEmbed.AddField("Reason", reason);

                if (!user.IsBot)
                    await user.SendMessageAsync("", embed: reasonEmbed.Build()).ConfigureAwait(false);

                await guild.AddBanAsync(user).ConfigureAwait(false);
            }
        }

        public async Task SoftbanUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, IUserMessage message, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();

                var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                embed.WithDescription("SoftBan");
                embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                embed.AddField("Moderator", moderator, true);
                embed.WithThumbnailUrl(user.RealAvatarUrl());
                if (reason != null)
                    embed.AddField("Reason", reason);

                if (guildDb != null)
                {
                    var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                    if (modlog != null)
                    {
                        await message.AddReactionAsync(new Emoji("✅")).ConfigureAwait(false);
                        await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                    else
                    {
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                }
                else
                {
                    await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }

                var reasonEmbed = new EmbedBuilder().WithColor(RiasBot.BadColor);
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

        public async Task PrunebanUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, IUserMessage message, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();

                var embed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                embed.WithDescription("PruneBan");
                embed.AddField("User", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                embed.AddField("Moderator", moderator, true);
                embed.WithThumbnailUrl(user.RealAvatarUrl());
                if (reason != null)
                    embed.AddField("Reason", reason);

                if (guildDb != null)
                {
                    var modlog = await guild.GetTextChannelAsync(guildDb.ModLogChannel).ConfigureAwait(false);
                    if (modlog != null)
                    {
                        await message.AddReactionAsync(new Emoji("✅")).ConfigureAwait(false);
                        await modlog.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                    else
                    {
                        await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                    }
                }
                else
                {
                    await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                }

                var reasonEmbed = new EmbedBuilder().WithColor(RiasBot.BadColor);
                reasonEmbed.WithDescription($"You have been banned from {Format.Bold(guild.Name)} server!");
                if (reason != null)
                    reasonEmbed.AddField("Reason", reason);

                if (!user.IsBot)
                    await user.SendMessageAsync("", embed: reasonEmbed.Build()).ConfigureAwait(false);
                await Task.Delay(1000).ConfigureAwait(false);
                await guild.AddBanAsync(user, 7).ConfigureAwait(false);
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
