namespace Rias.Core.Database.Entities
{
    public interface IWaifusEntity
    {
        public int Id { get; }
        public string? Name { get; }
        public string? ImageUrl { get; }
        public bool IsSpecial { get; set; }
        public int Position { get; set; }
    }
}