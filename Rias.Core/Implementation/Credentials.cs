using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Discord;
using Microsoft.Extensions.Configuration;
using Rias.Core.Commons.Configs;
using Serilog;

namespace Rias.Core.Implementation
{
    public class Credentials
    {
        public readonly string Prefix;
        public readonly string Token;

        public ulong MasterId { get; }
        public string Currency { get; }

        public readonly string Invite;
        public readonly string OwnerServerInvite;
        public ulong OwnerServerId { get; }

        public readonly string Patreon;
        public readonly string Website;
        public readonly string DiscordBotList;

        public readonly string UrbanDictionaryApiKey;
        public readonly string DiscordBotListToken;
        public readonly string WeebServicesToken;

        public readonly DatabaseConfig? DatabaseConfig;
        public readonly VotesConfig? VotesConfig;
        public readonly PatreonConfig? PatreonConfig;

        public readonly string GlobalPassword;
        public bool IsGlobal { get; }

        private readonly string _hash = "5700A1E58C59A2B9297478473CCD078ACAE139ED0D40734347F141A35566A7CE03C336D4575E09788839731AEB235A2A6E0C17744E0CEB64B6119D34F3300D8F";
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
            DiscordBotList = config.GetValue<string>(nameof(DiscordBotList));

            UrbanDictionaryApiKey = config.GetValue<string>(nameof(UrbanDictionaryApiKey));
            DiscordBotListToken = config.GetValue<string>(nameof(DiscordBotListToken));
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
            
            GlobalPassword = config.GetValue<string>(nameof(GlobalPassword));
            if (string.IsNullOrEmpty(GlobalPassword))
                return;
            
            using var sha = SHA512.Create();
            var hash = GetHash(sha, GlobalPassword);

            IsGlobal = hash.Equals(_hash, StringComparison.OrdinalIgnoreCase);
            Log.Information($"IsGlobal: {IsGlobal}");
        }
        
        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            
            foreach (var b in data)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}