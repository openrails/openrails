/// CAMERAS
/// 
/// The camera classes are responsible for camera movement.
/// Each camera has a ViewMatrix, ProjectionMatrix and WorldLocation etc
/// that is adjusted as the camera moves.
/// 
/// Various camera types derive from the basic camera, ie CabCamera, PassengerCamera etc
/// 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MSTS;

namespace ORTS
{
	/// <summary>
	/// Base class for all cameras
	/// Represents a free roaming camera.
	/// </summary>
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

		public float RightFrustrumA { get { return (float)Math.Cos(MathHelper.ToRadians(45.0f) / 2 * ((float)Viewer.DisplaySize.X / Viewer.DisplaySize.Y)); } }

		// This sucks. It's really not camera-related at all.
		public static Matrix XNASkyProjection;

		// The following group of properties are used by other code to vary
		// behavior by camera; e.g. Style is used for activating sounds,
		// AttachedCar for rendering the train or not, and IsUnderground for
		// automatically switching to/from cab view in tunnels.
		public enum Styles { External, Cab, Passenger }
		public virtual Styles Style { get { return Styles.External; } }
		public virtual TrainCar AttachedCar { get { return null; } }
		public virtual bool IsUnderground { get { return false; } }

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
			var fovWidthRadians = MathHelper.ToRadians(45.0f);
			xnaProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, 0.5f, Viewer.Settings.ViewingDistance);
			XNASkyProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, 0.5f, farPlaneDistance);    // TODO remove? 
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

		protected void Normalize()
		{
			while (cameraLocation.Location.X > 1024) { cameraLocation.Location.X -= 2048; ++cameraLocation.TileX; };
			while (cameraLocation.Location.X < -1024) { cameraLocation.Location.X += 2048; --cameraLocation.TileX; };
			while (cameraLocation.Location.Z > 1024) { cameraLocation.Location.Z -= 2048; ++cameraLocation.TileZ; };
			while (cameraLocation.Location.Z < -1024) { cameraLocation.Location.Z += 2048; --cameraLocation.TileZ; };
		}

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

	public abstract class RotatingCamera : Camera
	{
        protected float rotationXRadians = 0;
        protected float rotationYRadians = 0;

		protected RotatingCamera(Viewer3D viewer)
			: base(viewer)
		{
		}

		protected RotatingCamera(Viewer3D viewer, Camera previousCamera)
			: base(viewer, previousCamera)
		{
            if (previousCamera != null)
            {
                float h, a, b;
                ORTSMath.MatrixToAngles(previousCamera.XNAView, out h, out a, out b);
                rotationXRadians = -b;
                rotationYRadians = -h;
            }
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
				rotationYRadians += UserInput.MouseMoveX() * elapsedTime.RealSeconds;
                rotationXRadians += UserInput.MouseMoveY() * elapsedTime.RealSeconds;
            }
			if (UserInput.IsDown(UserCommands.CameraRotateLeft))
				rotationYRadians -= speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraRotateRight))
				rotationYRadians += speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraRotateUp))
				rotationXRadians -= speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraRotateDown))
				rotationXRadians += speed * SpeedAdjustmentForRotation;

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
				movement.Z += speed;
			if (UserInput.IsDown(UserCommands.CameraPanOut))
				movement.Z -= speed;

			movement = Vector3.Transform(movement, Matrix.CreateRotationX(rotationXRadians));
			movement = Vector3.Transform(movement, Matrix.CreateRotationY(rotationYRadians));
			cameraLocation.Location += movement;

			Normalize();

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
		public FreeRoamCamera(Viewer3D viewer, Camera previousCamera)
			: base(viewer, previousCamera)
		{
		}
	}

	/// <summary>
	/// This represents a camera attached to a car.  
	/// It moves with the car.
	/// </summary>
	public abstract class AttachedCamera : RotatingCamera
	{
		protected TrainCar attachedCar;
		protected Vector3 onboardLocation;   // where on the car is the viewer

		public override TrainCar AttachedCar { get { return attachedCar; } }

		protected AttachedCamera(Viewer3D viewer)
			: base(viewer)
		{
		}

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            if (attachedCar != null && attachedCar.Train != null && attachedCar.Train == Viewer.PlayerTrain)
                outf.Write(Viewer.PlayerTrain.Cars.IndexOf(attachedCar));
            else
                outf.Write((int)-1);
            outf.Write(onboardLocation.X);
            outf.Write(onboardLocation.Y);
            outf.Write(onboardLocation.Z);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            var carIndex = inf.ReadInt32();
            if (carIndex!= -1 && Viewer.PlayerTrain != null)
                attachedCar = Viewer.PlayerTrain.Cars[carIndex];
            onboardLocation.X = inf.ReadSingle();
            onboardLocation.Y = inf.ReadSingle();
            onboardLocation.Z = inf.ReadSingle();
        }

		public override void HandleUserInput(ElapsedTime elapsedTime)
		{
			var train = attachedCar.Train;
			TrainCar newCar = null;
			if (UserInput.IsPressed(UserCommands.CameraCarNext))
				newCar = attachedCar == train.FirstCar ? attachedCar : train.Cars[train.Cars.IndexOf(attachedCar) - 1];
			else if (UserInput.IsPressed(UserCommands.CameraCarPrevious))
				newCar = attachedCar == train.LastCar ? attachedCar : train.Cars[train.Cars.IndexOf(attachedCar) + 1];
			else if (UserInput.IsPressed(UserCommands.CameraCarFirst))
				newCar = attachedCar.Train.FirstCar;
			else if (UserInput.IsPressed(UserCommands.CameraCarLast))
				newCar = attachedCar.Train.LastCar;

			if (newCar != null)
			{
                if (newCar.Flipped != attachedCar.Flipped)
                    FlipCamera();
				attachedCar = newCar;
			}
			else
			{
				base.HandleUserInput(elapsedTime);
			}
		}

		public override void Update(ElapsedTime elapsedTime)
		{
			if (attachedCar != null)
			{
				cameraLocation.TileX = attachedCar.WorldPosition.TileX;
				cameraLocation.TileZ = attachedCar.WorldPosition.TileZ;
				cameraLocation.Location = onboardLocation;
				cameraLocation.Location.Z *= -1;
				cameraLocation.Location = Vector3.Transform(cameraLocation.Location, attachedCar.WorldPosition.XNAMatrix);
				cameraLocation.Location.Z *= -1;
			}
			base.Update(elapsedTime);
		}

		protected override Matrix GetCameraView()
		{
			var lookAtPosition = Vector3.UnitZ;
			lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(rotationXRadians));
			lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(rotationYRadians));
			lookAtPosition += onboardLocation;
			lookAtPosition.Z *= -1;
			lookAtPosition = Vector3.Transform(lookAtPosition, attachedCar.WorldPosition.XNAMatrix);
			return Matrix.CreateLookAt(XNALocation(cameraLocation), lookAtPosition, Vector3.Up);
		}

		protected virtual void FlipCamera()
		{
			onboardLocation.X *= -1;
			onboardLocation.Z *= -1;
			rotationYRadians += (float)Math.PI;
		}
	}

	/// <summary>
	/// The brakeman is on the car at the front or back
	/// TODO, allow brakeman to jump on or off cars
	/// </summary>
	public class BrakemanCamera : AttachedCamera
	{
		public BrakemanCamera(Viewer3D viewer)
			: base(viewer)
		{
		}

		protected override void OnActivate(bool sameCamera)
		{
			if (attachedCar == null || attachedCar.Train != Viewer.PlayerTrain)
			{
				if (Viewer.PlayerTrain.MUDirection == Direction.Forward)
					PositionViewer(true);
				else
					PositionViewer(false);
			}
			base.OnActivate(sameCamera);
		}

		public override void HandleUserInput(ElapsedTime elapsedTime)
		{
			var speed = GetSpeed(elapsedTime);

			if (UserInput.IsDown(UserCommands.CameraPanLeft))
				rotationYRadians -= speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraPanRight))
				rotationYRadians += speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraPanUp))
				rotationXRadians -= speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraPanDown))
				rotationXRadians += speed * SpeedAdjustmentForRotation;

			if (UserInput.IsPressed(UserCommands.CameraCarFirst))
				PositionViewer(true);
			else if (UserInput.IsPressed(UserCommands.CameraCarLast))
				PositionViewer(false);
			else
				base.HandleUserInput(elapsedTime);

			rotationXRadians = MathHelper.Clamp(rotationXRadians, -(float)Math.PI / 2.1f, (float)Math.PI / 2.1f);
			if (attachedCar == Viewer.PlayerTrain.FirstCar)
				rotationYRadians = MathHelper.Clamp(rotationYRadians, -(float)Math.PI / 2, (float)Math.PI);
			else
				rotationYRadians = MathHelper.Clamp(rotationYRadians, 0, (float)Math.PI * 1.5f);
		}

		void PositionViewer(bool front)
		{
			attachedCar = front ? Viewer.PlayerTrain.FirstCar : Viewer.PlayerTrain.LastCar;
			rotationYRadians = front == attachedCar.Flipped ? (float)Math.PI : 0;
			onboardLocation = new Vector3(1.8f, 2.0f, attachedCar.Length / 2 - 0.3f);
			if (front == attachedCar.Flipped)
				onboardLocation.Z *= -1;
		}
	}

	/// <summary>
	/// The brakeman is on the car at the front or back
	/// TODO, allow brakeman to jump on or off cars
	/// </summary>
	public class HeadOutCamera : AttachedCamera
	{
		public enum HeadDirection { Forward, Backward }
		readonly bool Forwards;

        public bool IsAvailable { get { return Viewer.PlayerLocomotive != null && Viewer.PlayerLocomotive.HeadOutViewpoints.Count > 0; } }

		public HeadOutCamera(Viewer3D viewer, HeadDirection headDirection)
			: base(viewer)
		{
			Forwards = headDirection == HeadDirection.Forward;
		}

		protected override void OnActivate(bool sameCamera)
		{
			if (attachedCar == null || attachedCar.Train != Viewer.PlayerTrain)
			{
				if (Viewer.PlayerTrain.MUDirection == Direction.Forward)
					PositionViewer(true);
				else
					PositionViewer(false);
			}
			base.OnActivate(sameCamera);
		}

		public override void HandleUserInput(ElapsedTime elapsedTime)
		{
			var speed = GetSpeed(elapsedTime);

			if (UserInput.IsDown(UserCommands.CameraPanLeft))
				rotationYRadians -= speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraPanRight))
				rotationYRadians += speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanUp))
                rotationXRadians -= speed * SpeedAdjustmentForRotation;
            if (UserInput.IsDown(UserCommands.CameraPanDown))
                rotationXRadians += speed * SpeedAdjustmentForRotation;

			// Do this here so we can clamp the angles below.
			base.HandleUserInput(elapsedTime);

			rotationXRadians = MathHelper.Clamp(rotationXRadians, -(float)Math.PI / 2.1f, (float)Math.PI / 2.1f);
			if (Forwards)
				rotationYRadians = MathHelper.Clamp(rotationYRadians, 0, (float)Math.PI);
			else
				rotationYRadians = MathHelper.Clamp(rotationYRadians, -(float)Math.PI, 0);
		}

		void PositionViewer(bool front)
		{
			attachedCar = front ? Viewer.PlayerTrain.FirstCar : Viewer.PlayerTrain.LastCar;
			rotationYRadians = front == attachedCar.Flipped ? (float)Math.PI : 0;

            if (attachedCar != null &&
                attachedCar.HeadOutViewpoints.Count > 0)
            {
                onboardLocation = attachedCar.HeadOutViewpoints[0].Location;
            }

			if (front == attachedCar.Flipped)
				onboardLocation.Z *= -1;

			if (!Forwards)
			{
				onboardLocation.X *= -1;
				rotationYRadians = -(float)Math.PI;
			}
		}
	}

	public class CabCamera : AttachedCamera
	{
		int iLocation = 0; // handle left and right side views in cab

		public CabCamera(Viewer3D viewer)
			: base(viewer)
		{
		}

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(iLocation);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            iLocation = inf.ReadInt32();
        }

		public override Camera.Styles Style { get { return Styles.Cab; } }

		public bool HasCABViews
		{
			get
			{
				if (Viewer.PlayerLocomotive != null)
					attachedCar = Viewer.PlayerLocomotive;
				return attachedCar != null && attachedCar.FrontCabViewpoints != null && attachedCar.FrontCabViewpoints.Count != 0;
			}
		}

		/// <summary>
		/// Make this the viewer's current camera.
		/// If the locomotive has no cab view, then do nothing.
		/// </summary>
		protected override void OnActivate(bool sameCamera)
		{
			if (Viewer.PlayerLocomotive != null)
				attachedCar = Viewer.PlayerLocomotive;
			if (attachedCar.FrontCabViewpoints.Count == 0)
				return;
			ShiftView(0);

			base.OnActivate(sameCamera);
		}

		// Added to support the side CAB views - by GeorgeS
		/// <summary>
		/// Gets the current camera location in CAB
		/// </summary>
		public int SideLocation
		{
			get
			{
				return iLocation;
			}
		}

		void ShiftView(int index)
		{
			iLocation += index;

			if (iLocation < 0)
				iLocation = attachedCar.FrontCabViewpoints.Count - 1;
			else if (iLocation >= attachedCar.FrontCabViewpoints.Count)
				iLocation = 0;

			rotationYRadians = MSTSMath.M.Radians(attachedCar.FrontCabViewpoints[iLocation].StartDirection.Y);
			// TODO add X rotation and Z rotation
			// Added X rotation in order to support 2D CAB views - by GeorgeS
			rotationXRadians = MSTSMath.M.Radians(attachedCar.FrontCabViewpoints[iLocation].StartDirection.X);
			onboardLocation = attachedCar.FrontCabViewpoints[iLocation].Location;
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
	}

	public class TrackingCamera : AttachedCamera
	{
		const float StartPositionDistance = 9;
		const float StartPositionXRadians = 0.399f;
		const float StartPositionYRadians = 0.387f;

		float positionDistance = StartPositionDistance;
		float positionXRadians = StartPositionXRadians;
		float positionYRadians;

		public enum AttachedTo { Front, Rear }
		readonly bool Front;

		public override bool IsUnderground
		{
			get
			{
				var elevationAtTrain = Viewer.Tiles.GetElevation(AttachedCar.WorldPosition.WorldLocation);
				var elevationAtCamera = Viewer.Tiles.GetElevation(cameraLocation);
				return AttachedCar.WorldPosition.WorldLocation.Location.Y + TerrainAltitudeMargin < elevationAtTrain || cameraLocation.Location.Y + TerrainAltitudeMargin < elevationAtCamera;
			}
		}

		public TrackingCamera(Viewer3D viewer, AttachedTo attachedTo)
			: base(viewer)
		{
			Front = attachedTo == AttachedTo.Front;
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
            if (attachedCar == null)
            {
                attachedCar = Front ? Viewer.PlayerTrain.FirstCar : Viewer.PlayerTrain.LastCar;
                positionYRadians = StartPositionYRadians + (Front == attachedCar.Flipped ? (float)Math.PI : 0);
                rotationXRadians = positionXRadians;
                rotationYRadians = positionYRadians - (float)Math.PI;
            }
            else if (attachedCar.Train != Viewer.PlayerTrain)
            {
                attachedCar = Front ? Viewer.PlayerTrain.FirstCar : Viewer.PlayerTrain.LastCar;
            }
            UpdateOnboardLocation();
            base.OnActivate(sameCamera);
        }

		/// <summary>
		/// From distance and elevation variables
		/// </summary>
		void UpdateOnboardLocation()
		{
			onboardLocation.X = 0;
			onboardLocation.Y = 2;
			onboardLocation.Z = positionDistance;
			onboardLocation = Vector3.Transform(onboardLocation, Matrix.CreateRotationX(-positionXRadians));
			onboardLocation = Vector3.Transform(onboardLocation, Matrix.CreateRotationY(positionYRadians));
			onboardLocation.Z += attachedCar.Length / 2.0f * (Front == attachedCar.Flipped ? -1 : 1);
		}

		public override void HandleUserInput(ElapsedTime elapsedTime)
		{
			var speed = GetSpeed(elapsedTime);

			if (UserInput.IsDown(UserCommands.CameraPanLeft))
			{
				positionYRadians += speed * SpeedAdjustmentForRotation;
				rotationYRadians += speed * SpeedAdjustmentForRotation;
				UpdateOnboardLocation();
			}
			if (UserInput.IsDown(UserCommands.CameraPanRight))
			{
				positionYRadians -= speed * SpeedAdjustmentForRotation;
				rotationYRadians -= speed * SpeedAdjustmentForRotation;
				UpdateOnboardLocation();
			}
			if (UserInput.IsDown(UserCommands.CameraPanDown))
			{
				positionXRadians -= speed * SpeedAdjustmentForRotation;
				rotationXRadians -= speed * SpeedAdjustmentForRotation;
				if (positionXRadians < -1.5f) positionXRadians = -1.5f;
				UpdateOnboardLocation();
			}
			if (UserInput.IsDown(UserCommands.CameraPanUp))
			{
				positionXRadians += speed * SpeedAdjustmentForRotation;
				rotationXRadians += speed * SpeedAdjustmentForRotation;
				if (positionXRadians > 1.5f) positionXRadians = 1.5f;
				UpdateOnboardLocation();
			}
			if (UserInput.IsDown(UserCommands.CameraPanIn))
			{
				positionDistance -= speed * positionDistance / 10;
				if (positionDistance < 1) positionDistance = 1;
				UpdateOnboardLocation();
			}
			if (UserInput.IsDown(UserCommands.CameraPanOut))
			{
				positionDistance += speed * positionDistance / 10;
				if (positionDistance > 100) positionDistance = 100;
				UpdateOnboardLocation();
			}

			base.HandleUserInput(elapsedTime);

            if (UserInput.IsPressed(UserCommands.CameraCarFirst) || UserInput.IsPressed(UserCommands.CameraCarLast) || UserInput.IsPressed(UserCommands.CameraCarNext) || UserInput.IsPressed(UserCommands.CameraCarPrevious))
                UpdateOnboardLocation();
        }

        protected override void FlipCamera()
        {
            base.FlipCamera();
            positionYRadians += (float)Math.PI;
        }
	}

	public class PassengerCamera : AttachedCamera
	{
		public PassengerCamera(Viewer3D viewer)
			: base(viewer)
		{
		}

		public bool HasPassengerCamera
		{
			get
			{
                attachedCar = Viewer.PlayerTrain.Cars.FirstOrDefault(c => c.PassengerViewpoints.Count > 0);
				return attachedCar != null;
			}
		}

		public override Camera.Styles Style { get { return Styles.Passenger; } }

		public override void HandleUserInput(ElapsedTime elapsedTime)
		{
			var speed = GetSpeed(elapsedTime);

			if (UserInput.IsDown(UserCommands.CameraPanLeft))
				rotationYRadians -= speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraPanRight))
				rotationYRadians += speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraPanUp))
				rotationXRadians -= speed * SpeedAdjustmentForRotation;
			if (UserInput.IsDown(UserCommands.CameraPanDown))
				rotationXRadians += speed * SpeedAdjustmentForRotation;

            // Specially select all cars with passenger viewpoints and flip through only them.
            var trainCars = attachedCar.Train.Cars.Where(c => c.PassengerViewpoints.Count > 0).ToList();
            TrainCar newCar = null;
            if (UserInput.IsPressed(UserCommands.CameraCarNext))
                newCar = attachedCar == trainCars.First() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) - 1];
            else if (UserInput.IsPressed(UserCommands.CameraCarPrevious))
                newCar = attachedCar == trainCars.Last() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) + 1];
            else if (UserInput.IsPressed(UserCommands.CameraCarFirst))
                newCar = trainCars.First();
            else if (UserInput.IsPressed(UserCommands.CameraCarLast))
                newCar = trainCars.Last();

            if (newCar != null)
            {
                if (newCar.Flipped != attachedCar.Flipped)
                    FlipCamera();
                attachedCar = newCar;
            }
            else
            {
                base.HandleUserInput(elapsedTime);
            }

            var viewPoint = attachedCar.PassengerViewpoints[0];
            rotationXRadians = MathHelper.Clamp(rotationXRadians, MSTSMath.M.Radians(viewPoint.StartDirection.X - viewPoint.RotationLimit.X), MSTSMath.M.Radians(viewPoint.StartDirection.X + viewPoint.RotationLimit.X));
            rotationYRadians = MathHelper.Clamp(rotationYRadians, MSTSMath.M.Radians(viewPoint.StartDirection.Y - viewPoint.RotationLimit.Y), MSTSMath.M.Radians(viewPoint.StartDirection.Y + viewPoint.RotationLimit.Y));
		}

		protected override void OnActivate(bool sameCamera)
		{
            attachedCar = Viewer.PlayerTrain.Cars.FirstOrDefault(c => c.PassengerViewpoints.Count > 0);
			if (attachedCar == null)
				return;

            var viewPoint = attachedCar.PassengerViewpoints[0];
            onboardLocation = viewPoint.Location;
            rotationXRadians = MSTSMath.M.Radians(viewPoint.StartDirection.X);
            rotationYRadians = MSTSMath.M.Radians(viewPoint.StartDirection.Y);

			base.OnActivate(sameCamera);
		}
	} // Class PassengerCamera

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

		readonly Random Random;
		new TrainCar AttachedCar;
		WorldLocation TrackCameraLocation;
		float CameraAltitudeOffset = 0;

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
            if (AttachedCar == null || AttachedCar.Train != Viewer.PlayerTrain)
				AttachedCar = Viewer.PlayerTrain.FirstCar;
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
			if (UserInput.IsPressed(UserCommands.CameraCarNext))
			{
				var train = AttachedCar.Train;
				AttachedCar = AttachedCar == train.FirstCar ? AttachedCar : train.Cars[train.Cars.IndexOf(AttachedCar) - 1];
			}
			else if (UserInput.IsPressed(UserCommands.CameraCarPrevious))
			{
				var train = AttachedCar.Train;
				AttachedCar = AttachedCar == train.LastCar ? AttachedCar : train.Cars[train.Cars.IndexOf(AttachedCar) + 1];
			}
			else
			{
				base.HandleUserInput(elapsedTime);
			}
		}

		public override void Update(ElapsedTime elapsedTime)
		{
			// TODO: What should we do here?
			if (Viewer.PlayerLocomotive == null)
			{
				base.Update(elapsedTime);
				return;
			}

			var trainForwards = Viewer.PlayerLocomotive.SpeedMpS >= 0;
			var firstCarLocation = Viewer.PlayerTrain.FirstCar.WorldPosition.WorldLocation;
			var lastCarLocation = Viewer.PlayerTrain.LastCar.WorldPosition.WorldLocation;
			targetLocation = AttachedCar.WorldPosition.WorldLocation;

			// Switch to new position if BOTH ends of the train are too far away.
			if ((WorldLocation.GetDistance2D(firstCarLocation, cameraLocation).Length() > MaximumDistance) && (WorldLocation.GetDistance2D(lastCarLocation, cameraLocation).Length() > MaximumDistance))
			{
				var tdb = new TDBTraveller(trainForwards ? Viewer.PlayerTrain.FrontTDBTraveller : Viewer.PlayerTrain.RearTDBTraveller);
				if (!trainForwards)
					tdb.ReverseDirection();
				tdb.Move(MaximumDistance * 0.75f);
				var newLocation = tdb.WorldLocation;
				TrackCameraLocation = new WorldLocation(newLocation);
				var directionForward = WorldLocation.GetDistance(targetLocation, newLocation);
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
} // namespace ORTS
