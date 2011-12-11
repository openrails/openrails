// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ORTS
{
    public abstract class Camera
    {
        protected const int TerrainAltitudeMargin = 2;

        protected readonly Viewer3D Viewer;

        protected WorldLocation cameraLocation = new WorldLocation();
        public int TileX { get { return cameraLocation.TileX; } }
        public int TileZ { get { return cameraLocation.TileZ; } }
        public Vector3 Location { get { return cameraLocation.Location; } }
        public WorldLocation CameraWorldLocation { get { return cameraLocation; } }

        protected Matrix xnaView;
        public Matrix XNAView { get { return xnaView; } }

        Matrix xnaProjection;
        public Matrix XNAProjection { get { return xnaProjection; } }

        Vector3 frustumRightProjected;
        Vector3 frustumLeft;
        Vector3 frustumRight;

        public float RightFrustrumA { get { return (float)Math.Cos(MathHelper.ToRadians(Viewer.Settings.ViewingFOV) / 2 * ((float)Viewer.DisplaySize.X / Viewer.DisplaySize.Y)); } }

        // This sucks. It's really not camera-related at all.
        public static Matrix XNASkyProjection;

        // The following group of properties are used by other code to vary
        // behavior by camera; e.g. Style is used for activating sounds,
        // AttachedCar for rendering the train or not, and IsUnderground for
        // automatically switching to/from cab view in tunnels.
        public enum Styles { External, Cab, Passenger }
        public virtual Styles Style { get { return Styles.External; } }
        public virtual TrainCar AttachedCar { get { return null; } }
        public virtual bool IsAvailable { get { return true; } }
        public virtual bool IsUnderground { get { return false; } }

        // We need to allow different cameras to have different near planes.
        public virtual float NearPlane { get { return 1.0f; } }

        protected Camera(Viewer3D viewer)
        {
            Viewer = viewer;
        }

        protected Camera(Viewer3D viewer, Camera previousCamera) // maintain visual continuity
            : this(viewer)
        {
            if (previousCamera != null)
                cameraLocation = previousCamera.CameraWorldLocation;
        }

        protected internal virtual void Save(BinaryWriter outf)
        {
            cameraLocation.Save(outf);
        }

        protected internal virtual void Restore(BinaryReader inf)
        {
            cameraLocation.Restore(inf);
        }

        /// <summary>
        /// Switches the <see cref="Viewer3D"/> to this camera, updating the view information.
        /// </summary>
        public void Activate()
        {
            ScreenChanged();
            OnActivate(Viewer.Camera == this);
            Viewer.Camera = this;
            Update(ElapsedTime.Zero);
            xnaView = GetCameraView();
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
            var fovWidthRadians = MathHelper.ToRadians(Viewer.Settings.ViewingFOV);
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
            frame.SetCamera(ref xnaView, ref xnaProjection);
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
        public bool InFOV(Vector3 mstsObjectCenter, float objectRadius)
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

            if (!InFOV(mstsObjectCenter, objectRadius))
                return false;

            return true;
        }

        public bool CanSee(Matrix xnaMatrix, float objectRadius, float objectViewingDistance)
        {
            var mstsLocation = new Vector3(xnaMatrix.Translation.X, xnaMatrix.Translation.Y, -xnaMatrix.Translation.Z);
            return CanSee(mstsLocation, objectRadius, objectViewingDistance);
        }

        protected float GetSpeed(ElapsedTime elapsedTime)
        {
            var speed = 5 * elapsedTime.RealSeconds;
            if (UserInput.IsDown(UserCommands.CameraMoveFast))
                speed *= 10;
            if (UserInput.IsDown(UserCommands.CameraMoveSlow))
                speed /= 10;
            return speed;
        }

        protected const float SpeedAdjustmentForRotation = 0.1f;

        /// <summary>
        /// Returns a position in XNA space relative to the camera's tile
        /// </summary>
        /// <param name="worldLocation"></param>
        /// <returns></returns>
        public Vector3 XNALocation(WorldLocation worldLocation)
        {
            var xnaVector = worldLocation.Location;
            xnaVector.X += 2048 * (worldLocation.TileX - cameraLocation.TileX);
            xnaVector.Z += 2048 * (worldLocation.TileZ - cameraLocation.TileZ);
            xnaVector.Z *= -1;
            return xnaVector;
        }
    }

    public abstract class LookAtCamera : Camera
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

        protected LookAtCamera(Viewer3D viewer)
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
            return Matrix.CreateLookAt(XNALocation(cameraLocation), XNALocation(targetLocation), Vector3.UnitY);
        }
    }

    public class CameraAngleClamper
    {
        float minAngle;
        float maxAngle;
        public CameraAngleClamper(float min, float max)
        {
            minAngle = min;
            maxAngle = max;
        }

        public float Clamp(float angle)
        {
            return MathHelper.Clamp(angle, minAngle, maxAngle);
        }
    }

    public abstract class RotatingCamera : Camera
    {
        protected float rotationXRadians = 0;
        protected float rotationYRadians = 0;

        private CameraAngleClamper rotationXClamper = null;
        private CameraAngleClamper rotationYClamper = null;

        protected float axisZSpeedBoost = 1.0f;

        protected RotatingCamera(Viewer3D viewer, CameraAngleClamper xClamper, CameraAngleClamper yClamper)
            : base(viewer)
        {
            rotationXClamper = xClamper;
            rotationYClamper = yClamper;
        }

        protected RotatingCamera(Viewer3D viewer, Camera previousCamera, CameraAngleClamper xClamper, CameraAngleClamper yClamper)
            : base(viewer, previousCamera)
        {
            if (previousCamera != null)
            {
                float h, a, b;
                ORTSMath.MatrixToAngles(previousCamera.XNAView, out h, out a, out b);
                rotationXRadians = -b;
                rotationYRadians = -h;
            }

            rotationXClamper = xClamper;
            rotationYClamper = yClamper;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(rotationXRadians);
            outf.Write(rotationYRadians);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            rotationXRadians = inf.ReadSingle();
            rotationYRadians = inf.ReadSingle();
        }        

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            var speed = GetSpeed(elapsedTime);

            // Rotation
            if (UserInput.IsMouseRightButtonDown())
            {
                // Mouse movement doesn't use 'var speed' because the MouseMove 
                // parameters are already scaled down with increasing frame rates, 
                // Mouse rotation speed is independant of shift key activation
                rotationXRadians += 0.01F * UserInput.MouseMoveY();
                rotationYRadians += 0.01F * UserInput.MouseMoveX();
            }
            if (UserInput.IsDown(UserCommands.CameraRotateUp))
                rotationXRadians -= speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraRotateDown))
                rotationXRadians += speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraRotateLeft))
                rotationYRadians -= speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraRotateRight))
                rotationYRadians += speed * SpeedAdjustmentForRotation;

            if(rotationXClamper != null)
                rotationXRadians = rotationXClamper.Clamp(rotationXRadians);

            if(rotationYClamper != null)
                rotationYRadians = rotationYClamper.Clamp(rotationYRadians);

            // Movement
            Vector3 movement = new Vector3(0, 0, 0);
            if (UserInput.IsDown(UserCommands.CameraPanLeft))
                movement.X -= speed;
            if (UserInput.IsDown(UserCommands.CameraPanRight))
                movement.X += speed;
            if (UserInput.IsDown(UserCommands.CameraPanUp))
                movement.Y += speed;
            if (UserInput.IsDown(UserCommands.CameraPanDown))
                movement.Y -= speed;
            if (UserInput.IsDown(UserCommands.CameraPanIn))
                movement.Z += speed * axisZSpeedBoost;
            if (UserInput.IsDown(UserCommands.CameraPanOut))
                movement.Z -= speed * axisZSpeedBoost;

            movement = Vector3.Transform(movement, Matrix.CreateRotationX(rotationXRadians));
            movement = Vector3.Transform(movement, Matrix.CreateRotationY(rotationYRadians));
            cameraLocation.Location += movement;
            cameraLocation.Normalize();

            base.HandleUserInput(elapsedTime);
        }

        protected override Matrix GetCameraView()
        {
            var lookAtPosition = Vector3.UnitZ;
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(rotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(rotationYRadians));
            lookAtPosition += cameraLocation.Location;
            lookAtPosition.Z *= -1;
            return Matrix.CreateLookAt(XNALocation(cameraLocation), lookAtPosition, Vector3.Up);
        }
    }

    public class FreeRoamCamera : RotatingCamera
    {
        const float maxCameraHeight = 1000;

        public FreeRoamCamera(Viewer3D viewer, Camera previousCamera)
            : base(viewer, previousCamera, new CameraAngleClamper(-MathHelper.Pi / 2.1f, MathHelper.Pi / 2.1f), null)
        {
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            base.HandleUserInput(elapsedTime);

            if (UserInput.IsDown(UserCommands.CameraPanIn) || UserInput.IsDown(UserCommands.CameraPanOut))
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
        }
    }

    public abstract class AttachedCamera : RotatingCamera
    {
        protected TrainCar attachedCar;
        public override TrainCar AttachedCar { get { return attachedCar; } }

        protected Vector3 attachedLocation;

        protected AttachedCamera(Viewer3D viewer, CameraAngleClamper xClamper, CameraAngleClamper yClamper)
            : base(viewer, xClamper, yClamper)
        {
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            if (attachedCar != null && attachedCar.Train != null && attachedCar.Train == Viewer.PlayerTrain)
                outf.Write(Viewer.PlayerTrain.Cars.IndexOf(attachedCar));
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
            if (carIndex != -1 && Viewer.PlayerTrain != null)
                attachedCar = Viewer.PlayerTrain.Cars[carIndex];
            attachedLocation.X = inf.ReadSingle();
            attachedLocation.Y = inf.ReadSingle();
            attachedLocation.Z = inf.ReadSingle();
        }

        protected override void OnActivate(bool sameCamera)
        {
            if (attachedCar == null || attachedCar.Train != Viewer.PlayerTrain)
            {
                if (Viewer.PlayerTrain.MUDirection != Direction.Reverse)
                    SetCameraCar(GetCameraCars().First());
                else
                    SetCameraCar(GetCameraCars().Last());
            }
            base.OnActivate(sameCamera);
        }

        protected virtual List<TrainCar> GetCameraCars()
        {
            return Viewer.PlayerTrain.Cars;
        }

        protected virtual void SetCameraCar(TrainCar car)
        {
            attachedCar = car;
        }

        protected virtual bool IsCameraFlipped()
        {
            return false;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            var trainCars = GetCameraCars();
            if (UserInput.IsPressed(UserCommands.CameraCarNext))
                SetCameraCar(attachedCar == trainCars.First() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) - 1]);
            else if (UserInput.IsPressed(UserCommands.CameraCarPrevious))
                SetCameraCar(attachedCar == trainCars.Last() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) + 1]);
            else if (UserInput.IsPressed(UserCommands.CameraCarFirst))
                SetCameraCar(trainCars.First());
            else if (UserInput.IsPressed(UserCommands.CameraCarLast))
                SetCameraCar(trainCars.Last());
            else
                base.HandleUserInput(elapsedTime);
        }
        
        private void FixCameraLocation()
        {
            var elevationAtCamera = Viewer.Tiles.GetElevation(cameraLocation);

            System.Console.WriteLine(elevationAtCamera.ToString() + " : " + cameraLocation.Location.Y.ToString());
            if (elevationAtCamera > cameraLocation.Location.Y)
            {
                cameraLocation.Location.Y = elevationAtCamera;
            }
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

                //FixCameraLocation();
            }
            base.Update(elapsedTime);
        }

        protected override Matrix GetCameraView()
        {
            var flipped = IsCameraFlipped();
            var lookAtPosition = Vector3.UnitZ;
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(rotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(rotationYRadians + (flipped ? MathHelper.Pi : 0)));
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
            return Matrix.CreateLookAt(XNALocation(cameraLocation), lookAtPosition, Vector3.Up);
        }
    }

    public class BrakemanCamera : AttachedCamera
    {
        protected bool attachedToRear;

        public override float NearPlane { get { return 0.25f; } }

        public BrakemanCamera(Viewer3D viewer)
            : base(viewer, new CameraAngleClamper(-MathHelper.Pi / 2.1f, MathHelper.Pi / 2.1f), new CameraAngleClamper(-MathHelper.Pi / 2, MathHelper.Pi))
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
            attachedLocation = new Vector3(1.8f, 2.0f, attachedCar.Length / 2 - 0.3f);
            attachedToRear = car.Train.Cars[0] != car;
        }

        protected override bool IsCameraFlipped()
        {
            return attachedToRear ^ attachedCar.Flipped;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            var speed = GetSpeed(elapsedTime);

            if (UserInput.IsDown(UserCommands.CameraPanUp))
                rotationXRadians -= speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanDown))
                rotationXRadians += speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanLeft))
                rotationYRadians -= speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanRight))
                rotationYRadians += speed * SpeedAdjustmentForRotation;

            base.HandleUserInput(elapsedTime);            
        }
    }

    public class HeadOutCamera : AttachedCamera
    {
        protected readonly bool Forwards;
        public enum HeadDirection { Forward, Backward }

        public override bool IsAvailable { get { return Viewer.PlayerTrain != null && Viewer.PlayerTrain.Cars.Any(c => c.HeadOutViewpoints.Count > 0); } }
        public override float NearPlane { get { return 0.25f; } }

        public HeadOutCamera(Viewer3D viewer, HeadDirection headDirection)
            : base(
                viewer, 
                new CameraAngleClamper(-MathHelper.Pi / 2.1f, MathHelper.Pi / 2.1f), 
                (headDirection == HeadDirection.Forward ? 
                    new CameraAngleClamper(0, MathHelper.Pi):
                    new CameraAngleClamper(-MathHelper.Pi, 0))
            )
        {
            Forwards = headDirection == HeadDirection.Forward;
            rotationYRadians = Forwards ? 0 : -MathHelper.Pi;
        }

        protected override List<TrainCar> GetCameraCars()
        {
            return base.GetCameraCars().Where(c => c.HeadOutViewpoints.Count > 0).ToList();
        }

        protected override void SetCameraCar(TrainCar car)
        {
            base.SetCameraCar(car);
            attachedLocation = attachedCar.HeadOutViewpoints[0].Location;
            if (!Forwards)
                attachedLocation.X *= -1;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            var speed = GetSpeed(elapsedTime);

            if (UserInput.IsDown(UserCommands.CameraPanUp))
                rotationXRadians -= speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanDown))
                rotationXRadians += speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanLeft))
                rotationYRadians -= speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanRight))
                rotationYRadians += speed * SpeedAdjustmentForRotation;

            // Do this here so we can clamp the angles below.
            base.HandleUserInput(elapsedTime);            
        }
    }

    public class CabCamera : AttachedCamera
    {
        protected int sideLocation = 0;
        public int SideLocation { get { return sideLocation; } }

        public override Styles Style { get { return Styles.Cab; } }
        public override bool IsAvailable { get { return Viewer.PlayerLocomotive != null && Viewer.PlayerLocomotive.FrontCabViewpoints.Count > 0; } }

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
                return attachedCar.WorldPosition.Location.Y + TerrainAltitudeMargin < elevationAtCameraTarget;
            }
        }

        public CabCamera(Viewer3D viewer)
            : base(viewer, null, null)
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

        protected override void OnActivate(bool sameCamera)
        {
            // Need to check PlayerLocomotive (not PlayerTrain) here so we're always looking at the right cab.
            if (attachedCar == null || attachedCar != Viewer.PlayerLocomotive)
            {
                SetCameraCar(GetCameraCars().First());
            }
            base.OnActivate(sameCamera);
        }

        protected override List<TrainCar> GetCameraCars()
        {
            return new List<TrainCar>(new[] { Viewer.PlayerLocomotive });
        }

        protected override void SetCameraCar(TrainCar car)
        {
            base.SetCameraCar(car);
            rotationXRadians = MSTSMath.M.Radians(attachedCar.FrontCabViewpoints[sideLocation].StartDirection.X);
            rotationYRadians = MSTSMath.M.Radians(attachedCar.FrontCabViewpoints[sideLocation].StartDirection.Y);
            attachedLocation = attachedCar.FrontCabViewpoints[sideLocation].Location;
        }

        void ShiftView(int index)
        {
            sideLocation += index;

            if (sideLocation < 0)
                sideLocation = attachedCar.FrontCabViewpoints.Count - 1;
            else if (sideLocation >= attachedCar.FrontCabViewpoints.Count)
                sideLocation = 0;

            SetCameraCar(attachedCar);
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            // Switched shift number to select the right cab view - by GeorgeS
            if (UserInput.IsPressed(UserCommands.CameraPanLeft))
                ShiftView(+1);
            if (UserInput.IsPressed(UserCommands.CameraPanRight))
                ShiftView(-1);

            // Don't call this or we'll let the user rotate the camera!
            //base.HandleUserInput(elapsedTime);
        }
    }

    public class TrackingCamera : AttachedCamera
    {
        const float StartPositionDistance = 20;
        const float StartPositionXRadians = 0.399f;
        const float StartPositionYRadians = 0.387f;

        protected readonly bool Front;
        public enum AttachedTo { Front, Rear }

        protected float positionDistance = StartPositionDistance;
        protected float positionXRadians = StartPositionXRadians;
        protected float positionYRadians;

        public override bool IsUnderground
        {
            get
            {                
                var elevationAtTrain = Viewer.Tiles.GetElevation(attachedCar.WorldPosition.WorldLocation);
                var elevationAtCamera = Viewer.Tiles.GetElevation(cameraLocation);
                return attachedCar.WorldPosition.WorldLocation.Location.Y + TerrainAltitudeMargin < elevationAtTrain || cameraLocation.Location.Y + TerrainAltitudeMargin < elevationAtCamera;                
            }
        }

        public TrackingCamera(Viewer3D viewer, AttachedTo attachedTo)
            : base(viewer, new CameraAngleClamper(-MathHelper.Pi / 2.1f, MathHelper.Pi / 2.1f), null)
        {
            Front = attachedTo == AttachedTo.Front;
            positionYRadians = StartPositionYRadians + (Front ? 0 : MathHelper.Pi);
            rotationXRadians = positionXRadians;
            rotationYRadians = positionYRadians - MathHelper.Pi;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(positionDistance);
            outf.Write(positionXRadians);
            outf.Write(positionYRadians);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            positionDistance = inf.ReadSingle();
            positionXRadians = inf.ReadSingle();
            positionYRadians = inf.ReadSingle();
        }

        protected override void OnActivate(bool sameCamera)
        {
            if (attachedCar == null || attachedCar.Train != Viewer.PlayerTrain)
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
            var speed = GetSpeed(elapsedTime);

            if (UserInput.IsDown(UserCommands.CameraPanUp))
            {
                positionXRadians += speed * SpeedAdjustmentForRotation;
                rotationXRadians += speed * SpeedAdjustmentForRotation;
                if (positionXRadians > 1.5f) positionXRadians = 1.5f;
            }
            if (UserInput.IsDown(UserCommands.CameraPanDown))
            {
                positionXRadians -= speed * SpeedAdjustmentForRotation;
                rotationXRadians -= speed * SpeedAdjustmentForRotation;
                if (positionXRadians < -1.5f) positionXRadians = -1.5f;
            }
            if (UserInput.IsDown(UserCommands.CameraPanLeft))
            {
                positionYRadians += speed * SpeedAdjustmentForRotation;
                rotationYRadians += speed * SpeedAdjustmentForRotation;
            }
            if (UserInput.IsDown(UserCommands.CameraPanRight))
            {
                positionYRadians -= speed * SpeedAdjustmentForRotation;
                rotationYRadians -= speed * SpeedAdjustmentForRotation;
            }
            if (UserInput.IsDown(UserCommands.CameraPanIn))
            {
                positionDistance -= speed * positionDistance / 10;
                if (positionDistance < 1) positionDistance = 1;
            }
            if (UserInput.IsDown(UserCommands.CameraPanOut))
            {
                positionDistance += speed * positionDistance / 10;
                if (positionDistance > 100) positionDistance = 100;
            }

            base.HandleUserInput(elapsedTime);            

            attachedLocation.X = 0;
            attachedLocation.Y = 2;
            attachedLocation.Z = positionDistance;
            attachedLocation = Vector3.Transform(attachedLocation, Matrix.CreateRotationX(-positionXRadians));
            attachedLocation = Vector3.Transform(attachedLocation, Matrix.CreateRotationY(positionYRadians));
            attachedLocation.Z += attachedCar.Length / 2.0f * (Front ? 1 : -1);
        }
    }

    public class PassengerCamera : AttachedCamera
    {
        public override Styles Style { get { return Styles.Passenger; } }
        public override bool IsAvailable { get { return Viewer.PlayerTrain != null && Viewer.PlayerTrain.Cars.Any(c => c.PassengerViewpoints.Count > 0); } }
        public override float NearPlane { get { return 0.1f; } }

        public PassengerCamera(Viewer3D viewer)
            : base(viewer, null, null)
        {
        }

        protected override List<TrainCar> GetCameraCars()
        {
            return base.GetCameraCars().Where(c => c.PassengerViewpoints.Count > 0).ToList();
        }

        protected override void SetCameraCar(TrainCar car)
        {
            base.SetCameraCar(car);
            var viewPoint = attachedCar.PassengerViewpoints[0];
            attachedLocation = viewPoint.Location;
            rotationXRadians = MSTSMath.M.Radians(viewPoint.StartDirection.X);
            rotationYRadians = MSTSMath.M.Radians(viewPoint.StartDirection.Y);
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            var speed = GetSpeed(elapsedTime);

            if (UserInput.IsDown(UserCommands.CameraPanUp))
                rotationXRadians -= speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanDown))
                rotationXRadians += speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanLeft))
                rotationYRadians -= speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanRight))
                rotationYRadians += speed * SpeedAdjustmentForRotation;

            base.HandleUserInput(elapsedTime);

            var viewPoint = attachedCar.PassengerViewpoints[0];
            rotationXRadians = MathHelper.Clamp(rotationXRadians, MSTSMath.M.Radians(viewPoint.StartDirection.X - viewPoint.RotationLimit.X), MSTSMath.M.Radians(viewPoint.StartDirection.X + viewPoint.RotationLimit.X));
            rotationYRadians = MathHelper.Clamp(rotationYRadians, MSTSMath.M.Radians(viewPoint.StartDirection.Y - viewPoint.RotationLimit.Y), MSTSMath.M.Radians(viewPoint.StartDirection.Y + viewPoint.RotationLimit.Y));
        }
    }

    public class TracksideCamera : LookAtCamera
    {
        const int MaximumDistance = 100;
        const float SidewaysScale = MaximumDistance / 10;
        // Heights above the terrain for the camera.
        const float CameraNormalAltitude = 2;
        const float CameraBridgeAltitude = 8;
        // Height above the coordinate center of target.
        const float TargetAltitude = TerrainAltitudeMargin;
        // Max altitude of terrain below coordinate center of train car before bridge-mode.
        const float BridgeCutoffAltitude = 1;

        protected TrainCar attachedCar;
        public override TrainCar AttachedCar { get { return attachedCar; } }

        protected TrainCar LastCheckCar;
        protected readonly Random Random;
        protected WorldLocation TrackCameraLocation;
        protected float CameraAltitudeOffset = 0;

        public override bool IsUnderground
        {
            get
            {
                // Camera is underground if target (base) is underground or
                // track location is underground. The latter means we switch
                // to cab view instead of putting the camera above the tunnel.
                if (base.IsUnderground)
                    return true;
                var elevationAtCameraTarget = Viewer.Tiles.GetElevation(TrackCameraLocation);
                return TrackCameraLocation.Location.Y + TerrainAltitudeMargin < elevationAtCameraTarget;
            }
        }

        public TracksideCamera(Viewer3D viewer)
            : base(viewer)
        {
            Random = new Random();
        }

        protected override void OnActivate(bool sameCamera)
        {
            if (sameCamera)
            {
                cameraLocation.TileX = 0;
                cameraLocation.TileZ = 0;
            }
            if (attachedCar == null || attachedCar.Train != Viewer.PlayerTrain)
            {
                if (Viewer.PlayerTrain.MUDirection != Direction.Reverse)
                    attachedCar = Viewer.PlayerTrain.Cars.First();
                else
                    attachedCar = Viewer.PlayerTrain.Cars.Last();
            }
            base.OnActivate(sameCamera);
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
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

            var trainCars = Viewer.PlayerTrain.Cars;
            if (UserInput.IsPressed(UserCommands.CameraCarNext))
                attachedCar = attachedCar == trainCars.First() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) - 1];
            else if (UserInput.IsPressed(UserCommands.CameraCarPrevious))
                attachedCar = attachedCar == trainCars.Last() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) + 1];
            else if (UserInput.IsPressed(UserCommands.CameraCarFirst))
                attachedCar = trainCars.First();
            else if (UserInput.IsPressed(UserCommands.CameraCarLast))
                attachedCar = trainCars.Last();
            else
                base.HandleUserInput(elapsedTime);
        }

        public override void Update(ElapsedTime elapsedTime)
        {
            var train = attachedCar.Train;

            if (train.LeadLocomotive == null)
            {
                base.Update(elapsedTime);
                return;
            }

            var trainForwards = (train.LeadLocomotive.SpeedMpS >= 0) ^ train.LeadLocomotive.Flipped;
            targetLocation = attachedCar.WorldPosition.WorldLocation;

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
            if (!trainClose || (TrackCameraLocation == null))
            {
                var tdb = new TDBTraveller(trainForwards ? train.FrontTDBTraveller : train.RearTDBTraveller);
                if (!trainForwards)
                    tdb.ReverseDirection();
                tdb.Move(MaximumDistance * 0.75f);
                var newLocation = tdb.WorldLocation;
                TrackCameraLocation = new WorldLocation(newLocation);
                var directionForward = WorldLocation.GetDistance((trainForwards ? train.FirstCar : train.LastCar).WorldPosition.WorldLocation, newLocation);
                if (Random.Next(2) == 0)
                {
                    newLocation.Location.X += -directionForward.Z / SidewaysScale; // Use swaped -X and Z to move to the left of the track.
                    newLocation.Location.Z += directionForward.X / SidewaysScale;
                }
                else
                {
                    newLocation.Location.X += directionForward.Z / SidewaysScale; // Use swaped X and -Z to move to the right of the track.
                    newLocation.Location.Z += -directionForward.X / SidewaysScale;
                }
                newLocation.Normalize();

                var newLocationElevation = Viewer.Tiles.GetElevation(newLocation);
                if (newLocationElevation > newLocation.Location.Y - BridgeCutoffAltitude)
                {
                    cameraLocation = newLocation;
                    cameraLocation.Location.Y = newLocationElevation + CameraNormalAltitude + CameraAltitudeOffset;
                }
                else
                {
                    cameraLocation = new WorldLocation(tdb.TileX, tdb.TileZ, tdb.X, tdb.Y + CameraBridgeAltitude + CameraAltitudeOffset, tdb.Z);
                }
            }

            targetLocation.Location.Y += TargetAltitude;

            base.Update(elapsedTime);
        }
    }
}
