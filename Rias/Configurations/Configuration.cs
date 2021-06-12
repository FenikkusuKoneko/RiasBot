using System;
using System.IO;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Rias.Implementation;

namespace Rias.Configurations
{
    public class Configuration
    {
        public string Prefix { get; private set; } = null!;
        public string Token { get; private set; } = null!;

        public ulong MasterId { get; private set; }
        public string Currency { get; private set; } = null!;

        public string? Invite { get; private set; }
        public string? OwnerServerInvite { get; private set; }
        public ulong OwnerServerId { get; private set; }
        
        public string Patreon { get; private set; } = null!;
        public string? Website { get; private set; }
        public string? DiscordBotList { get; private set; }

        public string? UrbanDictionaryApiKey { get; private set; }
        public string? ExchangeRateAccessKey { get; private set; }
        public string? DiscordBotListToken { get; private set; }
        public string? DiscordBotsToken { get; private set; }
        public string? WeebServicesToken { get; private set; }

        public DatabaseConfiguration? DatabaseConfiguration { get; private set; }
        public VotesConfiguration? VotesConfiguration { get; private set; }
        public PatreonConfiguration? PatreonConfiguration { get; private set; }
        
        private readonly string _configurationPath = Path.Combine(Environment.CurrentDirectory, "data/configuration.json");

        public Configuration()
        {
            LoadCredentials();
        }

        public void LoadCredentials()
        {
            var config = new ConfigurationBuilder().AddJsonFile(_configurationPath).Build();

            Prefix = config.GetValue<string>(nameof(Prefix));
            Token = config.GetValue<string>(nameof(Token));

            MasterId = config.GetValue<ulong>(nameof(MasterId));
            Currency = config.GetValue<string>(nameof(Currency));

            Invite = config.GetValue<string>(nameof(Invite));
            OwnerServerInvite = config.GetValue<string>(nameof(OwnerServerInvite));
            OwnerServerId = config.GetValue<ulong>(nameof(OwnerServerId));

            var confirmColor = RiasUtilities.HexToInt(config.GetValue<string>(nameof(RiasUtilities.ConfirmColor)));
            if (confirmColor.HasValue)
                RiasUtilities.ConfirmColor = new DiscordColor(confirmColor.Value);

            var errorColor = RiasUtilities.HexToInt(config.GetValue<string>(nameof(RiasUtilities.ErrorColor)));
            if (errorColor.HasValue)
                RiasUtilities.ErrorColor = new DiscordColor(errorColor.Value);

            Patreon = config.GetValue<string>(nameof(Patreon));
            Website = config.GetValue<string>(nameof(Website));
            DiscordBotList = config.GetValue<string>(nameof(DiscordBotList));

            UrbanDictionaryApiKey = config.GetValue<string>(nameof(UrbanDictionaryApiKey));
            ExchangeRateAccessKey = config.GetValue<string>(nameof(ExchangeRateAccessKey));
            DiscordBotListToken = config.GetValue<string>(nameof(DiscordBotListToken));
            DiscordBotsToken = config.GetValue<string>(nameof(DiscordBotsToken));
            WeebServicesToken = config.GetValue<string>(nameof(WeebServicesToken));

            var databaseConfiguration = config.GetSection(nameof(DatabaseConfiguration));
            DatabaseConfiguration = !databaseConfiguration.Exists() ? null : new DatabaseConfiguration
            {
                Host = databaseConfiguration.GetValue<string>(nameof(DatabaseConfiguration.Host)),
                Port = databaseConfiguration.GetValue<ushort>(nameof(DatabaseConfiguration.Port)),
                Database = databaseConfiguration.GetValue<string>(nameof(DatabaseConfiguration.Database)),
                Username = databaseConfiguration.GetValue<string>(nameof(DatabaseConfiguration.Username)),
                Password = databaseConfiguration.GetValue<string>(nameof(DatabaseConfiguration.Password)),
                ApplicationName = databaseConfiguration.GetValue<string>(nameof(DatabaseConfiguration.ApplicationName))
            };

            var votesConfiguration = config.GetSection(nameof(VotesConfiguration));
            VotesConfiguration = !votesConfiguration.Exists() ? null : new VotesConfiguration
            {
                WebSocketHost = votesConfiguration.GetValue<string>(nameof(VotesConfiguration.WebSocketHost)),
                WebSocketPort = votesConfiguration.GetValue<ushort>(nameof(VotesConfiguration.WebSocketPort)),
                IsSecureConnection = votesConfiguration.GetValue<bool>(nameof(VotesConfiguration.IsSecureConnection)),
                UrlParameters = votesConfiguration.GetValue<string>(nameof(VotesConfiguration.UrlParameters)),
                Authorization = votesConfiguration.GetValue<string>(nameof(VotesConfiguration.Authorization))
            };
            
            var patreonConfiguration = config.GetSection(nameof(PatreonConfiguration));
            PatreonConfiguration = !patreonConfiguration.Exists() ? null : new PatreonConfiguration
            {
                WebSocketHost = patreonConfiguration.GetValue<string>(nameof(PatreonConfiguration.WebSocketHost)),
                WebSocketPort = patreonConfiguration.GetValue<ushort>(nameof(PatreonConfiguration.WebSocketPort)),
                IsSecureConnection = patreonConfiguration.GetValue<bool>(nameof(PatreonConfiguration.IsSecureConnection)),
                UrlParameters = patreonConfiguration.GetValue<string>(nameof(PatreonConfiguration.UrlParameters)),
                Authorization = patreonConfiguration.GetValue<string>(nameof(PatreonConfiguration.Authorization))
            };
        }
    }
}