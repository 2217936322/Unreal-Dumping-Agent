using System;
using System.Collections.Generic;
using System.Text;

namespace Unreal_Dumping_Agent.Discord.Misc
{
    public enum UserOrder
    {
        GetProcess
    }
    public class UsersInfo
    {
        public ulong Id { get; set; }
        public string Ip { get; set; }
        public UserOrder LastOrder { get; set; }
    }
}
