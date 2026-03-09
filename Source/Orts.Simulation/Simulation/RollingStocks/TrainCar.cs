// COPYRIGHT 2009 - 2022 by the Open Rails project.
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

// Define this to log the wheel configurations on cars as they are loaded.
//#define DEBUG_WHEELS

// Debug car heat losses
// #define DEBUG_CAR_HEATLOSS

// Debug curve speed
// #define DEBUG_CURVE_SPEED

//Debug Tunnel Resistance
//   #define DEBUG_TUNNEL_RESISTANCE

// Debug User SuperElevation
//#define DEBUG_USER_SUPERELEVATION

// Debug Brake Slide Calculations
//#define DEBUG_BRAKE_SLIDE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.Coupling;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using Orts.Simulation.Signalling;
using ORTS.Common;
using ORTS.Scripting.Api;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks
{
    public class ViewPoint
    {
        public Vector3 Location;
        public Vector3 StartDirection;
        public Vector3 RotationLimit;

        public ViewPoint()
        {
        }

        public ViewPoint(Vector3 location)
        {
            Location = location;
        }

        public ViewPoint(ViewPoint copy, bool rotate)
        {
            Location = copy.Location;
            StartDirection = copy.StartDirection;
            RotationLimit = copy.RotationLimit;
            if (rotate)
            {
                Location.X *= -1;
                Location.Z *= -1;
                /*StartDirection.X += 180;
                StartDirection.Z += 180;*/
            }
        }
    }

    public class PassengerViewPoint : ViewPoint
    {
        // Remember direction of passenger camera and apply when user returns to it.
        public float RotationXRadians;
        public float RotationYRadians;
    }

    public abstract class TrainCar
    {
        public readonly Simulator Simulator;
        public readonly string WagFilePath;
        public string RealWagFilePath; //we are substituting missing remote cars in MP, so need to remember this

        public static int DbfEvalTravellingTooFast;//Debrief eval
        public static int DbfEvalTravellingTooFastSnappedBrakeHose;//Debrief eval
        public bool dbfEvalsnappedbrakehose = false;//Debrief eval
        public bool ldbfevalcurvespeed = false;//Debrief eval
        static float dbfmaxsafecurvespeedmps;//Debrief eval
        public static int DbfEvalTrainOverturned;//Debrief eval
        public bool ldbfevaltrainoverturned = false;
                                        
        // original consist of which car was part (used in timetable for couple/uncouple options)
        public string OrgConsist = string.Empty;

        // sound related variables
        public bool IsPartOfActiveTrain = true;
        public List<int> SoundSourceIDs = new List<int>();

        public IPowerSupply PowerSupply;

        // Used to calculate Carriage Steam Heat Loss - ToDo - ctn_steamer - consolidate these parameters with other steam heat ones, also check as some now may be obsolete
        public Interpolator TrainHeatBoilerWaterUsageGalukpH;
        public Interpolator TrainHeatBoilerFuelUsageGalukpH;

        // Input values to allow the water and fuel usage of steam heating boiler to be calculated based upon Spanner SwirlyFlo Mk111 Boiler
        static float[] SteamUsageLbpH = new float[]
        {
           0.0f, 3000.0f
        };

        // Water Usage
        static float[] WaterUsageGalukpH = new float[]
        {
           0.0f, 300.0f
        };

        // Fuel usage
        static float[] FuelUsageGalukpH = new float[]
        {
           0.0f, 31.0f
        };

        public Interpolator BrakeShoeFrictionFactor;  // Factor of friction for wagon brake shoes

        public static Interpolator SteamHeatBoilerWaterUsageGalukpH()
        {
            return new Interpolator(SteamUsageLbpH, WaterUsageGalukpH);
        }

        public static Interpolator SteamHeatBoilerFuelUsageGalukpH()
        {
            return new Interpolator(SteamUsageLbpH, FuelUsageGalukpH);
        }

        public float BackPressurePSIG; // Back pressure in steam cylinder for sound system

        public float MainSteamHeatPipeOuterDiaM = Me.FromIn(2.4f); // Steel pipe OD = 1.9" + 0.5" insulation (0.25" either side of pipe)
        public float MainSteamHeatPipeInnerDiaM = Me.FromIn(1.50f); // Steel pipe ID = 1.5"
        public float CarConnectSteamHoseOuterDiaM = Me.FromIn(2.05f); // Rubber hose OD = 2.05"
        public float CarConnectSteamHoseInnerDiaM = Me.FromIn(1.50f); // Rubber hose ID = 1.5"
        public bool IsSteamHeatBoilerLockedOut = false;
        public float MaximumSteamHeatingBoilerSteamUsageRateLbpS;
        public float MaximiumSteamHeatBoilerFuelTankCapacityL = 1500.0f; // Capacity of the fuel tank for the steam heating boiler
        public float CurrentCarSteamHeatBoilerWaterCapacityL;  // Current water level
        public float CurrentSteamHeatBoilerFuelCapacityL;  // Current fuel level - only on steam vans, diesels use main diesel tank
        public float MaximumSteamHeatBoilerWaterTankCapacityL = L.FromGUK(800.0f); // Capacity of the water feed tank for the steam heating boiler
        public float CompartmentHeatingPipeAreaFactor = 3.0f;
        public float DesiredCompartmentTempSetpointC = C.FromF(55.0f); // This is the desired temperature for the passenger compartment heating
        public float WindowDeratingFactor = 0.275f;   // fraction of windows in carriage side - 27.5% of space are windows
        public bool SteamHeatingBoilerOn = false;
        public bool SteamHeatingCompartmentSteamTrapOn = false;
        public float TotalCarCompartmentHeatLossW;      // Transmission loss for the wagon
        public float CarHeatCompartmentPipeAreaM2;  // Area of surface of car pipe
        public bool IsCarHeatingInitialized = false; // Allow steam heat to be initialised.
        public float CarHeatSteamMainPipeHeatLossBTU;  // BTU /hr
        public float CarHeatConnectSteamHoseHeatLossBTU;
        public float CarSteamHeatMainPipeCurrentHeatBTU;
        public float CarSteamHeatMainPipeSteamPressurePSI;
        public float CarCompartmentSteamPipeHeatConvW;
        public float CarCompartmentSteamHeatPipeRadW;
        public bool CarHeatCompartmentHeaterOn = false;
        public float CarHeatSteamTrapUsageLBpS;
        public float CarHeatConnectingSteamHoseLeakageLBpS;
        public float SteamHoseLeakRateRandom;
        public float CarNetHeatFlowRateW;        // Net Steam loss - Loss in Cars vs Steam Pipe Heat
        public float CarHeatCompartmentSteamPipeHeatW; // Heat generated by steam exchange area in compartment
        public float CarHeatCurrentCompartmentHeatJ;
        public float CarInsideTempC;

        // some properties of this car
        public float CarWidthM = 2.5f;
        public float CarLengthM = 40;       // derived classes must overwrite these defaults
        public float CarHeightM = 4;        // derived classes must overwrite these defaults
        public (Vector3 Mins, Vector3 Maxes) ShapeBoundingLimits;
        public bool AutoSize = false;       // Are the dimensions of this wagon to be calculated automatically from the shape file?
        public Vector3 AutoSizeOffsetM;
        public int FrontArticulation = -1;  // -1: Determine front articulation automatically, 0: Force no front articulation, 1: Force front articulation
        public int RearArticulation = -1;   // -1: Determine rear articulation automatically, 0: Force no rear articulation, 1: Force rear articulation
        public float MassKG = 10000;        // Mass in KG at runtime; coincides with InitialMassKG if there is no load and no ORTS freight anim
        public float InitialMassKG = 10000;
        public bool IsDriveable;
        public bool HasFreightAnim = false;
        public bool HasPassengerCapacity = false;
        public bool HasInsideView = false;
        public float CarHeightAboveSeaLevelM;
        public float WagonNumBogies;
        public float CarBogieCentreLengthM;
        public float CarBodyLengthM;
        public float CarCouplerFaceLengthM;
        public float DerailmentCoefficient;
        public float NadalDerailmentCoefficient;
        public bool DerailmentCoefficientEnabled = true;
        public float MaximumWheelFlangeAngleRad;
        public float WheelFlangeLengthM;
        public float AngleOfAttackmRad;
        public float DerailClimbDistanceM;
        public bool DerailPossible = false;
        public bool DerailExpected = false;
        public float DerailElapsedTimeS;
        public bool HasDerailed = false;

        public float MaxHandbrakeForceN;
        public float MaxBrakeForceN = 89e3f;
        public float MaxBrakeShoeForceN; // This is the force applied to the brake shoe, hence it will be decreased by CoF to give force applied to the wheel
        public int NumberCarBrakeShoes;
        public float InitialMaxHandbrakeForceN;  // Initial force when agon initialised
        public float InitialMaxBrakeForceN = 89e3f;   // Initial force when wagon initialised, this is the force on the wheel, ie after the brake shoe.

        // Coupler Animation
        public AnimatedCoupler FrontCoupler = new AnimatedCoupler();
        public AnimatedCoupler RearCoupler = new AnimatedCoupler();

        // Air hose animation
        public AnimatedAirHose FrontAirHose = new AnimatedAirHose();
        public AnimatedAirHose RearAirHose = new AnimatedAirHose();

        public float CarAirHoseLengthM;
        public float CarAirHoseHorizontalLengthM;

        // Used to calculate Carriage Steam Heat Loss
        public const float BogieHeightM = 1.06f; // Height reduced by 1.06m to allow for bogies, etc
        public const float CarCouplingPipeM = 1.2f;  // Allow for connection between cars (assume 2' each end) - no heat is contributed to carriages.
        public const float SpecificHeatCapacityAirKJpKgK = 1.006f; // Specific Heat Capacity of Air
        public const float DensityAirKgpM3 = 1.247f;   // Density of air - use a av value
        public float CarHeatVolumeM3 { get => CarWidthM * (CarLengthM - CarCouplingPipeM) * (CarHeightM - BogieHeightM); } // Volume of car for heating purposes
        public float CarHeatPipeAreaM2;  // Area of surface of car pipe
        public float CarOutsideTempC;   // Ambient temperature outside of car
        public float InitialCarOutsideTempC;
        public bool IsTrainHeatingBoilerInitialised { get { return Train.TrainHeatingBoilerInitialised; } set { Train.TrainHeatingBoilerInitialised = value; } }
        public float ConvectionFactor
        {
            get
            {
                const float LowSpeedMpS = 2.0f;
                float ConvHeatTxfMinSpeed = 10.45f - LowSpeedMpS + (10.0f * (float)Math.Pow(LowSpeedMpS, 0.5));
                float ConvHeatTxActualSpeed = 10.45f - AbsSpeedMpS + (10.0f * (float)Math.Pow(AbsSpeedMpS, 0.5));
                float ConvFactor;

                if (AbsSpeedMpS >= LowSpeedMpS)
                {
                    ConvFactor = ConvHeatTxActualSpeed / ConvHeatTxfMinSpeed; // Calculate fraction
                }
                else
                {
                    ConvFactor = 1.0f; // If speed less then 2m/s then set fraction to give stationary Kc value 
                }
                ConvFactor = MathHelper.Clamp(ConvFactor, 1.0f, 1.6f); // Keep Conv Factor ratio within bounds - should not exceed 1.6.

                return ConvFactor;
            }
        }

        // Used to calculate wheel sliding for locked brake
        public bool WheelBrakeSlideProtectionFitted = false;
        public bool WheelBrakeSlideProtectionActive = false;
        public bool WheelBrakeSlideProtectionLimitDisabled = false;
        public float wheelBrakeSlideTimerResetValueS = 7.0f; // Set wsp time to 7 secs
        public float WheelBrakeSlideProtectionTimerS = 7.0f;
        public bool WheelBrakeSlideProtectionDumpValveLockout = false;

        public bool BrakeSkid = false;
        public bool BrakeSkidWarning = false;
        public bool HUDBrakeSkid = false;

        float WagonBrakeAdhesiveForceN; // The adhesive force existing on the wheels of the wagon
        public float SkidFriction = 0.08f; // Friction if wheel starts skidding - based upon wheel dynamic friction of approx 0.08
        public float HuDBrakeShoeFriction;

        public float AuxTenderWaterMassKG;    // Water mass in auxiliary tender
        public string AuxWagonType;           // Store wagon type for use with auxilary tender calculations

        public LightCollection Lights;
        public FreightAnimations FreightAnimations;
        public int Headlight;

        // instance variables set by train physics when it creates the traincar
        public Train Train;  // the car is connected to this train
                             //        public bool IsPlayerTrain { get { return Train.TrainType == ORTS.Train.TRAINTYPE.PLAYER ? true : false; } set { } }
        public bool IsPlayerTrain { get { return Train != null && Train.IsPlayerDriven; } set { } }
        public bool Flipped; // the car is reversed in the consist
        public int UiD;
        public string CarID = "AI"; //CarID = "0 - UID" if player train, "ActivityID - UID" if loose consist, "AI" if AI train

        // status of the traincar - set by the train physics after it calls TrainCar.Update()
        public WorldPosition WorldPosition = new WorldPosition();  // current position of the car
        public float DistanceM;  // running total of distance travelled - always positive, updated by train physics
        public float _SpeedMpS; // meters per second; updated by train physics, relative to direction of car  50mph = 22MpS
        public float _PrevSpeedMpS;
        public float AbsSpeedMpS; // Math.Abs(SpeedMps) expression is repeated many times in the subclasses, maybe this deserves a class variable
        public float WheelSpeedMpS;
        public float AbsWheelSpeedMpS; // Math.Abs(WheelSpeedMpS) is used frequently in the subclasses, maybe it's more efficient to compute it once
        public float CouplerSlackM;  // extra distance between cars (calculated based on relative speeds)
        public int HUDCouplerForceIndication = 0; // Flag to indicate whether coupler is 1 - pulling, 2 - pushing or 0 - neither
        public float CouplerSlack2M;  // slack calculated using draft gear force
        public bool IsAdvancedCoupler = false; // Flag to indicate that coupler is to be treated as an advanced coupler
        public float FrontCouplerSlackM; // Slack in car front coupler
        public float RearCouplerSlackM;  // Slack in rear coupler
        public TrainCar CarAhead;
        public TrainCar CarBehind;
        public Vector3 RearCouplerLocation;
        public int RearCouplerLocationTileX;
        public int RearCouplerLocationTileZ;
        public float AdvancedCouplerDynamicTensionSlackLimitM;   // Varies as coupler moves
        public float AdvancedCouplerDynamicCompressionSlackLimitM; // Varies as coupler moves

        public bool WheelSlip;  // true if locomotive wheels slipping
        public bool WheelSlipWarning;
        public bool HuDIsWheelSlipWarninq;
        public bool HuDIsWheelSlip;
        public bool WheelSkid;  // True if wagon wheels lock up.
        public float _AccelerationMpSS;
        protected IIRFilter AccelerationFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, 1.0f, 0.1f);

        // Wheel Bearing Temperature parameters
        public float WheelBearingTemperatureDegC = 40.0f;
        public string DisplayWheelBearingTemperatureStatus;
        public float WheelBearingTemperatureRiseTimeS = 0;
        public float HotBoxTemperatureRiseTimeS = 0;
        public float WheelBearingTemperatureDeclineTimeS = 0;
        public float InitialWheelBearingDeclineTemperatureDegC;
        public float InitialWheelBearingRiseTemperatureDegC;
        public float InitialHotBoxRiseTemperatureDegS;
        public bool WheelBearingFailed = false;
        public bool WheelBearingHot = false;
        public bool HotBoxActivated = false;
        public bool HotBoxHasBeenInitialized = false;
        public bool HotBoxSoundActivated = false;
        public float HotBoxDelayS;
        public float ActivityHotBoxDurationS;
        public float ActivityElapsedDurationS;
        public float HotBoxStartTimeS;

        bool RigidWheelBaseInitialised = false;

        // Setup for ambient temperature dependency
        Interpolator OutsideWinterTempbyLatitudeC;  // Interploator to calculate ambient Winter temperature based upon the latitude of the route
        Interpolator OutsideAutumnTempbyLatitudeC;  // Interploator to calculate ambient Autumn temperature based upon the latitude of the route
        Interpolator OutsideSpringTempbyLatitudeC;  // Interploator to calculate ambient Spring temperature based upon the latitude of the route
        Interpolator OutsideSummerTempbyLatitudeC;  // Interploator to calculate ambient Summer temperature based upon the latitude of the route
        public bool AmbientTemperatureInitialised;  // Flag to indicate that ambient temperature has been initialised

        // Input values to allow the temperature for different values of latitude to be calculated
        static float[] WorldLatitudeDeg = new float[]
        {
           -50.0f, -40.0f, -30.0f, -20.0f, -10.0f, 0.0f, 10.0f, 20.0f, 30.0f, 40.0f, 50.0f, 60.0f
        };

        // Temperature in deg Celcius
        static float[] WorldTemperatureWinter = new float[]
        {
            0.9f, 8.7f, 12.4f, 17.2f, 20.9f, 25.9f, 22.8f, 18.2f, 11.1f, 1.1f, -10.2f, -18.7f
         };

        static float[] WorldTemperatureAutumn = new float[]
        {
            7.5f, 13.7f, 18.8f, 22.0f, 24.0f, 26.0f, 25.0f, 21.6f, 21.0f, 14.3f, 6.0f, 3.8f
         };

        static float[] WorldTemperatureSpring = new float[]
        {
            8.5f, 13.1f, 17.6f, 18.6f, 24.6f, 25.9f, 26.8f, 23.4f, 18.5f, 12.6f, 6.1f, 1.7f
         };

        static float[] WorldTemperatureSummer = new float[]
        {
            13.4f, 18.3f, 22.8f, 24.3f, 24.4f, 25.0f, 25.2f, 22.5f, 26.6f, 24.8f, 19.4f, 14.3f
         };

        public static Interpolator WorldWinterLatitudetoTemperatureC()
        {
            return new Interpolator(WorldLatitudeDeg, WorldTemperatureWinter);
        }

        public static Interpolator WorldAutumnLatitudetoTemperatureC()
        {
            return new Interpolator(WorldLatitudeDeg, WorldTemperatureAutumn);
        }

        public static Interpolator WorldSpringLatitudetoTemperatureC()
        {
            return new Interpolator(WorldLatitudeDeg, WorldTemperatureSpring);
        }

        public static Interpolator WorldSummerLatitudetoTemperatureC()
        {
            return new Interpolator(WorldLatitudeDeg, WorldTemperatureSummer);
        }

        public bool AcceptMUSignals = true; //indicates if the car accepts multiple unit signals; no more used
        /// <summary>
        /// Indicates which remote control group the car is in.
        /// -1: unconnected, 0: sync/front group, 1: async/rear group
        /// </summary>
        public int RemoteControlGroup;
        public bool IsMetric;
        public bool IsUK;

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
            get { return _AccelerationMpSS; }
        }

        public float LocalThrottlePercent;
        public float MaxThrottlePercent
        {
            get
            {
                float percent = 100;
                if (RemoteControlGroup == 0 && Train != null && Train.LeadLocomotive is MSTSLocomotive locomotive)
                {
                    if (!locomotive.TrainControlSystem.TractionAuthorization)
                    {
                        percent = 0;
                    }
                    else if (percent > locomotive.TrainControlSystem.MaxThrottlePercent)
                    {
                        percent = Math.Max(locomotive.TrainControlSystem.MaxThrottlePercent, 0);
                    }
                }
                if (this is MSTSLocomotive loco)
                {
                    if (percent > 100 - loco.LocomotivePowerSupply.ThrottleReductionPercent) percent = 100 - loco.LocomotivePowerSupply.ThrottleReductionPercent;
                    if (percent > loco.LocomotivePowerSupply.MaxThrottlePercent) percent = loco.LocomotivePowerSupply.MaxThrottlePercent / 100;
                }
                return percent;
            }
        }
        // represents the MU line travelling through the train.  Uncontrolled locos respond to these commands.
        public float ThrottlePercent
        {
            get
            {
                float percent;
                if (RemoteControlGroup == 0 && Train != null)
                {
                    percent = Train.MUThrottlePercent;
                }
                else if (RemoteControlGroup == 1 && Train != null)
                {
                    percent = Train.DPThrottlePercent;
                }
                else
                {
                    percent = LocalThrottlePercent;
                }
                if (this is MSTSLocomotive loco)
                {
                    if (loco.LocomotivePowerSupply.ThrottleReductionPercent > 0) percent *= 1 - loco.LocomotivePowerSupply.ThrottleReductionPercent / 100;
                }
                return Math.Min(percent, MaxThrottlePercent);
            }
            set
            {
                if (RemoteControlGroup == 0 && Train != null)
                    Train.MUThrottlePercent = value;
                else
                    LocalThrottlePercent = value;
            }
        }

        public int LocalGearboxGearIndex;
        public int GearboxGearIndex
        {
            get
            {
                if (RemoteControlGroup >= 0)
                    return Train.MUGearboxGearIndex;
                else
                    return LocalGearboxGearIndex;
            }
            set
            {
                if (RemoteControlGroup >= 0)
                    Train.MUGearboxGearIndex = value;
                else
                    LocalGearboxGearIndex = value;
            }
        }

        public float LocalDynamicBrakePercent = -1;
        public float MaxDynamicBrakePercent
        {
            get
            {
                float percent = 100;
                if (RemoteControlGroup == 0 && Train != null && Train.LeadLocomotive is MSTSLocomotive locomotive)
                {
                    if (!locomotive.TrainControlSystem.DynamicBrakingAuthorization)
                    {
                        percent = 0;
                    }
                }
                return percent;
            }
        }
        public float DynamicBrakePercent
        {
            get
            {
                float percent = -1;
                if (RemoteControlGroup == 0 && Train != null)
                {
                    if (Train.LeadLocomotive is MSTSLocomotive locomotive)
                    {
                        if (locomotive.TrainControlSystem.FullDynamicBrakingOrder)
                        {
                            return 100;
                        }
                    }

                    percent = Train.MUDynamicBrakePercent;
                }
                else if (RemoteControlGroup == 1 && Train != null)
                {
                    percent = Train.DPDynamicBrakePercent;
                }
                else
                {
                    percent = LocalDynamicBrakePercent;
                }
                if (this is MSTSLocomotive loco)
                {
                    if (loco.DynamicBrakeBlendingPercent > percent) percent = loco.DynamicBrakeBlendingPercent;
                    if (loco.LocomotivePowerSupply.PowerSupplyDynamicBrakePercent > percent) percent = loco.LocomotivePowerSupply.PowerSupplyDynamicBrakePercent;
                }
                return percent;
            }
            set
            {
                if (RemoteControlGroup != -1 && Train != null)
                    Train.MUDynamicBrakePercent = value;
                else
                    LocalDynamicBrakePercent = value;
                if (Train != null && this == Train.LeadLocomotive)
                    LocalDynamicBrakePercent = value;
            }
        }
        public Direction Direction
        {
            //TODO: following code lines have been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
            // To achieve the same result with other means, without flipping trainset physics, the code lines probably should be changed
            get
            {
                if (IsDriveable && Train.IsActualPlayerTrain)
                {
                    var loco = this as MSTSLocomotive;
                    return Flipped ^ loco.UsingRearCab ? DirectionControl.Flip(Train.MUDirection) : Train.MUDirection;
                }
                else
                {
                    return Flipped ? DirectionControl.Flip(Train.MUDirection) : Train.MUDirection;
                }
            }
            set
            {
                var loco = this as MSTSLocomotive;
                Train.MUDirection = Flipped ^ loco.UsingRearCab ? DirectionControl.Flip(value) : value;
            }
        }
        public BrakeSystem BrakeSystem;

        public float PreviousSteamBrakeCylinderPressurePSI;

        // TrainCar.Update() must set these variables
        /// <summary>
        /// Force transmitted to rail, excluding brake and friction force
        /// Adhesion-corrected tractive force
        /// </summary>
        public float MotiveForceN;
        /// <summary>
        /// Tractive force generated by the engine(s)
        /// </summary>
        public float TractiveForceN = 0f;
        public float RollingFrictionForceN;
        // Gravity forces have negative values on rising grade. 
        // This means they have the same sense as the motive forces and will push the train downhill.
        public float GravityForceN;  // Newtons  - signed relative to direction of car.
        public float CurveForceN;   // Resistive force due to curve, in Newtons
        public float WindForceN;  // Resistive force due to wind
        public float TractionForceN = 0f;
        public float DynamicBrakeForceN = 0f; // Raw dynamic brake force for diesel and electric locomotives

        // Derailment variables
        public float TotalWagonVerticalDerailForceN; // Vertical force of wagon/car - essentially determined by the weight
        public float TotalWagonLateralDerailForceN;
        public float LateralWindForceN;
        public float WagonFrontCouplerAngleRad;
        public float WagonFrontCouplerBuffAngleRad;
        public float WagonRearCouplerAngleRad;
        public float WagonRearCouplerBuffAngleRad;
        public float CarTrackPlayM = Me.FromIn(2.0f);
        public float AdjustedWagonFrontCouplerAngleRad;
        public float AdjustedWagonRearCouplerAngleRad;
        public float WagonFrontCouplerCurveExtM;
        public float WagonRearCouplerCurveExtM;
        public float WagonCouplerAngleDerailRad;


        public bool BuffForceExceeded;

        // filter curve force for audio to prevent rapid changes.
        //private IIRFilter CurveForceFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, 1.0f, 0.9f);
        protected SmoothedData CurveForceFilter = new SmoothedData(0.75f);
        public float CurveForceNFiltered;


        protected SmoothedData CurveSquealAoAmRadFilter = new SmoothedData(0.75f);
        public float CurveSquealAoAmRadFiltered;

        // Track sound effects - joints
        public float TrackJointSoundTriggered;
        public float realTimeTrackJointDistanceM;
        bool carOnJointTriggered = false;
        int jointTrigger;
        float jointTriggerDelayedS = 0.1f; // Set delay to 0.1 seconds
        float jointSpeedMpS;
        public float SoundAxleCount;
        public float CarTrackControlledDistanceM = 0;
        public float CarTunnelDistanceM;

        // Track sound effects - switch / crossover
        public float TrackSwitchSoundTriggered;
        bool carOnSwitchTriggered = false;
        float switchTriggerDelayedS = 0.1f; // Set delay to 0.1 seconds
        public bool EnableCarOnXoverTrigger = false;
        public float carOnXoverTriggerDelayedS = 0.1f;
        public float TrackXoverSoundTriggered;

        public float TrackSoundInTunnelTriggered;
        bool CarInTunnel = false;

        public float TunnelForceN;  // Resistive force due to tunnel, in Newtons
        public float FrictionForceN; // in Newtons ( kg.m/s^2 ) unsigned, includes effects of curvature
        public float BrakeForceN;    // current braking force applied to slow train (Newtons) - will be impacted by wheel/rail friction
        public float BrakeRetardForceN;    // brake force applied to wheel by brakeshoe (Newtons) independent of friction wheel/rail friction
        public float BrakeShoeForceN;
        public float FrictionBrakeBlendingMaxForceN; // This is the maximum force for the friction barke when it is blended with the dynamic brake

        public bool IsRackRailway = false;
        public bool BrakeCogWheelFitted;

        // Sum of all the forces acting on a Traincar in the direction of driving.
        // MotiveForceN and GravityForceN act to accelerate the train. The others act to brake the train.
        public float TotalForceN; // 

        public string CarBrakeSystemType;

        public float CurrentElevationPercent;

        public bool CurveSpeedDependent;

        protected float MaxDurableSafeCurveSpeedMpS;

        // temporary values used to compute coupler forces
        public float CouplerForceA; // left hand side value below diagonal
        public float CouplerForceB; // left hand side value on diagonal
        public float CouplerForceC; // left hand side value above diagonal
        public float CouplerForceG; // temporary value used by solver
        public float CouplerForceR; // right hand side value
        public float CouplerForceU; // result
        public float ImpulseCouplerForceUN;
        public SmoothedData CouplerForceUSmoothed = new SmoothedData(1.0f);
        public float PreviousCouplerSlackM;
        public float SmoothedCouplerForceUN;
        public bool CouplerExceedBreakLimit; //true when coupler force is higher then Break limit (set by 2nd parameter in Break statement)
        public bool CouplerOverloaded; //true when coupler force is higher then Proof limit, thus overloaded, but not necessarily broken (set by 1nd parameter in Break statement)
        public bool BrakesStuck; //true when brakes stuck

        // set when model is loaded
        public List<WheelAxle> WheelAxles = new List<WheelAxle>();
        public bool WheelAxlesLoaded;
        public List<TrainCarPart> Parts = new List<TrainCarPart>();
        public float[] BogieZOffsets;
        public float BogiePivotHeightM;

        // For use by cameras, initialized in MSTSWagon class and its derived classes
        public List<PassengerViewPoint> PassengerViewpoints = new List<PassengerViewPoint>();
        public List<PassengerViewPoint> CabViewpoints; //three dimensional cab view point
        public List<ViewPoint> HeadOutViewpoints = new List<ViewPoint>();

        // Used by Curve Speed Method
        protected float TrackGaugeM;  // Track gauge - read in MSTSWagon, otherwise uses value given by the route
        protected Vector3 InitialCentreOfGravityM = new Vector3(0, 1.8f, 0); // get centre of gravity - read in MSTSWagon
        public Vector3 CentreOfGravityM = new Vector3(0, 1.8f, 0); // get centre of gravity after adjusted for freight animation
        protected bool AutoCenter = false; // Should CentreOfGravityM.Z be set automatically to center the wagon?
        public float SuperElevationM; // Super elevation on the curve
        protected float MaxUnbalancedSuperElevationM;  // Maximum comfortable cant deficiency, read from MSTS Wagon File
        public float SuperElevationAngleRad;
        protected bool IsMaxSafeCurveSpeed = false; // Has equal loading speed around the curve been exceeded, ie are all the wheesl still on the track?
        protected float ComfortTolerance = 1.0f; // Tolerance for discomfort due to excess curve speed
        public bool IsCriticalMaxSpeed = false; // Has the critical maximum speed around the curve been reached, is the wagon about to overturn?
        public bool IsCriticalMinSpeed = false; // Is the speed less then the minimum required for the wagon to travel around the curve
        protected float StartCurveResistanceFactor = 2.0f; // Set curve friction at Start = 200%
        protected float RouteSpeedMpS; // Max Route Speed Limit
        public const float GravitationalAccelerationMpS2 = 9.80665f; // Acceleration due to gravity 9.80665 m/s2
        protected int WagonNumAxles; // Number of axles on a wagon
        protected int InitWagonNumAxles; // Initial read of number of axles on a wagon
        protected float MSTSWagonNumWheels; // Number of axles on a wagon - used to read MSTS value as default
        public int LocoNumDrvAxles; // Number of drive axles on locomotive
        protected float MSTSLocoNumDrvWheels; // Number of drive axles on locomotive - used to read MSTS value as default
        public float DriverWheelRadiusM = Me.FromIn(30.0f); // Drive wheel radius of locomotive wheels - Wheel radius of loco drive wheels can be anywhere from about 10" to 40".
                public enum SteamEngineTypes
        {
            Unknown,
            Simple,
            Geared,
            Compound,
        }

        public SteamEngineTypes SteamEngineType;

        public enum SandingSystemTypes
        {
            Unknown,
            Air,
            Steam,
            Gravity,
        }

        public SandingSystemTypes SandingSystemType;

        public enum WagonTypes
        {
            Unknown,
            Engine,
            Tender,
            Passenger,
            Freight,
            EOT,
        }
        public WagonTypes WagonType;

        public enum EngineTypes
        {
            Steam,
            Diesel,
            Electric,
            Control,
        }
        public EngineTypes EngineType;

        public enum WagonSpecialTypes
        {
            Unknown,
            HeatingBoiler,
            Heated,
            PowerVan,
        }
        public WagonSpecialTypes WagonSpecialType;

        public enum BrakeShoeTypes
        {
            Unknown,
            Cast_Iron_P6,
            Cast_Iron_P10,
            Disc_Pads,
            High_Friction_Composite,
            User_Defined,
        }
        public BrakeShoeTypes BrakeShoeType;

        protected float CurveResistanceZeroSpeedFactor = 0.5f; // Based upon research (Russian experiments - 1960) the older formula might be about 2x actual value
        protected float RigidWheelBaseM;   // Vehicle rigid wheelbase, read from MSTS Wagon file
        protected float TrainCrossSectionAreaM2; // Cross sectional area of the train
        protected float DoubleTunnelCrossSectAreaM2;
        protected float SingleTunnelCrossSectAreaM2;
        protected float DoubleTunnelPerimeterM;
        protected float SingleTunnelPerimeterAreaM;
        protected float TunnelCrossSectionAreaM2 = 0.0f;
        protected float TunnelPerimeterM = 0.0f;

        // used by tunnel processing
        public struct CarTunnelInfoData
        {
            public float? FrontPositionBeyondStartOfTunnel;          // position of front of wagon wrt start of tunnel
            public float? LengthMOfTunnelAheadFront;                 // Length of tunnel remaining ahead of front of wagon (negative if front of wagon out of tunnel)
            public float? LengthMOfTunnelBehindRear;                 // Length of tunnel behind rear of wagon (negative if rear of wagon has not yet entered tunnel)
            public int numTunnelPaths;                               // Number of paths through tunnel
        }

        public CarTunnelInfoData CarTunnelData;

        public virtual void Initialize()
        {
            BrakeSystem.Initialize();
            CurveSpeedDependent = Simulator.Settings.CurveSpeedDependent;

            // Check Brake Shoe Friction parameters
            if (BrakeShoeType == BrakeShoeTypes.Cast_Iron_P10 || BrakeShoeType == BrakeShoeTypes.Cast_Iron_P6 || BrakeShoeType == BrakeShoeTypes.High_Friction_Composite || BrakeShoeType == BrakeShoeTypes.Disc_Pads)
            {
                float NewtonsTokNewtons = 0.001f;
                float maxBrakeShoeForcekN = NewtonsTokNewtons * MaxBrakeShoeForceN / NumberCarBrakeShoes;

                if (maxBrakeShoeForcekN > 20 && Simulator.Settings.VerboseConfigurationMessages)
                {
                    Trace.TraceInformation("Maximum force per brakeshoe is {0} and has exceeded {1}, check MaxBrakeShoeForceN {2} or NumberCarBrakeShoes {3}", FormatStrings.FormatForce(maxBrakeShoeForcekN * 1000, IsMetric), FormatStrings.FormatForce(20 * 1000, IsMetric), FormatStrings.FormatForce(MaxBrakeShoeForceN, IsMetric), NumberCarBrakeShoes);
                }
            }

            //CurveForceFilter.Initialize();

            // Initialize tunnel resistance values
            DoubleTunnelCrossSectAreaM2 = (float)Simulator.TRK.Tr_RouteFile.DoubleTunnelAreaM2;
            SingleTunnelCrossSectAreaM2 = (float)Simulator.TRK.Tr_RouteFile.SingleTunnelAreaM2;
            DoubleTunnelPerimeterM = (float)Simulator.TRK.Tr_RouteFile.DoubleTunnelPerimeterM;
            SingleTunnelPerimeterAreaM = (float)Simulator.TRK.Tr_RouteFile.SingleTunnelPerimeterM;

            // get route speed limit
            RouteSpeedMpS = (float)Simulator.TRK.Tr_RouteFile.SpeedLimit;

            // if no values are in TRK file, calculate default values.
            // Single track Tunnels

            if (SingleTunnelCrossSectAreaM2 == 0)
            {

                if (RouteSpeedMpS >= 97.22) // if route speed greater then 350km/h
                {
                    SingleTunnelCrossSectAreaM2 = 70.0f;
                    SingleTunnelPerimeterAreaM = 32.0f;
                }
                else if (RouteSpeedMpS >= 69.4 && RouteSpeedMpS < 97.22) // Route speed greater then 250km/h and less then 350km/h
                {
                    SingleTunnelCrossSectAreaM2 = 70.0f;
                    SingleTunnelPerimeterAreaM = 32.0f;
                }
                else if (RouteSpeedMpS >= 55.5 && RouteSpeedMpS < 69.4) // Route speed greater then 200km/h and less then 250km/h
                {
                    SingleTunnelCrossSectAreaM2 = 58.0f;
                    SingleTunnelPerimeterAreaM = 28.0f;
                }
                else if (RouteSpeedMpS >= 44.4 && RouteSpeedMpS < 55.5) // Route speed greater then 160km/h and less then 200km/h
                {
                    SingleTunnelCrossSectAreaM2 = 50.0f;
                    SingleTunnelPerimeterAreaM = 25.5f;
                }
                else if (RouteSpeedMpS >= 33.3 && RouteSpeedMpS < 44.4) // Route speed greater then 120km/h and less then 160km/h
                {
                    SingleTunnelCrossSectAreaM2 = 42.0f;
                    SingleTunnelPerimeterAreaM = 22.5f;
                }
                else       // Route speed less then 120km/h
                {
                    SingleTunnelCrossSectAreaM2 = 21.0f;  // Typically older slower speed designed tunnels
                    SingleTunnelPerimeterAreaM = 17.8f;
                }
            }

            // Double track Tunnels

            if (DoubleTunnelCrossSectAreaM2 == 0)
            {

                if (RouteSpeedMpS >= 97.22) // if route speed greater then 350km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 100.0f;
                    DoubleTunnelPerimeterM = 37.5f;
                }
                else if (RouteSpeedMpS >= 69.4 && RouteSpeedMpS < 97.22) // Route speed greater then 250km/h and less then 350km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 100.0f;
                    DoubleTunnelPerimeterM = 37.5f;
                }
                else if (RouteSpeedMpS >= 55.5 && RouteSpeedMpS < 69.4) // Route speed greater then 200km/h and less then 250km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 90.0f;
                    DoubleTunnelPerimeterM = 35.0f;
                }
                else if (RouteSpeedMpS >= 44.4 && RouteSpeedMpS < 55.5) // Route speed greater then 160km/h and less then 200km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 80.0f;
                    DoubleTunnelPerimeterM = 34.5f;
                }
                else if (RouteSpeedMpS >= 33.3 && RouteSpeedMpS < 44.4) // Route speed greater then 120km/h and less then 160km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 76.0f;
                    DoubleTunnelPerimeterM = 31.0f;
                }
                else       // Route speed less then 120km/h
                {
                    DoubleTunnelCrossSectAreaM2 = 41.8f;  // Typically older slower speed designed tunnels
                    DoubleTunnelPerimeterM = 25.01f;
                }
            }

#if DEBUG_TUNNEL_RESISTANCE
                Trace.TraceInformation("================================== TrainCar.cs - Tunnel Resistance Initialisation ==============================================================");
                Trace.TraceInformation("Tunnel 1 tr perimeter {0} Tunnel 1 tr area {1}", SingleTunnelPerimeterAreaM, SingleTunnelPerimeterAreaM);
                Trace.TraceInformation("Tunnel 2 tr perimeter {0} Tunnel 2 tr area {1}", DoubleTunnelPerimeterM, DoubleTunnelCrossSectAreaM2);
#endif

            realTimeTrackJointDistanceM = (float)Simulator.TRK.Tr_RouteFile.DistanceBetweenTrackJointsM; // Initialise track joint distance
            SoundAxleCount = (LocoNumDrvAxles + WagonNumAxles);

            // make sure that axle count does not exceed maximum possible trigger
            if (SoundAxleCount > 8)
            {
                SoundAxleCount = 8f;
            }

        }

        // called when it's time to update the MotiveForce and FrictionForce
        public virtual void Update(float elapsedClockSeconds)
        {
            // Initialize RigidWheelBaseM in first loop if not defined in ENG file, then ignore
            if (RigidWheelBaseM == 0 && !RigidWheelBaseInitialised)   // Calculate default values if no value in Wag File
            {
                int Axles = WheelAxles.Sum(w => w.Fake ? 0 : 1); // Only consider real axles
                int Bogies = Parts.Sum(p => p.Bogie ? 1 : 0);
                int BogieSize = Axles;
                if (Bogies > 0)
                    BogieSize = (int)(WheelAxles.Sum(w => !w.Fake && w.Part.Bogie ? 1 : 0) / Bogies); // Only consider axles attached to bogies

                RigidWheelBaseM = 1.6764f;       // Set a default in case no option is found - assume a standard 4 wheel (2 axle) bogie - wheel base - 5' 6" (1.6764m)

                // Calculate the number of axles in a car

                if (WagonType != WagonTypes.Engine)   // if car is not a locomotive then determine wheelbase
                {
                    if (Bogies < 2)  // if less then two bogies assume that it is a fixed wheelbase wagon
                    {
                        if (Axles == 2)
                        {
                            RigidWheelBaseM = 3.5052f;       // Assume a standard 4 wheel (2 axle) wagon - wheel base - 11' 6" (3.5052m)
                        }
                        else if (Axles == 3)
                        {
                            RigidWheelBaseM = 3.6576f;       // Assume a standard 6 wheel (3 axle) wagon - wheel base - 12' 2" (3.6576m)
                        }

                        if (Simulator.Settings.VerboseConfigurationMessages)
                        {
                            Trace.TraceInformation("Rigid Wheelbase of CarID {0} set to {1} for number of axles {2}", CarID, FormatStrings.FormatVeryShortDistanceDisplay(RigidWheelBaseM, IsMetric), Axles);
                        }

                    }
                    else if (Bogies == 2)
                    {
                        if (BogieSize == 2)
                        {
                            if (WagonType == WagonTypes.Passenger)
                            {
                                RigidWheelBaseM = 2.4384f;       // Assume a standard 4 wheel passenger bogie (2 axle) wagon - wheel base - 8' (2.4384m)
                            }
                            else
                            {
                                RigidWheelBaseM = 1.6764f;       // Assume a standard 4 wheel freight bogie (2 axle) wagon - wheel base - 5' 6" (1.6764m)
                            }
                        }
                        else if (BogieSize == 3)
                        {
                            RigidWheelBaseM = 3.6576f;       // Assume a standard 6 wheel bogie (3 axle) wagon - wheel base - 12' 2" (3.6576m)
                        }

                        if (Simulator.Settings.VerboseConfigurationMessages)
                        {
                            Trace.TraceInformation("Rigid Wheelbase of CarID {0} set to {1} for number of axles {2}", CarID, FormatStrings.FormatVeryShortDistanceDisplay(RigidWheelBaseM, IsMetric), BogieSize);
                        }

                    }
                }
                else if (WagonType == WagonTypes.Engine)   // if car is a locomotive and either a diesel or electric then determine wheelbase
                {
                    if (EngineType != EngineTypes.Steam)  // Assume that it is a diesel or electric locomotive
                    {
                        if (BogieSize == 2)
                        {
                            RigidWheelBaseM = 1.6764f;       // Set a default in case no option is found - assume a standard 4 wheel (2 axle) bogie - wheel base - 5' 6" (1.6764m)
                        }
                        else if (BogieSize == 3)
                        {
                            RigidWheelBaseM = 3.5052f;       // Assume a standard 6 wheel bogie (3 axle) locomotive - wheel base - 11' 6" (3.5052m)
                        }

                        if (Simulator.Settings.VerboseConfigurationMessages)
                        {
                            Trace.TraceInformation("Rigid Wheelbase of CarID {0} set to {1} for number of axles {2}", CarID, FormatStrings.FormatVeryShortDistanceDisplay(RigidWheelBaseM, IsMetric), BogieSize);
                        }
                    }
                    else // assume steam locomotive
                    {

                        if (LocoNumDrvAxles >= Axles) // Test to see if ENG file value is too big (typically doubled)
                        {
                            LocoNumDrvAxles = LocoNumDrvAxles / 2;  // Appears this might be the number of wheels rather then the axles.
                        }

                        //    Approximation for calculating rigid wheelbase for steam locomotives
                        // Wheelbase = 1.25 x (Loco Drive Axles - 1.0) x Drive Wheel diameter

                        RigidWheelBaseM = 1.25f * (LocoNumDrvAxles - 1.0f) * (DriverWheelRadiusM * 2.0f);

                        if (Simulator.Settings.VerboseConfigurationMessages)
                        {
                            Trace.TraceInformation("Rigid Wheelbase of CarID {0} set to {1} for number of axles {2}", CarID, FormatStrings.FormatVeryShortDistanceDisplay(RigidWheelBaseM, IsMetric), LocoNumDrvAxles);
                        }
                    }
                }

                RigidWheelBaseInitialised = true; // Don't process again
            }


            // Initialise ambient temperatures on first initial loop, then ignore
            if (!AmbientTemperatureInitialised)
            {
                InitializeCarTemperatures();
                AmbientTemperatureInitialised = true;
            }

            // Update temperature variation for height of car above sea level
            // Typically in clear conditions there is a 9.8 DegC variation for every 1000m (1km) rise, in snow/rain there is approx 5.5 DegC variation for every 1000m (1km) rise
            float TemperatureHeightVariationDegC = 0;
            const float DryLapseTemperatureC = 9.8f;
            const float WetLapseTemperatureC = 5.5f;

            if (Simulator.WeatherType == WeatherType.Rain || Simulator.WeatherType == WeatherType.Snow) // Apply snow/rain height variation
            {
                TemperatureHeightVariationDegC = Me.ToKiloM(CarHeightAboveSeaLevelM) * WetLapseTemperatureC;
            }
            else  // Apply dry height variation
            {
                TemperatureHeightVariationDegC = Me.ToKiloM(CarHeightAboveSeaLevelM) * DryLapseTemperatureC;
            }

            TemperatureHeightVariationDegC = MathHelper.Clamp(TemperatureHeightVariationDegC, 0.00f, 30.0f);

            CarOutsideTempC = InitialCarOutsideTempC - TemperatureHeightVariationDegC;

            AbsSpeedMpS = Math.Abs(_SpeedMpS);

            // Update Track based sound flags for joints,curve squeal and switches
            AngleOfAttackmRad = GetAngleofAttackmRad();

            CurveSquealAoAmRadFilter.Update(elapsedClockSeconds, AngleOfAttackmRad);
            CurveSquealAoAmRadFiltered = CurveSquealAoAmRadFilter.SmoothedValue;

            TrackJointSoundTriggered = GetTrackJointPosition(elapsedClockSeconds);

            TrackSwitchSoundTriggered = GetTrackSwitchTrigger(elapsedClockSeconds);

            if (IsOverCrossover)
            {
                TrackXoverSoundTriggered = 1;
            }
            else
            {
                TrackXoverSoundTriggered = 0;
            }

            UpdateCurveSpeedLimit(elapsedClockSeconds);
            UpdateCurveForce(elapsedClockSeconds);
            UpdateTunnelForce();
            UpdateBrakeSlideCalculation();
            UpdateTrainDerailmentRisk(elapsedClockSeconds);

            // Update tunnel track sounds allows tunnel sound to increase in volume when train enters tunnel, and decrease in volume when train leaves tunnel
            // Maximum distance set to 25 meters
            if (CarInTunnel)
            {
                TrackSoundInTunnelTriggered = 1; // set rigger for sound to turn on
                if (CarTunnelDistanceM < 25) // calculate distance
                {
                    CarTunnelDistanceM += elapsedClockSeconds * AbsSpeedMpS;
                }
                else
                {
                    CarTunnelDistanceM = 25;
                }
            }
            else if (!CarInTunnel)
            {
                // Count down sound volume, then reset tunnel trigger
                if (CarTunnelDistanceM > 0)
                {
                    CarTunnelDistanceM -= elapsedClockSeconds * AbsSpeedMpS;
                }
                else
                {
                    CarTunnelDistanceM = 0;
                    TrackSoundInTunnelTriggered = 0;
                }
            }

            // acceleration
            if (elapsedClockSeconds > 0.0f)
            {
                _AccelerationMpSS = (_SpeedMpS - _PrevSpeedMpS) / elapsedClockSeconds;

                if (Simulator.UseAdvancedAdhesion && !Simulator.Settings.SimpleControlPhysics)
                    _AccelerationMpSS = AccelerationFilter.Filter(_AccelerationMpSS, elapsedClockSeconds);

                _PrevSpeedMpS = _SpeedMpS;
            }
        }



        /// <summary>
        /// update position of discrete freight animations (e.g. containers)
        /// </summary>  
        public void UpdateFreightAnimationDiscretePositions()
        {
            if (FreightAnimations?.Animations != null)
            {
                foreach (var freightAnim in FreightAnimations.Animations)
                {
                    if (freightAnim is FreightAnimationDiscrete)
                    {
                        var discreteFreightAnim = freightAnim as FreightAnimationDiscrete;
                        if (discreteFreightAnim.Loaded && discreteFreightAnim.Container != null)
                        {
                            var container = discreteFreightAnim.Container;
                            container.WorldPosition.XNAMatrix = Matrix.Multiply(container.RelativeContainerMatrix, discreteFreightAnim.Wagon.WorldPosition.XNAMatrix);
                            container.WorldPosition.TileX = WorldPosition.TileX;
                            container.WorldPosition.TileZ = WorldPosition.TileZ;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initialise Train Temperatures
        /// <\summary>           
        public void InitializeCarTemperatures()
        {
            OutsideWinterTempbyLatitudeC = WorldWinterLatitudetoTemperatureC();
            OutsideAutumnTempbyLatitudeC = WorldAutumnLatitudetoTemperatureC();
            OutsideSpringTempbyLatitudeC = WorldSpringLatitudetoTemperatureC();
            OutsideSummerTempbyLatitudeC = WorldSummerLatitudetoTemperatureC();

            // Find the latitude reading and set outside temperature
            double latitude = 0;
            double longitude = 0;

            new WorldLatLon().ConvertWTC(WorldPosition.TileX, WorldPosition.TileZ, WorldPosition.Location, ref latitude, ref longitude);
            
            float LatitudeDeg = MathHelper.ToDegrees((float)latitude);
                      

            // Sets outside temperature dependent upon the season
            if (Simulator.Season == SeasonType.Winter)
            {
                // Winter temps
                InitialCarOutsideTempC = OutsideWinterTempbyLatitudeC[LatitudeDeg];
            }
            else if (Simulator.Season == SeasonType.Autumn)
            {
                // Autumn temps
                InitialCarOutsideTempC = OutsideAutumnTempbyLatitudeC[LatitudeDeg];
            }
            else if (Simulator.Season == SeasonType.Spring)
            {
                // Spring temps
                InitialCarOutsideTempC = OutsideSpringTempbyLatitudeC[LatitudeDeg];
            }
            else
            {
                // Summer temps
                InitialCarOutsideTempC = OutsideSummerTempbyLatitudeC[LatitudeDeg];
            }

            // If weather is freezing. Snow will only be produced when temp is between 0 and 2 Deg C. Adjust temp as appropriate
            const float SnowTemperatureC = 2;

            if (Simulator.WeatherType == WeatherType.Snow && InitialCarOutsideTempC > SnowTemperatureC)
            {
                InitialCarOutsideTempC = 0;  // Weather snowing - freezing conditions. 
            }

            // Initialise wheel bearing temperature to ambient temperature
            WheelBearingTemperatureDegC = InitialCarOutsideTempC;
            InitialWheelBearingRiseTemperatureDegC = InitialCarOutsideTempC;
            InitialWheelBearingDeclineTemperatureDegC = InitialCarOutsideTempC;
        }

        #region Calculate Brake Skid

        /// <summary>
        /// This section calculates:
        /// i) Changing brake shoe friction coefficient due to changes in speed
        /// ii) force on the wheel due to braking, and whether sliding will occur.
        /// 
        /// </summary>

        public virtual void UpdateBrakeSlideCalculation()
        {
            if (this is MSTSLocomotive locomotive)
            {
                // If advanced adhesion model indicates wheel slip warning, then check other conditions (throttle and brake force) to determine whether it is a wheel slip or brake skid
                if (WheelSlipWarning && ThrottlePercent < 0.1f && BrakeRetardForceN > 25.0)
                {
                    BrakeSkidWarning = true;  // set brake skid flag true
                }
                else
                {
                    BrakeSkidWarning = false;
                }

                // If advanced adhesion model indicates wheel slip, then check other conditions (throttle and brake force) to determine whether it is a wheel slip or brake skid
                if (WheelSlip && ThrottlePercent < 0.1f && BrakeRetardForceN > 25.0)
                {
                    BrakeSkid = true;  // set brake skid flag true
                }
                else
                {
                    BrakeSkid = false;
                }
            }
            // Only apply slide, and advanced brake friction, if advanced adhesion is selected, simplecontrolphysics is not set, and it is a Player train
            // Rack stock with cog wheel fitted will not skid
            else if (Simulator.UseAdvancedAdhesion && !Simulator.Settings.SimpleControlPhysics && IsPlayerTrain && !(BrakeCogWheelFitted && IsRackRailway))
            {
                // Determine whether car is experiencing a wheel slip during braking
                if (!BrakeSkidWarning && AbsSpeedMpS > 0.01)
                {
                    var wagonbrakeadhesiveforcen = MassKG * GravitationalAccelerationMpS2 * Train.WagonCoefficientFriction; // Adhesive force wheel normal 

                    if (BrakeRetardForceN > 0.80f * WagonBrakeAdhesiveForceN && ThrottlePercent > 0.01)
                    {
                        BrakeSkidWarning = true; 	// wagon wheel is about to slip
                    }
                }
                else if (BrakeRetardForceN < 0.75f * WagonBrakeAdhesiveForceN)
                {
                    BrakeSkidWarning = false; 	// wagon wheel is back to normal
                }

                // Reset WSP dump valve lockout
                if (WheelBrakeSlideProtectionFitted && WheelBrakeSlideProtectionDumpValveLockout && (ThrottlePercent > 0.01 || AbsSpeedMpS <= 0.002))
                {
                    WheelBrakeSlideProtectionTimerS = wheelBrakeSlideTimerResetValueS;
                    WheelBrakeSlideProtectionDumpValveLockout = false;

                }       

                // Calculate adhesive force based upon whether in skid or not
                if (BrakeSkid)
                {
                    WagonBrakeAdhesiveForceN = MassKG * GravitationalAccelerationMpS2 * SkidFriction;  // Adhesive force if wheel skidding
                }
                else
                {
                    WagonBrakeAdhesiveForceN = MassKG * GravitationalAccelerationMpS2 * Train.WagonCoefficientFriction; // Adhesive force wheel normal
                }
                                   

                // Test if wheel forces are high enough to induce a slip. Set slip flag if slip occuring 
                if (!BrakeSkid && AbsSpeedMpS > 0.01)  // Train must be moving forward to experience skid
                {
                    if (BrakeRetardForceN > WagonBrakeAdhesiveForceN && !(BrakeCogWheelFitted && IsRackRailway))
                    {
                        BrakeSkid = true; 	// wagon wheel is slipping
                        var message = "Car ID: " + CarID + " - experiencing braking force wheel skid.";
                        Simulator.Confirmer.Message(ConfirmLevel.Warning, message);
                    }
                }
                else if (BrakeSkid && AbsSpeedMpS > 0.01)
                {
                    if (BrakeRetardForceN < WagonBrakeAdhesiveForceN || BrakeForceN == 0.0f)
                    {
                        BrakeSkid = false; 	// wagon wheel is not slipping
                    }
                        
                }
                else
                {
                    BrakeSkid = false;  // wagon wheel is not slipping
                }
                BrakeForceN = BrakeRetardForceN;
                if (BrakeSkid) BrakeForceN = Math.Min(BrakeForceN, MassKG * GravitationalAccelerationMpS2 * SkidFriction);
            }
            else  // set default values if simple adhesion model
            {
                BrakeSkid = false; 	// wagon wheel is not slipping
                BrakeForceN = BrakeRetardForceN;
            }

#if DEBUG_BRAKE_SLIDE

            Trace.TraceInformation("================================== Brake Force Slide (TrainCar.cs) ===================================");
            Trace.TraceInformation("Brake Shoe Friction- Car: {0} Speed: {1} Brake Force: {2} Advanced Adhesion: {3}", CarID, MpS.ToMpH(SpeedMpS), BrakeForceN, Simulator.UseAdvancedAdhesion);
            Trace.TraceInformation("BrakeSkidCheck: {0}", BrakeSkidCheck);
            Trace.TraceInformation("Brake Shoe Friction- Coeff: {0} Adjust: {1}", BrakeShoeCoefficientFriction, BrakeShoeCoefficientFrictionAdjFactor);
            Trace.TraceInformation("Brake Shoe Force - Ret: {0} Adjust: {1} Skid {2} Adj {3}", BrakeRetardForceN, BrakeShoeRetardCoefficientFrictionAdjFactor, BrakeSkid, SkidFriction);
            Trace.TraceInformation("Tread: {0} Adhesive: {1}", BrakeWheelTreadForceN, WagonBrakeAdhesiveForceN);
            Trace.TraceInformation("Mass: {0} Rail Friction: {1}", MassKG, Train.WagonCoefficientFriction);
#endif

        }


        #endregion

        #region Calculate resistance due to tunnels
        /// <summary>
        /// Tunnel force (resistance calculations based upon formula presented in papaer titled "Reasonable compensation coefficient of maximum gradient in long railway tunnels"
        /// </summary>
        public virtual void UpdateTunnelForce()
        {
            if (Train.IsPlayerDriven)   // Only calculate tunnel resistance when it is the player train.
            {
                if (CarTunnelData.FrontPositionBeyondStartOfTunnel.HasValue)
                {
                    // Calculate tunnel default effective cross-section area, and tunnel perimeter - based upon the designed speed limit of the railway (TRK File)
                    float TunnelLengthM = CarTunnelData.LengthMOfTunnelAheadFront.Value + CarTunnelData.LengthMOfTunnelBehindRear.Value;
                    float TrainLengthTunnelM = Train.Length;
                    float TrainMassTunnelKg = Train.MassKg;
                    float PrevTrainCrossSectionAreaM2 = TrainCrossSectionAreaM2;
                    TrainCrossSectionAreaM2 = CarWidthM * CarHeightM;
                    if (TrainCrossSectionAreaM2 < PrevTrainCrossSectionAreaM2)
                    {
                        TrainCrossSectionAreaM2 = PrevTrainCrossSectionAreaM2;  // Assume locomotive cross-sectional area is the largest, if not use new one.
                    }
                    const float DensityAirKgpM3 = 1.2f;

                    // Determine tunnel X-sect area and perimeter based upon number of tracks
                    if (CarTunnelData.numTunnelPaths >= 2)
                    {
                        TunnelCrossSectionAreaM2 = DoubleTunnelCrossSectAreaM2; // Set values for double track tunnels and above
                        TunnelPerimeterM = DoubleTunnelPerimeterM;
                    }
                    else
                    {
                        TunnelCrossSectionAreaM2 = SingleTunnelCrossSectAreaM2; // Set values for single track tunnels
                        TunnelPerimeterM = SingleTunnelPerimeterAreaM;
                    }

                    // Calculate first tunnel factor
                    float TunnelAComponent = (0.00003318f * DensityAirKgpM3 * TunnelCrossSectionAreaM2) / ((1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)) * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)));
                    float TunnelBComponent = 174.419f * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)) * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2));
                    float TunnelCComponent = (2.907f * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)) * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2))) / (4.0f * (TunnelCrossSectionAreaM2 / TunnelPerimeterM));

                    float TempTunnel1 = (float)Math.Sqrt(TunnelBComponent + (TunnelCComponent * (TunnelLengthM - TrainLengthTunnelM) / TrainLengthTunnelM));
                    float TempTunnel2 = (1.0f - (1.0f / (1.0f + TempTunnel1))) * (1.0f - (1.0f / (1.0f + TempTunnel1)));

                    float UnitAerodynamicDrag = ((TunnelAComponent * TrainLengthTunnelM) / Kg.ToTonne(TrainMassTunnelKg)) * TempTunnel2;

                    TunnelForceN = UnitAerodynamicDrag * Kg.ToTonne(MassKG) * AbsSpeedMpS * AbsSpeedMpS;
                    CarInTunnel = true;
                }
                else
                {
                    TunnelForceN = 0.0f; // Reset tunnel force to zero when train is no longer in the tunnel
                    CarInTunnel = false;
                }
            }
        }



        #endregion

        #region Calculate risk of train derailing

        //================================================================================================//
        /// <summary>
        /// Update Risk of train derailing and also calculate coupler angle
        /// Train will derail if lateral forces on the train exceed the vertical forces holding the train on the railway track. 
        /// Typically the train is most at risk when travelling around a curve
        ///
        /// Based upon "Fast estimation of the derailment risk of a braking train in curves and turnouts" - 
        /// https://www.researchgate.net/publication/304618476_Fast_estimation_of_the_derailment_risk_of_a_braking_train_in_curves_and_turnouts
        ///
        /// This section calculates the coupler angle behind the current car (ie the rear coupler on this car and the front coupler on the following car. The coupler angle will be used for
        /// coupler automation as well as calculating Lateral forces on the car.
        /// 
        /// In addition Chapter 2 - Flange Climb Derailment Criteria of the TRB’s Transit Cooperative Research Program (TCRP) Report 71, examines flange climb derailment criteria for transit 
        /// vehicles that include lateral-to-vertical ratio limits and a corresponding flange-climb-distance limit. The report also includes guidance to transit agencies on wheel and rail 
        /// maintenance practices.
        /// 
        /// Some of the concepts described in this publication have also been used to calculate the derailment likelihood.
        /// 
        /// https://www.nap.edu/read/13841/chapter/4
        /// 
        /// It should be noted that car derailment is a very complex process that is impacted by many diferent factors, including the track structure and train conditions. To model all of 
        /// these factors is not practical so only some of the key factors are considered. For eaxmple, wheel wear may determine whether a particular car will derial or not. So the same 
        /// type of car can either derail or not under similar circumstances.
        /// 
        /// Hence these calculations provide a "generic" approach to determining whether a car will derail or not.
        /// 
        /// Buff Coupler angle calculated from this publication: In-Train Force Limit Study by National Research Council Canada
        /// 
        /// https://nrc-publications.canada.ca/eng/view/ft/?id=8cc206d0-5dbd-42ed-9b4e-35fd9f8b8efb
        /// 
        /// </summary>

        public void UpdateTrainDerailmentRisk(float elapsedClockSeconds)
        {
            // Calculate coupler angle when travelling around curve
            // To achieve an accurate coupler angle calculation the following length need to be calculated. These values can be included in the ENG/WAG file for greatest accuracy, or alternatively OR will
            // calculate some default values based upon the length of the car specified in the "Size" statement. This value may however be inaccurate, and sets the "visual" distance for placement of the 
            // animated coupler. So often it is a good idea to add the values in the WAG file.
            
            var OverhangThisCarM = 0.5f * (CarBodyLengthM - CarBogieCentreLengthM); // Vehicle overhang - B
            var BogieDistanceThisCarM = 0.5f * CarBogieCentreLengthM; // 0.5 * distance between bogie centres - A
            var CouplerDistanceThisCarM = 0.5f * (CarCouplerFaceLengthM - CarBodyLengthM);
                        
            var OverhangBehindCarM = 2.545f;  // Vehicle overhang - B
            var BogieDistanceBehindCarM = 8.23f;  // 0.5 * distance between bogie centres - A
            var CouplerDistanceBehindCarM = 0.5f * (CarCouplerFaceLengthM - CarBodyLengthM);
            if (CarBehind != null)
            {
                OverhangBehindCarM = 0.5f * (CarBehind.CarBodyLengthM - CarBehind.CarBogieCentreLengthM);  // Vehicle overhang - B
                BogieDistanceBehindCarM = 0.5f * CarBehind.CarBogieCentreLengthM;  // 0.5 * distance between bogie centres - A
                CouplerDistanceBehindCarM = 0.5f * (CarBehind.CarCouplerFaceLengthM - CarBehind.CarBodyLengthM);
            }

            float CouplerAlphaAngleRad;
            float CouplerBetaAngleRad;
            float CouplerGammaAngleRad;

            float finalCouplerAlphaAngleRad;
            float finalCouplerBetaAngleRad;
            float finalCouplerGammaAngleRad;

            var couplerDistanceM = CouplerDistanceThisCarM + CouplerDistanceBehindCarM + CouplerSlackM;

            if (couplerDistanceM == 0)
            {
                couplerDistanceM = 0.0001f; // Stop couplerDistance equalling zero as this causes NaN calculations in following calculations.
            }
            
            float BogieCentresAdjVehiclesM = OverhangThisCarM + OverhangBehindCarM + couplerDistanceM; // L value = Overhangs + Coupler spacing - D

            if (CarBehind != null)
            {

                if (CurrentCurveRadiusM != 0 || CarBehind.CurrentCurveRadiusM != 0)
                {
                    //When coming into a curve or out of a curve it is possible for an infinity value to occur, this next section ensures that never happens
                    if (CurrentCurveRadiusM == 0)
                    {
                        float AspirationalCurveRadius = 10000;
                        CouplerAlphaAngleRad = BogieDistanceThisCarM / AspirationalCurveRadius;
                        CouplerGammaAngleRad = BogieCentresAdjVehiclesM / (2.0f * AspirationalCurveRadius);


                        finalCouplerAlphaAngleRad = BogieDistanceThisCarM / CarBehind.CurrentCurveRadiusM;
                        finalCouplerGammaAngleRad = BogieCentresAdjVehiclesM / (2.0f * CarBehind.CurrentCurveRadiusM);
                    }
                    else
                    {
                        CouplerAlphaAngleRad = BogieDistanceThisCarM / CurrentCurveRadiusM;  // current car curve
                        CouplerGammaAngleRad = BogieCentresAdjVehiclesM / (2.0f * CurrentCurveRadiusM); // assume curve between cars is the same as the curve for the front car.
                        finalCouplerAlphaAngleRad = BogieDistanceThisCarM / CurrentCurveRadiusM;  // current car curve
                        finalCouplerGammaAngleRad = BogieCentresAdjVehiclesM / (2.0f * CurrentCurveRadiusM); // assume curve between cars is the same as the curve for the front car.
                    }

                    //When coming into a curve or out of a curve it is possible for an infinity value to occur, which can cause calculation issues, this next section ensures that never happens
                    if (CarBehind.CurrentCurveRadiusM == 0)
                    {
                        float AspirationalCurveRadius = 10000;
                        CouplerBetaAngleRad = BogieDistanceBehindCarM / AspirationalCurveRadius;

                        finalCouplerBetaAngleRad = BogieDistanceBehindCarM / CurrentCurveRadiusM;
                    }
                    else
                    {
                        CouplerBetaAngleRad = BogieDistanceBehindCarM / CarBehind.CurrentCurveRadiusM; // curve of following car

                        finalCouplerBetaAngleRad = BogieDistanceBehindCarM / CarBehind.CurrentCurveRadiusM; // curve of following car
                    }

                    float AngleBetweenCarbodies = CouplerAlphaAngleRad + CouplerBetaAngleRad + 2.0f * CouplerGammaAngleRad;

                    float finalAngleBetweenCarbodies = finalCouplerAlphaAngleRad + finalCouplerBetaAngleRad + 2.0f * finalCouplerGammaAngleRad;


                    // Find maximum coupler angle expected in this curve, ie both cars will be on the curve
                    var finalWagonRearCouplerAngleRad = (BogieCentresAdjVehiclesM * (finalCouplerGammaAngleRad + finalCouplerAlphaAngleRad) - OverhangBehindCarM * finalAngleBetweenCarbodies) / couplerDistanceM;
                    var finalWagonFrontCouplerAngleRad = (BogieCentresAdjVehiclesM * (finalCouplerGammaAngleRad + finalCouplerBetaAngleRad) - OverhangThisCarM * finalAngleBetweenCarbodies) / couplerDistanceM;

                    // If first car is starting to turn then slowly increase coupler angle to the maximum value expected
                    if (CurrentCurveRadiusM != 0 && CarBehind.CurrentCurveRadiusM == 0)
                    {
                        WagonRearCouplerAngleRad += 0.0006f;
                        WagonRearCouplerAngleRad = MathHelper.Clamp(WagonRearCouplerAngleRad, 0, finalWagonRearCouplerAngleRad);

                        CarBehind.WagonFrontCouplerAngleRad += 0.0006f;
                        CarBehind.WagonFrontCouplerAngleRad = MathHelper.Clamp(CarBehind.WagonFrontCouplerAngleRad, 0, finalWagonFrontCouplerAngleRad);

                    }
                    else if (CurrentCurveRadiusM != 0 && CarBehind.CurrentCurveRadiusM != 0) // both cars on the curve
                    {
                        // Find coupler angle for rear coupler on the car
                        WagonRearCouplerAngleRad = (BogieCentresAdjVehiclesM * (CouplerGammaAngleRad + CouplerAlphaAngleRad) - OverhangBehindCarM * AngleBetweenCarbodies) / couplerDistanceM;
                        // Find coupler angle for front coupler on the following car
                        CarBehind.WagonFrontCouplerAngleRad = (BogieCentresAdjVehiclesM * (CouplerGammaAngleRad + CouplerBetaAngleRad) - OverhangThisCarM * AngleBetweenCarbodies) / couplerDistanceM;
                    }

                    // If first car is still on straight, and last car is still on the curve, then slowly decrease coupler angle so that it is "straight" again
                    else if (CurrentCurveRadiusM == 0 && CarBehind.CurrentCurveRadiusM != 0)
                    {
                        WagonRearCouplerAngleRad -= 0.0006f;
                        WagonRearCouplerAngleRad = MathHelper.Clamp(WagonRearCouplerAngleRad, 0, finalWagonRearCouplerAngleRad);

                        CarBehind.WagonFrontCouplerAngleRad -= 0.0006f;
                        CarBehind.WagonFrontCouplerAngleRad = MathHelper.Clamp(CarBehind.WagonFrontCouplerAngleRad, 0, finalWagonFrontCouplerAngleRad);
                    }

                    // Set direction of coupler angle depending upon whether curve is left or right handed. Coupler angle will be +ve or -ve with relation to the car as a reference frame.
                    // Left hand Curves will result in: Front coupler behind: +ve, and Rear coupler front: +ve
                    // Right hand Curves will result in: Front coupler behind: -ve, and Rear coupler front: -ve

                    // Determine whether curve is left hand or right hand
                    var curveDirection = GetCurveDirection();
                    var carBehindcurveDirection = CarBehind.GetCurveDirection();

                    if (curveDirection == "Right")
                    {
                        AdjustedWagonRearCouplerAngleRad = -WagonRearCouplerAngleRad;
                        CarBehind.AdjustedWagonFrontCouplerAngleRad = -CarBehind.WagonFrontCouplerAngleRad;
                    }

                    else if (curveDirection == "Left")
                    {
                        AdjustedWagonRearCouplerAngleRad = WagonRearCouplerAngleRad;
                        CarBehind.AdjustedWagonFrontCouplerAngleRad = CarBehind.WagonFrontCouplerAngleRad;
                    }
                    else
                    {
                        AdjustedWagonRearCouplerAngleRad = WagonRearCouplerAngleRad;
                        CarBehind.AdjustedWagonFrontCouplerAngleRad = CarBehind.WagonFrontCouplerAngleRad;
                    }

                    // Only process this code segment if coupler is in compression
                    if (CouplerForceU > 0 && CouplerSlackM < 0)
                    {

                        // Calculate Buff coupler angles. Car1 is current car, and Car2 is the car behind
                        // Car ahead rear coupler angle
                        var ThiscarCouplerlengthft = Me.ToFt(CarCouplerFaceLengthM - CarBodyLengthM) + CouplerSlackM / 2;
                        var CarbehindCouplerlengthft = Me.ToFt(CarBehind.CarCouplerFaceLengthM - CarBehind.CarBodyLengthM) + CouplerSlackM / 2;
                        var A1 = Math.Sqrt(Math.Pow(Me.ToFt(CurrentCurveRadiusM), 2) - Math.Pow(Me.ToFt(CarBogieCentreLengthM), 2) / 4.0f);
                        var A2 = (Me.ToFt(CarCouplerFaceLengthM) / 2.0f) - ThiscarCouplerlengthft;
                        var A = (float)Math.Atan(A1 / A2);

                        var B = (float)Math.Asin(2.0f * Me.ToFt(CarTrackPlayM) / Me.ToFt(CarBogieCentreLengthM));
                        var C1 = Math.Pow(ThiscarCouplerlengthft + CarbehindCouplerlengthft, 2);

                        var C2_1 = Math.Sqrt(Math.Pow(Me.ToFt(CarCouplerFaceLengthM) / 2.0f - ThiscarCouplerlengthft, 2) + Math.Pow(Me.ToFt(CurrentCurveRadiusM), 2) - Math.Pow(Me.ToFt(CarBogieCentreLengthM), 2) / 4.0f);
                        var C2_2 = (2.0f * Me.ToFt(CarTrackPlayM) * (Me.ToFt(CarCouplerFaceLengthM) / 2.0f - ThiscarCouplerlengthft)) / Me.ToFt(CarBogieCentreLengthM);
                        var C2 = Math.Pow((C2_1 + C2_2), 2);

                        var C3_1 = Math.Sqrt(Math.Pow(Me.ToFt(CarBehind.CarCouplerFaceLengthM) / 2.0f - CarbehindCouplerlengthft, 2) + Math.Pow(Me.ToFt(CurrentCurveRadiusM), 2) - Math.Pow(Me.ToFt(CarBehind.CarBogieCentreLengthM), 2) / 4.0f);
                        var C3_2 = (2.0f * Me.ToFt(CarBehind.CarTrackPlayM) * (Me.ToFt(CarBehind.CarCouplerFaceLengthM) / 2.0f - CarbehindCouplerlengthft)) / Me.ToFt(CarBehind.CarBogieCentreLengthM);
                        var C3 = Math.Pow((C3_1 + C3_2), 2);

                        var C4 = 2.0f * (ThiscarCouplerlengthft + CarbehindCouplerlengthft) * (C2_1 + C2_2);

                        var C = (float)Math.Acos((C1 + C2 - C3) / C4);

                        WagonRearCouplerBuffAngleRad = MathHelper.ToRadians(180.0f) - A + B - C;


                        //   Trace.TraceInformation("Buff - CarId {0} Carahead {1} A {2} B {3} C {4} 180 {5}", CarID, CarAhead.WagonRearCouplerBuffAngleRad, A, B, C, MathHelper.ToRadians(180.0f));



                        // This car front coupler angle
                        var X1 = Math.Sqrt(Math.Pow(Me.ToFt(CurrentCurveRadiusM), 2) - Math.Pow(Me.ToFt(CarBehind.CarBogieCentreLengthM), 2) / 4.0f);
                        var X2 = (Me.ToFt(CarBehind.CarCouplerFaceLengthM) / 2.0f) - CarbehindCouplerlengthft;
                        var X = (float)Math.Atan(X1 / X2);

                        var Y = (float)Math.Asin(2.0f * Me.ToFt(CarBehind.CarTrackPlayM) / Me.ToFt(CarBehind.CarBogieCentreLengthM));

                        var Z1 = Math.Pow(ThiscarCouplerlengthft + CarbehindCouplerlengthft, 2);
                        var Z2_1 = Math.Sqrt(Math.Pow(Me.ToFt(CarBehind.CarCouplerFaceLengthM) / 2.0f - CarbehindCouplerlengthft, 2) + Math.Pow(Me.ToFt(CurrentCurveRadiusM), 2) - Math.Pow(Me.ToFt(CarBehind.CarBogieCentreLengthM), 2) / 4.0f);
                        var Z2_2 = (2.0f * Me.ToFt(CarBehind.CarTrackPlayM) * (Me.ToFt(CarBehind.CarCouplerFaceLengthM) / 2.0f - CarbehindCouplerlengthft)) / Me.ToFt(CarBehind.CarBogieCentreLengthM);
                        var Z2 = Math.Pow((Z2_1 + Z2_2), 2);

                        var Z3_1 = Math.Sqrt(Math.Pow(Me.ToFt(CarCouplerFaceLengthM) / 2.0f - ThiscarCouplerlengthft, 2) + Math.Pow(Me.ToFt(CurrentCurveRadiusM), 2) - Math.Pow(Me.ToFt(CarBogieCentreLengthM), 2) / 4.0f);
                        var Z3_2 = (2.0f * Me.ToFt(CarTrackPlayM) * (Me.ToFt(CarCouplerFaceLengthM) / 2.0f - ThiscarCouplerlengthft)) / Me.ToFt(CarBogieCentreLengthM);
                        var Z3 = Math.Pow((Z3_1 + Z3_2), 2);

                        var Z4 = 2.0f * (ThiscarCouplerlengthft + CarbehindCouplerlengthft) * (Z2_1 + Z2_2);

                        var Z = (float)Math.Acos((Z1 + Z2 - Z3) / Z4);

                        CarBehind.WagonFrontCouplerBuffAngleRad = MathHelper.ToRadians(180.0f) - X + Y - Z;

                        //     Trace.TraceInformation("Buff - CarId {0} Thiscar {1} A {2} B {3} C {4} 180 {5}", CarID, WagonFrontCouplerBuffAngleRad, X, Y, Z, MathHelper.ToRadians(180.0f));

                       // Trace.TraceInformation("Buff - CarId {0} StringThis {1} StringBehind {2} BuffThis {3} BuffAhead {4}", CarID, WagonRearCouplerAngleRad, CarBehind.WagonFrontCouplerAngleRad, WagonRearCouplerBuffAngleRad, CarBehind.WagonFrontCouplerBuffAngleRad);

                    }

                }
                else if (CarAhead != null)
                {
                    if (CurrentCurveRadiusM == 0 && CarBehind.CurrentCurveRadiusM == 0 && CarAhead.CurrentCurveRadiusM == 0)
                    {
                        AdjustedWagonRearCouplerAngleRad = 0.0f;
                        CarBehind.AdjustedWagonFrontCouplerAngleRad = 0.0f;
                        WagonRearCouplerAngleRad = 0;
                        WagonFrontCouplerAngleRad = 0;
                        WagonRearCouplerBuffAngleRad = 0;
                        WagonFrontCouplerBuffAngleRad = 0;
                        CarBehind.WagonFrontCouplerAngleRad = 0;
                        CarAhead.WagonRearCouplerAngleRad = 0;
                    }
                }

                // Calculate airhose angles and height adjustment values for the air hose.  Firstly the "rest point" is calculated, and then the real time point. 
                // The height and angle variation are then calculated against "at rest" reference point. The air hose angle is used to rotate the hose in two directions, ie the Y and Z axis. 

                // Calculate height adjustment.
                var rearairhoseheightadjustmentreferenceM = (float)Math.Sqrt((float)Math.Pow(CarAirHoseLengthM, 2) - (float)Math.Pow(CarAirHoseHorizontalLengthM, 2));
                var frontairhoseheightadjustmentreferenceM = (float)Math.Sqrt((float)Math.Pow(CarAirHoseLengthM, 2) - (float)Math.Pow(CarBehind.CarAirHoseHorizontalLengthM, 2));

                // actual airhose height
                RearAirHose.HeightAdjustmentM = (float)Math.Sqrt((float)Math.Pow(CarAirHoseLengthM, 2) - (float)Math.Pow((CarAirHoseHorizontalLengthM + CouplerSlackM), 2));
                CarBehind.FrontAirHose.HeightAdjustmentM = (float)Math.Sqrt((float)Math.Pow(CarAirHoseLengthM, 2) - (float)Math.Pow((CarBehind.CarAirHoseHorizontalLengthM + CouplerSlackM), 2));

                // refererence adjustment heights to rest position
                // If higher then rest position, then +ve adjustment
                if (RearAirHose.HeightAdjustmentM >= rearairhoseheightadjustmentreferenceM)
                {
                    RearAirHose.HeightAdjustmentM -= rearairhoseheightadjustmentreferenceM;
                }
                else // if lower then the rest position, then -ve adjustment
                {
                    RearAirHose.HeightAdjustmentM = (rearairhoseheightadjustmentreferenceM - RearAirHose.HeightAdjustmentM);
                }

                if (CarBehind.FrontAirHose.HeightAdjustmentM >= frontairhoseheightadjustmentreferenceM)
                {
                    CarBehind.FrontAirHose.HeightAdjustmentM -= frontairhoseheightadjustmentreferenceM;
                }
                else
                {
                    CarBehind.FrontAirHose.HeightAdjustmentM = frontairhoseheightadjustmentreferenceM - CarBehind.FrontAirHose.HeightAdjustmentM;
                }

                // Calculate angle adjustments
                var rearAirhoseAngleAdjustmentReferenceRad = (float)Math.Asin(CarAirHoseHorizontalLengthM / CarAirHoseLengthM);
                var frontAirhoseAngleAdjustmentReferenceRad = (float)Math.Asin(CarBehind.CarAirHoseHorizontalLengthM / CarAirHoseLengthM);

                RearAirHose.ZAngleAdjustmentRad = (float)Math.Asin((CarAirHoseHorizontalLengthM + CouplerSlackM) / CarAirHoseLengthM);
                CarBehind.FrontAirHose.ZAngleAdjustmentRad = (float)Math.Asin((CarBehind.CarAirHoseHorizontalLengthM + CouplerSlackM) / CarAirHoseLengthM);

                // refererence adjustment angles to rest position
                if (RearAirHose.ZAngleAdjustmentRad >= rearAirhoseAngleAdjustmentReferenceRad)
                {
                    RearAirHose.ZAngleAdjustmentRad -= rearAirhoseAngleAdjustmentReferenceRad;
                }
                else
                {
                    RearAirHose.ZAngleAdjustmentRad = (rearAirhoseAngleAdjustmentReferenceRad - RearAirHose.ZAngleAdjustmentRad);
                }

                // The Y axis angle adjustment should be the same as the z axis
                RearAirHose.YAngleAdjustmentRad = RearAirHose.ZAngleAdjustmentRad;

                if (CarBehind.FrontAirHose.ZAngleAdjustmentRad >= frontAirhoseAngleAdjustmentReferenceRad)
                {
                    CarBehind.FrontAirHose.ZAngleAdjustmentRad -= frontAirhoseAngleAdjustmentReferenceRad;
                }
                else
                {
                    CarBehind.FrontAirHose.ZAngleAdjustmentRad = (frontAirhoseAngleAdjustmentReferenceRad - CarBehind.FrontAirHose.ZAngleAdjustmentRad);
                }

                // The Y axis angle adjustment should be the same as the z axis
                CarBehind.FrontAirHose.YAngleAdjustmentRad = CarBehind.FrontAirHose.ZAngleAdjustmentRad;

            }

            // Train will derail if lateral forces on the train exceed the vertical forces holding the train on the railway track.
            // Coupler force is calculated at the rear of each car, so calculation values may need to be from the car ahead. 
            // Typically the train is most at risk when travelling around a curve.

            // Calculate the vertical force on the wheel of the car, to determine whether wagon derails or not
            // To calculate vertical force on outer wheel = (WagMass / NumWheels) * gravity + WagMass / NumAxles * ( (Speed^2 / CurveRadius) - (gravity * superelevation angle)) * (height * track width)
            // Equation 5

            if (IsPlayerTrain && DerailmentCoefficientEnabled)
            {
                if (CouplerForceU > 0 && CouplerSlackM < 0) // If car coupler is in compression, use the buff angle
                {
                    WagonCouplerAngleDerailRad = Math.Abs(WagonRearCouplerBuffAngleRad);
                }
                else // if coupler in tension, then use tension angle
                {
                    WagonCouplerAngleDerailRad = Math.Abs(WagonRearCouplerAngleRad);
                }


                var numAxles = LocoNumDrvAxles + WagonNumAxles;
                var numWheels = numAxles * 2;

                if (CurrentCurveRadiusM != 0)
                {
                    float A = 0;
                    float B1 = 0;

                    // Prevent NaN if numWheels = 0
                    if (numWheels != 0)
                    {
                        A = (MassKG / numWheels) * GravitationalAccelerationMpS2;
                    }
                    else
                    {
                        A = MassKG * GravitationalAccelerationMpS2;
                    }

                    // Prevent NaN if numAxles = 0
                    if (numAxles != 0)
                    {
                        B1 = (MassKG / numAxles);
                    }
                    else
                    {
                        B1 = MassKG;
                    }
                    var B2 = GravitationalAccelerationMpS2 * (float)Math.Sin(SuperElevationAngleRad);
                    var B3 = (float)Math.Pow(Math.Abs(SpeedMpS), 2) / CurrentCurveRadiusM;
                    var B4 = CentreOfGravityM.Y / TrackGaugeM;

                    TotalWagonVerticalDerailForceN = A + B1 * (B3 - B2) * B4;

                    // Calculate lateral force per wheelset on the first bogie
                    // Lateral Force = (Coupler force x Sin (Coupler Angle) / NumBogies) + WagMass / NumAxles * ( (Speed^2 / CurveRadius) - (gravity * superelevation angle))

                    if (CarAhead != null)
                    {
                        float AA1 = 0;
                        float BB1 = 0;

                        // Prevent NaN if WagonNumBogies = 0
                        if ( WagonNumBogies != 0)
                        {
                            // AA1 = CarAhead.CouplerForceU * (float)Math.Sin(WagonCouplerAngleDerailRad) / WagonNumBogies;
                            AA1 = Math.Abs(CarAhead.CouplerForceUSmoothed.SmoothedValue) * (float)Math.Sin(WagonCouplerAngleDerailRad) / WagonNumBogies;
                        }
                        else
                        {
                           // AA1 = CarAhead.CouplerForceU * (float)Math.Sin(WagonCouplerAngleDerailRad);
                            AA1 = Math.Abs(CarAhead.CouplerForceUSmoothed.SmoothedValue) * (float)Math.Sin(WagonCouplerAngleDerailRad);
                        }

                        // Prevent NaN if numAxles = 0
                        if (numAxles != 0)
                        {
                            BB1 = MassKG / numAxles;
                        }
                        else
                        {
                            BB1 = MassKG;
                        }
                        var BB2 = (float)Math.Pow(Math.Abs(SpeedMpS), 2) / CurrentCurveRadiusM;
                        var BB3 = GravitationalAccelerationMpS2 * (float)Math.Sin(SuperElevationAngleRad);

                        TotalWagonLateralDerailForceN = Math.Abs(AA1 + BB1 * (BB2 - BB3));
                    }

                    DerailmentCoefficient = TotalWagonLateralDerailForceN / TotalWagonVerticalDerailForceN;

                    // use the dynamic multiplication coefficient to calculate final derailment coefficient, the above method calculated using quasi-static factors.
                    // The differences between quasi-static and dynamic limits are due to effects of creepage, curve, conicity, wheel unloading ratio, track geometry, 
                    // car configurations and the share of wheel load changes which are not taken into account in the static analysis etc. 
                    // Hence the following factors have been used to adjust to dynamic effects.
                    // Original figures quoted - Static Draft = 0.389, Static Buff = 0.389, Dynamic Draft = 0.29, Dynamic Buff = 0.22. 
                    // Hence use the following multiplication factors, Buff = 1.77, Draft = 1.34.
                    if (CouplerForceU > 0 && CouplerSlackM < 0)
                    {
                        DerailmentCoefficient *= 1.77f; // coupler in buff condition
                    }
                    else
                    {
                        DerailmentCoefficient *= 1.34f;
                    }

                    var wagonAdhesion = Train.WagonCoefficientFriction;

                    // Calculate Nadal derailment coefficient limit
                    NadalDerailmentCoefficient = ((float) Math.Tan(MaximumWheelFlangeAngleRad) - wagonAdhesion) / (1f + wagonAdhesion * (float) Math.Tan(MaximumWheelFlangeAngleRad));

                    // Calculate Angle of Attack - AOA = sin-1(2 * bogie wheel base / curve radius)
                    AngleOfAttackmRad = (float)Math.Asin(2 * RigidWheelBaseM / CurrentCurveRadiusM);
                    var angleofAttackmRad = AngleOfAttackmRad * 1000f; // Convert to micro radians

                    // Calculate the derail climb distance - uses the general form equation 2.4 from the above publication
                    var parameterA_1 = ((100 / (-1.9128f * MathHelper.ToDegrees(MaximumWheelFlangeAngleRad) + 146.56f)) + 3.1f) * Me.ToIn(WheelFlangeLengthM);

                    var parameterA_2 = (1.0f / (-0.0092f * Math.Pow(MathHelper.ToDegrees(MaximumWheelFlangeAngleRad), 2) + 1.2125f * MathHelper.ToDegrees(MaximumWheelFlangeAngleRad) - 39.031f)) + 1.23f;

                    var parameterA = parameterA_1 + parameterA_2;

                    var parameterB_1 = ((10f / (-21.157f * Me.ToIn(WheelFlangeLengthM) + 2.1052f)) + 0.05f) * MathHelper.ToDegrees(MaximumWheelFlangeAngleRad);

                    var parameterB_2 = (10 / (0.2688f * Me.ToIn(WheelFlangeLengthM) - 0.0266f)) - 5f;

                    var parameterB = parameterB_1 + parameterB_2;

                    DerailClimbDistanceM = Me.FromFt( (float)((parameterA * parameterB * Me.ToIn(WheelFlangeLengthM)) / ((angleofAttackmRad + (parameterB * Me.ToIn(WheelFlangeLengthM))))) );

                    // calculate the time taken to travel the derail climb distance
                    var derailTimeS = DerailClimbDistanceM / AbsSpeedMpS;

                    // Set indication that a derail may occur
                    if (DerailmentCoefficient > NadalDerailmentCoefficient)
                    {
                        DerailPossible = true;
                    }
                    else
                    {
                        DerailPossible = false;
                    }

                    // If derail climb time exceeded, then derail happens
                    if (DerailPossible && DerailElapsedTimeS > derailTimeS)
                    {
                        DerailExpected = true;
                        Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetStringFmt("Car {0} has derailed on the curve.", CarID));
                        if (!HasDerailed)
                        {
                            string derailReason = "defect";
                            if (CouplerForceU > 0 && CouplerSlackM < 0) { derailReason = "jackknifed"; }
                            else if (CouplerForceU < 0 && CouplerSlackM > 0) { derailReason = "stringlined"; }
                            Trace.TraceInformation("Car {0} derailed ({1}), on {2} curve with radius {3}, at speed {4}, after traveling {5}",
                                CarID, derailReason, GetCurveDirection(), FormatStrings.FormatDistance(CurrentCurveRadiusM, IsMetric), FormatStrings.FormatSpeed(AbsSpeedMpS, IsMetric), FormatStrings.FormatDistance(DistanceM, IsMetric));
                            // DistanceM is not a good location measure, as it is based on the train. Two railcars derailing at the same location have a different distance.
                        }
                        HasDerailed = true;
                        //  Trace.TraceInformation("Car Derail - CarID: {0}, Coupler: {1}, CouplerSmoothed {2}, Lateral {3}, Vertical {4}, Angle {5} Nadal {6} Coeff {7}", CarID, CouplerForceU, CouplerForceUSmoothed.SmoothedValue, TotalWagonLateralDerailForceN, TotalWagonVerticalDerailForceN, WagonCouplerAngleDerailRad, NadalDerailmentCoefficient, DerailmentCoefficient);
                        //   Trace.TraceInformation("Car Ahead Derail - CarID: {0}, Coupler: {1}, CouplerSmoothed {2}, Lateral {3}, Vertical {4}, Angle {5}", CarAhead.CarID, CarAhead.CouplerForceU, CarAhead.CouplerForceUSmoothed.SmoothedValue, CarAhead.TotalWagonLateralDerailForceN, CarAhead.TotalWagonVerticalDerailForceN, CarAhead.WagonCouplerAngleDerailRad);
                    }
                    else if (DerailPossible)
                    {
                        DerailElapsedTimeS += elapsedClockSeconds;
                     //   Trace.TraceInformation("Car Derail Time - CarID: {0}, Coupler: {1}, CouplerSmoothed {2}, Lateral {3}, Vertical {4}, Angle {5}, Elapsed {6}, DeratilTime {7}, Distance {8} Nadal {9} Coeff {10}", CarID, CouplerForceU, CouplerForceUSmoothed.SmoothedValue, TotalWagonLateralDerailForceN, TotalWagonVerticalDerailForceN, WagonCouplerAngleDerailRad, DerailElapsedTimeS, derailTimeS, DerailClimbDistanceM, NadalDerailmentCoefficient, DerailmentCoefficient);
                    }
                    else
                    {
                        DerailElapsedTimeS = 0; // Reset timer if derail is not possible
                        HasDerailed = false;
                    }

                    if (AbsSpeedMpS < 0.01)
                    {
                        DerailExpected = false;
                        DerailPossible = false;
                        DerailElapsedTimeS = 0;
                        HasDerailed = false;
                    }

//                    if (CarID == "0 - 84" || CarID == "0 - 83" || CarID == "0 - 82" || CarID == "0 - 81" || CarID == "0 - 80" || CarID == "0 - 79")
//                    {
//                        Trace.TraceInformation("Nadal - {0}, Adhesion {1} Flange Angle {2}", NadalDerailmentCoefficient, wagonAdhesion, MaximumWheelFlangeAngleRad);
//                        Trace.TraceInformation("Derailment - CarID {0}, Nadal {1}, Derail {2} Possible {3} Expected {4} Derail Distance {5} ElapsedTime {6} DerailTime {7}", CarID, NadalDerailmentCoefficient, DerailmentCoefficient, DerailPossible, DerailExpected, DerailClimbDistanceM, DerailElapsedTimeS, derailTimeS);
//                    }
                }
                else
                {
                    TotalWagonLateralDerailForceN = 0;
                    TotalWagonVerticalDerailForceN = 0;
                    DerailmentCoefficient = 0;
                    DerailExpected = false;
                    DerailPossible = false;
                    DerailElapsedTimeS = 0;
                    HasDerailed = false;
                }



                if (TotalWagonLateralDerailForceN > TotalWagonVerticalDerailForceN)
                {
                    BuffForceExceeded = true;
                }
                else
                {
                    BuffForceExceeded = false;
                }
            }

        }

        #endregion

        /// <summary>
        /// Get the Angle of attack for a car as it goes through a curve
        /// </summary>
        /// <returns>angle in micro radians</returns>
        /// 
        public float GetAngleofAttackmRad()
        {
            if (CurrentCurveRadiusM > 0)
            {
                // Calculate Angle of Attack - AOA = sin-1(2 * bogie wheel base / curve radius)
                var angleofAttackmRad = (float)Math.Asin(2 * RigidWheelBaseM / CurrentCurveRadiusM) * 1000f; // Convert to micro radians
                return angleofAttackmRad;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Get the track switch /crossover trigger for a car as it goes over a switch
        /// </summary>
        /// <returns>1 = switch, 0 = no switch</returns>
        ///
        public float GetTrackSwitchTrigger(float elapsedClockSeconds)
        {

            // Timer to hold trigger on for a period of time
            if (carOnSwitchTriggered)
            {
                switchTriggerDelayedS -= elapsedClockSeconds;
                if (switchTriggerDelayedS < 0)
                    switchTriggerDelayedS = 0;
            }

            if (IsOverSwitch && !carOnSwitchTriggered)
            {
                carOnSwitchTriggered = true;
                return 1; // Set trigger for car on switch
            }
            else if (!IsOverSwitch && switchTriggerDelayedS == 0 && carOnSwitchTriggered)
            {
                carOnSwitchTriggered = false;
                switchTriggerDelayedS = 0.1f;
                return 0; // Reset trigger when off
            }
            else if (carOnSwitchTriggered && switchTriggerDelayedS > 0)
            {
                return 1; // ensure trigger stays on until time out
            }

            return 0; // default if no result found
        }

        /// <summary>
        /// Get the track joint trigger for a car as it goes over a joint
        /// </summary>
        /// <returns>1 = track joint, 0 = no track joint</returns>
        ///
        public float GetTrackJointPosition(float elapsedClockSeconds)
        {
            if ((float)Simulator.TRK.Tr_RouteFile.DistanceBetweenTrackJointsM == 0)
            {
                return 0; // Rail joints have not been selected
            }
            else
            {
                // Calculate remaining distance beween track joints
                realTimeTrackJointDistanceM -= AbsSpeedMpS * elapsedClockSeconds;
                if (realTimeTrackJointDistanceM < 0)
                    realTimeTrackJointDistanceM = 0;
                if (realTimeTrackJointDistanceM == 0)
                {
                    jointTrigger = 1;
                    carOnJointTriggered = true;
                    jointTriggerDelayedS -= elapsedClockSeconds;
                    if (jointTriggerDelayedS < 0)
                        jointTriggerDelayedS = 0;
                }
                else
                {
                    jointTrigger = 0;
                }
                if (jointTrigger == 1 && jointTriggerDelayedS == 0)
                {
                    jointTriggerDelayedS = 0.1f; // Ensure enough delay to trigger sound
                    jointTrigger = 0;
                    // To ensure that track joints are never closer then 1 sec apart set to speedmps when distance traveled in 1 sec is greater then the joint distance.
                    if (AbsSpeedMpS > (float)Simulator.TRK.Tr_RouteFile.DistanceBetweenTrackJointsM)
                    {
                        realTimeTrackJointDistanceM = AbsSpeedMpS;
                        jointSpeedMpS = AbsSpeedMpS;
                    }
                    else
                    {
                        realTimeTrackJointDistanceM = (float)Simulator.TRK.Tr_RouteFile.DistanceBetweenTrackJointsM; // Reset for next pass
                        jointSpeedMpS = (float)Simulator.TRK.Tr_RouteFile.DistanceBetweenTrackJointsM;
                    }
                    carOnJointTriggered = false;
                }

                return jointTrigger;
            }
        }


/// <summary>
/// Get the current direction that curve is heading relative to the train.
/// </summary>
/// <returns>left or Right indication</returns>
public string GetCurveDirection()
        {
            string curveDirection = "Straight";

            if (CarBehind != null && (CurrentCurveRadiusM != 0 || CarBehind.CurrentCurveRadiusM != 0))
            {

                // Front Wagon Direction
                float direction = (float)Math.Atan2(WorldPosition.XNAMatrix.M13, WorldPosition.XNAMatrix.M11);
                float FrontWagonDirectionDeg = MathHelper.ToDegrees((float)direction);

                // If car is flipped, then the car's direction will be reversed by 180 compared to the rest of the train, and thus for calculation purposes only, 
                // it is necessary to reverse the "assumed" direction of the car back again. This shouldn't impact the visual appearance of the car.
                if (Flipped)
                {
                    FrontWagonDirectionDeg += 180.0f; // Reverse direction of car
                    if (FrontWagonDirectionDeg > 360) // If this results in an angle greater then 360, then convert it back to an angle between 0 & 360.
                    {
                        FrontWagonDirectionDeg -= 360;
                    }
                }

                // If a westerly direction (ie -ve) convert to an angle between 0 and 360
                if (FrontWagonDirectionDeg< 0)
                    FrontWagonDirectionDeg += 360;

                // Rear Wagon Direction
                direction = (float) Math.Atan2(CarBehind.WorldPosition.XNAMatrix.M13, CarBehind.WorldPosition.XNAMatrix.M11);
                float BehindWagonDirectionDeg = MathHelper.ToDegrees((float)direction);


                // If car is flipped, then the car's direction will be reversed by 180 compared to the rest of the train, and thus for calculation purposes only, 
                // it is necessary to reverse the "assumed" direction of the car back again. This shouldn't impact the visual appearance of the car.
                if (CarBehind.Flipped)
                {
                    BehindWagonDirectionDeg += 180.0f; // Reverse direction of car
                    if (BehindWagonDirectionDeg > 360) // If this results in an angle greater then 360, then convert it back to an angle between 0 & 360.
                    {
                        BehindWagonDirectionDeg -= 360;
                    }
                }

                // If a westerly direction (ie -ve) convert to an angle between 0 and 360
                if (BehindWagonDirectionDeg< 0)
                    BehindWagonDirectionDeg += 360;

                if (FrontWagonDirectionDeg > 270 && BehindWagonDirectionDeg< 90)
                {
                    FrontWagonDirectionDeg -= 360;
                }

                if (FrontWagonDirectionDeg< 90 && BehindWagonDirectionDeg> 270)
                {
                    BehindWagonDirectionDeg -= 360;
                }

                var directionBandwidth = Math.Abs(FrontWagonDirectionDeg - BehindWagonDirectionDeg);

                // Calculate curve direction
                if (FrontWagonDirectionDeg > BehindWagonDirectionDeg && directionBandwidth > 0.005)
                {
                    curveDirection = "Right";
                }
                else if (FrontWagonDirectionDeg<BehindWagonDirectionDeg && directionBandwidth> 0.005)
                {
                    curveDirection = "Left";
                }
            }
            else
            {
                curveDirection = "Straight";
            }

            return curveDirection;

        }


        #region Calculate permissible speeds around curves
        /// <summary>
        /// Reads current curve radius and computes the maximum recommended speed around the curve based upon the 
        /// superelevation of the track
        /// Based upon information extracted from - Critical Speed Analysis of Railcars and Wheelsets on Curved and Straight Track - https://scarab.bates.edu/cgi/viewcontent.cgi?article=1135&context=honorstheses
        /// </summary>
        public virtual void UpdateCurveSpeedLimit(float elapsedClockSeconds)
        {
            // Only check curve speed limit if on a curve, menu option is enabled, train is player driven, and not timeatable mode(???) (no calculations here are needed for AI)
            if (CurrentCurveRadiusM > 0 && CurveSpeedDependent && Train.IsPlayerDriven && !Simulator.TimetableMode)  
            {
#if DEBUG_USER_SUPERELEVATION
                       Trace.TraceInformation(" ============================================= User SuperElevation (TrainCar.cs) ========================================");
                        Trace.TraceInformation("CarID {0} TrackSuperElevation {1} Curve Radius {2}",  CarID, SuperElevationM, CurrentCurveRadius);
#endif
                float s = AbsSpeedMpS; // speed of train

                // Calulate equal wheel loading speed for current curve and superelevation - this was considered the "safe" speed to travel around a curve . In this instance the load on the both railes is evenly distributed.
                // max equal load speed = SQRT ( (superelevation x gravity x curve radius) / track gauge)
                // Superelevation is made up of two components = rail superelevation + the amount of sideways force that a passenger will be comfortable with. This is expressed as a figure similar to superelevation.

                float SpeedToleranceMpS = Me.FromMi(pS.FrompH(2.5f));  // Set bandwidth tolerance for resetting notifications

                // Railway companies often allow the vehicle to exceed the equal loading speed, provided that the passengers didn't feel uncomfortable, and that the car was not likely to excced the maximum critical speed
                float MaxSafeCurveSpeedMps = (float)Math.Sqrt(((SuperElevationM + MaxUnbalancedSuperElevationM) * GravitationalAccelerationMpS2 * CurrentCurveRadiusM) / TrackGaugeM);

                // Calculate critical speed - indicates the speed above which stock will overturn - sum of the moments of centrifrugal force and the vertical weight of the vehicle around the CoG
                // critical speed = SQRT ( (centrifrugal force x gravity x curve radius) / Vehicle weight)
                // centrifrugal force = Stock Weight x factor for movement of resultant force due to superelevation.

                float SinTheta = SuperElevationM / TrackGaugeM; // Definition of sine: opposite / hypotenuse = superelevation / gauge
                float CosTheta = (float)Math.Cos(SuperElevationAngleRad);
                float HalfTrackGaugeM = TrackGaugeM / 2.0f;

                float CriticalMaxSpeedMpS = (float)Math.Sqrt((CurrentCurveRadiusM * GravitationalAccelerationMpS2 * (CentreOfGravityM.Y * SinTheta + HalfTrackGaugeM * CosTheta)) / (CentreOfGravityM.Y * CosTheta - HalfTrackGaugeM * SinTheta));

                // This is not the correct equation for the minimum topple over speed as it produces nonzero values for curves where the minimum safe speed is a full stop
                // Also, is this even needed anymore considering the derailment factor code checks for topple over type derailments?
                // float Sin2Theta = 0.5f * (1 - (float)Math.Cos(2.0 * SuperElevationAngleRad));
                // float CriticalMinSpeedMpS = (float)Math.Sqrt((GravitationalAccelerationMpS2 * CurrentCurveRadiusM * HalfTrackGaugeM * Sin2Theta) / (CosTheta * (CentreOfGravityM.Y * CosTheta + HalfTrackGaugeM * SinTheta)));

                // Test current speed to see if greater then equal loading speed around the curve
                if (s > MaxSafeCurveSpeedMps)
                {
                    // Consider a tolerance so passengers won't immediately complain, should reduce overkill notifications on routes with jerky track laying
                    // Can go negative to punish continuous speeding
                    ComfortTolerance -= 0.25f * (s / MaxSafeCurveSpeedMps - 1.0f) * elapsedClockSeconds;

                    if (!IsMaxSafeCurveSpeed && ComfortTolerance <= 0.0f)
                    {
                        IsMaxSafeCurveSpeed = true; // set flag for IsMaxSafeCurveSpeed reached

                        if (Train.IsFreight)
                        {
                            Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetStringFmt("You are travelling too fast for this curve, your freight car {0} may be damaged. The recommended speed for this curve is {1}", CarID, FormatStrings.FormatSpeedDisplay(MaxSafeCurveSpeedMps, IsMetric)));
                        }
                        else
                        {
                            Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetStringFmt("You are travelling too fast for this curve, your passengers in car {0} are feeling uncomfortable. The recommended speed for this curve is {1}", CarID, FormatStrings.FormatSpeedDisplay(MaxSafeCurveSpeedMps, IsMetric)));
                        }

                        if (dbfmaxsafecurvespeedmps != MaxSafeCurveSpeedMps)//Debrief eval
                        {
                            dbfmaxsafecurvespeedmps = MaxSafeCurveSpeedMps;
                            //ldbfevalcurvespeed = true;
                            DbfEvalTravellingTooFast++;
                            Train.DbfEvalValueChanged = true;//Debrief eval
                        }
                    }
                }
                else if (s < MaxSafeCurveSpeedMps - SpeedToleranceMpS)  // Reset notification once speed drops
                {
                    if (IsMaxSafeCurveSpeed)
                    {
                        IsMaxSafeCurveSpeed = false; // reset flag for IsMaxSafeCurveSpeed reached - if speed on curve decreases
                    }
                    // Restore passenger tolerance gradually (100 seconds)
                    ComfortTolerance += elapsedClockSeconds / 100.0f;
                    if (ComfortTolerance > 1.0f)
                        ComfortTolerance = 1.0f;
                }

                // If speed exceeds the overturning speed, then indicated that an error condition has been reached.
                if (s > CriticalMaxSpeedMpS)
                {
                    // Consider a tolerance so error isn't immediately thrown, should reduce overkill notifications on routes with jerky track laying
                    // Will be reduced faster if simultaneously above the max safe speed
                    ComfortTolerance -= 0.25f * (s / MaxSafeCurveSpeedMps - 1.0f) * elapsedClockSeconds;

                    if (!IsCriticalMaxSpeed && ComfortTolerance <= 0.0f)
                    {
                        IsCriticalMaxSpeed = true; // set flag for IsCriticalSpeed reached

                        BrakeSystem.FrontBrakeHoseConnected = false; // break the brake hose connection between cars if the speed is too fast
                        Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("You were travelling too fast for this curve, and have snapped a brake hose on Car " + CarID + ". The maximum speed for this curve is "
                            + FormatStrings.FormatSpeedDisplay(CriticalMaxSpeedMpS, IsMetric) + ". You will need to repair the hose and restart."));

                        dbfEvalsnappedbrakehose = true;//Debrief eval

                        if (!ldbfevaltrainoverturned)
                        {
                            ldbfevaltrainoverturned = true;
                            DbfEvalTrainOverturned++;
                            Train.DbfEvalValueChanged = true;//Debrief eval
                        }
                    }

                }
                else if (s < CriticalMaxSpeedMpS - SpeedToleranceMpS) // Reset notification once speed drops
                {
                    if (IsCriticalMaxSpeed)
                    {
                        IsCriticalMaxSpeed = false; // reset flag for IsCriticalSpeed reached - if speed on curve decreases
                        ldbfevaltrainoverturned = false;

                        if (dbfEvalsnappedbrakehose)
                        {
                            DbfEvalTravellingTooFastSnappedBrakeHose++;//Debrief eval
                            dbfEvalsnappedbrakehose = false;
                            Train.DbfEvalValueChanged = true;//Debrief eval
                        }

                    }
                }

                // DISABLED: Critical minimum speed calculation appears to be incorrect and is calculating unreasonable values and causing erronious errors. Refactoring required in the future.
                // if speed doesn't reach minimum speed required around the curve then set notification
                // Breaking of brake hose will not apply to TT mode or AI trains or if on a curve less then 150m to cover operation in shunting yards, where track would mostly have no superelevation
                //if (s < CriticalMinSpeedMpS && CurrentCurveRadiusM > 150)
                //{
                //    if (!IsCriticalMinSpeed)
                //    {
                //        IsCriticalMinSpeed = true; // set flag for IsCriticalSpeed not reached

                //        Simulator.Confirmer.Message(ConfirmLevel.Warning, Simulator.Catalog.GetString("You were travelling too slow for this curve, and Car " + CarID + "may topple over."));
                //    }

                //}
                //else if (s > CriticalMinSpeedMpS + SpeedToleranceMpS) // Reset notification once speed increases
                //{
                //    if (IsCriticalMinSpeed)
                //    {
                //        IsCriticalMinSpeed = false; // reset flag for IsCriticalSpeed reached - if speed on curve decreases
                //    }
                //}

#if DEBUG_CURVE_SPEED
                Trace.TraceInformation("================================== TrainCar.cs - DEBUG_CURVE_SPEED ==============================================================");
                Trace.TraceInformation("CarID {0} Curve Radius {1} Super {2} Unbalanced {3} Durability {4}", CarID, CurrentCurveRadius, SuperElevationM, UnbalancedSuperElevationM, Simulator.CurveDurability);
                Trace.TraceInformation("CoG {0}", CentreOfGravityM);
                Trace.TraceInformation("Current Speed {0} Equal Load Speed {1} Max Safe Speed {2} Critical Max Speed {3} Critical Min Speed {4}", MpS.ToMpH(s), MpS.ToMpH(MaxCurveEqualLoadSpeedMps), MpS.ToMpH(MaxSafeCurveSpeedMps), MpS.ToMpH(CriticalMaxSpeedMpS), MpS.ToMpH(CriticalMinSpeedMpS));
                Trace.TraceInformation("IsMaxSafeSpeed {0} IsCriticalSpeed {1}", IsMaxSafeCurveSpeed, IsCriticalSpeed);
#endif
            }
            else
            {
                // reset flags if train is on a straight - in preparation for next curve
                IsCriticalMaxSpeed = false;   // reset flag for IsCriticalMaxSpeed reached
                IsCriticalMinSpeed = false;   // reset flag for IsCriticalMinSpeed reached
                IsMaxSafeCurveSpeed = false; // reset flag for IsMaxEqualLoadSpeed reached

                // Restore passenger tolerance gradually (100 seconds)
                ComfortTolerance += elapsedClockSeconds / 100.0f;
                if (ComfortTolerance > 1.0f)
                    ComfortTolerance = 1.0f;
            }

        }

        #endregion
    
        #region Calculate friction force in curves

        /// <summary>
        /// Reads current curve radius and computes the CurveForceN friction. Can be overriden by calling
        /// base.UpdateCurveForce();
        /// CurveForceN *= someCarSpecificCoef;     
        /// </summary>
        public virtual void UpdateCurveForce(float elapsedClockSeconds)
        {
            if (CurrentCurveRadiusM > 0)
            {
                // References:

                // i) The modern locomotive by  Clarence Edgar Allen – 1912 – pg 82 - https://archive.org/details/modernlocomotive00allerich

                //  ii)	Resistance to Traffic of Railway Rolling Stock by P.N.Astakhov – Moscow 1966 – pg 112
                //  http://scbist.com/scb/uploaded/1_astahov_p_n_soprotivlenie_dvizheniyu_zheleznodorozhnogo_podv.pdf

                // The CurveForce is a combination of these two components such that resistance will vary with stock characteristics and speed.
                // These formulas are a mix of imperial and metric expressions so these will be retained and converted to a common UoM in Newtons once calculations are complete.

                // Base Curve Resistance (from refernce i)) = (Vehicle mass x Coeff Friction) * (Track Gauge + Vehicle Fixed Wheelbase) / (2 * curve radius)
                // Vehicle Fixed Wheel base is the distance between the wheels, ie bogie or fixed wheels

                float rBaseWagonN = GravitationalAccelerationMpS2 * MassKG * Train.WagonCoefficientFriction * (TrackGaugeM + RigidWheelBaseM) / (2.0f * CurrentCurveRadiusM);

                // Speed Curve Resistance (from reference ii) - second term only) = ((Speed^2 / Curve Radius) - (Superelevation / Track Gauge) * Gravitational acceleration) * Constant

                float speedConstant = 1.5f;
                float rspeedKgpTonne = speedConstant * Math.Abs((SpeedMpS * SpeedMpS / CurrentCurveRadiusM) - (GravitationalAccelerationMpS2 * SuperElevationM / TrackGaugeM));
                float rSpeedWagonN = GravitationalAccelerationMpS2 * (Kg.ToTonne(MassKG) * rspeedKgpTonne);

                CurveForceN = rBaseWagonN + rSpeedWagonN;
            }
            else
            {
                CurveForceN = 0f;
            }
            //CurveForceNFiltered = CurveForceFilter.Filter(CurveForceN, elapsedClockSeconds);
            CurveForceFilter.Update(elapsedClockSeconds, CurveForceN);
            CurveForceNFiltered = CurveForceFilter.SmoothedValue;
        }

        #endregion

        /// <summary>
        /// Signals an event from an external source (player, multi-player controller, etc.) for this car.
        /// </summary>
        /// <param name="evt"></param>
        public virtual void SignalEvent(Event evt) { }
        public virtual void SignalEvent(TCSEvent evt) { }
        public virtual void SignalEvent(PowerSupplyEvent evt) { }
        public virtual void SignalEvent(PowerSupplyEvent evt, int id) { }

        public virtual string GetStatus() { return null; }
        public virtual string GetDebugStatus()
        {
            string locomotivetypetext = "";
            if (EngineType == EngineTypes.Control)
            {
                locomotivetypetext = "Unpowered Control Trailer Car";
            }

            var loco = this as MSTSDieselLocomotive;
            if (loco != null && loco.DieselEngines.HasGearBox && loco.DieselTransmissionType == MSTSDieselLocomotive.DieselTransmissionTypes.Mechanic)
            {
                return String.Format("{0}\t{1}\t{2}\t{3}\t{4:F0}%\t{5} - {6:F0} rpm\t\t{7}\t{8}\t{9}\t",
                CarID,
                FormatStrings.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(Direction)),
                Flipped ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                RemoteControlGroup == 0 ? Simulator.Catalog.GetString("Sync") : RemoteControlGroup == 1 ? Simulator.Catalog.GetString("Async") : "----",
                ThrottlePercent,
                String.Format("{0}", FormatStrings.FormatSpeedDisplay(SpeedMpS, IsMetric)),
                loco.DieselEngines[0].GearBox.HuDShaftRPM,
                // For Locomotive HUD display shows "forward" motive power (& force) as a positive value, braking power (& force) will be shown as negative values.
                FormatStrings.FormatPower(loco.LocomotiveAxles.DrivePowerW, IsMetric, false, false),
                String.Format("{0}{1}", FormatStrings.FormatForce(loco.LocomotiveAxles.DriveForceN, IsMetric), WheelSlip ? "!!!" : WheelSlipWarning ? "???" : ""),
                Simulator.Catalog.GetString(locomotivetypetext)
                );
            }
            else
            {
                return String.Format("{0}\t{2}\t{1}\t{3}\t{4:F0}%\t{5}\t\t{6}\t{7}\t{8}\t",
                CarID,
                Flipped ? Simulator.Catalog.GetString("Yes") : Simulator.Catalog.GetString("No"),
                FormatStrings.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(Direction)),
                RemoteControlGroup == 0 ? Simulator.Catalog.GetString("Sync") : RemoteControlGroup == 1 ? Simulator.Catalog.GetString("Async") : "----",
                ThrottlePercent,
                String.Format("{0}", FormatStrings.FormatSpeedDisplay(SpeedMpS, IsMetric)),
                // For Locomotive HUD display shows "forward" motive power (& force) as a positive value, braking power (& force) will be shown as negative values.
                FormatStrings.FormatPower((this as MSTSWagon).LocomotiveAxles.DrivePowerW, IsMetric, false, false),
                String.Format("{0}{1}", FormatStrings.FormatForce((this as MSTSWagon).LocomotiveAxles.DriveForceN, IsMetric), WheelSlip ? "!!!" : WheelSlipWarning ? "???" : ""),
                Simulator.Catalog.GetString(locomotivetypetext)
                );
            }
        }
        public virtual string GetTrainBrakeStatus() { return null; }
        public virtual string GetEngineBrakeStatus() { return null; }
        public virtual string GetBrakemanBrakeStatus() { return null; }
        public virtual string GetDynamicBrakeStatus() { return null; }
        public virtual string GetDPDynamicBrakeStatus() { return null; }
        public virtual string GetMultipleUnitsConfiguration() { return null; }
        public virtual bool GetSanderOn() { return false; }
        protected bool WheelHasBeenSet = false; //indicating that the car shape has been loaded, thus no need to reset the wheels

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
            outf.Write(TractionForceN);
            outf.Write(DynamicBrakeForceN);
            outf.Write(MotiveForceN);
            outf.Write(FrictionForceN);
            outf.Write(SpeedMpS);
            outf.Write(CouplerSlackM);
            outf.Write(Headlight);
            outf.Write(OrgConsist);
            outf.Write(TiltingZRot);
            outf.Write(BrakesStuck);
            outf.Write(IsCarHeatingInitialized);
            outf.Write(SteamHoseLeakRateRandom);
            outf.Write(CarHeatCurrentCompartmentHeatJ);
            outf.Write(CarSteamHeatMainPipeSteamPressurePSI);
            outf.Write(CarHeatCompartmentHeaterOn);
            outf.Write(CurveSquealAoAmRadFiltered);
        }

        // Game restore
        public virtual void Restore(BinaryReader inf)
        {
            Flipped = inf.ReadBoolean();
            UiD = inf.ReadInt32();
            CarID = inf.ReadString();
            BrakeSystem.Restore(inf);
            TractionForceN = inf.ReadSingle();
            DynamicBrakeForceN = inf.ReadSingle();
            MotiveForceN = inf.ReadSingle();
            FrictionForceN = inf.ReadSingle();
            SpeedMpS = inf.ReadSingle();
            _PrevSpeedMpS = SpeedMpS;
            CouplerSlackM = inf.ReadSingle();
            Headlight = inf.ReadInt32();
            OrgConsist = inf.ReadString();
            TiltingZRot = inf.ReadSingle();
            BrakesStuck = inf.ReadBoolean();
            IsCarHeatingInitialized = inf.ReadBoolean();
            SteamHoseLeakRateRandom = inf.ReadSingle();
            CarHeatCurrentCompartmentHeatJ = inf.ReadSingle();
            CarSteamHeatMainPipeSteamPressurePSI = inf.ReadSingle();
            CarHeatCompartmentHeaterOn = inf.ReadBoolean();
            CurveSquealAoAmRadFiltered = inf.ReadSingle();
            CurveSquealAoAmRadFilter.ForceSmoothValue(CurveSquealAoAmRadFiltered);
            FreightAnimations?.LoadDataList?.Clear();
        }

        //================================================================================================//
        /// <summary>
        /// Set starting conditions for TrainCars when initial speed > 0 
        /// 

        public virtual void InitializeMoving()
        {
            BrakeSystem.InitializeMoving();
            //TODO: next if/else block has been inserted to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
            // To achieve the same result with other means, without flipping trainset physics, the if/else block should be deleted and replaced by following instruction:
            //            SpeedMpS = Flipped ? -Train.InitialSpeed : Train.InitialSpeed;
            if (IsDriveable && Train.TrainType == Train.TRAINTYPE.PLAYER)
            {
                var loco = this as MSTSLocomotive;
                SpeedMpS = Flipped ^ loco.UsingRearCab ? -Train.InitialSpeed : Train.InitialSpeed;
            }

            else SpeedMpS = Flipped ? -Train.InitialSpeed : Train.InitialSpeed;
            _PrevSpeedMpS = SpeedMpS;
        }

        public bool HasFrontCab
        {
            get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Front;
                if (loco == null || loco.CabViewList.Count <= i || loco.CabViewList[i].CabViewType != CabViewType.Front) return false;
                return (loco.CabViewList[i].ViewPointList.Count > 0);
            }
        }

        public bool HasRearCab
        {
            get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Rear;
                if (loco == null || loco.CabViewList.Count <= i) return false;
                return (loco.CabViewList[i].ViewPointList.Count > 0);
            }
        }

        public bool HasFront3DCab
        {
            get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Front;
                if (loco == null || loco.CabView3D == null || loco.CabView3D.CabViewType != CabViewType.Front) return false;
                return (loco.CabView3D.ViewPointList.Count > i);
            }
        }

        public bool HasRear3DCab
        {
            get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Rear;
                if (loco == null || loco.CabView3D == null || loco.CabView3D.ViewPointList.Count <= i) return false;
                var cabViewAngle = loco.CabView3D.ViewPointList[i].StartDirection.Y;
                return ((cabViewAngle >= 90 && cabViewAngle <= 270) || (cabViewAngle <= -90 && cabViewAngle >= -270));
            }
        }

        public virtual bool GetCabFlipped()
        {
            return false;
        }

        //<comment>
        //Initializes the physics of the car taking into account its variable discrete loads
        //</comment>
        public void InitializeLoadPhysics()
        {
            // TODO
        }

        //<comment>
        //Updates the physics of the car taking into account its variable discrete loads
        //</comment>
        public void UpdateLoadPhysics()
        {
            // TODO
        }

        public virtual float GetCouplerZeroLengthM()
        {
            return 0;
        }

        public virtual float GetSimpleCouplerStiffnessNpM()
        {
            return 2e7f;
        }

        public virtual float GetCouplerStiffness1NpM()
        {
            return 1e7f;
        }

        public virtual float GetCouplerStiffness2NpM()
        {
            return 1e7f;
        }

        public virtual float GetCouplerSlackAM()
        {
            return 0;
        }

        public virtual float GetCouplerSlackBM()
        {
            return 0.1f;
        }

        public virtual bool GetCouplerRigidIndication()
        {
            return false;
        }

        public virtual float GetMaximumSimpleCouplerSlack1M()
        {
            return 0.03f;
        }
        
        public virtual float GetMaximumSimpleCouplerSlack2M()
        {
            return 0.035f;
        }

        public virtual float GetMaximumCouplerForceN()
        {
            return 1e10f;
        }

        // Advanced coupler parameters

        public virtual float GetCouplerTensionStiffness1N()
        {
            return 1e7f;
        }

        public virtual float GetCouplerTensionStiffness2N()
        {
            return 2e7f;
        }

        public virtual float GetCouplerCompressionStiffness1N()
        {
            return 1e7f;
        }

        public virtual float GetCouplerCompressionStiffness2N()
        {
            return 2e7f;
        }

        public virtual float GetCouplerTensionSlackAM()
        {
            return 0;
        }

        public virtual float GetCouplerTensionSlackBM()
        {
            return 0.1f;
        }
 
        public virtual float GetCouplerCompressionSlackAM()
        {
            return 0;
        }
 
        public virtual float GetCouplerCompressionSlackBM()
        {
            return 0.1f;
        }

        public virtual float GetMaximumCouplerTensionSlack1M()
        {
            return 0.05f;
        }
         
        public virtual float GetMaximumCouplerTensionSlack2M()
        {
            return 0.1f;
        }
 
        public virtual float GetMaximumCouplerTensionSlack3M()
        {
            return 0.13f;
        }

        public virtual float GetMaximumCouplerCompressionSlack1M()
        {
            return 0.05f;
        }

        public virtual float GetMaximumCouplerCompressionSlack2M()
        {
            return 0.1f;
        }
 
        public virtual float GetMaximumCouplerCompressionSlack3M()
        {
            return 0.13f;
        }

        public virtual float GetCouplerBreak1N() // Sets the break force????
        {
            return 1e10f;
        }

        public virtual float GetCouplerBreak2N() // Sets the break force????
        {
            return 1e10f;
        }

        public virtual float GetCouplerTensionR0Y() // Sets the break force????
        {
            return 0.0001f;
        }

        public virtual float GetCouplerCompressionR0Y() // Sets the break force????
        {
            return 0.0001f;
        }



        public virtual void CopyCoupler(TrainCar other)
        {
            CouplerSlackM = other.CouplerSlackM;
            CouplerSlack2M = other.CouplerSlack2M;
        }

        public virtual bool GetAdvancedCouplerFlag()
        {
            return false;
        }

        public virtual void CopyControllerSettings(TrainCar other)
        {
            Headlight = other.Headlight;
        }

        public void AddWheelSet(Vector3 offset, int bogieID, int parentMatrix, string wheels, int bogie1Axles, int bogie2Axles)
        {
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;

            // Currently looking for rolling stock that has more than 3 axles on a bogie.  This is rare, but some models are like this.
            // In this scenario, bogie1 contains 2 sets of axles.  One of them for bogie2.  Both bogie2 axles must be removed.
            // For the time being, the only rail-car that was having issues had 4 axles on one bogie. The second set of axles had a bogie index of 2 and both had to be dropped for the rail-car to operate under OR.
            if (Parts.Count > 0 && bogie1Axles == 4 || bogie2Axles == 4) // 1 bogie will have a Parts.Count of 2.
            {
                if (Parts.Count == 2)
                    if (parentMatrix == Parts[1].iMatrix && wheels.Length == 8)
                        if (bogie1Axles == 4 && bogieID == 2) // This test is strictly testing for and leaving out axles meant for a Bogie2 assignment.
                            return;

                if (Parts.Count == 3)
                {
                    if (parentMatrix == Parts[1].iMatrix && wheels.Length == 8)
                        if (bogie1Axles == 4 && bogieID == 2) // This test is strictly testing for and leaving out axles meant for a Bogie2 assignment.
                            return;
                    if (parentMatrix == Parts[2].iMatrix && wheels.Length == 8)
                        if (bogie2Axles == 4 && bogieID == 1) // This test is strictly testing for and leaving out axles meant for a Bogie1 assignment.
                            return;
                }

            }

            //some old stocks have only two wheels, but defined to have four, two share the same offset, thus all computing of rotations will have problem
            //will check, if so, make the offset different a bit.
            foreach (var axles in WheelAxles)
                if (offset.Z.AlmostEqual(axles.OffsetM.Z, 0.05f)) { offset.Z = axles.OffsetM.Z + 0.7f; break; }

            // Came across a model where the axle offset that is part of a bogie would become 0 during the initial process.  This is something we must test for.
            if (wheels.Length == 8 && Parts.Count > 0)
            {
                if (wheels == "WHEELS11" || wheels == "WHEELS12" || wheels == "WHEELS13" || wheels == "WHEELS14")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

                else if (wheels == "WHEELS21" || wheels == "WHEELS22" || wheels == "WHEELS23" || wheels == "WHEELS24")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

                else if (wheels == "WHEELS31" || wheels == "WHEELS32" || wheels == "WHEELS33" || wheels == "WHEELS34")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

                else if (wheels == "WHEELS41" || wheels == "WHEELS42" || wheels == "WHEELS43" || wheels == "WHEELS44")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
                // This else will cover additional Wheels added following the proper naming convention.
                else
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
            }
            // The else will cover WHEELS spelling where the length is less than 8.
            else
                WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

        } // end AddWheelSet()

        public void AddBogie(Vector3 offset, int matrix, int id, string bogie, int numBogie1, int numBogie2)
        {
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;
            foreach (var p in Parts) if (p.Bogie && offset.Z.AlmostEqual(p.OffsetM.Z, 0.05f)) { offset.Z = p.OffsetM.Z + 0.1f; break; }
            if (bogie == "BOGIE1")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(Vector3.Zero, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].Bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            else if (bogie == "BOGIE2")
            {
                // This was the initial problem.  If the shape file contained only one entry that was labeled as BOGIE2(should be BOGIE1)
                // the process would assign 2 to id, causing it to create 2 Parts entries( or 2 bogies) when one was only needed.  It is possible that
                // this issue created many of the problems with articulated wagons later on in the process.
                // 2 would be assigned to id, not because there were 2 entries, but because 2 was in BOGIE2.
                if (numBogie2 == 1 && numBogie1 == 0)
                {
                    id -= 1;
                    while (Parts.Count <= id)
                        Parts.Add(new TrainCarPart(Vector3.Zero, 0));
                    Parts[id].OffsetM = offset;
                    Parts[id].iMatrix = matrix;
                    Parts[id].Bogie = true;//identify this is a bogie, will be used for hold rails on track
                }
                else
                {
                    while (Parts.Count <= id)
                        Parts.Add(new TrainCarPart(Vector3.Zero, 0));
                    Parts[id].OffsetM = offset;
                    Parts[id].iMatrix = matrix;
                    Parts[id].Bogie = true;//identify this is a bogie, will be used for hold rails on track
                }
            }
            else if (bogie == "BOGIE3")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(Vector3.Zero, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].Bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            else if (bogie == "BOGIE4")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(Vector3.Zero, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].Bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            else if (bogie == "BOGIE")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(Vector3.Zero, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].Bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            // The else will cover additions not covered above.
            else
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(Vector3.Zero, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].Bogie = true;//identify this is a bogie, will be used for hold rails on track
            }

            WagonNumBogies = Parts.Count - 1;

        } // end AddBogie()

        public void SetUpWheels()
        {

#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
            WheelHasBeenSet = true;
            // No parts means no bogies (always?), so make sure we've got Parts[0] for the car itself.
            if (Parts.Count == 0)
                Parts.Add(new TrainCarPart(Vector3.Zero, 0));
            // Determine how many parts are considered bogies (used to calculate position of the car)
            int bogieCount = 0;
            foreach (TrainCarPart p in Parts)
                if (p.Bogie == true)
                    bogieCount++;
            // No axles but we have bogies. Each bogie needs axles to function correctly
            if (WheelAxles.Count == 0 && bogieCount > 0)
            {
                // Add a fake axle to each bogie, a second fake axle will be added later if needed
                for (int i = 1; i < Parts.Count; i++)
                    if (Parts[i].Bogie == true)
                        WheelAxles.Add(new WheelAxle(Parts[i].OffsetM, i, Parts[i].iMatrix, true));
                Trace.TraceInformation("Wheel axle data faked based on {1} bogies for {0}", WagFilePath, bogieCount);
            }
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
            }

            //fix bogies with only one wheel set:
            // This process is to fix the bogies that did not pivot under the cab of steam locomotives as well as other locomotives that have this symptom.
            // The cause involved the bogie and axle being close by 0.05f or less on the ZAxis.
            // The ComputePosition() process was unable to work with this.
            // The fix involves first testing for how close they are then moving the bogie offset up.
            // The final fix involves adding an additional axle.  Without this, both bogie and axle would never track properly?
            // Note: Steam locomotive modelers are aware of this issue and are now making sure there is ample spacing between axle and bogie.
            for (var i = 1; i < Parts.Count; i++)
            {
                if (Parts[i].Bogie == true && Parts[i].SumWgt < 1.5)
                {
                    foreach (var w in WheelAxles)
                    {
                        if (w.BogieMatrix == Parts[i].iMatrix)
                        {
                            if (w.OffsetM.Z.AlmostEqual(Parts[i].OffsetM.Z, 0.6f))
                            {
                                var w1 = new WheelAxle(new Vector3(w.OffsetM.X, w.OffsetM.Y, w.OffsetM.Z - 0.5f), w.BogieIndex, i, true);
                                w1.Part = Parts[w1.BogieIndex]; //create virtual wheel
                                w1.Part.SumWgt++;
                                WheelAxles.Add(w1);
                                w.OffsetM.Z += 0.5f; //move the original bogie forward, so we have two bogies to make the future calculation happy
                                Trace.TraceInformation("A virtual wheel axle was added for bogie {1} of {0}", WagFilePath, i);
                                break;
                            }
                        }
                    }
                }
            }

            // Check how many parts can drive the position of the car itself
            // Each part needs at least 2 components (sum of weights > 1.5) for position calculation to work
            for (var i = 1; i < Parts.Count; i++)
                if (Parts[i].SumWgt > 1.5)
                    Parts[0].SumWgt++;

            // Check if articulation is desired on this car, as this requires different handling
            bool articFront = (FrontArticulation == 1 || (FrontArticulation == -1 && !WheelAxles.Any(a => a.OffsetM.Z < 0)));
            bool articRear = (RearArticulation == 1 || (RearArticulation == -1 && !WheelAxles.Any(a => a.OffsetM.Z > 0)));

            // If car has insufficient bogies and it's not because of articulation, attempt to avoid position calculation errors
            // Detach wheels from the last bogie, and instead attach to the main part, which should allow calculations to work properly
            if (!articFront && !articRear && (Parts[0].SumWgt < 1.5))
            {
                foreach (WheelAxle w in WheelAxles)
                {
                    if (w.BogieIndex >= Parts.Count - 1)
                    {
                        w.BogieIndex = 0;
                        w.Part = Parts[w.BogieIndex];
                    }
                }
            }

#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
            // Add fake axle(s) to train car for articulation when desired
            // Adding fake axles automatically is only allowed on non-engines with 0, 2, or 3 axles
            // These limitations prevent various incompatibilities with existing content
            bool allowAutoArticulate = WagonType != WagonTypes.Engine && WheelAxles.Count != 1 && WheelAxles.Count <= 3;
            articFront &= !(FrontArticulation == -1 && !allowAutoArticulate);
            articRear &= !(RearArticulation == -1 && !allowAutoArticulate);

            if (articFront || articRear)
                SetUpWheelsArticulation(articFront, articRear);

            // Other calculations require axles to be sorted based on their Z-offset
            if (WheelAxles.Count > 0)
                WheelAxles.Sort(WheelAxles[0]);

            // After all processing is complete, check if the car can have its position calculated
            // using the position of the axles, which is indicated by the 'WheelAxlesLoaded' flag.
            // The train car must have at least 2 position references. These references can be either
            // an axle or a bogie, but each bogie itself needs 2 position references.
            int[] posReferences = new int[Parts.Count];
            // Count the number of axles associated with each part (including main object)
            foreach (WheelAxle w in WheelAxles)
                posReferences[w.BogieIndex]++;
            // Add a position reference to the main object for each bogie itself with at least 2 position references
            for (int i = 1; i < Parts.Count; i++)
                if (posReferences[i] >= 2) 
                    posReferences[0]++;
            // Car has a suitable arrangement of axles for position calculation if the main object has at least 2 position references
            if (posReferences[0] >= 2) 
                WheelAxlesLoaded = true;
        } // end SetUpWheels()

        protected void SetUpWheelsArticulation(bool front, bool rear)
        {
            // If there are no forward axles, this car is articulated (joined
            // to the car in front) at the front. Likewise for the rear.
            // This will cause the car to move incorrectly, so to produce the
            // expected motion, a fake axle is added at the articulated end(s)
            // of the car, attached to the car itself. This will drive the positioning
            // of the car as expected.
            if (front)
                WheelAxles.Add(new WheelAxle(new Vector3(0.0f, BogiePivotHeightM, -CarLengthM / 2.0f), 0, 0, true) { Part = Parts[0] });

            if (rear)
                WheelAxles.Add(new WheelAxle(new Vector3(0.0f, BogiePivotHeightM, CarLengthM / 2.0f), 0, 0, true) { Part = Parts[0] });


#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
        } // end SetUpWheelsArticulation()

        public void ComputePosition(Traveller traveler, bool backToFront, float elapsedTimeS, float distance, float speed)
        {
            for (int j = 0; j < Parts.Count; j++)
                Parts[j].InitLineFit();
            int tileX = traveler.TileX;
            int tileZ = traveler.TileZ;

            UpdateCurvePhys(new Traveller(traveler), BogieZOffsets);

            int direction = traveler.Direction == Traveller.TravellerDirection.Forward ? -1 : 1;

            if (Flipped == backToFront)
            {
                float o = -CarLengthM / 2 - CentreOfGravityM.Z;
                for (int k = 0; k < WheelAxles.Count; k++)
                {
                    float d = WheelAxles[k].OffsetM.Z - o;
                    o = WheelAxles[k].OffsetM.Z;
                    traveler.Move(d);

                    Vector3 location = traveler.CalcElevationPositionOffset(BogiePivotHeightM, Simulator.UseSuperElevation, out float r);
                    location += traveler.Location;

                    location.X += 2048 * (traveler.TileX - tileX);
                    location.Z += 2048 * (traveler.TileZ - tileZ);

                    // This car is flipped, so flip roll direction in part
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, location, -r);
                }
                o = CarLengthM / 2 - CentreOfGravityM.Z - o;
                traveler.Move(o);
                direction *= -1;
            }
            else
            {
                float o = CarLengthM / 2 - CentreOfGravityM.Z;
                for (int k = WheelAxles.Count - 1; k >= 0; k--)
                {
                    float d = o - WheelAxles[k].OffsetM.Z;
                    o = WheelAxles[k].OffsetM.Z;
                    traveler.Move(d);

                    Vector3 location = traveler.CalcElevationPositionOffset(BogiePivotHeightM, Simulator.UseSuperElevation, out float r);
                    location += traveler.Location;

                    location.X += 2048 * (traveler.TileX - tileX);
                    location.Z += 2048 * (traveler.TileZ - tileZ);

                    WheelAxles[k].Part.AddWheelSetLocation(1, o, location, r);
                }
                o = CarLengthM / 2 + CentreOfGravityM.Z + o;
                traveler.Move(o);
            }

            TrainCarPart p0 = Parts[0];
            for (int i = 1; i < Parts.Count; i++)
            {
                TrainCarPart p = Parts[i];
                p.FindCenterLine();

                if (p.SumWgt > 1.5f)
                    p0.AddPartLocation(1, p);
            }
            if (Parts.Count == 2 && p0.SumWgt < 1.5f)
            {
                // Train car lacks sufficient parts to locate using linear regression
                p0.Dir = Parts[1].Dir;
                p0.Pos = Parts[1].Pos;
                p0.Roll = Parts[1].Roll;
            }
            else
            {
                // Determine facing direction and position of train car
                p0.FindCenterLine();
            }
            Vector3 fwd = new Vector3(p0.Dir[0], p0.Dir[1], -p0.Dir[2]);
            // Check if null (0-length) vector
            if (!(fwd.X == 0 && fwd.Y == 0 && fwd.Z == 0))
                fwd.Normalize();
            Vector3 side = Vector3.Cross(Vector3.Up, fwd);
            // Check if null (0-length) vector
            if (!(side.X == 0 && side.Y == 0 && side.Z == 0))
                side.Normalize();
            Vector3 up = Vector3.Cross(fwd, side);
            Matrix m = Matrix.Identity;
            m.Right = side;
            m.Up = up;
            m.Backward = fwd;

            // Update whether track is rack or not
            UpdateRackRailDetection(traveler);

            // Update gravity force when position is updated, but before any secondary motion is added
            UpdateGravity(m);

            // Consider body roll from superelevation and from tilting.
            UpdateTilting(traveler, elapsedTimeS, speed, direction);
            Matrix rollMat = Matrix.Identity;
            float rollAngle = p0.Roll + TiltingZRot;

            if (rollAngle != 0.0f)
            {
                // For correct bogie positioning, need to offset rotation axis
                Vector3 offset = new Vector3(0.0f, BogiePivotHeightM, 0.0f);

                // Roll the car for superelevation about the offset axis of rotation
                rollMat.Translation -= offset;
                rollMat *= Matrix.CreateRotationZ(rollAngle);
                rollMat.Translation += offset;

                m = rollMat * m;
            }

            SuperElevationInverseMatrix = Matrix.Invert(rollMat);

            // Set position of train car
            m.Translation += new Vector3(p0.Pos[0], p0.Pos[1], -p0.Pos[2]);

            WorldPosition.XNAMatrix = m;
            WorldPosition.TileX = tileX;
            WorldPosition.TileZ = tileZ;

            UpdateVibration(traveler, elapsedTimeS, distance);

            // Check bogies for any corrections needed
            for (int i = 1; i < Parts.Count; i++)
            {
                TrainCarPart p = Parts[i];

                if (p.SumWgt < 1.5f && p.SumWgt > 0.5f) // Handle edge case of single axle pony trucks
                {
                    double d = p.OffsetM.Z - p.SumZOffset / p.SumWgt;
                    if (-.2 < d && d < .2)
                        continue;
                    // Add a fake "wheel" to serve as a pivot point
                    Vector3 pos = new Vector3(p0.Pos[0] + p.OffsetM.Z * p0.Dir[0], p0.Pos[1] + p.OffsetM.Z * p0.Dir[1], p0.Pos[2] + p.OffsetM.Z * p0.Dir[2]);
                    p.AddWheelSetLocation(1, p.OffsetM.Z, pos, p.Roll);
                    p.FindCenterLine();
                }
            }

            UpdatePositionFlags();
        }

        #region Traveller-based updates
        public float CurrentCurveRadiusM;

        public void UpdateRackRailDetection(Traveller traveller)
        {
            if (this is MSTSWagon wagon)
            {
                var thisSection = traveller.GetCurrentSection();

                if (thisSection != null && Simulator.TSectionDat.TrackShapes.ContainsKey(thisSection.ShapeIndex))
                {
                    TrackShape thisShape = Simulator.TSectionDat.TrackShapes[thisSection.ShapeIndex];

                    if (thisShape.RackShape)
                    {
                        IsRackRailway = true;
                    }
                    else
                    {
                        IsRackRailway = false;
                    }
                }
            }
        }

        internal void UpdateTilting(Traveller traveller,  float elapsedTimeS, float speedMpS, int direction)
        {
            // If not a tilting train, skip processing this
            // Future: Rework tilting to be per train car, instead of per consist
            if (!Train.IsTilting)
                return;

            float tiltDemand = 0.0f;

            // No tilt needed if going too slow, or not on a curve
            if (speedMpS > MinTiltSpeedMpS && CurrentCurveRadiusM != 0.0f)
            {
                // Compare actual superelevation to ideal, tilt makes up the difference
                // Sine of superelevation angle is v^2 / (g * r)
                float idealElevAngle = (speedMpS * speedMpS) / (GravitationalAccelerationMpS2 * CurrentCurveRadiusM);
                idealElevAngle = (float)Math.Asin(Math.Min(idealElevAngle, 0.99f));

                tiltDemand = MathHelper.Clamp(idealElevAngle - SuperElevationAngleRad, 0.0f, MaxTiltAngleRad);

                tiltDemand *= direction * traveller.CurveDirection;
            }

            // Smooth rotation
            TiltingZRot += (tiltDemand - TiltingZRot) * elapsedTimeS; 
        }
        #endregion

        #region SuperElevation
        /// <summary>
        /// Determines the curve situation (superelevation and radius) of the train car, given a
        /// traveller to move over an array of offsets to average out the curve across.
        /// This WILL move the traveller by the total amount of all the offsets.
        /// Directly sets the superelevation and curve radius for car physics.
        /// </summary>
        public void UpdateCurvePhys(Traveller traveller, float[] offsets)
        {
            // Ensure at least one offset is given
            if (offsets == null || offsets.Length <= 0)
                offsets = new[] { 0.0f };

            // Need to get superelevation at both ends of the car by offsetting the traveller
            traveller.GetCurveData(offsets, out float[] physicsElevation, out float[] curveRadii);

            // Superelevation MUST be limited to track gauge to avoid NaN errors
            SuperElevationM = Math.Min(physicsElevation.Average(), TrackGaugeM);
            // Set superelevation angle used by physics system
            SuperElevationAngleRad = (float)Math.Asin(SuperElevationM / TrackGaugeM);

            CurrentCurveRadiusM = curveRadii.Average();
            // Straight track has a "radius" of infinity, but rest of code expects straight to have a "radius" of 0
            if (CurrentCurveRadiusM == float.PositiveInfinity)
                CurrentCurveRadiusM = 0;
        }
        #endregion

        #region Vibration and tilting
        public Matrix VibrationInverseMatrix = Matrix.Identity;
        public Matrix SuperElevationInverseMatrix = Matrix.Identity;

        // https://en.wikipedia.org/wiki/Newton%27s_laws_of_motion#Newton.27s_2nd_Law
        //   Let F be the force in N
        //   Let m be the mass in kg
        //   Let a be the acceleration in m/s/s
        //   Then F = m * a
        // https://en.wikipedia.org/wiki/Hooke%27s_law
        //   Let F be the force in N
        //   Let k be the spring constant in N/m or kg/s/s
        //   Let x be the displacement in m
        //   Then F = k * x
        // If we assume that gravity is 9.8m/s/s, then the force needed to support the train car is:
        //   F = m * 9.8
        // If we assume that the train car suspension allows for 0.2m (20cm) of travel, then substitute Hooke's law:
        //   m * 9.8 = k * 0.2
        //   k = m * 9.8 / 0.2
        // Finally, we assume a mass (m) of 1kg to calculate a mass-independent value:
        //   k' = 9.8 / 0.2
        const float VibrationSpringConstantPrimepSpS = 9.8f / 0.2f; // 1/s/s

        // 
        const float VibratioDampingCoefficient = 0.01f;

        // This is multiplied by the CarVibratingLevel (which goes up to 3).
        const float VibrationIntroductionStrength = 0.03f;

        // The tightest curve we care about has a radius of 100m. This is used as the basis for the most violent vibrations.
        const float VibrationMaximumCurvaturepM = 1f / 100;

        const float VibrationFactorDistance = 1;
        const float VibrationFactorTrackVectorSection = 2;
        const float VibrationFactorTrackNode = 4;

        Vector3 VibrationOffsetM;
        Vector3 VibrationRotationRad;
        Vector3 VibrationRotationVelocityRadpS;
        Vector2 VibrationTranslationM;
        Vector2 VibrationTranslationVelocityMpS;

        int VibrationTrackNode;
        int VibrationTrackVectorSection;
        float VibrationTrackCurvaturepM;

        float TiltingZRot; // Actual tilting angle
        float MinTiltSpeedMpS = MpS.FromKpH(50.0f); // Minimum speed for tilting to be activated
        float MaxTiltAngleRad = MathHelper.ToRadians(8.0f); // Maximum angle of tilting allowed

        internal void UpdateVibration(Traveller traveler, float elapsedTimeS, float distanceM)
        {
            // We need to avoid introducing any unbounded effects, so cap the elapsed time to 0.25 seconds (4FPS).
            if (elapsedTimeS > 0.25f)
                return;
            // NOTE: Traveller is at the FRONT of the TrainCar!

            // Don't add vibrations to train cars less than 2.5 meter in length; they're unsuitable for these calculations.
            // Don't let vibrate car before EOT to avoid EOT not moving together with that car
            // Don't add vibrations to train cars with no bogies to prevent wheels vibrating around
            if (CarLengthM < 2.5f || (Train.EOT != null && Train.Cars.Count > 1 && Train.Cars[Train.Cars.Count - 2] == this) || Parts.Count <= 1)
                return;
            if (Simulator.Settings.CarVibratingLevel != 0)
            {

                //var elapsedTimeS = Math.Abs(speedMpS) > 0.001f ? distanceM / speedMpS : 0;
                if (VibrationOffsetM.X == 0)
                {
                    // Initialize three different offsets (0 - 1 meters) so that the different components of the vibration motion don't align.
                    VibrationOffsetM.X = (float)Simulator.Random.NextDouble();
                    VibrationOffsetM.Y = (float)Simulator.Random.NextDouble();
                    VibrationOffsetM.Z = (float)Simulator.Random.NextDouble();
                }

                if (VibrationTrackVectorSection == 0)
                    VibrationTrackVectorSection = traveler.TrackVectorSectionIndex;
                if (VibrationTrackNode == 0)
                    VibrationTrackNode = traveler.TrackNodeIndex;

                // Apply suspension/spring and damping.
                // https://en.wikipedia.org/wiki/Simple_harmonic_motion
                //   Let F be the force in N
                //   Let k be the spring constant in N/m or kg/s/s
                //   Let x be the displacement in m
                //   Then F = -k * x
                // Given F = m * a, solve for a:
                //   a = F / m
                // Substitute F:
                //   a = -k * x / m
                // Because our spring constant was never multiplied by m, we can cancel that out:
                //   a = -k' * x
                var rotationAccelerationRadpSpS = -VibrationSpringConstantPrimepSpS * VibrationRotationRad;
                var translationAccelerationMpSpS = -VibrationSpringConstantPrimepSpS * VibrationTranslationM;
                // https://en.wikipedia.org/wiki/Damping
                //   Let F be the force in N
                //   Let c be the damping coefficient in N*s/m
                //   Let v be the velocity in m/s
                //   Then F = -c * v
                // We apply the acceleration (let t be time in s, then dv/dt = a * t) and damping (-c * v) to the velocities:
                VibrationRotationVelocityRadpS += rotationAccelerationRadpSpS * elapsedTimeS - VibratioDampingCoefficient * VibrationRotationVelocityRadpS;
                VibrationTranslationVelocityMpS += translationAccelerationMpSpS * elapsedTimeS - VibratioDampingCoefficient * VibrationTranslationVelocityMpS;
                // Now apply the velocities (dx/dt = v * t):
                VibrationRotationRad += VibrationRotationVelocityRadpS * elapsedTimeS;
                VibrationTranslationM += VibrationTranslationVelocityMpS * elapsedTimeS;

                // Add new vibrations every CarLengthM in either direction.
                if (Math.Round((VibrationOffsetM.X + DistanceM) / CarLengthM) != Math.Round((VibrationOffsetM.X + DistanceM + distanceM) / CarLengthM))
                {
                    AddVibrations(VibrationFactorDistance);
                }

                // Add new vibrations every track vector section which changes the curve radius.
                if (VibrationTrackVectorSection != traveler.TrackVectorSectionIndex)
                {
                    var curvaturepM = MathHelper.Clamp(traveler.GetCurvature(), -VibrationMaximumCurvaturepM, VibrationMaximumCurvaturepM);
                    if (VibrationTrackCurvaturepM != curvaturepM)
                    {
                        // Use the difference in curvature to determine the strength of the vibration caused.
                        AddVibrations(VibrationFactorTrackVectorSection * Math.Abs(VibrationTrackCurvaturepM - curvaturepM) / VibrationMaximumCurvaturepM);
                        VibrationTrackCurvaturepM = curvaturepM;
                    }
                    VibrationTrackVectorSection = traveler.TrackVectorSectionIndex;
                }

                // Add new vibrations every track node.
                if (VibrationTrackNode != traveler.TrackNodeIndex)
                {
                    AddVibrations(VibrationFactorTrackNode);
                    VibrationTrackNode = traveler.TrackNodeIndex;
                }

                Matrix rotation = Matrix.CreateFromYawPitchRoll(VibrationRotationRad.Y, VibrationRotationRad.X, VibrationRotationRad.Z);
                Matrix translation = Matrix.CreateTranslation(VibrationTranslationM.X, VibrationTranslationM.Y, 0);
                WorldPosition.XNAMatrix = rotation * translation * WorldPosition.XNAMatrix;
                VibrationInverseMatrix = Matrix.Invert(rotation * translation);
            }
        }

        private void AddVibrations(float factor)
        {
            // NOTE: For low angles (as our vibration rotations are), sin(angle) ~= angle, and since the displacement at the end of the car is sin(angle) = displacement/half-length, sin(displacement/half-length) * half-length ~= displacement.
            switch (Simulator.Random.Next(4))
            {
                case 0:
                    VibrationRotationVelocityRadpS.Y += factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * 2 / CarLengthM;
                    break;
                case 1:
                    VibrationRotationVelocityRadpS.Z += factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength * 2 / CarLengthM;
                    break;
                case 2:
                    VibrationTranslationVelocityMpS.X += factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength;
                    break;
                case 3:
                    VibrationTranslationVelocityMpS.Y += factor * Simulator.Settings.CarVibratingLevel * VibrationIntroductionStrength;
                    break;
            }
        }
        #endregion

        public bool IsOverSwitch { get; private set; }
        public bool IsOverCrossover { get; private set; }
        public bool IsOverTrough { get; private set; }

        void UpdatePositionFlags()
        {
            // Position flags can only change when we're moving!
            if (Train == null || AbsSpeedMpS < 0.01f) return;

            // Calculate the position of the ends of this car relative to the REAR of the train
            var rearOffsetM = Train.PresentPosition[1].TCOffset;
            for (var i = Train.Cars.IndexOf(this) + 1; i < Train.Cars.Count; i++)
                rearOffsetM += Train.Cars[i - 1].CouplerSlackM + Train.Cars[i - 1].GetCouplerZeroLengthM() + Train.Cars[i].CarLengthM;
            var frontOffsetM = rearOffsetM + CarLengthM;

            var isOverSwitch = false;
            var isOverCrossover = false;
            var isOverTrough = false;

            // Scan through the track sections forwards from the REAR of the train (`Train.PresentPosition[1]`),
            // stopping as soon as we've passed this car (`checkedM`) or run out of track (`currentPin.Link`)
            var checkedM = 0f;
            var lastPin = new TrPin { Link = -1, Direction = -1 };
            var currentPin = new TrPin { Link = Train.PresentPosition[1].TCSectionIndex, Direction = Train.PresentPosition[1].TCDirection };
            while (checkedM <= frontOffsetM && currentPin.Link != -1)
            {
                var section = Simulator.Signals.TrackCircuitList[currentPin.Link];

                // Does this car overlap this track section?
                if (checkedM <= frontOffsetM && rearOffsetM <= checkedM + section.Length)
                {
                    if (section.CircuitType == TrackCircuitSection.TrackCircuitType.Junction) isOverSwitch = true;
                    if (section.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover) isOverCrossover = true;
                    if (section.TroughInfo != null)
                    {
                        foreach (var troughs in section.TroughInfo)
                        {
                            var trough = troughs[currentPin.Direction];
                            // Start and end are -1 if the trough extends beyond this section
                            var troughStart = trough.TroughStart < 0 ? 0 : trough.TroughStart;
                            var troughEnd = trough.TroughEnd < 0 ? section.Length : trough.TroughEnd;
                            if (checkedM + troughStart <= frontOffsetM && rearOffsetM <= checkedM + troughEnd) isOverTrough = true;
                        }
                    }
                }
                checkedM += section.Length;

                var nextPin = section.GetNextActiveLink(currentPin.Direction, lastPin.Link);
                lastPin = currentPin;
                currentPin = nextPin;
            }

            IsOverSwitch = isOverSwitch;
            IsOverCrossover = isOverCrossover;
            IsOverTrough = isOverTrough;
        }

        // TODO These three fields should be in the TrainCarViewer.
        public int TrackSoundType = 0;
        public WorldLocation TrackSoundLocation = WorldLocation.None;
        public float TrackSoundDistSquared = 0;

        public static WorldLocation TileLocation(UiD uid)
        {
            return new WorldLocation(uid.TileX, uid.TileZ, uid.X, uid.Y, uid.Z);
        }

        public virtual void SwitchToPlayerControl()
        {
            return;
        }

        public virtual void SwitchToAutopilotControl()
        {
            return;
        }

        public virtual float GetFilledFraction(uint pickupType)
        {
            return 0f;
        }

        /// <summary>
        /// Returns the coefficient of friction (CoF) for the brake shoe. For legacy operation, it will be a "representation" of the CoF that will adjust 
        /// the brake retard force with speed.
        /// </summary>
        public virtual float GetBrakeShoeFrictionFactor()
        {            
            var frictionfraction = 0.0f;
            float AdhesionMultiplier = Simulator.Settings.AdhesionFactor / 100.0f; // User set adjustment factor - convert to a factor where 100% = no change to adhesion

            if (Simulator.UseAdvancedAdhesion && !Simulator.Settings.SimpleControlPhysics && IsPlayerTrain)
            {
                // Formula 9 and 10 from the paper "Study of the influence of the brake shoe temperature and wheel tread on braking effectiveness" by P Ivanov1, A Khudonogov1,
                // E Dulskiy1, N Manuilov1, A Khamnaeva1, A Korsun1, S Treskin is used for the Cast Iron and Hi Friction Composite brake shoe friction curves.
                // https://iopscience.iop.org/article/10.1088/1742-6596/1614/1/012086/pdf
                //
                // These formulas are based upon the work of Karwatzki, which is described further in "Fahrdynamik des Schienenverkehrs" by Dietrich Wende 2003.
                // Karwatzki developed a formula for brakeshoe friction that used 5 constant values k1, ..... , k5.
                // Wende present a number of different brake shoes k values, two sets of which line up with the UIC values above.

                if (BrakeShoeType == BrakeShoeTypes.Cast_Iron_P6)
                {
                    frictionfraction = KarwatzkiBrakeShoeFriction(0.024f, 62.5f, 12.5f, 100, 20);
                }
                else if (BrakeShoeType == BrakeShoeTypes.Cast_Iron_P10)
                {
                    frictionfraction = KarwatzkiBrakeShoeFriction(0.05f, 62.5f, 31.25f, 100, 20);
                }
                else if (BrakeShoeType == BrakeShoeTypes.High_Friction_Composite)
                {
                    frictionfraction = KarwatzkiBrakeShoeFriction(0.055f, 200, 50, 150, 75);
                }
                else if (BrakeShoeType == BrakeShoeTypes.Disc_Pads)
                {
                    frictionfraction = KarwatzkiBrakeShoeFriction(0.385f, -24.5f, -27.2f, 39.5f, 33);
                }
                else if (BrakeShoeType == BrakeShoeTypes.User_Defined)
                {
                    frictionfraction = BrakeShoeFrictionFactor[MpS.ToKpH(AbsSpeedMpS)];
                }
                else // default curve - assume that this is legacy stock
                {
                    if (BrakeShoeFrictionFactor != null)  // User defined friction has been applied in WAG file, but brake shoe has not be described, hence a legacy condition - Assume MaxBrakeForce is correctly set in the WAG, so no adjustment required 
                    {
                        float userFriction = BrakeShoeFrictionFactor[MpS.ToKpH(AbsSpeedMpS)];
                        float zeroUserFriction = BrakeShoeFrictionFactor[MpS.ToKpH(0)];

                        frictionfraction = userFriction / zeroUserFriction * AdhesionMultiplier; // Factor calculated by normalising zero speed value on friction curve applied in WAG file

                        // Clamp adjustment factor to a value of 1.0 - i.e. the brakeforce can never exceed the Brake Force value defined in the WAG file
                        frictionfraction = MathHelper.Clamp(frictionfraction, 0.01f, 1.0f);
                    }
                    else
                    {
                        // Base Curtius - Kniffler equation - u = 0.50, all other values are scaled off this formula
                        float defaultBrakeShoeCoefficientFriction = (7.6f / (MpS.ToKpH(AbsSpeedMpS) + 17.5f) + 0.07f) * AdhesionMultiplier;

                        // Assuming that current MaxBrakeForce has been set with an existing Friction CoF of 0.2f, an adjustment factor needs to be developed to reduce
                        // the MAxBrakeForce by a relative amount. Note force will be higher then ENG file value at low speed and reduce to actual value at higher speeds.
                        frictionfraction = defaultBrakeShoeCoefficientFriction / 0.2f * AdhesionMultiplier;  // Assuming that current MaxBrakeForce has been set with an existing Friction CoF of 0.2f, an adjustment factor needs to be developed to reduce the MAxBrakeForce by a relative amount

                        // Clamp adjustment factor to a value of 1.0 - i.e. the brakeforce can never exceed the Brake Force value defined in the WAG file
                        frictionfraction = MathHelper.Clamp(frictionfraction, 0.01f, 1.0f);
                    }
                }

                return frictionfraction;
            }
            else
            {
                // set default values if simple adhesion model, or if diesel or electric locomotive is used, which doesn't check for brake skid.

                frictionfraction = 1.0f;  // Default value set to leave existing brakeforce constant regardless of changing speed
                return frictionfraction;
            }
        }

        /// <summary>
        /// Returns the coefficient of friction (CoF) of the brake shoe for display on the HuD.
        /// For legacy operation, it will be different to the "representation" of the CoF calculated above, and be the actual CoF.
        /// </summary>
        public virtual float GetBrakeShoeFrictionCoefficientHuD()
        {
            var frictionfraction = 0.0f;
            float AdhesionMultiplier = Simulator.Settings.AdhesionFactor / 100.0f; // User set adjustment factor - convert to a factor where 100% = no change to adhesion

            if (Simulator.UseAdvancedAdhesion && !Simulator.Settings.SimpleControlPhysics && IsPlayerTrain)
            {
                // Formula 9 and 10 from the paper "Study of the influence of the brake shoe temperature and wheel tread on braking effectiveness" by P Ivanov1, A Khudonogov1,
                // E Dulskiy1, N Manuilov1, A Khamnaeva1, A Korsun1, S Treskin is used for the Cast Iron and Hi Friction Composite brake shoe friction curves.
                // https://iopscience.iop.org/article/10.1088/1742-6596/1614/1/012086/pdf
                //
                // These formulas are based upon the work of Karwatzki, which is described further in "Fahrdynamik des Schienenverkehrs" by Dietrich Wende 2003.
                // Karwatzki developed a formula for brakeshoe friction that used 5 constant values k1, ..... , k5.
                // Wende present a number of different brake shoes k values, two sets of which line up with the UIC values above.

                if (BrakeShoeType == BrakeShoeTypes.Cast_Iron_P6)
                {
                    frictionfraction = KarwatzkiBrakeShoeFriction (0.024f, 62.5f, 12.5f, 100, 20);
                }
                else if (BrakeShoeType == BrakeShoeTypes.Cast_Iron_P10)
                {
                    frictionfraction = KarwatzkiBrakeShoeFriction(0.05f, 62.5f, 31.25f, 100, 20);
                }
                else if (BrakeShoeType == BrakeShoeTypes.High_Friction_Composite)
                {
                    frictionfraction = KarwatzkiBrakeShoeFriction(0.055f, 200, 50f, 150, 75);
                }
                else if (BrakeShoeType == BrakeShoeTypes.Disc_Pads)
                {
                    frictionfraction = KarwatzkiBrakeShoeFriction(0.385f, -24.5f, -27.2f, 39.5f, 33);
                }
                else if (BrakeShoeType == BrakeShoeTypes.User_Defined)
                {
                    frictionfraction = BrakeShoeFrictionFactor[MpS.ToKpH(AbsSpeedMpS)];
                }
                else // default curve - assume that this is legacy stock
                {
                    if (BrakeShoeFrictionFactor != null)  // User defined friction has been applied in WAG file, but brake shoe has not be described, hence a legacy condition - Assume MaxBrakeForce is correctly set in the WAG, so no adjustment required 
                    {
                        frictionfraction = BrakeShoeFrictionFactor[MpS.ToKpH(AbsSpeedMpS)];
                    }
                    else
                    {
                        frictionfraction = 7.6f / ((MpS.ToKpH(AbsSpeedMpS) + 17.5f) + 0.07f) * AdhesionMultiplier; // Base Curtius - Kniffler equation - u = 0.50, all other values are scaled off this formula
                    }
                }

                return frictionfraction;
            }
            else
            {
                frictionfraction = 1.0f;  // Default value set to leave existing brakeforce constant regardless of changing speed
                return frictionfraction;
            }
        }

        public float KarwatzkiBrakeShoeFriction ( float k1, float k2, float k3, float k4, float k5 )
        {
                var friction = 0.0f;
                float NewtonsTokNewtons = 0.001f;
                float brakeShoeForcekN = NewtonsTokNewtons * BrakeShoeForceN / NumberCarBrakeShoes;
            if (brakeShoeForcekN > 20) // Make sure that brake shoe force doesn't exceed 20 as it will cause a -ve brakeshoe CoF
            {
                brakeShoeForcekN = 20;
            }
                friction = k1 * ((brakeShoeForcekN + k2) / (brakeShoeForcekN + k3)) * ((MpS.ToKpH(AbsSpeedMpS) + k4) / (MpS.ToKpH(AbsSpeedMpS) + k5));

                return friction;
        }

        public virtual void InitializeCarHeatingVariables()
        {
            MSTSLocomotive mstsLocomotive = Train.LeadLocomotive as MSTSLocomotive;

            // Only initialise these values the first time around the loop
            if (!IsCarHeatingInitialized)
            {
                if (mstsLocomotive.EngineType == EngineTypes.Steam && Simulator.Settings.HotStart || mstsLocomotive.EngineType == EngineTypes.Diesel || mstsLocomotive.EngineType == EngineTypes.Electric)
                {
                    if (CarOutsideTempC < DesiredCompartmentTempSetpointC)
                    {
                        CarInsideTempC = DesiredCompartmentTempSetpointC; // Set intial temp
                    }
                    else
                    {
                        CarInsideTempC = CarOutsideTempC;
                    }
                }
                else
                {
                    CarInsideTempC = CarOutsideTempC;
                }

                // Calculate a random factor for steam heat leaks in connecting pipes
                SteamHoseLeakRateRandom = Simulator.Random.Next(100) / 100.0f; // Achieves a two digit random number betwee 0 and 1
                SteamHoseLeakRateRandom = MathHelper.Clamp(SteamHoseLeakRateRandom, 0.5f, 1.0f); // Keep Random Factor ratio within bounds

                // Initialise current Train Steam Heat based upon selected Current carriage Temp
                // Calculate Starting Heat value in Car Q = C * M * Tdiff, where C = Specific heat capacity, M = Mass ( Volume * Density), Tdiff - difference in temperature
                CarHeatCurrentCompartmentHeatJ = J.FromKJ(SpecificHeatCapacityAirKJpKgK * DensityAirKgpM3 * CarHeatVolumeM3 * (CarInsideTempC - CarOutsideTempC));

                IsCarHeatingInitialized = true;
            }
        }

        public virtual void UpdateHeatLoss()
        {
            // Heat loss due to train movement and air flow, based upon convection heat transfer information - http://www.engineeringtoolbox.com/convective-heat-transfer-d_430.html
            // The formula on this page ( hc = 10.45 - v + 10v1/2), where v = m/s. This formula is used to develop a multiplication factor with train speed.
            // Curve is only valid between 2.0m/s and 20.0m/s

            // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // Calculate heat loss from inside the carriage
            // Initialise car values for heating to zero
            TotalCarCompartmentHeatLossW = 0.0f;
            CarHeatCompartmentPipeAreaM2 = 0.0f;

            // Transmission heat loss = exposed area * heat transmission coeff (inside temp - outside temp)
            // Calculate the heat loss through the roof, wagon sides, and floor separately  
            // Calculate the heat loss through the carriage sides, per degree of temp change
            // References - https://www.engineeringtoolbox.com/heat-loss-transmission-d_748.html  and https://www.engineeringtoolbox.com/heat-loss-buildings-d_113.html
            float HeatTransCoeffRoofWpm2C = 1.7f * ConvectionFactor; // 2 inch wood - uninsulated
            float HeatTransCoeffEndsWpm2C = 0.9f * ConvectionFactor; // 2 inch wood - insulated - this compensates for the fact that the ends of the cars are somewhat protected from the environment
            float HeatTransCoeffSidesWpm2C = 1.7f * ConvectionFactor; // 2 inch wood - uninsulated
            float HeatTransCoeffWindowsWpm2C = 4.7f * ConvectionFactor; // Single glazed glass window in wooden frame
            float HeatTransCoeffFloorWpm2C = 2.5f * ConvectionFactor; // uninsulated floor

            // Calculate volume in carriage - note height reduced by 1.06m to allow for bogies, etc
            float CarCouplingPipeM = 1.2f;  // Allow for connection between cars (assume 2' each end) - no heat is contributed to carriages.

            // Calculate the heat loss through the roof, allow 15% additional heat loss through roof because of radiation to space
            float RoofHeatLossFactor = 1.15f;
            float HeatLossTransRoofW = RoofHeatLossFactor * (CarWidthM * (CarLengthM - CarCouplingPipeM)) * HeatTransCoeffRoofWpm2C * (CarInsideTempC - CarOutsideTempC);

            // Each car will have 2 x sides + 2 x ends. Each side will be made up of solid walls, and windows. A factor has been assumed to determine the ratio of window area to wall area.
            float HeatLossTransWindowsW = (WindowDeratingFactor * (CarHeightM - BogieHeightM) * (CarLengthM - CarCouplingPipeM)) * HeatTransCoeffWindowsWpm2C * (CarInsideTempC - CarOutsideTempC);
            float HeatLossTransSidesW = (1.0f - WindowDeratingFactor) * (CarHeightM - BogieHeightM) * (CarLengthM - CarCouplingPipeM) * HeatTransCoeffSidesWpm2C * (CarInsideTempC - CarOutsideTempC);
            float HeatLossTransEndsW = (CarHeightM - BogieHeightM) * (CarLengthM - CarCouplingPipeM) * HeatTransCoeffEndsWpm2C * (CarInsideTempC - CarOutsideTempC);

            // Total equals 2 x sides, ends, windows
            float HeatLossTransTotalSidesW = (2.0f * HeatLossTransWindowsW) + (2.0f * HeatLossTransSidesW) + (2.0f * HeatLossTransEndsW);

            // Calculate the heat loss through the floor
            float HeatLossTransFloorW = CarWidthM * (CarLengthM - CarCouplingPipeM) * HeatTransCoeffFloorWpm2C * (CarInsideTempC - CarOutsideTempC);

            float HeatLossTransmissionW = HeatLossTransRoofW + HeatLossTransTotalSidesW + HeatLossTransFloorW;

            // ++++++++++++++++++++++++
            // Ventilation Heat loss, per degree of temp change
            // This will occur when the train is stopped at the station and prior to being ready to depart. Typically will only apply in activity mode, and not explore mode
            float HeatLossVentilationW = 0;
            float HeatRecoveryEfficiency = 0.5f; // Assume a HRF of 50%
            float AirFlowVolumeM3pS = CarHeatVolumeM3 / 300.0f; // Assume that the volume of the car is emptied over a period of 5 minutes

            if (Train.AtStation && !Train.MayDepart) // When train is at station, if the train is ready to depart, assume all doors are closed, and hence no ventilation loss
            {
                HeatLossVentilationW = W.FromKW((1.0f - HeatRecoveryEfficiency) * SpecificHeatCapacityAirKJpKgK * DensityAirKgpM3 * AirFlowVolumeM3pS * (CarInsideTempC - CarOutsideTempC));
            }

            // ++++++++++++++++++++++++
            // Infiltration Heat loss, per degree of temp change
            float NumAirShiftspSec = pS.FrompH(10.0f);      // Pepper article suggests that approx 14 air changes per hour happen for a train that is moving @ 50mph, use and av figure of 10.0.
            float HeatLossInfiltrationW = W.FromKW(SpecificHeatCapacityAirKJpKgK * DensityAirKgpM3 * NumAirShiftspSec * CarHeatVolumeM3 * (CarInsideTempC - CarOutsideTempC));

            TotalCarCompartmentHeatLossW = HeatLossTransmissionW + HeatLossInfiltrationW + HeatLossVentilationW;
        }

        /// <summary>
        /// Determine latitude/longitude position of the current TrainCar
        /// </summary>
        public LatLonDirection GetLatLonDirection()
        {
            double lat = 0;
            double lon = 0;

            var playerLocation = WorldPosition.WorldLocation;

            new WorldLatLon().ConvertWTC(playerLocation.TileX, playerLocation.TileZ, playerLocation.Location, ref lat, ref lon);

            LatLon latLon = new LatLon(
                MathHelper.ToDegrees((float)lat),
                MathHelper.ToDegrees((float)lon));

            float direction = (float)Math.Atan2(WorldPosition.XNAMatrix.M13, WorldPosition.XNAMatrix.M11);
            float directionDeg = MathHelper.ToDegrees((float)direction);

            if (Direction == Direction.Reverse)
            {
                directionDeg += 180.0f;
            }
            var loco = this as MSTSLocomotive;
            if (loco.UsingRearCab)
            {
                directionDeg += 180.0f;
            }
            while (directionDeg > 360)
            {
                directionDeg -= 360;
            }

            return new LatLonDirection(latLon, directionDeg); ;
        }

        public int GetWagonNumAxles() { return WagonNumAxles; }

        public float GetGravitationalAccelerationMpS2() { return GravitationalAccelerationMpS2; }

        /// <summary>
        /// Update the gravity force and % gradient of this train car at the current position
        /// </summary>
        public void UpdateGravity()
        {
            UpdateGravity(WorldPosition.XNAMatrix);
        }

        /// <summary>
        /// Update the gravity force and % gradient of this train car at an arbitrary position
        /// </summary>
        /// <param name="orientation">Matrix giving the train car orientation used to determine gravity.</param>
        public void UpdateGravity(Matrix orientation)
        {
            // Percent slope = 100 * rise / run -> the Y component of the forward vector gives us the 'rise'
            // Derive the 'run' by assuming a hypotenuse length of 1, so per Pythagoras run = sqrt(1 - rise^2)
            float rise = orientation.Backward.Y;

            GravityForceN = MassKG * GravitationalAccelerationMpS2 * rise;
            CurrentElevationPercent = 100f * (rise / (float)Math.Sqrt(1 - rise * rise));

            // Reverse gravity force and % gradient on locomotives operated from the rear cab
            // FUTURE: Change rear cabs to not require such forbidden manipulations of physics
            if (IsDriveable && Train != null & Train.IsPlayerDriven && (this as MSTSLocomotive).UsingRearCab)
            {
                GravityForceN = -GravityForceN;
                CurrentElevationPercent = -CurrentElevationPercent;
            }
        }
    }

    public class WheelAxle : IComparer<WheelAxle>
    {
        public Vector3 OffsetM;   // Offset from the bogie center
        public int BogieIndex;    // Index in the Parts list of the bogie this is attached to
        public int BogieMatrix;   // Index in the matrix hierarchy of the bogie this is attached to
        public TrainCarPart Part; // Reference to the object for the bogie this is attached to
        public bool Fake;         // True for axles that aren't present in the 3D model
        public WheelAxle(Vector3 offset, int bogie, int parentMatrix, bool fake = false)
        {
            OffsetM = offset;
            BogieIndex = bogie;
            BogieMatrix = parentMatrix;
            Fake = fake;
        }
        public int Compare(WheelAxle a, WheelAxle b)
        {
            if (a.OffsetM.Z > b.OffsetM.Z)
                return 1;
            if (a.OffsetM.Z < b.OffsetM.Z)
                return -1;
            return 0;
        }
    }

    // data and methods used to align trucks and models to track
    public class TrainCarPart
    {
        public Vector3 OffsetM; // Position offset for this part relative to parent
        public int iMatrix; // matrix index in shape that needs to be moved
        // line fitting variables
        public double SumWgt; // Sum of component weights
        public double SumZOffset; // Sum of component weights times Z-offsets
        public double SumZOffsetSq; // Sum of component weights times Z-offsets squared
        public double[] SumPos = new double[3]; // Sum of component locations [x, y, z]
        public double[] SumPosZOffset = new double[3]; // Sum of component locations [x, y, z] times Z-offsets
        public float[] Pos = new float[3]; // Position [x, y, z] of this part, calculated with y-intercept of linear regression
        public float[] Dir = new float[3]; // Orientation [x, y, z] of this part, calculated with slope of linear regression
        public float SumRoll; // Sum of all roll angles of components
        public float Roll; // Roll angle of this part
        public bool Bogie; // True if this is a bogie
        public TrainCarPart(Vector3 offset, int i)
        {
            OffsetM = offset;
            iMatrix = i;
        }

        /// <summary>
        /// Resets the linear regression counters for this part
        /// </summary>
        public void InitLineFit()
        {
            SumWgt = SumZOffset = SumZOffsetSq = 0;
            for (int i = 0; i < 3; i++)
                SumPos[i] = SumPosZOffset[i] = 0;
            SumRoll = 0;
        }

        /// <summary>
        /// Directly adds the 3D position values of a sub part to this part. The position
        /// of sub parts will be used to derive the position of this part.
        /// </summary>
        public void AddWheelSetLocation(float weight, float zOffset, Vector3 position, float roll)
        {
            SumWgt += weight;
            SumZOffset += weight * zOffset;
            SumZOffsetSq += weight * zOffset * zOffset;
            SumPos[0] += weight * position.X;
            SumPosZOffset[0] += weight * position.X * zOffset;
            SumPos[1] += weight * position.Y;
            SumPosZOffset[1] += weight * position.Y * zOffset;
            SumPos[2] += weight * position.Z;
            SumPosZOffset[2] += weight * position.Z * zOffset;
            SumRoll += weight * roll;
        }

        /// <summary>
        /// Adds data of child part using a reference to the child part for calculating the position of this part.
        /// </summary>
        public void AddPartLocation(float weight, TrainCarPart part)
        {
            SumWgt += weight;
            SumZOffset += weight * part.OffsetM.Z;
            SumZOffsetSq += weight * part.OffsetM.Z * part.OffsetM.Z;
            for (int i = 0; i < 3; i++)
            {
                float position = part.Pos[i] + part.OffsetM.Z * part.Dir[i];
                SumPos[i] += weight * position;
                SumPosZOffset[i] += weight * position * part.OffsetM.Z;
            }
            SumRoll += weight * part.Roll;
        }

        /// <summary>
        /// Completes linear regression to determine the position and orientation of this part
        /// based on the position of child parts added previously.
        /// </summary>
        public void FindCenterLine()
        {
            // 2D Least regression between the offsets (along longitudinal axis of rail vehicle)
            // and actual positions in 3D space, repeated 3 times for each dimension in 3D.

            // Follows format of y = M * x + B where x is the forward/backward position along the train car axis
            // and y is the actual (x, y, or z) position in 3D space. We need to determine vectors B (the 3D
            // position of this part) and M (the 3D orientation of this part) using the offsets and positions added previously.

            // Denominator for regression calculation. N * sum(x^2) - (sum(x))^2 where N is the total weight and x is the offset.
            double denominator = SumWgt * SumZOffsetSq - SumZOffset * SumZOffset;
            if (denominator > 1e-20)
            {
                for (int i = 0; i < 3; i++)
                {
                    // The direction (M) is defined as 'M = [N * sum(x * y) - sum(x) * sum (y)] / denominator'
                    // where N is the total weight, x is the offset, and y is the 3D position
                    Dir[i] = (float)((SumWgt * SumPosZOffset[i] - SumZOffset * SumPos[i]) / denominator);
                    // The position (B) is defined as 'B = [sum(y) - M * sum(x)] / N', where N is the total
                    // weight, x is the offset, y is the 3D position, and M is the direction value from earlier.
                    // This uses an equivalent form that doesn't use the result of the above calculation to avoid
                    // precision errors from the value being converted to a float.
                    Pos[i] = (float)((SumZOffsetSq * SumPos[i] - SumZOffset * SumPosZOffset[i]) / denominator);
                }
            }
            else // Improperly defined wagon, fallback to basic calculation
            {
                for (int i = 0; i < 3; i++)
                {
                    Pos[i] = (float)(SumPos[i] / SumWgt);
                    Dir[i] = 0;
                }
            }

            Roll = SumRoll / (float)SumWgt;
        }
    }
}
