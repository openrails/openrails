// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using Orts.Viewer3D;
using Orts.Viewer3D.Popups;
using ORTS.Common;
using ORTS.Common.Input;
using ORTS.Settings;

namespace Orts.Viewer3D
{
    public abstract class Camera
    {
        protected double CommandStartTime;

        // 2.1 sets the limit at just under a right angle as get unwanted swivel at the full right angle.
        protected static CameraAngleClamper VerticalClamper = new CameraAngleClamper(-MathHelper.Pi / 2.1f, MathHelper.Pi / 2.1f);
        public const int TerrainAltitudeMargin = 2;

        protected readonly Viewer Viewer;

        protected WorldLocation cameraLocation = new WorldLocation();
        public int TileX { get { return cameraLocation.TileX; } }
        public int TileZ { get { return cameraLocation.TileZ; } }
        public Vector3 Location { get { return cameraLocation.Location; } }
        public WorldLocation CameraWorldLocation { get { return cameraLocation; } }
        protected int MouseScrollValue;
        protected internal float FieldOfView;

        protected Matrix xnaView;
        public Matrix XnaView { get { return xnaView; } }

        Matrix xnaProjection;
        public Matrix XnaProjection { get { return xnaProjection; } }
        public static Matrix XnaDistantMountainProjection;
        Vector3 frustumRightProjected;
        Vector3 frustumLeft;
        Vector3 frustumRight;

        // This sucks. It's really not camera-related at all.
        public static Matrix XNASkyProjection;

        // The following group of properties are used by other code to vary
        // behavior by camera; e.g. Style is used for activating sounds,
        // AttachedCar for rendering the train or not, and IsUnderground for
        // automatically switching to/from cab view in tunnels.
        public enum Styles { External, Cab, Passenger, ThreeDimCab }
        public virtual Styles Style { get { return Styles.External; } }
        public virtual TrainCar AttachedCar { get { return null; } }
        public virtual bool IsAvailable { get { return true; } }
        public virtual bool IsUnderground { get { return false; } }
        public virtual string Name { get { return ""; } }

        // We need to allow different cameras to have different near planes.
        public virtual float NearPlane { get { return 1.0f; } }

        public float ReplaySpeed { get; set; }
        const int SpeedFactorFastSlow = 8;  // Use by GetSpeed
        protected const float SpeedAdjustmentForRotation = 0.1f;

        protected Camera(Viewer viewer)
        {
            Viewer = viewer;
            FieldOfView = Viewer.Settings.ViewingFOV;
        }

        protected Camera(Viewer viewer, Camera previousCamera) // maintain visual continuity
            : this(viewer)
        {
            if (previousCamera != null)
            {
                cameraLocation = previousCamera.CameraWorldLocation;
                FieldOfView = previousCamera.FieldOfView;
            }
        }

        [CallOnThread("Updater")]
        protected internal virtual void Save(BinaryWriter output)
        {
            cameraLocation.Save(output);
            output.Write(FieldOfView);
        }

        [CallOnThread("Render")]
        protected internal virtual void Restore(BinaryReader input)
        {
            cameraLocation.Restore(input);
            FieldOfView = input.ReadSingle();
        }

        /// <summary>
        /// Resets a camera's position, location and attachment information.
        /// </summary>
        public virtual void Reset()
        {
            FieldOfView = Viewer.Settings.ViewingFOV;
            ScreenChanged();
        }

        /// <summary>
        /// Switches the <see cref="Viewer3D"/> to this camera, updating the view information.
        /// </summary>
        public void Activate()
        {
            ScreenChanged();
            if (!Viewer.IsFormationReversed)// Avoids flickering
            {
                OnActivate(Viewer.Camera == this);
            }
            Viewer.Camera = this;
            Viewer.Simulator.PlayerIsInCab = Style == Styles.Cab || Style == Styles.ThreeDimCab;
            Update(ElapsedTime.Zero);
            xnaView = GetCameraView();
            SoundBaseTile = new Point(cameraLocation.TileX, cameraLocation.TileZ);
        }

        /// <summary>
        /// A camera can use this method to handle any preparation when being activated.
        /// </summary>
        protected virtual void OnActivate(bool sameCamera)
        {
        }

        /// <summary>
        /// A camera can use this method to respond to user input.
        /// </summary>
        /// <param name="elapsedTime"></param>
        public virtual void HandleUserInput(ElapsedTime elapsedTime)
        {
        }

        /// <summary>
        /// A camera can use this method to update any calculated data that may have changed.
        /// </summary>
        /// <param name="elapsedTime"></param>
        public virtual void Update(ElapsedTime elapsedTime)
        {
        }

        /// <summary>
        /// A camera should use this method to return a unique view.
        /// </summary>
        protected abstract Matrix GetCameraView();

        /// <summary>
        /// Notifies the camera that the screen dimensions have changed.
        /// </summary>
        public void ScreenChanged()
        {
            var aspectRatio = (float)Viewer.DisplaySize.X / Viewer.DisplaySize.Y;
            var farPlaneDistance = SkyPrimitive.RadiusM + 100;  // so far the sky is the biggest object in view
            var fovWidthRadians = MathHelper.ToRadians(FieldOfView);
            if (Viewer.Settings.DistantMountains)
                XnaDistantMountainProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, MathHelper.Clamp(Viewer.Settings.ViewingDistance - 500, 500, 1500), Viewer.Settings.DistantMountainsViewingDistance);
            xnaProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, NearPlane, Viewer.Settings.ViewingDistance);
            XNASkyProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, NearPlane, farPlaneDistance);    // TODO remove? 
            frustumRightProjected.X = (float)Math.Cos(fovWidthRadians / 2 * aspectRatio);  // Precompute the right edge of the view frustrum.
            frustumRightProjected.Z = (float)Math.Sin(fovWidthRadians / 2 * aspectRatio);
        }

        /// <summary>
        /// Updates view and projection from this camera's data.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="elapsedTime"></param>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            xnaView = GetCameraView();
            frame.SetCamera(this);
            frustumLeft.X = -xnaView.M11 * frustumRightProjected.X + xnaView.M13 * frustumRightProjected.Z;
            frustumLeft.Y = -xnaView.M21 * frustumRightProjected.X + xnaView.M23 * frustumRightProjected.Z;
            frustumLeft.Z = -xnaView.M31 * frustumRightProjected.X + xnaView.M33 * frustumRightProjected.Z;
            frustumLeft.Normalize();
            frustumRight.X = xnaView.M11 * frustumRightProjected.X + xnaView.M13 * frustumRightProjected.Z;
            frustumRight.Y = xnaView.M21 * frustumRightProjected.X + xnaView.M23 * frustumRightProjected.Z;
            frustumRight.Z = xnaView.M31 * frustumRightProjected.X + xnaView.M33 * frustumRightProjected.Z;
            frustumRight.Normalize();
        }

        // Cull for fov
        public bool InFov(Vector3 mstsObjectCenter, float objectRadius)
        {
            mstsObjectCenter.X -= cameraLocation.Location.X;
            mstsObjectCenter.Y -= cameraLocation.Location.Y;
            mstsObjectCenter.Z -= cameraLocation.Location.Z;
            // TODO: This *2 is a complete fiddle because some objects don't currently pass in a correct radius and e.g. track sections vanish.
            objectRadius *= 2;
            if (frustumLeft.X * mstsObjectCenter.X + frustumLeft.Y * mstsObjectCenter.Y - frustumLeft.Z * mstsObjectCenter.Z > objectRadius)
                return false;
            if (frustumRight.X * mstsObjectCenter.X + frustumRight.Y * mstsObjectCenter.Y - frustumRight.Z * mstsObjectCenter.Z > objectRadius)
                return false;
            return true;
        }

        // Cull for distance
        public bool InRange(Vector3 mstsObjectCenter, float objectRadius, float objectViewingDistance)
        {
            mstsObjectCenter.X -= cameraLocation.Location.X;
            mstsObjectCenter.Z -= cameraLocation.Location.Z;

            // An object cannot be visible further away than the viewing distance.
            if (objectViewingDistance > Viewer.Settings.ViewingDistance)
                objectViewingDistance = Viewer.Settings.ViewingDistance;

            var distanceSquared = mstsObjectCenter.X * mstsObjectCenter.X + mstsObjectCenter.Z * mstsObjectCenter.Z;

            return distanceSquared < (objectRadius + objectViewingDistance) * (objectRadius + objectViewingDistance);
        }

        // Cull for screen coverage
        public bool BiggerThan(Matrix worldTileTranslation, Vector4[] boundingBoxNodes, float objectMinimumScreenCoverage)
        {
            // Following the Unity standard we check only the object's heigth as the culling prerequisite.
            // The screen projection gives the height in the -1..1 range, so we compare that with the double of the 0..1 screen coverage.
            objectMinimumScreenCoverage *= 2;
            
            var wvp = worldTileTranslation * XnaView * XnaProjection;

            var minY = float.MaxValue;
            var maxY = float.MinValue;
            for (var i = 0; i < boundingBoxNodes.Length; i++)
            {
                var screenPosition = Vector4.Transform(boundingBoxNodes[i], wvp);
                // Coordinates need to be divided by w at perspective projections:
                var y = screenPosition.Y / screenPosition.W;
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
                if (maxY - minY > objectMinimumScreenCoverage)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// If the nearest part of the object is within camera viewing distance
        /// and is within the object's defined viewing distance then
        /// we can see it.   The objectViewingDistance allows a small object
        /// to specify a cutoff beyond which the object can't be seen.
        /// </summary>
        public bool CanSee(Vector3 mstsObjectCenter, float objectRadius, float objectViewingDistance)
        {
            if (!InRange(mstsObjectCenter, objectRadius, objectViewingDistance))
                return false;

            if (!InFov(mstsObjectCenter, objectRadius))
                return false;

            return true;
        }

        protected static float GetSpeed(ElapsedTime elapsedTime)
        {
            var speed = 5 * elapsedTime.RealSeconds;
            if (UserInput.IsDown(UserCommand.CameraMoveFast))
                speed *= SpeedFactorFastSlow;
            if (UserInput.IsDown(UserCommand.CameraMoveSlow))
                speed /= SpeedFactorFastSlow;
            return speed;
        }

        protected virtual void ZoomIn(float speed)
        {
        }

        // TODO: Add a way to record this zoom operation for Replay.
        protected void ZoomByMouseWheel(float speed)
        {
            // Will not zoom-in-out when help windows is up.
            // TODO: Property input processing through WindowManager.
            if (UserInput.IsMouseWheelChanged && (!UserInput.IsDown(UserCommand.GameSwitchWithMouse) || !(this is ThreeDimCabCamera)) && !Viewer.HelpWindow.Visible)
            {
                var fieldOfView = MathHelper.Clamp(FieldOfView - speed * UserInput.MouseWheelChange / 10, 1, 135);
                new FieldOfViewCommand(Viewer.Log, fieldOfView);
            }
        }

        /// <summary>
        /// Returns a position in XNA space relative to the camera's tile
        /// </summary>
        /// <param name="worldLocation"></param>
        /// <returns></returns>
        public Vector3 XnaLocation(WorldLocation worldLocation)
        {
            var xnaVector = worldLocation.Location;
            xnaVector.X += 2048 * (worldLocation.TileX - cameraLocation.TileX);
            xnaVector.Z += 2048 * (worldLocation.TileZ - cameraLocation.TileZ);
            xnaVector.Z *= -1;
            return xnaVector;
        }


        protected class CameraAngleClamper
        {
            readonly float Minimum;
            readonly float Maximum;

            public CameraAngleClamper(float minimum, float maximum)
            {
                Minimum = minimum;
                Maximum = maximum;
            }

            public float Clamp(float angle)
            {
                return MathHelper.Clamp(angle, Minimum, Maximum);
            }
        }

        /// <summary>
        /// All OpenAL sound positions are normalized to this tile.
        /// Cannot be (0, 0) constantly, because some routes use extremely large tile coordinates,
        /// which would lead to imprecise absolute world coordinates, thus stuttering.
        /// </summary>
        public static Point SoundBaseTile = new Point(0, 0);
        /// <summary>
        /// CameraWorldLocation normalized to SoundBaseTile
        /// </summary>
        WorldLocation ListenerLocation;
        /// <summary>
        /// Set OpenAL listener position based on CameraWorldLocation normalized to SoundBaseTile
        /// </summary>
        public void UpdateListener()
        {
            ListenerLocation = new WorldLocation(CameraWorldLocation);
            ListenerLocation.NormalizeTo(SoundBaseTile.X, SoundBaseTile.Y);
            float[] cameraPosition = new float[] {
                        ListenerLocation.Location.X,
                        ListenerLocation.Location.Y,
                        ListenerLocation.Location.Z};

            float[] cameraVelocity = new float[] { 0, 0, 0 };

            if (!(this is TracksideCamera) && !(this is FreeRoamCamera) && AttachedCar != null)
            {
                var cars = Viewer.World.Trains.Cars;
                if (cars.ContainsKey(AttachedCar))
                    cameraVelocity = cars[AttachedCar].Velocity;
                else
                    cameraVelocity = new float[] { 0, 0, 0 };
            }

            float[] cameraOrientation = new float[] {
                        XnaView.Backward.X, XnaView.Backward.Y, XnaView.Backward.Z,
                        XnaView.Down.X, XnaView.Down.Y, XnaView.Down.Z };

            OpenAL.alListenerfv(OpenAL.AL_POSITION, cameraPosition);
            OpenAL.alListenerfv(OpenAL.AL_VELOCITY, cameraVelocity);
            OpenAL.alListenerfv(OpenAL.AL_ORIENTATION, cameraOrientation);
        }
    }

    public abstract class LookAtCamera : RotatingCamera
    {
        protected WorldLocation targetLocation = new WorldLocation();
        public WorldLocation TargetWorldLocation { get { return targetLocation; } }

        public override bool IsUnderground
        {
            get
            {
                var elevationAtTarget = Viewer.Tiles.GetElevation(targetLocation);
                return targetLocation.Location.Y + TerrainAltitudeMargin < elevationAtTarget;
            }
        }

        protected LookAtCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            targetLocation.Save(outf);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            targetLocation.Restore(inf);
        }

        protected override Matrix GetCameraView()
        {
            return Matrix.CreateLookAt(XnaLocation(cameraLocation), XnaLocation(targetLocation), Vector3.UnitY);
        }
    }

    public abstract class RotatingCamera : Camera
    {
        // Current camera values
        protected float RotationXRadians;
        protected float RotationYRadians;
        protected float XRadians;
        protected float YRadians;
        protected float ZRadians;

        // Target camera values
        public float? RotationXTargetRadians;
        public float? RotationYTargetRadians;
        public float? XTargetRadians;
        public float? YTargetRadians;
        public float? ZTargetRadians;
        public double EndTime;

        protected float axisZSpeedBoost = 1.0f;

        protected RotatingCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected RotatingCamera(Viewer viewer, Camera previousCamera)
            : base(viewer, previousCamera)
        {
            if (previousCamera != null)
            {
                float h, a, b;
                ORTSMath.MatrixToAngles(previousCamera.XnaView, out h, out a, out b);
                RotationXRadians = -b;
                RotationYRadians = -h;
            }
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(RotationXRadians);
            outf.Write(RotationYRadians);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            RotationXRadians = inf.ReadSingle();
            RotationYRadians = inf.ReadSingle();
        }

        public override void Reset()
        {
            base.Reset();
            RotationXRadians = RotationYRadians = XRadians = YRadians = ZRadians = 0;
        }

        protected override Matrix GetCameraView()
        {
            var lookAtPosition = Vector3.UnitZ;
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(RotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(RotationYRadians));
            lookAtPosition += cameraLocation.Location;
            lookAtPosition.Z *= -1;
            return Matrix.CreateLookAt(XnaLocation(cameraLocation), lookAtPosition, Vector3.Up);
        }

        protected static float GetMouseDelta(int mouseMovementPixels)
        {
            // Ignore CameraMoveFast as that is too fast to be useful
            var delta = 0.01f;
            if (UserInput.IsDown(UserCommand.CameraMoveSlow))
                delta *= 0.1f;
            return delta * mouseMovementPixels;
        }

        protected virtual void RotateByMouse()
        {
            if (UserInput.IsMouseRightButtonDown)
            {
                // Mouse movement doesn't use 'var speed' because the MouseMove 
                // parameters are already scaled down with increasing frame rates, 
                RotationXRadians += GetMouseDelta(UserInput.MouseMoveY);
                RotationYRadians += GetMouseDelta(UserInput.MouseMoveX);
            }
            // Support for replaying mouse movements
            if (UserInput.IsMouseRightButtonPressed)
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsMouseRightButtonReleased)
            {
                var commandEndTime = Viewer.Simulator.ClockTime;
                new CameraMouseRotateCommand(Viewer.Log, CommandStartTime, commandEndTime, RotationXRadians, RotationYRadians);
            }
        }

        protected void UpdateRotation(ElapsedTime elapsedTime)
        {
            var replayRemainingS = EndTime - Viewer.Simulator.ClockTime;
            if (replayRemainingS > 0)
            {
                var replayFraction = elapsedTime.ClockSeconds / replayRemainingS;
                if (RotationXTargetRadians != null && RotationYTargetRadians != null)
                {
                    var replayRemainingX = RotationXTargetRadians - RotationXRadians;
                    var replayRemainingY = RotationYTargetRadians - RotationYRadians;
                    var replaySpeedX = (float)(replayRemainingX * replayFraction);
                    var replaySpeedY = (float)(replayRemainingY * replayFraction);

                    if (IsCloseEnough(RotationXRadians, RotationXTargetRadians, replaySpeedX))
                    {
                        RotationXTargetRadians = null;
                    }
                    else
                    {
                        RotateDown(replaySpeedX);
                    }
                    if (IsCloseEnough(RotationYRadians, RotationYTargetRadians, replaySpeedY))
                    {
                        RotationYTargetRadians = null;
                    }
                    else
                    {
                        RotateRight(replaySpeedY);
                    }
                }
                else
                {
                    if (RotationXTargetRadians != null)
                    {
                        var replayRemainingX = RotationXTargetRadians - RotationXRadians;
                        var replaySpeedX = (float)(replayRemainingX * replayFraction);
                        if (IsCloseEnough(RotationXRadians, RotationXTargetRadians, replaySpeedX))
                        {
                            RotationXTargetRadians = null;
                        }
                        else
                        {
                            RotateDown(replaySpeedX);
                        }
                    }
                    if (RotationYTargetRadians != null)
                    {
                        var replayRemainingY = RotationYTargetRadians - RotationYRadians;
                        var replaySpeedY = (float)(replayRemainingY * replayFraction);
                        if (IsCloseEnough(RotationYRadians, RotationYTargetRadians, replaySpeedY))
                        {
                            RotationYTargetRadians = null;
                        }
                        else
                        {
                            RotateRight(replaySpeedY);
                        }
                    }
                }
            }
        }

        protected virtual void RotateDown(float speed)
        {
            RotationXRadians += speed;
            RotationXRadians = VerticalClamper.Clamp(RotationXRadians);
            MoveCamera();
        }

        protected virtual void RotateRight(float speed)
        {
            RotationYRadians += speed;
            MoveCamera();
        }

        protected void MoveCamera()
        {
            MoveCamera(new Vector3(0, 0, 0));
        }

        protected void MoveCamera(Vector3 movement)
        {
            movement = Vector3.Transform(movement, Matrix.CreateRotationX(RotationXRadians));
            movement = Vector3.Transform(movement, Matrix.CreateRotationY(RotationYRadians));
            cameraLocation.Location += movement;
            cameraLocation.Normalize();
        }

        /// <summary>
        /// A margin of half a step (increment/2) is used to prevent hunting once the target is reached.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        protected static bool IsCloseEnough(float current, float? target, float increment)
        {
            Trace.Assert(target != null, "Camera target position must not be null");
            // If a pause interrupts a camera movement, then the increment will become zero.
            if (increment == 0)
            {  // To avoid divide by zero error, just kill the movement.
                return true;
            }
            else
            {
                var error = (float)target - current;
                return error / increment < 0.5;
            }
        }
    }

    public class FreeRoamCamera : RotatingCamera
    {
        const float maxCameraHeight = 1000f;
        const float ZoomFactor = 2f;

        public override string Name { get { return Viewer.Catalog.GetString("Free"); } }

        public FreeRoamCamera(Viewer viewer, Camera previousCamera)
            : base(viewer, previousCamera)
        {
        }

        public void SetLocation(WorldLocation location)
        {
            cameraLocation = location;
        }

        public override void Reset()
        {
            // Intentionally do nothing at all.
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsDown(UserCommand.CameraZoomIn) || UserInput.IsDown(UserCommand.CameraZoomOut))
            {
                var elevation = Viewer.Tiles.GetElevation(cameraLocation);
                if (cameraLocation.Location.Y < elevation)
                    axisZSpeedBoost = 1;
                else
                {
                    cameraLocation.Location.Y = MathHelper.Min(cameraLocation.Location.Y, elevation + maxCameraHeight);
                    float cameraRelativeHeight = cameraLocation.Location.Y - elevation;
                    axisZSpeedBoost = ((cameraRelativeHeight / maxCameraHeight) * 50) + 1;
                }
            }

            var speed = GetSpeed(elapsedTime);

            // Pan and zoom camera
            if (UserInput.IsDown(UserCommand.CameraPanRight)) PanRight(speed);
            if (UserInput.IsDown(UserCommand.CameraPanLeft)) PanRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraPanUp)) PanUp(speed);
            if (UserInput.IsDown(UserCommand.CameraPanDown)) PanUp(-speed);
            if (UserInput.IsDown(UserCommand.CameraZoomIn)) ZoomIn(speed * ZoomFactor);
            if (UserInput.IsDown(UserCommand.CameraZoomOut)) ZoomIn(-speed * ZoomFactor);
            ZoomByMouseWheel(speed);

            if (UserInput.IsPressed(UserCommand.CameraPanRight) || UserInput.IsPressed(UserCommand.CameraPanLeft))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraPanRight) || UserInput.IsReleased(UserCommand.CameraPanLeft))
                new CameraXCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, XRadians);

            if (UserInput.IsPressed(UserCommand.CameraPanUp) || UserInput.IsPressed(UserCommand.CameraPanDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraPanUp) || UserInput.IsReleased(UserCommand.CameraPanDown))
                new CameraYCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, YRadians);

            if (UserInput.IsPressed(UserCommand.CameraZoomIn) || UserInput.IsPressed(UserCommand.CameraZoomOut))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraZoomIn) || UserInput.IsReleased(UserCommand.CameraZoomOut))
                new CameraZCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, ZRadians);

            speed *= SpeedAdjustmentForRotation;
            RotateByMouse();

            // Rotate camera
            if (UserInput.IsDown(UserCommand.CameraRotateUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommand.CameraRotateLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateRight)) RotateRight(speed);

            // Support for replaying camera rotation movements
            if (UserInput.IsPressed(UserCommand.CameraRotateUp) || UserInput.IsPressed(UserCommand.CameraRotateDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraRotateUp) || UserInput.IsReleased(UserCommand.CameraRotateDown))
                new CameraRotateUpDownCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationXRadians);

            if (UserInput.IsPressed(UserCommand.CameraRotateLeft) || UserInput.IsPressed(UserCommand.CameraRotateRight))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraRotateLeft) || UserInput.IsReleased(UserCommand.CameraRotateRight))
                new CameraRotateLeftRightCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationYRadians);
        }

        public override void Update(ElapsedTime elapsedTime)
        {
            UpdateRotation(elapsedTime);

            var replayRemainingS = EndTime - Viewer.Simulator.ClockTime;
            if (replayRemainingS > 0)
            {
                var replayFraction = elapsedTime.ClockSeconds / replayRemainingS;
                // Panning
                if (XTargetRadians != null)
                {
                    var replayRemainingX = XTargetRadians - XRadians;
                    var replaySpeedX = Math.Abs((float)(replayRemainingX * replayFraction));
                    if (IsCloseEnough(XRadians, XTargetRadians, replaySpeedX))
                    {
                        XTargetRadians = null;
                    }
                    else
                    {
                        PanRight(replaySpeedX);
                    }
                }
                if (YTargetRadians != null)
                {
                    var replayRemainingY = YTargetRadians - YRadians;
                    var replaySpeedY = Math.Abs((float)(replayRemainingY * replayFraction));
                    if (IsCloseEnough(YRadians, YTargetRadians, replaySpeedY))
                    {
                        YTargetRadians = null;
                    }
                    else
                    {
                        PanUp(replaySpeedY);
                    }
                }
                // Zooming
                if (ZTargetRadians != null)
                {
                    var replayRemainingZ = ZTargetRadians - ZRadians;
                    var replaySpeedZ = Math.Abs((float)(replayRemainingZ * replayFraction));
                    if (IsCloseEnough(ZRadians, ZTargetRadians, replaySpeedZ))
                    {
                        ZTargetRadians = null;
                    }
                    else
                    {
                        ZoomIn(replaySpeedZ);
                    }
                }
            }
            UpdateListener();
        }

        protected virtual void PanRight(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.X += speed;
            XRadians += movement.X;
            MoveCamera(movement);
        }

        protected virtual void PanUp(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.Y += speed;
            movement.Y = VerticalClamper.Clamp(movement.Y);    // Only the vertical needs to be clamped
            YRadians += movement.Y;
            MoveCamera(movement);
        }

        protected override void ZoomIn(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.Z += speed;
            ZRadians += movement.Z;
            MoveCamera(movement);
        }
    }

    public abstract class AttachedCamera : RotatingCamera
    {
        protected TrainCar attachedCar;
        public override TrainCar AttachedCar { get { return attachedCar; } }
        public bool tiltingLand;
        protected Vector3 attachedLocation;
        protected WorldPosition LookedAtPosition = new WorldPosition();
        protected AttachedCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            if (attachedCar != null && attachedCar.Train != null && attachedCar.Train == Viewer.SelectedTrain)
                outf.Write(Viewer.SelectedTrain.Cars.IndexOf(attachedCar));
            else
                outf.Write((int)-1);
            outf.Write(attachedLocation.X);
            outf.Write(attachedLocation.Y);
            outf.Write(attachedLocation.Z);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            var carIndex = inf.ReadInt32();
            if (carIndex != -1 && Viewer.SelectedTrain != null)
            {
                if (carIndex < Viewer.SelectedTrain.Cars.Count)
                    attachedCar = Viewer.SelectedTrain.Cars[carIndex];
                else if (Viewer.SelectedTrain.Cars.Count > 0)
                    attachedCar = Viewer.SelectedTrain.Cars[Viewer.SelectedTrain.Cars.Count -1];
            }
            attachedLocation.X = inf.ReadSingle();
            attachedLocation.Y = inf.ReadSingle();
            attachedLocation.Z = inf.ReadSingle();
        }

        protected override void OnActivate(bool sameCamera)
        {
            if (attachedCar == null || attachedCar.Train != Viewer.SelectedTrain)
            {
                if (Viewer.SelectedTrain.MUDirection != Direction.Reverse)
                    SetCameraCar(GetCameraCars().First());
                else
                    SetCameraCar(GetCameraCars().Last());
            }
            base.OnActivate(sameCamera);
        }

        protected virtual List<TrainCar> GetCameraCars()
        {
            if (Viewer.SelectedTrain.TrainType == Train.TRAINTYPE.AI_INCORPORATED) Viewer.ChangeSelectedTrain(Viewer.SelectedTrain.IncorporatingTrain);
            return Viewer.SelectedTrain.Cars;
        }

        protected virtual void SetCameraCar(TrainCar car)
        {
            attachedCar = car;
            Viewer.Simulator.SetWagonCommandReceivers((MSTSWagon)car);
        }

        protected virtual bool IsCameraFlipped()
        {
            return false;
        }

        protected void MoveCar()
        {
            if (UserInput.IsPressed(UserCommand.CameraCarNext))
                new NextCarCommand(Viewer.Log);
            else if (UserInput.IsPressed(UserCommand.CameraCarPrevious))
                new PreviousCarCommand(Viewer.Log);
            else if (UserInput.IsPressed(UserCommand.CameraCarFirst))
                new FirstCarCommand(Viewer.Log);
            else if (UserInput.IsPressed(UserCommand.CameraCarLast))
                new LastCarCommand(Viewer.Log);
        }

        public virtual void NextCar()
        {
            var trainCars = GetCameraCars();
            SetCameraCar(attachedCar == trainCars.First() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) - 1]);
        }

        public virtual void PreviousCar()
        {
            var trainCars = GetCameraCars();
            SetCameraCar(attachedCar == trainCars.Last() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) + 1]);
        }

        public virtual void FirstCar()
        {
            var trainCars = GetCameraCars();
            SetCameraCar(trainCars.First());
        }

        public virtual void LastCar()
        {
            var trainCars = GetCameraCars();
            SetCameraCar(trainCars.Last());
        }

        public void UpdateLocation(WorldPosition worldPosition)
        {
            if (worldPosition != null)
            {
                cameraLocation.TileX = worldPosition.TileX;
                cameraLocation.TileZ = worldPosition.TileZ;
                if (IsCameraFlipped())
                {
                    cameraLocation.Location.X = -attachedLocation.X;
                    cameraLocation.Location.Y = attachedLocation.Y;
                    cameraLocation.Location.Z = -attachedLocation.Z;
                }
                else
                {
                    cameraLocation.Location.X = attachedLocation.X;
                    cameraLocation.Location.Y = attachedLocation.Y;
                    cameraLocation.Location.Z = attachedLocation.Z;
                }
                cameraLocation.Location.Z *= -1;
                cameraLocation.Location = Vector3.Transform(cameraLocation.Location, worldPosition.XNAMatrix);
                cameraLocation.Location.Z *= -1;
            }
        }

        protected override Matrix GetCameraView()
        {
            var flipped = IsCameraFlipped();
            var lookAtPosition = Vector3.UnitZ;
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(RotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(RotationYRadians + (flipped ? MathHelper.Pi : 0)));
            if (flipped)
            {
                lookAtPosition.X -= attachedLocation.X;
                lookAtPosition.Y += attachedLocation.Y;
                lookAtPosition.Z -= attachedLocation.Z;
            }
            else
            {
                lookAtPosition.X += attachedLocation.X;
                lookAtPosition.Y += attachedLocation.Y;
                lookAtPosition.Z += attachedLocation.Z;
            }
            lookAtPosition.Z *= -1;
            lookAtPosition = Vector3.Transform(lookAtPosition, Viewer.Camera is TrackingCamera ? LookedAtPosition.XNAMatrix : attachedCar.WorldPosition.XNAMatrix);
            // Don't forget to rotate the up vector so the camera rotates with us.
            Vector3 up;
            if (Viewer.Camera is TrackingCamera)
                up = Vector3.Up;
            else
            {
                var upRotation = attachedCar.WorldPosition.XNAMatrix;
                upRotation.Translation = Vector3.Zero;
                up = Vector3.Transform(Vector3.Up, upRotation);
            }
            return Matrix.CreateLookAt(XnaLocation(cameraLocation), lookAtPosition, up);
        }

        public override void Update(ElapsedTime elapsedTime)
        {
            if (attachedCar != null)
            {
                cameraLocation.TileX = attachedCar.WorldPosition.TileX;
                cameraLocation.TileZ = attachedCar.WorldPosition.TileZ;
                if (IsCameraFlipped())
                {
                    cameraLocation.Location.X = -attachedLocation.X;
                    cameraLocation.Location.Y = attachedLocation.Y;
                    cameraLocation.Location.Z = -attachedLocation.Z;
                }
                else
                {
                    cameraLocation.Location.X = attachedLocation.X;
                    cameraLocation.Location.Y = attachedLocation.Y;
                    cameraLocation.Location.Z = attachedLocation.Z;
                }
                cameraLocation.Location.Z *= -1;
                cameraLocation.Location = Vector3.Transform(cameraLocation.Location, attachedCar.WorldPosition.XNAMatrix);
                cameraLocation.Location.Z *= -1;
            }
            UpdateRotation(elapsedTime);
            UpdateListener();
        }
    }

    public class TrackingCamera : AttachedCamera
    {
        const float StartPositionDistance = 20;
        const float StartPositionXRadians = 0.399f;
        const float StartPositionYRadians = 0.387f;

        protected readonly bool Front;
        public enum AttachedTo { Front, Rear }
        const float ZoomFactor = 0.1f;

        protected float PositionDistance = StartPositionDistance;
        protected float PositionXRadians = StartPositionXRadians;
        protected float PositionYRadians = StartPositionYRadians;
        public float? PositionDistanceTargetMetres;
        public float? PositionXTargetRadians;
        public float? PositionYTargetRadians;

        protected bool BrowseBackwards;
        protected bool BrowseForwards;
        const float BrowseSpeedMpS = 4;
        protected float ZDistanceM; // used to browse train;
        protected Traveller browsedTraveller;
        protected float BrowseDistance = 20;
        public bool BrowseMode = false;
        protected float LowWagonOffsetLimit;
        protected float HighWagonOffsetLimit;
        public int oldCarPosition;
        public bool IsCameraFront;
        public bool IsVisibleTrainCarViewerOrWebpage;
        public bool IsVisibleTrainCarWebPage;
        public bool IsDownCameraOutsideFront;
        public bool IsDownCameraOutsideRear;
        public UserCommand? CameraCommand;
        private static UserCommand? GetPressedKey(params UserCommand[] keysToTest) => keysToTest
            .Where((UserCommand key) => UserInput.IsDown(key))
            .FirstOrDefault();
        public override bool IsUnderground
        {
            get
            {
                var elevationAtTrain = Viewer.Tiles.GetElevation(LookedAtPosition.WorldLocation);
                var elevationAtCamera = Viewer.Tiles.GetElevation(cameraLocation);
                return LookedAtPosition.WorldLocation.Location.Y + TerrainAltitudeMargin < elevationAtTrain || cameraLocation.Location.Y + TerrainAltitudeMargin < elevationAtCamera;
            }
        }
        public override string Name { get { return Front ? Viewer.Catalog.GetString("Outside Front") : Viewer.Catalog.GetString("Outside Rear"); } }

        public TrackingCamera(Viewer viewer, AttachedTo attachedTo)
            : base(viewer)
        {
            Front = attachedTo == AttachedTo.Front;
            PositionYRadians = StartPositionYRadians + (Front ? 0 : MathHelper.Pi);
            RotationXRadians = PositionXRadians;
            RotationYRadians = PositionYRadians - MathHelper.Pi;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(PositionDistance);
            outf.Write(PositionXRadians);
            outf.Write(PositionYRadians);
            outf.Write(BrowseMode);
            outf.Write(BrowseForwards);
            outf.Write(BrowseBackwards);
            outf.Write(ZDistanceM);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            PositionDistance = inf.ReadSingle();
            PositionXRadians = inf.ReadSingle();
            PositionYRadians = inf.ReadSingle();
            BrowseMode = inf.ReadBoolean();
            BrowseForwards = inf.ReadBoolean();
            BrowseBackwards = inf.ReadBoolean();
            ZDistanceM = inf.ReadSingle();
            if (attachedCar != null && attachedCar.Train == Viewer.SelectedTrain)
            {
                var trainCars = GetCameraCars();
                BrowseDistance = attachedCar.CarLengthM * 0.5f;
                if (Front)
                {
                    browsedTraveller = new Traveller(attachedCar.Train.FrontTDBTraveller);
                    browsedTraveller.Move(-attachedCar.CarLengthM * 0.5f + ZDistanceM);
                }
                else
                {
                    browsedTraveller = new Traveller(attachedCar.Train.RearTDBTraveller);
                    browsedTraveller.Move((attachedCar.CarLengthM - trainCars.First().CarLengthM - trainCars.Last().CarLengthM) * 0.5f + attachedCar.Train.Length - ZDistanceM);
                }
                //               LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
                ComputeCarOffsets(this);
            }
        }

        public override void Reset()
        {
            base.Reset();
            PositionDistance = StartPositionDistance;
            PositionXRadians = StartPositionXRadians;
            PositionYRadians = StartPositionYRadians + (Front ? 0 : MathHelper.Pi);
            RotationXRadians = PositionXRadians;
            RotationYRadians = PositionYRadians - MathHelper.Pi;
        }

        protected override void OnActivate(bool sameCamera)
        {
            BrowseMode = BrowseForwards = BrowseBackwards = false;
            var trainCars = GetCameraCars();
            var TrainCarViewer = Viewer.TrainCarOperationsViewerWindow;
            var carPosition = !(TrainCarViewer.CarPosition < trainCars.Count()) ? TrainCarViewer.CarPosition - 1 : TrainCarViewer.CarPosition;
            var isDownCameraOutsideFront = UserInput.IsDown(UserCommand.CameraOutsideFront);
            var isDownCameraOutsideRear = UserInput.IsDown(UserCommand.CameraOutsideRear);

            if (Viewer.TrainCarOperationsWebpage == null) 
            {
                // when starting Open Rails by means of a restore Viewer.TrainCarOperationsWebpage not yet available
                IsVisibleTrainCarViewerOrWebpage = Viewer.TrainCarOperationsViewerWindow.Visible;
            }
            else
            {
                IsVisibleTrainCarViewerOrWebpage = (Viewer.TrainCarOperationsWindow.Visible && !Viewer.TrainCarOperationsViewerWindow.Visible) || Viewer.TrainCarOperationsViewerWindow.Visible || (Viewer.TrainCarOperationsWebpage?.Connections > 0 && Viewer.TrainCarOperationsWebpage.TrainCarSelected);
            }

            if (IsVisibleTrainCarViewerOrWebpage)
            {
                // Update the camera view
                oldCarPosition = oldCarPosition == 0 && carPosition == 0 ? -1 : oldCarPosition;
            }

            if (attachedCar != null && !IsVisibleTrainCarViewerOrWebpage)
            {   // Reset behaviour of camera 2 and camera 3, after closing F9-window and F9-web.
                var attachedCarPosition = Front ? Viewer.CameraOutsideFrontPosition : Viewer.CameraOutsideRearPosition;

                Viewer.FirstLoop = false;
                if (Front)
                {
                    SetCameraCar(trainCars[Viewer.CameraOutsideFrontPosition]);
                    Viewer.CameraFrontUpdated = true;
                }
                else
                {
                    if (Viewer.CameraOutsideRearPosition == 0)
                    {
                        SetCameraCar(GetCameraCars().Last());
                    }
                    else
                    {
                        if (Viewer.CameraOutsideRearPosition < trainCars.Count)
                        {
                            SetCameraCar(trainCars[Viewer.CameraOutsideRearPosition]);
                        }
                        else
                        {
                            SetCameraCar(trainCars[trainCars.Count - 1]);
                        }
                    }
                    Viewer.CameraRearUpdated = true;
                }
                Viewer.IsCameraPositionUpdated = Viewer.CameraFrontUpdated && Viewer.CameraRearUpdated;
            }

            if (attachedCar == null || attachedCar.Train != Viewer.SelectedTrain || carPosition != oldCarPosition)
            {
                if (Front)
                {
                    if (!IsVisibleTrainCarViewerOrWebpage && isDownCameraOutsideFront)
                    {
                        if (Viewer.CameraOutsideFrontPosition == 0) SetCameraCar(GetCameraCars().First());
                        oldCarPosition = 0;
                    }
                    else if (IsVisibleTrainCarViewerOrWebpage && carPosition >= 0)
                    {
                        if (carPosition < trainCars.Count)
                        {
                            // sometimes when decoupling cars the carPosition is out-of-range
                            SetCameraCar(trainCars[carPosition]);
                            oldCarPosition = carPosition;
                        }
                    }
                    else
                        SetCameraCar(GetCameraCars().First());

                    browsedTraveller = new Traveller(attachedCar.Train.FrontTDBTraveller);
                    ZDistanceM = -attachedCar.CarLengthM / 2;
                    HighWagonOffsetLimit = 0;
                    LowWagonOffsetLimit = -attachedCar.CarLengthM;
                }
                else
                {
                    if (!IsVisibleTrainCarViewerOrWebpage && isDownCameraOutsideRear)
                    {
                        if (Viewer.CameraOutsideRearPosition == 0) SetCameraCar(GetCameraCars().Last());
                        oldCarPosition = 0;
                    }
                    else if (carPosition < trainCars.Count && IsVisibleTrainCarViewerOrWebpage && carPosition >= 0)
                    {
                        SetCameraCar(trainCars[carPosition]);
                        oldCarPosition = carPosition;
                    }
                    else
                        SetCameraCar(trainCars.Last());

                    browsedTraveller = new Traveller(attachedCar.Train.RearTDBTraveller);
                    ZDistanceM = -attachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f + attachedCar.CarLengthM / 2;
                    LowWagonOffsetLimit = -attachedCar.Train.Length + trainCars.First().CarLengthM * 0.5f;
                    HighWagonOffsetLimit = LowWagonOffsetLimit + attachedCar.CarLengthM;
                }
                BrowseDistance = attachedCar.CarLengthM * 0.5f;
            }
            base.OnActivate(sameCamera);
            CameraOutsidePosition();
        }

        public void CameraOutsidePosition()
        {
            if (!IsVisibleTrainCarViewerOrWebpage)
            {
                var attachedCarIdPos = attachedCar.Train.Cars.TakeWhile(x => x.CarID != attachedCar.CarID).Count();
                if (Front)
                {
                    Viewer.CameraOutsideFrontPosition = attachedCarIdPos;
                }
                else if (!Front)
                {
                    Viewer.CameraOutsideRearPosition = attachedCarIdPos;
                }
            }
            Viewer.IsCameraPositionUpdated = !IsVisibleTrainCarViewerOrWebpage;
        }

        protected override bool IsCameraFlipped()
        {
            return BrowseMode ? false : attachedCar.Flipped;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            MoveCar();

            var speed = GetSpeed(elapsedTime);

            if (UserInput.IsDown(UserCommand.CameraZoomOut)) ZoomIn(speed * ZoomFactor);
            if (UserInput.IsDown(UserCommand.CameraZoomIn)) ZoomIn(-speed * ZoomFactor);
            ZoomByMouseWheel(speed);

            if (UserInput.IsPressed(UserCommand.CameraZoomOut) || UserInput.IsPressed(UserCommand.CameraZoomIn))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraZoomOut) || UserInput.IsReleased(UserCommand.CameraZoomIn))
                new TrackingCameraZCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, PositionDistance);

            speed = GetSpeed(elapsedTime) * SpeedAdjustmentForRotation;

            // Pan camera
            if (UserInput.IsDown(UserCommand.CameraPanUp)) PanUp(speed);
            if (UserInput.IsDown(UserCommand.CameraPanDown)) PanUp(-speed);
            if (UserInput.IsDown(UserCommand.CameraPanLeft)) PanRight(speed);
            if (UserInput.IsDown(UserCommand.CameraPanRight)) PanRight(-speed);

            // Support for replaying camera pan movements
            if (UserInput.IsPressed(UserCommand.CameraPanUp) || UserInput.IsPressed(UserCommand.CameraPanDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraPanUp) || UserInput.IsReleased(UserCommand.CameraPanDown))
                new TrackingCameraXCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, PositionXRadians);

            if (UserInput.IsPressed(UserCommand.CameraPanLeft) || UserInput.IsPressed(UserCommand.CameraPanRight))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraPanLeft) || UserInput.IsReleased(UserCommand.CameraPanRight))
                new TrackingCameraYCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, PositionYRadians);

            RotateByMouse();

            // Rotate camera
            if (UserInput.IsDown(UserCommand.CameraRotateUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommand.CameraRotateLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateRight)) RotateRight(speed);

            // Support for replaying camera rotation movements
            if (UserInput.IsPressed(UserCommand.CameraRotateUp) || UserInput.IsPressed(UserCommand.CameraRotateDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraRotateUp) || UserInput.IsReleased(UserCommand.CameraRotateDown))
                new CameraRotateUpDownCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationXRadians);

            if (UserInput.IsPressed(UserCommand.CameraRotateLeft) || UserInput.IsPressed(UserCommand.CameraRotateRight))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommand.CameraRotateLeft) || UserInput.IsReleased(UserCommand.CameraRotateRight))
                new CameraRotateLeftRightCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationYRadians);

            if (UserInput.IsPressed(UserCommand.CameraBrowseBackwards))
                new ToggleBrowseBackwardsCommand(Viewer.Log);
            if (UserInput.IsPressed(UserCommand.CameraBrowseForwards))
                new ToggleBrowseForwardsCommand(Viewer.Log);

            UserCommand? CameraCommand = GetPressedKey(UserCommand.CameraCarPrevious, UserCommand.CameraCarNext,
            UserCommand.CameraCarFirst, UserCommand.CameraCarLast);

            if (CameraCommand == UserCommand.CameraCarPrevious || CameraCommand == UserCommand.CameraCarNext || CameraCommand == UserCommand.CameraCarFirst
                || CameraCommand == UserCommand.CameraCarLast)
            {   // updates camera out side car position
                CameraOutsidePosition();
                Viewer.IsDownCameraChanged = IsDownCameraOutsideFront = IsDownCameraOutsideRear = false;
            }
        }

        public override void Update(ElapsedTime elapsedTime)
        {
            var replayRemainingS = EndTime - Viewer.Simulator.ClockTime;
            if (replayRemainingS > 0)
            {
                var replayFraction = elapsedTime.ClockSeconds / replayRemainingS;
                // Panning
                if (PositionXTargetRadians != null)
                {
                    var replayRemainingX = PositionXTargetRadians - PositionXRadians;
                    var replaySpeedX = (float)(replayRemainingX * replayFraction);
                    if (IsCloseEnough(PositionXRadians, PositionXTargetRadians, replaySpeedX))
                    {
                        PositionXTargetRadians = null;
                    }
                    else
                    {
                        PanUp(replaySpeedX);
                    }
                }
                if (PositionYTargetRadians != null)
                {
                    var replayRemainingY = PositionYTargetRadians - PositionYRadians;
                    var replaySpeedY = (float)(replayRemainingY * replayFraction);
                    if (IsCloseEnough(PositionYRadians, PositionYTargetRadians, replaySpeedY))
                    {
                        PositionYTargetRadians = null;
                    }
                    else
                    {
                        PanRight(replaySpeedY);
                    }
                }
                // Zooming
                if (PositionDistanceTargetMetres != null)
                {
                    var replayRemainingZ = PositionDistanceTargetMetres - PositionDistance;
                    var replaySpeedZ = (float)(replayRemainingZ * replayFraction);
                    if (IsCloseEnough(PositionDistance, PositionDistanceTargetMetres, replaySpeedZ))
                    {
                        PositionDistanceTargetMetres = null;
                    }
                    else
                    {
                        ZoomIn(replaySpeedZ / PositionDistance);
                    }
                }
            }

            // Rotation
            UpdateRotation(elapsedTime);

            // Update location of attachment
            attachedLocation.X = 0;
            attachedLocation.Y = 2;
            attachedLocation.Z = PositionDistance;
            attachedLocation = Vector3.Transform(attachedLocation, Matrix.CreateRotationX(-PositionXRadians));
            attachedLocation = Vector3.Transform(attachedLocation, Matrix.CreateRotationY(PositionYRadians));

            // Update location of camera
            if (BrowseMode)
            {
                UpdateTrainBrowsing(elapsedTime);
                attachedLocation.Z += BrowseDistance * (Front ? 1 : -1);
                LookedAtPosition.XNAMatrix = Matrix.CreateFromYawPitchRoll(-browsedTraveller.RotY, 0, 0);
                LookedAtPosition.XNAMatrix.M41 = browsedTraveller.X;
                LookedAtPosition.XNAMatrix.M42 = browsedTraveller.Y;
                LookedAtPosition.XNAMatrix.M43 = browsedTraveller.Z;
                LookedAtPosition.TileX = browsedTraveller.TileX;
                LookedAtPosition.TileZ = browsedTraveller.TileZ;
                LookedAtPosition.XNAMatrix.M43 *= -1;
            }
            else if (attachedCar != null)
            {
                LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);

                // Cancel out unwanted effects on camera motion caused by vibration and superelevation
                LookedAtPosition.XNAMatrix = attachedCar.SuperElevationInverseMatrix * attachedCar.VibrationInverseMatrix * LookedAtPosition.XNAMatrix;
            }
            UpdateLocation(LookedAtPosition);
            UpdateListener();
        }

        protected void UpdateTrainBrowsing(ElapsedTime elapsedTime)
        {
            var trainCars = GetCameraCars();
            if (BrowseBackwards)
            {
                var ZIncrM = -BrowseSpeedMpS * elapsedTime.ClockSeconds;
                ZDistanceM += ZIncrM;
                if (-ZDistanceM >= attachedCar.Train.Length - (trainCars.First().CarLengthM  + trainCars.Last().CarLengthM) * 0.5f)
                {
                    ZIncrM = -attachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f - (ZDistanceM - ZIncrM);
                    ZDistanceM = -attachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f;
                    BrowseBackwards = false;
                }
                else if (ZDistanceM < LowWagonOffsetLimit)
                {
                    base.PreviousCar();
                    HighWagonOffsetLimit = LowWagonOffsetLimit;
                    LowWagonOffsetLimit -= attachedCar.CarLengthM;
                }
                browsedTraveller.Move(elapsedTime.ClockSeconds * attachedCar.Train.SpeedMpS + ZIncrM);
            }
            else if (BrowseForwards)
            {
                var ZIncrM = BrowseSpeedMpS * elapsedTime.ClockSeconds;
                ZDistanceM += ZIncrM;
                if (ZDistanceM >= 0)
                {
                    ZIncrM = ZIncrM - ZDistanceM;
                    ZDistanceM = 0;
                    BrowseForwards = false;
                }
                else if (ZDistanceM > HighWagonOffsetLimit)
                {
                    base.NextCar();
                    LowWagonOffsetLimit = HighWagonOffsetLimit;
                    HighWagonOffsetLimit += attachedCar.CarLengthM;
                }
                browsedTraveller.Move(elapsedTime.ClockSeconds * attachedCar.Train.SpeedMpS + ZIncrM);
            }
            else browsedTraveller.Move(elapsedTime.ClockSeconds * attachedCar.Train.SpeedMpS);
        }

        protected void ComputeCarOffsets( TrackingCamera camera)
        {
            var trainCars = camera.GetCameraCars();
            camera.HighWagonOffsetLimit = trainCars.First().CarLengthM * 0.5f;
            foreach (TrainCar trainCar in trainCars)
            {
                camera.LowWagonOffsetLimit = camera.HighWagonOffsetLimit - trainCar.CarLengthM;
                if (ZDistanceM > LowWagonOffsetLimit) break;
                else camera.HighWagonOffsetLimit = camera.LowWagonOffsetLimit;
            }
        }

        protected void PanUp(float speed)
        {
            PositionXRadians += speed;
            PositionXRadians = VerticalClamper.Clamp(PositionXRadians);
            RotationXRadians += speed;
            RotationXRadians = VerticalClamper.Clamp(RotationXRadians);
        }

        protected void PanRight(float speed)
        {
            PositionYRadians += speed;
            RotationYRadians += speed;
        }

        protected override void ZoomIn(float speed)
        {
            // Speed depends on distance, slows down when zooming in, speeds up zooming out.
            PositionDistance += speed * PositionDistance;
            PositionDistance = MathHelper.Clamp(PositionDistance, 1, 100);
        }

        /// <summary>
        /// Swaps front and rear tracking camera after reversal point, to avoid abrupt change of picture
        /// </summary>

        public void SwapCameras()
        {
            if (Front)
            {
                SwapParams(this, Viewer.BackCamera);
                Viewer.BackCamera.Activate();
            }
            else
            {
                SwapParams(this, Viewer.FrontCamera);
                Viewer.FrontCamera.Activate();
            }
        }


        /// <summary>
        /// Swaps parameters of Front and Back Camera
        /// </summary>
        /// 
        protected void SwapParams(TrackingCamera oldCamera, TrackingCamera newCamera)
        {
            TrainCar swapCar = newCamera.attachedCar;
            newCamera.attachedCar = oldCamera.attachedCar;
            oldCamera.attachedCar = swapCar;
            float swapFloat = newCamera.PositionDistance;
            newCamera.PositionDistance = oldCamera.PositionDistance;
            oldCamera.PositionDistance = swapFloat;
            swapFloat = newCamera.PositionXRadians;
            newCamera.PositionXRadians = oldCamera.PositionXRadians;
            oldCamera.PositionXRadians = swapFloat;
            swapFloat = newCamera.PositionYRadians;
            newCamera.PositionYRadians = oldCamera.PositionYRadians + MathHelper.Pi * (Front ? 1 : -1);
            oldCamera.PositionYRadians = swapFloat - MathHelper.Pi * (Front ? 1 : -1);
            swapFloat = newCamera.RotationXRadians;
            newCamera.RotationXRadians = oldCamera.RotationXRadians;
            oldCamera.RotationXRadians = swapFloat;
            swapFloat = newCamera.RotationYRadians;
            newCamera.RotationYRadians = oldCamera.RotationYRadians - MathHelper.Pi * (Front ? 1 : -1);
            oldCamera.RotationYRadians = swapFloat + MathHelper.Pi * (Front ? 1 : -1);

            // adjust and swap data for camera browsing

            newCamera.BrowseForwards = newCamera.BrowseBackwards = false;
            var trainCars = newCamera.GetCameraCars();
            newCamera.ZDistanceM = -newCamera.attachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f - oldCamera.ZDistanceM;
            ComputeCarOffsets(newCamera);
            // Todo travellers
        }

        public override void NextCar()
        {
            BrowseBackwards = false;
            BrowseForwards = false;
            BrowseMode = false;
            var trainCars = GetCameraCars();
            var wasFirstCar = attachedCar == trainCars.First();
            base.NextCar();
            if (!wasFirstCar)
            {
                LowWagonOffsetLimit = HighWagonOffsetLimit;
                HighWagonOffsetLimit += attachedCar.CarLengthM;
                ZDistanceM = LowWagonOffsetLimit + attachedCar.CarLengthM * 0.5f;
            }
 //           LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
        }

        public override void PreviousCar()
        {
            BrowseBackwards = false;
            BrowseForwards = false;
            BrowseMode = false;
            var trainCars = GetCameraCars();
            var wasLastCar = attachedCar == trainCars.Last();
            base.PreviousCar();
            if (!wasLastCar)
            {
                HighWagonOffsetLimit = LowWagonOffsetLimit;
                LowWagonOffsetLimit -= attachedCar.CarLengthM;
                ZDistanceM = LowWagonOffsetLimit + attachedCar.CarLengthM * 0.5f;
            }
 //           LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
        }

        public override void FirstCar()
        {
            BrowseBackwards = false;
            BrowseForwards = false;
            BrowseMode = false;
            base.FirstCar();
            ZDistanceM = 0;
            HighWagonOffsetLimit = attachedCar.CarLengthM * 0.5f;
            LowWagonOffsetLimit = -attachedCar.CarLengthM * 0.5f;
//            LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
        }

        public override void LastCar()
        {
            BrowseBackwards = false;
            BrowseForwards = false;
            BrowseMode = false;
            base.LastCar();
            var trainCars = GetCameraCars();
            ZDistanceM = -attachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f;
            LowWagonOffsetLimit = -attachedCar.Train.Length + trainCars.First().CarLengthM * 0.5f;
            HighWagonOffsetLimit = LowWagonOffsetLimit + attachedCar.CarLengthM;
//            LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
        }

        public void ToggleBrowseBackwards()
        {
            BrowseBackwards = !BrowseBackwards;
            if (BrowseBackwards)
            {
                if (!BrowseMode)
                {
//                    LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
                    browsedTraveller = new Traveller(attachedCar.Train.FrontTDBTraveller);
                    browsedTraveller.Move(-attachedCar.CarLengthM * 0.5f + ZDistanceM);
                    BrowseDistance = attachedCar.CarLengthM * 0.5f;
                    BrowseMode = true;
                }
            }
            BrowseForwards = false;
        }

        public void ToggleBrowseForwards()
        {
            BrowseForwards = !BrowseForwards;
            if (BrowseForwards)
            {
                if (!BrowseMode)
                {
//                    LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
                    browsedTraveller = new Traveller(attachedCar.Train.RearTDBTraveller);
                    var trainCars = GetCameraCars();
                    browsedTraveller.Move((attachedCar.CarLengthM - trainCars.First().CarLengthM - trainCars.Last().CarLengthM) * 0.5f + attachedCar.Train.Length + ZDistanceM);
                    BrowseDistance = attachedCar.CarLengthM * 0.5f;
                    BrowseMode = true;
                }
            }          
            BrowseBackwards = false;
        }
    }

    public abstract class NonTrackingCamera : AttachedCamera
    {
        public NonTrackingCamera(Viewer viewer)
            : base(viewer)
        {
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            MoveCar();

            RotateByMouse();

            var speed = GetSpeed(elapsedTime) * SpeedAdjustmentForRotation;

            // Rotate camera
            if (UserInput.IsDown(UserCommand.CameraRotateUp) || UserInput.IsDown(UserCommand.CameraPanUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateDown) || UserInput.IsDown(UserCommand.CameraPanDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommand.CameraRotateLeft) || UserInput.IsDown(UserCommand.CameraPanLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateRight) || UserInput.IsDown(UserCommand.CameraPanRight)) RotateRight(speed);

            // Zoom
            ZoomByMouseWheel(speed);

            // Support for replaying camera rotation movements
            if (UserInput.IsPressed(UserCommand.CameraRotateUp) || UserInput.IsPressed(UserCommand.CameraRotateDown)
                || UserInput.IsPressed(UserCommand.CameraPanUp) || UserInput.IsPressed(UserCommand.CameraPanDown))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsReleased(UserCommand.CameraRotateUp) || UserInput.IsReleased(UserCommand.CameraRotateDown)
                || UserInput.IsReleased(UserCommand.CameraPanUp) || UserInput.IsReleased(UserCommand.CameraPanDown))
                new CameraRotateUpDownCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationXRadians);

            if (UserInput.IsPressed(UserCommand.CameraRotateLeft) || UserInput.IsPressed(UserCommand.CameraRotateRight)
                || UserInput.IsPressed(UserCommand.CameraPanLeft) || UserInput.IsPressed(UserCommand.CameraPanRight))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsReleased(UserCommand.CameraRotateLeft) || UserInput.IsReleased(UserCommand.CameraRotateRight)
                || UserInput.IsReleased(UserCommand.CameraPanLeft) || UserInput.IsReleased(UserCommand.CameraPanRight))
                new CameraRotateLeftRightCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationYRadians);
        }
    }

    public class BrakemanCamera : NonTrackingCamera
    {
        protected bool attachedToRear;

        public override float NearPlane { get { return 0.25f; } }
        public override string Name { get { return Viewer.Catalog.GetString("Brakeman"); } }

        public BrakemanCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected override List<TrainCar> GetCameraCars()
        {
            var cars = base.GetCameraCars();
            return new List<TrainCar>(new[] { cars.First(), cars.Last() });
        }

        protected override void SetCameraCar(TrainCar car)
        {
            base.SetCameraCar(car);
            attachedLocation = new Vector3(1.8f, 2.0f, attachedCar.CarLengthM / 2 - 0.3f);
            attachedToRear = car.Train.Cars[0] != car;
        }

        protected override bool IsCameraFlipped()
        {
            return attachedToRear ^ attachedCar.Flipped;
        }

        // attached car may be no more part of the list, therefore base methods would return errors
        public override void NextCar()
        {
            FirstCar();
        }

        public override void PreviousCar()
        {
            LastCar();
        }

        public override void LastCar()
        {
            base.LastCar();
            attachedToRear = true;
        }
    }

    public class InsideThreeDimCamera : NonTrackingCamera
    {
        public override float NearPlane { get { return 0.1f; } }

        public InsideThreeDimCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected Vector3 viewPointLocation;
        protected float viewPointRotationXRadians = 0;
        protected float viewPointRotationYRadians = 0;
        protected Vector3 StartViewPointLocation;
        protected float StartViewPointRotationXRadians = 0;
        protected float StartViewPointRotationYRadians = 0;
        protected string prevcar = "";
        protected int ActViewPoint = 0;
        protected int prevViewPoint = -1;
        protected bool PrevCabWasRear = false;

        /// <summary>
        /// A camera can use this method to handle any preparation when being activated.
        /// </summary>
        protected override void OnActivate(bool sameCamera)
        {
            var trainCars = GetCameraCars();
            if (trainCars.Count == 0) return;//may not have passenger or 3d cab viewpoints
            if (sameCamera)
            {
                if (!trainCars.Contains(attachedCar)) { attachedCar = trainCars.First(); }
                else if (trainCars.IndexOf(attachedCar) < trainCars.Count - 1)
                {
                    attachedCar = trainCars[trainCars.IndexOf(attachedCar) + 1];
                }
                else attachedCar = trainCars.First();
            }
            else
            {
                if (!trainCars.Contains(attachedCar)) attachedCar = trainCars.First();
            }
            SetCameraCar(attachedCar);
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            MoveCar();

            RotateByMouse();

            var speed = GetSpeed(elapsedTime) * SpeedAdjustmentForRotation;

            // Rotate camera
            if (UserInput.IsDown(UserCommand.CameraPanUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommand.CameraPanDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommand.CameraPanLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraPanRight)) RotateRight(speed);
            float x = 0.0f, y = 0.0f, z = 0.0f;

            // Move camera
            if (UserInput.IsDown(UserCommand.CameraZoomIn))
            {
                z = speed * 5;
                MoveCameraXYZ(0, 0, z);

            }
            if (UserInput.IsDown(UserCommand.CameraZoomOut))
            {
                z = speed * -5;
                MoveCameraXYZ(0, 0, z);
            }
            // Move camera
            if (UserInput.IsDown(UserCommand.CameraRotateUp))
            {
                y = speed / 2;
                MoveCameraXYZ(0, y, 0);

            }
            if (UserInput.IsDown(UserCommand.CameraRotateDown))
            {
                y = -speed / 2;
                MoveCameraXYZ(0, y, 0);
            }
            if (UserInput.IsDown(UserCommand.CameraRotateLeft))
            {
                x = speed * -2;
                MoveCameraXYZ(x, 0, 0);
            }
            if (UserInput.IsDown(UserCommand.CameraRotateRight))
            {
                x = speed * 2;
                MoveCameraXYZ(x, 0, 0);
            }
            // Zoom
            ZoomByMouseWheel(speed);

            // Support for replaying camera rotation movements
            if (UserInput.IsPressed(UserCommand.CameraPanUp) || UserInput.IsPressed(UserCommand.CameraPanDown))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsReleased(UserCommand.CameraPanUp) || UserInput.IsReleased(UserCommand.CameraPanDown))
                new CameraRotateUpDownCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationXRadians);

            if (UserInput.IsPressed(UserCommand.CameraPanLeft) || UserInput.IsPressed(UserCommand.CameraPanRight))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsReleased(UserCommand.CameraPanLeft) || UserInput.IsReleased(UserCommand.CameraPanRight))
                new CameraRotateLeftRightCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationYRadians);

            if (UserInput.IsPressed(UserCommand.CameraRotateUp) || UserInput.IsPressed(UserCommand.CameraRotateDown)
                || UserInput.IsPressed(UserCommand.CameraRotateLeft) || UserInput.IsPressed(UserCommand.CameraRotateRight))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsPressed(UserCommand.CameraRotateUp) || UserInput.IsPressed(UserCommand.CameraRotateDown)
                || UserInput.IsPressed(UserCommand.CameraRotateLeft) || UserInput.IsPressed(UserCommand.CameraRotateRight))
                new CameraMoveXYZCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, x, y, z);

        }
        public void MoveCameraXYZ(float x, float y, float z)
        {
            if (PrevCabWasRear)
            {
                x = -x;
                z = -z;
            }
            attachedLocation.X += x;
            attachedLocation.Y += y;
            attachedLocation.Z += z;
            viewPointLocation.X = attachedLocation.X; viewPointLocation.Y = attachedLocation.Y; viewPointLocation.Z = attachedLocation.Z;
            if (attachedCar != null) UpdateLocation(attachedCar.WorldPosition);
        }

        /// <summary>
        /// Remembers angle of camera to apply when user returns to this type of car.
        /// </summary>
        /// <param name="speed"></param>
        protected override void RotateRight(float speed)
        {
            base.RotateRight(speed);
            viewPointRotationYRadians = RotationYRadians;
        }

        /// <summary>
        /// Remembers angle of camera to apply when user returns to this type of car.
        /// </summary>
        /// <param name="speed"></param>
        protected override void RotateDown(float speed)
        {
            base.RotateDown(speed);
            viewPointRotationXRadians = RotationXRadians;
        }

        /// <summary>
        /// Remembers angle of camera to apply when user returns to this type of car.
        /// </summary>
        /// <param name="speed"></param>
        protected override void RotateByMouse()
        {
            base.RotateByMouse();
            if (UserInput.IsMouseRightButtonReleased)
            {
                viewPointRotationXRadians = RotationXRadians;
                viewPointRotationYRadians = RotationYRadians;
            }
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(ActViewPoint);
            outf.Write(prevViewPoint);
            outf.Write(prevcar);
            outf.Write(StartViewPointLocation.X);
            outf.Write(StartViewPointLocation.Y);
            outf.Write(StartViewPointLocation.Z);
            outf.Write(StartViewPointRotationXRadians);
            outf.Write(StartViewPointRotationYRadians);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            ActViewPoint = inf.ReadInt32();
            prevViewPoint = inf.ReadInt32();
            prevcar = inf.ReadString();
            StartViewPointLocation.X = inf.ReadSingle();
            StartViewPointLocation.Y = inf.ReadSingle();
            StartViewPointLocation.Z = inf.ReadSingle();
            StartViewPointRotationXRadians = inf.ReadSingle();
            StartViewPointRotationYRadians = inf.ReadSingle();
        }

        public override void Reset()
        {
            base.Reset();
            viewPointLocation = StartViewPointLocation;
            attachedLocation = StartViewPointLocation;
            viewPointRotationXRadians = StartViewPointRotationXRadians;
            viewPointRotationYRadians = StartViewPointRotationYRadians;
            RotationXRadians = StartViewPointRotationXRadians;
            RotationYRadians = StartViewPointRotationYRadians;
            XRadians = StartViewPointRotationXRadians;
            YRadians = StartViewPointRotationYRadians;
        }

        public void SwitchSideCameraCar(TrainCar car)
        {
            attachedLocation.X = -attachedLocation.X;
            RotationYRadians = -RotationYRadians;
        }
    }

    public class PassengerCamera : InsideThreeDimCamera
    {
        public override Styles Style { get { return Styles.Passenger; } }
        public override bool IsAvailable { get { return Viewer.SelectedTrain != null && Viewer.SelectedTrain.Cars.Any(c => c.PassengerViewpoints.Count > 0); } }
        public override string Name { get { return Viewer.Catalog.GetString("Passenger"); } }

        public PassengerCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected override List<TrainCar> GetCameraCars()
        {
            return base.GetCameraCars().Where(c => c.PassengerViewpoints.Count > 0).ToList();
        }

        protected override void SetCameraCar(TrainCar car)
        {
            base.SetCameraCar(car);
            // Settings are held so that when switching back from another camera, view is not reset.
            // View is only reset on move to a different car and/or viewpoint or "Ctl + 8".
            if (car.CarID != prevcar)
            {
                ActViewPoint = 0;
                ResetViewPoint(car);
            }
            else if (ActViewPoint != prevViewPoint)
            {
                ResetViewPoint(car);
            }
        }

        protected void ResetViewPoint (TrainCar car)
        {
            prevcar = car.CarID;
            prevViewPoint = ActViewPoint;
            viewPointLocation = attachedCar.PassengerViewpoints[ActViewPoint].Location;
            viewPointRotationXRadians = attachedCar.PassengerViewpoints[ActViewPoint].RotationXRadians;
            viewPointRotationYRadians = attachedCar.PassengerViewpoints[ActViewPoint].RotationYRadians;
            RotationXRadians = viewPointRotationXRadians;
            RotationYRadians = viewPointRotationYRadians;
            attachedLocation = viewPointLocation;
            StartViewPointLocation = viewPointLocation;
            StartViewPointRotationXRadians = viewPointRotationXRadians;
            StartViewPointRotationYRadians = viewPointRotationYRadians;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            base.HandleUserInput(elapsedTime);
            if (UserInput.IsPressed(UserCommand.CameraChangePassengerViewPoint))
                new CameraChangePassengerViewPointCommand(Viewer.Log);
        }
        
        public void ChangePassengerViewPoint(TrainCar car)
        {
            ActViewPoint++;
            if (ActViewPoint >= car.PassengerViewpoints.Count) ActViewPoint = 0;
            SetCameraCar(car);
        }
    }

    public class ThreeDimCabCamera : InsideThreeDimCamera
    {
        public override Styles Style { get { return Styles.ThreeDimCab; } }
        public override bool IsAvailable
        {
            get
            {
                return Viewer.SelectedTrain != null && Viewer.SelectedTrain.IsActualPlayerTrain &&
                    Viewer.PlayerLocomotive != null && Viewer.PlayerLocomotive.CabViewpoints != null &&
                    (Viewer.PlayerLocomotive.HasFront3DCab || Viewer.PlayerLocomotive.HasRear3DCab);
            }
        }
        public override string Name { get { return Viewer.Catalog.GetString("3D Cab"); } }

        public ThreeDimCabCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected override List<TrainCar> GetCameraCars()
        {
            if (Viewer.SelectedTrain != null && Viewer.SelectedTrain.IsActualPlayerTrain &&
            Viewer.PlayerLocomotive != null && Viewer.PlayerLocomotive.CabViewpoints != null)
            {
                List<TrainCar> l = new List<TrainCar>();
                l.Add(Viewer.PlayerLocomotive);
                return l;
            }
            else return base.GetCameraCars();
        }

        protected override void SetCameraCar(TrainCar car)
        {
            base.SetCameraCar(car);
            // Settings are held so that when switching back from another camera, view is not reset.
            // View is only reset on move to a different cab or "Ctl + 8".
            if (attachedCar.CabViewpoints != null)
            {
                if (car.CarID != prevcar || ActViewPoint != prevViewPoint)
                {
                    prevcar = car.CarID;
                    prevViewPoint = ActViewPoint;
                    viewPointLocation = attachedCar.CabViewpoints[ActViewPoint].Location;
                    viewPointRotationXRadians = attachedCar.CabViewpoints[ActViewPoint].RotationXRadians;
                    viewPointRotationYRadians = attachedCar.CabViewpoints[ActViewPoint].RotationYRadians;
                    RotationXRadians = viewPointRotationXRadians;
                    RotationYRadians = viewPointRotationYRadians;
                    attachedLocation = viewPointLocation;
                    StartViewPointLocation = viewPointLocation;
                    StartViewPointRotationXRadians = viewPointRotationXRadians;
                    StartViewPointRotationYRadians = viewPointRotationYRadians;
                }
            }
        }

        public void ChangeCab(TrainCar newCar)
        {
            try
            {
                var mstsLocomotive = newCar as MSTSLocomotive;
                if (PrevCabWasRear != mstsLocomotive.UsingRearCab)
                    RotationYRadians += MathHelper.Pi;
                ActViewPoint = mstsLocomotive.UsingRearCab && mstsLocomotive.HasFront3DCab ? 1 : 0;
                PrevCabWasRear = mstsLocomotive.UsingRearCab;
                SetCameraCar(newCar);
            }
            catch
            {
                Trace.TraceInformation("Change Cab failed");
            }
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(PrevCabWasRear);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            PrevCabWasRear = inf.ReadBoolean();
        }

        public override bool IsUnderground
        {
            get
            {
                // Camera is underground if target (base) is underground or
                // track location is underground. The latter means we switch
                // to cab view instead of putting the camera above the tunnel.
                if (base.IsUnderground)
                    return true;
                var elevationAtCameraTarget = Viewer.Tiles.GetElevation(attachedCar.WorldPosition.WorldLocation);
                return attachedCar.WorldPosition.Location.Y + TerrainAltitudeMargin < elevationAtCameraTarget || attachedCar.CarTunnelData.LengthMOfTunnelAheadFront > 0;
            }
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            base.HandleUserInput(elapsedTime);
            if (UserInput.IsPressed(UserCommand.CameraChange3DCabViewPoint))
                new CameraChange3DCabViewPointCommand(Viewer.Log);
        }

        public void Change3DCabViewPoint(TrainCar car)
        {
            ActViewPoint++;
            if (ActViewPoint >= car.CabViewpoints.Count) ActViewPoint = 0;
            SetCameraCar(car);
        }
    }

    public class HeadOutCamera : NonTrackingCamera
    {
        protected readonly bool Forwards;
        public enum HeadDirection { Forward, Backward }
        protected int CurrentViewpointIndex;
        protected bool PrevCabWasRear;

        // Head-out camera is only possible on the player train.
        public override bool IsAvailable { get { return Viewer.PlayerTrain != null && Viewer.PlayerTrain.Cars.Any(c => c.HeadOutViewpoints.Count > 0); } }
        public override float NearPlane { get { return 0.25f; } }
        public override string Name { get { return Viewer.Catalog.GetString("Head out"); } }

        public HeadOutCamera(Viewer viewer, HeadDirection headDirection)
            : base(viewer)
        {
            Forwards = headDirection == HeadDirection.Forward;
            RotationYRadians = Forwards ? 0 : -MathHelper.Pi;
        }

        protected override List<TrainCar> GetCameraCars()
        {
            // Head-out camera is only possible on the player train.
            return Viewer.PlayerTrain.Cars.Where(c => c.HeadOutViewpoints.Count > 0).ToList();
        }

        protected override void SetCameraCar(TrainCar car)
        {
            base.SetCameraCar(car);
            if (attachedCar.HeadOutViewpoints.Count > 0)
                attachedLocation = attachedCar.HeadOutViewpoints[CurrentViewpointIndex].Location;

            if (!Forwards)
                attachedLocation.X *= -1;
        }

        public void ChangeCab(TrainCar newCar)
        {
            var mstsLocomotive = newCar as MSTSLocomotive;
            if (PrevCabWasRear != mstsLocomotive.UsingRearCab)
                RotationYRadians += MathHelper.Pi;
            CurrentViewpointIndex = mstsLocomotive.UsingRearCab ? 1 : 0;
            PrevCabWasRear = mstsLocomotive.UsingRearCab;
            SetCameraCar(newCar);
        }
    }

    public class CabCamera : NonTrackingCamera
    {
        private readonly SavingProperty<bool> LetterboxProperty;
        protected int[] sideLocation = new int[2];
        public int SideLocation { get { return attachedCar == null? sideLocation[0] : (attachedCar as MSTSLocomotive).UsingRearCab ? sideLocation[1] : sideLocation[0]; } }

        public override Styles Style { get { return Styles.Cab; } }
        // Cab camera is only possible on the player train.
        public override bool IsAvailable { get { return Viewer.PlayerLocomotive != null && (Viewer.PlayerLocomotive.HasFrontCab || Viewer.PlayerLocomotive.HasRearCab); } }
        public override string Name { get { return Viewer.Catalog.GetString("Cab"); } }

        public override bool IsUnderground
        {
            get
            {
                // Camera is underground if target (base) is underground or
                // track location is underground. The latter means we switch
                // to cab view instead of putting the camera above the tunnel.
                if (base.IsUnderground)
                    return true;
                var elevationAtCameraTarget = Viewer.Tiles.GetElevation(attachedCar.WorldPosition.WorldLocation);
                return attachedCar.WorldPosition.Location.Y + TerrainAltitudeMargin < elevationAtCameraTarget || attachedCar.CarTunnelData.LengthMOfTunnelAheadFront > 0;
            }
        }

        public float RotationRatio = 0.00081f;
        public float RotationRatioHorizontal = 0.00081f;

        public CabCamera(Viewer viewer)
            : base(viewer)
        {
            LetterboxProperty = viewer.Settings.GetSavingProperty<bool>("Letterbox2DCab");
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(sideLocation[0]);
            outf.Write(sideLocation[1]);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            sideLocation[0] = inf.ReadInt32();
            sideLocation[1] = inf.ReadInt32();
        }

        public override void Reset()
        {
            FieldOfView = Viewer.Settings.ViewingFOV;
            RotationXRadians = RotationYRadians = XRadians = YRadians = ZRadians = 0;
            Viewer.CabYOffsetPixels = (Viewer.DisplaySize.Y - Viewer.CabHeightPixels) / 2;
            Viewer.CabXOffsetPixels = (Viewer.CabWidthPixels - Viewer.DisplaySize.X) / 2;
            if (attachedCar != null)
            {
                Initialize();
            }
            ScreenChanged();
            OnActivate(true);
        }

        public void Initialize()
        {
            if (Viewer.Settings.Letterbox2DCab)
            {
                float fovFactor = 1f - Math.Max((float)Viewer.CabXLetterboxPixels / Viewer.DisplaySize.X, (float)Viewer.CabYLetterboxPixels / Viewer.DisplaySize.Y);
                FieldOfView = MathHelper.ToDegrees((float)(2 * Math.Atan(fovFactor * Math.Tan(MathHelper.ToRadians(Viewer.Settings.ViewingFOV / 2)))));
            }
            else if (Viewer.CabExceedsDisplayHorizontally <= 0)
            {
                // We must modify FOV to get correct lookout
                    FieldOfView = MathHelper.ToDegrees((float)(2 * Math.Atan((float)Viewer.DisplaySize.Y / Viewer.DisplaySize.X / Viewer.CabTextureInverseRatio * Math.Tan(MathHelper.ToRadians(Viewer.Settings.ViewingFOV / 2)))));
                    RotationRatio = (float)(0.962314f * 2 * Math.Tan(MathHelper.ToRadians(FieldOfView / 2)) / Viewer.DisplaySize.Y);
            }
            else if (Viewer.CabExceedsDisplayHorizontally > 0)
            {
                    var halfFOVHorizontalRadians = (float)(Math.Atan((float)Viewer.DisplaySize.X / Viewer.DisplaySize.Y * Math.Tan(MathHelper.ToRadians(Viewer.Settings.ViewingFOV / 2))));
                    RotationRatioHorizontal = (float)(0.962314f * 2 * Viewer.DisplaySize.X / Viewer.DisplaySize.Y * Math.Tan(MathHelper.ToRadians(Viewer.Settings.ViewingFOV / 2)) / Viewer.DisplaySize.X);
            }
            InitialiseRotation(attachedCar);
            ScreenChanged();
        }

        protected override void OnActivate(bool sameCamera)
        {
            // Cab camera is only possible on the player locomotive.
            SetCameraCar(GetCameraCars().First());
            tiltingLand = false;
            if (Viewer.Simulator.UseSuperElevation || Viewer.Simulator.CarVibrating > 0) tiltingLand = true;
            var car = attachedCar;
            if (car != null && car.Train != null && car.Train.IsTilting == true) tiltingLand = true;
            base.OnActivate(sameCamera);
        }

        protected override List<TrainCar> GetCameraCars()
        {
            // Cab camera is only possible on the player locomotive.
            return new List<TrainCar>(new[] { Viewer.PlayerLocomotive });
        }

        protected override void SetCameraCar(TrainCar car)
        {
            base.SetCameraCar(car);
            if (car != null)
            {
                var loco = car as MSTSLocomotive;
                var sLIndex = (loco.UsingRearCab) ? (int)CabViewType.Rear : (int)CabViewType.Front;
                if (sideLocation[sLIndex] >= loco.CabViewList[sLIndex].ViewPointList.Count)
                    sideLocation[sLIndex] = 0;
                var viewpoint = loco.CabViewList[sLIndex].ViewPointList[sideLocation[sLIndex]];
                attachedLocation = viewpoint.Location;
            }
            InitialiseRotation(attachedCar);
        }

        /// <summary>
        /// Switches to another cab view (e.g. side view).
        /// Applies the inclination of the previous external view due to PanUp() to the new external view. 
        /// </summary>
        void ShiftView(int index)
        {
            var loco = attachedCar as MSTSLocomotive;
            var sLIndex = (loco.UsingRearCab) ? 1 : 0;

            sideLocation[sLIndex] += index;

            var count = (loco.UsingRearCab)
                ? loco.CabViewList[(int)CabViewType.Rear].ViewPointList.Count
                : loco.CabViewList[(int)CabViewType.Front].ViewPointList.Count;
            // Wrap around
            if (sideLocation[sLIndex] < 0)
                sideLocation[sLIndex] = count - 1;
            else if (sideLocation[sLIndex] >= count)
                sideLocation[sLIndex] = 0;

            SetCameraCar(attachedCar);
        }

        /// <summary>
        /// Where cabview image doesn't fit the display exactly, this method mimics the player looking up
        /// and pans the image down to reveal details at the top of the cab.
        /// The external view also moves down by a similar amount.
        /// </summary>
        void PanUp(bool up, float speed)
        {
            int max = 0;
            int min = Viewer.DisplaySize.Y - Viewer.CabHeightPixels - 2 * Viewer.CabYLetterboxPixels; // -ve value
            int cushionPixels = 40;
            int slowFactor = 4;

            // Cushioned approach to limits of travel. Within 40 pixels, travel at 1/4 speed
            if (up && Math.Abs(Viewer.CabYOffsetPixels - max) < cushionPixels)
                speed /= slowFactor;
            if (!up && Math.Abs(Viewer.CabYOffsetPixels - min) < cushionPixels)
                speed /= slowFactor;
            Viewer.CabYOffsetPixels += (up) ? (int)speed : -(int)speed;
            // Enforce limits to travel
            if (Viewer.CabYOffsetPixels >= max)
            {
                Viewer.CabYOffsetPixels = max;
                return;
            }
            if (Viewer.CabYOffsetPixels <= min)
            {
                Viewer.CabYOffsetPixels = min;
                return;
            }
            // Adjust inclination (up/down angle) of external view to match.
            var viewSpeed = (int)speed * RotationRatio; // factor found by trial and error.
            RotationXRadians -= (up) ? viewSpeed : -viewSpeed;
        }

        /// <summary>
        /// Where cabview image doesn't fit the display exactly (cabview image "larger" than display, this method mimics the player looking left and right
        /// and pans the image left/right to reveal details at the sides of the cab.
        /// The external view also moves sidewards by a similar amount.
        /// </summary>
        void ScrollRight (bool right, float speed)
        {
            int min = 0;
            int max = Viewer.CabWidthPixels - Viewer.DisplaySize.X - 2 * Viewer.CabXLetterboxPixels; // -ve value
            int cushionPixels = 40;
            int slowFactor = 4;

            // Cushioned approach to limits of travel. Within 40 pixels, travel at 1/4 speed
            if (right && Math.Abs(Viewer.CabXOffsetPixels - max) < cushionPixels)
                speed /= slowFactor;
            if (!right && Math.Abs(Viewer.CabXOffsetPixels - min) < cushionPixels)
                speed /= slowFactor;
            Viewer.CabXOffsetPixels += (right) ? (int)speed : -(int)speed;
            // Enforce limits to travel
            if (Viewer.CabXOffsetPixels >= max)
            {
                Viewer.CabXOffsetPixels = max;
                return;
            }
            if (Viewer.CabXOffsetPixels <= min)
            {
                Viewer.CabXOffsetPixels = min;
                return;
            }
            // Adjust direction (right/left angle) of external view to match.
            var viewSpeed = (int)speed * RotationRatioHorizontal; // factor found by trial and error.
            RotationYRadians += (right) ? viewSpeed : -viewSpeed;
        }

        /// <summary>
        /// Sets direction for view out of cab front window. Also called when toggling between full screen and windowed.
        /// </summary>
        /// <param name="attachedCar"></param>
        public void InitialiseRotation(TrainCar attachedCar)
        {
            if (attachedCar == null) return;

            var loco = attachedCar as MSTSLocomotive;
            var viewpoint = (loco.UsingRearCab)
            ? loco.CabViewList[(int)CabViewType.Rear].ViewPointList[sideLocation[1]]
            : loco.CabViewList[(int)CabViewType.Front].ViewPointList[sideLocation[0]];

            RotationXRadians = MathHelper.ToRadians(viewpoint.StartDirection.X) - RotationRatio * (Viewer.CabYOffsetPixels + Viewer.CabExceedsDisplay / 2);
            RotationYRadians = MathHelper.ToRadians(viewpoint.StartDirection.Y) - RotationRatioHorizontal * (-Viewer.CabXOffsetPixels + Viewer.CabExceedsDisplayHorizontally / 2); ;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            var speedFactor = 500;  // Gives a fairly smart response.
            var speed = speedFactor * elapsedTime.RealSeconds; // Independent of framerate

            if (UserInput.IsPressed(UserCommand.CameraPanLeft))
                ShiftView(+1);
            if (UserInput.IsPressed(UserCommand.CameraPanRight))
                ShiftView(-1);
            if (UserInput.IsDown(UserCommand.CameraPanUp))
                PanUp(true, speed);
            if (UserInput.IsDown(UserCommand.CameraPanDown))
                PanUp(false, speed);
            if (UserInput.IsDown(UserCommand.CameraScrollRight))
                ScrollRight(true, speed);
            if (UserInput.IsDown(UserCommand.CameraScrollLeft))
                ScrollRight(false, speed);
            if (UserInput.IsPressed(UserCommand.CameraToggleLetterboxCab))
            {
                LetterboxProperty.Value = !LetterboxProperty.Value;
                Viewer.AdjustCabHeight(Viewer.DisplaySize.X, Viewer.DisplaySize.Y);
                if (attachedCar != null)
                    Initialize();
            }
        }
    }

    public class TracksideCamera : LookAtCamera
    {
        protected const int MaximumDistance = 100;
        protected const float SidewaysScale = MaximumDistance / 10;
        // Heights above the terrain for the camera.
        protected const float CameraAltitude = 2;
        // Height above the coordinate center of target.
        protected const float TargetAltitude = TerrainAltitudeMargin;

        protected TrainCar attachedCar;
        public override TrainCar AttachedCar { get { return attachedCar; } }
        public override string Name { get { return Viewer.Catalog.GetString("Trackside"); } }

        protected TrainCar LastCheckCar;
        protected WorldLocation TrackCameraLocation;
        protected float CameraAltitudeOffset;

        public override bool IsUnderground
        {
            get
            {
                // Camera is underground if target (base) is underground or
                // track location is underground. The latter means we switch
                // to cab view instead of putting the camera above the tunnel.
                if (base.IsUnderground)
                    return true;
                if (TrackCameraLocation == WorldLocation.None) return false;
                var elevationAtCameraTarget = Viewer.Tiles.GetElevation(TrackCameraLocation);
                return TrackCameraLocation.Location.Y + TerrainAltitudeMargin < elevationAtCameraTarget;
            }
        }

        public TracksideCamera(Viewer viewer)
            : base(viewer)
        {
        }

        public override void Reset()
        {
            base.Reset();
            cameraLocation.Location.Y -= CameraAltitudeOffset;
            CameraAltitudeOffset = 0;
        }

        protected override void OnActivate(bool sameCamera)
        {
            if (sameCamera)
            {
                cameraLocation.TileX = 0;
                cameraLocation.TileZ = 0;
            }
            if (attachedCar == null || attachedCar.Train != Viewer.SelectedTrain)
            {
                if (Viewer.SelectedTrain.MUDirection != Direction.Reverse)
                    attachedCar = Viewer.SelectedTrain.Cars.First();
                else
                    attachedCar = Viewer.SelectedTrain.Cars.Last();
            }
            base.OnActivate(sameCamera);
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            RotationYRadians = -ORTSMath.MatrixToYAngle(XnaView);
            var speed = GetSpeed(elapsedTime);

            if (UserInput.IsDown(UserCommand.CameraPanUp))
            {
                CameraAltitudeOffset += speed;
                cameraLocation.Location.Y += speed;
            }
            if (UserInput.IsDown(UserCommand.CameraPanDown))
            {
                CameraAltitudeOffset -= speed;
                cameraLocation.Location.Y -= speed;
                if (CameraAltitudeOffset < 0)
                {
                    cameraLocation.Location.Y -= CameraAltitudeOffset;
                    CameraAltitudeOffset = 0;
                }
            }
            if (UserInput.IsDown(UserCommand.CameraPanRight)) PanRight(speed);
            if (UserInput.IsDown(UserCommand.CameraPanLeft)) PanRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraZoomIn)) ZoomIn(speed * 2);
            if (UserInput.IsDown(UserCommand.CameraZoomOut)) ZoomIn(-speed * 2);

            ZoomByMouseWheel(speed);

            var trainCars = Viewer.SelectedTrain.Cars;
            if (UserInput.IsPressed(UserCommand.CameraCarNext))
                attachedCar = attachedCar == trainCars.First() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) - 1];
            else if (UserInput.IsPressed(UserCommand.CameraCarPrevious))
                attachedCar = attachedCar == trainCars.Last() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) + 1];
            else if (UserInput.IsPressed(UserCommand.CameraCarFirst))
                attachedCar = trainCars.First();
            else if (UserInput.IsPressed(UserCommand.CameraCarLast))
                attachedCar = trainCars.Last();
        }

        public override void Update(ElapsedTime elapsedTime)
        {
            bool trainForwards;
            var train = PrepUpdate( out trainForwards);

            // Train is close enough if the last car we used is part of the same train and still close enough.
            var trainClose = (LastCheckCar != null) && (LastCheckCar.Train == train) && (WorldLocation.GetDistance2D(LastCheckCar.WorldPosition.WorldLocation, cameraLocation).Length() < MaximumDistance);

            // Otherwise, let's check out every car and remember which is the first one close enough for next time.
            if (!trainClose)
            {
                foreach (var car in train.Cars)
                {
                    if (WorldLocation.GetDistance2D(car.WorldPosition.WorldLocation, cameraLocation).Length() < MaximumDistance)
                    {
                        LastCheckCar = car;
                        trainClose = true;
                        break;
                    }
                }
            }

            // Switch to new position.
            if (!trainClose || (TrackCameraLocation == WorldLocation.None))
            {
                var tdb = trainForwards ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
                var newLocation = GoToNewLocation(ref tdb, train, trainForwards);
                newLocation.Normalize();

                var newLocationElevation = Viewer.Tiles.GetElevation(newLocation);
                cameraLocation = newLocation;
                cameraLocation.Location.Y = Math.Max(tdb.Y, newLocationElevation) + CameraAltitude + CameraAltitudeOffset;
            }

            targetLocation.Location.Y += TargetAltitude;
            UpdateListener();
        }

        protected Train PrepUpdate (out bool trainForwards)
        {
            var train = attachedCar.Train;

            // TODO: What is this code trying to do?
            //if (train != Viewer.PlayerTrain && train.LeadLocomotive == null) train.ChangeToNextCab();
            trainForwards = true;
            if (train.LeadLocomotive != null)
                //TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                // To achieve the same result with other means, without flipping trainset physics, maybe the line should be changed
                trainForwards = (train.LeadLocomotive.SpeedMpS >= 0) ^ train.LeadLocomotive.Flipped ^ ((MSTSLocomotive)train.LeadLocomotive).UsingRearCab;
            else if (Viewer.PlayerLocomotive != null && train.IsActualPlayerTrain)
                trainForwards = (Viewer.PlayerLocomotive.SpeedMpS >= 0) ^ Viewer.PlayerLocomotive.Flipped ^ ((MSTSLocomotive)Viewer.PlayerLocomotive).UsingRearCab;

            targetLocation = attachedCar.WorldPosition.WorldLocation;

            return train;
        }


        protected WorldLocation GoToNewLocation ( ref Traveller tdb, Train train, bool trainForwards)
        {
            tdb.Move(MaximumDistance * 0.75f);
            var newLocation = tdb.WorldLocation;
            TrackCameraLocation = new WorldLocation(newLocation);
            var directionForward = WorldLocation.GetDistance((trainForwards ? train.FirstCar : train.LastCar).WorldPosition.WorldLocation, newLocation);
            if (Viewer.Random.Next(2) == 0)
            {
                newLocation.Location.X += -directionForward.Z / SidewaysScale; // Use swapped -X and Z to move to the left of the track.
                newLocation.Location.Z += directionForward.X / SidewaysScale;
            }
            else
            {
                newLocation.Location.X += directionForward.Z / SidewaysScale; // Use swapped X and -Z to move to the right of the track.
                newLocation.Location.Z += -directionForward.X / SidewaysScale;
            }
            return newLocation;
        }

        protected virtual void PanRight(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.X += speed;
            XRadians += movement.X;
            MoveCamera(movement);
        }

        protected override void ZoomIn(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.Z += speed;
            ZRadians += movement.Z;
            MoveCamera(movement);
        }

    }

    public class SpecialTracksideCamera : TracksideCamera
    {
        const int MaximumSpecialPointDistance = 300;
        const float PlatformOffsetM = 3.3f;
        protected bool SpecialPointFound = false;
        const float CheckIntervalM = 50f; // every 50 meters it is checked wheter there is a near special point
        protected float DistanceRunM = 0f; // distance run since last check interval
        protected bool FirstUpdateLoop = true; // first update loop

        const float MaxDistFromRoadCarM = 100.0f; // maximum distance of train traveller to spawned roadcar
        protected RoadCar NearRoadCar;
        protected bool RoadCarFound;

        public SpecialTracksideCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected override void OnActivate(bool sameCamera)
        {
            DistanceRunM = 0;
            base.OnActivate(sameCamera);
            FirstUpdateLoop = Math.Abs(AttachedCar.Train.SpeedMpS) <= 0.2f || sameCamera;
            if (sameCamera)
            {
                SpecialPointFound = false;
                TrackCameraLocation = WorldLocation.None;
                RoadCarFound = false;
                NearRoadCar = null;
            }
        }

        public override void Update(ElapsedTime elapsedTime)
        {
            bool trainForwards;
            var train = PrepUpdate(out trainForwards);

            if (RoadCarFound)
            {
                // camera location is always behind the near road car, at a distance which increases at increased speed
                if (NearRoadCar != null && NearRoadCar.Travelled < NearRoadCar.Spawner.Length - 10f)
                {
                    var traveller = new Traveller(NearRoadCar.FrontTraveller);
                    traveller.Move(-2.5f - 0.15f * NearRoadCar.Length - NearRoadCar.Speed * 0.5f);
                    cameraLocation = TrackCameraLocation = new WorldLocation(traveller.WorldLocation);
                    cameraLocation.Location.Y += 1.8f;
                }
                else NearRoadCar = null;
            }

            // Train is close enough if the last car we used is part of the same train and still close enough.
            var trainClose = (LastCheckCar != null) && (LastCheckCar.Train == train) && (WorldLocation.GetDistance2D(LastCheckCar.WorldPosition.WorldLocation, cameraLocation).Length() < (SpecialPointFound ? MaximumSpecialPointDistance * 0.8f : MaximumDistance));

            // Otherwise, let's check out every car and remember which is the first one close enough for next time.
            if (!trainClose)
            {
                // if camera is not close to LastCheckCar, verify if it is still close to another car of the train
                foreach (var car in train.Cars)
                {
                    if (LastCheckCar != null && car == LastCheckCar &&
                        WorldLocation.GetDistance2D(car.WorldPosition.WorldLocation, cameraLocation).Length() < (SpecialPointFound ? MaximumSpecialPointDistance * 0.8f : MaximumDistance))
                    {
                        trainClose = true;
                        break;
                    }
                    else if (WorldLocation.GetDistance2D(car.WorldPosition.WorldLocation, cameraLocation).Length() <
                        (SpecialPointFound && NearRoadCar != null && train.SpeedMpS > NearRoadCar.Speed + 10 ? MaximumSpecialPointDistance * 0.8f : MaximumDistance))
                    {
                        LastCheckCar = car;
                        trainClose = true;
                        break;
                    }
                }
                if (!trainClose)
                    LastCheckCar = null;
            }
            if (RoadCarFound && NearRoadCar == null)
            {
                RoadCarFound = false;
                SpecialPointFound = false;
                trainClose = false;
            }
            var trySpecial = false;
            DistanceRunM += elapsedTime.ClockSeconds * train.SpeedMpS;
            // when camera not at a special point, try every CheckIntervalM meters if there is a new special point nearby
            if (Math.Abs(DistanceRunM) >= CheckIntervalM)
            {
                DistanceRunM = 0;
                if (!SpecialPointFound && trainClose) trySpecial = true;
            }
            // Switch to new position.
            if (!trainClose || (TrackCameraLocation == WorldLocation.None) || trySpecial)
            {
                SpecialPointFound = false;
                bool platformFound = false;
                NearRoadCar = null;
                RoadCarFound = false;
                Traveller tdb;
                // At first update loop camera location may be also behind train front (e.g. platform at start of activity)
                if (FirstUpdateLoop)
                    tdb = trainForwards ? new Traveller(train.RearTDBTraveller) : new Traveller(train.FrontTDBTraveller, Traveller.TravellerDirection.Backward);
                else
                    tdb = trainForwards ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
                var newLocation = WorldLocation.None;

                int tcSectionIndex;
                int routeIndex;
                Train.TCSubpathRoute thisRoute = null;
                // search for near platform in fast way, using TCSection data
                if (trainForwards && train.ValidRoute[0] != null)
                {
                    thisRoute = train.ValidRoute[0];
                }
                else if (!trainForwards && train.ValidRoute[1] != null)
                {
                    thisRoute = train.ValidRoute[1];
                }

                // Search for platform
                if (thisRoute != null)
                {
                    if (FirstUpdateLoop)
                    {
                        tcSectionIndex = trainForwards ? train.PresentPosition[1].TCSectionIndex : train.PresentPosition[0].TCSectionIndex;
                        routeIndex = trainForwards ? train.PresentPosition[1].RouteListIndex : train.PresentPosition[0].RouteListIndex;
                    }
                    else
                    {
                        tcSectionIndex = trainForwards ? train.PresentPosition[0].TCSectionIndex : train.PresentPosition[1].TCSectionIndex;
                        routeIndex = trainForwards ? train.PresentPosition[0].RouteListIndex : train.PresentPosition[1].RouteListIndex;
                    }
                    if (routeIndex != -1)
                    {
                        float distanceToViewingPoint = 0;
                        TrackCircuitSection TCSection = train.signalRef.TrackCircuitList[tcSectionIndex];
                        float distanceToAdd = TCSection.Length;
                        float incrDistance;
                        if (FirstUpdateLoop)
                            incrDistance = trainForwards ? -train.PresentPosition[1].TCOffset : -TCSection.Length + train.PresentPosition[0].TCOffset;
                        else
                            incrDistance = trainForwards ? -train.PresentPosition[0].TCOffset : -TCSection.Length + train.PresentPosition[1].TCOffset;
                        // scanning route in direction of train, searching for a platform
                        while (incrDistance < MaximumSpecialPointDistance * 0.7f)
                        {
                            foreach (int platformIndex in TCSection.PlatformIndex)
                            {
                                PlatformDetails thisPlatform = train.signalRef.PlatformDetailsList[platformIndex];
                                if (thisPlatform.TCOffset[0, thisRoute[routeIndex].Direction] + incrDistance < MaximumSpecialPointDistance * 0.7f
                                    && (thisPlatform.TCOffset[0, thisRoute[routeIndex].Direction] + incrDistance > 0 || FirstUpdateLoop))
                                {
                                    // platform found, compute distance to viewing point
                                    distanceToViewingPoint = Math.Min(MaximumSpecialPointDistance * 0.7f,
                                        incrDistance + thisPlatform.TCOffset[0, thisRoute[routeIndex].Direction] + thisPlatform.Length * 0.7f);
                                    if (FirstUpdateLoop && Math.Abs(train.SpeedMpS) <= 0.2f) distanceToViewingPoint =
                                            Math.Min(distanceToViewingPoint, train.Length * 0.95f);
                                    tdb.Move(distanceToViewingPoint);
                                    newLocation = tdb.WorldLocation;
                                    // shortTrav is used to state directions, to correctly identify in which direction (left or right) to move
                                    //the camera from center of track to the platform at its side
                                    Traveller shortTrav;
                                    PlatformItem platformItem = Viewer.Simulator.TDB.TrackDB.TrItemTable[thisPlatform.PlatformFrontUiD] as PlatformItem;
                                    if (platformItem == null) continue;
                                    shortTrav = new Traveller(Viewer.Simulator.TSectionDat, Viewer.Simulator.TDB.TrackDB.TrackNodes, platformItem.TileX,
                                        platformItem.TileZ, platformItem.X, platformItem.Z, Traveller.TravellerDirection.Forward);
                                    var distanceToViewingPoint1 = shortTrav.DistanceTo(newLocation.TileX, newLocation.TileZ,
                                        newLocation.Location.X, newLocation.Location.Y, newLocation.Location.Z, thisPlatform.Length);
                                    if (distanceToViewingPoint1 == -1) //try other direction
                                    {
                                        shortTrav.ReverseDirection();
                                        distanceToViewingPoint1 = shortTrav.DistanceTo(newLocation.TileX, newLocation.TileZ,
                                        newLocation.Location.X, newLocation.Location.Y, newLocation.Location.Z, thisPlatform.Length);
                                        if (distanceToViewingPoint1 == -1) continue;
                                    }
                                    platformFound = true;
                                    SpecialPointFound = true;
                                    trainClose = false;
                                    LastCheckCar = FirstUpdateLoop ^ trainForwards ? train.Cars.First() : train.Cars.Last();
                                    shortTrav.Move(distanceToViewingPoint1);
                                    // moving newLocation to platform at side of track
                                    newLocation.Location.X += (PlatformOffsetM + Viewer.Simulator.RouteTrackGaugeM / 2) * (float)Math.Cos(shortTrav.RotY) *
                                        (thisPlatform.PlatformSide[1] ? 1 : -1);
                                    newLocation.Location.Z += -(PlatformOffsetM + Viewer.Simulator.RouteTrackGaugeM / 2) * (float)Math.Sin(shortTrav.RotY) *
                                        (thisPlatform.PlatformSide[1] ? 1 : -1);
                                    TrackCameraLocation = new WorldLocation(newLocation);
                                    break;
                                }
                            }
                            if (platformFound) break;
                            if (routeIndex < thisRoute.Count - 1)
                            {
                                incrDistance += distanceToAdd;
                                routeIndex++;
                                TCSection = train.signalRef.TrackCircuitList[thisRoute[routeIndex].TCSectionIndex];
                                distanceToAdd = TCSection.Length;
                            }
                            else break;
                        }
                    }
                }

                if (!SpecialPointFound)
                {

                    // Search for near visible spawned car
                    var minDistanceM = 10000.0f;
                    NearRoadCar = null;
                    foreach (RoadCar visibleCar in Viewer.World.RoadCars.VisibleCars)
                    {
                        // check for direction
                        if (Math.Abs(visibleCar.FrontTraveller.RotY - train.FrontTDBTraveller.RotY) < 0.5f)
                        {
                            var traveller = visibleCar.Speed > Math.Abs(train.SpeedMpS) ^ trainForwards ?
                                train.RearTDBTraveller : train.FrontTDBTraveller;
                            var distanceTo = WorldLocation.GetDistance2D(visibleCar.FrontTraveller.WorldLocation, traveller.WorldLocation).Length();
                            if (distanceTo < MaxDistFromRoadCarM && Math.Abs(visibleCar.FrontTraveller.WorldLocation.Location.Y - traveller.WorldLocation.Location.Y) < 30.0f)
                            {
                                if (visibleCar.Travelled < visibleCar.Spawner.Length - 30)
                                {
                                    minDistanceM = distanceTo;
                                    NearRoadCar = visibleCar;
                                    break;
                                }
                            }
                        }
                    }
                    if (NearRoadCar != null)
                    // readcar found
                    {
                        SpecialPointFound = true;
                        RoadCarFound = true;
                        // CarriesCamera needed to increase distance of following car
                        NearRoadCar.CarriesCamera = true;
                        var traveller = new Traveller(NearRoadCar.FrontTraveller);
                        traveller.Move(-2.5f - 0.15f * NearRoadCar.Length);
                        TrackCameraLocation = newLocation = new WorldLocation(traveller.WorldLocation);
                        LastCheckCar = trainForwards ? train.Cars.First() : train.Cars.Last();
                    }
                }

                if (!SpecialPointFound)
                {
                    // try to find near level crossing then
                    LevelCrossingItem newLevelCrossingItem = LevelCrossingItem.None;
                    float FrontDist = -1;
                    newLevelCrossingItem = Viewer.Simulator.LevelCrossings.SearchNearLevelCrossing(train, MaximumSpecialPointDistance * 0.7f, trainForwards, out FrontDist);
                    if (newLevelCrossingItem != LevelCrossingItem.None)
                    {
                        SpecialPointFound = true;
                        trainClose = false;
                        LastCheckCar = trainForwards ? train.Cars.First() : train.Cars.Last();
                        newLocation = newLevelCrossingItem.Location;
                        TrackCameraLocation = new WorldLocation(newLocation);
                        Traveller roadTraveller;
                        // decide randomly at which side of the level crossing the camera will be located
                        if (Viewer.Random.Next(2) == 0)
                        {
                            roadTraveller = new Traveller(Viewer.Simulator.TSectionDat, Viewer.Simulator.RDB.RoadTrackDB.TrackNodes, Viewer.Simulator.RDB.RoadTrackDB.TrackNodes[newLevelCrossingItem.TrackIndex],
                                newLocation.TileX, newLocation.TileZ, newLocation.Location.X, newLocation.Location.Z, Traveller.TravellerDirection.Forward);
                        }
                        else
                        {
                            roadTraveller = new Traveller(Viewer.Simulator.TSectionDat, Viewer.Simulator.RDB.RoadTrackDB.TrackNodes, Viewer.Simulator.RDB.RoadTrackDB.TrackNodes[newLevelCrossingItem.TrackIndex],
                                newLocation.TileX, newLocation.TileZ, newLocation.Location.X, newLocation.Location.Z, Traveller.TravellerDirection.Backward);
                        }
                        roadTraveller.Move(12.5f);
                        tdb.Move(FrontDist);
                        newLocation = roadTraveller.WorldLocation;
                    }
                }
                if (!SpecialPointFound && !trainClose)
                {
                    tdb = trainForwards ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward); // return to standard
                    newLocation = GoToNewLocation(ref tdb, train, trainForwards);
                 }

                if (newLocation != WorldLocation.None && !trainClose)
                {
                    newLocation.Normalize();
                    cameraLocation = newLocation;
                    if (!RoadCarFound)
                    {
                        var newLocationElevation = Viewer.Tiles.GetElevation(newLocation);
                        cameraLocation.Location.Y = newLocationElevation;
                        TrackCameraLocation = new WorldLocation(cameraLocation);
                        cameraLocation.Location.Y = Math.Max(tdb.Y, newLocationElevation) + CameraAltitude + CameraAltitudeOffset + (platformFound ? 0.35f : 0.0f);
                    }
                    else
                    {
                        TrackCameraLocation = new WorldLocation(cameraLocation);
                        cameraLocation.Location.Y += 1.8f;
                    }
                    DistanceRunM = 0f;
                }
            }

            targetLocation.Location.Y += TargetAltitude;
            FirstUpdateLoop = false;
            UpdateListener();
        }

        protected override void ZoomIn(float speed)
        {
            if (!RoadCarFound)
            {
                var movement = new Vector3(0, 0, 0);
                movement.Z += speed;
                ZRadians += movement.Z;
                MoveCamera(movement);
            }
            else
            {
                NearRoadCar.ChangeSpeed(speed);
            }
        }
    }
}
