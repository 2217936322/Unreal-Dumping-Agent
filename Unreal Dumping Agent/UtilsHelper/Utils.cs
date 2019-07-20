using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

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

        #region Process
        public static bool Is64Bit(this Process process)
        {
            // PROCESS_QUERY_INFORMATION 
            var processHandle = Win32.OpenProcess(Win32.ProcessAccessFlags.QueryInformation, false, process.Id);
            bool ret = Win32.IsWow64Process(processHandle, out bool retVal) && retVal;
            Win32.CloseHandle(processHandle);
            return !ret;
        }
        public static TDelegate GetProcAddress<TDelegate>(string dllName, string funcName) where TDelegate : Delegate
        {
            IntPtr funcAddress = Win32.GetProcAddress(Win32.LoadLibrary(dllName), funcName);
            return Marshal.GetDelegateForFunctionPointer<TDelegate>(funcAddress);
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
            return true;
            // File.GetAttributes()
        }
        #endregion
    }
}
