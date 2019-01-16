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

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Simulation.Signalling;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
            OnActivate(Viewer.Camera == this);
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
            var farPlaneDistance = SkyConstants.skyRadius + 100;  // so far the sky is the biggest object in view
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
            if (UserInput.IsDown(UserCommands.CameraMoveFast))
                speed *= SpeedFactorFastSlow;
            if (UserInput.IsDown(UserCommands.CameraMoveSlow))
                speed /= SpeedFactorFastSlow;
            return speed;
        }

        protected virtual void ZoomIn(float speed)
        {
        }

        // <CJComment> To Do: Add a way to record this zoom operation. </CJComment>
        protected void ZoomByMouseWheel(float speed)
        {
            // Will not zoom-in-out when help windows is up.
            // TODO: Propery input processing through WindowManager.
            if (UserInput.IsMouseWheelChanged && !Viewer.HelpWindow.Visible)
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
//                if (targetLocation.Location.Y + TerrainAltitudeMargin < elevationAtTarget)
//                    Trace.TraceInformation("Y {0}, TerrainAltitudeMargin {1}, elevationAtTarget {2}", targetLocation.Location.Y,
//                        TerrainAltitudeMargin, elevationAtTarget);
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
            if (UserInput.IsDown(UserCommands.CameraMoveSlow))
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
            if (UserInput.IsDown(UserCommands.CameraZoomIn) || UserInput.IsDown(UserCommands.CameraZoomOut))
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
            if (UserInput.IsDown(UserCommands.CameraPanRight)) PanRight(speed);
            if (UserInput.IsDown(UserCommands.CameraPanLeft)) PanRight(-speed);
            if (UserInput.IsDown(UserCommands.CameraPanUp)) PanUp(speed);
            if (UserInput.IsDown(UserCommands.CameraPanDown)) PanUp(-speed);
            if (UserInput.IsDown(UserCommands.CameraZoomIn)) ZoomIn(speed * ZoomFactor);
            if (UserInput.IsDown(UserCommands.CameraZoomOut)) ZoomIn(-speed * ZoomFactor);
            ZoomByMouseWheel(speed);

            if (UserInput.IsPressed(UserCommands.CameraPanRight) || UserInput.IsPressed(UserCommands.CameraPanLeft))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraPanRight) || UserInput.IsReleased(UserCommands.CameraPanLeft))
                new CameraXCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, XRadians);

            if (UserInput.IsPressed(UserCommands.CameraPanUp) || UserInput.IsPressed(UserCommands.CameraPanDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraPanUp) || UserInput.IsReleased(UserCommands.CameraPanDown))
                new CameraYCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, YRadians);

            if (UserInput.IsPressed(UserCommands.CameraZoomIn) || UserInput.IsPressed(UserCommands.CameraZoomOut))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraZoomIn) || UserInput.IsReleased(UserCommands.CameraZoomOut))
                new CameraZCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, ZRadians);

            speed *= SpeedAdjustmentForRotation;
            RotateByMouse();

            // Rotate camera
            if (UserInput.IsDown(UserCommands.CameraRotateUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommands.CameraRotateDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommands.CameraRotateLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommands.CameraRotateRight)) RotateRight(speed);

            // Support for replaying camera rotation movements
            if (UserInput.IsPressed(UserCommands.CameraRotateUp) || UserInput.IsPressed(UserCommands.CameraRotateDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraRotateUp) || UserInput.IsReleased(UserCommands.CameraRotateDown))
                new CameraRotateUpDownCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationXRadians);

            if (UserInput.IsPressed(UserCommands.CameraRotateLeft) || UserInput.IsPressed(UserCommands.CameraRotateRight))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraRotateLeft) || UserInput.IsReleased(UserCommands.CameraRotateRight))
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
        }

        protected virtual bool IsCameraFlipped()
        {
            return false;
        }

        protected void MoveCar()
        {
            if (UserInput.IsPressed(UserCommands.CameraCarNext))
                new NextCarCommand(Viewer.Log);
            else if (UserInput.IsPressed(UserCommands.CameraCarPrevious))
                new PreviousCarCommand(Viewer.Log);
            else if (UserInput.IsPressed(UserCommands.CameraCarFirst))
                new FirstCarCommand(Viewer.Log);
            else if (UserInput.IsPressed(UserCommands.CameraCarLast))
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

        public void FirstCar()
        {
            var trainCars = GetCameraCars();
            SetCameraCar(trainCars.First());
        }

        public void LastCar()
        {
            var trainCars = GetCameraCars();
            SetCameraCar(trainCars.Last());
        }

        public void UpdateLocation()
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
            lookAtPosition = Vector3.Transform(lookAtPosition, attachedCar.WorldPosition.XNAMatrix);
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

        public override bool IsUnderground
        {
            get
            {
                var elevationAtTrain = Viewer.Tiles.GetElevation(attachedCar.WorldPosition.WorldLocation);
                var elevationAtCamera = Viewer.Tiles.GetElevation(cameraLocation);
                return attachedCar.WorldPosition.WorldLocation.Location.Y + TerrainAltitudeMargin < elevationAtTrain || cameraLocation.Location.Y + TerrainAltitudeMargin < elevationAtCamera;
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
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            PositionDistance = inf.ReadSingle();
            PositionXRadians = inf.ReadSingle();
            PositionYRadians = inf.ReadSingle();
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
            if (attachedCar == null || attachedCar.Train != Viewer.SelectedTrain)
            {
                if (Front)
                    SetCameraCar(GetCameraCars().First());
                else
                    SetCameraCar(GetCameraCars().Last());
            }
            base.OnActivate(sameCamera);
        }

        protected override bool IsCameraFlipped()
        {
            return attachedCar.Flipped;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            MoveCar();

            var speed = GetSpeed(elapsedTime);

            if (UserInput.IsDown(UserCommands.CameraZoomOut)) ZoomIn(speed * ZoomFactor);
            if (UserInput.IsDown(UserCommands.CameraZoomIn)) ZoomIn(-speed * ZoomFactor);
            ZoomByMouseWheel(speed);

            if (UserInput.IsPressed(UserCommands.CameraZoomOut) || UserInput.IsPressed(UserCommands.CameraZoomIn))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraZoomOut) || UserInput.IsReleased(UserCommands.CameraZoomIn))
                new TrackingCameraZCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, PositionDistance);

            speed = GetSpeed(elapsedTime) * SpeedAdjustmentForRotation;

            // Pan camera
            if (UserInput.IsDown(UserCommands.CameraPanUp)) PanUp(speed);
            if (UserInput.IsDown(UserCommands.CameraPanDown)) PanUp(-speed);
            if (UserInput.IsDown(UserCommands.CameraPanLeft)) PanRight(speed);
            if (UserInput.IsDown(UserCommands.CameraPanRight)) PanRight(-speed);

            // Support for replaying camera pan movements
            if (UserInput.IsPressed(UserCommands.CameraPanUp) || UserInput.IsPressed(UserCommands.CameraPanDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraPanUp) || UserInput.IsReleased(UserCommands.CameraPanDown))
                new TrackingCameraXCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, PositionXRadians);

            if (UserInput.IsPressed(UserCommands.CameraPanLeft) || UserInput.IsPressed(UserCommands.CameraPanRight))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraPanLeft) || UserInput.IsReleased(UserCommands.CameraPanRight))
                new TrackingCameraYCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, PositionYRadians);

            RotateByMouse();

            // Rotate camera
            if (UserInput.IsDown(UserCommands.CameraRotateUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommands.CameraRotateDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommands.CameraRotateLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommands.CameraRotateRight)) RotateRight(speed);

            // Support for replaying camera rotation movements
            if (UserInput.IsPressed(UserCommands.CameraRotateUp) || UserInput.IsPressed(UserCommands.CameraRotateDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraRotateUp) || UserInput.IsReleased(UserCommands.CameraRotateDown))
                new CameraRotateUpDownCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationXRadians);

            if (UserInput.IsPressed(UserCommands.CameraRotateLeft) || UserInput.IsPressed(UserCommands.CameraRotateRight))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.CameraRotateLeft) || UserInput.IsReleased(UserCommands.CameraRotateRight))
                new CameraRotateLeftRightCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationYRadians);
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
            attachedLocation.Z += attachedCar.CarLengthM / 2.0f * (Front ? 1 : -1);

            // Update location of camera
            UpdateLocation();
            UpdateListener();
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
            if (UserInput.IsDown(UserCommands.CameraRotateUp) || UserInput.IsDown(UserCommands.CameraPanUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommands.CameraRotateDown) || UserInput.IsDown(UserCommands.CameraPanDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommands.CameraRotateLeft) || UserInput.IsDown(UserCommands.CameraPanLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommands.CameraRotateRight) || UserInput.IsDown(UserCommands.CameraPanRight)) RotateRight(speed);

            // Zoom
            ZoomByMouseWheel(speed);

            // Support for replaying camera rotation movements
            if (UserInput.IsPressed(UserCommands.CameraRotateUp) || UserInput.IsPressed(UserCommands.CameraRotateDown)
                || UserInput.IsPressed(UserCommands.CameraPanUp) || UserInput.IsPressed(UserCommands.CameraPanDown))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsReleased(UserCommands.CameraRotateUp) || UserInput.IsReleased(UserCommands.CameraRotateDown)
                || UserInput.IsReleased(UserCommands.CameraPanUp) || UserInput.IsReleased(UserCommands.CameraPanDown))
                new CameraRotateUpDownCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationXRadians);

            if (UserInput.IsPressed(UserCommands.CameraRotateLeft) || UserInput.IsPressed(UserCommands.CameraRotateRight)
                || UserInput.IsPressed(UserCommands.CameraPanLeft) || UserInput.IsPressed(UserCommands.CameraPanRight))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsReleased(UserCommands.CameraRotateLeft) || UserInput.IsReleased(UserCommands.CameraRotateRight)
                || UserInput.IsReleased(UserCommands.CameraPanLeft) || UserInput.IsReleased(UserCommands.CameraPanRight))
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

    }

    public class InsideThreeDimCamera : NonTrackingCamera
    {
        public override float NearPlane { get { return 0.1f; } }

        public InsideThreeDimCamera(Viewer viewer)
            : base(viewer)
        {
        }

        public PassengerViewPoint viewPoint = null;

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
            if (UserInput.IsDown(UserCommands.CameraPanUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommands.CameraPanDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommands.CameraPanLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommands.CameraPanRight)) RotateRight(speed);
            float x = 0.0f, y = 0.0f, z = 0.0f;

            // Move camera
            if (UserInput.IsDown(UserCommands.CameraZoomIn))
            {
                z = speed * 5;
                MoveCameraXYZ(0, 0, z);

            }
            if (UserInput.IsDown(UserCommands.CameraZoomOut))
            {
                z = speed * -5;
                MoveCameraXYZ(0, 0, z);
            }
            // Move camera
            if (UserInput.IsDown(UserCommands.CameraRotateUp))
            {
                y = speed / 2;
                MoveCameraXYZ(0, y, 0);

            }
            if (UserInput.IsDown(UserCommands.CameraRotateDown))
            {
                y = -speed / 2;
                MoveCameraXYZ(0, y, 0);
            }
            if (UserInput.IsDown(UserCommands.CameraRotateLeft))
            {
                x = speed * -2;
                MoveCameraXYZ(x, 0, 0);
            }
            if (UserInput.IsDown(UserCommands.CameraRotateRight))
            {
                x = speed * 2;
                MoveCameraXYZ(x, 0, 0);
            }
            // Zoom
            ZoomByMouseWheel(speed);

            // Support for replaying camera rotation movements
            if (UserInput.IsPressed(UserCommands.CameraPanUp) || UserInput.IsPressed(UserCommands.CameraPanDown))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsReleased(UserCommands.CameraPanUp) || UserInput.IsReleased(UserCommands.CameraPanDown))
                new CameraRotateUpDownCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationXRadians);

            if (UserInput.IsPressed(UserCommands.CameraPanLeft) || UserInput.IsPressed(UserCommands.CameraPanRight))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsReleased(UserCommands.CameraPanLeft) || UserInput.IsReleased(UserCommands.CameraPanRight))
                new CameraRotateLeftRightCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, RotationYRadians);

            if (UserInput.IsPressed(UserCommands.CameraRotateUp) || UserInput.IsPressed(UserCommands.CameraRotateDown)
                || UserInput.IsPressed(UserCommands.CameraRotateLeft) || UserInput.IsPressed(UserCommands.CameraRotateRight))
                CommandStartTime = Viewer.Simulator.ClockTime;
            if (UserInput.IsPressed(UserCommands.CameraRotateUp) || UserInput.IsPressed(UserCommands.CameraRotateDown)
                || UserInput.IsPressed(UserCommands.CameraRotateLeft) || UserInput.IsPressed(UserCommands.CameraRotateRight))
                new CameraMoveXYZCommand(Viewer.Log, CommandStartTime, Viewer.Simulator.ClockTime, x, y, z);

        }
        public void MoveCameraXYZ(float x, float y, float z)
        {
            attachedLocation.X += x;
            attachedLocation.Y += y;
            attachedLocation.Z += z;
            viewPoint.Location.X = attachedLocation.X; viewPoint.Location.Y = attachedLocation.Y; viewPoint.Location.Z = attachedLocation.Z;
            UpdateLocation();
        }

        /// <summary>
        /// Remembers angle of camera to apply when user returns to this type of car.
        /// </summary>
        /// <param name="speed"></param>
        protected override void RotateRight(float speed)
        {
            base.RotateRight(speed);
            viewPoint.RotationYRadians = RotationYRadians;
        }

        /// <summary>
        /// Remembers angle of camera to apply when user returns to this type of car.
        /// </summary>
        /// <param name="speed"></param>
        protected override void RotateDown(float speed)
        {
            base.RotateDown(speed);
            viewPoint.RotationXRadians = RotationXRadians;
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
                viewPoint.RotationXRadians = RotationXRadians;
                viewPoint.RotationYRadians = RotationYRadians;
            }
        }
    }

    public class PassengerCamera : InsideThreeDimCamera
    {
        public override Styles Style { get { return Styles.Passenger; } }
        public override bool IsAvailable { get { return Viewer.SelectedTrain != null && Viewer.SelectedTrain.Cars.Any(c => c.PassengerViewpoints.Count > 0); } }
        public override string Name { get { return Viewer.Catalog.GetString("Passenger"); } }
        protected int ActViewPoint;

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
            viewPoint = attachedCar.PassengerViewpoints[ActViewPoint];
            attachedLocation = viewPoint.Location;
            // Apply previous angle of camera for this type of car.
            RotationXRadians = viewPoint.RotationXRadians;
            RotationYRadians = viewPoint.RotationYRadians;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            base.HandleUserInput(elapsedTime);
            if (UserInput.IsPressed(UserCommands.CameraChangePassengerViewPoint))
                new CameraChangePassengerViewPointCommand(Viewer.Log);
        }
        
            public void SwitchSideCameraCar(TrainCar car)
        {
            attachedLocation.X = -attachedLocation.X;
            RotationYRadians = -viewPoint.RotationYRadians;
        }

        public void ChangePassengerViewPoint(TrainCar car)
        {
            ActViewPoint++;
            if (ActViewPoint >= car.PassengerViewpoints.Count) ActViewPoint = 0;
            viewPoint = attachedCar.PassengerViewpoints[ActViewPoint];
            attachedLocation = viewPoint.Location;
            RotationXRadians = viewPoint.RotationXRadians;
            RotationYRadians = viewPoint.RotationYRadians;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(ActViewPoint);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            ActViewPoint = inf.ReadInt32();
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
        protected int sideLocation;
        public int SideLocation { get { return sideLocation; } }

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
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(sideLocation);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            sideLocation = inf.ReadInt32();
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
            if (Viewer.Settings.Cab2DStretch == 0 && Viewer.CabExceedsDisplayHorizontally <= 0)
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
        }

        protected override void OnActivate(bool sameCamera)
        {
            // Cab camera is only possible on the player locomotive.
            SetCameraCar(GetCameraCars().First());
            tiltingLand = false;
            if (Viewer.Simulator.UseSuperElevation > 0 || Viewer.Simulator.CarVibrating > 0) tiltingLand = true;
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
                var viewpoints = (loco.UsingRearCab)
                ? loco.CabViewList[(int)CabViewType.Rear].ViewPointList
                : loco.CabViewList[(int)CabViewType.Front].ViewPointList;
                attachedLocation = viewpoints[sideLocation].Location;
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

            var viewpointList = (loco.UsingRearCab)
            ? loco.CabViewList[(int)CabViewType.Rear].ViewPointList
            : loco.CabViewList[(int)CabViewType.Front].ViewPointList;

            sideLocation += index;

            var count = (loco.UsingRearCab)
                ? loco.CabViewList[(int)CabViewType.Rear].ViewPointList.Count
                : loco.CabViewList[(int)CabViewType.Front].ViewPointList.Count;
            // Wrap around
            if (sideLocation < 0)
                sideLocation = count - 1;
            else if (sideLocation >= count)
                sideLocation = 0;

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
            int min = Viewer.DisplaySize.Y - Viewer.CabHeightPixels; // -ve value
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
            int max = Viewer.CabWidthPixels - Viewer.DisplaySize.X; // -ve value
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
            var viewpoints = (loco.UsingRearCab)
            ? loco.CabViewList[(int)CabViewType.Rear].ViewPointList
            : loco.CabViewList[(int)CabViewType.Front].ViewPointList;

            RotationXRadians = MathHelper.ToRadians(viewpoints[sideLocation].StartDirection.X) - RotationRatio * (Viewer.CabYOffsetPixels + Viewer.CabExceedsDisplay / 2);
            RotationYRadians = MathHelper.ToRadians(viewpoints[sideLocation].StartDirection.Y) - RotationRatioHorizontal * (-Viewer.CabXOffsetPixels + Viewer.CabExceedsDisplayHorizontally / 2); ;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            var speedFactor = 500;  // Gives a fairly smart response.
            var speed = speedFactor * elapsedTime.RealSeconds; // Independent of framerate

            if (UserInput.IsPressed(UserCommands.CameraPanLeft))
                ShiftView(+1);
            if (UserInput.IsPressed(UserCommands.CameraPanRight))
                ShiftView(-1);
            if (UserInput.IsDown(UserCommands.CameraPanUp))
                PanUp(true, speed);
            if (UserInput.IsDown(UserCommands.CameraPanDown))
                PanUp(false, speed);
            if (UserInput.IsDown(UserCommands.CameraScrollRight))
                ScrollRight(true, speed);
            if (UserInput.IsDown(UserCommands.CameraScrollLeft))
                ScrollRight(false, speed);
        }
    }

    public class TracksideCamera : LookAtCamera
    {
        const int MaximumDistance = 100;
        const float SidewaysScale = MaximumDistance / 10;
        // Heights above the terrain for the camera.
        const float CameraAltitude = 2;
        // Height above the coordinate center of target.
        const float TargetAltitude = TerrainAltitudeMargin;
        const float MaxHalfPlatformLength = 600;
        const int MaximumSpecialPointDistance = 300;
        const float PlatformOffsetM = 3.3f;
        public bool SpecialPoints = false;
        private bool SpecialPointFound = false;
        const float CheckIntervalM = 50f; // every 50 meters it is checked wheter there is a near special point
        protected float DistanceRunM = 0f; // distance run since last check interval
        protected bool FirstUpdateLoop = true; // first update loop

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
//                if (TrackCameraLocation.Location.Y + TerrainAltitudeMargin < elevationAtCameraTarget)
//                    Trace.TraceInformation("CameraY {0}, elevationAtCamera {1}", TrackCameraLocation.Location.Y, elevationAtCameraTarget);
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

            if (UserInput.IsDown(UserCommands.CameraPanUp))
            {
                CameraAltitudeOffset += speed;
                cameraLocation.Location.Y += speed;
            }
            if (UserInput.IsDown(UserCommands.CameraPanDown))
            {
                CameraAltitudeOffset -= speed;
                cameraLocation.Location.Y -= speed;
                if (CameraAltitudeOffset < 0)
                {
                    cameraLocation.Location.Y -= CameraAltitudeOffset;
                    CameraAltitudeOffset = 0;
                }
            }
            if (UserInput.IsDown(UserCommands.CameraPanRight)) PanRight(speed);
            if (UserInput.IsDown(UserCommands.CameraPanLeft)) PanRight(-speed);
            if (UserInput.IsDown(UserCommands.CameraZoomIn)) ZoomIn(speed * 2);
            if (UserInput.IsDown(UserCommands.CameraZoomOut)) ZoomIn(-speed * 2);
            ZoomByMouseWheel(speed);

            var trainCars = Viewer.SelectedTrain.Cars;
            if (UserInput.IsPressed(UserCommands.CameraCarNext))
                attachedCar = attachedCar == trainCars.First() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) - 1];
            else if (UserInput.IsPressed(UserCommands.CameraCarPrevious))
                attachedCar = attachedCar == trainCars.Last() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) + 1];
            else if (UserInput.IsPressed(UserCommands.CameraCarFirst))
                attachedCar = trainCars.First();
            else if (UserInput.IsPressed(UserCommands.CameraCarLast))
                attachedCar = trainCars.Last();
        }

        public override void Update(ElapsedTime elapsedTime)
        {
            var train = attachedCar.Train;

            // TODO: What is this code trying to do?
            //if (train != Viewer.PlayerTrain && train.LeadLocomotive == null) train.ChangeToNextCab();
            var trainForwards = true;
            if (train.LeadLocomotive != null)
                //TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                // To achieve the same result with other means, without flipping trainset physics, maybe the line should be changed
                trainForwards = (train.LeadLocomotive.SpeedMpS >= 0) ^ train.LeadLocomotive.Flipped ^ ((MSTSLocomotive)train.LeadLocomotive).UsingRearCab;
            else if (Viewer.PlayerLocomotive != null && train.IsActualPlayerTrain)
                trainForwards = (Viewer.PlayerLocomotive.SpeedMpS >= 0) ^ Viewer.PlayerLocomotive.Flipped ^ ((MSTSLocomotive)Viewer.PlayerLocomotive).UsingRearCab;

            targetLocation = attachedCar.WorldPosition.WorldLocation;

            // Train is close enough if the last car we used is part of the same train and still close enough.
            var trainClose = (LastCheckCar != null) && (LastCheckCar.Train == train) && (WorldLocation.GetDistance2D(LastCheckCar.WorldPosition.WorldLocation, cameraLocation).Length() < (SpecialPointFound ? MaximumSpecialPointDistance * 0.8f : MaximumDistance));

            // Otherwise, let's check out every car and remember which is the first one close enough for next time.
            if (!trainClose)
            {
                foreach (var car in train.Cars)
                {
                    if (WorldLocation.GetDistance2D(car.WorldPosition.WorldLocation, cameraLocation).Length() < (SpecialPointFound ? MaximumSpecialPointDistance * 0.8f : MaximumDistance))
                    {
                        LastCheckCar = car;
                        trainClose = true;
                        break;
                    }
                }
            }
            var trySpecial = false;
            DistanceRunM += elapsedTime.ClockSeconds * train.SpeedMpS;
            if (Math.Abs(DistanceRunM) >= CheckIntervalM)
            {
                DistanceRunM = 0;
                if (!SpecialPointFound && SpecialPoints && trainClose) trySpecial = true;
            }
            // Switch to new position.
            if (!trainClose || (TrackCameraLocation == WorldLocation.None) || trySpecial)
            {
                SpecialPointFound = false;
                bool platformFound = false;
                Traveller tdb;
                if (FirstUpdateLoop && SpecialPoints) 
                    tdb = trainForwards ? new Traveller(train.RearTDBTraveller) : new Traveller(train.FrontTDBTraveller, Traveller.TravellerDirection.Backward);
                 else
                    tdb = trainForwards ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
                WorldLocation newLocation = WorldLocation.None;
                if (SpecialPoints)
                {
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
                            while (incrDistance < MaximumSpecialPointDistance * 0.7f)
                            {
                                foreach (int platformIndex in TCSection.PlatformIndex)
                                {
                                    PlatformDetails thisPlatform = train.signalRef.PlatformDetailsList[platformIndex];
                                    if (thisPlatform.TCOffset[0, thisRoute[routeIndex].Direction] + incrDistance < MaximumSpecialPointDistance * 0.7f
                                        && thisPlatform.TCOffset[0, thisRoute[routeIndex].Direction] + incrDistance > 0)
                                    {
                                        // platform found, compute distance to viewing point
                                        distanceToViewingPoint = Math.Min(MaximumSpecialPointDistance * 0.7f,
                                            incrDistance + thisPlatform.TCOffset[0, thisRoute[routeIndex].Direction] + thisPlatform.Length / 2.0f);
                                        platformFound = true;
                                        SpecialPointFound = true;
                                        tdb.Move(distanceToViewingPoint);
                                        newLocation = tdb.WorldLocation;
                                        Traveller shortTrav;
                                        PlatformItem platformItem = Viewer.Simulator.TDB.TrackDB.TrItemTable[thisPlatform.PlatformFrontUiD] as PlatformItem;
                                        if (platformItem == null) continue;
                                        shortTrav = new Traveller(Viewer.Simulator.TSectionDat, Viewer.Simulator.TDB.TrackDB.TrackNodes, platformItem.TileX,
                                            platformItem.TileZ, platformItem.X, platformItem.Z, Traveller.TravellerDirection.Forward);
                                        var distanceToViewingPoint1 = shortTrav.DistanceTo(newLocation.TileX, newLocation.TileZ,
                                            newLocation.Location.X, newLocation.Location.Y, newLocation.Location.Z, MaxHalfPlatformLength * 2);
                                        if (distanceToViewingPoint1 == -1) //try other direction
                                        {
                                            shortTrav.ReverseDirection();
                                            distanceToViewingPoint1 = shortTrav.DistanceTo(newLocation.TileX, newLocation.TileZ,
                                            newLocation.Location.X, newLocation.Location.Y, newLocation.Location.Z, MaxHalfPlatformLength * 2);
                                            if (distanceToViewingPoint1 == -1) continue;
                                        }
                                        shortTrav.Move(distanceToViewingPoint1);
                                        newLocation.Location.X += (PlatformOffsetM + Viewer.Simulator.SuperElevationGauge / 2) * (float)Math.Cos(shortTrav.RotY) *
                                            (thisPlatform.PlatformSide[1] ? 1 : -1);
                                        newLocation.Location.Z += -(PlatformOffsetM + Viewer.Simulator.SuperElevationGauge / 2) * (float)Math.Sin(shortTrav.RotY) *
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

                    if (!SpecialPointFound )
                    {
                        // try to find near level crossing then
                        LevelCrossingItem newLevelCrossingItem = LevelCrossingItem.None;
                        float FrontDist = - 1;
                        newLevelCrossingItem = Viewer.Simulator.LevelCrossings.SearchNearLevelCrossing(train, MaximumSpecialPointDistance * 0.75f, trainForwards, out FrontDist);
                        if (newLevelCrossingItem != LevelCrossingItem.None)
                        {
                            SpecialPointFound = true;
                            newLocation = newLevelCrossingItem.Location;
                            TrackCameraLocation = new WorldLocation(newLocation);
                            Traveller roadTraveller;
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
                            roadTraveller.Move(10.0f);
                            tdb.Move(FrontDist);
                            newLocation = roadTraveller.WorldLocation;
                        }
                    }
                }
                if (!SpecialPointFound && !trainClose)
                {
                    if (FirstUpdateLoop)
                        tdb = trainForwards ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward); // return to standard
                    tdb.Move(MaximumDistance * 0.75f);
                    newLocation = tdb.WorldLocation;
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
                }
                if (newLocation != WorldLocation.None)
                {
                    newLocation.Normalize();

                    var newLocationElevation = Viewer.Tiles.GetElevation(newLocation);
                    cameraLocation = newLocation;
                    cameraLocation.Location.Y = newLocationElevation;
                    TrackCameraLocation = new WorldLocation(cameraLocation);
                    cameraLocation.Location.Y = Math.Max(tdb.Y, newLocationElevation) + CameraAltitude + CameraAltitudeOffset + (platformFound ? 0.35f : 0.0f);
                }
            }

            targetLocation.Location.Y += TargetAltitude;
            FirstUpdateLoop = false;
            UpdateListener();
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
        public SpecialTracksideCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected override void OnActivate(bool sameCamera)
        {
            SpecialPoints = true;
            DistanceRunM = 0;
            base.OnActivate(sameCamera);
            FirstUpdateLoop = Math.Abs(attachedCar.Train.SpeedMpS) < 0.2 ? true : false;
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
                    Viewer.PlayerLocomotive != null && Viewer.PlayerLocomotive.CabViewpoints != null;
            }
        }
        public override string Name { get { return Viewer.Catalog.GetString("3D Cab"); } }
        bool StartDirectionSet = false;
        protected int CurrentViewpointIndex;
        protected bool PrevCabWasRear;

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
            if (attachedCar.CabViewpoints != null)
            {
                if (CurrentViewpointIndex >= attachedCar.CabViewpoints.Count) { viewPoint = attachedCar.CabViewpoints[0]; CurrentViewpointIndex = 0; }
                else viewPoint = attachedCar.CabViewpoints[CurrentViewpointIndex];
                attachedLocation = viewPoint.Location;
                if (!StartDirectionSet) // Only set the initial direction on first use so, when switching back from another camera, direction is not reset.
                {
                    StartDirectionSet = true;
                    RotationXRadians = MathHelper.ToRadians(viewPoint.StartDirection.X);
                    RotationYRadians = MathHelper.ToRadians(viewPoint.StartDirection.Y);
                }
                else
                {
                    RotationXRadians = viewPoint.RotationXRadians;
                    RotationYRadians = viewPoint.RotationYRadians;
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
                CurrentViewpointIndex = mstsLocomotive.UsingRearCab ? 1 : 0;
                PrevCabWasRear = mstsLocomotive.UsingRearCab;
                SetCameraCar(newCar);
            }
            catch { }
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
    }
}
