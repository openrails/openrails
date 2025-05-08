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
using System.Linq;
using System.Text;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using ORTS.Common;
using Event = Orts.Common.Event;

namespace Orts.Simulation.Signalling
{
    public enum SignalObjectType
    {
        Signal,
        SpeedSignal,
        SpeedPost,
    }

    public enum HoldState                // signal is locked in hold
    {
        None,                            // signal is clear
        StationStop,                     // because of station stop
        ManualLock,                      // because of manual lock. 
        ManualPass,                      // Sometime you want to set a light green, especially in MP
        ManualApproach,                  // Sometime to set approach, in MP again
                                         //PLEASE DO NOT CHANGE THE ORDER OF THESE ENUMS
    }

    public class SignalObject
    {
        public enum InternalBlockstate
        {
            Reserved,                   // all sections reserved for requiring train       //
            Reservable,                 // all sections clear and reservable for train    //
            OccupiedSameDirection,      // occupied by train moving in same direction      //
            ReservedOther,              // reserved for other train                        //
            ForcedWait,                 // train is forced to wait for other train         //
            OccupiedOppositeDirection,  // occupied by train moving in opposite direction  //
            Open,                       // sections are claimed and not accesible          //
            Blocked,                    // switch locked against train                     //
        }

        public enum Permission
        {
            Granted,
            Requested,
            Denied,
        }

        public readonly Signals signalRef;               // reference to overlaying Signal class
        public static SignalObject[] signalObjects;
        public static TrackNode[] trackNodes;
        public static TrItem[] trItems;
        public SignalWorldObject WorldObject;   // Signal World Object information
        public SpeedPostWorldObject SpeedPostWorldObject; // Speed Post World Object information

        public int trackNode;                   // Track node which contains this signal
        public int trRefIndex;                  // Index to TrItemRef within Track Node 

        public int TCReference = -1;            // Reference to TrackCircuit (index)
        public float TCOffset;                  // Position within TrackCircuit
        public int TCDirection;                 // Direction within TrackCircuit
        public int TCNextTC = -1;               // Index of next TrackCircuit (NORMAL signals only)
        public int TCNextDirection;             // Direction of next TrackCircuit 
        public int? nextSwitchIndex = null;     // index of first switch in path

        public List<int> JunctionsPassed = new List<int>();  // Junctions which are passed checking next signal //

        public int? platformRef = null;         // platform reference, used for request stop
        public float? visDistance = null;       // visibility distance for request stop

        public int thisRef;                     // This signal's reference.
        public int direction;                   // Direction facing on track

        public SignalObjectType Type { get; protected set; }
        public List<SignalHead> SignalHeads = new List<SignalHead>();

        public int SignalNumClearAhead_MSTS = -2;    // Overall maximum SignalNumClearAhead over all heads (MSTS calculation)
        public int SignalNumClearAhead_ORTS = -2;    // Overall maximum SignalNumClearAhead over all heads (ORTS calculation)
        public int SignalNumClearAheadActive = -2;   // Active SignalNumClearAhead (for ORST calculation only, as set by script)
        public int SignalNumNormalHeads;             // no. of normal signal heads in signal
        public int ReqNumClearAhead;                 // Passed on value for SignalNumClearAhead

        public int draw_state;                  // actual signal state
        public Dictionary<int, int> localStorage = new Dictionary<int, int>();  // list to store local script variables
        public bool noupdate = false;                // set if signal does not required updates (fixed signals)

        public Train.TrainRouted enabledTrain;  // full train structure for which signal is enabled

        private InternalBlockstate internalBlockState = InternalBlockstate.Open;    // internal blockstate
        public Permission hasPermission = Permission.Denied;  // Permission to pass red signal
        public HoldState holdState = HoldState.None;
        public bool CallOnManuallyAllowed;

        public Dictionary<SignalFunction, int> sigfound = new Dictionary<SignalFunction, int>();  // active next signal - used for signals with NORMAL heads only
        public int reqNormalSignal = -1;              // ref of normal signal requesting route clearing (only used for signals without NORMAL heads)
        private Dictionary<SignalFunction, int> defaultNextSignal = new Dictionary<SignalFunction, int>();  // default next signal
        public Traveller tdbtraveller;          // TDB traveller to determine distance between objects

        public Train.TCSubpathRoute signalRoute = new Train.TCSubpathRoute();  // train route from signal
        public int trainRouteDirectionIndex;    // direction index in train route array (usually 0, value 1 valid for Manual only)
        public int thisTrainRouteIndex;        // index of section after signal in train route list

        private Train.TCSubpathRoute fixedRoute = new Train.TCSubpathRoute();     // fixed route from signal
        public bool hasFixedRoute;              // signal has fixed route
        private bool fullRoute;                 // required route is full route to next signal or end-of-track
        private bool AllowPartRoute = false;    // signal is always allowed to clear unto partial route
        private bool propagated;                // route request propagated to next signal
        private bool isPropagated;              // route request for this signal was propagated from previous signal
        public bool ForcePropagation = false;   // Force propagation (used in case of signals at very short distance)

        public bool ApproachControlCleared;     // set in case signal has cleared on approach control
        public bool ApproachControlSet;         // set in case approach control is active
        public bool ClaimLocked;                // claim is locked in case of approach control
        public bool ForcePropOnApproachControl; // force propagation if signal is held on close control
        public double TimingTriggerValue;        // used timing trigger if time trigger is required, hold trigger time

        public bool StationHold = false;        // Set if signal must be held at station - processed by signal script
        protected List<KeyValuePair<int, int>> LockedTrains;

        public bool CallOnEnabled = false;      // set if signal script file uses CallOn functionality

        private readonly List<int> passedSections = new List<int>();
        private readonly List<int> SectionsWithAlternativePath = new List<int>();
        private readonly List<int> SectionsWithAltPathSet = new List<int>();
        private readonly static List<int> sectionsInRoute = new List<int>();
        private static readonly ObjectSpeedInfo DefaultSpeedInfo = new ObjectSpeedInfo(-1, -1, false, false, 0, false);

        public bool enabled
        {
            get
            {
                if (MPManager.IsMultiPlayer() && MPManager.PreferGreen == true) return true;
                return enabledTrain != null;
            }
        }

        public MstsBlockState blockState
        {
            get
            {
                MstsBlockState lstate = MstsBlockState.JN_OBSTRUCTED;
                switch (internalBlockState)
                {
                    case InternalBlockstate.Reserved:
                    case InternalBlockstate.Reservable:
                        lstate = MstsBlockState.CLEAR;
                        break;
                    case InternalBlockstate.OccupiedSameDirection:
                        lstate = MstsBlockState.OCCUPIED;
                        break;
                    default:
                        lstate = MstsBlockState.JN_OBSTRUCTED;
                        break;
                }

                return lstate;
            }
        }

        public int trItem
        {
            get
            {
                return trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
            }
        }

        public int revDir                //  Needed because signal faces train!
        {
            get
            {
                return direction == 0 ? 1 : 0;
            }
        }


        /// <summary>
        ///  Constructor for empty item
        /// </summary>
        public SignalObject(Signals signalReference, SignalObjectType type)
        {
            signalRef = signalReference;

            Type = type;

            LockedTrains = new List<KeyValuePair<int, int>>();

            foreach (SignalFunction function in signalRef.SignalFunctions.Values)
            {
                sigfound.Add(function, -1);
                defaultNextSignal.Add(function, -1);
            }
        }

        /// <summary>
        ///  Constructor for Copy 
        /// </summary>
        public SignalObject(SignalObject copy)
        {
            signalRef = copy.signalRef;
            Type = copy.Type;
            switch (Type)
            {
                case SignalObjectType.Signal:
                case SignalObjectType.SpeedSignal:
                    WorldObject = new SignalWorldObject(copy.WorldObject);
                    break;

                case SignalObjectType.SpeedPost:
                    SpeedPostWorldObject = new SpeedPostWorldObject(copy.SpeedPostWorldObject);
                    break;
            }

            trackNode = copy.trackNode;
            LockedTrains = new List<KeyValuePair<int, int>>();
            foreach (var lockInfo in copy.LockedTrains)
            {
                KeyValuePair<int, int> oneLock = new KeyValuePair<int, int>(lockInfo.Key, lockInfo.Value);
                LockedTrains.Add(oneLock);
            }

            TCReference = copy.TCReference;
            TCOffset = copy.TCOffset;
            TCDirection = copy.TCDirection;
            TCNextTC = copy.TCNextTC;
            TCNextDirection = copy.TCNextDirection;

            direction = copy.direction;
            SignalNumClearAhead_MSTS = copy.SignalNumClearAhead_MSTS;
            SignalNumClearAhead_ORTS = copy.SignalNumClearAhead_ORTS;
            SignalNumClearAheadActive = copy.SignalNumClearAheadActive;
            SignalNumNormalHeads = copy.SignalNumNormalHeads;

            draw_state = copy.draw_state;
            internalBlockState = copy.internalBlockState;
            hasPermission = copy.hasPermission;

            tdbtraveller = new Traveller(copy.tdbtraveller);

            sigfound = new Dictionary<SignalFunction, int>(copy.sigfound);
            defaultNextSignal = new Dictionary<SignalFunction, int>(copy.defaultNextSignal);
        }

        /// <summary>
        /// Constructor for restore
        /// IMPORTANT : enabled train is restore temporarily as Trains are restored later as Signals
        /// Full restore of train link follows in RestoreTrains
        /// </summary>
        public void Restore(Simulator simulator, BinaryReader inf)
        {
            int trainNumber = inf.ReadInt32();

            int sigfoundLength = inf.ReadInt32();
            foreach (SignalFunction function in signalRef.SignalFunctions.Values)
            {
                sigfound[function] = inf.ReadInt32();
            }

            bool validRoute = inf.ReadBoolean();

            if (validRoute)
            {
                signalRoute = new Train.TCSubpathRoute(inf);
            }

            thisTrainRouteIndex = inf.ReadInt32();
            holdState = (HoldState)inf.ReadInt32();

            int totalJnPassed = inf.ReadInt32();

            for (int iJn = 0; iJn < totalJnPassed; iJn++)
            {
                int thisJunction = inf.ReadInt32();
                JunctionsPassed.Add(thisJunction);
                signalRef.TrackCircuitList[thisJunction].SignalsPassingRoutes.Add(thisRef);
            }

            fullRoute = inf.ReadBoolean();
            AllowPartRoute = inf.ReadBoolean();
            propagated = inf.ReadBoolean();
            isPropagated = inf.ReadBoolean();
            ForcePropagation = false; // preset (not stored)
            SignalNumClearAheadActive = inf.ReadInt32();
            ReqNumClearAhead = inf.ReadInt32();
            StationHold = inf.ReadBoolean();
            ApproachControlCleared = inf.ReadBoolean();
            ApproachControlSet = inf.ReadBoolean();
            ClaimLocked = inf.ReadBoolean();
            ForcePropOnApproachControl = inf.ReadBoolean();
            hasPermission = (Permission)inf.ReadInt32();

            // set dummy train, route direction index will be set later on restore of train

            enabledTrain = null;

            if (trainNumber >= 0)
            {
                Train thisTrain = new Train(simulator, trainNumber);
                Train.TrainRouted thisTrainRouted = new Train.TrainRouted(thisTrain, 0);
                enabledTrain = thisTrainRouted;
            }
            //  Retrieve lock table
            LockedTrains = new List<KeyValuePair<int, int>>();
            int cntLock = inf.ReadInt32();
            for (int cnt = 0; cnt < cntLock; cnt++)
            {
                KeyValuePair<int, int> lockInfo = new KeyValuePair<int, int>(inf.ReadInt32(), inf.ReadInt32());
                LockedTrains.Add(lockInfo);

            }
        }


        /// <summary>
        /// Restore Train Reference
        /// </summary>
        public void RestoreTrains(List<Train> trains)
        {
            if (enabledTrain != null)
            {
                int number = enabledTrain.Train.Number;

                Train foundTrain = Signals.FindTrain(number, trains);

                // check if this signal is next signal forward for this train

                if (foundTrain != null && foundTrain.NextSignalObject[0] != null && this.thisRef == foundTrain.NextSignalObject[0].thisRef)
                {
                    enabledTrain = foundTrain.routedForward;
                    foundTrain.NextSignalObject[0] = this;
                }

                // check if this signal is next signal backward for this train

                else if (foundTrain != null && foundTrain.NextSignalObject[1] != null && this.thisRef == foundTrain.NextSignalObject[1].thisRef)
                {
                    enabledTrain = foundTrain.routedBackward;
                    foundTrain.NextSignalObject[1] = this;
                }
                else
                {
                    // check if this section is reserved for this train

                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[TCReference];
                    if (thisSection.CircuitState.TrainReserved != null && thisSection.CircuitState.TrainReserved.Train.Number == number)
                    {
                        enabledTrain = thisSection.CircuitState.TrainReserved;
                    }
                    else if (thisSection.CircuitState.HasTrainsOccupying())
                    {
                        List<Train.TrainRouted> trainList = thisSection.CircuitState.TrainsOccupying();
                        float? offsetInSection = null;

                        foreach (var thisRouted in trainList)
                        {
                            var thisTrain = thisRouted.Train;
                            var thisOffset = thisTrain.PresentPosition[0].TCOffset;
                            if (!offsetInSection.HasValue || thisOffset > offsetInSection)
                            {
                                offsetInSection = thisOffset;
                                if (thisTrain.Number == number)
                                {
                                    enabledTrain = thisRouted;
                                    thisTrain.NextSignalObject[0] = this;
                                }
                                else
                                {
                                    enabledTrain = null;
                                }
                            }
                        }
                    }
                    else
                    {
                        enabledTrain = null; // reset - train not found
                    }
                }
            }
        }

        /// <summary>
        /// Restore Signal Aspect based on train information
        /// Process non-propagated signals only, others are updated through propagation
        /// </summary>
        public void RestoreAspect()
        {
            if (enabledTrain != null && !isPropagated)
            {
                if (isSignalNormal())
                {
                    checkRouteState(false, signalRoute, enabledTrain);
                    propagateRequest();
                    StateUpdate();
                }
                else
                {
                    getBlockState_notRouted();
                    StateUpdate();
                }
            }
        }

        public void Save(BinaryWriter outf)
        {
            if (enabledTrain == null)
            {
                outf.Write(-1);
            }
            else
            {
                outf.Write(enabledTrain.Train.Number);
            }

            outf.Write(sigfound.Count);
            foreach (int thisSig in sigfound.Values)
            {
                outf.Write(thisSig);
            }

            if (signalRoute == null)
            {
                outf.Write(false);
            }
            else
            {
                outf.Write(true);
                signalRoute.Save(outf);
            }

            outf.Write(thisTrainRouteIndex);
            outf.Write((int)holdState);

            outf.Write(JunctionsPassed.Count);
            if (JunctionsPassed.Count > 0)
            {
                foreach (int thisJunction in JunctionsPassed)
                {
                    outf.Write(thisJunction);
                }
            }

            outf.Write(fullRoute);
            outf.Write(AllowPartRoute);
            outf.Write(propagated);
            outf.Write(isPropagated);
            outf.Write(SignalNumClearAheadActive);
            outf.Write(ReqNumClearAhead);
            outf.Write(StationHold);
            outf.Write(ApproachControlCleared);
            outf.Write(ApproachControlSet);
            outf.Write(ClaimLocked);
            outf.Write(ForcePropOnApproachControl);
            outf.Write((int)hasPermission);
            outf.Write(LockedTrains.Count);
            for (int cnt = 0; cnt < LockedTrains.Count; cnt++)
            {
                outf.Write(LockedTrains[cnt].Key);
                outf.Write(LockedTrains[cnt].Value);
            }
        }

        /// <summary>
        /// return blockstate
        /// </summary>
        public MstsBlockState block_state()
        {
            return blockState;
        }

        /// <summary>
        /// return station hold state
        /// </summary>
        public bool isStationHold()
        {
            return StationHold;
        }

        /// <summary>
        /// setSignalDefaultNextSignal : set default next signal based on non-Junction tracks ahead
        /// this routine also sets fixed routes for signals which do not lead onto junction or crossover
        /// </summary>
        public void setSignalDefaultNextSignal()
        {
            int thisTC = TCReference;
            float position = TCOffset;
            int direction = TCDirection;
            bool setFixedRoute = false;

            // for normal signals : start at next TC

            if (TCNextTC > 0)
            {
                thisTC = TCNextTC;
                direction = TCNextDirection;
                position = 0.0f;
                setFixedRoute = true;
            }

            bool completedFixedRoute = !setFixedRoute;

            // get trackcircuit

            TrackCircuitSection thisSection = null;
            if (thisTC > 0)
            {
                thisSection = signalRef.TrackCircuitList[thisTC];
            }

            // set default

            foreach (SignalFunction function in signalRef.SignalFunctions.Values)
            {
                defaultNextSignal[function] = -1;
            }

            // loop through valid sections

            while (thisSection != null && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal)
            {

                if (!completedFixedRoute)
                {
                    fixedRoute.Add(new Train.TCRouteElement(thisSection.Index, direction));
                }

                // normal signal

                if (defaultNextSignal[SignalFunction.NORMAL] < 0)
                {
                    if (thisSection.EndSignals[direction] != null)
                    {
                        defaultNextSignal[SignalFunction.NORMAL] = thisSection.EndSignals[direction].thisRef;
                        completedFixedRoute = true;
                    }
                }

                // other signals
                foreach (SignalFunction function in signalRef.SignalFunctions.Values)
                {
                    if (function != SignalFunction.NORMAL)
                    {
                        TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction][function];
                        bool signalFound = defaultNextSignal[function] >= 0;
                        for (int iItem = 0; iItem < thisList.TrackCircuitItem.Count && !signalFound; iItem++)
                        {
                            TrackCircuitSignalItem thisItem = thisList.TrackCircuitItem[iItem];
                            if (thisItem.SignalRef.thisRef != thisRef && (thisSection.Index != thisTC || thisItem.SignalLocation > position))
                            {
                                defaultNextSignal[function] = thisItem.SignalRef.thisRef;
                                signalFound = true;
                            }
                        }
                    }
                }

                int pinIndex = direction;
                direction = thisSection.Pins[pinIndex, 0].Direction;
                thisSection = signalRef.TrackCircuitList[thisSection.Pins[pinIndex, 0].Link];
            }

            // copy default as valid items

            foreach (SignalFunction function in signalRef.SignalFunctions.Values)
            {
                sigfound[function] = defaultNextSignal[function];
            }

            // Allow use of fixed route if ended on END_OF_TRACK

            if (thisSection != null && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
            {
                completedFixedRoute = true;
            }

            // if valid next normal signal found, signal has fixed route

            if (setFixedRoute && completedFixedRoute)
            {
                hasFixedRoute = true;
                fullRoute = true;
            }
            else
            {
                hasFixedRoute = false;
                fixedRoute.Clear();
            }
        }

        /// <summary>
        /// isSignalNormal : Returns true if at least one signal head is type normal.
        /// </summary>
        public bool isSignalNormal()
        {
            for (int i = 0; i < SignalHeads.Count; i++)
                if (SignalHeads[i].Function == SignalFunction.NORMAL)
                    return true;
            return false;
        }

        /// <summary>
        /// isORTSSignalType : Returns true if at least one signal head is of required type
        /// </summary>
        public bool isORTSSignalType(SignalFunction function)
        {
            for (int i = 0; i < SignalHeads.Count; i++)
                if (SignalHeads[i].Function == function)
                    return true;
            return false;
        }

        /// <summary>
        /// next_sig_mr : returns most restrictive state of next signal of required type
        /// </summary>
        public MstsSignalAspect next_sig_mr(SignalFunction function)
        {
            int nextSignal = sigfound[function];
            if (nextSignal < 0)
            {
                nextSignal = SONextSignal(function);
                sigfound[function] = nextSignal;
            }

            if (nextSignal >= 0)
            {
                return signalObjects[nextSignal].this_sig_mr(function);
            }
            else
            {
                return MstsSignalAspect.STOP;
            }
        }

        /// <summary>
        /// next_sig_lr : returns least restrictive state of next signal of required type
        /// </summary>
        public MstsSignalAspect next_sig_lr(SignalFunction function)
        {
            int nextSignal = sigfound[function];
            if (nextSignal < 0)
            {
                nextSignal = SONextSignal(function);
                sigfound[function] = nextSignal;
            }
            if (nextSignal >= 0)
            {
                return signalObjects[nextSignal].this_sig_lr(function);
            }
            else
            {
                return MstsSignalAspect.STOP;
            }
        }

        /// <summary>
        /// next_nsig_lr : returns least restrictive state of next signal of required type of the nth signal ahead
        /// </summary>
        public MstsSignalAspect next_nsig_lr(SignalFunction function, int nsignal, string dumpfile)
        {
            int foundsignal = 0;
            MstsSignalAspect foundAspect = MstsSignalAspect.CLEAR_2;
            SignalObject nextSignalObject = this;

            while (foundsignal < nsignal && foundAspect != MstsSignalAspect.STOP)
            {
                // use sigfound
                int nextSignal = nextSignalObject.sigfound[function];

                // sigfound not set, try direct search
                if (nextSignal < 0)
                {
                    nextSignal = SONextSignal(function);
                    nextSignalObject.sigfound[function] = nextSignal;
                }

                // signal found : get state
                if (nextSignal >= 0)
                {
                    foundsignal++;

                    nextSignalObject = signalObjects[nextSignal];
                    foundAspect = nextSignalObject.this_sig_lr(function);

                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        File.AppendAllText(dumpfile, "\nNEXT_NSIG_LR : Found signal " + foundsignal + " : " + nextSignalObject.thisRef + " ; state = " + foundAspect + "\n");
                    }

                    // reached required signal or state is stop : return
                    if (foundsignal >= nsignal || foundAspect == MstsSignalAspect.STOP)
                    {
                        if (!String.IsNullOrEmpty(dumpfile))
                        {
                            File.AppendAllText(dumpfile, "NEXT_NSIG_LR : returned : " + foundAspect + "\n");
                        }
                        return foundAspect;
                    }
                }

                // signal not found : return stop
                else
                {
                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        File.AppendAllText(dumpfile, "NEXT_NSIG_LR : returned : " + foundAspect + " ; last found index : " + foundsignal + "\n");
                    }
                    return MstsSignalAspect.STOP;
                }
            }

            if (!String.IsNullOrEmpty(dumpfile))
            {
                File.AppendAllText(dumpfile, "NEXT_NSIG_LR : while loop exited ; last found index : " + foundsignal + "\n");
            }
            return MstsSignalAspect.STOP; // emergency exit - loop should normally have exited on return
        }


        /// <summary>
        /// opp_sig_mr
        /// </summary>

        /// normal version
        public MstsSignalAspect opp_sig_mr(SignalFunction function)
        {
            int signalFound = SONextSignalOpp(function);
            return signalFound >= 0 ? signalObjects[signalFound].this_sig_mr(function) : MstsSignalAspect.STOP;
        }

        /// debug version
        public MstsSignalAspect opp_sig_mr(SignalFunction function, ref SignalObject foundSignal)
        {
            int signalFound = SONextSignalOpp(function);
            foundSignal = signalFound >= 0 ? signalObjects[signalFound] : null;
            return signalFound >= 0 ? signalObjects[signalFound].this_sig_mr(function) : MstsSignalAspect.STOP;
        }


        /// <summary>
        /// opp_sig_lr
        /// </summary>

        /// normal version
        public MstsSignalAspect opp_sig_lr(SignalFunction function)
        {
            int signalFound = SONextSignalOpp(function);
            return signalFound >= 0 ? signalObjects[signalFound].this_sig_lr(function) : MstsSignalAspect.STOP;
        }

        /// debug version
        public MstsSignalAspect opp_sig_lr(SignalFunction function, ref SignalObject foundSignal)
        {
            int signalFound = SONextSignalOpp(function);
            foundSignal = signalFound >= 0 ? signalObjects[signalFound] : null;
            return signalFound >= 0 ? signalObjects[signalFound].this_sig_lr(function) : MstsSignalAspect.STOP;
        }


        /// <summary>
        /// this_sig_mr : Returns the most restrictive state of this signal's heads of required type
        /// </summary>

        /// <summary>
        /// standard version without state return
        /// </summary>
        public MstsSignalAspect this_sig_mr(SignalFunction function)
        {
            bool sigfound = false;
            return this_sig_mr(function, ref sigfound);
        }

        /// <summary>
        /// standard version without state return using MSTS type parameter
        /// </summary>
        public MstsSignalAspect this_sig_mr(MstsSignalFunction msfn_type)
        {
            bool sigfound = false;
            return this_sig_mr(SignalConfigurationFile.MstsSignalFunctions[msfn_type], ref sigfound);
        }

        /// <summary>
        /// additional version with state return
        /// </summary>
        public MstsSignalAspect this_sig_mr(SignalFunction function, ref bool sigfound)
        {
            MstsSignalAspect sigAsp = MstsSignalAspect.UNKNOWN;
            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.Function == function && sigHead.state < sigAsp)
                {
                    sigAsp = sigHead.state;
                }
            }
            if (sigAsp == MstsSignalAspect.UNKNOWN)
            {
                sigfound = false;
                return MstsSignalAspect.STOP;
            }
            else
            {
                sigfound = true;
                return sigAsp;
            }
        }

        /// <summary>
        /// additional version using valid route heads only
        /// </summary>
        public MstsSignalAspect this_sig_mr_routed(SignalFunction function, string dumpfile)
        {
            MstsSignalAspect sigAsp = MstsSignalAspect.UNKNOWN;
            var sob = new StringBuilder();
            sob.AppendFormat("  signal type 1 : {0} \n", thisRef);

            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.Function == function)
                {
                    if (sigHead.route_set() == 1)
                    {
                        if (sigHead.state < sigAsp)
                        {
                            sigAsp = sigHead.state;
                            if (dumpfile.Length > 1)
                            {
                                sob.AppendFormat("   {0} : routed correct : {1} \n", sigHead.TDBIndex, sigHead.state);
                            }
                        }
                    }
                    else if (dumpfile.Length > 1)
                    {
                        sob.AppendFormat("   {0} : routed incorrect \n", sigHead.TDBIndex);
                    }
                }
            }

            if (sigAsp == MstsSignalAspect.UNKNOWN)
            {
                if (dumpfile.Length > 1)
                {
                    sob.AppendFormat("    No valid routed head found, returned STOP \n\n");
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                return MstsSignalAspect.STOP;
            }
            else
            {
                if (dumpfile.Length > 1)
                {
                    sob.AppendFormat("    Returned state : {0} \n\n", sigAsp.ToString());
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                return sigAsp;
            }
        }//this_sig_mr


        /// <summary>
        /// this_sig_lr : Returns the least restrictive state of this signal's heads of required type
        /// </summary>

        /// <summary>
        /// standard version without state return
        /// </summary>
        public MstsSignalAspect this_sig_lr(SignalFunction function)
        {
            bool sigfound = false;
            return this_sig_lr(function, ref sigfound);
        }

        /// <summary>
        /// standard version without state return using MSTS type parameter
        /// </summary>
        public MstsSignalAspect this_sig_lr(MstsSignalFunction msfn_type)
        {
            bool sigfound = false;
            return this_sig_lr(SignalConfigurationFile.MstsSignalFunctions[msfn_type], ref sigfound);
        }

        /// <summary>
        /// additional version with state return
        /// </summary>
        public MstsSignalAspect this_sig_lr(SignalFunction function, ref bool sigfound)
        {
            MstsSignalAspect sigAsp = MstsSignalAspect.STOP;
            bool sigAspSet = false;
            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.Function == function && sigHead.state >= sigAsp)
                {
                    sigAsp = sigHead.state;
                    sigAspSet = true;
                }
            }

            sigfound = sigAspSet;

            if (sigAspSet)
            {
                return sigAsp;
            }
            else if (function == SignalFunction.NORMAL)
            {
                return MstsSignalAspect.CLEAR_2;
            }
            else
            {
                return MstsSignalAspect.STOP;
            }
        }

        /// <summary>
        /// this_sig_speed : Returns the speed related to the least restrictive aspect (for normal signal)
        /// </summary>
        public ObjectSpeedInfo this_sig_speed(SignalFunction function)
        {
            var sigAsp = MstsSignalAspect.STOP;
            var set_speed = DefaultSpeedInfo;

            foreach (SignalHead sigHead in SignalHeads)
            {
                if (sigHead.Function == function && sigHead.state >= sigAsp && sigHead.CurrentSpeedInfo != null)
                {
                    sigAsp = sigHead.state;
                    set_speed = sigHead.CurrentSpeedInfo;
                }
            }

            return set_speed;
        }

        /// <summary>
        /// next_sig_id : returns ident of next signal of required type
        /// </summary>
        public int next_sig_id(SignalFunction function)
        {
            int nextSignal = sigfound[function];
            if (nextSignal < 0)
            {
                nextSignal = SONextSignal(function);
                sigfound[function] = nextSignal;
            }

            if (nextSignal >= 0)
            {
                if (function != SignalFunction.NORMAL)
                {
                    SignalObject foundSignalObject = signalRef.SignalObjects[nextSignal];
                    if (isSignalNormal())
                    {
                        foundSignalObject.reqNormalSignal = thisRef;
                    }
                    else
                    {
                        foundSignalObject.reqNormalSignal = reqNormalSignal;
                    }
                }

                return nextSignal;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// next_nsig_id : returns ident of next signal of required type
        /// </summary>
        public int next_nsig_id(SignalFunction function, int nsignal)
        {
            int nextSignal = thisRef;
            int foundsignal = 0;
            SignalObject nextSignalObject = this;

            while (foundsignal < nsignal && nextSignal >= 0)
            {
                // use sigfound
                nextSignal = nextSignalObject.sigfound[function];

                // sigfound not set, try direct search
                if (nextSignal < 0)
                {
                    nextSignal = nextSignalObject.SONextSignal(function);
                    nextSignalObject.sigfound[function] = nextSignal;
                }

                // signal found
                if (nextSignal >= 0)
                {
                    foundsignal++;
                    nextSignalObject = signalObjects[nextSignal];
                }
            }

            if (nextSignal >= 0 && foundsignal > 0)
            {
                return nextSignal;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// opp_sig_id : returns ident of next opposite signal of required type
        /// </summary>
        public int opp_sig_id(SignalFunction function)
        {
            return SONextSignalOpp(function);
        }

        /// <summary>
        /// this_sig_noSpeedReduction : Returns the setting if speed must be reduced on RESTRICTED or STOP_AND_PROCEED
        /// returns TRUE if speed reduction must be suppressed
        /// </summary>
        public bool this_sig_noSpeedReduction(SignalFunction function)
        {
            var sigAsp = MstsSignalAspect.STOP;
            bool setNoReduction = false;

            foreach (SignalHead sigHead in SignalHeads.Where(sigHead => sigHead.Function == function))
            {
                if (sigHead.state >= sigAsp)
                {
                    sigAsp = sigHead.state;
                    if (sigAsp <= MstsSignalAspect.RESTRICTING && sigHead.CurrentSpeedInfo != null)
                    {
                        setNoReduction = sigHead.CurrentSpeedInfo.speed_noSpeedReductionOrIsTempSpeedReduction == 1;
                    }
                    else
                    {
                        setNoReduction = false;
                    }
                }
            }

            return setNoReduction;
        }

        /// <summary>
        /// SpeedPostType : Returns 1 if it is a restricted (temporary) speedpost
        /// </summary>
        public int SpeedPostType()
        {
            SignalHead sigHead = SignalHeads.First();

            if (Type == SignalObjectType.SpeedPost && sigHead.CurrentSpeedInfo != null)
            {
                return sigHead.CurrentSpeedInfo.speed_noSpeedReductionOrIsTempSpeedReduction;
            }

            return 0;
        }

        /// <summary>
        /// this_lim_speed : Returns the lowest allowed speed (for speedpost and speed signal)
        /// </summary>
        public ObjectSpeedInfo this_lim_speed(SignalFunction function)
        {
            ObjectSpeedInfo set_speed = new ObjectSpeedInfo(9E9f, 9E9f, false, false, 0, false);

            foreach (SignalHead sigHead in SignalHeads.Where(sigHead => sigHead.Function == function))
            {
                ObjectSpeedInfo this_speed = sigHead.CurrentSpeedInfo;

                if (this_speed != null && !this_speed.speed_isWarning)
                {
                    if (this_speed.speed_pass > 0 && this_speed.speed_pass < set_speed.speed_pass)
                    {
                        set_speed.speed_pass = this_speed.speed_pass;
                        set_speed.speed_flag = 0;
                        set_speed.speed_reset = 0;
                        if (Type != SignalObjectType.Signal) set_speed.speed_noSpeedReductionOrIsTempSpeedReduction = this_speed.speed_noSpeedReductionOrIsTempSpeedReduction;
                    }

                    if (this_speed.speed_freight > 0 && this_speed.speed_freight < set_speed.speed_freight)
                    {
                        set_speed.speed_freight = this_speed.speed_freight;
                        set_speed.speed_flag = 0;
                        set_speed.speed_reset = 0;
                        if (Type != SignalObjectType.Signal) set_speed.speed_noSpeedReductionOrIsTempSpeedReduction = this_speed.speed_noSpeedReductionOrIsTempSpeedReduction;
                    }
                }
            }

            if (set_speed.speed_pass > 1E9f)
                set_speed.speed_pass = -1;
            if (set_speed.speed_freight > 1E9f)
                set_speed.speed_freight = -1;

            return set_speed;
        }

        /// <summary>
        /// store_lvar : store local variable
        /// </summary>
        public void store_lvar(int index, int value)
        {
            if (localStorage.ContainsKey(index))
            {
                localStorage.Remove(index);
            }
            localStorage.Add(index, value);
        }


        /// <summary>
        /// this_sig_lvar : retrieve variable from this signal
        /// </summary>
        public int this_sig_lvar(int index)
        {
            if (localStorage.ContainsKey(index))
            {
                return localStorage[index];
            }
            return 0;
        }


        /// <summary>
        /// next_sig_lvar : retrieve variable from next signal
        /// </summary>
        public int next_sig_lvar(SignalFunction function, int index)
        {
            int nextSignal = sigfound[function];
            if (nextSignal < 0)
            {
                nextSignal = SONextSignal(function);
                sigfound[function] = nextSignal;
            }
            if (nextSignal >= 0)
            {
                SignalObject nextSignalObject = signalObjects[nextSignal];
                if (nextSignalObject.localStorage.ContainsKey(index))
                {
                    return nextSignalObject.localStorage[index];
                }
            }

            return 0;
        }


        /// <summary>
        /// next_sig_hasnormalsubtype : check if next signal has normal head with required subtype
        /// </summary>
        public int next_sig_hasnormalsubtype(int reqSubtype)
        {
            int nextSignal = sigfound[SignalFunction.NORMAL];
            if (nextSignal < 0)
            {
                nextSignal = SONextSignal(SignalFunction.NORMAL);
                sigfound[SignalFunction.NORMAL] = nextSignal;
            }
            if (nextSignal >= 0)
            {
                SignalObject nextSignalObject = signalObjects[nextSignal];
                return nextSignalObject.this_sig_hasnormalsubtype(reqSubtype);
            }

            return 0;
        }


        /// <summary>
        /// this_sig_hasnormalsubtype : check if this signal has normal head with required subtype
        /// </summary>
        public int this_sig_hasnormalsubtype(int reqSubtype)
        {
            return SignalHeads.Exists(x =>
                x.Function == SignalFunction.NORMAL && x.ORTSNormalSubtypeIndex == reqSubtype) ? 1 : 0;
        }


        /// <summary>
        /// switchstand : link signal with next switch and set aspect according to switch state
        /// </summary>
        public int switchstand(int aspect1, int aspect2, string dumpfile)
        {
            // if switch index not yet set, find first switch in path
            if (!nextSwitchIndex.HasValue)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[TCReference];
                int sectionDirection = TCDirection;

                bool switchFound = false;

                while (!switchFound)
                {
                    int pinIndex = sectionDirection;

                    if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                    {
                        if (thisSection.Pins[pinIndex, 1].Link >= 0) // facing point
                        {
                            switchFound = true;
                            nextSwitchIndex = thisSection.Index;
                            if (thisSection.LinkedSignals == null)
                            {
                                thisSection.LinkedSignals = new List<int>();
                                thisSection.LinkedSignals.Add(thisRef);
                            }
                            else if (!thisSection.LinkedSignals.Contains(thisRef))
                            {
                                thisSection.LinkedSignals.Add(thisRef);
                            }
                        }

                    }

                    sectionDirection = thisSection.Pins[pinIndex, 0].Direction;

                    if (thisSection.CircuitType != TrackCircuitSection.TrackCircuitType.EndOfTrack && thisSection.Pins[pinIndex, 0].Link >= 0)
                    {
                        thisSection = signalRef.TrackCircuitList[thisSection.Pins[pinIndex, 0].Link];
                    }
                    else
                    {
                        break;
                    }
                }

                if (!switchFound)
                {
                    if (dumpfile.Length > 1)
                    {
                        File.AppendAllText(dumpfile, "SWITCHSTAND : no switch found /n");
                    }
                    nextSwitchIndex = -1;
                }
            }

            if (nextSwitchIndex >= 0)
            {
                TrackCircuitSection switchSection = signalRef.TrackCircuitList[nextSwitchIndex.Value];
                if (dumpfile.Length > 1)
                {
                    File.AppendAllText(dumpfile,
                        String.Format("SWITCHSTAND : switch found : {0}, switch state {1} \n", switchSection.Index, switchSection.JunctionLastRoute));
                }

                return switchSection.JunctionLastRoute == 0 ? aspect1 : aspect2;
            }

            return aspect1;
        }


        /// <summary>
        /// trainhasrequeststop : link signal with platform and set state according to request stop pickup requirements
        /// </summary>
        public int trainRequestStop(int aspect1, int aspect2, string dumpfile)
        {
            int aspect = 0;

            // set platform link if not yet set
            if (!platformRef.HasValue)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[TCReference];
                foreach (int pfIndex in thisSection.PlatformIndex)
                {
                    PlatformDetails thisPlatform = signalRef.PlatformDetailsList[pfIndex];
                    if (thisPlatform.TCOffset[0, TCDirection] < TCOffset && TCOffset < thisPlatform.TCOffset[1, TCDirection])
                    {
                        platformRef = pfIndex;
                        continue;
                    }
                    else if (thisPlatform.TCOffset[1, TCDirection] < TCOffset && TCOffset < thisPlatform.TCOffset[0, TCDirection])
                    {
                        platformRef = pfIndex;
                        continue;
                    }
                }
            }

            // if enabled, check if related station is next station for train

            if (enabled && platformRef.HasValue)
            {
                if (enabledTrain.Train.StationStops != null && enabledTrain.Train.StationStops.Count > 0)
                {
                    if (enabledTrain.Train.StationStops[0].PlatformItem.Name == signalRef.PlatformDetailsList[platformRef.Value].Name)
                    {
                        if (enabledTrain.Train.StationStops[0].ReqStopDetails != null)
                        {
                            foreach (var sighead in SignalHeads)
                            {
                                if (sighead.ReqStopVisDistance.HasValue)
                                {
                                    enabledTrain.Train.StationStops[0].ReqStopDetails.visDistance = sighead.ReqStopVisDistance.Value;
                                }
                                if (sighead.ReqStopAnnDistance.HasValue)
                                {
                                    enabledTrain.Train.StationStops[0].ReqStopDetails.annDistance = sighead.ReqStopAnnDistance.Value;
                                }
                            }

                            if (enabledTrain.Train.StationStops[0].ReqStopDetails.pickupSet)
                            {
                                aspect = 1;
                            }
                        }
                    }
                }
            }

            return aspect;
        }

        /// <summary>
        /// route_set : check if required route is set
        /// </summary>
        public bool route_set(int req_mainnode, uint req_jnnode)
        {
            bool routeset = false;
            bool retry = false;

            // if signal is enabled for a train, check if required section is in train route path

            if (enabledTrain != null && !MPManager.IsMultiPlayer())
            {
                Train.TCSubpathRoute RoutePart = enabledTrain.Train.ValidRoute[enabledTrain.TrainRouteDirectionIndex];

                TrackNode thisNode = signalRef.trackDB.TrackNodes[req_mainnode];
                if (RoutePart != null)
                {
                    for (int iSection = 0; iSection <= thisNode.TCCrossReference.Count - 1 && !routeset; iSection++)
                    {
                        int sectionIndex = thisNode.TCCrossReference[iSection].Index;

                        for (int iElement = 0; iElement < RoutePart.Count && !routeset; iElement++)
                        {
                            routeset = sectionIndex == RoutePart[iElement].TCSectionIndex && signalRef.TrackCircuitList[sectionIndex].CircuitType == TrackCircuitSection.TrackCircuitType.Normal;
                        }
                    }
                }

                // if not found in trainroute, try signalroute

                if (!routeset && signalRoute != null)
                {
                    for (int iElement = 0; iElement <= signalRoute.Count - 1 && !routeset; iElement++)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[signalRoute[iElement].TCSectionIndex];
                        routeset = thisSection.OriginalIndex == req_mainnode && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal;
                    }
                }
                retry = !routeset;
            }


            // not enabled, follow set route but only if not normal signal (normal signal will not clear if not enabled)
            // also, for normal enabled signals - try and follow pins (required node may be beyond present route)

            if (retry || !isSignalNormal() || MPManager.IsMultiPlayer())
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[TCReference];
                int curDirection = TCDirection;
                int newDirection = 0;
                int sectionIndex = -1;
                bool passedTrackJn = false;

                passedSections.Clear();
                passedSections.Add(thisSection.Index);

                routeset = req_mainnode == thisSection.OriginalIndex;
                while (!routeset && thisSection != null)
                {
                    if (thisSection.ActivePins[curDirection, 0].Link >= 0)
                    {
                        newDirection = thisSection.ActivePins[curDirection, 0].Direction;
                        sectionIndex = thisSection.ActivePins[curDirection, 0].Link;
                    }
                    else
                    {
                        newDirection = thisSection.ActivePins[curDirection, 1].Direction;
                        sectionIndex = thisSection.ActivePins[curDirection, 1].Link;
                    }

                    // if Junction, if active pins not set use selected route
                    if (sectionIndex < 0 && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction)
                    {
                        // check if this is required junction
                        if (Convert.ToUInt32(thisSection.Index) == req_jnnode)
                        {
                            passedTrackJn = true;
                        }
                        // break if passed required junction
                        else if (passedTrackJn)
                        {
                            break;
                        }

                        if (thisSection.ActivePins[1, 0].Link == -1 && thisSection.ActivePins[1, 1].Link == -1)
                        {
                            int selectedDirection = signalRef.trackDB.TrackNodes[thisSection.OriginalIndex].TrJunctionNode.SelectedRoute;
                            newDirection = thisSection.Pins[1, selectedDirection].Direction;
                            sectionIndex = thisSection.Pins[1, selectedDirection].Link;
                        }
                    }

                    // if NORMAL, if active pins not set use default pins
                    if (sectionIndex < 0 && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal)
                    {
                        newDirection = thisSection.Pins[curDirection, 0].Direction;
                        sectionIndex = thisSection.Pins[curDirection, 0].Link;
                    }

                    // check for loop
                    if (passedSections.Contains(sectionIndex))
                    {
                        thisSection = null;  // route is looped - exit
                    }

                    // next section
                    else if (sectionIndex >= 0)
                    {
                        passedSections.Add(sectionIndex);
                        thisSection = signalRef.TrackCircuitList[sectionIndex];
                        curDirection = newDirection;
                        routeset = req_mainnode == thisSection.OriginalIndex && thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Normal;
                    }

                    // no next section
                    else
                    {
                        thisSection = null;
                    }
                }
            }

            return routeset;
        }

        /// <summary>
        /// Find next signal of specified type along set sections - not for NORMAL signals
        /// </summary>
        public int SONextSignal(SignalFunction function)
        {
            int thisTC = TCReference;
            int direction = TCDirection;
            int signalFound = -1;
            TrackCircuitSection thisSection = null;
            bool sectionSet = false;

            // if searching for SPEED signal : check if enabled and use train to find next speedpost
            if (function == SignalFunction.SPEED)
            {
                if (enabledTrain != null)
                {
                    signalFound = SONextSignalSpeed(TCReference);
                }
                else
                {
                    return -1;
                }
            }
            // for normal signals
            else if (function == SignalFunction.NORMAL)
            {
                if (isSignalNormal())        // if this signal is normal : cannot be done using this route (set through sigfound variable)
                    return -1;
                signalFound = SONextSignalNormal(TCReference);   // other types of signals (sigfound not used)
            }
            // for other signals : move to next TC (signal would have been default if within same section)
            else
            {
                thisSection = signalRef.TrackCircuitList[thisTC];

                if (!isSignalNormal())
                {
                    TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction][function];
                    for (int i = 0; i < thisList.TrackCircuitItem.Count; i++)
                    {
                        if (thisList.TrackCircuitItem[i].SignalRef.TCOffset > TCOffset)
                        {
                            signalFound = thisList.TrackCircuitItem[i].SignalRef.thisRef;
                            break;
                        }
                    }
                }

                sectionSet = enabledTrain != null && thisSection.IsSet(enabledTrain, false);

                if (sectionSet)
                {
                    int pinIndex = direction;
                    thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                    direction = thisSection.ActivePins[pinIndex, 0].Direction;
                }
            }

            // loop through valid sections
            while (sectionSet && thisTC > 0 && signalFound < 0)
            {
                thisSection = signalRef.TrackCircuitList[thisTC];

                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction ||
                    thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
                {
                    if (!JunctionsPassed.Contains(thisTC))
                        JunctionsPassed.Add(thisTC);  // set reference to junction section
                    if (!thisSection.SignalsPassingRoutes.Contains(thisRef))
                        thisSection.SignalsPassingRoutes.Add(thisRef);
                }

                // check if required type of signal is along this section
                TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction][function];
                if (thisList.TrackCircuitItem.Count > 0)
                {
                    signalFound = thisList.TrackCircuitItem[0].SignalRef.thisRef;
                }

                // get next section if active link is set
                if (signalFound < 0)
                {
                    int pinIndex = direction;
                    sectionSet = thisSection.IsSet(enabledTrain, false);
                    if (sectionSet)
                    {
                        thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                        direction = thisSection.ActivePins[pinIndex, 0].Direction;
                        if (thisTC == -1)
                        {
                            thisTC = thisSection.ActivePins[pinIndex, 1].Link;
                            direction = thisSection.ActivePins[pinIndex, 1].Direction;
                        }
                    }
                }
            }

            // if signal not found following switches use signal route
            if (signalFound < 0 && signalRoute != null && signalRoute.Count > 0)
            {
                for (int iSection = 0; iSection <= (signalRoute.Count - 1) && signalFound < 0; iSection++)
                {
                    thisSection = signalRef.TrackCircuitList[signalRoute[iSection].TCSectionIndex];
                    direction = signalRoute[iSection].Direction;
                    TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction][function];
                    if (thisList.TrackCircuitItem.Count > 0)
                    {
                        signalFound = thisList.TrackCircuitItem[0].SignalRef.thisRef;
                    }
                }
            }

            // if signal not found, use route from requesting normal signal
            if (signalFound < 0 && reqNormalSignal >= 0)
            {
                SignalObject refSignal = signalRef.SignalObjects[reqNormalSignal];
                if (refSignal.signalRoute != null && refSignal.signalRoute.Count > 0)
                {
                    int nextSectionIndex = refSignal.signalRoute.GetRouteIndex(TCReference, 0);

                    if (nextSectionIndex >= 0)
                    {
                        for (int iSection = nextSectionIndex + 1; iSection <= (refSignal.signalRoute.Count - 1) && signalFound < 0; iSection++)
                        {
                            thisSection = signalRef.TrackCircuitList[refSignal.signalRoute[iSection].TCSectionIndex];
                            direction = refSignal.signalRoute[iSection].Direction;
                            TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction][function];
                            if (thisList.TrackCircuitItem.Count > 0)
                            {
                                signalFound = thisList.TrackCircuitItem[0].SignalRef.thisRef;
                            }
                        }
                    }
                }
            }

            return signalFound;
        }

        /// <summary>
        /// Find next signal of specified type along set sections - for SPEED signals only
        /// </summary>
        private int SONextSignalSpeed(int thisTC)
        {
            int routeListIndex = enabledTrain.Train.ValidRoute[0].GetRouteIndex(TCReference, enabledTrain.Train.PresentPosition[0].RouteListIndex);

            // signal not in train's route
            if (routeListIndex < 0)
            {
                return -1;
            }

            // find next speed object
            TrackCircuitSignalItem foundItem = signalRef.Find_Next_Object_InRoute(enabledTrain.Train.ValidRoute[0], routeListIndex, TCOffset, -1, SignalFunction.SPEED, enabledTrain);
            if (foundItem.SignalState == ObjectItemInfo.ObjectItemFindState.Object)
            {
                return foundItem.SignalRef.thisRef;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Find next signal of specified type along set sections - NORMAL signals ONLY
        /// </summary>
        private int SONextSignalNormal(int thisTC)
        {
            int direction = TCDirection;
            int signalFound = -1;
            TrackCircuitSection thisSection = null;

            int pinIndex = direction;

            if (thisTC < 0)
            {
                thisTC = TCReference;
                thisSection = signalRef.TrackCircuitList[thisTC];
                pinIndex = direction;
                thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                direction = thisSection.ActivePins[pinIndex, 0].Direction;
            }

            // loop through valid sections

            while (thisTC > 0 && signalFound < 0)
            {
                thisSection = signalRef.TrackCircuitList[thisTC];

                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction ||
                    thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
                {
                    if (!JunctionsPassed.Contains(thisTC))
                        JunctionsPassed.Add(thisTC);  // set reference to junction section
                    if (!thisSection.SignalsPassingRoutes.Contains(thisRef))
                        thisSection.SignalsPassingRoutes.Add(thisRef);
                }

                // check if normal signal is along this section

                if (thisSection.EndSignals[direction] != null)
                {
                    signalFound = thisSection.EndSignals[direction].thisRef;
                }

                // get next section if active link is set

                if (signalFound < 0)
                {
                    pinIndex = direction;
                    thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                    direction = thisSection.ActivePins[pinIndex, 0].Direction;
                    if (thisTC == -1)
                    {
                        thisTC = thisSection.ActivePins[pinIndex, 1].Link;
                        direction = thisSection.ActivePins[pinIndex, 1].Direction;
                    }

                    // if no active link but signal has route allocated, use train route to find next section

                    if (thisTC == -1 && signalRoute != null)
                    {
                        int thisIndex = signalRoute.GetRouteIndex(thisSection.Index, 0);
                        if (thisIndex >= 0 && thisIndex <= signalRoute.Count - 2)
                        {
                            thisTC = signalRoute[thisIndex + 1].TCSectionIndex;
                            direction = signalRoute[thisIndex + 1].Direction;
                        }
                    }
                }
            }

            return signalFound;
        }

        /// <summary>
        /// Find next signal in opp direction
        /// </summary>
        public int SONextSignalOpp(SignalFunction function)
        {
            int thisTC = TCReference;
            int direction = TCDirection == 0 ? 1 : 0;    // reverse direction
            int signalFound = -1;

            TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisTC];
            bool sectionSet = enabledTrain == null ? false : thisSection.IsSet(enabledTrain, false);

            // loop through valid sections
            while (sectionSet && thisTC > 0 && signalFound < 0)
            {
                thisSection = signalRef.TrackCircuitList[thisTC];

                if (thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Junction ||
                    thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.Crossover)
                {
                    if (!JunctionsPassed.Contains(thisTC))
                        JunctionsPassed.Add(thisTC);  // set reference to junction section
                    if (!thisSection.SignalsPassingRoutes.Contains(thisRef))
                        thisSection.SignalsPassingRoutes.Add(thisRef);
                }

                // check if required type of signal is along this section
                if (function == SignalFunction.NORMAL)
                {
                    signalFound = thisSection.EndSignals[direction] != null ? thisSection.EndSignals[direction].thisRef : -1;
                }
                else
                {
                    TrackCircuitSignalList thisList = thisSection.CircuitItems.TrackCircuitSignals[direction][function];
                    if (thisList.TrackCircuitItem.Count > 0)
                    {
                        signalFound = thisList.TrackCircuitItem[0].SignalRef.thisRef;
                    }
                }

                // get next section if active link is set
                if (signalFound < 0)
                {
                    int pinIndex = direction;
                    sectionSet = thisSection.IsSet(enabledTrain, false);
                    if (sectionSet)
                    {
                        thisTC = thisSection.ActivePins[pinIndex, 0].Link;
                        direction = thisSection.ActivePins[pinIndex, 0].Direction;
                        if (thisTC == -1)
                        {
                            thisTC = thisSection.ActivePins[pinIndex, 1].Link;
                            direction = thisSection.ActivePins[pinIndex, 1].Direction;
                        }
                    }
                }
            }

            return signalFound;
        }

        /// <summary>
        /// Perform route check and state update
        /// </summary>
        public void Update()
        {
            // perform route update for normal signals if enabled
            if (isSignalNormal())
            {
                // if in hold, set to most restrictive for each head
                if (holdState != HoldState.None)
                {
                    foreach (SignalHead sigHead in SignalHeads)
                    {
                        switch (holdState)
                        {
                            case HoldState.ManualLock:
                            case HoldState.StationStop:
                                sigHead.RequestMostRestrictiveAspect();
                                break;

                            case HoldState.ManualApproach:
                                sigHead.RequestApproachAspect();
                                break;

                            case HoldState.ManualPass:
                                sigHead.RequestLeastRestrictiveAspect();
                                break;
                        }
                    }
                }

                // if enabled - perform full update and propagate if not yet done
                if (enabledTrain != null)
                {
                    // if internal state is not reserved (route fully claimed), perform route check
                    if (internalBlockState != InternalBlockstate.Reserved)
                    {
                        checkRouteState(isPropagated, signalRoute, enabledTrain);
                    }

                    // propagate request
                    if (!isPropagated)
                    {
                        propagateRequest();
                    }

                    StateUpdate();

                    // propagate request if not yet done
                    if (!propagated && enabledTrain != null)
                    {
                        propagateRequest();
                    }
                }
                // fixed route - check route and update
                else if (hasFixedRoute)
                {
                    // if internal state is not reserved (route fully claimed), perform route check
                    if (internalBlockState != InternalBlockstate.Reserved)
                    {
                        checkRouteState(true, fixedRoute, null);
                    }

                    StateUpdate();
                }
                // no route - perform update only
                else
                {
                    StateUpdate();
                }
            }
            // check blockstate for other signals
            else
            {
                getBlockState_notRouted();
                StateUpdate();
            }
        }

        /// <summary>
        /// fully reset signal as train has passed
        /// </summary>
        public void resetSignalEnabled()
        {
            // reset train information
            enabledTrain = null;
            CallOnManuallyAllowed = false;
            trainRouteDirectionIndex = 0;
            signalRoute.Clear();
            fullRoute = hasFixedRoute;
            thisTrainRouteIndex = -1;

            isPropagated = false;
            propagated = false;
            ForcePropagation = false;
            ApproachControlCleared = false;
            ApproachControlSet = false;
            ClaimLocked = false;
            ForcePropOnApproachControl = false;

            // reset block state to most restrictive

            internalBlockState = InternalBlockstate.Blocked;

            // reset next signal information to default

            foreach (SignalFunction function in signalRef.SignalFunctions.Values)
            {
                sigfound[function] = defaultNextSignal[function];
            }

            foreach (int thisSectionIndex in JunctionsPassed)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];
                thisSection.SignalsPassingRoutes.Remove(thisRef);
            }

            // reset permission //

            hasPermission = Permission.Denied;

            StateUpdate();
        }

        /// <summary>
        /// Perform the update for each head on this signal to determine state of signal.
        /// </summary>
        public void StateUpdate()
        {
            // reset approach control (must be explicitly reset as test in script may be conditional)
            ApproachControlSet = false;

            // update all normal heads first

            if (MPManager.IsMultiPlayer())
            {
                if (MPManager.IsClient()) return; //client won't handle signal update

                //if there were hold manually, will not update
                if (holdState == HoldState.ManualApproach || holdState == HoldState.ManualLock || holdState == HoldState.ManualPass) return;
            }

            for (int i = 0; i < SignalHeads.Count; i++)
            {
                if (SignalHeads[i].Function == SignalFunction.NORMAL)
                    SignalHeads[i].Update();
            }

            // next, update all other heads
            for (int i = 0; i < SignalHeads.Count; i++)
            {
                if (SignalHeads[i].Function != SignalFunction.NORMAL)
                    SignalHeads[i].Update();
            }

        }

        /// <summary>
        /// Returns the distance from the TDBtraveller to this signal. 
        /// </summary>
        public float DistanceTo(Traveller tdbTraveller)
        {
            int trItem = trackNodes[trackNode].TrVectorNode.TrItemRefs[trRefIndex];
            return tdbTraveller.DistanceTo(trItems[trItem].TileX, trItems[trItem].TileZ, trItems[trItem].X, trItems[trItem].Y, trItems[trItem].Z);
        }

        /// <summary>
        /// Returns the distance from this object to the next object
        /// </summary>
        public float ObjectDistance(SignalObject nextObject)
        {
            int nextTrItem = trackNodes[nextObject.trackNode].TrVectorNode.TrItemRefs[nextObject.trRefIndex];
            return this.tdbtraveller.DistanceTo(
                                    trItems[nextTrItem].TileX, trItems[nextTrItem].TileZ,
                                    trItems[nextTrItem].X, trItems[nextTrItem].Y, trItems[nextTrItem].Z);
        }

        /// <summary>
        /// Check whether signal head is for this signal.
        /// </summary>
        public bool isSignalHead(SignalItem signalItem)
        {
            // Tritem for this signal
            SignalItem thisSignalItem = (SignalItem)trItems[this.trItem];
            // Same Tile
            if (signalItem.TileX == thisSignalItem.TileX && signalItem.TileZ == thisSignalItem.TileZ)
            {
                // Same position
                if ((Math.Abs(signalItem.X - thisSignalItem.X) < 0.01) &&
                    (Math.Abs(signalItem.Y - thisSignalItem.Y) < 0.01) &&
                    (Math.Abs(signalItem.Z - thisSignalItem.Z) < 0.01))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Adds a head to this signal (for signam).
        /// </summary>
        public void AddHead(int trItem, int TDBRef, SignalItem sigItem)
        {
            // create SignalHead
            SignalHead head = new SignalHead(this, trItem, TDBRef, sigItem);

            // set junction link
            if (head.TrackJunctionNode != 0)
            {
                if (head.JunctionPath == 0)
                {
                    head.JunctionMainNode =
                       trackNodes[head.TrackJunctionNode].TrPins[trackNodes[head.TrackJunctionNode].Inpins].Link;
                }
                else
                {
                    head.JunctionMainNode =
                       trackNodes[head.TrackJunctionNode].TrPins[trackNodes[head.TrackJunctionNode].Inpins + 1].Link;
                }
            }
            SignalHeads.Add(head);

        }

        /// <summary>
        /// Adds a head to this signal (for speedpost).
        /// </summary>
        public void AddHead(int trItem, int TDBRef, SpeedPostItem speedItem)
        {
            // create SignalHead
            SignalHead head = new SignalHead(this, trItem, TDBRef, speedItem);
            SignalHeads.Add(head);

        }

        /// <summary>
        /// Sets the signal type from the sigcfg file for each signal head.
        /// </summary>
        public void SetSignalType(SignalConfigurationFile sigCFG)
        {
            foreach (SignalHead sigHead in SignalHeads)
            {
                sigHead.SetSignalType(trItems, sigCFG);
            }

            if (Type == SignalObjectType.Signal
                && !SignalHeads.Exists(x => x.Function.MstsFunction != MstsSignalFunction.SPEED))
            {
                Type = SignalObjectType.SpeedSignal;
            }
        }

        public void Initialize()
        {
            foreach (SignalHead head in SignalHeads)
            {
                head.Initialize();
            }
        }

        /// <summary>
        /// Gets the display aspect for the track monitor.
        /// </summary>
        public TrackMonitorSignalAspect TranslateTMAspect(MstsSignalAspect SigState)
        {
            switch (SigState)
            {
                case MstsSignalAspect.STOP:
                    if (hasPermission == Permission.Granted)
                        return TrackMonitorSignalAspect.Permission;
                    else
                        return TrackMonitorSignalAspect.Stop;
                case MstsSignalAspect.STOP_AND_PROCEED:
                    return TrackMonitorSignalAspect.StopAndProceed;
                case MstsSignalAspect.RESTRICTING:
                    return TrackMonitorSignalAspect.Restricted;
                case MstsSignalAspect.APPROACH_1:
                    return TrackMonitorSignalAspect.Approach_1;
                case MstsSignalAspect.APPROACH_2:
                    return TrackMonitorSignalAspect.Approach_2;
                case MstsSignalAspect.APPROACH_3:
                    return TrackMonitorSignalAspect.Approach_3;
                case MstsSignalAspect.CLEAR_1:
                    return TrackMonitorSignalAspect.Clear_1;
                case MstsSignalAspect.CLEAR_2:
                    return TrackMonitorSignalAspect.Clear_2;
                default:
                    return TrackMonitorSignalAspect.None;
            }
        }

        /// <summary>
        /// request to clear signal in explorer mode
        /// </summary>
        public Train.TCSubpathRoute requestClearSignalExplorer(Train.TCSubpathRoute thisRoute,
            Train.TrainRouted thisTrain, bool propagated, int signalNumClearAhead)
        {
            // build output route from input route
            Train.TCSubpathRoute newRoute = new Train.TCSubpathRoute(thisRoute);

            // don't clear if enabled for another train
            if (enabledTrain != null && enabledTrain != thisTrain)
                return newRoute;

            // if signal has fixed route, use that else build route
            if (fixedRoute != null && fixedRoute.Count > 0)
            {
                signalRoute = new Train.TCSubpathRoute(fixedRoute);
            }

            // build route from signal, upto next signal or max distance, take into account manual switch settings
            else
            {
                List<int> nextRoute = signalRef.ScanRoute(thisTrain.Train, TCNextTC, 0.0f, TCNextDirection, true, -1, true, true, true, false,
                true, false, false, false, false, thisTrain.Train.IsFreight);

                signalRoute = new Train.TCSubpathRoute();

                foreach (int sectionIndex in nextRoute)
                {
                    Train.TCRouteElement thisElement = new Train.TCRouteElement(Math.Abs(sectionIndex), sectionIndex >= 0 ? 0 : 1);
                    signalRoute.Add(thisElement);
                }
            }

            // set full route if route ends with signal
            TrackCircuitSection lastSection = signalRef.TrackCircuitList[signalRoute[signalRoute.Count - 1].TCSectionIndex];
            int lastDirection = signalRoute[signalRoute.Count - 1].Direction;

            if (lastSection.EndSignals[lastDirection] != null)
            {
                fullRoute = true;
                sigfound[SignalFunction.NORMAL] = lastSection.EndSignals[lastDirection].thisRef;
            }

            // try and clear signal

            enabledTrain = thisTrain;
            checkRouteState(propagated, signalRoute, thisTrain);

            // extend route if block is clear or permission is granted, even if signal is not cleared (signal state may depend on next signal)
            bool extendRoute = false;
            if (this_sig_lr(MstsSignalFunction.NORMAL) > MstsSignalAspect.STOP) extendRoute = true;
            if (internalBlockState <= InternalBlockstate.Reservable) extendRoute = true;

            // if signal is cleared or permission is granted, extend route with signal route

            if (extendRoute || hasPermission == Permission.Granted)
            {
                foreach (Train.TCRouteElement thisElement in signalRoute)
                {
                    newRoute.Add(thisElement);
                }
            }

            // if signal is cleared, propagate request if required
            if (extendRoute && fullRoute)
            {
                isPropagated = propagated;
                int ReqNumClearAhead = GetReqNumClearAheadExplorer(isPropagated, signalNumClearAhead);
                if (ReqNumClearAhead > 0)
                {
                    int nextSignalIndex = sigfound[SignalFunction.NORMAL];
                    if (nextSignalIndex >= 0)
                    {
                        SignalObject nextSignal = signalObjects[nextSignalIndex];
                        newRoute = nextSignal.requestClearSignalExplorer(newRoute, thisTrain, true, ReqNumClearAhead);
                    }
                }
            }

            return newRoute;
        }

        /// <summary>
        /// number of remaining signals to clear
        /// </summary>
        public int GetReqNumClearAheadExplorer(bool isPropagated, int signalNumClearAhead)
        {
            if (SignalNumClearAhead_MSTS > -2)
                return propagated ? signalNumClearAhead - SignalNumNormalHeads : SignalNumClearAhead_MSTS - SignalNumNormalHeads;
            else if (SignalNumClearAheadActive == -1)
                return propagated ? signalNumClearAhead : 1;
            else if (SignalNumClearAheadActive == 0)
                return 0;
            else
                return isPropagated ? signalNumClearAhead - 1 : SignalNumClearAheadActive - 1;
        }

        /// <summary>
        /// request to clear signal
        /// </summary>
        public bool requestClearSignal(Train.TCSubpathRoute RoutePart, Train.TrainRouted thisTrain,
                        int clearNextSignals, bool requestIsPropagated, SignalObject lastSignal)
        {

#if DEBUG_REPORTS
			File.AppendAllText(@"C:\temp\printproc.txt",
				String.Format("Request for clear signal from train {0} at section {1} for signal {2}\n",
				thisTrain.Train.Number,
				thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex,
				thisRef));
#endif
            if (thisTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                    String.Format("Request for clear signal from train {0} at section {1} for signal {2}\n",
                    thisTrain.Train.Number,
                    thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex,
                    thisRef));
            }

            // set general variables
            int foundFirstSection = -1;
            int foundLastSection = -1;
            SignalObject nextSignal = null;

            isPropagated = requestIsPropagated;
            propagated = false;   // always pass on request

            // check if signal not yet enabled - if it is, give warning and quit

            // check if signal not yet enabled - if it is, give warning, reset signal and set both trains to node control, and quit

            if (enabledTrain != null && enabledTrain != thisTrain)
            {
                Trace.TraceWarning("Request to clear signal {0} from train {1}, signal already enabled for train {2}",
                                       thisRef, thisTrain.Train.Name, enabledTrain.Train.Name);
                Train.TrainRouted otherTrain = enabledTrain;
                ResetSignal(true);
                int routeListIndex = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].RouteListIndex;
                signalRef.BreakDownRouteList(thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex], routeListIndex, thisTrain);
                routeListIndex = otherTrain.Train.PresentPosition[otherTrain.TrainRouteDirectionIndex].RouteListIndex;
                signalRef.BreakDownRouteList(otherTrain.Train.ValidRoute[otherTrain.TrainRouteDirectionIndex], routeListIndex, otherTrain);

                thisTrain.Train.SwitchToNodeControl(thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].TCSectionIndex);
                if (otherTrain.Train.ControlMode != Train.TRAIN_CONTROL.EXPLORER && !otherTrain.Train.IsPathless) otherTrain.Train.SwitchToNodeControl(otherTrain.Train.PresentPosition[otherTrain.TrainRouteDirectionIndex].TCSectionIndex);
                return false;
            }
            if (thisTrain.Train.TCRoute != null && HasLockForTrain(thisTrain.Train.Number, thisTrain.Train.TCRoute.activeSubpath))
            {
                return false;
            }
            if (enabledTrain != thisTrain) // new allocation - reset next signals
            {
                foreach (SignalFunction function in signalRef.SignalFunctions.Values)
                {
                    sigfound[function] = defaultNextSignal[function];
                }
            }
            enabledTrain = thisTrain;

            // find section in route part which follows signal

            signalRoute.Clear();

            int firstIndex = -1;
            if (lastSignal != null)
            {
                firstIndex = lastSignal.thisTrainRouteIndex;
            }
            if (firstIndex < 0)
            {
                firstIndex = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex].RouteListIndex;
            }

            if (firstIndex >= 0)
            {
                for (int iNode = firstIndex;
                         iNode < RoutePart.Count && foundFirstSection < 0;
                         iNode++)
                {
                    Train.TCRouteElement thisElement = RoutePart[iNode];
                    if (thisElement.TCSectionIndex == TCNextTC)
                    {
                        foundFirstSection = iNode;
                        thisTrainRouteIndex = iNode;
                    }
                }
            }

            if (foundFirstSection < 0)
            {
                enabledTrain = null;

                // if signal on holding list, set hold state
                if (thisTrain.Train.HoldingSignals.Contains(thisRef) && holdState == HoldState.None)
                {
                    holdState = HoldState.StationStop;
                }
                return false;
            }

            // copy sections upto next normal signal
            // check for loop

            sectionsInRoute.Clear();

            for (int iNode = foundFirstSection; iNode < RoutePart.Count && foundLastSection < 0; iNode++)
            {
                Train.TCRouteElement thisElement = RoutePart[iNode];
                if (sectionsInRoute.Contains(thisElement.TCSectionIndex))
                {
                    foundLastSection = iNode;  // loop
                }
                else
                {
                    signalRoute.Add(thisElement);
                    sectionsInRoute.Add(thisElement.TCSectionIndex);

                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                    // exit if section is pool access section (signal will clear on new route on next try)
                    // reset train details to force new signal clear request
                    // check also creates new full train route
                    // applies to timetable mode only
                    if (thisTrain.Train.CheckPoolAccess(thisSection.Index))
                    {
                        enabledTrain = null;
                        signalRoute.Clear();

                        if (thisTrain.Train.CheckTrain)
                        {
                            File.AppendAllText(@"C:\temp\checktrain.txt",
                                String.Format("Reset signal for pool access : {0} \n", thisRef));
                        }

                        return false;
                    }

                    // check if section has end signal - if so is last section
                    if (thisSection.EndSignals[thisElement.Direction] != null)
                    {
                        foundLastSection = iNode;
                        nextSignal = thisSection.EndSignals[thisElement.Direction];
                    }
                }
            }

            // check if signal has route, is enabled, request is by enabled train and train is not occupying sections in signal route

            if (enabledTrain != null && enabledTrain == thisTrain && signalRoute != null && signalRoute.Count > 0)
            {
                foreach (Train.TCRouteElement routeElement in signalRoute)
                {
                    TrackCircuitSection routeSection = signalRef.TrackCircuitList[routeElement.TCSectionIndex];
                    if (routeSection.CircuitState.ThisTrainOccupying(thisTrain))
                    {
                        return false;  // train has passed signal - clear request is invalid
                    }
                }
            }

            // check if end of track reached

            Train.TCRouteElement lastSignalElement = signalRoute[signalRoute.Count - 1];
            TrackCircuitSection lastSignalSection = signalRef.TrackCircuitList[lastSignalElement.TCSectionIndex];

            fullRoute = true;

            // if end of signal route is not a signal or end-of-track it is not a full route

            if (nextSignal == null && lastSignalSection.CircuitType != TrackCircuitSection.TrackCircuitType.EndOfTrack)
            {
                fullRoute = false;
            }

            // if next signal is found and relevant, set reference

            if (nextSignal != null)
            {
                sigfound[SignalFunction.NORMAL] = nextSignal.thisRef;
            }
            else
            {
                sigfound[SignalFunction.NORMAL] = -1;
            }

            // set number of signals to clear ahead

            if (SignalNumClearAhead_MSTS > -2)
            {
                ReqNumClearAhead = clearNextSignals > 0 ?
                    clearNextSignals - SignalNumNormalHeads : SignalNumClearAhead_MSTS - SignalNumNormalHeads;
            }
            else
            {
                if (SignalNumClearAheadActive == -1)
                {
                    ReqNumClearAhead = clearNextSignals > 0 ? clearNextSignals : 1;
                }
                else if (SignalNumClearAheadActive == 0)
                {
                    ReqNumClearAhead = 0;
                }
                else
                {
                    ReqNumClearAhead = clearNextSignals > 0 ? clearNextSignals - 1 : SignalNumClearAheadActive - 1;
                }
            }

            // perform route check

            checkRouteState(isPropagated, signalRoute, thisTrain);

            // propagate request

            if (!isPropagated && enabledTrain != null)
            {
                propagateRequest();
            }
            if (thisTrain != null && thisTrain.Train is AITrain && Math.Abs(thisTrain.Train.SpeedMpS) <= Simulator.MaxStoppedMpS)
            {
                WorldLocation location = this.tdbtraveller.WorldLocation;
                ((AITrain)thisTrain.Train).AuxActionsContain.CheckGenActions(this.GetType(), location, 0f, 0f, this.tdbtraveller.TrackNodeIndex);
            }

            return this_sig_mr(MstsSignalFunction.NORMAL) != MstsSignalAspect.STOP;
        }

        /// <summary>
        /// check and update Route State
        /// </summary>
        public void checkRouteState(bool isPropagated, Train.TCSubpathRoute thisRoute, Train.TrainRouted thisTrain, bool sound = true)
        {
            // check if signal must be hold
            bool signalHold = holdState == HoldState.ManualLock || holdState == HoldState.StationStop;
            if (enabledTrain != null && enabledTrain.Train.HoldingSignals.Contains(thisRef) && holdState < HoldState.ManualLock)
            {
                holdState = HoldState.StationStop;
                signalHold = true;
            }
            else if (holdState == HoldState.StationStop)
            {
                if (enabledTrain == null || !enabledTrain.Train.HoldingSignals.Contains(thisRef))
                {
                    holdState = HoldState.None;
                    signalHold = false;
                }
            }

            // check if signal has route, is enabled, request is by enabled train and train is not occupying sections in signal route

            if (enabledTrain != null && enabledTrain == thisTrain && signalRoute != null && signalRoute.Count > 0)
            {
                var forcedRouteElementIndex = -1;
                foreach (Train.TCRouteElement routeElement in signalRoute)
                {
                    TrackCircuitSection routeSection = signalRef.TrackCircuitList[routeElement.TCSectionIndex];
                    if (routeSection.CircuitState.ThisTrainOccupying(thisTrain))
                    {
                        return;  // train has passed signal - clear request is invalid
                    }
                    if (routeSection.CircuitState.Forced)
                    {
                        // route must be recomputed after switch moved by dispatcher
                        forcedRouteElementIndex = signalRoute.IndexOf(routeElement);
                        break;
                    }
                }
                if (forcedRouteElementIndex >= 0)
                {
                    int forcedTCSectionIndex = signalRoute[forcedRouteElementIndex].TCSectionIndex;
                    TrackCircuitSection forcedTrackSection = signalRef.TrackCircuitList[forcedTCSectionIndex];
                    int forcedRouteSectionIndex = thisTrain.Train.ValidRoute[0].GetRouteIndex(forcedTCSectionIndex, 0);
                    thisTrain.Train.ReRouteTrain(forcedRouteSectionIndex, forcedTCSectionIndex);
                    if (thisTrain.Train.TrainType == Train.TRAINTYPE.AI || thisTrain.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING)
                        (thisTrain.Train as AITrain).ResetActions(true);
                    forcedTrackSection.CircuitState.Forced = false;
                }
            }

            // test if propagate state still correct - if next signal for enabled train is this signal, it is not propagated

            if (enabledTrain != null && enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex] != null &&
                enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex].thisRef == thisRef)
            {
                isPropagated = false;
            }

            // test clearance for full route section

            if (!signalHold)
            {
                if (fullRoute)
                {
                    bool newroute = getBlockState(thisRoute, thisTrain, !sound);
                    if (newroute)
                        thisRoute = this.signalRoute;
                }

                // test clearance for sections in route only if first signal ahead of train or if clearance unto partial route is allowed

                else if (enabledTrain != null && (!isPropagated || AllowPartRoute) && thisRoute.Count > 0)
                {
                    getPartBlockState(thisRoute);
                }

                // test clearance for sections in route if signal is second signal ahead of train, first signal route is clear but first signal is still showing STOP
                // case for double-hold signals

                else if (enabledTrain != null && isPropagated)
                {
                    SignalObject firstSignal = enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex];
                    if (firstSignal != null &&
                        firstSignal.sigfound[SignalFunction.NORMAL] == thisRef &&
                        firstSignal.internalBlockState <= InternalBlockstate.Reservable &&
                        firstSignal.this_sig_lr(MstsSignalFunction.NORMAL) == MstsSignalAspect.STOP)
                    {
                        getPartBlockState(thisRoute);
                    }
                }
            }

            // else consider route blocked

            else
            {
                internalBlockState = InternalBlockstate.Blocked;
            }

            // derive signal state

            StateUpdate();
            MstsSignalAspect signalState = this_sig_lr(MstsSignalFunction.NORMAL);

            float lengthReserved = 0.0f;

            // check for permission

            if (internalBlockState == InternalBlockstate.OccupiedSameDirection && hasPermission == Permission.Requested && !isPropagated)
            {
                hasPermission = Permission.Granted;
                if (sound) signalRef.Simulator.SoundNotify = Event.PermissionGranted;
            }
            else
            {
                if (enabledTrain != null && enabledTrain.Train.ControlMode == Train.TRAIN_CONTROL.MANUAL &&
                    internalBlockState <= InternalBlockstate.OccupiedSameDirection && hasPermission == Permission.Requested)
                {
                    signalRef.Simulator.SoundNotify = Event.PermissionGranted;
                }
                else if (hasPermission == Permission.Requested)
                {
                    if (sound) signalRef.Simulator.SoundNotify = Event.PermissionDenied;
                }

                if (enabledTrain != null && enabledTrain.Train.ControlMode == Train.TRAIN_CONTROL.MANUAL && signalState == MstsSignalAspect.STOP &&
                internalBlockState <= InternalBlockstate.OccupiedSameDirection && hasPermission == Permission.Requested)
                {
                    hasPermission = Permission.Granted;
                }
                else if (hasPermission == Permission.Requested)
                {
                    hasPermission = Permission.Denied;
                }
            }

            // reserve full section if allowed, do not set reserved if signal is held on approach control

            if (enabledTrain != null)
            {
                if (internalBlockState == InternalBlockstate.Reservable && !ApproachControlSet)
                {
                    internalBlockState = InternalBlockstate.Reserved; // preset all sections are reserved

                    foreach (Train.TCRouteElement thisElement in thisRoute)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        if (thisSection.CircuitState.TrainReserved != null || thisSection.CircuitState.TrainOccupy.Count > 0)
                        {
                            if (thisSection.CircuitState.TrainReserved != thisTrain)
                            {
                                internalBlockState = InternalBlockstate.Reservable; // not all sections are reserved // 
                                break;
                            }
                        }
                        thisSection.Reserve(enabledTrain, thisRoute);
                        enabledTrain.Train.LastReservedSection[enabledTrain.TrainRouteDirectionIndex] = thisElement.TCSectionIndex;
                        lengthReserved += thisSection.Length;
                    }

                    enabledTrain.Train.ClaimState = false;
                }

                // reserve partial sections if signal clears on occupied track or permission is granted

                else if ((signalState > MstsSignalAspect.STOP || hasPermission == Permission.Granted) &&
                         internalBlockState != InternalBlockstate.Reserved && internalBlockState < InternalBlockstate.ReservedOther)
                {

                    // reserve upto available section

                    int lastSectionIndex = 0;
                    bool reservable = true;

                    for (int iSection = 0; iSection < thisRoute.Count && reservable; iSection++)
                    {
                        Train.TCRouteElement thisElement = thisRoute[iSection];
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                        if (thisSection.IsAvailable(enabledTrain))
                        {
                            if (thisSection.CircuitState.TrainReserved == null)
                            {
                                thisSection.Reserve(enabledTrain, thisRoute);
                            }
                            enabledTrain.Train.LastReservedSection[enabledTrain.TrainRouteDirectionIndex] = thisElement.TCSectionIndex;
                            lastSectionIndex = iSection;
                            lengthReserved += thisSection.Length;
                        }
                        else
                        {
                            reservable = false;
                        }
                    }

                    // set pre-reserved or reserved for all other sections

                    for (int iSection = lastSectionIndex++; iSection < thisRoute.Count && reservable; iSection++)
                    {
                        Train.TCRouteElement thisElement = thisRoute[iSection];
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];

                        if (thisSection.IsAvailable(enabledTrain) && thisSection.CircuitState.TrainReserved == null)
                        {
                            thisSection.Reserve(enabledTrain, thisRoute);
                        }
                        else if (thisSection.CircuitState.HasOtherTrainsOccupying(enabledTrain))
                        {
                            thisSection.PreReserve(enabledTrain);
                        }
                        else if (thisSection.CircuitState.TrainReserved == null || thisSection.CircuitState.TrainReserved.Train != enabledTrain.Train)
                        {
                            thisSection.PreReserve(enabledTrain);
                        }
                        else
                        {
                            reservable = false;
                        }
                    }
                    enabledTrain.Train.ClaimState = false;
                }

                // if claim allowed - reserve free sections and claim all other if first signal ahead of train

                else if (enabledTrain.Train.ClaimState && internalBlockState != InternalBlockstate.Reserved &&
                         enabledTrain.Train.NextSignalObject[0] != null && enabledTrain.Train.NextSignalObject[0].thisRef == thisRef)
                {
                    foreach (Train.TCRouteElement thisElement in thisRoute)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        if (thisSection.DeadlockReference > 0) // do not claim into deadlock area as path may not have been resolved
                        {
                            break;
                        }

                        if (thisSection.CircuitState.TrainReserved == null || (thisSection.CircuitState.TrainReserved.Train != enabledTrain.Train))
                        {
                            // deadlock has been set since signal request was issued - reject claim, break and reset claimstate
                            if (thisSection.DeadlockTraps.ContainsKey(thisTrain.Train.Number))
                            {
                                thisTrain.Train.ClaimState = false;
                                break;
                            }

                            // claim only if signal claim is not locked (in case of approach control)
                            if (!ClaimLocked)
                            {
                                thisSection.Claim(enabledTrain);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// propagate clearance request
        /// </summary>
        private void propagateRequest()
        {
            // no. of next signals to clear : as passed on -1 if signal has normal clear ahead
            // if passed on < 0, use this signals num to clear

            // Do not propagate the request in explorer mode, as it is handled elsewhere
            if (enabledTrain != null && enabledTrain.Train.ControlMode == Train.TRAIN_CONTROL.EXPLORER) return;

            // sections not available
            bool validPropagationRequest = true;
            if (internalBlockState > InternalBlockstate.Reservable)
            {
                validPropagationRequest = false;
            }

            // sections not reserved and no forced propagation
            if (!ForcePropagation && !ForcePropOnApproachControl && internalBlockState > InternalBlockstate.Reserved)
            {
                validPropagationRequest = false;
            }

            // route is not fully available so do not propagate
            if (!validPropagationRequest)
            {
                return;
            }

            SignalObject nextSignal = null;
            if (sigfound[SignalFunction.NORMAL] >= 0)
            {
                nextSignal = signalObjects[sigfound[SignalFunction.NORMAL]];
            }

            Train.TCSubpathRoute RoutePart;
            if (enabledTrain != null)
            {
                RoutePart = enabledTrain.Train.ValidRoute[enabledTrain.TrainRouteDirectionIndex];   // if known which route to use
            }
            else
            {
                RoutePart = signalRoute; // else use signal route
            }

            bool propagateState = true;  // normal propagate state

            // update ReqNumClearAhead if signal is not propagated (only when SignamNumClearAheadActive has other than default value)

            if (!isPropagated)
            {
                // set number of signals to clear ahead

                if (SignalNumClearAhead_MSTS <= -2 && SignalNumClearAheadActive != SignalNumClearAhead_ORTS)
                {
                    if (SignalNumClearAheadActive == 0)
                    {
                        ReqNumClearAhead = 0;
                    }
                    else if (SignalNumClearAheadActive > 0)
                    {
                        ReqNumClearAhead = SignalNumClearAheadActive - 1;
                    }
                    else if (SignalNumClearAheadActive < 0)
                    {
                        ReqNumClearAhead = 1;
                    }
                }
            }

            bool validBlockState = internalBlockState <= InternalBlockstate.Reserved;

            // for approach control, use reservable state instead of reserved state (sections are not reserved on approach control)
            // also on forced propagation, use reservable state instead of reserved state
            if (ApproachControlSet && ForcePropOnApproachControl)
            {
                validBlockState = internalBlockState <= InternalBlockstate.Reservable;
            }

            // if section is clear but signal remains at stop - dual signal situation - do not treat as propagate
            if (validBlockState && this_sig_lr(MstsSignalFunction.NORMAL) == MstsSignalAspect.STOP && isSignalNormal())
            {
                propagateState = false;
            }

            if ((ReqNumClearAhead > 0 || ForcePropagation) && nextSignal != null && validBlockState && (!ApproachControlSet || ForcePropOnApproachControl))
            {
                nextSignal.requestClearSignal(RoutePart, enabledTrain, ReqNumClearAhead, propagateState, this);
                propagated = true;
                ForcePropagation = false;
            }

            // check if next signal is cleared by default (state != stop and enabled == false) - if so, set train as enabled train but only if train's route covers signal route

            if (nextSignal != null && nextSignal.this_sig_lr(MstsSignalFunction.NORMAL) >= MstsSignalAspect.APPROACH_1 && nextSignal.hasFixedRoute && !nextSignal.enabled && enabledTrain != null)
            {
                int firstSectionIndex = nextSignal.fixedRoute.First().TCSectionIndex;
                int lastSectionIndex = nextSignal.fixedRoute.Last().TCSectionIndex;
                int firstSectionRouteIndex = RoutePart.GetRouteIndex(firstSectionIndex, 0);
                int lastSectionRouteIndex = RoutePart.GetRouteIndex(lastSectionIndex, 0);

                if (firstSectionRouteIndex >= 0 && lastSectionRouteIndex >= 0)
                {
                    nextSignal.requestClearSignal(nextSignal.fixedRoute, enabledTrain, 0, true, null);

                    int furtherSignalIndex = nextSignal.sigfound[SignalFunction.NORMAL];
                    int furtherSignalsToClear = ReqNumClearAhead - 1;

                    while (furtherSignalIndex >= 0)
                    {
                        SignalObject furtherSignal = signalRef.SignalObjects[furtherSignalIndex];
                        if (furtherSignal.this_sig_lr(MstsSignalFunction.NORMAL) >= MstsSignalAspect.APPROACH_1 && !furtherSignal.enabled && furtherSignal.hasFixedRoute)
                        {
                            firstSectionIndex = furtherSignal.fixedRoute.First().TCSectionIndex;
                            lastSectionIndex = furtherSignal.fixedRoute.Last().TCSectionIndex;
                            firstSectionRouteIndex = RoutePart.GetRouteIndex(firstSectionIndex, 0);
                            lastSectionRouteIndex = RoutePart.GetRouteIndex(lastSectionIndex, 0);

                            if (firstSectionRouteIndex >= 0 && lastSectionRouteIndex >= 0)
                            {
                                furtherSignal.requestClearSignal(furtherSignal.fixedRoute, enabledTrain, 0, true, null);

                                furtherSignal.isPropagated = true;
                                furtherSignalsToClear = furtherSignalsToClear > 0 ? furtherSignalsToClear - 1 : 0;
                                furtherSignal.ReqNumClearAhead = furtherSignalsToClear;
                                furtherSignalIndex = furtherSignal.sigfound[SignalFunction.NORMAL];
                            }
                            else
                            {
                                furtherSignalIndex = -1;
                            }
                        }
                        else
                        {
                            furtherSignalIndex = -1;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// get block state - not routed
        /// Check blockstate for normal signal which is not enabled
        /// Check blockstate for other types of signals
        /// <summary>
        private void getBlockState_notRouted()
        {

            InternalBlockstate localBlockState = InternalBlockstate.Reserved; // preset to lowest option

            // check fixed route for normal signals

            if (isSignalNormal() && hasFixedRoute)
            {
                for (int i = 0; i < fixedRoute.Count; i++)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[fixedRoute[i].TCSectionIndex];
                    if (thisSection.CircuitState.HasTrainsOccupying())
                    {
                        localBlockState = InternalBlockstate.OccupiedSameDirection;
                    }
                }
            }

            // otherwise follow sections upto first non-set switch or next signal
            else
            {
                int thisTC = TCReference;
                int direction = TCDirection;
                int nextTC = -1;

                // for normal signals : start at next TC

                if (TCNextTC > 0)
                {
                    thisTC = TCNextTC;
                    direction = TCNextDirection;
                }

                // get trackcircuit

                TrackCircuitSection thisSection = null;
                if (thisTC > 0)
                {
                    thisSection = signalRef.TrackCircuitList[thisTC];
                }

                // loop through valid sections

                while (thisSection != null)
                {

                    // set blockstate

                    if (thisSection.CircuitState.HasTrainsOccupying())
                    {
                        if (thisSection.Index == TCReference)  // for section where signal is placed, check if train is ahead
                        {
                            Dictionary<Train, float> trainAhead =
                                                    thisSection.TestTrainAhead(null, TCOffset, TCDirection);
                            if (trainAhead.Count > 0)
                                localBlockState = InternalBlockstate.OccupiedSameDirection;
                        }
                        else
                        {
                            localBlockState = InternalBlockstate.OccupiedSameDirection;
                        }
                    }

                    // if section has signal at end stop check

                    if (thisSection.EndSignals[direction] != null || thisSection.CircuitType == TrackCircuitSection.TrackCircuitType.EndOfTrack)
                    {
                        thisSection = null;
                    }

                    // get next section if active link is set

                    else
                    {
                        //                     int pinIndex = direction == 0 ? 1 : 0;
                        int pinIndex = direction;
                        nextTC = thisSection.ActivePins[pinIndex, 0].Link;
                        direction = thisSection.ActivePins[pinIndex, 0].Direction;
                        if (nextTC == -1)
                        {
                            nextTC = thisSection.ActivePins[pinIndex, 1].Link;
                            direction = thisSection.ActivePins[pinIndex, 1].Direction;
                        }

                        // set state to blocked if ending at unset or unaligned switch

                        if (nextTC >= 0)
                        {
                            thisSection = signalRef.TrackCircuitList[nextTC];
                        }
                        else
                        {
                            thisSection = null;
                            localBlockState = InternalBlockstate.Blocked;
                        }
                    }
                }
            }

            internalBlockState = localBlockState;
        }

        /// <summary>
        /// Get block state
        /// Get internal state of full block for normal enabled signal upto next signal for clear request
        /// returns true if train set to use alternative route
        /// </summary>
        private bool getBlockState(Train.TCSubpathRoute thisRoute, Train.TrainRouted thisTrain, bool AIPermissionRequest)
        {
            if (signalRef.UseLocationPassingPaths)
            {
                return getBlockState_locationBased(thisRoute, thisTrain, AIPermissionRequest);
            }
            else
            {
                return getBlockState_pathBased(thisRoute, thisTrain);
            }
        }

        /// <summary>
        /// Get block state
        /// Get internal state of full block for normal enabled signal upto next signal for clear request
        /// returns true if train set to use alternative route
        /// based on path-based deadlock processing
        /// </summary>
        private bool getBlockState_pathBased(Train.TCSubpathRoute thisRoute, Train.TrainRouted thisTrain)
        {
            bool returnvalue = false;

            InternalBlockstate blockstate = InternalBlockstate.Reserved;  // preset to lowest possible state //

            // loop through all sections in route list

            Train.TCRouteElement lastElement = null;

            foreach (Train.TCRouteElement thisElement in thisRoute)
            {
                lastElement = thisElement;
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                int direction = thisElement.Direction;
                blockstate = thisSection.getSectionState(enabledTrain, direction, blockstate, thisRoute, thisRef);
                if (blockstate > InternalBlockstate.Reservable)
                    break;           // break on first non-reservable section //

                // if alternative path from section available but train already waiting for deadlock, set blocked
                if (thisElement.StartAlternativePath != null)
                {
                    TrackCircuitSection endSection = signalRef.TrackCircuitList[thisElement.StartAlternativePath[1]];
                    if (endSection.CheckDeadlockAwaited(thisTrain.Train.Number))
                    {
                        blockstate = InternalBlockstate.Blocked;
                        lastElement = thisElement;
                        break;
                    }
                }
            }

            // check if alternative route available

            int lastElementIndex = thisRoute.GetRouteIndex(lastElement.TCSectionIndex, 0);

            if (blockstate > InternalBlockstate.Reservable && thisTrain != null)
            {
                int startAlternativeRoute = -1;
                int altRoute = -1;

                Train.TCSubpathRoute trainRoute = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex];
                Train.TCPosition thisPosition = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex];

                for (int iElement = lastElementIndex; iElement >= 0; iElement--)
                {
                    Train.TCRouteElement prevElement = thisRoute[iElement];
                    if (prevElement.StartAlternativePath != null)
                    {
                        startAlternativeRoute =
                            trainRoute.GetRouteIndex(thisRoute[iElement].TCSectionIndex, thisPosition.RouteListIndex);
                        altRoute = prevElement.StartAlternativePath[0];
                        break;
                    }
                }

                // check if alternative path may be used

                if (startAlternativeRoute > 0)
                {
                    Train.TCRouteElement startElement = trainRoute[startAlternativeRoute];
                    int endSectionIndex = startElement.StartAlternativePath[1];
                    TrackCircuitSection endSection = signalRef.TrackCircuitList[endSectionIndex];
                    if (endSection.CheckDeadlockAwaited(thisTrain.Train.Number))
                    {
                        startAlternativeRoute = -1; // reset use of alternative route
                    }
                }

                // if available, select part of route upto next signal

                if (startAlternativeRoute > 0)
                {
                    Train.TCSubpathRoute altRoutePart = thisTrain.Train.ExtractAlternativeRoute_pathBased(altRoute);

                    // check availability of alternative route

                    InternalBlockstate newblockstate = InternalBlockstate.Reservable;

                    foreach (Train.TCRouteElement thisElement in altRoutePart)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        int direction = thisElement.Direction;
                        newblockstate = thisSection.getSectionState(enabledTrain, direction, newblockstate, thisRoute, thisRef);
                        if (newblockstate > InternalBlockstate.Reservable)
                            break;           // break on first non-reservable section //
                    }

                    // if available, use alternative route

                    if (newblockstate <= InternalBlockstate.Reservable)
                    {
                        blockstate = newblockstate;
                        thisTrain.Train.SetAlternativeRoute_pathBased(startAlternativeRoute, altRoute, this);
                        returnvalue = true;
                    }
                }
            }

            // check if approaching deadlock part, and if alternative route must be taken - if point where alt route start is not yet reserved
            // alternative route may not be taken if there is a train already waiting for the deadlock
            else if (thisTrain != null)
            {
                int startAlternativeRoute = -1;
                int altRoute = -1;
                TrackCircuitSection startSection = null;
                TrackCircuitSection endSection = null;

                Train.TCSubpathRoute trainRoute = thisTrain.Train.ValidRoute[thisTrain.TrainRouteDirectionIndex];
                Train.TCPosition thisPosition = thisTrain.Train.PresentPosition[thisTrain.TrainRouteDirectionIndex];

                for (int iElement = lastElementIndex; iElement >= 0; iElement--)
                {
                    Train.TCRouteElement prevElement = thisRoute[iElement];
                    if (prevElement.StartAlternativePath != null)
                    {
                        endSection = signalRef.TrackCircuitList[prevElement.StartAlternativePath[1]];
                        if (endSection.DeadlockTraps.ContainsKey(thisTrain.Train.Number) && !endSection.CheckDeadlockAwaited(thisTrain.Train.Number))
                        {
                            altRoute = prevElement.StartAlternativePath[0];
                            startAlternativeRoute =
                                trainRoute.GetRouteIndex(prevElement.TCSectionIndex, thisPosition.RouteListIndex);
                            startSection = signalRef.TrackCircuitList[prevElement.TCSectionIndex];
                        }
                        break;
                    }
                }

                // use alternative route

                if (startAlternativeRoute > 0 &&
                    (startSection.CircuitState.TrainReserved == null || startSection.CircuitState.TrainReserved.Train != thisTrain.Train))
                {
                    Train.TCSubpathRoute altRoutePart = thisTrain.Train.ExtractAlternativeRoute_pathBased(altRoute);

                    // check availability of alternative route

                    InternalBlockstate newblockstate = InternalBlockstate.Reservable;

                    foreach (Train.TCRouteElement thisElement in altRoutePart)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        int direction = thisElement.Direction;
                        newblockstate = thisSection.getSectionState(enabledTrain, direction, newblockstate, thisRoute, thisRef);
                        if (newblockstate > InternalBlockstate.Reservable)
                            break;           // break on first non-reservable section //
                    }

                    // if available, use alternative route

                    if (newblockstate <= InternalBlockstate.Reservable)
                    {
                        blockstate = newblockstate;
                        thisTrain.Train.SetAlternativeRoute_pathBased(startAlternativeRoute, altRoute, this);
                        if (endSection.DeadlockTraps.ContainsKey(thisTrain.Train.Number) && !endSection.DeadlockAwaited.Contains(thisTrain.Train.Number))
                            endSection.DeadlockAwaited.Add(thisTrain.Train.Number);
                        returnvalue = true;

                    }
                }
            }

            internalBlockState = blockstate;
            return returnvalue;
        }

        /// <summary>
        /// Get block state
        /// Get internal state of full block for normal enabled signal upto next signal for clear request
        /// returns true if train set to use alternative route
        /// based on location-based deadlock processing
        /// </summary>
        private bool getBlockState_locationBased(Train.TCSubpathRoute thisRoute, Train.TrainRouted thisTrain, bool AIPermissionRequest)
        {
            SectionsWithAlternativePath.Clear();
            SectionsWithAltPathSet.Clear();
            bool altRouteAssigned = false;

            bool returnvalue = false;
            bool deadlockArea = false;

            InternalBlockstate blockstate = InternalBlockstate.Reserved;  // preset to lowest possible state //

            // loop through all sections in route list

            Train.TCRouteElement lastElement = null;

            foreach (Train.TCRouteElement thisElement in thisRoute)
            {
                lastElement = thisElement;
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                int direction = thisElement.Direction;

                blockstate = thisSection.getSectionState(enabledTrain, direction, blockstate, thisRoute, thisRef);
                if (blockstate > InternalBlockstate.OccupiedSameDirection)
                    break;     // exit on first none-available section

                // check if section is trigger section for waitany instruction
                if (thisTrain != null)
                {
                    if (thisTrain.Train.CheckAnyWaitCondition(thisSection.Index))
                    {
                        blockstate = InternalBlockstate.Blocked;
                    }
                }

                // check if this section is start of passing path area
                // if so, select which path must be used - but only if cleared by train in AUTO mode

                if (thisSection.DeadlockReference > 0 && thisElement.FacingPoint && thisTrain != null)
                {
                    if (thisTrain.Train.ControlMode == Train.TRAIN_CONTROL.AUTO_NODE || thisTrain.Train.ControlMode == Train.TRAIN_CONTROL.AUTO_SIGNAL)
                    {
                        DeadlockInfo sectionDeadlockInfo = signalRef.DeadlockInfoList[thisSection.DeadlockReference];

                        // if deadlock area and no path yet selected - exit loop; else follow assigned path
                        if (sectionDeadlockInfo.HasTrainAndSubpathIndex(thisTrain.Train.Number, thisTrain.Train.TCRoute.activeSubpath) &&
                            thisElement.UsedAlternativePath < 0)
                        {
                            deadlockArea = true;
                            break; // exits on deadlock area
                        }
                        else
                        {
                            SectionsWithAlternativePath.Add(thisElement.TCSectionIndex);
                            altRouteAssigned = true;
                        }
                    }
                }
                if (thisTrain != null && blockstate == InternalBlockstate.OccupiedSameDirection && (AIPermissionRequest || hasPermission == Permission.Requested)) break;
            }

            // if deadlock area : check alternative path if not yet selected - but only if opening junction is reservable
            // if free alternative path is found, set path available otherwise set path blocked

            if (deadlockArea && lastElement.UsedAlternativePath < 0)
            {
                if (blockstate <= InternalBlockstate.Reservable)
                {

#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\Temp\deadlock.txt",
                    "\n **** Get block state for section " + lastElement.TCSectionIndex.ToString() + " for train : " + thisTrain.Train.Number.ToString() + "\n");
#endif
                    TrackCircuitSection lastSection = signalRef.TrackCircuitList[lastElement.TCSectionIndex];
                    DeadlockInfo sectionDeadlockInfo = signalRef.DeadlockInfoList[lastSection.DeadlockReference];
                    List<int> availableRoutes = sectionDeadlockInfo.CheckDeadlockPathAvailability(lastSection, thisTrain.Train);

#if DEBUG_DEADLOCK
                File.AppendAllText(@"C:\Temp\deadlock.txt", "\nReturned no. of available paths : " + availableRoutes.Count.ToString() + "\n");
                File.AppendAllText(@"C:\Temp\deadlock.txt", "****\n\n");
#endif

                    if (availableRoutes.Count >= 1)
                    {
                        int endSectionIndex = -1;
                        int usedRoute = sectionDeadlockInfo.SelectPath(availableRoutes, thisTrain.Train, ref endSectionIndex);
                        lastElement.UsedAlternativePath = usedRoute;
                        SectionsWithAltPathSet.Add(lastElement.TCSectionIndex);
                        altRouteAssigned = true;

                        thisTrain.Train.SetAlternativeRoute_locationBased(lastSection.Index, sectionDeadlockInfo, usedRoute, this);
                        returnvalue = true;
                        blockstate = InternalBlockstate.Reservable;
                    }
                    else
                    {
                        blockstate = InternalBlockstate.Blocked;
                    }
                }
                else
                {
                    blockstate = InternalBlockstate.Blocked;
                }
            }

            internalBlockState = blockstate;

            // reset any alternative route selections if route is not available
            if (altRouteAssigned && blockstate != InternalBlockstate.Reservable && blockstate != InternalBlockstate.Reserved)
            {
                foreach (int SectionNo in SectionsWithAlternativePath)
                {
#if DEBUG_REPORTS
                    Trace.TraceInformation("Train : {0} : state {1} but route already set for section {2}",
                        thisTrain.Train.Name, blockstate, SectionNo);
#endif
                    int routeIndex = thisTrain.Train.ValidRoute[0].GetRouteIndex(SectionNo, thisTrain.Train.PresentPosition[0].RouteListIndex);
                    Train.TCRouteElement thisElement = thisTrain.Train.ValidRoute[0][routeIndex];
                    thisElement.UsedAlternativePath = -1;
                }
                foreach (int SectionNo in SectionsWithAltPathSet)
                {
#if DEBUG_REPORTS
                    Trace.TraceInformation("Train : {0} : state {1} but route now set for section {2}",
                        thisTrain.Train.Name, blockstate, SectionNo);
#endif
                    int routeIndex = thisTrain.Train.ValidRoute[0].GetRouteIndex(SectionNo, thisTrain.Train.PresentPosition[0].RouteListIndex);
                    Train.TCRouteElement thisElement = thisTrain.Train.ValidRoute[0][routeIndex];
                    thisElement.UsedAlternativePath = -1;
                }
            }

            return returnvalue;
        }

        /// <summary>
        /// Get part block state
        /// Get internal state of part of block for normal enabled signal upto next signal for clear request
        /// if there are no switches before next signal or end of track, treat as full block
        /// </summary>
        private void getPartBlockState(Train.TCSubpathRoute thisRoute)
        {

            // check beyond last section for next signal or end of track 

            int listIndex = (thisRoute.Count > 0) ? (thisRoute.Count - 1) : thisTrainRouteIndex;

            Train.TCRouteElement lastElement = thisRoute[listIndex];
            int thisSectionIndex = lastElement.TCSectionIndex;
            int direction = lastElement.Direction;

            Train.TCSubpathRoute additionalElements = new Train.TCSubpathRoute();

            bool end_of_info = false;

            while (!end_of_info)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];

                TrackCircuitSection.TrackCircuitType thisType = thisSection.CircuitType;

                switch (thisType)
                {
                    case TrackCircuitSection.TrackCircuitType.EndOfTrack:
                        end_of_info = true;
                        break;

                    case TrackCircuitSection.TrackCircuitType.Junction:
                    case TrackCircuitSection.TrackCircuitType.Crossover:
                        end_of_info = true;
                        break;

                    default:
                        Train.TCRouteElement newElement = new Train.TCRouteElement(thisSectionIndex, direction);
                        additionalElements.Add(newElement);

                        if (thisSection.EndSignals[direction] != null)
                        {
                            end_of_info = true;
                        }
                        break;
                }

                if (!end_of_info)
                {
                    thisSectionIndex = thisSection.Pins[direction, 0].Link;
                    direction = thisSection.Pins[direction, 0].Direction;
                }
            }

            InternalBlockstate blockstate = InternalBlockstate.Reserved;  // preset to lowest possible state //

            // check all elements in original route

            foreach (Train.TCRouteElement thisElement in thisRoute)
            {
                TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                direction = thisElement.Direction;
                blockstate = thisSection.getSectionState(enabledTrain, direction, blockstate, thisRoute, thisRef);
                if (blockstate > InternalBlockstate.Reservable)
                    break;           // break on first non-reservable section //
            }

            // check all additional elements upto signal, junction or end-of-track

            if (blockstate <= InternalBlockstate.Reservable)
            {
                foreach (Train.TCRouteElement thisElement in additionalElements)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    direction = thisElement.Direction;
                    blockstate = thisSection.getSectionState(enabledTrain, direction, blockstate, additionalElements, thisRef);
                    if (blockstate > InternalBlockstate.Reservable)
                        break;           // break on first non-reservable section //
                }
            }

            internalBlockState = blockstate;
        }

        /// <summary>
        /// Set signal default route and next signal list as switch in route is reset
        /// Used in manual mode for signals which clear by default
        /// </summary>
        public void SetDefaultRoute()
        {
            signalRoute = new Train.TCSubpathRoute(fixedRoute);
            foreach (SignalFunction function in signalRef.SignalFunctions.Values)
            {
                sigfound[function] = defaultNextSignal[function];
            }
        }

        /// <summary>
        /// Reset signal and clear all train sections
        /// </summary>
        public void ResetSignal(bool propagateReset)
        {
            Train.TrainRouted thisTrain = enabledTrain;

            // search for last signal enabled for this train, start reset from there //

            SignalObject thisSignal = this;
            List<SignalObject> passedSignals = new List<SignalObject>();
            int thisSignalIndex = thisSignal.thisRef;

            if (propagateReset)
            {
                while (thisSignalIndex >= 0 && signalObjects[thisSignalIndex].enabledTrain == thisTrain)
                {
                    thisSignal = signalObjects[thisSignalIndex];
                    passedSignals.Add(thisSignal);
                    thisSignalIndex = thisSignal.sigfound[SignalFunction.NORMAL];
                }
            }
            else
            {
                passedSignals.Add(thisSignal);
            }

            foreach (SignalObject nextSignal in passedSignals)
            {
                if (nextSignal.signalRoute != null)
                {
                    List<TrackCircuitSection> sectionsToClear = new List<TrackCircuitSection>();
                    foreach (Train.TCRouteElement thisElement in nextSignal.signalRoute)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        sectionsToClear.Add(thisSection);  // store in list as signalRoute is lost during remove action
                    }
                    foreach (TrackCircuitSection thisSection in sectionsToClear)
                    {
                        if (thisTrain != null)
                        {
                            thisSection.RemoveTrain(thisTrain, false);
                        }
                        else
                        {
                            thisSection.Unreserve();
                        }
                    }
                }

                nextSignal.resetSignalEnabled();
            }
        }

        /// <summary>
        /// Reset signal route and next signal list as switch in route is reset
        /// </summary>
        public void ResetRoute(int resetSectionIndex)
        {

            // remove this signal from any other junctions

            foreach (int thisSectionIndex in JunctionsPassed)
            {
                if (thisSectionIndex != resetSectionIndex)
                {
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisSectionIndex];
                    thisSection.SignalsPassingRoutes.Remove(thisRef);
                }
            }

            JunctionsPassed.Clear();

            foreach (SignalFunction function in signalRef.SignalFunctions.Values)
            {
                sigfound[function] = defaultNextSignal[function];
            }

            // if signal is enabled, ensure next normal signal is reset

            if (enabledTrain != null && sigfound[SignalFunction.NORMAL] < 0)
            {
                sigfound[SignalFunction.NORMAL] = SONextSignalNormal(TCNextTC);
            }

#if DEBUG_REPORTS
            File.AppendAllText(@"C:\temp\printproc.txt",
				String.Format("Signal {0} reset on Junction Change\n",
				thisRef));

            if (enabledTrain != null)
            {
				File.AppendAllText(@"C:\temp\printproc.txt",
					String.Format("Train {0} affected; new NORMAL signal : {1}\n",
					enabledTrain.Train.Number, sigfound[(int)MstsSignalFunction.NORMAL]));
            }
#endif
            if (enabledTrain != null && enabledTrain.Train.CheckTrain)
            {
                File.AppendAllText(@"C:\temp\checktrain.txt",
                    String.Format("Signal {0} reset on Junction Change\n",
                    thisRef));
                File.AppendAllText(@"C:\temp\checktrain.txt",
                    String.Format("Train {0} affected; new NORMAL signal : {1}\n",
                    enabledTrain.Train.Number, sigfound[SignalFunction.NORMAL]));
            }
        }

        /// <summary>
        /// Set flag to allow signal to clear to partial route
        /// </summary>
        public void AllowClearPartialRoute(int setting)
        {
            AllowPartRoute = setting == 1 ? true : false;
        }

        /// <summary>
        /// Test for approach control - position only
        /// </summary>
        public bool ApproachControlPosition(int reqPositionM, string dumpfile, bool forced)
        {
            // no train approaching
            if (enabledTrain == null)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    File.AppendAllText(dumpfile, "APPROACH CONTROL : no train approaching");
                }

                return false;
            }

            // signal is not first signal for train - check only if not forced
            if (!forced)
            {
                if (enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex] == null ||
                    enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex].thisRef != thisRef)
                {
                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        var sob = new StringBuilder();
                        sob.AppendFormat("APPROACH CONTROL : Train {0} : First signal is not this signal but {1} \n",
                            enabledTrain.Train.Number, enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex].thisRef);
                        File.AppendAllText(dumpfile, sob.ToString());
                    }

                    ApproachControlSet = true;  // approach control is selected but train is yet further out, so assume approach control has locked signal
                    return false;
                }
            }

            // if already cleared - return true

            if (ApproachControlCleared)
            {
                ApproachControlSet = false;
                ClaimLocked = false;
                ForcePropOnApproachControl = false;
                return true;
            }

            bool found = false;
            bool isNormal = isSignalNormal();
            float distance = 0;
            int actDirection = enabledTrain.TrainRouteDirectionIndex;
            Train.TCSubpathRoute routePath = enabledTrain.Train.ValidRoute[actDirection];
            int actRouteIndex = routePath == null ? -1 : routePath.GetRouteIndex(enabledTrain.Train.PresentPosition[actDirection].TCSectionIndex, 0);
            if (actRouteIndex >= 0)
            {
                float offset;
                if (enabledTrain.TrainRouteDirectionIndex == 0)
                    offset = enabledTrain.Train.PresentPosition[0].TCOffset;
                else
                    offset = signalRef.TrackCircuitList[enabledTrain.Train.PresentPosition[1].TCSectionIndex].Length - enabledTrain.Train.PresentPosition[1].TCOffset;
                while (!found && actRouteIndex < routePath.Count)
                {
                    Train.TCRouteElement thisElement = routePath[actRouteIndex];
                    TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                    if (thisSection.EndSignals[thisElement.Direction] == this)
                    {
                        distance += thisSection.Length - offset;
                        found = true;
                    }
                    else if (!isNormal)
                    {
                        TrackCircuitSignalList thisSignalList = thisSection.CircuitItems.TrackCircuitSignals[thisElement.Direction][SignalHeads[0].Function];
                        foreach (TrackCircuitSignalItem thisSignal in thisSignalList.TrackCircuitItem)
                        {
                            if (thisSignal.SignalRef == this)
                            {
                                distance += thisSignal.SignalLocation - offset;
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found)
                    {
                        distance += thisSection.Length - offset;
                        offset = 0;
                        actRouteIndex++;
                    }
                }
            }

            if (!found)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("APPROACH CONTROL : Train {0} has no valid path to signal, clear not allowed \n", enabledTrain.Train.Number);
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                ApproachControlSet = true;
                return false;
            }

            // test distance

            if (Convert.ToInt32(distance) < reqPositionM)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("APPROACH CONTROL : Train {0} at distance {1} (required {2}), clear allowed \n",
                        enabledTrain.Train.Number, distance, reqPositionM);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                ApproachControlSet = false;
                ApproachControlCleared = true;
                ClaimLocked = false;
                ForcePropOnApproachControl = false;
                return true;
            }
            else
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("APPROACH CONTROL : Train {0} at distance {1} (required {2}), clear not allowed \n",
                        enabledTrain.Train.Number, distance, reqPositionM);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                ApproachControlSet = true;
                return false;
            }
        }

        /// <summary>
        /// Test for approach control - position and speed
        /// </summary>
        public bool ApproachControlSpeed(int reqPositionM, int reqSpeedMpS, string dumpfile)
        {
            // no train approaching
            if (enabledTrain == null)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    File.AppendAllText(dumpfile, "APPROACH CONTROL : no train approaching");
                }

                return false;
            }

            // signal is not first signal for train
            if (enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex] != null &&
                enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex].thisRef != thisRef)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("APPROACH CONTROL : Train {0} : First signal is not this signal but {1} \n",
                        enabledTrain.Train.Number, enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex].thisRef);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                ApproachControlSet = true;
                return false;
            }

            // if already cleared - return true

            if (ApproachControlCleared)
            {
                ApproachControlSet = false;
                ForcePropOnApproachControl = false;
                return true;
            }

            // check if distance is valid

            if (!enabledTrain.Train.DistanceToSignal.HasValue)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("APPROACH CONTROL : Train {0} has no valid distance to signal, clear not allowed \n",
                        enabledTrain.Train.Number);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                ApproachControlSet = true;
                return false;
            }

            // test distance

            if (Convert.ToInt32(enabledTrain.Train.DistanceToSignal.Value) < reqPositionM)
            {
                bool validSpeed = false;
                if (reqSpeedMpS > 0)
                {
                    if (Math.Abs(enabledTrain.Train.SpeedMpS) < reqSpeedMpS)
                    {
                        validSpeed = true;
                    }
                }
                else if (reqSpeedMpS == 0)
                {
                    if (Math.Abs(enabledTrain.Train.SpeedMpS) < 0.1)
                    {
                        validSpeed = true;
                    }
                }

                if (validSpeed)
                {
                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        var sob = new StringBuilder();
                        sob.AppendFormat("APPROACH CONTROL : Train {0} at distance {1} (required {2}) and speed {3} (required {4}), clear allowed \n",
                            enabledTrain.Train.Number, enabledTrain.Train.DistanceToSignal.Value, reqPositionM, enabledTrain.Train.SpeedMpS, reqSpeedMpS);
                        File.AppendAllText(dumpfile, sob.ToString());
                    }

                    ApproachControlCleared = true;
                    ApproachControlSet = false;
                    ClaimLocked = false;
                    ForcePropOnApproachControl = false;
                    return true;
                }
                else
                {
                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        var sob = new StringBuilder();
                        sob.AppendFormat("APPROACH CONTROL : Train {0} at distance {1} (required {2}) and speed {3} (required {4}), clear not allowed \n",
                            enabledTrain.Train.Number, enabledTrain.Train.DistanceToSignal.Value, reqPositionM, enabledTrain.Train.SpeedMpS, reqSpeedMpS);
                        File.AppendAllText(dumpfile, sob.ToString());
                    }

                    ApproachControlSet = true;
                    return false;
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("APPROACH CONTROL : Train {0} at distance {1} (required {2}), clear not allowed \n",
                        enabledTrain.Train.Number, enabledTrain.Train.DistanceToSignal.Value, reqPositionM);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                ApproachControlSet = true;
                return false;
            }
        }

        /// <summary>
        /// Test for approach control in case of APC on next STOP
        /// </summary>
        public bool ApproachControlNextStop(int reqPositionM, int reqSpeedMpS, string dumpfile)
        {
            // no train approaching
            if (enabledTrain == null)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    File.AppendAllText(dumpfile, "APPROACH CONTROL : no train approaching\n");
                }

                return false;
            }

            // signal is not first signal for train
            if (enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex] != null &&
                enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex].thisRef != thisRef)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("APPROACH CONTROL : Train {0} : First signal is not this signal but {1} \n",
                        enabledTrain.Train.Number, enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex].thisRef);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                ApproachControlSet = true;
                ForcePropOnApproachControl = true;
                return false;
            }

            // if already cleared - return true

            if (ApproachControlCleared)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    File.AppendAllText(dumpfile, "APPROACH CONTROL : cleared\n");
                }

                return true;
            }

            // check if distance is valid

            if (!enabledTrain.Train.DistanceToSignal.HasValue)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("APPROACH CONTROL : Train {0} has no valid distance to signal, clear not allowed \n",
                        enabledTrain.Train.Number);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                ApproachControlSet = true;
                return false;
            }

            // test distance

            if (Convert.ToInt32(enabledTrain.Train.DistanceToSignal.Value) < reqPositionM)
            {
                bool validSpeed = false;
                if (reqSpeedMpS > 0)
                {
                    if (Math.Abs(enabledTrain.Train.SpeedMpS) < reqSpeedMpS)
                    {
                        validSpeed = true;
                    }
                }
                else if (reqSpeedMpS == 0)
                {
                    if (Math.Abs(enabledTrain.Train.SpeedMpS) < 0.1)
                    {
                        validSpeed = true;
                    }
                }

                if (validSpeed)
                {
                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        var sob = new StringBuilder();
                        sob.AppendFormat("APPROACH CONTROL : Train {0} at distance {1} (required {2}) and speed {3} (required {4}), clear allowed \n",
                            enabledTrain.Train.Number, enabledTrain.Train.DistanceToSignal.Value, reqPositionM, enabledTrain.Train.SpeedMpS, reqSpeedMpS);
                        File.AppendAllText(dumpfile, sob.ToString());
                    }

                    ApproachControlCleared = true;
                    ApproachControlSet = false;
                    ClaimLocked = false;
                    ForcePropOnApproachControl = false;
                    return true;
                }
                else
                {
                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        var sob = new StringBuilder();
                        sob.AppendFormat("APPROACH CONTROL : Train {0} at distance {1} (required {2}) and speed {3} (required {4}), clear not allowed \n",
                            enabledTrain.Train.Number, enabledTrain.Train.DistanceToSignal.Value, reqPositionM, enabledTrain.Train.SpeedMpS, reqSpeedMpS);
                        File.AppendAllText(dumpfile, sob.ToString());
                    }

                    ApproachControlSet = true;
                    ForcePropOnApproachControl = true;
                    return false;
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("APPROACH CONTROL : Train {0} at distance {1} (required {2}), clear not allowed \n",
                        enabledTrain.Train.Number, enabledTrain.Train.DistanceToSignal.Value, reqPositionM);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                ApproachControlSet = true;
                ForcePropOnApproachControl = true;
                return false;
            }
        }

        /// <summary>
        /// Lock claim (only if approach control is active)
        /// </summary>
        public void LockClaim()
        {
            ClaimLocked = ApproachControlSet;
        }

        /// <summary>
        /// Activate timing trigger
        /// </summary>
        public void ActivateTimingTrigger()
        {
            TimingTriggerValue = signalRef.Simulator.GameTime;
        }

        /// <summary>
        /// Check timing trigger
        /// </summary>
        public bool CheckTimingTrigger(int reqTiming, string dumpfile)
        {
            int foundDelta = (int)(signalRef.Simulator.GameTime - TimingTriggerValue);
            bool triggerExceeded = foundDelta > reqTiming;

            if (!String.IsNullOrEmpty(dumpfile))
            {
                var sob = new StringBuilder();
                sob.AppendFormat("TIMING TRIGGER : found delta time : {0}; return state {1} \n", foundDelta, triggerExceeded.ToString());
                File.AppendAllText(dumpfile, sob.ToString());
            }

            return triggerExceeded;
        }

        /// <summary>
        /// Test if train has call-on set
        /// </summary>
        public bool TrainHasCallOn(bool allowOnNonePlatform, bool allowAdvancedSignal, string dumpfile)
        {
            // no train approaching
            if (enabledTrain == null)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    File.AppendAllText(dumpfile, "CALL ON : no train approaching \n");
                }

                return false;
            }

            // signal is not first signal for train
            var nextSignal = enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex];

            if (!allowAdvancedSignal &&
               nextSignal != null && nextSignal.thisRef != thisRef)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("CALL ON : Train {0} : First signal is not this signal but {1} \n",
                        enabledTrain.Train.Name, enabledTrain.Train.NextSignalObject[enabledTrain.TrainRouteDirectionIndex].thisRef);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                return false;
            }

            if (enabledTrain.Train != null && signalRoute != null)
            {
                bool callOnValid = CallOnManuallyAllowed || enabledTrain.Train.TestCallOn(this, allowOnNonePlatform, signalRoute, dumpfile);
                return callOnValid;
            }

            if (!String.IsNullOrEmpty(dumpfile))
            {
                var sob = new StringBuilder();
                sob.AppendFormat("CALL ON : Train {0} : not valid \n", enabledTrain.Train.Name);
                File.AppendAllText(dumpfile, sob.ToString());
            }
            return false;
        }

        /// <summary>
        /// Test if train requires next signal
        /// </summary>
        public bool RequiresNextSignal(int nextSignalId, int reqPosition, string dumpfile)
        {
            if (!String.IsNullOrEmpty(dumpfile))
            {
                var sob = new StringBuilder();
                sob.AppendFormat("REQ_NEXT_SIGNAL : check for signal {0} \n", nextSignalId);
                File.AppendAllText(dumpfile, sob.ToString());
            }

            // no enabled train
            if (enabledTrain == null)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("REQ_NEXT_SIGNAL : FALSE : no enabled train \n");
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                return false;
            }

            if (!String.IsNullOrEmpty(dumpfile))
            {
                var sob = new StringBuilder();
                sob.AppendFormat("REQ_NEXT_SIGNAL : enabled train : {0} = {1} \n", enabledTrain.Train.Name, enabledTrain.Train.Number);
                File.AppendAllText(dumpfile, sob.ToString());
            }

            // train has no path
            Train reqTrain = enabledTrain.Train;
            if (reqTrain.ValidRoute == null || reqTrain.ValidRoute[enabledTrain.TrainRouteDirectionIndex] == null || reqTrain.ValidRoute[enabledTrain.TrainRouteDirectionIndex].Count <= 0)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("REQ_NEXT_SIGNAL : FALSE : train has no valid route \n");
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                return false;
            }

            // next signal is not valid
            if (nextSignalId < 0 || nextSignalId >= signalObjects.Length || !signalObjects[nextSignalId].isSignalNormal())
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("REQ_NEXT_SIGNAL : FALSE : signal is not NORMAL signal \n");
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                return false;
            }

            // trains present position is unknown
            if (reqTrain.PresentPosition[enabledTrain.TrainRouteDirectionIndex].RouteListIndex < 0 ||
                reqTrain.PresentPosition[enabledTrain.TrainRouteDirectionIndex].RouteListIndex >= reqTrain.ValidRoute[enabledTrain.TrainRouteDirectionIndex].Count)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("REQ_NEXT_SIGNAL : FALSE : train has no valid position : {0} (of {1}) \n",
                        reqTrain.PresentPosition[enabledTrain.TrainRouteDirectionIndex].RouteListIndex,
                        reqTrain.ValidRoute[enabledTrain.TrainRouteDirectionIndex].Count);
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                return false;
            }

            // check if section beyond or ahead of next signal is within trains path ahead of present position of train
            int reqSection = reqPosition == 1 ? signalObjects[nextSignalId].TCNextTC : signalObjects[nextSignalId].TCReference;

            int sectionIndex = reqTrain.ValidRoute[enabledTrain.TrainRouteDirectionIndex].GetRouteIndex(reqSection, reqTrain.PresentPosition[enabledTrain.TrainRouteDirectionIndex].RouteListIndex);
            if (sectionIndex > 0)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("REQ_NEXT_SIGNAL : TRUE : signal position is in route : section {0} has index {1} \n",
                        signalObjects[nextSignalId].TCNextTC, sectionIndex);
                    File.AppendAllText(dumpfile, sob.ToString());
                }
                return true;
            }

            if (!String.IsNullOrEmpty(dumpfile))
            {
                var sob = new StringBuilder();
                sob.AppendFormat("REQ_NEXT_SIGNAL : FALSE : signal position is not in route : section {0} has index {1} \n",
                    signalObjects[nextSignalId].TCNextTC, sectionIndex);
                File.AppendAllText(dumpfile, sob.ToString());
            }
            return false;
        }

        /// <summary>
        /// Get ident of signal ahead with specific details
        /// </summary>
        public int FindReqNormalSignal(int req_value, string dumpfile)
        {
            int foundSignal = -1;

            Train.TrainRouted acttrain = enabledTrain;
            int reqTC = isSignalNormal() ? TCNextTC : TCReference; // for normal signals, use section beyond signal; for not-normal, use signal section as TCNextTC is not set

            // not normal signals may not have enabled train - check for train at next normal signal (use section ahead of signal in this case)
            // note - next normal signal may not have the correct normal subtype, so proper search is still required
            if (enabledTrain == null && !isSignalNormal())
            {
                int nextSignalIdent = SONextSignal(SignalFunction.NORMAL);

                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("FIND_REQ_NORMAL_SIGNAL : using next normal signal for not enabled none-normal signal, found {0} \n", nextSignalIdent);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                if (nextSignalIdent >= 0)
                {
                    SignalObject nextSignal = signalObjects[nextSignalIdent];
                    acttrain = nextSignal.enabledTrain;
                    reqTC = TCReference;
                }
            }

            // no train found - no route available
            if (acttrain == null || acttrain.Train.ValidRoute[acttrain.TrainRouteDirectionIndex] == null)
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.Append("FIND_REQ_NORMAL_SIGNAL : not found : signal is not enabled or train has no valid route\n");
                    File.AppendAllText(dumpfile, sob.ToString());
                }
            }
            else
            {
                int startIndex = acttrain.Train.ValidRoute[acttrain.TrainRouteDirectionIndex].GetRouteIndex(reqTC, acttrain.Train.PresentPosition[0].RouteListIndex);

                // this signal is not on trains route
                if (startIndex < 0)
                {
                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        var sob = new StringBuilder();
                        sob.AppendFormat("FIND_REQ_NORMAL_SIGNAL : not found : cannot find signal {0} at section {1} in path of train {2}\n", thisRef, TCNextTC, acttrain.Train.Name);
                        File.AppendAllText(dumpfile, sob.ToString());
                    }
                }
                else
                {
                    // search through train route until required signal type is found, or until end of route
                    for (int iRouteIndex = startIndex; iRouteIndex < acttrain.Train.ValidRoute[acttrain.TrainRouteDirectionIndex].Count; iRouteIndex++)
                    {
                        Train.TCRouteElement thisElement = acttrain.Train.ValidRoute[acttrain.TrainRouteDirectionIndex][iRouteIndex];
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[thisElement.TCSectionIndex];
                        if (thisSection.EndSignals[thisElement.Direction] != null)
                        {
                            SignalObject endSignal = thisSection.EndSignals[thisElement.Direction];

                            // found signal, check required value
                            bool found_value = false;

                            foreach (SignalHead thisHead in endSignal.SignalHeads)
                            {
                                if (thisHead.ORTSNormalSubtypeIndex == req_value)
                                {
                                    found_value = true;
                                    if (!String.IsNullOrEmpty(dumpfile))
                                    {
                                        var sob = new StringBuilder();
                                        sob.AppendFormat("FIND_REQ_NORMAL_SIGNAL : signal found : {0} : head : {1} : state : {2} \n", endSignal.thisRef, thisHead.TDBIndex, found_value);
                                    }
                                    break;
                                }
                            }

                            if (found_value)
                            {
                                foundSignal = endSignal.thisRef;
                                if (!String.IsNullOrEmpty(dumpfile))
                                {
                                    var sob = new StringBuilder();
                                    sob.AppendFormat("FIND_REQ_NORMAL_SIGNAL : signal found : {0} : ( ", endSignal.thisRef);

                                    foreach (SignalHead otherHead in endSignal.SignalHeads)
                                    {
                                        sob.AppendFormat(" {0} ", otherHead.TDBIndex);
                                    }

                                    sob.AppendFormat(") \n");
                                    File.AppendAllText(dumpfile, sob.ToString());
                                }
                                break;
                            }
                            else
                            {
                                if (!String.IsNullOrEmpty(dumpfile))
                                {
                                    var sob = new StringBuilder();
                                    sob.AppendFormat("FIND_REQ_NORMAL_SIGNAL : signal found : {0} : ( ", endSignal.thisRef);

                                    foreach (SignalHead otherHead in endSignal.SignalHeads)
                                    {
                                        sob.AppendFormat(" {0} ", otherHead.TDBIndex);
                                    }

                                    sob.AppendFormat(") ");
                                    sob.AppendFormat("incorrect variable value : {0} \n", found_value);
                                    File.AppendAllText(dumpfile, sob.ToString());
                                }
                            }
                        }
                    }
                }
            }

            return foundSignal;
        }

        /// <summary>
        /// Check if route for train is cleared upto or beyond next required signal
        /// parameter req_position : 0 = check upto signal, 1 = check beyond signal
        /// </summary>
        public MstsBlockState RouteClearedToSignal(int req_signalid, bool allowCallOn, string dumpfile)
        {
            MstsBlockState routeState = MstsBlockState.JN_OBSTRUCTED;
            if (enabledTrain != null && enabledTrain.Train.ValidRoute[enabledTrain.TrainRouteDirectionIndex] != null && req_signalid >= 0 && req_signalid < signalRef.SignalObjects.Length)
            {
                SignalObject otherSignal = signalRef.SignalObjects[req_signalid];

                TrackCircuitSection reqSection = null;
                reqSection = signalRef.TrackCircuitList[otherSignal.TCReference];
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("ROUTE_CLEARED_TO_SIGNAL : signal checked : {0} , section [ahead] found : {1} \n", req_signalid, reqSection.Index);
                    File.AppendAllText(dumpfile, sob.ToString());
                }

                Train.TCSubpathRoute trainRoute = enabledTrain.Train.ValidRoute[enabledTrain.TrainRouteDirectionIndex];

                int thisRouteIndex = trainRoute.GetRouteIndex(isSignalNormal() ? TCNextTC : TCReference, 0);
                int otherRouteIndex = trainRoute.GetRouteIndex(otherSignal.TCReference, thisRouteIndex);

                if (otherRouteIndex < 0)
                {
                    if (!String.IsNullOrEmpty(dumpfile))
                    {
                        var sob = new StringBuilder();
                        sob.AppendFormat("ROUTE_CLEARED_TO_SIGNAL : section found is not in this trains route \n");
                        File.AppendAllText(dumpfile, sob.ToString());
                    }
                }

                // extract route
                else
                {
                    bool routeCleared = true;
                    Train.TCSubpathRoute reqPath = new Train.TCSubpathRoute(trainRoute, thisRouteIndex, otherRouteIndex);

                    for (int iIndex = 0; iIndex < reqPath.Count && routeCleared; iIndex++)
                    {
                        TrackCircuitSection thisSection = signalRef.TrackCircuitList[reqPath[iIndex].TCSectionIndex];
                        if (!thisSection.IsSet(enabledTrain, false))
                        {
                            routeCleared = false;
                            if (!String.IsNullOrEmpty(dumpfile))
                            {
                                var sob = new StringBuilder();
                                sob.AppendFormat("ROUTE_CLEARED_TO_SIGNAL : section {0} is not set for required train \n", thisSection.Index);
                                File.AppendAllText(dumpfile, sob.ToString());
                            }
                        }
                    }

                    if (routeCleared)
                    {
                        routeState = MstsBlockState.CLEAR;
                        if (!String.IsNullOrEmpty(dumpfile))
                        {
                            var sob = new StringBuilder();
                            sob.AppendFormat("ROUTE_CLEARED_TO_SIGNAL : all sections set \n");
                            File.AppendAllText(dumpfile, sob.ToString());
                        }
                    }
                    else if (allowCallOn)
                    {
                        if (enabledTrain.Train.TestCallOn(this, false, reqPath, dumpfile))
                        {
                            routeCleared = true;
                            routeState = MstsBlockState.OCCUPIED;
                            if (!String.IsNullOrEmpty(dumpfile))
                            {
                                var sob = new StringBuilder();
                                sob.AppendFormat("ROUTE_CLEARED_TO_SIGNAL : callon allowed \n");
                                File.AppendAllText(dumpfile, sob.ToString());
                            }
                        }
                    }

                    if (!routeCleared)
                    {
                        routeState = MstsBlockState.JN_OBSTRUCTED;
                        if (!String.IsNullOrEmpty(dumpfile))
                        {
                            var sob = new StringBuilder();
                            sob.AppendFormat("ROUTE_CLEARED_TO_SIGNAL : route not available \n");
                            File.AppendAllText(dumpfile, sob.ToString());
                        }
                    }
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(dumpfile))
                {
                    var sob = new StringBuilder();
                    sob.AppendFormat("ROUTE_CLEARED_TO_SIGNAL : found state : invalid request (no enabled train or invalid signalident) \n");
                    File.AppendAllText(dumpfile, sob.ToString());
                }
            }

            return routeState;
        }

        /// <summary>
        /// Add a lock for a train and a specific subpath (default 0).  This allow the control of this signal by a specific action
        /// </summary>
        public bool LockForTrain(int trainNumber, int subpath = 0)
        {
            KeyValuePair<int, int> newLock = new KeyValuePair<int, int>(trainNumber, subpath);
            LockedTrains.Add(newLock);
            return false;
        }

        public bool UnlockForTrain(int trainNumber, int subpath = 0)
        {
            bool info = LockedTrains.Remove(LockedTrains.First(item => item.Key.Equals(trainNumber) && item.Value.Equals(subpath)));
            return info;
        }

        public bool HasLockForTrain(int trainNumber, int subpath = 0)
        {
            bool info = LockedTrains.Count > 0 && LockedTrains.Exists(item => item.Key.Equals(trainNumber) && item.Value.Equals(subpath));
            return info;
        }

        public bool CleanAllLock(int trainNumber)
        {
            int info = LockedTrains.RemoveAll(item => item.Key.Equals(trainNumber));
            if (info > 0)
                return true;
            return false;
        }


        /// <summary>
        /// Returns 1 if signal has optional head set, 0 if not
        /// </summary>
        public int HasHead(int requiredHeadIndex)
        {
            if (WorldObject == null || WorldObject.HeadsSet == null)
            {
                Trace.TraceInformation("Signal {0} (TDB {1}) has no heads", thisRef, SignalHeads[0].TDBIndex);
                return 0;
            }
            return (requiredHeadIndex < WorldObject.HeadsSet.Length) ? (WorldObject.HeadsSet[requiredHeadIndex] ? 1 : 0) : 0;
        }

        /// <summary>
        /// Increase SignalNumClearAhead from its default value with the value as passed
        /// <summary>
        public void IncreaseSignalNumClearAhead(int requiredIncreaseValue)
        {
            if (SignalNumClearAhead_ORTS > -2)
            {
                SignalNumClearAheadActive = SignalNumClearAhead_ORTS + requiredIncreaseValue;
            }
        }

        /// <summary>
        /// Decrease SignalNumClearAhead from its default value with the value as passed
        /// </summary>
        public void DecreaseSignalNumClearAhead(int requiredDecreaseValue)
        {
            if (SignalNumClearAhead_ORTS > -2)
            {
                SignalNumClearAheadActive = SignalNumClearAhead_ORTS - requiredDecreaseValue;
            }
        }

        /// <summary>
        /// Set SignalNumClearAhead to the value as passed
        /// <summary>
        public void SetSignalNumClearAhead(int requiredValue)
        {
            if (SignalNumClearAhead_ORTS > -2)
            {
                SignalNumClearAheadActive = requiredValue;
            }
        }

        /// <summary>
        /// Reset SignalNumClearAhead to the default value
        /// </summary>
        public void ResetSignalNumClearAhead()
        {
            if (SignalNumClearAhead_ORTS > -2)
            {
                SignalNumClearAheadActive = SignalNumClearAhead_ORTS;
            }
        }

        /// <summary>
        /// Set HOLD state for dispatcher control
        ///
        /// Parameter : bool, if set signal must be reset if set (and train position allows)
        ///
        /// Returned : bool[], dimension 2,
        ///            field [0] : if true, hold state is set
        ///            field [1] : if true, signal is reset (always returns false if reset not requested)
        /// </summary>
        public void RequestHoldSignalDispatcher(bool requestResetSignal)
        {
            MstsSignalAspect thisAspect = this_sig_lr(MstsSignalFunction.NORMAL);

            SetManualCallOn(false);

            // signal not enabled - set lock, reset if cleared (auto signal can clear without enabling)
            if (enabledTrain == null || enabledTrain.Train == null)
            {
                if (thisAspect > MstsSignalAspect.STOP)
                    ResetSignal(true);

                RequestMostRestrictiveAspect();
            }
            // if enabled, cleared and reset not requested : no action
            else if (!requestResetSignal && thisAspect > MstsSignalAspect.STOP)
            {
                RequestMostRestrictiveAspect();
            }
            // if enabled and not cleared : set hold, no reset required
            else if (thisAspect == MstsSignalAspect.STOP)
            {
                RequestMostRestrictiveAspect();
            }
            // enabled, cleared , reset required : check train speed
            // if train is moving : no action
            //temporarily removed by JTang, before the full revision is ready
            //          else if (Math.Abs(enabledTrain.Train.SpeedMpS) > 0.1f)
            //          {
            //          }
            // if train is stopped : reset signal, breakdown train route, set holdstate
            else
            {
                int signalRouteIndex = enabledTrain.Train.ValidRoute[enabledTrain.TrainRouteDirectionIndex].GetRouteIndex(TCNextTC, 0);
                if (signalRouteIndex >= 0)
                {
                    signalRef.BreakDownRouteList(enabledTrain.Train.ValidRoute[enabledTrain.TrainRouteDirectionIndex], signalRouteIndex, enabledTrain);
                    ResetSignal(true);
                }

                RequestMostRestrictiveAspect();
            }
        }

        public void RequestMostRestrictiveAspect()
        {
            holdState = HoldState.ManualLock;
            foreach (var sigHead in SignalHeads)
            {
                sigHead.RequestMostRestrictiveAspect();
            }
        }

        public void RequestApproachAspect()
        {
            holdState = HoldState.ManualApproach;
            foreach (var sigHead in SignalHeads)
            {
                sigHead.RequestApproachAspect();
            }
        }

        public void RequestLeastRestrictiveAspect()
        {
            holdState = HoldState.ManualPass;
            foreach (var sigHead in SignalHeads)
            {
                sigHead.RequestLeastRestrictiveAspect();
            }
        }

        /// <summary>
        /// Reset HOLD state for dispatcher control
        /// </summary>
        public void ClearHoldSignalDispatcher()
        {
            holdState = HoldState.None;
        }

        /// <summary>
        /// Set call on manually from dispatcher
        /// </summary>
        public void SetManualCallOn(bool state)
        {
            if (enabledTrain != null)
            {
                if (state && CallOnEnabled)
                {
                    ClearHoldSignalDispatcher();
                    CallOnManuallyAllowed = true;
                }
                else
                {
                    CallOnManuallyAllowed = false;
                }
            }
        }
    }
}
