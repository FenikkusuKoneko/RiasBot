namespace Rias.Core.Configuration
{
    public class DatabaseConfiguration
    {
        public string? Host { get; set; }
        public ushort Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Database { get; set; }
        public string? ApplicationName { get; set; }
    }
}