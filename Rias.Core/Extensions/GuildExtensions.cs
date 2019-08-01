using Discord;

namespace Rias.Core.Extensions
{
    public static class GuildExtensions
    {
        public static int GetGuildEmotesSlots(this IGuild guild)
        {
            return guild.PremiumTier switch
            {
                PremiumTier.Tier1 => 100,
                PremiumTier.Tier2 => 150,
                PremiumTier.Tier3 => 250,
                _ => 50    //default is PremiumTier.None
            };
        }
    }
}