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
        public string UrbanDictionaryApiKey { get; }
        public string DiscordBotListApiKey { get; }
        public string WeebServicesToken { get; }

        public DatabaseConfig? DatabaseConfig { get; }
        public LavalinkConfig? LavalinkConfig { get; }
        public VotesConfig? VotesConfig { get; }
        public PatreonConfig? PatreonConfig { get; }

        private readonly string _credsPath = Path.Combine(Environment.CurrentDirectory, "data/credentials.json");

        public Credentials()
        {
            var config = new ConfigurationBuilder().AddJsonFile(_credsPath).Build();

            Prefix = config.GetValue<string>(nameof(Prefix));
            Token = config.GetValue<string>(nameof(Token));

            MasterId = config.GetValue<ulong>(nameof(MasterId));
            Currency = config.GetValue<string>(nameof(Currency));

            Invite = config.GetValue<string>(nameof(Invite));
            OwnerServerInvite = config.GetValue<string>(nameof(OwnerServerInvite));
            OwnerServerId = config.GetValue<ulong>(nameof(OwnerServerId));

            var confirmColor = RiasUtils.HexToUint(config.GetValue<string>(nameof(RiasUtils.ConfirmColor)));
            if (confirmColor.HasValue)
                RiasUtils.ConfirmColor = new Color(confirmColor.Value);

            var errorColor = RiasUtils.HexToUint(config.GetValue<string>(nameof(RiasUtils.ErrorColor)));
            if (errorColor.HasValue)
                RiasUtils.ErrorColor = new Color(errorColor.Value);

            Patreon = config.GetValue<string>(nameof(Patreon));
            Website = config.GetValue<string>(nameof(Website));

            UrbanDictionaryApiKey = config.GetValue<string>(nameof(UrbanDictionaryApiKey));
            DiscordBotListApiKey = config.GetValue<string>(nameof(DiscordBotListApiKey));
            WeebServicesToken = config.GetValue<string>(nameof(WeebServicesToken));

            var databaseConfig = config.GetSection(nameof(DatabaseConfig));
            DatabaseConfig = !databaseConfig.Exists() ? null : new DatabaseConfig
            {
                Host = databaseConfig.GetValue<string>(nameof(DatabaseConfig.Host)),
                Port = databaseConfig.GetValue<ushort>(nameof(DatabaseConfig.Port)),
                Database = databaseConfig.GetValue<string>(nameof(DatabaseConfig.Database)),
                Username = databaseConfig.GetValue<string>(nameof(DatabaseConfig.Username)),
                Password = databaseConfig.GetValue<string>(nameof(DatabaseConfig.Password)),
                ApplicationName = databaseConfig.GetValue<string>(nameof(DatabaseConfig.ApplicationName))
            };

            var lavalinkConfig = config.GetSection(nameof(LavalinkConfig));
            LavalinkConfig = !lavalinkConfig.Exists() ? null : new LavalinkConfig
            {
                Host = lavalinkConfig.GetValue<string>(nameof(LavalinkConfig.Host)),
                Port = lavalinkConfig.GetValue<ushort>(nameof(LavalinkConfig.Port)),
                Authorization = lavalinkConfig.GetValue<string>(nameof(LavalinkConfig.Authorization))
            };

            var votesConfig = config.GetSection(nameof(VotesConfig));
            VotesConfig = !votesConfig.Exists() ? null : new VotesConfig
            {
                WebSocketHost = votesConfig.GetValue<string>(nameof(VotesConfig.WebSocketHost)),
                WebSocketPort = votesConfig.GetValue<ushort>(nameof(VotesConfig.WebSocketPort)),
                IsSecureConnection = votesConfig.GetValue<bool>(nameof(VotesConfig.IsSecureConnection)),
                UrlParameters = votesConfig.GetValue<string>(nameof(VotesConfig.UrlParameters)),
                Authorization = votesConfig.GetValue<string>(nameof(VotesConfig.Authorization))
            };
            
            var patreonConfig = config.GetSection(nameof(PatreonConfig));
            PatreonConfig = !votesConfig.Exists() ? null : new PatreonConfig
            {
                WebSocketHost = patreonConfig.GetValue<string>(nameof(PatreonConfig.WebSocketHost)),
                WebSocketPort = patreonConfig.GetValue<ushort>(nameof(PatreonConfig.WebSocketPort)),
                IsSecureConnection = patreonConfig.GetValue<bool>(nameof(PatreonConfig.IsSecureConnection)),
                UrlParameters = patreonConfig.GetValue<string>(nameof(PatreonConfig.UrlParameters)),
                Authorization = patreonConfig.GetValue<string>(nameof(PatreonConfig.Authorization))
            };
        }
    }
}