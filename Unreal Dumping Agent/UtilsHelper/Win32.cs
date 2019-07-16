using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Unreal_Dumping_Agent.UtilsHelper
{
    /// <summary>
    /// This class contains all Window API
    /// </summary>
    public static class Win32
    {
        [DllImport("user32.dll")]
        public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);

    }
}
