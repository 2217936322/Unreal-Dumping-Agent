using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Json;
using Utils = Unreal_Dumping_Agent.UtilsHelper.Utils;

namespace Unreal_Dumping_Agent.Tools.SdkGen
{
    /// <summary>
    /// Attribute to make read <see cref="JsonStruct"/> easy. (It's for fields only)
    /// <para>field name must equal <see cref="JsonVar"/> on <see cref="JsonStruct"/></para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class UnrealMemoryVar : System.Attribute
    {
        public static bool HasAttribute<T>() => GetCustomAttributes(typeof(T)).Any(a => a is UnrealMemoryVar);
        public static bool HasAttribute(FieldInfo fi) => fi.GetCustomAttributes().Any(a => a is UnrealMemoryVar);
    }
    public interface IEngineStruct
    {
        /// <summary>
        /// Check if object was init, (aka data was read).
        /// </summary>
        bool Init { get; }

        /// <summary>
        ///  Address of object on Remote process
        /// </summary>
        IntPtr ObjAddress { get; }

        /// <summary>
        /// Get object type name
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Get JsonStruct of this class
        /// </summary>
        JsonStruct JsonType { get; }


        /// <summary>
        /// Fix pointers for 32bit games on 64bit tool
        /// </summary>
        Task FixPointers();

        /// <summary>
        /// Read Object data from remote process
        /// </summary>
        /// <param name="address">Address of target on remote process</param>
        /// <returns>if success will return true</returns>
        Task<bool> ReadData(IntPtr address);

        /// <summary>
        /// Read Object data from remote process
        /// <para>Using <see cref="ObjAddress"/> as data address</para>
        /// </summary>
        /// <returns>if success will return true</returns>
        Task<bool> ReadData();
    }

    public static class EngineClasses
    {
        #region BasicStructs
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FPointer
        {
            public IntPtr Dummy;

            public void FixPointers() => Utils.FixPointers(this);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class FQWord
        {
            public int A;
            public int B;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FName
        {
            public int ComparisonIndex;
            public int Number;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class TArray
        {
            public IntPtr Data;
            public int Count;
            public int Max;

            public bool IsValidIndex(int index) => index < Count;
            public void FixPointers() => Utils.FixPointers(this);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FString : TArray
        {
            public override string ToString()
            {
                return "";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FText
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x18)]
            public byte[] UnknownData;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FWeakObjectPtr
        {
            public int ObjectIndex;
            public int ObjectSerialNumber;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FStringAssetReference
        {
            public FString AssetLongPathname;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FGuid
        {
            public int A;
            public int B;
            public int C;
            public int D;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FUniqueObjectGuid
        {
            public FGuid Guid;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FScriptDelegate
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
            public byte[] UnknownData;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FScriptMulticastDelegate
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
            public byte[] UnknownData;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class FUEnumItem
        {
            public FName Key;
            public ulong Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class TPersistentObjectPtr<TObjectId>
        {
            public FWeakObjectPtr WeakPtr;
            public int TagAtLastTest;
            public TObjectId ObjectId;
        }
        #endregion

        #region FStructs
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FScriptInterface : IEngineStruct
        {
            public bool Init { get; private set; }
            public IntPtr ObjAddress { get; private set; }

            [UnrealMemoryVar]
            public IntPtr ObjectPointer;
            [UnrealMemoryVar]
            public IntPtr InterfacePointer;

            public string TypeName => GetType().Name;
            public JsonStruct JsonType => JsonReflector.GetStruct(TypeName);
            public Task FixPointers() => Task.Run(() => Utils.FixPointers(this));

            public IntPtr GetObj()
            {
                return ObjectPointer;
            }
            public IntPtr GetInterface()
            {
                return ObjectPointer != IntPtr.Zero ? InterfacePointer : IntPtr.Zero;
            }

            public async Task<bool> ReadData(IntPtr address)
            {
                if (address == IntPtr.Zero)
                    throw new ArgumentNullException($"`address` can't equal null !!");

                // Set object address
                ObjAddress = address;

                // Read Struct
                Utils.MemObj.ReadJsonClass(this, ObjAddress, JsonType);

		        // It's Initialized
                Init = true;

                // Fix pointers for x32 games
                await FixPointers();
                return true;
            }
            public async Task<bool> ReadData() => await ReadData(ObjAddress);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FAssetPtr : TPersistentObjectPtr<FStringAssetReference>
        {
            
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class FLazyObjectPtr : TPersistentObjectPtr<FStringAssetReference>
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class FUObjectItem : IEngineStruct
        {
            public bool Init { get; private set; }
            public IntPtr ObjAddress { get; private set; }

            [UnrealMemoryVar]
            public IntPtr Object;

            public string TypeName => GetType().Name;
            public JsonStruct JsonType => JsonReflector.GetStruct(TypeName);
            public Task FixPointers() => Task.Run(() => Utils.FixPointers(this));

            public async Task<bool> ReadData(IntPtr address)
            {
                if (address == IntPtr.Zero)
                    throw new ArgumentNullException($"`address` can't equal null !!");

                // Set object address
                ObjAddress = address;

                // Read Struct
                Utils.MemObj.ReadJsonClass(this, ObjAddress, JsonType);

                // It's Initialized
                Init = true;

                // Fix pointers for x32 games
                await FixPointers();
                return true;
            }
            public async Task<bool> ReadData() => await ReadData(ObjAddress);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class FNameEntity : IEngineStruct
        {
            public bool Init { get; private set; }
            public IntPtr ObjAddress { get; private set; }

            [UnrealMemoryVar]
            public int Index;
            [UnrealMemoryVar]
            public string AnsiName;

            public string TypeName => GetType().Name;
            public JsonStruct JsonType => JsonReflector.GetStruct(TypeName);
            public Task FixPointers() => Task.Run(() => Utils.FixPointers(this));

            public async Task<bool> ReadData(IntPtr address)
            {
                if (address == IntPtr.Zero)
                    throw new ArgumentNullException($"`address` can't equal null !!");

                // Set object address
                ObjAddress = address;

                // Read Struct
                Utils.MemObj.ReadJsonClass(this, ObjAddress, JsonType);
                AnsiName = Utils.MemObj.ReadString(address + JsonType["AnsiName"].Offset);

                // It's Initialized
                Init = true;

                // Fix pointers for x32 games
                await FixPointers();
                return true;
            }
            public async Task<bool> ReadData() => await ReadData(ObjAddress);
        }
        #endregion

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UObject : IEngineStruct
        {
            public bool Init { get; private set; }
            public IntPtr ObjAddress { get; private set; }

            [UnrealMemoryVar]
            public IntPtr VfTable;
            [UnrealMemoryVar]
            public int Flags;
            [UnrealMemoryVar]
            public int InternalIndex;
            [UnrealMemoryVar]
            public IntPtr Class;
            [UnrealMemoryVar]
            public FName Name;
            [UnrealMemoryVar]
            public IntPtr Outer;

            public string TypeName => GetType().Name;
            public JsonStruct JsonType => JsonReflector.GetStruct(TypeName);
            public Task FixPointers() => Task.Run(() => Utils.FixPointers(this));

            public async Task<bool> ReadData(IntPtr address)
            {
                if (address == IntPtr.Zero)
                    throw new ArgumentNullException($"`address` can't equal null !!");

                // Set object address
                ObjAddress = address;

                // Read Struct
                Utils.MemObj.ReadJsonClass(this, ObjAddress, JsonType);

                // It's Initialized
                Init = true;

                // Fix pointers for x32 games
                await FixPointers();
                return true;
            }
            public async Task<bool> ReadData() => await ReadData(ObjAddress);
        }

    }
}
