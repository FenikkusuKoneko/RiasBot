using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using RiasBot.Modules.Music.Common;
using RiasBot.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RiasBot.Modules.Music.Services
{
    public class MusicService : IRService
    {
        private readonly DiscordShardedClient _client;
        private readonly DbService _db;

        public readonly ConcurrentDictionary<ulong, MusicPlayer> MPlayer = new ConcurrentDictionary<ulong, MusicPlayer>();

        /*public bool Paused => pauseTaskSource != null;
        private TaskCompletionSource<bool> pauseTaskSource { get; set; } = null;*/
        public MusicService(DiscordShardedClient client, DbService db)
        {
            _client = client;
            _db = db;
        }

        public async Task CheckIfAlone(SocketUser user, SocketVoiceState stateOld, SocketVoiceState stateNew)
        {
            try
            {
                if (user.IsBot)
                    return;
                if (stateOld.VoiceChannel == null)
                    return;
                if (!stateOld.VoiceChannel.Users.Contains(((SocketGuildUser)user).Guild.CurrentUser))
                    return;
                if (stateOld.VoiceChannel == (stateNew.VoiceChannel ?? null))
                    return;
                var users = 0;
                foreach (var u in stateOld.VoiceChannel.Users)
                {
                    if (!u.IsBot)
                    {
                        users++;
                    }
                }
                if (users < 1)
                {
                    var userG = (SocketGuildUser)user;
                    var mp = GetMusicPlayer(userG.Guild);
                    if (mp == null)
                    {
                        return;
                    }
                    MPlayer.TryGetValue(userG.Guild.Id, out var musicPlayer);
                    await musicPlayer.Destroy("I left because everybody left the voice channel!", true, true);
                }
            }
            catch
            {

            }

        }

        public async Task<MusicPlayer> GetOrAddMusicPlayer(IGuild guild)
        {
            var mp = new MusicPlayer(_client, this);
            if (MPlayer.ContainsKey(guild.Id))
            {
                MPlayer.TryGetValue(guild.Id, out mp);
                return mp;
            }
            else
            {
                MPlayer.TryAdd(guild.Id, mp);
                UnlockFeatures(mp, -1);
                return mp;
            }
        }

        public MusicPlayer GetMusicPlayer(IGuild guild)
        {
            if (MPlayer.TryGetValue(guild.Id, out var mp))
            {
                return mp;
            }
            else
            {
                return null;
            }
        }

        public MusicPlayer RemoveMusicPlayer(IGuild guild)
        {
            if (MPlayer.TryRemove(guild.Id, out var musicPlayer))
                return musicPlayer;
            else
                return null;
        }

        public void UnlockFeatures(MusicPlayer mp, int pledgeAmount)
        {
            if (pledgeAmount >= 5000)
            {
                mp.volumeFeature = true;
            }

            if (pledgeAmount >= 10000)
            {
                mp.queueLimit = 100;
            }

            if (pledgeAmount >= 25000)
            {
                mp.queueLimit = 250;
                mp.durationLimit = new TimeSpan(3, 5, 0); // I'll make a little exception of 5 minutes
            }

            if (pledgeAmount >= 50000)
            {
                mp.queueLimit = 500;
                mp.durationLimit = new TimeSpan(4, 5, 0); // I'll make a little exception of 5 minutes
            }

            if (pledgeAmount == -1)
            {
                mp.volumeFeature = true;
                mp.queueLimit = 500;
                mp.durationLimit = new TimeSpan(4, 5, 0);
            }
        }
    }

}
