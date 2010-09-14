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

		// The right frustrum is precomputed, represented by this equation:
		//   0 = Ax + Bz; where A^2 + B^2 = 1
		float rightFrustrumA, rightFrustrumB;
		public float RightFrustrumA { get { return rightFrustrumA; } }
		public float RightFrustrumB { get { return rightFrustrumB; } }

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
			ScreenChanged();
		}

		protected Camera(Viewer3D viewer, Camera previousCamera) // maintain visual continuity
			: this(viewer)
		{
			cameraLocation = previousCamera.CameraWorldLocation;
		}

		/// <summary>
		/// Switches the <see cref="Viewer3D"/> to this camera, updating the view information.
		/// </summary>
		public void Activate()
		{
			ScreenChanged();
			OnActivate();
			Viewer.Camera = this;
			Update(ElapsedTime.Zero);
			xnaView = GetCameraView();
		}

		/// <summary>
		/// A camera can use this method to handle any preparation when being activated.
		/// </summary>
		protected virtual void OnActivate()
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
		/// <param name="elapsedTime"></param>
		protected abstract Matrix GetCameraView();

		/// <summary>
		/// Notifies the camera that the screen dimensions have changed.
		/// </summary>
		public void ScreenChanged()
		{
			var aspectRatio = Viewer.DisplaySize.X / Viewer.DisplaySize.Y;
			var farPlaneDistance = SkyConstants.skyRadius + 100;  // so far the sky is the biggest object in view
			var fovWidthRadians = MathHelper.ToRadians(45.0f);
			xnaProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, 0.5f, Viewer.SettingsInt[(int)IntSettings.ViewingDistance]);
			XNASkyProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, 0.5f, farPlaneDistance);    // TODO remove? 
			rightFrustrumA = (float)Math.Cos(fovWidthRadians / 2 * aspectRatio);  // precompute the right edge of the view frustrum
			rightFrustrumB = (float)Math.Sin(fovWidthRadians / 2 * aspectRatio);
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
		}

		// Cull for fov
		public bool InFOV(Vector3 mstsObjectCenter, float objectRadius)
		{
			Vector3 xnaObjectCenter = new Vector3(mstsObjectCenter.X, mstsObjectCenter.Y, -mstsObjectCenter.Z);
			Vector3.Transform(ref xnaObjectCenter, ref xnaView, out xnaObjectCenter);

			if (xnaObjectCenter.Z > objectRadius * 2)
				return false;  // behind camera

			// Cull for left and right
			var d = MSTSMath.M.DistanceToLine(RightFrustrumA, RightFrustrumB, 0, xnaObjectCenter.X, xnaObjectCenter.Z);
			if (d > objectRadius * 2)
				return false;  // right of view

			d = MSTSMath.M.DistanceToLine(-RightFrustrumA, RightFrustrumB, 0, xnaObjectCenter.X, xnaObjectCenter.Z);
			if (d > objectRadius * 2)
				return false; // left of view

			return true;
		}

		// cull for distance
		public bool InRange(Vector3 mstsObjectCenter, float viewingRange)
		{
			var dx = mstsObjectCenter.X - cameraLocation.Location.X;
			var dz = mstsObjectCenter.Z - cameraLocation.Location.Z;
			var distanceSquared = dx * dx + dz * dz;

			return distanceSquared < viewingRange * viewingRange;
		}

		/// <summary>
		/// If the nearest part of the object is within camera viewing distance
		/// and is within the object's defined viewing distance then
		/// we can see it.   The objectViewingDistance allows a small object
		/// to specify a cutoff beyond which the object can't be seen.
		/// </summary>
		public bool CanSee(Vector3 mstsObjectCenter, float objectRadius, float objectViewingDistance)
		{
			// whichever is less, camera or object
			if (Viewer.SettingsInt[(int)IntSettings.ViewingDistance] < objectViewingDistance)
				objectViewingDistance = Viewer.SettingsInt[(int)IntSettings.ViewingDistance];

			// account for the object's size
			var minDistance = objectViewingDistance + objectRadius;

			if (!InRange(mstsObjectCenter, minDistance)) return false;
			if (!InFOV(mstsObjectCenter, objectRadius)) return false;
			return true;
		}

		public bool CanSee(Matrix xnaMatrix, float objectRadius, float objectViewingDistance)
		{
			var mstsLocation = new Vector3(xnaMatrix.Translation.X, xnaMatrix.Translation.Y, -xnaMatrix.Translation.Z);
			return CanSee(mstsLocation, objectRadius, objectViewingDistance);
		}

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

		protected override Matrix GetCameraView()
		{
			return Matrix.CreateLookAt(XNALocation(cameraLocation), XNALocation(targetLocation), Vector3.UnitY);
		}
	}

	public abstract class RotatingCamera : Camera
	{
		protected float rotationYRadians = 0;
		protected float rotationXRadians = 0;

		protected RotatingCamera(Viewer3D viewer)
			: base(viewer)
		{
		}

		protected RotatingCamera(Viewer3D viewer, Camera previousCamera)
			: base(viewer, previousCamera)
		{
			float h, a, b;
			ORTSMath.MatrixToAngles(previousCamera.XNAView, out h, out a, out b);
			rotationXRadians = -b;
			rotationYRadians = -h;
		}

		public override void HandleUserInput(ElapsedTime elapsedTime)
		{
			var elapsedRealMilliseconds = elapsedTime.RealSeconds * 1000;

			// Rotation
			if (UserInput.IsMouseRightButtonDown())
			{
				rotationXRadians += UserInput.MouseMoveY() * elapsedRealMilliseconds / 1000f;
				rotationYRadians += UserInput.MouseMoveX() * elapsedRealMilliseconds / 1000f;
			}

			// Movement
			var speed = 1.0f;
			if (UserInput.IsKeyDown(Keys.RightShift) || UserInput.IsKeyDown(Keys.LeftShift))
				speed = 10.0f;
			if (UserInput.IsKeyDown(Keys.End))
				speed = 0.05f;
			Vector3 movement = new Vector3(0, 0, 0);

			if (UserInput.IsKeyDown(Keys.Left))
				if (UserInput.IsKeyDown(Keys.LeftControl))
					rotationYRadians += speed * elapsedRealMilliseconds / 1000f;
				else
					movement.X -= speed * elapsedRealMilliseconds / 10f;
			if (UserInput.IsKeyDown(Keys.Right))
				if (UserInput.IsKeyDown(Keys.LeftControl))
					rotationYRadians -= speed * elapsedRealMilliseconds / 1000f;
				else
					movement.X += speed * elapsedRealMilliseconds / 10f;
			if (UserInput.IsKeyDown(Keys.Up))
				if (UserInput.IsKeyDown(Keys.LeftControl))
					movement.Y += speed * elapsedRealMilliseconds / 10f;
				else
					movement.Z += speed * elapsedRealMilliseconds / 10f;
			if (UserInput.IsKeyDown(Keys.Down))
				if (UserInput.IsKeyDown(Keys.LeftControl))
					movement.Y -= speed * elapsedRealMilliseconds / 10f;
				else
					movement.Z -= speed * elapsedRealMilliseconds / 10f;

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

		protected override void OnActivate()
		{
			if (attachedCar == null || attachedCar.Train != Viewer.PlayerTrain)
			{
				if (Viewer.PlayerTrain.MUDirection == Direction.Forward)
					PositionViewer(true);
				else
					PositionViewer(false);
			}
			base.OnActivate();
		}

		public override void HandleUserInput(ElapsedTime elapsedTime)
		{
			if (UserInput.IsPressed(Keys.Home))
				PositionViewer(true);
			else if (UserInput.IsPressed(Keys.End))
				PositionViewer(false);
			base.HandleUserInput(elapsedTime);
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
		bool Forwards;

		public HeadOutCamera(Viewer3D viewer, HeadDirection headDirection)
			: base(viewer)
		{
			Forwards = headDirection == HeadDirection.Forward;
		}

		protected override void OnActivate()
		{
			if (attachedCar == null || attachedCar.Train != Viewer.PlayerTrain)
			{
				if (Viewer.PlayerTrain.MUDirection == Direction.Forward)
					PositionViewer(true);
				else
					PositionViewer(false);
			}
			base.OnActivate();
		}

		public override void HandleUserInput(ElapsedTime elapsedTime)
		{
			var elapsedRealMilliseconds = elapsedTime.RealSeconds * 1000;
			var speed = 1.0f;

			if (UserInput.IsKeyDown(Keys.RightShift) || UserInput.IsKeyDown(Keys.LeftShift))
				speed = 10.0f;
			if (UserInput.IsKeyDown(Keys.End))
				speed = 0.05f;

			if (UserInput.IsKeyDown(Keys.Left))
				rotationYRadians -= speed * elapsedRealMilliseconds / 1000f;
			if (UserInput.IsKeyDown(Keys.Right))
				rotationYRadians += speed * elapsedRealMilliseconds / 1000f;

			// Do this here so we can clamp the angles below.
			base.HandleUserInput(elapsedTime);

			if (Forwards)
				rotationYRadians = MathHelper.Clamp(rotationYRadians, 0, (float)Math.PI);
			else
				rotationYRadians = MathHelper.Clamp(rotationYRadians, -(float)Math.PI, 0);
		}

		void PositionViewer(bool front)
		{
			attachedCar = front ? Viewer.PlayerTrain.FirstCar : Viewer.PlayerTrain.LastCar;
			rotationYRadians = front == attachedCar.Flipped ? (float)Math.PI : 0;

			onboardLocation.X = 1.8f;
			if (attachedCar != null && attachedCar.FrontCabViewpoints.Count > 0 && attachedCar.FrontCabViewpoints[0].Location != null)
			{
				onboardLocation.Y = attachedCar.FrontCabViewpoints[0].Location.Y;
				onboardLocation.Z = attachedCar.FrontCabViewpoints[0].Location.Z;
			}
			else
			{
				onboardLocation.Y = 3.3f;
				onboardLocation.Z = attachedCar.Length / 2 - 1;
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
		protected override void OnActivate()
		{
			if (Viewer.PlayerLocomotive != null)
				attachedCar = Viewer.PlayerLocomotive;
			if (attachedCar.FrontCabViewpoints.Count == 0)
				return;
			ShiftView(0);

			base.OnActivate();
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
			if (UserInput.IsPressed(Keys.Left) && !UserInput.IsKeyDown(Keys.LeftControl))
				ShiftView(+1);
			else if (UserInput.IsPressed(Keys.Right) && !UserInput.IsKeyDown(Keys.LeftControl))
				ShiftView(-1);

			// Don't call this or we'll let the user rotate the camera!
			//base.HandleUserInput(elapsedTime);
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
		bool Front;

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
			positionYRadians = StartPositionYRadians;
		}

		protected override void OnActivate()
		{
			attachedCar = Front ? Viewer.PlayerTrain.FirstCar : Viewer.PlayerTrain.LastCar;
			rotationXRadians = positionXRadians;
			rotationYRadians = positionYRadians - (float)Math.PI;
			UpdateOnboardLocation();
			base.OnActivate();
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
			var elapsedRealSeconds = elapsedTime.RealSeconds;
			var speedMpS = 5f;
			if (UserInput.IsShiftDown())
				speedMpS = 35;
			float movement = speedMpS * elapsedRealSeconds;

			if (UserInput.IsKeyDown(Keys.Left))
			{
				positionYRadians += movement / 10;
				rotationYRadians += movement / 10;
				UpdateOnboardLocation();
			}
			else if (UserInput.IsKeyDown(Keys.Right))
			{
				positionYRadians -= movement / 10;
				rotationYRadians -= movement / 10;
				UpdateOnboardLocation();
			}

			if (UserInput.IsCtrlKeyDown(Keys.Down))
			{
				positionXRadians -= movement / 10f;
				rotationXRadians -= movement / 10f;
				if (positionXRadians < -1.5f) positionXRadians = -1.5f;
				UpdateOnboardLocation();
			}
			else if (UserInput.IsCtrlKeyDown(Keys.Up))
			{
				positionXRadians += movement / 10f;
				rotationXRadians += movement / 10f;
				if (positionXRadians > 1.5f) positionXRadians = 1.5f;
				UpdateOnboardLocation();
			}

			if (UserInput.IsKeyDown(Keys.Down) && !UserInput.IsCtrlKeyDown())
			{
				positionDistance += movement * positionDistance / 10;
				if (positionDistance < 1) positionDistance = 1;
				UpdateOnboardLocation();
			}
			else if (UserInput.IsKeyDown(Keys.Up) && !UserInput.IsCtrlKeyDown())
			{
				positionDistance -= movement * positionDistance / 10;
				if (positionDistance > 100) positionDistance = 100;
				UpdateOnboardLocation();
			}

			base.HandleUserInput(elapsedTime);
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
                var train = Viewer.PlayerTrain;

                // find first car with a passenger view
                attachedCar = null;
                foreach (TrainCar car in train.Cars)
                    if (car.PassengerViewpoints.Count > 0)
                    {
                        attachedCar = car;
                        break;
                    }
                return attachedCar != null;
            }
        }

		public override Camera.Styles Style { get { return Styles.Passenger; } }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            var elapsedRealMilliseconds = elapsedTime.RealSeconds * 1000;
            var speed = 1.0f;

            if (UserInput.IsKeyDown(Keys.RightShift) || UserInput.IsKeyDown(Keys.LeftShift))
                speed = 10.0f;
            if (UserInput.IsKeyDown(Keys.End))
                speed = 0.05f;

            if (UserInput.IsKeyDown(Keys.Left))
                rotationYRadians -= speed * elapsedRealMilliseconds / 1000f;
            if (UserInput.IsKeyDown(Keys.Right))
                rotationYRadians += speed * elapsedRealMilliseconds / 1000f;

            // Do this here so we can clamp the angles below.
            base.HandleUserInput(elapsedTime);

            rotationYRadians = MathHelper.Clamp(rotationYRadians, -(float)Math.PI / 2, (float)Math.PI / 2);
        }

        protected override void OnActivate()
		{
			var train = Viewer.PlayerTrain;

			// find first car with a passenger view
			attachedCar = null;
			foreach (TrainCar car in train.Cars)
				if (car.PassengerViewpoints.Count > 0)
				{
					attachedCar = car;
					break;
				}
			if (attachedCar == null)
				return; // no cars have a passenger view

			rotationYRadians = MSTSMath.M.Radians(attachedCar.PassengerViewpoints[0].StartDirection.Y);
			// TODO finish X and Z rotation
			onboardLocation = attachedCar.PassengerViewpoints[0].Location;

			base.OnActivate();
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
		WorldLocation TrackCameraLocation;

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

		public override void Update(ElapsedTime elapsedTime)
		{
			// TODO: What should we do here?
			if (Viewer.PlayerLocomotive == null)
			{
				base.Update(elapsedTime);
				return;
			}

			var trainForwards = Viewer.PlayerLocomotive.SpeedMpS >= 0;
			var trainCar = trainForwards ? Viewer.PlayerTrain.FirstCar : Viewer.PlayerTrain.LastCar;
			targetLocation = trainCar.WorldPosition.WorldLocation;
			if (WorldLocation.GetDistance2D(targetLocation, cameraLocation).Length() > MaximumDistance)
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
					cameraLocation.Location.Y = newLocationElevation + CameraNormalAltitude;
				}
				else
				{
					cameraLocation = new WorldLocation(tdb.TileX, tdb.TileZ, tdb.X, tdb.Y + CameraBridgeAltitude, tdb.Z);
				}
			}

			targetLocation.Location.Y += TargetAltitude;

			base.Update(elapsedTime);
		}
	}
} // namespace ORTS
