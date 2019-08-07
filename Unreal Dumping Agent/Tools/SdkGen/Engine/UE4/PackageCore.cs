using System;
using System.Collections.Generic;
using System.Text;

namespace Unreal_Dumping_Agent.Tools.SdkGen.Engine.UE4
{
    public static class PackageCore
    {
        public static bool MakeType(UEPropertyFlags flags, out Package.Method.Parameter.Type type)
        {
            type = Package.Method.Parameter.Type.Default;

            if (flags.HasFlag(UEPropertyFlags.ReturnParm))
                type = Package.Method.Parameter.Type.Return;

            else if (flags.HasFlag(UEPropertyFlags.OutParm))
                //if it is a const parameter make it a default parameter
                type = flags.HasFlag(UEPropertyFlags.ConstParm) ? Package.Method.Parameter.Type.Default : Package.Method.Parameter.Type.Out;

            else if (flags.HasFlag(UEPropertyFlags.Parm))
                type = Package.Method.Parameter.Type.Default;

            else
                return false;

            return true;
        }
    }
}
