using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;

namespace Rias.Core.Services
{
    public class DatabaseService : RiasService
    {
        public DatabaseService(IServiceProvider services) : base(services) {}

        public async Task DeleteUserAsync(IUser user)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
            if (userDb != null)
                db.Remove(userDb);
            
            var waifusDb = db.Waifus.Where(x => x.UserId == user.Id);
            if (waifusDb.Any())
                db.RemoveRange(waifusDb);
            
            var profileDb = db.Profile.FirstOrDefault(x => x.UserId == user.Id);
            if (profileDb != null)
                db.Remove(profileDb);
            
            await db.SaveChangesAsync();
        }

        public Users GetUserDb(IUser user)
        {
            using var db = Services.GetRequiredService<RiasDbContext>();
            return db.Users.FirstOrDefault(x => x.UserId == user.Id);
        }

        public async Task AddBlacklistAsync(IUser user)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
            if (userDb != null)
            {
                userDb.IsBlacklisted = true;
            }
            else
            {
                var userConfig = new Users { UserId = user.Id, IsBlacklisted = true };
                await db.AddAsync(userConfig);
            }
            
            await db.SaveChangesAsync();
        }
        
        public async Task RemoveBlacklistAsync(IUser user)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
            if (userDb != null)
            {
                userDb.IsBlacklisted = false;
            }
            else
            {
                var userConfig = new Users { UserId = user.Id, IsBlacklisted = false };
                await db.AddAsync(userConfig);
            }
            
            await db.SaveChangesAsync();
        }
        
        public async Task AddBotBanAsync(IUser user)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
            if (userDb != null)
            {
                userDb.IsBlacklisted = true;
                userDb.IsBanned = true;
            }
            else
            {
                var userConfig = new Users { UserId = user.Id, IsBlacklisted = true, IsBanned = true };
                await db.AddAsync(userConfig);
            }
            
            await db.SaveChangesAsync();
        }
        
        public async Task RemoveBotBanAsync(IUser user)
        {
            await using var db = Services.GetRequiredService<RiasDbContext>();
            var userDb = db.Users.FirstOrDefault(x => x.UserId == user.Id);
            if (userDb != null)
            {
                userDb.IsBanned = false;
            }
            else
            {
                var userConfig = new Users { UserId = user.Id, IsBanned = false };
                await db.AddAsync(userConfig);
            }
            
            await db.SaveChangesAsync();
        }
    }
}