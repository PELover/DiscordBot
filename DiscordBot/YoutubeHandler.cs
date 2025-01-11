using Discord.Audio;
using Discord.WebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class YoutubeHandler
    {
        internal static List<string> queue = new List<string>();
        static string songname = "";
        //ใช้สำหรับโหลดไฟล์เพลงจาก Youtube
        //Download music files from Youtube
        internal async Task SearchYoutubeName(string name)
        {
            var search = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"--get-title \"ytsearch1:{name}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var start = Process.Start(search);
            string output = await start.StandardOutput.ReadToEndAsync();
            string error = await start.StandardError.ReadToEndAsync();

            queue.Add(output);            
            Console.WriteLine(output);
            Console.WriteLine(error);
            start.Kill();
        }
        internal async Task DownloadYouTubeAudio(SocketMessage message,IAudioClient audioClient)
        {                   
            var process = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"-f bestaudio --audio-format mp3 --max-downloads 1 -o \"aud.mp3\" ytsearch:\"{queue[0]}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(process);
            // Debug
            string output = await proc.StandardOutput.ReadToEndAsync();
            string error = await proc.StandardError.ReadToEndAsync();

            await proc.WaitForExitAsync();
            songname = queue[0];
            queue.RemoveAt(0);
            Console.WriteLine(output);
            Console.WriteLine(error);
            await PlayYoutube("aud.mp3",message, audioClient);

        }
        //ใช้สำหรับเล่นไฟล์เพลง อย่าไปแตะต้องน่าจะดีที่สุด
        //Play music files (don't touch, should be the best)
        internal async Task PlayYoutube(string path, SocketMessage message, IAudioClient audioClient)
        {

            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                //Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                Arguments = $"-hide_banner -loglevel error -i \"{path}\" -ac 2 -f s16le -ar 48000 -bufsize 64k pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var startprocess = Process.Start(ffmpeg);
            using (var stream = audioClient.CreatePCMStream(AudioApplication.Music))
            {
                try
                {
                    await message.Channel.SendMessageAsync($"now playing ----- {songname}");
                    await startprocess.StandardOutput.BaseStream.CopyToAsync(stream);
                }
                finally
                {                   
                    await stream.FlushAsync();
                    songname = "";
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Console.WriteLine("ลบไฟล์เพลงเรียบร้อยแล้ว!");
                }               
            }
            if (queue.Count > 0)
            {
                await DownloadYouTubeAudio(message,audioClient);
            }
        }
    }
}
