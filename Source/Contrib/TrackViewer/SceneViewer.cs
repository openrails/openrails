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
        public readonly TrackViewer Game;
        public StaticShape SelectedObject;
        public WorldFile SelectedWorldFile;
        public Orts.Formats.Msts.WorldObject SelectedWorldObject;
        Viewer Viewer;
        OrbitingCamera Camera;
        Stack<UndoDataSet> UndoStack = new Stack<UndoDataSet>();
        Stack<UndoDataSet> RedoStack = new Stack<UndoDataSet>();
        EditorState EditorState;

        /// <summary>The command-line arguments</summary>
        private string[] CommandLineArgs;

        /// <summary>
        /// Constructor. This is where it all starts.
        /// </summary>
        public SceneViewer(TrackViewer trackViewer, string[] args)
        {
            CommandLineArgs = args;

            Game = trackViewer;

            // Inject the secondary window into RunActivity
            Game.SwapChainWindow = GameWindow.Create(Game,
                Game.GraphicsDevice.PresentationParameters.BackBufferWidth,
                Game.GraphicsDevice.PresentationParameters.BackBufferHeight);

            RenderFrame.FinalRenderTarget = new SwapChainRenderTarget(Game.GraphicsDevice,
                Game.SwapChainWindow.Handle,
                Game.GraphicsDevice.PresentationParameters.BackBufferWidth,
                Game.GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                Game.GraphicsDevice.PresentationParameters.BackBufferFormat,
                Game.GraphicsDevice.PresentationParameters.DepthStencilFormat,
                1,
                RenderTargetUsage.PlatformContents,
                PresentInterval.Two);

            SceneWindow = new SceneWindow(new SceneViewerHwndHost(Game.SwapChainWindow.Handle));

            // The primary window activation events should not affect RunActivity
            Game.Activated -= Game.ActivateRunActivity;
            Game.Deactivated -= Game.DeactivateRunActivity;

            // The secondary window activation events should affect RunActivity
            SceneWindow.Activated += Game.ActivateRunActivity;
            SceneWindow.Activated += new System.EventHandler((sender, e) => SetKeyboardInput(true));
            SceneWindow.Deactivated += Game.DeactivateRunActivity;
            SceneWindow.Deactivated += new System.EventHandler((sender, e) => SetKeyboardInput(false));

            SceneWindow.DataContext = this;

            Game.ReplaceState(new GameStateRunActivity(new[] { "-start", "-viewer", Game.CurrentRoute.Path + "\\dummy\\.pat", "", "10:00", "1", "0" }));
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Update(GameTime gameTime)
        {
            Viewer = Viewer ?? Game.RenderProcess?.Viewer;
            if (Viewer == null)
                return;
            Camera = Camera ?? Viewer.OrbitingCamera;

            Viewer.EditorShapes.MouseCrosshairEnabled = true;

            UpdateViewUndoState();

            if (UserInput.IsMouseLeftButtonPressed && UserInput.ModifiersMaskShiftCtrlAlt(false, false, false))
            {
                if (Camera.PickByMouse(out var selectedObject))
                {
                    SelectedObject = selectedObject;
                    SelectedObjectChanged();
                    EditorState = EditorState.ObjectSelected;
                }
            }
            if (UserInput.IsPressed(UserCommand.EditorUnselectAll))
            {
                if (EditorState == EditorState.ObjectSelected || EditorState == EditorState.Default)
                {
                    SetDefaultMode();
                }
            }
            if (UserInput.IsPressed(UserCommand.EditorUndo))
            {
                if (EditorState == EditorState.ObjectSelected || EditorState == EditorState.Default)
                {
                    SetDefaultMode();
                    UndoCommand();
                }
            }
            if (UserInput.IsPressed(UserCommand.EditorRedo))
            {
                SetDefaultMode();
                RedoCommand();
            }

            SetCameraLocationStatus(Camera?.CameraWorldLocation ?? new WorldLocation());
            //FillCursorPositionStatus(Viewer?.TerrainPoint ?? new Vector3());
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
            SceneWindow.tileXZ.Text = string.Format(CultureInfo.InvariantCulture, "{0,-7} {1,-7}", cameraLocation.TileX, cameraLocation.TileZ);
            SceneWindow.LocationX.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", cameraLocation.Location.X);
            SceneWindow.LocationY.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", cameraLocation.Location.Y);
            SceneWindow.LocationZ.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", cameraLocation.Location.Z);
        }

        private void FillCursorPositionStatus(Vector3 cursorPosition)
        {
            SceneWindow.LocationX.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", cursorPosition.X);
            SceneWindow.LocationY.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", cursorPosition.Y);
            SceneWindow.LocationZ.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", -cursorPosition.Z);
        }

        public async Task SetCameraLocation()
        {
            var mouseLocation = Game.drawLabels.SetLocationMenuItem.CommandParameter as WorldLocation? ?? new WorldLocation();
            var elevatedLocation = 0f;
            var i = 0;
            while (true)
            {
                if (Viewer?.Tiles == null || Viewer?.Camera == null)
                {
                    if (i > 300)
                        return;
                    await Task.Delay(100);
                    i++;
                    continue;
                }
                elevatedLocation = Viewer.Tiles?.LoadAndGetElevation(
                    mouseLocation.TileX, mouseLocation.TileZ, mouseLocation.Location.X, mouseLocation.Location.Z, true) ?? 0;
                break;
            }
            mouseLocation.Location.Y = elevatedLocation + 15;
            Camera.SetLocation(mouseLocation);

            var lastView = UndoStack.Count > 0 ? UndoStack.First(s => s.UndoEvent == UndoEvent.ViewChanged) :
                new UndoDataSet()
                {
                    NewCameraLocation = Camera.CameraWorldLocation,
                    NewCameraRotationXRadians = Camera.GetRotationX(),
                    NewCameraRotationYRadians = Camera.GetRotationY(),
                };

            UndoStack.Push(new UndoDataSet()
            {
                UndoEvent = UndoEvent.ViewChanged,
                NewCameraLocation = Camera.CameraWorldLocation,
                NewCameraRotationXRadians = Camera.GetRotationX(),
                NewCameraRotationYRadians = Camera.GetRotationY(),
                OldCameraLocation = lastView.NewCameraLocation,
                OldCameraRotationXRadians = lastView.NewCameraRotationXRadians,
                OldCameraRotationYRadians = lastView.NewCameraRotationYRadians,
            });
        }

        void UpdateViewUndoState()
        {
            if (UndoStack.Count == 0)
                return;
            
            var lastView = UndoStack.First(s => s.UndoEvent == UndoEvent.ViewChanged);

            if (Camera.GetRotationX() == lastView.NewCameraRotationXRadians && Camera.GetRotationY() == lastView.NewCameraRotationYRadians && Camera.CameraWorldLocation == lastView.NewCameraLocation)
                return;

            if (UndoStack.First().UndoEvent == UndoEvent.ViewChanged) // then updatable
            {
                if ((Camera.GetRotationX() == lastView.NewCameraRotationXRadians && Camera.GetRotationY() == lastView.NewCameraRotationYRadians) ^
                    (lastView.NewCameraRotationXRadians != lastView.OldCameraRotationXRadians || lastView.NewCameraRotationYRadians != lastView.OldCameraRotationYRadians))
                {
                    // Group rotations and pan-zooms by just updating the last action
                    lastView.NewCameraRotationXRadians = Camera.GetRotationX();
                    lastView.NewCameraRotationYRadians = Camera.GetRotationY();
                    lastView.NewCameraLocation = Camera.CameraWorldLocation;
                    RedoStack.Clear();
                    return;
                }
            }
            if (Camera.GetRotationX() != lastView.NewCameraRotationXRadians || Camera.GetRotationY() != lastView.NewCameraRotationYRadians || Camera.CameraWorldLocation != lastView.NewCameraLocation)
            {
                UndoStack.Push(new UndoDataSet()
                {
                    UndoEvent = UndoEvent.ViewChanged,
                    NewCameraLocation = Camera.CameraWorldLocation,
                    NewCameraRotationXRadians = Camera.GetRotationX(),
                    NewCameraRotationYRadians = Camera.GetRotationY(),
                    OldCameraLocation = lastView.NewCameraLocation,
                    OldCameraRotationXRadians = lastView.NewCameraRotationXRadians,
                    OldCameraRotationYRadians = lastView.NewCameraRotationYRadians,
                });
                RedoStack.Clear();
            }
        }

        void SetDefaultMode()
        {
            SelectedObject = null;
            SelectedObjectChanged();
            EditorState = EditorState.Default;
        }

        void UndoCommand()
        {
            if (UndoStack.Count > 1)
            {
                var undoDataSet = UndoStack.Pop();
                RedoStack.Push(undoDataSet);
                UndoRedo(undoDataSet, true);
            }
        }

        void RedoCommand()
        {
            if (RedoStack.Count > 0)
            {
                var undoDataSet = RedoStack.Pop();
                UndoStack.Push(undoDataSet);
                UndoRedo(undoDataSet, false);
            }
        }

        void UndoRedo(UndoDataSet undoDataSet, bool undo)
        {
            if (undoDataSet.UndoEvent == UndoEvent.ViewChanged)
            {
                Camera.SetLocation(undo ? undoDataSet.OldCameraLocation : undoDataSet.NewCameraLocation);
                Camera.SetRotation(
                    undo ? undoDataSet.OldCameraRotationXRadians : undoDataSet.NewCameraRotationXRadians,
                    undo ? undoDataSet.OldCameraRotationYRadians : undoDataSet.NewCameraRotationYRadians);
            }
        }

        void SelectedObjectChanged()
        {
            Viewer.EditorShapes.SelectedObject = SelectedObject;

            SelectedWorldFile = Viewer.World.Scenery.WorldFiles.SingleOrDefault(w => w.TileX == SelectedObject?.Location.TileX && w.TileZ == SelectedObject?.Location.TileZ);
            SelectedWorldObject = SelectedWorldFile?.MstsWFile?.Tr_Worldfile?.SingleOrDefault(o => o.UID == SelectedObject?.Uid);

            // XAML binding doesn't work for fields (as opposed to properties), so doing it programmatically
            SceneWindow.Filename.Text = SelectedObject != null ? System.IO.Path.GetFileName(SelectedObject.SharedShape.FilePath) : "";
            SceneWindow.TileX.Text = SelectedObject?.Location.TileX.ToString(CultureInfo.InvariantCulture).Replace(",", "");
            SceneWindow.TileZ.Text = SelectedObject?.Location.TileZ.ToString(CultureInfo.InvariantCulture).Replace(",", "");
            SceneWindow.PosX.Text = SelectedObject?.Location.Location.X.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            SceneWindow.PosY.Text = SelectedObject?.Location.Location.Y.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            SceneWindow.PosZ.Text = SelectedObject?.Location.Location.Z.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            SceneWindow.Uid.Text = SelectedObject?.Uid.ToString(CultureInfo.InvariantCulture).Replace(",", "");

            if (SelectedWorldObject?.Matrix3x3 != null)
            {
                var yaw = (float)Math.Atan2(SelectedWorldObject.Matrix3x3.AZ, SelectedWorldObject.Matrix3x3.CZ);
                var pitch = (float)Math.Asin(-SelectedWorldObject.Matrix3x3.BZ);
                var roll = (float)Math.Atan2(SelectedWorldObject.Matrix3x3.BX, SelectedWorldObject.Matrix3x3.BY);
                SceneWindow.RotX.Text = pitch.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
                SceneWindow.RotY.Text = yaw.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
                SceneWindow.RotZ.Text = roll.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            }
            else if (SelectedWorldObject?.QDirection != null)
            {
                var x = SelectedWorldObject.QDirection.A;
                var y = SelectedWorldObject.QDirection.B;
                var z = SelectedWorldObject.QDirection.C;
                var w = SelectedWorldObject.QDirection.D;

                //var yaw = Math.Atan2(y, w) * 2 / Math.PI * 180;
                var yaw = Math.Atan2(2.0f * (y * w + x * z), 1.0f - 2.0f * (x * x + y * y)) / Math.PI * 180;
                var pitch = Math.Asin(2.0f * (x * w - y * z)) / Math.PI * 180;
                var roll = Math.Atan2(2.0f * (x * y + z * w), 1.0f - 2.0f * (x * x + z * z)) / Math.PI * 180;
                SceneWindow.RotX.Text = pitch.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
                SceneWindow.RotY.Text = yaw.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
                SceneWindow.RotZ.Text = roll.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            }
            else
            {
                SceneWindow.RotX.Text = "";
                SceneWindow.RotY.Text = "";
                SceneWindow.RotZ.Text = "";
            }

            //if (SelectedObject is StaticShape ppp)
            //{
            //    var sb = new StringBuilder();
            //    var aaa = SelectedWorldFile?.MstsWFile?.Tr_Worldfile;
            //    aaa.Serialize(sb);
            //    var ccc = sb.ToString();
            //}
        }
    }

    public class UndoDataSet
    {
        public UndoEvent UndoEvent;

        public int TileX;
        public int TileZ;
        public int Uid;
        public Orts.Formats.Msts.WorldObject OldWorldObject;
        public Orts.Formats.Msts.WorldObject NewWorldObject;

        public WorldLocation OldCameraLocation;
        public float OldCameraRotationXRadians;
        public float OldCameraRotationYRadians;

        public WorldLocation NewCameraLocation;
        public float NewCameraRotationXRadians;
        public float NewCameraRotationYRadians;
    }

    public enum UndoEvent
    {
        ViewChanged,
        WorldObjectChanged,
    }

    public enum EditorState
    {
        Default = 0,
        ObjectSelected,
        ObjectMovingX,
        ObjectMovingY,
        ObjectMovingZ,
    }

    public class SceneViewerHwndHost : HwndHost
    {
        readonly IntPtr HwndChildHandle;

        public SceneViewerHwndHost(IntPtr hwndChildHandle)
        {
            HwndChildHandle = hwndChildHandle;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var style = (int)(Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_CHILD |
                              Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_BORDER |
                              Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_CLIPCHILDREN |
                              Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_VISIBLE |
                              Windows.Win32.UI.WindowsAndMessaging.WINDOW_STYLE.WS_MAXIMIZE);

            var child = new Windows.Win32.Foundation.HWND(HwndChildHandle);
            var parent = new Windows.Win32.Foundation.HWND(hwndParent.Handle);

            PInvoke.SetWindowLong(child, Windows.Win32.UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX.GWL_STYLE, style);
            PInvoke.SetParent(child, parent);
            
            return new HandleRef(this, HwndChildHandle);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
        }
    }
}
