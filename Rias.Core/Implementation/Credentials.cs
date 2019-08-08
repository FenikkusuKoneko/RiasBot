using System;
using System.IO;
using Discord;
using Microsoft.Extensions.Configuration;
using Rias.Core.Commons.Configs;

namespace Rias.Core.Implementation
{
    public class Credentials
    {
        public string Prefix { get; }
        public string Token { get; }

        public ulong MasterId { get; }
        public string Currency { get; }

        public string Invite { get; }
        public string OwnerServerInvite { get; }
        public ulong OwnerServerId { get; }

        public string Patreon { get; }
        public string Website { get; }
        public string WeebApi { get; }
        public string GoogleApiKey { get; }
        public string UrbanDictionaryApiKey { get; }
        public string PatreonAccessToken { get; }
        public string DiscordBotsListApiKey { get; }
        public string WeebServicesToken { get; }

        public DatabaseConfig DatabaseConfig { get; }
        public LavalinkConfig LavalinkConfig { get; }
        public VotesManagerConfig VotesManagerConfig { get; }
        public bool IsBeta { get; } //beta bool is to protect things to run only on the public version, like apis

        private readonly string _credsPath = Path.Combine(Environment.CurrentDirectory, "data/credentials.json");

        public Credentials()
        {
            var config = new ConfigurationBuilder().AddJsonFile(_credsPath).Build();

            Prefix = config[nameof(Prefix)];
            Token = config[nameof(Token)];

            MasterId = ulong.TryParse(config[nameof(MasterId)], out var masterId) ? masterId : 0;
            Currency = config[nameof(Currency)];

            Invite = config[nameof(Invite)];
            OwnerServerInvite = config[nameof(OwnerServerInvite)];
            OwnerServerId = ulong.TryParse(config[nameof(OwnerServerId)], out var ownerServerId) ? ownerServerId : 0;

            var confirmColor = RiasUtils.HexToUint(config[nameof(RiasUtils.ConfirmColor)]);
            if (confirmColor != 0xFFFFFF)
                RiasUtils.ConfirmColor = new Color(confirmColor);

            var errorColor = RiasUtils.HexToUint(config[nameof(RiasUtils.ErrorColor)]);
            if (errorColor != 0xFFFFFF)
                RiasUtils.ErrorColor = new Color(errorColor);

            Patreon = config[nameof(Patreon)];
            Website = config[nameof(Website)];
            WeebApi = config[nameof(WeebApi)];

            GoogleApiKey = config[nameof(GoogleApiKey)];
            UrbanDictionaryApiKey = config[nameof(UrbanDictionaryApiKey)];
            PatreonAccessToken = config[nameof(PatreonAccessToken)];
            DiscordBotsListApiKey = config[nameof(DiscordBotsListApiKey)];
            WeebServicesToken = config[nameof(WeebServicesToken)];

            var databaseConfig = config.GetSection(nameof(DatabaseConfig));
            DatabaseConfig = new DatabaseConfig
            {
                Host = databaseConfig.GetValue<string>("Host"),
                Port = databaseConfig.GetValue<ushort>("Port"),
                Database = databaseConfig.GetValue<string>("Database"),
                Username = databaseConfig.GetValue<string>("Username"),
                Password = databaseConfig.GetValue<string>("Password")
            };

            var lavalinkConfig = config.GetSection(nameof(LavalinkConfig));
            LavalinkConfig = new LavalinkConfig
            {
                Host = lavalinkConfig.GetValue<string>("Host"),
                Port = lavalinkConfig.GetValue<int>("Port"),
                Password = lavalinkConfig.GetValue<string>("Password")
            };

            var votesManagerConfig = config.GetSection(nameof(VotesManagerConfig));
            VotesManagerConfig = new VotesManagerConfig
            {
                WebSocketHost = votesManagerConfig.GetValue<string>("WebSocketHost"),
                WebSocketPort = votesManagerConfig.GetValue<ushort>("WebSocketPort"),
                IsSecureConnection = votesManagerConfig.GetValue<bool>("IsSecureConnection"),
                UrlParameters = votesManagerConfig.GetValue<string>("UrlParameters"),
                Authorization = votesManagerConfig.GetValue<string>("Authorization")
            };

            IsBeta = config.GetValue<bool>(nameof(IsBeta));
        }
    }
}