using Orts.Formats.Msts;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orts.Simulation.Signalling
{
    public enum SignalEvent
    {
        RequestMostRestrictiveAspect,
        RequestApproachAspect,
        RequestLeastRestrictiveAspect,
    }

    // The exchange of information is done through the TextSignalAspect property.
    // The MSTS signal aspect is only used for TCS scripts that do not support TextSignalAspect.
    public abstract class CsSignalScript : AbstractScriptClass
    {
        // References and shortcuts. Must be private to not expose them through the API
        private SignalHead SignalHead { get; set; }
        private SignalObject SignalObject => SignalHead.mainSignal;
        private SignalFunction SignalFunction(string sigFn) => SignalObject.signalRef.SignalFunctions[sigFn];

        private SignalObject SignalObjectById(int id)
        {
            if (id >= 0 && id < SignalObject.signalRef.SignalObjects.Length)
            {
                return SignalObject.signalRef.SignalObjects[id];
            }
            else
            {
                return null;
            }
        }

        // Public interface
        /// <summary>
        /// File name where debugging information provided by signalling functions will be inserted
        /// </summary>
        public string DebugFileName = String.Empty;
        public enum Aspect
        {
            /// <summary>Stop (absolute)</summary>
            Stop,
            /// <summary>Stop and proceed</summary>
            StopAndProceed,
            /// <summary>Restricting</summary>
            Restricting,
            /// <summary>Final caution before 'stop' or 'stop and proceed'</summary>
            Approach_1,
            /// <summary>Advanced caution</summary>
            Approach_2,
            /// <summary>Least restrictive advanced caution</summary>
            Approach_3,
            /// <summary>Clear to next signal</summary>
            Clear_1,
            /// <summary>Clear to next signal (least restrictive)</summary>
            Clear_2
        }
        public enum BlockState
        {
            /// <summary>Block ahead is clear and accesible</summary>
            Clear,
            /// <summary>Block ahead is occupied by one or more wagons/locos not moving in opposite direction</summary>
            Occupied,
            /// <summary>Block ahead is impassable due to the state of a switch or occupied by moving train or not accesible</summary>
            Obstructed
        }
        /// <summary>
        /// Represents one of the eight aspects used for display purposes, AI traffic and SIGSCR signalling
        /// </summary>
        public Aspect MstsSignalAspect { get => (Aspect)SignalHead.state; protected set => SignalHead.state = (MstsSignalAspect)value; }
        /// <summary>
        /// Custom aspect of the signal
        /// </summary>
        public string TextSignalAspect { get => SignalHead.TextSignalAspect; protected set => SignalHead.TextSignalAspect = value; }
        /// <summary>
        /// Draw State that the signal object will show (i. e. active lights and semaphore position)
        /// </summary>
        public int DrawState { get => SignalHead.draw_state; protected set => SignalHead.draw_state = value; }
        /// <summary>
        /// True if a train is approaching the signal
        /// </summary>
        public bool Enabled => SignalObject.enabled;
        /// <summary>
        /// Distance at which the signal will be cleared during approach control
        /// </summary>
        public float? ApproachControlRequiredPosition => SignalHead.ApproachControlLimitPositionM;
        /// <summary>
        /// Maximum train speed at which the signal will be cleared during approach control
        /// </summary>
        public float? ApproachControlRequiredSpeed => SignalHead.ApproachControlLimitSpeedMpS;
        /// <summary>
        /// Occupation and reservation state of the signal's route
        /// </summary>
        public BlockState CurrentBlockState => (BlockState)SignalObject.block_state();
        /// <summary>
        /// True if the signal link is activated
        /// </summary>
        public bool RouteSet => SignalHead.route_set() > 0;
        /// <summary>
        /// Hold state of the signal
        /// </summary>
        public HoldState HoldState => SignalObject.holdState;
        /// <summary>
        /// Set this variable to true to allow clear to partial route
        /// </summary>
        public bool AllowClearPartialRoute { set { SignalObject.AllowClearPartialRoute(value ? 1 : 0); } }
        /// <summary>
        /// Number of signals to clear ahead of this signal
        /// </summary>
        public int SignalNumClearAhead { get { return SignalObject.SignalNumClearAheadActive; } set { SignalObject.SetSignalNumClearAhead(value); } }
        /// <summary>
        /// Default draw state of the signal for a specific aspect
        /// </summary>
        /// <param name="signalAspect">Aspect for which the default draw state must be found</param>
        /// <returns></returns>
        public int DefaultDrawState(Aspect signalAspect) => SignalHead.def_draw_state((MstsSignalAspect)signalAspect);

        /// <summary>
        /// Index of the draw state with the specified name
        /// </summary>
        /// <param name="name">Name of the draw state as defined in sigcfg</param>
        /// <returns>The index of the draw state, -1 if no one exist with that name</returns>
        public int GetDrawState(string name) => SignalHead.signalType.DrawStates.TryGetValue(name.ToLower(), out SignalDrawState drawState) ? drawState.Index : -1;

        /// <summary>
        /// Signal identity of this signal
        /// </summary>
        public int SignalId => SignalObject.thisRef;
        /// <summary>
        /// Name of this signal type, as defined in sigcfg
        /// </summary>
        public string SignalTypeName => SignalHead.SignalTypeName;
        /// <summary>
        /// Name of the signal shape, as defined in sigcfg
        /// </summary>
        public string SignalShapeName => Path.GetFileNameWithoutExtension(SignalObject.WorldObject.SFileName);
        /// <summary>
        /// Local storage of this signal, which can be accessed from other signals
        /// </summary>
        public Dictionary<int, int> SharedVariables => SignalObject.localStorage;
        public CsSignalScript()
        {
        }
        /// <summary>
        /// Sends a message to the specified signal
        /// </summary>
        /// <param name="signalId">Id of the signal to which the message shall be sent</param>
        /// <param name="message">Message to send</param>
        public void SendSignalMessage(int signalId, string message)
        {
            var heads = SignalObjectById(signalId)?.SignalHeads;
            if (heads != null)
            {
                foreach (SignalHead head in heads)
                    head.usedCsSignalScript?.HandleSignalMessage(SignalObject.thisRef, message);
            }
        }
        /// <summary>
        /// Check if this signal has a specific feature
        /// </summary>
        /// <param name="signalFeature">Name of the requested feature</param>
        /// <returns></returns>
        public bool IsSignalFeatureEnabled(string signalFeature)
        {
            int signalFeatureIndex = SignalShape.SignalSubObj.SignalSubTypes.IndexOf(signalFeature);

            return SignalHead.sig_feature(signalFeatureIndex);
        }
        /// <summary>
        /// Checks if the signal has a specific head
        /// </summary>
        /// <param name="requiredHeadIndex">Index of the required head</param>
        /// <returns>True if the required head is present</returns>
        public bool HasHead(int requiredHeadIndex)
        {
            return SignalObject.HasHead(requiredHeadIndex) == 1;
        }
        /// <summary>
        /// Get id of next signal
        /// </summary>
        /// <param name="sigfn">Signal function of the required signal</param>
        /// <param name="count">Get id of nth signal (first is 0)</param>
        /// <returns>Id of required signal</returns>
        public int NextSignalId(string sigfn, int count = 0)
        {
            return SignalObject.next_nsig_id(SignalFunction(sigfn), count + 1);
        }
        /// <summary>
        /// Get id of opposite signal
        /// </summary>
        /// <param name="sigfn">Signal function of the required signal</param>
        /// <returns></returns>
        public int OppositeSignalId(string sigfn)
        {
            return SignalObject.opp_sig_id(SignalFunction(sigfn));
        }
        /// <summary>
        /// Find next normal signal of a specific subtype
        /// </summary>
        /// <param name="normalSubtype">Required normal subtype</param>
        /// <returns>Id of required signal</returns>
        public int RequiredNormalSignalId(string normalSubtype)
        {
            return SignalObject.FindReqNormalSignal(SignalObject.signalRef.ORTSNormalsubtypes.IndexOf(normalSubtype), DebugFileName);
        }
        /// <summary>
        /// Check if required signal has a normal subtype
        /// </summary>
        /// <param name="id">Id of the signal</param>
        /// <param name="normalSubtype">Normal subtype to test</param>
        public bool IdSignalHasNormalSubtype(int id, string normalSubtype)
        {
            return SignalHead.id_sig_hasnormalsubtype(id, SignalObject.signalRef.ORTSNormalsubtypes.IndexOf(normalSubtype)) == 1;
        }

        /// <summary>
        /// Get the text aspect of a specific signal
        /// </summary>
        /// <param name="id">Id of the signal to query</param>
        /// <param name="sigfn">Consider only heads with a specific signal function</param>
        /// <param name="headindex">Get aspect of nth head of the specified type</param>
        public string IdTextSignalAspect(int id, string sigfn, int headindex=0)
        {
            if (SignalObject.signalRef.SignalFunctions.TryGetValue(sigfn, out SignalFunction function))
            {
                var heads = SignalObjectById(id)?.SignalHeads;
                if (heads != null)
                {
                    foreach (SignalHead head in heads)
                    {
                        if (head.Function != function) continue;
                        if (headindex <= 0) return head.TextSignalAspect;
                        headindex--;
                    }
                }
            }

            return "";
        }

        /// <summary>
        /// Obtains the most restrictive aspect of the signals of type A up to the first signal of type B
        /// </summary>
        /// <param name="sigfnA">Signals to search</param>
        /// <param name="sigfnB">Signal type where search is stopped</param>
        /// <param name="mostRestrictiveHead">Check most restrictive head per signal</param>
        public Aspect DistMultiSigMR(string sigfnA, string sigfnB, bool mostRestrictiveHead = true)
        {
            if(mostRestrictiveHead) return (Aspect)SignalHead.dist_multi_sig_mr(SignalFunction(sigfnA), SignalFunction(sigfnB), DebugFileName);
            return (Aspect)SignalHead.dist_multi_sig_mr_of_lr(SignalFunction(sigfnA), SignalFunction(sigfnB), DebugFileName);
        }

        /// <summary>
        /// Get aspect of required signal
        /// </summary>
        /// <param name="id">Id of required signal</param>
        /// <param name="sigfn">Function of the signal heads to consider</param>
        /// <param name="mostRestrictive">Get most restrictive instead of least restrictive</param>
        /// <param name="checkRouting">If looking for most restrictive aspect, consider only heads with the route link activated</param>
        public Aspect IdSignalAspect(int id, string sigfn, bool mostRestrictive = false, bool checkRouting = false)
        {
            if (!mostRestrictive) return (Aspect)SignalHead.id_sig_lr(id, SignalFunction(sigfn));
            else if (id >= 0 && id < SignalObject.signalRef.SignalObjects.Length)
            {
                var signal = SignalObjectById(id);
                if (checkRouting) return (Aspect)signal.this_sig_mr_routed(SignalFunction(sigfn), DebugFileName);
                else return (Aspect)signal.this_sig_mr(SignalFunction(sigfn));
            }
            return Aspect.Stop;
        }

        /// <summary>
        /// Get local variable of the required signal
        /// </summary>
        /// <param name="id">Id of the signal to get local variable from</param>
        /// <param name="key">Key of the variable</param>
        /// <returns>The value of the required variable</returns>
        public int IdSignalLocalVariable(int id, int key)
        {
            return SignalHead.id_sig_lvar(id, key);
        }
        /// <summary>
        /// Check if signal is enabled
        /// </summary>
        /// <param name="id">Id of the signal to check</param>
        public bool IdSignalEnabled(int id)
        {
            return SignalHead.id_sig_enabled(id) > 0;
        }
        /// <summary>
        /// Check if train has 'call on' set
        /// </summary>
        /// <param name="allowOnNonePlatform">Allow Call On without platform</param>
        /// <param name="allowAdvancedSignal">Allow Call On even if this is not first signal for train</param>
        /// <returns>True if train is allowed to call on with the required parameters, false otherwise</returns>
        public bool TrainHasCallOn(bool allowOnNonePlatform = true, bool allowAdvancedSignal = false)
        {
            SignalObject.CallOnEnabled = true;
            return SignalObject.TrainHasCallOn(allowOnNonePlatform, allowAdvancedSignal, DebugFileName);
        }
        /// <summary>
        /// Test if train requires next signal
        /// </summary>
        /// <param name="signalId">Id of the signal to be tested</param>
        /// <param name="reqPosition">1 if next track circuit after required signal is checked, 0 if not</param>
        public bool TrainRequiresSignal(int signalId, int reqPosition)
        {
            return SignalObject.RequiresNextSignal(signalId, reqPosition, DebugFileName);
        }
        /// <summary>
        /// Checks if train is closer than required position from the signal
        /// </summary>
        /// <param name="reqPositionM">Maximum distance to activate approach control</param>
        /// <param name="forced">Activate approach control even if this is not the first signal for the train</param>
        /// <returns>True if approach control is set</returns>
        public bool ApproachControlPosition(float reqPositionM, bool forced = false)
        {
            return SignalObject.ApproachControlPosition((int)reqPositionM, DebugFileName, forced);
        }
        /// <summary>
        /// Checks if train is closer than required distance to the signal, and if it is running at lower speed than specified
        /// </summary>
        /// <param name="reqPositionM">Maximum distance to activate approach control</param>
        /// <param name="reqSpeedMpS">Maximum speed at which approach control will be set</param>
        /// <returns>True if the conditions are fulfilled, false otherwise</returns>
        public bool ApproachControlSpeed(float reqPositionM, float reqSpeedMpS)
        {
            return SignalObject.ApproachControlSpeed((int)reqPositionM, (int)reqSpeedMpS, DebugFileName);
        }
        /// <summary>
        /// Checks if train is closer than required distance to the signal, and if it is running at lower speed than specified,
        /// in case of APC in next STOP
        /// </summary>
        /// <param name="reqPositionM">Maximum distance to activate approach control</param>
        /// <param name="reqSpeedMpS">Maximum speed at which approach control will be set</param>
        /// <returns>True if the conditions are fulfilled, false otherwise</returns>
        public bool ApproachControlNextStop(float reqPositionM, float reqSpeedMpS)
        {
            return SignalObject.ApproachControlNextStop((int)reqPositionM, (int)reqSpeedMpS, DebugFileName);
        }
        /// <summary>
        /// Lock claim (only if approach control is active)
        /// </summary>
        public void ApproachControlLockClaim()
        {
            SignalObject.LockClaim();
        }
        /// <summary>
        /// Checks if the route is cleared up to the required signal
        /// </summary>
        /// <param name="signalId">Id of the signal where check is stopped</param>
        /// <param name="allowCallOn">Consider route as cleared if train has call on</param>
        /// <returns>The state of the route from this signal to the required signal</returns>
        public BlockState RouteClearedToSignal(int signalId, bool allowCallOn = false)
        {
            return (BlockState)SignalObject.RouteClearedToSignal(signalId, allowCallOn, DebugFileName);
        }

        /// <summary>
        /// Internally called to assign this instance to a signal head
        /// </summary>
        internal void AttachToHead(SignalHead signalHead)
        {
            SignalHead = signalHead;

            // Build AbstractScriptClass API functions
            ClockTime = () => (float)SignalObject.signalRef.Simulator.ClockTime;
            GameTime = () => (float)SignalObject.signalRef.Simulator.GameTime;
            PreUpdate = () => SignalObject.signalRef.Simulator.PreUpdate;
        }

        // Functions to be implemented in script

        /// <summary>
        /// Called once at initialization time
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Called regularly during the simulation
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Called when a signal sends a message to this signal
        /// </summary>
        /// <param name="signalId">Signal ID of the calling signal</param>
        /// <param name="message">Message sent to signal</param>
        public virtual void HandleSignalMessage(int signalId, string message) {}

        /// <summary>
        /// Called when the simulator
        /// </summary>
        /// <param name="evt"></param>
        public virtual void HandleEvent(SignalEvent evt, string message = "") { }

        /// <summary>
        /// Indicates if the signal script has taken control over the speed limit (overrides the speed limit set in the SignalType).
        /// </summary>
        public bool SpeedLimitSetByScript { get => SignalHead.SpeedInfoSetBySignalScript; set => SignalHead.SpeedInfoSetBySignalScript = value; }

        /// <summary>
        /// Returns the parameters of the speed limit set by the script.
        /// </summary>
        /// <returns>A tuple containing the parameters of the speed limit, or null if the speed limit is not set</returns>
        public (float PassengerSpeedLimitMpS, float FreightSpeedLimitMpS, bool Asap, bool Reset, bool NoSpeedReduction, bool IsWarning)? SpeedLimit()
        {
            if (SignalHead.SignalScriptSpeedInfo != null)
            {
                return (SignalHead.SignalScriptSpeedInfo.speed_pass,
                        SignalHead.SignalScriptSpeedInfo.speed_freight,
                        SignalHead.SignalScriptSpeedInfo.speed_flag != 0,
                        SignalHead.SignalScriptSpeedInfo.speed_reset != 0,
                        SignalHead.SignalScriptSpeedInfo.speed_noSpeedReductionOrIsTempSpeedReduction != 0,
                        SignalHead.SignalScriptSpeedInfo.speed_isWarning);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets a speed limit for the signal head.
        /// </summary>
        /// <param name="passengerSpeedLimitMpS">The speed limit for passenger trains in meters per second.</param>
        /// <param name="freightSpeedLimitMpS">The speed limit for freight trains in meters per second.</param>
        /// <param name="asap">True if an AI train must apply this speed limit as soon as possible.</param>
        /// <param name="reset">True if the speed limit is set to the train's maximum speed.</param>
        /// <param name="noSpeedReduction">True if an AI train shouldn't approach the signal slowly for STOP_AND_PROCEED and RESTRICTED aspects.</param>
        /// <param name="isWarning">True if this signal head warns about a speed limit on a following signal.</param>
        public void SetSpeedLimit(float passengerSpeedLimitMpS, float freightSpeedLimitMpS, bool asap, bool reset, bool noSpeedReduction, bool isWarning)
        {
            if (SignalHead.SignalScriptSpeedInfo == null)
            {
                SignalHead.SignalScriptSpeedInfo = new ObjectSpeedInfo(passengerSpeedLimitMpS, freightSpeedLimitMpS, asap, reset, noSpeedReduction ? 1 : 0, isWarning);
            }
            else
            {
                SignalHead.SignalScriptSpeedInfo.speed_pass = passengerSpeedLimitMpS;
                SignalHead.SignalScriptSpeedInfo.speed_freight = freightSpeedLimitMpS;
                SignalHead.SignalScriptSpeedInfo.speed_flag = asap ? 1 : 0;
                SignalHead.SignalScriptSpeedInfo.speed_reset = reset ? 1 : 0;
                SignalHead.SignalScriptSpeedInfo.speed_noSpeedReductionOrIsTempSpeedReduction = noSpeedReduction ? 1 : 0;
                SignalHead.SignalScriptSpeedInfo.speed_isWarning = isWarning;
            }
        }

        /// <summary>
        /// Removes the speed limit set by the signal script.
        /// The signal head will now apply the speed limit of the 
        /// </summary>
        public void RemoveSpeedLimit()
        {
            if (SignalHead.SignalScriptSpeedInfo != null)
            {
                SignalHead.SignalScriptSpeedInfo = null;
            }
        }
    }
}
