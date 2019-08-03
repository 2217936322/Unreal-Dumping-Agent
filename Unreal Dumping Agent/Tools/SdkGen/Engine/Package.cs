using System;
using System.Collections.Generic;
using System.Text;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine
{
    public class Package
    {
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
            public List<string> Values = new List<string>();
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

            public List<Member> Members = new List<Member>();
            public List<PredefinedMethod> PredefinedMethods = new List<PredefinedMethod>();
        }
        public class Method
        {
            public abstract class Parameter
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

                /// <summary>
                /// Generates a valid type of the property flags.
                /// </summary>
                /// <param name="flags">The property flags.</param>
                /// <param name="type">[out] The parameter type.</param>
                /// <returns>true if it is a valid type, else false.</returns>
                public abstract bool MakeType(UEPropertyFlags flags, ref Type type);
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

    }
}
