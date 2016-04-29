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

using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Event = Orts.Common.Event;

namespace Orts.Simulation.Timetables
{
    public class TTTrain : AITrain
    {
        public float DefMaxDecelMpSSP = 1.0f;               // maximum decelleration
        public float DefMaxAccelMpSSP = 1.0f;               // maximum accelleration
        public float DefMaxDecelMpSSF = 0.8f;               // maximum decelleration
        public float DefMaxAccelMpSSF = 0.5f;               // maximum accelleration

        public int? ActivateTime;                           // time train is activated

        public bool Created = false;                        // train is created at start
        public string CreateAhead = String.Empty;           // train is created ahead of other train

        // Timetable Commands info
        public List<WaitInfo> WaitList = null;                            //used when in timetable mode for wait instructions
        public Dictionary<int, List<WaitInfo>> WaitAnyList = null;        //used when in timetable mode for waitany instructions
        public bool Stable_CallOn = false;                                //used when in timetable mode to show stabled train is allowed to call on

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
        public int FormedOf = -1;                                         //indicates out of which train this train is formed
        public FormCommand FormedOfType = FormCommand.None;               //indicates type of formed-of command
        public int OrgAINumber = -1;                                      //original AI number of formed player train
        public bool SetStop = false;                                      //indicates train must copy station stop from formed train
        public bool FormsAtStation = false;                               //indicates train must form into next service at last station, route must be curtailed to that stop
        public bool leadLocoAntiSlip = false;                             //anti slip indication for original leading engine

        public List<DetachInfo> DetachDetails = new List<DetachInfo>();   // detach information

        //================================================================================================//
        /// <summary>
        /// Constructor
        /// <\summary>
        public TTTrain(Simulator simulator)
            : base(simulator)
        {
            MaxAccelMpSSP = DefMaxAccelMpSSP;
            MaxAccelMpSSF = DefMaxAccelMpSSF;
            MaxDecelMpSSP = DefMaxDecelMpSSP;
            MaxDecelMpSSF = DefMaxDecelMpSSF;

            MovementState = AI_MOVEMENT_STATE.AI_STATIC;
            AI = simulator.AI;
        }

        //================================================================================================//
        /// <summary>
        /// Restore
        /// <\summary>

        public TTTrain(Simulator simulator, BinaryReader inf, AI airef)
            : base(simulator, inf, airef)
        {
            // TTTrains own additional fields
            Created = inf.ReadBoolean();
            CreateAhead = inf.ReadString();

            int activateTimeValue = inf.ReadInt32();
            if (activateTimeValue < 0)
            {
                ActivateTime = null;
            }
            else
            {
                ActivateTime = activateTimeValue;
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
            FormedOf = inf.ReadInt32();
            FormedOfType = (FormCommand)inf.ReadInt32();
            OrgAINumber = inf.ReadInt32();
            SetStop = inf.ReadBoolean();
            FormsAtStation = inf.ReadBoolean();

            int totalDetach = inf.ReadInt32();
            DetachDetails = new List<DetachInfo>();

            for (int iDetach = 0; iDetach < totalDetach; iDetach++)
            {
                DetachDetails.Add(new DetachInfo(inf));
            }
        }

        //================================================================================================//
        //
        // Find station on alternative route
        //

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

                // section found in new route - set new station details using old details
                if (altPlatformIndex > 0)
                {
                    StationStop newStop = CalculateStationStop(signalRef.PlatformDetailsList[altPlatformIndex].PlatformReference[0],
                        orgStop.ArrivalTime, orgStop.DepartTime, orgStop.arrivalDT, orgStop.departureDT, clearingDistanceM, false);

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

            return (null);
        }


        //================================================================================================//
        /// <summary>
        /// Save
        /// <\summary>

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

            // dummy for service list count
            outf.Write(-1);

            //TTTrains own additional fields
            outf.Write(Created);
            outf.Write(CreateAhead);

            if (ActivateTime.HasValue)
            {
                outf.Write(ActivateTime.Value);
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
            outf.Write(FormedOf);
            outf.Write((int)FormedOfType);
            outf.Write(OrgAINumber);
            outf.Write(SetStop);
            outf.Write(FormsAtStation);

            outf.Write(DetachDetails.Count);
            foreach (DetachInfo thisDetach in DetachDetails)
            {
                thisDetach.Save(outf);
            }
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
                if (Number == 0)
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

                        int startvalue = ActivateTime.HasValue ? ActivateTime.Value : StartTime.Value;

                        newStop.ArrivalTime = startvalue;
                        newStop.DepartTime = startvalue;
                        newStop.arrivalDT = new DateTime((long)(startvalue * Math.Pow(10, 7)));
                        newStop.departureDT = new DateTime((long)(startvalue * Math.Pow(10, 7)));
                        newStop.RouteIndex = lastSubpath.GetRouteIndex(newStop.TCSectionIndex, 0);
                        newStop.SubrouteIndex = TCRoute.TCRouteSubpaths.Count - 1;
                        if (newStop.RouteIndex >= 0) StationStops.Add(newStop); // do not set stop if platform is not on route
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

            // check deadlocks (if train has valid activate time only - otherwise it is static and won't move)

            if (ActivateTime.HasValue)
            {
                CheckDeadlock(ValidRoute[0], Number);
            }

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

                InitializeSignals(false);               // Get signal information
                TCRoute.SetReversalOffset(Length);      // set reversal information for first subpath
                SetEndOfRouteAction();                  // set action to ensure train stops at end of route
                ControlMode = TRAIN_CONTROL.AUTO_NODE;  // set control mode to NODE control

                // active train
                if (activateTrain)
                {
                    MovementState = AI_MOVEMENT_STATE.INIT;        // start in INIT mode to collect info

                    // check if train starts at station stop
                    if (StationStops.Count > 0)
                    {
                        atStation = CheckInitialStation();
                    }

                    if (!atStation)
                    {
                        if (StationStops.Count > 0)
                        {
                            SetNextStationAction();               // set station details
                        }
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
                File.AppendAllText(@"C:\temp\checktrain.txt", "Train : " + Number.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Name  : " + Name + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Frght : " + IsFreight.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Length: " + Length.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "MaxSpd: " + TrainMaxSpeedMpS.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Start : " + StartTime.Value.ToString() + "\n");
                File.AppendAllText(@"C:\temp\checktrain.txt", "Active: " + (ActivateTime.HasValue ? ActivateTime.Value.ToString() : "------") + "\n");
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

            return (validPosition);
        }

        //================================================================================================//
        /// <summary>
        /// Post Init : perform all actions required to start
        /// </summary>

        public override bool PostInit()
        {
            // start ahead of train if required

            bool validPosition = true;

            if (!String.IsNullOrEmpty(CreateAhead))
            {
                CalculateInitialTTTrainPosition(ref validPosition, null);
            }

            // if not yet started, start normally

            if (validPosition)
            {
                validPosition = InitialTrainPlacement();
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
        /// Create station stop list
        /// <\summary>

        public StationStop CalculateStationStop(int platformStartID, int arrivalTime, int departTime, DateTime arrivalDT, DateTime departureDT, float clearingDistanceM, bool terminal)
        {
            int platformIndex;
            int lastRouteIndex = 0;
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
                    Trace.TraceWarning("Train {0} : platform {1} is not on route",
                            Number.ToString(), platformStartID.ToString());
                    return (null);
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

                // if station is terminal, check if train is starting or terminating, and set stop position at 0.5 clearing distance from end

                float stopOffset = 0;
                float fullLength = thisPlatform.Length;

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
                    else if (lastRouteIndex == thisRoute.Count - 1 || !routeNodeAfterEnd)
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


                    // determine stop position

                    stopOffset = endOffset - (0.5f * deltaLength);

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
                }

                // check if stop offset beyond end signal - do not hold at signal

                int EndSignal = -1;
                bool HoldSignal = false;
                bool NoWaitSignal = false;
                bool NoClaimAllowed = false;

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
                        // train does not fit in platform - reset exit signal
                        else
                        {
                            EndSignal = -1;
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
                        // train does not fit in platform - reset exit signal
                        else
                        {
                            EndSignal = -1;
                        }
                    }
                }

                // build and add station stop

                TCRouteElement lastElement = thisRoute[lastRouteIndex];

                StationStop thisStation = new StationStop(
                        platformStartID,
                        thisPlatform,
                        activeSubroute,
                        lastRouteIndex,
                        lastElement.TCSectionIndex,
                        thisElement.Direction,
                        EndSignal,
                        HoldSignal,
                        NoWaitSignal,
                        NoClaimAllowed,
                        stopOffset,
                        arrivalTime,
                        departTime,
                        StationStop.STOPTYPE.STATION_STOP);

                thisStation.arrivalDT = arrivalDT;
                thisStation.departureDT = departureDT;

                return (thisStation);
            }
        }

        public bool CreateStationStop(int platformStartID, int arrivalTime, int departTime, DateTime arrivalDT, DateTime departureDT, float clearingDistanceM, bool terminal)
        {
            StationStop thisStation = CalculateStationStop(platformStartID, arrivalTime, departTime, arrivalDT, departureDT, clearingDistanceM, terminal);

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
        /// <\summary>

        public override bool CheckInitialStation()
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

            int beginSectionIndex = thisStation.Direction == 1 ?
                    thisPlatform.TCSectionIndex[thisPlatform.TCSectionIndex.Count - 1] :
                    thisPlatform.TCSectionIndex[0];
            int beginSectionRouteIndex = ValidRoute[0].GetRouteIndex(beginSectionIndex, 0);

            // check position

            float margin = 0.0f;
            if (AI.PreUpdate)
                margin = 2.0f * clearingDistanceM;  // allow margin in pre-update due to low update rate

            int stationIndex = ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, PresentPosition[0].RouteListIndex);

            // if not found from front of train, try from rear of train (front may be beyond platform)
            if (stationIndex < 0)
            {
                stationIndex = ValidRoute[0].GetRouteIndex(thisStation.TCSectionIndex, PresentPosition[1].RouteListIndex);
            }

            // if rear is in platform, station is valid
            if (PresentPosition[1].RouteListIndex == stationIndex)
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

        //================================================================================================//
        /// <summary>
        /// Start train out of AI train due to 'formed' action
        /// </summary>
        /// <param name="otherTrain"></param>
        /// <returns></returns>

        public bool StartFromAITrain(AITrain otherTrain, int presentTime)
        {
            // check if new train has route at present position of front of train
            int usedRefPosition = 0;
            int startPositionIndex = TCRoute.TCRouteSubpaths[0].GetRouteIndex(otherTrain.PresentPosition[usedRefPosition].TCSectionIndex, 0);

            // if not found, check for present rear position
            if (startPositionIndex < 0)
            {
                usedRefPosition = 1;
                startPositionIndex = TCRoute.TCRouteSubpaths[0].GetRouteIndex(otherTrain.PresentPosition[usedRefPosition].TCSectionIndex, 0);
            }

            // if not found - train cannot start out of other train as there is no valid route - let train start of its own
            if (startPositionIndex < 0)
            {
                FormedOf = -1;
                FormedOfType = FormCommand.None;
                return (false);
            }

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
                TrainMaxSpeedMpS = otherTrain.TrainMaxSpeedMpS;

                FrontTDBTraveller = new Traveller(otherTrain.FrontTDBTraveller);
                RearTDBTraveller = new Traveller(otherTrain.RearTDBTraveller);

                // check if train reversal is required
                if (TCRoute.TCRouteSubpaths[0][startPositionIndex].Direction != otherTrain.PresentPosition[usedRefPosition].TCDirection)
                {
                    ReverseFormation(false);

                    // if reversal is required and units must be detached at start : reverse detached units position
                    if (DetachDetails.Count > 0)
                    {
                        for (int iDetach = DetachDetails.Count - 1; iDetach >= 0; iDetach--)
                        {
                            DetachInfo thisDetach = DetachDetails[iDetach];
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
            }
            else if (FormedOfType == FormCommand.TerminationTriggered)
            {
                if (TCRoute.TCRouteSubpaths[0][startPositionIndex].Direction != otherTrain.PresentPosition[usedRefPosition].TCDirection)
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
            }

            // set state
            MovementState = AI_MOVEMENT_STATE.AI_STATIC;
            // if no activate time, set to now + 30
            if (!ActivateTime.HasValue)
                ActivateTime = presentTime + 30;
            InitialTrainPlacement();

            return (true);
        }

        //================================================================================================//
        /// <summary>
        /// Update for pre-update state
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
                        FormatStrings.FormatDistance(distanceM, true) + " set to " +
                        "0.0 > " + FormatStrings.FormatDistance(NextStopDistanceM, true) + " at " +
                        FormatStrings.FormatDistance(DistanceTravelledM, true) + "\n");
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

            if (ValidRoute != null && MovementState != AI_MOVEMENT_STATE.AI_STATIC)             // no actions required for static objects //
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
            }
        }

        //================================================================================================//
        /// <summary>
        /// Update train physics
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
            if (float.IsNaN(distanceM)) distanceM = 0;//avoid NaN, if so will not move
            if (TrainType == TRAINTYPE.AI && LeadLocomotiveIndex == (Cars.Count - 1) && LastCar.Flipped)
                distanceM = -distanceM;

            return (distanceM);
        }

        //================================================================================================//
        /// <summary>
        /// Calculate running delay if present time is later than next station arrival
        /// </summary>

        public override void UpdateMinimalDelay()
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
        }

        //================================================================================================//
        /// <summary>
        /// Update AI Static state
        /// </summary>
        /// <param name="presentTime"></param>

        public override void UpdateAIStaticState(int presentTime)
        {
#if DEBUG_CHECKTRAIN
            if (!CheckTrain)
            {
                if (Number == 0)
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
            if (ActivateTime.HasValue && ActivateTime.Value < (presentTime % (24 * 3600)) && TrainHasPower())
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

            // check if anything needs be detached
            if (TrainType != TRAINTYPE.PLAYER && DetachDetails.Count > 0)
            {
                for (int iDetach = DetachDetails.Count - 1; iDetach >= 0; iDetach--)
                {
                    DetachInfo thisDetach = DetachDetails[iDetach];

                    bool validTime = !thisDetach.DetachTime.HasValue || thisDetach.DetachTime.Value < presentTime;
                    if (thisDetach.DetachPosition == DetachInfo.DetachPositionInfo.atStart && validTime)
                    {
                        thisDetach.Detach(this, presentTime);
                        DetachDetails.RemoveAt(iDetach);
                    }
                }
            }

            // switch off power for all engines until 20 secs before start

            if (ActivateTime.HasValue && TrainHasPower())
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
        /// <\summary>

        public override void SetReversalAction()
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


                    nextActionInfo = new AIActionItem(null, AIActionItem.AI_ACTION_TYPE.REVERSAL);
                    nextActionInfo.SetParam(reqDistance, 0.0f, 0.0f, PresentPosition[0].DistanceTravelledM);
                    MovementState = AI_MOVEMENT_STATE.BRAKING;
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// change in authority state - check action
        /// <\summary>

        public override void CheckRequiredAction()
        {
            // check if train ahead
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
        /// <\summary>

        public override AITrain.AI_MOVEMENT_STATE UpdateStoppedState()
        {

            if (SpeedMpS > 0)   // if train still running force it to stop
            {
                SpeedMpS = 0;
                Update(0);   // stop the wheels from moving etc
                AITrainThrottlePercent = 0;
                AITrainBrakePercent = 100;
            }

            if (SpeedMpS < 0)   // if train still running force it to stop
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
                        Train OtherTrain = trainAhead.Key;
                        float distanceToTrain = trainAhead.Value + addOffset;

                        if (EndAuthorityType[0] == END_AUTHORITY.TRAIN_AHEAD)
                        {
                            DistanceToEndNodeAuthorityM[0] = distanceToTrain;
                        }

                        if (Math.Abs(OtherTrain.SpeedMpS) < 0.001f &&
                                    distanceToTrain > followDistanceStatTrainM)
                        {
                            // allow creeping closer
                            CreateTrainAction(creepSpeedMpS, 0.0f,
                                    distanceToTrain, null, AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD);
                            MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                            StartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);
                        }

                        else if (Math.Abs(OtherTrain.SpeedMpS) > 0 &&
                            distanceToTrain > keepDistanceMovingTrainM)
                        {
                            // train started moving
                            MovementState = AI_MOVEMENT_STATE.FOLLOWING;
                            StartMoving(AI_START_MOVEMENT.FOLLOW_TRAIN);
                        }
                    }

                    // update action info
                    if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.TRAIN_AHEAD)
                    {
                        nextActionInfo.ActivateDistanceM = DistanceTravelledM + DistanceToEndNodeAuthorityM[0];
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
                        StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
                    }
                }
                else if (DistanceToEndNodeAuthorityM[0] > clearingDistanceM)
                {
                    NextStopDistanceM = DistanceToEndNodeAuthorityM[0];
                    StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
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
                        StartMoving(AI_START_MOVEMENT.SIGNAL_RESTRICTED);
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
                        StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
                    }
                }

                else if (nextAspect == MstsSignalAspect.STOP)
                {
                    // if stop but train is well away from signal allow to close
                    if (distanceToSignal > 5 * signalApproachDistanceM)
                    {
                        MovementState = AI_MOVEMENT_STATE.ACCELERATING;
                        StartMoving(AI_START_MOVEMENT.PATH_ACTION);
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
                        MovementState = AI_MOVEMENT_STATE.RUNNING;
                        StartMoving(AI_START_MOVEMENT.SIGNAL_CLEARED);
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
                                FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + ")\n");
                    }
                }
            }

            return (MovementState);
        }

        //================================================================================================//
        /// <summary>
        /// Train is at station
        /// <\summary>

        public override void UpdateStationState(int presentTime)
        {
            StationStop thisStation = StationStops[0];
            bool removeStation = true;

            int eightHundredHours = 8 * 3600;
            int sixteenHundredHours = 16 * 3600;
            int actualdepart = thisStation.ActualDepart;

            // no arrival / departure time set : update times

            if (thisStation.ActualStopType == StationStop.STOPTYPE.STATION_STOP)
            {
                AtStation = true;

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

                    // set reference arrival for any waiting connections
                    if (thisStation.ConnectionsWaiting.Count > 0)
                    {
                        foreach (int otherTrainNumber in thisStation.ConnectionsWaiting)
                        {
                            Train otherTrain = GetOtherTrainByNumber(otherTrainNumber);
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

            thisStation.Passed = true;

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

            if (endOfPath[0])
            {
                removeStation = false; // do not remove station from list - is done by path processing
            }
            // check if station has exit signal and this signal is at danger
            else if (thisStation.ExitSignal >= 0 && NextSignalObject[0] != null && NextSignalObject[0].thisRef == thisStation.ExitSignal)
            {
                MstsSignalAspect nextAspect = GetNextSignalAspect(0);
                if (nextAspect == MstsSignalAspect.STOP && !thisStation.NoWaitSignal)
                {

#if DEBUG_TTANALYSIS
                    TTAnalysisUpdateStationState2();
#endif

                    return;  // do not depart if exit signal at danger and waiting is required
                }
            }

            DateTime baseDTd = new DateTime();
            DateTime depTime = baseDTd.AddSeconds(AI.clockTime);

            // change state if train still exists
            if (endOfPath[1])
            {
                if (MovementState == AI_MOVEMENT_STATE.STATION_STOP)
                {
                    MovementState = AI_MOVEMENT_STATE.STOPPED;   // if state is still station_stop and ready to depart - change to stop to check action
                    AtStation = false;
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
                StationStops.RemoveAt(0);

            ResetActions(true);
        }

        //================================================================================================//
        /// <summary>
        /// Train is braking
        /// <\summary>

        public override void UpdateBrakingState(float elapsedClockSeconds, int presentTime)
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
                              FormatStrings.FormatDistance(nextActionInfo.ActivateDistanceM, true) + " cleared (now at " +
                              FormatStrings.FormatDistance(PresentPosition[0].DistanceTravelledM, true) + " - " +
                              FormatStrings.FormatSpeed(SpeedMpS, true) + ")\n");
                    }
                }
                else if (nextActionInfo.ActiveItem.signal_state != MstsSignalAspect.STOP)
                {
                    nextActionInfo.NextAction = AIActionItem.AI_ACTION_TYPE.SIGNAL_ASPECT_RESTRICTED;
                    if (((nextActionInfo.ActivateDistanceM - PresentPosition[0].DistanceTravelledM) < signalApproachDistanceM) ||
                         nextActionInfo.ActiveItem.ObjectDetails.this_sig_noSpeedReduction(MstsSignalFunction.NORMAL))
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
                if (nextActionInfo.ActiveItem.signal_state >= MstsSignalAspect.APPROACH_1 ||
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
                    if (SpeedMpS < 0.01f) MovementState = AI_MOVEMENT_STATE.STOPPED;
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

            if (nextActionInfo != null && nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.STATION_STOP)
                creepDistanceM = 0.0f;
            if (nextActionInfo == null && requiredSpeedMpS == 0)
                creepDistanceM = clearingDistanceM;

            // keep speed within required speed band

            float lowestSpeedMpS = requiredSpeedMpS;

            if (requiredSpeedMpS == 0)
            {
                lowestSpeedMpS =
                    distanceToGoM < (3.0f * signalApproachDistanceM) ? (0.25f * creepSpeedMpS) : creepSpeedMpS;
            }
            else
            {
                lowestSpeedMpS = distanceToGoM < creepDistanceM ? requiredSpeedMpS :
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

                // clamp speed if still too high
                if (SpeedMpS > AllowedMaxSpeedMpS)
                {
                    AdjustControlsFixedSpeed(AllowedMaxSpeedMpS);
                }

                Alpha10 = 5;
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
            else if (distanceToGoM > 4 * preferredBrakingDistanceM && SpeedMpS < idealLowBandMpS)
            {
                AdjustControlsBrakeOff();
                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
            }
            else if (distanceToGoM > preferredBrakingDistanceM && SpeedMpS < ideal3LowBandMpS)
            {
                AdjustControlsBrakeOff();
                AdjustControlsAccelMore(0.5f * MaxAccelMpSS, elapsedClockSeconds, 20);
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

            // in preupdate : avoid problems with overshoot due to low update rate
            // check if at present speed train would pass beyond end of authority
            if (PreUpdate)
            {
                if (requiredSpeedMpS == 0 && (elapsedClockSeconds * SpeedMpS) > distanceToGoM)
                {
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
        /// Train is accelerating
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
        /// Train is following
        /// <\summary>

        public override void UpdateFollowingState(float elapsedClockSeconds, int presentTime)
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
                }
                else
                {
                    // ensure train in section is aware of this train in same section if this is required
                    UpdateTrainOnEnteringSection(thisSection, trainInfo);
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
                                                    FormatStrings.FormatDistance(OtherTrain.PresentPosition[0].TCOffset, true) +
                                                    " ; speed : " + FormatStrings.FormatSpeed(OtherTrain.SpeedMpS, true) + "\n");
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                                            "DistAhd: " + FormatStrings.FormatDistance(DistanceToEndNodeAuthorityM[0], true) + "\n");
                        }

                        // update action info with new position

                        float keepDistanceTrainM = 0f;
                        bool attachToTrain = AttachTo == OtherTrain.Number;

                        if (OtherTrain.SpeedMpS != 0.0f)
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
                            //float deltaDistance = nextActionInfo.ActivateDistanceM - DistanceTravelledM;
                            //if (deltaDistance < distanceToTrain) MovementState = AI_MOVEMENT_STATE.BRAKING; // switch to normal braking to handle action
                            //NextStopDistanceM = Math.Min(deltaDistance, (distanceToTrain - keepDistanceTrainM));
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

                        // check distance and speed
                        if (OtherTrain.SpeedMpS == 0.0f)
                        {
                            float brakingDistance = SpeedMpS * SpeedMpS * 0.5f * (0.5f * MaxDecelMpSS);
                            float reqspeed = (float)Math.Sqrt(distanceToTrain * MaxDecelMpSS);

                            float maxspeed = Math.Max(reqspeed / 2, creepSpeedMpS); // allow continue at creepspeed
                            maxspeed = Math.Min(maxspeed, AllowedMaxSpeedMpS); // but never beyond valid speed limit

                            // set brake or acceleration as required

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
                            else
                            {
                                float reqMinSpeedMpS = attachToTrain ? 0.25f * creepSpeedMpS : 0;
                                bool thisTrainFront;
                                bool otherTrainFront;

                                if (attachToTrain && CheckCouplePosition(OtherTrain, out thisTrainFront, out otherTrainFront))
                                {
                                    MovementState = AI_MOVEMENT_STATE.STOPPED;
                                    CoupleTT(OtherTrain, thisTrainFront, otherTrainFront);
                                }
                                else if ((SpeedMpS - reqMinSpeedMpS) > 0.1f)
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
                        else
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
                    }
                }

                // train not found - keep moving, state will change next update
            }
        }

        //================================================================================================//
        /// <summary>
        /// Train is running at required speed
        /// <\summary>

        public override void UpdateRunningState(float elapsedClockSeconds)
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

        public override void StartMoving(AI_START_MOVEMENT reason)
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
                AITrainThrottlePercent = PreUpdate ? 50 : 25;
                AdjustControlsBrakeOff();
            }

            SetPercentsFromTrainToTrainset();

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
                            Trace.TraceWarning("Not sufficient track to place train {0}", Number);
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
        //
        // Initiate player train
        //

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
        //
        // Initial train placement
        //

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
                Trace.TraceWarning("Train : " + Name + " : train refered to in /ahead qualifier is not in train's path, /ahead ignored\n");
                CreateAhead = String.Empty;
                tempRoute = CalculateInitialTrainPosition(ref validPlacement);
                return (validPlacement);
            }

            // front position is in this trains route - check direction
            TCRouteElement thisElement = trainRoute[routeListIndex];

            // not the same direction : cannot place train as front or rear is now not clear
            if (otherTTTrain.PresentPosition[0].TCDirection != thisElement.Direction)
            {
                Trace.TraceWarning("Train : " + Name + " : train refered to in /ahead qualifier has different direction, train can not be placed \n");
                return (false);
            }

            // train is positioned correctly

            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

            float startoffset = otherTTTrain.PresentPosition[0].TCOffset + keepDistanceStatTrainM_P;
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
                        Trace.TraceWarning("Not sufficient track to place train {0}", Number);
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
        //
        // Initial train placement
        //

        public override bool InitialTrainPlacement()
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

            // check if train has route, if not create dummy

            if (ValidRoute[0] == null)
            {
                ValidRoute[0] = signalRef.BuildTempRoute(this, PresentPosition[1].TCSectionIndex, PresentPosition[1].TCOffset,
                        PresentPosition[1].TCDirection, Length, true, true, false);
            }

            // get index of first section in route

            int rearIndex = ValidRoute[0].GetRouteIndex(PresentPosition[1].TCSectionIndex, 0);
            if (rearIndex < 0)
            {
                if (CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt", "Start position of end of train {0} not on route " + Number);
                }
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

            foreach (TrackCircuitSection thisSection in placementSections)
            {
                if (!thisSection.IsSet(routedForward, false))
                {
                    thisSection.Reserve(routedForward, ValidRoute[0]);
                    thisSection.SetOccupied(routedForward);
                }
            }

            return (true);
        }

        //================================================================================================//
        /// <summary>
        /// Update Section State - additional
        /// clear waitany actions for this section
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

        public void CoupleTT(TTTrain attachTrain, bool thisTrainFront, bool attachTrainFront)
        {
            // stop train
            SpeedMpS = 0;
            foreach (var car in Cars)
            {
                car.SpeedMpS = 0;
            }

            AdjustControlsThrottleOff();
            physicsUpdate(0);

            if (attachTrain.CheckTrain || CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Attaching : " + Number + " ; to : " + attachTrain.Number + " ; at front : " + attachTrainFront.ToString() + "\n");
            }

            // check on reverse formation
            if (thisTrainFront == attachTrainFront)
            {
                ReverseFormation(false);
            }

            var attachCar = Cars[0];

            // attach to front of waiting train
            if (attachTrainFront)
            {
                attachCar = Cars[Cars.Count - 1];
                for (int iCar = Cars.Count - 1; iCar >= 0; iCar--)
                {
                    var car = Cars[iCar];
                    car.Train = attachTrain;
                    attachTrain.Cars.Insert(0, car);
                }
            }
            // attach to rear of waiting train
            else
            {
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

            // recalculate position of formed train
            if (attachTrainFront)  // coupled to front, so rear position is still valid
            {
                attachTrain.CalculatePositionOfCars();
                DistanceTravelledM += Length;
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

            attachTrain.DistanceTravelledM = 0.0f;

            tn = attachTrain.RearTDBTraveller.TN;
            offset = attachTrain.RearTDBTraveller.TrackNodeOffset;
            direction = (int)attachTrain.RearTDBTraveller.Direction;

            attachTrain.PresentPosition[1].SetTCPosition(tn.TCCrossReference, offset, direction);

            // remove train from track and clear actions
            attachTrain.RemoveFromTrack();
            attachTrain.ClearActiveSectionItems();

            // set new track sections occupied
            Train.TCSubpathRoute tempRouteTrain = signalRef.BuildTempRoute(attachTrain, attachTrain.PresentPosition[1].TCSectionIndex,
                attachTrain.PresentPosition[1].TCOffset, attachTrain.PresentPosition[1].TCDirection, attachTrain.Length, false, true, false);

            for (int iIndex = 0; iIndex < tempRouteTrain.Count; iIndex++)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[tempRouteTrain[iIndex].TCSectionIndex];
                thisSection.SetOccupied(attachTrain.routedForward);
            }

            // set various items
            attachTrain.CheckFreight();
            attachCar.SignalEvent(Event.Couple);

            if (MovementState != AI_MOVEMENT_STATE.AI_STATIC)
            {
                InitializeSignals(true);
            }

            attachTrain.physicsUpdate(0);   // stop the wheels from moving etc

            // if train is player or intended player and train has no player engine, determine new loco lead index
            if (attachTrain.TrainType == Train.TRAINTYPE.PLAYER || attachTrain.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
            {
                if (attachTrain.LeadLocomotive == null)
                {
                    if (attachTrain.Cars[0].IsDriveable)
                    {
                        attachTrain.LeadLocomotive = attachTrain.Simulator.PlayerLocomotive = attachTrain.Cars[0];
                    }
                    else if (attachTrain.Cars[(Cars.Count - 1)].IsDriveable)
                    {
                        attachTrain.LeadLocomotive = attachTrain.Simulator.PlayerLocomotive = attachTrain.Cars[(Cars.Count - 1)];
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

                attachTrain.InitializeBrakes();

                if (AI.Simulator.PlayerLocomotive == null)
                {
                    throw new InvalidDataException("Can't find player locomotive in " + attachTrain.Name);
                }
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
            }

            // remove original train
            RemoveTrain();
        }

        //================================================================================================//
        /// <summary>
        /// Insert action item for end-of-route
        /// <\summary>

        public override void SetEndOfRouteAction()
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

            // if last section does not end at signal or next section is switch, set back overlap to keep clear of switch
            // only do so for last subroute to avoid falling short of reversal points

            TCRouteElement lastElement = ValidRoute[0][ValidRoute[0].Count - 1];
            TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastElement.TCSectionIndex];
            if (lastSection.EndSignals[lastElement.Direction] == null && TCRoute.activeSubpath == (TCRoute.TCRouteSubpaths.Count - 1))
            {
                int nextIndex = lastSection.Pins[lastElement.Direction, 0].Link;
                if (nextIndex >= 0)
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

            CreateTrainAction(TrainMaxSpeedMpS, 0.0f, lengthToGoM, null,
                    AIActionItem.AI_ACTION_TYPE.END_OF_ROUTE);
            NextStopDistanceM = lengthToGoM;
        }

        //================================================================================================//
        //
        // Check if train is in wait state
        //

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
        //
        // Check if train has AnyWait valid for this section
        //

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
        /// StationStop info may be null if command is not linked to a station
        /// </summary>

        public void ProcessTimetableStopCommands(TTTrainCommands thisCommand, int subrouteIndex, int sectionIndex, int stationIndex, TimetableInfo ttinfo)
        {

            StationStop thisStationStop = StationStops.Count > 0 ? StationStops[StationStops.Count - 1] : null;

            switch (thisCommand.CommandToken.Trim())
            {
                // WAIT command
                case "wait":
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

                        if (thisCommand.CommandQualifiers != null)
                        {
                            foreach (TTTrainCommands.TTTrainComQualifiers addQualifier in thisCommand.CommandQualifiers)
                            {
                                switch (addQualifier.QualifierName)
                                {
                                    case "maxdelay":
                                        newWaitItem.maxDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
                                        break;
                                    case "notstarted":
                                        newWaitItem.notStarted = true;
                                        break;
                                    case "owndelay":
                                        newWaitItem.ownDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
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
                                        Trace.TraceWarning("Invalid qualifier for WAIT command for train {0} at station {1} : {2}",
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
                                        newWaitItem.maxDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60;
                                        break;
                                    case "notstarted":
                                        newWaitItem.notStarted = true;
                                        break;
                                    case "owndelay":
                                        newWaitItem.ownDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
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
                                            newWaitItem.maxDelayS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
                                            break;
                                        case "hold":
                                            newWaitItem.holdTimeS = Convert.ToInt32(addQualifier.QualifierValues[0]) * 60; // defined in MINUTES!!
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
                Train otherTrain = null;
                List<WaitInfo> newWaitItems = new List<WaitInfo>();

                foreach (WaitInfo reqWait in WaitList)
                {
                    switch (reqWait.WaitType)
                    {
                        // WAIT command
                        case WaitInfo.WaitInfoType.Wait:
                            otherTrain = GetOtherTrainByName(reqWait.referencedTrainName);
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
                            otherTrain = GetOtherTrainByName(reqWait.referencedTrainName);
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
                            otherTrain = GetOtherTrainByName(reqWait.referencedTrainName);
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

        public void ProcessWaitRequest(WaitInfo reqWait, Train otherTrain, bool allowSameDirection, bool allowOppositeDirection, bool singleWait, ref List<WaitInfo> newWaitItems)
        {
            // find first common section to determine train directions
            int otherRouteIndex = -1;
            int thisSubpath = reqWait.startSubrouteIndex;
            int thisIndex = TCRoute.TCRouteSubpaths[thisSubpath].GetRouteIndex(reqWait.startSectionIndex, 0);
            int otherSubpath = 0;

            int startSectionIndex = TCRoute.TCRouteSubpaths[thisSubpath][thisIndex].TCSectionIndex;

            bool sameDirection = false;
            bool validWait = true;  // presume valid wait

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

            // set actual start

            TCRouteElement thisTrainElement = null;
            TCRouteElement otherTrainElement = null;

            int thisTrainStartSubpathIndex = reqWait.startSubrouteIndex;
            int thisTrainStartRouteIndex = TCRoute.TCRouteSubpaths[thisTrainStartSubpathIndex].GetRouteIndex(reqWait.startSectionIndex, 0);

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

                int notCommonSectionIndex = otherTrain.TCRoute.TCRouteSubpaths[otherSubpath][notCommonSectionRouteIndex].TCSectionIndex;
                thisTrainStartRouteIndex = TCRoute.TCRouteSubpaths[thisTrainStartSubpathIndex].GetRouteIndex(notCommonSectionIndex, 0);

                if (thisTrainStartRouteIndex < TCRoute.TCRouteSubpaths[thisTrainStartSubpathIndex].Count - 1) // not last entry
                {
                    thisTrainStartRouteIndex++;
                }
                else
                {
                    validWait = false; // full common route - no waiting point possible
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

                    newItem.waitTrainNumber = otherTrain.Number;
                    newItem.waitTrainSubpathIndex = sectionfound[1, 0];
                    newItem.waitTrainRouteIndex = sectionfound[1, 1];
                    newItem.maxDelayS = reqWait.maxDelayS;
                    newItem.ownDelayS = reqWait.ownDelayS;
                    newItem.notStarted = reqWait.notStarted;
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
        /// Process Connect Request : process details of connect command
        /// </summary>
        /// <param name="reqWait"></param>
        /// <param name="otherTrain"></param>
        /// <param name="allowSameDirection"></param>
        /// <param name="allowOppositeDirection"></param>
        /// <param name="singleWait"></param>
        /// <param name="newWaitItems"></param>
        public void ProcessConnectRequest(WaitInfo reqWait, Train otherTrain, ref List<WaitInfo> newWaitItems)
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
                newWait.waitTrainNumber = otherTrain.Number;
                StationStop otherTrainStationStop = otherTrain.StationStops[otherStationStopIndex];
                otherTrainStationStop.ConnectionsWaiting.Add(Number);
                newWait.waitTrainSubpathIndex = otherTrainStationStop.SubrouteIndex;
                newWait.startSectionIndex = otherTrainStationStop.TCSectionIndex;

                newWait.activeSubrouteIndex = reqWait.startSubrouteIndex;
                newWait.activeSectionIndex = reqWait.startSectionIndex;

                stopStation.ConnectionsAwaited.Add(otherTrain.Number, -1);
                stopStation.ConnectionDetails.Add(otherTrain.Number, newWait);

                newWaitItems.Add(newWait);

#if DEBUG_TRACEINFO
                Trace.TraceInformation("Connect for train {0} : Wait at {1} (=stop {2}) for {3} (=train {4}), hold {5}",
                    Name, stopStation.PlatformItem.Name, reqWait.stationIndex, reqWait.referencedTrainName, reqWait.waitTrainNumber,
                    reqWait.holdTimeS);
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
                                if (reqWait.ownDelayS.HasValue && Delay.HasValue)
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
                // if other train not in this subpath but notstarted is set, wait is valid
                else if (otherTrain.TCRoute.activeSubpath < reqWait.waitTrainSubpathIndex && reqWait.notStarted.HasValue)
                {
                    waitState = true;
                    reqWait.WaitActive = true;
                }
            }

            // check if waiting is also required if train not yet started
            else if (reqWait.notStarted.HasValue)
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
        //
        // Check route selection wait
        //

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
        //
        // Get block state for route wait check
        //

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
        /// <\summary>

        public override bool[] ProcessEndOfPath(int presentTime)
        {
            bool[] returnValue = new bool[2] { false, true };

            int directionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].Direction;
            int positionNow = ValidRoute[0][PresentPosition[0].RouteListIndex].TCSectionIndex;

            bool[] nextPart = UpdateRouteActions(0);

            if (!nextPart[0]) return (returnValue);   // not at end and not to attach to anything

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
                            if (thisStation.ExitSignal >= 0 && thisStation.HoldSignal && HoldingSignals.Contains(thisStation.ExitSignal))
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
                ProcessEndOfPathReached(ref returnValue, presentTime);
            }

            return (returnValue);
        }

        public override void ProcessEndOfPathReached(ref bool[] returnValue, int presentTime)
        {
            // check if train is to form new train
            // note : if formed train == 0, formed train is player train which requires different actions

            if (Forms >= 0)
            {
                // check if anything needs be detached
                if (DetachDetails.Count > 0)
                {
                    for (int iDetach = DetachDetails.Count - 1; iDetach >= 0; iDetach--)
                    {
                        DetachInfo thisDetach = DetachDetails[iDetach];
                        if (thisDetach.DetachPosition == DetachInfo.DetachPositionInfo.atEnd)
                        {
                            thisDetach.Detach(this, presentTime);
                            DetachDetails.RemoveAt(iDetach);
                        }
                    }
                }

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
                    Forms = -1;
                    RemoveTrain();

                    // set details for new train from existing train
                    bool validFormed = formedTrain.StartFromAITrain(this, presentTime);

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

                            formedTrain.InitializeBrakes();

                            if (AI.Simulator.PlayerLocomotive == null)
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

                    }
                    else if (!autogenStart)
                    {
                        // reinstate as to be started (note : train is not yet removed from reference)
                        AI.StartList.InsertTrain(formedTrain);
                    }
                }

                returnValue[1] = false;
            }

            // check if train is to remain as static
            else if (FormsStatic)
            {
                MovementState = AI_MOVEMENT_STATE.AI_STATIC;
                ControlMode = TRAIN_CONTROL.UNDEFINED;
                StartTime = null;  // set starttime to invalid
                ActivateTime = null;  // set activate to invalid
            }

#if DEBUG_REPORTS
                File.AppendAllText(@"C:\temp\printproc.txt", "Train " +
                     Number.ToString() + " removed\n");
#endif
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
        /// Remove train
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
                if (ActivateTime.HasValue)
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

        //================================================================================================//
        /// <summary>
        /// Add reversal info to TrackMonitorInfo
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
                TrainObjectItem nextItem = new TrainObjectItem(reversalEnabled, reversalDistanceM);
                thisInfo.ObjectInfoForward.Add(nextItem);
            }
        }

        //================================================================================================//
        //
        /// <summary>
        // Check for end of route actions - for PLAYER train only
        // Reverse train if required
        /// </summary>
        //

        public override void CheckRouteActions(float elapsedClockSeconds)
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

            //if in timetable mode, abandon train
            else if (TrainType == TRAINTYPE.PLAYER && Forms > 0)
            {
                ProcessRouteEndTimetablePlayer();
            }
        }

        //================================================================================================//
        /// <summary>
        /// setup station stop handling when run in timetable mode
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
                        StationStops[0].ActualArrival = presentTime;
                        StationStops[0].CalculateDepartTime(presentTime, this);
                        break;
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Check on station tasks, required when in timetable mode when there is no activity
        /// </summary>
        public override void CheckStationTask()
        {
            // if at station
            if (AtStation)
            {
                int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
                int eightHundredHours = 8 * 3600;
                int sixteenHundredHours = 16 * 3600;

                // if moving, set departed
                if (Math.Abs(SpeedMpS) > 1.0)
                {
                    StationStops[0].ActualDepart = presentTime;
                    StationStops[0].Passed = true;
                    AtStation = false;
                    MayDepart = false;
                    DisplayMessage = "";
                    Delay = TimeSpan.FromSeconds((presentTime - StationStops[0].DepartTime) % (24 * 3600));

                    PreviousStop = StationStops[0].CreateCopy();
                    StationStops.RemoveAt(0);
                }
                else
                {
                    // check for connection
                    int helddepart = -1;
                    int needwait = -1;

                    if (StationStops[0].ConnectionsAwaited.Count > 0)
                    {
                        needwait = ProcessConnections(StationStops[0], out helddepart);
                    }

                    int remaining;

                    if (needwait >= 0)
                    {
                        TTTrain otherTrain = GetOtherTTTrainByNumber(needwait);
                        DisplayMessage = Simulator.Catalog.GetString("Held for connecting train : ");
                        DisplayMessage = String.Concat(DisplayMessage, otherTrain.Name);
                        DisplayColor = Color.Orange;
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
                        if (remaining < 120 && StationStops[0].ExitSignal >= 0) // within two minutes of departure and hold signal?
                        {
                            HoldingSignals.Remove(StationStops[0].ExitSignal);

                            if (ControlMode == TRAIN_CONTROL.AUTO_SIGNAL)
                            {
                                SignalObject nextSignal = signalRef.SignalObjects[StationStops[0].ExitSignal];
                                nextSignal.requestClearSignal(ValidRoute[0], routedForward, 0, false, null);
                            }
                            StationStops[0].ExitSignal = -1;
                        }

                        // check departure time
                        if (remaining <= 0)
                        {
                            if (!MayDepart)
                            {
                                // check if signal ahead is cleared - if not, do not allow depart
                                if (NextSignalObject[0] != null && NextSignalObject[0].this_sig_lr(MstsSignalFunction.NORMAL) == MstsSignalAspect.STOP
                                    && NextSignalObject[0].hasPermission != SignalObject.Permission.Granted && !StationStops[0].NoWaitSignal)
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
                // if stations to be checked
                if (StationStops.Count > 0)
                {
                    // check if stopped at station
                    if (Math.Abs(SpeedMpS) == 0.0f)
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
                                            if (String.Compare(StationStops[0].PlatformItem.Name, otherStop.PlatformItem.Name) == 0)
                                            {
                                                int RefNumber = OrgAINumber > 0 ? OrgAINumber : Number;
                                                if (otherStop.ConnectionsAwaited.ContainsKey(RefNumber))
                                                {
                                                    otherStop.ConnectionsAwaited.Remove(RefNumber);
                                                    otherStop.ConnectionsAwaited.Add(RefNumber, StationStops[0].ActualArrival);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                    else
                    {
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
                        }
                    }
                }
            }
        }

        //================================================================================================//
        /// <summary>
        /// Clear station from list, clear exit signal if required
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
                    Train otherTrain = GetOtherTTTrainByNumber(otherTrainNumber);
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
                            if (otherTrain.Delay.HasValue)
                            {
                                if (otherTrain.Delay.Value.TotalSeconds <= reqWait.maxDelayS)
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
        //
        /// <summary>
        // Form new AI train out of player train
        // Detach any required portions
        /// </summary>
        //

        public void ProcessRouteEndTimetablePlayer()
        {
            int presentTime = Convert.ToInt32(Math.Floor(Simulator.ClockTime));
            int nextTrainNumber = -1;
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
                bool validFormed = formedTrain.StartFromAITrain(this, presentTime);

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
                Number = formedTrain.Number;  // switch numbers
                AI.TrainsToRemove.Add(this);

                // remove formed train from AI list
                AI.AITrains.Remove(formedTrain);

                // set proper details for new formed train
                formedTrain.OrgAINumber = nextTrainNumber;
                formedTrain.Number = 0;
                AI.AITrains.Insert(0, formedTrain);
                AI.aiListChanged = true;
                Simulator.Trains.Add(formedTrain);

                formedTrain.SetFormedOccupied();
                formedTrain.TrainType = TRAINTYPE.PLAYER;
                formedTrain.ControlMode = TRAIN_CONTROL.INACTIVE;
                formedTrain.SetupStationStopHandling();

                // copy train control details
                formedTrain.MUDirection = MUDirection;
                formedTrain.MUThrottlePercent = MUThrottlePercent;
                formedTrain.MUGearboxGearIndex = MUGearboxGearIndex;
                formedTrain.MUReverserPercent = MUReverserPercent;
                formedTrain.MUDynamicBrakePercent = MUDynamicBrakePercent;

                formedTrain.InitializeBrakes();

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
                    // TODO Apparently player locomotive causes a crash in the viewer. If it does, let's fix the viewer.
                    Simulator.PlayerLocomotive = newPlayerLocomotive;
                    Simulator.OnPlayerLocomotiveChanged();
                }

                // notify viewer of change in selected train
                Simulator.OnPlayerTrainChanged(this, formedTrain);
                Simulator.PlayerLocomotive.Train = formedTrain;

                // clear replay commands
                Simulator.Log.CommandList.Clear();

                // display messages
                if (Simulator.Confirmer != null) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Information("Player switched to train : " + formedTrain.Name);
            }
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
        /// Get other train from number
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
            return Simulator.Trains.CheckTrainNotStartedByNumber(reqNumber);
        }

        //================================================================================================//
        /// <summary>
        /// TTAnalys methods : dump methods for Timetable Analysis
        /// Use Simulator.Trains to get other train
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

        public void TTAnalysisStartMoving()
        {
            DateTime baseDTA = new DateTime();
            DateTime moveTimeA = baseDTA.AddSeconds(AI.clockTime);

            var sob = new StringBuilder();
            sob.AppendFormat("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12}",
                Number, AI.clockTime, Name, Delay, "", "", "", "", "", moveTimeA.ToString("HH:mm:ss"), "", "", "");
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

        /// <summary>
        /// Empty constructor
        /// </summary>
        public WaitInfo()
        {
        }

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

        /// <summary>
        /// Create full copy
        /// </summary>
        /// <returns></returns>
        public WaitInfo CreateCopy()
        {
            return ((WaitInfo)this.MemberwiseClone());
        }
    }


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
        }

        public enum DetachUnitsInfo
        {
            onlyPower,
            allLeadingPower,
            allTrailingPower,
            unitsAtFront,
            unitsAtEnd,
        }

        public DetachPositionInfo DetachPosition;
        public int DetachSectionInfo;
        public DetachUnitsInfo DetachUnits;
        public int NumberOfUnits;
        public int DetachFormedTrain;
        public bool ReverseDetachedTrain;
        public int? DetachTime;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="atStart"></param>
        /// <param name="atEnd"></param>
        /// <param name="atStation"></param>
        /// <param name="sectionIndex"></param>
        /// <param name="leadingPower"></param>
        /// <param name="trailingPower"></param>
        /// <param name="units"></param>
        public DetachInfo(bool atStart, bool atEnd, bool atStation, int sectionIndex, bool leadingPower, bool trailingPower, bool onlyPower,
            int units, int? time, int formedTrain, bool reverseTrain)
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
                DetachUnits = DetachUnitsInfo.allLeadingPower;
            }
            else if (trailingPower)
            {
                DetachUnits = DetachUnitsInfo.allTrailingPower;
            }
            else if (onlyPower)
            {
                DetachUnits = DetachUnitsInfo.onlyPower;
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
            ReverseDetachedTrain = reverseTrain;
        }

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
            ReverseDetachedTrain = inf.ReadBoolean();
        }

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
            outf.Write(ReverseDetachedTrain);
        }

        public int Detach(AITrain train, int presentTime)
        {
            int newTrainNumber = -1;

            // Determine no. of units to detach

            int iunits = 0;
            bool frontpos = true;

            // if position of power not defined, set position according to present position of power
            if (DetachUnits == DetachUnitsInfo.onlyPower)
            {
                DetachUnits = DetachUnitsInfo.allLeadingPower;
                if (train.Cars[train.Cars.Count - 1] is MSTSLocomotive)
                {
                    DetachUnits = DetachUnitsInfo.allTrailingPower;
                }
            }

            switch (DetachUnits)
            {
                case DetachUnitsInfo.allLeadingPower:
                    for (int iCar = 0; iCar < train.Cars.Count; iCar++)
                    {
                        var thisCar = train.Cars[iCar];
                        if (thisCar is MSTSLocomotive)
                        {
                            iunits++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    break;

                case DetachUnitsInfo.allTrailingPower:
                    frontpos = false;
                    for (int iCar = train.Cars.Count - 1; iCar >= 0; iCar--)
                    {
                        var thisCar = train.Cars[iCar];
                        if (thisCar is MSTSLocomotive)
                        {
                            iunits++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    break;

                default:
                    iunits = NumberOfUnits;
                    frontpos = DetachUnits == DetachUnitsInfo.unitsAtFront;
                    break;
            }

            if (train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt", "Detaching from : " + train.Number + " ; units : " + iunits + " ; from front position : " + frontpos.ToString() + "\n");
            }

            // check if anything to detach and anything left on train
            if (iunits > 0 && iunits < train.Cars.Count)
            {
                TTTrain newTrain = train.AI.Simulator.GetAutoGenTTTrainByNumber(DetachFormedTrain);
                if (newTrain == null)
                {
                    Trace.TraceInformation("Cannot find train {0} to detach from train {1} ( = {2} )", DetachFormedTrain, train.Number, train.Name);
                }
                else
                {
                    if (train.AI.Simulator.AutoGenDictionary != null && train.AI.Simulator.AutoGenDictionary.ContainsKey(newTrain.Number))
                        train.AI.Simulator.AutoGenDictionary.Remove(newTrain.Number);
                    train.AI.Simulator.UncoupleBehind(train, iunits, frontpos, newTrain, ReverseDetachedTrain);
                    newTrain.StartTime = presentTime + 30; // start in 30 seconds
                    newTrain.ActivateTime = presentTime + 30; // activate in 30 seconds
                    newTrainNumber = newTrain.Number;

                    if (train.CheckTrain)
                    {
                        File.AppendAllText(@"C:\temp\checktrain.txt", "Detaching from : " + train.Number + " ; to : " + newTrainNumber + "\n");
                    }
                }
            }

            // if train is player or intended player, determine new loco lead index
            if (train.TrainType == Train.TRAINTYPE.PLAYER || train.TrainType == Train.TRAINTYPE.INTENDED_PLAYER)
            {
                train.LeadLocomotive = null;
                train.Simulator.PlayerLocomotive = null;

                foreach (var thisCar in train.Cars)
                {
                    if (thisCar.IsDriveable)
                    {
                        train.LeadLocomotive = train.Simulator.PlayerLocomotive = thisCar;
                    }
                }
            }

            return (newTrainNumber);
        }
    }
}
