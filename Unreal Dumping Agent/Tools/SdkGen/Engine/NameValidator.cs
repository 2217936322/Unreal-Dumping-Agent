using System;
using System.Collections.Generic;
using System.Text;

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

            if (string.IsNullOrEmpty(name)) return name;

            if (char.IsDigit(name[0]))
                name = '_' + name;

            return name;
        }
        public static string SimplifyEnumName(string name)
        {
            return !name.Contains(":") ? string.Empty : name.Substring(name.LastIndexOf(':') + 1);
        }
        //public static string MakeUniqueCppNameImpl<T>(ref T t)
        //{
        //    string name;
        //}
    }
}
