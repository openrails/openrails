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
using System.Diagnostics;
using System.IO;
using System.Text;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using ORTS.Common;

namespace Orts.Simulation.Signalling
{
    public class SignalHead
    {
        public SignalType signalType;           // from sigcfg file
        public CsSignalScript usedCsSignalScript = null;
        public SignalScripts.SCRScripts usedSigScript = null;   // used sigscript
        public MstsSignalAspect state = MstsSignalAspect.STOP;
        public string TextSignalAspect = String.Empty;
        public int draw_state;
        public int trItemIndex;                 // Index to trItem   
        public uint TrackJunctionNode;          // Track Junction Node (= 0 if not set)
        public uint JunctionPath;               // Required Junction Path
        public int JunctionMainNode;            // Main node following junction
        public int TDBIndex;                    // Index to TDB Signal Item
        protected ObjectSpeedInfo[] speed_info;    // speed limit info (per aspect)

        public ObjectSpeedInfo CurrentSpeedInfo => SpeedInfoSetBySignalScript ? SignalScriptSpeedInfo : speed_info[(int)state];

        public bool SpeedInfoSetBySignalScript = false;
        public ObjectSpeedInfo SignalScriptSpeedInfo = null; // speed limit info set by C# signal script

        public SignalObject mainSignal;        //  This is the signal which this head forms a part.

        public float? ApproachControlLimitPositionM;
        public float? ApproachControlLimitSpeedMpS;

        public SignalFunction Function { get; protected set; } = SignalFunction.UNKNOWN;

        public int ORTSNormalSubtypeIndex;     // subtype index form sigcfg file

        public string SignalTypeName => signalType?.Name ?? string.Empty;

        public int[] LocalFloats;

        /// <summary>
        /// Constructor for signals
        /// </summary>
        public SignalHead(SignalObject sigObject, int trItem, int TDBRef, SignalItem sigItem)
        {
            mainSignal = sigObject;
            trItemIndex = trItem;
            TDBIndex = TDBRef;

            if (sigItem.NoSigDirs > 0)
            {
                TrackJunctionNode = sigItem.TrSignalDirs[0].TrackNode;
                JunctionPath = sigItem.TrSignalDirs[0].LinkLRPath;
            }

            var sigasp_values = Enum.GetValues(typeof(MstsSignalAspect));
            speed_info = new ObjectSpeedInfo[sigasp_values.Length];
        }

        /// <summary>
        /// Constructor for speedposts
        /// </summary>
        public SignalHead(SignalObject sigObject, int trItem, int TDBRef, SpeedPostItem speedItem)
        {
            mainSignal = sigObject;
            trItemIndex = trItem;
            TDBIndex = TDBRef;
            draw_state = 1;
            state = MstsSignalAspect.CLEAR_2;
            signalType = new SignalType(SignalFunction.SPEED, MstsSignalAspect.CLEAR_2);
            Function = SignalFunction.SPEED;

            var sigasp_values = Enum.GetValues(typeof(MstsSignalAspect));
            speed_info = new ObjectSpeedInfo[sigasp_values.Length];

            float speedMpS = MpS.ToMpS(speedItem.SpeedInd, !speedItem.IsMPH);
            if (speedItem.IsResume)
                speedMpS = 999f;

            float passSpeed = speedItem.IsPassenger ? speedMpS : -1;
            float freightSpeed = speedItem.IsFreight ? speedMpS : -1;
            ObjectSpeedInfo speedinfo = new ObjectSpeedInfo(passSpeed, freightSpeed, false, false, speedItem is TempSpeedPostItem ? (speedMpS == 999f ? 2 : 1) : 0, speedItem.IsWarning);
            speed_info[(int)state] = speedinfo;
        }

        /// <summary>
        /// Set the signal type object from the CIGCFG file
        /// </summary>
        public void SetSignalType(TrItem[] TrItems, SignalConfigurationFile sigCFG)
        {
            if (TrItems[TDBIndex] is SignalItem sigItem)
            {
                // set signal type
                if (sigCFG.SignalTypes.ContainsKey(sigItem.SignalType))
                {
                    // set signal type
                    signalType = sigCFG.SignalTypes[sigItem.SignalType];
                    Function = signalType.Function;

                    // get related signalscript
                    Signals.scrfile.SignalScripts.Scripts.TryGetValue(signalType, out usedSigScript);

                    usedCsSignalScript = Signals.CsSignalScripts.LoadSignalScript(signalType.Script)
                                         ?? Signals.CsSignalScripts.LoadSignalScript(signalType.Name);
                    usedCsSignalScript?.AttachToHead(this);

                    // set signal speeds
                    foreach (SignalAspect thisAspect in signalType.Aspects)
                    {
                        int arrindex = (int)thisAspect.Aspect;
                        speed_info[arrindex] = new ObjectSpeedInfo(thisAspect.SpeedMpS, thisAspect.SpeedMpS, thisAspect.Asap, thisAspect.Reset, thisAspect.NoSpeedReduction ? 1 : 0, false);
                    }

                    // set normal subtype
                    ORTSNormalSubtypeIndex = signalType.ORTSSubtypeIndex;

                    // update overall SignalNumClearAhead

                    if (Function == SignalFunction.NORMAL)
                    {
                        mainSignal.SignalNumClearAhead_MSTS = Math.Max(mainSignal.SignalNumClearAhead_MSTS, signalType.NumClearAhead_MSTS);
                        mainSignal.SignalNumClearAhead_ORTS = Math.Max(mainSignal.SignalNumClearAhead_ORTS, signalType.NumClearAhead_ORTS);
                        mainSignal.SignalNumClearAheadActive = mainSignal.SignalNumClearAhead_ORTS;
                    }

                    // set approach control limits

                    if (signalType.ApproachControlDetails != null)
                    {
                        ApproachControlLimitPositionM = signalType.ApproachControlDetails.ApproachControlPositionM;
                        ApproachControlLimitSpeedMpS = signalType.ApproachControlDetails.ApproachControlSpeedMpS;
                    }
                    else
                    {
                        ApproachControlLimitPositionM = null;
                        ApproachControlLimitSpeedMpS = null;
                    }
                }
                else
                {
                    Trace.TraceWarning("SignalObject trItem={0}, trackNode={1} has SignalHead with undefined SignalType {2}.",
                                  mainSignal.trItem, mainSignal.trackNode, sigItem.SignalType);
                }
            }
        }

        public void Initialize()
        {
            usedCsSignalScript?.Initialize();
        }

        /// <summary>
        ///  Set of methods called per signal head from signal script processing
        ///  All methods link through to the main method set for signal objec
        /// </summary>
        public MstsSignalAspect next_sig_mr(SignalFunction function)
        {
            return mainSignal.next_sig_mr(function);
        }

        public MstsSignalAspect next_sig_lr(SignalFunction function)
        {
            return mainSignal.next_sig_lr(function);
        }

        public MstsSignalAspect this_sig_lr(SignalFunction function)
        {
            return mainSignal.this_sig_lr(function);
        }

        public MstsSignalAspect this_sig_lr(SignalFunction function, ref bool sigfound)
        {
            return mainSignal.this_sig_lr(function, ref sigfound);
        }

        public MstsSignalAspect this_sig_mr(SignalFunction function)
        {
            return mainSignal.this_sig_mr(function);
        }

        public MstsSignalAspect this_sig_mr(SignalFunction function, ref bool sigfound)
        {
            return mainSignal.this_sig_mr(function, ref sigfound);
        }

        public MstsSignalAspect opp_sig_mr(SignalFunction function)
        {
            return mainSignal.opp_sig_mr(function);
        }

        public MstsSignalAspect opp_sig_mr(SignalFunction function, ref SignalObject signalFound) // for debug purposes
        {
            return mainSignal.opp_sig_mr(function, ref signalFound);
        }

        public MstsSignalAspect opp_sig_lr(SignalFunction function)
        {
            return mainSignal.opp_sig_lr(function);
        }

        public MstsSignalAspect opp_sig_lr(SignalFunction function, ref SignalObject signalFound) // for debug purposes
        {
            return mainSignal.opp_sig_lr(function, ref signalFound);
        }

        public MstsSignalAspect next_nsig_lr(SignalFunction function, int nsignals, string dumpfile)
        {
            return mainSignal.next_nsig_lr(function, nsignals, dumpfile);
        }

        public int next_sig_id(SignalFunction function)
        {
            return mainSignal.next_sig_id(function);
        }

        public int next_nsig_id(SignalFunction function, int nsignal)
        {
            return mainSignal.next_nsig_id(function, nsignal);
        }

        public int opp_sig_id(SignalFunction function)
        {
            return mainSignal.opp_sig_id(function);
        }
        public int opp_sig_id_trainpath(SignalFunction function)
        {
            return mainSignal.opp_sig_id_trainpath(function);
        }

        public MstsSignalAspect id_sig_lr(int sigId, SignalFunction function)
        {
            if (sigId >= 0 && sigId < mainSignal.signalRef.SignalObjects.Length)
            {
                SignalObject reqSignal = mainSignal.signalRef.SignalObjects[sigId];
                return reqSignal.this_sig_lr(function);
            }
            return MstsSignalAspect.STOP;
        }

        public int id_sig_enabled(int sigId)
        {
            bool sigEnabled = false;
            if (sigId >= 0 && sigId < mainSignal.signalRef.SignalObjects.Length)
            {
                SignalObject reqSignal = mainSignal.signalRef.SignalObjects[sigId];
                sigEnabled = reqSignal.enabled;
            }
            return sigEnabled ? 1 : 0;
        }

        public void store_lvar(int index, int value)
        {
            mainSignal.store_lvar(index, value);
        }

        public int this_sig_lvar(int index)
        {
            return mainSignal.this_sig_lvar(index);
        }

        public int next_sig_lvar(SignalFunction function, int index)
        {
            return mainSignal.next_sig_lvar(function, index);
        }

        public int id_sig_lvar(int sigId, int index)
        {
            if (sigId >= 0 && sigId < mainSignal.signalRef.SignalObjects.Length)
            {
                SignalObject reqSignal = mainSignal.signalRef.SignalObjects[sigId];
                return reqSignal.this_sig_lvar(index);
            }
            return 0;
        }

        public int next_sig_hasnormalsubtype(int reqSubtype)
        {
            return mainSignal.next_sig_hasnormalsubtype(reqSubtype);
        }

        public int this_sig_hasnormalsubtype(int reqSubtype)
        {
            return mainSignal.this_sig_hasnormalsubtype(reqSubtype);
        }

        public int id_sig_hasnormalsubtype(int sigId, int reqSubtype)
        {
            if (sigId >= 0 && sigId < mainSignal.signalRef.SignalObjects.Length)
            {
                SignalObject reqSignal = mainSignal.signalRef.SignalObjects[sigId];
                return reqSignal.this_sig_hasnormalsubtype(reqSubtype);
            }
            return 0;
        }

        public int switchstand(int aspect1, int aspect2, string dumpfile)
        {
            return mainSignal.switchstand(aspect1, aspect2, dumpfile);
        }

        /// <summary>
        ///  Returns most restrictive state of signal type A, for all type A upto type B
        ///  Uses Most Restricted state per signal, but checks for valid routing
        /// </summary>
        public MstsSignalAspect dist_multi_sig_mr(SignalFunction function1, SignalFunction function2, string dumpfile)
        {
            MstsSignalAspect foundState = MstsSignalAspect.CLEAR_2;
            bool foundValid = false;

            // get signal of type 2 (end signal)

            if (dumpfile.Length > 1)
            {
                File.AppendAllText(dumpfile,
                    String.Format("DIST_MULTI_SIG_MR for {0} + upto {1}\n",
                        function1, function2));
            }

            int sig2Index = mainSignal.sigfound[function2];
            if (sig2Index < 0)           // try renewed search with full route
            {
                sig2Index = mainSignal.SONextSignal(function2);
                mainSignal.sigfound[function2] = sig2Index;
            }

            if (dumpfile.Length > 1)
            {
                if (sig2Index < 0)
                    File.AppendAllText(dumpfile, "  no signal type 2 found\n");
            }

            if (dumpfile.Length > 1)
            {
                var sob = new StringBuilder();
                sob.AppendFormat("  signal type 2 : {0}", mainSignal.sigfound[function2]);

                if (mainSignal.sigfound[function2] > 0)
                {
                    SignalObject otherSignal = mainSignal.signalRef.SignalObjects[mainSignal.sigfound[function2]];
                    sob.AppendFormat(" (");

                    foreach (SignalHead otherHead in otherSignal.SignalHeads)
                    {
                        sob.AppendFormat(" {0} ", otherHead.TDBIndex);
                    }

                    sob.AppendFormat(") ");
                }
                sob.AppendFormat("\n");

                File.AppendAllText(dumpfile, sob.ToString());
            }

            SignalObject thisSignal = mainSignal;

            // ensure next signal of type 1 is located correctly (cannot be done for normal signals searching next normal signal)

            if (!thisSignal.isSignalNormal() || function1 != SignalFunction.NORMAL)
            {
                thisSignal.sigfound[function1] = thisSignal.SONextSignal(function1);
            }

            // loop through all available signals of type 1

            while (thisSignal.sigfound[function1] >= 0)
            {
                thisSignal = thisSignal.signalRef.SignalObjects[thisSignal.sigfound[function1]];

                MstsSignalAspect thisState = thisSignal.this_sig_mr_routed(function1, dumpfile);

                // ensure correct next signals are located
                if (function1 != SignalFunction.NORMAL || !thisSignal.isSignalNormal())
                {
                    var sigFound = thisSignal.SONextSignal(function1);
                    if (sigFound >= 0) thisSignal.sigfound[function1] = thisSignal.SONextSignal(function1);
                }
                if (function2 != SignalFunction.NORMAL || !thisSignal.isSignalNormal())
                {
                    var sigFound = thisSignal.SONextSignal(function2);
                    if (sigFound >= 0) thisSignal.sigfound[function2] = thisSignal.SONextSignal(function2);
                }

                if (sig2Index == thisSignal.thisRef) // this signal also contains type 2 signal and is therefor valid
                {
                    foundValid = true;
                    foundState = foundState < thisState ? foundState : thisState;
                    return foundState;
                }
                else if (sig2Index >= 0 && thisSignal.sigfound[function2] != sig2Index)  // we are beyond type 2 signal
                {
                    return foundValid ? foundState : MstsSignalAspect.STOP;
                }
                foundValid = true;
                foundState = foundState < thisState ? foundState : thisState;
            }

            return foundValid ? foundState : MstsSignalAspect.STOP;   // no type 2 or running out of signals before finding type 2
        }

        /// <summary>
        ///  Returns most restrictive state of signal type A, for all type A upto type B
        ///  Uses Least Restrictive state per signal
        /// </summary>
        public MstsSignalAspect dist_multi_sig_mr_of_lr(SignalFunction function1, SignalFunction function2, string dumpfile)
        {
            MstsSignalAspect foundState = MstsSignalAspect.CLEAR_2;
            bool foundValid = false;

            // get signal of type 2 (end signal)

            if (dumpfile.Length > 1)
            {
                File.AppendAllText(dumpfile,
                    String.Format("DIST_MULTI_SIG_MR_OF_LR for {0} + upto {1}\n",
                        function1, function2));
            }

            int sig2Index = mainSignal.sigfound[function2];
            if (sig2Index < 0)           // try renewed search with full route
            {
                sig2Index = mainSignal.SONextSignal(function2);
                mainSignal.sigfound[function2] = sig2Index;
            }

            if (dumpfile.Length > 1)
            {
                if (sig2Index < 0)
                    File.AppendAllText(dumpfile, "  no signal type 2 found\n");
            }

            if (dumpfile.Length > 1)
            {
                var sob = new StringBuilder();
                sob.AppendFormat("  signal type 2 : {0}", mainSignal.sigfound[function2]);

                if (mainSignal.sigfound[function2] > 0)
                {
                    SignalObject otherSignal = mainSignal.signalRef.SignalObjects[mainSignal.sigfound[function2]];
                    sob.AppendFormat(" (");

                    foreach (SignalHead otherHead in otherSignal.SignalHeads)
                    {
                        sob.AppendFormat(" {0} ", otherHead.TDBIndex);
                    }

                    sob.AppendFormat(") ");
                }
                sob.AppendFormat("\n");

                File.AppendAllText(dumpfile, sob.ToString());
            }

            SignalObject thisSignal = mainSignal;

            // ensure next signal of type 1 is located correctly (cannot be done for normal signals searching next normal signal)

            if (!thisSignal.isSignalNormal() || function1 != SignalFunction.NORMAL)
            {
                thisSignal.sigfound[function1] = thisSignal.SONextSignal(function1);
            }

            // loop through all available signals of type 1

            while (thisSignal.sigfound[function1] >= 0)
            {
                thisSignal = thisSignal.signalRef.SignalObjects[thisSignal.sigfound[function1]];

                MstsSignalAspect thisState = thisSignal.this_sig_lr(function1);
                if (dumpfile.Length > 1)
                {
                    File.AppendAllText(dumpfile, "Found lr state : " + thisState.ToString() + "\n");
                }

                // ensure correct next signals are located
                if (function1 != SignalFunction.NORMAL || !thisSignal.isSignalNormal())
                {
                    var sigFound = thisSignal.SONextSignal(function1);
                    if (sigFound >= 0) thisSignal.sigfound[function1] = thisSignal.SONextSignal(function1);
                }
                if (function2 != SignalFunction.NORMAL || !thisSignal.isSignalNormal())
                {
                    var sigFound = thisSignal.SONextSignal(function2);
                    if (sigFound >= 0) thisSignal.sigfound[function2] = thisSignal.SONextSignal(function2);
                }

                if (sig2Index == thisSignal.thisRef) // this signal also contains type 2 signal and is therefor valid
                {
                    foundValid = true;
                    foundState = foundState < thisState ? foundState : thisState;
                    return foundState;
                }
                else if (sig2Index >= 0 && thisSignal.sigfound[function2] != sig2Index)  // we are beyond type 2 signal
                {
                    return foundValid ? foundState : MstsSignalAspect.STOP;
                }
                foundValid = true;
                foundState = foundState < thisState ? foundState : thisState;
            }

            return foundValid ? foundState : MstsSignalAspect.STOP;   // no type 2 or running out of signals before finding type 2
        }

        /// </summary>
        ///  Return state of requested feature through signal head flags
        /// </summary>
        public bool sig_feature(int feature)
        {
            bool flag_value = true;

            if (mainSignal.WorldObject != null)
            {
                if (feature < mainSignal.WorldObject.FlagsSet.Length)
                {
                    flag_value = mainSignal.WorldObject.FlagsSet[feature];
                }
            }

            return flag_value;
        }

        /// <summary>
        ///  Returns the default draw state for this signal head from the SIGCFG file
        ///  Retruns -1 id no draw state.
        /// </summary>
        public int def_draw_state(MstsSignalAspect state)
        {
            if (signalType != null)
                return signalType.def_draw_state(state);
            else
                return -1;
        }//def_draw_state

        /// <summary>
        ///  Sets the state to the most restrictive aspect for this head.
        /// </summary>
        public void RequestMostRestrictiveAspect()
        {
            if (usedCsSignalScript != null)
            {
                usedCsSignalScript.HandleEvent(SignalEvent.RequestMostRestrictiveAspect);
                usedCsSignalScript.Update();
            }
            else
            {
                if (signalType != null)
                    state = signalType.GetMostRestrictiveAspect();
                else
                    state = MstsSignalAspect.STOP;

                draw_state = def_draw_state(state);
            }
        }

        public void RequestApproachAspect()
        {
            if (usedCsSignalScript != null)
            {
                usedCsSignalScript.HandleEvent(SignalEvent.RequestApproachAspect);
                usedCsSignalScript.Update();
            }
            else
            {
                var drawstate1 = def_draw_state(MstsSignalAspect.APPROACH_1);
                var drawstate2 = def_draw_state(MstsSignalAspect.APPROACH_2);

                if (drawstate1 > 0)
                {
                    state = MstsSignalAspect.APPROACH_1;
                }
                else if (drawstate2 > 0)
                {
                    state = MstsSignalAspect.APPROACH_2;
                }
                else
                {
                    state = MstsSignalAspect.APPROACH_3;
                }

                draw_state = def_draw_state(state);
            }
        }

        /// <summary>
        ///  Sets the state to the least restrictive aspect for this head.
        /// </summary>
        public void RequestLeastRestrictiveAspect()
        {
            if (usedCsSignalScript != null)
            {
                usedCsSignalScript.HandleEvent(SignalEvent.RequestLeastRestrictiveAspect);
                usedCsSignalScript.Update();
            }
            else
            {
                if (signalType != null)
                    state = signalType.GetLeastRestrictiveAspect();
                else
                    state = MstsSignalAspect.CLEAR_2;

                draw_state = def_draw_state(state);
            }
        }

        /// <summary>
        ///  check if linked route is set
        /// </summary>
        public int route_set()
        {
            bool juncfound = true;

            // call route_set routine from main signal

            if (TrackJunctionNode > 0)
            {
                juncfound = mainSignal.route_set(JunctionMainNode, TrackJunctionNode);
            }
            //added by JTang
            else if (MPManager.IsMultiPlayer())
            {
                var node = mainSignal.signalRef.trackDB.TrackNodes[mainSignal.trackNode];
                if (node.TrJunctionNode == null && node.TrPins != null && mainSignal.TCDirection < node.TrPins.Length)
                {
                    node = mainSignal.signalRef.trackDB.TrackNodes[node.TrPins[mainSignal.TCDirection].Link];
                    if (node.TrJunctionNode == null) return 0;
                    for (var pin = node.Inpins; pin < node.Inpins + node.Outpins; pin++)
                    {
                        if (node.TrPins[pin].Link == mainSignal.trackNode && pin - node.Inpins != node.TrJunctionNode.SelectedRoute)
                        {
                            juncfound = false;
                            break;
                        }
                    }
                }
            }
            if (juncfound)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        ///  Default update process
        /// </summary>
        public void Update()
        {
            if (usedCsSignalScript is CsSignalScript)
            {
                usedCsSignalScript.Update();
            }
            else
            {
                SIGSCRfile.SH_update(this, Signals.scrfile);
            }
        }
    }
}
