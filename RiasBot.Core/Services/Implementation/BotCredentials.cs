using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RiasBot.Services.Implementation
{
    public class BotCredentials : IBotCredentials
    {
        public ulong ClientId { get; }
        public string Prefix { get; }
        public string Token { get; }
        public string GoogleApiKey { get; }
        public string UrbanDictionaryApiKey { get; }
        public string PatreonAccessToken { get; }
        public string DiscordBotsListApiKey { get; }
        public string ImgurClientID { get; } //this client id is from koneko who manage the lists of reactions images
        public string HelpDM { get; }

        private readonly string _credsFileName = Path.Combine(Environment.CurrentDirectory, "data/credentials.json");
        public BotCredentials()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(_credsFileName);

            var _config = configBuilder.Build();

            UInt64.TryParse(_config[nameof(ClientId)], out var clientId);
            ClientId = clientId;
            Prefix = _config[nameof(Prefix)];
            Token = _config[nameof(Token)];
            GoogleApiKey = _config[nameof(GoogleApiKey)];
            UrbanDictionaryApiKey = _config[nameof(UrbanDictionaryApiKey)];
            PatreonAccessToken = _config[nameof(PatreonAccessToken)];
            DiscordBotsListApiKey = _config[nameof(DiscordBotsListApiKey)];
            ImgurClientID = _config[nameof(ImgurClientID)];
            HelpDM = _config[nameof(HelpDM)];
        }
    }
}
