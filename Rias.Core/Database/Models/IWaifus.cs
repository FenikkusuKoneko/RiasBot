namespace Rias.Core.Database.Models
{
    public interface IWaifus
    {
        public int Id { get; }
        public string? Name { get; }
        public string? ImageUrl { get; }
        public bool IsSpecial { get; set; }
        public int Position { get; set; }
    }
}