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
// #define DEBUG_TTANALYSIS
// DEBUG flag for debug prints

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using ORTS.Common;
using Event = Orts.Common.Event;

namespace Orts.Simulation.Timetables
{
    public class TTTrain : AITrain
    {
        public float DefMaxDecelMpSSP = 1.0f;               // maximum decelleration
        public float DefMaxAccelMpSSP = 1.0f;               // maximum accelleration
        public float DefMaxDecelMpSSF = 0.8f;               // maximum decelleration
        public float DefMaxAccelMpSSF = 0.5f;               // maximum accelleration

        public bool Closeup = false;                           // closeup to other train when stabling
        public static float keepDistanceCloseupM = 2.5f;       // stay 2.5m from end of route when closeup required (for stabling only)
        public static float keepDistanceTrainAheadCloseupM = 0.5f;       // stay 0.5m from train ahead when closeup required (for stabling only)
        public static float keepDistanceCloseupSignalM = 7.0f;          // stay 10m from signal ahead when signalcloseup required
        public static float endOfRouteDistance = 150f;         // Max length to remain for train to continue on route

        public int? ActivateTime;                           // time train is activated
        public bool TriggeredActivationRequired = false;    // train activation is triggered by other train

        public bool Created = false;                        // train is created at start
        public string CreateAhead = String.Empty;           // train is created ahead of other train
        public string CreateInPool = String.Empty;          // train is to be created in pool at start of timetable
        public string CreateFromPool = String.Empty;        // train is to be created from pool
        public TimetablePool.PoolExitDirectionEnum CreatePoolDirection = TimetablePool.PoolExitDirectionEnum.Undefined;
                                                            // required direction on leaving pool (if applicable)
        public string ForcedConsistName = String.Empty;     // forced consist name for extraction from pool

        // Timetable Commands info
        public List<WaitInfo> WaitList = null;                            //used when in timetable mode for wait instructions
        public Dictionary<int, List<WaitInfo>> WaitAnyList = null;        //used when in timetable mode for waitany instructions
        public bool Stable_CallOn = false;                                //used when in timetable mode to show stabled train is allowed to call on
        public bool DriverOnlyOperation = false;                          //used when in timetable mode to indicate driver only operation
        public bool ForceReversal = false;                                //used when in timetable mode to force reversal at diverging point ignoring signals

        public enum FormCommand                                           //enum to indicate type of form sequence
        {
            TerminationFormed,
            TerminationTriggered,
            Detached,
            Created,
            None,
        }

        public int Forms = -1;                                            //indicates which train is to be formed out of this train on termination
        public bool FormsStatic = false;                                  //indicate if train is to remain as static
        public string ExitPool = String.Empty;                            //set if train is to be stabled in pool
        public int PoolAccessSection = -1;                                //set to last section index if train is to be stabled in pool, section is access section to pool

        public enum PoolAccessState                                       // used to indicate access state to pool, combined with storage index
                                                                          // values are <0, value >= 0 is returned storage index
        {
            PoolClaimed = -1,
            PoolOverflow = -2,
            PoolInvalid = -3,
        }
        public int PoolStorageIndex = -1;                                 // index in selected pool path (>=0)

        public TimetablePool.PoolExitDirectionEnum PoolExitDirection = TimetablePool.PoolExitDirectionEnum.Undefined;
                                                                          // required exit direction from pool (if applicable) 
        public TimetableTurntableControl ActiveTurntable = null;          //active turntable

        public int FormedOf = -1;                                         //indicates out of which train this train is formed
        public FormCommand FormedOfType = FormCommand.None;               //indicates type of formed-of command
        public int OrgAINumber = -1;                                      //original AI number of formed player train
        public bool SetStop = false;                                      //indicates train must copy station stop from formed train
        public bool FormsAtStation = false;                               //indicates train must form into next service at last station, route must be curtailed to that stop
        public bool leadLocoAntiSlip = false;                             //anti slip indication for original leading engine

        // detach details
        public Dictionary<int, List<DetachInfo>> DetachDetails = new Dictionary<int, List<DetachInfo>>();
        // key is platform reference (use -1 for detach at start or end), list is detach commands at that location
        public int[] DetachActive = new int[2] { -1, -1 };                // detach is activated - first index is key in DetachDetails, second index is index in valuelist
        // 2nd index = -1 indicates invalid (first index -1 is a valid index)
        public int DetachUnits = 0;                                       // no. of units to detach
        public bool DetachPosition = false;                               // if true detach from front
        public bool DetachPending = false;                                // true when player detach window is displayed

        // attach details
        public AttachInfo AttachDetails;                                  // attach details
        public Dictionary<int, List<int>> NeedAttach = new Dictionary<int, List<int>>();
        // key is platform reference or -1 for attach to static train, list are trains which are to attach

        // pickup details
        public List<PickUpInfo> PickUpDetails = new List<PickUpInfo>();   // only used during train building
        public List<int> PickUpTrains = new List<int>();                  // list of train to be picked up
        public List<int> PickUpStatic = new List<int>();                  // index of locations where static consists are to be picked up
        public bool PickUpStaticOnForms = false;                          // set if pickup of static is required when forming next train
        public bool NeedPickUp = false;                                   // indicates pickup is required

        // transfer details
        public Dictionary<int, TransferInfo> TransferStationDetails = new Dictionary<int, TransferInfo>();
        // list of transfer to take place in station
        public Dictionary<int, List<TransferInfo>> TransferTrainDetails = new Dictionary<int, List<TransferInfo>>();
        // list of transfers defined per train - if int = -1, transfer is to be performed on static train
        public bool NeedTransfer = false;                                 // indicates transfer is required
        public Dictionary<int, List<int>> NeedStationTransfer = new Dictionary<int, List<int>>();
        // list of required station transfers, per station index
        public Dictionary<int, int> NeedTrainTransfer = new Dictionary<int, int>();
        // number of required train transfers per section

        // delayed restart
        public bool DelayedStart = false;                                 // start is delayed
        public float RestdelayS = 0.0f;                                   // time to wait
        public AITrain.AI_START_MOVEMENT DelayedStartState;               // state to start

        public struct DelayedStartBase
        {
            public int fixedPartS;                                        // fixed part for restart delay
            public int randomPartS;                                       // random part for restart delay
        }

        public struct DelayedStartValues
        {
            public DelayedStartBase newStart;                             // delay on new start
            public DelayedStartBase pathRestart;                          // delay on pathing stop restart (e.g. signal, reversal, node)
            public DelayedStartBase followRestart;                        // delay on restart when following other train
            public DelayedStartBase stationRestart;                       // delay on restart from station stop
            public DelayedStartBase attachRestart;                        // delay on restart after attaching
            public DelayedStartBase detachRestart;                        // delay between stop and detaching
            public DelayedStartBase movingtableRestart;                   // delay for movement of train and moving table
            public float reverseAddedDelaySperM;                          // additional delay on reversal based on train length
        }

        public struct SpeedValues
        {
            public float? maxSpeedMpS;                                    // timetable defined max speed
            public float? cruiseSpeedMpS;                                 // timetable defined cruise speed
            public int? cruiseMaxDelayS;                                  // max. delay to maintain cruise speed
            public float? creepSpeedMpS;                                  // timetable defined creep speed
            public float? attachSpeedMpS;                                 // timetable defined attach speed
            public float? detachSpeedMpS;                                 // timetable defined detach speed
            public float? movingtableSpeedMpS;                            // timetable defined speed for moving tables
            public float routeSpeedMpS;                                   // route defined max speed
            public float consistSpeedMpS;                                 // consist defined max speed
            public bool restrictedSet;                                    // special speed has been set
        }

        public DelayedStartValues DelayedStartSettings = new DelayedStartValues();
        public SpeedValues SpeedSettings = new SpeedValues();

        // special patch conditions
        public enum LastSignalStop
        {
            None,
            Last,
            Reverse,
        }
        public LastSignalStop ReqLastSignalStop = LastSignalStop.None;

        public enum TriggerActivationType
        {
            Start,
            Dispose,
            StationStop,
            StationDepart,
        }

        public struct TriggerActivation
        {
            public int activatedTrain;                                    // train to be activated
            public TriggerActivationType activationType;                  // type of activation
            public int platformId;                                        // trigger platform ident (in case of station stop)
            public string activatedName;                                  // name of activated train (used in processing timetable only)
        }

        public List<TriggerActivation> activatedTrainTriggers = new List<TriggerActivation>();
        public string Briefing { get; set; } = "";

        //================================================================================================//
        /// <summary>
        /// Constructor
        /// <\summary>
        public TTTrain(Simulator simulator)
            : base(simulator)
        {
            // set AI reference
            AI = simulator.AI;

            // preset accel and decel values
            MaxAccelMpSSP = DefMaxAccelMpSSP;
            MaxAccelMpSSF = DefMaxAccelMpSSF;
            MaxDecelMpSSP = DefMaxDecelMpSSP;
            MaxDecelMpSSF = DefMaxDecelMpSSF;

            // preset movement state
            MovementState = AI_MOVEMENT_STATE.AI_STATIC;

            // preset restart delays
            DelayedStartSettings.newStart.fixedPartS = 0;
            DelayedStartSettings.newStart.randomPartS = 10;
            DelayedStartSettings.pathRestart.fixedPartS = 1;
            DelayedStartSettings.pathRestart.randomPartS = 10;
            DelayedStartSettings.followRestart.fixedPartS = 15;
            DelayedStartSettings.followRestart.randomPartS = 10;
            DelayedStartSettings.stationRestart.fixedPartS = 0;
            DelayedStartSettings.stationRestart.randomPartS = 15;
            DelayedStartSettings.attachRestart.fixedPartS = 30;
            DelayedStartSettings.attachRestart.randomPartS = 30;
            DelayedStartSettings.detachRestart.fixedPartS = 5;
            DelayedStartSettings.detachRestart.randomPartS = 20;
            DelayedStartSettings.movingtableRestart.fixedPartS = 1;
            DelayedStartSettings.movingtableRestart.randomPartS = 10;
            DelayedStartSettings.reverseAddedDelaySperM = 0.5f;

            // preset speed values
            SpeedSettings.maxSpeedMpS = null;
            SpeedSettings.cruiseSpeedMpS = null;
            SpeedSettings.cruiseMaxDelayS = null;
            SpeedSettings.creepSpeedMpS = null;
            SpeedSettings.attachSpeedMpS = null;
            SpeedSettings.detachSpeedMpS = null;
            SpeedSettings.movingtableSpeedMpS = null;
            SpeedSettings.restrictedSet = false;
        }

        //================================================================================================//
        /// <summary>
        /// Constructor using existing train
        /// <\summary>
        public TTTrain(Simulator simulator, TTTrain TTrain)
            : base(simulator)
        {
            // set AI reference
            AI = simulator.AI;

            // preset accel and decel values
            MaxAccelMpSSP = DefMaxAccelMpSSP;
            MaxAccelMpSSF = DefMaxAccelMpSSF;
            MaxDecelMpSSP = DefMaxDecelMpSSP;
            MaxDecelMpSSF = DefMaxDecelMpSSF;

            // preset movement state
            MovementState = AI_MOVEMENT_STATE.AI_STATIC;

            // copy restart delays
            DelayedStartSettings = TTrain.DelayedStartSettings;

            // copy speed values
            SpeedSettings = TTrain.SpeedSettings;
        }

        //================================================================================================//
        /// <summary>
        /// Restore
        /// <\summary>

        public TTTrain(Simulator simulator, BinaryReader inf, AI airef)
            : base(simulator, inf, airef)
        {
            // TTTrains own additional fields
            Closeup = inf.ReadBoolean();
            Created = inf.ReadBoolean();
            CreateAhead = inf.ReadString();
            CreateFromPool = inf.ReadString();
            CreateInPool = inf.ReadString();
            ForcedConsistName = inf.ReadString();
            CreatePoolDirection = (TimetablePool.PoolExitDirectionEnum)inf.ReadInt32();

            MaxAccelMpSSP = inf.ReadSingle();
            MaxAccelMpSSF = inf.ReadSingle();
            MaxDecelMpSSP = inf.ReadSingle();
            MaxDecelMpSSF = inf.ReadSingle();

            int activateTimeValue = inf.ReadInt32();
            if (activateTimeValue < 0)
            {
                ActivateTime = null;
            }
            else
            {
                ActivateTime = activateTimeValue;
            }

            TriggeredActivationRequired = inf.ReadBoolean();

            int triggerActivations = inf.ReadInt32();
            activatedTrainTriggers = new List<TriggerActivation>();

            for (int itrigger = 0; itrigger < triggerActivations; itrigger++)
            {
                TriggerActivation newTrigger = new TriggerActivation();
                newTrigger.activatedName = inf.ReadString();
                newTrigger.activatedTrain = inf.ReadInt32();
                newTrigger.activationType = (TriggerActivationType)inf.ReadInt32();
                newTrigger.platformId = inf.ReadInt32();

                activatedTrainTriggers.Add(newTrigger);
            }

            int totalWait = inf.ReadInt32();
            if (totalWait > 0)
            {
                WaitList = new List<WaitInfo>();
                for (int iWait = 0; iWait < totalWait; iWait++)
                {
                    WaitList.Add(new WaitInfo(inf));
                }
            }

            int totalWaitAny = inf.ReadInt32();

            if (totalWaitAny > 0)
            {
                WaitAnyList = new Dictionary<int, List<WaitInfo>>();
                for (int iWait = 0; iWait < totalWaitAny; iWait++)
                {
                    int keyvalue = inf.ReadInt32();

                    List<WaitInfo> newList = new List<WaitInfo>();
                    int totalWaitInfo = inf.ReadInt32();
                    for (int iWinfo = 0; iWinfo < totalWaitInfo; iWinfo++)
                    {
                        newList.Add(new WaitInfo(inf));
                    }
                    WaitAnyList.Add(keyvalue, newList);
                }
            }

            Stable_CallOn = inf.ReadBoolean();

            Forms = inf.ReadInt32();
            FormsStatic = inf.ReadBoolean();
            ExitPool = inf.ReadString();
            PoolAccessSection = inf.ReadInt32();
            PoolStorageIndex = inf.ReadInt32();
            PoolExitDirection = (TimetablePool.PoolExitDirectionEnum)inf.ReadInt32();

            ActiveTurntable = null;
            if (inf.ReadBoolean())
            {
                ActiveTurntable = new TimetableTurntableControl(inf, AI.Simulator, this);
            }

            FormedOf = inf.ReadInt32();
            FormedOfType = (FormCommand)inf.ReadInt32();
            OrgAINumber = inf.ReadInt32();
            SetStop = inf.ReadBoolean();
            FormsAtStation = inf.ReadBoolean();

            int totalDetachLists = inf.ReadInt32();
            DetachDetails = new Dictionary<int, List<DetachInfo>>();

            for (int iDetachList = 0; iDetachList < totalDetachLists; iDetachList++)
            {
                int detachKey = inf.ReadInt32();
                int totalDetach = inf.ReadInt32();
                List<DetachInfo> DetachDetailsList = new List<DetachInfo>();
                for (int iDetach = 0; iDetach < totalDetach; iDetach++)
                {
                    DetachDetailsList.Add(new DetachInfo(inf));
                }
                DetachDetails.Add(detachKey, DetachDetailsList);
            }

            DetachActive = new int[2];
            DetachActive[0] = inf.ReadInt32();
            DetachActive[1] = inf.ReadInt32();
            DetachUnits = inf.ReadInt32();
            DetachPosition = inf.ReadBoolean();
            DetachPending = inf.ReadBoolean();

            bool attachValid = inf.ReadBoolean();
            AttachDetails = null;
            if (attachValid)
            {
                AttachDetails = new AttachInfo(inf);
            }

            PickUpTrains = new List<int>();
            int totalPickUpTrains = inf.ReadInt32();
            for (int iPickUp = 0; iPickUp < totalPickUpTrains; iPickUp++)
            {
                PickUpTrains.Add(inf.ReadInt32());
            }

            PickUpStatic = new List<int>();
            int totalPickUpStatic = inf.ReadInt32();
            for (int iPickUp = 0; iPickUp < totalPickUpStatic; iPickUp++)
            {
                PickUpStatic.Add(inf.ReadInt32());
            }

            PickUpStaticOnForms = inf.ReadBoolean();

            NeedPickUp = inf.ReadBoolean();

            TransferStationDetails = new Dictionary<int, TransferInfo>();
            int totalStationTransfers = inf.ReadInt32();
            for (int iTransferList = 0; iTransferList < totalStationTransfers; iTransferList++)
            {
                int stationKey = inf.ReadInt32();
                TransferInfo thisTransfer = new TransferInfo(inf);
                TransferStationDetails.Add(stationKey, thisTransfer);
            }

            TransferTrainDetails = new Dictionary<int, List<TransferInfo>>();
            int totalTrainTransfers = inf.ReadInt32();
            for (int iTransferList = 0; iTransferList < totalTrainTransfers; iTransferList++)
            {
                int trainKey = inf.ReadInt32();
                int totalTransfer = inf.ReadInt32();
                List<TransferInfo> thisTransferList = new List<TransferInfo>();
                for (int iTransfer = 0; iTransfer < totalTransfer; iTransfer++)
                {
                    TransferInfo thisTransfer = new TransferInfo(inf);
                    thisTransferList.Add(thisTransfer);
                }
                TransferTrainDetails.Add(trainKey, thisTransferList);
            }

            int totalNeedAttach = inf.ReadInt32();
            NeedAttach = new Dictionary<int, List<int>>();

            for (int iNeedAttach = 0; iNeedAttach < totalNeedAttach; iNeedAttach++)
            {
                int needAttachKey = inf.ReadInt32();
                int totalNeedAttachInfo = inf.ReadInt32();
                List<int> allNeedAttachInfo = new List<int>();

                for (int iNeedInfo = 0; iNeedInfo < totalNeedAttachInfo; iNeedInfo++)
                {
                    int needAttachInfo = inf.ReadInt32();
                    allNeedAttachInfo.Add(needAttachInfo);
                }

                NeedAttach.Add(needAttachKey, allNeedAttachInfo);
            }

            int totalNeedStationTransfer = inf.ReadInt32();
            NeedStationTransfer = new Dictionary<int, List<int>>();

            for (int iNeedTransferList = 0; iNeedTransferList < totalNeedStationTransfer; iNeedTransferList++)
            {
                int transferStationKey = inf.ReadInt32();
                List<int> tempList = new List<int>();
                int totalTransfers = inf.ReadInt32();

                for (int iTransfer = 0; iTransfer < totalTransfers; iTransfer++)
                {
                    tempList.Add(inf.ReadInt32());
                }
                NeedStationTransfer.Add(transferStationKey, tempList);
            }

            int totalNeedTrainTransfer = inf.ReadInt32();
            NeedTrainTransfer = new Dictionary<int,int>();

            for (int iNeedTransferList = 0; iNeedTransferList < totalNeedTrainTransfer; iNeedTransferList++)
            {
                int transferTrainKey = inf.ReadInt32();
                int transferTrainValue = inf.ReadInt32();
                NeedTrainTransfer.Add(transferTrainKey, transferTrainValue);
            }

            DelayedStartSettings = new DelayedStartValues();
            DelayedStartSettings.newStart.fixedPartS = inf.ReadInt32();
            DelayedStartSettings.newStart.randomPartS = inf.ReadInt32();
            DelayedStartSettings.pathRestart.fixedPartS = inf.ReadInt32();
            DelayedStartSettings.pathRestart.randomPartS = inf.ReadInt32();
            DelayedStartSettings.followRestart.fixedPartS = inf.ReadInt32();
            DelayedStartSettings.followRestart.randomPartS = inf.ReadInt32();
            DelayedStartSettings.stationRestart.fixedPartS = inf.ReadInt32();
            DelayedStartSettings.stationRestart.randomPartS = inf.ReadInt32();
            DelayedStartSettings.attachRestart.fixedPartS = inf.ReadInt32();
            DelayedStartSettings.attachRestart.randomPartS = inf.ReadInt32();
            DelayedStartSettings.detachRestart.fixedPartS = inf.ReadInt32();
            DelayedStartSettings.detachRestart.randomPartS = inf.ReadInt32();
            DelayedStartSettings.movingtableRestart.fixedPartS = inf.ReadInt32();
            DelayedStartSettings.movingtableRestart.randomPartS = inf.ReadInt32();
            DelayedStartSettings.reverseAddedDelaySperM = inf.ReadSingle();

            DelayedStart = inf.ReadBoolean();
            DelayedStartState = (AI_START_MOVEMENT)inf.ReadInt32();
            RestdelayS = inf.ReadSingle();

            // preset speed values
            SpeedSettings = new SpeedValues();

            SpeedSettings.routeSpeedMpS = inf.ReadSingle();
            SpeedSettings.consistSpeedMpS = inf.ReadSingle();

            if (inf.ReadBoolean())
            {
                SpeedSettings.maxSpeedMpS = inf.ReadSingle();
            }
            else
            {
                SpeedSettings.maxSpeedMpS = null;
            }

            if (inf.ReadBoolean())
            {
                SpeedSettings.cruiseSpeedMpS = inf.ReadSingle();
            }
            else
            {
                SpeedSettings.cruiseSpeedMpS = null;
            }

            if (inf.ReadBoolean())
            {
                SpeedSettings.cruiseMaxDelayS = inf.ReadInt32();
            }
            else
            {
                SpeedSettings.cruiseMaxDelayS = null;
            }

            if (inf.ReadBoolean())
            {
                SpeedSettings.creepSpeedMpS = inf.ReadSingle();
            }
            else
            {
                SpeedSettings.creepSpeedMpS = null;
            }

            if (inf.ReadBoolean())
            {
                SpeedSettings.attachSpeedMpS = inf.ReadSingle();
            }
            else
            {
                SpeedSettings.attachSpeedMpS = null;
            }

            if (inf.ReadBoolean())
            {
                SpeedSettings.detachSpeedMpS = inf.ReadSingle();
            }
            else
            {
                SpeedSettings.detachSpeedMpS = null;
            }

            if (inf.ReadBoolean())
            {
                SpeedSettings.movingtableSpeedMpS = inf.ReadSingle();
            }
            else
            {
                SpeedSettings.movingtableSpeedMpS = null;
            }

            SpeedSettings.restrictedSet = inf.ReadBoolean();

            DriverOnlyOperation = inf.ReadBoolean();
            ForceReversal = inf.ReadBoolean();

            Briefing = inf.ReadString();

            // reset actions if train is active
            bool activeTrain = true;

            if (TrainType == TRAINTYPE.AI_NOTSTARTED) activeTrain = false;
            if (TrainType == TRAINTYPE.AI_AUTOGENERATE) activeTrain = false;

            if (activeTrain)
            {
                if (MovementState == AI_MOVEMENT_STATE.AI_STATIC || MovementState == AI_MOVEMENT_STATE.INIT) activeTrain = false;
            }

            if (activeTrain)
            {
                ResetActions(true);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Save
        /// Override from Train class
        /// </summary>

        public override void Save(BinaryWriter outf)
        {
            outf.Write("TT");
            SaveBase(outf);

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
            outf.Write(doorCloseAdvance);
            outf.Write(doorOpenDelay);
            // dummy for level crossing horn pattern
            outf.Write(-1);

            // dummy for service list count
            outf.Write(-1);

            //TTTrains own additional fields
            outf.Write(Closeup);
            outf.Write(Created);
            outf.Write(CreateAhead);
            outf.Write(CreateFromPool);
            outf.Write(CreateInPool);
            outf.Write(ForcedConsistName);
            outf.Write((int)CreatePoolDirection);

            outf.Write(MaxAccelMpSSP);
            outf.Write(MaxAccelMpSSF);
            outf.Write(MaxDecelMpSSP);
            outf.Write(MaxDecelMpSSF);

            if (ActivateTime.HasValue)
            {
                outf.Write(ActivateTime.Value);
            }
            else
            {
                outf.Write(-1);
            }

            outf.Write(TriggeredActivationRequired);

            if (activatedTrainTriggers.Count > 0)
            {
                outf.Write(activatedTrainTriggers.Count);
                foreach (TriggerActivation thisTrigger in activatedTrainTriggers)
                {
                    outf.Write(thisTrigger.activatedName);
                    outf.Write(thisTrigger.activatedTrain);
                    outf.Write((int)thisTrigger.activationType);
                    outf.Write(thisTrigger.platformId);
                }
            }
            else
            {
                outf.Write(-1);
            }

            if (WaitList == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(WaitList.Count);
                foreach (WaitInfo thisWait in WaitList)
                {
                    thisWait.Save(outf);
                }
            }

            if (WaitAnyList == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(WaitAnyList.Count);
                foreach (KeyValuePair<int, List<WaitInfo>> thisWInfo in WaitAnyList)
                {
                    outf.Write((int)thisWInfo.Key);
                    List<WaitInfo> thisWaitList = thisWInfo.Value;

                    outf.Write(thisWaitList.Count);
                    foreach (WaitInfo thisWaitInfo in thisWaitList)
                    {
                        thisWaitInfo.Save(outf);
                    }
                }
            }

            outf.Write(Stable_CallOn);

            outf.Write(Forms);
            outf.Write(FormsStatic);
            outf.Write(ExitPool);
            outf.Write(PoolAccessSection);
            outf.Write(PoolStorageIndex);
            outf.Write((int)PoolExitDirection);

            if (ActiveTurntable == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                ActiveTurntable.Save(outf);
            }

            outf.Write(FormedOf);
            outf.Write((int)FormedOfType);
            outf.Write(OrgAINumber);
            outf.Write(SetStop);
            outf.Write(FormsAtStation);

            outf.Write(DetachDetails.Count);
            foreach (KeyValuePair<int, List<DetachInfo>> thisDetachInfo in DetachDetails)
            {
                outf.Write(thisDetachInfo.Key);
                outf.Write(thisDetachInfo.Value.Count);
                foreach (DetachInfo thisDetach in thisDetachInfo.Value)
                {
                    thisDetach.Save(outf);
                }
            }
            outf.Write(DetachActive[0]);
            outf.Write(DetachActive[1]);
            outf.Write(DetachUnits);
            outf.Write(DetachPosition);
            outf.Write(DetachPending);

            if (AttachDetails == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                AttachDetails.Save(outf);
            }

            outf.Write(PickUpTrains.Count);
            foreach (int thisTrainNumber in PickUpTrains)
            {
                outf.Write(thisTrainNumber);
            }

            outf.Write(PickUpStatic.Count);
            foreach (int thisStaticNumber in PickUpStatic)
            {
                outf.Write(thisStaticNumber);
            }

            outf.Write(PickUpStaticOnForms);

            outf.Write(NeedPickUp);

            outf.Write(TransferStationDetails.Count);
            foreach (KeyValuePair<int, TransferInfo> thisStationTransfer in TransferStationDetails)
            {
                outf.Write(thisStationTransfer.Key);
                TransferInfo thisTransfer = thisStationTransfer.Value;
                thisTransfer.Save(outf);
            }

            outf.Write(TransferTrainDetails.Count);
            foreach (KeyValuePair<int, List<TransferInfo>> thisTrainTransfer in TransferTrainDetails)
            {
                outf.Write(thisTrainTransfer.Key);
                List<TransferInfo> thisTransferList = thisTrainTransfer.Value;
                outf.Write(thisTransferList.Count);
                foreach (TransferInfo thisTransfer in thisTransferList)
                {
                    thisTransfer.Save(outf);
                }
            }

            outf.Write(NeedAttach.Count);
            foreach (KeyValuePair<int, List<int>> thisNeedAttach in NeedAttach)
            {
                outf.Write(thisNeedAttach.Key);
                outf.Write(thisNeedAttach.Value.Count);
                foreach (int needAttachInfo in thisNeedAttach.Value)
                {
                    outf.Write(needAttachInfo);
                }
            }

            outf.Write(NeedStationTransfer.Count);
            foreach (KeyValuePair<int, List<int>> thisNeedTransfer in NeedStationTransfer)
            {
                outf.Write(thisNeedTransfer.Key);
                outf.Write(thisNeedTransfer.Value.Count);
                foreach (int needTransferInfo in thisNeedTransfer.Value)
                {
                    outf.Write(needTransferInfo);
                }
            }

            outf.Write(NeedTrainTransfer.Count);
            foreach (KeyValuePair<int,int> thisNeedTransfer in NeedTrainTransfer)
            {
                outf.Write(thisNeedTransfer.Key);
                outf.Write(thisNeedTransfer.Value);
            }

            outf.Write(DelayedStartSettings.newStart.fixedPartS);
            outf.Write(DelayedStartSettings.newStart.randomPartS);
            outf.Write(DelayedStartSettings.pathRestart.fixedPartS);
            outf.Write(DelayedStartSettings.pathRestart.randomPartS);
            outf.Write(DelayedStartSettings.followRestart.fixedPartS);
            outf.Write(DelayedStartSettings.followRestart.randomPartS);
            outf.Write(DelayedStartSettings.stationRestart.fixedPartS);
            outf.Write(DelayedStartSettings.stationRestart.randomPartS);
            outf.Write(DelayedStartSettings.attachRestart.fixedPartS);
            outf.Write(DelayedStartSettings.attachRestart.randomPartS);
            outf.Write(DelayedStartSettings.detachRestart.fixedPartS);
            outf.Write(DelayedStartSettings.detachRestart.randomPartS);
            outf.Write(DelayedStartSettings.movingtableRestart.fixedPartS);
            outf.Write(DelayedStartSettings.movingtableRestart.randomPartS);
            outf.Write(DelayedStartSettings.reverseAddedDelaySperM);

            outf.Write(DelayedStart);
            outf.Write((int)DelayedStartState);
            outf.Write(RestdelayS);

            outf.Write(SpeedSettings.routeSpeedMpS);
            outf.Write(SpeedSettings.consistSpeedMpS);

            outf.Write(SpeedSettings.maxSpeedMpS.HasValue);
            if (SpeedSettings.maxSpeedMpS.HasValue)
            {
                outf.Write(SpeedSettings.maxSpeedMpS.Value);
            }
            outf.Write(SpeedSettings.cruiseSpeedMpS.HasValue);
            if (SpeedSettings.cruiseSpeedMpS.HasValue)
            {
                outf.Write(SpeedSettings.cruiseSpeedMpS.Value);
            }
            outf.Write(SpeedSettings.cruiseMaxDelayS.HasValue);
            if (SpeedSettings.cruiseMaxDelayS.HasValue)
            {
                outf.Write(SpeedSettings.cruiseMaxDelayS.Value);
            }
            outf.Write(SpeedSettings.creepSpeedMpS.HasValue);
            if (SpeedSettings.creepSpeedMpS.HasValue)
            {
                outf.Write(SpeedSettings.creepSpeedMpS.Value);
            }
            outf.Write(SpeedSettings.attachSpeedMpS.HasValue);
            if (SpeedSettings.attachSpeedMpS.HasValue)
            {
                outf.Write(SpeedSettings.attachSpeedMpS.Value);
            }
            outf.Write(SpeedSettings.detachSpeedMpS.HasValue);
            if (SpeedSettings.detachSpeedMpS.HasValue)
            {
                outf.Write(SpeedSettings.detachSpeedMpS.Value);
            }
            outf.Write(SpeedSettings.movingtableSpeedMpS.HasValue);
            if (SpeedSettings.movingtableSpeedMpS.HasValue)
            {
                outf.Write(SpeedSettings.movingtableSpeedMpS.Value);
            }
            outf.Write(SpeedSettings.restrictedSet);
            outf.Write(DriverOnlyOperation);
            outf.Write(ForceReversal);
            outf.Write(Briefing);
        }


        //================================================================================================//
        /// <summary>
        /// Terminate route at last signal in train's direction
        /// </summary>

        public void EndRouteAtLastSignal()
        {
            // no action required
            if (ReqLastSignalStop == LastSignalStop.None)
            {
                return;
            }

            int lastIndex = TCRoute.TCRouteSubpaths.Count - 1;
            TCSubpathRoute lastSubpath = new TCSubpathRoute(TCRoute.TCRouteSubpaths[lastIndex]);

            int lastSectionIndex = -1;

            // search for last signal in required direction
            for (int iIndex = lastSubpath.Count - 1; iIndex >= 0 && lastSectionIndex < 0; iIndex--)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[lastSubpath[iIndex].TCSectionIndex];
                int reqEndSignal = ReqLastSignalStop == LastSignalStop.Last ? lastSubpath[iIndex].Direction : lastSubpath[iIndex].Direction == 0 ? 1 : 0;

                if (thisSection.EndSignals[reqEndSignal] != null)
                {
                    lastSectionIndex = iIndex;
                }
            }

            // remove sections beyond last signal
            for (int iIndex = lastSubpath.Count - 1; iIndex > lastSectionIndex; iIndex--)
            {
                lastSubpath.RemoveAt(iIndex);
            }

            // reinsert subroute
            TCRoute.TCRouteSubpaths.RemoveAt(lastIndex);
            TCRoute.TCRouteSubpaths.Add(new TCSubpathRoute(lastSubpath));
        }

        //================================================================================================//
        /// <summary>
        /// Set alternative station stop when alternative path is selected
        /// Override from Train class
        /// </summary>
        /// <param name="orgStop"></param>
        /// <param name="newRoute"></param>
        /// <returns></returns>

        public override StationStop SetAlternativeStationStop(StationStop orgStop, TCSubpathRoute newRoute)
        {
            int altPlatformIndex = -1;

            // get station platform list
            if (signalRef.StationXRefList.ContainsKey(orgStop.PlatformItem.Name))
            {
                List<int> XRefKeys = signalRef.StationXRefList[orgStop.PlatformItem.Name];

                // search through all available platforms
                for (int platformIndex = 0; platformIndex <= XRefKeys.Count - 1 && altPlatformIndex < 0; platformIndex++)
                {
                    int platformXRefIndex = XRefKeys[platformIndex];
                    PlatformDetails altPlatform = signalRef.PlatformDetailsList[platformXRefIndex];

                    // check if section is in new route
                    for (int iSectionIndex = 0; iSectionIndex <= altPlatform.TCSectionIndex.Count - 1 && altPlatformIndex < 0; iSectionIndex++)
                    {
                        if (newRoute.GetRouteIndex(altPlatform.TCSectionIndex[iSectionIndex], 0) > 0)
                        {
                            altPlatformIndex = platformXRefIndex;
                        }
                    }
                }

                // remove holding signal if set
                int holdSig = -1;
                if (orgStop.HoldSignal && orgStop.ExitSignal >= 0 && HoldingSignals.Contains(orgStop.ExitSignal))
                {
                    holdSig = orgStop.ExitSignal;
                    HoldingSignals.Remove(holdSig);
                }

                // section found in new route - set new station details using old details
                if (altPlatformIndex >= 0)
                {
                    bool isNewPlatform = true;
                    // check if new found platform is actually same as original
                    foreach (int platfReference in signalRef.PlatformDetailsList[altPlatformIndex].PlatformReference)
                    {
                        if (platfReference == orgStop.PlatformReference)
                        {
                            isNewPlatform = false;
                            break;
                        }
                    }

                    // if platform found is original platform, reinstate hold signal but take no further action
                    if (!isNewPlatform)
                    {
                        if (holdSig >= 0) HoldingSignals.Add(holdSig);
                        return (orgStop);
                    }
                    else
                    {
                        // calculate new stop
                        StationStop newStop = CalculateStationStop(signalRef.PlatformDetailsList[altPlatformIndex].PlatformReference[0],
                        orgStop.ArrivalTime, orgStop.DepartTime, orgStop.arrivalDT, orgStop.departureDT, clearingDistanceM, minStopDistanceM,
                        orgStop.Terminal, orgStop.ActualMinStopTime, orgStop.KeepClearFront, orgStop.KeepClearRear, orgStop.ForcePosition,
                        orgStop.CloseupSignal, orgStop.Closeup, orgStop.RestrictPlatformToSignal, orgStop.ExtendPlatformToSignal, orgStop.EndStop);

                        // add new holding signal if required
                        if (newStop.HoldSignal && newStop.ExitSignal >= 0)
                        {
                            HoldingSignals.Add(newStop.ExitSignal);
                        }

#if DEBUG_REPORTS
                    if (newStop != null)
                    {
                        File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number.ToString() +
                        " : alternative stop required for " + orgStop.PlatformItem.Name +
                        " ; found : " + newStop.PlatformReference + "\n");
                    }
                    else
                    {
                        File.AppendAllText(@"C:\temp\printproc.txt", "Train " + Number.ToString() +
                        " : alternative stop required for " + orgStop.PlatformItem.Name +
                        " ; not found \n");
                    }
#endif

                        if (CheckTrain)
                        {
                            if (newStop != null)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number.ToString() +
                                " : alternative stop required for " + orgStop.PlatformItem.Name +
                                " ; found : " + newStop.PlatformReference + "\n");
                            }
                            else
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number.ToString() +
                                " : alternative stop required for " + orgStop.PlatformItem.Name +
                                " ; not found \n");
                            }
                        }

                        if (newStop != null)
                        {
                            foreach (KeyValuePair<int, WaitInfo> thisConnect in orgStop.ConnectionDetails)
                            {
                                newStop.ConnectionDetails.Add(thisConnect.Key, thisConnect.Value);
                            }
                        }

                        return (newStop);
                    }
                }
            }

            return (null);
        }

        //================================================================================================//
        /// <summary>
        /// Post Init (override from Train) (with activate train option)
        /// perform all actions required to start
        /// </summary>

        public bool PostInit(bool activateTrain)
        {

#if DEBUG_CHECKTRAIN
            if (!CheckTrain)
            {
                if (Number == 595)
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
            // if train itself forms other train, check if train is to end at station (only if other train is not autogen and this train has SetStop set)

            if (Forms >= 0 && SetStop)
            {
                TTTrain nextTrain = AI.StartList.GetNotStartedTTTrainByNumber(Forms, false);

                if (nextTrain != null && nextTrain.StationStops != null && nextTrain.StationStops.Count > 0)
                {
                    TCSubpathRoute lastSubpath = TCRoute.TCRouteSubpaths[TCRoute.TCRouteSubpaths.Count - 1];
                    int lastSectionIndex = lastSubpath[lastSubpath.Count - 1].TCSectionIndex;

                    if (nextTrain.StationStops[0].PlatformItem.TCSectionIndex.Contains(lastSectionIndex))
                    {
                        StationStops = new List<StationStop>();
                        StationStop newStop = nextTrain.StationStops[0].CreateCopy();

                        int startvalue = nextTrain.ActivateTime.HasValue ? nextTrain.ActivateTime.Value : nextTrain.StartTime.Value;

                        newStop.ArrivalTime = startvalue;
                        newStop.DepartTime = startvalue;
                        newStop.arrivalDT = new DateTime((long)(startvalue * Math.Pow(10, 7)));
                        newStop.departureDT = new DateTime((long)(startvalue * Math.Pow(10, 7)));
                        newStop.RouteIndex = lastSubpath.GetRouteIndex(newStop.TCSectionIndex, 0);
                        newStop.SubrouteIndex = TCRoute.TCRouteSubpaths.Count - 1;
                        if (newStop.RouteIndex >= 0)
                        {
                            StationStops.Add(newStop); // do not set stop if platform is not on route

                            // switch stop position if train is to reverse
                            int nextTrainRouteIndex = nextTrain.TCRoute.TCRouteSubpaths[0].GetRouteIndex(lastSectionIndex, 0);
                            if (nextTrainRouteIndex >= 0 && nextTrain.TCRoute.TCRouteSubpaths[0][nextTrainRouteIndex].Direction != lastSubpath[newStop.RouteIndex].Direction)
                            {
                                TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastSectionIndex];
                                newStop.StopOffset = lastSection.Length - newStop.StopOffset + Length;
                            }
                        }
                    }
                }
            }

            if (Forms >= 0 && FormsAtStation && StationStops != null && StationStops.Count > 0)  // curtail route to last station stop
            {
                StationStop lastStop = StationStops[StationStops.Count - 1];
                TCSubpathRoute reqSubroute = TCRoute.TCRouteSubpaths[lastStop.SubrouteIndex];
                for (int iRouteIndex = reqSubroute.Count - 1; iRouteIndex > lastStop.RouteIndex; iRouteIndex--)
                {
                    reqSubroute.RemoveAt(iRouteIndex);
                }

                // if subroute is present route, create new ValidRoute
                if (lastStop.SubrouteIndex == TCRoute.activeSubpath)
                {
                    ValidRoute[0] = new TCSubpathRoute(TCRoute.TCRouteSubpaths[TCRoute.activeSubpath]);
                }
            }

            // on activation : if train is to join pool, set proper dispose details
            // copy new route if required

            if (activateTrain && !String.IsNullOrEmpty(ExitPool) && ActiveTurntable == null)
            {
                TimetablePool thisPool = Simulator.PoolHolder.Pools[ExitPool];
                bool validPool = thisPool.TestPoolExit(this);

                if (validPool)
                {
                    PoolAccessSection = TCRoute.TCRouteSubpaths.Last().Last().TCSectionIndex;
                }
                else
                {
                    ExitPool = String.Empty;
                }
            }

            // check deadlocks (if train has valid activate time only - otherwise it is static and won't move)

            if (ActivateTime.HasValue)
            {
                CheckDeadlock(ValidRoute[0], Number);
            }

            // set initial position and state

            bool atStation = AtStation;
            bool validPosition = InitialTrainPlacement(String.IsNullOrEmpty(CreateAhead));     // Check track and if clear, set occupied

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

                InitializeSignals(false);               // Get signal information
                TCRoute.SetReversalOffset(Length);      // set reversal information for first subpath
                SetEndOfRouteAction();                  // set action to ensure train stops at end of route
                ControlMode = TRAIN_CONTROL.INACTIVE;   // set control mode to INACTIVE

                // active train
                if (activateTrain)
                {
                    MovementState = AI_MOVEMENT_STATE.INIT;        // start in INIT mode to collect info
                    ControlMode = TRAIN_CONTROL.AUTO_NODE;         // start up in NODE control

                    // if there is an active turntable and action is not completed, start in turntable state
                    if (ActiveTurntable != null && ActiveTurntable.MovingTableState != TimetableTurntableControl.MovingTableStateEnum.Completed)
                    {
                        MovementState = AI_MOVEMENT_STATE.TURNTABLE;
                        if (TrainType == TRAINTYPE.PLAYER)
                        {
                            if (ActiveTurntable.MovingTableState == TimetableTurntableControl.MovingTableStateEnum.WaitingMovingTableAvailability)
                            {
                                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                                    Simulator.Confirmer.Information("Wait for turntable to become available");
                            }
                        }
                    }

                    // recalculate station stops based on present train length
                    RecalculateStationStops(atStation);

                    // check if train starts at station stop
                    if (StationStops.Count > 0 && !atStation)
                    {
                        atStation = CheckInitialStation();

                        if (!atStation)
                        {
                            if (StationStops.Count > 0)
                            {
                                SetNextStationAction();               // set station details
                            }
                        }
                    }
                    else if (atStation)
                    {
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
                }
                // start train as static
                else
                {
                    MovementState = AI_MOVEMENT_STATE.AI_STATIC;   // start in STATIC mode until required activate time
                }
            }

            if (CheckTrain)
            {
                DateTime baseDT = new DateTime();
                DateTime actTime = baseDT.AddSeconds(AI.clockTime);

                File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "PostInit at " + actTime.ToString("HH:mm:ss") + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Train : " + Number.ToString() + " ( AI : " + OrgAINumber.ToString() + " )\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Name  : " + Name + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Frght : " + IsFreight.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Length: " + Length.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "MaxSpd: " + TrainMaxSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Start : " + (StartTime.HasValue ? StartTime.Value.ToString() : "--:--:--") + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Active: " + (ActivateTime.HasValue ? ActivateTime.Value.ToString() : "------") + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "ActTrg: " + TriggeredActivationRequired.ToString());
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

                File.AppendAllText(@"C:\temp\checktrain.txt", "Occupied sections : \n");
                foreach (TrackCircuitSection section in OccupiedTrack)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "    Section : " + section.Index + "\n\n");
                }

                if (!String.IsNullOrEmpty(ExitPool))
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "    Exit to Pool : " + ExitPool + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "    From section : " + PoolAccessSection + "\n");
                }

            }

            return (validPosition);
        }

        //================================================================================================//
        /// <summary>
        /// Post Init : perform all actions required to start
        /// Override from Train class
        /// </summary>

        public override bool PostInit()
        {
#if DEBUG_CHECKTRAIN
            if (!CheckTrain)
            {
                if (Number == 595)
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
            // start ahead of train if required

            bool validPosition = true;

            if (!String.IsNullOrEmpty(CreateAhead))
            {
                CalculateInitialTTTrainPosition(ref validPosition, null);
            }

            // if not yet started, start normally

            if (validPosition)
            {
                validPosition = InitialTrainPlacement(true);
            }

            if (validPosition)
            {
                InitializeSignals(false);     // Get signal information - only if train has route //
                if (TrainType != TRAINTYPE.STATIC)
                    CheckDeadlock(ValidRoute[0], Number);    // Check deadlock against all other trains (not for static trains)
                if (TCRoute != null) TCRoute.SetReversalOffset(Length);
            }

            // set train speed logging flag (valid per activity, so will be restored after save)

            if (TrainType == TRAINTYPE.PLAYER)
            {
                SetupStationStopHandling(); // player train must now perform own station stop handling (no activity function available)

                DatalogTrainSpeed = Simulator.Settings.DataLogTrainSpeed;
                DatalogTSInterval = Simulator.Settings.DataLogTSInterval;

                DatalogTSContents = new int[Simulator.Settings.DataLogTSContents.Length];
                Simulator.Settings.DataLogTSContents.CopyTo(DatalogTSContents, 0);

                // if logging required, derive filename and open file
                if (DatalogTrainSpeed)
                {
                    DataLogFile = Simulator.DeriveLogFile("Speed");
                    if (String.IsNullOrEmpty(DataLogFile))
                    {
                        DatalogTrainSpeed = false;
                    }
                    else
                    {
                        CreateLogFile();
                    }
                }

                // if debug, print out all passing paths

#if DEBUG_DEADLOCK
                Printout_PassingPaths();
#endif
            }

            return (validPosition);
        }


        //================================================================================================//
        /// <summary>
        /// Calculate actual station stop details
        /// <\summary>

        public StationStop CalculateStationStop(int platformStartID, int arrivalTime, int departTime, DateTime arrivalDT, DateTime departureDT, float clearingDistanceM,
            float minStopDistance, bool terminal, int? actMinStopTime, float? keepClearFront, float? keepClearRear, bool forcePosition, bool closeupSignal, 
            bool closeup, bool restrictPlatformToSignal, bool extendPlatformToSignal, bool endStop)
        {
            int platformIndex;
            int activeSubroute = 0;

            TCSubpathRoute thisRoute = TCRoute.TCRouteSubpaths[activeSubroute];

            // get platform details

            if (!signalRef.PlatformXRefList.TryGetValue(platformStartID, out platformIndex))
            {
                return (null); // station not found
            }
            else
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
                    Trace.TraceWarning("Train {0} ({1}) : platform {2} is not on route",
                            Name, Number.ToString(), platformStartID.ToString());
                    return (null);
                }

                // determine end stop position depending on direction

                StationStop dummyStop = CalculateStationStopPosition(thisRoute, routeIndex, thisPlatform, activeSubroute,
                    keepClearFront, keepClearRear, forcePosition, closeupSignal, closeup, restrictPlatformToSignal, extendPlatformToSignal, 
                    terminal, platformIndex);

                // build and add station stop

                StationStop thisStation = new StationStop(
                        platformStartID,
                        thisPlatform,
                        activeSubroute,
                        dummyStop.RouteIndex,
                        dummyStop.TCSectionIndex,
                        dummyStop.Direction,
                        dummyStop.ExitSignal,
                        dummyStop.HoldSignal,
                        false,
                        false,
                        dummyStop.StopOffset,
                        arrivalTime,
                        departTime,
                        terminal,
                        actMinStopTime,
                        keepClearFront,
                        keepClearRear,
                        forcePosition,
                        closeupSignal,
                        closeup,
                        restrictPlatformToSignal,
                        extendPlatformToSignal,
                        endStop,
                        StationStop.STOPTYPE.STATION_STOP);

                thisStation.arrivalDT = arrivalDT;
                thisStation.departureDT = departureDT;

                return (thisStation);
            }
        }

        /// <summary>
        /// Calculate station stop position
        /// </summary>
        /// <param name="thisRoute"></param>
        /// <param name="routeIndex"></param>
        /// <param name="thisPlatform"></param>
        /// <param name="activeSubroute"></param>
        /// <param name="keepClearFront"></param>
        /// <param name="keepClearRear"></param>
        /// <param name="forcePosition"></param>
        /// <param name="terminal"></param>
        /// <param name="platformIndex"></param>
        /// <returns></returns>
        public StationStop CalculateStationStopPosition(TCSubpathRoute thisRoute, int routeIndex, PlatformDetails thisPlatform, int activeSubroute,
            float? keepClearFront, float? keepClearRear, bool forcePosition, bool closeupSignal, bool closeup, 
            bool restrictPlatformToSignal, bool ExtendPlatformToSignal, bool terminal, int platformIndex)
        {
            StationStop dummyStop = new StationStop();

            TCRouteElement thisElement = thisRoute[routeIndex];

            int routeSectionIndex = thisElement.TCSectionIndex;
            int endSectionIndex = thisElement.Direction == 0 ?
                thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1] :
                thisPlatform.TCSectionIndex[0];
            int beginSectionIndex = thisElement.Direction == 0 ?
                thisPlatform.TCSectionIndex[0] :
                thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1];

            bool platformHasEndSignal = thisElement.Direction == 0 ? (thisPlatform.EndSignals[0] >= 0) : (thisPlatform.EndSignals[1] >= 0);
            float distanceToEndSignal = platformHasEndSignal ? (thisElement.Direction == 0 ? thisPlatform.DistanceToSignals[0] : thisPlatform.DistanceToSignals[1]) : -1;

            float endOffset = thisPlatform.TCOffset[1, thisElement.Direction];
            float beginOffset = thisPlatform.TCOffset[0, thisElement.Direction];

            float deltaLength = thisPlatform.Length - Length; // platform length - train length

            // get all sections which form platform
            TCSubpathRoute platformRoute = signalRef.BuildTempRoute(this, beginSectionIndex, beginOffset, thisElement.Direction, thisPlatform.Length, true, true, false);
            int platformRouteIndex = platformRoute.GetRouteIndex(routeSectionIndex, 0);

            TrackCircuitSection beginSection = signalRef.TrackCircuitList[platformRoute.First().TCSectionIndex];
            TrackCircuitSection endSection = signalRef.TrackCircuitList[platformRoute.Last().TCSectionIndex];

            int firstRouteIndex = thisRoute.GetRouteIndex(beginSectionIndex, 0);
            int lastRouteIndex = thisRoute.GetRouteIndex(endSectionIndex, 0);

            int signalSectionIndex = -1;
            TrackCircuitSection signalSection = null;
            int signalRouteIndex = -1;

            float fullLength = thisPlatform.Length;

            // path does not extend through station : adjust variables to last section
            if (lastRouteIndex < 0)
            {
                lastRouteIndex = thisRoute.Count - 1;
                routeSectionIndex = lastRouteIndex;
                routeIndex = lastRouteIndex;
                endSectionIndex = lastRouteIndex;
                endSection = signalRef.TrackCircuitList[thisRoute[lastRouteIndex].TCSectionIndex];
                endOffset = endSection.Length - 1.0f;
                platformHasEndSignal = (endSection.EndSignals[thisRoute[lastRouteIndex].Direction] != null);
                distanceToEndSignal = 1.0f;

                float newLength = -beginOffset;  // correct new length for begin offset
                for (int sectionRouteIndex = firstRouteIndex; sectionRouteIndex <= lastRouteIndex; sectionRouteIndex++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisRoute[sectionRouteIndex].TCSectionIndex];
                    newLength += thisSection.Length;
                }

                platformRoute = signalRef.BuildTempRoute(this, beginSectionIndex, beginOffset, thisElement.Direction, newLength, true, true, false);
                platformRouteIndex = routeIndex;

                deltaLength = newLength - Length; // platform length - train length
                fullLength = newLength;
            }

            // if required, check if there is a signal within the platform
            // not possible if there is only one section
            else if (restrictPlatformToSignal && !platformHasEndSignal && firstRouteIndex != lastRouteIndex)
            {
                bool intermediateSignal = false;

                for (int sectionRouteIndex = lastRouteIndex - 1; sectionRouteIndex >= firstRouteIndex; sectionRouteIndex--)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisRoute[sectionRouteIndex].TCSectionIndex];
                    if (thisSection.EndSignals[thisRoute[sectionRouteIndex].Direction] != null)
                    {
                        intermediateSignal = true;
                        signalSectionIndex = thisSection.Index;
                        signalSection = thisSection;
                        signalRouteIndex = sectionRouteIndex;
                        break;
                    }
                }

                // if signal found, reset all end indicators
                if (intermediateSignal)
                {
                    routeSectionIndex = signalRouteIndex;
                    routeIndex = signalRouteIndex;
                    lastRouteIndex = signalRouteIndex;
                    endSectionIndex = signalSectionIndex;
                    endOffset = signalSection.Length - keepDistanceCloseupSignalM;
                    platformHasEndSignal = true;
                    distanceToEndSignal = 1.0f;

                    endSection = signalSection;

                    float newLength = -beginOffset;  // correct new length for begin offset
                    for (int sectionRouteIndex = firstRouteIndex; sectionRouteIndex <= lastRouteIndex; sectionRouteIndex++)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisRoute[sectionRouteIndex].TCSectionIndex];
                        newLength += thisSection.Length;
                    }

                    platformRoute = signalRef.BuildTempRoute(this, beginSectionIndex, beginOffset, thisElement.Direction, newLength, true, true, false);
                    platformRouteIndex = routeIndex;

                    deltaLength = newLength - Length; // platform length - train length
                    fullLength = newLength;
                }
            }

            // extend platform to next signal
            // only if required, platform has no signal and train does not fit into platform
            else if (ExtendPlatformToSignal && !platformHasEndSignal && deltaLength < 0)
            {
                bool nextSignal = false;

                // find next signal in route
                for (int sectionRouteIndex = lastRouteIndex + 1; sectionRouteIndex < thisRoute.Count; sectionRouteIndex++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisRoute[sectionRouteIndex].TCSectionIndex];
                    if (thisSection.EndSignals[thisRoute[sectionRouteIndex].Direction] != null)
                    {
                        nextSignal = true;
                        signalSectionIndex = thisSection.Index;
                        signalSection = thisSection;
                        signalRouteIndex = sectionRouteIndex;
                        break;
                    }

                }

                // if signal found, reset all end indicators
                if (nextSignal)
                {
                    routeSectionIndex = signalRouteIndex;
                    routeIndex = signalRouteIndex;
                    lastRouteIndex = signalRouteIndex;
                    endSectionIndex = signalSectionIndex;
                    endOffset = signalSection.Length - keepDistanceCloseupSignalM;
                    platformHasEndSignal = true;
                    distanceToEndSignal = 1.0f;

                    endSection = signalSection;

                    float newLength = -beginOffset + endOffset;  // correct new length for begin offset and end offset
                    // do not add last section as that is included through endOffset
                    for (int sectionRouteIndex = firstRouteIndex; sectionRouteIndex < lastRouteIndex; sectionRouteIndex++)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisRoute[sectionRouteIndex].TCSectionIndex];
                        newLength += thisSection.Length;
                    }

                    platformRoute = signalRef.BuildTempRoute(this, beginSectionIndex, beginOffset, thisElement.Direction, newLength, true, true, false);
                    platformRouteIndex = routeIndex;

                    deltaLength = newLength - Length; // platform length - train length
                    fullLength = newLength;
                }
            }

            // calculate corrected offsets related to last section
            float beginOffsetCorrection = 0;
            float endOffsetCorrection = 0;

            if (firstRouteIndex < 0)
            {
                for (int iIndex = 0; iIndex < platformRouteIndex - 1; iIndex++)
                {
                    beginOffsetCorrection += signalRef.TrackCircuitList[platformRoute[iIndex].TCSectionIndex].Length;
                }
                firstRouteIndex = routeIndex;
                beginOffset -= beginOffsetCorrection;
            }
            else if (firstRouteIndex < routeIndex)
            {
                for (int iIndex = firstRouteIndex; iIndex <= routeIndex - 1; iIndex++)
                {
                    beginOffsetCorrection += signalRef.TrackCircuitList[thisRoute[iIndex].TCSectionIndex].Length;
                }
                firstRouteIndex = routeIndex;
                beginOffset -= beginOffsetCorrection;
            }

            if (lastRouteIndex < 0)
            {
                for (int iIndex = platformRouteIndex; iIndex < platformRoute.Count - 1; iIndex++)
                {
                    endOffsetCorrection += signalRef.TrackCircuitList[platformRoute[iIndex].TCSectionIndex].Length;
                }
                lastRouteIndex = routeIndex;
                endOffset += endOffsetCorrection;
            }
            else if (lastRouteIndex > routeIndex)
            {
                for (int iIndex = routeIndex; iIndex <= lastRouteIndex - 1; iIndex++)
                {
                    endOffsetCorrection += signalRef.TrackCircuitList[thisRoute[iIndex].TCSectionIndex].Length;
                }
                lastRouteIndex = routeIndex;
                endOffset += endOffsetCorrection;
            }

            // relate beginoffset and endoffset to section defined as platform section

            // if station is terminal, check if train is starting or terminating, and set stop position at 0.5 clearing distance from end

            float stopOffset = 0;
            bool forceThroughSignal = false;  // set if rear clearance is defined with force parameter

            // check for terminal position
            if (terminal)
            {
                int startRouteIndex = firstRouteIndex < lastRouteIndex ? firstRouteIndex : lastRouteIndex;
                int endRouteIndex = firstRouteIndex > lastRouteIndex ? firstRouteIndex : lastRouteIndex;

                bool routeNodeBeforeStart = false;
                bool routeNodeAfterEnd = false;

                // check if any junctions in path before start
                for (int iIndex = 0; iIndex < startRouteIndex && !routeNodeBeforeStart; iIndex++)
                {
                    routeNodeBeforeStart = (signalRef.TrackCircuitList[thisRoute[iIndex].TCSectionIndex].CircuitType == TrackCircuitSection.TrackCircuitType.Junction);
                }

                // check if any junctions in path after end
                for (int iIndex = lastRouteIndex + 1; iIndex < (thisRoute.Count - 1) && !routeNodeAfterEnd; iIndex++)
                {
                    routeNodeAfterEnd = (signalRef.TrackCircuitList[thisRoute[iIndex].TCSectionIndex].CircuitType == TrackCircuitSection.TrackCircuitType.Junction);
                }

                // check if terminal is at start of route
                if (firstRouteIndex == 0 || !routeNodeBeforeStart)
                {
                    stopOffset = beginOffset + (0.5f * clearingDistanceM) + Length;
                }
                // if at end of route use closeup distance
                else if (lastRouteIndex == thisRoute.Count - 1 || !routeNodeAfterEnd)
                {
                    stopOffset = endOffset - keepDistanceCloseupM;
                }
                // if inbetween use safety distance
                else
                {
                    stopOffset = endOffset - (0.5f * clearingDistanceM);
                }
            }

            // if train too long : search back for platform with same name
            else
            {
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

                // default stopposition : place train in middle of platform
                stopOffset = endOffset - (0.5f * deltaLength);

                // check if position is not beyond end of route
                TrackCircuitSection followingSection = signalRef.TrackCircuitList[endSectionIndex];
                float remLength = followingSection.Length - endOffset;

                for (int iSection = lastRouteIndex + 1; iSection < thisRoute.Count; iSection++ )
                {
                    followingSection = signalRef.TrackCircuitList[thisRoute[iSection].TCSectionIndex];
                    remLength += followingSection.Length;
                    if (followingSection.CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
                    {
                        remLength -= keepDistanceCloseupM; // stay clear from end of track
                    }
                    if (remLength > (0.5f * deltaLength)) break; // stop check if length exceeds required overshoot
                }

                stopOffset = Math.Min(stopOffset, endOffset + remLength);

                // keep clear at front
                if (keepClearFront.HasValue)
                {
                    // if force position is set, stop train as required regardless of rear position of train
                    if (forcePosition)
                    {
                        stopOffset = endOffset - keepClearFront.Value;
                    }
                    else
                    // keep clear at front but ensure train is in station
                    {
                        float frontClear = Math.Min(keepClearFront.Value, (endOffset - Length - beginOffset));
                        if (frontClear > 0) stopOffset = endOffset - frontClear;
                    }
                }
                else if (keepClearRear.HasValue)
                {
                    // if force position is set, stop train as required regardless of front position of train
                    // reset hold signal state if front position is passed signal
                    if (forcePosition)
                    {
                        stopOffset = beginOffset + keepClearRear.Value + Length;
                        forceThroughSignal = true;

                        // beyond original platform and beyond section : check for route validity (may not exceed route)
                        if (stopOffset > endOffset && stopOffset > endSection.Length)
                        {
                            float addOffset = stopOffset - endOffset;
                            float overlap = 0f;

                            for (int iIndex = lastRouteIndex + 1; iIndex < thisRoute.Count && overlap < addOffset; iIndex++)
                            {
                                TrackCircuitSection nextSection = signalRef.TrackCircuitList[thisRoute[iIndex].TCSectionIndex];
                                overlap += nextSection.Length;
                            }

                            if (overlap < addOffset)
                                stopOffset = endSection.Length + overlap;
                        }
                    }
                    else
                    {
                        // check if space available between end of platform and exit signal
                        float endPosition = endOffset;
                        if (platformHasEndSignal)
                        {
                            endPosition = endOffset + distanceToEndSignal;   // distance to signal
                            endPosition = closeupSignal ? (endPosition - keepDistanceCloseupSignalM - 1.0f) : (endPosition - clearingDistanceM - 1.0f);   // correct for clearing distance
                        }

                        stopOffset = Math.Min((beginOffset + keepClearRear.Value + Length), endPosition);

                        //float rearClear = Math.Min(keepClearRear.Value, (endOffset - stopOffset));
                        //if (rearClear > 0) stopOffset = beginOffset + rearClear + Length;
                    }
                }
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

            if (inDirection)
            {
                if (distanceToEndSignal >= 0)
                {
                    EndSignal = thisPlatform.EndSignals[useDirection];

                    // stop location is in front of signal
                    if (distanceToEndSignal > (stopOffset - endOffset))
                    {
                        HoldSignal = true;

                        // check if stop is too close to signal
                        // if platform length is forced always use closeup
                        if (ExtendPlatformToSignal || restrictPlatformToSignal)
                        {
                            stopOffset = Math.Min(stopOffset, endOffset + distanceToEndSignal - keepDistanceCloseupSignalM - 1.0f);
                        }
                        else if (!closeupSignal && (distanceToEndSignal + (endOffset - stopOffset)) < clearingDistanceM)
                        {
                            if (forceThroughSignal)
                            {
                                HoldSignal = false;
                                EndSignal = -1;
                            }
                            else
                            {
                                stopOffset = endOffset + distanceToEndSignal - clearingDistanceM - 1.0f;
                                // check if train still fits in platform
                                if ((stopOffset - beginOffset) < Length)
                                {
                                    float keepDistanceM = Math.Max((0.5f * clearingDistanceM), (endOffset + distanceToEndSignal) - (beginOffset + Length));
                                    stopOffset = endOffset + distanceToEndSignal - keepDistanceM;
                                }
                            }
                        }
                        else if (closeupSignal && (distanceToEndSignal + (endOffset - stopOffset)) < keepDistanceCloseupSignalM)
                        {
                            if (forceThroughSignal)
                            {
                                HoldSignal = false;
                                EndSignal = -1;
                            }
                            else
                            {
                                stopOffset = endOffset + distanceToEndSignal - keepDistanceCloseupSignalM - 1.0f;
                            }
                        }
                    }
                    // reset hold signal if stop is forced beyond signal
                    else if (forceThroughSignal)
                    {
                        HoldSignal = false;
                        EndSignal = -1;
                    }
                    // if most of train fits in platform then stop at signal
                    else if ((distanceToEndSignal - clearingDistanceM + thisPlatform.Length) > (0.6 * Length))
                    {
                        HoldSignal = true;
                        if (closeupSignal || ExtendPlatformToSignal || restrictPlatformToSignal)
                        {
                            stopOffset = endOffset + distanceToEndSignal - keepDistanceCloseupSignalM - 1.0f;
                        }
                        else
                        {
                            stopOffset = endOffset + distanceToEndSignal - clearingDistanceM - 1.0f;
                        }
                        // set 1m earlier to give priority to station stop over signal
                    }
                    // if platform positions forced always use closeup
                    else if (ExtendPlatformToSignal || restrictPlatformToSignal)
                    {
                        HoldSignal = true;
                        stopOffset = endOffset + distanceToEndSignal - keepDistanceCloseupSignalM - 1.0f;
                        // set 1m earlier to give priority to station stop over signal
                    }
                    // train does not fit in platform - reset exit signal
                    else
                    {
                        EndSignal = -1;
                    }
                }
            }
            else
            // check in reverse direction
            // end of train is beyond signal
            {
                if (thisPlatform.EndSignals[useDirection] >= 0)
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
                    // train does not fit in platform - reset exit signal
                    else
                    {
                        EndSignal = -1;
                    }
                }
            }

            // store details
            TCRouteElement lastElement = thisRoute[lastRouteIndex];
            dummyStop.TCSectionIndex = lastElement.TCSectionIndex;
            dummyStop.Direction = lastElement.Direction;
            dummyStop.ExitSignal = EndSignal;
            dummyStop.HoldSignal = EndSignal >= 0 ? HoldSignal : false;
            dummyStop.StopOffset = stopOffset;
            dummyStop.RouteIndex = lastRouteIndex;

            return (dummyStop);
        }

        /// <summary>
        /// Create new station stop
        /// </summary>
        /// <param name="platformStartID"></param>
        /// <param name="arrivalTime"></param>
        /// <param name="departTime"></param>
        /// <param name="arrivalDT"></param>
        /// <param name="departureDT"></param>
        /// <param name="clearingDistanceM"></param>
        /// <param name="minStopDistanceM"></param>
        /// <param name="terminal"></param>
        /// <param name="actMinStopTime"></param>
        /// <param name="keepClearFront"></param>
        /// <param name="keepClearRear"></param>
        /// <param name="forcePosition"></param>
        /// <param name="endStop"></param>
        /// <returns></returns>
        public bool CreateStationStop(int platformStartID, int arrivalTime, int departTime, DateTime arrivalDT, DateTime departureDT, float clearingDistanceM,
            float minStopDistanceM, bool terminal, int? actMinStopTime, float? keepClearFront, float? keepClearRear, bool forcePosition, bool closeupSignal, 
            bool closeup, bool restrictPlatformToSignal, bool extendPlatformToSignal, bool endStop)
        {
            StationStop thisStation = CalculateStationStop(platformStartID, arrivalTime, departTime, arrivalDT, departureDT, clearingDistanceM,
                minStopDistanceM, terminal, actMinStopTime, keepClearFront, keepClearRear, forcePosition, closeupSignal, closeup,
                restrictPlatformToSignal, extendPlatformToSignal, endStop);

            if (thisStation != null)
            {
                bool HoldSignal = thisStation.HoldSignal;
                int EndSignal = thisStation.ExitSignal;

                StationStops.Add(thisStation);

                // if station has hold signal and this signal is the same as the exit signal for previous station, remove the exit signal from the previous station

                if (HoldSignal && StationStops.Count > 1)
                {
                    if (EndSignal == StationStops[StationStops.Count - 2].ExitSignal && StationStops[StationStops.Count - 2].HoldSignal)
                    {
                        StationStops[StationStops.Count - 2].HoldSignal = false;
                        StationStops[StationStops.Count - 2].ExitSignal = -1;
                        if (HoldingSignals.Contains(EndSignal))
                        {
                            HoldingSignals.Remove(EndSignal);
                        }
                    }
                }

                // add signal to list of hold signals

                if (HoldSignal)
                {
                    HoldingSignals.Add(EndSignal);
                }
                else if (HoldingSignals.Contains(EndSignal))
                {
                    HoldingSignals.Remove(EndSignal);
                }
            }
            else
            {
                return (false);
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

            return (true);
        }

        //================================================================================================//
        /// <summary>
        /// Check initial station
        /// Override from AITrain class
        /// <\summary>

        public override bool CheckInitialStation()
        {
            bool atStation = false;
            if (FormedOf > 0 && AtStation)  // if train was formed and at station, train is in initial station
            {
                return (true);
            }

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

            atStation = CheckStationPosition(thisStation.PlatformItem, thisStation.Direction, thisStation.TCSectionIndex);

            // At station : set state, create action item

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

            return (atStation);
        }

        /// <summary>
        /// Check if train is stopped in station
        /// </summary>
        /// <param name="thisPlatform"></param>
        /// <param name="stationDirection"></param>
        /// <param name="stationTCSectionIndex"></param>
        /// <returns></returns>
        public override bool CheckStationPosition(PlatformDetails thisPlatform, int stationDirection, int stationTCSectionIndex)
        {
            bool atStation = false;
            //            PlatformDetails thisPlatform = thisStation.PlatformItem;

            float platformBeginOffset = thisPlatform.TCOffset[0, stationDirection];
            float platformEndOffset = thisPlatform.TCOffset[1, stationDirection];
            int endSectionIndex = stationDirection == 0 ?
                    thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1] :
                    thisPlatform.TCSectionIndex[0];
            int endSectionRouteIndex = ValidRoute[0].GetRouteIndex(endSectionIndex, 0);

            int beginSectionIndex = stationDirection == 1 ?
                    thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1] :
                    thisPlatform.TCSectionIndex[0];
            int beginSectionRouteIndex = ValidRoute[0].GetRouteIndex(beginSectionIndex, 0);

            // check position

            float margin = 0.0f;
            if (Simulator.PreUpdate)
                margin = 2.0f * clearingDistanceM;  // allow margin in pre-update due to low update rate

            int stationIndex = ValidRoute[0].GetRouteIndex(stationTCSectionIndex, PresentPosition[0].RouteListIndex);

            // if not found from front of train, try from rear of train (front may be beyond platform)
            if (stationIndex < 0)
            {
                stationIndex = ValidRoute[0].GetRouteIndex(stationTCSectionIndex, PresentPosition[1].RouteListIndex);
            }

            // if rear is in platform, station is valid
            if (PresentPosition[1].RouteListIndex == stationIndex && PresentPosition[1].TCOffset > platformEndOffset)
            {
                atStation = true;
            }

            // if front is in platform and most of the train is as well, station is valid
            else if (PresentPosition[0].RouteListIndex == stationIndex &&
                    ((thisPlatform.Length - (platformBeginOffset - PresentPosition[0].TCOffset)) > (Length / 2)))
            {
                atStation = true;
            }

            // if front is beyond platform and rear is not on route or before platform : train spans platform
            else if (PresentPosition[0].RouteListIndex > stationIndex && PresentPosition[1].RouteListIndex < stationIndex)
            {
                atStation = true;
            }

            // if front is beyond platform and rear is in platform section but ahead of position : train spans platform
            else if (PresentPosition[0].RouteListIndex > stationIndex && PresentPosition[1].RouteListIndex == stationIndex && PresentPosition[1].TCOffset < platformEndOffset)
            {
                atStation = true;
            }
           
            return (atStation);
        }

        //================================================================================================//
        /// <summary>
        /// Check for next station
        /// Override from AITrain
        /// <\summary>

        public override void SetNextStationAction(bool fromAutopilotSwitch = false)
        {
            // do not set action if stopped in station
            if (MovementState == AI_MOVEMENT_STATE.STATION_STOP || AtStation)
            {
                return;
            }

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

            // get distance to station, but not if just after switch to Autopilot and not during station stop
            bool validStop = false;
            while (!validStop)
            {
                float[] distancesM = CalculateDistancesToNextStation(thisStation, TrainMaxSpeedMpS, false);
                if (distancesM[0] < 0f && !(MovementState == AI_MOVEMENT_STATE.STATION_STOP && distancesM[0] != -1)) // stop is not valid
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
                    AIActionItem newAction = new AIActionItem(null, AIActionItem.AI_ACTION_TYPE.STATION_STOP);
                    newAction.SetParam(distancesM[1], 0.0f, distancesM[0], DistanceTravelledM);
                    requiredActions.InsertAction(newAction);

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

        /// <summary>
        /// Recalculate station stop
        /// Main method, check if train presently in station
        /// Called e.g. after detach or attach etc. because lenght of train has changed so stop positions must be recalculated
        /// </summary>
        public void RecalculateStationStops()
        {
            bool isAtStation = AtStation || MovementState == AI_MOVEMENT_STATE.STATION_STOP;
            RecalculateStationStops(isAtStation);
        }

        /// <summary>
        /// Recalculate station stop
        /// Actual calculation
        /// </summary>
        /// <param name="atStation"></param>
        public void RecalculateStationStops(bool atStation)
        {
            int firstStopIndex = atStation ? 1 : 0;

            for (int iStation = firstStopIndex; iStation < StationStops.Count; iStation++)
            {
                StationStop actualStation = StationStops[iStation];
                Train.TCSubpathRoute thisRoute = TCRoute.TCRouteSubpaths[actualStation.SubrouteIndex];
                TCRouteElement thisElement = thisRoute[actualStation.RouteIndex];
                PlatformDetails thisPlatform = actualStation.PlatformItem;

                StationStop newStop = CalculateStationStopPosition(TCRoute.TCRouteSubpaths[actualStation.SubrouteIndex], actualStation.RouteIndex, actualStation.PlatformItem,
                    actualStation.SubrouteIndex, actualStation.KeepClearFront, actualStation.KeepClearRear, actualStation.ForcePosition, 
                    actualStation.CloseupSignal, actualStation.Closeup, actualStation.RestrictPlatformToSignal, actualStation.ExtendPlatformToSignal,
                    actualStation.Terminal, actualStation.PlatformReference);

                actualStation.RouteIndex = newStop.RouteIndex;
                actualStation.TCSectionIndex = newStop.TCSectionIndex;
                actualStation.StopOffset = newStop.StopOffset;
                actualStation.HoldSignal = newStop.HoldSignal;
                actualStation.ExitSignal = newStop.ExitSignal;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Start train out of AI train due to 'formed' action
        /// </summary>
        /// <param name="otherTrain"></param>
        /// <returns></returns>

        public bool StartFromAITrain(TTTrain otherTrain, int presentTime, TrackCircuitSection[] occupiedTrack)
        {
            // check if new train has route at present position of front of train
            int usedRefPosition = 0;
            int startPositionIndex = TCRoute.TCRouteSubpaths[0].GetRouteIndex(otherTrain.PresentPosition[0].TCSectionIndex, 0);
            int usedPositionIndex = startPositionIndex;

            int rearPositionIndex = TCRoute.TCRouteSubpaths[0].GetRouteIndex(otherTrain.PresentPosition[1].TCSectionIndex, 0);

            if (startPositionIndex < 0)
            {
                usedRefPosition = 1;
                usedPositionIndex = rearPositionIndex;
            }

            // if not found - train cannot start out of other train as there is no valid route - let train start of its own
            if (startPositionIndex < 0 && rearPositionIndex < 0)
            {
                FormedOf = -1;
                FormedOfType = FormCommand.None;
                return (false);
            }

            OccupiedTrack.Clear();
            foreach (TrackCircuitSection thisSection in occupiedTrack)
            {
                OccupiedTrack.Add(thisSection);
            }

            int addedSections = AdjustTrainRouteOnStart(startPositionIndex, rearPositionIndex, otherTrain);
            usedPositionIndex += addedSections;

            // copy consist information incl. max speed and type

            if (FormedOfType == FormCommand.TerminationFormed)
            {
                Cars.Clear();
                int carId = 0;
                foreach (TrainCar car in otherTrain.Cars)
                {
                    Cars.Add(car);
                    car.Train = this;
                    car.CarID = String.Concat(Number.ToString("0###"), "_", carId.ToString("0##"));
                    carId++;
                }
                IsFreight = otherTrain.IsFreight;
                Length = otherTrain.Length;
                MassKg = otherTrain.MassKg;
                LeadLocomotiveIndex = otherTrain.LeadLocomotiveIndex;

                // copy other train speed if not restricted for either train
                if (!otherTrain.SpeedSettings.restrictedSet && !SpeedSettings.restrictedSet)
                {
                    TrainMaxSpeedMpS = otherTrain.TrainMaxSpeedMpS;
                }
                AllowedMaxSpeedMpS = otherTrain.AllowedMaxSpeedMpS;
                allowedMaxSpeedSignalMpS = otherTrain.allowedMaxSpeedSignalMpS;
                allowedMaxSpeedLimitMpS = otherTrain.allowedMaxSpeedLimitMpS;

                FrontTDBTraveller = new Traveller(otherTrain.FrontTDBTraveller);
                RearTDBTraveller = new Traveller(otherTrain.RearTDBTraveller);

                // check if train reversal is required

                if (TCRoute.TCRouteSubpaths[0][usedPositionIndex].Direction != otherTrain.PresentPosition[usedRefPosition].TCDirection)
                {
                    ReverseFormation(false);

                    // if reversal is required and units must be detached at start : reverse detached units position
                    if (DetachDetails.ContainsKey(-1))
                    {
                        List<DetachInfo> detachList = DetachDetails[-1];

                        for (int iDetach = detachList.Count - 1; iDetach >= 0; iDetach--)
                        {
                            DetachInfo thisDetach = detachList[iDetach];
                            if (thisDetach.DetachPosition == DetachInfo.DetachPositionInfo.atStart)
                            {
                                switch (thisDetach.DetachUnits)
                                {
                                    case DetachInfo.DetachUnitsInfo.allLeadingPower:
                                        thisDetach.DetachUnits = DetachInfo.DetachUnitsInfo.allTrailingPower;
                                        break;

                                    case DetachInfo.DetachUnitsInfo.allTrailingPower:
                                        thisDetach.DetachUnits = DetachInfo.DetachUnitsInfo.allLeadingPower;
                                        break;

                                    case DetachInfo.DetachUnitsInfo.unitsAtEnd:
                                        thisDetach.DetachUnits = DetachInfo.DetachUnitsInfo.unitsAtFront;
                                        break;

                                    case DetachInfo.DetachUnitsInfo.unitsAtFront:
                                        thisDetach.DetachUnits = DetachInfo.DetachUnitsInfo.unitsAtEnd;
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }

                InitialTrainPlacement(false);

            }
            else if (FormedOfType == FormCommand.TerminationTriggered)
            {
                if (TCRoute.TCRouteSubpaths[0][usedPositionIndex].Direction != otherTrain.PresentPosition[usedRefPosition].TCDirection)
                {
                    FrontTDBTraveller = new Traveller(otherTrain.RearTDBTraveller, Traveller.TravellerDirection.Backward);
                    RearTDBTraveller = new Traveller(otherTrain.FrontTDBTraveller, Traveller.TravellerDirection.Backward);
                }
                else
                {
                    FrontTDBTraveller = new Traveller(otherTrain.FrontTDBTraveller);
                    RearTDBTraveller = new Traveller(otherTrain.RearTDBTraveller);
                }
                CalculatePositionOfCars();
                InitialTrainPlacement(true);
            }

            // set state
            MovementState = AI_MOVEMENT_STATE.AI_STATIC;
            ControlMode = TRAIN_CONTROL.INACTIVE;

            int eightHundredHours = 8 * 3600;
            int sixteenHundredHours = 16 * 3600;

            // if no activate time, set to now + 30
            if (!ActivateTime.HasValue)
            {
                ActivateTime = presentTime + 30;
            }
            // if activate time < 08:00 and present time > 16:00, assume activate time is after midnight
            else if (ActivateTime.Value < eightHundredHours && presentTime > sixteenHundredHours)
            {
                ActivateTime = ActivateTime.Value + (24 * 3600);
            }

            return (true);
        }

        //================================================================================================//
        /// <summary>
        /// Update for pre-update state
        /// Override from AITrain class
        /// <\summary>

        public override void AIPreUpdate(float elapsedClockSeconds)
        {
            // calculate delta speed and speed
            float distanceM = physicsPreUpdate(elapsedClockSeconds);

            // force stop - no forced stop if mode is following and attach is true
            if (distanceM > NextStopDistanceM)
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
                        //FormatStrings.FormatDistance(distanceM, true) + " set to " +
                        //"0.0 > " + FormatStrings.FormatDistance(NextStopDistanceM, true) + " at " +
                        //FormatStrings.FormatDistance(DistanceTravelledM, true) + "\n");
                        distanceM.ToString() + " set to " +
                        "0.0 > " + NextStopDistanceM.ToString() + " at " +
                        DistanceTravelledM.ToString() + "\n");
                }

                distanceM = Math.Max(0.0f, NextStopDistanceM);
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

            CalculatePositionOfCars(elapsedClockSeconds, distanceM);

            DistanceTravelledM += distanceM;

            // perform overall update

            if (ControlMode == TRAIN_CONTROL.TURNTABLE)
            {
                UpdateTurntable(elapsedClockSeconds);
            }
            else if (ValidRoute != null && MovementState != AI_MOVEMENT_STATE.AI_STATIC)        // no actions required for static objects //
            {
                movedBackward = CheckBackwardClearance();                                       // check clearance at rear //
                UpdateTrainPosition();                                                          // position update         //              
                UpdateTrainPositionInformation();                                               // position linked info    //
                int SignalObjIndex = CheckSignalPassed(0, PresentPosition[0], PreviousPosition[0]);    // check if passed signal  //
                UpdateSectionState(movedBackward);                                              // update track occupation //
                ObtainRequiredActions(movedBackward);                                           // process Actions         //
                UpdateRouteClearanceAhead(SignalObjIndex, movedBackward, elapsedClockSeconds);  // update route clearance  //
                UpdateSignalState(movedBackward);                                               // update signal state     //

                UpdateMinimalDelay();

                // if train ahead and approaching turntable, check if train is beyond turntable
                if (ValidRoute[0].Last().MovingTableApproachPath > -1 && EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
                {
                    CheckTrainBeyondTurntable();
                }

            }
        }

        //================================================================================================//
        /// <summary>
        /// Update train physics during Pre-Update
        /// <\summary>

        public float physicsPreUpdate(float elapsedClockSeconds)
        {

            // Update train physics, position and movement
            // Simplified calculation for use in pre-update phase

            PropagateBrakePressure(elapsedClockSeconds);

            float massKg = 0f;
            foreach (TrainCar car in Cars)
            {
                car.MotiveForceN = 0;
                car.Update(elapsedClockSeconds);
                car.TotalForceN = car.MotiveForceN + car.GravityForceN - car.CurveForceN;
                massKg += car.MassKG;

                if (car.Flipped)
                {
                    car.TotalForceN = -car.TotalForceN;
                    car.SpeedMpS = -car.SpeedMpS;
                }
            }
            MassKg = massKg;

            UpdateCarSpeeds(elapsedClockSeconds);

            float distanceM = LastCar.SpeedMpS * elapsedClockSeconds;
            if (Math.Abs(distanceM) < 0.1f) distanceM = 0.0f; //clamp to avoid movement due to calculation noise
            if (float.IsNaN(distanceM)) distanceM = 0;        //avoid NaN, if so will not move

            if (TrainType == TRAINTYPE.AI && LeadLocomotiveIndex == (Cars.Count - 1) && LastCar.Flipped)
                distanceM = -distanceM;

            return (distanceM);
        }

        //================================================================================================//
        /// <summary>
        /// Update train 
        /// </summary>

        public override void Update(float elapsedClockSeconds, bool auxiliaryUpdate = true)
        {
            // Update train physics, position and movement

#if DEBUG_CHECKTRAIN
            if (!CheckTrain)
            {
                if (Number == 160)
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
            physicsUpdate(elapsedClockSeconds);

            // Update the UiD of First Wagon
            FirstCarUiD = GetFirstWagonUiD();

            // Check to see if wagons are attached to train
            WagonsAttached = GetWagonsAttachedIndication();

            //Exit here when train is static consist (no further actions required)

            if (GetAIMovementState() == AITrain.AI_MOVEMENT_STATE.AI_STATIC)
            {
                int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
                UpdateAIStaticState(presentTime);
            }

            if (TrainType == TRAINTYPE.STATIC)
                return;

            // perform overall update

            if (ControlMode == TRAIN_CONTROL.MANUAL)                                        // manual mode
            {
                UpdateManual(elapsedClockSeconds);
            }

            else if (TrainType == TRAINTYPE.PLAYER && ControlMode == TRAIN_CONTROL.TURNTABLE) // turntable mode
            {
                string infoString = String.Copy("Do NOT move the train");

                if (LeadLocomotive.ThrottlePercent > 1)
                {
                    infoString = String.Concat(infoString, " ; set throttle to 0");
                }
                if (LeadLocomotive.Direction != Direction.N || Math.Abs(MUReverserPercent) > 1)
                {
                    infoString = String.Concat(infoString, " ; set reverser to neutral (or 0)");
                }
                Simulator.Confirmer.Warning(infoString);

                ActiveTurntable.UpdateTurntableStatePlayer(elapsedClockSeconds);            // update turntable state
            }
            else if (ValidRoute[0] != null && GetAIMovementState() != AITrain.AI_MOVEMENT_STATE.AI_STATIC)     // no actions required for static objects //
            {
                if (ControlMode != TRAIN_CONTROL.OUT_OF_CONTROL) movedBackward = CheckBackwardClearance();  // check clearance at rear if not out of control //
                UpdateTrainPosition();                                                          // position update         //
                UpdateTrainPositionInformation();                                               // position update         //
                int SignalObjIndex = CheckSignalPassed(0, PresentPosition[0], PreviousPosition[0]);   // check if passed signal  //
                UpdateSectionState(movedBackward);                                              // update track occupation //
                ObtainRequiredActions(movedBackward);                                           // process list of actions //

                bool stillExist = true;

                if (TrainType == TRAINTYPE.PLAYER)                                              // player train is to check own stations
                {
                    if (MovementState == AI_MOVEMENT_STATE.TURNTABLE)
                    {
                        ActiveTurntable.UpdateTurntableStatePlayer(elapsedClockSeconds);
                    }
                    else
                    {
                        CheckStationTask();
                        CheckPlayerAttachState();                                               // check for player attach

                        if (ControlMode != TRAIN_CONTROL.OUT_OF_CONTROL)
                        {
                            stillExist = CheckRouteActions(elapsedClockSeconds);                 // check routepath (AI check at other point) //
                        }
                    }
                }
                if (stillExist && ControlMode != TRAIN_CONTROL.OUT_OF_CONTROL)
                {
                    UpdateRouteClearanceAhead(SignalObjIndex, movedBackward, elapsedClockSeconds);  // update route clearance  //
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "MovementState : " + MovementState.ToString() + " ; End Authority : " + EndAuthorityType[0].ToString() + "\n");
                    }

                    if (MovementState != AI_MOVEMENT_STATE.TURNTABLE)
                    {
                        UpdateSignalState(movedBackward);                                           // update signal state but not when on turntable
                    }

                    // if train ahead and approaching turntable, check if train is beyond turntable
                    if (ValidRoute[0].Last().MovingTableApproachPath > -1 && EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
                    {
                        CheckTrainBeyondTurntable();
                    }
                }
            }

            // calculate minimal delay (Timetable only)
            UpdateMinimalDelay();

            // check position of train wrt tunnels
            ProcessTunnels();

            // log train details

            if (DatalogTrainSpeed)
            {
                LogTrainSpeed(Simulator.ClockTime);
            }

        } // end Update

        //================================================================================================//
        /// <summary>
        /// If approaching turntable and there is a train ahead, check if train is beyond turntable
        /// </summary>

        public void CheckTrainBeyondTurntable()
        {
            TCRouteElement lastElement = ValidRoute[0].Last();
            if (lastElement.MovingTableApproachPath > -1 && AI.Simulator.PoolHolder.Pools.ContainsKey(ExitPool))
            {
                TimetablePool thisPool = AI.Simulator.PoolHolder.Pools[ExitPool];
                float lengthToGoM = thisPool.GetEndOfRouteDistance(TCRoute.TCRouteSubpaths.Last(), PresentPosition[0], lastElement.MovingTableApproachPath, signalRef);

                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "State check if train ahead is beyond turntable\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "    Train Ahead : " + DistanceToEndNodeAuthorityM[0] + " ; Distance to turntable : " + lengthToGoM + "\n");
                }

                if (lengthToGoM < DistanceToEndNodeAuthorityM[0])
                {
                    EndAuthorityType[0] = END_AUTHORITY.END_OF_PATH;
                    DistanceToEndNodeAuthorityM[0] = NextStopDistanceM = lengthToGoM + clearingDistanceM; // add clearing distance to avoid position lock short of turntable
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Calculate running delay if present time is later than next station arrival
        /// Override from Train class
        /// </summary>

        public void UpdateMinimalDelay()
        {
            int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));

            if (TrainType == TRAINTYPE.AI)
            {
                AITrain thisAI = this as AITrain;
                presentTime = Convert.ToInt32(Math.Floor(thisAI.AI.clockTime));
            }

            if (StationStops != null && StationStops.Count > 0 && !AtStation)
            {
                if (presentTime > StationStops[0].ArrivalTime)
                {
                    TimeSpan tempDelay = TimeSpan.FromSeconds((presentTime - StationStops[0].ArrivalTime) % (24 * 3600));
                    //skip when delay exceeds 12 hours - that's due to passing midnight
                    if (tempDelay.TotalSeconds < (12 * 3600) && (!Delay.HasValue || tempDelay > Delay.Value))
                    {
                        Delay = tempDelay;
                    }
                }
            }

            // update max speed if separate cruise speed is set
            if (SpeedSettings.cruiseSpeedMpS.HasValue)
            {
                if (SpeedSettings.cruiseMaxDelayS.HasValue && Delay.HasValue && Delay.Value.TotalSeconds > SpeedSettings.cruiseMaxDelayS.Value)
                {
                    TrainMaxSpeedMpS = SpeedSettings.maxSpeedMpS.Value;
                }
                else
                {
                    TrainMaxSpeedMpS = SpeedSettings.cruiseSpeedMpS.Value;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// TestAbsDelay
        /// Dummy to allow function for parent classes (Train class) to be called in common methods
        /// </summary>
        /// 
        public override void TestAbsDelay(ref int delay, int correctedTime)
        {
        }

        //================================================================================================//
        /// <summary>
        /// Update AI Static state
        /// Override from Train class
        /// </summary>
        /// <param name="presentTime"></param>

        public override void UpdateAIStaticState(int presentTime)
        {
#if DEBUG_CHECKTRAIN
            if (!CheckTrain)
            {
                if (Number == 595)
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

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Train update AI state : " + Number + " ; type : " + TrainType + "\n");
            }

            // start if start time is reached
            bool reqActivate = false;
            if (ActivateTime.HasValue && !TriggeredActivationRequired)
            {
                if (ActivateTime.Value < (presentTime % (24 * 3600))) reqActivate = true;
                if (ActivateTime > (24 * 3600) && ActivateTime < presentTime) reqActivate = true;
            }

            bool maystart = true;
            if (TriggeredActivationRequired)
            {
                maystart = false;
            }

            // check if anything needs to attach or transfer
            if (reqActivate)
            {
                if (NeedAttach != null && NeedAttach.ContainsKey(-1))
                {
                    List<int> needAttachList = NeedAttach[-1];
                    if (needAttachList.Count > 0)
                    {
                        reqActivate = false;
                        maystart = false;
                    }
                }

                foreach (TrackCircuitSection occSection in OccupiedTrack)
                {
                    if (NeedTrainTransfer.ContainsKey(occSection.Index))
                    {
                        reqActivate = false;
                        maystart = false;
                        break;
                    }
                }
            }

            // check if anything needs be detached
            if (DetachDetails.ContainsKey(-1))
            {
                List<DetachInfo> detachList = DetachDetails[-1];

                for (int iDetach = detachList.Count - 1; iDetach >= 0; iDetach--)
                {
                    DetachInfo thisDetach = detachList[iDetach];
                    if (thisDetach.Valid)
                    {

                        bool validTime = !thisDetach.DetachTime.HasValue || thisDetach.DetachTime.Value < presentTime;
                        if (thisDetach.DetachPosition == DetachInfo.DetachPositionInfo.atStart && validTime)
                        {
                            DetachActive[0] = -1;
                            DetachActive[1] = iDetach;
                            thisDetach.PerformDetach(this, true);
                            thisDetach.Valid = false;
                        }

                        if (reqActivate && thisDetach.DetachPosition == DetachInfo.DetachPositionInfo.atActivation)
                        {
                            DetachActive[0] = -1;
                            DetachActive[1] = iDetach;
                            thisDetach.PerformDetach(this, true);
                            thisDetach.Valid = false;
                        }
                    }
                }

                if (detachList.Count <= 0) DetachDetails.Remove(-1);
            }

            // check if other train must be activated
            if (reqActivate)
            {
                ActivateTriggeredTrain(TriggerActivationType.Start, -1);
            }

            // switch power
            if (reqActivate && TrainHasPower())
            {
                if (CheckTrain)
                {
                    DateTime baseDT = new DateTime();
                    DateTime actTime = baseDT.AddSeconds(AI.clockTime);

                    File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " activated \n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", actTime.ToString("HH:mm:ss") + "\n");
                    File.AppendAllText(@"C:\temp\checktrain.txt", "--------\n");
                }

                foreach (var car in Cars)
                {
                    if (car is MSTSLocomotive)
                    {
                        MSTSLocomotive loco = car as MSTSLocomotive;
                        loco.SetPower(true);
                    }
                }
                PowerState = true;

                if (TrainType == TRAINTYPE.INTENDED_PLAYER)
                {
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train promoted to PLAYER : " + Number + "\n");
                    }

                    TrainType = TRAINTYPE.PLAYER;
                }

                PostInit(true);

                if (TrainType == TRAINTYPE.PLAYER)
                {
                    SetupStationStopHandling();
                }

                return;
            }

            // switch off power for all engines until 20 secs before start

            if (ActivateTime.HasValue && TrainHasPower() && maystart)
            {
                if (PowerState && ActivateTime.Value < (presentTime - 20))
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
                else if (!PowerState) // switch power on 20 secs before start
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
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Set reversal point action
        /// Override from AITrain class
        /// <\summary>

        public override void SetReversalAction()
        {
            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
            {
                return; // station stop required - reversal not valid
            }
            
            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.REVERSAL)
            {
                return; // other reversal still active - reversal not valid
            }

            if (StationStops != null && StationStops.Count > 0 && StationStops[0].SubrouteIndex == TCRoute.activeSubpath)
            {
                return; // station stop required in this subpath - reversal not valid
            }

            if (TCRoute.ReversalInfo[TCRoute.activeSubpath].Valid)
            {
                int reqSection = (TCRoute.ReversalInfo[TCRoute.activeSubpath].SignalUsed && !ForceReversal) ?
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].LastSignalIndex :
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].LastDivergeIndex;

                if (reqSection >= 0 && PresentPosition[1].RouteListIndex >= reqSection && TCRoute.ReversalInfo[TCRoute.activeSubpath].ReversalActionInserted == false)
                {
                    if (!NeedPickUp && !CheckTransferRequired())
                    {
                        float reqDistance = (SpeedMpS * SpeedMpS * MaxDecelMpSS) + DistanceTravelledM;
                        reqDistance = nextActionInfo != null ? Math.Min(nextActionInfo.RequiredDistance, reqDistance) : reqDistance;

                        nextActionInfo = new AIActionItem(null, AIActionItem.AI_ACTION_TYPE.REVERSAL);
                        nextActionInfo.SetParam((PresentPosition[0].DistanceTravelledM - 1), 0.0f, reqDistance, PresentPosition[0].DistanceTravelledM);
                        MovementState = MovementState != AI_MOVEMENT_STATE.STOPPED ? AI_MOVEMENT_STATE.BRAKING : AI_MOVEMENT_STATE.STOPPED;
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// change in authority state - check action
        /// Override from AITrain class
        /// <\summary>

        public override void CheckRequiredAction()
        {
            // check if train ahead
            if (EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
            {
                if (MovementState != AI_MOVEMENT_STATE.STATION_STOP && MovementState != AI_MOVEMENT_STATE.STOPPED)
                {
                    MovementState = AI_MOVEMENT_STATE.FOLLOWING;  // start following
                    CheckReadyToAttach();                         // check for attach
                }
            }
            else if (EndAuthorityType[0] == END_AUTHORITY.RESERVED_SWITCH || EndAuthorityType[0] == END_AUTHORITY.LOOP || EndAuthorityType[0] == END_AUTHORITY.NO_PATH_RESERVED)
            {
                ResetActions(true);
                NextStopDistanceM = DistanceToEndNodeAuthorityM[0] - 2.0f * junctionOverlapM;
                CreateTrainAction(SpeedMpS, 0.0f, NextStopDistanceM, null,
                           AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY);
            }
            // first handle outstanding actions
            else if (EndAuthorityType[0] == END_AUTHORITY.END_OF_PATH &&
                (nextActionInfo == null || nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE))
            {
                ResetActions(false);
                NextStopDistanceM = DistanceToEndNodeAuthorityM[0] - clearingDistanceM;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update train in stopped state
        /// Override from AITrain class
        /// <\summary>

        public override AITrain.AI_MOVEMENT_STATE UpdateStoppedState(float elapsedClockSeconds)
        {
            // check if restart is delayed
            if (DelayedStart && Simulator.Settings.TTUseRestartDelays)
            {
                RestdelayS -= elapsedClockSeconds;
                if (RestdelayS <= 0)   // wait time has elapsed - start moving
                {
                    DelayedStart = false;
                    RestdelayS = 0;
                    StartMoving(DelayedStartState);
                }

                if (CheckTrain && DelayedStart)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " delayed start : " + RestdelayS.ToString() + "\n");
                }
                else if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " : start moving \n");
                }

                return (MovementState);
            }

            else if (RestdelayS > 0)
            {
                RestdelayS -= elapsedClockSeconds; // decrease pre-restart wait time while stopped
            }

            if (SpeedMpS > 0 || SpeedMpS < 0)   // if train still running force it to stop
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
                Dictionary<Train, float> trainInfo = thisSection.TestTrainAhead(this, PresentPosition[0].TCOffset, PresentPosition[0].TCDirection);

                float addOffset = 0;
                if (trainInfo.Count <= 0)
                {
                    addOffset = thisSection.Length - PresentPosition[0].TCOffset;
                }

                // train not in this section, try reserved sections ahead
                for (int iIndex = startIndex + 1; iIndex <= endIndex && trainInfo.Count <= 0; iIndex++)
                {
                    TrackCircuitSection nextSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                    trainInfo = nextSection.TestTrainAhead(this, 0, ValidRoute[0][iIndex].Direction);
                    if (trainInfo.Count <= 0)
                    {
                        addOffset += nextSection.Length;
                    }
                }

                // if train not ahead, try first section beyond last reserved
                if (trainInfo.Count <= 0 && endIndex < ValidRoute[0].Count - 1)
                {
                    TrackCircuitSection nextSection = signalRef.TrackCircuitList[ValidRoute[0][endIndex + 1].TCSectionIndex];
                    trainInfo = nextSection.TestTrainAhead(this, 0, ValidRoute[0][endIndex + 1].Direction);
                }

                // if train found get distance
                if (trainInfo.Count > 0)  // found train
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo) // always just one
                    {
                        TTTrain OtherTrain = trainAhead.Key as TTTrain;
                        float distanceToTrain = trainAhead.Value + addOffset;

                        if (EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
                        {
                            DistanceToEndNodeAuthorityM[0] = distanceToTrain;
                        }

                        if (Math.Abs(OtherTrain.SpeedMpS) < 0.1f &&
                                    distanceToTrain > followDistanceStatTrainM)
                        {
                            // allow creeping closer
                            CreateTrainAction(SpeedSettings.creepSpeedMpS.Value, 0.0f,
                                    distanceToTrain, null, AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD);
                            DelayedStartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);
                        }

                        else if (Math.Abs(OtherTrain.SpeedMpS) > 0.1f &&
                            distanceToTrain > keepDistanceMovingTrainM)
                        {
                            // train started moving
                            DelayedStartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);
                        }
                        else
                        {
                            bool attachToTrain = false;
                            bool pickUpTrain = false;

                            bool transferTrain = false;
                            int? transferStationIndex = null;
                            int? transferTrainIndex = null;

                            CheckReadyToAttach();

                            // check attach details
                            if (AttachDetails != null && AttachDetails.Valid && AttachDetails.ReadyToAttach && AttachDetails.AttachTrain == OtherTrain.OrgAINumber)
                            {
                                attachToTrain = true;
                            }

                            if (!attachToTrain)
                            {
                                // check pickup details
                                pickUpTrain = CheckPickUp(OtherTrain);

                                // check transfer details
                                transferTrain = CheckTransfer(OtherTrain, ref transferStationIndex, ref transferTrainIndex);
                            }

                            // if to attach to train, start moving
                            if (attachToTrain || pickUpTrain || transferTrain)
                            {
                                DelayedStartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);
                            }

                            // if other train in station, check if this train to terminate in station - if so, set state as in station
                            else if (OtherTrain.AtStation && StationStops != null && StationStops.Count == 1 && OtherTrain.StationStops[0].PlatformReference == StationStops[0].PlatformReference)
                            {
                                MovementState = AI_MOVEMENT_STATE.STATION_STOP;

                                if (CheckTrain)
                                {
                                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " assumed to be in station : " +
                                        StationStops[0].PlatformItem.Name + " as stopped behind other train in station (other train : " + OtherTrain.Name + "[" + OtherTrain.Number + "] \n");
                                }
                            }
                        }
                    }

                    // update action info
                    if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD)
                    {
                        nextActionInfo.ActivateDistanceM = DistanceTravelledM + DistanceToEndNodeAuthorityM[0];
                    }

                    // if no action, check for station stop (may not be activated due to distance travelled)
                    else if (nextActionInfo == null && StationStops != null && StationStops.Count > 0)
                    {
                        if (StationStops[0].SubrouteIndex == TCRoute.activeSubpath &&
                           ValidRoute[0].GetRouteIndex(StationStops[0].TCSectionIndex, PresentPosition[0].RouteListIndex) <= PresentPosition[0].RouteListIndex)
                        // assume to be in station
                        {
                            MovementState = AI_MOVEMENT_STATE.STATION_STOP;

                            if (CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " assumed to be in station : " +
                                    StationStops[0].PlatformItem.Name + "( present section = " + PresentPosition[0].TCSectionIndex +
                                    " ; station section = " + StationStops[0].TCSectionIndex + " )\n");
                            }
                        }
                    }
                }
                else
                {
                    // if next action still is train ahead, reset actions
                    if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD)
                    {
                        ResetActions(true);
                    }
                }
                // if train not found, do nothing - state will change next update

            }

     // Other node mode : check distance ahead (path may have cleared)

            else if (ControlMode == TRAIN_CONTROL.AUTO_NODE)
            {
                if (EndAuthorityType[0] == END_AUTHORITY.RESERVED_SWITCH || EndAuthorityType[0] == END_AUTHORITY.LOOP)
                {
                    float ReqStopDistanceM = DistanceToEndNodeAuthorityM[0] - 2.0f * junctionOverlapM;
                    if (ReqStopDistanceM > clearingDistanceM)
                    {
                        NextStopDistanceM = DistanceToEndNodeAuthorityM[0];
                        DelayedStartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
                    }
                }
                else if (DistanceToEndNodeAuthorityM[0] > clearingDistanceM)
                {
                    NextStopDistanceM = DistanceToEndNodeAuthorityM[0];
                    DelayedStartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
                }
            }

    // signal node : check state of signal

            else if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
            {
                MstsSignalAspect nextAspect = MstsSignalAspect.UNKNOWN;
                // there is a next item and it is the next signal
                if (nextActionInfo != null && nextActionInfo.ActiveItem != null &&
                    nextActionInfo.ActiveItem.ObjectDetails == NextSignalObject[0])
                {
                    nextAspect = nextActionInfo.ActiveItem.ObjectDetails.this_sig_lr(MstsSignalFunction.NORMAL);
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

                else if (nextAspect > MstsSignalAspect.STOP &&
                        nextAspect < MstsSignalAspect.APPROACH_1)
                {
                    // check if any other signals within clearing distance
                    bool signalCleared = true;
                    bool withinDistance = true;

                    for (int iitem = 0; iitem <= SignalObjectItems.Count - 1 && withinDistance && signalCleared; iitem++)
                    {
                        ObjectItemInfo nextObject = SignalObjectItems[iitem];
                        if (nextObject.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
                        {
                            if (nextObject.ObjectDetails != NextSignalObject[0]) // not signal we are waiting for
                            {
                                if (nextObject.distance_to_train > 2.0 * clearingDistanceM)
                                {
                                    withinDistance = false;  // signal is far enough ahead
                                }
                                else if (nextObject.signal_state == MstsSignalAspect.STOP)
                                {
                                    signalCleared = false;   // signal is not clear
                                    NextSignalObject[0].ForcePropagation = true;
                                }
                            }
                        }
                    }

                    if (signalCleared)
                    {
                        ResetActions(true);
                        NextStopDistanceM = 5000f; // clear to 5000m, will be updated if required
                        DelayedStartMoving(AI_START_MOVEMENT.SIGNAL_RESTRICTED);
                    }
                }
                else if (nextAspect >= MstsSignalAspect.APPROACH_1)
                {
                    // check if any other signals within clearing distance
                    bool signalCleared = true;
                    bool withinDistance = true;

                    for (int iitem = 0; iitem <= SignalObjectItems.Count - 1 && withinDistance && signalCleared; iitem++)
                    {
                        ObjectItemInfo nextObject = SignalObjectItems[iitem];
                        if (nextObject.ObjectType == ObjectItemInfo.ObjectItemType.Signal)
                        {
                            if (nextObject.ObjectDetails != NextSignalObject[0]) // not signal we are waiting for
                            {
                                if (nextObject.distance_to_train > 2.0 * clearingDistanceM)
                                {
                                    withinDistance = false;  // signal is far enough ahead
                                }
                                else if (nextObject.signal_state == MstsSignalAspect.STOP)
                                {
                                    // set this signal as passed, and next signal as waiting
                                    signalCleared = false;   // signal is not clear
                                    int nextSignalIndex = NextSignalObject[0].sigfound[SignalFunction.NORMAL];
                                    if (nextSignalIndex >= 0)
                                    {
                                        NextSignalObject[0] = signalRef.SignalObjects[nextSignalIndex];

                                        int reqSectionIndex = NextSignalObject[0].TCReference;
                                        float endOffset = NextSignalObject[0].TCOffset;

                                        DistanceToSignal = GetDistanceToTrain(reqSectionIndex, endOffset);
                                        SignalObjectItems.RemoveAt(0);
                                    }
                                }
                            }
                        }
                    }

                    if (signalCleared)
                    {
                        ResetActions(true);
                        NextStopDistanceM = 5000f; // clear to 5000m, will be updated if required
                        DelayedStartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
                    }
                }

                else if (nextAspect == MstsSignalAspect.STOP)
                {
                    // if stop but train is well away from signal allow to close
                    if (DistanceToSignal.HasValue && DistanceToSignal.Value > 5 * signalApproachDistanceM)
                    {
                        ResetActions(true);
                        DelayedStartMoving(AI_START_MOVEMENT.PATH_ACTION);
                    }
                }
                else if (nextActionInfo != null &&
                 nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                {
                    if (StationStops[0].SubrouteIndex == TCRoute.activeSubpath &&
                       ValidRoute[0].GetRouteIndex(StationStops[0].TCSectionIndex, PresentPosition[0].RouteListIndex) <= PresentPosition[0].RouteListIndex)
                    // assume to be in station
                    {
                        MovementState = AI_MOVEMENT_STATE.STATION_STOP;

                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " assumed to be in station : " +
                                StationStops[0].PlatformItem.Name + "( present section = " + PresentPosition[0].TCSectionIndex +
                                " ; station section = " + StationStops[0].TCSectionIndex + " )\n");
                        }
                    }
                    else
                    // approaching next station
                    {
                        MovementState = AI_MOVEMENT_STATE.BRAKING;

                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " departing from station stop to next stop : " +
                                StationStops[0].PlatformItem.Name + "( next section = " + PresentPosition[0].TCSectionIndex +
                                " ; station section = " + StationStops[0].TCSectionIndex + " )\n");
                        }
                    }
                }
                else if (nextActionInfo == null || nextActionInfo.NextAction != AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                {
                    if (nextAspect != MstsSignalAspect.STOP)
                    {
                        DelayedStartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
                    }

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                                Number.ToString() + " , forced to BRAKING from invalid stop (now at " +
                                FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
#endif

                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                Number.ToString() + " , forced to BRAKING from invalid stop (now at " +
                            //                                FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
                                PresentPosition[0].DistanceTravelledM.ToString() + ")\n");
                    }
                }
            }

            return (MovementState);
        }

        //================================================================================================//
        /// <summary>
        /// Update when train on turntable
        /// </summary>

        public override void UpdateTurntableState(float elapsedClockSeconds, int presentTime)
        {

            // check if delayed action is due
            if (DelayedStart)
            {
                RestdelayS -= elapsedClockSeconds;
                if (RestdelayS <= 0)   // wait time has elapsed - start moving
                {
                    DelayedStart = false;
                    RestdelayS = 0;

                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " : restart moving in turntable mode\n");
                    }
                }
                else
                {
                    if (CheckTrain && DelayedStart)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Number + " delayed start (turntable mode) : " + RestdelayS.ToString() + "\n");
                    }
                    return;
                }
            }

            // check if turntable available, else exit turntable mode
            if (ActiveTurntable == null || ActiveTurntable.MovingTableState == TimetableTurntableControl.MovingTableStateEnum.Inactive)
            {
                NextStopDistanceM = DistanceToEndNodeAuthorityM[0];   // set authorized distance
                MovementState = AI_MOVEMENT_STATE.STOPPED;  // set state to stopped to revert to normal working
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "TURNTABLE : exit from table, distance to go : " + NextStopDistanceM + "\n");
                }
                return;
            }

            if (ActiveTurntable.CheckTurntableAvailable())
            {
                ActiveTurntable.UpdateTurntableStateAI(elapsedClockSeconds, presentTime);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update for train in Station state (train is at station)
        /// Override for AITrain class
        /// <\summary>

        public override void UpdateStationState(float elapsedClockSeconds, int presentTime)
        {
            StationStop thisStation = StationStops[0];
            bool removeStation = false;

            int eightHundredHours = 8 * 3600;
            int sixteenHundredHours = 16 * 3600;
            int actualdepart = thisStation.ActualDepart;

            // no arrival / departure time set : update times

            if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
            {
                AtStation = true;

                if (CheckTrain)
                {
                    DateTime baseDTCT = new DateTime();
                    DateTime prsTimeCT = baseDTCT.AddSeconds(presentTime);

                    if (thisStation.ActualArrival < 0)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                             Number.ToString() + " at station " +
                             StationStops[0].PlatformItem.Name + " ; no arrival time set; now at " +
                             prsTimeCT.ToString("HH:mm:ss") + "\n");
                    }
                    else
                    {
                        DateTime arrTimeCT = baseDTCT.AddSeconds(thisStation.ActualArrival);

                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                             Number.ToString() + " at station " +
                             StationStops[0].PlatformItem.Name + " with arrival time " +
                             arrTimeCT.ToString("HH:mm:ss") + " ; now at " +
                             prsTimeCT.ToString("HH:mm:ss") + "\n");
                    }
                }

                if (thisStation.ActualArrival < 0)
                {
                    thisStation.ActualArrival = presentTime;
                    thisStation.CalculateDepartTime(presentTime, this);
                    actualdepart = thisStation.ActualDepart;

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
#if DEBUG_TTANALYSIS
                    TTAnalysisUpdateStationState1(presentTime, thisStation);
#endif
                }

                // check for activation of other train
                ActivateTriggeredTrain(TriggerActivationType.StationStop, thisStation.PlatformReference);

                // set reference arrival for any waiting connections
                if (thisStation.ConnectionsWaiting.Count > 0)
                {
                    foreach (int otherTrainNumber in thisStation.ConnectionsWaiting)
                    {
                        Train otherTrain = GetOtherTTTrainByNumber(otherTrainNumber);
                        if (otherTrain != null)
                        {
                            foreach (StationStop otherStop in otherTrain.StationStops)
                            {
                                if (String.Compare(thisStation.PlatformItem.Name, otherStop.PlatformItem.Name) == 0)
                                {
                                    if (otherStop.ConnectionsAwaited.ContainsKey(Number))
                                    {
                                        otherStop.ConnectionsAwaited.Remove(Number);
                                        otherStop.ConnectionsAwaited.Add(Number, thisStation.ActualArrival);
                                    }
                                }
                            }
                        }
                    }
                }

                // check for detach actions

                if (DetachDetails.ContainsKey(thisStation.PlatformReference))
                {
                    List<DetachInfo> detachList = DetachDetails[thisStation.PlatformReference];
                    for (int iDetach = 0; iDetach < detachList.Count; iDetach++)
                    {
                        DetachInfo thisDetach = detachList[iDetach];
                        if (thisDetach.Valid)
                        {
                            DetachActive[0] = thisStation.PlatformReference;
                            DetachActive[1] = iDetach;
                            thisDetach.PerformDetach(this, true);
                            thisDetach.Valid = false;
                        }
                    }
                    DetachDetails.Remove(thisStation.PlatformReference);
                }

                // check for connections

                if (thisStation.ConnectionsAwaited.Count > 0)
                {
                    int deptime = -1;
                    int needwait = -1;
                    needwait = ProcessConnections(thisStation, out deptime);

                    // if required to wait : exit
                    if (needwait >= 0)
                    {
                        return;
                    }

                    if (deptime >= 0)
                    {
                        actualdepart = CompareTimes.LatestTime(actualdepart, deptime);
                        thisStation.ActualDepart = actualdepart;
                    }
                }

                // check for attachments or transfers

                if (NeedAttach.ContainsKey(thisStation.PlatformReference))
                {
                    // waiting for train to attach : exit
                    if (NeedAttach[thisStation.PlatformReference].Count > 0)
                    {
                        return;
                    }
                }

                if (NeedStationTransfer.ContainsKey(thisStation.PlatformReference))
                {
                    // waiting for transfer : exit
                    if (NeedStationTransfer[thisStation.PlatformReference].Count > 0)
                    {
                        return;
                    }
                }

                foreach (TrackCircuitSection occSection in OccupiedTrack)
                {
                    // waiting for transfer : exit
                    if (NeedTrainTransfer.ContainsKey(occSection.Index))
                    {
                        return;
                    }
                }

            }

            // not yet time to depart - check if signal can be released

            int correctedTime = presentTime;

            if (actualdepart > sixteenHundredHours && presentTime < eightHundredHours) // should have departed before midnight
            {
                correctedTime = presentTime + (24 * 3600);
            }

            if (actualdepart < eightHundredHours && presentTime > sixteenHundredHours) // to depart after midnight
            {
                correctedTime = presentTime - 24 * 3600;
            }

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
                        nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null);// for AI always use direction 0
                    }
                    thisStation.HoldSignal = false;
                }
                return;
            }

            // depart

            // check for activation of other train
            ActivateTriggeredTrain(TriggerActivationType.StationDepart, thisStation.PlatformReference);

            // check if to attach in this platform

            bool readyToAttach = false;
            TTTrain trainToTransfer = null;

            if (AttachDetails != null)
            {
                // check if to attach at this station
                bool attachAtStation = AttachDetails.StationPlatformReference == StationStops[0].PlatformReference;

                // check if to attach at end, train is in last station, not first in and other train is ahead
                if (!attachAtStation && !AttachDetails.FirstIn)
                {
                    if (AttachDetails.StationPlatformReference == -1 && StationStops.Count == 1)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[StationStops[0].TCSectionIndex];
                        foreach (KeyValuePair<TrainRouted, int> trainToCheckInfo in thisSection.CircuitState.TrainOccupy)
                        {
                            TTTrain otherTrain = trainToCheckInfo.Key.Train as TTTrain;
                            if (otherTrain.OrgAINumber == AttachDetails.AttachTrain)
                            {
                                attachAtStation = true;
                                AttachDetails.StationPlatformReference = StationStops[0].PlatformReference; // set attach is at this station
                            }
                        }
                    }
                }

                readyToAttach = attachAtStation;
            }

            if (readyToAttach)
            {
                trainToTransfer = GetOtherTTTrainByNumber(AttachDetails.AttachTrain);

                // check if train exists
                if (trainToTransfer == null)
                {
                    // if firstin, check if train is among trains to be started, or if it is an autogen train, or if it is waiting to be started
                    if (AttachDetails.FirstIn)
                    {
                        trainToTransfer = Simulator.GetAutoGenTTTrainByNumber(AttachDetails.AttachTrain);

                        if (trainToTransfer == null)
                        {
                            trainToTransfer = AI.StartList.GetNotStartedTTTrainByNumber(AttachDetails.AttachTrain, false);
                        }

                        if (trainToTransfer == null)
                        {
                            foreach (TTTrain wtrain in AI.TrainsToAdd)
                            {
                                if (wtrain.Number == AttachDetails.AttachTrain || wtrain.OrgAINumber == AttachDetails.AttachTrain)
                                {
                                    return;  // found train - just wait a little longer
                                }
                            }
                        }

                        // train cannot be found
                        if (trainToTransfer == null)
                        {
                            Trace.TraceInformation("Train {0} : cannot find train {1} to attach", Name, AttachDetails.AttachTrainName);
                            AttachDetails = null;
                        }
                        else
                        {
                            return;  // wait until train exists
                        }
                    }
                    // not first in - train not found
                    else
                    {
                        Trace.TraceInformation("Train {0} : cannot find train {1} to attach", Name, AttachDetails.AttachTrainName);
                        AttachDetails = null;
                    }
                }
                else
                {
                    if (trainToTransfer.AtStation && trainToTransfer.StationStops[0].PlatformReference == AttachDetails.StationPlatformReference)
                    {
                        readyToAttach = AttachDetails.ReadyToAttach = true;
                    }
                    else if (trainToTransfer.TrainType == TRAINTYPE.PLAYER && trainToTransfer.AtStation)
                    {
                        readyToAttach = AttachDetails.ReadyToAttach = true;
                    }
                    else
                    {
                        // exit as departure is not allowed
                        return;
                    }
                }

                if (readyToAttach)
                {
                    // if setback required, reverse train
                    if (AttachDetails.SetBack)
                    {
                        // remove any reserved sections
                        RemoveFromTrackNotOccupied(ValidRoute[0]);

                        // check if train in same section
                        float distanceToTrain = 0.0f;
                        if (trainToTransfer.PresentPosition[0].TCSectionIndex == PresentPosition[1].TCSectionIndex)
                        {
                            TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][PresentPosition[1].RouteListIndex].TCSectionIndex];
                            distanceToTrain = thisSection.Length;
                        }
                        else
                        {
                            // get section index of other train in train route
                            int endSectionIndex = ValidRoute[0].GetRouteIndexBackward(trainToTransfer.PresentPosition[0].TCSectionIndex, PresentPosition[1].RouteListIndex);
                            if (endSectionIndex < 0)
                            {
                                Trace.TraceWarning("Train {0} : attach to train {1} failed, cannot find path", Name, trainToTransfer.Name);
                            }

                            // get distance to train
                            for (int iSection = PresentPosition[0].RouteListIndex; iSection >= endSectionIndex; iSection--)
                            {
                                TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iSection].TCSectionIndex];
                                distanceToTrain += thisSection.Length;
                            }
                        }

                        // create temp route and set as valid route
                        int newDirection = PresentPosition[0].TCDirection == 0 ? 1 : 0;
                        TCSubpathRoute tempRoute = signalRef.BuildTempRoute(this, PresentPosition[0].TCSectionIndex, 0.0f, newDirection, distanceToTrain, true, true, false);

                        // set reverse positions
                        TCPosition tempPosition = new TCPosition();
                        PresentPosition[0].CopyTo(ref tempPosition);
                        PresentPosition[1].CopyTo(ref PresentPosition[0]);
                        tempPosition.CopyTo(ref PresentPosition[1]);

                        PresentPosition[0].Reverse(ValidRoute[0][PresentPosition[0].RouteListIndex].Direction, tempRoute, Length, signalRef);
                        PresentPosition[0].CopyTo(ref PreviousPosition[0]);
                        PresentPosition[1].Reverse(ValidRoute[0][PresentPosition[1].RouteListIndex].Direction, tempRoute, 0.0f, signalRef);

                        // reverse formation
                        ReverseFormation(false);

                        // get new route list indices from new route

                        DistanceTravelledM = 0;
                        ValidRoute[0] = tempRoute;
                        TCRoute.TCRouteSubpaths[TCRoute.activeSubpath] = new TCSubpathRoute(tempRoute);

                        PresentPosition[0].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
                        PresentPosition[1].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
                    }
                    else
                    {
                        // build path to train - straight forward, set distance of 2000m (should be enough)
                        TCSubpathRoute tempRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, 0.0f, PresentPosition[1].TCDirection, 2000, true, true, false);
                        ValidRoute[0] = tempRoute;
                        TCRoute.TCRouteSubpaths[TCRoute.activeSubpath] = new TCSubpathRoute(tempRoute);

                        PresentPosition[0].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
                        PresentPosition[1].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
                    }

                    LastReservedSection[0] = PresentPosition[0].TCSectionIndex;
                    LastReservedSection[1] = PresentPosition[1].TCSectionIndex;

                    MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                    SwitchToNodeControl(PresentPosition[0].TCSectionIndex);
                    StartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);

                    return;
                }
            }

            // first, check state of signal

            if (thisStation.ExitSignal >= 0 && thisStation.HoldSignal)
            {
                HoldingSignals.Remove(thisStation.ExitSignal);
                var nextSignal = signalRef.SignalObjects[thisStation.ExitSignal];

                // only request signal if in signal mode (train may be in node control)
                if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
                {
                    nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null); // for AI always use direction 0
                }
            }

            // check if station is end of path

            bool[] endOfPath = ProcessEndOfPath(presentTime);

            // check for exit signal

            bool exitSignalStop = false;
            if (thisStation.ExitSignal >= 0 && NextSignalObject[0] != null && NextSignalObject[0].thisRef == thisStation.ExitSignal)
            {
                MstsSignalAspect nextAspect = GetNextSignalAspect(0);
                exitSignalStop = (nextAspect == MstsSignalAspect.STOP && !thisStation.NoWaitSignal);
            }

            // if not end of path, check if departure allowed
            if (!endOfPath[0] && exitSignalStop)
            {

#if DEBUG_TTANALYSIS
                    TTAnalysisUpdateStationState2();
#endif

                return;  // do not depart if exit signal at danger and waiting is required
            }

            DateTime baseDTd = new DateTime();
            DateTime depTime = baseDTd.AddSeconds(AI.clockTime);

            // change state if train still exists
            if (endOfPath[1])
            {
                if (MovementState == AI_MOVEMENT_STATE.STATION_STOP && !exitSignalStop)
                {
                    AtStation = false;
                    thisStation.Passed = true;

                    MovementState = AI_MOVEMENT_STATE.STOPPED;   // if state is still station_stop and ready and allowed to depart - change to stop to check action
                    RestdelayS = (float)DelayedStartSettings.stationRestart.fixedPartS + ((float)Simulator.Random.Next(DelayedStartSettings.stationRestart.randomPartS * 10) / 10f);
                    if (!endOfPath[0])
                    {
                        removeStation = true;  // set next station if not at end of path
                    }
                    else if (StationStops.Count > 0 && thisStation.PlatformReference == StationStops[0].PlatformReference)
                    {
                        removeStation = true;  // this station is still set as next station so remove
                    }
                }

                if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
                {
                    Delay = TimeSpan.FromSeconds((presentTime - thisStation.DepartTime) % (24 * 3600));
                }
            }

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
            {
                StationStops.RemoveAt(0);
            }

            ResetActions(true);
        }

        //================================================================================================//
        /// <summary>
        /// Update for train in Braking state
        /// Override for AITrain class
        /// <\summary>

        public override void UpdateBrakingState(float elapsedClockSeconds, int presentTime)
        {

            // check if action still required

            bool clearAction = false;
            float distanceToGoM = clearingDistanceM;

            if (MovementState == AI_MOVEMENT_STATE.TURNTABLE)
            {
                distanceToGoM = DistanceToEndNodeAuthorityM[0];
            }

            else if (nextActionInfo == null) // action has been reset - keep status quo
            {
                if (ControlMode == TRAIN_CONTROL.AUTO_NODE)  // node control : use control distance
                {
                    distanceToGoM = DistanceToEndNodeAuthorityM[0];

                    if (EndAuthorityType[0] == END_AUTHORITY.RESERVED_SWITCH)
                    {
                        distanceToGoM = DistanceToEndNodeAuthorityM[0] - 2.0f * junctionOverlapM;
                    }
                    else if (EndAuthorityType[0] == END_AUTHORITY.END_OF_PATH || EndAuthorityType[0] == END_AUTHORITY.END_OF_AUTHORITY)
                    {
                        distanceToGoM = DistanceToEndNodeAuthorityM[0] - (Closeup ? keepDistanceCloseupM : clearingDistanceM);
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

                    if (distanceToGoM < clearingDistanceM && SpeedMpS <= 0)
                    {
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                    "Brake mode - auto node - passed distance stopped - to stop state\n");
                        }
                        MovementState = AI_MOVEMENT_STATE.STOPPED;

#if DEBUG_TTANALYSIS
                        TTAnalysisUpdateBrakingState1();
#endif

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
                            //FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " (now at " +
                            //FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
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
                              nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                              PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
            }

        // check if STOP signal cleared

            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
            {

                if (nextActionInfo.ActiveItem.signal_state >= MstsSignalAspect.APPROACH_1)
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
                            //FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                            //FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
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
                          FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                          FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
#endif
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                              Number.ToString() + " : signal " +
                              nextActionInfo.ActiveItem.ObjectDetails.thisRef.ToString() + " at " +
                                //FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                                //FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                              nextActionInfo.ActivateDistanceM.ToString() + " cleared (now at " +
                              PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                        }
                    }
                }
            }

        // check if RESTRICTED signal cleared

            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED)
            {
                if ((nextActionInfo.ActiveItem.signal_state >= MstsSignalAspect.APPROACH_1) ||
                   ((nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM) ||
                   (nextActionInfo.ActiveItem.ObjectDetails.this_sig_noSpeedReduction(SignalFunction.NORMAL)))
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
                            //FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                            //FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                          nextActionInfo.ActivateDistanceM.ToString() + " cleared (now at " +
                          PresentPosition[0].DistanceTravelledM.ToString() + " - " +
                          FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
            }

    // check if END_AUTHORITY extended

            else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_AUTHORITY)
            {
                nextActionInfo.ActivateDistanceM = DistanceToEndNodeAuthorityM[0] + DistanceTravelledM;
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
                              nextActionInfo.ActivateDistanceM.ToString() + " (now at " +
                              PresentPosition[0].DistanceTravelledM.ToString() + " - " +
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

            if (MovementState == AI_MOVEMENT_STATE.TURNTABLE)
            {
                creepDistanceM = distanceToGoM + signalApproachDistanceM; // ensure creep distance always exceeds distance to go
                NextStopDistanceM = distanceToGoM;

                // if almost in the middle, apply full brakes
                if (distanceToGoM < 0.25)
                {
                    AdjustControlsBrakeFull();
                }

                // if stopped, move to next state
                if (distanceToGoM < 1 && Math.Abs(SpeedMpS) < 0.05f)
                {
                    ActiveTurntable.SetNextStageOnStopped();
                    return;
                }
            }
            else if (nextActionInfo != null)
            {
                requiredSpeedMpS = nextActionInfo.RequiredSpeedMpS;
                distanceToGoM = nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM;

                if (nextActionInfo.ActiveItem != null)
                {
                    distanceToGoM = nextActionInfo.ActiveItem.distance_to_train;
                }

                // check if stopped at station

                if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                {
                    NextStopDistanceM = distanceToGoM;

                    // check if station has exit signal and if signal is clear
                    // if signal is at stop, check if stop position is sufficiently clear of signal

                    if (NextSignalObject[0] != null && NextSignalObject[0].this_sig_lr(MstsSignalFunction.NORMAL) == MstsSignalAspect.STOP)
                    {
                        float reqsignaldistance = StationStops[0].CloseupSignal ? keepDistanceCloseupSignalM : signalApproachDistanceM;
                        if (distanceToGoM > DistanceToSignal.Value - reqsignaldistance)
                        {
                            if (CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                     Number.ToString() + " stop distance at station " + StationStops[0].PlatformItem.Name + " : " + distanceToGoM.ToString() +
                                     " corrected to " + (DistanceToSignal.Value - reqsignaldistance) + "\n");
                            }
                            distanceToGoM = DistanceToSignal.Value - reqsignaldistance;
                        }
                    }

                    // check if stopped
                    // train is stopped - set departure time

                    if (distanceToGoM < 0.25f * keepDistanceCloseupSignalM)
                    {
                        if (Math.Abs(SpeedMpS) < 0.05f)
                        {
                            SpeedMpS = 0;
                            MovementState = AI_MOVEMENT_STATE.STATION_STOP;

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

                    // perform slow approach to stop
                        else if (distanceToGoM > 0)
                        {
                            if (AITrainBrakePercent < 50)
                            {
                                AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 10);
                                AITrainThrottlePercent = 0;
                            }
                        }

                        else
                        {
                            AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 100);
                            AITrainThrottlePercent = 0;
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
                        Alpha10 = 5;
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
                    if (SpeedMpS < 0.05f) MovementState = AI_MOVEMENT_STATE.STOPPED;
                    RestdelayS = DelayedStartSettings.reverseAddedDelaySperM * Length;
                }

                // check if stopped at signal

                else if (nextActionInfo.RequiredSpeedMpS == 0)
                {
                    NextStopDistanceM = distanceToGoM;
                    float stopDistanceM = signalApproachDistanceM;

                    // allow closeup on end of route
                    if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE && Closeup) stopDistanceM = keepDistanceCloseupM;

                    // set distance for signal
                    else if (nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP) stopDistanceM = clearingDistanceM;

                    if (distanceToGoM < stopDistanceM)
                    {
                        AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);
                        AITrainThrottlePercent = 0;
                        if (Math.Abs(SpeedMpS) < 0.05f)
                        {
                            SpeedMpS = 0;
                            MovementState = AI_MOVEMENT_STATE.STOPPED;
#if DEBUG_TTANALYSIS
                            TTAnalysisUpdateBrakingState2();
#endif

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
                            Trace.TraceWarning("Forced stop for signal at danger for train {0} ({1}) at speed {2}", Name, Number, SpeedMpS);
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
                            requiredSpeedMpS = SpeedSettings.creepSpeedMpS.Value;
                        }
                    }
                }
            }

            // keep speed within required speed band

            // preset, also valid for reqSpeed > 0
            float lowestSpeedMpS = requiredSpeedMpS;

            if (requiredSpeedMpS == 0)
            {
                // station stop : use closeup distance as final stop approach
                if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                {
                    creepDistanceM = keepDistanceCloseupSignalM;
                    lowestSpeedMpS = SpeedSettings.creepSpeedMpS.Value;
                }
                // signal : use 3 * signalApproachDistanceM as final stop approach to avoid signal overshoot
                else if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                {
                    creepDistanceM = 3.0f * signalApproachDistanceM;
                    lowestSpeedMpS =
                        distanceToGoM < creepDistanceM ? (0.5f * SpeedSettings.creepSpeedMpS.Value) : SpeedSettings.creepSpeedMpS.Value;
                }
                // otherwise use clearingDistanceM as approach distance
                else if (nextActionInfo == null && requiredSpeedMpS == 0)
                {
                    creepDistanceM = clearingDistanceM;
                    lowestSpeedMpS =
                        distanceToGoM < creepDistanceM ? (0.5f * SpeedSettings.creepSpeedMpS.Value) : SpeedSettings.creepSpeedMpS.Value;
                }
                else
                {
                    lowestSpeedMpS = SpeedSettings.creepSpeedMpS.Value;
                }

            }

            lowestSpeedMpS = Math.Min(lowestSpeedMpS, AllowedMaxSpeedMpS);

            // braking distance - use 0.25 * MaxDecelMpSS as average deceleration (due to braking delay)
            // Videal - Vreq = a * T => T = (Videal - Vreq) / a
            // R = Vreq * T + 0.5 * a * T^2 => R = Vreq * (Videal - Vreq) / a + 0.5 * a * (Videal - Vreq)^2 / a^2 =>
            // R = Vreq * Videal / a - Vreq^2 / a + Videal^2 / 2a - 2 * Vreq * Videal / 2a + Vreq^2 / 2a => R = Videal^2 / 2a - Vreq^2 /2a
            // so : Vreq = SQRT (2 * a * R + Vreq^2)
            // remaining distance is corrected for minimal approach distance as safety margin
            // for requiredSpeed > 0, take hysteris margin off ideal speed so speed settles on required speed
            // for requiredSpeed == 0, use ideal speed, this allows actual speed to be a little higher
            // upto creep distance : set creep speed as lowest possible speed

            float correctedDistanceToGoM = distanceToGoM - creepDistanceM;

            float maxPossSpeedMpS = lowestSpeedMpS;
            if (correctedDistanceToGoM > 0)
            {
                maxPossSpeedMpS = (float)Math.Sqrt(0.25f * MaxDecelMpSS * 2.0f * correctedDistanceToGoM + (requiredSpeedMpS * requiredSpeedMpS));
                maxPossSpeedMpS = Math.Max(lowestSpeedMpS, maxPossSpeedMpS);
            }

            float idealSpeedMpS = requiredSpeedMpS == 0 ? Math.Min((AllowedMaxSpeedMpS - 2f * hysterisMpS), maxPossSpeedMpS) : Math.Min(AllowedMaxSpeedMpS, maxPossSpeedMpS) - (2f * hysterisMpS);
            float idealLowBandMpS = Math.Max(0.25f * lowestSpeedMpS, idealSpeedMpS - (3f * hysterisMpS));
            float ideal3LowBandMpS = Math.Max(0.5f * lowestSpeedMpS, idealSpeedMpS - (9f * hysterisMpS));
            float idealHighBandMpS = Math.Min(AllowedMaxSpeedMpS, Math.Max(lowestSpeedMpS, idealSpeedMpS) + hysterisMpS);
            float ideal3HighBandMpS = Math.Min(AllowedMaxSpeedMpS, Math.Max(lowestSpeedMpS, idealSpeedMpS) + (2f * hysterisMpS));

            float deltaSpeedMpS = SpeedMpS - requiredSpeedMpS;
            float idealDecelMpSS = Math.Max((0.5f * MaxDecelMpSS), (deltaSpeedMpS * deltaSpeedMpS / (2.0f * distanceToGoM)));

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
                               "     alpha : " + Alpha10.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     dist  : " + distanceToGoM.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     A&B(S): " + AITrainThrottlePercent.ToString() + " - " + AITrainBrakePercent.ToString() + "\n");
            }

            // keep speed withing band 

            // speed exceeds allowed maximum - set brakes and clamp speed
            if (SpeedMpS > AllowedMaxSpeedMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : SpeedMpS > AllowedMaxSpeedMpS \n");
                }

                if (AITrainThrottlePercent > 0)
                {
                    AdjustControlsThrottleOff();
                }
                else
                {
                    AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 20);
                }

                // clamp speed
                AdjustControlsFixedSpeed(AllowedMaxSpeedMpS);

                Alpha10 = 5;
            }

            // reached end position
            else if (SpeedMpS > requiredSpeedMpS && distanceToGoM < 0)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : SpeedMpS > requiredSpeedMpS && distanceToGoM < 0 \n");
                }

                // if required to stop then force stop
                if (requiredSpeedMpS == 0 && nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
                {
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                        "Train : " + Name + "(" + Number + ") forced to stop, " +
                        " at " + DistanceTravelledM.ToString() + ", and speed " + SpeedMpS.ToString() + "\n");
                    }
                    Trace.TraceInformation("Train : {0} ({1}) forced to stop, at {2}, and speed {3} \n", Name, Number, DistanceTravelledM.ToString(), SpeedMpS.ToString());
                    SpeedMpS = 0;  // force to standstill
                }
                // increase brakes
                else
                {
                    AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);
                }
            }

            // speed beyond top threshold
            else if (SpeedMpS > ideal3HighBandMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : SpeedMpS > ideal3HighBandMpS \n");
                }

                if (AITrainThrottlePercent > 0)
                {
                    AdjustControlsThrottleOff();
                }
                else if (AITrainBrakePercent < 50)
                {
                    AdjustControlsBrakeMore(2.0f * MaxDecelMpSS, elapsedClockSeconds, 10);
                    Alpha10 = 10;
                }
                // if at full brake always perform application as it forces braking in case of brake failure (eg due to wheelslip)
                else if (AITrainBrakePercent == 100)
                {
                    AdjustControlsBrakeMore(2.0f * MaxDecelMpSS, elapsedClockSeconds, 50);
                    Alpha10 = 0;
                }
                else if (lastDecelMpSS < 0.5f * idealDecelMpSS)
                {
                    AdjustControlsBrakeMore(2.0f * MaxDecelMpSS, elapsedClockSeconds, 10);
                    Alpha10 = 10;
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }

            // speed just above ideal
            else if (SpeedMpS > idealHighBandMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : SpeedMpS > idealHighBandMpS \n");
                }

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
                    else if (AITrainThrottlePercent <= 50 && Alpha10 <= 0)
                    {
                        AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                        Alpha10 = 10;
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

            // speed just below ideal
            else if (SpeedMpS > idealLowBandMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : SpeedMpS > idealLowBandMpS \n");
                }

                if (SpeedMpS > LastSpeedMpS)
                {
                    if (AITrainThrottlePercent > 50)
                    {
                        AdjustControlsAccelLess(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                        Alpha10 = 10;
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
                    else if (AITrainThrottlePercent <= 50)
                    {
                        AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                    }
                    else if (Alpha10 <= 0)
                    {
                        AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                        Alpha10 = 10;
                    }
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }

            // speed below ideal but above lowest threshold
            else if (SpeedMpS > ideal3LowBandMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : SpeedMpS > ideal3LowBandMpS \n");
                }

                if (AITrainBrakePercent > 50)
                {
                    AdjustControlsBrakeLess(0.0f, elapsedClockSeconds, 20);
                }
                else if (AITrainBrakePercent > 0)
                {
                    AdjustControlsBrakeOff();
                }
                else if (AITrainThrottlePercent <= 50)
                {
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
                else if (SpeedMpS < LastSpeedMpS || Alpha10 <= 0)
                {
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                    Alpha10 = 10;
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }

            // speed below required speed
            else if (SpeedMpS < requiredSpeedMpS || (requiredSpeedMpS == 0 && Math.Abs(SpeedMpS) < 0.1f))
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : SpeedMpS < requiredSpeedMpS \n");
                }

                AdjustControlsBrakeOff();
                if (((SpeedMpS - LastSpeedMpS) / elapsedClockSeconds) < 0.5f * MaxAccelMpSS)
                {
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
                else if (AITrainThrottlePercent > 99)
                {
                    // force setting to 100% to force acceleration
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
                Alpha10 = 5;
            }
            else if (distanceToGoM > 4 * preferredBrakingDistanceM && SpeedMpS < idealLowBandMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : distanceToGoM > 4 * preferredBrakingDistanceM && SpeedMpS < idealLowBandMpS \n");
                }

                if (AITrainBrakePercent > 0)
                {
                    AdjustControlsBrakeOff();
                }
                else
                {
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
            }
            else if (distanceToGoM > preferredBrakingDistanceM && SpeedMpS < ideal3LowBandMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : distanceToGoM > preferredBrakingDistanceM && SpeedMpS < ideal3LowBandMpS \n");
                }

                if (AITrainBrakePercent > 0)
                {
                    AdjustControlsBrakeOff();
                }
                else
                {
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
            }
            else if (requiredSpeedMpS == 0 && distanceToGoM > creepDistanceM && SpeedMpS < idealLowBandMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : requiredSpeedMpS == 0 && distanceToGoM > creepDistanceM && SpeedMpS < idealLowBandMpS \n");
                }

                if (AITrainBrakePercent > 0)
                {
                    AdjustControlsBrakeOff();
                }
                else
                {
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
            }
            else if (requiredSpeedMpS == 0 && distanceToGoM > signalApproachDistanceM && SpeedMpS < ideal3LowBandMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : requiredSpeedMpS == 0 && distanceToGoM > signalApproachDistanceM && SpeedMpS < ideal3LowBandMpS \n");
                }

                if (AITrainBrakePercent > 0)
                {
                    AdjustControlsBrakeOff();
                }
                else
                {
                    AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                }
            }

            // in preupdate : avoid problems with overshoot due to low update rate
            // check if at present speed train would pass beyond end of authority
            if (PreUpdate)
            {
                if (requiredSpeedMpS == 0 && distanceToGoM < (10.0f * clearingDistanceM) && (elapsedClockSeconds * SpeedMpS) > (0.5f * distanceToGoM) && SpeedMpS > SpeedSettings.creepSpeedMpS)
                {
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Speed forced down : position " + distanceToGoM.ToString() + ", speed " + SpeedMpS.ToString() + " \n");
                    }

                    SpeedMpS = (0.5f * SpeedMpS);
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
        /// Update in accelerating mode
        /// Override from AITrain class
        /// <\summary>

        public override void UpdateAccelState(float elapsedClockSeconds)
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
        /// Update train in following state (train ahead in same section)
        /// Override from AITrain class
        /// <\summary>

        public override void UpdateFollowingState(float elapsedClockSeconds, int presentTime)
        {
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                                        "Update Train Ahead - now at : " +
                                        PresentPosition[0].TCSectionIndex.ToString() + " " +
                    //                                        FormatStrings.FormatDistance(PresentPosition[0].TCOffset, true) +
                                        PresentPosition[0].TCOffset.ToString() +
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
                Dictionary<Train, float> trainInfo = null;

                // find other train
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

                    // train not in this section, try reserved sections ahead
                    for (int iIndex = startIndex + 1; iIndex <= endIndex; iIndex++)
                    {
                        TrackCircuitSection nextSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                        trainInfo = nextSection.TestTrainAhead(this, 0, ValidRoute[0][iIndex].Direction);
                        if (trainInfo.Count <= 0)
                        {
                            addOffset += nextSection.Length;
                        }
                    }
                    // if train not ahead, try first section beyond last reserved
                    if (trainInfo.Count <= 0 && endIndex < ValidRoute[0].Count - 1)
                    {
                        TrackCircuitSection nextSection = signalRef.TrackCircuitList[ValidRoute[0][endIndex + 1].TCSectionIndex];
                        trainInfo = nextSection.TestTrainAhead(this, 0, ValidRoute[0][endIndex + 1].Direction);
                    }

                    // if train found get distance
                    if (trainInfo.Count > 0)  // found train
                    {
                        foreach (KeyValuePair<Train, float> trainAhead in trainInfo) // always just one
                        {
                            Train OtherTrain = trainAhead.Key;
                            float distanceToTrain = trainAhead.Value + addOffset;
                        }
                    }
                }
                else
                {
                    // ensure train in section is aware of this train in same section if this is required
                    if (PresentPosition[1].TCSectionIndex != thisSection.Index)
                    {
                        UpdateTrainOnEnteringSection(thisSection, trainInfo);
                    }
                }

                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                                        "Train count in section " + sectionIndex.ToString() + " = " + trainInfo.Count.ToString() + "\n");
                }

                // train not in this section, try reserved sections ahead
                for (int iIndex = startIndex + 1; iIndex <= endIndex && trainInfo.Count <= 0; iIndex++)
                {
                    TrackCircuitSection nextSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                    trainInfo = nextSection.TestTrainAhead(this, 0, ValidRoute[0][iIndex].Direction);
                }

                // if train not ahead, try first section beyond last reserved
                if (trainInfo.Count <= 0 && endIndex < ValidRoute[0].Count - 1)
                {
                    TrackCircuitSection nextSection = signalRef.TrackCircuitList[ValidRoute[0][endIndex + 1].TCSectionIndex];
                    trainInfo = nextSection.TestTrainAhead(this, 0, ValidRoute[0][endIndex + 1].Direction);
                    if (trainInfo.Count <= 0)
                    {
                        addOffset += nextSection.Length;
                    }
                }

                // train is found
                if (trainInfo.Count > 0)  // found train
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo) // always just one
                    {
                        TTTrain OtherTrain = trainAhead.Key as TTTrain;
                        float distanceToTrain = trainAhead.Value + addOffset;

                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                    "Other train : " + OtherTrain.Number.ToString() + " at : " +
                                                    OtherTrain.PresentPosition[0].TCSectionIndex.ToString() + " " +
                                //                                                    FormatStrings.FormatDistance(OtherTrain.PresentPosition[0].TCOffset, true) +
                                                    OtherTrain.PresentPosition[0].TCOffset.ToString() +
                                                    " ; speed : " + FormatStrings.FormatSpeed(OtherTrain.SpeedMpS, true) + "\n");
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                //                                                            "DistAhd: " + FormatStrings.FormatDistance(DistanceToEndNodeAuthorityM[0], true) + "\n");
                                                            "DistAhd: " + DistanceToEndNodeAuthorityM[0].ToString() + "\n");
                        }

                        // update action info with new position

                        float keepDistanceTrainM = 0f;
                        bool attachToTrain = false;
                        bool pickUpTrain = false;

                        bool transferTrain = false;
                        int? transferStationIndex = null;
                        int? transferTrainIndex = null;

                        // check attach details
                        if (AttachDetails != null && AttachDetails.Valid && AttachDetails.ReadyToAttach && AttachDetails.AttachTrain == OtherTrain.OrgAINumber)
                        {
                            attachToTrain = true;
                        }

                        // check pickup details
                        if (!attachToTrain)
                        {
                            pickUpTrain = CheckPickUp(OtherTrain);

                            // check transfer details
                            transferTrain = CheckTransfer(OtherTrain, ref transferStationIndex, ref transferTrainIndex);
                        }

                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "attach : " + attachToTrain.ToString() + " ; pickup : " + pickUpTrain.ToString() +
                                               " ; transfer : " + transferTrain.ToString() + "\n");
                        }

                        if (Math.Abs(OtherTrain.SpeedMpS) > 0.1f)
                        {
                            keepDistanceTrainM = keepDistanceMovingTrainM;
                        }
                        else if (!attachToTrain && !pickUpTrain && !transferTrain)
                        {
                            keepDistanceTrainM = (OtherTrain.IsFreight || IsFreight) ? keepDistanceStatTrainM_F : keepDistanceStatTrainM_P;
                            // if closeup is set for termination
                            if (Closeup)
                            {
                                keepDistanceTrainM = keepDistanceTrainAheadCloseupM;
                            }
                            // if train has station stop and closeup set, check if approaching train in station

                            if (StationStops != null && StationStops.Count > 0 && StationStops[0].Closeup)
                            {
                                // other train at station and this is same station
                                if (OtherTrain.AtStation && OtherTrain.StationStops[0].PlatformItem.Name == StationStops[0].PlatformItem.Name)
                                {
                                    keepDistanceTrainM = keepDistanceTrainAheadCloseupM;
                                }
                                // other train in station and this is same station
                                else if (OtherTrain.PresentPosition[1].TCSectionIndex == StationStops[0].TCSectionIndex)
                                {
                                    keepDistanceTrainM = keepDistanceTrainAheadCloseupM;
                                }
                            }

                            // if reversing on track where train is located, also allow closeup
                            if (TCRoute.activeSubpath < TCRoute.TCRouteSubpaths.Count - 1 && TCRoute.ReversalInfo[TCRoute.activeSubpath].Valid)
                            {
                                if (TCRoute.ReversalInfo[TCRoute.activeSubpath].ReversalSectionIndex == OtherTrain.PresentPosition[0].TCSectionIndex ||
                                    TCRoute.ReversalInfo[TCRoute.activeSubpath].ReversalSectionIndex == OtherTrain.PresentPosition[1].TCSectionIndex)
                                {
                                    keepDistanceTrainM = keepDistanceTrainAheadCloseupM;
                                }
                            }
                        }

                        if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD)
                        {
                            NextStopDistanceM = distanceToTrain - keepDistanceTrainM;
                        }

                        // disregard action if train is to attach
                        else if (nextActionInfo != null && !(attachToTrain || pickUpTrain || transferTrain))
                        {
                            float deltaDistance = nextActionInfo.ActivateDistanceM - DistanceTravelledM;
                            if (nextActionInfo.RequiredSpeedMpS > 0.0f)
                            {
                                NextStopDistanceM = distanceToTrain - keepDistanceTrainM;
                            }
                            else
                            {
                                NextStopDistanceM = Math.Min(deltaDistance, (distanceToTrain - keepDistanceTrainM));
                            }

                            if (deltaDistance < distanceToTrain) // perform to normal braking to handle action
                            {
                                MovementState = AI_MOVEMENT_STATE.BRAKING;  // not following the train
                                UpdateBrakingState(elapsedClockSeconds, presentTime);
                                return;
                            }
                        }

                        bool atCouplePosition = false;
                        bool thisTrainFront = false;
                        bool otherTrainFront = false;

                        if (Math.Abs(OtherTrain.SpeedMpS) < 0.1f && (attachToTrain || pickUpTrain || transferTrain))
                        {
                            atCouplePosition = CheckCouplePosition(OtherTrain, out thisTrainFront, out otherTrainFront);
                        }

                        if (Math.Abs(OtherTrain.SpeedMpS) < 0.1f && atCouplePosition)
                        {
                            float reqMinSpeedMpS = SpeedSettings.attachSpeedMpS.Value;
                            if (CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Checked couple position : result : " + atCouplePosition.ToString() + "\n");
                            }

                            if (attachToTrain)
                            {
                                // check if any other train needs to be activated
                                ActivateTriggeredTrain(TriggerActivationType.Dispose, -1);

                                TTCouple(OtherTrain, thisTrainFront, otherTrainFront); // couple this train to other train (this train is aborted)
                            }
                            else if (pickUpTrain)
                            {
                                OtherTrain.TTCouple(this, otherTrainFront, thisTrainFront); // couple other train to this train (other train is aborted)
                                NeedPickUp = false;
                            }
                            else if (transferTrain)
                            {
                                TransferInfo thisTransfer = transferStationIndex.HasValue ? TransferStationDetails[transferStationIndex.Value] : TransferTrainDetails[transferTrainIndex.Value][0];
                                thisTransfer.PerformTransfer(OtherTrain, otherTrainFront, this, thisTrainFront);
                                if (transferStationIndex.HasValue)
                                {
                                    TransferStationDetails.Remove(transferStationIndex.Value);
                                }
                                else if (transferTrainIndex.HasValue)
                                {
                                    TransferTrainDetails.Remove(transferTrainIndex.Value);
                                }
                                NeedTransfer = false;
                            }
                        }

                        // check distance and speed
                        else if (Math.Abs(OtherTrain.SpeedMpS) < 0.1f)
                        {
                            float brakingDistance = SpeedMpS * SpeedMpS * 0.5f * (0.5f * MaxDecelMpSS);
                            float reqspeed = (float)Math.Sqrt(distanceToTrain * MaxDecelMpSS);

                            // allow creepspeed, but if to attach, allow only attach speed
                            float maxspeed = attachToTrain || pickUpTrain || transferTrain ? Math.Max(reqspeed / 2, SpeedSettings.attachSpeedMpS.Value) : Math.Max(reqspeed / 2, SpeedSettings.creepSpeedMpS.Value);

                            if (attachToTrain && AttachDetails.SetBack)
                            {
                                maxspeed = SpeedSettings.attachSpeedMpS.Value;
                            }

                            maxspeed = Math.Min(maxspeed, AllowedMaxSpeedMpS); // but never beyond valid speed limit

                            // set brake or acceleration as required

                            if (SpeedMpS > maxspeed)
                            {
                                AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                            }

                            if ((distanceToTrain - brakingDistance) > keepDistanceTrainM * 3.0f)
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
                            else if ((distanceToTrain - brakingDistance) > keepDistanceTrainM + standardClearingDistanceM)
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
                            else
                            {
                                float reqMinSpeedMpS = attachToTrain || pickUpTrain || transferTrain ? SpeedSettings.attachSpeedMpS.Value : 0.0f;
                                if ((SpeedMpS - reqMinSpeedMpS) > 0.1f)
                                {
                                    AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);

                                    // if too close, force stop or slow down if coupling
                                    if (distanceToTrain < 0.25 * keepDistanceTrainM)
                                    {
                                        foreach (TrainCar car in Cars)
                                        {
                                            //TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
                                            // To achieve the same result with other means, without flipping trainset physics, the line should be changed as follows:
                                            //  car.SpeedMpS = car.Flipped ? -reqMinSpeedMpS : reqMinSpeedMpS;
                                            // car.SpeedMpS = car.Flipped ^ (car.IsDriveable && car.Train.IsActualPlayerTrain && ((MSTSLocomotive)car).UsingRearCab) ? -reqMinSpeedMpS : reqMinSpeedMpS;
                                            car.SpeedMpS = car.Flipped ? -reqMinSpeedMpS : reqMinSpeedMpS;
                                        }
                                        SpeedMpS = reqMinSpeedMpS;
                                    }
                                }
                                else if (attachToTrain || pickUpTrain || transferTrain)
                                {
                                    AdjustControlsBrakeOff();
                                    if (SpeedMpS < 0.2 * SpeedSettings.creepSpeedMpS.Value)
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

                                    TTTrain OtherAITrain = OtherTrain as TTTrain;
                                    otherTrainInStation = (OtherAITrain.MovementState == AI_MOVEMENT_STATE.STATION_STOP || OtherAITrain.MovementState == AI_MOVEMENT_STATE.AI_STATIC);

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

                                    if (CheckTrain && StationStops.Count > 0)
                                    {
                                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                             Number.ToString() + " checking station " +
                                             StationStops[0].PlatformItem.Name +
                                             " \n   -- other train movement state : " + OtherAITrain.MovementState.ToString() + " ; at station : " + otherTrainInStation.ToString() +
                                             " \n   -- station position : " + ValidRoute[0].GetRouteIndex(StationStops[0].TCSectionIndex, PresentPosition[0].RouteListIndex) +
                                             " checked against " + PresentPosition[0].RouteListIndex + "\n");
                                    }
                                    else if (CheckTrain)
                                    {
                                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                                             Number.ToString() + " ; Station List : " + StationStops.Count + "\n");
                                    }

                                    if (thisTrainInStation)
                                    {
                                        MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                                        AtStation = true;
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
                        // if getting too close apply full brake
                        else if (distanceToTrain < (2 * clearingDistanceM))
                        {
                            AdjustControlsBrakeFull();
                        }
                        // check only if train is close and other train speed is below allowed speed
                        else if (distanceToTrain < keepDistanceTrainM - clearingDistanceM && OtherTrain.SpeedMpS < AllowedMaxSpeedMpS)
                        {
                            if (SpeedMpS > (OtherTrain.SpeedMpS + hysterisMpS) ||
                                SpeedMpS > (maxFollowSpeedMpS + hysterisMpS) ||
                                       distanceToTrain < (keepDistanceTrainM - clearingDistanceM))
                            {
                                AdjustControlsBrakeMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 10);
                            }
                            else if (SpeedMpS < (OtherTrain.SpeedMpS - hysterisMpS) &&
                                       SpeedMpS < maxFollowSpeedMpS &&
                                       distanceToTrain > (keepDistanceTrainM + clearingDistanceM))
                            {
                                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 2);
                            }
                        }
                        // perform normal update
                        else UpdateRunningState(elapsedClockSeconds);
                    }
                }

                // train not found - keep moving, state will change next update
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update train in running state
        /// Override from AITrain class
        /// <\summary>

        public override void UpdateRunningState(float elapsedClockSeconds)
        {

            float topBand = AllowedMaxSpeedMpS - ((1.5f - Efficiency) * hysterisMpS);
            float highBand = Math.Max(0.5f, AllowedMaxSpeedMpS - ((3.0f - 2.0f * Efficiency) * hysterisMpS));
            float lowBand = Math.Max(0.4f, AllowedMaxSpeedMpS - ((9.0f - 3.0f * Efficiency) * hysterisMpS));

            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                                        "Running calculation details : \n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     Actual: " + SpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     Allwd : " + AllowedMaxSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     MaxDec: " + MaxDecelMpSS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     low   : " + lowBand.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     middle: " + highBand.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     high  : " + topBand.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     alpha : " + Alpha10.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt",
                               "     A&B(S): " + AITrainThrottlePercent.ToString() + " - " + AITrainBrakePercent.ToString() + "\n");
            }

            // check speed

            if (SpeedMpS > AllowedMaxSpeedMpS)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt","Section : > AllowedMaxSpeedMps \n");
                }

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
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : > topBand \n");
                }

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
                        AdjustControlsBrakeMore(MaxDecelMpSS, elapsedClockSeconds, 50);
                    }
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }
            else if (SpeedMpS > highBand)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : > highBand \n");
                }

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
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : > lowBand \n");
                }

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
                    Alpha10 = 0;
                }
                Alpha10 = Alpha10 > 0 ? --Alpha10 : 0;
            }
            else
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Section : lowest section \n");
                }

                AdjustControlsBrakeOff();
                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
                Alpha10 = 0;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Delay Start Moving
        /// <\summary>

        public void DelayedStartMoving(AI_START_MOVEMENT reason)
        {
            // do not apply delayed restart while running in pre-update
            if (Simulator.PreUpdate)
            {
                RestdelayS = 0.0f;
                DelayedStart = false;
                StartMoving(reason);
                return;
            }

            // note : RestDelayS may have a preset value due to previous action (e.g. reverse)
            int randDelayPart = 0;
            float baseDelayPart = 0;

            switch (reason)
            {
                case AI_START_MOVEMENT.END_STATION_STOP: // not used as state is processed through stop - rest delay is preset
                    break;

                case AI_START_MOVEMENT.FOLLOW_TRAIN:
                    baseDelayPart = DelayedStartSettings.followRestart.fixedPartS;
                    randDelayPart = DelayedStartSettings.followRestart.randomPartS * 10;
                    break;

                case AI_START_MOVEMENT.NEW:
                    baseDelayPart = DelayedStartSettings.newStart.fixedPartS;
                    randDelayPart = DelayedStartSettings.newStart.randomPartS * 10;
                    break;

                case AI_START_MOVEMENT.PATH_ACTION:
                case AI_START_MOVEMENT.SIGNAL_CLEARED:
                case AI_START_MOVEMENT.SIGNAL_RESTRICTED:
                    baseDelayPart = DelayedStartSettings.pathRestart.fixedPartS;
                    randDelayPart = DelayedStartSettings.pathRestart.randomPartS * 10;
                    break;

                case AI_START_MOVEMENT.TURNTABLE:
                    baseDelayPart = DelayedStartSettings.movingtableRestart.fixedPartS;
                    randDelayPart = DelayedStartSettings.movingtableRestart.randomPartS * 10;
                    break;

                default:
                    break;
            }

            float randDelay = (float)Simulator.Random.Next(randDelayPart);
            RestdelayS += (baseDelayPart + (randDelay / 10f));
            DelayedStart = true;
            DelayedStartState = reason;

#if DEBUG_TTANALYSIS
            TTAnalysisStartMoving("Delayed");
#endif
        }

        //================================================================================================//
        /// <summary>
        /// Start Moving
        /// Override from AITrain class
        /// <\summary>

        public override void StartMoving(AI_START_MOVEMENT reason)
        {
            // reset brakes, set throttle

            if (reason == AI_START_MOVEMENT.FOLLOW_TRAIN)
            {
                MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                AITrainThrottlePercent = 25;
                AdjustControlsBrakeOff();
            }
            else if (reason == AI_START_MOVEMENT.TURNTABLE)
            {
                if (MovementState != AI_MOVEMENT_STATE.AI_STATIC)  // do not restart while still in static mode)
                {
                    MovementState = AI_MOVEMENT_STATE.TURNTABLE;
                }
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
                AITrainThrottlePercent = PreUpdate ? 50 : 25;
                AdjustControlsBrakeOff();
            }

            SetPercentsFromTrainToTrainset();

#if DEBUG_TTANALYSIS
            TTAnalysisStartMoving("Move");
#endif
        }

        //================================================================================================//
        /// <summary>
        /// Calculate initial position
        /// </summary>

        public TCSubpathRoute CalculateInitialTTTrainPosition(ref bool trackClear, List<TTTrain> nextTrains)
        {
            bool sectionAvailable = true;

            // calculate train length

            float trainLength = 0f;

            for (var i = Cars.Count - 1; i >= 0; --i)
            {
                var car = Cars[i];
                if (i < Cars.Count - 1)
                {
                    trainLength += car.CouplerSlackM + car.GetCouplerZeroLengthM();
                }
                trainLength += car.CarLengthM;
            }

            // default is no referenced train
            TTTrain otherTTTrain = null;

            // check if to be placed ahead of other train

            if (!String.IsNullOrEmpty(CreateAhead))
            {
                otherTTTrain = GetOtherTTTrainByName(CreateAhead);

                // if not found - check if it is the player train
                if (otherTTTrain == null)
                {
                    if (Simulator.PlayerLocomotive != null && Simulator.PlayerLocomotive.Train != null && String.Equals(Simulator.PlayerLocomotive.Train.Name.ToLower(), CreateAhead))
                    {
                        TTTrain playerTrain = Simulator.PlayerLocomotive.Train as TTTrain;
                        if (playerTrain.TrainType == TRAINTYPE.PLAYER || playerTrain.TrainType == TRAINTYPE.INTENDED_PLAYER) // train is started
                        {
                            otherTTTrain = Simulator.PlayerLocomotive.Train as TTTrain;
                        }
                    }
                }

                // if other train does not yet exist, check if it is on the 'to start' list, and check start-time
                if (otherTTTrain == null)
                {
                    otherTTTrain = AI.StartList.GetNotStartedTTTrainByName(CreateAhead, false);

                    // if other train still does not exist, check if it is on starting list
                    if (otherTTTrain == null && nextTrains != null)
                    {
                        foreach (TTTrain otherTT in nextTrains)
                        {
                            if (String.Equals(otherTT.Name.ToLower(), CreateAhead))
                            {
                                otherTTTrain = otherTT;
                                break;
                            }
                        }
                    }

                    // if really not found - set error
                    if (otherTTTrain == null)
                    {
                        Trace.TraceWarning("Creating train : " + Name + " ; cannot find train " + CreateAhead + " for initial placement, /ahead qualifier ignored\n");
                        CreateAhead = String.Empty;
                    }
                    else
                    {
                        if (!otherTTTrain.StartTime.HasValue)
                        {
                            Trace.TraceWarning("Creating train : " + Name + " ; train refered in /ahead qualifier is not started by default, /ahead qualifier ignored\n");
                            CreateAhead = String.Empty;
                        }
                        else if (otherTTTrain.StartTime > StartTime)
                        {
                            Trace.TraceWarning("Creating train : " + Name + " ; train refered in /ahead qualifier has later start-time, start time for this train reset\n");
                            StartTime = otherTTTrain.StartTime + 1;
                        }
                        // train is to be started now so just wait
                        else
                        {
                            trackClear = false;
                            return (null);
                        }
                    }
                }
            }

            // get starting position and route

            TrackNode tn = RearTDBTraveller.TN;
            float offset = RearTDBTraveller.TrackNodeOffset;
            int direction = (int)RearTDBTraveller.Direction;

            PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
            offset = PresentPosition[1].TCOffset;

            // create route if train has none

            if (ValidRoute[0] == null)
            {
                ValidRoute[0] = signalRef.BuildTempRoute(this, thisSection.Index, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, trainLength, true, true, false);
            }

            // find sections

            float remLength = trainLength;
            int routeIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
            if (routeIndex < 0)
                routeIndex = 0;

            bool sectionsClear = true;

            TCSubpathRoute tempRoute = new TCSubpathRoute();
            TCRouteElement thisElement = ValidRoute[0][routeIndex];

            // check sections if not placed ahead of other train

            if (otherTTTrain != null)
            {
                sectionAvailable = false;
                sectionAvailable = GetPositionAheadOfTrain(otherTTTrain, ValidRoute[0], ref tempRoute);
            }
            else
            {
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
                            Trace.TraceWarning("Not sufficient track to place train {0}", Name);
                            sectionAvailable = false;
                        }
                    }

                }
            }

            trackClear = true;

            if (!sectionAvailable || !sectionsClear)
            {
                trackClear = false;
                tempRoute.Clear();
            }

            return (tempRoute);
        }

        //================================================================================================//
        /// <summary>
        /// Calculate initial train placement
        /// </summary>
        /// <param name="testOccupied"></param>
        /// <returns></returns>

        public bool InitialTrainPlacement(bool testOccupied)
        {
            // for initial placement, use direction 0 only
            // set initial positions

            TrackNode tn = FrontTDBTraveller.TN;
            float offset = FrontTDBTraveller.TrackNodeOffset;
            int direction = (int)FrontTDBTraveller.Direction;

            PresentPosition[0].SetTCPosition(tn.TCCrossReference, offset, direction);
            PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            DistanceTravelledM = 0.0f;

            tn = RearTDBTraveller.TN;
            offset = RearTDBTraveller.TrackNodeOffset;
            direction = (int)RearTDBTraveller.Direction;

            PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);

            // create route to cover all train sections

            TCSubpathRoute tempRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                        PresentPosition[1].TCDirection, Length, true, true, false);

            if (ValidRoute[0] == null)
            {
                ValidRoute[0] = new TCSubpathRoute(tempRoute);
            }

            // get index of first section in route

            int rearIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
            if (rearIndex < 0)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Start position of end of train {" + Number + "} ({" + Name + "}) not on route ");
                }
                rearIndex = 0;
            }

            PresentPosition[1].RouteListIndex = rearIndex;

            // get index of front of train

            int frontIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
            if (frontIndex < 0)
            {
                Trace.TraceWarning("Start position of front of train {0} ({1}) not on route ", Name, Number);
                frontIndex = 0;
            }

            PresentPosition[0].RouteListIndex = frontIndex;

            // check if train can be placed
            // get index of section in train route //

            int routeIndex = rearIndex;
            List<TrackCircuitSection> placementSections = new List<TrackCircuitSection>();

            // check if route is available - use temp route as trains own route may not cover whole train

            offset = PresentPosition[1].TCOffset;
            float remLength = Length;
            bool sectionAvailable = true;

            for (int iRouteIndex = 0; iRouteIndex <= tempRoute.Count - 1 && sectionAvailable; iRouteIndex++)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[tempRoute[iRouteIndex].TCSectionIndex];
                if (thisSection.CanPlaceTrain(this, offset, remLength) || !testOccupied)
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
                            Trace.TraceWarning("Not sufficient track to place train {0}", Name);
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

            // set any deadlocks for sections ahead of start with end beyond start

            for (int iIndex = 0; iIndex < rearIndex; iIndex++)
            {
                int rearSectionIndex = ValidRoute[0][iIndex].TCSectionIndex;
                if (DeadlockInfo.ContainsKey(rearSectionIndex))
                {
                    foreach (Dictionary<int, int> thisDeadlock in DeadlockInfo[rearSectionIndex])
                    {
                        foreach (KeyValuePair<int, int> thisDetail in thisDeadlock)
                        {
                            int endSectionIndex = thisDetail.Value;
                            if (ValidRoute[0].GetRouteIndex(endSectionIndex, rearIndex) >= 0)
                            {
                                TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];
                                endSection.SetDeadlockTrap(Number, thisDetail.Key);
                            }
                        }
                    }
                }
            }

            // set track occupied (if not done yet)

            List<TrackCircuitSection> newPlacementSections = new List<TrackCircuitSection>();
            foreach (TrackCircuitSection thisSection in placementSections)
            {
                if (!thisSection.IsSet(routedForward, false))
                {
                    newPlacementSections.Add(thisSection);
                }
            }

            // first reserve to ensure switches are all alligned properly
            foreach (TrackCircuitSection thisSection in newPlacementSections)
            {
                thisSection.Reserve(routedForward, ValidRoute[0]);
            }

            // next set occupied
            foreach (TrackCircuitSection thisSection in newPlacementSections)
            {
                if (OccupiedTrack.Contains(thisSection)) OccupiedTrack.Remove(thisSection);
                thisSection.SetOccupied(routedForward);
            }

            // reset TrackOccupied to remove any 'hanging' occupations and set the sections in correct sequence
            OccupiedTrack.Clear();
            foreach (TrackCircuitSection thisSection in placementSections)
            {
                OccupiedTrack.Add(thisSection);
            }

            return (true);
        }

        //================================================================================================//
        /// <summary>
        /// Get position of train if train is placed ahead of other train in same section
        /// </summary>
        /// <param name="otherTTTrain"></param>
        /// <param name="trainRoute"></param>
        /// <param name="tempRoute"></param>
        /// <returns></returns>

        public bool GetPositionAheadOfTrain(TTTrain otherTTTrain, TCSubpathRoute trainRoute, ref TCSubpathRoute tempRoute)
        {
            bool validPlacement = false;
            float remainingLength = Length;

            // get front position of other train
            int otherTrainSectionIndex = otherTTTrain.PresentPosition[0].TCSectionIndex;
            int routeListIndex = trainRoute.GetRouteIndex(otherTrainSectionIndex, 0);

            // front position is not in this trains route - reset clear ahead and check normal path
            if (routeListIndex < 0)
            {
                Trace.TraceWarning("Train : " + Name + " : train referred to in /ahead qualifier is not in train's path, /ahead ignored\n");
                CreateAhead = String.Empty;
                tempRoute = CalculateInitialTrainPosition(ref validPlacement);
                return (validPlacement);
            }

            // front position is in this trains route - check direction
            TCRouteElement thisElement = trainRoute[routeListIndex];

            // not the same direction : cannot place train as front or rear is now not clear
            if (otherTTTrain.PresentPosition[0].TCDirection != thisElement.Direction)
            {
                Trace.TraceWarning("Train : " + Name + " : train referred to in /ahead qualifier has different direction, train can not be placed \n");
                return (false);
            }

            // train is positioned correctly

            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

            float startoffset = otherTTTrain.PresentPosition[0].TCOffset + keepDistanceCloseupM;
            int firstSection = thisElement.TCSectionIndex;

            // train starts in same section - check rest of section
            if (startoffset <= thisSection.Length)
            {

                tempRoute.Add(thisElement);

                PresentPosition[0].TCDirection = thisElement.Direction;
                PresentPosition[0].TCSectionIndex = thisElement.TCSectionIndex;
                PresentPosition[0].TCOffset = startoffset;
                PresentPosition[0].RouteListIndex = trainRoute.GetRouteIndex(thisElement.TCSectionIndex, 0);

                Dictionary<Train, float> moreTrains = thisSection.TestTrainAhead(this, startoffset, thisElement.Direction);

                // more trains found - check if sufficient space in between, if so, place train
                if (moreTrains.Count > 0)
                {
                    KeyValuePair<Train, float> nextTrainInfo = moreTrains.ElementAt(0);
                    if (nextTrainInfo.Value > Length)
                    {
                        return (true);
                    }

                    // more trains found - cannot place train
                    // do not report as warning - train may move away in time
                    return (false);
                }

                // no other trains in section - determine remaining length
                remainingLength -= (thisSection.Length - startoffset);
                validPlacement = true;
            }
            else
            {
                startoffset = startoffset - thisSection.Length; // offset in next section

                routeListIndex++;
                if (routeListIndex <= trainRoute.Count - 1)
                {
                    thisElement = trainRoute[routeListIndex];
                    firstSection = thisElement.TCSectionIndex;
                }
                else
                {
                    validPlacement = false;
                }
            }

            // check for rest of train

            float offset = startoffset;

            // test rest of train in rest of route
            while (remainingLength > 0 && validPlacement)
            {
                tempRoute.Add(thisElement);
                remainingLength -= (thisSection.Length - offset);

                if (remainingLength > 0)
                {
                    if (routeListIndex < trainRoute.Count - 1)
                    {
                        routeListIndex++;
                        thisElement = trainRoute[routeListIndex];
                        thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        offset = 0;

                        if (!thisSection.CanPlaceTrain(this, 0, remainingLength))
                        {
                            validPlacement = false;
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Not sufficient track to place train {0}", Name);
                        validPlacement = false;
                    }
                }
            }

            // adjust front traveller to use found offset
            float moved = -RearTDBTraveller.TrackNodeOffset;

            foreach (TCRouteElement nextElement in trainRoute)
            {
                if (nextElement.TCSectionIndex == firstSection)
                {
                    moved += startoffset;
                    break;
                }
                else
                {
                    moved += signalRef.TrackCircuitList[nextElement.TCSectionIndex].Length;
                }
            }

            RearTDBTraveller.Move(moved);

            return (validPlacement);
        }

        //================================================================================================//
        /// <summary>
        /// Check if train is close enough to other train to perform coupling
        /// Override from AITrain
        /// </summary>
        /// <param name="attachTrain"></param>
        /// <param name="thisTrainFront"></param>
        /// <param name="otherTrainFront"></param>
        /// <returns></returns>

        public override bool CheckCouplePosition(Train attachTrain, out bool thisTrainFront, out bool otherTrainFront)
        {
            thisTrainFront = true;
            otherTrainFront = true;

            Traveller usedTraveller = new Traveller(FrontTDBTraveller);
            int usePosition = 0;

            if (MUDirection == Direction.Reverse)
            {
                usedTraveller = new Traveller(RearTDBTraveller, Traveller.TravellerDirection.Backward); // use in direction of movement
                thisTrainFront = false;
                usePosition = 1;
            }

            Traveller otherTraveller = null;
            int useOtherPosition = 0;
            bool withinSection = false;

            // Check if train is in same section as other train, either for the other trains front or rear
            if (PresentPosition[usePosition].TCSectionIndex == attachTrain.PresentPosition[0].TCSectionIndex) // train in same section as front
            {
                withinSection = true;
            }
            else if (PresentPosition[usePosition].TCSectionIndex == attachTrain.PresentPosition[1].TCSectionIndex) // train in same section as rear
            {
                useOtherPosition = 1;
                withinSection = true;
            }

            if (!withinSection) // not yet in same section
            {
                return (false);
            }

            // test directions
            if (PresentPosition[usePosition].TCDirection == attachTrain.PresentPosition[useOtherPosition].TCDirection) // trains are in same direction
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

            //if (PreUpdate) return (true); // in pre-update, being in the same section is good enough

            // check distance to other train
            float dist = usedTraveller.OverlapDistanceM(otherTraveller, false);
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Check couple position : distance : " + dist.ToString() + "\n");
            }

            return (dist < 0.1f);
        }

        //================================================================================================//
        /// <summary>
        /// Update Section State - additional
        /// clear waitany actions for this section
        /// Override from Train class
        /// </summary>

        public override void UpdateSectionState_Additional(int sectionIndex)
        {
            // clear any entries in WaitAnyList as these are now redundant
            if (WaitAnyList != null && WaitAnyList.ContainsKey(sectionIndex))
            {
                WaitAnyList.Remove(sectionIndex);
                if (WaitAnyList.Count <= 0)
                {
                    WaitAnyList = null;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Clear moving table after moving off table
        /// </summary>

        public override void ClearMovingTable()
        {
            // only if valid reference
            if (ActiveTurntable != null)
            {
                ActiveTurntable.RemoveTrainFromTurntable();
                ActiveTurntable = null;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Initialize player train
        /// </summary>

        public void InitalizePlayerTrain()
        {
            TrainType = TRAINTYPE.PLAYER;
            InitializeBrakes();

            foreach (var tcar in Cars)
            {
                if (tcar is MSTSLocomotive)
                {
                    MSTSLocomotive loco = tcar as MSTSLocomotive;
                    loco.SetPower(true);
                    loco.AntiSlip = leadLocoAntiSlip;
                }
            }

            PowerState = true;
        }

        //================================================================================================//
        /// <summary>
        /// Check couple actions for player train to other train
        /// </summary>
        public void CheckPlayerAttachState()
        {
            // check for attach
            if (AttachDetails != null)
            {
                CheckPlayerAttachTrain();
            }

            // check for pickup
            CheckPlayerPickUpTrain();

            // check for transfer
            CheckPlayerTransferTrain();
        }

        //================================================================================================//
        /// <summary>
        /// Check attach for player train
        /// Perform attach if train is ready
        /// </summary>
        public void CheckPlayerAttachTrain()
        {
            // check for attach for player train
            if (AttachDetails.Valid && AttachDetails.ReadyToAttach)
            {
                TTTrain attachTrain = GetOtherTTTrainByNumber(AttachDetails.AttachTrain);

                if (attachTrain != null)
                {
                    // if in neutral, use forward position
                    int direction = MUDirection == Direction.N ? (int)Direction.Forward : (int)MUDirection;

                    // check if train is in same section
                    if (PresentPosition[direction].TCSectionIndex == attachTrain.PresentPosition[0].TCSectionIndex ||
                        PresentPosition[direction].TCSectionIndex == attachTrain.PresentPosition[1].TCSectionIndex)
                    {
                        bool thisTrainFront = true;
                        bool otherTrainFront = true;
                        bool readyToAttach = CheckCouplePosition(attachTrain, out thisTrainFront, out otherTrainFront);

                        if (readyToAttach)
                        {
                            ProcessRouteEndTimetablePlayer();  // perform end of route actions
                            TTCouple(attachTrain, thisTrainFront, otherTrainFront);
                        }
                    }
                }
                // check if train not yet started
                else
                {
                    attachTrain = AI.StartList.GetNotStartedTTTrainByNumber(AttachDetails.AttachTrain, false);

                    if (attachTrain == null)
                    {
                        attachTrain = Simulator.GetAutoGenTTTrainByNumber(AttachDetails.AttachTrain);
                    }
                }

                // train cannot be found
                if (attachTrain == null)
                {
                    Trace.TraceWarning("Train {0} : Train {1} to attach to not found", Name, AttachDetails.AttachTrainName);
                    AttachDetails.Valid = false;
                }
            }
            // check for train to attach in static mode
            else if (EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD && AttachDetails.StationPlatformReference < 0 && AttachDetails.Valid)
            {
                for (int iRouteSection = PresentPosition[0].RouteListIndex; iRouteSection < ValidRoute[0].Count; iRouteSection++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iRouteSection].TCSectionIndex];
                    if (thisSection.CircuitState.HasTrainsOccupying())
                    {
                        List<TrainRouted> allTrains = thisSection.CircuitState.TrainsOccupying();
                        foreach (TrainRouted routedTrain in allTrains)
                        {
                            TTTrain otherTrain = routedTrain.Train as TTTrain;
                            if (otherTrain.OrgAINumber == AttachDetails.AttachTrain && otherTrain.MovementState == AI_MOVEMENT_STATE.AI_STATIC && otherTrain.ActivateTime != null)
                            {
                                AttachDetails.ReadyToAttach = true;
                            }
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check pickup for player train
        /// If ready perform pickup
        /// </summary>
        public void CheckPlayerPickUpTrain()
        {
            List<TTTrain> pickUpTrainList = new List<TTTrain>();

            int thisSectionIndex = PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

            if (thisSection.CircuitState.HasOtherTrainsOccupying(routedForward))
            {
                List<TrainRouted> otherTrains = thisSection.CircuitState.TrainsOccupying();

                foreach (TrainRouted otherTrain in otherTrains)
                {
                    TTTrain otherTTTrain = otherTrain.Train as TTTrain;
                    CheckPickUp(otherTTTrain);

                    if (NeedPickUp)
                    {
                        // if in neutral, use forward position
                        int direction = MUDirection == Direction.N ? (int)Direction.Forward : (int)MUDirection;

                        // check if train is in same section
                        if (PresentPosition[direction].TCSectionIndex == otherTTTrain.PresentPosition[0].TCSectionIndex ||
                            PresentPosition[direction].TCSectionIndex == otherTTTrain.PresentPosition[1].TCSectionIndex)
                        {
                            bool thisTrainFront = true;
                            bool otherTrainFront = true;
                            bool readyToPickUp = CheckCouplePosition(otherTTTrain, out thisTrainFront, out otherTrainFront);

                            if (readyToPickUp)
                            {
                                otherTTTrain.TTCouple(this, otherTrainFront, thisTrainFront);
                                NeedPickUp = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check transfer state for player train
        /// If ready perform transfer
        /// </summary>
        public void CheckPlayerTransferTrain()
        {
            bool transferRequired = false;
            int? transferStationIndex = null;
            int? transferTrainIndex = null;

            TTTrain otherTrain = null;
            TransferInfo thisTransfer = null;

            int thisSectionIndex = PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

            // check train ahead
            if (thisSection.CircuitState.HasOtherTrainsOccupying(routedForward))
            {
                Dictionary<Train, float> trainInfo = thisSection.TestTrainAhead(this, PresentPosition[0].TCOffset, PresentPosition[0].TCDirection);

                foreach (KeyValuePair<Train, float> thisTrain in trainInfo) // always just one
                {
                    otherTrain = thisTrain.Key as TTTrain;
                    transferRequired = CheckTransfer(otherTrain, ref transferStationIndex, ref transferTrainIndex);
                    break;
                }

                if (transferRequired)
                {
                    // if in neutral, use forward position
                    int direction = MUDirection == Direction.N ? (int)Direction.Forward : (int)MUDirection;

                    // check if train is in same section
                    if (PresentPosition[direction].TCSectionIndex == otherTrain.PresentPosition[0].TCSectionIndex ||
                        PresentPosition[direction].TCSectionIndex == otherTrain.PresentPosition[1].TCSectionIndex)
                    {
                        bool thisTrainFront = true;
                        bool otherTrainFront = true;
                        bool readyToTransfer = CheckCouplePosition(otherTrain, out thisTrainFront, out otherTrainFront);

                        if (readyToTransfer)
                        {
                            if (transferStationIndex.HasValue)
                            {
                                thisTransfer = TransferStationDetails[transferStationIndex.Value];
                                TransferStationDetails.Remove(transferStationIndex.Value);
                            }
                            else if (transferTrainIndex.HasValue)
                            {
                                thisTransfer = TransferTrainDetails[transferTrainIndex.Value][0];
                                TransferTrainDetails.Remove(transferTrainIndex.Value);
                            }
                        }

                        if (thisTransfer != null)
                        {
                            thisTransfer.PerformTransfer(otherTrain, otherTrainFront, this, thisTrainFront);
                            NeedTransfer = false;
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Test if section is access to pool
        /// If so, extend route to pool storage road
        /// Override from Train class
        /// </endsummary>
        public override bool CheckPoolAccess(int sectionIndex)
        {
            bool validPool = false;

            if (PoolAccessSection == sectionIndex)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Name + "(" +
                         Number.ToString() + ") at section " + sectionIndex + " exits to pool " + ExitPool + "\n");
                }

                TimetablePool thisPool = Simulator.PoolHolder.Pools[ExitPool];
                int PoolStorageState = (int)PoolAccessState.PoolInvalid;

                TCSubpathRoute newRoute = thisPool.SetPoolExit(this, out PoolStorageState, true);

                // if pool is valid, set new path
                if (newRoute != null)
                {
                    // reset pool access
                    PoolAccessSection = -1;

                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Name + "(" +
                             Number.ToString() + ") exits to pool " + ExitPool + "; pool valid, path extended \n");
                    }

                    PoolStorageIndex = PoolStorageState;
                    TCRoute.TCRouteSubpaths[TCRoute.TCRouteSubpaths.Count - 1] = new TCSubpathRoute(newRoute);
                    if (TCRoute.activeSubpath == TCRoute.TCRouteSubpaths.Count - 1)
                    {
                        ValidRoute[0] = new TCSubpathRoute(newRoute);

                        // remove end-of-route action and recreate it as it may be altered on approach to moving table
                        DistanceTravelledItem removeAction = null;
                        foreach (DistanceTravelledItem thisAction in requiredActions)
                        {
                            if (thisAction.GetType() == typeof(AIActionItem))
                            {
                                AIActionItem thisAIAction = thisAction as AIActionItem;
                                if (thisAIAction.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE)
                                {
                                    removeAction = thisAction;
                                    break;
                                }
                            }
                        }

                        if (removeAction != null) requiredActions.Remove(removeAction);

                        // set new end of route action
                        SetEndOfRouteAction();
                    }
                    validPool = true;
                }

                // if pool is claimed, set valid pool but take no further actions
                else if (PoolStorageState == (int)PoolAccessState.PoolClaimed)
                {
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Name + "(" +
                             Number.ToString() + ") exits to pool " + ExitPool + "; pool claimed, path not extended \n");
                    }

                    validPool = true;
                }

                // if pool is not valid, reset pool info
                else
                {
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + Name + "(" +
                             Number.ToString() + ") exits to pool " + ExitPool + "; pool overflow, pool exit removed \n");
                    }

                    // reset pool access
                    PoolAccessSection = -1;
                    ExitPool = String.Empty;
                }
            }

            return (validPool);
        }

        //================================================================================================//
        /// <summary>
        /// Test for Call On state if train is stopped at signal
        /// CallOn state for TT mode depends on $callon flag, or attach/pickup/transfer requirement for train ahead
        /// Override from Train class
        /// </summary>
        /// <param name="thisSignal"></param>
        /// <param name="allowOnNonePlatform"></param>
        /// <param name="thisRoute"></param>
        /// <param name="dumpfile"></param>
        /// <returns></returns>
        public override bool TestCallOn(SignalObject thisSignal, bool allowOnNonePlatform, TCSubpathRoute thisRoute, string dumpfile)
        {
            // always allow if set for stable working
            if (Stable_CallOn)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("CALL ON : Train {0} : valid - train has Stable_CallOn set \n", Name);
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                return (true);
            }

            // test for pool
            if (PoolStorageIndex >= 0)
            {
                TimetablePool thisPool = AI.Simulator.PoolHolder.Pools[ExitPool];
                if (thisPool.TestRouteLeadingToPool(thisRoute, PoolStorageIndex, dumpfile, Name))
                {
                    return (true);
                }
            }

            // loop through sections in signal route
            bool allclear = true;
            bool intoPlatform = false;
            bool firstTrainFound = false;

            foreach (Train.TCRouteElement routeElement in thisRoute)
            {
                TrackCircuitSection routeSection = signalRef.TrackCircuitList[routeElement.TCSectionIndex];

                // if train is to attach to train in section, allow callon if train is stopped

                if (routeSection.CircuitState.HasOtherTrainsOccupying(routedForward))
                {
                    firstTrainFound = true;
                    Dictionary<Train, float> trainInfo = routeSection.TestTrainAhead(this, 0, routeElement.Direction);

                    foreach (KeyValuePair<Train, float> thisTrain in trainInfo) // always just one
                    {
                        TTTrain occTTTrain = thisTrain.Key as TTTrain;
                        AITrain.AI_MOVEMENT_STATE movState = occTTTrain.ControlMode == Train.TRAIN_CONTROL.INACTIVE ? AITrain.AI_MOVEMENT_STATE.AI_STATIC : occTTTrain.MovementState;

                        // if train is moving - do not allow call on
                        if (Math.Abs(occTTTrain.SpeedMpS) > 0.1f)
                        {
                            if (!String.IsNullOrEmpty(dumpfile))
                            {
                                var sob = new StringBuilder();
                                sob.AppendFormat("CALL ON : Train {0} : invalid - train {1} is moving (speed : {2} (m/s)) \n", Name, occTTTrain.Name, occTTTrain.SpeedMpS);
                                File.AppendAllText(dumpfile, sob.ToString());
                            }
                            return (false);
                        }

                        bool goingToAttach = false;
                        if (AttachDetails != null && AttachDetails.Valid && AttachDetails.ReadyToAttach && AttachDetails.AttachTrain == occTTTrain.OrgAINumber)
                        {
                            goingToAttach = true;
                        }

                        if (goingToAttach)
                        {
                            if (movState == AITrain.AI_MOVEMENT_STATE.STOPPED || movState == AITrain.AI_MOVEMENT_STATE.STATION_STOP || movState == AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                            {
                                if (!String.IsNullOrEmpty(dumpfile))
                                {
                                    var sob = new StringBuilder();
                                    sob.AppendFormat("CALL ON : Train {0} : valid - train is to attach to {1} \n", Name, occTTTrain.Name);
                                    File.AppendAllText(dumpfile, sob.ToString());
                                }
                                return (true);
                            }
                            else if ((occTTTrain.TrainType == TRAINTYPE.PLAYER || occTTTrain.TrainType == TRAINTYPE.INTENDED_PLAYER) && occTTTrain.AtStation)
                            {
                                if (!String.IsNullOrEmpty(dumpfile))
                                {
                                    var sob = new StringBuilder();
                                    sob.AppendFormat("CALL ON : Train {0} : valid - train is to attach to {1} \n", Name, occTTTrain.Name);
                                    File.AppendAllText(dumpfile, sob.ToString());
                                }
                                return (true);
                            }
                            else
                            {
                                if (!String.IsNullOrEmpty(dumpfile))
                                {
                                    var sob = new StringBuilder();
                                    sob.AppendFormat("CALL ON : Train {0} : invalid - train is to attach to {1} but train is moving \n", Name, occTTTrain.Name);
                                    File.AppendAllText(dumpfile, sob.ToString());
                                }
                                return (false);
                            }
                        }

                        // check if going to pick up or transfer
                        int? transferStationIndex = null;
                        int? transferTrainIndex = null;

                        if (CheckPickUp(occTTTrain))
                        {
                            if (!String.IsNullOrEmpty(dumpfile))
                            {
                                var sob = new StringBuilder();
                                sob.AppendFormat("CALL ON : Train {0} : valid - train is to pickup {1} \n", Name, occTTTrain.Name);
                                File.AppendAllText(dumpfile, sob.ToString());
                            }
                            return (true);
                        }
                        else if (CheckTransfer(occTTTrain, ref transferStationIndex, ref transferTrainIndex))
                        {
                            if (!String.IsNullOrEmpty(dumpfile))
                            {
                                var sob = new StringBuilder();
                                sob.AppendFormat("CALL ON : Train {0} : valid - train is to transfer to {1} \n", Name, occTTTrain.Name);
                                File.AppendAllText(dumpfile, sob.ToString());
                            }
                            return (true);
                        }
                    }

                    // check if route leads into platform

                    if (routeSection.PlatformIndex.Count > 0)
                    {
                        intoPlatform = true;

                        PlatformDetails thisPlatform = signalRef.PlatformDetailsList[routeSection.PlatformIndex[0]];

                        // stop is next station stop and callon is set
                        if (StationStops.Count > 0 && StationStops[0].PlatformItem.Name == thisPlatform.Name && StationStops[0].CallOnAllowed)
                        {
                            // only allow if train ahead is stopped
                            foreach (KeyValuePair<Train.TrainRouted, int> occTrainInfo in routeSection.CircuitState.TrainOccupy)
                            {
                                Train.TrainRouted occTrain = occTrainInfo.Key;
                                TTTrain occTTTrain = occTrain.Train as TTTrain;
                                AITrain.AI_MOVEMENT_STATE movState = occTTTrain.ControlMode == Train.TRAIN_CONTROL.INACTIVE ? AITrain.AI_MOVEMENT_STATE.AI_STATIC : occTTTrain.MovementState;

                                if (movState == AITrain.AI_MOVEMENT_STATE.STOPPED || movState == AITrain.AI_MOVEMENT_STATE.STATION_STOP || movState == AITrain.AI_MOVEMENT_STATE.AI_STATIC)
                                {
                                    if (!String.IsNullOrEmpty(dumpfile))
                                    {
                                        var sob = new StringBuilder();
                                        sob.AppendFormat("CALL ON : Train {0} : access to platform {1}, train {2} is stopped \n", Name, thisPlatform.Name, occTTTrain.Name);
                                        File.AppendAllText(dumpfile, sob.ToString());
                                    }
                                }
                                else if (occTTTrain.TrainType == Train.TRAINTYPE.PLAYER && occTTTrain.AtStation)
                                {
                                    if (!String.IsNullOrEmpty(dumpfile))
                                    {
                                        var sob = new StringBuilder();
                                        sob.AppendFormat("CALL ON : Train {0} : access to platform {1}, train {2} (player train) is at station \n", Name, thisPlatform.Name, occTTTrain.Name);
                                        File.AppendAllText(dumpfile, sob.ToString());
                                    }
                                }
                                else
                                {
                                    if (!String.IsNullOrEmpty(dumpfile))
                                    {
                                        var sob = new StringBuilder();
                                        sob.AppendFormat("CALL ON : Train {0} : invalid - access to platform {1}, but train {2} is moving \n", Name, thisPlatform.Name, occTTTrain.Name);
                                        File.AppendAllText(dumpfile, sob.ToString());
                                    }
                                    allclear = false;
                                    break; // no need to check for other trains
                                }
                            }
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(dumpfile))
                            {
                                var sob = new StringBuilder();
                                sob.AppendFormat("CALL ON : Train {0} : invalid - access to platform {1} but train has no stop \n", Name, thisPlatform.Name);
                                File.AppendAllText(dumpfile, sob.ToString());
                            }
                            allclear = false;
                        }
                    }

                    // if first train found, check rest of route for platform
                    if (firstTrainFound && !intoPlatform)
                    {
                        int thisSectionRouteIndex = thisRoute.GetRouteIndex(routeSection.Index, 0);
                        for (int iSection = thisSectionRouteIndex + 1; iSection < thisRoute.Count && !intoPlatform; iSection++)
                        {
                            routeSection = signalRef.TrackCircuitList[thisRoute[iSection].TCSectionIndex];
                            if (routeSection.PlatformIndex.Count > 0)
                            {
                                PlatformDetails thisPlatform = signalRef.PlatformDetailsList[routeSection.PlatformIndex[0]];
                                if (StationStops.Count > 0) // train has stops
                                {
                                    if (String.Compare(StationStops[0].PlatformItem.Name, thisPlatform.Name) == 0)
                                    {
                                        intoPlatform = StationStops[0].CallOnAllowed;
                                    }
                                }
                            }
                        }
                    }

                    if (firstTrainFound) break;
                }
            }

            if (intoPlatform)
            {
                return (allclear);
            }
            else
            {
                // path does not lead into platform - return state as defined in call
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("CALL ON : Train {0} : {1} - route does not lead into platform \n", Name, allowOnNonePlatform);
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                return (allowOnNonePlatform);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check if train is ready to attach
        /// </summary>
        public void CheckReadyToAttach()
        {
            // test for attach for train ahead
            if (AttachDetails != null && AttachDetails.StationPlatformReference < 0 && !AttachDetails.ReadyToAttach)
            {
                for (int iRoute = PresentPosition[0].RouteListIndex; iRoute < ValidRoute[0].Count && !AttachDetails.ReadyToAttach; iRoute++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iRoute].TCSectionIndex];
                    if (thisSection.CircuitState.HasTrainsOccupying())
                    {
                        List<TrainRouted> allTrains = thisSection.CircuitState.TrainsOccupying();
                        foreach (TrainRouted routedTrain in allTrains)
                        {
                            TTTrain otherTrain = routedTrain.Train as TTTrain;
                            if (otherTrain.OrgAINumber == AttachDetails.AttachTrain && otherTrain.MovementState == AI_MOVEMENT_STATE.AI_STATIC && otherTrain.ActivateTime != null)
                            {
                                AttachDetails.ReadyToAttach = true;
                                break;
                            }
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check if train is ready to pick up
        /// </summary>
        /// <param name="otherTrain"></param>
        /// <returns></returns>
        public bool CheckPickUp(TTTrain otherTrain)
        {
            bool pickUpTrain = false;

            // if allready set, no need to check again
            if (NeedPickUp)
            {
                return (true);
            }

            // pick up is only possible if train is stopped, inactive and not reactivated at any time
            if (Math.Abs(otherTrain.SpeedMpS) < 0.1f && otherTrain.ControlMode == TRAIN_CONTROL.INACTIVE && otherTrain.ActivateTime == null)
            {
                // check train
                if (PickUpTrains.Contains(otherTrain.OrgAINumber))
                {
                    pickUpTrain = true;
                    PickUpTrains.Remove(otherTrain.Number);
                    NeedPickUp = true;
                }

                // check platform location
                else
                {
                    foreach (TrackCircuitSection thisSection in otherTrain.OccupiedTrack)
                    {
                        foreach (int thisPlatform in thisSection.PlatformIndex)
                        {
                            foreach (int platformReference in signalRef.PlatformDetailsList[thisPlatform].PlatformReference)
                            {
                                if (PickUpStatic.Contains(platformReference))
                                {
                                    pickUpTrain = true;
                                    PickUpStatic.Remove(platformReference);
                                    NeedPickUp = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // check if train is at end of path and pickup on forms is required

                if (!pickUpTrain && PickUpStaticOnForms)
                {
                    // train is not in last subpath so pickup cannot be valid
                    if (TCRoute.activeSubpath != TCRoute.TCRouteSubpaths.Count - 1)
                    {
                        return (false);
                    }

                    // check if we are at end of route
                    if (CheckEndOfRoutePositionTT())
                    {
                        pickUpTrain = true;
                        PickUpStaticOnForms = false;
                        NeedPickUp = true;
                    }
                    // check position other train or check remaining route
                    else
                    {
                        int otherTrainRearIndex = ValidRoute[0].GetRouteIndex(otherTrain.PresentPosition[0].TCSectionIndex, PresentPosition[0].RouteListIndex);
                        int otherTrainFrontIndex = ValidRoute[0].GetRouteIndex(otherTrain.PresentPosition[1].TCSectionIndex, PresentPosition[0].RouteListIndex);

                        bool validtrain = false;

                        // other train front or rear is in final section
                        if (otherTrain.PresentPosition[0].TCSectionIndex == ValidRoute[0][ValidRoute[0].Count - 1].TCSectionIndex)
                        {
                            validtrain = true;
                        }
                        else if (otherTrain.PresentPosition[1].TCSectionIndex == ValidRoute[0][ValidRoute[0].Count - 1].TCSectionIndex)
                        {
                            validtrain = true;
                        }
                        // other train front or rear is not on our route - other train stretches beyond end of route
                        else if (otherTrainRearIndex < 0 || otherTrainFrontIndex < 0)
                        {
                            validtrain = true;
                        }
                        // check if length of remaining path is less than safety clearance
                        // use intended route, not actual route as that may be restricted
                        else
                        {
                            int useindex = Math.Max(otherTrainRearIndex, otherTrainFrontIndex);
                            float remLength = 0;

                            for (int iElement = useindex + 1; iElement < TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count; iElement++)
                            {
                                remLength += signalRef.TrackCircuitList[TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][iElement].TCSectionIndex].Length;
                            }

                            if (remLength < endOfRouteDistance)
                            {
                                validtrain = true;
                            }
                        }

                        // check if there are any other trains in remaining path
                        if (!validtrain)
                        {
                            bool moretrains = false;
                            int useindex = Math.Max(otherTrainRearIndex, otherTrainFrontIndex);

                            for (int iElement = useindex + 1; iElement < TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count; iElement++)
                            {
                                TrackCircuitSection thisSection = signalRef.TrackCircuitList[TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][iElement].TCSectionIndex];
                                List<Train.TrainRouted> trainsInSection = thisSection.CircuitState.TrainsOccupying();
                                if (trainsInSection.Count > 0)
                                {
                                    moretrains = true;
                                    break;
                                }
                            }
                            validtrain = !moretrains;
                        }

                        if (validtrain)
                        {
                            pickUpTrain = true;
                            PickUpStaticOnForms = false;
                            NeedPickUp = true;
                        }
                    }
                }
            }

            return (pickUpTrain);
        }

        //================================================================================================//
        /// <summary>
        /// Check if train is ready to transfer
        /// </summary>
        /// <param name="otherTrain"></param>
        /// <param name="stationTransferIndex"></param>
        /// <param name="trainTransferIndex"></param>
        /// <returns></returns>
        public bool CheckTransfer(TTTrain otherTrain, ref int? stationTransferIndex, ref int? trainTransferIndex)
        {
            bool transferTrain = false;

            // transfer is only possible if train is stopped, either at station (station transfer) or as inactive (train transfer)
            if (Math.Abs(otherTrain.SpeedMpS) > 0.1f)
            {
                return (transferTrain);
            }

            // train transfer
            if (otherTrain.ControlMode == TRAIN_CONTROL.INACTIVE)
            {
                if (TransferTrainDetails.ContainsKey(otherTrain.OrgAINumber))
                {
                    transferTrain = true;
                    trainTransferIndex = otherTrain.OrgAINumber;
                }
                // static transfer required and train is static, set this train number
                else if (TransferTrainDetails.ContainsKey(-99) && otherTrain.MovementState == AI_MOVEMENT_STATE.AI_STATIC && otherTrain.Forms < 0)
                {
                    TransferTrainDetails.Add(otherTrain.OrgAINumber, TransferTrainDetails[-99]);
                    TransferTrainDetails.Remove(-99);
                    transferTrain = true;
                    trainTransferIndex = otherTrain.OrgAINumber;
                }
                
                // if found, no need to look any further
                if (transferTrain)
                {
                    return (transferTrain);
                }
            }

            // station transfer
            if (otherTrain.AtStation)
            {
                if (otherTrain.StationStops != null && otherTrain.StationStops.Count > 0)
                {
                    int stationIndex = otherTrain.StationStops[0].PlatformReference;
                    if (TransferStationDetails.ContainsKey(stationIndex))
                    {
                        TransferInfo thisTransfer = TransferStationDetails[stationIndex];
                        if (thisTransfer.TransferTrain == otherTrain.OrgAINumber)
                        {
                            transferTrain = true;
                            stationTransferIndex = stationIndex;
                        }
                    }
                }
            }

            // transfer at dispose - check if train in required section
            if (!transferTrain)
            {
                if (TransferTrainDetails.ContainsKey(otherTrain.OrgAINumber))
                {
                    foreach (TrackCircuitSection occSection in otherTrain.OccupiedTrack)
                    {
                        if (otherTrain.NeedTrainTransfer.ContainsKey(occSection.Index))
                        {
                            transferTrain = true;
                            trainTransferIndex = otherTrain.OrgAINumber;
                            break;
                        }
                    }
                }
            }

            return (transferTrain);
        }

        //================================================================================================//
        /// <summary>
        /// Check if transfer is required
        /// </summary>
        /// <returns></returns>
        public bool CheckTransferRequired()
        {
            bool transferRequired = false;

            // check if state allready set, if so return state

            if (NeedTransfer)
            {
                return (NeedTransfer);
            }

            // check if transfer required
            if ((TransferStationDetails != null && TransferStationDetails.Count > 0) || (TransferTrainDetails != null && TransferTrainDetails.Count > 0))
            {
                bool firstTrainFound = false;

                for (int iRouteIndex = PresentPosition[0].RouteListIndex; iRouteIndex < ValidRoute[0].Count && !firstTrainFound; iRouteIndex++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iRouteIndex].TCSectionIndex];
                    if (thisSection.CircuitState.HasOtherTrainsOccupying(routedForward))
                    {
                        firstTrainFound = true;
                        Dictionary<Train, float> trainInfo = thisSection.TestTrainAhead(this, 0, ValidRoute[0][iRouteIndex].Direction);

                        foreach (KeyValuePair<Train, float> thisTrain in trainInfo) // always just one
                        {
                            int? transferStationIndex = 0;
                            int? transferTrainIndex = 0;
                            TTTrain otherTrain = thisTrain.Key as TTTrain;

                            if (CheckTransfer(otherTrain, ref transferStationIndex, ref transferTrainIndex))
                            {
                                transferRequired = true;
                                break;
                            }
                        }
                    }
                }
            }

            return (transferRequired);
        }

        //================================================================================================//
        /// <summary>
        /// Insert action item for end-of-route
        /// Override from AITrain class
        /// <\summary>

        public override void SetEndOfRouteAction()
        {
            // check if route leads to moving table

            float lengthToGoM = 0;

            TCRouteElement lastElement = ValidRoute[0].Last();
            if (lastElement.MovingTableApproachPath > -1 && AI.Simulator.PoolHolder.Pools.ContainsKey(ExitPool))
            {
                TimetablePool thisPool = AI.Simulator.PoolHolder.Pools[ExitPool];
                lengthToGoM = thisPool.GetEndOfRouteDistance(TCRoute.TCRouteSubpaths.Last(), PresentPosition[0], lastElement.MovingTableApproachPath, signalRef);
            }

            // remaining length first section
            else
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex];
                lengthToGoM = thisSection.Length - PresentPosition[0].TCOffset;
                // go through all further sections

                for (int iElement = PresentPosition[0].RouteListIndex + 1; iElement < ValidRoute[0].Count; iElement++)
                {
                    TCRouteElement thisElement = ValidRoute[0][iElement];
                    thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    lengthToGoM += thisSection.Length;
                }

                lengthToGoM -= 5.0f; // keep save distance from end

                // if last section does not end at signal or next section is switch, set back overlap to keep clear of switch
                // only do so for last subroute to avoid falling short of reversal points
                // only do so if last section is not a station and closeup is not set for dispose

                TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastElement.TCSectionIndex];
                if (lastSection.EndSignals[lastElement.Direction] == null && TCRoute.activeSubpath == (TCRoute.TCRouteSubpaths.Count - 1))
                {
                    int nextIndex = lastSection.Pins[lastElement.Direction, 0].Link;
                    bool lastIsStation = false;

                    if (StationStops != null && StationStops.Count > 0)
                    {
                        StationStop lastStop = StationStops.Last();
                        if (lastStop.SubrouteIndex == TCRoute.TCRouteSubpaths.Count - 1 && lastStop.PlatformItem.TCSectionIndex.Contains(lastSection.Index))
                        {
                            lastIsStation = true;
                        }
                    }

                    // closeup to junction if closeup is set except on last stop or when storing in pool
                    bool reqcloseup = Closeup && String.IsNullOrEmpty(ExitPool);
                    if (nextIndex >= 0 && !lastIsStation && !reqcloseup)
                    {
                        if (signalRef.TrackCircuitList[nextIndex].CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                        {
                            float lengthCorrection = Math.Max(Convert.ToSingle(signalRef.TrackCircuitList[nextIndex].Overlap), standardOverlapM);
                            if (lastSection.Length - 2 * lengthCorrection < Length) // make sure train fits
                            {
                                lengthCorrection = Math.Max(0.0f, (lastSection.Length - Length) / 2);
                            }
                            lengthToGoM -= lengthCorrection; // correct for stopping position
                        }
                    }
                }
            }

            CreateTrainAction(TrainMaxSpeedMpS, 0.0f, lengthToGoM, null,
                    AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE);
            NextStopDistanceM = lengthToGoM;
        }

        //================================================================================================//
        /// <summary>
        /// Check if train is in Wait state
        /// Override from Train class
        /// </summary>
        /// <returns></returns>
        public override bool isInWaitState()
        {
            bool inWaitState = false;

            if (WaitList != null && WaitList.Count > 0 && WaitList[0].WaitActive) inWaitState = true;

            if (WaitAnyList != null)
            {
                foreach (KeyValuePair<int, List<WaitInfo>> actWInfo in WaitAnyList)
                {
                    foreach (WaitInfo actWait in actWInfo.Value)
                    {
                        if (actWait.WaitActive)
                        {
                            inWaitState = true;
                            break;
                        }
                    }
                }
            }

            return (inWaitState);
        }

        //================================================================================================//
        /// <summary>
        /// Check if train has AnyWait condition at this location
        /// Override from Train class
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override bool CheckAnyWaitCondition(int index)
        {
            if (WaitAnyList != null && WaitAnyList.ContainsKey(index))
            {
                foreach (WaitInfo reqWait in WaitAnyList[index])
                {
                    bool pathClear = CheckForRouteWait(reqWait);
                    return (!pathClear);
                }
            }
            return (false);
        }

        //================================================================================================//
        /// <summary>
        /// Set unprocessed info for train timetable commands
        /// Info will be fully processed after all trains have been created - this is necessary as only then is all required info available
        /// StationStop info may be null if command is not linked to a station (for commands issued at #note line)
        /// </summary>

        public void ProcessTimetableStopCommands(TTTrainCommands thisCommand, int subrouteIndex, int sectionIndex, int stationIndex, int plattformReferenceID, TimetableInfo ttinfo)
        {

            StationStop thisStationStop = (StationStops.Count > 0 && stationIndex >= 0) ? StationStops[stationIndex] : null;

            switch (thisCommand.CommandToken.Trim())
            {
                // WAIT command
                case "wait":
                    if (thisCommand.CommandValues == null)
                    {
                        Trace.TraceInformation("Train : {0} : invalid wait command at {1}", Name, thisStationStop == null ? "note line" : thisStationStop.PlatformItem.Name);
                        break;
                    }

                    foreach (string reqReferenceTrain in thisCommand.CommandValues)
                    {
                        WaitInfo newWaitItem = new WaitInfo();
                        newWaitItem.WaitType = WaitInfo.WaitInfoType.Wait;

                        if (sectionIndex < 0)
                        {
                            newWaitItem.startSectionIndex = TCRoute.TCRouteSubpaths[subrouteIndex][0].TCSectionIndex;
                        }
                        else
                        {
                            newWaitItem.startSectionIndex = sectionIndex;
                        }
                        newWaitItem.startSubrouteIndex = subrouteIndex;

                        newWaitItem.referencedTrainName = String.Copy(reqReferenceTrain);

                        // check if name is full name, otherwise add timetable file info from this train
                        if (!newWaitItem.referencedTrainName.Contains(':'))
                        {
                            int seppos = Name.IndexOf(':');
                            newWaitItem.referencedTrainName = String.Concat(newWaitItem.referencedTrainName, ":", Name.Substring(seppos + 1).ToLower());
                        }

                        // qualifiers : 
                        //  maxdelay (single value only)
                        //  owndelay (single value only)
                        //  notstarted (no value)
                        //  trigger (single value only)
                        //  endtrigger (single value only)
                        //  atstart (no value)

                        if (thisCommand.CommandQualifiers != null)
                        {
                            foreach (TTTrainCommands.TTTrainComQualifiers addQualifier in thisCommand.CommandQualifiers)
                            {
                                switch (addQualifier.QualifierName)
                                {
                                    case "maxdelay":
                                        try
                                        {
                                            newWaitItem.maxDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
                                        }
                                        catch
                                        {
                                            Trace.TraceInformation("Train {0} : invalid value in $wait command for {1} : {2} \n",
                                                Name, addQualifier.QualifierName, addQualifier.QualifierValues[0]);
                                        }
                                        break;
                                    case "notstarted":
                                        newWaitItem.notStarted = true;
                                        break;
                                    case "atstart":
                                        newWaitItem.atStart = true;
                                        break;
                                    case "owndelay":
                                        try
                                        {
                                            newWaitItem.ownDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
                                        }
                                        catch
                                        {
                                            Trace.TraceInformation("Train {0} : invalid value in $wait command for {1} : {2} \n",
                                                Name, addQualifier.QualifierName, addQualifier.QualifierValues[0]);
                                        }
                                        break;
                                    case "trigger":
                                        TimeSpan ttime;
                                        bool validTriggerTim = false;

                                        validTriggerTim = TimeSpan.TryParse(addQualifier.QualifierValues[0], out ttime);
                                        if (validTriggerTim)
                                        {
                                            newWaitItem.waittrigger = Convert.ToInt32(ttime.TotalSeconds);
                                        }
                                        break;
                                    case "endtrigger":
                                        TimeSpan etime;
                                        bool validEndTime = false;

                                        validEndTime = TimeSpan.TryParse(addQualifier.QualifierValues[0], out etime);
                                        if (validEndTime)
                                        {
                                            newWaitItem.waitendtrigger = Convert.ToInt32(etime.TotalSeconds);
                                        }
                                        break;

                                    default:
                                        if (thisStationStop == null)
                                        {
                                            Trace.TraceWarning("Invalid qualifier for WAIT command for train {0} in #note line : {1}",
                                                Name, addQualifier.QualifierName);
                                        }
                                        else
                                        {
                                            Trace.TraceWarning("Invalid qualifier for WAIT command for train {0} at station {1} : {2}",
                                                Name, thisStationStop.PlatformItem.Name, addQualifier.QualifierName);
                                        }
                                        break;
                                }
                            }
                        }

                        if (WaitList == null)
                        {
                            WaitList = new List<WaitInfo>();
                        }
                        WaitList.Add(newWaitItem);
                    }
                    break;

                // FOLLOW command
                case "follow":
                    foreach (string reqReferenceTrain in thisCommand.CommandValues)
                    {
                        WaitInfo newWaitItem = new WaitInfo();
                        newWaitItem.WaitType = WaitInfo.WaitInfoType.Follow;

                        if (sectionIndex < 0)
                        {
                            newWaitItem.startSectionIndex = TCRoute.TCRouteSubpaths[subrouteIndex][0].TCSectionIndex;
                        }
                        else
                        {
                            newWaitItem.startSectionIndex = sectionIndex;
                        }
                        newWaitItem.startSubrouteIndex = subrouteIndex;

                        newWaitItem.referencedTrainName = String.Copy(reqReferenceTrain);

                        // check if name is full name, otherwise add timetable file info from this train
                        if (!newWaitItem.referencedTrainName.Contains(':'))
                        {
                            int seppos = Name.IndexOf(':');
                            newWaitItem.referencedTrainName = String.Concat(newWaitItem.referencedTrainName, ":", Name.Substring(seppos + 1).ToLower());
                        }

                        // qualifiers : 
                        //  maxdelay (single value only)
                        //  owndelay (single value only)
                        //  notstarted (no value)
                        //  trigger (single value only)
                        //  endtrigger (single value only)

                        if (thisCommand.CommandQualifiers != null)
                        {
                            foreach (TTTrainCommands.TTTrainComQualifiers addQualifier in thisCommand.CommandQualifiers)
                            {
                                switch (addQualifier.QualifierName)
                                {
                                    case "maxdelay":
                                        try
                                        {
                                            newWaitItem.maxDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60;
                                        }
                                        catch
                                        {
                                            Trace.TraceInformation("Train {0} : invalid value in $follow command for {1} : {2} \n",
                                                Name, addQualifier.QualifierName, addQualifier.QualifierValues[0]);
                                        }
                                        break;
                                    case "notstarted":
                                        newWaitItem.notStarted = true;
                                        break;
                                    case "atStart":
                                        newWaitItem.atStart = true;
                                        break;
                                    case "owndelay":
                                        try
                                        {
                                            newWaitItem.ownDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
                                        }
                                        catch
                                        {
                                            Trace.TraceInformation("Train {0} : invalid value in $follow command for {1} : {2} \n",
                                                Name, addQualifier.QualifierName, addQualifier.QualifierValues[0]);
                                        }
                                        break;
                                    case "trigger":
                                        TimeSpan ttime;
                                        bool validTriggerTim = false;

                                        validTriggerTim = TimeSpan.TryParse(addQualifier.QualifierValues[0], out ttime);
                                        if (validTriggerTim)
                                        {
                                            newWaitItem.waittrigger = Convert.ToInt32(ttime.TotalSeconds);
                                        }
                                        break;
                                    case "endtrigger":
                                        TimeSpan etime;
                                        bool validEndTime = false;

                                        validEndTime = TimeSpan.TryParse(addQualifier.QualifierValues[0], out etime);
                                        if (validEndTime)
                                        {
                                            newWaitItem.waitendtrigger = Convert.ToInt32(etime.TotalSeconds);
                                        }
                                        break;

                                    default:
                                        Trace.TraceWarning("Invalid qualifier for FOLLOW command for train {0} at station {1} : {2}",
                                            Name, thisStationStop.PlatformItem.Name, addQualifier.QualifierName);
                                        break;
                                }
                            }
                        }

                        if (WaitList == null)
                        {
                            WaitList = new List<WaitInfo>();
                        }
                        WaitList.Add(newWaitItem);
                    }
                    break;

                case "connect":
                    // only valid with section index

                    if (sectionIndex < 0 || stationIndex < 0)
                    {
                        Trace.TraceInformation("Invalid CONNECT command for train {0} : command must be linked to location", Name);
                    }
                    else
                    {
                        foreach (string reqReferenceTrain in thisCommand.CommandValues)
                        {
                            WaitInfo newWaitItem = new WaitInfo();
                            newWaitItem.WaitType = WaitInfo.WaitInfoType.Connect;

                            newWaitItem.startSectionIndex = sectionIndex;
                            newWaitItem.startSubrouteIndex = subrouteIndex;
                            newWaitItem.stationIndex = stationIndex;

                            newWaitItem.referencedTrainName = String.Copy(reqReferenceTrain);

                            // check if name is full name, otherwise add timetable file info from this train
                            if (!newWaitItem.referencedTrainName.Contains(':'))
                            {
                                int seppos = Name.IndexOf(':');
                                newWaitItem.referencedTrainName = String.Concat(newWaitItem.referencedTrainName, ":", Name.Substring(seppos + 1).ToLower());
                            }

                            // qualifiers : 
                            //  maxdelay (single value only)
                            //  hold (single value only)

                            if (thisCommand.CommandQualifiers != null)
                            {
                                foreach (TTTrainCommands.TTTrainComQualifiers addQualifier in thisCommand.CommandQualifiers)
                                {
                                    switch (addQualifier.QualifierName)
                                    {
                                        case "maxdelay":
                                            try
                                            {
                                                newWaitItem.maxDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
                                            }
                                            catch
                                            {
                                                Trace.TraceInformation("Train {0} : invalid value in $connect command for {1} : {2} \n",
                                                    Name, addQualifier.QualifierName, addQualifier.QualifierValues[0]);
                                            }
                                            break;
                                        case "hold":
                                            try
                                            {
                                                newWaitItem.holdTimeS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
                                            }
                                            catch
                                            {
                                                Trace.TraceInformation("Train {0} : invalid value in $connect command for {1} : {2} \n",
                                                    Name, addQualifier.QualifierName, addQualifier.QualifierValues[0]);
                                            }
                                            break;

                                        default:
                                            Trace.TraceWarning("Invalid qualifier for CONNECT command for train {0} at station {1} : {2}",
                                                Name, thisStationStop.PlatformItem.Name, addQualifier.QualifierName);
                                            break;
                                    }
                                }
                            }

                            if (WaitList == null)
                            {
                                WaitList = new List<WaitInfo>();
                            }
                            WaitList.Add(newWaitItem);
                        }
                    }
                    break;

                case "waitany":
                    foreach (string reqReferencePath in thisCommand.CommandValues)
                    {
                        WaitInfo newWaitItem = new WaitInfo();
                        newWaitItem.WaitType = WaitInfo.WaitInfoType.WaitAny;
                        newWaitItem.WaitActive = false;

                        newWaitItem.PathDirection = WaitInfo.CheckPathDirection.Same;
                        if (thisCommand.CommandQualifiers != null && thisCommand.CommandQualifiers.Count > 0)
                        {
                            TTTrainCommands.TTTrainComQualifiers thisQualifier = thisCommand.CommandQualifiers[0]; // takes only 1 qualifier
                            switch (thisQualifier.QualifierName)
                            {
                                case "both":
                                    newWaitItem.PathDirection = WaitInfo.CheckPathDirection.Both;
                                    break;

                                case "opposite":
                                    newWaitItem.PathDirection = WaitInfo.CheckPathDirection.Opposite;
                                    break;

                                default:
                                    Trace.TraceWarning("Invalid qualifier for WAITANY command for train {0} at station {1} : {2}",
                                        Name, thisStationStop.PlatformItem.Name, thisQualifier.QualifierName);
                                    break;
                            }
                        }

                        bool loadPathNoFailure;
                        AIPath fullpath = ttinfo.LoadPath(reqReferencePath, out loadPathNoFailure);

                        // create a copy of this train to process route
                        // copy is required as otherwise the original route would be lost

                        if (fullpath != null) // valid path
                        {
                            TCRoutePath fullRoute = new TCRoutePath(fullpath, -2, 1, signalRef, -1, Simulator.Settings);
                            newWaitItem.CheckPath = new TCSubpathRoute(fullRoute.TCRouteSubpaths[0]);

                            // find first overlap section with train route
                            int overlapSection = -1;
                            int useSubpath = 0;

                            while (overlapSection < 0 && useSubpath <= TCRoute.TCRouteSubpaths.Count)
                            {
                                foreach (TCRouteElement pathElement in newWaitItem.CheckPath)
                                {
                                    if (TCRoute.TCRouteSubpaths[useSubpath].GetRouteIndex(pathElement.TCSectionIndex, 0) > 0)
                                    {
                                        overlapSection = pathElement.TCSectionIndex;
                                        break;
                                    }
                                }

                                useSubpath++;
                            }

                            // if overlap found : insert in waiting list
                            if (overlapSection >= 0)
                            {
                                if (WaitAnyList == null)
                                {
                                    WaitAnyList = new Dictionary<int, List<WaitInfo>>();
                                }

                                if (WaitAnyList.ContainsKey(overlapSection))
                                {
                                    List<WaitInfo> waitList = WaitAnyList[overlapSection];
                                    waitList.Add(newWaitItem);
                                }
                                else
                                {
                                    List<WaitInfo> waitList = new List<WaitInfo>();
                                    waitList.Add(newWaitItem);
                                    WaitAnyList.Add(overlapSection, waitList);
                                }
                            }
                        }
                    }
                    break;

                case "callon":
                    if (thisStationStop != null) thisStationStop.CallOnAllowed = true;
                    break;

                case "hold":
                    if (thisStationStop != null)
                        thisStationStop.HoldSignal = thisStationStop.ExitSignal >= 0; // set holdstate only if exit signal is defined
                    break;

                case "nohold":
                    if (thisStationStop != null)
                        thisStationStop.HoldSignal = false;
                    break;

                case "forcehold":
                    if (thisStationStop != null)
                    {
                        // use platform signal
                        if (thisStationStop.ExitSignal >= 0)
                        {
                            thisStationStop.HoldSignal = true;
                        }
                        // use first signal in route
                        else
                        {
                            TCSubpathRoute usedRoute = TCRoute.TCRouteSubpaths[thisStationStop.SubrouteIndex];
                            int signalFound = -1;

                            TCRouteElement routeElement = usedRoute[thisStationStop.RouteIndex];
                            float distanceToStationSignal = thisStationStop.PlatformItem.DistanceToSignals[routeElement.Direction];

                            for (int iRouteIndex = thisStationStop.RouteIndex; iRouteIndex <= usedRoute.Count - 1 && signalFound < 0; iRouteIndex++)
                            {
                                routeElement = usedRoute[iRouteIndex];
                                TrackCircuitSection routeSection = signalRef.TrackCircuitList[routeElement.TCSectionIndex];

                                if (routeSection.EndSignals[routeElement.Direction] != null)
                                {
                                    signalFound = routeSection.EndSignals[routeElement.Direction].thisRef;
                                }
                                else
                                {
                                    distanceToStationSignal += routeSection.Length;
                                }
                            }

                            if (signalFound >= 0)
                            {
                                thisStationStop.ExitSignal = signalFound;
                                thisStationStop.HoldSignal = true;
                                HoldingSignals.Add(signalFound);

                                thisStationStop.StopOffset = Math.Min(thisStationStop.StopOffset, distanceToStationSignal + thisStationStop.PlatformItem.Length - 10.0f);
                            }
                        }
                    }
                    break;

                case "forcewait":
                    if (thisStationStop != null)
                    {
                        // if no platform signal, use first signal in route
                        if (thisStationStop.ExitSignal < 0)
                        {
                            TCSubpathRoute usedRoute = TCRoute.TCRouteSubpaths[thisStationStop.SubrouteIndex];
                            int signalFound = -1;

                            TCRouteElement routeElement = usedRoute[thisStationStop.RouteIndex];
                            float distanceToStationSignal = thisStationStop.PlatformItem.DistanceToSignals[routeElement.Direction];

                            for (int iRouteIndex = thisStationStop.RouteIndex; iRouteIndex <= usedRoute.Count - 1 && signalFound < 0; iRouteIndex++)
                            {
                                routeElement = usedRoute[iRouteIndex];
                                TrackCircuitSection routeSection = signalRef.TrackCircuitList[routeElement.TCSectionIndex];

                                if (routeSection.EndSignals[routeElement.Direction] != null)
                                {
                                    signalFound = routeSection.EndSignals[routeElement.Direction].thisRef;
                                }
                                else
                                {
                                    distanceToStationSignal += routeSection.Length;
                                }
                            }

                            if (signalFound >= 0)
                            {
                                thisStationStop.ExitSignal = signalFound;
                                thisStationStop.StopOffset = Math.Min(thisStationStop.StopOffset, distanceToStationSignal + thisStationStop.PlatformItem.Length - 10.0f);
                            }
                        }
                    }
                    break;

                case "nowaitsignal":
                    thisStationStop.NoWaitSignal = true;
                    break;

                case "waitsignal":
                    thisStationStop.NoWaitSignal = false;
                    break;

                case "noclaim":
                    thisStationStop.NoClaimAllowed = true;
                    break;

                // no action for terminal (processed in create station stop)
                case "terminal":
                    break;

                // no action for closeupsignal (processed in create station stop)
                case "closeupsignal":
                    break;

                // no action for closeupsignal (processed in create station stop)
                case "closeup":
                    break;

                // no action for extendplatformtosignal (processed in create station stop)
                case "extendplatformtosignal":
                    break;

                // no action for restrictplatformtosignal (processed in create station stop)
                case "restrictplatformtosignal":
                    break;

                // no action for keepclear (processed in create station stop)
                case "keepclear":
                    break;

                // no action for endstop (processed in create station stop)
                case "endstop":
                    break;

                // no action for stoptime (processed in create station stop)
                case "stoptime":
                    break;

                case "detach":
                    // detach at station
                    if (thisStationStop != null)
                    {
                        DetachInfo thisDetach = new DetachInfo(this, thisCommand, false, true, false, thisStationStop.TCSectionIndex, thisStationStop.ArrivalTime);
                        if (DetachDetails.ContainsKey(thisStationStop.PlatformReference))
                        {
                            List<DetachInfo> thisDetachList = DetachDetails[thisStationStop.PlatformReference];
                            thisDetachList.Add(thisDetach);
                        }
                        else
                        {
                            List<DetachInfo> thisDetachList = new List<DetachInfo>();
                            thisDetachList.Add(thisDetach);
                            DetachDetails.Add(thisStationStop.PlatformReference, thisDetachList);
                        }
                    }
                    // detach at start
                    else
                    {
                        int startSection = TCRoute.TCRouteSubpaths[0][0].TCSectionIndex;
                        DetachInfo thisDetach = new DetachInfo(this, thisCommand, true, false, false, startSection, ActivateTime);
                        if (DetachDetails.ContainsKey(-1))
                        {
                            List<DetachInfo> thisDetachList = DetachDetails[-1];
                            thisDetachList.Add(thisDetach);
                        }
                        else
                        {
                            List<DetachInfo> thisDetachList = new List<DetachInfo>();
                            thisDetachList.Add(thisDetach);
                            DetachDetails.Add(-1, thisDetachList);
                        }
                    }
                    break;

                case "attach":
                    // attach at station
                    if (plattformReferenceID >= 0)
                    {
                        AttachDetails = new AttachInfo(plattformReferenceID, thisCommand, this);
                    }
                    break;

                case "pickup":
                    // pickup at station
                    if (plattformReferenceID >= 0)
                    {
                        PickUpInfo thisPickUp = new PickUpInfo(plattformReferenceID, thisCommand, this);
                        PickUpDetails.Add(thisPickUp);
                    }
                    break;

                case "transfer":
                    TransferInfo thisTransfer = new TransferInfo(plattformReferenceID, thisCommand, this);
                    if (plattformReferenceID >= 0)
                    {
                        if (TransferStationDetails.ContainsKey(plattformReferenceID))
                        {
                            Trace.TraceInformation("Train {0} : transfer command : cannot define multiple transfer at a single stop", Name);
                        }
                        else
                        {
                            List<TransferInfo> thisTransferList = new List<TransferInfo>();
                            TransferStationDetails.Add(plattformReferenceID, thisTransfer);
                        }
                    }
                    else
                    {
                        // for now, insert with train set to -1 - will be updated for proper crossreference later
                        if (TransferTrainDetails.ContainsKey(-1))
                        {
                            TransferTrainDetails[-1].Add(thisTransfer);
                        }
                        else
                        {
                            List<TransferInfo> thisTransferList = new List<TransferInfo>();
                            thisTransferList.Add(thisTransfer);
                            TransferTrainDetails.Add(-1, thisTransferList);
                        }
                    }
                    break;

                case "activate":
                    if (thisCommand.CommandValues == null || thisCommand.CommandValues.Count < 1)
                    {
                        Trace.TraceInformation("No train reference set for train activation, train {0}", Name);
                        break;
                    }

                    TriggerActivation thisTrigger = new TriggerActivation();

                    if (plattformReferenceID >= 0)
                    {
                        thisTrigger.platformId = plattformReferenceID;
                        thisTrigger.activationType = TriggerActivationType.StationStop;

                        if (thisCommand.CommandQualifiers != null && thisCommand.CommandQualifiers.Count > 0)
                        {
                            TTTrainCommands.TTTrainComQualifiers thisQualifier = thisCommand.CommandQualifiers[0]; // takes only 1 qualifier
                            if (thisQualifier.QualifierName.Trim().ToLower() == "depart")
                            {
                                thisTrigger.activationType = TriggerActivationType.StationDepart;
                            }
                        }
                    }
                    else
                    {
                        thisTrigger.activationType = TriggerActivationType.Start;
                    }

                    thisTrigger.activatedName = thisCommand.CommandValues[0];
                    activatedTrainTriggers.Add(thisTrigger);

                    break;

                default:
                    Trace.TraceWarning("Invalid station stop command for train {0} : {1}", Name, thisCommand.CommandToken);
                    break;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Finalize the command information - process referenced train details
        /// </summary>

        public void FinalizeTimetableCommands()
        {
            // process all wait information
            if (WaitList != null)
            {
                TTTrain otherTrain = null;
                List<WaitInfo> newWaitItems = new List<WaitInfo>();

                foreach (WaitInfo reqWait in WaitList)
                {
                    switch (reqWait.WaitType)
                    {
                        // WAIT command
                        case WaitInfo.WaitInfoType.Wait:
                            otherTrain = GetOtherTTTrainByName(reqWait.referencedTrainName);
#if DEBUG_TRACEINFO
                            Trace.TraceInformation("Train : " + Name);
                            Trace.TraceInformation("WAIT : Search for : {0} - found {1}", reqWait.referencedTrainName, otherTrain == null ? -1 : otherTrain.Number);
#endif
                            if (otherTrain != null)
                            {
                                ProcessWaitRequest(reqWait, otherTrain, true, true, true, ref newWaitItems);
                            }
                            reqWait.WaitType = WaitInfo.WaitInfoType.Invalid; // set to invalid as item is processed
                            break;

                        // FOLLOW command
                        case WaitInfo.WaitInfoType.Follow:
                            otherTrain = GetOtherTTTrainByName(reqWait.referencedTrainName);
#if DEBUG_TRACEINFO
                            Trace.TraceInformation("Train : " + Name);
                            Trace.TraceInformation("FOLLOW : Search for : {0} - found {1}", reqWait.referencedTrainName, otherTrain == null ? -1 : otherTrain.Number);
#endif
                            if (otherTrain != null)
                            {
                                ProcessWaitRequest(reqWait, otherTrain, true, false, false, ref newWaitItems);
                            }
                            reqWait.WaitType = WaitInfo.WaitInfoType.Invalid; // set to invalid as item is processed
                            break;

                        // CONNECT command
                        case WaitInfo.WaitInfoType.Connect:
                            otherTrain = GetOtherTTTrainByName(reqWait.referencedTrainName);
#if DEBUG_TRACEINFO
                            Trace.TraceInformation("Train : " + Name);
                            Trace.TraceInformation("CONNECT : Search for : {0} - found {1}", reqWait.referencedTrainName, otherTrain == null ? -1 : otherTrain.Number);
#endif
                            if (otherTrain != null)
                            {
                                ProcessConnectRequest(reqWait, otherTrain, ref newWaitItems);
                            }
                            reqWait.WaitType = WaitInfo.WaitInfoType.Invalid; // set to invalid as item is processed
                            break;

                        default:
                            break;
                    }
                }

                // remove processed and invalid items
                for (int iWaitItem = WaitList.Count - 1; iWaitItem >= 0; iWaitItem--)
                {
                    if (WaitList[iWaitItem].WaitType == WaitInfo.WaitInfoType.Invalid)
                    {
                        WaitList.RemoveAt(iWaitItem);
                    }
                }

                // add new created items
                foreach (WaitInfo newWait in newWaitItems)
                {
                    WaitList.Add(newWait);
                }

                // sort list - list is sorted on subpath and route index
                WaitList.Sort();
            }
        }

        //================================================================================================//
        /// <summary>
        /// Process wait request for Timetable waits
        /// </summary>

        public void ProcessWaitRequest(WaitInfo reqWait, TTTrain otherTrain, bool allowSameDirection, bool allowOppositeDirection, bool singleWait, ref List<WaitInfo> newWaitItems)
        {
            // find first common section to determine train directions
            int otherRouteIndex = -1;
            int thisSubpath = reqWait.startSubrouteIndex;
            int thisIndex = TCRoute.TCRouteSubpaths[thisSubpath].GetRouteIndex(reqWait.startSectionIndex, 0);
            int otherSubpath = 0;

            int startSectionIndex = TCRoute.TCRouteSubpaths[thisSubpath][thisIndex].TCSectionIndex;

            bool allSubpathsProcessed = false;
            bool sameDirection = false;
            bool validWait = true;  // presume valid wait

            // set actual start

            TCRouteElement thisTrainElement = null;
            TCRouteElement otherTrainElement = null;

            int thisTrainStartSubpathIndex = reqWait.startSubrouteIndex;
            int thisTrainStartRouteIndex = TCRoute.TCRouteSubpaths[thisTrainStartSubpathIndex].GetRouteIndex(reqWait.startSectionIndex, 0);

            // loop while no section found and further subpaths available

            while (!allSubpathsProcessed)
            {
                otherRouteIndex = -1;

                while (otherRouteIndex < 0)
                {
                    int sectionIndex = TCRoute.TCRouteSubpaths[thisSubpath][thisIndex].TCSectionIndex;
                    otherRouteIndex = otherTrain.TCRoute.TCRouteSubpaths[otherSubpath].GetRouteIndex(sectionIndex, 0);

                    if (otherRouteIndex < 0 && otherSubpath < otherTrain.TCRoute.TCRouteSubpaths.Count - 1)
                    {
                        otherSubpath++;
                    }
                    else if (otherRouteIndex < 0 && thisIndex < TCRoute.TCRouteSubpaths[thisSubpath].Count - 1)
                    {
                        thisIndex++;
                        otherSubpath = 0; // reset other train subpath
                    }
                    else if (otherRouteIndex < 0 && thisSubpath < TCRoute.TCRouteSubpaths.Count - 1)
                    {
                        thisSubpath++;
                        thisIndex = 0;
                        otherSubpath = 0; // reset other train subpath
                    }
                    else if (otherRouteIndex < 0)
                    {
                        validWait = false;
                        break; // no common section found
                    }
                }

                // if valid wait but wait is in next subpath, use start section of next subpath

                if (validWait && thisTrainStartSubpathIndex < thisSubpath)
                {
                    thisTrainStartSubpathIndex = thisSubpath;
                    thisTrainStartRouteIndex = 0;
                }

                // check directions
                if (validWait)
                {
                    thisTrainElement = TCRoute.TCRouteSubpaths[thisSubpath][thisIndex];
                    otherTrainElement = otherTrain.TCRoute.TCRouteSubpaths[otherSubpath][otherRouteIndex];

                    sameDirection = thisTrainElement.Direction == otherTrainElement.Direction;

                    validWait = sameDirection ? allowSameDirection : allowOppositeDirection;
                }

                // if original start section index is also first common index, search for first not common section
                if (validWait && startSectionIndex == otherTrainElement.TCSectionIndex)
                {
                    int notCommonSectionRouteIndex = -1;

                    if (sameDirection)
                    {
                        notCommonSectionRouteIndex =
                                FindCommonSectionEnd(TCRoute.TCRouteSubpaths[thisSubpath], thisIndex,
                                otherTrain.TCRoute.TCRouteSubpaths[otherSubpath], otherRouteIndex);
                    }
                    else
                    {
                        notCommonSectionRouteIndex =
                                FindCommonSectionEndReverse(TCRoute.TCRouteSubpaths[thisSubpath], thisIndex,
                                otherTrain.TCRoute.TCRouteSubpaths[otherSubpath], otherRouteIndex);
                    }

                    // check on found not-common section - start wait here if atstart is set, otherwise start wait at first not-common section
                    int notCommonSectionIndex = otherTrain.TCRoute.TCRouteSubpaths[otherSubpath][notCommonSectionRouteIndex].TCSectionIndex;
                    int lastIndex = TCRoute.TCRouteSubpaths[thisTrainStartSubpathIndex].GetRouteIndex(notCommonSectionIndex, 0);

                    bool atStart = reqWait.atStart.HasValue ? reqWait.atStart.Value : false;

                    if (lastIndex < TCRoute.TCRouteSubpaths[thisTrainStartSubpathIndex].Count - 1) // not last entry
                    {
                        // if opposite direction, use next section as start for common section search
                        // if same direction and atStart not set also use next section as start for common section search
                        // if same direction and atStart is set use first section as start for common section search as train is to wait in this section
                        lastIndex++;
                        if (!sameDirection || !atStart)  
                        {
                            thisTrainStartRouteIndex = lastIndex;
                        }
                        // valid wait, so set all subpaths processed
                        allSubpathsProcessed = true;
                    }
                    else
                    {
                        // full common route but further subpath available - try next subpath
                        if (otherSubpath < otherTrain.TCRoute.TCRouteSubpaths.Count - 1)
                        {
                            otherSubpath++;
                        }
                        else
                        {
                            validWait = false; // full common route - no waiting point possible
                            allSubpathsProcessed = true;
                        }
                    }
                }
                else
                // no valid wait or common section found
                {
                    allSubpathsProcessed = true;
                }
            }

            if (!validWait) return;

            // if in same direction, start at beginning and move downward
            // if in opposite direction, start at end (of found subpath!) and move upward

            int startSubpath = sameDirection ? 0 : otherSubpath;
            int endSubpath = sameDirection ? otherSubpath : 0;
            int startIndex = sameDirection ? 0 : otherTrain.TCRoute.TCRouteSubpaths[startSubpath].Count - 1;
            int endIndex = sameDirection ? otherTrain.TCRoute.TCRouteSubpaths[startSubpath].Count - 1 : 0;
            int increment = sameDirection ? 1 : -1;

            // if first section is common, first search for first non common section

            bool allCommonFound = false;

            // loop through all possible waiting points
            while (!allCommonFound)
            {
                int[,] sectionfound = FindCommonSectionStart(thisTrainStartSubpathIndex, thisTrainStartRouteIndex, otherTrain.TCRoute,
                    startSubpath, startIndex, endSubpath, endIndex, increment);

                // no common section found
                if (sectionfound[0, 0] < 0)
                {
                    allCommonFound = true;
                }
                else
                {
                    WaitInfo newItem = new WaitInfo();
                    newItem.WaitActive = false;
                    newItem.WaitType = reqWait.WaitType;
                    newItem.activeSubrouteIndex = sectionfound[0, 0];
                    newItem.activeRouteIndex = sectionfound[0, 1];
                    newItem.activeSectionIndex = TCRoute.TCRouteSubpaths[newItem.activeSubrouteIndex][newItem.activeRouteIndex].TCSectionIndex;

                    newItem.waitTrainNumber = otherTrain.OrgAINumber;
                    newItem.waitTrainSubpathIndex = sectionfound[1, 0];
                    newItem.waitTrainRouteIndex = sectionfound[1, 1];
                    newItem.maxDelayS = reqWait.maxDelayS;
                    newItem.ownDelayS = reqWait.ownDelayS;
                    newItem.notStarted = reqWait.notStarted;
                    newItem.atStart = reqWait.atStart;
                    newItem.waittrigger = reqWait.waittrigger;
                    newItem.waitendtrigger = reqWait.waitendtrigger;

#if DEBUG_TRACEINFO
                    Trace.TraceInformation("Added wait : other train : {0} at section {1}", newItem.waitTrainNumber, newItem.activeSectionIndex);
#endif
                    newWaitItems.Add(newItem);

                    int endSection = -1;

                    if (singleWait)
                    {
                        allCommonFound = true;
                        break;
                    }
                    else if (sameDirection)
                    {
                        endSection = FindCommonSectionEnd(TCRoute.TCRouteSubpaths[newItem.activeSubrouteIndex], newItem.activeRouteIndex,
                            otherTrain.TCRoute.TCRouteSubpaths[newItem.waitTrainSubpathIndex], newItem.waitTrainRouteIndex);
                    }
                    else
                    {
                        endSection = FindCommonSectionEndReverse(TCRoute.TCRouteSubpaths[newItem.activeSubrouteIndex], newItem.activeRouteIndex,
                            otherTrain.TCRoute.TCRouteSubpaths[newItem.waitTrainSubpathIndex], newItem.waitTrainRouteIndex);
                    }

                    // last common section
                    int lastSectionIndex = otherTrain.TCRoute.TCRouteSubpaths[newItem.waitTrainSubpathIndex][endSection].TCSectionIndex;
                    thisTrainStartRouteIndex = TCRoute.TCRouteSubpaths[thisTrainStartSubpathIndex].GetRouteIndex(lastSectionIndex, thisTrainStartRouteIndex);
                    if (thisTrainStartRouteIndex < TCRoute.TCRouteSubpaths[thisTrainStartSubpathIndex].Count - 1)
                    {
                        thisTrainStartRouteIndex++;  // first not-common section
                    }
                    // end of subpath - shift to next subpath if available
                    else
                    {
                        if (thisTrainStartSubpathIndex < endSubpath)
                        {
                            thisTrainStartSubpathIndex++;
                            thisTrainStartRouteIndex = 0;
                        }
                        else
                        {
                            allCommonFound = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find start of common section of two trains
        /// May check through all subpaths for other train but only through start subpath for this train
        /// 
        /// Return value indices :
        ///   [0, 0] = own train subpath index
        ///   [0, 1] = own train route index
        ///   [1, 0] = other train subpath index
        ///   [1, 1] = other train route index
        /// </summary>
        /// <param name="thisTrainStartSubpathIndex"></param>
        /// <param name="thisTrainStartRouteIndex"></param>
        /// <param name="otherTrainRoute"></param>
        /// <param name="startSubpath"></param>
        /// <param name="startIndex"></param>
        /// <param name="endSubpath"></param>
        /// <param name="endIndex"></param>
        /// <param name="increment"></param>
        /// <param name="sameDirection"></param>
        /// <param name="oppositeDirection"></param>
        /// <param name="foundSameDirection"></param>
        /// <returns></returns>

        public int[,] FindCommonSectionStart(int thisTrainStartSubpathIndex, int thisTrainStartRouteIndex, TCRoutePath otherTrainRoute,
                int startSubpath, int startIndex, int endSubpath, int endIndex, int increment)
        {
            // preset all loop data
            int thisSubpathIndex = thisTrainStartSubpathIndex;
            int thisRouteIndex = thisTrainStartRouteIndex;
            TCSubpathRoute thisRoute = TCRoute.TCRouteSubpaths[thisSubpathIndex];
            int thisRouteEndIndex = thisRoute.Count - 1;

            int otherSubpathIndex = startSubpath;
            int otherRouteIndex = startIndex;
            TCSubpathRoute otherRoute = otherTrainRoute.TCRouteSubpaths[otherSubpathIndex];
            int otherSubpathEndIndex = endSubpath;
            int otherRouteEndIndex = endIndex;

            bool thisEndOfRoute = false;
            bool otherEndOfRoute = false;
            bool commonSectionFound = false;

            // preset result
            int[,] sectionFound = new int[2, 2] { { -1, -1 }, { -1, -1 } };

            // derive sections
            int thisSectionIndex = thisRoute[thisRouteIndex].TCSectionIndex;
            int otherSectionIndex = otherRoute[otherRouteIndex].TCSectionIndex;

            // convert other subpath to dictionary for quick reference
            TCSubpathRoute partRoute;
            if (otherRouteIndex < otherRouteEndIndex)
            {
                partRoute = new TCSubpathRoute(otherRoute, otherRouteIndex, otherRouteEndIndex);
            }
            else
            {
                partRoute = new TCSubpathRoute(otherRoute, otherRouteEndIndex, otherRouteIndex);
            }
            Dictionary<int, int> dictRoute = partRoute.ConvertRoute();


            // loop until common section is found or route is ended
            while (!commonSectionFound && !otherEndOfRoute)
            {
                while (!thisEndOfRoute)
                {
                    // get section indices
                    thisSectionIndex = TCRoute.TCRouteSubpaths[thisSubpathIndex][thisRouteIndex].TCSectionIndex;

                    if (dictRoute.ContainsKey(thisSectionIndex))
                    {
                        int thisDirection = TCRoute.TCRouteSubpaths[thisSubpathIndex][thisRouteIndex].Direction;
                        int otherDirection = dictRoute[thisSectionIndex];

                        commonSectionFound = true;
                        sectionFound[0, 0] = thisSubpathIndex;
                        sectionFound[0, 1] = thisRouteIndex;
                        sectionFound[1, 0] = otherSubpathIndex;
                        sectionFound[1, 1] = otherRoute.GetRouteIndex(thisSectionIndex, 0);
                        break;
                    }

                    // move to next section for this train
                    thisRouteIndex++;
                    if (thisRouteIndex > thisRouteEndIndex)
                    {
                        thisEndOfRoute = true;
                    }
                }

                // move to next subpath for other train
                // move to next subpath if end of subpath reached
                if (increment > 0)
                {
                    otherSubpathIndex++;
                    if (otherSubpathIndex > otherSubpathEndIndex)
                    {
                        otherEndOfRoute = true;
                    }
                    else
                    {
                        otherRoute = otherTrainRoute.TCRouteSubpaths[otherSubpathIndex];
                        dictRoute = otherTrainRoute.TCRouteSubpaths[otherSubpathIndex].ConvertRoute();
                    }
                }
                else
                {
                    otherSubpathIndex--;
                    if (otherSubpathIndex < otherSubpathEndIndex)
                    {
                        otherEndOfRoute = true;
                    }
                    else
                    {
                        dictRoute = otherTrainRoute.TCRouteSubpaths[otherSubpathIndex].ConvertRoute();
                    }
                }

                // reset to start for this train
                thisRouteIndex = thisTrainStartRouteIndex;
                thisEndOfRoute = false;
            }

            return (sectionFound);
        }

        /// <summary>
        /// Find end of common section searching through both routes in forward direction
        /// </summary>
        /// <param name="thisRoute"></param>
        /// <param name="thisRouteIndex"></param>
        /// <param name="otherRoute"></param>
        /// <param name="otherRouteIndex"></param>
        /// <returns></returns>
        public int FindCommonSectionEnd(TCSubpathRoute thisRoute, int thisRouteIndex, TCSubpathRoute otherRoute, int otherRouteIndex)
        {
            int lastIndex = otherRouteIndex;
            int thisIndex = thisRouteIndex;
            int otherIndex = otherRouteIndex;
            int thisSectionIndex = thisRoute[thisIndex].TCSectionIndex;
            int otherSectionIndex = otherRoute[otherIndex].TCSectionIndex;

            while (thisSectionIndex == otherSectionIndex)
            {
                lastIndex = otherIndex;
                thisIndex++;
                otherIndex++;

                if (thisIndex >= thisRoute.Count || otherIndex >= otherRoute.Count)
                {
                    break;
                }
                else
                {
                    thisSectionIndex = thisRoute[thisIndex].TCSectionIndex;
                    otherSectionIndex = otherRoute[otherIndex].TCSectionIndex;
                }
            }

            return (lastIndex);
        }

        /// <summary>
        /// Find end of common section searching through own train forward but through other train backward
        /// </summary>
        /// <param name="thisRoute"></param>
        /// <param name="thisRouteIndex"></param>
        /// <param name="otherRoute"></param>
        /// <param name="otherRouteIndex"></param>
        /// <returns></returns>
        public int FindCommonSectionEndReverse(TCSubpathRoute thisRoute, int thisRouteIndex, TCSubpathRoute otherRoute, int otherRouteIndex)
        {
            int lastIndex = otherRouteIndex;
            int thisIndex = thisRouteIndex;
            int otherIndex = otherRouteIndex;
            int thisSectionIndex = thisRoute[thisIndex].TCSectionIndex;
            int otherSectionIndex = otherRoute[otherIndex].TCSectionIndex;

            while (thisSectionIndex == otherSectionIndex)
            {
                lastIndex = otherIndex;
                thisIndex++;
                otherIndex--;
                if (thisIndex >= thisRoute.Count || otherIndex < 0)
                {
                    break;
                }
                else
                {
                    thisSectionIndex = thisRoute[thisIndex].TCSectionIndex;
                    otherSectionIndex = otherRoute[otherIndex].TCSectionIndex;
                }
            }

            return (lastIndex);
        }

        //================================================================================================//
        /// <summary>
        /// Process Connect Request : process details of connect command
        /// </summary>
        /// <param name="reqWait"></param>
        /// <param name="otherTrain"></param>
        /// <param name="allowSameDirection"></param>
        /// <param name="allowOppositeDirection"></param>
        /// <param name="singleWait"></param>
        /// <param name="newWaitItems"></param>
        public void ProcessConnectRequest(WaitInfo reqWait, TTTrain otherTrain, ref List<WaitInfo> newWaitItems)
        {
            // find station reference
            StationStop stopStation = StationStops[reqWait.stationIndex];
            int otherStationStopIndex = -1;

            for (int iStation = 0; iStation <= otherTrain.StationStops.Count - 1; iStation++)
            {
                if (String.Compare(stopStation.PlatformItem.Name, otherTrain.StationStops[iStation].PlatformItem.Name) == 0)
                {
                    otherStationStopIndex = iStation;
                    break;
                }
            }

            if (otherStationStopIndex >= 0) // if other stop is found
            {
                WaitInfo newWait = reqWait.CreateCopy();
                newWait.waitTrainNumber = otherTrain.OrgAINumber;
                StationStop otherTrainStationStop = otherTrain.StationStops[otherStationStopIndex];
                otherTrainStationStop.ConnectionsWaiting.Add(Number);
                newWait.waitTrainSubpathIndex = otherTrainStationStop.SubrouteIndex;
                newWait.startSectionIndex = otherTrainStationStop.TCSectionIndex;

                newWait.activeSubrouteIndex = reqWait.startSubrouteIndex;
                newWait.activeSectionIndex = reqWait.startSectionIndex;

                stopStation.ConnectionsAwaited.Add(newWait.waitTrainNumber, -1);
                stopStation.ConnectionDetails.Add(newWait.waitTrainNumber, newWait);

                newWaitItems.Add(newWait);

#if DEBUG_TRACEINFO
                Trace.TraceInformation("Connect for train {0} : Wait at {1} (=stop {2}) for {3} (=train {4}, stop {5}), hold {6}",
                    Name, stopStation.PlatformItem.Name, newWait.stationIndex, newWait.referencedTrainName, newWait.waitTrainNumber,otherStationStopIndex,
                    newWait.holdTimeS);
#endif
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check for active wait condition for this section
        /// </summary>
        /// <param name="trackSectionIndex"></param>
        /// <returns></returns>
        public override bool CheckWaitCondition(int trackSectionIndex)
        {
            // no waits defined
            if (WaitList == null || WaitList.Count <= 0)
            {
                return (false);
            }

            bool waitState = false;

            // check if first wait is this section

            int processedWait = 0;
            WaitInfo firstWait = WaitList[processedWait];

            // if first wait is connect : no normal waits or follows to process
            if (firstWait.WaitType == WaitInfo.WaitInfoType.Connect)
            {
                return (false);
            }

            while (firstWait.activeSubrouteIndex == TCRoute.activeSubpath && firstWait.activeSectionIndex == trackSectionIndex)
            {
                switch (firstWait.WaitType)
                {
                    case WaitInfo.WaitInfoType.Wait:
                    case WaitInfo.WaitInfoType.Follow:
                        waitState = CheckForSingleTrainWait(firstWait);
                        break;

                    default:
                        break;
                }

                // if not awaited, check for further waits
                // wait list may have changed if first item is no longer valid
                if (!waitState)
                {
                    if (processedWait > WaitList.Count - 1)
                    {
                        break; // no more waits to check
                    }
                    else if (firstWait == WaitList[processedWait])  // wait was maintained
                    {
                        processedWait++;
                    }

                    if (WaitList.Count > processedWait)
                    {
                        firstWait = WaitList[processedWait];
                    }
                    else
                    {
                        break; // no more waits to check
                    }
                }
                else
                {
                    break; // no more waits to check
                }
            }

            return (waitState);
        }

        //================================================================================================//
        /// <summary>
        /// Check for active wait condition for this section
        /// </summary>
        /// <param name="trackSectionIndex"></param>
        /// <returns></returns>
        public override bool VerifyDeadlock(List<int> deadlockReferences)
        {
            bool attachTrainAhead = false;
            bool otherTrainAhead = false;
            List<int> possibleAttachTrains = new List<int>();

            if (AttachDetails != null)
            {
                if (deadlockReferences.Contains(AttachDetails.AttachTrain))
                {
                    possibleAttachTrains.Add(AttachDetails.AttachTrain);
                }
            }

            if (TransferStationDetails != null && TransferStationDetails.Count > 0)
            {
                foreach (KeyValuePair<int, TransferInfo> thisTransfer in TransferStationDetails)
                {
                    if (deadlockReferences.Contains(thisTransfer.Value.TransferTrain))
                    {
                        possibleAttachTrains.Add(thisTransfer.Value.TransferTrain);
                    }
                }
            }

            if (TransferTrainDetails != null && TransferTrainDetails.Count > 0)
            {
                foreach (KeyValuePair<int, List<TransferInfo>> thisTransferList in TransferTrainDetails)
                {
                    foreach (TransferInfo thisTransfer in thisTransferList.Value)
                    {
                        if (deadlockReferences.Contains(thisTransfer.TransferTrain))
                        {
                            possibleAttachTrains.Add(thisTransfer.TransferTrain);
                        }
                    }
                }
            }

            // check if any possible attach trains ahead in deadlock references
            if (possibleAttachTrains.Count > 0)
            {
                // test if required train is first train ahead
                int presentSectionListIndex = PresentPosition[0].RouteListIndex;

                for (int iSection = presentSectionListIndex + 1; iSection < ValidRoute[0].Count && !attachTrainAhead && !otherTrainAhead; iSection++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iSection].TCSectionIndex];
                    List<TrainRouted> occupyingTrains = thisSection.CircuitState.TrainsOccupying();
                    foreach (TrainRouted nextTrain in occupyingTrains)
                    {
                        if (possibleAttachTrains.Contains(nextTrain.Train.Number))
                        {
                            attachTrainAhead = true;
                        }
                        else
                        {
                            otherTrainAhead = true;
                        }
                    }
                }
            }

            return (!attachTrainAhead);
        }

        //================================================================================================//
        /// <summary>
        /// TrainGetSectionStateClearNode
        /// Override method from train
        /// </summary>

        public override bool TrainGetSectionStateClearNode(int elementDirection, Train.TCSubpathRoute routePart, TrackCircuitSection thisSection)
        {
            return (thisSection.getSectionState(routedForward, elementDirection, SignalObject.InternalBlockstate.Reserved, routePart, -1) <= SignalObject.InternalBlockstate.OccupiedSameDirection);
        }

        //================================================================================================//
        /// <summary>
        /// Check for actual wait condition (for single train wait - $wait, $follow or $forcewait commands
        /// </summary>
        /// <param name="reqWait"></param>
        /// <returns></returns>

        public bool CheckForSingleTrainWait(WaitInfo reqWait)
        {
            bool waitState = false;

            // get other train
            TTTrain otherTrain = GetOtherTTTrainByNumber(reqWait.waitTrainNumber);

            // get clock time - for AI use AI clock as simulator clock is not valid during pre-process
            double presentTime = Simulator.ClockTime;
            if (TrainType == TRAINTYPE.AI)
            {
                AITrain aitrain = this as AITrain;
                presentTime = aitrain.AI.clockTime;
            }

            if (reqWait.waittrigger.HasValue)
            {
                if (reqWait.waittrigger.Value < Convert.ToInt32(presentTime))
                {
                    return (waitState); // exit as wait must be retained
                }
            }

            // check if end trigger time passed
            if (reqWait.waitendtrigger.HasValue)
            {
                if (reqWait.waitendtrigger.Value < Convert.ToInt32(presentTime))
                {
                    otherTrain = null;          // reset other train to remove wait
                    reqWait.notStarted = false; // ensure wait is not triggered accidentally
                }
            }

            // check on own delay condition
            bool owndelayexceeded = true;  // default is no own delay
            if (reqWait.ownDelayS.HasValue && Delay.HasValue)
            {
                float ownDelayS = (float)Delay.Value.TotalSeconds;
                owndelayexceeded = ownDelayS > reqWait.ownDelayS.Value;
            }

            // other train does exist or wait is cancelled
            if (otherTrain != null)
            {
                // other train in correct subpath
                // check if trigger time passed

                if (otherTrain.TCRoute.activeSubpath == reqWait.waitTrainSubpathIndex)
                {
                    // check if section on train route and if so, if end of train is beyond this section
                    // check only for forward path - train must have passed section in 'normal' mode
                    if (otherTrain.ValidRoute[0] != null)
                    {
                        int waitTrainRouteIndex = otherTrain.ValidRoute[0].GetRouteIndex(reqWait.activeSectionIndex, 0);

                        if (waitTrainRouteIndex >= 0)
                        {
                            if (otherTrain.PresentPosition[1].RouteListIndex < waitTrainRouteIndex) // train is not yet passed this section
                            {
                                float? totalDelayS = null;
                                float? ownDelayS = null;

                                // determine own delay
                                if (reqWait.ownDelayS.HasValue)
                                {
                                    if (owndelayexceeded)
                                    {
                                        ownDelayS = (float)Delay.Value.TotalSeconds;

                                        if (otherTrain.Delay.HasValue)
                                        {
                                            ownDelayS -= (float)otherTrain.Delay.Value.TotalSeconds;
                                        }

                                        if (ownDelayS.Value > reqWait.ownDelayS.Value)
                                        {
                                            waitState = true;
                                            reqWait.WaitActive = true;
                                        }
                                    }
                                }

                                // determine other train delay
                                else
                                {
                                    if (reqWait.maxDelayS.HasValue && otherTrain.Delay.HasValue)
                                    {
                                        totalDelayS = reqWait.maxDelayS.Value;
                                        totalDelayS += Delay.HasValue ? (float)Delay.Value.TotalSeconds : 0f;     // add own delay if set
                                    }

                                    if (!totalDelayS.HasValue || (float)otherTrain.Delay.Value.TotalSeconds < totalDelayS)           // train is not too much delayed
                                    {
                                        waitState = true;
                                        reqWait.WaitActive = true;
                                    }
                                }
                            }
                        }
                    }
                }

                // if other train not in this subpath but notstarted is set, wait is valid (except when conditioned by own delay)
                else if (otherTrain.TCRoute.activeSubpath < reqWait.waitTrainSubpathIndex && reqWait.notStarted.HasValue && owndelayexceeded)
                {
                    waitState = true;
                    reqWait.WaitActive = true;
                }
            }

            // check if waiting is also required if train not yet started
            else if (reqWait.notStarted.HasValue && owndelayexceeded)
            {
                if (CheckTTTrainNotStartedByNumber(reqWait.waitTrainNumber))
                {
                    waitState = true;
                    reqWait.WaitActive = true;
                }
            }

            if (!waitState) // wait is no longer valid
            {
                WaitList.RemoveAt(0); // remove this wait
            }

            return (waitState);
        }

        //================================================================================================//
        /// <summary>
        /// Check for route wait state (for $anywait command)
        /// </summary>
        /// <param name="reqWait"></param>
        /// <returns></returns>
        public bool CheckForRouteWait(WaitInfo reqWait)
        {
            bool pathClear = false;

            if (reqWait.PathDirection == WaitInfo.CheckPathDirection.Same || reqWait.PathDirection == WaitInfo.CheckPathDirection.Both)
            {
                pathClear = CheckRouteWait(reqWait.CheckPath, true);
                if (!pathClear)
                {
                    reqWait.WaitActive = true;
                    return (pathClear);  // no need to check opposite direction if allready blocked
                }
            }

            if (reqWait.PathDirection == WaitInfo.CheckPathDirection.Opposite || reqWait.PathDirection == WaitInfo.CheckPathDirection.Both)
            {
                pathClear = CheckRouteWait(reqWait.CheckPath, false);
            }

            reqWait.WaitActive = !pathClear;
            return (pathClear);
        }

        //================================================================================================//
        /// <summary>
        /// Check block state for route wait request
        /// </summary>
        /// <param name="thisRoute"></param>
        /// <param name="sameDirection"></param>
        /// <returns></returns>
        private bool CheckRouteWait(TCSubpathRoute thisRoute, bool sameDirection)
        {
            SignalObject.InternalBlockstate blockstate = SignalObject.InternalBlockstate.Reserved;  // preset to lowest possible state //

            // loop through all sections in route list

            TCRouteElement lastElement = null;

            foreach (TCRouteElement thisElement in thisRoute)
            {
                lastElement = thisElement;
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                int direction = sameDirection ? thisElement.Direction : thisElement.Direction == 0 ? 1 : 0;

                blockstate = thisSection.getSectionState(routedForward, direction, blockstate, thisRoute, -1);
                if (blockstate > SignalObject.InternalBlockstate.Reservable)
                    break;     // exit on first none-available section
            }

            return (blockstate < SignalObject.InternalBlockstate.OccupiedSameDirection);
        }

        //================================================================================================//
        /// <summary>
        /// Check for any active waits in indicated path
        /// </summary>

        public override bool HasActiveWait(int startSectionIndex, int endSectionIndex)
        {
            bool returnValue = false;

            int startRouteIndex = ValidRoute[0].GetRouteIndex(startSectionIndex, PresentPosition[0].RouteListIndex);
            int endRouteIndex = ValidRoute[0].GetRouteIndex(endSectionIndex, startRouteIndex);

            if (startRouteIndex < 0 || endRouteIndex < 0)
            {
                return (returnValue);
            }

            // check for any wait in indicated route section
            for (int iRouteIndex = startRouteIndex; iRouteIndex <= endRouteIndex; iRouteIndex++)
            {
                int sectionIndex = ValidRoute[0][iRouteIndex].TCSectionIndex;
                if (WaitList != null && WaitList.Count > 0)
                {
                    if (CheckWaitCondition(sectionIndex))
                    {
                        returnValue = true;
                    }
                }

                if (WaitAnyList != null && WaitAnyList.Count > 0)
                {
                    if (WaitAnyList.ContainsKey(sectionIndex))
                    {
                        foreach (WaitInfo reqWait in WaitAnyList[sectionIndex])
                        {
                            if (CheckForRouteWait(reqWait))
                            {
                                returnValue = true;
                            }
                        }
                    }
                }
            }

            return (returnValue);
        }

        //================================================================================================//
        /// <summary>
        /// Process end of path 
        /// returns :
        /// [0] : true : end of route, false : not end of route
        /// [1] : true : train still exists, false : train is removed and no longer exists
        /// 
        /// Override from AITrain class
        /// <\summary>

        public override bool[] ProcessEndOfPath(int presentTime, bool checkLoop = true)
        {
            bool[] returnValue = new bool[2] { false, true };

            // if train not on route in can't be at end
            if (PresentPosition[0].RouteListIndex < 0)
            {
                return (returnValue);
            }

            int directionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].Direction;
            int positionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].TCSectionIndex;

            bool[] nextPart = UpdateRouteActions(0, checkLoop);

            if (!nextPart[0])
            {
                return (returnValue);   // not at end and not to attach to anything
            }

            returnValue[0] = true; // end of path reached
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
                    ReverseFormation(TrainType == TRAINTYPE.PLAYER);

#if DEBUG_REPORTS
                    File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                         Number.ToString() + " reversed\n");
#endif
                }
                else if (positionNow == PresentPosition[1].TCSectionIndex && directionNow != PresentPosition[1].TCDirection)
                {
                    ReverseFormation(TrainType == TRAINTYPE.PLAYER);

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
                    // if station was in previous path, checked if passed
                    else if (thisStation.SubrouteIndex < TCRoute.activeSubpath)
                    {
                        int routeIndex = ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, 0);
                        if (routeIndex < 0 || PresentPosition[0].RouteListIndex > routeIndex) // station no longer on route or train beyond station
                        {
                            if (thisStation.ExitSignal >= 0 && thisStation.HoldSignal && HoldingSignals.Contains(thisStation.ExitSignal))
                            {
                                HoldingSignals.Remove(thisStation.ExitSignal);
                            }
                            StationStops.RemoveAt(0);
                        }
                        // if in station set correct subroute and route indices
                        else if (PresentPosition[0].RouteListIndex == routeIndex)
                        {
                            thisStation.SubrouteIndex = TCRoute.activeSubpath;
                            thisStation.RouteIndex = routeIndex;

                            AtStation = true;
                            MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                        }
                    }
                }

                // reset to node control, also reset required actions

                SwitchToNodeControl(-1);
                ResetActions(true);
            }
            else
            {
                ProcessEndOfPathReached(ref returnValue, presentTime);
            }

            return (returnValue);
        }

        //================================================================================================//
        /// <summary>
        /// Process actions when end of path is reached
        /// Override from AITrain class
        /// </summary>
        /// <param name="returnValue"></param>
        /// <param name="presentTime"></param>
        public override void ProcessEndOfPathReached(ref bool[] returnValue, int presentTime)
        {
            // check if any other train needs to be activated
            ActivateTriggeredTrain(TriggerActivationType.Dispose, -1);

            // check if any outstanding moving table actions
            List<DistanceTravelledItem> reqActions = requiredActions.GetActions(0.0f, typeof(ClearMovingTableAction));
            foreach (DistanceTravelledItem thisAction in reqActions)
            {
                ClearMovingTable();
            }

            // check if train is to form new train
            // note : if formed train == 0, formed train is player train which requires different actions

            if (Forms >= 0 && DetachActive[1] == -1)
            {
                // check if anything needs be detached
                bool allowForm = true; // preset form may be activated

                if (DetachDetails.ContainsKey(-1))
                {
                    List<DetachInfo> detachList = DetachDetails[-1];

                    for (int iDetach = detachList.Count - 1; iDetach >= 0; iDetach--)
                    {
                        DetachInfo thisDetach = detachList[iDetach];
                        if (thisDetach.DetachPosition == DetachInfo.DetachPositionInfo.atEnd && thisDetach.Valid)
                        {
                            DetachActive[0] = -1;
                            DetachActive[1] = iDetach;
                            allowForm = thisDetach.PerformDetach(this, true);
                            thisDetach.Valid = false;
                        }
                    }
                    if (detachList.Count <= 0 & allowForm) DetachDetails.Remove(-1);
                }

                // if detach was performed, form may proceed
                if (allowForm)
                {
                    FormTrainFromAI(presentTime);
                    returnValue[1] = false;
                }
            }

            // check if train is to remain as static
            else if (FormsStatic)
            {
                // check if anything needs be detached
                if (DetachDetails.ContainsKey(-1))
                {
                    List<DetachInfo> detachList = DetachDetails[-1];

                    for (int iDetach = detachList.Count - 1; iDetach >= 0; iDetach--)
                    {
                        DetachInfo thisDetach = detachList[iDetach];
                        if (thisDetach.DetachPosition == DetachInfo.DetachPositionInfo.atEnd && thisDetach.Valid)
                        {
                            DetachActive[0] = -1;
                            DetachActive[1] = iDetach;
                            thisDetach.PerformDetach(this, true);
                            thisDetach.Valid = false;
                        }
                    }
                    if (detachList.Count <= 0) DetachDetails.Remove(-1);
                }

                MovementState = AI_MOVEMENT_STATE.AI_STATIC;
                ControlMode = TRAIN_CONTROL.INACTIVE;
                StartTime = null;  // set starttime to invalid
                ActivateTime = null;  // set activate to invalid

                // remove existing train from track
                TrackCircuitSection[] occupiedSections = new TrackCircuitSection[OccupiedTrack.Count];
                OccupiedTrack.CopyTo(occupiedSections);
                RemoveFromTrack();
                ClearDeadlocks();

                foreach (TrackCircuitSection occSection in occupiedSections)
                {
                    occSection.SetOccupied(routedForward);
                }

                // train is in pool : update pool info
                if (!String.IsNullOrEmpty(ExitPool))
                {
                    TimetablePool thisPool = Simulator.PoolHolder.Pools[ExitPool];

                    if (thisPool.StoragePool[PoolStorageIndex].StoredUnits.Contains(Number) && !thisPool.StoragePool[PoolStorageIndex].ClaimUnits.Contains(Number))
                    {
                        Trace.TraceWarning("Pool {0} : train : {1} ({2}) : adding train allready in pool \n", thisPool.PoolName, Name, Number);
                    }
                    else
                    {
                        thisPool.AddUnit(this, false);
                        Update(0);
                    }
                }
            }

#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                     Number.ToString() + " removed\n");
#endif
            else if (AttachDetails != null && AttachDetails.Valid)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                         Number.ToString() + " waiting to attach to " + AttachDetails.AttachTrainName + "\n");
                }
            }
            else
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Train " +
                         Number.ToString() + " removed\n");
                }
                RemoveTrain();
                returnValue[1] = false;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Form train from existing AI train (i.e. not from player train)
        /// </summary>
        /// <param name="presentTime"></param>
        public void FormTrainFromAI(int presentTime)
        {
            TTTrain formedTrain = null;
            bool autogenStart = false;

            if (Forms == 0)
            {
                formedTrain = Simulator.Trains[0] as TTTrain; // get player train
                formedTrain.TrainType = TRAINTYPE.INTENDED_PLAYER;
            }
            else
            {
                // get train which is to be formed
                formedTrain = AI.StartList.GetNotStartedTTTrainByNumber(Forms, true);

                if (formedTrain == null)
                {
                    formedTrain = Simulator.GetAutoGenTTTrainByNumber(Forms);
                    autogenStart = true;
                }
            }

            // if found - start train
            if (formedTrain != null)
            {
                // remove existing train
                TrackCircuitSection[] occupiedSections = new TrackCircuitSection[OccupiedTrack.Count];
                OccupiedTrack.CopyTo(occupiedSections);

                Forms = -1;
                RemoveTrain();

                // set details for new train from existing train
                bool validFormed = formedTrain.StartFromAITrain(this, presentTime, occupiedSections);

#if DEBUG_TRACEINFO
                        Trace.TraceInformation("{0} formed into {1}", Name, formedTrain.Name);
#endif

                if (validFormed)
                {
                    // start new train
                    if (!autogenStart)
                    {
                        AI.Simulator.StartReference.Remove(formedTrain.Number);
                    }

                    if (formedTrain.TrainType == TRAINTYPE.INTENDED_PLAYER)
                    {
                        AI.TrainsToAdd.Add(formedTrain);

                        // set player locomotive
                        // first test first and last cars - if either is drivable, use it as player locomotive
                        int lastIndex = formedTrain.Cars.Count - 1;

                        if (formedTrain.Cars[0].IsDriveable)
                        {
                            AI.Simulator.PlayerLocomotive = formedTrain.LeadLocomotive = formedTrain.Cars[0];
                        }
                        else if (formedTrain.Cars[lastIndex].IsDriveable)
                        {
                            AI.Simulator.PlayerLocomotive = formedTrain.LeadLocomotive = formedTrain.Cars[lastIndex];
                        }
                        else
                        {
                            foreach (TrainCar car in formedTrain.Cars)
                            {
                                if (car.IsDriveable)  // first loco is the one the player drives
                                {
                                    AI.Simulator.PlayerLocomotive = formedTrain.LeadLocomotive = car;
                                    break;
                                }
                            }
                        }

                        // only initialize brakes if previous train was not player train
                        if (TrainType == TRAINTYPE.PLAYER)
                        {
                            formedTrain.ConnectBrakeHoses();
                        }
                        else
                        {
                            formedTrain.InitializeBrakes();
                        }

                        if (AI.Simulator.PlayerLocomotive == null && (formedTrain.NeedAttach == null || formedTrain.NeedAttach.Count <= 0))
                        {
                            throw new InvalidDataException("Can't find player locomotive in " + formedTrain.Name);
                        }
                        else
                        {
                            foreach (TrainCar car in formedTrain.Cars)
                            {
                                if (car.WagonType == TrainCar.WagonTypes.Engine)
                                {
                                    MSTSLocomotive loco = car as MSTSLocomotive;
                                    loco.AntiSlip = formedTrain.leadLocoAntiSlip;
                                }
                            }
                        }
                    }
                    else
                    {
                        formedTrain.TrainType = Train.TRAINTYPE.AI;
                        AI.TrainsToAdd.Add(formedTrain);
                    }

                    formedTrain.MovementState = AI_MOVEMENT_STATE.AI_STATIC;
                    formedTrain.SetFormedOccupied();

                    if (MovementState == AI_MOVEMENT_STATE.STATION_STOP && formedTrain.StationStops != null && formedTrain.StationStops.Count > 0)
                    {
                        if (StationStops[0].PlatformReference == formedTrain.StationStops[0].PlatformReference)
                        {
                            formedTrain.AtStation = true;
                            formedTrain.StationStops[0].ActualArrival = StationStops[0].ActualArrival;
                            formedTrain.StationStops[0].arrivalDT = StationStops[0].arrivalDT;
                            formedTrain.StationStops[0].ArrivalTime = StationStops[0].ArrivalTime;
                            formedTrain.StationStops[0].CalculateDepartTime(presentTime, this);
                        }
                    }
                }
                else if (!autogenStart)
                {
                    // reinstate as to be started (note : train is not yet removed from reference)
                    AI.StartList.InsertTrain(formedTrain);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check End of Route Position
        /// Override class from Train, but needs additional checks before actually testing end of route
        /// </summary>

        public override bool CheckEndOfRoutePosition()
        {
            // check if at end of route
            bool endOfRoute = CheckEndOfRoutePositionTT();

            // if so, perform further checks
            if (endOfRoute)
            {
                // if train needs to pick up before reaching end of path, continue train
                if (PickUpStaticOnForms && TCRoute.activeSubpath == (TCRoute.TCRouteSubpaths.Count - 1))
                {
                    return (false);
                }

                // if present or any further sections are platform section and pick up is required in platform, continue train
                // if present or any further sections occupied by train which must be picked up, continue train
                for (int iRouteIndex = PresentPosition[0].RouteListIndex; iRouteIndex < ValidRoute[0].Count; iRouteIndex++)
                {
                    // check platform
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iRouteIndex].TCSectionIndex];
                    foreach (int thisPlatform in thisSection.PlatformIndex)
                    {
                        foreach (int platformReference in signalRef.PlatformDetailsList[thisPlatform].PlatformReference)
                        {
                            if (PickUpStatic.Contains(platformReference))
                            {
                                return (false);
                            }
                        }
                    }


                    // check occupying trains
                    List<TrainRouted> otherTrains = thisSection.CircuitState.TrainsOccupying();

                    foreach (TrainRouted otherTrain in otherTrains)
                    {
                        TTTrain otherTTTrain = otherTrain.Train as TTTrain;
                        // check for pickup
                        if (CheckPickUp(otherTTTrain))
                        {
                            return (false);
                        }
                        else if (TransferTrainDetails.ContainsKey(otherTTTrain.OrgAINumber))
                        {
                            return (false);
                        }
                        else if (AttachDetails != null && AttachDetails.Valid && AttachDetails.AttachTrain == otherTTTrain.OrgAINumber)
                        {
                            return (false);
                        }
                    }
                }
            }

            return (endOfRoute);
        }

        //================================================================================================//
        /// <summary>
        /// Check End of Route Position
        /// </summary>

        public bool CheckEndOfRoutePositionTT()
        {
            if (CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Check end of route (TT) ; MovementState : " + MovementState + "\n");
            }

            bool endOfRoute = false;

            // only allowed when stopped
            if (Math.Abs(SpeedMpS) > 0.05f)
            {
                return (endOfRoute);
            }

            // if access to pool is required and section is in present route, train can never be at end of route

            if (PoolAccessSection >= 0)
            {
                int poolAccessRouteIndex = ValidRoute[0].GetRouteIndex(PoolAccessSection, 0);
                if (poolAccessRouteIndex >= 0)
                {
                    if (CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Pool Access required ; poolAccessRouteIndex : " + poolAccessRouteIndex + "\n");
                    }
                    return (endOfRoute);
                }
            }

            // if not stopped in station and next stop in this subpath, it cannot be end of route
            if (!AtStation && StationStops.Count > 0 && StationStops[0].SubrouteIndex == TCRoute.activeSubpath)
            {
                return (endOfRoute);
            }

            // if stopped at station and next stop in this subpath, it cannot be end of route
            if (AtStation && StationStops.Count > 1 && StationStops[1].SubrouteIndex == TCRoute.activeSubpath)
            {
                return (endOfRoute);
            }

            // if stopped in last section of route and this section is exit to moving table switch to moving table mode
            if (ValidRoute[0][PresentPosition[0].RouteListIndex].MovingTableApproachPath > -1)
            {
                if (AI.Simulator.PoolHolder.Pools.ContainsKey(ExitPool))
                {
                    TimetablePool thisPool = AI.Simulator.PoolHolder.Pools[ExitPool];
                    if (thisPool.GetType() == typeof(TimetableTurntablePool))
                    {
                        TimetableTurntablePool thisTurntablePool = thisPool as TimetableTurntablePool;
                        ActiveTurntable = new TimetableTurntableControl(thisTurntablePool, thisTurntablePool.PoolName, thisTurntablePool.AdditionalTurntableDetails.TurntableIndex,
                            AI.Simulator, this);
                        ActiveTurntable.MovingTableState = TimetableTurntableControl.MovingTableStateEnum.WaitingMovingTableAvailability;
                        ActiveTurntable.MovingTableAction = TimetableTurntableControl.MovingTableActionEnum.FromAccess;
                        MovementState = AI_MOVEMENT_STATE.TURNTABLE;
                        if (CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt", "Moving table access ; Movement State : " + MovementState + "\n");
                        }
                        return (endOfRoute);
                    }
                }
            }

            // obtain reversal section index
            int reversalSectionIndex = -1;
            if (TCRoute != null && (ControlMode == TRAIN_CONTROL.AUTO_NODE || ControlMode == TRAIN_CONTROL.AUTO_SIGNAL))
            {
                TCReversalInfo thisReversal = TCRoute.ReversalInfo[TCRoute.activeSubpath];
                if (thisReversal.Valid)
                {
                    reversalSectionIndex = (thisReversal.SignalUsed && !ForceReversal) ? thisReversal.LastSignalIndex : thisReversal.LastDivergeIndex;
                }
            }

            // if last entry in route is END_OF_TRACK, check against previous entry as this can never be the trains position nor a signal reference section
            int lastValidRouteIndex = ValidRoute[0].Count - 1;
            if (signalRef.TrackCircuitList[ValidRoute[0][lastValidRouteIndex].TCSectionIndex].CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
                lastValidRouteIndex--;

            // train authority is end of path
            if (ControlMode == TRAIN_CONTROL.AUTO_NODE &&
                (EndAuthorityType[0] == END_AUTHORITY.END_OF_TRACK || EndAuthorityType[0] == END_AUTHORITY.END_OF_PATH || EndAuthorityType[0] == END_AUTHORITY.END_OF_AUTHORITY))
            {
                // front is in last route section
                if (PresentPosition[0].RouteListIndex == lastValidRouteIndex)
                {
                    endOfRoute = true;
                }
                // front is within 150m. of end of route and no junctions inbetween (only very short sections ahead of train)
                else
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex];
                    float lengthToGo = thisSection.Length - PresentPosition[0].TCOffset;

                    bool junctionFound = false;
                    for (int iIndex = PresentPosition[0].RouteListIndex + 1; iIndex <= lastValidRouteIndex && !junctionFound; iIndex++)
                    {
                        thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                        junctionFound = thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction;
                        lengthToGo += thisSection.Length;
                    }

                    if (lengthToGo < endOfRouteDistance && !junctionFound)
                    {
                        endOfRoute = true;
                    }
                }
            }


            // other checks unrelated to state
            if (!endOfRoute)
            {
                // if train is in station and endstop is set
                if (AtStation)
                {
                    endOfRoute = StationStops[0].EndStop;
                }
            }

            // if end of train on last section in route - end of route reached
            if (!endOfRoute)
            {
                if (PresentPosition[1].RouteListIndex == lastValidRouteIndex)
                {
                    endOfRoute = true;
                }
                // if length of last section is less than train length, check if front position is on last section
                else
                {
                    TrackCircuitSection lastSection = signalRef.TrackCircuitList[ValidRoute[0][lastValidRouteIndex].TCSectionIndex];
                    if (lastSection.Length < Length && PresentPosition[0].RouteListIndex == lastValidRouteIndex)
                    {
                        endOfRoute = true;
                    }
                }
            }

            // if in last station and station is on end of route
            if (!endOfRoute)
            {
                if (MovementState == AI_MOVEMENT_STATE.STATION_STOP && StationStops.Count == 1)
                {
                    StationStop presentStation = StationStops[0];
                    int stationRouteIndex = -1;

                    // check all platform sections
                    foreach (int sectionIndex in presentStation.PlatformItem.TCSectionIndex)
                    {
                        stationRouteIndex = ValidRoute[0].GetRouteIndex(sectionIndex, PresentPosition[1].RouteListIndex);
                        if (stationRouteIndex > 0)
                        {
                            break;
                        }
                    }

                    if (stationRouteIndex < 0 || stationRouteIndex == lastValidRouteIndex)
                    {
                        endOfRoute = true;
                    }

                    // test length of track beyond station
                    else
                    {
                        float remainLength = 0;
                        for (int Index = stationRouteIndex; Index <= lastValidRouteIndex; Index++)
                        {
                            remainLength += signalRef.TrackCircuitList[ValidRoute[0][Index].TCSectionIndex].Length;
                            if (remainLength > 2 * Length) break;
                        }

                        if (remainLength < Length)
                        {
                            endOfRoute = true;
                        }
                    }
                }
            }

            // if waiting for next signal and section in front of signal is last in route - end of route reached
            // if stopped at station and NoWaitSignal is set, train cannot be waiting for a signal
            if (!endOfRoute && !(AtStation && StationStops[0].NoWaitSignal))
            {
                if (NextSignalObject[0] != null && PresentPosition[0].TCSectionIndex == NextSignalObject[0].TCReference &&
                     NextSignalObject[0].TCReference == ValidRoute[0][lastValidRouteIndex].TCSectionIndex)
                {
                    endOfRoute = true;
                }
                if (NextSignalObject[0] != null && ControlMode == TRAIN_CONTROL.AUTO_SIGNAL && CheckTrainWaitingForSignal(NextSignalObject[0], 0) &&
                 NextSignalObject[0].TCReference == ValidRoute[0][lastValidRouteIndex].TCSectionIndex)
                {
                    endOfRoute = true;
                }
            }

            // if waiting for next signal and section beyond signal is last in route and there is no valid reversal index - end of route reached
            // if stopped at station and NoWaitSignal is set, train cannot be waiting for a signal
            if (!endOfRoute && !(AtStation && StationStops[0].NoWaitSignal))
            {
                if (NextSignalObject[0] != null && PresentPosition[0].TCSectionIndex == NextSignalObject[0].TCReference &&
                     NextSignalObject[0].TCNextTC == ValidRoute[0][lastValidRouteIndex].TCSectionIndex && reversalSectionIndex < 0)
                {
                    endOfRoute = true;
                }
            }

            // if remaining section length is less than safety distance
            if (!endOfRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][PresentPosition[0].RouteListIndex].TCSectionIndex];
                int direction = ValidRoute[0][PresentPosition[0].RouteListIndex].Direction;
                float remLength = (thisSection.Length - PresentPosition[0].TCOffset);

                for (int Index = PresentPosition[0].RouteListIndex + 1; Index <= lastValidRouteIndex && (remLength < 2 * standardOverlapM); Index++)
                {
                    remLength += signalRef.TrackCircuitList[ValidRoute[0][Index].TCSectionIndex].Length;
                }

                if (remLength < 2 * standardOverlapM)
                {
                    endOfRoute = true;
                }
            }

            // if next action is end of route and remaining distance is less than safety distance and no junction ahead of rear of train
            if (!endOfRoute)
            {
                bool junctionFound = false;

                for (int Index = PresentPosition[1].RouteListIndex + 1; Index <= lastValidRouteIndex && !junctionFound; Index++)
                {
                    junctionFound = signalRef.TrackCircuitList[ValidRoute[0][Index].TCSectionIndex].CircuitType == TrackCircuitSection.TrackCircuitType.Junction;
                }

                if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE && !junctionFound)
                {
                    float remDistance = nextActionInfo.ActivateDistanceM - DistanceTravelledM;
                    if (remDistance < 2 * standardOverlapM)
                    {
                        endOfRoute = true;
                    }
                }
            }

            // if rear of train is beyond reversal section
            if (!endOfRoute)
            {
                if (reversalSectionIndex >= 0 && PresentPosition[1].RouteListIndex >= reversalSectionIndex)
                {
                    // if there is a station ahead, this is not end of route
                    if (MovementState != AI_MOVEMENT_STATE.STATION_STOP && StationStops != null && StationStops.Count > 0 &&
                        StationStops[0].SubrouteIndex == TCRoute.activeSubpath)
                    {
                        endOfRoute = false;
                    }
                    else
                    {
                        endOfRoute = true;
                    }
                }
            }

            // if remaining length less then train length and no junctions to end of route - end of route reached
            // if no junctions or signals to end of route - end of route reached
            if (!endOfRoute)
            {
                bool intermediateJunction = false;
                bool intermediateSignal = false;
                float length = 0f;
                float distanceToNextJunction = -1f;
                float distanceToNextSignal = -1f;

                if (PresentPosition[1].RouteListIndex >= 0) // end of train is on route
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][PresentPosition[1].RouteListIndex].TCSectionIndex];
                    int direction = ValidRoute[0][PresentPosition[1].RouteListIndex].Direction;
                    length = (thisSection.Length - PresentPosition[1].TCOffset);
                    if (thisSection.EndSignals[direction] != null)                         // check for signal only in direction of train (other signal is behind train)
                    {
                        intermediateSignal = true;
                        distanceToNextSignal = length; // distance is total length
                    }

                    if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction || thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
                    {
                        intermediateJunction = true;
                        distanceToNextJunction = 0f;
                    }

                    for (int iIndex = PresentPosition[1].RouteListIndex + 1; iIndex >= 0 && iIndex <= lastValidRouteIndex; iIndex++)
                    {
                        thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                        length += thisSection.Length;

                        if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction ||
                            thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
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
                    // however, do accept signal or junction if train is still in first section
                    float frontlength = length;
                    if (intermediateJunction)
                    {
                        if ((frontlength - distanceToNextJunction) < Length && PresentPosition[0].RouteListIndex > 0) intermediateJunction = false;
                    }

                    if (intermediateSignal)
                    {
                        if ((frontlength - distanceToNextSignal) < Length && PresentPosition[0].RouteListIndex > 0) intermediateSignal = false;
                    }
                }
                else if (PresentPosition[0].RouteListIndex >= 0) // else use front position - check for further signals or junctions only
                {
                    for (int iIndex = PresentPosition[0].RouteListIndex; iIndex >= 0 && iIndex <= lastValidRouteIndex; iIndex++)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                        int direction = ValidRoute[0][iIndex].Direction;

                        if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction ||
                            thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
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

                if (!intermediateJunction && !intermediateSignal && (StationStops == null || StationStops.Count < 1))  // no more junctions and no more signal and no more stations - reverse subpath
                {
                    endOfRoute = true;
                }

                // check if there is a train ahead, and that train is stopped at the end of our route - if so, we can't go any further

                if (!endOfRoute)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[PresentPosition[0].TCSectionIndex];
                    Dictionary<Train, float> trainInfo = thisSection.TestTrainAhead(this, PresentPosition[0].TCOffset, PresentPosition[0].TCDirection);

                    if (trainInfo.Count > 0)
                    {
                        foreach (KeyValuePair<Train, float> trainAhead in trainInfo)
                        {
                            TTTrain otherTrain = trainAhead.Key as TTTrain;
                            if (Math.Abs(otherTrain.SpeedMpS) < 0.1f) // other train must be stopped
                            {
                                if (otherTrain.PresentPosition[0].TCSectionIndex == ValidRoute[0][lastValidRouteIndex].TCSectionIndex)
                                {
                                    endOfRoute = true;
                                }
                                else if (otherTrain.PresentPosition[1].TCSectionIndex == ValidRoute[0][lastValidRouteIndex].TCSectionIndex)
                                {
                                    endOfRoute = true;
                                }
                            }
                        }
                    }
                }
            }

            // return state

            // never return end of route if train has not moved
            if (endOfRoute && DistanceTravelledM < 0.1) endOfRoute = false;

            return (endOfRoute);
        }

        //================================================================================================//
        /// <summary>
        /// Remove train
        /// Override from Train class
        /// <\summary>

        public override void RemoveTrain()
        {
            RemoveFromTrack();
            ClearDeadlocks();

            // if train was to form another train, ensure this other train is started by removing the formed link
            if (Forms >= 0)
            {
                TTTrain formedTrain = AI.StartList.GetNotStartedTTTrainByNumber(Forms, true);
                if (formedTrain != null)
                {
                    formedTrain.FormedOf = -1;
                    formedTrain.FormedOfType = FormCommand.None;
                }
            }

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
            // remove train

            AI.TrainsToRemove.Add(this);
        }

        //================================================================================================//
        /// <summary>
        /// Add movement status to train status string
        /// Used to build movement state information in dispatcher HUD info
        /// Override from AITrain class
        /// <\summary>

        public override String[] AddMovementState(String[] stateString, bool metric)
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
            }

            string abString = AITrainThrottlePercent.ToString("000");
            abString = String.Concat(abString, "&", AITrainBrakePercent.ToString("000"));

            // if station stop : show departure time
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
            else if (MovementState == AI_MOVEMENT_STATE.AI_STATIC)
            {
                if (TriggeredActivationRequired)
                {
                    abString = "TrigAct ";
                }
                else if (ActivateTime.HasValue)
                {
                    long startNSec = (long)(ActivateTime.Value * Math.Pow(10, 7));
                    DateTime startDT = new DateTime(startNSec);
                    abString = startDT.ToString("HH:mm:ss");
                }
                else
                {
                    abString = "--------";
                }
            }

            string nameString = Name.Substring(0, Math.Min(Name.Length, 6));

            string actString = "";

            if (MovementState != AI_MOVEMENT_STATE.AI_STATIC && nextActionInfo != null)
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

        //================================================================================================//
        /// <summary>
        /// Add reversal info to TrackMonitorInfo
        /// Override from Train class
        /// </summary>

        public override void AddTrainReversalInfo(TCReversalInfo thisReversal, ref TrainInfo thisInfo)
        {
            if (!thisReversal.Valid) return;

            int reversalSection = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][(TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Count) - 1].TCSectionIndex;
            if (thisReversal.LastDivergeIndex >= 0)
            {
                reversalSection = thisReversal.SignalUsed ? thisReversal.SignalSectorIndex : thisReversal.DivergeSectorIndex;
            }

            TrackCircuitSection rearSection = signalRef.TrackCircuitList[PresentPosition[1].TCSectionIndex];
            float reversalDistanceM = rearSection.GetDistanceBetweenObjects(PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset, PresentPosition[1].TCDirection,
            reversalSection, 0.0f);

            bool reversalEnabled = true;
            if (reversalDistanceM > 0)
            {
                TrainObjectItem nextItem = new TrainObjectItem(reversalEnabled, reversalDistanceM, true);
                thisInfo.ObjectInfoForward.Add(nextItem);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check for end of route actions - for PLAYER train only
        /// Reverse train if required
        /// Return parameter : true if train still exists (used only for player train)
        /// Override from Train class
        /// </summary>
        public override bool CheckRouteActions(float elapsedClockSeconds)
        {
            int directionNow = PresentPosition[0].TCDirection;
            int positionNow = PresentPosition[0].TCSectionIndex;

            if (PresentPosition[0].RouteListIndex >= 0) directionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].Direction;

            // check if at station
            CheckStationTask();
            if (DetachPending) return (true);  // do not check for further actions if player train detach is pending

            bool[] nextRoute = UpdateRouteActions(elapsedClockSeconds);
            if (!nextRoute[0]) return (true);  // not at end of route

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

                // check if next station was on previous subpath - if so, move to this subpath

                if (StationStops.Count > 0)
                {
                    StationStop thisStation = StationStops[0];
                    if (thisStation.SubrouteIndex < TCRoute.activeSubpath)
                    {
                        thisStation.SubrouteIndex = TCRoute.activeSubpath;
                    }
                }
            }

            //process train end for player if stopped
            else
            {
                if (Math.Abs(SpeedMpS) < 0.05f)
                {
                    SpeedMpS = 0.0f;
                    return (ProcessRouteEndTimetablePlayer());
                }
            }

            // return train still exists

            return (true);
        }

        //================================================================================================//
        /// <summary>
        /// compute boarding time for timetable mode
        /// also check validity of depart time value
        /// Override from Train class
        /// <\summary>

        public override bool ComputeTrainBoardingTime(StationStop thisStop, ref int stopTime)
        {
            // use minimun station dwell time
            if (stopTime <= 0 && thisStop.ActualMinStopTime.HasValue)
            {
                stopTime = thisStop.ActualMinStopTime.Value;
            }
            else if (stopTime <= 0)
            {
                stopTime = (int)thisStop.PlatformItem.MinWaitingTime;
            }
            else if (thisStop.ActualArrival > thisStop.ArrivalTime && stopTime > thisStop.PlatformItem.MinWaitingTime)
            {
                stopTime = (int)thisStop.PlatformItem.MinWaitingTime;
            }

            return (true);
        }

        //================================================================================================//
        /// <summary>
        /// setup station stop handling for player train
        /// </summary>

        public void SetupStationStopHandling()
        {
            CheckStations = true;  // set station stops to be handled by train

            // check if initial at station
            if (StationStops.Count > 0)
            {
                int frontIndex = PresentPosition[0].RouteListIndex;
                int rearIndex = PresentPosition[1].RouteListIndex;
                List<int> occupiedSections = new List<int>();

                int startIndex = frontIndex < rearIndex ? frontIndex : rearIndex;
                int stopIndex = frontIndex < rearIndex ? rearIndex : frontIndex;

                for (int iIndex = startIndex; iIndex <= stopIndex; iIndex++)
                {
                    occupiedSections.Add(ValidRoute[0][iIndex].TCSectionIndex);
                }

                foreach (int sectionIndex in StationStops[0].PlatformItem.TCSectionIndex)
                {
                    if (occupiedSections.Contains(sectionIndex))
                    {
                        AtStation = true;
                        int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
                        if (StationStops[0].ActualArrival < 0)
                        {
                            StationStops[0].ActualArrival = presentTime;
                            StationStops[0].CalculateDepartTime(presentTime, this);
                        }
                        break;
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check on station tasks for player train
        /// Override from Train class, to allow call from common methods
        /// </summary>
        public override void CheckStationTask()
        {
            // if at station
            if (AtStation)
            {
                // check for activation of other train
                ActivateTriggeredTrain(TriggerActivationType.StationStop, StationStops[0].PlatformReference);

                // get time
                int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
                int eightHundredHours = 8 * 3600;
                int sixteenHundredHours = 16 * 3600;

                // if moving, set departed
                if (Math.Abs(SpeedMpS) > 1.0f)
                {
                    StationStops[0].ActualDepart = presentTime;
                    StationStops[0].Passed = true;
                    AtStation = false;
                    MayDepart = false;
                    DisplayMessage = "";
                    Delay = TimeSpan.FromSeconds((presentTime - StationStops[0].DepartTime) % (24 * 3600));

                    // check for activation of other train
                    ActivateTriggeredTrain(TriggerActivationType.StationDepart, StationStops[0].PlatformReference);

                    // remove stop
                    PreviousStop = StationStops[0].CreateCopy();
                    StationStops.RemoveAt(0);
                }
                else
                {
                    // check for detach
                    if (DetachDetails.ContainsKey(StationStops[0].PlatformReference))
                    {
                        List<DetachInfo> detachList = DetachDetails[StationStops[0].PlatformReference];
                        bool detachPerformed = DetachActive[1] < 0;

                        for (int iDetach = 0; iDetach < detachList.Count; iDetach++)
                        {
                            DetachInfo thisDetach = detachList[iDetach];
                            if (thisDetach.Valid)
                            {
                                DetachActive[0] = StationStops[0].PlatformReference;
                                DetachActive[1] = iDetach;
                                detachPerformed = thisDetach.PerformDetach(this, true);
                                thisDetach.Valid = false;
                            }
                        }
                        if (detachPerformed)
                        {
                            DetachDetails.Remove(StationStops[0].PlatformReference);
                        }
                    }

                    // check for connection
                    int helddepart = -1;
                    int needwait = -1;

                    // keep trying to set connections as train may be created during stop
                    if (StationStops[0].ConnectionsWaiting.Count > 0)
                    {
                        foreach (int otherTrainNumber in StationStops[0].ConnectionsWaiting)
                        {
                            TTTrain otherTrain = GetOtherTTTrainByNumber(otherTrainNumber);
                            if (otherTrain != null)
                            {
                                foreach (StationStop otherStop in otherTrain.StationStops)
                                {
                                    if (String.Compare(StationStops[0].PlatformItem.Name, otherStop.PlatformItem.Name) == 0 && otherStop.ConnectionsAwaited.ContainsKey(OrgAINumber))
                                    {
                                        otherStop.ConnectionsAwaited.Remove(OrgAINumber);
                                        otherStop.ConnectionsAwaited.Add(OrgAINumber, StationStops[0].ActualArrival);
                                    }
                                }
                            }
                        }
                    }

                    // check if waiting for connection
                    if (StationStops[0].ConnectionsAwaited.Count > 0)
                    {
                        needwait = ProcessConnections(StationStops[0], out helddepart);
                    }

                    // check for attachments
                    int waitAttach = -1;

                    if (NeedAttach.ContainsKey(StationStops[0].PlatformReference))
                    {
                        List<int> needAttachList = NeedAttach[StationStops[0].PlatformReference];
                        if (needAttachList.Count > 0)
                        {
                            waitAttach = needAttachList[0];
                        }
                    }

                    int waitTransfer = -1;
                    if (NeedStationTransfer.ContainsKey(StationStops[0].PlatformReference))
                    {
                        List<int> needTransferList = NeedStationTransfer[StationStops[0].PlatformReference];
                        if (needTransferList.Count > 0)
                        {
                            waitTransfer = needTransferList[0];
                        }
                    }

                    // check if attaching
                    int waitArrivalAttach = -1;
                    bool readyToAttach = false;
                    bool attaching = false;
                    TTTrain attachTrain = null;

                    if (AttachDetails != null && AttachDetails.StationPlatformReference == StationStops[0].PlatformReference && AttachDetails.FirstIn)
                    {
                        attachTrain = GetOtherTTTrainByNumber(AttachDetails.AttachTrain);
                        if (attachTrain != null)
                        {
                            waitArrivalAttach = AttachDetails.AttachTrain;
                            if (attachTrain.MovementState == AI_MOVEMENT_STATE.STATION_STOP && attachTrain.StationStops[0].PlatformReference == StationStops[0].PlatformReference)
                            {
                                // attach not already taking place
                                if (AttachDetails.ReadyToAttach)
                                {
                                    attaching = true;
                                }
                                else
                                {
                                    readyToAttach = AttachDetails.ReadyToAttach = true;
                                }
                            }
                        }
                    }

                    // set message
                    int remaining = 999;

                    if (needwait >= 0)
                    {
                        TTTrain otherTrain = GetOtherTTTrainByNumber(needwait);
                        DisplayMessage = Simulator.Catalog.GetString("Held for connecting train : ");
                        DisplayMessage = String.Concat(DisplayMessage, otherTrain.Name);
                        DisplayColor = Color.Orange;
                        remaining = 999;
                    }
                    else if (waitAttach >= 0)
                    {
                        TTTrain otherTrain = GetOtherTTTrainByNumber(waitAttach);
                        DisplayMessage = Simulator.Catalog.GetString("Waiting for train to attach : ");
                        if (otherTrain != null)
                        {
                            DisplayMessage = String.Concat(DisplayMessage, otherTrain.Name);
                        }
                        else
                        {
                            DisplayMessage = String.Concat(DisplayMessage, "train no. ");
                            DisplayMessage = String.Concat(DisplayMessage, waitAttach.ToString());
                        }
                        DisplayColor = Color.Orange;
                        remaining = 999;
                    }
                    else if (waitTransfer >= 0)
                    {
                        TTTrain otherTrain = GetOtherTTTrainByNumber(waitTransfer);
                        DisplayMessage = Simulator.Catalog.GetString("Waiting for transfer with train : ");
                        if (otherTrain != null)
                        {
                            DisplayMessage = String.Concat(DisplayMessage, otherTrain.Name);
                        }
                        else
                        {
                            DisplayMessage = String.Concat(DisplayMessage, "train no. ");
                            DisplayMessage = String.Concat(DisplayMessage, waitAttach.ToString());
                        }
                        DisplayColor = Color.Orange;
                        remaining = 999;
                    }
                    else if (waitArrivalAttach >= 0 && !readyToAttach && !attaching)
                    {
                        DisplayMessage = Simulator.Catalog.GetString("Waiting for train to arrive : ");
                        DisplayMessage = String.Concat(DisplayMessage, attachTrain.Name);
                        DisplayColor = Color.Orange;
                        remaining = 999;
                    }
                    else if (readyToAttach)
                    {
                        string attachPositionInfo = String.Empty;

                        // if setback required, reverse train
                        if (AttachDetails.SetBack)
                        {
                            // remove any reserved sections
                            RemoveFromTrackNotOccupied(ValidRoute[0]);

                            // check if train in same section
                            float distanceToTrain = 0.0f;
                            if (attachTrain.PresentPosition[0].TCSectionIndex == PresentPosition[1].TCSectionIndex)
                            {
                                TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][PresentPosition[1].RouteListIndex].TCSectionIndex];
                                distanceToTrain = thisSection.Length;
                            }
                            else
                            {
                                // get section index of other train in train route
                                int endSectionIndex = ValidRoute[0].GetRouteIndexBackward(attachTrain.PresentPosition[0].TCSectionIndex, PresentPosition[1].RouteListIndex);
                                if (endSectionIndex < 0)
                                {
                                    Trace.TraceWarning("Train {0} : attach to train {1} failed, cannot find path", Name, attachTrain.Name);
                                }

                                // get distance to train
                                for (int iSection = PresentPosition[0].RouteListIndex; iSection >= endSectionIndex; iSection--)
                                {
                                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iSection].TCSectionIndex];
                                    distanceToTrain += thisSection.Length;
                                }
                            }

                            // create temp route and set as valid route
                            int newDirection = PresentPosition[0].TCDirection == 0 ? 1 : 0;
                            TCSubpathRoute tempRoute = signalRef.BuildTempRoute(this, PresentPosition[0].TCSectionIndex, 0.0f, newDirection, distanceToTrain, true, true, false);

                            // set reverse positions
                            TCPosition tempPosition = new TCPosition();
                            PresentPosition[0].CopyTo(ref tempPosition);
                            PresentPosition[1].CopyTo(ref PresentPosition[0]);
                            tempPosition.CopyTo(ref PresentPosition[1]);

                            PresentPosition[0].Reverse(ValidRoute[0][PresentPosition[0].RouteListIndex].Direction, tempRoute, Length, signalRef);
                            PresentPosition[0].CopyTo(ref PreviousPosition[0]);
                            PresentPosition[1].Reverse(ValidRoute[0][PresentPosition[1].RouteListIndex].Direction, tempRoute, 0.0f, signalRef);

                            // reverse formation
                            ReverseFormation(true);
                            attachPositionInfo = Simulator.Catalog.GetString(", backward");

                            // get new route list indices from new route

                            DistanceTravelledM = 0;
                            ValidRoute[0] = tempRoute;

                            PresentPosition[0].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
                            PresentPosition[1].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
                        }
                        else
                        {
                            // build path to train - straight forward, set distance of 2000m (should be enough)
                            TCSubpathRoute tempRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, 0.0f, PresentPosition[1].TCDirection, 2000, true, true, false);
                            ValidRoute[0] = tempRoute;
                            attachPositionInfo = Simulator.Catalog.GetString(", forward");
                        }

                        LastReservedSection[0] = PresentPosition[0].TCSectionIndex;
                        LastReservedSection[1] = PresentPosition[1].TCSectionIndex;

                        MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                        SwitchToNodeControl(PresentPosition[0].TCSectionIndex);

                        DisplayMessage = Simulator.Catalog.GetString("Train is ready to attach to : ");
                        DisplayMessage = String.Concat(DisplayMessage, attachTrain.Name, attachPositionInfo);
                        DisplayColor = Color.Green;
                        remaining = 999;
                    }
                    else if (attaching)
                    {
                        string attachPositionInfo = AttachDetails.SetBack ? Simulator.Catalog.GetString(", backward") : Simulator.Catalog.GetString(", forward");
                        DisplayMessage = Simulator.Catalog.GetString("Train is ready to attach to : ");
                        DisplayMessage = String.Concat(DisplayMessage, attachTrain.Name, attachPositionInfo);
                        DisplayColor = Color.Green;
                        remaining = 999;
                    }
                    else
                    {
                        int actualDepart = StationStops[0].ActualDepart;
                        if (helddepart >= 0)
                        {
                            actualDepart = CompareTimes.LatestTime(helddepart, actualDepart);
                            StationStops[0].ActualDepart = actualDepart;
                        }

                        int correctedTime = presentTime;
                        if (presentTime > sixteenHundredHours && StationStops[0].DepartTime < eightHundredHours)
                        {
                            correctedTime = presentTime - 24 * 3600;  // correct to time before midnight (negative value!)
                        }

                        remaining = actualDepart - correctedTime;

                        // set display text color
                        if (remaining < 1)
                        {
                            DisplayColor = Color.LightGreen;
                        }
                        else if (remaining < 11)
                        {
                            DisplayColor = new Color(255, 255, 128);
                        }
                        else
                        {
                            DisplayColor = Color.White;
                        }

                        // clear holding signal
                        if (remaining < 120 && StationStops[0].ExitSignal >= 0 && HoldingSignals.Contains(StationStops[0].ExitSignal)) // within two minutes of departure and hold signal?
                        {
                            HoldingSignals.Remove(StationStops[0].ExitSignal);

                            if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
                            {
                                SignalObject nextSignal = signalRef.SignalObjects[StationStops[0].ExitSignal];
                                nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null);
                            }
                        }

                        // check departure time
                        if (remaining <= 0)
                        {
                            // if at end of route allow depart without playing departure sound
                            if (CheckEndOfRoutePositionTT())
                            {
                                MayDepart = true;
                                DisplayMessage = Simulator.Catalog.GetString("Passenger detraining completed. Train terminated.");
                            }
                            else if (!MayDepart)
                            {
                                // check if signal ahead is cleared - if not, and signal is station exit signal, do not allow depart
                                if (NextSignalObject[0] != null && NextSignalObject[0].this_sig_lr(MstsSignalFunction.NORMAL) == MstsSignalAspect.STOP
                                    && NextSignalObject[0].hasPermission != SignalObject.Permission.Granted && !StationStops[0].NoWaitSignal
                                    && NextSignalObject[0].thisRef == StationStops[0].ExitSignal)
                                {
                                    DisplayMessage = Simulator.Catalog.GetString("Passenger boarding completed. Waiting for signal ahead to clear.");
                                }
                                else
                                {
                                    MayDepart = true;
                                    if (!StationStops[0].EndStop)
                                    {
                                        if (!DriverOnlyOperation) Simulator.SoundNotify = Event.PermissionToDepart;  // sound departure if not doo
                                        DisplayMessage = Simulator.Catalog.GetString("Passenger boarding completed. You may depart now.");
                                    }
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
                // if stations to be checked
                if (StationStops.Count > 0)
                {
                    // check if stopped at station
                    if (Math.Abs(SpeedMpS) < 0.05f)
                    {
                        // build list of occupied section
                        int frontIndex = PresentPosition[0].RouteListIndex;
                        int rearIndex = PresentPosition[1].RouteListIndex;
                        List<int> occupiedSections = new List<int>();

                        // check valid positions
                        if (frontIndex < 0 && rearIndex < 0) // not on route so cannot be in station
                        {
                            return; // no further actions possible
                        }

                        // correct position if either end is off route
                        if (frontIndex < 0) frontIndex = rearIndex;
                        if (rearIndex < 0) rearIndex = frontIndex;

                        // set start and stop in correct order
                        int startIndex = frontIndex < rearIndex ? frontIndex : rearIndex;
                        int stopIndex = frontIndex < rearIndex ? rearIndex : frontIndex;

                        for (int iIndex = startIndex; iIndex <= stopIndex; iIndex++)
                        {
                            occupiedSections.Add(ValidRoute[0][iIndex].TCSectionIndex);
                        }

                        // check if any platform section is in list of occupied sections - if so, we're in the station
                        foreach (int sectionIndex in StationStops[0].PlatformItem.TCSectionIndex)
                        {
                            if (occupiedSections.Contains(sectionIndex))
                            {
                                // TODO : check offset within section
                                AtStation = true;
                                break;
                            }
                        }

                        if (AtStation)
                        {
                            MovementState = AI_MOVEMENT_STATE.STATION_STOP;

                            int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
                            StationStops[0].ActualArrival = presentTime;
                            StationStops[0].CalculateDepartTime(presentTime, this);

                            if (StationStops[0].ConnectionsWaiting.Count > 0)
                            {
                                foreach (int otherTrainNumber in StationStops[0].ConnectionsWaiting)
                                {
                                    TTTrain otherTrain = GetOtherTTTrainByNumber(otherTrainNumber);
                                    if (otherTrain != null)
                                    {
                                        foreach (StationStop otherStop in otherTrain.StationStops)
                                        {
                                            if (String.Compare(StationStops[0].PlatformItem.Name, otherStop.PlatformItem.Name) == 0 && otherStop.ConnectionsAwaited.ContainsKey(OrgAINumber))
                                            {
                                                otherStop.ConnectionsAwaited.Remove(OrgAINumber);
                                                otherStop.ConnectionsAwaited.Add(OrgAINumber, StationStops[0].ActualArrival);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        MovementState = AI_MOVEMENT_STATE.RUNNING;   // reset movement state (must not remain set at STATION_STOP)
                        if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                        {
                            nextActionInfo = null;   // clear next action if still referring to station stop
                        }

                        // check if station missed : station must be at least 500m. behind us
                        bool missedStation = false;

                        int stationRouteIndex = ValidRoute[0].GetRouteIndex(StationStops[0].TCSectionIndex, 0);

                        if (StationStops[0].SubrouteIndex == TCRoute.activeSubpath)
                        {
                            if (stationRouteIndex < 0)
                            {
                                missedStation = true;
                            }
                            else if (stationRouteIndex < PresentPosition[1].RouteListIndex)
                            {
                                missedStation = ValidRoute[0].GetDistanceAlongRoute(stationRouteIndex, StationStops[0].StopOffset, PresentPosition[1].RouteListIndex, PresentPosition[1].TCOffset, true, signalRef) > 500f;
                            }
                        }

                        if (missedStation)
                        {
                            PreviousStop = StationStops[0].CreateCopy();
                            StationStops.RemoveAt(0);

                            if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                                Simulator.Confirmer.Information("Missed station stop : " + PreviousStop.PlatformItem.Name);

                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Special actions in timetable mode when waiting for signal to clear
        /// Override from Train class to allow call from common methods
        /// <\summary>

        public override void ActionsForSignalStop(ref bool claimAllowed)
        {
            // cannot claim if in station and noclaim is set
            if (AtStation)
            {
                if (StationStops[0].NoClaimAllowed) claimAllowed = false;
            }

            // test for attach for train ahead
            if (AttachDetails != null && AttachDetails.StationPlatformReference < 0 && !AttachDetails.ReadyToAttach)
            {
                for (int iIndex = PresentPosition[0].RouteListIndex; iIndex < ValidRoute[0].Count; iIndex++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][iIndex].TCSectionIndex];
                    if (thisSection.CircuitState.HasTrainsOccupying())
                    {
                        List<TrainRouted> allTrains = thisSection.CircuitState.TrainsOccupying();
                        foreach (TrainRouted routedTrain in allTrains)
                        {
                            TTTrain otherTrain = routedTrain.Train as TTTrain;
                            if (otherTrain.OrgAINumber == AttachDetails.AttachTrain)
                            {
                                if (otherTrain.AtStation)
                                {
                                    AttachDetails.ReadyToAttach = true;
                                }
                                else if (otherTrain.MovementState == AI_MOVEMENT_STATE.AI_STATIC && otherTrain.ActivateTime != null)
                                {
                                    AttachDetails.ReadyToAttach = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Switch to Node control
        /// Override from Train class
        /// <\summary>

        public override void SwitchToNodeControl(int thisSectionIndex)
        {
            base.SwitchToNodeControl(thisSectionIndex);

            // check if train is to attach in sections ahead (otherwise done at signal)
            if (TrainType != TRAINTYPE.PLAYER)
            {
                CheckReadyToAttach();
            }
        }

        //================================================================================================//
        /// <summary>
        /// Clear station from list, clear exit signal if required
        /// Override from Train class
        /// <\summary>

        public override void ClearStation(uint id1, uint id2, bool removeStation)
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
            if (removeStation)
            {
                for (int iStation = foundStation; iStation >= 0; iStation--)
                {
                    StationStops.RemoveAt(iStation);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Process connection at station stop
        /// </summary>
        /// <param name="thisStop"></param>
        /// <param name="deptime"></param>
        /// <returns></returns>
        public int ProcessConnections(StationStop thisStop, out int deptime)
        {
            int? helddepart = null;
            int needwait = -1;
            List<int> removeKeys = new List<int>();

            foreach (KeyValuePair<int, int> connectionInfo in thisStop.ConnectionsAwaited)
            {
                // check if train arrival time set
                int otherTrainNumber = connectionInfo.Key;
                WaitInfo reqWait = thisStop.ConnectionDetails[otherTrainNumber];

                if (connectionInfo.Value >= 0)
                {
                    removeKeys.Add(connectionInfo.Key);
                    int reqHoldTime = (reqWait.holdTimeS.HasValue) ? reqWait.holdTimeS.Value : 0;
                    int allowedDepart = (connectionInfo.Value + reqHoldTime) % (24 * 3600);
                    if (helddepart.HasValue)
                    {
                        helddepart = CompareTimes.LatestTime(helddepart.Value, allowedDepart);
                    }
                    else
                    {
                        helddepart = allowedDepart;
                    }
                }
                else
                // check if train exists and if so, check its delay
                {
                    TTTrain otherTrain = GetOtherTTTrainByNumber(otherTrainNumber);

                    if (otherTrain != null)
                    {
                        // get station index for other train
                        StationStop reqStop = null;

                        foreach (StationStop nextStop in otherTrain.StationStops)
                        {
                            if (nextStop.PlatformItem.Name.Equals(StationStops[0].PlatformItem.Name))
                            {
                                reqStop = nextStop;
                                break;
                            }
                        }

                        // check if train is not passed the station
                        if (reqStop != null)
                        {
                            if (otherTrain.Delay.HasValue && reqWait.maxDelayS.HasValue)
                            {
                                if (otherTrain.Delay.Value.TotalSeconds <= reqWait.maxDelayS.Value)
                                {
                                    needwait = otherTrainNumber;  // train is within allowed time - wait required
                                    break;                        // no need to check other trains
                                }
                                else if (Delay.HasValue && (thisStop.ActualDepart > reqStop.ArrivalTime + otherTrain.Delay.Value.TotalSeconds))
                                {
                                    needwait = otherTrainNumber;  // train expected to arrive before our departure - wait
                                    break;
                                }
                                else
                                {
                                    removeKeys.Add(connectionInfo.Key); // train is excessively late - remove connection
                                }
                            }
                            else
                            {
                                needwait = otherTrainNumber;
                                break;                        // no need to check other trains
                            }
                        }
                    }
                }
            }

            // remove processed keys
            foreach (int key in removeKeys)
            {
                thisStop.ConnectionsAwaited.Remove(key);
            }

            // set departure time
            deptime = -1;
            if (helddepart.HasValue)
            {
                deptime = helddepart.Value;
            }

            return (needwait);
        }

        //================================================================================================//
        /// <summary>
        /// Perform end of route actions for player train
        /// Detach any required portions
        /// Return parameter : true is train still exists
        /// </summary>
        public bool ProcessRouteEndTimetablePlayer()
        {
            ActivateTriggeredTrain(TriggerActivationType.Dispose, -1);

            int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
            int nextTrainNumber = -1;
            bool stillExist = true;

            // check if needs to attach - if so, keep alive
            if (AttachDetails != null && AttachDetails.Valid)
            {
                return (true);
            }

            // check if final station not yet processed and any detach actions required
            bool allowForm = DetachActive[1] == -1; // preset if form is allowed to proceed - may not proceed if detach action is still active

            // check if detach action active
            if (DetachActive[1] == -1)
            {
                if (StationStops != null && StationStops.Count > 0)
                {
                    if (DetachDetails.ContainsKey(StationStops[0].PlatformReference))
                    {
                        List<DetachInfo> detachList = DetachDetails[StationStops[0].PlatformReference];

                        for (int iDetach = detachList.Count - 1; iDetach >= 0; iDetach--)
                        {
                            DetachInfo thisDetach = detachList[iDetach];
                            if (thisDetach.DetachPosition == DetachInfo.DetachPositionInfo.atEnd && thisDetach.Valid)
                            {
                                DetachActive[0] = -1;
                                DetachActive[1] = iDetach;
                                allowForm = thisDetach.PerformDetach(this, true);
                                thisDetach.Valid = false;
                            }
                        }
                    }
                    if (allowForm) DetachDetails.Remove(StationStops[0].PlatformReference);
                }

                // check if anything needs be detached at formed
                if (DetachDetails.ContainsKey(-1))
                {
                    List<DetachInfo> detachList = DetachDetails[-1];

                    for (int iDetach = detachList.Count - 1; iDetach >= 0; iDetach--)
                    {
                        DetachInfo thisDetach = detachList[iDetach];
                        if (thisDetach.DetachPosition == DetachInfo.DetachPositionInfo.atEnd && thisDetach.Valid)
                        {
                            DetachActive[0] = -1;
                            DetachActive[1] = iDetach;
                            allowForm = thisDetach.PerformDetach(this, true);
                            thisDetach.Valid = false;
                        }
                    }
                    if (allowForm) DetachDetails.Remove(-1);
                }
            }

            // check if train is still player train
            if (TrainType != TRAINTYPE.PLAYER)
            {
                FormTrainFromAI(presentTime);
                stillExist = false;
            }

            // if player train, only form new train if allowed - may be blocked by detach if detach is performed through player detach window
            else if (allowForm)
            {
                // train is terminated and does not form next train - set to static
                if (Forms < 0)
                {
                    ControlMode = TRAIN_CONTROL.INACTIVE;
                    ActivateTime = null;
                    StartTime = null;

                    // train is stored in pool
                    if (!String.IsNullOrEmpty(ExitPool))
                    {
                        TimetablePool thisPool = Simulator.PoolHolder.Pools[ExitPool];
                        thisPool.AddUnit(this, false);
                    }

                    MovementState = AI_MOVEMENT_STATE.AI_STATIC;
                    return (true);
                }

                // form next train
                TTTrain nextPlayerTrain = null;
                List<int> newTrains = new List<int>();

                bool autogenStart = false;

                // get train which is to be formed
                TTTrain formedTrain = Simulator.AI.StartList.GetNotStartedTTTrainByNumber(Forms, true);

                if (formedTrain == null)
                {
                    formedTrain = Simulator.GetAutoGenTTTrainByNumber(Forms);
                    autogenStart = true;
                }

                // if found - start train
                if (formedTrain != null)
                {
                    // remove existing train
                    Forms = -1;

                    // remove all existing deadlock path references
                    signalRef.RemoveDeadlockPathReferences(0);

                    // set details for new train from existing train
                    TrackCircuitSection[] occupiedSections = new TrackCircuitSection[OccupiedTrack.Count];
                    OccupiedTrack.CopyTo(occupiedSections);
                    bool validFormed = formedTrain.StartFromAITrain(this, presentTime, occupiedSections);

#if DEBUG_TRACEINFO
                        Trace.TraceInformation("{0} formed into {1}", Name, formedTrain.Name);
#endif

                    if (validFormed)
                    {
                        // start new train
                        if (!autogenStart)
                        {
                            Simulator.StartReference.Remove(formedTrain.Number);
                        }
                        if (nextTrainNumber < 0)
                        {
                            nextPlayerTrain = formedTrain;
                            nextTrainNumber = formedTrain.Number;
                        }
                        else
                        {
                            formedTrain.SetFormedOccupied();
                            Simulator.AI.TrainsToAdd.Add(formedTrain);
                        }
                    }
                    else if (!autogenStart)
                    {
                        // reinstate as to be started (note : train is not yet removed from reference)
                        Simulator.AI.StartList.InsertTrain(formedTrain);
                    }
                }

                // set proper player train references

                if (nextTrainNumber > 0)
                {
                    // clear this train - prepare for removal
                    RemoveFromTrack();
                    ClearDeadlocks();
                    Simulator.Trains.Remove(this);
                    Number = OrgAINumber;  // reset number
                    stillExist = false;
                    AI.TrainsToRemove.Add(this);

                    // remove formed train from AI list
                    AI.TrainsToRemoveFromAI.Add(formedTrain);

                    // set proper details for new formed train
                    formedTrain.OrgAINumber = nextTrainNumber;
                    formedTrain.Number = 0;
                    AI.TrainsToAdd.Add(formedTrain);
                    AI.aiListChanged = true;
                    Simulator.Trains.Add(formedTrain);

                    formedTrain.SetFormedOccupied();
                    formedTrain.TrainType = TRAINTYPE.PLAYER;
                    formedTrain.ControlMode = TRAIN_CONTROL.INACTIVE;
                    formedTrain.MovementState = AI_MOVEMENT_STATE.AI_STATIC;

                    // copy train control details
                    formedTrain.MUDirection = MUDirection;
                    formedTrain.MUThrottlePercent = MUThrottlePercent;
                    formedTrain.MUGearboxGearIndex = MUGearboxGearIndex;
                    formedTrain.MUReverserPercent = MUReverserPercent;
                    formedTrain.MUDynamicBrakePercent = MUDynamicBrakePercent;

                    if (TrainType == TRAINTYPE.PLAYER)
                    {
                        formedTrain.ConnectBrakeHoses();
                    }
                    else
                    {
                        formedTrain.InitializeBrakes();
                    }

                    // reallocate deadlock path references for new train
                    signalRef.ReallocateDeadlockPathReferences(nextTrainNumber, 0);

                    bool foundPlayerLocomotive = false;
                    TrainCar newPlayerLocomotive = null;

                    // search for player locomotive
                    for (int icar = 0; icar < formedTrain.Cars.Count; icar++)
                    {
                        var car = formedTrain.Cars[icar];
                        if (car.IsDriveable)
                        {
                            if (Simulator.PlayerLocomotive == car)
                            {
                                foundPlayerLocomotive = true;
                                formedTrain.LeadLocomotiveIndex = icar;
                                break;
                            }
                            else if (newPlayerLocomotive == null)
                            {
                                newPlayerLocomotive = car;
                                formedTrain.LeadLocomotiveIndex = icar;
                            }
                        }
                    }

                    if (!foundPlayerLocomotive)
                    {
                        Simulator.PlayerLocomotive = newPlayerLocomotive;
                        Simulator.OnPlayerLocomotiveChanged();
                    }

                    // notify viewer of change in selected train
                    Simulator.OnPlayerTrainChanged(this, formedTrain);
                    Simulator.PlayerLocomotive.Train = formedTrain;

                    // set up station handling for new train
                    formedTrain.SetupStationStopHandling();

                    if (AtStation && formedTrain.AtStation && StationStops[0].PlatformReference == formedTrain.StationStops[0].PlatformReference)
                    {
                        formedTrain.MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                        formedTrain.StationStops[0].ActualArrival = StationStops[0].ActualArrival;
                        formedTrain.StationStops[0].arrivalDT = StationStops[0].arrivalDT;
                        formedTrain.StationStops[0].ArrivalTime = StationStops[0].ArrivalTime;
                        formedTrain.StationStops[0].CalculateDepartTime(presentTime, this);
                    }

                    // clear replay commands
                    Simulator.Log.CommandList.Clear();

                    // display messages
                    if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                        Simulator.Confirmer.Information("Player switched to train : " + formedTrain.Name);
                }
            }

            return (stillExist);
        }

        //================================================================================================//
        /// <summary>
        /// Process speed settings defined in timetable
        /// </summary>

        public void ProcessSpeedSettings()
        {
            SpeedSettings.consistSpeedMpS = SpeedSettings.consistSpeedMpS == 0 ? SpeedSettings.routeSpeedMpS : Math.Min(SpeedSettings.consistSpeedMpS, SpeedSettings.routeSpeedMpS);

            // correct cruise speed if value is incorrect
            if (SpeedSettings.maxSpeedMpS.HasValue && SpeedSettings.cruiseSpeedMpS.HasValue &&
                SpeedSettings.maxSpeedMpS < SpeedSettings.cruiseSpeedMpS)
            {
                SpeedSettings.cruiseSpeedMpS = SpeedSettings.maxSpeedMpS;
            }

            // take max of maxspeed and consist speed, or set maxspeed
            if (SpeedSettings.maxSpeedMpS.HasValue)
            {
                SpeedSettings.maxSpeedMpS = Math.Min(SpeedSettings.maxSpeedMpS.Value, SpeedSettings.consistSpeedMpS);
                SpeedSettings.restrictedSet = true;
            }
            else
            {
                SpeedSettings.maxSpeedMpS = SpeedSettings.consistSpeedMpS;
            }

            // take max of cruisespeed and consist speed
            if (SpeedSettings.cruiseSpeedMpS.HasValue)
            {
                SpeedSettings.cruiseSpeedMpS = Math.Min(SpeedSettings.cruiseSpeedMpS.Value, SpeedSettings.consistSpeedMpS);
            }

            // set creep, attach and detach speed if not defined
            if (!SpeedSettings.creepSpeedMpS.HasValue)
            {
                SpeedSettings.creepSpeedMpS = TTTrain.creepSpeedMpS;
            }

            if (!SpeedSettings.attachSpeedMpS.HasValue)
            {
                SpeedSettings.attachSpeedMpS = TTTrain.couplingSpeedMpS;
            }

            if (!SpeedSettings.detachSpeedMpS.HasValue)
            {
                SpeedSettings.detachSpeedMpS = TTTrain.couplingSpeedMpS;
            }

            if (!SpeedSettings.movingtableSpeedMpS.HasValue)
            {
                SpeedSettings.movingtableSpeedMpS = TTTrain.movingtableSpeedMpS;
            }

            TrainMaxSpeedMpS = SpeedSettings.maxSpeedMpS.Value;
        }

        //================================================================================================//
        /// <summary>
        /// Get no. of units which are to be detached
        /// Process detach or transfer command to determine no. of required units
        /// </summary>
        /// <param name="detachUnits"></param>
        /// <param name="numberOfUnits"></param>
        /// <param name="detachConsist"></param>
        /// <param name="frontpos"></param>
        /// <returns></returns>
        public int GetUnitsToDetach(DetachInfo.DetachUnitsInfo detachUnits, int numberOfUnits, List<string> detachConsist, ref bool frontpos)
        {
            int iunits = 0;
            var thisCar = Cars[0];

            switch (detachUnits)
            {
                case DetachInfo.DetachUnitsInfo.leadingPower:
                    bool checktender = false;
                    bool checkengine = false;

                    // check first unit
                    thisCar = Cars[0];
                    if (thisCar.WagonType == TrainCar.WagonTypes.Engine)
                    {
                        iunits++;
                        checktender = true;
                    }
                    else if (thisCar.WagonType == TrainCar.WagonTypes.Tender)
                    {
                        iunits++;
                        checkengine = true;
                    }

                    int nextunit = 1;
                    while (checktender && nextunit < Cars.Count)
                    {
                        thisCar = Cars[nextunit];
                        if (thisCar.WagonType == TrainCar.WagonTypes.Tender)
                        {
                            iunits++;
                            nextunit++;
                        }
                        else
                        {
                            checktender = false;
                        }
                    }

                    while (checkengine && nextunit < Cars.Count)
                    {
                        thisCar = Cars[nextunit];
                        if (thisCar.WagonType == TrainCar.WagonTypes.Tender)
                        {
                            iunits++;
                            nextunit++;
                        }
                        else if (thisCar.WagonType == TrainCar.WagonTypes.Engine)
                        {
                            iunits++;
                            checkengine = false;
                        }
                        else
                        {
                            checkengine = false;
                        }
                    }
                    break;

                case DetachInfo.DetachUnitsInfo.allLeadingPower:
                    for (int iCar = 0; iCar < Cars.Count; iCar++)
                    {
                        thisCar = Cars[iCar];
                        if (thisCar.WagonType == TrainCar.WagonTypes.Engine || thisCar.WagonType == TrainCar.WagonTypes.Tender)
                        {
                            iunits++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    break;

                case DetachInfo.DetachUnitsInfo.trailingPower:
                    checktender = false;
                    checkengine = false;
                    frontpos = false;

                    // check first unit
                    thisCar = Cars[Cars.Count - 1];
                    if (thisCar.WagonType == TrainCar.WagonTypes.Engine)
                    {
                        iunits++;
                        checktender = true;
                    }
                    else if (thisCar.WagonType == TrainCar.WagonTypes.Tender)
                    {
                        iunits++;
                        checkengine = true;
                    }

                    nextunit = Cars.Count - 2;
                    while (checktender && nextunit >= 0)
                    {
                        thisCar = Cars[nextunit];
                        if (thisCar.WagonType == TrainCar.WagonTypes.Tender)
                        {
                            iunits++;
                            nextunit--;
                        }
                        else
                        {
                            checktender = false;
                        }
                    }

                    while (checkengine && nextunit >= 0)
                    {
                        thisCar = Cars[nextunit];
                        if (thisCar.WagonType == TrainCar.WagonTypes.Tender)
                        {
                            iunits++;
                            nextunit--;
                        }
                        else if (thisCar.WagonType == TrainCar.WagonTypes.Engine)
                        {
                            iunits++;
                            checkengine = false;
                        }
                        else
                        {
                            checkengine = false;
                        }
                    }
                    break;

                case DetachInfo.DetachUnitsInfo.allTrailingPower:
                    frontpos = false;

                    for (int iCar = Cars.Count - 1; iCar >= 0; iCar--)
                    {
                        thisCar = Cars[iCar];
                        if (thisCar.WagonType == TrainCar.WagonTypes.Engine || thisCar.WagonType == TrainCar.WagonTypes.Tender)
                        {
                            iunits++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    break;

                case DetachInfo.DetachUnitsInfo.nonPower:

                    int frontunits = 0;
                    // power is at front
                    if (Cars[0].WagonType == TrainCar.WagonTypes.Engine || Cars[0].WagonType == TrainCar.WagonTypes.Tender)
                    {
                        frontpos = false;
                        nextunit = 0;
                        thisCar = Cars[nextunit];

                        while ((thisCar.WagonType == TrainCar.WagonTypes.Engine || thisCar.WagonType == TrainCar.WagonTypes.Tender) && nextunit < Cars.Count)
                        {
                            frontunits++;
                            nextunit++;
                            if (nextunit < Cars.Count)
                            {
                                thisCar = Cars[nextunit];
                            }
                        }
                        iunits = Cars.Count - frontunits;
                    }
                    // power is at rear
                    else
                    {
                        frontpos = true;
                        nextunit = Cars.Count - 1;
                        thisCar = Cars[nextunit];

                        while ((thisCar.WagonType == TrainCar.WagonTypes.Engine || thisCar.WagonType == TrainCar.WagonTypes.Tender) && nextunit >= 0)
                        {
                            frontunits++;
                            nextunit--;
                            if (nextunit >= 0)
                            {
                                thisCar = Cars[nextunit];
                            }
                        }
                        iunits = Cars.Count - frontunits;
                    }
                    break;

                case DetachInfo.DetachUnitsInfo.consists:
                    bool inConsist = false;
                    
                    // check if front must be detached
                    if (detachConsist.Contains(Cars[0].OrgConsist))
                    {
                        inConsist = true;
                        frontpos = true;
                        nextunit = 1;
                        iunits = 1;

                        while (nextunit < Cars.Count && inConsist)
                        {
                            if (detachConsist.Contains(Cars[nextunit].OrgConsist))
                            {
                                iunits++;
                                nextunit++;
                            }
                            else
                            {
                                inConsist = false;
                            }
                        }
                    }
                    else if (detachConsist.Contains(Cars[Cars.Count - 1].OrgConsist))
                    {
                        inConsist = true;
                        frontpos = false;
                        nextunit = Cars.Count - 2;
                        iunits = 1;

                        while (nextunit >= 0 && inConsist)
                        {
                            if (detachConsist.Contains(Cars[nextunit].OrgConsist))
                            {
                                iunits++;
                                nextunit--;
                            }
                            else
                            {
                                inConsist = false;
                            }
                        }
                    }
                    break;

                default:
                    iunits = numberOfUnits;
                    frontpos = detachUnits == DetachInfo.DetachUnitsInfo.unitsAtFront;
                    break;
            }

            return (iunits);
        }

        //================================================================================================//
        /// <summary>
        /// Couple trains
        /// </summary>
        /// <param name="attachTrain"></param>
        /// <param name="thisTrainFront"></param>
        /// <param name="attachTrainFront"></param>
        public void TTCouple(TTTrain attachTrain, bool thisTrainFront, bool attachTrainFront)
        {
            // stop train
            SpeedMpS = 0;
            foreach (var car in Cars)
            {
                car.SpeedMpS = 0;
            }

            if (TrainType != TRAINTYPE.PLAYER) AdjustControlsThrottleOff();
            physicsUpdate(0);

            // stop attach train
            attachTrain.SpeedMpS = 0;
            foreach (var car in attachTrain.Cars)
            {
                car.SpeedMpS = 0;
            }

            if (attachTrain.TrainType != TRAINTYPE.PLAYER) attachTrain.AdjustControlsThrottleOff();
            attachTrain.physicsUpdate(0);

            // set message for checktrain
            if (attachTrain.CheckTrain || CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Attaching : " + Number + " ; to : " + attachTrain.Number + " ; at front : " + attachTrainFront.ToString() + "\n");
            }

            // check on reverse formation
            if (thisTrainFront == attachTrainFront)
            {
                ReverseFormation(TrainType == TRAINTYPE.PLAYER);
            }

            var attachCar = Cars[0];

            int playerLocomotiveIndex = -1;
            if (TrainType == TRAINTYPE.PLAYER || TrainType == TRAINTYPE.INTENDED_PLAYER)
            {
                playerLocomotiveIndex = LeadLocomotiveIndex;
            }
            else if (attachTrain.TrainType == TRAINTYPE.PLAYER || attachTrain.TrainType == TRAINTYPE.INTENDED_PLAYER)
            {
                playerLocomotiveIndex = attachTrain.LeadLocomotiveIndex;
            }

            // attach to front of waiting train
            if (attachTrainFront)
            {
                attachCar = Cars[Cars.Count - 1];
                for (int iCar = Cars.Count - 1; iCar >= 0; iCar--)
                {
                    var car = Cars[iCar];
                    car.Train = attachTrain;
                    attachTrain.Cars.Insert(0, car);
                    if (attachTrain.TrainType == TRAINTYPE.PLAYER) playerLocomotiveIndex++;
                }
            }
            // attach to rear of waiting train
            else
            {
                if (TrainType == TRAINTYPE.PLAYER) playerLocomotiveIndex += attachTrain.Cars.Count;
                foreach (var car in Cars)
                {
                    car.Train = attachTrain;
                    attachTrain.Cars.Add(car);
                }
            }

            // renumber cars
            int carId = 0;
            foreach (var car in attachTrain.Cars)
            {
                car.CarID = String.Concat(attachTrain.Number.ToString("0###"), "_", carId.ToString("0##"));
                carId++;
            }

            // remove cars from this train
            Cars.Clear();
            attachTrain.Length += Length;
            float distanceTravelledCorrection = 0;

            // recalculate position of formed train
            if (attachTrainFront)  // coupled to front, so rear position is still valid
            {
                attachTrain.CalculatePositionOfCars();
                attachTrain.DistanceTravelledM += Length;
                distanceTravelledCorrection = Length;
            }
            else // coupled to rear so front position is still valid
            {
                attachTrain.RepositionRearTraveller();    // fix the rear traveller
                attachTrain.CalculatePositionOfCars();
            }

            // update positions train
            TrackNode tn = attachTrain.FrontTDBTraveller.TN;
            float offset = attachTrain.FrontTDBTraveller.TrackNodeOffset;
            int direction = (int)attachTrain.FrontTDBTraveller.Direction;

            attachTrain.PresentPosition[0].SetTCPosition(tn.TCCrossReference, offset, direction);
            attachTrain.PresentPosition[0].CopyTo(ref attachTrain.PreviousPosition[0]);

            tn = attachTrain.RearTDBTraveller.TN;
            offset = attachTrain.RearTDBTraveller.TrackNodeOffset;
            direction = (int)attachTrain.RearTDBTraveller.Direction;

            attachTrain.PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);

            // set new track sections occupied
            TCSubpathRoute tempRoute = signalRef.BuildTempRoute(attachTrain, attachTrain.PresentPosition[1].TCSectionIndex,
                attachTrain.PresentPosition[1].TCOffset, attachTrain.PresentPosition[1].TCDirection, attachTrain.Length, true, true, false);

            List<TrackCircuitSection> newOccupiedSections = new List<TrackCircuitSection>();
            foreach (TCRouteElement thisElement in tempRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                if (!attachTrain.OccupiedTrack.Contains(thisSection))
                {
                    newOccupiedSections.Add(thisSection);
                }
            }

            // first reserve to ensure all switches are properly alligned
            foreach (TrackCircuitSection newSection in newOccupiedSections)
            {
                newSection.Reserve(attachTrain.routedForward, tempRoute);
            }

            // next set occupied
            foreach (TrackCircuitSection newSection in newOccupiedSections)
            {
                newSection.SetOccupied(attachTrain.routedForward);
            }

            // reset OccupiedTrack to ensure it is set in correct sequence
            attachTrain.OccupiedTrack.Clear();
            foreach (TCRouteElement thisElement in tempRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                attachTrain.OccupiedTrack.Add(thisSection);
            }

            // set various items
            attachTrain.CheckFreight();
            attachTrain.SetDPUnitIDs();
            attachTrain.ReinitializeEOT();
            attachCar.SignalEvent(Event.Couple);
            attachTrain.ProcessSpeedSettings();

            // adjust set actions for updated distance travelled value
            if (distanceTravelledCorrection > 0)
            {
                attachTrain.requiredActions.ModifyRequiredDistance(distanceTravelledCorrection);
            }

            // if not static, reassess signals if coupled at front (no need to reassess signals if coupled to rear)
            // also, reset movement state if not player train
            if (attachTrain.MovementState != AI_MOVEMENT_STATE.AI_STATIC)
            {
                if (attachTrainFront)
                {
                    attachTrain.InitializeSignals(true);
                }

                if (attachTrain.TrainType != TRAINTYPE.PLAYER && attachTrain.TrainType != TRAINTYPE.INTENDED_PLAYER)
                {
                    attachTrain.MovementState = AI_MOVEMENT_STATE.STOPPED;
                    AIActionItem.AI_ACTION_TYPE attachTrainAction = AIActionItem.AI_ACTION_TYPE.NONE;
                    if (attachTrain.nextActionInfo != null)
                    {
                        attachTrainAction = attachTrain.nextActionInfo.NextAction;
                    }
                    attachTrain.ResetActions(true, false);

                    // check if stopped in station - either this train or the attach train
                    if (AtStation && attachTrain.StationStops.Count > 0)
                    {
                        if (StationStops[0].PlatformReference == attachTrain.StationStops[0].PlatformReference)
                        {
                            attachTrain.MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                            if (attachTrain.nextActionInfo != null && attachTrain.nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                            {
                                attachTrain.nextActionInfo = null;
                            }
                        }
                    }
                    else if (attachTrain.AtStation)
                    {
                        attachTrain.MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                    }
                    else if (attachTrainAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                    {
                        if (attachTrain.StationStops[0].SubrouteIndex == attachTrain.TCRoute.activeSubpath &&
                           attachTrain.ValidRoute[0].GetRouteIndex(attachTrain.StationStops[0].TCSectionIndex, attachTrain.PresentPosition[0].RouteListIndex) <= attachTrain.PresentPosition[0].RouteListIndex)
                        // assume to be in station
                        // also set state of present train to station stop
                        {
                            MovementState = attachTrain.MovementState = AI_MOVEMENT_STATE.STATION_STOP;
                            attachTrain.AtStation = true;

                            if (attachTrain.CheckTrain)
                            {
                                File.AppendAllText(@"C:\temp\checktrain.txt", "Train " + attachTrain.Number + " assumed to be in station : " +
                                    attachTrain.StationStops[0].PlatformItem.Name + "( present section = " + attachTrain.PresentPosition[0].TCSectionIndex +
                                    " ; station section = " + attachTrain.StationStops[0].TCSectionIndex + " )\n");
                            }
                        }
                    }
                }
            }
            else
            {
                attachTrain.DistanceTravelledM = 0;
            }

            // check for needattach in attached train
            // if stopped at station use platform reference

            bool needAttachFound = false;
            if (AtStation)
            {
                int stationPlatformIndex = StationStops[0].PlatformReference;
                if (attachTrain.NeedAttach.ContainsKey(stationPlatformIndex))
                {
                    List<int> trainList = attachTrain.NeedAttach[stationPlatformIndex];
                    if (trainList.Contains(OrgAINumber))
                    {
                        needAttachFound = true;
                        trainList.Remove(OrgAINumber);
                        if (trainList.Count < 1)
                        {
                            attachTrain.NeedAttach.Remove(stationPlatformIndex);
                        }
                    }
                }
            }
            // else search through all entries

            if (!needAttachFound && attachTrain.NeedAttach != null && attachTrain.NeedAttach.Count > 0)
            {
                int? indexRemove = null;
                foreach (KeyValuePair<int, List<int>> thisNeedAttach in attachTrain.NeedAttach)
                {
                    int foundKey = thisNeedAttach.Key;
                    List<int> trainList = thisNeedAttach.Value;
                    if (trainList.Contains(OrgAINumber))
                    {
                        trainList.Remove(OrgAINumber);
                        needAttachFound = true;
                    }

                    if (trainList.Count < 1)
                    {
                        indexRemove = foundKey;
                    }
                }

                if (indexRemove.HasValue) attachTrain.NeedAttach.Remove(indexRemove.Value);
            }

#if DEBUG_REPORTS
            if (!needAttachFound)
            {
                Trace.TraceWarning("Train : {0} : coupling to train {1} : internal data error, needAttach reference not found", attachTrain.Name, Name);
            }
#endif

            // if train is player or intended player and train has no player engine, determine new loco lead index
            if (attachTrain.TrainType == Train.TRAINTYPE.PLAYER || attachTrain.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
            {
                if (Simulator.Confirmer != null) Simulator.Confirmer.Information("Train " + Name + " has attached");
                Trace.TraceInformation("Train " + Name + " has attached to player train");

                if (attachTrain.LeadLocomotive == null)
                {
                    if (attachTrain.Cars[0].IsDriveable)
                    {
                        attachTrain.LeadLocomotive = attachTrain.Simulator.PlayerLocomotive = attachTrain.Cars[0];
                    }
                    else if (attachTrain.Cars[(attachTrain.Cars.Count - 1)].IsDriveable)
                    {
                        attachTrain.LeadLocomotive = attachTrain.Simulator.PlayerLocomotive = attachTrain.Cars[(attachTrain.Cars.Count - 1)];
                    }
                    else
                    {
                        foreach (var thisCar in attachTrain.Cars)
                        {
                            if (thisCar.IsDriveable)
                            {
                                attachTrain.LeadLocomotive = attachTrain.Simulator.PlayerLocomotive = thisCar;
                            }
                        }
                    }
                }
                else
                {
                    // reassign leadlocomotive to reset index
                    attachTrain.LeadLocomotive = attachTrain.Simulator.PlayerLocomotive = attachTrain.Cars[playerLocomotiveIndex];
                    attachTrain.LeadLocomotiveIndex = playerLocomotiveIndex;
                }

                // if not in preupdate there must be an engine
                if (AI.Simulator.PlayerLocomotive == null && !Simulator.PreUpdate)
                {
                    throw new InvalidDataException("Can't find player locomotive in " + attachTrain.Name);
                }
            }
            // if attaching train is player : switch trains and set new engine index
            else if (TrainType == TRAINTYPE.PLAYER)
            {
                // prepare to remove old train
                Number = OrgAINumber;
                attachTrain.OrgAINumber = attachTrain.Number;
                attachTrain.Number = 0;

                RemoveTrain();
                Simulator.Trains.Remove(this);

                // reassign leadlocomotive to reset index
                attachTrain.LeadLocomotiveIndex = playerLocomotiveIndex;
                attachTrain.LeadLocomotive = attachTrain.Simulator.PlayerLocomotive = attachTrain.Cars[playerLocomotiveIndex];

                // correctly insert new player train
                attachTrain.AI.TrainsToRemoveFromAI.Add(attachTrain);
                attachTrain.Simulator.Trains.Remove(attachTrain);
                attachTrain.AI.TrainsToAdd.Add(attachTrain);
                attachTrain.AI.aiListChanged = true;
                attachTrain.Simulator.Trains.Add(attachTrain);

                attachTrain.SetFormedOccupied();
                attachTrain.TrainType = Train.TRAINTYPE.PLAYER;

                // if present movement state is active state, copy to new train
                if (MovementState != AI_MOVEMENT_STATE.AI_STATIC)
                {
                    attachTrain.MovementState = MovementState;
                }

                // inform viewer about player train switch
                attachTrain.Simulator.OnPlayerTrainChanged(this, attachTrain);
                attachTrain.Simulator.PlayerLocomotive.Train = attachTrain;

                attachTrain.SetupStationStopHandling();

                if (Simulator.Confirmer != null)
                {
                    Simulator.Confirmer.Information("Train attached to " + attachTrain.Name);
                    Simulator.Confirmer.Information("Train continues as " + attachTrain.Name);
                }

            }
            // set anti-slip for all engines in AI train
            else
            {
                foreach (TrainCar car in attachTrain.Cars)
                {
                    if (car.WagonType == TrainCar.WagonTypes.Engine)
                    {
                        MSTSLocomotive loco = car as MSTSLocomotive;
                        loco.AntiSlip = attachTrain.leadLocoAntiSlip;
                    }
                }
            }

            // remove original train
            RemoveTrain();

            // stop the wheels from moving etc
            attachTrain.physicsUpdate(0);

            // initialize brakes on resulting train except when both trains are player trains
            if (attachTrain.TrainType == TRAINTYPE.PLAYER)
            {
                attachTrain.ConnectBrakeHoses();
            }
            else
            {
                attachTrain.InitializeBrakes();
            }

            // update route positions if required
            int trainRearPositionIndex = attachTrain.ValidRoute[0].GetRouteIndex(tempRoute.First().TCSectionIndex, 0);
            int trainFrontPositionIndex = attachTrain.ValidRoute[0].GetRouteIndex(tempRoute.Last().TCSectionIndex, 0);

            if (trainRearPositionIndex < 0 || trainFrontPositionIndex < 0)
            {
                attachTrain.AdjustTrainRouteOnStart(trainRearPositionIndex, trainFrontPositionIndex, this);
            }

            // recalculate station stop positions
            attachTrain.RecalculateStationStops();

            // if normal stop, set restart delay
            if (!AtStation && !attachTrain.AtStation)
            {
                float randDelay = (float)Simulator.Random.Next((DelayedStartSettings.attachRestart.randomPartS * 10));
                RestdelayS = DelayedStartSettings.attachRestart.fixedPartS + (randDelay / 10f);
                DelayedStart = true;
                DelayedStartState = AI_START_MOVEMENT.PATH_ACTION;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Uncouple required units to form pre-defined train
        /// Uncouple performed in detach or transfer commands
        /// </summary>
        /// <param name="newTrain"></param>
        /// <param name="reverseTrain"></param>
        /// <param name="leadLocomotiveIndex"></param>
        /// <param name="newIsPlayer"></param>
        /// <returns></returns>
        public int TTUncoupleBehind(TTTrain newTrain, bool reverseTrain, int leadLocomotiveIndex, bool newIsPlayer)
        {
            // if front portion : move req units to new train and remove from old train
            // remove from rear to front otherwise they cannot be deleted

            int newLeadLocomotiveIndex = leadLocomotiveIndex;
            bool leadLocomotiveInNewTrain = false;
            var detachCar = Cars[DetachUnits];

            // detach from front
            if (DetachPosition)
            {
                detachCar = Cars[DetachUnits];
                newTrain.Cars.Clear();  // remove any cars on new train

                for (int iCar = 0; iCar <= DetachUnits - 1; iCar++)
                {
                    var car = Cars[0]; // each car is removed so always detach first car!!!
                    Cars.Remove(car);
                    Length = -car.CarLengthM;
                    newTrain.Cars.Add(car); // place in rear
                    car.Train = newTrain;
                    car.CarID = String.Concat(newTrain.Number.ToString("0000"), "_", (newTrain.Cars.Count - 1).ToString("0000"));
                    newTrain.Length += car.CarLengthM;
                    leadLocomotiveInNewTrain = leadLocomotiveInNewTrain || iCar == LeadLocomotiveIndex; // if detached car is leadlocomotive, the locomotive is in the new train
                }
                // if lead locomotive is beyond detach unit, update index
                if (leadLocomotiveIndex >= DetachUnits)
                {
                    newLeadLocomotiveIndex = leadLocomotiveIndex - DetachUnits;
                }
                // if new train is player but engine is not in new train, reset Simulator.Playerlocomotive
                else if (newIsPlayer && !leadLocomotiveInNewTrain)
                {
                    Simulator.PlayerLocomotive = null;
                }

            }
            // detach from rear
            else
            {
                int detachUnitsFromFront = Cars.Count - DetachUnits;
                detachCar = Cars[Cars.Count - DetachUnits];
                int totalCars = Cars.Count;
                newTrain.Cars.Clear();  // remove any cars on new train

                for (int iCar = 0; iCar <= DetachUnits - 1; iCar++)
                {
                    var car = Cars[totalCars - 1 - iCar]; // total cars is original length which keeps value despite cars are removed
                    Cars.Remove(car);
                    Length -= car.CarLengthM;
                    newTrain.Cars.Insert(0, car); // place in front
                    car.Train = newTrain;
                    car.CarID = String.Concat(newTrain.Number.ToString("0000"), "_", (DetachUnits - newTrain.Cars.Count).ToString("0000"));
                    newTrain.Length += car.CarLengthM;
                    leadLocomotiveInNewTrain = leadLocomotiveInNewTrain || (totalCars - 1 - iCar) == LeadLocomotiveIndex;
                }
                // if lead locomotive is beyond detach unit, update index
                if (leadLocomotiveIndex >= detachUnitsFromFront)
                {
                    newLeadLocomotiveIndex = leadLocomotiveIndex - detachUnitsFromFront;
                }
                else if (newIsPlayer && !leadLocomotiveInNewTrain)
                {
                    Simulator.PlayerLocomotive = null;
                }

            }

            // and fix up the travellers
            if (DetachPosition)
            {
                CalculatePositionOfCars();
                newTrain.RearTDBTraveller = new Traveller(FrontTDBTraveller);
                newTrain.CalculatePositionOfCars();
            }
            else
            {
                newTrain.RearTDBTraveller = new Traveller(RearTDBTraveller);
                newTrain.CalculatePositionOfCars();
                RepositionRearTraveller();    // fix the rear traveller
            }

            LastCar.CouplerSlackM = 0;

            newTrain.SpeedMpS = SpeedMpS = 0;
            newTrain.TrainMaxSpeedMpS = TrainMaxSpeedMpS;
            newTrain.AITrainBrakePercent = AITrainBrakePercent;
            newTrain.AITrainDirectionForward = true;

            // disconnect brake hose and close angle cocks
            if (DetachPosition)
            {
                Cars[0].BrakeSystem.FrontBrakeHoseConnected = false;
                Cars[0].BrakeSystem.AngleCockAOpen = false;
                newTrain.Cars[newTrain.Cars.Count - 1].BrakeSystem.AngleCockBOpen = false;
            }
            else
            {
                newTrain.Cars[0].BrakeSystem.FrontBrakeHoseConnected = false;
                newTrain.Cars[0].BrakeSystem.AngleCockAOpen = false;
                Cars[Cars.Count - 1].BrakeSystem.AngleCockBOpen = false;
            }

            // reverse new train if required
            if (reverseTrain)
            {
                newTrain.ReverseFormation(false);
                if (leadLocomotiveInNewTrain)
                {
                    newLeadLocomotiveIndex = newTrain.Cars.Count - newLeadLocomotiveIndex - 1;
                }
            }

            // check freight for both trains
            CheckFreight();
            SetDPUnitIDs();
            ReinitializeEOT();
            newTrain.CheckFreight();
            newTrain.SetDPUnitIDs();
            newTrain.ReinitializeEOT();

            // check speed values for both trains
            ProcessSpeedSettings();
            newTrain.ProcessSpeedSettings();

            // set states
            newTrain.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC; // start of as AI static
            newTrain.StartTime = null; // time will be set later

            // set delay
            float randDelay = (float)Simulator.Random.Next((DelayedStartSettings.detachRestart.randomPartS * 10));
            RestdelayS = DelayedStartSettings.detachRestart.fixedPartS + (randDelay / 10f);
            DelayedStart = true;
            DelayedStartState = AI_START_MOVEMENT.NEW;

            if (!newIsPlayer)
            {
                newTrain.TrainType = Train.TRAINTYPE.AI;
                newTrain.ControlMode = TRAIN_CONTROL.INACTIVE;
                newTrain.AI.TrainsToAdd.Add(newTrain);
            }

            // signal event
            detachCar.SignalEvent(Event.Uncouple);

            // update positions train
            TrackNode tn = FrontTDBTraveller.TN;
            float offset = FrontTDBTraveller.TrackNodeOffset;
            int direction = (int)FrontTDBTraveller.Direction;

            PresentPosition[0].SetTCPosition(tn.TCCrossReference, offset, direction);
            PresentPosition[0].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[0].TCSectionIndex, 0);
            PresentPosition[0].CopyTo(ref PreviousPosition[0]);

            if (DetachPosition)
            {
                DistanceTravelledM -= newTrain.Length;
            }

            tn = RearTDBTraveller.TN;
            offset = RearTDBTraveller.TrackNodeOffset;
            direction = (int)RearTDBTraveller.Direction;

            PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);
            PresentPosition[1].RouteListIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);

            // get new track sections occupied
            Train.TCSubpathRoute tempRouteTrain = Simulator.Signals.BuildTempRoute(this, PresentPosition[1].TCSectionIndex,
                PresentPosition[1].TCOffset, PresentPosition[1].TCDirection, Length, false, true, false);

            // if detached from front, clear train from track and all further sections
            // set train occupied for new sections
            if (DetachPosition)
            {
                RemoveFromTrack();

                // first reserve all sections to ensure switched are alligned, next set occupied
                for (int iIndex = 0; iIndex < tempRouteTrain.Count; iIndex++)
                {
                    TrackCircuitSection thisSection = Simulator.Signals.TrackCircuitList[tempRouteTrain[iIndex].TCSectionIndex];
                    thisSection.Reserve(routedForward, tempRouteTrain);
                }
                for (int iIndex = 0; iIndex < tempRouteTrain.Count; iIndex++)
                {
                    TrackCircuitSection thisSection = Simulator.Signals.TrackCircuitList[tempRouteTrain[iIndex].TCSectionIndex];
                    thisSection.SetOccupied(routedForward);
                }

                if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL) ControlMode = TRAIN_CONTROL.AUTO_NODE;  // set to node control as detached portion is in front
                NextSignalObject[0] = null; // reset signal object (signal is not directly in front)
            }

            // remove train from track which it no longer occupies and clear actions for those sections
            else
            {
                RemoveFromTrackNotOccupied(tempRouteTrain);
            }

            // update positions new train
            tn = newTrain.FrontTDBTraveller.TN;
            offset = newTrain.FrontTDBTraveller.TrackNodeOffset;
            direction = (int)newTrain.FrontTDBTraveller.Direction;

            newTrain.PresentPosition[0].SetTCPosition(tn.TCCrossReference, offset, direction);
            newTrain.PresentPosition[0].RouteListIndex = newTrain.ValidRoute[0].GetRouteIndex(newTrain.PresentPosition[0].TCSectionIndex, 0);
            newTrain.PresentPosition[0].CopyTo(ref newTrain.PreviousPosition[0]);

            newTrain.DistanceTravelledM = 0.0f;

            tn = newTrain.RearTDBTraveller.TN;
            offset = newTrain.RearTDBTraveller.TrackNodeOffset;
            direction = (int)newTrain.RearTDBTraveller.Direction;

            newTrain.PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);
            newTrain.PresentPosition[1].RouteListIndex = newTrain.ValidRoute[0].GetRouteIndex(newTrain.PresentPosition[1].TCSectionIndex, 0);
            newTrain.PresentPosition[1].CopyTo(ref newTrain.PreviousPosition[1]);

            // check if train is on valid path
            if (newTrain.PresentPosition[0].RouteListIndex < 0 && newTrain.PresentPosition[1].RouteListIndex < 0)
            {
                Trace.TraceInformation("Train : {0} ({1}) : detached from {2} ({3}) : is not on valid path\n", newTrain.Name, newTrain.Number, Name, Number);
                newTrain.ValidRoute[0].Clear();
                newTrain.ValidRoute[0] = null;
            }
            else
            {
                // ensure new trains route extends fully underneath train
                AdjustTrainRouteOnStart(newTrain.PresentPosition[0].RouteListIndex, newTrain.PresentPosition[1].RouteListIndex, this);
            }

            // build temp route for new train
            Train.TCSubpathRoute tempRouteNewTrain = Simulator.Signals.BuildTempRoute(newTrain, newTrain.PresentPosition[1].TCSectionIndex,
                newTrain.PresentPosition[1].TCOffset, newTrain.PresentPosition[1].TCDirection, newTrain.Length, false, true, false);

            // if train has no valid route, create from occupied sections
            if (newTrain.ValidRoute[0] == null)
            {
                newTrain.ValidRoute[0] = new Train.TCSubpathRoute(tempRouteNewTrain);
                newTrain.TCRoute.TCRouteSubpaths.Add(new Train.TCSubpathRoute(tempRouteNewTrain));
                newTrain.PresentPosition[0].RouteListIndex = newTrain.ValidRoute[0].GetRouteIndex(newTrain.PresentPosition[0].TCSectionIndex, 0);
                newTrain.PresentPosition[0].CopyTo(ref newTrain.PreviousPosition[0]);
                newTrain.PresentPosition[1].RouteListIndex = newTrain.ValidRoute[0].GetRouteIndex(newTrain.PresentPosition[1].TCSectionIndex, 0);
                newTrain.PresentPosition[1].CopyTo(ref newTrain.PreviousPosition[1]);
            }

            // set track section reserved - first reserve to ensure correct alignment of switches
            for (int iIndex = 0; iIndex < tempRouteNewTrain.Count; iIndex++)
            {
                TrackCircuitSection thisSection = Simulator.Signals.TrackCircuitList[tempRouteNewTrain[iIndex].TCSectionIndex];
                thisSection.Reserve(newTrain.routedForward, tempRouteNewTrain);
            }

            // set track section occupied
            for (int iIndex = 0; iIndex < tempRouteNewTrain.Count; iIndex++)
            {
                TrackCircuitSection thisSection = Simulator.Signals.TrackCircuitList[tempRouteNewTrain[iIndex].TCSectionIndex];
                thisSection.SetOccupied(newTrain.routedForward);
            }

            // update station stop offsets for continuing train
            RecalculateStationStops();

            // if normal stop, set restart delay
            if (MovementState == AI_MOVEMENT_STATE.STOPPED)
            {
                randDelay = (float)Simulator.Random.Next((DelayedStartSettings.detachRestart.randomPartS * 10));
                RestdelayS = DelayedStartSettings.detachRestart.fixedPartS + (randDelay / 10f);
                DelayedStart = true;
                DelayedStartState = AI_START_MOVEMENT.PATH_ACTION;
            }

            // return new lead locomotive position
            return (newLeadLocomotiveIndex);
        }

        //================================================================================================//
        /// <summary>
        /// Check if other train must be activated through trigger 
        /// </summary>
        /// <param name="thisTriggerType"></param>
        /// <param name="reqPlatformID"></param>
        /// <returns></returns>
        public void ActivateTriggeredTrain(TriggerActivationType thisTriggerType, int reqPlatformID)
        {
            for (int itrigger = activatedTrainTriggers.Count - 1; itrigger >= 0; itrigger--)
            {
                TriggerActivation thisTrigger = activatedTrainTriggers[itrigger];
                if (thisTrigger.activationType == thisTriggerType)
                {
                    if (thisTriggerType == TriggerActivationType.StationDepart || thisTriggerType == TriggerActivationType.StationStop)
                    {
                        if (thisTrigger.platformId != reqPlatformID)
                        {
                            continue;
                        }
                    }

                    TTTrain triggeredTrain = GetOtherTTTrainByNumber(thisTrigger.activatedTrain);
                    if (triggeredTrain == null)
                    {
                        triggeredTrain = AI.StartList.GetNotStartedTTTrainByNumber(thisTrigger.activatedTrain, false);
                    }

                    if (triggeredTrain != null)
                    {
                        triggeredTrain.TriggeredActivationRequired = false;
                    }
                    else
                    {
                        Trace.TraceInformation("Train to trigger : {0} not found for train {1}", thisTrigger.activatedName, Name);
                    }

                    activatedTrainTriggers.RemoveAt(itrigger);
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Adjust train route om start of train
        /// Front or rear may be off route as result of only partial overlap of old and new routes
        /// Can occur on form or couple/uncouple actions
        /// </summary>
        /// <param name="trainRearPositionIndex"></param>
        /// <param name="trainFrontPositionIndex"></param>
        /// <param name="oldTrain"></param>
        /// <returns></returns>
        public int AdjustTrainRouteOnStart(int trainRearPositionIndex, int trainFrontPositionIndex, TTTrain oldTrain)
        {
            int addedSections = 0;

            TrackCircuitSection[] occupiedSections = new TrackCircuitSection[OccupiedTrack.Count];
            OccupiedTrack.CopyTo(occupiedSections);

            // check if train is occupying end of route (happens when attaching) or front of route (happens when forming)
            bool addFront = false;

            int firstSectionIndex = ValidRoute[0][0].TCSectionIndex;
            foreach (TrackCircuitSection thisSection in OccupiedTrack)
            {
                if (thisSection.Index == firstSectionIndex)
                {
                    addFront = true;
                    break;
                }
            }

            // if start position not on route, add sections to route to cover
            if (trainRearPositionIndex < 0)
            {
                // add to front
                if (addFront)
                {
                    //ensure first section in route is occupied, otherwise do not add sections
                    int firstIndex = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][0].TCSectionIndex;
                    bool firstoccupied = false;
                    for (int iSection = occupiedSections.Length - 1; iSection >= 0 && !firstoccupied; iSection--)
                    {
                        TrackCircuitSection thisSection = occupiedSections[iSection];
                        firstoccupied = (thisSection.Index == firstIndex);
                    }

                    // create route for occupied sections if position is available

                    TCSubpathRoute tempRoute = null;
                    if (PresentPosition[1].TCSectionIndex >= 0)
                    {
                        tempRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, Length, true, true, false);
                    }

                    // add if first section is occupied
                    for (int iSection = occupiedSections.Length - 1; iSection >= 0 && firstoccupied; iSection--)
                    {
                        TrackCircuitSection thisSection = occupiedSections[iSection];
                        int routeIndex = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].GetRouteIndex(thisSection.Index, 0);

                        if (routeIndex < 0)
                        {
                            TCRouteElement newElement = null;

                            // try to use element from old route
                            int otherTrainRouteIndex = oldTrain.ValidRoute[0].GetRouteIndex(thisSection.Index, 0);
                            if (otherTrainRouteIndex >= 0)
                            {
                                newElement = new TCRouteElement(oldTrain.ValidRoute[0][otherTrainRouteIndex]);
                            }
                            // if failed and temp route available, try to use from temp route
                            else if (tempRoute != null)
                            {
                                otherTrainRouteIndex = tempRoute.GetRouteIndex(thisSection.Index, 0);
                                {

                                    if (otherTrainRouteIndex >= 0)
                                    {
                                        newElement = new TCRouteElement(tempRoute[otherTrainRouteIndex]);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }

                            TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Insert(0, newElement);
                            addedSections++;
                        }
                    }
                }
                // add to rear
                else
                {
                    //ensure last section in route is occupied, otherwise do not add sections
                    int lastIndex = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Last().TCSectionIndex;
                    bool lastoccupied = false;
                    for (int iSection = occupiedSections.Length - 1; iSection >= 0 && !lastoccupied; iSection--)
                    {
                        TrackCircuitSection thisSection = occupiedSections[iSection];
                        lastoccupied = (thisSection.Index == lastIndex);
                    }

                    // create route for occupied sections if position is available
                    TCSubpathRoute tempRoute = null;
                    if (PresentPosition[1].TCSectionIndex >= 0)
                    {
                        tempRoute = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                            PresentPosition[1].TCDirection, Length, true, true, false);
                    }

                    // add if last section is occupied
                    for (int iSection = 0; iSection < occupiedSections.Length && lastoccupied; iSection++)
                    {
                        TrackCircuitSection thisSection = occupiedSections[iSection];
                        int routeIndex = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].GetRouteIndex(thisSection.Index, 0);
                        if (routeIndex < 0)
                        {
                            TCRouteElement newElement = null;

                            // first try to add from old route
                            int otherTrainRouteIndex = oldTrain.ValidRoute[0].GetRouteIndex(thisSection.Index, 0);
                            if (otherTrainRouteIndex >= 0)
                            {
                                newElement = new TCRouteElement(oldTrain.ValidRoute[0][otherTrainRouteIndex]);
                            }
                            // if failed try from temp route if available
                            else if (tempRoute != null)
                            {
                                otherTrainRouteIndex = tempRoute.GetRouteIndex(thisSection.Index, 0);
                                {

                                    if (otherTrainRouteIndex >= 0)
                                    {
                                        newElement = new TCRouteElement(tempRoute[otherTrainRouteIndex]);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }

                            TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Add(newElement);
                            addedSections++;
                        }
                    }
                }

                ValidRoute[0] = new TCSubpathRoute(TCRoute.TCRouteSubpaths[TCRoute.activeSubpath]);

                // update section index references in TCroute data
                if (TCRoute.ReversalInfo[0].Valid)
                {
                    TCRoute.ReversalInfo[0].FirstDivergeIndex += addedSections;
                    TCRoute.ReversalInfo[0].FirstSignalIndex += addedSections;
                    TCRoute.ReversalInfo[0].LastDivergeIndex += addedSections;
                    TCRoute.ReversalInfo[0].LastSignalIndex += addedSections;
                }

                // update station stop indices
                if (StationStops != null)
                {
                    foreach (StationStop thisStation in StationStops)
                    {
                        if (thisStation.SubrouteIndex == 0)
                        {
                            thisStation.RouteIndex += addedSections;
                        }
                    }
                }
            }

            // if end position not on route, add sections to route to cover
            else if (trainFrontPositionIndex < 0)
            {
                if (addFront)
                {
                    //ensure first section in route is occupied, otherwise do not add sections
                    int firstIndex = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath][0].TCSectionIndex;
                    bool firstoccupied = false;
                    for (int iSection = occupiedSections.Length - 1; iSection >= 0 && !firstoccupied; iSection--)
                    {
                        TrackCircuitSection thisSection = occupiedSections[iSection];
                        firstoccupied = (thisSection.Index == firstIndex);
                    }

                    for (int iSection = occupiedSections.Length - 1; iSection >= 0 && firstoccupied; iSection--)
                    {
                        TrackCircuitSection thisSection = occupiedSections[iSection];
                        int routeIndex = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].GetRouteIndex(thisSection.Index, 0);
                        if (routeIndex < 0)
                        {
                            int otherTrainRouteIndex = oldTrain.ValidRoute[0].GetRouteIndex(thisSection.Index, 0);
                            TCRouteElement newElement = new TCRouteElement(oldTrain.ValidRoute[0][otherTrainRouteIndex]);
                            TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Insert(0, newElement);
                            addedSections++;
                        }
                    }
                }
                else
                {
                    //ensure last section in route is occupied, otherwise do not add sections
                    int lastIndex = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Last().TCSectionIndex;
                    bool lastoccupied = false;
                    for (int iSection = occupiedSections.Length - 1; iSection >= 0 && !lastoccupied; iSection--)
                    {
                        TrackCircuitSection thisSection = occupiedSections[iSection];
                        lastoccupied = (thisSection.Index == lastIndex);
                    }

                    for (int iSection = 0; iSection < occupiedSections.Length && lastoccupied; iSection++)
                    {
                        TrackCircuitSection thisSection = occupiedSections[iSection];
                        int routeIndex = TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].GetRouteIndex(thisSection.Index, 0);
                        if (routeIndex < 0)
                        {
                            int otherTrainRouteIndex = oldTrain.ValidRoute[0].GetRouteIndex(thisSection.Index, 0);
                            TCRouteElement newElement = new TCRouteElement(oldTrain.ValidRoute[0][otherTrainRouteIndex]);
                            TCRoute.TCRouteSubpaths[TCRoute.activeSubpath].Add(newElement);
                            addedSections++;
                        }
                    }
                }

                trainFrontPositionIndex = 0;
                ValidRoute[0] = new TCSubpathRoute(TCRoute.TCRouteSubpaths[TCRoute.activeSubpath]);

                // update section index references in TCroute data
                if (TCRoute.ReversalInfo[TCRoute.activeSubpath].Valid)
                {
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].FirstDivergeIndex += addedSections;
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].FirstSignalIndex += addedSections;
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].LastDivergeIndex += addedSections;
                    TCRoute.ReversalInfo[TCRoute.activeSubpath].LastSignalIndex += addedSections;
                }
                // update station stop indices
                if (StationStops != null)
                {
                    foreach (StationStop thisStation in StationStops)
                    {
                        if (thisStation.SubrouteIndex == 0)
                        {
                            thisStation.RouteIndex += addedSections;
                        }
                    }
                }
            }
            return (addedSections);
        }

        //================================================================================================//
        /// <summary>
        /// Create reference name for static train
        /// </summary>
        /// <param name="train"></param>
        /// <param name="trainlist"></param>
        /// <param name="reqName"></param>
        /// <param name="sectionInfo"></param>
        /// <returns></returns>
        public int CreateStaticTrainRef(TTTrain train, ref List<TTTrain> trainlist, string reqName, int sectionInfo, int seqNo)
        {
            TTTrain formedTrain = new TTTrain(train.Simulator, train);
            formedTrain.Name = String.Concat("S", train.Number.ToString("0000"), "_", seqNo.ToString("00"));
            formedTrain.FormedOf = train.Number;
            formedTrain.FormedOfType = FormCommand.Detached;
            formedTrain.TrainType = TRAINTYPE.AI_AUTOGENERATE;

            TrackCircuitSection DetachSection = signalRef.TrackCircuitList[sectionInfo];
            if (DetachSection.CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
            {
                DetachSection = signalRef.TrackCircuitList[DetachSection.Pins[0, 0].Link];
            }
            TrackNode DetachNode = train.Simulator.TDB.TrackDB.TrackNodes[DetachSection.OriginalIndex];

            formedTrain.RearTDBTraveller = new Traveller(train.Simulator.TSectionDat, train.Simulator.TDB.TrackDB.TrackNodes, DetachNode);

            trainlist.Add(formedTrain);

            return (formedTrain.Number);
        }

        //================================================================================================//
        /// <summary>
        /// Create static train
        /// </summary>
        /// <param name="train"></param>
        /// <param name="trainList"></param>
        /// <param name="reqName"></param>
        /// <param name="sectionInfo"></param>
        /// <returns></returns>
        public int CreateStaticTrain(TTTrain train, ref List<TTTrain> trainList, string reqName, int sectionInfo)
        {
            TTTrain formedTrain = new TTTrain(train.Simulator, train);
            TrackCircuitSection DetachSection = signalRef.TrackCircuitList[sectionInfo];
            TrackNode DetachNode = train.Simulator.TDB.TrackDB.TrackNodes[DetachSection.OriginalIndex];

            formedTrain.RearTDBTraveller = new Traveller(train.Simulator.TSectionDat, train.Simulator.TDB.TrackDB.TrackNodes, DetachNode);
            train.PresentPosition[0].CopyTo(ref formedTrain.PresentPosition[0]);
            train.PresentPosition[1].CopyTo(ref formedTrain.PresentPosition[1]);
            formedTrain.CreateRoute(true);
            if (formedTrain.TCRoute == null)
            {
                formedTrain.TCRoute = new TCRoutePath(formedTrain.ValidRoute[0]);
            }
            else
            {
                formedTrain.ValidRoute[0] = new Train.TCSubpathRoute(formedTrain.TCRoute.TCRouteSubpaths[0]);
            }

            formedTrain.AITrainDirectionForward = true;
            if (String.IsNullOrEmpty(reqName))
            {
                formedTrain.Name = String.Concat("D_", train.Number.ToString("0000"), "_", formedTrain.Number.ToString("00"));
            }
            else
            {
                formedTrain.Name = String.Copy(reqName);
            }
            formedTrain.FormedOf = train.Number;
            formedTrain.FormedOfType = TTTrain.FormCommand.Detached;
            formedTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
            formedTrain.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;

            // set starttime to 1 sec, and set activate time to null (train is never activated)
            formedTrain.StartTime = 1;
            formedTrain.ActivateTime = null;
            formedTrain.AI = train.AI;

            trainList.Add(formedTrain);
            return(formedTrain.Number);
        }

        //================================================================================================//
        /// <summary>
        /// Get other train from number
        /// Use Simulator.Trains to get other train
        /// </summary>

        public TTTrain GetOtherTTTrainByNumber(int reqNumber)
        {
            TTTrain returnTrain = Simulator.Trains.GetTrainByNumber(reqNumber) as TTTrain;

            // if not found, try if player train has required number as original number
            if (returnTrain == null)
            {
                TTTrain playerTrain = Simulator.Trains.GetTrainByNumber(0) as TTTrain;
                if (playerTrain.OrgAINumber == reqNumber)
                {
                    returnTrain = playerTrain;
                }
            }

            return (returnTrain);
        }

        //================================================================================================//
        /// <summary>
        /// Get other train from name
        /// Use Simulator.Trains to get other train
        /// </summary>

        public TTTrain GetOtherTTTrainByName(string reqName)
        {
            return (Simulator.Trains.GetTrainByName(reqName) as TTTrain);
        }

        //================================================================================================//
        /// <summary>
        /// Check if other train is yet to be started
        /// Use Simulator.Trains to get other train
        /// </summary>

        public bool CheckTTTrainNotStartedByNumber(int reqNumber)
        {
            bool notStarted = false;
            // check if on startlist
            if (Simulator.Trains.CheckTrainNotStartedByNumber(reqNumber))
            {
                notStarted = true;
            }
            // check if on autogen list
            else if (Simulator.AutoGenDictionary.ContainsKey(reqNumber))
            {
                notStarted = true;
            }
            // check if in process of being started
            else foreach (TTTrain thisTrain in AI.TrainsToAdd)
                {
                    if (thisTrain.Number == reqNumber)
                    {
                        notStarted = true;
                        break;
                    }
                }

            return (notStarted);
        }

        //================================================================================================//
        /// <summary>
        /// TTAnalys methods : dump methods for Timetable Analysis
        /// </summary>

        public void TTAnalysisUpdateStationState1(int presentTime, StationStop thisStation)
        {

            DateTime baseDTA = new DateTime();
            DateTime arrTimeA = baseDTA.AddSeconds(presentTime);
            DateTime depTimeA = baseDTA.AddSeconds(thisStation.ActualDepart);

            var sob = new StringBuilder();
            sob.AppendFormat("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12}",
                Number, AI.clockTime, Name, Delay, thisStation.PlatformItem.Name, thisStation.arrivalDT.ToString("HH:mm:ss"), thisStation.departureDT.ToString("HH:mm:ss"),
                arrTimeA.ToString("HH:mm:ss"), depTimeA.ToString("HH:mm:ss"), "", "", "", "");
            File.AppendAllText(@"C:\temp\TTAnalysis.csv", sob.ToString() + "\n");
        }

        public void TTAnalysisUpdateStationState2()
        {
            var signalstring = new StringBuilder();
            signalstring.AppendFormat("Signal : {0}", NextSignalObject[0].SignalHeads[0].TDBIndex);

            bool trainfound = false;
            var waitforstring = new StringBuilder();

            if (WaitList != null && WaitList.Count > 0 && WaitList[0].WaitActive)
            {
                waitforstring.AppendFormat("WAIT : {0} ({1})", WaitList[0].waitTrainNumber, WaitList[0].WaitType);
                trainfound = true;
            }

            for (int isection = PresentPosition[0].RouteListIndex + 1; isection <= ValidRoute[0].Count - 1 && !trainfound; isection++)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][isection].TCSectionIndex];
                if (thisSection.CircuitState.TrainOccupy.Count > 0)
                {
                    foreach (KeyValuePair<TrainRouted, int> traininfo in thisSection.CircuitState.TrainOccupy)
                    {
                        TrainRouted trainahead = traininfo.Key;
                        waitforstring.AppendFormat("Train occupying : {0}", trainahead.Train.Name);
                        trainfound = true;
                        break;
                    }
                }

                if (!trainfound && thisSection.CircuitState.TrainReserved != null)
                {
                    Train trainahead = thisSection.CircuitState.TrainReserved.Train;
                    if (trainahead != this)
                    {
                        waitforstring.AppendFormat("Train occupying : {0}", thisSection.CircuitState.TrainReserved.Train.Name);
                        trainfound = true;
                    }
                }

                if (!trainfound && thisSection.CircuitState.TrainClaimed.Count > 0)
                {
                    Train trainahead = thisSection.CircuitState.TrainClaimed.PeekTrain();
                    if (trainahead != this)
                    {
                        waitforstring.AppendFormat("Train claimed : {0}", trainahead.Name);
                        trainfound = true;
                    }
                }
            }

            DateTime baseDT = new DateTime();
            DateTime stopTime = baseDT.AddSeconds(AI.clockTime);

            var sob = new StringBuilder();
            sob.AppendFormat("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12}", Number, AI.clockTime, Name, Delay, "", "", "", "", "", "", stopTime.ToString("HH:mm:ss"), signalstring.ToString(), waitforstring.ToString());
            File.AppendAllText(@"C:\temp\TTAnalysis.csv", sob.ToString() + "\n");
        }

        public void TTAnalysisUpdateBrakingState1()
        {
            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_STOP)
            {
                var signalstring = new StringBuilder();
                signalstring.AppendFormat("Signal : {0}", NextSignalObject[0].SignalHeads[0].TDBIndex);

                bool trainfound = false;
                var waitforstring = new StringBuilder();

                if (WaitList != null && WaitList.Count > 0 && WaitList[0].WaitActive)
                {
                    waitforstring.AppendFormat("WAIT : {0} ({1})", WaitList[0].waitTrainNumber, WaitList[0].WaitType);
                    trainfound = true;
                }

                for (int isection = PresentPosition[0].RouteListIndex + 1; isection <= ValidRoute[0].Count - 1 && !trainfound; isection++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][isection].TCSectionIndex];
                    if (thisSection.CircuitState.TrainOccupy.Count > 0)
                    {
                        foreach (KeyValuePair<TrainRouted, int> traininfo in thisSection.CircuitState.TrainOccupy)
                        {
                            TrainRouted trainahead = traininfo.Key;
                            waitforstring.AppendFormat("Train occupying : {0}", trainahead.Train.Name);
                            trainfound = true;
                            break;
                        }
                    }

                    if (!trainfound && thisSection.CircuitState.TrainReserved != null)
                    {
                        Train trainahead = thisSection.CircuitState.TrainReserved.Train;
                        if (trainahead != this)
                        {
                            waitforstring.AppendFormat("Train occupying : {0}", thisSection.CircuitState.TrainReserved.Train.Name);
                            trainfound = true;
                        }
                    }

                    if (!trainfound && thisSection.CircuitState.TrainClaimed.Count > 0)
                    {
                        Train trainahead = thisSection.CircuitState.TrainClaimed.PeekTrain();
                        if (trainahead != this)
                        {
                            waitforstring.AppendFormat("Train claimed : {0}", trainahead.Name);
                            trainfound = true;
                        }
                    }
                }

                DateTime baseDT = new DateTime();
                DateTime stopTime = baseDT.AddSeconds(AI.clockTime);

                var sob = new StringBuilder();
                sob.AppendFormat("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12}", Number, AI.clockTime, Name, Delay, "", "", "", "", "", "", stopTime.ToString("HH:mm:ss"), signalstring.ToString(), waitforstring.ToString());
                File.AppendAllText(@"C:\temp\TTAnalysis.csv", sob.ToString() + "\n");
            }
        }

        public void TTAnalysisUpdateBrakingState2()
        {
            var signalstring = new StringBuilder();
            var waitforstring = new StringBuilder();

            if (NextSignalObject[0] != null)
            {
                signalstring.AppendFormat("Signal : {0}", NextSignalObject[0].SignalHeads[0].TDBIndex);

                bool trainfound = false;

                if (WaitList != null && WaitList.Count > 0 && WaitList[0].WaitActive)
                {
                    waitforstring.AppendFormat("WAIT : {0} ({1})", WaitList[0].waitTrainNumber, WaitList[0].WaitType);
                    trainfound = true;
                }

                for (int isection = PresentPosition[0].RouteListIndex + 1; isection <= ValidRoute[0].Count - 1 && !trainfound; isection++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[ValidRoute[0][isection].TCSectionIndex];
                    if (thisSection.CircuitState.TrainOccupy.Count > 0)
                    {
                        foreach (KeyValuePair<TrainRouted, int> traininfo in thisSection.CircuitState.TrainOccupy)
                        {
                            TrainRouted trainahead = traininfo.Key;
                            waitforstring.AppendFormat("Train occupying : {0}", trainahead.Train.Name);
                            trainfound = true;
                            break;
                        }
                    }

                    if (!trainfound && thisSection.CircuitState.TrainReserved != null)
                    {
                        Train trainahead = thisSection.CircuitState.TrainReserved.Train;
                        if (trainahead != this)
                        {
                            waitforstring.AppendFormat("Train occupying : {0}", thisSection.CircuitState.TrainReserved.Train.Name);
                            trainfound = true;
                        }
                    }

                    if (!trainfound && thisSection.CircuitState.TrainClaimed.Count > 0)
                    {
                        Train trainahead = thisSection.CircuitState.TrainClaimed.PeekTrain();
                        if (trainahead != this)
                        {
                            waitforstring.AppendFormat("Train claimed : {0}", trainahead.Name);
                            trainfound = true;
                        }
                    }
                }
            }
            else
            {
                signalstring.AppendFormat("Action : {0}", nextActionInfo.NextAction.ToString());
            }

            DateTime baseDT = new DateTime();
            DateTime stopTime = baseDT.AddSeconds(AI.clockTime);

            var sob = new StringBuilder();
            sob.AppendFormat("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12}", Number, AI.clockTime, Name, Delay, "", "", "", "", "", "", stopTime.ToString("HH:mm:ss"), signalstring.ToString(), waitforstring.ToString());
            File.AppendAllText(@"C:\temp\TTAnalysis.csv", sob.ToString() + "\n");
        }

        public void TTAnalysisStartMoving(String info)
        {
            DateTime baseDTA = new DateTime();
            DateTime moveTimeA = baseDTA.AddSeconds(AI.clockTime);

            var sob = new StringBuilder();
            sob.AppendFormat("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12}",
                Number, AI.clockTime, Name, Delay, "", "", "", "", "", moveTimeA.ToString("HH:mm:ss"), "", "", info);
            File.AppendAllText(@"C:\temp\TTAnalysis.csv", sob.ToString() + "\n");
        }
    }

    //================================================================================================//
    //================================================================================================//
    /// <summary>
    /// Class for waiting instructions
    /// <\summary>

    public class WaitInfo : IComparable<WaitInfo>
    {
        public enum WaitInfoType
        {
            Wait,
            Follow,
            WaitAny,
            Connect,
            Invalid,
        }

        public enum CheckPathDirection
        {
            Both,
            Same,
            Opposite,
        }

        // General info
        public WaitInfoType WaitType;                         // type of wait instruction
        public bool WaitActive;                               // wait state is active

        // preprocessed info - info is removed after processing
        public int startSectionIndex;                         // section from which command is valid (-1 if valid from start)
        public int startSubrouteIndex;                        // subpath index from which command is valid
        public string referencedTrainName;                    // referenced train name (for Wait, Follow or Connect)

        // processed info
        public int activeSectionIndex;                        // index of TrackCircuitSection where wait must be activated
        public int activeSubrouteIndex;                       // subpath in which this wait is valid
        public int activeRouteIndex;                          // index of section in active subpath

        // common for Wait, Follow and Connect
        public int waitTrainNumber;                           // number of train for which to wait
        public int? maxDelayS = null;                         // max. delay for waiting (in seconds)
        public int? ownDelayS = null;                         // min. own delay for waiting to be active (in seconds)
        public bool? notStarted = null;                       // also wait if not yet started
        public bool? atStart = null;                          // wait at start of wait section, otherwise wait at first not-common section
        public int? waittrigger = null;                       // time at which wait is triggered
        public int? waitendtrigger = null;                    // time at which wait is cancelled

        // wait types Wait and Follow :
        public int waitTrainSubpathIndex;                     // subpath index for train - set to -1 if wait is always active
        public int waitTrainRouteIndex;                       // index of section in active subpath

        // wait types Connect :
        public int stationIndex;                              // index in this train station stop list
        public int? holdTimeS;                                // required hold time (in seconds)

        // wait types WaitInfo (no post-processing required) :
        public Train.TCSubpathRoute CheckPath = null;         // required path to check in case of WaitAny

        public CheckPathDirection PathDirection = CheckPathDirection.Same; // required path direction

        //================================================================================================//
        /// <summary>
        /// Empty constructor
        /// </summary>
        public WaitInfo()
        {
        }

        //================================================================================================//
        /// <summary>
        /// Constructor for restore
        /// </summary>
        /// <param name="inf"></param>
        public WaitInfo(BinaryReader inf)
        {
            WaitType = (WaitInfoType)inf.ReadInt32();
            WaitActive = inf.ReadBoolean();

            activeSubrouteIndex = inf.ReadInt32();
            activeSectionIndex = inf.ReadInt32();
            activeRouteIndex = inf.ReadInt32();

            waitTrainNumber = inf.ReadInt32();
            int mdelayValue = inf.ReadInt32();
            if (mdelayValue < 0)
            {
                maxDelayS = null;
            }
            else
            {
                maxDelayS = mdelayValue;
            }

            int odelayValue = inf.ReadInt32();
            if (odelayValue < 0)
            {
                ownDelayS = null;
            }
            else
            {
                ownDelayS = odelayValue;
            }

            int notStartedValue = inf.ReadInt32();
            if (notStartedValue > 0)
            {
                notStarted = inf.ReadBoolean();
            }
            else
            {
                notStarted = null;
            }

            int atStartValue = inf.ReadInt32();
            if (atStartValue > 0)
            {
                atStart = inf.ReadBoolean();
            }
            else
            {
                atStart = null;
            }

            int triggervalue = inf.ReadInt32();
            if (triggervalue > 0)
            {
                waittrigger = triggervalue;
            }
            else
            {
                waittrigger = null;
            }

            int endtriggervalue = inf.ReadInt32();
            if (endtriggervalue > 0)
            {
                waitendtrigger = endtriggervalue;
            }
            else
            {
                waitendtrigger = null;
            }

            waitTrainSubpathIndex = inf.ReadInt32();
            waitTrainRouteIndex = inf.ReadInt32();

            stationIndex = inf.ReadInt32();
            int holdTimevalue = inf.ReadInt32();
            if (holdTimevalue < 0)
            {
                holdTimeS = null;
            }
            else
            {
                holdTimeS = holdTimevalue;
            }

            int validCheckPath = inf.ReadInt32();

            if (validCheckPath < 0)
            {
                CheckPath = null;
                PathDirection = CheckPathDirection.Same;
            }
            else
            {
                CheckPath = new Train.TCSubpathRoute(inf);
                PathDirection = (CheckPathDirection)inf.ReadInt32();
            }
        }

        //================================================================================================//
        /// <summary>
        /// Save
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write((int)WaitType);
            outf.Write(WaitActive);

            outf.Write(activeSubrouteIndex);
            outf.Write(activeSectionIndex);
            outf.Write(activeRouteIndex);

            outf.Write(waitTrainNumber);

            if (maxDelayS.HasValue)
            {
                outf.Write(maxDelayS.Value);
            }
            else
            {
                outf.Write(-1f);
            }

            if (ownDelayS.HasValue)
            {
                outf.Write(ownDelayS.Value);
            }
            else
            {
                outf.Write(-1f);
            }

            if (notStarted.HasValue)
            {
                outf.Write(1f);
                outf.Write(notStarted.Value);
            }
            else
            {
                outf.Write(-1f);
            }

            if (atStart.HasValue)
            {
                outf.Write(1f);
                outf.Write(atStart.Value);
            }
            else
            {
                outf.Write(-1f);
            }

            if (waittrigger.HasValue)
            {
                outf.Write(waittrigger.Value);
            }
            else
            {
                outf.Write(-1f);
            }

            if (waitendtrigger.HasValue)
            {
                outf.Write(waitendtrigger.Value);
            }
            else
            {
                outf.Write(-1f);
            }

            outf.Write(waitTrainSubpathIndex);
            outf.Write(waitTrainRouteIndex);

            outf.Write(stationIndex);

            if (holdTimeS.HasValue)
            {
                outf.Write(holdTimeS.Value);
            }
            else
            {
                outf.Write(-1f);
            }

            if (CheckPath == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(1);
                CheckPath.Save(outf);
                outf.Write((int)PathDirection);
            }
        }

        //================================================================================================//
        //
        // Compare To (to allow sort)
        //

        public int CompareTo(WaitInfo otherItem)
        {
            // all connects are moved to the end of the queue
            if (this.WaitType == WaitInfoType.Connect)
            {
                if (otherItem.WaitType != WaitInfoType.Connect)
                    return (1);
                return (0);
            }
            if (otherItem.WaitType == WaitInfoType.Connect)
                return (-1);

            if (this.activeSubrouteIndex < otherItem.activeSubrouteIndex)
                return (-1);
            if (this.activeSubrouteIndex == otherItem.activeSubrouteIndex && this.activeRouteIndex < otherItem.activeRouteIndex)
                return (-1);
            if (this.activeSubrouteIndex == otherItem.activeSubrouteIndex && this.activeRouteIndex == otherItem.activeRouteIndex)
                return (0);
            return (1);
        }

        //================================================================================================//
        /// <summary>
        /// Create full copy
        /// </summary>
        /// <returns></returns>
        public WaitInfo CreateCopy()
        {
            return ((WaitInfo)this.MemberwiseClone());
        }
    }

    //================================================================================================//
    //================================================================================================//
    /// <summary>
    /// Class for detach information
    /// </summary>
    public class DetachInfo
    {
        public enum DetachPositionInfo
        {
            atStart,
            atStation,
            atSection,
            atEnd,
            atActivation,
        }

        public enum DetachUnitsInfo
        {
            onlyPower,
            leadingPower,
            allLeadingPower,
            trailingPower,
            allTrailingPower,
            nonPower,
            unitsAtFront,
            unitsAtEnd,
            consists
        }

        public DetachPositionInfo DetachPosition;
        public int DetachSectionInfo;
        public DetachUnitsInfo DetachUnits;
        public List<String> DetachConsists;
        public int NumberOfUnits;
        public int DetachFormedTrain;                       // used for auto formed detaches
        public String DetachFormedTrainName = String.Empty;   // used for station and startup detaches
        public bool DetachFormedStatic;                     // used for station and startup detaches
        public bool? ReverseDetachedTrain;
        public bool PlayerAutoDetach;
        public int? DetachTime;
        public bool Valid;

        //================================================================================================//
        /// <summary>
        /// Default constructor for auto-generated detach
        /// </summary>
        /// <param name="atStart"></param>
        /// <param name="atEnd"></param>
        /// <param name="atStation"></param>
        /// <param name="sectionIndex"></param>
        /// <param name="leadingPower"></param>
        /// <param name="trailingPower"></param>
        /// <param name="units"></param>
        public DetachInfo(bool atStart, bool atEnd, bool atStation, int sectionIndex, bool leadingPower, bool allLeadingPower, bool trailingPower, bool allTrailingPower, 
            bool onlyPower, bool nonPower, int units, int? time, int formedTrain, bool reverseTrain)
        {
            if (atStart)
            {
                DetachPosition = DetachPositionInfo.atStart;
            }
            else if (atEnd)
            {
                DetachPosition = DetachPositionInfo.atEnd;
            }
            else if (atStation)
            {
                DetachPosition = DetachPositionInfo.atStation;
                DetachSectionInfo = sectionIndex;
            }
            else
            {
                DetachPosition = DetachPositionInfo.atSection;
                DetachSectionInfo = sectionIndex;
            }

            if (leadingPower)
            {
                DetachUnits = DetachUnitsInfo.leadingPower;
            }
            else if (allLeadingPower)
            {
                DetachUnits = DetachUnitsInfo.allLeadingPower;
            }
            else if (trailingPower)
            {
                DetachUnits = DetachUnitsInfo.trailingPower;
            }
            else if (allTrailingPower)
            {
                DetachUnits = DetachUnitsInfo.allTrailingPower;
            }
            else if (onlyPower)
            {
                DetachUnits = DetachUnitsInfo.onlyPower;
            }
            else if (nonPower)
            {
                DetachUnits = DetachUnitsInfo.nonPower;
            }
            else if (units < 0)
            {
                DetachUnits = DetachUnitsInfo.unitsAtEnd;
                NumberOfUnits = -units;
            }
            else
            {
                DetachUnits = DetachUnitsInfo.unitsAtFront;
                NumberOfUnits = units;
            }

            DetachTime = time;
            DetachFormedTrain = formedTrain;
            DetachFormedStatic = false;
            ReverseDetachedTrain = reverseTrain;
            PlayerAutoDetach = true;
            DetachConsists = null;
            Valid = true;
        }

        //================================================================================================//
        /// <summary>
        /// Default constructor for detach at station or at start
        /// </summary>
        /// <param name="thisTrain"></param>
        /// <param name="commandInfo"></param>
        /// <param name="atActivation"></param>
        /// <param name="atStation"></param>
        /// <param name="detachSectionIndex"></param>
        /// <param name="detachTime"></param>
        public DetachInfo(TTTrain thisTrain, TTTrainCommands commandInfo, bool atActivation, bool atStation, bool atForms, int detachSectionIndex, int? detachTime)
        {
            DetachPosition = atActivation ? DetachPositionInfo.atActivation : atStation ? DetachPositionInfo.atStation : atForms ? DetachPositionInfo.atEnd : DetachPositionInfo.atSection;
            DetachSectionInfo = detachSectionIndex;
            DetachFormedTrain = -1;
            PlayerAutoDetach = true;
            DetachConsists = null;
            DetachFormedTrainName = String.Empty;

            bool portionDefined = false;
            bool formedTrainDefined = false;

            foreach (TTTrainCommands.TTTrainComQualifiers Qualifier in commandInfo.CommandQualifiers)
            {
                switch (Qualifier.QualifierName.Trim().ToLower())
                {
                    // detach info qualifiers
                    case "power":
                        DetachUnits = DetachUnitsInfo.onlyPower;
                        portionDefined = true;
                        break;

                    case "leadingpower":
                        DetachUnits = DetachUnitsInfo.leadingPower;
                        portionDefined = true;
                        break;

                    case "allleadingpower":
                        DetachUnits = DetachUnitsInfo.allLeadingPower;
                        portionDefined = true;
                        break;

                    case "trailingpower":
                        DetachUnits = DetachUnitsInfo.trailingPower;
                        portionDefined = true;
                        break;

                    case "alltrailingpower":
                        DetachUnits = DetachUnitsInfo.allTrailingPower;
                        portionDefined = true;
                        break;

                    case "nonpower":
                        DetachUnits = DetachUnitsInfo.nonPower;
                        portionDefined = true;
                        break;

                    case "units":
                        int nounits = 0;
                        bool unitvalid = false;

                        try
                        {
                            nounits = Convert.ToInt32(Qualifier.QualifierValues[0]);
                            unitvalid = true;
                        }
                        catch
                        {
                            Trace.TraceInformation("Train {0} : invalid value for units qualifier in detach command : {1} \n", thisTrain.Name, Qualifier.QualifierValues[0]);
                        }

                        if (unitvalid)
                        {
                            if (nounits > 0)
                            {
                                DetachUnits = DetachUnitsInfo.unitsAtFront;
                            }
                            else
                            {
                                DetachUnits = DetachUnitsInfo.unitsAtEnd;
                            }
                            NumberOfUnits = Math.Abs(nounits);
                            portionDefined = true;
                        }
                        break;

                    case "consist":
                        DetachUnits = DetachUnitsInfo.consists;

                        if (DetachConsists == null) DetachConsists = new List<string>();

                        foreach (String consistname in Qualifier.QualifierValues)
                        {
                            DetachConsists.Add(consistname);
                        }
                        portionDefined = true;
                        break;

                    // form qualifier
                    case "forms":
                        if (Qualifier.QualifierValues == null || Qualifier.QualifierValues.Count <= 0)
                        {
                            Trace.TraceInformation("Train {0} : detach command : missing name for formed train", thisTrain.Name);
                        }
                        else
                        {
                            DetachFormedTrainName = Qualifier.QualifierValues[0];
                            if (!DetachFormedTrainName.Contains(":"))
                            {
                                int seppos = thisTrain.Name.IndexOf(':');
                                DetachFormedTrainName = String.Concat(DetachFormedTrainName, ":", thisTrain.Name.Substring(seppos + 1).ToLower());
                            }

                            DetachFormedStatic = false;
                            formedTrainDefined = true;
                        }
                        break;

                    // static qualifier
                    case "static":
                        if (Qualifier.QualifierValues != null && Qualifier.QualifierValues.Count > 0)
                        {
                            DetachFormedTrainName = Qualifier.QualifierValues[0];
                            if (!DetachFormedTrainName.Contains(":"))
                            {
                                int seppos = thisTrain.Name.IndexOf(':');
                                DetachFormedTrainName = String.Concat(DetachFormedTrainName, ":", thisTrain.Name.Substring(seppos + 1).ToLower());
                            }
                        }

                        DetachFormedStatic = true;
                        formedTrainDefined = true;
                        break;

                    // manual or auto detach for player train (note : not yet implemented)
                    case "manual":
                        PlayerAutoDetach = false;
                        break;

                    case "auto":
                        PlayerAutoDetach = true;
                        break;

                    // default : invalid qualifier
                    default:
                        Trace.TraceWarning("Train {0} : invalid qualifier for detach command : {1}", thisTrain.Name, Qualifier.QualifierName.Trim());
                        break;
                }
            }

            // set detach time to arrival time or activate time
            DetachTime = detachTime.HasValue ? detachTime.Value : 0;

            if (!portionDefined)
            {
                Trace.TraceWarning("Train {0} : detach command : missing portion information", thisTrain.Name);
                Valid = false;
            }
            else if (!formedTrainDefined)
            {
                Trace.TraceWarning("Train {0} : detach command : no train defined for detached portion", thisTrain.Name);
                Valid = false;
            }
            else
            {
                Valid = true;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Constructor for Restore
        /// </summary>
        /// <param name="inf"></param>
        public DetachInfo(BinaryReader inf)
        {
            DetachPosition = (DetachPositionInfo)inf.ReadInt32();
            DetachSectionInfo = inf.ReadInt32();
            DetachUnits = (DetachUnitsInfo)inf.ReadInt32();
            NumberOfUnits = inf.ReadInt32();
            int detachTimeValue = inf.ReadInt32();
            if (detachTimeValue < 0)
            {
                DetachTime = null;
            }
            else
            {
                DetachTime = detachTimeValue;
            }

            DetachFormedTrain = inf.ReadInt32();
            DetachFormedTrainName = inf.ReadString();
            bool RevDetTrainValid = inf.ReadBoolean();
            if (RevDetTrainValid) ReverseDetachedTrain = inf.ReadBoolean();
            PlayerAutoDetach = inf.ReadBoolean();
            int noConstistStrings = inf.ReadInt32();
            if (noConstistStrings <= 0)
            {
                DetachConsists = null;
            }
            else
            {
                DetachConsists = new List<string>();
                for (int cstring = 1; cstring <= noConstistStrings; cstring++)
                {
                    DetachConsists.Add(inf.ReadString());
                }
            }

            DetachFormedStatic = inf.ReadBoolean();
            Valid = inf.ReadBoolean();
        }

        //================================================================================================//
        /// <summary>
        /// Save routine
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write((int)DetachPosition);
            outf.Write(DetachSectionInfo);
            outf.Write((int)DetachUnits);
            outf.Write(NumberOfUnits);
            if (DetachTime.HasValue)
            {
                outf.Write(DetachTime.Value);
            }
            else
            {
                outf.Write(-1);
            }
            outf.Write(DetachFormedTrain);
            outf.Write(DetachFormedTrainName);
            if (ReverseDetachedTrain.HasValue)
            {
                outf.Write(true);
                outf.Write(ReverseDetachedTrain.Value);
            }
            else
            {
                outf.Write(false);
            }
            outf.Write(PlayerAutoDetach);

            if (DetachConsists == null || DetachConsists.Count == 0)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(DetachConsists.Count);
                foreach (String cString in DetachConsists)
                {
                    outf.Write(cString);
                }
            }

            outf.Write(DetachFormedStatic);
            outf.Write(Valid);
        }

        //================================================================================================//
        /// <summary>
        /// Perform detach, return state : true if detach may be performed, false if detach is handled through window
        /// </summary>
        /// <param name="train"></param>
        /// <param name="presentTime"></param>
        /// <returns></returns>
        public bool PerformDetach(TTTrain train, bool allowPlayerSelect)
        {
            // Determine no. of units to detach

            int iunits = 0;
            bool frontpos = true;

            // if position of power not defined, set position according to present position of power
            if (DetachUnits == DetachUnitsInfo.onlyPower)
            {
                DetachUnits = DetachUnitsInfo.allLeadingPower;
                if (train.Cars[0].WagonType == TrainCar.WagonTypes.Engine || train.Cars[0].WagonType == TrainCar.WagonTypes.Tender)
                {
                    DetachUnits = DetachUnitsInfo.allLeadingPower;
                }
                else
                {
                    DetachUnits = DetachUnitsInfo.allTrailingPower;
                }
            }

            iunits = train.GetUnitsToDetach(this.DetachUnits, this.NumberOfUnits, this.DetachConsists, ref frontpos);

            if (train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Detaching from : " + train.Number + " ; units : " + iunits + " ; from front position : " + frontpos.ToString() + "\n");
            }

            // check if anything to detach and anything left on train

            TTTrain newTrain = null;
            if (DetachFormedTrain == 0)
            {
                newTrain = train.Simulator.PlayerLocomotive.Train as TTTrain;
            }
            else
            {
                newTrain = train.AI.Simulator.GetAutoGenTTTrainByNumber(DetachFormedTrain);
            }

            if (newTrain == null)
            {
                Trace.TraceInformation("Train {0} : detach to train {1} : cannot find new train \n", train.Name, DetachFormedTrainName);
            }

            train.DetachUnits = iunits;
            train.DetachPosition = frontpos;

            if (iunits == 0)
            {
                Trace.TraceInformation("Train {0} : detach to train {1} : no units to detach \n", train.Name, DetachFormedTrainName);
                if (train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Detaching from : " + train.Number + " to " + DetachFormedTrainName + " no units to detach \n");
                }
            }
            else if (iunits == train.Cars.Count)
            {
                Trace.TraceInformation("Train {0} : detach to train {1} : no units remaining on train \n", train.Name, DetachFormedTrainName);
                if (train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Detaching from : " + train.Number + " to " + DetachFormedTrainName + " no units remaining on train \n");
                }
            }
            else
            {
                if (newTrain == null)
                {
                    // create dummy train - train will be removed but timetable can continue
                    newTrain = new TTTrain(train.AI.Simulator, train);
                    newTrain.AI = train.AI;  // set AT as Simulator.AI does not exist in prerun mode
                    newTrain.ValidRoute[0] = train.signalRef.BuildTempRoute(newTrain, train.PresentPosition[0].TCSectionIndex, 
                        train.PresentPosition[0].TCOffset, train.PresentPosition[0].TCDirection, train.Length, true, true, false);
                    train.PresentPosition[0].CopyTo(ref newTrain.PresentPosition[0]);
                    train.PresentPosition[1].CopyTo(ref newTrain.PresentPosition[1]);

                    ReverseDetachedTrain = false;
                    int newLocoIndex = train.TTUncoupleBehind(newTrain, ReverseDetachedTrain.Value, train.LeadLocomotiveIndex, false);
                    newTrain.RemoveTrain();
                }
                else
                {
                    // if new train has no route, create from present position
                    if (newTrain.TCRoute == null)
                    {
                        Train.TCSubpathRoute newTrainPath = new Train.TCSubpathRoute(train.ValidRoute[0], train.PresentPosition[1].RouteListIndex, train.PresentPosition[0].RouteListIndex);
                        newTrain.TCRoute = new Train.TCRoutePath(newTrainPath);
                        newTrain.ValidRoute[0] = new Train.TCSubpathRoute(newTrain.TCRoute.TCRouteSubpaths[0]);
                    }

                    // handle player train
                    if (train.TrainType == Train.TRAINTYPE.PLAYER)
                    {
                        bool detachablePower = CheckDetachedDriveablePower(train);
                        bool keepPower = CheckKeepDriveablePower(train);

                        // if both portions contain detachable power, display window
                        // quit method as detaching is handled through window button activation
                        if (detachablePower && keepPower && allowPlayerSelect)
                        {
                            // reinsert newtrain as autogen train otherwise it cannot be found
                            train.AI.AutoGenTrains.Add(newTrain);
                            train.Simulator.AutoGenDictionary.Add(newTrain.Number, newTrain);

                            // show window, set detach action is pending
                            train.Simulator.OnRequestTTDetachWindow();
                            train.DetachPending = true;
                            return (false);
                        }

                        // detachable portion has no power, so detach immediately

                        bool playerEngineInRemainingPortion = CheckPlayerPowerPortion(train);

                        // player engine is in remaining portion
                        if (playerEngineInRemainingPortion)
                        {
                            if (!ReverseDetachedTrain.HasValue)
                            {
                                ReverseDetachedTrain = GetDetachReversalInfo(train, newTrain);
                            }
                            int newLocoIndex = train.TTUncoupleBehind(newTrain, ReverseDetachedTrain.Value, train.LeadLocomotiveIndex, false);
                            train.LeadLocomotiveIndex = newLocoIndex;
                            train.Simulator.Confirmer.Information(train.DetachUnits.ToString() + " units detached as train : " + newTrain.Name);
                            train.DetachActive[1] = -1;

                            // set proper details for new train
                            newTrain.SetFormedOccupied();
                            newTrain.ControlMode = Train.TRAIN_CONTROL.INACTIVE;
                            newTrain.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;
                            newTrain.SetupStationStopHandling();
                        }
                        // keep portion has no power, so detach immediately and switch to new train
                        else
                        {
                            if (!ReverseDetachedTrain.HasValue)
                            {
                                ReverseDetachedTrain = GetDetachReversalInfo(train, newTrain);
                            }
                            int newLocoIndex = train.TTUncoupleBehind(newTrain, ReverseDetachedTrain.Value, train.LeadLocomotiveIndex, true);
                            train.Simulator.Confirmer.Information(train.DetachUnits.ToString() + " units detached as train : " + newTrain.Name);
                            Trace.TraceInformation("Detach : " + train.DetachUnits.ToString() + " units detached as train : " + newTrain.Name + "\n");
                            train.DetachActive[1] = -1;

                            // set proper details for existing train
                            train.Number = train.OrgAINumber;
                            train.TrainType = Train.TRAINTYPE.AI;
                            train.LeadLocomotiveIndex = -1;
                            train.Simulator.Trains.Remove(train);
                            train.AI.TrainsToRemoveFromAI.Add(train);
                            train.AI.TrainsToAdd.Add(train);

                            // set proper details for new train
                            newTrain.AI.TrainsToRemoveFromAI.Add(newTrain);

                            // set proper details for new formed train
                            newTrain.OrgAINumber = newTrain.Number;
                            newTrain.Number = 0;
                            newTrain.LeadLocomotiveIndex = newLocoIndex;
                            newTrain.AI.TrainsToAdd.Add(newTrain);
                            newTrain.AI.aiListChanged = true;
                            newTrain.Simulator.Trains.Add(newTrain);

                            newTrain.SetFormedOccupied();
                            newTrain.TrainType = Train.TRAINTYPE.PLAYER;
                            newTrain.ControlMode = Train.TRAIN_CONTROL.INACTIVE;
                            newTrain.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;

                            // inform viewer about player train switch
                            train.Simulator.OnPlayerTrainChanged(train, newTrain);
                            train.Simulator.PlayerLocomotive.Train = newTrain;

                            newTrain.SetupStationStopHandling();

                            // clear replay commands
                            train.Simulator.Log.CommandList.Clear();

                            // display messages
                            if (train.Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                                train.Simulator.Confirmer.Information("Player switched to train : " + newTrain.Name);

                        }
                    }
                    else
                    {
                        // handle AI train
                        if (train.AI.Simulator.AutoGenDictionary != null && train.AI.Simulator.AutoGenDictionary.ContainsKey(newTrain.Number))
                            train.AI.Simulator.AutoGenDictionary.Remove(newTrain.Number);

                        if (!ReverseDetachedTrain.HasValue)
                        {
                            ReverseDetachedTrain = GetDetachReversalInfo(train, newTrain);
                        }

                        bool newIsPlayer = newTrain.TrainType == Train.TRAINTYPE.INTENDED_PLAYER;
                        newTrain.LeadLocomotiveIndex = train.TTUncoupleBehind(newTrain, ReverseDetachedTrain.Value, -1, newIsPlayer);
                        train.DetachActive[1] = -1;
                    }

                    if (train.CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt",
                            "Detaching from : " + train.Number + "( = " + newTrain.Name + ") ; to : " + newTrain.Number + "( = " + newTrain.Name + ")\n");
                    }
                }


                // if train is player or intended player, determine new loco lead index
                if (train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
                {
                    if (train.LeadLocomotiveIndex >= 0)
                    {
                        train.LeadLocomotive = train.Simulator.PlayerLocomotive = train.Cars[train.LeadLocomotiveIndex];
                    }
                    else
                    {
                        train.LeadLocomotive = null;
                        train.Simulator.PlayerLocomotive = null;

                        for (int iCar = 0; iCar <= train.Cars.Count - 1 && train.LeadLocomotiveIndex < 0; iCar++)
                        {
                            var eachCar = train.Cars[iCar];
                            if (eachCar.IsDriveable)
                            {
                                train.LeadLocomotive = train.Simulator.PlayerLocomotive = eachCar;
                                train.LeadLocomotiveIndex = iCar;
                            }
                        }
                    }
                }

                else if (newTrain.TrainType == Train.TRAINTYPE.PLAYER || newTrain.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
                {
                    newTrain.TrainType = Train.TRAINTYPE.PLAYER;
                    newTrain.ControlMode = Train.TRAIN_CONTROL.INACTIVE;
                    newTrain.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;
                    if (!newTrain.StartTime.HasValue) newTrain.StartTime = 0;

                    newTrain.AI.TrainsToAdd.Add(newTrain);

                    if (newTrain.LeadLocomotiveIndex >= 0)
                    {
                        newTrain.LeadLocomotive = newTrain.Simulator.PlayerLocomotive = newTrain.Cars[newTrain.LeadLocomotiveIndex];
                    }
                    else
                    {
                        newTrain.LeadLocomotive = null;
                        newTrain.Simulator.PlayerLocomotive = null;

                        for (int iCar = 0; iCar <= newTrain.Cars.Count - 1 && newTrain.LeadLocomotiveIndex < 0; iCar++)
                        {
                            var eachCar = newTrain.Cars[iCar];
                            if (eachCar.IsDriveable)
                            {
                                newTrain.LeadLocomotive = newTrain.Simulator.PlayerLocomotive = eachCar;
                                newTrain.LeadLocomotiveIndex = iCar;
                            }
                        }
                    }
                }
            }

            return(true);
        }

        //================================================================================================//
        /// <summary>
        /// Perform detach for player train
        /// Called from player detach selection window
        /// </summary>
        /// <param name="train"></param>
        /// <param name="newTrainNumber"></param>
        public void DetachPlayerTrain(TTTrain train, int newTrainNumber)
        {
            // Determine no. of units to detach

            int iunits = 0;
            bool frontpos = true;

            // if position of power not defined, set position according to present position of power
            if (DetachUnits == DetachUnitsInfo.onlyPower)
            {
                DetachUnits = DetachUnitsInfo.allLeadingPower;
                if (train.Cars[0].WagonType == TrainCar.WagonTypes.Engine || train.Cars[0].WagonType == TrainCar.WagonTypes.Tender)
                {
                    DetachUnits = DetachUnitsInfo.allLeadingPower;
                }
                else
                {
                    DetachUnits = DetachUnitsInfo.allTrailingPower;
                }
            }

            iunits = train.GetUnitsToDetach(this.DetachUnits, this.NumberOfUnits, this.DetachConsists, ref frontpos);

            if (train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Detaching from : " + train.Number + " ; units : " + iunits + " ; from front position : " + frontpos.ToString() + "\n");
            }

            // check if anything to detach and anything left on train

            train.DetachUnits = iunits;
            train.DetachPosition = frontpos;

            TTTrain newTrain = train.AI.Simulator.GetAutoGenTTTrainByNumber(newTrainNumber);
            if (newTrain == null)
            {
                newTrain = train.AI.StartList.GetNotStartedTTTrainByNumber(newTrainNumber, true);
            }
            bool playerEngineInRemainingPortion = CheckPlayerPowerPortion(train);

            ReverseDetachedTrain = GetDetachReversalInfo(train, newTrain);

            int newLocoIndex = train.TTUncoupleBehind(newTrain, ReverseDetachedTrain.Value, train.LeadLocomotiveIndex, !playerEngineInRemainingPortion);
            train.Simulator.Confirmer.Information(train.DetachUnits.ToString() + " units detached as train : " + newTrain.Name);
            Trace.TraceInformation(train.DetachUnits.ToString() + " units detached as train : " + newTrain.Name);
            train.DetachActive[1] = -1;

            // player engine is in remaining portion
            if (playerEngineInRemainingPortion)
            {
                train.LeadLocomotiveIndex = newLocoIndex;
            }

            // player engine is in detached portion, so switch trains
            else
            {
                // set proper details for existing train
                train.Number = train.OrgAINumber;
                train.TrainType = Train.TRAINTYPE.AI;
                train.MovementState = train.AtStation ? AITrain.AI_MOVEMENT_STATE.STATION_STOP : AITrain.AI_MOVEMENT_STATE.STOPPED;
                train.LeadLocomotiveIndex = -1;
                train.Simulator.Trains.Remove(train);
                train.AI.TrainsToRemoveFromAI.Add(train);
                train.AI.TrainsToAdd.Add(train);
                train.MUDirection = Direction.Forward;

                // set proper details for new formed train
                newTrain.AI.TrainsToRemoveFromAI.Add(newTrain);

                newTrain.OrgAINumber = newTrain.Number;
                newTrain.Number = 0;
                newTrain.LeadLocomotiveIndex = newLocoIndex;
                newTrain.TrainType = Train.TRAINTYPE.PLAYER;
                newTrain.ControlMode = Train.TRAIN_CONTROL.INACTIVE;
                newTrain.MovementState = AITrain.AI_MOVEMENT_STATE.AI_STATIC;
                newTrain.AI.TrainsToAdd.Add(newTrain);
                newTrain.AI.aiListChanged = true;
                newTrain.Simulator.Trains.Add(newTrain);

                newTrain.SetFormedOccupied();

                // inform viewer about player train switch
                train.Simulator.OnPlayerTrainChanged(train, newTrain);
                train.Simulator.PlayerLocomotive.Train = newTrain;

                newTrain.SetupStationStopHandling();

                // clear replay commands
                train.Simulator.Log.CommandList.Clear();

                // display messages
                if (train.Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    train.Simulator.Confirmer.Information("Player switched to train : " + newTrain.Name);
                Trace.TraceInformation("Player switched to train : " + newTrain.Name);
            }

            train.DetachPending = false;   // detach completed
        }

        //================================================================================================//
        /// <summary>
        /// Set detach cross-reference train information
        /// </summary>
        /// <param name="dettrain"></param>
        /// <param name="trainList"></param>
        /// <param name="playerTrain"></param>
        public void SetDetachXRef(TTTrain dettrain, List<TTTrain> trainList, TTTrain playerTrain)
        {
            bool trainFound = false;

            foreach (TTTrain otherTrain in trainList)
            {
                if (String.Compare(otherTrain.Name, DetachFormedTrainName, true) == 0)
                {
                    if (otherTrain.FormedOf >= 0)
                    {
                        Trace.TraceWarning("Train : {0} : detach details : detached train {1} already formed out of another train",
                            dettrain.Name, otherTrain.Name);
                        break;
                    }

                    otherTrain.FormedOf = dettrain.Number;
                    otherTrain.FormedOfType = TTTrain.FormCommand.Detached;
                    otherTrain.TrainType = Train.TRAINTYPE.AI_AUTOGENERATE;
                    DetachFormedTrain = otherTrain.Number;
                    trainFound = true;

                    break;
                }
            }

            // if not found, try player train
            if (!trainFound)
            {
                if (playerTrain != null && String.Compare(playerTrain.Name, DetachFormedTrainName, true) == 0)
                {
                    if (playerTrain.FormedOf >= 0)
                    {
                        Trace.TraceWarning("Train : {0} : detach details : detached train {1} already formed out of another train",
                            dettrain.Name, playerTrain.Name);
                    }

                    playerTrain.FormedOf = dettrain.Number;
                    playerTrain.FormedOfType = TTTrain.FormCommand.Detached;
                    DetachFormedTrain = playerTrain.Number;
                    trainFound = true;
                }
            }

            // issue warning if train not found
            if (!trainFound)
            {
                Trace.TraceWarning("Train :  {0} : detach details : detached train {1} not found",
                    dettrain.Name, DetachFormedTrainName);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check if detached portion off player train has driveable power
        /// </summary>
        /// <param name="train"></param>
        /// <returns></returns>
        public bool CheckDetachedDriveablePower(TTTrain train)
        {
            bool portionHasDriveablePower = false;

            // detach at front
            if (train.DetachPosition)
            {
                for (int iCar = 0; iCar < train.DetachUnits && !portionHasDriveablePower; iCar++)
                {
                    var thisCar = train.Cars[iCar];
                    if (thisCar.IsDriveable) portionHasDriveablePower = true;
                }
            }
            // detach at rear
            else
            {
                for (int iCar = 0; iCar < train.DetachUnits && !portionHasDriveablePower; iCar++)
                {
                    int actCar = train.Cars.Count - 1 - iCar;
                    var thisCar = train.Cars[actCar];
                    if (thisCar.IsDriveable) portionHasDriveablePower = true;
                }
            }
            return (portionHasDriveablePower);
        }

        //================================================================================================//
        /// <summary>
        /// Check if remaining portion off player train has driveable power
        /// </summary>
        /// <param name="train"></param>
        /// <returns></returns>
        public bool CheckKeepDriveablePower(TTTrain train)
        {
            bool portionHasDriveablePower = false;

            // detach at front - so check rear portion
            if (train.DetachPosition)
            {
                for (int iCar = 0; iCar < (train.Cars.Count - train.DetachUnits) && !portionHasDriveablePower; iCar++)
                {
                    int actCar = train.Cars.Count - 1 - iCar;
                    var thisCar = train.Cars[actCar];
                    if (thisCar.IsDriveable) portionHasDriveablePower = true;
                }
            }
            // detach at rear - so check front portion
            else
            {
                for (int iCar = 0; iCar < (train.Cars.Count - train.DetachUnits) && !portionHasDriveablePower; iCar++)
                {
                    var thisCar = train.Cars[iCar];
                    if (thisCar.IsDriveable) portionHasDriveablePower = true;
                }
            }
            return (portionHasDriveablePower);
        }

        //================================================================================================//
        /// <summary>
        /// Check if player engine is in remaining or detached portion
        /// </summary>
        /// <param name="train"></param>
        /// <returns></returns>
        public bool CheckPlayerPowerPortion(TTTrain train)
        {
            bool PlayerInRemainingPortion = false;

            // detach at front - so check rear portion
            if (train.DetachPosition)
            {
                for (int iCar = 0; iCar < (train.Cars.Count - train.DetachUnits) && !PlayerInRemainingPortion; iCar++)
                {
                    int actCar = train.Cars.Count - 1 - iCar;
                    if (actCar == train.LeadLocomotiveIndex)
                    {
                        PlayerInRemainingPortion = true;
                    }
                }
            }
            // detach at rear - so check front portion
            else
            {
                for (int iCar = 0; iCar < (train.Cars.Count - train.DetachUnits) && !PlayerInRemainingPortion; iCar++)
                {
                    if (iCar == train.LeadLocomotiveIndex)
                    {
                        PlayerInRemainingPortion = true;
                    }
                }
            }
            return (PlayerInRemainingPortion);
        }

        //================================================================================================//
        /// <summary>
        /// Determine if detached train is to be reversed
        /// </summary>
        /// <param name="thisTrain"></param>
        /// <param name="detachedTrain"></param>
        /// <returns></returns>
        public bool GetDetachReversalInfo(TTTrain thisTrain, TTTrain detachedTrain)
        {
            bool reversed = false;

            // if detached at front, use front position
            if (thisTrain.DetachPosition)
            {
                int frontSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
                int thisDirection = thisTrain.PresentPosition[0].TCDirection;

                Train.TCSubpathRoute otherPath = detachedTrain.TCRoute.TCRouteSubpaths[0];
                int otherTrainIndex = otherPath.GetRouteIndex(frontSectionIndex, 0);

                if (otherTrainIndex >= 0)
                {
                    int otherTrainDirection = otherPath[otherTrainIndex].Direction;
                    reversed = (thisDirection != otherTrainDirection);
                }
            }
            else
            {
                int frontSectionIndex = thisTrain.PresentPosition[1].TCSectionIndex;
                int thisDirection = thisTrain.PresentPosition[1].TCDirection;

                Train.TCSubpathRoute otherPath = detachedTrain.TCRoute.TCRouteSubpaths[0];
                int otherTrainIndex = otherPath.GetRouteIndex(frontSectionIndex, 0);

                if (otherTrainIndex >= 0)
                {
                    int otherTrainDirection = otherPath[otherTrainIndex].Direction;
                    reversed = (thisDirection != otherTrainDirection);
                }
            }

            return (reversed);
        }
    }

    //================================================================================================//
    //================================================================================================//
    /// <summary>
    /// Class AttachInfo : class for attach details
    /// </summary>
    public class AttachInfo
    {
        public int AttachTrain;                             // train number to which to attach
        public string AttachTrainName = String.Empty;       // train name to which to attach

        public int StationPlatformReference;                // station platform reference - set to -1 if attaching to static train in dispose command
        public bool FirstIn;                                // this train arrives first
        public bool SetBack;                                // reverse in order to attach
        public bool Valid;                                  // attach info is valid

        public bool ReadyToAttach;                          // trains are ready to attach

        //================================================================================================//
        /// <summary>
        /// Constructor for attach details at station
        /// </summary>
        /// <param name="stationPlatformReference"></param>
        /// <param name="thisCommand"></param>
        /// <param name="thisTrain"></param>
        public AttachInfo(int stationPlatformReference, TTTrainCommands thisCommand, TTTrain thisTrain)
        {
            FirstIn = false;
            SetBack = false;
            ReadyToAttach = false;

            StationPlatformReference = stationPlatformReference;

            if (thisCommand.CommandValues == null || thisCommand.CommandValues.Count <= 0)
            {
                Trace.TraceInformation("Train {0} : missing train name in attach command", thisTrain.Name);
                Valid = false;
                return;
            }

            AttachTrainName = String.Copy(thisCommand.CommandValues[0]);
            if (!AttachTrainName.Contains(":"))
            {
                int seppos = thisTrain.Name.IndexOf(':');
                AttachTrainName = String.Concat(AttachTrainName, ":", thisTrain.Name.Substring(seppos + 1).ToLower());
            }

            if (thisCommand.CommandQualifiers != null)
            {
                foreach (TTTrainCommands.TTTrainComQualifiers thisQualifier in thisCommand.CommandQualifiers)
                {
                    switch (thisQualifier.QualifierName)
                    {
                        case "firstin":
                            if (StationPlatformReference < 0)
                            {
                                Trace.TraceWarning("Train {0} : dispose attach command : FirstIn not allowed for dispose command", thisTrain.Name);
                            }
                            else
                            {
                                FirstIn = true;
                            }
                            break;

                        case "setback":
                            if (StationPlatformReference < 0)
                            {
                                Trace.TraceWarning("Train {0} : dispose attach command : SetBack not allowed for dispose command", thisTrain.Name);
                            }
                            else
                            {
                                SetBack = true;
                                FirstIn = true;
                            }
                            break;

                        default:
                            Trace.TraceWarning("Train {0} : Invalid qualifier for attach command : {1}", thisTrain.Name, thisQualifier.QualifierName);
                            break;
                    }
                }

            }

            // straight forward attach in station without first in and no set back : set no need to store (attach can be set directly)
            if (!FirstIn && !SetBack && StationPlatformReference >= 0)
            {
                ReadyToAttach = true;
            }

            Valid = true;
        }

        //================================================================================================//
        /// <summary>
        /// Contructor for attach at dispose
        /// </summary>
        /// <param name="rrtrain"></param>
        public AttachInfo(TTTrain rrtrain)
        {
            AttachTrain = rrtrain.Number;
            AttachTrainName = String.Copy(rrtrain.Name);
            StationPlatformReference = -1;
            FirstIn = false;
            SetBack = false;

            Valid = true;
            ReadyToAttach = true;
        }

        //================================================================================================//
        /// <summary>
        /// Contructor for restore
        /// </summary>
        /// <param name="inf"></param>
        public AttachInfo(BinaryReader inf)
        {
            AttachTrain = inf.ReadInt32();
            AttachTrainName = inf.ReadString();

            StationPlatformReference = inf.ReadInt32();
            FirstIn = inf.ReadBoolean();
            SetBack = inf.ReadBoolean();

            Valid = inf.ReadBoolean();
            ReadyToAttach = inf.ReadBoolean();
        }

        //================================================================================================//
        /// <summary>
        /// Save method
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(AttachTrain);
            outf.Write(AttachTrainName);

            outf.Write(StationPlatformReference);
            outf.Write(FirstIn);
            outf.Write(SetBack);

            outf.Write(Valid);
            outf.Write(ReadyToAttach);
        }

        //================================================================================================//
        /// <summary>
        ///  Finalize attach details - if valid, work out cross reference informatio
        /// </summary>
        /// <param name="thisTrain"></param>
        /// <param name="trainList"></param>
        /// <param name="playerTrain"></param>
        public void FinalizeAttachDetails(TTTrain thisTrain, List<TTTrain> trainList, TTTrain playerTrain)
        {
            if (Valid)
            {
                // set Xref to train to which to attach
                SetAttachXRef(thisTrain, trainList, playerTrain);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Set attach cross-reference train information
        /// </summary>
        /// <param name="dettrain"></param>
        /// <param name="trainList"></param>
        /// <param name="playerTrain"></param>
        public void SetAttachXRef(TTTrain dettrain, List<TTTrain> trainList, TTTrain playerTrain)
        {
            bool trainFound = false;
            TTTrain attachedTrain = null;

            foreach (TTTrain otherTrain in trainList)
            {
                if (String.Compare(otherTrain.Name, AttachTrainName, true) == 0)
                {
                    AttachTrain = otherTrain.OrgAINumber;
                    attachedTrain = otherTrain;
                    trainFound = true;
                    break;
                }
            }

            // if not found, try player train
            if (!trainFound)
            {
                if (playerTrain != null && String.Compare(playerTrain.Name, AttachTrainName, true) == 0)
                {
                    AttachTrain = playerTrain.OrgAINumber;
                    attachedTrain = playerTrain;
                    trainFound = true;
                }
            }

            // issue warning if train not found
            if (!trainFound)
            {
                Trace.TraceWarning("Train :  {0} : attach details : train {1} to attach to is not found",
                    dettrain.Name, AttachTrainName);
            }

            // search for station stop if set
            else if (StationPlatformReference >= 0)
            {
                int stationIndex = -1;
                for (int iStationStop = 0; iStationStop < attachedTrain.StationStops.Count && stationIndex < 0; iStationStop++)
                {
                    if (attachedTrain.StationStops[iStationStop].PlatformReference == StationPlatformReference)
                    {
                        stationIndex = iStationStop;
                    }
                }

                if (stationIndex < 0)
                {
                    Trace.TraceWarning("Train {0} : attach details : station stop for train {1} not found", dettrain.Name, attachedTrain.Name);
                    trainFound = false;
                }
            }

            // if train is found, set need attach information
            if (trainFound)
            {
                // set need attach
                if (attachedTrain.NeedAttach.ContainsKey(StationPlatformReference))
                {
                    List<int> needAttachList = attachedTrain.NeedAttach[StationPlatformReference];
                    needAttachList.Add(dettrain.OrgAINumber);
                }
                else
                {
                    List<int> needAttachList = new List<int>();
                    needAttachList.Add(dettrain.OrgAINumber);
                    attachedTrain.NeedAttach.Add(StationPlatformReference, needAttachList);
                }
            }
            else
            // if not found, set attach to invalid (nothing to attach to)
            {
                Valid = false;
                Trace.TraceWarning("Train {0} : attach command to attach to train {1} : command invalid", dettrain.Name, AttachTrainName);
            }
        }
    }

    //================================================================================================//
    //================================================================================================//
    /// <summary>
    /// Class for pick up information
    /// </summary>
    public class PickUpInfo
    {
        public int PickUpTrain;                             // train number to which to attach
        public string PickUpTrainName = String.Empty;       // train name to which to attach
        public bool PickUpStatic = false;                         // pickup unnamed static consist

        public int StationPlatformReference;                // station platform reference - set to -1 if attaching to static train in dispose command
        public bool Valid;                                  // attach info is valid

        //================================================================================================//
        /// <summary>
        /// Constructor for pick up at station
        /// </summary>
        /// <param name="stationPlatformReference"></param>
        /// <param name="thisCommand"></param>
        /// <param name="thisTrain"></param>
        public PickUpInfo(int stationPlatformReference, TTTrainCommands thisCommand, TTTrain thisTrain)
        {
            Valid = true;
            StationPlatformReference = stationPlatformReference;
            PickUpTrain = -1;
            PickUpStatic = false;

            if (thisCommand.CommandValues != null && thisCommand.CommandValues.Count > 0)
            {
                PickUpTrainName = String.Copy(thisCommand.CommandValues[0]);
                if (!PickUpTrainName.Contains(":"))
                {
                    int seppos = thisTrain.Name.IndexOf(':');
                    PickUpTrainName = String.Concat(PickUpTrainName, ":", thisTrain.Name.Substring(seppos + 1).ToLower());
                }
            }
            else if (thisCommand.CommandQualifiers != null && thisCommand.CommandQualifiers.Count > 0)
            {
                switch (thisCommand.CommandQualifiers[0].QualifierName)
                {
                    case "static" :
                        PickUpStatic = true;
                        StationPlatformReference = stationPlatformReference;
                        break;

                    default :
                        Trace.TraceInformation("Train : {0} : unknown pickup qualifier : {1}", thisTrain.Name, thisCommand.CommandQualifiers[0].QualifierName);
                        break;
                }
            }
            else
            {
                Trace.TraceInformation("Train : {0} : pick-up command must include a train name or static qualifier", thisTrain.Name);
                Valid = false;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Constructor for restore
        /// </summary>
        /// <param name="inf"></param>
        public PickUpInfo(BinaryReader inf)
        {
            PickUpTrain = inf.ReadInt32();
            PickUpTrainName = inf.ReadString();
            PickUpStatic = inf.ReadBoolean();

            StationPlatformReference = inf.ReadInt32();

            Valid = inf.ReadBoolean();
        }

        //================================================================================================//
        /// <summary>
        /// Save method
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write(PickUpTrain);
            outf.Write(PickUpTrainName);
            outf.Write(PickUpStatic);

            outf.Write(StationPlatformReference);

            outf.Write(Valid);
        }

        //================================================================================================//
        /// <summary>
        /// Finalize pickup details : set cross-reference information and check validity
        /// </summary>
        /// <param name="thisTrain"></param>
        /// <param name="trainList"></param>
        /// <param name="playerTrain"></param>
        public void FinalizePickUpDetails(TTTrain thisTrain, List<TTTrain> trainList, TTTrain playerTrain)
        {
            // set Xref to train to which to attach
            SetPickUpXRef(thisTrain, trainList, playerTrain);

            // sort out information per train or per location
            foreach (PickUpInfo thisPickUp in thisTrain.PickUpDetails)
            {
                thisTrain.PickUpStaticOnForms = false;

                if (thisPickUp.Valid)
                {
                    if (thisPickUp.PickUpStatic)
                    {
                        if (thisPickUp.StationPlatformReference >= 0)
                        {
                            if (thisTrain.PickUpStatic.Contains(thisPickUp.StationPlatformReference))
                            {
                                Trace.TraceInformation("Train {0} : multiple PickUp definition for same location : {1}", thisTrain.Name, thisPickUp.StationPlatformReference);
                            }
                            else
                            {
                                thisTrain.PickUpStatic.Add(thisPickUp.StationPlatformReference);
                            }
                        }
                        else
                        {
                            thisTrain.PickUpStaticOnForms = true;
                        }
                    }
                    else
                    {
                        if (thisTrain.PickUpTrains.Contains(thisPickUp.PickUpTrain))
                        {
                            Trace.TraceInformation("Train {0} : multiple PickUp definition for same train : {1}", thisTrain.Name, thisPickUp.PickUpTrainName);
                        }
                        else
                        {
                            thisTrain.PickUpTrains.Add(thisPickUp.PickUpTrain);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set pickup cross-reference train information
        /// </summary>
        /// <param name="dettrain"></param>
        /// <param name="trainList"></param>
        /// <param name="playerTrain"></param>
        public void SetPickUpXRef(TTTrain dettrain, List<TTTrain> trainList, TTTrain playerTrain)
        {
            bool trainFound = false;
            TTTrain pickUpTrain = null;

            if (!PickUpStatic)
            {
                foreach (TTTrain otherTrain in trainList)
                {
                    if (String.Compare(otherTrain.Name, PickUpTrainName, true) == 0)
                    {
                        PickUpTrain = otherTrain.OrgAINumber;
                        pickUpTrain = otherTrain;
                        trainFound = true;
                        break;
                    }
                }

                // if not found, try player train
                if (!trainFound)
                {
                    if (playerTrain != null && String.Compare(playerTrain.Name, PickUpTrainName, true) == 0)
                    {
                        PickUpTrain = playerTrain.OrgAINumber;
                        pickUpTrain = playerTrain;
                        trainFound = true;
                    }
                }

                // issue warning if train not found
                if (!trainFound)
                {
                    Trace.TraceWarning("Train :  {0} : pickup details : train {1} to pick up is not found",
                        dettrain.Name, PickUpTrainName);
                    Valid = false;
                    Trace.TraceWarning("Train {0} : pickup command to pick up train {1} : command invalid", dettrain.Name, PickUpTrainName);
                }
            }
        }
    }

    //================================================================================================//
    //================================================================================================//
    /// <summary>
    /// Class for transfer details
    /// </summary>
    public class TransferInfo
    {
        public enum TransferType
        {
            Give,
            Take,
            Keep,
            Leave,
        }

        public TransferType TypeOfTransfer;                     // type of transfer
        public DetachInfo.DetachUnitsInfo TransferUnitsInfo;    // type of unit definition
        public int TransferUnitCount;                           // no. of units (if defined as units)
        public List<string> TransferConsist = null;             // consists to transfer (if defined as consist)
        public int TransferTrain;                               // Number of other train
        public string TransferTrainName = String.Empty;         // Name of other train
        public int StationPlatformReference;                    // platform reference of transfer location
        public bool Valid;                                      // transfer is valid

        //================================================================================================//
        /// <summary>
        /// Constructor for new transfer details
        /// </summary>
        /// <param name="stationPlatformReference"></param>
        /// <param name="thisCommand"></param>
        /// <param name="thisTrain"></param>
        public TransferInfo(int stationPlatformReference, TTTrainCommands thisCommand, TTTrain thisTrain)
        {
            bool trainDefined = true;
            bool typeDefined = false;
            bool portionDefined = false;

            // set station platform reference
            StationPlatformReference = stationPlatformReference;

            // set transfer train name
            if (thisCommand.CommandValues != null && thisCommand.CommandValues.Count > 0)
            {
                TransferTrainName = String.Copy(thisCommand.CommandValues[0]);
                if (!TransferTrainName.Contains(":"))
                {
                    int seppos = thisTrain.Name.IndexOf(':');
                    TransferTrainName = String.Concat(TransferTrainName, ":", thisTrain.Name.Substring(seppos + 1).ToLower());
                }
            }
            else if (thisCommand.CommandQualifiers != null && thisCommand.CommandQualifiers.Count > 0)
            {
                switch (thisCommand.CommandQualifiers[0].QualifierName)
                {
                    // static is allowed, will be inserted with key -99
                    case "static" :
                        TransferTrain = -99;
                        break;

                    // other qualifiers processed below
                    default :
                        break;
                }
            }
            else
            {
                Trace.TraceInformation("Train {0} : transfer command : missing other train name in transfer command", thisTrain.Name);
                trainDefined = false;
            }

            // transfer unit type
            if (thisCommand.CommandQualifiers == null || thisCommand.CommandQualifiers.Count <= 0)
            {
                Trace.TraceInformation("Train {0} : transfer command : missing transfer type", thisTrain.Name);
            }
            else
            {
                foreach (TTTrainCommands.TTTrainComQualifiers Qualifier in thisCommand.CommandQualifiers)
                {
                    switch (Qualifier.QualifierName.Trim().ToLower())
                    {
                        // transfer type qualifiers
                        case "give":
                            TypeOfTransfer = TransferType.Give;
                            typeDefined = true;
                            break;

                        case "take":
                            TypeOfTransfer = TransferType.Take;
                            typeDefined = true;
                            break;

                        case "keep":
                            TypeOfTransfer = TransferType.Keep;
                            typeDefined = true;
                            break;

                        case "leave":
                            TypeOfTransfer = TransferType.Leave;
                            typeDefined = true;
                            break;

                        // transfer info qualifiers
                        case "onepower":
                            TransferUnitsInfo = DetachInfo.DetachUnitsInfo.leadingPower;
                            portionDefined = true;
                            break;

                        case "allpower":
                            TransferUnitsInfo = DetachInfo.DetachUnitsInfo.allLeadingPower;
                            portionDefined = true;
                            break;

                        case "nonpower":
                            TransferUnitsInfo = DetachInfo.DetachUnitsInfo.nonPower;
                            portionDefined = true;
                            break;

                        case "units":
                            int nounits = 0;
                            bool unitvalid = false;

                            try
                            {
                                nounits = Convert.ToInt32(Qualifier.QualifierValues[0]);
                                unitvalid = true;
                            }
                            catch
                            {
                                Trace.TraceInformation("Train {0} : invalid value for units qualifier in transfer command : {1} \n", thisTrain.Name, Qualifier.QualifierValues[0]);
                            }

                            if (unitvalid)
                            {
                                if (nounits > 0)
                                {
                                    TransferUnitsInfo = DetachInfo.DetachUnitsInfo.unitsAtFront;
                                    TransferUnitCount = nounits;
                                    portionDefined = true;
                                }
                                else
                                {
                                    Trace.TraceInformation("Train {0} : transfer command : invalid definition for units to transfer : {1}", thisTrain.Name, Qualifier.QualifierValues[0]);
                                }
                            }
                            break;

                        case "consist":
                            TransferUnitsInfo = DetachInfo.DetachUnitsInfo.consists;

                            if (TransferConsist == null) TransferConsist = new List<string>();
                            foreach (String consistname in Qualifier.QualifierValues)
                            {
                                TransferConsist.Add(consistname);
                            }
                            portionDefined = true;
                            break;

                        // static is allready processed, so skip
                        case "static":
                            break;

                        default:
                            Trace.TraceInformation("Train {0} : transfer command : invalid qualifier : {1}", thisTrain.Name, Qualifier.QualifierName);
                            break;
                    }
                }
            }
            if (!typeDefined)
            {
                Trace.TraceInformation("Train {0} : transfer command : no valid transfer type defined", thisTrain.Name);
            }
            else if (!portionDefined)
            {
                Trace.TraceInformation("Train {0} : transfer command : no valid transfer portion defined", thisTrain.Name);
            }
            else if (trainDefined)
            {
                Valid = true;
            }
            else
            {
                Trace.TraceInformation("Train {0} : transfer command : invalid transfer command, command ignored", thisTrain.Name);
            }
        }

        //================================================================================================//
        /// <summary>
        /// Constructor for restore
        /// </summary>
        /// <param name="inf"></param>
        public TransferInfo(BinaryReader inf)
        {
            TypeOfTransfer = (TransferType) inf.ReadInt32();
            TransferUnitsInfo = (DetachInfo.DetachUnitsInfo)inf.ReadInt32();
            TransferUnitCount = inf.ReadInt32();

            int totalTransferConsist = inf.ReadInt32();
            TransferConsist = null;

            if (totalTransferConsist > 0)
            {
                TransferConsist = new List<string>();
                for (int iConsist = 0; iConsist < totalTransferConsist; iConsist++)
                {
                    TransferConsist.Add(inf.ReadString());
                }
            }

            TransferTrain = inf.ReadInt32();
            TransferTrainName = inf.ReadString();

            StationPlatformReference = inf.ReadInt32();
            Valid = inf.ReadBoolean();
        }

        //================================================================================================//
        /// <summary>
        /// Save method
        /// </summary>
        /// <param name="outf"></param>
        public void Save(BinaryWriter outf)
        {
            outf.Write((int)TypeOfTransfer);
            outf.Write((int)TransferUnitsInfo);
            outf.Write(TransferUnitCount);

            if (TransferConsist == null || TransferConsist.Count <= 0)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(TransferConsist.Count);
                foreach (string thisConsist in TransferConsist)
                {
                    outf.Write(thisConsist);
                }
            }

            outf.Write(TransferTrain);
            outf.Write(TransferTrainName);
            outf.Write(StationPlatformReference);
            outf.Write(Valid);
        }

        //================================================================================================//
        /// <summary>
        /// Perform transfer
        /// </summary>
        /// <param name="otherTrain"></param>
        /// <param name="otherTrainFront"></param>
        /// <param name="thisTrain"></param>
        /// <param name="thisTrainFront"></param>
        public void PerformTransfer(TTTrain otherTrain, bool otherTrainFront, TTTrain thisTrain, bool thisTrainFront)
        {
            TTTrain GivingTrain = null;
            TTTrain TakingTrain = null;

            bool GiveTrainFront = false;
            bool TakeTrainFront = false;

            // stop train
            thisTrain.SpeedMpS = 0;
            foreach (var car in thisTrain.Cars)
            {
                car.SpeedMpS = 0;
            }

            if (thisTrain.TrainType != Train.TRAINTYPE.PLAYER) thisTrain.AdjustControlsThrottleOff();
            thisTrain.physicsUpdate(0);

            // sort out what to detach
            int iunits = 0;
            bool frontpos = true;
            DetachInfo.DetachUnitsInfo thisInfo;
            bool reverseUnits = false;

            switch (TypeOfTransfer)
            {
                case TransferType.Give:
                    GivingTrain = thisTrain;
                    GiveTrainFront = thisTrainFront;

                    TakingTrain = otherTrain;
                    TakeTrainFront = otherTrainFront;

                    iunits = GivingTrain.GetUnitsToDetach(TransferUnitsInfo, TransferUnitCount, TransferConsist, ref frontpos);
                    break;

                case TransferType.Take:
                    GivingTrain = otherTrain;
                    GiveTrainFront = otherTrainFront;
                    TakingTrain = thisTrain;
                    TakeTrainFront = thisTrainFront;

                    if (GiveTrainFront)
                    {
                        iunits = GivingTrain.GetUnitsToDetach(TransferUnitsInfo, TransferUnitCount, TransferConsist, ref frontpos);
                    }
                    else
                    {
                        thisInfo = TransferUnitsInfo;
                        switch (thisInfo)
                        {
                            case DetachInfo.DetachUnitsInfo.leadingPower:
                                TransferUnitsInfo = DetachInfo.DetachUnitsInfo.trailingPower;
                                break;

                            case DetachInfo.DetachUnitsInfo.allLeadingPower:
                                TransferUnitsInfo = DetachInfo.DetachUnitsInfo.allTrailingPower;
                                break;

                            case DetachInfo.DetachUnitsInfo.nonPower:
                                TransferUnitsInfo = DetachInfo.DetachUnitsInfo.allLeadingPower;
                                reverseUnits = true;
                                break;

                            case DetachInfo.DetachUnitsInfo.unitsAtFront:
                                TransferUnitsInfo = DetachInfo.DetachUnitsInfo.unitsAtEnd;
                                break;

                            // other definitions : no change
                            default:
                                break;
                        }
                        iunits = GivingTrain.GetUnitsToDetach(TransferUnitsInfo, TransferUnitCount, TransferConsist, ref frontpos);

                        if (reverseUnits)
                        {
                            iunits = GivingTrain.Cars.Count - iunits;
                            frontpos = !frontpos;
                        }
                    }
                    break;

                case TransferType.Leave:
                    GivingTrain = otherTrain;
                    GiveTrainFront = otherTrainFront;
                    TakingTrain = thisTrain;
                    TakeTrainFront = thisTrainFront;

                    if (GiveTrainFront)
                    {
                        thisInfo = TransferUnitsInfo;
                        switch (thisInfo)
                        {
                            case DetachInfo.DetachUnitsInfo.leadingPower:
                                TransferUnitsInfo = DetachInfo.DetachUnitsInfo.trailingPower;
                                reverseUnits = true;
                                break;

                            case DetachInfo.DetachUnitsInfo.allLeadingPower:
                                TransferUnitsInfo = DetachInfo.DetachUnitsInfo.allTrailingPower;
                                reverseUnits = true;
                                break;

                            case DetachInfo.DetachUnitsInfo.nonPower:
                                TransferUnitsInfo = DetachInfo.DetachUnitsInfo.allLeadingPower;
                                break;

                            case DetachInfo.DetachUnitsInfo.unitsAtFront:
                                TransferUnitsInfo = DetachInfo.DetachUnitsInfo.unitsAtEnd;
                                reverseUnits = true;
                                break;

                            case DetachInfo.DetachUnitsInfo.consists:
                                reverseUnits = true;
                                break;
                        }
                        iunits = GivingTrain.GetUnitsToDetach(TransferUnitsInfo, TransferUnitCount, TransferConsist, ref frontpos);

                        if (reverseUnits)
                        {
                            iunits = GivingTrain.Cars.Count - iunits;
                            frontpos = !frontpos;
                        }
                    }
                    else
                    {
                        thisInfo = TransferUnitsInfo;
                        switch (thisInfo)
                        {
                            case DetachInfo.DetachUnitsInfo.leadingPower:
                                reverseUnits = true;
                                break;

                            case DetachInfo.DetachUnitsInfo.allLeadingPower:
                                reverseUnits = true;
                                break;

                            case DetachInfo.DetachUnitsInfo.nonPower:
                                TransferUnitsInfo = DetachInfo.DetachUnitsInfo.allTrailingPower;
                                break;

                            case DetachInfo.DetachUnitsInfo.unitsAtFront:
                                reverseUnits = true;
                                break;

                            case DetachInfo.DetachUnitsInfo.consists:
                                reverseUnits = true;
                                break;
                        }
                        iunits = GivingTrain.GetUnitsToDetach(TransferUnitsInfo, TransferUnitCount, TransferConsist, ref frontpos);

                        if (reverseUnits)
                        {
                            iunits = GivingTrain.Cars.Count - iunits;
                            frontpos = !frontpos;
                        }
                    }
                    break;

                case TransferType.Keep:
                    GivingTrain = thisTrain;
                    GiveTrainFront = thisTrainFront;
                    TakingTrain = otherTrain;
                    TakeTrainFront = otherTrainFront;

                    thisInfo = TransferUnitsInfo;
                    switch (thisInfo)
                    {
                        case DetachInfo.DetachUnitsInfo.leadingPower:
                            TransferUnitsInfo = DetachInfo.DetachUnitsInfo.trailingPower;
                            reverseUnits = true;
                            break;

                        case DetachInfo.DetachUnitsInfo.allLeadingPower:
                            TransferUnitsInfo = DetachInfo.DetachUnitsInfo.allTrailingPower;
                            reverseUnits = true;
                            break;

                        case DetachInfo.DetachUnitsInfo.nonPower:
                            TransferUnitsInfo = DetachInfo.DetachUnitsInfo.allLeadingPower;
                            break;

                        case DetachInfo.DetachUnitsInfo.unitsAtFront:
                            TransferUnitsInfo = DetachInfo.DetachUnitsInfo.unitsAtEnd;
                            reverseUnits = true;
                            break;

                        case DetachInfo.DetachUnitsInfo.consists:
                            reverseUnits = true;
                            break;
                    }

                    iunits = GivingTrain.GetUnitsToDetach(TransferUnitsInfo, TransferUnitCount, TransferConsist, ref frontpos);

                    if (reverseUnits)
                    {
                        iunits = GivingTrain.Cars.Count - iunits;
                        frontpos = !frontpos;
                    }
                    break;

            }

            if (iunits == 0)
            {
                Trace.TraceInformation("Train {0} : transfer command : no units to transfer from train {0} to train {1}", thisTrain.Name, GivingTrain.Name, TakingTrain.Name);
            }
            else if (iunits == GivingTrain.Cars.Count)
            {
                Trace.TraceInformation("Train {0} : transfer command : transfer requires all units of train {0} to transfer to train {1}", thisTrain.Name, GivingTrain.Name, TakingTrain.Name);
                if (thisTrain.OrgAINumber == GivingTrain.OrgAINumber)
                {
                    thisTrain.TTCouple(TakingTrain, true, frontpos);
                }
                else
                {
                    GivingTrain.TTCouple(thisTrain, frontpos, true);
                }
            }
            else
            {
                // create temp train which will hold transfered units
                List<TTTrain> tempList = new List<TTTrain>();
                string tempName = String.Concat("T_", thisTrain.OrgAINumber.ToString("0000"));
                int formedTrainNo = thisTrain.CreateStaticTrain(thisTrain, ref tempList, tempName, thisTrain.PresentPosition[0].TCSectionIndex);
                TTTrain tempTrain = tempList[0];

                // add new train (stored in tempList) to AutoGenTrains
                thisTrain.AI.AutoGenTrains.Add(tempList[0]);
                thisTrain.Simulator.AutoGenDictionary.Add(formedTrainNo, tempList[0]);

                // if detached at rear set negative no of units
                if (!frontpos) iunits = -iunits;

                // create detach command
                DetachInfo thisDetach = new DetachInfo(true, false, false, -1, false, false, false, false, false, false, iunits, null, formedTrainNo, false);

                // perform detach on giving train
                thisDetach.PerformDetach(GivingTrain, false);

                // attach temp train to taking train
                tempTrain.TTCouple(TakingTrain, GiveTrainFront, TakeTrainFront);

                // remove train from need transfer list
                if (StationPlatformReference >= 0)
                {
                    if (otherTrain.NeedStationTransfer.ContainsKey(StationPlatformReference))
                    {
                        List<int> needTransferList = otherTrain.NeedStationTransfer[StationPlatformReference];
                        needTransferList.Remove(thisTrain.Number);

                        // remove list if empty
                        if (tempList.Count < 1)
                        {
                            otherTrain.NeedStationTransfer.Remove(StationPlatformReference);
                        }
                    }
                }
                else if (TransferTrain == otherTrain.OrgAINumber)
                {
                    // get last section as reference to transfer position
                    int lastSectionIndex = thisTrain.TCRoute.TCRouteSubpaths.Last().Last().TCSectionIndex;
                    if (otherTrain.NeedTrainTransfer.ContainsKey(lastSectionIndex))
                    {
                        int transferCount = otherTrain.NeedTrainTransfer[lastSectionIndex];
                        transferCount--;
                        otherTrain.NeedTrainTransfer.Remove(lastSectionIndex);

                        if (transferCount > 0)
                        {
                            otherTrain.NeedTrainTransfer.Add(lastSectionIndex, transferCount);
                        }
                    }
                }
            }

            // if transfer was part of dispose command, curtail train route so train is positioned at end of route
            if (StationPlatformReference < 0)
            {
                // get furthest index
                int firstSectionIndex = thisTrain.OccupiedTrack[0].Index;
                int lastSectionIndex = thisTrain.OccupiedTrack.Last().Index;
                int lastRouteIndex = Math.Max(thisTrain.ValidRoute[0].GetRouteIndex(firstSectionIndex, 0), thisTrain.ValidRoute[0].GetRouteIndex(lastSectionIndex, 0));
                Train.TCSubpathRoute newRoute = new Train.TCSubpathRoute(thisTrain.TCRoute.TCRouteSubpaths[thisTrain.TCRoute.activeSubpath], 0, lastRouteIndex);
                thisTrain.TCRoute.TCRouteSubpaths[thisTrain.TCRoute.activeSubpath] = new Train.TCSubpathRoute(newRoute);
                thisTrain.ValidRoute[0] = new Train.TCSubpathRoute(newRoute);

                thisTrain.MovementState = AITrain.AI_MOVEMENT_STATE.STOPPED;
            }
        }

        //================================================================================================//
        /// <summary>
        /// Set transfer cross-reference train information
        /// </summary>
        /// <param name="dettrain"></param>
        /// <param name="trainList"></param>
        /// <param name="playerTrain"></param>
        public void SetTransferXRef(TTTrain dettrain, List<TTTrain> trainList, TTTrain playerTrain, bool stationTransfer, bool trainTransfer)
        {
            bool trainFound = false;
            TTTrain transferTrain = null;

            foreach (TTTrain otherTrain in trainList)
            {
                if (String.Compare(otherTrain.Name, TransferTrainName, true) == 0)
                {
                    TransferTrain = otherTrain.OrgAINumber;
                    transferTrain = otherTrain;
                    trainFound = true;
                    break;
                }
            }

            // if not found, try player train
            if (!trainFound)
            {
                if (playerTrain != null && String.Compare(playerTrain.Name, TransferTrainName, true) == 0)
                {
                    TransferTrain = playerTrain.OrgAINumber;
                    transferTrain = playerTrain;
                    trainFound = true;
                }
            }          

            // issue warning if train not found
            if (!trainFound)
            {
                Trace.TraceWarning("Train :  {0} : transfer details : train {1} is not found",
                    dettrain.Name, TransferTrainName);
                Valid = false;
                Trace.TraceWarning("Train {0} : transfer command with train {1} : command invalid", dettrain.Name, TransferTrainName);
            }
            else
            // set need to transfer
            {
                if (stationTransfer)
                {
                    if (transferTrain.NeedStationTransfer.ContainsKey(StationPlatformReference))
                    {
                        transferTrain.NeedStationTransfer[StationPlatformReference].Add(dettrain.OrgAINumber);
                    }
                    else
                    {
                        List<int> tempList = new List<int>();
                        tempList.Add(dettrain.OrgAINumber);
                        transferTrain.NeedStationTransfer.Add(StationPlatformReference, tempList);
                    }
                }
                else
                {
                    // get last section if train - assume this to be the transfer section
                    int lastSectionIndex = dettrain.TCRoute.TCRouteSubpaths.Last().Last().TCSectionIndex;
                    if (transferTrain.NeedTrainTransfer.ContainsKey(lastSectionIndex))
                    {
                        int transferCount = transferTrain.NeedTrainTransfer[lastSectionIndex];
                        transferCount++;
                        transferTrain.NeedTrainTransfer.Remove(lastSectionIndex);
                        transferTrain.NeedTrainTransfer.Add(lastSectionIndex, transferCount);
                    }
                    else
                    {
                        transferTrain.NeedTrainTransfer.Add(lastSectionIndex, 1);
                    }
                }
            }
        }
    }
}
