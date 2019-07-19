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

        public enum ProcessorArchitecture
        {
            X86 = 0,
            X64 = 9,
            @Arm = -1,
            Itanium = 6,
            Unknown = 0xFFFF,
        }
        [Flags]
        public enum MemoryState
        {
            MEM_COMMIT = 0x1000,
            MEM_FREE = 0x10000,
            MEM_RESERVE = 0x2000
        }
        [Flags]
        public enum MemoryType
        {
            MEM_IMAGE = 0x1000000,
            MEM_MAPPED = 0x40000,
            MEM_PRIVATE = 0x20000
        }
        [Flags]
        public enum MemoryProtection
        {
            PAGE_EXECUTE = 0x00000010,
            PAGE_EXECUTE_READ = 0x00000020,
            PAGE_EXECUTE_READWRITE = 0x00000040,
            PAGE_EXECUTE_WRITECOPY = 0x00000080,
            PAGE_NOACCESS = 0x00000001,
            PAGE_READONLY = 0x00000002,
            PAGE_READWRITE = 0x00000004,
            PAGE_WRITECOPY = 0x00000008,
            PAGE_GUARD = 0x00000100,
            PAGE_NOCACHE = 0x00000200,
            PAGE_WRITECOMBINE = 0x00000400,
            PAGE_TARGETS_INVALID = 0x40000000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemInfo
        {
            public ProcessorArchitecture ProcessorArchitecture; // WORD
            public uint PageSize; // DWORD
            public IntPtr MinimumApplicationAddress; // (long)void*
            public IntPtr MaximumApplicationAddress; // (long)void*
            public IntPtr ActiveProcessorMask; // DWORD*
            public uint NumberOfProcessors; // DWORD (WTF)
            public uint ProcessorType; // DWORD
            public uint AllocationGranularity; // DWORD
            public ushort ProcessorLevel; // WORD
            public ushort ProcessorRevision; // WORD
        }
#if x64
        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryBasicInformation
        {
            public ulong BaseAddress;
            public ulong AllocationBase;
            public int AllocationProtect;
            public int __alignment1;
            public ulong RegionSize;
            public int State;
            public int Protect;
            public int Type;
            public int __alignment2;
        }
#else
        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryBasicInformation
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }
#endif

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
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out, MarshalAs(UnmanagedType.AsAny)] object lpBuffer, long dwSize, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [MarshalAs(UnmanagedType.AsAny)] object lpBuffer, long dwSize, out IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void GetSystemInfo(out SystemInfo info);
        [DllImport("kernel32.dll")]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MemoryBasicInformation lpBuffer, uint dwLength);
    }
}
