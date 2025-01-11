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
using Microsoft.Extensions.Configuration;


namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private MessageHandler messageHandler = new MessageHandler();

        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();
        //รันโปรแกรมไม่อยากแก้ Main เลยสร้าง Task ใหม่
        //Start the bot
        public async Task RunBotAsync()
        {
            var config = new ConfigurationBuilder()  
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("settings.json", optional: false, reloadOnChange: true)
                .Build();
            // ตั้งค่า Client
            // Client Settings
            string token = config["token"];
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            });
            
            // เพิ่ม Event Handlers
            // Add Event Handlers
            _client.Log += Log;
            _client.MessageReceived += messageHandler.MessageReceivedAsync;

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
    }    
}