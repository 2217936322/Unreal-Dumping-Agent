using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine
{
    public static class NameValidator
    {
        /// <summary>
        /// Makes valid C++ name from the given name.
        /// </summary>
        /// <param name="name">The name to process.</param>
        /// <returns>A valid C++ name.</returns>
        public static string MakeValidName(string name)
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

            if (string.IsNullOrEmpty(name))
                return name;

            if (char.IsDigit(name[0]))
                name = '_' + name;

            return name;
        }
        public static string SimplifyEnumName(string name)
        {
            return !name.Contains(":") ? string.Empty : name.Substring(name.LastIndexOf(':') + 1);
        }
        private static async Task<string> MakeUniqueCppNameImpl<T>(T t) where T : GenericTypes.UEObject, new()
        {
            string name = string.Empty;

            if (ObjectsStore.CountObjects<T>(await t.GetName()) > 1)
                name += $"{MakeValidName((await t.GetOuter()).GetName().Result)}_";

            return $"{name}{MakeValidName(await t.GetName())}";
        }

        public static async Task<string> MakeUniqueCppName(GenericTypes.UEConst c) => await MakeUniqueCppNameImpl(c);
        public static async Task<string> MakeUniqueCppName(GenericTypes.UEEnum e) => await MakeUniqueCppNameImpl(e);
        public static async Task<string> MakeUniqueCppName(GenericTypes.UEStruct ss) => await MakeUniqueCppNameImpl(ss);

    }
}
