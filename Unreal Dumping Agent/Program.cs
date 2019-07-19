using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Unreal_Dumping_Agent.Chat;
using Unreal_Dumping_Agent.Discord;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent
{
    internal class Program
    {
        private readonly ChatManager _chatManager = new ChatManager();
        private readonly DiscordManager _discordManager = new DiscordManager();

        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            var initChat = _chatManager.Init();
            var initDiscord = _discordManager.Init();

            // Wait ChatManager init
            await initChat;
            await initDiscord;

            // Start
            _discordManager.Start();
            _discordManager.MessageHandler += DiscordManager_MessageHandler;

            // Wait until window closed
            while (true)
                Thread.Sleep(1);
        }

        private async Task DiscordManager_MessageHandler(SocketUserMessage message, SocketCommandContext context)
        {
            // message form server
            int argPos = 0;
            if (!context.IsPrivate && 
                message.HasStringPrefix("!agent ", ref argPos) ||
                message.HasMentionPrefix(_discordManager.CurrentBot, ref argPos))
            {
                var result = await _discordManager.ExecuteAsync(context, argPos);
                if (!result.IsSuccess)
                    Utils.ConsoleText("Commands", $"Can't executing a command. Text: {context.Message.Content} | Error: {result.ErrorReason}", ConsoleColor.Red);
            }

            // message from private

        }
    }
}
