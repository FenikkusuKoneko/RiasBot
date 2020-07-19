using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Rias.Core.Database.Entities;
using Rias.Core.Models;

namespace Rias.Core.Database
{
    // public class RiasContextFactory : IDesignTimeDbContextFactory<RiasDbContext>
    // {
    //     public RiasDbContext CreateDbContext(string[] args)
    //     {
    //         var optionsBuilder = new DbContextOptionsBuilder<RiasDbContext>();
    //         optionsBuilder.UseNpgsql("Host=;Port=;Username=;Password=;Database=");
    //         optionsBuilder.UseSnakeCaseNamingConvention();
    //         var ctx = new RiasDbContext(optionsBuilder.Options);
    //         return ctx;
    //     }
    // }
    
    public class RiasDbContext : DbContext
    {
#nullable disable
        public DbSet<CharactersEntity> Characters { get; set; }
        public DbSet<CustomCharactersEntity> CustomCharacters { get; set; }
        public DbSet<CustomWaifusEntity> CustomWaifus { get; set; }
        public DbSet<GuildsEntity> Guilds { get; set; }
        public DbSet<GuildUsersEntity> GuildUsers { get; set; }
        public DbSet<GuildXpRolesEntity> GuildXpRoles { get; set; }
        public DbSet<MuteTimersEntity> MuteTimers { get; set; }
        public DbSet<ProfileEntity> Profile { get; set; }
        public DbSet<SelfAssignableRolesEntity> SelfAssignableRoles { get; set; }
        public DbSet<UsersEntity> Users { get; set; }
        public DbSet<WaifusEntity> Waifus { get; set; }
        public DbSet<WarningsEntity> Warnings { get; set; }
        public DbSet<PatreonEntity> Patreon { get; set; }
        public DbSet<VotesEntity> Votes { get; set; }
#nullable enable

        public RiasDbContext(DbContextOptions<RiasDbContext> options) : base(options)
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<LastChargeStatus>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<PatronStatus>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<LastChargeStatus>();
            modelBuilder.HasPostgresEnum<PatronStatus>();
            
            modelBuilder.Entity<CharactersEntity>()
                .HasIndex(x => x.CharacterId)
                .IsUnique();
            
            modelBuilder.Entity<CustomCharactersEntity>()
                .HasIndex(x => x.CharacterId)
                .IsUnique();
            
            modelBuilder.Entity<CustomWaifusEntity>();
            
            modelBuilder.Entity<GuildsEntity>()
                .HasIndex(x => x.GuildId)
                .IsUnique();
            
            modelBuilder.Entity<GuildUsersEntity>()
                .HasIndex(x => new {x.GuildId, x.UserId})
                .IsUnique();
            
            modelBuilder.Entity<GuildXpRolesEntity>()
                .HasIndex(x => new {x.GuildId, x.RoleId})
                .IsUnique();
            
            modelBuilder.Entity<MuteTimersEntity>()
                .HasIndex(x => new {x.GuildId, x.UserId})
                .IsUnique();
            
            modelBuilder.Entity<ProfileEntity>()
                .HasIndex(x => x.UserId)
                .IsUnique();
            
            modelBuilder.Entity<SelfAssignableRolesEntity>()
                .HasIndex(x => new {x.GuildId, x.RoleId})
                .IsUnique();
            
            modelBuilder.Entity<UsersEntity>()
                .HasIndex(x => x.UserId)
                .IsUnique();

            var waifuEntity = modelBuilder.Entity<WaifusEntity>();

            waifuEntity.HasOne(x => x.Character)
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .HasPrincipalKey(x => x!.CharacterId);

            waifuEntity.HasOne(x => x.CustomCharacter)
                .WithMany()
                .HasForeignKey(x => x.CustomCharacterId)
                .HasPrincipalKey(x => x!.CharacterId);

            modelBuilder.Entity<WarningsEntity>();
        }

        public async Task<TEntity> GetOrAddAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, Func<TEntity> entityValue) where TEntity : class
        {
            var entity = await Set<TEntity>().FirstOrDefaultAsync(predicate);
            if (entity != null) return entity;

            var newEntity = entityValue();
            await AddAsync(newEntity);
            
            return newEntity;
        }

        public Task<List<TEntity>> GetOrderedListAsync<TEntity, TKey>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TKey>> keySelector, bool descending = false, Range? range = null) where TEntity : class
        {
            var query = Set<TEntity>().Where(predicate);
            var orderedQuery = !descending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);

            IQueryable<TEntity>? finalQuery = null;
            if (range != null)
            {
                var start = range.Value.Start.Value;
                var end = range.Value.End.Value;
                finalQuery = orderedQuery.Skip(start).Take(end - start);
            }

            return finalQuery?.ToListAsync() ?? orderedQuery.ToListAsync();
        }

        public Task<List<TEntity>> GetOrderedListAsync<TEntity, TKey>(Expression<Func<TEntity, TKey>> keySelector, bool descending = false, Range? range = null) where TEntity : class
        {
            var orderedQuery = !descending ? Set<TEntity>().OrderBy(keySelector) : Set<TEntity>().OrderByDescending(keySelector);

            IQueryable<TEntity>? finalQuery = null;
            if (range != null)
            {
                var start = range.Value.Start.Value;
                var end = range.Value.End.Value;
                finalQuery = orderedQuery.Skip(start).Take(end - start);
            }

            return finalQuery?.ToListAsync() ?? orderedQuery.ToListAsync();
        }

        public Task<List<TEntity>> GetListAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class
            => Set<TEntity>().Where(predicate).ToListAsync();

        public Task<List<TEntity>> GetListAsync<TEntity, TProperty1, TProperty2>(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TProperty1>> include1,
            Expression<Func<TEntity, TProperty2>> include2) where TEntity : class
            => Set<TEntity>()
                .Where(predicate)
                .Include(include1)
                .Include(include2)
                .ToListAsync();
    }
}