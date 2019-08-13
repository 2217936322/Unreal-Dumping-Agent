using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Unreal_Dumping_Agent.Json;
using Unreal_Dumping_Agent.Memory;
using Unreal_Dumping_Agent.Tools.SdkGen;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    public static class Utils
    {
        private static readonly Random _random = new Random();

        public const string Version = "1.0.0";
        public const string Title = "Welcome Agent";
        public const string UnrealWindowClass = "UnrealWindow";

        #region Extinction
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        public static bool HasFlag<T>(this int intVal, T flag) where T : Enum
        {
            T intToEnum = (T) Enum.ToObject(typeof(T), intVal);
            return intToEnum.HasFlag(flag);
        }
        public static bool IsNull(this IntPtr intPtr) => intPtr == IntPtr.Zero;
        public static bool IsValid(this IntPtr intPtr) => intPtr != IntPtr.Zero && intPtr.ToInt64() != -1;
        public static bool Empty(this string str) => string.IsNullOrEmpty(str);
        public static bool Empty<T>(this List<T> list) => list.Count == 0;
        #endregion

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

        public static byte[] StructToBytes<T>(T structBase) where T : class
        {
            int size = Marshal.SizeOf(structBase);
            var arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structBase, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
        public static T BytesToStruct<T>(byte[] structBytes) where T : class, new()
        {
            var ret = new T();
            int size = Marshal.SizeOf(ret);
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.Copy(structBytes, 0, ptr, size);

            ret = Marshal.PtrToStructure<T>(ptr);
            Marshal.FreeHGlobal(ptr);

            return ret;
        }
        #endregion

        #region Variables
        public static BotType BotWorkType;
        public static Memory.Memory MemObj;
        public static Scanner ScanObj;
        public static object MainLocker = new object();
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
        public static int GamePointerSize()
        {
            return MemObj.Is64Bit ? 0x8 : 0x4;
        }
        public static IntPtr GameStartAddress()
        {
            // Get System Info
            Win32.GetSystemInfo(out var si);

            return si.MinimumApplicationAddress;
        }
        public static IntPtr GameEndAddress()
        {
            // Get System Info
            Win32.GetSystemInfo(out var si);

            return si.MaximumApplicationAddress;
        }
        #endregion

        #region Misc
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
        public static bool IsNumber(string str, out int val, out bool isHex)
        {
            val = -1;
            isHex = str.StartsWith("0x");

            if (isHex)
                isHex = int.TryParse(str.Remove(0, 2), NumberStyles.HexNumber, new NumberFormatInfo(), out val);

            return isHex || int.TryParse(str, out val);
        }
        public static bool IsNumber(string str, out int val)
        {
            return IsNumber(str, out val, out _);
        }
        public static bool IsNumber(string str)
        {
            return IsNumber(str, out _, out _);
        }
        public static int GetBitPosition(byte value)
        {
            int i4 = ((value & 0xf) == 0 ? 1 : 0) << 2;
            value >>= i4;

            int i2 = ((value & 0x3) == 0 ? 1 : 0) << 1;
            value >>= i2;

            int i1 = (value & 0x1) == 0 ? 1 : 0;

            int i0 = ((value >> i1) & 1) == 1 ? 0 : -8;

            return i4 + i2 + i1 + i0;
        }
        #endregion

        #region FixPointers
        public static void FixPointers<T>(T structBase) where T : class, new()
        {
            if (ProgramIs64() && MemObj.Is64Bit)
                return;

            var structBytes = StructToBytes(structBase);
            var varsOffset = structBase.GetType().GetFields()
                .Where(field => field.FieldType == typeof(IntPtr) && JsonMemoryVar.HasAttribute(field))
                .Select(field => Marshal.OffsetOf<T>(field.Name).ToInt32());

            foreach (var i in varsOffset)
                FixStructPointer(ref structBytes, i);

            var newStruct = BytesToStruct<T>(structBytes);

            // Set Fields
            foreach (var field in structBase.GetType().GetFields().Where(JsonMemoryVar.HasAttribute))
                field.SetValue(structBase, field.GetValue(newStruct));
        }
        private static void FixStructPointer(ref byte[] structBase, int varOffset)
        {
            if (ProgramIs64() && MemObj.Is64Bit)
                throw new Exception("FixStructPointer only work for 32bit games with 64bit tool version.");

            int structSize = structBase.Length;
            int srcSize = Math.Abs(varOffset - structSize) - 0x4;

            int src = varOffset;
            int dest = src + 0x4;

            Array.Copy(structBase, src, structBase, dest, srcSize);
            structBase[dest + 0] = 0;
            structBase[dest + 1] = 0;
            structBase[dest + 2] = 0;
            structBase[dest + 3] = 0;
        }
        #endregion

        #region Address Stuff
        public static bool IsValidRemoteAddress(IntPtr address)
        {
            if (MemObj == null || address == IntPtr.Zero || address.ToInt64() < 0)
                return false;

            if (Win32.VirtualQueryEx(MemObj.ProcessHandle, address, out var info, (uint)Marshal.SizeOf<Win32.MemoryBasicInformation>()) != 0)
            {
		        // Bad Memory
                return /*(info.State & (int)Win32.MemoryState.MemCommit) != 0 && */ !info.Protect.HasFlag(Win32.MemoryProtection.PageNoAccess);
            }

            return false;
        }
        public static bool IsValidRemotePointer(IntPtr pointer, out IntPtr address)
        {
            address = IntPtr.Zero;
            if (MemObj == null || pointer == IntPtr.Zero || pointer.ToInt64() < 0)
                return false;

            address = MemObj.ReadAddress(pointer);
            return IsValidRemoteAddress(address);
        }
        private static bool IsValidGNamesAddress(IntPtr address, bool chunkCheck)
        {
            if (MemObj == null || !IsValidRemoteAddress(address))
                return false;

            if (!chunkCheck && !IsValidRemotePointer(address, out _))
                return false;

            if (!chunkCheck)
                address = MemObj.ReadAddress(address);

            int nullCount = 0;

            // Chunks array must have null pointers, if not then it's not valid
            for (int i = 0; i < 50 && nullCount <= 3; i++)
            {
                // Read Chunk Address
                var offset = i * GamePointerSize();
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
        public static bool IsValidGNamesAddress(IntPtr staticAddress)
        {
            return IsValidGNamesAddress(staticAddress, false);
        }
        public static bool IsValidGNamesChunksAddress(IntPtr chunkAddress)
        {
            return IsValidGNamesAddress(chunkAddress, true);
        }
        public static int CalcNameOffset(IntPtr address, bool isNoneAddress = true)
        {
            if (isNoneAddress)
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

            var noneSig = PatternScanner.Parse("None", 0, "4E 6F 6E 65 00");
            var sigResult = PatternScanner.FindPattern(MemObj, address, address + 0x18, new List<PatternScanner.Pattern> { noneSig }, true).Result;
            if (sigResult.ContainsKey("None"))
                return (int)(sigResult["None"][0].ToInt64() - address.ToInt64());

            return -1;
        }
        public static bool IsTArray(IntPtr address)
        {
            // Check PreAllocatedObjects it's always null, it's only on new TUObjectArray then it's good to check
            return !MemObj.ReadAddress(address + GamePointerSize()).IsNull();
        }
        public static bool IsTUobjectArray(IntPtr address)
        {
            // if game have chunks, then it's not TArray
            IntPtr gObjectArray = MemObj.ReadAddress(MemObj.ReadAddress(address));
            if (!IsTArray(address) && IsValidGObjectsAddress(gObjectArray))
                return true;

            // if game don't use chunks, then it's must be TArray
            gObjectArray = MemObj.ReadAddress(address);
            return IsTArray(address) && IsValidGObjectsAddress(gObjectArray);
        }
        public static bool IsValidGObjectsAddress(IntPtr chunksAddress)
        {
            if (JsonReflector.StructsList.Count == 0)
                throw new NullReferenceException("You must init `JsonReflector` first.");

            // => Get information
            var objectItem = JsonReflector.GetStruct("FUObjectItem");
            var objectItemSize = objectItem.GetSize();

            var objectInfo = JsonReflector.GetStruct("UObject");
            var objOuter = objectInfo["Outer"].Offset;
            var objInternalIndex = objectInfo["InternalIndex"].Offset;
            var objNameIndex = objectInfo["Name"].Offset;
            // => Get information

	        IntPtr addressHolder = chunksAddress;
            if (MemObj == null)
                throw new NullReferenceException("`MemObj` is null !!");

            if (!IsValidRemoteAddress(addressHolder))
                return false;

            /*
	        * NOTE:
	        * Nested loops will be slow, split-ed best.
	        */
            const int objCount = 2;
            var objects = new IntPtr[objCount];
            var vTables = new IntPtr[objCount];

            // Check (UObject*) Is Valid Pointer
            for (int i = 0; i < objCount; i++)
            {
                int offset = objectItemSize * i;
                if (!IsValidRemotePointer(addressHolder + offset, out objects[i]))
                    return false;
            }

	        // Check (VTable) Is Valid Pointer
            for (int i = 0; i < objCount; i++)
            {
                if (!IsValidRemotePointer(objects[i], out vTables[i]))
                    return false;
            }

            // Check (InternalIndex) Is Valid
            for (int i = 0; i < objCount; i++)
            {
                int internalIndex = MemObj.Read<int>(objects[i] + objInternalIndex);
                if (internalIndex != i)
                    return false;
            }

            // Check (Outer) Is Valid
            // first object must have Outer == nullptr(0x0000000000)
            int uOuter = MemObj.Read<int>(objects[0] + objOuter);
            if (uOuter != 0)
                return false;

            // Check (FName_index) Is Valid
            // 2nd object must have FName_index == 100
            int uFNameIndex = MemObj.Read<int>(objects[1] + objNameIndex);
            return uFNameIndex == 100;
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
            if (MemObj?.TargetProcess == null)
                throw new ArgumentException("init MemObj first.");
            if (MemObj.TargetProcess.MainModule == null)
                return false;

            var fvi = FileVersionInfo.GetVersionInfo(MemObj.TargetProcess.MainModule.FileName);
            version = string.Concat(fvi.ProductMajorPart.ToString(), ".", fvi.ProductMinorPart.ToString(), ".", fvi.ProductBuildPart.ToString());
            return true;
        }
        #endregion
    }
}
