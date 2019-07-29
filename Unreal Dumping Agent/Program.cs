using System;
using System.Collections.Generic;
using System.IO;
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
using Unreal_Dumping_Agent.Json;
using Unreal_Dumping_Agent.Memory;
using Unreal_Dumping_Agent.Tools;
using Unreal_Dumping_Agent.Tools.SdkGen;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent
{
    /**
     * NOTES:
     * 1- To debug async Exception Settings -> Check Common Language Runtime
     */
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

        private static async Task Test()
        {
            Utils.MemObj = new Memory.Memory(Utils.DetectUnrealGame());
            Utils.ScanObj = new Scanner(Utils.MemObj);

            var fPointer = new EngineClasses.FPointer { Dummy = (IntPtr)0x1121314151617181, gg = 55, Dummy2 = (IntPtr)0x1121314151617181 };
            fPointer.FixPointers();

            //JsonReflector.LoadJsonEngine("EngineBase");
            //var ss = JsonReflector.StructsList;
            //var gg = await GObjectsFinder.Find();
            //var gg = await GNamesFinder.Find();
            //var gg = await new Scanner(Utils.MemObj).Scan(50, Scanner.ScanAlignment.Alignment4Bytes, Scanner.ScanType.TypeExact);
            //var pat = PatternScanner.Parse("None", 0, "4E 6F 6E 65 00", 0xFF);
            //var gg = await PatternScanner.FindPattern(Utils.MemObj, new List<PatternScanner.Pattern>() { pat });

            Console.WriteLine(fPointer.Dummy.ToInt32());
            Console.WriteLine(fPointer.Dummy2.ToInt32());
        }

        private async Task MainAsync()
        {
            await Test();
            return;

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
            var uTask = await _chatManager.PredictQuestion(context.Message.Content);
            if (uTask.TypeEnum() == EQuestionType.None)
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
            await context.User.SendMessageAsync($"ok, that's what i think you need to do:\n`{uTask.TypeEnum():G}` => `{uTask.TaskEnum():G}`\n--------------------------");

            await ExecuteTasks(curUser, uTask, message, context);
        }

        private static async Task ExecuteTasks(UsersInfo curUser, QuestionPrediction uTask, SocketUserMessage messageParam, SocketCommandContext context)
        {
            var requestInfo = new AgentRequestInfo()
            {
                User = curUser,
                SocketMessage = messageParam,
                Context = context
            };

            // Lock process, auto detect process
            if (uTask.TypeEnum() == EQuestionType.LockProcess ||
                uTask.TypeEnum() == EQuestionType.Find && uTask.TaskEnum() == EQuestionTask.Process)
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
                Utils.ScanObj = new Memory.Scanner(Utils.MemObj);

                // Get Game Unreal Version
                if (!Utils.UnrealEngineVersion(out string ueVersion) || string.IsNullOrWhiteSpace(ueVersion))
                    ueVersion = "Can't Detected";

                // Load Engine File
                // TODO: Make engine name dynamically stetted by user 
                string corePath = Path.Combine(Environment.CurrentDirectory, "Config", "EngineCore");
                var filesName = Directory.EnumerateFiles(corePath).Select(Path.GetFileName).ToList();
                if (!filesName.Contains($"{ueVersion}.json"))
                {
                    if (ueVersion != "Can't Detected")
                        await context.User.SendMessageAsync($"Can't found core engine called `{ueVersion}.json`.\nSo i will use `EngineBase.json`");
                    ueVersion = "EngineBase";
                }
                JsonReflector.LoadJsonEngine(ueVersion);

                var emb = new EmbedBuilder
                {
                    Color = Color.Green,
                    Title = "Target Info",
                    Description = "**Information** about your __target__.",
                };
                emb.WithFooter("Donate to keep me working :)");

                emb.AddField("Window Name", Utils.MemObj.TargetProcess.MainWindowTitle);
                emb.AddField("Exe Name", Path.GetFileName(Utils.MemObj.TargetProcess.MainModule?.FileName));
                emb.AddField("Unreal Version", ueVersion);
                emb.AddField("Game Architecture", Utils.MemObj.Is64Bit ? "64Bit" : "32bit");

                await context.User.SendMessageAsync(embed: emb.Build());
            }

            // Finder
            else if (uTask.TypeEnum() == EQuestionType.Find)
            {
                if (Utils.MemObj == null)
                {
                    if (uTask.TaskEnum() == EQuestionTask.None)
                        await context.User.SendMessageAsync(DiscordText.GetRandomNotUnderstandString());
                    else
                        await context.User.SendMessageAsync($"Give me your target !!");
                    curUser.LastOrder = UserOrder.GetProcess;
                    return;
                }
                if (uTask.TaskEnum() == EQuestionTask.None)
                {
                    await context.User.SendMessageAsync(DiscordText.GetRandomNotUnderstandString());
                    return;
                }

                var lastMessage = await context.User.SendMessageAsync($":white_check_mark: Working on that.");

                // Do work
                var finderResult = new List<IntPtr>();
                switch (uTask.TaskEnum())
                {
                    case EQuestionTask.GNames:
                        finderResult = await GNamesFinder.Find(requestInfo);
                        break;
                    case EQuestionTask.GObject:
                        finderResult = await GObjectsFinder.Find(requestInfo);
                        break;
                }

                if (finderResult.Count == 0)
                {
                    await lastMessage.ModifyAsync(msg => msg.Content = ":x: Can't found any thing !!");
                    return;
                }

                var emb = new EmbedBuilder
                {
                    Color = Color.Green,
                    Title = $"Finder Result ({uTask.TaskEnum():G})",
                    Description = "That's what i found for you :-\n\n",
                };
                emb.WithFooter("Donate to keep me working :)");
                for (int i = 0; i < finderResult.Count; i++)
                    emb.Description += $"{i}) `0x{finderResult[i].ToInt64():X}`.\n";

                await lastMessage.ModifyAsync(msg =>
                {
                    msg.Content = string.Empty;
                    msg.Embed = emb.Build();
                });
            }
        }
    }
}
