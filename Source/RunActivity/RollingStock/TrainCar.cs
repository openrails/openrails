using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace ORTS
{
    public class ViewPoint
    {
        public Vector3 Location;
        public Vector3 StartDirection;
        public Vector3 RotationLimit;
    }

    public abstract class TrainCar
    {
        // some properties of this car
        public float Length = 40;       // derived classes must overwrite these defaults
        public float MassKG = 10000;
        public bool IsDriveable = false;
        //public bool HasCabView = false;

        // instance variables set by train train physics when it creates the traincar
        public bool Flipped = false; // the car is reversed in the consist
        public Train Train = null;  // the car is connected to this train

        // status of the traincar - set by the train physics after it call calls TrainCar.Update()
        public WorldPosition WorldPosition = new WorldPosition();  // current position of the car
        public float DistanceM = 0.0f;  // running total of distance travelled - always positive, updated by train physics
        public float SpeedMpS = 0.0f; // meters pers second; updated by train physics, relative to direction of car  50mph = 22MpS

        // represents the MU line travelling through the train.  Uncontrolled locos respond to these commands.
        public float ThrottlePercent { get { return Train.MUThrottlePercent; } set { Train.MUThrottlePercent = value; } }
        public Direction Direction { 
            get { return Flipped ? DirectionControl.Flip(Train.MUDirection) : Train.MUDirection; } 
            set { Train.MUDirection = Flipped ? DirectionControl.Flip( value ) : value; } }
        public BrakeSystem BrakeSystem = null;

        // TrainCar.Update() must set these variables
        public float MotiveForceN = 0.0f;   // ie motor power in Newtons  - signed relative to direction of car - 
        public float FrictionForceN = 0.0f; // in Newtons ( kg.m/s^2 ) unsigned, includes effects of curvature

        // For use by cameras, initialized in MSTSWagon class and its derived classes
        public List<ViewPoint> FrontCabViewpoints = new List<ViewPoint>();
        public List<ViewPoint> RearCabViewpoints = new List<ViewPoint>();
        public List<ViewPoint> PassengerViewpoints = new List<ViewPoint>();

        // Load 3D geometry into this 3D viewer and return it as a TrainCarViewer
        public abstract TrainCarViewer GetViewer(Viewer3D viewer);

        // called when its time to update the MotiveForce and FrictionForce
        public abstract void Update(float elapsedClockSeconds);

        // Notifications from others of key outside events, ie coupling etc, pantograph up etc
        public abstract void SignalEvent(EventID eventID);


    }


    public abstract class TrainCarViewer
    {
         // TODO add view location and limits
        public TrainCar Car;

        protected Viewer3D Viewer;

        public TrainCarViewer( Viewer3D viewer, TrainCar car)
        {
            Car = car;
            Viewer = viewer;

        }

        public abstract void HandleUserInput(ElapsedTime elapsedTime);

        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public abstract void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime);
    }

}
