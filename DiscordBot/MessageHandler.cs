using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    internal class MessageHandler
    {
        internal async Task MessageReceivedAsync(SocketMessage message)
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
                    // เรียกบอทเข้าแชลแนล
                    // Summon bot into channel
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
                    // ไล่บอทออกจากแชลแนล
                    // kick bot out of channel
                    else if (message.Content.StartsWith("!dis"))
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
                            for (int i = YoutubeHandler.queue.Count(); i > 0; i--)
                            {
                                YoutubeHandler.queue.RemoveAt(0);
                            }
                            ffmpegProcess.Kill();
                            await Task.Delay(3000);
                            Console.WriteLine("บังคับให้กระบวนการ FFmpeg จบ");
                            var filePath = "aud.mp3";
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                                Console.WriteLine("ลบไฟล์เพลงเรียบร้อยแล้ว!");
                            }
                        }
                        await audioChannel.DisconnectAsync();
                        await message.Channel.SendMessageAsync("บอทออกจากห้องเสียงเรียบร้อยแล้ว!");
                    }
                    // ทอยเต๋า
                    // dice
                    else if (message.Content == "!dice")
                    {
                        var random = new Random();
                        int result = random.Next(1, 7);
                        await message.Channel.SendMessageAsync($"หน้าลูกเต๋า = {result}!");
                    }
                    // เล่นเพลง
                    // play music
                    else if (message.Content.StartsWith("!p "))
                    {
                        var youtubehandler = new YoutubeHandler();
                        var url = message.Content.Substring(3); // ดึง URL หลัง "!play " // Get the URL after "!play "                        
                        var audioChannel = (message.Author as IGuildUser)?.VoiceChannel;
                        // เช็คว่าเล่นเพลงอยู่ไหม
                        // Check if the bot is playing music
                        var ffmpegProcess = Process.GetProcessesByName("ffmpeg").FirstOrDefault();
                        if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                        {
                            await youtubehandler.SearchYoutubeName(url);
                            return;
                        }
                        var filePath = "aud.mp3";
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            Console.WriteLine("ลบไฟล์เพลงเรียบร้อยแล้ว!");
                        }

                        // ฟังก์ชันหลัก
                        // Main function
                        await youtubehandler.SearchYoutubeName(url);
                        if (audioChannel == null)
                        {
                            await message.Channel.SendMessageAsync("คุณต้องอยู่ในห้องเสียงก่อน!");
                            return;
                        }
                        var client = await audioChannel.ConnectAsync();
                        var youtubeHandler = new YoutubeHandler();
                        await youtubeHandler.DownloadYouTubeAudio(message,client);
                    }
                    // ดูคิวเพลง
                    // music queue
                    else if (message.Content.StartsWith("!qu"))
                    {
                        if (YoutubeHandler.queue.Count() > 0)
                        {
                            for (int i = 0; i < YoutubeHandler.queue.Count(); i++)
                            {
                                await message.Channel.SendMessageAsync($"{i + 1}. {YoutubeHandler.queue[i]}");
                            }
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"ไม่มีคิว");
                        }
                    }
                    // ลบเพลง
                    // remove music
                    else if (message.Content.StartsWith("!remove "))
                    {
                        string remove = message.Content.Substring(8);
                        YoutubeHandler.queue.RemoveAt(int.Parse(remove) - 1);
                    }
                    // ข้ามเพลง
                    // next song
                    else if (message.Content == "!skip")
                    {
                        // เช็คว่าเล่นเพลงอยู่ไหม
                        // Check if the bot is playing music
                        var ffmpegProcess = Process.GetProcessesByName("ffmpeg").FirstOrDefault();
                        if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                        {
                            ffmpegProcess.Kill();
                        }
                        else 
                        {
                            await message.Channel.SendMessageAsync("บอทไม่ได้เล่นเพลง");
                        }
                    }
                    else if(message.Content == "!help")
                    {
                        await message.Channel.SendMessageAsync("!join = เรียกบอทเข้าแชทเสียง\n!dis = ไล่บอทออกแชทเสียง\n!dice = ทอยลูกเต๋า\n!p = เล่นเพลง\nตัวอย่างการใช้งาน\n!p https://www.youtube.com/watch?v=xxxxxxxx\n!p songname\n!qu = ลิสเพลง\n!remove เลขลำดับ = ลบเพลงออกจากลิส\n!skip = ข้ามเพลง");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

    }
}
