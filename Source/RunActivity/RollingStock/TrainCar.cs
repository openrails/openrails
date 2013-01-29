// Define this to log the wheel configurations on cars as they are loaded.
//#define DEBUG_WHEELS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

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
        public readonly Simulator Simulator;
        public readonly string WagFilePath;
		public string RealWagFilePath; //we are substituting missing remote cars in MP, so need to remember this

        // Some housekeeping
        public bool IsPartOfActiveTrain = true;

        // some properties of this car
        public float Length = 40;       // derived classes must overwrite these defaults
        public float Height = 4;        // derived classes must overwrite these defaults
        public float MassKG = 10000;
        public bool IsDriveable = false;
	    public bool IsFreight = false;  // indication freight wagon or passenger car

        public LightCollection Lights = null;
        public int Headlight = 0;

        // instance variables set by train physics when it creates the traincar
        public Train Train = null;  // the car is connected to this train
        public bool Flipped = false; // the car is reversed in the consist
        public int UiD;
        public string CarID = "AI"; //CarID = "0 - UID" if player train, "ActivityID - UID" if loose consist, "AI" if AI train

        // status of the traincar - set by the train physics after it calls TrainCar.Update()
        public WorldPosition WorldPosition = new WorldPosition();  // current position of the car
        public float DistanceM = 0.0f;  // running total of distance travelled - always positive, updated by train physics
        public float _SpeedMpS = 0.0f; // meters per second; updated by train physics, relative to direction of car  50mph = 22MpS
        public float _PrevSpeedMpS = 0.0f;
        public float CouplerSlackM = 0f;// extra distance between cars (calculated based on relative speeds)
        public float CouplerSlack2M = 0f;// slack calculated using draft gear force
        public bool WheelSlip = false;// true if locomotive wheels slipping
        public float _AccelerationMpSS = 0.0f;

        private IIRFilter AccelerationFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, 1.0f, 0.1f);

        public float SpeedMpS
        {
            get
            {
                return _SpeedMpS;
            }
            set
            {
                _SpeedMpS = value;
            }
        }

        public float AccelerationMpSS
        { 
            get{ return _AccelerationMpSS; }
        }

        // represents the MU line travelling through the train.  Uncontrolled locos respond to these commands.
        public float ThrottlePercent { get { return Train.MUThrottlePercent; } set { Train.MUThrottlePercent = value; } }
        public float DynamicBrakePercent { get { return Train.MUDynamicBrakePercent; } set { Train.MUDynamicBrakePercent = value; } }
        public Direction Direction
        { 
            get { return Flipped ? DirectionControl.Flip(Train.MUDirection) : Train.MUDirection; } 
            set { Train.MUDirection = Flipped ? DirectionControl.Flip( value ) : value; } }
        public BrakeSystem BrakeSystem = null;

        // TrainCar.Update() must set these variables
        public float MotiveForceN = 0.0f;   // ie motor power in Newtons  - signed relative to direction of car - 
        public float GravityForceN = 0.0f;  // Newtons  - signed relative to direction of car - 
        public float FrictionForceN = 0.0f; // in Newtons ( kg.m/s^2 ) unsigned, includes effects of curvature
        public float BrakeForceN = 0.0f;    // brake force in Newtons
        public float TotalForceN; // sum of all the forces active on car relative train direction

        // temporary values used to compute coupler forces
        public float CouplerForceA; // left hand side value below diagonal
        public float CouplerForceB; // left hand side value on diagonal
        public float CouplerForceC; // left hand side value above diagonal
        public float CouplerForceG; // temporary value used by solver
        public float CouplerForceR; // right hand side value
        public float CouplerForceU; // result
        public bool  CouplerOverloaded; //true when coupler force is higher then Break limit

        // set when model is loaded
        public List<WheelAxle> WheelAxles = new List<WheelAxle>();
        public bool WheelAxlesLoaded = false;
        public List<TrainCarPart> Parts = new List<TrainCarPart>();

        // For use by cameras, initialized in MSTSWagon class and its derived classes
        public List<ViewPoint> PassengerViewpoints = new List<ViewPoint>();
        public List<ViewPoint> HeadOutViewpoints = new List<ViewPoint>();

        // Load 3D geometry into this 3D viewer and return it as a TrainCarViewer
        public virtual TrainCarViewer GetViewer(Viewer3D viewer) { return null; }

        // called when it's time to update the MotiveForce and FrictionForce
        public virtual void Update(float elapsedClockSeconds)
        {
            // gravity force, M32 is up component of forward vector
            GravityForceN = MassKG * 9.8f * WorldPosition.XNAMatrix.M32;
            // acceleration
            if (elapsedClockSeconds > 0.0f)
            {
                _AccelerationMpSS = (_SpeedMpS - _PrevSpeedMpS) / elapsedClockSeconds;
                
                if (Simulator.UseAdvancedAdhesion)
                    _AccelerationMpSS = AccelerationFilter.Filter(_AccelerationMpSS, elapsedClockSeconds);

                _PrevSpeedMpS = _SpeedMpS;
            }
        }

        // Notifications from others of key outside events, ie coupling etc, pantograph up etc
        public virtual void SignalEvent(EventID eventID) { }

        public virtual string GetStatus() { return null; }
        public virtual string GetTrainBrakeStatus() { return null; }
        public virtual string GetEngineBrakeStatus() { return null; }
        public virtual string GetDynamicBrakeStatus() { return null; }
        public virtual bool GetSanderOn() { return false; }

        public TrainCar()
        {
        }

        public TrainCar(Simulator simulator, string wagFile)
        {
			Simulator = simulator;
            WagFilePath = wagFile;
			RealWagFilePath = wagFile;
        }

        // Game save
        public virtual void Save(BinaryWriter outf)
        {
            outf.Write(Flipped);
			outf.Write(UiD);
			outf.Write(CarID);
			BrakeSystem.Save(outf);
            outf.Write(MotiveForceN);
            outf.Write(FrictionForceN);
            outf.Write(SpeedMpS);
            outf.Write(CouplerSlackM);
            outf.Write( Headlight );
        }

        // Game restore
        public virtual void Restore(BinaryReader inf)
        {
            Flipped = inf.ReadBoolean();
			UiD = inf.ReadInt32();
			CarID = inf.ReadString();
			BrakeSystem.Restore(inf);
            MotiveForceN = inf.ReadSingle();
            FrictionForceN = inf.ReadSingle();
            SpeedMpS = inf.ReadSingle();
            _PrevSpeedMpS = SpeedMpS;
            CouplerSlackM = inf.ReadSingle();
            Headlight = inf.ReadInt32();
        }

        public bool HasFrontCab { get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Front;
                if (loco == null || loco.CabViewList.Count <= i) return false;
                return (loco.CabViewList[i].ViewPointList.Count > 0);
            }
        }

        public bool HasRearCab { get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Rear;
                if (loco == null || loco.CabViewList.Count <= i) return false;
                return (loco.CabViewList[i].ViewPointList.Count > 0);
            }
        }

#if NEW_SIGNALLING
        public virtual bool GetCabFlipped()
        {
            return false;
        }
#endif

        public virtual float GetCouplerZeroLengthM()
        {
            return 0;
        }

        public virtual float GetCouplerStiffnessNpM()
        {
            return 2e7f;
        }

        public virtual float GetMaximumCouplerSlack1M()
        {
            return .012f;
        }

        public virtual float GetMaximumCouplerSlack2M()
        {
            return .12f;
        }

        public virtual float GetMaximumCouplerForceN()
        {
            return 1e10f;
        }
        
        public virtual void CopyCoupler(TrainCar other)
        {
            CouplerSlackM = other.CouplerSlackM;
            CouplerSlack2M = other.CouplerSlack2M;
        }

        public virtual void CopyControllerSettings(TrainCar other)
        {
            Headlight = other.Headlight;
        }

        public void AddWheelSet(float offset, int bogie, int parentMatrix)
        {
            if (WheelAxlesLoaded)
                return;
            WheelAxles.Add(new WheelAxle(offset, bogie, parentMatrix));
        }

        public void AddBogie(float offset, int matrix, int id)
        {
            if (WheelAxlesLoaded)
                return;
            while (Parts.Count <= id)
                Parts.Add(new TrainCarPart(0, 0));
            Parts[id].OffsetM = offset;
            Parts[id].iMatrix = matrix;
        }

        public void SetUpWheels()
        {
            
#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", Length);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif

           // No axles but we have bogies.
           if (WheelAxles.Count == 0 && Parts.Count > 1)
            {
                // Fake the axles by pretending each has 1 axle.
                foreach (var part in Parts.Skip(1))
                    WheelAxles.Add(new WheelAxle(part.OffsetM, part.iMatrix, 0));
                Trace.TraceInformation("Wheel axle data faked based on {1} bogies for {0}", WagFilePath, Parts.Count - 1);
            }
            // Less that two axles is bad.
            // In this situation, it is not bad because spine car lacks both WheelAxles and Parts
           //if (WheelAxles.Count < 2)
           //{
           //    Trace.TraceWarning("Car has less than two axles in {0}", WagFilePath);
           //    return;
           //}
            // No parts means no bogies (always?), so make sure we've got Parts[0] for the car itself.
            if (Parts.Count == 0)
                Parts.Add(new TrainCarPart(0, 0));
            // Validate the axles' assigned bogies and count up the axles on each bogie.
            if (WheelAxles.Count > 0)
            {
                foreach (var w in WheelAxles)
                {
                    if (w.BogieIndex >= Parts.Count)
                        w.BogieIndex = 0;
                    if (w.BogieMatrix > 0)
                    {
                        for (var i = 0; i < Parts.Count; i++)
                            if (Parts[i].iMatrix == w.BogieMatrix)
                            {
                                w.BogieIndex = i;
                                break;
                            }
                    }
                    w.Part = Parts[w.BogieIndex];
                    w.Part.SumWgt++;
                }
                // Make sure the axles are sorted by OffsetM along the car.
                // Attempting to sort car w/o WheelAxles will resort to an error.
                WheelAxles.Sort(WheelAxles[0]);
            }
            // Make sure the axles are sorted by OffsetM along the car.
            //WheelAxles.Sort(WheelAxles[0]);
            // Count up the number of bogies (parts) with at least 2 axles.
            for (var i = 1; i < Parts.Count; i++)
                if (Parts[i].SumWgt > 1.5)
                    Parts[0].SumWgt++;
            // Check for articulation and if we have enough wheels.
            bool articulatedFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articulatedRear = !WheelAxles.Any(a => a.OffsetM > 0);
            //var carIndex = Train.Cars.IndexOf(this);
            if (!articulatedFront && !articulatedRear && (Parts[0].SumWgt < 1.5))
            {
                // Not articulated, but not enough wheels/bogies attached to the car.
                Trace.TraceWarning("Car with less than two axles/bogies ({1} axles, {2} bogies) in {0}", WagFilePath, WheelAxles.Count, Parts.Count - 1);
                // Put all the axles directly on the car, ignoring any bogies.
                foreach (WheelAxle w in WheelAxles)
                {
                    w.BogieIndex = 0;
                    w.Part = Parts[0];
                }
            }
            // Using WheelAxles.Count test to control WheelAxlesLoaded flag.
            if (WheelAxles.Count > 2)
            {
                WheelAxlesLoaded = true;
            }
                                                                                 
#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", Length);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
            // Decided to control what is sent to SetUpWheelsArticulation()by using
            // WheelAxlesLoaded as a flag.  This way, wagons that have to be processed are included
            // and the rest left out.
            if (Train != null)
                foreach (var car in Train.Cars)
                {
                    if (car.WheelAxlesLoaded)
                        car.SetUpWheelsArticulation();
                }
        } // end SetUpWheels()

        void SetUpWheelsArticulation()
        {
            // If there are no forward wheels, this car is articulated (joined
            // to the car in front) at the front. Likewise for the rear.
            bool articulatedFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articulatedRear = !WheelAxles.Any(a => a.OffsetM > 0);
            // If the car is articulated, steal some wheels from nearby cars.
            
                if (articulatedFront || articulatedRear)
                {
                    var carIndex = Train.Cars.IndexOf(this);
                    if (articulatedFront && carIndex > 0)
                    {
                        var otherCar = Train.Cars[carIndex - 1];
                        var otherPart = otherCar.Parts.OrderBy(p => -p.OffsetM).FirstOrDefault();
                        if (otherPart == null)
                        {
                            WheelAxles.Add(new WheelAxle(Length / 2, 0, 0) { Part = Parts[0] });
                        }
                        else
                        {
                            var offset = otherCar.Length / 2 + Length / 2;
                            var otherPartIndex = otherCar.Parts.IndexOf(otherPart);
                            var otherAxles = otherCar.WheelAxles.Where(a => a.BogieIndex == otherPartIndex);
                            var part = new TrainCarPart(otherPart.OffsetM - offset, 0) { SumWgt = otherPart.SumWgt };
                            WheelAxles.AddRange(otherAxles.Select(a => new WheelAxle(a.OffsetM - offset, Parts.Count, 0) { Part = part }));
                            Parts.Add(part);
                        }
                    }
                    if (articulatedRear && carIndex < Train.Cars.Count - 1)
                    {
                        var otherCar = Train.Cars[carIndex + 1];
                        var otherPart = otherCar.Parts.OrderBy(p => p.OffsetM).FirstOrDefault();
                        if (otherPart == null)
                        {
                            WheelAxles.Add(new WheelAxle(-Length / 2, 0, 0) { Part = Parts[0] });
                        }
                        else
                        {
                            var offset = otherCar.Length / 2 + Length / 2;
                            var otherPartIndex = otherCar.Parts.IndexOf(otherPart);
                            var otherAxles = otherCar.WheelAxles.Where(a => a.BogieIndex == otherPartIndex);
                            var part = new TrainCarPart(otherPart.OffsetM + offset, 0) { SumWgt = otherPart.SumWgt };
                            WheelAxles.AddRange(otherAxles.Select(a => new WheelAxle(a.OffsetM + offset, Parts.Count, 0) { Part = part }));
                            Parts.Add(part);
                        }
                    }
                    WheelAxles.Sort(WheelAxles[0]);
                }
           
#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", Length);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
        } // end SetUpWheelsArticulation()

        public void ComputePosition(Traveller traveler, bool backToFront)
        {
            for (int j = 0; j < Parts.Count; j++)
                Parts[j].InitLineFit();
            int tileX = traveler.TileX;
            int tileZ = traveler.TileZ;
            if (Flipped == backToFront)
            {
                float o = -Length / 2;
                for (int k = 0; k < WheelAxles.Count; k++)
                {
                    float d = WheelAxles[k].OffsetM - o;
                    o = WheelAxles[k].OffsetM;
                    traveler.Move(d);
                    float x = traveler.X + 2048 * (traveler.TileX - tileX);
                    float y = traveler.Y;
                    float z = traveler.Z + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0);
                }
                o = Length / 2 - o;
                traveler.Move(o);
            }
            else
            {
                float o = Length / 2;
                for (int k = WheelAxles.Count - 1; k>=0 ; k--)
                {
                    float d = o - WheelAxles[k].OffsetM;
                    o = WheelAxles[k].OffsetM;
                    traveler.Move(d);
                    float x = traveler.X + 2048 * (traveler.TileX - tileX);
                    float y = traveler.Y;
                    float z = traveler.Z + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0);
                }
                o = Length / 2 + o;
                traveler.Move(o);
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
            // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
            if (fwd.X != 0 && fwd.Y != 0 && fwd.Z != 0)
                fwd.Normalize();
            Vector3 side = Vector3.Cross(Vector3.Up, fwd);
            // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
            if (side.X != 0 && side.Y != 0 && side.Z != 0)
                side.Normalize();
            Vector3 up = Vector3.Cross(fwd, side);
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
            }
        }
    }

    public class WheelAxle : IComparer<WheelAxle>
    {
        public float OffsetM;   // distance from center of model, positive forward
        public int BogieIndex;
        public int BogieMatrix;
        public TrainCarPart Part;
        public WheelAxle(float offset, int bogie, int parentMatrix)
        {
            OffsetM = offset;
            BogieIndex = bogie;
            BogieMatrix = parentMatrix;
        }
        int IComparer<WheelAxle>.Compare(WheelAxle a, WheelAxle b)
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
            SumOffset += w * o;
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
        }
    }


    public abstract class TrainCarViewer
    {
         // TODO add view location and limits
        public TrainCar Car;
        public LightDrawer lightDrawer = null;

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

        [CallOnThread("Loader")]
        internal virtual void LoadForPlayer() { }

        [CallOnThread("Loader")]
        internal abstract void Mark();
    }

}
