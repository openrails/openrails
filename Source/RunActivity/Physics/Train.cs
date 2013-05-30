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

/* TRAINS
 * 
 * Contains code to represent a train as a list of TrainCars and to handle the physics of moving
 * the train through the Track Database.
 * 
 * A train has:
 *  - a list of TrainCars 
 *  - a front and back position in the TDB ( represented by TDBTravellers )
 *  - speed
 *  - MU signals that are relayed from player locomtive to other locomotives and cars such as:
 *      - direction
 *      - throttle percent
 *      - brake percent  ( TODO, this should be changed to brake pipe pressure )
 *      
 *  Individual TrainCars provide information on friction and motive force they are generating.
 *  This is consolidated by the train class into overall movement for the train.
 */

// Compiler flags for debug print-out facilities
// #define DEBUG_TEST
// #define DEBUG_REPORTS
// #define DEBUG_DEADLOCK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using MSTS;
using ORTS.MultiPlayer;
using ORTS.Popups;

namespace ORTS
{
    public class Train
    {
        public List<TrainCar> Cars = new List<TrainCar>();           // listed front to back
        public int Number;
        public static int TotalNumber = 0;
        public TrainCar FirstCar
        {
            get
            {
                return Cars[0];
            }
        }
        public TrainCar LastCar
        {
            get
            {
                return Cars[Cars.Count - 1];
            }
        }
        public Traveller RearTDBTraveller;               // positioned at the back of the last car in the train
        public Traveller FrontTDBTraveller;              // positioned at the front of the train by CalculatePositionOfCars
        public float Length;                             // length of train from FrontTDBTraveller to RearTDBTraveller
        public float SpeedMpS = 0.0f;                    // meters per second +ve forward, -ve when backing
        float LastSpeedMpS;                              // variable to remember last speed used for projected speed
        SmoothedData AccelerationMpSpS = new SmoothedData(); // smoothed acceleration data
        public float ProjectedSpeedMpS;                  // projected speed
        public float LastReportedSpeed;

        public Train UncoupledFrom = null;               // train not to coupled back onto
        public float TotalCouplerSlackM = 0;
        public float MaximumCouplerForceN = 0;
        public int NPull = 0;
        public int NPush = 0;
        public int LeadLocomotiveIndex = -1;
        public bool IsFreight = false;
        public float SlipperySpotDistanceM = 0;          // distance to extra slippery part of track
        public float SlipperySpotLengthM = 0;

        // These signals pass through to all cars and locomotives on the train
        public Direction MUDirection = Direction.N;      // set by player locomotive to control MU'd locomotives
        public float MUThrottlePercent = 0;              // set by player locomotive to control MU'd locomotives
        public float MUReverserPercent = 100;            // steam engine direction/cutoff control for MU'd locomotives
        public float MUDynamicBrakePercent = -1;         // dynamic brake control for MU'd locomotives, <0 for off
        public float BrakeLine1PressurePSI = 90;         // set by player locomotive to control entire train brakes
        public float BrakeLine2PressurePSI = 0;          // extra line for dual line systems, main reservoir
        public float BrakeLine3PressurePSI = 0;          // extra line just in case, engine brake pressure
        public float BrakeLine4PressurePSI = 0;          // extra line just in case, ep brake control line
        public RetainerSetting RetainerSetting = RetainerSetting.Exhaust;
        public int RetainerPercent = 100;

        public enum TRAINTYPE
        {
            PLAYER,
            STATIC,
            AI,
            AI_NOTSTARTED,
            REMOTE
        }

        public TRAINTYPE TrainType = TRAINTYPE.PLAYER;

        public float distanceToSignal = 0.1f;
        public List<ObjectItemInfo> SignalObjectItems;
        public int IndexNextSignal = -1;                 // Index in SignalObjectItems for next signal
        public int IndexNextSpeedlimit = -1;             // Index in SignalObjectItems for next speedpost
        public SignalObject[] NextSignalObject = new SignalObject[2];  // direct reference to next signal

        public TrackMonitorSignalAspect TMaspect = TrackMonitorSignalAspect.None;

        public SignalHead.SIGASP CABAspect = SignalHead.SIGASP.UNKNOWN; // By GeorgeS

        public float TrainMaxSpeedMpS = 0;               // Max speed as set by route (default value)
        public float AllowedMaxSpeedMpS = 0;             // Max speed as allowed
        public float allowedMaxSpeedSignalMpS = 0;       // Max speed as set by signal
        public float allowedMaxSpeedLimitMpS = 0;        // Max speed as set by limit
        public float maxTimeS = 120;                     // check ahead for distance covered in 2 mins.
        public float minCheckDistanceM = 5000;           // minimum distance to check ahead
        public float minCheckDistanceManualM = 3000;     // minimum distance to check ahead in manual mode

        public float standardOverlapM = 15.0f;           // standard overlap on clearing sections
        public float junctionOverlapM = 75.0f;           // standard overlap on clearing sections
        private float rearPositionOverlap = 25.0f;       // allowed overlap when slipping
        private float standardWaitTimeS = 60.0f;         // wait for 1 min before claim state
        private float backwardThreshold = 20;            // counter threshold to detect backward move

        protected Signals signalRef;                     // reference to main Signals class
        public TCRoutePath TCRoute = null;               // train path converted to TC base
        public TCSubpathRoute[] ValidRoute = new TCSubpathRoute[2] { null, null };  // actual valid path
        public TCSubpathRoute TrainRoute = null;         // partial route under train for Manual mode
        public bool ClaimState = false;                  // train is allowed to perform claim on sections
        public float actualWaitTimeS = 0.0f;             // actual time waiting for signal
        public int movedBackward = 0;                    // counter to detect backward move
        public float waitingPointWaitTimeS = -1.0f;      // time due at waiting point (PLAYER train only, valid in >= 0)

        public List<TrackCircuitSection> OccupiedTrack = new List<TrackCircuitSection>();
        public List<int> HoldingSignals = new List<int>();// list of signals which must not be cleared (eg station stops)
        public List<StationStop> StationStops = new List<StationStop>();  //list of station stop details
        public Traffic_Service_Definition TrafficService = null;
        public int[] MisalignedSwitch = new int[2] { -1, -1 };  // misaligned switch indication :
        // cell 0 : index of switch, cell 1 : required linked section; -1 if not valid
        public Dictionary<int, float> PassedSignalSpeeds = new Dictionary<int, float>();  // list of signals and related speeds pending processing (manual and explorer mode)
        public int[] LastPassedSignal = new int[2] { -1, -1 };  // index of last signal which set speed limit per direction (manual and explorer mode)

        public TrainRouted routedForward = null;          // routed train class for forward moves (used in signalling)
        public TrainRouted routedBackward = null;         // routed train class for backward moves (used in signalling)

        public enum TRAIN_CONTROL
        {
            AUTO_SIGNAL,
            AUTO_NODE,
            MANUAL,
            EXPLORER,
            OUT_OF_CONTROL,
            UNDEFINED
        }

        public TRAIN_CONTROL ControlMode = TRAIN_CONTROL.UNDEFINED;     // train control mode

        public enum OUTOFCONTROL
        {
            SPAD,
            SPAD_REAR,
            MISALIGNED_SWITCH,
            OUT_OF_AUTHORITY,
            OUT_OF_PATH,
            SLIPPED_INTO_PATH,
            SLIPPED_TO_ENDOFTRACK,
            OUT_OF_TRACK,
            UNDEFINED
        }

        public OUTOFCONTROL OutOfControlReason = OUTOFCONTROL.UNDEFINED; // train out of control

        public TCPosition[] PresentPosition = new TCPosition[2] { new TCPosition(), new TCPosition() };         // present position : 0 = front, 1 = rear
        public TCPosition[] PreviousPosition = new TCPosition[2] { new TCPosition(), new TCPosition() };        // previous train position

        public float DistanceTravelledM = 0;                             // actual distance travelled

        public float travelled = 0;                                      // distance travelled, but not exactly
        public DistanceTravelledActions requiredActions = new DistanceTravelledActions(); // distance travelled action list

        public float ClearanceAtRearM = -1;              // save distance behind train (when moving backward)
        public SignalObject RearSignalObject = null;     // direct reference to signal at rear (when moving backward)
        public bool tilted = false;

        public enum END_AUTHORITY
        {
            END_OF_TRACK,
            END_OF_PATH,
            RESERVED_SWITCH,
            TRAIN_AHEAD,
            MAX_DISTANCE,
            LOOP,
            SIGNAL,                                       // in Manual mode only
            END_OF_AUTHORITY,                             // when moving backward in Auto mode
            NO_PATH_RESERVED
        }

        public END_AUTHORITY[] EndAuthorityType = new END_AUTHORITY[2] { END_AUTHORITY.NO_PATH_RESERVED, END_AUTHORITY.NO_PATH_RESERVED };

        public int[] LastReservedSection = new int[2] { -1, -1 };         // index of furthest cleared section (for NODE control)
        public float[] DistanceToEndNodeAuthorityM = new float[2];      // distance to end of authority
        public int LoopSection = -1;                                    // section where route loops back onto itself

        // Deadlock Info : 
        // list of sections where deadlock begins
        // per section : list with trainno and end section
        public Dictionary<int, List<Dictionary<int, int>>> DeadlockInfo =
            new Dictionary<int, List<Dictionary<int, int>>>();


        public bool CheckTrain = false;                  // debug print required

        protected Simulator Simulator;                   // reference to the simulator


        // For AI control of the train
        public float AITrainBrakePercent
        {
            get
            {
                return aiBrakePercent;
            }
            set
            {
                aiBrakePercent = value;
                foreach (TrainCar car in Cars)
                    car.BrakeSystem.AISetPercent(aiBrakePercent);
            }
        }
        private float aiBrakePercent = 0;
        public float AITrainThrottlePercent
        {
            get
            {
                return MUThrottlePercent;
            }
            set
            {
                MUThrottlePercent = value;
            }
        }
        public bool AITrainDirectionForward
        {
            get
            {
                return MUDirection == Direction.Forward;
            }
            set
            {
                MUDirection = value ? Direction.Forward : Direction.Reverse;
                MUReverserPercent = value ? 100 : -100;
            }
        }
        public TrainCar LeadLocomotive
        {
            get
            {
                return LeadLocomotiveIndex >= 0 ? Cars[LeadLocomotiveIndex] : null;
            }
            set
            {
                LeadLocomotiveIndex = -1;
                for (int i = 0; i < Cars.Count; i++)
                    if (value == Cars[i] && value.IsDriveable)
                    {
                        LeadLocomotiveIndex = i;
                        //MSTSLocomotive lead = (MSTSLocomotive)Cars[LeadLocomotiveIndex];
                        //if (lead.EngineBrakeController != null)
                        //    lead.EngineBrakeController.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, 1000);
                    }
                if (LeadLocomotiveIndex < 0)
                    foreach (TrainCar car in Cars)
                        car.BrakeSystem.BrakeLine1PressurePSI = -1;
            }
        }

        //================================================================================================//
        //
        // Constructor
        //

        public Train(Simulator simulator)
        {
            Simulator = simulator;
            Number = TotalNumber;
            TotalNumber++;
            SignalObjectItems = new List<ObjectItemInfo>();
            signalRef = simulator.Signals;

            routedForward = new TrainRouted(this, 0);
            routedBackward = new TrainRouted(this, 1);
        }

        //================================================================================================//
        //
        // Constructor for Dummy entries used on restore
        // Signals is restored before Trains, links are restored by Simulator
        //

        public Train(int number)
        {
            Number = number;
        }

        //================================================================================================//
        //
        // Constructor for uncoupled trains
        // copy path info etc. from original train
        //

        public Train(Simulator simulator, Train orgTrain)
        {
            Simulator = simulator;
            Number = TotalNumber;
            TotalNumber++;
            SignalObjectItems = new List<ObjectItemInfo>();
            signalRef = simulator.Signals;

            if (orgTrain.TrafficService != null)
            {
                TrafficService = new Traffic_Service_Definition();
                TrafficService.Time = orgTrain.TrafficService.Time;
                TrafficService.TrafficDetails = new List<Traffic_Traffic_Item>();

                foreach (Traffic_Traffic_Item thisTrafficItem in orgTrain.TrafficService.TrafficDetails)
                {
                    TrafficService.TrafficDetails.Add(thisTrafficItem);
                }
            }

            if (orgTrain.TCRoute != null)
            {
                TCRoute = new TCRoutePath(orgTrain.TCRoute);
            }

            ValidRoute[0] = new TCSubpathRoute(orgTrain.ValidRoute[0]);
            ValidRoute[1] = new TCSubpathRoute(orgTrain.ValidRoute[1]);

            DistanceTravelledM = orgTrain.DistanceTravelledM;

            if (orgTrain.requiredActions.Count > 0)
            {
                requiredActions = orgTrain.requiredActions.Copy();
            }

            routedForward = new TrainRouted(this, 0);
            routedBackward = new TrainRouted(this, 1);

            ControlMode = orgTrain.ControlMode;

            AllowedMaxSpeedMpS = orgTrain.AllowedMaxSpeedMpS;
            allowedMaxSpeedLimitMpS = orgTrain.allowedMaxSpeedLimitMpS;
            allowedMaxSpeedSignalMpS = orgTrain.allowedMaxSpeedSignalMpS;

            if (orgTrain.StationStops != null)
            {
                foreach (StationStop thisStop in orgTrain.StationStops)
                {
                    StationStop newStop = new StationStop(thisStop);
                    StationStops.Add(newStop);
                }
            }
            else
            {
                StationStops = null;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Restore
        /// <\summary>

        public Train(Simulator simulator, BinaryReader inf)
        {
            routedForward = new TrainRouted(this, 0);
            routedBackward = new TrainRouted(this, 1);

            Simulator = simulator;
            RestoreCars(simulator, inf);
            CheckFreight();
            Number = inf.ReadInt32();
            TotalNumber = Math.Max(Number + 1, TotalNumber);
            SpeedMpS = inf.ReadSingle();
            TrainType = (TRAINTYPE)inf.ReadInt32();
            MUDirection = (Direction)inf.ReadInt32();
            MUThrottlePercent = inf.ReadSingle();
            MUDynamicBrakePercent = inf.ReadSingle();
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            BrakeLine4PressurePSI = inf.ReadSingle();
            aiBrakePercent = inf.ReadSingle();
            LeadLocomotiveIndex = inf.ReadInt32();
            RetainerSetting = (RetainerSetting)inf.ReadInt32();
            RetainerPercent = inf.ReadInt32();
            RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, inf);
            SlipperySpotDistanceM = inf.ReadSingle();
            SlipperySpotLengthM = inf.ReadSingle();
            TrainMaxSpeedMpS = inf.ReadSingle();
            AllowedMaxSpeedMpS = inf.ReadSingle();
            allowedMaxSpeedSignalMpS = inf.ReadSingle();
            allowedMaxSpeedLimitMpS = inf.ReadSingle();

            SignalObjectItems = new List<ObjectItemInfo>();
            signalRef = simulator.Signals;

            TrainType = (TRAINTYPE)inf.ReadInt32();
            tilted = inf.ReadBoolean();
            ClaimState = inf.ReadBoolean();

            TCRoute = null;
            bool routeAvailable = inf.ReadBoolean();
            if (routeAvailable)
            {
                TCRoute = new TCRoutePath(inf);
            }

            ValidRoute[0] = null;
            bool validRouteAvailable = inf.ReadBoolean();
            if (validRouteAvailable)
            {
                ValidRoute[0] = new TCSubpathRoute(inf);
            }

            ValidRoute[1] = null;
            validRouteAvailable = inf.ReadBoolean();
            if (validRouteAvailable)
            {
                ValidRoute[1] = new TCSubpathRoute(inf);
            }

            int totalOccTrack = inf.ReadInt32();
            for (int iTrack = 0; iTrack < totalOccTrack; iTrack++)
            {
                int sectionIndex = inf.ReadInt32();
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[sectionIndex];
                OccupiedTrack.Add(thisSection);
            }

            int totalHoldSignals = inf.ReadInt32();
            for (int iSignal = 0; iSignal < totalHoldSignals; iSignal++)
            {
                int thisHoldSignal = inf.ReadInt32();
                HoldingSignals.Add(thisHoldSignal);
            }

            int totalStations = inf.ReadInt32();
            for (int iStation = 0; iStation < totalStations; iStation++)
            {
                StationStop thisStation = new StationStop(inf, signalRef);
                StationStops.Add(thisStation);
            }

            int totalPassedSignals = inf.ReadInt32();
            for (int iPassedSignal = 0; iPassedSignal < totalPassedSignals; iPassedSignal++)
            {
                int passedSignalKey = inf.ReadInt32();
                float passedSignalValue = inf.ReadSingle();
                PassedSignalSpeeds.Add(passedSignalKey, passedSignalValue);
            }
            LastPassedSignal[0] = inf.ReadInt32();
            LastPassedSignal[1] = inf.ReadInt32();

            bool trafficServiceAvailable = inf.ReadBoolean();
            if (trafficServiceAvailable)
            {
                TrafficService = RestoreTrafficSDefinition(inf);
            }

            ControlMode = (TRAIN_CONTROL)inf.ReadInt32();
            OutOfControlReason = (OUTOFCONTROL)inf.ReadInt32();
            EndAuthorityType[0] = (END_AUTHORITY)inf.ReadInt32();
            EndAuthorityType[1] = (END_AUTHORITY)inf.ReadInt32();
            LastReservedSection[0] = inf.ReadInt32();
            LastReservedSection[1] = inf.ReadInt32();
            LoopSection = inf.ReadInt32();
            DistanceToEndNodeAuthorityM[0] = inf.ReadSingle();
            DistanceToEndNodeAuthorityM[1] = inf.ReadSingle();

            if (TrainType != TRAINTYPE.AI_NOTSTARTED)
            {
                CalculatePositionOfCars(0);

                DistanceTravelledM = inf.ReadSingle();
                PresentPosition[0] = new TCPosition();
                PresentPosition[0].RestorePresentPosition(inf, this);
                PresentPosition[1] = new TCPosition();
                PresentPosition[1].RestorePresentRear(inf, this);
                PreviousPosition[0] = new TCPosition();
                PreviousPosition[0].RestorePreviousPosition(inf);

                PresentPosition[0].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
                PresentPosition[1].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
            }
            else
            {
                DistanceTravelledM = inf.ReadSingle();
                PresentPosition[0] = new TCPosition();
                PresentPosition[0].RestorePresentPositionDummy(inf, this);
                PresentPosition[1] = new TCPosition();
                PresentPosition[1].RestorePresentRearDummy(inf, this);
                PreviousPosition[0] = new TCPosition();
                PreviousPosition[0].RestorePreviousPositionDummy(inf);
            }

            int activeActions = inf.ReadInt32();
            for (int iAction = 0; iAction < activeActions; iAction++)
            {
                int actionType = inf.ReadInt32();

                switch (actionType)
                {
                    case 1:
                        ActivateSpeedLimit speedLimit = new ActivateSpeedLimit(inf);
                        requiredActions.InsertAction(speedLimit);
                        break;
                    case 2:
                        ClearSectionItem clearSection = new ClearSectionItem(inf);
                        requiredActions.InsertAction(clearSection);
                        break;
                    case 3:
                        AIActionItem actionItem = new AIActionItem(inf, signalRef);
                        requiredActions.InsertAction(actionItem);
                        break;
                    default:
                        Trace.TraceWarning("Unknown type of DistanceTravelledItem (type {0}",
                                actionType.ToString());
                        break;
                }
            }

            RestoreDeadlockInfo(inf);

            // restore signal references depending on state
            if (TrainType != TRAINTYPE.AI_NOTSTARTED)
            {
                if (ControlMode == TRAIN_CONTROL.EXPLORER)
                {
                    RestoreExplorerMode();
                }
                else if (ControlMode == TRAIN_CONTROL.MANUAL)
                {
                    RestoreManualMode();
                }
                else
                {
                    InitializeSignals(true);
                }
            }

            // restore leadlocomotive
            if (LeadLocomotiveIndex >= 0)
            {
                LeadLocomotive = Cars[LeadLocomotiveIndex];
                Program.Simulator.PlayerLocomotive = LeadLocomotive;
            }
        }

        private void RestoreCars(Simulator simulator, BinaryReader inf)
        {
            int count = inf.ReadInt32();
            for (int i = 0; i < count; ++i)
                Cars.Add(RollingStock.Restore(simulator, inf, this));
        }

        private Traffic_Service_Definition RestoreTrafficSDefinition(BinaryReader inf)
        {
            Traffic_Service_Definition thisDefinition = new Traffic_Service_Definition();
            thisDefinition.Time = inf.ReadInt32();
            thisDefinition.TrafficDetails = new List<Traffic_Traffic_Item>();

            int totalTrafficItems = inf.ReadInt32();

            for (int iTraffic = 0; iTraffic < totalTrafficItems; iTraffic++)
            {
                Traffic_Traffic_Item thisItem = RestoreTrafficItem(inf);
                thisDefinition.TrafficDetails.Add(thisItem);
            }

            return (thisDefinition);
        }

        private Traffic_Traffic_Item RestoreTrafficItem(BinaryReader inf)
        {
            Traffic_Traffic_Item thisTraffic = new Traffic_Traffic_Item();
            thisTraffic.ArrivalTime = inf.ReadInt32();
            thisTraffic.DepartTime = inf.ReadInt32();
            thisTraffic.DistanceDownPath = inf.ReadSingle();
            thisTraffic.PlatformStartID = inf.ReadInt32();

            return (thisTraffic);
        }

        private void RestoreDeadlockInfo(BinaryReader inf)
        {
            int totalDeadlock = inf.ReadInt32();
            for (int iDeadlockList = 0; iDeadlockList < totalDeadlock; iDeadlockList++)
            {
                int deadlockListKey = inf.ReadInt32();
                int deadlockListLength = inf.ReadInt32();

                List<Dictionary<int, int>> thisDeadlockList = new List<Dictionary<int, int>>();

                for (int iDeadlock = 0; iDeadlock < deadlockListLength; iDeadlock++)
                {
                    int deadlockInfoLength = inf.ReadInt32();
                    Dictionary<int, int> thisDeadlockDetails = new Dictionary<int, int>();

                    for (int iDeadlockDetails = 0; iDeadlockDetails < deadlockInfoLength; iDeadlockDetails++)
                    {
                        int deadlockKey = inf.ReadInt32();
                        int deadlockValue = inf.ReadInt32();

                        thisDeadlockDetails.Add(deadlockKey, deadlockValue);
                    }

                    thisDeadlockList.Add(thisDeadlockDetails);
                }
                DeadlockInfo.Add(deadlockListKey, thisDeadlockList);
            }
        }


        //================================================================================================//
        /// <summary>
        /// save game state
        /// <\summary>

        public virtual void Save(BinaryWriter outf)
        {
            SaveCars(outf);
            outf.Write(Number);
            outf.Write(SpeedMpS);
            outf.Write((int)TrainType);
            outf.Write((int)MUDirection);
            outf.Write(MUThrottlePercent);
            outf.Write(MUDynamicBrakePercent);
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(BrakeLine4PressurePSI);
            outf.Write(aiBrakePercent);
            outf.Write(LeadLocomotiveIndex);
            outf.Write((int)RetainerSetting);
            outf.Write(RetainerPercent);
            RearTDBTraveller.Save(outf);
            outf.Write(SlipperySpotDistanceM);
            outf.Write(SlipperySpotLengthM);
            outf.Write(TrainMaxSpeedMpS);
            outf.Write(AllowedMaxSpeedMpS);
            outf.Write(allowedMaxSpeedSignalMpS);
            outf.Write(allowedMaxSpeedLimitMpS);

            outf.Write((int)TrainType);
            outf.Write(tilted);
            outf.Write(ClaimState);

            if (TCRoute == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                TCRoute.Save(outf);
            }

            if (ValidRoute[0] == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                ValidRoute[0].Save(outf);
            }

            if (ValidRoute[1] == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                ValidRoute[1].Save(outf);
            }

            outf.Write(OccupiedTrack.Count);
            foreach (TrackCircuitSection thisSection in OccupiedTrack)
            {
                outf.Write(thisSection.Index);
            }

            outf.Write(HoldingSignals.Count);
            foreach (int thisHold in HoldingSignals)
            {
                outf.Write(thisHold);
            }

            outf.Write(StationStops.Count);
            foreach (StationStop thisStop in StationStops)
            {
                thisStop.Save(outf);
            }

            outf.Write(PassedSignalSpeeds.Count);
            foreach (KeyValuePair<int, float> thisPair in PassedSignalSpeeds)
            {
                outf.Write(thisPair.Key);
                outf.Write(thisPair.Value);
            }
            outf.Write(LastPassedSignal[0]);
            outf.Write(LastPassedSignal[1]);

            if (TrafficService == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                SaveTrafficSDefinition(outf, TrafficService);
            }

            outf.Write((int)ControlMode);
            outf.Write((int)OutOfControlReason);
            outf.Write((int)EndAuthorityType[0]);
            outf.Write((int)EndAuthorityType[1]);
            outf.Write(LastReservedSection[0]);
            outf.Write(LastReservedSection[1]);
            outf.Write(LoopSection);
            outf.Write(DistanceToEndNodeAuthorityM[0]);
            outf.Write(DistanceToEndNodeAuthorityM[1]);

            outf.Write(DistanceTravelledM);
            PresentPosition[0].Save(outf);
            PresentPosition[1].Save(outf);
            PreviousPosition[0].Save(outf);

            outf.Write(requiredActions.Count);
            foreach (DistanceTravelledItem thisAction in requiredActions)
            {
                thisAction.Save(outf);
            }

            SaveDeadlockInfo(outf);
        }

        private void SaveCars(BinaryWriter outf)
        {
            outf.Write(Cars.Count);
            foreach (TrainCar car in Cars)
                RollingStock.Save(outf, car);
        }

        private void SaveTrafficSDefinition(BinaryWriter outf, Traffic_Service_Definition thisTSD)
        {
            outf.Write(thisTSD.Time);
            outf.Write(thisTSD.TrafficDetails.Count);
            foreach (Traffic_Traffic_Item thisTI in thisTSD.TrafficDetails)
            {
                SaveTrafficItem(outf, thisTI);
            }
        }

        private void SaveTrafficItem(BinaryWriter outf, Traffic_Traffic_Item thisTI)
        {
            outf.Write(thisTI.ArrivalTime);
            outf.Write(thisTI.DepartTime);
            outf.Write(thisTI.DistanceDownPath);
            outf.Write(thisTI.PlatformStartID);
        }

        private void SaveDeadlockInfo(BinaryWriter outf)
        {
            outf.Write(DeadlockInfo.Count);
            foreach (KeyValuePair<int, List<Dictionary<int, int>>> thisInfo in DeadlockInfo)
            {
                outf.Write(thisInfo.Key);
                outf.Write(thisInfo.Value.Count);

                foreach (Dictionary<int, int> thisDeadlock in thisInfo.Value)
                {
                    outf.Write(thisDeadlock.Count);
                    foreach (KeyValuePair<int, int> thisDeadlockDetails in thisDeadlock)
                    {
                        outf.Write(thisDeadlockDetails.Key);
                        outf.Write(thisDeadlockDetails.Value);
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Changes the Lead locomotive (i.e. the loco which the player controls) to the next in the consist.
        /// Steps back through the train, ignoring any cabs that face rearwards until there are no forward-facing
        /// cabs left. Then continues from the rearmost, rearward-facing cab, reverses the train and resumes stepping back.
        /// E.g. if consist is made of 3 cars, each with front and rear-facing cabs
        ///     (A-b]:(C-d]:[e-F)
        /// then pressing Ctrl+E cycles the cabs in the sequence
        ///     A -> b -> C -> d -> e -> F
        /// </summary>
        public TrainCar GetNextCab()
        {
            // negative numbers used if rear cab selected
            // because '0' has no negative, all indices are shifted by 1!!!!

            int presentIndex = LeadLocomotiveIndex+1;
            if (((MSTSLocomotive)LeadLocomotive).UsingRearCab) presentIndex = -presentIndex;

            List<int> cabList = new List<int>();

            for (int i = 0; i < Cars.Count; i++)
            {
                if (SkipOtherUsersCar(i)) continue;
                if (Cars[i].Flipped)
                {
                    if (Cars[i].HasRearCab) cabList.Add(-(i+1));
                    if (Cars[i].HasFrontCab) cabList.Add(i+1);
                }
                else
                {
                    if (Cars[i].HasFrontCab) cabList.Add(i+1);
                    if (Cars[i].HasRearCab) cabList.Add(-(i+1));
                }
            }

            int lastIndex = cabList.IndexOf(presentIndex);
            if (lastIndex >= cabList.Count - 1) lastIndex = -1;

            int nextCabIndex = cabList[lastIndex + 1];

            TrainCar oldLead = LeadLocomotive;
            LeadLocomotiveIndex = Math.Abs(nextCabIndex)-1;
            Trace.Assert(LeadLocomotive != null, "Tried to switch to non-existent loco");
            TrainCar newLead = LeadLocomotive;  // Changing LeadLocomotiveIndex also changed LeadLocomotive
            ((MSTSLocomotive)newLead).UsingRearCab = nextCabIndex < 0;

            if (oldLead != null && newLead != null && oldLead != newLead)
            {
                newLead.CopyControllerSettings(oldLead);
                // TODO :: need to link HeadOut cameras to new lead locomotive
                // following should do it but cannot be used due to protection level
                // Simulator.Confirmer.Viewer.HeadOutBackCamera.SetCameraCar(Cars[LeadLocomotiveIndex]);
                // seems there is nothing to attach camera to car
            }

            if (Simulator.PlayerLocomotive != null && Simulator.PlayerLocomotive.Train == this)
            {
                Simulator.PlayerLocomotive = newLead;
            }
            return Simulator.PlayerLocomotive;
        }

        //this function is needed for Multiplayer games as they do not need to have cabs, but need to know lead locomotives
        // Sets the Lead locomotive to the next in the consist
        public void LeadNextLocomotive()
        {
            // First driveable
            int firstLead = -1;
            // Next driveale to the current
            int nextLead = -1;
            // Count of driveable locos
            int coud = 0;

            for (int i = 0; i < Cars.Count; i++)
            {
                if (Cars[i].IsDriveable)
                {
                    // Count the driveables
                    coud++;

                    // Get the first driveable
                    if (firstLead == -1)
                        firstLead = i;

                    // If later than current select the next
                    if (LeadLocomotiveIndex < i && nextLead == -1)
                    {
                        nextLead = i;
                    }
                }
            }

            TrainCar prevLead = LeadLocomotive;

            // If found one after the current
            if (nextLead != -1)
                LeadLocomotiveIndex = nextLead;
            // If not, and have more than one, set the first
            else if (coud > 1)
                LeadLocomotiveIndex = firstLead;
            TrainCar newLead = LeadLocomotive;
            if (prevLead != null && newLead != null && prevLead != newLead)
                newLead.CopyControllerSettings(prevLead);
        }

        //================================================================================================//
        /// <summary>
        /// Is there another cab in the player's train to change to?
        /// </summary>
        public bool IsChangeCabAvailable()
        {
            Trace.Assert(Simulator.PlayerLocomotive != null, "Player loco is null when trying to switch locos");
            Trace.Assert(Simulator.PlayerLocomotive.Train == this, "Trying to switch locos but not on player's train");

            int driveableCabs = 0;
            for (int i = 0; i < Cars.Count; i++)
            {
                if (SkipOtherUsersCar(i)) continue;
                if (Cars[i].HasFrontCab) driveableCabs++;
                if (Cars[i].HasRearCab) driveableCabs++;
            }
            if (driveableCabs < 2)
            {
                Simulator.Confirmer.Warning(CabControl.ChangeCab, CabSetting.Warn1);
                return false;
            }
            return true;
        }

        //================================================================================================//
        /// <summary>
        /// In multiplayer, only want to change to my locomotives; i.e. those that start with my name.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private bool SkipOtherUsersCar(int i)
        {
            return MPManager.IsMultiPlayer() && !Cars[i].CarID.StartsWith(MPManager.GetUserName() + " ");
        }

        //================================================================================================//
        /// <summary>
        /// Flips the train if necessary so that the train orientation matches the lead locomotive cab direction
        /// </summary>

        //       public void Orient()
        //       {
        //           TrainCar lead = LeadLocomotive;
        //           if (lead == null || !(lead.Flipped ^ lead.GetCabFlipped()))
        //               return;
        //
        //           if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL || ControlMode == TRAIN_CONTROL.AUTO_NODE || ControlMode == TRAIN_CONTROL.MANUAL)
        //               return;
        //
        //           for (int i = Cars.Count - 1; i > 0; i--)
        //               Cars[i].CopyCoupler(Cars[i - 1]);
        //           for (int i = 0; i < Cars.Count / 2; i++)
        //           {
        //               int j = Cars.Count - i - 1;
        //               TrainCar car = Cars[i];
        //               Cars[i] = Cars[j];
        //               Cars[j] = car;
        //           }
        //           if (LeadLocomotiveIndex >= 0)
        //               LeadLocomotiveIndex = Cars.Count - LeadLocomotiveIndex - 1;
        //           for (int i = 0; i < Cars.Count; i++)
        //               Cars[i].Flipped = !Cars[i].Flipped;
        //
        //           Traveller t = FrontTDBTraveller;
        //           FrontTDBTraveller = new Traveller(RearTDBTraveller, Traveller.TravellerDirection.Backward);
        //           RearTDBTraveller = new Traveller(t, Traveller.TravellerDirection.Backward);
        //
        //           MUDirection = DirectionControl.Flip(MUDirection);
        //           MUReverserPercent = -MUReverserPercent;
        //       }

        //================================================================================================//
        /// <summary>
        /// Reverse train formation
        /// Only performed when train activates a reversal point
        /// NOTE : this routine handles the physical train orientation only, all related route settings etc. must be handled separately
        /// </summary>

        public void ReverseFormation(bool setMUParameters)
        {
            // Shift all the coupler data along the train by 1 car.
            for (var i = Cars.Count - 1; i > 0; i--)
                Cars[i].CopyCoupler(Cars[i - 1]);
            // Reverse the actual order of the cars in the train.
            Cars.Reverse();
            // Update leading locomotive index.
            if (LeadLocomotiveIndex >= 0)
                LeadLocomotiveIndex = Cars.Count - LeadLocomotiveIndex - 1;
            // Update flipped state of each car.
            for (var i = 0; i < Cars.Count; i++)
                Cars[i].Flipped = !Cars[i].Flipped;
            // Flip the train's travellers.
            var t = FrontTDBTraveller;
            FrontTDBTraveller = new Traveller(RearTDBTraveller, Traveller.TravellerDirection.Backward);
            RearTDBTraveller = new Traveller(t, Traveller.TravellerDirection.Backward);
            // If we are updating the controls...
            if (setMUParameters)
            {
                // Flip the controls.
                MUDirection = DirectionControl.Flip(MUDirection);
                MUReverserPercent = -MUReverserPercent;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Someone is sending an event notification to all cars on this train.
        /// ie doors open, pantograph up, lights on etc.
        /// </summary>

        public void SignalEvent(Event evt)
        {
            foreach (var car in Cars)
                car.SignalEvent(evt);
        }


        //================================================================================================//
        /// <summary>
        /// Update train 
        /// <\summary>

        public virtual void Update(float elapsedClockSeconds)
        {
            // Update train physics, position and movement

            physicsUpdate(elapsedClockSeconds);

            //Exit here when train is static consist (no further actions required)

            if (TrainType == TRAINTYPE.STATIC)
                return;

            // perform overall update

            if (ControlMode == TRAIN_CONTROL.MANUAL)                                        // manual mode
            {
                UpdateManual(elapsedClockSeconds);
            }

            else if (ControlMode == TRAIN_CONTROL.EXPLORER)                                 // explorer mode
            {
                UpdateExplorer(elapsedClockSeconds);
            }

            else if (ValidRoute[0] != null)     // no actions required for static objects //
            {
                movedBackward = CheckBackwardClearance();                                       // check clearance at rear //
                UpdateTrainPosition();                                                          // position update         //
                UpdateTrainPositionInformation();                                               // position update         //
                int SignalObjIndex = CheckSignalPassed(0, PresentPosition[0], PreviousPosition[0]);   // check if passed signal  //
                UpdateSectionState(movedBackward);                                              // update track occupation //
                ObtainRequiredActions(movedBackward);                                           // process list of actions //
                if (TrainType != TRAINTYPE.AI && ControlMode != TRAIN_CONTROL.OUT_OF_CONTROL)
                {
                    CheckRouteActions(elapsedClockSeconds);                                     // check routepath (AI check at other point) //
                }
                UpdateRouteClearanceAhead(SignalObjIndex, movedBackward, elapsedClockSeconds);  // update route clearance  //
                UpdateSignalState(movedBackward);                                               // update signal state     //
            }
        } // end Update

        //================================================================================================//
        /// <summary>
        /// Update train physics
        /// <\summary>

        public virtual void physicsUpdate(float elapsedClockSeconds)
        {
            //if out of track, will set it to stop
            if ((FrontTDBTraveller != null && FrontTDBTraveller.IsEnd) || (RearTDBTraveller != null && RearTDBTraveller.IsEnd))
            {
                if (FrontTDBTraveller.IsEnd && RearTDBTraveller.IsEnd)
                {//if both travellers are out, very rare occation, but have to treat it
                    RearTDBTraveller.ReverseDirection();
                    RearTDBTraveller.NextTrackNode();
                } 
                else if (FrontTDBTraveller.IsEnd) RearTDBTraveller.Move(-1);//if front is out, move back
                else if (RearTDBTraveller.IsEnd) RearTDBTraveller.Move(1);//if rear is out, move forward
                foreach (var car in Cars) { car.SpeedMpS = 0; } //can set crash here by setting XNA matrix
                SignalEvent(Event._ResetWheelSlip);//reset everything to 0 power
            }

            if (this.TrainType == TRAINTYPE.REMOTE || updateMSGReceived == true) //server tolds me this train (may include mine) needs to update position
            {
                UpdateRemoteTrainPos(elapsedClockSeconds);
                return;
            }
            // Update train physics, position and movement

            PropagateBrakePressure(elapsedClockSeconds);

            foreach (TrainCar car in Cars)
            {
                car.MotiveForceN = 0;
                car.Update(elapsedClockSeconds);
                car.TotalForceN = car.MotiveForceN + car.GravityForceN;

                if (car.Flipped)
                {
                    car.TotalForceN = -car.TotalForceN;
                    car.SpeedMpS = -car.SpeedMpS;
                }
            }

            AddCouplerImpuseForces();
            ComputeCouplerForces();
            UpdateCarSpeeds(elapsedClockSeconds);
            UpdateCouplerSlack(elapsedClockSeconds);

            float distanceM = LastCar.SpeedMpS * elapsedClockSeconds;
            if (float.IsNaN(distanceM)) distanceM = 0;//avoid NaN, if so will not move
            if (TrainType == TRAINTYPE.AI && LeadLocomotiveIndex == (Cars.Count - 1) && LastCar.Flipped)
                distanceM = -distanceM;
            DistanceTravelledM += distanceM;

            SpeedMpS = 0;
            foreach (TrainCar car1 in Cars)
            {
                SpeedMpS += car1.SpeedMpS;
                if (car1.Flipped)
                    car1.SpeedMpS = -car1.SpeedMpS;
            }
            SpeedMpS /= Cars.Count;

            SlipperySpotDistanceM -= SpeedMpS * elapsedClockSeconds;

            CalculatePositionOfCars(distanceM);

            // calculate projected speed
            if (elapsedClockSeconds < AccelerationMpSpS.SmoothPeriodS)
                AccelerationMpSpS.Update(elapsedClockSeconds, (SpeedMpS - LastSpeedMpS) / elapsedClockSeconds);
            LastSpeedMpS = SpeedMpS;
            ProjectedSpeedMpS = SpeedMpS + 60 * AccelerationMpSpS.SmoothedValue;
            ProjectedSpeedMpS = SpeedMpS > float.Epsilon ?
                Math.Max(0, ProjectedSpeedMpS) : SpeedMpS < -float.Epsilon ? Math.Min(0, ProjectedSpeedMpS) : 0;
        }

        //================================================================================================//
        /// <summary>
        /// Update in manual mode
        /// <\summary>

        public void UpdateManual(float elapsedClockSeconds)
        {
            UpdateTrainPosition();                                                                // position update                  //
            int SignalObjIndex = CheckSignalPassed(0, PresentPosition[0], PreviousPosition[0]);   // check if passed signal forward   //
            if (SignalObjIndex < 0)
            {
                SignalObjIndex = CheckSignalPassed(1, PresentPosition[1], PreviousPosition[1]);   // check if passed signal backward  //
            }
            UpdateSectionStateManual();                                                           // update track occupation          //
            UpdateManualMode(SignalObjIndex);                                                     // update route clearance           //
            // for manual, also includes signal update //
        }

        //================================================================================================//
        /// <summary>
        /// Update in manual mode
        /// <\summary>

        public void UpdateExplorer(float elapsedClockSeconds)
        {
            UpdateTrainPosition();                                                                // position update                  //
            int SignalObjIndex = CheckSignalPassed(0, PresentPosition[0], PreviousPosition[0]);   // check if passed signal forward   //
            if (SignalObjIndex < 0)
            {
                SignalObjIndex = CheckSignalPassed(1, PresentPosition[1], PreviousPosition[1]);   // check if passed signal backward  //
            }
            UpdateSectionStateExplorer();                                                         // update track occupation          //
            UpdateExplorerMode(SignalObjIndex);                                                   // update route clearance           //
            // for manual, also includes signal update //
        }

        //================================================================================================//
        /// <summary>
        /// Post Init : perform all actions required to start
        /// </summary>

        public virtual bool PostInit()
        {

            // if train has no valid route, build route over trainlength (from back to front)

            bool validPosition = InitialTrainPlacement();

            if (validPosition)
            {
                InitializeSignals(false);     // Get signal information - only if train has route //
                if (TrainType != TRAINTYPE.STATIC)
                    CheckDeadlock(ValidRoute[0], Number);    // Check deadlock against all other trains (not for static trains)
                if (TCRoute != null) TCRoute.SetReversalOffset(Length);
            }
            return (validPosition);
        }

        //================================================================================================//
        /// <summary>
        /// get aspect of next signal ahead
        /// </summary>

        public SignalHead.SIGASP GetNextSignalAspect(int direction)
        {
            SignalHead.SIGASP thisAspect = SignalHead.SIGASP.STOP;
            if (NextSignalObject[direction] != null)
            {
                thisAspect = NextSignalObject[direction].this_sig_lr(SignalHead.SIGFN.NORMAL);
            }

            return thisAspect;
        }

        //================================================================================================//
        /// <summary>
        /// initialize signal array
        /// </summary>

        public void InitializeSignals(bool existingSpeedLimits)
        {
            Debug.Assert(signalRef != null, "Cannot InitializeSignals() without Simulator.Signals.");

            // to initialize, use direction 0 only
            // preset indices

            IndexNextSignal = -1;
            IndexNextSpeedlimit = -1;

            //  set overall speed limits if these do not yet exist

            if (!existingSpeedLimits)
            {
                AllowedMaxSpeedMpS = TrainMaxSpeedMpS;   // set default
                allowedMaxSpeedSignalMpS = TrainMaxSpeedMpS;   // set default

                //  try to find first speed limits behind the train

                List<int> speedpostList = signalRef.ScanRoute(null, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                                PresentPosition[1].TCDirection, false, -1, false, true, false, false, false, false, false, true, false, IsFreight);

                if (speedpostList.Count > 0)
                {
                    SignalObject thisSpeedpost = signalRef.SignalObjects[speedpostList[0]];
                    ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalHead.SIGFN.SPEED);

                    AllowedMaxSpeedMpS = Math.Min(AllowedMaxSpeedMpS, IsFreight ? speed_info.speed_freight : speed_info.speed_pass);
                }

                float validSpeedMpS = AllowedMaxSpeedMpS;

                //  try to find first speed limits along train - scan back to front

                bool noMoreSpeedposts = false;
                int thisSectionIndex = PresentPosition[1].TCSectionIndex;
                float thisSectionOffset = PresentPosition[1].TCOffset;
                int thisDirection = PresentPosition[1].TCDirection;
                float remLength = Length;

                while (!noMoreSpeedposts)
                {
                    speedpostList = signalRef.ScanRoute(null, thisSectionIndex, thisSectionOffset,
                            thisDirection, true, remLength, false, true, false, false, false, false, false, true, false, IsFreight);

                    if (speedpostList.Count > 0)
                    {
                        SignalObject thisSpeedpost = signalRef.SignalObjects[speedpostList[0]];
                        ObjectSpeedInfo speed_info = thisSpeedpost.this_lim_speed(SignalHead.SIGFN.SPEED);
                        float distanceFromFront = Length - thisSpeedpost.DistanceTo(RearTDBTraveller);
                        if (distanceFromFront >= 0)
                        {
                            float newSpeedMpS = IsFreight ? speed_info.speed_freight : speed_info.speed_pass;
                            if (newSpeedMpS <= validSpeedMpS)
                            {
                                validSpeedMpS = newSpeedMpS;
                                if (validSpeedMpS < AllowedMaxSpeedMpS)
                                {
                                    AllowedMaxSpeedMpS = validSpeedMpS;
                                }
                                requiredActions.UpdatePendingSpeedlimits(validSpeedMpS);  // update any older pending speed limits
                            }
                            else
                            {
                                validSpeedMpS = newSpeedMpS;
                                float reqDistance = DistanceTravelledM + Length - distanceFromFront;
                                ActivateSpeedLimit speedLimit = new ActivateSpeedLimit(reqDistance, newSpeedMpS, -1f);
                                requiredActions.InsertAction(speedLimit);
                                requiredActions.UpdatePendingSpeedlimits(newSpeedMpS);  // update any older pending speed limits
                            }

                            thisSectionIndex = thisSpeedpost.TCReference;
                            thisSectionOffset = thisSpeedpost.TCOffset;
                            thisDirection = thisSpeedpost.TCDirection;
                            remLength = distanceFromFront;
                        }
                        else
                        {
                            noMoreSpeedposts = true;
                        }
                    }
                    else
                    {
                        noMoreSpeedposts = true;
                    }
                }

                allowedMaxSpeedLimitMpS = AllowedMaxSpeedMpS;   // set default
            }

            //  get first item from train (irrespective of distance)

            ObjectItemInfo.ObjectItemFindState returnState = ObjectItemInfo.ObjectItemFindState.NONE_FOUND;
            float distanceToLastObject = 9E29f;  // set to overlarge value
            SignalHead.SIGASP nextAspect = SignalHead.SIGASP.UNKNOWN;

            ObjectItemInfo firstObject = signalRef.GetNextObject_InRoute(routedForward, ValidRoute[0],
                PresentPosition[0].RouteListIndex, PresentPosition[0].TCOffset, -1,
                ObjectItemInfo.ObjectItemType.ANY);

            returnState = firstObject.ObjectState;
            if (returnState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
            {
                firstObject.distance_to_train = firstObject.distance_found;
                SignalObjectItems.Add(firstObject);
                if (firstObject.ObjectDetails.isSignal)
                {
                    nextAspect = firstObject.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
                }
                distanceToLastObject = firstObject.distance_found;
            }

            // get next items within max distance

            float maxDistance = Math.Max(AllowedMaxSpeedMpS * maxTimeS, minCheckDistanceM);

            // look maxTimeS or minCheckDistance ahead

            ObjectItemInfo nextObject;
            ObjectItemInfo prevObject = firstObject;

            int routeListIndex = PresentPosition[0].RouteListIndex;
            float offset = PresentPosition[0].TCOffset;
            int nextIndex = routeListIndex;

            while (returnState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND &&
                distanceToLastObject < maxDistance &&
                nextAspect != SignalHead.SIGASP.STOP)
            {
                int foundSection = -1;

                SignalObject thisSignal = prevObject.ObjectDetails;

                int reqTCReference = thisSignal.TCReference;
                float reqOffset = thisSignal.TCOffset + 0.0001f;   // make sure you find NEXT object ! //

                if (thisSignal.TCNextTC > 0)
                {
                    reqTCReference = thisSignal.TCNextTC;
                    reqOffset = 0.0f;
                }

                if (nextIndex < 0)
                    nextIndex = 0;
                for (int iNode = nextIndex; iNode < ValidRoute[0].Count && foundSection < 0 && reqTCReference > 0; iNode++)
                {
                    Train.TCRouteElement thisElement = ValidRoute[0][iNode];
                    if (thisElement.TCSectionIndex == reqTCReference)
                    {
                        foundSection = iNode;
                        nextIndex = iNode;
                        offset = reqOffset;
                    }
                }

                nextObject = signalRef.GetNextObject_InRoute(routedForward, ValidRoute[0],
                nextIndex, offset, -1, ObjectItemInfo.ObjectItemType.ANY);

                returnState = nextObject.ObjectState;

                if (returnState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
                {
                    if (nextObject.ObjectDetails.isSignal)
                    {
                        nextObject.signal_state = nextObject.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
                        nextAspect = nextObject.signal_state;

                    }

                    nextObject.distance_to_object = nextObject.distance_found;
                    nextObject.distance_to_train = prevObject.distance_to_train + nextObject.distance_to_object;
                    distanceToLastObject = nextObject.distance_to_train;
                    SignalObjectItems.Add(nextObject);
                    prevObject = nextObject;
                }
            }

            //
            // get first signal and first speedlimit
            // also initiate nextSignal variable
            //

            bool signalFound = false;
            bool speedlimFound = false;

            for (int isig = 0; isig < SignalObjectItems.Count && (!signalFound || !speedlimFound); isig++)
            {
                if (!signalFound)
                {
                    ObjectItemInfo thisObject = SignalObjectItems[isig];
                    if (thisObject.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                    {
                        signalFound = true;
                        IndexNextSignal = isig;
                    }
                }

                if (!speedlimFound)
                {
                    ObjectItemInfo thisObject = SignalObjectItems[isig];
                    if (thisObject.ObjectType == ObjectItemInfo.ObjectItemType.SPEEDLIMIT)
                    {
                        speedlimFound = true;
                        IndexNextSpeedlimit = isig;
                    }
                }
            }

            //
            // If signal in list, set signal reference,
            // else try to get first signal if in signal mode
            //

            NextSignalObject[0] = null;
            if (IndexNextSignal >= 0)
            {
                NextSignalObject[0] = SignalObjectItems[IndexNextSignal].ObjectDetails;
                distanceToSignal = SignalObjectItems[IndexNextSignal].distance_to_train;
            }
            else
            {
                ObjectItemInfo firstSignalObject = signalRef.GetNextObject_InRoute(routedForward, ValidRoute[0],
                    PresentPosition[0].RouteListIndex, PresentPosition[0].TCOffset, -1,
                    ObjectItemInfo.ObjectItemType.SIGNAL);

                if (firstSignalObject.ObjectState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
                {
                    NextSignalObject[0] = firstSignalObject.ObjectDetails;
                    firstSignalObject.distance_to_train = firstSignalObject.distance_found;
                    distanceToSignal = firstSignalObject.distance_found;
                }
            }

            //
            // determine actual speed limits depending on overall speed and type of train
            //

            updateSpeedInfo();
        }

        //================================================================================================//
        /// <summary>
        ///  Update the distance to and aspect of next signal
        /// </summary>

        public void UpdateSignalState(int backward)
        {
            // for AUTO mode, use direction 0 only

            ObjectItemInfo.ObjectItemFindState returnState = ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND;

            bool listChanged = false;
            bool signalFound = false;
            bool speedlimFound = false;

            ObjectItemInfo firstObject = null;

            //
            // get distance to first object
            //

            if (SignalObjectItems.Count > 0)
            {
                firstObject = SignalObjectItems[0];
                firstObject.distance_to_train = GetObjectDistanceToTrain(firstObject);


                //
                // check if passed object - if so, remove object
                // if object is speed, set max allowed speed as distance travelled action
                //

                while (firstObject.distance_to_train < 0.0f && SignalObjectItems.Count > 0)
                {
#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Passed Signal : " + firstObject.ObjectDetails.thisRef.ToString() +
                            " with speed : " + firstObject.actual_speed.ToString() + "\n");
#endif
                    if (firstObject.actual_speed > 0)
                    {
#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Passed speedpost : " + firstObject.ObjectDetails.thisRef.ToString() +
                            " = " + firstObject.actual_speed.ToString()+"\n");

                        File.AppendAllText(@"C:\temp\printproc.txt", "Present Limits : " +
                            "Limit : "+allowedMaxSpeedLimitMpS.ToString() + " ; " +
                            "Signal : "+allowedMaxSpeedSignalMpS.ToString() + " ; " +
                            "Overall : "+AllowedMaxSpeedMpS.ToString() + "\n");
#endif
                        if (firstObject.actual_speed <= AllowedMaxSpeedMpS)
                        {
                            AllowedMaxSpeedMpS = firstObject.actual_speed;
                            if (firstObject.ObjectDetails.isSignal)
                            {
                                allowedMaxSpeedSignalMpS = AllowedMaxSpeedMpS;
                            }
                            else
                            {
                                allowedMaxSpeedLimitMpS = AllowedMaxSpeedMpS;
                            }
                            requiredActions.UpdatePendingSpeedlimits(AllowedMaxSpeedMpS);  // update any older pending speed limits
                        }
                        else
                        {
                            ActivateSpeedLimit speedLimit;
                            float reqDistance = DistanceTravelledM + Length;
                            if (firstObject.ObjectDetails.isSignal)
                            {
                                speedLimit = new ActivateSpeedLimit(reqDistance, -1f, firstObject.actual_speed);
                            }
                            else
                            {
                                speedLimit = new ActivateSpeedLimit(reqDistance, firstObject.actual_speed, -1f);
                            }
                            requiredActions.InsertAction(speedLimit);
                            requiredActions.UpdatePendingSpeedlimits(firstObject.actual_speed);  // update any older pending speed limits
                        }
                    }

                    if (NextSignalObject[0] != null && firstObject.ObjectDetails == NextSignalObject[0])
                    {
                        NextSignalObject[0] = null;
                    }

                    SignalObjectItems.RemoveAt(0);
                    listChanged = true;

                    if (SignalObjectItems.Count > 0)
                    {
                        firstObject = SignalObjectItems[0];
                        firstObject.distance_to_train = GetObjectDistanceToTrain(firstObject);
                    }
                }

                //
                // if moving backward, check signals have been passed
                //

                if (backward > backwardThreshold)
                {

                    int newSignalIndex = -1;
                    bool noMoreNewSignals = false;

                    int thisIndex = PresentPosition[0].RouteListIndex;
                    float offset = PresentPosition[0].TCOffset;

                    while (!noMoreNewSignals)
                    {
                        ObjectItemInfo newObjectItem = signalRef.GetNextObject_InRoute(routedForward, ValidRoute[0],
                           thisIndex, offset, -1, ObjectItemInfo.ObjectItemType.SIGNAL);

                        returnState = newObjectItem.ObjectState;
                        if (returnState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
                        {
                            newSignalIndex = newObjectItem.ObjectDetails.thisRef;

                            noMoreNewSignals = (NextSignalObject[0] == null || (NextSignalObject[0] != null && newSignalIndex == NextSignalObject[0].thisRef));

                            if (!noMoreNewSignals)
                            {
                                if (SignalObjectItems.Count > 0)  // reset distance to train to distance to object //
                                {
                                    firstObject = SignalObjectItems[0];
                                    firstObject.distance_to_object =
                                        firstObject.distance_to_train - newObjectItem.distance_to_train;
                                }

                                SignalObjectItems.Insert(0, newObjectItem);
                                listChanged = true;

                                int foundIndex = ValidRoute[0].GetRouteIndex(newObjectItem.ObjectDetails.TCNextTC, thisIndex);

                                if (foundIndex > 0)
                                {
                                    thisIndex = foundIndex;
                                    offset = 0.0f;
                                }
                            }
                        }
                        else
                        {
                            noMoreNewSignals = true;
                        }
                    }
                }
            }

            //
            // if no objects left on list, find first object whatever the distance
            //

            if (SignalObjectItems.Count <= 0)
            {
                firstObject = signalRef.GetNextObject_InRoute(routedForward, ValidRoute[0],
                      PresentPosition[0].RouteListIndex, PresentPosition[0].TCOffset, -1,
                      ObjectItemInfo.ObjectItemType.ANY);

                returnState = firstObject.ObjectState;
                if (returnState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
                {
                    firstObject.distance_to_train = firstObject.distance_found;
                    SignalObjectItems.Add(firstObject);
                }
            }

            // reset next signal object if none found

            if (SignalObjectItems.Count <= 0 || (SignalObjectItems.Count == 1 && SignalObjectItems[0].ObjectType == ObjectItemInfo.ObjectItemType.SPEEDLIMIT))
            {
                NextSignalObject[0] = null;
                distanceToSignal = 0;
            }

            //
            // process further if any object available
            //

            if (SignalObjectItems.Count > 0)
            {

                //
                // Update state and speed of first object if signal
                //

                if (firstObject.ObjectDetails.isSignal)
                {
                    firstObject.signal_state = firstObject.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
                    ObjectSpeedInfo thisSpeed = firstObject.ObjectDetails.this_sig_speed(SignalHead.SIGFN.NORMAL);
                    firstObject.speed_passenger = thisSpeed == null ? -1 : thisSpeed.speed_pass;
                    firstObject.speed_freight = thisSpeed == null ? -1 : thisSpeed.speed_freight;
                    firstObject.speed_flag = thisSpeed == null ? 0 : thisSpeed.speed_flag;
                }

                //
                // Update all objects in list (except first)
                //

                float lastDistance = firstObject.distance_to_train;

                ObjectItemInfo prevObject = firstObject;

                for (int isig = 1; isig < SignalObjectItems.Count && !signalFound; isig++)
                {
                    ObjectItemInfo nextObject = SignalObjectItems[isig];
                    nextObject.distance_to_train = prevObject.distance_to_train + nextObject.distance_to_object;
                    lastDistance = nextObject.distance_to_train;

                    if (nextObject.ObjectDetails.isSignal)
                    {
                        nextObject.signal_state = nextObject.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
                        if (nextObject.ObjectDetails.enabledTrain != null && nextObject.ObjectDetails.enabledTrain.Train != this)
                            nextObject.signal_state = SignalHead.SIGASP.STOP; // state not valid if not enabled for this train
                        ObjectSpeedInfo thisSpeed = nextObject.ObjectDetails.this_sig_speed(SignalHead.SIGFN.NORMAL);
                        nextObject.speed_passenger = thisSpeed == null || nextObject.signal_state == SignalHead.SIGASP.STOP ? -1 : thisSpeed.speed_pass;
                        nextObject.speed_freight = thisSpeed == null || nextObject.signal_state == SignalHead.SIGASP.STOP ? -1 : thisSpeed.speed_freight;
                        nextObject.speed_flag = thisSpeed == null || nextObject.signal_state == SignalHead.SIGASP.STOP ? 0 : thisSpeed.speed_flag;
                    }

                    prevObject = nextObject;
                }

                //
                // check if last signal aspect is STOP, and if last signal is enabled for this train
                // If so, no check on list is required
                //

                SignalHead.SIGASP nextAspect = SignalHead.SIGASP.UNKNOWN;

                for (int isig = SignalObjectItems.Count - 1; isig >= 0 && !signalFound; isig--)
                {
                    ObjectItemInfo nextObject = SignalObjectItems[isig];
                    if (nextObject.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                    {
                        signalFound = true;
                        nextAspect = nextObject.signal_state;

                        SignalObject nextSignal = nextObject.ObjectDetails;
                    }
                }

                //
                // read next items if last item within max distance
                //

                float maxDistance = Math.Max(AllowedMaxSpeedMpS * maxTimeS, minCheckDistanceM);

                int routeListIndex = PresentPosition[0].RouteListIndex;
                int lastIndex = routeListIndex;
                float offset = PresentPosition[0].TCOffset;

                prevObject = SignalObjectItems[SignalObjectItems.Count - 1];  // last object

                while (lastDistance < maxDistance &&
                          returnState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND &&
                          nextAspect != SignalHead.SIGASP.STOP)
                {

                    SignalObject prevSignal = prevObject.ObjectDetails;
                    int reqTCReference = prevSignal.TCReference;
                    float reqOffset = prevSignal.TCOffset + 0.0001f;   // make sure you find NEXT object ! //

                    if (prevSignal.TCNextTC > 0)
                    {
                        reqTCReference = prevSignal.TCNextTC;
                        reqOffset = 0.0f;
                    }

                    int foundSection = ValidRoute[0].GetRouteIndex(reqTCReference, lastIndex);
                    if (foundSection >= 0)
                    {
                        lastIndex = foundSection;
                        offset = reqOffset;
                    }

                    ObjectItemInfo nextObject = signalRef.GetNextObject_InRoute(routedForward, ValidRoute[0],
                         lastIndex, offset, -1, ObjectItemInfo.ObjectItemType.ANY);

                    returnState = nextObject.ObjectState;

                    if (returnState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
                    {
                        nextObject.distance_to_object = nextObject.distance_found;
                        nextObject.distance_to_train = prevObject.distance_to_train + nextObject.distance_to_object;

                        lastDistance = nextObject.distance_to_train;
                        SignalObjectItems.Add(nextObject);

                        if (nextObject.ObjectDetails.isSignal)
                        {
                            nextObject.signal_state = nextObject.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
                            nextAspect = nextObject.signal_state;
                            ObjectSpeedInfo thisSpeed = nextObject.ObjectDetails.this_sig_speed(SignalHead.SIGFN.NORMAL);
                            nextObject.speed_passenger = thisSpeed == null ? -1 : thisSpeed.speed_pass;
                            nextObject.speed_freight = thisSpeed == null ? -1 : thisSpeed.speed_freight;
                            nextObject.speed_flag = thisSpeed == null ? 0 : thisSpeed.speed_flag;
                        }

                        prevObject = nextObject;
                        listChanged = true;
                    }
                }

                //
                // check if IndexNextSignal still valid, if not, force list changed
                //

                if (IndexNextSignal >= SignalObjectItems.Count)
                {
                    if (CheckTrain)
                        File.AppendAllText(@"F:\temp\checktrain.txt", "Error in UpdateSignalState: IndexNextSignal out of range : " + IndexNextSignal + 
                                             " (max value : " + SignalObjectItems.Count + ") \n");
                    listChanged = true;
                }

                //
                // if list is changed, get new indices to first signal and speedpost
                //

                if (listChanged)
                {
                    signalFound = false;
                    speedlimFound = false;

                    IndexNextSignal = -1;
                    IndexNextSpeedlimit = -1;
                    NextSignalObject[0] = null;

                    for (int isig = 0; isig < SignalObjectItems.Count && (!signalFound || !speedlimFound); isig++)
                    {
                        ObjectItemInfo nextObject = SignalObjectItems[isig];
                        if (!signalFound && nextObject.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                        {
                            signalFound = true;
                            IndexNextSignal = isig;
                        }
                        else if (!speedlimFound && nextObject.ObjectType == ObjectItemInfo.ObjectItemType.SPEEDLIMIT)
                        {
                            speedlimFound = true;
                            IndexNextSpeedlimit = isig;
                        }
                    }
                }

                //
                // check if any signal in list, if not get direct from train
                // get state and details
                //

                if (IndexNextSignal < 0)
                {
                    ObjectItemInfo firstSignalObject = signalRef.GetNextObject_InRoute(routedForward, ValidRoute[0],
                            PresentPosition[0].RouteListIndex, PresentPosition[0].TCOffset, -1,
                            ObjectItemInfo.ObjectItemType.SIGNAL);

                    if (firstSignalObject.ObjectState == ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND)
                    {
                        NextSignalObject[0] = firstSignalObject.ObjectDetails;
                        firstSignalObject.distance_to_train = firstSignalObject.distance_found;
                    }
                }
                else
                {
                    NextSignalObject[0] = SignalObjectItems[IndexNextSignal].ObjectDetails;
                }

                //
                // update distance of signal if out of list
                // get state of next signal
                //

                SignalHead.SIGASP thisState = SignalHead.SIGASP.UNKNOWN;

                if (IndexNextSignal >= 0)
                {
                    distanceToSignal = SignalObjectItems[IndexNextSignal].distance_to_train;
                    thisState = SignalObjectItems[IndexNextSignal].signal_state;

                    SignalObject thisSignal = SignalObjectItems[IndexNextSignal].ObjectDetails;
                }
                else if (NextSignalObject[0] != null)
                {
                    distanceToSignal = NextSignalObject[0].DistanceTo(FrontTDBTraveller);
                    thisState = NextSignalObject[0].this_sig_lr(SignalHead.SIGFN.NORMAL);
                }

                CABAspect = thisState;
                SignalObject dummyObject = new SignalObject();
                TMaspect = dummyObject.TranslateTMAspect(thisState);

                //
                // determine actual speed limits depending on overall speed and type of train
                //

                updateSpeedInfo();
            }

        }

        //================================================================================================//
        /// <summary>
        /// set actual speed limit for all objects depending on state and type of train
        /// </summary>

        public void updateSpeedInfo()
        {
            float validSpeedMpS = AllowedMaxSpeedMpS;
            float validSpeedSignalMpS = allowedMaxSpeedSignalMpS;
            float validSpeedLimitMpS = allowedMaxSpeedLimitMpS;

            foreach (ObjectItemInfo thisObject in SignalObjectItems)
            {

                //
                // select speed on type of train 
                //

                float actualSpeedMpS = IsFreight ? thisObject.speed_freight : thisObject.speed_passenger;

                if (thisObject.ObjectDetails.isSignal)
                {
                    if (actualSpeedMpS > 0 && thisObject.speed_flag == 0)
                    {
                        validSpeedSignalMpS = actualSpeedMpS;
                        if (validSpeedSignalMpS > validSpeedLimitMpS)
                        {
                            if (validSpeedMpS < validSpeedLimitMpS)
                            {
                                actualSpeedMpS = validSpeedLimitMpS;
                            }
                            else
                            {
                                actualSpeedMpS = -1;
                            }
#if DEBUG_REPORTS
                            File.AppendAllText(@"C:\temp\printproc.txt", "Speed reset : Signal : " + thisObject.ObjectDetails.thisRef.ToString() +
                                " : " + validSpeedSignalMpS.ToString() + " ; Limit : " + validSpeedLimitMpS.ToString() + "\n");
#endif
                        }
                    }
                    else
                    {
                        validSpeedSignalMpS = TrainMaxSpeedMpS;
                        float newSpeedMpS = Math.Min(validSpeedSignalMpS, validSpeedLimitMpS);

                        if (newSpeedMpS != validSpeedMpS)
                        {
                            actualSpeedMpS = newSpeedMpS;
                        }
                        else
                        {
                            actualSpeedMpS = -1;
                        }
                    }
                    thisObject.actual_speed = actualSpeedMpS;
                    if (actualSpeedMpS > 0)
                    {
                        validSpeedMpS = actualSpeedMpS;
                    }
                }
                else
                {
                    if (actualSpeedMpS > 998f)
                    {
                        actualSpeedMpS = TrainMaxSpeedMpS;
                    }

                    if (actualSpeedMpS > 0)
                    {
                        validSpeedMpS = actualSpeedMpS;
                        validSpeedLimitMpS = actualSpeedMpS;
                    }
                    thisObject.actual_speed = actualSpeedMpS;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Initialize brakes
        /// <\summary>

        public virtual void InitializeBrakes()
        {
            if (Math.Abs(SpeedMpS) > 0.1)
            {
                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Warning(CabControl.InitializeBrakes, CabSetting.Warn1);
                return;
            }
            if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                Simulator.Confirmer.Confirm(CabControl.InitializeBrakes, CabSetting.Off);

            float maxPressurePSI = 90;
            if (LeadLocomotiveIndex >= 0)
            {
                MSTSLocomotive lead = (MSTSLocomotive)Cars[LeadLocomotiveIndex];
                if (lead.TrainBrakeController != null)
                {
                    lead.TrainBrakeController.UpdatePressure(ref BrakeLine1PressurePSI, 1000, ref BrakeLine4PressurePSI);
                    maxPressurePSI = lead.TrainBrakeController.GetMaxPressurePSI();
                    BrakeLine1PressurePSI =
                            MathHelper.Max(BrakeLine1PressurePSI, maxPressurePSI - lead.TrainBrakeController.GetFullServReductionPSI());
                }
                if (lead.EngineBrakeController != null)
                    lead.EngineBrakeController.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, 1000);
                if (lead.DynamicBrakeController != null)
                {
                    MUDynamicBrakePercent = lead.DynamicBrakeController.Update(1000) * 100;
                    if (MUDynamicBrakePercent == 0)
                        MUDynamicBrakePercent = -1;
                }
            }
            else
            {
                BrakeLine1PressurePSI = BrakeLine3PressurePSI = BrakeLine4PressurePSI = 0;
            }
            BrakeLine2PressurePSI = maxPressurePSI;
            foreach (TrainCar car in Cars)
            {
                car.BrakeSystem.Initialize(LeadLocomotiveIndex < 0, maxPressurePSI, false);
                if (LeadLocomotiveIndex < 0)
                    car.BrakeSystem.BrakeLine1PressurePSI = -1;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Set handbrakes
        /// <\summary>

        public void SetHandbrakePercent(float percent)
        {
            if (SpeedMpS < -.1 || SpeedMpS > .1)
                return;
            foreach (TrainCar car in Cars)
                car.BrakeSystem.SetHandbrakePercent(percent);
        }

        //================================================================================================//
        /// <summary>
        /// Connect brakes
        /// <\summary>

        public void ConnectBrakeHoses()
        {
            if (SpeedMpS < -.1 || SpeedMpS > .1)
                return;
            foreach (TrainCar car in Cars)
                car.BrakeSystem.Connect();
        }

        //================================================================================================//
        /// <summary>
        /// Disconnect brakes
        /// <\summary>

        public void DisconnectBrakes()
        {
            if (SpeedMpS < -.1 || SpeedMpS > .1)
                return;
            int first = -1;
            int last = -1;
            FindLeadLocomotives(ref first, ref last);
            for (int i = 0; i < Cars.Count; i++)
            {
                if (first <= i && i <= last)
                    continue;
                TrainCar car = Cars[i];
                car.BrakeSystem.Disconnect();
            }
        }

        //================================================================================================//
        /// <summary>
        /// Set retainers
        /// <\summary>

        public void SetRetainers(bool increase)
        {
            if (SpeedMpS < -.1 || SpeedMpS > .1)
                return;
            if (!increase)
            {
                RetainerSetting = RetainerSetting.Exhaust;
                RetainerPercent = 100;
            }
            else if (RetainerPercent < 100)
                RetainerPercent *= 2;
            else if (RetainerSetting != RetainerSetting.SlowDirect)
            {
                RetainerPercent = 25;
                switch (RetainerSetting)
                {
                    case RetainerSetting.Exhaust:
                        RetainerSetting = RetainerSetting.LowPressure;
                        break;
                    case RetainerSetting.LowPressure:
                        RetainerSetting = RetainerSetting.HighPressure;
                        break;
                    case RetainerSetting.HighPressure:
                        RetainerSetting = RetainerSetting.SlowDirect;
                        break;
                }
            }
            int first = -1;
            int last = -1;
            FindLeadLocomotives(ref first, ref last);
            int step = 100 / RetainerPercent;
            for (int i = 0; i < Cars.Count; i++)
            {
                int j = Cars.Count - 1 - i;
                if (j <= last)
                    break;
                Cars[j].BrakeSystem.SetRetainer(i % step == 0 ? RetainerSetting : RetainerSetting.Exhaust);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Find lead locomotive
        /// <\summary>

        public void FindLeadLocomotives(ref int first, ref int last)
        {
            first = last = -1;
            if (LeadLocomotiveIndex >= 0)
            {
                for (int i = LeadLocomotiveIndex; i < Cars.Count && Cars[i].IsDriveable; i++)
                    last = i;
                for (int i = LeadLocomotiveIndex; i >= 0 && Cars[i].IsDriveable; i--)
                    first = i;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Propagate brake pressure
        /// <\summary>

        private void PropagateBrakePressure(float elapsedClockSeconds)
        {
            if (LeadLocomotiveIndex >= 0)
            {
                MSTSLocomotive lead = (MSTSLocomotive)Cars[LeadLocomotiveIndex];
                if (lead.TrainBrakeController != null)
                    lead.TrainBrakeController.UpdatePressure(ref BrakeLine1PressurePSI, elapsedClockSeconds, ref BrakeLine4PressurePSI);
                if (lead.EngineBrakeController != null)
                    lead.EngineBrakeController.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, elapsedClockSeconds);
                lead.BrakeSystem.PropagateBrakePressure(elapsedClockSeconds);
            }
            else
            {
                foreach (TrainCar car in Cars)
                {
                    if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                        continue;
                    car.BrakeSystem.BrakeLine1PressurePSI = BrakeLine1PressurePSI;
                    car.BrakeSystem.BrakeLine2PressurePSI = BrakeLine2PressurePSI;
                    car.BrakeSystem.BrakeLine3PressurePSI = 0;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Cars have been added to the rear of the train, recalc the rearTDBtraveller
        /// </summary>

        public void RepositionRearTraveller()
        {
            var traveller = new Traveller(FrontTDBTraveller, Traveller.TravellerDirection.Backward);
            // The traveller location represents the front of the train.
            var length = 0f;

            // process the cars first to last
            for (var i = 0; i < Cars.Count; ++i)
            {
                var car = Cars[i];
                if (car.WheelAxlesLoaded)
                {
                    car.ComputePosition(traveller, false, SpeedMpS);
                }
                else
                {
                    var bogieSpacing = car.Length * 0.65f;  // we'll use this approximation since the wagfile doesn't contain info on bogie position

                    // traveller is positioned at the front of the car
                    // advance to the first bogie 
                    traveller.Move((car.Length - bogieSpacing) / 2.0f);
                    var tileX = traveller.TileX;
                    var tileZ = traveller.TileZ;
                    var x = traveller.X;
                    var y = traveller.Y;
                    var z = traveller.Z;
                    traveller.Move(bogieSpacing);

                    // normalize across tile boundaries
                    while (tileX > traveller.TileX)
                    {
                        x += 2048;
                        --tileX;
                    }
                    while (tileX < traveller.TileX)
                    {
                        x -= 2048;
                        ++tileX;
                    }
                    while (tileZ > traveller.TileZ)
                    {
                        z += 2048;
                        --tileZ;
                    }
                    while (tileZ < traveller.TileZ)
                    {
                        z -= 2048;
                        ++tileZ;
                    }

                    // note the railcar sits 0.275meters above the track database path  TODO - is this always consistent?
                    car.WorldPosition.XNAMatrix = Matrix.Identity;
                    if (!car.Flipped)
                    {
                        //  Rotate matrix 180' around Y axis.
                        car.WorldPosition.XNAMatrix.M11 = -1;
                        car.WorldPosition.XNAMatrix.M33 = -1;
                    }
                    car.WorldPosition.XNAMatrix *= Simulator.XNAMatrixFromMSTSCoordinates(traveller.X, traveller.Y + 0.275f, traveller.Z, x, y + 0.275f, z);
                    car.WorldPosition.TileX = traveller.TileX;
                    car.WorldPosition.TileZ = traveller.TileZ;

                    traveller.Move((car.Length - bogieSpacing) / 2.0f);
                }
                if (i < Cars.Count - 1)
                {
                    traveller.Move(car.CouplerSlackM + car.GetCouplerZeroLengthM());
                    length += car.CouplerSlackM + car.GetCouplerZeroLengthM();
                }
                length += car.Length;
            }

            traveller.ReverseDirection();
            RearTDBTraveller = traveller;
            Length = length;
        } // RepositionRearTraveller


        //================================================================================================//
        /// <summary>
        /// Check if train is passenger or freight train
        /// </summary>

        public void CheckFreight()
        {
            IsFreight = false;
            foreach (var car in Cars)
            {
                if (car.IsFreight)
                    IsFreight = true;
            }
        } // CheckFreight

        //================================================================================================//
        /// <summary>
        /// Distance is the signed distance the cars are moving.
        /// </summary>
        /// <param name="distance"></param>

        public void CalculatePositionOfCars(float distance)
        {
            if (float.IsNaN(distance)) distance = 0;//sanity check

            var tn = RearTDBTraveller.TN;
            RearTDBTraveller.Move(distance);

            // TODO : check if train moved back into previous section

            var traveller = new Traveller(RearTDBTraveller);
            // The traveller location represents the back of the train.
            var length = 0f;

            // process the cars last to first
            for (var i = Cars.Count - 1; i >= 0; --i)
            {
                var car = Cars[i];
                if (i < Cars.Count - 1)
                {
                    traveller.Move(car.CouplerSlackM + car.GetCouplerZeroLengthM());
                    length += car.CouplerSlackM + car.GetCouplerZeroLengthM();
                }
                if (car.WheelAxlesLoaded)
                {
                    car.ComputePosition(traveller, true, SpeedMpS);
                }
                else
                {
                    var bogieSpacing = car.Length * 0.65f;  // we'll use this approximation since the wagfile doesn't contain info on bogie position

                    // traveller is positioned at the back of the car
                    // advance to the first bogie 
                    traveller.Move((car.Length - bogieSpacing) / 2.0f);
                    var tileX = traveller.TileX;
                    var tileZ = traveller.TileZ;
                    var x = traveller.X;
                    var y = traveller.Y;
                    var z = traveller.Z;
                    traveller.Move(bogieSpacing);

                    // normalize across tile boundaries
                    while (tileX > traveller.TileX)
                    {
                        x += 2048;
                        --tileX;
                    }
                    while (tileX < traveller.TileX)
                    {
                        x -= 2048;
                        ++tileX;
                    }
                    while (tileZ > traveller.TileZ)
                    {
                        z += 2048;
                        --tileZ;
                    }
                    while (tileZ < traveller.TileZ)
                    {
                        z -= 2048;
                        ++tileZ;
                    }


                    // note the railcar sits 0.275meters above the track database path  TODO - is this always consistent?
                    car.WorldPosition.XNAMatrix = Matrix.Identity;
                    if (car.Flipped)
                    {
                        //  Rotate matrix 180' around Y axis.
                        car.WorldPosition.XNAMatrix.M11 = -1;
                        car.WorldPosition.XNAMatrix.M33 = -1;
                    }
                    car.WorldPosition.XNAMatrix *= Simulator.XNAMatrixFromMSTSCoordinates(traveller.X, traveller.Y + 0.275f, traveller.Z, x, y + 0.275f, z);
                    car.WorldPosition.TileX = traveller.TileX;
                    car.WorldPosition.TileZ = traveller.TileZ;


                    if (Program.Simulator.UseSuperElevation > 0 || Program.Simulator.CarVibrating > 0 || this.tilted)
                    {
                        car.RealXNAMatrix = car.WorldPosition.XNAMatrix;
                        car.SuperElevation(SpeedMpS, Program.Simulator.UseSuperElevation, traveller);
                    }

                    traveller.Move((car.Length - bogieSpacing) / 2.0f);  // Move to the front of the car 
                }
                length += car.Length;
            }

            FrontTDBTraveller = traveller;
            Length = length;
            travelled += distance;
        } // CalculatePositionOfCars

        //================================================================================================//
        /// <summary>
        ///  Sets this train's speed so that momentum is conserved when otherTrain is coupled to it
        /// <\summary>

        public void SetCoupleSpeed(Train otherTrain, float otherMult)
        {
            float kg1 = 0;
            foreach (TrainCar car in Cars)
                kg1 += car.MassKG;
            float kg2 = 0;
            foreach (TrainCar car in otherTrain.Cars)
                kg2 += car.MassKG;
            SpeedMpS = (kg1 * SpeedMpS + kg2 * otherTrain.SpeedMpS * otherMult) / (kg1 + kg2);
            otherTrain.SpeedMpS = SpeedMpS;
            foreach (TrainCar car1 in Cars)
                car1.SpeedMpS = car1.Flipped ? -SpeedMpS : SpeedMpS;
            foreach (TrainCar car2 in otherTrain.Cars)
                car2.SpeedMpS = car2.Flipped ? -SpeedMpS : SpeedMpS;
        }


        //================================================================================================//
        /// <summary>
        /// setups of the left hand side of the coupler force solving equations
        /// <\summary>

        void SetupCouplerForceEquations()
        {
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                car.CouplerForceB = 1 / car.MassKG;
                car.CouplerForceA = -car.CouplerForceB;
                car.CouplerForceC = -1 / Cars[i + 1].MassKG;
                car.CouplerForceB -= car.CouplerForceC;
            }
        }


        //================================================================================================//
        /// <summary>
        /// solves coupler force equations
        /// <\summary>

        void SolveCouplerForceEquations()
        {
            float b = Cars[0].CouplerForceB;
            Cars[0].CouplerForceU = Cars[0].CouplerForceR / b;
            for (int i = 1; i < Cars.Count - 1; i++)
            {
                Cars[i].CouplerForceG = Cars[i - 1].CouplerForceC / b;
                b = Cars[i].CouplerForceB - Cars[i].CouplerForceA * Cars[i].CouplerForceG;
                Cars[i].CouplerForceU = (Cars[i].CouplerForceR - Cars[i].CouplerForceA * Cars[i - 1].CouplerForceU) / b;
            }
            for (int i = Cars.Count - 3; i >= 0; i--)
                Cars[i].CouplerForceU -= Cars[i + 1].CouplerForceG * Cars[i + 1].CouplerForceU;
        }


        //================================================================================================//
        /// <summary>
        /// removes equations if forces don't match faces in contact
        /// returns true if a change is made
        /// <\summary>

        bool FixCouplerForceEquations()
        {
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                if (car.CouplerSlackM < 0 || car.CouplerForceB >= 1)
                    continue;
                float maxs1 = car.GetMaximumCouplerSlack1M();
                if (car.CouplerSlackM < maxs1 || car.CouplerForceU > 0)
                {
                    SetCouplerForce(car, 0);
                    return true;
                }
            }
            for (int i = Cars.Count - 1; i >= 0; i--)
            {
                TrainCar car = Cars[i];
                if (car.CouplerSlackM > 0 || car.CouplerForceB >= 1)
                    continue;
                float maxs1 = car.GetMaximumCouplerSlack1M();
                if (car.CouplerSlackM > -maxs1 || car.CouplerForceU < 0)
                {
                    SetCouplerForce(car, 0);
                    return true;
                }
            }
            return false;
        }


        //================================================================================================//
        /// <summary>
        /// changes the coupler force equation for car to make the corresponding force equal to forceN
        /// <\summary>

        void SetCouplerForce(TrainCar car, float forceN)
        {
            car.CouplerForceA = car.CouplerForceC = 0;
            car.CouplerForceB = 1;
            car.CouplerForceR = forceN;
        }

        //================================================================================================//
        /// <summary>
        /// removes equations if forces don't match faces in contact
        /// returns true if a change is made
        /// <\summary>

        bool FixCouplerImpulseForceEquations()
        {
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                if (car.CouplerSlackM < 0 || car.CouplerForceB >= 1)
                    continue;
                if (car.CouplerSlackM < car.CouplerSlack2M || car.CouplerForceU > 0)
                {
                    SetCouplerForce(car, 0);
                    return true;
                }
            }
            for (int i = Cars.Count - 1; i >= 0; i--)
            {
                TrainCar car = Cars[i];
                if (car.CouplerSlackM > 0 || car.CouplerForceB >= 1)
                    continue;
                if (car.CouplerSlackM > -car.CouplerSlack2M || car.CouplerForceU < 0)
                {
                    SetCouplerForce(car, 0);
                    return true;
                }
            }
            return false;
        }


        //================================================================================================//
        /// <summary>
        /// computes and applies coupler impulse forces which force speeds to match when no relative movement is possible
        /// <\summary>

        void AddCouplerImpuseForces()
        {
            if (Cars.Count < 2)
                return;
            SetupCouplerForceEquations();
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                float max = car.CouplerSlack2M;
                if (-max < car.CouplerSlackM && car.CouplerSlackM < max)
                {
                    car.CouplerForceB = 1;
                    car.CouplerForceA = car.CouplerForceC = car.CouplerForceR = 0;
                }
                else
                    car.CouplerForceR = Cars[i + 1].SpeedMpS - car.SpeedMpS;
            }
            do
                SolveCouplerForceEquations();
            while (FixCouplerImpulseForceEquations());
            MaximumCouplerForceN = 0;
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                Cars[i].SpeedMpS += Cars[i].CouplerForceU / Cars[i].MassKG;
                Cars[i + 1].SpeedMpS -= Cars[i].CouplerForceU / Cars[i + 1].MassKG;
                //if (Cars[i].CouplerForceU != 0)
                //    Console.WriteLine("impulse {0} {1} {2} {3} {4}", i, Cars[i].CouplerForceU, Cars[i].CouplerSlackM, Cars[i].SpeedMpS, Cars[i+1].SpeedMpS);
                //if (MaximumCouplerForceN < Math.Abs(Cars[i].CouplerForceU))
                //    MaximumCouplerForceN = Math.Abs(Cars[i].CouplerForceU);
            }
        }


        //================================================================================================//
        /// <summary>
        /// computes coupler acceleration balancing forces
        /// <\summary>

        void ComputeCouplerForces()
        {
            for (int i = 0; i < Cars.Count; i++)
                if (Cars[i].SpeedMpS > 0)
                    Cars[i].TotalForceN -= (Cars[i].FrictionForceN + Cars[i].BrakeForceN);
                else if (Cars[i].SpeedMpS < 0)
                    Cars[i].TotalForceN += Cars[i].FrictionForceN + Cars[i].BrakeForceN;
            if (Cars.Count < 2)
                return;
            SetupCouplerForceEquations();
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                float max = car.GetMaximumCouplerSlack1M();
                if (-max < car.CouplerSlackM && car.CouplerSlackM < max)
                {
                    car.CouplerForceB = 1;
                    car.CouplerForceA = car.CouplerForceC = car.CouplerForceR = 0;
                }
                else
                    car.CouplerForceR = Cars[i + 1].TotalForceN / Cars[i + 1].MassKG - car.TotalForceN / car.MassKG;
            }
            do
                SolveCouplerForceEquations();
            while (FixCouplerForceEquations());
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                car.TotalForceN += car.CouplerForceU;
                Cars[i + 1].TotalForceN -= car.CouplerForceU;
                if (MaximumCouplerForceN < Math.Abs(car.CouplerForceU))
                    MaximumCouplerForceN = Math.Abs(car.CouplerForceU);
                float maxs = car.GetMaximumCouplerSlack2M();
                if (car.CouplerForceU > 0)
                {
                    float f = -(car.CouplerSlackM + car.GetMaximumCouplerSlack1M()) * car.GetCouplerStiffnessNpM();
                    if (car.CouplerSlackM > -maxs && f > car.CouplerForceU)
                        car.CouplerSlack2M = -car.CouplerSlackM;
                    else
                        car.CouplerSlack2M = maxs;
                }
                else if (car.CouplerForceU == 0)
                    car.CouplerSlack2M = maxs;
                else
                {
                    float f = (car.CouplerSlackM - car.GetMaximumCouplerSlack1M()) * car.GetCouplerStiffnessNpM();
                    if (car.CouplerSlackM < maxs && f > car.CouplerForceU)
                        car.CouplerSlack2M = car.CouplerSlackM;
                    else
                        car.CouplerSlack2M = maxs;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update Car speeds
        /// <\summary>

        void UpdateCarSpeeds(float elapsedTime)
        {
            int n = 0;

            foreach (TrainCar car in Cars)
            {
                if (car.SpeedMpS > 0)
                {
                    car.SpeedMpS += car.TotalForceN / car.MassKG * elapsedTime;
                    if (car.SpeedMpS < 0)
                        car.SpeedMpS = 0;
                }
                else if (car.SpeedMpS < 0)
                {
                    car.SpeedMpS += car.TotalForceN / car.MassKG * elapsedTime;
                    if (car.SpeedMpS > 0)
                        car.SpeedMpS = 0;
                }
                else
                    n++;
            }
            if (n == 0)
                return;

            // start cars moving forward

            for (int i = 0; i < Cars.Count; i++)
            {
                TrainCar car = Cars[i];
                if (car.SpeedMpS != 0 || car.TotalForceN <= (car.FrictionForceN + car.BrakeForceN))
                    continue;
                int j = i;
                float f = 0;
                float m = 0;
                for (; ; )
                {
                    if (car.IsDriveable)
                        f += car.TotalForceN - (car.FrictionForceN);
                    else
                        f += car.TotalForceN - (car.FrictionForceN + car.BrakeForceN);
                    m += car.MassKG;
                    if (j == Cars.Count - 1 || car.CouplerSlackM < car.GetMaximumCouplerSlack2M())
                        break;
                    j++;
                    car = Cars[j];
                }
                if (f > 0)
                {
                    for (int k = i; k <= j; k++)
                        Cars[k].SpeedMpS = f / m * elapsedTime;
                    n -= j - i + 1;
                }
            }
            if (n == 0)
                return;
            // start cars moving backward
            for (int i = Cars.Count - 1; i >= 0; i--)
            {
                TrainCar car = Cars[i];
                if (car.SpeedMpS != 0 || car.TotalForceN > (-1.0f * (car.FrictionForceN + car.BrakeForceN)))
                    continue;
                int j = i;
                float f = 0;
                float m = 0;
                for (; ; )
                {
                    if (car.IsDriveable)
                        f += car.TotalForceN + car.FrictionForceN;
                    else
                        f += car.TotalForceN + car.FrictionForceN + car.BrakeForceN;
                    m += car.MassKG;
                    if (j == 0 || car.CouplerSlackM > -car.GetMaximumCouplerSlack2M())
                        break;
                    j--;
                    car = Cars[j];
                }
                if (f < 0)
                {
                    for (int k = j; k <= i; k++)
                        Cars[k].SpeedMpS = f / m * elapsedTime;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update coupler slack
        /// <\summary>

        void UpdateCouplerSlack(float elapsedTime)
        {
            TotalCouplerSlackM = 0;
            NPull = NPush = 0;
            for (int i = 0; i < Cars.Count - 1; i++)
            {
                TrainCar car = Cars[i];
                car.CouplerSlackM += (car.SpeedMpS - Cars[i + 1].SpeedMpS) * elapsedTime;
                float max = car.GetMaximumCouplerSlack2M();
                if (car.CouplerSlackM < -max)
                    car.CouplerSlackM = -max;
                else if (car.CouplerSlackM > max)
                    car.CouplerSlackM = max;
                TotalCouplerSlackM += car.CouplerSlackM;
                max = car.GetMaximumCouplerSlack1M();
                if (car.CouplerSlackM >= max)
                    NPull++;
                else if (car.CouplerSlackM <= -max)
                    NPush++;
            }
            foreach (TrainCar car in Cars)
                car.DistanceM += Math.Abs(car.SpeedMpS * elapsedTime);
        }

        //================================================================================================//
        /// <summary>
        /// Calculate initial position
        /// </summary>

        public TCSubpathRoute CalculateInitialTrainPosition(ref bool trackClear)
        {

            // calculate train length

            float trainLength = 0f;

            for (var i = Cars.Count - 1; i >= 0; --i)
            {
                var car = Cars[i];
                if (i < Cars.Count - 1)
                {
                    trainLength += car.CouplerSlackM + car.GetCouplerZeroLengthM();
                }
                trainLength += car.Length;
            }

            // get starting position and route

            TrackNode tn = RearTDBTraveller.TN;
            float offset = RearTDBTraveller.TrackNodeOffset;
            int direction = (int)RearTDBTraveller.Direction;

            tn.TCCrossReference.GetTCPosition(offset, direction, ref PresentPosition[1]);
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
            offset = PresentPosition[1].TCOffset;

            // create route if train has none

            if (ValidRoute[0] == null)
            {
                ValidRoute[0] = signalRef.BuildTempRoute(this, thisSection.Index, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, trainLength, false, true, true);
            }

            // find sections

            bool sectionAvailable = true;
            float remLength = trainLength;
            int routeIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
            if (routeIndex < 0)
                routeIndex = 0;

            bool sectionsClear = true;

            TCSubpathRoute tempRoute = new TCSubpathRoute();

            TCRouteElement thisElement = ValidRoute[0][routeIndex];
            thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
            if (!thisSection.CanPlaceTrain(this, offset, remLength))
            {
                sectionsClear = false;
            }

            while (remLength > 0 && sectionAvailable)
            {
                tempRoute.Add(thisElement);
                remLength -= (thisSection.Length - offset);
                offset = 0.0f;

                if (remLength > 0)
                {
                    if (routeIndex < ValidRoute[0].Count - 1)
                    {
                        routeIndex++;
                        thisElement = ValidRoute[0][routeIndex];
                        thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        if (!thisSection.CanPlaceTrain(this, offset, remLength))
                        {
                            sectionsClear = false;
                        }
                        offset = 0.0f;
                    }
                    else
                    {
                        Trace.TraceWarning("Not sufficient track to place train {0}", Number);
                        sectionAvailable = false;
                    }
                }

            }

            trackClear = true;

            if (MultiPlayer.MPManager.IsMultiPlayer()) return (tempRoute);
            if (!sectionAvailable || !sectionsClear)
            {
                trackClear = false;
                tempRoute.Clear();
            }

            return (tempRoute);
        }

        //================================================================================================//
        //
        // Set initial train route
        //

        public void SetInitialTrainRoute(TCSubpathRoute tempRoute)
        {

            // reserve sections, use direction 0 only

            foreach (TCRouteElement thisElement in tempRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                thisSection.Reserve(routedForward, tempRoute);
            }
        }

        //================================================================================================//
        //
        // Reset initial train route
        //

        public void ResetInitialTrainRoute(TCSubpathRoute tempRoute)
        {

            // unreserve sections

            foreach (TCRouteElement thisElement in tempRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                thisSection.RemoveTrain(this, false);
            }
        }

        //================================================================================================//
        //
        // Initial train placement
        //

        public bool InitialTrainPlacement()
        {
            // for initial placement, use direction 0 only
            // set initial positions

            TrackNode tn = FrontTDBTraveller.TN;
            float offset = FrontTDBTraveller.TrackNodeOffset;
            int direction = (int)FrontTDBTraveller.Direction;

            tn.TCCrossReference.GetTCPosition(offset, direction, ref PresentPosition[0]);
            PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            DistanceTravelledM = 0.0f;

            tn = RearTDBTraveller.TN;
            offset = RearTDBTraveller.TrackNodeOffset;
            direction = (int)RearTDBTraveller.Direction;

            tn.TCCrossReference.GetTCPosition(offset, direction, ref PresentPosition[1]);

            // check if train has route, if not create dummy

            if (ValidRoute[0] == null)
            {
                ValidRoute[0] = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                        PresentPosition[1].TCDirection, Length, false, true, true);
            }

            // get index of first section in route

            int rearIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
            if (rearIndex < 0)
            {
                Trace.TraceWarning("Start position of end of train {0} not on route ", Number);
                rearIndex = 0;
            }

            PresentPosition[1].RouteListIndex = rearIndex;

            // get index of front of train

            int frontIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
            if (frontIndex < 0)
            {
                Trace.TraceWarning("Start position of front of train {0} not on route ", Number);
                frontIndex = 0;
            }

            PresentPosition[0].RouteListIndex = frontIndex;

            // check if train can be placed
            // get index of section in train route //

            int routeIndex = rearIndex;
            List<TrackCircuitSection> placementSections = new List<TrackCircuitSection>();

            // check if route is available

            offset = PresentPosition[1].TCOffset;
            float remLength = Length;
            bool sectionAvailable = true;

            for (int iRouteIndex = rearIndex; iRouteIndex <= frontIndex && sectionAvailable; iRouteIndex++)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iRouteIndex].TCSectionIndex];
                if (thisSection.CanPlaceTrain(this, offset, remLength))
                {
                    placementSections.Add(thisSection);
                    remLength -= (thisSection.Length - offset);

                    if (remLength > 0)
                    {
                        if (routeIndex < ValidRoute[0].Count - 1)
                        {
                            routeIndex++;
                            TCRouteElement thisElement = ValidRoute[0][routeIndex];
                            thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                            offset = 0.0f;
                        }
                        else
                        {
                            Trace.TraceWarning("Not sufficient track to place train");
                            sectionAvailable = false;
                        }
                    }

                }
                else
                {
                    sectionAvailable = false;
                }
            }

            // if not available - return

            if (!sectionAvailable || placementSections.Count <= 0)
            {
                return (false);
            }

            // set track occupied

            foreach (TrackCircuitSection thisSection in placementSections)
            {
                thisSection.Reserve(routedForward, ValidRoute[0]);
                thisSection.SetOccupied(routedForward);
            }

            return (true);
        }

        //================================================================================================//
        /// <summary>
        /// Update train position
        /// Returns indices in present route path for sections which train has entered
        /// </summary>

        public void UpdateTrainPosition()
        {
            List<int> sectionsPassed = new List<int>();

            // update positions

            PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            TrackNode tn = FrontTDBTraveller.TN;
            float offset = FrontTDBTraveller.TrackNodeOffset;
            int direction = (int)FrontTDBTraveller.Direction;

            tn.TCCrossReference.GetTCPosition(offset, direction, ref PresentPosition[0]);
            int routeIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
            PresentPosition[0].RouteListIndex = routeIndex;

            tn = RearTDBTraveller.TN;
            offset = RearTDBTraveller.TrackNodeOffset;
            direction = (int)RearTDBTraveller.Direction;

            tn.TCCrossReference.GetTCPosition(offset, direction, ref PresentPosition[1]);
            routeIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
            PresentPosition[1].RouteListIndex = routeIndex;
        }

        //================================================================================================//
        /// <summary>
        /// Update Position linked information
        /// Switches train to Out_Of_Control if it runs out of path
        /// <\summary>

        public void UpdateTrainPositionInformation()
        {

            // check if train still on route - set train to OUT_OF_CONTROL

            PresentPosition[0].DistanceTravelledM = DistanceTravelledM;
            PresentPosition[1].DistanceTravelledM = DistanceTravelledM - Length;

            if (PresentPosition[0].RouteListIndex < 0)
            {
                SetTrainOutOfControl(OUTOFCONTROL.OUT_OF_PATH);
            }
            else if (StationStops.Count > 0)
            {
                StationStop thisStation = StationStops[0];

                int thisSectionIndex = PresentPosition[0].TCSectionIndex;
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];
                float leftInSectionM = thisSection.Length - PresentPosition[0].TCOffset;

                // if present position off route, try rear position
                // if both off route, skip station stop
                int stationIndex = ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, PresentPosition[0].RouteListIndex);
                float distanceToTrainM = ValidRoute[0].GetDistanceAlongRoute(PresentPosition[0].RouteListIndex,
                    leftInSectionM, stationIndex, thisStation.StopOffset, true, signalRef);

                thisStation.DistanceToTrainM = distanceToTrainM;
            }
        }


        //================================================================================================//
        /// <summary>
        /// get list of required actions (only if not moving backward)
        /// </summary>

        public void ObtainRequiredActions(int backward)
        {
            if (backward < backwardThreshold)
            {
                List<DistanceTravelledItem> nowActions = requiredActions.GetActions(DistanceTravelledM);
                if (nowActions.Count > 0)
                {
                    PerformActions(nowActions);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update section occupy states
        /// Input is backward movement counter
        /// </summary>

        public void UpdateSectionState(int backward)
        {

            List<int> sectionList = new List<int>();

            int lastIndex = PreviousPosition[0].RouteListIndex;
            float lastOffset = PreviousPosition[0].TCOffset;
            int presentIndex = PresentPosition[0].RouteListIndex;
            float presentOffset = PresentPosition[0].TCOffset;

            // don't bother with update if train out of control - all will be reset when train is stopped

            if (ControlMode == TRAIN_CONTROL.OUT_OF_CONTROL)
            {
                return;
            }

            // don't bother with update if train off route - set train to out of control

            if (presentIndex < 0)
            {
                SetTrainOutOfControl(OUTOFCONTROL.OUT_OF_PATH);
                return;
            }

            // train moved backward

            if (backward > backwardThreshold)
            {
                if (presentIndex < lastIndex)
                {
                    int sectionIndex;
                    TrackCircuitSection thisSection;
                    float offset = PreviousPosition[0].TCOffset;

                    for (int iIndex = lastIndex; iIndex > presentIndex; iIndex--)
                    {
                        sectionIndex = ValidRoute[0][iIndex].TCSectionIndex;
                        sectionList.Add(iIndex);
                        thisSection = signalRef.TrackCircuitList[sectionIndex];
                    }

                    sectionIndex = ValidRoute[0][presentIndex].TCSectionIndex;
                    thisSection = signalRef.TrackCircuitList[sectionIndex];
                    sectionList.Add(presentIndex);
                }
            }

        // train moves forward

            else
            {
                if (presentIndex > lastIndex)
                {
                    int sectionIndex;
                    TrackCircuitSection thisSection;

                    sectionIndex = ValidRoute[0][lastIndex].TCSectionIndex;
                    thisSection = signalRef.TrackCircuitList[sectionIndex];

                    for (int iIndex = lastIndex + 1; iIndex < presentIndex; iIndex++)
                    {
                        sectionIndex = ValidRoute[0][iIndex].TCSectionIndex;
                        sectionList.Add(iIndex);
                        thisSection = signalRef.TrackCircuitList[sectionIndex];
                    }

                    sectionIndex = ValidRoute[0][presentIndex].TCSectionIndex;
                    sectionList.Add(presentIndex);
                }
            }

            // set section states, for AUTOMODE use direction 0 only

            foreach (int routeListIndex in sectionList)
            {
                int sectionIndex = ValidRoute[0][routeListIndex].TCSectionIndex;
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[sectionIndex];
                if (!thisSection.CircuitState.ThisTrainOccupying(routedForward))
                {
                    thisSection.SetOccupied(routedForward);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check if train went passed signal
        /// if so, and signal was at danger, set train Out_Of_Control
        /// </summary>

        public int CheckSignalPassed(int direction, TCPosition trainPosition, TCPosition trainPreviousPos)
        {
            int passedSignalIndex = -1;
            if (NextSignalObject[direction] != null)
            {

                TrackCircuitSection nextSection = signalRef.TrackCircuitList[NextSignalObject[direction].TCNextTC];
                while (NextSignalObject[direction] != null && !ValidRoute[direction].SignalIsAheadOfTrain(NextSignalObject[direction], trainPosition)) // signal not in front //
                {
                    // check if train really went passed signal in correct direction
                    if (ValidRoute[direction].SignalIsAheadOfTrain(NextSignalObject[direction], trainPreviousPos)) // train was in front on last check, so we did pass
                    {
                        SignalHead.SIGASP signalState = GetNextSignalAspect(direction);
                        passedSignalIndex = NextSignalObject[direction].thisRef;

#if DEBUG_REPORTS
                        String report = "Passing signal ";
                        report = String.Concat(report, NextSignalObject[direction].thisRef.ToString());
                        report = String.Concat(report, " with state ", signalState.ToString());
                        report = String.Concat(report, " by train ", Number.ToString());
                        report = String.Concat(report, " at ", FormatStrings.FormatDistance(DistanceTravelledM, true));
                        report = String.Concat(report, " and ", FormatStrings.FormatSpeed(SpeedMpS, true));
                        File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
                        if (CheckTrain)
                        {
                            String reportCT = "Passing signal ";
                            reportCT = String.Concat(reportCT, NextSignalObject[direction].thisRef.ToString());
                            reportCT = String.Concat(reportCT, " with state ", signalState.ToString());
                            reportCT = String.Concat(reportCT, " by train ", Number.ToString());
                            reportCT = String.Concat(reportCT, " at ", FormatStrings.FormatDistance(DistanceTravelledM, true));
                            reportCT = String.Concat(reportCT, " and ", FormatStrings.FormatSpeed(SpeedMpS, true));
                            File.AppendAllText(@"C:\temp\checktrain.txt", reportCT + "\n");
                        }

                        if (signalState == SignalHead.SIGASP.STOP && NextSignalObject[direction].hasPermission == SignalObject.PERMISSION.DENIED)
                        {
                            Trace.TraceWarning("Train {0} passing signal {1} at {2} at danger at {3}",
                               Number.ToString(), NextSignalObject[direction].thisRef.ToString(),
                               DistanceTravelledM.ToString("###0.0"), SpeedMpS.ToString("##0.00"));
                            SetTrainOutOfControl(OUTOFCONTROL.SPAD);
                            break;
                        }

                        else if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL && NextSignalObject[direction].sigfound[(int)SignalHead.SIGFN.NORMAL] < 0) // no next signal
                        {
                            SwitchToNodeControl(LastReservedSection[direction]);
#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number.ToString() +
                                        " set to NODE control for no next signal from " + NextSignalObject[direction].thisRef.ToString() + "\n");
#endif
                            if (CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number.ToString() +
                                                " set to NODE control for no next signal from " + NextSignalObject[direction].thisRef.ToString() + "\n");
                            }
                            break;
                        }
                        else if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL && NextSignalObject[direction].block_state() != SignalObject.BLOCKSTATE.CLEAR) // route to next signal not clear
                        {
                            SwitchToNodeControl(LastReservedSection[direction]);
#if DEBUG_REPORTS
                        File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number.ToString() +
                                        " set to NODE control for route to next signal not clear from " + NextSignalObject[direction].thisRef.ToString() + "\n");
#endif
                            if (CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number.ToString() +
                                                " set to NODE control for route to next signal not clear from " + NextSignalObject[direction].thisRef.ToString() + "\n");
                            }
                            break;
                        }
                    }

                    // get next signal
                    int nextSignalIndex = NextSignalObject[direction].sigfound[(int)SignalHead.SIGFN.NORMAL];
                    if (nextSignalIndex >= 0)
                    {
                        NextSignalObject[direction] = signalRef.SignalObjects[nextSignalIndex];
                        nextSection = signalRef.TrackCircuitList[NextSignalObject[direction].TCNextTC];
                    }
                    else
                    {
                        NextSignalObject[direction] = null;
                    }
                }
            }

            return (passedSignalIndex);
        }

        //================================================================================================//
        /// <summary>
        /// Check if train moves backward and if so, check clearance behindtrain
        /// If no save clearance left, set train to Out_Of_Control
        /// </summary>

        public int CheckBackwardClearance()
        {
            bool outOfControl = false;

            int lastIndex = PreviousPosition[0].RouteListIndex;
            float lastOffset = PreviousPosition[0].TCOffset;
            int presentIndex = PresentPosition[0].RouteListIndex;
            float presentOffset = PresentPosition[0].TCOffset;

            if (presentIndex < 0) // we are off the path, stop train //
            {
                SetTrainOutOfControl(OUTOFCONTROL.OUT_OF_PATH);
            }

            // backward

            if (presentIndex < lastIndex || (presentIndex == lastIndex && presentOffset < lastOffset))
            {
                movedBackward = movedBackward < 2 * backwardThreshold ? ++movedBackward : movedBackward;

#if DEBUG_REPORTS
                String report = "Moving backward : ";
                report = String.Concat(report, " train ", Number.ToString());
                File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
                report = "Previous position : ";
                report = String.Concat(report, lastIndex.ToString(), " + ", lastOffset.ToString());
                File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
                report = "Present  position : ";
                report = String.Concat(report, presentIndex.ToString(), " + ", presentOffset.ToString());
                File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
                report = "Backward counter : ";
                report = String.Concat(report, movedBackward.ToString());
                File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
                if (CheckTrain)
                {
                    string ctreport = "Moving backward : ";
                    ctreport = String.Concat(ctreport, " train ", Number.ToString());
                    File.AppendAllText(@"C:\temp\checktrain.txt", ctreport + "\n");
                    ctreport = "Previous position : ";
                    ctreport = String.Concat(ctreport, lastIndex.ToString(), " + ", lastOffset.ToString());
                    File.AppendAllText(@"C:\temp\checktrain.txt", ctreport + "\n");
                    ctreport = "Present  position : ";
                    ctreport = String.Concat(ctreport, presentIndex.ToString(), " + ", presentOffset.ToString());
                    File.AppendAllText(@"C:\temp\checktrain.txt", ctreport + "\n");
                    ctreport = "Backward counter : ";
                    ctreport = String.Concat(ctreport, movedBackward.ToString());
                    File.AppendAllText(@"C:\temp\checktrain.txt", ctreport + "\n");
                }

                // run through sections behind train
                // if still in train route : try to reserve section
                // if multiple train in section : calculate distance to next train, stop oncoming train
                // if section reserved for train : stop train
                // if out of route : set out_of_control
                // if signal : set distance, check if passed

                // TODO : check if other train in section, get distance to train
                // TODO : check correct alignment of any switches passed over while moving backward (reset activepins)

                if (RearSignalObject != null)
                {

                    // create new position some 25 m. behind train as allowed overlap

                    TCPosition overlapPosition = new TCPosition();
                    PresentPosition[1].CopyTo(ref overlapPosition);
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[overlapPosition.TCSectionIndex];
                    overlapPosition.TCOffset = thisSection.Length - (PresentPosition[1].TCOffset + rearPositionOverlap);  // reverse offset because of reversed direction
                    overlapPosition.TCDirection = overlapPosition.TCDirection == 0 ? 1 : 0; // looking backwards, so reverse direction

                    TrackCircuitSection rearSection = signalRef.TrackCircuitList[RearSignalObject.TCNextTC];
                    if (!ValidRoute[0].IsAheadOfTrain(rearSection, 0.0f, overlapPosition))
                    {
                        if (RearSignalObject.this_sig_lr(SignalHead.SIGFN.NORMAL) == SignalHead.SIGASP.STOP)
                        {
                            SetTrainOutOfControl(OUTOFCONTROL.SPAD_REAR);
                            outOfControl = true;
                        }
                        else
                        {
                            RearSignalObject = null;   // passed signal, so reset //
                        }
                    }
                }

                if (!outOfControl && RearSignalObject == null)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
                    float clearPath = thisSection.Length - PresentPosition[1].TCOffset;   // looking other direction //
                    int direction = PresentPosition[1].TCDirection == 0 ? 1 : 0;

                    while (clearPath < rearPositionOverlap && !outOfControl && RearSignalObject == null)
                    {
                        if (thisSection.EndSignals[direction] != null)
                        {
                            RearSignalObject = thisSection.EndSignals[direction];
                        }
                        else
                        {
                            int pinLink = direction == 0 ? 1 : 0;

                            // TODO : check required junction and crossover path

                            int nextSectionIndex = thisSection.Pins[pinLink, 0].Link;
                            if (nextSectionIndex >= 0)
                            {
                                TrackCircuitSection nextSection = signalRef.TrackCircuitList[nextSectionIndex];
                                if (!nextSection.IsAvailable(this))
                                {
                                    SetTrainOutOfControl(OUTOFCONTROL.SLIPPED_INTO_PATH);
                                    outOfControl = true;

                                    // stop train in path

                                    List<TrainRouted> trainsInSection = nextSection.CircuitState.TrainsOccupying();
                                    foreach (TrainRouted nextTrain in trainsInSection)
                                    {
                                        nextTrain.Train.ForcedStop("Other train is blocking path", Number);
                                    }

                                    if (nextSection.CircuitState.TrainReserved != null)
                                    {
                                        nextSection.CircuitState.TrainReserved.Train.ForcedStop("Other train is blocking path", Number);
                                    }
                                }
                                else
                                {
                                    clearPath += nextSection.Length;
                                    thisSection = nextSection;
                                    if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
                                    {
                                        SetTrainOutOfControl(OUTOFCONTROL.SLIPPED_TO_ENDOFTRACK);
                                        outOfControl = true;
                                    }
                                }
                            }
                        }
                    }

                    if (outOfControl)
                    {
                        ClearanceAtRearM = -1;
                        RearSignalObject = null;
                    }
                    else
                    {
                        ClearanceAtRearM = clearPath;
                    }
                }
            }
            else
            {
                movedBackward = movedBackward >= 0 ? --movedBackward : movedBackward;
                ClearanceAtRearM = -1;
                RearSignalObject = null;
            }

            return (movedBackward);

        }

        //================================================================================================//
        //
        /// <summary>
        // Check for end of route actions - for PLAYER train only
        // Reverse train if required
        /// </summary>
        //

        public void CheckRouteActions(float elapsedClockSeconds)
        {
            int directionNow = PresentPosition[0].TCDirection;
            int positionNow = PresentPosition[0].TCSectionIndex;

            if (PresentPosition[0].RouteListIndex >= 0) directionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].Direction;

            bool[] nextRoute = UpdateRouteActions(elapsedClockSeconds);
            if (!nextRoute[0]) return;  // not at end of route

            // check if train reversed

            if (nextRoute[1])
            {
                if (positionNow == PresentPosition[0].TCSectionIndex && directionNow != PresentPosition[0].TCDirection)
                {
                    ReverseFormation(true);
                }
                else if (positionNow == PresentPosition[1].TCSectionIndex && directionNow != PresentPosition[1].TCDirection)
                {
                    ReverseFormation(true);
                }
            }

            // check if next station was on previous subpath - if so, move to this subpath

            if (nextRoute[1] && StationStops.Count > 0)
            {
                StationStop thisStation = StationStops[0];
                if (thisStation.SubrouteIndex < TCRoute.activeSubpath)
                {
                    thisStation.SubrouteIndex = TCRoute.activeSubpath;
                }
            }

        }

        //================================================================================================//
        /// <summary>
        /// Check for end of route actions
        /// Called every update, actions depend on route state
        /// returns :
        /// bool[0] "false" end of route not reached
        /// bool[1] "false" if no further route available
        /// </summary>

        public bool[] UpdateRouteActions(float elapsedClockSeconds)
        {

            bool endOfRoute = false;
            bool[] returnState = new bool[2] { false, false };

            // obtain reversal section index

            int reversalSectionIndex = -1;
            if (TCRoute != null && (ControlMode == TRAIN_CONTROL.AUTO_NODE || ControlMode == TRAIN_CONTROL.AUTO_SIGNAL))
            {
                TCReversalInfo thisReversal = TCRoute.ReversalInfo[TCRoute.activeSubpath];
                if (thisReversal.Valid)
                {
                    reversalSectionIndex = thisReversal.SignalUsed ? thisReversal.LastSignalIndex : thisReversal.LastDivergeIndex;
                }
            }

            // can only be performed if train is stationary

            if (Math.Abs(SpeedMpS) > 0.01)
                return (returnState);

            // check position in relation to present end of path

            if (ControlMode == TRAIN_CONTROL.AUTO_NODE &&
                (EndAuthorityType[0] == END_AUTHORITY.END_OF_TRACK ||
                 EndAuthorityType[0] == END_AUTHORITY.END_OF_PATH))
            {
                // front is in last route section
                if (PresentPosition[0].RouteListIndex == (ValidRoute[0].Count - 1))
                {
                    endOfRoute = true;
                }
                // front is within 150m. of end of route and no junctions inbetween (only very short sections ahead of train)
                else
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex];
                    float lengthToGo = thisSection.Length - PresentPosition[0].TCOffset;

                    bool junctionFound = false;
                    for (int iIndex = PresentPosition[0].RouteListIndex + 1; iIndex < ValidRoute[0].Count && !junctionFound; iIndex++)
                    {
                        thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                        junctionFound = thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION;
                        lengthToGo += thisSection.Length;
                    }

                    if (lengthToGo < 150f && !junctionFound)
                    {
                        endOfRoute = true;
                    }
                }
            }

            // other checks unrelated to state
            if (!endOfRoute)
            {
                // if last entry in route is END_OF_TRACK, check against previous entry as this can never be the trains position nor a signal reference section
                int lastValidRouteIndex = ValidRoute[0].Count - 1;
                if (signalRef.TrackCircuitList[ValidRoute[0][lastValidRouteIndex].TCSectionIndex].CircuitType == TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
                    lastValidRouteIndex--;
                
                // if end of train on last section in route - end of route reached

                if (PresentPosition[1].RouteListIndex == lastValidRouteIndex)
                {
                    endOfRoute = true;
                }

                // if waiting for next signal and section in front of signal is last in route - end of route reached

                if (NextSignalObject[0] != null && PresentPosition[0].TCSectionIndex == NextSignalObject[0].TCReference &&
                     NextSignalObject[0].TCReference == ValidRoute[0][lastValidRouteIndex].TCSectionIndex)
                {
                    endOfRoute = true;
                }

                // if waiting for next signal and section beyond signal is last in route and there is no valid reversal index - end of route reached
                if (NextSignalObject[0] != null && PresentPosition[0].TCSectionIndex == NextSignalObject[0].TCReference &&
                     NextSignalObject[0].TCNextTC == ValidRoute[0][lastValidRouteIndex].TCSectionIndex && reversalSectionIndex < 0)
                {
                    endOfRoute = true;
                }

                // if rear of train is beyond reversal section

                else if (reversalSectionIndex >= 0 && PresentPosition[1].RouteListIndex >= reversalSectionIndex)
                {
                    endOfRoute = true;
                }

                // if remaining length less then train length and no junctions to end of route - end of route reached
                // if no junctions or signals to end of route - end of route reached
                else
                {
                    bool intermediateJunction = false;
                    bool intermediateSignal = false;
                    float length = 0f;
                    float distanceToNextJunction = -1f;
                    float distanceToNextSignal   = -1f;

                    if (PresentPosition[1].RouteListIndex >= 0) // end of train is on route
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][PresentPosition[1].RouteListIndex].TCSectionIndex];
                        int direction = ValidRoute[0][PresentPosition[1].RouteListIndex].Direction;
                        length = (thisSection.Length - PresentPosition[1].TCOffset);
                        if (thisSection.EndSignals[direction] != null)                         // check for signal only in direction of train (other signal is behind train)
                        {
                            intermediateSignal = true;
                            distanceToNextSignal = length + Length; // distance is total length plus train length (must be re-compensated)
                        }

                        if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION || thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                        {
                            intermediateJunction = true;
                            distanceToNextJunction = 0f;
                        }

                        for (int iIndex = PresentPosition[1].RouteListIndex + 1; iIndex >= 0 && iIndex <= ValidRoute[0].Count - 1; iIndex++)
                        {
                            thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                            length += thisSection.Length;

                            if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                                thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                            {
                                intermediateJunction = true;
                                distanceToNextJunction = distanceToNextJunction < 0 ? length : distanceToNextJunction;
                            }

                            if (thisSection.EndSignals[direction] != null)
                            {
                                intermediateSignal = true;
                                distanceToNextSignal = distanceToNextSignal < 0 ? length : distanceToNextSignal;
                            }
                            if (thisSection.EndSignals[direction == 1 ? 0 : 1] != null) // check in other direction
                            {
                                intermediateSignal = true;
                                distanceToNextSignal = distanceToNextSignal < 0 ? length - thisSection.Length : distanceToNextSignal; // signal is at start of section
                            }
                        }
                        // check if intermediate junction or signal is valid : only so if there is enough distance (from the front of the train) left for train to pass that junction

                        float frontlength = length - Length;
                        if (intermediateJunction)
                        {
                            if ((frontlength - distanceToNextJunction) < Length) intermediateJunction = false;
                        }

                        if (intermediateSignal)
                        {
                            if ((frontlength - distanceToNextSignal) < Length) intermediateSignal = false;
                        }
                    }
                    else if (PresentPosition[0].RouteListIndex >= 0) // else use front position - check for further signals or junctions only
                    {
                        for (int iIndex = PresentPosition[0].RouteListIndex; iIndex >= 0 && iIndex <= ValidRoute[0].Count - 1; iIndex++)
                        {
                            TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                            int direction = ValidRoute[0][iIndex].Direction;

                            if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                                thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                            {
                                intermediateJunction = true;
                            }

                            if (thisSection.EndSignals[direction] != null)
                            {
                                intermediateSignal = true;
                            }
                        }
                    }

                    // check overall position

                    if (!intermediateJunction && !intermediateSignal)  // no more junctions and no more signal - reverse subpath
                    {
                        endOfRoute = true;
                    }

//                    if (length < Length && !intermediateJunction)  // no more junctions and short track - reverse subpath
//                    {
//                        endOfRoute = true;
//                    }
                }
            }
            // not end of route - no action

            if (!endOfRoute)
            {
                return (returnState);
            }

            // if end of route and waiting point, check or set waiting time (PLAYER only, AI uses station stop)

            if (endOfRoute && TrainType == TRAINTYPE.PLAYER)
            {
                if (waitingPointWaitTimeS > 0)
                {
                    waitingPointWaitTimeS -= elapsedClockSeconds;
                    endOfRoute = false;
                }
                else if (waitingPointWaitTimeS < 0)
                {
                    for (int iWP = 0; iWP <= TCRoute.WaitingPoints.Count - 1; iWP++)
                    {
                        if (TCRoute.WaitingPoints[iWP][0] == TCRoute.activeSubpath)
                        {
                            waitingPointWaitTimeS = TCRoute.WaitingPoints[iWP][2];   // TODO : if absolute time set, use that
                            endOfRoute = false;
                            TCRoute.WaitingPoints[iWP][0] = -1; // invalidate waiting point

                            if (TCRoute.WaitingPoints[iWP][4] > 0) // clear holding signal
                            {
                                if (HoldingSignals.Contains(TCRoute.WaitingPoints[iWP][4])) HoldingSignals.Remove(TCRoute.WaitingPoints[iWP][4]);
                            }
                        }
                    }
                }
            }

            // if next subpath available : check if it can be activated

            bool nextRouteAvailable = false;
            bool nextRouteReady = false;

            TCSubpathRoute nextRoute = null;

            if (endOfRoute && TCRoute.activeSubpath < (TCRoute.TCRouteSubpaths.Count - 1))
            {
                nextRouteAvailable = true;

                nextRoute = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath + 1];
                int firstSectionIndex = PresentPosition[1].TCSectionIndex;

                // find index of present rear position

                int firstRouteIndex = nextRoute.GetRouteIndex(firstSectionIndex, 0);

                // if not found try index of present front position

                if (firstRouteIndex >= 0)
                {
                    nextRouteReady = true;
                }
                else
                {
                    firstSectionIndex = PresentPosition[0].TCSectionIndex;
                    firstRouteIndex = nextRoute.GetRouteIndex(firstSectionIndex, 0);

                    // cant find next part of route - check if really at end of this route, if so, error, else just wait and see (train stopped for other reason)

                    if (PresentPosition[0].RouteListIndex == ValidRoute[0].Count - 1)
                    {
                        if (firstRouteIndex < 0)
                        {
                            Trace.TraceInformation(
                                "Cannot find next part of route (index {0}) for Train {1} (at section {2})",
                                TCRoute.activeSubpath.ToString(), Number.ToString(),
                                PresentPosition[0].TCSectionIndex.ToString());
                        }
                        // search for junction and check if it is not clear

                        else
                        {
                            bool junctionFound = false;
                            bool junctionOccupied = false;

                            for (int iIndex = firstRouteIndex + 1; iIndex < nextRoute.Count && !junctionFound; iIndex++)
                            {
                                int thisSectionIndex = nextRoute[iIndex].TCSectionIndex;
                                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];
                                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                                {
                                    junctionFound = true;
                                    if (thisSection.CircuitState.ThisTrainOccupying(this))
                                    {
                                        junctionOccupied = true;
                                    }
                                }
                            }

                            if (!junctionOccupied)
                            {
                                nextRouteReady = true;
                            }
                        }
                    }
                    else
                    {
                        endOfRoute = false;
                    }
                }
            }

            // if end reached : clear any remaining reservations ahead

            if (endOfRoute && (!nextRouteAvailable || (nextRouteAvailable && nextRouteReady)))
            {
                if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL) // for Auto mode try forward only
                {
                    if (NextSignalObject[0] != null && NextSignalObject[0].enabledTrain == routedForward)
                    {
                        NextSignalObject[0].resetSignalEnabled();
                        int nextRouteIndex = ValidRoute[0].GetRouteIndex(NextSignalObject[0].TCNextTC, 0);

                        // clear rest of route to avoid accidental signal activation
                        if (nextRouteIndex >= 0)
                        {
                            signalRef.BreakDownRouteList(ValidRoute[0], nextRouteIndex, routedForward);
                            ValidRoute[0].RemoveRange(nextRouteIndex, ValidRoute[0].Count - nextRouteIndex);
                        }
                    }

                    if (PresentPosition[0].RouteListIndex >= 0 && PresentPosition[0].RouteListIndex < ValidRoute[0].Count - 1) // not at end of route
                    {
                        int nextRouteIndex = PresentPosition[0].RouteListIndex + 1;
                        signalRef.BreakDownRouteList(ValidRoute[0], nextRouteIndex, routedForward);
                        ValidRoute[0].RemoveRange(nextRouteIndex, ValidRoute[0].Count - nextRouteIndex);
                    }
                }

#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt",
                                "Train " + Number.ToString() + " at end of path\n");
#endif
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                                    "Train " + Number.ToString() + " at end of path\n");
                }


                int nextIndex = PresentPosition[0].RouteListIndex + 1;
                if (nextIndex <= (ValidRoute[0].Count - 1))
                {
                    signalRef.BreakDownRoute(ValidRoute[0][nextIndex].TCSectionIndex, routedForward);
                }
            }

            // if next route available : reverse train, reset and reinitiate signals

            if (endOfRoute && nextRouteAvailable && nextRouteReady)
            {

                // check if reverse is required

                int newIndex = nextRoute.GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
                if (newIndex < 0)
                {
                    newIndex = nextRoute.GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
                }

                if (ValidRoute[0][PresentPosition[0].RouteListIndex].Direction != nextRoute[newIndex].Direction)
                {

                    // set new train positions and reset distance travelled

                    TCPosition tempPosition = new TCPosition();
                    PresentPosition[0].CopyTo(ref tempPosition);
                    PresentPosition[1].CopyTo(ref PresentPosition[0]);
                    tempPosition.CopyTo(ref PresentPosition[1]);

                    PresentPosition[0].Reverse(ValidRoute[0][PresentPosition[0].RouteListIndex].Direction, nextRoute, Length, signalRef);
                    PresentPosition[0].CopyTo(ref PreviousPosition[0]);
                    PresentPosition[1].Reverse(ValidRoute[0][PresentPosition[1].RouteListIndex].Direction, nextRoute, 0.0f, signalRef);
                }
                else
                {
                    PresentPosition[0].RouteListIndex = nextRoute.GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
                    PresentPosition[1].RouteListIndex = nextRoute.GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
                    PresentPosition[0].CopyTo(ref PreviousPosition[0]);
                }

                DistanceTravelledM = PresentPosition[0].DistanceTravelledM;

                // perform any remaining actions of type clear section (except sections now occupied)

                ClearSectionItem dummyItem = new ClearSectionItem(0.0f, 0);

#if DEBUG_REPORTS
                int nextSubpath = TCRoute.activeSubpath + 1;
                File.AppendAllText(@"C:\temp\printproc.txt",
                                "Train " + Number.ToString() +
                                " starts subpath " + nextSubpath.ToString() + "\n");
#endif
                if (CheckTrain)
                {
                    int nextSubpathCT = TCRoute.activeSubpath + 1;
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                                    "Train " + Number.ToString() +
                                    " starts subpath " + nextSubpathCT.ToString() + "\n");
                }

                // reset old actions
                ClearActiveSectionItems();

                // set new route
                TCRoute.activeSubpath++;
                ValidRoute[0] = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath];
                TCRoute.SetReversalOffset(Length);

                // clear existing list of occupied track, and build new list
                for (int iSection = OccupiedTrack.Count() - 1; iSection >= 0; iSection--)
                {
                    TrackCircuitSection thisSection = OccupiedTrack[iSection];
                    thisSection.ResetOccupied(this);

                }
                int rearIndex = PresentPosition[1].RouteListIndex;
                int frontIndex = PresentPosition[0].RouteListIndex;

                if (rearIndex < 0) // end of train not on new route
                {
                    TCSubpathRoute tempRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                        PresentPosition[1].TCDirection, Length, false, false, true);

                    for (int iIndex = 0; iIndex < tempRoute.Count; iIndex++)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[tempRoute[iIndex].TCSectionIndex];
                        thisSection.SetOccupied(routedForward);
                    }
                }
                else
                {
                    for (int iIndex = PresentPosition[1].RouteListIndex; iIndex <= PresentPosition[0].RouteListIndex; iIndex++)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                        thisSection.SetOccupied(routedForward);
                    }
                }

                // Check deadlock against all other trains
                CheckDeadlock(ValidRoute[0], Number);


                // reset signal information

                SignalObjectItems.Clear();
                NextSignalObject[0] = null;

                InitializeSignals(true);

                LastReservedSection[0] = PresentPosition[0].TCSectionIndex;
                if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
                {
                    SwitchToNodeControl(PresentPosition[0].TCSectionIndex);
                }
            }

            returnState[0] = endOfRoute;
            returnState[1] = nextRouteAvailable;

            return (returnState);  // return state
        }

        //================================================================================================//
        /// <summary>
        /// Update route clearance ahead of train
        /// Called every update, actions depend on present control state
        /// </summary>

        public void UpdateRouteClearanceAhead(int signalObjectIndex, int backward, float elapsedClockSeconds)
        {
            switch (ControlMode)
            {
                case (TRAIN_CONTROL.AUTO_SIGNAL):
                    {
                        UpdateSignalMode(signalObjectIndex, backward, elapsedClockSeconds);
                        break;
                    }
                case (TRAIN_CONTROL.AUTO_NODE):
                    {
                        UpdateNodeMode();
                        break;
                    }
                case (TRAIN_CONTROL.MANUAL):
                    {
                        break;   // called directly
                    }
                case (TRAIN_CONTROL.OUT_OF_CONTROL):
                    {
                        UpdateOutOfControl();
                        break;
                    }
                case (TRAIN_CONTROL.UNDEFINED):
                    {
                        SwitchToNodeControl(-1);
                        break;
                    }
            }

            // reset signal which we've just passed

            if (signalObjectIndex >= 0)
            {
                SignalObject signalObject = signalRef.SignalObjects[signalObjectIndex];

                //the following is added by JTang, passing a hold signal, will take back control by the system
                if (signalObject.holdState == SignalObject.HOLDSTATE.MANUAL_PASS ||
                    signalObject.holdState == SignalObject.HOLDSTATE.MANUAL_APPROACH) signalObject.holdState = SignalObject.HOLDSTATE.NONE;

                signalObject.resetSignalEnabled();
            }
        }

        //================================================================================================//
        /// <summary>
        /// Perform auto signal mode update
        /// </summary>

        public void UpdateSignalMode(int signalObjectIndex, int backward, float elapsedClockSeconds)
        {
            // in AUTO mode, use forward route only
            // if moving backward, check if slipped passed signal, if so, re-enable signal

            if (backward > backwardThreshold)
            {
                if (NextSignalObject[0] != null && NextSignalObject[0].enabledTrain != routedForward)
                {
                    if (NextSignalObject[0].enabledTrain != null)
                    {
                        NextSignalObject[0].ResetSignal(true);
                    }
                    signalObjectIndex = NextSignalObject[0].thisRef;
                }
            }

            // if signal passed, send request to clear to next signal

            if (signalObjectIndex >= 0)
            {
                SignalObject thisSignal = signalRef.SignalObjects[signalObjectIndex];
                int nextSignalIndex = thisSignal.sigfound[(int)SignalHead.SIGFN.NORMAL];
                if (nextSignalIndex >= 0)
                {
                    SignalObject nextSignal = signalRef.SignalObjects[nextSignalIndex];
                    nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null);
                }
            }

     // check if waiting for signal

            else if (SpeedMpS < Math.Abs(0.1) &&
             NextSignalObject[0] != null &&
             GetNextSignalAspect(0) == SignalHead.SIGASP.STOP &&
                     CheckTrainWaitingForSignal(NextSignalObject[0], 0))
            {
                bool hasClaimed = ClaimState;
                bool DeadlockWait = CheckDeadlockWait(NextSignalObject[0]);

                if (!DeadlockWait) // cannot claim on deadlock to prevent further deadlocks
                {
                    if (CheckStoppedTrains(NextSignalObject[0].signalRoute)) // do not claim when train ahead is stationary or in Manual mode
                    {
                        actualWaitTimeS = standardWaitTimeS;  // allow immediate claim if other train moves
                        ClaimState = false;
                    }
                    else
                    {
                        actualWaitTimeS += elapsedClockSeconds;
                        if (actualWaitTimeS > standardWaitTimeS)
                        {
                            ClaimState = true;
                        }
                    }
                }
                else
                {
                    actualWaitTimeS = 0.0f;
                    ClaimState = false;
                }

                if (hasClaimed && !ClaimState)
                {
                    foreach (TCRouteElement thisElement in NextSignalObject[0].signalRoute)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        thisSection.UnreserveTrain(routedForward, false);
                    }
                }
            }
            else
            {
                actualWaitTimeS = 0.0f;
                ClaimState = false;
            }

        }

        //================================================================================================//
        //
        // Check if train is waiting for a stationary (stopped) train or a train in manual mode
        //

        public bool CheckStoppedTrains(TCSubpathRoute thisRoute)
        {
            foreach (TCRouteElement thisElement in thisRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                foreach (KeyValuePair<TrainRouted, int> thisTrain in thisSection.CircuitState.TrainOccupy)
                {
                    if (thisTrain.Key.Train.SpeedMpS == 0.0f)
                    {
                        return (true);
                    }
                    if (thisTrain.Key.Train.ControlMode == TRAIN_CONTROL.MANUAL)
                    {
                        return (true);
                    }
                }
            }

            return (false);
        }

        //================================================================================================//
        /// <summary>
        /// Check if train is waiting for signal
        /// </summary>

        public bool CheckTrainWaitingForSignal(SignalObject thisSignal, int direction)
        {
            TrainRouted thisRouted = direction == 0 ? routedForward : routedBackward;
            int trainRouteIndex = PresentPosition[direction].RouteListIndex;
            int signalRouteIndex = ValidRoute[direction].GetRouteIndex(thisSignal.TCReference, trainRouteIndex);

            // signal section is not in train route, so train can't be waiting for signal

            if (signalRouteIndex < 0)
            {
                return (false);
            }

            // check if any other trains in section ahead of this train

            int thisSectionIndex = ValidRoute[0][trainRouteIndex].TCSectionIndex;
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

            Dictionary<Train, float> trainAhead = thisSection.TestTrainAhead(this,
                    PresentPosition[0].TCOffset, PresentPosition[0].TCDirection);

            if (trainAhead.Count > 0)
            {
                return (false);
            }

            // check if any other sections inbetween train and signal

            if (trainRouteIndex != signalRouteIndex)
            {
                for (int iIndex = trainRouteIndex + 1; iIndex <= signalRouteIndex; iIndex++)
                {
                    int nextSectionIndex = ValidRoute[0][iIndex].TCSectionIndex;
                    TrackCircuitSection nextSection = signalRef.TrackCircuitList[nextSectionIndex];

                    if (nextSection.CircuitState.HasTrainsOccupying())  // train is ahead - it's not our signal //
                    {
                        return (false);
                    }
                    else if (!nextSection.IsAvailable(this)) // is section really available to us? //

                    // something is wrong - section upto signal is not available - give warning and switch to node control
                    // also reset signal if it was enabled to us
                    {
                        Trace.TraceWarning("Train {0} in Signal control but route to signal not cleared - switching to Node control",
                                Number);

                        File.AppendAllText(@"C:\temp\passtrain.txt",
                                "Train " + Number.ToString() + " in Section : " + PresentPosition[0].TCSectionIndex.ToString() +
                                " = " + PresentPosition[0].RouteListIndex + "\n");
                        File.AppendAllText(@"C:\temp\passtrain.txt",
                                "Signal " + thisSignal.thisRef.ToString() + " in Section : " +
                                thisSignal.TCReference.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\passtrain.txt",
                                "This section : " + nextSection.Index.ToString() + "\n");

                        bool signalFound = false;

                        for (int iSection = PresentPosition[0].RouteListIndex + 1; iSection < ValidRoute[0].Count && !signalFound; iSection++)
                        {
                            TrackCircuitSection printSection = signalRef.TrackCircuitList[ValidRoute[0][iSection].TCSectionIndex];
                            File.AppendAllText(@"C:\temp\passtrain.txt",
                                    "Section : " + printSection.Index.ToString() + "\n");
                            if (printSection.CircuitState.TrainReserved != null)
                            {
                                File.AppendAllText(@"C:\temp\passtrain.txt",
                                    "Reserved : " + printSection.CircuitState.TrainReserved.Train.Number.ToString() + "\n");
                            }
                        }

                        if (thisSignal.enabledTrain == thisRouted)
                        {
                            thisSignal.ResetSignal(true);
                        }
                        SwitchToNodeControl(thisSection.Index);

                        return (false);
                    }
                }
            }

            // we are waiting, but is signal clearance requested ?

            if (thisSignal.enabledTrain == null)
            {
                thisSignal.requestClearSignal(ValidRoute[0], thisRouted, 0, false, null);
            }

            // we are waiting, but is it really our signal ?

            else if (thisSignal.enabledTrain != thisRouted)
            {

                // something is wrong - we are waiting, but it is not our signal - give warning, reset signal and clear route

                Trace.TraceWarning("Train {0} waiting for signal which is enabled to train {1}",
                        Number, thisSignal.enabledTrain.Train.Number);

                // stop other train - switch other train to node control

                Train otherTrain = thisSignal.enabledTrain.Train;
                otherTrain.LastReservedSection[0] = -1;
                if (Math.Abs(otherTrain.SpeedMpS) > 0)
                {
                    otherTrain.ForcedStop("Stopped due to errors in route setting", Number);
                }
                otherTrain.SwitchToNodeControl(-1);

                // reset signal and clear route

                thisSignal.ResetSignal(false);
                thisSignal.requestClearSignal(ValidRoute[0], thisRouted, 0, false, null);
                return (false);   // do not yet set to waiting, signal might clear //
            }

            // signal is in holding list - so not really waiting - but remove from list if held for station stop

            if (thisSignal.holdState == SignalObject.HOLDSTATE.MANUAL_LOCK)
            {
                return (false);
            }
            else if (thisSignal.holdState == SignalObject.HOLDSTATE.STATION_STOP)
            {
                HoldingSignals.Remove(thisSignal.thisRef);
                return (false);
            }

            return (true);  // it is our signal and we are waiting //
        }

        //================================================================================================//
        /// <summary>
        /// Perform auto node mode update
        /// </summary>

        public void UpdateNodeMode()
        {

            // update distance to end of authority

            int lastRouteIndex = ValidRoute[0].GetRouteIndex(LastReservedSection[0], PresentPosition[0].RouteListIndex);

            TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex];
            DistanceToEndNodeAuthorityM[0] = thisSection.Length - PresentPosition[0].TCOffset;

            for (int iSection = PresentPosition[0].RouteListIndex + 1; iSection <= lastRouteIndex; iSection++)
            {
                thisSection = signalRef.TrackCircuitList[ValidRoute[0][iSection].TCSectionIndex];
                DistanceToEndNodeAuthorityM[0] += thisSection.Length;
            }

            // run out of authority : train is out of control

            // TODO : check end of (sub)path
            //        set variable accordingly
            //
            //            if (DistanceToEndNodeAuthorityM < 0.0f)
            //            {
            //                SetTrainOutOfControl(OUTOFCONTROL.OUT_OF_AUTHORITY);
            //                return;
            //            }

            // look maxTimeS or minCheckDistance ahead
            float maxDistance = Math.Max(AllowedMaxSpeedMpS * maxTimeS, minCheckDistanceM);
            if (EndAuthorityType[0] == END_AUTHORITY.MAX_DISTANCE && DistanceToEndNodeAuthorityM[0] > maxDistance)
            {
                return;   // no update required //
            }

            // perform node update - forward only

            signalRef.requestClearNode(routedForward, ValidRoute[0]);
        }

        //================================================================================================//
        /// <summary>
        /// Update section occupy states for manual mode
        /// Note : manual mode has no distance actions so sections must be cleared immediately
        /// </summary>

        public void UpdateSectionStateManual()
        {
            // occupation is set in forward mode only
            // build route from rear to front - before reset occupy so correct switch alignment is used
            TrainRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, Length, false, false, true);

            // save present occupation list

            List<TrackCircuitSection> clearedSections = new List<TrackCircuitSection>();
            for (int iindex = OccupiedTrack.Count - 1; iindex >= 0; iindex--)
            {
                clearedSections.Add(OccupiedTrack[iindex]);
            }

            // first, check for misaligned switch

            foreach (TCRouteElement thisElement in TrainRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                // occupying misaligned switch : reset routes and position
                if (thisSection.Index == MisalignedSwitch[0])
                {
                    // align switch
                    thisSection.alignSwitchPins(MisalignedSwitch[1]);
                    MisalignedSwitch[0] = -1;
                    MisalignedSwitch[1] = -1;

                    // set to out of control
                    SetTrainOutOfControl(OUTOFCONTROL.MISALIGNED_SWITCH);

                    // recalculate track position
                    UpdateTrainPosition();

                    // rebuild this list
                    UpdateSectionStateManual();

                    // exit, as routine has called itself
                    return;
                }
            }

            // if all is well, set track occupied

            OccupiedTrack.Clear();

            foreach (TCRouteElement thisElement in TrainRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                if (clearedSections.Contains(thisSection))
                {
                    thisSection.ResetOccupied(this); // reset occupation if it was occupied
                    clearedSections.Remove(thisSection);  // remove from cleared list
                }

                thisSection.Reserve(routedForward, TrainRoute);  // reserve first to reset switch alignments
                thisSection.SetOccupied(routedForward);
            }

            foreach (TrackCircuitSection exSection in clearedSections)
            {
                exSection.ClearOccupied(this, true); // sections really cleared
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update Manual Mode
        /// </summary>

        public void UpdateManualMode(int signalObjectIndex)
        {
            // check present forward
            TCSubpathRoute newRouteF = CheckManualPath(0, PresentPosition[0], ValidRoute[0], true, ref EndAuthorityType[0],
                ref DistanceToEndNodeAuthorityM[0]);
            ValidRoute[0] = newRouteF;

            // check present reverse
            // reverse present rear position direction to build correct path backwards
            TCPosition tempRear = new TCPosition();
            PresentPosition[1].CopyTo(ref tempRear);
            tempRear.TCDirection = tempRear.TCDirection == 0 ? 1 : 0;
            TCSubpathRoute newRouteR = CheckManualPath(1, tempRear, ValidRoute[1], true, ref EndAuthorityType[1],
                ref DistanceToEndNodeAuthorityM[1]);
            ValidRoute[1] = newRouteR;


            // select valid route

            if (MUDirection == Direction.Forward)
            {
                // use position from other end of section
                float reverseOffset = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex].Length - PresentPosition[1].TCOffset;
                CheckSpeedLimitManual(ValidRoute[1], TrainRoute, reverseOffset, PresentPosition[1].TCOffset, signalObjectIndex, 0);
            }
            else
            {
                TCSubpathRoute tempRoute = new TCSubpathRoute(); // reversed trainRoute
                for (int iindex = TrainRoute.Count - 1; iindex >= 0; iindex--)
                {
                    TCRouteElement thisElement = TrainRoute[iindex];
                    thisElement.Direction = thisElement.Direction == 0 ? 1 : 0;
                    tempRoute.Add(thisElement);
                }
                float reverseOffset = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex].Length - PresentPosition[0].TCOffset;
                CheckSpeedLimitManual(ValidRoute[0], tempRoute, PresentPosition[0].TCOffset, reverseOffset, signalObjectIndex, 1);
            }

            // reset signal

            if (signalObjectIndex >= 0)
            {
                SignalObject thisSignal = signalRef.SignalObjects[signalObjectIndex];
                thisSignal.hasPermission = SignalObject.PERMISSION.DENIED;
                //the following is added by JTang, passing a hold signal, will take back control by the system
                if (thisSignal.holdState == SignalObject.HOLDSTATE.MANUAL_PASS ||
                    thisSignal.holdState == SignalObject.HOLDSTATE.MANUAL_APPROACH) thisSignal.holdState = SignalObject.HOLDSTATE.NONE;

                thisSignal.resetSignalEnabled();
            }

            // get next signal

            // forward
            SignalObject nextSignal = null;
            for (int iindex = 0; iindex < ValidRoute[0].Count && nextSignal == null; iindex++)
            {
                TCRouteElement thisElement = ValidRoute[0][iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                nextSignal = thisSection.EndSignals[thisElement.Direction];
            }

            NextSignalObject[0] = nextSignal;

            // backward
            nextSignal = null;
            for (int iindex = 0; iindex < ValidRoute[1].Count && nextSignal == null; iindex++)
            {
                TCRouteElement thisElement = ValidRoute[1][iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                nextSignal = thisSection.EndSignals[thisElement.Direction];
            }

            NextSignalObject[1] = nextSignal;

            // clear all build up distance actions
            requiredActions.RemovePendingAIActionItems(true);

            // set cabaspect

            SignalHead.SIGASP forwardstate =
                NextSignalObject[0] == null ? SignalHead.SIGASP.UNKNOWN :
                NextSignalObject[0].this_sig_lr(SignalHead.SIGFN.NORMAL);

            SignalHead.SIGASP backwardstate =
                NextSignalObject[1] == null ? SignalHead.SIGASP.UNKNOWN :
                NextSignalObject[1].this_sig_lr(SignalHead.SIGFN.NORMAL);

            CABAspect = MUDirection == Direction.Forward ? forwardstate : backwardstate;
        }


        //================================================================================================//
        /// <summary>
        /// Check Manual Path
        /// <\summary>

        public TCSubpathRoute CheckManualPath(int direction, TCPosition requiredPosition, TCSubpathRoute requiredRoute, bool forward,
            ref END_AUTHORITY endAuthority, ref float endAuthorityDistanceM)
        {
            TrainRouted thisRouted = direction == 0 ? routedForward : routedBackward;

            // create new route or set to existing route

            TCSubpathRoute newRoute = null;

            TCRouteElement thisElement = null;
            TrackCircuitSection thisSection = null;
            int reqDirection = 0;
            float offsetM = 0.0f;
            float totalLengthM = 0.0f;

            if (requiredRoute == null)
            {
                newRoute = new TCSubpathRoute();
            }
            else
            {
                newRoute = requiredRoute;
            }

            // check if train on valid position in route

            int thisRouteIndex = newRoute.GetRouteIndex(requiredPosition.TCSectionIndex, 0);
            if (thisRouteIndex < 0)    // no valid point in route
            {
                if (requiredRoute != null && requiredRoute.Count > 0)  // if route defined, then breakdown route
                {
                    signalRef.BreakDownRouteList(requiredRoute, 0, thisRouted);
                    requiredRoute.Clear();
                }


                // build new route

                List<int> tempSections = new List<int>();
                tempSections = signalRef.ScanRoute(this, requiredPosition.TCSectionIndex, requiredPosition.TCOffset,
                        requiredPosition.TCDirection, forward, minCheckDistanceManualM, true, false,
                        true, false, true, false, false, false, false, IsFreight);

                if (tempSections.Count > 0)
                {

                    // create subpath route

                    int prevSection = -2;    // preset to invalid

                    foreach (int sectionIndex in tempSections)
                    {
                        int sectionDirection = sectionIndex > 0 ? 0 : 1;
                        thisElement = new TCRouteElement(signalRef.TrackCircuitList[Math.Abs(sectionIndex)],
                                sectionDirection, signalRef, prevSection);
                        newRoute.Add(thisElement);
                        prevSection = Math.Abs(sectionIndex);
                    }
                }
            }
            // remove any sections before present position - train has passed over these sections
            else if (thisRouteIndex > 0)
            {
                for (int iindex = thisRouteIndex - 1; iindex >= 0; iindex--)
                {
                    newRoute.RemoveAt(iindex);
                }
            }

            // check if route ends at signal, determine length

            totalLengthM = 0;
            thisSection = signalRef.TrackCircuitList[requiredPosition.TCSectionIndex];
            offsetM = direction == 0 ? requiredPosition.TCOffset : thisSection.Length - requiredPosition.TCOffset;
            bool endWithSignal = false;    // ends with signal at STOP
            bool hasEndSignal = false;     // ends with cleared signal
            int sectionWithSignalIndex = 0;

            for (int iindex = 0; iindex < newRoute.Count && !endWithSignal; iindex++)
            {
                thisElement = newRoute[iindex];

                thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                totalLengthM += (thisSection.Length - offsetM);
                offsetM = 0.0f; // reset offset for further sections

                reqDirection = thisElement.Direction;
                if (thisSection.EndSignals[reqDirection] != null)
                {
                    SignalObject endSignal = thisSection.EndSignals[reqDirection];
                    SignalHead.SIGASP thisAspect = thisSection.EndSignals[reqDirection].this_sig_lr(SignalHead.SIGFN.NORMAL);
                    hasEndSignal = true;

                    if (thisAspect == SignalHead.SIGASP.STOP && endSignal.hasPermission != SignalObject.PERMISSION.GRANTED)
                    {
                        endWithSignal = true;
                        sectionWithSignalIndex = iindex;
                    }
                    else if (endSignal.enabledTrain == null && endSignal.hasFixedRoute) // signal cleared by default - make sure train is set
                    {
                        endSignal.enabledTrain = thisRouted;
                        endSignal.SetDefaultRoute();
                    }
                }
            }

            // check if signal is in last section
            // if not, probably moved forward beyond a signal, so remove all beyond first signal

            if (endWithSignal && sectionWithSignalIndex < newRoute.Count - 1)
            {
                for (int iindex = newRoute.Count - 1; iindex >= sectionWithSignalIndex + 1; iindex--)
                {
                    thisSection = signalRef.TrackCircuitList[newRoute[iindex].TCSectionIndex];
                    thisSection.RemoveTrain(this, true);
                    newRoute.RemoveAt(iindex);
                }
            }

            // if route does not end with signal and is too short, extend

            if (!endWithSignal && totalLengthM < minCheckDistanceManualM)
            {

                float extendedDistanceM = minCheckDistanceManualM - totalLengthM;
                TCRouteElement lastElement = newRoute[newRoute.Count - 1];

                int lastSectionIndex = lastElement.TCSectionIndex;
                TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastSectionIndex];

                int nextSectionIndex = lastSection.Pins[lastElement.OutPin[0], lastElement.OutPin[1]].Link;
                int nextSectionDirection = lastSection.Pins[lastElement.OutPin[0], lastElement.OutPin[1]].Direction;

                // check if last item is non-aligned switch

                MisalignedSwitch[0] = -1;
                MisalignedSwitch[1] = -1;

                TrackCircuitSection nextSection = nextSectionIndex >= 0 ? signalRef.TrackCircuitList[nextSectionIndex] : null;
                if (nextSection != null && nextSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                {
                    if (nextSection.Pins[0, 0].Link != lastSectionIndex &&
                        nextSection.Pins[1, nextSection.JunctionLastRoute].Link != lastSectionIndex)
                    {
                        MisalignedSwitch[0] = nextSection.Index;
                        MisalignedSwitch[1] = lastSectionIndex;
                    }
                }

                List<int> tempSections = new List<int>();

                if (nextSectionIndex >= 0 && MisalignedSwitch[0] < 0)
                {
                    bool reqAutoAlign = hasEndSignal; // auto-align switchs if route is extended from signal

                    tempSections = signalRef.ScanRoute(this, nextSectionIndex, 0,
                            nextSectionDirection, forward, extendedDistanceM, true, reqAutoAlign,
                            true, false, true, false, false, false, false, IsFreight);
                }

                if (tempSections.Count > 0)
                {
                    // add new sections

                    int prevSection = lastElement.TCSectionIndex;

                    foreach (int sectionIndex in tempSections)
                    {
                        thisElement = new Train.TCRouteElement(signalRef.TrackCircuitList[Math.Abs(sectionIndex)],
                                sectionIndex > 0 ? 0 : 1, signalRef, prevSection);
                        newRoute.Add(thisElement);
                        prevSection = Math.Abs(sectionIndex);
                    }
                }
            }

                // if route is too long, remove sections at end

            else if (totalLengthM > minCheckDistanceManualM)
            {
                float remainingLengthM = totalLengthM - signalRef.TrackCircuitList[newRoute[0].TCSectionIndex].Length; // do not count first section
                bool lengthExceeded = remainingLengthM > minCheckDistanceManualM;

                for (int iindex = newRoute.Count - 1; iindex > 1 && lengthExceeded; iindex--)
                {
                    thisElement = newRoute[iindex];
                    thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                    if ((remainingLengthM - thisSection.Length) > minCheckDistanceManualM)
                    {
                        remainingLengthM -= thisSection.Length;
                        newRoute.RemoveAt(iindex);
                    }
                    else
                    {
                        lengthExceeded = false;
                    }
                }
            }

            // route created to signal or max length, now check availability
            // check if other train in first section

            if (newRoute.Count > 0)
            {
                thisElement = newRoute[0];
                thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                reqDirection = forward ? thisElement.Direction : (thisElement.Direction == 0 ? 1 : 0);
                offsetM = direction == 0 ? requiredPosition.TCOffset : thisSection.Length - requiredPosition.TCOffset;

                Dictionary<Train, float> firstTrainInfo = thisSection.TestTrainAhead(this, offsetM, reqDirection);
                if (firstTrainInfo.Count > 0)
                {
                    foreach (KeyValuePair<Train, float> thisTrainAhead in firstTrainInfo)  // there is only one value
                    {
                        endAuthority = END_AUTHORITY.TRAIN_AHEAD;
                        endAuthorityDistanceM = thisTrainAhead.Value;
                        if (!thisSection.CircuitState.ThisTrainOccupying(this)) thisSection.PreReserve(thisRouted);
                    }
                }

                // check route availability
                // reserve sections which are available

                else
                {
                    int lastValidSectionIndex = 0;
                    bool isAvailable = true;
                    totalLengthM = 0;

                    for (int iindex = 0; iindex < newRoute.Count && isAvailable; iindex++)
                    {
                        thisSection = signalRef.TrackCircuitList[newRoute[iindex].TCSectionIndex];

                        if (isAvailable)
                        {
                            if (thisSection.IsAvailable(this))
                            {
                                lastValidSectionIndex = iindex;
                                totalLengthM += (thisSection.Length - offsetM);
                                offsetM = 0;
                                thisSection.Reserve(thisRouted, newRoute);
                            }
                            else
                            {
                                isAvailable = false;
                            }
                        }
                    }

                    // set default authority to max distance
                    endAuthority = END_AUTHORITY.MAX_DISTANCE;
                    endAuthorityDistanceM = totalLengthM;

                    // if last section ends with signal, set authority to signal
                    thisElement = newRoute[lastValidSectionIndex];
                    thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    reqDirection = forward ? thisElement.Direction : (thisElement.Direction == 0 ? 1 : 0);
                    // last section ends with signal
                    if (thisSection.EndSignals[reqDirection] != null)
                    {
                        endAuthority = END_AUTHORITY.SIGNAL;
                        endAuthorityDistanceM = totalLengthM;
                    }

                // sections not clear - check if end has signal

                    else
                    {

                        TrackCircuitSection nextSection = null;
                        TCRouteElement nextElement = null;

                        if (lastValidSectionIndex < newRoute.Count - 1)
                        {
                            nextElement = newRoute[lastValidSectionIndex + 1];
                            nextSection = signalRef.TrackCircuitList[nextElement.TCSectionIndex];
                        }

                        // check for end authority if not ended with signal
                        // last section is end of track
                        if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
                        {
                            endAuthority = END_AUTHORITY.END_OF_TRACK;
                            endAuthorityDistanceM = totalLengthM;
                        }

                        // first non-available section is switch or crossover
                        else if (nextSection != null && (nextSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                                     nextSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER))
                        {
                            endAuthority = END_AUTHORITY.RESERVED_SWITCH;
                            endAuthorityDistanceM = totalLengthM;
                        }

                        // set authority is end of path unless train ahead
                        else
                        {
                            endAuthority = END_AUTHORITY.END_OF_PATH;
                            endAuthorityDistanceM = totalLengthM;

                            // check if train ahead not moving in opposite direction, in first non-available section

                            if (nextSection != null)
                            {
                                int oppositeDirection = forward ? (nextElement.Direction == 0 ? 1 : 0) : (nextElement.Direction == 0 ? 0 : 1);
                                reqDirection = forward ? nextElement.Direction : (nextElement.Direction == 0 ? 1 : 0);

                                bool oppositeTrain = nextSection.CircuitState.HasTrainsOccupying(oppositeDirection, false);

                                if (!oppositeTrain)
                                {
                                    Dictionary<Train, float> nextTrainInfo = nextSection.TestTrainAhead(this, 0.0f, reqDirection);
                                    if (nextTrainInfo.Count > 0)
                                    {
                                        foreach (KeyValuePair<Train, float> thisTrainAhead in nextTrainInfo)  // there is only one value
                                        {
                                            endAuthority = END_AUTHORITY.TRAIN_AHEAD;
                                            endAuthorityDistanceM = thisTrainAhead.Value + totalLengthM;
                                            lastValidSectionIndex++;
                                            nextSection.PreReserve(thisRouted);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // remove invalid sections from route
                    if (lastValidSectionIndex < newRoute.Count - 1)
                    {
                        for (int iindex = newRoute.Count - 1; iindex > lastValidSectionIndex; iindex--)
                        {
                            newRoute.RemoveAt(iindex);
                        }
                    }
                }
            }

            // no valid route could be found
            else
            {
                endAuthority = END_AUTHORITY.NO_PATH_RESERVED;
                endAuthorityDistanceM = 0.0f;
            }

            return (newRoute);
        }

        //================================================================================================//
        /// <summary>
        /// Restore Manual Mode
        /// </summary>

        public void RestoreManualMode()
        {
            // get next signal

            // forward
            SignalObject nextSignal = null;
            for (int iindex = 0; iindex < ValidRoute[0].Count && nextSignal == null; iindex++)
            {
                TCRouteElement thisElement = ValidRoute[0][iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                nextSignal = thisSection.EndSignals[thisElement.Direction];
            }

            NextSignalObject[0] = nextSignal;

            // backward
            nextSignal = null;
            for (int iindex = 0; iindex < ValidRoute[1].Count && nextSignal == null; iindex++)
            {
                TCRouteElement thisElement = ValidRoute[1][iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                nextSignal = thisSection.EndSignals[thisElement.Direction];
            }

            NextSignalObject[1] = nextSignal;

            // set cabaspect

            SignalHead.SIGASP forwardstate =
                NextSignalObject[0] == null ? SignalHead.SIGASP.UNKNOWN :
                NextSignalObject[0].this_sig_lr(SignalHead.SIGFN.NORMAL);

            SignalHead.SIGASP backwardstate =
                NextSignalObject[1] == null ? SignalHead.SIGASP.UNKNOWN :
                NextSignalObject[1].this_sig_lr(SignalHead.SIGFN.NORMAL);

            CABAspect = MUDirection == Direction.Forward ? forwardstate : backwardstate;
        }


        //================================================================================================//
        //
        // Request signal permission in manual mode
        //

        public void RequestManualSignalPermission(ref TCSubpathRoute selectedRoute, int routeIndex)
        {

            // check if route ends with signal at danger

            TCRouteElement lastElement = selectedRoute[selectedRoute.Count - 1];
            TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastElement.TCSectionIndex];

            // no signal in required direction at end of path

            if (lastSection.EndSignals[lastElement.Direction] == null)
            {
                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Message(ConfirmLevel.Information, "No signal in train's path");
                return;
            }

            SignalObject requestedSignal = lastSection.EndSignals[lastElement.Direction];
            if (requestedSignal.enabledTrain != null && requestedSignal.enabledTrain.Train != this)
            {
                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "Next signal already allocated to other train");
                return;
            }

            requestedSignal.enabledTrain = routeIndex == 0 ? routedForward : routedBackward;
            requestedSignal.signalRoute.Clear();
            requestedSignal.holdState = SignalObject.HOLDSTATE.NONE;
            requestedSignal.hasPermission = SignalObject.PERMISSION.REQUESTED;

            // get route from next signal - extend to next signal or maximum length

            // first, get present length (except first section)

            float totalLengthM = 0;
            for (int iindex = 1; iindex < selectedRoute.Count; iindex++)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[selectedRoute[iindex].TCSectionIndex];
                totalLengthM += thisSection.Length;
            }

            float remainingLengthM =
                Math.Min(minCheckDistanceManualM, Math.Max((minCheckDistanceManualM - totalLengthM), (minCheckDistanceManualM * 0.25f)));

            // get section behind signal

            int nextSectionIndex = lastSection.Pins[lastElement.OutPin[0], lastElement.OutPin[1]].Link;
            int nextSectionDirection = lastSection.Pins[lastElement.OutPin[0], lastElement.OutPin[1]].Direction;

            bool requestValid = false;

            // get route from signal - set remaining length or upto next signal

            if (nextSectionIndex > 0)
            {
                List<int> tempSections = signalRef.ScanRoute(this, nextSectionIndex, 0,
                    nextSectionDirection, true, remainingLengthM, true, true,
                    true, false, true, false, false, false, false, IsFreight);

                // set as signal route

                if (tempSections.Count > 0)
                {
                    int prevSection = -1;

                    foreach (int sectionIndex in tempSections)
                    {
                        TCRouteElement thisElement = new Train.TCRouteElement(signalRef.TrackCircuitList[Math.Abs(sectionIndex)],
                                sectionIndex > 0 ? 0 : 1, signalRef, prevSection);
                        requestedSignal.signalRoute.Add(thisElement);
                        selectedRoute.Add(thisElement);
                        prevSection = Math.Abs(sectionIndex);
                    }

                    requestedSignal.checkRouteState(false, requestedSignal.signalRoute, routedForward);
                    requestValid = true;
                }

                if (!requestValid)
                {
                    if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                        Simulator.Confirmer.Message(ConfirmLevel.Information, "Request to clear signal cannot be processed");
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Process request to set switch in manual mode
        /// Request may contain direction or actual node
        /// </summary>
        public bool ProcessRequestManualSetSwitch(Direction direction)
        {
            // find first switch in required direction

            TrackCircuitSection reqSwitch = null;
            int routeDirectionIndex = direction == Direction.Forward ? 0 : 1;
            bool switchSet = false;

            for (int iindex = 0; iindex < ValidRoute[routeDirectionIndex].Count && reqSwitch == null; iindex++)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[routeDirectionIndex][iindex].TCSectionIndex];
                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                {
                    reqSwitch = thisSection;
                }
            }

            if (reqSwitch == null)
            {
                // search beyond last section for switch using default pins (continue through normal sections only)

                TCRouteElement thisElement = ValidRoute[routeDirectionIndex][ValidRoute[routeDirectionIndex].Count - 1];
                TrackCircuitSection lastSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                int curDirection = thisElement.Direction;
                int nextSectionIndex = thisElement.TCSectionIndex;

                bool validRoute = lastSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL;

                while (reqSwitch == null && validRoute)
                {
                    if (lastSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                    {
                        int outPinIndex = curDirection == 0 ? 1 : 0;
                        if (lastSection.Pins[curDirection, 0].Link == nextSectionIndex)
                        {
                            nextSectionIndex = lastSection.Pins[outPinIndex, 0].Link;
                            curDirection = lastSection.Pins[outPinIndex, 0].Direction;
                        }
                        else if (lastSection.Pins[curDirection, 1].Link == nextSectionIndex)
                        {
                            nextSectionIndex = lastSection.Pins[outPinIndex, 1].Link;
                            curDirection = lastSection.Pins[outPinIndex, 1].Direction;
                        }
                    }
                    else
                    {
                        nextSectionIndex = lastSection.Pins[curDirection, 0].Link;
                        curDirection = lastSection.ActivePins[curDirection, 0].Direction;
                        lastSection = signalRef.TrackCircuitList[nextSectionIndex];
                    }

                    if (lastSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                    {
                        reqSwitch = lastSection;
                    }
                    else if (lastSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.NORMAL)
                    {
                        validRoute = false;
                    }
                }
            }

            if (reqSwitch != null)
            {
                // check if switch is clear
                if (!reqSwitch.CircuitState.HasTrainsOccupying() && reqSwitch.CircuitState.TrainReserved == null && reqSwitch.CircuitState.SignalReserved < 0)
                {
                    reqSwitch.JunctionSetManual = reqSwitch.JunctionLastRoute == 0 ? 1 : 0;
                    signalRef.setSwitch(reqSwitch.OriginalIndex, reqSwitch.JunctionSetManual, reqSwitch);
                    switchSet = true;
                }
                // check if switch reserved by this train - if so, dealign
                else if (reqSwitch.CircuitState.TrainReserved != null && reqSwitch.CircuitState.TrainReserved.Train == this)
                {
                    reqSwitch.deAlignSwitchPins();
                    reqSwitch.JunctionSetManual = reqSwitch.JunctionLastRoute == 0 ? 1 : 0;
                    signalRef.setSwitch(reqSwitch.OriginalIndex, reqSwitch.JunctionSetManual, reqSwitch);
                    switchSet = true;
                }

                if (switchSet)
                    ProcessManualSwitch(routeDirectionIndex, reqSwitch, direction);
            }
            else
            {
                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "No switch found");
            }

            return (switchSet);
        }

        public bool ProcessRequestManualSetSwitch(int reqSwitchIndex)
        {
            // find switch in route - forward first

            int routeDirectionIndex = -1;
            bool switchFound = false;
            Direction direction = Direction.N;

            for (int iindex = 0; iindex < ValidRoute[0].Count - 1 && !switchFound; iindex++)
            {
                if (ValidRoute[0][iindex].TCSectionIndex == reqSwitchIndex)
                {
                    routeDirectionIndex = 0;
                    direction = Direction.Forward;
                    switchFound = true;
                }
            }

            for (int iindex = 0; iindex < ValidRoute[1].Count - 1 && !switchFound; iindex++)
            {
                if (ValidRoute[1][iindex].TCSectionIndex == reqSwitchIndex)
                {
                    routeDirectionIndex = 1;
                    direction = Direction.Reverse;
                    switchFound = true;
                }
            }

            if (switchFound)
            {
                TrackCircuitSection reqSwitch = signalRef.TrackCircuitList[reqSwitchIndex];
                ProcessManualSwitch(routeDirectionIndex, reqSwitch, direction);
                return (true);
            }

            return (false);
        }

        //================================================================================================//
        /// <summary>
        /// Process switching of manual switch
        /// </summary>

        public void ProcessManualSwitch(int routeDirectionIndex, TrackCircuitSection switchSection, Direction direction)
        {
            TrainRouted thisRouted = direction == Direction.Reverse ? routedForward : routedBackward;
            TCSubpathRoute selectedRoute = ValidRoute[routeDirectionIndex];

            // store required position
            int reqSwitchPosition = switchSection.JunctionSetManual;

            // find index of section in present route
            int junctionIndex = selectedRoute.GetRouteIndex(switchSection.Index, 0);

            // check if any signals between train and switch
            List<SignalObject> signalsFound = new List<SignalObject>();

            for (int iindex = 0; iindex < junctionIndex; iindex++)
            {
                TCRouteElement thisElement = selectedRoute[iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                int signalDirection = thisElement.Direction == 0 ? 0 : 1;

                if (thisSection.EndSignals[signalDirection] != null)
                {
                    signalsFound.Add(thisSection.EndSignals[signalDirection]);
                }
            }

            // if any signals found : reset signals

            foreach (SignalObject thisSignal in signalsFound)
            {
                thisSignal.ResetSignal(false);
            }

            // breakdown and clear route

            signalRef.BreakDownRouteList(selectedRoute, 0, thisRouted);
            selectedRoute.Clear();

            // restore required position (is cleared by route breakdown)
            switchSection.JunctionSetManual = reqSwitchPosition;

            // reset indication for misaligned switch
            MisalignedSwitch[0] = -1;
            MisalignedSwitch[1] = -1;

            // build new route

            int routeIndex = -1;

            if (direction == Direction.Forward)
            {
                selectedRoute = CheckManualPath(0, PresentPosition[0], null, true, ref EndAuthorityType[0],
                    ref DistanceToEndNodeAuthorityM[0]);
                routeIndex = 0;

            }
            else
            {
                TCPosition tempRear = new TCPosition();
                PresentPosition[1].CopyTo(ref tempRear);
                tempRear.TCDirection = tempRear.TCDirection == 0 ? 1 : 0;
                selectedRoute = CheckManualPath(1, tempRear, null, true, ref EndAuthorityType[1],
                     ref DistanceToEndNodeAuthorityM[1]);
                routeIndex = 1;
            }

            // if route ends at previously cleared signal, request clear signal again

            TCRouteElement lastElement = selectedRoute[selectedRoute.Count - 1];
            TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastElement.TCSectionIndex];
            int lastDirection = lastElement.Direction == 0 ? 0 : 1;

            SignalObject lastSignal = lastSection.EndSignals[lastDirection];

            while (lastSignal != null && signalsFound.Contains(lastSignal))
            {
                RequestManualSignalPermission(ref selectedRoute, routeIndex);

                lastElement = selectedRoute[selectedRoute.Count - 1];
                lastSection = signalRef.TrackCircuitList[lastElement.TCSectionIndex];
                lastDirection = lastElement.Direction == 0 ? 0 : 1;

                lastSignal = lastSection.EndSignals[lastDirection];
            }

            ValidRoute[routeDirectionIndex] = selectedRoute;
        }

        //================================================================================================//
        /// <summary>
        /// Update speed limit in manual mode
        /// </summary>

        public void CheckSpeedLimitManual(TCSubpathRoute routeBehind, TCSubpathRoute routeUnderTrain, float offsetStart,
            float reverseOffset, int passedSignalIndex, int routeDirection)
        {
            // check backward for last speedlimit in direction of train - raise speed if passed

            TCRouteElement thisElement = routeBehind[0];
            List<int> foundSpeedLimit = new List<int>();

            foundSpeedLimit = signalRef.ScanRoute(this, thisElement.TCSectionIndex, offsetStart, thisElement.Direction,
                    true, -1, false, true, false, false, false, false, false, false, true, IsFreight);

            if (foundSpeedLimit.Count > 0)
            {
                SignalObject speedLimit = signalRef.SignalObjects[Math.Abs(foundSpeedLimit[0])];
                ObjectSpeedInfo thisSpeedInfo = speedLimit.this_lim_speed(SignalHead.SIGFN.SPEED);
                float thisSpeedMpS = IsFreight ? thisSpeedInfo.speed_freight : thisSpeedInfo.speed_pass;

                if (thisSpeedMpS > 0)
                {
                    allowedMaxSpeedLimitMpS = thisSpeedMpS;
                    AllowedMaxSpeedMpS = allowedMaxSpeedLimitMpS;
                }
            }

            // check backward for last signal in direction of train - check with list of pending signal speeds
            // search also checks for speedlimit to see which is nearest train

            foundSpeedLimit.Clear();
            foundSpeedLimit = signalRef.ScanRoute(this, thisElement.TCSectionIndex, offsetStart, thisElement.Direction,
                    true, -1, false, true, false, false, false, false, true, false, true, IsFreight);

            if (foundSpeedLimit.Count > 0)
            {
                SignalObject thisSignal = signalRef.SignalObjects[Math.Abs(foundSpeedLimit[0])];
                if (thisSignal.isSignal)
                {
                    // if signal is now just behind train - set speed as signal speed limit, do not reenter in list
                    if (PassedSignalSpeeds.ContainsKey(thisSignal.thisRef))
                    {
                        allowedMaxSpeedSignalMpS = PassedSignalSpeeds[thisSignal.thisRef];
                        AllowedMaxSpeedMpS = Math.Min(allowedMaxSpeedSignalMpS, AllowedMaxSpeedMpS);
                        LastPassedSignal[routeDirection] = thisSignal.thisRef;
                    }
                    // if signal is not last passed signal - reset signal speed limit
                    else if (thisSignal.thisRef != LastPassedSignal[routeDirection])
                    {
                        allowedMaxSpeedSignalMpS = -1;
                        LastPassedSignal[routeDirection] = -1;
                    }
                    // set signal limit as speed limit
                    else
                    {
                        AllowedMaxSpeedMpS = Math.Min(allowedMaxSpeedSignalMpS, AllowedMaxSpeedMpS);
                    }
                }
            }

            // check forward along train for speedlimit and signal in direction of train - limit speed if passed
            // loop as there might be more than one

            thisElement = routeUnderTrain[0];
            foundSpeedLimit.Clear();
            float remLength = Length;
            Dictionary<int, float> remainingSignals = new Dictionary<int, float>();

            foundSpeedLimit = signalRef.ScanRoute(this, thisElement.TCSectionIndex, reverseOffset, thisElement.Direction,
                    true, remLength, false, true, false, false, false, true, false, true, false, IsFreight);

            bool limitAlongTrain = true;
            while (foundSpeedLimit.Count > 0 && limitAlongTrain)
            {
                SignalObject thisObject = signalRef.SignalObjects[Math.Abs(foundSpeedLimit[0])];

                // check if not beyond end of train
                TrackCircuitSection reqSection = signalRef.TrackCircuitList[thisObject.TCReference];
                float speedLimitDistance = reqSection.GetDistanceBetweenObjects(thisElement.TCSectionIndex, reverseOffset, thisElement.Direction,
                    thisObject.TCReference, thisObject.TCOffset);
                if (speedLimitDistance > Length)
                {
                    limitAlongTrain = false;
                }
                else
                {
                    int nextSectionIndex = thisObject.TCReference;
                    int direction = thisObject.TCDirection;
                    float objectOffset = thisObject.TCOffset;

                    if (thisObject.isSignal)
                    {
                        nextSectionIndex = thisObject.TCNextTC;
                        direction = thisObject.TCNextDirection;
                        objectOffset = 0.0f;

                        if (PassedSignalSpeeds.ContainsKey(thisObject.thisRef))
                        {
                            allowedMaxSpeedSignalMpS = PassedSignalSpeeds[thisObject.thisRef];
                            AllowedMaxSpeedMpS = Math.Min(AllowedMaxSpeedMpS, allowedMaxSpeedSignalMpS);

                            remainingSignals.Add(thisObject.thisRef, allowedMaxSpeedSignalMpS);
                        }
                    }
                    else
                    {
                        ObjectSpeedInfo thisSpeedInfo = thisObject.this_lim_speed(SignalHead.SIGFN.SPEED);
                        float thisSpeedMpS = IsFreight ? thisSpeedInfo.speed_freight : thisSpeedInfo.speed_pass;

                        if (thisSpeedMpS > 0)
                        {
                            allowedMaxSpeedLimitMpS = Math.Min(allowedMaxSpeedLimitMpS, thisSpeedMpS);
                            AllowedMaxSpeedMpS = allowedMaxSpeedLimitMpS;
                        }
                    }

                    remLength -= (thisObject.TCOffset - offsetStart);

                    foundSpeedLimit = signalRef.ScanRoute(this, nextSectionIndex, objectOffset, direction,
                        true, remLength, false, true, false, false, false, true, false, true, false, IsFreight);
                }
            }

            // set list of remaining signals as new pending list
            PassedSignalSpeeds.Clear();
            foreach (KeyValuePair<int, float> thisPair in remainingSignals)
            {
                PassedSignalSpeeds.Add(thisPair.Key, thisPair.Value);
            }

            // check if signal passed posed a speed limit lower than present limit

            if (passedSignalIndex >= 0)
            {
                SignalObject passedSignal = signalRef.SignalObjects[passedSignalIndex];
                ObjectSpeedInfo thisSpeedInfo = passedSignal.this_sig_speed(SignalHead.SIGFN.NORMAL);

                if (thisSpeedInfo != null)
                {
                    float thisSpeedMpS = IsFreight ? thisSpeedInfo.speed_freight : thisSpeedInfo.speed_pass;
                    if (thisSpeedMpS > 0)
                    {
                        allowedMaxSpeedSignalMpS = allowedMaxSpeedSignalMpS > 0 ? Math.Min(allowedMaxSpeedSignalMpS, thisSpeedMpS) : thisSpeedMpS;
                        AllowedMaxSpeedMpS = Math.Min(AllowedMaxSpeedMpS, allowedMaxSpeedSignalMpS);

                        PassedSignalSpeeds.Add(passedSignal.thisRef, thisSpeedMpS);
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update section occupy states fore explorer mode
        /// Note : explorer mode has no distance actions so sections must be cleared immediately
        /// </summary>

        public void UpdateSectionStateExplorer()
        {
            // occupation is set in forward mode only
            // build route from rear to front - before reset occupy so correct switch alignment is used
            TrainRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, Length, false, false, true);

            // save present occupation list

            List<TrackCircuitSection> clearedSections = new List<TrackCircuitSection>();
            for (int iindex = OccupiedTrack.Count - 1; iindex >= 0; iindex--)
            {
                clearedSections.Add(OccupiedTrack[iindex]);
            }

            // first check for misaligned switch

            foreach (TCRouteElement thisElement in TrainRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                // occupying misaligned switch : reset routes and position
                if (thisSection.Index == MisalignedSwitch[0])
                {
                    // align switch
                    thisSection.alignSwitchPins(MisalignedSwitch[1]);
                    MisalignedSwitch[0] = -1;
                    MisalignedSwitch[1] = -1;

                    // recalculate track position
                    UpdateTrainPosition();

                    // rebuild this list
                    UpdateSectionStateExplorer();

                    // exit, as routine has called itself
                    return;
                }
            }

            // if all is well, set tracks to occupied

            OccupiedTrack.Clear();

            foreach (TCRouteElement thisElement in TrainRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                if (clearedSections.Contains(thisSection))
                {
                    thisSection.ResetOccupied(this); // reset occupation if it was occupied
                    clearedSections.Remove(thisSection);  // remove from cleared list
                }

                thisSection.Reserve(routedForward, TrainRoute);  // reserve first to reset switch alignments
                thisSection.SetOccupied(routedForward);
            }

            foreach (TrackCircuitSection exSection in clearedSections)
            {
                exSection.ClearOccupied(this, true); // sections really cleared
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update Explorer Mode
        /// </summary>

        public void UpdateExplorerMode(int signalObjectIndex)
        {
            // check present forward
            TCSubpathRoute newRouteF = CheckExplorerPath(0, PresentPosition[0], ValidRoute[0], true, ref EndAuthorityType[0],
                ref DistanceToEndNodeAuthorityM[0]);
            ValidRoute[0] = newRouteF;

            // check present reverse
            // reverse present rear position direction to build correct path backwards
            TCPosition tempRear = new TCPosition();
            PresentPosition[1].CopyTo(ref tempRear);
            tempRear.TCDirection = tempRear.TCDirection == 0 ? 1 : 0;
            TCSubpathRoute newRouteR = CheckExplorerPath(1, tempRear, ValidRoute[1], true, ref EndAuthorityType[1],
                ref DistanceToEndNodeAuthorityM[1]);
            ValidRoute[1] = newRouteR;

            // select valid route

            if (MUDirection == Direction.Forward)
            {
                // use position from other end of section
                float reverseOffset = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex].Length - PresentPosition[1].TCOffset;
                CheckSpeedLimitManual(ValidRoute[1], TrainRoute, reverseOffset, PresentPosition[1].TCOffset, signalObjectIndex, 0);
            }
            else
            {
                TCSubpathRoute tempRoute = new TCSubpathRoute(); // reversed trainRoute
                for (int iindex = TrainRoute.Count - 1; iindex >= 0; iindex--)
                {
                    TCRouteElement thisElement = TrainRoute[iindex];
                    thisElement.Direction = thisElement.Direction == 0 ? 1 : 0;
                    tempRoute.Add(thisElement);
                }
                float reverseOffset = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex].Length - PresentPosition[0].TCOffset;
                CheckSpeedLimitManual(ValidRoute[0], tempRoute, PresentPosition[0].TCOffset, reverseOffset, signalObjectIndex, 1);
            }

            // reset signal permission

            if (signalObjectIndex >= 0)
            {
                SignalObject thisSignal = signalRef.SignalObjects[signalObjectIndex];
                thisSignal.hasPermission = SignalObject.PERMISSION.DENIED;
                //the following is added by JTang, passing a hold signal, will take back control by the system
                if (thisSignal.holdState == SignalObject.HOLDSTATE.MANUAL_PASS ||
                    thisSignal.holdState == SignalObject.HOLDSTATE.MANUAL_APPROACH) thisSignal.holdState = SignalObject.HOLDSTATE.NONE;

                thisSignal.resetSignalEnabled();
            }

            // get next signal

            // forward
            SignalObject nextSignal = null;
            for (int iindex = 0; iindex < ValidRoute[0].Count && nextSignal == null; iindex++)
            {
                TCRouteElement thisElement = ValidRoute[0][iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                nextSignal = thisSection.EndSignals[thisElement.Direction];
            }

            NextSignalObject[0] = nextSignal;

            // backward
            nextSignal = null;
            for (int iindex = 0; iindex < ValidRoute[1].Count && nextSignal == null; iindex++)
            {
                TCRouteElement thisElement = ValidRoute[1][iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                nextSignal = thisSection.EndSignals[thisElement.Direction];
            }

            NextSignalObject[1] = nextSignal;

            // clear all build up distance actions
            requiredActions.RemovePendingAIActionItems(true);

            // set cabaspect

            SignalHead.SIGASP forwardstate =
                NextSignalObject[0] == null ? SignalHead.SIGASP.UNKNOWN :
                NextSignalObject[0].this_sig_lr(SignalHead.SIGFN.NORMAL);

            SignalHead.SIGASP backwardstate =
                NextSignalObject[1] == null ? SignalHead.SIGASP.UNKNOWN :
                NextSignalObject[1].this_sig_lr(SignalHead.SIGFN.NORMAL);

            CABAspect = MUDirection == Direction.Forward ? forwardstate : backwardstate;
        }

        //================================================================================================//
        /// <summary>
        /// Check Explorer Path
        /// <\summary>

        public TCSubpathRoute CheckExplorerPath(int direction, TCPosition requiredPosition, TCSubpathRoute requiredRoute, bool forward,
            ref END_AUTHORITY endAuthority, ref float endAuthorityDistanceM)
        {
            TrainRouted thisRouted = direction == 0 ? routedForward : routedBackward;

            // create new route or set to existing route

            TCSubpathRoute newRoute = null;

            TCRouteElement thisElement = null;
            TrackCircuitSection thisSection = null;
            int reqDirection = 0;
            float offsetM = 0.0f;
            float totalLengthM = 0.0f;

            if (requiredRoute == null)
            {
                newRoute = new TCSubpathRoute();
            }
            else
            {
                newRoute = requiredRoute;
            }

            // check if train on valid position in route

            int thisRouteIndex = newRoute.GetRouteIndex(requiredPosition.TCSectionIndex, 0);
            if (thisRouteIndex < 0)    // no valid point in route
            {
                if (requiredRoute != null && requiredRoute.Count > 0)  // if route defined, then breakdown route
                {
                    signalRef.BreakDownRouteList(requiredRoute, 0, thisRouted);
                    requiredRoute.Clear();
                }

                // build new route

                List<int> tempSections = new List<int>();

                tempSections = signalRef.ScanRoute(this, requiredPosition.TCSectionIndex, requiredPosition.TCOffset,
                        requiredPosition.TCDirection, forward, minCheckDistanceM, true, false,
                        false, false, true, false, false, false, false, IsFreight);

                if (tempSections.Count > 0)
                {

                    // create subpath route

                    int prevSection = -2;    // preset to invalid

                    foreach (int sectionIndex in tempSections)
                    {
                        int sectionDirection = sectionIndex > 0 ? 0 : 1;
                        thisElement = new TCRouteElement(signalRef.TrackCircuitList[Math.Abs(sectionIndex)],
                                sectionDirection, signalRef, prevSection);
                        newRoute.Add(thisElement);
                        prevSection = Math.Abs(sectionIndex);
                    }
                }
            }
            // remove any sections before present position - train has passed over these sections
            else if (thisRouteIndex > 0)
            {
                for (int iindex = thisRouteIndex - 1; iindex >= 0; iindex--)
                {
                    newRoute.RemoveAt(iindex);
                }
            }

            // check if route ends at signal, determine length

            totalLengthM = 0;
            thisSection = signalRef.TrackCircuitList[requiredPosition.TCSectionIndex];
            offsetM = direction == 0 ? requiredPosition.TCOffset : thisSection.Length - requiredPosition.TCOffset;
            bool endWithSignal = false;    // ends with signal at STOP
            bool hasEndSignal = false;     // ends with cleared signal
            int sectionWithSignalIndex = 0;

            for (int iindex = 0; iindex < newRoute.Count && !endWithSignal; iindex++)
            {
                thisElement = newRoute[iindex];

                thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                totalLengthM += (thisSection.Length - offsetM);
                offsetM = 0.0f; // reset offset for further sections

                reqDirection = thisElement.Direction;
                if (thisSection.EndSignals[reqDirection] != null)
                {
                    SignalObject endSignal = thisSection.EndSignals[reqDirection];
                    SignalHead.SIGASP thisAspect = thisSection.EndSignals[reqDirection].this_sig_lr(SignalHead.SIGFN.NORMAL);
                    hasEndSignal = true;

                    if (thisAspect == SignalHead.SIGASP.STOP && endSignal.hasPermission != SignalObject.PERMISSION.GRANTED)
                    {
                        endWithSignal = true;
                        sectionWithSignalIndex = iindex;
                    }
                }
            }

            // check if signal is in last section
            // if not, probably moved forward beyond a signal, so remove all beyond first signal

            if (endWithSignal && sectionWithSignalIndex < newRoute.Count - 1)
            {
                for (int iindex = newRoute.Count - 1; iindex >= sectionWithSignalIndex + 1; iindex--)
                {
                    thisSection = signalRef.TrackCircuitList[newRoute[iindex].TCSectionIndex];
                    thisSection.RemoveTrain(this, true);
                    newRoute.RemoveAt(iindex);
                }
            }

            // if route does not end with signal and is too short, extend

            if (!endWithSignal && totalLengthM < minCheckDistanceM)
            {

                float extendedDistanceM = minCheckDistanceM - totalLengthM;
                TCRouteElement lastElement = newRoute[newRoute.Count - 1];

                int lastSectionIndex = lastElement.TCSectionIndex;
                TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastSectionIndex];

                int nextSectionIndex = lastSection.Pins[lastElement.OutPin[0], lastElement.OutPin[1]].Link;
                int nextSectionDirection = lastSection.Pins[lastElement.OutPin[0], lastElement.OutPin[1]].Direction;

                // check if last item is non-aligned switch

                MisalignedSwitch[0] = -1;
                MisalignedSwitch[1] = -1;

                TrackCircuitSection nextSection = nextSectionIndex >= 0 ? signalRef.TrackCircuitList[nextSectionIndex] : null;
                if (nextSection != null && nextSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                {
                    if (nextSection.Pins[0, 0].Link != lastSectionIndex &&
                        nextSection.Pins[0, 1].Link != lastSectionIndex &&
                        nextSection.Pins[1, nextSection.JunctionLastRoute].Link != lastSectionIndex)
                    {
                        MisalignedSwitch[0] = nextSection.Index;
                        MisalignedSwitch[1] = lastSectionIndex;
                    }
                }

                List<int> tempSections = new List<int>();

                if (nextSectionIndex >= 0 && MisalignedSwitch[0] < 0)
                {
                    bool reqAutoAlign = hasEndSignal; // auto-align switches if route is extended from signal

                    tempSections = signalRef.ScanRoute(this, nextSectionIndex, 0,
                            nextSectionDirection, forward, extendedDistanceM, true, reqAutoAlign,
                            true, false, true, false, false, false, false, IsFreight);
                }

                if (tempSections.Count > 0)
                {
                    // add new sections

                    int prevSection = lastElement.TCSectionIndex;

                    foreach (int sectionIndex in tempSections)
                    {
                        thisElement = new Train.TCRouteElement(signalRef.TrackCircuitList[Math.Abs(sectionIndex)],
                                sectionIndex > 0 ? 0 : 1, signalRef, prevSection);
                        newRoute.Add(thisElement);
                        prevSection = Math.Abs(sectionIndex);
                    }
                }
            }

                // if route is too long, remove sections at end

            else if (totalLengthM > minCheckDistanceM)
            {
                float remainingLengthM = totalLengthM - signalRef.TrackCircuitList[newRoute[0].TCSectionIndex].Length; // do not count first section
                bool lengthExceeded = remainingLengthM > minCheckDistanceM;

                for (int iindex = newRoute.Count - 1; iindex > 1 && lengthExceeded; iindex--)
                {
                    thisElement = newRoute[iindex];
                    thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                    if ((remainingLengthM - thisSection.Length) > minCheckDistanceM)
                    {
                        remainingLengthM -= thisSection.Length;
                        newRoute.RemoveAt(iindex);
                    }
                    else
                    {
                        lengthExceeded = false;
                    }
                }
            }

            // check for any uncleared signals in route - if first found, request clear signal

            bool unclearedSignal = false;
            int signalIndex = newRoute.Count - 1;
            int nextUnclearSignalIndex = -1;

            for (int iindex = 0; iindex <= newRoute.Count - 1 && !unclearedSignal; iindex++)
            {
                thisElement = newRoute[iindex];
                thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                SignalObject nextSignal = thisSection.EndSignals[thisElement.Direction];
                if (nextSignal != null &&
                    nextSignal.this_sig_lr(SignalHead.SIGFN.NORMAL) == SignalHead.SIGASP.STOP &&
                    nextSignal.hasPermission != SignalObject.PERMISSION.GRANTED)
                {
                    unclearedSignal = true;
                    signalIndex = iindex;
                    nextUnclearSignalIndex = nextSignal.thisRef;
                }
            }

            // route created to signal or max length, now check availability - but only up to first unclear signal
            // check if other train in first section

            if (newRoute.Count > 0)
            {
                thisElement = newRoute[0];
                thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                reqDirection = forward ? thisElement.Direction : (thisElement.Direction == 0 ? 1 : 0);
                offsetM = direction == 0 ? requiredPosition.TCOffset : thisSection.Length - requiredPosition.TCOffset;

                Dictionary<Train, float> firstTrainInfo = thisSection.TestTrainAhead(this, offsetM, reqDirection);
                if (firstTrainInfo.Count > 0)
                {
                    foreach (KeyValuePair<Train, float> thisTrainAhead in firstTrainInfo)  // there is only one value
                    {
                        endAuthority = END_AUTHORITY.TRAIN_AHEAD;
                        endAuthorityDistanceM = thisTrainAhead.Value;
                        if (!thisSection.CircuitState.ThisTrainOccupying(this)) thisSection.PreReserve(thisRouted);
                    }
                }

                // check route availability
                // reserve sections which are available

                else
                {
                    int lastValidSectionIndex = 0;
                    bool isAvailable = true;
                    totalLengthM = 0;

                    for (int iindex = 0; iindex <= signalIndex && isAvailable; iindex++)
                    {
                        thisSection = signalRef.TrackCircuitList[newRoute[iindex].TCSectionIndex];

                        if (isAvailable)
                        {
                            if (thisSection.IsAvailable(this))
                            {
                                lastValidSectionIndex = iindex;
                                totalLengthM += (thisSection.Length - offsetM);
                                offsetM = 0;
                                thisSection.Reserve(thisRouted, newRoute);
                            }
                            else
                            {
                                isAvailable = false;
                            }
                        }
                    }

                    // set default authority to max distance
                    endAuthority = END_AUTHORITY.MAX_DISTANCE;
                    endAuthorityDistanceM = totalLengthM;

                    // if last section ends with signal, set authority to signal
                    thisElement = newRoute[lastValidSectionIndex];
                    thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    reqDirection = forward ? thisElement.Direction : (thisElement.Direction == 0 ? 1 : 0);
                    // last section ends with signal
                    if (thisSection.EndSignals[reqDirection] != null)
                    {
                        endAuthority = END_AUTHORITY.SIGNAL;
                        endAuthorityDistanceM = totalLengthM;
                    }

                // sections not clear - check if end has signal

                    else
                    {

                        TrackCircuitSection nextSection = null;
                        TCRouteElement nextElement = null;

                        if (lastValidSectionIndex < newRoute.Count - 1)
                        {
                            nextElement = newRoute[lastValidSectionIndex + 1];
                            nextSection = signalRef.TrackCircuitList[nextElement.TCSectionIndex];
                        }

                        // check for end authority if not ended with signal
                        // last section is end of track
                        if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
                        {
                            endAuthority = END_AUTHORITY.END_OF_TRACK;
                            endAuthorityDistanceM = totalLengthM;
                        }

                        // first non-available section is switch or crossover
                        else if (nextSection != null && (nextSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                                     nextSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER))
                        {
                            endAuthority = END_AUTHORITY.RESERVED_SWITCH;
                            endAuthorityDistanceM = totalLengthM;
                        }

                        // set authority is end of path unless train ahead
                        else
                        {
                            endAuthority = END_AUTHORITY.END_OF_PATH;
                            endAuthorityDistanceM = totalLengthM;

                            // check if train ahead not moving in opposite direction, in first non-available section

                            if (nextSection != null)
                            {
                                int oppositeDirection = forward ? (nextElement.Direction == 0 ? 1 : 0) : (nextElement.Direction == 0 ? 0 : 1);
                                reqDirection = forward ? nextElement.Direction : (nextElement.Direction == 0 ? 1 : 0);

                                bool oppositeTrain = nextSection.CircuitState.HasTrainsOccupying(oppositeDirection, false);

                                if (!oppositeTrain)
                                {
                                    Dictionary<Train, float> nextTrainInfo = nextSection.TestTrainAhead(this, 0.0f, reqDirection);
                                    if (nextTrainInfo.Count > 0)
                                    {
                                        foreach (KeyValuePair<Train, float> thisTrainAhead in nextTrainInfo)  // there is only one value
                                        {
                                            endAuthority = END_AUTHORITY.TRAIN_AHEAD;
                                            endAuthorityDistanceM = thisTrainAhead.Value + totalLengthM;
                                            lastValidSectionIndex++;
                                            nextSection.PreReserve(thisRouted);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // remove invalid sections from route
                    if (lastValidSectionIndex < newRoute.Count - 1)
                    {
                        for (int iindex = newRoute.Count - 1; iindex > lastValidSectionIndex; iindex--)
                        {
                            newRoute.RemoveAt(iindex);
                        }
                    }
                }

                // check if route ends at signal and this is first unclear signal
                // if so, request clear signal

                if (endAuthority == END_AUTHORITY.SIGNAL)
                {
                    TrackCircuitSection lastSection = signalRef.TrackCircuitList[newRoute[newRoute.Count - 1].TCSectionIndex];
                    int lastDirection = newRoute[newRoute.Count - 1].Direction;
                    if (lastSection.EndSignals[lastDirection] != null && lastSection.EndSignals[lastDirection].thisRef == nextUnclearSignalIndex)
                    {
                        float remainingDistance = minCheckDistanceM - endAuthorityDistanceM;
                        SignalObject reqSignal = signalRef.SignalObjects[nextUnclearSignalIndex];
                        newRoute = reqSignal.requestClearSignalExplorer(newRoute, remainingDistance, forward ? routedForward : routedBackward, false, 0);
                    }
                }
            }

            // no valid route could be found
            else
            {
                endAuthority = END_AUTHORITY.NO_PATH_RESERVED;
                endAuthorityDistanceM = 0.0f;
            }

            return (newRoute);
        }

        //================================================================================================//
        /// <summary>
        /// Restore Explorer Mode
        /// </summary>

        public void RestoreExplorerMode()
        {
            // get next signal

            // forward
            SignalObject nextSignal = null;
            for (int iindex = 0; iindex < ValidRoute[0].Count && nextSignal == null; iindex++)
            {
                TCRouteElement thisElement = ValidRoute[0][iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                nextSignal = thisSection.EndSignals[thisElement.Direction];
            }

            NextSignalObject[0] = nextSignal;

            // backward
            nextSignal = null;
            for (int iindex = 0; iindex < ValidRoute[1].Count && nextSignal == null; iindex++)
            {
                TCRouteElement thisElement = ValidRoute[1][iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                nextSignal = thisSection.EndSignals[thisElement.Direction];
            }

            NextSignalObject[1] = nextSignal;

            // set cabaspect

            SignalHead.SIGASP forwardstate =
                NextSignalObject[0] == null ? SignalHead.SIGASP.UNKNOWN :
                NextSignalObject[0].this_sig_lr(SignalHead.SIGFN.NORMAL);

            SignalHead.SIGASP backwardstate =
                NextSignalObject[1] == null ? SignalHead.SIGASP.UNKNOWN :
                NextSignalObject[1].this_sig_lr(SignalHead.SIGFN.NORMAL);

            CABAspect = MUDirection == Direction.Forward ? forwardstate : backwardstate;
        }


        //================================================================================================//
        //
        // Request signal permission in manual mode
        //

        public void RequestExplorerSignalPermission(ref TCSubpathRoute selectedRoute, int routeIndex)
        {
            // check route for first signal at danger, from present position

            SignalObject reqSignal = null;
            bool signalFound = false;

            if (ValidRoute[routeIndex] != null)
            {
                for (int iIndex = PresentPosition[routeIndex].RouteListIndex; iIndex <= ValidRoute[routeIndex].Count - 1 && !signalFound; iIndex++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[routeIndex][iIndex].TCSectionIndex];
                    int direction = ValidRoute[routeIndex][iIndex].Direction;

                    if (thisSection.EndSignals[direction] != null)
                    {
                        reqSignal = thisSection.EndSignals[direction];
                        signalFound = (reqSignal.this_sig_lr(SignalHead.SIGFN.NORMAL) == SignalHead.SIGASP.STOP);
                    }
                }
            }

            // if no signal at danger is found - report warning
            if (!signalFound)
            {
                if (Simulator.Confirmer != null && this.TrainType != TRAINTYPE.REMOTE) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Message(ConfirmLevel.Information, "No signal in train's path");
                return;
            }

            // signal at danger is found - set PERMISSION REQUESTED, and request clear signal
            // if signal has a route, set PERMISSION REQUESTED, and perform signal update
            reqSignal.hasPermission = SignalObject.PERMISSION.REQUESTED;

            TCPosition tempPos = new TCPosition();

            if (routeIndex == 0)
            {
                PresentPosition[0].CopyTo(ref tempPos);
            }
            else
            {
                PresentPosition[1].CopyTo(ref tempPos);
                tempPos.TCDirection = tempPos.TCDirection == 0 ? 1 : 0;
            }

            TCSubpathRoute newRouteR = CheckExplorerPath(routeIndex, tempPos, ValidRoute[routeIndex], true, ref EndAuthorityType[routeIndex],
                ref DistanceToEndNodeAuthorityM[routeIndex]);
            ValidRoute[routeIndex] = newRouteR;
        }

        //================================================================================================//
        /// <summary>
        /// Process request to set switch in explorer mode
        /// Request may contain direction or actual node
        /// </summary>

        public bool ProcessRequestExplorerSetSwitch(Direction direction)
        {
            // find first switch in required direction

            TrackCircuitSection reqSwitch = null;
            int routeDirectionIndex = direction == Direction.Forward ? 0 : 1;
            bool switchSet = false;

            for (int iindex = 0; iindex < ValidRoute[routeDirectionIndex].Count && reqSwitch == null; iindex++)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[routeDirectionIndex][iindex].TCSectionIndex];
                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                {
                    reqSwitch = thisSection;
                }
            }

            if (reqSwitch == null)
            {
                // search beyond last section for switch using default pins (continue through normal sections only)

                TCRouteElement thisElement = ValidRoute[routeDirectionIndex][ValidRoute[routeDirectionIndex].Count - 1];
                TrackCircuitSection lastSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                int curDirection = thisElement.Direction;
                int nextSectionIndex = thisElement.TCSectionIndex;

                bool validRoute = lastSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL;

                while (reqSwitch == null && validRoute)
                {
                    if (lastSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                    {
                        int outPinIndex = curDirection == 0 ? 1 : 0;
                        if (lastSection.Pins[curDirection, 0].Link == nextSectionIndex)
                        {
                            nextSectionIndex = lastSection.Pins[outPinIndex, 0].Link;
                            curDirection = lastSection.Pins[outPinIndex, 0].Direction;
                        }
                        else if (lastSection.Pins[curDirection, 1].Link == nextSectionIndex)
                        {
                            nextSectionIndex = lastSection.Pins[outPinIndex, 1].Link;
                            curDirection = lastSection.Pins[outPinIndex, 1].Direction;
                        }
                    }
                    else
                    {
                        nextSectionIndex = lastSection.Pins[curDirection, 0].Link;
                        curDirection = lastSection.ActivePins[curDirection, 0].Direction;
                        lastSection = signalRef.TrackCircuitList[nextSectionIndex];
                    }

                    if (lastSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                    {
                        reqSwitch = lastSection;
                    }
                    else if (lastSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.NORMAL)
                    {
                        validRoute = false;
                    }
                }
            }

            if (reqSwitch != null)
            {
                // check if switch is clear
                if (!reqSwitch.CircuitState.HasTrainsOccupying() && reqSwitch.CircuitState.TrainReserved == null && reqSwitch.CircuitState.SignalReserved < 0)
                {
                    reqSwitch.JunctionSetManual = reqSwitch.JunctionLastRoute == 0 ? 1 : 0;
                    signalRef.setSwitch(reqSwitch.OriginalIndex, reqSwitch.JunctionSetManual, reqSwitch);
                    switchSet = true;
                }
                // check if switch reserved by this train - if so, dealign
                else if (reqSwitch.CircuitState.TrainReserved != null && reqSwitch.CircuitState.TrainReserved.Train == this)
                {
                    reqSwitch.deAlignSwitchPins();
                    reqSwitch.JunctionSetManual = reqSwitch.JunctionLastRoute == 0 ? 1 : 0;
                    signalRef.setSwitch(reqSwitch.OriginalIndex, reqSwitch.JunctionSetManual, reqSwitch);
                    switchSet = true;
                }

                if (switchSet)
                    ProcessExplorerSwitch(routeDirectionIndex, reqSwitch, direction);
            }
            else
            {
                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "No switch found");
            }

            return (switchSet);
        }

        public bool ProcessRequestExplorerSetSwitch(int reqSwitchIndex)
        {
            // find switch in route - forward first

            int routeDirectionIndex = -1;
            bool switchFound = false;
            Direction direction = Direction.N;

            for (int iindex = 0; iindex < ValidRoute[0].Count - 1 && !switchFound; iindex++)
            {
                if (ValidRoute[0][iindex].TCSectionIndex == reqSwitchIndex)
                {
                    routeDirectionIndex = 0;
                    direction = Direction.Forward;
                    switchFound = true;
                }
            }

            for (int iindex = 0; iindex < ValidRoute[1].Count - 1 && !switchFound; iindex++)
            {
                if (ValidRoute[1][iindex].TCSectionIndex == reqSwitchIndex)
                {
                    routeDirectionIndex = 1;
                    direction = Direction.Reverse;
                    switchFound = true;
                }
            }

            if (switchFound)
            {
                TrackCircuitSection reqSwitch = signalRef.TrackCircuitList[reqSwitchIndex];
                ProcessExplorerSwitch(routeDirectionIndex, reqSwitch, direction);
                return (true);
            }

            return (false);
        }

        //================================================================================================//
        /// <summary>
        /// Process switching of explorer switch
        /// </summary>

        public void ProcessExplorerSwitch(int routeDirectionIndex, TrackCircuitSection switchSection, Direction direction)
        {
            TrainRouted thisRouted = direction == Direction.Reverse ? routedForward : routedBackward;
            TCSubpathRoute selectedRoute = ValidRoute[routeDirectionIndex];

            // store required position
            int reqSwitchPosition = switchSection.JunctionSetManual;

            // find index of section in present route
            int junctionIndex = selectedRoute.GetRouteIndex(switchSection.Index, 0);
            int lastIndex = junctionIndex - 1; // set previous index as last valid index

            // find first signal from train and before junction
            SignalObject firstSignal = null;
            float coveredLength = 0;

            for (int iindex = 0; iindex < junctionIndex && firstSignal == null; iindex++)
            {
                TCRouteElement thisElement = selectedRoute[iindex];
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                if (iindex > 0) coveredLength += thisSection.Length; // do not use first section

                int signalDirection = thisElement.Direction == 0 ? 0 : 1;

                if (thisSection.EndSignals[signalDirection] != null &&
                    thisSection.EndSignals[signalDirection].enabledTrain != null &&
                    thisSection.EndSignals[signalDirection].enabledTrain.Train == this)
                {
                    firstSignal = thisSection.EndSignals[signalDirection];
                    lastIndex = iindex;
                }
            }

            // if last first is found : reset signal and further signals, clear route as from signal and request clear signal

            if (firstSignal != null)
            {
                firstSignal.ResetSignal(true);

                // breakdown and clear route

                signalRef.BreakDownRouteList(selectedRoute, lastIndex + 1, thisRouted);
                selectedRoute.RemoveRange(lastIndex + 1, selectedRoute.Count - lastIndex - 1);

                // restore required position (is cleared by route breakdown)
                switchSection.JunctionSetManual = reqSwitchPosition;

                // build new route - use signal request
                float remLength = minCheckDistanceM - coveredLength;
                TCSubpathRoute newRoute = firstSignal.requestClearSignalExplorer(selectedRoute, remLength, thisRouted, false, 0);
                selectedRoute = newRoute;
            }

            // no signal is found - build route using full update process
            else
            {
                signalRef.BreakDownRouteList(selectedRoute, 0, thisRouted);
                selectedRoute.Clear();
                UpdateExplorerMode(-1);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update out-of-control mode
        /// </summary>

        public void UpdateOutOfControl()
        {

            // if train not yet at a stand, keep applying emergency break

            if (SpeedMpS > Math.Abs(0.1f))
            {
                if (LeadLocomotive != null)
                    ((MSTSLocomotive)LeadLocomotive).SetEmergency();
            }

            // train is at a stand : 
            // clear all occupied blocks
            // clear signal/speedpost list 
            // clear DistanceTravelledActions 
            // clear all previous occupied sections 
            // set sections occupied on which train stands

            else
            {
                // all the above is still TODO
            }
        }

        //================================================================================================//
        /// <summary>
        /// Switch to Auto Signal mode
        /// </summary>

        public virtual void SwitchToSignalControl(SignalObject thisSignal)
        {
            // in auto mode, use forward direction only

            ControlMode = TRAIN_CONTROL.AUTO_SIGNAL;
            thisSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null);
        }

        //================================================================================================//
        /// <summary>
        /// Switch to Auto Node mode
        /// </summary>

        public virtual void SwitchToNodeControl(int thisSectionIndex)
        {
            // use direction forward only
            float maxDistance = Math.Max(AllowedMaxSpeedMpS * maxTimeS, minCheckDistanceM);
            float clearedDistanceM = 0.0f;

            int activeSectionIndex = thisSectionIndex;
            int endListIndex = -1;

            ControlMode = TRAIN_CONTROL.AUTO_NODE;
            EndAuthorityType[0] = END_AUTHORITY.NO_PATH_RESERVED;
            IndexNextSignal = -1; // no next signal in Node Control

            // if section is set, check if it is on route and ahead of train

            if (activeSectionIndex > 0)
            {
                endListIndex = ValidRoute[0].GetRouteIndex(thisSectionIndex, PresentPosition[0].RouteListIndex);

                // section is not on route - give warning and break down route, following active links and resetting reservation

                if (endListIndex < 0)
                {
                    signalRef.BreakDownRoute(thisSectionIndex, routedForward);
                    activeSectionIndex = -1;
                }

                // if section is (still) set, check if this is at maximum distance

                if (activeSectionIndex > 0)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[activeSectionIndex];
                    clearedDistanceM = GetDistanceToTrain(activeSectionIndex, thisSection.Length);

                    if (clearedDistanceM > maxDistance)
                    {
                        EndAuthorityType[0] = END_AUTHORITY.MAX_DISTANCE;
                        LastReservedSection[0] = thisSection.Index;
                        DistanceToEndNodeAuthorityM[0] = clearedDistanceM;
                    }
                }
                else
                {
                    EndAuthorityType[0] = END_AUTHORITY.NO_PATH_RESERVED;
                }
            }

            // new request or not beyond max distance

            if (activeSectionIndex < 0 || EndAuthorityType[0] != END_AUTHORITY.MAX_DISTANCE)
            {
                signalRef.requestClearNode(routedForward, ValidRoute[0]);
            }
        }

        //================================================================================================//
        //
        // Request to switch to or from manual mode
        //

        public void RequestToggleManualMode()
        {
            if (ControlMode == TRAIN_CONTROL.MANUAL)
            {
                // check if train is back on path

                TCSubpathRoute lastRoute = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath];
                int routeIndex = lastRoute.GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);

                if (routeIndex < 0)
                {
                    if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                        Simulator.Confirmer.Message(ConfirmLevel.Warning, "Train is not back on original route");
                }
                else
                {
                    int lastDirection = lastRoute[routeIndex].Direction;
                    int presentDirection = PresentPosition[0].TCDirection;
                    if (lastDirection != presentDirection && Math.Abs(SpeedMpS) > 0.1f)
                    {
                        if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                            Simulator.Confirmer.Message(ConfirmLevel.Warning, "Original route is reverse from present direction, stop train before switching");
                    }
                    else
                    {
                        ToggleFromManualMode(routeIndex);
                    }
                }

            }
            else if (ControlMode == TRAIN_CONTROL.EXPLORER)
            {
                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "Cannot change to Manual Mode while in Explorer Mode");
            }
            else
            {
                if (Math.Abs(SpeedMpS) > 0.1f)
                {
                    if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                        Simulator.Confirmer.Message(ConfirmLevel.Warning, "Train must be at standstill before switching Mode");
                }
                else
                {
                    ToggleToManualMode();
                }
            }
        }

        //================================================================================================//
        //
        // Switch to manual mode
        //

        public void ToggleToManualMode()
        {

            // set track occupation (using present route)
            UpdateSectionStateManual();

            // breakdown present route - both directions if set

            if (ValidRoute[0] != null)
            {
                int listIndex = PresentPosition[0].RouteListIndex;
                signalRef.BreakDownRouteList(ValidRoute[0], listIndex, routedForward);
            }
            ValidRoute[0] = null;
            LastReservedSection[0] = -1;

            if (ValidRoute[1] != null)
            {
                int listIndex = PresentPosition[1].RouteListIndex;
                signalRef.BreakDownRouteList(ValidRoute[1], listIndex, routedBackward);
            }
            ValidRoute[1] = null;
            LastReservedSection[1] = -1;

            // clear all outstanding actions

            ClearActiveSectionItems();
            requiredActions.RemovePendingAIActionItems(true);

            // clear signal info

            NextSignalObject[0] = null;
            NextSignalObject[1] = null;

            SignalObjectItems.Clear();

            PassedSignalSpeeds.Clear();

            // set manual mode

            ControlMode = TRAIN_CONTROL.MANUAL;

            // reset routes and check sections either end of train

            PresentPosition[0].RouteListIndex = -1;
            PresentPosition[1].RouteListIndex = -1;
            PreviousPosition[0].RouteListIndex = -1;

            UpdateManualMode(-1);
        }

        //================================================================================================//
        //
        // Switch back from manual mode
        //

        public void ToggleFromManualMode(int routeIndex)
        {
            // extract route at present front position

            TCSubpathRoute newRoute = new TCSubpathRoute();
            TCSubpathRoute oldRoute = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath];

            // test on reversal, if so check rear of train

            bool reversal = false;
            if (!CheckReversal(oldRoute, ref reversal))
            {
                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, "Reversal required and rear of train not on required route");
                return;
            }

            // breakdown present routes, forward and backward

            signalRef.BreakDownRouteList(ValidRoute[0], 0, routedForward);
            signalRef.BreakDownRouteList(ValidRoute[1], 0, routedBackward);

            // remove any actions build up during manual mode

            requiredActions.RemovePendingAIActionItems(true);

            // restore train placement

            RestoreTrainPlacement(ref newRoute, oldRoute, routeIndex, reversal);

            // restore signal information

            PassedSignalSpeeds.Clear();
            InitializeSignals(true);

            // restore deadlock information

            CheckDeadlock(ValidRoute[0], Number);    // Check deadlock against all other trains

            // switch to AutoNode mode

            LastReservedSection[0] = PresentPosition[0].TCSectionIndex;
            LastReservedSection[1] = PresentPosition[1].TCSectionIndex;
            SwitchToNodeControl(PresentPosition[0].TCSectionIndex);
            TCRoute.SetReversalOffset(Length);
        }

        //================================================================================================//
        //
        // Check if reversal is required
        //

        public bool CheckReversal(TCSubpathRoute reqRoute, ref bool reversal)
        {
            bool valid = true;

            int presentRouteIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
            int reqRouteIndex = reqRoute.GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
            if (presentRouteIndex < 0 || reqRouteIndex < 0)
            {
                valid = false;  // front of train not on present route or not on required route
            }
            // valid point : check if reversal is required
            else
            {
                TCRouteElement presentElement = ValidRoute[0][presentRouteIndex];
                TCRouteElement pathElement = reqRoute[reqRouteIndex];

                if (presentElement.Direction != pathElement.Direction)
                {
                    reversal = true;
                }
            }

            // if reversal required : check if rear of train is on required route
            if (valid && reversal)
            {
                int rearRouteIndex = reqRoute.GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
                valid = rearRouteIndex >= 0;
            }

            return (valid);
        }

        //================================================================================================//
        //
        // Restore train placement
        //

        public void RestoreTrainPlacement(ref TCSubpathRoute newRoute, TCSubpathRoute oldRoute, int frontIndex, bool reversal)
        {
            // reverse train if required

            if (reversal)
            {
                ReverseFormation(true);
            }

            // reset distance travelled

            DistanceTravelledM = 0.0f;

            // check if end of train on original route
            // copy sections from earliest start point (front or rear)

            int rearIndex = oldRoute.GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
            int startIndex = rearIndex >= 0 ? Math.Min(rearIndex, frontIndex) : frontIndex;

            for (int iindex = startIndex; iindex < oldRoute.Count; iindex++)
            {
                newRoute.Add(oldRoute[iindex]);
            }

            // if rear not on route, build route under train and add sections

            if (rearIndex < 0)
            {

                TCSubpathRoute tempRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, Length, false, true, true);

                for (int iindex = tempRoute.Count - 1; iindex >= 0; iindex--)
                {
                    TCRouteElement thisElement = tempRoute[iindex];
                    if (!newRoute.ContainsSection(thisElement))
                    {
                        newRoute.Insert(0, thisElement);
                    }
                }
            }

            // set route as valid route

            ValidRoute[0] = newRoute;

            // get index of first section in route

            rearIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
            PresentPosition[1].RouteListIndex = rearIndex;

            // get index of front of train

            frontIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
            PresentPosition[0].RouteListIndex = frontIndex;

            PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            // set track occupied - forward only

            foreach (TrackCircuitSection thisSection in OccupiedTrack)
            {
                if (!thisSection.CircuitState.ThisTrainOccupying(this))
                {
                    thisSection.Reserve(routedForward, ValidRoute[0]);
                    thisSection.SetOccupied(routedForward);
                }
            }

        }


        //================================================================================================//
        //
        // Request permission to pass signal
        //

        public void RequestSignalPermission(Direction direction)
        {
            if (MPManager.IsClient())
            {
                MPManager.Notify((new MSGResetSignal(MPManager.GetUserName())).ToString());
                return;
            }
            if (ControlMode == TRAIN_CONTROL.MANUAL)
            {
                if (direction == Direction.Forward)
                {
                    RequestManualSignalPermission(ref ValidRoute[0], 0);
                }
                else
                {
                    RequestManualSignalPermission(ref ValidRoute[1], 1);
                }
            }
            else if (ControlMode == TRAIN_CONTROL.EXPLORER)
            {
                if (direction == Direction.Forward)
                {
                    RequestExplorerSignalPermission(ref ValidRoute[0], 0);
                }
                else
                {
                    RequestExplorerSignalPermission(ref ValidRoute[1], 1);
                }
            }
            else
            {
                if (direction != Direction.Forward)
                {
                    if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                        Simulator.Confirmer.Message(ConfirmLevel.Warning, "Cannot clear signal behind train while in AUTO mode");
                }

                else if (NextSignalObject[0] != null)
                {
                    NextSignalObject[0].hasPermission = SignalObject.PERMISSION.REQUESTED;
                }
            }
        }

        //================================================================================================//
        //
        // Request shunt lock on or release
        //

        public void RequestShuntLock(int lockType, int OnOff)
        {

            // TODO
        }

        //================================================================================================//
        /// <summary>
        /// Get distance from train to object position using route list
        /// </summary>

        public float GetObjectDistanceToTrain(ObjectItemInfo thisObject)
        {

            // follow active links to get to object

            int reqSectionIndex = thisObject.ObjectDetails.TCReference;
            float endOffset = thisObject.ObjectDetails.TCOffset;

            float distanceM = GetDistanceToTrain(reqSectionIndex, endOffset);

            //          if (distanceM < 0)
            //          {
            //              distanceM = thisObject.ObjectDetails.DistanceTo(FrontTDBTraveller);
            //          }

            return (distanceM);
        }

        //================================================================================================//
        /// <summary>
        /// Get distance from train to location using route list
        /// TODO : rewrite to use active links, and if fails use traveller
        /// location must have same direction as train
        /// </summary>

        public float GetDistanceToTrain(int sectionIndex, float endOffset)
        {

            // use start of list to see if passed position

            int endListIndex = ValidRoute[0].GetRouteIndex(sectionIndex, PresentPosition[0].RouteListIndex);
            if (endListIndex < 0)
                endListIndex = ValidRoute[0].GetRouteIndex(sectionIndex, 0);

            if (endListIndex >= 0 && endListIndex < PresentPosition[0].RouteListIndex) // index before present so we must have passed object
            {
                return (-1.0f);
            }

            if (endListIndex == PresentPosition[0].RouteListIndex && endOffset < PresentPosition[0].TCOffset) // just passed
            {
                return (-1.0f);
            }

            // section is not on route

            if (endListIndex < 0)
            {
                return (-1.0f);
            }

            int thisSectionIndex = PresentPosition[0].TCSectionIndex;
            int direction = PresentPosition[0].TCDirection;
            float startOffset = PresentPosition[0].TCOffset;
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

            return (thisSection.GetDistanceBetweenObjects(thisSectionIndex, startOffset, direction, sectionIndex, endOffset));
        }

        //================================================================================================//
        /// <summary>
        /// Switch train to Out-of-Control
        /// Set mode and apply emergency brake
        /// </summary>

        public void SetTrainOutOfControl(OUTOFCONTROL reason)
        {

            // clear all reserved sections etc. - both directions
            if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
            {
                if (NextSignalObject[0] != null && NextSignalObject[0].enabledTrain == routedForward)
                {
                    NextSignalObject[0].ResetSignal(true);
                }
                if (NextSignalObject[1] != null && NextSignalObject[1].enabledTrain == routedBackward)
                {
                    NextSignalObject[1].ResetSignal(true);
                }
            }
            else if (ControlMode == TRAIN_CONTROL.AUTO_NODE)
            {
                signalRef.BreakDownRoute(LastReservedSection[0], routedForward);
            }

            // TODO : clear routes for MANUAL

            // set control state and issue warning

            if (ControlMode != TRAIN_CONTROL.EXPLORER)
                ControlMode = TRAIN_CONTROL.OUT_OF_CONTROL;

            String report = "Train ";
            report = String.Concat(report, Number.ToString());
            report = String.Concat(report, " is out of control and will be stopped. Reason : ");

            OutOfControlReason = reason;

            switch (reason)
            {
                case (OUTOFCONTROL.SPAD):
                    report = String.Concat(report, " train passed signal at Danger");
                    break;
                case (OUTOFCONTROL.SPAD_REAR):
                    report = String.Concat(report, " train passed signal at Danger at rear of train");
                    break;
                case (OUTOFCONTROL.OUT_OF_AUTHORITY):
                    report = String.Concat(report, " train passed limit of authority");
                    break;
                case (OUTOFCONTROL.OUT_OF_PATH):
                    report = String.Concat(report, " train has ran off its allocated path");
                    break;
                case (OUTOFCONTROL.SLIPPED_INTO_PATH):
                    report = String.Concat(report, " train slipped back into path of another train");
                    break;
                case (OUTOFCONTROL.SLIPPED_TO_ENDOFTRACK):
                    report = String.Concat(report, " train slipped of the end of track");
                    break;
                case (OUTOFCONTROL.OUT_OF_TRACK):
                    report = String.Concat(report, " train has moved off the track");
                    break;
            }

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", report + "\n");
            }

            if (LeadLocomotive != null)
                ((MSTSLocomotive)LeadLocomotive).SetEmergency();
        }

        //================================================================================================//
        /// <summary>
        /// Perform actions linked to distance travelled
        /// </summary>

        public virtual void PerformActions(List<DistanceTravelledItem> nowActions)
        {
            foreach (var thisAction in nowActions)
            {
                if (thisAction is ClearSectionItem)
                {
                    ClearOccupiedSection(thisAction as ClearSectionItem);
                }
                else if (thisAction is ActivateSpeedLimit)
                {
                    SetPendingSpeedLimit(thisAction as ActivateSpeedLimit);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Clear section
        /// </summary>

        public void ClearOccupiedSection(ClearSectionItem sectionInfo)
        {
            int thisSectionIndex = sectionInfo.TrackSectionIndex;
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

            thisSection.ClearOccupied(this, true);
        }

        //================================================================================================//
        /// <summary>
        /// Set pending speed limits
        /// </summary>

        public void SetPendingSpeedLimit(ActivateSpeedLimit speedInfo)
        {
            float prevMaxSpeedMpS = AllowedMaxSpeedMpS;

            if (speedInfo.MaxSpeedMpSSignal > 0)
            {
                allowedMaxSpeedSignalMpS = speedInfo.MaxSpeedMpSSignal;
                AllowedMaxSpeedMpS = speedInfo.MaxSpeedMpSSignal;
            }
            if (speedInfo.MaxSpeedMpSLimit > 0)
            {
                allowedMaxSpeedLimitMpS = speedInfo.MaxSpeedMpSLimit;
                AllowedMaxSpeedMpS = speedInfo.MaxSpeedMpSLimit;
            }

#if DEBUG_REPORTS
                         File.AppendAllText(@"C:\temp\printproc.txt", "Validated speedlimit : " +
                            "Limit : " + allowedMaxSpeedLimitMpS.ToString() + " ; " +
                            "Signal : " + allowedMaxSpeedSignalMpS.ToString() + " ; " +
                            "Overall : " + AllowedMaxSpeedMpS.ToString() + "\n");

#endif
            if (TrainType == TRAINTYPE.PLAYER && AllowedMaxSpeedMpS > prevMaxSpeedMpS && !Simulator.Confirmer.Viewer.TrackMonitorWindow.Visible && Simulator.Confirmer != null)
            {
                String message = "Allowed speed raised to " + FormatStrings.FormatSpeed(AllowedMaxSpeedMpS, Simulator.Confirmer.Viewer.MilepostUnitsMetric);
                Simulator.Confirmer.Message(ConfirmLevel.Information, message);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Clear all active items on occupied track
        /// <\summary>

        public void ClearActiveSectionItems()
        {
            ClearSectionItem dummyItem = new ClearSectionItem(0.0f, 0);
            List<DistanceTravelledItem> activeActions = requiredActions.GetActions(99999999f, dummyItem.GetType());
            foreach (DistanceTravelledItem thisAction in activeActions)
            {
                if (thisAction is ClearSectionItem)
                {
                    ClearSectionItem sectionInfo = thisAction as ClearSectionItem;
                    int thisSectionIndex = sectionInfo.TrackSectionIndex;
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

                    if (!OccupiedTrack.Contains(thisSection))
                    {
                        thisSection.ClearOccupied(this, true);
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Forced stop due to problems with other train
        /// <\summary>

        public void ForcedStop(String reason, int otherTrainNumber)
        {
            Trace.TraceInformation("Train {0} stopped for train {1} : {2}",
                    Number, otherTrainNumber, reason);

            if (Program.Simulator.PlayerLocomotive != null && Program.Simulator.PlayerLocomotive.Train == this)
            {
                String report = "Train stopped due to problems with other train : train ";
                report = String.Concat(report, otherTrainNumber.ToString());
                report = String.Concat(report, " , reason : ", reason);

                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Message(ConfirmLevel.Warning, report);

#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", report + "\n");
#endif
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", report + "\n");
                }

            }

            if (LeadLocomotive != null)
                ((MSTSLocomotive)LeadLocomotive).SetEmergency();
        }

        //================================================================================================//
        //
        // Remove train (after coupling)
        //

        public void RemoveFromTrack()
        {
            // check if no reserved sections remain

            int presentIndex = PresentPosition[1].RouteListIndex;

            if (presentIndex >= 0)
            {
                for (int iIndex = presentIndex; iIndex < ValidRoute[0].Count; iIndex++)
                {
                    TCRouteElement thisElement = ValidRoute[0][iIndex];
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    thisSection.RemoveTrain(this, true);
                }
            }

            // clear occupied track

            TrackCircuitSection[] tempSectionArray = new TrackCircuitSection[OccupiedTrack.Count]; // copy sections as list is cleared by ClearOccupied method
            OccupiedTrack.CopyTo(tempSectionArray);

            for (int iIndex = 0; iIndex < tempSectionArray.Length; iIndex++)
            {
                TrackCircuitSection thisSection = tempSectionArray[iIndex];
                thisSection.ClearOccupied(this, true);
            }

            // clear outstanding clear sections

            foreach (DistanceTravelledItem thisAction in requiredActions)
            {
                if (thisAction is ClearSectionItem)
                {
                    ClearSectionItem thisItem = thisAction as ClearSectionItem;
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisItem.TrackSectionIndex];
                    thisSection.ClearOccupied(this, true);
                }
            }
        }

        //================================================================================================//
        //
        // Update track actions after coupling
        //

        public void UpdateTrackActionsCoupling(bool couple_to_front)
        {

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt",
                            "Train " + Number.ToString() +
                            " coupled (front : " + couple_to_front.ToString() +
            " ) while on section " + PresentPosition[0].TCSectionIndex.ToString() + "\n");
#endif
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Train " + Number.ToString() +
                                " coupled (front : " + couple_to_front.ToString() +
                " ) while on section " + PresentPosition[0].TCSectionIndex.ToString() + "\n");
            }

            // remove train from track - clear all reservations etc.

            RemoveFromTrack();
            DeadlockInfo.Clear();

            // check if new train is freight or not

            CheckFreight();

            // clear all track occupation actions

            ClearSectionItem dummyItem = new ClearSectionItem(0.0f, 0);
            List<DistanceTravelledItem> activeActions = requiredActions.GetActions(99999999f, dummyItem.GetType());
            activeActions.Clear();

            // save existing TCPositions

            TCPosition oldPresentPosition = new TCPosition();
            PresentPosition[0].CopyTo(ref oldPresentPosition);
            TCPosition oldRearPosition = new TCPosition();
            PresentPosition[1].CopyTo(ref oldRearPosition);

            PresentPosition[0] = new TCPosition();
            PresentPosition[1] = new TCPosition();

            // create new TCPositions

            TrackNode tn = FrontTDBTraveller.TN;
            float offset = FrontTDBTraveller.TrackNodeOffset;
            int direction = (int)FrontTDBTraveller.Direction;

            tn.TCCrossReference.GetTCPosition(offset, direction, ref PresentPosition[0]);
            PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            tn = RearTDBTraveller.TN;
            offset = RearTDBTraveller.TrackNodeOffset;
            direction = (int)RearTDBTraveller.Direction;

            tn.TCCrossReference.GetTCPosition(offset, direction, ref PresentPosition[1]);

            PresentPosition[0].DistanceTravelledM = DistanceTravelledM;
            PresentPosition[1].DistanceTravelledM = oldRearPosition.DistanceTravelledM;

            // use difference in position to update existing DistanceTravelled

            float deltaoffset = 0.0f;

            if (couple_to_front)
            {
                float offset_old = oldPresentPosition.TCOffset;
                float offset_new = PresentPosition[0].TCOffset;

                if (oldPresentPosition.TCSectionIndex == PresentPosition[0].TCSectionIndex)
                {
                    deltaoffset = offset_new - offset_old;
                }
                else
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[oldPresentPosition.TCSectionIndex];
                    deltaoffset = thisSection.Length - offset_old;
                    deltaoffset += offset_new;

                    for (int iIndex = oldPresentPosition.RouteListIndex + 1; iIndex < PresentPosition[0].RouteListIndex; iIndex++)
                    {
                        thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                        deltaoffset += thisSection.Length;
                    }
                }
                PresentPosition[0].DistanceTravelledM += deltaoffset;
                DistanceTravelledM += deltaoffset;
            }
            else
            {
                float offset_old = oldRearPosition.TCOffset;
                float offset_new = PresentPosition[1].TCOffset;

                if (oldRearPosition.TCSectionIndex == PresentPosition[1].TCSectionIndex)
                {
                    deltaoffset = offset_old - offset_new;
                }
                else
                {
                    deltaoffset = offset_old;
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
                    deltaoffset += (thisSection.Length - offset_new);

                    for (int iIndex = oldRearPosition.RouteListIndex - 1; iIndex > PresentPosition[1].RouteListIndex; iIndex--)
                    {
                        thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                        deltaoffset += thisSection.Length;
                    }
                }
                PresentPosition[1].DistanceTravelledM -= deltaoffset;
            }

            // Set track sections to occupied - forward direction only

            OccupiedTrack.Clear();
            if (TrainRoute != null) TrainRoute.Clear();
            TrainRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                PresentPosition[1].TCDirection, Length, false, false, true);

            foreach (TCRouteElement thisElement in TrainRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                thisSection.SetOccupied(routedForward);
            }

            // add sections to required actions list

            foreach (TrackCircuitSection thisSection in OccupiedTrack)
            {
                float distanceToClear = DistanceTravelledM + thisSection.Length + standardOverlapM;
                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION ||
                    thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                {
                    distanceToClear += Length + junctionOverlapM;
                }

                if (PresentPosition[0].TCSectionIndex == thisSection.Index)
                {
                    distanceToClear += Length - PresentPosition[0].TCOffset;
                }
                else if (PresentPosition[1].TCSectionIndex == thisSection.Index)
                {
                    distanceToClear -= PresentPosition[1].TCOffset;
                }
                else
                {
                    distanceToClear += Length;
                }
                requiredActions.InsertAction(new ClearSectionItem(distanceToClear, thisSection.Index));
            }

            // rebuild list of station stops

            if (StationStops.Count > 0)
            {
                int presentStop = StationStops[0].PlatformReference;
                StationStops.Clear();
                HoldingSignals.Clear();

                BuildStationList(15.0f);

                bool removeStations = false;
                for (int iStation = StationStops.Count - 1; iStation >= 0; iStation--)
                {
                    if (removeStations)
                    {
                        if (StationStops[iStation].ExitSignal >= 0 && HoldingSignals.Contains(StationStops[iStation].ExitSignal))
                        {
                            HoldingSignals.Remove(StationStops[iStation].ExitSignal);
                        }
                        StationStops.RemoveAt(iStation);
                    }

                    if (StationStops[iStation].PlatformReference == presentStop)
                    {
                        removeStations = true;
                    }
                }
            }

            // reset signals etc.

            SignalObjectItems.Clear();
            NextSignalObject[0] = null;
            NextSignalObject[1] = null;
            LastReservedSection[0] = PresentPosition[0].TCSectionIndex;
            LastReservedSection[1] = PresentPosition[0].TCSectionIndex;

            InitializeSignals(true);

            if (TCRoute != null && (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL || ControlMode == TRAIN_CONTROL.AUTO_NODE))
            {
                PresentPosition[0].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
                PresentPosition[1].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);

                SwitchToNodeControl(PresentPosition[0].TCSectionIndex);
                CheckDeadlock(ValidRoute[0], Number);
                TCRoute.SetReversalOffset(Length);
            }
            else if (ControlMode == TRAIN_CONTROL.MANUAL)
            {
                // set track occupation

                UpdateSectionStateManual();

                // reset routes and check sections either end of train

                PresentPosition[0].RouteListIndex = -1;
                PresentPosition[1].RouteListIndex = -1;
                PreviousPosition[0].RouteListIndex = -1;

                UpdateManualMode(-1);
            }
            else
            {
                signalRef.requestClearNode(routedForward, ValidRoute[0]);
            }

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt",
                            "Train " + Number.ToString() +
                            " couple procedure completed \n");
#endif
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Train " + Number.ToString() +
                                " couple procedure completed \n");
            }
        }

        //================================================================================================//
        //
        // Update track details after uncoupling
        //

        public void UpdateTrackActionsUncoupling(bool originalTrain)
        {

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt",
                            "Train " + Number.ToString() +
                            " uncouple actions, org train : " + originalTrain.ToString() +
                " ; new type : " + TrainType.ToString() + "\n");
#endif
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Train " + Number.ToString() +
                            " uncouple actions, org train : " + originalTrain.ToString() +
                " ; new type : " + TrainType.ToString() + "\n");
            }

            if (originalTrain)
            {
                RemoveFromTrack();
                DeadlockInfo.Clear();

                ClearSectionItem dummyItem = new ClearSectionItem(0.0f, 0);
                List<DistanceTravelledItem> activeActions = requiredActions.GetActions(99999999f, dummyItem.GetType());
                activeActions.Clear();
            }

            // create new TCPositions

            TrackNode tn = FrontTDBTraveller.TN;
            float offset = FrontTDBTraveller.TrackNodeOffset;
            int direction = (int)FrontTDBTraveller.Direction;

            tn.TCCrossReference.GetTCPosition(offset, direction, ref PresentPosition[0]);
            PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            tn = RearTDBTraveller.TN;
            offset = RearTDBTraveller.TrackNodeOffset;
            direction = (int)RearTDBTraveller.Direction;

            tn.TCCrossReference.GetTCPosition(offset, direction, ref PresentPosition[1]);

            PresentPosition[0].DistanceTravelledM = DistanceTravelledM;
            PresentPosition[1].DistanceTravelledM = DistanceTravelledM - Length;

            // Set track sections to occupied

            OccupiedTrack.Clear();

            // build route of sections now occupied
            OccupiedTrack.Clear();
            if (TrainRoute != null) TrainRoute.Clear();
            TrainRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                PresentPosition[1].TCDirection, Length, false, false, true);

            foreach (TCRouteElement thisElement in TrainRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                thisSection.SetOccupied(routedForward);
            }

            // static train

            if (TrainType == TRAINTYPE.STATIC)
            {

                // clear routes, required actions, traffic details

                ControlMode = TRAIN_CONTROL.UNDEFINED;
                if (TCRoute != null)
                {
                    if (TCRoute.TCRouteSubpaths != null) TCRoute.TCRouteSubpaths.Clear();
                    if (TCRoute.TCAlternativePaths != null) TCRoute.TCAlternativePaths.Clear();
                }
                if (ValidRoute[0] != null && ValidRoute[0].Count > 0)
                {
                    signalRef.BreakDownRouteList(ValidRoute[0], 0, routedForward);
                    ValidRoute[0].Clear();
                }
                if (ValidRoute[1] != null && ValidRoute[1].Count > 0)
                {
                    signalRef.BreakDownRouteList(ValidRoute[1], 0, routedBackward);
                    ValidRoute[1].Clear();
                }
                requiredActions.Clear();

                if (TrafficService != null)
                    TrafficService.TrafficDetails.Clear();

                // build dummy route

                TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
                offset = PresentPosition[1].TCOffset;

                ValidRoute[0] = signalRef.BuildTempRoute(this, thisSection.Index, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, Length, false, true, true);

            }

            // player train

            else
            {

                // rebuild list of station stops

                if (StationStops.Count > 0)
                {
                    int presentStop = StationStops[0].PlatformReference;
                    StationStops.Clear();
                    HoldingSignals.Clear();

                    BuildStationList(15.0f);

                    bool removeStations = false;
                    for (int iStation = StationStops.Count - 1; iStation >= 0; iStation--)
                    {
                        if (removeStations)
                        {
                            if (StationStops[iStation].ExitSignal >= 0 && HoldingSignals.Contains(StationStops[iStation].ExitSignal))
                            {
                                HoldingSignals.Remove(StationStops[iStation].ExitSignal);
                            }
                            StationStops.RemoveAt(iStation);
                        }

                        if (StationStops[iStation].PlatformReference == presentStop)
                        {
                            removeStations = true;
                        }
                    }
                }

                // reset signals etc.

                SignalObjectItems.Clear();
                NextSignalObject[0] = null;
                NextSignalObject[1] = null;
                LastReservedSection[0] = PresentPosition[0].TCSectionIndex;
                LastReservedSection[1] = PresentPosition[1].TCSectionIndex;

                InitializeSignals(true);

                if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL || ControlMode == TRAIN_CONTROL.AUTO_NODE)
                {
                    PresentPosition[0].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
                    PresentPosition[1].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);

                    CheckDeadlock(ValidRoute[0], Number);
                    SwitchToNodeControl(PresentPosition[0].TCSectionIndex);
                    TCRoute.SetReversalOffset(Length);
                }
                else if (ControlMode == TRAIN_CONTROL.MANUAL)
                {
                    // set track occupation

                    UpdateSectionStateManual();

                    // reset routes and check sections either end of train

                    PresentPosition[0].RouteListIndex = -1;
                    PresentPosition[1].RouteListIndex = -1;
                    PreviousPosition[0].RouteListIndex = -1;

                    UpdateManualMode(-1);
                }
                else
                {
                    CheckDeadlock(ValidRoute[0], Number);
                    signalRef.requestClearNode(routedForward, ValidRoute[0]);
                }
            }

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt",
                            "Train " + Number.ToString() +
                            " uncouple procedure completed \n");
#endif
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                                "Train " + Number.ToString() +
                                " uncouple procedure completed \n");
            }
        }

        //================================================================================================//
        //
        // Check on deadlock
        //

        internal void CheckDeadlock(TCSubpathRoute thisRoute, int thisNumber)
        {
            // clear existing deadlock info

            DeadlockInfo.Clear();

            // build new deadlock info

            foreach (Train otherTrain in Simulator.Trains)
            {
                if (otherTrain.Number != thisNumber && otherTrain.TrainType != TRAINTYPE.STATIC)
                {
                    TCSubpathRoute otherRoute = otherTrain.ValidRoute[0];
                    Dictionary<int, int> otherRouteDict = otherRoute.ConvertRoute();

                    for (int iElement = 0; iElement < thisRoute.Count; iElement++)
                    {
                        TCRouteElement thisElement = thisRoute[iElement];
                        int thisSectionIndex = thisElement.TCSectionIndex;
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];
                        int thisSectionDirection = thisElement.Direction;

                        if (thisSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                        {
                            if (otherRouteDict.ContainsKey(thisSectionIndex))
                            {
                                int otherTrainDirection = otherRouteDict[thisSectionIndex];
                                if (otherTrainDirection != thisSectionDirection)
                                {
                                    int[] endDeadlock = SetDeadlock(iElement, thisRoute, otherRoute, otherTrain);
                                    // use end of alternative path if set - if so, compensate for iElement++
                                    iElement = endDeadlock[1] > 0 ? --endDeadlock[1] : endDeadlock[0];
                                }
                                else
                                {
                                    iElement = EndCommonSection(iElement, thisRoute, otherRoute);
                                }
                            }
                        }
                    }
                }
            }
#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\Temp\deadlock.txt", "\n=================\nTrain : " + Number.ToString() + "\n");
            foreach (KeyValuePair<int, List<Dictionary<int, int>>> thisDeadlock in DeadlockInfo)
            {
                File.AppendAllText(@"C:\Temp\deadlock.txt", "Section : " + thisDeadlock.Key.ToString() + "\n");
                foreach (Dictionary<int, int> actDeadlocks in thisDeadlock.Value)
                {
                    foreach (KeyValuePair<int, int> actDeadlockInfo in actDeadlocks)
                    {
                        File.AppendAllText(@"C:\Temp\deadlock.txt", "  Other Train : " + actDeadlockInfo.Key.ToString() +
                            " - end Sector : " + actDeadlockInfo.Value.ToString() + "\n");
                    }
                }
                File.AppendAllText(@"C:\Temp\deadlock.txt", "\n");
            }
#endif
        }

        //================================================================================================//
        //
        // Obtain deadlock details
        //

        private int[] SetDeadlock(int thisIndex, TCSubpathRoute thisRoute, TCSubpathRoute otherRoute, Train otherTrain)
        {
            int[] returnValue = new int[2];
            returnValue[1] = -1;  // set to no alternative path used

            TCRouteElement firstElement = thisRoute[thisIndex];
            int firstSectionIndex = firstElement.TCSectionIndex;
            bool allreadyActive = false;

            int thisTrainSection = firstSectionIndex;
            int otherTrainSection = firstSectionIndex;

            int thisTrainIndex = thisIndex;
            int otherTrainIndex = otherRoute.GetRouteIndex(firstSectionIndex, 0);

            int thisFirstIndex = thisTrainIndex;
            int otherFirstIndex = otherTrainIndex;

            TCRouteElement thisTrainElement = thisRoute[thisTrainIndex];
            TCRouteElement otherTrainElement = otherRoute[otherTrainIndex];

            // loop while not at end of route for either train and sections are equal
            // loop is also exited when alternative path is found for either train
            for (int iLoop = 0; ((thisFirstIndex + iLoop) <= (thisRoute.Count - 1)) && ((otherFirstIndex - iLoop)) >= 0 && (thisTrainSection == otherTrainSection); iLoop++)
            {
                thisTrainIndex = thisFirstIndex + iLoop;
                otherTrainIndex = otherFirstIndex - iLoop;

                thisTrainElement = thisRoute[thisTrainIndex];
                otherTrainElement = otherRoute[otherTrainIndex];
                thisTrainSection = thisTrainElement.TCSectionIndex;
                otherTrainSection = otherTrainElement.TCSectionIndex;

                if (thisTrainElement.StartAlternativePath != null)
                {
                    int endAlternativeSection = thisTrainElement.StartAlternativePath[1];
                    returnValue[1] = thisRoute.GetRouteIndex(endAlternativeSection, thisIndex);
                    break;
                }

                if (otherTrainElement.EndAlternativePath != null)
                {
                    int endAlternativeSection = otherTrainElement.EndAlternativePath[1];
                    returnValue[1] = thisRoute.GetRouteIndex(endAlternativeSection, thisIndex);
                    break;
                }

                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisTrainSection];

                if (thisSection.IsSet(otherTrain))
                {
                    allreadyActive = true;
                }
            }

            // get sections on which loop ended
            thisTrainElement = thisRoute[thisTrainIndex];
            thisTrainSection = thisTrainElement.TCSectionIndex;

            otherTrainElement = otherRoute[otherTrainIndex];
            otherTrainSection = otherTrainElement.TCSectionIndex;

            // if last sections are still equal - end of route reached for one of the trains
            // otherwise, last common sections was previous sections for this train
            int lastSectionIndex = (thisTrainSection == otherTrainSection) ? thisTrainSection :
                thisRoute[thisTrainIndex - 1].TCSectionIndex;

            // if section is not a junction, check if either route not ended, if so continue up to next junction
            TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastSectionIndex];
            if (lastSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.JUNCTION)
            {
                bool endSectionFound = false;
                if (thisTrainIndex < (thisRoute.Count - 1))
                {
                    for (int iIndex = thisTrainIndex + 1; iIndex < thisRoute.Count - 1 && !endSectionFound; iIndex++)
                    {
                        lastSection = signalRef.TrackCircuitList[thisRoute[iIndex].TCSectionIndex];
                        endSectionFound = lastSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION;
                    }
                }

                else if (otherTrainIndex > 0)
                {
                    for (int iIndex = otherTrainIndex - 1; iIndex >= 0 && !endSectionFound; iIndex--)
                    {
                        lastSection = signalRef.TrackCircuitList[otherRoute[iIndex].TCSectionIndex];
                        endSectionFound = lastSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION;
                        if (lastSection.IsSet(otherTrain))
                        {
                            allreadyActive = true;
                        }
                    }
                }
                lastSectionIndex = lastSection.Index;
            }

            // set deadlock info for both trains

            SetDeadlockInfo(firstSectionIndex, lastSectionIndex, otherTrain.Number);
            otherTrain.SetDeadlockInfo(lastSectionIndex, firstSectionIndex, Number);

            if (allreadyActive)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[lastSectionIndex];
                thisSection.SetDeadlockTrap(otherTrain, otherTrain.DeadlockInfo[lastSectionIndex]);
            }

            returnValue[0] = thisRoute.GetRouteIndex(lastSectionIndex, thisIndex);
            if (returnValue[0] < 0)
                returnValue[0] = thisTrainIndex;
            return (returnValue);
        }

        //================================================================================================//
        //
        // Set deadlock information
        //

        private void SetDeadlockInfo(int firstSection, int lastSection, int otherTrainNumber)
        {
            List<Dictionary<int, int>> DeadlockList = null;

            if (DeadlockInfo.ContainsKey(firstSection))
            {
                DeadlockList = DeadlockInfo[firstSection];
            }
            else
            {
                DeadlockList = new List<Dictionary<int, int>>();
                DeadlockInfo.Add(firstSection, DeadlockList);
            }
            Dictionary<int, int> thisDeadlock = new Dictionary<int, int>();
            thisDeadlock.Add(otherTrainNumber, lastSection);
            DeadlockList.Add(thisDeadlock);
        }

        //================================================================================================//
        //
        // Get end of common section
        //

        private int EndCommonSection(int thisIndex, TCSubpathRoute thisRoute, TCSubpathRoute otherRoute)
        {
            int firstSection = thisRoute[thisIndex].TCSectionIndex;

            int thisTrainSection = firstSection;
            int otherTrainSection = firstSection;

            int thisTrainIndex = thisIndex;
            int otherTrainIndex = otherRoute.GetRouteIndex(firstSection, 0);

            while (thisTrainSection == otherTrainSection && thisTrainIndex < (thisRoute.Count - 1) && otherTrainIndex > 0)
            {
                thisTrainIndex++;
                otherTrainIndex--;
                thisTrainSection = thisRoute[thisTrainIndex].TCSectionIndex;
                otherTrainSection = otherRoute[otherTrainIndex].TCSectionIndex;
            }

            return (thisTrainIndex);
        }

        //================================================================================================//
        //
        // Check if waiting for deadlock
        //

        public bool CheckDeadlockWait(SignalObject nextSignal)
        {

            bool deadlockWait = false;

            // check section list of signal for any deadlock traps

            foreach (TCRouteElement thisElement in nextSignal.signalRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                if (thisSection.DeadlockTraps.ContainsKey(Number))              // deadlock trap
                {
                    deadlockWait = true;

                    List<int> deadlockTrains = thisSection.DeadlockTraps[Number];

                    if (DeadlockInfo.ContainsKey(thisSection.Index))        // reverse deadlocks
                    {
                        foreach (Dictionary<int, int> thisDeadlockList in DeadlockInfo[thisSection.Index])
                        {
                            foreach (KeyValuePair<int, int> thisDeadlock in thisDeadlockList)
                            {
                                if (!deadlockTrains.Contains(thisDeadlock.Key))
                                {
                                    TrackCircuitSection endSection = signalRef.TrackCircuitList[thisDeadlock.Value];
                                    endSection.SetDeadlockTrap(Number, thisDeadlock.Key);
                                }
                            }
                        }
                    }
                }
            }
            return (deadlockWait);
        }

        //================================================================================================//
        /// <summary>
        /// Create station stop list
        /// <\summary>

        public void BuildStationList(float clearingDistanceM)
        {
            if (TrafficService == null)
                return;   // no traffic definition

            int activeSubroute = 0;
            int lastRouteIndex = 0;

            TCSubpathRoute thisRoute = TCRoute.TCRouteSubpaths[activeSubroute];

            // loop through traffic points

            foreach (Traffic_Traffic_Item thisItem in TrafficService.TrafficDetails)
            {
                int platformIndex;

                // get platform details

                if (signalRef.PlatformXRefList.TryGetValue(thisItem.PlatformStartID, out platformIndex))
                {
                    PlatformDetails thisPlatform = signalRef.PlatformDetailsList[platformIndex];
                    int sectionIndex = thisPlatform.TCSectionIndex[0];
                    int routeIndex = thisRoute.GetRouteIndex(sectionIndex, 0);

                    // if first section not found in route, try last

                    if (routeIndex < 0)
                    {
                        sectionIndex = thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1];
                        routeIndex = thisRoute.GetRouteIndex(sectionIndex, 0);
                    }

                    // if neither section found - try next subroute - keep trying till found or out of subroutes

                    while (routeIndex < 0 && activeSubroute < (TCRoute.TCRouteSubpaths.Count - 1))
                    {
                        activeSubroute++;
                        thisRoute = TCRoute.TCRouteSubpaths[activeSubroute];
                        routeIndex = thisRoute.GetRouteIndex(sectionIndex, 0);

                        // if first section not found in route, try last

                        if (routeIndex < 0)
                        {
                            sectionIndex = thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1];
                            routeIndex = thisRoute.GetRouteIndex(sectionIndex, 0);
                        }
                    }

                    // if neither section found - platform is not on route - skip

                    if (routeIndex < 0)
                    {
                        Trace.TraceWarning("Train {0} : platform {1} is not on route",
                                Number.ToString(), thisItem.PlatformStartID.ToString());
                        break;
                    }

                    // determine end stop position depending on direction

                    TCRouteElement thisElement = thisRoute[routeIndex];

                    int endSectionIndex = thisElement.Direction == 0 ?
                        thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1] :
                        thisPlatform.TCSectionIndex[0];
                    int beginSectionIndex = thisElement.Direction == 0 ?
                        thisPlatform.TCSectionIndex[0] :
                        thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1];

                    float endOffset = thisPlatform.TCOffset[1, thisElement.Direction];
                    float beginOffset = thisPlatform.TCOffset[0, thisElement.Direction];

                    float deltaLength = thisPlatform.Length - Length; // platform length - train length

                    TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];


                    int firstRouteIndex = thisRoute.GetRouteIndex(beginSectionIndex, 0);
                    if (firstRouteIndex < 0)
                        firstRouteIndex = routeIndex;
                    lastRouteIndex = thisRoute.GetRouteIndex(endSectionIndex, 0);
                    if (lastRouteIndex < 0)
                        lastRouteIndex = routeIndex;

                    // if train too long : search back for platform with same name

                    float fullLength = thisPlatform.Length;

                    if (deltaLength < 0)
                    {
                        float actualBegin = beginOffset;

                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[beginSectionIndex];

                        // Other platforms in same section

                        if (thisSection.PlatformIndex.Count > 1)
                        {
                            foreach (int nextIndex in thisSection.PlatformIndex)
                            {
                                if (nextIndex != platformIndex)
                                {
                                    PlatformDetails otherPlatform = signalRef.PlatformDetailsList[nextIndex];
                                    if (String.Compare(otherPlatform.Name, thisPlatform.Name) == 0)
                                    {
                                        int otherSectionIndex = thisElement.Direction == 0 ?
                                            otherPlatform.TCSectionIndex[0] :
                                            otherPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1];
                                        if (otherSectionIndex == beginSectionIndex)
                                        {
                                            if (otherPlatform.TCOffset[0, thisElement.Direction] < actualBegin)
                                            {
                                                actualBegin = otherPlatform.TCOffset[0, thisElement.Direction];
                                                fullLength = endOffset - actualBegin;
                                            }
                                        }
                                        else
                                        {
                                            int addRouteIndex = thisRoute.GetRouteIndex(otherSectionIndex, 0);
                                            float addOffset = otherPlatform.TCOffset[1, thisElement.Direction == 0 ? 1 : 0];
                                            // offset of begin in other direction is length of available track

                                            if (lastRouteIndex > 0)
                                            {
                                                float thisLength =
                                                    thisRoute.GetDistanceAlongRoute(addRouteIndex, addOffset,
                                                            lastRouteIndex, endOffset, true, signalRef);
                                                if (thisLength > fullLength)
                                                    fullLength = thisLength;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        deltaLength = fullLength - Length;
                    }

                    // search back along route

                    if (deltaLength < 0)
                    {
                        float distance = fullLength + beginOffset;
                        bool platformFound = false;

                        for (int iIndex = firstRouteIndex - 1;
                                    iIndex >= 0 && distance < 500f && platformFound;
                                    iIndex--)
                        {
                            int nextSectionIndex = thisRoute[iIndex].TCSectionIndex;
                            TrackCircuitSection nextSection = signalRef.TrackCircuitList[nextSectionIndex];

                            foreach (int otherPlatformIndex in nextSection.PlatformIndex)
                            {
                                PlatformDetails otherPlatform = signalRef.PlatformDetailsList[otherPlatformIndex];
                                if (String.Compare(otherPlatform.Name, thisPlatform.Name) == 0)
                                {
                                    fullLength = otherPlatform.Length + distance;
                                    // we miss a little bit (offset) - that's because we don't know direction of other platform
                                    platformFound = true; // only check for one more
                                }
                            }
                            distance += nextSection.Length;
                        }

                        deltaLength = fullLength - Length;
                    }


                    // determine stop position

                    float stopOffset = endOffset - (0.5f * deltaLength);

                    // beyond section : check for route validity (may not exceed route)

                    if (stopOffset > endSection.Length)
                    {
                        float addOffset = stopOffset - endSection.Length;
                        float overlap = 0f;

                        for (int iIndex = lastRouteIndex; iIndex < thisRoute.Count && overlap < addOffset; iIndex++)
                        {
                            TrackCircuitSection nextSection = signalRef.TrackCircuitList[thisRoute[iIndex].TCSectionIndex];
                            overlap += nextSection.Length;
                        }

                        if (overlap < stopOffset)
                            stopOffset = overlap;
                    }

                    // check if stop offset beyond end signal - do not hold at signal

                    int EndSignal = -1;
                    bool HoldSignal = false;

                    // check if train is to reverse in platform
                    // if so, set signal at other end as hold signal

                    int useDirection = thisElement.Direction;
                    bool inDirection = true;

                    if (TCRoute.ReversalInfo[activeSubroute].Valid)
                    {
                        TCReversalInfo thisReversal = TCRoute.ReversalInfo[activeSubroute];
                        int reversalIndex = thisReversal.SignalUsed ? thisReversal.LastSignalIndex : thisReversal.LastDivergeIndex;
                        if (reversalIndex >= 0 && reversalIndex <= lastRouteIndex) // reversal point is this section or earlier
                        {
                            useDirection = useDirection == 0 ? 1 : 0;
                            inDirection = false;
                        }
                    }

                    // check for end signal

                    if (thisPlatform.EndSignals[useDirection] >= 0)
                    {
                        EndSignal = thisPlatform.EndSignals[useDirection];
                        // stop location is in front of signal
                        if (inDirection)
                        {
                            if (thisPlatform.DistanceToSignals[useDirection] > (stopOffset - endOffset))
                            {
                                HoldSignal = true;

                                if ((thisPlatform.DistanceToSignals[useDirection] + (endOffset - stopOffset)) < clearingDistanceM)
                                {
                                    stopOffset = endOffset + thisPlatform.DistanceToSignals[useDirection] - clearingDistanceM - 1.0f;
                                }
                            }
                            // if most of train fits in platform then stop at signal
                            else if ((thisPlatform.DistanceToSignals[useDirection] - clearingDistanceM + thisPlatform.Length) >
                                          (0.6 * Length))
                            {
                                HoldSignal = true;
                                stopOffset = endOffset + thisPlatform.DistanceToSignals[useDirection] - clearingDistanceM - 1.0f;
                                // set 1m earlier to give priority to station stop over signal
                            }
                        }
                        else
                        // end of train is beyond signal
                        {
                            if ((beginOffset - thisPlatform.DistanceToSignals[useDirection]) < (stopOffset - Length))
                            {
                                HoldSignal = true;

                                if ((stopOffset - Length - beginOffset + thisPlatform.DistanceToSignals[useDirection]) < clearingDistanceM)
                                {
                                    stopOffset = beginOffset - thisPlatform.DistanceToSignals[useDirection] + Length + clearingDistanceM + 1.0f;
                                }
                            }
                            // if most of train fits in platform then stop at signal
                            else if ((thisPlatform.DistanceToSignals[useDirection] - clearingDistanceM + thisPlatform.Length) >
                                          (0.6 * Length))
                            {
                                // set 1m earlier to give priority to station stop over signal
                                stopOffset = beginOffset - thisPlatform.DistanceToSignals[useDirection] + Length + clearingDistanceM + 1.0f;

                                // check if stop is clear of end signal (if any)
                                if (thisPlatform.EndSignals[thisElement.Direction] != -1)
                                {
                                    if (stopOffset < (endOffset + thisPlatform.DistanceToSignals[thisElement.Direction]))
                                    {
                                        HoldSignal = true; // if train fits between signals
                                    }
                                    else
                                    {
                                        stopOffset = endOffset + thisPlatform.DistanceToSignals[thisElement.Direction] - 1.0f; // stop at end signal
                                    }
                                }
                            }
                        }
                    }

                    // build and add station stop

                    TCRouteElement lastElement = thisRoute[lastRouteIndex];

                    StationStop thisStation = new StationStop(
                            thisItem.PlatformStartID,
                            thisPlatform,
                            activeSubroute,
                            lastRouteIndex,
                            lastElement.TCSectionIndex,
                            thisElement.Direction,
                            EndSignal,
                            HoldSignal,
                            stopOffset,
                            thisItem.ArrivalTime,
                            thisItem.DepartTime,
                            StationStop.STOPTYPE.STATION_STOP);
                    StationStops.Add(thisStation);

                    // add signal to list of hold signals

                    if (HoldSignal)
                    {
                        HoldingSignals.Add(EndSignal);
                    }
                }
                else
                {
                    Trace.TraceInformation("Train {0} : cannot find platform {1}",
                            Number.ToString(), thisItem.PlatformStartID.ToString());
                }

            }
#if DEBUG_TEST
            File.AppendAllText(@"C:\temp\TCSections.txt", "\nSTATION STOPS\n\n");

            if (StationStops.Count <= 0)
            {
                File.AppendAllText(@"C:\temp\TCSections.txt", " No stops\n");
            }
            else
            {
                foreach (StationStop thisStation in StationStops)
                {
                    File.AppendAllText(@"C:\temp\TCSections.txt", "\n");
                    if (thisStation.PlatformItem == null)
                    {
                        File.AppendAllText(@"C:\temp\TCSections.txt", "Waiting Point");
                    }
                    else
                    {
                        File.AppendAllText(@"C:\temp\TCSections.txt", "Station : " + thisStation.PlatformItem.Name + "\n");
                        DateTime baseDT = new DateTime();
                        DateTime arrTime = baseDT.AddSeconds(thisStation.ArrivalTime);
                        File.AppendAllText(@"C:\temp\TCSections.txt", "Arrive  : " + arrTime.ToString("HH:mm:ss") + "\n");
                        DateTime depTime = baseDT.AddSeconds(thisStation.DepartTime);
                        File.AppendAllText(@"C:\temp\TCSections.txt", "Depart  : " + depTime.ToString("HH:mm:ss") + "\n");
                    }
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Exit Sig: " + thisStation.ExitSignal.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Hold Sig: " + thisStation.HoldSignal.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Subpath : " + thisStation.SubrouteIndex.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Index   : " + lastRouteIndex.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Section : " + thisStation.TCSectionIndex.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Direct  : " + thisStation.Direction.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Stop    : " + thisStation.StopOffset.ToString("###0.00") + "\n");
                }
            }
#endif
        }

        //================================================================================================//
        /// <summary>
        /// Create waiting point list
        /// <\summary>

        public virtual void BuildWaitingPointList(float clearingDistanceM)
        {

            // loop through all waiting points - back to front as the processing affects the actual routepaths

            int prevSection = -1;

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
                    Trace.TraceInformation("Waiting points for train " + Number.ToString() + " combined, total time set to " + prevWP[2].ToString());
                    continue;
                }

                // check if section has signal

                prevSection = waitingPoint[1];  // save

                bool endSectionFound = false;
                int endSignalIndex = -1;

                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisRoute[routeIndex].TCSectionIndex];
                TrackCircuitSection nextSection =
                    routeIndex < thisRoute.Count - 2 ? signalRef.TrackCircuitList[thisRoute[routeIndex + 1].TCSectionIndex] : null;

                int direction = thisRoute[routeIndex].Direction;
                if (thisSection.EndSignals[direction] != null)
                {
                    endSectionFound = true;
                    endSignalIndex = thisSection.EndSignals[direction].thisRef;
                }

                // check if next section is junction

                else if (nextSection == null || nextSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.NORMAL)
                {
                    endSectionFound = true;
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
                        endSignalIndex = nextSection.EndSignals[direction].thisRef;
                    }
                    else if (nextSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.NORMAL)
                    {
                        endSectionFound = true;
                        lastIndex = nextIndex - 1;
                    }
                    nextIndex++;
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

                // repeat actual waiting section in next subroute

                nextRoute.Insert(0, thisRoute[thisRoute.Count - 1]);

                // add end signal to hold list, set ref in waiting point

                if (endSignalIndex >= 0)
                {
                    TCRoute.WaitingPoints[iWait][4] = endSignalIndex;
                    HoldingSignals.Add(endSignalIndex);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Convert player traffic list to station list
        /// <\summary>

        public void ConvertPlayerTraffic(List<Player_Traffic_Item> playerList)
        {

            if (playerList == null || playerList.Count == 0)
            {
                return;    // no traffic details
            }

            TrafficService = new Traffic_Service_Definition();
            TrafficService.TrafficDetails = new List<Traffic_Traffic_Item>();

            foreach (Player_Traffic_Item thisItem in playerList)
            {
                int iArrivalTime = Convert.ToInt32(thisItem.ArrivalTime.TimeOfDay.TotalSeconds);
                int iDepartTime = Convert.ToInt32(thisItem.DepartTime.TimeOfDay.TotalSeconds);
                Traffic_Traffic_Item newItem = new Traffic_Traffic_Item(iArrivalTime, iDepartTime,
                        0, thisItem.DistanceDownPath, thisItem.PlatformStartID);
                TrafficService.TrafficDetails.Add(newItem);
            }

            BuildStationList(15.0f);  // use 15m. clearing distance
        }

        //================================================================================================//
        /// <summary>
        /// Clear station from list, clear exit signal if required
        /// <\summary>

        public void ClearStation(uint id1, uint id2)
        {
            int foundStation = -1;
            StationStop thisStation = null;

            for (int iStation = 0; iStation < StationStops.Count && foundStation < 0; iStation++)
            {
                thisStation = StationStops[iStation];
                if (thisStation.PlatformReference == id1 ||
                    thisStation.PlatformReference == id2)
                {
                    foundStation = iStation;
                }

                if (thisStation.SubrouteIndex > TCRoute.activeSubpath) break; // stop looking if station is in next subpath
            }

            if (foundStation >= 0)
            {
                thisStation = StationStops[foundStation];
                if (thisStation.ExitSignal >= 0)
                {
                    HoldingSignals.Remove(thisStation.ExitSignal);

                    if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
                    {
                        SignalObject nextSignal = signalRef.SignalObjects[thisStation.ExitSignal];
                        nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null);
                    }
                }
            }

            for (int iStation = foundStation; iStation >= 0; iStation--)
            {
                StationStops.RemoveAt(iStation);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Create status line
        /// <\summary>

        public String[] GetStatus(bool metric)
        {

            int iColumn = 0;

            string[] statusString = new string[13];

            statusString[iColumn] = Number.ToString();

            if (IsFreight)
            {
                statusString[iColumn] = String.Concat(statusString[iColumn], " F");
            }
            else
            {
                statusString[iColumn] = String.Concat(statusString[iColumn], " P");
            }
            iColumn++;

            statusString[iColumn] = FormatStrings.FormatDistance(DistanceTravelledM, metric);
            iColumn++;
            statusString[iColumn] = FormatStrings.FormatSpeed(SpeedMpS, metric);
            iColumn++;
            statusString[iColumn] = FormatStrings.FormatSpeed(AllowedMaxSpeedMpS, metric);
            iColumn++;

            statusString[iColumn] = " ";  // for AI trains
            iColumn++;
            statusString[iColumn] = " ";  // for AI trains
            iColumn++;

            switch (ControlMode)
            {
                case TRAIN_CONTROL.AUTO_SIGNAL:
                    statusString[iColumn] = "SIGN";
                    break;
                case TRAIN_CONTROL.AUTO_NODE:
                    statusString[iColumn] = "NODE";
                    break;
                case TRAIN_CONTROL.MANUAL:
                    statusString[iColumn] = "MAN";
                    break;
                case TRAIN_CONTROL.OUT_OF_CONTROL:
                    statusString[iColumn] = "OOC";
                    break;
                case TRAIN_CONTROL.EXPLORER:
                    statusString[iColumn] = "EXPL";
                    break;
            }

            iColumn++;
            if (ControlMode == TRAIN_CONTROL.OUT_OF_CONTROL)
            {
                switch (OutOfControlReason)
                {
                    case OUTOFCONTROL.SPAD:
                        statusString[iColumn] = "SPAD";
                        break;
                    case OUTOFCONTROL.SPAD_REAR:
                        statusString[iColumn] = "RSPD";
                        break;
                    case OUTOFCONTROL.OUT_OF_AUTHORITY:
                        statusString[iColumn] = "OOAU";
                        break;
                    case OUTOFCONTROL.OUT_OF_PATH:
                        statusString[iColumn] = "OOPA";
                        break;
                    case OUTOFCONTROL.SLIPPED_INTO_PATH:
                        statusString[iColumn] = "SLPP";
                        break;
                    case OUTOFCONTROL.SLIPPED_TO_ENDOFTRACK:
                        statusString[iColumn] = "SLPT";
                        break;
                    case OUTOFCONTROL.OUT_OF_TRACK:
                        statusString[iColumn] = "OOTR";
                        break;
                    case OUTOFCONTROL.MISALIGNED_SWITCH:
                        statusString[iColumn] = "MASW";
                        break;
                    default:
                        statusString[iColumn] = "....";
                        break;
                }

                iColumn++;
                statusString[iColumn] = " ";
            }

            else if (ControlMode == TRAIN_CONTROL.AUTO_NODE)
            {
                switch (EndAuthorityType[0])
                {
                    case END_AUTHORITY.END_OF_TRACK:
                        statusString[iColumn] = "EOT";
                        break;
                    case END_AUTHORITY.END_OF_PATH:
                        statusString[iColumn] = "EOP";
                        break;
                    case END_AUTHORITY.RESERVED_SWITCH:
                        statusString[iColumn] = "RSW";
                        break;
                    case END_AUTHORITY.LOOP:
                        statusString[iColumn] = "LP ";
                        break;
                    case END_AUTHORITY.TRAIN_AHEAD:
                        statusString[iColumn] = "TAH";
                        break;
                    case END_AUTHORITY.MAX_DISTANCE:
                        statusString[iColumn] = "MXD";
                        break;
                    case END_AUTHORITY.NO_PATH_RESERVED:
                        statusString[iColumn] = "NOP";
                        break;
                    default:
                        statusString[iColumn] = "";
                        break;
                }

                iColumn++;
                if (EndAuthorityType[0] != END_AUTHORITY.MAX_DISTANCE && EndAuthorityType[0] != END_AUTHORITY.NO_PATH_RESERVED)
                {
                    statusString[iColumn] = FormatStrings.FormatDistance(DistanceToEndNodeAuthorityM[0], metric);
                }
                else
                {
                    statusString[iColumn] = " ";
                }
            }
            else
            {
                statusString[iColumn] = " ";
                iColumn++;
                statusString[iColumn] = " ";
            }

            iColumn++;
            if (ControlMode == TRAIN_CONTROL.MANUAL || ControlMode == TRAIN_CONTROL.EXPLORER)
            {
                // reverse direction
                string firstchar = "-";

                if (NextSignalObject[1] != null)
                {
                    SignalHead.SIGASP nextAspect = GetNextSignalAspect(1);
                    if (NextSignalObject[1].enabledTrain == null || NextSignalObject[1].enabledTrain.Train != this) nextAspect = SignalHead.SIGASP.STOP;  // aspect only valid if signal enabled for this train

                    switch (nextAspect)
                    {
                        case SignalHead.SIGASP.STOP:
                            if (NextSignalObject[1].hasPermission == SignalObject.PERMISSION.GRANTED)
                            {
                                firstchar = "G";
                            }
                            else
                            {
                                firstchar = "S";
                            }
                            break;
                        case SignalHead.SIGASP.STOP_AND_PROCEED:
                            firstchar = "P";
                            break;
                        case SignalHead.SIGASP.RESTRICTING:
                            firstchar = "R";
                            break;
                        case SignalHead.SIGASP.APPROACH_1:
                            firstchar = "A";
                            break;
                        case SignalHead.SIGASP.APPROACH_2:
                            firstchar = "A";
                            break;
                        case SignalHead.SIGASP.APPROACH_3:
                            firstchar = "A";
                            break;
                        case SignalHead.SIGASP.CLEAR_1:
                            firstchar = "C";
                            break;
                        case SignalHead.SIGASP.CLEAR_2:
                            firstchar = "C";
                            break;
                    }
                }

                // forward direction
                string lastchar = "-";

                if (NextSignalObject[0] != null)
                {
                    SignalHead.SIGASP nextAspect = GetNextSignalAspect(0);
                    if (NextSignalObject[0].enabledTrain == null || NextSignalObject[0].enabledTrain.Train != this) nextAspect = SignalHead.SIGASP.STOP;  // aspect only valid if signal enabled for this train

                    switch (nextAspect)
                    {
                        case SignalHead.SIGASP.STOP:
                            if (NextSignalObject[0].hasPermission == SignalObject.PERMISSION.GRANTED)
                            {
                                lastchar = "G";
                            }
                            else
                            {
                                lastchar = "S";
                            }
                            break;
                        case SignalHead.SIGASP.STOP_AND_PROCEED:
                            lastchar = "P";
                            break;
                        case SignalHead.SIGASP.RESTRICTING:
                            lastchar = "R";
                            break;
                        case SignalHead.SIGASP.APPROACH_1:
                            lastchar = "A";
                            break;
                        case SignalHead.SIGASP.APPROACH_2:
                            lastchar = "A";
                            break;
                        case SignalHead.SIGASP.APPROACH_3:
                            lastchar = "A";
                            break;
                        case SignalHead.SIGASP.CLEAR_1:
                            lastchar = "C";
                            break;
                        case SignalHead.SIGASP.CLEAR_2:
                            lastchar = "C";
                            break;
                    }
                }

                statusString[iColumn] = String.Concat(firstchar, "<>", lastchar);
                iColumn++;
                statusString[iColumn] = " ";
            }
            else
            {
                if (NextSignalObject[0] != null)
                {
                    SignalHead.SIGASP nextAspect = GetNextSignalAspect(0);

                    switch (nextAspect)
                    {
                        case SignalHead.SIGASP.STOP:
                            statusString[iColumn] = "STOP";
                            break;
                        case SignalHead.SIGASP.STOP_AND_PROCEED:
                            statusString[iColumn] = "SPRC";
                            break;
                        case SignalHead.SIGASP.RESTRICTING:
                            statusString[iColumn] = "REST";
                            break;
                        case SignalHead.SIGASP.APPROACH_1:
                            statusString[iColumn] = "APP1";
                            break;
                        case SignalHead.SIGASP.APPROACH_2:
                            statusString[iColumn] = "APP2";
                            break;
                        case SignalHead.SIGASP.APPROACH_3:
                            statusString[iColumn] = "APP3";
                            break;
                        case SignalHead.SIGASP.CLEAR_1:
                            statusString[iColumn] = "CLR1";
                            break;
                        case SignalHead.SIGASP.CLEAR_2:
                            statusString[iColumn] = "CLR2";
                            break;
                    }

                    iColumn++;
                    statusString[iColumn] = FormatStrings.FormatDistance(distanceToSignal, metric);
                }
                else
                {
                    statusString[iColumn] = " ";
                    iColumn++;
                    statusString[iColumn] = " ";
                }
            }

            iColumn++;
            statusString[iColumn] = "PLAYER";

            string circuitString = String.Empty;

            if (ControlMode != TRAIN_CONTROL.MANUAL && ControlMode != TRAIN_CONTROL.EXPLORER)
            {
                circuitString = String.Concat(circuitString, TCRoute.activeSubpath.ToString());
                circuitString = String.Concat(circuitString, "={");

                int startIndex = PresentPosition[0].RouteListIndex;
                if (startIndex < 0)
                {
                    circuitString = String.Concat(circuitString, "<out of route>");
                }
                else
                {
                    for (int iIndex = PresentPosition[0].RouteListIndex; iIndex < ValidRoute[0].Count; iIndex++)
                    {
                        TCRouteElement thisElement = ValidRoute[0][iIndex];
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                        circuitString = BuildSectionString(circuitString, thisSection, 0);

                    }
                }

                circuitString = String.Concat(circuitString, "}");
                if (TCRoute.activeSubpath < TCRoute.TCRouteSubpaths.Count - 1)
                {
                    circuitString = String.Concat(circuitString, "x", (TCRoute.activeSubpath + 1).ToString());
                }
            }
            else
            {
                // backward path
                string backstring = String.Empty;
                for (int iindex = ValidRoute[1].Count - 1; iindex >= 0; iindex--)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[1][iindex].TCSectionIndex];
                    backstring = BuildSectionString(backstring, thisSection, 1);
                }

                if (backstring.Length > 30)
                {
                    backstring = backstring.Substring(backstring.Length - 30);
                    // ensure string starts with section delimiter
                    while (String.Compare(backstring.Substring(0, 1), "-") != 0 &&
                           String.Compare(backstring.Substring(0, 1), "+") != 0 &&
                           String.Compare(backstring.Substring(0, 1), "<") != 0)
                    {
                        backstring = backstring.Substring(1);
                    }

                    circuitString = String.Concat(circuitString, "...");
                }
                circuitString = String.Concat(circuitString, backstring);

                // train indication and direction
                circuitString = String.Concat(circuitString, "={");
                if (MUDirection == Direction.Reverse)
                {
                    circuitString = String.Concat(circuitString, "<");
                }
                else
                {
                    circuitString = String.Concat(circuitString, ">");
                }
                circuitString = String.Concat(circuitString, "}=");

                // forward path

                string forwardstring = String.Empty;
                for (int iindex = 0; iindex < ValidRoute[0].Count; iindex++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iindex].TCSectionIndex];
                    forwardstring = BuildSectionString(forwardstring, thisSection, 0);
                }
                circuitString = String.Concat(circuitString, forwardstring);
            }

            iColumn++;
            statusString[iColumn] = String.Copy(circuitString);

            return (statusString);
        }

        //================================================================================================//
        /// <summary>
        /// Build string for section information
        /// </summary>

        public string BuildSectionString(string thisString, TrackCircuitSection thisSection, int direction)
        {

            string returnString = String.Copy(thisString);

            switch (thisSection.CircuitType)
            {
                case TrackCircuitSection.CIRCUITTYPE.JUNCTION:
                    returnString = String.Concat(returnString, ">");
                    break;
                case TrackCircuitSection.CIRCUITTYPE.CROSSOVER:
                    returnString = String.Concat(returnString, "+");
                    break;
                case TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK:
                    returnString = direction == 0 ? String.Concat(returnString, "]") : String.Concat(returnString, "[");
                    break;
                default:
                    returnString = String.Concat(returnString, "-");
                    break;
            }

            if (thisSection.DeadlockTraps.ContainsKey(Number))
            {
                if (thisSection.DeadlockAwaited.Contains(Number))
                {
                    returnString = String.Concat(returnString, "^");
                }
                else if (thisSection.DeadlockAwaited.Count > 0)
                {
                    returnString = String.Concat(returnString, "~");
                }
                returnString = String.Concat(returnString, "*");
            }

            if (thisSection.CircuitState.TrainOccupy.Count > 0)
            {
                List<TrainRouted> allTrains = thisSection.CircuitState.TrainsOccupying();
                int trainno = allTrains[0].Train.Number;
                returnString = String.Concat(returnString, trainno.ToString());
                if (allTrains.Count > 1)
                {
                    returnString = String.Concat(returnString, "&");
                }
            }

            if (thisSection.CircuitState.TrainReserved != null)
            {
                int trainno = thisSection.CircuitState.TrainReserved.Train.Number;
                returnString = String.Concat(returnString, "(", trainno.ToString(), ")");
            }

            if (thisSection.CircuitState.SignalReserved >= 0)
            {
                returnString = String.Concat(returnString, "(S", thisSection.CircuitState.SignalReserved.ToString(), ")");
            }

            if (thisSection.CircuitState.TrainClaimed.Count > 0)
            {
                returnString = String.Concat(returnString, "#");
            }

            return (returnString);
        }

        //================================================================================================//
        /// <summary>
        /// Create TrackInfoObject for information in TrackMonitor window
        /// </summary>

        public TrainInfo GetTrainInfo()
        {
            TrainInfo thisInfo = new TrainInfo();

            if (ControlMode == TRAIN_CONTROL.AUTO_NODE || ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
            {
                GetTrainInfoAuto(ref thisInfo);
            }
            else if (ControlMode == TRAIN_CONTROL.MANUAL || ControlMode == TRAIN_CONTROL.EXPLORER)
            {
                GetTrainInfoManual(ref thisInfo);
            }
            else if (ControlMode == TRAIN_CONTROL.OUT_OF_CONTROL)
            {
                GetTrainInfoOOC(ref thisInfo);
            }
            else // no state? should not occur, but just set no details at all
            {
                thisInfo.ControlMode = ControlMode;
                thisInfo.direction = 0;
                thisInfo.speedMpS = 0;
                TrainObjectItem dummyItem = new TrainObjectItem(END_AUTHORITY.NO_PATH_RESERVED, 0.0f);
                thisInfo.ObjectInfoForward.Add(dummyItem);
                thisInfo.ObjectInfoBackward.Add(dummyItem);
            }

            // sort items on increasing distance

            thisInfo.ObjectInfoForward.Sort();
            thisInfo.ObjectInfoBackward.Sort();

            return (thisInfo);
        }

        //================================================================================================//
        /// <summary>
        /// Create TrackInfoObject for information in TrackMonitor window for Auto mode
        /// </summary>

        public void GetTrainInfoAuto(ref TrainInfo thisInfo)
        {
            // set control mode
            thisInfo.ControlMode = ControlMode;

            // set speed
            thisInfo.speedMpS = SpeedMpS;

            // set projected speed
            thisInfo.projectedSpeedMpS = ProjectedSpeedMpS;

            // set max speed
            thisInfo.allowedSpeedMpS = AllowedMaxSpeedMpS;

            // set direction
            thisInfo.direction = MUDirection == Direction.Forward ? 0 : (MUDirection == Direction.Reverse ? 1 : -1);

            // set orientation
            thisInfo.cabOrientation = (LeadLocomotive.Flipped ^ LeadLocomotive.GetCabFlipped()) ? 1 : 0;

            // set reversal point

            TCReversalInfo thisReversal = TCRoute.ReversalInfo[TCRoute.activeSubpath];
            if (thisReversal.Valid)
            {
                int reversalSection = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][(TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count) - 1].TCSectionIndex;
                if (thisReversal.LastDivergeIndex >= 0)
                {
                    reversalSection = thisReversal.SignalUsed ? thisReversal.SignalSectorIndex : thisReversal.DivergeSectorIndex;
                }

                TrackCircuitSection rearSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
                float reversalDistanceM =
                    rearSection.GetDistanceBetweenObjects(PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset, PresentPosition[1].TCDirection,
                    reversalSection, 0.0f);
                if (reversalDistanceM > 0)
                {
                    TrainObjectItem nextItem = new TrainObjectItem(0, reversalDistanceM);
                    thisInfo.ObjectInfoForward.Add(nextItem);
                }
            }

            bool maxAuthSet = false;
            // set object items - forward
            if (ControlMode == TRAIN_CONTROL.AUTO_NODE)
            {
                TrainObjectItem nextItem = new TrainObjectItem(EndAuthorityType[0], DistanceToEndNodeAuthorityM[0]);
                thisInfo.ObjectInfoForward.Add(nextItem);
                maxAuthSet = true;
            }

            bool signalProcessed = false;
            foreach (ObjectItemInfo thisItem in SignalObjectItems)
            {
                if (thisItem.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                {
                    TrackMonitorSignalAspect signalAspect =
                        thisItem.ObjectDetails.TranslateTMAspect(thisItem.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL));
                    if (thisItem.ObjectDetails.enabledTrain == null || thisItem.ObjectDetails.enabledTrain.Train != this)
                    {
                        signalAspect = TrackMonitorSignalAspect.Stop;
                        TrainObjectItem stopItem = new TrainObjectItem(signalAspect,
                             thisItem.actual_speed, thisItem.distance_to_train);
                        thisInfo.ObjectInfoForward.Add(stopItem);
                        signalProcessed = true;
                        break;
                    }
                    TrainObjectItem nextItem = new TrainObjectItem(signalAspect,
                         thisItem.actual_speed, thisItem.distance_to_train);
                    thisInfo.ObjectInfoForward.Add(nextItem);
                    signalProcessed = true;
                }
                else if (thisItem.ObjectType == ObjectItemInfo.ObjectItemType.SPEEDLIMIT && thisItem.actual_speed > 0)
                {
                    TrainObjectItem nextItem = new TrainObjectItem(thisItem.actual_speed, thisItem.distance_to_train);
                    thisInfo.ObjectInfoForward.Add(nextItem);
                }
            }

            if (!signalProcessed && NextSignalObject[0] != null && NextSignalObject[0].enabledTrain != null && NextSignalObject[0].enabledTrain.Train == this)
            {
                TrackMonitorSignalAspect signalAspect =
                    NextSignalObject[0].TranslateTMAspect(NextSignalObject[0].this_sig_lr(SignalHead.SIGFN.NORMAL));
                ObjectSpeedInfo thisSpeedInfo = NextSignalObject[0].this_sig_speed(SignalHead.SIGFN.NORMAL);
                float validSpeed = thisSpeedInfo == null ? -1 : (IsFreight ? thisSpeedInfo.speed_freight : thisSpeedInfo.speed_pass);

                TrainObjectItem nextItem = new TrainObjectItem(signalAspect, validSpeed, distanceToSignal);
                thisInfo.ObjectInfoForward.Add(nextItem);
            }

            if (StationStops != null && StationStops.Count > 0 &&
                (!maxAuthSet || StationStops[0].DistanceToTrainM < DistanceToEndNodeAuthorityM[0]) &&
                StationStops[0].SubrouteIndex == TCRoute.activeSubpath)
            {
                TrainObjectItem nextItem = new TrainObjectItem(StationStops[0].DistanceToTrainM);
                thisInfo.ObjectInfoForward.Add(nextItem);
            }

            // set object items - backward

            if (ClearanceAtRearM <= 0)
            {
                TrainObjectItem nextItem = new TrainObjectItem(END_AUTHORITY.NO_PATH_RESERVED, 0.0f);
                thisInfo.ObjectInfoBackward.Add(nextItem);
            }
            else
            {
                if (RearSignalObject != null)
                {
                    TrackMonitorSignalAspect signalAspect = RearSignalObject.TranslateTMAspect(RearSignalObject.this_sig_lr(SignalHead.SIGFN.NORMAL));
                    TrainObjectItem nextItem = new TrainObjectItem(signalAspect, -1.0f, ClearanceAtRearM);
                    thisInfo.ObjectInfoBackward.Add(nextItem);
                }
                else
                {
                    TrainObjectItem nextItem = new TrainObjectItem(END_AUTHORITY.END_OF_AUTHORITY, ClearanceAtRearM);
                    thisInfo.ObjectInfoBackward.Add(nextItem);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Create TrackInfoObject for information in TrackMonitor window when in Manual mode
        /// </summary>

        public void GetTrainInfoManual(ref TrainInfo thisInfo)
        {
            // set control mode
            thisInfo.ControlMode = ControlMode;

            // set speed
            thisInfo.speedMpS = SpeedMpS;

            // set projected speed
            thisInfo.projectedSpeedMpS = ProjectedSpeedMpS;

            // set max speed
            thisInfo.allowedSpeedMpS = AllowedMaxSpeedMpS;

            // set direction
            thisInfo.direction = MUDirection == Direction.Forward ? 0 : (MUDirection == Direction.Reverse ? 1 : -1);

            // set orientation
            thisInfo.cabOrientation = (LeadLocomotive.Flipped ^ LeadLocomotive.GetCabFlipped()) ? 1 : 0;

            // check if train is on original path
            thisInfo.isOnPath = false;
            if (TCRoute != null && TCRoute.activeSubpath >= 0)
            {
                TCSubpathRoute validPath = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath];
                int routeIndex = validPath.GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
                thisInfo.isOnPath = (routeIndex >= 0);
            }

            // set forward information

            // set authority
            TrainObjectItem thisItem = new TrainObjectItem(EndAuthorityType[0], DistanceToEndNodeAuthorityM[0]);
            thisInfo.ObjectInfoForward.Add(thisItem);

            // run along forward path to catch all speedposts and signals

            if (ValidRoute[0] != null)
            {
                float distanceToTrainM = 0.0f;
                float offset = PresentPosition[0].TCOffset;
                float sectionStart = -offset;

                foreach (TCRouteElement thisElement in ValidRoute[0])
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    int sectionDirection = thisElement.Direction;

                    if (thisSection.EndSignals[sectionDirection] != null)
                    {
                        distanceToTrainM = sectionStart + thisSection.Length;
                        SignalObject thisSignal = thisSection.EndSignals[sectionDirection];
                        ObjectSpeedInfo thisSpeedInfo = thisSignal.this_sig_speed(SignalHead.SIGFN.NORMAL);
                        float validSpeed = thisSpeedInfo == null ? -1 : (IsFreight ? thisSpeedInfo.speed_freight : thisSpeedInfo.speed_pass);

                        TrackMonitorSignalAspect signalAspect = thisSignal.TranslateTMAspect(thisSignal.this_sig_lr(SignalHead.SIGFN.NORMAL));
                        thisItem = new TrainObjectItem(signalAspect, validSpeed, distanceToTrainM);
                        thisInfo.ObjectInfoForward.Add(thisItem);
                    }

                    if (thisSection.CircuitItems.TrackCircuitSpeedPosts[sectionDirection] != null)
                    {
                        foreach (ORTS.TrackCircuitSignalItem thisSpeeditem in thisSection.CircuitItems.TrackCircuitSpeedPosts[sectionDirection].TrackCircuitItem)
                        {
                            SignalObject thisSpeedpost = thisSpeeditem.SignalRef;
                            ObjectSpeedInfo thisSpeedInfo = thisSpeedpost.this_sig_speed(SignalHead.SIGFN.SPEED);
                            float validSpeed = thisSpeedInfo == null ? -1 : (IsFreight ? thisSpeedInfo.speed_freight : thisSpeedInfo.speed_pass);

                            distanceToTrainM = sectionStart + thisSpeeditem.SignalLocation;

                            if (distanceToTrainM > 0 && validSpeed > 0)
                            {
                                thisItem = new TrainObjectItem(validSpeed, distanceToTrainM);
                                thisInfo.ObjectInfoForward.Add(thisItem);
                            }
                        }
                    }

                    sectionStart += thisSection.Length;
                }
            }

            // set backward information

            // set authority
            thisItem = new TrainObjectItem(EndAuthorityType[1], DistanceToEndNodeAuthorityM[1]);
            thisInfo.ObjectInfoBackward.Add(thisItem);

            // run along backward path to catch all speedposts and signals

            if (ValidRoute[1] != null)
            {
                float distanceToTrainM = 0.0f;
                float offset = PresentPosition[1].TCOffset;
                TrackCircuitSection firstSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
                float sectionStart = offset - firstSection.Length;

                foreach (TCRouteElement thisElement in ValidRoute[1])
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    int sectionDirection = thisElement.Direction;

                    if (thisSection.EndSignals[sectionDirection] != null)
                    {
                        distanceToTrainM = sectionStart + thisSection.Length;
                        SignalObject thisSignal = thisSection.EndSignals[sectionDirection];
                        ObjectSpeedInfo thisSpeedInfo = thisSignal.this_sig_speed(SignalHead.SIGFN.NORMAL);
                        float validSpeed = thisSpeedInfo == null ? -1 : (IsFreight ? thisSpeedInfo.speed_freight : thisSpeedInfo.speed_pass);

                        TrackMonitorSignalAspect signalAspect = thisSignal.TranslateTMAspect(thisSignal.this_sig_lr(SignalHead.SIGFN.NORMAL));
                        thisItem = new TrainObjectItem(signalAspect, validSpeed, distanceToTrainM);
                        thisInfo.ObjectInfoBackward.Add(thisItem);
                    }

                    if (thisSection.CircuitItems.TrackCircuitSpeedPosts[sectionDirection] != null)
                    {
                        foreach (ORTS.TrackCircuitSignalItem thisSpeeditem in thisSection.CircuitItems.TrackCircuitSpeedPosts[sectionDirection].TrackCircuitItem)
                        {
                            SignalObject thisSpeedpost = thisSpeeditem.SignalRef;
                            ObjectSpeedInfo thisSpeedInfo = thisSpeedpost.this_sig_speed(SignalHead.SIGFN.SPEED);
                            float validSpeed = IsFreight ? thisSpeedInfo.speed_freight : thisSpeedInfo.speed_pass;

                            distanceToTrainM = sectionStart + thisSpeeditem.SignalLocation;

                            if (distanceToTrainM > 0 && validSpeed > 0)
                            {
                                thisItem = new TrainObjectItem(validSpeed, distanceToTrainM);
                                thisInfo.ObjectInfoBackward.Add(thisItem);
                            }
                        }
                    }

                    sectionStart += thisSection.Length;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Create TrackInfoObject for information in TrackMonitor window when OutOfControl
        /// </summary>

        public void GetTrainInfoOOC(ref TrainInfo thisInfo)
        {
            // set control mode
            thisInfo.ControlMode = ControlMode;

            // set speed
            thisInfo.speedMpS = SpeedMpS;

            // set projected speed
            thisInfo.projectedSpeedMpS = ProjectedSpeedMpS;

            // set max speed
            thisInfo.allowedSpeedMpS = AllowedMaxSpeedMpS;

            // set direction
            thisInfo.direction = MUDirection == Direction.Forward ? 0 : 1;

            // set orientation
            thisInfo.cabOrientation = (LeadLocomotive.Flipped ^ LeadLocomotive.GetCabFlipped()) ? 1 : 0;

            // set out of control reason
            TrainObjectItem thisItem = new TrainObjectItem(OutOfControlReason);
            thisInfo.ObjectInfoForward.Add(thisItem);
        }

        //================================================================================================//
        /// <summary>
        /// Create Track Circuit Route Path
        /// </summary>

        public void SetRoutePath(AIPath aiPath)
        {
#if DEBUG_TEST
            File.AppendAllText(@"C:\temp\TCSections.txt", "--------------------------------------------------\n");
            File.AppendAllText(@"C:\temp\TCSections.txt", "Train : " + Number.ToString() + "\n\n");
#endif
            TCRoute = new TCRoutePath(aiPath, (int)FrontTDBTraveller.Direction, Length, signalRef);
            ValidRoute[0] = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath];
        }


        //================================================================================================//
        /// <summary>
        /// Create Track Circuit Route Path
        /// </summary>

        public void SetRoutePath(AIPath aiPath, Signals orgSignals)
        {
#if DEBUG_TEST
            File.AppendAllText(@"C:\temp\TCSections.txt", "--------------------------------------------------\n");
            File.AppendAllText(@"C:\temp\TCSections.txt", "Train : " + Number.ToString() + "\n\n");
#endif
            int orgDirection = (RearTDBTraveller != null) ? (int)RearTDBTraveller.Direction : -2;
            TCRoute = new TCRoutePath(aiPath, orgDirection, 0, orgSignals);
            ValidRoute[0] = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath];
        }

        //================================================================================================//
        //
        // Preset switches for explorer mode
        //

        public void PresetExplorerPath(AIPath aiPath, Signals orgSignals)
        {
            int orgDirection = (RearTDBTraveller != null) ? (int)RearTDBTraveller.Direction : -2;
            TCRoute = new TCRoutePath(aiPath, orgDirection, 0, orgSignals);

            // loop through all sections in first subroute except first and last (neither can be junction)

            for (int iElement = 1; iElement <= TCRoute.TCRouteSubpaths[0].Count - 2; iElement++)
            {
                TrackCircuitSection thisSection = orgSignals.TrackCircuitList[TCRoute.TCRouteSubpaths[0][iElement].TCSectionIndex];
                int nextSectionIndex = TCRoute.TCRouteSubpaths[0][iElement + 1].TCSectionIndex;
                int prevSectionIndex = TCRoute.TCRouteSubpaths[0][iElement - 1].TCSectionIndex;

                // process Junction

                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                {
                    if (thisSection.Pins[0, 0].Link == nextSectionIndex)
                    {
                        thisSection.alignSwitchPins(prevSectionIndex);   // trailing switch
                    }
                    else
                    {
                        thisSection.alignSwitchPins(nextSectionIndex);   // facing switch
                    }
                }
            }
        }

        //================================================================================================//
        //
        // Extract alternative route
        //

        public TCSubpathRoute ExtractAlternativeRoute(int altRouteIndex)
        {
            TCSubpathRoute returnRoute = new TCSubpathRoute();

            // extract entries of alternative route upto first signal

            foreach (TCRouteElement thisElement in TCRoute.TCAlternativePaths[altRouteIndex])
            {
                returnRoute.Add(thisElement);
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                if (thisSection.EndSignals[thisElement.Direction] != null)
                {
                    break;
                }
            }

            return (returnRoute);
        }

        //================================================================================================//
        //
        // Set train route to alternative route
        //

        public virtual void SetAlternativeRoute(int startElementIndex, int altRouteIndex, SignalObject nextSignal)
        {

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number.ToString() +
                    " : set alternative route no. : " + altRouteIndex.ToString() +
                    " from section " + ValidRoute[0][startElementIndex].TCSectionIndex.ToString() +
                    " (request from signal " + nextSignal.thisRef.ToString() + " )\n");
#endif

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number.ToString() +
                " : set alternative route no. : " + altRouteIndex.ToString() +
                " from section " + ValidRoute[0][startElementIndex].TCSectionIndex.ToString() +
                " (request from signal " + nextSignal.thisRef.ToString() + " )\n");
            }

            // set new train route

            TCSubpathRoute thisRoute = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath];
            TCSubpathRoute newRoute = new TCSubpathRoute();

            TCSubpathRoute altRoute = TCRoute.TCAlternativePaths[altRouteIndex];
            TCRoute.activeAltpath = altRouteIndex;

            // part upto split

            for (int iElement = 0; iElement < startElementIndex; iElement++)
            {
                newRoute.Add(thisRoute[iElement]);
            }

            // alternative path

            for (int iElement = 0; iElement < altRoute.Count; iElement++)
            {
                newRoute.Add(altRoute[iElement]);
            }

            // continued path

            int lastAlternativeSectionIndex = thisRoute.GetRouteIndex(altRoute[altRoute.Count - 1].TCSectionIndex, startElementIndex);
            for (int iElement = lastAlternativeSectionIndex + 1; iElement < thisRoute.Count; iElement++)
            {
                newRoute.Add(thisRoute[iElement]);
            }

            // set new route

            ValidRoute[0] = newRoute;
            TCRoute.TCRouteSubpaths[TCRoute.activeSubpath] = newRoute;

            // set signal route
            // part upto split

            TCSubpathRoute newSignalRoute = new TCSubpathRoute();

            int splitSignalIndex = nextSignal.signalRoute.GetRouteIndex(thisRoute[startElementIndex].TCSectionIndex, 0);
            for (int iElement = 0; iElement < splitSignalIndex; iElement++)
            {
                newSignalRoute.Add(nextSignal.signalRoute[iElement]);
            }

            // extract new route upto next signal

            TCSubpathRoute nextPart = ExtractAlternativeRoute(altRouteIndex);
            foreach (TCRouteElement thisElement in nextPart)
            {
                newSignalRoute.Add(thisElement);
            }

            nextSignal.signalRoute = newSignalRoute;
        }

        //================================================================================================//
        /// <summary>
        /// Get other train from number
        /// Use Simulator.Trains to get other train
        /// </summary>

        public Train GetOtherTrainByNumber(int reqNumber)
        {
            return Simulator.Trains.GetTrainByNumber(reqNumber);
        }

        //================================================================================================//
        /// <summary>
        /// Routed train class : train class plus valid route direction indication
        /// Used throughout in the signalling process in order to derive correct route in Manual and Explorer modes
        /// </summary>

        public class TrainRouted
        {
            public Train Train;
            public int TrainRouteDirectionIndex;

            //================================================================================================//
            /// <summary>
            /// Constructor
            /// </summary>

            public TrainRouted(Train thisTrain, int thisIndex)
            {
                Train = thisTrain;
                TrainRouteDirectionIndex = thisIndex;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Track Circuit Route Path
        /// </summary>

        public class TCRoutePath
        {
            public List<TCSubpathRoute> TCRouteSubpaths = new List<TCSubpathRoute>();
            public List<TCSubpathRoute> TCAlternativePaths = new List<TCSubpathRoute>();
            public int activeSubpath;
            public int activeAltpath;
            public List<int[]> WaitingPoints = new List<int[]>(); //[0] = sublist in which WP is placed; 
                                                                  // [1] = WP section; [2] = WP wait time (delta); [3] = WP depart time;
                                                                  // [4] = hold signal
            public List<TCReversalInfo> ReversalInfo = new List<TCReversalInfo>();

            //================================================================================================//
            /// <summary>
            /// Constructor (from AIPath)
            /// </summary>

            public TCRoutePath(AIPath aiPath, int orgDir, float thisTrainLength, ORTS.Signals orgSignals)
            {

                activeSubpath = 0;
                activeAltpath = -1;

                //
                // collect all TC Elements
                //
                // get tracknode from first path node
                //

                int sublist = 0;

                Dictionary<int, int[]> AlternativeRoutes = new Dictionary<int, int[]>();
                Queue<int> ActiveAlternativeRoutes = new Queue<int>();

                TCSubpathRoute thisSubpath = new TCSubpathRoute();
                TCRouteSubpaths.Add(thisSubpath);

                int currentDir = orgDir;
                int newDir = orgDir;

                List<float> reversalOffset = new List<float>();
                List<int> reversalIndex = new List<int>();

                //
                // if original direction not set, determine it through first switch
                //

                if (orgDir < -1)
                {
                    bool firstSwitch = false;
                    int prevTNode = 0;
                    int jnDir = 0;

                    for (int iPNode = 0; iPNode < aiPath.Nodes.Count - 1 && !firstSwitch; iPNode++)
                    {
                        AIPathNode pNode = aiPath.Nodes[iPNode];
                        if (pNode.JunctionIndex > 0)
                        {
                            TrackNode jn = aiPath.TrackDB.TrackNodes[pNode.JunctionIndex];
                            firstSwitch = true;
                            for (int iPin = 0; iPin < jn.TrPins.Length; iPin++)
                            {
                                if (jn.TrPins[iPin].Link == prevTNode)
                                {
                                    jnDir = jn.TrPins[iPin].Direction == 1 ? 0 : 1;
                                }
                            }
                        }
                        else
                        {
                            if (pNode.Type == AIPathNodeType.Other)
                                prevTNode = pNode.NextMainTVNIndex;
                        }
                    }

                    currentDir = jnDir;
                }

                //
                // loop through path nodes
                //

                AIPathNode thisPathNode = aiPath.Nodes[0];
                AIPathNode nextPathNode = null;
                AIPathNode lastPathNode = null;

                int trackNodeIndex = thisPathNode.NextMainTVNIndex;
                TrackNode thisNode = null;

                thisPathNode = thisPathNode.NextMainNode;
                int reversal = 0;
                bool breakpoint = false;

                while (thisPathNode != null)
                {
                    lastPathNode = thisPathNode;

                    // process siding items

                    if (thisPathNode.Type == AIPathNodeType.SidingStart)
                    {
                        TrackNode sidingNode = aiPath.TrackDB.TrackNodes[thisPathNode.JunctionIndex];
                        int startTCSectionIndex = sidingNode.TCCrossReference[0].CrossRefIndex;
                        int[] altRouteReference = new int[3];
                        altRouteReference[0] = sublist;
                        altRouteReference[1] = thisPathNode.Index;
                        altRouteReference[2] = -1;
                        AlternativeRoutes.Add(startTCSectionIndex, altRouteReference);
                        ActiveAlternativeRoutes.Enqueue(startTCSectionIndex);

                        thisPathNode.Type = AIPathNodeType.Other;
                    }
                    else if (thisPathNode.Type == AIPathNodeType.SidingEnd)
                    {
                        TrackNode sidingNode = aiPath.TrackDB.TrackNodes[thisPathNode.JunctionIndex];
                        int endTCSectionIndex = sidingNode.TCCrossReference[0].CrossRefIndex;

                        int refStartIndex = ActiveAlternativeRoutes.Dequeue();
                        int[] altRouteReference = AlternativeRoutes[refStartIndex];
                        altRouteReference[2] = endTCSectionIndex;

                        thisPathNode.Type = AIPathNodeType.Other;
                    }

                    //
                    // process last non-junction section
                    //

                    if (thisPathNode.Type == AIPathNodeType.Other)
                    {
                        thisNode = aiPath.TrackDB.TrackNodes[trackNodeIndex];

                        if (currentDir == 0)
                        {
                            for (int iTC = 0; iTC < thisNode.TCCrossReference.Count; iTC++)
                            {
                                TCRouteElement thisElement =
                                    new TCRouteElement(thisNode, iTC, currentDir, orgSignals);
                                thisSubpath.Add(thisElement);
                            }
                            newDir = thisNode.TrPins[currentDir].Direction;

                        }
                        else
                        {
                            for (int iTC = thisNode.TCCrossReference.Count - 1; iTC >= 0; iTC--)
                            {
                                TCRouteElement thisElement =
                                    new TCRouteElement(thisNode, iTC, currentDir, orgSignals);
                                thisSubpath.Add(thisElement);
                            }
                            newDir = thisNode.TrPins[currentDir].Direction;
                        }

                        if (reversal > 0)
                        {
                            while (reversal > 0)
                            {
                                sublist++;
                                thisSubpath = new TCSubpathRoute();
                                TCRouteSubpaths.Add(thisSubpath);
                                currentDir = currentDir == 1 ? 0 : 1;
                                reversal--;        // reset reverse point
                            }
                            continue;          // process this node again in reverse direction
                        }
                        else if (breakpoint)
                        {
                            sublist++;
                            thisSubpath = new TCSubpathRoute();
                            TCRouteSubpaths.Add(thisSubpath);

                            breakpoint = false;
                        }

                        //
                        // process junction section
                        //

                        if (thisPathNode.JunctionIndex > 0)
                        {
                            TrackNode junctionNode = aiPath.TrackDB.TrackNodes[thisPathNode.JunctionIndex];
                            TCRouteElement thisElement =
                                new TCRouteElement(junctionNode, 0, newDir, orgSignals);
                            thisSubpath.Add(thisElement);

                            trackNodeIndex = thisPathNode.NextMainTVNIndex;

                            if (thisPathNode.IsFacingPoint)   // exit is one of two switch paths //
                            {
                                uint firstpin = (junctionNode.Inpins > 1) ? 0 : junctionNode.Inpins;
                                if (junctionNode.TrPins[firstpin].Link == trackNodeIndex)
                                {
                                    newDir = junctionNode.TrPins[firstpin].Direction;
                                    thisElement.OutPin[1] = 0;
                                }
                                else
                                {
                                    firstpin++;
                                    newDir = junctionNode.TrPins[firstpin].Direction;
                                    thisElement.OutPin[1] = 1;
                                }
                            }
                            else  // exit is single path //
                            {
                                uint firstpin = (junctionNode.Inpins > 1) ? junctionNode.Inpins : 0;
                                newDir = junctionNode.TrPins[firstpin].Direction;
                            }
                        }

                        currentDir = newDir;

                        //
                        // find next junction path node
                        //

                        nextPathNode = thisPathNode.NextMainNode;
                    }
                    else
                    {
                        nextPathNode = thisPathNode;
                    }

                    while (nextPathNode != null && nextPathNode.JunctionIndex < 0)
                    {
                        lastPathNode = nextPathNode;

                        if (nextPathNode.Type == AIPathNodeType.Reverse)
                        {
                            TrackNode reversalNode = aiPath.TrackDB.TrackNodes[nextPathNode.NextMainTVNIndex];
                            TrVectorSection firstSection = reversalNode.TrVectorNode.TrVectorSections[0];
                            Traveller TDBTrav = new Traveller(aiPath.TSectionDat, aiPath.TrackDB.TrackNodes, reversalNode,
                                            firstSection.TileX, firstSection.TileZ,
                                            firstSection.X, firstSection.Z, (Traveller.TravellerDirection)1);
                            float offset = TDBTrav.DistanceTo(reversalNode,
                                nextPathNode.Location.TileX, nextPathNode.Location.TileZ,
                                nextPathNode.Location.Location.X,
                                nextPathNode.Location.Location.Y,
                                nextPathNode.Location.Location.Z);

                            reversalOffset.Add(offset);
                            reversalIndex.Add(sublist);
                            reversal++;
                        }
                        else if (nextPathNode.Type == AIPathNodeType.Stop)
                        {
                            int[] waitingPoint = new int[5];
                            waitingPoint[0] = sublist;
                            waitingPoint[1] = ConvertWaitingPoint(nextPathNode, aiPath.TrackDB, aiPath.TSectionDat, currentDir);

                            waitingPoint[2] = nextPathNode.WaitTimeS;
                            waitingPoint[3] = nextPathNode.WaitUntil;
                            waitingPoint[4] = -1; // hold signal set later

                            WaitingPoints.Add(waitingPoint);
                            breakpoint = true;
                        }

                        // other type of path need not be processed

                        // go to next node
                        nextPathNode = nextPathNode.NextMainNode;
                    }

                    thisPathNode = nextPathNode;
                }

                //
                // add last section
                //

                thisNode = aiPath.TrackDB.TrackNodes[trackNodeIndex];
                TrVectorSection endFirstSection = thisNode.TrVectorNode.TrVectorSections[0];
                Traveller TDBEndTrav = new Traveller(aiPath.TSectionDat, aiPath.TrackDB.TrackNodes, thisNode,
                                endFirstSection.TileX, endFirstSection.TileZ,
                                endFirstSection.X, endFirstSection.Z, (Traveller.TravellerDirection)1);
                float endOffset = TDBEndTrav.DistanceTo(thisNode,
                    lastPathNode.Location.TileX, lastPathNode.Location.TileZ,
                    lastPathNode.Location.Location.X,
                    lastPathNode.Location.Location.Y,
                    lastPathNode.Location.Location.Z);

                if (currentDir == 0)
                {
                    for (int iTC = 0; iTC < thisNode.TCCrossReference.Count; iTC++)
                    {
                        if ((thisNode.TCCrossReference[iTC].Position[1] + thisNode.TCCrossReference[iTC].Length) > endOffset)
                        //                      if (thisNode.TCCrossReference[iTC].Position[0] < endOffset)
                        {
                            TCRouteElement thisElement =
                                new TCRouteElement(thisNode, iTC, currentDir, orgSignals);
                            if (thisSubpath.Count <= 0 || thisSubpath[thisSubpath.Count-1].TCSectionIndex != thisElement.TCSectionIndex) thisSubpath.Add(thisElement); // only add if not yet set
                        }
                    }
                }
                else
                {
                    for (int iTC = thisNode.TCCrossReference.Count - 1; iTC >= 0; iTC--)
                    {
                        if (thisNode.TCCrossReference[iTC].Position[1] < endOffset)
                        {
                            TCRouteElement thisElement =
                            new TCRouteElement(thisNode, iTC, currentDir, orgSignals);
                            if (thisSubpath.Count <= 0 || thisSubpath[thisSubpath.Count - 1].TCSectionIndex != thisElement.TCSectionIndex) thisSubpath.Add(thisElement); // only add if not yet set
                        }
                    }
                }

                // check if section extends to end of track

                TCRouteElement lastElement = thisSubpath[thisSubpath.Count - 1];
                TrackCircuitSection lastEndSection = orgSignals.TrackCircuitList[lastElement.TCSectionIndex];
                int lastDirection = lastElement.Direction;

                List<TCRouteElement> addedElements = new List<TCRouteElement>();
                if (lastEndSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
                {
                    int thisDirection = lastDirection;
                    lastDirection = lastEndSection.Pins[thisDirection, 0].Direction;
                    lastEndSection = orgSignals.TrackCircuitList[lastEndSection.Pins[thisDirection, 0].Link];

                    while (lastEndSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.NORMAL)
                    {
                        addedElements.Add(new TCRouteElement(lastEndSection.Index, lastDirection));
                        thisDirection = lastDirection;
                        lastDirection = lastEndSection.Pins[thisDirection, 0].Direction;
                        lastEndSection = orgSignals.TrackCircuitList[lastEndSection.Pins[thisDirection, 0].Link];
                    }

                    if (lastEndSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
                    {
                        foreach (TCRouteElement addedElement in addedElements)
                        {
                            thisSubpath.Add(addedElement);
                        }
                        thisSubpath.Add(new TCRouteElement(lastEndSection.Index, lastDirection));
                    }
                }


                // remove sections beyond reversal points

                for (int iSub = 0; iSub < reversalOffset.Count; iSub++)  // no reversal for final path
                {
                    TCSubpathRoute revSubPath = TCRouteSubpaths[reversalIndex[iSub]];
                    float offset = reversalOffset[iSub];
                    if (revSubPath.Count <= 0)
                        continue;

                    int direction = revSubPath[revSubPath.Count - 1].Direction;

                    bool withinOffset = true;
                    List<int> removeSections = new List<int>();
                    int lastSectionIndex = revSubPath.Count - 1;

                    // create list of sections beyond reversal point 

                    if (direction == 0)
                    {
                        for (int iSection = revSubPath.Count - 1; iSection > 0 && withinOffset; iSection--)
                        {
                            TrackCircuitSection thisSection = orgSignals.TrackCircuitList[revSubPath[iSection].TCSectionIndex];
                            if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                            {
                                withinOffset = false;    // always end on junction (next node)
                            }
                            else if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                            {
                                removeSections.Add(iSection);        // always remove crossover if last section was removed
                                lastSectionIndex = iSection - 1;
                            }
                            else if (thisSection.OffsetLength[1] + thisSection.Length < offset) // always use offsetLength[1] as offset is wrt begin of original section
                            {
                                removeSections.Add(iSection);
                                lastSectionIndex = iSection - 1;
                            }
                            else
                            {
                                withinOffset = false;
                            }
                        }
                    }
                    else
                    {
                        for (int iSection = revSubPath.Count - 1; iSection > 0 && withinOffset; iSection--)
                        {
                            TrackCircuitSection thisSection = orgSignals.TrackCircuitList[revSubPath[iSection].TCSectionIndex];
                            if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.JUNCTION)
                            {
                                withinOffset = false;     // always end on junction (next node)
                            }
                            else if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                            {
                                removeSections.Add(iSection);        // always remove crossover if last section was removed
                                lastSectionIndex = iSection - 1;
                            }
                            else if (thisSection.OffsetLength[1] > offset)
                            {
                                removeSections.Add(iSection);
                                lastSectionIndex = iSection - 1;
                            }
                            else
                            {
                                withinOffset = false;
                            }
                        }
                    }

                    // extend route to first signal or first node

                    bool signalFound = false;

                    for (int iSection = lastSectionIndex; iSection < revSubPath.Count - 1 && !signalFound; iSection++)
                    {
                        TrackCircuitSection thisSection = orgSignals.TrackCircuitList[revSubPath[iSection].TCSectionIndex];
                        removeSections.Remove(iSection);
                        if (thisSection.EndSignals[direction] != null)
                        {
                            signalFound = true;
                        }
                    }

                    // remove sections beyond first signal or first node from reversal point

                    for (int iSection = 0; iSection < removeSections.Count; iSection++)
                    {
                        revSubPath.RemoveAt(removeSections[iSection]);
                    }
                }

                // remove dummy subpaths (from double reversion)

                List<int> subRemoved = new List<int>();
                int orgCount = TCRouteSubpaths.Count;
                int removed = 0;
                Dictionary<int, int> newIndices = new Dictionary<int, int>();

                for (int iSub = TCRouteSubpaths.Count - 1; iSub >= 0; iSub--)
                {
                    if (TCRouteSubpaths[iSub].Count <= 0)
                    {
                        TCRouteSubpaths.RemoveAt(iSub);
                        subRemoved.Add(iSub);
                    }
                }

                // calculate new indices
                for (int iSub = 0; iSub <= orgCount - 1; iSub++)
                {
                    if (subRemoved.Contains(iSub))
                    {
                        removed++;
                    }
                    else
                    {
                        newIndices.Add(iSub, iSub - removed);
                    }
                }


                // if removed, update indices of waiting points
                if (removed > 0)
                {
                    foreach (int[] thisWP in WaitingPoints)
                    {
                        thisWP[0] = newIndices[thisWP[0]];
                    }

                    // if remove, update indices of alternative paths
                    foreach (KeyValuePair<int, int[]> thisAltPath in AlternativeRoutes)
                    {
                        TCSubpathRoute thisAltpath = new TCSubpathRoute();

                        int startSection = thisAltPath.Key;
                        int[] pathDetails = thisAltPath.Value;
                        int sublistRef = pathDetails[0];

                        int newSublistRef = newIndices[sublistRef];
                        pathDetails[0] = newSublistRef;
                    }
                }

                // find if last stretch is dummy track

                // first, find last signal - there may not be a junction between last signal and end
                // last end must be end-of-track

                foreach (TCSubpathRoute endSubPath in TCRouteSubpaths)
                {
                    int lastIndex = endSubPath.Count - 1;
                    TCRouteElement thisElement = endSubPath[lastIndex];
                    TrackCircuitSection lastSection = orgSignals.TrackCircuitList[thisElement.TCSectionIndex];

                    // build additional route from end of last section but not further than train length

                    int nextSectionIndex = lastSection.ActivePins[thisElement.OutPin[0], thisElement.OutPin[1]].Link;
                    int nextDirection = lastSection.ActivePins[thisElement.OutPin[0], thisElement.OutPin[1]].Direction;
                    int lastUseIndex = lastIndex - 1;  // do not use final element if this is end of track

                    List<int> addSections = new List<int>();

                    if (nextSectionIndex > 0)
                    {
                        lastUseIndex = lastIndex;  // last element is not end of track
                        addSections = orgSignals.ScanRoute(null, nextSectionIndex, 0.0f, nextDirection,
                           true, thisTrainLength, false, true, true, false, true, false, false, false, false, false);

                        if (addSections.Count > 0)
                        {
                            lastSection = orgSignals.TrackCircuitList[Math.Abs(addSections[addSections.Count - 1])];
                        }
                    }

                    if (lastSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.END_OF_TRACK)
                    {

                        // first length of added sections

                        float totalLength = 0.0f;
                        bool juncfound = false;

                        for (int iSection = 0; iSection < addSections.Count - 2; iSection++)  // exclude end of track
                        {
                            TrackCircuitSection thisSection = orgSignals.TrackCircuitList[Math.Abs(addSections[iSection])];
                            totalLength += thisSection.Length;
                            if (thisSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.NORMAL)
                            {
                                juncfound = true;
                            }
                        }

                        // next length of sections back to last signal
                        // stop loop : when junction found, when signal found, when length exceeds train length

                        int sigIndex = -1;

                        for (int iSection = lastUseIndex;
                                iSection >= 0 && sigIndex < 0 && !juncfound && totalLength < 0.5 * thisTrainLength;
                                iSection--)
                        {
                            thisElement = endSubPath[iSection];
                            TrackCircuitSection thisSection = orgSignals.TrackCircuitList[thisElement.TCSectionIndex];

                            if (thisSection.EndSignals[thisElement.Direction] != null)
                            {
                                sigIndex = iSection;
                            }
                            else if (thisSection.CircuitType != TrackCircuitSection.CIRCUITTYPE.NORMAL)
                            {
                                juncfound = true;
                            }
                            else
                            {
                                totalLength += thisSection.Length;
                            }
                        }

                        // remove dummy ends

                        if (sigIndex > 0 && totalLength < 0.5f * thisTrainLength)
                        {
                            for (int iSection = endSubPath.Count - 1; iSection > sigIndex; iSection--)
                            {
                                endSubPath.RemoveAt(iSection);
                            }
                        }
                    }
                }

                // for reversals, find actual diverging section

                int prevDivergeSectorIndex = -1;
                for (int iSubpath = 1; iSubpath < TCRouteSubpaths.Count; iSubpath++)
                {
                    TCReversalInfo reversalInfo = new TCReversalInfo(TCRouteSubpaths[iSubpath - 1], prevDivergeSectorIndex,
                        TCRouteSubpaths[iSubpath], orgSignals);
                    ReversalInfo.Add(reversalInfo);
                    prevDivergeSectorIndex = reversalInfo.Valid ? reversalInfo.FirstDivergeIndex : -1;
                }
                ReversalInfo.Add(new TCReversalInfo());  // add invalid item to make up the numbers (equals no. subpaths)

                // process alternative paths

                int altlist = 0;

                foreach (KeyValuePair<int, int[]> thisAltPath in AlternativeRoutes)
                {
                    TCSubpathRoute thisAltpath = new TCSubpathRoute();

                    int startSection = thisAltPath.Key;
                    int[] pathDetails = thisAltPath.Value;
                    int sublistRef = pathDetails[0];

                    int startSectionRouteIndex = TCRouteSubpaths[sublistRef].GetRouteIndex(startSection, 0);
                    int endSectionRouteIndex = -1;

                    int endSection = pathDetails[2];
                    if (endSection < 0)
                    {
                        Trace.TraceInformation("No end-index found for alternative path starting at " + startSection.ToString());
                    }
                    else
                    {
                        endSectionRouteIndex = TCRouteSubpaths[sublistRef].GetRouteIndex(endSection, 0);
                    }

                    if (startSectionRouteIndex < 0 || endSectionRouteIndex < 0)
                    {
                        Trace.TraceInformation("Start section " + startSection.ToString() + "or end section " + endSection.ToString() +
                                               " for alternative path not in subroute " + sublistRef.ToString());
                    }
                    else
                    {
                        TCRouteElement startElement = TCRouteSubpaths[sublistRef][startSectionRouteIndex];
                        TCRouteElement endElement = TCRouteSubpaths[sublistRef][endSectionRouteIndex];

                        startElement.StartAlternativePath = new int[2];
                        startElement.StartAlternativePath[0] = altlist;
                        startElement.StartAlternativePath[1] = endSection;

                        endElement.EndAlternativePath = new int[2];
                        endElement.EndAlternativePath[0] = altlist;
                        endElement.EndAlternativePath[1] = startSection;

                        currentDir = startElement.Direction;

                        //
                        // loop through path nodes
                        //

                        thisPathNode = aiPath.Nodes[pathDetails[1]];
                        nextPathNode = null;

                        // process junction node

                        TrackNode firstJunctionNode = aiPath.TrackDB.TrackNodes[thisPathNode.JunctionIndex];
                        TCRouteElement thisJunctionElement =
                            new TCRouteElement(firstJunctionNode, 0, currentDir, orgSignals);
                        thisAltpath.Add(thisJunctionElement);

                        trackNodeIndex = thisPathNode.NextSidingTVNIndex;

                        uint firstJunctionPin = (firstJunctionNode.Inpins > 1) ? 0 : firstJunctionNode.Inpins;
                        if (firstJunctionNode.TrPins[firstJunctionPin].Link == trackNodeIndex)
                        {
                            currentDir = firstJunctionNode.TrPins[firstJunctionPin].Direction;
                            thisJunctionElement.OutPin[1] = 0;
                        }
                        else
                        {
                            firstJunctionPin++;
                            currentDir = firstJunctionNode.TrPins[firstJunctionPin].Direction;
                            thisJunctionElement.OutPin[1] = 1;
                        }

                        // process alternative path

                        thisNode = null;
                        thisPathNode = thisPathNode.NextSidingNode;

                        while (thisPathNode != null)
                        {

                            //
                            // process last non-junction section
                            //

                            if (thisPathNode.Type == AIPathNodeType.Other)
                            {
                                if (trackNodeIndex > 0)
                                {
                                    thisNode = aiPath.TrackDB.TrackNodes[trackNodeIndex];

                                    if (currentDir == 0)
                                    {
                                        for (int iTC = 0; iTC < thisNode.TCCrossReference.Count; iTC++)
                                        {
                                            TCRouteElement thisElement =
                                                new TCRouteElement(thisNode, iTC, currentDir, orgSignals);
                                            thisAltpath.Add(thisElement);
                                        }
                                        newDir = thisNode.TrPins[currentDir].Direction;

                                    }
                                    else
                                    {
                                        for (int iTC = thisNode.TCCrossReference.Count - 1; iTC >= 0; iTC--)
                                        {
                                            TCRouteElement thisElement =
                                                new TCRouteElement(thisNode, iTC, currentDir, orgSignals);
                                            thisAltpath.Add(thisElement);
                                        }
                                        newDir = thisNode.TrPins[currentDir].Direction;
                                    }
                                    trackNodeIndex = -1;
                                }

                                //
                                // process junction section
                                //

                                if (thisPathNode.JunctionIndex > 0)
                                {
                                    TrackNode junctionNode = aiPath.TrackDB.TrackNodes[thisPathNode.JunctionIndex];
                                    TCRouteElement thisElement =
                                        new TCRouteElement(junctionNode, 0, newDir, orgSignals);
                                    thisAltpath.Add(thisElement);

                                    trackNodeIndex = thisPathNode.NextSidingTVNIndex;

                                    if (thisPathNode.IsFacingPoint)   // exit is one of two switch paths //
                                    {
                                        uint firstpin = (junctionNode.Inpins > 1) ? 0 : junctionNode.Inpins;
                                        if (junctionNode.TrPins[firstpin].Link == trackNodeIndex)
                                        {
                                            newDir = junctionNode.TrPins[firstpin].Direction;
                                            thisElement.OutPin[1] = 0;
                                        }
                                        else
                                        {
                                            firstpin++;
                                            newDir = junctionNode.TrPins[firstpin].Direction;
                                            thisElement.OutPin[1] = 1;
                                        }
                                    }
                                    else  // exit is single path //
                                    {
                                        uint firstpin = (junctionNode.Inpins > 1) ? junctionNode.Inpins : 0;
                                        newDir = junctionNode.TrPins[firstpin].Direction;
                                    }
                                }

                                currentDir = newDir;

                                //
                                // find next junction path node
                                //

                                nextPathNode = thisPathNode.NextSidingNode;
                            }
                            else
                            {
                                nextPathNode = thisPathNode;
                            }

                            while (nextPathNode != null && nextPathNode.JunctionIndex < 0)
                            {
                                nextPathNode = nextPathNode.NextSidingNode;
                            }

                            lastPathNode = thisPathNode;
                            thisPathNode = nextPathNode;
                        }
                        //
                        // add last section
                        //

                        if (trackNodeIndex > 0)
                        {
                            thisNode = aiPath.TrackDB.TrackNodes[trackNodeIndex];

                            if (currentDir == 0)
                            {
                                for (int iTC = 0; iTC < thisNode.TCCrossReference.Count; iTC++)
                                {
                                    TCRouteElement thisElement =
                                        new TCRouteElement(thisNode, iTC, currentDir, orgSignals);
                                    thisAltpath.Add(thisElement);
                                }
                            }
                            else
                            {
                                for (int iTC = thisNode.TCCrossReference.Count - 1; iTC >= 0; iTC--)
                                {
                                    TCRouteElement thisElement =
                                        new TCRouteElement(thisNode, iTC, currentDir, orgSignals);
                                    thisAltpath.Add(thisElement);
                                }
                            }
                        }

                        TCAlternativePaths.Add(thisAltpath);
                        altlist++;
                    }
                }

#if DEBUG_TEST
                for (int iSub = 0; iSub < TCRouteSubpaths.Count; iSub++)
                {
                    TCSubpathRoute printSubpath = TCRouteSubpaths[iSub];
                    File.AppendAllText(@"C:\temp\TCSections.txt", "\n-- Subpath : " + iSub.ToString() + " --\n\n");

                    foreach (TCRouteElement printElement in printSubpath)
                    {
                        File.AppendAllText(@"C:\temp\TCSections.txt", " TC Index   : " + printElement.TCSectionIndex.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\TCSections.txt", " direction  : " + printElement.Direction.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\TCSections.txt",
                            " outpins    : " + printElement.OutPin[0].ToString() + " - " + printElement.OutPin[1].ToString() + "\n");
                        if (printElement.StartAlternativePath != null)
                        {
                            File.AppendAllText(@"C:\temp\TCSections.txt", "\n Start Alternative Path : " +
					    printElement.StartAlternativePath[0].ToString() +
					    " upto section " + printElement.StartAlternativePath[1].ToString() + "\n");
                        }
                        if (printElement.EndAlternativePath != null)
                        {
                            File.AppendAllText(@"C:\temp\TCSections.txt", "\n End Alternative Path : " +
					    printElement.EndAlternativePath[0].ToString() +
					    " from section " + printElement.EndAlternativePath[1].ToString() + "\n");
                        }

                        File.AppendAllText(@"C:\temp\TCSections.txt", "\n");
                    }

                    if (iSub < TCRouteSubpaths.Count - 1)
                    {
                        File.AppendAllText(@"C:\temp\TCSections.txt", "\n-- reversal --\n");
                    }
                }

                for (int iAlt = 0; iAlt < TCAlternativePaths.Count; iAlt++)
                {
                    File.AppendAllText(@"C:\temp\TCSections.txt", "--------------------------------------------------\n");

                    TCSubpathRoute printSubpath = TCAlternativePaths[iAlt];
                    File.AppendAllText(@"C:\temp\TCSections.txt", "\n-- Alternative path : " + iAlt.ToString() + " --\n\n");

                    foreach (TCRouteElement printElement in printSubpath)
                    {
                        File.AppendAllText(@"C:\temp\TCSections.txt", " TC Index   : " + printElement.TCSectionIndex.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\TCSections.txt", " direction  : " + printElement.Direction.ToString() + "\n");
                        File.AppendAllText(@"C:\temp\TCSections.txt",
                            " outpins    : " + printElement.OutPin[0].ToString() + " - " + printElement.OutPin[1].ToString() + "\n");
                        File.AppendAllText(@"C:\temp\TCSections.txt", "\n");
                    }
                }

                for (int iRI = 0; iRI < ReversalInfo.Count; iRI++)
                {
                    File.AppendAllText(@"C:\temp\TCSections.txt", "--------------------------------------------------\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "\n-- Reversal Info : " + iRI.ToString() + " --\n\n");
                    TCReversalInfo thisReversalInfo = ReversalInfo[iRI];

                    File.AppendAllText(@"C:\temp\TCSections.txt", "Diverge sector : " + thisReversalInfo.DivergeSectorIndex.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Diverge offset : " + thisReversalInfo.DivergeOffset.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "First Index    : " + thisReversalInfo.FirstDivergeIndex.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "First Signal   : " + thisReversalInfo.FirstSignalIndex.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Last Index     : " + thisReversalInfo.LastDivergeIndex.ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Last Signal    : " + thisReversalInfo.LastSignalIndex.ToString() + "\n");
                }

                for (int iWP = 0; iWP < WaitingPoints.Count; iWP++)
                {

                    File.AppendAllText(@"C:\temp\TCSections.txt", "--------------------------------------------------\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "\n-- Waiting Point Info : " + iWP.ToString() + " --\n\n");
                    int[] thisWaitingPoint = WaitingPoints[iWP];

                    File.AppendAllText(@"C:\temp\TCSections.txt", "Sublist   : " + thisWaitingPoint[0].ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Section   : " + thisWaitingPoint[1].ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Wait time : " + thisWaitingPoint[2].ToString() + "\n");
                    File.AppendAllText(@"C:\temp\TCSections.txt", "Dep time  : " + thisWaitingPoint[3].ToString() + "\n");
                }

                File.AppendAllText(@"C:\temp\TCSections.txt", "--------------------------------------------------\n");
#endif

            }

            //================================================================================================//
            //
            // Constructor from existing path
            //

            public TCRoutePath(TCRoutePath otherPath)
            {
                activeSubpath = otherPath.activeSubpath;
                activeAltpath = otherPath.activeAltpath;

                for (int iSubpath = 0; iSubpath < otherPath.TCRouteSubpaths.Count; iSubpath++)
                {
                    TCSubpathRoute newSubpath = new TCSubpathRoute(otherPath.TCRouteSubpaths[iSubpath]);
                    TCRouteSubpaths.Add(newSubpath);
                }

                for (int iAltpath = 0; iAltpath < otherPath.TCAlternativePaths.Count; iAltpath++)
                {
                    TCSubpathRoute newAltpath = new TCSubpathRoute(otherPath.TCAlternativePaths[iAltpath]);
                    TCAlternativePaths.Add(newAltpath);
                }

                for (int iWaitingPoint = 0; iWaitingPoint < otherPath.WaitingPoints.Count; iWaitingPoint++)
                {
                    int[] oldWaitingPoint = otherPath.WaitingPoints[iWaitingPoint];
                    int[] newWaitingPoint = new int[oldWaitingPoint.Length];
                    oldWaitingPoint.CopyTo(newWaitingPoint, 0);
                    WaitingPoints.Add(newWaitingPoint);
                }

                for (int iReversalPoint = 0; iReversalPoint < otherPath.ReversalInfo.Count; iReversalPoint++)
                {
                    if (otherPath.ReversalInfo[iReversalPoint] == null)
                    {
                        ReversalInfo.Add(null);
                    }
                    else
                    {
                        TCReversalInfo reversalInfo = new TCReversalInfo(otherPath.ReversalInfo[iReversalPoint]);
                        ReversalInfo.Add(reversalInfo);
                    }
                }
            }

            //================================================================================================//
            //
            // Restore
            //

            public TCRoutePath(BinaryReader inf)
            {
                activeSubpath = inf.ReadInt32();
                activeAltpath = inf.ReadInt32();

                int totalSubpath = inf.ReadInt32();
                for (int iSubpath = 0; iSubpath < totalSubpath; iSubpath++)
                {
                    TCSubpathRoute thisSubpath = new TCSubpathRoute(inf);
                    TCRouteSubpaths.Add(thisSubpath);
                }

                int totalAltpath = inf.ReadInt32();
                for (int iAltpath = 0; iAltpath < totalAltpath; iAltpath++)
                {
                    TCSubpathRoute thisSubpath = new TCSubpathRoute(inf);
                    TCAlternativePaths.Add(thisSubpath);
                }

                int totalWaitingPoint = inf.ReadInt32();
                for (int iWP = 0; iWP < totalWaitingPoint; iWP++)
                {
                    int[] waitingPoint = new int[5];
                    waitingPoint[0] = inf.ReadInt32();
                    waitingPoint[1] = inf.ReadInt32();
                    waitingPoint[2] = inf.ReadInt32();
                    waitingPoint[3] = inf.ReadInt32();
                    waitingPoint[4] = inf.ReadInt32();

                    WaitingPoints.Add(waitingPoint);
                }

                int totalReversalPoint = inf.ReadInt32();
                for (int iRP = 0; iRP < totalReversalPoint; iRP++)
                {
                    ReversalInfo.Add(new TCReversalInfo(inf));
                }
            }

            //================================================================================================//
            //
            // Save
            //

            public void Save(BinaryWriter outf)
            {
                outf.Write(activeSubpath);
                outf.Write(activeAltpath);
                outf.Write(TCRouteSubpaths.Count);
                foreach (TCSubpathRoute thisSubpath in TCRouteSubpaths)
                {
                    thisSubpath.Save(outf);
                }

                outf.Write(TCAlternativePaths.Count);
                foreach (TCSubpathRoute thisAltpath in TCAlternativePaths)
                {
                    thisAltpath.Save(outf);
                }

                outf.Write(WaitingPoints.Count);
                foreach (int[] waitingPoint in WaitingPoints)
                {
                    outf.Write(waitingPoint[0]);
                    outf.Write(waitingPoint[1]);
                    outf.Write(waitingPoint[2]);
                    outf.Write(waitingPoint[3]);
                    outf.Write(waitingPoint[4]);
                }

                outf.Write(ReversalInfo.Count);
                for (int iRP = 0; iRP < ReversalInfo.Count; iRP++)
                {
                    ReversalInfo[iRP].Save(outf);
                }
            }

            //================================================================================================//
            //
            // Convert waiting point to section no.
            //

            private int ConvertWaitingPoint(AIPathNode stopPathNode, TrackDB TrackDB, TSectionDatFile TSectionDat, int direction)
            {
                TrackNode waitingNode = TrackDB.TrackNodes[stopPathNode.NextMainTVNIndex];
                TrVectorSection firstSection = waitingNode.TrVectorNode.TrVectorSections[0];
                Traveller TDBTrav = new Traveller(TSectionDat, TrackDB.TrackNodes, waitingNode,
                                firstSection.TileX, firstSection.TileZ,
                                firstSection.X, firstSection.Z, (Traveller.TravellerDirection)1);
                float offset = TDBTrav.DistanceTo(waitingNode,
                    stopPathNode.Location.TileX, stopPathNode.Location.TileZ,
                    stopPathNode.Location.Location.X,
                    stopPathNode.Location.Location.Y,
                    stopPathNode.Location.Location.Z);

                int TCSectionIndex = -1;

                for (int iXRef = waitingNode.TCCrossReference.Count - 1; iXRef >= 0 && TCSectionIndex < 0; iXRef--)
                {
                    if (offset <
                     (waitingNode.TCCrossReference[iXRef].Position[1] + waitingNode.TCCrossReference[iXRef].Length))
                    {
                        TCSectionIndex = waitingNode.TCCrossReference[iXRef].CrossRefIndex;
                    }
                }

                if (TCSectionIndex < 0)
                {
                    TCSectionIndex = waitingNode.TCCrossReference[0].CrossRefIndex;
                }

                return TCSectionIndex;
            }

            //================================================================================================//
            //
            // Check for reversal offset margin
            //

            public void SetReversalOffset(float trainLength)
            {
                TCReversalInfo thisReversal = ReversalInfo[activeSubpath];
                thisReversal.SignalUsed = thisReversal.Valid && thisReversal.SignalAvailable && trainLength < thisReversal.SignalOffset;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Track Circuit Route Element
        /// </summary>

        public class TCRouteElement
        {
            public int TCSectionIndex;
            public int Direction;
            public int[] OutPin = new int[2];
            public int[] StartAlternativePath = null;  // if used : index 0 = index of alternative path, index 1 = TC end index
            public int[] EndAlternativePath = null;  // if used : index 0 = index of alternative path, index 1 = TC start index

            //================================================================================================//
            /// <summary>
            /// Constructor from tracknode
            /// </summary>

            public TCRouteElement(TrackNode thisNode, int TCIndex, int direction, ORTS.Signals mySignals)
            {
                TCSectionIndex = thisNode.TCCrossReference[TCIndex].CrossRefIndex;
                Direction = direction;
                OutPin[0] = direction;
                OutPin[1] = 0;           // always 0 for NORMAL sections, updated for JUNCTION sections

                TrackCircuitSection thisSection = mySignals.TrackCircuitList[TCSectionIndex];
                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                {
                    int outPinLink = direction;
                    int nextIndex = thisNode.TCCrossReference[TCIndex + 1].CrossRefIndex;
                    if (direction == 1)
                    {
                        nextIndex = thisNode.TCCrossReference[TCIndex - 1].CrossRefIndex;
                    }
                    OutPin[1] = (thisSection.Pins[outPinLink, 0].Link == nextIndex) ? 0 : 1;
                }
            }

            //================================================================================================//
            /// <summary>
            /// Constructor from CircuitSection
            /// </summary>

            public TCRouteElement(TrackCircuitSection thisSection, int direction, ORTS.Signals mySignals, int lastSectionIndex)
            {
                TCSectionIndex = thisSection.Index;
                Direction = direction;
                OutPin[0] = direction;
                OutPin[1] = 0;           // always 0 for NORMAL sections, updated for JUNCTION sections

                if (thisSection.CircuitType == TrackCircuitSection.CIRCUITTYPE.CROSSOVER)
                {
                    int inPinLink = direction == 0 ? 1 : 0;
                    OutPin[1] = (thisSection.Pins[inPinLink, 0].Link == lastSectionIndex) ? 0 : 1;
                }
            }

            //================================================================================================//
            /// <summary>
            /// Constructor for additional items for route checking (not part of train route, NORMAL items only)
            /// </summary>

            public TCRouteElement(int TCIndex, int direction)
            {
                TCSectionIndex = TCIndex;
                Direction = direction;
                OutPin[0] = direction;
                OutPin[1] = 0;
            }

            //================================================================================================//
            //
            // Restore
            //

            public TCRouteElement(BinaryReader inf)
            {
                TCSectionIndex = inf.ReadInt32();
                Direction = inf.ReadInt32();
                OutPin[0] = inf.ReadInt32();
                OutPin[1] = inf.ReadInt32();

                int altindex = inf.ReadInt32();
                if (altindex >= 0)
                {
                    StartAlternativePath = new int[2];
                    StartAlternativePath[0] = altindex;
                    StartAlternativePath[1] = inf.ReadInt32();
                }

                altindex = inf.ReadInt32();
                if (altindex >= 0)
                {
                    EndAlternativePath = new int[2];
                    EndAlternativePath[0] = altindex;
                    EndAlternativePath[1] = inf.ReadInt32();
                }
            }

            //================================================================================================//
            //
            // Save
            //

            public void Save(BinaryWriter outf)
            {
                outf.Write(TCSectionIndex);
                outf.Write(Direction);
                outf.Write(OutPin[0]);
                outf.Write(OutPin[1]);

                if (StartAlternativePath != null)
                {
                    outf.Write(StartAlternativePath[0]);
                    outf.Write(StartAlternativePath[1]);
                }
                else
                {
                    outf.Write(-1);
                }


                if (EndAlternativePath != null)
                {
                    outf.Write(EndAlternativePath[0]);
                    outf.Write(EndAlternativePath[1]);
                }
                else
                {
                    outf.Write(-1);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Subpath list : list of TCRouteElements building a subpath
        /// </summary>

        public class TCSubpathRoute : List<TCRouteElement>
        {


            //================================================================================================//
            //
            // Base contstructor
            //

            public TCSubpathRoute()
            {
            }


            //================================================================================================//
            //
            // Constructor from existing subpath
            //

            public TCSubpathRoute(TCSubpathRoute otherSubpathRoute)
            {
                if (otherSubpathRoute != null)
                {
                    for (int iIndex = 0; iIndex < otherSubpathRoute.Count; iIndex++)
                    {
                        this.Add(otherSubpathRoute[iIndex]);
                    }
                }
            }

            //================================================================================================//
            //
            // Restore
            //

            public TCSubpathRoute(BinaryReader inf)
            {
                int totalElements = inf.ReadInt32();

                for (int iElements = 0; iElements < totalElements; iElements++)
                {
                    TCRouteElement thisElement = new TCRouteElement(inf);
                    this.Add(thisElement);
                }
            }

            //================================================================================================//
            //
            // Save
            //

            public void Save(BinaryWriter outf)
            {
                outf.Write(this.Count);
                foreach (TCRouteElement thisElement in this)
                {
                    thisElement.Save(outf);
                }
            }

            //================================================================================================//
            /// <summary>
            /// Get sectionindex in subpath
            /// <\summary>

            public int GetRouteIndex(int thisSectionIndex, int startIndex)
            {
                for (int iNode = startIndex; iNode >= 0 && iNode < this.Count; iNode++)
                {
                    Train.TCRouteElement thisElement = this[iNode];
                    if (thisElement.TCSectionIndex == thisSectionIndex)
                    {
                        return (iNode);
                    }
                }

                return (-1);
            }


            //================================================================================================//
            /// <summary>
            /// Get sectionindex in subpath
            /// <\summary>

            public int GetRouteIndexBackward(int thisSectionIndex, int startIndex)
            {
                for (int iNode = startIndex - 1; iNode >= 0 && iNode < this.Count; iNode--)
                {
                    Train.TCRouteElement thisElement = this[iNode];
                    if (thisElement.TCSectionIndex == thisSectionIndex)
                    {
                        return (iNode);
                    }
                }

                return (-1);
            }

            //================================================================================================//
            /// <summary>
            /// returns if signal is ahead of train
            /// <\summary>

            public bool SignalIsAheadOfTrain(SignalObject thisSignal, TCPosition trainPosition)
            {
                int signalSection = thisSignal.TCReference;
                int signalRouteIndex = GetRouteIndexBackward(signalSection, trainPosition.RouteListIndex);
                if (signalRouteIndex >= 0)
                    return (false);  // signal section passed earlier in route
                signalRouteIndex = GetRouteIndex(signalSection, trainPosition.RouteListIndex);
                if (signalRouteIndex >= 0)
                    return (true); // signal section still ahead

                if (trainPosition.TCSectionIndex == thisSignal.TCNextTC)
                    return (false); // if train in section following signal, assume we passed

                // signal is not on route - assume we did not pass

#if DEBUG_REPORTS
                int trainno = (thisSignal.enabledTrain != null) ? thisSignal.enabledTrain.Train.Number : -1;

                File.AppendAllText(@"C:\temp\printproc.txt", "Cannot find signal on route : " +
                                " Train " + trainno.ToString() +
                                ", Signal : " + thisSignal.thisRef.ToString() +
                                " in section " + thisSignal.TCReference.ToString() +
                                ", starting from section " + trainPosition.TCSectionIndex.ToString() + "\n");
#endif
                if (thisSignal.enabledTrain != null && thisSignal.enabledTrain.Train.CheckTrain)
                {
                    int trainnoCT = (thisSignal.enabledTrain != null) ? thisSignal.enabledTrain.Train.Number : -1;

                    File.AppendAllText(@"C:\temp\checktrain.txt", "Cannot find signal on route : " +
                                    " Train " + trainnoCT.ToString() +
                                    ", Signal : " + thisSignal.thisRef.ToString() +
                                    " in section " + thisSignal.TCReference.ToString() +
                                    ", starting from section " + trainPosition.TCSectionIndex.ToString() + "\n");
                }
                return (true);
            }

            //================================================================================================//
            /// <summary>
            /// returns distance along route
            /// <\summary>

            public float GetDistanceAlongRoute(int startSectionIndex, float startOffset,
               int endSectionIndex, float endOffset, bool forward, Signals signals)

        // startSectionIndex and endSectionIndex are indices in route list
            // startOffset is remaining length of startSection in required direction
            // endOffset is length along endSection in required direction
            {
                float totalLength = startOffset;

                if (startSectionIndex == endSectionIndex)
                {
                    TrackCircuitSection thisSection = signals.TrackCircuitList[this[startSectionIndex].TCSectionIndex];
                    totalLength = startOffset - (thisSection.Length - endOffset);
                    return (totalLength);
                }

                if (forward)
                {
                    if (startSectionIndex > endSectionIndex)
                        return (-1);

                    for (int iIndex = startSectionIndex + 1; iIndex < endSectionIndex; iIndex++)
                    {
                        TrackCircuitSection thisSection = signals.TrackCircuitList[this[iIndex].TCSectionIndex];
                        totalLength += thisSection.Length;
                    }
                }
                else
                {
                    if (startSectionIndex < endSectionIndex)
                        return (-1);

                    for (int iIndex = startSectionIndex - 1; iIndex > endSectionIndex; iIndex--)
                    {
                        TrackCircuitSection thisSection = signals.TrackCircuitList[this[iIndex].TCSectionIndex];
                        totalLength += thisSection.Length;
                    }
                }

                totalLength += endOffset;

                return (totalLength);
            }

            //================================================================================================//
            /// <summary>
            /// returns if position is ahead of train
            /// <\summary>

            // without offset
            public bool IsAheadOfTrain(TrackCircuitSection thisSection, TCPosition trainPosition)
            {
                float distanceAhead = thisSection.GetDistanceBetweenObjects(
                    trainPosition.TCSectionIndex, trainPosition.TCOffset, trainPosition.TCDirection,
                        thisSection.Index, 0.0f);
                return (distanceAhead > 0.0f);
            }

            // with offset
            public bool IsAheadOfTrain(TrackCircuitSection thisSection, float offset, TCPosition trainPosition)
            {
                float distanceAhead = thisSection.GetDistanceBetweenObjects(
                    trainPosition.TCSectionIndex, trainPosition.TCOffset, trainPosition.TCDirection,
                        thisSection.Index, offset);
                return (distanceAhead > 0.0f);
            }

            //================================================================================================//
            //
            // Converts list of elements to dictionary
            //

            public Dictionary<int, int> ConvertRoute()
            {
                Dictionary<int, int> thisDict = new Dictionary<int, int>();

                foreach (TCRouteElement thisElement in this)
                {
                    if (!thisDict.ContainsKey(thisElement.TCSectionIndex))
                    {
                        thisDict.Add(thisElement.TCSectionIndex, thisElement.Direction);
                    }
                }

                return (thisDict);
            }

            //================================================================================================//
            /// <summary>
            /// check if subroute contains section
            /// <\summary>

            public bool ContainsSection(TCRouteElement thisElement)
            {
                // convert route to dictionary

                Dictionary<int, int> thisRoute = ConvertRoute();
                return (thisRoute.ContainsKey(thisElement.TCSectionIndex));
            }
        }

        //================================================================================================//
        /// <summary>
        /// TrackCircuit position class
        /// </summary>

        public class TCPosition
        {
            public int TCSectionIndex;
            public int TCDirection;
            public float TCOffset;
            public int RouteListIndex;
            public int TrackNode;
            public float DistanceTravelledM;

            //================================================================================================//
            /// <summary>
            /// constructor - creates empty item
            /// </summary>

            public TCPosition()
            {
                TCSectionIndex = -1;
                TCDirection = 0;
                TCOffset = 0.0f;
                RouteListIndex = -1;
                TrackNode = -1;
                DistanceTravelledM = 0.0f;
            }

            //================================================================================================//
            //
            // Restore
            //

            public void RestorePresentPosition(BinaryReader inf, Train train)
            {
                TrackNode tn = train.FrontTDBTraveller.TN;
                float offset = train.FrontTDBTraveller.TrackNodeOffset;
                int direction = (int)train.FrontTDBTraveller.Direction;

                TCPosition tempPosition = new TCPosition();
                tn.TCCrossReference.GetTCPosition(offset, direction, ref tempPosition);

                TCSectionIndex = inf.ReadInt32();
                TCDirection = inf.ReadInt32();
                TCOffset = inf.ReadSingle();
                RouteListIndex = inf.ReadInt32();
                TrackNode = inf.ReadInt32();
                DistanceTravelledM = inf.ReadSingle();

                float offsetDif = Math.Abs(TCOffset - tempPosition.TCOffset);
                if (TCSectionIndex != tempPosition.TCSectionIndex ||
                        (TCSectionIndex == tempPosition.TCSectionIndex && offsetDif > 5.0f))
                {
                    Trace.TraceWarning("Train {0} restored at different present position : was {1}-{3}, is {2}-{4}",
                            train.Number, TCSectionIndex, tempPosition.TCSectionIndex,
                            TCOffset, tempPosition.TCOffset);
                }
            }


            public void RestorePresentRear(BinaryReader inf, Train train)
            {
                TrackNode tn = train.RearTDBTraveller.TN;
                float offset = train.RearTDBTraveller.TrackNodeOffset;
                int direction = (int)train.RearTDBTraveller.Direction;

                TCPosition tempPosition = new TCPosition();
                tn.TCCrossReference.GetTCPosition(offset, direction, ref tempPosition);

                TCSectionIndex = inf.ReadInt32();
                TCDirection = inf.ReadInt32();
                TCOffset = inf.ReadSingle();
                RouteListIndex = inf.ReadInt32();
                TrackNode = inf.ReadInt32();
                DistanceTravelledM = inf.ReadSingle();

                float offsetDif = Math.Abs(TCOffset - tempPosition.TCOffset);
                if (TCSectionIndex != tempPosition.TCSectionIndex ||
                        (TCSectionIndex == tempPosition.TCSectionIndex && offsetDif > 5.0f))
                {
                    Trace.TraceWarning("Train {0} restored at different present rear : was {1}-{2}, is {3}-{4}",
                            train.Number, TCSectionIndex, tempPosition.TCSectionIndex,
                            TCOffset, tempPosition.TCOffset);
                }
            }


            public void RestorePreviousPosition(BinaryReader inf)
            {
                TCSectionIndex = inf.ReadInt32();
                TCDirection = inf.ReadInt32();
                TCOffset = inf.ReadSingle();
                RouteListIndex = inf.ReadInt32();
                TrackNode = inf.ReadInt32();
                DistanceTravelledM = inf.ReadSingle();
            }


            //================================================================================================//
            //
            // Restore dummies for trains not yet started
            //

            public void RestorePresentPositionDummy(BinaryReader inf, Train train)
            {
                TCSectionIndex = inf.ReadInt32();
                TCDirection = inf.ReadInt32();
                TCOffset = inf.ReadSingle();
                RouteListIndex = inf.ReadInt32();
                TrackNode = inf.ReadInt32();
                DistanceTravelledM = inf.ReadSingle();
            }


            public void RestorePresentRearDummy(BinaryReader inf, Train train)
            {
                TCSectionIndex = inf.ReadInt32();
                TCDirection = inf.ReadInt32();
                TCOffset = inf.ReadSingle();
                RouteListIndex = inf.ReadInt32();
                TrackNode = inf.ReadInt32();
                DistanceTravelledM = inf.ReadSingle();
            }


            public void RestorePreviousPositionDummy(BinaryReader inf)
            {
                TCSectionIndex = inf.ReadInt32();
                TCDirection = inf.ReadInt32();
                TCOffset = inf.ReadSingle();
                RouteListIndex = inf.ReadInt32();
                TrackNode = inf.ReadInt32();
                DistanceTravelledM = inf.ReadSingle();
            }

            //================================================================================================//
            //
            // Save
            //

            public void Save(BinaryWriter outf)
            {
                outf.Write(TCSectionIndex);
                outf.Write(TCDirection);
                outf.Write(TCOffset);
                outf.Write(RouteListIndex);
                outf.Write(TrackNode);
                outf.Write(DistanceTravelledM);
            }

            //================================================================================================//
            /// <summary>
            /// Copy TCPosition
            /// <\summary>

            public void CopyTo(ref TCPosition thisPosition)
            {
                thisPosition.TCSectionIndex = this.TCSectionIndex;
                thisPosition.TCDirection = this.TCDirection;
                thisPosition.TCOffset = this.TCOffset;
                thisPosition.RouteListIndex = this.RouteListIndex;
                thisPosition.TrackNode = this.TrackNode;
                thisPosition.DistanceTravelledM = this.DistanceTravelledM;
            }

            //================================================================================================//
            /// <summary>
            /// Reverse (or continue in same direction)
            /// <\summary>

            public void Reverse(int oldDirection, TCSubpathRoute thisRoute, float offset, Signals orgSignals)
            {
                RouteListIndex = thisRoute.GetRouteIndex(TCSectionIndex, 0);
                if (RouteListIndex >= 0)
                {
                    TCDirection = thisRoute[RouteListIndex].Direction;
                }
                else
                {
                    TCDirection = TCDirection == 0 ? 1 : 0;
                }

                TrackCircuitSection thisSection = orgSignals.TrackCircuitList[TCSectionIndex];
                if (oldDirection != TCDirection)
                    TCOffset = thisSection.Length - TCOffset; // actual reversal so adjust offset

                DistanceTravelledM = offset;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Reversal information class
        /// </summary>

        public class TCReversalInfo
        {
            public bool Valid;
            public int LastDivergeIndex;
            public int FirstDivergeIndex;
            public int DivergeSectorIndex;
            public float DivergeOffset;
            public bool SignalAvailable;
            public bool SignalUsed;
            public int LastSignalIndex;
            public int FirstSignalIndex;
            public int SignalSectorIndex;
            public float SignalOffset;

            //================================================================================================//
            /// <summary>
            /// Constructor (from route path details)
            /// <\summary>

            public TCReversalInfo(TCSubpathRoute lastRoute, int prevReversalIndex, TCSubpathRoute firstRoute, Signals orgSignals)
            {
                // preset values
                Valid = false;
                LastDivergeIndex = -1;
                FirstDivergeIndex = -1;
                LastSignalIndex = -1;
                FirstSignalIndex = -1;
                SignalAvailable = false;
                SignalUsed = false;

                // search for first common section in last and first

                int lastIndex = lastRoute.Count - 1;
                int firstIndex = 0;

                int lastCommonSection = -1;
                int firstCommonSection = -1;

                bool commonFound = false;
                bool validDivPoint = false;

                while (!commonFound && lastIndex >= 0)
                {
                    TCRouteElement lastElement = lastRoute[lastIndex];

                    while (!commonFound && firstIndex <= firstRoute.Count - 1)
                    {
                        TCRouteElement firstElement = firstRoute[firstIndex];
                        if (lastElement.TCSectionIndex == firstElement.TCSectionIndex)
                        {
                            commonFound = true;
                            lastCommonSection = lastIndex;
                            firstCommonSection = firstIndex;

                            Valid = (lastElement.Direction != firstElement.Direction);
                        }
                        else
                        {
                            firstIndex++;
                        }
                    }
                    lastIndex--;
                    firstIndex = 0;
                }

                // search for last common section going backward along route
                // do not go back on last route beyond previous reversal point to prevent fall through of reversals
                if (Valid)
                {
                    Valid = false;

                    lastIndex = lastCommonSection;
                    firstIndex = firstCommonSection;

                    int endLastIndex = (prevReversalIndex > 0 && prevReversalIndex < lastCommonSection) ? prevReversalIndex : 0;

                    while (lastIndex >= endLastIndex && firstIndex <= (firstRoute.Count - 1) && lastRoute[lastIndex].TCSectionIndex == firstRoute[firstIndex].TCSectionIndex)
                    {
                        LastDivergeIndex = lastIndex;
                        FirstDivergeIndex = firstIndex;
                        DivergeSectorIndex = lastRoute[lastIndex].TCSectionIndex;

                        lastIndex--;
                        firstIndex++;
                    }

                    Valid = LastDivergeIndex >= 0; // it is a reversal
                    validDivPoint = LastDivergeIndex > 0 && FirstDivergeIndex < (firstRoute.Count-1); // valid reversal point
                    if (lastRoute.Count == 1 && FirstDivergeIndex < (firstRoute.Count - 1)) validDivPoint = true; // valid reversal point in first and only section
                }

                // determine offset

                if (validDivPoint)
                {
                    DivergeOffset = 0.0f;
                    for (int iSection = LastDivergeIndex; iSection < lastRoute.Count; iSection++)
                    {
                        TrackCircuitSection thisSection = orgSignals.TrackCircuitList[lastRoute[iSection].TCSectionIndex];
                        DivergeOffset += thisSection.Length;
                    }

                    // find last signal furthest away from diverging point

                    bool signalFound = false;
                    for (int iSection = 0; iSection <= FirstDivergeIndex && !signalFound; iSection++)
                    {
                        TrackCircuitSection thisSection = orgSignals.TrackCircuitList[firstRoute[iSection].TCSectionIndex];
                        if (thisSection.EndSignals[firstRoute[iSection].Direction] != null)   // signal in required direction
                        {
                            signalFound = true;
                            FirstSignalIndex = iSection;
                            SignalSectorIndex = thisSection.Index;
                        }
                    }

                    if (signalFound)
                    {
                        LastSignalIndex = lastRoute.GetRouteIndex(SignalSectorIndex, LastDivergeIndex);
                        if (LastSignalIndex > 0)
                        {
                            SignalAvailable = true;

                            SignalOffset = 0.0f;
                            for (int iSection = LastSignalIndex; iSection < lastRoute.Count; iSection++)
                            {
                                TrackCircuitSection thisSection = orgSignals.TrackCircuitList[lastRoute[iSection].TCSectionIndex];
                                SignalOffset += thisSection.Length;
                            }
                        }
                    }
                }
                else
                {
                    FirstDivergeIndex = -1;
                    LastDivergeIndex = -1;
                }

            }//constructor

            //================================================================================================//
            /// <summary>
            /// Constructor (from copy)
            /// <\summary>

            public TCReversalInfo(TCReversalInfo otherInfo)
            {
                Valid = otherInfo.Valid;

                LastDivergeIndex = otherInfo.LastDivergeIndex;
                FirstDivergeIndex = otherInfo.FirstDivergeIndex;
                DivergeSectorIndex = otherInfo.DivergeSectorIndex;
                DivergeOffset = otherInfo.DivergeOffset;

                SignalAvailable = otherInfo.SignalAvailable;
                SignalUsed = otherInfo.SignalUsed;
                LastSignalIndex = otherInfo.LastSignalIndex;
                FirstSignalIndex = otherInfo.FirstSignalIndex;
                SignalSectorIndex = otherInfo.SignalSectorIndex;
                SignalOffset = otherInfo.SignalOffset;
            }

            //================================================================================================//
            /// <summary>
            /// Constructor (for invalid item)
            /// <\summary>

            public TCReversalInfo()
            {
                // preset values
                Valid = false;

                LastDivergeIndex = -1;
                FirstDivergeIndex = -1;
                DivergeSectorIndex = -1;
                DivergeOffset = 0.0f;

                LastSignalIndex = -1;
                FirstSignalIndex = -1;
                SignalSectorIndex = -1;
                SignalOffset = 0.0f;

                SignalAvailable = false;
                SignalUsed = false;
            }

            //================================================================================================//
            /// <summary>
            /// Constructor for Restore
            /// <\summary>

            public TCReversalInfo(BinaryReader inf)
            {
                Valid = inf.ReadBoolean();

                if (Valid)
                {
                    LastDivergeIndex = inf.ReadInt32();
                    FirstDivergeIndex = inf.ReadInt32();
                    DivergeSectorIndex = inf.ReadInt32();
                    DivergeOffset = inf.ReadSingle();

                    SignalAvailable = inf.ReadBoolean();
                    SignalUsed = inf.ReadBoolean();
                    LastSignalIndex = inf.ReadInt32();
                    FirstSignalIndex = inf.ReadInt32();
                    SignalSectorIndex = inf.ReadInt32();
                    SignalOffset = inf.ReadSingle();
                }
                else
                {
                    LastDivergeIndex = -1;
                    FirstDivergeIndex = -1;
                    DivergeSectorIndex = -1;
                    DivergeOffset = 0.0f;

                    LastSignalIndex = -1;
                    FirstSignalIndex = -1;
                    SignalSectorIndex = -1;
                    SignalOffset = 0.0f;

                    SignalAvailable = false;
                    SignalUsed = false;
                }
            }

            //================================================================================================//
            /// <summary>
            /// Save
            /// <\summary>

            public void Save(BinaryWriter outf)
            {
                outf.Write(Valid);

                if (Valid)
                {
                    outf.Write(LastDivergeIndex);
                    outf.Write(FirstDivergeIndex);
                    outf.Write(DivergeSectorIndex);
                    outf.Write(DivergeOffset);

                    outf.Write(SignalAvailable);
                    outf.Write(SignalUsed);
                    outf.Write(LastSignalIndex);
                    outf.Write(FirstSignalIndex);
                    outf.Write(SignalSectorIndex);
                    outf.Write(SignalOffset);
                }
            }

        }//TCReversalInfo

        //================================================================================================//
        /// <summary>
        /// Distance Travelled action item list
        /// </summary>

        public class DistanceTravelledActions : LinkedList<DistanceTravelledItem>
        {

            //================================================================================================//
            //
            // Copy list
            //

            public DistanceTravelledActions Copy()
            {
                DistanceTravelledActions newList = new DistanceTravelledActions();

                LinkedListNode<DistanceTravelledItem> nextNode = this.First;
                DistanceTravelledItem thisItem = nextNode.Value;

                newList.AddFirst(thisItem);
                LinkedListNode<DistanceTravelledItem> prevNode = newList.First;

                nextNode = nextNode.Next;

                while (nextNode != null)
                {
                    thisItem = nextNode.Value;
                    newList.AddAfter(prevNode, thisItem);
                    nextNode = nextNode.Next;
                    prevNode = prevNode.Next;
                }

                return (newList);
            }


            //================================================================================================//
            /// <summary>
            /// Insert item on correct distance
            /// <\summary>

            public void InsertAction(DistanceTravelledItem thisItem)
            {
                if (this.Count == 0)
                {
                    this.AddFirst(thisItem);
                }
                else
                {
                    LinkedListNode<DistanceTravelledItem> nextNode = this.First;
                    DistanceTravelledItem nextItem = nextNode.Value;
                    bool inserted = false;
                    while (!inserted)
                    {
                        if (thisItem.RequiredDistance < nextItem.RequiredDistance)
                        {
                            this.AddBefore(nextNode, thisItem);
                            inserted = true;
                        }
                        else if (nextNode.Next == null)
                        {
                            this.AddAfter(nextNode, thisItem);
                            inserted = true;
                        }
                        else
                        {
                            nextNode = nextNode.Next;
                            nextItem = nextNode.Value;
                        }
                    }
                }
            }

            //================================================================================================//
            /// <summary>
            /// Insert section clearance item
            /// <\summary>

            public void InsertClearSection(float distance, int sectionIndex)
            {
                ClearSectionItem thisItem = new ClearSectionItem(distance, sectionIndex);
                InsertAction(thisItem);
            }

            //================================================================================================//
            /// <summary>
            /// Get list of items to be processed
            /// <\summary>

            public List<DistanceTravelledItem> GetActions(float distance)
            {
                List<DistanceTravelledItem> itemList = new List<DistanceTravelledItem>();

                bool itemsCollected = false;
                LinkedListNode<DistanceTravelledItem> nextNode = this.First;
                LinkedListNode<DistanceTravelledItem> prevNode;

                while (!itemsCollected && nextNode != null)
                {
                    if (nextNode.Value.RequiredDistance <= distance)
                    {
                        itemList.Add(nextNode.Value);
                        prevNode = nextNode;
                        nextNode = prevNode.Next;
                        this.Remove(prevNode);
                    }
                    else
                    {
                        itemsCollected = true;
                    }
                }

                return (itemList);
            }

            //================================================================================================//
            /// <summary>
            /// Get list of items to be processed of particular type
            /// <\summary>

            public List<DistanceTravelledItem> GetActions(float distance, Type reqType)
            {
                List<DistanceTravelledItem> itemList = new List<DistanceTravelledItem>();

                bool itemsCollected = false;
                LinkedListNode<DistanceTravelledItem> nextNode = this.First;
                LinkedListNode<DistanceTravelledItem> prevNode;

                while (!itemsCollected && nextNode != null)
                {
                    if (nextNode.Value.RequiredDistance <= distance && nextNode.Value.GetType() == reqType)
                    {
                        itemList.Add(nextNode.Value);
                        prevNode = nextNode;
                        nextNode = prevNode.Next;
                        this.Remove(prevNode);
                    }
                    else
                    {
                        itemsCollected = true;
                    }
                }

                return (itemList);
            }

            //================================================================================================//
            /// <summary>
            /// update any pending speed limits to new limit
            /// <\summary>

            public void UpdatePendingSpeedlimits(float reqSpeedMpS)
            {
                foreach (var thisAction in this)
                {
                    if (thisAction is ActivateSpeedLimit)
                    {
                        ActivateSpeedLimit thisLimit = (thisAction as ActivateSpeedLimit);

                        if (thisLimit.MaxSpeedMpSLimit > reqSpeedMpS)
                        {
                            thisLimit.MaxSpeedMpSLimit = reqSpeedMpS;
                        }
                        if (thisLimit.MaxSpeedMpSSignal > reqSpeedMpS)
                        {
                            thisLimit.MaxSpeedMpSSignal = reqSpeedMpS;
                        }
                    }
                }
            }

            //================================================================================================//
            /// <summary>
            /// remove any pending AIActionItems
            /// <\summary>

            public void RemovePendingAIActionItems(bool removeAll)
            {
                List<DistanceTravelledItem> itemsToRemove = new List<DistanceTravelledItem>();

                foreach (var thisAction in this)
                {
                    if (thisAction is AIActionItem || removeAll)
                    {
                        DistanceTravelledItem thisItem = thisAction;
                        itemsToRemove.Add(thisItem);
                    }
                }

                foreach (var thisAction in itemsToRemove)
                {
                    this.Remove(thisAction);
                }

            }

        }

        //================================================================================================//
        /// <summary>
        /// Distance Travelled action item - base class for all possible actions
        /// </summary>

        public class DistanceTravelledItem
        {
            public float RequiredDistance;

            //================================================================================================//
            //
            // Base contructor
            //

            public DistanceTravelledItem()
            {
            }

            //================================================================================================//
            //
            // Restore
            //

            public DistanceTravelledItem(BinaryReader inf)
            {
                RequiredDistance = inf.ReadSingle();
            }

            //================================================================================================//
            //
            // Save
            //

            public void Save(BinaryWriter outf)
            {
                if (this is ActivateSpeedLimit)
                {
                    outf.Write(1);
                    outf.Write(RequiredDistance);
                    ActivateSpeedLimit thisLimit = this as ActivateSpeedLimit;
                    thisLimit.SaveItem(outf);
                }
                else if (this is ClearSectionItem)
                {
                    outf.Write(2);
                    outf.Write(RequiredDistance);
                    ClearSectionItem thisSection = this as ClearSectionItem;
                    thisSection.SaveItem(outf);
                }
                else if (this is AIActionItem)
                {
                    outf.Write(3);
                    outf.Write(RequiredDistance);
                    AIActionItem thisAction = this as AIActionItem;
                    thisAction.SaveItem(outf);
                }
                else
                {
                    outf.Write(-1);
                }

            }
        }

        //================================================================================================//
        /// <summary>
        /// Distance Travelled Clear Section action item
        /// </summary>

        public class ClearSectionItem : DistanceTravelledItem
        {
            public int TrackSectionIndex;  // in case of CLEAR_SECTION  //

            //================================================================================================//
            /// <summary>
            /// constructor for clear section
            /// </summary>

            public ClearSectionItem(float distance, int sectionIndex)
            {
                RequiredDistance = distance;
                TrackSectionIndex = sectionIndex;
            }

            //================================================================================================//
            //
            // Restore
            //

            public ClearSectionItem(BinaryReader inf)
                : base(inf)
            {
                TrackSectionIndex = inf.ReadInt32();
            }

            //================================================================================================//
            //
            // Save
            //

            public void SaveItem(BinaryWriter outf)
            {
                outf.Write(TrackSectionIndex);
            }


        }

        //================================================================================================//
        /// <summary>
        /// Distance Travelled Speed Limit Item
        /// </summary>

        public class ActivateSpeedLimit : DistanceTravelledItem
        {
            public float MaxSpeedMpSLimit = -1;
            public float MaxSpeedMpSSignal = -1;

            //================================================================================================//
            /// <summary>
            /// constructor for speedlimit value
            /// </summary>

            public ActivateSpeedLimit(float reqDistance, float maxSpeedMpSLimit, float maxSpeedMpSSignal)
            {
                RequiredDistance = reqDistance;
                MaxSpeedMpSLimit = maxSpeedMpSLimit;
                MaxSpeedMpSSignal = maxSpeedMpSSignal;
            }

            //================================================================================================//
            //
            // Restore
            //

            public ActivateSpeedLimit(BinaryReader inf)
                : base(inf)
            {
                MaxSpeedMpSLimit = inf.ReadSingle();
                MaxSpeedMpSSignal = inf.ReadSingle();
            }

            //================================================================================================//
            //
            // Save
            //

            public void SaveItem(BinaryWriter outf)
            {
                outf.Write(MaxSpeedMpSLimit);
                outf.Write(MaxSpeedMpSSignal);
            }

        }

        //================================================================================================//
        /// <summary>
        /// StationStop class
        /// Class to hold information on station stops
        /// <\summary>

        public class StationStop : IComparable<StationStop>
        {

            public enum STOPTYPE
            {
                STATION_STOP,
                SIDING_STOP,
                MANUAL_STOP,
                WAITING_POINT,
            }

            public STOPTYPE ActualStopType;

            public int PlatformReference;
            public PlatformDetails PlatformItem;
            public int SubrouteIndex;
            public int RouteIndex;
            public int TCSectionIndex;
            public int Direction;
            public int ExitSignal;
            public bool HoldSignal;
            public float StopOffset;
            public float DistanceToTrainM;
            public int ArrivalTime;
            public int DepartTime;
            public int ActualArrival;
            public int ActualDepart;
            public bool Passed;


            //================================================================================================//
            //
            // Constructor
            //

            public StationStop(int platformReference, PlatformDetails platformItem, int subrouteIndex, int routeIndex,
                int tcSectionIndex, int direction, int exitSignal, bool holdSignal, float stopOffset,
                int arrivalTime, int departTime, STOPTYPE actualStopType)
            {
                ActualStopType = actualStopType;
                PlatformReference = platformReference;
                PlatformItem = platformItem;
                SubrouteIndex = subrouteIndex;
                RouteIndex = routeIndex;
                TCSectionIndex = tcSectionIndex;
                Direction = direction;
                ExitSignal = exitSignal;
                HoldSignal = holdSignal;
                StopOffset = stopOffset;
                if (actualStopType == STOPTYPE.STATION_STOP)
                {
                    ArrivalTime = Math.Max(0, arrivalTime);
                    DepartTime = Math.Max(0, departTime);
                }
                else
                    // times may be <0 for waiting point
                {
                    ArrivalTime = arrivalTime;
                    DepartTime = departTime;
                }
                ActualArrival = arrivalTime;
                ActualDepart = departTime;
                DistanceToTrainM = 9999999f;
                Passed = false;
            }

            //================================================================================================//
            //
            // Constructor from copy
            //

            public StationStop(StationStop orgStop)
            {
                ActualStopType = orgStop.ActualStopType;
                PlatformReference = orgStop.PlatformReference;
                PlatformItem = new PlatformDetails(orgStop.PlatformItem);
                SubrouteIndex = orgStop.SubrouteIndex;
                RouteIndex = orgStop.RouteIndex;
                TCSectionIndex = orgStop.TCSectionIndex;
                Direction = orgStop.Direction;
                ExitSignal = orgStop.ExitSignal;
                HoldSignal = orgStop.HoldSignal;
                StopOffset = orgStop.StopOffset;
                ArrivalTime = orgStop.ArrivalTime;
                DepartTime = orgStop.DepartTime;
                ActualArrival = orgStop.ActualArrival;
                ActualDepart = orgStop.ActualDepart;
                DistanceToTrainM = orgStop.DistanceToTrainM;
                Passed = orgStop.Passed;
            }

            //================================================================================================//
            //
            // Restore
            //

            public StationStop(BinaryReader inf, Signals signalRef)
            {
                ActualStopType = (STOPTYPE)inf.ReadInt32();
                PlatformReference = inf.ReadInt32();

                if (PlatformReference >= 0)
                {
                    int platformIndex;
                    if (signalRef.PlatformXRefList.TryGetValue(PlatformReference, out platformIndex))
                    {
                        PlatformItem = signalRef.PlatformDetailsList[platformIndex];
                    }
                    else
                    {
                        Trace.TraceInformation("Cannot find platform {0}", PlatformReference);
                    }
                }
                else
                {
                    PlatformItem = null;
                }

                SubrouteIndex = inf.ReadInt32();
                RouteIndex = inf.ReadInt32();
                TCSectionIndex = inf.ReadInt32();
                Direction = inf.ReadInt32();
                ExitSignal = inf.ReadInt32();
                HoldSignal = inf.ReadBoolean();
                StopOffset = inf.ReadSingle();
                ArrivalTime = inf.ReadInt32();
                DepartTime = inf.ReadInt32();
                ActualArrival = inf.ReadInt32();
                ActualDepart = inf.ReadInt32();
                DistanceToTrainM = 9999999f;
                Passed = inf.ReadBoolean();
            }

            //================================================================================================//
            //
            // Compare To (to allow sort)
            //

            public int CompareTo(StationStop otherStop)
            {
                if (this.SubrouteIndex < otherStop.SubrouteIndex)
                {
                    return -1;
                }
                else if (this.SubrouteIndex > otherStop.SubrouteIndex)
                {
                    return 1;
                }
                else if (this.RouteIndex < otherStop.RouteIndex)
                {
                    return -1;
                }
                else if (this.RouteIndex > otherStop.RouteIndex)
                {
                    return 1;
                }
                else if (this.StopOffset < otherStop.StopOffset)
                {
                    return -1;
                }
                else if (this.StopOffset > otherStop.StopOffset)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }

            //================================================================================================//
            //
            // Save
            //

            public void Save(BinaryWriter outf)
            {
                outf.Write((int)ActualStopType);
                outf.Write(PlatformReference);
                outf.Write(SubrouteIndex);
                outf.Write(RouteIndex);
                outf.Write(TCSectionIndex);
                outf.Write(Direction);
                outf.Write(ExitSignal);
                outf.Write(HoldSignal);
                outf.Write(StopOffset);
                outf.Write(ArrivalTime);
                outf.Write(DepartTime);
                outf.Write(ActualArrival);
                outf.Write(ActualDepart);
                outf.Write(Passed);
            }
        }


        //================================================================================================//
        /// <summary>
        /// Class for info to TrackMonitor display
        /// <\summary>

        public class TrainInfo
        {
            public TRAIN_CONTROL ControlMode;                // present control mode 
            public float speedMpS;                           // present speed
            public float projectedSpeedMpS;                  // projected speed
            public float allowedSpeedMpS;                    // max allowed speed
            public int direction;                            // present direction (0=forward, 1=backward)
            public int cabOrientation;                       // present cab orientation (0=forward, 1=backward)
            public bool isOnPath;                            // train is on defined path (valid in Manual mode only)
            public List<TrainObjectItem> ObjectInfoForward;  // forward objects
            public List<TrainObjectItem> ObjectInfoBackward; // backward objects

            //================================================================================================//
            /// <summary>
            /// Constructor - creates empty objects, data is filled by GetInfo routine from Train
            /// <\summary>

            public TrainInfo()
            {
                ObjectInfoForward = new List<TrainObjectItem>();
                ObjectInfoBackward = new List<TrainObjectItem>();
            }

            /// no need for Restore or Save items as info is not kept in permanent variables

        }

        //================================================================================================//
        /// <summary>
        /// Class TrainObjectItem : info on objects etc. in train path
        /// Used as interface for TrackMonitorWindow as part of TrainInfo class
        /// <\summary>

        public class TrainObjectItem : IComparable<TrainObjectItem>
        {
            public enum TRAINOBJECTTYPE
            {
                SIGNAL,
                SPEEDPOST,
                STATION,
                AUTHORITY,
                REVERSAL,
                OUT_OF_CONTROL
            }

            public TRAINOBJECTTYPE ItemType;
            public OUTOFCONTROL OutOfControlReason;
            public END_AUTHORITY AuthorityType;
            public TrackMonitorSignalAspect SignalState;
            public float AllowedSpeedMpS;
            public float DistanceToTrainM;

            // field validity :
            // if ItemType == SIGNAL :
            //      SignalState
            //      AllowedSpeedMpS if value > 0
            //      DistanceToTrainM
            //
            // if ItemType == SPEEDPOST :
            //      AllowedSpeedMpS
            //      DistanceToTrainM
            //
            // if ItemType == STATION :
            //      DistanceToTrainM
            //
            // if ItemType == AUTHORITY :
            //      AuthorityType
            //      DistanceToTrainM
            //
            // if ItemType == REVERSAL :
            //      DistanceToTrainM
            //
            // if ItemType == OUTOFCONTROL :
            //      OutOfControlReason


            //================================================================================================//
            /// <summary>
            /// Constructors
            /// <\summary>

            // Constructor for Signal
            public TrainObjectItem(TrackMonitorSignalAspect thisAspect, float thisSpeedMpS, float thisDistanceM)
            {
                ItemType = TRAINOBJECTTYPE.SIGNAL;
                AuthorityType = END_AUTHORITY.NO_PATH_RESERVED;
                SignalState = thisAspect;
                AllowedSpeedMpS = thisSpeedMpS;
                DistanceToTrainM = thisDistanceM;
            }

            // Constructor for Speedpost
            public TrainObjectItem(float thisSpeedMpS, float thisDistanceM)
            {
                ItemType = TRAINOBJECTTYPE.SPEEDPOST;
                AuthorityType = END_AUTHORITY.NO_PATH_RESERVED;
                SignalState = TrackMonitorSignalAspect.Clear_2;
                AllowedSpeedMpS = thisSpeedMpS;
                DistanceToTrainM = thisDistanceM;
            }

            // Constructor for Station
            public TrainObjectItem(float thisDistanceM)
            {
                ItemType = TRAINOBJECTTYPE.STATION;
                AuthorityType = END_AUTHORITY.NO_PATH_RESERVED;
                SignalState = TrackMonitorSignalAspect.Clear_2;
                AllowedSpeedMpS = -1;
                DistanceToTrainM = thisDistanceM;
            }

            // Constructor for Reversal
            public TrainObjectItem(int dummy, float thisDistanceM)  // dummy int as first parameter to make it unique
            {
                ItemType = TRAINOBJECTTYPE.REVERSAL;
                AuthorityType = END_AUTHORITY.NO_PATH_RESERVED;
                SignalState = TrackMonitorSignalAspect.Clear_2;
                AllowedSpeedMpS = -1;
                DistanceToTrainM = thisDistanceM;
            }

            // Constructor for Authority
            public TrainObjectItem(END_AUTHORITY thisAuthority, float thisDistanceM)
            {
                ItemType = TRAINOBJECTTYPE.AUTHORITY;
                AuthorityType = thisAuthority;
                SignalState = TrackMonitorSignalAspect.Clear_2;
                AllowedSpeedMpS = -1;
                DistanceToTrainM = thisDistanceM;
            }

            // Constructor for OutOfControl
            public TrainObjectItem(OUTOFCONTROL thisReason)
            {
                ItemType = TRAINOBJECTTYPE.OUT_OF_CONTROL;
                OutOfControlReason = thisReason;
            }

            /// no need for Restore or Save items as info is not kept in permanent variables

            //================================================================================================//
            //
            // Compare To (to allow sort)
            //

            public int CompareTo(TrainObjectItem otherItem)
            {
                if (this.DistanceToTrainM < otherItem.DistanceToTrainM)
                    return (-1);
                if (this.DistanceToTrainM == otherItem.DistanceToTrainM)
                    return (0);
                return (1);
            }
        }

        //used by remote train to update location based on message received
        public int expectedTileX, expectedTileZ, expectedTracIndex, expectedDIr, expectedTDir;
        public float expectedX, expectedZ, expectedTravelled, expectedLength;
        public bool updateMSGReceived = false;

        public void ToDoUpdate(int tni, int tX, int tZ, float x, float z, float eT, float speed, int dir, int tDir, float len)
        {
            SpeedMpS = speed;
            expectedTileX = tX;
            expectedTileZ = tZ;
            expectedX = x;
            expectedZ = z;
            expectedTravelled = eT;
            expectedTracIndex = tni;
            expectedDIr = dir;
            expectedTDir = tDir;
            expectedLength = len;
            updateMSGReceived = true;
        }

        private void UpdateCarSlack(float expectedLength)
        {
            if (Cars.Count <= 1) return;
            var staticLength = 0f;
            foreach (var car in Cars)
            {
                staticLength += car.Length;
            }
            staticLength = (expectedLength - staticLength) / (Cars.Count - 1);
            foreach (var car in Cars)//update slack for each car
            {
                car.CouplerSlackM = staticLength - car.GetCouplerZeroLengthM();
            }

        }
        public void UpdateRemoteTrainPos(float elapsedClockSeconds)
        {
            if (updateMSGReceived)
            {
                updateMSGReceived = false;
                float move = 0.0f;
                var requestedSpeed = SpeedMpS;
                try
                {
                    UpdateCarSlack(expectedLength);//update car slack first

                    var x = travelled + LastSpeedMpS * elapsedClockSeconds + (SpeedMpS - LastSpeedMpS) / 2 * elapsedClockSeconds;
                    this.MUDirection = (Direction)expectedDIr;

                    if (Math.Abs(x - expectedTravelled) < 0.2 || Math.Abs(x - expectedTravelled) > 10)
                    {
                        CalculatePositionOfCars(expectedTravelled - travelled);
                        //if something wrong with the switch
                        if (this.RearTDBTraveller.TrackNodeIndex != expectedTracIndex)
                        {
                            Traveller t = null;
                            if (expectedTracIndex <= 0)
                            {
                                t = new Traveller(Simulator.TSectionDat, Simulator.TDB.TrackDB.TrackNodes,  expectedTileX, expectedTileZ, expectedX, expectedZ, (Traveller.TravellerDirection)expectedTDir);
                            }
                            else
                            {
                                t = new Traveller(Simulator.TSectionDat, Simulator.TDB.TrackDB.TrackNodes, Simulator.TDB.TrackDB.TrackNodes[expectedTracIndex], expectedTileX, expectedTileZ, expectedX, expectedZ, (Traveller.TravellerDirection)expectedTDir);
                            }
                            //move = SpeedMpS > 0 ? 0.001f : -0.001f;
                            this.travelled = expectedTravelled;
                            this.RearTDBTraveller = t;
                            CalculatePositionOfCars(0);

                        }
                    }
                    else//if the predicted location and reported location are similar, will try to increase/decrease the speed to bridge the gap in 1 second
                    {
                        SpeedMpS += (expectedTravelled - x) / 1;
                        CalculatePositionOfCars(SpeedMpS * elapsedClockSeconds);
                    }
                }
                catch (Exception)
                {
                    move = expectedTravelled - travelled;
                }
                /*if (Math.Abs(requestedSpeed) < 0.00001 && Math.Abs(SpeedMpS) > 0.01) updateMSGReceived = true; //if requested is stop, but the current speed is still moving
                else*/

            }
            else//no message received, will move at the previous speed
            {
                CalculatePositionOfCars(SpeedMpS * elapsedClockSeconds);
            }

            //update speed for each car, so wheels will rotate
            foreach (TrainCar car in Cars)
            {
                if (car != null)
                {
                    if (car.IsDriveable && car is MSTSWagon) (car as MSTSWagon).WheelSpeedMpS = SpeedMpS;
                    car.SpeedMpS = SpeedMpS;
                    if (car.Flipped) car.SpeedMpS = -car.SpeedMpS;



#if INDIVIDUAL_CONTROL
        		if (car is MSTSLocomotive && car.CarID.StartsWith(MPManager.GetUserName()))
						{
							car.Update(elapsedClockSeconds);
						}
#endif
                }
            }
            LastSpeedMpS = SpeedMpS;
            //Orient();
            return;

        }
    }// class Train
}

