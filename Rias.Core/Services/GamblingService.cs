using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;

namespace Rias.Core.Services
{
    public class GamblingService : RiasService
    {
        public GamblingService(IServiceProvider services) : base(services) {}

        public const int MinimumBet = 5;
        public const int MaximumBet = 5000;

        public readonly string[] Arrows = {"⬆", "↗", "➡", "↘", "⬇", "↙", "⬅", "↖"};
        public readonly float[] Multipliers = {1.7f, 2.0f, 1.2f, 0.5f, 0.3f, 0.0f, 0.2f, 1.5f};

        public int GetUserCurrency(SocketUser user)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return db.Users.FirstOrDefault(x => x.UserId == user.Id)?.Currency ?? 0;
        }

        public async Task AddUserCurrencyAsync(ulong userId, int currency)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.GetOrAddAsync(x => x.UserId == userId, () => new Users {UserId = userId});
            userDb.Currency += currency;
            await db.SaveChangesAsync();
        }
        
        public async Task<int> RemoveUserCurrencyAsync(IUser user, int currency)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var userDb = await db.Users.FirstOrDefaultAsync(x => x.UserId == user.Id);
            
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