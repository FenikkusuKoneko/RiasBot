namespace Rias.Core.Configuration
{
    public interface IWebSocketConfiguration
    {
        public string? WebSocketHost { get; set; }
        public ushort WebSocketPort { get; set; }
        public bool IsSecureConnection { get; set; }
        public string? UrlParameters { get; set; }
        public string? Authorization { get; set; }
    }
}