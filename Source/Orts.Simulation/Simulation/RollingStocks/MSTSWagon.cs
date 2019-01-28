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
using Microsoft.Xna.Framework.Graphics;
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
        const float WaterLBpUKG = 10.0f;    // lbs of water in 1 gal (uk)
        float TempMassDiffRatio;

        // simulation parameters
        public float Variable1;  // used to convey status to soundsource
        public float Variable2;
        public float Variable3;

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
        public float WheelRadiusM = 1;          // provide some defaults in case it's missing from the wag
        protected float StaticFrictionFactorLb;    // factor to multiply friction by to determine static or starting friction - will vary depending upon whether roller or friction bearing
        float FrictionLowSpeedN;
        public float Friction0N;        // static friction
        protected float Friction5N;               // Friction at 5mph
        public float DavisAN;           // davis equation constant
        public float DavisBNSpM;        // davis equation constant for speed
        public float DavisCNSSpMM;      // davis equation constant for speed squared
        public float DavisDragConstant; // Drag coefficient for wagon
        public float WagonFrontalAreaM2; // Frontal area of wagon
        public float TrailLocoResistanceFactor; // Factor to reduce base and wind resistance if locomotive is not leading - based upon original Davis drag coefficients

        // Wind Impacts
        float WagonDirectionDeg;
        float WagonResultantWindComponentDeg;
        float WagonWindResultantSpeedMpS;

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

        // Colours for smoke and steam effects
        public Color ExhaustTransientColor = Color.Black;
        public Color ExhaustDecelColor = Color.WhiteSmoke;
        public Color ExhaustSteadyColor = Color.Gray;

        // Wagon steam leaks
        public float HeatingHoseParticleDurationS;
        public float HeatingHoseSteamVelocityMpS;
        public float HeatingHoseSteamVolumeM3pS;

        // Wagon Power Generator
        public float WagonGeneratorDurationS = 1.5f;
        public float WagonGeneratorVolumeM3pS = 2.0f;
        public Color WagonGeneratorSteadyColor = Color.Gray;

        // Heating Steam Boiler
        public float HeatingSteamBoilerDurationS;
        public float HeatingSteamBoilerVolumeM3pS;
        public Color HeatingSteamBoilerSteadyColor = Color.Aqua;

        // Wagon Smoke
        public float WagonSmokeVolumeM3pS;
        float InitialWagonSmokeVolumeM3pS = 3.0f;
        public float WagonSmokeDurationS;
        float InitialWagonSmokeDurationS = 1.0f;
        public float WagonSmokeVelocityMpS = 15.0f;
        public Color WagonSmokeSteadyColor = Color.Gray;


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

        /// <summary>
        /// Attached steam locomotive in case this wagon is an auxiliary tender
        /// </summary>
        public MSTSSteamLocomotive AuxTendersSteamLocomotive { get; private set; }

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
            SpecialMail = 14  // New to OR
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

        float LoadFullMassKg;
        float LoadFullORTSDavis_A;
        float LoadFullORTSDavis_B;
        float LoadFullORTSDavis_C;
        float LoadFullWagonFrontalAreaM2;
        float LoadFullDavisDragConstant;
        float LoadFullMaxBrakeForceN;
        float LoadFullMaxHandbrakeForceN;
        float LoadFullCentreOfGravityM_Y;


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

            // Initialise key wagon parameters
            MassKG = InitialMassKG;
            MaxHandbrakeForceN = InitialMaxHandbrakeForceN;
            MaxBrakeForceN = InitialMaxBrakeForceN;
            CentreOfGravityM = InitialCentreOfGravityM;

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
                else
                {
                    LoadEmptyORTSDavis_A = DavisAN;
                }

                if (FreightAnimations.EmptyORTSDavis_B > 0)
                {
                    LoadEmptyORTSDavis_B = FreightAnimations.EmptyORTSDavis_B;
                }
                else
                {
                    LoadEmptyORTSDavis_B = DavisBNSpM;
                }

                if (FreightAnimations.EmptyORTSDavis_C > 0)
                {
                    LoadEmptyORTSDavis_C = FreightAnimations.EmptyORTSDavis_C;
                }
                else
                {
                    LoadEmptyORTSDavis_C = DavisCNSSpMM;
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

                if (FreightAnimations.EmptyMaxBrakeForceN > 0)
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

                // Read (initialise) Static load ones if a static load
                // Test each value to make sure that it has been defined in the WAG file, if not default to Root WAG file value
                if (FreightAnimations.FullPhysicsStaticOne != null)
                {
                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_A > 0)
                    {
                        LoadFullORTSDavis_A = FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_A;
                    }
                    else
                    {
                        LoadFullORTSDavis_A = DavisAN;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_B > 0)
                    {
                        LoadFullORTSDavis_B = FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_B;
                    }
                    else
                    {
                        LoadFullORTSDavis_B = DavisBNSpM;
                    }

                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_C > 0)
                    {
                        LoadFullORTSDavis_C = FreightAnimations.FullPhysicsStaticOne.FullStaticORTSDavis_C;
                    }
                    else
                    {
                        LoadFullORTSDavis_C = DavisCNSSpMM;
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


                    if (FreightAnimations.FullPhysicsStaticOne.FullStaticMaxBrakeForceN > 0)
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
                    else
                    {
                        LoadFullORTSDavis_A = DavisAN;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_B > 0)
                    {
                        LoadFullORTSDavis_B = FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_B;
                    }
                    else
                    {
                        LoadFullORTSDavis_B = DavisBNSpM;
                    }

                    if (FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_C > 0)
                    {
                        LoadFullORTSDavis_C = FreightAnimations.FullPhysicsContinuousOne.FullORTSDavis_C;
                    }
                    else
                    {
                        LoadFullORTSDavis_C = DavisCNSSpMM;
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


                    if (FreightAnimations.FullPhysicsContinuousOne.FullMaxBrakeForceN > 0)
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
                }

                if (!FreightAnimations.MSTSFreightAnimEnabled) FreightShapeFileName = null;
                    if (FreightAnimations.WagonEmptyWeight != -1)
                    {

                        MassKG = FreightAnimations.WagonEmptyWeight + FreightAnimations.FreightWeight + FreightAnimations.StaticFreightWeight;

                        if (FreightAnimations.StaticFreightAnimationsPresent) // If it is static freight animation, set wagon physics to full wagon value
                        {
                            // Update brake parameters   
                            MaxBrakeForceN = LoadFullMaxBrakeForceN;
                            MaxHandbrakeForceN = LoadFullMaxHandbrakeForceN;

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
                    if (Math.Abs(InitialCentreOfGravityM.Z) > 1)
                    {
                        STFException.TraceWarning(stf, string.Format("Ignored CentreOfGravity Z value {0} outside range -1 to +1", InitialCentreOfGravityM.Z));
                        InitialCentreOfGravityM.Z = 0;
                    }
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsunbalancedsuperelevation": UnbalancedSuperElevationM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
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
                case "wagon(mass": InitialMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); if (InitialMassKG < 0.1f) InitialMassKG = 0.1f; break;
                case "wagon(wheelradius": WheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(wheelradius": DriverWheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(sound": MainSoundFileName = stf.ReadStringBlock(null); break;
                case "wagon(ortsbrakeshoefriction": BrakeShoeFrictionFactor = new Interpolator(stf); break;
                case "wagon(maxhandbrakeforce": InitialMaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(maxbrakeforce": InitialMaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortsdavis_a": DavisAN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortsdavis_b": DavisBNSpM = stf.ReadFloatBlock(STFReader.UNITS.Resistance, null); break;
                case "wagon(ortsdavis_c": DavisCNSSpMM = stf.ReadFloatBlock(STFReader.UNITS.ResistanceDavisC, null); break;
                case "wagon(ortsdavisdragconstant": DavisDragConstant = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortswagonfrontalarea": WagonFrontalAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "wagon(ortstraillocomotiveresistancefactor": TrailLocoResistanceFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(effects(specialeffects": ParseEffects(lowercasetoken, stf); break;
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
                case "wagon(coupling(spring(damping":
                    stf.MustMatch("(");
                    Couplers[Couplers.Count - 1].SetDamping(stf.ReadFloat(STFReader.UNITS.Resistance, null), stf.ReadFloat(STFReader.UNITS.Resistance, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(ortsslack":
                    stf.MustMatch("(");
                     // IsAdvancedCoupler = true; // If this parameter is present in WAG file then treat coupler as advanced ones.  Temporarily disabled for v1.3 release
                    Couplers[Couplers.Count - 1].SetSlack(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
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
                case "wagon(ortsexternalsoundpassedthroughpercent": ExternalSoundPassThruPercent = stf.ReadFloatBlock(STFReader.UNITS.None, -1); break;
                case "wagon(ortsalternatepassengerviewpoints": // accepted only if there is already a passenger viewpoint
                    if (HasInsideView)
                    {
                        ParseAlternatePassengerViewPoints(stf);
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
            FreightShapeFileName = copy.FreightShapeFileName;
            FreightAnimMaxLevelM = copy.FreightAnimMaxLevelM;
            FreightAnimMinLevelM = copy.FreightAnimMinLevelM;
            FreightAnimFlag = copy.FreightAnimFlag;
            CarWidthM = copy.CarWidthM;
            CarHeightM = copy.CarHeightM;
            CarLengthM = copy.CarLengthM;
            TrackGaugeM = copy.TrackGaugeM;
            CentreOfGravityM = copy.CentreOfGravityM;
            InitialCentreOfGravityM = copy.InitialCentreOfGravityM;
            UnbalancedSuperElevationM = copy.UnbalancedSuperElevationM;
            RigidWheelBaseM = copy.RigidWheelBaseM;
            AuxTenderWaterMassKG = copy.AuxTenderWaterMassKG;
            MassKG = copy.MassKG;
            InitialMassKG = copy.InitialMassKG;
            WheelRadiusM = copy.WheelRadiusM;
            DriverWheelRadiusM = copy.DriverWheelRadiusM;
            MainSoundFileName = copy.MainSoundFileName;
            BrakeShoeFrictionFactor = copy.BrakeShoeFrictionFactor;
            InitialMaxBrakeForceN = copy.InitialMaxBrakeForceN;
            InitialMaxHandbrakeForceN = copy.InitialMaxHandbrakeForceN;
            MaxBrakeForceN = copy.MaxBrakeForceN;
            MaxHandbrakeForceN = copy.MaxHandbrakeForceN;
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
            ExternalSoundPassThruPercent = copy.ExternalSoundPassThruPercent;
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
            LoadFullMassKg = copy.LoadFullMassKg;
            LoadFullCentreOfGravityM_Y = copy.LoadFullCentreOfGravityM_Y;
            LoadFullMaxBrakeForceN = copy.LoadFullMaxBrakeForceN;
            LoadFullMaxHandbrakeForceN = copy.LoadFullMaxHandbrakeForceN;
            LoadFullORTSDavis_A = copy.LoadFullORTSDavis_A;
            LoadFullORTSDavis_B = copy.LoadFullORTSDavis_B;
            LoadFullORTSDavis_C = copy.LoadFullORTSDavis_C;
            LoadFullDavisDragConstant = copy.LoadFullDavisDragConstant;
            LoadFullWagonFrontalAreaM2 = copy.LoadFullWagonFrontalAreaM2;

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

        // parses additional passenger viewpoints, if any
        protected void ParseAlternatePassengerViewPoints(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("ortsalternatepassengerviewpoint", ()=>{ ParseWagonInside(stf); }),
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
            outf.Write(MaxBrakeForceN);
            outf.Write(MaxHandbrakeForceN);
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
            MaxBrakeForceN = inf.ReadSingle();
            MaxHandbrakeForceN = inf.ReadSingle();
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

            ConfirmSteamLocomotiveTender(); // Confirms that a tender is connected to the steam locomotive

            UpdateTenderLoad(); // Updates the load physics characteristics of tender and aux tender

            UpdateLocomotiveLoadPhysics(); // Updates the load physics characteristics of locomotives

            UpdateSpecialEffects(elapsedClockSeconds); // Updates the special effects

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

            UpdateTrainBaseResistance();

            UpdateWindForce();

            //            Trace.TraceInformation("Coupler - ID {0} GetSlack1 {1:N4} GetSlack2 {2:N4} GetStiff {3:N3} GetZero {4:N3}", CarID, GetMaximumCouplerSlack1M(), GetMaximumCouplerSlack2M(), GetCouplerStiffnessNpM(), GetCouplerZeroLengthM());

            // Get Coupler HUD Indication
            HUDCouplerRigidIndication = GetCouplerRigidIndication();

            foreach (MSTSCoupling coupler in Couplers)
            {

                // Test to see if coupler forces have exceeded the Proof (or safety limit). Exceeding this limit will provide an indication only
                if (IsPlayerTrain)
                {
                    if (-CouplerForceU > coupler.Break1N)  // break couplers if forces exceeded
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
                    if (-CouplerForceU > coupler.Break2N )  // break couplers if forces exceeded
                    {
                        CouplerExceedBreakLimit = true;
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
                if (FreightAnimations.WagonEmptyWeight != -1) MassKG = FreightAnimations.WagonEmptyWeight + FreightAnimations.FreightWeight + FreightAnimations.StaticFreightWeight;
                if (WaitForAnimationReady && WeightLoadController.CommandStartTime + FreightAnimations.UnloadingStartDelay <= Simulator.ClockTime)
                {
                    WaitForAnimationReady = false;
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Starting unload"));
                    WeightLoadController.StartDecrease(WeightLoadController.MinimumValue);
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
                            MassKG = LoadEmptyMassKg + Kg.FromLb(SteamLocomotiveIdentification.BoilerMassLB) + SteamLocomotiveIdentification.FireMassKG + SteamLocomotiveIdentification.TenderCoalMassKG + Kg.FromLb(SteamLocomotiveIdentification.CombinedTenderWaterVolumeUKG * WaterLBpUKG);
                            MassKG = MathHelper.Clamp(MassKG, LoadEmptyMassKg, LoadFullMassKg); // Clamp Mass to between the empty and full wagon values   
                            // Adjust drive wheel weight
                            SteamLocomotiveIdentification.DrvWheelWeightKg = (MassKG / InitialMassKG) * SteamLocomotiveIdentification.InitialDrvWheelWeightKg;
                        }
                        else // locomotive must be a tender type locomotive
                        // This is a tender locomotive. A tender locomotive does not have any fuel onboard.
                        // Thus the loco weight only changes as boiler level goes up and down, and coal mass varies in the fire
                        {
                            MassKG = LoadEmptyMassKg + Kg.FromLb(SteamLocomotiveIdentification.BoilerMassLB) + SteamLocomotiveIdentification.FireMassKG;
                            MassKG = MathHelper.Clamp(MassKG, LoadEmptyMassKg, LoadFullMassKg); // Clamp Mass to between the empty and full wagon values        
                        // Adjust drive wheel weight
                            SteamLocomotiveIdentification.DrvWheelWeightKg = (MassKG / InitialMassKG) * SteamLocomotiveIdentification.InitialDrvWheelWeightKg;
                        }

                        // Update wagon physics parameters sensitive to wagon mass change
                        // Calculate the difference ratio, ie how full the wagon is. This value allows the relevant value to be scaled from the empty mass to the full mass of the wagon
                        float TempTenderMassDiffRatio = (MassKG - LoadEmptyMassKg) / (LoadFullMassKg - LoadEmptyMassKg);
                        // Update brake parameters
                        MaxBrakeForceN = ((LoadFullMaxBrakeForceN - LoadEmptyMaxBrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxBrakeForceN;
                        MaxHandbrakeForceN = ((LoadFullMaxHandbrakeForceN - LoadEmptyMaxHandbrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxHandbrakeForceN;
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

                        MassKG = LoadEmptyMassKg + (DieselLocomotiveIdentification.DieselLevelL * DieselLocomotiveIdentification.DieselWeightKgpL);
                        MassKG = MathHelper.Clamp(MassKG, LoadEmptyMassKg, LoadFullMassKg); // Clamp Mass to between the empty and full wagon values  
                        // Adjust drive wheel weight
                        DieselLocomotiveIdentification.DrvWheelWeightKg = (MassKG / InitialMassKG) * DieselLocomotiveIdentification.InitialDrvWheelWeightKg;

                        // Update wagon parameters sensitive to wagon mass change
                        // Calculate the difference ratio, ie how full the wagon is. This value allows the relevant value to be scaled from the empty mass to the full mass of the wagon
                        float TempTenderMassDiffRatio = (MassKG - LoadEmptyMassKg) / (LoadFullMassKg - LoadEmptyMassKg);
                        // Update brake parameters
                        MaxBrakeForceN = ((LoadFullMaxBrakeForceN - LoadEmptyMaxBrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxBrakeForceN;
                        MaxHandbrakeForceN = ((LoadFullMaxHandbrakeForceN - LoadEmptyMaxHandbrakeForceN) * TempMassDiffRatio) + LoadEmptyMaxHandbrakeForceN;
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


        private void UpdateTrainBaseResistance()
        {

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
                {
                    FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * DavisCNSSpMM);

                    // if this car is a locomotive, but not the lead one then recalculate the resistance with lower value as drag will not be as high on trailing locomotives
                    // Only the drag (C) factor changes if a trailing locomotive, so only running resistance, and not starting resistance needs to be corrected
                    if (WagonType == WagonTypes.Engine && Train.LeadLocomotive != this)
                    {
                        FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * (TrailLocoResistanceFactor * DavisCNSSpMM));
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
                            FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * (TrailLocoResistanceFactor * DavisCNSSpMM));
                        }

                    }

                }

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

                    // Calculation of resistance @ low speeds
                    // Wind resistance is not included at low speeds, as it does not have a significant enough impact
                    const float speed5 = 2.2352f; // 5 mph
                    Friction5N = DavisAN + speed5 * (DavisBNSpM + speed5 * DavisCNSSpMM); // Calculate friction @ 5 mph
                    Friction0N = N.FromLbf(Kg.ToTUS(MassKG) * StaticFrictionFactorLb); // Static friction is journal or roller bearing friction x factor
                    FrictionLowSpeedN = ((1.0f - (AbsSpeedMpS / speed5)) * (Friction0N - Friction5N)) + Friction5N; // Calculate friction below 5mph - decreases linearly with speed
                    FrictionForceN = FrictionLowSpeedN; // At low speed use this value

                }
                else
                {

                    FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * DavisCNSSpMM); // for normal speed operation

                    // if this car is a locomotive, but not the lead one then recalculate the resistance with lower value as drag will not be as high on trailing locomotives
                    // Only the drag (C) factor changes if a trailing locomotive, so only running resistance, and not starting resistance needs to be corrected
                    if (WagonType == WagonTypes.Engine && Train.LeadLocomotive != this)
                    {
                             FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * (TrailLocoResistanceFactor * DavisCNSSpMM));
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
                            FrictionForceN = DavisAN + AbsSpeedMpS * (DavisBNSpM + AbsSpeedMpS * (TrailLocoResistanceFactor * DavisCNSSpMM));
                        }

                    }

                }


#if DEBUG_FRICTION

                Trace.TraceInformation("========================== Debug Friction in MSTSWagon.cs ==========================================");
                Trace.TraceInformation("Stationary - CarID {0} Force0N {1} Force5N {2} Speed {3} Factor {4}", CarID, Friction0N, Friction5N, AbsSpeedMpS, StaticFrictionFactorLb);
                Trace.TraceInformation("Stationary - Mass {0} Mass (US-tons) {1}", MassKG, Kg.ToTUS(MassKG));
                Trace.TraceInformation("Stationary - Weather Type (1 for Snow) {0}", (int)Simulator.WeatherType);
                Trace.TraceInformation("Stationary - Force0 lbf {0} Force5 lbf {1}", N.ToLbf(Friction0N), N.ToLbf(Friction5N));

#endif

            }

        }

        private void UpdateWindForce()
        {

            // Calculate compensation for  wind
            // There are two components due to wind - 
            // Drag, impact of wind on train, will increase resistance when head on, will decrease resistance when acting as a tailwind.
            // Lateral resistance - due to wheel flange being pushed against rail due to side wind.
            // Calculation based upon information provided in AREA 1942 Proceedings - https://archive.org/details/proceedingsofann431942amer - pg 56

            if (Train.TrainWindResistanceDependent && !CarTunnelData.FrontPositionBeyondStartOfTunnel.HasValue && AbsSpeedMpS > 2.2352) // Only calculate wind resistance if option selected in options menu, and not in a tunnel, and speed is sufficient for wind effects (>5mph)
            {

                // Wagon Direction
                float direction = (float)Math.Atan2(WorldPosition.XNAMatrix.M13, WorldPosition.XNAMatrix.M11);
                WagonDirectionDeg = MathHelper.ToDegrees((float)direction);

                // If car is flipped, then the car's direction will be reversed by 180 compared to the rest of the train, and thus for calculation purposes only, 
                // it is necessary to reverse the "assumed" direction of the car back again. This shouldn't impact the visual appearance of the car.
                if (Flipped)
                {
                    WagonDirectionDeg += 180.0f; // Reverse direction of car
                    if (WagonDirectionDeg > 360) // If this results in an angle greater then 360, then convert it back to an angle between 0 & 360.
                    {
                        WagonDirectionDeg -= 360;
                    }
                }                   

                // If a westerly direction (ie -ve) convert to an angle between 0 and 360
                if (WagonDirectionDeg < 0)
                    WagonDirectionDeg += 360;

                float TrainSpeedMpS = Math.Abs(SpeedMpS);
                
                // Find angle between wind and direction of train
                if (Train.PhysicsWindDirectionDeg > WagonDirectionDeg)
                    WagonResultantWindComponentDeg = Train.PhysicsWindDirectionDeg - WagonDirectionDeg;
                else if (WagonDirectionDeg > Train.PhysicsWindDirectionDeg)
                    WagonResultantWindComponentDeg = WagonDirectionDeg - Train.PhysicsWindDirectionDeg;
                else
                    WagonResultantWindComponentDeg = 0.0f;

                // Correct wind direction if it is greater then 360 deg, then correct to a value less then 360
                if (Math.Abs(WagonResultantWindComponentDeg) > 360)
                    WagonResultantWindComponentDeg = WagonResultantWindComponentDeg - 360.0f;

                // Wind angle should be kept between 0 and 180 the formulas do not cope with angles > 180. If angle > 180, denotes wind of "other" side of train
                if (WagonResultantWindComponentDeg > 180)
                    WagonResultantWindComponentDeg = 360 - WagonResultantWindComponentDeg;

                float ResultantWindComponentRad = MathHelper.ToRadians(WagonResultantWindComponentDeg);

                // Find the resultand wind vector for the combination of wind and train speed
                WagonWindResultantSpeedMpS = (float)Math.Sqrt(TrainSpeedMpS * TrainSpeedMpS + Train.PhysicsWindSpeedMpS * Train.PhysicsWindSpeedMpS + 2.0f * TrainSpeedMpS * Train.PhysicsWindSpeedMpS * (float)Math.Cos(ResultantWindComponentRad));

                // Calculate Drag Resistance
                // The drag resistance will be the difference between the STILL firction calculated using the standard Davies equation, 
                // and that produced using the wind resultant speed (combination of wind speed and train speed)
                float TempStillDragResistanceForceN = AbsSpeedMpS * AbsSpeedMpS * DavisCNSSpMM;
                float TempCombinedDragResistanceForceN = WagonWindResultantSpeedMpS * WagonWindResultantSpeedMpS * DavisCNSSpMM; // R3 of Davis formula taking into account wind
                float WindDragResistanceForceN = 0.0f;

                // Find the difference between the Still and combined resistances
                // This difference will be added or subtracted from the overall friction force depending upon the estimated wind direction.
                // Wind typically headon to train - increase resistance - +ve differential
                if (TempCombinedDragResistanceForceN > TempStillDragResistanceForceN)
                {
                    WindDragResistanceForceN = TempCombinedDragResistanceForceN - TempStillDragResistanceForceN;
                }
                else // wind typically following train - reduce resistance - -ve differential
                {
                    WindDragResistanceForceN = TempStillDragResistanceForceN - TempCombinedDragResistanceForceN;
                    WindDragResistanceForceN *= -1.0f;  // Convert to negative number to allow subtraction from ForceN
                }

                // Calculate Lateral Resistance

                // Calculate lateral resistance due to wind
                // Resistance is due to the wheel flanges being pushed further onto rails when a cross wind is experienced by a train
                float A = Train.PhysicsWindSpeedMpS / AbsSpeedMpS;
                float C = (float)Math.Sqrt((1 + (A * A) + 2.0f * A * Math.Cos(ResultantWindComponentRad)));
                float WindConstant = 8.25f;
                float TrainSpeedMpH = Me.ToMi(pS.TopH(AbsSpeedMpS));
                float WindSpeedMpH = Me.ToMi(pS.TopH(Train.PhysicsWindSpeedMpS));

                float WagonFrontalAreaFt2 = Me2.ToFt2(WagonFrontalAreaM2);

                LateralWindForceN = N.FromLbf(WindConstant * A * (float)Math.Sin(ResultantWindComponentRad) * DavisDragConstant * WagonFrontalAreaFt2 * TrainSpeedMpH * TrainSpeedMpH * C);

                float LateralWindResistanceForceN = N.FromLbf(WindConstant * A * (float)Math.Sin(ResultantWindComponentRad) * DavisDragConstant * WagonFrontalAreaFt2 * TrainSpeedMpH * TrainSpeedMpH * C * Train.WagonCoefficientFriction);

                // if this car is a locomotive, but not the lead one then recalculate the resistance with lower C value as drag will not be as high on trailing locomotives
                if (WagonType == WagonTypes.Engine && Train.LeadLocomotive != this)
                {
                    LateralWindResistanceForceN *= TrailLocoResistanceFactor;
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
                        LateralWindResistanceForceN *= TrailLocoResistanceFactor;
                    }

                }

                    WindForceN = LateralWindResistanceForceN + WindDragResistanceForceN;

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

                    MassKG = FreightAnimations.WagonEmptyWeight + TendersSteamLocomotive.TenderCoalMassKG + Kg.FromLb( (TendersSteamLocomotive.CurrentLocoTenderWaterVolumeUKG * WaterLBpUKG));
                    MassKG = MathHelper.Clamp(MassKG, LoadEmptyMassKg, LoadFullMassKg); // Clamp Mass to between the empty and full wagon values   

                    // Update wagon parameters sensitive to wagon mass change
                    // Calculate the difference ratio, ie how full the wagon is. This value allows the relevant value to be scaled from the empty mass to the full mass of the wagon
                    float TempTenderMassDiffRatio = (MassKG - LoadEmptyMassKg) / (LoadFullMassKg - LoadEmptyMassKg);
                    // Update brake parameters
                    MaxBrakeForceN = ((LoadFullMaxBrakeForceN - LoadEmptyMaxBrakeForceN) * TempTenderMassDiffRatio) + LoadEmptyMaxBrakeForceN;
                    MaxHandbrakeForceN = ((LoadFullMaxHandbrakeForceN - LoadEmptyMaxHandbrakeForceN) * TempTenderMassDiffRatio) + LoadEmptyMaxHandbrakeForceN;
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

            // Update Steam Leaks Information
            if (Train.CarSteamHeatOn)
            {
                // Turn wagon steam leaks on 
                HeatingHoseParticleDurationS = 0.75f;
                HeatingHoseSteamVelocityMpS = 15.0f;
                HeatingHoseSteamVolumeM3pS = 4.0f;
            }
            else
            {
                // Turn wagon steam leaks off 
                HeatingHoseParticleDurationS = 0.0f;
                HeatingHoseSteamVelocityMpS = 0.0f;
                HeatingHoseSteamVolumeM3pS = 0.0f;
            }

            // Decrease wagon smoke as speed increases, smoke completely dissappears when wagon reaches 5MpS.
            float WagonSmokeMaxRise = -1.0f;
            float WagonSmokeMaxRun = 5.0f;
            float WagonSmokeGrad = WagonSmokeMaxRise / WagonSmokeMaxRun;

            float WagonSmokeRatio = (WagonSmokeGrad * AbsSpeedMpS) + 1.0f;
         //   WagonSmokeDurationS = InitialWagonSmokeDurationS * WagonSmokeRatio;
         //   WagonSmokeVolumeM3pS = InitialWagonSmokeVolumeM3pS * WagonSmokeRatio;
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
                }

                else if (tenderIndex > 0 && Train.Cars[tenderIndex - 1].WagonType == WagonTypes.Tender) // Assuming the tender is "in front" of the locomotive, ie it is running in reverse
                {
                    // TO BE CHECKED - What happens if multiple locomotives are coupled together in reverse?
                    SteamLocomotiveTender = Train.Cars[tenderIndex] as MSTSSteamLocomotive;
                    SteamLocomotiveTender.HasTenderCoupled = true;
                }
                else // Assuming that locomotive is a tank locomotive, and no tender is coupled
                {
                    SteamLocomotiveTender = Train.Cars[tenderIndex] as MSTSSteamLocomotive;
                    SteamLocomotiveTender.HasTenderCoupled = false;
                }
            }
        }

        public void FindAuxTendersSteamLocomotive()
        {
            // Find the steam locomotive associated with this wagon aux tender, this allows parameters processed in the steam loocmotive module to be used elsewhere
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
            get
            {
                if (Couplers.Count == 0) return null;
                if (Flipped && Couplers.Count > 1) return Couplers[1];
                return Couplers[0];
            }
        }
        public override float GetCouplerZeroLengthM()
        {
            if (Simulator.UseAdvancedAdhesion && IsAdvancedCoupler)
            {
                float zerolength;
                if (Coupler != null)
                {
                   zerolength = Coupler.CouplerSlackAM;
                }
                else
                {
                   zerolength = base.GetCouplerZeroLengthM();
                }

                // Ensure zerolength doesn't go higher then 0.15
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

        public override float GetCouplerStiffnessNpM()
        {
            return Coupler != null && Coupler.R0X == 0 ? 7 * (Coupler.Stiffness1NpM + Coupler.Stiffness2NpM) : base.GetCouplerStiffnessNpM();
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

        public override float GetCouplerDamping1NMpS()
        {
            if (Coupler == null)
            {
                return base.GetCouplerDamping1NMpS();
            }
            return Coupler.Damping1NMps;
        }

        public override float GetCouplerDamping2NMpS()
        {
            if (Coupler == null)
            {
                return base.GetCouplerDamping2NMpS();
            }
            return Coupler.Damping2NMps;
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

        public override int GetCouplerRigidIndication()
        {
            if (Coupler == null)
            {
                 return base.GetCouplerRigidIndication();   // If no coupler defined
            }
            return Coupler.Rigid ? 1 : 2; // Return whether coupler Rigid or Flexible
        }

        public override bool GetAdvancedCouplerFlag()
        {
            if (Coupler == null)
            {
                return base.GetAdvancedCouplerFlag();
            }
            return IsAdvancedCoupler;
        }

        public override float GetMaximumCouplerSlack0M()  // This limits the maximum amount of slack, and typically will be equal to y - x of R0 statement
        {
            if (Coupler == null)
                return base.GetMaximumCouplerSlack0M();
            return Coupler.Rigid ? 0.0001f : Coupler.CouplerSlackBM;
        }

        public override float GetMaximumCouplerSlack1M()  // This limits the maximum amount of slack, and typically will be equal to y - x of R0 statement
        {
            if (Simulator.UseAdvancedAdhesion && IsAdvancedCoupler)
            {
                if (Coupler == null)
                    return base.GetMaximumCouplerSlack1M();
                return Coupler.Rigid ? 0.0001f : Coupler.CouplerSlackBM + Coupler.R0X;

            }
            else
            {
                if (Coupler == null)
                    return base.GetMaximumCouplerSlack1M();
                return Coupler.Rigid ? 0.0001f : Coupler.R0Diff;
            }


        }

        public override float GetMaximumCouplerSlack2M() // This limits the slack due to draft forces (?) and should be marginally greater then GetMaximumCouplerSlack1M
        {
            if (Simulator.UseAdvancedAdhesion && IsAdvancedCoupler) // for Advanced coupler
            {
                if (Coupler == null)
                    return base.GetMaximumCouplerSlack2M();
                return Coupler.Rigid ? 0.0002f : Coupler.CouplerSlackBM + Coupler.R0Y; //  GetMaximumCouplerSlack2M > GetMaximumCouplerSlack1M
            }
            else  // for simple coupler
            {
                if (Coupler == null)
                    return base.GetMaximumCouplerSlack2M();
                return Coupler.Rigid ? 0.0002f : base.GetMaximumCouplerSlack2M(); //  GetMaximumCouplerSlack2M > GetMaximumCouplerSlack1M
            }


        }


        // TODO: This code appears to be being called by ReverseCars (in Trains.cs). 
        // Reverse cars moves the couplers along by one car, however this may be encountering a null coupler at end of train. 
        // Thus all coupler parameters need to be tested for null coupler and defasult values inserted (To be confirmed)
        public override void CopyCoupler(TrainCar other)
        {
            base.CopyCoupler(other);
            MSTSCoupling coupler = new MSTSCoupling();
            coupler.R0X = other.GetCouplerZeroLengthM();
            coupler.R0Y = other.GetCouplerZeroLengthM();
            coupler.R0Diff = other.GetMaximumCouplerSlack1M();
            coupler.Rigid = coupler.R0Diff < .0002f;
            coupler.Stiffness1NpM = other.GetCouplerStiffnessNpM() / 7;
            coupler.Stiffness2NpM = 0;
            coupler.Damping1NMps = other.GetCouplerDamping1NMpS();
            coupler.Damping2NMps = other.GetCouplerDamping2NMpS();
            coupler.CouplerSlackAM = other.GetCouplerSlackAM();
            coupler.CouplerSlackBM = other.GetCouplerSlackBM();
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
                FreightAnimations.LoadedOne = intakePoint.LinkedFreightAnim;
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
        public MSTSWagon.PickupType Type;          // 'freightgrain', 'freightcoal', 'freightgravel', 'freightsand', 'fuelcoal', 'fuelwater', 'fueldiesel', 'fuelwood', freightgeneral, freightlivestock, specialmail
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
        public float R0X;
        public float R0Y;
        public float R0Diff = .012f;
        public float Stiffness1NpM = 1e7f;
        public float Stiffness2NpM = 2e7f;
        public float Damping1NMps = 1e7f;
        public float Damping2NMps = 2e7f;
        public float Break1N = 1e10f;
        public float Break2N = 1e10f;
        public float CouplerSlackAM;
        public float CouplerSlackBM;

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
            Damping1NMps = copy.Damping1NMps;
            Damping2NMps = copy.Damping2NMps;
            CouplerSlackAM = copy.CouplerSlackAM;
            CouplerSlackBM = copy.CouplerSlackBM;
        }
        public void SetR0(float a, float b)
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
        public void SetStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            Stiffness1NpM = a;
            Stiffness2NpM = b;
        }

        public void SetDamping(float a, float b)
        {
            if (a + b< 0)
                return;

            Damping1NMps = a;
            Damping2NMps = b;
        }

        public void SetSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerSlackAM = a;
            CouplerSlackBM = b;
        }

        public void SetBreak(float a, float b)
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
            outf.Write(Damping1NMps);
            outf.Write(Damping2NMps);
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
            Damping1NMps = inf.ReadSingle();
            Damping2NMps = inf.ReadSingle();
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
