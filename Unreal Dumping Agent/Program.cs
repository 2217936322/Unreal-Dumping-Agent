using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Unreal_Dumping_Agent.Chat;
using Unreal_Dumping_Agent.Discord;
using Unreal_Dumping_Agent.Http;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent
{
    internal class Program
    {
        private readonly ChatManager _chatManager = new ChatManager();
        private readonly HttpManager _httpManager = new HttpManager();
        private readonly DiscordManager _discordManager = new DiscordManager();
        private readonly List<ulong> _knownUsers = new List<ulong>();

        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        #region Workers
        private async Task FindGNames()
        {

        }
        #endregion

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

            _httpManager.Start(2911);

            // Wait until window closed
            while (Console.ReadLine() != "exit")
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
            var userTask = await _chatManager.PredictQuestion(context.Message.Content);
            if (userTask.TaskEnum() == EQuestionTask.None)
            {
                await context.User.SendMessageAsync(DiscordText.GetRandomNotUnderstandString());
                return;
            }

            if (!_knownUsers.Contains(context.User.Id))
            {
                var bEmbed = new EmbedBuilder();
                bEmbed.WithColor(Color.DarkOrange);
                bEmbed.WithTitle($"Linking");
                bEmbed.WithDescription($"I should make link to your local `Unreal Dumping Agent`.\n" +
                                       $"That's mean i will get your `IP` (it's safe i will not `DDos` u {DiscordText.GetRandomHappyEmoji()}).\n" +
                                       $"So open the blow `link` and let me play {DiscordText.GetRandomHappyEmoji()}.");
                bEmbed.AddField($"Linking Link", $"http://localhost:2911");
                bEmbed.WithFooter("Say Thanks to CorrM");

                await context.User.SendMessageAsync(embed: bEmbed.Build());
                return;
            }

            var sentMessage = await context.User.SendMessageAsync($"ok, that's what i think you need to do:\n{userTask.TypeEnum():G}, {userTask.TaskEnum():G}");
            if (userTask.TypeEnum() == EQuestionType.Find)
            {
                switch (userTask.TaskEnum())
                {
                    case EQuestionTask.GNames:

                        break;
                    case EQuestionTask.GObject:
                        break;
                    case EQuestionTask.GNamesAndGObject:
                        break;
                }
            }
        }
    }
}
