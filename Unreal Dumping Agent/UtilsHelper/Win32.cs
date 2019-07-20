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
        public const int MaxPath = 260;
        public const int HwndDesktop = 0;

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
            MemCommit = 0x1000,
            MemFree = 0x10000,
            MemReserve = 0x2000
        }
        [Flags]
        public enum MemoryType
        {
            MemImage = 0x1000000,
            MemMapped = 0x40000,
            MemPrivate = 0x20000
        }
        [Flags]
        public enum MemoryProtection
        {
            PageExecute = 0x00000010,
            PageExecuteRead = 0x00000020,
            PageExecuteReadwrite = 0x00000040,
            PageExecuteWriteCopy = 0x00000080,
            PageNoAccess = 0x00000001,
            PageReadonly = 0x00000002,
            PageReadwrite = 0x00000004,
            PageWriteCopy = 0x00000008,
            PageGuard = 0x00000100,
            PageNocache = 0x00000200,
            PageWriteCombine = 0x00000400,
            PageTargetsInvalid = 0x40000000
        }
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }
        public enum MemoryInformationClass
        {
            MemoryBasicInformation,
            MemoryWorkingSetInformation,
            MemoryMappedFilenameInformation,
            MemoryRegionInformation,
            MemoryWorkingSetExInformation,
            MemorySharedCommitInformation,
            MemoryImageInformation,
            MemoryRegionInformationEx,
            MemoryPrivilegedBasicInformation,
            MemoryEnclaveImageInformation,
            MemoryBasicInformationCapped
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
        [StructLayout(LayoutKind.Sequential)]
        public struct SectionInfo
        {
            public short Len;
            public short MaxLen;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string SzData;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pData;
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

        public static bool NtSuccess(int state) => state >= 0;

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
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] object lpBuffer, long dwSize, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, object lpBuffer, long dwSize, out IntPtr lpNumberOfBytesWritten);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void GetSystemInfo(out SystemInfo info);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool IsWow64Process([In] IntPtr processHandle, [Out] out bool wow64Process);
        [DllImport("kernel32.dll")]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MemoryBasicInformation lpBuffer, uint dwLength);
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetLogicalDriveStrings(int nBufferLength, [Out, MarshalAs(UnmanagedType.LPWStr)] string lpBuffer);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    }
}
