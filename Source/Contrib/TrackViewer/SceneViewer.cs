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

namespace ORTS.TrackViewer
{
    public class SceneViewer
    {
        public static GettextResourceManager Catalog;

        public SceneWindow SceneWindow;
        public GameWindow SwapChainWindow;
        TrackViewer TrackViewer;
        SwapChainRenderTarget SwapChain;

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
            //SceneWindow = new SceneWindow(new SceneViewerVisualHost(GameWindow));

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

            Microsoft.Xna.Framework.Input.Mouse.WindowHandle = SwapChainWindow.Handle;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// relation ontent.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
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
            SetCameraLocationStatus(TrackViewer.RenderProcess?.Viewer?.Camera?.CameraWorldLocation ?? new WorldLocation());
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Draw(GameTime gameTime)
        {
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

        public async Task SetCameraLocation()
        {
            var mouseLocation = TrackViewer.drawLabels.SetLocationMenuItem.CommandParameter as WorldLocation? ?? new WorldLocation();
            var elevatedLocation = 0f;
            var i = 0;
            while (true)
            {
                if (TrackViewer.RenderProcess.Viewer?.Tiles == null)
                {
                    if (i > 50)
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

    public class SceneViewerVisualHost : UIElement
    {
        System.Windows.Media.Visual Visual;

        public SceneViewerVisualHost(GameWindow gameWindow)
        {
            Visual = HwndSource.FromHwnd(gameWindow.Handle).RootVisual;
        }

        protected override int VisualChildrenCount { get { return Visual != null ? 1 : 0; } }

        protected override System.Windows.Media.Visual GetVisualChild(int index)
        {
            return Visual;
        }
    }
}
