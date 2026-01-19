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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;

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
        double NextLogTime;

        float PreviousLoggedSpeedMpH = -1.0f;

        public InfoDisplay(Viewer viewer)
        {
            Viewer = viewer;
            Logger = new DataLogger(Path.Combine(Viewer.Settings.LoggingPath, "OpenRailsDump.csv"));

            if (Viewer.Settings.DataLogger)
                DataLoggerStart();
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
                {
                    DataLoggerStart();
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Data Logger started"));
                }
                else
                {
                    DataLoggerStop();
                    Viewer.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Data Logger stopped"));
                }
            }
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            FrameNumber++;

            if (Viewer.RealTime - LastUpdateRealTime >= 0.25)
            {
                double elapsedRealSeconds = Viewer.RealTime - LastUpdateRealTime;
                LastUpdateRealTime = Viewer.RealTime;
                Profile(elapsedRealSeconds);
            }

            if (Viewer.Settings.DataLogger && (Viewer.Settings.DataLoggerInterval == 0 || Viewer.RealTime >= NextLogTime))
            {
                DataLoggerLog();
                NextLogTime = Viewer.RealTime + Viewer.Settings.DataLoggerInterval / 1000f;
            }
        }

        void DataLoggerLog()
        {
            // NOTE: Conditions and data here MUST match similar code in DataLoggerStart
            // Failure to update both places will result in mismatched columns or worse
            if (Viewer.Settings.DataLogExclusiveSteamPerformance)
            {
                if (Viewer.PlayerLocomotive is MSTSSteamLocomotive steam)
                {
                    var speedMpH = MpS.ToMpH(steam.SpeedMpS);
                    if (speedMpH >= PreviousLoggedSpeedMpH + 5) // Add a new record every time speed increases by 5 mph
                    {
                        PreviousLoggedSpeedMpH = (int)speedMpH; // Keep speed records close to whole numbers

                        Logger.Data(MpS.FromMpS(Viewer.PlayerLocomotive.SpeedMpS, false).ToString("F0"));
                        Logger.Data(S.ToM(steam.SteamPerformanceTimeS).ToString("F1"));
                        Logger.Data(Viewer.PlayerLocomotive.ThrottlePercent.ToString("F0"));
                        Logger.Data(Viewer.PlayerTrain.MUReverserPercent.ToString("F0"));
                        Logger.Data(N.ToLbf(Viewer.PlayerLocomotive.MotiveForceN).ToString("F0"));
                        Logger.Data(steam.IndicatedHorsePowerHP.ToString("F0"));
                        Logger.Data(steam.DrawBarPullLbsF.ToString("F0"));
                        Logger.Data(steam.DrawbarHorsePowerHP.ToString("F0"));
                        Logger.Data(N.ToLbf(steam.LocomotiveCouplerForceN).ToString("F0"));
                        Logger.Data(N.ToLbf(steam.LocoTenderFrictionForceN).ToString("F0"));
                        Logger.Data(N.ToLbf(steam.TotalFrictionForceN).ToString("F0"));
                        Logger.Data(Kg.ToTUK(steam.TrainLoadKg).ToString("F0"));
                        Logger.Data(steam.BoilerPressurePSI.ToString("F0"));
                        Logger.Data(steam.LogSteamChestPressurePSI.ToString("F0"));
                        Logger.Data(steam.LogInitialPressurePSI.ToString("F0"));
                        Logger.Data(steam.LogCutoffPressurePSI.ToString("F0"));
                        Logger.Data(steam.LogReleasePressurePSI.ToString("F0"));
                        Logger.Data(steam.LogBackPressurePSI.ToString("F0"));
                        Logger.Data(steam.MeanEffectivePressurePSI.ToString("F0"));
                        Logger.Data(steam.CurrentSuperheatTempF.ToString("F0"));
                        Logger.Data(pS.TopH(steam.CylinderSteamUsageLBpS).ToString("F0"));
                        Logger.Data(pS.TopH(steam.WaterConsumptionLbpS).ToString("F0"));
                        Logger.Data(Kg.ToLb(pS.TopH(steam.FuelBurnRateSmoothedKGpS)).ToString("F0"));
                        Logger.Data(steam.SuperheaterSteamUsageFactor.ToString("F2"));
                        Logger.Data(steam.CumulativeCylinderSteamConsumptionLbs.ToString("F0"));
                        Logger.Data(steam.CumulativeWaterConsumptionLbs.ToString("F0"));
                        Logger.Data(steam.CutoffPressureDropRatio.ToString("F2"));
                        Logger.Data(steam.HPCylinderMEPPSI.ToString("F0"));
                        Logger.Data(steam.LogLPInitialPressurePSI.ToString("F0"));
                        Logger.Data(steam.LogLPCutoffPressurePSI.ToString("F0"));
                        Logger.Data(steam.LogLPReleasePressurePSI.ToString("F0"));
                        Logger.Data(steam.LogLPBackPressurePSI.ToString("F0"));
                        Logger.Data(steam.CutoffPressureDropRatio.ToString("F2"));
                        Logger.Data(steam.LPCylinderMEPPSI.ToString("F0"));
                    }
                }
            }
            else if (Viewer.Settings.DataLogExclusiveSteamPowerCurve)
            {
                if (Viewer.PlayerLocomotive is MSTSSteamLocomotive steam)
                {
                    var speedMpH = MpS.ToMpH(steam.SpeedMpS);
                    if (speedMpH >= PreviousLoggedSpeedMpH + 1) // Add a new record every time speed increases by 5 mph
                    {
                        PreviousLoggedSpeedMpH = (int)speedMpH; // Keep speed records close to whole numbers

                        Logger.Data(speedMpH.ToString("F1"));
                        Logger.Data(W.ToHp(steam.MotiveForceN * steam.SpeedMpS).ToString("F1"));
                        Logger.Data(steam.ThrottlePercent.ToString("F0"));
                        Logger.Data(steam.Train.MUReverserPercent.ToString("F0"));
                    }
                }
            }
            else
            {
                Logger.Data(VersionInfo.Version);
                Logger.Data(FrameNumber.ToString("F0"));
                Logger.Data(FormatStrings.FormatPreciseTime(Viewer.Simulator.ClockTime));
                if (Viewer.Settings.DataLogPerformance)
                {
                    Logger.Data(Viewer.Game.HostProcess.CPUMemoryWorkingSet.ToString("F0"));
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
                    Logger.Data(Viewer.PlayerLocomotive.Direction.ToString());
                    Logger.Data(Viewer.PlayerTrain.MUReverserPercent.ToString("F0"));
                    Logger.Data(Viewer.PlayerLocomotive.ThrottlePercent.ToString("F0"));
                    Logger.Data(Viewer.PlayerLocomotive.MotiveForceN.ToString("F0"));
                    Logger.Data(Viewer.PlayerLocomotive.BrakeForceN.ToString("F0"));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).LocomotiveAxles.AxleMotiveForceN.ToString("F2"));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).LocomotiveAxles.SlipSpeedPercent.ToString("F1"));
                    DataLoggerLogSpeed(Viewer.PlayerLocomotive.SpeedMpS);
                    DataLoggerLogSpeed(Viewer.PlayerTrain.AllowedMaxSpeedMpS);
                    Logger.Data((Viewer.PlayerLocomotive.DistanceM.ToString("F0")));
                    Logger.Data((Viewer.PlayerLocomotive.GravityForceN.ToString("F0")));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).TrainBrakeController?.CurrentValue.ToString("F2"));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).EngineBrakeController?.CurrentValue.ToString("F2"));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).BrakemanBrakeController?.CurrentValue.ToString("F2"));
                    Logger.Data(Viewer.PlayerLocomotive.BrakeSystem.GetCylPressurePSI().ToString("F0"));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).MainResPressurePSI.ToString("F0"));
                    if (Viewer.PlayerLocomotive is MSTSDieselLocomotive diesel)
                    {
                        Logger.Data(diesel.DieselEngines[0].RealRPM.ToString("F0"));
                        Logger.Data(diesel.DieselEngines[0].DemandedRPM.ToString("F0"));
                        Logger.Data(diesel.DieselEngines[0].LoadPercent.ToString("F0"));
                        if (diesel.DieselEngines.HasGearBox)
                        {
                            Logger.Data(diesel.DieselEngines[0].GearBox.CurrentGearIndex.ToString());
                            Logger.Data(diesel.DieselEngines[0].GearBox.NextGearIndex.ToString());
                            Logger.Data(diesel.DieselEngines[0].GearBox.ClutchPercent.ToString());
                        }
                        else
                        {
                            Logger.Data(null);
                            Logger.Data(null);
                            Logger.Data(null);
                        }
                        Logger.Data(diesel.DieselFlowLps.ToString("F2"));
                        Logger.Data(diesel.DieselLevelL.ToString("F0"));
                        Logger.Data(null);
                        Logger.Data(null);
                        Logger.Data(null);
                        Logger.Data(null);
                    }
                    else if (Viewer.PlayerLocomotive is MSTSElectricLocomotive electric)
                    {
                        Logger.Data(electric.Pantographs[1].CommandUp.ToString());
                        Logger.Data(electric.Pantographs[2].CommandUp.ToString());
                        Logger.Data(electric.Pantographs.List.Count > 2 ? electric.Pantographs[3].CommandUp.ToString() : null);
                        Logger.Data(electric.Pantographs.List.Count > 3 ? electric.Pantographs[4].CommandUp.ToString() : null);
                        Logger.Data(null);
                        Logger.Data(null);
                        Logger.Data(null);
                        Logger.Data(null);
                        Logger.Data(null);
                        Logger.Data(null);
                        Logger.Data(null);
                        Logger.Data(null);
                    }
                    else if (Viewer.PlayerLocomotive is MSTSSteamLocomotive steam)
                    {
                        Logger.Data(steam.BlowerSteamUsageLBpS.ToString("F0"));
                        Logger.Data(steam.BoilerPressurePSI.ToString("F0"));
                        Logger.Data(steam.CylinderCocksAreOpen.ToString());
                        Logger.Data(steam.CylinderCompoundOn.ToString());
                        Logger.Data(steam.EvaporationLBpS.ToString("F0"));
                        Logger.Data(steam.FireMassKG.ToString("F0"));
                        Logger.Data(steam.CylinderSteamUsageLBpS.ToString("F0"));
                        Logger.Data(steam.BlowerController?.CurrentValue.ToString("F0"));
                        Logger.Data(steam.DamperController?.CurrentValue.ToString("F0"));
                        Logger.Data(steam.FiringRateController?.CurrentValue.ToString("F0"));
                        Logger.Data(steam.Injector1Controller?.CurrentValue.ToString("F0"));
                        Logger.Data(steam.Injector2Controller?.CurrentValue.ToString("F0"));
                    }
                }
            }
            Logger.End();
        }

        void DataLoggerLogSpeed(float speedMpS)
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

        void DataLoggerStart()
        {
            // NOTE: Conditions and data here MUST match similar code in DataLoggerLog
            // Failure to update both places will result in mismatched columns or worse
            if (Viewer.Settings.DataLogExclusiveSteamPerformance)
            {
                Logger.Data("Speed (mph)");
                Logger.Data("Time (M)");
                Logger.Data("Throttle (%)");
                Logger.Data("Cut-off (%)");
                Logger.Data("ITE (MotiveForce - lbf)");
                Logger.Data("IHP (hp)");
                Logger.Data("Drawbar TE (lbf)");
                Logger.Data("Drawbar HP (hp)");
                Logger.Data("Coupler Force (lbf)");
                Logger.Data("Loco & Tender Resistance (lbf)");
                Logger.Data("Train Resistance (lbf)");
                Logger.Data("Train Load (t-uk)");
                Logger.Data("Boiler Pressure (psi)");
                Logger.Data("Steam Chest Pressure (psi)");
                Logger.Data("Initial Pressure (psi)");
                Logger.Data("Cutoff Pressure (psi)");
                Logger.Data("Release Pressure (psi)");
                Logger.Data("Back Pressure (psi)");
                Logger.Data("MEP (psi)");
                Logger.Data("Superheat Temp (F)");
                Logger.Data("Steam consumption (lbs/h)");
                Logger.Data("Water consumption (lbs/h)");
                Logger.Data("Coal consumption (lbs/h)");
                Logger.Data("Cylinder Thermal Efficiency");
                Logger.Data("Cumulative Steam (lbs)");
                Logger.Data("Cumulative Water (lbs)");
                Logger.Data("Cutoff pressure Ratio");
                Logger.Data("HP MEP (psi)");
                Logger.Data("LPInitial Pressure (psi)");
                Logger.Data("LPCutoff Pressure (psi)");
                Logger.Data("LPRelease Pressure (psi)");
                Logger.Data("LPBack Pressure (psi)");
                Logger.Data("LPCutoff pressure Ratio");
                Logger.Data("LP MEP (psi)");
            }
            else if (Viewer.Settings.DataLogExclusiveSteamPowerCurve)
            {
                Logger.Data("Speed (mph)");
                Logger.Data("Power (hp)");
                Logger.Data("Throttle (%)");
                Logger.Data("Cut-off (%)");
            }
            else
            {
                Logger.Data("Version");
                Logger.Data("Frame");
                Logger.Data("Time");
                if (Viewer.Settings.DataLogPerformance)
                {
                    Logger.Data("Memory");
                    Logger.Data("Memory (Managed)");
                    Logger.Data("Gen 0 GC");
                    Logger.Data("Gen 1 GC");
                    Logger.Data("Gen 2 GC");
                    Logger.Data("Processors");
                    Logger.Data("Frame Rate");
                    Logger.Data("Frame Time");
                    Logger.Data("Shadow Primitives");
                    Logger.Data("Render Primitives");
                    Logger.Data("Render Process");
                    Logger.Data("Updater Process");
                    Logger.Data("Loader Process");
                    Logger.Data("Sound Process");
                }
                if (Viewer.Settings.DataLogPhysics)
                {
                    Logger.Data("Player Direction");
                    Logger.Data("Player Reverser [%]");
                    Logger.Data("Player Throttle [%]");
                    Logger.Data("Player Motive Force [N]");
                    Logger.Data("Player Brake Force [N]");
                    Logger.Data("Player Axle Force [N]");
                    Logger.Data("Player Wheelslip");
                    Logger.Data($"Player Speed [{Viewer.Settings.DataLogSpeedUnits}]");
                    Logger.Data($"Speed Limit [{Viewer.Settings.DataLogSpeedUnits}]");
                    Logger.Data("Distance [m]");
                    Logger.Data("Player Gravity Force [N]");
                    Logger.Data("Train Brake");
                    Logger.Data("Engine Brake");
                    Logger.Data("Brakeman Brake");
                    Logger.Data("Player Cylinder PSI");
                    Logger.Data("Player Main Res PSI");
                    Logger.Data("D:Real RPM / E:panto 1 / S:Blower usage LBpS");
                    Logger.Data("D:Demanded RPM / E:panto 2 / S:Boiler PSI");
                    Logger.Data("D:Load % / E:panto 3 / S:Cylinder Cocks open");
                    Logger.Data("D:Gearbox Current Gear / E:panto 4 / S:Cylinder Compound on");
                    Logger.Data("D:Gearbox Next Gear / E:null / S:Evaporation LBpS");
                    Logger.Data("D:Clutch % / E:null / S:Fire Mass KG");
                    Logger.Data("D:Fuel Flow Lps / E:null / S:Steam usage LBpS");
                    Logger.Data("D:Fuel level L / E:null / S:Blower");
                    Logger.Data("D:null / E:null / S:Damper");
                    Logger.Data("D:null / E:null / S:Firing Rate");
                    Logger.Data("D:null / E:null / S:Injector 1");
                    Logger.Data("D:null / E:null / S:Injector 2");
                }
            }
            Logger.End();
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
