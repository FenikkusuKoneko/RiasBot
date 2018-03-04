using RiasBot.Services.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace RiasBot.Services
{
    public class DbService : IRService
    {
        private readonly DbContextOptions<KurumiContext> options;
        private readonly DbContextOptions<KurumiContext> migrateOptions;

        public DbService()
        {
            var builder = new SqliteConnectionStringBuilder("Data Source=data/RiasBot.db");
            builder.DataSource = Path.Combine(Environment.CurrentDirectory, builder.DataSource);

            var optionsBuilder = new DbContextOptionsBuilder<KurumiContext>();
            optionsBuilder.UseSqlite(builder.ToString());
            options = optionsBuilder.Options;

            optionsBuilder = new DbContextOptionsBuilder<KurumiContext>();
            optionsBuilder.UseSqlite(builder.ToString(), x => x.SuppressForeignKeyEnforcement());
            migrateOptions = optionsBuilder.Options;
        }

        public KurumiContext GetDbContext()
        {
            var context = new KurumiContext(options);
            if (context.Database.GetPendingMigrations().Any())
            {
                var mContext = new KurumiContext(migrateOptions);
                mContext.Database.Migrate();
                mContext.SaveChanges();
                mContext.Dispose();
            }

            return context;
        }
    }
}
