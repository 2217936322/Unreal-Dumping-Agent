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
        public delegate Task ReactionReceived(SocketReaction reaction);

        public event MessageReceived MessageHandler;
        public event ReactionReceived ReactionAddedHandler;
        public event ReactionReceived ReactionRemovedHandler;

        public DiscordSocketClient Client { get; private set; }
        public SocketUser CurrentBot => Client.GetUser(Client.CurrentUser.Id);

        private bool _init;
        private CommandService _commands;

        public async Task Init()
        {
            // Config
            Client = new DiscordSocketClient(new DiscordSocketConfig()
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
            Client.MessageReceived += Client_MessageReceived;
            Client.ReactionAdded += (cacheAble, channel, reaction) => Task.Run(() => ReactionAddedHandler?.Invoke(reaction));
            Client.ReactionRemoved += (cacheAble, channel, reaction) => Task.Run(() => ReactionRemovedHandler?.Invoke(reaction));

            Client.Ready += Client_Ready;
            Client.Log += Client_Log;
            _init = true;
        }
        public async void Start()
        {
            if (!_init)
                throw new Exception("Call Init function first.!!");

            // Setup
            await Client.LoginAsync(TokenType.Bot, BotToken);
            await Client.StartAsync();
        }

        private static async Task Client_Log(LogMessage message)
        {
            Utils.ConsoleText(message.Source, message.Message, ConsoleColor.Green);
            await Task.Delay(0);
        }
        private async Task Client_Ready()
        {
            await Client.SetGameAsync("Dumping");
        }
        private async Task Client_MessageReceived(SocketMessage messageParam)
        {
            var message = (SocketUserMessage)messageParam;
            var context = new SocketCommandContext(Client, message);

            // Bad ??
            if (context.Message == null || string.IsNullOrEmpty(context.Message.Content)) return;
            if (context.User.IsBot) return;

            await Task.Run(() => MessageHandler?.Invoke(message, context));
        }
        public Task<IResult> ExecuteAsync(ICommandContext context, int argPos)
        {
            return _commands.ExecuteAsync(context, argPos, null);
        }
    }
}
