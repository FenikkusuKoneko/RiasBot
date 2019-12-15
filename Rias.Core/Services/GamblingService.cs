using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
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
        
        public Users? GetUser(SocketUser user)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Users.FirstOrDefault(x => x.UserId == user.Id);
        }

        public int GetUserCurrency(SocketUser user)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Users.FirstOrDefault(x => x.UserId == user.Id)?.Currency ?? 0;
        }

        public IList<Users> GetUsersCurrency(int page, int amount)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Users.OrderByDescending(x => x.Currency).Skip(page * amount).Take(amount).ToList();
        }

        public async Task<int> AddUserCurrencyAsync(ulong userId, int currency)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == userId);

            int newCurrency;
            if (userDb != null)
            {
                newCurrency = userDb.Currency += currency;
            }
            else
            {
                var newUserDb = new Users {UserId = userId, Currency = currency};
                await db.AddAsync(newUserDb);
                newCurrency = currency;
            }
            
            await db.SaveChangesAsync();
            return newCurrency;
        }
        
        public async Task<int> RemoveUserCurrencyAsync(IUser user, int currency)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
            
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

        public async Task UpdateDailyAsync(IUser user, DateTime dateTime)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
            
            if (userDb != null)
            {
                userDb.DailyTaken = dateTime;
            }
            else
            {
                var newUserDb = new Users { UserId = user.Id, DailyTaken = dateTime};
                await db.AddAsync(newUserDb);
            }

            await db.SaveChangesAsync();
        }
    }
}