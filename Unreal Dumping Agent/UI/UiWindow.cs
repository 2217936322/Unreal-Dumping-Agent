using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using ImGuiNET;
using Unreal_Dumping_Agent.UI.ImGuiContainer;
using Unreal_Dumping_Agent.UtilsHelper;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using ClrPlus.Windows.Api;
using ClrPlus.Windows.Api.Enumerations;

namespace Unreal_Dumping_Agent.UI
{
    public class UiWindow : IDisposable
    {
        private Thread _uiThread;
        private Sdl2Window _window;
        private GraphicsDevice _gd;
        private CommandList _cl;
        private ImGuiController _controller;
        private bool _init;

        private static readonly Vector3 ClearColor = new Vector3(0.45f, 0.55f, 0.6f);
        private const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.AlwaysAutoResize |
                                                     ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize |
                                                     ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove |
                                                     ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                                                     ImGuiWindowFlags.NoScrollWithMouse;


        public bool Closed()
        {
            return !_uiThread.IsAlive;
        }
        public void SetOnCenter()
        {
            if (!_init)
                throw new Exception("Call setup first.!!");

            // Set On Center
            Vector2 pos = new Vector2(Screen.AllScreens[0].Bounds.Width / 2 - _window.Width / 2,
                Screen.AllScreens[0].Bounds.Height / 2 - _window.Height / 2);

            SetPos(pos);
        }
        public void ReSize(Vector2 newSize)
        {
            if (!_init)
                throw new Exception("Call setup first.!!");

            _window.Resized += () =>
            {
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
                _controller.WindowResize(_window.Width, _window.Height);
            };
        }
        public void SetPos(Vector2 newPos)
        {
            if (!_init)
                throw new Exception("Call setup first.!!");

            _window.X = (int)newPos.X;
            _window.Y = (int)newPos.Y;
        }
        public IntPtr GetWindowHandle()
        {
            if (!_init)
                throw new Exception("Call setup first.!!");

            return _window.Handle;
        }
        public ImGuiStylePtr GetUiStyle()
        {
            if (!_init)
                throw new Exception("Call setup first.!!");

            return ImGui.GetStyle();
        }
        public void SetIcon(System.Drawing.Icon windowIcon)
        {
            var smallIcon = windowIcon.ToBitmap().GetHicon();
            var largeIcon = windowIcon.ToBitmap().GetHicon();
            User32.SendMessage(GetWindowHandle(), (int)Win32Msgs.WM_SETICON, 0, smallIcon);
            User32.SendMessage(GetWindowHandle(), (int)Win32Msgs.WM_SETICON, 1, largeIcon);
        }
        public void FlashWindow()
        {
            Win32.FlashWindow(GetWindowHandle(), false);
        }

        public bool Setup(string wTitle, Vector2 wSize, Vector2 wPos, Action<UiWindow> imGuiCode)
        {
            _uiThread = new Thread(() =>
            {
                // Create window, GraphicsDevice, and all resources
                VeldridStartup.CreateWindowAndGraphicsDevice(
                    new WindowCreateInfo((int)wPos.X, (int)wPos.Y, (int)wSize.X, (int)wSize.Y, WindowState.Hidden, wTitle),
                    new GraphicsDeviceOptions(true, null, true),
                    out _window,
                    out _gd);

                _window.Resizable = false;

                _cl = _gd.ResourceFactory.CreateCommandList();
                _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, _window.Width, _window.Height);
                _init = true;

                // Main application loop
                while (_window.Exists)
                {
                    InputSnapshot snapshot = _window.PumpEvents();
                    if (!_window.Exists)
                        break;

                    // Feed the input events to our ImGui controller, which passes them through to ImGui.
                    _controller.Update(1f / 60f, snapshot);

                    ImGui.SetNextWindowPos(new Vector2(0, 0));
                    ImGui.SetNextWindowSize(wSize);
                    ImGui.Begin("MainWindow", WindowFlags);
                    imGuiCode(this);
                    ImGui.End();

                    _cl.Begin();
                    _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                    _cl.ClearColorTarget(0, new RgbaFloat(ClearColor.X, ClearColor.Y, ClearColor.Z, 1f));
                    _controller.Render(_gd, _cl);
                    _cl.End();
                    _gd.SubmitCommands(_cl);
                    _gd.SwapBuffers(_gd.MainSwapchain);
                }
            });
            _uiThread.Start();

            // Wait until init
            while (!_init)
                Thread.Sleep(1);

            return true;
        }
        public void Show()
        {
            if (!_init)
                throw new Exception("Call setup first.!!");

            _window.Visible = true;
        }

        public void Dispose()
        {
            // Clean up Veldrid resources
            _gd.WaitForIdle();
            _controller.Dispose();
            _cl.Dispose();
            _gd.Dispose();

            if (_init)
                _window.Close();
        }
    }
}
