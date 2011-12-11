/*
 *    TrainCarSimulator
 *    
 *    TrainCarViewer
 *    
 *  Every TrainCar generates a FrictionForce.
 *  
 *  The viewer is a separate class object since there could be multiple 
 *  viewers potentially on different devices for a single car. 
 *  
 */
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using MSTS;

namespace ORTS
{

///////////////////////////////////////////////////
///   SIMULATION BEHAVIOUR
///////////////////////////////////////////////////


    /// <summary>
    /// Represents the physical motion and behaviour of the car.
    /// </summary>
    public class MSTSWagon: TrainCar
    {
		public bool Pan = false;     // false = down; some wagon has pantograph
		public bool FrontPanUp = false; // if the Front pantograph is up
		public bool AftPanUp = false; // if the Aft pantograph is up
		public int NumPantograph = 0;
		public bool DoorLeftOpen = false;
		public bool DoorRightOpen = false;
		public bool MirrorOpen = false;

        // simulation parameters
        public float Variable1 = 0.0f;  // used to convey status to soundsource
        public float Variable2 = 0.0f;
        public float Variable3 = 0.0f;

        // wag file data
        public string MainShapeFileName = null;
        public string FreightShapeFileName = null;
        public float FreightAnimHeight = 0;
        public string InteriorShapeFileName = null; // passenger view shape file name
        public string MainSoundFileName = null;
        public string InteriorSoundFileName = null;
        public float WheelRadiusM = 1;          // provide some defaults in case its missing from the wag
        public float DriverWheelRadiusM = 1.5f;    // provide some defaults in case its missing from the wag
        public float Friction0N = 0;    // static friction
        public float DavisAN = 0;       // davis equation constant
        public float DavisBNSpM = 0;    // davis equation constant for speed
        public float DavisCNSSpMM = 0;  // davis equation constant for speed squared
        public List<MSTSCoupling> Couplers = new List<MSTSCoupling>();
        public float Adhesion1 = .27f;   // 1st MSTS adheasion value
        public float Adhesion2 = .49f;   // 2nd MSTS adheasion value
        public float Adhesion3 = 2;   // 3rd MSTS adheasion value
        public float Curtius_KnifflerA = 7.5f;               //Curtius-Kniffler constants                   A
        public float Curtius_KnifflerB = 44.0f;              // (adhesion coeficient)       umax = ---------------------  + C
        public float Curtius_KnifflerC = 0.161f;             //                                      speedMpS * 3.6 + B
        public float AdhesionK = 0.7f;   //slip characteristics slope
        //public AntislipControl AntislipControl = AntislipControl.None;
        public float AxleInertiaKgm2 = 0;   //axle inertia
        public float WheelSpeedMpS = 0;
        public float SlipWarningTresholdPercent = 70;

        public MSTSBrakeSystem MSTSBrakeSystem { get { return (MSTSBrakeSystem)base.BrakeSystem; } }

        public MSTSWagon(Simulator simulator, string wagFilePath, TrainCar previousCar)
            : base(simulator, wagFilePath, previousCar)
        {
            if (CarManager.LoadedCars.ContainsKey(wagFilePath))
            {
                InitializeFromCopy(CarManager.LoadedCars[wagFilePath]);
            }
            else
            {
                InitializeFromWagFile(wagFilePath);
                CarManager.LoadedCars.Add(wagFilePath, this);
            }
        }

        /// <summary>
        /// This initializer is called when we haven't loaded this type of car before
        /// and must read it new from the wag file.
        /// </summary>
        public virtual void InitializeFromWagFile(string wagFilePath)
        {
            string dir = Path.GetDirectoryName(wagFilePath);
            string file = Path.GetFileName(wagFilePath);
            string orFile = dir + @"\openrails\" + file;
            if (File.Exists(orFile))
                wagFilePath = orFile;
            using (STFReader stf = new STFReader(wagFilePath, true))
                while (!stf.Eof)
                {
                    string token = stf.ReadItem();
                    Parse(stf.Tree.ToLower(), stf);
                }
             if (BrakeSystem == null)
                    BrakeSystem = new AirSinglePipe(this);
        }

        string brakeSystemType = null;

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(wagonshape": MainShapeFileName = stf.ReadStringBlock(null); break;
                case "wagon(freightanim":
                    stf.MustMatch("(");
                    FreightShapeFileName = stf.ReadString();
                    FreightAnimHeight = stf.ReadFloat(STFReader.UNITS.Distance, null) - stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(size":
                    stf.MustMatch("(");
                    stf.ReadFloat(STFReader.UNITS.Distance, null);
                    Height = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    Length = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(mass": MassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(wheelradius": WheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(wheelradius": DriverWheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(sound": MainSoundFileName = stf.ReadStringBlock(null); break;
                case "wagon(friction": ParseFriction(stf); break;
                case "wagon(brakesystemtype":
                    brakeSystemType = stf.ReadStringBlock(null).ToLower();
                    BrakeSystem = MSTSBrakeSystem.Create(brakeSystemType, this);
                    break;
                case "wagon(coupling":
                    Couplers.Add(new MSTSCoupling());
                    break;
                case "wagon(coupling(couplinghasrigidconnection":
                    Couplers[Couplers.Count - 1].Rigid = stf.ReadBoolBlock(true);
                    break;
                case "wagon(coupling(spring(stiffness":
                    stf.MustMatch("(");
                    Couplers[Couplers.Count - 1].SetStiffness(stf.ReadFloat(STFReader.UNITS.Stiffness, null), stf.ReadFloat(STFReader.UNITS.Stiffness, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(r0":
                    stf.MustMatch("(");
                    Couplers[Couplers.Count - 1].SetR0(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(adheasion":
                    stf.MustMatch("(");
                    Adhesion1 = stf.ReadFloat(STFReader.UNITS.Any, null);
                    Adhesion2 = stf.ReadFloat(STFReader.UNITS.Any, null);
                    Adhesion3 = stf.ReadFloat(STFReader.UNITS.Any, null);
                    stf.ReadFloat(STFReader.UNITS.Any, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(curtius_kniffler":   
                    stf.MustMatch("(");                      //e.g. Wagon ( OR_adhesion ( Curtius_Kniffler ( 7.5 44 0.161 0.7 ) ) )
                    Curtius_KnifflerA = stf.ReadFloat(STFReader.UNITS.Any, 7.5f);   if (Curtius_KnifflerA <= 0) Curtius_KnifflerA = 7.5f;
                    Curtius_KnifflerB = stf.ReadFloat(STFReader.UNITS.Any, 44.0f);  if (Curtius_KnifflerB <= 0) Curtius_KnifflerB = 44.0f;
                    Curtius_KnifflerC = stf.ReadFloat(STFReader.UNITS.Any, 0.161f); if (Curtius_KnifflerC <= 0) Curtius_KnifflerA = 0.161f;
                    AdhesionK = stf.ReadFloat(STFReader.UNITS.Any, 0.7f);           if (AdhesionK <= 0) AdhesionK = 0.7f;
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(slipwarningtreshold":
                    stf.MustMatch("(");
                    SlipWarningTresholdPercent = stf.ReadFloat(STFReader.UNITS.Any, 70.0f); if (SlipWarningTresholdPercent <= 0) SlipWarningTresholdPercent = 70.0f ; 
                    stf.ReadFloat(STFReader.UNITS.Any, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(antislip":
                    stf.MustMatch("(");
                    //AntislipControl = stf.ReadStringBlock(null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(wheelset(axle(inertia":
                    stf.MustMatch("(");                    
                    AxleInertiaKgm2 = stf.ReadFloat(STFReader.UNITS.Any, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(or_adhesion(wheelset(axle(radius":
                    stf.MustMatch("(");
                    AxleInertiaKgm2 = stf.ReadFloatBlock(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(lights":
                    if (Simulator.Settings.TrainLights)
                    {
                        try { Lights = new LightCollection(stf); }
                        catch { Lights = null; }
                    }
                    else
                        stf.SkipBlock();
                    break;
                case "wagon(inside": ParseWagonInside(stf); break;
                default:
                    if (MSTSBrakeSystem != null)
                        MSTSBrakeSystem.Parse(lowercasetoken, stf);
                    break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// 
        /// IMPORTANT NOTE:  everything you initialized in parse, must be initialized here
        /// </summary>
        public virtual void InitializeFromCopy(MSTSWagon copy)
        {
            MainShapeFileName = copy.MainShapeFileName;
            FreightShapeFileName = copy.FreightShapeFileName;
            FreightAnimHeight = copy.FreightAnimHeight;
            InteriorShapeFileName = copy.InteriorShapeFileName;
            MainSoundFileName = copy.MainSoundFileName;
            InteriorSoundFileName = copy.InteriorSoundFileName;
            WheelRadiusM = copy.WheelRadiusM;
            DriverWheelRadiusM = copy.DriverWheelRadiusM;
            Friction0N = copy.Friction0N;
            DavisAN = copy.DavisAN;
            DavisBNSpM = copy.DavisBNSpM;
            DavisCNSSpMM = copy.DavisCNSSpMM;
            Length = copy.Length;
			Height = copy.Height;
            MassKG = copy.MassKG;
            Adhesion1 = copy.Adhesion1;
            Adhesion2 = copy.Adhesion2;
            Adhesion3 = copy.Adhesion3;
            Curtius_KnifflerA = copy.Curtius_KnifflerA;
            Curtius_KnifflerB = copy.Curtius_KnifflerB;
            Curtius_KnifflerC = copy.Curtius_KnifflerC;
            AdhesionK = copy.AdhesionK;
            AxleInertiaKgm2 = copy.AxleInertiaKgm2;
            SlipWarningTresholdPercent = copy.SlipWarningTresholdPercent;
            Lights = copy.Lights;
            foreach (ViewPoint passengerViewPoint in copy.PassengerViewpoints)
                PassengerViewpoints.Add(passengerViewPoint);
            foreach (ViewPoint frontCabViewPoint in copy.FrontCabViewpoints)
                FrontCabViewpoints.Add(frontCabViewPoint);
            foreach (ViewPoint rearCabViewPoint in copy.RearCabViewpoints)
                RearCabViewpoints.Add(rearCabViewPoint);
            foreach (ViewPoint headOutViewPoint in copy.HeadOutViewpoints)
                HeadOutViewpoints.Add(headOutViewPoint);
            foreach (MSTSCoupling coupler in copy.Couplers)
                Couplers.Add(coupler);

            brakeSystemType = copy.brakeSystemType;
            BrakeSystem = MSTSBrakeSystem.Create(brakeSystemType, this);
            MSTSBrakeSystem.InitializeFromCopy(copy.BrakeSystem);
        }
        private void ParseWagonInside(STFReader stf)
        {
            ViewPoint passengerViewPoint = new ViewPoint();
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("sound", ()=>{ InteriorSoundFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("passengercabinfile", ()=>{ InteriorShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("passengercabinheadpos", ()=>{ passengerViewPoint.Location = stf.ReadVector3Block(STFReader.UNITS.Distance, new Vector3()); }),
                new STFReader.TokenProcessor("rotationlimit", ()=>{ passengerViewPoint.RotationLimit = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
                new STFReader.TokenProcessor("startdirection", ()=>{ passengerViewPoint.StartDirection = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
            });
            PassengerViewpoints.Add(passengerViewPoint);
        }
        public void ParseFriction(STFReader stf)
        {
            stf.MustMatch("(");
            float c1 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
            float e1 = stf.ReadFloat(STFReader.UNITS.None, null);
            float v2 = stf.ReadFloat(STFReader.UNITS.Speed,null);
            float c2 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
            float e2 = stf.ReadFloat(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
            if (v2 < 0 || v2 > 4.4407f)
            {   // not fcalc ignore friction and use default davis equation
                // Starting Friction 
                //
                //                      Above Freezing   Below Freezing
                //    Journal Bearing      25 lb/ton        35 lb/ton   (short ton)
                //     Roller Bearing       5 lb/ton        15 lb/ton
                //
                // [2009-10-25 from http://www.arema.org/publications/pgre/ ]
                Friction0N = MassKG * 30f /* lb/ton */ * 4.84e-3f;  // convert lbs/short-ton to N/kg
                DavisAN = 6.3743f * MassKG / 1000 + 128.998f * 4;
                DavisBNSpM = .49358f * MassKG / 1000;
                DavisCNSSpMM = .11979f * 100 / 10.76f;
            }
            else
            {   // probably fcalc, recover approximate davis equation
                float mps1 = v2;
                float mps2 = 80 * .44704f;
                float s = mps2 - mps1;
                float x1 = mps1 * mps1;
                float x2 = mps2 * mps2;
                float sx = (x2 - x1) / 2;
                float y0 = c1 * (float)Math.Pow(mps1, e1) + c2 * mps1;
                float y1 = c2 * (float)Math.Pow(mps1, e2) * mps1;
                float y2 = c2 * (float)Math.Pow(mps2, e2) * mps2;
                float sy = y0 * (mps2 - mps1) + (y2 - y1) / (1 + e2);
                y1 *= mps1;
                y2 *= mps2;
                float syx = y0 * (x2 - x1) / 2 + (y2 - y1) / (2 + e2);
                x1 *= mps1;
                x2 *= mps2;
                float sx2 = (x2 - x1) / 3;
                y1 *= mps1;
                y2 *= mps2;
                float syx2 = y0 * (x2 - x1) / 3 + (y2 - y1) / (3 + e2);
                x1 *= mps1;
                x2 *= mps2;
                float sx3 = (x2 - x1) / 4;
                x1 *= mps1;
                x2 *= mps2;
                float sx4 = (x2 - x1) / 5;
                float s1 = syx - sy * sx / s;
                float s2 = sx * sx2 / s - sx3;
                float s3 = sx2 - sx * sx / s;
                float s4 = syx2 - sy * sx2 / s;
                float s5 = sx2 * sx2 / s - sx4;
                float s6 = sx3 - sx * sx2 / s;
                DavisCNSSpMM = (s1 * s6 - s3 * s4) / (s3 * s5 - s2 * s6);
                DavisBNSpM = (s1 + DavisCNSSpMM * s2) / s3;
                DavisAN = (sy - DavisBNSpM * sx - DavisCNSSpMM * sx2) / s;
                Friction0N = c1;
                if (e1 < 0)
                    Friction0N *= (float)Math.Pow(.0025 * .44704, e1);
            }
            //Console.WriteLine("friction {0} {1} {2} {3} {4}", c1, e1, v2, c2, e2);
            //Console.WriteLine("davis {0} {1} {2} {3}", Friction0N, DavisAN, DavisBNSpM, DavisCNSSpMM);
        }
        public float ParseFloat(string token)
        {   // is there a better way to ignore any suffix?
            while (token.Length > 0)
            {
                try
                {
                    return float.Parse(token);
                }
                catch (System.Exception)
                {
                    token = token.Substring(0, token.Length - 1);
                }
            }
            return 0;
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            outf.Write(Variable1);
            outf.Write(Variable2);
            outf.Write(Variable3);
            outf.Write(Friction0N);
            outf.Write(DavisAN);
            outf.Write(DavisBNSpM);
            outf.Write(DavisCNSSpMM);
            outf.Write(Couplers.Count);
            foreach (MSTSCoupling coupler in Couplers)
                coupler.Save(outf);
            base.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            Variable1 = inf.ReadSingle();
            Variable2 = inf.ReadSingle();
            Variable3 = inf.ReadSingle();
            Friction0N = inf.ReadSingle();
            DavisAN = inf.ReadSingle();
            DavisBNSpM = inf.ReadSingle();
            DavisCNSSpMM = inf.ReadSingle();
            int n = inf.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                Couplers.Add(new MSTSCoupling());
                Couplers[i].Restore(inf);
            }
            base.Restore(inf);
        }


        public override TrainCarViewer GetViewer(Viewer3D viewer)
        {
            // warning - don't assume there is only one viewer, or that there are any viewers at all.
            // Best practice is not to give the TrainCar class any knowledge of its viewers.
            return new MSTSWagonViewer(viewer, this);
        }

        public override void Update( float elapsedClockSeconds )
        {
            base.Update(elapsedClockSeconds);

            float s = Math.Abs(SpeedMpS);
            if (s < 0.1)
                FrictionForceN = Friction0N;
            else
                FrictionForceN = DavisAN + s * (DavisBNSpM + s * DavisCNSSpMM);

            MSTSBrakeSystem.Update(elapsedClockSeconds);
        }


        /// <summary>
        /// Used when someone want to notify us of an event
        /// </summary>
        public override void SignalEvent(EventID eventID)
        {
			// Modified according to replacable IDs - by GeorgeS
			//switch (eventID)
			do
			{
				if (eventID == EventID.PantographUp) { Pan = true; if (FrontPanUp == false && AftPanUp == false) AftPanUp = true; break; }  // pan up
				if (eventID == EventID.PantographDown) { Pan = false; FrontPanUp = AftPanUp = false;  break; } // pan down
				if (eventID == EventID.PantographToggle) {	
					Pan = !Pan;
					if (Pan && FrontPanUp == false && AftPanUp == false) AftPanUp = true;
					if (Pan == false) FrontPanUp = AftPanUp = false;
					break; 
				} // pan down
			} while (false);

            foreach (CarEventHandler eventHandler in EventHandlers)
                eventHandler.HandleCarEvent(eventID);
        }

        // sound sources or and viewers can register them selves to get direct notification of an event
        public List<CarEventHandler> EventHandlers = new List<CarEventHandler>();

        public MSTSCoupling Coupler
        {
            get
            {
                if (Couplers.Count == 0) return null;
                if (Flipped && Couplers.Count > 1) return Couplers[1];
                return Couplers[0];
            }
        }
        public override float GetCouplerZeroLengthM()
        {
            return Coupler != null ? Coupler.R0 : base.GetCouplerZeroLengthM();
        }

        public override float GetCouplerStiffnessNpM()
        {
            return Coupler != null && Coupler.R0 == 0 ? 7 * (Coupler.Stiffness1NpM + Coupler.Stiffness2NpM) : base.GetCouplerStiffnessNpM();
        }

        public override float GetMaximumCouplerSlack1M()
        {
            if (Coupler == null)
                return base.GetMaximumCouplerSlack1M();
            return Coupler.Rigid ? 0.0001f : Coupler.R0Diff;
        }

        public override float GetMaximumCouplerSlack2M()
        {
            if (Coupler == null)
                return base.GetMaximumCouplerSlack2M();
            return Coupler.Rigid ? 0.0002f : base.GetMaximumCouplerSlack2M();
        }
    }

    public class MSTSCoupling
    {
        public bool Rigid = false;
        public float R0 = 0;
        public float R0Diff = .012f;
        public float Stiffness1NpM = 1e7f;
        public float Stiffness2NpM = 2e7f;
        public MSTSCoupling()
        {
        }
        public MSTSCoupling(MSTSCoupling copy)
        {
            Rigid = copy.Rigid;
            R0 = copy.R0;
            R0Diff = copy.R0Diff;
        }
        public void SetR0(float a, float b)
        {
            R0 = a;
            if (a == 0)
                R0Diff = b / 2 * Stiffness2NpM / (Stiffness1NpM + Stiffness2NpM);
            else
                R0Diff = .012f;
            if (R0Diff < .001)
                R0Diff = .001f;
            else if (R0Diff > .1)
                R0Diff = .1f;
            //Console.WriteLine("setR0 {0} {1} {2} {3} {4} {5}", a, b, R0, R0Diff, Stiffness1NpM, Stiffness2NpM);
        }
        public void SetStiffness(float a, float b)
        {
            if (a + b < 0)
                return;
            Stiffness1NpM = a;
            Stiffness2NpM = b;
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public void Save(BinaryWriter outf)
        {
            outf.Write(Rigid);
            outf.Write(R0);
            outf.Write(R0Diff);
            outf.Write(Stiffness1NpM);
            outf.Write(Stiffness2NpM);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public void Restore(BinaryReader inf)
        {
            Rigid = inf.ReadBoolean();
            R0 = inf.ReadSingle();
            R0Diff = inf.ReadSingle();
            Stiffness1NpM = inf.ReadSingle();
            Stiffness2NpM = inf.ReadSingle();
        }
    }

    // This supports animation of Pantographs, Mirrors and Doors - any up/down on/off 2 state types
    // It is initialized with a list of indexes for the matrices related to this part
    // On Update( position ) it slowly moves the parts towards the specified position
    public class AnimatedPart
    {
        /// <summary>
        /// Construct with a link to the shape that contains the animated parts 
        /// </summary>
        public AnimatedPart( PoseableShape poseableShape )
        {
            PoseableShape = poseableShape;
        }

        /// <summary>
        /// All the matrices associated with this part are added during initialization by the MSTSWagon constructor
        /// </summary>
        public void MatrixIndexAdd( int i )
        {
            MatrixIndexes.Add( i );
            if( FrameCount == 0 ) // only do this once for each AnimatedPart
            {
                // determine the number of frames in this animation from the animation controller for first matrix component
                SharedShape shape = PoseableShape.SharedShape;
                if( shape.Animations != null )
                {
                    // find the controller set for this part, ie anim_node WIPERBLADERIGHT1 ( controllers ( 2
                    controllers controllers = shape.Animations[0].anim_nodes[i].controllers;
                    if( controllers.Count > 0 )
                    {
                        controller controller = controllers[0];  // ie tcb_rot ( 3 is a controller with three keypositions
                                                                // we want the frame number of the last key position
                        FrameCount = controller[controller.Count-1].Frame;
                    }
                }
            }
        }

        /// <summary>
        /// Ensure the shape file contained parts of this type 
        /// and those parts have an animation section.
        /// </summary>
        public bool Exists()
        {
            return MatrixIndexes.Count != 0 && FrameCount != 0;
        }

        /// <summary>
        /// Disable animation for this part by clearing the matrix and animation data.
        /// </summary>
        public void MakeEmpty()
        {
            MatrixIndexes.Clear();
            FrameCount = 0;
            AnimationKey = 0;
        }

        /// <summary>
        /// Bypass the normal slow transition and jump the part immediately to this new state
        /// </summary>
        public void SetPosition(bool newState)
        {
            AnimationKey = newState ? FrameCount : 0;
			foreach (int iMatrix in MatrixIndexes)
				PoseableShape.AnimateMatrix(iMatrix, AnimationKey);
        }


        /// <summary>
        /// Transition the part toward the specified state. 
        /// </summary>
        public void Update( bool state, ElapsedTime elapsedTime)
        {
            if (MatrixIndexes.Count == 0) return;

            if (state)  // panto up/door open, etc.
            {
                if (AnimationKey < FrameCount)  // skip this if we are already up
                {                               // otherwise transition up
                    // Animation speed is hard coded at 2 frames per second
                    AnimationKey += 2f * elapsedTime.ClockSeconds;
                    if (AnimationKey > FrameCount) AnimationKey = FrameCount;
                    foreach (int iMatrix in MatrixIndexes)
                        PoseableShape.AnimateMatrix(iMatrix, AnimationKey);
                }
            }
            else  // down, closed etc
            {
                if (AnimationKey > 0)   // if we are already down, don't do anything
                {                       // otherwise transition down
                    AnimationKey -= 2f * elapsedTime.ClockSeconds;
                    if (AnimationKey < 0) AnimationKey = 0;
                    foreach (int iMatrix in MatrixIndexes)
                        PoseableShape.AnimateMatrix(iMatrix, AnimationKey);
                }
            }
        }

        private float AnimationKey = 0;  // This is where we are in the timeline. 
                                         // The timeline is measured in frames 
                                         // It runs from 0 to the number of frames provided in the animation sequence
        
        private List<int>MatrixIndexes = new List<int>();   // the matrices are associated with this animated part

        private PoseableShape PoseableShape;    // the animated part is contained in this shape file

        private int FrameCount = 0;             // the shape file contains this many frames of animation for this part

        /// <summary>
        /// Swap the pointers around.
        /// </summary>
        public static void Swap(ref AnimatedPart a, ref AnimatedPart b)
        {
            AnimatedPart temp = a;
            a = b;
            b = temp;
        }
    }



    ///////////////////////////////////////////////////
    ///   3D VIEW
    ///////////////////////////////////////////////////

    /// <summary>
    /// Note:  we need a separate viewer class since there could be multiple viewers
    /// for a single traincar, or possibly none
    /// </summary>

    public class MSTSWagonViewer: TrainCarViewer
    {
        protected float WheelRotationR = 0f;  // radians track rolling of wheels
        float DriverRotationKey;  // advances animation with the driver rotation


        protected PoseableShape TrainCarShape = null;
        protected AnimatedShape FreightShape = null;
        protected AnimatedShape InteriorShape = null;
        protected List<SoundSource> SoundSources = new List<SoundSource>();

        List<int> WheelPartIndexes = new List<int>();   // these index into a matrix in the shape file
		List<int> RunningGearPartIndexes = new List<int>();

		AnimatedPart AftPantograph;  // matrixes for the Panto***2* parts
		AnimatedPart FrontPantograph;  // matrixes for the Panto***1* parts
		AnimatedPart LeftDoor; //left door
		AnimatedPart RightDoor;//right door
		AnimatedPart Mirrors; //mirror

        protected MSTSWagon MSTSWagon { get { return (MSTSWagon) Car; } }

        Viewer3D _Viewer3D;

        public MSTSWagonViewer(Viewer3D viewer, MSTSWagon car): base( viewer, car )
        {
            _Viewer3D = viewer;
            string wagonFolderSlash = Path.GetDirectoryName(car.WagFilePath) + @"\";
            string shapePath = wagonFolderSlash + car.MainShapeFileName;
			
            TrainCarShape = new PoseableShape(viewer, shapePath, car.WorldPosition, ShapeFlags.ShadowCaster);

            if (car.FreightShapeFileName != null)
            {
                FreightShape = new AnimatedShape(viewer, wagonFolderSlash + car.FreightShapeFileName, car.WorldPosition, ShapeFlags.ShadowCaster);
            }
            if (car.InteriorShapeFileName != null)
            {
                InteriorShape = new AnimatedShape(viewer, wagonFolderSlash + car.InteriorShapeFileName, car.WorldPosition);
            }

            AftPantograph = new AnimatedPart( TrainCarShape);  // matrixes for the Panto***2* parts
		    FrontPantograph = new AnimatedPart(TrainCarShape);  // matrixes for the Panto***1* parts
		    LeftDoor = new AnimatedPart(TrainCarShape); //left door
		    RightDoor = new AnimatedPart(TrainCarShape);//right door
		    Mirrors = new AnimatedPart(TrainCarShape); //mirror

            LoadCarSounds(wagonFolderSlash);
            LoadTrackSounds();

            // Adding all loaded SoundSource to the main sound update thread
            _Viewer3D.SoundProcess.AddSoundSource(this, SoundSources);

            // Get indexes of all the animated parts
			for (int iMatrix = 0; iMatrix < TrainCarShape.SharedShape.MatrixNames.Count; ++iMatrix)
            {
                string matrixName = TrainCarShape.SharedShape.MatrixNames[iMatrix].ToUpper();
                if (matrixName.StartsWith("WHEELS"))
                {
                    if (matrixName.Length == 7)
                    {
                        if (TrainCarShape.SharedShape.Animations != null
                                   && TrainCarShape.SharedShape.Animations[0].FrameCount > 0
                                   && TrainCarShape.SharedShape.Animations[0].anim_nodes[iMatrix].controllers.Count > 0)  // ensure shape file is setup properly
                            RunningGearPartIndexes.Add(iMatrix);
                        Matrix m = TrainCarShape.SharedShape.GetMatrixProduct(iMatrix);
                        int pmatrix = TrainCarShape.SharedShape.GetParentMatrix(iMatrix);
                        car.AddWheelSet(m.M43, 0, pmatrix);
                    }
                    else if (matrixName.Length == 8)
                    {
                        WheelPartIndexes.Add(iMatrix);
                        try
                        {
                            int id = Int32.Parse(matrixName.Substring(6, 1));
                            Matrix m = TrainCarShape.SharedShape.GetMatrixProduct(iMatrix);
                            int pmatrix = TrainCarShape.SharedShape.GetParentMatrix(iMatrix);
                            car.AddWheelSet(m.M43, id, pmatrix);
                        }
                        catch
                        {
                        }
                    }
                }
                else if (matrixName.StartsWith("BOGIE") && matrixName.Length == 6)
                {
                    try
                    {
                        int id = Int32.Parse(matrixName.Substring(5, 1));
                        Matrix m = TrainCarShape.SharedShape.GetMatrixProduct(iMatrix);
                        car.AddBogie(m.M43, iMatrix, id);
                    }
                    catch
                    {
                    }
                }
				else if (matrixName.StartsWith("WIPER")) // wipers
				{//will be captured later by MSTSLocomotive
				}
				else if (matrixName.StartsWith("DOOR") || matrixName.StartsWith("DEUR")) // doors (left / right)
				{
					if (TrainCarShape.SharedShape.Animations != null
							   && TrainCarShape.SharedShape.Animations[0].FrameCount > 0
							   && TrainCarShape.SharedShape.Animations[0].anim_nodes[iMatrix].controllers.Count > 0)
					{
						if (matrixName.StartsWith("DOOR_D") || matrixName.StartsWith("DOOR_E") || matrixName.StartsWith("DOOR_F")) LeftDoor.MatrixIndexAdd(iMatrix);
						else if (matrixName.StartsWith("DOOR_A") || matrixName.StartsWith("DOOR_B") || matrixName.StartsWith("DOOR_C")) RightDoor.MatrixIndexAdd(iMatrix);
						else LeftDoor.MatrixIndexAdd(iMatrix); //some train may not follow the above convention of left/right, put them as left by default
					}
				}
				else if (matrixName.StartsWith("PANTOGRAPH")) //pantographs (1/2)
				{
					if (TrainCarShape.SharedShape.Animations == null) continue;
					switch (matrixName)
					{
						case "PANTOGRAPHBOTTOM1":
						case "PANTOGRAPHBOTTOM1A":
						case "PANTOGRAPHBOTTOM1B":
						case "PANTOGRAPHMIDDLE1":
						case "PANTOGRAPHMIDDLE1A":
						case "PANTOGRAPHMIDDLE1B":
						case "PANTOGRAPHTOP1":
						case "PANTOGRAPHTOP1A":
						case "PANTOGRAPHTOP1B":
							FrontPantograph.MatrixIndexAdd(iMatrix);
							break;
						case "PANTOGRAPHBOTTOM2":
						case "PANTOGRAPHBOTTOM2A":
						case "PANTOGRAPHBOTTOM2B":
						case "PANTOGRAPHMIDDLE2":
						case "PANTOGRAPHMIDDLE2A":
						case "PANTOGRAPHMIDDLE2B":
						case "PANTOGRAPHTOP2":
						case "PANTOGRAPHTOP2A":
						case "PANTOGRAPHTOP2B":
							AftPantograph.MatrixIndexAdd(iMatrix);
							break;
					}
				}
                else if (matrixName.StartsWith("MIRROR")) // mirrors
                {
                    if (TrainCarShape.SharedShape.Animations != null
                               && TrainCarShape.SharedShape.Animations[0].FrameCount > 0
                               && TrainCarShape.SharedShape.Animations[0].anim_nodes[iMatrix].controllers.Count > 0)
                    {
                        Mirrors.MatrixIndexAdd(iMatrix);
                    }
                }
                else if (matrixName.StartsWith("PANTO"))  // TODO, not sure why this is needed, see above!
                {
                    if (TrainCarShape.SharedShape.Animations == null) continue;
                    if (matrixName.Contains("1"))
                    {
                        FrontPantograph.MatrixIndexAdd(iMatrix);
                    }
                    else if (matrixName.Contains("2"))
                    {
                        AftPantograph.MatrixIndexAdd(iMatrix);
                    }
                }
                else
                {
                    if (TrainCarShape.SharedShape.Animations != null
                        && TrainCarShape.SharedShape.Animations[0].FrameCount > 0
                        && TrainCarShape.SharedShape.Animations[0].anim_nodes[iMatrix].controllers.Count > 0)  // ensure shape file is setup properly
                        RunningGearPartIndexes.Add(iMatrix);
                }
            }

            car.SetupWheels();

			//determine how many panto
			if (FrontPantograph.Exists()) car.NumPantograph++;
			if (AftPantograph.Exists() ) car.NumPantograph++;

			//we always want to raise aft by default, so rename panto1 to aft if there is only one set of pant
			if (car.NumPantograph == 1 && !AftPantograph.Exists() )
			{
                AnimatedPart.Swap( ref AftPantograph, ref FrontPantograph);
			}

			//now handle the direction of the car; if reverse, then the pantoaft should use Panto***1*
			if (car.Direction == Direction.Reverse && car.NumPantograph == 2)
			{
                AnimatedPart.Swap( ref AftPantograph, ref FrontPantograph);
			}

			//now handle the direction of the car; if reverse, then the left/right door should be switched
			if (car.Direction == Direction.Reverse )
			{
                AnimatedPart.Swap(ref RightDoor, ref  LeftDoor);
			}

            AftPantograph.SetPosition(MSTSWagon.AftPanUp);
            FrontPantograph.SetPosition(MSTSWagon.FrontPanUp);
            LeftDoor.SetPosition(MSTSWagon.DoorLeftOpen);
            RightDoor.SetPosition(MSTSWagon.DoorRightOpen);
            Mirrors.SetPosition(MSTSWagon.MirrorOpen);


        }

        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
			// Pantograph
			if (UserInput.IsPressed(UserCommands.ControlPantographSecond))
			{
				MSTSWagon.FrontPanUp = !MSTSWagon.FrontPanUp;
				if (Viewer.Simulator.PlayerLocomotive == this.Car) //inform everyone else in the train
					foreach (TrainCar car in Car.Train.Cars)
						if (car != this.Car && car is MSTSWagon) ((MSTSWagon)car).FrontPanUp = MSTSWagon.FrontPanUp;
				if (MSTSWagon.FrontPanUp || MSTSWagon.AftPanUp) Car.SignalEvent(EventID.PantographUp);
				else Car.SignalEvent(EventID.PantographDown);
			}
			if (UserInput.IsPressed(UserCommands.ControlPantographFirst))
			{
				MSTSWagon.AftPanUp = !MSTSWagon.AftPanUp;
				if (Viewer.Simulator.PlayerLocomotive == this.Car)//inform everyone else in the train
					foreach (TrainCar car in Car.Train.Cars)
						if (car != this.Car && car is MSTSWagon) ((MSTSWagon)car).AftPanUp = MSTSWagon.AftPanUp;
				if (MSTSWagon.FrontPanUp || MSTSWagon.AftPanUp) Car.SignalEvent(EventID.PantographUp);
				else Car.SignalEvent(EventID.PantographDown);
			}
			if (UserInput.IsPressed(UserCommands.ControlDoorLeft)) //control door (or only left)
			{
				MSTSWagon.DoorLeftOpen = !MSTSWagon.DoorLeftOpen;
				if (Viewer.Simulator.PlayerLocomotive == this.Car)//inform everyone else in the train
					foreach (TrainCar car in Car.Train.Cars)
						if (car != this.Car && car is MSTSWagon) ((MSTSWagon)car).DoorLeftOpen = MSTSWagon.DoorLeftOpen;
				/*if (MSTSWagon.DoorLeftOpen) Car.SignalEvent(EventID.DoorOpen);
				else Car.SignalEvent(EventID.DoorClose);*/
				//comment out, but can be added back to animate sound
			}
			if (UserInput.IsPressed(UserCommands.ControlDoorRight)) //control right door
			{
				MSTSWagon.DoorRightOpen = !MSTSWagon.DoorRightOpen;
				if (Viewer.Simulator.PlayerLocomotive == this.Car)//inform everyone else in the train
					foreach (TrainCar car in Car.Train.Cars)
						if (car != this.Car && car is MSTSWagon) ((MSTSWagon)car).DoorRightOpen = MSTSWagon.DoorRightOpen;
				/*if (MSTSWagon.DoorLeftOpen) Car.SignalEvent(EventID.DoorOpen);
				else Car.SignalEvent(EventID.DoorClose);*/
				//comment out, but can be added back to animate sound
			}
			if (UserInput.IsPressed(UserCommands.ControlMirror)) //control right door
			{
				MSTSWagon.MirrorOpen = !MSTSWagon.MirrorOpen;
			}
		}


        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            AftPantograph.Update( MSTSWagon.AftPanUp, elapsedTime);
            FrontPantograph.Update( MSTSWagon.FrontPanUp, elapsedTime);
            LeftDoor.Update( MSTSWagon.DoorLeftOpen, elapsedTime);
            RightDoor.Update( MSTSWagon.DoorRightOpen, elapsedTime);
            Mirrors.Update( MSTSWagon.MirrorOpen, elapsedTime);
			UpdateAnimation(frame, elapsedTime);
        }


        public void UpdateSound(ElapsedTime elapsedTime)
        {
			try
			{
				foreach (SoundSource soundSource in SoundSources)
					soundSource.Update();
			}
			catch (Exception error)
			{
				Trace.WriteLine(error);
			}
        }


        private void UpdateAnimation( RenderFrame frame, ElapsedTime elapsedTime )
        {
            float distanceTravelledM = 0;
            if ((MSTSWagon.IsDriveable)&&(MSTSWagon.Simulator.UseAdvancedAdhesion))
            {
                distanceTravelledM = MSTSWagon.WheelSpeedMpS * elapsedTime.ClockSeconds;
            }
            else
            {
                distanceTravelledM = MSTSWagon.SpeedMpS * elapsedTime.ClockSeconds;
            }

            // Running gear animation
            if (RunningGearPartIndexes.Count > 0 && MSTSWagon.DriverWheelRadiusM > 0.001 )  // skip this if there is no running gear and only engines can have running gear
            {
                float driverWheelCircumferenceM = 3.14159f * 2.0f * MSTSWagon.DriverWheelRadiusM;
                //float framesAdvanced = (float)TrainCarShape.SharedShape.Animations[0].FrameCount * distanceTravelledM / driverWheelCircumferenceM;
                float framesAdvanced = (float)TrainCarShape.SharedShape.Animations[0].FrameRate * 8/30f * distanceTravelledM / driverWheelCircumferenceM;
                DriverRotationKey += framesAdvanced;  // ie, with 8 frames of animation, the key will advance from 0 to 8 at the specified speed.
                while (DriverRotationKey >= TrainCarShape.SharedShape.Animations[0].FrameCount) DriverRotationKey -= TrainCarShape.SharedShape.Animations[0].FrameCount;
                while (DriverRotationKey < -0.00001) DriverRotationKey += TrainCarShape.SharedShape.Animations[0].FrameCount;
                foreach (int iMatrix in RunningGearPartIndexes)
                    TrainCarShape.AnimateMatrix(iMatrix, DriverRotationKey);
            }

            // Wheel animation
            if (WheelPartIndexes.Count > 0)
            {
                float wheelCircumferenceM = 3.14159f * 2.0f * MSTSWagon.WheelRadiusM;
                float rotationalDistanceR = 3.14159f * 2.0f * distanceTravelledM / wheelCircumferenceM;  // in radians
                WheelRotationR -= rotationalDistanceR;
                while (WheelRotationR > Math.PI) WheelRotationR -= (float)Math.PI * 2;   // normalize for -180 to +180 degrees
                while (WheelRotationR < -Math.PI) WheelRotationR += (float)Math.PI * 2;
                Matrix wheelRotationMatrix = Matrix.CreateRotationX(WheelRotationR);
                foreach (int iMatrix in WheelPartIndexes)
                    TrainCarShape.XNAMatrices[iMatrix] = wheelRotationMatrix * TrainCarShape.SharedShape.Matrices[iMatrix];
            }

            // truck angle animation
            foreach (var p in Car.Parts)
            {
                if (p.iMatrix <= 0)
                    continue;
                Matrix m = Matrix.Identity;
                m.Translation= TrainCarShape.SharedShape.Matrices[p.iMatrix].Translation;
                m.M11 = p.Cos;
                m.M13 = p.Sin;
                m.M31 = -p.Sin;
                m.M33 = p.Cos;
                TrainCarShape.XNAMatrices[p.iMatrix] = m;
            }

            if (FreightShape != null)
            {
                FreightShape.XNAMatrices[0].M42 = MSTSWagon.FreightAnimHeight;
                FreightShape.PrepareFrame(frame, elapsedTime);
            }

            // Control visibility of passenger cabin when inside it
            if (Viewer.Camera.AttachedCar == this.MSTSWagon
                 && //( Viewer.ViewPoint == Viewer.ViewPoints.Cab ||  // TODO, restore when we complete cab views - 
                     Viewer.Camera.Style == Camera.Styles.Passenger)
            {
                // We are in the passenger cabin
                if (InteriorShape != null)
                    InteriorShape.PrepareFrame(frame, elapsedTime);
                else
                    TrainCarShape.PrepareFrame(frame, elapsedTime);
            }
            else
            {
                // Skip drawing if CAB view - draw 2D view instead - by GeorgeS
                if (Viewer.Camera.AttachedCar == this.MSTSWagon &&
                    Viewer.Camera.Style == Camera.Styles.Cab)
                    return;
                
                // We are outside the passenger cabin
                TrainCarShape.PrepareFrame(frame, elapsedTime);
            }

        }



        /// <summary>
        /// Unload and release the car - its not longer being displayed
        /// </summary>
        public virtual void Unload()
        {
            // Removing sound sources from sound update thread
            _Viewer3D.SoundProcess.RemoveSoundSource(this);
            SoundSources.Clear();
        }


        /// <summary>
        /// Load the various car sounds
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        private void LoadCarSounds(string wagonFolderSlash)
        {
            if( MSTSWagon.MainSoundFileName != null ) LoadCarSound(wagonFolderSlash, MSTSWagon.MainSoundFileName );
            if (MSTSWagon.InteriorSoundFileName != null) LoadCarSound(wagonFolderSlash, MSTSWagon.InteriorSoundFileName);
        }


        /// <summary>
        /// Load the car sound, attach it to the car
        /// check first in the wagon folder, then the global folder for the sound.
        /// If not found, report a warning.
        /// </summary>
        /// <param name="wagonFolderSlash"></param>
        /// <param name="filename"></param>
        protected void LoadCarSound(string wagonFolderSlash, string filename)
        {
            if (filename == null)
                return;
            string smsFilePath = wagonFolderSlash + @"sound\" + filename;
            if (!File.Exists(smsFilePath))
                smsFilePath = Viewer.Simulator.BasePath + @"\sound\" + filename;
            if (!File.Exists(smsFilePath))
            {
                Trace.TraceWarning("Cannot find {1} car sound file {0}", filename, wagonFolderSlash);
                return;
            }

            try
            {
                SoundSources.Add(new SoundSource(Viewer, MSTSWagon, smsFilePath));
            }
            catch (Exception error)
            {
                Trace.WriteLine(error);
            }
        }

        /// <summary>
        /// Load the inside and outside sounds for the default level 0 track type.
        /// </summary>
        private void LoadTrackSounds()
        {
            if (Viewer.TTypeDatFile.Count > 0)  // TODO, still have to figure out if this should be part of the car, or train, or track
            {
                if (!string.IsNullOrEmpty(MSTSWagon.InteriorSoundFileName))
                    LoadTrackSound(Viewer.TTypeDatFile[0].InsideSound);

                LoadTrackSound(Viewer.TTypeDatFile[0].OutsideSound);
            }
        }

        /// <summary>
        /// Load the sound source, attach it to the car.
        /// Check first in route\SOUND folder, then in base\SOUND folder.
        /// </summary>
        /// <param name="filename"></param>
        private void LoadTrackSound(string filename)
        {
            if (filename == null)
                return;
            string path = Viewer.Simulator.RoutePath + @"\SOUND\" + filename;
            if (!File.Exists(path))
                path = Viewer.Simulator.BasePath + @"\SOUND\" + filename;
            if (!File.Exists(path))
            {
                Trace.TraceWarning("Cannot find track sound file {0}", filename);
                return;
            }
            SoundSources.Add(new SoundSource(Viewer, MSTSWagon, path));
        }

    } // class carshape


    /// <summary>
    /// Utility class to avoid loading the wag file multiple times
    /// </summary>
    public class CarManager
    {
        public static Dictionary<string, MSTSWagon> LoadedCars = new Dictionary<string, MSTSWagon>();
    }


} // namespace ORTS
