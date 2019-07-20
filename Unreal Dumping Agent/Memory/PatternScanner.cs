using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Memory
{
    /**
     * @todo Complete PatternScanner
     * @body test it
     */
    using PatternScanResult = Dictionary<string, List<IntPtr>>;
    public static class PatternScanner
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
        public static PatternScanResult FindPattern(Memory mem, IntPtr start, IntPtr end, List<Pattern> patterns, bool firstOnly = false)
        {
            var ret = new PatternScanResult();
            var memRegions = new List<Utils.MemoryRegion>();

            // Init ret list
            foreach (var pattern in patterns)
                ret[pattern.Name] = new List<IntPtr>();

            // Get System Info
            Win32.GetSystemInfo(out var si);

            // Init Start and End address
            if (start.ToInt64() < si.MinimumApplicationAddress.ToInt64())
                start = si.MinimumApplicationAddress;
            if (end.ToInt64() < si.MaximumApplicationAddress.ToInt64())
                end = si.MaximumApplicationAddress;

            // Cycle through memory based on RegionSize
            {
                uint sizeOfStruct = (uint)Marshal.SizeOf<Win32.MemoryBasicInformation>();
                IntPtr currentAddress = start;

                do
                {
                    // Get Region information
                    var exitLoop = Win32.VirtualQueryEx(mem.TargetProcess.Handle, currentAddress, out var info, sizeOfStruct) != sizeOfStruct &&
                                    currentAddress.ToInt64() < end.ToInt64();

                    if (exitLoop)
                        break;

                    // Size will used to alloc and read memory
                    long allocSize = end.ToInt64() - start.ToInt64() >= (long)info.RegionSize ? (long)info.RegionSize : end.ToInt64() - start.ToInt64();

                    // Bad Memory
                    if (!((Win32.MemoryState)info.State).HasFlag(Win32.MemoryState.MemCommit) ||
                        ((Win32.MemoryProtection)info.Protect).HasFlag(Win32.MemoryProtection.PageNoAccess | Win32.MemoryProtection.PageWriteCopy | Win32.MemoryProtection.PageTargetsInvalid) ||
                        info.RegionSize > 0x300000)
                    {
                        // Get next address
                        currentAddress = new IntPtr(currentAddress.ToInt64() + allocSize);
                        continue;
                    }

                    // Insert region information on Regions Holder
                    memRegions.Add(new Utils.MemoryRegion { Address = currentAddress, RegionSize = allocSize});

                    // Get next address
                    currentAddress = new IntPtr(currentAddress.ToInt64() + allocSize);

                } while (true);
            }

            object lockObj = new object();
            Parallel.ForEach(memRegions, (memRegion, loopState) =>
            {
                var buf = mem.ReadBytes(memRegion.Address, memRegion.RegionSize);

                if (buf.Length == 0)
                    return;

                foreach (var pattern in patterns)
                {
                    int k = 0;
                    var uPattern = pattern.Sig;
                    var nLen = pattern.Len;

                    for (int j = 0; j <= buf.Length; j++)
                    {
                        // If the byte matches our pattern or wildcard
                        if (buf[j] == uPattern[k] || uPattern[k] == pattern.Wildcard)
                        {
                            // Did we find it?
                            if (++k != nLen) continue;

                            // Our match function places us at the begin of the pattern
                            // To locate the pointer we need to subtract nOffset bytes
                            lock (lockObj)
                            {
                                ret[pattern.Name].Add(new IntPtr(memRegion.Address.ToInt64() + j - (nLen - 1) + pattern.Offset));
                                if (firstOnly)
                                {
                                    loopState.Stop();
                                    break;
                                }
                            }
                        }
                        else
                        {
                            k = 0;
                        }
                    }
                }
            });

            return ret;
        }
    }
}
