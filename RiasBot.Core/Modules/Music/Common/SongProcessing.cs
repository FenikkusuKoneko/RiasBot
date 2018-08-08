using RiasBot.Extensions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RiasBot.Modules.Music.Common
{
    public class SongProcessing
    {
        private readonly MusicPlayer _mp;
        public SongProcessing(MusicPlayer mp)
        {
            _mp = mp;
        }

        public async Task<string> GetAudioUrl(string input)
        {
            //Input can be a video id, or a url. Doesn't matter.
            using (var p = Process.Start(new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = "--geo-bypass -f bestaudio -g " + input,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }))
            {
                if (p != null)
                {
                    var result = await p.StandardOutput.ReadToEndAsync();
                    var error = await p.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(error))
                        await _mp.Channel.SendErrorEmbed(error.Substring(error.IndexOf("YouTube said:", StringComparison.Ordinal)).TrimStart());

                    result = result.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");

                    _mp.IsDownloading = false;
                
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        public Process CreateStream(string path)
        {
            var args = $"-err_detect ignore_err -i {path} -f s16le -ar 48000 -vn -ac 2 pipe:1 -loglevel error";

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
            if (_mp.Queue.Count > 0)
            {
                var song = _mp.Queue[1];
                if (string.IsNullOrEmpty(song.DlUrl))
                {
                    var audioUrl = await GetAudioUrl(song.Url).ConfigureAwait(false);
                    _mp.Queue[1].DlUrl = audioUrl;
                    _mp.IsDownloading = false;
                }
                else
                {
                    _mp.IsDownloading = false;
                }
            }
        }
    }
}
