namespace Rias.Core.Database.Models
{
    public class Waifus : DbEntity
    {
        public ulong UserId { get; set; }
        public int WaifuId { get; set; }
        public string WaifuName { get; set; }
        public string WaifuUrl { get; set; }
        public string WaifuImage { get; set; }
        public string BelovedWaifuImage { get; set; }
        public int WaifuPrice { get; set; }
        public bool IsPrimary { get; set; }
    }
}