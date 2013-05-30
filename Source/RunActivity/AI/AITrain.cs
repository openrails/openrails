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
// DEBUG flag for debug prints

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using MSTS;
using ORTS.Popups;

namespace ORTS
{
    public class AITrain : Train
    {
        public int UiD;
        public string Name;
        public AIPath Path = null;

        public bool CoupleOnNextStop = false;            // true if cars at next stop to couple onto
        public float MaxDecelMpSSP = 1.0f;               // maximum decelleration
        public float MaxAccelMpSSP = 1.0f;               // maximum accelleration
        public float MaxDecelMpSSF = 0.8f;               // maximum decelleration
        public float MaxAccelMpSSF = 0.5f;               // maximum accelleration
        public float MaxDecelMpSS = 0.5f;                // maximum decelleration
        public float MaxAccelMpSS = 1.0f;                // maximum accelleration
        public float Efficiency = 1.0f;                  // train efficiency
        public float LastSpeedMpS;                       // previous speed
        public int Alpha10 = 10;                         // 10*alpha

        public bool PreUpdate = false;                   // pre update state
        public AIActionItem nextActionInfo = null;       // no next action
        public float NextStopDistanceM = 0;              // distance to next stop node
        public int StartTime;                            // starting time

        public enum AI_MOVEMENT_STATE
        {
            INIT,
            STOPPED,
            STATION_STOP,
            BRAKING,
            ACCELERATING,
            FOLLOWING,
            RUNNING,
            APPROACHING_END_OF_PATH
        }

        public AI_MOVEMENT_STATE MovementState = AI_MOVEMENT_STATE.INIT;  // actual movement state

        public enum AI_START_MOVEMENT
        {
            SIGNAL_CLEARED,
            SIGNAL_RESTRICTED,
            FOLLOW_TRAIN,
            END_STATION_STOP,
            NEW,
            PATH_ACTION
        }

        public AI AI;

        static float keepDistanceStatTrainM_P = 20.0f;  // stay 20m behind stationary train (pass)
        static float keepDistanceStatTrainM_F = 50.0f;  // stay 50m behind stationary train (freight)
        static float followDistanceStatTrainM = 30.0f;  // min dist for starting to follow
        static float keepDistanceMovingTrainM = 300.0f; // stay 300m behind moving train
        static float creepSpeedMpS = 2.5f;              // speed for creeping up behind train or upto signal
        static float maxFollowSpeedMpS = 15.0f;         // max. speed when following
        static float hysterisMpS = 0.5f;                // speed hysteris value to avoid instability
        static float clearingDistanceM = 30.0f;         // clear distance to stopping point
        static float signalApproachDistanceM = 20.0f;   // final approach to signal

        //================================================================================================//
        /// <summary>
        /// Constructor
        /// <\summary>

        public AITrain(Simulator simulator, int uid, AI ai, AIPath path, int start, float efficiency,
                string name, Traffic_Service_Definition trafficService)
            : base(simulator)
        {
            UiD = uid;
            AI = ai;
            Path = path;
            TrainType = TRAINTYPE.AI_NOTSTARTED;
            StartTime = start;
            Efficiency = efficiency;
            Name = String.Copy(name);
            TrafficService = trafficService;
        }

        //================================================================================================//
        /// <summary>
        /// convert route and build station list
        /// <\summary>

        public void CreateRoute()
        {
            if (Path != null)
            {
                SetRoutePath(Path);
            }
            else
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
                float offset = PresentPosition[1].TCOffset;

                ValidRoute[0] = signalRef.BuildTempRoute(this, thisSection.Index, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, Length, false, true, true);
            }
        }

        //================================================================================================//
        /// <summary>
        /// convert route and build station list
        /// <\summary>

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
                float offset = PresentPosition[1].TCOffset;

                ValidRoute[0] = signalRef.BuildTempRoute(this, thisSection.Index, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, Length, false, true, true);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Restore
        /// <\summary>

        public AITrain(Simulator simulator, BinaryReader inf)
            : base(simulator, inf)
        {
            UiD = inf.ReadInt32();
            Name = inf.ReadString();
            CoupleOnNextStop = inf.ReadBoolean();
            MaxDecelMpSS = inf.ReadSingle();
            MaxAccelMpSS = inf.ReadSingle();
            StartTime = inf.ReadInt32();

            Alpha10 = inf.ReadInt32();

            MovementState = (AI_MOVEMENT_STATE)inf.ReadInt32();

            nextActionInfo = null;
            if (TrainType != TRAINTYPE.AI_NOTSTARTED)
            {
                ResetActions(true);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Save
        /// <\summary>

        public override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(UiD);
            outf.Write(Name);
            outf.Write(CoupleOnNextStop);
            outf.Write(MaxDecelMpSS);
            outf.Write(MaxAccelMpSS);
            outf.Write(StartTime);
            outf.Write(Alpha10);

            outf.Write((int)MovementState);
        }

        //================================================================================================//
        /// <summary>
        /// Post Init (override from Train)
        ///           perform all actions required to start
        /// </summary>

        public override bool PostInit()
        {

#if DEBUG_CHECKTRAIN
            if (Number == 1)
            {
                CheckTrain = true;
            }
#endif
            // check deadlocks

            CheckDeadlock(ValidRoute[0], Number);

            // set initial position and state

            bool atStation = false;
            bool validPosition = InitialTrainPlacement();     // Check track and if clear, set occupied

            if (validPosition)
            {
                if (IsFreight)
                {
                    MaxAccelMpSS = MaxAccelMpSSF;  // set freigth accel and decel
                    MaxDecelMpSS = MaxAccelMpSSF;
                }
                else
                {
                    MaxAccelMpSS = MaxAccelMpSSP;  // set passenger accel and decel
                    MaxDecelMpSS = MaxDecelMpSSP;
                    if (TrainMaxSpeedMpS > 40.0f)
                    {
                        MaxDecelMpSS = 1.5f * MaxDecelMpSSP;  // higher decel for high speed trains
                    }
                    if (TrainMaxSpeedMpS > 55.0f)
                    {
                        MaxDecelMpSS = 2.5f * MaxDecelMpSSP;  // higher decel for very high speed trains
                    }
                }

                BuildWaitingPointList(clearingDistanceM);
                BuildStationList(clearingDistanceM);

                StationStops.Sort();

                InitializeSignals(false);           // Get signal information
                TCRoute.SetReversalOffset(Length);  // set reversal information for first subpath
                SetEndOfRouteAction();              // set action to ensure train stops at end of route

                // check if train starts at station stop

                if (StationStops.Count > 0)
                {
                    atStation = CheckInitialStation();
                }

                if (!atStation)
                {
                    if (StationStops.Count > 0)
                        SetNextStationAction();               // set station details
                    MovementState = AI_MOVEMENT_STATE.INIT;   // start in STOPPED mode to collect info
                }
            }

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Train : " + Number.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Name  : " + Name + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Frght : " + IsFreight.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Length: " + Length.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "MaxSpd: " + TrainMaxSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Start : " + StartTime.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "State : " + MovementState.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Sttion: " + atStation.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "ValPos: " + validPosition.ToString() + "\n");
            }

            return (validPosition);
        }

        //================================================================================================//
        /// <summary>
        /// Check initial station
        /// <\summary>

        public bool CheckInitialStation()
        {
            bool atStation = false;

            // get station details

            StationStop thisStation = StationStops[0];
            if (thisStation.SubrouteIndex != TCRoute.activeSubpath)
            {
                return (false);
            }

            if (thisStation.ActualStopType != StationStop.STOPTYPE.STATION_STOP)
            {
                return (false);
            }

            PlatformDetails thisPlatform = thisStation.PlatformItem;

            float platformBeginOffset = thisPlatform.TCOffset[0, thisStation.Direction];
            float platformEndOffset = thisPlatform.TCOffset[1, thisStation.Direction];
            int endSectionIndex = thisStation.Direction == 0 ?
                    thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1] :
                    thisPlatform.TCSectionIndex[0];
            int endSectionRouteIndex = ValidRoute[0].GetRouteIndex(endSectionIndex, 0);

            // check position

            float margin = 0.0f;
            if (AI.PreUpdate)
                margin = 2.0f * clearingDistanceM;  // allow margin in pre-update due to low update rate

            int stationIndex = ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, PresentPosition[0].RouteListIndex);
            if (PresentPosition[0].RouteListIndex == stationIndex)   // front of train in correct section
            {
                // if train shorter than platform : check if end of train in platform
                if (Length < thisPlatform.Length)
                {
                    if ((endSectionRouteIndex < 0) ||
                        (PresentPosition[1].RouteListIndex > endSectionRouteIndex) ||
                        (PresentPosition[1].RouteListIndex == endSectionRouteIndex && PresentPosition[1].TCOffset > (platformBeginOffset - margin)))
                    {
                        atStation = true;
                    }
                }
                // if train longer than platform : check if begin of train beyond end of platform
                else if (PresentPosition[0].TCOffset > (platformEndOffset - margin))
                {
                    atStation = true;
                }
                // if train longer than platform : check if begin of train beyond defined stop position
                else if (PresentPosition[0].TCOffset > (thisStation.StopOffset - margin))
                {
                    atStation = true;
                }
                // in pre-update : train is close enough to required stop offset
                else if (Math.Abs(PresentPosition[0].TCOffset - thisStation.StopOffset) < margin)
                {
                    atStation = true;
                }
            }

            // At station : set state, create action item

            if (atStation)
            {
                thisStation.ActualArrival = -1;
                thisStation.ActualDepart = -1;
                MovementState = AI_MOVEMENT_STATE.STATION_STOP;

                AIActionItem newAction = new AIActionItem(-10f, 0.0f, 0.0f, 0.0f, null, AIActionItem.AI_ACTION_TYPE.STATION_STOP);
                nextActionInfo = newAction;
                NextStopDistanceM = 0.0f;

#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                     Number.ToString() + " initial at station " +
                     StationStops[0].PlatformItem.Name + "\n");
#endif
            }

            return (atStation);
        }

        //================================================================================================//
        /// <summary>
        /// Update
        /// Update function for a single AI train.
        /// </summary>

        public void AIUpdate(float elapsedClockSeconds, double clockTime, bool preUpdate)
        {
#if DEBUG_CHECKTRAIN
            if (Number == 1)
            {
                CheckTrain = true;
            }
#endif
            PreUpdate = preUpdate;   // flag for pre-update phase

            // Check if at stop point and stopped

            float actClearance = clearingDistanceM;
            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
            {
                actClearance = 0f;
            }

            //          if ((NextStopDistanceM < actClearance) || (SpeedMpS <= 0 && MovementState == AI_MOVEMENT_STATE.STOPPED))
            if (MovementState == AI_MOVEMENT_STATE.STOPPED || MovementState == AI_MOVEMENT_STATE.STATION_STOP)
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

            // update position, route clearance and objects

            if (!preUpdate)
            {
                Update(elapsedClockSeconds);
            }
            else
            {
                AIPreUpdate(elapsedClockSeconds);
            }

            // get through list of objects, determine necesarry actions

            CheckSignalObjects();

            // check if state still matches authority level

            if (MovementState != AI_MOVEMENT_STATE.INIT && ControlMode == TRAIN_CONTROL.AUTO_NODE && EndAuthorityType[0] != END_AUTHORITY.MAX_DISTANCE) // restricted authority
            {
                CheckRequiredAction();
            }

            // check if reversal point reached and not yet activated - but station stop has preference over reversal point
            if ((nextActionInfo == null ||
                 (nextActionInfo.NextAction != AIActionItem.AI_ACTION_TYPE.STATION_STOP && nextActionInfo.NextAction != AIActionItem.AI_ACTION_TYPE.REVERSAL)) &&
                 TCRoute.ReversalInfo[TCRoute.activeSubpath].Valid)
            {
                int reqSection = TCRoute.ReversalInfo[TCRoute.activeSubpath].SignalUsed ?
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].LastSignalIndex :
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].LastDivergeIndex;

                if (reqSection >= 0 && PresentPosition[1].RouteListIndex >= reqSection)
                {
                    float reqDistance = SpeedMpS * SpeedMpS * MaxDecelMpSS;
                    reqDistance = nextActionInfo != null ? Math.Min(nextActionInfo.RequiredDistance, reqDistance) : reqDistance;
                    nextActionInfo = new AIActionItem(reqDistance, 0.0f, 0.0f, PresentPosition[0].DistanceTravelledM, null, AIActionItem.AI_ACTION_TYPE.REVERSAL);
                    MovementState = AI_MOVEMENT_STATE.BRAKING;
                }
            }

            // check if out of control - if so, remove

            if (ControlMode == TRAIN_CONTROL.OUT_OF_CONTROL)
            {
                RemoveTrain();
            }

            // switch on action depending on state


            switch (MovementState)
            {
                case AI_MOVEMENT_STATE.STOPPED:
                    ProcessEndOfPath();
                    UpdateStoppedState();
                    break;
                case AI_MOVEMENT_STATE.INIT:
                    UpdateStoppedState();
                    break;
                case AI_MOVEMENT_STATE.STATION_STOP:
                    UpdateStationState(clockTime);
                    break;
                case AI_MOVEMENT_STATE.BRAKING:
                    UpdateBrakingState(elapsedClockSeconds, clockTime);
                    break;
                case AI_MOVEMENT_STATE.APPROACHING_END_OF_PATH:
                    UpdateBrakingState(elapsedClockSeconds, clockTime);
                    break;
                case AI_MOVEMENT_STATE.ACCELERATING:
                    UpdateAccelState(elapsedClockSeconds);
                    break;
                case AI_MOVEMENT_STATE.FOLLOWING:
                    UpdateFollowingState(elapsedClockSeconds, clockTime);
                    break;
                case AI_MOVEMENT_STATE.RUNNING:
                    UpdateRunningState(elapsedClockSeconds);
                    break;
            }
            LastSpeedMpS = SpeedMpS;

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "DistTrv: " + FormatStrings.FormatDistance(DistanceTravelledM, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "PresPos: " + PresentPosition[0].TCSectionIndex.ToString() + " + " +
                                     FormatStrings.FormatDistance(PresentPosition[0].TCOffset, true) + " : " +
                                     PresentPosition[0].RouteListIndex.ToString() + "\n");
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
                       "AuthDis: " + FormatStrings.FormatDistance(DistanceToEndNodeAuthorityM[0], true) + "\n");
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
                       "ActDist: " + FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + "\n");

                    if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                           "NextSig: " + nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                           "Section: " + nextActionInfo.ActiveItem.ObjectDetails.TCReference.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                           "DistTr : " + FormatStrings.FormatDistance(nextActionInfo.ActiveItem.distance_to_train, true) + "\n");
                    }
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                        "Action : null\n");
                }

                File.AppendAllText(@"C:\temp\checktrain.txt",
                       "StopDst: " + FormatStrings.FormatDistance(NextStopDistanceM, true) + "\n");

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

        }

        //================================================================================================//
        /// <summary>
        /// Update for pre-update state
        /// <\summary>

        public void AIPreUpdate(float elapsedClockSeconds)
        {

            // calculate delta speed and speed

            float deltaSpeedMpS = (0.01f * AITrainThrottlePercent * MaxAccelMpSS - 0.01f * AITrainBrakePercent * MaxDecelMpSS) *
                Efficiency * elapsedClockSeconds;
            if (AITrainBrakePercent > 0 && deltaSpeedMpS < 0 && Math.Abs(deltaSpeedMpS) > SpeedMpS)
            {
                deltaSpeedMpS = -SpeedMpS;
            }
            SpeedMpS = Math.Min(TrainMaxSpeedMpS, Math.Max(0.0f, SpeedMpS + deltaSpeedMpS));

            // calculate position

            float distanceM = SpeedMpS * elapsedClockSeconds;

            if (float.IsNaN(distanceM)) distanceM = 0;//sometime it may become NaN, force it to be 0, so no move
            // force stop

            if (distanceM > (NextStopDistanceM + 1.0f))
            {
#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                    Number.ToString() + " forced stop : calculated " +
                    FormatStrings.FormatSpeed(SpeedMpS, true) + " > " +
                    FormatStrings.FormatDistance(distanceM, true) + " set to " +
                    "0.0 > " + FormatStrings.FormatDistance(NextStopDistanceM, true) + " at " +
                    FormatStrings.FormatDistance(DistanceTravelledM, true) + "\n");
#endif

                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                        Number.ToString() + " forced stop : calculated " +
                        FormatStrings.FormatSpeed(SpeedMpS, true) + " > " +
                        FormatStrings.FormatDistance(distanceM, true) + " set to " +
                        "0.0 > " + FormatStrings.FormatDistance(NextStopDistanceM, true) + " at " +
                        FormatStrings.FormatDistance(DistanceTravelledM, true) + "\n");
                }

                distanceM = Math.Max(0.0f, NextStopDistanceM + 1.0f);
                SpeedMpS = 0;

            }

            // set speed and position

            foreach (TrainCar car in Cars)
            {
                if (car.Flipped)
                {
                    car.SpeedMpS = -SpeedMpS;
                }
                else
                {
                    car.SpeedMpS = SpeedMpS;
                }
            }

            CalculatePositionOfCars(distanceM);

            DistanceTravelledM += distanceM;

            // perform overall update

            if (ValidRoute != null)     // no actions required for static objects //
            {
                movedBackward = CheckBackwardClearance();                                       // check clearance at rear //
                UpdateTrainPosition();                                                          // position update         //              
                UpdateTrainPositionInformation();                                               // position linked info    //
                int SignalObjIndex = CheckSignalPassed(0, PresentPosition[0], PreviousPosition[0]);    // check if passed signal  //
                UpdateSectionState(movedBackward);                                              // update track occupation //
                ObtainRequiredActions(movedBackward);                                           // process Actions         //
                UpdateRouteClearanceAhead(SignalObjIndex, movedBackward, elapsedClockSeconds);  // update route clearance  //
                UpdateSignalState(movedBackward);                                               // update signal state     //
            }
        }

        //================================================================================================//
        /// <summary>
        /// change in authority state - check action
        /// <\summary>

        public void CheckRequiredAction()
        {
            if (EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
            {
                if (MovementState != AI_MOVEMENT_STATE.STATION_STOP && MovementState != AI_MOVEMENT_STATE.STOPPED)
                {
                    MovementState = AI_MOVEMENT_STATE.FOLLOWING;  // start following
                }
            }
            else if (EndAuthorityType[0] == END_AUTHORITY.RESERVED_SWITCH || EndAuthorityType[0] == END_AUTHORITY.LOOP)
            {
                ResetActions(true);
                NextStopDistanceM = DistanceToEndNodeAuthorityM[0] - 2.0f * junctionOverlapM;
                CreateTrainAction(SpeedMpS, 0.0f, NextStopDistanceM, null,
                           AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY);
            }
            // first handle outstanding actions
            else if (EndAuthorityType[0] == END_AUTHORITY.END_OF_PATH && (nextActionInfo == null || nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE))
            {
                ResetActions(false);
                NextStopDistanceM = DistanceToEndNodeAuthorityM[0] - clearingDistanceM;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check all signal objects
        /// <\summary>

        public void CheckSignalObjects()
        {
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Check Objects \n");
            }

            float validSpeed = AllowedMaxSpeedMpS;
            List<ObjectItemInfo> processedList = new List<ObjectItemInfo>();

            foreach (ObjectItemInfo thisInfo in SignalObjectItems)
            {

                // check speedlimit
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                            "Item : " + thisInfo.ObjectType.ToString() + " at " +
                                        FormatStrings.FormatDistance(thisInfo.distance_to_train, true) +
                        " - processed : " + thisInfo.processed.ToString() + "\n");

                    if (thisInfo.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                "  Signal State : " + thisInfo.signal_state.ToString() + "\n");
                    }
                }

                float setSpeed = IsFreight ? thisInfo.speed_freight : thisInfo.speed_passenger;
                if (setSpeed < validSpeed && setSpeed < AllowedMaxSpeedMpS && setSpeed > 0)
                {
                    if (!thisInfo.processed)
                    {
                        bool process_req = true;

                        if (ControlMode == TRAIN_CONTROL.AUTO_NODE &&
                                        thisInfo.distance_to_train > DistanceToEndNodeAuthorityM[0])
                        {
                            process_req = false;
                        }
                        else if (thisInfo.distance_to_train > signalApproachDistanceM ||
                                 (MovementState == AI_MOVEMENT_STATE.RUNNING && SpeedMpS > setSpeed) ||
                                  MovementState == AI_MOVEMENT_STATE.ACCELERATING)
                        {
                            process_req = true;
                        }
                        else
                        {
                            process_req = false;
                        }

                        if (process_req)
                        {
                            if (thisInfo.ObjectType == ObjectItemInfo.ObjectItemType.SPEEDLIMIT)
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

                // check signal state

                if (thisInfo.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL &&
                        thisInfo.signal_state < SignalHead.SIGASP.APPROACH_1 &&
                        !thisInfo.processed)
                {
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Signal restricted\n");
                    }
                    if (!(ControlMode == TRAIN_CONTROL.AUTO_NODE &&
                                    thisInfo.distance_to_train > DistanceToEndNodeAuthorityM[0]))
                    {
                        if (thisInfo.signal_state == SignalHead.SIGASP.STOP ||
                            thisInfo.ObjectDetails.enabledTrain != routedForward)
                        {
                            CreateTrainAction(validSpeed, 0.0f,
                                    thisInfo.distance_to_train, thisInfo,
                                    AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP);
                            processedList.Add(thisInfo);
                            if (((thisInfo.distance_to_train - clearingDistanceM) < clearingDistanceM) &&
                                         (SpeedMpS > 0.0f || MovementState == AI_MOVEMENT_STATE.ACCELERATING))
                            {
                                AITrainBrakePercent = 100;
                                AITrainThrottlePercent = 0;
                                NextStopDistanceM = clearingDistanceM;
                            }
                        }
                        else if (thisInfo.distance_to_train > 2.0f * signalApproachDistanceM) // set restricted only if not close
                        {
                            CreateTrainAction(validSpeed, 0.0f,
                                    thisInfo.distance_to_train, thisInfo,
                                    AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED);
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

            // set processed items - must be collected as item can be processed twice (speed and signal)

            foreach (ObjectItemInfo thisInfo in processedList)
            {
                thisInfo.processed = true;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check for next station
        /// <\summary>

        public void SetNextStationAction()
        {

            // check if station in this subpath

            int stationIndex = 0;
            StationStop thisStation = StationStops[stationIndex];
            while (thisStation.SubrouteIndex < TCRoute.activeSubpath) // station was in previous subpath
            {
                StationStops.RemoveAt(0);
                if (StationStops.Count == 0) // no more stations
                {
                    return;
                }
                thisStation = StationStops[0];
            }

            if (thisStation.SubrouteIndex > TCRoute.activeSubpath)    // station is not in this subpath
            {
                return;
            }

            // get distance to station

            bool validStop = false;
            while (!validStop)
            {
                float[] distancesM = CalculateDistancesToNextStation(thisStation, TrainMaxSpeedMpS, false);
                if (distancesM[0] < 0f) // stop is not valid
                {
                    StationStops.RemoveAt(0);
                    if (StationStops.Count == 0)
                    {
                        return;  // no more stations - exit
                    }

                    thisStation = StationStops[0];
                    if (thisStation.SubrouteIndex > TCRoute.activeSubpath) return;  // station not in this subpath - exit
                }
                else
                {
                    validStop = true;
                    AIActionItem newAction = new AIActionItem(distancesM[1], 0.0f, distancesM[0], DistanceTravelledM,
                            null, AIActionItem.AI_ACTION_TYPE.STATION_STOP);

                    requiredActions.InsertAction(newAction);

#if DEBUG_REPORTS
                    if (StationStops[0].ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                    {
                        File.AppendAllText(@"C:\temp\printproc.txt", "Insert for train " +
                            Number.ToString() + ", type STATION_STOP (" +
                            StationStops[0].PlatformItem.Name + "), at " +
                            FormatStrings.FormatDistance(distancesM[0], true) + ", trigger at " +
                            FormatStrings.FormatDistance(distancesM[1], true) + " (now at " +
                            FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
            }
            else if (StationStops[0].ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Insert for train " +
                            Number.ToString() + ", type WAITING_POINT (" +
                            FormatStrings.FormatDistance(distancesM[0], true) + ", trigger at " +
                            FormatStrings.FormatDistance(distancesM[1], true) + " (now at " +
                            FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
                    }
#endif

                    if (CheckTrain)
                    {
                        if (StationStops[0].ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Insert for train " +
                                    Number.ToString() + ", type STATION_STOP (" +
                                    StationStops[0].PlatformItem.Name + "), at " +
                                    FormatStrings.FormatDistance(distancesM[0], true) + ", trigger at " +
                                    FormatStrings.FormatDistance(distancesM[1], true) + " (now at " +
                                    FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
                        }
                        else if (StationStops[0].ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Insert for train " +
                                        Number.ToString() + ", type WAITING_POINT (" +
                                        FormatStrings.FormatDistance(distancesM[0], true) + ", trigger at " +
                                        FormatStrings.FormatDistance(distancesM[1], true) + " (now at " +
                                        FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Calculate actual distance and trigger distance for next station
        /// <\summary>

        public float[] CalculateDistancesToNextStation(StationStop thisStation, float presentSpeedMpS, bool reschedule)
        {
            int thisSectionIndex = PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - PresentPosition[0].TCOffset;

            // get station route index - if not found, return distances < 0

            int stationIndex0 = ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, PresentPosition[0].RouteListIndex);
            int stationIndex1 = ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, PresentPosition[1].RouteListIndex);

            float distanceToTrainM = -1f;
            if (stationIndex0 >= 0)
            {
                distanceToTrainM = ValidRoute[0].GetDistanceAlongRoute(PresentPosition[0].RouteListIndex,
                    leftInSectionM, stationIndex0, thisStation.StopOffset, true, signalRef);
            }
            // front of train is passed station but rear is not or train is stopped - return present position
            if (distanceToTrainM < 0f && MovementState == AI_MOVEMENT_STATE.STATION_STOP)
            {
                return (new float[2] { PresentPosition[0].DistanceTravelledM, 0.0f });
            }

            // if station not on route at all return negative values
            if (distanceToTrainM < 0f && stationIndex0 < 0 && stationIndex1 < 0)
            {
                return (new float[2] { -1f, -1f });
            }

            // if reschedule, use actual speed

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

                if (firstPartRangeM < remainingRangeM && SpeedMpS < TrainMaxSpeedMpS) // if distance left and not at max speed
                // split remaining distance based on relation between acceleration and deceleration
                {
                    secndPartRangeM = (remainingRangeM - firstPartRangeM) * (2.0f * MaxDecelMpSS) / (MaxDecelMpSS + MaxAccelMpSS);
                }

                triggerDistanceM = activateDistanceTravelledM - (firstPartRangeM + secndPartRangeM);
            }
            else

            // use maximum speed
            {
                float deltaTime = TrainMaxSpeedMpS / MaxDecelMpSS;
                float brakingDistanceM = (TrainMaxSpeedMpS * deltaTime) + (0.5f * MaxDecelMpSS * deltaTime * deltaTime);
                triggerDistanceM = activateDistanceTravelledM - brakingDistanceM;
            }

            float[] distancesM = new float[2];
            distancesM[0] = activateDistanceTravelledM;
            distancesM[1] = triggerDistanceM;

            return (distancesM);
        }

        //================================================================================================//
        /// <summary>
        /// Update train in stopped state
        /// <\summary>

        public override void SwitchToSignalControl(SignalObject thisSignal)
        {
            base.SwitchToSignalControl(thisSignal);
            ResetActions(true);

            // check if any actions must be processed immediately

            ObtainRequiredActions(0);
        }

        //================================================================================================//
        /// <summary>
        /// Update train in stopped state
        /// <\summary>

        public override void SwitchToNodeControl(int thisSectionIndex)
        {
            base.SwitchToNodeControl(thisSectionIndex);
            ResetActions(true);

            // check if any actions must be processed immediately

            ObtainRequiredActions(0);
        }

        //================================================================================================//
        /// <summary>
        /// Update train in stopped state
        /// <\summary>

        private void UpdateStoppedState()
        {

            if (SpeedMpS > 0)   // if train still running force it to stop
            {
                SpeedMpS = 0;
                Update(0);   // stop the wheels from moving etc
                AITrainThrottlePercent = 0;
                AITrainBrakePercent = 100;
            }

            // check if train ahead - if so, determine speed and distance

            if (ControlMode == TRAIN_CONTROL.AUTO_NODE &&
                EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
            {

                // check if train ahead is in same section
                int sectionIndex = PresentPosition[0].TCSectionIndex;
                int startIndex = ValidRoute[0].GetRouteIndex(sectionIndex, 0);
                int endIndex = ValidRoute[0].GetRouteIndex(LastReservedSection[0], 0);

                TrackCircuitSection thisSection = signalRef.TrackCircuitList[sectionIndex];

                Dictionary<Train, float> trainInfo = thisSection.TestTrainAhead(this,
                                PresentPosition[0].TCOffset, PresentPosition[0].TCDirection);

                // search for train ahead in route sections
                for (int iIndex = startIndex + 1; iIndex <= endIndex && trainInfo.Count <= 0; iIndex++)
                {
                    thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                    trainInfo = thisSection.TestTrainAhead(this, 0.0f, ValidRoute[0][iIndex].Direction);
                }

                if (trainInfo.Count <= 0)
                // train is in section beyond last reserved
                {
                    if (endIndex < ValidRoute[0].Count - 1)
                    {
                        thisSection = signalRef.TrackCircuitList[ValidRoute[0][endIndex + 1].TCSectionIndex];

                        trainInfo = thisSection.TestTrainAhead(this, 0.0f, ValidRoute[0][endIndex + 1].Direction);
                    }
                }

                if (trainInfo.Count > 0)  // found train
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo) // always just one
                    {
                        Train OtherTrain = trainAhead.Key;
                        if (Math.Abs(OtherTrain.SpeedMpS) < 0.001f &&
                                    DistanceToEndNodeAuthorityM[0] > followDistanceStatTrainM)
                        {
                            // allow creeping closer
                            CreateTrainAction(creepSpeedMpS, 0.0f,
                                    DistanceToEndNodeAuthorityM[0], null, AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD);
                            MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                            StartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);
                        }

                        else if (Math.Abs(OtherTrain.SpeedMpS) > 0 &&
                            DistanceToEndNodeAuthorityM[0] > keepDistanceMovingTrainM)
                        {
                            // train started moving
                            MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                            StartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);
                        }
                    }
                }

                // if train not found, do nothing - state will change next update

            }

     // Other node mode : check distance ahead (path may have cleared)

            else if (ControlMode == TRAIN_CONTROL.AUTO_NODE &&
                        DistanceToEndNodeAuthorityM[0] > clearingDistanceM)
            {
                NextStopDistanceM = DistanceToEndNodeAuthorityM[0];
                StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
            }

    // signal node : check state of signal

            else if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
            {
                SignalHead.SIGASP nextAspect = SignalHead.SIGASP.UNKNOWN;
                // there is a next item and it is the next signal
                if (nextActionInfo != null && nextActionInfo.ActiveItem != null &&
                    nextActionInfo.ActiveItem.ObjectDetails == NextSignalObject[0])
                {
                    nextAspect = nextActionInfo.ActiveItem.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
                }
                else
                {
                    nextAspect = GetNextSignalAspect(0);
                }

                if (NextSignalObject[0] == null) // no signal ahead so switch Node control
                {
                    SwitchToNodeControl(PresentPosition[0].TCSectionIndex);
                    NextStopDistanceM = DistanceToEndNodeAuthorityM[0];
                }

                else if (nextAspect > SignalHead.SIGASP.STOP &&
                        nextAspect < SignalHead.SIGASP.APPROACH_1)
                {
                    // check if any other signals within clearing distance
                    bool signalCleared = true;
                    bool withinDistance = true;

                    for (int iitem = 0; iitem <= SignalObjectItems.Count - 1 && withinDistance && signalCleared; iitem++)
                    {
                        ObjectItemInfo nextObject = SignalObjectItems[iitem];
                        if (nextObject.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                        {
                            if (nextObject.ObjectDetails != NextSignalObject[0]) // not signal we are waiting for
                            {
                                if (nextObject.distance_to_train > 2.0 * clearingDistanceM)
                                {
                                    withinDistance = false;  // signal is far enough ahead
                                }
                                else if (nextObject.signal_state == SignalHead.SIGASP.STOP)
                                {
                                    signalCleared = false;   // signal is not clear
                                }
                            }
                        }
                    }

                    if (signalCleared)
                    {
                        ResetActions(true);
                        NextStopDistanceM = 5000f; // clear to 5000m, will be updated if required
                        StartMoving(AI_START_MOVEMENT.SIGNAL_RESTRICTED);
                    }
                }
                else if (nextAspect >= SignalHead.SIGASP.APPROACH_1)
                {
                    // check if any other signals within clearing distance
                    bool signalCleared = true;
                    bool withinDistance = true;

                    for (int iitem = 0; iitem <= SignalObjectItems.Count - 1 && withinDistance && signalCleared; iitem++)
                    {
                        ObjectItemInfo nextObject = SignalObjectItems[iitem];
                        if (nextObject.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                        {
                            if (nextObject.ObjectDetails != NextSignalObject[0]) // not signal we are waiting for
                            {
                                if (nextObject.distance_to_train > 2.0 * clearingDistanceM)
                                {
                                    withinDistance = false;  // signal is far enough ahead
                                }
                                else if (nextObject.signal_state == SignalHead.SIGASP.STOP)
                                {
                                    signalCleared = false;   // signal is not clear
                                }
                            }
                        }
                    }

                    if (signalCleared)
                    {
                        ResetActions(true);
                        NextStopDistanceM = 5000f; // clear to 5000m, will be updated if required
                        StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
                    }
                }
                else if (nextActionInfo != null &&
                 nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP &&
                 StationStops[0].SubrouteIndex == TCRoute.activeSubpath &&
                 ValidRoute[0].GetRouteIndex(StationStops[0].TCSectionIndex, PresentPosition[0].RouteListIndex) >= PresentPosition[0].RouteListIndex)
                // assume to be in station
                {
                    MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                }
                else if (nextActionInfo == null || nextActionInfo.NextAction != AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                {
                    MovementState = AI_MOVEMENT_STATE.RUNNING;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                                Number.ToString() + " , forced to BRAKING from invalid stop (now at " +
                                FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
#endif

                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Train is at station
        /// <\summary>

        public void UpdateStationState(double ClockTime)
        {
            StationStop thisStation = StationStops[0];
            int presentTime = Convert.ToInt32(Math.Floor(ClockTime));
            bool removeStation = true;

            // no arrival / departure time set : update times

            if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
            {
                if (thisStation.ActualArrival < 0)
                {
                    thisStation.ActualArrival = presentTime;
                    thisStation.ActualDepart = thisStation.DepartTime;

                    if (thisStation.ActualArrival > thisStation.ArrivalTime)
                    {
                        int stopTime = thisStation.DepartTime - thisStation.ArrivalTime;
                        if (stopTime <= 0 || stopTime > thisStation.PlatformItem.MinWaitingTime)
                        {
                            stopTime = (int)thisStation.PlatformItem.MinWaitingTime;
                        }
                        thisStation.ActualDepart = Math.Max((presentTime + stopTime), thisStation.DepartTime);
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
            }


            // not yet time to depart - check if signal can be released

            if (thisStation.ActualDepart > presentTime)
            {
                if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP &&
                    (thisStation.ActualDepart - 120 < presentTime) &&
                     thisStation.HoldSignal)
                {
                    HoldingSignals.Remove(thisStation.ExitSignal);
                    SignalObject nextSignal = signalRef.SignalObjects[thisStation.ExitSignal];

                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                             Number.ToString() + " clearing hold signal " + nextSignal.thisRef.ToString() + " at station " +
                             StationStops[0].PlatformItem.Name + "\n");
                    }

                    if (nextSignal.enabledTrain != null && nextSignal.enabledTrain.Train == this)
                    {
                        nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null);// for AI always use direction 0
                    }
                    thisStation.HoldSignal = false;
                }
                return;
            }

            // depart

            thisStation.Passed = true;

            // first, check state of signal

            if (thisStation.ExitSignal >= 0)
            {
                HoldingSignals.Remove(thisStation.ExitSignal);
                SignalObject nextSignal = signalRef.SignalObjects[thisStation.ExitSignal];
                nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null); // for AI always use direction 0
            }

            // check if stopped for hold exit signal

            if (thisStation.ExitSignal >= 0 && NextSignalObject[0] != null && NextSignalObject[0].thisRef == thisStation.ExitSignal)
            {
                SignalHead.SIGASP nextAspect = GetNextSignalAspect(0);
                if (nextAspect == SignalHead.SIGASP.STOP)
                {
                    // check if end of route reached

                    if ((PresentPosition[0].RouteListIndex == (ValidRoute[0].Count - 1)) ||
                        (NextSignalObject[0] != null &&
                         NextSignalObject[0].TCReference == ValidRoute[0][ValidRoute[0].Count - 1].TCSectionIndex) ||
                        (NextSignalObject[0] != null &&
                         NextSignalObject[0].TCNextTC == ValidRoute[0][ValidRoute[0].Count - 1].TCSectionIndex))
                    {
                        ProcessEndOfPath();
                        removeStation = false; // do not remove station from list - is done by path processing
                    }
                    else
                    {
                        return;  // do not depart if exit signal at danger
                    }
                }
            }
            else
            {
                if ((PresentPosition[0].RouteListIndex == (ValidRoute[0].Count - 1)) ||
                    (NextSignalObject[0] != null &&
                     NextSignalObject[0].TCReference == ValidRoute[0][ValidRoute[0].Count - 1].TCSectionIndex) ||
                    (NextSignalObject[0] != null &&
                     NextSignalObject[0].TCNextTC == ValidRoute[0][ValidRoute[0].Count - 1].TCSectionIndex))
                {
                    ProcessEndOfPath();
                    removeStation = false; // do not remove station from list - is done by path processing
                }
                else if (ControlMode == TRAIN_CONTROL.AUTO_NODE &&
                    (EndAuthorityType[0] == END_AUTHORITY.END_OF_TRACK ||
                     EndAuthorityType[0] == END_AUTHORITY.END_OF_PATH))
                {
                    ProcessEndOfPath();
                    removeStation = false; // do not remove station from list - is done by path processing
                }
            }

            MovementState = AI_MOVEMENT_STATE.STOPPED;   // ready to depart - change to stop to check action

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

            if (removeStation)
                StationStops.RemoveAt(0);

            ResetActions(true);
        }

        //================================================================================================//
        /// <summary>
        /// Train is braking
        /// <\summary>

        public void UpdateBrakingState(float elapsedClockSeconds, double ClockTime)
        {

            // check if action still required

            bool clearAction = false;
            float distanceToGoM = clearingDistanceM;

            if (nextActionInfo == null) // action has been reset - keep status quo
            {
                if (ControlMode == TRAIN_CONTROL.AUTO_NODE)  // node control : use control distance
                {
                    distanceToGoM = DistanceToEndNodeAuthorityM[0];

                    if (EndAuthorityType[0] == END_AUTHORITY.RESERVED_SWITCH)
                    {
                        distanceToGoM = DistanceToEndNodeAuthorityM[0] - 2.0f * junctionOverlapM;
                    }
                    else if (EndAuthorityType[0] == END_AUTHORITY.END_OF_PATH)
                    {
                        distanceToGoM = DistanceToEndNodeAuthorityM[0] - clearingDistanceM;
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
                            AdjustControlsBrakeFull();
                        }
                    }

                    if (distanceToGoM < clearingDistanceM && SpeedMpS <= 0)
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
                else // action cleared - set running or stopped
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

                // check if speedlimit on signal is cleared

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
                          FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " (now at " +
                          FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : signal " +
                              nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " : speed : " +
                              FormatStrings.FormatSpeed(nextActionInfo.ActiveItem.actual_speed, true) + " >= limit : " +
                              FormatStrings.FormatSpeed(AllowedMaxSpeedMpS, true) + " at " +
                              FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " (now at " +
                              FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
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
                          FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " (now at " +
                          FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : signal " +
                              nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " : speed : " +
                              FormatStrings.FormatSpeed(nextActionInfo.ActiveItem.actual_speed, true) + " cleared at " +
                              FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " (now at " +
                              FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
            }

        // check if STOP signal cleared

            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
            {
                if (nextActionInfo.ActiveItem.signal_state >= SignalHead.SIGASP.APPROACH_1)
                {
                    clearAction = true;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                          Number.ToString() + " : signal " +
                          nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                          FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                          FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : signal " +
                              nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                              FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                              FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
                else if (nextActionInfo.ActiveItem.signal_state != SignalHead.SIGASP.STOP)
                {
                    nextActionInfo.NextAction = AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED;
                    if ((nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM)
                    {
                        clearAction = true;
#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt",
                          Number.ToString() + " : signal " +
                          nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                          FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                          FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                              Number.ToString() + " : signal " +
                              nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                              FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                              FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                        }
                    }
                }
            }

        // check if RESTRICTED signal cleared

            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED)
            {
                if (nextActionInfo.ActiveItem.signal_state >= SignalHead.SIGASP.APPROACH_1 ||
                (nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM)
                {
                    clearAction = true;
#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt",
                      Number.ToString() + " : signal " +
                      nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                      FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                      FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                      FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                          Number.ToString() + " : signal " +
                          nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                          FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                          FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
            }

    // check if END_AUTHORITY extended

            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY)
            {
                nextActionInfo.ActivateDistanceM = DistanceToEndNodeAuthorityM[0];
                if (EndAuthorityType[0] == END_AUTHORITY.MAX_DISTANCE)
                {
                    clearAction = true;
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
                          FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " (now at " +
                          FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : speed : " +
                              FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " >= limit : " +
                              FormatStrings.FormatSpeed(AllowedMaxSpeedMpS, true) + " at " +
                              FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " (now at " +
                              FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
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
                          FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " (now at " +
                          FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                              Number.ToString() + " : speed : " +
                              FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " changed to : " +
                              FormatStrings.FormatSpeed(nextActionInfo.ActiveItem.actual_speed, true) + " at " +
                              FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " (now at " +
                              FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
            }

            // action cleared - reset processed info for object items to determine next action
            // clear list of pending action to create new list

            if (clearAction)
            {
                ResetActions(true);
                MovementState = AI_MOVEMENT_STATE.RUNNING;
                Alpha10 = 10;
                if (SpeedMpS < AllowedMaxSpeedMpS - 3.0f * hysterisMpS)
                {
                    AdjustControlsBrakeOff();
                }
                return;
            }

            // check ideal speed

            float requiredSpeedMpS = 0;

            float creepDistanceM = 3.0f * signalApproachDistanceM;
            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                creepDistanceM = 0.0f;
            if (nextActionInfo == null && requiredSpeedMpS == 0)
                creepDistanceM = clearingDistanceM;

            if (nextActionInfo != null)
            {
                requiredSpeedMpS = nextActionInfo.RequiredSpeedMpS;
                distanceToGoM = nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM;

                if (nextActionInfo.ActiveItem != null)
                {
                    distanceToGoM = nextActionInfo.ActiveItem.distance_to_train - signalApproachDistanceM;
                }

                // check if stopped at station

                if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                {
                    NextStopDistanceM = distanceToGoM;
                    if (distanceToGoM <= 0.1f)
                    {
                        AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 100);
                        AITrainThrottlePercent = 0;

                        // train is stopped - set departure time

                        if (SpeedMpS == 0)
                        {
                            MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                            StationStop thisStation = StationStops[0];
                            int presentTime = Convert.ToInt32(Math.Floor(ClockTime));

                            if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                            {
                                thisStation.ActualArrival = presentTime;
                                thisStation.ActualDepart = thisStation.DepartTime;

                                if (thisStation.ActualArrival > thisStation.ArrivalTime)
                                {
                                    int stopTime = thisStation.DepartTime - thisStation.ArrivalTime;
                                    if (stopTime <= 0 || stopTime > thisStation.PlatformItem.MinWaitingTime)
                                    {
                                        stopTime = (int)thisStation.PlatformItem.MinWaitingTime;
                                    }
                                    thisStation.ActualDepart = Math.Max((presentTime + stopTime), thisStation.DepartTime);
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

                                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                         Number.ToString() + " arrives station " +
                                         StationStops[0].PlatformItem.Name + " at " +
                                         arrTimeCT.ToString("HH:mm:ss") + "\n");
                                }
                            }
                            else if (thisStation.ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                            {
                                thisStation.ActualArrival = presentTime;

                                // delta time set
                                if (thisStation.DepartTime < 0)
                                {
                                    thisStation.ActualDepart = presentTime - thisStation.DepartTime; // depart time is negative!!
                                }
                                // actual time set
                                else
                                {
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

                // check speed reduction position reached

                else if (nextActionInfo.RequiredSpeedMpS > 0)
                {
                    if (distanceToGoM <= 0.0f)
                    {
                        AdjustControlsBrakeOff();
                        AllowedMaxSpeedMpS = nextActionInfo.RequiredSpeedMpS;
                        MovementState = AI_MOVEMENT_STATE.RUNNING;
                        Alpha10 = 10;
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

        // check if approaching reversal point

                else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.REVERSAL)
                {
                    if (Math.Abs(SpeedMpS) < 0.01f) MovementState = AI_MOVEMENT_STATE.STOPPED;
                }

                // check if stopped at signal

                else if (nextActionInfo.RequiredSpeedMpS == 0)
                {
                    NextStopDistanceM = distanceToGoM;
                    if (distanceToGoM < signalApproachDistanceM)
                    {
                        AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);
                        AITrainThrottlePercent = 0;
                        if (SpeedMpS == 0)
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

                        // if approaching signal and at 0.25 of approach distance and still moving, force stop
                        if (distanceToGoM < (0.25 * signalApproachDistanceM) && SpeedMpS > 0 &&
                            nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                        {

#if DEBUG_EXTRAINFO
                            Trace.TraceWarning("Forced stop for signal at danger for train {0} at speed {1}", Number, SpeedMpS);
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

            // keep speed within required speed band

            float lowestSpeedMpS = requiredSpeedMpS;

            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
            {
                lowestSpeedMpS =
                    distanceToGoM < (3.0f * signalApproachDistanceM) ? (0.25f * creepSpeedMpS) : creepSpeedMpS;
            }
            else
            {
                lowestSpeedMpS = distanceToGoM < signalApproachDistanceM ? requiredSpeedMpS :
                    Math.Max(creepSpeedMpS, requiredSpeedMpS);
            }

            lowestSpeedMpS = Math.Min(lowestSpeedMpS, AllowedMaxSpeedMpS);

            float maxPossSpeedMpS = distanceToGoM > 0 ? (float)Math.Sqrt(0.25f * MaxDecelMpSS * distanceToGoM) : 0.0f;
            float idealSpeedMpS = Math.Min(AllowedMaxSpeedMpS, Math.Max(maxPossSpeedMpS, lowestSpeedMpS));

            if (requiredSpeedMpS > 0)
            {
                maxPossSpeedMpS =
                        (float)Math.Sqrt(0.12f * MaxDecelMpSS * Math.Max(0.0f, distanceToGoM - (3.0f * signalApproachDistanceM)));
                idealSpeedMpS = Math.Min(AllowedMaxSpeedMpS, Math.Max(maxPossSpeedMpS + requiredSpeedMpS, lowestSpeedMpS)) -
                                    (2f * hysterisMpS);
            }

            float idealLowBandMpS = Math.Max(lowestSpeedMpS, idealSpeedMpS - (3f * hysterisMpS));
            float ideal3LowBandMpS = Math.Max(lowestSpeedMpS, idealSpeedMpS - (9f * hysterisMpS));
            float idealHighBandMpS = Math.Min(AllowedMaxSpeedMpS, Math.Max(lowestSpeedMpS, idealSpeedMpS + hysterisMpS));
            float ideal3HighBandMpS = Math.Min(AllowedMaxSpeedMpS, Math.Max(lowestSpeedMpS, idealSpeedMpS + (2f * hysterisMpS)));

            float deltaSpeedMpS = SpeedMpS - requiredSpeedMpS;
            float idealDecelMpSS = Math.Max((0.5f * MaxDecelMpSS), (deltaSpeedMpS * deltaSpeedMpS / (2.0f * distanceToGoM)));

            float lastDecelMpSS = elapsedClockSeconds > 0 ? ((SpeedMpS - LastSpeedMpS) / elapsedClockSeconds) : idealDecelMpSS;

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
                               "     Actual: " + FormatStrings.FormatSpeed(SpeedMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     Allwd : " + FormatStrings.FormatSpeed(AllowedMaxSpeedMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     Reqd  : " + FormatStrings.FormatSpeed(requiredSpeedMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     Ideal : " + FormatStrings.FormatSpeed(idealSpeedMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     lowest: " + FormatStrings.FormatSpeed(lowestSpeedMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     3high : " + FormatStrings.FormatSpeed(ideal3HighBandMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     high  : " + FormatStrings.FormatSpeed(idealHighBandMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     low   : " + FormatStrings.FormatSpeed(idealLowBandMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     3low  : " + FormatStrings.FormatSpeed(ideal3LowBandMpS, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     dist  : " + FormatStrings.FormatDistance(distanceToGoM, true) + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     A&B(S): " + AITrainThrottlePercent.ToString() + " - " + AITrainBrakePercent.ToString() + "\n");
            }

            // keep speed withing band 

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
                Alpha10 = 5;
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
                    Alpha10 = 5;
                }
                else if (lastDecelMpSS < 0.5f * idealDecelMpSS || Alpha10 <= 0)
                {
                    AdjustControlsBrakeMore(2.0f * MaxDecelMpSS, elapsedClockSeconds, 50);
                    Alpha10 = 5;
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
                            Alpha10 = 10;
                        }
                    }
                }
                else
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
                        Alpha10 = 10;
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
                        Alpha10 = 5;
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
            else if (SpeedMpS > ideal3LowBandMpS)
            {
                if (AITrainBrakePercent > 0)
                {
                    AdjustControlsBrakeLess(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
                else if (LastSpeedMpS > SpeedMpS)
                {
                    if (Alpha10 <= 0)
                    {
                        AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                        Alpha10 = 5;
                    }
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }
            else if (SpeedMpS < requiredSpeedMpS)
            {
                AdjustControlsBrakeOff();
                if (((SpeedMpS - LastSpeedMpS) / elapsedClockSeconds) < 0.5f * MaxAccelMpSS)
                {
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
                Alpha10 = 5;
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

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                     "     A&B(E): " + AITrainThrottlePercent.ToString() + " - " + AITrainBrakePercent.ToString() + "\n");
            }
        }

        //================================================================================================//
        /// <summary>
        /// Train is accelerating
        /// <\summary>

        public void UpdateAccelState(float elapsedClockSeconds)
        {

            // check speed

            if (((SpeedMpS - LastSpeedMpS) / elapsedClockSeconds) < 0.5f * MaxAccelMpSS)
            {
                AdjustControlsAccelMore(Efficiency * 0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
            }

            if (SpeedMpS > (AllowedMaxSpeedMpS - ((9.0f - 6.0f * Efficiency) * hysterisMpS)))
            {
                AdjustControlsAccelLess(0.0f, elapsedClockSeconds, (int)(AITrainThrottlePercent * 0.5f));
                MovementState = AI_MOVEMENT_STATE.RUNNING;
                Alpha10 = 0;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Train is following
        /// <\summary>

        public void UpdateFollowingState(float elapsedClockSeconds, double ClockTime)
        {
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                                        "Update Train Ahead - now at : " +
                                        PresentPosition[0].TCSectionIndex.ToString() + " " +
                                        FormatStrings.FormatDistance(PresentPosition[0].TCOffset, true) +
                                        " ; speed : " + FormatStrings.FormatSpeed(SpeedMpS, true) + "\n");
            }

            if (ControlMode != TRAIN_CONTROL.AUTO_NODE || EndAuthorityType[0] != END_AUTHORITY.TRAIN_AHEAD) // train is gone
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
                // check if train is in sections ahead
                bool trainFound = false;
                bool lastSection = false;
                Dictionary<Train, float> trainInfo = null;
                int sectionIndex = -1;
                float accDistance = 0;

                for (int iSection = PresentPosition[0].RouteListIndex; iSection < ValidRoute[0].Count && !lastSection && !trainFound; iSection++)
                {
                    sectionIndex = ValidRoute[0][iSection].TCSectionIndex;
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[sectionIndex];

                    if (sectionIndex == PresentPosition[0].TCSectionIndex)
                    {
                        trainInfo = thisSection.TestTrainAhead(this,
                             PresentPosition[0].TCOffset, PresentPosition[0].TCDirection);
                        if (trainInfo.Count <= 0)
                            accDistance -= PresentPosition[0].TCOffset;  // compensate for offset
                    }
                    else
                    {
                        trainInfo = thisSection.TestTrainAhead(this,
                            0, ValidRoute[0][iSection].Direction);
                    }

                    trainFound = (trainInfo.Count > 0);
                    lastSection = (sectionIndex == LastReservedSection[0]);

                    if (!trainFound)
                    {
                        accDistance += thisSection.Length;
                    }

                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                                            "Train count in section " + sectionIndex.ToString() + " = " + trainInfo.Count.ToString() + "\n");
                    }
                }

                if (trainInfo == null || trainInfo.Count == 0) // try next section after last reserved
                {
                    if (sectionIndex == LastReservedSection[0])
                    {
                        int routeIndex = ValidRoute[0].GetRouteIndex(sectionIndex, PresentPosition[0].RouteListIndex);
                        if (routeIndex >= 0 && routeIndex <= (ValidRoute[0].Count - 1))
                        {
                            sectionIndex = ValidRoute[0][routeIndex + 1].TCSectionIndex;
                            TrackCircuitSection thisSection = signalRef.TrackCircuitList[sectionIndex];

                            trainInfo = thisSection.TestTrainAhead(this,
                                0, ValidRoute[0][routeIndex + 1].Direction);
                        }
                    }
                }

                if (trainInfo != null && trainInfo.Count > 0)  // found train
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo) // always just one
                    {
                        Train OtherTrain = trainAhead.Key;
                        float distanceToTrain = trainAhead.Value + accDistance;

                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                    "Other train : " + OtherTrain.Number.ToString() + " at : " +
                                                    OtherTrain.PresentPosition[0].TCSectionIndex.ToString() + " " +
                                                    FormatStrings.FormatDistance(OtherTrain.PresentPosition[0].TCOffset, true) +
                                                    " ; speed : " + FormatStrings.FormatSpeed(OtherTrain.SpeedMpS, true) + "\n");
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                            "DistAhd: " + FormatStrings.FormatDistance(DistanceToEndNodeAuthorityM[0], true) + "\n");
                        }

                        // update action info with new position

                        if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD)
                            nextActionInfo.RequiredDistance = distanceToTrain;

                        // check distance and speed
                        if (OtherTrain.SpeedMpS == 0.0f)
                        {
                            float keepDistanceStatTrainM = (OtherTrain.IsFreight || IsFreight) ? keepDistanceStatTrainM_F : keepDistanceStatTrainM_P;
                            if (PreUpdate) keepDistanceStatTrainM = keepDistanceMovingTrainM;

                            float brakingDistance = SpeedMpS * SpeedMpS * 0.5f * (0.5f * MaxDecelMpSS);

                            float minspeedMpS = Math.Min(2.0f * creepSpeedMpS, AllowedMaxSpeedMpS - (5.0f * hysterisMpS));

                            if ((distanceToTrain - brakingDistance) > keepDistanceStatTrainM * 3.0f)
                            {
                                if (brakingDistance > DistanceToEndNodeAuthorityM[0])
                                {
                                    AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                                }
                                else if (SpeedMpS < minspeedMpS)
                                {
                                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                                }
                                else if (SpeedMpS < (AllowedMaxSpeedMpS - (2.0f * hysterisMpS)))
                                {
                                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                                }
                                else if (SpeedMpS > (AllowedMaxSpeedMpS - (2.0f * hysterisMpS)))
                                {
                                    AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                                }
                            }
                            else if ((distanceToTrain - brakingDistance) > keepDistanceStatTrainM)
                            {
                                if (SpeedMpS > minspeedMpS)
                                {
                                    AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 50);
                                }
                                else if (SpeedMpS > 0.25f * minspeedMpS)
                                {
                                    AdjustControlsBrakeOff();
                                }
                                else
                                {
                                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                                }
                            }
                            else
                            {
                                if (SpeedMpS > 0.1f)
                                {
                                    AdjustControlsBrakeFull();

                                    // if too close, force stop
                                    if (distanceToTrain < 0.25 * keepDistanceStatTrainM)
                                    {
                                        SpeedMpS = 0.0f;
                                        foreach (TrainCar car in Cars)
                                        {
                                            car.SpeedMpS = SpeedMpS;
                                        }
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

                                    if (OtherTrain.TrainType == TRAINTYPE.AI)
                                    {
                                        AITrain OtherAITrain = OtherTrain as AITrain;
                                        otherTrainInStation = (OtherAITrain.MovementState == AI_MOVEMENT_STATE.STATION_STOP);
                                    }

                                    bool thisTrainInStation = (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP);
                                    if (thisTrainInStation) thisTrainInStation = (StationStops[0].SubrouteIndex == TCRoute.activeSubpath);
                                    if (thisTrainInStation)
                                    {
                                        if (otherTrainInStation)
                                        {
                                            thisTrainInStation =
                                                (ValidRoute[0].GetRouteIndex(StationStops[0].TCSectionIndex, PresentPosition[0].RouteListIndex) >= PresentPosition[0].RouteListIndex);
                                        }
                                        else
                                        {
                                            thisTrainInStation =
                                                (ValidRoute[0].GetRouteIndex(StationStops[0].TCSectionIndex, PresentPosition[0].RouteListIndex) == PresentPosition[0].RouteListIndex);
                                        }
                                    }

                                    if (thisTrainInStation)
                                    {
                                        MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                                        StationStop thisStation = StationStops[0];
                                        int presentTime = Convert.ToInt32(Math.Floor(ClockTime));

                                        if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                                        {
                                            thisStation.ActualArrival = presentTime;
                                            thisStation.ActualDepart = thisStation.DepartTime;

                                            if (thisStation.ActualArrival > thisStation.ArrivalTime)
                                            {
                                                int stopTime = thisStation.DepartTime - thisStation.ArrivalTime;
                                                if (stopTime <= 0 || stopTime > thisStation.PlatformItem.MinWaitingTime)
                                                {
                                                    stopTime = (int)thisStation.PlatformItem.MinWaitingTime;
                                                }
                                                thisStation.ActualDepart = Math.Max((presentTime + stopTime), thisStation.DepartTime);
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

                                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                                     Number.ToString() + " arrives station " +
                                                     StationStops[0].PlatformItem.Name + " at " +
                                                     arrTimeCT.ToString("HH:mm:ss") + "\n");
                                            }
                                        }
                                        else if (thisStation.ActualStopType == StationStop.STOPTYPE.WAITING_POINT)
                                        {
                                            thisStation.ActualArrival = presentTime;

                                            // delta time set
                                            if (thisStation.DepartTime < 0)
                                            {
                                                thisStation.ActualDepart = presentTime - thisStation.DepartTime; // depart time is negative!!
                                            }
                                            // actual time set
                                            else
                                            {
                                                thisStation.ActualDepart = thisStation.DepartTime;
                                            }

                                            // if waited behind other train, move remaining track sections to next subroute if required

                                            // scan sections in backward order
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
                            if (SpeedMpS > (OtherTrain.SpeedMpS + hysterisMpS) ||
                                SpeedMpS > (maxFollowSpeedMpS + hysterisMpS) ||
                                       DistanceToEndNodeAuthorityM[0] < (keepDistanceMovingTrainM - clearingDistanceM))
                            {
                                AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                            }
                            else if (SpeedMpS < (OtherTrain.SpeedMpS - hysterisMpS) &&
                                       SpeedMpS < maxFollowSpeedMpS &&
                                       DistanceToEndNodeAuthorityM[0] > (keepDistanceMovingTrainM + clearingDistanceM))
                            {
                                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 2);
                            }
                        }
                    }
                }

                // train not found - keep moving, state will change next update
            }
        }

        //================================================================================================//
        /// <summary>
        /// Train is running at required speed
        /// <\summary>

        public void UpdateRunningState(float elapsedClockSeconds)
        {

            float topBand = AllowedMaxSpeedMpS - ((1.5f - Efficiency) * hysterisMpS);
            float highBand = Math.Max(0.5f, AllowedMaxSpeedMpS - ((3.0f - 2.0f * Efficiency) * hysterisMpS));
            float lowBand = Math.Max(0.4f, AllowedMaxSpeedMpS - ((9.0f - 3.0f * Efficiency) * hysterisMpS));

            // check speed

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
                Alpha10 = 5;
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
                    if (AITrainThrottlePercent > 50)
                    {
                        AdjustControlsAccelLess(0.0f, elapsedClockSeconds, 20);
                    }
                    else if (AITrainThrottlePercent > 0)
                    {
                        if (Alpha10 <= 0)
                        {
                            AdjustControlsAccelLess(0.0f, elapsedClockSeconds, 2);
                            Alpha10 = 5;
                        }
                    }
                    else if (AITrainBrakePercent < 50)
                    {
                        AdjustControlsBrakeMore(0.0f, elapsedClockSeconds, 10);
                    }
                    else
                    {
                        AdjustControlsBrakeFull();
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
                        Alpha10 = 10;
                    }
                }
                else
                {
                    if (AITrainThrottlePercent > 50)
                    {
                        AdjustControlsAccelLess(0.3f * MaxAccelMpSS, elapsedClockSeconds, 20);
                    }
                    else if (Alpha10 <= 0 && AITrainThrottlePercent > 20)
                    {
                        AdjustControlsAccelLess(0.3f * MaxAccelMpSS, elapsedClockSeconds, 5);
                        Alpha10 = 10;
                    }
                    else if (Alpha10 <= 0 && AITrainThrottlePercent < 10)
                    {
                        AdjustControlsAccelMore(0.3f * MaxAccelMpSS, elapsedClockSeconds, 2);
                        Alpha10 = 10;
                    }
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }
            else if (SpeedMpS > lowBand)
            {
                if (LastSpeedMpS < SpeedMpS)
                {
                    if (AITrainThrottlePercent > 50)
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
        /// <\summary>

        public void StartMoving(AI_START_MOVEMENT reason)
        {

            // reset brakes, set throttle

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
            else if (nextActionInfo != null)  // train has valid action, so start in BRAKE mode
            {
                MovementState = AI_MOVEMENT_STATE.BRAKING;
                Alpha10 = 10;
                AITrainThrottlePercent = 25;
                AdjustControlsBrakeOff();
            }
            else
            {
                MovementState = AI_MOVEMENT_STATE.ACCELERATING;
                Alpha10 = 10;
                AITrainThrottlePercent = 25;
                AdjustControlsBrakeOff();
            }

            if (FirstCar != null)
            {
                FirstCar.ThrottlePercent = AITrainThrottlePercent;
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
            }

        }

        //================================================================================================//
        /// <summary>
        /// Train control routines
        /// <\summary>

        public void AdjustControlsBrakeMore(float reqDecelMpSS, float timeS, int stepSize)
        {
            float thisds = 0.0f;
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
                float ds = timeS * (reqDecelMpSS);
                thisds = ds;
                SpeedMpS = Math.Max(SpeedMpS - ds, 0); // avoid negative speeds
                foreach (TrainCar car in Cars)
                {
                    car.SpeedMpS = car.Flipped ? -SpeedMpS : SpeedMpS;
                }
            }

            if (FirstCar != null)
            {
                FirstCar.ThrottlePercent = AITrainThrottlePercent;
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
            }

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
                float ds = timeS * (reqDecelMpSS);
                SpeedMpS = SpeedMpS + ds; // avoid negative speeds
                foreach (TrainCar car in Cars)
                {
                    car.SpeedMpS = car.Flipped ? -SpeedMpS : SpeedMpS;
                }
            }

            if (FirstCar != null)
            {
                FirstCar.ThrottlePercent = AITrainThrottlePercent;
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
            }
        }

        public void AdjustControlsBrakeOff()
        {
            AITrainBrakePercent = 0;
            InitializeBrakes();

            if (FirstCar != null)
            {
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
            }
        }

        public void AdjustControlsBrakeFull()
        {
            AITrainThrottlePercent = 0;
            AITrainBrakePercent = 100;

            if (FirstCar != null)
            {
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
            }
        }

        public void AdjustControlsThrottleOff()
        {
            AITrainThrottlePercent = 0;

            if (FirstCar != null)
            {
                FirstCar.ThrottlePercent = AITrainThrottlePercent;
            }
        }

        public void AdjustControlsAccelMore(float reqAccelMpSS, float timeS, int stepSize)
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
            else if (LastSpeedMpS == 0 || LastSpeedMpS >= SpeedMpS)
            {
                float ds = timeS * (reqAccelMpSS);
                SpeedMpS = SpeedMpS + ds;
                foreach (TrainCar car in Cars)
                {
                    car.SpeedMpS = car.Flipped ? -SpeedMpS : SpeedMpS;
                }
            }

            if (FirstCar != null)
            {
                FirstCar.ThrottlePercent = AITrainThrottlePercent;
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
            }
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
                float ds = timeS * (reqAccelMpSS);
                SpeedMpS = Math.Max(SpeedMpS - ds, 0); // avoid negative speeds
                foreach (TrainCar car in Cars)
                {
                    car.SpeedMpS = car.Flipped ? -SpeedMpS : SpeedMpS;
                }
            }

            if (FirstCar != null)
            {
                FirstCar.ThrottlePercent = AITrainThrottlePercent;
                FirstCar.BrakeSystem.AISetPercent(AITrainBrakePercent);
            }

        }

        public void AdjustControlsFixedSpeed(float reqSpeedMpS)
        {
            foreach (TrainCar car in Cars)
            {
                car.SpeedMpS = car.Flipped ? -reqSpeedMpS : reqSpeedMpS;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Create waiting point list
        /// <\summary>

        public override void BuildWaitingPointList(float clearingDistanceM)
        {
            int prevSection = -1;

            // loop through all waiting points - back to front as the processing affects the actual routepaths

            for (int iWait = TCRoute.WaitingPoints.Count - 1; iWait >= 0; iWait--)
            {
                int[] waitingPoint = TCRoute.WaitingPoints[iWait];

                TCSubpathRoute thisRoute = TCRoute.TCRouteSubpaths[waitingPoint[0]];
                int routeIndex = thisRoute.GetRouteIndex(waitingPoint[1], 0);
                int lastIndex = routeIndex;

                // check if waiting point is in route - else give warning and skip
                if (routeIndex < 0)
                {
                    Trace.TraceInformation("Waiting point for train " + Number.ToString() + " is not on route - point removed");
                    continue;
                }

                // waiting point is in same section as previous - add time to previous point, remove this point
                if (waitingPoint[1] == prevSection)
                {
                    int[] prevWP = TCRoute.WaitingPoints[iWait + 1];
                    prevWP[2] += waitingPoint[2];
                    TCRoute.WaitingPoints.RemoveAt(iWait);
                    StationStops[StationStops.Count - 1].DepartTime = -prevWP[2];
                    StationStops[StationStops.Count - 1].ActualDepart = -prevWP[2];
                    Trace.TraceInformation("Waiting points for train " + Number.ToString() + " combined, total time set to " + prevWP[2].ToString());
                    continue;
                }

                // check if section has signal

                prevSection = waitingPoint[1];  // save

                SignalObject exitSignal = null;
                float offset = 0.0f;
                bool endSectionFound = false;

                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisRoute[routeIndex].TCSectionIndex];
                TrackCircuitSection nextSection =
                    routeIndex < thisRoute.Count - 2 ? signalRef.TrackCircuitList[thisRoute[routeIndex + 1].TCSectionIndex] : null;

                int direction = thisRoute[routeIndex].Direction;
                if (thisSection.EndSignals[direction] != null)
                {
                    endSectionFound = true;
                    offset = thisSection.Length - clearingDistanceM - 1.0f; // 1 m short to force as first action
                    exitSignal = thisSection.EndSignals[direction];
                }

                // check if next section is junction

                else if (nextSection == null || nextSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.NORMAL)
                {
                    endSectionFound = true;
                    offset = thisSection.Length - junctionOverlapM;
                }

                // try and find next section with signal; if junction is found, stop search

                int nextIndex = routeIndex + 1;
                while (nextIndex < thisRoute.Count - 1 && !endSectionFound)
                {
                    nextSection = signalRef.TrackCircuitList[thisRoute[nextIndex].TCSectionIndex];
                    direction = thisRoute[nextIndex].Direction;

                    if (nextSection.EndSignals[direction] != null)
                    {
                        endSectionFound = true;
                        lastIndex = nextIndex;
                        offset = nextSection.Length - clearingDistanceM - 1.0f; // 1 m short to force as first action
                        exitSignal = nextSection.EndSignals[direction];
                    }
                    else if (nextSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.NORMAL)
                    {
                        endSectionFound = true;
                        lastIndex = nextIndex - 1;
                    }

                    if (!endSectionFound)
                    {
                        nextIndex++;
                        offset = nextSection.Length - junctionOverlapM;  // use this section length if next section is junction
                    }
                }

                // move sections beyond waiting point to next subroute

                TCSubpathRoute nextRoute = null;
                if ((waitingPoint[0] + 1) > (TCRoute.TCRouteSubpaths.Count - 1))
                {
                    nextRoute = new TCSubpathRoute();
                    TCRoute.TCRouteSubpaths.Add(nextRoute);
                    TCReversalInfo nextReversalPoint = new TCReversalInfo(); // also add dummy reversal info to match total number
                    TCRoute.ReversalInfo.Add(nextReversalPoint);
                }
                else
                {
                    nextRoute = TCRoute.TCRouteSubpaths[waitingPoint[0] + 1];
                }

                for (int iElement = thisRoute.Count - 1; iElement >= lastIndex + 1; iElement--)
                {
                    nextRoute.Insert(0, thisRoute[iElement]);
                    thisRoute.RemoveAt(iElement);
                }

                // repeat actual waiting section in next subroute (if not allready there)

                if (nextRoute.Count <= 0 || nextRoute[0].TCSectionIndex != thisRoute[thisRoute.Count - 1].TCSectionIndex)
                nextRoute.Insert(0, thisRoute[thisRoute.Count - 1]);

                // build station stop

                bool HoldSignal = exitSignal != null;
                int exitSignalReference = exitSignal != null ? exitSignal.thisRef : -1;

                int DepartTime = waitingPoint[2] > 0 ? -waitingPoint[2] : waitingPoint[3];

                StationStop thisStation = new StationStop(
                        -1,
                        null,
                        waitingPoint[0],
                        lastIndex,
                        thisRoute[lastIndex].TCSectionIndex,
                        direction,
                        exitSignalReference,
                        HoldSignal,
                        offset,
                        0,
                        DepartTime,
                        StationStop.STOPTYPE.WAITING_POINT);
                StationStops.Add(thisStation);
            }

	    // adjust station stop indices for removed subpaths
            for (int i = 0; i < StationStops.Count; i++)
            {
                var WPcur = StationStops[i];
                for (int iTC = TCRoute.TCRouteSubpaths.Count - 1; iTC >= 0; iTC--)
                {
                    var tcRS = TCRoute.TCRouteSubpaths[iTC];
                    for (int iTCE = tcRS.Count - 1; iTCE >= 0; iTCE--)
                    {
                        var tcSR = tcRS[iTCE];
                        if (WPcur.TCSectionIndex == tcSR.TCSectionIndex)
                        {
                            WPcur.SubrouteIndex = iTC;
                            WPcur.RouteIndex = iTCE;
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Initialize brakes for AI trains
        /// <\summary>

        public override void InitializeBrakes()
        {
            float maxPressurePSI = 90;
            BrakeLine3PressurePSI = BrakeLine4PressurePSI = 0;
            BrakeLine1PressurePSI = BrakeLine2PressurePSI = maxPressurePSI;
            foreach (TrainCar car in Cars)
            {
                car.BrakeSystem.Initialize(false, maxPressurePSI, true);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Process end of path 
        /// <\summary>

        public void ProcessEndOfPath()
        {
            int directionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].Direction;
            int positionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].TCSectionIndex;

            bool[] nextPart = UpdateRouteActions(0);

            if (!nextPart[0]) return;   // not at end

            if (nextPart[1])   // next route available
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

                if (positionNow == PresentPosition[0].TCSectionIndex && directionNow != PresentPosition[0].TCDirection)
                {
                    ReverseFormation(false);

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                         Number.ToString() + " reversed\n");
#endif
                }
                else if (positionNow == PresentPosition[1].TCSectionIndex && directionNow != PresentPosition[1].TCDirection)
                {
                    ReverseFormation(false);

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

                // check if next station was on previous subpath - if so, move to this subpath

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

                        if (ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, 0) < 0) // station no longer on route
                        {
                            if (thisStation.ExitSignal >= 0 && HoldingSignals.Contains(thisStation.ExitSignal))
                            {
                                HoldingSignals.Remove(thisStation.ExitSignal);
                            }
                            StationStops.RemoveAt(0);
                        }
                    }
                }

                // reset to node control, also reset required actions

                SwitchToNodeControl(-1);
                ResetActions(true);

            }
            else
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
                RemoveTrain();
            }
        }

        //================================================================================================//
        /// <summary>
        /// Remove train
        /// <\summary>

        public void RemoveTrain()
        {
            RemoveFromTrack();

            // clear deadlocks
            foreach (KeyValuePair<int, List<Dictionary<int, int>>> thisDeadlock in DeadlockInfo)
            {
#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\Temp\deadlock.txt", "Removed Train : " + Number.ToString() + "\n");
                File.AppendAllText(@"C:\Temp\deadlock.txt", "Deadlock at section : " + thisDeadlock.Key.ToString() + "\n");
#endif
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisDeadlock.Key];
                foreach (Dictionary<int, int> deadlockTrapInfo in thisDeadlock.Value)
                {
                    foreach (KeyValuePair<int, int> deadlockedTrain in deadlockTrapInfo)
                    {
                        Train otherTrain = GetOtherTrainByNumber(deadlockedTrain.Key);

#if DEBUG_DEADLOCK
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "Other train index : " + deadlockedTrain.Key.ToString() + "\n");
                        if (otherTrain == null)
                        {
                            File.AppendAllText(@"C:\Temp\deadlock.txt", "Other train not found!" + "\n");
                        }
                        else
                        {
                            File.AppendAllText(@"C:\Temp\deadlock.txt", "CrossRef train info : " + "\n");
                            foreach (KeyValuePair<int, List<Dictionary<int, int>>> reverseDeadlock in otherTrain.DeadlockInfo)
                            {
                                File.AppendAllText(@"C:\Temp\deadlock.txt", "   " + reverseDeadlock.Key.ToString() + "\n");
                            }

                            foreach (KeyValuePair<int, List<Dictionary<int, int>>> reverseDeadlock in otherTrain.DeadlockInfo)
                            {
                                if (reverseDeadlock.Key == deadlockedTrain.Value)
                                {
                                    File.AppendAllText(@"C:\Temp\deadlock.txt", "Reverse Info : " + "\n");
                                    foreach (Dictionary<int, int> sectorList in reverseDeadlock.Value)
                                    {
                                        foreach (KeyValuePair<int, int> reverseInfo in sectorList)
                                        {
                                            File.AppendAllText(@"C:\Temp\deadlock.txt", "   " + reverseInfo.Key.ToString() + " + " + reverseInfo.Value.ToString() + "\n");
                                        }
                                    }
                                }
                            }
                        }
#endif
                        if (otherTrain != null && otherTrain.DeadlockInfo.ContainsKey(deadlockedTrain.Value))
                        {
                            List<Dictionary<int, int>> otherDeadlock = otherTrain.DeadlockInfo[deadlockedTrain.Value];
                            for (int iDeadlock = otherDeadlock.Count - 1; iDeadlock >= 0; iDeadlock--)
                            {
                                Dictionary<int, int> otherDeadlockInfo = otherDeadlock[iDeadlock];
                                if (otherDeadlockInfo.ContainsKey(Number)) otherDeadlockInfo.Remove(Number);
                                if (otherDeadlockInfo.Count <= 0) otherDeadlock.RemoveAt(iDeadlock);
                            }

                            if (otherDeadlock.Count <= 0)
                                otherTrain.DeadlockInfo.Remove(deadlockedTrain.Value);

                            if (otherTrain.DeadlockInfo.Count <= 0)
                                thisSection.ClearDeadlockTrap(otherTrain.Number);
                        }
                        TrackCircuitSection otherSection = signalRef.TrackCircuitList[deadlockedTrain.Value];
                        otherSection.ClearDeadlockTrap(Number);
                    }
                }
            }

#if DEBUG_DEADLOCK
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
            // remove train

            AI.TrainsToRemove.Add(this);
        }

        //================================================================================================//
        /// <summary>
        /// Insert action item
        /// <\summary>

        public void CreateTrainAction(float presentSpeedMpS, float reqSpeedMpS, float distanceToTrainM,
                ObjectItemInfo thisItem, AIActionItem.AI_ACTION_TYPE thisAction)
        {

            // if signal or speed limit take off clearing distance

            float activateDistanceTravelledM = PresentPosition[0].DistanceTravelledM + distanceToTrainM;
            if (thisItem != null)
            {
                activateDistanceTravelledM -= clearingDistanceM;
            }

            // calculate braking distance

            float firstPartTime = 0.0f;
            float firstPartRangeM = 0.0f;
            float secndPartRangeM = 0.0f;
            float remainingRangeM = activateDistanceTravelledM - PresentPosition[0].DistanceTravelledM;

            float triggerDistanceM = PresentPosition[0].DistanceTravelledM; // worst case

            // braking distance based on max speed - use 0.25 * MaxDecelMpSS as average deceleration (due to braking delay)
            // T = deltaV / A
            float fullPartTime = (AllowedMaxSpeedMpS - reqSpeedMpS) / (0.25f * MaxDecelMpSS);
            // R = 0.5 * Vstart * T + 0.5 * A * T**2 
            // 0.5 * Vstart is average speed over used time, 0.5 * Vstart * T is related distance covered , 0.5 A T**2 is distance covered to reduce speed
            float fullPartRangeM = (0.5f * 0.25f * MaxDecelMpSS * fullPartTime * fullPartTime) + ((AllowedMaxSpeedMpS - reqSpeedMpS) * 0.5f * fullPartTime);

            if (presentSpeedMpS > reqSpeedMpS)   // if present speed higher, brake distance is always required (same equation)
            {
                firstPartTime = (presentSpeedMpS - reqSpeedMpS) / (0.25f * MaxDecelMpSS);
                firstPartRangeM = (0.5f * 0.25f * MaxDecelMpSS * firstPartTime * firstPartTime) + ((presentSpeedMpS - reqSpeedMpS) * 0.5f * fullPartTime);
            }

            if (firstPartRangeM > remainingRangeM)
            {
                triggerDistanceM = activateDistanceTravelledM - firstPartRangeM;
            }

                // if brake from max speed is possible taking into account acc up to full speed, use it as braking distance
            else if (fullPartRangeM < (remainingRangeM - ((AllowedMaxSpeedMpS - reqSpeedMpS) * (AllowedMaxSpeedMpS - reqSpeedMpS) * 0.5 / MaxAccelMpSS)))
            {
                triggerDistanceM = activateDistanceTravelledM - fullPartRangeM;
            }

            // if distance from max speed is too long and from present speed too short and train not at max speed,
            // remaining distance calculation :
            // max. time to reach allowed max speed : Tacc = (Vmax - Vnow) / MaxAccel
            // max. time to reduce speed from max back to present speed : Tdec = (Vmax - Vnow) / 0.25 * MaxDecel
            // convered distance : R = Vnow*(Tacc + Tdec) + 0.5 * MaxAccel * Tacc**2 + 0.5 * 0*25 * MaxDecel * Tdec**2
            else
            {
                secndPartRangeM = 0;
                if (SpeedMpS < presentSpeedMpS)
                {
                    float Tacc = (presentSpeedMpS - SpeedMpS) / MaxAccelMpSS;
                    float Tdec = (presentSpeedMpS - SpeedMpS) / 0.25f * MaxDecelMpSS;
                    secndPartRangeM = (SpeedMpS * (Tacc + Tdec)) + (0.5f * MaxAccelMpSS * (Tacc * Tacc)) + (0.5f * 0.25f * MaxDecelMpSS * (Tdec * Tdec));
                }

                triggerDistanceM = activateDistanceTravelledM - (firstPartRangeM + secndPartRangeM);
            }

            // create and insert action

            AIActionItem newAction = new AIActionItem(triggerDistanceM, reqSpeedMpS, activateDistanceTravelledM,
                    DistanceTravelledM, thisItem, thisAction);

            requiredActions.InsertAction(newAction);

#if DEBUG_REPORTS
            if (thisItem != null && thisItem.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Insert for train " +
                         Number.ToString() + ", type " +
                         thisAction.ToString() + " for signal " +
                         thisItem.ObjectDetails.thisRef.ToString() + ", at " +
                         FormatStrings.FormatDistance(activateDistanceTravelledM, true) + ", trigger at " +
                         FormatStrings.FormatDistance(triggerDistanceM, true) + " (now at " +
                         FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
            }
            else
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Insert for train " +
                         Number.ToString() + ", type " +
                         thisAction.ToString() + " at " +
                         FormatStrings.FormatDistance(activateDistanceTravelledM, true) + ", trigger at " +
                         FormatStrings.FormatDistance(triggerDistanceM, true) + " (now at " +
                         FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
            }
#endif
            if (CheckTrain)
            {
                if (thisItem != null && thisItem.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Insert for train " +
                             Number.ToString() + ", type " +
                             thisAction.ToString() + " for signal " +
                             thisItem.ObjectDetails.thisRef.ToString() + ", at " +
                             FormatStrings.FormatDistance(activateDistanceTravelledM, true) + ", trigger at " +
                             FormatStrings.FormatDistance(triggerDistanceM, true) + " (now at " +
                             FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Insert for train " +
                             Number.ToString() + ", type " +
                             thisAction.ToString() + " at " +
                             FormatStrings.FormatDistance(activateDistanceTravelledM, true) + ", trigger at " +
                             FormatStrings.FormatDistance(triggerDistanceM, true) + " (now at " +
                             FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Insert action item for end-of-route
        /// <\summary>

        public void SetEndOfRouteAction()
        {
            // remaining length first section

            TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex];
            float lengthToGoM = thisSection.Length - PresentPosition[0].TCOffset;

            // go through all further sections

            for (int iElement = PresentPosition[0].RouteListIndex + 1; iElement < ValidRoute[0].Count; iElement++)
            {
                TCRouteElement thisElement = ValidRoute[0][iElement];
                thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                lengthToGoM += thisSection.Length;
            }

            lengthToGoM -= 5.0f; // keep save distance from end

            CreateTrainAction(TrainMaxSpeedMpS, 0.0f, lengthToGoM, null,
                    AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE);
            NextStopDistanceM = lengthToGoM;
        }

        //================================================================================================//
        /// <summary>
        /// Reset action list
        /// <\summary>

        public void ResetActions(bool setEndOfPath)
        {
#if DEBUG_REPORTS
            if (nextActionInfo != null)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Reset all for train " +
                         Number.ToString() + ", type " +
                         nextActionInfo.NextAction.ToString() + ", at " +
                         FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + ", trigger at " +
                         FormatStrings.FormatDistance(nextActionInfo.RequiredDistance, true) + " (now at " +
                         FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                         FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
            }
            else
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Reset all for train " +
                         Number.ToString() + " (now at " +
                         FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
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
                             FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + ", trigger at " +
                             FormatStrings.FormatDistance(nextActionInfo.RequiredDistance, true) + " (now at " +
                             FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Reset all for train " +
                             Number.ToString() + " (now at " +
                             FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                }
            }

            nextActionInfo = null;
            foreach (ObjectItemInfo thisInfo in SignalObjectItems)
            {
                thisInfo.processed = false;
            }
            requiredActions.RemovePendingAIActionItems(false);

            if (StationStops.Count > 0)
                SetNextStationAction();

            if (setEndOfPath)
            {
                SetEndOfRouteAction();
            }
        }

        //================================================================================================//
        /// <summary>
        /// Perform stored actions
        /// <\summary>

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
                    SetAIPendingSpeedLimit(thisAction as ActivateSpeedLimit);
                }
                else if (thisAction is AIActionItem)
                {
                    ProcessActionItem(thisAction as AIActionItem);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Set pending speed limits
        /// <\summary>

        public void SetAIPendingSpeedLimit(ActivateSpeedLimit speedInfo)
        {
            if (speedInfo.MaxSpeedMpSSignal > 0)
            {
                allowedMaxSpeedSignalMpS = speedInfo.MaxSpeedMpSSignal;
                AllowedMaxSpeedMpS = Math.Min(allowedMaxSpeedLimitMpS, speedInfo.MaxSpeedMpSSignal);

            }
            if (speedInfo.MaxSpeedMpSLimit > 0)
            {
                allowedMaxSpeedLimitMpS = speedInfo.MaxSpeedMpSLimit;
                AllowedMaxSpeedMpS = speedInfo.MaxSpeedMpSLimit;
            }

            if (MovementState == AI_MOVEMENT_STATE.RUNNING && SpeedMpS < AllowedMaxSpeedMpS - 2.0f * hysterisMpS)
            {
                MovementState = AI_MOVEMENT_STATE.ACCELERATING;
                Alpha10 = 10;
            }

            // reset pending actions to recalculate braking distance

            ResetActions(true);
        }

        //================================================================================================//
        /// <summary>
        /// Process pending actions
        /// <\summary>

        public void ProcessActionItem(AIActionItem thisItem)
        {

            // normal actions

            bool actionValid = true;
            bool actionCleared = false;

#if DEBUG_REPORTS
            if (thisItem.ActiveItem != null && thisItem.ActiveItem.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Activated for train " +
                         Number.ToString() + ", type " +
                         thisItem.NextAction.ToString() + " for signal " +
                         thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + ", at " +
                         FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", trigger at " +
                         FormatStrings.FormatDistance(thisItem.RequiredDistance, true) + " (now at " +
                         FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                         FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
            }
            else
            {
                File.AppendAllText(@"C:\temp\printproc.txt", "Activated for train " +
                         Number.ToString() + ", type " +
                         thisItem.NextAction.ToString() + " at " +
                         FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", trigger at " +
                         FormatStrings.FormatDistance(thisItem.RequiredDistance, true) + " (now at " +
                         FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                         FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
            }
#endif
            if (CheckTrain)
            {
                if (thisItem.ActiveItem != null && thisItem.ActiveItem.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Activated for train " +
                             Number.ToString() + ", type " +
                             thisItem.NextAction.ToString() + " for signal " +
                             thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + ", at " +
                             FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", trigger at " +
                             FormatStrings.FormatDistance(thisItem.RequiredDistance, true) + " (now at " +
                             FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                }
                else
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Activated for train " +
                             Number.ToString() + ", type " +
                             thisItem.NextAction.ToString() + " at " +
                             FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", trigger at " +
                             FormatStrings.FormatDistance(thisItem.RequiredDistance, true) + " (now at " +
                             FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                }
            }

            // if signal speed, check if still set

            if (thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SPEED_SIGNAL)
            {
                if (thisItem.ActiveItem.actual_speed == AllowedMaxSpeedMpS)  // no longer valid
                {
                    actionValid = false;
                }
                else if (thisItem.ActiveItem.actual_speed != thisItem.RequiredSpeedMpS)
                {
                    actionValid = false;
                }
            }

            // if signal, check if not held for station stop (station stop comes first)

            else if (thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
            {
                if (thisItem.ActiveItem.signal_state == SignalHead.SIGASP.STOP &&
                    thisItem.ActiveItem.ObjectDetails.holdState == SignalObject.HOLDSTATE.STATION_STOP)
                {
                    actionValid = false;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " : signal " +
                            thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                            FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + " is held for station stop\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " : signal " +
                                thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + " is held for station stop\n");
                    }

                }

            // check if cleared

                else if (thisItem.ActiveItem.signal_state >= SignalHead.SIGASP.APPROACH_1)
                {
                    actionValid = false;
                    actionCleared = true;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " : signal " +
                            thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                            FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + " cleared\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " : signal " +
                                thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + " cleared\n");
                    }
                }

            // check if restricted

                else if (thisItem.ActiveItem.signal_state != SignalHead.SIGASP.STOP)
                {
                    thisItem.NextAction = AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED;
                    if ((thisItem.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM)
                    {
                        actionValid = false;
                        actionCleared = true;

#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " : signal " +
                            thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                            FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + " set to RESTRICTED\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " : signal " +
                                thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + " set to RESTRICTED\n");
                        }
                    }
                }

                // recalculate braking distance if train is running slow
                if (actionValid && SpeedMpS < creepSpeedMpS)
                {
                    float firstPartTime = 0.0f;
                    float firstPartRangeM = 0.0f;
                    float secndPartRangeM = 0.0f;
                    float remainingRangeM = thisItem.ActivateDistanceM - PresentPosition[0].DistanceTravelledM;

                    if (SpeedMpS > thisItem.RequiredSpeedMpS)   // if present speed higher, brake distance is always required
                    {
                        firstPartTime = (SpeedMpS - thisItem.RequiredSpeedMpS) / (0.25f * MaxDecelMpSS);
                        firstPartRangeM = 0.25f * MaxDecelMpSS * (firstPartTime * firstPartTime);
                    }

                    if (firstPartRangeM < remainingRangeM && SpeedMpS < TrainMaxSpeedMpS) // if distance left and not at max speed
                    // split remaining distance based on relation between acceleration and deceleration
                    {
                        secndPartRangeM = (remainingRangeM - firstPartRangeM) * (2.0f * MaxDecelMpSS) / (MaxDecelMpSS + MaxAccelMpSS);
                    }

                    float fullRangeM = firstPartRangeM + secndPartRangeM;
                    if (fullRangeM < remainingRangeM && remainingRangeM > 300.0f) // if range is shorter and train not too close, reschedule
                    {
                        actionValid = false;
                        thisItem.RequiredDistance = thisItem.ActivateDistanceM - fullRangeM;
                        requiredActions.InsertAction(thisItem);

#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Rescheduled for train " +
                             Number.ToString() + ", type " +
                             thisItem.NextAction.ToString() + " for signal " +
                             thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + ", at " +
                             FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", trigger at " +
                             FormatStrings.FormatDistance(thisItem.RequiredDistance, true) + " (now at " +
                             FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                             FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Rescheduled for train " +
                                 Number.ToString() + ", type " +
                                 thisItem.NextAction.ToString() + " for signal " +
                                 thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + ", at " +
                                 FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", trigger at " +
                                 FormatStrings.FormatDistance(thisItem.RequiredDistance, true) + " (now at " +
                                 FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                                 FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                        }
                    }
                }
            }

    // if signal at RESTRICTED, check if not cleared

            else if (thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED)
            {
                if (thisItem.ActiveItem.signal_state >= SignalHead.SIGASP.APPROACH_1 ||
                (thisItem.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM)
                {
                    actionValid = false;

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                            Number.ToString() + " : signal " +
                            thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                            FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + " cleared\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " : signal " +
                                thisItem.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + " cleared\n");
                    }
                }
            }

    // get station stop, recalculate with present speed if required

            else if (thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
            {
                float[] distancesM = CalculateDistancesToNextStation(StationStops[0], SpeedMpS, true);

                if (distancesM[1] - 300.0f > DistanceTravelledM) // trigger point more than 300m away
                {
                    actionValid = false;
                    thisItem.RequiredDistance = distancesM[1];
                    thisItem.ActivateDistanceM = distancesM[0];
                    requiredActions.InsertAction(thisItem);

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "StationStop rescheduled for train " +
                        Number.ToString() + ", at " +
                        FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", trigger at " +
                        FormatStrings.FormatDistance(thisItem.RequiredDistance, true) + " ( now at " +
                        FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                        FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "StationStop rescheduled for train " +
                            Number.ToString() + ", at " +
                            FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", trigger at " +
                            FormatStrings.FormatDistance(thisItem.RequiredDistance, true) + " ( now at " +
                            FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                            FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
            }

            // if still valid - check if at station and signal is exit signal

            if (actionValid && nextActionInfo != null &&
                nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP &&
                thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
            {
                int signalIdent = thisItem.ActiveItem.ObjectDetails.thisRef;
                if (signalIdent == StationStops[0].ExitSignal)
                {
                    actionValid = false;
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

            // if still valid - check if more severe as existing action

            if (actionValid)
            {
                if (nextActionInfo != null)
                {
                    bool earlier = false;
                    float thisDistanceToTrainM = thisItem.ActivateDistanceM - DistanceTravelledM;
                    float nextDistanceToTrainM = nextActionInfo.ActivateDistanceM - DistanceTravelledM;

                    if (thisItem.ActivateDistanceM < nextActionInfo.ActivateDistanceM)
                    {
                        if (thisItem.RequiredSpeedMpS <= nextActionInfo.RequiredSpeedMpS)
                        {
                            earlier = true;
                        }
                        else  // new requirement earlier with higher speed - check if enough braking distance remaining
                        {
                            float deltaTime = (thisItem.RequiredSpeedMpS - nextActionInfo.RequiredSpeedMpS) / MaxDecelMpSS;
                            float brakingDistanceM = (thisItem.RequiredSpeedMpS * deltaTime) + (0.5f * MaxDecelMpSS * deltaTime * deltaTime);

                            if (brakingDistanceM < (nextActionInfo.ActivateDistanceM - thisItem.ActivateDistanceM))
                            {
                                earlier = true;
                            }
                        }
                    }
                    else if (thisItem.RequiredSpeedMpS < nextActionInfo.RequiredSpeedMpS)
                    // new requirement further but with lower speed - check if enough braking distance left
                    {
                        float deltaTime = (nextActionInfo.RequiredSpeedMpS - thisItem.RequiredSpeedMpS) / MaxDecelMpSS;
                        float brakingDistanceM = (nextActionInfo.RequiredSpeedMpS * deltaTime) + (0.5f * MaxDecelMpSS * deltaTime * deltaTime);

                        if (brakingDistanceM > (thisItem.ActivateDistanceM - nextActionInfo.ActivateDistanceM))
                        {
                            earlier = true;
                        }
                    }

                    // if earlier : check if present action is station stop, new action is signal - if so, check is signal really in front of or behind station stop

                    if (earlier && thisItem.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP &&
                                 nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                    {
                        float newposition = thisItem.ActivateDistanceM + 0.75f * clearingDistanceM; // correct with clearing distance - leave smaller gap
                        float actposition = nextActionInfo.ActivateDistanceM;

                        if (actposition < newposition) earlier = false;

                        if (!earlier && CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "allowing minimum gap : " + newposition.ToString() + " and " + actposition.ToString() + "\n");
                        }
                    }

                    // reject if less severe (will be rescheduled if active item is cleared)

                    if (!earlier)
                    {
                        actionValid = false;

#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Rejected : Train " +
                             Number.ToString() + " : this " +
                             FormatStrings.FormatSpeed(thisItem.RequiredSpeedMpS, true) + " at " +
                             FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", active " +
                             FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " at " +
                             FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + "\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Rejected : Train " +
                                 Number.ToString() + " : this " +
                                 FormatStrings.FormatSpeed(thisItem.RequiredSpeedMpS, true) + " at " +
                                 FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", active " +
                                 FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " at " +
                                 FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + "\n");
                        }
                    }
                    else
                    {
#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Accepted : Train " +
                             Number.ToString() + " : this " +
                             FormatStrings.FormatSpeed(thisItem.RequiredSpeedMpS, true) + " at " +
                             FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", active " +
                             FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " at " +
                             FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + "\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Accepted : Train " +
                                 Number.ToString() + " : this " +
                                 FormatStrings.FormatSpeed(thisItem.RequiredSpeedMpS, true) + " at " +
                                 FormatStrings.FormatDistance(thisItem.ActivateDistanceM, true) + ", active " +
                                 FormatStrings.FormatSpeed(nextActionInfo.RequiredSpeedMpS, true) + " at " +
                                 FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + "\n");
                        }
                    }
                }
            }

            // if still valid, set as action, set state to braking if still running

            if (actionValid)
            {
#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Validated\n");
#endif
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Validated\n");
                }
                nextActionInfo = thisItem;
                if (nextActionInfo.RequiredSpeedMpS == 0)
                {
                    NextStopDistanceM = thisItem.ActivateDistanceM - PresentPosition[0].DistanceTravelledM;
                    if (AI.PreUpdate)
                    {
                        AITrainBrakePercent = 100; // because of short reaction time
                        AITrainThrottlePercent = 0;
                    }
                }

#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number.ToString() +
                " , Present state : " + MovementState.ToString() + "\n");

#endif

                if (MovementState != AI_MOVEMENT_STATE.STATION_STOP && MovementState != AI_MOVEMENT_STATE.STOPPED)
                {
                    MovementState = AI_MOVEMENT_STATE.BRAKING;
                    Alpha10 = 10;
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
                // reset actions - ensure next action is validated

                ResetActions(true);
            }
        }

        //================================================================================================//
        //
        // Extra actions when alternative route is set
        //

        public override void SetAlternativeRoute(int startElementIndex, int altRouteIndex, SignalObject nextSignal)
        {
            base.SetAlternativeRoute(startElementIndex, altRouteIndex, nextSignal);

            // reset actions to recalculate distances

            ResetActions(true);
        }


        //================================================================================================//
        /// <summary>
        /// Add movement status to train status string
        /// <\summary>

        public String[] AddMovementState(String[] stateString, bool metric)
        {
            String[] retString = new String[stateString.Length];
            stateString.CopyTo(retString, 0);

            string movString = "";
            switch (MovementState)
            {
                case AI_MOVEMENT_STATE.INIT:
                    movString = "INI ";
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
            }

            string abString = AITrainThrottlePercent.ToString("000");
            abString = String.Concat(abString, "&", AITrainBrakePercent.ToString("000"));

            if (MovementState == AI_MOVEMENT_STATE.STATION_STOP)
            {
                DateTime baseDT = new DateTime();
                if (StationStops[0].DepartTime > 0)
                {
                    DateTime depTime = baseDT.AddSeconds(StationStops[0].DepartTime);
                    abString = depTime.ToString("HH:mm:ss");
                }
                else if (StationStops[0].ActualDepart > 0)
                {
                    DateTime depTime = baseDT.AddSeconds(StationStops[0].ActualDepart);
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
                }

                retString[7] = String.Copy(actString);
                retString[8] = FormatStrings.FormatDistance(
                        nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM, metric);

            }

            retString[4] = String.Copy(movString);
            retString[5] = String.Copy(abString);
            retString[11] = String.Copy(nameString);

            return (retString);
        }

    }

    //================================================================================================//
    /// <summary>
    /// AIActionItem class : class to hold info on next restrictive action
    /// <\summary>


    public class AIActionItem : Train.DistanceTravelledItem
    {
        public float RequiredSpeedMpS = 0;
        public float ActivateDistanceM = 0;
        public float InsertedDistanceM = 0;
        public ObjectItemInfo ActiveItem = null;

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
            NONE
        }

        public AI_ACTION_TYPE NextAction = AI_ACTION_TYPE.NONE;

        //================================================================================================//
        /// <summary>
        /// constructor for AIActionItem
        /// </summary>

        public AIActionItem(float distance, float requiredSpeedMpS, float activateDistance, float insertedDistance,
            ObjectItemInfo thisItem, AI_ACTION_TYPE thisAction)
        {
            RequiredDistance = distance;
            RequiredSpeedMpS = requiredSpeedMpS;
            ActivateDistanceM = activateDistance;
            InsertedDistanceM = insertedDistance;
            ActiveItem = thisItem;
            NextAction = thisAction;

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

            bool validActiveItem = inf.ReadBoolean();
            ActiveItem = null;

            if (validActiveItem)
            {
                ActiveItem = RestoreActiveItem(inf, signalRef);
            }

            NextAction = (AI_ACTION_TYPE)inf.ReadInt32();
        }

        public ObjectItemInfo RestoreActiveItem(BinaryReader inf, Signals signalRef)
        {

            ObjectItemInfo thisInfo = new ObjectItemInfo(ObjectItemInfo.ObjectItemFindState.NONE_FOUND);

            thisInfo.ObjectType = (ObjectItemInfo.ObjectItemType)inf.ReadInt32();
            thisInfo.ObjectState = (ObjectItemInfo.ObjectItemFindState)inf.ReadInt32();

            int signalIndex = inf.ReadInt32();
            thisInfo.ObjectDetails = signalRef.SignalObjects[signalIndex];

            thisInfo.distance_found = inf.ReadSingle();
            thisInfo.distance_to_train = inf.ReadSingle();
            thisInfo.distance_to_object = inf.ReadSingle();

            thisInfo.speed_passenger = inf.ReadSingle();
            thisInfo.speed_freight = inf.ReadSingle();
            thisInfo.speed_flag = inf.ReadUInt32();
            thisInfo.actual_speed = inf.ReadSingle();

            thisInfo.processed = inf.ReadBoolean();

            thisInfo.signal_state = SignalHead.SIGASP.UNKNOWN;
            if (thisInfo.ObjectDetails.isSignal)
            {
                thisInfo.signal_state = thisInfo.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
            }

            return (thisInfo);
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

        public void SaveActiveItem(BinaryWriter outf, ObjectItemInfo ActiveItem)
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
    }

}
