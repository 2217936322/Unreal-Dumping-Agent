using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unreal_Dumping_Agent.Utils
{
    public static class UtilsHelper
    {
        public const string Version = "1.0.0";
        public const string Title = "Unreal Suspender";

        public static bool Is64Bit()
        {
#if x64
            return true;
#else
            return true;
#endif
        }
    }
}
