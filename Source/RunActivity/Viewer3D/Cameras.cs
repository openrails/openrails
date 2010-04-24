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
    public class Camera
    {
        protected Viewer3D Viewer;
        protected float RotationYRadians = 0f;
        protected float RotationXRadians = 0;

        public Matrix XNAView;   // set by the camera class
        public Matrix XNAProjection;
        public static Matrix XNASkyProjection;
        public static Matrix XNAShadowViewProjection;
        public float RightFrustrumA, RightFrustrumB;  // the right frustrum is precomputed,
        //       represented by this equation 0 = Ax + Bz; where A^2 + B^2 = 1;

        public volatile int TileX;                           // camera position and orientation 
        public volatile int TileZ;
        public Vector3 Location = new Vector3(45, 242, 178);
        public WorldLocation WorldLocation
        {
            get
            {
                WorldLocation worldLocation = new WorldLocation();
                worldLocation.TileX = TileX;
                worldLocation.TileZ = TileZ;
                worldLocation.Location = Location;
                return worldLocation;
            }
        }

        public ViewPoints ViewPoint = ViewPoints.External;  
        public enum ViewPoints { External, Cab, Passenger };
        public TrainCar AttachedToCar = null;  // we are attached to this car, or null if we are free roaming
        public TrainCarViewer AttachedToCarViewer = null; // the car we are attached to has this viewer

        // Internals
        private int MouseX = 0, MouseY = 0;


        protected Camera(Viewer3D viewer)
        {
            Viewer = viewer;
            ScreenChanged();
        }

        public Camera(Viewer3D viewer, Camera previousCamera) // maintain visual continuity
        {
            Viewer = viewer;
            TileX = previousCamera.TileX;
            TileZ = previousCamera.TileZ;
            Location = previousCamera.Location;
            float h,a,b;
            ORTSMath.MatrixToAngles( previousCamera.XNAView, out h, out a, out b );
            RotationXRadians = -b;
            RotationYRadians = -h;
            ScreenChanged();
        }

        public virtual void Activate()
        {
            Viewer.Camera = this;
            Update( ElapsedTime.Zero );
        }

        /// <summary>
        /// Notifies the camera that the screen dimensions have changed
        /// Sets up a new XNAProjection
        /// </summary>
        public virtual void ScreenChanged()
        {
            float aspectRatio = (float)Viewer.GraphicsDevice.Viewport.Width / (float)Viewer.GraphicsDevice.Viewport.Height;
            float farPlaneDistance =  SkyConstants.skyRadius + 100;  // so far the sky is the biggest object in view
            float fovWidthRadians = MathHelper.ToRadians(45.0f);
            XNAProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, 0.5f, Viewer.ViewingDistance);
            XNASkyProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, 0.5f, farPlaneDistance);    // TODO remove? 
            XNAShadowViewProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, 0.5f, 300);    // TODO remove? 
            RightFrustrumA = (float)Math.Cos(fovWidthRadians / 2 * aspectRatio);  // precompute the right edge of the view frustrum
            RightFrustrumB = (float)Math.Sin(fovWidthRadians / 2 * aspectRatio);
        }




        public virtual void HandleUserInput(ElapsedTime elapsedTime)
        {
        }

        public virtual void Update(ElapsedTime elapsedTime)
        {
            Vector3 lookAtPosition;

            lookAtPosition = new Vector3(0, 0, 1);
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(RotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(RotationYRadians));
            lookAtPosition += Location;

            Vector3 xnaLookAtPosition = new Vector3(lookAtPosition.X, lookAtPosition.Y, -lookAtPosition.Z);
            Vector3 xnaCameraPosition = new Vector3(Location.X, Location.Y, -Location.Z);

            XNAView = Matrix.CreateLookAt(xnaCameraPosition, xnaLookAtPosition, Vector3.Up);
        }

        /// <summary>
        /// Update the XNAView based on the current camera pose and location
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            float elapsedRealMilliseconds = elapsedTime.RealSeconds * 1000;

            // Rotation
            if (UserInput.MouseState.RightButton == ButtonState.Pressed)
                RotationXRadians += (UserInput.MouseState.Y - MouseY) * elapsedRealMilliseconds /1000f;
            if (UserInput.MouseState.RightButton == ButtonState.Pressed)
                RotationYRadians -= (MouseX - UserInput.MouseState.X) * elapsedRealMilliseconds /1000f;


            // Movement
            float speed = 1.0f;
            if (UserInput.IsKeyDown(Keys.RightShift) || UserInput.IsKeyDown(Keys.LeftShift))
                speed = 10.0f;
            if (UserInput.IsKeyDown(Keys.End))
                speed = 0.05f;
            Vector3 movement = new Vector3(0, 0, 0);

            if (UserInput.IsKeyDown(Keys.Left))
                if (UserInput.IsKeyDown(Keys.LeftControl))
                    RotationYRadians += speed * elapsedRealMilliseconds / 1000f ;
                else
                    movement.X -= speed * elapsedRealMilliseconds / 10f;
            if (UserInput.IsKeyDown(Keys.Right))
                if (UserInput.IsKeyDown(Keys.LeftControl))
                    RotationYRadians -= speed * elapsedRealMilliseconds / 1000f;
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

            movement = Vector3.Transform(movement, Matrix.CreateRotationX(RotationXRadians));
            movement = Vector3.Transform(movement, Matrix.CreateRotationY(RotationYRadians));
            Location += movement;
            // normalize position on tile
            Vector3 location = Location;
            while (location.X > 1024) { location.X -= 2048; ++TileX; };
            while (location.X < -1024) { location.X += 2048; --TileX; };
            while (location.Z > 1024) { location.Z -= 2048; ++TileZ; };
            while (location.Z < -1024) { location.Z += 2048; --TileZ; };
            Location = location;

            // TODO, clamp movement above the ground

            MouseX = UserInput.MouseState.X;
            MouseY = UserInput.MouseState.Y;

            Update( elapsedTime);
            frame.SetCamera(ref XNAView, ref XNAProjection);
        }


        // Cull for fov
        public bool InFOV(Vector3 mstsObjectCenter, float objectRadius)
        {
            Vector3 xnaObjectCenter = new Vector3(mstsObjectCenter.X, mstsObjectCenter.Y, -mstsObjectCenter.Z);
            Vector3.Transform(ref xnaObjectCenter, ref XNAView, out xnaObjectCenter);

            if (xnaObjectCenter.Z > objectRadius * 2)
                return false;  // behind camera

            // Cull for left and right
            float d = MSTSMath.M.DistanceToLine(RightFrustrumA, RightFrustrumB, 0, xnaObjectCenter.X, xnaObjectCenter.Z);
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
            float dx = mstsObjectCenter.X - Location.X;
            float dz = mstsObjectCenter.Z - Location.Z;
            float distanceSquared = dx * dx + dz * dz;

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
            if (Viewer.ViewingDistance < objectViewingDistance)
                objectViewingDistance = Viewer.ViewingDistance;

            // account for the object's size
            float minDistance = objectViewingDistance + objectRadius;

            if (!InRange(mstsObjectCenter, minDistance )) return false;
            if (!InFOV( mstsObjectCenter, objectRadius )) return false;
            return true;
        }

        public bool CanSee(Matrix xnaMatrix, float objectRadius, float objectViewingDistance)
        {
            Vector3 mstsLocation = new Vector3(xnaMatrix.Translation.X, xnaMatrix.Translation.Y, -xnaMatrix.Translation.Z);
            return CanSee(mstsLocation, objectRadius, objectViewingDistance);
        }

        /// <summary>
        /// Returns a position in XNA space relative to the camera's tile
        /// </summary>
        /// <param name="worldLocation"></param>
        /// <returns></returns>
        public Vector3 XNALocation(WorldLocation worldLocation)
        {
            Vector3 xnaVector = worldLocation.Location;
            xnaVector.X += 2048 * (worldLocation.TileX - TileX);
            xnaVector.Z += 2048 * (worldLocation.TileZ - TileZ);
            xnaVector.Z *= -1;
            return xnaVector;
        }

    }

    /// <summary>
    /// This represents a camera attached to a car.  
    /// It moves with the car.
    /// </summary>
    public class AttachedCamera : Camera
    {
        protected Vector3 OnboardLocation;   // where on the car is the viewer

        public AttachedCamera(Viewer3D viewer)
            : base(viewer)
        {
        }

        public override void Update( ElapsedTime elapsedTime )
        {
            Vector3 xnaOnboardLocation = new Vector3(OnboardLocation.X, OnboardLocation.Y, -OnboardLocation.Z);

            Vector3 xnaLocation = Vector3.Transform(xnaOnboardLocation, AttachedToCar.WorldPosition.XNAMatrix);

            TileX = AttachedToCar.WorldPosition.TileX;
            TileZ = AttachedToCar.WorldPosition.TileZ;

            /* Viewer */
            Location = new Vector3(xnaLocation.X, xnaLocation.Y, -xnaLocation.Z);

            //  HANDLE ROTATION
            Vector3 xnaLookAtPosition;

            xnaLookAtPosition = new Vector3(0, 0, -1);
            xnaLookAtPosition = Vector3.Transform(xnaLookAtPosition, Matrix.CreateRotationX(-RotationXRadians));
            xnaLookAtPosition = Vector3.Transform(xnaLookAtPosition, Matrix.CreateRotationY(-RotationYRadians));
            xnaLookAtPosition += xnaOnboardLocation;
            xnaLookAtPosition = Vector3.Transform(xnaLookAtPosition, AttachedToCar.WorldPosition.XNAMatrix);

            XNAView = Matrix.CreateLookAt(xnaLocation, xnaLookAtPosition, Vector3.Up);

        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            base.PrepareFrame(frame, elapsedTime);
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

        public override void Activate()
        {
            Train playerTrain = Viewer.PlayerTrain;
            if ( AttachedToCar == null || playerTrain != AttachedToCar.Train)
            {
                if( playerTrain.MUDirection == Direction.Forward )
                    GotoFront();
                else
                    GotoBack();
            }
            base.Activate();
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(Keys.Home))
                GotoFront();
            else if (UserInput.IsPressed(Keys.End))
                GotoBack();

            base.HandleUserInput(elapsedTime);
        }

        public void PositionViewer()
        {
            float z = AttachedToCar.Length / 2 - 0.3f;
            if (AttachedToCar.Flipped)
                z *= -1;
            OnboardLocation = new Vector3(1.8f, 2.0f, z);
        }

        public void GotoFront()
        {
            Train train = Viewer.PlayerTrain;
            AttachedToCar = train.FirstCar;
            if (AttachedToCar.Flipped)
                RotationYRadians = (float)Math.PI;
            else
                RotationYRadians = 0;
            PositionViewer();
        }

        public void GotoBack()
        {
            Train train = Viewer.PlayerTrain;
            AttachedToCar = train.LastCar;
            if (AttachedToCar.Flipped)
                RotationYRadians = 0;
            else
                RotationYRadians = (float)Math.PI;
            PositionViewer();
            OnboardLocation.Z *= -1;
        }

    }

    public class CabCamera : AttachedCamera
    {
        int iLocation = 0; // handle left and right side views in cab

        public CabCamera(Viewer3D viewer)
            : base(viewer)
        {
            ViewPoint = ViewPoints.Cab;
        }

        /// <summary>
        /// Make this the viewer's current camera.
        /// If the locomotive has no cab view, then do nothing.
        /// </summary>
        public override void Activate()
        {
            if (Viewer.PlayerLocomotive != null)
                AttachedToCar = Viewer.PlayerLocomotive;
            if (AttachedToCar.FrontCabViewpoints.Count == 0 )
                return;
            ShiftView(0);
            base.Activate();
        }

        private void ShiftView(int index)
        {
            iLocation += index;

            if (iLocation < 0)
                iLocation = AttachedToCar.FrontCabViewpoints.Count - 1;
            else if (iLocation >= AttachedToCar.FrontCabViewpoints.Count)
                iLocation = 0;

            RotationYRadians = MSTSMath.M.Radians(AttachedToCar.FrontCabViewpoints[iLocation].StartDirection.Y);
            // TODO add X rotation and Z rotation
            OnboardLocation = AttachedToCar.FrontCabViewpoints[iLocation].Location;
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(Keys.Left) && !UserInput.IsKeyDown(Keys.LeftControl))
                    ShiftView(-1);
            else if (UserInput.IsPressed(Keys.Right) && !UserInput.IsKeyDown(Keys.LeftControl))
                ShiftView(+1);
            
            base.HandleUserInput(elapsedTime);
        }
    }

    // Used by a tracking camera to determine where it is attached.
    public enum Tether { ToFront, ToRear };

    public class TrackingCamera : AttachedCamera
    {
        float distance = 9;
        float rotationR = 0.387f;
        float tiltR = 0.399f;

        Tether TetherAttachment;  // ie ToFront or ToRear

        public TrackingCamera(Viewer3D viewer, Tether tetherAttachment )
            : base( viewer )
        {
            TetherAttachment = tetherAttachment;
            if (TetherAttachment == Tether.ToRear)
                rotationR -= (float)Math.PI;
        }

        public override void Activate()
        {
            Train playerTrain = Viewer.PlayerTrain;
            if (TetherAttachment == Tether.ToFront)
                AttachedToCar = playerTrain.FirstCar;
            else
                AttachedToCar = playerTrain.LastCar;
            RotationXRadians = tiltR;
            RotationYRadians = rotationR - (float)Math.PI;
            UpdateOnboardLocation();
            base.Activate();
        }

        /// <summary>
        /// From distance and elevation variables
        /// </summary>
        public void UpdateOnboardLocation()
        {
            // normalize rotation
            while (rotationR > Math.PI) rotationR -= 2f * (float)Math.PI;
            while (rotationR < -Math.PI) rotationR += 2f * (float)Math.PI;

            OnboardLocation.Z = distance;
            OnboardLocation.X = 0;
            OnboardLocation.Y = 2;
            OnboardLocation = Vector3.Transform(OnboardLocation, Matrix.CreateRotationX(-tiltR));
            OnboardLocation = Vector3.Transform(OnboardLocation, Matrix.CreateRotationY(rotationR));
            OnboardLocation.Z += AttachedToCar.Length / 2.0f *(TetherAttachment == Tether.ToFront ? 1 : -1);
        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            base.HandleUserInput(elapsedTime);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            float elapsedRealSeconds = elapsedTime.RealSeconds;
            float speedMpS = 5;
            if (UserInput.IsShiftDown())
                speedMpS = 35;
            float movement = speedMpS * elapsedRealSeconds;


            if (UserInput.IsKeyDown(Keys.Left))
            {
                rotationR += movement / 10;
                RotationYRadians += movement / 10;
                UpdateOnboardLocation();
            }
            else if (UserInput.IsKeyDown(Keys.Right))
            {
                rotationR -= movement / 10;
                RotationYRadians -= movement / 10;
                UpdateOnboardLocation();
            }

            if (UserInput.IsCtrlKeyDown(Keys.Down))
            {
                tiltR -= movement / 10f;
                RotationXRadians -= movement / 10f;
                if (tiltR < -1.5f) tiltR = -1.5f;
                UpdateOnboardLocation();
            }
            else if (UserInput.IsCtrlKeyDown(Keys.Up))
            {
                tiltR += movement / 10f;
                RotationXRadians += movement / 10f;
                if (tiltR > 1.5f) tiltR = 1.5f;
                UpdateOnboardLocation();
            }

            if (UserInput.IsKeyDown(Keys.Down) && !UserInput.IsCtrlKeyDown())
            {
                distance += movement * distance / 10;
                if (distance < 1) distance = 1;
                UpdateOnboardLocation();
            }
            else if (UserInput.IsKeyDown(Keys.Up) && !UserInput.IsCtrlKeyDown())
            {
                distance -= movement * distance / 10;
                if (distance > 100) distance = 100;
                UpdateOnboardLocation();
            }

            base.PrepareFrame(frame, elapsedTime);
        }

    }
    
    public class PassengerCamera : AttachedCamera
    {

        public PassengerCamera(Viewer3D viewer)
            : base(viewer)
        {
            ViewPoint = ViewPoints.Passenger;
        }

        public override void Activate()
        {
            Train train = Viewer.PlayerTrain;

            // find first car with a passenger view
            AttachedToCar = null;
            foreach (TrainCar car in train.Cars)
                if (car.PassengerViewpoints.Count > 0)
                {
                    AttachedToCar = car;
                    break;
                }
            if (AttachedToCar == null) 
                return; // no cars have a passenger view

            RotationYRadians = MSTSMath.M.Radians(AttachedToCar.PassengerViewpoints[0].StartDirection.Y);
            // TODO finish X and Z rotation
            OnboardLocation = AttachedToCar.PassengerViewpoints[0].Location;

            base.Activate();
        }

    } // Class PassengerCamera

} // namespace ORTS
