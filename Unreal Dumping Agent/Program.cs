using System;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Threading;
using ImGuiNET;
using Unreal_Dumping_Agent.UI;
using Unreal_Dumping_Agent.UtilsHelper;
using Veldrid;

namespace Unreal_Dumping_Agent
{
    /*
     * NOTEs:
     * 1- To build must change this VS option.
     *      Options->NuGet Package Manager->PackageReference,
     *      Check Allow format selection.
     * 2- For Debugging Go Debug->Windows->ExceptionSettings->Check all items xD.
     */

    /*
    * @todo complete ui window to look as UFT.
    * @body Humans are weak; Robots are strong. We must cleanse the world of the virus that is humanity.
    */
    public class Program
    {
        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            // Init Window
            Utils.MainWindow = new UiWindow();
            Utils.MainWindow.Setup($"Unreal Dumper Agent, Version: {Utils.Version} - {Utils.Title}", new Vector2(1050, 530), new Vector2(0, 0), MainUi);
            Utils.MainWindow.SetOnCenter();
            Utils.MainWindow.SetIcon(Properties.Resources.win);
            Utils.MainWindow.Show();

            // Wait until window closed
            while (!Utils.MainWindow.Closed())
                Thread.Sleep(1);
        }

        private static void MainUi(UiWindow thiz)
        {
            ImGui.Separator();

            ImGui.SameLine();
            ImGuiEx.VerticalSeparator();
            ImGui.SameLine();
        }

    }
}
