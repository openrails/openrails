// COPYRIGHT 2014 by the Open Rails project.
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using ORTS.Processes;

namespace ORTS.Scripting
{
    [CallOnThread("Loader")]
    public class ScriptManager
    {
        readonly Simulator Simulator;
        readonly Dictionary<string, Assembly> Scripts = new Dictionary<string, Assembly>();
        static readonly CSharpCodeProvider Compiler = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } }); 
        static readonly CompilerParameters CompilerParameters = new CompilerParameters();

        [CallOnThread("Loader")]
        internal ScriptManager(Simulator simulator)
        {
            Simulator = simulator;
            CompilerParameters.GenerateExecutable = false;
            CompilerParameters.GenerateInMemory = true;
            //CompilerParameters.IncludeDebugInformation = true;
            //CompilerParameters.CompilerOptions += " /debug:pdbonly"; 
            CompilerParameters.ReferencedAssemblies.Add("System.dll");
            CompilerParameters.ReferencedAssemblies.Add("System.Core.dll");
            CompilerParameters.ReferencedAssemblies.Add("ORTS.Common.dll");
            CompilerParameters.ReferencedAssemblies.Add("RunActivity.exe");
        }

        public object Load(string[] pathArray, string name)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("SharedScriptManager.Load incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            if (pathArray == null || pathArray.Length == 0 || name == null || name == "")
                return null;

            if (Path.GetExtension(name) != ".cs")
                name += ".cs";

            var path = ORTSPaths.GetFileFromFolders(pathArray, name);

            if (path == null || path == "")
                return null;
            
            path = path.ToLowerInvariant();

            var type = String.Format("ORTS.Scripting.Script.{0}", Path.GetFileNameWithoutExtension(path));

            if (Scripts.ContainsKey(path))
                return Scripts[path].CreateInstance(type, true);

            try
            {
                var sourceCode = new StringBuilder();
                var prefixLines = 0;
                using (var sr = new StreamReader(path))
                {
                    sourceCode.Append(sr.ReadToEnd());
                    sr.Close();
                }

                var compilerResults = Compiler.CompileAssemblyFromSource(CompilerParameters, sourceCode.ToString());
                if (!compilerResults.Errors.HasErrors)
                {
                    var script = compilerResults.CompiledAssembly;
                    Scripts.Add(path, script);
                    return script.CreateInstance(type, true);
                }
                else
                {
                    var errorString = new StringBuilder();
                    errorString.AppendFormat("Skipped script {0} with error:", path);
                    errorString.Append(Environment.NewLine);
                    foreach (CompilerError error in compilerResults.Errors)
                    {
                        errorString.AppendFormat("   {0}, line: {1}, column: {2}", error.ErrorText, error.Line - prefixLines, error.Column);
                        errorString.Append(Environment.NewLine);
                    }

                    Trace.TraceWarning(errorString.ToString());
                    return null;
                }
            }
            catch (InvalidDataException error)
            {
                Trace.TraceWarning("Skipped script {0} with error: {1}", path, error.Message);
                return null;
            }
            catch (Exception error)
            {
                if (File.Exists(path))
                    Trace.WriteLine(new FileLoadException(path, error));
                else
                    Trace.TraceWarning("Ignored missing script file {0}", path);
                return null;
            }
        }

        /*
        static ClassType CreateInstance<ClassType>(Assembly assembly) where ClassType : class
        {
            foreach (var type in assembly.GetTypes())
                if (typeof(ClassType).IsAssignableFrom(type))
                    return Activator.CreateInstance(type) as ClassType;

            return default(ClassType);
        }
        */

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return String.Format("{0:F0} scripts", Scripts.Keys.Count);
        }
    }
}

namespace ORTS.Scripting.Api
{
    #region TrainControlSystem

    public abstract class TrainControlSystem
    {
        public bool Activated { get; set; }

        /// <summary>
        /// False if vigilance monitor was switched off in game options, thus requested to be auto reset.
        /// </summary>
        public Func<bool> IsAlerterEnabled;
        /// <summary>
        /// True if alerter sound rings, otherwise false
        /// </summary>
        public Func<bool> AlerterSound;
        /// <summary>
        /// Max allowed speed for the train determined by consist.
        /// </summary>
        public Func<float> TrainSpeedLimitMpS;
        /// <summary>
        /// Max allowed speed determined by current signal.
        /// </summary>
        public Func<float> CurrentSignalSpeedLimitMpS;
        /// <summary>
        /// Max allowed speed determined by next signal.
        /// </summary>
        public Func<int, float> NextSignalSpeedLimitMpS;
        /// <summary>
        /// Aspect of the next signal.
        /// </summary>
        public Func<int, Aspect> NextSignalAspect;
        /// <summary>
        /// Distance to next signal.
        /// </summary>
        public Func<int, float> NextSignalDistanceM;
        /// <summary>
        /// Max allowed speed determined by current speedpost.
        /// </summary>
        public Func<float> CurrentPostSpeedLimitMpS;
        /// <summary>
        /// Max allowed speed determined by next speedpost.
        /// </summary>
        public Func<int, float> NextPostSpeedLimitMpS;
        /// <summary>
        /// Distance to next speedpost.
        /// </summary>
        public Func<int, float> NextPostDistanceM;
        /// <summary>
        /// Train's actual absolute speed.
        /// </summary>
        public Func<float> SpeedMpS;
        /// <summary>
        /// Clock value (in seconds) for the simulation. Starts at activity start time.
        /// </summary>
        public Func<float> ClockTime;
        /// <summary>
        /// Running total of distance travelled - always positive, updated by train physics.
        /// </summary>
        public Func<float> DistanceM;
        /// <summary>
        /// True if train brake controller is in emergency position, otherwise false.
        /// </summary>
        public Func<bool> IsBrakeEmergency;
        /// <summary>
        /// True if train brake controller is in full service position, otherwise false.
        /// </summary>
        public Func<bool> IsBrakeFullService;
        public Func<bool> EmergencyCausesThrottleDown;
        public Func<bool> EmergencyEngagesHorn;

        // TODO: The following will be available in .NET 4 as normal Func:
        public delegate TResult Func5<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

        /// <summary>
        /// (float targetDistanceM, float targetSpeedLimitMpS, float slope, float delayS, float trainDecelerationMpS2)
        /// Returns a speed curve based speed limit
        /// </summary>
        public Func5<float, float, float, float, float, float> SpeedCurve;

        /// <summary>
        /// Set train brake controller to full service position.
        /// </summary>
        public Action SetFullBrake;
        /// <summary>
        /// Set train brake controller to emergency position.
        /// </summary>
        public Action SetEmergencyBrake;
        /// <summary>
        /// Set throttle controller to position in range [0-1].
        /// </summary>
        public Action<float> SetThrottleController;
        /// <summary>
        /// Set dynamic brake controller to position in range [0-1].
        /// </summary>
        public Action<float> SetDynamicBrakeController;
        /// <summary>
        /// Cut power by pull all pantographs down.
        /// </summary>
        public Action SetPantographsDown;
        /// <summary>
        /// Switch vigilance alarm sound on (true) or off (false).
        /// </summary>
        public Action<bool> SetVigilanceAlarm;
        /// <summary>
        /// Set horn on (true) or off (false).
        /// </summary>
        public Action<bool> SetHorn;
        /// <summary>
        /// Trigger Info1 sound event
        /// </summary>
        public Action TriggerSoundInfo1;
        /// <summary>
        /// Trigger Info2 sound event
        /// </summary>
        public Action TriggerSoundInfo2;
        /// <summary>
        /// Trigger Penalty1 sound event
        /// </summary>
        public Action TriggerSoundPenalty1;
        /// <summary>
        /// Trigger Penalty2 sound event
        /// </summary>
        public Action TriggerSoundPenalty2;
        /// <summary>
        /// Trigger Activate sound event
        /// </summary>
        public Action TriggerSoundSystemActivate;
        /// <summary>
        /// Trigger Deactivate sound event
        /// </summary>
        public Action TriggerSoundSystemDeactivate;

        /// <summary>
        /// Set ALERTER_DISPLAY cabcontrol display's alarm state on or off.
        /// </summary>
        public Action<bool> SetVigilanceAlarmDisplay;
        /// <summary>
        /// Set ALERTER_DISPLAY cabcontrol display's emergency state on or off.
        /// </summary>
        public Action<bool> SetVigilanceEmergencyDisplay;
        /// <summary>
        /// Set OVERSPEED cabcontrol display on or off.
        /// </summary>
        public Action<bool> SetOverspeedWarningDisplay;
        /// <summary>
        /// Set PENALTY_APP cabcontrol display on or off.
        /// </summary>
        public Action<bool> SetPenaltyApplicationDisplay;
        /// <summary>
        /// Set current speed limit of the train, as to be shown on SPEEDLIMIT cabcontrol.
        /// </summary>
        public Action<float> SetCurrentSpeedLimitMpS;
        /// <summary>
        /// Set speed limit of the next signal, as to be shown on SPEEDLIM_DISPLAY cabcontrol.
        /// </summary>
        public Action<float> SetNextSpeedLimitMpS;
        /// <summary>
        /// Will be whown on ASPECT_DISPLAY cabcontrol.
        /// </summary>
        public Action<Aspect> SetNextSignalAspect;

        /// <summary>
        /// Called once at initialization time.
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        public abstract void Update();
        /// <summary>
        /// Internal reset request by touched systems other than the alerter button.
        /// </summary>
        public abstract void AlerterReset();
        /// <summary>
        /// Reset request by the alerter button.
        /// </summary>
        public abstract void AlerterPressed();
        /// <summary>
        /// Called by signalling code externally to stop the train in certain circumstances.
        /// </summary>
        public abstract void SetEmergency();
    }

    /// <summary>
    /// Base class for Timer and OdoMeter. Not to be used directly.
    /// </summary>
    public class Counter
    {
        float EndValue;
        protected Func<float> CurrentValue;

        public float AlarmValue { get; private set; }
        public float RemainingValue { get { return EndValue - CurrentValue(); } }
        public bool Started { get; private set; }
        public void Setup(float alarmValue) { AlarmValue = alarmValue; }
        public void Start() { EndValue = CurrentValue() + AlarmValue; Started = true; }
        public void Stop() { Started = false; }
        public bool Triggered { get { return Started && CurrentValue() >= EndValue; } }
    }

    public class Timer : Counter { public Timer(TrainControlSystem tcs) { CurrentValue = tcs.ClockTime; } }
    public class OdoMeter : Counter { public OdoMeter(TrainControlSystem tcs) { CurrentValue = tcs.DistanceM; } }

    // Represents the same enum as TrackMonitorSignalAspect
    // [Flags] is for allowing the following syntax: if ((NextSignalAspect() & (Aspect.Approach_3 | Aspect.Approach_2)) != 0)
    /// <summary>
    /// A signal aspect, as shown on track monitor
    /// </summary>
    [Flags]
    public enum Aspect
    {
        None,
        Clear_2,
        Clear_1,
        Approach_3,
        Approach_2,
        Approach_1,
        Restricted,
        StopAndProceed,
        Stop,
        Permission,
    }


    #endregion
}