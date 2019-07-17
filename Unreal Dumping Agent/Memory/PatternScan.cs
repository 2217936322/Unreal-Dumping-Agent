using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unreal_Dumping_Agent.Memory
{
    /**
     * @todo Complete PatternScanner
     * @body Add FindPattern function, and test it
     */
    public static class PatternScan
    {
        public struct Pattern
        {
            public string Name;
            public List<byte> Sig;
            public int Len;
            public int Offset;
            public uint Wildcard;
        }

        public static Pattern Parse(string name, int offset, string hexStr, uint wildcard, string delimiter = " ")
        {
            if (!string.IsNullOrEmpty(delimiter))
                hexStr = hexStr.Replace(delimiter, "");

            string wildcardStr = wildcard.ToString("X2");
            var bytes = new List<byte>();

            for (int i = 0; i < hexStr.Length; i += 2)
            {
                string byteStr = hexStr.Substring(i, 2);
                if (byteStr == "??")
                    byteStr = wildcardStr;

                bytes.Add(byte.Parse(byteStr, NumberStyles.HexNumber));
            }

            var ret = new Pattern
            {
                Name = name,
                Sig = bytes,
                Offset = offset,
                Len = bytes.Count,
                Wildcard = wildcard
            };
            return ret;
        }
    }
}
