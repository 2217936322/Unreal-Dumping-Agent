using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using System.Threading;
using ImGuiNET;
using Unreal_Dumping_Agent.UI;
using Unreal_Dumping_Agent.UtilsHelper;
using Veldrid;

using static Unreal_Dumping_Agent.UI.ImControl;
namespace Unreal_Dumping_Agent
{
    /*
     * NOTEs:
     * 1- To build must change this VS option.
     *      Options->NuGet Package Manager->PackageReference,
     *      Check Allow format selection.
     * 2- For Debugging Go Debug->Windows->ExceptionSettings->Check all items xD.
     */

    // TODO: complete ui window to look as UFT.
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
            ImGuiEx.VerticalSeparator();
        }
        private static void DonationPopup(UiWindow thiz)
        {
#if !DEBUG
            if (donateShow)
                ImGui.OpenPopup("Donate?");

            // Popup
            bool open = false;
            if (ImGui.BeginPopupModal("Donate?", ref open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
            {
                ImGui.TextColored(new Vector4(0.92f, 0.30f, 0.29f, 1.0f), "Welcome on Unreal Finder Tool");
                ImGui.Text("(To code this tool it take a BIG time.\nWith your support i can give it more time.\nAny help, even small, make a difference.)");
        
                ImGui.TextColored(new Vector4(230, 126, 34, 255), "On Patreon:\nYou will open future Exclusive articles and tutorial");
                ImGui.Separator();

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.92f, 0.30f, 0.29f, 1.0f));
                if (ImGui.Button("Patreon", new Vector2(120, 0)))
                {
                    Process.Start("https://www.patreon.com/bePatron?u=16013498");
                    donateShow = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor();

                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.28f, 0.20f, 0.83f, 1.0f));
                if (ImGui.Button("PayPal", new Vector2(120, 0)))
                {
                    Process.Start("http://paypal.me/IslamNofl");

                    donateShow = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor();

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    donateShow = false;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.PopStyleColor();

                ImGui.EndPopup();
            }
#endif
        }
        private static void TitleBarUi(UiWindow thiz)
        {
            // ImGui.ShowDemoWindow();

            // Settings Button
            {
                if (ImGui.Button(FontAwesome5.Cog))
                    ImGui.OpenPopup("SettingsMenu");

                if (ImGui.BeginPopup("SettingsMenu"))
                {
                    if (ImGui.BeginMenu("Process##menu"))
                    {
                        if (ImGui.MenuItem("Pause Process", "", ProcessControllerToggles[0]))
                        {
                            if (!IsReadyToGo())
                            {
                                ProcessControllerToggles[0] = false;
                                PopupNotValidProcess = true;
                            }
                            else
                            {
                                if (ProcessControllerToggles[0])
                                    Utils::MemoryObj->SuspendProcess();
                                else
                                    Utils::MemoryObj->ResumeProcess();
                            }
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.Separator();

                    if (ImGui.BeginMenu("Locator##menu"))
                    {
                        if (ImGui.MenuItem("Tool Folder"))
                        {
                            ShellExecute(nullptr,
                                "open",
                                Utils::GetWorkingDirectoryA().c_str(),
                                nullptr,
                                nullptr,
                                SW_SHOWDEFAULT);
                        }
                        ImGui.Separator();

                        if (ImGui.MenuItem("SDK Folder"))
                        {
                            ShellExecute(nullptr,
                                "open",
                                (Utils::GetWorkingDirectoryA() + "\\Results").c_str(),
                                nullptr,
                                nullptr,
                                SW_SHOWDEFAULT);
                        }
                        ImGui.Separator();

                        if (ImGui.MenuItem("Config Folder"))
                        {
                            ShellExecute(nullptr,
                                "open",
                                (Utils::GetWorkingDirectoryA() + "\\Config").c_str(),
                                nullptr,
                                nullptr,
                                SW_SHOWDEFAULT);
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.Separator();

                    if (ImGui.BeginMenu("Help##menu"))
                    {
                        if (ImGui.MenuItem("GitHub"))
                        {
                            ShellExecute(nullptr,
                                "open",
                                "https://github.com/CorrM/Unreal-Finder-Tool",
                                nullptr,
                                nullptr,
                                SW_SHOWDEFAULT);
                        }
                        ImGui.Separator();

                        if (ImGui.MenuItem("Wiki"))
                        {
                            ShellExecute(nullptr,
                                "open",
                                "https://github.com/CorrM/Unreal-Finder-Tool/wiki",
                                nullptr,
                                nullptr,
                                SW_SHOWDEFAULT);
                        }
                        ImGui.Separator();

                        if (ImGui.MenuItem("Report issue"))
                        {
                            ShellExecute(nullptr,
                                "open",
                                "https://github.com/CorrM/Unreal-Finder-Tool/issues/new",
                                nullptr,
                                nullptr,
                                SW_SHOWDEFAULT);
                        }
                        ImGui.Separator();

                        if (ImGui.MenuItem("Last version"))
                        {
                            ShellExecute(nullptr,
                                "open",
                                "https://github.com/CorrM/Unreal-Finder-Tool/releases/latest",
                                nullptr,
                                nullptr,
                                SW_SHOWDEFAULT);
                        }
                        ImGui.Separator();

                        if (ImGui.MenuItem("Version note"))
                        {
                            ShellExecute(nullptr,
                                "open",
                                "https://github.com/CorrM/Unreal-Finder-Tool/releases/tag/" TOOL_VERSION,
                                nullptr,
                                nullptr,
                                SW_SHOWDEFAULT);
                        }

                        ImGui.EndMenu();
                    }

# ifndef _DEBUG
                    ImGui.Separator();

                    ImGui.PushStyleColor(ImGuiCol_Text, ImVec4(0.0f, 1.0f, 0.0f, 1.0f));
                    if (ImGui.MenuItem("DONATE")) donate_show = true;
                    ImGui.PopStyleColor();
#endif
                    ImGui.EndPopup();
                }
            }

            // Title
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(abs(ImGui.CalcTextSize("Unreal Finder Tool By CorrM").x - ImGui.GetWindowWidth()) / 2);
                ImGui.TextColored(ImVec4(1.0f, 1.0f, 0.0f, 1.0f), "Unreal Finder Tool By CorrM");

# ifdef MIDI_h
                ImGui.SameLine();
                ImGui.SetCursorPosX(abs(ImGui.GetWindowWidth() - 65));

                if (ImGui.Button(!MidiPlayer || (MidiPlayer->IsPaused() || !MidiPlayer->IsPlaying()) ? ICON_FA_PLAY : ICON_FA_PAUSE))
                {
                    if (!MidiPlayer)
                    {
                        MidiPlayer = new CMIDI();
                        MidiPlayer->Create(const_cast<LPBYTE>(midi_track1), sizeof midi_track1);
                    }
                    if (MidiPlayer->IsPaused())
                        MidiPlayer->Continue();
                    else if (MidiPlayer->IsPlaying())
                        MidiPlayer->Pause();
                    else
                        MidiPlayer->Play(true);
                }
                ImGui.SameLine();
                if (ImGui.Button(ICON_FA_STOP))
                {
                    if (MidiPlayer && MidiPlayer->IsPlaying())
                        MidiPlayer->Stop();
                }
#endif
            }
        }
    }
}
