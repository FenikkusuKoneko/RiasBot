using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Rias.Core.Services.Commons;
using Rias.Interactive;
using Serilog;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace Rias.Core.Services
{
    public class MusicService : RiasService
    {
        public readonly DiscordShardedClient Client;
        public readonly LavaNode<MusicPlayer> Lavalink;
        public readonly InteractiveService Interactive;
        private readonly PatreonService _patreonService;

        public MusicService(IServiceProvider services) : base(services)
        {
            Client = services.GetRequiredService<DiscordShardedClient>();
            Lavalink = services.GetRequiredService<LavaNode<MusicPlayer>>();
            Interactive = services.GetRequiredService<InteractiveService>();
            _patreonService = services.GetRequiredService<PatreonService>();
            
            Client.UserVoiceStateUpdated += UserVoiceStateUpdatedAsync;
            Lavalink.OnTrackException += TrackExceptionAsync;
            Lavalink.OnTrackStuck += TrackStuckAsync;
            Lavalink.OnTrackEnded += TrackEndedAsync;
        }
        
        private readonly string _module = "Music";
        public readonly string YoutubeUrl = "https://youtu.be/{0}?list={1}";

        public Task<IUserMessage> ReplyConfirmationAsync(IMessageChannel channel, ulong guildId, string key, params object[] args)
            => base.ReplyConfirmationAsync(channel, guildId, _module, key, args);

        public Task<IUserMessage> ReplyErrorAsync(IMessageChannel channel, ulong guildId, string key, params object[] args)
            => base.ReplyErrorAsync(channel, guildId, _module, key, args);

        public string GetText(ulong guildId, string key, params object[] args)
            => base.GetText(guildId, _module, key, args);

        public async Task<MusicPlayer> InitializePlayerAsync(IVoiceChannel voiceChannel, SocketTextChannel textChannel, SocketGuildUser user)
        {
            var player = await Lavalink.JoinAsync(voiceChannel, textChannel);
            await ReplyConfirmationAsync(textChannel, voiceChannel.GuildId, "ChannelConnected", voiceChannel.Name);

            player.Initialize(this, GetPatreonPlayerFeatures(user));
            return player;
        }
        
        private async Task UserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            var guildUser = (IGuildUser) user;
            if (!Lavalink.TryGetPlayer(guildUser.Guild, out var player))
                return;

            if (oldState.VoiceChannel != null)
                await AutoDisconnect(oldState.VoiceChannel.Users, guildUser, player);

            if (newState.VoiceChannel != null)
                await AutoDisconnect(newState.VoiceChannel.Users, guildUser, player);
        }

        private async Task AutoDisconnect(IReadOnlyCollection<SocketGuildUser> users, IGuildUser guildUser, MusicPlayer player)
        {
            if (!users.Contains(await guildUser.Guild.GetCurrentUserAsync()))
                return;

            if (users.Count(u => !u.IsBot) < 1)
            {
                await StartAutoDisconnecting(TimeSpan.FromMinutes(2), player);
            }
            else
            {
                await StopAutoDisconnecting(player);
            }
        }

        private async Task StartAutoDisconnecting(TimeSpan dueTime, MusicPlayer player)
        {
            if (player.PlayerState == PlayerState.Playing)
                await player.PauseAsync(false);

            player.AutoDisconnectTimer = new Timer(async _ => await player.LeaveAndDisposeAsync(), null, dueTime, TimeSpan.Zero);

            var outputChannelState = CheckOutputChannel(player.GuildId, player.TextChannel);
            if (outputChannelState == OutputChannelState.Available)
                await ReplyConfirmationAsync(player.TextChannel, player.GuildId, "StopAfter");
        }

        private async Task StopAutoDisconnecting(MusicPlayer player)
        {
            if (player.AutoDisconnectTimer is null)
                return;

            if (player.PlayerState == PlayerState.Paused)
            {
                var outputChannelState = CheckOutputChannel(player.GuildId, player.TextChannel);
                var sendMessage = outputChannelState == OutputChannelState.Available;
                await player.ResumeAsync(sendMessage);
            }

            player.AutoDisconnectTimer.Dispose();
            player.AutoDisconnectTimer = null;
        }

        private async Task TrackExceptionAsync(TrackExceptionEventArgs args)
        {
            var player = Lavalink.GetPlayer(args.Player.VoiceChannel.Guild);

            player.CurrentTime.Stop();
            await player.PlayNextTrackAsync();
            
            var outputChannelState = CheckOutputChannel(player.GuildId, player.TextChannel);
            if (outputChannelState == OutputChannelState.Available)
            {
                await ReplyErrorAsync(player.TextChannel, player.GuildId, "TrackException", args.Track.Title);
            }
            
            Log.Error($"Lavalink: {args.Track.Title} threw an exception: {args.ErrorMessage}");
        }

        private async Task TrackStuckAsync(TrackStuckEventArgs args)
        {
            var player = Lavalink.GetPlayer(args.Player.VoiceChannel.Guild);
            
            player.CurrentTime.Stop();
            await player.PlayNextTrackAsync();
            var outputChannelState = CheckOutputChannel(player.GuildId, player.TextChannel);
            if (outputChannelState == OutputChannelState.Available)
            {
                await ReplyErrorAsync(player.TextChannel, player.GuildId, "TrackStuck", args.Track.Title);
            }
            
            Log.Error($"Lavalink: {args.Track.Title} got stuck: {args.Threshold.Humanize(3)}");
        }

        private async Task TrackEndedAsync(TrackEndedEventArgs args)
        {
            var player = Lavalink.GetPlayer(args.Player.VoiceChannel.Guild);

            if (args.Reason != TrackEndReason.Finished)
                return;
            
            player.CurrentTime.Stop();
            await player.PlayNextTrackAsync();
        }
        
        /// <summary>
        /// Checks the music output channel. Returns one of the reasons: NULL, AVAILABLE, NO_SEND_MESSAGES_PERMISSION, NO_VIEW_CHANNEL_PERMISSION
        /// </summary>
        public OutputChannelState CheckOutputChannel(ulong guildId, IMessageChannel oldChannel)
        {
            var guild = Client.GetGuild(guildId);

            var channel = guild?.GetChannel(oldChannel.Id);
            if (channel is null)
                return OutputChannelState.Null;

            var permissions = guild!.CurrentUser?.GetPermissions(channel);
            if (!permissions.HasValue)
                return OutputChannelState.Null;

            if (!permissions.Value.ViewChannel)
            {
                return OutputChannelState.NoViewPermission;
            }

            if (!permissions.Value.SendMessages)
            {
                return OutputChannelState.NoSendPermission;
            }

            return OutputChannelState.Available;
        }
        
        public PlayerPatreonFeatures GetPatreonPlayerFeatures(SocketGuildUser user)
        {
            if (user.Id == Creds.MasterId)
                return PlayerPatreonFeatures.Volume | PlayerPatreonFeatures.LongTracks | PlayerPatreonFeatures.Livestream;
            
            if (user.Guild.OwnerId == Creds.MasterId)
                return PlayerPatreonFeatures.Volume | PlayerPatreonFeatures.LongTracks | PlayerPatreonFeatures.Livestream;

            if (Creds.PatreonConfig is null)
                return PlayerPatreonFeatures.Volume | PlayerPatreonFeatures.LongTracks | PlayerPatreonFeatures.Livestream;

            var patreonTier = _patreonService.GetPatreonTier(user);
            var playerPatreonFeatures = PlayerPatreonFeatures.None;
            
            if (patreonTier == 0)
                return playerPatreonFeatures;
            
            if (patreonTier >= 2)
                playerPatreonFeatures |= PlayerPatreonFeatures.Volume;
            if (patreonTier >= 4)
                playerPatreonFeatures |= PlayerPatreonFeatures.LongTracks | PlayerPatreonFeatures.Livestream;

            return playerPatreonFeatures;
        }
    }
}