using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using ImGuiNET;
using SharpDX.Direct3D11;
using Unreal_Dumping_Agent.Chat;
using Unreal_Dumping_Agent.Http;
using Unreal_Dumping_Agent.UI;
using Unreal_Dumping_Agent.UtilsHelper;
using Veldrid;

using static Unreal_Dumping_Agent.UI.ImControl;
namespace Unreal_Dumping_Agent
{
    /**
     * @todo New ui idea
     * @body Create a ui looks like chat (like fb bot) bot talk to u and can show buttons, list of information, other stuff.
     * use ML.Net for and make the bot response to normal user text like `i need to find GObjects` and set small menu for task list `finder, sdk gen, instance, ..etc`
     */

    /*
     * NOTEs:
     * 1- To build must change this VS option.
     *      Options->NuGet Package Manager->PackageReference,
     *      Check Allow format selection.
     * 2- For Debugging Go Debug->Windows->ExceptionSettings->Check all items xD.
     */

    public class Program
    {
        private readonly ChatManager _chatManager = new ChatManager();
        private readonly HttpManager _httpManager = new HttpManager();

        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync()
        {
            var initChat = _chatManager.Init();
            _httpManager.Start(@"C:\Users\CorrM\source\repos\Unreal Dumping Agent\Unreal Dumping Agent\WebSite\", 8080);

            // Init Window
            Utils.MainWindow = new UiWindow();
            Utils.MainWindow.Setup($"Unreal Dumper Agent, Version: {Utils.Version} - {Utils.Title}", new Vector2(1050, 530), new Vector2(0, 0), MainUi);
            Utils.MainWindow.SetOnCenter();
            Utils.MainWindow.SetIcon(Properties.Resources.win);
            Utils.MainWindow.Show();

            // Wait ChatManager init
            await initChat;

            // Wait until window closed
            while (!Utils.MainWindow.Closed())
                Thread.Sleep(1);
        }

        private static void MainUi(UiWindow thiz)
        {
            ImGui.Separator();
            ImGuiEx.VerticalSeparator();
        }
    }
}
