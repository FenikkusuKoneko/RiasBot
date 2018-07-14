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
                var nrWarnings = 0;
                var guildDb = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                var warnings = db.Warnings.Where(x => x.GuildId == guild.Id).Where(y => y.UserId == user.Id).ToList();

                nrWarnings = warnings.Where(x => x.UserId == user.Id).Count();
                var warning = new Warnings { GuildId = guild.Id, UserId = user.Id, Reason = reason, Moderator = moderator.Id };
                await db.AddAsync(warning).ConfigureAwait(false);
                await db.SaveChangesAsync().ConfigureAwait(false);

                var embed = new EmbedBuilder().WithColor(0xffff00);
                embed.WithTitle($"Warn");
                embed.AddField("Username", $"{user}", true).AddField("ID", user.Id.ToString(), true);
                embed.AddField("Warn no.", nrWarnings + 1).AddField("Moderator", moderator, true);
                if (!String.IsNullOrEmpty(reason))
                    embed.AddField("Reason", reason, true);

                await channel.SendMessageAsync("", embed: embed.Build()).ConfigureAwait(false);
                try
                {
                    if (nrWarnings + 1 >= guildDb.WarnsPunishment && guildDb.WarnsPunishment != 0)
                    {
                        db.Remove(warning);
                        db.RemoveRange(warnings);
                        await db.SaveChangesAsync().ConfigureAwait(false);
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
                            case "softban":
                                await _adminService.SoftbanUser(guild, moderator, user, channel, $"You got {guildDb.WarnsPunishment} warnings! SoftBan punishment applied!").ConfigureAwait(false);
                                break;
                            case "pruneban":
                                await _adminService.PrunebanUser(guild, moderator, user, channel, $"You got {guildDb.WarnsPunishment} warnings! PruneBan punishment applied!").ConfigureAwait(false);
                                break;
                        }
                    }
                }
                catch
                {

                }
            }
        }

        public async Task RegisterMuteWarning(IGuild guild, IGuildUser user, IMessageChannel channel, int warns)
        {
            using (var db = _db.GetDbContext())
            {
                var warnings = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                if (warnings != null)
                {
                    warnings.WarnsPunishment = warns;
                    warnings.PunishmentMethod = "mute";
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("muted")}.").ConfigureAwait(false);
                }
                else
                {
                    var warningPunishment = new GuildConfig { GuildId = guild.Id, WarnsPunishment = warns, PunishmentMethod = "mute" };
                    await db.AddAsync(warningPunishment).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("muted")}.").ConfigureAwait(false);
                }
            }
        }

        public async Task RegisterKickWarning(IGuild guild, IGuildUser user, IMessageChannel channel, int warns)
        {
            using (var db = _db.GetDbContext())
            {
                var warnings = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                if (warnings != null)
                {
                    warnings.WarnsPunishment = warns;
                    warnings.PunishmentMethod = "kick";
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("kicked")}.").ConfigureAwait(false);
                }
                else
                {
                    var warningPunishment = new GuildConfig { GuildId = guild.Id, WarnsPunishment = warns, PunishmentMethod = "kick" };
                    await db.AddAsync(warningPunishment).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("kicked")}.").ConfigureAwait(false);
                }
            }
        }

        public async Task RegisterBanWarning(IGuild guild, IGuildUser user, IMessageChannel channel, int warns)
        {
            using (var db = _db.GetDbContext())
            {
                var warnings = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                if (warnings != null)
                {
                    warnings.WarnsPunishment = warns;
                    warnings.PunishmentMethod = "ban";
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("banned")}.").ConfigureAwait(false);
                }
                else
                {
                    var warningPunishment = new GuildConfig { GuildId = guild.Id, WarnsPunishment = warns, PunishmentMethod = "ban" };
                    await db.AddAsync(warningPunishment).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("banned")}.").ConfigureAwait(false);
                }
            }
        }

        public async Task RegisterSoftbanWarning(IGuild guild, IGuildUser user, IMessageChannel channel, int warns)
        {
            using (var db = _db.GetDbContext())
            {
                var warnings = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                if (warnings != null)
                {
                    warnings.WarnsPunishment = warns;
                    warnings.PunishmentMethod = "softban";
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("softbanned")}.").ConfigureAwait(false);
                }
                else
                {
                    var warningPunishment = new GuildConfig { GuildId = guild.Id, WarnsPunishment = warns, PunishmentMethod = "softban" };
                    await db.AddAsync(warningPunishment).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("softbanned")}.").ConfigureAwait(false);
                }
            }
        }

        public async Task RegisterPrunebanWarning(IGuild guild, IGuildUser user, IMessageChannel channel, int warns)
        {
            using (var db = _db.GetDbContext())
            {
                var warnings = db.Guilds.Where(x => x.GuildId == guild.Id).FirstOrDefault();
                if (warnings != null)
                {
                    warnings.WarnsPunishment = warns;
                    warnings.PunishmentMethod = "pruneban";
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("prunebanned")}.").ConfigureAwait(false);
                }
                else
                {
                    var warningPunishment = new GuildConfig { GuildId = guild.Id, WarnsPunishment = warns, PunishmentMethod = "pruneban" };
                    await db.AddAsync(warningPunishment).ConfigureAwait(false);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                    await channel.SendConfirmationEmbed($"{user.Mention} at {Format.Bold(warns.ToString())} warnings the user will be {Format.Bold("prunebanned")}.").ConfigureAwait(false);
                }
            }
        }
    }
}
