using Discord;
using Discord.WebSocket;
using RiasBot.Modules.Music.Common;
using RiasBot.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RiasBot.Extensions;

namespace RiasBot.Modules.Music.Services
{
    public class MusicService : IRService
    {
        private readonly DbService _db;
        private readonly IBotCredentials _creds;

        public MusicService(DbService db, IBotCredentials creds)
        {
            _db = db;
            _creds = creds;
        }
        
        public readonly ConcurrentDictionary<ulong, MusicPlayer> MPlayer = new ConcurrentDictionary<ulong, MusicPlayer>();

        public async Task CheckIfAlone(SocketUser user, SocketVoiceState stateOld, SocketVoiceState stateNew)
        {
            try
            {
                if (user.IsBot)
                    return;
                
                var userG = (SocketGuildUser)user;
                var mp = GetMusicPlayer(userG.Guild);

                if (mp != null)
                {
                    if (stateNew.VoiceChannel != null)
                    {
                        if (stateNew.VoiceChannel.Id == mp.VoiceChannel.Id)
                        {
                            if (stateNew.VoiceChannel.Users.Contains(((SocketGuildUser) user).Guild.CurrentUser) && stateNew.VoiceChannel.Users.Count <= 2)
                            {
                                if (mp?.Timeout != null)
                                {
                                    await mp.TogglePause(false, true);
                                    mp.Timeout?.Dispose();
                                    mp.Timeout = null;
                                }
                            }
                        }
                    }
                }
                
                if (stateOld.VoiceChannel == null)
                    return;
                if (!stateOld.VoiceChannel.Users.Contains(((SocketGuildUser)user).Guild.CurrentUser))
                    return;
                if (stateOld.VoiceChannel == (stateNew.VoiceChannel))
                    return;
                
                var users = stateOld.VoiceChannel.Users.Count(u => !u.IsBot);
                
                if (users < 1)
                {
                    if (mp != null)
                    {
                        await mp.Channel.SendConfirmationEmbed("All users left the voice channel! The music player has been paused and I will leave in two minutes " +
                                                               "if you don't join back!");
                        await mp.TogglePause(true, false);
                        mp.Timeout = new Timer(async _ => await mp.Destroy("I left the voice channel due to inactivity!", true, true), null,
                            TimeSpan.FromMinutes(2), TimeSpan.Zero);
                    }
                }
            }
            catch
            {
                //ignored
            }

        }

        public MusicPlayer GetOrAddMusicPlayer(IGuild guild)
        {
            var mp = MPlayer.GetOrAdd(guild.Id, new MusicPlayer(this));
            UnlockFeatures(mp, guild.OwnerId);
            return mp;
        }

        public MusicPlayer GetMusicPlayer(IGuild guild)
        {
            return MPlayer.TryGetValue(guild.Id, out var mp) ? mp : null;
        }

        public MusicPlayer RemoveMusicPlayer(IGuild guild)
        {
            return MPlayer.TryRemove(guild.Id, out var musicPlayer) ? musicPlayer : null;
        }

        public bool UnlockMusic(ulong guildOwnerId)
        {
            if (string.IsNullOrEmpty(_creds.PatreonAccessToken)) return true;    //self hosters can use the music module if they don't have a Patreon

            if (guildOwnerId == RiasBot.KonekoId) return true;
            
            using (var db = _db.GetDbContext())
            {
                var patron = db.Patreon.FirstOrDefault(x => x.UserId == guildOwnerId);
                if (patron != null)
                {
                    return patron.Reward >= 5000;
                }
                else
                    return false;
            }
        }

        private void UnlockFeatures(MusicPlayer mp, ulong guildOwnerId)
        {
            if (guildOwnerId == RiasBot.KonekoId)
                mp.DurationLimit = new TimeSpan(5, 5, 0);
            using (var db = _db.GetDbContext())
            {
                var pledgeAmount = 0;
                var patron = db.Patreon.FirstOrDefault(x => x.UserId == guildOwnerId);
                if (patron != null)
                {
                    pledgeAmount = patron.Reward;
                }
            }
        }
    }

}
