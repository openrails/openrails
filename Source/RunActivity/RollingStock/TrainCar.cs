using System;
using System.Collections;
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
        public float CouplerSlackM = 0f;// extra distance between cars

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

        // temporary values used to compute coupler forces
        public float CouplerForceA;
        public float CouplerForceB;
        public float CouplerForceC;
        public float CouplerForceG;
        public float CouplerForceR;
        public float CouplerForceU;

        // set when model is loaded
        public List<WheelSet> WheelSets = new List<WheelSet>();
        public bool WheelSetsLoaded = false;
        public List<TrainCarPart> Parts = new List<TrainCarPart>();

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
            outf.Write(SpeedMpS);
            outf.Write(CouplerSlackM);
        }

        // Game restore
        public virtual void Restore(BinaryReader inf)
        {
            Flipped = inf.ReadBoolean();
            BrakeSystem.Restore(inf);
            MotiveForceN = inf.ReadSingle();
            FrictionForceN = inf.ReadSingle();
            SpeedMpS = inf.ReadSingle();
            CouplerSlackM = inf.ReadSingle();
        }

        public virtual float GetMaximumCouplerSlackM()
        {
            return 0;
        }

        public virtual float GetMaximumCouplerForceN()
        {
            return 1e10f;
        }

        public void AddWheelSet(float offset, int bogie)
        {
            if (WheelSetsLoaded)
                return;
            WheelSet w = new WheelSet(offset, bogie);
            WheelSets.Add(w);
        }
        public void AddBogie(float offset, int matrix, int id)
        {
            if (WheelSetsLoaded)
                return;
            while (Parts.Count <= id)
                Parts.Add(new TrainCarPart(0, 0));
            Parts[id].OffsetM= offset;
            Parts[id].iMatrix= matrix;
        }
        public void SetupWheels()
        {
            if (WheelSets.Count < 2)
                return;
            if (Parts.Count == 0)
                Parts.Add(new TrainCarPart(0, 0));
            foreach (WheelSet w in WheelSets)
            {
                if (w.BogieIndex >= Parts.Count)
                    w.BogieIndex = 0;
                w.Part = Parts[w.BogieIndex];
                w.Part.SumWgt++;
            }
            WheelSets.Sort(WheelSets[0]);
            for (int i = 1; i < Parts.Count; i++)
                if (Parts[i].SumWgt > 1.5)
                    Parts[0].SumWgt++;
            if (Parts[0].SumWgt < 1.5)
            {
                foreach (WheelSet w in WheelSets)
                {
                    w.BogieIndex = 0;
                    w.Part = Parts[0];
                }
            }
#if false
            Console.WriteLine("length {0}", Length);
            foreach (WheelSet w in WheelSets)
                Console.WriteLine("wheel {0} {1}", w.OffsetM, w.BogieIndex);
            foreach (TrainCarPart p in Parts)
                Console.WriteLine("part {0} {1}", p.OffsetM, p.iMatrix);
#endif
            WheelSetsLoaded = true;
        }
        public void ComputePosition(TDBTraveller traveler, bool backToFront)
        {
            for (int j = 0; j < Parts.Count; j++)
                Parts[j].InitLineFit();
            int tileX = traveler.TileX;
            int tileZ = traveler.TileZ;
            if (Flipped == backToFront)
            {
                float o = -Length / 2;
                for (int k = 0; k < WheelSets.Count; k++)
                {
                    float d = WheelSets[k].OffsetM - o;
                    //Console.WriteLine("{0} {1} {2}", d, Length, WheelSets[k].OffsetM);
                    o = WheelSets[k].OffsetM;
                    traveler.Move(d);
                    float x = traveler.X + 2048 * (traveler.TileX - tileX);
                    float y = traveler.Y;
                    float z = traveler.Z + 2048 * (traveler.TileZ - tileZ);
                    WheelSets[k].Part.AddWheelSetLocation(1, o, x, y, z, 0);
                }
                o = Length / 2 - o;
                traveler.Move(o);
                //Console.WriteLine("{0} {1}", o, Length);
            }
            else
            {
                float o = Length / 2;
                for (int k = WheelSets.Count - 1; k>=0 ; k--)
                {
                    float d = o - WheelSets[k].OffsetM;
                    //Console.WriteLine("{0} {1} {2}", d, Length, WheelSets[k].OffsetM);
                    o = WheelSets[k].OffsetM;
                    traveler.Move(d);
                    float x = traveler.X + 2048 * (traveler.TileX - tileX);
                    float y = traveler.Y;
                    float z = traveler.Z + 2048 * (traveler.TileZ - tileZ);
                    WheelSets[k].Part.AddWheelSetLocation(1, o, x, y, z, 0);
                }
                o = Length / 2 + o;
                traveler.Move(o);
                //Console.WriteLine("{0} {1}", o, Length);
            }
            TrainCarPart p0= Parts[0];
            for (int i=1; i<Parts.Count; i++)
            {
                TrainCarPart p= Parts[i];
                p.FindCenterLine();
                if (p.SumWgt > 1.5)
                    p0.AddPartLocation(1, p);
            }
            p0.FindCenterLine();
            Vector3 fwd = new Vector3(p0.B[0], p0.B[1], -p0.B[2]);
            fwd.Normalize();
            Vector3 side = Vector3.Cross(Vector3.Up, fwd);
            side.Normalize();
            Vector3 up = Vector3.Cross(fwd, side);
            //Console.WriteLine("fwd {0}", fwd);
            //Console.WriteLine("side {0}", side);
            //Console.WriteLine("up {0}", up);
            Matrix m = Matrix.Identity;
            m.M11 = side.X;
            m.M12 = side.Y;
            m.M13 = side.Z;
            m.M21 = up.X;
            m.M22 = up.Y;
            m.M23 = up.Z;
            m.M31 = fwd.X;
            m.M32 = fwd.Y;
            m.M33 = fwd.Z;
            m.M41 = p0.A[0];
            m.M42 = p0.A[1] + 0.275f;
            m.M43 = -p0.A[2];
            WorldPosition.XNAMatrix = m;
            WorldPosition.TileX = tileX;
            WorldPosition.TileZ = tileZ;
            //Console.WriteLine(" {0}", m.ToString());
            // calculate truck angles
            for (int i = 1; i < Parts.Count; i++)
            {
                TrainCarPart p = Parts[i];
                if (p.SumWgt < .5)
                    continue;
                if (p.SumWgt < 1.5)
                {   // single axle pony trunk
                    float d = p.OffsetM - p.SumOffset / p.SumWgt;
                    if (-.2 < d && d < .2)
                        continue;
                    p.AddWheelSetLocation(1, p.OffsetM, p0.A[0] + p.OffsetM * p0.B[0], p0.A[1] + p.OffsetM * p0.B[1], p0.A[2] + p.OffsetM * p0.B[2], 0);
                    p.FindCenterLine();
                }
                Vector3 fwd1 = new Vector3(p.B[0], p.B[1], -p.B[2]);
                fwd1.Normalize();
                p.Cos = Vector3.Dot(fwd, fwd1);
                if (p.Cos >= .99999f)
                    p.Sin = 0;
                else
                {
                    p.Sin = (float)Math.Sqrt(1 - p.Cos * p.Cos);
                    if (fwd.X * fwd1.Z < fwd.Z * fwd1.X)
                        p.Sin = -p.Sin;
                }
                //Console.WriteLine("cs {0} {1} {2}", i, p.Cos, p.Sin);
            }
        }

    }

    public class WheelSet : IComparer<WheelSet>
    {
        public float OffsetM;   // distance from center of model, positive forward
        public int BogieIndex;
        public TrainCarPart Part;
        public WheelSet(float offset, int bogie)
        {
            OffsetM = offset;
            BogieIndex = bogie;
        }
        int IComparer<WheelSet>.Compare(WheelSet a, WheelSet b)
        {
            if (a.OffsetM > b.OffsetM) return 1;
            if (a.OffsetM < b.OffsetM) return -1;
            return 0;
        }
    }
    // data and methods used to align trucks and models to track
    public class TrainCarPart
    {
        public float OffsetM;   // distance from center of model, positive forward
        public int iMatrix;     // matrix in shape that needs to be moved
        public float Cos= 1;       // truck angle cosine
        public float Sin= 0;       // truck angle sin
        // line fitting variables
        public float SumWgt;
        public float SumOffset;
        public float SumOffsetSq;
        public float[] SumX= new float[4];
        public float[] SumXOffset= new float[4];
        public float[] A= new float[4];
        public float[] B= new float[4];
        public TrainCarPart(float offset, int i)
        {
            OffsetM = offset;
            iMatrix = i;
        }
        public void InitLineFit()
        {
            SumWgt = SumOffset = SumOffsetSq = 0;
            for (int i = 0; i < 4; i++)
                SumX[i] = SumXOffset[i] = 0;
        }
        public void AddWheelSetLocation(float w, float o, float x, float y, float z, float t)
        {
            SumWgt += w;
            SumOffset += w*o;
            SumOffsetSq += w * o * o;
            SumX[0] += w * x;
            SumXOffset[0] += w * x * o;
            SumX[1] += w * y;
            SumXOffset[1] += w * y * o;
            SumX[2] += w * z;
            SumXOffset[2] += w * z * o;
            SumX[3] += w * t;
            SumXOffset[3] += w * t * o;
        }
        public void AddPartLocation(float w, TrainCarPart part)
        {
            SumWgt += w;
            SumOffset += w * part.OffsetM;
            SumOffsetSq += w * part.OffsetM * part.OffsetM;
            for (int i = 0; i < 4; i++)
            {
                float x = part.A[i] + part.OffsetM * part.B[i];
                SumX[i] += w * x;
                SumXOffset[i] += w * x * part.OffsetM;
            }
        }
        public void FindCenterLine()
        {
            float d = SumWgt * SumOffsetSq - SumOffset * SumOffset;
            if (d > 1e-20)
            {
                for (int i = 0; i < 4; i++)
                {
                    A[i] = (SumOffsetSq * SumX[i] - SumOffset * SumXOffset[i]) / d;
                    B[i] = (SumWgt * SumXOffset[i] - SumOffset * SumX[i]) / d;
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    A[i] = SumX[i] / SumWgt;
                    B[i] = 0;
                }
            }
            //for (int j = 0; j < 4; j++)
            //    Console.Write(" {0} {1}", A[j], B[j]);
            //Console.WriteLine(" {0}", OffsetM);
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
