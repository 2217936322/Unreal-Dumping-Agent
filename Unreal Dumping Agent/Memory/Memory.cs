using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.UtilsHelper;
using ClrPlus.Windows.Api;
using ClrPlus.Windows.Api.Enumerations;
using ClrPlus.Windows.Api.Structures;

namespace Unreal_Dumping_Agent.Memory
{
    /*
     * TODO: Add SetDebugPrivileges
     * BODY: Add debug stuff
     */
    public class Memory
    {
        private readonly Process _targetProcess;
        private readonly SafeProcessHandle _processHandle;
        private readonly bool _is64Bit;

        public Memory(Process targetProcess)
        {
            if (targetProcess.Handle == IntPtr.Zero || targetProcess.HasExited)
                return;

            // Open Process
            _processHandle = Kernel32.OpenProcess(0x000F0000 | 0x00100000 | 0xFFFF, false, targetProcess.Id);
            _is64Bit = targetProcess.Is64Bit();
            _targetProcess = targetProcess;
        }
        public Memory(int processId) : this(Process.GetProcessById(processId)) { }

        public List<ProcessModule> GetModuleList()
        {
            return _targetProcess.Modules.Cast<ProcessModule>().ToList();
        }
        public bool GetModuleInfo(string moduleName, out ProcessModule pModule)
        {
            pModule = null;
            try
            {
                pModule = _targetProcess.Modules.Cast<ProcessModule>().First(m => m.ModuleName == moduleName);
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
        public bool SuspendProcess()
        {
            return Win32.NtSuspendProcess(_targetProcess.Handle) >= 0;
        }
        public bool ResumeProcess()
        {
            return Win32.NtResumeProcess(_targetProcess.Handle) >= 0;
        }
        public bool TerminateProcess()
        {
            return Win32.NtTerminateProcess(_targetProcess.Handle, 0) >= 0;
        }

        public byte[] ReadBytes(IntPtr lpBaseAddress, int len)
        {
            var buffer = new byte[len];
            Win32.ReadProcessMemory(_targetProcess.Handle, lpBaseAddress, buffer, len, out _);

            return buffer;
        }
        public T Rpm<T>(IntPtr lpBaseAddress) where T : struct
        {
            var buffer = new T();
            Win32.ReadProcessMemory(_targetProcess.Handle, lpBaseAddress, buffer, Marshal.SizeOf(buffer), out _);

            return buffer;
        }
        public T RpmPointer<T>(IntPtr lpBaseAddress) where T : struct
        {
            return Rpm<T>(ReadAddress(lpBaseAddress));
        }
        public bool Wpm<T>(IntPtr lpBaseAddress, T value) where T : struct
        {
            return Win32.WriteProcessMemory(_targetProcess.Handle, lpBaseAddress, value, Marshal.SizeOf(value), out _);
        }
        public bool WpmPointer<T>(IntPtr lpBaseAddress, T value) where T : struct
        {
            return Wpm(ReadAddress(lpBaseAddress), value);
        }
        /// <summary>
        /// Read address based on Target Game (32 or 64)bit
        /// </summary>
        public IntPtr ReadAddress(IntPtr lpBaseAddress)
        {
            return new IntPtr(_is64Bit ? Rpm<long>(lpBaseAddress) : Rpm<int>(lpBaseAddress));
        }
    }
}
