// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

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

//#define ALLOW_ORTS_SPECIFIC_ENG_PARAMETERS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using MSTS.Formats;
using MSTS.Parsers;
using ORTS.Common;
using ORTS.Viewer3D;

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
        public bool Pan;     // false = down; some wagon has pantograph
        public bool Pan1Up;  // if the forwards pantograph is up
        public bool Pan2Up;  // if the backwards pantograph is up
        public bool DoorLeftOpen;
        public bool DoorRightOpen;
        public bool MirrorOpen;
        public bool IsRollerBearing; // Has roller bearings
        public bool IsStandStill = true;  // Used for MSTS type friction
        public bool IsDavisFriction = true; // Default to new Davis type friction
        public bool IsLowSpeed = true; // set indicator for low speed operation  0 - 5mph

        // simulation parameters
        public float Variable1;  // used to convey status to soundsource
        public float Variable2;
        public float Variable3;
        float s;    // speed variable

        // wag file data
        public string MainShapeFileName;
        public string FreightShapeFileName;
        public float FreightAnimMaxLevelM;
        public float FreightAnimMinLevelM;
        public string InteriorShapeFileName; // passenger view shape file name
        public string MainSoundFileName;
        public string InteriorSoundFileName;
        public float WheelRadiusM = 1;          // provide some defaults in case it's missing from the wag
        public float DriverWheelRadiusM = 1.5f;    // provide some defaults in case i'ts missing from the wag
        float StaticFrictionFactorLb;    // factor to multiply friction by to determine static or starting friction - will vary depending upon whether roller or friction bearing
        float RollerBearingCutoutSpeedMpS; // Speed at which the roller bearing will no longer have a reduced influence on friction
        public float Friction0N;        // static friction
        float Friction5N;               // Friction at 5mph
        float DavisANnew;               // Temporary value of Davis A for roller bearing calculations
        float RollerBearingFrictionN; // Temp value for calculating roller bearing friction
        public float DavisAN;           // davis equation constant
        public float DavisBNSpM;        // davis equation constant for speed
        public float DavisCNSSpMM;      // davis equation constant for speed squared
        public float SteamLocoMechFrictN; // Steam locomotive mechanical friction
        public List<MSTSCoupling> Couplers = new List<MSTSCoupling>();
        public float Adhesion1 = .27f;   // 1st MSTS adhesion value
        public float Adhesion2 = .49f;   // 2nd MSTS adhesion value
        public float Adhesion3 = 2;   // 3rd MSTS adhesion value
        public float Curtius_KnifflerA = 7.5f;               //Curtius-Kniffler constants                   A
        public float Curtius_KnifflerB = 44.0f;              // (adhesion coeficient)       umax = ---------------------  + C
        public float Curtius_KnifflerC = 0.161f;             //                                      speedMpS * 3.6 + B
        public float AdhesionK = 0.7f;   //slip characteristics slope
        //public AntislipControl AntislipControl = AntislipControl.None;
        public float AxleInertiaKgm2;    //axle inertia
        public float WheelSpeedMpS;
        public float SlipWarningThresholdPercent = 70;
        public float NumWheelsBrakingFactor = 4;   // MSTS braking factor loosely based on the number of braked wheels. Not used yet.
        float CentreOfGravityM; // Lateral Centre of gravity
        float Gauge1M; // temporary variable for the track gauge
        float Gauge2M; // temporary variable for the track gauge
        float TrackGaugeM = 1435.0f; // Gauge of track
        float XCoGM; // Centre of Gravity - X value
        float YCoGM; // Centre of Gravity - Y value
        float ZCoGM; // Centre of Gravity - Z value
        float UnbalancedSuperElevationM; // Unbalanced Superelevation
        float RigidWheelBaseM;
        float WagonNumWheels;
        float WheelBase1M;
        float WheelBase2M;
        public bool IsPassenger;
        public bool IsEngine;
        string WagonType;

        /// <summary>
        /// Attached steam locomotive in case this wagon is a tender
        /// </summary>
        public MSTSSteamLocomotive TendersSteamLocomotive { get; private set; }
        
        public List<IntakePoint> IntakePointList = new List<IntakePoint>();

        public MSTSBrakeSystem MSTSBrakeSystem { get { return (MSTSBrakeSystem)base.BrakeSystem; } }

        // Get steam locomotive friction from steam folder
        protected virtual float GetSteamLocoMechFrictN()
        {
            return 0f;
        }

        public MSTSWagon(Simulator simulator, string wagFilePath)
            : base(simulator, wagFilePath)
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
                    stf.ReadItem();
                    Parse(stf.Tree.ToLower(), stf);
                }
             if (BrakeSystem == null)
                    BrakeSystem = new AirSinglePipe(this);
             Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        string brakeSystemType;

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(wagonshape": MainShapeFileName = stf.ReadStringBlock(null); break;
		        case "wagon(type":
		            stf.MustMatch("(");
		            string typeString = stf.ReadString();
		            IsFreight = String.Compare(typeString,"Freight") == 0 ? true : false;
		            IsTender = String.Compare(typeString,"Tender") == 0 ? true : false;
                    IsPassenger = String.Compare(typeString, "Carriage") == 0 ? true : false;
                    IsEngine = String.Compare(typeString, "Engine") == 0 ? true : false;
                    break;
                case "wagon(freightanim":
                    stf.MustMatch("(");
                    FreightShapeFileName = stf.ReadString();
                    FreightAnimMaxLevelM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FreightAnimMinLevelM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(size":
                    stf.MustMatch("(");
                    stf.ReadFloat(STFReader.UNITS.Distance, null);
                    HeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    LengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortstrackgauge":
                    stf.MustMatch("(");
                    Gauge1M = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    Gauge2M = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(centreofgravity":
                    stf.MustMatch("(");
                    XCoGM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    YCoGM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    ZCoGM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsunbalancedsuperelevation": UnbalancedSuperElevationM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsrigidwheelbase":  
                    stf.MustMatch("(");
                    WheelBase1M = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    WheelBase2M = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(mass": MassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); if (MassKG < 0.1f) MassKG = 0.1f; break;
                case "wagon(wheelradius": WheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(wheelradius": DriverWheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(sound": MainSoundFileName = stf.ReadStringBlock(null); break;
                case "wagon(ortsdavis_a": DavisAN = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsdavis_b": DavisBNSpM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsdavis_c": DavisCNSSpMM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsbearingtype":
		            stf.MustMatch("(");
		            string typeString2 = stf.ReadString();
		            IsRollerBearing = String.Compare(typeString2,"Roller") == 0 ? true : false;
		            break;
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
                case "wagon(coupling(spring(break":
                    stf.MustMatch("(");
                    Couplers[Couplers.Count - 1].SetBreak(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(r0":
                    stf.MustMatch("(");
                    Couplers[Couplers.Count - 1].SetR0(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion":
                case "wagon(adheasion":
                    stf.MustMatch("(");
                    Adhesion1 = stf.ReadFloat(STFReader.UNITS.None, null);
                    Adhesion2 = stf.ReadFloat(STFReader.UNITS.None, null);
                    Adhesion3 = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(ortscurtius_kniffler":   
                    stf.MustMatch("(");                      //e.g. Wagon ( OR_adhesion ( Curtius_Kniffler ( 7.5 44 0.161 0.7 ) ) )
                    Curtius_KnifflerA = stf.ReadFloat(STFReader.UNITS.None, 7.5f); if (Curtius_KnifflerA <= 0) Curtius_KnifflerA = 7.5f;
                    Curtius_KnifflerB = stf.ReadFloat(STFReader.UNITS.None, 44.0f); if (Curtius_KnifflerB <= 0) Curtius_KnifflerB = 44.0f;
                    Curtius_KnifflerC = stf.ReadFloat(STFReader.UNITS.None, 0.161f); if (Curtius_KnifflerC <= 0) Curtius_KnifflerA = 0.161f;
                    AdhesionK = stf.ReadFloat(STFReader.UNITS.None, 0.7f); if (AdhesionK <= 0) AdhesionK = 0.7f;
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(ortsslipwarningthreshold":
                    stf.MustMatch("(");
                    SlipWarningThresholdPercent = stf.ReadFloat(STFReader.UNITS.None, 70.0f); if (SlipWarningThresholdPercent <= 0) SlipWarningThresholdPercent = 70.0f;
                    stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(ortsantislip":
                    stf.MustMatch("(");
                    //AntislipControl = stf.ReadStringBlock(null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(wheelset(axle(ortsinertia":
                    stf.MustMatch("(");                    
                    AxleInertiaKgm2 = stf.ReadFloat(STFReader.UNITS.RotationalInertia, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(wheelset(axle(ortsradius":
                    stf.MustMatch("(");
                    // <CJComment> Shouldn't this be "WheelRadiusM = " ? </CJComment>
                    AxleInertiaKgm2 = stf.ReadFloatBlock(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(lights":
                    Lights = new LightCollection(stf);
                    break;
                case "wagon(inside": ParseWagonInside(stf); break;
                case "wagon(numwheels": NumWheelsBrakingFactor = stf.ReadFloatBlock(STFReader.UNITS.None, 4.0f); break;
                case "wagon(intakepoint": IntakePointList.Add(new IntakePoint(stf)); break;
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
            FreightAnimMaxLevelM = copy.FreightAnimMaxLevelM;
            FreightAnimMinLevelM = copy.FreightAnimMinLevelM;
            IsFreight = copy.IsFreight;
            InteriorShapeFileName = copy.InteriorShapeFileName;
            MainSoundFileName = copy.MainSoundFileName;
            InteriorSoundFileName = copy.InteriorSoundFileName;
            WheelRadiusM = copy.WheelRadiusM;
            DriverWheelRadiusM = copy.DriverWheelRadiusM;
            Friction0N = copy.Friction0N;
            DavisAN = copy.DavisAN;
            DavisBNSpM = copy.DavisBNSpM;
            DavisCNSSpMM = copy.DavisCNSSpMM;
            IsRollerBearing = copy.IsRollerBearing;
            LengthM = copy.LengthM;
            HeightM = copy.HeightM;
            TrackGaugeM = copy.TrackGaugeM;
            XCoGM = copy.XCoGM;
            YCoGM = copy.YCoGM;
            ZCoGM = copy.ZCoGM;
            UnbalancedSuperElevationM = copy.UnbalancedSuperElevationM;
            WheelBase1M = copy.WheelBase1M;
            WheelBase2M = copy.WheelBase2M;
            MassKG = copy.MassKG;
            Adhesion1 = copy.Adhesion1;
            Adhesion2 = copy.Adhesion2;
            Adhesion3 = copy.Adhesion3;
            Curtius_KnifflerA = copy.Curtius_KnifflerA;
            Curtius_KnifflerB = copy.Curtius_KnifflerB;
            Curtius_KnifflerC = copy.Curtius_KnifflerC;
            AdhesionK = copy.AdhesionK;
            AxleInertiaKgm2 = copy.AxleInertiaKgm2;
            SlipWarningThresholdPercent = copy.SlipWarningThresholdPercent;
            Lights = copy.Lights;
            foreach (ViewPoint passengerViewPoint in copy.PassengerViewpoints)
                PassengerViewpoints.Add(passengerViewPoint);
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
            if (DavisAN == 0 || DavisBNSpM == 0 || DavisCNSSpMM == 0) // If Davis parameters are not defined in WAG file, then use default methods
                IsDavisFriction = false; // set to false - indicating that "new" Davis friction is not used
            {
                stf.MustMatch("(");
                float c1 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
                float e1 = stf.ReadFloat(STFReader.UNITS.None, null);
                float v2 = stf.ReadFloat(STFReader.UNITS.Speed, null);
                float c2 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
                float e2 = stf.ReadFloat(STFReader.UNITS.None, null);
                stf.SkipRestOfBlock();
                if (v2 < 0 || v2 > 4.4407f) // > 10 mph
                {   // not fcalc ignore friction and use default davis equation
                    // Starting Friction 
                    //
                    //                      Above Freezing   Below Freezing
                    //    Journal Bearing      25 lb/ton        35 lb/ton   (short ton)
                    //     Roller Bearing       5 lb/ton        15 lb/ton
                    //
                    // [2009-10-25 from http://www.arema.org/publications/pgre/ ]
                    //Friction0N = MassKG * 30f /* lb/ton */ * 4.84e-3f;  // convert lbs/short-ton to N/kg 
                    DavisAN = 6.3743f * MassKG / 1000 + 128.998f * 4;
                    DavisBNSpM = .49358f * MassKG / 1000;
                    DavisCNSSpMM = .11979f * 100 / 10.76f;
                    Friction0N = DavisAN * 2.0f;            //More firendly to high load trains and the new physics
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
            }
        }
        
        public static float ParseFloat(string token)
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

        public override void Update( float elapsedClockSeconds )
        {
            base.Update(elapsedClockSeconds);

            if (IsDavisFriction)  // If set to use next Davis friction then do so
            {
                // Davis formulas only apply above about 5mph, so different treatment required for low speed < 5mph.
                s = Math.Abs(SpeedMpS);
                if (s > MpS.FromMpH(5))     // if speed above 5 mph then turn off low speed calculations
                    IsLowSpeed = false;
                if (s == 0.0)
                    IsLowSpeed = true;
                
                float SteamLocoMechFrictN = GetSteamLocoMechFrictN();        
                
                if (IsLowSpeed)
                {
                    const float s5 = 2.2352f; // 5 mph
                    Friction5N = DavisAN + s5 * (DavisBNSpM + s5 * DavisCNSSpMM); // Calculate friction @ 5 mph
                    if (IsRollerBearing)
                    {
                        StaticFrictionFactorLb = 12.0f; // multiplier factor for roller bearings - 10lbs / short ton, seems low so try 12lbs
                        DavisANnew = DavisAN;
                        RollerBearingFrictionN = 0.9f * (DavisAN - SteamLocoMechFrictN); // Roller bearing friction reduced to 90% when train operating between 5 & 35mph
                        DavisANnew = RollerBearingFrictionN + SteamLocoMechFrictN; // Calculate new Davis A value based on a reduction for roller bearing               
                        Friction5N = DavisANnew + s5 * (DavisBNSpM + s5 * DavisCNSSpMM); // Calculate friction @ 5 mph - normal for friction bearings
                    }
                    else
                    {
                        StaticFrictionFactorLb = 20.0f; // multiplier factor for friction bearings - 20lbs / short ton
                        Friction5N = DavisAN + s5 * (DavisBNSpM + s5 * DavisCNSSpMM); // Calculate friction @ 5 mph - normal for friction bearings
                    }
                    Friction0N = N.FromLbf(Kg.ToTUS(MassKG) * StaticFrictionFactorLb) + SteamLocoMechFrictN; // Static friction is journal or roller bearing friction x factor + Mech Factor if steam 
                    float FrictionLowSpeedN = ((1.0f - (s / s5)) * (Friction0N - Friction5N)) + Friction5N; // Calculate friction below 5mph - decreases linearly with speed
                    FrictionForceN = FrictionLowSpeedN; // At low speed use this value
                }
                else
                {
                    if (IsRollerBearing)
                    {
                        RollerBearingCutoutSpeedMpS = MpS.FromMpH(35.0f);  // set the roller bearing cutout speed at 35mph
                        if (s < RollerBearingCutoutSpeedMpS)
                        {
                            DavisANnew = DavisAN;
                            RollerBearingFrictionN = 0.9f * (DavisANnew - SteamLocoMechFrictN); // Roller bearing friction reduced to 90% when train operating between 5 & 35mph
                            DavisANnew = RollerBearingFrictionN + SteamLocoMechFrictN; // Calculate new Davis A value based on a reduction for roller bearing                
                            FrictionForceN = DavisANnew + s * (DavisBNSpM + s * DavisCNSSpMM); // At speeds above 35mph use this value for roller bearings
                        }
                        else
                        {
                            FrictionForceN = DavisAN + s * (DavisBNSpM + s * DavisCNSSpMM); // At speeds above 35mph use this value for roller bearings
                        }
                    }
                    else
                    {
                        FrictionForceN = DavisAN + s * (DavisBNSpM + s * DavisCNSSpMM); // for friction bearings use this formula
                    }
                }
            }
            else  // else use old MSTS type friction
            {
                s = Math.Abs(SpeedMpS);
                if (s > 0.1)
                    IsStandStill = false;
                if (s == 0.0)
                    IsStandStill = true;

                if (IsStandStill)
                    FrictionForceN = Friction0N;
                else
                    FrictionForceN = DavisAN + s * (DavisBNSpM + s * DavisCNSSpMM);
            }




            foreach (MSTSCoupling coupler in Couplers)
            {
                if (-CouplerForceU > coupler.Break1N)
                {
                    CouplerOverloaded = true;
                }
                else
                    CouplerOverloaded = false;
            }

            MSTSBrakeSystem.Update(elapsedClockSeconds);
        }

        public override void SignalEvent(Event evt)
        {
            switch (evt)
            {
                case Event.Pantograph1Up: { Pan1Up = true; Pan = Pan1Up || Pan2Up; break; }
                case Event.Pantograph1Down: { Pan1Up = false; Pan = Pan1Up || Pan2Up; break; }
                case Event.Pantograph2Up: { Pan2Up = true; Pan = Pan1Up || Pan2Up; break; }
                case Event.Pantograph2Down: { Pan2Up = false; Pan = Pan1Up || Pan2Up; break; }
            }

            // TODO: This should be moved to TrainCar probably.
            foreach (var eventHandler in EventHandlers) // e.g. for HandleCarEvent() in Sounds.cs
                eventHandler.HandleEvent(evt);

            base.SignalEvent(evt);
        }

        // <CJComment> Expected pantograph handling to be in MSTSElectricLocomotive.cs,
        // but guess that some trains have pantographs on non-motorised cars </CJComment>
        public void ToggleFirstPantograph()
        {
    		Pan1Up = !Pan1Up;
            if (Simulator.PlayerLocomotive == this) //inform everyone else in the train
            {
                foreach (var car in Train.Cars)
                {
                    var mstsWagon = car as MSTSWagon;
                    if (car != this && mstsWagon != null)
                    {
                        if(car.AcceptMUSignals)
                            mstsWagon.Pan1Up = Pan1Up;
                    }
                }
            }
            if( Pan1Up ) {
                SignalEvent(Event.Pantograph1Up);
            } else {
                SignalEvent(Event.Pantograph1Down);
            }
        }

        public void ToggleSecondPantograph() {
            Pan2Up = !Pan2Up;
            if (Simulator.PlayerLocomotive == this) //inform everyone else in the train
            {
                foreach (var car in Train.Cars)
                {
                    var mstsWagon = car as MSTSWagon;
                    if (car != this && mstsWagon != null)
                    {
                        if (car.AcceptMUSignals)
                            mstsWagon.Pan2Up = Pan2Up;
                    }
                }
            }
            if( Pan2Up ) {
                SignalEvent(Event.Pantograph2Up);
            } else {
                SignalEvent(Event.Pantograph2Down);
            }
        }
        
        public void ToggleDoorsLeft() {
            DoorLeftOpen = !DoorLeftOpen;
            if( Simulator.PlayerLocomotive == this ) {//inform everyone else in the train
                foreach (var car in Train.Cars)
                {
                    var mstsWagon = car as MSTSWagon;
                    if (car != this && mstsWagon != null)
                    {
                        mstsWagon.DoorLeftOpen = DoorLeftOpen;
                        mstsWagon.SignalEvent(DoorLeftOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                    }
                }
                if (DoorLeftOpen) SignalEvent(Event.DoorOpen); // hook for sound trigger
                else SignalEvent(Event.DoorClose);
                Simulator.Confirmer.Confirm( CabControl.DoorsLeft, DoorLeftOpen ? CabSetting.On : CabSetting.Off );
            }
        }

        public void ToggleDoorsRight() {
            DoorRightOpen = !DoorRightOpen;
            if( Simulator.PlayerLocomotive == this ) { //inform everyone else in the train
                foreach (TrainCar car in Train.Cars)
                {
                    var mstsWagon = car as MSTSWagon;
                    if (car != this && mstsWagon != null)
                    {
                        mstsWagon.DoorRightOpen = DoorRightOpen;
                        mstsWagon.SignalEvent(DoorRightOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                    }
                }
                if (DoorRightOpen) SignalEvent(Event.DoorOpen); // hook for sound trigger
                else SignalEvent(Event.DoorClose);
                Simulator.Confirmer.Confirm( CabControl.DoorsRight, DoorRightOpen ? CabSetting.On : CabSetting.Off );
            }
        }

        public void ToggleMirrors() {
            MirrorOpen = !MirrorOpen;
            if (MirrorOpen) SignalEvent(Event.MirrorOpen); // hook for sound trigger
            else SignalEvent(Event.MirrorClose);
            Simulator.Confirmer.Confirm( CabControl.Mirror, MirrorOpen ? CabSetting.On : CabSetting.Off );
        }

        public void FindTendersSteamLocomotive()
        {
            if (Train == null || Train.Cars == null || Train.Cars.Count < 2)
            {
                TendersSteamLocomotive = null;
                return;
            }

            var tenderIndex = 0;
            for (var i = 0; i < Train.Cars.Count; i++)
            {
                if (Train.Cars[i] == this)
                    tenderIndex = i;
            }
            if (tenderIndex > 0 && Train.Cars[tenderIndex - 1] is MSTSSteamLocomotive)
                TendersSteamLocomotive = Train.Cars[tenderIndex - 1] as MSTSSteamLocomotive;
            if (tenderIndex < Train.Cars.Count - 1 && Train.Cars[tenderIndex + 1] is MSTSSteamLocomotive)
                TendersSteamLocomotive = Train.Cars[tenderIndex + 1] as MSTSSteamLocomotive;
        }

        // Make the Track Gauge available to other classes
        public override float GetTrackGaugeM()
        {
            TrackGaugeM = Gauge1M + Gauge2M;    // Calculate track gauge - it can be entered in ft in or M.
            if (TrackGaugeM == 0)
            {
                TrackGaugeM = 1.435f;       // If track gauge value not found then assume standard gauge - 4' 8.5" or 1.435m
            }
            
            return TrackGaugeM;
        }

        // Make the Centre of Gravity available to other classes
        public override float GetCentreofGravityM()
        {
            CentreOfGravityM = YCoGM;
            if (CentreOfGravityM == 0 || CentreOfGravityM > 3)
            {
                CentreOfGravityM = 1.8f; // if no value in wag file or is outside of bounds then set to a default value
            }
            return CentreOfGravityM;
        }

        // Make the vehicle rigid wheelbase is available to other classes
        public override float GetRigidWheelBaseM()
        {

            // Calculate the default Rigid Wheelbase if not in WAG File
            
            RigidWheelBaseM = WheelBase1M + WheelBase2M;
            
            return RigidWheelBaseM;
        }

        // Make the Locomotive Drive wheel Radius available to other classes
        public override float GetDriverWheelRadiusM()
        {

           return DriverWheelRadiusM;
        }




        // Make the vehicle num wheels available to other classes
        public override float GetWagonNumWheels()
        {

            WagonNumWheels = NumWheelsBrakingFactor;

            return WagonNumWheels;
        }

        // Pass the string wagon type to other classes
        public override string GetWagonType()
        {
          WagonType ="";  // set default
          
          if (IsFreight)
          {
          WagonType ="Freight";  // set as freight wagon
          }
          
          if (IsPassenger)
          {
          WagonType ="Passenger";  // set as passenger car
          }
          
          if (IsEngine)
          {
          WagonType ="Engine";  // set as passenger car
          }

          if (IsTender)
          {
          WagonType ="Tender";  // set as passenger car
          }
            return WagonType;
        }

        // Make the Unbalanced SuperElevation available to other classes
        public override float GetUnbalancedSuperElevationM()
        {
            if (UnbalancedSuperElevationM == 0 || UnbalancedSuperElevationM > 0.5) // If UnbalancedSuperElevationM > 12", or equal to zero, then set a default value
            {
                if (IsFreight)
                {
                    UnbalancedSuperElevationM = Me.FromIn(0.0f);  // Unbalanced superelevation has a maximum value of 0"
                }
                else if (IsPassenger)
                {
                    UnbalancedSuperElevationM = Me.FromIn(3.0f);  // Unbalanced superelevation has a maximum value of 6"
                }
                else if (IsEngine)
                {
                    UnbalancedSuperElevationM = Me.FromIn(6.0f);  // Unbalanced superelevation has a maximum value of 6"
                }
                else if (IsTender)
                {
                    UnbalancedSuperElevationM = Me.FromIn(6.0f);  // Unbalanced superelevation has a maximum value of 6"
                }
                else
                {
                    UnbalancedSuperElevationM = Me.FromIn(0.01f);  // if no value in wag file or is outside of bounds then set to a default value
                }
            }
            return UnbalancedSuperElevationM;
        }

        public bool GetTrainHandbrakeStatus()
        {
            return MSTSBrakeSystem.GetHandbrakeStatus();
        }

        // sound sources and viewers can register themselves to get direct notification of an event
        public List<EventHandler> EventHandlers = new List<EventHandler>();

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
        public override void CopyCoupler(TrainCar other)
        {
            base.CopyCoupler(other);
            MSTSCoupling coupler = new MSTSCoupling();
            coupler.R0 = other.GetCouplerZeroLengthM();
            coupler.R0Diff = other.GetMaximumCouplerSlack1M();
            coupler.Rigid = coupler.R0Diff < .0002f;
            coupler.Stiffness1NpM = other.GetCouplerStiffnessNpM() / 7;
            coupler.Stiffness2NpM = 0;
            if (Couplers.Count == 0)
                Couplers.Add(coupler);
            else
                Couplers[0]= coupler;
            if (Couplers.Count > 1)
                Couplers.RemoveAt(1);
        }

        public void SetWagonHandbrake(bool ToState)
        {
            if (ToState)
                MSTSBrakeSystem.SetHandbrakePercent(100);
            else
                MSTSBrakeSystem.SetHandbrakePercent(0);
        }
    }

    /// <summary>
    /// An IntakePoint object is created for any engine or wagon having a 
    /// IntakePoint block in its ENG/WAG file. 
    /// Called from within the MSTSWagon class.
    /// </summary>
    public class IntakePoint
    {
        public float OffsetM = 0f;   // distance forward? from the centre of the vehicle as defined by LengthM/2.
        public float WidthM = 10f;   // of the filling point. Is the maximum positioning error allowed equal to this or half this value? 
        public string Type;          // 'fuelcoal', 'fuelwater', 'fueldiesel', 'fuelwood'
        public float? DistanceFromFrontOfTrainM;

        public IntakePoint()
        {
        }
        
        public IntakePoint(STFReader stf)
        {
            stf.MustMatch("(");
            OffsetM = stf.ReadFloat(STFReader.UNITS.None, 0f);
            WidthM = stf.ReadFloat(STFReader.UNITS.None, 10f);
            Type = stf.ReadString().ToLower();
            stf.SkipRestOfBlock();
        }
    }

    public class MSTSCoupling
    {
        public bool Rigid;
        public float R0;
        public float R0Diff = .012f;
        public float Stiffness1NpM = 1e7f;
        public float Stiffness2NpM = 2e7f;
        public float Break1N = 1e10f;
        public float Break2N = 1e10f;

        public MSTSCoupling()
        {
        }
        public MSTSCoupling(MSTSCoupling copy)
        {
            Rigid = copy.Rigid;
            R0 = copy.R0;
            R0Diff = copy.R0Diff;
            Break1N = copy.Break1N;
            Break2N = copy.Break2N;
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
        }
        public void SetStiffness(float a, float b)
        {
            if (a + b < 0)
                return;
            Stiffness1NpM = a;
            Stiffness2NpM = b;
        }

        public void SetBreak(float a, float b)
        {
            if (a + b < 0)
                return;
            Break1N = a;
            Break2N = b;
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
            outf.Write(Break1N);
            outf.Write(Break2N);
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
            Break1N = inf.ReadSingle();
            Break2N = inf.ReadSingle();
        }
    }

    /// <summary>
    /// Support for animating any sub-part of a wagon or locomotive. Supports both on/off toggled animations and continuous-running ones.
    /// </summary>
    public class AnimatedPart
    {
        // Shape that we're animating.
        readonly PoseableShape PoseableShape;

        // Number of animation key-frames that are used by this part. This is calculated from the matrices provided.
        int FrameCount;

        // Current frame of the animation.
        float AnimationKey;

        // List of the matrices we're animating for this part.
        List<int> MatrixIndexes = new List<int>();

        /// <summary>
        /// Construct with a link to the shape that contains the animated parts 
        /// </summary>
        public AnimatedPart(PoseableShape poseableShape)
        {
            PoseableShape = poseableShape;
        }

        /// <summary>
        /// All the matrices associated with this part are added during initialization by the MSTSWagon constructor
        /// </summary>
        public void AddMatrix(int matrix)
        {
            if (matrix < 0) return;
            MatrixIndexes.Add(matrix);
            UpdateFrameCount(matrix);
        }

        void UpdateFrameCount(int matrix)
        {
            if (PoseableShape.SharedShape.Animations != null
                && PoseableShape.SharedShape.Animations.Count > 0
                && PoseableShape.SharedShape.Animations[0].anim_nodes.Count > matrix
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0
                && PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].Count > 0)
            {
                FrameCount = Math.Max(FrameCount, PoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].ToArray().Cast<KeyPosition>().Last().Frame);
            }
            for (var i = 0; i < PoseableShape.Hierarchy.Length; i++)
                if (PoseableShape.Hierarchy[i] == matrix)
                    UpdateFrameCount(i);
        }

        /// <summary>
        /// Ensure the shape file contained parts of this type 
        /// and those parts have an animation section.
        /// </summary>
        public bool Empty()
        {
            return MatrixIndexes.Count == 0;
        }

        void SetFrame(float frame)
        {
            AnimationKey = frame;
            foreach (var matrix in MatrixIndexes)
                PoseableShape.AnimateMatrix(matrix, AnimationKey);
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst clamping it to the frame count range.
        /// </summary>
        public void SetFrameClamp(float frame)
        {
            if (frame > FrameCount) frame = FrameCount;
            if (frame < 0) frame = 0;
            SetFrame(frame);
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst cycling back to the start as input goes beyond the last frame.
        /// </summary>
        public void SetFrameCycle(float frame)
        {
            // Animates from 0-FrameCount then FrameCount-0 for values of 0>=frame<=2*FrameCount.
            SetFrameClamp(FrameCount - Math.Abs(frame - FrameCount));
        }

        /// <summary>
        /// Sets the animation to a particular frame whilst wrapping it around the frame count range.
        /// </summary>
        public void SetFrameWrap(float frame)
        {
            // Wrap the frame around 0-FrameCount without hanging when FrameCount=0.
            while (FrameCount > 0 && frame < 0) frame += FrameCount;
            if (frame < 0) frame = 0;
            frame %= FrameCount;
            SetFrame(frame);
        }

        /// <summary>
        /// Bypass the normal slow transition and jump the part immediately to this new state
        /// </summary>
        public void SetState(bool state)
        {
            SetFrame(state ? FrameCount : 0);
        }

        /// <summary>
        /// Updates an animated part that toggles between two states (e.g. pantograph, doors, mirrors).
        /// </summary>
        public void UpdateState(bool state, ElapsedTime elapsedTime)
        {
            SetFrameClamp(AnimationKey + (state ? 1 : -1) * elapsedTime.ClockSeconds);
        }

        /// <summary>
        /// Updates an animated part that loops (e.g. running gear), changing by the given amount.
        /// </summary>
        public void UpdateLoop(float change)
        {
            if (PoseableShape.SharedShape.Animations == null || PoseableShape.SharedShape.Animations.Count == 0 || FrameCount == 0)
                return;

            // The speed of rotation is set at 8 frames of animation per rotation at 30 FPS (so 16 frames = 60 FPS, etc.).
            var frameRate = PoseableShape.SharedShape.Animations[0].FrameRate * 8 / 30f;
            SetFrameWrap(AnimationKey + change * frameRate);
        }

        /// <summary>
        /// Updates an animated part that loops only when enabled (e.g. wipers).
        /// </summary>
        public void UpdateLoop(bool running, ElapsedTime elapsedTime)
        {
            if (PoseableShape.SharedShape.Animations == null || PoseableShape.SharedShape.Animations.Count == 0 || FrameCount == 0)
                return;

            // The speed of cycling is set at 1.5 frames of animation per second at 30 FPS.
            var frameRate = PoseableShape.SharedShape.Animations[0].FrameRate * 1.5f / 30f;
            if (running || (AnimationKey > 0 && AnimationKey + elapsedTime.ClockSeconds < FrameCount))
                SetFrameWrap(AnimationKey + elapsedTime.ClockSeconds * frameRate);
            else
                SetFrame(0);
        }

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

    /// <summary>
    /// Utility class to avoid loading the wag file multiple times
    /// </summary>
    public class CarManager
    {
        public static Dictionary<string, MSTSWagon> LoadedCars = new Dictionary<string, MSTSWagon>();
    }


} // namespace ORTS
