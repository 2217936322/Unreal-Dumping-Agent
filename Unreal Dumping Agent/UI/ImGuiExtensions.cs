using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Unreal_Dumping_Agent.UI
{
    public static class ImGuiEx
    {
        public static void VerticalSeparator()
        {
            ImGui.GetWindowDrawList().AddLine(new Vector2(10, 50), new Vector2(60, 60), ImGui.GetColorU32(ImGuiCol.Text));
        }
    }
}
