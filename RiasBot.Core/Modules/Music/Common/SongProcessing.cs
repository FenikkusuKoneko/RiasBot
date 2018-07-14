using RiasBot.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace RiasBot.Modules.Music.Common
{
    public class SongProcessing
    {
        MusicPlayer _mp;
        public SongProcessing(MusicPlayer mp)
        {
            _mp = mp;
        }

        public async Task<string> GetAudioURL(string input)
        {
            Process p = null;
            try
            {
                //Input can be a video id, or a url. Doesn't matter.
                p = Process.Start(new ProcessStartInfo
                {
                    FileName = "youtube-dl",
                    Arguments = "--geo-bypass -f bestaudio -g " + input,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                var result = await p.StandardOutput.ReadToEndAsync();
                var error = await p.StandardError.ReadToEndAsync();
                if (!String.IsNullOrEmpty(error))
                    await _mp._channel.SendErrorEmbed(error.Substring(error.IndexOf("YouTube said:")).TrimStart());

                result = result.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");

                _mp.isDownloading = false;
                return result;
            }
            catch
            {
                return null;
            }
        }

        public Process CreateStream(string path)
        {
            Process p = null;
            var args = $"-err_detect ignore_err -i {path} -f s16le -ar 48000 -vn -ac 2 pipe:1 -loglevel error";

            try
            {
                p = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    CreateNoWindow = true,
                });

                return p;
            }
            catch
            {
                return null;
            }
        }

        public async Task DownloadNextSong()
        {
            try
            {
                var index = _mp.position + 1;
                var song = _mp.Queue[index];
                if (String.IsNullOrEmpty(song.dlUrl))
                {
                    var audioURL = await GetAudioURL(song.url).ConfigureAwait(false);
                    song.dlUrl = audioURL;
                    _mp.Queue[index] = song;
                }
                else
                {
                    _mp.isDownloading = false;
                }
            }
            catch
            {

            }
        }
    }
}
