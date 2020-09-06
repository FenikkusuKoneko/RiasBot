using System;
using System.IO;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Rias.Implementation;

namespace Rias.Configuration
{
    public class Credentials
    {
        private readonly string _prefix;
        private readonly string _token;

        private readonly ulong _masterId;
        private readonly string _currency;

        private readonly string _invite;
        private readonly string _ownerServerInvite;
        private readonly ulong _ownerServerId;

        private readonly string _patreon;
        private readonly string _website;
        private readonly string _discordBotList;

        private readonly string _urbanDictionaryApiKey;
        private readonly string _discordBotListToken;
        private readonly string _weebServicesToken;

        private readonly DatabaseConfiguration? _databaseConfig;
        private readonly VotesConfiguration? _votesConfig;
        private readonly PatreonConfiguration? _patreonConfig;

        private readonly bool _isDevelopment;

        private readonly string _credsPath = Path.Combine(Environment.CurrentDirectory, "data/credentials.json");

        public Credentials()
        {
            var config = new ConfigurationBuilder().AddJsonFile(_credsPath).Build();

            _prefix = config.GetValue<string>(nameof(_prefix));
            _token = config.GetValue<string>(nameof(_token));

            _masterId = config.GetValue<ulong>(nameof(_masterId));
            _currency = config.GetValue<string>(nameof(_currency));

            _invite = config.GetValue<string>(nameof(_invite));
            _ownerServerInvite = config.GetValue<string>(nameof(_ownerServerInvite));
            _ownerServerId = config.GetValue<ulong>(nameof(_ownerServerId));

            var confirmColor = RiasUtilities.HexToInt(config.GetValue<string>(nameof(RiasUtilities.ConfirmColor)));
            if (confirmColor.HasValue)
                RiasUtilities.ConfirmColor = new DiscordColor(confirmColor.Value);

            var errorColor = RiasUtilities.HexToInt(config.GetValue<string>(nameof(RiasUtilities.ErrorColor)));
            if (errorColor.HasValue)
                RiasUtilities.ErrorColor = new DiscordColor(errorColor.Value);

            _patreon = config.GetValue<string>(nameof(_patreon));
            _website = config.GetValue<string>(nameof(_website));
            _discordBotList = config.GetValue<string>(nameof(_discordBotList));

            _urbanDictionaryApiKey = config.GetValue<string>(nameof(_urbanDictionaryApiKey));
            _discordBotListToken = config.GetValue<string>(nameof(_discordBotListToken));
            _weebServicesToken = config.GetValue<string>(nameof(_weebServicesToken));

            var databaseConfig = config.GetSection(nameof(_databaseConfig));
            _databaseConfig = !databaseConfig.Exists() ? null : new DatabaseConfiguration
            {
                Host = databaseConfig.GetValue<string>(nameof(_databaseConfig.Host)),
                Port = databaseConfig.GetValue<ushort>(nameof(_databaseConfig.Port)),
                Database = databaseConfig.GetValue<string>(nameof(_databaseConfig.Database)),
                Username = databaseConfig.GetValue<string>(nameof(_databaseConfig.Username)),
                Password = databaseConfig.GetValue<string>(nameof(_databaseConfig.Password)),
                ApplicationName = databaseConfig.GetValue<string>(nameof(_databaseConfig.ApplicationName))
            };

            var votesConfig = config.GetSection(nameof(_votesConfig));
            _votesConfig = !votesConfig.Exists() ? null : new VotesConfiguration
            {
                WebSocketHost = votesConfig.GetValue<string>(nameof(_votesConfig.WebSocketHost)),
                WebSocketPort = votesConfig.GetValue<ushort>(nameof(_votesConfig.WebSocketPort)),
                IsSecureConnection = votesConfig.GetValue<bool>(nameof(_votesConfig.IsSecureConnection)),
                UrlParameters = votesConfig.GetValue<string>(nameof(_votesConfig.UrlParameters)),
                Authorization = votesConfig.GetValue<string>(nameof(_votesConfig.Authorization))
            };
            
            var patreonConfig = config.GetSection(nameof(_patreonConfig));
            _patreonConfig = !patreonConfig.Exists() ? null : new PatreonConfiguration
            {
                WebSocketHost = patreonConfig.GetValue<string>(nameof(_patreonConfig.WebSocketHost)),
                WebSocketPort = patreonConfig.GetValue<ushort>(nameof(_patreonConfig.WebSocketPort)),
                IsSecureConnection = patreonConfig.GetValue<bool>(nameof(_patreonConfig.IsSecureConnection)),
                UrlParameters = patreonConfig.GetValue<string>(nameof(_patreonConfig.UrlParameters)),
                Authorization = patreonConfig.GetValue<string>(nameof(_patreonConfig.Authorization))
            };
            
            _isDevelopment = config.GetValue<bool>(nameof(_isDevelopment));
        }

        public string Prefix => _prefix;

        public string Token => _token;

        public ulong MasterId => _masterId;

        public string Currency => _currency;

        public string Invite => _invite;

        public string OwnerServerInvite => _ownerServerInvite;

        public ulong OwnerServerId => _ownerServerId;

        public string Patreon => _patreon;

        public string Website => _website;

        public string DiscordBotList => _discordBotList;

        public string UrbanDictionaryApiKey => _urbanDictionaryApiKey;

        public string DiscordBotListToken => _discordBotListToken;

        public string WeebServicesToken => _weebServicesToken;

        public DatabaseConfiguration? DatabaseConfig => _databaseConfig;

        public VotesConfiguration? VotesConfig => _votesConfig;

        public PatreonConfiguration? PatreonConfig => _patreonConfig;

        public bool IsDevelopment => _isDevelopment;
    }
}