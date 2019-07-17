using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    /// <summary>
    /// This class contains all Window API
    /// </summary>
    public static class Win32
    {
        #region Structs Enums Flags
        public const uint HandleFlagInherit = 0x00000001;
        public const uint HandleFlagProtectFromClose = 0x00000002;
        #endregion

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetHandleInformation(IntPtr hObject, out uint lpdwFlags);
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtSuspendProcess(IntPtr processHandle);
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtResumeProcess(IntPtr processHandle);
        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtTerminateProcess(IntPtr processHandle, int exitStatus);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out, MarshalAs(UnmanagedType.AsAny)] object lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [MarshalAs(UnmanagedType.AsAny)] object lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);
    }
}
