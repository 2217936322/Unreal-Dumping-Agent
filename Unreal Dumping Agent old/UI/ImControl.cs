using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unreal_Dumping_Agent.UI
{
    public static class ImControl
    {
        public static bool DonateShow { get; set; }

        // => Patreon Section
        public struct PatreonGoal
        {
            public int CompletedPercentage;
            public string Description;
        }
        public struct PatreonPost
        {
            public string Title;
            public string Content;
        }

        public static List<PatreonGoal> Goals;
        public static PatreonPost LastNews;
        // => Patreon Section

        // => Main Options Section
        public static bool ProcessIdDisabled { get; set; } = false;
        public static bool ProcessLockDisabled { get; set; } = false;
        public static bool ProcessDetectorDisabled { get; set; } = false;
        public static int ProcessId { get; set; } = 19804;
        public static bool[] ProcessControllerToggles{ get; set; } = { false };

        public static bool UseKernelDisabled { get; set; } = false;
        public static bool UseKernel { get; set; }

        public static bool ProcessModuleDisabled { get; set; } = false;
        public static List<string> ProcessModuleItems { get; set; }
        public static int ProcessModuleItemCurrent { get; set; } = 0;

        public static bool GObjectsDisabled { get; set; } = false;
        public static bool GNamesDisabled { get; set; } = false;

        public static bool GameUeDisabled { get; set; } = false;
        public static string GameUeVersion { get; set; } = "0.0.0";
        public static int UeSelectedVersion { get; set; }
        public static List<string> UnrealVersions { get; set; }

        public static string WindowTitle { get; set; } = "NONE";
        // => Main Options Section

        // => Popup
        public static bool PopupNotValidProcess { get; set; } = false;
        public static bool PopupNotValidGnames { get; set; } = false;
        public static bool PopupNotValidGobjects { get; set; } = false;
        // => Popup

        // => Tabs
        public static int CurTapId { get; set; } = 0;
        // => Tabs

        // => GObjects, GNames, Class
        public static bool GObjectsFindDisabled { get; set; } = false;
        public static IntPtr GObjectsAddress { get; set; }
        public static string GObjectsBuf { get; set; } = "7FF68DEB2B00";
        public static List<string> GObjListboxItems { get; set; }
        public static int GObjListboxItemCurrent { get; set; } = 0;

        public static bool GNamesFindDisabled { get; set; } = false;
        public static IntPtr GNamesAddress { get; set; }
        public static string GNamesBuf = "7FF68DFCF1A8";// { 0 };
        public static List<string> GNamesListboxItems { get; set; }
        public static int GNamesListboxItemCurrent { get; set; } = 0;

        public static bool ClassFindDisabled { get; set; } = false;
        public static bool ClassFindInputDisabled { get; set; } = false;
        public static string ClassFindBuf { get; set; }
        public static List<string> ClassListboxItems { get; set; }
        public static int ClassListboxItemCurrent { get; set; } = 0;
        // => GObjects, GNames, Class

        // => Instance Logger
        public static bool IlStartDisabled { get; set; } = false;
        public static int IlObjectsCount { get; set; } = 0;
        public static int IlNamesCount { get; set; } = 0;
        public static string IlState { get; set; } = "Ready ..!!";
        // => Instance Logger
    }
}
