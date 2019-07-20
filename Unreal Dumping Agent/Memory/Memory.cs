using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Memory
{
    /*
     * @todo Add SetDebugPrivileges
     * @body Add debug stuff.
     */
    public class Memory
    {
        private delegate int NtQueryVirtualMemory(IntPtr processHandle, IntPtr baseAddress,
            Win32.MemoryInformationClass memoryInformationClass, [Out] object buffer,
            ulong length, ref ulong resultLength);

        public Process TargetProcess { get; }
        public IntPtr ProcessHandle { get; }
        public bool Is64Bit { get; }

        public Memory(Process targetProcess)
        {
            if (targetProcess.Handle == IntPtr.Zero || targetProcess.HasExited)
                return;

            // Open Process
            ProcessHandle = Win32.OpenProcess(Win32.ProcessAccessFlags.All, false, targetProcess.Id);
            Is64Bit = targetProcess.Is64Bit();
            TargetProcess = targetProcess;
        }
        public Memory(int processId): this(Process.GetProcessById(processId)) { }

        #region Process Control
        public List<ProcessModule> GetModuleList()
        {
            return TargetProcess.Modules.Cast<ProcessModule>().ToList();
        }
        public bool GetModuleInfo(string moduleName, out ProcessModule pModule)
        {
            pModule = null;
            try
            {
                pModule = TargetProcess.Modules.Cast<ProcessModule>().First(m => m.ModuleName == moduleName);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool HandleIsValid(IntPtr processHandle)
        {
            if (processHandle == IntPtr.Zero || Process.GetProcesses().FirstOrDefault(p => p.Handle == processHandle) == null)
                return false;

            return Win32.GetHandleInformation(processHandle, out _);
        }
        public static bool IsValidProcess(int pId, out Process processHandle)
        {
            processHandle = null;
            try
            {
                processHandle = Process.GetProcessById(pId);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool IsValidProcess(int pId)
        {
            return IsValidProcess(pId, out _);
        }
        public bool IsValidProcess()
        {
            return !TargetProcess.HasExited;
        }
        public bool SuspendProcess()
        {
            return Win32.NtSuspendProcess(TargetProcess.Handle) >= 0;
        }
        public bool ResumeProcess()
        {
            return Win32.NtResumeProcess(TargetProcess.Handle) >= 0;
        }
        public bool TerminateProcess()
        {
            return Win32.NtTerminateProcess(TargetProcess.Handle, 0) >= 0;
        }
        #endregion

        #region ReadWriteMemory
        /// <summary>
        /// Read address based on Target Game (32 or 64)bit
        /// </summary>
        public IntPtr ReadAddress(IntPtr lpBaseAddress)
        {
            return new IntPtr(Is64Bit ? Rpm<long>(lpBaseAddress) : Rpm<int>(lpBaseAddress));
        }

        public byte[] ReadBytes(IntPtr lpBaseAddress, long len)
        {
            var buffer = new byte[len];
            Win32.ReadProcessMemory(TargetProcess.Handle, lpBaseAddress, buffer, len, out _);

            return buffer;
        }
        public string ReadString(IntPtr lpBaseAddress, bool isUnicode = false)
        {
            int charSize = isUnicode ? 2 : 1;
            string ret = string.Empty;

            while (true)
            {
                var buf = ReadBytes(lpBaseAddress, charSize);

                // Null-Terminator
                if (buf.All(b => b == 0))
                    break;

                ret += System.Text.Encoding.UTF8.GetString(buf);
                lpBaseAddress += charSize;
            }

            return ret;
        }
        public byte[] ReadPBytes(IntPtr lpBaseAddress, long len)
        {
            return ReadBytes(ReadAddress(lpBaseAddress), len);
        }
        public string ReadPString(IntPtr lpBaseAddress, bool isUnicode = false)
        {
            return ReadString(ReadAddress(lpBaseAddress), isUnicode);
        }

        public T Rpm<T>(IntPtr lpBaseAddress) where T : struct
        {
            var buffer = new T();
            Win32.ReadProcessMemory(TargetProcess.Handle, lpBaseAddress, buffer, Marshal.SizeOf(buffer), out _);

            return buffer;
        }
        public T RpmPointer<T>(IntPtr lpBaseAddress) where T : struct
        {
            return Rpm<T>(ReadAddress(lpBaseAddress));
        }
        public bool Wpm<T>(IntPtr lpBaseAddress, T value) where T : struct
        {
            return Win32.WriteProcessMemory(TargetProcess.Handle, lpBaseAddress, value, Marshal.SizeOf(value), out _);
        }
        public bool WpmPointer<T>(IntPtr lpBaseAddress, T value) where T : struct
        {
            return Wpm(ReadAddress(lpBaseAddress), value);
        }
        
        #endregion

        public bool IsStaticAddress(IntPtr address)
        {
            if (!IsValidProcess())
                throw new Exception("Process not found !!");
            if (address == IntPtr.Zero)
                return false;

            var ntQueryVirtualMemory = Utils.GetProcAddress<NtQueryVirtualMemory>("ntdll.dll", "NtQueryVirtualMemory");
            ulong length = 0;
            Win32.SectionInfo sectionInformation = new Win32.SectionInfo();
            int retStatus = ntQueryVirtualMemory(ProcessHandle, 
                address, 
                Win32.MemoryInformationClass.MemoryMappedFilenameInformation, 
                sectionInformation,
                (ulong)Marshal.SizeOf<Win32.SectionInfo>(),
                ref length);


            if (!Win32.NtSuccess(retStatus))
                return false;

            string deviceName = sectionInformation.SzData;
            string filePath = deviceName;
            filePath = filePath.Substring(filePath.LastIndexOf('\\') - 1);

            string driveLetters = string.Empty;
            var driveSize = Win32.GetLogicalDriveStrings(Win32.MaxPath, driveLetters);

            return true;
        }
    }
}
