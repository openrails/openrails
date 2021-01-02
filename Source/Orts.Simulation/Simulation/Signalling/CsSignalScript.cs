using Orts.Formats.Msts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Simulation.Signalling
{
    // The exchange of information is done through the TextSignalAspect property.
    // The MSTS signal aspect is only used for TCS scripts that do not support TextSignalAspect.
    public abstract class CsSignalScript
    {
        // References and shortcuts. Must be private to not expose them through the API
        private SignalHead SignalHead { get; set; }
        private SignalObject SignalObject => SignalHead.mainSignal;
        private int SigFnIndex(string sigFn) => SignalObject.signalRef.ORTSSignalTypes.IndexOf(sigFn);
        private SignalObject SignalObjectById(int id) => SignalObject.signalRef.SignalObjects[id];

        // Public interface
        public string DebugFileName = String.Empty;
        public MstsSignalAspect MstsSignalAspect { get => SignalHead.state; protected set => SignalHead.state = value; }
        public string TextSignalAspect { get => SignalHead.TextSignalAspect; protected set => SignalHead.TextSignalAspect = value; }
        public int DrawState { get => SignalHead.draw_state; protected set => SignalHead.draw_state = value; }
        public bool Enabled => SignalObject.enabled;
        public float? ApproachControlRequiredPosition => SignalHead.ApproachControlLimitPositionM.Value;
        public float? ApproachControlRequiredSpeed => SignalHead.ApproachControlLimitSpeedMpS.Value;
        public MstsBlockState BlockState => SignalObject.block_state();
        public bool RouteSet => SignalHead.route_set() > 0;
        public bool AllowClearPartialRoute { set { SignalObject.AllowClearPartialRoute(value ? 1 : 0); } }
        public int SignalNumClearAhead { get { return SignalObject.SignalNumClearAheadActive; } set { SignalObject.SetSignalNumClearAhead(value); } }
        public int DefaultDrawState(MstsSignalAspect signalAspect) => SignalHead.def_draw_state(signalAspect);
        public int SignalId => SignalObject.thisRef;
        public Dictionary<int, int> SharedVariables => SignalObject.localStorage;
        public float ClockTimeS => (float)SignalObject.signalRef.Simulator.GameTime;
        public class Timer
        {
            float EndValue;
            protected Func<float> CurrentValue;
            public Timer(CsSignalScript script)
            {
                CurrentValue = () => script.ClockTimeS;
            }
            public float AlarmValue { get; private set; }
            public float RemainingValue { get { return EndValue - CurrentValue(); } }
            public bool Started { get; private set; }
            public void Setup(float alarmValue) { AlarmValue = alarmValue; }
            public void Start() { EndValue = CurrentValue() + AlarmValue; Started = true; }
            public void Stop() { Started = false; }
            public bool Triggered { get { return Started && CurrentValue() >= EndValue; } }
        }
        public CsSignalScript()
        {
        }
        public void SendSignalMessage(int signalId, string message)
        {
            Console.WriteLine("Message to " + signalId + ": " + message);
            if (signalId < 0 || signalId > SignalObject.signalRef.SignalObjects.Length) return;
            foreach (var head in SignalObjectById(signalId).SignalHeads)
            {
                head.usedCsSignalScript?.HandleSignalMessage(SignalObject.thisRef, message);
            }
        }
        public bool IsSignalFeatureEnabled(string signalFeature)
        {
            int signalFeatureIndex = SignalShape.SignalSubObj.SignalSubTypes.IndexOf(signalFeature);

            return SignalHead.sig_feature(signalFeatureIndex);
        }

        public int NextSignalId(string sigfn, int count = 0)
        {
            return SignalObject.next_nsig_id(SigFnIndex(sigfn), count + 1);
        }
        public int OppositeSignalId(string sigfn)
        {
            return SignalObject.opp_sig_id(SigFnIndex(sigfn));
        }
        public int RequiredNormalSignalId(string normalSubtype)
        {
            return SignalObject.FindReqNormalSignal(SignalObject.signalRef.ORTSNormalsubtypes.IndexOf(normalSubtype), DebugFileName);
        }
        public bool IdSignalHasNormalSubtype(int id, string normalSubtype)
        {
            return SignalHead.id_sig_hasnormalsubtype(id, SignalObject.signalRef.ORTSNormalsubtypes.IndexOf(normalSubtype)) == 1;
        }
        public string IdTextSignalAspect(int id, string sigfn, int headindex=0)
        {
            if (id < 0 || id > SignalObject.signalRef.SignalObjects.Length) return String.Empty;
            foreach (var head in SignalObjectById(id).SignalHeads)
            {
                if (head.ORTSsigFunctionIndex == SigFnIndex(sigfn))
                {
                    if (headindex <= 0) return head.TextSignalAspect;
                    headindex--;
                }
            }
            return String.Empty;
        }
        public MstsSignalAspect DistMultiSigMR(string sigfnA, string sigfnB, bool mostRestrictiveHead = true)
        {
            if(mostRestrictiveHead) return SignalHead.dist_multi_sig_mr(SigFnIndex(sigfnA), SigFnIndex(sigfnB), DebugFileName);
            return SignalHead.dist_multi_sig_mr_of_lr(SigFnIndex(sigfnA), SigFnIndex(sigfnB), DebugFileName);
        }
        public MstsSignalAspect IdSignalAspect(int id, string sigfn, bool mostRestrictive = false)
        {
            return SignalHead.id_sig_lr(id, SigFnIndex(sigfn));
        }
        public int IdSignalLocalVariable(int id, int key)
        {
            return SignalHead.id_sig_lvar(id, key);
        }
        public bool IdSignalEnabled(int id)
        {
            return SignalHead.id_sig_enabled(id) > 0;
        }
        public bool TrainHasCallOn(bool allowOnNonePlatform = true, bool allowAdvancedSignal = false)
        {
            return SignalObject.TrainHasCallOn(allowOnNonePlatform, allowAdvancedSignal, DebugFileName);
        }
        public bool TrainRequiresSignal(int signalId, float reqPositionM)
        {
            return SignalObject.RequiresNextSignal(signalId, (int)reqPositionM, DebugFileName);
        }
        public bool ApproachControlPosition(float reqPositionM, bool forced = false)
        {
            return SignalObject.ApproachControlPosition((int)reqPositionM, DebugFileName, forced);
        }
        public bool ApproachControlSpeed(float reqPositionM, float reqSpeedMpS)
        {
            return SignalObject.ApproachControlSpeed((int)reqPositionM, (int)reqSpeedMpS, DebugFileName);
        }
        public bool ApproachControlNextStop(float reqPositionM, float reqSpeedMpS)
        {
            return SignalObject.ApproachControlNextStop((int)reqPositionM, (int)reqSpeedMpS, DebugFileName);
        }
        public void ApproachControlLockClaim()
        {
            SignalObject.LockClaim();
        }
        public MstsBlockState RouteClearedToSignal(int signalId, bool allowCallOn = false)
        {
            return SignalObject.RouteClearedToSignal(signalId, allowCallOn, DebugFileName);
        }
        internal void AttachToHead(SignalHead signalHead)
        {
            SignalHead = signalHead;
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
        /// <returns></returns>
        public virtual void HandleSignalMessage(int signalId, string message) {}
    }
}
