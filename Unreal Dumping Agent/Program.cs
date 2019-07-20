using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Unreal_Dumping_Agent.Chat;
using Unreal_Dumping_Agent.Discord;
using Unreal_Dumping_Agent.Discord.Misc;
using Unreal_Dumping_Agent.Http;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent
{
    internal class Program
    {
        private readonly ChatManager _chatManager = new ChatManager();
        private readonly HttpManager _httpManager = new HttpManager();
        private readonly DiscordManager _discordManager = new DiscordManager();
        private readonly List<UsersInfo> _knownUsers = new List<UsersInfo>();

        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        #region Workers
        private async Task FindGNames()
        {

        }
        #endregion

        private async Task MainAsync()
        {
            // Init
            Utils.BotWorkType = Utils.BotType.Local;
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
            // Update users
            var curUser = _knownUsers.FirstOrDefault(u => u.Id == context.User.Id);
            if (curUser == null)
            {
                _knownUsers.Add(new UsersInfo { Id = context.User.Id });
                curUser = _knownUsers.First(u => u.Id == context.User.Id);
            }

            // message not form DM
            int argPos = 0;
            if (!context.IsPrivate && 
                message.HasStringPrefix("!agent ", ref argPos) ||
                message.HasMentionPrefix(_discordManager.CurrentBot, ref argPos))
            {
                var result = await _discordManager.ExecuteAsync(context, argPos);
                if (!result.IsSuccess)
                    Utils.ConsoleText("Commands", $"Can't executing a command. Text: {context.Message.Content} | Error: {result.ErrorReason}", ConsoleColor.Red);
            }

            // message from DM
            var userTask = await _chatManager.PredictQuestion(context.Message.Content);
            if (userTask.TypeEnum() == EQuestionType.None)
            {
                await context.User.SendMessageAsync(DiscordText.GetRandomNotUnderstandString());
                return;
            }

            // if bot it public bot, not run on local pc
            if (Utils.BotWorkType == Utils.BotType.Public)
            {
                if (string.IsNullOrEmpty(curUser.Ip))
                {
                    var bEmbed = new EmbedBuilder();
                    bEmbed.WithColor(Color.DarkOrange);
                    bEmbed.WithTitle($"Linking");
                    bEmbed.WithDescription($"I should link with your local `Unreal Dumping Agent`.\n" +
                                           $"That's mean i will get your `IP` (it's safe i will not `Attack` u {DiscordText.GetRandomHappyEmoji()}).\n" +
                                           $"So open the blow `link` and let me play {DiscordText.GetRandomHappyEmoji()}.");
                    bEmbed.AddField($"Linking Link", $"http://localhost:2911");
                    bEmbed.WithFooter("Say Thanks to CorrM :heart:");

                    await context.User.SendMessageAsync(embed: bEmbed.Build());
                    return;
                }

                // create function to process text through http
                return;
            }

            /* COMMANDS START HERE */
            await context.User.SendMessageAsync($"ok, that's what i think you need to do:\n`{userTask.TypeEnum():G}` => `{userTask.TaskEnum():G}`");

            // Lock process, auto detect process
            if (userTask.TypeEnum() == EQuestionType.LockProcess)
            {
                // Try to get process id
                bool findProcessId = int.TryParse(Regex.Match(context.Message.Content, @"\d+").Value, out int processId);

                // Try auto detect it
                if (!findProcessId)
                    processId = Utils.DetectUnrealGame();

                // not valid process
                if (processId == 0 || !Memory.Memory.IsValidProcess(processId))
                {
                    await context.User.SendMessageAsync($"I can't found your `target`. " +
                                                                      (!findProcessId ? "\nI tried to `detect any running target`, but i can't `see anyone`. !! " : null) +
                                                                      DiscordText.GetRandomSadEmoji());
                    return;
                }

                // Setup Memory
                Utils.MemObj = new Memory.Memory(processId);

            }
            // Finder
            else if (userTask.TypeEnum() == EQuestionType.Find)
            {
                if (Utils.MemObj == null)
                {
                    if (userTask.TaskEnum() == EQuestionTask.None)
                        await context.User.SendMessageAsync(DiscordText.GetRandomNotUnderstandString());
                    else
                        await context.User.SendMessageAsync($"Give me your target !!");
                    curUser.LastOrder = UserOrder.GetProcess;
                    return;
                }

                // Do work
                switch (userTask.TaskEnum())
                {
                    case EQuestionTask.GNames:

                        break;
                    case EQuestionTask.GObject:
                        break;
                }
            }
        }
    }
}
