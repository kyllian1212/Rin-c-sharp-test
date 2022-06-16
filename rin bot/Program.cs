using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace rin_bot
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private DiscordSocketConfig _config;
        private CommandService _commands;

        private static Task Main(string[] args) => new Program().MainAsync();
        public async Task MainAsync()
        {
            _config = new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Debug,
                GatewayIntents = GatewayIntents.All,
                MessageCacheSize = 5000
            };

            _commands = new CommandService();

            _client = new DiscordSocketClient(_config);

            _client.Log += Log;

            var token = File.ReadAllText("token.txt");

            await InstallCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived event into our command handler
            _client.MessageReceived += HandleCommandAsync;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private async Task HandleCommandAsync(SocketMessage pMessage)
        {
            // Don't process the command if it was a system message
            var message = pMessage as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasStringPrefix("!!", ref argPos) || 
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || 
                message.Author.IsBot) 
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            var result = await _commands.ExecuteAsync(context, argPos, null);

            // If error, send a message in the channel explaining the error
            if (!result.IsSuccess) await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
