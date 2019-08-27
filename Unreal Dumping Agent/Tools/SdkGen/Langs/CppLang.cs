using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Json;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Langs
{
    public class BasicHeader : IncludeFile<CppLang>
    {
        private readonly List<string> _pragmas = new List<string>() { "warning(disable: 4267)" };
        private readonly List<string> _include = new List<string>() { "<vector>", "<locale>", "<set>" };
        public override string FileName { get; set; } = "Basic.h";

        public override void Process(string includePath)
        {
            // Read File
            var fileStr = ReadThisFile(includePath);

            // Replace Main stuff
            fileStr.BaseStr.Replace("/*!!INCLUDE!!*/", TargetLang.GetFileHeader(_pragmas, _include, true));
            fileStr.BaseStr.Replace("/*!!FOOTER!!*/", TargetLang.GetFileFooter());

            var jStruct = JsonReflector.GetStruct("FUObjectItem");
            string fUObjectItemStr = string.Empty;

            // Replace
            foreach (var (_, var) in jStruct.Vars)
            {
                fUObjectItemStr += var.VarType.All(char.IsDigit)
                    ? $"\tunsigned char {var.Name} [{var.VarType}];{Utils.NLine}"
                    : $"\t{var.VarType} {var.Name};{Utils.NLine}";
            }

            fileStr.BaseStr.Replace("/*!!DEFINE!!*/", Generator.IsGObjectsChunks ? "#define GOBJECTS_CHUNKS" : "");
            fileStr.BaseStr.Replace("/*!!POINTER_SIZE!!*/", Utils.GamePointerSize().ToString());
            fileStr.BaseStr.Replace("/*!!FUObjectItem_MEMBERS!!*/", fUObjectItemStr);

            // Write File
            CopyToSdk(fileStr);
        }
    }
    public class BasicCpp : IncludeFile<CppLang>
    {
        private readonly List<string> _include = new List<string>() { "\"../SDK.h\"", "<Windows.h>" };
        public override string FileName { get; set; } = "Basic.cpp";

        public override void Process(string includePath)
        {
            // Read File
            var fileStr = ReadThisFile(includePath);

            // Replace Main stuff
            fileStr.BaseStr.Replace("/*!!INCLUDE!!*/", TargetLang.GetFileHeader(_include, false));
            fileStr.BaseStr.Replace("/*!!FOOTER!!*/", TargetLang.GetFileFooter());

            // Replace
            fileStr.BaseStr.Replace("/*!!DEFINE!!*/", "");
            if (!Generator.GameModule.Empty())
            {
                long gObjectsOffset = ObjectsStore.GObjects.Address.ToInt64() - Generator.GameModuleBase.ToInt64();
                long gNamesOffset = NamesStore.GNames.Address.ToInt64() - Generator.GameModuleBase.ToInt64();
                fileStr.BaseStr.Replace("/*!!AUTO_INIT_SDK!!*/", $"InitSdk(\"{Generator.GameModule}\", 0x{gObjectsOffset:X}, 0x{gNamesOffset:X});");
            }
            else
            {
                fileStr.BaseStr.Replace("/*!!AUTO_INIT_SDK!!*/", "throw std::exception(\"Don't use this func.\");");
            }

            // Write File
            CopyToSdk(fileStr);
        }
    }

    public class CppLang : SdkLang
    {
        public string IncludePath => Path.Combine(Generator.LangPaths, "Cpp", Generator.SdkType == SdkType.External ? "External" : "Internal");

        #region FileStruct
        public enum FileContentType
        {
            Structs,
            Classes,
            Functions,
            FunctionParameters
        }
        public string GetFileHeader(List<string> pragmas, List<string> includes, bool isHeaderFile)
        {
            var sb = new CorrmStringBuilder();

            // Pragmas
            if (isHeaderFile)
            {
                sb.BaseStr.Append($"#pragma once{Utils.NLine}");
                if (pragmas.Count > 0)
                    foreach (string i in pragmas) { sb.BaseStr.Append($"#pragma " + i + $"{Utils.NLine}"); }
                sb.BaseStr.Append($"{Utils.NLine}");
            }

            if (Generator.SdkType == SdkType.External)
                sb.BaseStr.Append($"#include \"../Memory.h\"{Utils.NLine}");

            // Includes
            if (includes.Count > 0)
                foreach (string i in includes) { sb.BaseStr.Append("#include " + i + $"{Utils.NLine}"); }
            sb.BaseStr.Append($"{Utils.NLine}");

            // 
            sb.BaseStr.Append($"// Name: {Generator.GameName.Trim()}, Version: {Generator.GameVersion}{Utils.NLine}{Utils.NLine}");
            sb.BaseStr.Append($"#ifdef _MSC_VER{Utils.NLine}\t#pragma pack(push, 0x{Generator.GetGlobalMemberAlignment():X2}){Utils.NLine}#endif{Utils.NLine}{Utils.NLine}");
            sb.BaseStr.Append($"namespace {Generator.NameSpace}{Utils.NLine}{{{Utils.NLine}");

            return sb;
        }
        public string GetFileHeader(List<string> includes, bool isHeaderFile)
        {
            return GetFileHeader(new List<string>(), includes, isHeaderFile);
        }
        public string GetFileHeader(bool isHeaderFile)
        {
            return GetFileHeader(new List<string>(), new List<string>(), isHeaderFile);
        }
        public string GetFileFooter()
        {
            return $"}}{Utils.NLine}{Utils.NLine}#ifdef _MSC_VER{Utils.NLine}\t#pragma pack(pop){Utils.NLine}#endif{Utils.NLine}";
        }
        public string GetSectionHeader(string name)
        {
            return
                $"//---------------------------------------------------------------------------{Utils.NLine}" +
                $"// {name}{Utils.NLine}" +
                $"//---------------------------------------------------------------------------{Utils.NLine}{Utils.NLine}";
        }
        public string GenerateFileName(FileContentType type, string packageName)
        {
            switch (type)
            {
                case FileContentType.Structs:
                    return $"{packageName}_structs.h";
                case FileContentType.Classes:
                    return $"{packageName}_classes.h";
                case FileContentType.Functions:
                    return $"{packageName}_functions.cpp";
                case FileContentType.FunctionParameters:
                    return $"{packageName}_parameters.h";
                default:
                    throw new Exception("WHAT IS THIS TYPE .?!!");
            }
        }
        #endregion

        #region BuildMethod
        public string MakeValidName(string name)
        {
            name = name.Replace(' ', '_')
                .Replace('?', '_')
                .Replace('+', '_')
                .Replace('-', '_')
                .Replace(':', '_')
                .Replace('/', '_')
                .Replace('^', '_')
                .Replace('(', '_')
                .Replace(')', '_')
                .Replace('[', '_')
                .Replace(']', '_')
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('&', '_')
                .Replace('.', '_')
                .Replace('#', '_')
                .Replace('\\', '_')
                .Replace('"', '_')
                .Replace('%', '_');

            if (string.IsNullOrEmpty(name)) return name;

            if (char.IsDigit(name[0]))
                name = '_' + name;

            return name;
        }
        public string BuildMethodSignature(Package.Method m, Package.Class c, bool inHeader)
        {
            var text = new CorrmStringBuilder();

            // static
            if (m.IsStatic && inHeader && Generator.ShouldConvertStaticMethods)
                text += $"static ";

            // Return Type
            var retn = m.Parameters.Where(item => item.ParamType == Package.Method.Parameter.Type.Return).ToList();
            text += (retn.Any() ? retn.First().CppType : $"void") + $" ";

            // inHeader
            if (!inHeader)
                text += $"{c.NameCpp}::";
            if (m.IsStatic && Generator.ShouldConvertStaticMethods)
                text += $"STATIC_";
            text += m.Name;

            // Parameters
            text += $"(";
            if (m.Parameters.Count > 0)
            {
                var paramList = m.Parameters
                    .Where(p => p.ParamType != Package.Method.Parameter.Type.Return)
                    .OrderBy(p => p.ParamType)
                    .Select(p => (p.PassByReference ? $"const " : "") + p.CppType + (p.PassByReference ? $"& " :
                                     p.ParamType == Package.Method.Parameter.Type.Out ? $"* " : $" ") + p.Name).ToList();
                if (paramList.Count > 0)
                    text += paramList.Aggregate((cur, next) => cur + ", " + next);
            }
            text += $")";

            return text;
        }
        public string BuildMethodBody(Package.Class c, Package.Method m)
        {
            var text = new CorrmStringBuilder();

            // Function Pointer
            text += $"{{{Utils.NLine}\tstatic auto fn";
            if (Generator.ShouldUseStrings)
            {
                text += $" = UObject::FindObject<UFunction>(";

                if (Generator.ShouldXorStrings)
                    text += $"_xor_(\"{m.FullName}\")";
                else
                    text += $"\"{m.FullName}\"";

                text += $");{Utils.NLine}{Utils.NLine}";
            }
            else
            {
                text += $" = UObject::GetObjectCasted<UFunction>({m.Index});{Utils.NLine}{Utils.NLine}";
            }

            // Parameters
            if (Generator.ShouldGenerateFunctionParametersFile())
            {
                text += $"\t{c.NameCpp}_{m.Name}_Params params;{Utils.NLine}";
            }
            else
            {
                text += $"\tstruct{Utils.NLine}\t{{{Utils.NLine}";
                foreach (var param in m.Parameters)
                    text += $"\t\t{param.CppType,-30} {param.Name};{Utils.NLine}";
                text += $"\t}} params;{Utils.NLine}";
            }

            var retn = m.Parameters.Where(item => item.ParamType == Package.Method.Parameter.Type.Default).ToList();
            if (retn.Any())
            {
                foreach (var param in retn)
                    text += $"\tparams.{param.Name} = {param.Name};{Utils.NLine}";
            }
            text += $"{Utils.NLine}";

            //Function Call
            text += $"\tauto flags = fn->FunctionFlags;{Utils.NLine}";
            if (m.IsNative)
                text += $"\tfn->FunctionFlags |= 0x{UEFunctionFlags.Native:X};{Utils.NLine}";
            text += $"{Utils.NLine}";

            if (m.IsStatic && !Generator.ShouldConvertStaticMethods)
            {
                text += $"\tstatic auto defaultObj = StaticClass()->CreateDefaultObject();{Utils.NLine}";
                text += $"\tdefaultObj->ProcessEvent(fn, &params);{Utils.NLine}{Utils.NLine}";
            }
            else
            {
                text += $"\tUObject::ProcessEvent(fn, &params);{Utils.NLine}{Utils.NLine}";
            }
            text += $"\tfn->FunctionFlags = flags;{Utils.NLine}";

            //Out Parameters
            var rOut = m.Parameters.Where(item => item.ParamType == Package.Method.Parameter.Type.Out).ToList();
            if (rOut.Any())
            {
                text += $"{Utils.NLine}";
                foreach (var param in rOut)
                    text += $"\tif ({param.Name} != nullptr){Utils.NLine}" +
                            $"\t\t*{param.Name} = params.{param.Name};{Utils.NLine}";
            }
            text += $"{Utils.NLine}";

            //Return Value
            var ret = m.Parameters.Where(item => item.ParamType == Package.Method.Parameter.Type.Return).ToList();
            if (ret.Any())
                text += $"{Utils.NLine}\treturn params.{ret.First().Name};{Utils.NLine}";

            text += $"}}{Utils.NLine}";

            return text;
        }
        #endregion

        #region Print
        public void PrintConstant(string fileName, Package.Constant c)
        {
            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, $"#define CONST_{c.Name,-50} {c.Value}{Utils.NLine}");
        }
        public void PrintEnum(string fileName, Package.Enum e)
        {
            CorrmStringBuilder text = new CorrmStringBuilder($"// {e.FullName}{Utils.NLine}enum class {e.Name} : uint8_t{Utils.NLine}{{{Utils.NLine}");

            for (int i = 0; i < e.Values.Count; i++)
                text += $"\t{e.Values[i],-30} = {i},{Utils.NLine}";

            text += $"{Utils.NLine}}};{Utils.NLine}{Utils.NLine}";

            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, text);
        }
        public void PrintStruct(string fileName, Package.ScriptStruct ss)
        {
            var text = new CorrmStringBuilder($"// {ss.FullName}{Utils.NLine}// ");

            if (ss.InheritedSize > 0)
                text += $"0x{(ss.Size - ss.InheritedSize):X4} (0x{ss.Size:X4} - 0x{ss.InheritedSize:X4}){Utils.NLine}";
            else
                text += $"0x{(long)ss.Size:X4}{Utils.NLine}";

            text += $"{ss.NameCppFull}{Utils.NLine}{{{Utils.NLine}";

            //Member
            foreach (var m in ss.Members)
            {
                text +=
                    $"\t{(m.IsStatic ? "static " + m.Type : m.Type),-50} {m.Name + ";",-58} // 0x{(long)m.Offset:X4}(0x{(long)m.Size:X4})" +
                    (!string.IsNullOrEmpty(m.Comment) ? " " + m.Comment : "") +
                    (!string.IsNullOrEmpty(m.FlagsString) ? " (" + m.FlagsString + ")" : "") +
                    $"{Utils.NLine}";
            }
            text += $"{Utils.NLine}";

            //Predefined Methods
            if (ss.PredefinedMethods.Count > 0)
            {
                text += $"{Utils.NLine}";
                foreach (var m in ss.PredefinedMethods)
                {
                    if (m.MethodType == PredefinedMethod.Type.Inline)
                        text += m.Body;
                    else
                        text += $"\t{m.Signature};";
                    text += $"{Utils.NLine}{Utils.NLine}";
                }
            }
            text += $"}};{Utils.NLine}";

            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, text);
        }
        public void PrintClass(string fileName, Package.Class c)
        {
            var text = new CorrmStringBuilder($"// {c.FullName}{Utils.NLine}// ");

            if (c.InheritedSize > 0)
                text += $"0x{c.Size - c.InheritedSize:X4} (0x{(long)c.Size:X4} - 0x{(long)c.InheritedSize:X4}){Utils.NLine}";
            else
                text += $"0x{(long)c.Size:X4}{Utils.NLine}";

            text += $"{c.NameCppFull}{Utils.NLine}{{{Utils.NLine}public:{Utils.NLine}";

            // Member
            foreach (var m in c.Members)
            {
                text +=
                    $"\t{(m.IsStatic ? "static " + m.Type : m.Type),-50} {m.Name,-58}; // 0x{(long)m.Offset:X4}(0x{(long)m.Size:X4})" +
                    (!string.IsNullOrEmpty(m.Comment) ? " " + m.Comment : "") +
                    (!string.IsNullOrEmpty(m.FlagsString) ? " (" + m.FlagsString + ")" : "") +
                    $"{Utils.NLine}";
            }
            text += $"{Utils.NLine}";

            // Predefined Methods
            if (c.PredefinedMethods.Count > 0)
            {
                text += $"{Utils.NLine}";
                foreach (var m in c.PredefinedMethods)
                {
                    if (m.MethodType == PredefinedMethod.Type.Inline)
                        text += m.Body;
                    else
                        text += $"\t{m.Signature};";

                    text += $"{Utils.NLine}{Utils.NLine}";
                }
            }

            // Methods
            if (c.PredefinedMethods.Count > 0)
            {
                text += $"{Utils.NLine}";
                foreach (var m in c.Methods)
                {
                    text += $"\t{BuildMethodSignature(m, new Package.Class(), true)};{Utils.NLine}";
                }
            }

            text += $"}};{Utils.NLine}{Utils.NLine}";

            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, text);
        }
        #endregion

        #region SavePackage
        public override async Task SaveStructs(Package package)
        {
            // Create file
            string fileName = GenerateFileName(FileContentType.Structs, await package.GetName());

            // Init File
            IncludeFile<CppLang>.CreateFile(Generator.SdkPath, fileName);
            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetFileHeader(true));

            if (package.Constants.Count > 0)
            {
                IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetSectionHeader("Constants"));
                foreach (var c in package.Constants)
                    PrintConstant(fileName, c);
            }

            if (package.Enums.Count > 0)
            {
                IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetSectionHeader("Enums"));
                foreach (var e in package.Enums)
                    PrintEnum(fileName, e);
            }

            if (package.ScriptStructs.Count > 0)
            {
                IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetSectionHeader("Script Structs"));
                foreach (var ss in package.ScriptStructs)
                    PrintStruct(fileName, ss);
            }

            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetFileFooter());
        }
        public override async Task SaveClasses(Package package)
        {
            // Create file
            string fileName = GenerateFileName(FileContentType.Classes, await package.GetName());

            // Init File
            IncludeFile<CppLang>.CreateFile(Generator.SdkPath, fileName);
            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetFileHeader(true));

            if (package.Classes.Count <= 0) return;

            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetSectionHeader("Classes"));
            foreach (var c in package.Classes)
                PrintClass(fileName, c);

            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetFileFooter());
        }
        public override async Task SaveFunctions(Package package)
        {
            if (Generator.SdkType == SdkType.External)
                return;

            // Create Function Parameters File
            if (Generator.ShouldGenerateFunctionParametersFile())
                await SaveFunctionParameters(package);

            // ////////////////////////

            // Create Functions file
            string fileName = GenerateFileName(FileContentType.Functions, await package.GetName());

            // Init Functions File
            IncludeFile<CppLang>.CreateFile(Generator.SdkPath, fileName);
            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetFileHeader(new List<string>() { "\"../SDK.h\"" }, false));

            var text = new CorrmStringBuilder(GetSectionHeader("Functions"));
            foreach (var s in package.ScriptStructs)
            {
                foreach (var m in s.PredefinedMethods)
                {
                    if (m.MethodType != PredefinedMethod.Type.Inline)
                        text += $"{m.Body}{Utils.NLine}{Utils.NLine}";
                }
            }

            foreach (var c in package.Classes)
            {
                foreach (var m in c.PredefinedMethods)
                {
                    if (m.MethodType != PredefinedMethod.Type.Inline)
                        text += $"{m.Body}{Utils.NLine}{Utils.NLine}";
                }

                foreach (var m in c.Methods)
                {
                    //Method Info
                    text += $"// {m.FullName}{Utils.NLine}" + $"// ({m.FlagsString}){Utils.NLine}";

                    if (m.Parameters.Count > 0)
                    {
                        text += $"// Parameters:{Utils.NLine}";
                        foreach (var param in m.Parameters)
                            text += $"// {param.CppType,-30} {param.Name,-30} ({param.FlagsString}){Utils.NLine}";
                    }

                    text += $"{Utils.NLine}";
                    text += BuildMethodSignature(m, c, false) + $"{Utils.NLine}";
                    text += BuildMethodBody(c, m) + $"{Utils.NLine}{Utils.NLine}";
                }
            }

            text += GetFileFooter();

            // Write the file
            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, text);
        }
        public override async Task SaveFunctionParameters(Package package)
        {
            // Create file
            string fileName = GenerateFileName(FileContentType.FunctionParameters, await package.GetName());

            // Init File
            IncludeFile<CppLang>.CreateFile(Generator.SdkPath, fileName);
            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, GetFileHeader(new List<string>() { "\"../SDK.h\"" }, true));

            // Section
            var text = new CorrmStringBuilder(GetSectionHeader("Parameters"));

            // Method Params
            foreach (var c in package.Classes)
            {
                foreach (var m in c.Methods)
                {
                    text += $"// {m.FullName}{Utils.NLine}" +
                            $"struct {c.NameCpp}_{m.Name}_Params{Utils.NLine}{{{Utils.NLine}";

                    foreach (var param in m.Parameters)
                        text += $"\t{param.CppType,-50} {param.Name + ";",-58} // ({param.FlagsString}){Utils.NLine}";
                    text += $"}};{Utils.NLine}{Utils.NLine}";
                }
            }

            text += GetFileFooter();

            // Write the file
            IncludeFile<CppLang>.AppendToSdk(Generator.SdkPath, fileName, text);
        }
        public override async Task SaveConstants(Package package)
        {
            
        }

        public override async Task SdkAfterFinish(List<Package> packages, List<GenericTypes.UEStruct> missing)
        {
            var basicHeader = new BasicHeader();
            var basicCpp = new BasicCpp();

            // Copy Include File
            basicHeader.Process(IncludePath);
            basicCpp.Process(IncludePath);

            var text = new CorrmStringBuilder();
            text += $"#pragma once{Utils.NLine}{Utils.NLine}";
            text += $"// ------------------------------------------------ \\\\{Utils.NLine}";
            text += $"// Sdk Generated By ( Unreal Finder Tool By CorrM ) \\\\{Utils.NLine}";
            text += $"// ------------------------------------------------ \\\\{Utils.NLine}";
            text += $"// Name: {Generator.GameName.Trim()}, Version: {Generator.GameVersion}{Utils.NLine}{Utils.NLine}";
            text += $"{Utils.NLine}";

            text += $"#include <set>{Utils.NLine}";
            text += $"#include <string>{Utils.NLine}";
            text += $"#include \"SDK/{basicHeader.FileName}\"{Utils.NLine}";

            // Check for missing structs
            if (missing.Count > 0)
            {
                string missingText = string.Empty;

                // Init File
                IncludeFile<CppLang>.CreateFile(Path.GetDirectoryName(Generator.SdkPath), "MISSING.h");

                foreach (var s in missing)
                {
                    IncludeFile<CppLang>.AppendToSdk(Path.GetDirectoryName(Generator.SdkPath), "MISSING.h", GetFileHeader(true));

                    missingText += $"// {await s.GetFullName()}{Utils.NLine}// ";
                    missingText += $"0x{await s.GetPropertySize():X4}{Utils.NLine}";

                    missingText += $"struct {MakeValidName(await s.GetNameCpp())}{Utils.NLine}{{{Utils.NLine}";
                    missingText += $"\tunsigned char UnknownData[0x{await s.GetPropertySize():X}];{Utils.NLine}}};{Utils.NLine}{Utils.NLine}";
                }

                missingText += GetFileFooter();
                IncludeFile<CppLang>.WriteToSdk(Path.GetDirectoryName(Generator.SdkPath), "MISSING.h", missingText);

                // Append To Sdk Header
                text += $"{Utils.NLine}#include \"SDK/MISSING.h\"{Utils.NLine}";
            }

            text += $"{Utils.NLine}";
            foreach (var package in packages)
            {
                text += $"#include \"SDK/{GenerateFileName(FileContentType.Structs, await package.GetName())}\"{Utils.NLine}";
                text += $"#include \"SDK/{GenerateFileName(FileContentType.Classes, await package.GetName())}\"{Utils.NLine}";

                if (Generator.ShouldGenerateFunctionParametersFile())
                    text += $"#include \"SDK/{GenerateFileName(FileContentType.FunctionParameters, await package.GetName())}\"{Utils.NLine}";
            }

            // Write SDK.h
            IncludeFile<CppLang>.AppendToSdk(Path.GetDirectoryName(Generator.SdkPath), "SDK.h", text);
        }
        #endregion
    }
}
