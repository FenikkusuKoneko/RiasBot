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
            try
            {
                //Input can be a video id, or a url. Doesn't matter.
                Process p = Process.Start(new ProcessStartInfo
                {
                    FileName = "youtube-dl",
                    Arguments = "-f bestaudio -g " + input,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });

                string result = await p.StandardOutput.ReadToEndAsync();

                result = result.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");

                return result;
            }
            catch
            {
                return null;
            }
        }

        public Process CreateStream(string path)
        {
            var args = $"-err_detect ignore_err -i {path} -f s16le -ar 48000 -vn -ac 2 pipe:1 -loglevel error";
            /*if (!_isLocal)
                args = "-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 " + args;*/

            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
            });
        }

        public async Task DownloadNextSong()
        {
            try
            {
                int index = _mp.position + 1;
                var song = _mp.Queue[index];
                if (String.IsNullOrEmpty(song.dlUrl))
                {
                    var audioURL = await GetAudioURL(song.url).ConfigureAwait(false);
                    song.dlUrl = audioURL;
                    _mp.Queue[index] = song;
                }
            }
            catch
            {

            }
        }
    }
}
