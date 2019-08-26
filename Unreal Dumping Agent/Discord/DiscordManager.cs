using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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
        private Dictionary<ulong, string> _waitQuestion;

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
            _waitQuestion = new Dictionary<ulong, string>();

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
        private Task Client_OnReactionAdded(Cacheable<IUserMessage, ulong> cacheAble, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (_waitedReactions.ContainsKey(reaction.MessageId) && reaction.UserId != CurrentBot.Id)
                _waitedReactions[reaction.MessageId].Add(reaction.Emote);

            return Task.Run(() => ReactionAddedHandler?.Invoke(reaction));
        }
        private Task Client_OnReactionRemoved(Cacheable<IUserMessage, ulong> cacheAble, ISocketMessageChannel channel, SocketReaction reaction)
        {
            return Task.Run(() => ReactionRemovedHandler?.Invoke(reaction));
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
            if (_waitQuestion.ContainsKey(message.Author.Id))
                _waitQuestion[message.Author.Id] = message.Content;

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
            _waitQuestion[userId] = string.Empty;

            while (_waitQuestion[userId].Empty())
                await Task.Delay(8);

            string ret = _waitQuestion[userId];
            _waitQuestion.Remove(userId);
            return ret;
        }


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

            var message = await requestInfo.Context.Channel.SendMessageAsync($"**{question}**");
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
            await requestInfo.Context.Channel.SendMessageAsync($"**{question}**");
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
            var message = await requestInfo.Context.Channel.SendMessageAsync(question);
            string answer = await WaitStringQuestion(requestInfo.User.ID);

            return answer;
        }
        #endregion
    }
}
