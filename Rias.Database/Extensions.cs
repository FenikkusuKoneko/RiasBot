using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Rias.Database.Entities;

namespace Rias.Database;

public static class Extensions
{
    public static async Task<TEntity> GetOrAddAsync<TEntity>(this DbSet<TEntity> dbSet, Expression<Func<TEntity, bool>> predicate, Func<TEntity> factory)
        where TEntity : DbEntity
    {
        var entity = await dbSet.FirstOrDefaultAsync(predicate);

        if (entity is not null)
            return entity;

        entity = factory();
        dbSet.Add(entity);

        return entity;
    }
}