using System.IO;
using System.Reflection;

namespace Orts.Simulation.Signalling
{
    public class CsSignalScripts
    {
        readonly Simulator Simulator;

        Assembly ScriptAssembly;

        public CsSignalScripts(Simulator simulator)
        {
            Simulator = simulator;
            ScriptAssembly = Simulator.ScriptManager.LoadFolder(Path.Combine(Simulator.RoutePath, "Script", "Signal"));
        }

        public CsSignalScript LoadSignalScript(string scriptName)
        {
            if (ScriptAssembly == null) return null;
            var type = string.Format("{0}.{1}", "ORTS.Scripting.Script", scriptName);
            return ScriptAssembly.CreateInstance(type, true) as CsSignalScript;
        }
    }
}
