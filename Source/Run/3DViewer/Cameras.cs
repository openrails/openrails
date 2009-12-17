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
        protected Viewer Viewer;
        protected float RotationYRadians = 0f;
        protected float RotationXRadians = 0;

        public Matrix XNAView;   // set by the camera class
        public Matrix XNAProjection;
        public float RightFrustrumA, RightFrustrumB;  // the right frustrum is precomputed,
        //       represented by this equation 0 = Ax + Bz; where A^2 + B^2 = 1;

        public volatile int TileX;                           // camera position and orientation TODO, move these to the camera class
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
        public TrainCarSimulator AttachedToCar = null;  // we are attached to this car, or null if we are free roaming

        // Internals
        private int MouseX = 0, MouseY = 0;


        protected Camera(Viewer viewer)
        {
            Viewer = viewer;
            ScreenChanged();
        }

        public Camera(Viewer viewer, Camera previousCamera) // maintain visual continuity
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
            Update(null);  
        }

        /// <summary>
        /// Notifies the camera that the screen dimensions have changed
        /// Sets up a new XNAProjection
        /// </summary>
        public virtual void ScreenChanged()
        {
            float aspectRatio = (float)Viewer.GraphicsDevice.Viewport.Width / (float)Viewer.GraphicsDevice.Viewport.Height;
            float farPlaneDistance = SkyConstants.skyRadius + 100;  // so far the sky is the biggest object in view
            float fovWidthRadians = MathHelper.ToRadians(45.0f);
            XNAProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, 0.1f, farPlaneDistance);
            RightFrustrumA = (float)Math.Cos(fovWidthRadians / 2 * aspectRatio);  // precompute the right edge of the view frustrum
            RightFrustrumB = (float)Math.Sin(fovWidthRadians / 2 * aspectRatio);
        }


        public virtual void HandleInput(KeyboardInput keyboard, MouseState mouse, GameTime gameTime)
        {
            float milliseconds = (float)gameTime.ElapsedGameTime.Milliseconds;

            // Rotation
            if (mouse.RightButton == ButtonState.Pressed)
                RotationXRadians += (mouse.Y - MouseY) * milliseconds / 1000f;
            if (mouse.RightButton == ButtonState.Pressed)
                RotationYRadians -= (MouseX - mouse.X) * milliseconds / 1000f;


            // Movement
            float speed = 1.0f;
            if (keyboard.IsKeyDown(Keys.RightShift) || keyboard.IsKeyDown(Keys.LeftShift))
                speed = 10.0f;
            if (keyboard.IsKeyDown(Keys.End))
                speed = 0.05f;
            Vector3 movement = new Vector3(0, 0, 0);

            if (keyboard.IsKeyDown(Keys.Left))
                if (keyboard.IsKeyDown(Keys.LeftControl))
                    RotationYRadians += speed * milliseconds / 1000f;
                else
                    movement.X -= speed * milliseconds / 10f;
            if (keyboard.IsKeyDown(Keys.Right))
                if (keyboard.IsKeyDown(Keys.LeftControl))
                    RotationYRadians -= speed * milliseconds / 1000f;
                else
                    movement.X += speed * milliseconds / 10f;
            if (keyboard.IsKeyDown(Keys.Up))
                if (keyboard.IsKeyDown(Keys.LeftControl))
                    movement.Y += speed * milliseconds / 10f;
                else
                    movement.Z += speed * milliseconds / 10f;
            if (keyboard.IsKeyDown(Keys.Down))
                if (keyboard.IsKeyDown(Keys.LeftControl))
                    movement.Y -= speed * milliseconds / 10f;
                else
                    movement.Z -= speed * milliseconds / 10f;

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

            MouseX = mouse.X;
            MouseY = mouse.Y;
        }

        /// <summary>
        /// Update the XNAView based on the current camera pose and location
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime)
        {
            Vector3 lookAtPosition;

            lookAtPosition = new Vector3(0, 0, 1);
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(RotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(RotationYRadians));
            lookAtPosition += Location;

            Vector3 xnaLookAtPosition = new Vector3(lookAtPosition.X, lookAtPosition.Y, -lookAtPosition.Z);
            Vector3 xnaCameraPosition = new Vector3(Location.X, Location.Y, -Location.Z);

            Viewer.Camera.XNAView = Matrix.CreateLookAt(xnaCameraPosition, xnaLookAtPosition, Vector3.Up);
        }

        /// <summary>
        /// If the nearest part of the object is within camera viewing distance
        /// and is within the object's defined viewing distance then
        /// we can see it.   The objectViewingDistance allows a small object
        /// to specify a cutoff beyond which the object can't be seen.
        /// </summary>
        /// <param name="objectCenter"></param>
        /// <param name="objectRadius"></param>
        /// <param name="objectViewingDistance"></param>
        /// <returns></returns>
        public bool CanSee(Vector3 mstsObjectCenter, float objectRadius, float objectViewingDistance)
        {
            // cull for distance
            float distance = Math.Abs(mstsObjectCenter.X - Location.X) + Math.Abs(mstsObjectCenter.Z - Location.Z);

            if (distance < objectRadius * 2)
                return true;// we are 'in the object' - can't cull it

            distance -= objectRadius;
            if (distance > Viewer.ViewingDistance || distance > objectViewingDistance)
                return false;  // nearest part of object is too far away

            // cull for fov
            Vector3 xnaObjectCenter = new Vector3(mstsObjectCenter.X, mstsObjectCenter.Y, -mstsObjectCenter.Z);
            Vector3.Transform(ref xnaObjectCenter, ref XNAView, out xnaObjectCenter);

            if (xnaObjectCenter.Z > objectRadius * 2)
                return false;  // behind camera


            // Todo, cull for left and right
            float d = MSTSMath.M.DistanceToLine(RightFrustrumA, RightFrustrumB, 0, xnaObjectCenter.X, xnaObjectCenter.Z);
            if (d > objectRadius * 2)
                return false;  // right of view

            d = MSTSMath.M.DistanceToLine(-RightFrustrumA, RightFrustrumB, 0, xnaObjectCenter.X, xnaObjectCenter.Z);
            if (d > objectRadius * 2)
                return false; // left of view


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

        public AttachedCamera(Viewer viewer)
            : base(viewer)
        {
        }

        public override void Update(GameTime gameTime)
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

    }

    /// <summary>
    /// The brakeman is on the car at the front or back
    /// TODO, allow brakeman to jump on or off cars
    /// </summary>
    public class BrakemanCamera : AttachedCamera
    {
        public BrakemanCamera(Viewer viewer)
            : base(viewer)
        {
        }

        public override void Activate()
        {
            Train playerTrain = Viewer.Simulator.PlayerTrain;
            if ( AttachedToCar == null || playerTrain != AttachedToCar.Train)
            {
                if( playerTrain.SpeedMpS >= 0 )
                    GotoFront();
                else
                    GotoBack();
            }
            base.Activate();
        }

        public override void HandleInput(KeyboardInput keyboard, MouseState mouse, GameTime gameTime)
        {
            if (keyboard.IsPressed(Keys.Home))
                GotoFront();
            else if (keyboard.IsPressed(Keys.End))
                GotoBack();

            base.HandleInput(keyboard, mouse, gameTime);
        }

        public void PositionViewer()
        {
            float z = AttachedToCar.WagFile.Wagon.Length / 2 - 0.3f;
            if (AttachedToCar.Flipped)
                z *= -1;
            OnboardLocation = new Vector3(1.8f, 2.0f, z);
        }

        public void GotoFront()
        {
            Train train = Viewer.Simulator.PlayerTrain;
            AttachedToCar = train.FirstCar;
            PositionViewer();
        }

        public void GotoBack()
        {
            Train train = Viewer.Simulator.PlayerTrain;
            AttachedToCar = train.LastCar;
            PositionViewer();
            OnboardLocation.Z *= -1;
        }

    }

    public class CabCamera : AttachedCamera
    {
        int iLocation = 0; // handle left and right side views in cab

        public CabCamera(Viewer viewer)
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
            if (Viewer.Simulator.PlayerLocomotive != null)
                AttachedToCar = Viewer.Simulator.PlayerLocomotive;
            if (AttachedToCar.CVFFile == null)
                return;
            ShiftView(0);
            base.Activate();
        }

        private void ShiftView(int index)
        {
            iLocation += index;

            if (iLocation < 0)
                iLocation = AttachedToCar.CVFFile.Locations.Count - 1;
            else if (iLocation >= AttachedToCar.CVFFile.Locations.Count)
                iLocation = 0;

            RotationYRadians = MSTSMath.M.Radians(AttachedToCar.CVFFile.Directions[iLocation].Y);
            OnboardLocation = AttachedToCar.CVFFile.Locations[iLocation];
        }

        public override void HandleInput(KeyboardInput keyboard, MouseState mouse, GameTime gameTime)
        {
            if (keyboard.IsPressed(Keys.Left))
                ShiftView(-1);
            else if (keyboard.IsPressed(Keys.Right))
                ShiftView(+1);
            
            base.HandleInput(keyboard, mouse, gameTime);
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

        public TrackingCamera(Viewer viewer, Tether tetherAttachment )
            : base( viewer )
        {
            TetherAttachment = tetherAttachment;
            if (TetherAttachment == Tether.ToRear)
                rotationR -= (float)Math.PI;
        }

        public override void Activate()
        {
            Train playerTrain = Viewer.Simulator.PlayerTrain;
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
            OnboardLocation.Z += AttachedToCar.WagFile.Wagon.Length / 2.0f *(TetherAttachment == Tether.ToFront ? 1 : -1);
        }

        public override void HandleInput(KeyboardInput keyboard, MouseState mouse, GameTime gameTime)
        {
            float elapsedS = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float speedMpS = 5;
            float movement = speedMpS * elapsedS;

            if (keyboard.IsKeyDown(Keys.Left))
            {
                rotationR += movement/10;
                RotationYRadians += movement / 10;
                UpdateOnboardLocation();
            }
            else if (keyboard.IsKeyDown(Keys.Right))
            {
                rotationR -= movement/10;
                RotationYRadians -= movement / 10;
                UpdateOnboardLocation();
            }

            if (keyboard.IsAltKeyDown(Keys.Down))
            {
                tiltR -= movement / 10f;
                RotationXRadians -= movement / 10f;
                if (tiltR < -1.5f) tiltR = -1.5f;
                UpdateOnboardLocation();
            }
            else if (keyboard.IsAltKeyDown(Keys.Up))
            {
                tiltR += movement / 10f;
                RotationXRadians += movement / 10f;
                if (tiltR > 1.5f) tiltR = 1.5f;
                UpdateOnboardLocation();
            }

            if (keyboard.IsKeyDown(Keys.Down) && !keyboard.IsAltKeyDown())
            {
                distance += movement * distance/10;
                if (distance < 1) distance = 1;
                UpdateOnboardLocation();
            }
            else if (keyboard.IsKeyDown(Keys.Up) && !keyboard.IsAltKeyDown() )
            {
                distance -= movement * distance/10;
                if (distance > 100) distance = 100;
                UpdateOnboardLocation();
            }


            base.HandleInput(keyboard, mouse, gameTime);
        }

    }
    
    public class PassengerCamera : AttachedCamera
    {

        public PassengerCamera(Viewer viewer)
            : base(viewer)
        {
            ViewPoint = ViewPoints.Passenger;
        }

        public override void Activate()
        {
            Train train = Viewer.Simulator.PlayerTrain;

            // find first car with a passenger view
            AttachedToCar = null;
            foreach (TrainCarSimulator car in train.Cars)
                if (car.WagFile.HasInsideView)
                {
                    AttachedToCar = car;
                    break;
                }
            if (AttachedToCar == null) 
                return; // no cars have a passenger view

            OnboardLocation = AttachedToCar.WagFile.Wagon.Inside.PassengerCabinHeadPos;

            base.Activate();
        }

    } // Class PassengerCamera

} // namespace ORTS
