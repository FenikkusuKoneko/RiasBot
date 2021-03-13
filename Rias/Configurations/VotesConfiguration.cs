namespace Rias.Configurations
{
    public class VotesConfiguration : IWebSocketConfiguration
    {
        public string? WebSocketHost { get; init; }
        
        public ushort WebSocketPort { get; init; }
        
        public bool IsSecureConnection { get; init; }
        
        public string? UrlParameters { get; init; }
        
        public string? Authorization { get; init; }
    }
}