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
using Orts.Formats.Msts;
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
        public float CarWidthM = 2.5f;
        public float CarLengthM = 40;       // derived classes must overwrite these defaults
        public float CarHeightM = 4;        // derived classes must overwrite these defaults
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
//        public bool IsPlayerTrain { get { return Train.TrainType == ORTS.Train.TRAINTYPE.PLAYER ? true : false; } set { } }
        public bool IsPlayerTrain { get { return Train.IsPlayerDriven; } set { } }
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

        // TrainCar.Update() must set these variables
        public float MotiveForceN;   // ie motor power in Newtons  - signed relative to direction of car - 
        public SmoothedData MotiveForceSmoothedN = new SmoothedData(0.5f);
        public float PrevMotiveForceN;
        public float GravityForceN;  // Newtons  - signed relative to direction of car - 
        public float CurveForceN;   // Resistive force due to curve, in Newtons
        public float TunnelForceN;  // Resistive force due to tunnel, in Newtons
        public float FrictionForceN; // in Newtons ( kg.m/s^2 ) unsigned, includes effects of curvature
        public float BrakeForceN;    // brake force in Newtons
        public float TotalForceN; // sum of all the forces active on car relative train direction

        public float CurrentElevationPercent;

        public bool CurveResistanceSpeedDependent;
        public bool CurveSpeedDependent;
        public bool TunnelResistanceDependent;

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
        public List<PassengerViewPoint> CabViewpoints; //three dimensional cab view point
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
        float TrainCrossSectionAreaM2; // Cross sectional area of the train
        float DoubleTunnelCrossSectAreaM2;
        float SingleTunnelCrossSectAreaM2;
        float DoubleTunnelPerimeterM;
        float SingleTunnelPerimeterAreaM;
        float TunnelCrossSectionAreaM2 = 0.0f;
        float TunnelPerimeterM = 0.0f;
        
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
            CurveResistanceSpeedDependent = Simulator.Settings.CurveResistanceSpeedDependent;
            CurveSpeedDependent = Simulator.Settings.CurveSpeedDependent;
            TunnelResistanceDependent = Simulator.Settings.TunnelResistanceDependent;
            
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
         }

        // called when it's time to update the MotiveForce and FrictionForce
        public virtual void Update(float elapsedClockSeconds)
        {
            // gravity force, M32 is up component of forward vector
            GravityForceN = MassKG * GravitationalAccelerationMpS2 * WorldPosition.XNAMatrix.M32;
            CurrentElevationPercent = 100f * WorldPosition.XNAMatrix.M32;


            //TODO: next if block has been inserted to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
            // To achieve the same result with other means, without flipping trainset physics, the block should be deleted
            //      
            if (IsDriveable && Train != null & Train.IsPlayerDriven && (this as MSTSLocomotive).UsingRearCab)
            {
                GravityForceN = -GravityForceN;
                CurrentElevationPercent = -CurrentElevationPercent;
            }
 
            UpdateCurveSpeedLimit(); // call this first as it will provide inputs for the curve force.
            UpdateCurveForce();
            UpdateTunnelForce();
            
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


        #region Calculate resistance due to tunnels
        /// <summary>
        /// Tunnel force (resistance calculations based upon formula presented in papaer titled "Reasonable compensation coefficient of maximum gradient in long railway tunnels"
        /// </summary>
        public virtual void UpdateTunnelForce()
        {
          if (Train.IsPlayerDriven)   // Only calculate tunnel resistance when it is the player train.
          {
              if (TunnelResistanceDependent)
              {
                  if (CarTunnelData.FrontPositionBeyondStartOfTunnel.HasValue)
                  {

                      float? TunnelStart;
                      float? TunnelAhead;
                      float? TunnelBehind;

                      TunnelStart = CarTunnelData.FrontPositionBeyondStartOfTunnel;      // position of front of wagon wrt start of tunnel
                      TunnelAhead = CarTunnelData.LengthMOfTunnelAheadFront;            // Length of tunnel remaining ahead of front of wagon (negative if front of wagon out of tunnel)
                      TunnelBehind = CarTunnelData.LengthMOfTunnelBehindRear;           // Length of tunnel behind rear of wagon (negative if rear of wagon has not yet entered tunnel)

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
                       
                      // 
                      // Calculate first tunnel factor

                      float TunnelAComponent = (0.00003318f * DensityAirKgpM3 * TunnelCrossSectionAreaM2) / ((1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)) * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)));
                      float TunnelBComponent = 174.419f * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)) * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2));
                      float TunnelCComponent = (2.907f * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2)) * (1 - (TrainCrossSectionAreaM2 / TunnelCrossSectionAreaM2))) / (4.0f * (TunnelCrossSectionAreaM2 / TunnelPerimeterM));

                      float TempTunnel1 = (float)Math.Sqrt(TunnelBComponent + (TunnelCComponent * (TunnelLengthM - TrainLengthTunnelM) / TrainLengthTunnelM));
                      float TempTunnel2 = (1.0f - (1.0f / (1.0f + TempTunnel1))) * (1.0f - (1.0f / (1.0f + TempTunnel1)));

                      float UnitAerodynamicDrag = ((TunnelAComponent * TrainLengthTunnelM) / Kg.ToTonne(TrainMassTunnelKg)) * TempTunnel2;

                      TunnelForceN = UnitAerodynamicDrag * Kg.ToTonne(MassKG) * Math.Abs(SpeedMpS) * Math.Abs(SpeedMpS);
                  }
                  else
                  {
                      TunnelForceN = 0.0f; // Reset tunnel force to zero when train is no longer in the tunnel
                  }
              }
          }
        }



        #endregion


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
                           
                          if (Train.IsPlayerDriven)   
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
                            
                            if (Train.IsPlayerDriven)
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
        public virtual string GetDebugStatus()
        {
            return String.Format("Car {0}\t{2} {1}\t\t{3}\t{4:F0}%\t{5}\t\t{6}\t{7}",
                UiD,
                Flipped ? Viewer.Catalog.GetString("(flipped)") : "",
                FormatStrings.Catalog.GetParticularString("Reverser", GetStringAttribute.GetPrettyName(Direction)),
                AcceptMUSignals ? Viewer.Catalog.GetString("MU'd") : Viewer.Catalog.GetString("Single"),
                ThrottlePercent,
                String.Format("{0}{1}", FormatStrings.FormatSpeedDisplay(SpeedMpS, IsMetric), WheelSlip ? "!!!" : ""),
                FormatStrings.FormatPower(MotiveForceN * SpeedMpS, IsMetric, false, false),
                String.Format("{0}{1}", FormatStrings.FormatForce(MotiveForceN, IsMetric), CouplerOverloaded ? "???" : ""));
        }
        public virtual string GetTrainBrakeStatus() { return null; }
        public virtual string GetEngineBrakeStatus() { return null; }
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

        public bool HasFrontCab { get
            {
                var loco = this as MSTSLocomotive;
                var i = (int)CabViewType.Front;
                if (loco == null || loco.CabViewList.Count <= i || loco.CabViewList[i].CabViewType != CabViewType.Front) return false;
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

        public void AddWheelSet(float offset, int bogieID, int parentMatrix, string wheels, int numWheels1, int numWheels2)
        {
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;
            //some old stocks have only two wheels, but defined to have four, two share the same offset, thus all computing of rotations will have problem
            //will check, if so, make the offset different a bit. 
            foreach (var axles in WheelAxles) if (offset.AlmostEqual(axles.OffsetM, 0.05f)) { offset = axles.OffsetM + 0.7f; break; }
            if (wheels.Length == 8)
            {
                if (wheels == "WHEELS11" || wheels == "WHEELS12" || wheels == "WHEELS13" || wheels == "SPOKES11" || wheels == "SPOKES12" || wheels == "SPOKES13")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
                else if (wheels == "WHEELS21" || wheels == "WHEELS22" || wheels == "WHEELS23" || wheels == "SPOKES21" || wheels == "SPOKES22" || wheels == "SPOKES23")
                {
                    // The process would assign 2 to the id because WHEELS21 or WHEELS22 would contain the number 2.
                    // If the shape file contained only one set of WHEELS that was labeled as WHEELS21 && WHEELS22 then BogieIndex would be 2, not 1.
                    if (numWheels2 <= 2 && numWheels1 == 0)
                    {
                        bogieID -= 1;
                        WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
                    }
                    else
                        WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
                }
            }
            else
                // Wheels can be Wheels1, Wheels2, Wheels3. 
                WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
        } // end AddWheelSet()

        public void AddBogie(float offset, int matrix, int id, string bogie, int numBogie1, int numBogie2, int numBogie)
        {
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;
            foreach (var p in Parts) if (p.bogie && offset.AlmostEqual(p.OffsetM, 0.05f)) { offset = p.OffsetM + 0.1f; break; }
            if (bogie == "BOGIE1")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
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
                        Parts.Add(new TrainCarPart(0, 0));
                    Parts[id].OffsetM = offset;
                    Parts[id].iMatrix = matrix;
                    Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
                }
                else
                {
                    while (Parts.Count <= id)
                        Parts.Add(new TrainCarPart(0, 0));
                    Parts[id].OffsetM = offset;
                    Parts[id].iMatrix = matrix;
                    Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
                }
            }
            else if (bogie == "BOGIE")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
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
                Parts.Add(new TrainCarPart(0, 0));
            // No axles but we have bogies.
            if (WheelAxles.Count == 0 && Parts.Count > 1)
            {
                // Fake the axles by pretending each has 1 axle.
                foreach (var part in Parts.Skip(1))
                    WheelAxles.Add(new WheelAxle(part.OffsetM, part.iMatrix, 0));
                Trace.TraceInformation("Wheel axle data faked based on {1} bogies for {0}", WagFilePath, Parts.Count - 1);
            }
            bool articFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articRear = !WheelAxles.Any(a => a.OffsetM > 0);
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

            //fix bogies with only one wheel set:
            // This process is to fix the bogies that did not pivot under the cab of steam locomotives as well as other locomotives that have this symptom.
            // The cause involved the bogie and axle being close by 0.05f or less on the ZAxis.
            // The ComputePosition() process was unable to work with this.
            // The fix involves first testing for how close they are then moving the bogie offset up.
            // The final fix involves adding an additional axle.  Without this, both bogie and axle would never track properly?
            // Note: Steam locomotive modelers are aware of this issue and are now making sure there is ample spacing between axle and bogie.
            for (var i = 1; i < Parts.Count; i++)
            {
                if (Parts[i].bogie == true && Parts[i].SumWgt < 1.5)
                {
                    foreach (var w in WheelAxles)
                    {
                        if (w.BogieMatrix == Parts[i].iMatrix)
                        {
                            if (w.OffsetM.AlmostEqual(Parts[i].OffsetM, 0.6f))
                            {
                                var w1 = new WheelAxle(w.OffsetM - 0.5f, w.BogieIndex, i);
                                w1.Part = Parts[w1.BogieIndex]; //create virtual wheel
                                w1.Part.SumWgt++;
                                WheelAxles.Add(w1);
                                w.OffsetM += 0.5f; //move the original bogie forward, so we have two bogies to make the future calculation happy
                                Trace.TraceInformation("A virtual wheel axle was added for bogie {1} of {0}", WagFilePath, i);
                                break;
                            }
                        }
                    }
                }
            }

            // Count up the number of bogies (parts) with at least 2 axles.
            for (var i = 1; i < Parts.Count; i++)
                if (Parts[i].SumWgt > 1.5)
                    Parts[0].SumWgt++;

            // This check is for the single axle/bogie issue.
            // Check SumWgt using Parts[0].SumWgt.
            // Certain locomotives do not test well when using Part.SumWgt versus Parts[0].SumWgt.
            // Make sure test using Parts[0] is performed after the above for loop.
            if (!articFront && !articRear && (Parts[0].SumWgt < 1.5))
            {
                foreach (var w in WheelAxles)
                {
                    if (w.BogieIndex >= Parts.Count - 1)
                    {
                        w.BogieIndex = 0;
                        w.Part = Parts[w.BogieIndex];

                    }
                }
            }
            // Using WheelAxles.Count test to control WheelAxlesLoaded flag.
            if (WheelAxles.Count > 2)
            {
                WheelAxles.Sort(WheelAxles[0]);
                WheelAxlesLoaded = true;
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
            // Decided to control what is sent to SetUpWheelsArticulation()by using
            // WheelAxlesLoaded as a flag.  This way, wagons that have to be processed are included
            // and the rest left out.
            bool articulatedFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articulatedRear = !WheelAxles.Any(a => a.OffsetM > 0);
            var carIndex = Train.Cars.IndexOf(this);
            //Certain locomotives are testing as articulated wagons for some reason.
            this.WagonType = GetWagonType();
            if (this.WagonType != "Engine")
                if (this.WheelAxles.Count >= 2)
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
            // Original process originally used caused too many issues.
            // The original process did include the below process of just using WheelAxles.Add
            //  if the initial test did not work.  Since the below process is working without issues the
            //  original process was stripped down to what is below
            if (articulatedFront || articulatedRear)
            {
                if (articulatedFront && WheelAxles.Count <= 3)
                    WheelAxles.Add(new WheelAxle(-CarLengthM / 2, 0, 0) { Part = Parts[0] });

                if (articulatedRear && WheelAxles.Count <= 3)
                    WheelAxles.Add(new WheelAxle(CarLengthM / 2, 0, 0) { Part = Parts[0] });

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
                float o = -CarLengthM / 2;
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
                o = CarLengthM / 2 - o;
                traveler.Move(o);
            }
            else
            {
                float o = CarLengthM / 2;
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
                o = CarLengthM / 2 + o;
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
        public int TrackSoundType = 0;
        public WorldLocation TrackSoundLocation = WorldLocation.None;
        public float TrackSoundDistSquared = 0;

        public void UpdateSoundPosition()
        {
            if (SoundSourceIDs.Count == 0 || Program.Simulator.Confirmer.Viewer == null || Program.Simulator.Confirmer.Viewer.Camera == null)
                return;
            
            if (Train != null)
            {
                var realSpeedMpS = SpeedMpS;
                //TODO Following if block is needed due to physics flipping when using rear cab
                // If such physics flipping is removed next block has to be removed.
                if (this is MSTSLocomotive)
                {
                    var loco = this as MSTSLocomotive;
                    if (loco.UsingRearCab) realSpeedMpS = -realSpeedMpS;
                }
                Vector3 directionVector = Vector3.Multiply(GetXNAMatrix().Forward, realSpeedMpS);
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

            // make a copy of SoundSourceIDs, but check that it didn't change during the copy; if it changed, try again up to 5 times.
            var sSIDsFinalCount = -1;
            var sSIDsInitCount = -2;
            int[] soundSourceIDs = {0} ;
            int trialCount = 0;
            while (sSIDsInitCount != sSIDsFinalCount && trialCount < 5)
            {
                sSIDsInitCount = SoundSourceIDs.Count;
                soundSourceIDs = SoundSourceIDs.ToArray();
                sSIDsFinalCount = SoundSourceIDs.Count;
                trialCount++;
            }
            if (trialCount >= 5)
                return;
            foreach (var soundSourceID in soundSourceIDs)
            {
                Simulator.updaterWorking = true;
                if (OpenAL.alIsSource(soundSourceID))
                {
                    OpenAL.alSourcefv(soundSourceID, OpenAL.AL_POSITION, position);
                    OpenAL.alSourcefv(soundSourceID, OpenAL.AL_VELOCITY, Velocity);
                }
                Simulator.updaterWorking = false;
            }
        }

        public virtual void SwitchToPlayerControl()
        {
            return;
        }

        public virtual void SwitchToAutopilotControl()
        {
            return;
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

        public abstract void InitializeUserInputCommands();

        /// <summary>
        /// Called at the full frame rate
        /// elapsedTime is time since last frame
        /// Executes in the UpdaterThread
        /// </summary>
        public abstract void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime);

        [CallOnThread("Loader")]
        public virtual void Unload() { }

        [CallOnThread("Loader")]
        internal virtual void LoadForPlayer() { }

        [CallOnThread("Loader")]
        internal abstract void Mark();
    }
}
