namespace Rias.Configuration
{
    public interface IWebSocketConfiguration
    {
        public string? WebSocketHost { get; }
        
        public ushort WebSocketPort { get; }
        
        public bool IsSecureConnection { get; }
        
        public string? UrlParameters { get; }
        
        public string? Authorization { get; }
    }
}