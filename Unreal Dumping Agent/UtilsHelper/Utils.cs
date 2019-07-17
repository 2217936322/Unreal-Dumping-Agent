using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClrPlus.Windows.Api;
using ClrPlus.Windows.Api.Structures;
using Unreal_Dumping_Agent.UI;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    public static class Utils
    {
        public const string Version = "1.0.0";
        public const string Title = "Unreal Suspender";

        public static UiWindow MainWindow;

        public static bool ProgramIs64()
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

        #region Process
        public static bool Is64Bit(this Process process)
        {
            // PROCESS_QUERY_INFORMATION 
            var processHandle = Kernel32.OpenProcess(0x0400, false, process.Id);
            bool ret = Kernel32.IsWow64Process(processHandle, out bool retVal) && retVal;
            processHandle.Close();
            return !ret;
        }
        #endregion
    }
}
