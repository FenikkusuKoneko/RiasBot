using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Rias.Attributes;
using Rias.Implementation;
using Rias.Services.Commons;

namespace Rias.Services
{
    [AutoStart]
    public class BlackjackService : RiasService
    {
        public readonly DiscordEmoji CardEmoji = DiscordEmoji.FromUnicode("🎴");
        public readonly DiscordEmoji HandEmoji = DiscordEmoji.FromUnicode("🤚");
        public readonly DiscordEmoji SplitEmoji = DiscordEmoji.FromUnicode("↔");
        
        private readonly GamblingService _gamblingService;
        private readonly ConcurrentDictionary<ulong, BlackjackGame> _sessions = new ConcurrentDictionary<ulong, BlackjackGame>();

        private string? _spadesEmoji;
        private string? _heartsEmoji;
        private string? _clubsEmoji;
        private string? _diamondEmoji;

        public BlackjackService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _gamblingService = serviceProvider.GetRequiredService<GamblingService>();

            RiasBot.Client.MessageReactionAdded += MessageReactionAddedAsync;
            RiasBot.Client.MessageReactionRemoved += MessageReactionRemovedAsync;
            RiasBot.Client.GuildDownloadCompleted += (client, args) =>
            {
                foreach (var (_, guild) in args.Guilds)
                {
                    if (_spadesEmoji is null)
                        if (guild.Emojis.TryGetValue(745275268582735934, out var emoji))
                            _spadesEmoji = emoji.ToString();
                    
                    if (_heartsEmoji is null)
                        if (guild.Emojis.TryGetValue(745274996787773530, out var emoji))
                            _heartsEmoji = emoji.ToString();
                    
                    if (_clubsEmoji is null)
                        if (guild.Emojis.TryGetValue(745274996947157112, out var emoji))
                            _clubsEmoji = emoji.ToString();
                    
                    if (_diamondEmoji is null)
                        if (guild.Emojis.TryGetValue(745274996699561992, out var emoji))
                            _diamondEmoji = emoji.ToString();
                }

                return Task.CompletedTask;
            };
        }
        
        public string SpadesEmoji => _spadesEmoji ?? "♠️";
        
        public string HeartsEmoji => _heartsEmoji ?? "♥️";
        
        public string ClubsEmoji => _clubsEmoji ?? "♣️";
        
        public string DiamondsEmoji => _diamondEmoji ?? "♦️";

        public async Task PlayBlackjackAsync(DiscordMember member, DiscordChannel channel, int bet, string prefix)
        {
            if (!_sessions.TryGetValue(member.Id, out var blackjack))
            {
                blackjack = new BlackjackGame(this, member);
                _sessions[member.Id] = blackjack;
            }

            if (!blackjack.IsRunning)
                await blackjack.CreateGameAsync(channel, bet);
            else
                await ReplyErrorAsync(channel, channel.GuildId, Localization.GamblingBlackjackSession, prefix);
        }

        public async Task ResumeBlackjackAsync(DiscordMember member, DiscordChannel channel)
        {
            if (!_sessions.TryGetValue(member.Id, out var blackjack) || !blackjack.IsRunning)
                await ReplyErrorAsync(channel, channel.GuildId, Localization.GamblingBlackjackNoSession);
            else
                await blackjack.ResumeGameAsync(channel);
        }

        public async Task StopBlackjackAsync(DiscordMember member, DiscordChannel channel)
        {
            if (!_sessions.TryGetValue(member.Id, out var blackjack) || !blackjack.IsRunning)
                await ReplyErrorAsync(channel, channel.GuildId, Localization.GamblingBlackjackNoSession);
            else
            {
                blackjack.StopGame();
                await ReplyConfirmationAsync(channel, channel.GuildId, Localization.GamblingBlackjackStopped);
            }
        }

        /// <summary>
        /// Gets the user's currency.
        /// </summary>
        public Task<int> GetUserCurrencyAsync(ulong userId)
            => _gamblingService.GetUserCurrencyAsync(userId);

        /// <summary>
        /// Adds currency to the user and returns the new currency.
        /// </summary>
        public Task<int> AddUserCurrencyAsync(ulong userId, int currency)
            => _gamblingService.AddUserCurrencyAsync(userId, currency);

        /// <summary>
        /// Remove currency from the user and returns the new currency.
        /// </summary>
        public Task<int> RemoveUserCurrencyAsync(ulong userId, int currency)
            => _gamblingService.RemoveUserCurrencyAsync(userId, currency);

        public void RemoveSession(DiscordMember member)
            => _sessions.TryRemove(member.Id, out _);

        private Task MessageReactionAddedAsync(DiscordClient client, MessageReactionAddEventArgs args)
        {
            if (!_sessions.TryGetValue(args.User.Id, out var blackjack))
                return Task.CompletedTask;

            if (args.Message.Id != blackjack.Message?.Id)
                return Task.CompletedTask;

            if (!blackjack.IsRunning)
                return Task.CompletedTask;

            return RunTaskAsync(ProcessBlackjackAsync(blackjack, args.Emoji));
        }
        
        private Task MessageReactionRemovedAsync(DiscordClient client, MessageReactionRemoveEventArgs args)
        {
            if (!_sessions.TryGetValue(args.User.Id, out var blackjack))
                return Task.CompletedTask;

            if (args.Message.Id != blackjack.Message?.Id)
                return Task.CompletedTask;
            
            if (!blackjack.IsRunning)
                return Task.CompletedTask;
            
            return RunTaskAsync(ProcessBlackjackAsync(blackjack, args.Emoji));
        }

        private async Task ProcessBlackjackAsync(BlackjackGame blackjack, DiscordEmoji emoji)
        {
            if (emoji.Equals(CardEmoji))
                await blackjack.HitAsync();
            else if (emoji.Equals(HandEmoji))
                await blackjack.StandAsync();
            else if (emoji.Equals(SplitEmoji))
                await blackjack.SplitAsync();
        }
    }
}