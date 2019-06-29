namespace Rias.Core.Commons.Configs
{
    public class VotesManagerConfig
    {
        public string WebSocketHost { get; set; }
        public ushort WebSocketPort { get; set; }
        public bool IsSecureConnection { get; set; }
        public string UrlParameters { get; set; }
        public string Authorization { get; set; }
    }
}