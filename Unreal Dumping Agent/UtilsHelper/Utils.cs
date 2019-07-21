using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Unreal_Dumping_Agent.Memory;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    public static class Utils
    {
        public const string Version = "1.0.0";
        public const string Title = "Welcome Agent";
        public const string UnrealWindowClass = "UnrealWindow";

        #region Enums
        public enum BotType
        {
            Local,
            Public
        }
        #endregion

        #region Structs
        public struct MemoryRegion
        {
            public IntPtr Address;
            public long RegionSize;
        }
        #endregion

        #region Variables
        public static BotType BotWorkType;
        public static Memory.Memory MemObj;
        public static Scanner ScanObj;
        #endregion

        #region Tool
        public static bool ProgramIs64()
        {
#if x64
            return true;
#else
            return true;
#endif
        }
        public static bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return true;
#endif
        }
        public static void ConsoleText(string category, string message, ConsoleColor textColor)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{category,-10}] ");
            Console.ForegroundColor = textColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        #endregion

        #region Address Stuff
        public static int PointerSize()
        {
            return MemObj.Is64Bit ? 0x8 : 0x4;
        }
        public static bool IsValidRemoteAddress(IntPtr address)
        {
            if (MemObj == null || address == IntPtr.Zero || address.ToInt64() < 0)
                return false;

            if (Win32.VirtualQueryEx(MemObj.ProcessHandle, address, out var info, (uint)Marshal.SizeOf<Win32.MemoryBasicInformation>()) != 0)
            {
		        // Bad Memory
                return (info.State & (int)Win32.MemoryState.MemCommit) != 0 && (info.Protect & (int)Win32.MemoryProtection.PageNoAccess) == 0;
            }

            return false;
        }
        public static bool IsValidRemotePointer(IntPtr address)
        {
            if (MemObj == null || address == IntPtr.Zero || address.ToInt64() < 0)
                return false;

            address = MemObj.ReadAddress(address);
            return IsValidRemoteAddress(address);
        }
        public static bool IsValidGNamesAddress(IntPtr address, bool chunkCheck = false)
        {
            if (MemObj == null || !IsValidRemoteAddress(address))
                return false;

            if (!chunkCheck && !IsValidRemotePointer(address))
                return false;

            if (!chunkCheck)
                address = MemObj.ReadAddress(address);

            int nullCount = 0;

            // Chunks array must have null pointers, if not then it's not valid
            for (int i = 0; i < 50 && nullCount <= 3; i++)
            {
                // Read Chunk Address
                var offset = i * PointerSize();
                var chunkAddress = MemObj.ReadAddress(address + offset);
                if (chunkAddress == IntPtr.Zero)
                    ++nullCount;
            }

            if (nullCount <= 3)
                return false;

            // Read First FName Address
            var noneFName = MemObj.ReadAddress(MemObj.ReadAddress(address));
            if (!IsValidRemoteAddress(noneFName))
                return false;

            // Search for none FName
            var pattern = PatternScanner.Parse("NoneSig", 0, "4E 6F 6E 65 00");
            var result = PatternScanner.FindPattern(MemObj, noneFName, noneFName + 0x50, new List<PatternScanner.Pattern> { pattern }, true).Result;

            return result["NoneSig"].Count > 0;
        }
        public static bool IsValidGNamesChunksAddress(IntPtr chunkPtr)
        {
            return IsValidGNamesAddress(chunkPtr, true);
        }
        public static int CalcNameOffset(IntPtr address)
        {
            long curAddress = address.ToInt64();
            uint sizeOfStruct = (uint)Marshal.SizeOf<Win32.MemoryBasicInformation>();

            while (
                Win32.VirtualQueryEx(MemObj.ProcessHandle, (IntPtr)curAddress, out var info, sizeOfStruct) == sizeOfStruct &&
                info.BaseAddress != (ulong)curAddress &&
                curAddress >= address.ToInt64() - 0x10)
            {
                --curAddress;
            }

            return (int)(address.ToInt64() - curAddress);
        }

        #endregion

        #region Process
        public static bool Is64Bit(this Process process)
        {
            // PROCESS_QUERY_INFORMATION 
            var processHandle = Win32.OpenProcess(Win32.ProcessAccessFlags.QueryInformation, false, process.Id);
            bool ret = Win32.IsWow64Process(processHandle, out bool retVal) && retVal;
            Win32.CloseHandle(processHandle);
            return !ret;
        }
        public static TDelegate GetProcAddress<TDelegate>(string dllName, string funcName) where TDelegate : System.Delegate
        {
            IntPtr funcAddress = Win32.GetProcAddress(Win32.LoadLibrary(dllName), funcName);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(funcAddress);
        }
        #endregion

        #region Extensions
        public static IEnumerable<long> SteppedIterator(long startIndex, long endIndex, long stepSize)
        {
            for (long i = startIndex; i < endIndex; i += stepSize)
                yield return i;
        }
        public static bool Equal(this byte[] b1, byte[] b2)
        {
            if (b1 == b2) return true;
            if (b1 == null || b2 == null) return false;
            if (b1.Length != b2.Length) return false;
            for (int i = 0; i < b1.Length; i++)
                if (b1[i] != b2[i]) return false;
            return true;
        }
        #endregion

        #region UnrealEngine
        /// <summary>
        /// Get ProcessID for unreal games
        /// </summary>
        /// <param name="windowHandle">Window of unreal game</param>
        /// <param name="windowTitle">Window title of unreal games</param>
        /// <returns>ProcessId of unreal games</returns>
        public static int DetectUnrealGame(out IntPtr windowHandle, out string windowTitle)
        {
            windowHandle = IntPtr.Zero;
            windowTitle = string.Empty;

            retry:
            IntPtr childControl = Win32.FindWindowEx(new IntPtr(Win32.HwndDesktop), IntPtr.Zero, UnrealWindowClass, null);
            if (childControl == IntPtr.Zero)
                return 0;

            Win32.GetWindowThreadProcessId(childControl, out int pId);

            if (Process.GetProcessById(pId).ProcessName.Contains("EpicGamesLauncher"))
            {
                childControl = Win32.FindWindowEx(new IntPtr(Win32.HwndDesktop), childControl, UnrealWindowClass, null);
                goto retry;
            }

            windowHandle = childControl;

            var sb = new StringBuilder();
            Win32.GetWindowText(childControl, sb, 30);
            windowTitle = sb.ToString();

            return pId;
        }
        public static int DetectUnrealGame(out string windowTitle)
        {
            return DetectUnrealGame(out _, out windowTitle);
        }
        public static int DetectUnrealGame()
        {
            return DetectUnrealGame(out _, out _);
        }
        public static bool UnrealEngineVersion(out string version)
        {
            version = null;
            if (MemObj.TargetProcess == null)
                throw new ArgumentException("init MemObj first.");
            if (MemObj.TargetProcess.MainModule == null)
                return false;

            var fVersion = FileVersionInfo.GetVersionInfo(MemObj.TargetProcess.MainModule.FileName);
            version = fVersion.ProductVersion;

            return true;
        }
        #endregion

    }
}
