using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;

namespace ORTS
{
    public class ViewPoint
    {
        public Vector3 Location;
        public Vector3 StartDirection;
        public Vector3 RotationLimit;
    }

    public class TrainCar
    {
        public string WagFilePath;

        // some properties of this car
        public float Length = 40;       // derived classes must overwrite these defaults
        public float MassKG = 10000;
        public bool IsDriveable = false;
        //public bool HasCabView = false;

        //public Lights Lights = null;

        // instance variables set by train train physics when it creates the traincar
        public Train Train = null;  // the car is connected to this train
        public bool Flipped = false; // the car is reversed in the consist

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
        public float GravityForceN = 0.0f;   // Newtons  - signed relative to direction of car - 
        public float FrictionForceN = 0.0f; // in Newtons ( kg.m/s^2 ) unsigned, includes effects of curvature

        // For use by cameras, initialized in MSTSWagon class and its derived classes
        public List<ViewPoint> FrontCabViewpoints = new List<ViewPoint>();
        public List<ViewPoint> RearCabViewpoints = new List<ViewPoint>();
        public List<ViewPoint> PassengerViewpoints = new List<ViewPoint>();

        // Load 3D geometry into this 3D viewer and return it as a TrainCarViewer
        public virtual TrainCarViewer GetViewer(Viewer3D viewer) { return null; }

        // called when its time to update the MotiveForce and FrictionForce
        public virtual void Update(float elapsedClockSeconds)
        {
            // gravity force, M32 is up component of forward vector
            GravityForceN = MassKG * 9.8f * WorldPosition.XNAMatrix.M32;
            //Console.WriteLine("mf {0} {1} {2}", MotiveForceN, WorldPosition.XNAMatrix.Forward, WorldPosition.XNAMatrix.M32);
        }

        // Notifications from others of key outside events, ie coupling etc, pantograph up etc
        public virtual void SignalEvent(EventID eventID) { }

        public virtual string GetStatus() { return null; }

        public TrainCar(string wagFile)
        {
            WagFilePath = wagFile;
        }

        // Game save
        public virtual void Save(BinaryWriter outf)
        {
            outf.Write(Flipped);
            BrakeSystem.Save(outf);
            outf.Write(MotiveForceN);
            outf.Write(FrictionForceN);
        }

        // Game restore
        public virtual void Restore(BinaryReader inf)
        {
            Flipped = inf.ReadBoolean();
            BrakeSystem.Restore(inf);
            MotiveForceN = inf.ReadSingle();
            FrictionForceN = inf.ReadSingle();
        }
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
