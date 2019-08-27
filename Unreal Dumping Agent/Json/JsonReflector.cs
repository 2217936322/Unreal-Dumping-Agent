using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unreal_Dumping_Agent.UtilsHelper;

namespace Unreal_Dumping_Agent.Json
{
    using JsonStructs = Dictionary<string, JsonStruct>;
    using JsonVariables = Dictionary<string, JsonVar>;

    public static class JsonReflector
    {
        public static JsonStructs StructsList { get; set; } = new JsonStructs();
        private static JObject _engineJObject;

        internal static bool IsStructType(string typeName)
        {
            bool isStruct =
                typeName == "bool" ||
                typeName == "int8" ||
                typeName == "int16" ||
                typeName == "int" ||
                typeName == "int32" ||
                typeName == "int64" ||

                typeName == "uint8" ||
                typeName == "uint16" ||
                typeName == "uint" ||
                typeName == "uint32" ||
                typeName == "uint64" ||

                typeName.EndsWith("*") || // pointer
                typeName == "DWORD" ||
                typeName == "DWORD64" ||
                typeName == "string" ||

                Utils.IsNumber(typeName, out _, out _);

            return !isStruct;
        }

        internal static int VarSizeFromName(string typeName)
        {
            switch (typeName)
            {
                case "bool":
                case "int8":
                case "uint8":
                    return sizeof(byte);

                case "int16":
                case "uint16":
                    return sizeof(short);

                case "DWORD":
                case "int":
                case "int32":
                case "uint":
                case "uint32":
                    return sizeof(int);

                case "DWORD64":
                case "int64":
                case "uint64":
                case "string":
                    return sizeof(long);
            }

            // Pointer
            if (typeName.EndsWith("*"))
                return IntPtr.Size;

	        // Other type (usually) structs
            if (Utils.IsNumber(typeName, out int val))
                return val;

            if (!IsStructType(typeName))
                throw new KeyNotFoundException($"Cant detect size of `{typeName}`.");

            if (StructsList.ContainsKey(typeName) || LoadStruct(typeName))
                return StructsList[typeName].GetSize(false);

            throw new NullReferenceException($"Cant find struct `{typeName}`.");
        }

        /// <summary>
        /// Load json file to init UnrealEngine structs
        /// </summary>
        /// <param name="jsonFileName">ex: EngineBase</param>
        /// <param name="overrideOld">override old struct if found ?</param>
        public static void LoadJsonEngine(string jsonFileName, bool overrideOld = false)
        {
            jsonFileName = jsonFileName.EndsWith(".json") ? jsonFileName : $"{jsonFileName}.json";

            string filePath = Path.Combine(Program.ConfigPath, "EngineCore", jsonFileName);
            _engineJObject = JObject.Parse(File.ReadAllText(filePath));

            foreach (var jsonStructs in _engineJObject["structs"])
                LoadStruct(jsonStructs["name"].ToString(), overrideOld);
        }

        private static bool LoadStruct(string structName, bool overrideOld = false)
        {
            if (_engineJObject == null)
                throw new NullReferenceException($"{_engineJObject} is null!!, call `ReadJsonEngine` first.");

            if (StructsList.ContainsKey(structName) && !overrideOld)
                return true;

            var structsArray = _engineJObject["structs"];
            foreach (var structObj in structsArray.Where(s => s["name"].ToString() == structName))
            {
                int offset = 0;
                int structSize = 0;

                var ret = new JsonStruct
                {
                    StructName = structObj["name"].ToString(),
                    StructSuper = structObj["super"].ToString()
                };

                // Get Super
                {
                    if (!string.IsNullOrWhiteSpace(ret.StructSuper))
                    {
                        // Add super struct Variables to struct first
                        if ((StructsList.ContainsKey(ret.StructSuper) && !overrideOld) || LoadStruct(ret.StructSuper, overrideOld))
                        {
                            var s = StructsList[ret.StructSuper];
                            foreach (var structVar in s.Vars)
                            {
                                var sVar = structVar.Value;
                                var jVar = new JsonVar(
                                    sVar.Name,
                                    sVar.VarType,
                                    offset,
                                    sVar.IsStruct,
                                    true);

                                ret.Vars.Add(sVar.Name, jVar);
                                offset += jVar.Size;
                            }

                            structSize += s.GetSize();
                        }
                        else
                        {
                            throw new KeyNotFoundException($"Can't find `{ret.StructSuper}` Struct.");
                        }
                    }
                }

                // Init vars
                {
                    var sVars = structObj["vars"];
                    foreach (var sVar in sVars.Children<JObject>().Properties())
                    {
                        string sName = sVar.Name.Replace("pad",  $"pad_{Utils.RandomString(2)}");

                        var jVar = new JsonVar(
                            sName,
                            sVar.Value.ToString(),
                            offset,
                            IsStructType(sVar.Value.ToString()),
                            false);

                        ret.Vars.Add(sName, jVar);
                        offset += jVar.Size;

                        structSize += Utils.IsNumber(sVar.Value.ToString(), out int val) 
                            ? val
                            : VarSizeFromName(sVar.Value.ToString());
                    }
                }

                // Init Struct
                ret.SetSize(structSize);

                // Add struct to struct list
                if (overrideOld)
                {
                    // Update all structs that inheritance form this overrides struct
                    // Twice to be sure all structs override
                    for (int i = 0; i < 2; i++)
                    {
                        foreach (var (sName, _) in StructsList)
                            LoadStruct(sName, true);
                    }
                }

                // check if it not in the list
                if (!StructsList.ContainsKey(ret.StructName))
                    StructsList.Add(ret.StructName, ret);
            }

            return true;
        }

        // Read struct form loaded json structs
        public static JsonStruct GetStruct(string structName)
        {
            if (StructsList.ContainsKey(structName))
                return StructsList[structName];

            throw new KeyNotFoundException($"Can't find {structName} in loaded structs.");
        }
    }

    public class JsonStruct
    {
	    // Size of this struct
        private int _structSize;

	    // Struct Name
        public string StructName { get; set; }

	    // Super Name
        public string StructSuper { get; set; }

        // Variables inside this struct
        public JsonVariables Vars { get; set; }

        public JsonStruct()
        {
            Vars = new JsonVariables();
        }

        // Get unneeded size to sub from the struct size
        private int GetUnneededSize()
        {
            int sSub = 0;

            // if it's 32bit game (4byte pointer) sub 4byte for every pointer
            if (!Utils.ProgramIs64() || Utils.MemObj.Is64Bit) return sSub;

            foreach (var (_, value) in Vars)
            {
                if (value.VarType.EndsWith("*"))
                    sSub += 0x4;
                else if (value.IsStruct)
                    sSub += value.Struct.GetUnneededSize();
            }

            return sSub;
        }

	    // Size of this struct, useful for 32bit games in 64bit version of this tool
        public int GetSize(bool subUnneeded = true)
        {
            if (!subUnneeded)
                return _structSize;

            return _structSize - GetUnneededSize();
        }

	    // Don't use it outside `JsonReflector Load Functions`
        internal void SetSize(int newSize)
        {
            _structSize = newSize;
        }

        /// <summary>
        /// Access to variable inside this struct
        /// </summary>
        /// <param name="name">Variable Name</param>
        public JsonVar GetVar(string name)
        {
            if (Vars.ContainsKey(name))
                return Vars[name];

            throw new Exception($"Not found {name} in JsonVariables");
        }

        /// <summary>
        /// Access to variable inside this struct
        /// </summary>
        /// <param name="name">Variable Name</param>
        public JsonVar this[string name] => GetVar(name);
    }

    public class JsonVar
    {
	    // Variable Name
        public string Name { get; set; }

        // Variable Type
        public string VarType { get; set; }

        // Variable Size
        public int Size { get; set; }

        // Variable offset of his parent
        public int Offset { get; set; }

        // Variable is struct
        public bool IsStruct { get; set; }

        // Variable is pointer of `VarType`
        public bool IsPointer { get; set; }

        // Variable from super struct
        public bool FromSuper { get; set; }

        // If this variable is struct this is variables in the struct
        public JsonStruct Struct { get; set; }

        public JsonVar(string name, string varType, int offset, bool isStruct, bool fromSuper)
        {
            Name = name;
            VarType = varType;
            Size = JsonReflector.VarSizeFromName(varType);
            Offset = offset;
            IsStruct = isStruct;
            IsPointer = varType.EndsWith("*");
            FromSuper = fromSuper;

            if (isStruct)
                LoadStructVars();
        }

        /// <summary>
        /// Access variable inside this variable, <remarks>ONLY work if this variable is struct</remarks>
        /// </summary>
        /// <param name="name">Variable Name</param>
        public JsonVar GetVar(string name)
        {
            if (!IsStruct)
                throw new Exception($"{Name} not a struct.!!");

            if (Struct.Vars.ContainsKey(name))
                return Struct.Vars[name];

            throw new Exception($"Not found {name} in JsonVariables");
        }

        /// <summary>
        /// Access variable inside this variable, <remarks>ONLY work if this variable is struct</remarks>
        /// </summary>
        /// <param name="name">Variable Name</param>
        public JsonVar this[string name] => GetVar(name);

        /// <summary>
        /// Read variable as struct, <remarks>ONLY work if this variable is struct</remarks> [NOT POINTER TO STRUCT]
        /// </summary>
        public void LoadStructVars()
        {
            if (!IsStruct)
                throw new Exception($"{Name} not a struct.!!");

            if (!JsonReflector.StructsList.ContainsKey(VarType))
                throw new KeyNotFoundException($"Can't find struct When try read as {VarType}");

            Struct = JsonReflector.StructsList[VarType];
        }
    }
}
