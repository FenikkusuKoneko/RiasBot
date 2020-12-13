using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Database;
using Rias.Database.Entities;

namespace Rias.Services
{
    public class GamblingService : RiasService
    {
        public const int MinimumBet = 5;
        public const int MaximumBet = 5000;

        public static readonly string[] Arrows = { "⬆", "↗", "➡", "↘", "⬇", "↙", "⬅", "↖" };
        public static readonly float[] Multipliers = { 1.7f, 2.0f, 1.2f, 0.5f, 0.3f, 0.0f, 0.2f, 1.5f };
        
        public GamblingService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
        
        /// <summary>
        /// Gets the user's currency.
        /// </summary>
        public async Task<int> GetUserCurrencyAsync(ulong userId)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return (await db.Users.FirstOrDefaultAsync(x => x.UserId == userId))?.Currency ?? 0;
        }

        /// <summary>
        /// Adds currency to the user and returns the new currency.
        /// </summary>
        public async Task<int> AddUserCurrencyAsync(ulong userId, int currency)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.GetOrAddAsync(x => x.UserId == userId, () => new UserEntity { UserId = userId });
            userDb.Currency += currency;
            await db.SaveChangesAsync();
            return userDb.Currency;
        }
        
        /// <summary>
        /// Remove currency from the user and returns the new currency.
        /// </summary>
        public async Task<int> RemoveUserCurrencyAsync(ulong userId, int currency)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId);
            
            if (userDb == null) return 0;

            userDb.Currency -= Math.Min(currency, userDb.Currency);
            await db.SaveChangesAsync();
            return userDb.Currency;
        }
    }
}