using RiasBot.Services.Database.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

namespace RiasBot.Services.Database
{
    public class RiasContextFactory : IDesignTimeDbContextFactory<RiasContext>
    {
        public RiasContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RiasContext>();
            var builder = new SqliteConnectionStringBuilder("Data Source=data/RiasBot.db");
            builder.DataSource = Path.Combine(Environment.CurrentDirectory, builder.DataSource);
            optionsBuilder.UseSqlite(builder.ToString());
            var ctx = new RiasContext(optionsBuilder.Options);
            return ctx;
        }
    }
    public class RiasContext : DbContext
    {
        public DbSet<GuildConfig> Guilds { get; set; }
        public DbSet<UserConfig> Users { get; set; }
        public DbSet<Warnings> Warnings { get; set; }
        public DbSet<XpSystem> XpSystem { get; set; }
        public DbSet<Waifus> Waifus { get; set; }
        public DbSet<Patreon> Patreon { get; set; }
        public DbSet<SelfAssignableRoles> SelfAssignableRoles { get; set; }

        public RiasContext(DbContextOptions<RiasContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            #region GuildConfig

            var guildEntity = modelBuilder.Entity<GuildConfig>();
            guildEntity
                .HasIndex(c => c.GuildId)
                .IsUnique();

            #endregion

            #region UserConfig

            var userEntity = modelBuilder.Entity<UserConfig>();
            userEntity
                .HasIndex(c => c.UserId)
                .IsUnique();

            #endregion

            #region Warning

            var warning = modelBuilder.Entity<Warnings>();

            #endregion

            #region Xp

            var xp = modelBuilder.Entity<XpSystem>();

            #endregion

            #region Waifus

            var waifu = modelBuilder.Entity<Waifus>();

            #endregion

            #region Patreon

            var patreon = modelBuilder.Entity<Patreon>();
            patreon
                .HasIndex(c => c.UserId)
                .IsUnique();

            #endregion

            #region SelfAssignableRoles

            var sar = modelBuilder.Entity<SelfAssignableRoles>();

            #endregion
        }
    }
}
