using Microsoft.EntityFrameworkCore;
using Npgsql;
using Rias.Core.Commons;
using Rias.Core.Database.Models;

namespace Rias.Core.Database
{
    // public class RiasContextFactory : IDesignTimeDbContextFactory<RiasDbContext>
    // {
    //     public RiasDbContext CreateDbContext(string[] args)
    //     {
    //         var optionsBuilder = new DbContextOptionsBuilder<RiasDbContext>();
    //         optionsBuilder.UseNpgsql("");
    //         optionsBuilder.UseSnakeCaseNamingConvention();
    //         var ctx = new RiasDbContext(optionsBuilder.Options);
    //         return ctx;
    //     }
    // }
    
    public class RiasDbContext : DbContext
    {
        public DbSet<Characters>? Characters { get; set; }
        public DbSet<CustomCharacters>? CustomCharacters { get; set; }
        public DbSet<CustomWaifus>? CustomWaifus { get; set; }
        public DbSet<Guilds>? Guilds { get; set; }
        public DbSet<GuildsXp>? GuildsXp { get; set; }
        public DbSet<GuildUsers>? GuildUsers { get; set; }
        public DbSet<GuildXpRoles>? GuildXpRoles { get; set; }
        public DbSet<MuteTimers>? MuteTimers { get; set; }
        public DbSet<Profile>? Profile { get; set; }
        public DbSet<SelfAssignableRoles>? SelfAssignableRoles { get; set; }
        public DbSet<Users>? Users { get; set; }
        public DbSet<Waifus>? Waifus { get; set; }
        public DbSet<Warnings>? Warnings { get; set; }
        public DbSet<Patreon>? Patreon { get; set; }
        public DbSet<Votes>? Votes { get; set; }

        public RiasDbContext(DbContextOptions<RiasDbContext> options) : base(options)
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<LastChargeStatus>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<PatronStatus>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Characters>()
                .HasIndex(x => x.CharacterId)
                .IsUnique();
            
            modelBuilder.Entity<CustomCharacters>()
                .HasIndex(x => x.CharacterId)
                .IsUnique();
            
            modelBuilder.Entity<CustomWaifus>();
            
            modelBuilder.Entity<Guilds>()
                .HasIndex(x => x.GuildId)
                .IsUnique();

            modelBuilder.Entity<GuildsXp>();
            modelBuilder.Entity<GuildUsers>();
            modelBuilder.Entity<GuildXpRoles>();
            modelBuilder.Entity<MuteTimers>();
            
            modelBuilder.Entity<Profile>()
                .HasIndex(x => x.UserId)
                .IsUnique();
            
            modelBuilder.Entity<SelfAssignableRoles>();
            
            modelBuilder.Entity<Users>()
                .HasIndex(x => x.UserId)
                .IsUnique();

            var waifuEntity = modelBuilder.Entity<Waifus>();

            waifuEntity.HasOne(x => x.Character)
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .HasPrincipalKey(x => x!.CharacterId);

            waifuEntity.HasOne(x => x.CustomCharacter)
                .WithMany()
                .HasForeignKey(x => x.CustomCharacterId)
                .HasPrincipalKey(x => x!.CharacterId);

            modelBuilder.Entity<Warnings>();
        }
    }
}