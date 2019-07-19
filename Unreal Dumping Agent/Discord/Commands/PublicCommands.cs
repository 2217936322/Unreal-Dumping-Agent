using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Unreal_Dumping_Agent.Discord.Commands
{
    public class PublicCommands : ModuleBase<SocketCommandContext>
    {
        [Command("add"), Summary("Start private chat with user")]
        public async Task AddMe()
        {
            await Context.User.SendMessageAsync("Hi noob.");
        }
    }
}
