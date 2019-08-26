using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Unreal_Dumping_Agent.Discord.Misc;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Discord
{
    public class DiscordManager
    {
        // ToDo: Add to settings file
        // Continue QuestionMessage, Add QuestionListMessage => List Of Options As String Messages with Reaction (Yes) for every message
        private const string BotToken = "NjAxNjM4NjY1NTg4MDQ3OTEw.XTFNxA.nGeHiWe-ZAG89DsF98RbR-X9bKE";
        private bool _init;
        private CommandService _commands;

        /// <summary>
        /// Key     => MessageID
        /// Value   => User Emotes
        /// </summary>
        private Dictionary<ulong, List<IEmote>> _waitedReactions;
        /// <summary>
        /// Key     => UserID
        /// Value   => Answer
        /// </summary>
        private Dictionary<ulong, string> _waitedQuestions;
        /// <summary>
        /// Key     => UserID
        /// Value   => List Of Messages ID (Options), Selected Option String
        /// </summary>
        private Dictionary<ulong, Utils.KeyVal<List<ulong>, string>> _waitedListQuestion;

        #region Events
        public delegate Task MessageReceived(SocketUserMessage message, SocketCommandContext context);
        public delegate Task ReactionReceived(SocketReaction reaction);

        public event MessageReceived MessageHandler;
        public event ReactionReceived ReactionAddedHandler;
        public event ReactionReceived ReactionRemovedHandler;
        #endregion

        public DiscordSocketClient Client { get; private set; }
        public SocketUser CurrentBot { get; private set; }

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
            _waitedReactions = new Dictionary<ulong, List<IEmote>>();
            _waitedQuestions = new Dictionary<ulong, string>();
            _waitedListQuestion = new Dictionary<ulong, Utils.KeyVal<List<ulong>, string>>();

            // Add All Commands to Execute
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            // Events
            Client.MessageReceived += Client_MessageReceived;
            Client.ReactionAdded += Client_OnReactionAdded;
            Client.ReactionRemoved += Client_OnReactionRemoved;

            Client.Ready += Client_Ready;
            Client.Log += Client_Log;

            _init = true;
        }
        public async Task Start()
        {
            if (!_init)
                throw new Exception("Call Init function first.!!");

            // Setup
            await Client.LoginAsync(TokenType.Bot, BotToken);
            await Client.StartAsync();
        }

        private async Task Client_Ready()
        {
            CurrentBot = Client.GetUser(Client.CurrentUser.Id);
            await Client.SetGameAsync("Dumping");
        }
        private static Task Client_Log(LogMessage message)
        {
            Utils.ConsoleText(message.Source, message.Message, ConsoleColor.Green);
            return Task.CompletedTask;
        }

        #region Reactions
        private async Task Client_OnReactionAdded(Cacheable<IUserMessage, ulong> cacheAble, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId != CurrentBot.Id && _waitedReactions.ContainsKey(reaction.MessageId))
                _waitedReactions[reaction.MessageId].Add(reaction.Emote);

            if (reaction.UserId != CurrentBot.Id && _waitedListQuestion.ContainsKey(reaction.UserId) && _waitedListQuestion[reaction.UserId].Key.Contains(reaction.MessageId))
            {
                // YES emote
                if (reaction.Emote.Name == "\u2705")
                    _waitedListQuestion[reaction.UserId].Value = (await reaction.Channel.GetMessageAsync(reaction.MessageId)).Content;
            }

            ReactionAddedHandler?.Invoke(reaction);
        }
        private Task Client_OnReactionRemoved(Cacheable<IUserMessage, ulong> cacheAble, ISocketMessageChannel channel, SocketReaction reaction)
        {
            return reaction.UserId != CurrentBot.Id
                ? Task.Run(() => ReactionRemovedHandler?.Invoke(reaction))
                : Task.CompletedTask;
        }
        #endregion

        #region MessageHandler
        private Task Client_MessageReceived(SocketMessage messageParam)
        {
            var message = (SocketUserMessage)messageParam;
            var context = new SocketCommandContext(Client, message);

            // Bad ??
            if (context.Message == null || string.IsNullOrEmpty(context.Message.Content))
                return Task.CompletedTask;

            if (context.User.IsBot)
                return Task.CompletedTask;

            // Answer for question
            if (_waitedQuestions.ContainsKey(message.Author.Id))
                _waitedQuestions[message.Author.Id] = message.Content;

            // not question answer .?, pass it to MessageHandler
            else
                return Task.Run(() => MessageHandler?.Invoke(message, context));

            return Task.CompletedTask;
        }
        public Task<IResult> ExecuteAsync(ICommandContext context, int argPos)
        {
            return _commands.ExecuteAsync(context, argPos, null);
        }
        #endregion

        #region DiscordQuestion
        /// <summary>
        /// Wait To Receive One Of <see cref="waitedEmojis"/>
        /// </summary>
        /// <param name="msgId">Message ID to wait Emote</param>
        /// <param name="waitedEmojis"></param>
        /// <returns>Emote that's selected</returns>
        private async Task<IEmote> WaitReaction(ulong msgId, IEmote[] waitedEmojis)
        {
            _waitedReactions[msgId] = new List<IEmote>();

            IEmote ret;
            while ((ret = _waitedReactions[msgId].FirstOrDefault(waitedEmojis.Contains)) == null)
                await Task.Delay(8);

            _waitedReactions[msgId].Clear();
            _waitedReactions.Remove(msgId);
            return ret;
        }

        /// <summary>
        /// Wait To Receive Next Massage After Ask
        /// </summary>
        /// <param name="userId">User ID To Wait Response</param>
        /// <returns>User String</returns>
        private async Task<string> WaitStringQuestion(ulong userId)
        {
            _waitedQuestions[userId] = string.Empty;

            while (_waitedQuestions[userId].Empty())
                await Task.Delay(8);

            string ret = _waitedQuestions[userId];
            _waitedQuestions.Remove(userId);
            return ret;
        }

        /// <summary>
        /// Wait To Get Selected Options
        /// </summary>
        /// <param name="userId">User ID To Wait Response</param>
        /// <param name="optionsMessageList">List Of Options Massages</param>
        /// <returns>Selected String String</returns>
        private async Task<string> WaitListQuestion(ulong userId, List<ulong> optionsMessageList)
        {
            _waitedListQuestion[userId] = new Utils.KeyVal<List<ulong>, string>(optionsMessageList, string.Empty);

            while (_waitedListQuestion[userId].Value.Empty())
                await Task.Delay(8);

            string ret = _waitedListQuestion[userId].Value;
            _waitedListQuestion.Remove(userId);

            return ret;
        }

        // ################################

        /// <summary>
        /// Create A Discord Question, That's Have 2 Reactions (Yes, No)
        /// </summary>
        /// <param name="requestInfo">User To Send</param>
        /// <param name="question">Message Text</param>
        /// <returns>True If Yes</returns>
        public async Task<bool> YesNoMessage(AgentRequestInfo requestInfo, string question)
        {
            //                                   YES                         NO
            var emojis = new IEmote[] { new Emoji("\u2705"), new Emoji("\u274C") };

            var message = await requestInfo.Context.Channel.SendMessageAsync($"> **{question}**");
            await message.AddReactionsAsync(emojis);

            var answer = await WaitReaction(message.Id, emojis);

            // Don't wait remove
            #pragma warning disable 4014
            message.RemoveReactionsAsync(CurrentBot, emojis.Where(r => r.Name != answer.Name).ToArray());
            #pragma warning restore 4014

            return answer.Name == "\u2705";
        }

        /// <summary>
        /// Create A Discord Question, And Give User Ability To Response As Text
        /// </summary>
        /// <param name="requestInfo">User To Send</param>
        /// <param name="question">Message Text</param>
        /// <returns>User Response Text</returns>
        public async Task<string> StringQuestion(AgentRequestInfo requestInfo, string question)
        {
            await requestInfo.Context.Channel.SendMessageAsync($"> **{question}**");
            string answer = await WaitStringQuestion(requestInfo.User.ID);

            return answer;
        }

        /// <summary>
        /// Create A Discord Question As List Options
        /// </summary>
        /// <param name="requestInfo">User To Send</param>
        /// <param name="question">Message Text</param>
        /// <param name="options">List Of Options</param>
        /// <returns>User Response Text</returns>
        public async Task<string> OptionsQuestion(AgentRequestInfo requestInfo, string question, List<string> options)
        {
            var yesReact = new Emoji("\u2705");
            var msgList = new List<RestUserMessage>();

            // Send Question
            await requestInfo.Context.Channel.SendMessageAsync($"> **{question}**");

            // Send Options
            foreach (var option in options)
                msgList.Add(await requestInfo.Context.Channel.SendMessageAsync($"`{option}`"));

            await requestInfo.Context.Channel.SendMessageAsync($"------------------");

            // Send (Yes) Emote
            foreach (var message in msgList)
                await message.AddReactionAsync(yesReact);

            string answer = await WaitListQuestion(requestInfo.User.ID, msgList.Select(mt => mt.Id).ToList());

            // Don't wait remove
            foreach (var message in msgList.Where(mt => mt.Content != answer))
            {
                #pragma warning disable 4014
                message.RemoveReactionAsync(yesReact, CurrentBot);
                #pragma warning restore 4014
            }

            return answer.Replace("`", "");
        }
        #endregion
    }
}
