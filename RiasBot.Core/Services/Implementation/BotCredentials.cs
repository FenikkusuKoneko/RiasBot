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
        public string WeebServicesToken { get; }
        public string HelpDM { get; }

        private readonly string _credsFileName = Path.Combine(Environment.CurrentDirectory, "data/credentials.json");
        public BotCredentials()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(_credsFileName);

            var config = configBuilder.Build();

            UInt64.TryParse(config[nameof(ClientId)], out var clientId);
            ClientId = clientId;
            Prefix = config[nameof(Prefix)];
            Token = config[nameof(Token)];
            GoogleApiKey = config[nameof(GoogleApiKey)];
            UrbanDictionaryApiKey = config[nameof(UrbanDictionaryApiKey)];
            PatreonAccessToken = config[nameof(PatreonAccessToken)];
            DiscordBotsListApiKey = config[nameof(DiscordBotsListApiKey)];
            WeebServicesToken = config[nameof(WeebServicesToken)];
            HelpDM = config[nameof(HelpDM)];
        }
    }
}
