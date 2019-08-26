using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Unreal_Dumping_Agent.Chat;
using Unreal_Dumping_Agent.Discord;
using Unreal_Dumping_Agent.Discord.Misc;
using Unreal_Dumping_Agent.Json;
using Unreal_Dumping_Agent.Memory;
using Unreal_Dumping_Agent.Tools;
using Unreal_Dumping_Agent.Tools.SdkGen.Langs;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent
{
    /**
     * NOTES:
     * 1-
     *      To debug async Exception Settings -> Check Common Language Runtime.
     *
     * 2-
     *      Don't use string as file container (Read file or contact string to write a file),
     *      use CorrmStringBuilder instead,
     *      it's way faster and less resources.
     */

    public class Program
    {
        #region Paths
        public static string ConfigPath { get; private set; }
        public static string LangsPath { get; private set; }
        public static string GenPath { get; private set; }
        #endregion

        #region SdkLang
        public static Dictionary<string, SdkLang> SupportedLangs = new Dictionary<string, SdkLang>
        {
            { "Cpp", new CppLang() }
        };
        #endregion

        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();
        private async Task MainAsync()
        {
            //await Test();
            //return;

            // Init Paths
            ConfigPath = Path.Combine(Environment.CurrentDirectory, "Config");
            LangsPath = Path.Combine(ConfigPath, "Langs");
            GenPath = Path.Combine(Environment.CurrentDirectory, "Dump");

            // Init
            Utils.BotWorkType = Utils.BotType.Local;
            var initChat = Utils.ChatManager.Init();
            var initDiscord = Utils.DiscordManager.Init();

            // Wait init
            await initChat;
            await initDiscord;

            // Start
            await Utils.DiscordManager.Start();
            Utils.DiscordManager.MessageHandler += DiscordManager_MessageHandler;
            Utils.DiscordManager.ReactionAddedHandler += DiscordManager_ReactionAdded;

            // Wait until window closed
            while (Console.ReadLine() != "exit")
                Thread.Sleep(1);
        }
        private static async Task Test()
        {
            Utils.MemObj = new Memory.Memory(Utils.DetectUnrealGame());
            Utils.ScanObj = new Scanner(Utils.MemObj);

            Utils.MemObj.SuspendProcess();
            JsonReflector.LoadJsonEngine("EngineBase");

            await new SdkGenerator((IntPtr)0x7FF759482B00, (IntPtr)0x7FF75959F1A8).Start(new AgentRequestInfo());

            //var fPointer = new EngineClasses.UField();
            //await fPointer.ReadData((IntPtr)0x228E0C92B30);
            //var ss = JsonReflector.StructsList;
            //var gg = await GObjectsFinder.Find();
            //var gg = await GNamesFinder.Find();
            //var gg = await new Scanner(Utils.MemObj).Scan(50, Scanner.ScanAlignment.Alignment4Bytes, Scanner.ScanType.TypeExact);
            //var pat = PatternScanner.Parse("None", 0, "4E 6F 6E 65 00", 0xFF);
            //var gg = await PatternScanner.FindPattern(Utils.MemObj, new List<PatternScanner.Pattern>() { pat });

            Console.WriteLine("FINIIIISHED");
        }

        #region Discord Handlers
        private async Task DiscordManager_MessageHandler(SocketUserMessage message, SocketCommandContext context)
        {
            // Update users
            var curUser = Utils.KnownUsers.FirstOrDefault(u => u.ID == context.User.Id);
            if (curUser == null)
            {
                Utils.KnownUsers.Add(new UsersInfo { ID = context.User.Id });
                curUser = Utils.KnownUsers.First(u => u.ID == context.User.Id);
            }

            // message not form DM
            int argPos = 0;
            if (!context.IsPrivate &&
                message.HasStringPrefix("!agent ", ref argPos) ||
                message.HasMentionPrefix(Utils.DiscordManager.CurrentBot, ref argPos))
            {
                var result = await Utils.DiscordManager.ExecuteAsync(context, argPos);
                if (!result.IsSuccess)
                    Utils.ConsoleText("Commands", $"Can't executing a command. Text: {context.Message.Content} | Error: {result.ErrorReason}", ConsoleColor.Red);
            }

            // message from DM
            var uTask = await Utils.ChatManager.PredictQuestion(context.Message.Content);
            if (uTask.TypeEnum() == EQuestionType.None)
            {
                await context.User.SendMessageAsync(DiscordText.GetRandomNotUnderstandString());
                return;
            }

            // if bot is public bot, not run on local pc
            if (Utils.BotWorkType == Utils.BotType.Public)
            {
                if (string.IsNullOrEmpty(curUser.IP))
                {
                    var bEmbed = new EmbedBuilder();
                    bEmbed.WithColor(Color.DarkOrange);
                    bEmbed.WithTitle($"Linking");
                    bEmbed.WithDescription($"I should link with your local `Unreal Dumping Agent`.\n" +
                                           $"That's mean i will get your `IP` (it's safe i will not `Attack` u {DiscordText.GetRandomHappyEmoji()}).\n" +
                                           $"So open the blow `link` and let me play {DiscordText.GetRandomHappyEmoji()}.");
                    bEmbed.AddField($"Linking Link", $"http://localhost:2911");
                    bEmbed.WithUrl(Utils.DonateUrl);
                    bEmbed.WithFooter(Utils.DiscordFooterText, Utils.DiscordFooterImg);

                    await context.User.SendMessageAsync(embed: bEmbed.Build());
                    return;
                }

                // create function to process text through http
                return;
            }

            /* COMMANDS START HERE */
            await context.User.SendMessageAsync($"ok, that's what i think you need to do:\n`{uTask.TypeEnum():G}` => `{uTask.TaskEnum():G}`\n--------------------------");

            // Don't Wait
#pragma warning disable 4014
            ExecuteTasks(curUser, uTask, message, context);
#pragma warning restore 4014
        }
        private async Task DiscordManager_ReactionAdded(SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot)
                return;

            var knownUser = Utils.KnownUsers.FirstOrDefault(user => reaction.UserId == user.ID);
            if (knownUser == null)
                return;

            // React Stuff
            var message = (RestUserMessage)await reaction.Channel.GetMessageAsync(reaction.MessageId);
            var removeReact = message.RemoveReactionsAsync(
                Utils.DiscordManager.CurrentBot,
                DiscordText.GenEmojiNumberList(9, false).Where(r => r.Name != reaction.Emote.Name).ToArray());

            var embed = message.Embeds.FirstOrDefault();
            if (embed == null)
                return;

            // Get Reaction Number
            int reactNum = 0;
            for (int i = 0; i < 10; i++)
            {
                if (DiscordText.GetEmojiNumber(i).Name != reaction.Emote.Name)
                    continue;

                reactNum = i;
                break;
            }

            // Get Address From String
            string str = embed.Description;
            var spilled = str.Split(new[] { "0x" }, StringSplitOptions.None);
            var address = new IntPtr(long.Parse(spilled[reactNum].Split('`')[0], NumberStyles.HexNumber));

            if (embed.Title.Contains("GNames"))
                knownUser.GnamesPtr = address;

            else if (embed.Title.Contains("GObject"))
                knownUser.GobjectsPtr = address;

            await removeReact;
        }
        #endregion

        private static async Task ExecuteTasks(UsersInfo curUser, QuestionPrediction uTask, SocketUserMessage messageParam, SocketCommandContext context)
        {
            var requestInfo = new AgentRequestInfo
            {
                User = curUser,
                SocketMessage = messageParam,
                Context = context
            };

            #region Lock process, auto detect process
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
                Utils.ScanObj = new Scanner(Utils.MemObj);

                Utils.MemObj.SuspendProcess();

                // Get Game Unreal Version
                if (!Utils.UnrealEngineVersion(out string ueVersion) || string.IsNullOrWhiteSpace(ueVersion))
                    ueVersion = "Can Not Detected";

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
                emb.WithUrl(Utils.DonateUrl);
                emb.WithFooter(Utils.DiscordFooterText, Utils.DiscordFooterImg);

                emb.AddField("Window Name", Utils.MemObj.TargetProcess.MainWindowTitle);
                emb.AddField("Exe Name", Path.GetFileName(Utils.MemObj.TargetProcess.MainModule?.FileName));
                emb.AddField("Unreal Version", ueVersion);
                emb.AddField("Game Architecture", Utils.MemObj.Is64Bit ? "64Bit" : "32bit");
                
                await context.User.SendMessageAsync(embed: emb.Build());
            }
            #endregion

            #region Finder
            else if (uTask.TypeEnum() == EQuestionType.Find)
            {
                if (Utils.MemObj == null)
                {
                    if (uTask.TaskEnum() == EQuestionTask.None)
                        await context.User.SendMessageAsync(DiscordText.GetRandomNotUnderstandString());
                    else
                        await context.User.SendMessageAsync($"Give me your target FIRST !!");
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

                if (finderResult.Empty())
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
                emb.WithUrl(Utils.DonateUrl);
                emb.WithFooter(Utils.DiscordFooterText, Utils.DiscordFooterImg);

                for (int i = 0; i < finderResult.Count; i++)
                    emb.Description += $"{DiscordText.GetEmojiNumber(i + 1, true)}) `0x{finderResult[i].ToInt64():X}`.\n";

                await lastMessage.ModifyAsync(msg =>
                {
                    msg.Content = string.Empty;
                    msg.Embed = emb.Build();
                });

                await lastMessage.AddReactionsAsync(DiscordText.GenEmojiNumberList(finderResult.Count, false));
            }
            #endregion

            #region Sdk Generator
            else if (uTask.TypeEnum() == EQuestionType.SdkDump)
            {
                if (Utils.MemObj == null)
                {
                    await context.User.SendMessageAsync($"Give me your target FIRST !!");
                    curUser.LastOrder = UserOrder.GetProcess;
                    return;
                }

                var dumpState = await new SdkGenerator(curUser.GobjectsPtr, curUser.GnamesPtr).Start(requestInfo);
                if (dumpState.State == SdkGenerator.GeneratorState.Good)
                    await context.Channel.SendMessageAsync($"**Take** => {dumpState.StartTime - DateTime.Now:T}");
                else
                    await context.Channel.SendMessageAsync($"**Problem** => {dumpState.State:G}");
            }
            #endregion

            #region Open
            else if (uTask.TypeEnum() == EQuestionType.Open)
            {
                if (uTask.TaskEnum() == EQuestionTask.Sdk)
                    Utils.OpenFolder(GenPath);
                else if (uTask.TaskEnum() == EQuestionTask.Tool)
                    Utils.OpenFolder(Environment.CurrentDirectory);
            }
            #endregion

            #region Help
            else if (uTask.TypeEnum() == EQuestionType.Help)
            {
                var emb = new EmbedBuilder();
                emb.WithUrl(Utils.DonateUrl);
                emb.WithFooter(Utils.DiscordFooterText, Utils.DiscordFooterImg);

                emb.Title = "How To Use";
                emb.Description = File.ReadAllText(Path.Combine(ConfigPath, "help.txt"));

                await context.Channel.SendMessageAsync(embed: emb.Build());

                string gg = await Utils.DiscordManager.OptionsQuestion(requestInfo, "Are u okay .?", new List<string> { "Islam" , "CorrM", "HHH", "GGG", "ZZZ", "LLL" });
                Console.WriteLine(gg);
            }
            #endregion
        }
    }
}
