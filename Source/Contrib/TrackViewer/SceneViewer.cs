// COPYRIGHT 2023 by the Open Rails project.
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Windows.Win32;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GNU.Gettext;
using ORTS.Menu;
using ORTS.Common;
using Orts.Common;
using ORTS.TrackViewer.UserInterface;
using Orts.Viewer3D;
using Orts.Viewer3D.Processes;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using ORTS.Common.Input;

namespace ORTS.TrackViewer
{
    public class SceneViewer
    {
        public static GettextResourceManager Catalog;

        public SceneWindow SceneWindow;
        public GameWindow SwapChainWindow;
        public readonly TrackViewer TrackViewer;
        SwapChainRenderTarget SwapChain;
        internal StaticShape SelectedObject;
        internal Orts.Formats.Msts.WorldObject SelectedWorldObject;
        Viewer Viewer;
        ViewerCamera Camera;

        /// <summary>The command-line arguments</summary>
        private string[] CommandLineArgs;

        /// <summary>
        /// Constructor. This is where it all starts.
        /// </summary>
        public SceneViewer(TrackViewer trackViewer, string[] args)
        {
            CommandLineArgs = args;

            TrackViewer = trackViewer;
            SwapChainWindow = GameWindow.Create(TrackViewer,
                TrackViewer.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferWidth,
                TrackViewer.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferHeight);
            SwapChainWindow.Title = "SceneViewer";

            // The RunActivity.Game class can be accessed as "TrackViewer" from here because of the inheritance
            SwapChain = new SwapChainRenderTarget(TrackViewer.GraphicsDevice,
                SwapChainWindow.Handle,
                TrackViewer.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferWidth,
                TrackViewer.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                TrackViewer.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferFormat,
                TrackViewer.RenderProcess.GraphicsDevice.PresentationParameters.DepthStencilFormat,
                1,
                RenderTargetUsage.PlatformContents,
                PresentInterval.Two);

            // Inject the secondary window into RunActivity
            TrackViewer.SwapChainWindow = SwapChainWindow;

            RenderFrame.FinalRenderTarget = SwapChain;

            SceneWindow = new SceneWindow(new SceneViewerHwndHost(SwapChainWindow));

            // The primary window activation events should not affect RunActivity
            TrackViewer.Activated -= TrackViewer.ActivateRunActivity;
            TrackViewer.Deactivated -= TrackViewer.DeactivateRunActivity;

            // The secondary window activation events should affect RunActivity
            SceneWindow.Activated += TrackViewer.ActivateRunActivity;
            SceneWindow.Activated += new System.EventHandler((sender, e) => SetKeyboardInput(true));
            SceneWindow.Deactivated += TrackViewer.DeactivateRunActivity;
            SceneWindow.Deactivated += new System.EventHandler((sender, e) => SetKeyboardInput(false));

            // Not "running" this Game, so manual init is needed
            Initialize();
            LoadContent();
        }

        public SceneViewer() { }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// relation ontent.
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        public void LoadContent()
        {
            TrackViewer.ReplaceState(new GameStateRunActivity(new[] { "-start", "-viewer", TrackViewer.CurrentRoute.Path + "\\dummy\\.pat", "", "10:00", "1", "0" }));
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Update(GameTime gameTime)
        {
            Viewer = Viewer ?? TrackViewer.RenderProcess?.Viewer;
            if (Viewer == null)
                return;
            Camera = Camera ?? Viewer.ViewerCamera;

            Viewer.EditorShapes.MouseCrosshairEnabled = true;

            if (UserInput.IsMouseLeftButtonPressed && UserInput.ModifiersMaskShiftCtrlAlt(false, false, false)
                && Camera.PickByMouse(out SelectedObject))
            {
                SelectedObjectChanged();
            }
            if (UserInput.IsMouseMiddleButtonPressed && UserInput.ModifiersMaskShiftCtrlAlt(false, false, false))
            {
                Camera.StoreRotationOrigin(Viewer.TerrainPoint);
                Viewer.EditorShapes.CrosshairPositionUpdateEnabled = false;
            }
            if (UserInput.IsMouseMiddleButtonDown && UserInput.ModifiersMaskShiftCtrlAlt(false, false, false))
            {
                Camera.RotateByMouse();
            }
            else
            {
                Viewer.EditorShapes.CrosshairPositionUpdateEnabled = true;
            }
            if (UserInput.IsMouseMiddleButtonDown && UserInput.ModifiersMaskShiftCtrlAlt(true, false, false))
            {
                Camera.PanByMouse();
            }
            else
            {
                Camera.ZoomByMouseWheel(1);
            }
            
            if (UserInput.IsPressed(UserCommand.EditorUnselectAll))
            {
                SelectedObject = null;
                SelectedObjectChanged();
            }

            SetCameraLocationStatus(TrackViewer.RenderProcess?.Viewer?.Camera?.CameraWorldLocation ?? new WorldLocation());
            //FillCursorPositionStatus(TrackViewer.RenderProcess?.Viewer?.TerrainPoint ?? new Vector3());
        }

        public void EndDraw()
        {
            SwapChain.Present();
        }

        /// <summary>
        /// A workaround for a MonoGame bug where the <see cref="Microsoft.Xna.Framework.Input.Keyboard.GetState()" />
        /// doesn't return the valid keyboard state. Needs to be enabled via reflection in a private method.
        /// </summary>
        public void SetKeyboardInput(bool enable)
        {
            var keyboardType = typeof(Microsoft.Xna.Framework.Input.Keyboard);
            var methodInfo = keyboardType.GetMethod("SetActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            methodInfo.Invoke(null, new object[] { enable });
        }

        /// <summary>
        /// Put the mouse location in the statusbar
        /// </summary>
        /// <param name="mouseLocation"></param>
        private void SetCameraLocationStatus(WorldLocation cameraLocation)
        {
            SceneWindow.tileXZ.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0,-7} {1,-7}", cameraLocation.TileX, cameraLocation.TileZ);
            SceneWindow.LocationX.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0,3:F3} ", cameraLocation.Location.X);
            SceneWindow.LocationY.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0,3:F3} ", cameraLocation.Location.Y);
            SceneWindow.LocationZ.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0,3:F3} ", cameraLocation.Location.Z);
        }

        private void FillCursorPositionStatus(Vector3 cursorPosition)
        {
            SceneWindow.LocationX.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", cursorPosition.X);
            SceneWindow.LocationY.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", cursorPosition.Y);
            SceneWindow.LocationZ.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", -cursorPosition.Z);
        }

        public async Task SetCameraLocation()
        {
            var mouseLocation = TrackViewer.drawLabels.SetLocationMenuItem.CommandParameter as WorldLocation? ?? new WorldLocation();
            var elevatedLocation = 0f;
            var i = 0;
            while (true)
            {
                if (TrackViewer.RenderProcess.Viewer?.Tiles == null)
                {
                    if (i > 300)
                        return;
                    await Task.Delay(100);
                    i++;
                    continue;
                }
                elevatedLocation = TrackViewer.RenderProcess.Viewer.Tiles?.LoadAndGetElevation(
                    mouseLocation.TileX, mouseLocation.TileZ, mouseLocation.Location.X, mouseLocation.Location.Z, true) ?? 0;
                break;
            }
            mouseLocation.Location.Y = elevatedLocation + 15;
            TrackViewer.RenderProcess.Viewer.ViewerCamera.SetLocation(mouseLocation);
        }

        void SelectedObjectChanged()
        {
            Viewer.EditorShapes.SelectedObject = SelectedObject;

            SelectedWorldObject = Viewer.World.Scenery.WorldFiles
                .SingleOrDefault(w => w.TileX == SelectedObject?.Location.TileX && w.TileZ == SelectedObject?.Location.TileZ)
                ?.MstsWFile?.Tr_Worldfile
                ?.SingleOrDefault(o => o.UID == SelectedObject?.Uid);

            //SceneWindow.Filename.Text = SelectedObject != null ? System.IO.Path.GetFileName(SelectedObject.SharedShape.FilePath) : "";
            //SceneWindow.TileX.Text = SelectedObject?.Location.TileX.ToString(CultureInfo.InvariantCulture).Replace(",", "");
            //SceneWindow.TileZ.Text = SelectedObject?.Location.TileZ.ToString(CultureInfo.InvariantCulture).Replace(",", "");
            //SceneWindow.PosX.Text = SelectedObject?.Location.Location.X.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            //SceneWindow.PosY.Text = SelectedObject?.Location.Location.Y.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            //SceneWindow.PosZ.Text = SelectedObject?.Location.Location.Z.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            //SceneWindow.Uid.Text = SelectedObject.Uid.ToString(CultureInfo.InvariantCulture).Replace(",", "");
            var q = new Quaternion();
            if (SelectedObject?.Location.XNAMatrix.Decompose(out var _, out q, out var _) ?? false)
            {
                var mag = Math.Sqrt(q.W * q.W + q.Y * q.Y);
                var w = q.W / mag;
                var ang = 2.0 * Math.Acos(w) / Math.PI * 180;
                SceneWindow.RotY.Text = ang.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            }
            else
            {
                SceneWindow.RotY.Text = "";
            }

            if (SelectedObject is StaticShape ppp)
            {
                var sb = new StringBuilder();
                var aaa = Viewer.World.Scenery.WorldFiles
                    .SingleOrDefault(w => w.TileX == SelectedObject.Location.TileX && w.TileZ == SelectedObject.Location.TileZ)
                    ?.MstsWFile?.Tr_Worldfile;
                aaa.Serialize(sb);
                var ccc = sb.ToString();
            }
        }

    }

    public class SceneViewerHwndHost : HwndHost
    {
        public readonly GameWindow GameWindow;

        public SceneViewerHwndHost(GameWindow gameWindow)
        {
            GameWindow = gameWindow;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var style = (int)(Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_CHILD |
                              Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_BORDER |
                              Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_CLIPCHILDREN |
                              Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_VISIBLE |
                              Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_MAXIMIZE);

            var child = new Windows.Win32.Foundation.HWND(GameWindow.Handle);
            var parent = new Windows.Win32.Foundation.HWND(hwndParent.Handle);

            PInvoke.SetWindowLong(child, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE, style);
            PInvoke.SetParent(child, parent);
            
            return new HandleRef(this, GameWindow.Handle);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
        }
    }
}
