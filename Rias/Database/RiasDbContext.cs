using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using Rias.Database.Entities;
using Rias.Models;

namespace Rias.Database
{
    public class RiasContextFactory : IDesignTimeDbContextFactory<RiasDbContext>
    {
        public RiasDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RiasDbContext>();
            optionsBuilder.UseNpgsql(args[0]);
            optionsBuilder.UseSnakeCaseNamingConvention();
            var ctx = new RiasDbContext(optionsBuilder.Options);
            return ctx;
        }
    }
    
    public class RiasDbContext : DbContext
    {
        public RiasDbContext(DbContextOptions<RiasDbContext> options)
            : base(options)
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<LastChargeStatus>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<PatronStatus>();
        }
        
#nullable disable
        public DbSet<CharacterEntity> Characters { get; set; }
        
        public DbSet<CustomCharacterEntity> CustomCharacters { get; set; }
        
        public DbSet<CustomWaifuEntity> CustomWaifus { get; set; }
        
        public DbSet<GuildEntity> Guilds { get; set; }
        
        public DbSet<GuildXpRoleEntity> GuildXpRoles { get; set; }
        
        public DbSet<MembersEntity> Members { get; set; }
        
        public DbSet<MuteTimerEntity> MuteTimers { get; set; }
        
        public DbSet<PatreonEntity> Patreon { get; set; }
        
        public DbSet<ProfileEntity> Profile { get; set; }
        
        public DbSet<SelfAssignableRoleEntity> SelfAssignableRoles { get; set; }
        
        public DbSet<UserEntity> Users { get; set; }
        
        public DbSet<VoteEntity> Votes { get; set; }
        
        public DbSet<WaifuEntity> Waifus { get; set; }
        
        public DbSet<WarningEntity> Warnings { get; set; }
#nullable enable
        
        public async Task<TEntity> GetOrAddAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, Func<TEntity> entityValue)
            where TEntity : class
        {
            var entity = await Set<TEntity>().FirstOrDefaultAsync(predicate);
            if (entity != null)
                return entity;

            var newEntity = entityValue();
            await AddAsync(newEntity);
            
            return newEntity;
        }

        public Task<List<TEntity>> GetOrderedListAsync<TEntity, TKey>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TKey>> keySelector, bool descending = false, Range? range = null)
            where TEntity : class
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

        public Task<List<TEntity>> GetOrderedListAsync<TEntity, TKey>(Expression<Func<TEntity, TKey>> keySelector, bool descending = false, Range? range = null)
            where TEntity : class
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

        public Task<List<TEntity>> GetListAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
            => Set<TEntity>().Where(predicate).ToListAsync();

        public Task<List<TEntity>> GetListAsync<TEntity, TProperty1, TProperty2>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TProperty1>> include1,
            Expression<Func<TEntity, TProperty2>> include2)
            where TEntity : class
            => Set<TEntity>()
                .Where(predicate)
                .Include(include1)
                .Include(include2)
                .ToListAsync();

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is DbEntity && e.State is EntityState.Modified);

            foreach (var entityEntry in entries)
                ((DbEntity) entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<LastChargeStatus>();
            modelBuilder.HasPostgresEnum<PatronStatus>();
            
            modelBuilder.Entity<CharacterEntity>()
                .HasIndex(x => x.CharacterId)
                .IsUnique();
            
            modelBuilder.Entity<CustomCharacterEntity>()
                .HasIndex(x => x.CharacterId)
                .IsUnique();
            
            modelBuilder.Entity<CustomWaifuEntity>();
            
            modelBuilder.Entity<GuildEntity>()
                .HasIndex(x => x.GuildId)
                .IsUnique();
            
            modelBuilder.Entity<GuildXpRoleEntity>()
                .HasIndex(x => new { x.GuildId, x.RoleId })
                .IsUnique();
            
            modelBuilder.Entity<MembersEntity>()
                .HasIndex(x => new { x.GuildId, UserId = x.MemberId })
                .IsUnique();
            
            modelBuilder.Entity<MuteTimerEntity>()
                .HasIndex(x => new { x.GuildId, x.UserId })
                .IsUnique();
            
            modelBuilder.Entity<ProfileEntity>()
                .HasIndex(x => x.UserId)
                .IsUnique();
            
            modelBuilder.Entity<SelfAssignableRoleEntity>()
                .HasIndex(x => new { x.GuildId, x.RoleId })
                .IsUnique();
            
            modelBuilder.Entity<UserEntity>()
                .HasIndex(x => x.UserId)
                .IsUnique();

            var waifuEntity = modelBuilder.Entity<WaifuEntity>();

            waifuEntity.HasOne(x => x.Character)
                .WithMany()
                .HasForeignKey(x => x.CharacterId)
                .HasPrincipalKey(x => x!.CharacterId);

            waifuEntity.HasOne(x => x.CustomCharacter)
                .WithMany()
                .HasForeignKey(x => x.CustomCharacterId)
                .HasPrincipalKey(x => x!.CharacterId);

            modelBuilder.Entity<WarningEntity>();
        }
    }
}