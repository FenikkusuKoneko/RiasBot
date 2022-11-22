using System.Diagnostics.CodeAnalysis;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Rias.Database.Entities;
using Rias.Database.Enums;

namespace Rias.Database;

public class RiasContextFactory : IDesignTimeDbContextFactory<RiasDbContext>
{
    public RiasDbContext CreateDbContext(string[] args)
    {
        var configPath = Path.Combine(Environment.CurrentDirectory, "../Rias/appsettings.Development.json");
        var config = new ConfigurationBuilder().AddJsonFile(configPath).Build();

        var optionsBuilder = new DbContextOptionsBuilder<RiasDbContext>()
            .UseNpgsql(config.GetConnectionString("Database") ?? throw new NullReferenceException("Missing database configuration."))
            .UseSnakeCaseNamingConvention();
            
        return new RiasDbContext(optionsBuilder.Options);
    }
}

public class RiasDbContext : DbContext
{
    [NotNull]
    public DbSet<CharacterEntity>? Characters { get; set; }
    
    [NotNull]
    public DbSet<CustomCharacterEntity>? CustomCharacters { get; set; }
    
    [NotNull]
    public DbSet<CustomWaifuEntity>? CustomWaifus { get; set; }
    
    [NotNull]
    public DbSet<GuildEntity>? Guilds { get; set; }
    
    [NotNull]
    public DbSet<GuildXpRoleEntity>? GuildXpRoles { get; set; }
    
    [NotNull]
    public DbSet<MembersEntity>? Members { get; set; }
    
    [NotNull]
    public DbSet<MuteTimerEntity>? MuteTimers { get; set; }
    
    [NotNull]
    public DbSet<PatreonEntity>? Patreon { get; set; }
    
    [NotNull]
    public DbSet<ProfileEntity>? Profile { get; set; }
    
    [NotNull]
    public DbSet<SelfAssignableRoleEntity>? SelfAssignableRoles { get; set; }
    
    [NotNull]
    public DbSet<UserEntity>? Users { get; set; }
    
    [NotNull]
    public DbSet<VoteEntity>? Votes { get; set; }
    
    [NotNull]
    public DbSet<WaifuEntity>? Waifus { get; set; }
    
    [NotNull]
    public DbSet<WarningEntity>? Warnings { get; set; }
    
    public RiasDbContext(DbContextOptions<RiasDbContext> options)
        : base(options)
    {
        NpgsqlConnection.GlobalTypeMapper.MapEnum<LastChargeStatus>();
        NpgsqlConnection.GlobalTypeMapper.MapEnum<PatronStatus>();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<LastChargeStatus>();
        modelBuilder.HasPostgresEnum<PatronStatus>();
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RiasDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Snowflake>()
            .HaveConversion<SnowflakeConverter>();
    }
}