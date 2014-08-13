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

// Define this to log the wheel configurations on cars as they are loaded.
//#define DEBUG_WHEELS

using Microsoft.Xna.Framework;
using MSTS.Formats;
using ORTS.Common;
using ORTS.Scripting.Api;
using ORTS.Viewer3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Camera = ORTS.Viewer3D.Camera;

namespace ORTS
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

        // sound related variables
        public bool IsPartOfActiveTrain = true;
        public List<int> SoundSourceIDs = new List<int>();

        // some properties of this car
        public float LengthM = 40;       // derived classes must overwrite these defaults
        public float HeightM = 4;        // derived classes must overwrite these defaults
        public float MassKG = 10000;
        public bool IsDriveable;
        public bool IsFreight;           // indication freight wagon or passenger car
        public bool IsTender;
        public bool HasFreightAnim = false;
        public bool HasPassengerCapacity = false;
        public bool HasInsideView = false;

        public LightCollection Lights;
        public int Headlight;

        // instance variables set by train physics when it creates the traincar
        public Train Train;  // the car is connected to this train
        public bool Flipped; // the car is reversed in the consist
        public int UiD;
        public string CarID = "AI"; //CarID = "0 - UID" if player train, "ActivityID - UID" if loose consist, "AI" if AI train

        // status of the traincar - set by the train physics after it calls TrainCar.Update()
        public WorldPosition WorldPosition = new WorldPosition();  // current position of the car
        public Matrix RealXNAMatrix = Matrix.Identity;
        public float DistanceM;  // running total of distance travelled - always positive, updated by train physics
        public float _SpeedMpS; // meters per second; updated by train physics, relative to direction of car  50mph = 22MpS
        public float _PrevSpeedMpS;
        public float CouplerSlackM;  // extra distance between cars (calculated based on relative speeds)
        public float CouplerSlack2M;  // slack calculated using draft gear force
        public bool WheelSlip;  // true if locomotive wheels slipping
        public bool WheelSlipWarning;
        public float _AccelerationMpSS;
        private float Stiffness = 3.0f; //used by vibrating cars
        private float MaxVibSpeed = 15.0f;//the speed when max shaking happens
        private IIRFilter AccelerationFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, 1.0f, 0.1f);

        public bool AcceptMUSignals = true; //indicates if the car accepts multiple unit signals

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
        public Matrix GetXNAMatrix() //in case of train car vibrating, the camera needs to stay stable
        {
            if (RealXNAMatrix == Matrix.Identity) return WorldPosition.XNAMatrix;
            return RealXNAMatrix;
        }

        public Matrix SuperElevationMatrix = Matrix.Identity;

        public float LocalThrottlePercent;
        // represents the MU line travelling through the train.  Uncontrolled locos respond to these commands.
        public float ThrottlePercent 
        { 
            get
            {
                if (AcceptMUSignals && Train != null)
                    return Train.MUThrottlePercent;
                else
                    return LocalThrottlePercent;
            }
            set
            {
                if (AcceptMUSignals && Train != null)
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
                if (AcceptMUSignals)
                    return Train.MUGearboxGearIndex;
                else
                    return LocalGearboxGearIndex;
            } 
            set
            {
                if (AcceptMUSignals)
                    Train.MUGearboxGearIndex = value;
                else
                    LocalGearboxGearIndex = value;
            }
        }
        public float DynamicBrakePercent { get { return Train.MUDynamicBrakePercent; } set { Train.MUDynamicBrakePercent = value; } }
        public Direction Direction
        { 
            get { return Flipped ? DirectionControl.Flip(Train.MUDirection) : Train.MUDirection; } 
            set { Train.MUDirection = Flipped ? DirectionControl.Flip( value ) : value; } }
        public BrakeSystem BrakeSystem;

        // TrainCar.Update() must set these variables
        public float MotiveForceN;   // ie motor power in Newtons  - signed relative to direction of car - 
        public SmoothedData MotiveForceSmoothedN = new SmoothedData(0.5f);
        public float PrevMotiveForceN;
        public float GravityForceN;  // Newtons  - signed relative to direction of car - 
        public float CurveForceN;   // in Newtons
        public float FrictionForceN; // in Newtons ( kg.m/s^2 ) unsigned, includes effects of curvature
        public float BrakeForceN;    // brake force in Newtons
        public float TotalForceN; // sum of all the forces active on car relative train direction

        public float CurrentElevationPercent;

        public bool CurveResistanceSpeedDependent;
        public bool CurveSpeedDependent;

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
        public bool WheelAxlesLoaded;
        public List<TrainCarPart> Parts = new List<TrainCarPart>();

        // For use by cameras, initialized in MSTSWagon class and its derived classes
        public List<PassengerViewPoint> PassengerViewpoints = new List<PassengerViewPoint>();
        public List<ViewPoint> CabViewpoints; //three dimensional cab view point
        public List<ViewPoint> HeadOutViewpoints = new List<ViewPoint>();

        // Used by Curve Speed Method
        float TrackGaugeM;  // Track gauge - read in MSTSWagon
        float CentreOfGravityM; // get centre of gravity - read in MSTSWagon
        float SuperelevationM; // Super elevation on the curve
        float UnbalancedSuperElevationM;  // Unbalanced superelevation, read from MSTS Wagon File
        float SuperElevationTotalM; // Total superelevation
        bool IsMaxSafeCurveSpeed = false; // Has equal loading speed around the curve been exceeded, ie are all the wheesl still on the track?
        bool IsCriticalSpeed = true; // Has the critical speed around the curve been reached, is is the wagon about to overturn?
        float MaxCurveEqualLoadSpeedMps; // Max speed that rolling stock can do whist maintaining equal load on track
        float StartCurveResistanceFactor = 2.0f; // Set curve friction at Start = 200%
        float RouteSpeedMpS; // Max Route Speed Limit
        const float GravitationalAccelerationMpS2 = 9.80665f; // Acceleration due to gravity 9.80665 m/s2
        float WagonNumWheels; // Number of wheels on a wagon
        float LocoNumDrvWheels;    // Number of drive wheels on locomotive
        float DriverWheelRadiusM; // Drive wheel radius of locomotive wheels
        string WagonType;
        string EngineType;
        float CurveResistanceZeroSpeedFactor = 0.5f; // Based upon research (Russian experiments - 1960) the older formula might be about 2x actual value
        float CoefficientFriction = 0.5f; // Initialise coefficient of Friction - 0.5 for dry rails, 0.1 - 0.3 for wet rails
        float RigidWheelBaseM;   // Vehicle rigid wheelbase, read from MSTS Wagon file
        
        public virtual void Initialize()
        {
            CurveResistanceSpeedDependent = Simulator.Settings.CurveResistanceSpeedDependent;
            CurveSpeedDependent = Simulator.Settings.CurveSpeedDependent;
        }

        // called when it's time to update the MotiveForce and FrictionForce
        public virtual void Update(float elapsedClockSeconds)
        {
            // gravity force, M32 is up component of forward vector
            GravityForceN = MassKG * GravitationalAccelerationMpS2 * WorldPosition.XNAMatrix.M32;
            CurrentElevationPercent = 100f * WorldPosition.XNAMatrix.M32;
            UpdateCurveSpeedLimit(); // call this first as it will provide inputs for the curve force.
            UpdateCurveForce();
            
            // acceleration
            if (elapsedClockSeconds > 0.0f)
            {
                _AccelerationMpSS = (_SpeedMpS - _PrevSpeedMpS) / elapsedClockSeconds;
                
                if (Simulator.UseAdvancedAdhesion)
                    _AccelerationMpSS = AccelerationFilter.Filter(_AccelerationMpSS, elapsedClockSeconds);

                _PrevSpeedMpS = _SpeedMpS;
            }
            UpdateSoundPosition();
        }


        #region Calculate permissible speeds around curves
        /// <summary>
        /// Reads current curve radius and computes the maximum recommended speed around the curve based upon the 
        /// superelevation of the track
        /// </summary>
        public virtual void UpdateCurveSpeedLimit()
        {
            float s = Math.Abs(SpeedMpS); // speed of train
            CentreOfGravityM = GetCentreofGravityM();
            TrackGaugeM = GetTrackGaugeM();
            UnbalancedSuperElevationM = GetUnbalancedSuperElevationM();
          
       

            // get curve radius

            RouteSpeedMpS = (float)Simulator.TRK.Tr_RouteFile.SpeedLimit;
             
            if (CurveSpeedDependent || CurveResistanceSpeedDependent)  // Function enabled by menu selection for either curve resistance or curve speed limit
            {


                if (CurrentCurveRadius > 0)  // only check curve speed if it is a curve
                {
                    
                    if (CurrentCurveRadius > 2000)
                    {
                        if (RouteSpeedMpS > 55.0)   // If route speed limit is greater then 200km/h, assume high speed passenger route
                        {
                            // Calculate superelevation based upon the route speed limit and the curve radius
                            // SE = ((TrackGauge x Velocity^2 ) / Gravity x curve radius)

                            SuperelevationM = (TrackGaugeM * RouteSpeedMpS * RouteSpeedMpS) / (GravitationalAccelerationMpS2 * CurrentCurveRadius);

                            SuperelevationM = MathHelper.Clamp(SuperelevationM, 0.0f, 0.150f); // If superelevation is greater then 6" (150mm) then limit to this value
                          
                        }
                        else
                        {
                            SuperelevationM = 0.005f;  // Assume minimal superelevation if conventional mixed route
                        }

                    }
                    // Set Superelevation value - based upon standard figures
                    else if (CurrentCurveRadius <= 2000 & CurrentCurveRadius > 1600)
                    {
                        SuperelevationM = 0.0254f;  // Assume 1" (or 0.0254m)
                    }
                    else if (CurrentCurveRadius <= 1600 & CurrentCurveRadius > 1200)
                    {
                        SuperelevationM = 0.038100f;  // Assume 1.5" (or 0.038100m)
                    }
                    else if (CurrentCurveRadius <= 1200 & CurrentCurveRadius > 1000)
                    {
                        SuperelevationM = 0.050800f;  // Assume 2" (or 0.050800m)
                    }
                    else if (CurrentCurveRadius <= 1000 & CurrentCurveRadius > 800)
                    {
                        SuperelevationM = 0.063500f;  // Assume 2.5" (or 0.063500m)
                    }
                    else if (CurrentCurveRadius <= 800 & CurrentCurveRadius > 600)
                    {
                        SuperelevationM = 0.0889f;  // Assume 3.5" (or 0.0889m)
                    }
                    else if (CurrentCurveRadius <= 600 & CurrentCurveRadius > 500)
                    {
                        SuperelevationM = 0.1016f;  // Assume 4" (or 0.1016m)
                    }
                    // for tighter radius curves assume on branch lines and less superelevation
                    else if (CurrentCurveRadius <= 500 & CurrentCurveRadius > 280)
                    {
                        SuperelevationM = 0.0889f;  // Assume 3" (or 0.0762m)
                    }
                    else if (CurrentCurveRadius <= 280 & CurrentCurveRadius > 0)
                    {
                        SuperelevationM = 0.063500f;  // Assume 2.5" (or 0.063500m)
                    }

                    // Calulate equal wheel loading speed for current curve and superelevation - this was considered the "safe" speed to travel around a curve
                    // max equal load speed = SQRT ( (superelevation x gravity x curve radius) / track gauge)
                    // SuperElevation is made up of two components = rail superelevation + the amount of sideways force that a passenger will be comfortable with. This is expressed as a figure similar to superelevation.

                    SuperElevationTotalM = SuperelevationM + UnbalancedSuperElevationM;

                    float MaxSafeCurveSpeedMps = (float)Math.Sqrt((SuperElevationTotalM * GravitationalAccelerationMpS2 * CurrentCurveRadius) / TrackGaugeM);

                    MaxCurveEqualLoadSpeedMps = (float)Math.Sqrt((SuperelevationM * GravitationalAccelerationMpS2 * CurrentCurveRadius) / TrackGaugeM);

                  if (CurveSpeedDependent)
                  {
                  
                    // Test current speed to see if greater then "safe" speed around the curve
                    if (s > MaxSafeCurveSpeedMps)
                    {
                        if (!IsMaxSafeCurveSpeed)
                        {
                            IsMaxSafeCurveSpeed = true; // set flag for IsMaxEqualLoadSpeed reached
                           
                          if (Train.TrainType == ORTS.Train.TRAINTYPE.PLAYER)   
                                {
                                    if (Train.IsFreight)
                                        {
                                            Simulator.Confirmer.Message(ConfirmLevel.Warning, Viewer.Catalog.GetString("You are traveling too fast for this curve. Slow down, your freight may be damaged and your train may derail."));
                                        }
                                    else
                                        {
                                            Simulator.Confirmer.Message(ConfirmLevel.Warning, Viewer.Catalog.GetString("You are travelling too fast for this curve. Slow down, your passengers are feeling uncomfortable and your train may derail."));
                                        }
                                }
                          else{
                                Trace.TraceWarning("At speed {0}mph, train {1} is exceeding the safe speed {2}mph on this curve.", MpS.ToMpH(s), Train.Name, MpS.ToMpH(MaxSafeCurveSpeedMps));
                               }  
                        }
                        else
                        {
                            if (s < MaxSafeCurveSpeedMps)
                            {
                                IsMaxSafeCurveSpeed = false; // reset flag for IsMaxEqualLoadSpeed reached
                            }
                        }
                    }

                    // Calculate critical speed - indicates the speed above which stock will overturn - sum of the centrifrugal force and the vertical weight of the vehicle around the CoG
                    // critical speed = SQRT ( (centrifrugal force x gravity x curve radius) / Vehicle weight)
                    // centrifrugal force = Stock Weight x factor for movement of resultant force due to superelevation.

                    float EJ = (SuperElevationTotalM * CentreOfGravityM) / TrackGaugeM;
                    float KC = (TrackGaugeM / 2.0f) + EJ;
                    const float KgtoTonne = 0.001f;
                    float CentrifrugalForceN = MassKG * KgtoTonne * (KC / CentreOfGravityM);

                    float CriticalSpeedMpS = (float)Math.Sqrt((CentrifrugalForceN * GravitationalAccelerationMpS2 * CurrentCurveRadius) / (MassKG * KgtoTonne));

                    if (s > CriticalSpeedMpS)
                    {
                        if (!IsCriticalSpeed)
                        {
                            IsCriticalSpeed = true; // set flag for IsMaxEqualLoadSpeed reached
                            
                            if (Train.TrainType == ORTS.Train.TRAINTYPE.PLAYER)
                            {
                              Simulator.Confirmer.Message(ConfirmLevel.Warning, Viewer.Catalog.GetString("Your train has overturned."));
                            }
                        }
                        else
                        {
                            if (s < CriticalSpeedMpS)
                            {
                                IsCriticalSpeed = false; // reset flag for IsCriticalSpeed reached
                            }
                        }
                    }

                }
                else
                {
                    // reset flags if train is on a straight
                    IsCriticalSpeed = false;   // reset flag for IsCriticalSpeed reached
                    IsMaxSafeCurveSpeed = false; // reset flag for IsMaxEqualLoadSpeed reached
                }
                
                }
          

            }


        }

        #endregion

        #region Calculate friction force in curves

        /// <summary>
        /// Reads current curve radius and computes the CurveForceN friction. Can be overriden by calling
        /// base.UpdateCurveForce();
        /// CurveForceN *= someCarSpecificCoef;     
        /// </summary>
        public virtual void UpdateCurveForce()
        {
            if (CurveResistanceSpeedDependent)
            {
               
                if (CurrentCurveRadius > 0)
                {

                    if (RigidWheelBaseM == 0)   // Calculate default values if no value in Wag File
                    {
                        WagonNumWheels = 0.0f;
                        RigidWheelBaseM = GetRigidWheelBaseM();
                        WagonNumWheels = GetWagonNumWheels();
                        WagonType = GetWagonType();
                        EngineType = GetEngineType();
                        DriverWheelRadiusM = GetDriverWheelRadiusM();
                        LocoNumDrvWheels = GetLocoNumWheels();

                        // Determine whether the track is wet due to rain or snow.

                        int FrictionWeather = (int)Program.Simulator.Weather;
                        
                        if (FrictionWeather == 1 | FrictionWeather == 2)
                        {
                            CoefficientFriction = 0.25f;  // Weather snowing or raining
                        }
                        else
                        {
                            CoefficientFriction = 0.5f;  // Clear
                        }

                        float Axles = WheelAxles.Count;
                        float Bogies = Parts.Count - 1;
                        float BogieSize = Axles / Bogies;

                        RigidWheelBaseM = 1.6764f;       // Set a default in case no option is found - assume a standard 4 wheel (2 axle) bogie - wheel base - 5' 6" (1.6764m)

                   //     Trace.TraceInformation("WagonWheels {0} DriveWheels {1} WheelRadius {2} Axles {3} Bogies {4}", WagonNumWheels, LocoNumDrvWheels, DriverWheelRadiusM, Axles, Bogies);

                        // Calculate the number of axles in a car

                        if (WagonType != "Engine")   // if car is not a locomotive then determine wheelbase
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
                            }
                            else if (Bogies == 2)
                            {
                                if (Axles == 2)
                                {
                                    if (WagonType == "Passenger")
                                    {

                                        RigidWheelBaseM = 2.4384f;       // Assume a standard 4 wheel passenger bogie (2 axle) wagon - wheel base - 8' (2.4384m)
                                    }
                                    else
                                    {
                                        RigidWheelBaseM = 1.6764f;       // Assume a standard 4 wheel freight bogie (2 axle) wagon - wheel base - 5' 6" (1.6764m)
                                    }
                                }
                                else if (Axles == 3)
                                {
                                    RigidWheelBaseM = 3.6576f;       // Assume a standard 6 wheel bogie (3 axle) wagon - wheel base - 12' 2" (3.6576m)
                                }
                            }

                        }
                        if (WagonType == "Engine")   // if car is a locomotive and either a diesel or electric then determine wheelbase
                        {
                            if (EngineType != "Steam")  // Assume that it is a diesel or electric locomotive
                            {
                                if (Axles == 2)
                                {
                                    RigidWheelBaseM = 1.6764f;       // Set a default in case no option is found - assume a standard 4 wheel (2 axle) bogie - wheel base - 5' 6" (1.6764m)
                                }
                                else if (Axles == 3)
                                {
                                    RigidWheelBaseM = 3.5052f;       // Assume a standard 6 wheel bogie (3 axle) locomotive - wheel base - 11' 6" (3.5052m)
                                }
                            }
                            else // assume steam locomotive
                            {

                                if (LocoNumDrvWheels >= Axles) // Test to see if ENG file value is too big (typically doubled)
                                {
                                    LocoNumDrvWheels = LocoNumDrvWheels / 2.0f;  // Appears this might be the number of wheels rather then the axles.
                                }

                                //    Approximation for calculating rigid wheelbase for steam locomotives
                                // Wheelbase = 1.25 x (Loco Drive Axles - 1.0) x Drive Wheel diameter

                                RigidWheelBaseM = 1.25f * (LocoNumDrvWheels - 1.0f) * (DriverWheelRadiusM * 2.0f);
                              //  Trace.TraceInformation("Drv {0} Radius {1}", LocoNumDrvWheels, DriverWheelRadiusM);

                            }

                        }


                    }
                  
                    // Curve Resistance = (Vehicle mass x Coeff Friction) * (Track Gauge + Vehicle Fixed Wheelbase) / (2 * curve radius)
                    // Vehicle Fixed Wheel base is the distance between the wheels, ie bogie or fixed wheels

                    CurveForceN = MassKG * CoefficientFriction * (TrackGaugeM + RigidWheelBaseM) / (2.0f * CurrentCurveRadius);
                    float CurveResistanceSpeedFactor = Math.Abs((MaxCurveEqualLoadSpeedMps - Math.Abs(SpeedMpS)) / MaxCurveEqualLoadSpeedMps) * StartCurveResistanceFactor;
                    CurveForceN *= CurveResistanceSpeedFactor * CurveResistanceZeroSpeedFactor;
                    CurveForceN *= GravitationalAccelerationMpS2; // to convert to Newtons
              
                }
                else
                {
                    CurveForceN = 0f;
                }

            }
        }

         #endregion

        /// <summary>
        /// Signals an event from an external source (player, multi-player controller, etc.) for this car.
        /// </summary>
        /// <param name="evt"></param>
        public virtual void SignalEvent(Event evt) { }
        public virtual void SignalEvent(PowerSupplyEvent evt) { }
        public virtual void SignalEvent(PowerSupplyEvent evt, int id) { }

        public virtual string GetStatus() { return null; }
        public virtual string GetDebugStatus() { return null; }
        public virtual string GetTrainBrakeStatus(PressureUnit unit) { return null; }
        public virtual string GetEngineBrakeStatus(PressureUnit unit) { return null; }
        public virtual string GetDynamicBrakeStatus() { return null; }
        public virtual bool GetSanderOn() { return false; }
        bool WheelHasBeenSet = false; //indicating that the car shape has been loaded, thus no need to reset the wheels

        public TrainCar()
        {
        }

        public TrainCar(Simulator simulator, string wagFile)
        {
            Simulator = simulator;
            WagFilePath = wagFile;
            RealWagFilePath = wagFile;
            Stiffness = (float)Program.Random.NextDouble() * 2f + 3f;//stiffness range from 4-8 (i.e. vibrating frequency)
            MaxVibSpeed = 15f + Stiffness * 2;//about 50km/h
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
       // Method to get Track Gauge from MSTSWagon
        public virtual float GetTrackGaugeM()
        {
            
            return TrackGaugeM;
        }

        // Method to get Centre of Gravity from MSTSWagon
        public virtual float GetCentreofGravityM()
        {

            return CentreOfGravityM;
        }

        // Method to get Unbalanced SuperElevation from MSTSWagon
        public virtual float GetUnbalancedSuperElevationM()
        {

            return UnbalancedSuperElevationM;
        }
        
        // Method to get rigid wheelbase from MSTSWagon file
        public virtual float GetRigidWheelBaseM()
        {

            return RigidWheelBaseM;
        }

        // Method to get rigid wheelbase from MSTSWagon file
        public virtual float GetLocoNumWheels()
        {

            return LocoNumDrvWheels;
        }
        // Method to get Driver Wheel radius from MSTSWagon file
        public virtual float GetDriverWheelRadiusM()
        {

            return DriverWheelRadiusM;
        }

        // Method to get Wagon type from MSTSWagon file
        public virtual string GetWagonType()
        {

            return WagonType;
        }
        
        // Method to get Engine type from MSTSLocomotive file
        public virtual string GetEngineType()
        {

            return EngineType;
        }

        // Method to get wagon num wheels from MSTSWagon file
        public virtual float GetWagonNumWheels()
        {

            return WagonNumWheels;
        }

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
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;
            //some old stocks have only two wheels, but defined to have four, two share the same offset, thus all computing of rotations will have problem
            //will check, if so, make the offset different a bit. 
            foreach (var axles in WheelAxles) if (offset.AlmostEqual(axles.OffsetM, 0.05f)) { offset = axles.OffsetM + 0.1f; break; }
            WheelAxles.Add(new WheelAxle(offset, bogie, parentMatrix));
        }

        public void AddBogie(float offset, int matrix, int id)
        {
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;
            //make sure two bogies are not defined too close
            foreach (var p in Parts) if (p.bogie && offset.AlmostEqual(p.OffsetM, 0.05f)) { offset = p.OffsetM + 0.1f; break; }
            while (Parts.Count <= id)
                Parts.Add(new TrainCarPart(0, 0));
            Parts[id].OffsetM = offset;
            Parts[id].iMatrix = matrix;
            Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
        }

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
            // No axles but we have bogies.
            if (WheelAxles.Count == 0 && Parts.Count > 1)
            {
                // Fake the axles by pretending each has 1 axle.
                foreach (var part in Parts.Skip(1))
                    WheelAxles.Add(new WheelAxle(part.OffsetM, part.iMatrix, 0));
                Trace.TraceInformation("Wheel axle data faked based on {1} bogies for {0}", WagFilePath, Parts.Count - 1);
            }
            // No parts means no bogies (always?), so make sure we've got Parts[0] for the car itself.
            if (Parts.Count == 0)
                Parts.Add(new TrainCarPart(0, 0));
            bool articFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articRear = !WheelAxles.Any(a => a.OffsetM > 0);
            // Validate the axles' assigned bogies and count up the axles on each bogie.
            if (WheelAxles.Count > 0)
            {
                foreach (var w in WheelAxles)
                {
                    if (!articFront && !articRear && Parts[0].SumWgt < 1.5)
                        if (w.BogieIndex >= Parts.Count - 1)
                            w.BogieIndex = 0;
                    if (w.BogieIndex >= Parts.Count)
                        w.BogieIndex = 0;
                    if (w.BogieMatrix > 0 && w.BogieIndex > 0)
                    {
                        for (var i = 0; i <= Parts.Count; i++)
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
            // Count up the number of bogies (parts) with at least 2 axles.
            for (var i = 1; i < Parts.Count; i++)
                if (Parts[i].SumWgt > 1.5)
                    Parts[0].SumWgt++;
            
            // Using WheelAxles.Count test to control WheelAxlesLoaded flag.
            if (WheelAxles.Count > 2)
                WheelAxlesLoaded = true;
            
                                                                                 
#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
            // Decided to control what is sent to SetUpWheelsArticulation()by using
            // WheelAxlesLoaded as a flag.  This way, wagons that have to be processed are included
            // and the rest left out.
            bool articulatedFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articulatedRear = !WheelAxles.Any(a => a.OffsetM > 0);
            var carIndex = Train.Cars.IndexOf(this);
            //Certain locomotives are testing as articulated wagons for some reason.
            this.WagonType = GetWagonType();
            if (this.WagonType != "Engine" && this.WagonType != "Carriage")
                if (this.WagonType == "Freight" && this.WheelAxles.Count >= 2)
                    if (articulatedFront || articulatedRear)
                    {
                        WheelAxlesLoaded = true;
                        this.SetUpWheelsArticulation(carIndex);
                    }
        } // end SetUpWheels()

        void SetUpWheelsArticulation(int carIndex)
        {
            // If there are no forward wheels, this car is articulated (joined
            // to the car in front) at the front. Likewise for the rear.
            bool articulatedFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articulatedRear = !WheelAxles.Any(a => a.OffsetM > 0);
            // If the car is articulated, steal some wheels from nearby cars.
            
                if (articulatedFront || articulatedRear)
                {
                    if (articulatedFront && carIndex > 0)
                    {
                        var otherCar = Train.Cars[carIndex - 1];
                        var otherPart = otherCar.Parts.OrderBy(p => p.OffsetM).FirstOrDefault();
                        if (otherPart == null)
                            WheelAxles.Add(new WheelAxle(-LengthM / 2, 0, 0) { Part = Parts[0] });
                        else
                        {
                            var offset = otherCar.LengthM / 2 - LengthM / 2;
                            var otherPartIndex = otherCar.Parts.IndexOf(otherPart);
                            var otherAxles = otherCar.WheelAxles.Where(a => a.BogieIndex == otherPartIndex);
                            var part = new TrainCarPart(otherPart.OffsetM + offset, 0) { SumWgt = otherPart.SumWgt };
                            WheelAxles.AddRange(otherAxles.Select(a => new WheelAxle(a.OffsetM + offset, Parts.Count, 0) { Part = part }));
                            Parts.Add(part);
                        }
                    }
                    if (articulatedRear && carIndex < Train.Cars.Count - 1 && carIndex != 2)
                    {
                        var otherCar = Train.Cars[carIndex + 1];
                        var otherPart = otherCar.Parts.OrderBy(p => -p.OffsetM).FirstOrDefault();
                        if (otherPart == null)
                            WheelAxles.Add(new WheelAxle(LengthM / 2, 0, 0) { Part = Parts[0] });
                        else
                        {
                            var offset = otherCar.LengthM / 2 + LengthM / 2;
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
            Console.WriteLine("  length {0,10:F4}", LengthM);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
        } // end SetUpWheelsArticulation()

        public void ComputePosition(Traveller traveler, bool backToFront, float speed)
        {
            for (int j = 0; j < Parts.Count; j++)
                Parts[j].InitLineFit();
            int tileX = traveler.TileX;
            int tileZ = traveler.TileZ;
            if (Flipped == backToFront)
            {
                float o = -LengthM / 2;
                for (int k = 0; k < WheelAxles.Count; k++)
                {
                    float d = WheelAxles[k].OffsetM - o;
                    o = WheelAxles[k].OffsetM;
                    traveler.Move(d);
                    float x = traveler.X + 2048 * (traveler.TileX - tileX);
                    float y = traveler.Y;
                    float z = traveler.Z + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0, traveler);
                }
                o = LengthM / 2 - o;
                traveler.Move(o);
            }
            else
            {
                float o = LengthM / 2;
                for (int k = WheelAxles.Count - 1; k>=0 ; k--)
                {
                    float d = o - WheelAxles[k].OffsetM;
                    o = WheelAxles[k].OffsetM;
                    traveler.Move(d);
                    float x = traveler.X + 2048 * (traveler.TileX - tileX);
                    float y = traveler.Y;
                    float z = traveler.Z + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0, traveler);
                }
                o = LengthM / 2 + o;
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
            RealXNAMatrix = WorldPosition.XNAMatrix;
          
                SuperElevation(speed, Program.Simulator.UseSuperElevation, traveler);
         
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
                    p.AddWheelSetLocation(1, p.OffsetM, p0.A[0] + p.OffsetM * p0.B[0], p0.A[1] + p.OffsetM * p0.B[1], p0.A[2] + p.OffsetM * p0.B[2], 0, null);
                    p.FindCenterLine();
                }
                Vector3 fwd1 = new Vector3(p.B[0], p.B[1], -p.B[2]);
                if (fwd1.X == 0 && fwd1.Y == 0 && fwd1.Z == 0)
                {
                    p.Cos = 1;
                }
                else
                {
                    fwd1.Normalize();
                    p.Cos = Vector3.Dot(fwd, fwd1);
                }

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

        public float sx=0.0f, sy=0.0f, sz=0.0f, prevElev=-100f, prevTilted;//time series from 0-3.14
        public float currentStiffness = 1.0f;
        public double lastTime = -1.0;
        public float totalRotationZ;
        public float totalRotationX;
        public float prevY2 = -1000f;
        public float prevY = -1000f;

        public float CurrentCurveRadius;

        public void SuperElevation(float speed, int superEV, Traveller traveler)
        {
            CurrentCurveRadius = traveler.GetCurrentCurveRadius();
                      
           // ignore the rest of superelevation if option is not selected under menu options TAB
          if (Program.Simulator.UseSuperElevation > 0 || Program.Simulator.CarVibrating > 0 || this.Train.tilted)
          { 

            if (prevElev < -30f) { prevElev += 40f; return; }//avoid the first two updates as they are not valid
            speed = (float) Math.Abs(speed);//will make computation easier later, as we only deal with abs value
            if (speed > 40) speed = 40; //vib will not increase after 120km
            float timeInterval = 0f;
            if (lastTime <= 0.0)
            {
                sx = (float)Program.Random.NextDouble()*3.13f;
                sy = (float)Program.Random.NextDouble()*3.13f;
                sz = (float)Program.Random.NextDouble()*3.13f;
                currentStiffness = Stiffness;
                prevY = prevY2 = WorldPosition.XNAMatrix.Translation.Y;//remember Y values for acceleration compute
            }
            else
            {
                timeInterval = (float)(Program.Simulator.GameTime - lastTime);
                //sin wave of frequency 3-5 per second
                sx += timeInterval * currentStiffness;
                if (sx > 6.28) { sx = sx - 6.28f; currentStiffness = Stiffness + (float)(0.5 - Program.Random.NextDouble()) * speed / 20; }
                sy += timeInterval * currentStiffness;
                if (sy > 6.28) { sy = sy - 6.28f; }
                sz += timeInterval * currentStiffness;
                if (sz > 6.28) { sz = sz - 6.28f; }
            }
            lastTime = Program.Simulator.GameTime;
            //System.Console.WriteLine("" + x + " " + y + " " + z);

            //get superelevation
            float z = 0.0f;
            if (superEV > 0)
            {
                z = traveler.SuperElevationValue(speed, timeInterval, true);
                if (this.Flipped) z *= -1f;

                if (prevElev < -10f) prevElev = z;//initial, will jump to the desired value
                //else if (Math.Abs(z) > 0.0001 && Math.Sign(prevElev) >0.0001 && Math.Sign(z) != Math.Sign(prevElev)) { z = prevElev; }//changing signs indicating something wrong
                else
                {
                    z = prevElev + (z - prevElev) * timeInterval;//smooth rotation
                    prevElev = z;
                }
            }

            //compute max shaking (rotation value), will peak at MaxVibSpeed, then decrease with half the value
            var max = 1f;
            if (speed <= MaxVibSpeed) max = speed / MaxVibSpeed;
            else max = 1 - (speed - MaxVibSpeed) / MaxVibSpeed * 2;
            max *= Program.Simulator.CarVibrating/500f;//user may want more vibration (by Ctrl-V)
            var tz = traveler.FindTiltedZ(speed);//rotation if tilted, an indication of centrifugal force

            

            max = ComputeMaxXZ(timeInterval, max, tz);//add a damping, also based on acceleration
            //small vibration (rotation to add on x,y,z axis)
            var sx1 = (float)Math.Sin(sx) * max; var sz1 = (float)Math.Sin(sz) * max;

            //check for tilted train, add more to the body
            if (this.Train != null && this.Train.tilted == true)
            {
                tz = prevTilted + (tz - prevTilted)*timeInterval;//smooth rotation
                prevTilted = tz;
                if (this.Flipped) tz *= -1f;
                sz1 += tz;
            }

            totalRotationZ = -(sz1 + z) / 4;
            totalRotationX = sx1 / 2;
            //this matrix is for the body, bogie will do an inverse to keep on track
            SuperElevationMatrix = Matrix.CreateRotationX(sx1) /* * Matrix.CreateRotationY(sy1)*/ * Matrix.CreateRotationZ(sz1);
            //SuperElevationMatrix.Translation += new Vector3(sx1, sy1, sz1);
            WorldPosition.XNAMatrix = Matrix.CreateRotationZ(z) * SuperElevationMatrix * WorldPosition.XNAMatrix;
            try
            {
                SuperElevationMatrix = Matrix.Invert(SuperElevationMatrix);
            }
            catch { SuperElevationMatrix = Matrix.Identity; }
          }
        }

        public float accumedAcceTime = 4f;
        //compute the max shaking around x and z based on accelation on Y and centrifugal values
        public float ComputeMaxXZ(float interval, float max, float tz)
        {
            if (interval < 0.001f) return max;

            float maxV = 0f;
            var newY = WorldPosition.XNAMatrix.Translation.Y;
            //compute acceleration on Y
            var acce = (newY - 2 * prevY + prevY2) / (interval * interval);
            prevY2 = prevY; prevY = newY;
            maxV = Math.Abs(acce);

            //if has big acceleration, will resume vibration
            if (maxV > 1 || Math.Abs(tz) > 0.02 || Math.Abs(AccelerationMpSS)>0.5) { accumedAcceTime = 1f; return max; }
            accumedAcceTime += interval/5;
            return max / accumedAcceTime;//otherwise slowly decrease the value
        }

        public float[] Velocity = new float[] { 0, 0, 0 };
        WorldLocation SoundLocation;
        public void UpdateSoundPosition()
        {
            if (SoundSourceIDs.Count == 0 || Program.Simulator.Confirmer.Viewer == null || Program.Simulator.Confirmer.Viewer.Camera == null)
                return;
            
            if (Train != null)
            {
                Vector3 directionVector = Vector3.Multiply(GetXNAMatrix().Forward, SpeedMpS);
                Velocity = new float[] { directionVector.X, directionVector.Y, -directionVector.Z };
            }
            else
                Velocity = new float[] { 0, 0, 0 };

            SoundLocation = new WorldLocation(WorldPosition.WorldLocation);
            SoundLocation.NormalizeTo(Camera.SoundBaseTile.X, Camera.SoundBaseTile.Y);
            float[] position = new float[] {
                SoundLocation.Location.X,
                SoundLocation.Location.Y,
                SoundLocation.Location.Z};

            var soundSourceIDs = SoundSourceIDs.ToArray();
            foreach (var soundSourceID in soundSourceIDs)
            {
                OpenAL.alSourcefv(soundSourceID, OpenAL.AL_POSITION, position);
                OpenAL.alSourcefv(soundSourceID, OpenAL.AL_VELOCITY, Velocity);
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
        public bool bogie;
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
        public void AddWheelSetLocation(float w, float o, float x, float y, float z, float t, Traveller traveler)
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
        public LightViewer lightDrawer;

        protected Viewer Viewer;

        public TrainCarViewer( Viewer viewer, TrainCar car)
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

        /// <summary>
        /// Unload and release the car - its not longer being displayed
        /// </summary>
        public abstract void Unload();

        [CallOnThread("Loader")]
        internal virtual void LoadForPlayer() { }

        [CallOnThread("Loader")]
        internal abstract void Mark();
    }
}
