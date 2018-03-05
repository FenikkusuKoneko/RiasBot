using Discord;
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
    public class WarningService : IRService
    {
        private readonly AdministrationService _adminService;
        private readonly DbService _db;
        public WarningService(AdministrationService adminService, DbService db)
        {
            _adminService = adminService;
            _db = db;
        }

        public async Task WarnUser(IGuild guild, IGuildUser moderator, IGuildUser user, IMessageChannel channel, string reason)
        {
            using (var db = _db.GetDbContext())
            {
                int nrWarnings = 0;
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                var warnings = db.Warnings.Where(x => x.GuildId == guild.Id).Where(y => y.UserId == user.Id).ToList();

                nrWarnings = warnings.Where(x => x.UserId == user.Id).Count();
                var warning = new Warnings { GuildId = guild.Id, UserId = user.Id, Reason = reason, Moderator = moderator.Id };
                await db.AddAsync(warning).ConfigureAwait(false);
                await db.SaveChangesAsync().ConfigureAwait(false);

                var embed = new EmbedBuilder().WithColor(0xffff00);
                embed.WithTitle($"Warn");
                embed.AddField("Username", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                embed.AddField("Warn nr.", nrWarnings + 1).AddField("Moderator", moderator, true);
                if (!String.IsNullOrEmpty(reason))
                    embed.AddField("Reason", reason, true);

                await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);

                if (nrWarnings + 1 == guildDb.WarnsPunishment)
                {
                    switch (guildDb.PunishmentMethod)
                    {
                        case "mute":
                            await _adminService.MuteUser(guild, moderator, user, channel, $"You got {guildDb.WarnsPunishment} warnings! Mute punishment applied!").ConfigureAwait(false);
                            break;
                        case "kick":
                            await _adminService.KickUser(guild, moderator, user, channel, $"You got {guildDb.WarnsPunishment} warnings! Kick punishment applied!").ConfigureAwait(false);
                            break;
                        case "ban":
                            await _adminService.BanUser(guild, moderator, user, channel, $"You got {guildDb.WarnsPunishment} warnings! Ban punishment applied!").ConfigureAwait(false);
                            break;
                    }
                }
            }
        }

        public async Task RegisterMuteWarning(IGuild guild, IGuildUser user, IMessageChannel channel, int warns)
        {
            using (var db = _db.GetDbContext())
            {
                var warnings = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                try
                {
                    warnings.WarnsPunishment = warns;
                    warnings.PunishmentMethod = "mute";
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("muted")}.");
                }
                catch
                {
                    await channel.SendConfirmationEmbed($"{user.Mention} no warning punishment will be applied in this server.");
                }
            }
        }

        public async Task RegisterKickWarning(IGuild guild, IGuildUser user, IMessageChannel channel, int warns)
        {
            using (var db = _db.GetDbContext())
            {
                var warnings = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                try
                {
                    warnings.WarnsPunishment = warns;
                    warnings.PunishmentMethod = "kick";
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("kicked")}.");
                }
                catch
                {
                    await channel.SendConfirmationEmbed($"{user.Mention} no warning punishment will be applied in this server.");
                }
            }
        }

        public async Task RegisterBanWarning(IGuild guild, IGuildUser user, IMessageChannel channel, int warns)
        {
            using (var db = _db.GetDbContext())
            {
                var warnings = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                try
                {
                    warnings.WarnsPunishment = warns;
                    warnings.PunishmentMethod = "ban";
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("banned")}.");
                }
                catch
                {
                    await channel.SendConfirmationEmbed($"{user.Mention} no warning punishment will be applied in this server.");
                }
            }
        }
    }
}
