// COPYRIGHT 2021 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using ORTS.Common;

namespace Orts.Simulation.Signalling
{
    public class TrackCircuitSection
    {
        public enum TrackCircuitType
        {
            Normal,
            Junction,
            Crossover,
            EndOfTrack,
            Empty,
        }

        // Properties Index, Length and OffsetLength come from TrackCircuitSectionXref

        public int Index;                                         // Index of TCS                           //
        public float Length;                                      // Length of Section                      //
        public float[] OffsetLength = new float[2];               // Offset length in original tracknode    //
        public Signals signalRef;                                 // reference to Signals class             //
        public int OriginalIndex;                                 // original TDB section index             //
        public TrackCircuitType CircuitType;                      // type of section                        //

        public TrPin[,] Pins = new TrPin[2, 2];                   // next sections                          //
        public TrPin[,] ActivePins = new TrPin[2, 2];             // active next sections                   //
        public bool[] EndIsTrailingJunction = new bool[2];        // next section is trailing jn            //

        public int JunctionDefaultRoute = -1;                     // jn default route, value is out-pin      //
        public int JunctionLastRoute = -1;                        // jn last route, value is out-pin         //
        public int JunctionSetManual = -1;                        // jn set manual, value is out-pin         //
        public List<int> LinkedSignals = null;                    // switchstands linked with this switch    //
        public bool AILock;                                       // jn is locked agains AI trains           //
        public List<int> SignalsPassingRoutes;                    // list of signals reading passed junction //

        public SignalObject[] EndSignals = new SignalObject[2];   // signals at either end      //

        public double Overlap;                                    // overlap for junction nodes //
        public List<int> PlatformIndex = new List<int>();         // platforms along section    //

        public TrackCircuitItems CircuitItems;                    // all items                  //
        public TrackCircuitState CircuitState;                    // normal states              //

        // old style deadlock definitions
        public Dictionary<int, List<int>> DeadlockTraps;          // deadlock traps             //
        public List<int> DeadlockActives;                         // list of trains with active deadlock traps //
        public List<int> DeadlockAwaited;                         // train is waiting for deadlock to clear //

        // new style deadlock definitions
        public int DeadlockReference;                             // index of deadlock to related deadlockinfo object for boundary //
        public Dictionary<int, int> DeadlockBoundaries;           // list of boundaries and path index to boundary for within deadlock //

        // tunnel data
        public struct tunnelInfoData
        {
            public float TunnelStart;                             // start position of tunnel : -1 if start is in tunnel
            public float TunnelEnd;                               // end position of tunnel : -1 if end is in tunnel
            public float LengthInTCS;                             // length of tunnel within this TCS
            public float TotalLength;                             // total length of tunnel
            public float TCSStartOffset;                          // offset in tunnel of start of this TCS : -1 if tunnel start in this TCS
            public int numTunnelPaths;                            // number of paths through tunnel
        }

        public List<tunnelInfoData[]> TunnelInfo = null;          // full tunnel info data

        // trough data
        public struct troughInfoData
        {
            public float TroughStart;                             // start position of trough : -1 if start is in trough
            public float TroughEnd;                               // end position of trough : -1 if end is in trough
            public float LengthInTCS;                             // length of trough within this TCS
            public float TotalLength;                             // total length of trough
            public float TCSStartOffset;                          // offset in trough of start of this TCS : -1 if trough start in this TCS
        }

        public List<troughInfoData[]> TroughInfo = null;          // full trough info data

        public TrackCircuitSection(TrackNode thisNode, int orgINode, TrackSectionsFile tsectiondat, Signals thisSignals)
        {
            // Copy general info
            signalRef = thisSignals;

            Index = orgINode;
            OriginalIndex = orgINode;

            if (thisNode.TrEndNode)
            {
                CircuitType = TrackCircuitType.EndOfTrack;
            }
            else if (thisNode.TrJunctionNode != null)
            {
                CircuitType = TrackCircuitType.Junction;
            }
            else
            {
                CircuitType = TrackCircuitType.Normal;
            }

            // Preset pins, then copy pin info
            for (int direction = 0; direction < 2; direction++)
            {
                for (int pin = 0; pin < 2; pin++)
                {
                    Pins[direction, pin] = new TrPin() { Direction = -1, Link = -1 };
                    ActivePins[direction, pin] = new TrPin() { Direction = -1, Link = -1 };
                }
            }

            int PinNo = 0;
            for (int pin = 0; pin < Math.Min(thisNode.Inpins, Pins.GetLength(1)); pin++)
            {
                Pins[0, pin] = thisNode.TrPins[PinNo].Copy();
                PinNo++;
            }
            if (PinNo < thisNode.Inpins) PinNo = (int)thisNode.Inpins;
            for (int pin = 0; pin < Math.Min(thisNode.Outpins, Pins.GetLength(1)); pin++)
            {
                Pins[1, pin] = thisNode.TrPins[PinNo].Copy();
                PinNo++;
            }

            // preset no end signals
            // preset no trailing junction
            for (int direction = 0; direction < 2; direction++)
            {
                EndSignals[direction] = null;
                EndIsTrailingJunction[direction] = false;
            }

            // Preset length and offset
            // If section index not in tsectiondat, set length to 0.
            float totalLength = 0.0f;

            if (thisNode.TrVectorNode != null && thisNode.TrVectorNode.TrVectorSections != null)
            {
                foreach (TrVectorSection thisSection in thisNode.TrVectorNode.TrVectorSections)
                {
                    float thisLength = 0.0f;

                    if (tsectiondat.TrackSections.ContainsKey(thisSection.SectionIndex))
                    {
                        Orts.Formats.Msts.TrackSection TS = tsectiondat.TrackSections[thisSection.SectionIndex];

                        if (TS.SectionCurve != null)
                        {
                            thisLength =
                                    MathHelper.ToRadians(Math.Abs(TS.SectionCurve.Angle)) *
                                    TS.SectionCurve.Radius;
                        }
                        else
                        {
                            thisLength = TS.SectionSize.Length;

                        }
                    }

                    totalLength += thisLength;
                }
            }

            Length = totalLength;

            for (int direction = 0; direction < 2; direction++)
            {
                OffsetLength[direction] = 0;
            }

            // set signal list for junctions
            if (CircuitType == TrackCircuitType.Junction)
            {
                SignalsPassingRoutes = new List<int>();
            }
            else
            {
                SignalsPassingRoutes = null;
            }

            // for Junction nodes, obtain default route
            // set switch to default route
            // copy overlap (if set)
            if (CircuitType == TrackCircuitType.Junction)
            {
                uint trackShapeIndex = thisNode.TrJunctionNode.ShapeIndex;
                try
                {
                    TrackShape trackShape = tsectiondat.TrackShapes[trackShapeIndex];
                    JunctionDefaultRoute = (int)trackShape.MainRoute;

                    Overlap = trackShape.ClearanceDistance;
                }
                catch (Exception)
                {
                    Trace.TraceWarning("Missing TrackShape in tsection.dat : " + trackShapeIndex);
                    JunctionDefaultRoute = 0;
                    Overlap = 0;
                }

                JunctionLastRoute = JunctionDefaultRoute;
                signalRef.setSwitch(OriginalIndex, JunctionLastRoute, this);
            }

            // Create circuit items
            CircuitItems = new TrackCircuitItems(signalRef.SignalFunctions);
            CircuitState = new TrackCircuitState();

            DeadlockTraps = new Dictionary<int, List<int>>();
            DeadlockActives = new List<int>();
            DeadlockAwaited = new List<int>();

            DeadlockReference = -1;
            DeadlockBoundaries = null;
        }

        /// <summary>
        /// Constructor for empty entries
        /// </summary>
        public TrackCircuitSection(int INode, Signals thisSignals)
        {
            signalRef = thisSignals;

            Index = INode;
            OriginalIndex = -1;
            CircuitType = TrackCircuitType.Empty;

            for (int iDir = 0; iDir < 2; iDir++)
            {
                EndIsTrailingJunction[iDir] = false;
                EndSignals[iDir] = null;
                OffsetLength[iDir] = 0;
            }

            for (int iDir = 0; iDir < 2; iDir++)
            {
                for (int pin = 0; pin < 2; pin++)
                {
                    Pins[iDir, pin] = new TrPin() { Direction = -1, Link = -1 };
                    ActivePins[iDir, pin] = new TrPin() { Direction = -1, Link = -1 };
                }
            }

            CircuitItems = new TrackCircuitItems(signalRef.SignalFunctions);
            CircuitState = new TrackCircuitState();

            DeadlockTraps = new Dictionary<int, List<int>>();
            DeadlockActives = new List<int>();
            DeadlockAwaited = new List<int>();

            DeadlockReference = -1;
            DeadlockBoundaries = null;
        }

        public void Restore(Simulator simulator, BinaryReader inf)
        {
            ActivePins[0, 0].Link = inf.ReadInt32();
            ActivePins[0, 0].Direction = inf.ReadInt32();
            ActivePins[1, 0].Link = inf.ReadInt32();
            ActivePins[1, 0].Direction = inf.ReadInt32();
            ActivePins[0, 1].Link = inf.ReadInt32();
            ActivePins[0, 1].Direction = inf.ReadInt32();
            ActivePins[1, 1].Link = inf.ReadInt32();
            ActivePins[1, 1].Direction = inf.ReadInt32();

            JunctionSetManual = inf.ReadInt32();
            JunctionLastRoute = inf.ReadInt32();
            AILock = inf.ReadBoolean();

            CircuitState.Restore(simulator, inf);

            // if physical junction, throw switch

            if (CircuitType == TrackCircuitType.Junction)
            {
                signalRef.setSwitch(OriginalIndex, JunctionLastRoute, this);
            }

            int deadlockTrapsCount = inf.ReadInt32();
            for (int iDeadlock = 0; iDeadlock < deadlockTrapsCount; iDeadlock++)
            {
                int deadlockKey = inf.ReadInt32();
                int deadlockListCount = inf.ReadInt32();
                List<int> deadlockList = new List<int>();

                for (int iDeadlockInfo = 0; iDeadlockInfo < deadlockListCount; iDeadlockInfo++)
                {
                    int deadlockDetail = inf.ReadInt32();
                    deadlockList.Add(deadlockDetail);
                }
                DeadlockTraps.Add(deadlockKey, deadlockList);
            }

            int deadlockActivesCount = inf.ReadInt32();
            for (int iDeadlockActive = 0; iDeadlockActive < deadlockActivesCount; iDeadlockActive++)
            {
                int deadlockActiveDetails = inf.ReadInt32();
                DeadlockActives.Add(deadlockActiveDetails);
            }

            int deadlockWaitCount = inf.ReadInt32();
            for (int iDeadlockWait = 0; iDeadlockWait < deadlockWaitCount; iDeadlockWait++)
            {
                int deadlockWaitDetails = inf.ReadInt32();
                DeadlockAwaited.Add(deadlockWaitDetails);
            }

            DeadlockReference = inf.ReadInt32();

            DeadlockBoundaries = null;
            int deadlockBoundariesAvailable = inf.ReadInt32();
            if (deadlockBoundariesAvailable > 0)
            {
                DeadlockBoundaries = new Dictionary<int, int>();
                for (int iInfo = 0; iInfo <= deadlockBoundariesAvailable - 1; iInfo++)
                {
                    int boundaryInfo = inf.ReadInt32();
                    int pathInfo = inf.ReadInt32();
                    DeadlockBoundaries.Add(boundaryInfo, pathInfo);
                }
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(ActivePins[0, 0].Link);
            outf.Write(ActivePins[0, 0].Direction);
            outf.Write(ActivePins[1, 0].Link);
            outf.Write(ActivePins[1, 0].Direction);
            outf.Write(ActivePins[0, 1].Link);
            outf.Write(ActivePins[0, 1].Direction);
            outf.Write(ActivePins[1, 1].Link);
            outf.Write(ActivePins[1, 1].Direction);

            outf.Write(JunctionSetManual);
            outf.Write(JunctionLastRoute);
            outf.Write(AILock);

            CircuitState.Save(outf);

            outf.Write(DeadlockTraps.Count);
            foreach (KeyValuePair<int, List<int>> thisTrap in DeadlockTraps)
            {
                outf.Write(thisTrap.Key);
                outf.Write(thisTrap.Value.Count);

                foreach (int thisDeadlockRef in thisTrap.Value)
                {
                    outf.Write(thisDeadlockRef);
                }
            }

            outf.Write(DeadlockActives.Count);
            foreach (int thisDeadlockActive in DeadlockActives)
            {
                outf.Write(thisDeadlockActive);
            }

            outf.Write(DeadlockAwaited.Count);
            foreach (int thisDeadlockWait in DeadlockAwaited)
            {
                outf.Write(thisDeadlockWait);
            }

            outf.Write(DeadlockReference);

            if (DeadlockBoundaries == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(DeadlockBoundaries.Count);
                foreach (KeyValuePair<int, int> thisInfo in DeadlockBoundaries)
                {
                    outf.Write(thisInfo.Key);
                    outf.Write(thisInfo.Value);
                }
            }
        }

        /// <summary>
        /// Copy basic info only
        /// </summary>
        public TrackCircuitSection CopyBasic(int INode)
        {
            TrackCircuitSection newSection = new TrackCircuitSection(INode, this.signalRef);

            newSection.OriginalIndex = this.OriginalIndex;
            newSection.CircuitType = this.CircuitType;

            newSection.EndSignals[0] = this.EndSignals[0];
            newSection.EndSignals[1] = this.EndSignals[1];

            newSection.Length = this.Length;

            Array.Copy(this.OffsetLength, newSection.OffsetLength, this.OffsetLength.Length);

            return (newSection);
        }

        /// <summary>
        /// Check if set for train
        /// </summary>
        public bool IsSet(Train.TrainRouted thisTrain, bool claim_is_valid)   // using routed train
        {

            // if train in this section, return true; if other train in this section, return false

            if (CircuitState.ThisTrainOccupying(thisTrain))
            {
                return (true);
            }

            // check reservation

            if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train == thisTrain.Train)
            {
                return (true);
            }

            // check claim if claim is valid as state

            if (CircuitState.TrainClaimed.Count > 0 && claim_is_valid)
            {
                return (CircuitState.TrainClaimed.PeekTrain() == thisTrain.Train);
            }

            // section is not yet set for this train

            return (false);
        }

        public bool IsSet(Train thisTrain, bool claim_is_valid)    // using unrouted train
        {
            if (IsSet(thisTrain.routedForward, claim_is_valid))
            {
                return (true);
            }
            else
            {
                return (IsSet(thisTrain.routedBackward, claim_is_valid));
            }
        }

        /// <summary>
        /// Check available state for train
        /// </summary>
        public bool IsAvailable(Train.TrainRouted thisTrain)    // using routed train
        {
            // if train in this section, return true; if other train in this section, return false
            // check if train is in section in expected direction - otherwise return false
            if (CircuitState.ThisTrainOccupying(thisTrain))
            {
                return true;
            }

            if (CircuitState.HasOtherTrainsOccupying(thisTrain))
            {
                return false;
            }

            // check reservation
            if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train == thisTrain.Train)
            {
                return true;
            }

            if (!signalRef.Simulator.TimetableMode && thisTrain.Train.TrainType == Train.TRAINTYPE.AI_NOTSTARTED)
            {
                if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train != thisTrain.Train)
                {
                    ClearSectionsOfTrainBehind(CircuitState.TrainReserved, this);
                }
            }
            else if (thisTrain.Train.IsPlayerDriven && thisTrain.Train.ControlMode != Train.TRAIN_CONTROL.MANUAL && thisTrain.Train.DistanceTravelledM == 0.0 &&
                     thisTrain.Train.TCRoute != null && thisTrain.Train.ValidRoute[0] != null && thisTrain.Train.TCRoute.activeSubpath == 0) // We are at initial placement
            // Check if section is under train, and therefore can be unreserved from other trains
            {
                int thisRouteIndex = thisTrain.Train.ValidRoute[0].GetRouteIndex(Index, 0);
                if ((thisRouteIndex <= thisTrain.Train.PresentPosition[0].RouteListIndex && Index >= thisTrain.Train.PresentPosition[1].RouteListIndex) ||
                    (thisRouteIndex >= thisTrain.Train.PresentPosition[0].RouteListIndex && Index <= thisTrain.Train.PresentPosition[1].RouteListIndex))
                {
                    if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train != thisTrain.Train)
                    {
                        Train.TrainRouted trainRouted = CircuitState.TrainReserved;
                        ClearSectionsOfTrainBehind(trainRouted, this);
                        if (trainRouted.Train.TrainType == Train.TRAINTYPE.AI || trainRouted.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING)
                            ((AITrain)trainRouted.Train).ResetActions(true);
                    }
                }
            }
            else if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train != thisTrain.Train)
            {
                return false;
            }

            // check signal reservation

            if (CircuitState.SignalReserved >= 0)
            {
                return false;
            }

            // check claim

            if (CircuitState.TrainClaimed.Count > 0)
            {
                return (CircuitState.TrainClaimed.PeekTrain() == thisTrain.Train);
            }

            // check deadlock trap

            if (DeadlockTraps.ContainsKey(thisTrain.Train.Number))
            {
                if (!DeadlockAwaited.Contains(thisTrain.Train.Number))
                    DeadlockAwaited.Add(thisTrain.Train.Number); // train is waiting for deadlock to clear
                return false;
            }
            // check deadlock is in use - only if train has valid route

            if (thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex] != null)
            {

                int routeElementIndex = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex].GetRouteIndex(Index, 0);
                if (routeElementIndex >= 0)
                {
                    Train.TCRouteElement thisElement = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][routeElementIndex];

                    // check for deadlock awaited at end of passing loop - path based deadlock processing
                    if (!signalRef.UseLocationPassingPaths)
                    {
                        // if deadlock is allready awaited set available to false to keep one track open
                        if (thisElement.StartAlternativePath != null)
                        {
                            TrackCircuitSection endSection = signalRef.TrackCircuitList[thisElement.StartAlternativePath[1]];
                            if (endSection.CheckDeadlockAwaited(thisTrain.Train.Number))
                            {
                                return false;
                            }
                        }
                    }

                    // check on available paths through deadlock area - location based deadlock processing
                    else
                    {
                        if (DeadlockReference >= 0 && thisElement.FacingPoint)
                        {
#if DEBUG_DEADLOCK
                            File.AppendAllText(@"C:\Temp\deadlock.txt",
                                "\n **** Check IfAvailable for section " + Index.ToString() + " for train : " + thisTrain.Train.Number.ToString() + "\n");
#endif
                            DeadlockInfo sectionDeadlockInfo = signalRef.DeadlockInfoList[DeadlockReference];
                            List<int> pathAvail = sectionDeadlockInfo.CheckDeadlockPathAvailability(this, thisTrain.Train);
#if DEBUG_DEADLOCK
                            File.AppendAllText(@"C:\Temp\deadlock.txt", "\nReturned no. of available paths : " + pathAvail.Count.ToString() + "\n");
                            File.AppendAllText(@"C:\Temp\deadlock.txt", "****\n\n");
#endif
                            if (pathAvail.Count <= 0) return (false);
                        }
                    }
                }
            }

            // section is clear
            return true;
        }

        public bool IsAvailable(Train thisTrain)    // using unrouted train
        {
            if (IsAvailable(thisTrain.routedForward))
            {
                return (true);
            }
            else
            {
                return (IsAvailable(thisTrain.routedBackward));
            }
        }

        //================================================================================================//
        /// <summary>
        /// Reserve : set reserve state
        /// </summary>

        public void Reserve(Train.TrainRouted thisTrain, Train.TCSubpathRoute thisRoute)
        {

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt",
                String.Format("Reserve section {0} for train {1}\n",
                this.Index,
                thisTrain.Train.Number));
#endif
#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\temp\deadlock.txt",
                String.Format("Reserve section {0} for train {1}\n",
                this.Index,
                thisTrain.Train.Number));
#endif
            if (thisTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                    String.Format("Reserve section {0} for train {1}\n",
                    this.Index,
                    thisTrain.Train.Number));
            }

            Train.TCRouteElement thisElement;

            if (!CircuitState.ThisTrainOccupying(thisTrain.Train))
            {
                // check if not beyond trains route

                bool validPosition = true;
                int routeIndex = 0;

                // try from rear of train
                if (thisTrain.Train.PresentPosition[1].RouteListIndex > 0)
                {
                    routeIndex = thisTrain.Train.ValidRoute[0].GetRouteIndex(Index, thisTrain.Train.PresentPosition[1].RouteListIndex);
                    validPosition = routeIndex >= 0;
                }
                // if not possible try from front
                else if (thisTrain.Train.PresentPosition[0].RouteListIndex > 0)
                {
                    routeIndex = thisTrain.Train.ValidRoute[0].GetRouteIndex(Index, thisTrain.Train.PresentPosition[0].RouteListIndex);
                    validPosition = routeIndex >= 0;
                }

                if (validPosition)
                {
                    CircuitState.TrainReserved = thisTrain;
                }

                // remove from claim or deadlock claim

                CircuitState.TrainClaimed.RemoveTrain(thisTrain);

                // get element in routepath to find required alignment

                int thisIndex = -1;

                for (int iElement = 0; iElement < thisRoute.Count && thisIndex < 0; iElement++)
                {
                    thisElement = thisRoute[iElement];
                    if (thisElement.TCSectionIndex == Index)
                    {
                        thisIndex = iElement;
                    }
                }

                // if junction or crossover, align pins
                // also reset manual set (path will have followed setting)

                if (CircuitType == TrackCircuitType.Junction || CircuitType == TrackCircuitType.Crossover)
                {
                    if (CircuitState.Forced == false)
                    {
                        // set active pins for leading section

                        JunctionSetManual = -1;  // reset manual setting (will have been honoured in route definition if applicable)

                        int leadSectionIndex = -1;
                        if (thisIndex > 0)
                        {
                            thisElement = thisRoute[thisIndex - 1];
                            leadSectionIndex = thisElement.TCSectionIndex;

                            alignSwitchPins(leadSectionIndex);
                        }

                        // set active pins for trailing section

                        int trailSectionIndex = -1;
                        if (thisIndex <= thisRoute.Count - 2)
                        {
                            thisElement = thisRoute[thisIndex + 1];
                            trailSectionIndex = thisElement.TCSectionIndex;

                            alignSwitchPins(trailSectionIndex);
                        }

                        // reset signals which routed through this junction

                        foreach (int thisSignalIndex in SignalsPassingRoutes)
                        {
                            SignalObject thisSignal = signalRef.SignalObjects[thisSignalIndex];
                            thisSignal.ResetRoute(Index);
                        }
                        SignalsPassingRoutes.Clear();
                    }
                }

                // enable all signals along section in direction of train
                // do not enable those signals who are part of NORMAL signal

                if (thisIndex < 0) return; //Added by JTang
                thisElement = thisRoute[thisIndex];
                int direction = thisElement.Direction;

                foreach (SignalFunction function in signalRef.SignalFunctions.Values)
                {
                    TrackCircuitSignalList thisSignalList = CircuitItems.TrackCircuitSignals[direction][function];
                    foreach (TrackCircuitSignalItem thisItem in thisSignalList.TrackCircuitItem)
                    {
                        SignalObject thisSignal = thisItem.SignalRef;
                        if (!thisSignal.isSignalNormal())
                        {
                            thisSignal.enabledTrain = thisTrain;
                        }
                    }
                }

                // also set enabled for speedpost to process speed signals
                TrackCircuitSignalList thisSpeedpostList = CircuitItems.TrackCircuitSpeedPosts[direction];
                foreach (TrackCircuitSignalItem thisItem in thisSpeedpostList.TrackCircuitItem)
                {
                    SignalObject thisSpeedpost = thisItem.SignalRef;
                    if (!thisSpeedpost.isSignalNormal())
                    {
                        thisSpeedpost.enabledTrain = thisTrain;
                    }
                }

                // set deadlock trap if required - do not set deadlock if wait is required at this location

                if (thisTrain.Train.DeadlockInfo.ContainsKey(Index))
                {
                    bool waitRequired = thisTrain.Train.CheckWaitCondition(Index);
                    if (!waitRequired)
                    {
                        SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[Index]);
                    }
                }

                // if start of alternative route, set deadlock keys for other end
                // check using path based deadlock processing

                if (!signalRef.UseLocationPassingPaths)
                {
                    if (thisElement != null && thisElement.StartAlternativePath != null)
                    {
                        TrackCircuitSection endSection = signalRef.TrackCircuitList[thisElement.StartAlternativePath[1]];

                        // no deadlock yet active
                        if (thisTrain.Train.DeadlockInfo.ContainsKey(endSection.Index))
                        {
                            endSection.SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[endSection.Index]);
                        }
                        else if (endSection.DeadlockTraps.ContainsKey(thisTrain.Train.Number) && !endSection.DeadlockAwaited.Contains(thisTrain.Train.Number))
                        {
                            endSection.DeadlockAwaited.Add(thisTrain.Train.Number);
                        }
                    }
                }
                // search for path using location based deadlock processing

                else
                {
                    if (thisElement != null && thisElement.FacingPoint && DeadlockReference >= 0)
                    {
                        DeadlockInfo sectionDeadlockInfo = signalRef.DeadlockInfoList[DeadlockReference];
                        if (sectionDeadlockInfo.HasTrainAndSubpathIndex(thisTrain.Train.Number, thisTrain.Train.TCRoute.activeSubpath))
                        {
                            int trainAndSubpathIndex = sectionDeadlockInfo.GetTrainAndSubpathIndex(thisTrain.Train.Number, thisTrain.Train.TCRoute.activeSubpath);
                            int availableRoute = sectionDeadlockInfo.TrainReferences[trainAndSubpathIndex][0];
                            int endSectionIndex = sectionDeadlockInfo.AvailablePathList[availableRoute].EndSectionIndex;
                            TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];

                            // no deadlock yet active - do not set deadlock if train has wait within deadlock section
                            if (thisTrain.Train.DeadlockInfo.ContainsKey(endSection.Index))
                            {
                                if (!thisTrain.Train.HasActiveWait(Index, endSection.Index))
                                {
                                    endSection.SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[endSection.Index]);
                                }
                            }
                            else if (endSection.DeadlockTraps.ContainsKey(thisTrain.Train.Number) && !endSection.DeadlockAwaited.Contains(thisTrain.Train.Number))
                            {
                                endSection.DeadlockAwaited.Add(thisTrain.Train.Number);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// insert Claim
        /// </summary>
        public void Claim(Train.TrainRouted thisTrain)
        {
            if (!CircuitState.TrainClaimed.ContainsTrain(thisTrain))
            {

#if DEBUG_REPORTS
			File.AppendAllText(@"C:\temp\printproc.txt",
				String.Format("Claim section {0} for train {1}\n",
				this.Index,
				thisTrain.Train.Number));
#endif
#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\temp\deadlock.txt",
                    String.Format("Claim section {0} for train {1}\n",
                    this.Index,
                    thisTrain.Train.Number));
#endif
                if (thisTrain.Train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                        String.Format("Claim section {0} for train {1}\n",
                        this.Index,
                        thisTrain.Train.Number));
                }

                CircuitState.TrainClaimed.Enqueue(thisTrain);
            }

            // set deadlock trap if required

            if (thisTrain.Train.DeadlockInfo.ContainsKey(Index))
            {
                SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[Index]);
            }
        }

        /// <summary>
        /// insert pre-reserve
        /// </summary>
        public void PreReserve(Train.TrainRouted thisTrain)
        {
            if (!CircuitState.TrainPreReserved.ContainsTrain(thisTrain))
            {
                CircuitState.TrainPreReserved.Enqueue(thisTrain);
            }
        }

        /// <summary>
        /// set track occupied
        /// </summary>
        public void SetOccupied(Train.TrainRouted thisTrain)
        {
            SetOccupied(thisTrain, Convert.ToInt32(thisTrain.Train.DistanceTravelledM));
        }

        public void SetOccupied(Train.TrainRouted thisTrain, int reqDistanceTravelledM)
        {
#if DEBUG_REPORTS
			File.AppendAllText(@"C:\temp\printproc.txt",
				String.Format("Occupy section {0} for train {1}\n",
				this.Index,
				thisTrain.Train.Number));
#endif
#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\temp\deadlock.txt",
                String.Format("Occupy section {0} for train {1}\n",
                this.Index,
                thisTrain.Train.Number));
#endif
            if (thisTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                    String.Format("Occupy section {0} for train {1}\n",
                    this.Index,
                    thisTrain.Train.Number));
            }

            int routeIndex = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex].GetRouteIndex(Index, thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex == 0 ? 1 : 0].RouteListIndex);
            int direction = routeIndex < 0 ? 0 : thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][routeIndex].Direction;
            CircuitState.TrainOccupy.Add(thisTrain, direction);
            CircuitState.Forced = false;
            thisTrain.Train.OccupiedTrack.Add(this);

            // clear all reservations
            CircuitState.TrainReserved = null;
            CircuitState.SignalReserved = -1;

            CircuitState.TrainClaimed.RemoveTrain(thisTrain);
            CircuitState.TrainPreReserved.RemoveTrain(thisTrain);

            float distanceToClear = reqDistanceTravelledM + Length + thisTrain.Train.standardOverlapM;

            // add to clear list of train

            if (CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
            {
                if (Pins[direction, 1].Link >= 0)  // facing point
                {
                    if (Overlap > 0)
                    {
                        distanceToClear = reqDistanceTravelledM + Length + Convert.ToSingle(Overlap);
                    }
                    else
                    {
                        distanceToClear = reqDistanceTravelledM + Length + thisTrain.Train.junctionOverlapM;
                    }
                }
                else
                {
                    distanceToClear = reqDistanceTravelledM + Length + thisTrain.Train.standardOverlapM;
                }
            }

            else if (CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
            {
                if (Overlap > 0)
                {
                    distanceToClear = reqDistanceTravelledM + Length + Convert.ToSingle(Overlap);
                }
                else
                {
                    distanceToClear = reqDistanceTravelledM + Length + thisTrain.Train.junctionOverlapM;
                }
            }

            Train.TCPosition presentFront = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex];
            int reverseDirectionIndex = thisTrain.TrainRouteDirectionIndex == 0 ? 1 : 0;
            Train.TCPosition presentRear = thisTrain.Train.PresentPosition[reverseDirectionIndex];

            // correct offset if position direction is not equal to route direction
            float frontOffset = presentFront.TCOffset;
            if (presentFront.RouteListIndex >= 0 &&
                presentFront.TCDirection != thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][presentFront.RouteListIndex].Direction)
                frontOffset = Length - frontOffset;

            float rearOffset = presentRear.TCOffset;
            if (presentRear.RouteListIndex >= 0 &&
                presentRear.TCDirection != thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex][presentRear.RouteListIndex].Direction)
                rearOffset = Length - rearOffset;

            if (presentFront.TCSectionIndex == Index)
            {
                distanceToClear += thisTrain.Train.Length - frontOffset;
            }
            else if (presentRear.TCSectionIndex == Index)
            {
                distanceToClear -= rearOffset;
            }
            else
            {
                distanceToClear += thisTrain.Train.Length;
            }

            // make sure items are cleared in correct sequence
            float? lastDistance = thisTrain.Train.requiredActions.GetLastClearingDistance();
            if (lastDistance.HasValue && lastDistance > distanceToClear)
            {
                distanceToClear = lastDistance.Value;
            }

            thisTrain.Train.requiredActions.InsertAction(new Train.ClearSectionItem(distanceToClear, Index));

            if (thisTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                    "Set clear action : section : " + Index + " : distance to clear : " + distanceToClear + "\n");
            }

            // set deadlock trap if required

            if (thisTrain.Train.DeadlockInfo.ContainsKey(Index))
            {
                SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[Index]);
            }

            // check for deadlock trap if taking alternative path

            if (thisTrain.Train.TCRoute != null && thisTrain.Train.TCRoute.activeAltpath >= 0)
            {
                Train.TCSubpathRoute altRoute = thisTrain.Train.TCRoute.TCAlternativePaths[thisTrain.Train.TCRoute.activeAltpath];
                Train.TCRouteElement startElement = altRoute[0];
                if (Index == startElement.TCSectionIndex)
                {
                    TrackCircuitSection endSection = signalRef.TrackCircuitList[altRoute[altRoute.Count - 1].TCSectionIndex];

                    // set deadlock trap for next section

                    if (thisTrain.Train.DeadlockInfo.ContainsKey(endSection.Index))
                    {
                        endSection.SetDeadlockTrap(thisTrain.Train, thisTrain.Train.DeadlockInfo[endSection.Index]);
                    }
                }
            }
        }

        /// <summary>
        /// clear track occupied for routed train
        /// </summary>
        public void ClearOccupied(Train.TrainRouted thisTrain, bool resetEndSignal)
        {

#if DEBUG_REPORTS
			File.AppendAllText(@"C:\temp\printproc.txt",
				String.Format("Clear section {0} for train {1}\n",
				this.Index,
				thisTrain.Train.Number));
#endif
            if (thisTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                    String.Format("Clear section {0} for train {1}\n",
                    this.Index,
                    thisTrain.Train.Number));
            }

            if (CircuitState.TrainOccupy.ContainsTrain(thisTrain))
            {
                CircuitState.TrainOccupy.RemoveTrain(thisTrain);
                thisTrain.Train.OccupiedTrack.Remove(this);
            }

            RemoveTrain(thisTrain, false);   // clear occupy first to prevent loop, next clear all hanging references

            ClearDeadlockTrap(thisTrain.Train.Number); // clear deadlock traps

            // if signal at either end is still enabled for this train, reset the signal

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                if (EndSignals[iDirection] != null)
                {
                    SignalObject endSignal = EndSignals[iDirection];
                    if (endSignal.enabledTrain == thisTrain && resetEndSignal)
                    {
                        endSignal.resetSignalEnabled();
                    }
                }

                // disable all signals along section if enabled for this train

                foreach (SignalFunction function in signalRef.SignalFunctions.Values)
                {
                    TrackCircuitSignalList thisSignalList = CircuitItems.TrackCircuitSignals[iDirection][function];
                    foreach (TrackCircuitSignalItem thisItem in thisSignalList.TrackCircuitItem)
                    {
                        SignalObject thisSignal = thisItem.SignalRef;
                        if (thisSignal.enabledTrain == thisTrain)
                        {
                            thisSignal.resetSignalEnabled();
                        }
                    }
                }

                // also reset enabled for speedpost to process speed signals
                TrackCircuitSignalList thisSpeedpostList = CircuitItems.TrackCircuitSpeedPosts[iDirection];
                foreach (TrackCircuitSignalItem thisItem in thisSpeedpostList.TrackCircuitItem)
                {
                    SignalObject thisSpeedpost = thisItem.SignalRef;
                    if (!thisSpeedpost.isSignalNormal())
                    {
                        thisSpeedpost.resetSignalEnabled();
                    }
                }
            }

            // if section is Junction or Crossover, reset active pins but only if section is not occupied by other train

            if ((CircuitType == TrackCircuitType.Junction || CircuitType == TrackCircuitType.Crossover) && CircuitState.TrainOccupy.Count == 0)
            {
                deAlignSwitchPins();

                // reset signals which routed through this junction

                foreach (int thisSignalIndex in SignalsPassingRoutes)
                {
                    SignalObject thisSignal = signalRef.SignalObjects[thisSignalIndex];
                    thisSignal.ResetRoute(Index);
                }
                SignalsPassingRoutes.Clear();
            }

            // reset manual junction setting if train is in manual mode

            if (thisTrain.Train.ControlMode == Train.TRAIN_CONTROL.MANUAL && CircuitType == TrackCircuitType.Junction && JunctionSetManual >= 0)
            {
                JunctionSetManual = -1;
            }

            // if no longer occupied and pre-reserved not empty, promote first entry of prereserved

            if (CircuitState.TrainOccupy.Count <= 0 && CircuitState.TrainPreReserved.Count > 0)
            {
                Train.TrainRouted nextTrain = CircuitState.TrainPreReserved.Dequeue();
                Train.TCSubpathRoute RoutePart = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex];

                Reserve(nextTrain, RoutePart);
            }

        }

        /// <summary>
        /// clear track occupied for unrouted train
        /// </summary>
        public void ClearOccupied(Train thisTrain, bool resetEndSignal)
        {
            ClearOccupied(thisTrain.routedForward, resetEndSignal); // forward
            ClearOccupied(thisTrain.routedBackward, resetEndSignal);// backward
        }

        /// <summary>
        /// only reset occupied state - use in case of reversal or mode change when train has not actually moved
        /// routed train
        /// </summary>
        public void ResetOccupied(Train.TrainRouted thisTrain)
        {

            if (CircuitState.TrainOccupy.ContainsTrain(thisTrain))
            {
                CircuitState.TrainOccupy.RemoveTrain(thisTrain);
                thisTrain.Train.OccupiedTrack.Remove(this);

                if (thisTrain.Train.CheckTrain)
                {
                    File.AppendAllText(@"C:\temp\checktrain.txt",
                        "Reset Occupy for section : " + Index + "\n");
                }
            }

        }

        /// <summary>
        /// unrouted train
        /// </summary>
        public void ResetOccupied(Train thisTrain)
        {
            ResetOccupied(thisTrain.routedForward); // forward
            ResetOccupied(thisTrain.routedBackward);// backward
        }

        /// <summary>
        /// Remove train from section for routed train
        /// </summary>
        public void RemoveTrain(Train.TrainRouted thisTrain, bool resetEndSignal)
        {
#if DEBUG_REPORTS
			File.AppendAllText(@"C:\temp\printproc.txt",
				String.Format("Remove train from section {0} for train {1}\n",
				this.Index,
				thisTrain.Train.Number));
#endif
            if (thisTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                    String.Format("Remove train from section {0} for train {1}\n",
                    this.Index,
                    thisTrain.Train.Number));
            }

            if (CircuitState.ThisTrainOccupying(thisTrain))
            {
                ClearOccupied(thisTrain, resetEndSignal);
            }

            if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train == thisTrain.Train)
            {
                CircuitState.TrainReserved = null;
                ClearOccupied(thisTrain, resetEndSignal);    // call clear occupy to reset signals and switches //
            }

            CircuitState.TrainClaimed.RemoveTrain(thisTrain);
            CircuitState.TrainPreReserved.RemoveTrain(thisTrain);
        }


        /// <summary>
        /// Remove train from section for unrouted train
        /// </summary>
        public void RemoveTrain(Train thisTrain, bool resetEndSignal)
        {
            RemoveTrain(thisTrain.routedForward, resetEndSignal);
            RemoveTrain(thisTrain.routedBackward, resetEndSignal);
        }

        /// <summary>
        /// Remove train reservations from section
        /// </summary>
        public void UnreserveTrain(Train.TrainRouted thisTrain, bool resetEndSignal)
        {
            if (CircuitState.TrainReserved != null && CircuitState.TrainReserved.Train == thisTrain.Train)
            {
                CircuitState.TrainReserved = null;
                ClearOccupied(thisTrain, resetEndSignal);    // call clear occupy to reset signals and switches //
            }

            CircuitState.TrainClaimed.RemoveTrain(thisTrain);
            CircuitState.TrainPreReserved.RemoveTrain(thisTrain);
        }

        /// <summary>
        /// Remove train clain from section
        /// </summary>
        public void UnclaimTrain(Train.TrainRouted thisTrain)
        {
            CircuitState.TrainClaimed.RemoveTrain(thisTrain);
        }

        /// <summary>
        /// Remove all reservations from section if signal not enabled for train
        /// </summary>
        public void Unreserve()
        {
            CircuitState.SignalReserved = -1;
        }

        /// <summary>
        /// Remove reservation of train
        /// </summary>
        public void UnreserveTrain()
        {
            CircuitState.TrainReserved = null;
        }

        /// <summary>
        /// Remove claims from sections for reversed trains
        /// </summary>
        public void ClearReversalClaims(Train.TrainRouted thisTrain)
        {
            // check if any trains have claimed this section
            List<Train.TrainRouted> claimedTrains = new List<Train.TrainRouted>();

            // get list of trains with claims on this section
            foreach (Train.TrainRouted claimingTrain in CircuitState.TrainClaimed)
            {
                claimedTrains.Add(claimingTrain);
            }
            foreach (Train.TrainRouted claimingTrain in claimedTrains)
            {
                UnclaimTrain(claimingTrain);
                claimingTrain.Train.ClaimState = false; // reset train claim state
            }

            // get train route
            Train.TCSubpathRoute usedRoute = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex];
            int routeIndex = usedRoute.GetRouteIndex(Index, 0);

            // run down route and clear all claims for found trains, until end 
            for (int iRouteIndex = routeIndex + 1; iRouteIndex <= usedRoute.Count - 1 && (claimedTrains.Count > 0); iRouteIndex++)
            {
                TrackCircuitSection nextSection = signalRef.TrackCircuitList[usedRoute[iRouteIndex].TCSectionIndex];

                for (int iTrain = claimedTrains.Count - 1; iTrain >= 0; iTrain--)
                {
                    Train.TrainRouted claimingTrain = claimedTrains[iTrain];

                    if (nextSection.CircuitState.TrainClaimed.ContainsTrain(claimingTrain))
                    {
                        nextSection.UnclaimTrain(claimingTrain);
                    }
                    else
                    {
                        claimedTrains.Remove(claimingTrain);
                    }
                }

                nextSection.Claim(thisTrain);
            }
        }

        /// <summary>
        /// align pins switch or crossover
        /// </summary>
        public void alignSwitchPins(int linkedSectionIndex)
        {
            int alignDirection = -1;  // pin direction for leading section
            int alignLink = -1;       // link index for leading section

            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                for (int iLink = 0; iLink <= 1; iLink++)
                {
                    if (Pins[iDirection, iLink].Link == linkedSectionIndex)
                    {
                        alignDirection = iDirection;
                        alignLink = iLink;
                    }
                }
            }

            if (alignDirection >= 0)
            {
                ActivePins[alignDirection, 0].Link = -1;
                ActivePins[alignDirection, 1].Link = -1;

                ActivePins[alignDirection, alignLink].Link =
                        Pins[alignDirection, alignLink].Link;
                ActivePins[alignDirection, alignLink].Direction =
                        Pins[alignDirection, alignLink].Direction;

                TrackCircuitSection linkedSection = signalRef.TrackCircuitList[linkedSectionIndex];
                for (int iDirection = 0; iDirection <= 1; iDirection++)
                {
                    for (int iLink = 0; iLink <= 1; iLink++)
                    {
                        if (linkedSection.Pins[iDirection, iLink].Link == Index)
                        {
                            linkedSection.ActivePins[iDirection, iLink].Link = Index;
                            linkedSection.ActivePins[iDirection, iLink].Direction =
                                    linkedSection.Pins[iDirection, iLink].Direction;
                        }
                    }
                }
            }

            // if junction, align physical switch

            if (CircuitType == TrackCircuitType.Junction)
            {
                int switchPos = -1;
                if (ActivePins[1, 0].Link != -1)
                    switchPos = 0;
                if (ActivePins[1, 1].Link != -1)
                    switchPos = 1;

                if (switchPos >= 0)
                {
                    signalRef.setSwitch(OriginalIndex, switchPos, this);
                }
            }
        }

        /// <summary>
        /// de-align active switch pins
        /// </summary>
        public void deAlignSwitchPins()
        {
            for (int iDirection = 0; iDirection <= 1; iDirection++)
            {
                if (Pins[iDirection, 1].Link > 0)     // active switchable end
                {
                    for (int iLink = 0; iLink <= 1; iLink++)
                    {
                        int activeLink = Pins[iDirection, iLink].Link;
                        int activeDirection = Pins[iDirection, iLink].Direction == 0 ? 1 : 0;
                        ActivePins[iDirection, iLink].Link = -1;

                        TrackCircuitSection linkSection = signalRef.TrackCircuitList[activeLink];
                        linkSection.ActivePins[activeDirection, 0].Link = -1;
                    }
                }
            }
        }

        /// <summary>
        /// Get section state for request clear node
        /// Method is put through to train class because of differences between activity and timetable mode
        /// </summary>
        public bool GetSectionStateClearNode(Train.TrainRouted thisTrain, int elementDirection, Train.TCSubpathRoute routePart)
        {
            bool returnValue = thisTrain.Train.TrainGetSectionStateClearNode(elementDirection, routePart, this);
            return (returnValue);
        }

        /// <summary>
        /// Get state of single section
        /// Check for train
        /// </summary>
        public SignalObject.InternalBlockstate getSectionState(Train.TrainRouted thisTrain, int direction,
                        SignalObject.InternalBlockstate passedBlockstate, Train.TCSubpathRoute thisRoute, int signalIndex)
        {
            SignalObject.InternalBlockstate thisBlockstate;
            SignalObject.InternalBlockstate localBlockstate = SignalObject.InternalBlockstate.Reservable;  // default value
            bool stateSet = false;

            TrackCircuitState thisState = CircuitState;

            // track occupied - check speed and direction - only for normal sections

            if (thisTrain != null && thisState.TrainOccupy.ContainsTrain(thisTrain))
            {
                localBlockstate = SignalObject.InternalBlockstate.Reserved;  // occupied by own train counts as reserved
                stateSet = true;
            }
            else if (thisState.HasTrainsOccupying(direction, true))
            {
                {
                    localBlockstate = SignalObject.InternalBlockstate.OccupiedSameDirection;
                    stateSet = true;
                }
            }
            else
            {
                int reqDirection = direction == 0 ? 1 : 0;
                if (thisState.HasTrainsOccupying(reqDirection, false))
                {
                    localBlockstate = SignalObject.InternalBlockstate.OccupiedOppositeDirection;
                    stateSet = true;
                }
            }

            // for junctions or cross-overs, check route selection

            if (CircuitType == TrackCircuitType.Junction || CircuitType == TrackCircuitType.Crossover)
            {
                if (thisState.HasTrainsOccupying())    // there is a train on the switch
                {
                    if (thisRoute == null)  // no route from signal - always report switch blocked
                    {
                        localBlockstate = SignalObject.InternalBlockstate.Blocked;
                        stateSet = true;
                    }
                    else
                    {
                        int reqPinIndex = -1;
                        for (int iPinIndex = 0; iPinIndex <= 1 && reqPinIndex < 0; iPinIndex++)
                        {
                            if (Pins[iPinIndex, 1].Link > 0)
                                reqPinIndex = iPinIndex;  // switchable end
                        }

                        int switchEnd = -1;
                        for (int iSwitch = 0; iSwitch <= 1; iSwitch++)
                        {
                            int nextSectionIndex = Pins[reqPinIndex, iSwitch].Link;
                            int routeListIndex = thisRoute == null ? -1 : thisRoute.GetRouteIndex(nextSectionIndex, 0);
                            if (routeListIndex >= 0)
                                switchEnd = iSwitch;  // required exit
                        }
                        // allow if switch not active (both links dealligned)
                        int otherEnd = switchEnd == 0 ? 1 : 0;
                        if (switchEnd < 0 || (ActivePins[reqPinIndex, switchEnd].Link < 0 && ActivePins[reqPinIndex, otherEnd].Link >= 0)) // no free exit available or switch misaligned
                        {
                            localBlockstate = SignalObject.InternalBlockstate.Blocked;
                            stateSet = true;
                        }
                    }
                }
            }

            // track reserved - check direction

            if (thisState.TrainReserved != null && thisTrain != null && !stateSet)
            {
                Train.TrainRouted reservedTrain = thisState.TrainReserved;
                if (reservedTrain.Train == thisTrain.Train)
                {
                    localBlockstate = SignalObject.InternalBlockstate.Reserved;
                    stateSet = true;
                }
                else
                {
                    if (MPManager.IsMultiPlayer())
                    {
                        var reservedTrainStillThere = false;
                        foreach (var s in this.EndSignals)
                        {
                            if (s != null && s.enabledTrain != null && s.enabledTrain.Train == reservedTrain.Train) reservedTrainStillThere = true;
                        }

                        if (reservedTrainStillThere == true && reservedTrain.Train.ValidRoute[0] != null && reservedTrain.Train.PresentPosition[0] != null &&
                            reservedTrain.Train.GetDistanceToTrain(this.Index, 0.0f) > 0)
                            localBlockstate = SignalObject.InternalBlockstate.ReservedOther;
                        else
                        {
                            //if (reservedTrain.Train.RearTDBTraveller.DistanceTo(this.
                            thisState.TrainReserved = thisTrain;
                            localBlockstate = SignalObject.InternalBlockstate.Reserved;
                        }
                    }
                    else
                    {
                        localBlockstate = SignalObject.InternalBlockstate.ReservedOther;
                    }
                }
            }

            // signal reserved - reserved for other

            if (thisState.SignalReserved >= 0 && thisState.SignalReserved != signalIndex)
            {
                localBlockstate = SignalObject.InternalBlockstate.ReservedOther;
                stateSet = true;
            }

            // track claimed

            if (!stateSet && thisTrain != null && thisState.TrainClaimed.Count > 0 && thisState.TrainClaimed.PeekTrain() != thisTrain.Train)
            {
                localBlockstate = SignalObject.InternalBlockstate.Open;
                stateSet = true;
            }

            // wait condition

            if (thisTrain != null)
            {
                bool waitRequired = thisTrain.Train.CheckWaitCondition(Index);

                if ((!stateSet || localBlockstate < SignalObject.InternalBlockstate.ForcedWait) && waitRequired)
                {
                    localBlockstate = SignalObject.InternalBlockstate.ForcedWait;
                    thisTrain.Train.ClaimState = false; // claim not allowed for forced wait
                }
            }

            // deadlock trap - may not set deadlock if wait is active 

            if (thisTrain != null && localBlockstate != SignalObject.InternalBlockstate.ForcedWait && DeadlockTraps.ContainsKey(thisTrain.Train.Number))
            {
                bool acceptDeadlock = thisTrain.Train.VerifyDeadlock(DeadlockTraps[thisTrain.Train.Number]);

                if (acceptDeadlock)
                {
                    localBlockstate = SignalObject.InternalBlockstate.Blocked;
                    stateSet = true;
                    if (!DeadlockAwaited.Contains(thisTrain.Train.Number))
                        DeadlockAwaited.Add(thisTrain.Train.Number);
                }
            }

            thisBlockstate = localBlockstate > passedBlockstate ? localBlockstate : passedBlockstate;

            return (thisBlockstate);
        }


        /// <summary>
        /// Test only if section reserved to train
        /// </summary>
        public bool CheckReserved(Train.TrainRouted thisTrain)
        {
            var reserved = false;
            if (CircuitState.TrainReserved != null && thisTrain != null)
            {
                Train.TrainRouted reservedTrain = CircuitState.TrainReserved;
                if (reservedTrain.Train == thisTrain.Train)
                {
                    reserved = true;
                }
            }
            return reserved;
        }

        /// <summary>
        /// Test if train ahead and calculate distance to that train (front or rear depending on direction)
        /// </summary>
        public Dictionary<Train, float> TestTrainAhead(Train thisTrain, float offset, int direction)
        {
            Train trainFound = null;
            float distanceTrainAheadM = Length + 1.0f; // ensure train is always within section

            List<Train.TrainRouted> trainsInSection = CircuitState.TrainsOccupying();

            // remove own train
            if (thisTrain != null)
            {
                for (int iindex = trainsInSection.Count - 1; iindex >= 0; iindex--)
                {
                    if (trainsInSection[iindex].Train == thisTrain)
                        trainsInSection.RemoveAt(iindex);
                }
            }

            // search for trains in section
            foreach (Train.TrainRouted nextTrain in trainsInSection)
            {
                int nextTrainRouteIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(Index, 0);
                if (nextTrainRouteIndex >= 0)
                {
                    Train.TCPosition nextFront = nextTrain.Train.PresentPosition[nextTrain.TrainRouteDirectionIndex];
                    int reverseDirection = nextTrain.TrainRouteDirectionIndex == 0 ? 1 : 0;
                    Train.TCPosition nextRear = nextTrain.Train.PresentPosition[reverseDirection];

                    Train.TCRouteElement thisElement = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex][nextTrainRouteIndex];
                    if (thisElement.Direction == direction) // same direction, so if the train is in front we're looking at the rear of the train
                    {
                        if (nextRear.TCSectionIndex == Index) // rear of train is in same section
                        {
                            float thisTrainDistanceM = nextRear.TCOffset;

                            if (thisTrainDistanceM < distanceTrainAheadM && nextRear.TCOffset >= offset) // train is nearest train and in front
                            {
                                distanceTrainAheadM = thisTrainDistanceM;
                                trainFound = nextTrain.Train;
                            }
                            else if (nextRear.TCOffset < offset && nextRear.TCOffset + nextTrain.Train.Length > offset) // our end is in the middle of the train
                            {
                                distanceTrainAheadM = offset; // set distance to 0 (offset is deducted later)
                                trainFound = nextTrain.Train;
                            }
                        }
                        else
                        {
                            // try to use next train indices
                            int nextRouteFrontIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(nextFront.TCSectionIndex, 0);
                            int nextRouteRearIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(nextRear.TCSectionIndex, 0);
                            int usedTrainRouteIndex = nextTrainRouteIndex;

                            // if not on route, try this trains route
                            if (thisTrain != null && (nextRouteFrontIndex < 0 || nextRouteRearIndex < 0))
                            {
                                nextRouteFrontIndex = thisTrain.ValidRoute[0].GetRouteIndex(nextFront.TCSectionIndex, 0);
                                nextRouteRearIndex = thisTrain.ValidRoute[0].GetRouteIndex(nextRear.TCSectionIndex, 0);
                                usedTrainRouteIndex = thisTrain.ValidRoute[0].GetRouteIndex(Index, 0);
                            }

                            // if not found either, build temp route
                            if (nextRouteFrontIndex < 0 || nextRouteRearIndex < 0)
                            {
                                Train.TCSubpathRoute tempRoute = signalRef.BuildTempRoute(nextTrain.Train, nextFront.TCSectionIndex, nextFront.TCOffset, nextFront.TCDirection,
                                    nextTrain.Train.Length, true, true, false);
                                nextRouteFrontIndex = tempRoute.GetRouteIndex(nextFront.TCSectionIndex, 0);
                                nextRouteRearIndex = tempRoute.GetRouteIndex(nextRear.TCSectionIndex, 0);
                                usedTrainRouteIndex = tempRoute.GetRouteIndex(Index, 0);
                            }

                            if (nextRouteRearIndex < usedTrainRouteIndex)
                            {
                                if (nextRouteFrontIndex > usedTrainRouteIndex) // train spans section, so we're in the middle of it - return 0
                                {
                                    distanceTrainAheadM = offset; // set distance to 0 (offset is deducted later)
                                    trainFound = nextTrain.Train;
                                } // otherwise train is not in front, so don't use it
                            }
                            else  // if index is greater, train has moved on
                            {
                                // check if still ahead of us

                                if (thisTrain != null && thisTrain.ValidRoute != null)
                                {
                                    int lastSectionIndex = thisTrain.ValidRoute[0].GetRouteIndex(nextRear.TCSectionIndex, thisTrain.PresentPosition[0].RouteListIndex);
                                    if (lastSectionIndex >= thisTrain.PresentPosition[0].RouteListIndex)
                                    {
                                        distanceTrainAheadM = Length;  // offset is deducted later
                                        for (int isection = nextTrainRouteIndex + 1; isection <= nextRear.RouteListIndex - 1; isection++)
                                        {
                                            distanceTrainAheadM += signalRef.TrackCircuitList[nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex][isection].TCSectionIndex].Length;
                                        }
                                        distanceTrainAheadM += nextTrain.Train.PresentPosition[1].TCOffset;
                                        trainFound = nextTrain.Train;
                                    }
                                }
                            }
                        }
                    }
                    else // reverse direction, so we're looking at the front - use section length - offset as position
                    {
                        float thisTrainOffset = Length - nextFront.TCOffset;
                        if (nextFront.TCSectionIndex == Index)  // front of train in section
                        {
                            float thisTrainDistanceM = thisTrainOffset;

                            if (thisTrainDistanceM < distanceTrainAheadM && thisTrainOffset >= offset) // train is nearest train and in front
                            {
                                distanceTrainAheadM = thisTrainDistanceM;
                                trainFound = nextTrain.Train;
                            }
                            // extra test : if front is beyond other train but rear is not, train is considered to be still in front (at distance = offset)
                            // this can happen in pre-run mode due to large interval
                            if (thisTrain != null && thisTrainDistanceM < distanceTrainAheadM && thisTrainOffset < offset)
                            {
                                if ((!signalRef.Simulator.TimetableMode && thisTrainOffset >= (offset - nextTrain.Train.Length)) ||
                                    (signalRef.Simulator.TimetableMode && thisTrainOffset >= (offset - thisTrain.Length)))
                                {
                                    distanceTrainAheadM = offset;
                                    trainFound = nextTrain.Train;
                                }
                            }
                        }
                        else
                        {
                            int nextRouteFrontIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(nextFront.TCSectionIndex, 0);
                            int nextRouteRearIndex = nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex].GetRouteIndex(nextRear.TCSectionIndex, 0);
                            int usedTrainRouteIndex = nextTrainRouteIndex;

                            // if not on route, try this trains route
                            if (thisTrain != null && (nextRouteFrontIndex < 0 || nextRouteRearIndex < 0))
                            {
                                nextRouteFrontIndex = thisTrain.ValidRoute[0].GetRouteIndex(nextFront.TCSectionIndex, 0);
                                nextRouteRearIndex = thisTrain.ValidRoute[0].GetRouteIndex(nextRear.TCSectionIndex, 0);
                                usedTrainRouteIndex = thisTrain.ValidRoute[0].GetRouteIndex(Index, 0);
                            }

                            // if not found either, build temp route
                            if (nextRouteFrontIndex < 0 || nextRouteRearIndex < 0)
                            {
                                Train.TCSubpathRoute tempRoute = signalRef.BuildTempRoute(nextTrain.Train, nextFront.TCSectionIndex, nextFront.TCOffset, nextFront.TCDirection,
                                    nextTrain.Train.Length, true, true, false);
                                nextRouteFrontIndex = tempRoute.GetRouteIndex(nextFront.TCSectionIndex, 0);
                                nextRouteRearIndex = tempRoute.GetRouteIndex(nextRear.TCSectionIndex, 0);
                                usedTrainRouteIndex = tempRoute.GetRouteIndex(Index, 0);
                            }

                            if (nextRouteFrontIndex < usedTrainRouteIndex)
                            {
                                if (nextRouteRearIndex > usedTrainRouteIndex)  // train spans section so we're in the middle of it
                                {
                                    distanceTrainAheadM = offset; // set distance to 0 (offset is deducted later)
                                    trainFound = nextTrain.Train;
                                } // else train is not in front of us
                            }
                            else  // if index is greater, train has moved on - return section length minus offset
                            {
                                // check if still ahead of us
                                if (thisTrain != null && thisTrain.ValidRoute != null)
                                {
                                    int lastSectionIndex = thisTrain.ValidRoute[0].GetRouteIndex(nextRear.TCSectionIndex, thisTrain.PresentPosition[0].RouteListIndex);
                                    if (lastSectionIndex > thisTrain.PresentPosition[0].RouteListIndex)
                                    {
                                        distanceTrainAheadM = Length;  // offset is deducted later
                                        for (int isection = nextTrainRouteIndex + 1; isection <= nextRear.RouteListIndex - 1; isection++)
                                        {
                                            distanceTrainAheadM += signalRef.TrackCircuitList[nextTrain.Train.ValidRoute[nextTrain.TrainRouteDirectionIndex][isection].TCSectionIndex].Length;
                                        }
                                        distanceTrainAheadM += nextTrain.Train.PresentPosition[1].TCOffset;
                                        trainFound = nextTrain.Train;
                                    }
                                }
                            }
                        }

                    }
                }
                else
                {
                    distanceTrainAheadM = offset; // train is off its route - check if track occupied by train is ahead or behind us //

                    if (thisTrain != null)
                    {
                        int presentFront = thisTrain.PresentPosition[0].RouteListIndex;

                        foreach (TrackCircuitSection occSection in nextTrain.Train.OccupiedTrack)
                        {
                            int otherSectionIndex = thisTrain.TCRoute.TCRouteSubpaths[thisTrain.TCRoute.activeSubpath].GetRouteIndex(occSection.Index, 0);

                            // other index is lower - train is behind us
                            if (otherSectionIndex >= 0 && otherSectionIndex < presentFront)
                            {
                                trainFound = null;
                                continue;
                            }
                            // other index is higher - train is in front of us
                            else if (otherSectionIndex >= 0 && otherSectionIndex > presentFront)
                            {
                                trainFound = nextTrain.Train;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        // else assume ahead of us - assume full section occupied, offset is deducted later //
                        trainFound = nextTrain.Train;
                    }
                }
            }

            Dictionary<Train, float> result = new Dictionary<Train, float>();
            if (trainFound != null)
                if (distanceTrainAheadM >= offset) // train is indeed ahead
                {
                    result.Add(trainFound, (distanceTrainAheadM - offset));
                }
            return (result);
        }

        /// <summary>
        /// Get next active link
        /// </summary>
        public TrPin GetNextActiveLink(int direction, int lastIndex)
        {

            // Crossover

            if (CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
            {
                int inPinIndex = direction == 0 ? 1 : 0;
                if (Pins[inPinIndex, 0].Link == lastIndex)
                {
                    return (ActivePins[direction, 0]);
                }
                else if (Pins[inPinIndex, 1].Link == lastIndex)
                {
                    return (ActivePins[direction, 1]);
                }
                else
                {
                    TrPin dummyPin = new TrPin() { Direction = -1, Link = -1 };
                    return (dummyPin);
                }
            }

            // All other sections

            if (ActivePins[direction, 0].Link > 0)
            {
                return (ActivePins[direction, 0]);
            }

            return (ActivePins[direction, 1]);
        }

        /// <summary>
        /// Get distance between objects
        /// </summary>
        public float GetDistanceBetweenObjects(int startSectionIndex, float startOffset, int startDirection,
            int endSectionIndex, float endOffset)
        {
            int thisSectionIndex = startSectionIndex;
            int direction = startDirection;

            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

            float distanceM = 0.0f;
            int lastIndex = -2;  // set to non-occuring value

            while (thisSectionIndex != endSectionIndex && thisSectionIndex > 0)
            {
                distanceM += thisSection.Length;
                TrPin nextLink = thisSection.GetNextActiveLink(direction, lastIndex);

                lastIndex = thisSectionIndex;
                thisSectionIndex = nextLink.Link;
                direction = nextLink.Direction;

                if (thisSectionIndex > 0)
                {
                    thisSection = signalRef.TrackCircuitList[thisSectionIndex];
                    if (thisSectionIndex == startSectionIndex)  // loop found - return distance found sofar
                    {
                        distanceM -= startOffset;
                        return (distanceM);
                    }
                }
            }

            // use found distance, correct for begin and end offset

            if (thisSectionIndex == endSectionIndex)
            {
                distanceM += endOffset - startOffset;
                return (distanceM);
            }

            return (-1.0f);
        }

        /// <summary>
        /// Check if train can be placed in section
        /// </summary>
        public bool CanPlaceTrain(Train thisTrain, float offset, float trainLength)
        {

            if (!IsAvailable(thisTrain))
            {
                if (CircuitState.TrainReserved != null ||
                CircuitState.TrainClaimed.Count > 0)
                {
                    return false;
                }

                if (DeadlockTraps.ContainsKey(thisTrain.Number))
                {
                    return false;  // prevent deadlock
                }

                if (CircuitType != TrackCircuitType.Normal) // other than normal and not clear - return false
                {
                    return false;
                }

                if (offset == 0 && trainLength > Length) // train spans section
                {
                    return false;
                }

                // get other trains in section

                Dictionary<Train, float> trainInfo;
                float offsetFromStart = offset;

                // test train ahead of rear end (for non-placed trains, always use direction 0)

                if (thisTrain.PresentPosition[1].TCSectionIndex == Index)
                {
                    trainInfo = TestTrainAhead(thisTrain,
                            offsetFromStart, thisTrain.PresentPosition[1].TCDirection); // rear end in this section, use offset
                }
                else
                {
                    offsetFromStart = 0.0f;
                    trainInfo = TestTrainAhead(thisTrain,
                            0.0f, thisTrain.PresentPosition[1].TCDirection); // test from start
                }

                if (trainInfo.Count > 0)
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo)
                    {
                        if (trainAhead.Value < trainLength) // train ahead not clear
                        {
                            return false;
                        }
                        else
                        {
                            var trainPosition = trainAhead.Key.PresentPosition[trainAhead.Key.MUDirection == Direction.Forward ? 0 : 1];
                            if (trainPosition.TCSectionIndex == Index && trainAhead.Key.SpeedMpS > 0 && trainPosition.TCDirection != thisTrain.PresentPosition[0].TCDirection)
                            {
                                return false;   // train is moving towards us
                            }
                        }
                    }
                }

                // test train behind of front end

                int revDirection = thisTrain.PresentPosition[0].TCDirection == 0 ? 1 : 0;
                if (thisTrain.PresentPosition[0].TCSectionIndex == Index)
                {
                    float offsetFromEnd = Length - (trainLength + offsetFromStart);
                    trainInfo = TestTrainAhead(thisTrain, offsetFromEnd, revDirection); // test remaining length
                }
                else
                {
                    trainInfo = TestTrainAhead(thisTrain, 0.0f, revDirection); // test full section
                }

                if (trainInfo.Count > 0)
                {
                    foreach (KeyValuePair<Train, float> trainAhead in trainInfo)
                    {
                        if (trainAhead.Value < trainLength) // train behind not clear
                        {
                            return false;
                        }
                    }
                }

            }

            return true;
        }

        /// <summary>
        /// Set deadlock trap for all trains which deadlock from this section at begin section
        /// </summary>
        public void SetDeadlockTrap(Train thisTrain, List<Dictionary<int, int>> thisDeadlock)
        {
            foreach (Dictionary<int, int> deadlockInfo in thisDeadlock)
            {
                foreach (KeyValuePair<int, int> deadlockDetails in deadlockInfo)
                {
                    int otherTrainNumber = deadlockDetails.Key;
                    Train otherTrain = thisTrain.GetOtherTrainByNumber(deadlockDetails.Key);

                    int endSectionIndex = deadlockDetails.Value;

                    // check if endsection still in path
                    if (thisTrain.ValidRoute[0].GetRouteIndex(endSectionIndex, thisTrain.PresentPosition[0].RouteListIndex) >= 0)
                    {
                        TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];

                        // if other section allready set do not set deadlock
                        if (otherTrain != null && endSection.IsSet(otherTrain, true))
                            break;

                        if (DeadlockTraps.ContainsKey(thisTrain.Number))
                        {
                            List<int> thisTrap = DeadlockTraps[thisTrain.Number];
                            if (thisTrap.Contains(otherTrainNumber))
                                break;  // cannot set deadlock for train which has deadlock on this end
                        }

                        if (endSection.DeadlockTraps.ContainsKey(otherTrainNumber))
                        {
                            if (!endSection.DeadlockTraps[otherTrainNumber].Contains(thisTrain.Number))
                            {
                                endSection.DeadlockTraps[otherTrainNumber].Add(thisTrain.Number);
                            }
                        }
                        else
                        {
                            List<int> deadlockList = new List<int>();
                            deadlockList.Add(thisTrain.Number);
                            endSection.DeadlockTraps.Add(otherTrainNumber, deadlockList);
                        }

                        if (!endSection.DeadlockActives.Contains(thisTrain.Number))
                        {
                            endSection.DeadlockActives.Add(thisTrain.Number);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set deadlock trap for individual train at end section
        /// </summary>
        public void SetDeadlockTrap(int thisTrainNumber, int otherTrainNumber)
        {

#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\Temp\deadlock.txt",
                    "\n **** Set deadlock " + Index + " for train : " + thisTrainNumber.ToString() + " with train :  " + otherTrainNumber.ToString() + "\n");
#endif

            if (DeadlockTraps.ContainsKey(otherTrainNumber))
            {
                if (!DeadlockTraps[otherTrainNumber].Contains(thisTrainNumber))
                {
                    DeadlockTraps[otherTrainNumber].Add(thisTrainNumber);
                }
            }
            else
            {
                List<int> deadlockList = new List<int>();
                deadlockList.Add(thisTrainNumber);
                DeadlockTraps.Add(otherTrainNumber, deadlockList);
            }

            if (!DeadlockActives.Contains(thisTrainNumber))
            {
                DeadlockActives.Add(thisTrainNumber);
            }
        }

        /// <summary>
        /// Clear deadlock trap
        /// </summary>
        public void ClearDeadlockTrap(int thisTrainNumber)
        {
            List<int> deadlocksCleared = new List<int>();

            if (DeadlockActives.Contains(thisTrainNumber))
            {

#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\Temp\deadlock.txt",
                    "\n **** Clearing deadlocks " + Index + " for train : " + thisTrainNumber.ToString() + "\n");
#endif

                foreach (KeyValuePair<int, List<int>> thisDeadlock in DeadlockTraps)
                {
                    if (thisDeadlock.Value.Contains(thisTrainNumber))
                    {
                        thisDeadlock.Value.Remove(thisTrainNumber);
                        if (thisDeadlock.Value.Count <= 0)
                        {
                            deadlocksCleared.Add(thisDeadlock.Key);
                        }
                    }
                }
                DeadlockActives.Remove(thisTrainNumber);
            }

            foreach (int deadlockKey in deadlocksCleared)
            {
                DeadlockTraps.Remove(deadlockKey);
            }

            DeadlockAwaited.Remove(thisTrainNumber);

#if DEBUG_DEADLOCK
            File.AppendAllText(@"C:\Temp\deadlock.txt",
                "\n **** \n");
#endif
        }

        /// <summary>
        /// Check if train is waiting for deadlock
        /// </summary>
        public bool CheckDeadlockAwaited(int trainNumber)
        {
            int totalCount = DeadlockAwaited.Count;
            if (DeadlockAwaited.Contains(trainNumber))
                totalCount--;
            return (totalCount > 0);
        }

        /// <summary>
        /// Clear track sections from train behind
        /// </summary>
        public void ClearSectionsOfTrainBehind(Train.TrainRouted trainRouted, TrackCircuitSection startTCSectionIndex)
        {
            int startindex = 0;
            startTCSectionIndex.UnreserveTrain(trainRouted, true);
            for (int iindex = 0; iindex < trainRouted.Train.ValidRoute[0].Count; iindex++)
            {
                if (startTCSectionIndex == signalRef.TrackCircuitList[trainRouted.Train.ValidRoute[0][iindex].TCSectionIndex])
                {
                    startindex = iindex + 1;
                    break;
                }
            }

            for (int iindex = startindex; iindex < trainRouted.Train.ValidRoute[0].Count; iindex++)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[trainRouted.Train.ValidRoute[0][iindex].TCSectionIndex];
                if (thisSection.CircuitState.TrainReserved == null)
                    break;
                thisSection.UnreserveTrain(trainRouted, true);
            }
            // signalRef.BreakDownRouteList(trainRouted.Train.ValidRoute[trainRouted.TrainRouteDirectionIndex], startindex-1, trainRouted);
            // Reset signal behind new train
            for (int iindex = startindex - 2; iindex >= trainRouted.Train.PresentPosition[0].RouteListIndex; iindex--)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[trainRouted.Train.ValidRoute[trainRouted.TrainRouteDirectionIndex][iindex].TCSectionIndex];
                SignalObject thisSignal = thisSection.EndSignals[trainRouted.Train.ValidRoute[trainRouted.TrainRouteDirectionIndex][iindex].Direction];
                if (thisSignal != null)
                {
                    thisSignal.ResetSignal(false);
                    break;
                }
            }
        }
    }
}
