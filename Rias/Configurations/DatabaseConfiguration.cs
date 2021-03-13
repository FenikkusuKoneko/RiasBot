namespace Rias.Configurations
{
    public class DatabaseConfiguration
    {
        public string? Host { get; init; }
        
        public ushort Port { get; init; }
        
        public string? Username { get; init; }
        
        public string? Password { get; init; }
        
        public string? Database { get; init; }
        
        public string? ApplicationName { get; init; }
    }
}