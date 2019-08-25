using System;
using System.Collections.Generic;
using System.Text;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace Unreal_Dumping_Agent.Discord.Misc
{
    /// <summary>
    /// Contains Information About User That's Send A Request For The Bot
    /// </summary>
    public class AgentRequestInfo
    {
        public UsersInfo User { get; set; }
        public RestUserMessage AgentMessage { get; set; }
        public SocketUserMessage SocketMessage { get; set; }
        public SocketCommandContext Context { get; set; }
    }
}
