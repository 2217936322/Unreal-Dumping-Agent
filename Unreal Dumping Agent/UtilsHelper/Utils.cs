using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unreal_Dumping_Agent.UI;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    public static class Utils
    {
        public const string Version = "1.0.0";
        public const string Title = "Unreal Suspender";

        public static UiWindow MainWindow;

        public static bool Is64Bit()
        {
#if x64
            return true;
#else
            return true;
#endif
        }
        public static bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return true;
#endif
        }
    }
}
