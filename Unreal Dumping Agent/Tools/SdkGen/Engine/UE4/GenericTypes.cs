using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.UtilsHelper;
using static Unreal_Dumping_Agent.Tools.SdkGen.EngineClasses;
using static Unreal_Dumping_Agent.Tools.SdkGen.Engine.NameValidator;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4
{
    public class GenericTypes : IEngineVersion
    {
        [DebuggerDisplay("Address = {Object.ObjAddress.ToInt64().ToString(\"X8\")}, TypeID = {TypeId}, Name = {ObjName}")]
        // ReSharper disable once InconsistentNaming
        public class UEObject : IUnrealStruct
        {
            protected bool Equals(UEObject other)
            {
                return Equals(Object, other.Object);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((UEObject) obj);
            }

            public override int GetHashCode()
            {
                return (Object != null ? Object.GetHashCode() : 0);
            }

            protected UClass ObjClass { get; set; } 
            protected UEObject Outer { get; set; }
            protected UEObject Package { get; set; }
            protected string ObjName { get; set; }
            protected string FullName { get; set; }
            protected string NameCpp { get; set; }

            public UObject Object { get; set; }

            public UEObject()
            {
                Object = new UObject();
            }
            public UEObject(UObject uObject) => Object = uObject;

            public int TypeId => NamesStore.GetByName(GetType().Name.Remove(0, 2));
            public UEClass StaticClass => ObjectsStore.FindClass($"Class CoreUObject.{GetType().Name.Remove(0, 2)}").Result;

            public IntPtr GetAddress() => Object.ObjAddress;
            public bool IsValid()
            {
                return Object.ObjAddress.IsValid() && Object.VfTable.IsValid() && (Object.Name.Index > 0 && Object.Name.Index <= NamesStore.GNames.Names.Count);
            }
            public int GetIndex()
            {
                return ObjectsStore.GetIndexByAddress(Object.ObjAddress);
            }
            public Task<string> GetName()
            {
                return Task.Run(() =>
                {
                    if (!ObjName.Empty())
                        return ObjName;

                    string name = NamesStore.GetByIndex(Object.Name.Index); // TODO: Check Here !!
                    if (!name.Empty() && (Object.Name.Number > 0 && Object.Name.Index != Object.Name.Number))
                        name += "_" + Object.Name.Number;

                    int pos = name.LastIndexOf('/');
                    if (pos == -1)
                    {
                        ObjName = name;
                        return ObjName;
                    }

                    ObjName = name.Substring(pos + 1);
                    return ObjName;
                });
            }
            public async Task<string> GetInstanceClassName()
            {
                if (!IsValid())
                    return string.Empty;

                UEObject obj = ObjectsStore.GetByAddress(GetAddress(), out bool found);

                return found ? await (await obj.GetClass()).GetNameCpp() : string.Empty;
            }
            public async Task<string> GetFullName()
            {
                if (!FullName.Empty())
                    return FullName;

                var cClass = await GetClass();
                if (!cClass.IsValid())
                    return "(null)";

                string temp = string.Empty;
                for (UEObject outer = await GetOuter(); outer.IsValid(); outer = await outer.GetOuter())
                    temp = temp.Insert(0, $"{await outer.GetName()}.");

                FullName = $"{await cClass.GetName()} {temp}{await GetName()}";

                return FullName;
            }
            public async Task<string> GetNameCpp()
            {
                if (!NameCpp.Empty())
                    return NameCpp;

                string name = string.Empty;
                if (IsA<UEClass>().Result)
                {
                    var c = this.Cast<UEClass>();
                    while (c.IsValid())
                    {
                        string className = await c.GetName();
                        if (className == "Actor")
                        {
                            name += "A";
                            break;
                        }
                        if (className == "Object")
                        {
                            name += "U";
                            break;
                        }

                        c = (await c.GetSuper()).Cast<UEClass>();
                    }
                }
                else
                {
		            name += "F";
                }

                name += GetName();
                NameCpp = name;

                return NameCpp;
            }

            public async Task<UEClass> GetClass()
            {
                // Must have a class, aka must not hit that condition
                if (!Object.Class.IsValid())
                    return new UEClass();

                return (await ObjectsStore.GetByAddress(Object.Class)).Cast<UEClass>();
            }
            public async Task<UEObject> GetOuter()
            {
                return !Object.Outer.IsValid() ? new UEObject() : await ObjectsStore.GetByAddress(Object.Outer);
            }
            public async Task<UEObject> GetPackageObject()
            {
                if (Package != null)
                    return Package;

                // Package Is The Last Outer
                for (UEObject outer = await GetOuter(); outer.IsValid(); outer = await outer.GetOuter())
                    Package = outer;

                return Package ?? (Package = await ObjectsStore.GetByAddress(Object.ObjAddress));
            }

            // ToDo: Add Cache here to be faster
            public async Task<bool> IsA<T>() where T : UEObject, new()
            {
                if (!IsValid())
                    return false;

                int cmpTypeId = new T().TypeId;
                for (UEClass super = await GetClass(); super.IsValid(); super = (await super.GetSuper()).Cast<UEClass>())
                {
                    if (super.Object.Name.Index == cmpTypeId)
                        return true;
                }
                return false;
            }
            public async Task<bool> IsA(string typeName)
            {
                if (!IsValid())
                    return false;

                for (UEClass super = await GetClass(); super.IsValid(); super = (await super.GetSuper()).Cast<UEClass>())
                {
                    if (await super.GetName() == typeName || await super.GetNameCpp() == typeName)
                        return true;
                }

                return false;
            }

            #region Casting
            public T Cast<T>() where T : UEObject, new()
            {
                var ret = new T
                {
                    FullName = FullName,
                    NameCpp = NameCpp,
                    ObjName = ObjName,
                    Object = Object,
                    ObjClass = ObjClass,
                    Outer = Outer,
                    Package = Package
                };

                return ret;
            }
            #endregion

            public static bool operator ==(UEObject lhs, UEObject rhs) => lhs?.GetAddress() == rhs?.GetAddress();
            public static bool operator !=(UEObject lhs, UEObject rhs) => lhs?.GetAddress() != rhs?.GetAddress();
        }

        // ReSharper disable once InconsistentNaming
        public class UEField : UEObject
        {
            protected UField ObjField { get; set; } = new UField();

            public async Task<UEField> GetNext()
            {
                if (!ObjField.Empty())
                    ObjField = await Object.Cast<UField>();

                if (!ObjField.Next.IsValid())
                    return new UEField();

                return (await ObjectsStore.GetByAddress(ObjField.Next)).Cast<UEField>();
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEEnum : UEField
        {
            protected UEnum ObjEnum = new UEnum();

            public async Task<List<string>> GetNames()
            {
                if (ObjEnum.Empty())
                    ObjEnum = await Object.Cast<UEnum>();

                // Get Names
                IntPtr dataAddress = ObjEnum.Names.Data;
                if (ObjEnum.Names.Count > 300)
                    throw new IndexOutOfRangeException("Enum have more than 300 value !!, Maybe EngineStructs Problem.!");

                var cls = Utils.MemObj.ReadClassArray<FUEnumItem>(dataAddress, ObjEnum.Names.Count);
                var buffer = cls
                    .Where(e => e.Key.Index < NamesStore.GNames.Names.Count || e.Key.Index != 0)
                    .Select(e => NamesStore.GetByIndex(e.Key.Index))
                    .ToList();

                return buffer;
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEConst : UEField
        {
            public string GetValue()
            {
                throw new Exception("UE4 didn't have `UEConst`");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEStruct : UEField
        {
            protected UStruct ObjStruct = new UStruct();

            public async Task<UEStruct> GetSuper()
            {
                if (ObjStruct.Empty())
                    ObjStruct = await Object.Cast<UStruct>();

                if (!ObjStruct.SuperField.IsValid())
                    return new UEStruct();

                return (await ObjectsStore.GetByAddress(ObjStruct.SuperField)).Cast<UEStruct>();
            }
            public async Task<UEField> GetChildren()
            {
                if (ObjStruct.Empty())
                    ObjStruct = await Object.Cast<UStruct>();

                if (!ObjStruct.Children.IsValid())
                    return new UEField();

                return (await ObjectsStore.GetByAddress(ObjStruct.Children)).Cast<UEField>();
            }
            public async Task<int> GetPropertySize()
            {
                if (ObjStruct.Empty())
                    ObjStruct = await Object.Cast<UStruct>();

                return ObjStruct.PropertySize;
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEScriptStruct : UEStruct
        {

        }

        // ReSharper disable once InconsistentNaming
        public class UEFunction : UEStruct
        {
            protected UFunction ObjFunction = new UFunction();

            public async Task<UEFunctionFlags> GetFunctionFlags()
            {
                if (ObjFunction.Empty())
                    ObjFunction = await Object.Cast<UFunction>();

                return (UEFunctionFlags)ObjFunction.FunctionFlags;
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEClass : UEStruct
        {
            
        }

        // ReSharper disable once InconsistentNaming
        public class UEProperty : UEField
        {
            public enum PropertyType
            {
                Unknown,
                Primitive,
                PredefinedStruct,
                CustomStruct,
                Container
            }
            public struct Info
            {
                public PropertyType Type;
                public int Size;
                public bool CanBeReference;
                public string CppType;

                public Info(PropertyType type, int size, bool reference, string cppType)
                {
                    Type = type;
                    Size = size;
                    CanBeReference = reference;
                    CppType = cppType;
                }
            }

            protected UProperty ObjProperty = new UProperty();
            protected bool InfoChanged;
            protected Info CurInfo;

            public async Task<int> GetArrayDim()
            {
                if (ObjProperty.Empty())
                    ObjProperty = await Object.Cast<UProperty>();

                return ObjProperty.ArrayDim;
            }
            public async Task<int> GetElementSize()
            {
                if (ObjProperty.Empty())
                    ObjProperty = await Object.Cast<UProperty>();

                return ObjProperty.ElementSize;
            }
            public async Task<UEPropertyFlags> GetPropertyFlags()
            {
                if (ObjProperty.Empty())
                    ObjProperty = await Object.Cast<UProperty>();

                return (UEPropertyFlags)ObjProperty.PropertyFlags.A;
            }
            public async Task<int> GetOffset()
            {
                if (ObjProperty.Empty())
                    ObjProperty = await Object.Cast<UProperty>();

                return ObjProperty.Offset;
            }
            public async Task<Info> GetInfo()
            {
                if (InfoChanged)
                    return CurInfo;

                if (!IsValid())
                    return new Info { Type = PropertyType.Unknown };

                if (await IsA<UEByteProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEByteProperty>().GetInfo();
                }
                else if (await IsA<UEUInt16Property>())
                {
                    InfoChanged = true;
                    CurInfo = UEUInt16Property.GetInfo();
                }
                else if (await IsA<UEUInt32Property>())
                {
                    InfoChanged = true;
                    CurInfo = UEUInt32Property.GetInfo();
                }
                else if (await IsA<UEUInt64Property>())
                {
                    InfoChanged = true;
                    CurInfo = UEUInt64Property.GetInfo();
                }
                else if (await IsA<UEInt8Property>())
                {
                    InfoChanged = true;
                    CurInfo = UEInt8Property.GetInfo();
                }
                else if (await IsA<UEInt16Property>())
                {
                    InfoChanged = true;
                    CurInfo = UEInt16Property.GetInfo();
                }
                else if (await IsA<UEIntProperty>())
                {
                    InfoChanged = true;
                    CurInfo = UEIntProperty.GetInfo();
                }
                else if (await IsA<UEInt64Property>())
                {
                    InfoChanged = true;
                    CurInfo = UEInt64Property.GetInfo();
                }
                else if (await IsA<UEFloatProperty>())
                {
                    InfoChanged = true;
                    CurInfo = UEFloatProperty.GetInfo();
                }
                else if (await IsA<UEDoubleProperty>())
                {
                    InfoChanged = true;
                    CurInfo = UEDoubleProperty.GetInfo();
                }
                else if (await IsA<UEBoolProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEBoolProperty>().GetInfo();
                }
                else if (await IsA<UEObjectProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEObjectProperty>().GetInfo();
                }
                else if (await IsA<UEClassProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEClassProperty>().GetInfo();
                }
                else if (await IsA<UEInterfaceProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEInterfaceProperty>().GetInfo();
                }
                else if (await IsA<UEWeakObjectProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEWeakObjectProperty>().GetInfo();
                }
                else if (await IsA<UELazyObjectProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UELazyObjectProperty>().GetInfo();
                }
                else if (await IsA<UEAssetObjectProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEAssetObjectProperty>().GetInfo();
                }
                else if (await IsA<UEAssetClassProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await UEAssetClassProperty.GetInfo();
                }
                else if (await IsA<UENameProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await UENameProperty.GetInfo();
                }
                else if (await IsA<UEStructProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEStructProperty>().GetInfo();
                }
                else if (await IsA<UEStrProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await UEStrProperty.GetInfo();
                }
                else if (await IsA<UETextProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await UETextProperty.GetInfo();
                }
                else if (await IsA<UEArrayProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEArrayProperty>().GetInfo();
                }
                else if (await IsA<UEMapProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEMapProperty>().GetInfo();
                }
                else if (await IsA<UEDelegateProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await UEDelegateProperty.GetInfo();
                }
                else if (await IsA<UEMulticastDelegateProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await UEMulticastDelegateProperty.GetInfo();
                }
                else if (await IsA<UEEnumProperty>())
                {
                    InfoChanged = true;
                    CurInfo = await this.Cast<UEEnumProperty>().GetInfo();
                }

                return CurInfo;
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UENumericProperty : UEProperty
        {

        }

        // ReSharper disable once InconsistentNaming
        public class UEByteProperty : UEProperty
        {
            protected UByteProperty ObjByteProperty = new UByteProperty();

            public async Task<bool> IsEnum()
            {
                return (await GetEnum()).IsValid();
            }
            public async Task<UEEnum> GetEnum()
            {
                if (ObjByteProperty.Empty())
                    ObjByteProperty = await Object.Cast<UByteProperty>();

                if (!ObjByteProperty.Enum.IsValid())
                    return new UEEnum();

                return (await ObjectsStore.GetByAddress(ObjByteProperty.Enum)).Cast<UEEnum>();
            }
            public new async Task<Info> GetInfo()
            {
                string typeStr = await IsEnum() ? $"TEnumAsByte < {MakeUniqueCppName(await GetEnum())} > " : "unsigned char";
                return new Info(PropertyType.Primitive, sizeof(byte), false, typeStr);
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEUInt16Property : UENumericProperty
        {
            public new static Info GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(ushort), false, "uint16_t");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEUInt32Property : UENumericProperty
        {
            public new static Info GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(uint), false, "uint32_t");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEUInt64Property : UENumericProperty
        {
            public new static Info GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(ulong), false, "uint64_t");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEInt8Property : UENumericProperty
        {
            public new static Info GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(sbyte), false, "int8_t");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEInt16Property : UENumericProperty
        {
            public new static Info GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(short), false, "int16_t");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEIntProperty : UENumericProperty
        {
            public new static Info GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(int), false, "int");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEInt64Property : UENumericProperty
        {
            public new static Info GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(long), false, "int64_t");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEFloatProperty : UENumericProperty
        {
            public new static Info GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(float), false, "float");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEDoubleProperty : UENumericProperty
        {
            public new static Info GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(double), false, "double");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEBoolProperty : UEProperty
        {
            protected UBoolProperty ObjBoolProperty = new UBoolProperty();

            public async Task<bool> IsNativeBool()
            {
                return await GetFieldMask() == 0xFF;
            }
            public async Task<bool> IsBitfield()
            {
                return !await IsNativeBool();
            }

            public async Task<byte> GetFieldSize()
            {
                if (ObjBoolProperty.Empty())
                    ObjBoolProperty = await Object.Cast<UBoolProperty>();

                return ObjBoolProperty.FieldSize;
            }
            public async Task<byte> GetByteOffset()
            {
                if (ObjBoolProperty.Empty())
                    ObjBoolProperty = await Object.Cast<UBoolProperty>();

                return ObjBoolProperty.ByteOffset;
            }
            public async Task<byte> GetByteMask()
            {
                if (ObjBoolProperty.Empty())
                    ObjBoolProperty = await Object.Cast<UBoolProperty>();

                return ObjBoolProperty.ByteMask;
            }
            public async Task<byte> GetFieldMask()
            {
                if (ObjBoolProperty.Empty())
                    ObjBoolProperty = await Object.Cast<UBoolProperty>();

                return ObjBoolProperty.FieldMask;
            }

            public async Task<List<int>> GetMissingBitsCount(UEBoolProperty other)
            {
                var byteMask = GetByteMask();
                var offset = GetOffset();

                var otherByteMask = other.GetByteMask();
                var otherOffset = other.GetOffset();

                // If there is no previous bitfield member, just calculate the missing bits.
                if (!other.IsValid())
                    return new List<int> { Utils.GetBitPosition(await byteMask), -1 };

                // If both bitfield member belong to the same byte, calculate the bit position difference.
                if (await offset == await otherOffset)
                    return new List<int> { Utils.GetBitPosition(await byteMask) - Utils.GetBitPosition(await otherByteMask) - 1, -1 };

                // If they have different offsets, we need two distances
                // |00001000|00100000|
                // 1.   ^---^
                // 2.       ^--^

                return new List<int> { /* Number of bits on byte is => */ 8 - Utils.GetBitPosition(await otherByteMask) - 1, Utils.GetBitPosition(await byteMask) };
            }
            public new async Task<Info> GetInfo()
            {
                return await IsNativeBool()
                    ? new Info(PropertyType.Primitive, sizeof(bool), false, "bool")
                    : new Info(PropertyType.Primitive, sizeof(char), false, "unsigned char");
            }

            public static bool operator <(UEBoolProperty lhs, UEBoolProperty rhs)
            {
                if (lhs.GetByteOffset().Result == rhs.GetByteOffset().Result)
                    return lhs.GetByteMask().Result < rhs.GetByteMask().Result;

                return lhs.GetByteOffset().Result < rhs.GetByteOffset().Result;
            }
            public static bool operator >(UEBoolProperty lhs, UEBoolProperty rhs)
            {
                if (lhs.GetByteOffset().Result == rhs.GetByteOffset().Result)
                    return lhs.GetByteMask().Result > rhs.GetByteMask().Result;

                return lhs.GetByteOffset().Result > rhs.GetByteOffset().Result;
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEObjectPropertyBase : UEProperty
        {
	        protected UObjectPropertyBase ObjObjectPropertyBase = new UObjectPropertyBase();

            public async Task<UEClass> GetPropertyClass()
            {
                if (ObjObjectPropertyBase.Empty())
                    ObjObjectPropertyBase = await Object.Cast<UObjectPropertyBase>();

                if (!ObjObjectPropertyBase.PropertyClass.IsValid())
                    return new UEClass();

                return (await ObjectsStore.GetByAddress(ObjObjectPropertyBase.PropertyClass)).Cast<UEClass>();
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEObjectProperty : UEObjectPropertyBase
        {
            public new async Task<Info> GetInfo()
            {
                var pClass = await GetPropertyClass();
                if (!pClass.IsValid())
                    return new Info { Type = PropertyType.Unknown };

                return new Info(PropertyType.Primitive, Utils.GamePointerSize(), false, $"class {MakeValidName(await pClass.GetNameCpp())}*");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEClassProperty : UEObjectProperty
        {
            protected UClassProperty ObjClassProperty = new UClassProperty();

            public async Task<UEClass> GetMetaClass()
            {
                if (ObjClassProperty.Empty())
                    ObjClassProperty = await Object.Cast<UClassProperty>();

                if (!ObjClassProperty.MetaClass.IsValid())
                    return new UEClass();

                return (await ObjectsStore.GetByAddress(ObjClassProperty.MetaClass)).Cast<UEClass>();
            }
            public new async Task<Info> GetInfo()
            {
                return new Info(PropertyType.Primitive, Utils.GamePointerSize(), false, $"class {MakeValidName((await GetMetaClass()).GetNameCpp().Result)}*");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEInterfaceProperty : UEProperty
        {
            protected UInterfaceProperty ObjInterfaceProperty = new UInterfaceProperty();

            public async Task<UEClass> GetInterfaceClass()
            {
                if (ObjInterfaceProperty.Empty())
                    ObjInterfaceProperty = await Object.Cast<UInterfaceProperty>();

                if (!ObjInterfaceProperty.InterfaceClass.IsValid())
                    return new UEClass();

                return (await ObjectsStore.GetByAddress(ObjInterfaceProperty.InterfaceClass)).Cast<UEClass>();
            }
            public new async Task<Info> GetInfo()
            {
                return new Info(PropertyType.PredefinedStruct, new FScriptInterface().StructSize(), true, $"TScriptInterface<class {MakeValidName((await GetInterfaceClass()).GetNameCpp().Result)}>");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEWeakObjectProperty : UEObjectPropertyBase
        {
            public new async Task<Info> GetInfo()
            {
                return new Info(PropertyType.Container, Marshal.SizeOf<FWeakObjectPtr>(), false, $"TWeakObjectPtr<class {MakeValidName((await GetPropertyClass()).GetNameCpp().Result)}>");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UELazyObjectProperty : UEObjectPropertyBase
        {
            public new async Task<Info> GetInfo()
            {
                return new Info(PropertyType.Container, Marshal.SizeOf<FLazyObjectPtr>(), false, $"TLazyObjectPtr<class {MakeValidName((await GetPropertyClass()).GetNameCpp().Result)}>");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEAssetObjectProperty : UEObjectPropertyBase
        {
            public new async Task<Info> GetInfo()
            {
                return new Info(PropertyType.Container, Marshal.SizeOf<FAssetPtr>(), false, $"TAssetPtr<class {MakeValidName((await GetPropertyClass()).GetNameCpp().Result)}>");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEAssetClassProperty : UEAssetObjectProperty
        {
            protected UAssetClassProperty ObjAssetClassProperty = new UAssetClassProperty();

            public async Task<UEClass> GetMetaClass()
            {
                if (ObjAssetClassProperty.Empty())
                    ObjAssetClassProperty = await Object.Cast<UAssetClassProperty>();

                if (!ObjAssetClassProperty.MetaClass.IsValid())
                    return new UEClass();

                return (await ObjectsStore.GetByAddress(ObjAssetClassProperty.MetaClass)).Cast<UEClass>();
            }
            public new static Task<Info> GetInfo()
            {
                return Task.FromResult(new Info(PropertyType.Primitive, sizeof(byte), false, ""));
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UENameProperty : UEProperty
        {
            public new static Task<Info> GetInfo()
            {
                return Task.FromResult(new Info(PropertyType.PredefinedStruct, Marshal.SizeOf<FName>(), true, "struct FName"));
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEStructProperty : UEProperty
        {
            protected UStructProperty ObjStructProperty = new UStructProperty();

            public async Task<UEScriptStruct> GetStruct()
            {
                if (ObjStructProperty.Empty())
                    ObjStructProperty = await Object.Cast<UStructProperty>();

                if (!ObjStructProperty.Struct.IsValid())
                    return new UEScriptStruct();

                return (await ObjectsStore.GetByAddress(ObjStructProperty.Struct)).Cast<UEScriptStruct>();
            }
            public new async Task<Info> GetInfo()
            {
                return new Info(PropertyType.PredefinedStruct, Marshal.SizeOf<FName>(), true, $"struct {MakeUniqueCppName(await GetStruct())}");
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEStrProperty : UEProperty
        {
            public new static Task<Info> GetInfo()
            {
                return Task.FromResult(new Info(PropertyType.PredefinedStruct, Marshal.SizeOf<FString>(), true, "struct FString"));
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UETextProperty : UEProperty
        {
            public new static Task<Info> GetInfo()
            {
                return Task.FromResult(new Info(PropertyType.PredefinedStruct, Marshal.SizeOf<FText>(), true, "struct FText"));
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEArrayProperty : UEProperty
        {
            protected UArrayProperty ObjArrayProperty = new UArrayProperty();
            public async Task<UEProperty> GetInner()
            {
                if (ObjArrayProperty.Empty())
                    ObjArrayProperty = await Object.Cast<UArrayProperty>();

                if (!ObjArrayProperty.Inner.IsValid())
                    return new UEProperty();

                return (await ObjectsStore.GetByAddress(ObjArrayProperty.Inner)).Cast<UEProperty>();
            }
            public new async Task<Info> GetInfo()
            {
                var inner = await (await GetInner()).GetInfo();

                return inner.Type != PropertyType.Unknown
                    ? new Info(PropertyType.Container, Marshal.SizeOf<TArray>(), false, $"TArray<{Generator.GetOverrideType(inner.CppType)}>")
                    : new Info { Type = PropertyType.Unknown };
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEMapProperty : UEProperty
        {
            protected UMapProperty ObjMapProperty = new UMapProperty();

            public async Task<UEProperty> GetKeyProperty()
            {
                if (ObjMapProperty.Empty())
                    ObjMapProperty = await Object.Cast<UMapProperty>();

                if (!ObjMapProperty.KeyProp.IsValid())
                    return new UEProperty();

                return (await ObjectsStore.GetByAddress(ObjMapProperty.KeyProp)).Cast<UEProperty>();
            }
            public async Task<UEProperty> GetValueProperty()
            {
                if (ObjMapProperty.Empty())
                    ObjMapProperty = await Object.Cast<UMapProperty>();

                if (!ObjMapProperty.ValueProp.IsValid())
                    return new UEProperty();

                return (await ObjectsStore.GetByAddress(ObjMapProperty.ValueProp)).Cast<UEProperty>();
            }
            public new async Task<Info> GetInfo()
            {
                // Tasks
                var keyT = GetKeyProperty();
                var valueT = GetValueProperty();

                // Values
                var key = await (await keyT).GetInfo();
                var value = await (await valueT).GetInfo();

                if (key.Type != PropertyType.Unknown && value.Type != PropertyType.Unknown)
                    return new Info(PropertyType.Container, 0x50, false, $"TMap<{Generator.GetOverrideType(key.CppType)}, {Generator.GetOverrideType(value.CppType)}>");

                return new Info { Type = PropertyType.Unknown };
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEDelegateProperty : UEProperty
        {
            protected UDelegateProperty ObjDelegateProperty = new UDelegateProperty();

            public async Task<UEFunction> GetSignatureFunction()
            {
                if (ObjDelegateProperty.Empty())
                    ObjDelegateProperty = await Object.Cast<UDelegateProperty>();

                if (!ObjDelegateProperty.SignatureFunction.IsValid())
                    return new UEFunction();

                return (await ObjectsStore.GetByAddress(ObjDelegateProperty.SignatureFunction)).Cast<UEFunction>();
            }
            public new static Task<Info> GetInfo()
            {
                return Task.FromResult(new Info(PropertyType.PredefinedStruct, Marshal.SizeOf<FScriptDelegate>(), true, "struct FScriptDelegate"));
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEMulticastDelegateProperty : UEProperty
        {
            protected UDelegateProperty ObjDelegateProperty = new UDelegateProperty();

            public async Task<UEFunction> GetSignatureFunction()
            {
                if (ObjDelegateProperty.Empty())
                    ObjDelegateProperty = await Object.Cast<UDelegateProperty>();

                if (!ObjDelegateProperty.SignatureFunction.IsValid())
                    return new UEFunction();

                return (await ObjectsStore.GetByAddress(ObjDelegateProperty.SignatureFunction)).Cast<UEFunction>();
            }
            public new static Task<Info> GetInfo()
            {
                return Task.FromResult(new Info(PropertyType.PredefinedStruct, Marshal.SizeOf<FScriptMulticastDelegate>(), true, "struct FScriptMulticastDelegate"));
            }
        }

        // ReSharper disable once InconsistentNaming
        public class UEEnumProperty : UEProperty
        {
            protected UEnumProperty ObjEnumProperty = new UEnumProperty();

            public async Task<UENumericProperty> GetUnderlyingProperty()
            {
                if (ObjEnumProperty.Empty())
                    ObjEnumProperty = await Object.Cast<UEnumProperty>();

                if (!ObjEnumProperty.UnderlyingProp.IsValid())
                    return new UENumericProperty();

                return (await ObjectsStore.GetByAddress(ObjEnumProperty.UnderlyingProp)).Cast<UENumericProperty>();
            }
            public async Task<UEEnum> GetEnum()
            {
                if (ObjEnumProperty.Empty())
                    ObjEnumProperty = await Object.Cast<UEnumProperty>();

                if (!ObjEnumProperty.Enum.IsValid())
                    return new UEEnum();

                return (await ObjectsStore.GetByAddress(ObjEnumProperty.Enum)).Cast<UEEnum>();
            }
            public new async Task<Info> GetInfo()
            {
                return new Info(PropertyType.Primitive, sizeof(byte), false, await MakeUniqueCppName(await GetEnum()));
            }
        }
    }
}
