using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Memory;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Tools
{
    public class GObjectsFinder
    {
        public static async Task<List<IntPtr>> Find()
        {
            var ret = new List<IntPtr>();
            var memRegions = new List<Utils.MemoryRegion>();
            // Cycle through memory based on RegionSize
            {
                uint sizeOfStruct = (uint)Marshal.SizeOf<Win32.MemoryBasicInformation>();
                IntPtr currentAddress = Utils.GameStartAddress();

                do
                {
                    // Get Region information
                    var exitLoop = Win32.VirtualQueryEx(Utils.MemObj.TargetProcess.Handle, currentAddress, out var info, sizeOfStruct) != sizeOfStruct ||
                                   currentAddress.ToInt64() > Utils.GameEndAddress().ToInt64();

                    if (exitLoop)
                        break;

                    // Size will used to alloc and read memory
                    long allocSize = 
                        Utils.GameEndAddress().ToInt64() - Utils.GameStartAddress().ToInt64() >= (long)info.RegionSize 
                        ? (long)info.RegionSize 
                        : Utils.GameEndAddress().ToInt64() - Utils.GameStartAddress().ToInt64();

                    // Bad Memory
                    if (((Win32.MemoryProtection)info.Protect).HasFlag(Win32.MemoryProtection.PageNoAccess))
                    {
                        // Get next address
                        currentAddress = new IntPtr(currentAddress.ToInt64() + allocSize);
                        continue;
                    }

                    // Insert region information on Regions Holder
                    memRegions.Add(new Utils.MemoryRegion { Address = currentAddress, RegionSize = allocSize });

                    // Get next address
                    currentAddress = new IntPtr(currentAddress.ToInt64() + allocSize);

                } while (true);

                var lockObj = new object(); // 0x227BDCD0000
                Parallel.ForEach(memRegions, (memReg, loopState) =>
                {
                    // Insert region information on Regions Holder
                    if (!Utils.IsValidGObjectsAddress(memReg.Address)) return;

                    lock (lockObj)
                        ret.Add(memReg.Address);
                });
            }

            // Check if there a GObjects Chunks
            {
                var searchResult = new List<IntPtr>();
                for (int i = 0; i < ret.Count; i++)
                {
                    var addressHolder = await Utils.ScanObj.Scan(ret[i].ToInt64(), 
                        Utils.MemObj.Is64Bit ? Scanner.ScanAlignment.Alignment8Bytes : Scanner.ScanAlignment.Alignment4Bytes, 
                        Scanner.ScanType.TypeExact);

                    if (addressHolder.Count == 0)
                    {
                        ret.RemoveAt(i);
                        continue;
                    }

                    foreach (var addressPtr in addressHolder)
                    {
                        if (Utils.MemObj.IsStaticAddress(addressPtr))
                        {
                            var addressHolder2 = await Utils.ScanObj.Scan(addressPtr.ToInt64(),
                                Utils.MemObj.Is64Bit ? Scanner.ScanAlignment.Alignment8Bytes : Scanner.ScanAlignment.Alignment4Bytes,
                                Scanner.ScanType.TypeExact);

                            foreach (var holder2Address in addressHolder2)
                                searchResult.Add(holder2Address);

                            if (addressHolder2.Count == 0)
                                continue;
                        }

                        searchResult.Add(addressPtr);
                    }
                }

                ret.AddRange(searchResult);
                searchResult.Clear();
                ret.Reverse();
            }

            return ret;
        }
    }
}
