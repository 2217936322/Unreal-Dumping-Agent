using System;
using System.Collections.Generic;
using System.Text;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    public class CorrmStringBuilder
    {
        public readonly StringBuilder BaseStr;

        public CorrmStringBuilder() : this(string.Empty) { }

        public CorrmStringBuilder(string str)
        {
            BaseStr = new StringBuilder(str);
        }

        public static CorrmStringBuilder operator +(CorrmStringBuilder sBuilder, string str)
        {
            sBuilder.BaseStr.Append(str);
            return sBuilder;
        }

        public static implicit operator string(CorrmStringBuilder builder)
        {
            return builder.ToString();
        }

        public override string ToString()
        {
            return BaseStr.ToString();
        }
    }
}
