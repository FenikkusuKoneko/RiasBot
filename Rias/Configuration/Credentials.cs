using System;
using System.IO;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Rias.Implementation;

namespace Rias.Configuration
{
    public class Credentials
    {
        public string Prefix { get; private set; } = null!;
        public string Token { get; private set; } = null!;

        public ulong MasterId { get; private set; }
        public string Currency { get; private set; } = null!;

        public string? Invite { get; private set; }
        public string? OwnerServerInvite { get; private set; }
        public ulong OwnerServerId { get; private set; }

        
        // TODO: If Patreon is null then disable related commands
        public string Patreon { get; private set; } = null!;
        public string? Website { get; private set; }
        public string? DiscordBotList { get; private set; }

        public string? UrbanDictionaryApiKey { get; private set; }
        public string? FixerAccessKey { get; private set; }
        public string? DiscordBotListToken { get; private set; }
        public string? DiscordBotsToken { get; private set; }
        public string? WeebServicesToken { get; private set; }

        public DatabaseConfiguration? DatabaseConfig { get; private set; }
        public VotesConfiguration? VotesConfig { get; private set; }
        public PatreonConfiguration? PatreonConfig { get; private set; }
        
        private string _credsPath = Path.Combine(Environment.CurrentDirectory, "data/credentials.json");

        public Credentials()
        {
            LoadCredentials();
        }

        public void LoadCredentials()
        {
            var config = new ConfigurationBuilder().AddJsonFile(_credsPath).Build();

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
            FixerAccessKey = config.GetValue<string>(nameof(FixerAccessKey));
            DiscordBotListToken = config.GetValue<string>(nameof(DiscordBotListToken));
            DiscordBotsToken = config.GetValue<string>(nameof(DiscordBotsToken));
            WeebServicesToken = config.GetValue<string>(nameof(WeebServicesToken));

            var databaseConfig = config.GetSection(nameof(DatabaseConfig));
            DatabaseConfig = !databaseConfig.Exists() ? null : new DatabaseConfiguration
            {
                Host = databaseConfig.GetValue<string>(nameof(DatabaseConfig.Host)),
                Port = databaseConfig.GetValue<ushort>(nameof(DatabaseConfig.Port)),
                Database = databaseConfig.GetValue<string>(nameof(DatabaseConfig.Database)),
                Username = databaseConfig.GetValue<string>(nameof(DatabaseConfig.Username)),
                Password = databaseConfig.GetValue<string>(nameof(DatabaseConfig.Password)),
                ApplicationName = databaseConfig.GetValue<string>(nameof(DatabaseConfig.ApplicationName))
            };

            var votesConfig = config.GetSection(nameof(VotesConfig));
            VotesConfig = !votesConfig.Exists() ? null : new VotesConfiguration
            {
                WebSocketHost = votesConfig.GetValue<string>(nameof(VotesConfig.WebSocketHost)),
                WebSocketPort = votesConfig.GetValue<ushort>(nameof(VotesConfig.WebSocketPort)),
                IsSecureConnection = votesConfig.GetValue<bool>(nameof(VotesConfig.IsSecureConnection)),
                UrlParameters = votesConfig.GetValue<string>(nameof(VotesConfig.UrlParameters)),
                Authorization = votesConfig.GetValue<string>(nameof(VotesConfig.Authorization))
            };
            
            var patreonConfig = config.GetSection(nameof(PatreonConfig));
            PatreonConfig = !patreonConfig.Exists() ? null : new PatreonConfiguration
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