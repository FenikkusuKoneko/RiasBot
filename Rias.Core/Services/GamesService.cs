using System;

namespace Rias.Core.Services
{
    public class GamesService : RiasService
    {
        public GamesService(IServiceProvider services) : base(services)
        {
        }
        
        public Rps Choose()
        {
            var random = new Random();
            return (Rps) random.Next(1, 4);
        }
        
        public enum Rps
        {
            Rock = 1,
            Paper = 2,
            Scissors = 3
        }
    }
}