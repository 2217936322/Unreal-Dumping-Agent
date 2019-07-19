using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Discord
{
    public class DiscordManager
    {
        private const string BotToken = "NjAxNjM4NjY1NTg4MDQ3OTEw.XTFNxA.nGeHiWe-ZAG89DsF98RbR-X9bKE"; // Add to settings file
        public delegate Task MessageReceived(SocketUserMessage message, SocketCommandContext context);
        public event MessageReceived MessageHandler;

        private bool _init;
        private DiscordSocketClient _client;
        private CommandService _commands;

        public SocketSelfUser CurrentBot => _client.CurrentUser;
        public async Task Init()
        {
            // Config
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = Utils.IsDebug() ? LogSeverity.Debug : LogSeverity.Error
            });
            _commands = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async,
                LogLevel = Utils.IsDebug() ? LogSeverity.Debug : LogSeverity.Error
            });

            // Add All Commands to Execute
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            // Events
            _client.MessageReceived += Client_MessageReceived;
            _client.Ready += Client_Ready;
            _client.Log += Client_Log;
            _init = true;
        }
        public async void Start()
        {
            if (!_init)
                throw new Exception("Call Init function first.!!");

            // Setup
            await _client.LoginAsync(TokenType.Bot, BotToken);
            await _client.StartAsync();
        }

        private static async Task Client_Log(LogMessage message)
        {
            Utils.ConsoleText(message.Source, message.Message, ConsoleColor.Green);
            await Task.Delay(0);
        }
        private async Task Client_Ready()
        {
            await _client.SetGameAsync("Dumping");
        }
        private async Task Client_MessageReceived(SocketMessage messageParam)
        {
            var message = (SocketUserMessage)messageParam;
            var context = new SocketCommandContext(_client, message);

            // Bad ??
            if (context.Message == null || string.IsNullOrEmpty(context.Message.Content)) return;
            if (context.User.IsBot) return;

            MessageHandler?.Invoke(message, context);
        }
        public Task<IResult> ExecuteAsync(ICommandContext context, int argPos)
        {
            return _commands.ExecuteAsync(context, argPos, null);
        }
    }
}
