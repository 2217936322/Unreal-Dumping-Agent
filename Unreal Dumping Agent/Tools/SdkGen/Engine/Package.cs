using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Memory;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine
{
    using VirtualFunctionPatterns = Dictionary<PatternScanner.Pattern, string>;

    // ToDo: Most Important Thing, like on GenerateMembers, in loop u can run all async methods in prop and then wait when u need

#pragma warning disable 660,661
    [DebuggerDisplay("Name = {GetName().Result}")]
    public class Package
#pragma warning restore 660,661
    {
        public static Dictionary<GenericTypes.UEObject, Package> PackageMap { get; } = new Dictionary<GenericTypes.UEObject, Package>();
        public static Dictionary<IntPtr, bool> ProcessedObjects { get; private set; }

        private readonly GenericTypes.UEObject _packageObj;
        private readonly List<GenericTypes.UEObject> _dependencies;

        public List<Constant> Constants { get; }
        public List<Class> Classes { get; }
        public List<ScriptStruct> ScriptStructs { get; }
        public List<Enum> Enums { get; }

        #region Structs

        public class Constant
        {
            public string Name;
            public string Value;
        }

        public class Enum
        {
            public string Name;
            public string FullName;
            public List<string> Values;

            public Enum()
            {
                Values = new List<string>();
            }
        }

        public class Member
        {
            public string Name;
            public string Type;
            public bool IsStatic;

            public int Offset;
            public int Size;

            public int Flags;
            public string FlagsString;

            public string Comment;
        }

        public class ScriptStruct
        {
            public string Name;
            public string FullName;
            public string NameCpp;
            public string NameCppFull;

            public int Size;
            public int InheritedSize;

            public List<Member> Members;
            public List<PredefinedMethod> PredefinedMethods;

            public ScriptStruct()
            {
                Members = new List<Member>();
                PredefinedMethods = new List<PredefinedMethod>();
            }
        }

        public class Method
        {
            public class Parameter
            {
                public enum Type
                {
                    Default,
                    Out,
                    Return
                }

                public Type ParamType;
                public bool PassByReference;
                public string CppType;
                public string Name;
                public string FlagsString;

                private readonly UnrealVersion _unrealVersion;

                public Parameter(UnrealVersion targetEngine)
                {
                    _unrealVersion = targetEngine;
                }

                /// <summary>
                /// Generates a valid type of the property flags.
                /// </summary>
                /// <param name="flags">The property flags.</param>
                /// <param name="type">[out] The parameter type.</param>
                /// <returns>true if it is a valid type, else false.</returns>
                public bool MakeType(UEPropertyFlags flags, out Type type)
                {
                    type = Type.Default;

                    switch (_unrealVersion)
                    {
                        case UnrealVersion.Unreal4:
                            return PackageCore.MakeType(flags, out type);
                    }

                    return false;
                }
            }

            public int Index;
            public string Name;
            public string FullName;
            public List<Parameter> Parameters = new List<Parameter>();
            public string FlagsString;
            public bool IsNative;
            public bool IsStatic;
        }

        public class Class : ScriptStruct
        {
            public List<string> VirtualFunctions = new List<string>();
            public List<Method> Methods = new List<Method>();
        }

        #endregion

        #region Operators
        public static bool operator ==(Package lhs, Package rhs) => lhs?._packageObj.GetAddress() == rhs?._packageObj.GetAddress();
        public static bool operator !=(Package lhs, Package rhs) => !(lhs == rhs);
        public static async Task<bool> ComparePropertyLess(GenericTypes.UEProperty lhs, GenericTypes.UEProperty rhs)
        {
            var offsetL = lhs.GetOffset();
            var offsetR = rhs.GetOffset();
            var isBoolPropertyL = lhs.IsA<GenericTypes.UEBoolProperty>();
            var isBoolPropertyR = rhs.IsA<GenericTypes.UEBoolProperty>();

            if (await offsetL == await offsetR &&
                await isBoolPropertyL &&
                await isBoolPropertyR)
            {
                return lhs.Cast<GenericTypes.UEBoolProperty>() < rhs.Cast<GenericTypes.UEBoolProperty>();
            }

            return await offsetL < await offsetR;
        }
        public static bool PackageDependencyComparer(Package lhs, Package rhs)
        {
            if (rhs._dependencies.Empty())
                return false;

            if (rhs._dependencies.Any(o => o == lhs._packageObj))
                return true;

            foreach (var dep in rhs._dependencies)
            {
                var package = PackageMap[dep];
                if (package == null)
                    continue; // Missing package, should not occur...

                if (PackageDependencyComparer(lhs, package))
                    return true;
            }

            return false;
        }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="packageObj">The package object.</param>
        public Package(GenericTypes.UEObject packageObj)
        {
            _packageObj = packageObj;
            _dependencies = new List<GenericTypes.UEObject>();

            Constants = new List<Constant>();
            Classes = new List<Class>();
            ScriptStructs = new List<ScriptStruct>();
            Enums = new List<Enum>();
            ProcessedObjects = new Dictionary<IntPtr, bool>();
        }

        /// <summary>
        /// Get package name
        /// </summary>
        public async Task<string> GetName() => await _packageObj.GetName();

        /// <summary>
        /// Get all objects in this package
        /// </summary>
        public static Task<List<GenericTypes.UEObject>> GetObjsInPack(GenericTypes.UEObject packageObj)
        {
            return Task.Run(() =>
            {
                var outPackages = new List<GenericTypes.UEObject>();
                var lockObj = new object();

                foreach (var obj in ObjectsStore.GObjects.Objects)
                {
                    if (packageObj != obj.GetPackageObject().Result)
                        continue;

                    lock (lockObj)
                        outPackages.Add(obj);
                }

                return outPackages;
            });
        }

        /// <summary>
        /// Process the classes the package contains.
        /// </summary>
        /// <returns>Return processedObjects</returns>
        public async Task Process()
        {
            var objsInPack = GetObjsInPack(_packageObj);

            foreach (var obj in await objsInPack)
            {
                // IsA
                var isEnumT = obj.IsA<GenericTypes.UEEnum>();
                var isClassT = obj.IsA<GenericTypes.UEClass>();
                var isSsT = obj.IsA<GenericTypes.UEScriptStruct>();
                var isConstT = obj.IsA<GenericTypes.UEConst>();

                // Checks
                if (await isEnumT)
                {
                    await GenerateEnum(obj.Cast<GenericTypes.UEEnum>());
                }
                else if (await isClassT || await isSsT)
                {
                    await GeneratePrerequisites(obj);
                }
                else if (await isConstT)
                {
                    await GenerateConst(obj.Cast<GenericTypes.UEConst>());
                }
            }
        }

        /// <summary>
        /// Saves the package classes as SdkLang code.
        /// Files are only generated if there is code present or the generator forces the generation of empty files.
        /// </summary>
        /// <returns>true if files got saved, else false.</returns>
        public async Task<bool> Save()
        {
            if (Generator.ShouldGenerateEmptyFiles ||
                Enums.Any(e => !e.Values.Empty()) ||
                ScriptStructs.Any(s => !s.Members.Empty() || s.PredefinedMethods.Empty()) ||
                Classes.Any(c => !c.Methods.Empty() || !c.PredefinedMethods.Empty() || !c.Methods.Empty()) ||
                Constants.Any(c => !c.Name.Empty()))
            {
                SaveStructs();
                SaveClasses();
                SaveFunctions();
                SaveConstants();

                return true;
            }

            await Logger.Log($"Skip Empty:    {await _packageObj.GetFullName()}");
            return false;
        }

        #region Generate
        private bool AddDependency(GenericTypes.UEObject package)
        {
            if (package == _packageObj)
                return false;

            lock (_dependencies)
                _dependencies.Add(package);

            return true;
        }

        /// <summary>
        /// Checks and generates the prerequisites of the object.
        /// Should be a UEClass or UEScriptStruct.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>Return Processed Objects</returns>
        private async Task GeneratePrerequisites(GenericTypes.UEObject obj)
        {
            if (!obj.IsValid())
                return;

            var isClassT = obj.IsA<GenericTypes.UEClass>();
            var isScriptStructT = obj.IsA<GenericTypes.UEClass>();

            if (!await isClassT && !await isScriptStructT)
                return;

            string name = await obj.GetName();
            if (name.Contains("Default__") ||
                name.Contains("<uninitialized>") ||
                name.Contains("PLACEHOLDER-CLASS"))
            {
                return;
            }

            if (!ProcessedObjects.ContainsKey(obj.GetAddress()))
                ProcessedObjects[obj.GetAddress()] = false;

            ProcessedObjects[obj.GetAddress()] = ProcessedObjects[obj.GetAddress()] | false;

            var classPackage = await obj.GetPackageObject();
            if (!classPackage.IsValid())
                return;

            if (AddDependency(classPackage))
                return;

            // Exit if package already processed
            if (ProcessedObjects[obj.GetAddress()])
                return;

            ProcessedObjects[obj.GetAddress()] = true;

            // Outer
            var outer = await obj.GetOuter();
            if (outer.IsValid() && outer != obj)
                await GeneratePrerequisites(outer);

            // Super
            var structObj = obj.Cast<GenericTypes.UEStruct>();
            var super = await structObj.GetSuper();
            if (super.IsValid() && super != obj)
                await GeneratePrerequisites(super);

            await GenerateMemberPrerequisites((await structObj.GetChildren()).Cast<GenericTypes.UEProperty>());


            if (await isClassT)
                await GenerateClass(obj.Cast<GenericTypes.UEClass>());
            else
                await GenerateScriptStruct(obj.Cast<GenericTypes.UEScriptStruct>());
        }

        /// <summary>
        /// Checks and generates the prerequisites of the members.
        /// </summary>
        /// <param name="first">The first member in the chain.</param>
        /// <returns>Return Processed Objects</returns>
        private async Task GenerateMemberPrerequisites(GenericTypes.UEProperty first)
        {
            for (GenericTypes.UEProperty prop = first; prop.IsValid(); prop = (await prop.GetNext()).Cast<GenericTypes.UEProperty>())
            {
                var info = await prop.GetInfo();
                switch (info.Type)
                {
                    case GenericTypes.UEProperty.PropertyType.Primitive:
                        if (await prop.IsA<GenericTypes.UEByteProperty>())
                        {
                            var byteProperty = prop.Cast<GenericTypes.UEByteProperty>();
                            if (await byteProperty.IsEnum())
                                AddDependency(await (await byteProperty.GetEnum()).GetPackageObject());
                        }
                        else if (await prop.IsA<GenericTypes.UEEnumProperty>())
                        {
                            var enumProperty = prop.Cast<GenericTypes.UEEnumProperty>();
                            AddDependency(await (await enumProperty.GetEnum()).GetPackageObject());
                        }
                        break;

                    case GenericTypes.UEProperty.PropertyType.CustomStruct:
                        await GeneratePrerequisites(await prop.Cast<GenericTypes.UEStructProperty>().GetStruct());
                        break;

                    case GenericTypes.UEProperty.PropertyType.Container:
                        var innerProperties = new List<GenericTypes.UEProperty>();

                        if (await prop.IsA<GenericTypes.UEArrayProperty>())
                        {
                            innerProperties.Add(await prop.Cast<GenericTypes.UEArrayProperty>().GetInner());
                        }
                        else if (await prop.IsA<GenericTypes.UEMapProperty>())
                        {
                            var mapProp = prop.Cast<GenericTypes.UEMapProperty>();
                            innerProperties.Add(await mapProp.GetKeyProperty());
                            innerProperties.Add(await mapProp.GetValueProperty());
                        }


                        foreach (var innerProp in innerProperties.Where(p => p.GetInfo().Result.Type == GenericTypes.UEProperty.PropertyType.CustomStruct))
                            await GeneratePrerequisites(await innerProp.Cast<GenericTypes.UEStructProperty>().GetStruct());

                        break;

                    // Not Need
                    case GenericTypes.UEProperty.PropertyType.PredefinedStruct:
                    case GenericTypes.UEProperty.PropertyType.Unknown:
                        break;

                    default:
                        if (await prop.IsA<GenericTypes.UEFunction>())
                        {
                            var function = prop.Cast<GenericTypes.UEFunction>();
                            await GenerateMemberPrerequisites((await function.GetChildren()).Cast<GenericTypes.UEProperty>());
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Generates a script structure.
        /// </summary>
        /// <param name="scriptStructObj">The script structure object.</param>
        private async Task GenerateScriptStruct(GenericTypes.UEScriptStruct scriptStructObj)
        {
            var ss = new ScriptStruct
            {
                Name = await scriptStructObj.GetName(),
                FullName = await scriptStructObj.GetFullName()
            };

            var logTask = Logger.Log($"Struct:  {await GetName() + "." + ss.Name, -85} - instance: 0x{scriptStructObj.GetAddress().ToInt64():X8}");

            ss.NameCpp = NameValidator.MakeValidName(await scriptStructObj.GetNameCpp());
            ss.NameCppFull = "struct ";

            //some classes need special alignment
            var alignment = Generator.GetClassAlignas(ss.FullName);
            if (alignment == 0)
                ss.NameCppFull += $"alignas({alignment}) ";

            ss.NameCppFull += await NameValidator.MakeUniqueCppName(scriptStructObj);
            ss.Size = await scriptStructObj.GetPropertySize();
            ss.InheritedSize = 0;

            int offset = 0;
            var super = await scriptStructObj.GetSuper();

            if (super.IsValid() && super != scriptStructObj)
            {
                ss.InheritedSize = offset = await scriptStructObj.GetPropertySize();
                ss.NameCppFull += $" : public {await NameValidator.MakeUniqueCppName(super.Cast<GenericTypes.UEScriptStruct>())}";
            }

            var properties = new List<GenericTypes.UEProperty>();
            for (var prop = (await scriptStructObj.GetChildren()).Cast<GenericTypes.UEProperty>(); prop.IsValid(); prop = (await prop.GetNext()).Cast<GenericTypes.UEProperty>())
            {
                var isScriptStruct = prop.IsA<GenericTypes.UEScriptStruct>();
                var isFunction = prop.IsA<GenericTypes.UEFunction>();
                var isEnum = prop.IsA<GenericTypes.UEEnum>();
                var isConst = prop.IsA<GenericTypes.UEConst>();

                if (await prop.GetElementSize() > 0 &&
                    !await isScriptStruct &&
                    !await isFunction &&
                    !await isEnum &&
                    !await isConst)
                {
                    properties.Add(prop);
                }
            }

            // ToDo: Check Here
            properties.Sort((x, y) => ComparePropertyLess(x, y).Result ? 0 : 1);

            var memberT = GenerateMembers(scriptStructObj, offset, properties);

            if (Generator.SdkType == SdkType.External)
            {
                // ToDO: Add external Read/Write here for external
            }

            Generator.GetPredefinedClassMethods(await scriptStructObj.GetFullName(), ref ss.PredefinedMethods);

            ss.Members = await memberT;
            ScriptStructs.Add(ss);

            // wait logger
            await logTask;
        }

        /// <summary>
        /// Generates an enum.
        /// </summary>
        /// <param name="enumObj">The enum object.</param>
        private async Task GenerateEnum(GenericTypes.UEEnum enumObj)
        {
            var e = new Enum
            {
                Name = await NameValidator.MakeUniqueCppName(enumObj)
            };

            if (e.Name.Contains("Default__") ||
                e.Name.Contains("PLACEHOLDER-CLASS"))
            {
                return;
            }

            e.FullName = await enumObj.GetFullName();

            var conflicts = new Dictionary<string, int>();
            foreach (var s in await enumObj.GetNames())
            {
                var clean = NameValidator.MakeValidName(s);
                if (!conflicts.ContainsKey(clean))
                {
                    e.Values.Add(clean);
                    conflicts[clean] = 1;
                }
                else
                {
                    e.Values.Add($"{clean}{conflicts[clean]:D2}");
                    conflicts[clean]++;
                }
            }

            Enums.Add(e);
        }

        /// <summary>
        /// Generates a constant.
        /// </summary>
        /// <param name="constObj">The constant object.</param>
        private async Task GenerateConst(GenericTypes.UEConst constObj)
        {
            var name = await NameValidator.MakeUniqueCppName(constObj);

            if (name.Contains("Default__") ||
                name.Contains("PLACEHOLDER-CLASS"))
            {
                return;
            }

            Constants.Add(new Constant { Name = name, Value = constObj.GetValue() });
        }

        /// <summary>
        /// Generates the class.
        /// </summary>
        /// <param name="classObj">The class object.</param>
        private async Task GenerateClass(GenericTypes.UEClass classObj)
        {
            var c = new Class
            {
                Name = await classObj.GetName(),
                FullName = await classObj.GetFullName()
            };

            var logTask = Logger.Log($"Class:   {await GetName() + "." + c.Name,-85} - instance: 0x{classObj.GetAddress().ToInt64():X8}");

            c.NameCpp = NameValidator.MakeValidName(await classObj.GetNameCpp());
            c.NameCppFull = $"class {c.NameCpp}";

            c.Size = await classObj.GetPropertySize();
            c.InheritedSize = 0;

            int offset = 0;

            var super = await classObj.GetSuper();
            if (super.IsValid() && super != classObj)
            {
                c.InheritedSize = offset = await super.GetPropertySize();
                c.NameCppFull += $" : public {NameValidator.MakeValidName(await super.GetNameCpp())}";
            }

            var predefinedStaticMembers = new List<PredefinedMember>();
            if (Generator.GetPredefinedClassStaticMembers(c.FullName, ref predefinedStaticMembers))
            {
                foreach (var prop in predefinedStaticMembers)
                {
                    var p = new Member
                    {
                        Offset = 0,
                        Size = 0,
                        Name = prop.Name,
                        Type = prop.Type,
                        IsStatic = true
                    };
                    c.Members.Add(p);
                }
            }

            var predefinedMembers = new List<PredefinedMember>();
            if (Generator.GetPredefinedClassMembers(c.FullName, ref predefinedMembers))
            {
                foreach (var prop in predefinedMembers)
                {
                    var p = new Member
                    {
                        Offset = 0,
                        Size = 0,
                        Name = prop.Name,
                        Type = prop.Type,
                        IsStatic = false,
                        Comment = "NOT AUTO-GENERATED PROPERTY"
                    };
                    c.Members.Add(p);
                }
            }
            else
            {
                var properties = new List<GenericTypes.UEProperty>();
                for (var prop = (await classObj.GetChildren()).Cast<GenericTypes.UEProperty>(); prop.IsValid(); prop = (await prop.GetNext()).Cast<GenericTypes.UEProperty>())
                {
                    var elementSizeT = prop.GetElementSize();
                    var isScriptStruct = prop.IsA<GenericTypes.UEScriptStruct>();
                    var isFunction = prop.IsA<GenericTypes.UEFunction>();
                    var isEnum = prop.IsA<GenericTypes.UEEnum>();
                    var isConst = prop.IsA<GenericTypes.UEConst>();

                    if (await elementSizeT > 0 && 
                        !await isScriptStruct &&
                        !await isFunction &&
                        !await isEnum &&
                        !await isConst &&
                        (!super.IsValid() || (super != classObj && await prop.GetOffset() >= await super.GetPropertySize())))
                    {
                        properties.Add(prop);
                    }
                }

                // ToDo: As C# sort not same as C++ version, that's not work
                // Anyway after some testes it's not needed !!
                // properties.Sort((x, y) => ComparePropertyLess(x, y).Result ? 0 : 1);

                c.Members = await GenerateMembers(classObj, offset, properties);
            }

            Generator.GetPredefinedClassMethods(c.FullName, ref c.PredefinedMethods);

            if (Generator.SdkType == SdkType.External)
            {
                // ToDO: Add external Read/Write here for external
            }
            else
            {
                if (Generator.ShouldUseStrings)
                {
                    string classStr = Generator.ShouldXorStrings ? $"_xor_(\"{c.FullName}\")" : $"\"{c.FullName}\"";
                    c.PredefinedMethods.Add(PredefinedMethod.Inline($@"	static UClass* StaticClass()
	{{
		static auto ptr = UObject::FindClass({classStr});
		return ptr;
	}}")
                    );
                }
                else
                {
                    c.PredefinedMethods.Add(PredefinedMethod.Inline($@"	static UClass* StaticClass()
	{{
		static auto ptr = UObject::GetObjectCasted<UClass>({classObj.GetIndex()});
		return ptr;
	}}")
                    );
                }

                c.Methods = await GenerateMethods(classObj);

                //search virtual functions
                var patterns = new VirtualFunctionPatterns();
                if (Generator.GetVirtualFunctionPatterns(c.FullName, ref patterns))
                {
                    int ptrSize = Utils.GamePointerSize();
                    IntPtr vTableAddress = classObj.Object.VfTable;
                    var vTable = new List<IntPtr>();

                    int methodCount = 0;
                    while (methodCount < 150)
                    {
				        // Dereference Pointer
                        IntPtr vAddress = Utils.MemObj.ReadAddress(vTableAddress + (methodCount * ptrSize));

                        // Check valid address
                        int res = Win32.VirtualQueryEx(Utils.MemObj.ProcessHandle, vAddress, out var info, (uint)Marshal.SizeOf<Win32.MemoryBasicInformation>());
                        if (res == 0 || info.Protect.HasFlag(Win32.MemoryProtection.PageNoAccess))
                            break;

                        vTable.Add(vAddress);
                        methodCount++;
                    }

                    foreach (var (pattern, funcStr) in patterns)
                    {
                        for (int i = 0; i < methodCount; i++)
                        {
                            if (vTable[i].IsNull())
                                continue;

                            var scanResult = await PatternScanner.FindPattern(Utils.MemObj, vTable[i], vTable[i] + 0x300, new List<PatternScanner.Pattern>{ pattern }, true);
                            if (!scanResult.ContainsKey(pattern.Name) || scanResult[pattern.Name].Empty())
                                continue;

                            c.PredefinedMethods.Add(PredefinedMethod.Inline($@"{funcStr.Replace("%d", i.ToString())}"));
                            break;
                        }
                    }
                }
            }

            // Wait logger
            await logTask;

            Classes.Add(c);
        }

        /// <summary>
        /// Generates a padding member.
        /// </summary>
        /// <param name="id">The unique name identifier.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="size">The size.</param>
        /// <param name="reason">The reason.</param>
        /// <returns>A padding member.</returns>
        private static Member CreatePadding(int id, int offset, int size, string reason)
        {
            var ss = new Member
            {
                Name = $"UnknownData{id}[0x{size:X}]",
                Type = "unsigned char",
                Flags = 0,
                Offset = offset,
                Size = size,
                Comment = reason
            };
            return ss;
        }

        /// <summary>
        /// Generates a padding member.
        /// </summary>
        /// <param name="id">The unique name identifier.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="type">The type.</param>
        /// <param name="bits">The bits.</param>
        /// <returns>A padding member.</returns>
        private static Member CreateBitfieldPadding(int id, int offset, string type, int bits)
        {
            var ss = new Member
            {
                Name = $"UnknownData{id} : {bits}",
                Type = type,
                Flags = 0,
                Offset = offset,
                Size = 1
            };
            return ss;
        }

        /// <summary>
        /// Generates the members of a struct or class.
        /// </summary>
        /// <param name="structObj">The structure object.</param>
        /// <param name="offset">The start offset.</param>
        /// <param name="properties">The properties describing the members.</param>
        /// <returns>The members of the struct or class.</returns>
        private async Task<List<Member>> GenerateMembers(GenericTypes.UEStruct structObj, int offset, List<GenericTypes.UEProperty> properties)
        {
            var members = new List<Member>();
            var uniqueMemberNames = new Dictionary<string, int>();
            int unknownDataCounter = 0;
            var previousBitfieldProperty = new GenericTypes.UEBoolProperty();

            foreach (var prop in properties)
            {
                if (offset < await prop.GetOffset())
                {
                    int size = await prop.GetOffset() - offset;
                    members.Add(CreatePadding(unknownDataCounter++, offset, size, "MISSED OFFSET"));
                }

                var info = await prop.GetInfo();
                if (info.Type != GenericTypes.UEProperty.PropertyType.Unknown)
                {
                    var sp = new Member
                    {
                        Offset = await prop.GetOffset(),
                        Size = info.Size,
                        Type = info.CppType,
                        Name = NameValidator.MakeValidName(await prop.GetName())
                    };

                    if (!uniqueMemberNames.ContainsKey(sp.Name))
                    {
                        uniqueMemberNames[sp.Name] = 1;
                    }
                    else
                    {
                        uniqueMemberNames[sp.Name]++;
                        sp.Name += uniqueMemberNames[sp.Name];
                    }

                    if (await prop.GetArrayDim() > 1)
                        sp.Name += $"[0x{await prop.GetArrayDim():X}]";

                    if (await prop.IsA<GenericTypes.UEBoolProperty>() && await prop.Cast<GenericTypes.UEBoolProperty>().IsBitfield())
                    {
                        var boolProp = prop.Cast<GenericTypes.UEBoolProperty>();
                        var missingBits = await boolProp.GetMissingBitsCount(previousBitfieldProperty);

                        if (missingBits[1] != -1)
                        {
                            if (missingBits[0] > 0)
                            {
                                members.Add(CreateBitfieldPadding(unknownDataCounter++, await previousBitfieldProperty.GetOffset(), info.CppType, missingBits[0]));
                            }
                            if (missingBits[1] > 0)
                            {
                                members.Add(CreateBitfieldPadding(unknownDataCounter++, sp.Offset, info.CppType, missingBits[1]));
                            }
                        }
                        else if (missingBits[0] > 0)
                        {
                            members.Add(CreateBitfieldPadding(unknownDataCounter++, sp.Offset, info.CppType, missingBits[0]));
                        }

                        previousBitfieldProperty = boolProp;
                        sp.Name += " : 1";
                    }

                    sp.Name = Generator.GetSafeKeywordsName(sp.Name);
                    sp.Flags = (int)await prop.GetPropertyFlags();
                    sp.FlagsString = PropertyFlags.StringifyFlags(await prop.GetPropertyFlags());

                    members.Add(sp);

                    int sizeMismatch = (await prop.GetElementSize() * await prop.GetArrayDim()) - (info.Size * await prop.GetArrayDim());
                    if (sizeMismatch > 0)
                    {
                        members.Add(CreatePadding(unknownDataCounter++, offset, sizeMismatch, "FIX WRONG TYPE SIZE OF PREVIOUS PROPERTY"));
                    }
                }
                else
                {
                    var size = await prop.GetElementSize() * await prop.GetArrayDim();
                    members.Add(CreatePadding(unknownDataCounter++, offset, size, "UNKNOWN PROPERTY: " + await prop.GetFullName()));
                }

                offset = await prop.GetOffset() + await prop.GetElementSize() * await prop.GetArrayDim();
            }

            if (offset < await structObj.GetPropertySize())
            {
                int size = await structObj.GetPropertySize() - offset;
                members.Add(CreatePadding(unknownDataCounter, offset, size, "MISSED OFFSET"));
            }

            return members;
        }

        /// <summary>
        /// Generates the methods of a class.
        /// </summary>
        /// <param name="classObj">The class object.</param>
        /// <returns>Return The methods of the class.</returns>
        private async Task<List<Method>> GenerateMethods(GenericTypes.UEClass classObj)
        {
            var methods = new List<Method>();

            //some classes (AnimBlueprintGenerated...) have multiple members with the same name, so filter them out
            var uniqueMethods = new List<string>();

            // prop can be UEObject, UEField, UEProperty
            for (var prop = (await classObj.GetChildren()).Cast<GenericTypes.UEField>(); prop.IsValid(); prop = await prop.GetNext())
            {
                if (!await prop.IsA<GenericTypes.UEFunction>())
                    continue;

                var function = prop.Cast<GenericTypes.UEFunction>();

                var m = new Method
                {
                    Index = function.GetIndex(),
                    FullName = await function.GetFullName(),
                    Name = Generator.GetSafeKeywordsName(NameValidator.MakeValidName(await function.GetName()))
                };

                if (uniqueMethods.Contains(m.FullName))
                    continue;

                uniqueMethods.Add(m.FullName);

                m.IsNative = (await function.GetFunctionFlags()).HasFlag(UEFunctionFlags.Native);
                m.IsStatic = (await function.GetFunctionFlags()).HasFlag(UEFunctionFlags.Static);
                m.FlagsString = FunctionFlags.StringifyFlags(await function.GetFunctionFlags());

                var parameters = new List<KeyValuePair<GenericTypes.UEProperty, Method.Parameter>>();
                var unique = new Dictionary<string, int>();
                for (var param = (await function.GetChildren()).Cast<GenericTypes.UEProperty>(); param.IsValid(); param = (await param.GetNext()).Cast<GenericTypes.UEProperty>())
                {
                    if (await param.GetElementSize() == 0)
                        continue;

                    var info = await param.GetInfo();
                    if (info.Type == GenericTypes.UEProperty.PropertyType.Unknown)
                        continue;

                    var p = new Method.Parameter(UnrealVersion.Unreal4);

                    if (!p.MakeType(await param.GetPropertyFlags(), out p.ParamType))
                    {
                        //child isn't a parameter
                        continue;
                    }

                    p.PassByReference = false;
                    p.Name = NameValidator.MakeValidName(await param.GetName());

                    if (!unique.ContainsKey(p.Name))
                    {
                        unique[p.Name] = 1;
                    }
                    else
                    {
                        unique[p.Name]++;
                        p.Name += unique[p.Name];
                    }

                    p.FlagsString = PropertyFlags.StringifyFlags(await param.GetPropertyFlags());
                    p.CppType = info.CppType;

                    if (await param.IsA<GenericTypes.UEBoolProperty>())
                    {
                        p.CppType = Generator.GetOverrideType("bool");
                    }

                    if (p.ParamType == Method.Parameter.Type.Default)
                    {
                        if (await param.GetArrayDim() > 1)
                        {
                            p.CppType += "*";
                        }
                        else if (info.CanBeReference)
                        {
                            p.PassByReference = true;
                        }
                    }

                    p.Name = Generator.GetSafeKeywordsName(p.Name);
                    parameters.Add(new KeyValuePair<GenericTypes.UEProperty, Method.Parameter>(param, p));
                }

                parameters.Sort((lhs, rhs) => ComparePropertyLess(lhs.Key, rhs.Key).Result ? 0 : 1);

                foreach (var param in parameters)
                    m.Parameters.Add(param.Value);

                methods.Add(m);
            }

            return methods;
        }
        #endregion

        #region SavePackage
        private void SaveStructs() => Generator.GenLang.SaveStructs(this);
        private void SaveClasses() => Generator.GenLang.SaveClasses(this);
        private void SaveFunctions() => Generator.GenLang.SaveFunctions(this);
        private void SaveConstants() => Generator.GenLang.SaveConstants(this);
        #endregion
    }
}
