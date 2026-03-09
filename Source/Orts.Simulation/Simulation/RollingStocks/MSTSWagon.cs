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

// Debug for Freight Animation Variable Mass
//#define DEBUG_VARIABLE_MASS

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using Orts.Simulation.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public ScriptedPassengerCarPowerSupply PassengerCarPowerSupply => PowerSupply as ScriptedPassengerCarPowerSupply;
        public Doors Doors;        
        public Door RightDoor => Doors.RightDoor;
        public Door LeftDoor => Doors.LeftDoor;

        public enum WindowState
            // Don't change the order of entries within this enum
        {
            Closed,
            Closing,
            Opening,
            Open,
        }

        public static int LeftWindowFrontIndex = 0;
        public static int RightWindowFrontIndex = 1;
        public static int LeftWindowRearIndex = 2;
        public static int RightWindowRearIndex = 3;
        public WindowState[] WindowStates = new WindowState[4];
        public float[] SoundHeardInternallyCorrection = new float[2];

        public bool MirrorOpen;
        public bool UnloadingPartsOpen;
        public bool WaitForAnimationReady; // delay counter to start loading/unliading is on;
        public enum BearingTypes
        {
            Default,    // MSTS friction
            Grease,     // Plain bearings with grease lubricant
            Friction,   // Plain bearings with oil lubricant
            Roller,     // Traditional roller bearings
            Low,        // Modern roller bearings
        }
        public BearingTypes BearingType = BearingTypes.Default;
        public bool IsStandStill = true;  // Used for MSTS type friction
        public bool IsDavisFriction = true; // Default to new Davis type friction
        public bool IsBelowMergeSpeed = true; // set indicator for low speed operation as per given speed


        public bool GenericItem1;
        public bool GenericItem2;
                
        const float WaterLBpUKG = 10.0f;    // lbs of water in 1 gal (uk)
        float TempMassDiffRatio;

        // simulation parameters
        public float Variable1;  // used to convey status to soundsource
        public float Variable2;
        public float Variable3;
        // additional engines
        public float Variable1_2;
        public float Variable1_3;
        public float Variable1_4;
        public float Variable2_Booster;

        // wag file data
        public string MainShapeFileName;
        public string FreightShapeFileName;
        public float FreightAnimMaxLevelM;
        public float FreightAnimMinLevelM;
        public float FreightAnimFlag = 1;   // if absent or >= 0 causes the freightanim to drop in tenders
        public string Cab3DShapeFileName; // 3DCab view shape file name
        public string InteriorShapeFileName; // passenger view shape file name
        public string MainSoundFileName;
        public string InteriorSoundFileName;
        public string Cab3DSoundFileName;
        public float ExternalSoundPassThruPercent = -1;
        public float TrackSoundPassThruPercent = -1;
        public float WheelRadiusM = Me.FromIn(18.0f);  // Provide some defaults in case it's missing from the wag - Wagon wheels could vary in size from approx 10" to 25".
        protected float StaticFrictionFactorN;    // factor to multiply friction by to determine static or starting friction - will vary depending upon whether roller or friction bearing
        float FrictionLowSpeedN; // Davis low speed value 0 - 5 mph
        float FrictionBelowMergeSpeedN; // Davis low speed value for defined speed
        public float Friction0N;        // static friction
        protected float Friction5N;               // Friction at 5mph
        public float StandstillFrictionN;
        public float MergeSpeedFrictionN;
        public float MergeSpeedMpS = MpS.FromMpH(5f);
        public float DavisAN;           // davis equation constant
        public float DavisBNSpM;        // davis equation constant for speed
        public float DavisCNSSpMM;      // davis equation constant for speed squared
        public float DavisDragConstant; // Drag coefficient for wagon
        public float WagonFrontalAreaM2; // Frontal area of wagon
        public float TrailLocoResistanceFactor; // Factor to reduce base and wind resistance if locomotive is not leading - based upon original Davis drag coefficients

        bool TenderWeightInitialized = false;
        float TenderWagonMaxFuelMassKG;
        float TenderWagonMaxOilMassL;
        float TenderWagonMaxWaterMassKG;

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
        public float AxleInertiaKgm2;    //axle inertia
        public float DriveWheelSpeedMpS; // wheel speed of steam loco drive wheels, allowing for drive wheels to spin different from idle wheels
        public float SlipWarningThresholdPercent = 70;
        public MSTSNotchController WeightLoadController; // Used to control freight loading in freight cars

        public Axles LocomotiveAxles;

        // Colours for smoke and steam effects
        public Color ExhaustTransientColor = Color.Black;
        public Color ExhaustDecelColor = Color.WhiteSmoke;
        public Color ExhaustSteadyColor = Color.Gray;

        // Wagon steam leaks
        public float HeatingHoseParticleDurationS;
        public float HeatingHoseSteamVelocityMpS;
        public float HeatingHoseSteamVolumeM3pS;

        // Wagon heating compartment steamtrap leaks
        public float HeatingCompartmentSteamTrapParticleDurationS;
        public float HeatingCompartmentSteamTrapVelocityMpS;
        public float HeatingCompartmentSteamTrapVolumeM3pS;

        // Wagon heating steamtrap leaks
        public float HeatingMainPipeSteamTrapDurationS;
        public float HeatingMainPipeSteamTrapVelocityMpS;
        public float HeatingMainPipeSteamTrapVolumeM3pS;

        // Steam Brake leaks
        public float SteamBrakeLeaksDurationS;
        public float SteamBrakeLeaksVelocityMpS;
        public float SteamBrakeLeaksVolumeM3pS;

        // Water Scoop Spray
        public float WaterScoopParticleDurationS;
        public float WaterScoopWaterVelocityMpS;
        public float WaterScoopWaterVolumeM3pS;

        // Tender Water overflow
        public float TenderWaterOverflowParticleDurationS;
        public float TenderWaterOverflowVelocityMpS;
        public float TenderWaterOverflowVolumeM3pS;

        // Wagon Power Generator
        public float WagonGeneratorDurationS = 1.5f;
        public float WagonGeneratorVolumeM3pS = 2.0f;
        public Color WagonGeneratorSteadyColor = Color.Gray;

        // Heating Steam Boiler
        public float HeatingSteamBoilerDurationS;
        public float HeatingSteamBoilerVolumeM3pS;
        public Color HeatingSteamBoilerSteadyColor = Color.LightSlateGray;
        public bool HeatingBoilerSet = false;

        // Wagon Smoke
        public float WagonSmokeVolumeM3pS;
        float InitialWagonSmokeVolumeM3pS = 3.0f;
        public float WagonSmokeDurationS;
        float InitialWagonSmokeDurationS = 1.0f;
        public float WagonSmokeVelocityMpS = 15.0f;
        public Color WagonSmokeSteadyColor = Color.Gray;

        float TrueCouplerCount = 0;
        int CouplerCountLocation;

        // Bearing Hot Box Smoke
        public float BearingHotBoxSmokeVolumeM3pS;
        public float BearingHotBoxSmokeDurationS;
        public float BearingHotBoxSmokeVelocityMpS = 15.0f;
        public Color BearingHotBoxSmokeSteadyColor = Color.Gray;
        List<string> BrakeEquipment = new List<string>();

        /// <summary>
        /// Indicates whether a non auto (straight) brake is present or not when braking is selected.
        /// </summary>
        public bool NonAutoBrakePresent;

        /// <summary>
        /// Active locomotive for a control trailer
        /// </summary>
        public MSTSLocomotive ControlActiveLocomotive { get; private set; }

        /// <summary>
        /// Attached steam locomotive in case this wagon is a tender
        /// </summary>
        public MSTSSteamLocomotive TendersSteamLocomotive { get; private set; }

        /// <summary>
        /// Attached steam locomotive in case this wagon is an auxiliary tender
        /// </summary>
        public MSTSSteamLocomotive AuxTendersSteamLocomotive { get; private set; }

        /// <summary>
        /// Tender attached to this steam locomotive
        /// </summary>
        public TrainCar AttachedTender { get; private set; }

        /// <summary>
        /// Steam locomotive has a tender coupled to it
        /// </summary>
        public MSTSSteamLocomotive SteamLocomotiveTender { get; private set; }

        /// <summary>
        /// Steam locomotive identifier (pass parameters from MSTSSteamLocomotive to MSTSWagon)
        /// </summary>
        public MSTSSteamLocomotive SteamLocomotiveIdentification { get; private set; }

        /// <summary>
        /// Diesel locomotive identifier  (pass parameters from MSTSDieselLocomotive to MSTSWagon)
        /// </summary>
        public MSTSDieselLocomotive DieselLocomotiveIdentification { get; private set; }

        public Dictionary<string, List<ParticleEmitterData>> EffectData = new Dictionary<string, List<ParticleEmitterData>>();

        protected void ParseEffects(string lowercasetoken, STFReader stf)
        {
            EffectData.Clear(); // Remove any existing effects

            stf.MustMatch("(");
            string s;
            
            while ((s = stf.ReadItem()) != ")")
            {
                var data = new ParticleEmitterData(stf);
                if (!EffectData.ContainsKey(s))
                    EffectData.Add(s, new List<ParticleEmitterData>());
                EffectData[s].Add(data);
            }

        }


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
            FuelWood = 8,    // Think this is new to OR and not recognised by MSTS
            FuelSand = 9,  // New to OR
            FreightGeneral = 10, // New to OR
            FreightLivestock = 11,  // New to OR
            FreightFuel = 12,  // New to OR
            FreightMilk = 13,   // New to OR
            SpecialMail = 14,  // New to OR
            Container = 15  // New to OR
        }

        public class RefillProcess
        {
            public static bool OkToRefill { get; set; }
            public static int ActivePickupObjectUID { get; set; }
            public static bool Unload { get; set; }
        }

        public MSTSBrakeSystem MSTSBrakeSystem
        {
            get { return (MSTSBrakeSystem)base.BrakeSystem; }
            set { base.BrakeSystem = value; } // value needs to be set to allow trailing cars to have same brake system as locomotive when in simple brake mode
        }

        public MSTSWagon(Simulator simulator, string wagFilePath)
            : base(simulator, wagFilePath)
        {
            Pantographs = new Pantographs(this);
            Doors = new Doors(this);
            LocomotiveAxles = new Axles(this);
        }

        public void Load()
        {
            // If this wagon already has a viewer, the viewer will need to be reloaded
            StaleViewer = true;

            if (CarManager.LoadedCars.ContainsKey(WagFilePath) && !CarManager.LoadedCars[WagFilePath].StaleData)
            {
                Copy(CarManager.LoadedCars[WagFilePath]);
            }
            else
            {
                LoadFromWagFile(WagFilePath);
                CarManager.LoadedCars[WagFilePath] = this;
            }

            GetMeasurementUnits();

            StaleData = false;
        }

        // Values for adjusting wagon physics due to load changes
        float LoadEmptyMassKg;
        float LoadEmptyORTSDavis_A;
        float LoadEmptyORTSDavis_B;
        float LoadEmptyORTSDavis_C;
        float LoadEmptyWagonFrontalAreaM2;
        float LoadEmptyDavisDragConstant;
        float LoadEmptyMaxBrakeForceN;
        float LoadEmptyMaxHandbrakeForceN;
        float LoadEmptyCentreOfGravityM_Y;
        float LoadEmptyRelayValveRatio;
        float LoadEmptyInshotPSI;

        float LoadFullMassKg;
        float LoadFullORTSDavis_A;
        float LoadFullORTSDavis_B;
        float LoadFullORTSDavis_C;
        float LoadFullWagonFrontalAreaM2;
        float LoadFullDavisDragConstant;
        float LoadFullMaxBrakeForceN;
        float LoadFullMaxHandbrakeForceN;
        float LoadFullCentreOfGravityM_Y;
        float LoadFullRelayValveRatio;
        float LoadFullInshotPSI;


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

            // Get the path starting at the TRAINS folder, in order to produce a shorter, more legible, path
            string shortPath = wagFilePath.Remove(0, Simulator.BasePath.Length);

            using (STFReader stf = new STFReader(wagFilePath, true))
            {
                while (!stf.Eof)
                {
                    stf.ReadItem();
                    Parse(stf.Tree.ToLower(), stf);
                }
                if (Simulator.Settings.EnableHotReloading)
                    FilesReferenced = stf.FileNames.Select(p => p.ToLowerInvariant()).ToHashSet();
            }

            var wagonFolderSlash = Path.GetDirectoryName(WagFilePath) + @"\";
            if (MainShapeFileName != null && !File.Exists(wagonFolderSlash + MainShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", shortPath, wagonFolderSlash + MainShapeFileName);
                MainShapeFileName = string.Empty;
            }
            if (FreightShapeFileName != null && !File.Exists(wagonFolderSlash + FreightShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", shortPath, wagonFolderSlash + FreightShapeFileName);
                FreightShapeFileName = null;
            }
            if (InteriorShapeFileName != null && !File.Exists(wagonFolderSlash + InteriorShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", shortPath, wagonFolderSlash + InteriorShapeFileName);
                InteriorShapeFileName = null;
            }

            if (FrontCoupler.Closed.ShapeFileName != null && !File.Exists(wagonFolderSlash + FrontCoupler.Closed.ShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", shortPath, wagonFolderSlash + FrontCoupler.Closed.ShapeFileName);
                FrontCoupler.Closed.ShapeFileName = null;
            }

            if (RearCoupler.Closed.ShapeFileName != null && !File.Exists(wagonFolderSlash + RearCoupler.Closed.ShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", shortPath, wagonFolderSlash + RearCoupler.Closed.ShapeFileName);
                RearCoupler.Closed.ShapeFileName = null;
            }

            if (FrontAirHose.Connected.ShapeFileName != null && !File.Exists(wagonFolderSlash + FrontAirHose.Connected.ShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", shortPath, wagonFolderSlash + FrontAirHose.Connected.ShapeFileName);
                FrontAirHose.Connected.ShapeFileName = null;
            }

            if (RearAirHose.Connected.ShapeFileName != null && !File.Exists(wagonFolderSlash + RearAirHose.Connected.ShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", shortPath, wagonFolderSlash + RearAirHose.Connected.ShapeFileName);
                RearAirHose.Connected.ShapeFileName = null;
            }

            // If trailing loco resistance constant has not been  defined in WAG/ENG file then assign default value based upon orig Davis values
            if (TrailLocoResistanceFactor == 0)
            {
                if (WagonType == WagonTypes.Engine)
                {
                    TrailLocoResistanceFactor = 0.2083f;  // engine drag value
                }
                else if (WagonType == WagonTypes.Tender)
                {
                    TrailLocoResistanceFactor = 1.0f;  // assume that tenders have been set with a value of 0.0005 as per freight wagons
                }
                else  //Standard default if not declared anywhere else
                {
                    TrailLocoResistanceFactor = 1.0f;
                }
            }

            // Initialise car body lengths. Assume overhang is 2.0m each end, and bogie centres are the car length minus this value

            if (CarCouplerFaceLengthM == 0)
            {
                CarCouplerFaceLengthM = CarLengthM;
            }

            if (CarBodyLengthM == 0)
            {
                CarBodyLengthM = CarCouplerFaceLengthM - 0.8f;
            }

            if (CarBogieCentreLengthM == 0)
            {
                CarBogieCentreLengthM = CarCouplerFaceLengthM - 4.3f;

                // Prevent negative values on very short train cars
                if (CarBogieCentreLengthM < 0)
                    CarBogieCentreLengthM = CarLengthM * 0.65f;
            }

            if (CarAirHoseLengthM == 0)
            {
                CarAirHoseLengthM = Me.FromIn(26.25f); // 26.25 inches
            }

            var couplerlength = ((CarCouplerFaceLengthM - CarBodyLengthM) / 2) + 0.1f; // coupler length at rest, allow 0.1m also for slack

            if (CarAirHoseHorizontalLengthM == 0)
            {
                CarAirHoseHorizontalLengthM = 0.3862f; // 15.2 inches
            }

            // Disable derailment coefficent on "dummy" cars. NB: Ideally this should never be used as "dummy" cars interfer with the overall train physics.
            if (MSTSWagonNumWheels == 0 && InitWagonNumAxles == 0 )
            {
                DerailmentCoefficientEnabled = false;

                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Derailment Coefficient set to false for Wagon {0}", shortPath);
                }
            }

            // Ensure Drive Axles is set to a default if no OR value added to WAG file
            if (InitWagonNumAxles == 0 && WagonType != WagonTypes.Engine)
            {
                if (MSTSWagonNumWheels != 0 && MSTSWagonNumWheels < 6)
                {
                    WagonNumAxles = (int) MSTSWagonNumWheels;
                }
                else
                {
                    WagonNumAxles = 4; // Set 4 axles as default
                }

                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Number of Wagon Axles set to default value of {0} on Wagon {1}", WagonNumAxles, shortPath);
                }
            }
            else
            {
                WagonNumAxles = InitWagonNumAxles;
            }

            // Should always be at least one bogie on rolling stock. If is zero then NaN error occurs.
            if (WagonNumBogies == 0)
            {
                WagonNumBogies = 1;
            }

            // Set wheel flange parameters to default values.
            if (MaximumWheelFlangeAngleRad == 0)
            {
                MaximumWheelFlangeAngleRad = 1.22173f; // Default = 70 deg - Pre 1990 AAR 1:20 wheel
            }

            if (WheelFlangeLengthM == 0)
            {
                WheelFlangeLengthM = 0.0254f; // Height = 1.00in - Pre 1990 AAR 1:20 wheel
            }

            // Initialise steam heat parameters
            if (TrainHeatBoilerWaterUsageGalukpH == null) // If no table entered in WAG file, then use the default table
            {
                TrainHeatBoilerWaterUsageGalukpH = SteamHeatBoilerWaterUsageGalukpH();
            }

            if (TrainHeatBoilerFuelUsageGalukpH == null) // If no table entered in WAG file, then use the default table
            {
                TrainHeatBoilerFuelUsageGalukpH = SteamHeatBoilerFuelUsageGalukpH();
            }
            MaximumSteamHeatingBoilerSteamUsageRateLbpS = TrainHeatBoilerWaterUsageGalukpH.MaxX(); // Find maximum steam capacity of the generator based upon the information in the water usage table
            CurrentSteamHeatBoilerFuelCapacityL = MaximiumSteamHeatBoilerFuelTankCapacityL;

            if (MaximumSteamHeatBoilerWaterTankCapacityL != 0)
            {
                CurrentCarSteamHeatBoilerWaterCapacityL = MaximumSteamHeatBoilerWaterTankCapacityL;
            }
            else
            {
                CurrentCarSteamHeatBoilerWaterCapacityL = L.FromGUK(800.0f);
            }

            MassKG = InitialMassKG;

            // If Davis A value is not defined, but bearing type is, estimate Davis A based on the bearing and wagon parameters
            if (BearingType != BearingTypes.Default && DavisAN <= 0)
            {
                DavisAN = CalcDavisAValue(BearingType, MassKG, (WagonNumAxles + LocoNumDrvAxles));

                // Add some extra resistance to steam locomotives for running gear drag
                if (this is MSTSLocomotive loco && loco.EngineType == EngineTypes.Steam)
                        DavisAN += N.FromLbf(20.0f * Kg.ToTUS(loco.InitialDrvWheelWeightKg)); // 20 pounds per us ton of driven weight
                // Note: at this point, loco.DrvWheelWeightKg hasn't been determined, so we use the InitialDrvWheelWeightKg as an estimate

                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Rolling stock {0} defines ORTSBearingType ( {1} ) but does not define a value for ORTSDavis_A.", shortPath, BearingType);
                    Trace.TraceInformation("Davis A value automatically calculated to be {0}, given {1} bearings, mass of {2}, and {3} axles.\n",
                        FormatStrings.FormatForce(DavisAN, IsMetric), BearingType, FormatStrings.FormatLargeMass(MassKG, IsMetric, IsUK), (WagonNumAxles + LocoNumDrvAxles));
                }
            }

            // If Davis B value is not defined, but bearing type is, estimate Davis B based on the bearing and wagon parameters
            if (BearingType != BearingTypes.Default && DavisBNSpM <= 0)
            {
                DavisBNSpM = CalcDavisBValue(BearingType, MassKG, (WagonNumAxles + LocoNumDrvAxles), WagonType);

                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Rolling stock {0} defines ORTSBearingType ( {1} ) but does not define a value for ORTSDavis_B.", shortPath, BearingType);
                    Trace.TraceInformation("Davis B value automatically calculated to be {0}, given {1} bearings, mass of {2}, and wagon type {3}.\n",
                        FormatStrings.FormatLinearResistance(DavisBNSpM, IsMetric), BearingType, FormatStrings.FormatLargeMass(MassKG, IsMetric, IsUK), WagonType);
                }
            }

            // If Drag constant not defined in WAG/ENG file then assign default value based upon orig Davis values
            if (DavisDragConstant == 0)
            {
                if (WagonType == WagonTypes.Engine)
                {
                    DavisDragConstant = 0.0024f;
                }
                else if (WagonType == WagonTypes.Freight)
                {
                    DavisDragConstant = 0.0005f;
                }
                else if (WagonType == WagonTypes.Passenger)
                {
                    DavisDragConstant = 0.00034f;
                }
                else if (WagonType == WagonTypes.Tender)
                {
                    DavisDragConstant = 0.0005f;
                }
                else  //Standard default if not declared anywhere else
                {
                    DavisDragConstant = 0.0005f;
                }
            }

            // If wagon frontal area not user defined, assign a default value based upon the wagon dimensions

            if (WagonFrontalAreaM2 == 0)
            {
                WagonFrontalAreaM2 = CarWidthM * CarHeightM;
            }

            // If Davis C value is not defined, determine it from the drag constant and area
            if (DavisCNSSpMM <= 0)
            {
                // Note: Davis drag constant is intended to be used with area in ft^2
                DavisCNSSpMM = NSSpMM.FromLbfpMpH2(Me2.ToFt2(WagonFrontalAreaM2) * DavisDragConstant);

                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Rolling stock {0} does not define a value for ORTSDavis_C.", shortPath);
                    Trace.TraceInformation("Davis C value automatically calculated to be {0}, given frontal area of {1} and Davis drag constant of {2:F5}.\n",
                        FormatStrings.FormatQuadraticResistance(DavisCNSSpMM, IsMetric), FormatStrings.FormatArea(WagonFrontalAreaM2, IsMetric), DavisDragConstant);
                }
            }

            // Initialise key wagon parameters
            MaxHandbrakeForceN = InitialMaxHandbrakeForceN;

            FrictionBrakeBlendingMaxForceN = InitialMaxBrakeForceN; // set the value of braking when blended with dynamic brakes

            if (MaxBrakeShoeForceN != 0 && BrakeShoeType != BrakeShoeTypes.Unknown)
            {
                MaxBrakeForceN = MaxBrakeShoeForceN;            
            }
            else
            {
                MaxBrakeForceN = InitialMaxBrakeForceN;

                if (Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Unknown BrakeShoeType set to OR default (Cast Iron) with a MaxBrakeForce of {0}", FormatStrings.FormatForce(MaxBrakeForceN, IsMetric));
                }
            }

            // Initialise number of brake shoes per wagon
            if (NumberCarBrakeShoes == 0 && WagonType == WagonTypes.Engine)
            {
                var LocoTest = Simulator.PlayerLocomotive as MSTSLocomotive;

                if (LocoTest != null && LocoTest.DriveWheelOnlyBrakes)
                {
                    NumberCarBrakeShoes = LocoNumDrvAxles * 4; // Assume 4 brake shoes per axle on drive wheels only                    
                }
                else
                {
                    NumberCarBrakeShoes = (LocoNumDrvAxles * 4) + (WagonNumAxles * 4); // Assume 4 brake shoes per axle on all wheels
                } 

                if (Simulator.Settings.VerboseConfigurationMessages && (BrakeShoeType != BrakeShoeTypes.User_Defined || BrakeShoeType != BrakeShoeTypes.Unknown))
                {
                    Trace.TraceInformation("Number of Locomotive Brakeshoes set to default value of {0}", NumberCarBrakeShoes);
                }
            }
            else if (NumberCarBrakeShoes == 0)
            {
                NumberCarBrakeShoes = WagonNumAxles * 4; // Assume 4 brake shoes per axle

                if (Simulator.Settings.VerboseConfigurationMessages && (BrakeShoeType != BrakeShoeTypes.User_Defined || BrakeShoeType != BrakeShoeTypes.Unknown))
                {
                    Trace.TraceInformation("Number of Wagon Brakeshoes set to default value of {0}", NumberCarBrakeShoes);
                }
            }

            CentreOfGravityM = InitialCentreOfGravityM;

            if (FreightAnimations != null)
            {
                foreach (var ortsFreightAnim in FreightAnimations.Animations)
                {
                    if (ortsFreightAnim.ShapeFileName != null && !File.Exists(wagonFolderSlash + ortsFreightAnim.ShapeFileName))
                    {
                        Trace.TraceWarning("ORTS FreightAnim in trainset {0} references non-existent shape {1}", shortPath, wagonFolderSlash + ortsFreightAnim.ShapeFileName);
                        ortsFreightAnim.ShapeFileName = null;
                    }

                }

                // Read freight animation values from animation INCLUDE files
                // Read (initialise) "common" (empty values first).
                // Test each value to make sure that it has been defined in the WAG file, if not default to Root WAG file value
                if (FreightAnimations.WagonEmptyWeight > 0)
                {
                    LoadEmptyMassKg = FreightAnimations.WagonEmptyWeight;
                }
                else
                {
                    LoadEmptyMassKg = MassKG;
                }  
                
                if (FreightAnimations.EmptyORTSDavis_A > 0)
                {
                    LoadEmptyORTSDavis_A = FreightAnimations.EmptyORTSDavis_A;
                }
                else if (BearingType == BearingTypes.Default)
                {
                    // Use default if bearing type isn't given
                    // If bearing type is given, we will calculate the Davis value later
                    LoadEmptyORTSDavis_A = DavisAN;
                }

                if (FreightAnimations.EmptyORTSDavis_B > 0)
                {
                    LoadEmptyORTSDavis_B = FreightAnimations.EmptyORTSDavis_B;
                }
                else if (BearingType == BearingTypes.Default)
                {
                    // Use default if bearing type isn't given
                    // If bearing type is given, we will calculate the Davis value later
                    LoadEmptyORTSDavis_B = DavisBNSpM;
                }

                if (FreightAnimations.EmptyORTSDavisDragConstant > 0)
                {
                    LoadEmptyDavisDragConstant = FreightAnimations.EmptyORTSDavisDragConstant;
                }
                else
                {
                    LoadEmptyDavisDragConstant = DavisDragConstant;
                }

                if (FreightAnimations.EmptyORTSWagonFrontalAreaM2 > 0)
                {
                    LoadEmptyWagonFrontalAreaM2 = FreightAnimations.EmptyORTSWagonFrontalAreaM2;
                }
                else
                {
                    LoadEmptyWagonFrontalAreaM2 = WagonFrontalAreaM2;
                }

                if (FreightAnimations.EmptyORTSDavis_C > 0)
                {
                    LoadEmptyORTSDavis_C = FreightAnimations.EmptyORTSDavis_C;
                }
                else
                {
                    LoadEmptyORTSDavis_C = NSSpMM.FromLbfpMpH2(Me2.ToFt2(LoadEmptyWagonFrontalAreaM2) * LoadEmptyDavisDragConstant);
                }

                if (FreightAnimations.EmptyMaxBrakeShoeForceN > 0)
                {
                    LoadEmptyMaxBrakeForceN = FreightAnimations.EmptyMaxBrakeShoeForceN;
                }
                else if (FreightAnimations.EmptyMaxBrakeForceN > 0)
                {
                    LoadEmptyMaxBrakeForceN = FreightAnimations.EmptyMaxBrakeForceN;
                }
                else
                {
                    LoadEmptyMaxBrakeForceN = MaxBrakeForceN;
                }

                if (FreightAnimations.EmptyMaxHandbrakeForceN > 0)
                {
                    LoadEmptyMaxHandbrakeForceN = FreightAnimations.EmptyMaxHandbrakeForceN;
                }
                else
                {
                    LoadEmptyMaxHandbrakeForceN = MaxHandbrakeForceN;
                }

                if (FreightAnimations.EmptyCentreOfGravityM_Y > 0)
                {
                    LoadEmptyCentreOfGravityM_Y = FreightAnimations.EmptyCentreOfGravityM_Y;
                }
                else
                {
                    LoadEmptyCentreOfGravityM_Y = CentreOfGravityM.Y;
                }

                if (FreightAnimations.EmptyRelayValveRatio > 0)
                {
                    LoadEmptyRelayValveRatio = FreightAnimations.EmptyRelayValveRatio;
                }
                else if (BrakeSystem is AirSinglePipe brakes)
                {
                    LoadEmptyRelayValveRatio = brakes.RelayValveRatio;
                }

                if (FreightAnimations.EmptyInshotPSI != 0)
                {
                    LoadEmptyInshotPSI = FreightAnimations.EmptyInshotPSI;
                }
                else if (BrakeSystem is AirSinglePipe brakes)
                {
                    LoadEmptyInshotPSI = brakes.RelayValveInshotPSI;
                }

                // Read (initialise) Static load ones if a static load
                // Test each value to make sure that it has been defined in the WAG file, if not default to Root WAG file value
                if (FreightAnimations.FullPhysicsStaticOne != null)
                {
                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_A > 0)
                    {
                        LoadFullORTSDavis_A = FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_A;
                    }
                    else if (BearingType == BearingTypes.Default)
                    {
                        // Use default if bearing type isn't given
                        // If bearing type is given, we will calculate the Davis value later
                        LoadFullORTSDavis_A = DavisAN;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_B > 0)
                    {
                        LoadFullORTSDavis_B = FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_B;
                    }
                    else if (BearingType == BearingTypes.Default)
                    {
                        // Use default if bearing type isn't given
                        // If bearing type is given, we will calculate the Davis value later
                        LoadFullORTSDavis_B = DavisBNSpM;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavisDragConstant > 0)
                    {
                        LoadFullDavisDragConstant = FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavisDragConstant;
                    }
                    else
                    {
                        LoadFullDavisDragConstant = DavisDragConstant;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticORTSWagonFrontalAreaM2 > 0)
                    {
                        LoadFullWagonFrontalAreaM2 = FreightAnimations.FullPhysicsStaticOne.FullStaticORTSWagonFrontalAreaM2;
                    }
                    else
                    {
                        LoadFullWagonFrontalAreaM2 = WagonFrontalAreaM2;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_C > 0)
                    {
                        LoadFullORTSDavis_C = FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_C;
                    }
                    else
                    {
                        LoadFullORTSDavis_C = NSSpMM.FromLbfpMpH2(Me2.ToFt2(LoadFullWagonFrontalAreaM2) * LoadFullDavisDragConstant);
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticMaxBrakeShoeForceN > 0)
                    {
                        LoadFullMaxBrakeForceN = FreightAnimations.FullPhysicsStaticOne.FullStaticMaxBrakeShoeForceN;
                    }
                    else if (FreightAnimations.FullPhysicsStaticOne.FullStaticMaxBrakeForceN > 0)
                    {
                        LoadFullMaxBrakeForceN = FreightAnimations.FullPhysicsStaticOne.FullStaticMaxBrakeForceN;
                    }
                    else
                    {
                        LoadFullMaxBrakeForceN = MaxBrakeForceN;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticMaxHandbrakeForceN > 0)
                    {
                        LoadFullMaxHandbrakeForceN = FreightAnimations.FullPhysicsStaticOne.FullStaticMaxHandbrakeForceN;
                    }
                    else
                    {
                        LoadFullMaxHandbrakeForceN = MaxHandbrakeForceN;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticCentreOfGravityM_Y > 0)
                    {
                        LoadFullCentreOfGravityM_Y = FreightAnimations.FullPhysicsStaticOne.FullStaticCentreOfGravityM_Y;
                    }
                    else
                    {
                        LoadFullCentreOfGravityM_Y = CentreOfGravityM.Y;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticRelayValveRatio > 0)
                    {
                        LoadFullRelayValveRatio = FreightAnimations.FullPhysicsStaticOne.FullStaticRelayValveRatio;
                    }
                    else if (BrakeSystem is AirSinglePipe brakes)
                    {
                        LoadFullRelayValveRatio = brakes.RelayValveRatio;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticInshotPSI > 0)
                    {
                        LoadFullInshotPSI = FreightAnimations.FullPhysicsStaticOne.FullStaticInshotPSI;
                    }
                    else if (BrakeSystem is AirSinglePipe brakes)
                    {
                        LoadFullInshotPSI = brakes.RelayValveInshotPSI;
                    }
                }

                // Read (initialise) Continuous load ones if a continuous load
                // Test each value to make sure that it has been defined in the WAG file, if not default to Root WAG file value
                if (FreightAnimations.FullPhysicsContinuousOne != null)
                {
                    if (FreightAnimations.FullPhysicsContinuousOne.FreightWeightWhenFull > 0)
                    {
                        LoadFullMassKg = FreightAnimations.WagonEmptyWeight + FreightAnimations.FullPhysicsContinuousOne.FreightWeightWhenFull;
                    }
                    else
                    {
                        LoadFullMassKg = MassKG;
                    } 

                    if (FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_A > 0)
                    {
                        LoadFullORTSDavis_A = FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_A;
                    }
                    else if (BearingType == BearingTypes.Default)
                    {
                        // Use default if bearing type isn't given
                        // If bearing type is given, we will calculate the Davis value later
                        LoadFullORTSDavis_A = DavisAN;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_B > 0)
                    {
                        LoadFullORTSDavis_B = FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_B;
                    }
                    else if (BearingType == BearingTypes.Default)
                    {
                        // Use default if bearing type isn't given
                        // If bearing type is given, we will calculate the Davis value later
                        LoadFullORTSDavis_B = DavisBNSpM;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullORTSDavisDragConstant > 0)
                    {
                        LoadFullDavisDragConstant = FreightAnimations.FullPhysicsContinuousOne.FullORTSDavisDragConstant;
                    }
                    else
                    {
                        LoadFullDavisDragConstant = DavisDragConstant;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullORTSWagonFrontalAreaM2 > 0)
                    {
                        LoadFullWagonFrontalAreaM2 = FreightAnimations.FullPhysicsContinuousOne.FullORTSWagonFrontalAreaM2;
                    }
                    else
                    {
                        LoadFullWagonFrontalAreaM2 = WagonFrontalAreaM2;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_C > 0)
                    {
                        LoadFullORTSDavis_C = FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_C;
                    }
                    else
                    {
                        LoadFullORTSDavis_C = NSSpMM.FromLbfpMpH2(Me2.ToFt2(LoadFullWagonFrontalAreaM2) * LoadFullDavisDragConstant);
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullMaxBrakeShoeForceN > 0)
                    {
                        LoadFullMaxBrakeForceN = FreightAnimations.FullPhysicsContinuousOne.FullMaxBrakeShoeForceN;
                    }
                    else if (FreightAnimations.FullPhysicsContinuousOne.FullMaxBrakeForceN > 0)
                    {
                        LoadFullMaxBrakeForceN = FreightAnimations.FullPhysicsContinuousOne.FullMaxBrakeForceN;
                    }
                    else
                    {
                        LoadFullMaxBrakeForceN = MaxBrakeForceN;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullMaxHandbrakeForceN > 0)
                    {
                        LoadFullMaxHandbrakeForceN = FreightAnimations.FullPhysicsContinuousOne.FullMaxHandbrakeForceN;
                    }
                    else
                    {
                        LoadFullMaxHandbrakeForceN = MaxHandbrakeForceN;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullCentreOfGravityM_Y > 0)
                    {
                        LoadFullCentreOfGravityM_Y = FreightAnimations.FullPhysicsContinuousOne.FullCentreOfGravityM_Y;
                    }
                    else
                    {
                        LoadFullCentreOfGravityM_Y = CentreOfGravityM.Y;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullRelayValveRatio > 0)
                    {
                        LoadFullRelayValveRatio = FreightAnimations.FullPhysicsContinuousOne.FullRelayValveRatio;
                    }
                    else if (BrakeSystem is AirSinglePipe brakes)
                    {
                        LoadFullRelayValveRatio = brakes.RelayValveRatio;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullInshotPSI != 0)
                    {
                        LoadFullInshotPSI = FreightAnimations.FullPhysicsContinuousOne.FullInshotPSI;
                    }
                    else if (BrakeSystem is AirSinglePipe brakes)
                    {
                        LoadFullInshotPSI = brakes.RelayValveInshotPSI;
                    }
                }

                if (!FreightAnimations.MSTSFreightAnimEnabled) FreightShapeFileName = null;
                if (FreightAnimations.WagonEmptyWeight != -1)
                {
                    // Computes mass when it carries containers
                    float totalContainerMassKG = 0;
                    if (FreightAnimations.Animations != null)
                    {
                        foreach (var anim in FreightAnimations.Animations)
                            if (anim is FreightAnimationDiscrete discreteAnim && discreteAnim.Container != null)
                            {
                                totalContainerMassKG += discreteAnim.Container.MassKG;
                            }
                    }
                    CalculateTotalMass(totalContainerMassKG);

                    // If Davis values are still missing, calculate them
                    if (LoadEmptyORTSDavis_A <= 0 && BearingType != BearingTypes.Default)
                        LoadEmptyORTSDavis_A = CalcDavisAValue(BearingType, LoadEmptyMassKg, (WagonNumAxles + LocoNumDrvAxles));
                    if (LoadEmptyORTSDavis_B <= 0 && BearingType != BearingTypes.Default)
                        LoadEmptyORTSDavis_B = CalcDavisBValue(BearingType, LoadEmptyMassKg, (WagonNumAxles + LocoNumDrvAxles), WagonType);
                    if (LoadFullORTSDavis_A <= 0 && BearingType != BearingTypes.Default)
                        LoadFullORTSDavis_A = CalcDavisAValue(BearingType, MassKG, (WagonNumAxles + LocoNumDrvAxles));
                    if (LoadFullORTSDavis_B <= 0 && BearingType != BearingTypes.Default)
                        LoadFullORTSDavis_B = CalcDavisBValue(BearingType, MassKG, (WagonNumAxles + LocoNumDrvAxles), WagonType);

                    if (FreightAnimations.StaticFreightAnimationsPresent) // If it is static freight animation, set wagon physics to full wagon value
                    {
                        // Update brake parameters   
                        MaxBrakeForceN = LoadFullMaxBrakeForceN;
                        MaxHandbrakeForceN = LoadFullMaxHandbrakeForceN;
                        if (BrakeSystem is AirSinglePipe brakes)
                        {
                            brakes.RelayValveRatio = LoadFullRelayValveRatio;
                            brakes.RelayValveInshotPSI = LoadFullInshotPSI;
                        }

                        // Update friction related parameters
                        DavisAN = LoadFullORTSDavis_A;
                        DavisBNSpM = LoadFullORTSDavis_B;
                        DavisCNSSpMM = LoadFullORTSDavis_C;
                        DavisDragConstant = LoadFullDavisDragConstant;
                        WagonFrontalAreaM2 = LoadFullWagonFrontalAreaM2;

                        // Update CoG related parameters
                        CentreOfGravityM.Y = LoadFullCentreOfGravityM_Y;

                    }

                }
                if (FreightAnimations.LoadedOne != null) // If it is a Continuouos freight animation, set freight wagon parameters to FullatStart
                {
                    WeightLoadController.CurrentValue = FreightAnimations.LoadedOne.LoadPerCent / 100;

                    // Update wagon parameters sensitive to wagon mass change
                    // Calculate the difference ratio, ie how full the wagon is. This value allows the relevant value to be scaled from the empty mass to the full mass of the wagon
                    TempMassDiffRatio = WeightLoadController.CurrentValue;
                    // Update brake parameters
                    MaxBrakeForceN = ((LoadFullMaxBrakeForceN - LoadEmptyMaxBrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxBrakeForceN;
                    MaxHandbrakeForceN = ((LoadFullMaxHandbrakeForceN - LoadEmptyMaxHandbrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxHandbrakeForceN;
                    // Not sensible to vary the relay valve ratio continouously; instead, it changes to loaded if more than 25% cargo is present
                    if (BrakeSystem is AirSinglePipe brakes)
                    {
                        brakes.RelayValveRatio = TempMassDiffRatio > 0.25f ? LoadFullRelayValveRatio : LoadEmptyRelayValveRatio;
                        brakes.RelayValveInshotPSI = TempMassDiffRatio > 0.25f ? LoadFullInshotPSI : LoadEmptyInshotPSI;
                    }

                    // Update friction related parameters
                    DavisAN = ((LoadFullORTSDavis_A - LoadEmptyORTSDavis_A) * TempMassDiffRatio) + LoadEmptyORTSDavis_A;
                    DavisBNSpM = ((LoadFullORTSDavis_B - LoadEmptyORTSDavis_B) * TempMassDiffRatio) + LoadEmptyORTSDavis_B;
                    DavisCNSSpMM = ((LoadFullORTSDavis_C - LoadEmptyORTSDavis_C) * TempMassDiffRatio) + LoadEmptyORTSDavis_C;

                    if (LoadEmptyDavisDragConstant > LoadFullDavisDragConstant ) // Due to wind turbulence empty drag might be higher then loaded drag, and therefore both scenarios need to be covered.
                    {
                        DavisDragConstant = LoadEmptyDavisDragConstant -   ((LoadEmptyDavisDragConstant - LoadFullDavisDragConstant) * TempMassDiffRatio);
                    }
                    else
                    {
                        DavisDragConstant = ((LoadFullDavisDragConstant - LoadEmptyDavisDragConstant) * TempMassDiffRatio) + LoadEmptyDavisDragConstant;
                    }
                    
                    WagonFrontalAreaM2 = ((LoadFullWagonFrontalAreaM2 - LoadEmptyWagonFrontalAreaM2) * TempMassDiffRatio) + LoadEmptyWagonFrontalAreaM2;

                    // Update CoG related parameters
                    CentreOfGravityM.Y = ((LoadFullCentreOfGravityM_Y - LoadEmptyCentreOfGravityM_Y) * TempMassDiffRatio) + LoadEmptyCentreOfGravityM_Y;
                }
                else  // If Freight animation is Continuous and freight is not loaded then set initial values to the empty wagon values
                {
                    if (FreightAnimations.ContinuousFreightAnimationsPresent)
                    {
                        // If it is an empty continuous freight animation, set wagon physics to empty wagon value
                        // Update brake physics
                        MaxBrakeForceN = LoadEmptyMaxBrakeForceN;
                        MaxHandbrakeForceN = LoadEmptyMaxHandbrakeForceN;
                        if (BrakeSystem is AirSinglePipe brakes)
                        {
                            brakes.RelayValveRatio = LoadEmptyRelayValveRatio;
                            brakes.RelayValveInshotPSI = LoadEmptyInshotPSI;
                        }

                        // Update friction related parameters
                        DavisAN = LoadEmptyORTSDavis_A;
                        DavisBNSpM = LoadEmptyORTSDavis_B;
                        DavisCNSSpMM = LoadEmptyORTSDavis_C;

                        // Update CoG related parameters
                        CentreOfGravityM.Y = LoadEmptyCentreOfGravityM_Y;
                    }
                }

#if DEBUG_VARIABLE_MASS

                Trace.TraceInformation(" ===============================  Variable Load Initialisation (MSTSWagon.cs) ===============================");

                Trace.TraceInformation("CarID {0}", CarID);
                Trace.TraceInformation("Initial Values = Brake {0} Handbrake {1} CoGY {2} Mass {3}", InitialMaxBrakeForceN, InitialMaxHandbrakeForceN, InitialCentreOfGravityM.Y, InitialMassKG);
                Trace.TraceInformation("Empty Values = Brake {0} Handbrake {1} DavisA {2} DavisB {3} DavisC {4} CoGY {5}", LoadEmptyMaxBrakeForceN, LoadEmptyMaxHandbrakeForceN, LoadEmptyORTSDavis_A, LoadEmptyORTSDavis_B, LoadEmptyORTSDavis_C, LoadEmptyCentreOfGravityM_Y);
                Trace.TraceInformation("Full Values = Brake {0} Handbrake {1} DavisA {2} DavisB {3} DavisC {4} CoGY {5}", LoadFullMaxBrakeForceN, LoadFullMaxHandbrakeForceN, LoadFullORTSDavis_A, LoadFullORTSDavis_B, LoadFullORTSDavis_C, LoadFullCentreOfGravityM_Y);
#endif
            }

            // Determine whether or not to use the Davis friction model. Must come after freight animations are initialized.
            IsDavisFriction = DavisAN != 0 && DavisBNSpM != 0 && DavisCNSSpMM != 0;

            if (BrakeSystem == null)
                BrakeSystem = MSTSBrakeSystem.Create(CarBrakeSystemType, this);

            if (TrackGaugeM <= 0) // Use gauge of route/sim settings if gauge wasn't defined
                TrackGaugeM = Simulator.RouteTrackGaugeM;
        }

        // Compute total mass of wagon including freight animations and variable loads like containers
        public void CalculateTotalMass(float totalContainerMassKG)
        {
            MassKG = FreightAnimations.WagonEmptyWeight + FreightAnimations.FreightWeight + FreightAnimations.StaticFreightWeight + totalContainerMassKG;
        }

        public void GetMeasurementUnits()
        {
            IsMetric = Simulator.Settings.Units == "Metric" || (Simulator.Settings.Units == "Automatic" && System.Globalization.RegionInfo.CurrentRegion.IsMetric) ||
                (Simulator.Settings.Units == "Route" && Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric);
            IsUK = Simulator.Settings.Units == "UK";
        }

        public override void Initialize(bool reinitialize = false)
        {
            Pantographs.Initialize();
            Doors.Initialize();
            PassengerCarPowerSupply?.Initialize();
            LocomotiveAxles.Initialize();

            base.Initialize(reinitialize);
                       
            if (MaxUnbalancedSuperElevationM == 0 || MaxUnbalancedSuperElevationM > 0.5) // If MaxUnbalancedSuperElevationM > 18", or equal to zero, then set a default value
            {
                switch (WagonType)
                {
                    case WagonTypes.Freight:
                        MaxUnbalancedSuperElevationM = Me.FromIn(3.0f);  // Unbalanced superelevation has a maximum default value of 3"
                        break;
                    case WagonTypes.Passenger:
                        MaxUnbalancedSuperElevationM = Me.FromIn(3.0f);  // Unbalanced superelevation has a maximum default value of 3"
                        break;
                    case WagonTypes.Engine:
                        MaxUnbalancedSuperElevationM = Me.FromIn(6.0f);  // Unbalanced superelevation has a maximum default value of 6"
                        break;
                    case WagonTypes.Tender:
                        MaxUnbalancedSuperElevationM = Me.FromIn(6.0f);  // Unbalanced superelevation has a maximum default value of 6"
                        break;
                    default:
                        MaxUnbalancedSuperElevationM = Me.FromIn(0.01f);  // if no value in wag file or is outside of bounds then set to a default value
                        break;
                }
            }
            FreightAnimations?.Load(FreightAnimations.LoadDataList, true);
            InitializeLoadPhysics();
            if (!(this is MSTSLocomotive) && Simulator.Settings.ElectricHotStart) Pantographs.HandleEvent(PowerSupplyEvent.RaisePantograph, 1);
        }

        public override void InitializeMoving()
        {
            base.InitializeMoving();
            PassengerCarPowerSupply?.InitializeMoving();
            LocomotiveAxles.InitializeMoving();
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
                case "wagon(ortswagonspecialtype":
                    stf.MustMatch("(");
                    var wagonspecialType = stf.ReadString();
                    try
                    {
                        WagonSpecialType = (WagonSpecialTypes)Enum.Parse(typeof(WagonSpecialTypes), wagonspecialType);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown engine type " + wagonspecialType);
                    }
                    break;
                case "wagon(freightanim":
                    stf.MustMatch("(");
                    FreightShapeFileName = stf.ReadString();
                    FreightAnimMaxLevelM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FreightAnimMinLevelM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    // Flags are optional and we want to avoid a warning.
                    if (!stf.EndOfBlock())
                    {
                        // TODO: The variable name (Flag), data type (Float), and unit (Distance) don't make sense here.
                        FreightAnimFlag = stf.ReadFloat(STFReader.UNITS.Distance, 1.0f);
                        stf.SkipRestOfBlock();
                    }
                    break;
                case "wagon(size":
                    stf.MustMatch("(");
                    CarWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    CarHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    CarLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsfrontarticulation": FrontArticulation = stf.ReadIntBlock(null); break;
                case "wagon(ortsreararticulation": RearArticulation = stf.ReadIntBlock(null); break;
                case "wagon(ortslengthbogiecentre": CarBogieCentreLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortslengthcarbody": CarBodyLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortslengthairhose": CarAirHoseLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortshorizontallengthairhose": CarAirHoseHorizontalLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortslengthcouplerface": CarCouplerFaceLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortswheelflangelength": WheelFlangeLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsmaximumwheelflangeangle": MaximumWheelFlangeAngleRad = stf.ReadFloatBlock(STFReader.UNITS.Angle, null); break;
                case "wagon(ortstrackgauge":
                    stf.MustMatch("(");
                    TrackGaugeM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    // Allow for imperial feet and inches to be specified separately (not ideal - please don't copy this).
                    if (!stf.EndOfBlock())
                    {
                        TrackGaugeM += stf.ReadFloat(STFReader.UNITS.Distance, 0);
                        stf.SkipRestOfBlock();
                    }
                    break;
                case "wagon(centreofgravity":
                    stf.MustMatch("(");
                    InitialCentreOfGravityM.X = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    InitialCentreOfGravityM.Y = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    InitialCentreOfGravityM.Z = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    if (Math.Abs(InitialCentreOfGravityM.Z) > 2)
                    {
                        STFException.TraceWarning(stf, string.Format("CentreOfGravity Z set to zero because value {0} outside range -2 to +2", InitialCentreOfGravityM.Z));
                        InitialCentreOfGravityM.Z = 0;
                    }
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsunbalancedsuperelevation": MaxUnbalancedSuperElevationM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsrigidwheelbase":
                    stf.MustMatch("(");
                    RigidWheelBaseM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    // Allow for imperial feet and inches to be specified separately (not ideal - please don't copy this).
                    if (!stf.EndOfBlock())
                    {
                        RigidWheelBaseM += stf.ReadFloat(STFReader.UNITS.Distance, 0);
                        stf.SkipRestOfBlock();
                    }
                    break;
                case "wagon(ortsauxtenderwatermass": AuxTenderWaterMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(ortstenderwagonwoodmass":
                case "wagon(ortstenderwagoncoalmass": TenderWagonMaxFuelMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(ortstenderwagonfueloilvolume": TenderWagonMaxOilVolumeL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "wagon(ortstenderwagonwatermass": TenderWagonMaxWaterMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(ortsheatingwindowderatingfactor": WindowDeratingFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsheatingcompartmenttemperatureset": DesiredCompartmentTempSetpointC = stf.ReadFloatBlock(STFReader.UNITS.Temperature, null); break; 
                case "wagon(ortsheatingcompartmentpipeareafactor": CompartmentHeatingPipeAreaFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsheatingtrainpipeouterdiameter": MainSteamHeatPipeOuterDiaM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsheatingtrainpipeinnerdiameter": MainSteamHeatPipeInnerDiaM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsheatingconnectinghoseinnerdiameter": CarConnectSteamHoseInnerDiaM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsheatingconnectinghoseouterdiameter": CarConnectSteamHoseOuterDiaM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(mass": InitialMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); if (InitialMassKG < 0.1f) InitialMassKG = 0.1f; break;
                case "wagon(ortsheatingboilerwatertankcapacity": MaximumSteamHeatBoilerWaterTankCapacityL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "wagon(ortsheatingboilerfueltankcapacity": MaximiumSteamHeatBoilerFuelTankCapacityL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "wagon(ortsheatingboilerwaterusage": TrainHeatBoilerWaterUsageGalukpH = new Interpolator(stf); break;
                case "wagon(ortsheatingboilerfuelusage": TrainHeatBoilerFuelUsageGalukpH = new Interpolator(stf); break;
                case "wagon(wheelradius": WheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(wheelradius": DriverWheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(sound": MainSoundFileName = stf.ReadStringBlock(null); break;
                case "wagon(ortsbrakeshoefriction": BrakeShoeFrictionFactor = new Interpolator(stf); break;
                case "wagon(maxhandbrakeforce": InitialMaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(maxbrakeforce": InitialMaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortsmaxbrakeshoeforce": MaxBrakeShoeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortsnumbercarbrakeshoes": NumberCarBrakeShoes = stf.ReadIntBlock(null); break;
                case "wagon(ortsbrakeshoetype":
                    stf.MustMatch("(");
                    var brakeShoeType = stf.ReadString();
                    try
                    {
                        BrakeShoeType = (BrakeShoeTypes)Enum.Parse(typeof(BrakeShoeTypes), brakeShoeType);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown brake shoe type " + brakeShoeType);
                    }
                    break;

                case "wagon(ortswheelbrakeslideprotection":
                  // stf.MustMatch("(");
                    var brakeslideprotection = stf.ReadFloatBlock(STFReader.UNITS.None, null);
                    if (brakeslideprotection == 1)
                    {
                        WheelBrakeSlideProtectionFitted = true;
                    }
                    else
                    {
                        WheelBrakeSlideProtectionFitted = false;
                    }
                    break;
                case "wagon(ortswheelbrakesslideprotectionlimitdisable":
                    // stf.MustMatch("(");
                    var brakeslideprotectiondisable = stf.ReadFloatBlock(STFReader.UNITS.None, null);
                    if (brakeslideprotectiondisable == 1)
                    {
                        WheelBrakeSlideProtectionLimitDisabled = true;
                    }
                    else
                    {
                        WheelBrakeSlideProtectionLimitDisabled = false;
                    }
                    break;
                case "wagon(ortsdavis_a": DavisAN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortsdavis_b": DavisBNSpM = stf.ReadFloatBlock(STFReader.UNITS.Resistance, null); break;
                case "wagon(ortsdavis_c": DavisCNSSpMM = stf.ReadFloatBlock(STFReader.UNITS.ResistanceDavisC, null); break;
                case "wagon(ortsdavisdragconstant": DavisDragConstant = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortswagonfrontalarea": WagonFrontalAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "wagon(ortstraillocomotiveresistancefactor": TrailLocoResistanceFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsstandstillfriction": StandstillFrictionN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortsmergespeed": MergeSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, MergeSpeedMpS); break;
                case "wagon(effects(specialeffects": ParseEffects(lowercasetoken, stf); break;
                case "wagon(ortsbearingtype":
                    stf.MustMatch("(");
                    string bearingType = stf.ReadString().ToLower();
                    try
                    {
                        BearingType = (BearingTypes)Enum.Parse(typeof(BearingTypes), bearingType, true);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Unknown wheel bearing type " + bearingType);
                    }
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
                    MSTSBrakeSystem?.SetBrakeEquipment(BrakeEquipment);
                    break;
                case "wagon(brakeequipmenttype":
                    BrakeEquipment = stf.ReadStringBlock("").ToLower().Replace(" ", "").Split(',').ToList();
                    MSTSBrakeSystem?.SetBrakeEquipment(BrakeEquipment);
                    break;
                case "wagon(coupling":
                    Couplers.Add(new MSTSCoupling()); // Adds a new coupler every time "Coupler" parameters found in WAG and INC file
                    CouplerCountLocation = 0;
                    TrueCouplerCount += 1;
                    // it is possible for there to be more then two couplers per car if the coupler details are added via an INC file. Therefore the couplers need to be adjusted appropriately.
                    // Front coupler stored in slot 0, and rear coupler stored in slot 1
                    if (Couplers.Count > 2 && TrueCouplerCount == 3)  // If front coupler has been added via INC file
                    {
                        Couplers.RemoveAt(0);  // Remove old front coupler
                        CouplerCountLocation = 0;  // Write info to old front coupler location. 
                    }
                    else if (Couplers.Count > 2 && TrueCouplerCount == 4)  // If rear coupler has been added via INC file
                    {
                        Couplers.RemoveAt(1);  // Remove old rear coupler
                        CouplerCountLocation = 1;  // Write info to old rear coupler location. 
                    }
                    else
                    {
                        CouplerCountLocation = Couplers.Count - 1;  // By default write info into 0 and 1 slots as required.
                    };
                    break;

                // Used for simple or legacy coupler
                case "wagon(coupling(spring(break":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetSimpleBreak(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(r0":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetSimpleR0(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(spring(stiffness":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetSimpleStiffness(stf.ReadFloat(STFReader.UNITS.Stiffness, null), stf.ReadFloat(STFReader.UNITS.Stiffness, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(ortsslack":
                    stf.MustMatch("(");
                    // IsAdvancedCoupler = true; // If this parameter is present in WAG file then treat coupler as advanced ones.  Temporarily disabled for v1.3 release
                    Couplers[CouplerCountLocation].SetSlack(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;

                // Used for advanced coupler

                case "wagon(coupling(frontcoupleranim":
                    stf.MustMatch("(");
                    FrontCoupler.Closed.ShapeFileName = stf.ReadString();
                    FrontCoupler.Size = stf.ReadVector3(STFReader.UNITS.Distance, Vector3.Zero);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(frontairhoseanim":
                    stf.MustMatch("(");
                    FrontAirHose.Connected.ShapeFileName = stf.ReadString();
                    FrontAirHose.Size = stf.ReadVector3(STFReader.UNITS.Distance, Vector3.Zero);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(rearcoupleranim":
                    stf.MustMatch("(");
                    RearCoupler.Closed.ShapeFileName = stf.ReadString();
                    RearCoupler.Size = stf.ReadVector3(STFReader.UNITS.Distance, Vector3.Zero);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(rearairhoseanim":
                    stf.MustMatch("(");
                    RearAirHose.Connected.ShapeFileName = stf.ReadString();
                    RearAirHose.Size = stf.ReadVector3(STFReader.UNITS.Distance, Vector3.Zero);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(spring(ortstensionstiffness":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetTensionStiffness(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;

               case "wagon(coupling(frontcoupleropenanim":
                    stf.MustMatch("(");
                    FrontCoupler.Open.ShapeFileName = stf.ReadString();
                    // NOTE: Skip reading the size as it is unused: stf.ReadVector3(STFReader.UNITS.Distance, Vector3.Zero);
                    stf.SkipRestOfBlock();
                    break;
                    
               case "wagon(coupling(rearcoupleropenanim":
                    stf.MustMatch("(");
                    RearCoupler.Open.ShapeFileName = stf.ReadString();
                    // NOTE: Skip reading the size as it is unused: stf.ReadVector3(STFReader.UNITS.Distance, Vector3.Zero);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(frontairhosediconnectedanim":
                    stf.MustMatch("(");
                    FrontAirHose.Disconnected.ShapeFileName = stf.ReadString();
                    // NOTE: Skip reading the size as it is unused: stf.ReadVector3(STFReader.UNITS.Distance, Vector3.Zero);
                    stf.SkipRestOfBlock();
                    break;
                    
                case "wagon(coupling(rearairhosediconnectedanim":
                    stf.MustMatch("(");
                    RearAirHose.Disconnected.ShapeFileName = stf.ReadString();
                    // NOTE: Skip reading the size as it is unused: stf.ReadVector3(STFReader.UNITS.Distance, Vector3.Zero);
                    stf.SkipRestOfBlock();
                    break;


                case "wagon(coupling(spring(ortscompressionstiffness":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetCompressionStiffness(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(spring(ortstensionslack":
                    stf.MustMatch("(");
                    IsAdvancedCoupler = true; // If this parameter is present in WAG file then treat coupler as advanced ones.
                    Couplers[CouplerCountLocation].SetTensionSlack(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;
               case "wagon(coupling(spring(ortscompressionslack":
                    stf.MustMatch("(");
                    IsAdvancedCoupler = true; // If this parameter is present in WAG file then treat coupler as advanced ones.
                    Couplers[CouplerCountLocation].SetCompressionSlack(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;
                // This is for the advanced coupler and is designed to be used instead of the MSTS parameter Break

                case "wagon(coupling(spring(ortsbreak":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetAdvancedBreak(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;
                    
                    // This is for the advanced coupler and is designed to be used instead of the MSTS parameter R0
               case "wagon(coupling(spring(ortstensionr0":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetTensionR0(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;
               case "wagon(coupling(spring(ortscompressionr0":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetCompressionR0(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;

                // Used for both coupler types
                case "wagon(coupling(couplinghasrigidconnection":
                    Couplers[CouplerCountLocation].Rigid = false;
                    Couplers[CouplerCountLocation].Rigid = stf.ReadBoolBlock(true);
                    break;

                case "wagon(brakingcogwheelfitted":
                    BrakeCogWheelFitted = stf.ReadBoolBlock(false);
                    break;

                case "wagon(adheasion":
                    stf.MustMatch("(");
                    Adhesion1 = stf.ReadFloat(STFReader.UNITS.None, null);
                    Adhesion2 = stf.ReadFloat(STFReader.UNITS.None, null);
                    Adhesion3 = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortscurtius_kniffler":
                case "wagon(ortsadhesion(ortscurtius_kniffler":
                    //e.g. Wagon ( ORTSAdhesion ( ORTSCurtius_Kniffler ( 7.5 44 0.161 0.7 ) ) )
                    stf.MustMatch("(");
                    Curtius_KnifflerA = stf.ReadFloat(STFReader.UNITS.None, 7.5f); if (Curtius_KnifflerA <= 0) Curtius_KnifflerA = 7.5f;
                    Curtius_KnifflerB = stf.ReadFloat(STFReader.UNITS.None, 44.0f); if (Curtius_KnifflerB <= 0) Curtius_KnifflerB = 44.0f;
                    Curtius_KnifflerC = stf.ReadFloat(STFReader.UNITS.None, 0.161f); if (Curtius_KnifflerC <= 0) Curtius_KnifflerC = 0.161f;
                    AdhesionK = stf.ReadFloat(STFReader.UNITS.None, 0.7f); if (AdhesionK <= 0) AdhesionK = 0.7f;
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsslipwarningthreshold":
                case "wagon(ortsadhesion(ortsslipwarningthreshold":
                    stf.MustMatch("(");
                    SlipWarningThresholdPercent = stf.ReadFloat(STFReader.UNITS.None, 70.0f); if (SlipWarningThresholdPercent <= 0) SlipWarningThresholdPercent = 70.0f;
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(wheelset":
                    LocomotiveAxles.Parse(lowercasetoken, stf);
                    break;
                case "wagon(lights":
                    Lights = new LightCollection(stf);
                    break;
                case "wagon(inside": HasInsideView = true; ParseWagonInside(stf); break;
                case "wagon(orts3dcab": Parse3DCab(stf); break;
                case "wagon(numwheels": MSTSWagonNumWheels= stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsnumberaxles": InitWagonNumAxles = stf.ReadIntBlock(null); break;
                case "wagon(ortsnumberbogies": WagonNumBogies = stf.ReadIntBlock(null); break;
                case "wagon(ortspantographs":
                    Pantographs.Parse(lowercasetoken, stf);
                    break;
                case "wagon(ortsdoors(closingdelay":
                case "wagon(ortsdoors(openingdelay":
                    Doors.Parse(lowercasetoken, stf);
                    break;
                case "wagon(ortspowersupply":
                case "wagon(ortspowerondelay":
                case "wagon(ortsbattery":
                case "wagon(ortspowersupplycontinuouspower":
                case "wagon(ortspowersupplyheatingpower":
                case "wagon(ortspowersupplyairconditioningpower":
                case "wagon(ortspowersupplyairconditioningyield":
                    if (this is MSTSLocomotive)
                    {
                        Trace.TraceWarning($"Defining the {lowercasetoken} parameter is forbidden for locomotives (in {stf.FileName}:line {stf.LineNumber})");
                    }
                    else if (PassengerCarPowerSupply == null)
                    {
                        PowerSupply = new ScriptedPassengerCarPowerSupply(this);
                    }
                    PassengerCarPowerSupply?.Parse(lowercasetoken, stf);
                    break;

                case "wagon(intakepoint": IntakePointList.Add(new IntakePoint(stf)); break;
                case "wagon(passengercapacity": HasPassengerCapacity = true; break;
                case "wagon(ortsfreightanims":
                    FreightAnimations = new FreightAnimations(stf, this);
                    break;
                case "wagon(ortsexternalsoundpassedthroughpercent": ExternalSoundPassThruPercent = stf.ReadFloatBlock(STFReader.UNITS.None, -1); break;
                case "wagon(ortstracksoundpassedthroughpercent": TrackSoundPassThruPercent = stf.ReadFloatBlock(STFReader.UNITS.None, -1); break;
                case "wagon(ortsalternatepassengerviewpoints": // accepted only if there is already a passenger viewpoint
                    if (HasInsideView)
                    {
                        ParseAlternatePassengerViewPoints(stf);
                    }
                    else stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsalternate3dcabviewpoints": // accepted only if there is already a 3D cabview
                    if (Cab3DShapeFileName != null)
                    {
                        ParseAlternate3DCabViewPoints(stf);
                    }
                    else stf.SkipRestOfBlock();
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
            WagonSpecialType = copy.WagonSpecialType;
            BrakeShoeType = copy.BrakeShoeType;
            FreightShapeFileName = copy.FreightShapeFileName;
            FreightAnimMaxLevelM = copy.FreightAnimMaxLevelM;
            FreightAnimMinLevelM = copy.FreightAnimMinLevelM;
            FreightAnimFlag = copy.FreightAnimFlag;
            FrontCoupler = copy.FrontCoupler;
            RearCoupler = copy.RearCoupler;
            FrontAirHose = copy.FrontAirHose;
            RearAirHose = copy.RearAirHose;

            CarWidthM = copy.CarWidthM;
            CarHeightM = copy.CarHeightM;
            CarLengthM = copy.CarLengthM;
            FrontArticulation = copy.FrontArticulation;
            RearArticulation = copy.RearArticulation;
            TrackGaugeM = copy.TrackGaugeM;
            CentreOfGravityM = copy.CentreOfGravityM;
            InitialCentreOfGravityM = copy.InitialCentreOfGravityM;
            MaxUnbalancedSuperElevationM = copy.MaxUnbalancedSuperElevationM;
            RigidWheelBaseM = copy.RigidWheelBaseM;
            CarBogieCentreLengthM = copy.CarBogieCentreLengthM;
            CarBodyLengthM = copy.CarBodyLengthM;
            CarCouplerFaceLengthM = copy.CarCouplerFaceLengthM;
            CarAirHoseLengthM = copy.CarAirHoseLengthM;
            CarAirHoseHorizontalLengthM = copy.CarAirHoseHorizontalLengthM;
            MaximumWheelFlangeAngleRad = copy.MaximumWheelFlangeAngleRad;
            WheelFlangeLengthM = copy.WheelFlangeLengthM;
            AuxTenderWaterMassKG = copy.AuxTenderWaterMassKG;
            TenderWagonMaxFuelMassKG = copy.TenderWagonMaxFuelMassKG;
            TenderWagonMaxOilVolumeL = copy.TenderWagonMaxOilMassL;
            TenderWagonMaxWaterMassKG = copy.TenderWagonMaxWaterMassKG;
            InitWagonNumAxles = copy.InitWagonNumAxles;
            WagonNumAxles = copy.WagonNumAxles;
            DerailmentCoefficientEnabled = copy.DerailmentCoefficientEnabled;
            WagonNumBogies = copy.WagonNumBogies;
            MSTSWagonNumWheels = copy.MSTSWagonNumWheels;
            MassKG = copy.MassKG;
            InitialMassKG = copy.InitialMassKG;
            WheelRadiusM = copy.WheelRadiusM;
            DriverWheelRadiusM = copy.DriverWheelRadiusM;
            MainSoundFileName = copy.MainSoundFileName;
            BrakeShoeFrictionFactor = copy.BrakeShoeFrictionFactor;
            WheelBrakeSlideProtectionFitted = copy.WheelBrakeSlideProtectionFitted;
            WheelBrakeSlideProtectionLimitDisabled = copy.WheelBrakeSlideProtectionLimitDisabled;
            InitialMaxBrakeForceN = copy.InitialMaxBrakeForceN;
            InitialMaxHandbrakeForceN = copy.InitialMaxHandbrakeForceN;
            MaxBrakeForceN = copy.MaxBrakeForceN;
            MaxBrakeShoeForceN = copy.MaxBrakeShoeForceN;
            NumberCarBrakeShoes = copy.NumberCarBrakeShoes;
            BrakeCogWheelFitted = copy.BrakeCogWheelFitted;
            MaxHandbrakeForceN = copy.MaxHandbrakeForceN;
            FrictionBrakeBlendingMaxForceN = copy.FrictionBrakeBlendingMaxForceN;
            WindowDeratingFactor = copy.WindowDeratingFactor;
            DesiredCompartmentTempSetpointC = copy.DesiredCompartmentTempSetpointC;
            CompartmentHeatingPipeAreaFactor = copy.CompartmentHeatingPipeAreaFactor;
            MainSteamHeatPipeOuterDiaM = copy.MainSteamHeatPipeOuterDiaM;
            MainSteamHeatPipeInnerDiaM = copy.MainSteamHeatPipeInnerDiaM;
            CarConnectSteamHoseInnerDiaM = copy.CarConnectSteamHoseInnerDiaM;
            CarConnectSteamHoseOuterDiaM = copy.CarConnectSteamHoseOuterDiaM;
            MaximumSteamHeatBoilerWaterTankCapacityL = copy.MaximumSteamHeatBoilerWaterTankCapacityL;
            MaximiumSteamHeatBoilerFuelTankCapacityL = copy.MaximiumSteamHeatBoilerFuelTankCapacityL;
            TrainHeatBoilerWaterUsageGalukpH = new Interpolator(copy.TrainHeatBoilerWaterUsageGalukpH);
            TrainHeatBoilerFuelUsageGalukpH = new Interpolator(copy.TrainHeatBoilerFuelUsageGalukpH);
            DavisAN = copy.DavisAN;
            DavisBNSpM = copy.DavisBNSpM;
            DavisCNSSpMM = copy.DavisCNSSpMM;
            DavisDragConstant = copy.DavisDragConstant;
            WagonFrontalAreaM2 = copy.WagonFrontalAreaM2;
            TrailLocoResistanceFactor = copy.TrailLocoResistanceFactor;
            FrictionC1 = copy.FrictionC1;
            FrictionE1 = copy.FrictionE1;
            FrictionV2 = copy.FrictionV2;
            FrictionC2 = copy.FrictionC2;
            FrictionE2 = copy.FrictionE2;
            EffectData = copy.EffectData;
            IsBelowMergeSpeed = copy.IsBelowMergeSpeed;
            StandstillFrictionN = copy.StandstillFrictionN;
            MergeSpeedFrictionN = copy.MergeSpeedFrictionN;
            MergeSpeedMpS = copy.MergeSpeedMpS;
            IsDavisFriction = copy.IsDavisFriction;
            BearingType = copy.BearingType;
            CarBrakeSystemType = copy.CarBrakeSystemType;
            BrakeSystem = MSTSBrakeSystem.Create(CarBrakeSystemType, this);
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
            SlipWarningThresholdPercent = copy.SlipWarningThresholdPercent;
            Lights = copy.Lights;
            HasInsideView = copy.HasInsideView;
            ExternalSoundPassThruPercent = copy.ExternalSoundPassThruPercent;
            TrackSoundPassThruPercent = copy.TrackSoundPassThruPercent;
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
            IsAdvancedCoupler = copy.IsAdvancedCoupler;
            foreach (MSTSCoupling coupler in copy.Couplers)
                Couplers.Add(coupler);
            Pantographs.Copy(copy.Pantographs);
            Doors.Copy(copy.Doors);
            if (copy.FreightAnimations != null)
            {
                FreightAnimations = new FreightAnimations(copy.FreightAnimations, this);
            }

            LoadEmptyMassKg = copy.LoadEmptyMassKg;
            LoadEmptyCentreOfGravityM_Y = copy.LoadEmptyCentreOfGravityM_Y;
            LoadEmptyMaxBrakeForceN = copy.LoadEmptyMaxBrakeForceN;
            LoadEmptyMaxHandbrakeForceN = copy.LoadEmptyMaxHandbrakeForceN;
            LoadEmptyORTSDavis_A = copy.LoadEmptyORTSDavis_A;
            LoadEmptyORTSDavis_B = copy.LoadEmptyORTSDavis_B;
            LoadEmptyORTSDavis_C = copy.LoadEmptyORTSDavis_C;
            LoadEmptyDavisDragConstant = copy.LoadEmptyDavisDragConstant;
            LoadEmptyWagonFrontalAreaM2 = copy.LoadEmptyWagonFrontalAreaM2;
            LoadEmptyRelayValveRatio = copy.LoadEmptyRelayValveRatio;
            LoadEmptyInshotPSI = copy.LoadEmptyInshotPSI;
            LoadFullMassKg = copy.LoadFullMassKg;
            LoadFullCentreOfGravityM_Y = copy.LoadFullCentreOfGravityM_Y;
            LoadFullMaxBrakeForceN = copy.LoadFullMaxBrakeForceN;
            LoadFullMaxHandbrakeForceN = copy.LoadFullMaxHandbrakeForceN;
            LoadFullORTSDavis_A = copy.LoadFullORTSDavis_A;
            LoadFullORTSDavis_B = copy.LoadFullORTSDavis_B;
            LoadFullORTSDavis_C = copy.LoadFullORTSDavis_C;
            LoadFullDavisDragConstant = copy.LoadFullDavisDragConstant;
            LoadFullWagonFrontalAreaM2 = copy.LoadFullWagonFrontalAreaM2;
            LoadFullRelayValveRatio = copy.LoadFullRelayValveRatio;
            LoadFullInshotPSI = copy.LoadFullInshotPSI;

            if (copy.IntakePointList != null)
            {
                foreach (IntakePoint copyIntakePoint in copy.IntakePointList)
                {
                    // If freight animations not used or else wagon is a tender or locomotive, use the "MSTS" type IntakePoints if present in WAG / ENG file

                    if (copyIntakePoint.LinkedFreightAnim == null)
               //     if (copyIntakePoint.LinkedFreightAnim == null || WagonType == WagonTypes.Engine || WagonType == WagonTypes.Tender || AuxWagonType == "AuxiliaryTender")
                        IntakePointList.Add(new IntakePoint(copyIntakePoint));
                }
            }
            BrakeEquipment = new List<string>(BrakeEquipment);
            MSTSBrakeSystem.InitializeFromCopy(copy.BrakeSystem);
            if (copy.WeightLoadController != null) WeightLoadController = new MSTSNotchController(copy.WeightLoadController);

            if (copy.PassengerCarPowerSupply != null)
            {
                PowerSupply = new ScriptedPassengerCarPowerSupply(this);
                PassengerCarPowerSupply.Copy(copy.PassengerCarPowerSupply);
            }
            LocomotiveAxles.Copy(copy.LocomotiveAxles);
            MoveParamsToAxle();
        }

        /// <summary>
        /// We are moving parameters from locomotive to axle. 
        /// </summary>
        public void MoveParamsToAxle()
        {
            foreach (var axle in LocomotiveAxles)
            {
                axle.SlipWarningTresholdPercent = SlipWarningThresholdPercent;
                axle.AdhesionK = AdhesionK;
            }
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

        // parses additional passenger viewpoints, if any
        protected void ParseAlternatePassengerViewPoints(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("ortsalternatepassengerviewpoint", ()=>{ ParseWagonInside(stf); }),
            });
        }

        // parses additional 3Dcab viewpoints, if any
        protected void ParseAlternate3DCabViewPoints(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("ortsalternate3dcabviewpoint", ()=>{ Parse3DCab(stf); }),
            });
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
            outf.Write(Variable2_Booster);
            outf.Write(Variable3);
            outf.Write(Variable1_2);
            outf.Write(Variable1_3);
            outf.Write(Variable1_4);
            outf.Write(Friction0N);
            outf.Write(DavisAN);
            outf.Write(DavisBNSpM);
            outf.Write(DavisCNSSpMM);
            outf.Write(MergeSpeedFrictionN);
            outf.Write(IsBelowMergeSpeed);
            outf.Write(MassKG);
            outf.Write(MaxBrakeForceN);
            outf.Write(MaxHandbrakeForceN);
            outf.Write(Couplers.Count);
            foreach (MSTSCoupling coupler in Couplers)
                coupler.Save(outf);
            Pantographs.Save(outf);
            Doors.Save(outf);
            PassengerCarPowerSupply?.Save(outf);
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
            outf.Write(CurrentSteamHeatBoilerFuelCapacityL);
            outf.Write(CarInsideTempC);
            outf.Write(CurrentCarSteamHeatBoilerWaterCapacityL);

            outf.Write(WheelBrakeSlideProtectionActive);
            outf.Write(WheelBrakeSlideProtectionTimerS);
            outf.Write(AngleOfAttackmRad);
            outf.Write(DerailClimbDistanceM);
            outf.Write(DerailPossible);
            outf.Write(DerailExpected);
            outf.Write(DerailElapsedTimeS);
            for (int index = 0; index < 4; index++)
            {
                outf.Write((int)WindowStates[index]);
            }

            LocomotiveAxles.Save(outf);

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
            Variable2_Booster = inf.ReadSingle();
            Variable3 = inf.ReadSingle();
            Variable1_2 = inf.ReadSingle();
            Variable1_3 = inf.ReadSingle();
            Variable1_4 = inf.ReadSingle();
            Friction0N = inf.ReadSingle();
            DavisAN = inf.ReadSingle();
            DavisBNSpM = inf.ReadSingle();
            DavisCNSSpMM = inf.ReadSingle();
            MergeSpeedFrictionN = inf.ReadSingle();
            IsBelowMergeSpeed = inf.ReadBoolean();
            MassKG = inf.ReadSingle();
            MaxBrakeForceN = inf.ReadSingle();
            MaxHandbrakeForceN = inf.ReadSingle();
            Couplers = ReadCouplersFromSave(inf).ToList();
            Pantographs.Restore(inf);
            Doors.Restore(inf);
            PassengerCarPowerSupply?.Restore(inf);
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
            CurrentSteamHeatBoilerFuelCapacityL = inf.ReadSingle();
            CarInsideTempC = inf.ReadSingle();
            CurrentCarSteamHeatBoilerWaterCapacityL = inf.ReadSingle();

            WheelBrakeSlideProtectionActive = inf.ReadBoolean();
            WheelBrakeSlideProtectionTimerS = inf.ReadInt32();
            AngleOfAttackmRad = inf.ReadSingle();
            DerailClimbDistanceM = inf.ReadSingle();
            DerailPossible = inf.ReadBoolean();
            DerailExpected = inf.ReadBoolean();
            DerailElapsedTimeS = inf.ReadSingle();
            for (int index = 0; index < 4; index++)
            {
                WindowStates[index] = (WindowState)inf.ReadInt32();
            }

            MoveParamsToAxle();
            LocomotiveAxles.Restore(inf);

            base.Restore(inf);
        }

        /// <summary>
        /// Read the coupler state(s) from a save stream.
        /// </summary>
        /// <remarks>
        /// Has no side effects besides advancing the save stream, thus avoiding any shared-state pitfalls.
        /// </remarks>
        /// <param name="inf">The save stream.</param>
        /// <returns>A list of newly restored <see cref="MSTSCoupling"/> instances.</returns>
        private static IEnumerable<MSTSCoupling> ReadCouplersFromSave(BinaryReader inf)
        {
            var n = inf.ReadInt32();
            foreach (int _ in Enumerable.Range(0, n))
            {
                var coupler = new MSTSCoupling();
                coupler.Restore(inf);
                yield return coupler;
            }
        }

        public override void Update(float elapsedClockSeconds)
        {
            if (StaleData) // Something about the .eng/.wag/.inc data is out of date (we don't know what specifically)
            {
                // Reload the wagon, overwriting existing parameters, in order to capture updates
                Load();

                // Re-initialize the wagon to properly integrate updated data
                Initialize(true);
            }

            base.Update(elapsedClockSeconds);

            PassengerCarPowerSupply?.Update(elapsedClockSeconds);

            ConfirmSteamLocomotiveTender(); // Confirms that a tender is connected to the steam locomotive

            // Adjusts water and coal mass based upon values assigned to the tender found in the WAG file rather then those defined in ENG file.
            if (WagonType == WagonTypes.Tender && !TenderWeightInitialized && (TenderWagonMaxFuelMassKG != 0 || TenderWagonMaxOilMassL != 0) && TenderWagonMaxWaterMassKG != 0)
            {

                // Find the associated steam locomotive for this tender
                if (TendersSteamLocomotive == null) FindTendersSteamLocomotive();

                // If no locomotive is found to be associated with this tender, then OR crashes, ie TendersSteamLocomotive is still null. 
                // This message will provide the user with information to correct the problem
                if (TendersSteamLocomotive == null)
                {
                    Trace.TraceInformation("Tender @ position {0} does not have a locomotive associated with. Check that it is preceeded by a steam locomotive.", CarID);
                }

                if (TendersSteamLocomotive != null)
                {
                    if (TendersSteamLocomotive.IsTenderRequired == 1)
                    {
                        // Combined total water found by taking the current combined water (which may have extra water added via the auxiliary tender), and subtracting the 
                        // amount of water defined in the ENG file, and adding the water defined in the WAG file.
                        float TempMaxCombinedWater = TendersSteamLocomotive.MaxTotalCombinedWaterVolumeUKG;
                        TendersSteamLocomotive.MaxTotalCombinedWaterVolumeUKG = (TempMaxCombinedWater - (Kg.ToLb(TendersSteamLocomotive.MaxLocoTenderWaterMassKG) / WaterLBpUKG)) + (Kg.ToLb(TenderWagonMaxWaterMassKG) / WaterLBpUKG);
                                                
                        TendersSteamLocomotive.MaxLocoTenderWaterMassKG = TenderWagonMaxWaterMassKG;

                        if (TendersSteamLocomotive.SteamLocomotiveFuelType == MSTSSteamLocomotive.SteamLocomotiveFuelTypes.Oil)
                        {
                            TendersSteamLocomotive.MaxTenderFuelMassKG = TendersSteamLocomotive.MaxTenderOilMassL * TendersSteamLocomotive.OilSpecificGravity;
                        }
                        else
                        {
                            TendersSteamLocomotive.MaxTenderFuelMassKG = TenderWagonMaxFuelMassKG;
                        }

                        if (Simulator.Settings.VerboseConfigurationMessages)
                        {
                            Trace.TraceInformation("Fuel and Water Masses adjusted to Tender Values Specified in WAG File - Coal mass {0} kg, Water Mass {1}", FormatStrings.FormatMass(TendersSteamLocomotive.MaxTenderFuelMassKG, IsMetric), FormatStrings.FormatFuelVolume(L.FromGUK(TendersSteamLocomotive.MaxTotalCombinedWaterVolumeUKG), IsMetric, IsUK));
                        }
                    }
                }

                // Rest flag so that this loop is not executed again
                TenderWeightInitialized = true;
            }

            UpdateTenderLoad(); // Updates the load physics characteristics of tender and aux tender

            UpdateLocomotiveLoadPhysics(); // Updates the load physics characteristics of locomotives

            UpdateSpecialEffects(elapsedClockSeconds); // Updates the wagon special effects

            // Update Aux Tender Information

            // TODO: Replace AuxWagonType with new values of WagonType or similar. It's a bad idea having two fields that are nearly the same but not quite.
            if (AuxTenderWaterMassKG != 0)   // SetStreamVolume wagon type for later use
            {

                AuxWagonType = "AuxiliaryTender";
            }
            else if (AuxWagonType == "")
            {
                AuxWagonType = WagonType.ToString();
            }

#if DEBUG_AUXTENDER
            Trace.TraceInformation("***************************************** DEBUG_AUXTENDER (MSTSWagon.cs) ***************************************************************");
            Trace.TraceInformation("Car ID {0} Aux Tender Water Mass {1} Wagon Type {2}", CarID, AuxTenderWaterMassKG, AuxWagonType);
#endif

            AbsWheelSpeedMpS = Math.Abs(WheelSpeedMpS);

            UpdateTrainBaseResistance();

            UpdateWindForce();

            UpdateWheelBearingTemperature(elapsedClockSeconds);

            foreach (MSTSCoupling coupler in Couplers)
            {

                // Test to see if coupler forces have exceeded the Proof (or safety limit). Exceeding this limit will provide an indication only
                if (IsPlayerTrain)
                {
                    if (Math.Abs(CouplerForceU) > GetCouplerBreak1N() || Math.Abs(ImpulseCouplerForceUN) > GetCouplerBreak1N())  // break couplers if either static or impulse forces exceeded
                    {
                        CouplerOverloaded = true;
                    }
                    else
                    {
                        CouplerOverloaded = false;
                    }
                }
                else
                {
                    CouplerOverloaded = false;
                }

                // Test to see if coupler forces have been exceeded, and coupler has broken. Exceeding this limit will break the coupler
                if (IsPlayerTrain) // Only break couplers on player trains
                {
                    if (Math.Abs(CouplerForceU) > GetCouplerBreak2N() || Math.Abs(ImpulseCouplerForceUN) > GetCouplerBreak2N())  // break couplers if either static or impulse forces exceeded
                    {
                        CouplerExceedBreakLimit = true;

                        if (Math.Abs(CouplerForceU) > GetCouplerBreak2N())
                        {
                            Trace.TraceInformation("Coupler on CarID {0} has broken due to excessive static coupler force {1}", CarID, CouplerForceU);

                        }
                        else if (Math.Abs(ImpulseCouplerForceUN) > GetCouplerBreak2N())
                        {
                            Trace.TraceInformation("Coupler on CarID {0} has broken due to excessive impulse coupler force {1}", CarID, ImpulseCouplerForceUN);
                        }
                    }
                    else
                    {
                        CouplerExceedBreakLimit = false;
                    }
                }
                else // if not a player train then don't ever break the couplers
                {
                    CouplerExceedBreakLimit = false;
                }
            }

            Pantographs.Update(elapsedClockSeconds);

            Doors.Update(elapsedClockSeconds);

            MSTSBrakeSystem.Update(elapsedClockSeconds);

            // Updates freight load animations when defined in WAG file - Locomotive and Tender load animation are done independently in UpdateTenderLoad() & UpdateLocomotiveLoadPhysics()
            if (WeightLoadController != null && WagonType != WagonTypes.Tender && AuxWagonType != "AuxiliaryTender" && WagonType != WagonTypes.Engine)
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
                        // Update wagon parameters sensitive to wagon mass change
                        // Calculate the difference ratio, ie how full the wagon is. This value allows the relevant value to be scaled from the empty mass to the full mass of the wagon
                        TempMassDiffRatio = WeightLoadController.CurrentValue;
                        // Update brake parameters
                        MaxBrakeForceN = ((LoadFullMaxBrakeForceN - LoadEmptyMaxBrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxBrakeForceN;
                        MaxHandbrakeForceN = ((LoadFullMaxHandbrakeForceN - LoadEmptyMaxHandbrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxHandbrakeForceN;
                        // Not sensible to vary the relay valve ratio continouously; instead, it changes to loaded if more than 25% cargo is present
                        if (BrakeSystem is AirSinglePipe brakes)
                        {
                            brakes.RelayValveRatio = TempMassDiffRatio > 0.25f ? LoadFullRelayValveRatio : LoadEmptyRelayValveRatio;
                            brakes.RelayValveInshotPSI = TempMassDiffRatio > 0.25f ? LoadFullInshotPSI : LoadEmptyInshotPSI;
                        }
                        // Update friction related parameters
                        DavisAN = ((LoadFullORTSDavis_A - LoadEmptyORTSDavis_A) * TempMassDiffRatio) + LoadEmptyORTSDavis_A;
                        DavisBNSpM = ((LoadFullORTSDavis_B - LoadEmptyORTSDavis_B) * TempMassDiffRatio) + LoadEmptyORTSDavis_B;
                        DavisCNSSpMM = ((LoadFullORTSDavis_C - LoadEmptyORTSDavis_C) * TempMassDiffRatio) + LoadEmptyORTSDavis_C;

                        if (LoadEmptyDavisDragConstant > LoadFullDavisDragConstant) // Due to wind turbulence empty drag might be higher then loaded drag, and therefore both scenarios need to be covered.
                        {
                            DavisDragConstant = LoadEmptyDavisDragConstant - ((LoadEmptyDavisDragConstant - LoadFullDavisDragConstant) * TempMassDiffRatio);
                        }
                        else
                        {
                            DavisDragConstant = ((LoadFullDavisDragConstant - LoadEmptyDavisDragConstant) * TempMassDiffRatio) + LoadEmptyDavisDragConstant;
                        }

                        WagonFrontalAreaM2 = ((LoadFullWagonFrontalAreaM2 - LoadEmptyWagonFrontalAreaM2) * TempMassDiffRatio) + LoadEmptyWagonFrontalAreaM2;


                        // Update CoG related parameters
                        CentreOfGravityM.Y = ((LoadFullCentreOfGravityM_Y - LoadEmptyCentreOfGravityM_Y) * TempMassDiffRatio) + LoadEmptyCentreOfGravityM_Y;
                    }
                }
                if (WeightLoadController.UpdateValue == 0.0 && FreightAnimations.LoadedOne != null && FreightAnimations.LoadedOne.LoadPerCent == 0.0)
                {
                    FreightAnimations.LoadedOne = null;
                    FreightAnimations.FreightType = PickupType.None;
                }
                                if (WaitForAnimationReady && WeightLoadController.CommandStartTime + FreightAnimations.UnloadingStartDelay <= Simulator.ClockTime)
                {
                    WaitForAnimationReady = false;
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Starting unload"));
                    if (FreightAnimations.LoadedOne is FreightAnimationContinuous)
                        WeightLoadController.StartDecrease(WeightLoadController.MinimumValue);
                }
            }

            if (WagonType != WagonTypes.Tender && AuxWagonType != "AuxiliaryTender" && WagonType != WagonTypes.Engine)
            {
                // Updates mass when it carries containers
                float totalContainerMassKG = 0;
                if (FreightAnimations?.Animations != null)
                {
                    foreach (var anim in FreightAnimations.Animations)
                        if (anim is FreightAnimationDiscrete discreteAnim && discreteAnim.Container != null)
                        {
                            totalContainerMassKG += discreteAnim.Container.MassKG;
                        }
                }

                // Updates the mass of the wagon considering all types of loads
                if (FreightAnimations != null && FreightAnimations.WagonEmptyWeight != -1)
                {
                    CalculateTotalMass(totalContainerMassKG);
                }
            }
        }

       private void UpdateLocomotiveLoadPhysics()
        {
            // This section updates the weight and physics of the locomotive
            if (FreightAnimations != null && FreightAnimations.ContinuousFreightAnimationsPresent) // make sure that a freight animation INCLUDE File has been defined, and it contains "continuous" animation data.
            {
                if (this is MSTSSteamLocomotive)
                // If steam locomotive then water, and coal variations will impact the weight of the locomotive
                {
                    // set a process to pass relevant locomotive parameters from locomotive file to this wagon file
                    var LocoIndex = 0;
                    for (var i = 0; i < Train.Cars.Count; i++) // test each car to find where the steam locomotive is in the consist
                        if (Train.Cars[i] == this)  // If this car is a Steam locomotive then set loco index
                            LocoIndex = i;
                    if (Train.Cars[LocoIndex] is MSTSSteamLocomotive)
                        SteamLocomotiveIdentification = Train.Cars[LocoIndex] as MSTSSteamLocomotive;
                    if (SteamLocomotiveIdentification != null)
                    {
                        if (SteamLocomotiveIdentification.IsTenderRequired == 0) // Test to see if the locomotive is a tender locomotive or tank locomotive. 
                        // If = 0, then locomotive must be a tank type locomotive. A tank locomotive has the fuel (coal and water) onboard.
                        // Thus the loco weight changes as boiler level goes up and down, and coal mass varies with the fire mass. Also onboard fuel (coal and water ) will vary as used.
                        {
                            MassKG = LoadEmptyMassKg + Kg.FromLb(SteamLocomotiveIdentification.BoilerMassLB) + SteamLocomotiveIdentification.FireMassKG + SteamLocomotiveIdentification.TenderFuelMassKG + Kg.FromLb(SteamLocomotiveIdentification.CombinedTenderWaterVolumeUKG * WaterLBpUKG);
                            MassKG = MathHelper.Clamp(MassKG, LoadEmptyMassKg, LoadFullMassKg); // Clamp Mass to between the empty and full wagon values   
                            // Adjust drive wheel weight
                            SteamLocomotiveIdentification.DrvWheelWeightKg = (MassKG / InitialMassKG) * SteamLocomotiveIdentification.InitialDrvWheelWeightKg;

                            // update drive wheel weight for each multiple steam engine
                            UpdateDriveWheelWeight(LocoIndex, MassKG, SteamLocomotiveIdentification.SteamEngines.Count);

                        }
                        else // locomotive must be a tender type locomotive
                        // This is a tender locomotive. A tender locomotive does not have any fuel onboard.
                        // Thus the loco weight only changes as boiler level goes up and down, and coal mass varies in the fire
                        {
                            MassKG = LoadEmptyMassKg + Kg.FromLb(SteamLocomotiveIdentification.BoilerMassLB) + SteamLocomotiveIdentification.FireMassKG + +(SteamLocomotiveIdentification.CurrentTrackSandBoxCapacityM3 * SteamLocomotiveIdentification.SandWeightKgpM3);
                            var MassUpperLimit = LoadFullMassKg * 1.02f; // Allow full load to go slightly higher so that rounding errors do not skew results
                            MassKG = MathHelper.Clamp(MassKG, LoadEmptyMassKg, MassUpperLimit); // Clamp Mass to between the empty and full wagon values        
                                                                                                // Adjust drive wheel weight
                            SteamLocomotiveIdentification.DrvWheelWeightKg = (MassKG / InitialMassKG) * SteamLocomotiveIdentification.InitialDrvWheelWeightKg;

                            // update drive wheel weight for each multiple steam engine
                            UpdateDriveWheelWeight(LocoIndex, MassKG, SteamLocomotiveIdentification.SteamEngines.Count);

                        }          

                        // Update wagon physics parameters sensitive to wagon mass change
                        // Calculate the difference ratio, ie how full the wagon is. This value allows the relevant value to be scaled from the empty mass to the full mass of the wagon
                        float TempTenderMassDiffRatio = (MassKG - LoadEmptyMassKg) / (LoadFullMassKg - LoadEmptyMassKg);
                        // Update brake parameters
                        MaxBrakeForceN = ((LoadFullMaxBrakeForceN - LoadEmptyMaxBrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxBrakeForceN;
                        MaxHandbrakeForceN = ((LoadFullMaxHandbrakeForceN - LoadEmptyMaxHandbrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxHandbrakeForceN;
                        // Not sensible to vary the relay valve ratio continouously; instead, it changes to loaded if more than 25% cargo is present
                        if (BrakeSystem is AirSinglePipe brakes)
                        {
                            brakes.RelayValveRatio = TempMassDiffRatio > 0.25f ? LoadFullRelayValveRatio : LoadEmptyRelayValveRatio;
                            brakes.RelayValveInshotPSI = TempMassDiffRatio > 0.25f ? LoadFullInshotPSI : LoadEmptyInshotPSI;
                        }
                        // Update friction related parameters
                        DavisAN = ((LoadFullORTSDavis_A - LoadEmptyORTSDavis_A) * TempMassDiffRatio) + LoadEmptyORTSDavis_A;
                        DavisBNSpM = ((LoadFullORTSDavis_B - LoadEmptyORTSDavis_B) * TempMassDiffRatio) + LoadEmptyORTSDavis_B;
                        DavisCNSSpMM = ((LoadFullORTSDavis_C - LoadEmptyORTSDavis_C) * TempMassDiffRatio) + LoadEmptyORTSDavis_C;

                        if (LoadEmptyDavisDragConstant > LoadFullDavisDragConstant) // Due to wind turbulence empty drag might be higher then loaded drag, and therefore both scenarios need to be covered.
                        {
                            DavisDragConstant = LoadEmptyDavisDragConstant - ((LoadEmptyDavisDragConstant - LoadFullDavisDragConstant) * TempMassDiffRatio);
                        }
                        else
                        {
                            DavisDragConstant = ((LoadFullDavisDragConstant - LoadEmptyDavisDragConstant) * TempMassDiffRatio) + LoadEmptyDavisDragConstant;
                        }

                        WagonFrontalAreaM2 = ((LoadFullWagonFrontalAreaM2 - LoadEmptyWagonFrontalAreaM2) * TempMassDiffRatio) + LoadEmptyWagonFrontalAreaM2;

                        // Update CoG related parameters
                        CentreOfGravityM.Y = ((LoadFullCentreOfGravityM_Y - LoadEmptyCentreOfGravityM_Y) * TempMassDiffRatio) + LoadEmptyCentreOfGravityM_Y;
                    }
                }

                else if (this is MSTSDieselLocomotive)
                // If diesel locomotive
                {
                   // set a process to pass relevant locomotive parameters from locomotive file to this wagon file
                    var LocoIndex = 0;
                    for (var i = 0; i < Train.Cars.Count; i++) // test each car to find the where the Diesel locomotive is in the consist
                        if (Train.Cars[i] == this)  // If this car is a Diesel locomotive then set loco index
                            LocoIndex = i;
                    if (Train.Cars[LocoIndex] is MSTSDieselLocomotive)
                        DieselLocomotiveIdentification = Train.Cars[LocoIndex] as MSTSDieselLocomotive;
                    if (DieselLocomotiveIdentification != null)
                    {

                        MassKG = LoadEmptyMassKg + (DieselLocomotiveIdentification.DieselLevelL * DieselLocomotiveIdentification.DieselWeightKgpL) + DieselLocomotiveIdentification.CurrentLocomotiveSteamHeatBoilerWaterCapacityL + (DieselLocomotiveIdentification.CurrentTrackSandBoxCapacityM3 * DieselLocomotiveIdentification.SandWeightKgpM3);
                        var MassUpperLimit = LoadFullMassKg * 1.02f; // Allow full load to go slightly higher so that rounding errors do not skew results
                        MassKG = MathHelper.Clamp(MassKG, LoadEmptyMassKg, MassUpperLimit); // Clamp Mass to between the empty and full wagon values  
                        // Adjust drive wheel weight
                        DieselLocomotiveIdentification.DrvWheelWeightKg = (MassKG / InitialMassKG) * DieselLocomotiveIdentification.InitialDrvWheelWeightKg;

                        // Update wagon parameters sensitive to wagon mass change
                        // Calculate the difference ratio, ie how full the wagon is. This value allows the relevant value to be scaled from the empty mass to the full mass of the wagon
                        float TempTenderMassDiffRatio = (MassKG - LoadEmptyMassKg) / (LoadFullMassKg - LoadEmptyMassKg);
                        // Update brake parameters
                        MaxBrakeForceN = ((LoadFullMaxBrakeForceN - LoadEmptyMaxBrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxBrakeForceN;
                        MaxHandbrakeForceN = ((LoadFullMaxHandbrakeForceN - LoadEmptyMaxHandbrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxHandbrakeForceN;
                        // Not sensible to vary the relay valve ratio continouously; instead, it changes to loaded if more than 25% cargo is present
                        if (BrakeSystem is AirSinglePipe brakes)
                        {
                            brakes.RelayValveRatio = TempMassDiffRatio > 0.25f ? LoadFullRelayValveRatio : LoadEmptyRelayValveRatio;
                            brakes.RelayValveInshotPSI = TempMassDiffRatio > 0.25f ? LoadFullInshotPSI : LoadEmptyInshotPSI;
                        }
                        // Update friction related parameters
                        DavisAN = ((LoadFullORTSDavis_A - LoadEmptyORTSDavis_A) * TempMassDiffRatio) + LoadEmptyORTSDavis_A;
                        DavisBNSpM = ((LoadFullORTSDavis_B - LoadEmptyORTSDavis_B) * TempMassDiffRatio) + LoadEmptyORTSDavis_B;
                        DavisCNSSpMM = ((LoadFullORTSDavis_C - LoadEmptyORTSDavis_C) * TempMassDiffRatio) + LoadEmptyORTSDavis_C;

                        if (LoadEmptyDavisDragConstant > LoadFullDavisDragConstant) // Due to wind turbulence empty drag might be higher then loaded drag, and therefore both scenarios need to be covered.
                        {
                            DavisDragConstant = LoadEmptyDavisDragConstant - ((LoadEmptyDavisDragConstant - LoadFullDavisDragConstant) * TempMassDiffRatio);
                        }
                        else
                        {
                            DavisDragConstant = ((LoadFullDavisDragConstant - LoadEmptyDavisDragConstant) * TempMassDiffRatio) + LoadEmptyDavisDragConstant;
                        }

                        WagonFrontalAreaM2 = ((LoadFullWagonFrontalAreaM2 - LoadEmptyWagonFrontalAreaM2) * TempMassDiffRatio) + LoadEmptyWagonFrontalAreaM2;

                        // Update CoG related parameters
                        CentreOfGravityM.Y = ((LoadFullCentreOfGravityM_Y - LoadEmptyCentreOfGravityM_Y) * TempMassDiffRatio) + LoadEmptyCentreOfGravityM_Y;
                        
                    }
                }
            }
        }

        private void UpdateDriveWheelWeight(int index,  float masskg, int numberofengines)
        {
           var  LocoIdentification = Train.Cars[index] as MSTSSteamLocomotive;
            if (LocoIdentification != null)
            {
                for (int i = 0; i < LocoIdentification.SteamEngines.Count; i++)
                {
                    LocoIdentification.SteamEngines[i].AttachedAxle.WheelWeightKg = (MassKG / InitialMassKG) * LocoIdentification.SteamEngines[i].AttachedAxle.InitialDrvWheelWeightKg;
                 }
            }
        }

        private void UpdateTrainBaseResistance()
        {
            IsBelowMergeSpeed = AbsSpeedMpS < MergeSpeedMpS;
            IsStandStill = AbsSpeedMpS < 0.1f;
            bool isStartingFriction = StandstillFrictionN != 0;

            if (IsDavisFriction) // If set to use next Davis friction then do so
            {
                if (isStartingFriction && IsBelowMergeSpeed) // Davis formulas only apply above merge speed, so different treatment required for low speed
                    UpdateTrainBaseResistance_StartingFriction();
                else if (IsBelowMergeSpeed)
                    UpdateTrainBaseResistance_DavisLowSpeed();
                else
                    UpdateTrainBaseResistance_DavisHighSpeed();
            }
            else if (isStartingFriction && IsBelowMergeSpeed)
            {
                UpdateTrainBaseResistance_StartingFriction();
            }
            else
            {
                UpdateTrainBaseResistance_ORTS();
            }

            // TODO: the Davis A and B parameters already include rolling friction. Thus, there is an over-estimation
            // of the rolling friction forces, due to a small amount of friction being inserted to the axle module.
            // This needs to be fixed by inserting the Davis A and B parameters to the axle, to the 'friction' and
            // 'damping' axle parameters respectively, and their calculation must be removed from UpdateTrainBaseResistance_ methods
            // This means that low/high speed friction has to be calculated and passed to the axle module elsewhere
            // Davis C is related to air speed and must be calculated here and not inside the axle module
            FrictionForceN += RollingFrictionForceN;
        }

        /// <summary>
        /// Update train base resistance with the conventional Open Rails algorithm.
        /// </summary>
        /// <remarks>
        /// For all speeds.
        /// </remarks>
        private void UpdateTrainBaseResistance_ORTS()
        {
            if (FrictionV2 < 0 || FrictionV2 > 4.4407f) // > 10 mph
            {   // not fcalc ignore friction and use default davis equation
                // Assume plain bearings and calculate resistance per original Davis equation
                DavisAN = CalcDavisAValue(BearingTypes.Friction, MassKG, (WagonNumAxles + LocoNumDrvAxles));
                DavisBNSpM = CalcDavisBValue(BearingTypes.Friction, MassKG, (WagonNumAxles + LocoNumDrvAxles), WagonType);
                DavisCNSSpMM = NSSpMM.FromLbfpMpH2(Me2.ToFt2(WagonFrontalAreaM2) * DavisDragConstant);
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

            if (IsStandStill)
            {
                FrictionForceN = Friction0N;
            }
            else
            {
                FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * DavisCNSSpMM);

                // if this car is a locomotive, but not the lead one then recalculate the resistance with lower value as drag will not be as high on trailing locomotives
                // Only the drag (C) factor changes if a trailing locomotive, so only running resistance, and not starting resistance needs to be corrected
                if (WagonType == WagonTypes.Engine && Train.LeadLocomotive != this)
                    FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * (TrailLocoResistanceFactor * DavisCNSSpMM));

                // Test to identify whether a tender is attached to the leading engine, if not then the resistance should also be derated as for the locomotive
                bool IsLeadTender = false;
                if (WagonType == WagonTypes.Tender)
                {
                    bool PrevCarLead = false;
                    foreach (var car in Train.Cars)
                    {
                        // If this car is a tender and the previous car is the lead locomotive then set the flag so that resistance will be reduced
                        if (car == this && PrevCarLead)
                        {
                            IsLeadTender = true;
                            break;  // If the tender has been identified then break out of the loop, otherwise keep going until whole train is done.
                        }
                        // Identify whether car is a lead locomotive or not. This is kept for when the next iteration (next car) is checked.
                        PrevCarLead = Train.LeadLocomotive == car;
                    }

                    // If tender is coupled to a trailing locomotive then reduce resistance
                    if (!IsLeadTender)
                        FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * (TrailLocoResistanceFactor * DavisCNSSpMM));
                }
            }
        }

        /// <summary>
        /// Calculate an estimate for the Davis A value of a wagon using the davis formula and variations on it.
        /// </summary>
        /// <param name="bearings">BearingType we want to calculate the resistance for.</param>
        /// <param name="mass">kg weight of the rail vehicle.</param>
        /// <param name="axles">Total number of axles on the rail vehicle.</param>
        /// <returns>An estimate for the Davis A value of the wagon in newtons.</returns>
        public static float CalcDavisAValue(BearingTypes bearings, float mass, int axles)
        {
            float cT = 1.5f; // Resistance component in pounds per US ton
            float cN = 20f; // Resistance component in pounds per axle

            // Calculations based on Davis studies, with some estimation where data isn't availble
            switch (bearings)
            {
                case BearingTypes.Grease:
                case BearingTypes.Friction: // 1926 Davis
                    if (Kg.ToTUS(mass) / axles < 5.0f) // Alternate Davis formula for light vehicles
                    {
                        cT = 9.4f * (float)Math.Sqrt(Kg.ToTUS(mass));
                        cN = 12.5f;
                    }
                    else
                    {
                        cT = 1.3f;
                        cN = 29f;
                    }
                    break;
                case BearingTypes.Roller: // 1992 Canadian National
                    cT = 1.5f;
                    cN = 18f;
                    break;
                case BearingTypes.Low: // Estimate from CN method and tests on new bearings
                    cT = 1.5f;
                    cN = 11f;
                    break;
            }
            // Davis uses imperial, convert to metric afterward
            return N.FromLbf(cT * Kg.ToTUS(mass) + cN * axles);
        }

        /// <summary>
        /// Calculate an estimate for the Davis B value of a wagon using the davis formula and variations on it.
        /// </summary>
        /// <param name="bearings">BearingType we want to calculate the resistance for.</param>
        /// <param name="mass">kg weight of the rail vehicle.</param>
        /// <param name="axles">Total number of axles on the rail vehicle.</param>
        /// <param name="type">WagonType we want to calculate the resistance for.</param>
        /// <returns>An estimate for the Davis B value of the wagon in newtons per meter per second.</returns>
        public static float CalcDavisBValue(BearingTypes bearings, float mass, int axles, WagonTypes type = WagonTypes.Freight)
        {
            float cT = 0.03f; // Resistance component in pounds per US ton

            // Calculations based on Davis studies, with some estimation where data isn't availble
            switch (bearings)
            {
                case BearingTypes.Grease:
                case BearingTypes.Friction: // 1926 Davis
                    if (Kg.ToTUS(mass) / axles < 5.0f) // Alternate Davis formula for light vehicles
                    {
                        cT = 0.009f;
                    }
                    else
                    {
                        switch (type)
                        {
                            case WagonTypes.Tender:
                            case WagonTypes.Freight:
                                cT = 0.045f;
                                break;
                            case WagonTypes.Engine:
                            case WagonTypes.Passenger:
                                cT = 0.03f;
                                break;
                        }
                    }
                    break;
                case BearingTypes.Roller: // 1992 Canadian National
                    switch (type)
                    {
                        case WagonTypes.Tender:
                        case WagonTypes.Freight:
                            cT = 0.03f;
                            break;
                        case WagonTypes.Engine:     // Estimate from CN method and Davis study
                        case WagonTypes.Passenger:
                            cT = 0.02f;
                            break;
                    }
                    break;
                case BearingTypes.Low: // Estimate from CN method and tests on new bearings
                    switch (type)
                    {
                        case WagonTypes.Tender:
                        case WagonTypes.Freight:
                            cT = 0.02f;
                            break;
                        case WagonTypes.Engine:
                        case WagonTypes.Passenger:
                            cT = 0.015f;
                            break;
                    }
                    break;
            }
            // Davis uses imperial, convert to metric afterward
            return NSpM.FromLbfpMpH(cT * Kg.ToTUS(mass));
        }

        /// <summary>
        /// Update train base resistance with a manually specified starting friction.
        /// </summary>
        /// <remarks>
        /// For speeds slower than the merge speed.
        /// </remarks>
        private void UpdateTrainBaseResistance_StartingFriction()
        {
            // Dtermine the starting friction factor based upon the type of bearing
            float StartFrictionLoadN = StandstillFrictionN;  // Starting friction
            
            // Determine the starting resistance due to wheel bearing temperature
            // Note reference values in lbf and US tons - converted to metric values as appropriate
            // At -10 DegC it will be equal to the snowing value, as the temperature increases to 25 DegC, it will move towards the summer value
            // Assume a linear relationship between the two sets of points above and plot a straight line relationship.
            const float RunGrad = -0.0085714285714286f;
            const float RunIntersect = 1.2142857142857f;
            if (WheelBearingTemperatureDegC < -10) // Set to snowing (frozen value)
                StartFrictionLoadN = 1.2f;  // Starting friction, snowing
            else if (WheelBearingTemperatureDegC > 25) // Set to normal temperature value
                StartFrictionLoadN = 1.0f;  // Starting friction, not snowing
            else // Set to variable value as bearing heats and cools
                StartFrictionLoadN = RunGrad * WheelBearingTemperatureDegC + RunIntersect;
            StaticFrictionFactorN = StartFrictionLoadN;

            // Determine the running resistance due to wheel bearing temperature
            float WheelBearingTemperatureResistanceFactor = 0;

            // Assume the running resistance is impacted by wheel bearing temperature, ie gets higher as tmperature decreasses. This will only impact the A parameter as it is related to
            // bearing. Assume that resistance will increase by 30% as temperature drops below 0 DegC.
            // At -10 DegC it will be equal to the snowing value, as the temperature increases to 25 DegC, it will move towards the summer value
            // Assume a linear relationship between the two sets of points above and plot a straight line relationship.

            if (WheelBearingTemperatureDegC < -10) // Set to snowing (frozen value)
                WheelBearingTemperatureResistanceFactor = 1.3f;
            else if (WheelBearingTemperatureDegC > 25) // Set to normal temperature value
                WheelBearingTemperatureResistanceFactor = 1.0f;
            else // Set to variable value as bearing heats and cools
                WheelBearingTemperatureResistanceFactor = RunGrad * WheelBearingTemperatureDegC + RunIntersect;
            // If hot box has been initiated, then increase friction on the wagon significantly
            if (HotBoxActivated && ActivityElapsedDurationS > HotBoxStartTimeS)
            {
                WheelBearingTemperatureResistanceFactor = 2.0f;
                StaticFrictionFactorN *= 2.0f;
            }
            // Calculation of resistance @ low speeds
            // Wind resistance is not included at low speeds, as it does not have a significant enough impact
            MergeSpeedFrictionN = DavisAN * WheelBearingTemperatureResistanceFactor + (MergeSpeedMpS) * (DavisBNSpM + (MergeSpeedMpS) * DavisCNSSpMM); // Calculate friction @ merge speed
            Friction0N = StandstillFrictionN * StaticFrictionFactorN; // Static friction x external resistance as this matches reference value
            FrictionBelowMergeSpeedN = ((1.0f - (AbsSpeedMpS / (MergeSpeedMpS))) * (Friction0N - MergeSpeedFrictionN)) + MergeSpeedFrictionN; // Calculate friction below merge speed - decreases linearly with speed
            FrictionForceN = FrictionBelowMergeSpeedN; // At low speed use this value
        }

        /// <summary>
        /// Update train base resistance with the Davis function.
        /// </summary>
        /// <remarks>
        /// For speeds slower than the "slow" speed.
        /// Based upon the article "Carriage and Wagon Tractive Resistance" by L. I. Sanders and printed "The Locomotive" of June 15, 1938.
        /// It is suggested that Rs (Starting Resistance) = Rin (Internal resistance of wagon - typically journal resistance) + Rt (Track resistance - due to weight of car depressing track).
        /// 
        /// Rt = 1120 x weight on axle x tan (angle of track depression) lbs/ton (UK). Typical depression angles for wagons would be 1 in 800, and locomotives 1 in 400.
        /// 
        /// This article suggests the following values for Rin Internal Starting Resistance:
        /// 
        ///                            Above Freezing
        ///    Journal (Oil) Bearing      17.5 lb/ton   (long (UK) ton)
        ///    Journal (Grease) Bearing   30 lb/ton     (long (UK) ton)
        ///    Roller Bearing             4.5 lb/ton    (long (UK) ton)
        /// 
        /// AREMA suggests the following figures for Starting Resistance:
        /// 
        ///                       Above Freezing   Below Freezing                       Above Freezing   Below Freezing
        ///    Journal Bearing      25 lb/ton        35 lb/ton   (short (US) ton)           29.75 lb/ton    41.65 lb/ton   (long (UK) ton)
        ///    Roller Bearing        5 lb/ton        15 lb/ton                              5.95 lb/ton     17.85 lb/ton
        ///    
        /// Davis suggests, "After a long stop in cold weather, the tractive effort at the instant of starting may reach 15 to 25 pounds per ton (us),
        /// diminishing rapidly to a minimum at 5 to 10 miles per hour".
        /// 
        /// AREMA suggests - "The starting resistance of roller bearings is essentially the same as when they are in motion". Hence the starting resistance should not be less 
        /// then the A value in the Davis formula.
        /// 
        /// This model uses the following criteria:
        /// i) Fixed journal resistance based upon UK figures (never is less then the A Davis value). This value is also varied with different wheel diameters. Reference wheel diameter = 37" (uk wheel).
        /// ii) Track resistance which varies depending upon the axle weight
        /// 
        /// </remarks>
        private void UpdateTrainBaseResistance_DavisLowSpeed()
        {
            // Determine the internal starting friction factor based upon the type of bearing

            float StartFrictionInternalFactorN = 0.0f;  // Internal starting friction
            float StartFrictionTrackN = 0.0f;
            float AxleLoadKg = 0;
            float ResistanceGrade = 0;
            float ReferenceWheelRadiusM = Me.FromIn(37.0f / 2.0f);
            float wheelVariationFactor = 1.0f;

            // Find the variation in journal resistance due to wheel size. Steam locomotive don't have any variation at this time.
            if (WagonType == WagonTypes.Engine)
            {
                if (EngineType != EngineTypes.Steam)
                {
                    wheelVariationFactor = DriverWheelRadiusM / ReferenceWheelRadiusM;
                }
            }
            else
            {
                wheelVariationFactor = WheelRadiusM / ReferenceWheelRadiusM;
            }

            float LowTemperature = -10.0f;
            float HighTemeprature = 25.0f;

            float LowTemperatureResistanceN;
            float HighTemperatureResistanceN;

            switch (BearingType)
            {
                // Determine the starting resistance due to wheel bearing temperature
                // Note reference values in lbf and US tons - converted to metric values as appropriate
                // At -10 DegC it will be equal to the snowing value, as the temperature increases to 25 DegC, it will move towards the summer value
                // Assume a linear relationship between the two sets of points above and plot a straight line relationship.
                case BearingTypes.Low:
                    LowTemperatureResistanceN = N.FromLbf(7.5f) * wheelVariationFactor;
                    HighTemperatureResistanceN = N.FromLbf(2.5f) * wheelVariationFactor;
                    break;
                case BearingTypes.Roller:
                    LowTemperatureResistanceN = N.FromLbf(12.0f) * wheelVariationFactor;
                    HighTemperatureResistanceN = N.FromLbf(4.5f) * wheelVariationFactor;
                    break;
                case BearingTypes.Grease:
                    LowTemperatureResistanceN = N.FromLbf(45.0f) * wheelVariationFactor;
                    HighTemperatureResistanceN = N.FromLbf(30.0f) * wheelVariationFactor;
                    break;
                case BearingTypes.Friction:
                default:
                    LowTemperatureResistanceN = N.FromLbf(30.0f) * wheelVariationFactor;
                    HighTemperatureResistanceN = N.FromLbf(20.0f) * wheelVariationFactor;
                    break;

            }

            if (WheelBearingTemperatureDegC < LowTemperature)
            {
                // Set to snowing (frozen value)
                StartFrictionInternalFactorN = LowTemperatureResistanceN;
            }
            else if (WheelBearingTemperatureDegC > HighTemeprature)
            {
                // Set to normal temperature value
                StartFrictionInternalFactorN = HighTemperatureResistanceN;
            }
            else
            {
                // Set to variable value as bearing heats and cools
                float LowGrad = (LowTemperatureResistanceN - HighTemperatureResistanceN) / (LowTemperature - HighTemeprature);
                float LowIntersect = LowTemperatureResistanceN - (LowGrad * LowTemperature);
                StartFrictionInternalFactorN = LowGrad * WheelBearingTemperatureDegC + LowIntersect;
            }

            // Determine the track starting resistance, based upon the axle loading of the wagon
            float LowLoadGrade = 800.0f;
            float HighLoadGrade = 400.0f;
            float LowLoadKg = Kg.FromTUK(5.0f); // Low value is determined by average weight of passenger car with 6 axles = approx 30/6 = 5 tons uk
            float HighLoadKg = Kg.FromTUK(26.0f); // High value is determined by average maximum axle loading for PRR K2 locomotive - used for deflection tests 

            float TrackGrad = (LowLoadGrade - HighLoadGrade) / (LowLoadKg - HighLoadKg);
            float TrackIntersect = LowLoadGrade - (TrackGrad * LowLoadKg);

            // Determine Axle loading of Car
            if (WagonType == WagonTypes.Engine && IsPlayerTrain && Simulator.PlayerLocomotive is MSTSLocomotive locoParameters)
            {
                // This only takes into account the driven axles for 100% accuracy the non driven axles should also be considered
                AxleLoadKg = locoParameters.DrvWheelWeightKg / locoParameters.LocoNumDrvAxles;
            }
            else
            {
                // Typically this loop should only be processed when it is a car of some description, and therefore it will use the wagon axles as it reference.
                if (WagonNumAxles > 0)
                {
                    AxleLoadKg = MassKG / WagonNumAxles;
                }
            }

            // Calculate the track gradient based on wagon axle loading
            ResistanceGrade = TrackGrad * AxleLoadKg + TrackIntersect;

            ResistanceGrade = Math.Max(ResistanceGrade, 100); // Clamp gradient so it doesn't go below 1 in 100

            const float trackfactor = 1120.0f;
            StartFrictionTrackN = N.FromLbf(trackfactor * (1 / ResistanceGrade) * Kg.ToTUK(AxleLoadKg));

            // Determine the running resistance due to wheel bearing temperature
            float WheelBearingTemperatureResistanceFactor = 0;

            // This section temperature compensates the running friction only - for comparion of merge point of running and starting friction.
            // Assume the running resistance is impacted by wheel bearing temperature, ie gets higher as tmperature decreasses. This will only impact the A parameter as it is related to
            // bearing. Assume that resistance will increase by 30% as temperature drops below 0 DegC.
            // At -10 DegC it will be equal to the snowing value, as the temperature increases to 25 DegC, it will move towards the summer value
            // Assume a linear relationship between the two sets of points above and plot a straight line relationship.
            float MotionLowTemperature = -10.0f;
            float MotionHighTemeprature = 25.0f;
            float MotionLowTemperatureResistance = 1.3f;
            float MotionHighTemperatureResistance = 1.0f;

            if (WheelBearingTemperatureDegC < MotionLowTemperature)
            {
                // Set to snowing (frozen value)
                WheelBearingTemperatureResistanceFactor = 1.3f;
            }
            else if (WheelBearingTemperatureDegC > MotionHighTemeprature)
            {
                // Set to normal temperature value
                WheelBearingTemperatureResistanceFactor = 1.0f;
            }
            else
            {
                // Set to variable value as bearing heats and cools
                float RunGrad = (MotionLowTemperatureResistance - MotionHighTemperatureResistance) / (MotionLowTemperature - MotionHighTemeprature);
                float RunIntersect = MotionLowTemperatureResistance - (RunGrad * MotionLowTemperature);
                WheelBearingTemperatureResistanceFactor = RunGrad * WheelBearingTemperatureDegC + RunIntersect;
            }

            Friction0N = (Kg.ToTUK(MassKG) * StartFrictionInternalFactorN) + StartFrictionTrackN; // Static friction is journal or roller bearing friction x weight + track resistance. Mass value must be in tons uk to match reference used for starting resistance

            float Friction0DavisN = DavisAN * WheelBearingTemperatureResistanceFactor; // Calculate the starting firction if Davis formula was extended to zero

            // if the starting friction is less then the zero davis value, then set it higher then the zero davis value.
            if (Friction0N < Friction0DavisN)
            {
                Friction0N = Friction0DavisN * 1.2f;
            }

            // Calculation of resistance @ low speeds
            // Wind resistance is not included at low speeds, as it does not have a significant enough impact
            float speed5 = MpS.FromMpH(5); // 5 mph
            Friction5N = DavisAN * WheelBearingTemperatureResistanceFactor + speed5 * (DavisBNSpM + speed5 * DavisCNSSpMM); // Calculate friction @ 5 mph using "running" Davis values
            FrictionLowSpeedN = ((1.0f - (AbsSpeedMpS / speed5)) * (Friction0N - Friction5N)) + Friction5N; // Calculate friction below 5mph - decreases linearly with speed
            FrictionForceN = FrictionLowSpeedN; // At low speed use this value

#if DEBUG_FRICTION

            Trace.TraceInformation("========================== Debug Stationary Friction in MSTSWagon.cs ==========================================");
            Trace.TraceInformation("Stationary - CarID {0} Bearing - Roller: {1}, Low: {2}, Grease: {3}, Friction(Oil) {4}", CarID, IsRollerBearing, IsLowTorqueRollerBearing, IsGreaseFrictionBearing, IsFrictionBearing);
            Trace.TraceInformation("Stationary - Mass {0}, Mass (UK-tons) {1}, AxleLoad {2}, BearingTemperature {3}", MassKG, Kg.ToTUK(MassKG), Kg.ToTUK(AxleLoadKg), WheelBearingTemperatureDegC);

            Trace.TraceInformation("Stationary - Weather Type (1 for Snow) {0}", (int)Simulator.WeatherType);
            Trace.TraceInformation("Stationary - StartFrictionInternal {0}", N.ToLbf(StartFrictionInternalFactorN));
            Trace.TraceInformation("Stationary - StartFrictionTrack: {0}, ResistanceGrade: {1}", N.ToLbf(StartFrictionTrackN), ResistanceGrade);
            Trace.TraceInformation("Stationary - Force0N {0}, FrictionDavis0N {1}, Force5N {2}, Speed {3}, TemperatureFactor {4}", N.ToLbf(Friction0N), N.ToLbf(Friction0DavisN), N.ToLbf(Friction5N), AbsSpeedMpS, WheelBearingTemperatureResistanceFactor);

            Trace.TraceInformation("=============================================================================================================");
#endif
        }




        /// <summary>
        /// Update train base resistance with the Davis function.
        /// </summary>
        /// <remarks>
        /// For speeds faster than the "slow" speed.
        /// </remarks>
        private void UpdateTrainBaseResistance_DavisHighSpeed()
        {
            // Determine the running resistance due to wheel bearing temperature
            float WheelBearingTemperatureResistanceFactor = 0;

            // Assume the running resistance is impacted by wheel bearing temperature, ie gets higher as tmperature decreasses. This will only impact the A parameter as it is related to
            // bearing. Assume that resisnce will increase by 30% as temperature drops below 0 DegC.
            // At -10 DegC it will be equal to the snowing value, as the temperature increases to 25 DegC, it will move towards the summer value
            // Assume a linear relationship between the two sets of points above and plot a straight line relationship.
            const float RunGrad = -0.0085714285714286f;
            const float RunIntersect = 1.2142857142857f;

            if (WheelBearingTemperatureDegC < -10)
            {
                // Set to snowing (frozen value)
                WheelBearingTemperatureResistanceFactor = 1.3f;
            }
            else if (WheelBearingTemperatureDegC > 25)
            {
                // Set to normal temperature value
                WheelBearingTemperatureResistanceFactor = 1.0f;
            }
            else
            {
                // Set to variable value as bearing heats and cools
                WheelBearingTemperatureResistanceFactor = RunGrad * WheelBearingTemperatureDegC + RunIntersect;

            }

            // If hot box has been initiated, then increase friction on the wagon significantly
            if (HotBoxActivated && ActivityElapsedDurationS > HotBoxStartTimeS)
            {
                WheelBearingTemperatureResistanceFactor = 2.0f;
            }


            // if this car is a locomotive, but not the lead one then calculate the resistance with lower value as drag will not be as high on trailing locomotives
            // Only the drag (C) factor changes if a trailing locomotive, so only running resistance, and not starting resistance needs to be corrected
            if (WagonType == WagonTypes.Engine && Train.LeadLocomotive != this)
            {
                FrictionForceN = DavisAN * WheelBearingTemperatureResistanceFactor + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * (TrailLocoResistanceFactor * DavisCNSSpMM));
            }
            else
            {
                FrictionForceN = DavisAN * WheelBearingTemperatureResistanceFactor + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * DavisCNSSpMM); // for normal speed operation
            }

            // Test to identify whether a tender is attached to the leading engine, if not then the resistance should also be derated as for the locomotive
            bool IsLeadTender = false;
            if (WagonType == WagonTypes.Tender)
            {
                bool PrevCarLead = false;
                foreach (var car in Train.Cars)
                {
                    // If this car is a tender and the previous car is the lead locomotive then set the flag so that resistance will be reduced
                    if (car == this && PrevCarLead)
                    {
                        IsLeadTender = true;
                        break;  // If the tender has been identified then break out of the loop, otherwise keep going until whole train is done.
                    }
                    // Identify whether car is a lead locomotive or not. This is kept for when the next iteration (next car) is checked.
                    if (Train.LeadLocomotive == car)
                    {
                        PrevCarLead = true;
                    }
                    else
                    {
                        PrevCarLead = false;
                    }

                }

                // If tender is coupled to a trailing locomotive then reduce resistance
                if (!IsLeadTender)
                {
                    FrictionForceN = DavisAN * WheelBearingTemperatureResistanceFactor + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * (TrailLocoResistanceFactor * DavisCNSSpMM));
                }
            }
        }

        /// <summary>
        /// Updates the temperature of the wheel bearing on each wagon.
        /// </summary>
        private void UpdateWheelBearingTemperature(float elapsedClockSeconds)
        {
            // Increased bearing temperature impacts the train physics model in two ways - it reduces the starting friction, and also a hot box failure, can result in failure of the train.
            // This is a "representative" model of bearing heat based upon the information described in the following publications- 
            // PRR Report (Bulletin #26) - Train Resistance and Tonnage Rating
            // Illinois Test Report (Bulletin #59) - The Effects of Cold Weather upon Train Resistance and Tonnage Rating
            // This information is for plain (friction) type bearings, and there are many variables that effect bearing heating and cooling, however it is considered a "close approximation" 
            // for the purposes it serves, ie to simulate resistance variation with temperature.
            // The model uses the Newton Law of Heating and cooling to model the time taken for temperature rise and fall - ie of the form T(t) = Ts + (T0 - Ts)exp(kt)

            // Keep track of Activity details if an activity, setup random wagon, and start time for hotbox
            if (Simulator.ActivityRun != null && IsPlayerTrain)
            {
                if (ActivityElapsedDurationS<HotBoxStartTimeS)
                {
                    ActivityElapsedDurationS += elapsedClockSeconds;
                }

                // Determine whether car will be activated with a random hot box, only tested once at start of activity
                if (!HotBoxHasBeenInitialized) // If already initialised then skip
                {
                    // Activity randomizatrion needs to be active in Options menu, and HotBox will not be applied to a locomotive or tender.
                    if (Simulator.Settings.ActRandomizationLevel > 0 && WagonType != WagonTypes.Engine && WagonType != WagonTypes.Tender)
                    {                        
                        var HotboxRandom = Simulator.Random.Next(100) / Simulator.Settings.ActRandomizationLevel;
                        float PerCentRandom = 0.66f; // Set so that random time is always in first 66% of activity duration
                        var RawHotBoxTimeRandomS = Simulator.Random.Next(Train.ActivityDurationS);
                        if (!Train.HotBoxSetOnTrain) // only allow one hot box to be set per train 
                        {
                            if (HotboxRandom< 10)
                            {
                                HotBoxActivated = true;
                                Train.HotBoxSetOnTrain = true;
                                HotBoxStartTimeS = PerCentRandom* RawHotBoxTimeRandomS;

                                Trace.TraceInformation("Hotbox Bearing Activated on CarID {0}. Hotbox to start from {1:F1} minutes into activity", CarID, S.ToM(HotBoxStartTimeS));
                            }
                        }

                                            
                    }
                }

                HotBoxHasBeenInitialized = true; // Only allow to loop once at first pass
            }
            

            float BearingSpeedMaximumTemperatureDegC = 0;
            float MaximumNormalBearingTemperatureDegC = 90.0f;
            float MaximumHotBoxBearingTemperatureDegC = 120.0f;

            // K values calculated based on data in PRR report
            float CoolingKConst = -0.0003355569417321907f; // Time = 1380s, amb = -9.4. init = 56.7C, final = 32.2C
            float HeatingKConst = -0.000790635114477831f;  // Time = 3600s, amb = -9.4. init = 56.7C, final = 12.8C

            // Empty wagons take longer for hot boxes to heat up, this section looks at the load on a wagon, and assigns a K value to suit loading.
            // Guesstimated K values for Hotbox
            float HotBoxKConst = 0;
            float HotBoxKConstHighLoad = -0.002938026821980944f;  // Time = 600s, amb = -9.4. init = 120.0C, final = 12.8C
            float HotBoxKConstLowLoad = -0.001469013410990472f;  // Time = 1200s, amb = -9.4. init = 120.0C, final = 12.8C

            // Aligns to wagon weights used in friction calculations, ie < 10 tonsUS, and > 100 tonsUS either the low or high value used rspectively. In between these two values KConst scaled.
            if (MassKG < Kg.FromTUS(10)) // Lightly loaded wagon
            {
                HotBoxKConst = -0.001469013410990472f;
            }
            else if (MassKG > Kg.FromTUS(100)) // Heavily loaded wagon
            {
                HotBoxKConst = -0.002938026821980944f;
            }
            else
            {
                // Scaled between light and heavy loads
                var HotBoxScaleFactor = (MassKG - Kg.FromTUS(10)) / (Kg.FromTUS(100) - Kg.FromTUS(10));
                HotBoxKConst = HotBoxKConstLowLoad - ((float)Math.Abs(HotBoxKConstHighLoad - HotBoxKConstLowLoad)) * HotBoxScaleFactor;
            }


            if (elapsedClockSeconds > 0) // Prevents zero values resetting temperature
            {
                
                // Keep track of wheel bearing temperature until activtaion time reached
                if (ActivityElapsedDurationS<HotBoxStartTimeS) 
                {
                   InitialHotBoxRiseTemperatureDegS = WheelBearingTemperatureDegC;
                }

                // Calculate Hot box bearing temperature
                if (HotBoxActivated && ActivityElapsedDurationS > HotBoxStartTimeS && AbsSpeedMpS > 7.0)
                {

                    if (!HotBoxSoundActivated)
                    {
                        SignalEvent(Event.HotBoxBearingOn);
                        HotBoxSoundActivated = true;
                    }

                    HotBoxTemperatureRiseTimeS += elapsedClockSeconds;

                    // Calculate predicted bearing temperature based upon elapsed time
                    WheelBearingTemperatureDegC = MaximumHotBoxBearingTemperatureDegC + (InitialHotBoxRiseTemperatureDegS - MaximumHotBoxBearingTemperatureDegC) * (float) (Math.Exp(HotBoxKConst* HotBoxTemperatureRiseTimeS));

                    // Reset temperature decline values in preparation for next cylce
                    WheelBearingTemperatureDeclineTimeS = 0;
                    InitialWheelBearingDeclineTemperatureDegC = WheelBearingTemperatureDegC;

                }
                // Normal bearing temperature operation
                else if (AbsSpeedMpS > 7.0) // If train is moving calculate heating temperature
                {
                    // Calculate maximum bearing temperature based on current speed using approximated linear graph y = 0.25x + 55
                    const float MConst = 0.25f;
                    const float BConst = 55;
                    BearingSpeedMaximumTemperatureDegC = MConst* AbsSpeedMpS + BConst;

                    WheelBearingTemperatureRiseTimeS += elapsedClockSeconds;

                    // Calculate predicted bearing temperature based upon elapsed time
                    WheelBearingTemperatureDegC = MaximumNormalBearingTemperatureDegC + (InitialWheelBearingRiseTemperatureDegC - MaximumNormalBearingTemperatureDegC) * (float) (Math.Exp(HeatingKConst* WheelBearingTemperatureRiseTimeS));

                    // Cap bearing temperature depending upon speed
                    if (WheelBearingTemperatureDegC > BearingSpeedMaximumTemperatureDegC)
                    {
                        WheelBearingTemperatureDegC = BearingSpeedMaximumTemperatureDegC;
                    }

                    // Reset Decline values in preparation for next cylce
                    WheelBearingTemperatureDeclineTimeS = 0;
                    InitialWheelBearingDeclineTemperatureDegC = WheelBearingTemperatureDegC;

                }
                // Calculate cooling temperature if train stops or slows down 
                else
                {
                    if (WheelBearingTemperatureDegC > CarOutsideTempC)
                    {
                        WheelBearingTemperatureDeclineTimeS += elapsedClockSeconds;
                        WheelBearingTemperatureDegC = CarOutsideTempC + (InitialWheelBearingDeclineTemperatureDegC - CarOutsideTempC) * (float) (Math.Exp(CoolingKConst* WheelBearingTemperatureDeclineTimeS));
                    }

                    WheelBearingTemperatureRiseTimeS = 0;
                    InitialWheelBearingRiseTemperatureDegC = WheelBearingTemperatureDegC;

                }

                WheelBearingTemperatureRiseTimeS = 0;
                InitialWheelBearingRiseTemperatureDegC = WheelBearingTemperatureDegC;
                
                // Turn off Hotbox sounds
                SignalEvent(Event.HotBoxBearingOff);
                HotBoxSoundActivated = false;

            }

            // Set warning messages for hot bearing and failed bearings
            if (WheelBearingTemperatureDegC > 115)
            {
                var hotboxfailuremessage = "CarID" + CarID + "has experienced a failure due to a hot wheel bearing";
                Simulator.Confirmer.Message(ConfirmLevel.Warning, hotboxfailuremessage);
                WheelBearingFailed = true;
            }
            else if (WheelBearingTemperatureDegC > 100 && WheelBearingTemperatureDegC <= 115)
            {
                if (!WheelBearingHot)
                {
                    var hotboxmessage = "CarID" + CarID + "is experiencing a hot wheel bearing";
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, hotboxmessage);
                    WheelBearingHot = true;
                }
            }
            else
            {
                WheelBearingHot = false;
            }

            // Assume following limits for HUD - Normal operation: 50 - 90, Cool: < 50, Warm: 90 - 100, Hot: 100 - 115, Fail: > 115 - Set up text for HUD
            DisplayWheelBearingTemperatureStatus = WheelBearingTemperatureDegC > 115 ? "Fail" + "!!!" : WheelBearingTemperatureDegC > 100 && WheelBearingTemperatureDegC <= 115 ? "Hot" + "!!!"
                : WheelBearingTemperatureDegC > 90 && WheelBearingTemperatureDegC <= 100 ? "Warm" + "???" : WheelBearingTemperatureDegC <= 50 ? "Cool" + "%%%" : "Norm" + "";

            if (WheelBearingTemperatureDegC > 90)
            {
                // Turn on smoke effects for bearing hot box
                BearingHotBoxSmokeDurationS = 1;
                BearingHotBoxSmokeVelocityMpS = 10.0f;
                BearingHotBoxSmokeVolumeM3pS = 1.5f;
            }
            else if (WheelBearingTemperatureDegC < 50)
            {
                // Turn off smoke effects for hot boxs
                BearingHotBoxSmokeDurationS = 0;
                BearingHotBoxSmokeVelocityMpS = 0;
                BearingHotBoxSmokeVolumeM3pS = 0;
            }

        }

        private void UpdateWindForce()
        {
            // Calculate compensation for  wind
            // There are two components due to wind -
            // Drag, impact of wind on train, will increase resistance when head on, will decrease resistance when acting as a tailwind.
            // Lateral resistance - due to wheel flange being pushed against rail due to side wind.
            // Calculation based upon information provided in AREA 1942 Proceedings - https://archive.org/details/proceedingsofann431942amer - pg 56

            // Only calculate wind resistance if option selected in options menu, and not in a tunnel, and speed is sufficient for wind effects (>5mph)
            if (!CarTunnelData.FrontPositionBeyondStartOfTunnel.HasValue && AbsSpeedMpS > 2.2352)
            {
                // Wagon Direction
                var directionRad = (float)Math.Atan2(WorldPosition.XNAMatrix.M13, WorldPosition.XNAMatrix.M11);
                var directionDeg = MathHelper.ToDegrees(directionRad);

                // If car is flipped, then the car's direction will be reversed by 180 compared to the rest of the train, and thus for calculation purposes only,
                // it is necessary to reverse the "assumed" direction of the car back again. This shouldn't impact the visual appearance of the car.
                if (Flipped)
                {
                    // Reverse direction of car
                    directionDeg += 180.0f;

                    // If this results in an angle greater then 360, then convert it back to an angle between 0 & 360.
                    if (directionDeg > 360)
                        directionDeg -= 360;
                }

                // If a westerly direction (ie -ve) convert to an angle between 0 and 360
                if (directionDeg < 0)
                    directionDeg += 360;

                // Find angle between wind and direction of train
                var resultantWindComponentDeg = 0.0f;
                if (Train.PhysicsWindDirectionDeg > directionDeg)
                    resultantWindComponentDeg = Train.PhysicsWindDirectionDeg - directionDeg;
                else if (directionDeg > Train.PhysicsWindDirectionDeg)
                    resultantWindComponentDeg = directionDeg - Train.PhysicsWindDirectionDeg;

                // Correct wind direction if it is greater then 360 deg, then correct to a value less then 360
                if (Math.Abs(resultantWindComponentDeg) > 360)
                    resultantWindComponentDeg -= 360.0f;

                // Wind angle should be kept between 0 and 180 the formulas do not cope with angles > 180. If angle > 180, denotes wind of "other" side of train
                if (resultantWindComponentDeg > 180)
                    resultantWindComponentDeg = 360 - resultantWindComponentDeg;

                var resultantWindComponentRad = MathHelper.ToRadians(resultantWindComponentDeg);

                // Find the resultand wind vector for the combination of wind and train speed
                var windResultantSpeedMpS = (float)Math.Sqrt(AbsSpeedMpS * AbsSpeedMpS + Train.PhysicsWindSpeedMpS * Train.PhysicsWindSpeedMpS + 2.0f * AbsSpeedMpS * Train.PhysicsWindSpeedMpS * (float)Math.Cos(resultantWindComponentRad));

                // Calculate Drag Resistance
                // The drag resistance will be the difference between the STILL firction calculated using the standard Davies equation,
                // and that produced using the wind resultant speed (combination of wind speed and train speed)
                var tempStillDragResistanceForceN = AbsSpeedMpS * AbsSpeedMpS * DavisCNSSpMM;
                var tempCombinedDragResistanceForceN = windResultantSpeedMpS * windResultantSpeedMpS * DavisCNSSpMM; // R3 of Davis formula taking into account wind
                float windDragResistanceForceN;

                // Find the difference between the Still and combined resistances
                // This difference will be added or subtracted from the overall friction force depending upon the estimated wind direction.
                if (tempCombinedDragResistanceForceN > tempStillDragResistanceForceN)
                {
                    // Wind typically headon to train - increase resistance - +ve differential
                    windDragResistanceForceN = tempCombinedDragResistanceForceN - tempStillDragResistanceForceN;
                }
                else
                {
                    // Wind typically following train - reduce resistance - -ve differential
                    windDragResistanceForceN = tempStillDragResistanceForceN - tempCombinedDragResistanceForceN;
                    windDragResistanceForceN *= -1.0f;  // Convert to negative number to allow subtraction from ForceN
                }

                // Calculate Lateral Resistance

                // Calculate lateral resistance due to wind
                // Resistance is due to the wheel flanges being pushed further onto rails when a cross wind is experienced by a train
                var a = Train.PhysicsWindSpeedMpS / AbsSpeedMpS;
                var c = (float)Math.Sqrt((1 + (a * a) + 2.0f * a * Math.Cos(resultantWindComponentRad)));
                var windConstant = 8.25f;
                var speedMpH = Me.ToMi(pS.TopH(AbsSpeedMpS));

                var wagonFrontalAreaFt2 = Me2.ToFt2(WagonFrontalAreaM2);

                LateralWindForceN = N.FromLbf(windConstant * a * (float)Math.Sin(resultantWindComponentRad) * DavisDragConstant * wagonFrontalAreaFt2 * speedMpH * speedMpH * c);

                var lateralWindResistanceForceN = N.FromLbf(windConstant * a * (float)Math.Sin(resultantWindComponentRad) * DavisDragConstant * wagonFrontalAreaFt2 * speedMpH * speedMpH * c * Train.WagonCoefficientFriction);

                // if this car is a locomotive, but not the lead one then recalculate the resistance with lower C value as drag will not be as high on trailing locomotives
                if (WagonType == WagonTypes.Engine && Train.LeadLocomotive != this)
                {
                    lateralWindResistanceForceN *= TrailLocoResistanceFactor;
                }

                // Test to identify whether a tender is attached to the leading engine, if not then the resistance should also be derated as for the locomotive
                var isLeadTender = false;
                if (WagonType == WagonTypes.Tender)
                {
                    var prevCarLead = false;
                    foreach (var car in Train.Cars)
                    {
                        // If this car is a tender and the previous car is the lead locomotive then set the flag so that resistance will be reduced
                        if (car == this && prevCarLead)
                        {
                            isLeadTender = true;
                            break;  // If the tender has been identified then break out of the loop, otherwise keep going until whole train is done.
                        }

                        // Identify whether car is a lead locomotive or not. This is kept for when the next iteration (next car) is checked.
                        if (Train.LeadLocomotive == car)
                        {
                            prevCarLead = true;
                        }
                        else
                        {
                            prevCarLead = false;
                        }
                    }

                    // If tender is coupled to a trailing locomotive then reduce resistance
                    if (!isLeadTender)
                    {
                        lateralWindResistanceForceN *= TrailLocoResistanceFactor;
                    }
                }

                WindForceN = lateralWindResistanceForceN + windDragResistanceForceN;
            }
            else
            {
                WindForceN = 0.0f; // Set to zero if wind resistance is not to be calculated
            }
        }

        private void UpdateTenderLoad()
        // This section updates the weight and physics of the tender, and aux tender as load varies on it
        {

            if (FreightAnimations != null && FreightAnimations.ContinuousFreightAnimationsPresent) // make sure that a freight animation INCLUDE File has been defined, and it contains "continuous" animation data.
            {

                if (WagonType == WagonTypes.Tender)
                {
                    // Find the associated steam locomotive for this tender
                    if (TendersSteamLocomotive == null) FindTendersSteamLocomotive();

                    // If no locomotive is found to be associated with this tender, then OR crashes, ie TendersSteamLocomotive is still null. 
                    // This message will provide the user with information to correct the problem
                    if (TendersSteamLocomotive == null)
                    {
                        Trace.TraceInformation("Tender @ position {0} does not have a locomotive associated with. Check that it is preceeded by a steam locomotive.", CarID);
                    }

                    MassKG = FreightAnimations.WagonEmptyWeight + TendersSteamLocomotive.TenderFuelMassKG + Kg.FromLb( (TendersSteamLocomotive.CurrentLocoTenderWaterVolumeUKG * WaterLBpUKG));
                    MassKG = MathHelper.Clamp(MassKG, LoadEmptyMassKg, LoadFullMassKg); // Clamp Mass to between the empty and full wagon values   

                    // Update wagon parameters sensitive to wagon mass change
                    // Calculate the difference ratio, ie how full the wagon is. This value allows the relevant value to be scaled from the empty mass to the full mass of the wagon
                    float TempTenderMassDiffRatio = (MassKG - LoadEmptyMassKg) / (LoadFullMassKg - LoadEmptyMassKg);
                    // Update brake parameters
                    MaxBrakeForceN = ((LoadFullMaxBrakeForceN - LoadEmptyMaxBrakeForceN) * TempTenderMassDiffRatio) + LoadEmptyMaxBrakeForceN;
                    MaxHandbrakeForceN = ((LoadFullMaxHandbrakeForceN - LoadEmptyMaxHandbrakeForceN) * TempTenderMassDiffRatio) + LoadEmptyMaxHandbrakeForceN;
                    // Not sensible to vary the relay valve ratio continouously; instead, it changes to loaded if more than 25% cargo is present
                    if (BrakeSystem is AirSinglePipe brakes)
                    {
                        brakes.RelayValveRatio = TempTenderMassDiffRatio > 0.25f ? LoadFullRelayValveRatio : LoadEmptyRelayValveRatio;
                        brakes.RelayValveInshotPSI = TempTenderMassDiffRatio > 0.25f ? LoadFullInshotPSI : LoadEmptyInshotPSI;
                    }
                    // Update friction related parameters
                    DavisAN = ((LoadFullORTSDavis_A - LoadEmptyORTSDavis_A) * TempTenderMassDiffRatio) + LoadEmptyORTSDavis_A;
                    DavisBNSpM = ((LoadFullORTSDavis_B - LoadEmptyORTSDavis_B) * TempTenderMassDiffRatio) + LoadEmptyORTSDavis_B;
                    DavisCNSSpMM = ((LoadFullORTSDavis_C - LoadEmptyORTSDavis_C) * TempTenderMassDiffRatio) + LoadEmptyORTSDavis_C;

                    if (LoadEmptyDavisDragConstant > LoadFullDavisDragConstant) // Due to wind turbulence empty drag might be higher then loaded drag, and therefore both scenarios need to be covered.
                    {
                        DavisDragConstant = LoadEmptyDavisDragConstant - ((LoadEmptyDavisDragConstant - LoadFullDavisDragConstant) * TempMassDiffRatio);
                    }
                    else
                    {
                        DavisDragConstant = ((LoadFullDavisDragConstant - LoadEmptyDavisDragConstant) * TempMassDiffRatio) + LoadEmptyDavisDragConstant;
                    }

                    WagonFrontalAreaM2 = ((LoadFullWagonFrontalAreaM2 - LoadEmptyWagonFrontalAreaM2) * TempMassDiffRatio) + LoadEmptyWagonFrontalAreaM2;

                    // Update CoG related parameters
                    CentreOfGravityM.Y = ((LoadFullCentreOfGravityM_Y - LoadEmptyCentreOfGravityM_Y) * TempTenderMassDiffRatio) + LoadEmptyCentreOfGravityM_Y;
                }
                else if (AuxWagonType == "AuxiliaryTender")
                {
                    // Find the associated steam locomotive for this tender
                    if (AuxTendersSteamLocomotive == null) FindAuxTendersSteamLocomotive();

                    MassKG = FreightAnimations.WagonEmptyWeight + Kg.FromLb((AuxTendersSteamLocomotive.CurrentAuxTenderWaterVolumeUKG * WaterLBpUKG));
                    MassKG = MathHelper.Clamp(MassKG, LoadEmptyMassKg, LoadFullMassKg); // Clamp Mass to between the empty and full wagon values   

                    // Update wagon parameters sensitive to wagon mass change
                    // Calculate the difference ratio, ie how full the wagon is. This value allows the relevant value to be scaled from the empty mass to the full mass of the wagon
                    float TempTenderMassDiffRatio = (MassKG - LoadEmptyMassKg) / (LoadFullMassKg - LoadEmptyMassKg);
                    // Update brake parameters
                    MaxBrakeForceN = ((LoadFullMaxBrakeForceN - LoadEmptyMaxBrakeForceN) * TempTenderMassDiffRatio) + LoadEmptyMaxBrakeForceN;
                    MaxHandbrakeForceN = ((LoadFullMaxHandbrakeForceN - LoadEmptyMaxHandbrakeForceN) * TempTenderMassDiffRatio) + LoadEmptyMaxHandbrakeForceN;
                    // Not sensible to vary the relay valve ratio continouously; instead, it changes to loaded if more than 25% cargo is present
                    if (BrakeSystem is AirSinglePipe brakes)
                    {
                        brakes.RelayValveRatio = TempTenderMassDiffRatio > 0.25f ? LoadFullRelayValveRatio : LoadEmptyRelayValveRatio;
                        brakes.RelayValveInshotPSI = TempTenderMassDiffRatio > 0.25f ? LoadFullInshotPSI : LoadEmptyInshotPSI;
                    }
                    // Update friction related parameters
                    DavisAN = ((LoadFullORTSDavis_A - LoadEmptyORTSDavis_A) * TempTenderMassDiffRatio) + LoadEmptyORTSDavis_A;
                    DavisBNSpM = ((LoadFullORTSDavis_B - LoadEmptyORTSDavis_B) * TempTenderMassDiffRatio) + LoadEmptyORTSDavis_B;
                    DavisCNSSpMM = ((LoadFullORTSDavis_C - LoadEmptyORTSDavis_C) * TempTenderMassDiffRatio) + LoadEmptyORTSDavis_C;

                    if (LoadEmptyDavisDragConstant > LoadFullDavisDragConstant) // Due to wind turbulence empty drag might be higher then loaded drag, and therefore both scenarios need to be covered.
                    {
                        DavisDragConstant = LoadEmptyDavisDragConstant - ((LoadEmptyDavisDragConstant - LoadFullDavisDragConstant) * TempMassDiffRatio);
                    }
                    else
                    {
                        DavisDragConstant = ((LoadFullDavisDragConstant - LoadEmptyDavisDragConstant) * TempMassDiffRatio) + LoadEmptyDavisDragConstant;
                    }

                    WagonFrontalAreaM2 = ((LoadFullWagonFrontalAreaM2 - LoadEmptyWagonFrontalAreaM2) * TempMassDiffRatio) + LoadEmptyWagonFrontalAreaM2;

                    // Update CoG related parameters
                    CentreOfGravityM.Y = ((LoadFullCentreOfGravityM_Y - LoadEmptyCentreOfGravityM_Y) * TempTenderMassDiffRatio) + LoadEmptyCentreOfGravityM_Y;
                }
            }
        }

        private void UpdateSpecialEffects(float elapsedClockSeconds)
        // This section updates the special effects
        {

            var LocomotiveParameters = Simulator.PlayerLocomotive as MSTSLocomotive;

            if (LocomotiveParameters != null)
            {

                // if this is a heating steam boiler car then adjust steam pressure
                // Don't turn steam heat on until pressure valve has been opened, water and fuel capacity also needs to be present, steam heating shouldn't already be present on diesel or steam locomotive
                if (IsPlayerTrain && WagonSpecialType == MSTSWagon.WagonSpecialTypes.HeatingBoiler && !LocomotiveParameters.IsSteamHeatFitted && LocomotiveParameters.SteamHeatController.CurrentValue > 0.05 && CurrentCarSteamHeatBoilerWaterCapacityL > 0 && CurrentSteamHeatBoilerFuelCapacityL > 0 && !IsSteamHeatBoilerLockedOut)
                {
                    //   LocomotiveParameters.CurrentSteamHeatPressurePSI = LocomotiveParameters.SteamHeatController.CurrentValue * 100;
                    LocomotiveParameters.CurrentSteamHeatPressurePSI = 60.0f;
                    Train.CarSteamHeatOn = true; // turn on steam effects on wagons
                }
                else if (IsPlayerTrain && WagonSpecialType == MSTSWagon.WagonSpecialTypes.HeatingBoiler)
                {
                    LocomotiveParameters.CurrentSteamHeatPressurePSI = 0.0f;
                    Train.CarSteamHeatOn = false; // turn off steam effects on wagons
                    SteamHeatingBoilerOn = false;
                }

                // Turn on Heating steam boiler
                if (Train.CarSteamHeatOn && LocomotiveParameters.SteamHeatController.CurrentValue > 0)
                {
                    // Turn heating boiler on 
                    HeatingSteamBoilerDurationS = 1.0f * LocomotiveParameters.SteamHeatController.CurrentValue;
                    HeatingSteamBoilerVolumeM3pS = 1.5f * LocomotiveParameters.SteamHeatController.CurrentValue;
                }
                else
                {
                    // Turn heating boiler off 
                    HeatingSteamBoilerVolumeM3pS = 0.0f;
                    HeatingSteamBoilerDurationS = 0.0f;
                }

                // Update Heating hose steam leaks Information
                if (Train.CarSteamHeatOn && CarSteamHeatMainPipeSteamPressurePSI > 0)
                {
                    // Turn wagon steam leaks on 
                    HeatingHoseParticleDurationS = 0.75f;
                    HeatingHoseSteamVelocityMpS = 15.0f;
                    HeatingHoseSteamVolumeM3pS = 4.0f * SteamHoseLeakRateRandom;
                }
                else
                {
                    // Turn wagon steam leaks off 
                    HeatingHoseParticleDurationS = 0.0f;
                    HeatingHoseSteamVelocityMpS = 0.0f;
                    HeatingHoseSteamVolumeM3pS = 0.0f;
                }

                // Update Heating main pipe steam trap leaks Information
                if (Train.CarSteamHeatOn && CarSteamHeatMainPipeSteamPressurePSI > 0)
                {
                    // Turn wagon steam leaks on 
                    HeatingMainPipeSteamTrapDurationS = 0.75f;
                    HeatingMainPipeSteamTrapVelocityMpS = 15.0f;
                    HeatingMainPipeSteamTrapVolumeM3pS = 8.0f;
                }
                else
                {
                    // Turn wagon steam leaks off 
                    HeatingMainPipeSteamTrapDurationS = 0.0f;
                    HeatingMainPipeSteamTrapVelocityMpS = 0.0f;
                    HeatingMainPipeSteamTrapVolumeM3pS = 0.0f;
                }

                // Update Heating compartment steam trap leaks Information
                if (SteamHeatingCompartmentSteamTrapOn)
                {
                    // Turn wagon steam leaks on 
                    HeatingCompartmentSteamTrapParticleDurationS = 0.75f;
                    HeatingCompartmentSteamTrapVelocityMpS = 15.0f;
                    HeatingCompartmentSteamTrapVolumeM3pS = 4.0f;
                }
                else
                {
                    // Turn wagon steam leaks off 
                    HeatingCompartmentSteamTrapParticleDurationS = 0.0f;
                    HeatingCompartmentSteamTrapVelocityMpS = 0.0f;
                    HeatingCompartmentSteamTrapVolumeM3pS = 0.0f;
                }

                // Update Water Scoop Spray Information when scoop is down and filling from trough

                bool ProcessWaterEffects = false; // Initialise test flag to see whether this wagon will have water sccop effects active

                if (WagonType == WagonTypes.Tender || WagonType == WagonTypes.Engine)
                {

                    if (WagonType == WagonTypes.Tender)
                    {
                        // Find the associated steam locomotive for this tender
                        if (TendersSteamLocomotive == null) FindTendersSteamLocomotive();

                        if (TendersSteamLocomotive == LocomotiveParameters && TendersSteamLocomotive.HasWaterScoop)
                        {
                            ProcessWaterEffects = true; // Set flag if this tender is attached to player locomotive
                        }

                    }
                    else if (Simulator.PlayerLocomotive == this && LocomotiveParameters.HasWaterScoop)
                    {
                        ProcessWaterEffects = true; // Allow water effects to be processed
                    }
                    else
                    {
                        ProcessWaterEffects = false; // Default off
                    }

                    // Tender Water overflow control
                    if (LocomotiveParameters.RefillingFromTrough && ProcessWaterEffects)
                    {
                        float SpeedRatio = AbsSpeedMpS / MpS.FromMpH(100); // Ratio to reduce water disturbance with speed - an arbitary value of 100mph has been chosen as the reference

                        // Turn tender water overflow on if water level is greater then 100% nominally and minimum water scoop speed is reached
                        if (LocomotiveParameters.TenderWaterLevelFraction >= 0.9999 && AbsSpeedMpS > LocomotiveParameters.WaterScoopMinSpeedMpS)
                        {
                            float InitialTenderWaterOverflowParticleDurationS = 1.25f;
                            float InitialTenderWaterOverflowVelocityMpS = 50.0f;
                            float InitialTenderWaterOverflowVolumeM3pS = 10.0f;

                            // Turn tender water overflow on - changes due to speed of train
                            TenderWaterOverflowParticleDurationS = InitialTenderWaterOverflowParticleDurationS * SpeedRatio;
                            TenderWaterOverflowVelocityMpS = InitialTenderWaterOverflowVelocityMpS * SpeedRatio;
                            TenderWaterOverflowVolumeM3pS = InitialTenderWaterOverflowVolumeM3pS * SpeedRatio;
                        }
                    }
                    else
                    {
                        // Turn tender water overflow off 
                        TenderWaterOverflowParticleDurationS = 0.0f;
                        TenderWaterOverflowVelocityMpS = 0.0f;
                        TenderWaterOverflowVolumeM3pS = 0.0f;
                    }

                    // Water scoop spray effects control - always on when scoop over trough, regardless of whether above minimum speed or not
                    if (ProcessWaterEffects && LocomotiveParameters.IsWaterScoopDown && IsOverTrough && AbsSpeedMpS > 0.1)
                    {
                        float SpeedRatio = AbsSpeedMpS / MpS.FromMpH(100); // Ratio to reduce water disturbance with speed - an arbitary value of 100mph has been chosen as the reference

                        float InitialWaterScoopParticleDurationS = 1.25f;
                        float InitialWaterScoopWaterVelocityMpS = 50.0f;
                        float InitialWaterScoopWaterVolumeM3pS = 10.0f;

                        // Turn water scoop spray effects on
                        if (AbsSpeedMpS <= MpS.FromMpH(10))
                        {
                            float SprayDecay = (MpS.FromMpH(25) / MpS.FromMpH(100)) / MpS.FromMpH(10); // Linear decay factor - based upon previous level starts @ a value @ 25mph
                            SpeedRatio = (SprayDecay * AbsSpeedMpS) / MpS.FromMpH(100); // Decrease the water scoop spray effect to minimum level of visibility
                            WaterScoopParticleDurationS = InitialWaterScoopParticleDurationS * SpeedRatio;
                            WaterScoopWaterVelocityMpS = InitialWaterScoopWaterVelocityMpS * SpeedRatio;
                            WaterScoopWaterVolumeM3pS = InitialWaterScoopWaterVolumeM3pS * SpeedRatio;

                        }
                        // Below 25mph effect does not vary, above 25mph effect varies according to speed
                        else if (AbsSpeedMpS < MpS.FromMpH(25) && AbsSpeedMpS > MpS.FromMpH(10))
                        {
                            SpeedRatio = MpS.FromMpH(25) / MpS.FromMpH(100); // Hold the water scoop spray effect to a minimum level of visibility
                            WaterScoopParticleDurationS = InitialWaterScoopParticleDurationS * SpeedRatio;
                            WaterScoopWaterVelocityMpS = InitialWaterScoopWaterVelocityMpS * SpeedRatio;
                            WaterScoopWaterVolumeM3pS = InitialWaterScoopWaterVolumeM3pS * SpeedRatio;
                        }
                        else
                        {
                            // Allow water sccop spray effect to vary with speed
                            WaterScoopParticleDurationS = InitialWaterScoopParticleDurationS * SpeedRatio;
                            WaterScoopWaterVelocityMpS = InitialWaterScoopWaterVelocityMpS * SpeedRatio;
                            WaterScoopWaterVolumeM3pS = InitialWaterScoopWaterVolumeM3pS * SpeedRatio;
                        }
                    }
                    else
                    {
                        // Turn water scoop spray effects off 
                        WaterScoopParticleDurationS = 0.0f;
                        WaterScoopWaterVelocityMpS = 0.0f;
                        WaterScoopWaterVolumeM3pS = 0.0f;

                    }

                    // Update Steam Brake leaks Information
                    if (LocomotiveParameters.EngineBrakeFitted && LocomotiveParameters.SteamEngineBrakeFitted && (WagonType == WagonTypes.Tender || WagonType == WagonTypes.Engine))
                    {
                        // Find the steam leakage rate based upon valve opening and current boiler pressure
                        float SteamBrakeLeakRate = LocomotiveParameters.EngineBrakeController.CurrentValue * (LocomotiveParameters.BoilerPressurePSI / LocomotiveParameters.MaxBoilerPressurePSI);

                        if (Simulator.PlayerLocomotive == this && LocomotiveParameters.EngineBrakeController.CurrentValue > 0)
                        {
                            // Turn steam brake leaks on 
                            SteamBrakeLeaksDurationS = 0.75f;
                            SteamBrakeLeaksVelocityMpS = 15.0f;
                            SteamBrakeLeaksVolumeM3pS = 4.0f * SteamBrakeLeakRate;
                        }
                        else
                        {
                            // Turn steam brake leaks off 
                            SteamBrakeLeaksDurationS = 0.0f;
                            SteamBrakeLeaksVelocityMpS = 0.0f;
                            SteamBrakeLeaksVolumeM3pS = 0.0f;
                        }

                        if (WagonType == WagonTypes.Tender)
                        {
                            // Find the associated steam locomotive for this tender
                            if (TendersSteamLocomotive == null) FindTendersSteamLocomotive();

                            // Turn steam brake effect on or off
                            if (TendersSteamLocomotive == LocomotiveParameters && LocomotiveParameters.EngineBrakeController.CurrentValue > 0)
                            {
                                // Turn steam brake leaks on 
                                SteamBrakeLeaksDurationS = 0.75f;
                                SteamBrakeLeaksVelocityMpS = 15.0f;
                                SteamBrakeLeaksVolumeM3pS = 4.0f * SteamBrakeLeakRate;
                            }
                            else
                            {
                                // Turn steam brake leaks off 
                                SteamBrakeLeaksDurationS = 0.0f;
                                SteamBrakeLeaksVelocityMpS = 0.0f;
                                SteamBrakeLeaksVolumeM3pS = 0.0f;
                            }
                        }
                    }
                }
            }

            WagonSmokeDurationS = InitialWagonSmokeDurationS;
            WagonSmokeVolumeM3pS = InitialWagonSmokeVolumeM3pS;
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
                case Event.Pantograph3Up:
                    SignalEvent(PowerSupplyEvent.RaisePantograph, 3);
                    break;
                case Event.Pantograph3Down:
                    SignalEvent(PowerSupplyEvent.LowerPantograph, 3);
                    break;
                case Event.Pantograph4Up:
                    SignalEvent(PowerSupplyEvent.RaisePantograph, 4);
                    break;
                case Event.Pantograph4Down:
                    SignalEvent(PowerSupplyEvent.LowerPantograph, 4);
                    break;
            }

            // TODO: This should be moved to TrainCar probably.
            try
            {
                foreach (var eventHandler in EventHandlers) // e.g. for HandleCarEvent() in Sounds.cs
                    eventHandler.HandleEvent(evt);
            }
            catch (Exception error)
            {
                Trace.TraceInformation("Sound event skipped due to thread safety problem " + error.Message);
            }

            base.SignalEvent(evt);
        }

        public override void SignalEvent(PowerSupplyEvent evt)
        {
            if (Simulator.PlayerLocomotive == this || RemoteControlGroup >= 0)
            {
                switch (evt)
                {
                    case PowerSupplyEvent.RaisePantograph:
                    case PowerSupplyEvent.LowerPantograph:
                        if (Pantographs != null)
                        {
                            Pantographs.HandleEvent(evt);
                        }
                        break;
                }
            }

            base.SignalEvent(evt);
        }

        public override void SignalEvent(PowerSupplyEvent evt, int id)
        {
            if (Simulator.PlayerLocomotive == this || RemoteControlGroup >= 0)
            {
                switch (evt)
                {
                    case PowerSupplyEvent.RaisePantograph:
                    case PowerSupplyEvent.LowerPantograph:
                        if (Pantographs != null)
                        {
                            Pantographs.HandleEvent(evt, id);
                        }
                        break;
                }
            }

            base.SignalEvent(evt, id);
        }

        public void ToggleMirrors()
        {
            MirrorOpen = !MirrorOpen;
            if (MirrorOpen) SignalEvent(Event.MirrorOpen); // hook for sound trigger
            else SignalEvent(Event.MirrorClose);
            if (Simulator.PlayerLocomotive == this) Simulator.Confirmer.Confirm(CabControl.Mirror, MirrorOpen ? CabSetting.On : CabSetting.Off);
        }

        public void ToggleWindow(bool rear, bool left)
        {
            var open = false;
            var index = (left ? 0 : 1) + 2 * (rear ? 1 : 0);
                if (WindowStates[index] == WindowState.Closed || WindowStates[index] == WindowState.Closing)
                    WindowStates[index] = WindowState.Opening;
                else if (WindowStates[index] == WindowState.Open || WindowStates[index] == WindowState.Opening)
                    WindowStates[index] = WindowState.Closing;
                if (WindowStates[index] == WindowState.Opening) open = true;


            if (open) SignalEvent(Event.WindowOpening); // hook for sound trigger
            else SignalEvent(Event.WindowClosing);
            if (Simulator.PlayerLocomotive == this) Simulator.Confirmer.Confirm(left ^ rear ? CabControl.WindowLeft : CabControl.WindowRight, open ? CabSetting.On : CabSetting.Off);
        }

        public void FindControlActiveLocomotive()
        {
            // Find the active locomotive associated with a control car
            if (Train == null || Train.Cars == null || Train.Cars.Count == 1)
            {
                ControlActiveLocomotive = null;
                return;
            }
            MSTSLocomotive unmatchedLocomotive = null;
            MSTSLocomotive unmatchedControlCar = null;
            foreach (var car in Train.Cars)
            {
                if (car.EngineType == TrainCar.EngineTypes.Electric || car.EngineType == TrainCar.EngineTypes.Diesel)
                {
                    if (unmatchedControlCar != null)
                    {
                        if (unmatchedControlCar == this)
                        {
                            unmatchedLocomotive = car as MSTSLocomotive;
                            break;
                        }
                        else
                        {
                            unmatchedControlCar = null;
                        }
                    }
                    else
                    {
                        unmatchedLocomotive = car as MSTSLocomotive;
                    }
                }
                if (car.EngineType == TrainCar.EngineTypes.Control)
                {
                    if (unmatchedLocomotive != null)
                    {
                        if (car == this)
                        {
                            break;
                        }
                        else
                        {
                            unmatchedLocomotive = null;
                        }
                    }
                    else
                    {
                        unmatchedControlCar = car as MSTSLocomotive;
                    }
                }
            }
            ControlActiveLocomotive = unmatchedLocomotive;
        }

        public void FindTendersSteamLocomotive()
        {
            // Find the steam locomotive associated with this wagon tender, this allows parameters processed in the steam loocmotive module to be used elsewhere
            if (Train == null || Train.Cars == null || Train.Cars.Count == 1)
            {
                TendersSteamLocomotive = null;
                return;
            }

            bool HasTender = false;
            var tenderIndex = 0;

            // Check to see if this car is defined as a tender, if so then set linkage to relevant steam locomotive. If no tender, then set linkage to null
            for (var i = 0; i < Train.Cars.Count; i++)
            {
                if (Train.Cars[i] == this && Train.Cars[i].WagonType == TrainCar.WagonTypes.Tender)
                {
                    tenderIndex = i;
                    HasTender = true;
                }
            }
            if (HasTender && tenderIndex > 0 && Train.Cars[tenderIndex - 1] is MSTSSteamLocomotive)
                TendersSteamLocomotive = Train.Cars[tenderIndex - 1] as MSTSSteamLocomotive;
            else if (HasTender && tenderIndex < Train.Cars.Count - 1 && Train.Cars[tenderIndex + 1] is MSTSSteamLocomotive)
                TendersSteamLocomotive = Train.Cars[tenderIndex + 1] as MSTSSteamLocomotive;
            else
                TendersSteamLocomotive = null;
        }

         /// <summary>
        /// This function checks each steam locomotive to see if it has a tender attached.
        /// </summary>
        public void ConfirmSteamLocomotiveTender()
        {
            
            // Check each steam locomotive to see if it has a tender attached.
            if (this is MSTSSteamLocomotive )
            {

                if (Train == null || Train.Cars == null)
                {
                    SteamLocomotiveTender = null;
                     return;
                }
                else if(Train.Cars.Count == 1) // If car count is equal to 1, then there must be no tender attached
                {
                    SteamLocomotiveTender = Train.Cars[0] as MSTSSteamLocomotive;
                    SteamLocomotiveTender.HasTenderCoupled = false;
                    SteamLocomotiveTender.AttachedTender = null;
                }

                var tenderIndex = 0;
                for (var i = 0; i < Train.Cars.Count; i++) // test each car to find the where the steam locomotive is in the consist
                {
                    if (Train.Cars[i] == this)  // If this car is a Steam locomotive the set tender index
                        tenderIndex = i;
                }

                if (tenderIndex < Train.Cars.Count - 1 && Train.Cars[tenderIndex + 1].WagonType == WagonTypes.Tender) // Assuming the tender is behind the locomotive
                {
                    SteamLocomotiveTender = Train.Cars[tenderIndex] as MSTSSteamLocomotive;
                    SteamLocomotiveTender.HasTenderCoupled = true;
                    SteamLocomotiveTender.AttachedTender = Train.Cars[tenderIndex + 1];
                }

                else if (tenderIndex > 0 && Train.Cars[tenderIndex - 1].WagonType == WagonTypes.Tender) // Assuming the tender is "in front" of the locomotive, ie it is running in reverse
                {
                    // TO BE CHECKED - What happens if multiple locomotives are coupled together in reverse?
                    SteamLocomotiveTender = Train.Cars[tenderIndex] as MSTSSteamLocomotive;
                    SteamLocomotiveTender.HasTenderCoupled = true;
                    SteamLocomotiveTender.AttachedTender = Train.Cars[tenderIndex - 1];
                }
                else // Assuming that locomotive is a tank locomotive, and no tender is coupled
                {
                    SteamLocomotiveTender = Train.Cars[tenderIndex] as MSTSSteamLocomotive;
                    SteamLocomotiveTender.HasTenderCoupled = false;
                    SteamLocomotiveTender.AttachedTender = null;
                }
            }
        }

        /// <summary>
        /// This function finds the steam locomotive associated with this wagon aux tender, this allows parameters processed in the steam loocmotive module to be used elsewhere.
        /// </summary>
        public void FindAuxTendersSteamLocomotive()
        {
            if (Train == null || Train.Cars == null || Train.Cars.Count == 1)
            {
                AuxTendersSteamLocomotive = null;
                return;
            }
            bool AuxTenderFound = false;
            var tenderIndex = 0;
            for (var i = 0; i < Train.Cars.Count; i++)
            {
                if (Train.Cars[i] == this)
                    tenderIndex = i;
            }

            // If a "normal" tender is not connected try checking if locomotive is directly connected to the auxiliary tender - this will be the case for a tank locomotive.
            if (tenderIndex > 0 && Train.Cars[tenderIndex - 1] is MSTSSteamLocomotive)
            {
                AuxTendersSteamLocomotive = Train.Cars[tenderIndex - 1] as MSTSSteamLocomotive;
                AuxTenderFound = true;
            }

            if (tenderIndex < Train.Cars.Count - 1 && Train.Cars[tenderIndex + 1] is MSTSSteamLocomotive)
            {
                AuxTendersSteamLocomotive = Train.Cars[tenderIndex + 1] as MSTSSteamLocomotive;
                AuxTenderFound = true;
            }

            // If a "normal" tender is connected then the steam locomotive will be two cars away.
                      
            if (!AuxTenderFound)
            {
            
                if (tenderIndex > 0 && Train.Cars[tenderIndex - 2] is MSTSSteamLocomotive)
                {
                    AuxTendersSteamLocomotive = Train.Cars[tenderIndex - 2] as MSTSSteamLocomotive;
                }
                
                if (tenderIndex < Train.Cars.Count - 2 && Train.Cars[tenderIndex + 2] is MSTSSteamLocomotive)
                {
                    AuxTendersSteamLocomotive = Train.Cars[tenderIndex + 2] as MSTSSteamLocomotive;
                }
            }
        }

        public bool GetTrainHandbrakeStatus()
        {
            return MSTSBrakeSystem.GetHandbrakeStatus();
        }

        // sound sources and viewers can register themselves to get direct notification of an event
        public List<Orts.Common.EventHandler> EventHandlers = new List<Orts.Common.EventHandler>();

        public MSTSCoupling Coupler
        {
            get  // This determines which coupler to use from WAG file, typically it will be the first one as by convention the rear coupler is always read first.
            {
                if (Couplers.Count == 0) return null;
                if (Flipped && Couplers.Count > 1) return Couplers[1];
                return Couplers[0]; // defaults to the rear coupler (typically the first read)
            }
        }

        public float TenderWagonMaxOilVolumeL { get; private set; }

        public override float GetCouplerZeroLengthM()
        {
            if (IsPlayerTrain && Simulator.UseAdvancedAdhesion && !Simulator.Settings.SimpleControlPhysics && IsAdvancedCoupler)
            {
                float zerolength;
                if (Coupler != null)
                {
                   zerolength = Coupler.R0X;
                }
                else
                {
                   zerolength = base.GetCouplerZeroLengthM();
                }

                // Ensure zerolength doesn't go higher then 0.5
                if (zerolength > 0.5)
                {
                   zerolength = 0.5f;
                }

                return zerolength;
            }
            else
            {
               return Coupler != null ? Coupler.R0X : base.GetCouplerZeroLengthM();
            } 
        }

        public override float GetSimpleCouplerStiffnessNpM()
        {
            return Coupler != null && Coupler.R0X == 0 ? 7 * (Coupler.Stiffness1NpM + Coupler.Stiffness2NpM) : base.GetSimpleCouplerStiffnessNpM();
        }

        public override float GetCouplerStiffness1NpM()
        {
            if (Coupler == null)
            {
                return base.GetCouplerStiffness1NpM();
            }
            return Coupler.Rigid? 10 * Coupler.Stiffness1NpM : Coupler.Stiffness1NpM;
        }
 
        public override float GetCouplerStiffness2NpM()
        {
            if (Coupler == null)
            {
                return base.GetCouplerStiffness2NpM();
            }
            return Coupler.Rigid? 10 * Coupler.Stiffness1NpM : Coupler.Stiffness2NpM;
        }

        public override float GetCouplerSlackAM()
        {
            if (Coupler == null)
            {
                return base.GetCouplerSlackAM();
            }
            return Coupler.CouplerSlackAM;
        }

        public override float GetCouplerSlackBM()
        {
            if (Coupler == null)
            {
                return base.GetCouplerSlackBM();
            }
            return Coupler.CouplerSlackBM;
        }

        public override bool GetCouplerRigidIndication()
        {
            if (Coupler == null)
            {
                 return base.GetCouplerRigidIndication();   // If no coupler defined
            }
            return Coupler.Rigid ? true : false; // Return whether coupler Rigid or Flexible
        }

        public override bool GetAdvancedCouplerFlag()
        {
            if (Coupler == null)
            {
                return base.GetAdvancedCouplerFlag();
            }
            return IsAdvancedCoupler;
        }

        public override float GetMaximumSimpleCouplerSlack1M()  // This limits the maximum amount of slack, and typically will be equal to y - x of R0 statement
        {

                if (Coupler == null)
                    return base.GetMaximumSimpleCouplerSlack1M();
                return Coupler.Rigid ? 0.0001f : Coupler.R0Diff;

        }

        public override float GetMaximumSimpleCouplerSlack2M() // This limits the slack due to draft forces (?) and should be marginally greater then GetMaximumCouplerSlack1M
        {

                if (Coupler == null)
                    return base.GetMaximumSimpleCouplerSlack2M();
                return Coupler.Rigid ? 0.0002f : base.GetMaximumSimpleCouplerSlack2M(); //  GetMaximumCouplerSlack2M > GetMaximumCouplerSlack1M

        }

        // Advanced coupler parameters

        public override float GetCouplerTensionStiffness1N()
        {
            if (Coupler == null)
            {
                return base.GetCouplerTensionStiffness1N();
            }
            return Coupler.Rigid ? 10 * Coupler.TensionStiffness1N : Coupler.TensionStiffness1N;
        }

        public override float GetCouplerTensionStiffness2N()
        {
            if (Coupler == null)
            {
                return base.GetCouplerTensionStiffness2N();
            }
            return Coupler.Rigid ? 10 * Coupler.TensionStiffness2N : Coupler.TensionStiffness2N;
        }

        public override float GetCouplerCompressionStiffness1N()
        {
            if (Coupler == null)
            {
                return base.GetCouplerCompressionStiffness1N();
            }
            return Coupler.Rigid ? 10 * Coupler.CompressionStiffness1N : Coupler.CompressionStiffness1N;
        }

        public override float GetCouplerCompressionStiffness2N()
        {
            if (Coupler == null)
            {
                return base.GetCouplerCompressionStiffness2N();
            }
            return Coupler.Rigid ? 10 * Coupler.CompressionStiffness2N : Coupler.CompressionStiffness2N;
        }

        public override float GetCouplerTensionSlackAM()
        {
            if (Coupler == null)
            {

                return base.GetCouplerTensionSlackAM();
            }

            return Coupler.CouplerTensionSlackAM;
        }

        public override float GetCouplerTensionSlackBM()
        {
            if (Coupler == null)
            {

                return base.GetCouplerTensionSlackBM();
            }

            return Coupler.CouplerTensionSlackBM;
        }

        public override float GetCouplerCompressionSlackAM()
        {
            if (Coupler == null)
            {
                return base.GetCouplerCompressionSlackAM();
            }
            return Coupler.CouplerCompressionSlackAM;
        }

        public override float GetCouplerCompressionSlackBM()
        {
            if (Coupler == null)
            {
                return base.GetCouplerCompressionSlackBM();
            }
            return Coupler.CouplerCompressionSlackBM;
        }

        public override float GetMaximumCouplerTensionSlack1M()  // This limits the maximum amount of slack, and typically will be equal to y - x of R0 statement
        {
            if (Coupler == null)
                return base.GetMaximumCouplerTensionSlack1M();

            if (Coupler.TensionR0Y == 0)
            {
                Coupler.TensionR0Y = GetCouplerTensionR0Y(); // if no value present, default value to tension value
            }
            return Coupler.Rigid ? 0.00001f : Coupler.TensionR0Y;
        }
        public override float GetMaximumCouplerTensionSlack2M()
        {

            // Zone 2 limit - ie Zone 1 + 2
            if (Coupler == null)
                return base.GetMaximumCouplerTensionSlack2M();

            return Coupler.Rigid? 0.0001f : Coupler.TensionR0Y + GetCouplerTensionSlackAM();
        }

        public override float GetMaximumCouplerTensionSlack3M() // This limits the slack due to draft forces (?) and should be marginally greater then GetMaximumCouplerSlack2M
        {
            if (Coupler == null)

            {
                return base.GetMaximumCouplerTensionSlack3M();
            }
            float Coupler2MTemporary = GetCouplerTensionSlackBM();
            if (Coupler2MTemporary == 0)
            {
                Coupler2MTemporary = 0.1f; // make sure that SlackBM is always > 0
            }
            return Coupler.Rigid ? 0.0002f : Coupler.TensionR0Y + GetCouplerTensionSlackAM() + Coupler2MTemporary; //  GetMaximumCouplerSlack3M > GetMaximumCouplerSlack2M
        }

        public override float GetMaximumCouplerCompressionSlack1M()  // This limits the maximum amount of slack, and typically will be equal to y - x of R0 statement
        {
            if (Coupler == null)
                return base.GetMaximumCouplerCompressionSlack1M();
            if (Coupler.CompressionR0Y == 0)
            {
                Coupler.CompressionR0Y = GetCouplerCompressionR0Y(); // if no value present, default value to compression value
            }
            return Coupler.Rigid ? 0.00005f : Coupler.CompressionR0Y;
        }

        public override float GetMaximumCouplerCompressionSlack2M()  // This limits the maximum amount of slack, and typically will be equal to y - x of R0 statement
        {
            if (Coupler == null)
                return base.GetMaximumCouplerCompressionSlack2M();

            return Coupler.Rigid ? 0.0001f : Coupler.CompressionR0Y + GetCouplerCompressionSlackAM();
        }

        public override float GetMaximumCouplerCompressionSlack3M() // This limits the slack due to draft forces (?) and should be marginally greater then GetMaximumCouplerSlack1M
        {
            if (Coupler == null)
            {
                return base.GetMaximumCouplerCompressionSlack3M();
            }
            float Coupler2MTemporary = GetCouplerCompressionSlackBM();
            if (Coupler2MTemporary == 0)
            {
                Coupler2MTemporary = 0.1f; // make sure that SlackBM is always > 0
            }
            return Coupler.Rigid ? 0.0002f : Coupler.CompressionR0Y + GetCouplerCompressionSlackAM() + Coupler2MTemporary; //  GetMaximumCouplerSlack3M > GetMaximumCouplerSlack2M
        }

        public override float GetCouplerBreak1N() 
        {
            if (Coupler == null)
            {
                return base.GetCouplerBreak1N();
            }
            return Coupler.Break1N;
        }

        public override float GetCouplerBreak2N() 
        {
            if (Coupler == null)
            {
                return base.GetCouplerBreak2N();
            }
            return Coupler.Break2N;
        }

        public override float GetCouplerTensionR0Y() 
        {
            if (Coupler == null)
            {
                return base.GetCouplerTensionR0Y();
            }
            return Coupler.TensionR0Y;
        }

        public override float GetCouplerCompressionR0Y()
        {
            if (Coupler == null)
            {
                return base.GetCouplerCompressionR0Y();
            }
            return Coupler.CompressionR0Y;
        }


        // TODO: This code appears to be being called by ReverseCars (in Trains.cs). 
        // Reverse cars moves the couplers along by one car, however this may be encountering a null coupler at end of train. 
        // Thus all coupler parameters need to be tested for null coupler and default values inserted (To be confirmed)
        public override void CopyCoupler(TrainCar other)
        {
            base.CopyCoupler(other);
            MSTSCoupling coupler = new MSTSCoupling();
            // Simple Coupler parameters
            coupler.R0X = other.GetCouplerZeroLengthM();
            coupler.R0Y = other.GetCouplerZeroLengthM();
            coupler.R0Diff = other.GetMaximumSimpleCouplerSlack1M();
            coupler.Stiffness1NpM = other.GetSimpleCouplerStiffnessNpM() / 7;
            coupler.Stiffness2NpM = 0;
            coupler.CouplerSlackAM = other.GetCouplerSlackAM();
            coupler.CouplerSlackBM = other.GetCouplerSlackBM();

            // Common simple and advanced parameters
            coupler.Rigid = other.GetCouplerRigidIndication();
            coupler.Break1N = other.GetCouplerBreak1N();
            coupler.Break2N = other.GetCouplerBreak2N();

            // ADvanced coupler parameters
            IsAdvancedCoupler = other.GetAdvancedCouplerFlag();

            coupler.TensionR0X = other.GetCouplerZeroLengthM();
            coupler.TensionR0Y = other.GetCouplerTensionR0Y();
            coupler.CouplerTensionSlackAM = other.GetCouplerTensionSlackAM();
            coupler.CouplerTensionSlackBM = other.GetCouplerTensionSlackBM();
            coupler.TensionStiffness1N = other.GetCouplerTensionStiffness1N();
            coupler.TensionStiffness2N = other.GetCouplerTensionStiffness2N();

            coupler.CompressionR0X = GetCouplerZeroLengthM();
            coupler.CompressionR0Y = other.GetCouplerCompressionR0Y();
            coupler.CouplerCompressionSlackAM = other.GetCouplerCompressionSlackAM();
            coupler.CouplerCompressionSlackBM = other.GetCouplerCompressionSlackBM();
            coupler.CompressionStiffness1N = other.GetCouplerCompressionStiffness1N();
            coupler.CompressionStiffness2N = other.GetCouplerCompressionStiffness2N();


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
        /// Starts a continuous increase in controlled value.
        /// </summary>
        /// <param name="type">Pickup point</param>
        public void StartRefillingOrUnloading(PickupObj matchPickup, IntakePoint intakePoint, float fraction, bool unload)
        {
            var type = matchPickup.PickupType;
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
                if (intakePoint.LinkedFreightAnim is FreightAnimationContinuous)
                    FreightAnimations.LoadedOne = (FreightAnimationContinuous)intakePoint.LinkedFreightAnim;
            }
            if (!unload)
            {
                controller.SetStepSize(matchPickup.PickupCapacity.FeedRateKGpS/ MSTSNotchController.StandardBoost / FreightAnimations.LoadedOne.FreightWeightWhenFull);
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Starting refill"));
                controller.StartIncrease(controller.MaximumValue);
            }
            else
            {
                controller.SetStepSize(-matchPickup.PickupCapacity.FeedRateKGpS / MSTSNotchController.StandardBoost / FreightAnimations.LoadedOne.FreightWeightWhenFull);
                WaitForAnimationReady = true;
                UnloadingPartsOpen = true;
                if (FreightAnimations.UnloadingStartDelay > 0)
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Preparing for unload"));
            }

        }


        /// <summary>
        /// Starts loading or unloading of a discrete load.
        /// </summary>
        /// <param name="type">Pickup point</param>
        public void StartLoadingOrUnloading(PickupObj matchPickup, IntakePoint intakePoint, bool unload)
        {
            var type = matchPickup.PickupType;
 /*           var controller = WeightLoadController;
            if (controller == null)
            {
                Simulator.Confirmer.Message(ConfirmLevel.Error, Simulator.Catalog.GetString("Incompatible data"));
                return;
            }
            controller.CommandStartTime = Simulator.ClockTime;  // for Replay to use */

            FreightAnimations.FreightType = (MSTSWagon.PickupType)type;

            var containerStation = Simulator.ContainerManager.ContainerHandlingItems.Where(item => item.Key == matchPickup.TrItemIDList[0].dbID).Select(item => item.Value).First();
            if (containerStation.Status != ContainerHandlingItem.ContainerStationStatus.Idle)
            {
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Container station busy with preceding mission"));
                return;
            }
            if (!unload)
            {
                if (containerStation.Containers.Count == 0)
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("No containers to load"));
                    return;
                }  
 //               var container = containerStation.Containers.Last();
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Starting load"));
                // immediate load at the moment
//                FreightAnimations.DiscreteLoadedOne.Container = container;
                 containerStation.PrepareForLoad((FreightAnimationDiscrete)intakePoint.LinkedFreightAnim);
 //               FreightAnimations.DiscreteLoadedOne.Loaded = true;
            }
            else
            {
                if (containerStation.Containers.Count >= containerStation.MaxStackedContainers * containerStation.StackLocationsCount)
                {
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Container station full, can't unload"));
                    return;
                }
                WaitForAnimationReady = true;
                UnloadingPartsOpen = true;
                if (FreightAnimations.UnloadingStartDelay > 0)
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Preparing for unload"));
                // immediate unload at the moment
                // switch from freightanimation to container
                containerStation.PrepareForUnload((FreightAnimationDiscrete)intakePoint.LinkedFreightAnim);
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
        public MSTSWagon.PickupType Type;          // 'freightgrain', 'freightcoal', 'freightgravel', 'freightsand', 'fuelcoal', 'fuelwater', 'fueldiesel', 'fuelwood', freightgeneral, freightlivestock, specialmail, container
        public float? DistanceFromFrontOfTrainM;
        public FreightAnimation LinkedFreightAnim = null;

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

        public bool Validity(bool onlyUnload, PickupObj pickup, ContainerManager containerManager, FreightAnimations freightAnimations, out ContainerHandlingItem containerStation)
        {
            var validity = false;
            containerStation = null;
            var load = LinkedFreightAnim as FreightAnimationDiscrete;
            // discrete freight wagon animation
            if (load == null)
                return validity;
            else
            {
                containerStation = containerManager.ContainerHandlingItems.Where(item => item.Key == pickup.TrItemIDList[0].dbID).Select(item => item.Value).First();
                if (containerStation.Containers.Count == 0 && !onlyUnload)
                    return validity;
            }
            if (load.Container != null && !onlyUnload)
                return validity;
            else if (load.Container == null && onlyUnload)
                return validity;
            if (freightAnimations.DoubleStacker)
            {
                if (onlyUnload)
                    for (var i = freightAnimations.Animations.Count - 1; i >= 0; i--)
                    {
                        if (freightAnimations.Animations[i] is FreightAnimationDiscrete discreteAnimation)
                            if (discreteAnimation.LoadPosition == LoadPosition.Above && load != discreteAnimation)
                                return validity;
                            else break;
                    }
            }
            if (!onlyUnload)
            {
                if (containerStation.Containers.Count == 0)
                    return validity;
                foreach (var stackLocation in containerStation.StackLocations)
                {
                    if (stackLocation.Containers?.Count > 0)
                    {
                        if (freightAnimations.Validity(load.Wagon, stackLocation.Containers[stackLocation.Containers.Count - 1],
                            load.LoadPosition, load.Offset, load.LoadingAreaLength, out Vector3 offset))
                            return true;
                    }
                }
                return validity;
            }
            if (onlyUnload)
            {
                validity = containerStation.CheckForEligibleStackPosition(load.Container);
            }
            else validity = true;
            return validity;
        }

    }

    public class MSTSCoupling
    {
        public bool Rigid;
        public float R0X;
        public float R0Y;
        public float R0Diff = 0.012f;
        public float Stiffness1NpM = 1e7f;
        public float Stiffness2NpM = 2e7f;
        public float Break1N = 1e10f;
        public float Break2N = 1e10f;
        public float CouplerSlackAM;
        public float CouplerSlackBM;
        public float CouplerTensionSlackAM;
        public float CouplerTensionSlackBM;
        public float TensionStiffness1N = 1e7f;
        public float TensionStiffness2N = 2e7f;
        public float TensionR0X;
        public float TensionR0Y;
        public float CompressionR0X;
        public float CompressionR0Y;
        public float CompressionStiffness1N;
        public float CompressionStiffness2N;
        public float CouplerCompressionSlackAM;
        public float CouplerCompressionSlackBM;


        public MSTSCoupling()
        {
        }
        public MSTSCoupling(MSTSCoupling copy)
        {
            Rigid = copy.Rigid;
            R0X = copy.R0X;
            R0Y = copy.R0Y;
            R0Diff = copy.R0Diff;
            Break1N = copy.Break1N;
            Break2N = copy.Break2N;
            Stiffness1NpM = copy.Stiffness1NpM;
            Stiffness2NpM = copy.Stiffness2NpM;
            CouplerSlackAM = copy.CouplerSlackAM;
            CouplerSlackBM = copy.CouplerSlackBM;
            TensionStiffness1N = copy.TensionStiffness1N;
            TensionStiffness2N = copy.TensionStiffness2N;
            CouplerTensionSlackAM = copy.CouplerTensionSlackAM;
            CouplerTensionSlackBM = copy.CouplerTensionSlackBM;
            TensionR0X = copy.TensionR0X;
            TensionR0Y = copy.TensionR0Y;
            CompressionR0X = copy.CompressionR0X;
            CompressionR0Y = copy.CompressionR0Y;
            CompressionStiffness1N = copy.CompressionStiffness1N;
            CompressionStiffness2N = copy.CompressionStiffness2N;
            CouplerCompressionSlackAM = copy.CouplerCompressionSlackAM;
            CouplerCompressionSlackBM = copy.CouplerCompressionSlackBM;
        }
        public void SetSimpleR0(float a, float b)
        {
                R0X = a;
                R0Y = b;
            if (a == 0)
                R0Diff = b / 2 * Stiffness2NpM / (Stiffness1NpM + Stiffness2NpM);
            else
                R0Diff = 0.012f;
            //               R0Diff = b - a;

            // Ensure R0Diff stays within "reasonable limits"
            if (R0Diff < 0.001)
                R0Diff = 0.001f;
            else if (R0Diff > 0.1) 
                R0Diff = 0.1f;

        }
        public void SetSimpleStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            Stiffness1NpM = a;
            Stiffness2NpM = b;
        }

        public void SetTensionR0(float a, float b)
        {
            TensionR0X = a;
            TensionR0Y = b;
        }

        public void SetCompressionR0(float a, float b)
        {
            CompressionR0X = a;
            CompressionR0Y = b;
        }

public void SetTensionStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            TensionStiffness1N = a;
            TensionStiffness2N = b;
        }

        public void SetCompressionStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            CompressionStiffness1N = a;
            CompressionStiffness2N = b;
        }

        public void SetTensionSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerTensionSlackAM = a;
            CouplerTensionSlackBM = b;
        }

        public void SetCompressionSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerCompressionSlackAM = a;
            CouplerCompressionSlackBM = b;
        }

        public void SetAdvancedBreak(float a, float b)
        {
            if (a + b < 0)
                return;

            Break1N = a;

            // Check if b = 0, as some stock has a zero value, set a default
            if (b == 0)
            {
                Break2N = 2e7f;
            }
            else
            {
                Break2N = b;
            }

        }


        public void SetSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerSlackAM = a;
            CouplerSlackBM = b;
        }

        public void SetSimpleBreak(float a, float b)
        {
            if (a + b < 0)
                return;

            Break1N = a;

            // Check if b = 0, as some stock has a zero value, set a default
            if ( b == 0)
            {
                Break2N = 2e7f;
            }
            else
            {
                Break2N = b;
            }
            
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public void Save(BinaryWriter outf)
        {
            outf.Write(Rigid);
            outf.Write(R0X);
            outf.Write(R0Y);
            outf.Write(R0Diff);
            outf.Write(Stiffness1NpM);
            outf.Write(Stiffness2NpM);
            outf.Write(CouplerSlackAM);
            outf.Write(CouplerSlackBM);
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
            R0X = inf.ReadSingle();
            R0Y = inf.ReadSingle();
            R0Diff = inf.ReadSingle();
            Stiffness1NpM = inf.ReadSingle();
            Stiffness2NpM = inf.ReadSingle();
            CouplerSlackAM = inf.ReadSingle();
            CouplerSlackBM = inf.ReadSingle();
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

        /// <summary>
        /// Sets the stale data flag for ALL loaded cars to the given bool
        /// (default true)
        /// </summary>
        public static void SetAllStale(bool stale = true)
        {
            foreach (MSTSWagon wagon in LoadedCars.Values)
                wagon.StaleData = stale;
        }

        /// <summary>
        /// Sets the stale data flag for train cars using any of the eng/wag from the given set of paths,
        /// with an optional set of include files to also mark sound files as stale
        /// </summary>
        /// <returns>bool indicating if any car changed from fresh to stale</returns>
        public static bool MarkStale(HashSet<string> wagPaths, HashSet<string> incPaths = null)
        {
            bool found = false;

            foreach (string wagKey in LoadedCars.Keys)
            {
                if (!LoadedCars[wagKey].StaleData)
                {
                    string ortsWagKey = Path.GetDirectoryName(wagKey) + @"\openrails\" + Path.GetFileName(wagKey);

                    if (wagPaths != null && (wagPaths.Contains(wagKey) || wagPaths.Contains(ortsWagKey)))
                    {
                        LoadedCars[wagKey].StaleData = true;
                        found = true;

                        Trace.TraceInformation("Train car {0} was updated on disk and will be reloaded.", wagKey);
                    }
                    else if (incPaths != null && LoadedCars[wagKey].FilesReferenced.Count > 1)
                    {
                        foreach (string fileRef in LoadedCars[wagKey].FilesReferenced)
                        {
                            if (incPaths.Contains(fileRef))
                            {
                                LoadedCars[wagKey].StaleData = true;
                                found = true;

                                Trace.TraceInformation("INC file {0} used by train car {1} was updated on disk, train car will be reloaded.", fileRef, wagKey);
                                break;
                            }
                        }
                    }
                }
                // Continue scanning next car, there may be multiple cars with stale include files
            }

            return found;
        }

        /// <summary>
        /// Sets the stale data flag for cabviews using any of the cvf from the given list of paths,
        /// with an optional set of include files to also mark sound files as stale
        /// </summary>
        /// <returns>bool indicating if any car changed from fresh to stale</returns>
        public static bool MarkCabsStale(HashSet<string> cvfPaths, HashSet<string> incPaths = null)
        {
            bool found = false;

            foreach (string wagKey in LoadedCars.Keys)
            {
                if (!LoadedCars[wagKey].StaleCab && LoadedCars[wagKey] is MSTSLocomotive loco)
                {
                    foreach (CabView cab in loco.CabViewList)
                    {
                        if (cvfPaths != null && cvfPaths.Contains(cab.CVFFile.CabFilePath))
                        {
                            LoadedCars[wagKey].StaleCab = true;
                            found = true;

                            Trace.TraceInformation("Cabview {0} was updated on disk and will be reloaded.", cab.CVFFile.CabFilePath);
                        }
                        else if (incPaths != null && cab.CVFFile.FilesReferenced.Count > 0)
                        {
                            foreach (string fileRef in cab.CVFFile.FilesReferenced)
                            {
                                if (incPaths.Contains(fileRef))
                                {
                                    LoadedCars[wagKey].StaleCab = true;
                                    found = true;

                                    Trace.TraceInformation("INC file {0} used by cabview {1} was updated on disk, cabview will be reloaded.", fileRef, cab.CVFFile.CabFilePath);
                                    break;
                                }
                            }
                        }
                        if (LoadedCars[wagKey].StaleCab)
                            break;
                    }
                }
                // Continue scanning next loco, there may be multiple locos with stale cabs
            }

            return found;
        }
    }

    public struct ParticleEmitterData
    {
        public readonly Vector3 XNALocation;
        public readonly Vector3 XNADirection;
        public readonly float NozzleWidth;

        public ParticleEmitterData(STFReader stf)
        {
            stf.MustMatch("(");
            XNALocation.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNALocation.Y = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNALocation.Z = -stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Y = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Z = -stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Normalize();
            NozzleWidth = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            stf.SkipRestOfBlock();
        }
    }
}
