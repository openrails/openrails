// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

#define GEARBOX_DEBUG_LOG

#define DEBUG_DUMP_STEAM_POWER_CURVE
// Uses the DataLogger to record power curve data for steam locos when no other option is chosen.
// To use this, on the Menu, check the Logging box and cancel all Options > DataLogger.
// The data logger records data in the file "Program\dump.csv".
// For steam locomotives only this replaces the default data with a record for each speed increment (mph).
// Collect the data by starting from rest and accelerating the loco to maximum speed.
// Only horsepower and mph available currently.
// Analyse the data using a spreadsheet and graph with an XY chart.


using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;
using ORTS.Settings;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Displays Viewer frame rate and Viewer.Text debug messages in the upper left corner of the screen.
    /// </summary>
    public class InfoDisplay
    {
        readonly Viewer Viewer;
        readonly DataLogger Logger;
        readonly int ProcessorCount = System.Environment.ProcessorCount;

        int FrameNumber;
        double LastUpdateRealTime;   // update text message only 10 times per second

        float previousLoggedSteamSpeedMpH = -5.0f;

#if DEBUG_DUMP_STEAM_POWER_CURVE
        float previousLoggedSpeedMpH = -1.0f;
#endif

        [StructLayout(LayoutKind.Sequential, Size = 40)]
        struct PROCESS_MEMORY_COUNTERS
        {
            public int cb;
            public int PageFaultCount;
            public int PeakWorkingSetSize;
            public int WorkingSetSize;
            public int QuotaPeakPagedPoolUsage;
            public int QuotaPagedPoolUsage;
            public int QuotaPeakNonPagedPoolUsage;
            public int QuotaNonPagedPoolUsage;
            public int PagefileUsage;
            public int PeakPagefileUsage;
        }

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, int size);

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        readonly IntPtr ProcessHandle;
        PROCESS_MEMORY_COUNTERS ProcessMemoryCounters;

        public InfoDisplay(Viewer viewer)
        {
            Viewer = viewer;
            Logger = new DataLogger(Path.Combine(Viewer.Settings.LoggingPath, "OpenRailsDump.csv"));

            ProcessHandle = OpenProcess(0x410 /* PROCESS_QUERY_INFORMATION | PROCESS_VM_READ */, false, Process.GetCurrentProcess().Id);
            ProcessMemoryCounters = new PROCESS_MEMORY_COUNTERS() { cb = 40 };

            if (Viewer.Settings.DataLogger)
                DataLoggerStart(Viewer.Settings);
        }

        [ThreadName("Render")]
        internal void Terminate()
        {
            if (Viewer.Settings.DataLogger)
                DataLoggerStop();
        }

        public void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(UserCommand.DebugLogger))
            {
                Viewer.Settings.DataLogger = !Viewer.Settings.DataLogger;
                if (Viewer.Settings.DataLogger)
                    DataLoggerStart(Viewer.Settings);
                else
                    DataLoggerStop();
            }
        }

        public bool IsRecordingSteamPerformance
        {
            get
            {
                return Viewer.Settings.DataLogger
                && Viewer.Settings.DataLogSteamPerformance
                && Viewer.PlayerLocomotive.GetType() == typeof(MSTSSteamLocomotive);
            }
        }

        void RecordSteamPerformance()
        {
            MSTSSteamLocomotive steamloco = (MSTSSteamLocomotive)Viewer.PlayerLocomotive;
            float SteamspeedMpH = MpS.ToMpH(steamloco.SpeedMpS);
            if (SteamspeedMpH >= previousLoggedSteamSpeedMpH + 5) // Add a new record every time speed increases by 5 mph
            {
                previousLoggedSteamSpeedMpH = (float)(int)SteamspeedMpH; // Keep speed records close to whole numbers

                Logger.Data(MpS.FromMpS(Viewer.PlayerLocomotive.SpeedMpS, false).ToString("F0"));
                Logger.Data(S.ToM(steamloco.SteamPerformanceTimeS).ToString("F1"));
                Logger.Data(Viewer.PlayerLocomotive.ThrottlePercent.ToString("F0"));
                Logger.Data(Viewer.PlayerTrain.MUReverserPercent.ToString("F0"));
                Logger.Data(N.ToLbf(Viewer.PlayerLocomotive.MotiveForceN).ToString("F0"));
                Logger.Data(steamloco.IndicatedHorsePowerHP.ToString("F0"));
                Logger.Data(steamloco.DrawBarPullLbsF.ToString("F0"));
                Logger.Data(steamloco.DrawbarHorsePowerHP.ToString("F0"));
                Logger.Data(N.ToLbf(steamloco.LocomotiveCouplerForceN).ToString("F0"));
                Logger.Data(N.ToLbf(steamloco.LocoTenderFrictionForceN).ToString("F0"));
                Logger.Data(N.ToLbf(steamloco.TotalFrictionForceN).ToString("F0"));
                Logger.Data(Kg.ToTUK(steamloco.TrainLoadKg).ToString("F0"));
                Logger.Data(steamloco.BoilerPressurePSI.ToString("F0"));
                Logger.Data(steamloco.LogSteamChestPressurePSI.ToString("F0"));
                Logger.Data(steamloco.LogInitialPressurePSI.ToString("F0"));
                Logger.Data(steamloco.LogCutoffPressurePSI.ToString("F0"));
                Logger.Data(steamloco.LogReleasePressurePSI.ToString("F0"));
                Logger.Data(steamloco.LogBackPressurePSI.ToString("F0"));

                Logger.Data(steamloco.MeanEffectivePressurePSI.ToString("F0"));


                Logger.Data(steamloco.CurrentSuperheatTempF.ToString("F0"));

                Logger.Data(pS.TopH(steamloco.CylinderSteamUsageLBpS).ToString("F0"));
                Logger.Data(pS.TopH(steamloco.WaterConsumptionLbpS).ToString("F0"));
                Logger.Data(Kg.ToLb(pS.TopH(steamloco.FuelBurnRateSmoothedKGpS)).ToString("F0"));


                Logger.Data(steamloco.SuperheaterSteamUsageFactor.ToString("F2"));
                Logger.Data(steamloco.CumulativeCylinderSteamConsumptionLbs.ToString("F0"));
                Logger.Data(steamloco.CumulativeWaterConsumptionLbs.ToString("F0"));

                Logger.Data(steamloco.CutoffPressureDropRatio.ToString("F2"));

                Logger.Data(steamloco.HPCylinderMEPPSI.ToString("F0"));
                Logger.Data(steamloco.LogLPInitialPressurePSI.ToString("F0"));
                Logger.Data(steamloco.LogLPCutoffPressurePSI.ToString("F0"));
                Logger.Data(steamloco.LogLPReleasePressurePSI.ToString("F0"));
                Logger.Data(steamloco.LogLPBackPressurePSI.ToString("F0"));
                Logger.Data(steamloco.CutoffPressureDropRatio.ToString("F2"));
                Logger.Data(steamloco.LPCylinderMEPPSI.ToString("F0"));

                Logger.End();
            }
        }

#if DEBUG_DUMP_STEAM_POWER_CURVE
        public bool IsRecordingSteamPowerCurve
        {
            get
            {
                return Viewer.Settings.DataLogger
                && !Viewer.Settings.DataLogPerformance
                && !Viewer.Settings.DataLogPhysics
                && !Viewer.Settings.DataLogMisc
                && !Viewer.Settings.DataLogSteamPerformance
                && Viewer.PlayerLocomotive.GetType() == typeof(MSTSSteamLocomotive);
            }
        }

        void RecordSteamPowerCurve()
        {
            MSTSSteamLocomotive loco = (MSTSSteamLocomotive)Viewer.PlayerLocomotive;
            float speedMpH = MpS.ToMpH(loco.SpeedMpS);
            if (speedMpH >= previousLoggedSpeedMpH + 1) // Add a new record every time speed increases by 1 mph
            {
                previousLoggedSpeedMpH = (float)(int)speedMpH; // Keep speed records close to whole numbers
                Logger.Data(speedMpH.ToString("F1"));
                float power = W.ToHp(loco.MotiveForceN * loco.SpeedMpS);
                Logger.Data(power.ToString("F1"));
                Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).ThrottlePercent.ToString("F0"));
                Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).Train.MUReverserPercent.ToString("F0"));
                Logger.End();
            }
        }
#endif

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            FrameNumber++;

            if (Viewer.RealTime - LastUpdateRealTime >= 0.25)
            {
                double elapsedRealSeconds = Viewer.RealTime - LastUpdateRealTime;
                LastUpdateRealTime = Viewer.RealTime;
                Profile(elapsedRealSeconds);
            }

#if DEBUG_DUMP_STEAM_POWER_CURVE
            if (IsRecordingSteamPowerCurve)
            {
                RecordSteamPowerCurve();
            }
            else
            {
#endif
                if (IsRecordingSteamPerformance)
                {
                    RecordSteamPerformance();
                }
                else


                //Here's where the logger stores the data from each frame
                if (Viewer.Settings.DataLogger)
                {
                    Logger.Separator = (DataLogger.Separators)Enum.Parse(typeof(DataLogger.Separators), Viewer.Settings.DataLoggerSeparator);
                    if (Viewer.Settings.DataLogPerformance)
                    {
                        Logger.Data(VersionInfo.Version);
                        Logger.Data(FrameNumber.ToString("F0"));
                        Logger.Data(GetWorkingSetSize().ToString("F0"));
                        Logger.Data(GC.GetTotalMemory(false).ToString("F0"));
                        Logger.Data(GC.CollectionCount(0).ToString("F0"));
                        Logger.Data(GC.CollectionCount(1).ToString("F0"));
                        Logger.Data(GC.CollectionCount(2).ToString("F0"));
                        Logger.Data(ProcessorCount.ToString("F0"));
                        Logger.Data(Viewer.RenderProcess.FrameRate.Value.ToString("F0"));
                        Logger.Data(Viewer.RenderProcess.FrameTime.Value.ToString("F6"));
                        Logger.Data(Viewer.RenderProcess.ShadowPrimitivePerFrame.Sum().ToString("F0"));
                        Logger.Data(Viewer.RenderProcess.PrimitivePerFrame.Sum().ToString("F0"));
                        Logger.Data(Viewer.RenderProcess.Profiler.Wall.Value.ToString("F0"));
                        Logger.Data(Viewer.UpdaterProcess.Profiler.Wall.Value.ToString("F0"));
                        Logger.Data(Viewer.LoaderProcess.Profiler.Wall.Value.ToString("F0"));
                        Logger.Data(Viewer.SoundProcess.Profiler.Wall.Value.ToString("F0"));
                    }
                    if (Viewer.Settings.DataLogPhysics)
                    {
                        Logger.Data(FormatStrings.FormatPreciseTime(Viewer.Simulator.ClockTime));
                        Logger.Data(Viewer.PlayerLocomotive.Direction.ToString());
                        Logger.Data(Viewer.PlayerTrain.MUReverserPercent.ToString("F0"));
                        Logger.Data(Viewer.PlayerLocomotive.ThrottlePercent.ToString("F0"));
                        Logger.Data(Viewer.PlayerLocomotive.MotiveForceN.ToString("F0"));
                        Logger.Data(Viewer.PlayerLocomotive.BrakeForceN.ToString("F0"));
                        Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).LocomotiveAxle.AxleForceN.ToString("F2"));
                        Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).LocomotiveAxle.SlipSpeedPercent.ToString("F1"));

                        void logSpeed(float speedMpS)
                        {
                            string result;
                            switch (Viewer.Settings.DataLogSpeedUnits)
                            {
                                case "route":
                                    result = FormatStrings.FormatSpeed(speedMpS, Viewer.MilepostUnitsMetric);
                                    break;
                                case "mps":
                                    result = speedMpS.ToString("F1");
                                    break;
                                case "mph":
                                    result = MpS.FromMpS(speedMpS, false).ToString("F1");
                                    break;
                                case "kmph":
                                    result = MpS.FromMpS(speedMpS, true).ToString("F1");
                                    break;
                                default:
                                    result = FormatStrings.FormatSpeed(speedMpS, Viewer.MilepostUnitsMetric);
                                    break;
                            }
                            Logger.Data(result);
                        }
                        logSpeed(Viewer.PlayerLocomotive.SpeedMpS);
                        logSpeed(Viewer.PlayerTrain.AllowedMaxSpeedMpS);

                        Logger.Data((Viewer.PlayerLocomotive.DistanceM.ToString("F0")));
                        Logger.Data((Viewer.PlayerLocomotive.GravityForceN.ToString("F0")));

                        if ((Viewer.PlayerLocomotive as MSTSLocomotive).TrainBrakeController != null)
                            Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).TrainBrakeController.CurrentValue.ToString("F2"));
                        else
                            Logger.Data("null");

                        if ((Viewer.PlayerLocomotive as MSTSLocomotive).EngineBrakeController != null)
                            Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).EngineBrakeController.CurrentValue.ToString("F2"));
                        else
                            Logger.Data("null");

                        if ((Viewer.PlayerLocomotive as MSTSLocomotive).BrakemanBrakeController != null)
                            Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).BrakemanBrakeController.CurrentValue.ToString("F2"));
                        else
                            Logger.Data("null");

                        Logger.Data(Viewer.PlayerLocomotive.BrakeSystem.GetCylPressurePSI().ToString("F0"));
                        Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).MainResPressurePSI.ToString("F0"));
                        Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).CompressorIsOn.ToString());
#if GEARBOX_DEBUG_LOG
                        if (Viewer.PlayerLocomotive.GetType() == typeof(MSTSDieselLocomotive))
                        {
                            Logger.Data((Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].RealRPM.ToString("F0"));
                            Logger.Data((Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].DemandedRPM.ToString("F0"));
                            Logger.Data((Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].LoadPercent.ToString("F0"));
                            if ((Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines.HasGearBox)
                            {
                                Logger.Data((Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].GearBox.CurrentGearIndex.ToString());
                                Logger.Data((Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].GearBox.NextGearIndex.ToString());
                                Logger.Data((Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselEngines[0].GearBox.ClutchPercent.ToString());
                            }
                            else
                            {
                                Logger.Data("null");
                                Logger.Data("null");
                                Logger.Data("null");
                            }
                            Logger.Data((Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselFlowLps.ToString("F2"));
                            Logger.Data((Viewer.PlayerLocomotive as MSTSDieselLocomotive).DieselLevelL.ToString("F0"));
                            Logger.Data("null");
                            Logger.Data("null");
                            Logger.Data("null");
                        }
                        if (Viewer.PlayerLocomotive.GetType() == typeof(MSTSElectricLocomotive))
                        {
                            Logger.Data((Viewer.PlayerLocomotive as MSTSElectricLocomotive).Pantographs[1].CommandUp.ToString());
                            Logger.Data((Viewer.PlayerLocomotive as MSTSElectricLocomotive).Pantographs[2].CommandUp.ToString());
                            Logger.Data((Viewer.PlayerLocomotive as MSTSElectricLocomotive).Pantographs.List.Count > 2 ?
                                (Viewer.PlayerLocomotive as MSTSElectricLocomotive).Pantographs[3].CommandUp.ToString() : null);
                            Logger.Data((Viewer.PlayerLocomotive as MSTSElectricLocomotive).Pantographs.List.Count > 3 ?
                                (Viewer.PlayerLocomotive as MSTSElectricLocomotive).Pantographs[4].CommandUp.ToString() : null);
                            Logger.Data("null");
                            Logger.Data("null");
                            Logger.Data("null");
                            Logger.Data("null");
                            Logger.Data("null");
                            Logger.Data("null");
                            Logger.Data("null");
                        }
                        if (Viewer.PlayerLocomotive.GetType() == typeof(MSTSSteamLocomotive))
                        {
                            Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).BlowerSteamUsageLBpS.ToString("F0"));
                            Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).BoilerPressurePSI.ToString("F0"));
                            Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).CylinderCocksAreOpen.ToString());
                            Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).CylinderCompoundOn.ToString());
                            Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).EvaporationLBpS.ToString("F0"));
                            Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).FireMassKG.ToString("F0"));
                            Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).CylinderSteamUsageLBpS.ToString("F0"));
                            if ((Viewer.PlayerLocomotive as MSTSSteamLocomotive).BlowerController != null)
                                Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).BlowerController.CurrentValue.ToString("F0"));
                            else
                                Logger.Data("null");

                            if ((Viewer.PlayerLocomotive as MSTSSteamLocomotive).DamperController != null)
                                Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).DamperController.CurrentValue.ToString("F0"));
                            else
                                Logger.Data("null");
                            if ((Viewer.PlayerLocomotive as MSTSSteamLocomotive).FiringRateController != null)
                                Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).FiringRateController.CurrentValue.ToString("F0"));
                            else
                                Logger.Data("null");
                            if ((Viewer.PlayerLocomotive as MSTSSteamLocomotive).Injector1Controller != null)
                                Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).Injector1Controller.CurrentValue.ToString("F0"));
                            else
                                Logger.Data("null");
                            if ((Viewer.PlayerLocomotive as MSTSSteamLocomotive).Injector2Controller != null)
                                Logger.Data((Viewer.PlayerLocomotive as MSTSSteamLocomotive).Injector2Controller.CurrentValue.ToString("F0"));
                            else
                                Logger.Data("null");
                        }
#endif
                    }
                    Logger.End();
#if DEBUG_DUMP_STEAM_POWER_CURVE
                }
#endif
            }
        }

        int GetWorkingSetSize()
        {
            // Get memory usage (working set).
            GetProcessMemoryInfo(ProcessHandle, out ProcessMemoryCounters, ProcessMemoryCounters.cb);
            var memory = ProcessMemoryCounters.WorkingSetSize;
            return memory;
        }

        static void DataLoggerStart(UserSettings settings)
        {
            using (StreamWriter file = File.AppendText(Path.Combine(settings.LoggingPath, "OpenRailsDump.csv")))
            {
                DataLogger.Separators separator = (DataLogger.Separators)Enum.Parse(typeof(DataLogger.Separators), settings.DataLoggerSeparator);
                string headerLine = "";
                if (settings.DataLogPerformance)
                {
                    headerLine = String.Join(Convert.ToString((char)separator),
                        new string[]
                            {
                                "SVN",
                                "Frame",
                                "Memory",
                                "Memory (Managed)",
                                "Gen 0 GC",
                                "Gen 1 GC",
                                "Gen 2 GC",
                                "Processors",
                                "Frame Rate",
                                "Frame Time",
                                "Shadow Primitives",
                                "Render Primitives",
                                "Render Process",
                                "Updater Process",
                                "Loader Process",
                                "Sound Process"
                            }
                        );
                }
                if (settings.DataLogPhysics)
                {
                    if (settings.DataLogPerformance)
                        headerLine += Convert.ToString((char)separator);

                    headerLine += String.Join(Convert.ToString((char)separator),
                            new string[]
                            {
                                "Time",
                                "Player Direction",
                                "Player Reverser [%]",
                                "Player Throttle [%]",
                                "Player Motive Force [N]",
                                "Player Brake Force [N]",
                                "Player Axle Force [N]",
                                "Player Wheelslip",
                                $"Player Speed [{settings.DataLogSpeedUnits}]",
                                $"Speed Limit [{settings.DataLogSpeedUnits}]",
                                "Distance [m]",
                                "Player Gravity Force [N]",
                                "Train Brake",
                                "Engine Brake",
                                "Player Cylinder PSI",
                                "Player Main Res PSI",
                                "Player Compressor On",
                                "D:Real RPM / E:panto 1 / S:Blower usage LBpS",
                                "D:Demanded RPM / E:panto 2 / S:Boiler PSI",
                                "D:Load % / E:panto 3 / S:Cylinder Cocks open",
                                "D:Gearbox Current Gear / E:panto 4 / S:Evaporation LBpS",
                                "D:Gearbox Next Gear / E:null / S:Fire Mass KG",
                                "D:Clutch % / E:null / S:Steam usage LBpS",
                                "D:Fuel Flow Lps / E:null / S:Blower",
                                "D:Fuel level L / E:null / S:Damper",
                                "D:null / E:null / S:Firing Rate",
                                "D:null / E:null / S:Injector 1",
                                "D:null / E:null / S:Injector 2"
                            }
                        );
                }
                //Ready to use...
                //if (settings.DataLogMisc)
                //{
                //    if (settings.DataLogPerformance || settings.DataLogPhysics)
                //        headerLine += Convert.ToString((char)separator);
                //    headerLine += String.Join(Convert.ToString((char)separator),
                //        new string[] {"null",
                //        "null"});
                //}

                if (settings.DataLogSteamPerformance)
                {
                    headerLine = String.Join(Convert.ToString((char)separator),
                        new string[]
                            {
                                "Speed (mph)",
                                "Time (M)",
                                "Throttle (%)",
                                "Cut-off (%)",
                                "ITE (MotiveForce - lbf)",
                                "IHP (hp)",
                                "Drawbar TE (lbf)",
                                "Drawbar HP (hp)",
                                "Coupler Force (lbf)",
                                "Loco & Tender Resistance (lbf)",
                                "Train Resistance (lbf)",
                                "Train Load (t-uk)",
                                "Boiler Pressure (psi)",
                                "Steam Chest Pressure (psi)",
                                "Initial Pressure (psi)",
                                "Cutoff Pressure (psi)",
                                "Release Pressure (psi)",
                                "Back Pressure (psi)",
                                "MEP (psi)",
                                "Superheat Temp (F)",
                                "Steam consumption (lbs/h)",
                                "Water consumption (lbs/h)",
                                "Coal consumption (lbs/h)",
                                "Cylinder Thermal Efficiency",
                                "Cumulative Steam (lbs)",
                                "Cumulative Water (lbs)",
                                "Cutoff pressure Ratio",

                                "HP MEP (psi)",
                                "LPInitial Pressure (psi)",
                                "LPCutoff Pressure (psi)",
                                "LPRelease Pressure (psi)",
                                "LPBack Pressure (psi)",
                                "LPCutoff pressure Ratio",
                                "LP MEP (psi)"

                             }
                        );
                }

#if DEBUG_DUMP_STEAM_POWER_CURVE
                if (!settings.DataLogPerformance
                && !settings.DataLogPhysics
                && !settings.DataLogMisc
                && !settings.DataLogSteamPerformance)
                {

                    headerLine = String.Join(Convert.ToString((char)separator),
                        new string[]
                            {
                                "speed (mph)",
                                "power (hp)",
                                "throttle (%)",
                                "cut-off (%)"
                            });
                }
#endif
                file.WriteLine(headerLine);
            }
        }

        void DataLoggerStop()
        {
            Logger.Flush();
        }

        public void Profile(double elapsedRealSeconds) // should be called every 100mS
        {
            if (elapsedRealSeconds < 0.01)  // just in case
                return;

            Viewer.RenderProcess.Profiler.Mark();
            Viewer.UpdaterProcess.Profiler.Mark();
            Viewer.LoaderProcess.Profiler.Mark();
            Viewer.SoundProcess.Profiler.Mark();
        }
    }
}
