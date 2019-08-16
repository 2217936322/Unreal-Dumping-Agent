using System;
using System.Collections.Generic;
using System.IO;
using Unreal_Dumping_Agent.Json;
using Unreal_Dumping_Agent.Memory;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Tools.SdkGen
{
    using VirtualFunctionPatterns = Dictionary<PatternScanner.Pattern, string>;

    public static class Generator
    {
        private static Dictionary<string, int> _alignasClasses;
        private static Dictionary<string, string> _badChars;
        private static Dictionary<string, string> _badKeywords;
        private static Dictionary<string, string> _overrideTypes;
        private static Dictionary<string, List<PredefinedMember>> _predefinedMembers;
        private static Dictionary<string, List<PredefinedMember>> _predefinedStaticMembers;
        private static Dictionary<string, List<PredefinedMethod>> _predefinedMethods;
        private static Dictionary<string, VirtualFunctionPatterns> _virtualFunctionPattern;

        public static SdkType SdkType { get; set; }
        public static string NameSpace { get; set; } = "Sdk";
        public static string SdkLangName { get; set; }
        public static SdkLang GenLang { get; set; }
        public static string LangPaths { get; set; }
        public static string SdkPath { get; set; }
        public static bool ShouldDumpArrays { get; set; } = false;
        public static bool ShouldGenerateEmptyFiles { get; set; } = false;
        public static bool ShouldUseStrings { get; set; } = true;
        public static bool ShouldXorStrings { get; set; } = false;
        public static bool ShouldConvertStaticMethods { get; set; } = true;
        public static bool IsGObjectsChunks { get; set; }

        public static IntPtr GameModuleBase { get; set; }
        public static string GameModule { get; set; }
        public static string GameName { get; set; }
        public static string GameVersion { get; set; }

        private static List<PredefinedMember> GetJsonStructPreMembers(string jStructName)
        {
            var jStruct = JsonReflector.GetStruct(jStructName);
            var varList = new List<PredefinedMember>();

            foreach (var (_, jStructVar) in jStruct.Vars)
            {
                if (jStructVar.FromSuper)
                    continue;

                PredefinedMember cur;
                if (Utils.IsNumber(jStructVar.VarType))
                {
                    cur.Name = $"{jStructVar.Name}[{jStructVar.VarType}]";
                    cur.Type = "unsigned char ";
                }
                else
                {
                    cur.Name = jStructVar.Name;
                    cur.Type = jStructVar.IsPointer && !jStructVar.VarType.Contains("void*") ? $"class {jStructVar.VarType}" : jStructVar.VarType;
                }

                varList.Add(cur);
            }

            return varList;
        }
        public static bool Initialize()
        {
            _overrideTypes = new Dictionary<string, string>();
            _predefinedMembers = new Dictionary<string, List<PredefinedMember>>();
            _predefinedStaticMembers = new Dictionary<string, List<PredefinedMember>>();
            _predefinedMethods = new Dictionary<string, List<PredefinedMethod>>();
            _virtualFunctionPattern = new Dictionary<string, VirtualFunctionPatterns>();

            // BadKeywords
            _badKeywords = new Dictionary<string, string>
            {
                {"return", "returnValue"},
                {"continue", "continueValue"},
                {"break", "breakValue"},
                {"int", "intValue"},
                {"bool", "boolValue"}
            };

            // BacChar
            _badChars = new Dictionary<string, string>
            {
                { ",", ""},
                { "!", ""},
                { "-", ""}
            };

            // AlignasClasses
            _alignasClasses = new Dictionary<string, int>
            {
                {"ScriptStruct CoreUObject.Plane", 16},
                {"ScriptStruct CoreUObject.Quat", 16},
                {"ScriptStruct CoreUObject.Transform", 16},
                {"ScriptStruct CoreUObject.Vector4", 16},
                {"ScriptStruct Engine.RootMotionSourceGroup", 8}
            };

            // VirtualFunctionPattern
            _virtualFunctionPattern["Class CoreUObject.Object"] = new VirtualFunctionPatterns
            {
                {
                    PatternScanner.Parse("ProcessEvent", 0, "4C 8B DC 57 48 81 EC"),
                    @"	inline void ProcessEvent(class UFunction* function, void* parms)
	{
		GetVFunction<void(*)(UObject*, class UFunction*, void*)>(this, %d)(this, function, parms);
	}"
                }
            };
            _virtualFunctionPattern["Class CoreUObject.Class"] = new VirtualFunctionPatterns
            {
                {
                    PatternScanner.Parse("CreateDefaultObject", 0, "4C 8B DC 57 48 81 EC"),
                    @"	inline UObject* CreateDefaultObject()
	{
		GetVFunction<UObject*(*)(UClass*)>(this, %d)(this);
	}"
                }
            };

            // PredefinedMembers
            _predefinedMembers["Class CoreUObject.Object"] = GetJsonStructPreMembers("UObject");
            _predefinedMembers["Class CoreUObject.Field"] = GetJsonStructPreMembers("UField");
            _predefinedMembers["Class CoreUObject.Struct"] = GetJsonStructPreMembers("UStruct");
            _predefinedMembers["Class CoreUObject.Function"] = GetJsonStructPreMembers("UFunction");

            // PredefinedStaticMembers
            _predefinedStaticMembers["Class CoreUObject.Object"] = new List<PredefinedMember>
            {
                new PredefinedMember("FUObjectArray*", "GObjects")
            };

            // PredefinedMethods
            _predefinedMethods["ScriptStruct CoreUObject.Vector2D"] = new List<PredefinedMethod>
            {
                PredefinedMethod.Inline(@"	inline FVector2D()
		: X(0), Y(0)
	{ }"),
                PredefinedMethod.Inline(@"	inline FVector2D(float x, float y)
        : X(x), Y(y)
    { }")
            };
            _predefinedMethods["ScriptStruct CoreUObject.LinearColor"] = new List<PredefinedMethod>
            {
                PredefinedMethod.Inline(@"	inline FLinearColor()
		: R(0), G(0), B(0), A(0)
	{ }"),
                PredefinedMethod.Inline(@"	inline FLinearColor(float r, float g, float b, float a)
		: R(r),
		  G(g),
		  B(b),
		  A(a)
	{ }")
            };
            _predefinedMethods["Class CoreUObject.Object"] = new List<PredefinedMethod>
            {
                PredefinedMethod.Inline(@"static inline TUObjectArray& GetGlobalObjects()
	{
		return GObjects->ObjObjects;
	}"),
                PredefinedMethod.Default("std::string GetName() const", @"std::string UObject::GetName() const
{
	std::string name(Name.GetName());
	if (Name.Number > 0)
	{
		name += '_' + std::to_string(Name.Number);
	}

	auto pos = name.rfind('/');
	if (pos == std::string::npos)
	{
		return name;
	}
	
	return name.substr(pos + 1);
}"),
                PredefinedMethod.Default("std::string GetFullName() const", @"std::string UObject::GetFullName() const
{
	std::string name;

	if (Class != nullptr)
	{
		std::string temp;
		for (auto p = Outer; p; p = p->Outer)
		{
			temp = p->GetName() + ""."" + temp;
		}

		name = Class->GetName();
		name += "" "";
		name += temp;
		name += GetName();
	}

	return name;
}"),
                PredefinedMethod.Inline(@"template<typename T>
	static T* FindObject(const std::string& name)
	{
		for (int i = 0; i < GetGlobalObjects().Num(); ++i)
		{
			auto object = GetGlobalObjects().GetByIndex(i);
	
			if (object == nullptr)
			{
				continue;
			}
	
			if (object->GetFullName() == name)
			{
				return static_cast<T*>(object);
			}
		}
		return nullptr;
	}"),
                PredefinedMethod.Inline(@"	template<typename T>
	static T* FindObject()
	{
		auto v = T::StaticClass();
		for (int i = 0; i < SDK::UObject::GetGlobalObjects().Num(); ++i)
		{
			auto object = SDK::UObject::GetGlobalObjects().GetByIndex(i);

			if (object == nullptr)
			{
				continue;
			}

			if (object->IsA(v))
			{
				return static_cast<T*>(object);
			}
		}
		return nullptr;
	}"),
                PredefinedMethod.Inline(@"	template<typename T>
	static std::vector<T*> FindObjects(const std::string& name)
	{
		std::vector<T*> ret;
		for (int i = 0; i < GetGlobalObjects().Num(); ++i)
		{
			auto object = GetGlobalObjects().GetByIndex(i);

			if (object == nullptr)
			{
				continue;
			}

			if (object->GetFullName() == name)
			{
				ret.push_back(static_cast<T*>(object));
			}
		}
		return ret;
	}"),
                PredefinedMethod.Inline(@"	template<typename T>
	static std::vector<T*> FindObjects()
	{
		std::vector<T*> ret;
		auto v = T::StaticClass();
		for (int i = 0; i < SDK::UObject::GetGlobalObjects().Num(); ++i)
		{
			auto object = SDK::UObject::GetGlobalObjects().GetByIndex(i);

			if (object == nullptr)
			{
				continue;
			}

			if (object->IsA(v))
			{
				ret.push_back(static_cast<T*>(object));
			}
		}
		return ret;
	}"),
                PredefinedMethod.Inline(@"	static UClass* FindClass(const std::string& name)
	{
		return FindObject<UClass>(name);
	}"),
                PredefinedMethod.Inline(@"template<typename T>
	static T* GetObjectCasted(std::size_t index)
	{
		return static_cast<T*>(GetGlobalObjects().GetByIndex(index));
	}"),
                PredefinedMethod.Default("bool IsA(UClass* cmp) const", @"bool UObject::IsA(UClass* cmp) const
{
	for (auto super = Class; super; super = static_cast<UClass*>(super->SuperField))
	{
		if (super == cmp)
		{
			return true;
		}
	}

	return false;
}"),
            };
            _predefinedMethods["Class CoreUObject.Class"] = new List<PredefinedMethod>
            {
                PredefinedMethod.Inline(@"template<typename T>
	inline T* CreateDefaultObject()
	{
		return static_cast<T*>(CreateDefaultObject());
	}")
            };

            /*
             predefinedMethods["Class Engine.GameViewportClient"] =
	{
		PredefinedMethod::Inline(R"(	inline void PostRender(UCanvas* Canvas)
{
	return GetVFunction<void(*)(UGameViewportClient*, UCanvas*)>(this, %d)(this, Canvas);
})")
	};
             */

            return true;
        }
        public static string GetOutputDirectory()
        {
            return Path.Combine(Environment.CurrentDirectory, "Result", "Sdk");
        }
        public static int GetGlobalMemberAlignment()
        {
            return Utils.MemObj.Is64Bit ? 0x8 : 0x4;
        }
        public static int GetClassAlignas(string name)
        {
            return _alignasClasses.ContainsKey(name) ? _alignasClasses[name] : 0;
        }
        public static string GetOverrideType(string type)
        {
            return _overrideTypes.ContainsKey(type) ? _overrideTypes[type] : type;
        }
        public static string GetSafeKeywordsName(string name)
        {
            name = _badKeywords.ContainsKey(name) ? _badKeywords[name] : name;

            foreach (var (toFind, toReplace) in _badChars)
                name = name.Replace(toFind, toReplace);

            return name;
        }
        public static bool GetPredefinedClassMembers(string name, ref List<PredefinedMember> members)
        {
            if (!_predefinedMembers.ContainsKey(name))
                return false;

            members = _predefinedMembers[name];
            return true;
        }
        public static bool GetPredefinedClassStaticMembers(string name, ref List<PredefinedMember> members)
        {
            if (!_predefinedStaticMembers.ContainsKey(name))
                return false;

            members = _predefinedStaticMembers[name];
            return true;
        }
        public static bool GetVirtualFunctionPatterns(string name, ref VirtualFunctionPatterns patterns)
        {
            if (!_virtualFunctionPattern.ContainsKey(name))
                return false;

            patterns = _virtualFunctionPattern[name];
            return true;
        }
        public static bool GetPredefinedClassMethods(string name, ref List<PredefinedMethod> methods)
        {
            if (!_predefinedMethods.ContainsKey(name))
                return false;

            methods = _predefinedMethods[name];
            return true;
        }
        public static bool ShouldGenerateFunctionParametersFile()
        {
            return SdkType == SdkType.Internal;
        }

    }
}
