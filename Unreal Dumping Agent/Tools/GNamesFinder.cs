using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Memory;
using Unreal_Dumping_Agent.UtilsHelper;
using static Unreal_Dumping_Agent.Memory.PatternScanner;

namespace Unreal_Dumping_Agent.Tools
{
    public static class GNamesFinder
    {
        private static readonly Pattern NoneSig         = Parse("None", 0, "4E 6F 6E 65 00");
        private static readonly Pattern ByteSig         = Parse("Byte", 0, "42 79 74 65 50 72 6F 70 65 72 74 79 00");
        private static readonly Pattern IntSig          = Parse("Int", 0, "49 6E 74 50 72 6F 70 65 72 74 79 00");
        private static readonly Pattern MulticastSig    = Parse("MulticastDelegate", 0, "4D 75 6C 74 69 63 61 73 74 44 65 6C 65 67 61 74 65 50 72 6F 70 65 72 74 79");

        public static Task<List<IntPtr>> Find(Memory.Memory memory)
        {
            return Task.Run(async () =>
            {
                var ret = new List<IntPtr>();
                var result = await FindPattern(memory, new List<Pattern> { NoneSig, ByteSig, IntSig, MulticastSig });

                var noneR = result[NoneSig.Name];
                var byteR = result[ByteSig.Name];
                var intR = result[IntSig.Name];
                var multicastR = result[MulticastSig.Name];

                var cmp1 = GetNearNumbers(noneR, byteR, 0x150);
                var cmp2 = GetNearNumbers(cmp1, intR, 0x150);
                var cmp3 = GetNearNumbers(cmp2, multicastR, 0x400);

                int nameOffset = Utils.MemObj.Is64Bit ? 0x10 : 0x8;
                if (cmp3.Count > 0)
                    nameOffset = Utils.CalcNameOffset(cmp3[0]);

                var searchResult = new List<IntPtr>();
                for (int i = 0; i < cmp3.Count; i++)
                {
                    var scanVal = GetChunksAddress(cmp3[i] - nameOffset);
                    if (scanVal == IntPtr.Zero)
                        continue;

                    var addressHolder = await Utils.ScanObj.Scan(
                        scanVal.ToInt64(),
                        Utils.MemObj.Is64Bit
                            ? Scanner.ScanAlignment.Alignment8Bytes
                            : Scanner.ScanAlignment.Alignment4Bytes,
                        Scanner.ScanType.TypeExact);

                    if (addressHolder.Count == 0)
                    {
                        ret.RemoveAt(i);
                        continue;
                    }

                    // For GNames most time has 2 static address so we take 3 xD
                    addressHolder.Reverse();
                    for (int iStatic = 0; iStatic < addressHolder.Count && iStatic < 3; iStatic++)
                        searchResult.Add(addressHolder[iStatic]);
                }

                ret.AddRange(searchResult);
                return ret;
            });
        }

        private static IntPtr GetChunksAddress(IntPtr fNameAddress)
        {
            IntPtr ret = fNameAddress;
            var addressHolder = Utils.ScanObj.Scan(
                Utils.MemObj.Is64Bit ? ret.ToInt64() : ret.ToInt32(),
                Utils.MemObj.Is64Bit ? Scanner.ScanAlignment.Alignment8Bytes : Scanner.ScanAlignment.Alignment4Bytes,
                Scanner.ScanType.TypeExact
            ).Result;

            if (addressHolder.Count == 0)
                return IntPtr.Zero;

            foreach (var ptr in addressHolder)
            {
			    // Any address larger than that is usually garbage
                if (ptr.ToInt64() > 0x7ff000000000)
                    continue;

                // Scan for Gnames chunks address
                var gnameArrayAddress = Utils.ScanObj.Scan(
                    Utils.MemObj.Is64Bit ? ptr.ToInt64() : ptr.ToInt32(),
                    Utils.MemObj.Is64Bit ? Scanner.ScanAlignment.Alignment4Bytes : Scanner.ScanAlignment.Alignment8Bytes,
                    Scanner.ScanType.TypeExact
                ).Result;

                foreach (var chunkPtr in gnameArrayAddress)
                {
                    if (!Utils.IsValidGNamesChunksAddress(chunkPtr)) continue;

                    ret = chunkPtr;
                    break;
                }
            }

            return ret;
        }

        private static List<IntPtr> GetNearNumbers(IEnumerable<IntPtr> list1, IReadOnlyCollection<IntPtr> list2, int maxValue)
        {
            // Cool right .? xD
            return (from IntPtr i in list1 from IntPtr i2 in list2 where Math.Abs(i.ToInt64() - i2.ToInt64()) <= maxValue select i).ToList();
        }
    }
}
