// COPYRIGHT 2013 by the Open Rails project.
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

/* AI
 * 
 * Contains code to initialize and control AI trains.
 * 
 */

// #define DEBUG_REPORTS
// #define DEBUG_CHECKTRAIN
// #define DEBUG_DEADLOCK
// #define DEBUG_EXTRAINFO
// #define DEBUG_TRACEINFO
// DEBUG flag for debug prints

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.MultiPlayer;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS;
using Orts.Simulation.Signalling;
using ORTS.Common;
using Event = Orts.Common.Event;

namespace Orts.Simulation.AIs
{
    public class AITrain : Train
    {
        public int UiD;
        public AIPath Path;

        public float MaxDecelMpSSP = 1.0f;                  // maximum decelleration
        public float MaxAccelMpSSP = 1.0f;                  // maximum accelleration
        public float MaxDecelMpSSF = 0.8f;                  // maximum decelleration
        public float MaxAccelMpSSF = 0.5f;                  // maximum accelleration
        public float MaxDecelMpSS = 0.5f;                   // maximum decelleration
        public float MaxAccelMpSS = 1.0f;                   // maximum accelleration
        public float Efficiency = 1.0f;                     // train efficiency
        public float LastSpeedMpS;                          // previous speed
        public int Alpha10 = 10;                            // 10*alpha

        public bool PreUpdate;                              // pre update state
        public AIActionItem nextActionInfo;                 // no next action
        public AIActionItem nextGenAction;                  // Can't remove GenAction if already active but we need to manage the normal Action, so
        public float NextStopDistanceM;                     // distance to next stop node
        public int? StartTime;                              // starting time
        public bool PowerState = true;                      // actual power state : true if power in on
        public float MaxVelocityA = 30.0f;                  // max velocity as set in .con file
        public Service_Definition ServiceDefinition = null; // train's service definition in .act file
        public bool UncondAttach = false;                   // if false it states that train will unconditionally attach to a train on its path

        public float DoorOpenTimer = -1f;
        public float DoorCloseTimer = -1f;
        public AILevelCrossingHornPattern LevelCrossingHornPattern { get; set; }
        public bool ApproachTriggerSet = false;         // station approach trigger for AI trains has been set

        public float PathLength;


        public enum AI_MOVEMENT_STATE
        {
            AI_STATIC,
            INIT,
            STOPPED,
            STATION_STOP,
            BRAKING,
            ACCELERATING,
            FOLLOWING,
            RUNNING,
            APPROACHING_END_OF_PATH,
            STOPPED_EXISTING,
            INIT_ACTION,
            HANDLE_ACTION,
            SUSPENDED,
            FROZEN,
            TURNTABLE,
            UNKNOWN
        }

        public AI_MOVEMENT_STATE MovementState = AI_MOVEMENT_STATE.INIT;  // actual movement state

        public enum AI_START_MOVEMENT
        {
            SIGNAL_CLEARED,
            SIGNAL_RESTRICTED,
            FOLLOW_TRAIN,
            END_STATION_STOP,
            NEW,
            PATH_ACTION,
            TURNTABLE,
            RESET             // used to clear state
        }

        public AI AI;

        //  SPA: Add public in order to be able to get these infos in new AIActionItems
        public static float keepDistanceStatTrainM_P = 10.0f;  // stay 10m behind stationary train (pass in station)
        public static float keepDistanceStatTrainM_F = 50.0f;  // stay 50m behind stationary train (freight or pass outside station)
        public static float followDistanceStatTrainM = 30.0f;  // min dist for starting to follow
        public static float keepDistanceMovingTrainM = 300.0f; // stay 300m behind moving train
        public static float creepSpeedMpS = 2.5f;              // speed for creeping up behind train or upto signal
        public static float couplingSpeedMpS = 0.4f;           // speed for coupling to other train
        public static float maxFollowSpeedMpS = 15.0f;         // max. speed when following
        public static float movingtableSpeedMpS = 2.5f;        // speed for moving tables (approx. max 8 kph)
        public static float hysterisMpS = 0.5f;                // speed hysteris value to avoid instability
        public static float clearingDistanceM = 30.0f;         // clear distance to stopping point
        public static float minStopDistanceM = 3.0f;           // minimum clear distance for stopping at signal in station
        public static float signalApproachDistanceM = 20.0f;   // final approach to signal

        private readonly List<ObjectItemInfo> processedList = new List<ObjectItemInfo>(); // internal processing list for CheckSignalObjects()

#if WITH_PATH_DEBUG
        //  Only for EnhancedActCompatibility
        public string currentAIState = "";
        public string currentAIStation = "";
        int countRequiredAction = 0;
        public AIActionItem savedActionInfo = null;              // no next action

#endif
        //================================================================================================//
        /// <summary>
        /// Constructor
        /// </summary>

        public AITrain(Simulator simulator, Service_Definition sd, AI ai, AIPath path, float efficiency,
                string name, Traffic_Service_Definition trafficService, float maxVelocityA)
            : base(simulator)
        {
            ServiceDefinition = sd;
            UiD = ServiceDefinition.UiD;
            AI = ai;
            Path = path;
            TrainType = TRAINTYPE.AI_NOTSTARTED;
            StartTime = ServiceDefinition.Time;
            Efficiency = efficiency;
            if (Simulator.Settings.ActRandomizationLevel > 0 && Simulator.ActivityRun != null) // randomize efficiency
            {
                RandomizeEfficiency(ref Efficiency);
            }
            Name = name;
            TrafficService = trafficService;
            MaxVelocityA = maxVelocityA;
            // <CSComment> TODO: as Cars.Count is always = 0 at this point, activityClearingDistanceM is set to the short distance also for long trains
            // However as no one complained about AI train SPADs it may be considered to consolidate short distance for all trains</CSComment>
            if (Cars.Count < standardTrainMinCarNo) activityClearingDistanceM = shortClearingDistanceM;
        }

        public AITrain(Simulator simulator)
            : base(simulator)
        {
            TrainType = TRAINTYPE.AI_NOTSTARTED;

        }

        //================================================================================================//
        /// <summary>
        /// Convert route and build station list
        /// </summary>

        public void CreateRoute()
        {
            if (Path != null)
            {
                SetRoutePath(Path);
            }
            else
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];

                ValidRoute[0] = signalRef.BuildTempRoute(this, thisSection.Index, PresentPosition[1].TCOffset, PresentPosition[1].TCDirection, Length, true, true, false);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Convert route and build station list
        /// </summary>

        public void CreateRoute(bool usePosition)
        {
            if (Path != null && !usePosition)
            {
                SetRoutePath(Path, signalRef);
            }
            else if (Path != null)
            {
                SetRoutePath(Path);
            }
            else if (usePosition)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];

                ValidRoute[0] = signalRef.BuildTempRoute(this, thisSection.Index, PresentPosition[1].TCOffset, PresentPosition[1].TCDirection, Length, true, true, false);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Restore
        /// </summary>

        public AITrain(Simulator simulator, BinaryReader inf, AI airef)
            : base(simulator, inf)
        {
            AI = airef;
            ColdStart = false;
            UiD = inf.ReadInt32();
            MaxDecelMpSS = inf.ReadSingle();
            MaxAccelMpSS = inf.ReadSingle();

            if (Cars.Count < standardTrainMinCarNo) activityClearingDistanceM = shortClearingDistanceM;

            int startTimeValue = inf.ReadInt32();
            StartTime = startTimeValue < 0 ? null : (int?)startTimeValue;

            PowerState = inf.ReadBoolean();
            Alpha10 = inf.ReadInt32();

            MovementState = (AI_MOVEMENT_STATE)inf.ReadInt32();
            if (MovementState == AI_MOVEMENT_STATE.INIT_ACTION || MovementState == AI_MOVEMENT_STATE.HANDLE_ACTION) MovementState = AI_MOVEMENT_STATE.BRAKING;

            Efficiency = inf.ReadSingle();
            MaxVelocityA = inf.ReadSingle();
            UncondAttach = inf.ReadBoolean();
            DoorCloseTimer = inf.ReadSingle();
            DoorOpenTimer = inf.ReadSingle();
            ApproachTriggerSet = inf.ReadBoolean();
            if (!Simulator.TimetableMode && DoorOpenTimer <= 0 && DoorCloseTimer > 0 && Simulator.OpenDoorsInAITrains &&
                MovementState == AI_MOVEMENT_STATE.STATION_STOP && StationStops.Count > 0)
            {
                StationStop thisStation = StationStops[0];
                var frontIsFront = thisStation.PlatformReference == thisStation.PlatformItem.PlatformFrontUiD;
                foreach (MSTSWagon car in Cars.Cast<MSTSWagon>())
                {
                    if (thisStation.PlatformItem.PlatformSide[0])
                    {
                        // Open left doors
                        SetDoors(frontIsFront ? DoorSide.Right : DoorSide.Left, true);
                    }
                    if (thisStation.PlatformItem.PlatformSide[1])
                    {
                        // Open right doors
                        SetDoors(frontIsFront ? DoorSide.Left : DoorSide.Right, true);
                    }
                }
            }
            var doesLevelCrossingPatternExist = inf.ReadInt32();
            if (doesLevelCrossingPatternExist == 0)
                LevelCrossingHornPattern = AILevelCrossingHornPattern.Restore(inf);
            int serviceListCount = inf.ReadInt32();
            if (serviceListCount > 0) RestoreServiceDefinition(inf, serviceListCount);

            // Set signals and actions if train is active train

            bool activeTrain = true;

            if (TrainType == TRAINTYPE.AI_NOTSTARTED) activeTrain = false;
            if (TrainType == TRAINTYPE.AI_AUTOGENERATE) activeTrain = false;
            if (TrainType == TRAINTYPE.AI_INCORPORATED) activeTrain = false;

            if (activeTrain)
            {
                if (MovementState == AI_MOVEMENT_STATE.AI_STATIC || MovementState == AI_MOVEMENT_STATE.INIT) activeTrain = false;
            }

            if (activeTrain)
            {
                InitializeSignals(true);
                ResetActions(true);
                CheckSignalObjects();
                if (MovementState != AI_MOVEMENT_STATE.SUSPENDED) ObtainRequiredActions(0);
            }
            // Associate location events
            Simulator.ActivityRun?.AssociateEvents(this);
            LastSpeedMpS = SpeedMpS;
        }

        //================================================================================================//
        //
        // Restore of useful Service Items parameters
        //

        public void RestoreServiceDefinition(BinaryReader inf, int serviceLC)
        {
            ServiceDefinition = new Service_Definition();
            for (int iServiceList = 0; iServiceList < serviceLC; iServiceList++)
            {
                ServiceDefinition.ServiceList.Add(new Service_Item(inf.ReadSingle(), 0, 0.0f, inf.ReadInt32()));
            }
        }
        //================================================================================================//
        /// <summary>
        /// Save
        /// </summary>

        public override void Save(BinaryWriter outf)
        {
            // If something changes in this list, it must be changed also in Save(BinaryWriter outf) within TTTrain.cs
            outf.Write("AI");
            base.Save(outf);
            outf.Write(UiD);
            outf.Write(MaxDecelMpSS);
            outf.Write(MaxAccelMpSS);
            if (StartTime.HasValue)
            {
                outf.Write(StartTime.Value);
            }
            else
            {
                outf.Write(-1);
            }
            outf.Write(PowerState);
            outf.Write(Alpha10);

            outf.Write((int)MovementState);
            outf.Write(Efficiency);
            outf.Write(MaxVelocityA);
            outf.Write(UncondAttach);
            outf.Write(DoorCloseTimer);
            outf.Write(DoorOpenTimer);
            outf.Write(ApproachTriggerSet);
            if (LevelCrossingHornPattern != null)
            {
                outf.Write(0);
                LevelCrossingHornPattern.Save(outf);
            }
            else outf.Write(-1);
            if (ServiceDefinition != null) ServiceDefinition.Save(outf);
            else outf.Write(-1);
        }

        // Call base save method only
        public void SaveBase(BinaryWriter outf)
        {
            base.Save(outf);
        }

        //================================================================================================//
        /// <summary>
        /// Set starting conditions when speed > 0 
        /// </summary>
        /// 

        public override void InitializeMoving() // TODO
        {
            {
                ColdStart = false;
                if (TrainType == TRAINTYPE.AI_PLAYERDRIVEN)
                {
                    base.InitializeMoving();
                    return;
                }
                SpeedMpS = InitialSpeed;
                MUDirection = Direction.Forward;
                float initialThrottlepercent = InitialThrottlepercent;
                MUDynamicBrakePercent = -1;
                AITrainBrakePercent = 0;
                // Force calculate gradient at the front of the train
                FirstCar.UpdateGravity();
                // Give it a bit more gas if it is uphill
                if (FirstCar.CurrentElevationPercent < -2.0) initialThrottlepercent = 40f;
                // Better block gas if it is downhill
                else if (FirstCar.CurrentElevationPercent > 1.0) initialThrottlepercent = 0f;
                AdjustControlsBrakeOff();
                AITrainThrottlePercent = initialThrottlepercent;

                TraincarsInitializeMoving();
                LastSpeedMpS = SpeedMpS;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Post Init (override from Train)
        /// Performs all actions required to start
        /// </summary>

        public override bool PostInit()
        {

#if DEBUG_CHECKTRAIN
            if (!CheckTrain)
            {
                if (Number == 134)
                {
                    DateTime baseDT = new DateTime();
                    DateTime actTime = baseDT.AddSeconds(AI.clockTime);

                    File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Activated : ");
                    File.AppendAllText(@"C:\temp\checktrain.txt", actTime.ToString("HH:mm:ss") + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");

                    CheckTrain = true;
                }
            }
#endif
            // Check deadlocks; do it after placing for player train, like done for it when autopilot option unchecked

            if (!IsActualPlayerTrain)
                CheckDeadlock(ValidRoute[0], Number);

            // Set up horn blow at crossings if required
            var activityFile = Simulator.Activity.Tr_Activity.Tr_Activity_File;
            LevelCrossingHornPattern = activityFile.AIBlowsHornAtLevelCrossings ? AILevelCrossingHornPattern.CreateInstance(activityFile.AILevelCrossingHornPattern) : null;

            // Set initial position and state

            bool atStation = false;
            bool validPosition = InitialTrainPlacement(); // Check track and if clear, set occupied

            if (validPosition)
            {
                if (IsFreight)
                {
                    MaxAccelMpSS = MaxAccelMpSSF; // Set freight acceleration and deceleration
                    MaxDecelMpSS = MaxAccelMpSSF;
                }
                else
                {
                    MaxAccelMpSS = MaxAccelMpSSP; // Set passenger accel. and decel.
                    MaxDecelMpSS = MaxDecelMpSSP;
                    if (TrainMaxSpeedMpS > 55.0f)
                    {
                        MaxDecelMpSS = 2.5f * MaxDecelMpSSP; // Higher decel. for very high speed trains
                    }
                    else if (TrainMaxSpeedMpS > 40.0f)
                    {
                        MaxDecelMpSS = 1.5f * MaxDecelMpSSP; // Higher decel. for high speed trains

                    }
                    else
                    {
                        var carF = Cars[0];
                        var carL = Cars[Cars.Count - 1];
                        if (carF.IsDriveable && carF.HasPassengerCapacity && (carF is MSTSElectricLocomotive)
                            && carL.IsDriveable && carL.HasPassengerCapacity && (carL is MSTSElectricLocomotive)) // EMU or DMU train, higher decel.
                        {
                            MaxAccelMpSS = 1.5f * MaxAccelMpSS;
                            MaxDecelMpSS = 2f * MaxDecelMpSSP;
                        }
                    }

                }

                BuildWaitingPointList(activityClearingDistanceM);
                BuildStationList(activityClearingDistanceM);

                // <CSComment> This creates problems in push-pull paths </CSComment>
                //                StationStops.Sort();
                if (!atStation && StationStops.Count > 0 && this != Simulator.Trains[0])
                {
                    if (MaxVelocityA > 0 &&
                        ServiceDefinition != null && ServiceDefinition.ServiceList.Count > 0)
                    {
                        // <CScomment> Gets efficiency from .act file to override TrainMaxSpeedMpS computed from .srv efficiency
                        var sectionEfficiency = ServiceDefinition.ServiceList[0].Efficiency;
                        if (Simulator.Settings.ActRandomizationLevel > 0) RandomizeEfficiency(ref sectionEfficiency);
                        if (sectionEfficiency > 0)
                            TrainMaxSpeedMpS = Math.Min((float)Simulator.TRK.Tr_RouteFile.SpeedLimit, MaxVelocityA * sectionEfficiency);
                    }
                }

                InitializeSignals(false); // Get signal information
                if (IsActualPlayerTrain)
                    CheckDeadlock(ValidRoute[0], Number);
                TCRoute.SetReversalOffset(Length, false); // Set reversal information for first subpath
                SetEndOfRouteAction(); // Set action to ensure train stops at end of route

                // Check if train starts at station stop
                AuxActionsContain.SetAuxAction(this);
                if (StationStops.Count > 0)
                {
                    atStation = CheckInitialStation();
                }

                if (!atStation)
                {
                    if (StationStops.Count > 0)
                    {
                        SetNextStationAction(); // Set station details
                    }

                    if (TrainHasPower())
                    {
                        MovementState = AI_MOVEMENT_STATE.INIT; // Start in STOPPED mode to collect info
                    }
                }
            }

            if (IsActualPlayerTrain)
                SetTrainSpeedLoggingFlag();

            if (CheckTrain)
            {
                DateTime baseDT = new DateTime();
                DateTime actTime = baseDT.AddSeconds(AI.clockTime);

                File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "PostInit at " + actTime.ToString("HH:mm:ss") + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Train : " + Number.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Name  : " + Name + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Frght : " + IsFreight.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Length: " + Length.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "MaxSpd: " + TrainMaxSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Start : " + StartTime.Value.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "State : " + MovementState.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "AtStat: " + atStation.ToString() + "\n");
                if (Delay.HasValue)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Delay : " + Delay.Value.TotalMinutes.ToString() + "\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Delay : - \n");
                }
                File.AppendAllText(@"C:\temp\checktrain.txt", "ValPos: " + validPosition.ToString() + "\n");
            }

            return validPosition;
        }

        //================================================================================================//
        /// <summary>
        /// Check initial station
        /// </summary>

        public virtual bool CheckInitialStation()
        {
            bool atStation = false;

            // Get station details
            StationStop thisStation = StationStops[0];
            if (thisStation.SubrouteIndex != TCRoute.activeSubpath)
            {
                return false;
            }

            if (thisStation.ActualStopType != StationStop.STOPTYPE.STATION_STOP)
            {
                return false;
            }

            atStation = CheckStationPosition(thisStation.PlatformItem, thisStation.Direction, thisStation.TCSectionIndex);

            // At station: set state, create action item
            if (atStation)
            {
                thisStation.ActualArrival = -1;
                thisStation.ActualDepart = -1;
                MovementState = AI_MOVEMENT_STATE.STATION_STOP;

                AIActionItem newAction = new AIActionItem(null, AIActionItem.AI_ACTION_TYPE.STATION_STOP);
                newAction.SetParam(-10f, 0.0f, 0.0f, 0.0f);
                nextActionInfo = newAction;
                NextStopDistanceM = 0.0f;

#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                     Number.ToString() + " initial at station " +
                     StationStops[0].PlatformItem.Name + "\n");
#endif
            }

            return atStation;
        }

        //================================================================================================//
        /// <summary>
        /// Get AI Movement State
        /// </summary>

        public override AI_MOVEMENT_STATE GetAIMovementState()
        {
            return ControlMode == TRAIN_CONTROL.INACTIVE ? AI_MOVEMENT_STATE.AI_STATIC : MovementState;
        }

        //================================================================================================//
        /// <summary>
        /// Get AI Movement State
        /// </summary>
        /// 
        private void RandomizeEfficiency(ref float efficiency)
        {
            efficiency *= 100;
            var incOrDecEfficiency = DateTime.Now.Millisecond % 2 == 0;
            if (incOrDecEfficiency) efficiency = Math.Min(100, efficiency + RandomizedDelayWithThreshold(20)); // increment it
            else if (efficiency > 50) efficiency = Math.Max(50, efficiency - RandomizedDelayWithThreshold(20)); // decrement it
            efficiency /= 100;
        }

        //================================================================================================//
        /// <summary>
        /// Update
        /// Update function for a single AI train.
        /// </summary>

        public void AIUpdate(float elapsedClockSeconds, double clockTime, bool preUpdate)
        {
#if DEBUG_CHECKTRAIN
            if (!CheckTrain)
            {
                if (Number == 134)
                {
                    DateTime baseDT = new DateTime();
                    DateTime actTime = baseDT.AddSeconds(AI.clockTime);

                    File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Activated : ");
                    File.AppendAllText(@"C:\temp\checktrain.txt", actTime.ToString("HH:mm:ss") + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");

                    CheckTrain = true;
                }
            }
#endif

            PreUpdate = preUpdate; // Flag for pre-update phase
#if WITH_PATH_DEBUG
            int lastIndex = PreviousPosition[0].RouteListIndex;
            int presentIndex = PresentPosition[0].RouteListIndex;
            if (lastIndex != presentIndex || countRequiredAction != requiredActions.Count)
            {
                countRequiredAction = requiredActions.Count;
                File.AppendAllText(@"C:\temp\checkpath.txt", "Train position change: Train" + Number  
                    + " direction " + PresentPosition[0].TCDirection 
                    + "\n");
            }
            if (nextActionInfo != savedActionInfo)
            {
                savedActionInfo = nextActionInfo;
            }
#endif

            if (TrainType == TRAINTYPE.AI_INCORPORATED || TrainType == TRAINTYPE.STATIC || MovementState == AI_MOVEMENT_STATE.SUSPENDED || MovementState == AI_MOVEMENT_STATE.FROZEN)
                return;
            // Check if at stop point and stopped.
            //          if ((NextStopDistanceM < actClearance) || (SpeedMpS <= 0 && MovementState == AI_MOVEMENT_STATE.STOPPED))
            // <CSComment> TODO: next if block is in effect only a workaround due to OR braking physics not working well with AI trains
            if (MovementState == AI_MOVEMENT_STATE.STOPPED || MovementState == AI_MOVEMENT_STATE.STATION_STOP || MovementState == AI_MOVEMENT_STATE.AI_STATIC ||
                MovementState == AI_MOVEMENT_STATE.INIT_ACTION || MovementState == AI_MOVEMENT_STATE.HANDLE_ACTION)
            {
                SpeedMpS = 0;
                foreach (TrainCar car in Cars)
                {
                    car.MotiveForceN = 0;
                    car.TotalForceN = 0;
                    car.SpeedMpS = 0;
                }

                AITrainThrottlePercent = 0;
                AITrainBrakePercent = 100;
            }

            // Update position, route clearance and objects
            if (MovementState == AI_MOVEMENT_STATE.AI_STATIC)
            {
                CalculatePositionOfCars(0, 0); // Required to make train visible; set elapsed time to zero to avoid actual movement
            }
            else
            {
                if (!preUpdate)
                {
                    Update(elapsedClockSeconds, false);
                }
                else
                {
                    AIPreUpdate(elapsedClockSeconds);
                }

                // Get through list of objects, determine necesarry actions
                CheckSignalObjects();

                // Check if state still matches authority level
                if (MovementState != AI_MOVEMENT_STATE.INIT && ControlMode == TRAIN_CONTROL.AUTO_NODE && EndAuthorityType[0] != END_AUTHORITY.MAX_DISTANCE) // restricted authority
                {
                    CheckRequiredAction();
                }

                // Check if reversal point reached and not yet activated - but station stop has preference over reversal point
                SetReversalAction();

                // Check if out of control - if so, remove
                if (ControlMode == TRAIN_CONTROL.OUT_OF_CONTROL && !(TrainType == TRAINTYPE.AI_PLAYERHOSTING || Autopilot))
                {
                    Trace.TraceInformation("Train {0} ({1}) is removed for out of control, reason : {2}", Name, Number, OutOfControlReason.ToString());
                    RemoveTrain();
                    return;
                }

                if (Cars[0] is MSTSLocomotive leadingLoco)
                {
                    var isRainingOrSnowing = Simulator.Weather.PrecipitationIntensityPPSPM2 > 0;
                    if (leadingLoco.Wiper && !isRainingOrSnowing)
                        leadingLoco.SignalEvent(Event.WiperOff);
                    else if (!leadingLoco.Wiper && isRainingOrSnowing)
                        leadingLoco.SignalEvent(Event.WiperOn);
                }
            }

            // Switch on action depending on state
            int presentTime = Convert.ToInt32(Math.Floor(clockTime));

#if WITH_PATH_DEBUG
            currentAIStation = " ---";
#endif
            bool[] stillExist;

            AuxActionsContain.ProcessGenAction(this, presentTime, elapsedClockSeconds, MovementState);
            MovementState = AuxActionsContain.ProcessSpecAction(this, presentTime, elapsedClockSeconds, MovementState);

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Switch MovementState : " + MovementState + "\n");
            }

            switch (MovementState)
            {
                case AI_MOVEMENT_STATE.AI_STATIC:
                    UpdateAIStaticState(presentTime);
                    break;
                case AI_MOVEMENT_STATE.STOPPED:
                    if (nextActionInfo != null && nextActionInfo.GetType().IsSubclassOf(typeof(AuxActionItem)))
                    {
                        MovementState = nextActionInfo.ProcessAction(this, presentTime, elapsedClockSeconds, MovementState);
                    }
                    else
                    {
                        stillExist = ProcessEndOfPath(presentTime, false);
                        if (stillExist[1])
                        {
                            if (nextActionInfo != null && nextActionInfo.GetType().IsSubclassOf(typeof(AuxActionItem)))
                            {
                                MovementState = nextActionInfo.ProcessAction(this, presentTime, elapsedClockSeconds, MovementState);
                            }
                            else if (MovementState == AI_MOVEMENT_STATE.STOPPED) // process only if moving state has not changed
                            {
                                UpdateStoppedState(elapsedClockSeconds);
                            }
                        }
                    }
                    break;
                case AI_MOVEMENT_STATE.INIT:
                    stillExist = ProcessEndOfPath(presentTime);
                    if (stillExist[1]) UpdateStoppedState(elapsedClockSeconds);
                    break;
                case AI_MOVEMENT_STATE.TURNTABLE:
                    UpdateTurntableState(elapsedClockSeconds, presentTime);
                    break;
                case AI_MOVEMENT_STATE.STATION_STOP:
                    UpdateStationState(elapsedClockSeconds, presentTime);
                    break;
                case AI_MOVEMENT_STATE.BRAKING:
                    UpdateBrakingState(elapsedClockSeconds, presentTime);
                    break;
                case AI_MOVEMENT_STATE.APPROACHING_END_OF_PATH:
                    UpdateBrakingState(elapsedClockSeconds, presentTime);
                    break;
                case AI_MOVEMENT_STATE.ACCELERATING:
                    UpdateAccelState(elapsedClockSeconds);
                    break;
                case AI_MOVEMENT_STATE.FOLLOWING:
                    UpdateFollowingState(elapsedClockSeconds, presentTime);
                    break;
                case AI_MOVEMENT_STATE.RUNNING:
                    UpdateRunningState(elapsedClockSeconds);
                    break;
                case AI_MOVEMENT_STATE.STOPPED_EXISTING:
                    UpdateStoppedState(elapsedClockSeconds);
                    break;
                default:
                    if (nextActionInfo != null && nextActionInfo.GetType().IsSubclassOf(typeof(AuxActionItem)))
                    {
                        MovementState = nextActionInfo.ProcessAction(this, presentTime, elapsedClockSeconds, MovementState);
                    }
                    break;
            }
#if WITH_PATH_DEBUG
            //if (Simulator.Settings.EnhancedActCompatibility)
            {
                switch (MovementState)
                {
                    case AI_MOVEMENT_STATE.AI_STATIC:
                        currentAIState = "STATIC";
                        break;
                    case AI_MOVEMENT_STATE.STOPPED:
                        currentAIState = "STOPPED";
                        break;
                    case AI_MOVEMENT_STATE.INIT:
                        currentAIState = "INIT";
                        break;
                    case AI_MOVEMENT_STATE.STATION_STOP:
                        currentAIState = "STATION_STOP";
                        break;
                    case AI_MOVEMENT_STATE.BRAKING:
                        currentAIState = "BRAKING";
                        break;
                    case AI_MOVEMENT_STATE.APPROACHING_END_OF_PATH:
                        currentAIState = "APPROACHING_EOP";
                        break;
                    case AI_MOVEMENT_STATE.ACCELERATING:
                        currentAIState = "ACCELERATING";
                        break;
                    case AI_MOVEMENT_STATE.FOLLOWING:
                        currentAIState = "FOLLOWING";
                        break;
                    case AI_MOVEMENT_STATE.RUNNING:
                        currentAIState = "RUNNING";
                        break;
                    case AI_MOVEMENT_STATE.HANDLE_ACTION:
                        currentAIState = "HANDLE";
                        break;
                }
                if (nextActionInfo != null)
                {
                    switch (nextActionInfo.NextAction)
                    {
                        case AIActionItem.AI_ACTION_TYPE.STATION_STOP:
                            currentAIState = String.Concat(currentAIState, " to STOP");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.SPEED_LIMIT:
                            currentAIState = String.Concat(currentAIState, " to SPEED_LIMIT");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.SPEED_SIGNAL:
                            currentAIState = String.Concat(currentAIState, " to SPEED_SIGNAL");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP:
                            currentAIState = String.Concat(currentAIState, " to SIGNAL_ASPECT_STOP");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED:
                            currentAIState = String.Concat(currentAIState, " to SIGNAL_ASPECT_RESTRICTED");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY:
                            currentAIState = String.Concat(currentAIState, " to END_OF_AUTHORITY");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD:
                            currentAIState = String.Concat(currentAIState, " to TRAIN_AHEAD");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE:
                            currentAIState = String.Concat(currentAIState, " to END_OF_ROUTE");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.REVERSAL:
                            currentAIState = String.Concat(currentAIState, " to REVERSAL");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.AUX_ACTION:
                            currentAIState = String.Concat(currentAIState, " to AUX ACTION ");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.NONE:
                            currentAIState = String.Concat(currentAIState, " to NONE ");
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    currentAIState = String.Concat(currentAIState, " to ???? ");
                }
                currentAIState = String.Concat(currentAIState, currentAIStation);
            }
#endif
            LastSpeedMpS = SpeedMpS;

            if (CheckTrain)
            {
                DateTime baseDT = new DateTime();
                DateTime actTime = baseDT.AddSeconds(AI.clockTime);

                File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", actTime.ToString("HH:mm:ss") + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "DistTrv: " + DistanceTravelledM.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "PresPos: " + PresentPosition[0].TCSectionIndex.ToString() + " + " +
                                     PresentPosition[0].TCOffset.ToString() + " : " +
                                     PresentPosition[0].RouteListIndex.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "Route Length : " + ValidRoute[0].Count + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "Speed  : " + FormatStrings.FormatSpeed(SpeedMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "Thrott : " + AITrainThrottlePercent.ToString() + " ; Brake : " + AITrainBrakePercent.ToString() + "\n");

                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "Control: " + ControlMode.ToString() + "\n");

                if (ControlMode == TRAIN_CONTROL.AUTO_NODE)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                       "Auth   : " + EndAuthorityType[0].ToString() + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                       "AuthDis: " + DistanceToEndNodeAuthorityM[0].ToString() + "\n");
                }

                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "Movm   : " + MovementState.ToString() + "\n");

                if (NextSignalObject[0] != null)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                       "NextSig: " + NextSignalObject[0].thisRef.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                       "Section: " + NextSignalObject[0].TCReference.ToString() + "\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                        "NextSig: null\n");
                }

                if (nextActionInfo != null)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                       "Action : " + nextActionInfo.NextAction.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                       "ActDist: " + nextActionInfo.ActivateDistanceM.ToString() + "\n");

                    if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                           "NextSig: " + nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                           "Section: " + nextActionInfo.ActiveItem.ObjectDetails.TCReference.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                           "DistTr : " + nextActionInfo.ActiveItem.distance_to_train.ToString() + "\n");
                    }
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                        "Action : null\n");
                }

                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "StopDst: " + NextStopDistanceM.ToString() + "\n");

                File.AppendAllText(@"C:\temp\checktrain.txt", "\nDeadlock Info\n");
                foreach (KeyValuePair<int, List<Dictionary<int, int>>> thisDeadlock in DeadlockInfo)
                {
                    File.AppendAllText(@"C:\Temp\checktrain.txt", "Section : " + thisDeadlock.Key.ToString() + "\n");
                    foreach (Dictionary<int, int> actDeadlocks in thisDeadlock.Value)
                    {
                        foreach (KeyValuePair<int, int> actDeadlockInfo in actDeadlocks)
                        {
                            File.AppendAllText(@"C:\Temp\checktrain.txt", "  Other Train : " + actDeadlockInfo.Key.ToString() +
                                " - end Sector : " + actDeadlockInfo.Value.ToString() + "\n");
                        }
                    }
                    File.AppendAllText(@"C:\Temp\checktrain.txt", "\n");
                }
            }
#if WITH_PATH_DEBUG
            lastIndex = PreviousPosition[0].RouteListIndex;
            presentIndex = PresentPosition[0].RouteListIndex;
            if (lastIndex != presentIndex || countRequiredAction != requiredActions.Count)
            {
                countRequiredAction = requiredActions.Count;
            }
#endif
            // TODO: Can we remove this?
            // Trace.TraceWarning ("Time {0} Train no. {1} Speed {2} AllowedMaxSpeed {3} Throttle percent {4} Distance travelled {5} Movement State {6} BrakePerCent {7}",
            // clockTime, Number, SpeedMpS, AllowedMaxSpeedMpS, AITrainThrottlePercent, DistanceTravelledM, MovementState, AITrainBrakePercent);
        }

        //================================================================================================//
        /// <summary>
        /// Update for pre-update state
        /// </summary>

        public virtual void AIPreUpdate(float elapsedClockSeconds)
        {
            // Calculate delta speed and speed
            float deltaSpeedMpS = ((0.01f * AITrainThrottlePercent * MaxAccelMpSS) - (0.01f * AITrainBrakePercent * MaxDecelMpSS)) *
                Efficiency * elapsedClockSeconds;
            if (AITrainBrakePercent > 0 && deltaSpeedMpS < 0 && Math.Abs(deltaSpeedMpS) > SpeedMpS)
            {
                deltaSpeedMpS = -SpeedMpS;
            }
            SpeedMpS = Math.Min(TrainMaxSpeedMpS, Math.Max(0.0f, SpeedMpS + deltaSpeedMpS));

            // Calculate position
            float distanceM = SpeedMpS * elapsedClockSeconds;

            if (float.IsNaN(distanceM)) distanceM = 0; // Sometimes this turns out to be NaN, force it to be 0 and prevent AI movement

            // Force stop
            if (distanceM > NextStopDistanceM)
            {
#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                    Number.ToString() + " forced stop : calculated " +
                    FormatStrings.FormatSpeed(SpeedMpS, true) + " > " +
                    distanceM.ToString() + " set to " +
                    "0.0 > " + NextStopDistanceM.ToString() + " at " +
                    DistanceTravelledM.ToString() + "\n");
#endif
                // TODO: Should this be removed?
                // Trace.TraceWarning("Forced stop for train {0} ({1}) at speed {2}", Number, Name, SpeedMpS);
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                        Number.ToString() + " forced stop : calculated " +
                        FormatStrings.FormatSpeed(SpeedMpS, true) + " > " +
                        distanceM.ToString() + " set to " +
                        "0.0 > " + NextStopDistanceM.ToString() + " at " +
                        DistanceTravelledM.ToString() + "\n");
                }

                distanceM = Math.Max(0.0f, NextStopDistanceM);
                SpeedMpS = 0;
            }

            // Set speed and position
            foreach (TrainCar car in Cars)
            {
                car.SpeedMpS = car.Flipped ? -SpeedMpS : SpeedMpS;
            }

            CalculatePositionOfCars(elapsedClockSeconds, distanceM);

            DistanceTravelledM += distanceM;

            // Perform overall update
            if (ValidRoute != null) // no actions required for static objects
            {
                movedBackward = CheckBackwardClearance();                                           // check clearance at rear
                UpdateTrainPosition();                                                              // position update           
                UpdateTrainPositionInformation();                                                   // position linked info
                int SignalObjIndex = CheckSignalPassed(0, PresentPosition[0], PreviousPosition[0]); // check if passed signal
                UpdateSectionState(movedBackward);                                                  // update track occupation
                ObtainRequiredActions(movedBackward);                                               // process Actions
                UpdateRouteClearanceAhead(SignalObjIndex, movedBackward, elapsedClockSeconds);      // update route clearance
                UpdateSignalState(movedBackward);                                                   // update signal state
            }
        }

        //================================================================================================//
        /// <summary>
        /// Set reversal point action
        /// </summary>

        public virtual void SetReversalAction()
        {
            if ((nextActionInfo == null ||
                 (nextActionInfo.NextAction != AIActionItem.AI_ACTION_TYPE.STATION_STOP && nextActionInfo.NextAction != AIActionItem.AI_ACTION_TYPE.REVERSAL)) &&
                 TCRoute.ReversalInfo[TCRoute.activeSubpath].Valid)
            {
                int reqSection = TCRoute.ReversalInfo[TCRoute.activeSubpath].SignalUsed ?
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].LastSignalIndex :
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].LastDivergeIndex;

                if (reqSection >= 0 && PresentPosition[1].RouteListIndex >= reqSection && TCRoute.ReversalInfo[TCRoute.activeSubpath].ReversalActionInserted == false)
                {
                    float reqDistance = SpeedMpS * SpeedMpS * MaxDecelMpSS;
                    reqDistance = nextActionInfo != null ? Math.Min(nextActionInfo.RequiredDistance, reqDistance) : reqDistance;

                    var distanceToReversalPoint = ComputeDistanceToReversalPoint();
                    // <CSComment: The AI train runs up to the reverse point no matter how far it is from the diverging point.

                    CreateTrainAction(TrainMaxSpeedMpS, 0.0f, distanceToReversalPoint, null, AIActionItem.AI_ACTION_TYPE.REVERSAL);
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].ReversalActionInserted = true;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Change in authority state - check action
        /// </summary>

        public virtual void CheckRequiredAction()
        {
            // Check if train ahead
            if (EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
            {
                if (MovementState != AI_MOVEMENT_STATE.STATION_STOP && MovementState != AI_MOVEMENT_STATE.STOPPED)
                {
                    if (MovementState != AI_MOVEMENT_STATE.INIT_ACTION && MovementState != AI_MOVEMENT_STATE.HANDLE_ACTION)
                    {
                        MovementState = AI_MOVEMENT_STATE.FOLLOWING; // Start following
                    }
                }
            }
            else if (EndAuthorityType[0] == END_AUTHORITY.RESERVED_SWITCH || EndAuthorityType[0] == END_AUTHORITY.LOOP)
            {
                if (MovementState != AI_MOVEMENT_STATE.INIT_ACTION && MovementState != AI_MOVEMENT_STATE.HANDLE_ACTION &&
                     (nextActionInfo == null || nextActionInfo.NextAction != AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY))
                {
                    ResetActions(true);
                    NextStopDistanceM = DistanceToEndNodeAuthorityM[0] - (2.0f * junctionOverlapM);
                    CreateTrainAction(SpeedMpS, 0.0f, NextStopDistanceM, null,
                                AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY);
                    ObtainRequiredActions(0);
                }
            }
            // First handle outstanding actions
            else if (EndAuthorityType[0] == END_AUTHORITY.END_OF_PATH &&
                (nextActionInfo == null || nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE))
            {
                ResetActions(false);
                NextStopDistanceM = TCRoute.activeSubpath < TCRoute.TCRouteSubpaths.Count - 1
                    ? DistanceToEndNodeAuthorityM[0] - activityClearingDistanceM
                    : ComputeDistanceToReversalPoint() - activityClearingDistanceM;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check all signal objects
        /// </summary>
        public void CheckSignalObjects()
        {
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Check Objects \n");
            }

            float validSpeed = AllowedMaxSpeedMpS;
            processedList.Clear();

            foreach (ObjectItemInfo thisInfo in SignalObjectItems)
            {
                if (thisInfo.speed_isWarning)
                    continue;

                // Check speedlimit
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                            "Item : " + thisInfo.ObjectType.ToString() + " at " +
                                        thisInfo.distance_to_train.ToString() +
                        " - processed : " + thisInfo.processed.ToString() + "\n");

                    if (thisInfo.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                "  Signal : " + thisInfo.ObjectDetails.thisRef + " - State : " + thisInfo.signal_state.ToString() + "\n");
                    }
                }

                float setSpeed = IsFreight ? thisInfo.speed_freight : thisInfo.speed_passenger;
                if (setSpeed < validSpeed && setSpeed < AllowedMaxSpeedMpS && setSpeed > 0)
                {
                    if (!thisInfo.processed)
                    {
                        var process_req = (ControlMode != TRAIN_CONTROL.AUTO_NODE ||
                                        thisInfo.distance_to_train <= DistanceToEndNodeAuthorityM[0])
&& (thisInfo.distance_to_train > signalApproachDistanceM ||
                                                             (MovementState == AI_MOVEMENT_STATE.RUNNING && SpeedMpS > setSpeed) ||
                                                              MovementState == AI_MOVEMENT_STATE.ACCELERATING);
                        if (process_req)
                        {
                            if (thisInfo.ObjectType == ObjectItemInfo.ObjectItemType.Speedlimit)
                            {
                                CreateTrainAction(validSpeed, setSpeed,
                                        thisInfo.distance_to_train, thisInfo, AIActionItem.AI_ACTION_TYPE.SPEED_LIMIT);
                            }
                            else
                            {
                                CreateTrainAction(validSpeed, setSpeed,
                                        thisInfo.distance_to_train, thisInfo, AIActionItem.AI_ACTION_TYPE.SPEED_SIGNAL);
                            }
                            processedList.Add(thisInfo);
                        }
                    }
                    validSpeed = setSpeed;
                }
                else if (setSpeed > 0)
                {
                    validSpeed = setSpeed;
                }

                // Check signal state
                if (thisInfo.ObjectType == ObjectItemInfo.ObjectItemType.Signal &&
                        thisInfo.signal_state < MstsSignalAspect.APPROACH_1 &&
                        !thisInfo.processed && thisInfo.ObjectDetails.hasPermission != SignalObject.Permission.Granted)
                {
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Signal restricted\n");
                    }
                    if (!(ControlMode == TRAIN_CONTROL.AUTO_NODE &&
                                    thisInfo.distance_to_train > (DistanceToEndNodeAuthorityM[0] - clearingDistanceM)))
                    {
                        if (thisInfo.signal_state == MstsSignalAspect.STOP ||
                            thisInfo.ObjectDetails.enabledTrain != routedForward)
                        {
                            CreateTrainAction(validSpeed, 0.0f,
                                    thisInfo.distance_to_train, thisInfo,
                                    AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP);
                            processedList.Add(thisInfo);
                            var validClearingDistanceM = Simulator.TimetableMode ? clearingDistanceM : activityClearingDistanceM;
                            if (((thisInfo.distance_to_train - validClearingDistanceM) < validClearingDistanceM) &&
                                         (SpeedMpS > 0.0f || MovementState == AI_MOVEMENT_STATE.ACCELERATING))
                            {
                                AITrainBrakePercent = 100;
                                AITrainThrottlePercent = 0;
                                NextStopDistanceM = validClearingDistanceM;
                                if (PreUpdate && !Simulator.TimetableMode) ObtainRequiredActions(movedBackward); // Fast track to stop train; else a precious update is lost
                            }
                        }
                        else if (thisInfo.distance_to_train > 2.0f * signalApproachDistanceM) // Set restricted only if not close
                        {
                            if (!thisInfo.ObjectDetails.this_sig_noSpeedReduction(SignalFunction.NORMAL))
                            {
                                CreateTrainAction(validSpeed, 0.0f,
                                        thisInfo.distance_to_train, thisInfo,
                                        AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED);
                            }
                            processedList.Add(thisInfo);
                        }
                    }
                    else if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Signal not set due to AUTO_NODE state \n");
                    }
                }
            }

            // Set processed items - must be collected as item can be processed twice (speed and signal)
            foreach (ObjectItemInfo thisInfo in processedList)
            {
                thisInfo.processed = true;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check for next station
        /// </summary>
        public virtual void SetNextStationAction(bool fromAutopilotSwitch = false)
        {
            // If train is player driven and is at station, do nothing
            if (TrainType == TRAINTYPE.AI_PLAYERDRIVEN && this == Simulator.OriginalPlayerTrain && Simulator.ActivityRun.Current is ActivityTaskPassengerStopAt &&
                ((ActivityTaskPassengerStopAt)Simulator.ActivityRun.Current).IsAtStation(this)) return;

            // Check if station in this subpath
            int stationIndex = 0;
            StationStop thisStation = StationStops[stationIndex];
            while (thisStation.SubrouteIndex < TCRoute.activeSubpath) // Station was in previous subpath
            {
                StationStops.RemoveAt(0);
                if (StationStops.Count == 0) // No more stations
                {
                    return;
                }
                thisStation = StationStops[0];
            }

            if (thisStation.SubrouteIndex > TCRoute.activeSubpath) // Station is not in this subpath
            {
                return;
            }

            // Get distance to station, but not if just after switch to Autopilot and not during station stop
            bool validStop = false;
            if (!fromAutopilotSwitch || (Simulator.PlayerLocomotive != null && Simulator.ActivityRun != null && !(
                this == Simulator.OriginalPlayerTrain && Simulator.ActivityRun.Current is ActivityTaskPassengerStopAt && ((ActivityTaskPassengerStopAt)Simulator.ActivityRun.Current).IsAtStation(this))))
            {
                while (!validStop)
                {
                    float[] distancesM = CalculateDistancesToNextStation(thisStation, TrainMaxSpeedMpS, false);
                    if (distancesM[0] < 0f && !(MovementState == AI_MOVEMENT_STATE.STATION_STOP && distancesM[0] != -1)) // Stop is not valid
                    {

                        StationStops.RemoveAt(0);
                        if (StationStops.Count == 0)
                        {
                            return; // No more stations - exit
                        }

                        thisStation = StationStops[0];
                        if (thisStation.SubrouteIndex > TCRoute.activeSubpath) return; // Station not in this subpath - exit
                    }
                    else
                    {
                        validStop = true;
                        AIActionItem newAction = new AIActionItem(null, AIActionItem.AI_ACTION_TYPE.STATION_STOP);
                        newAction.SetParam(distancesM[1], 0.0f, distancesM[0], DistanceTravelledM);
                        requiredActions.InsertAction(newAction);
                        ApproachTriggerSet = false;

#if DEBUG_REPORTS
                if (StationStops[0].ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                {
                    File.AppendAllText(@"C:\temp\printproc.txt", "Insert for train " +
                        Number.ToString() + ", type STATION_STOP (" +
                        StationStops[0].PlatformItem.Name + "), at " +
                        distancesM[0].ToString() + ", trigger at " +
                        distancesM[1].ToString() + " (now at " +
                        PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
        }
        else if (StationStops[0].ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
        {
            File.AppendAllText(@"C:\temp\printproc.txt", "Insert for train " +
                        Number.ToString() + ", type WAITING_POINT (" +
                        distancesM[0].ToString() + ", trigger at " +
                        distancesM[1].ToString() + " (now at " +
                        PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
                }
#endif

                        if (CheckTrain)
                        {
                            if (StationStops[0].ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Insert for train " +
                                        Number.ToString() + ", type STATION_STOP (" +
                                        StationStops[0].PlatformItem.Name + "), at " +
                                        distancesM[0].ToString() + ", trigger at " +
                                        distancesM[1].ToString() + " (now at " +
                                        PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
                            }
                            else if (StationStops[0].ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Insert for train " +
                                            Number.ToString() + ", type WAITING_POINT (" +
                                            distancesM[0].ToString() + ", trigger at " +
                                            distancesM[1].ToString() + " (now at " +
                                            PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
                            }
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Calculate actual distance and trigger distance for next station
        /// </summary>
        public float[] CalculateDistancesToNextStation(StationStop thisStation, float presentSpeedMpS, bool reschedule)
        {
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex];
            float leftInSectionM = thisSection.Length - PresentPosition[0].TCOffset;

            // Get station route index - if not found, return distances < 0

            int stationIndex0 = ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, PresentPosition[0].RouteListIndex);
            int stationIndex1 = ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, PresentPosition[1].RouteListIndex);

            float distanceToTrainM = -1f;

            // Use front position
            if (stationIndex0 >= 0)
            {
                distanceToTrainM = ValidRoute[0].GetDistanceAlongRoute(PresentPosition[0].RouteListIndex,
                    leftInSectionM, stationIndex0, thisStation.StopOffset, true, signalRef);
            }

            // If front beyond station, use rear position (correct for length)
            else if (stationIndex1 >= 0)
            {
                thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
                leftInSectionM = thisSection.Length - PresentPosition[1].TCOffset;
                distanceToTrainM = ValidRoute[0].GetDistanceAlongRoute(PresentPosition[1].RouteListIndex,
                    leftInSectionM, stationIndex1, thisStation.StopOffset, true, signalRef) - Length;
            }

            // If beyond station and train is stopped - return present position
            if (distanceToTrainM < 0f && MovementState == AI_MOVEMENT_STATE.STATION_STOP)
            {
                return new float[2] { PresentPosition[0].DistanceTravelledM, 0.0f };
            }

            // If station not on route at all return negative values
            if (distanceToTrainM < 0f && stationIndex0 < 0 && stationIndex1 < 0)
            {
                return new float[2] { -1f, -1f };
            }

            // If reschedule, use actual speed
            float activateDistanceTravelledM = PresentPosition[0].DistanceTravelledM + distanceToTrainM;
            float triggerDistanceM = 0.0f;

            if (reschedule)
            {
                float firstPartTime = 0.0f;
                float firstPartRangeM = 0.0f;
                float secndPartRangeM = 0.0f;
                float remainingRangeM = activateDistanceTravelledM - PresentPosition[0].DistanceTravelledM;

                firstPartTime = presentSpeedMpS / (0.25f * MaxDecelMpSS);
                firstPartRangeM = 0.25f * MaxDecelMpSS * (firstPartTime * firstPartTime);

                if (firstPartRangeM < remainingRangeM && SpeedMpS < TrainMaxSpeedMpS) // If distance left and not at max speed
                {
                    // Split remaining distance based on relation between acceleration and deceleration
                    secndPartRangeM = (remainingRangeM - firstPartRangeM) * (2.0f * MaxDecelMpSS) / (MaxDecelMpSS + MaxAccelMpSS);
                }

                triggerDistanceM = activateDistanceTravelledM - (firstPartRangeM + secndPartRangeM);
            }
            else // Use maximum speed
            {
                float deltaTime = TrainMaxSpeedMpS / MaxDecelMpSS;
                float brakingDistanceM = (TrainMaxSpeedMpS * deltaTime) + (0.5f * MaxDecelMpSS * deltaTime * deltaTime);
                triggerDistanceM = activateDistanceTravelledM - brakingDistanceM;
            }

            float[] distancesM = new float[2];
            distancesM[0] = activateDistanceTravelledM;
            distancesM[1] = triggerDistanceM;

            return distancesM;
        }

        //================================================================================================//
        /// <summary>
        /// Override Switch to Signal control
        /// </summary
        public override void SwitchToSignalControl(SignalObject thisSignal)
        {
            base.SwitchToSignalControl(thisSignal);
            if (TrainType != TRAINTYPE.PLAYER)
            {
                if (!((this is AITrain) && this.MovementState == AI_MOVEMENT_STATE.SUSPENDED))
                {
                    ResetActions(true);

                    // Check if any actions must be processed immediately

                    ObtainRequiredActions(0);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Override Switch to Node control
        /// </summary>
        public override void SwitchToNodeControl(int thisSectionIndex)
        {
            base.SwitchToNodeControl(thisSectionIndex);
            if (TrainType != TRAINTYPE.PLAYER)
            {
                if (!((this is AITrain) && this.MovementState == AI_MOVEMENT_STATE.SUSPENDED))
                {
                    ResetActions(true);

                    // Check if any actions must be processed immediately
                    ObtainRequiredActions(0);
                }
            }
        }

        public override void UpdateNodeMode()
        {
            // Update node mode
            END_AUTHORITY oldAuthority = EndAuthorityType[0];
            base.UpdateNodeMode();

            // If authoriy type changed, reset actions
            if (EndAuthorityType[0] != oldAuthority)
            {
                ResetActions(true, false);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update AI Static state
        /// </summary>
        /// <param name="presentTime"></param>
        public override void UpdateAIStaticState(int presentTime)
        {
            // Start if start time is reached
            if (StartTime.HasValue && StartTime.Value < presentTime && TrainHasPower())
            {
                foreach (var car in Cars)
                {
                    if (car is MSTSLocomotive)
                    {
                        MSTSLocomotive loco = car as MSTSLocomotive;
                        loco.SetPower(true);
                    }
                }
                PowerState = true;

                PostInit();
                return;
            }

            // Switch off power for all engines
            if (PowerState)
            {
                foreach (var car in Cars)
                {
                    if (car is MSTSLocomotive)
                    {
                        MSTSLocomotive loco = car as MSTSLocomotive;
                        loco.SetPower(false);
                    }
                }
                PowerState = false;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update train in stopped state
        /// </summary>
        public virtual AITrain.AI_MOVEMENT_STATE UpdateStoppedState(float elapsedClockSeconds)
        {
            var AuxActionnextActionInfo = nextActionInfo;
            var tryBraking = true;

            // TODO: Can we merge these two conditions?
            if (SpeedMpS > 0) // If train still running force it to stop
            {
                SpeedMpS = 0;
                Update(0); // Stop the wheels from moving etc
                AITrainThrottlePercent = 0;
                AITrainBrakePercent = 100;

            }

            if (SpeedMpS < 0) // If train still running force it to stop
            {
                SpeedMpS = 0;
                Update(0); // Stop the wheels from moving etc
                AITrainThrottlePercent = 0;
                AITrainBrakePercent = 100;
            }

            // Check if there's a train ahead - if so, determine speed and distance
            if (ControlMode == TRAIN_CONTROL.AUTO_NODE && EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
            {
                // Check if train ahead is in same section
                int sectionIndex = PresentPosition[0].TCSectionIndex;
                int startIndex = ValidRoute[0].GetRouteIndex(sectionIndex, 0);
                int endIndex = ValidRoute[0].GetRouteIndex(LastReservedSection[0], 0);

                TrackCircuitSection thisSection = signalRef.TrackCircuitList[sectionIndex];

                Dictionary<Train, float> trainInfo = thisSection.TestTrainAhead(this, PresentPosition[0].TCOffset, PresentPosition[0].TCDirection);

                // Search for train ahead in route sections
                for (int iIndex = startIndex + 1; iIndex <= endIndex && trainInfo.Count <= 0; iIndex++)
                {
                    thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                    trainInfo = thisSection.TestTrainAhead(this, 0.0f, ValidRoute[0][iIndex].Direction);
                }

                if (trainInfo.Count <= 0)
                {
                    // Train is in section beyond last reserved
                    if (endIndex < ValidRoute[0].Count - 1)
                    {
                        thisSection = signalRef.TrackCircuitList[ValidRoute[0][endIndex + 1].TCSectionIndex];
                        trainInfo = thisSection.TestTrainAhead(this, 0.0f, ValidRoute[0][endIndex + 1].Direction);
                    }
                }

                if (trainInfo.Count > 0) // Found train
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo) // Always just one
                    {
                        Train OtherTrain = trainAhead.Key;
                        if (Math.Abs(OtherTrain.SpeedMpS) < 0.001f &&
                                    (DistanceToEndNodeAuthorityM[0] > followDistanceStatTrainM || UncondAttach || OtherTrain.TrainType == TRAINTYPE.STATIC ||
                                    OtherTrain.PresentPosition[0].TCSectionIndex ==
                                    TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count - 1].TCSectionIndex
                                    || OtherTrain.PresentPosition[1].TCSectionIndex ==
                                    TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count - 1].TCSectionIndex))
                        {
                            // Allow creeping closer
                            CreateTrainAction(creepSpeedMpS, 0.0f, DistanceToEndNodeAuthorityM[0], null, AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD);
                            MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                            StartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);
                        }

                        else if (Math.Abs(OtherTrain.SpeedMpS) > 0 &&
                            DistanceToEndNodeAuthorityM[0] > keepDistanceMovingTrainM)
                        {
                            // Train started moving
                            MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                            StartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);
                        }
                    }
                }
                // If train not found, do nothing - state will change next update
            }

            // Other node mode: check distance ahead (path may have cleared)
            else if (ControlMode == TRAIN_CONTROL.AUTO_NODE && EndAuthorityType[0] != END_AUTHORITY.RESERVED_SWITCH &&
                        DistanceToEndNodeAuthorityM[0] > activityClearingDistanceM)
            {
                NextStopDistanceM = DistanceToEndNodeAuthorityM[0];
                StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
            }

            else if (ControlMode == TRAIN_CONTROL.AUTO_NODE && EndAuthorityType[0] == END_AUTHORITY.RESERVED_SWITCH &&
                        DistanceToEndNodeAuthorityM[0] > (2.0f * junctionOverlapM) + activityClearingDistanceM)
            {
                NextStopDistanceM = DistanceToEndNodeAuthorityM[0] - (2.0f * junctionOverlapM);
                StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
            }

            // Signal node: check state of signal
            else if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
            {
                MstsSignalAspect nextAspect = MstsSignalAspect.UNKNOWN;
                bool nextPermission = false;
                SignalObject nextSignal = null;

                // There is a next item and it is the next signal
                if (nextActionInfo != null && nextActionInfo.ActiveItem != null &&
                    nextActionInfo.ActiveItem.ObjectDetails == NextSignalObject[0])
                {
                    nextSignal = nextActionInfo.ActiveItem.ObjectDetails;
                    nextAspect = nextSignal.this_sig_lr(MstsSignalFunction.NORMAL);
                }
                else
                {
                    nextAspect = GetNextSignalAspect(0);
                    if (NextSignalObject[0] != null) nextSignal = NextSignalObject[0];
                }
                nextPermission = nextSignal != null && nextSignal.hasPermission == SignalObject.Permission.Granted;

                if (NextSignalObject[0] == null) // No signal ahead so switch Node control
                {
                    SwitchToNodeControl(PresentPosition[0].TCSectionIndex);
                    NextStopDistanceM = DistanceToEndNodeAuthorityM[0];
                }
                // TODO: Can we merge these two else if clauses?
                else if ((nextAspect > MstsSignalAspect.STOP || nextPermission) &&
                        nextAspect < MstsSignalAspect.APPROACH_1)
                {
                    // Check if any other signals within clearing distance
                    bool signalCleared = true;
                    bool withinDistance = true;

                    for (int iitem = 0; iitem <= SignalObjectItems.Count - 1 && withinDistance && signalCleared; iitem++)
                    {
                        ObjectItemInfo nextObject = SignalObjectItems[iitem];
                        if (nextObject.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
                        {
                            if (nextObject.ObjectDetails != NextSignalObject[0]) // Not signal we are waiting for
                            {
                                if (nextObject.distance_to_train > 2.0 * clearingDistanceM)
                                {
                                    withinDistance = false; // Signal is far enough ahead
                                }
                                else if (nextObject.signal_state == MstsSignalAspect.STOP)
                                {
                                    signalCleared = false; // Signal is not clear
                                    NextSignalObject[0].ForcePropagation = true;
                                }
                            }
                        }
                    }

                    if (signalCleared)
                    {
                        ResetActions(true);
                        NextStopDistanceM = 5000f; // Clear to 5000m, will be updated if required
                        StartMoving(AI_START_MOVEMENT.SIGNAL_RESTRICTED);
                    }
                }
                else if (nextAspect >= MstsSignalAspect.APPROACH_1)
                {
                    // Check if any other signals within clearing distance
                    bool signalCleared = true;
                    bool withinDistance = true;

                    for (int iitem = 0; iitem <= SignalObjectItems.Count - 1 && withinDistance && signalCleared; iitem++)
                    {
                        ObjectItemInfo nextObject = SignalObjectItems[iitem];
                        if (nextObject.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
                        {
                            if (nextObject.ObjectDetails != NextSignalObject[0]) // Not signal we are waiting for
                            {
                                if (nextObject.distance_to_train > 2.0 * clearingDistanceM)
                                {
                                    withinDistance = false; // Signal is far enough ahead
                                }
                                else if (nextObject.signal_state == MstsSignalAspect.STOP)
                                {
                                    signalCleared = false; // Signal is not clear
                                    NextSignalObject[0].ForcePropagation = true;
                                }
                            }
                        }
                    }

                    if (signalCleared)
                    {
                        ResetActions(true);
                        NextStopDistanceM = 5000f; // Clear to 5000m, will be updated if required
                        StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
                    }
                }

                else if (nextAspect == MstsSignalAspect.STOP)
                {
                    // If stop but train is well away from signal allow to close; also if at end of path.
                    if ((DistanceToSignal.HasValue && DistanceToSignal.Value > 5 * signalApproachDistanceM) ||
                        (TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count - 1 == PresentPosition[0].RouteListIndex))
                    {
                        MovementState = AI_MOVEMENT_STATE.ACCELERATING;
                        StartMoving(AI_START_MOVEMENT.PATH_ACTION);
                    }
                    else tryBraking = false;
                    // TODO: Can we remove this?
                    // else if (IsActualPlayerTrain && NextSignalObject[0].hasPermission == SignalObject.Permission.Granted)
                    // {
                    //     MovementState = AI_MOVEMENT_STATE.ACCELERATING;
                    //     StartMoving(AI_START_MOVEMENT.PATH_ACTION);
                    // }
                }

                else if (nextActionInfo != null &&
                 nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                {
                    if (StationStops[0].SubrouteIndex == TCRoute.activeSubpath &&
                       ValidRoute[0].GetRouteIndex(StationStops[0].TCSectionIndex, PresentPosition[0].RouteListIndex) <= PresentPosition[0].RouteListIndex)
                    {
                        // Assume to be in station
                        MovementState = AI_MOVEMENT_STATE.STATION_STOP;

                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " assumed to be in station : " +
                                StationStops[0].PlatformItem.Name + "( present section = " + PresentPosition[0].TCSectionIndex +
                                " ; station section = " + StationStops[0].TCSectionIndex + " )\n");
                        }
                    }
                    else
                    {
                        // Approaching next station
                        MovementState = AI_MOVEMENT_STATE.BRAKING;

                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " departing from station stop to next stop : " +
                                StationStops[0].PlatformItem.Name + "( next section = " + PresentPosition[0].TCSectionIndex +
                                " ; station section = " + StationStops[0].TCSectionIndex + " )\n");
                        }
                    }
                }
                else if (nextActionInfo != null &&
                    nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.AUX_ACTION)
                {
                    MovementState = AI_MOVEMENT_STATE.BRAKING;
                }
                else if (nextActionInfo == null || nextActionInfo.NextAction != AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                {
                    if (nextAspect != MstsSignalAspect.STOP)
                    {
                        MovementState = AI_MOVEMENT_STATE.RUNNING;
                        StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
                    }
                    else
                    {
                        //<CSComment: Without this train would not start moving if there is a stop signal in front
                        if (NextSignalObject[0] != null)
                        {
                            var distanceSignaltoTrain = NextSignalObject[0].DistanceTo(FrontTDBTraveller);
                            float distanceToReversalPoint = 10000000f;
                            if (TCRoute.ReversalInfo[TCRoute.activeSubpath] != null && TCRoute.ReversalInfo[TCRoute.activeSubpath].Valid)
                            {
                                distanceToReversalPoint = ComputeDistanceToReversalPoint();
                            }
                            if (distanceSignaltoTrain >= 100.0f || (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.REVERSAL
                                && nextActionInfo.ActivateDistanceM - DistanceTravelledM > 10) ||
                                distanceSignaltoTrain > distanceToReversalPoint)
                            {
                                MovementState = AI_MOVEMENT_STATE.BRAKING;
                                //>CSComment: Better be sure the train will stop in front of signal
                                CreateTrainAction(0.0f, 0.0f, distanceSignaltoTrain, SignalObjectItems[0], AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP);
                                Alpha10 = PreUpdate ? 2 : 10;
                                AITrainThrottlePercent = 25;
                                AdjustControlsBrakeOff();
                            }
                        }
                    }

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                                Number.ToString() + " , forced to BRAKING from invalid stop (now at " +
                                PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
#endif

                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " , forced to BRAKING from invalid stop (now at " +
                                PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
                    }
                }
            }
            float distanceToNextSignal = DistanceToSignal ?? 0.1f;
            if (AuxActionnextActionInfo != null && MovementState == AI_MOVEMENT_STATE.STOPPED && tryBraking && distanceToNextSignal > clearingDistanceM
                && EndAuthorityType[0] != END_AUTHORITY.RESERVED_SWITCH && DistanceToEndNodeAuthorityM[0] <= 2.0f * junctionOverlapM)   // && ControlMode == TRAIN_CONTROL.AUTO_NODE)
            {
                MovementState = AI_MOVEMENT_STATE.BRAKING;
            }
            return MovementState;
        }

        //================================================================================================//
        /// <summary>
        /// Train is on turntable
        /// Dummy method for child instancing
        /// </summary>
        public virtual void UpdateTurntableState(float elapsedTimeSeconds, int presentTime)
        { }

        //================================================================================================//
        /// <summary>
        /// Train is at station
        /// </summary>
        public virtual void UpdateStationState(float elapsedClockSeconds, int presentTime)
        {
            StationStop thisStation = StationStops[0];
            bool removeStation = true;

            int eightHundredHours = 8 * 3600;
            int sixteenHundredHours = 16 * 3600;
            int actualdepart = thisStation.ActualDepart;

            // No arrival / departure time set: update times
            if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
            {
                AtStation = true;

                if (thisStation.ActualArrival < 0)
                {
                    thisStation.ActualArrival = presentTime;
                    var stopTime = thisStation.CalculateDepartTime(presentTime, this);
                    actualdepart = thisStation.ActualDepart;
                    DoorOpenTimer = PreUpdate ? 0 : 4;
                    DoorCloseTimer = PreUpdate ? stopTime - 20 : stopTime - 10.0f;
                    if (DoorCloseTimer - 6 < DoorOpenTimer)
                    {
                        DoorOpenTimer = 0;
                        DoorCloseTimer = Math.Max (stopTime - 3, 0);
                    }

#if DEBUG_REPORTS
                    DateTime baseDT = new DateTime();
                    DateTime arrTime = baseDT.AddSeconds(presentTime);

                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                         Number.ToString() + " arrives station " +
                         StationStops[0].PlatformItem.Name + " at " +
                         arrTime.ToString("HH:mm:ss") + "\n");
#endif
                    if (CheckTrain)
                    {
                        DateTime baseDTCT = new DateTime();
                        DateTime arrTimeCT = baseDTCT.AddSeconds(presentTime);
                        DateTime depTimeCT = baseDTCT.AddSeconds(thisStation.ActualDepart);

                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                             Number.ToString() + " arrives station " +
                             StationStops[0].PlatformItem.Name + " at " +
                             arrTimeCT.ToString("HH:mm:ss") + " ; dep. at " +
                             depTimeCT.ToString("HH:mm:ss") + "\n");
                    }
                }
                else
                {
                    if (!IsFreight && Simulator.OpenDoorsInAITrains)
                    {
                        var frontIsFront = thisStation.PlatformReference == thisStation.PlatformItem.PlatformFrontUiD;
                        if (DoorOpenTimer >= 0)
                        {
                            DoorOpenTimer -= elapsedClockSeconds;
                            if (DoorOpenTimer < 0)
                            {
                                if (thisStation.PlatformItem.PlatformSide[0])
                                {
                                    // Open left doors
                                    SetDoors(frontIsFront ? DoorSide.Right : DoorSide.Left, true);
                                }
                                if (thisStation.PlatformItem.PlatformSide[1])
                                {
                                    // Open right doors
                                    SetDoors(frontIsFront ? DoorSide.Left : DoorSide.Right, true);
                                }
                            }
                        }
                        if (DoorCloseTimer >= 0)
                        {
                            DoorCloseTimer -= elapsedClockSeconds;
                            if (DoorCloseTimer < 0)
                            {
                                if (thisStation.PlatformItem.PlatformSide[0])
                                {
                                    // Close left doors
                                    SetDoors(frontIsFront ? DoorSide.Right : DoorSide.Left, false);
                                }
                                if (thisStation.PlatformItem.PlatformSide[1])
                                {
                                    // Close right doors
                                    SetDoors(frontIsFront ? DoorSide.Left : DoorSide.Right, false);
                                }
                            }
                        }
                    }
                }
            }

            // Not yet time to depart - check if signal can be released
            int correctedTime = presentTime;

            if (actualdepart > sixteenHundredHours && presentTime < eightHundredHours) // Should have departed before midnight
            {
                correctedTime = presentTime + (24 * 3600);
            }

            if (actualdepart < eightHundredHours && presentTime > sixteenHundredHours) // To depart after midnight
            {
                correctedTime = presentTime - (24 * 3600);
            }

#if WITH_PATH_DEBUG
            if (Simulator.Settings.EnhancedActCompatibility)
            {
                currentAIStation = " ---";
                switch (thisStation.ActualStopType)
                {
                    case StationStop.STOPTYPE.MANUAL_STOP:
                        currentAIStation = " Manual stop";
                        break;
                    case StationStop.STOPTYPE.SIDING_STOP:
                        currentAIStation = " Siding stop";
                        break;
                    case StationStop.STOPTYPE.STATION_STOP:
                        currentAIStation = " Station stop";
                        break;
                    case StationStop.STOPTYPE.WAITING_POINT:
                        currentAIStation = " Waiting Point";
                        break;
                    default:
                        currentAIStation = " ---";
                        break;
                }
                currentAIStation = String.Concat(currentAIStation, " ", actualdepart.ToString(), "?", correctedTime.ToString());
            }
#endif
            if (actualdepart > correctedTime)
            {
                if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP &&
                    (actualdepart - 120 < correctedTime) &&
                     thisStation.HoldSignal)
                {
                    HoldingSignals.Remove(thisStation.ExitSignal);
                    var nextSignal = signalRef.SignalObjects[thisStation.ExitSignal];

                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                             Number.ToString() + " clearing hold signal " + nextSignal.thisRef.ToString() + " at station " +
                             StationStops[0].PlatformItem.Name + "\n");
                    }

                    if (nextSignal.enabledTrain != null && nextSignal.enabledTrain.Train == this)
                    {
                        nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null); // For AI always use direction 0
                    }

                    thisStation.HoldSignal = false;
                }
                return;
            }

            // Depart
            thisStation.Passed = true;
            if (thisStation.ArrivalTime >= 0)
            {
                Delay = TimeSpan.FromSeconds((presentTime - thisStation.DepartTime) % (24 * 3600));
            }
            PreviousStop = thisStation.CreateCopy();

            if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP
                && MaxVelocityA > 0 && ServiceDefinition != null && ServiceDefinition.ServiceList.Count > 0 && this != Simulator.Trains[0])
            {
                // <CScomment> Recalculate TrainMaxSpeedMpS and AllowedMaxSpeedMpS
                var actualServiceItemIdx = ServiceDefinition.ServiceList.FindIndex(si => si.PlatformStartID == thisStation.PlatformReference);
                if (actualServiceItemIdx >= 0 && ServiceDefinition.ServiceList.Count >= actualServiceItemIdx + 2)
                {
                    var sectionEfficiency = ServiceDefinition.ServiceList[actualServiceItemIdx + 1].Efficiency;
                    if (Simulator.Settings.ActRandomizationLevel > 0) RandomizeEfficiency(ref sectionEfficiency);
                    if (sectionEfficiency > 0)
                    {
                        TrainMaxSpeedMpS = Math.Min((float)Simulator.TRK.Tr_RouteFile.SpeedLimit, MaxVelocityA * sectionEfficiency);
                        RecalculateAllowedMaxSpeed();
                    }
                }
                else if (MaxVelocityA > 0 && Efficiency > 0)
                {
                    TrainMaxSpeedMpS = Math.Min((float)Simulator.TRK.Tr_RouteFile.SpeedLimit, MaxVelocityA * Efficiency);
                    RecalculateAllowedMaxSpeed();
                }
            }

            // First, check state of signal
            if (thisStation.ExitSignal >= 0 && (thisStation.HoldSignal || signalRef.SignalObjects[thisStation.ExitSignal].holdState == HoldState.StationStop))
            {
                if (HoldingSignals.Contains(thisStation.ExitSignal)) HoldingSignals.Remove(thisStation.ExitSignal);
                var nextSignal = signalRef.SignalObjects[thisStation.ExitSignal];

                // Only request signal if in signal mode (train may be in node control)
                if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
                {
                    nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null); // For AI always use direction 0
                }
            }

            // Check if station is end of path
            bool[] endOfPath = ProcessEndOfPath(presentTime, false);

            if (endOfPath[0])
            {
                removeStation = false; // Do not remove station from list - is done by path processing
            }
            // Check if station has exit signal and this signal is at danger
            else if (thisStation.ExitSignal >= 0 && NextSignalObject[0] != null && NextSignalObject[0].thisRef == thisStation.ExitSignal)
            {
                MstsSignalAspect nextAspect = GetNextSignalAspect(0);
                if (nextAspect == MstsSignalAspect.STOP && !NextSignalObject[0].HasLockForTrain(Number, TCRoute.activeSubpath) &&
                    !(TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count - 1 == PresentPosition[0].RouteListIndex &&
                        TCRoute.TCRouteSubpaths.Count - 1 == TCRoute.activeSubpath))
                {
                    return; // Do not depart if exit signal at danger
                }
            }

            // Change state if train still exists
            if (endOfPath[1])
            {
                if (MovementState == AI_MOVEMENT_STATE.STATION_STOP)
                {
                    // If state is still station_stop and ready to depart - change to stop to check action
                    MovementState = AI_MOVEMENT_STATE.STOPPED_EXISTING;
                    if (TrainType != TRAINTYPE.AI_PLAYERHOSTING) AtStation = false;
                }
                Delay = TimeSpan.FromSeconds((presentTime - thisStation.DepartTime) % (24 * 3600));
            }
            if (Cars[0] is MSTSLocomotive) Cars[0].SignalEvent(Event.AITrainLeavingStation);

#if DEBUG_REPORTS
            DateTime baseDTd = new DateTime();
            DateTime depTime = baseDTd.AddSeconds(presentTime);

            if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " departs station " +
                            StationStops[0].PlatformItem.Name + " at " +
                            depTime.ToString("HH:mm:ss") + "\n");
            }
            else if (thisStation.ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " departs waiting point at " +
                            depTime.ToString("HH:mm:ss") + "\n");
            }

            if (thisStation.ExitSignal >= 0)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Exit signal : " + thisStation.ExitSignal.ToString() + "\n");
                File.AppendAllText(@"C:\temp\printproc.txt", "Holding signals : \n");
                foreach (int thisSignal in HoldingSignals)
                {
                    File.AppendAllText(@"C:\temp\printproc.txt", "Signal : " + thisSignal.ToString() + "\n");
                }
                File.AppendAllText(@"C:\temp\printproc.txt", "\n");
            }
#endif

            if (CheckTrain)
            {
                DateTime baseDTdCT = new DateTime();
                DateTime depTimeCT = baseDTdCT.AddSeconds(presentTime);

                if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " departs station " +
                                thisStation.PlatformItem.Name + " at " +
                                depTimeCT.ToString("HH:mm:ss") + "\n");
                }
                else if (thisStation.ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " departs waiting point at " +
                                depTimeCT.ToString("HH:mm:ss") + "\n");
                }

                if (thisStation.ExitSignal >= 0)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Exit signal : " + thisStation.ExitSignal.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Holding signals : \n");
                    foreach (int thisSignal in HoldingSignals)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Signal : " + thisSignal.ToString() + "\n");
                    }
                    File.AppendAllText(@"C:\temp\checktrain.txt", "\n");
                }
            }

            if (StationStops.Count > 0) PreviousStop = StationStops[0].CreateCopy();
            if (removeStation)
                StationStops.RemoveAt(0);
            ResetActions(true);
        }

        //================================================================================================//
        /// <summary>
        /// Train is braking
        /// </summary>
        public virtual void UpdateBrakingState(float elapsedClockSeconds, int presentTime)
        {

            // Check if action still required
            bool clearAction = false;

            float distanceToGoM = activityClearingDistanceM;
            if (nextActionInfo != null && nextActionInfo.RequiredSpeedMpS == 99999f) // RequiredSpeed doesn't matter
            {
                return;
            }

            if (nextActionInfo == null) // Action has been reset - keep status quo
            {
                if (ControlMode == TRAIN_CONTROL.AUTO_NODE)  // Node control: use control distance
                {
                    distanceToGoM = DistanceToEndNodeAuthorityM[0];

                    if (EndAuthorityType[0] == END_AUTHORITY.RESERVED_SWITCH)
                    {
                        distanceToGoM = DistanceToEndNodeAuthorityM[0] - (2.0f * junctionOverlapM);
                    }
                    else if (EndAuthorityType[0] == END_AUTHORITY.END_OF_PATH)
                    {
                        distanceToGoM = DistanceToEndNodeAuthorityM[0] - activityClearingDistanceM;
                    }

                    if (distanceToGoM <= 0)
                    {
                        if (SpeedMpS > 0)
                        {
                            if (CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt",
                                                        "Brake mode - auto node - passed distance moving - set brakes\n");
                            }
                            AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);
                        }
                    }

                    if (distanceToGoM < activityClearingDistanceM && SpeedMpS <= 0)
                    {
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                    "Brake mode - auto node - passed distance stopped - to stop state\n");
                        }
                        MovementState = AI_MOVEMENT_STATE.STOPPED;
                        return;
                    }
                }
                else // Action cleared - set running or stopped
                {
                    if (SpeedMpS > 0)
                    {
                        MovementState = AI_MOVEMENT_STATE.RUNNING;
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                    "Brake mode - auto node - action clear while moving - to running state\n");
                        }
                    }
                    else
                    {
                        MovementState = AI_MOVEMENT_STATE.STOPPED;
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                    "Brake mode - auto node - action cleared while stopped - to stop state\n");
                        }
                    }
                    return;
                }

            }

            // Check if speed limit on signal is cleared
            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SPEED_SIGNAL)
            {
                if (nextActionInfo.ActiveItem.actual_speed >= AllowedMaxSpeedMpS)
                {
                    clearAction = true;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                          Number.ToString() + " : signal " +
                          nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " : speed : " +
                          FormatStrings.FormatSpeed(nextActionInfo.ActiveItem.actual_speed, true) + " >= limit : " +
                          FormatStrings.FormatSpeed(AllowedMaxSpeedMpS, true) + " at " +
                          nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                          PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : signal " +
                              nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " : speed : " +
                              FormatStrings.FormatSpeed(nextActionInfo.ActiveItem.actual_speed, true) + " >= limit : " +
                              FormatStrings.FormatSpeed(AllowedMaxSpeedMpS, true) + " at " +
                              nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                              PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
                else if (nextActionInfo.ActiveItem.actual_speed < 0)
                {
                    clearAction = true;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                          Number.ToString() + " : signal " +
                          nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " : speed : " +
                          FormatStrings.FormatSpeed(nextActionInfo.ActiveItem.actual_speed, true) + " cleared at " +
                          nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                          PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : signal " +
                              nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " : speed : " +
                              FormatStrings.FormatSpeed(nextActionInfo.ActiveItem.actual_speed, true) + " cleared at " +
                              nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                              PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
            }

            // Check if STOP signal cleared
            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
            {
                var nextSignal = nextActionInfo.ActiveItem.ObjectDetails;
                var nextPermission = nextSignal.hasPermission == SignalObject.Permission.Granted;
                if (nextActionInfo.ActiveItem.signal_state >= MstsSignalAspect.APPROACH_1 || nextPermission)
                {
                    clearAction = true;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                          Number.ToString() + " : signal " +
                          nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                          nextActionInfo.ActivateDistanceM.ToString() + " cleared (now at " +
                          PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : signal " +
                              nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                              nextActionInfo.ActivateDistanceM.ToString() + " cleared (now at " +
                              PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
                else if (nextActionInfo.ActiveItem.signal_state != MstsSignalAspect.STOP)
                {
                    nextActionInfo.NextAction = AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED;
                    if (((nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM) ||
                         nextActionInfo.ActiveItem.ObjectDetails.this_sig_noSpeedReduction(SignalFunction.NORMAL))
                    {
                        clearAction = true;
#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt",
                          Number.ToString() + " : signal " +
                          nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                          nextActionInfo.ActivateDistanceM.ToString() + " cleared (now at " +
                          PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                              Number.ToString() + " : signal " +
                              nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                              nextActionInfo.ActivateDistanceM.ToString() + " cleared (now at " +
                              PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                        }
                    }
                }
            }

            // Check if RESTRICTED signal cleared
            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED)
            {
                if ((nextActionInfo.ActiveItem.signal_state >= MstsSignalAspect.APPROACH_1) ||
                   ((nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM) ||
                   nextActionInfo.ActiveItem.ObjectDetails.this_sig_noSpeedReduction(SignalFunction.NORMAL))
                {
                    clearAction = true;
#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt",
                      Number.ToString() + " : signal " +
                      nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                      nextActionInfo.ActivateDistanceM.ToString() + " cleared (now at " +
                      PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                      FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                          Number.ToString() + " : signal " +
                          nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                          nextActionInfo.ActivateDistanceM.ToString() + " cleared (now at " +
                          PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
            }

            // Check if END_AUTHORITY extended
            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY)
            {
                nextActionInfo.ActivateDistanceM = DistanceToEndNodeAuthorityM[0] + DistanceTravelledM;
                if (EndAuthorityType[0] == END_AUTHORITY.MAX_DISTANCE)
                {
                    clearAction = true;
                }
                else if (EndAuthorityType[0] == END_AUTHORITY.RESERVED_SWITCH)
                {
                    nextActionInfo.ActivateDistanceM -= 2.0f * junctionOverlapM;
                }
            }

            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SPEED_LIMIT)
            {
                if (nextActionInfo.RequiredSpeedMpS >= AllowedMaxSpeedMpS)
                {
                    clearAction = true;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                          Number.ToString() + " : speed : " +
                          FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " >= limit : " +
                          FormatStrings.FormatSpeed(AllowedMaxSpeedMpS, true) + " at " +
                          nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                          PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : speed : " +
                              FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " >= limit : " +
                              FormatStrings.FormatSpeed(AllowedMaxSpeedMpS, true) + " at " +
                              nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                              PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }

                }
                else if (nextActionInfo.ActiveItem.actual_speed != nextActionInfo.RequiredSpeedMpS)
                {
                    clearAction = true;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                          Number.ToString() + " : speed : " +
                          FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " changed to : " +
                          FormatStrings.FormatSpeed(nextActionInfo.ActiveItem.actual_speed, true) + " at " +
                          nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                          PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : speed : " +
                              FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " changed to : " +
                              FormatStrings.FormatSpeed(nextActionInfo.ActiveItem.actual_speed, true) + " at " +
                              nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                              PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
            }

            // Action cleared - reset processed info for object items to determine next action
            // Clear list of pending action to create new list
            if (clearAction)
            {
                ResetActions(true);
                MovementState = AI_MOVEMENT_STATE.RUNNING;
                Alpha10 = PreUpdate ? 2 : 10;
                if (SpeedMpS < AllowedMaxSpeedMpS - (3.0f * hysterisMpS))
                {
                    AdjustControlsBrakeOff();
                }
                return;
            }

            // Calculate ideal speed
            float requiredSpeedMpS = 0;
            float creepDistanceM = 3.0f * signalApproachDistanceM;

            if (nextActionInfo != null)
            {
                requiredSpeedMpS = nextActionInfo.RequiredSpeedMpS;
                distanceToGoM = nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM;

                if (nextActionInfo.ActiveItem != null)
                {
                    if (Cars != null && Cars.Count < 10)
                    {
                        distanceToGoM = nextActionInfo.ActiveItem.distance_to_train - (signalApproachDistanceM / 4);
                        if (PreUpdate) distanceToGoM -= signalApproachDistanceM * 0.25f;
                        // Be more conservative if braking downhill
                        /* else if (FirstCar != null)
                        {
                            var Elevation = FirstCar.CurrentElevationPercent;
                            if (FirstCar.Flipped ^ (FirstCar.IsDriveable && FirstCar.Train.IsPlayerDriven && ((MSTSLocomotive)FirstCar).UsingRearCab)) Elevation = -Elevation;
                            if (FirstCar.CurrentElevationPercent < -2.0) distanceToGoM -= signalApproachDistanceM;
                        }*/
                    }
                    else distanceToGoM = nextActionInfo.ActiveItem.distance_to_train - signalApproachDistanceM;
                    // distanceToGoM = nextActionInfo.ActiveItem.distance_to_train - signalApproachDistanceM;
                }

                // Check if stopped at station
                if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                {
                    NextStopDistanceM = distanceToGoM;
                    if (distanceToGoM <= 0.1f)
                    {
                        AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 100);
                        AITrainThrottlePercent = 0;

                        // Train is stopped - set departure time
                        if (Math.Abs(SpeedMpS) <= Simulator.MaxStoppedMpS)
                        {
                            MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                            StationStop thisStation = StationStops[0];

                            if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                            {
#if DEBUG_REPORTS
                                DateTime baseDT = new DateTime();
                                DateTime arrTime = baseDT.AddSeconds(presentTime);

                                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                                     Number.ToString() + " arrives station " +
                                     StationStops[0].PlatformItem.Name + " at " +
                                     arrTime.ToString("HH:mm:ss") + "\n");
#endif
                                if (CheckTrain)
                                {
                                    DateTime baseDTCT = new DateTime();
                                    DateTime arrTimeCT = baseDTCT.AddSeconds(presentTime);

                                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                         Number.ToString() + " arrives station " +
                                         StationStops[0].PlatformItem.Name + " at " +
                                         arrTimeCT.ToString("HH:mm:ss") + "\n");
                                }
                            }
                            else if (thisStation.ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                            {
                                thisStation.ActualArrival = presentTime;

                                if (thisStation.DepartTime < 0)
                                {
                                    // Delta time set
                                    thisStation.ActualDepart = presentTime - thisStation.DepartTime; // Depart time is negative!
                                }
                                else
                                {
                                    // Actual time set
                                    thisStation.ActualDepart = thisStation.DepartTime;
                                }

#if DEBUG_REPORTS
                                DateTime baseDT = new DateTime();
                                DateTime arrTime = baseDT.AddSeconds(presentTime);

                                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                                     Number.ToString() + " arrives waiting point at " +
                                     arrTime.ToString("HH:mm:ss") + "\n");
#endif
                                if (CheckTrain)
                                {
                                    DateTime baseDTCT = new DateTime();
                                    DateTime arrTimeCT = baseDTCT.AddSeconds(presentTime);

                                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                         Number.ToString() + " arrives waiting point at " +
                                         arrTimeCT.ToString("HH:mm:ss") + "\n");
                                }
                            }
                        }
                        return;
                    }
                }
                else if (nextActionInfo.GetType().IsSubclassOf(typeof(AuxActionItem)))
                {
                    NextStopDistanceM = distanceToGoM;
                    MovementState = nextActionInfo.ProcessAction(this, presentTime, elapsedClockSeconds, MovementState);
                }

                // Check speed reduction position reached
                else if (nextActionInfo.RequiredSpeedMpS > 0)
                {
                    if (distanceToGoM <= 0.0f)
                    {
                        AdjustControlsBrakeOff();
                        AllowedMaxSpeedMpS = nextActionInfo.RequiredSpeedMpS;
                        MovementState = AI_MOVEMENT_STATE.RUNNING;
                        Alpha10 = PreUpdate ? 2 : 10;
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                    "Speed limit reached : " +
                                           "Speed : " + FormatStrings.FormatSpeed(SpeedMpS, true) +
                                           " ; Reqd : " + FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + "\n");
                        }
                        ResetActions(true);
                        return;
                    }
                }

                // Check if approaching reversal point
                else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.REVERSAL)
                {
                    if (Math.Abs(SpeedMpS) < 0.03f && nextActionInfo.ActivateDistanceM - DistanceTravelledM < 10.0f)
                        MovementState = AI_MOVEMENT_STATE.STOPPED;
                }

                // Check if stopped at signal
                else if (nextActionInfo.RequiredSpeedMpS == 0)
                {
                    NextStopDistanceM = distanceToGoM;
                    if (distanceToGoM < signalApproachDistanceM * 0.75f)
                    {
                        AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);
                        AITrainThrottlePercent = 0;
                        if (Math.Abs(SpeedMpS) <= Simulator.MaxStoppedMpS)
                        {
                            MovementState = AI_MOVEMENT_STATE.STOPPED;
                        }
                        else
                        {
                            AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 10);
                        }

                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                    "Signal Approach reached : " +
                                           "Speed : " + FormatStrings.FormatSpeed(SpeedMpS, true) + "\n");
                        }

                        // If approaching signal and at approach distance and still moving, force stop
                        if (distanceToGoM < 0 && SpeedMpS > 0 &&
                            nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                        {

#if DEBUG_EXTRAINFO
                            Trace.TraceWarning("Forced stop for signal at danger for train {0} {1} at speed {2}", Number, Name, SpeedMpS);
#endif
                            if (CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt",
                                                        "Signal forced stop : " +
                                               "Speed : " + FormatStrings.FormatSpeed(SpeedMpS, true) + "\n");
                            }

                            SpeedMpS = 0.0f;
                            foreach (TrainCar car in Cars)
                            {
                                car.SpeedMpS = SpeedMpS;
                            }
                        }

                        return;
                    }

                    if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED)
                    {
                        if (distanceToGoM < creepDistanceM)
                        {
                            requiredSpeedMpS = creepSpeedMpS;
                        }
                    }
                }
            }

            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP &&
                distanceToGoM < 150 + StationStops[0].PlatformItem.Length && !ApproachTriggerSet)
            {
                if (Cars[0] is MSTSLocomotive) Cars[0].SignalEvent(Event.AITrainApproachingStation);
                ApproachTriggerSet = true;
            }

            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                creepDistanceM = 0.0f;
            if (nextActionInfo == null && requiredSpeedMpS == 0)
                creepDistanceM = clearingDistanceM;

            // Keep speed within required speed band
            // Preset, also valid for reqSpeed > 0
            float lowestSpeedMpS = requiredSpeedMpS;
            creepDistanceM = 0.5f * signalApproachDistanceM;

            if (requiredSpeedMpS == 0)
            {
                // Station stop: use 0.5 signalApproachDistanceM as final stop approach
                if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                {
                    creepDistanceM = 0.0f;
                    lowestSpeedMpS = creepSpeedMpS;
                }
                // Signal: use 3 * signalApproachDistanceM as final stop approach to avoid signal overshoot
                if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                {
                    creepDistanceM = 3.0f * signalApproachDistanceM;
                    lowestSpeedMpS =
                        distanceToGoM < creepDistanceM ? (0.5f * creepSpeedMpS) : creepSpeedMpS;
                }
                // Otherwise use clearingDistanceM as approach distance
                else if (nextActionInfo == null && requiredSpeedMpS == 0)
                {
                    creepDistanceM = clearingDistanceM;
                    lowestSpeedMpS =
                        distanceToGoM < creepDistanceM ? (0.5f * creepSpeedMpS) : creepSpeedMpS;
                }
                else
                {
                    lowestSpeedMpS = creepSpeedMpS;
                }

            }

            lowestSpeedMpS = Math.Min(lowestSpeedMpS, AllowedMaxSpeedMpS);

            // braking distance - use 0.22 * MaxDecelMpSS as average deceleration (due to braking delay)
            // Videal - Vreq = a * T => T = (Videal - Vreq) / a
            // R = Vreq * T + 0.5 * a * T^2 => R = Vreq * (Videal - Vreq) / a + 0.5 * a * (Videal - Vreq)^2 / a^2 =>
            // R = Vreq * Videal / a - Vreq^2 / a + Videal^2 / 2a - 2 * Vreq * Videal / 2a + Vreq^2 / 2a => R = Videal^2 / 2a - Vreq^2 /2a
            // so : Videal = SQRT (2 * a * R + Vreq^2)
            // remaining distance is corrected for minimal approach distance as safety margin
            // for requiredSpeed > 0, take hysteris margin off ideal speed so speed settles on required speed
            // for requiredSpeed == 0, use ideal speed, this allows actual speed to be a little higher
            // upto creep distance : set creep speed as lowest possible speed

            float correctedDistanceToGoM = distanceToGoM - creepDistanceM;

            float maxPossSpeedMpS = lowestSpeedMpS;
            if (correctedDistanceToGoM > 0)
            {
                maxPossSpeedMpS = (float)Math.Sqrt((0.22f * MaxDecelMpSS * 2.0f * correctedDistanceToGoM) + (requiredSpeedMpS * requiredSpeedMpS));
                maxPossSpeedMpS = Math.Max(lowestSpeedMpS, maxPossSpeedMpS);
            }

            float idealSpeedMpS = requiredSpeedMpS == 0 ? Math.Min(AllowedMaxSpeedMpS - (2f * hysterisMpS), maxPossSpeedMpS) : Math.Min(AllowedMaxSpeedMpS, maxPossSpeedMpS) - (2f * hysterisMpS);
            float idealLowBandMpS = Math.Max(0.25f * lowestSpeedMpS, idealSpeedMpS - (3f * hysterisMpS));
            float ideal3LowBandMpS = Math.Max(0.5f * lowestSpeedMpS, idealSpeedMpS - (9f * hysterisMpS));
            float idealHighBandMpS = Math.Min(AllowedMaxSpeedMpS, Math.Max(lowestSpeedMpS, idealSpeedMpS) + hysterisMpS);
            float ideal3HighBandMpS = Math.Min(AllowedMaxSpeedMpS, Math.Max(lowestSpeedMpS, idealSpeedMpS) + (2f * hysterisMpS));

            float deltaSpeedMpS = SpeedMpS - requiredSpeedMpS;
            float idealDecelMpSS = Math.Max(0.5f * MaxDecelMpSS, deltaSpeedMpS * deltaSpeedMpS / (2.0f * distanceToGoM));

            float lastDecelMpSS = elapsedClockSeconds > 0 ? ((SpeedMpS - LastSpeedMpS) / elapsedClockSeconds) : idealDecelMpSS;

            float preferredBrakingDistanceM = 2 * AllowedMaxSpeedMpS / (MaxDecelMpSS * MaxDecelMpSS);

            if (distanceToGoM < 0f)
            {
                idealSpeedMpS = requiredSpeedMpS;
                idealLowBandMpS = Math.Max(0.0f, idealSpeedMpS - hysterisMpS);
                idealHighBandMpS = idealSpeedMpS;
                idealDecelMpSS = MaxDecelMpSS;
            }

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                                        "Brake calculation details : \n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     Actual: " + SpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     Allwd : " + AllowedMaxSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     MaxDec: " + MaxDecelMpSS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     MaxPos: " + maxPossSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     Reqd  : " + requiredSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     Ideal : " + idealSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     lowest: " + lowestSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     3high : " + ideal3HighBandMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     high  : " + idealHighBandMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     low   : " + idealLowBandMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     3low  : " + ideal3LowBandMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     dist  : " + distanceToGoM.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     A&B(S): " + AITrainThrottlePercent.ToString() + " - " + AITrainBrakePercent.ToString() + "\n");
            }

            // Keep speed within band 
            if (SpeedMpS > AllowedMaxSpeedMpS)
            {
                if (AITrainThrottlePercent > 0)
                {
                    AdjustControlsThrottleOff();
                }
                else
                {
                    AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 20);
                }

                // Clamp speed if still too high
                if (SpeedMpS > AllowedMaxSpeedMpS)
                {
                    AdjustControlsFixedSpeed(AllowedMaxSpeedMpS);
                    // PreUpdate doesn't use car speeds, so you need to adjust also overall train speed
                    if (PreUpdate) SpeedMpS = AllowedMaxSpeedMpS;
                }

                Alpha10 = PreUpdate ? 1 : 5;
            }
            else if (SpeedMpS > requiredSpeedMpS && distanceToGoM < 0)
            {
                AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);
            }
            else if (SpeedMpS > ideal3HighBandMpS)
            {
                if (AITrainThrottlePercent > 0)
                {
                    AdjustControlsThrottleOff();
                }
                else if (AITrainBrakePercent < 50)
                {
                    AdjustControlsBrakeMore(2.0f * MaxDecelMpSS, elapsedClockSeconds, 10);
                    Alpha10 = PreUpdate ? 1 : 5;
                }
                // If at full brake always perform application as it forces braking in case of brake failure (eg. due to wheelslip)
                else if (AITrainBrakePercent == 100)
                {
                    AdjustControlsBrakeMore(2.0f * MaxDecelMpSS, elapsedClockSeconds, 50);
                    Alpha10 = 0;
                }
                else if (lastDecelMpSS < 0.5f * idealDecelMpSS || Alpha10 <= 0)
                {
                    AdjustControlsBrakeMore(2.0f * MaxDecelMpSS, elapsedClockSeconds, 50);
                    Alpha10 = PreUpdate ? 1 : 5;
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }
            else if (SpeedMpS > idealHighBandMpS)
            {
                if (LastSpeedMpS > SpeedMpS)
                {
                    if (AITrainBrakePercent > 50)
                    {
                        AdjustControlsBrakeLess(0.3f * MaxDecelMpSS, elapsedClockSeconds, 10);
                    }
                    else if (AITrainBrakePercent > 0)
                    {
                        if (lastDecelMpSS > 1.5f * idealDecelMpSS)
                        {
                            AdjustControlsBrakeLess(0.3f * MaxDecelMpSS, elapsedClockSeconds, 2);
                        }
                        else if (Alpha10 <= 0)
                        {
                            AdjustControlsBrakeLess(0.3f * MaxDecelMpSS, elapsedClockSeconds, 2);
                            Alpha10 = PreUpdate ? 2 : 10;
                        }
                    }
                }
                else if (LastSpeedMpS < SpeedMpS)
                {
                    if (AITrainThrottlePercent > 50)
                    {
                        AdjustControlsAccelLess(MaxAccelMpSS, elapsedClockSeconds, 10);
                    }
                    else if (AITrainThrottlePercent > 20)
                    {
                        AdjustControlsAccelLess(MaxAccelMpSS, elapsedClockSeconds, 2);
                    }
                    else if (AITrainThrottlePercent > 0)
                    {
                        AdjustControlsThrottleOff();
                    }
                    else if (Alpha10 <= 0 || lastDecelMpSS < (0.5 * idealDecelMpSS))
                    {
                        AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 10);
                        Alpha10 = PreUpdate ? 2 : 10;
                    }

                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }
            else if (SpeedMpS > idealLowBandMpS)
            {
                if (LastSpeedMpS < SpeedMpS)
                {
                    if (AITrainThrottlePercent > 50)
                    {
                        AdjustControlsAccelLess(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                        Alpha10 = PreUpdate ? 1 : 5;
                    }
                }
                else
                {
                    if (AITrainBrakePercent > 50)
                    {
                        AdjustControlsBrakeLess(0.0f, elapsedClockSeconds, 20);
                    }
                    else if (AITrainBrakePercent > 0)
                    {
                        AdjustControlsBrakeOff();
                    }
                    else
                    {
                        AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                    }
                }
            }
            else if (distanceToGoM > 4 * preferredBrakingDistanceM && SpeedMpS < idealLowBandMpS)
            {
                AdjustControlsBrakeOff();
                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
            }
            else if (SpeedMpS > ideal3LowBandMpS)
            {
                if (AITrainBrakePercent > 0)
                {
                    AdjustControlsBrakeLess(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
                else if (LastSpeedMpS >= SpeedMpS)
                {
                    if (Alpha10 <= 0)
                    {
                        AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                        Alpha10 = PreUpdate ? 1 : 5;
                    }
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }
            else if (distanceToGoM > preferredBrakingDistanceM && SpeedMpS < ideal3LowBandMpS)
            {
                AdjustControlsBrakeOff();
                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
            }
            else if (SpeedMpS < requiredSpeedMpS)
            {
                AdjustControlsBrakeOff();
                if (((SpeedMpS - LastSpeedMpS) / elapsedClockSeconds) < 0.5f * MaxAccelMpSS)
                {
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
                Alpha10 = PreUpdate ? 1 : 5;
            }
            else if (requiredSpeedMpS == 0 && distanceToGoM > creepDistanceM && SpeedMpS < creepSpeedMpS)
            {
                AdjustControlsBrakeOff();
                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
            }
            else if (requiredSpeedMpS == 0 && distanceToGoM > signalApproachDistanceM && SpeedMpS < creepSpeedMpS)
            {
                AdjustControlsBrakeOff();
                AdjustControlsAccelMore(0.25f * MaxAccelMpSS, elapsedClockSeconds, 10);
            }

            // In preupdate: avoid problems with overshoot due to low update rate
            // Check if at present speed train would pass beyond end of authority
            if (PreUpdate)
            {
                if (requiredSpeedMpS == 0 && (elapsedClockSeconds * SpeedMpS) > distanceToGoM && SpeedMpS > creepSpeedMpS)
                {
                    SpeedMpS = 0.5f * SpeedMpS;
                }
            }

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                     "     A&B(E): " + AITrainThrottlePercent.ToString() + " - " + AITrainBrakePercent.ToString() + "\n");
            }
        }

        //================================================================================================//
        /// <summary>
        /// Train is accelerating
        /// </summary>
        public virtual void UpdateAccelState(float elapsedClockSeconds)
        {
            // Check speed
            if (((SpeedMpS - LastSpeedMpS) / elapsedClockSeconds) < 0.5f * MaxAccelMpSS)
            {
                int stepSize = (!PreUpdate) ? 10 : 40;
                float corrFactor = (!PreUpdate) ? 0.5f : 1.0f;
                AdjustControlsAccelMore(Efficiency * corrFactor * MaxAccelMpSS, elapsedClockSeconds, stepSize);
            }

            if (SpeedMpS > (AllowedMaxSpeedMpS - ((9.0f - (6.0f * Efficiency)) * hysterisMpS)))
            {
                AdjustControlsAccelLess(0.0f, elapsedClockSeconds, (int)(AITrainThrottlePercent * 0.5f));
                MovementState = AI_MOVEMENT_STATE.RUNNING;
                Alpha10 = 0;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Train is following
        /// </summary>
        public virtual void UpdateFollowingState(float elapsedClockSeconds, int presentTime)
        {
            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD && nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM < -5)
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                                            "Update Train Ahead - now at : " +
                                            PresentPosition[0].TCSectionIndex.ToString() + " " +
                                            PresentPosition[0].TCOffset.ToString() +
                                            " ; speed : " + FormatStrings.FormatSpeed(SpeedMpS, true) + "\n");
                }

            if (ControlMode != TRAIN_CONTROL.AUTO_NODE || EndAuthorityType[0] != END_AUTHORITY.TRAIN_AHEAD) // Train is gone
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train ahead is cleared \n");
                }
                MovementState = AI_MOVEMENT_STATE.RUNNING;
                ResetActions(true);
            }
            else
            {
                // Check if train is in sections ahead
                Dictionary<Train, float> trainInfo = null;

                // Find other train
                int sectionIndex = ValidRoute[0][PresentPosition[0].RouteListIndex].TCSectionIndex;
                int startIndex = PresentPosition[0].RouteListIndex;
                int endSectionIndex = LastReservedSection[0];
                int endIndex = ValidRoute[0].GetRouteIndex(endSectionIndex, startIndex);

                TrackCircuitSection thisSection = signalRef.TrackCircuitList[sectionIndex];

                trainInfo = thisSection.TestTrainAhead(this, PresentPosition[0].TCOffset, PresentPosition[0].TCDirection);
                float addOffset = 0;
                if (trainInfo.Count <= 0)
                {
                    addOffset = thisSection.Length - PresentPosition[0].TCOffset;
                }
                else
                {
                    // Ensure train in section is aware of this train in same section if this is required
                    UpdateTrainOnEnteringSection(thisSection, trainInfo);
                }

                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                                        "Train count in section " + sectionIndex.ToString() + " = " + trainInfo.Count.ToString() + "\n");
                }

                // Train not in this section, try reserved sections ahead
                for (int iIndex = startIndex + 1; iIndex <= endIndex && trainInfo.Count <= 0; iIndex++)
                {
                    TrackCircuitSection nextSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                    trainInfo = nextSection.TestTrainAhead(this, 0, ValidRoute[0][iIndex].Direction);
                }

                // If train not ahead, try first section beyond last reserved
                if (trainInfo.Count <= 0 && endIndex < ValidRoute[0].Count - 1)
                {
                    TrackCircuitSection nextSection = signalRef.TrackCircuitList[ValidRoute[0][endIndex + 1].TCSectionIndex];
                    trainInfo = nextSection.TestTrainAhead(this, 0, ValidRoute[0][endIndex + 1].Direction);
                    if (trainInfo.Count <= 0)
                    {
                        addOffset += nextSection.Length;
                    }
                }

                // Train is found
                if (trainInfo.Count > 0)
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo) // Always just one
                    {
                        Train OtherTrain = trainAhead.Key;

                        float distanceToTrain = trainAhead.Value + addOffset;

                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                    "Other train : " + OtherTrain.Number.ToString() + " at : " +
                                                    OtherTrain.PresentPosition[0].TCSectionIndex.ToString() + " " +
                                                    OtherTrain.PresentPosition[0].TCOffset.ToString() +
                                                    " ; speed : " + FormatStrings.FormatSpeed(OtherTrain.SpeedMpS, true) + "\n");
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                            "DistAhd: " + DistanceToEndNodeAuthorityM[0].ToString() + "\n");
                        }

                        // Update action info with new position
                        float keepDistanceTrainM = 0f;
                        bool attachToTrain = AttachTo == OtherTrain.Number;

                        // <CScomment> Make check when this train in same section of OtherTrain or other train at less than 50m;
                        // if other train is static or other train is in last section of this train, pass to passive coupling
                        if (Math.Abs(OtherTrain.SpeedMpS) < 0.025f && distanceToTrain <= 2 * keepDistanceMovingTrainM)
                        {
                            var rearOrFront = ValidRoute[0][ValidRoute[0].Count - 1].Direction == 1 ? 0 : 1;

                            if (OtherTrain.TrainType == TRAINTYPE.STATIC || ((OtherTrain.PresentPosition[0].TCSectionIndex ==
                                TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count - 1].TCSectionIndex
                                || OtherTrain.PresentPosition[1].TCSectionIndex ==
                                TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count - 1].TCSectionIndex) &&
                                (TCRoute.ReversalInfo[TCRoute.activeSubpath].Valid || TCRoute.activeSubpath == TCRoute.TCRouteSubpaths.Count - 1))
                                || UncondAttach)
                            {
                                attachToTrain = true;
                                AttachTo = OtherTrain.Number;
                            }

                        }
                        if (Math.Abs(OtherTrain.SpeedMpS) >= 0.025f)
                        {
                            keepDistanceTrainM = keepDistanceMovingTrainM;
                        }
                        else if (!attachToTrain)
                        {
                            keepDistanceTrainM = (OtherTrain.IsFreight || IsFreight) ? keepDistanceStatTrainM_F : keepDistanceStatTrainM_P;
                        }

                        if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD)
                        {
                            NextStopDistanceM = distanceToTrain - keepDistanceTrainM;
                        }
                        else if (nextActionInfo != null)
                        {
                            float deltaDistance = nextActionInfo.ActivateDistanceM - DistanceTravelledM;
                            NextStopDistanceM = nextActionInfo.RequiredSpeedMpS > 0.0f
                                ? distanceToTrain - keepDistanceTrainM
                                : Math.Min(deltaDistance, distanceToTrain - keepDistanceTrainM);

                            if (deltaDistance < distanceToTrain) // Perform to normal braking to handle action
                            {
                                MovementState = AI_MOVEMENT_STATE.BRAKING;  // Not following the train
                                UpdateBrakingState(elapsedClockSeconds, presentTime);
                                return;
                            }
                        }

                        // Check distance and speed
                        if (Math.Abs(OtherTrain.SpeedMpS) < 0.025f)
                        {
                            float brakingDistance = SpeedMpS * SpeedMpS * 0.5f * (0.5f * MaxDecelMpSS);
                            float reqspeed = (float)Math.Sqrt(distanceToTrain * MaxDecelMpSS);

                            float maxspeed = Math.Max(reqspeed / 2, creepSpeedMpS); // Allow continue at creepspeed
                            if (distanceToTrain < keepDistanceStatTrainM_P - 2.0f && attachToTrain)
                                maxspeed = Math.Min(maxspeed, couplingSpeedMpS);
                            maxspeed = Math.Min(maxspeed, AllowedMaxSpeedMpS); // But never beyond valid speed limit

                            // Set brake or acceleration as required

                            if (SpeedMpS > maxspeed)
                            {
                                AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                            }
                            else if ((distanceToTrain - brakingDistance) > keepDistanceTrainM * 3.0f)
                            {
                                if (brakingDistance > distanceToTrain)
                                {
                                    AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                                }
                                else if (SpeedMpS < maxspeed)
                                {
                                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                                }
                            }
                            else if ((distanceToTrain - brakingDistance) > keepDistanceTrainM)
                            {
                                if (SpeedMpS > maxspeed)
                                {
                                    AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 50);
                                }
                                else if (SpeedMpS > 0.25f * maxspeed)
                                {
                                    AdjustControlsBrakeOff();
                                }
                                else
                                {
                                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                                }
                            }
                            if (OtherTrain.UncoupledFrom == this)
                            {
                                if (distanceToTrain > 5.0f)
                                {
                                    UncoupledFrom = null;
                                    OtherTrain.UncoupledFrom = null;
                                }
                                else
                                    attachToTrain = false;
                            }
                            // if (distanceToTrain < keepDistanceStatTrainM_P - 4.0f || (distanceToTrain - brakingDistance) <= keepDistanceTrainM) // Other possibility
                            if ((distanceToTrain - brakingDistance) <= keepDistanceTrainM)
                            {
                                float reqMinSpeedMpS = attachToTrain ? couplingSpeedMpS : 0;
                                bool thisTrainFront;
                                bool otherTrainFront;

                                if (attachToTrain && CheckCouplePosition(OtherTrain, out thisTrainFront, out otherTrainFront))
                                {
                                    MovementState = AI_MOVEMENT_STATE.STOPPED;
                                    CoupleAI(OtherTrain, thisTrainFront, otherTrainFront);
                                    AI.aiListChanged = true;
                                    AttachTo = -1;
                                }
                                else if ((SpeedMpS - reqMinSpeedMpS) > 0.1f)
                                {
                                    AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);

                                    // If too close, force stop or slow down if coupling
                                    if (distanceToTrain < 0.25 * keepDistanceTrainM)
                                    {
                                        foreach (TrainCar car in Cars)
                                        {
                                            // TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                                            // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                                            // car.SpeedMpS = car.Flipped ? -reqMinSpeedMpS : reqMinSpeedMpS;
                                            car.SpeedMpS = car.Flipped ^ (car.IsDriveable && car.Train.IsActualPlayerTrain && ((MSTSLocomotive)car).UsingRearCab) ? -reqMinSpeedMpS : reqMinSpeedMpS;
                                        }
                                        SpeedMpS = reqMinSpeedMpS;
                                    }
                                }
                                else if (attachToTrain)
                                {
                                    AdjustControlsBrakeOff();
                                    if (SpeedMpS < 0.2 * creepSpeedMpS)
                                    {
                                        AdjustControlsAccelMore(0.2f * MaxAccelMpSSP, 0.0f, 20);
                                    }
                                }
                                else
                                {
                                    MovementState = AI_MOVEMENT_STATE.STOPPED;

                                    // check if stopped in next station
                                    // conditions : 
                                    // next action must be station stop
                                    // next station must be in this subroute
                                    // if next train is AI and that trains state is STATION_STOP, station must be ahead of present position
                                    // else this train must be in station section

                                    bool otherTrainInStation = false;

                                    if (OtherTrain.TrainType == TRAINTYPE.AI || OtherTrain.TrainType == TRAINTYPE.AI_PLAYERHOSTING)
                                    {
                                        AITrain OtherAITrain = OtherTrain as AITrain;
                                        otherTrainInStation = OtherAITrain.MovementState == AI_MOVEMENT_STATE.STATION_STOP;
                                    }

                                    bool thisTrainInStation = nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP;
                                    if (thisTrainInStation) thisTrainInStation = StationStops[0].SubrouteIndex == TCRoute.activeSubpath;
                                    if (thisTrainInStation)
                                    {
                                        var thisStation = StationStops[0];
                                        thisTrainInStation = CheckStationPosition(thisStation.PlatformItem, thisStation.Direction, thisStation.TCSectionIndex);
                                    }

                                    if (thisTrainInStation)
                                    {
                                        MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                                        StationStop thisStation = StationStops[0];

                                        if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                                        {
#if DEBUG_REPORTS
                                            DateTime baseDT = new DateTime();
                                            DateTime arrTime = baseDT.AddSeconds(presentTime);

                                            File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                                                 Number.ToString() + " arrives station " +
                                                 StationStops[0].PlatformItem.Name + " at " +
                                                 arrTime.ToString("HH:mm:ss") + "\n");
#endif
                                            if (CheckTrain)
                                            {
                                                DateTime baseDTCT = new DateTime();
                                                DateTime arrTimeCT = baseDTCT.AddSeconds(presentTime);

                                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                                     Number.ToString() + " arrives station " +
                                                     StationStops[0].PlatformItem.Name + " at " +
                                                     arrTimeCT.ToString("HH:mm:ss") + "\n");
                                            }
                                        }
                                        else if (thisStation.ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                                        {
                                            thisStation.ActualArrival = presentTime;

                                            // Delta time set
                                            if (thisStation.DepartTime < 0)
                                            {
                                                thisStation.ActualDepart = presentTime - thisStation.DepartTime; // Depart time is negative
                                            }
                                            // Actual time set
                                            else
                                            {
                                                thisStation.ActualDepart = thisStation.DepartTime;
                                            }

                                            // If waited behind other train, move remaining track sections to next subroute if required
                                            // Scan sections in backward order
                                            TCSubpathRoute nextRoute = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath + 1];

                                            for (int iIndex = ValidRoute[0].Count - 1; iIndex > PresentPosition[0].RouteListIndex; iIndex--)
                                            {
                                                int nextSectionIndex = ValidRoute[0][iIndex].TCSectionIndex;
                                                if (nextRoute.GetRouteIndex(nextSectionIndex, 0) <= 0)
                                                {
                                                    nextRoute.Insert(0, ValidRoute[0][iIndex]);
                                                }
                                                ValidRoute[0].RemoveAt(iIndex);
                                            }

#if DEBUG_REPORTS
                                            DateTime baseDT = new DateTime();
                                            DateTime arrTime = baseDT.AddSeconds(presentTime);

                                            File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                                                Number.ToString() + " arrives waiting point at " +
                                                arrTime.ToString("HH:mm:ss") + "\n");
#endif
                                            if (CheckTrain)
                                            {
                                                DateTime baseDTCT = new DateTime();
                                                DateTime arrTimeCT = baseDTCT.AddSeconds(presentTime);

                                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                                     Number.ToString() + " arrives waiting point at " +
                                                     arrTimeCT.ToString("HH:mm:ss") + "\n");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Check whether trains are running same direction or not
                            bool runningAgainst = false;
                            if (PresentPosition[0].TCSectionIndex == OtherTrain.PresentPosition[0].TCSectionIndex &&
                                PresentPosition[0].TCDirection != OtherTrain.PresentPosition[0].TCDirection) runningAgainst = true;
                            if ((SpeedMpS > (OtherTrain.SpeedMpS + hysterisMpS) && !runningAgainst) ||
                                SpeedMpS > (maxFollowSpeedMpS + hysterisMpS) ||
                                distanceToTrain < (keepDistanceTrainM - clearingDistanceM))
                            {
                                AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                            }
                            else if (SpeedMpS < (OtherTrain.SpeedMpS - hysterisMpS) && !runningAgainst &&
                                       SpeedMpS < maxFollowSpeedMpS &&
                                       distanceToTrain > (keepDistanceTrainM + clearingDistanceM))
                            {
                                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 2);
                            }
                        }
                    }
                }

                // Train not found - keep moving, state will change next update
                else AttachTo = -1;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Train is running at required speed
        /// </summary>
        public virtual void UpdateRunningState(float elapsedClockSeconds)
        {
            float topBand = AllowedMaxSpeedMpS > creepSpeedMpS ? AllowedMaxSpeedMpS - ((1.5f - Efficiency) * hysterisMpS) : AllowedMaxSpeedMpS;
            float highBand = AllowedMaxSpeedMpS > creepSpeedMpS ? Math.Max(0.5f, AllowedMaxSpeedMpS - ((3.0f - (2.0f * Efficiency)) * hysterisMpS)) : AllowedMaxSpeedMpS;
            float lowBand = AllowedMaxSpeedMpS > creepSpeedMpS ? Math.Max(0.4f, AllowedMaxSpeedMpS - ((9.0f - (3.0f * Efficiency)) * hysterisMpS)) : AllowedMaxSpeedMpS;
            int throttleTop = 90;

            // Check speed
            if (SpeedMpS > AllowedMaxSpeedMpS)
            {
                if (AITrainThrottlePercent > 0)
                {
                    AdjustControlsThrottleOff();
                }
                else
                {
                    AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 20);
                }

                AdjustControlsFixedSpeed(AllowedMaxSpeedMpS);
                Alpha10 = PreUpdate ? 1 : 5;
            }
            else if (SpeedMpS > topBand)
            {
                if (LastSpeedMpS > SpeedMpS)
                {
                    if (AITrainBrakePercent > 0)
                    {
                        AdjustControlsBrakeLess(0.5f * MaxDecelMpSS, elapsedClockSeconds, 10);
                    }
                }
                else
                {
                    if (AITrainThrottlePercent > throttleTop)
                    {
                        AdjustControlsAccelLess(0.0f, elapsedClockSeconds, 20);
                    }
                    else if (AITrainThrottlePercent > 0)
                    {
                        if (Alpha10 <= 0)
                        {
                            AdjustControlsAccelLess(0.0f, elapsedClockSeconds, 2);
                            Alpha10 = PreUpdate ? 1 : 5;
                        }
                    }
                    else if (AITrainBrakePercent < 50)
                    {
                        AdjustControlsBrakeMore(0.0f, elapsedClockSeconds, 10);
                    }
                    else
                    {
                        AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);
                    }
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }
            else if (SpeedMpS > highBand)
            {
                if (LastSpeedMpS > SpeedMpS)
                {
                    if (AITrainBrakePercent > 50)
                    {
                        AdjustControlsBrakeLess(0.3f * MaxDecelMpSS, elapsedClockSeconds, 10);
                    }
                    else if (AITrainBrakePercent > 0)
                    {
                        AdjustControlsBrakeLess(0.3f * MaxDecelMpSS, elapsedClockSeconds, 2);
                    }
                    else if (Alpha10 <= 0)
                    {
                        AdjustControlsAccelMore(0.3f * MaxAccelMpSS, elapsedClockSeconds, 2);
                        Alpha10 = PreUpdate ? 2 : 10;
                    }
                }
                else
                {
                    if (AITrainThrottlePercent > throttleTop)
                    {
                        AdjustControlsAccelLess(0.3f * MaxAccelMpSS, elapsedClockSeconds, 20);
                    }
                    else if (Alpha10 <= 0 && AITrainThrottlePercent > 20)
                    {
                        AdjustControlsAccelLess(0.3f * MaxAccelMpSS, elapsedClockSeconds, 5);
                        Alpha10 = PreUpdate ? 2 : 10;
                    }
                    else if (Alpha10 <= 0 && AITrainThrottlePercent < 10)
                    {
                        AdjustControlsAccelMore(0.3f * MaxAccelMpSS, elapsedClockSeconds, 2);
                        Alpha10 = PreUpdate ? 2 : 10;
                    }
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }
            else if (SpeedMpS > lowBand)
            {
                if (LastSpeedMpS < SpeedMpS)
                {
                    if (AITrainThrottlePercent > throttleTop)
                    {
                        AdjustControlsAccelLess(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                    }
                }
                else
                {
                    if (AITrainBrakePercent > 50)
                    {
                        AdjustControlsBrakeLess(0.0f, elapsedClockSeconds, 20);
                    }
                    else if (AITrainBrakePercent > 0)
                    {
                        AdjustControlsBrakeOff();
                    }
                    else
                    {
                        AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                    }
                }
                Alpha10 = 0;
            }
            else
            {
                AdjustControlsBrakeOff();
                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                Alpha10 = 0;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Start Moving
        /// </summary>
        public virtual void StartMoving(AI_START_MOVEMENT reason)
        {
            // Reset brakes, set throttle
            if (reason == AI_START_MOVEMENT.FOLLOW_TRAIN)
            {
                MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                AITrainThrottlePercent = 25;
                AdjustControlsBrakeOff();
            }
            else if (ControlMode == TRAIN_CONTROL.AUTO_NODE && EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
            {
                MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                AITrainThrottlePercent = 0;
            }
            else if (reason == AI_START_MOVEMENT.NEW)
            {
                MovementState = AI_MOVEMENT_STATE.STOPPED;
                AITrainThrottlePercent = 0;
            }
            else if (nextActionInfo != null) // Train has valid action, so start in BRAKE mode
            {
                MovementState = AI_MOVEMENT_STATE.BRAKING;
                Alpha10 = PreUpdate ? 2 : 10;
                AITrainThrottlePercent = 25;
                AdjustControlsBrakeOff();
            }
            else
            {
                MovementState = AI_MOVEMENT_STATE.ACCELERATING;
                Alpha10 = PreUpdate ? 2 : 10;
                AITrainThrottlePercent = (!PreUpdate) ? 25 : 50;
                AdjustControlsBrakeOff();
            }

            SetPercentsFromTrainToTrainset();
        }

        //================================================================================================//
        /// <summary>
        /// Set correct state for train allready in section when entering occupied section
        /// </summary>
        public void UpdateTrainOnEnteringSection(TrackCircuitSection thisSection, Dictionary<Train, float> trainsInSection)
        {
            foreach (KeyValuePair<Train, float> trainAhead in trainsInSection) // Always just one
            {
                Train OtherTrain = trainAhead.Key;
                if (OtherTrain.ControlMode == TRAIN_CONTROL.AUTO_SIGNAL) // Train is still in signal mode, might need adjusting
                {
                    // Check directions of this and other train
                    int owndirection = -1;
                    int otherdirection = -1;

                    foreach (KeyValuePair<TrainRouted, int> trainToCheckInfo in thisSection.CircuitState.TrainOccupy)
                    {
                        TrainRouted trainToCheck = trainToCheckInfo.Key;

                        if (trainToCheck.Train.Number == Number) // This train
                        {
                            owndirection = trainToCheckInfo.Value;
                        }
                        else if (trainToCheck.Train.Number == OtherTrain.Number)
                        {
                            otherdirection = trainToCheckInfo.Value;
                        }
                    }

                    if (owndirection >= 0 && otherdirection >= 0) // Both trains found
                    {
                        if (owndirection != otherdirection) // Opposite directions - this train is now ahead of train in section
                        {
                            OtherTrain.SwitchToNodeControl(thisSection.Index);
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Train control routines
        /// </summary>
        public void AdjustControlsBrakeMore(float reqDecelMpSS, float timeS, int stepSize)
        {
            if (AITrainThrottlePercent > 0)
            {
                AITrainThrottlePercent = 0;
            }

            if (AITrainBrakePercent < 100)
            {
                AITrainBrakePercent += stepSize;
                if (AITrainBrakePercent > 100)
                    AITrainBrakePercent = 100;
            }
            else
            {
                float ds = timeS * reqDecelMpSS;
                SpeedMpS = Math.Max(SpeedMpS - ds, 0); // Avoid negative speeds
                foreach (TrainCar car in Cars)
                {
                    // TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                    // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                    // car.SpeedMpS = car.Flipped ? -SpeedMpS : SpeedMpS;
                    car.SpeedMpS = car.Flipped ^ (car.IsDriveable && car.Train.IsActualPlayerTrain && ((MSTSLocomotive)car).UsingRearCab) ? -SpeedMpS : SpeedMpS;
                }
            }

            SetPercentsFromTrainToTrainset();
        }

        public void AdjustControlsBrakeLess(float reqDecelMpSS, float timeS, int stepSize)
        {
            if (AITrainThrottlePercent > 0)
            {
                AITrainThrottlePercent = 0;
            }

            if (AITrainBrakePercent > 0)
            {
                AITrainBrakePercent -= stepSize;
                if (AITrainBrakePercent < 0)
                    AdjustControlsBrakeOff();
            }
            else
            {
                float ds = timeS * reqDecelMpSS;
                SpeedMpS += ds; // avoid negative speeds
                foreach (TrainCar car in Cars)
                {
                    // TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                    // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                    // car.SpeedMpS = car.Flipped ? -SpeedMpS : SpeedMpS;
                    car.SpeedMpS = car.Flipped ^ (car.IsDriveable && car.Train.IsActualPlayerTrain && ((MSTSLocomotive)car).UsingRearCab) ? -SpeedMpS : SpeedMpS;
                }
            }

            SetPercentsFromTrainToTrainset();
        }

        public void AdjustControlsBrakeOff()
        {
            AITrainBrakePercent = 0;
            InitializeBrakes();

            if (FirstCar != null)
            {
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
                if (TrainType == TRAINTYPE.AI_PLAYERHOSTING || Autopilot)
                {
                    if (FirstCar is MSTSLocomotive)
                        ((MSTSLocomotive)FirstCar).SetTrainBrakePercent(AITrainBrakePercent);
                    if (Simulator.PlayerLocomotive != null && FirstCar != Simulator.PlayerLocomotive)
                    {
                        Simulator.PlayerLocomotive.BrakeSystem.AISetPercent(AITrainBrakePercent);
                        ((MSTSLocomotive)Simulator.PlayerLocomotive).SetTrainBrakePercent(AITrainBrakePercent);
                    }
                }
            }
        }

        public void AdjustControlsBrakeFull()
        {
            AITrainThrottlePercent = 0;
            AITrainBrakePercent = 100;

            if (FirstCar != null)
            {
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
                if (TrainType == TRAINTYPE.AI_PLAYERHOSTING || Autopilot)
                {
                    if (FirstCar is MSTSLocomotive)
                        ((MSTSLocomotive)FirstCar).SetTrainBrakePercent(AITrainBrakePercent);
                    if (Simulator.PlayerLocomotive != null && FirstCar != Simulator.PlayerLocomotive)
                    {
                        Simulator.PlayerLocomotive.BrakeSystem.AISetPercent(AITrainBrakePercent);
                        ((MSTSLocomotive)Simulator.PlayerLocomotive).SetTrainBrakePercent(AITrainBrakePercent);
                    }
                }
            }
        }

        public void AdjustControlsThrottleOff()
        {
            AITrainThrottlePercent = 0;

            if (FirstCar != null)
            {
                FirstCar.ThrottlePercent = AITrainThrottlePercent;
                if (TrainType == TRAINTYPE.AI_PLAYERHOSTING || Autopilot)
                {
                    if (FirstCar is MSTSLocomotive)
                    {
                        ((MSTSLocomotive)FirstCar).SetThrottlePercent(AITrainThrottlePercent);
                    }
                    if (Simulator.PlayerLocomotive != null && FirstCar != Simulator.PlayerLocomotive)
                    {
                        Simulator.PlayerLocomotive.ThrottlePercent = AITrainThrottlePercent;
                        ((MSTSLocomotive)Simulator.PlayerLocomotive).SetThrottlePercent(AITrainThrottlePercent);
                    }
                }
            }
        }

        public virtual void AdjustControlsAccelMore(float reqAccelMpSS, float timeS, int stepSize)
        {
            if (AITrainBrakePercent > 0)
            {
                AdjustControlsBrakeOff();
            }

            if (AITrainThrottlePercent < 100)
            {
                AITrainThrottlePercent += stepSize;
                if (AITrainThrottlePercent > 100)
                    AITrainThrottlePercent = 100;
            }
            else if (LastSpeedMpS == 0 || (((SpeedMpS - LastSpeedMpS) / timeS) < 0.5f * MaxAccelMpSS))
            {
                float ds = timeS * reqAccelMpSS;
                SpeedMpS = LastSpeedMpS + ds;
                foreach (TrainCar car in Cars)
                {
                    // TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                    // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                    // car.SpeedMpS = car.Flipped ? -SpeedMpS : SpeedMpS;
                    car.SpeedMpS = car.Flipped ^ (car.IsDriveable && car.Train.IsActualPlayerTrain && ((MSTSLocomotive)car).UsingRearCab) ? -SpeedMpS : SpeedMpS;
                }

                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Forced speed increase : was " + LastSpeedMpS + " - now " + SpeedMpS + "\n");
                }

            }

            SetPercentsFromTrainToTrainset();
        }


        public void AdjustControlsAccelLess(float reqAccelMpSS, float timeS, int stepSize)
        {
            if (AITrainBrakePercent > 0)
            {
                AdjustControlsBrakeOff();
            }

            if (AITrainThrottlePercent > 0)
            {
                AITrainThrottlePercent -= stepSize;
                if (AITrainThrottlePercent < 0)
                    AITrainThrottlePercent = 0;
            }
            else
            {
                float ds = timeS * reqAccelMpSS;
                SpeedMpS = Math.Max(SpeedMpS - ds, 0); // Avoid negative speeds
                foreach (TrainCar car in Cars)
                {
                    // TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                    // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                    // car.SpeedMpS = car.Flipped ? -SpeedMpS : SpeedMpS;
                    car.SpeedMpS = car.Flipped ^ (car.IsDriveable && car.Train.IsActualPlayerTrain && ((MSTSLocomotive)car).UsingRearCab) ? -SpeedMpS : SpeedMpS;
                }
            }
            SetPercentsFromTrainToTrainset();
        }

        public void AdjustControlsFixedSpeed(float reqSpeedMpS)
        {
            foreach (TrainCar car in Cars)
            {
                // TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                // car.SpeedMpS = car.Flipped ? -reqSpeedMpS : reqSpeedMpS;
                car.SpeedMpS = car.Flipped ^ (car.IsDriveable && car.Train.IsActualPlayerTrain && ((MSTSLocomotive)car).UsingRearCab) ? -reqSpeedMpS : reqSpeedMpS;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Set first car and player loco throttle and brake percent in accordance with their AI train ones
        /// </summary>
        ///
        public void SetPercentsFromTrainToTrainset()
        {
            if (FirstCar != null)
            {
                FirstCar.ThrottlePercent = AITrainThrottlePercent;
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
                if (TrainType == TRAINTYPE.AI_PLAYERHOSTING || Autopilot)
                {
                    if (FirstCar is MSTSLocomotive)
                    {
                        ((MSTSLocomotive)FirstCar).SetTrainBrakePercent(AITrainBrakePercent);
                        ((MSTSLocomotive)FirstCar).SetThrottlePercent(AITrainThrottlePercent);
                    }
                    if (Simulator.PlayerLocomotive != null && FirstCar != Simulator.PlayerLocomotive)
                    {
                        Simulator.PlayerLocomotive.ThrottlePercent = AITrainThrottlePercent;
                        Simulator.PlayerLocomotive.BrakeSystem.AISetPercent(AITrainBrakePercent);
                        ((MSTSLocomotive)Simulator.PlayerLocomotive).SetTrainBrakePercent(AITrainBrakePercent);
                        ((MSTSLocomotive)Simulator.PlayerLocomotive).SetThrottlePercent(AITrainThrottlePercent);
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update AllowedMaxSpeedMps after station stop
        /// </summary>
        /// 
        public void RecalculateAllowedMaxSpeed()
        {
            var allowedMaxSpeedPathMpS = Math.Min(allowedAbsoluteMaxSpeedSignalMpS, allowedAbsoluteMaxSpeedLimitMpS);
            allowedMaxSpeedPathMpS = Math.Min(allowedMaxSpeedPathMpS, allowedAbsoluteMaxTempSpeedLimitMpS);
            AllowedMaxSpeedMpS = Math.Min(allowedMaxSpeedPathMpS, TrainMaxSpeedMpS);
            allowedMaxSpeedSignalMpS = Math.Min(allowedAbsoluteMaxSpeedSignalMpS, TrainMaxSpeedMpS);
            allowedMaxSpeedLimitMpS = Math.Min(allowedAbsoluteMaxSpeedLimitMpS, TrainMaxSpeedMpS);
            allowedMaxTempSpeedLimitMpS = Math.Min(allowedAbsoluteMaxTempSpeedLimitMpS, TrainMaxSpeedMpS);
        }

        //================================================================================================//
        /// <summary>
        /// Create waiting point list
        /// </summary>
        public void BuildWaitingPointList(float clearingDistanceM)
        {
            bool insertSigDelegate = true;

            // Loop through all waiting points - back to front as the processing affects the actual routepaths
            List<int> signalIndex = new List<int>();
            for (int iWait = 0; iWait <= TCRoute.WaitingPoints.Count - 1; iWait++)
            {
                int[] waitingPoint = TCRoute.WaitingPoints[iWait];

                // Check if waiting point is in existing subpath
                if (waitingPoint[0] >= TCRoute.TCRouteSubpaths.Count)
                {
                    Trace.TraceInformation("Waiting point for train " + Name + "(" + Number.ToString() + ") is not on route - point removed");
                    continue;
                }

                TCSubpathRoute thisRoute = TCRoute.TCRouteSubpaths[waitingPoint[0]];
                int routeIndex = thisRoute.GetRouteIndex(waitingPoint[1], 0);
                int lastIndex = routeIndex;

                // Check if waiting point is in route - else give warning and skip
                if (routeIndex < 0)
                {
                    Trace.TraceInformation("Waiting point for train " + Name + "(" + Number.ToString() + ") is not on route - point removed");
                    continue;
                }

                bool endSectionFound = false;
                int endSignalIndex = -1;
                float distanceToEndOfWPSection = 0;

                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisRoute[routeIndex].TCSectionIndex];
                TrackCircuitSection nextSection =
                    routeIndex < thisRoute.Count - 2 ? signalRef.TrackCircuitList[thisRoute[routeIndex + 1].TCSectionIndex] : null;
                int direction = thisRoute[routeIndex].Direction;
                if (thisSection.EndSignals[direction] != null)
                {
                    endSectionFound = true;
                    if (routeIndex < thisRoute.Count - 1)
                        endSignalIndex = thisSection.EndSignals[direction].thisRef;
                }

                // Check if next section is junction
                else if (nextSection == null || nextSection.CircuitType != TrackCircuitSection.TrackCircuitType.Normal)
                {
                    endSectionFound = true;
                }

                // Try and find next section with signal; if junction is found, stop search
                int nextIndex = routeIndex + 1;
                while (nextIndex < thisRoute.Count - 1 && !endSectionFound)
                {
                    nextSection = signalRef.TrackCircuitList[thisRoute[nextIndex].TCSectionIndex];
                    direction = thisRoute[nextIndex].Direction;

                    if (nextSection.EndSignals[direction] != null)
                    {
                        endSectionFound = true;
                        lastIndex = nextIndex;
                        if (lastIndex < thisRoute.Count - 1)
                            endSignalIndex = nextSection.EndSignals[direction].thisRef;
                    }
                    else if (nextSection.CircuitType != TrackCircuitSection.TrackCircuitType.Normal)
                    {
                        endSectionFound = true;
                        lastIndex = nextIndex - 1;
                    }
                    nextIndex++;
                    if (nextSection != null) distanceToEndOfWPSection += nextSection.Length;
                }
                signalIndex.Add(endSignalIndex);

                //<CSComment> TODO This is probably redundant now, however removing it would require extensive testing </CSComment>
                // move backwards WPs within clearingDistanceM, except if of type Horn
                for (int rWP = iWait; insertSigDelegate && signalIndex[iWait] != -1 && rWP >= 0; rWP--)
                {
                    int[] currWP = TCRoute.WaitingPoints[rWP];
                    if ((currWP[2] >= 60011 && currWP[2] <= 60021)
                        || currWP[1] != thisSection.Index || currWP[5] < (int)(thisSection.Length + distanceToEndOfWPSection - clearingDistanceM - 1)) break;
                    currWP[5] = (int)(thisSection.Length + distanceToEndOfWPSection - clearingDistanceM - 1);
                }

            }
            // insertSigDelegate = false;
            for (int iWait = 0; iWait <= TCRoute.WaitingPoints.Count - 1; iWait++)
            {
                insertSigDelegate = true;
                int[] waitingPoint = TCRoute.WaitingPoints[iWait];

                // Check if waiting point is in existing subpath
                if (waitingPoint[0] >= TCRoute.TCRouteSubpaths.Count)
                {
                    Trace.TraceInformation("Waiting point for train " + Name + "(" + Number.ToString() + ") is not on route - point removed");
                    continue;
                }

                TCSubpathRoute thisRoute = TCRoute.TCRouteSubpaths[waitingPoint[0]];
                int routeIndex = thisRoute.GetRouteIndex(waitingPoint[1], 0);
                int lastIndex = routeIndex;
                if (!(waitingPoint[2] >= 60011 && waitingPoint[2] <= 60021))
                {
                    if (iWait != TCRoute.WaitingPoints.Count - 1)
                    {
                        for (int nextWP = iWait + 1; nextWP < TCRoute.WaitingPoints.Count; nextWP++)
                        {
                            if (signalIndex[iWait] != signalIndex[nextWP])
                            {
                                break;
                            }
                            else if (TCRoute.WaitingPoints[nextWP][2] >= 60011 && TCRoute.WaitingPoints[nextWP][2] <= 60021) continue;
                            else
                            {
                                insertSigDelegate = false;
                                break;
                            }
                        }
                    }
                }

                // Check if waiting point is in route - else give warning and skip
                if (routeIndex < 0)
                {
                    Trace.TraceInformation("Waiting point for train " + Name + "(" + Number.ToString() + ") is not on route - point removed");
                    continue;
                }
                int direction = thisRoute[routeIndex].Direction;
                if (!IsActualPlayerTrain)
                {
                    if (waitingPoint[2] >= 60011 && waitingPoint[2] <= 60021)
                    {
                        var durationS = waitingPoint[2] - 60010;
                        AILevelCrossingHornPattern hornPattern;
                        switch (durationS)
                        {
                            case 11:
                                hornPattern = AILevelCrossingHornPattern.CreateInstance(ORTS.Common.LevelCrossingHornPattern.US);
                                break;
                            default:
                                hornPattern = AILevelCrossingHornPattern.CreateInstance(ORTS.Common.LevelCrossingHornPattern.Single);
                                break;
                        }
                        AIActionHornRef action = new AIActionHornRef(this, waitingPoint[5], 0f, waitingPoint[0], lastIndex, thisRoute[lastIndex].TCSectionIndex, direction, durationS, hornPattern);
                        AuxActionsContain.Add(action);
                    }
                    else
                    {
                        AIActionWPRef action = new AIActionWPRef(this, waitingPoint[5], 0f, waitingPoint[0], lastIndex, thisRoute[lastIndex].TCSectionIndex, direction);
                        var randomizedDelay = waitingPoint[2];
                        if (Simulator.Settings.ActRandomizationLevel > 0)
                        {
                            RandomizedWPDelay(ref randomizedDelay);
                        }
                        action.SetDelay(randomizedDelay);
                        action.OriginalDelay = action.Delay;
                        AuxActionsContain.Add(action);
                        if (insertSigDelegate && (waitingPoint[2] != 60002) && signalIndex[iWait] > -1)
                        {
                            AIActSigDelegateRef delegateAction = new AIActSigDelegateRef(this, waitingPoint[5], 0f, waitingPoint[0], lastIndex, thisRoute[lastIndex].TCSectionIndex, direction, action);
                            signalRef.SignalObjects[signalIndex[iWait]].LockForTrain(this.Number, waitingPoint[0]);
                            delegateAction.SetEndSignalIndex(signalIndex[iWait]);

                            if (randomizedDelay >= 30000 && randomizedDelay < 40000)
                            {
                                delegateAction.Delay = randomizedDelay;
                                delegateAction.IsAbsolute = true;
                            }
                            else delegateAction.Delay = 0;
                            delegateAction.SetSignalObject(signalRef.SignalObjects[signalIndex[iWait]]);

                            AuxActionsContain.Add(delegateAction);
                        }
                    }
                }
                else if (insertSigDelegate && signalIndex[iWait] > -1)
                {
                    AIActionWPRef action = new AIActionWPRef(this, waitingPoint[5], 0f, waitingPoint[0], lastIndex, thisRoute[lastIndex].TCSectionIndex, direction);
                    var randomizedDelay = waitingPoint[2];
                    if (Simulator.Settings.ActRandomizationLevel > 0)
                    {
                        RandomizedWPDelay(ref randomizedDelay);
                    }
                    action.SetDelay((randomizedDelay >= 30000 && randomizedDelay < 40000) ? randomizedDelay : 0);
                    AuxActionsContain.Add(action);
                    AIActSigDelegateRef delegateAction = new AIActSigDelegateRef(this, waitingPoint[5], 0f, waitingPoint[0], lastIndex, thisRoute[lastIndex].TCSectionIndex, direction, action);
                    signalRef.SignalObjects[signalIndex[iWait]].LockForTrain(this.Number, waitingPoint[0]);
                    delegateAction.SetEndSignalIndex(signalIndex[iWait]);
                    delegateAction.Delay = randomizedDelay;
                    if (randomizedDelay >= 30000 && randomizedDelay < 40000) delegateAction.IsAbsolute = true;
                    delegateAction.SetSignalObject(signalRef.SignalObjects[signalIndex[iWait]]);

                    AuxActionsContain.Add(delegateAction);
                }
                // insertSigDelegate = false;
            }
        }


        //================================================================================================//
        /// <summary>
        /// Initialize brakes for AI trains
        /// </summary>
        public override void InitializeBrakes()
        {
            if (TrainType == TRAINTYPE.AI_PLAYERDRIVEN || TrainType == TRAINTYPE.PLAYER)
            {
                base.InitializeBrakes();
                return;
            }
            float maxPressurePSI = 90;
            float fullServPressurePSI = 64;
            float maxPressurePSIVacuum = 21;
            float fullServReductionPSI = -5;
            float max = maxPressurePSI;
            float fullServ = fullServPressurePSI;
            BrakeLine3PressurePSI = 0;
            BrakeLine4 = -1;
            if (FirstCar != null && FirstCar.BrakeSystem is VacuumSinglePipe)
            {
                max = maxPressurePSIVacuum;
                fullServ = maxPressurePSIVacuum + fullServReductionPSI;
            }
            EqualReservoirPressurePSIorInHg = BrakeLine2PressurePSI = max;
            ConnectBrakeHoses();
            foreach (TrainCar car in Cars)
            {
                car.BrakeSystem.Initialize(false, max, fullServ, true);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Process end of path 
        /// returns :
        /// [0] : true : end of route, false : not end of route
        /// [1] : true : train still exists, false : train is removed and no longer exists
        /// </summary>
        public virtual bool[] ProcessEndOfPath(int presentTime, bool checkLoop = true)
        {
            bool[] returnValue = new bool[2] { false, true };

            if (PresentPosition[0].RouteListIndex < 0)
            // Is already off path
            {
                returnValue[0] = true;
                if (TrainType != TRAINTYPE.AI_PLAYERHOSTING) Trace.TraceWarning("AI Train {0} service {1} off path and removed", Number, Name);
                ProcessEndOfPathReached(ref returnValue, presentTime);
                return returnValue;
            }

            int directionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].Direction;
            int positionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].TCSectionIndex;
            int directionNowBack = PresentPosition[1].TCDirection;
            int positionNowBack = PresentPosition[1].TCSectionIndex;

            bool[] nextPart = UpdateRouteActions(0, checkLoop);

            if (!nextPart[0]) return returnValue;   // Not at end and not to attach to anything

            returnValue[0] = true; // End of path reached
            if (nextPart[1]) // Next route available
            {
#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                     Number.ToString() + " continued, part : " + TCRoute.activeSubpath.ToString() + "\n");
#endif
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                         Number.ToString() + " continued, part : " + TCRoute.activeSubpath.ToString() + "\n");
                }

                if (positionNowBack == PresentPosition[0].TCSectionIndex && directionNowBack != PresentPosition[0].TCDirection)
                {
                    ReverseFormation(false);
                    // Active subpath must be incremented in parallel in incorporated train if present
                    if (IncorporatedTrainNo >= 0)
                        IncrementSubpath(Simulator.TrainDictionary[IncorporatedTrainNo]);

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                         Number.ToString() + " reversed\n");
#endif
                }
                else if (positionNow == PresentPosition[1].TCSectionIndex && directionNow != PresentPosition[1].TCDirection)
                {
                    ReverseFormation(false);
                    // Active subpath must be incremented in parallel in incorporated train if present
                    if (IncorporatedTrainNo >= 0) IncrementSubpath(Simulator.TrainDictionary[IncorporatedTrainNo]);

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                         Number.ToString() + " reversed\n");
#endif
                }

                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                         Number.ToString() + " reversed\n");
                }

                // Check if next station was on previous subpath - if so, move to this subpath
                if (StationStops.Count > 0)
                {
                    StationStop thisStation = StationStops[0];

                    if (thisStation.Passed)
                    {
                        StationStops.RemoveAt(0);
                    }
                    else if (thisStation.SubrouteIndex < TCRoute.activeSubpath)
                    {
                        thisStation.SubrouteIndex = TCRoute.activeSubpath;

                        if (ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, 0) < 0) // Station no longer on route
                        {
                            if (thisStation.ExitSignal >= 0 && thisStation.HoldSignal && HoldingSignals.Contains(thisStation.ExitSignal))
                            {
                                HoldingSignals.Remove(thisStation.ExitSignal);
                            }
                            StationStops.RemoveAt(0);
                        }
                    }
                }

                // Reset to node control, also reset required actions
                SwitchToNodeControl(-1);
            }
            else
            {
                ProcessEndOfPathReached(ref returnValue, presentTime);
            }

            return returnValue;
        }

        public virtual void ProcessEndOfPathReached(ref bool[] returnValue, int PresentTime)
        {
#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                     Number.ToString() + " removed\n");
#endif
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                     Number.ToString() + " removed\n");
            }
            var removeIt = true;
            var distanceThreshold = PreUpdate ? 5.0f : 2.0f;
            var distanceToNextSignal = DistanceToSignal ?? 0.1f;

            if (Simulator.TimetableMode) removeIt = true;
            else if (TrainType == TRAINTYPE.AI_PLAYERHOSTING || Simulator.OriginalPlayerTrain == this) removeIt = false;
            else if (TCRoute.TCRouteSubpaths.Count == 1 || TCRoute.activeSubpath != TCRoute.TCRouteSubpaths.Count - 1) removeIt = true;
            else if (NextSignalObject[0] != null && NextSignalObject[0].Type == SignalObjectType.Signal && distanceToNextSignal < 25 && distanceToNextSignal >= 0 && PresentPosition[1].DistanceTravelledM < distanceThreshold)
            {
                removeIt = false;
                MovementState = AI_MOVEMENT_STATE.FROZEN;
            }
            else if (PresentPosition[1].DistanceTravelledM < distanceThreshold && FrontTDBTraveller.TrackNodeOffset + 25 > FrontTDBTraveller.TrackNodeLength)
            {
                var tempTraveller = new Traveller(FrontTDBTraveller);
                if (tempTraveller.NextTrackNode() && tempTraveller.IsEnd)
                {
                    removeIt = false;
                    MovementState = AI_MOVEMENT_STATE.FROZEN;
                }
            }
            else
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
                if (TCRoute.ReversalInfo[TCRoute.activeSubpath - 1].Valid && PresentPosition[1].DistanceTravelledM < distanceThreshold && PresentPosition[1].TCOffset < 25)
                {
                    var tempTraveller = new Traveller(RearTDBTraveller);
                    tempTraveller.ReverseDirection();
                    if (tempTraveller.NextTrackNode() && tempTraveller.IsEnd)
                    {
                        removeIt = false;
                        MovementState = AI_MOVEMENT_STATE.FROZEN;
                    }
                }
            }

            if (removeIt)
            {
                if (IncorporatedTrainNo >= 0 && Simulator.TrainDictionary.Count > IncorporatedTrainNo &&
                   Simulator.TrainDictionary[IncorporatedTrainNo] != null) Simulator.TrainDictionary[IncorporatedTrainNo].RemoveTrain();
                RemoveTrain();
            }
            returnValue[1] = false;
        }

        public virtual bool CheckCouplePosition(Train attachTrain, out bool thisTrainFront, out bool otherTrainFront)
        {
            thisTrainFront = true;
            otherTrainFront = true;

            Traveller usedTraveller = new Traveller(FrontTDBTraveller);
            int usePosition = 0;

            if (MUDirection == Direction.Reverse)
            {
                usedTraveller = new Traveller(RearTDBTraveller, Traveller.TravellerDirection.Backward); // Use in direction of movement
                thisTrainFront = false;
                usePosition = 1;
            }

            Traveller otherTraveller = null;
            int useOtherPosition = 0;
            bool withinSection = false;

            // Check if train is in same section as other train, either for the other trains front or rear
            if (PresentPosition[usePosition].TCSectionIndex == attachTrain.PresentPosition[0].TCSectionIndex) // Train in same section as front
            {
                withinSection = true;
            }
            else if (PresentPosition[usePosition].TCSectionIndex == attachTrain.PresentPosition[1].TCSectionIndex) // Train in same section as rear
            {
                useOtherPosition = 1;
                withinSection = true;
            }

            if (!withinSection) // Not yet in same section
            {
                return false;
            }

            // Test directions
            if (PresentPosition[usePosition].TCDirection == attachTrain.PresentPosition[useOtherPosition].TCDirection) // Trains are in same direction
            {
                if (usePosition == 1)
                {
                    otherTraveller = new Traveller(attachTrain.FrontTDBTraveller);
                }
                else
                {
                    otherTraveller = new Traveller(attachTrain.RearTDBTraveller);
                    otherTrainFront = false;
                }
            }
            else
            {
                if (usePosition == 1)
                {
                    otherTraveller = new Traveller(attachTrain.RearTDBTraveller);
                    otherTrainFront = false;
                }
                else
                {
                    otherTraveller = new Traveller(attachTrain.FrontTDBTraveller);
                }
            }

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Check couple position : preupdate : " + PreUpdate.ToString() + "\n");
            }

            if (PreUpdate) return true; // In pre-update, being in the same section is good enough

            // Check distance to other train
            float dist = usedTraveller.OverlapDistanceM(otherTraveller, false);
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Check couple position : distance : " + dist.ToString() + "\n");
            }

            return dist < 0.1f;
        }

        public void CoupleAI(Train attachTrain, bool thisTrainFront, bool attachTrainFront)
        {
            // Stop train
            SpeedMpS = 0;
            AdjustControlsThrottleOff();
            physicsUpdate(0);
            // Check for length of remaining path
            if (attachTrain.TrainType == Train.TRAINTYPE.STATIC && (TCRoute.activeSubpath < TCRoute.TCRouteSubpaths.Count - 1 || ValidRoute[0].Count > 5))
            {
                CoupleAIToStatic(attachTrain, thisTrainFront, attachTrainFront);
                return;
            }
            else if (attachTrain.TrainType != Train.TRAINTYPE.STATIC && TCRoute.activeSubpath < TCRoute.TCRouteSubpaths.Count - 1 && !UncondAttach)
            {
                if ((thisTrainFront && Cars[0] is MSTSLocomotive) || (!thisTrainFront && Cars[Cars.Count - 1] is MSTSLocomotive))
                {
                    StealCarsToLivingTrain(attachTrain, thisTrainFront, attachTrainFront);
                    return;
                }
                else
                {
                    LeaveCarsToLivingTrain(attachTrain, thisTrainFront, attachTrainFront);
                    return;
                }
            }

            {
                // check on reverse formation
                if (thisTrainFront == attachTrainFront)
                {
                    ReverseFormation(false);
                }

                if (attachTrain.TrainType == TRAINTYPE.AI_PLAYERDRIVEN)
                {
                    foreach (var car in Cars)
                        if (car is MSTSLocomotive) (car as MSTSLocomotive).AntiSlip = (attachTrain.LeadLocomotive as MSTSLocomotive).AntiSlip; // <CSComment> TODO Temporary patch until AntiSlip is re-implemented
                }

                var attachCar = Cars[0];
                // Must save this because below the player locomotive passes to the other train
                var isActualPlayerTrain = IsActualPlayerTrain;

                // Attach to front of waiting train
                if (attachTrainFront)
                {
                    attachCar = Cars[Cars.Count - 1];
                    for (int iCar = Cars.Count - 1; iCar >= 0; iCar--)
                    {
                        var car = Cars[iCar];
                        car.Train = attachTrain;
                        // car.CarID = "AI" + attachTrain.Number.ToString() + " - " + (attachTrain.Cars.Count - 1).ToString();
                        attachTrain.Cars.Insert(0, car);
                    }
                    if (attachTrain.LeadLocomotiveIndex >= 0)
                        attachTrain.LeadLocomotiveIndex += Cars.Count;
                }
                // Attach to rear of waiting train
                else
                {
                    foreach (var car in Cars)
                    {
                        car.Train = attachTrain;
                        // car.CarID = "AI" + attachTrain.Number.ToString() + " - " + (attachTrain.Cars.Count - 1).ToString();
                        attachTrain.Cars.Add(car);
                    }
                }

                // Remove cars from this train
                Cars.Clear();
                attachTrain.Length += Length;

                attachTrain.ReinitializeEOT();

                // Recalculate position of formed train
                if (attachTrainFront) // Coupled to front, so rear position is still valid
                {
                    attachTrain.CalculatePositionOfCars();
                    attachTrain.DistanceTravelledM += Length;
                }
                else // Coupled to rear so front position is still valid
                {
                    attachTrain.RepositionRearTraveller(); // Fix the rear traveller
                    attachTrain.CalculatePositionOfCars();
                }

                // Update positions train
                TrackNode tn = attachTrain.FrontTDBTraveller.TN;
                float offset = attachTrain.FrontTDBTraveller.TrackNodeOffset;
                int direction = (int)attachTrain.FrontTDBTraveller.Direction;

                attachTrain.PresentPosition[0].SetTCPosition(tn.TCCrossReference, offset, direction);
                attachTrain.PresentPosition[0].CopyTo(ref attachTrain.PreviousPosition[0]);

                tn = attachTrain.RearTDBTraveller.TN;
                offset = attachTrain.RearTDBTraveller.TrackNodeOffset;
                direction = (int)attachTrain.RearTDBTraveller.Direction;

                attachTrain.PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);
                // Set various items
                attachTrain.CheckFreight();
                attachTrain.SetDPUnitIDs();
                attachTrain.activityClearingDistanceM = attachTrain.Cars.Count < standardTrainMinCarNo ? shortClearingDistanceM : standardClearingDistanceM;
                attachCar.SignalEvent(Event.Couple);

                // <CSComment> as of now it seems to run better without this initialization
                //if (MovementState != AI_MOVEMENT_STATE.AI_STATIC)
                //{
                //     if (!Simulator.Settings.EnhancedActCompatibility) InitializeSignals(true);
                //}
                //  <CSComment> Why initialize brakes of a disappeared train?    
                //            InitializeBrakes();
                attachTrain.physicsUpdate(0);   // stop the wheels from moving etc
                // Remove original train
                if (isActualPlayerTrain && this != Simulator.OriginalPlayerTrain)
                {
                    // Switch to the attached train as the one where we are now will be removed
                    Simulator.TrainSwitcher.PickedTrainFromList = attachTrain;
                    Simulator.TrainSwitcher.ClickedTrainFromList = true;
                    attachTrain.TrainType = TRAINTYPE.AI_PLAYERHOSTING;
                    AI.TrainsToRemoveFromAI.Add((AITrain)attachTrain);
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Player train has been included into train {0} service {1}, that automatically becomes the new player train",
                        Number, Name));
                    Simulator.PlayerLocomotive = Simulator.SetPlayerLocomotive(attachTrain);
                    (attachTrain as AITrain).SwitchToPlayerControl();
                    Simulator.OnPlayerLocomotiveChanged();
                    AI.AITrains.Add(this);
                    AI.aiListChanged = true;
                }
                else 
                    attachTrain.RedefineSoundTriggers();
                if (!UncondAttach)
                {
                    RemoveTrain();
                }
                else
                {
                    // If there is just here a reversal point, increment subpath in order to be in accordance with attachTrain
                    var ppTCSectionIndex = PresentPosition[0].TCSectionIndex;
                    this.IncorporatingTrainNo = attachTrain.Number;
                    this.IncorporatingTrain = attachTrain;
                    SuspendTrain(attachTrain);
                    if (ppTCSectionIndex == TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count - 1].TCSectionIndex)
                        IncrementSubpath(this);
                    attachTrain.IncorporatedTrainNo = this.Number;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Couple AI train to static train
        /// </summary>
        /// 
        public void CoupleAIToStatic(Train attachTrain, bool thisTrainFront, bool attachTrainFront)
        {
            // Check on reverse formation
            if (thisTrainFront == attachTrainFront)
            {
                attachTrain.ReverseFormation(false);
            }
            // Move cars from attachTrain to train
            // Attach to front of this train
            var attachCar = Cars[Cars.Count - 1];
            if (thisTrainFront)
            {
                attachCar = Cars[0];
                for (int iCar = attachTrain.Cars.Count - 1; iCar >= 0; iCar--)
                {
                    var car = attachTrain.Cars[iCar];
                    car.Train = this;
                    // car.CarID = "AI" + Number.ToString() + " - " + (Cars.Count - 1).ToString();
                    Cars.Insert(0, car);
                }
            }
            else
            {
                foreach (var car in attachTrain.Cars)
                {
                    car.Train = this;
                    // car.CarID = "AI" + Number.ToString() + " - " + (Cars.Count - 1).ToString();
                    Cars.Add(car);
                }
            }
            // Remove cars from attached train
            Length += attachTrain.Length;
            attachTrain.Cars.Clear();

            ReinitializeEOT();

            // Recalculate position of formed train
            if (thisTrainFront)  // Coupled to front, so rear position is still valid
            {
                CalculatePositionOfCars();
                DistanceTravelledM += attachTrain.Length;
                PresentPosition[0].DistanceTravelledM = DistanceTravelledM;
                requiredActions.ModifyRequiredDistance(attachTrain.Length);
            }
            else // Coupled to rear so front position is still valid
            {
                RepositionRearTraveller(); // Fix the rear traveller
                CalculatePositionOfCars();
                PresentPosition[1].DistanceTravelledM = DistanceTravelledM - Length;
            }

            // Update positions train
            TrackNode tn = FrontTDBTraveller.TN;
            float offset = FrontTDBTraveller.TrackNodeOffset;
            int direction = (int)FrontTDBTraveller.Direction;

            PresentPosition[0].SetTCPosition(tn.TCCrossReference, offset, direction);
            PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            tn = RearTDBTraveller.TN;
            offset = RearTDBTraveller.TrackNodeOffset;
            direction = (int)RearTDBTraveller.Direction;

            PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);
            // Set various items
            CheckFreight();
            SetDPUnitIDs();
            activityClearingDistanceM = Cars.Count < standardTrainMinCarNo ? shortClearingDistanceM : standardClearingDistanceM;
            attachCar.SignalEvent(Event.Couple);

            // Remove attached train
            if (attachTrain.TrainType == TRAINTYPE.AI)
                ((AITrain)attachTrain).RemoveTrain();
            else
            {
                attachTrain.RemoveFromTrack();
                Simulator.Trains.Remove(attachTrain);
                Simulator.TrainDictionary.Remove(attachTrain.Number);
                Simulator.NameDictionary.Remove(attachTrain.Name.ToLower());
            }
            if (MPManager.IsMultiPlayer()) MPManager.BroadCast(new MSGCouple(this, attachTrain, false).ToString());
            UpdateOccupancies();
            AddTrackSections();
            ResetActions(true);
            physicsUpdate(0);
            RedefineSoundTriggers();
        }

        //================================================================================================//
        /// <summary>
        /// Couple AI train to living train (AI or player) and leave cars to it; both remain alive in this case
        /// </summary>
        /// 
        public void LeaveCarsToLivingTrain(Train attachTrain, bool thisTrainFront, bool attachTrainFront)
        {
            // Find set of cars between loco and attachtrain and pass them to train to attachtrain
            var passedLength = 0.0f;
            if (thisTrainFront)
            {
                while (0 < Cars.Count - 1)
                {
                    var car = Cars[0];
                    if (car is MSTSLocomotive)
                    {
                        break;
                    }
                    else
                    {
                        if (attachTrainFront)
                        {
                            attachTrain.Cars.Insert(0, car);
                            car.Train = attachTrain;
                            car.Flipped = !car.Flipped;
                            if (attachTrain.IsActualPlayerTrain && attachTrain.LeadLocomotiveIndex != -1) attachTrain.LeadLocomotiveIndex++;
                        }
                        else
                        {
                            attachTrain.Cars.Add(car);
                            car.Train = attachTrain;
                        }
                        passedLength += car.CarLengthM;
                        attachTrain.Length += car.CarLengthM;
                        Length -= car.CarLengthM;
                        Cars.Remove(car);
                    }
                }
                Cars[0].SignalEvent(Event.Couple);
            }
            else
            {
                while (0 < Cars.Count - 1)
                {
                    var car = Cars[Cars.Count - 1];
                    if (car is MSTSLocomotive)
                    {
                        break;
                    }
                    else
                    {
                        if (!attachTrainFront)
                        {
                            attachTrain.Cars.Add(car);
                            car.Train = attachTrain;
                            car.Flipped = !car.Flipped;
                        }
                        else
                        {
                            attachTrain.Cars.Insert(0, car);
                            car.Train = attachTrain;
                            if (attachTrain.IsActualPlayerTrain && attachTrain.LeadLocomotiveIndex != -1) attachTrain.LeadLocomotiveIndex++;
                        }
                        passedLength += car.CarLengthM;
                        attachTrain.Length += car.CarLengthM;
                        Length -= car.CarLengthM;
                        Cars.Remove(car);
                    }
                }
                Cars[Cars.Count - 1].SignalEvent(Event.Couple);
            }

            TerminateCoupling(attachTrain, thisTrainFront, attachTrainFront, passedLength);
        }

        //================================================================================================//
        /// <summary>
        /// Coupling AI train steals cars to coupled AI train
        /// </summary>
        public void StealCarsToLivingTrain(Train attachTrain, bool thisTrainFront, bool attachTrainFront)
        {
            var stealedLength = 0.0f;
            if (attachTrainFront)
            {
                while (0 < attachTrain.Cars.Count - 1)
                {
                    var car = attachTrain.Cars[0];
                    if (car is MSTSLocomotive)
                    {
                        // No other car to steal, leave to the attached train its loco
                        break;
                    }
                    else
                    {
                        if (thisTrainFront)
                        {
                            Cars.Insert(0, car);
                            car.Train = this;
                            car.Flipped = !car.Flipped;
                        }
                        else
                        {
                            Cars.Add(car);
                            car.Train = this;
                        }
                        stealedLength += car.CarLengthM;
                        Length += car.CarLengthM;
                        attachTrain.Length -= car.CarLengthM;
                        attachTrain.Cars.Remove(car);
                        if (attachTrain.IsActualPlayerTrain && attachTrain.LeadLocomotiveIndex != -1) attachTrain.LeadLocomotiveIndex--;
                    }
                }
                attachTrain.Cars[0].SignalEvent(Event.Couple);
            }
            else
            {
                while (0 < attachTrain.Cars.Count - 1)
                {
                    var car = attachTrain.Cars[attachTrain.Cars.Count - 1];
                    if (car is MSTSLocomotive)
                    {
                        // ditto
                        break;
                    }
                    else
                    {
                        if (!thisTrainFront)
                        {
                            Cars.Add(car);
                            car.Train = this;
                            car.Flipped = !car.Flipped;
                        }
                        else
                        {
                            Cars.Insert(0, car);
                            car.Train = this;
                        }
                        stealedLength += car.CarLengthM;
                        Length += car.CarLengthM;
                        attachTrain.Length -= car.CarLengthM;
                        attachTrain.Cars.Remove(car);
                    }
                }
                attachTrain.Cars[attachTrain.Cars.Count - 1].SignalEvent(Event.Couple);
            }

            TerminateCoupling(attachTrain, thisTrainFront, attachTrainFront, -stealedLength);
        }

        //================================================================================================//
        /// <summary>
        /// Uncouple and perform housekeeping
        /// </summary>
        /// 
        public void TerminateCoupling(Train attachTrain, bool thisTrainFront, bool attachTrainFront, float passedLength)
        {

            // Uncouple
            UncoupledFrom = attachTrain;
            attachTrain.UncoupledFrom = this;

            ReinitializeEOT();
            attachTrain.ReinitializeEOT();

            // Recalculate position of coupling train
            if (thisTrainFront) // Coupled to front, so rear position is still valid
            {
                CalculatePositionOfCars();
                DistanceTravelledM -= passedLength;
                Cars[0].BrakeSystem.AngleCockAOpen = false;
            }
            else // Coupled to rear so front position is still valid
            {
                RepositionRearTraveller(); // Fix the rear traveller
                CalculatePositionOfCars();
                Cars[Cars.Count - 1].BrakeSystem.AngleCockBOpen = false;
            }

            // Recalculate position of coupled train
            if (attachTrainFront) // Coupled to front, so rear position is still valid
            {
                attachTrain.CalculatePositionOfCars();
                attachTrain.DistanceTravelledM += passedLength;
                attachTrain.Cars[0].BrakeSystem.AngleCockAOpen = false;
            }
            else // Coupled to rear so front position is still valid
            {
                attachTrain.RepositionRearTraveller(); // Fix the rear traveller
                attachTrain.CalculatePositionOfCars();
                attachTrain.Cars[attachTrain.Cars.Count - 1].BrakeSystem.AngleCockBOpen = false;
            }

            // Update positions of coupling train
            TrackNode tn = FrontTDBTraveller.TN;
            float offset = FrontTDBTraveller.TrackNodeOffset;
            int direction = (int)FrontTDBTraveller.Direction;

            PresentPosition[0].SetTCPosition(tn.TCCrossReference, offset, direction);
            PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            tn = RearTDBTraveller.TN;
            offset = RearTDBTraveller.TrackNodeOffset;
            direction = (int)RearTDBTraveller.Direction;

            PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);

            // Update positions of coupled train
            tn = attachTrain.FrontTDBTraveller.TN;
            offset = attachTrain.FrontTDBTraveller.TrackNodeOffset;
            direction = (int)attachTrain.FrontTDBTraveller.Direction;

            attachTrain.PresentPosition[0].SetTCPosition(tn.TCCrossReference, offset, direction);
            attachTrain.PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            tn = attachTrain.RearTDBTraveller.TN;
            offset = attachTrain.RearTDBTraveller.TrackNodeOffset;
            direction = (int)attachTrain.RearTDBTraveller.Direction;

            attachTrain.PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);

            // Set various items
            CheckFreight();
            SetDPUnitIDs();
            activityClearingDistanceM = Cars.Count < standardTrainMinCarNo ? shortClearingDistanceM : standardClearingDistanceM;
            attachTrain.CheckFreight();
            attachTrain.SetDPUnitIDs();
            attachTrain.activityClearingDistanceM = attachTrain.Cars.Count < standardTrainMinCarNo ? shortClearingDistanceM : standardClearingDistanceM;

            // Anticipate reversal point and remove active action
            TCRoute.ReversalInfo[TCRoute.activeSubpath].ReverseReversalOffset = Math.Max(PresentPosition[0].TCOffset - 10f, 0.3f);
            if (PresentPosition[0].TCSectionIndex != TCRoute.ReversalInfo[TCRoute.activeSubpath].ReversalSectionIndex)
            {
                TCRoute.ReversalInfo[TCRoute.activeSubpath].ReversalSectionIndex = PresentPosition[0].TCSectionIndex;
            }
            if (PresentPosition[1].RouteListIndex < TCRoute.ReversalInfo[TCRoute.activeSubpath].LastSignalIndex)
                TCRoute.ReversalInfo[TCRoute.activeSubpath].LastSignalIndex = PresentPosition[1].RouteListIndex;
            // Move WP, if any, just under the loco;
            AuxActionsContain.MoveAuxActionAfterReversal(this);
            ResetActions(true);
            RedefineSoundTriggers();
            attachTrain.RedefineSoundTriggers();
            physicsUpdate(0);// Stop the wheels from moving etc

        }

        //================================================================================================//
        /// <summary>
        /// TestUncouple
        /// Tests if Waiting point delay >40000 and <59999; under certain conditions this means that
        /// an uncoupling action happens
        /// delay (in decimal notation) = 4NNSS (uncouple cars after NNth from train front (locos included), wait SS seconds)
        ///                            or 5NNSS (uncouple cars before NNth from train rear (locos included), keep rear, wait SS seconds)
        /// remember that for AI trains train front is the one of the actual moving direction, so train front changes at every reverse point
        /// </summary>
        /// 
        public void TestUncouple(ref int delay)
        {
            if (delay <= 40000 || delay >= 60000) return;
            bool keepFront = true;
            int carsToKeep;
            if (delay > 50000 && delay < 60000)
            {
                keepFront = false;
                delay = delay - 10000;
            }
            carsToKeep = (delay - 40000) / 100;
            delay = delay - 40000 - (carsToKeep * 100);
            if (IsActualPlayerTrain && TrainType == TRAINTYPE.AI_PLAYERDRIVEN && this != Simulator.OriginalPlayerTrain)
            {
                Simulator.ActivityRun.MsgFromNewPlayer = String.Format("Uncouple and keep coupled only {0} {1} cars", carsToKeep, keepFront ? "first" : "last");
                Simulator.ActivityRun.NewMsgFromNewPlayer = true;
            }
            else UncoupleSomeWagons(carsToKeep, keepFront);
        }

        //================================================================================================//
        /// <summary>
        /// UncoupleSomeWagons
        /// Uncouples some wagons, starting from rear if keepFront is true and from front if it is false
        /// Uncoupled wagons become a static consist
        /// </summary>
        /// 
        private void UncoupleSomeWagons(int carsToKeep, bool keepFront)
        {
            // First test that carsToKeep is smaller than number of cars of train
            if (carsToKeep >= Cars.Count)
            {
                carsToKeep = Cars.Count - 1;
                Trace.TraceWarning("Train {0} Service {1} reduced cars to uncouple", Number, Name);
            }
            // Then test if there is at least one loco in the not-uncoupled part
            int startCarIndex = keepFront ? 0 : Cars.Count - carsToKeep;
            int endCarIndex = keepFront ? carsToKeep - 1 : Cars.Count - 1;
            bool foundLoco = false;
            for (int carIndex = startCarIndex; carIndex <= endCarIndex; carIndex++)
            {
                if (Cars[carIndex] is MSTSLocomotive)
                {
                    foundLoco = true;
                    break;
                }
            }
            if (!foundLoco)
            {
                // No loco in remaining part, abort operation
                Trace.TraceWarning("Train {0} Service {1} Uncoupling not executed, no loco in remaining part of train", Number, Name);
                return;
            }
            int uncouplePoint = keepFront ? carsToKeep - 1 : Cars.Count - carsToKeep - 1;
            Simulator.UncoupleBehind(Cars[uncouplePoint], keepFront);
        }

        //================================================================================================//
        /// <summary>
        /// TestUncondAttach
        /// Tests if Waiting point delay =60001; under certain conditions this means that the train has to attach the nearby train
        /// </summary>
        public void TestUncondAttach(ref int delay)
        {
            if (delay != 60001) return;
            else
            {
                if (IsActualPlayerTrain && this != Simulator.OriginalPlayerTrain)
                {
                    Simulator.ActivityRun.MsgFromNewPlayer = "You are involved in a join and split task; when you will couple to next train, you automatically will be switched to drive such next train";
                    Simulator.ActivityRun.NewMsgFromNewPlayer = true;
                }
                delay = 0;
                UncondAttach = true;
            }
        }

        //================================================================================================//
        /// <summary>
        /// TestPermission
        /// Tests if Waiting point delay =60002; a permission request to pass next signal is launched.
        /// </summary>
        public void TestPermission(ref int delay)
        {
            if (delay != 60002) return;
            else
            {
                delay = 20;
                if (IsActualPlayerTrain && TrainType == TRAINTYPE.AI_PLAYERDRIVEN && this != Simulator.OriginalPlayerTrain)
                {
                    Simulator.ActivityRun.MsgFromNewPlayer = "Ask permission to pass signal (press TAB or Shift-TAB) and proceed";
                    Simulator.ActivityRun.NewMsgFromNewPlayer = true;
                }
                else RequestSignalPermission(ValidRoute[0], 0);
            }
        }

        //================================================================================================//
        //
        // Request signal permission for AI trains (triggered by WP 60002)
        //
        public void RequestSignalPermission(TCSubpathRoute selectedRoute, int routeIndex)
        {
            // Check if signal at danger
            TCRouteElement thisElement = selectedRoute[PresentPosition[0].RouteListIndex];
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

            // No signal in required direction
            if (thisSection.EndSignals[thisElement.Direction] == null)
                return;

            var requestedSignal = thisSection.EndSignals[thisElement.Direction];
            if (requestedSignal.enabledTrain != null && requestedSignal.enabledTrain.Train != this)
                return;

            requestedSignal.enabledTrain = routeIndex == 0 ? routedForward : routedBackward;
            requestedSignal.holdState = HoldState.None;
            requestedSignal.hasPermission = SignalObject.Permission.Requested;

            requestedSignal.checkRouteState(false, requestedSignal.signalRoute, routedForward, false);
        }

        //================================================================================================//
        /// <summary>
        /// Remove train
        /// </summary>
        public override void RemoveTrain()
        {
            RemoveFromTrack();
            ClearDeadlocks();

#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\Temp\deadlock.txt", "\n === Remove train : " + Number + " - Clearing Deadlocks : \n");

            foreach (TrackCircuitSection thisSection in Simulator.Signals.TrackCircuitList)
            {
                if (thisSection.DeadlockTraps.Count > 0)
                {
                    File.AppendAllText(@"C:\Temp\deadlock.txt", "Section : " + thisSection.Index.ToString() + "\n");
                    foreach (KeyValuePair<int, List<int>> thisDeadlock in thisSection.DeadlockTraps)
                    {
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "    Train : " + thisDeadlock.Key.ToString() + "\n");
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "       With : " + "\n");
                        foreach (int otherTrain in thisDeadlock.Value)
                        {
                            File.AppendAllText(@"C:\Temp\deadlock.txt", "          " + otherTrain.ToString() + "\n");
                        }
                    }
                }
            }
#endif
            // Remove train
            AI.TrainsToRemove.Add(this);
        }

        //================================================================================================//
        /// <summary>
        /// Suspend train because incorporated in other train
        /// </summary>
        public virtual void SuspendTrain(Train incorporatingTrain)
        {
            RemoveFromTrack();
            ClearDeadlocks();
            NextSignalObject[0] = null;
            NextSignalObject[1] = null;
            // Reset AuxAction if any
            AuxActionsContain.ResetAuxAction(this);
            TrainType = TRAINTYPE.AI_INCORPORATED;
            LeadLocomotiveIndex = -1;
            Cars.Clear();
            requiredActions.RemovePendingAIActionItems(true);
            UncondAttach = false;
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Join success: Train {0} service {1} has been incorporated into train {2} service {3}",
                Number, Name.Substring(0, Math.Min(Name.Length, 20)), incorporatingTrain.Number, incorporatingTrain.Name.Substring(0, Math.Min(incorporatingTrain.Name.Length, 20))));
        }

        //================================================================================================//
        /// <summary>
        /// Insert action item
        /// </summary>
        public void CreateTrainAction(float presentSpeedMpS, float reqSpeedMpS, float distanceToTrainM,
                ObjectItemInfo thisItem, AIActionItem.AI_ACTION_TYPE thisAction)
        {
            // If signal or speed limit take off clearing distance
            float activateDistanceTravelledM = PresentPosition[0].DistanceTravelledM + distanceToTrainM;
            if (thisItem != null)
            {
                activateDistanceTravelledM -= Simulator.TimetableMode ? clearingDistanceM : activityClearingDistanceM;
            }

            // Calculate braking distance
            float firstPartTime = 0.0f;
            float firstPartRangeM = 0.0f;
            float secondPartTime = 0.0f;
            float secondPartRangeM = 0.0f;
            float minReqRangeM = 0.0f;
            float remainingRangeM = activateDistanceTravelledM - PresentPosition[0].DistanceTravelledM;

            float triggerDistanceM = PresentPosition[0].DistanceTravelledM; // worst case

            // TODO: Can we remove this?
            // braking distance - use 0.22 * MaxDecelMpSS as average deceleration (due to braking delay)
            // T = deltaV / A
            // R = 0.5 * Vdelta * T + Vreq * T = 0.5 * (Vnow + Vreq) * T 
            // 0.5 * Vdelta is average speed over used time, 0.5 * Vdelta * T is related distance covered , Vreq * T is additional distance covered at minimal speed

            float fullPartTime = (AllowedMaxSpeedMpS - reqSpeedMpS) / (0.22f * MaxDecelMpSS);
            float fullPartRangeM = (AllowedMaxSpeedMpS + reqSpeedMpS) * 0.5f * fullPartTime;

            // If present speed higher, brake distance is always required (same equation)
            if (presentSpeedMpS > reqSpeedMpS)
            {
                firstPartTime = (presentSpeedMpS - reqSpeedMpS) / (0.22f * MaxDecelMpSS);
                firstPartRangeM = (presentSpeedMpS + reqSpeedMpS) * 0.5f * firstPartTime;
            }

            minReqRangeM = Math.Max(fullPartRangeM, firstPartRangeM);

            // If present speed below max speed, calculate distance required to accelerate to max speed (same equation)
            if (presentSpeedMpS < AllowedMaxSpeedMpS)
            {
                secondPartTime = (AllowedMaxSpeedMpS - presentSpeedMpS) / (0.5f * MaxAccelMpSS);
                secondPartRangeM = (AllowedMaxSpeedMpS + presentSpeedMpS) * 0.5f * secondPartTime;
            }

            // If full length possible, set as trigger distance
            if ((minReqRangeM + secondPartRangeM) < remainingRangeM)
            {
                triggerDistanceM = activateDistanceTravelledM - (fullPartRangeM + secondPartRangeM);
            }
            // If braking from full speed still possible, set as trigger distance
            // Train will accelerate upto trigger point but probably not reach full speed, so there is enough braking distance available
            else if (minReqRangeM < remainingRangeM)
            {
                triggerDistanceM = activateDistanceTravelledM - fullPartRangeM;
            }
            // Else if still possible, use minimun range based on present speed
            else if (firstPartRangeM < remainingRangeM)
            {
                triggerDistanceM = activateDistanceTravelledM - firstPartRangeM;
            }

            // Correct trigger for approach distance but not backward beyond present position
            triggerDistanceM = Math.Max(PresentPosition[0].DistanceTravelledM, triggerDistanceM - (3.0f * signalApproachDistanceM));

            // For signal stop item: check if action allready in list, if so, remove (can be result of restore action)
            LinkedListNode<DistanceTravelledItem> thisItemLink = requiredActions.First;
            bool itemFound = false;

            while (thisItemLink != null && !itemFound)
            {
                DistanceTravelledItem thisDTItem = thisItemLink.Value;
                if (thisDTItem is AIActionItem)
                {
                    AIActionItem thisActionItem = thisDTItem as AIActionItem;
                    if (thisActionItem.ActiveItem != null && thisActionItem.NextAction == thisAction)
                    {
                        if (thisActionItem.ActiveItem.ObjectDetails.thisRef == thisItem.ObjectDetails.thisRef)
                        {
                            // Equal item, so remove it
                            requiredActions.Remove(thisDTItem);
                            itemFound = true;
                        }
                    }
                }
                if (!itemFound)
                {
                    thisItemLink = thisItemLink.Next;
                }
            }

            // Create and insert action
            AIActionItem newAction = new AIActionItem(thisItem, thisAction);
            newAction.SetParam(triggerDistanceM, reqSpeedMpS, activateDistanceTravelledM, DistanceTravelledM);
            requiredActions.InsertAction(newAction);

#if DEBUG_REPORTS
            if (thisItem != null && thisItem.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Insert for train " +
                         Number.ToString() + ", type " +
                         thisAction.ToString() + " for signal " +
                         thisItem.ObjectDetails.thisRef.ToString() + ", at " +
                         activateDistanceTravelledM.ToString() + ", trigger at " +
                         triggerDistanceM.ToString() + " (now at " +
                         PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
            }
            else
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Insert for train " +
                         Number.ToString() + ", type " +
                         thisAction.ToString() + " at " +
                         activateDistanceTravelledM.ToString() + ", trigger at " +
                         triggerDistanceM.ToString() + " (now at " +
                         PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
            }
#endif
            if (CheckTrain)
            {
                if (thisItem != null && thisItem.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Insert for train " +
                             Number.ToString() + ", type " +
                             thisAction.ToString() + " for signal " +
                             thisItem.ObjectDetails.thisRef.ToString() + ", at " +
                             activateDistanceTravelledM.ToString() + ", trigger at " +
                             triggerDistanceM.ToString() + " (now at " +
                             PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Insert for train " +
                             Number.ToString() + ", type " +
                             thisAction.ToString() + " at " +
                             activateDistanceTravelledM.ToString() + ", trigger at " +
                             triggerDistanceM.ToString() + " (now at " +
                             PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Insert action item for end-of-route
        /// </summary>
        public virtual void SetEndOfRouteAction()
        {
            // Remaining length first section
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex];
            float lengthToGoM = thisSection.Length - PresentPosition[0].TCOffset;
            if (TCRoute.activeSubpath < TCRoute.TCRouteSubpaths.Count - 1)
            {
                // Go through all further sections
                for (int iElement = PresentPosition[0].RouteListIndex + 1; iElement < ValidRoute[0].Count; iElement++)
                {
                    TCRouteElement thisElement = ValidRoute[0][iElement];
                    thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    lengthToGoM += thisSection.Length;
                }
            }
            else lengthToGoM = ComputeDistanceToReversalPoint();
            lengthToGoM -= 5.0f; // Keep save distance from end

            // Only do so for last subroute to avoid falling short of reversal points
            TCRouteElement lastElement = ValidRoute[0][ValidRoute[0].Count - 1];
            TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastElement.TCSectionIndex];

            CreateTrainAction(TrainMaxSpeedMpS, 0.0f, lengthToGoM, null,
                    AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE);
            NextStopDistanceM = lengthToGoM;
        }

        //================================================================================================//
        /// <summary>
        /// Reset action list
        /// </summary>
        public void ResetActions(bool setEndOfPath, bool fromAutopilotSwitch = false)
        {
#if DEBUG_REPORTS
            if (nextActionInfo != null)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Reset all for train " +
                         Number.ToString() + ", type " +
                         nextActionInfo.NextAction.ToString() + ", at " +
                         nextActionInfo.ActivateDistanceM.ToString() + ", trigger at " +
                         nextActionInfo.RequiredDistance.ToString() + " (now at " +
                         PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                         FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
            }
            else
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Reset all for train " +
                         Number.ToString() + " (now at " +
                         PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                         FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
            }
#endif
            if (CheckTrain)
            {
                if (nextActionInfo != null)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Reset all for train " +
                             Number.ToString() + ", type " +
                             nextActionInfo.NextAction.ToString() + ", at " +
                             nextActionInfo.ActivateDistanceM.ToString() + ", trigger at " +
                             nextActionInfo.RequiredDistance.ToString() + " (now at " +
                             PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Reset all for train " +
                             Number.ToString() + " (now at " +
                             PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                }
            }

            // Do not set actions for player train
            if (TrainType == TRAINTYPE.PLAYER)
            {
                return;
            }

            // Reset signal items processed state
            nextActionInfo = null;
            foreach (ObjectItemInfo thisInfo in SignalObjectItems)
            {
                thisInfo.processed = false;
            }

            // Clear any outstanding actions
            requiredActions.RemovePendingAIActionItems(false);

            // Reset auxiliary actions
            AuxActionsContain.SetAuxAction(this);

            // Set next station stop in not at station
            if (StationStops.Count > 0)
            {
                SetNextStationAction(fromAutopilotSwitch);
            }

            // Set end of path if required
            if (setEndOfPath)
            {
                SetEndOfRouteAction();
            }

            // To allow re-inserting of reversal action if necessary
            if (TCRoute.ReversalInfo[TCRoute.activeSubpath].ReversalActionInserted == true)
            {
                TCRoute.ReversalInfo[TCRoute.activeSubpath].ReversalActionInserted = false;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Perform stored actions
        /// </summary>
        public override void PerformActions(List<DistanceTravelledItem> nowActions)
        {
            foreach (var thisAction in nowActions)
            {
                if (thisAction is ClearSectionItem)
                {
                    ClearOccupiedSection(thisAction as ClearSectionItem);
                }
                else if (thisAction is ActivateSpeedLimit)
                {
                    if (TrainType == TRAINTYPE.PLAYER)
                    {
                        SetPendingSpeedLimit(thisAction as ActivateSpeedLimit);
                    }
                    else
                    {
                        SetAIPendingSpeedLimit(thisAction as ActivateSpeedLimit);
                    }
                }
                else if (thisAction is ClearMovingTableAction)
                {
                    ClearMovingTable(thisAction);
                }
                else if (thisAction is AIActionItem && !(thisAction is AuxActionItem))
                {
                    ProcessActionItem(thisAction as AIActionItem);
                }
                else if (thisAction is AuxActionWPItem)
                {
                    var valid = ((AuxActionItem)thisAction).ValidAction(this);
                    if (valid && TrainType == TRAINTYPE.AI_PLAYERDRIVEN)
                    {
                        var presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
                        ((AuxActionItem)thisAction).ProcessAction(this, presentTime);
                    }
                }
                else if (thisAction is AuxActionItem)
                {
                    var presentTime = !PreUpdate ? Convert.ToInt32(Math.Floor(Simulator.ClockTime)) : Convert.ToInt32(Math.Floor(AI.clockTime));
                    var actionState = ((AuxActionItem)thisAction).ProcessAction(this, presentTime);
                    if (actionState != AI_MOVEMENT_STATE.INIT_ACTION && actionState != AI_MOVEMENT_STATE.HANDLE_ACTION)
                        MovementState = actionState;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Set pending speed limits
        /// </summary>
        public void SetAIPendingSpeedLimit(ActivateSpeedLimit speedInfo)
        {
            if (speedInfo.MaxSpeedMpSSignal > 0)
            {
                allowedMaxSpeedSignalMpS = Simulator.TimetableMode ? speedInfo.MaxSpeedMpSSignal : allowedAbsoluteMaxSpeedSignalMpS;
                AllowedMaxSpeedMpS = Math.Min(speedInfo.MaxSpeedMpSSignal, Math.Min(allowedMaxSpeedLimitMpS, allowedMaxTempSpeedLimitMpS));
            }
            if (speedInfo.MaxSpeedMpSLimit > 0)
            {
                allowedMaxSpeedLimitMpS = Simulator.TimetableMode ? speedInfo.MaxSpeedMpSLimit : allowedAbsoluteMaxSpeedLimitMpS;
                AllowedMaxSpeedMpS = Simulator.TimetableMode
                    ? speedInfo.MaxSpeedMpSLimit
                    : Math.Min(speedInfo.MaxSpeedMpSLimit, Math.Min(allowedMaxSpeedSignalMpS, allowedMaxTempSpeedLimitMpS));
            }
            if (speedInfo.MaxTempSpeedMpSLimit > 0)
            {
                allowedMaxTempSpeedLimitMpS = allowedAbsoluteMaxTempSpeedLimitMpS;
                AllowedMaxSpeedMpS = Math.Min(speedInfo.MaxTempSpeedMpSLimit, Math.Min(allowedMaxSpeedSignalMpS, allowedMaxSpeedLimitMpS));
            }
            // <CScomment> Following statement should be valid in general, as it seems there was a bug here in the original SW
            AllowedMaxSpeedMpS = Math.Min(AllowedMaxSpeedMpS, TrainMaxSpeedMpS);

            if (MovementState == AI_MOVEMENT_STATE.RUNNING && SpeedMpS < AllowedMaxSpeedMpS - (2.0f * hysterisMpS))
            {
                MovementState = AI_MOVEMENT_STATE.ACCELERATING;
                Alpha10 = PreUpdate & !Simulator.TimetableMode ? 2 : 10;
            }

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number + " Validated speedlimit : " +
               "Limit : " + allowedMaxSpeedLimitMpS.ToString() + " ; " +
               "Signal : " + allowedMaxSpeedSignalMpS.ToString() + " ; " +
               "Overall : " + AllowedMaxSpeedMpS.ToString() + "\n");

#endif

            // Reset pending actions to recalculate braking distance
            ResetActions(true);
        }

        //================================================================================================//
        /// <summary>
        /// Process pending actions
        /// </summary>
        public void ProcessActionItem(AIActionItem thisItem)
        {
            // Normal actions
            bool actionValid = true;
            bool actionCleared = false;

#if DEBUG_REPORTS
            if (thisItem.ActiveItem != null && thisItem.ActiveItem.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Activated for train " +
                         Number.ToString() + ", type " +
                         thisItem.NextAction.ToString() + " for signal " +
                         thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + ", at " +
                         thisItem.ActivateDistanceM.ToString() + ", trigger at " +
                         thisItem.RequiredDistance.ToString() + " (now at " +
                         PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                         FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
            }
            else
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Activated for train " +
                         Number.ToString() + ", type " +
                         thisItem.NextAction.ToString() + " at " +
                         thisItem.ActivateDistanceM.ToString() + ", trigger at " +
                         thisItem.RequiredDistance.ToString() + " (now at " +
                         PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                         FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
            }
#endif
            if (CheckTrain)
            {
                if (thisItem.ActiveItem != null && thisItem.ActiveItem.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Activated for train " +
                             Number.ToString() + ", type " +
                             thisItem.NextAction.ToString() + " for signal " +
                             thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + ", at " +
                             thisItem.ActivateDistanceM.ToString() + ", trigger at " +
                             thisItem.RequiredDistance.ToString() + " (now at " +
                             PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Activated for train " +
                             Number.ToString() + ", type " +
                             thisItem.NextAction.ToString() + " at " +
                             thisItem.ActivateDistanceM.ToString() + ", trigger at " +
                             thisItem.RequiredDistance.ToString() + " (now at " +
                             PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                }
            }

            // If signal speed, check if still set
            if (thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SPEED_SIGNAL)
            {
                if (thisItem.ActiveItem.actual_speed == AllowedMaxSpeedMpS) // No longer valid
                {
                    actionValid = false;
                }
                else if (thisItem.ActiveItem.actual_speed != thisItem.RequiredSpeedMpS)
                {
                    actionValid = false;
                }
            }

            // If signal, check if not held for station stop (station stop comes first)
            else if (thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
            {
                if (thisItem.ActiveItem.signal_state == MstsSignalAspect.STOP &&
                    thisItem.ActiveItem.ObjectDetails.holdState == HoldState.StationStop)
                {
                    // Check if train is approaching or standing at station and has not yet departed
                    if (StationStops != null && StationStops.Count >= 1 && AtStation && StationStops[0].ExitSignal == thisItem.ActiveItem.ObjectDetails.thisRef)
                    {
                        actionValid = false;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " : signal " +
                            thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                            thisItem.ActivateDistanceM.ToString() + " is held for station stop\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                    Number.ToString() + " : signal " +
                                    thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                    thisItem.ActivateDistanceM.ToString() + " is held for station stop\n");
                        }
                    }
                    else
                    {
#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " : signal " +
                            thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                            thisItem.ActivateDistanceM.ToString() + " is held for station stop but train is no longer stopped in station\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                    Number.ToString() + " : signal " +
                                    thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                    thisItem.ActivateDistanceM.ToString() + " is held for station stop but train is no longer stopped in station\n");
                        }
                    }
                }
                // Check if cleared
                else if (thisItem.ActiveItem.signal_state >= MstsSignalAspect.APPROACH_1)
                {
                    actionValid = false;
                    actionCleared = true;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " : signal " +
                            thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                            thisItem.ActivateDistanceM.ToString() + " cleared\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " : signal " +
                                thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                thisItem.ActivateDistanceM.ToString() + " cleared\n");
                    }
                }
                // Dheck if restricted
                else if (thisItem.ActiveItem.signal_state != MstsSignalAspect.STOP)
                {
                    thisItem.NextAction = AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED;
                    if (((thisItem.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM) ||
                         thisItem.ActiveItem.ObjectDetails.this_sig_noSpeedReduction(SignalFunction.NORMAL))
                    {
                        actionValid = false;
                        actionCleared = true;

#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " : signal " +
                            thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                            thisItem.ActivateDistanceM.ToString() + " set to RESTRICTED\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " : signal " +
                                thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                thisItem.ActivateDistanceM.ToString() + " set to RESTRICTED\n");
                        }
                    }
                }

                // Recalculate braking distance if train is running slow
                if (actionValid && SpeedMpS < creepSpeedMpS)
                {
                    float firstPartTime = 0.0f;
                    float firstPartRangeM = 0.0f;
                    float secndPartRangeM = 0.0f;
                    float remainingRangeM = thisItem.ActivateDistanceM - PresentPosition[0].DistanceTravelledM;

                    if (SpeedMpS > thisItem.RequiredSpeedMpS) // If present speed higher, brake distance is always required
                    {
                        firstPartTime = (SpeedMpS - thisItem.RequiredSpeedMpS) / (0.25f * MaxDecelMpSS);
                        firstPartRangeM = 0.25f * MaxDecelMpSS * (firstPartTime * firstPartTime);
                    }

                    if (firstPartRangeM < remainingRangeM && SpeedMpS < TrainMaxSpeedMpS) // If distance left and not at max speed
                    // Split remaining distance based on relation between acceleration and deceleration
                    {
                        secndPartRangeM = (remainingRangeM - firstPartRangeM) * (2.0f * MaxDecelMpSS) / (MaxDecelMpSS + MaxAccelMpSS);
                    }

                    float fullRangeM = firstPartRangeM + secndPartRangeM;
                    if (fullRangeM < remainingRangeM && remainingRangeM > 300.0f) // If range is shorter and train not too close, reschedule
                    {
                        actionValid = false;
                        thisItem.RequiredDistance = thisItem.ActivateDistanceM - fullRangeM;
                        requiredActions.InsertAction(thisItem);

#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Rescheduled for train " +
                             Number.ToString() + ", type " +
                             thisItem.NextAction.ToString() + " for signal " +
                             thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + ", at " +
                             thisItem.ActivateDistanceM.ToString() + ", trigger at " +
                             thisItem.RequiredDistance.ToString() + " (now at " +
                             PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Rescheduled for train " +
                                 Number.ToString() + ", type " +
                                 thisItem.NextAction.ToString() + " for signal " +
                                 thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + ", at " +
                                 thisItem.ActivateDistanceM.ToString() + ", trigger at " +
                                 thisItem.RequiredDistance.ToString() + " (now at " +
                                 PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                                 FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                        }
                    }
                }
            }

            // If signal at RESTRICTED, check if not cleared
            else if (thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED)
            {
                if (thisItem.ActiveItem.signal_state >= MstsSignalAspect.APPROACH_1 ||
                (thisItem.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM)
                {
                    actionValid = false;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " : signal " +
                            thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                            thisItem.ActivateDistanceM.ToString() + " cleared\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " : signal " +
                                thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                thisItem.ActivateDistanceM.ToString() + " cleared\n");
                    }
                }
            }

            // Get station stop, recalculate with present speed if required
            else if (thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
            {
                float[] distancesM = CalculateDistancesToNextStation(StationStops[0], SpeedMpS, true);

                if (distancesM[1] - 300.0f > DistanceTravelledM) // Trigger point more than 300m away
                {
                    actionValid = false;
                    thisItem.RequiredDistance = distancesM[1];
                    thisItem.ActivateDistanceM = distancesM[0];
                    requiredActions.InsertAction(thisItem);

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "StationStop rescheduled for train " +
                        Number.ToString() + ", at " +
                        thisItem.ActivateDistanceM.ToString() + ", trigger at " +
                        thisItem.RequiredDistance.ToString() + " ( now at " +
                        PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                        FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "StationStop rescheduled for train " +
                            Number.ToString() + ", at " +
                            thisItem.ActivateDistanceM.ToString() + ", trigger at " +
                            thisItem.RequiredDistance.ToString() + " ( now at " +
                            PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                            FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
                else
                {
                    // Always copy active stop distance
                    thisItem.ActivateDistanceM = distancesM[0];
                }
            }

            EndProcessAction(actionValid, thisItem, actionCleared);
        }

        //  SPA: To be able to call it by AuxActionItems
        public void EndProcessAction(bool actionValid, AIActionItem thisItem, bool actionCleared)
        {
            // If still valid - check if at station and signal is exit signal
            // If so, use minimum distance of both items to ensure train stops in time for signal
            if (actionValid && nextActionInfo != null &&
                nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP &&
                thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
            {
                int signalIdent = thisItem.ActiveItem.ObjectDetails.thisRef;
                if (signalIdent == StationStops[0].ExitSignal)
                {
                    actionValid = false;
                    nextActionInfo.ActivateDistanceM = Math.Min(nextActionInfo.ActivateDistanceM, thisItem.ActivateDistanceM);
#if DEBUG_REPORTS
                    if (StationStops[0].ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Rejected : Train " +
                             Number.ToString() + " : signal " +
                             signalIdent.ToString() + " is exit signal for " +
                             StationStops[0].PlatformItem.Name + "\n");
                    }
                    else if (StationStops[0].ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Rejected : Train " +
                             Number.ToString() + " : signal " +
                             signalIdent.ToString() + " is exit signal for Waiting Point \n");
                    }
#endif
                    if (CheckTrain)
                    {
                        if (StationStops[0].ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Rejected : Train " +
                                 Number.ToString() + " : signal " +
                                 signalIdent.ToString() + " is exit signal for " +
                                 StationStops[0].PlatformItem.Name + "\n");
                        }
                        else if (StationStops[0].ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Rejected : Train " +
                                 Number.ToString() + " : signal " +
                                 signalIdent.ToString() + " is exit signal for Waiting Point \n");
                        }
                    }
                }
            }

            // If still valid, check if this action is end of route and actual next action is station stop - if so, reject
            if (actionValid && nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP &&
                thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE)
            {
                actionValid = false;
#if DEBUG_REPORTS
                    if (StationStops[0].ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Rejected : Train " +
                             Number.ToString() + " : end of route in favor of " +
                             StationStops[0].PlatformItem.Name + "\n");
                    }
#endif
                if (CheckTrain)
                {
                    if (StationStops[0].ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Rejected : Train " +
                             Number.ToString() + " : end of route in favor of " +
                             StationStops[0].PlatformItem.Name + "\n");
                    }
                }
            }

            // If still valid - check if actual next action is WP and signal is WP controlled signal
            // If so, use minimum distance of both items to ensure train stops in time for signal
            if (actionValid && nextActionInfo != null && nextActionInfo is AuxActionWPItem &&
                thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
            {
                if ((thisItem.ActiveItem.ObjectDetails.HasLockForTrain(Number, TCRoute.activeSubpath) && nextActionInfo.ActivateDistanceM - thisItem.ActivateDistanceM < 40) ||
                    nextActionInfo.ActivateDistanceM - thisItem.ActivateDistanceM < activityClearingDistanceM)
                {
                    actionValid = false;
                    nextActionInfo.ActivateDistanceM = Math.Min(nextActionInfo.ActivateDistanceM, thisItem.ActivateDistanceM);
                }
            }

            // If still valid - check if more severe as existing action
            bool earlier = false;

            if (actionValid)
            {
                if (nextActionInfo != null)
                {
                    if (thisItem.ActivateDistanceM < nextActionInfo.ActivateDistanceM)
                    {
                        if (thisItem.RequiredSpeedMpS <= nextActionInfo.RequiredSpeedMpS)
                        {
                            earlier = true;
                        }
                        else
                        {
                            // New requirement earlier with higher speed - check if enough braking distance remaining
                            float deltaTime = (thisItem.RequiredSpeedMpS - nextActionInfo.RequiredSpeedMpS) / MaxDecelMpSS;
                            float brakingDistanceM = (thisItem.RequiredSpeedMpS * deltaTime) + (0.5f * MaxDecelMpSS * deltaTime * deltaTime);

                            if (brakingDistanceM < (nextActionInfo.ActivateDistanceM - thisItem.ActivateDistanceM))
                            {
                                earlier = true;
                            }
                        }
                    }
                    else if (thisItem.RequiredSpeedMpS < nextActionInfo.RequiredSpeedMpS)
                    {
                        // New requirement further but with lower speed - check if enough braking distance left
                        float deltaTime = (nextActionInfo.RequiredSpeedMpS - thisItem.RequiredSpeedMpS) / MaxDecelMpSS;
                        float brakingDistanceM = (nextActionInfo.RequiredSpeedMpS * deltaTime) + (0.5f * MaxDecelMpSS * deltaTime * deltaTime);

                        if (brakingDistanceM > (thisItem.ActivateDistanceM - nextActionInfo.ActivateDistanceM))
                        {
                            earlier = true;
                        }
                    }

                    // If earlier: check if present action is station stop, new action is signal - if so, check is signal really in front of or behind station stop
                    if (earlier && thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP &&
                                 nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                    {
                        float newposition = thisItem.ActivateDistanceM + (0.75f * activityClearingDistanceM); // Correct with clearing distance - leave smaller gap
                        float actposition = nextActionInfo.ActivateDistanceM;

                        if (actposition < newposition) earlier = false;

                        if (!earlier && CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "allowing minimum gap : " + newposition.ToString() + " and " + actposition.ToString() + "\n");
                        }

                        // If still earlier: check if signal really beyond start of platform
                        if (earlier && (StationStops[0].DistanceToTrainM - thisItem.ActiveItem.distance_to_train) < StationStops[0].StopOffset)
                        {
                            earlier = false;
                            if (CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "station stop position corrected due to signal location ; was : "
                                    + StationStops[0].DistanceToTrainM.ToString() + " ; now is " + (thisItem.ActiveItem.distance_to_train - 1).ToString() + "\n");
                            }
                            StationStops[0].DistanceToTrainM = thisItem.ActiveItem.distance_to_train - 1;
                            nextActionInfo.ActivateDistanceM = thisItem.ActivateDistanceM - 1;
                        }
                    }

                    // Check if present action is signal and new action is station - if so, check actual position of signal in relation to stop
                    if (thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                    {
                        if (StationStops[0].DistanceToTrainM < nextActionInfo.ActiveItem.distance_to_train)
                        {
                            earlier = true;
                        }
                    }

                    // If not earlier and station stop and present action is signal stop: check if signal is hold signal, if so set station stop
                    // Set distance to signal if that is less than distance to platform to ensure trains stops at signal
                    if (!earlier && thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP &&
                               nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                    {
                        if (HoldingSignals.Contains(nextActionInfo.ActiveItem.ObjectDetails.thisRef))
                        {
                            earlier = true;
                            thisItem.ActivateDistanceM = Math.Min(nextActionInfo.ActivateDistanceM, thisItem.ActivateDistanceM);
                        }
                    }

                    // If not earlier and station stop and present action is end of route: favour station
                    if (!earlier && thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP &&
                               (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE || nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY))
                    {
                        earlier = true;
                        nextActionInfo.ActivateDistanceM = thisItem.ActivateDistanceM + 1.0f;
                    }

                    // If not earlier and is a waiting point and present action is signal stop: check if signal is locking signal, if so set waiting
                    // Set distance to signal if that is less than distance to WP to ensure trains stops at signal
                    if (!earlier && thisItem is AuxActionWPItem &&
                               nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                    {
                        // check if it is the the AI action is related to the signal linked to the WP
                        if ((nextActionInfo.ActiveItem.ObjectDetails.HasLockForTrain(Number, TCRoute.activeSubpath) && thisItem.ActivateDistanceM - nextActionInfo.ActivateDistanceM < 40) ||
                            thisItem.ActivateDistanceM - nextActionInfo.ActivateDistanceM < activityClearingDistanceM)
                        {
                            earlier = true;
                            thisItem.ActivateDistanceM = Math.Min(nextActionInfo.ActivateDistanceM, thisItem.ActivateDistanceM);
                        }
                    }

                    if (MovementState == AI_MOVEMENT_STATE.INIT_ACTION || MovementState == AI_MOVEMENT_STATE.HANDLE_ACTION) earlier = false;

                    // Reject if less severe (will be rescheduled if active item is cleared)
                    if (!earlier)
                    {
                        actionValid = false;

#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Rejected : Train " +
                             Number.ToString() + " : this " +
                             FormatStrings.FormatSpeed(thisItem.RequiredSpeedMpS, true) + " at " +
                             thisItem.ActivateDistanceM.ToString() + ", active " +
                             FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " at " +
                             nextActionInfo.ActivateDistanceM.ToString() + "\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Rejected : Train " +
                                 Number.ToString() + " : this " +
                                 FormatStrings.FormatSpeed(thisItem.RequiredSpeedMpS, true) + " at " +
                                 thisItem.ActivateDistanceM.ToString() + ", active " +
                                 FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " at " +
                                 nextActionInfo.ActivateDistanceM.ToString() + "\n");
                        }
                    }
                    else
                    {
#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Accepted : Train " +
                             Number.ToString() + " : this " +
                             FormatStrings.FormatSpeed(thisItem.RequiredSpeedMpS, true) + " at " +
                             thisItem.ActivateDistanceM.ToString() + ", active " +
                             FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " at " +
                             nextActionInfo.ActivateDistanceM.ToString() + "\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Accepted : Train " +
                                 Number.ToString() + " : this " +
                                 FormatStrings.FormatSpeed(thisItem.RequiredSpeedMpS, true) + " at " +
                                 thisItem.ActivateDistanceM.ToString() + ", active " +
                                 FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " at " +
                                 nextActionInfo.ActivateDistanceM.ToString() + "\n");
                        }
                    }
                }
            }

            // If still valid, set as action, set state to braking if still running
            var stationCancelled = false;
            if (actionValid)
            {
#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Validated\n");
#endif
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Validated\n");
                }
                if (thisItem.GetType().IsSubclassOf(typeof(AuxActionItem)))
                {
                    AuxActionItem action = thisItem as AuxActionItem;
                    AuxActionRef actionRef = action.ActionRef;
                    if (actionRef.IsGeneric)
                    {
                        nextGenAction = thisItem; // SPA: In order to manage GenericAuxAction without disturbing normal actions
                        requiredActions.Remove(thisItem);
                    }
                    else
                    {
                        if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                            stationCancelled = true;
                        nextActionInfo = thisItem;
                    }
                }
                else
                {
                    if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                        stationCancelled = true;
                    nextActionInfo = thisItem;
                }
                if (nextActionInfo.RequiredSpeedMpS == 0)
                {
                    NextStopDistanceM = thisItem.ActivateDistanceM - PresentPosition[0].DistanceTravelledM;
                    if (Simulator.PreUpdate && !(nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.AUX_ACTION && NextStopDistanceM > minCheckDistanceM))
                    {
                        AITrainBrakePercent = 100; // Because of short reaction time
                        AITrainThrottlePercent = 0;
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number.ToString() + " clamping brakes due to process action\n");
                        }
                    }
                }

#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number.ToString() +
                " , Present state : " + MovementState.ToString() + "\n");

#endif

                if (MovementState != AI_MOVEMENT_STATE.STATION_STOP &&
                    MovementState != AI_MOVEMENT_STATE.STOPPED &&
                    MovementState != AI_MOVEMENT_STATE.HANDLE_ACTION &&
                    MovementState != AI_MOVEMENT_STATE.FOLLOWING &&
                    MovementState != AI_MOVEMENT_STATE.TURNTABLE &&
                    MovementState != AI_MOVEMENT_STATE.BRAKING)
                {
                    MovementState = AI_MOVEMENT_STATE.BRAKING;
                    Alpha10 = PreUpdate & !Simulator.TimetableMode ? 2 : 10;
#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number.ToString() +
                    " , new state : " + MovementState.ToString() + "\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number.ToString() +
                        " , new state : " + MovementState.ToString() + "\n");
                    }
                }
                else if (MovementState == AI_MOVEMENT_STATE.STOPPED && nextActionInfo.GetType().IsSubclassOf(typeof(AuxActionItem)))
                {
                    MovementState = AI_MOVEMENT_STATE.BRAKING;
                    Alpha10 = PreUpdate ? 2 : 10;
                }
                else
                {
#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number.ToString() +
                    " , unchanged \n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number.ToString() +
                        " , unchanged \n");
                    }
                }
            }
            else
            {
#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Action Rejected\n");
#endif
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Action Rejected\n");
                }
            }

            if (actionCleared)
            {
#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Action Cleared\n");
#endif
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Action Cleared\n");
                }

                // Reset actions - ensure next action is validated
                ResetActions(true);
            }
            else if (stationCancelled)
            {
                SetNextStationAction(false);
            }
        }

        public bool TrainHasPower()
        {
            foreach (var car in Cars)
            {
                if (car is MSTSLocomotive)
                {
                    return true;
                }
            }

            return false;
        }

        //================================================================================================//
        //
        // Extra actions when alternative route is set
        //
        public override void SetAlternativeRoute_pathBased(int startElementIndex, int altRouteIndex, SignalObject nextSignal)
        {
            base.SetAlternativeRoute_pathBased(startElementIndex, altRouteIndex, nextSignal);

            // Reset actions to recalculate distances
            ResetActions(true);
        }

        public override void SetAlternativeRoute_locationBased(int startSectionIndex, DeadlockInfo sectionDeadlockInfo, int usedPath, SignalObject nextSignal)
        {
            base.SetAlternativeRoute_locationBased(startSectionIndex, sectionDeadlockInfo, usedPath, nextSignal);

            // Reset actions to recalculate distances
            ResetActions(true);
        }

        //================================================================================================//
        //
        // Find station on alternative route
        //
        public override StationStop SetAlternativeStationStop(StationStop orgStop, TCSubpathRoute newRoute)
        {
            var newStop = base.SetAlternativeStationStop(orgStop, newRoute);
            if (newStop != null)
            {
                // Modify PlatformStartID in ServiceList
                var actualServiceItem = ServiceDefinition.ServiceList.Find(si => si.PlatformStartID == orgStop.PlatformReference);
                if (actualServiceItem != null)
                {
                    actualServiceItem.PlatformStartID = newStop.PlatformReference;
                }
            }
            return newStop;
        }

        //================================================================================================//
        /// <summary>
        /// Add movement status to train status string
        /// Update the string for 'TextPageDispatcherInfo' in case of AI train.
        /// Modifiy fields 4, 5, 7, 8 & 11
        /// 4   AIMode :
        ///     INI     : AI is in INIT mode
        ///     STC     : AI is static
        ///     STP     : AI is Stopped
        ///     BRK     : AI Brakes
        ///     ACC     : AI do acceleration
        ///     FOL     : AI follows
        ///     RUN     : AI is running
        ///     EOP     : AI approch and of path
        ///     STA     : AI is on Station Stop
        ///     WTP     : AI is on Waiting Point
        /// 5   AI Data :
        ///     000&000     : for mode INI, BRK, ACC, FOL, RUN or EOP
        ///     HH:mm:ss    : for mode STA or WTP with actualDepart or DepartTime
        ///                 : for mode STC with Start Time Value
        ///     ..:..:..    : For other case
        /// 7   Next Action : 
        ///     SPDL    :   Speed limit
        ///     SIGL    :   Speed signal
        ///     STOP    :   Signal STOP
        ///     REST    :   Signal RESTRICTED
        ///     EOA     :   End Of Authority
        ///     STAT    :   Station Stop
        ///     TRAH    :   Train Ahead
        ///     EOR     :   End Of Route
        ///     NONE    :   None
        /// 8   Distance :
        ///     Distance to
        /// 11  Train Name
        /// </summary>
        public virtual String[] AddMovementState(String[] stateString, bool metric)
        {
            String[] retString = new String[stateString.Length];
            stateString.CopyTo(retString, 0);

            string movString = "";
            switch (MovementState)
            {
                case AI_MOVEMENT_STATE.INIT:
                    movString = "INI ";
                    break;
                case AI_MOVEMENT_STATE.AI_STATIC:
                    movString = "STC ";
                    break;
                case AI_MOVEMENT_STATE.STOPPED:
                    movString = "STP ";
                    break;
                case AI_MOVEMENT_STATE.STATION_STOP:
                    break;   // set below
                case AI_MOVEMENT_STATE.BRAKING:
                    movString = "BRK ";
                    break;
                case AI_MOVEMENT_STATE.ACCELERATING:
                    movString = "ACC ";
                    break;
                case AI_MOVEMENT_STATE.FOLLOWING:
                    movString = "FOL ";
                    break;
                case AI_MOVEMENT_STATE.RUNNING:
                    movString = "RUN ";
                    break;
                case AI_MOVEMENT_STATE.APPROACHING_END_OF_PATH:
                    movString = "EOP ";
                    break;
                case AI_MOVEMENT_STATE.SUSPENDED:
                    movString = "SUS ";
                    break;
                case AI_MOVEMENT_STATE.FROZEN:
                    movString = "FRO ";
                    break;
                case AI_MOVEMENT_STATE.STOPPED_EXISTING:
                    movString = "STE ";
                    break;
            }

            string abString = AITrainThrottlePercent.ToString("000");
            abString = String.Concat(abString, "&", AITrainBrakePercent.ToString("000"));

            if (MovementState == AI_MOVEMENT_STATE.STATION_STOP)
            {
                DateTime baseDT = new DateTime();
                if (StationStops[0].ActualDepart > 0)
                {
                    DateTime depTime = baseDT.AddSeconds(StationStops[0].ActualDepart);
                    abString = depTime.ToString("HH:mm:ss");
                }
                else if (StationStops[0].DepartTime > 0)
                {
                    DateTime depTime = baseDT.AddSeconds(StationStops[0].DepartTime);
                    abString = depTime.ToString("HH:mm:ss");
                }
                else
                {
                    abString = "..:..:..";
                }

                if (StationStops[0].ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                {
                    movString = "STA";
                }
                else if (StationStops[0].ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                {
                    movString = "WTP";
                }
            }
            else if (MovementState == AI_MOVEMENT_STATE.HANDLE_ACTION)
            {
                if (nextActionInfo != null && nextActionInfo.GetType().IsSubclassOf(typeof(AuxActionItem)))
                {
                    AuxActionRef actionRef = ((AuxActionItem)nextActionInfo).ActionRef;
                    if (actionRef.IsGeneric)
                    {
                        movString = "Gen";
                    }
                    else if (AuxActionsContain[0] != null && ((AIAuxActionsRef)AuxActionsContain[0]).NextAction == AuxActionRef.AUX_ACTION.WAITING_POINT)
                    {
                        movString = "WTP";
                        DateTime baseDT = new DateTime();
                        if (((AuxActionWPItem)nextActionInfo).ActualDepart > 0)
                        {
                            DateTime depTime = baseDT.AddSeconds(((AuxActionWPItem)nextActionInfo).ActualDepart);
                            abString = depTime.ToString("HH:mm:ss");
                        }
                        else
                        {
                            abString = "..:..:..";
                        }
                    }
                }
                else if (AuxActionsContain.specRequiredActions.Count > 0 && AuxActionsContain.specRequiredActions.First.Value is AuxActSigDelegate &&
                     (AuxActionsContain.specRequiredActions.First.Value as AuxActSigDelegate).currentMvmtState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION)
                {
                    movString = "WTS";
                    DateTime baseDT = new DateTime();
                    DateTime depTime = baseDT.AddSeconds((AuxActionsContain.specRequiredActions.First.Value as AuxActSigDelegate).ActualDepart);
                    abString = depTime.ToString("HH:mm:ss");
                }

            }
            else if (MovementState == AI_MOVEMENT_STATE.AI_STATIC)
            {
                if (StartTime.HasValue)
                {
                    long startNSec = (long)(StartTime.Value * Math.Pow(10, 7));
                    DateTime startDT = new DateTime(startNSec);
                    abString = startDT.ToString("HH:mm:ss");
                }
                else
                {
                    abString = "--------";
                }
            }

            string nameString = Name.Substring(0, Math.Min(Name.Length, 7));
            string actString = "";

            if (nextActionInfo != null)
            {
                switch (nextActionInfo.NextAction)
                {
                    case AIActionItem.AI_ACTION_TYPE.SPEED_LIMIT:
                        actString = "SPDL";
                        break;
                    case AIActionItem.AI_ACTION_TYPE.SPEED_SIGNAL:
                        actString = "SIGL";
                        break;
                    case AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP:
                        actString = "STOP";
                        break;
                    case AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED:
                        actString = "REST";
                        break;
                    case AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY:
                        actString = "EOA ";
                        break;
                    case AIActionItem.AI_ACTION_TYPE.STATION_STOP:
                        actString = "STAT";
                        break;
                    case AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD:
                        actString = "TRAH";
                        break;
                    case AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE:
                        actString = "EOR ";
                        break;
                    case AIActionItem.AI_ACTION_TYPE.NONE:
                        actString = "NONE";
                        break;
                    case AIActionItem.AI_ACTION_TYPE.REVERSAL:
                        actString = "REV";
                        break;
                    default:
                        actString = "AUX";
                        break;
                }

                retString[7] = actString;
                retString[8] = FormatStrings.FormatDistance(
                        nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM, metric);

            }

            retString[4] = movString;
            retString[5] = abString;
            retString[11] = nameString;

            return retString;
        }

        //================================================================================================//
        /// <summary>
        /// When in autopilot mode, switches to player control
        /// </summary>
        /// 
        public virtual bool SwitchToPlayerControl()
        {
            bool success = false;
            int leadLocomotiveIndex = -1;
            var j = 0;
            foreach (TrainCar car in Cars)
            {
                if (car is MSTSLocomotive)
                {
                    var loco = car as MSTSLocomotive;
                    loco.LocomotiveAxles.InitializeMoving();
                    loco.AntiSlip = false; // <CSComment> TODO Temporary patch until AntiSlip is re-implemented
                }
                if (car == Simulator.PlayerLocomotive) { leadLocomotiveIndex = j; }
                j++;
            }
            MSTSLocomotive lead = (MSTSLocomotive)Simulator.PlayerLocomotive;
            EqualReservoirPressurePSIorInHg = Math.Min(EqualReservoirPressurePSIorInHg, lead.TrainBrakeController.MaxPressurePSI);
            foreach (TrainCar car in Cars)
            {
                if (car.BrakeSystem is AirSinglePipe)
                {
                    ((AirSinglePipe)car.BrakeSystem).NormalizePressures(lead.TrainBrakeController.MaxPressurePSI);
                }
            }
            LeadLocomotiveIndex = leadLocomotiveIndex;
            Simulator.PlayerLocomotive.SwitchToPlayerControl();
            if (MovementState == AI_MOVEMENT_STATE.HANDLE_ACTION && nextActionInfo != null && nextActionInfo.GetType().IsSubclassOf(typeof(AuxActionItem))
                && AuxActionsContain[0] != null && ((AIAuxActionsRef)AuxActionsContain[0]).NextAction == AuxActionRef.AUX_ACTION.WAITING_POINT)
            {
                (AuxActionsContain.SpecAuxActions[0] as AIActionWPRef).keepIt.currentMvmtState = AI_MOVEMENT_STATE.HANDLE_ACTION;
            }
            TrainType = TRAINTYPE.AI_PLAYERDRIVEN;
            success = true;
            return success;
        }

        //================================================================================================//
        /// <summary>
        /// When in player mode, switches to autopilot control
        /// </summary>
        /// 
        public virtual bool SwitchToAutopilotControl()
        {
            bool success = false;
            // MUDirection set within following method call
            Simulator.PlayerLocomotive.SwitchToAutopilotControl();
            LeadLocomotive = null;
            LeadLocomotiveIndex = -1;
            TrainType = TRAINTYPE.AI_PLAYERHOSTING;
            InitializeBrakes();
            foreach (TrainCar car in Cars)
            {
                if (car is MSTSLocomotive)
                {
                    var loco = car as MSTSLocomotive;
                    if (loco.EngineBrakeController != null) loco.SetEngineBrakePercent(0);
                    if (loco.DynamicBrakeController != null) loco.DynamicBrakePercent = -1;
                }
            }

            if (FirstCar != null)
            {
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
                if (FirstCar is MSTSLocomotive)
                    ((MSTSLocomotive)FirstCar).SetTrainBrakePercent(AITrainBrakePercent);
                if (Simulator.PlayerLocomotive != null && FirstCar != Simulator.PlayerLocomotive)
                {
                    Simulator.PlayerLocomotive.BrakeSystem.AISetPercent(AITrainBrakePercent);
                    ((MSTSLocomotive)Simulator.PlayerLocomotive).SetTrainBrakePercent(AITrainBrakePercent);
                }
            }
            ResetActions(true, true);
            if (SpeedMpS != 0) MovementState = AI_MOVEMENT_STATE.BRAKING;
            else if (this == Simulator.OriginalPlayerTrain && Simulator.ActivityRun != null && Simulator.ActivityRun.Current is ActivityTaskPassengerStopAt && ((ActivityTaskPassengerStopAt)Simulator.ActivityRun.Current).IsAtStation(this) &&
                ((ActivityTaskPassengerStopAt)Simulator.ActivityRun.Current).BoardingS > 0)
            {
                StationStops[0].ActualDepart = (int)((ActivityTaskPassengerStopAt)Simulator.ActivityRun.Current).BoardingEndS;
                StationStops[0].ActualArrival = -(int)(new DateTime().Add(TimeSpan.FromSeconds(0.0)) - ((ActivityTaskPassengerStopAt)Simulator.ActivityRun.Current).ActArrive).Value.TotalSeconds;
                MovementState = AI_MOVEMENT_STATE.STATION_STOP;
            }
            else
            {
                MovementState = this != Simulator.OriginalPlayerTrain && AtStation
                    ? AI_MOVEMENT_STATE.STATION_STOP
                    : Math.Abs(SpeedMpS) <= 0.1f && ((AuxActionsContain.SpecAuxActions.Count > 0 && AuxActionsContain.SpecAuxActions[0] is AIActionWPRef && (AuxActionsContain.SpecAuxActions[0] as AIActionWPRef).keepIt != null &&
                                            (AuxActionsContain.SpecAuxActions[0] as AIActionWPRef).keepIt.currentMvmtState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION) || (nextActionInfo is AuxActionWPItem &&
                                                    MovementState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION))
                                    ? AI_MOVEMENT_STATE.HANDLE_ACTION
                                    : AI_MOVEMENT_STATE.STOPPED;
            }
            success = true;
            return success;
        }


#if WITH_PATH_DEBUG 
        //================================================================================================//
        /// <summary>
        /// AddPathInfo:  Used to construct a single line for path debug in HUD Windows
        /// </summary>

        public String[] AddPathInfo(String[] stateString, bool metric)
        {
            String[] retString = this.TCRoute.GetTCRouteInfo(stateString, PresentPosition[0]);

            retString[1] = currentAIState;
            return (retString);
        }

        public String[] GetActionStatus(bool metric)
        {
            int iColumn = 0;

            string[] statusString = new string[2];

            //  "Train"
            statusString[0] = Number.ToString();
            iColumn++;

            //  "Action"
            statusString[1] = "Actions: ";
            foreach (var action in requiredActions)
            {
                statusString[1] = String.Concat(statusString[1], showActionInfo(action));
            }
            statusString[1] = String.Concat(statusString[1], "NextAction->", showActionInfo(nextActionInfo));
            return statusString;
        }

        String showActionInfo(Train.DistanceTravelledItem action)
        {
            string actionString = String.Empty;
            //actionString = string.Concat(actionString, "Actions:");
            if (action == null)
                return "";

            if (action.GetType() == typeof(ClearSectionItem))
            {
                ClearSectionItem TrainAction = action as ClearSectionItem;
                //actionString = String.Concat(actionString, " ClearSection(", action.RequiredDistance.ToString("F0"), "):");
                actionString = String.Concat(actionString, " CLR Section(", TrainAction.TrackSectionIndex, "):");
            }
            else if (action.GetType() == typeof(ActivateSpeedLimit))
            {
                actionString = String.Concat(actionString, " ASL(", action.RequiredDistance.ToString("F0"), "m):");
            }

            else if (action.GetType() == typeof(AIActionItem) || action.GetType().IsSubclassOf(typeof(AuxActionItem)))
            {
                AIActionItem AIaction = action as AIActionItem;
                {
                    switch (AIaction.NextAction)
                    {
                        case AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY:
                            actionString = String.Concat(actionString, " EOA(", NextStopDistanceM.ToString("F0"), "m):");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE:
                            actionString = String.Concat(actionString, " EOR(", NextStopDistanceM.ToString("F0"), "m):");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.REVERSAL:
                            actionString = String.Concat(actionString, " REV(", NextStopDistanceM.ToString("F0"), "m):");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED:
                            actionString = String.Concat(actionString, " SAR(", NextStopDistanceM.ToString("F0"), "m):");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP:
                            string infoSignal = "";
                            if (AIaction.ActiveItem.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
                            {
                                infoSignal = AIaction.ActiveItem.signal_state.ToString();
                                infoSignal = String.Concat(infoSignal, ",", AIaction.ActiveItem.ObjectDetails.blockState.ToString());
                            }
                            actionString = String.Concat(actionString, " SAS(", infoSignal, "):");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.SPEED_LIMIT:
                            actionString = String.Concat(actionString, " SL(", NextStopDistanceM.ToString("F0"), "m):");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.SPEED_SIGNAL:
                            actionString = String.Concat(actionString, " Speed(", NextStopDistanceM.ToString("F0"), "m):");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.STATION_STOP:
                            actionString = String.Concat(actionString, " StationStop(", NextStopDistanceM.ToString("F0"), "m):");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD:
                            float diff = AIaction.ActivateDistanceM - AIaction.InsertedDistanceM;
                            actionString = String.Concat(actionString, " TrainAhead(", NextStopDistanceM.ToString("F0"), "m):");
                            break;
                        case AIActionItem.AI_ACTION_TYPE.NONE:
                            actionString = String.Concat(actionString, " None(", NextStopDistanceM.ToString("F0"), "m):");
                            break;

                        case AIActionItem.AI_ACTION_TYPE.AUX_ACTION:
                            string coord = String.Concat("X:", this.FrontTDBTraveller.X.ToString(), ", Z:", this.FrontTDBTraveller.Z.ToString());
                            actionString = String.Concat(actionString, AIaction.AsString(this), NextStopDistanceM.ToString("F0"), "m):", coord);
                            //actionString = String.Concat(actionString, " AUX(", NextStopDistanceM.ToString("F0"), "m):");
                            break;

                    }
                }
            }

            return (actionString);
        }
#endif

        //================================================================================================//
        /// <summary>
        /// Check on station tasks, required when player train is not original player train
        /// </summary>
        public override void CheckStationTask()
        {
            // If at station
            if (AtStation)
            {
                int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
                int eightHundredHours = 8 * 3600;
                int sixteenHundredHours = 16 * 3600;

                // If moving, set departed
                if (Math.Abs(SpeedMpS) > 0)
                {
                    if (TrainType != TRAINTYPE.AI_PLAYERHOSTING)
                    {
                        StationStops[0].ActualDepart = presentTime;
                        StationStops[0].Passed = true;
                        Delay = TimeSpan.FromSeconds((presentTime - StationStops[0].DepartTime) % (24 * 3600));
                        PreviousStop = StationStops[0].CreateCopy();
                        StationStops.RemoveAt(0);
                    }
                    AtStation = false;
                    MayDepart = false;
                    DisplayMessage = "";
                }
                else
                {
                    {
                        int remaining;
                        if (StationStops.Count == 0)
                        {
                            remaining = 0;
                        }
                        else
                        {
                            int actualDepart = StationStops[0].ActualDepart;
                            int correctedTime = presentTime;
                            if (presentTime > sixteenHundredHours && StationStops[0].DepartTime < eightHundredHours)
                            {
                                correctedTime = presentTime - (24 * 3600); // Correct to time before midnight (negative value!)
                            }

                            remaining = actualDepart - correctedTime;
                        }

                        // Set display text color
                        DisplayColor = remaining < 1 ? Color.LightGreen : remaining < 11 ? new Color(255, 255, 128) : Color.White;

                        // Clear holding signal
                        if (remaining < (IsActualPlayerTrain ? 120 : 2) && remaining > 0 && StationStops[0].ExitSignal >= 0) // Within two minutes of departure and hold signal?
                        {
                            HoldingSignals.Remove(StationStops[0].ExitSignal);

                            if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
                            {
                                SignalObject nextSignal = signalRef.SignalObjects[StationStops[0].ExitSignal];
                                nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null);
                            }
                            StationStops[0].ExitSignal = -1;
                        }

                        // Check departure time
                        if (remaining <= 0)
                        {
                            if (!MayDepart)
                            {
                                float distanceToNextSignal = -1;
                                if (NextSignalObject[0] != null) distanceToNextSignal = NextSignalObject[0].DistanceTo(FrontTDBTraveller);
                                // Check if signal ahead is cleared - if not, do not allow depart
                                if (NextSignalObject[0] != null && distanceToNextSignal >= 0 && distanceToNextSignal < 300 &&
                                        NextSignalObject[0].this_sig_lr(MstsSignalFunction.NORMAL) == MstsSignalAspect.STOP
                                    && NextSignalObject[0].hasPermission != SignalObject.Permission.Granted)
                                {
                                    DisplayMessage = Simulator.Catalog.GetString("Passenger boarding completed. Waiting for signal ahead to clear.");
                                }
                                else
                                {
                                    MayDepart = true;
                                    DisplayMessage = Simulator.Catalog.GetString("Passenger boarding completed. You may depart now.");
                                    Simulator.SoundNotify = Event.PermissionToDepart;
                                }
                            }
                        }
                        else
                        {
                            DisplayMessage = Simulator.Catalog.GetStringFmt("Passenger boarding completes in {0:D2}:{1:D2}",
                                remaining / 60, remaining % 60);
                        }
                    }
                }
            }
            else
            {
                // If stations to be checked
                if (StationStops.Count > 0)
                {
                    // Check if stopped at station
                    if (Math.Abs(SpeedMpS) == 0.0f)
                    {
                        AtStation = IsAtPlatform();
                        if (AtStation)
                        {
                            int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
                            StationStops[0].ActualArrival = presentTime;
                            StationStops[0].CalculateDepartTime(presentTime, this);
                        }
                    }
                    else if (ControlMode == TRAIN_CONTROL.AUTO_NODE || ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
                    {
                        // Check if station missed : station must be at least 250m. behind us
                        bool missedStation = IsMissedPlatform(250);

                        if (missedStation)
                        {
                            PreviousStop = StationStops[0].CreateCopy();
                            if (TrainType != TRAINTYPE.AI_PLAYERHOSTING) StationStops.RemoveAt(0);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Restarts waiting train due to event triggered by player train
        /// </summary>
        public void RestartWaitingTrain(RestartWaitingTrain restartWaitingTrain)
        {
            var delayToRestart = restartWaitingTrain.DelayToRestart;
            var matchingWPDelay = restartWaitingTrain.MatchingWPDelay;
            int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
            var roughActualDepart = presentTime + delayToRestart;
            if (MovementState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION && (((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).OriginalDelay == matchingWPDelay ||
                (AuxActionsContain.specRequiredActions.Count > 0 && ((AuxActSigDelegate)AuxActionsContain.specRequiredActions.First.Value).currentMvmtState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION &&
                (((AuxActSigDelegate)AuxActionsContain.specRequiredActions.First.Value).ActionRef as AIActSigDelegateRef).Delay == matchingWPDelay)))
            {
                if (((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay >= 30000 && ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay < 32400)
                {
                    // Absolute WP, use minutes as unit of measure
                    (nextActionInfo as AuxActionWPItem).ActualDepart = (roughActualDepart / 60 * 60) + (roughActualDepart % 60 == 0 ? 0 : 60);
                    // Compute hrs and minutes
                    var hrs = (nextActionInfo as AuxActionWPItem).ActualDepart / 3600;
                    var minutes = ((nextActionInfo as AuxActionWPItem).ActualDepart - (hrs * 3600)) / 60;
                    ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay = 30000 + minutes + (hrs * 100);
                    (nextActionInfo as AuxActionWPItem).SetDelay(30000 + minutes + (hrs * 100));
                }
                else
                {
                    (nextActionInfo as AuxActionWPItem).ActualDepart = roughActualDepart;
                    ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay = delayToRestart;
                    (nextActionInfo as AuxActionWPItem).SetDelay(delayToRestart);
                }
                if (((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).LinkedAuxAction)
                {
                    // Also a signal is connected with this WP
                    if (AuxActionsContain.specRequiredActions.Count > 0 && AuxActionsContain.specRequiredActions.First.Value is AuxActSigDelegate)
                    {
                        // If should be true only for absolute WPs, where the linked aux action is started in parallel
                        (AuxActionsContain.specRequiredActions.First.Value as AuxActSigDelegate).ActualDepart = (nextActionInfo as AuxActionWPItem).ActualDepart;
                        ((AuxActionsContain.specRequiredActions.First.Value as AuxActSigDelegate).ActionRef as AIActSigDelegateRef).Delay = ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay;
                    }
                }

            }
            else if (nextActionInfo != null & nextActionInfo is AuxActionWPItem && ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay == matchingWPDelay)
            {
                var actualDepart = 0;
                var delay = 0;
                // Not yet at WP
                if (((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay >= 30000 && ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay < 32400)
                {
                    // Compute hrs and minutes
                    var hrs = roughActualDepart / 3600;
                    var minutes = (roughActualDepart - (hrs * 3600)) / 60;
                    ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay = 30000 + minutes + (hrs * 100);
                    (nextActionInfo as AuxActionWPItem).SetDelay(30000 + minutes + (hrs * 100));
                    if (AuxActionsContain.SpecAuxActions.Count > 0 && AuxActionsContain.SpecAuxActions[0] is AIActionWPRef)
                        (AuxActionsContain.SpecAuxActions[0] as AIActionWPRef).Delay = 30000 + minutes + (hrs * 100);
                    actualDepart = (roughActualDepart / 60 * 60) + (roughActualDepart % 60 == 0 ? 0 : 60);
                    delay = ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay;
                }
                else
                {
                    ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay = delayToRestart;
                    (nextActionInfo as AuxActionWPItem).SetDelay(delayToRestart);
                    actualDepart = roughActualDepart;
                    delay = 1;
                }
                if (((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).LinkedAuxAction)
                {
                    // Also a signal is connected with this WP
                    if (AuxActionsContain.specRequiredActions.Count > 0 && AuxActionsContain.specRequiredActions.First.Value is AuxActSigDelegate)
                    {
                        // If should be true only for absolute WPs, where the linked aux action is started in parallel
                        (AuxActionsContain.specRequiredActions.First.Value as AuxActSigDelegate).ActualDepart = actualDepart;
                        ((AuxActionsContain.specRequiredActions.First.Value as AuxActSigDelegate).ActionRef as AIActSigDelegateRef).Delay = ((nextActionInfo as AuxActionWPItem).ActionRef as AIActionWPRef).Delay;
                    }
                    if (AuxActionsContain.SpecAuxActions.Count > 1 && AuxActionsContain.SpecAuxActions[1] is AIActSigDelegateRef)
                        (AuxActionsContain.SpecAuxActions[1] as AIActSigDelegateRef).Delay = delay;
                }
            }
        }

    }


    /// <summary>
    /// Abstract class for a programmatic horn pattern sounded by the AI at level crossings.
    /// </summary>
    public abstract class AILevelCrossingHornPattern
    {
        /// <summary>
        /// Determines whether or not to create an <see cref="AIActionHornRef"/> based on the states of the train and the approaching level crossing.
        /// </summary>
        /// <param name="crossing">The level crossing group.</param>
        /// <param name="absoluteSpeedMpS">The absolute value of the current speed of the train.</param>
        /// <param name="distanceToCrossingM">The closest distance between the train (front or rear) and the level crossing (either end).</param>
        /// <returns></returns>
        public abstract bool ShouldActivate(LevelCrossing crossing, float absoluteSpeedMpS, float distanceToCrossingM);

        /// <summary>
        /// Sound the horn pattern using the provided locomotive. Called by <see cref="AuxActionHornItem"/>.
        /// </summary>
        /// <param name="locomotive">The locomotive to manipulate.</param>
        /// <param name="durationS">The duration ("delay") set for this horn event, if set.</param>
        /// <returns>On each iteration, set the locomotive's controls, then yield the clock time until the next step.</returns>
        public abstract IEnumerator<int> Execute(MSTSLocomotive locomotive, int? durationS);

        /// <summary>
        /// Get the horn pattern that corresponds to a <see cref="LevelCrossingHornPattern"/> value.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static AILevelCrossingHornPattern CreateInstance(LevelCrossingHornPattern type)
        {
            switch (type)
            {
                case LevelCrossingHornPattern.Single:
                    return new AILevelCrossingSingleHorn();
                case LevelCrossingHornPattern.US:
                    return new AILevelCrossingAmericanHorn();
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Save type (not state) information to a save file.
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            var type = this is AILevelCrossingSingleHorn
                ? LevelCrossingHornPattern.Single
                : this is AILevelCrossingAmericanHorn ? LevelCrossingHornPattern.US : throw new ArgumentException();
            outf.Write((int)type);
        }

        /// <summary>
        /// Restore type (not state) information from a save file.
        /// </summary>
        /// <param name="inf"></param>
        /// <returns></returns>
        public static AILevelCrossingHornPattern Restore(BinaryReader inf)
            => CreateInstance((LevelCrossingHornPattern)inf.ReadInt32());
    }


    /// <summary>
    /// Sound a single blast just before reaching the level crossing, with a slightly randomized duration, and stop the bell after 30 seconds if triggered.
    /// </summary>
    public class AILevelCrossingSingleHorn : AILevelCrossingHornPattern
    {
        /// <summary>
        /// Sound the horn within 6s of the crossing.
        /// </summary>
        public override bool ShouldActivate(LevelCrossing crossing, float absoluteSpeedMpS, float distanceToCrossingM)
            => distanceToCrossingM / absoluteSpeedMpS < 6f;

        public override IEnumerator<int> Execute(MSTSLocomotive locomotive, int? durationS)
        {
            if (!durationS.HasValue)
            {
                // Sound the horn for a pseudorandom period of seconds between 2 and 5.
                durationS = (DateTime.Now.Millisecond % 10 / 3) + 2;
            }

            locomotive.ManualHorn = true;
            yield return durationS.Value;

            locomotive.ManualHorn = false;

            if (locomotive.DoesHornTriggerBell)
            {
                yield return 30 - durationS.Value;
                locomotive.BellState = MSTSLocomotive.SoundState.Stopped;
            }
        }
    }

    /// <summary>
    /// Sound the long-long-short-long pattern used in the United States and Canada, and stop the bell after 30 seconds if triggered.
    /// </summary>
    public class AILevelCrossingAmericanHorn : AILevelCrossingHornPattern
    {
        /// <summary>
        /// Sound the horn within 19s of crossing to accomodate the full sequence.
        /// </summary>
        public override bool ShouldActivate(LevelCrossing crossing, float absoluteSpeedMpS, float distanceToCrossingM)
            => distanceToCrossingM / absoluteSpeedMpS < 19f;

        /// <summary>
        /// This pattern ignores the supplied duration.
        /// </summary>
        public override IEnumerator<int> Execute(MSTSLocomotive locomotive, int? durationS)
        {
            locomotive.ManualHorn = true;
            yield return 3;

            locomotive.ManualHorn = false;
            yield return 2;

            locomotive.ManualHorn = true;
            yield return 3;

            locomotive.ManualHorn = false;
            yield return 2;

            locomotive.ManualHorn = true;
            yield return 0;

            locomotive.ManualHorn = false;
            yield return 1;

            locomotive.ManualHorn = true;
            yield return 8;

            locomotive.ManualHorn = false;

            if (locomotive.DoesHornTriggerBell)
            {
                yield return 11;
                locomotive.BellState = MSTSLocomotive.SoundState.Stopped;
            }
        }
    }

    //================================================================================================//
    /// <summary>
    /// AIActionItem class : class to hold info on next restrictive action
    /// </summary>
    public class AIActionItem : Train.DistanceTravelledItem
    {
        public float RequiredSpeedMpS;
        public float ActivateDistanceM;
        public float InsertedDistanceM;
        public ObjectItemInfo ActiveItem;
        public int ReqTablePath;

        public enum AI_ACTION_TYPE
        {
            SPEED_LIMIT,
            SPEED_SIGNAL,
            SIGNAL_ASPECT_STOP,
            SIGNAL_ASPECT_RESTRICTED,
            END_OF_AUTHORITY,
            STATION_STOP,
            TRAIN_AHEAD,
            END_OF_ROUTE,
            REVERSAL,
            AUX_ACTION,
            APPROACHING_MOVING_TABLE,
            NONE
        }

        public AI_ACTION_TYPE NextAction = AI_ACTION_TYPE.NONE;

        //================================================================================================//
        /// <summary>
        /// Constructor for AIActionItem
        /// </summary>
        public AIActionItem(ObjectItemInfo thisItem, AI_ACTION_TYPE thisAction)
        {
            ActiveItem = thisItem;
            NextAction = thisAction;
        }

        public void SetParam(float requiredDistance, float requiredSpeedMpS, float activateDistanceM, float insertedDistanceM)
        {
            RequiredDistance = requiredDistance;
            RequiredSpeedMpS = requiredSpeedMpS;
            ActivateDistanceM = activateDistanceM;
            InsertedDistanceM = insertedDistanceM;
        }

        //================================================================================================//
        //
        // Restore
        //
        public AIActionItem(BinaryReader inf, Signals signalRef)
        {
            RequiredDistance = inf.ReadSingle();
            RequiredSpeedMpS = inf.ReadSingle();
            ActivateDistanceM = inf.ReadSingle();
            InsertedDistanceM = inf.ReadSingle();
            ReqTablePath = inf.ReadInt32();

            bool validActiveItem = inf.ReadBoolean();

            if (validActiveItem)
            {
                ActiveItem = RestoreActiveItem(inf, signalRef);
            }

            NextAction = (AI_ACTION_TYPE)inf.ReadInt32();
        }

        public static ObjectItemInfo RestoreActiveItem(BinaryReader inf, Signals signalRef)
        {
            ObjectItemInfo thisInfo = new ObjectItemInfo(ObjectItemInfo.ObjectItemFindState.None)
            {
                ObjectType = (ObjectItemInfo.ObjectItemType)inf.ReadInt32(),
                ObjectState = (ObjectItemInfo.ObjectItemFindState)inf.ReadInt32()
            };

            int signalIndex = inf.ReadInt32();
            thisInfo.ObjectDetails = signalRef.SignalObjects[signalIndex];

            thisInfo.distance_found = inf.ReadSingle();
            thisInfo.distance_to_train = inf.ReadSingle();
            thisInfo.distance_to_object = inf.ReadSingle();

            thisInfo.speed_passenger = inf.ReadSingle();
            thisInfo.speed_freight = inf.ReadSingle();
            thisInfo.speed_flag = inf.ReadInt32();
            thisInfo.actual_speed = inf.ReadSingle();

            thisInfo.processed = inf.ReadBoolean();

            thisInfo.signal_state = MstsSignalAspect.UNKNOWN;
            if (thisInfo.ObjectDetails.Type == SignalObjectType.Signal)
            {
                thisInfo.signal_state = thisInfo.ObjectDetails.this_sig_lr(MstsSignalFunction.NORMAL);
            }

            return thisInfo;
        }

        //================================================================================================//
        //
        // Save
        //
        public void SaveItem(BinaryWriter outf)
        {
            outf.Write(RequiredSpeedMpS);
            outf.Write(ActivateDistanceM);
            outf.Write(InsertedDistanceM);
            outf.Write(ReqTablePath);

            if (ActiveItem == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                SaveActiveItem(outf, ActiveItem);
            }

            outf.Write((int)NextAction);
        }

        public static void SaveActiveItem(BinaryWriter outf, ObjectItemInfo ActiveItem)
        {
            outf.Write((int)ActiveItem.ObjectType);
            outf.Write((int)ActiveItem.ObjectState);

            outf.Write(ActiveItem.ObjectDetails.thisRef);

            outf.Write(ActiveItem.distance_found);
            outf.Write(ActiveItem.distance_to_train);
            outf.Write(ActiveItem.distance_to_object);

            outf.Write(ActiveItem.speed_passenger);
            outf.Write(ActiveItem.speed_freight);
            outf.Write(ActiveItem.speed_flag);
            outf.Write(ActiveItem.actual_speed);

            outf.Write(ActiveItem.processed);
        }

        //================================================================================================//
        //
        // Generic Handler for all derived class
        //
        public virtual bool ValidAction(Train thisTrain)
        {
            return false;
        }

        public virtual AITrain.AI_MOVEMENT_STATE InitAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            return movementState;
        }

        public virtual AITrain.AI_MOVEMENT_STATE HandleAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            return movementState;
        }

        public virtual AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            return movementState;
        }

        public virtual AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime)
        {
            return AITrain.AI_MOVEMENT_STATE.AI_STATIC;
        }

        public virtual string AsString(AITrain thisTrain)
        {
            return " ??(";
        }
    }
}
