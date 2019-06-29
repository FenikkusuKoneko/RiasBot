using System.Text;
using Microsoft.EntityFrameworkCore;
using Rias.Core.Database.Models;
using Rias.Core.Implementation;
using Serilog;

namespace Rias.Core.Database
{
    public class RiasDbContext : DbContext
    {
        public DbSet<Guilds> Guilds { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<UserGuilds> UserGuilds { get; set; }
        public DbSet<Warnings> Warnings { get; set; }
        public DbSet<XpSystem> XpSystem { get; set; }
        public DbSet<Waifus> Waifus { get; set; }
        public DbSet<Patreon> Patreon { get; set; }
        public DbSet<SelfAssignableRoles> SelfAssignableRoles { get; set; }
        public DbSet<XpRolesSystem> XpRolesSystem { get; set; }
        public DbSet<Profile> Profile { get; set; }
        public DbSet<MuteTimers> MuteTimers { get; set; }
        public DbSet<Dailies> Dailies { get; set; }
        
        private readonly Credentials _creds;

        public RiasDbContext(Credentials creds)
        {
            _creds = creds;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_creds.DatabaseConfig != null)
            {
                var connectionString = new StringBuilder();
                connectionString.Append("Host=").Append(_creds.DatabaseConfig.Host).Append(";");

                if (_creds.DatabaseConfig.Port > 0)
                    connectionString.Append("Port=").Append(_creds.DatabaseConfig.Port).Append(";");

                connectionString.Append("Database=").Append(_creds.DatabaseConfig.Database).Append(";")
                    .Append("Username=").Append(_creds.DatabaseConfig.Username).Append(";")
                    .Append("Password=").Append(_creds.DatabaseConfig.Password);

                optionsBuilder.UseNpgsql(connectionString.ToString());
            }
            else
            {
                Log.Error("The database connection is not defined in credentials.json");
            }
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var guildEntity = modelBuilder.Entity<Guilds>();
            guildEntity
                .HasIndex(c => c.GuildId)
                .IsUnique();
            
            var userEntity = modelBuilder.Entity<Users>();
            userEntity
                .HasIndex(c => c.UserId)
                .IsUnique();
            
            modelBuilder.Entity<UserGuilds>();
            modelBuilder.Entity<Warnings>();
            modelBuilder.Entity<XpSystem>();
            modelBuilder.Entity<Waifus>();

            var patreon = modelBuilder.Entity<Patreon>();
            patreon
                .HasIndex(c => c.UserId)
                .IsUnique();
            
            modelBuilder.Entity<SelfAssignableRoles>();
            modelBuilder.Entity<XpRolesSystem>();

            var profile = modelBuilder.Entity<Profile>();
            profile
                .HasIndex(c => c.UserId)
                .IsUnique();

            modelBuilder.Entity<MuteTimers>();
            modelBuilder.Entity<Dailies>();
        }
    }
}