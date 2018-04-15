using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using IBM.WatsonDeveloperCloud.TextToSpeech.v1;
using IBM.WatsonDeveloperCloud.TextToSpeech.v1.Model;
using RiasBot.Extensions;
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

namespace RiasBot.Modules.Music.MusicServices
{
    public class MusicService : IRService
    {
        private DiscordSocketClient _client;

        public readonly ConcurrentDictionary<ulong, MusicPlayer> MPlayer = new ConcurrentDictionary<ulong, MusicPlayer>();

        /*public bool Paused => pauseTaskSource != null;
        private TaskCompletionSource<bool> pauseTaskSource { get; set; } = null;*/
        public MusicService(DiscordSocketClient client)
        {
            _client = client;
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
                int users = 0;
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
                    MPlayer.TryRemove(userG.Guild.Id, out var musicPlayer);
                    await musicPlayer.Destroy("I left because everybody left the voice channel!");
                }
            }
            catch
            {

            }

        }

        public MusicPlayer GetOrAddMusicPlayer(IGuild guild)
        {
            MusicPlayer mp = new MusicPlayer(_client, this);
            if (MPlayer.ContainsKey(guild.Id))
            {
                MPlayer.TryGetValue(guild.Id, out mp);
                return mp;
            }
            else
            {
                MPlayer.TryAdd(guild.Id, mp);
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
    }

}
