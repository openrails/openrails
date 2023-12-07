using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using ORTS.Common;
using Orts.Viewer3D;
using ORTS.Common.Input;
using Windows.Win32;
using Microsoft.Xna.Framework;

namespace ORTS.TrackViewer.UserInterface
{
    /// <summary>
    /// Interaction logic for SceneWindow.xaml
    /// </summary>
    public partial class SceneView : Window
    {
        readonly Stack<UndoDataSet> UndoStack = new Stack<UndoDataSet>();
        readonly Stack<UndoDataSet> RedoStack = new Stack<UndoDataSet>();
        public Viewer Viewer;
        OrbitingCamera Camera;

        EditorState EditorState;
        EditorMoveState EditorMoveState;
        StaticShape SelectedObject;
        WorldFile SelectedWorldFile;
        Orts.Formats.Msts.WorldObject SelectedWorldObject;
        StaticShape MovedObject;
        WorldPosition MovedObjectOriginalPosition;
        WorldPosition HandlePosition;
        WorldPosition HandleOriginalPosition;
        float DeltaX, DeltaY, DeltaZ;
        UndoDataSet DeltaContext;
        WorldLocation CursorLocation;
        readonly List<(int TileX, int TileZ)> FlaggedTiles = new List<(int, int)>();

        public SceneView(IntPtr hostWindow)
        {
            InitializeComponent();

            var hostWindowElement = new SceneViewerHwndHost(hostWindow);
            GraphicsHostElement.Children.Add(hostWindowElement);
        }

        public void Update(GameTime gameTime)
        {
            Camera = Camera ?? Viewer.OrbitingCamera;

            Viewer.EditorShapes.MouseCrosshairEnabled = true;

            UpdateViewUndoState();

            if (UserInput.IsPressed(UserCommand.EditorCancel))
            {
                ApplicationCommands.Stop.Execute(null, null);
            }

            if (EditorState == EditorState.Default || EditorState == EditorState.ObjectSelected)
            {
                if (UserInput.IsMouseLeftButtonPressed && UserInput.ModifiersMaskShiftCtrlAlt(false, false, false))
                {
                    if (Camera.PickByMouse(out var selectedObject))
                    {
                        SelectedObject = selectedObject;
                        SelectedObjectChanged();
                        EditorState = EditorState.ObjectSelected;
                    }
                }
            }
            if (EditorState == EditorState.HandleMoving)
            {
                if (UserInput.IsMouseLeftButtonPressed)
                {
                    ApplyHandleMove();
                }
            }
            if (EditorState == EditorState.ObjectMoving)
            {
                if (UserInput.IsMouseLeftButtonPressed)
                {
                    ApplyObjectMove();
                }
            }

            CursorLocation = Camera?.CameraWorldLocation ?? new WorldLocation();
            CursorLocation.Location = Viewer?.TerrainPoint ?? new Vector3();
            CursorLocation.Location.Z *= -1;
            CursorLocation.Normalize();
            FillCursorPositionStatus(CursorLocation);
            SetCameraLocationStatus(Camera?.CameraWorldLocation ?? new WorldLocation());

            // A second pass after user input handled, do the effective work
            if (EditorState == EditorState.ObjectMoving)
            {
                MovedObject.Location.XNAMatrix = GetMovingMatrix(MovedObjectOriginalPosition, HandleOriginalPosition, HandlePosition);
                Viewer.EditorShapes.MovedObject = MovedObject;
                Viewer.EditorShapes.MovedObjectLocation = MovedObject.Location;
            }
            else
            {
                Viewer.EditorShapes.MovedObject = null;
                Viewer.EditorShapes.MovedObjectLocation = null;
            }

            if (EditorState == EditorState.HandleMoving)
            {
                HandlePosition.XNAMatrix = GetMovingMatrix(HandleOriginalPosition);
                Viewer.EditorShapes.HandleLocation = HandlePosition;
            }

            FillDeltaStatus();
        }

        /// <summary>
        /// Put the mouse location in the statusbar
        /// </summary>
        /// <param name="mouseLocation"></param>
        void SetCameraLocationStatus(WorldLocation location)
        {
            tileXZ.Text = string.Format(CultureInfo.InvariantCulture, "{0,-7} {1,-7}", location.TileX, location.TileZ);
            LocationX.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", location.Location.X);
            LocationY.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", location.Location.Y);
            LocationZ.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", location.Location.Z);
        }

        void FillCursorPositionStatus(WorldLocation location)
        {
            tileXZcursor.Text = string.Format(CultureInfo.InvariantCulture, "{0,-7} {1,-7}", location.TileX, location.TileZ);
            LocationXcursor.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", location.Location.X);
            LocationYcursor.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", location.Location.Y);
            LocationZcursor.Text = string.Format(CultureInfo.InvariantCulture, "{0,3:F3} ", location.Location.Z);
        }

        void FillDeltaStatus()
        {
            //if (DeltaContext == null)
            {
                if (EditorState == EditorState.ObjectMoving)
                {
                    DeltaXBlock.Text = DeltaX.ToString("N3", CultureInfo.InvariantCulture);
                    DeltaYBlock.Text = DeltaY.ToString("N3", CultureInfo.InvariantCulture);
                    DeltaZBlock.Text = DeltaZ.ToString("N3", CultureInfo.InvariantCulture);
                }
            }
        }

        public async Task SetCameraLocation(WorldLocation worldLocation)
        {
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
                    worldLocation.TileX, worldLocation.TileZ, worldLocation.Location.X, worldLocation.Location.Z, true) ?? 0;
                break;
            }
            worldLocation.Location.Y = elevatedLocation + 50;
            Camera.SetLocation(worldLocation);

            var lastView = UndoStack.Count > 0 ?
                UndoStack.First(s => s.UndoEvent == UndoEvent.ViewChanged) :
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

        Matrix GetMovingMatrix(in WorldPosition originalPosition, in WorldPosition handleOriginalPosition = null, WorldPosition handlePosition = null)
        {
            var handle = handleOriginalPosition ?? originalPosition;
            var xnaMatrix = originalPosition.XNAMatrix;

            if (EditorMoveState == EditorMoveState.Rotate)
            {
                var distance = WorldLocation.GetDistance(handle.WorldLocation, CursorLocation);
                distance.Z *= -1;

                var angle = MathHelper.WrapAngle((float)(Math.Atan2(originalPosition.XNAMatrix.M13, originalPosition.XNAMatrix.M33) - Math.Atan2(distance.Z, distance.X)));
                var rotation = Matrix.CreateFromYawPitchRoll(angle, 0, 0);
                var translation = handle.XNAMatrix.Translation;
                xnaMatrix.Translation -= translation;
                xnaMatrix *= rotation;
                xnaMatrix.Translation += translation;

                if (handlePosition != null && handleOriginalPosition != null)
                {
                    angle = MathHelper.WrapAngle((float)(Math.Atan2(handleOriginalPosition.XNAMatrix.M13, handleOriginalPosition.XNAMatrix.M33) - Math.Atan2(distance.Z, distance.X)));
                    rotation = Matrix.CreateFromYawPitchRoll(angle, 0, 0);
                    var handleMatrix = handleOriginalPosition.XNAMatrix;
                    handleMatrix.Translation -= translation;
                    handleMatrix *= rotation;
                    handleMatrix.Translation += translation;
                    handlePosition.XNAMatrix = handleMatrix;
                }

                DeltaX = 0;
                DeltaY = MathHelper.ToDegrees(angle);
                DeltaZ = 0;
            }
            else if (EditorMoveState == EditorMoveState.Move)
            {
                var distance = WorldLocation.GetDistance(originalPosition.WorldLocation, CursorLocation);
                distance.Z *= -1;

                var axisX = Vector3.Normalize(handle.XNAMatrix.Right);
                var axisY = Vector3.Normalize(handle.XNAMatrix.Up);
                var axisZ = Vector3.Normalize(handle.XNAMatrix.Backward);

                var tileLocation = xnaMatrix.Translation;

                if (UserInput.IsDown(UserCommand.EditorLockOrthogonal))
                {
                    var distanceX = Vector3.Dot(axisX, distance);
                    var distanceZ = Vector3.Dot(axisZ, distance);

                    tileLocation += Math.Abs(distanceX) > Math.Abs(distanceZ) ? distanceX * axisX : distanceZ * axisZ;
                }
                else
                {
                    tileLocation.X += distance.X;
                    tileLocation.Z += distance.Z;
                }

                if (!UserInput.IsDown(UserCommand.EditorLockElevation))
                {
                    tileLocation.Y = Viewer.Tiles.GetElevation(handle.TileX, handle.TileZ, tileLocation.X, -tileLocation.Z);
                }
                xnaMatrix.Translation = tileLocation;

                distance = xnaMatrix.Translation - originalPosition.XNAMatrix.Translation;

                if (handlePosition != null && handleOriginalPosition != null)
                {
                    var handleMatrix = handleOriginalPosition.XNAMatrix;
                    handleMatrix.Translation += distance;
                    handlePosition.XNAMatrix = handleMatrix;
                }

                DeltaX = Vector3.Dot(axisX, distance);
                DeltaY = Vector3.Dot(axisY, distance);
                DeltaZ = Vector3.Dot(axisZ, distance);
            }
            return xnaMatrix;
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

        public void SetDefaultMode()
        {
            SelectedObject = null;
            SelectedObjectChanged();
            EditorState = EditorState.Default;
        }

        private void UndoCommand(object sender, ExecutedRoutedEventArgs e)
        {
            SetDefaultMode();
            if (UndoStack.Count > 1)
            {
                var undoDataSet = UndoStack.Pop();
                RedoStack.Push(undoDataSet);
                UndoRedo(undoDataSet, true);
            }
        }

        public void RedoCommand(object sender, ExecutedRoutedEventArgs e)
        {
            SetDefaultMode();
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
            else if (undoDataSet.UndoEvent == UndoEvent.WorldObjectChanged)
            {
                var newPosition = new WorldPosition(undoDataSet.ChangedStaticShape.Location);
                undoDataSet.ChangedStaticShape.Location.CopyFrom(undoDataSet.OldPosition);
                undoDataSet.OldPosition.CopyFrom(newPosition);
                var flag = (undoDataSet.ChangedStaticShape.Location.TileX, undoDataSet.ChangedStaticShape.Location.TileZ);
                if (!FlaggedTiles.Contains(flag))
                    FlaggedTiles.Add(flag);
            }
        }

        public void StartObjectMove()
        {
            MovedObject = SelectedObject;
            MovedObjectOriginalPosition = new WorldPosition(MovedObject.Location);
            if (HandlePosition != null)
                HandleOriginalPosition = new WorldPosition(HandlePosition);
            DeltaContext = null;
            EditorState = EditorState.ObjectMoving;
        }

        void CancelObjectMove()
        {
            MovedObject.Location.CopyFrom(MovedObjectOriginalPosition);
            MovedObject = null;
            EditorState = EditorState.ObjectSelected;
        }

        void ApplyObjectMove()
        {
            UndoStack.Push(new UndoDataSet()
            {
                UndoEvent = UndoEvent.WorldObjectChanged,
                TileX = MovedObject.Location.TileX,
                TileZ = MovedObject.Location.TileZ,
                Uid = MovedObject.Uid,
                ChangedStaticShape = MovedObject,
                OldPosition = new WorldPosition(MovedObjectOriginalPosition),
                MoveOrigin = HandlePosition,
            });
            RedoStack.Clear();

            DeltaContext = UndoStack.Peek();
            MovedObject = null;
            EditorState = EditorState.ObjectSelected;
        }

        public void StartHandleMove()
        {
            HandlePosition = new WorldPosition(SelectedObject.Location);
            HandleOriginalPosition = new WorldPosition(HandlePosition);
            DeltaContext = null;
            EditorState = EditorState.HandleMoving;
        }

        void CancelHandleMove()
        {
            HandlePosition = null;
            HandleOriginalPosition = null;
            EditorState = EditorState.ObjectSelected;
        }

        void ApplyHandleMove()
        {
            HandleOriginalPosition = new WorldPosition(HandlePosition);
            EditorState = EditorState.ObjectSelected;
        }

        void SelectedObjectChanged()
        {
            Viewer.EditorShapes.SelectedObject = SelectedObject;
            Viewer.EditorShapes.MovedObject = null;
            Viewer.EditorShapes.HandleLocation = null;
            HandlePosition = null;
            HandleOriginalPosition = null;

            SelectedWorldFile = Viewer.World.Scenery.WorldFiles.SingleOrDefault(w => w.TileX == SelectedObject?.Location.TileX && w.TileZ == SelectedObject?.Location.TileZ);
            SelectedWorldObject = SelectedWorldFile?.MstsWFile?.Tr_Worldfile?.SingleOrDefault(o => o.UID == SelectedObject?.Uid);

            // XAML binding doesn't work for fields (as opposed to properties), so doing it programmatically
            Filename.Text = SelectedObject != null ? System.IO.Path.GetFileName(SelectedObject.SharedShape.FilePath) : "";
            TileX.Text = SelectedObject?.Location.TileX.ToString(CultureInfo.InvariantCulture).Replace(",", "");
            TileZ.Text = SelectedObject?.Location.TileZ.ToString(CultureInfo.InvariantCulture).Replace(",", "");
            PosX.Text = SelectedObject?.Location.Location.X.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            PosY.Text = SelectedObject?.Location.Location.Y.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            PosZ.Text = SelectedObject?.Location.Location.Z.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            Uid.Text = SelectedObject?.Uid.ToString(CultureInfo.InvariantCulture).Replace(",", "");

            double yaw = 0, pitch = 0, roll = 0;
            if (SelectedWorldObject?.Matrix3x3 != null)
            {
                yaw = Math.Atan2(SelectedWorldObject.Matrix3x3.AZ, SelectedWorldObject.Matrix3x3.CZ);
                pitch = Math.Asin(-SelectedWorldObject.Matrix3x3.BZ);
                roll = Math.Atan2(SelectedWorldObject.Matrix3x3.BX, SelectedWorldObject.Matrix3x3.BY);
            }
            else if (SelectedWorldObject?.QDirection != null)
            {
                var x = SelectedWorldObject.QDirection.A;
                var y = SelectedWorldObject.QDirection.B;
                var z = SelectedWorldObject.QDirection.C;
                var w = SelectedWorldObject.QDirection.D;

                //yaw = Math.Atan2(y, w) * 2 / Math.PI * 180;
                yaw = Math.Atan2(2.0f * (y * w + x * z), 1.0f - 2.0f * (x * x + y * y)) / Math.PI * 180;
                pitch = Math.Asin(2.0f * (x * w - y * z)) / Math.PI * 180;
                roll = Math.Atan2(2.0f * (x * y + z * w), 1.0f - 2.0f * (x * x + z * z)) / Math.PI * 180;
            }
            RotX.Text = SelectedWorldObject == null ? "" : pitch.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            RotY.Text = SelectedWorldObject == null ? "" : yaw.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");
            RotZ.Text = SelectedWorldObject == null ? "" : roll.ToString("N3", CultureInfo.InvariantCulture).Replace(",", "");

            //if (SelectedObject is StaticShape ppp)
            //{
            //    var sb = new StringBuilder();
            //    var aaa = SelectedWorldFile?.MstsWFile?.Tr_Worldfile;
            //    aaa.Serialize(sb);
            //    var ccc = sb.ToString();
            //}
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void IntValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = int.TryParse(e.Text, out var _);
        }

        private void UndoRedoCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = EditorState == EditorState.Default || EditorState == EditorState.ObjectSelected;
        }

        private void CancelCommand(object sender, ExecutedRoutedEventArgs e)
        {
            if (EditorState == EditorState.Default || EditorState == EditorState.ObjectSelected)
                SetDefaultMode();
            else if (EditorState == EditorState.HandleMoving)
                CancelHandleMove();
            else if (EditorState == EditorState.ObjectMoving)
                CancelObjectMove();
        }

        private void RotateCommand(object sender, ExecutedRoutedEventArgs e)
        {
            EditorMoveState = EditorMoveState.Rotate;

            if (EditorState == EditorState.ObjectSelected)
                StartObjectMove();
        }

        private void MoveCommand(object sender, ExecutedRoutedEventArgs e)
        {
            EditorMoveState = EditorMoveState.Move;

            if (EditorState == EditorState.ObjectSelected)
                StartObjectMove();
        }

        private void MoveHandleCommand(object sender, ExecutedRoutedEventArgs e)
        {
            EditorMoveState = EditorMoveState.Move;

            if (EditorState == EditorState.ObjectSelected)
                StartHandleMove();
        }

        private void UintValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = uint.TryParse(e.Text, out var _);
        }

        private void FloatValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = float.TryParse(e.Text, out var _);
        }
    }

    public class UndoDataSet
    {
        public UndoEvent UndoEvent;

        public int TileX;
        public int TileZ;
        public int Uid;
        public StaticShape ChangedStaticShape;
        public WorldPosition OldPosition;
        public WorldPosition MoveOrigin;
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
        ObjectMoving,
        HandleMoving,
    }

    public enum EditorMoveState
    {
        Move,
        Rotate,
    }

    class SceneViewerHwndHost : HwndHost
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
