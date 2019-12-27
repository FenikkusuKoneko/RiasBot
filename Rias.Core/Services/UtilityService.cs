using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;

namespace Rias.Core.Services
{
    public class UtilityService : RiasService
    {
        public UtilityService(IServiceProvider services) : base(services)
        {
        }

        public async Task SetPrefixAsync(SocketGuild guild, string prefix)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb != null)
            {
                guildDb.Prefix = prefix;
            }
            else
            {
                var newGuildDb = new Guilds {GuildId = guild.Id, Prefix = prefix};
                await db.AddAsync(newGuildDb);
            }

            await db.SaveChangesAsync();
        }

        public async Task SetLocaleAsync(SocketGuild guild, string locale)
        {
            Resources.SetGuildCulture(guild.Id, new CultureInfo(locale));
            
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var guildDb = db.Guilds.FirstOrDefault(x => x.GuildId == guild.Id);
            if (guildDb != null)
            {
                guildDb.Locale = locale;
            }
            else
            {
                var newGuildDb = new Guilds {GuildId = guild.Id, Locale = locale};
                await db.AddAsync(newGuildDb);
            }

            await db.SaveChangesAsync();
        }

        public Stream GenerateColorImage(Color color)
        {
            using var image = new MagickImage(MagickColor.FromRgb(color.R, color.G, color.B), 300, 300);
            var ms = new MemoryStream();
            image.Write(ms, MagickFormat.Png);
            ms.Position = 0;

            return ms;
        }

        public readonly (string Locale, string Language)[] Languages = new[]
        {
            ("EN", "English"),
            ("LV", "Latvian")
        };
    }
}