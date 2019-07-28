using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Tools
{
    public class GObjectsFinder
    {
        public static List<IntPtr> Find()
        {
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
                    if (!((Win32.MemoryState)info.State).HasFlag(Win32.MemoryState.MemCommit) ||
                        ((Win32.MemoryProtection)info.Protect).HasFlag(Win32.MemoryProtection.PageNoAccess | Win32.MemoryProtection.PageWriteCopy | Win32.MemoryProtection.PageTargetsInvalid) ||
                        info.RegionSize > 0x300000)
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
            }

            Parallel.ForEach(memRegions, (memReg, loopState) => 
            {
                
            });

            return new List<IntPtr>();
        }
    }
}
