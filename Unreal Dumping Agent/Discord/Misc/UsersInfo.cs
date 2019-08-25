using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace Unreal_Dumping_Agent.Discord.Misc
{
    /// <summary>
    /// Last Order Type, Useful To Make Q & A Task.<para/>
    /// Like Bot Ask User For GameName or Process Name
    /// </summary>
    public enum UserOrder
    {
        GetProcess
    }

    /// <summary>
    /// Contains Information About Discord User
    /// </summary>
    public class UsersInfo
    {
        public ulong ID { get; set; }
        public string IP { get; set; }

        public UserOrder LastOrder { get; set; }

        public IntPtr GnamesPtr { get; set; }
        public IntPtr GobjectsPtr { get; set; }
    }
}
