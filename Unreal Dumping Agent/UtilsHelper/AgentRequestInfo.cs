using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Unreal_Dumping_Agent.Discord.Misc;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    public class AgentRequestInfo
    {
        public UsersInfo User { get; set; }
        public SocketUserMessage SocketMessage { get; set; }
        public SocketCommandContext Context { get; set; }
    }
}
