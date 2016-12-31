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
//#define DEBUG_AUXTENDER

// Debug for Friction Force
//#define DEBUG_FRICTION

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks
{

    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////


    /// <summary>
    /// Represents the physical motion and behaviour of the car.
    /// </summary>

    public class MSTSWagon : TrainCar
    {
        public Pantographs Pantographs;
        public bool AuxPowerOn;
        public bool DoorLeftOpen;
        public bool DoorRightOpen;
        public bool MirrorOpen;
        public bool UnloadingPartsOpen;
        public bool WaitForAnimationReady; // delay counter to start loading/unliading is on;
        public bool IsRollerBearing; // Has roller bearings
        public bool IsLowTorqueRollerBearing; // Has low torque roller bearings
        public bool IsFrictionBearing; //Has friction (or solid bearings)
        public bool IsStandStill = true;  // Used for MSTS type friction
        public bool IsDavisFriction = true; // Default to new Davis type friction
        public bool IsLowSpeed = true; // set indicator for low speed operation  0 - 5mph

        Interpolator BrakeShoeFrictionFactor;  // Factor of friction for wagon brake shoes

        // simulation parameters
        public float Variable1;  // used to convey status to soundsource
        public float Variable2;
        public float Variable3;

        // wag file data
        public string MainShapeFileName;
        public string FreightShapeFileName;
        public float FreightAnimMaxLevelM;
        public float FreightAnimMinLevelM;
        public float FreightAnimFlag;   // if absent or >= 0 causes the freightanim to drop in tenders
        public string Cab3DShapeFileName; // 3DCab view shape file name
        public string InteriorShapeFileName; // passenger view shape file name
        public string MainSoundFileName;
        public string InteriorSoundFileName;
        public string Cab3DSoundFileName;
        public float WheelRadiusM = 1;          // provide some defaults in case it's missing from the wag
        protected float StaticFrictionFactorLb;    // factor to multiply friction by to determine static or starting friction - will vary depending upon whether roller or friction bearing
        public float Friction0N;        // static friction
        protected float Friction5N;               // Friction at 5mph
        public float DavisAN;           // davis equation constant
        public float DavisBNSpM;        // davis equation constant for speed
        public float DavisCNSSpMM;      // davis equation constant for speed squared
        protected float FrictionC1; // MSTS Friction parameters
        protected float FrictionE1; // MSTS Friction parameters
        protected float FrictionV2; // MSTS Friction parameters
        protected float FrictionC2; // MSTS Friction parameters
        protected float FrictionE2; // MSTS Friction parameters
        //protected float FrictionSpeedMpS; // Train current speed value for friction calculations ; this value is never used outside of this class, and FrictionSpeedMpS is always = AbsSpeedMpS
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
        public float AdhesionDriveWheelRadiusM;
        public float WheelSpeedMpS;
        public float WheelSpeedSlipMpS; // speed of wheel if locomotive is slipping
        public float SlipWarningThresholdPercent = 70;
        public float NumWheelsBrakingFactor = 4;   // MSTS braking factor loosely based on the number of braked wheels. Not used yet.
        public MSTSNotchController WeightLoadController; // Used to control freight loading in freight cars
        public float AbsWheelSpeedMpS; // Math.Abs(WheelSpeedMpS) is used frequently in the subclasses, maybe it's more efficient to compute it once

        /// <summary>
        /// True if vehicle is equipped with an additional emergency brake reservoir
        /// </summary>
        public bool EmergencyReservoirPresent;
        /// <summary>
        /// True if triple valve is capable of releasing brake gradually
        /// </summary>
        public bool DistributorPresent;
        /// <summary>
        /// True if equipped with handbrake. (Not common for older steam locomotives.)
        /// </summary>
        public bool HandBrakePresent;
        /// <summary>
        /// Number of available retainer positions. (Used on freight cars, mostly.) Might be 0, 3 or 4.
        /// </summary>
        public int RetainerPositions;

        /// <summary>
        /// Attached steam locomotive in case this wagon is a tender
        /// </summary>
        public MSTSSteamLocomotive TendersSteamLocomotive { get; private set; }

        public List<IntakePoint> IntakePointList = new List<IntakePoint>();

        /// <summary>
        /// Supply types for freight wagons and locos
        /// </summary>
        public enum PickupType
        {
            None = 0,
            FreightGrain = 1,
            FreightCoal = 2,
            FreightGravel = 3,
            FreightSand = 4,
            FuelWater = 5,
            FuelCoal = 6,
            FuelDiesel = 7,
            FuelWood = 8    // Think this is new to OR and not recognised by MSTS
        }

        public class RefillProcess
        {
            public static bool OkToRefill { get; set; }
            public static int ActivePickupObjectUID { get; set; }
            public static bool Unload { get; set; }
        }

        public MSTSBrakeSystem MSTSBrakeSystem { get { return (MSTSBrakeSystem)base.BrakeSystem; } }

        public MSTSWagon(Simulator simulator, string wagFilePath)
            : base(simulator, wagFilePath)
        {
            Pantographs = new Pantographs(this);
        }

        public void Load()
        {
            if (CarManager.LoadedCars.ContainsKey(WagFilePath))
            {
                Copy(CarManager.LoadedCars[WagFilePath]);
            }
            else
            {
                LoadFromWagFile(WagFilePath);
                CarManager.LoadedCars.Add(WagFilePath, this);
            }

            GetMeasurementUnits();
        }

        /// <summary>
        /// This initializer is called when we haven't loaded this type of car before
        /// and must read it new from the wag file.
        /// </summary>
        public virtual void LoadFromWagFile(string wagFilePath)
        {
            string dir = Path.GetDirectoryName(wagFilePath);
            string file = Path.GetFileName(wagFilePath);
            string orFile = dir + @"\openrails\" + file;
            if (File.Exists(orFile))
                wagFilePath = orFile;

            using (STFReader stf = new STFReader(wagFilePath, true))
            {
                while (!stf.Eof)
                {
                    stf.ReadItem();
                    Parse(stf.Tree.ToLower(), stf);
                }
            }

            var wagonFolderSlash = Path.GetDirectoryName(WagFilePath) + @"\";
            if (MainShapeFileName != null && !File.Exists(wagonFolderSlash + MainShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + MainShapeFileName);
                MainShapeFileName = string.Empty;
            }
            if (FreightShapeFileName != null && !File.Exists(wagonFolderSlash + FreightShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + FreightShapeFileName);
                FreightShapeFileName = null;
            }
            if (InteriorShapeFileName != null && !File.Exists(wagonFolderSlash + InteriorShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + InteriorShapeFileName);
                InteriorShapeFileName = null;
            }

            MassKG = InitialMassKG;
            if (FreightAnimations != null)
            {
                foreach (var ortsFreightAnim in FreightAnimations.Animations)
                {
                    if (ortsFreightAnim.ShapeFileName != null && !File.Exists(wagonFolderSlash + ortsFreightAnim.ShapeFileName))
                    {
                        Trace.TraceWarning("ORTS FreightAnim in trainset {0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + ortsFreightAnim.ShapeFileName);
                        ortsFreightAnim.ShapeFileName = null;
                    }

                }
                if (!FreightAnimations.MSTSFreightAnimEnabled) FreightShapeFileName = null;
                if (FreightAnimations.WagonEmptyWeight != -1) MassKG = FreightAnimations.WagonEmptyWeight + FreightAnimations.FreightWeight + FreightAnimations.StaticFreightWeight;
                if (FreightAnimations.LoadedOne != null) WeightLoadController.CurrentValue = FreightAnimations.LoadedOne.LoadPerCent / 100;

            }

            if (BrakeSystem == null)
                BrakeSystem = MSTSBrakeSystem.Create(CarBrakeSystemType, this);
        }

        public void GetMeasurementUnits()
        {
            IsMetric = Simulator.Settings.Units == "Metric" || (Simulator.Settings.Units == "Automatic" && System.Globalization.RegionInfo.CurrentRegion.IsMetric) ||
                (Simulator.Settings.Units == "Route" && Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric);
            IsUK = Simulator.Settings.Units == "UK";
        }

        public override void Initialize()
        {
            Pantographs.Initialize();

            base.Initialize();

            if (UnbalancedSuperElevationM == 0 || UnbalancedSuperElevationM > 0.5) // If UnbalancedSuperElevationM > 18", or equal to zero, then set a default value
            {
                switch (WagonType)
                {
                    case WagonTypes.Freight:
                        UnbalancedSuperElevationM = Me.FromIn(3.0f);  // Unbalanced superelevation has a maximum default value of 3"
                        break;
                    case WagonTypes.Passenger:
                        UnbalancedSuperElevationM = Me.FromIn(3.0f);  // Unbalanced superelevation has a maximum default value of 3"
                        break;
                    case WagonTypes.Engine:
                        UnbalancedSuperElevationM = Me.FromIn(6.0f);  // Unbalanced superelevation has a maximum default value of 6"
                        break;
                    case WagonTypes.Tender:
                        UnbalancedSuperElevationM = Me.FromIn(6.0f);  // Unbalanced superelevation has a maximum default value of 6"
                        break;
                    default:
                        UnbalancedSuperElevationM = Me.FromIn(0.01f);  // if no value in wag file or is outside of bounds then set to a default value
                        break;
                }
            }
        }

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
                    var wagonType = stf.ReadString();
                    try
                    {
                        WagonType = (WagonTypes)Enum.Parse(typeof(WagonTypes), wagonType.Replace("Carriage", "Passenger"));
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Skipped unknown wagon type " + wagonType);
                    }
                    break;
                case "wagon(freightanim":
                    stf.MustMatch("(");
                    FreightShapeFileName = stf.ReadString();
                    FreightAnimMaxLevelM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FreightAnimMinLevelM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FreightAnimFlag = stf.ReadFloat(STFReader.UNITS.Distance, 1.0f);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(size":
                    stf.MustMatch("(");
                    CarWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    CarHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    CarLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortstrackgauge":
                    stf.MustMatch("(");
                    TrackGaugeM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    TrackGaugeM += stf.ReadFloat(STFReader.UNITS.Distance, 0);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(centreofgravity":
                    stf.MustMatch("(");
                    CentreOfGravityM.X = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    CentreOfGravityM.Y = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    CentreOfGravityM.Z = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    if (Math.Abs(CentreOfGravityM.Z) > 1)
                    {
                        STFException.TraceWarning(stf, string.Format("Ignored CentreOfGravity Z value {0} outside range -1 to +1", CentreOfGravityM.Z));
                        CentreOfGravityM.Z = 0;
                    }
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsunbalancedsuperelevation": UnbalancedSuperElevationM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsrigidwheelbase":
                    stf.MustMatch("(");
                    RigidWheelBaseM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    RigidWheelBaseM += stf.ReadFloat(STFReader.UNITS.Distance, 0);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsauxtenderwatermass": AuxTenderWaterMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(mass": InitialMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); if (InitialMassKG < 0.1f) InitialMassKG = 0.1f; break;
                case "wagon(wheelradius": WheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(wheelradius": DriverWheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(sound": MainSoundFileName = stf.ReadStringBlock(null); break;
                case "wagon(ortsbrakeshoefriction": BrakeShoeFrictionFactor = new Interpolator(stf); break;
                case "wagon(ortsdavis_a": DavisAN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortsdavis_b": DavisBNSpM = stf.ReadFloatBlock(STFReader.UNITS.Resistance, null); break;
                case "wagon(ortsdavis_c": DavisCNSSpMM = stf.ReadFloatBlock(STFReader.UNITS.ResistanceDavisC, null); break;
                case "wagon(ortsbearingtype":
                    stf.MustMatch("(");
                    string typeString2 = stf.ReadString();
                    IsRollerBearing = String.Compare(typeString2, "Roller") == 0;
                    IsLowTorqueRollerBearing = String.Compare(typeString2, "Low") == 0;
                    IsFrictionBearing = String.Compare(typeString2, "Friction") == 0;
                    break;
                case "wagon(friction":
                    stf.MustMatch("(");
                    FrictionC1 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
                    FrictionE1 = stf.ReadFloat(STFReader.UNITS.None, null);
                    FrictionV2 = stf.ReadFloat(STFReader.UNITS.Speed, null);
                    FrictionC2 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
                    FrictionE2 = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                    ; break;
                case "wagon(brakesystemtype":
                    CarBrakeSystemType = stf.ReadStringBlock(null).ToLower();
                    BrakeSystem = MSTSBrakeSystem.Create(CarBrakeSystemType, this);
                    break;
                case "wagon(brakeequipmenttype":
                    foreach (var equipment in stf.ReadStringBlock("").ToLower().Replace(" ", "").Split(','))
                    {
                        switch (equipment)
                        {
                            case "distributor":
                            case "graduated_release_triple_valve": DistributorPresent = true; break;
                            case "emergency_brake_reservoir": EmergencyReservoirPresent = true; break;
                            case "handbrake": HandBrakePresent = true; break;
                            case "retainer_3_position": RetainerPositions = 3; break;
                            case "retainer_4_position": RetainerPositions = 4; break;
                        }
                    }
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
                case "wagon(adheasion":
                    stf.MustMatch("(");
                    Adhesion1 = stf.ReadFloat(STFReader.UNITS.None, null);
                    Adhesion2 = stf.ReadFloat(STFReader.UNITS.None, null);
                    Adhesion3 = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(ortscurtius_kniffler":
                    //e.g. Wagon ( ORTSAdhesion ( ORTSCurtius_Kniffler ( 7.5 44 0.161 0.7 ) ) )
                    stf.MustMatch("(");
                    Curtius_KnifflerA = stf.ReadFloat(STFReader.UNITS.None, 7.5f); if (Curtius_KnifflerA <= 0) Curtius_KnifflerA = 7.5f;
                    Curtius_KnifflerB = stf.ReadFloat(STFReader.UNITS.None, 44.0f); if (Curtius_KnifflerB <= 0) Curtius_KnifflerB = 44.0f;
                    Curtius_KnifflerC = stf.ReadFloat(STFReader.UNITS.None, 0.161f); if (Curtius_KnifflerC <= 0) Curtius_KnifflerC = 0.161f;
                    AdhesionK = stf.ReadFloat(STFReader.UNITS.None, 0.7f); if (AdhesionK <= 0) AdhesionK = 0.7f;
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(ortsslipwarningthreshold":
                    stf.MustMatch("(");
                    SlipWarningThresholdPercent = stf.ReadFloat(STFReader.UNITS.None, 70.0f); if (SlipWarningThresholdPercent <= 0) SlipWarningThresholdPercent = 70.0f;
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
                    AdhesionDriveWheelRadiusM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(lights":
                    Lights = new LightCollection(stf);
                    break;
                case "wagon(inside": HasInsideView = true; ParseWagonInside(stf); break;
                case "wagon(orts3dcab": Parse3DCab(stf); break;
                case "wagon(numwheels": NumWheelsBrakingFactor = stf.ReadFloatBlock(STFReader.UNITS.None, 4.0f); break;
                case "wagon(ortspantographs":
                    Pantographs.Parse(lowercasetoken, stf);
                    break;
                case "wagon(intakepoint": IntakePointList.Add(new IntakePoint(stf)); break;
                case "wagon(passengercapacity": HasPassengerCapacity = true; break;
                case "wagon(ortsfreightanims":
                    FreightAnimations = new FreightAnimations(stf, this);
                    break;
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
        public virtual void Copy(MSTSWagon copy)
        {
            MainShapeFileName = copy.MainShapeFileName;
            HasPassengerCapacity = copy.HasPassengerCapacity;
            WagonType = copy.WagonType;
            FreightShapeFileName = copy.FreightShapeFileName;
            FreightAnimMaxLevelM = copy.FreightAnimMaxLevelM;
            FreightAnimMinLevelM = copy.FreightAnimMinLevelM;
            FreightAnimFlag = copy.FreightAnimFlag;
            CarWidthM = copy.CarWidthM;
            CarHeightM = copy.CarHeightM;
            CarLengthM = copy.CarLengthM;
            TrackGaugeM = copy.TrackGaugeM;
            CentreOfGravityM = copy.CentreOfGravityM;
            UnbalancedSuperElevationM = copy.UnbalancedSuperElevationM;
            RigidWheelBaseM = copy.RigidWheelBaseM;
            AuxTenderWaterMassKG = copy.AuxTenderWaterMassKG;
            MassKG = copy.MassKG;
            InitialMassKG = copy.InitialMassKG;
            WheelRadiusM = copy.WheelRadiusM;
            DriverWheelRadiusM = copy.DriverWheelRadiusM;
            MainSoundFileName = copy.MainSoundFileName;
            BrakeShoeFrictionFactor = copy.BrakeShoeFrictionFactor;
            DavisAN = copy.DavisAN;
            DavisBNSpM = copy.DavisBNSpM;
            DavisCNSSpMM = copy.DavisCNSSpMM;
            FrictionC1 = copy.FrictionC1;
            FrictionE1 = copy.FrictionE1;
            FrictionV2 = copy.FrictionV2;
            FrictionC2 = copy.FrictionC2;
            FrictionE2 = copy.FrictionE2;
            IsDavisFriction = copy.IsDavisFriction;
            IsRollerBearing = copy.IsRollerBearing;
            IsLowTorqueRollerBearing = copy.IsLowTorqueRollerBearing;
            IsFrictionBearing = copy.IsFrictionBearing;
            CarBrakeSystemType = copy.CarBrakeSystemType;
            BrakeSystem = MSTSBrakeSystem.Create(CarBrakeSystemType, this);
            EmergencyReservoirPresent = copy.EmergencyReservoirPresent;
            DistributorPresent = copy.DistributorPresent;
            HandBrakePresent = copy.HandBrakePresent;
            RetainerPositions = copy.RetainerPositions;
            InteriorShapeFileName = copy.InteriorShapeFileName;
            InteriorSoundFileName = copy.InteriorSoundFileName;
            Cab3DShapeFileName = copy.Cab3DShapeFileName;
            Cab3DSoundFileName = copy.Cab3DSoundFileName;
            Adhesion1 = copy.Adhesion1;
            Adhesion2 = copy.Adhesion2;
            Adhesion3 = copy.Adhesion3;
            Curtius_KnifflerA = copy.Curtius_KnifflerA;
            Curtius_KnifflerB = copy.Curtius_KnifflerB;
            Curtius_KnifflerC = copy.Curtius_KnifflerC;
            AdhesionK = copy.AdhesionK;
            AxleInertiaKgm2 = copy.AxleInertiaKgm2;
            AdhesionDriveWheelRadiusM = copy.AdhesionDriveWheelRadiusM;
            SlipWarningThresholdPercent = copy.SlipWarningThresholdPercent;
            Lights = copy.Lights;
            foreach (PassengerViewPoint passengerViewPoint in copy.PassengerViewpoints)
                PassengerViewpoints.Add(passengerViewPoint);
            foreach (ViewPoint headOutViewPoint in copy.HeadOutViewpoints)
                HeadOutViewpoints.Add(headOutViewPoint);
            if (copy.CabViewpoints != null)
            {
                CabViewpoints = new List<PassengerViewPoint>();
                foreach (PassengerViewPoint cabViewPoint in copy.CabViewpoints)
                    CabViewpoints.Add(cabViewPoint);
            }
            foreach (MSTSCoupling coupler in copy.Couplers)
                Couplers.Add(coupler);

            Pantographs.Copy(copy.Pantographs);
            if (copy.FreightAnimations != null)
            {
                FreightAnimations = new FreightAnimations(copy.FreightAnimations, this);
            }

            if (copy.IntakePointList != null)
            {
                foreach (IntakePoint copyIntakePoint in copy.IntakePointList)
                {
                    if (copyIntakePoint.LinkedFreightAnim == null)
                        IntakePointList.Add(new IntakePoint(copyIntakePoint));
                }
            }

            MSTSBrakeSystem.InitializeFromCopy(copy.BrakeSystem);
            if (copy.WeightLoadController != null) WeightLoadController = new MSTSNotchController(copy.WeightLoadController);

        }

        protected void ParseWagonInside(STFReader stf)
        {
            PassengerViewPoint passengerViewPoint = new PassengerViewPoint();
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("sound", ()=>{ InteriorSoundFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("passengercabinfile", ()=>{ InteriorShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("passengercabinheadpos", ()=>{ passengerViewPoint.Location = stf.ReadVector3Block(STFReader.UNITS.Distance, new Vector3()); }),
                new STFReader.TokenProcessor("rotationlimit", ()=>{ passengerViewPoint.RotationLimit = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
                new STFReader.TokenProcessor("startdirection", ()=>{ passengerViewPoint.StartDirection = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
            });
            // Set initial direction
            passengerViewPoint.RotationXRadians = MathHelper.ToRadians(passengerViewPoint.StartDirection.X);
            passengerViewPoint.RotationYRadians = MathHelper.ToRadians(passengerViewPoint.StartDirection.Y);
            PassengerViewpoints.Add(passengerViewPoint);
        }
        protected void Parse3DCab(STFReader stf)
        {
            PassengerViewPoint passengerViewPoint = new PassengerViewPoint();
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("sound", ()=>{ Cab3DSoundFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("orts3dcabfile", ()=>{ Cab3DShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("orts3dcabheadpos", ()=>{ passengerViewPoint.Location = stf.ReadVector3Block(STFReader.UNITS.Distance, new Vector3()); }),
                new STFReader.TokenProcessor("rotationlimit", ()=>{ passengerViewPoint.RotationLimit = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
                new STFReader.TokenProcessor("startdirection", ()=>{ passengerViewPoint.StartDirection = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
            });
            // Set initial direction
            passengerViewPoint.RotationXRadians = MathHelper.ToRadians(passengerViewPoint.StartDirection.X);
            passengerViewPoint.RotationYRadians = MathHelper.ToRadians(passengerViewPoint.StartDirection.Y);
            if (this.CabViewpoints == null) CabViewpoints = new List<PassengerViewPoint>();
            CabViewpoints.Add(passengerViewPoint);
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
            outf.Write(IsDavisFriction);
            outf.Write(IsRollerBearing);
            outf.Write(IsLowTorqueRollerBearing);
            outf.Write(IsFrictionBearing);
            outf.Write(Friction0N);
            outf.Write(DavisAN);
            outf.Write(DavisBNSpM);
            outf.Write(DavisCNSSpMM);
            outf.Write(MassKG);
            outf.Write(Couplers.Count);
            foreach (MSTSCoupling coupler in Couplers)
                coupler.Save(outf);
            Pantographs.Save(outf);
            if (FreightAnimations != null)
            {
                FreightAnimations.Save(outf);
                if (WeightLoadController != null)
                {
                    outf.Write(true);
                    WeightLoadController.Save(outf);
                }
                else outf.Write(false);
            }
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
            IsDavisFriction = inf.ReadBoolean();
            IsRollerBearing = inf.ReadBoolean();
            IsLowTorqueRollerBearing = inf.ReadBoolean();
            IsFrictionBearing = inf.ReadBoolean();
            Friction0N = inf.ReadSingle();
            DavisAN = inf.ReadSingle();
            DavisBNSpM = inf.ReadSingle();
            DavisCNSSpMM = inf.ReadSingle();
            MassKG = inf.ReadSingle();
            int n = inf.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                Couplers.Add(new MSTSCoupling());
                Couplers[i].Restore(inf);
            }
            Pantographs.Restore(inf);
            if (FreightAnimations != null)
            {
                FreightAnimations.Restore(inf);
                var doesWeightLoadControllerExist = inf.ReadBoolean();
                if (doesWeightLoadControllerExist)
                {
                    var controllerType = inf.ReadInt32();
                    WeightLoadController.Restore(inf);
                }
            }

            base.Restore(inf);

            // always set aux power on due to error in PowerSupplyClass
            AuxPowerOn = true;
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);

            // Update Aux Tender Information

            // TODO: Replace AuxWagonType with new values of WagonType or similar. It's a bad idea having two fields that are nearly the same but not quite.
            if (AuxTenderWaterMassKG != 0)   // SetStreamVolume wagon type for later use
            {

                AuxWagonType = "AuxiliaryTender";
            }
            else
            {
                AuxWagonType = WagonType.ToString();
            }

#if DEBUG_AUXTENDER
            Trace.TraceInformation("***************************************** DEBUG_AUXTENDER (MSTSWagon.cs) ***************************************************************");
            Trace.TraceInformation("Car ID {0} Aux Tender Water Mass {1} Wagon Type {2}", CarID, AuxTenderWaterMassKG, AuxWagonType);
#endif

            AbsWheelSpeedMpS = Math.Abs(WheelSpeedMpS);
            if (IsDavisFriction == true) // test to see if OR thinks that Davis Values have been entered in WG file.
            {
                if (DavisAN == 0 || DavisBNSpM == 0 || DavisCNSSpMM == 0) // If Davis parameters are not defined in WAG file, then set falg to use default friction values
                    IsDavisFriction = false; // set to false - indicating that Davis friction is not used
            }

            if (IsDavisFriction == false)    // If Davis parameters are not defined in WAG file, then use default methods
            {

                if (FrictionV2 < 0 || FrictionV2 > 4.4407f) // > 10 mph
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
                    float mps1 = FrictionV2;
                    float mps2 = 80 * .44704f;
                    float s = mps2 - mps1;
                    float x1 = mps1 * mps1;
                    float x2 = mps2 * mps2;
                    float sx = (x2 - x1) / 2;
                    float y0 = FrictionC1 * (float)Math.Pow(mps1, FrictionE1) + FrictionC2 * mps1;
                    float y1 = FrictionC2 * (float)Math.Pow(mps1, FrictionE2) * mps1;
                    float y2 = FrictionC2 * (float)Math.Pow(mps2, FrictionE2) * mps2;
                    float sy = y0 * (mps2 - mps1) + (y2 - y1) / (1 + FrictionE2);
                    y1 *= mps1;
                    y2 *= mps2;
                    float syx = y0 * (x2 - x1) / 2 + (y2 - y1) / (2 + FrictionE2);
                    x1 *= mps1;
                    x2 *= mps2;
                    float sx2 = (x2 - x1) / 3;
                    y1 *= mps1;
                    y2 *= mps2;
                    float syx2 = y0 * (x2 - x1) / 3 + (y2 - y1) / (3 + FrictionE2);
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
                    Friction0N = FrictionC1;
                    if (FrictionE1 < 0)
                        Friction0N *= (float)Math.Pow(.0025 * .44704, FrictionE1);
                }

                if (AbsSpeedMpS > 0.1)
                    IsStandStill = false;
                if (AbsSpeedMpS == 0.0)
                    IsStandStill = true;

                if (IsStandStill)
                    FrictionForceN = Friction0N;
                else
                    FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * DavisCNSSpMM);

            }

            if (IsDavisFriction)  // If set to use next Davis friction then do so
            {
                // Davis formulas only apply above about 5mph, so different treatment required for low speed < 5mph.
                if (AbsSpeedMpS > MpS.FromMpH(5))     // if speed above 5 mph then turn off low speed calculations
                    IsLowSpeed = false;
                if (AbsSpeedMpS == 0.0)
                    IsLowSpeed = true;

                if (IsLowSpeed)
                {
                    // If weather is freezing, then starting friction will be greater until bearings have warmed up.
                    // Chwck whether weather is snowing

                    int FrictionWeather = (int)Simulator.WeatherType;
                    bool IsSnowing = false;

                    if (FrictionWeather == 1)
                    {
                        IsSnowing = true;  // Weather snowing - freezing conditions
                    }

                    // Dtermine the starting friction factor based upon the type of bearing

                    float StartFrictionLow = 0.0f;
                    float StartFrictionHigh = 0.0f;

                    if (IsRollerBearing)
                    {
                        if (!IsSnowing)
                        {
                            StartFrictionLow = 4.257f;  // Starting friction for a 10 ton(US) car with standard roller bearings, not snowing
                            StartFrictionHigh = 15.93f;  // Starting friction for a 100 ton(US) car with standard roller bearings, not snowing
                        }
                        else
                        {
                            StartFrictionLow = 12.771f;  // Starting friction for a 10 ton(US) car with standard roller bearings, snowing
                            StartFrictionHigh = 30.0f;  // Starting friction for a 100 ton(US) car with standard roller bearings, snowing
                        }
                        if (Kg.ToTUS(MassKG) < 10.0)
                        {
                            StaticFrictionFactorLb = StartFrictionLow;  // Starting friction for a < 10 ton(US) car with standard roller bearings
                        }
                        else if (Kg.ToTUS(MassKG) > 100.0)
                        {
                            StaticFrictionFactorLb = StartFrictionHigh;  // Starting friction for a > 100 ton(US) car with standard roller bearings
                        }
                        else
                        {
                            StaticFrictionFactorLb = (((Kg.ToTUS(MassKG) - 10.0f) / 90.0f) * (StartFrictionHigh - StartFrictionLow)) + StartFrictionLow;
                        }
                    }
                    else if (IsLowTorqueRollerBearing)
                    {
                        if (!IsSnowing)
                        {
                            StartFrictionLow = 2.66f;  // Starting friction for a 10 ton(US) car with Low troque bearings, not snowing
                            StartFrictionHigh = 7.714f;  // Starting friction for a 100 ton(US) car with low torque bearings, not snowing
                        }
                        else
                        {
                            StartFrictionLow = 7.98f;  // Starting friction for a 10 ton(US) car with Low troque bearings, not snowing
                            StartFrictionHigh = 23.142f;  // Starting friction for a 100 ton(US) car with low torque bearings, not snowing
                        }
                        if (Kg.ToTUS(MassKG) < 10.0)
                        {
                            StaticFrictionFactorLb = StartFrictionLow;  // Starting friction for a < 10 ton(US) car with Low troque bearings
                        }
                        else if (Kg.ToTUS(MassKG) > 100.0)
                        {
                            StaticFrictionFactorLb = StartFrictionHigh;  // Starting friction for a > 100 ton(US) car with low torque bearings
                        }
                        else
                        {
                            StaticFrictionFactorLb = (((Kg.ToTUS(MassKG) - 10.0f) / 90.0f) * (StartFrictionHigh - StartFrictionLow)) + StartFrictionLow;
                        }
                    }
                    else  // default to friction (solid - journal) bearing
                    {

                        if (!IsSnowing)
                        {
                            StartFrictionLow = 10.0f; // Starting friction for a < 10 ton(US) car with friction (journal) bearings - ton (US)
                            StartFrictionHigh = 20.0f; // Starting friction for a > 100 ton(US) car with friction (journal) bearings - ton (US)
                        }
                        else
                        {
                            StartFrictionLow = 15.0f; // Starting friction for a < 10 ton(US) car with friction (journal) bearings - ton (US)
                            StartFrictionHigh = 35.0f; // Starting friction for a > 100 ton(US) car with friction (journal) bearings - ton (US)
                        }

                        if (Kg.ToTUS(MassKG) < 10.0)
                        {
                            StaticFrictionFactorLb = StartFrictionLow;  // Starting friction for a < 10 ton(US) car with friction (journal) bearings
                        }
                        else if (Kg.ToTUS(MassKG) > 100.0)
                        {
                            StaticFrictionFactorLb = StartFrictionHigh;  // Starting friction for a > 100 ton(US) car with friction (journal) bearings
                        }
                        else
                        {
                            StaticFrictionFactorLb = (((Kg.ToTUS(MassKG) - 10.0f) / 90.0f) * (StartFrictionHigh - StartFrictionLow)) + StartFrictionLow;
                        }

                    }

                    const float speed5 = 2.2352f; // 5 mph
                    Friction5N = DavisAN + speed5 * (DavisBNSpM + speed5 * DavisCNSSpMM); // Calculate friction @ 5 mph
                    Friction0N = N.FromLbf(Kg.ToTUS(MassKG) * StaticFrictionFactorLb); // Static friction is journal or roller bearing friction x factor
                    float FrictionLowSpeedN = ((1.0f - (AbsSpeedMpS / speed5)) * (Friction0N - Friction5N)) + Friction5N; // Calculate friction below 5mph - decreases linearly with speed
                    FrictionForceN = FrictionLowSpeedN; // At low speed use this value

                }
                else
                {
                    FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * DavisCNSSpMM); // for normal speed operation
                }

#if DEBUG_FRICTION

                Trace.TraceInformation("========================== Debug Friction in MSTSWagon.cs ==========================================");
                Trace.TraceInformation("Stationary - CarID {0} Force0N {1} Force5N {2} Speed {3} Factor {4}", CarID, Friction0N, Friction5N, AbsSpeedMpS, StaticFrictionFactorLb);
                Trace.TraceInformation("Stationary - Mass {0} Mass (US-tons) {1}", MassKG, Kg.ToTUS(MassKG));
                Trace.TraceInformation("Stationary - Weather Type (1 for Snow) {0}", (int)Simulator.WeatherType);
                Trace.TraceInformation("Stationary - Force0 lbf {0} Force5 lbf {1}", N.ToLbf(Friction0N), N.ToLbf(Friction5N));

#endif

            }


            foreach (MSTSCoupling coupler in Couplers)
            {
                if (IsPlayerTrain) // Only break couplers on player trains
                {
                    if (-CouplerForceU > coupler.Break1N || IsCriticalSpeed == true)  // break couplers if forces exceeded onm coupler or train has "overturned" on curve
                    {
                        CouplerOverloaded = true;
                    }
                    else
                    {
                        CouplerOverloaded = false;
                    }
                }
                else // if not a player train then don't ever break the couplers
                {
                    CouplerOverloaded = false;
                }
            }

            Pantographs.Update(elapsedClockSeconds);
            
            MSTSBrakeSystem.Update(elapsedClockSeconds);

            if (WeightLoadController != null)
            {
                WeightLoadController.Update(elapsedClockSeconds);
                if (FreightAnimations.LoadedOne != null)
                {
                    FreightAnimations.LoadedOne.LoadPerCent = WeightLoadController.CurrentValue * 100;
                    FreightAnimations.FreightWeight = WeightLoadController.CurrentValue * FreightAnimations.LoadedOne.FreightWeightWhenFull;
                    if (IsPlayerTrain)
                    {
                        if (WeightLoadController.UpdateValue != 0.0)
                            Simulator.Confirmer.UpdateWithPerCent(CabControl.FreightLoad,
                                CabSetting.Increase, WeightLoadController.CurrentValue * 100);
                    }
                }
                if (WeightLoadController.UpdateValue == 0.0 && FreightAnimations.LoadedOne != null && FreightAnimations.LoadedOne.LoadPerCent == 0.0)
                {
                    FreightAnimations.LoadedOne = null;
                    FreightAnimations.FreightType = PickupType.None;
                }
                if (FreightAnimations.WagonEmptyWeight != -1) MassKG = FreightAnimations.WagonEmptyWeight + FreightAnimations.FreightWeight + FreightAnimations.StaticFreightWeight;
                if (WaitForAnimationReady && WeightLoadController.CommandStartTime + FreightAnimations.UnloadingStartDelay <= Simulator.ClockTime)
                {
                    WaitForAnimationReady = false;
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Starting unload"));
                    WeightLoadController.StartDecrease(WeightLoadController.MinimumValue);
                }
            }
        }

        public override void SignalEvent(Event evt)
        {
            switch (evt)
            {
                // Compatibility layer for MSTS events
                case Event.Pantograph1Up:
                    SignalEvent(PowerSupplyEvent.RaisePantograph, 1);
                    break;
                case Event.Pantograph1Down:
                    SignalEvent(PowerSupplyEvent.LowerPantograph, 1);
                    break;
                case Event.Pantograph2Up:
                    SignalEvent(PowerSupplyEvent.RaisePantograph, 2);
                    break;
                case Event.Pantograph2Down:
                    SignalEvent(PowerSupplyEvent.LowerPantograph, 2);
                    break;
            }

            // TODO: This should be moved to TrainCar probably.
            foreach (var eventHandler in EventHandlers) // e.g. for HandleCarEvent() in Sounds.cs
                eventHandler.HandleEvent(evt);

            base.SignalEvent(evt);
        }

        public override void SignalEvent(PowerSupplyEvent evt)
        {
            if (Simulator.PlayerLocomotive == this || AcceptMUSignals)
            {
                switch (evt)
                {
                    case PowerSupplyEvent.RaisePantograph:
                    case PowerSupplyEvent.LowerPantograph:
                        if (Pantographs != null)
                        {
                            Pantographs.HandleEvent(evt);
                            SignalEvent(Event.PantographToggle);
                        }
                        break;
                }
            }

            base.SignalEvent(evt);
        }

        public override void SignalEvent(PowerSupplyEvent evt, int id)
        {
            if (Simulator.PlayerLocomotive == this || AcceptMUSignals)
            {
                switch (evt)
                {
                    case PowerSupplyEvent.RaisePantograph:
                    case PowerSupplyEvent.LowerPantograph:
                        if (Pantographs != null)
                        {
                            Pantographs.HandleEvent(evt, id);
                            SignalEvent(Event.PantographToggle);
                        }
                        break;
                }
            }

            base.SignalEvent(evt, id);
        }

        public void ToggleDoorsLeft()
        {
            DoorLeftOpen = !DoorLeftOpen;
            if (Simulator.PlayerLocomotive == this || Train.LeadLocomotive == this) // second part for remote trains
            {//inform everyone else in the train
                foreach (var car in Train.Cars)
                {
                    var mstsWagon = car as MSTSWagon;
                    if (car != this && mstsWagon != null)
                    {
                        if (!car.Flipped ^ Flipped)
                        {
                            mstsWagon.DoorLeftOpen = DoorLeftOpen;
                            mstsWagon.SignalEvent(DoorLeftOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                        }
                        else
                        {
                            mstsWagon.DoorRightOpen = DoorLeftOpen;
                            mstsWagon.SignalEvent(DoorLeftOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                        }
                    }
                }
                if (DoorLeftOpen) SignalEvent(Event.DoorOpen); // hook for sound trigger
                else SignalEvent(Event.DoorClose);
                if (Simulator.PlayerLocomotive == this)
                {
                    if (!GetCabFlipped()) Simulator.Confirmer.Confirm(CabControl.DoorsLeft, DoorLeftOpen ? CabSetting.On : CabSetting.Off);
                    else Simulator.Confirmer.Confirm(CabControl.DoorsRight, DoorLeftOpen ? CabSetting.On : CabSetting.Off);
                }
            }
        }

        public void ToggleDoorsRight()
        {
            DoorRightOpen = !DoorRightOpen;
            if (Simulator.PlayerLocomotive == this || Train.LeadLocomotive == this) // second part for remote trains
            { //inform everyone else in the train
                foreach (TrainCar car in Train.Cars)
                {
                    var mstsWagon = car as MSTSWagon;
                    if (car != this && mstsWagon != null)
                    {
                        if (!car.Flipped ^ Flipped)
                        {
                            mstsWagon.DoorRightOpen = DoorRightOpen;
                            mstsWagon.SignalEvent(DoorRightOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                        }
                        else
                        {
                            mstsWagon.DoorLeftOpen = DoorRightOpen;
                            mstsWagon.SignalEvent(DoorRightOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                        }
                    }
                }
                if (DoorRightOpen) SignalEvent(Event.DoorOpen); // hook for sound trigger
                else SignalEvent(Event.DoorClose);
                if (Simulator.PlayerLocomotive == this)
                {
                    if (!GetCabFlipped()) Simulator.Confirmer.Confirm(CabControl.DoorsRight, DoorRightOpen ? CabSetting.On : CabSetting.Off);
                    else Simulator.Confirmer.Confirm(CabControl.DoorsLeft, DoorRightOpen ? CabSetting.On : CabSetting.Off);
                }
            }
        }

        public void ToggleMirrors()
        {
            MirrorOpen = !MirrorOpen;
            if (MirrorOpen) SignalEvent(Event.MirrorOpen); // hook for sound trigger
            else SignalEvent(Event.MirrorClose);
            if (Simulator.PlayerLocomotive == this) Simulator.Confirmer.Confirm(CabControl.Mirror, MirrorOpen ? CabSetting.On : CabSetting.Off);
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

        public bool GetTrainHandbrakeStatus()
        {
            return MSTSBrakeSystem.GetHandbrakeStatus();
        }

        // sound sources and viewers can register themselves to get direct notification of an event
        public List<Orts.Common.EventHandler> EventHandlers = new List<Orts.Common.EventHandler>();

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
                Couplers[0] = coupler;
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

        /// <summary>
        /// Returns the fraction of load already in wagon.
        /// </summary>
        /// <param name="pickupType">Pickup type</param>
        /// <returns>0.0 to 1.0. If type is unknown, returns 0.0</returns>
        public override float GetFilledFraction(uint pickupType)
        {
            var fraction = 0.0f;
            if (FreightAnimations.LoadedOne != null) fraction = FreightAnimations.LoadedOne.LoadPerCent / 100;
            return fraction;
        }

        /// <summary>
        /// Returns the Brake shoe coefficient.
        /// </summary>

        public override float GetUserBrakeShoeFrictionFactor()
        {
            var frictionfraction = 0.0f;
            if ( BrakeShoeFrictionFactor == null)
            {
                frictionfraction = 0.0f;
            }
            else
            {
                frictionfraction = BrakeShoeFrictionFactor[MpS.ToKpH(AbsSpeedMpS)];
            }
            
            return frictionfraction;
        }

        /// <summary>
        /// Returns the Brake shoe coefficient at zero speed.
        /// </summary>

        public override float GetZeroUserBrakeShoeFrictionFactor()
        {
            var frictionfraction = 0.0f;
            if (BrakeShoeFrictionFactor == null)
            {
                frictionfraction = 0.0f;
            }
            else
            {
                frictionfraction = BrakeShoeFrictionFactor[0.0f];
            }

            return frictionfraction;
        }       
      
        
        
        /// <summary>
        /// Starts a continuous increase in controlled value.
        /// </summary>
        /// <param name="type">Pickup point</param>
        public void StartRefillingOrUnloading(uint type, IntakePoint intakePoint, float fraction, bool unload)
        {
            var controller = WeightLoadController;
            if (controller == null)
            {
                Simulator.Confirmer.Message(ConfirmLevel.Error, Simulator.Catalog.GetString("Incompatible data"));
                return;
            }
            controller.SetValue(fraction);
            controller.CommandStartTime = Simulator.ClockTime;  // for Replay to use 

            if (FreightAnimations.LoadedOne == null)
            {
                FreightAnimations.FreightType = (MSTSWagon.PickupType)type;
                FreightAnimations.LoadedOne = intakePoint.LinkedFreightAnim;
            }
            if (!unload)
            {
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Starting refill"));
                controller.StartIncrease(controller.MaximumValue);
            }
            else
            {
                WaitForAnimationReady = true;
                UnloadingPartsOpen = true;
                if (FreightAnimations.UnloadingStartDelay > 0)
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Preparing for unload"));
            }

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
        public MSTSWagon.PickupType Type;          // 'freightgrain', 'freightcoal', 'freightgravel', 'freightsand', 'fuelcoal', 'fuelwater', 'fueldiesel', 'fuelwood'
        public float? DistanceFromFrontOfTrainM;
        public FreightAnimationContinuous LinkedFreightAnim = null;

        public IntakePoint()
        {
        }

        public IntakePoint(STFReader stf)
        {
            stf.MustMatch("(");
            OffsetM = stf.ReadFloat(STFReader.UNITS.None, 0f);
            WidthM = stf.ReadFloat(STFReader.UNITS.None, 10f);
            Type = (MSTSWagon.PickupType)Enum.Parse(typeof(MSTSWagon.PickupType), stf.ReadString().ToLower(), true);
            stf.SkipRestOfBlock();
        }

        // for copy
        public IntakePoint(IntakePoint copy)
        {
            OffsetM = copy.OffsetM;
            WidthM = copy.WidthM;
            Type = copy.Type;

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
    /// Utility class to avoid loading the wag file multiple times
    /// </summary>
    public class CarManager
    {
        public static Dictionary<string, MSTSWagon> LoadedCars = new Dictionary<string, MSTSWagon>();
    }
}
