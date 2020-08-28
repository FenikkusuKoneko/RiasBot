using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Entities;

namespace Rias.Core.Services
{
    public class GamblingService : RiasService
    {
        public GamblingService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        
        public const int MinimumBet = 5;
        public const int MaximumBet = 5000;

        public readonly string[] Arrows = {"⬆", "↗", "➡", "↘", "⬇", "↙", "⬅", "↖"};
        public readonly float[] Multipliers = {1.7f, 2.0f, 1.2f, 0.5f, 0.3f, 0.0f, 0.2f, 1.5f};

        public async Task<int> GetUserCurrencyAsync(ulong userId)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return (await db.Users.FirstOrDefaultAsync(x => x.UserId == userId))?.Currency ?? 0;
        }

        public async Task AddUserCurrencyAsync(ulong userId, int currency)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.GetOrAddAsync(x => x.UserId == userId, () => new UsersEntity {UserId = userId});
            userDb.Currency += currency;
            await db.SaveChangesAsync();
        }
        
        public async Task<int> RemoveUserCurrencyAsync(ulong userId, int currency)
        {
            using var scope = RiasBot.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.Users.FirstOrDefaultAsync(x => x.UserId == userId);
            
            if (userDb == null) return 0;

            var currencyTaken = currency;
            if (currency > userDb.Currency)
            {
                currencyTaken = userDb.Currency;
                userDb.Currency = 0;
            }
            else
            {
                userDb.Currency -= currency;
            }

            await db.SaveChangesAsync();
            return currencyTaken;
        }
    }
}