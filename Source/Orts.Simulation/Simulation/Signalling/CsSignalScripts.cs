using System.IO;

namespace Orts.Simulation.Signalling
{
    public class CsSignalScripts
    {
        readonly Simulator Simulator;

        public CsSignalScripts(Simulator simulator)
        {
            Simulator = simulator;
        }

        public bool ScriptFileExists(string scriptName)
        {
            string path = Path.Combine(Simulator.RoutePath, "Script", "Signal", scriptName + ".cs");

            return File.Exists(path);
        }

        public CsSignalScript LoadSignalScript(string scriptName)
        {
            var pathArray = new string[] { Path.Combine(Simulator.RoutePath, "Script", "Signal") };

            return Simulator.ScriptManager.Load(pathArray, scriptName, "Orts.Simulation.Signalling") as CsSignalScript;
        }
    }
}
