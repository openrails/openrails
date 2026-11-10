// COPYRIGHT 2026 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Viewer3D.VR;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using MenuForm = Menu.MainForm;

namespace Orts.Viewer3D.Processes
{
    /// <summary>
    /// The desktop start menu (the Menu program's main window, with all its drop-downs, buttons and
    /// dialogs) hosted inside the game and shown as a panel floating in front of the driver, so a run
    /// can be chosen from a headset where the desktop cannot be seen.
    ///
    /// The window itself is real: it opens on the desktop as usual and is captured into a texture
    /// thirty times a second, together with any pop-ups it has open (drop-down lists, the options
    /// dialog). A controller points at the panel like a mouse: the trigger clicks, the thumbstick
    /// scrolls, and the menu button recentres the view. Clicks are fed to the window as mouse input,
    /// so the real mouse works on it too. Starting a run carries on in this process, with the command
    /// line the menu would have given RunActivity.
    /// </summary>
    public class GameStateVRMenu : GameState
    {
        // The panel in the seat frame: metres in front of and below the eyes, and its width.
        const float PanelDistanceM = 1.6f;
        const float PanelDropM = 0.10f;
        const float PanelWidthM = 1.8f;
        // Room below the window for drop-down lists that hang out of its bottom.
        const int DropdownMarginPx = 240;
        const float LaserWidthM = 0.004f;
        const float CursorSizeM = 0.025f;
        const int CaptureIntervalMs = 33;

        Thread FormThread;
        volatile MenuForm Form;
        readonly object Lock = new object();
        string[] PendingArguments;
        volatile bool FormClosed;
        bool Launched;

        // Capturing the window(s), on the window's own thread.
        System.Drawing.Size CaptureSize;
        System.Drawing.Point CaptureOrigin;   // of the window's client area, in the (Windows) screen; moves with the window
        System.Drawing.Bitmap CaptureBitmap;
        byte[] Pixels;                        // BGRA, CaptureSize, guarded by Lock
        int PixelsVersion;
        int UploadedVersion;
        System.Windows.Forms.Timer CaptureTimer;

        // Drawing, on the render thread.
        Texture2D PanelTexture;
        VRQuadPrimitive PanelQuad;
        VRTexturedMaterial PanelMaterial;
        VRTexturedMaterial LaserMaterial, LaserHitMaterial, CursorMaterial;
        VRQuadPrimitive CursorQuad;
        VRBoxPrimitive ControllerBox;
        VRTexturedMaterial ControllerMaterial;
        Matrix PanelMatrix;
        bool PacingSet;
        float PanelHeightM;

        // Pointing, on the updater thread.
        int PointerHand = VRSession.RightHand;
        bool TriggerWasDown;
        readonly bool[] MenuWasDown = new bool[VRSession.HandCount];
        double NextWheelTime;
        Vector2 LastPointerUV;

        internal override void Update(RenderFrame frame, double totalRealSeconds)
        {
            if (FormThread == null)
            {
                GameStateRunActivity.StartLogFile(Game.Settings, false);
                Trace.TraceInformation("Menu: the desktop menu is hosted in the game");
                StartFormThread();
            }

            string[] args;
            lock (Lock)
            {
                args = PendingArguments;
                PendingArguments = null;
            }
            if (args != null)
            {
                Start(args);
                return;
            }
            if (FormClosed && !Launched)
            {
                Game.Exit();
                return;
            }

            if (UserInput.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape))
                CloseForm();

            var display = Game.RenderProcess.DisplaySize;
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(50), (float)display.X / display.Y, 0.1f, 100);
            frame.SetView(Matrix.Identity, projection);

            var vr = Game.RenderProcess.VR;
            if (vr != null)
                PointWithHands(frame, vr, totalRealSeconds);

            var panelQuad = PanelQuad;
            if (panelQuad != null)
                frame.AddPrimitive(PanelMaterial, panelQuad, RenderPrimitiveGroup.World, ref PanelMatrix);

            base.Update(frame, totalRealSeconds);
        }

        #region Controllers
        void PointWithHands(RenderFrame frame, VRSession vr, double time)
        {
            var hands = new VRHand[VRSession.HandCount];
            for (var h = 0; h < hands.Length; h++)
                hands[h] = vr.Hand(h);

            // The hand that last pulled its trigger does the pointing; the other one just shows its beam.
            for (var h = 0; h < hands.Length; h++)
                if (hands[h].Active && hands[h].TriggerDown && !hands[PointerHand].TriggerDown)
                    PointerHand = h;
            for (var h = 0; h < hands.Length; h++)
            {
                if (hands[h].Enabled && hands[h].MenuButton && !MenuWasDown[h])
                    vr.RequestRecenter();
                MenuWasDown[h] = hands[h].MenuButton;
            }

            var panelCentre = new Vector3(0, -PanelDropM, -PanelDistanceM);
            for (var h = 0; h < hands.Length; h++)
            {
                var hand = hands[h];
                if (!hand.Active)
                    continue;
                // In the menu the seat frame is the world, so the hand's pose needs no conversion.
                var ray = VRPointer.RayFrom(hand.Pose);
                if (ControllerBox != null)
                {
                    var body = hand.Pose;
                    frame.AddPrimitive(ControllerMaterial, ControllerBox, RenderPrimitiveGroup.World, ref body);
                }
                var hit = Vector3.Zero;
                var uv = PanelQuad != null ? VRPointer.HitPanel(ray, panelCentre, PanelWidthM, PanelHeightM, out hit) : null;
                var end = uv != null ? hit : ray.Position + ray.Direction * 2;
                AddLaser(frame, ray.Position, end, uv != null && h == PointerHand);
                if (h != PointerHand)
                    continue;

                if (uv != null)
                {
                    AddCursor(frame, hit);
                    LastPointerUV = uv.Value;
                    SendMouse(uv.Value, 0);
                    if (Math.Abs(hand.Stick.Y) > 0.5f && time >= NextWheelTime)
                    {
                        SendMouse(uv.Value, Native.MouseWheel, hand.Stick.Y > 0 ? 120 : -120);
                        NextWheelTime = time + 0.15;
                    }
                }
                var down = hand.TriggerDown;
                if (down && !TriggerWasDown && uv != null)
                {
                    SendMouse(uv.Value, Native.MouseLeftDown);
                    vr.Vibrate(h, 0.03f);
                }
                else if (!down && TriggerWasDown)
                    SendMouse(LastPointerUV, Native.MouseLeftUp);
                TriggerWasDown = down;
            }
        }

        void AddLaser(RenderFrame frame, Vector3 from, Vector3 to, bool onPanel)
        {
            var material = onPanel ? LaserHitMaterial : LaserMaterial;
            if (material == null)
                return;
            var matrix = Matrix.Identity;
            frame.AddPrimitive(material, new VRLinePrimitive(from, to, LaserWidthM), RenderPrimitiveGroup.World, ref matrix);
        }

        void AddCursor(RenderFrame frame, Vector3 hit)
        {
            if (CursorQuad == null)
                return;
            var matrix = Matrix.CreateTranslation(hit + new Vector3(0, 0, 0.003f));
            frame.AddPrimitive(CursorMaterial, CursorQuad, RenderPrimitiveGroup.World, ref matrix);
        }

        /// <summary>Moves the (Windows) mouse to the point of the captured window the panel's (u, v) shows, with optional button or wheel flags.</summary>
        void SendMouse(Vector2 uv, uint flags, int wheel = 0)
        {
            System.Drawing.Point origin;
            lock (Lock)
                origin = CaptureOrigin;
            var x = origin.X + (int)(uv.X * CaptureSize.Width);
            var y = origin.Y + (int)(uv.Y * CaptureSize.Height);
            var screenX = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
            var screenY = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
            var screenWidth = Math.Max(1, Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN));
            var screenHeight = Math.Max(1, Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN));
            var input = new Native.INPUT
            {
                Type = Native.InputMouse,
                Mouse = new Native.MOUSEINPUT
                {
                    X = (int)((x - screenX) * 65535L / screenWidth),
                    Y = (int)((y - screenY) * 65535L / screenHeight),
                    Data = (uint)wheel,
                    Flags = Native.MouseMove | Native.MouseAbsolute | Native.MouseVirtualDesk | flags,
                },
            };
            Native.SendInput(1, new[] { input }, Marshal.SizeOf(typeof(Native.INPUT)));
        }
        #endregion

        #region The window
        void StartFormThread()
        {
            FormThread = new Thread(RunForm) { Name = "Menu Window", IsBackground = true };
            FormThread.SetApartmentState(ApartmentState.STA);
            FormThread.Start();
        }

        void RunForm()
        {
            try
            {
                using (var form = new MenuForm())
                {
                    // The window may be resized by the desktop once shown (tiling window managers do); capture
                    // the size it was designed with, plus room underneath for drop-down lists.
                    CaptureSize = new System.Drawing.Size(form.ClientSize.Width, form.ClientSize.Height + DropdownMarginPx);
                    form.Shown += (sender, e) => StartCapture(form);
                    Form = form;
                    var result = form.ShowDialog();
                    StopCapture();
                    if (result == DialogResult.OK)
                    {
                        var args = global::Menu.MenuLauncher.GetRunActivityArguments(form).ToArray();
                        lock (Lock)
                            PendingArguments = args;
                    }
                }
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
            Form = null;
            FormClosed = true;
        }

        void CloseForm()
        {
            var form = Form;
            if (form != null && !form.IsDisposed)
                form.BeginInvoke((Action)(() => { if (!form.IsDisposed) form.Close(); }));
        }

        void StartCapture(MenuForm form)
        {
            CaptureBitmap = new System.Drawing.Bitmap(CaptureSize.Width, CaptureSize.Height, PixelFormat.Format32bppArgb);
            lock (Lock)
                Pixels = new byte[CaptureSize.Width * CaptureSize.Height * 4];
            CaptureTimer = new System.Windows.Forms.Timer { Interval = CaptureIntervalMs };
            CaptureTimer.Tick += (sender, e) => Capture(form);
            CaptureTimer.Start();
            Trace.TraceInformation("Menu: showing the menu window as a panel ({0}x{1})", CaptureSize.Width, CaptureSize.Height);
        }

        void StopCapture()
        {
            if (CaptureTimer != null)
            {
                CaptureTimer.Stop();
                CaptureTimer.Dispose();
                CaptureTimer = null;
            }
        }

        /// <summary>
        /// Copies the window's client area and, on top of it, every other visible window of this thread
        /// (drop-down lists, dialogs, message boxes) into the capture, by their positions on screen.
        /// </summary>
        void Capture(MenuForm form)
        {
            if (form.IsDisposed || !form.Visible)
                return;
            var origin = form.PointToScreen(System.Drawing.Point.Empty);
            var windows = new List<IntPtr>();
            Native.EnumThreadWindows(Native.GetCurrentThreadId(), (hwnd, lParam) =>
            {
                if (Native.IsWindowVisible(hwnd))
                    windows.Add(hwnd);
                return true;
            }, IntPtr.Zero);

            using (var graphics = System.Drawing.Graphics.FromImage(CaptureBitmap))
            {
                graphics.Clear(System.Drawing.SystemColors.Control);
                var destination = graphics.GetHdc();
                try
                {
                    // Enumeration is top-most first; paint bottom-up so pop-ups land on top of the main window.
                    for (var i = windows.Count - 1; i >= 0; i--)
                    {
                        var hwnd = windows[i];
                        Native.RECT client;
                        if (!Native.GetClientRect(hwnd, out client))
                            continue;
                        var topLeft = new Native.POINT();
                        Native.ClientToScreen(hwnd, ref topLeft);
                        var x = topLeft.X - origin.X;
                        var y = topLeft.Y - origin.Y;
                        var width = Math.Min(client.Right - client.Left, CaptureSize.Width - x);
                        var height = Math.Min(client.Bottom - client.Top, CaptureSize.Height - y);
                        if (width <= 0 || height <= 0)
                            continue;
                        var source = Native.GetDC(hwnd);
                        if (source == IntPtr.Zero)
                            continue;
                        Native.BitBlt(destination, x, y, width, height, source, 0, 0, Native.SrcCopy);
                        Native.ReleaseDC(hwnd, source);
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(destination);
                }
            }

            var data = CaptureBitmap.LockBits(new System.Drawing.Rectangle(0, 0, CaptureSize.Width, CaptureSize.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                lock (Lock)
                {
                    var rowBytes = CaptureSize.Width * 4;
                    for (var row = 0; row < CaptureSize.Height; row++)
                        Marshal.Copy(data.Scan0 + row * data.Stride, Pixels, row * rowBytes, rowBytes);
                    CaptureOrigin = origin;
                    PixelsVersion++;
                }
            }
            finally
            {
                CaptureBitmap.UnlockBits(data);
            }
        }
        #endregion

        internal override void BeginRender(RenderFrame frame)
        {
            var graphicsDevice = Game.RenderProcess.GraphicsDevice;
            if (!PacingSet)
            {
                // The game starts at a fixed 10 FPS (for loading) and sleeps when its window is not focused, which
                // the menu window takes; a headset needs every frame, so run free until the run starts.
                PacingSet = true;
                Game.IsFixedTimeStep = false;
                Game.InactiveSleepTime = TimeSpan.Zero;
            }
            lock (Lock)
            {
                if (Pixels != null && PixelsVersion != UploadedVersion)
                {
                    if (PanelTexture == null)
                    {
                        PanelTexture = new Texture2D(graphicsDevice, CaptureSize.Width, CaptureSize.Height, false, SurfaceFormat.Bgra32);
                        PanelHeightM = PanelWidthM * CaptureSize.Height / CaptureSize.Width;
                        PanelMaterial = new VRTexturedMaterial(graphicsDevice, PanelTexture, false);
                        PanelMatrix = Matrix.CreateTranslation(0, -PanelDropM, -PanelDistanceM);
                        PanelQuad = new VRQuadPrimitive(graphicsDevice, PanelWidthM, PanelHeightM);
                    }
                    PanelTexture.SetData(Pixels);
                    UploadedVersion = PixelsVersion;
                }
            }
            if (LaserMaterial == null)
            {
                LaserMaterial = new VRTexturedMaterial(graphicsDevice, VRPointer.SolidTexture(graphicsDevice, Color.White), false);
                LaserHitMaterial = new VRTexturedMaterial(graphicsDevice, VRPointer.SolidTexture(graphicsDevice, Color.LightGreen), false);
                CursorMaterial = new VRTexturedMaterial(graphicsDevice, VRPointer.SolidTexture(graphicsDevice, Color.Red), false);
                CursorQuad = new VRQuadPrimitive(graphicsDevice, CursorSizeM, CursorSizeM);
                ControllerMaterial = new VRTexturedMaterial(graphicsDevice, VRPointer.SolidTexture(graphicsDevice, new Color(60, 60, 70)), false);
                ControllerBox = new VRBoxPrimitive(graphicsDevice, VRPointer.ControllerSize, VRPointer.ControllerOffset);
            }
            base.BeginRender(frame);
        }

        void Start(string[] args)
        {
            Launched = true;
            Trace.TraceInformation("Menu: starting RunActivity {0}", global::Menu.MenuLauncher.JoinArguments(args));
            // The menu may have changed the options (its own Options dialog); the run must see them as saved.
            var options = Environment.GetCommandLineArgs().Skip(1).Where(a => a.StartsWith("-") || a.StartsWith("/")).Select(a => a.Substring(1));
            Game.ReplaceSettings(new UserSettings(options));
            // Back to the loading-time pacing the run expects (see RenderProcess.Start), except that a headset
            // must not be starved while the game window is unfocused.
            Game.IsFixedTimeStep = true;
            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(100);
            Game.InactiveSleepTime = Game.RenderProcess.VR != null ? TimeSpan.Zero : TimeSpan.FromMilliseconds(100);
            Game.ReplaceState(new GameStateRunActivity(args));
        }

        internal override void Dispose()
        {
            CloseForm();
            base.Dispose();
        }

        static class Native
        {
            public delegate bool EnumThreadDelegate(IntPtr hwnd, IntPtr lParam);

            [DllImport("user32.dll")] public static extern bool EnumThreadWindows(uint threadId, EnumThreadDelegate callback, IntPtr lParam);
            [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
            [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
            [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);
            [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hwnd);
            [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
            [DllImport("user32.dll")] public static extern uint SendInput(uint count, [In] INPUT[] inputs, int size);
            [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
            [DllImport("gdi32.dll")] public static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint rop);
            [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();

            [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
            [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
            [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT { public int X, Y; public uint Data, Flags, Time; public IntPtr ExtraInfo; }
            // 40 bytes on x64: SendInput rejects any other size.
            [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint Type; public MOUSEINPUT Mouse; }

            public const uint InputMouse = 0;
            public const uint MouseMove = 0x0001, MouseLeftDown = 0x0002, MouseLeftUp = 0x0004, MouseWheel = 0x0800, MouseVirtualDesk = 0x4000, MouseAbsolute = 0x8000;
            public const int SM_XVIRTUALSCREEN = 76, SM_YVIRTUALSCREEN = 77, SM_CXVIRTUALSCREEN = 78, SM_CYVIRTUALSCREEN = 79;
            public const uint SrcCopy = 0x00CC0020;
        }
    }
}
