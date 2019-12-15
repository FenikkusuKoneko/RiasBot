using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;

namespace Rias.Core.Services
{
    public class WarningsService : RiasService
    {
        public WarningsService(IServiceProvider services) : base(services)
        {
        }

        public PermissionRequired CheckRequiredPermissions(SocketGuildUser user)
        {
            if (user.Id == user.Guild.OwnerId || user.GuildPermissions.Administrator)
                return PermissionRequired.NoPermission;

            using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == user.Guild.Id);

            var warnPunishment = guildDb?.WarningPunishment;
            if (string.IsNullOrEmpty(warnPunishment))
                return user.GuildPermissions.MuteMembers || user.GuildPermissions.KickMembers || user.GuildPermissions.BanMembers
                    ? PermissionRequired.NoPermission
                    : PermissionRequired.MuteKickBan;

            if (string.Equals(warnPunishment, "mute", StringComparison.InvariantCultureIgnoreCase))
                return user.GuildPermissions.MuteMembers ? PermissionRequired.NoPermission : PermissionRequired.Mute;

            if (string.Equals(warnPunishment, "kick", StringComparison.InvariantCultureIgnoreCase))
                return user.GuildPermissions.KickMembers ? PermissionRequired.NoPermission : PermissionRequired.Kick;

            if (string.Equals(warnPunishment, "ban", StringComparison.InvariantCultureIgnoreCase))
                return user.GuildPermissions.BanMembers ? PermissionRequired.NoPermission : PermissionRequired.Ban;

            if (string.Equals(warnPunishment, "softban", StringComparison.InvariantCultureIgnoreCase))
            {
                if (user.Id == user.Guild.CurrentUser.Id)
                {
                    return user.GuildPermissions.KickMembers && user.GuildPermissions.BanMembers
                        ? PermissionRequired.NoPermission
                        : PermissionRequired.KickBan;
                }

                return user.GuildPermissions.KickMembers ? PermissionRequired.NoPermission : PermissionRequired.Kick;
            }

            if (string.Equals(warnPunishment, "pruneban", StringComparison.InvariantCultureIgnoreCase))
                return user.GuildPermissions.BanMembers ? PermissionRequired.NoPermission : PermissionRequired.Ban;

            return default;
        }

        public Guilds? GetGuildDb(SocketGuild guild)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
        }

        public async Task<WarningResult> AddWarningAsync(SocketGuildUser user, SocketGuildUser moderator, string? reason)
        {
            var guild = user.Guild;

            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            var userWarningsDb = db.Warnings.Where(x => x.GuildId == guild.Id && x.UserId == user.Id).ToArray();

            var warnsPunishment = guildDb?.PunishmentWarningsRequired ?? 0;
            var warnsCount = userWarningsDb.Length;
            if (warnsCount + 1 >= warnsPunishment && warnsPunishment != 0)
            {
                await RemoveWarningsAsync(userWarningsDb);
                return new WarningResult
                {
                    WarningNumber = warnsCount + 1,
                    Punishment = Enum.TryParse<PunishmentMethod>(guildDb?.WarningPunishment, true, out var warningResult)
                        ? warningResult : default
                };
            }

            if (warnsCount >= 10)
            {
                return new WarningResult
                {
                    WarningNumber = warnsCount,
                    LimitReached = true
                };
            }

            var newWarningDb = new Warnings {GuildId = user.Guild.Id, UserId = user.Id, ModeratorId = moderator.Id, Reason = reason};
            await db.AddAsync(newWarningDb);
            await db.SaveChangesAsync();

            return new WarningResult
            {
                WarningNumber = warnsCount + 1
            };
        }

        public IList<Warnings> GetWarnings(SocketGuild guild)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Warnings.Where(x => x.GuildId == guild.Id).ToList();
        }

        public IList<Warnings> GetUserWarnings(SocketGuildUser user)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Warnings.Where(x => x.GuildId == user.Guild.Id && x.UserId == user.Id).ToList();
        }

        public async Task RemoveWarningAsync(Warnings warning)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            db.Remove(warning);
            await db.SaveChangesAsync();
        }

        public async Task RemoveWarningsAsync(IEnumerable<Warnings> warnings)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            db.RemoveRange(warnings);
            await db.SaveChangesAsync();
        }

        public async Task SetWarningPunishmentAsync(SocketGuild guild, int number, string? punishment)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb is null)
            {
                var newGuildDb = new Guilds {GuildId = guild.Id, PunishmentWarningsRequired = number, WarningPunishment = punishment?.ToLowerInvariant()};
                await db.AddAsync(newGuildDb);
            }
            else
            {
                guildDb.PunishmentWarningsRequired = number;
                guildDb.WarningPunishment = punishment;
            }

            await db.SaveChangesAsync();
        }

        public ulong? GetModLogChannelId(SocketGuild guild)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id)?.ModLogChannelId;
        }

        public class WarningResult
        {
            public int WarningNumber { get; set; }
            public PunishmentMethod Punishment { get; set; } = PunishmentMethod.NoPunishment;
            public bool LimitReached { get; set; }
        }

        public enum PermissionRequired
        {
            NoPermission,
            MuteKickBan,
            Mute,
            Kick,
            Ban,
            KickBan
        }

        public enum PunishmentMethod
        {
            NoPunishment,
            Mute,
            Kick,
            Ban,
            Softban,
            Pruneban
        }
    }
}