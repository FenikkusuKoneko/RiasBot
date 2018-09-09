using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RiasBot.Commons;

namespace RiasBot.Services.Implementation
{
    public class BotCredentials : IBotCredentials
    {
        public string Prefix { get; }
        public string Token { get; }
        public string GoogleApiKey { get; }
        public string UrbanDictionaryApiKey { get; }
        public string PatreonAccessToken { get; }
        public string DiscordBotsListApiKey { get; }
        public string WeebServicesToken { get; }
        public LavalinkConfig LavalinkConfig { get; }
        public bool IsBeta { get; }    //beta bool is too protect things to run only on the public version, like apis

        private readonly string _credsFileName = Path.Combine(Environment.CurrentDirectory, "data/credentials.json");
        public BotCredentials()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile(_credsFileName);

            var config = configBuilder.Build();

            Prefix = config[nameof(Prefix)];
            Token = config[nameof(Token)];
            GoogleApiKey = config[nameof(GoogleApiKey)];
            UrbanDictionaryApiKey = config[nameof(UrbanDictionaryApiKey)];
            PatreonAccessToken = config[nameof(PatreonAccessToken)];
            DiscordBotsListApiKey = config[nameof(DiscordBotsListApiKey)];
            WeebServicesToken = config[nameof(WeebServicesToken)];

            var lavalinkConfig = config.GetSection(nameof(LavalinkConfig));
            LavalinkConfig = new LavalinkConfig(lavalinkConfig["RestHost"], ushort.Parse(lavalinkConfig["RestPort"]),
                lavalinkConfig["WebSocketHost"], ushort.Parse(lavalinkConfig["WebSocketPort"]),
                lavalinkConfig["Authorization"]);
            IsBeta = config.GetValue<bool>(nameof(IsBeta));
        }
    }
}
