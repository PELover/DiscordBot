using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Threading.Channels;
using Discord.Audio;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;

namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private  string token = Environment.GetEnvironmentVariable("MTMyNjQyMDM1MTEzMTA2MjI3Mw.G98elm.vBeLeIfUjiXmQOTD4FG1uqRH3CVZJ48gBieiu0");

        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();
        //รันโปรแกรมไม่อยากแก้ Main เลยสร้าง Task ใหม่
        //Start the bot
        public async Task RunBotAsync()
        {
            // ตั้งค่า Client
            // Client Settings
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });

            // เพิ่ม Event Handlers
            // Add Event Handlers
            _client.Log += Log;
            _client.MessageReceived += MessageReceivedAsync;

            // Login และเชื่อมต่อ
            // Login and Connect
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // ป้องกันไม่ให้โปรแกรมปิดตัว
            // Prevent the program from closing
            await Task.Delay(-1);
        }

        private Task _client_MessageReceived(SocketMessage arg)
        {
            throw new NotImplementedException();
        }

        //ใช้สำหรับแสดง Log ข้อความ
        //Log message
        private Task Log(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        //ใช้สำหรับรับข้อความจาก Discord (อนาคตอาจจะเปลี่ยนไปใช้ slashcommand แต่พิมพ์เป็น Text ก็น่ารักดี)
        //Receive message from Discord
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            Console.WriteLine($"Message received: {message.Content}");
            // ตรวจสอบว่าเป็นข้อความจากบอทหรือเปล่า
            // Check if the message is from the bot itself
            if (message.Author.IsBot) return;

            // รันคำสั่งใน Task แยกเพื่อป้องกันการบล็อก Gateway
            // Run the command in a separate Task to prevent blocking the Gateway
            _ = Task.Run(async () =>
            {
                try
                {
                    if (message.Content == "!join")
                    {
                        var audioChannel = (message.Author as IGuildUser)?.VoiceChannel;
                        if (audioChannel == null)
                        {
                            await message.Channel.SendMessageAsync("คุณต้องอยู่ในห้องเสียงก่อน!");
                            return;
                        }
                        await audioChannel.ConnectAsync();
                        await message.Channel.SendMessageAsync($"บอทเข้าร่วมช่องเสียง: {audioChannel.Name}");
                    }
                    else if (message.Content == "!leave")
                    {
                        var audioChannel = (message.Author as IGuildUser)?.VoiceChannel;
                        if (audioChannel == null)
                        {
                            await message.Channel.SendMessageAsync("บอทไม่ได้อยู่ในห้องเสียง!");
                            return;
                        }
                        // เช็คว่าเล่นเพลงอยู่ไหม
                        // Check if the bot is playing music
                        var ffmpegProcess = Process.GetProcessesByName("ffmpeg").FirstOrDefault();
                        if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                        {
                            // ถ้ามีกระบวนการ ffmpeg กำลังทำงาน ให้หยุดทันที
                            // If the ffmpeg process is running, stop it immediately
                            ffmpegProcess.Kill();
                            Console.WriteLine("บังคับให้กระบวนการ FFmpeg จบ");
                            var filePath = "aud.mp3";
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                                Console.WriteLine("ลบไฟล์เพลงเรียบร้อยแล้ว!");
                            }
                        }
                        await Task.Delay(3000);
                        await audioChannel.DisconnectAsync();
                        await message.Channel.SendMessageAsync("บอทออกจากห้องเสียงเรียบร้อยแล้ว!");
                    }
                    else if (message.Content == "!dice")
                    {
                        var random = new Random();
                        int result = random.Next(1, 7);
                        await message.Channel.SendMessageAsync($"หน้าลูกเต๋า = {result}!");
                    }
                    else if (message.Content.StartsWith("!play "))
                    {
                        // เช็คว่าเล่นเพลงอยู่ไหม
                        // Check if the bot is playing music
                        var ffmpegProcess = Process.GetProcessesByName("ffmpeg").FirstOrDefault();
                        if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                        {
                            await ffmpegProcess.WaitForExitAsync(); // รอเล่นเพลงจบก่อน // Wait for the music to finish playing
                        }
                        var filePath = "aud.mp3";
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            Console.WriteLine("ลบไฟล์เพลงเรียบร้อยแล้ว!");
                        }
                        var url = message.Content.Substring(6); // ดึง URL หลัง "!play " // Get the URL after "!play "
                        var audioChannel = (message.Author as IGuildUser)?.VoiceChannel;
                        if (audioChannel == null)
                        {
                            await message.Channel.SendMessageAsync("คุณต้องอยู่ในห้องเสียงก่อน!");
                            return;
                        }
                        var client = await audioChannel.ConnectAsync();
                        if(client == null)
                        {
                            await audioChannel.DisconnectAsync();
                            await audioChannel.ConnectAsync();
                            return;
                        }
                        await DownloadYouTubeAudio(url, client);                                            
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }
        //ใช้สำหรับโหลดไฟล์เพลงจาก Youtube
        //ต้องเพิ่มฟังก์ชั่น ค้นหาโดยชื่อเพลง
        //Download music files from Youtube
        //(todo)Add a function to search by song name
        private async Task DownloadYouTubeAudio(string videoUrl, IAudioClient audioClient)
        {
            var process = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                //Arguments = $"-f bestaudio -o \"%(title)s.%(ext)s\" {videoUrl}",
                Arguments = $"-f bestaudio --audio-format mp3 -o \"aud.mp3\" {videoUrl}",
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

            Console.WriteLine(output);
            Console.WriteLine(error);
            await PlayYoutube("aud.mp3", audioClient);

        }
        //ใช้สำหรับเล่นไฟล์เพลง อย่าไปแตะต้องน่าจะดีที่สุด
        //Play music files (don't touch, should be the best)
        private async Task  PlayYoutube(string path, IAudioClient audioClient)
        {

             var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using var startprocess = Process.Start(ffmpeg);
            using (var stream = audioClient.CreatePCMStream(AudioApplication.Music))
            {
                try 
                { 
                    await startprocess.StandardOutput.BaseStream.CopyToAsync(stream); 
                }
                finally 
                {
                    await stream.FlushAsync(); 
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Console.WriteLine("ลบไฟล์เพลงเรียบร้อยแล้ว!");
                }
            }
        }
    }    
}