using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using Unreal_Dumping_Agent.Json;
using Unreal_Dumping_Agent.Tools.SdkGen;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Memory
{
    /*
     * @todo Add SetDebugPrivileges
     * @body Add debug stuff.
     */
    public class Memory
    {
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
            return new IntPtr(Is64Bit ? Read<long>(lpBaseAddress) : Read<int>(lpBaseAddress));
        }

        public bool ReadBytes(IntPtr lpBaseAddress, long len, out byte[] bytes)
        {
            bytes = new byte[len];
            return Win32.ReadProcessMemory(TargetProcess.Handle, lpBaseAddress, bytes, len, out _);
        }
        public string ReadString(IntPtr lpBaseAddress, bool isUnicode = false)
        {
            int charSize = isUnicode ? 2 : 1;
            string ret = string.Empty;

            while (true)
            {
                if (!ReadBytes(lpBaseAddress, charSize, out var buf))
                    break;

                // Null-Terminator
                if (buf.All(b => b == 0))
                    break;

                ret += System.Text.Encoding.UTF8.GetString(buf);
                lpBaseAddress += charSize;
            }

            return ret;
        }
        public bool ReadPBytes(IntPtr lpBaseAddress, long len, out byte[] bytes)
        {
            return ReadBytes(ReadAddress(lpBaseAddress), len, out bytes);
        }
        public string ReadPString(IntPtr lpBaseAddress, bool isUnicode = false)
        {
            return ReadString(ReadAddress(lpBaseAddress), isUnicode);
        }

        public bool Read<T>(ref T refStruct, IntPtr lpBaseAddress) where T : struct
        {
            if (!ReadBytes(lpBaseAddress, Marshal.SizeOf<T>(), out var buffer))
                return false;

            refStruct = buffer.ToStructure<T>();

            return true;
        }
        public T Read<T>(IntPtr lpBaseAddress) where T : struct
        {
            var retStruct = new T();
            Read(ref retStruct, lpBaseAddress);
            return retStruct;
        }
        public T ReadPointer<T>(IntPtr lpBaseAddress) where T : struct
        {
            return Read<T>(ReadAddress(lpBaseAddress));
        }


        /// <summary>
        /// Read data from remote process as class.
        /// </summary>
        /// <typeparam name="T">Class type to read</typeparam>
        /// <param name="refClass">Class instance of <see cref="T"/></param>
        /// <param name="lpBaseAddress">Data address on remote process</param>
        /// <param name="jsonMemoryOnly">Set data to <see cref="UnrealMemoryVar"/> only</param>
        /// <returns>true if read successful</returns>
        public bool ReadClass<T>(T refClass, IntPtr lpBaseAddress, bool jsonMemoryOnly = false) where T : class
        {
            if (!ReadBytes(lpBaseAddress, Marshal.SizeOf<T>(), out var buffer))
                return false;

            var outClass = buffer.ToClass<T>();

            foreach (var field in refClass.GetType().GetFields())
            {
                if (jsonMemoryOnly && !UnrealMemoryVar.HasAttribute(field))
                    continue;

                field.SetValue(refClass, field.GetValue(outClass));
            }

            return true;
        }
        public T ReadClass<T>(IntPtr lpBaseAddress, bool jsonMemoryOnly = false) where T : class, new()
        {
            var ret = new T();
            return !ReadClass(ret, lpBaseAddress, jsonMemoryOnly) ? null : ret;
        }

        /// <summary>
        /// Read memory as <see cref="JsonStruct"/> and cast to class
        /// </summary>
        /// <typeparam name="T">Class type most of time will be inheritance form <see cref="IEngineStruct"/></typeparam>
        /// <param name="refClass">Class to fill fields on</param>
        /// <param name="lpBaseAddress">Address of data</param>
        /// <param name="jsonStruct">JsonStruct that will cast call to</param>
        /// <returns></returns>
        public bool ReadJsonClass<T>(T refClass, IntPtr lpBaseAddress, JsonStruct jsonStruct) where T : class
        {
            if (!ReadBytes(lpBaseAddress, Marshal.SizeOf<T>(), out var buffer))
                return false;

            // Must Have UnrealMemoryVar Attribute AND Field With Same Name With JsonVar Name
            foreach (var field in refClass.GetType().GetFields().Where(UnrealMemoryVar.HasAttribute))
            {
                // Get JsonVar
                var jVar = jsonStruct[field.Name];

                // Convert bytes to field type
                var jVarBytes = new byte[jVar.Size];
                Array.Copy(buffer, jVar.Offset, jVarBytes, 0, jVar.Size);

                // Set JsonVar on Class Field
                field.SetValue(refClass, jVarBytes.ToStructure(field.FieldType));
            }

            return true;
        }
        public T RpmClassPointer<T>(IntPtr lpBaseAddress) where T : class, new()
        {
            var ret = new T();
            ReadClass(ret, ReadAddress(lpBaseAddress));

            return ret;
        }

        public T ReadJsonClass<T>(IntPtr lpBaseAddress, JsonStruct jsonStruct) where T : class, new()
        {
            var ret = new T();
            return !ReadJsonClass(ret, lpBaseAddress, jsonStruct) ? null : ret;
        }

        public bool Write<T>(IntPtr lpBaseAddress, T value) where T : struct
        {
            byte[] bytes = BitConverter.GetBytes((dynamic)value);
            return Win32.WriteProcessMemory(TargetProcess.Handle, lpBaseAddress, bytes, Marshal.SizeOf<T>(), out _);
        }
        public bool WritePointer<T>(IntPtr lpBaseAddress, T value) where T : struct
        {
            return Write(ReadAddress(lpBaseAddress), value);
        }
        
        #endregion

        public bool IsStaticAddress(IntPtr address)
        {
            /*
             * Thanks To Roman_Ablo @ GuidedHacking
             * https://guidedhacking.com/threads/hyperscan-fast-vast-memory-scanner.9659/
             * i converted to C#
             */

            if (!IsValidProcess())
                throw new Exception("Process not found !!");
            if (address == IntPtr.Zero)
                return false;

            var ntQueryVirtualMemory = Utils.GetProcAddress<Win32.NtQueryVirtualMemory>("ntdll.dll", "NtQueryVirtualMemory");
            ulong length = 0;
            var sectionInformation = new HeapHelper.StructAllocer<Win32.SectionInfo>();
            int retStatus = ntQueryVirtualMemory(ProcessHandle, 
                address, 
                Win32.MemoryInformationClass.MemoryMappedFilenameInformation,
                sectionInformation,
                (ulong)Marshal.SizeOf<Win32.SectionInfo>(),
                ref length);

            if (!Win32.NtSuccess(retStatus))
                return false;

            sectionInformation.Update();
            string deviceName = sectionInformation.ManageStruct.SzData;
            string filePath = deviceName;
            for (int i = 0; i < 3; i++)
                filePath = filePath.Substring(filePath.IndexOf('\\') + 1);

            var driveLetters = DriveInfo.GetDrives().Select(d => d.Name.Replace("\\", "")).ToList();
            foreach (var driveLetter in driveLetters)
            {
                var sb = new StringBuilder(64);
                Win32.QueryDosDevice(driveLetter, sb, 64 * 2); // * 2 Unicode

                if (deviceName.Contains(sb.ToString()))
                    return true;
            }

            return false;
        }
    }
}
