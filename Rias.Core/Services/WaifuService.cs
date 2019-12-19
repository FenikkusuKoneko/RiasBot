using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Database;
using Rias.Core.Database.Models;

namespace Rias.Core.Services
{
    public class WaifuService : RiasService
    {
        public WaifuService(IServiceProvider services) : base(services)
        {
        }
        
        public const int WaifuStartingPrice = 1000;
        public const int SpecialWaifuPrice = 5000;
        public const int WaifuCreationPrice = 10000;

        public const int WaifuPositionLimit = 1000;

        public IList<IWaifus> GetUserWaifus(SocketUser user)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var waifus = db.Waifus
                .Include(x => x.Character)
                .Include(x => x.CustomCharacter)
                .Where(x => x.UserId == user.Id)
                .AsEnumerable()
                .Cast<IWaifus>()
                .ToList();
            
            waifus.AddRange(db.CustomWaifus.Where(x => x.UserId == user.Id));
            return waifus;
        }

        public IList<Waifus> GetWaifuUsers(int characterId, bool isCustom)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            return isCustom
                ? db.Waifus.Where(x => x.CustomCharacterId == characterId).ToList()
                : db.Waifus.Where(x => x.CharacterId == characterId).ToList();
        }
        
        public IWaifus? GetWaifu(SocketUser user, string name)
        {
            var waifus = GetUserWaifus(user);
            IWaifus? waifu;
            
            if (name.StartsWith("@") && int.TryParse(name[1..], out var id))
            {
                waifu = waifus.FirstOrDefault(x => x is Waifus normalWaifu && normalWaifu.CustomCharacterId == id);
                if (waifu != null)
                    return waifu;
            }

            if (int.TryParse(name, out id))
            {
                waifu = waifus.FirstOrDefault(x => x is Waifus normalWaifu && normalWaifu.CharacterId == id);
                if (waifu != null)
                    return waifu;
            }
            
            waifu = waifus.FirstOrDefault(x =>
                name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .All(y => x.Name!.Contains(y, StringComparison.InvariantCultureIgnoreCase)));
            
            return waifu;
        }

        public async Task AddWaifuAsync(SocketUser user, ICharacter character, int price, int position)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var waifuDb = new Waifus
            {
                UserId = user.Id,
                Price = price,
                Position = position
            };

            if (character is CustomCharacters)
                waifuDb.CustomCharacterId = character.CharacterId;
            else
                waifuDb.CharacterId = character.CharacterId;

            await db.AddAsync(waifuDb);
            await db.SaveChangesAsync();
        }

        public async Task RemoveWaifuAsync(SocketUser user, IWaifus waifu)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            if (waifu is CustomWaifus customWaifu)
                db.Remove(customWaifu);
            else
                db.Remove((Waifus) waifu);

            await db.SaveChangesAsync();
        }
        
        public async Task SetSpecialWaifuAsync(SocketUser user, IWaifus waifu)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var currentSpecialWaifu = (IWaifus) db.Waifus.FirstOrDefault(x => x.UserId == user.Id && x.IsSpecial)
                                      ?? db.CustomWaifus.FirstOrDefault(x => x.UserId == user.Id && x.IsSpecial);

            if (currentSpecialWaifu != null)
            {
                currentSpecialWaifu.IsSpecial = false;
                if (currentSpecialWaifu is Waifus currentWaifuDb)
                    currentWaifuDb.CustomImageUrl = null;
            }

            var waifuDb = waifu is CustomWaifus
                ? db.CustomWaifus.FirstOrDefault(x => x.Id == waifu.Id)
                : (IWaifus) db.Waifus.FirstOrDefault(x => x.Id == waifu.Id);
            
            if (waifuDb != null)
                waifuDb.IsSpecial = true;

            await db.SaveChangesAsync();
        }

        public async Task SetWaifuImageAsync(SocketUser user, IWaifus waifu, string url)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var waifuDb = waifu is CustomWaifus
                ? db.CustomWaifus.FirstOrDefault(x => x.Id == waifu.Id)
                : (IWaifus) db.Waifus.FirstOrDefault(x => x.Id == waifu.Id);

            if (waifuDb != null)
            {
                if (waifuDb is Waifus normalWaifuDb)
                    normalWaifuDb.CustomImageUrl = url;
                else
                    ((CustomWaifus) waifu).ImageUrl = url;
                
                await db.SaveChangesAsync();
            }
        }

        public async Task<int> SetWaifuPositionAsync(SocketUser user, IWaifus waifu, int position)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();
            var waifus = db.Waifus
                .Where(x => x.UserId == user.Id)
                .AsEnumerable()
                .Cast<IWaifus>()
                .ToList();
            
            waifus.AddRange(db.CustomWaifus.Where(x => x.UserId == user.Id));
            
            waifus = waifus.Where(x => x.Position != 0)
                .OrderBy(x => x.Position)
                .Concat(waifus.Where(x => x.Position == 0))
                .ToList();
            
            position = Math.Min(position, waifus.Count);

            var currentWaifu = waifus.FirstOrDefault(x => x.GetType().IsInstanceOfType(waifu) && x.Id == waifu.Id);
            waifus.Remove(currentWaifu);
            
            waifus.Insert(position - 1, currentWaifu);

            for (var i = 0; i < waifus.Count; i++)
                waifus[i].Position = i + 1;

            await db.SaveChangesAsync();
            return position;
        }

        public async Task CreateWaifuAsync(SocketUser user, string name, string url)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiasDbContext>();

            var waifus = GetUserWaifus(user);
            var newCustomWaifuDb = new CustomWaifus
            {
                UserId = user.Id,
                Name = name,
                ImageUrl = url,
                IsSpecial = true,
                Position = waifus.Max(x => x.Position) + 1
            };

            await db.AddAsync(newCustomWaifuDb);
            await db.SaveChangesAsync();
        }
    }
}