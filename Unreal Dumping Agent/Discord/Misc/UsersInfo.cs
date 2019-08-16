using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace Unreal_Dumping_Agent.Discord.Misc
{
    public enum UserOrder
    {
        GetProcess
    }
    public class UsersInfo
    {
        public ulong ID { get; set; }
        public string IP { get; set; }

        public UserOrder LastOrder { get; set; }

        public IntPtr GnamesPtr { get; set; }
        public IntPtr GobjectsPtr { get; set; }
    }
}
