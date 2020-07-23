using Orts.Formats.Msts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Simulation.Signalling
{
    // The C# signal script is supposed to be used on routes where all signals are using C# scripts.
    // The exchange of information is done through the TextSignalAspect property.
    // The MSTS signal aspect is only used for TCS scripts that do not support TextSignalAspect.
    // The MSTS SIGSCR functions are still usable through the SignalObject and SignalHead objects.
    public abstract class CsSignalScript
    {
        // References
        public SignalHead SignalHead { get; set; }
        public SignalObject SignalObject => SignalHead.mainSignal;

        // Aliases
        public MstsSignalAspect MstsSignalAspect { get => SignalHead.state; protected set => SignalHead.state = value; }
        public string TextSignalAspect { get => SignalHead.TextSignalAspect; protected set => SignalHead.TextSignalAspect = value; }
        public int DrawState { get => SignalHead.draw_state; protected set => SignalHead.draw_state = value; }
        public bool Enabled => SignalObject.enabled;
        public float? ApproachControlRequiredPosition => SignalHead.ApproachControlLimitPositionM.Value;
        public float? ApproachControlRequiredSpeed => SignalHead.ApproachControlLimitSpeedMpS.Value;
        public MstsBlockState BlockState => SignalObject.block_state();
        public bool RouteSet => SignalHead.route_set() > 0;
        public int DefaultDrawState(MstsSignalAspect signalAspect) => SignalHead.def_draw_state(signalAspect);

        public CsSignalScript()
        {
        }

        public abstract void Initialize();

        public abstract void Update();

        public SignalObject NextSignalObject(MstsSignalFunction signalFunction)
        {
            return NextSignalObjects(signalFunction, 1).FirstOrDefault();
        }

        public List<SignalObject> NextSignalObjects(MstsSignalFunction signalFunction, uint number)
        {
            // Sanity check
            if (number > 20)
            {
                number = 20;
            }

            int signalFunctionInt = Convert.ToInt32(signalFunction);

            List<SignalObject> signalObjects = new List<SignalObject>();
            SignalObject nextSignalObject = SignalHead.mainSignal;

            while (signalObjects.Count < number)
            {
                int nextSignal = nextSignalObject.next_sig_id(signalFunctionInt);

                // signal found : get state
                if (nextSignal >= 0)
                {
                    nextSignalObject = SignalObject.signalObjects[nextSignal];
                    signalObjects.Add(nextSignalObject);
                }
                else
                {
                    break;
                }
            }

            return signalObjects;
        }

        public List<string> GetThisSignalTextAspects(MstsSignalFunction signalFunction)
        {
            return SignalHead.mainSignal.GetAllTextSignalAspects(signalFunction);
        }

        public List<string> GetNextSignalTextAspects(MstsSignalFunction signalFunction)
        {
            SignalObject nextSignal = NextSignalObject(signalFunction);
            List<string> result = nextSignal?.GetAllTextSignalAspects(signalFunction) ?? new List<string>();

            return result;
        }

        public bool IsSignalFeatureEnabled(string signalFeature)
        {
            int signalFeatureIndex = SignalShape.SignalSubObj.SignalSubTypes.IndexOf(signalFeature);

            return SignalHead.sig_feature(signalFeatureIndex);
        }
    }
}
