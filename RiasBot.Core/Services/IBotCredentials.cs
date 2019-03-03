using System;
using System.Collections.Generic;
using System.Text;
using RiasBot.Commons;

namespace RiasBot.Services
{
    public interface IBotCredentials
    {
        string Prefix { get; }
        string Token { get; }
        string GoogleApiKey { get; }
        string UrbanDictionaryApiKey { get; }
        string PatreonAccessToken { get; }
        string DiscordBotsListApiKey { get; }
        string WeebServicesToken { get; }
        DatabaseConfig DatabaseConfig { get; }
        LavalinkConfig LavalinkConfig { get; }
        VotesManagerConfig VotesManagerConfig { get; }
        bool IsBeta { get; }    //beta bool is too protect things to run only on the public version, like apis
    }
    
    public class DatabaseConfig
    {
        public string Host { get; set; }
        public ushort Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
    
    public class LavalinkConfig
    {
        public string Host { get; set; }
        public ushort Port { get; set; }
        public string Authorization { get; set; }
    }
    
    public class VotesManagerConfig
    {
        public string WebSocketHost { get; }
        public ushort WebSocketPort { get; }
        public bool IsSecureConnection { get; }
        public string UrlParameters { get; }
        public string Authorization { get; }

        public VotesManagerConfig(string webSocketHost, ushort webSocketPort, bool isSecureConnection, string urlParameters, string authorization)
        {
            WebSocketHost = webSocketHost;
            WebSocketPort = webSocketPort;
            IsSecureConnection = isSecureConnection;
            UrlParameters = urlParameters;
            Authorization = authorization;
        }
    }
}
