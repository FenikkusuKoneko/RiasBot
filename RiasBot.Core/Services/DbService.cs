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
        private readonly DbContextOptions<RiasContext> options;
        private readonly DbContextOptions<RiasContext> migrateOptions;

        public DbService()
        {
            var builder = new SqliteConnectionStringBuilder("Data Source=data/RiasBot.db");
            builder.DataSource = Path.Combine(Environment.CurrentDirectory, builder.DataSource);

            var optionsBuilder = new DbContextOptionsBuilder<RiasContext>();
            optionsBuilder.UseSqlite(builder.ToString());
            options = optionsBuilder.Options;

            optionsBuilder = new DbContextOptionsBuilder<RiasContext>();
            optionsBuilder.UseSqlite(builder.ToString(), x => x.SuppressForeignKeyEnforcement());
            migrateOptions = optionsBuilder.Options;
        }

        public RiasContext GetDbContext()
        {
            var context = new RiasContext(options);
            if (context.Database.GetPendingMigrations().Any())
            {
                var mContext = new RiasContext(migrateOptions);
                mContext.Database.Migrate();
                mContext.SaveChanges();
                mContext.Dispose();
            }

            return context;
        }
    }
}
