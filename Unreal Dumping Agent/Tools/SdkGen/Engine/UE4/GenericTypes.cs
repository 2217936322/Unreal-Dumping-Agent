using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.UtilsHelper;
using static Unreal_Dumping_Agent.Tools.SdkGen.EngineClasses;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4
{
    // TODO: Convert most of functions to async
    public class GenericTypes : IEngineVersion
    {
        [DebuggerDisplay("Address = {Object.ObjAddress.ToInt64().ToString(\"X8\")}")]
        public class UEObject : IUnrealStruct
        {
            private static readonly int _typeId = NamesStore.GetByName(MethodBase.GetCurrentMethod().DeclaringType?.Name.Remove(1, 1)); // Remove E from `UEObject`

            protected UClass ObjClass { get; set; }
            protected UEObject Outer { get; set; }
            protected UEObject Package { get; set; }
            protected string ObjName { get; set; }
            protected string FullName { get; set; }
            protected string NameCpp { get; set; }

            public UObject Object { get; set; }

            public UEObject() => Object = new UObject();
            public UEObject(UObject uObject) => Object = uObject;

            public int TypeId() => _typeId;
            public UClass StaticClass() => _typeId;

            public IntPtr GetAddress() => Object.ObjAddress;
            public bool IsValid()
            {
                return Object.ObjAddress.IsValid() && Object.VfTable.IsValid() && (Object.Name.Index > 0 && Object.Name.Index <= NamesStore.GNames.Names.Count);
            }
            public int GetIndex()
            {
                return ObjectsStore.GetIndexByAddress(Object.ObjAddress);
            }
            public string GetName()
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
            }
            public string GetInstanceClassName()
            {
                if (!IsValid())
                    return string.Empty;

                UEObject obj = ObjectsStore.GetByAddress(GetAddress(), out bool found);

                return found ? obj.GetClass().GetNameCpp() : string.Empty;
            }
            public string GetFullName()
            {
                if (!FullName.Empty())
                    return FullName;

                var cClass = GetClass();
                if (!cClass.IsValid()) return "(null)";

                string temp = string.Empty;
                for (UEObject outer = GetOuter(); outer.IsValid(); outer = outer.GetOuter())
                    temp = temp.Insert(0, $"{outer.GetName()}.");

                FullName = $"{cClass.GetName()} {temp}{GetName()}";

                return FullName;
            }
            public string GetNameCpp()
            {
                if (!NameCpp.Empty())
                    return NameCpp;

                string name = string.Empty;
                if (IsA<UEClass>().Result)
                {
                    var c = (UEClass)this;
                    while (c.IsValid())
                    {
                        string className = c.GetName();
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

                        c = (UEClass)c.GetSuper();

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

            public UEClass GetClass()
            {
                // Must have a class, aka must not hit that condition
                if (!Object.Class.IsValid())
                    return new UEClass();

                return (UEClass)ObjectsStore.GetByAddress(Object.Class);
            }
            public UEObject GetOuter()
            {
                return !Object.Outer.IsValid() ? new UEObject() : ObjectsStore.GetByAddress(Object.Outer);
            }
            public UEObject GetPackageObject()
            {
                if (Package != null)
                    return Package;

                // Package Is The Last Outer
                for (UEObject outer = GetOuter(); outer.IsValid(); outer = outer.GetOuter())
                    Package = outer;

                return Package ?? (Package = ObjectsStore.GetByAddress(Object.ObjAddress));
            }

            public async Task<bool> IsA<T>() where T : IUnrealStruct, new()
            {
                return await Task.Run(() =>
                {
                    if (!IsValid())
                        return false;

                    int cmpTypeId = new T().TypeId();
                    for (UEClass super = GetClass(); super.IsValid(); super = (UEClass)super.GetSuper())
                    {
                        if (super.Object.Name.Index == cmpTypeId)
                            return true;
                    }
                    return false;
                });
            }
            public bool IsA(string typeName)
            {

            }
        }

        public class UEField : UEObject
        {
            protected UField ObjField { get; set; }

            public UEField GetNext()
            {
                

            }
        }

        public class UEEnum : UEField
        {
            protected UEnum ObjEnum;

            public List<string> GetNames()
            {

            }
        }

        public class UEConst : UEField
        {
            public string GetValue()
            {

            }
        }

        public class UEStruct : UEField
        {
            protected UStruct ObjStruct;

            public UEStruct GetSuper()
            {

            }

            public UEField GetChildren()
            {

            }

            public int GetPropertySize()
            {

            }
        }

        public class UEScriptStruct : UEStruct
        {

        }

        public class UEFunction : UEStruct
        {
            protected UFunction ObjFunction;

            public UEFunctionFlags GetFunctionFlags()
            {

            }
        }

        public class UEClass : UEStruct
        {
            
        }
    }
}
