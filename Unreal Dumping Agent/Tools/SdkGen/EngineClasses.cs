using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Json;
using Utils = Unreal_Dumping_Agent.UtilsHelper.Utils;

namespace Unreal_Dumping_Agent.Tools.SdkGen
{
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
            public int Index;
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

            public int StructSize() => JsonType.GetSize();

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
            public int StructSize() => JsonType.GetSize();
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
        [DebuggerDisplay("String = {AnsiName}, Index = {Index}")]
        // ReSharper disable once InconsistentNaming
        public class FNameEntity : IEngineStruct
        {
            public bool Init { get; private set; }
            public IntPtr ObjAddress { get; private set; }
            private int AnsiNameOffset { get; set; } = -1;

            [UnrealMemoryVar]
            public int Index;
            // [UnrealMemoryVar] Not needed Read
            public string AnsiName;

            public string TypeName => GetType().Name;
            public JsonStruct JsonType => JsonReflector.GetStruct(TypeName);
            public int StructSize() => JsonType.GetSize();
            public Task FixPointers() => Task.Run(() => Task.Delay(0));

            public async Task<bool> ReadData(IntPtr address)
            {
                if (address == IntPtr.Zero)
                    throw new ArgumentNullException($"`address` can't equal null !!");
                if (AnsiNameOffset == -1)
                    throw new NotSupportedException($"Call ReadData(IntPtr, int).");

                // Set object address
                ObjAddress = address;

                // Read Struct
                Utils.MemObj.ReadJsonClass(this, ObjAddress, JsonType);
                AnsiName = Utils.MemObj.ReadString(address + AnsiNameOffset);

                // It's Initialized
                Init = true;

                // Fix pointers for x32 games
                await FixPointers();
                return true;
            }
            public async Task<bool> ReadData() => await ReadData(ObjAddress);
            public async Task<bool> ReadData(IntPtr address, int ansiNameOffset)
            {
                ObjAddress = address;
                AnsiNameOffset = ansiNameOffset;
                return await ReadData();
            }
        }
        #endregion

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UObject : IEngineStruct
        {
            public bool Init { get; protected set; }
            public IntPtr ObjAddress { get; protected set; }

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

            public virtual string TypeName => GetType().Name;
            public int StructSize() => JsonType.GetSize();
            public JsonStruct JsonType => JsonReflector.GetStruct(TypeName);

            public Task FixPointers() => Task.Run(() => Utils.FixPointers(this));

            public virtual async Task<bool> ReadData(IntPtr address)
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
            public virtual async Task<bool> ReadData() => await ReadData(ObjAddress);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UField : UObject
        {
            [UnrealMemoryVar]
            public IntPtr Next;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UEnum : UField
        {
            [UnrealMemoryVar]
            public FString CppType;
            [UnrealMemoryVar]
            public TArray Names;
            [UnrealMemoryVar]
            public long CppForm;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UStruct : UField
        {
            [UnrealMemoryVar]
            public IntPtr SuperField;
            [UnrealMemoryVar]
            public IntPtr Children;
            [UnrealMemoryVar]
            public int PropertySize;
            [UnrealMemoryVar]
            public int MinAlignment;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UScriptStruct : UStruct
        {
            
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UFunction : UStruct
        {
            [UnrealMemoryVar]
            public int FunctionFlags;
            [UnrealMemoryVar]
            public IntPtr FirstPropertyToInit;
            [UnrealMemoryVar]
            public IntPtr EventGraphFunction;
            [UnrealMemoryVar]
            public IntPtr Func;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UClass : UStruct
        {
            
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UProperty : UField
        {
            [UnrealMemoryVar]
            public int ArrayDim;
            [UnrealMemoryVar]
            public int ElementSize;
            [UnrealMemoryVar]
            public FQWord PropertyFlags;
            [UnrealMemoryVar]
            public int Offset;
            [UnrealMemoryVar]
            public IntPtr PropertyLinkNext;
            [UnrealMemoryVar]
            public IntPtr NextRef;
            [UnrealMemoryVar]
            public IntPtr DestructorLinkNext;
            [UnrealMemoryVar]
            public IntPtr PostConstructLinkNext;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UNumericProperty : UProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UByteProperty : UProperty
        {
            [UnrealMemoryVar]
            public IntPtr Enum;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UUInt16Property : UNumericProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UUInt32Property : UNumericProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UUInt64Property : UNumericProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UInt8Property : UNumericProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UInt16Property : UNumericProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UIntProperty : UNumericProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UInt64Property : UNumericProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UFloatProperty : UNumericProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UDoubleProperty : UNumericProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UBoolProperty : UProperty
        {
            [UnrealMemoryVar]
            public byte FieldSize;
            [UnrealMemoryVar]
            public byte ByteOffset;
            [UnrealMemoryVar]
            public byte ByteMask;
            [UnrealMemoryVar]
            public byte FieldMask;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UObjectPropertyBase : UProperty
        {
            [UnrealMemoryVar]
            public IntPtr PropertyClass;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UObjectProperty : UObjectPropertyBase
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UClassProperty : UObjectProperty
        {
            [UnrealMemoryVar]
            public IntPtr MetaClass;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UInterfaceProperty : UProperty
        {
            [UnrealMemoryVar]
            public IntPtr InterfaceClass;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UWeakObjectProperty : UObjectPropertyBase
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class ULazyObjectProperty : UObjectPropertyBase
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UAssetObjectProperty : UObjectPropertyBase
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UAssetClassProperty : UAssetObjectProperty
        {
            [UnrealMemoryVar]
            public IntPtr MetaClass;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UNameProperty : UProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UStructProperty : UProperty
        {
            [UnrealMemoryVar]
            public IntPtr Struct;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UStrProperty : UProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UTextProperty : UProperty
        {

        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UArrayProperty : UProperty
        {
            [UnrealMemoryVar]
            public IntPtr Inner;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UMapProperty : UProperty
        {
            [UnrealMemoryVar]
            public IntPtr KeyProp;
            [UnrealMemoryVar]
            public IntPtr ValueProp;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UDelegateProperty : UProperty
        {
            [UnrealMemoryVar]
            public IntPtr SignatureFunction;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UMulticastDelegateProperty : UProperty
        {
            [UnrealMemoryVar]
            public IntPtr SignatureFunction;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        // ReSharper disable once InconsistentNaming
        public class UEnumProperty : UProperty
        {
            [UnrealMemoryVar]
            public IntPtr UnderlyingProp;
            [UnrealMemoryVar]
            public IntPtr Enum;
        }
    }
}
