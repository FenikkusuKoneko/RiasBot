using System;
using System.Linq;
using Rias.Core.Attributes;
using Rias.Core.Database;
using Rias.Core.Implementation;

namespace Rias.Core.Services
{
    public class DbService : RiasService
    {
        [Inject] private readonly Credentials _creds;
        
        public DbService(IServiceProvider services) : base(services)
        {
        }

        public TEntity FirstOrDefault<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
        {
            using (var db = new RiasDbContext(_creds))
            {
                return db.Set<TEntity>().FirstOrDefault(predicate);
            }
        }
    }
}