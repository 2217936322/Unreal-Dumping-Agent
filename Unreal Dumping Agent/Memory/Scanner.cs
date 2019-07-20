using System;
using System.Collections.Generic;
using System.Text;

namespace Unreal_Dumping_Agent.Memory
{
    /**
     * Thanks To Roman_Ablo @ GuidedHacking
     * https://guidedhacking.com/threads/hyperscan-fast-vast-memory-scanner.9659/
     * i converted to C#
     */
    public class Scanner
    {
        public enum ScanAlignment
        {
            AlignmentByte = 0x1,
            Alignment2Bytes = 0x2,
            Alignment4Bytes = 0x4,
            Alignment8Bytes = 0x8
        }
        public enum ScanType
        {
            TypeExact = 0x00E,
            TypeSmaller = 0x0E,
            TypeBigger = 0x000E,
            TypeDifferent = 0x0000A,
            TypeAll = 0xABCDEF
        }
        public enum ScanMode
        {
            ScanFirst = 0xFF0,
            ScanNext = 0x0FF
        }


    }
}
