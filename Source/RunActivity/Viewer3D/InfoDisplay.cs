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
using System.Globalization;
using System.IO;
using System.Linq;
using GNU.Gettext;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Displays Viewer frame rate and Viewer.Text debug messages in the upper left corner of the screen.
    /// </summary>
    public class InfoDisplay
    {
        public static GettextResourceManager Catalog = new GettextResourceManager("ORTS.Common");

        readonly Viewer Viewer;
        readonly DataLogger Logger;
        readonly int ProcessorCount = System.Environment.ProcessorCount;

        readonly bool UseNativeUnits = false;  // when true, do not perform any conversions
        readonly bool IsMetric = true;
        readonly bool IsUK = false;

        readonly string DistanceUnit = FormatStrings.m;
        readonly string SpeedUnit = FormatStrings.mps;
        readonly string ForceUnit = FormatStrings.n;
        readonly string PowerUnit = FormatStrings.w;
        readonly string LargeMassUnit = FormatStrings.kg;
        readonly string MassUnit = FormatStrings.kg;
        readonly string VolumeUnit = FormatStrings.l;
        readonly string TemperatureUnit = FormatStrings.c;
        readonly string VolumeConsumptionUnit = FormatStrings.lps;
        readonly string MassConsumptionUnit = FormatStrings.kgps;
        private PressureUnit MainPressureUnit = PressureUnit.PSI;  // may change when train or loco changes

        // conversion factors not defined in Conversions.cs
        const float DieselDensityKgpL = 0.85f;
        const float BunkerCDensityKgpl = 0.95f;
        const float WaterDensityKgpL = 1.0f;

        int FrameNumber;
        double LastUpdateRealTime;   // update text message only 10 times per second
        double NextLogTime;

        TrainCar PreviousPlayerLocomotive = null;
        float PreviousLoggedSpeedMpH = -1.0f;

        public InfoDisplay(Viewer viewer)
        {
            Viewer = viewer;
            UseNativeUnits = viewer.Settings.DataLogUseInternalUnits;

            // from GetMeasurementUnits() in MSTSWagon.cs
            IsMetric = viewer.Settings.Units == "Metric" || (viewer.Settings.Units == "Automatic" && System.Globalization.RegionInfo.CurrentRegion.IsMetric) ||
                (viewer.Settings.Units == "Route" && viewer.Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric);
            IsUK = viewer.Settings.Units == "UK";

            if (!UseNativeUnits)
            {
                DistanceUnit = IsMetric ? FormatStrings.km : FormatStrings.mi;
                SpeedUnit = IsMetric ? FormatStrings.kmph : FormatStrings.mph;
                ForceUnit = IsMetric ? FormatStrings.kN : FormatStrings.klbf;
                PowerUnit = IsMetric ? FormatStrings.kW : FormatStrings.hp;
                LargeMassUnit = IsMetric ? FormatStrings.t : IsUK ? FormatStrings.tonUK : FormatStrings.tonUS;
                MassUnit = IsMetric ? FormatStrings.kg : FormatStrings.lb;
                VolumeUnit = IsMetric ? FormatStrings.l : IsUK ? FormatStrings.galUK : FormatStrings.galUS;
                TemperatureUnit = IsMetric ? FormatStrings.c : FormatStrings.f;
                VolumeConsumptionUnit = IsMetric ? FormatStrings.lph : IsUK ? FormatStrings.gphUK : FormatStrings.gphUS;
                MassConsumptionUnit = IsMetric ? FormatStrings.kgph : FormatStrings.lbph;
                // MainPressureUnit will be set in DataLoggerStart()
            }

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
                // when the player changes train or locomotive, rewrite the header as the logged parameters could change
                if (Viewer.PlayerLocomotive != PreviousPlayerLocomotive)
                {
                    // PreviousPlayerLocomotive is set in DataLoggerStart()
                    DataLoggerStart();
                }

                DataLoggerLog();
                NextLogTime = Viewer.RealTime + Viewer.Settings.DataLoggerInterval / 1000f;
            }
        }

        /// <summary>Log a data row.</summary>
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

                        // based on MSTSSteamLocomotive.Initialize() at about line 1928, there always is at least one steam engine for a steam locomotive.
                        var firstSteamEngine = steam.SteamEngines[0];

                        Logger.Data(ConvertSpeed(Viewer.PlayerLocomotive.SpeedMpS));
                        Logger.Data(UseNativeUnits ? steam.SteamPerformanceTimeS.ToString("F1") : S.ToM(steam.SteamPerformanceTimeS).ToString("F1"));
                        Logger.Data(Viewer.PlayerLocomotive.ThrottlePercent.ToString("F0"));
                        Logger.Data(Viewer.PlayerTrain.MUReverserPercent.ToString("F0"));
                        Logger.Data(ConvertForce(Viewer.PlayerLocomotive.MotiveForceN));
                        Logger.Data(ConvertPower(steam.IndicatedHorsePowerHP, true));
                        Logger.Data(ConvertForce(steam.DrawBarPullLbsF, true));
                        Logger.Data(ConvertPower(steam.DrawbarHorsePowerHP, true));
                        Logger.Data(ConvertForce(steam.LocomotiveCouplerForceN));
                        Logger.Data(ConvertForce(steam.LocoTenderFrictionForceN));
                        Logger.Data(ConvertForce(steam.TotalFrictionForceN));
                        Logger.Data(ConvertMass(steam.TrainLoadKg, false, true));
                        Logger.Data(ConvertPressure(steam.BoilerPressurePSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogSteamChestPressurePSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogInitialPressurePSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogCutoffPressurePSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogReleasePressurePSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogBackPressurePSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.MeanEffectivePressurePSI, MainPressureUnit));
                        Logger.Data(ConvertTemp(steam.CurrentSuperheatTempF, true));
                        Logger.Data(ConvertMassConsumption(steam.CylinderSteamUsageLBpS, true));
                        Logger.Data(ConvertVolumeConsumption(steam.WaterConsumptionLbpS, true, WaterDensityKgpL));
                        Logger.Data(ConvertMassConsumption(steam.FuelBurnRateSmoothedKGpS));
                        Logger.Data(steam.SuperheaterSteamUsageFactor.ToString("F2"));
                        Logger.Data(ConvertMass(steam.CumulativeCylinderSteamConsumptionLbs, true));
                        Logger.Data(ConvertVolume(steam.CumulativeWaterConsumptionLbs, true, WaterDensityKgpL));
                        Logger.Data(steam.CutoffPressureDropRatio.ToString("F2"));
                        Logger.Data(ConvertPressure(firstSteamEngine.HPCylinderMEPPSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogLPInitialPressurePSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogLPCutoffPressurePSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogLPReleasePressurePSI, MainPressureUnit));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogLPBackPressurePSI, MainPressureUnit));
                        Logger.Data(steam.LPCutoffPressureDropRatio.ToString("F2"));
                        Logger.Data(ConvertPressure(firstSteamEngine.LPCylinderMEPPSI, MainPressureUnit));

                        Logger.End();
                    }
                }
            }
            else if (Viewer.Settings.DataLogExclusiveSteamPowerCurve)
            {
                if (Viewer.PlayerLocomotive is MSTSSteamLocomotive steam)
                {
                    var speedMpH = MpS.ToMpH(steam.SpeedMpS);
                    if (speedMpH >= PreviousLoggedSpeedMpH + 1) // Add a new record every time speed increases by 1 mph
                    {
                        PreviousLoggedSpeedMpH = (int)speedMpH; // Keep speed records close to whole numbers

                        Logger.Data(ConvertSpeed(steam.SpeedMpS));
                        Logger.Data(ConvertPower(steam.MotiveForceN * steam.SpeedMpS));
                        Logger.Data(ConvertForce(Viewer.PlayerLocomotive.MotiveForceN));
                        Logger.Data(steam.ThrottlePercent.ToString("F0"));
                        Logger.Data(steam.Train.MUReverserPercent.ToString("F0"));

                        Logger.End();
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
                if (Viewer.Settings.DataLogPhysics && Viewer.PlayerLocomotive is MSTSLocomotive loco)
                {
                    Logger.Data(loco.Direction.ToString());
                    Logger.Data(Viewer.PlayerTrain.MUReverserPercent.ToString("F0"));
                    Logger.Data(loco.ThrottlePercent.ToString("F0"));
                    Logger.Data(ConvertForce(loco.MotiveForceN));
                    Logger.Data(ConvertForce(loco.BrakeForceN));
                    Logger.Data(ConvertForce(loco.LocomotiveAxles.AxleMotiveForceN));
                    Logger.Data(ConvertPower(loco.LocomotiveAxles.AxleMotivePowerW));
                    Logger.Data(loco.LocomotiveAxles.SlipSpeedPercent.ToString("F1"));
                    Logger.Data(ConvertSpeed(loco.SpeedMpS));
                    Logger.Data(ConvertSpeed(Viewer.PlayerTrain.AllowedMaxSpeedMpS));  // loco does not have a max speed
                    Logger.Data(ConvertDistance(loco.DistanceM));
                    Logger.Data(ConvertForce(loco.GravityForceN));
                    Logger.Data(loco.TrainBrakeController?.CurrentValue.ToString("F2"));
                    Logger.Data(loco.EngineBrakeController?.CurrentValue.ToString("F2"));
                    Logger.Data(loco.BrakemanBrakeController?.CurrentValue.ToString("F2"));
                    Logger.Data(ConvertPressure(loco.BrakeSystem.GetCylPressurePSI(), MainPressureUnit));
                    Logger.Data(ConvertPressure(loco.BrakeSystem.BrakeLine1PressurePSI, MainPressureUnit));
                    Logger.Data(ConvertPressure(loco.MainResPressurePSI, MainPressureUnit));
                    if (Viewer.PlayerLocomotive is MSTSDieselLocomotive diesel)
                    {
                        // Logger.Data(ConvertPower(diesel.DieselEngines[0].AvailableTractionPowerW));  // need all engines  -- for now using axle power
                        Logger.Data(diesel.DieselEngines[0].RealRPM.ToString("F0"));
                        Logger.Data(diesel.DieselEngines[0].DemandedRPM.ToString("F0"));
                        Logger.Data(diesel.DieselEngines[0].LoadPercent.ToString("F0"));
                        Logger.Data(ConvertVolumeConsumption(diesel.DieselFlowLps));
                        Logger.Data(ConvertVolume(diesel.DieselLevelL));
                        if (diesel.DieselEngines.HasGearBox)
                        {
                            Logger.Data(diesel.DieselEngines[0].GearBox.CurrentGearIndex.ToString());
                            Logger.Data(diesel.DieselEngines[0].GearBox.NextGearIndex.ToString());
                            Logger.Data(diesel.DieselEngines[0].GearBox.ClutchPercent.ToString());
                        }
                    }
                    else if (Viewer.PlayerLocomotive is MSTSElectricLocomotive electric)
                    {
                        // Logger.Data(ConvertPower(electric.???);  -- for now using axle power
                        Logger.Data(electric.Pantographs[1].CommandUp.ToString());
                        Logger.Data(electric.Pantographs[2].CommandUp.ToString());
                        Logger.Data(electric.Pantographs.List.Count > 2 ? electric.Pantographs[3].CommandUp.ToString() : null);
                        Logger.Data(electric.Pantographs.List.Count > 3 ? electric.Pantographs[4].CommandUp.ToString() : null);
                    }
                    else if (Viewer.PlayerLocomotive is MSTSSteamLocomotive steam)
                    {
                        // Logger.Data(ConvertPower(W.FromHp(steam.IndicatedHorsePowerHP)));  -- for now using axle power
                        Logger.Data(ConvertMassConsumption(steam.BlowerSteamUsageLBpS, true));
                        Logger.Data(ConvertPressure(steam.BoilerPressurePSI, MainPressureUnit));
                        Logger.Data(steam.CylinderCocksAreOpen.ToString());
                        Logger.Data(steam.CylinderCompoundOn.ToString());
                        Logger.Data(ConvertMassConsumption(steam.EvaporationLBpS, true));
                        Logger.Data(ConvertMass(steam.FireMassKG));
                        Logger.Data(ConvertMassConsumption(steam.CylinderSteamUsageLBpS, true));
                        Logger.Data(steam.BlowerController?.CurrentValue.ToString("F0"));
                        Logger.Data(steam.DamperController?.CurrentValue.ToString("F0"));
                        Logger.Data(steam.FiringRateController?.CurrentValue.ToString("F0"));
                        Logger.Data(steam.Injector1Controller?.CurrentValue.ToString("F0"));
                        Logger.Data(steam.Injector2Controller?.CurrentValue.ToString("F0"));
                    }
                }

                Logger.End();
            }
        }

        /// <summary>Start logging data. Logs a header row. Also saves the player locomotive to detect player train (or loco) changes.</summary>
        void DataLoggerStart()
        {
            PreviousPlayerLocomotive = Viewer.PlayerLocomotive;
            PreviousLoggedSpeedMpH = -1.0f;
            if (Viewer.PlayerLocomotive is MSTSLocomotive loco) MainPressureUnit = loco.MainPressureUnit; // settings have already been applied

            // NOTE: Conditions and data here MUST match similar code in DataLoggerLog
            // Failure to update both places will result in mismatched columns or worse
            if (Viewer.Settings.DataLogExclusiveSteamPerformance)
            {
                Logger.Data($"Speed [{SpeedUnit}]");
                Logger.Data(UseNativeUnits ? "Time [S]" : "Time [M]");
                Logger.Data("Throttle [%]");
                Logger.Data("Cut-off [%]");
                Logger.Data($"ITE (MotiveForce) [{ForceUnit}]");
                Logger.Data($"IHP [{(UseNativeUnits ? FormatStrings.hp : PowerUnit)}]");
                Logger.Data($"Drawbar TE [{(UseNativeUnits ? FormatStrings.lbf : ForceUnit)}]");
                Logger.Data($"Drawbar HP [{(UseNativeUnits ? FormatStrings.hp : PowerUnit)}]");
                Logger.Data($"Coupler Force [{ForceUnit}]");
                Logger.Data($"Loco & Tender Resistance [{ForceUnit}]");
                Logger.Data($"Train Resistance [{ForceUnit}]");
                Logger.Data($"Train Load [{LargeMassUnit}]");
                Logger.Data($"Boiler Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data($"Steam Chest Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data($"Initial Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data($"Cutoff Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data($"Release Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data($"Back Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data($"MEP [{MainPressureUnit.ToString()}]");
                Logger.Data($"Superheat Temp [{(UseNativeUnits ? FormatStrings.f : TemperatureUnit)}]");
                Logger.Data($"Steam consumption [{(UseNativeUnits ? FormatStrings.lbps : MassConsumptionUnit)}]");
                Logger.Data($"Water consumption [{(UseNativeUnits ? FormatStrings.lbps : VolumeConsumptionUnit)}]");
                Logger.Data($"Coal consumption [{MassConsumptionUnit}]");
                Logger.Data("Cylinder Thermal Efficiency");
                Logger.Data($"Cumulative Steam [{(UseNativeUnits ? FormatStrings.lb : MassUnit)}]");
                Logger.Data($"Cumulative Water [{(UseNativeUnits ? FormatStrings.lb : VolumeUnit)}]");
                Logger.Data("Cutoff pressure Ratio");
                Logger.Data($"HP MEP [{MainPressureUnit.ToString()}]");
                Logger.Data($"LPInitial Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data($"LPCutoff Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data($"LPRelease Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data($"LPBack Pressure [{MainPressureUnit.ToString()}]");
                Logger.Data("LPCutoff pressure Ratio");
                Logger.Data($"LP MEP [{MainPressureUnit.ToString()}]");
            }
            else if (Viewer.Settings.DataLogExclusiveSteamPowerCurve)
            {
                Logger.Data($"Speed [{SpeedUnit}]");
                Logger.Data($"Power [{PowerUnit}]");
                Logger.Data($"ITE (MotiveForce) [{ForceUnit}]");
                Logger.Data("Throttle [%]");
                Logger.Data("Cut-off [%]");
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
                    Logger.Data($"Player Motive Force [{ForceUnit}]");
                    Logger.Data($"Player Brake Force [{ForceUnit}]");
                    Logger.Data($"Player Axle Force [{ForceUnit}]");
                    Logger.Data($"Player Axle Power [{PowerUnit}]");
                    Logger.Data("Player Wheelslip");
                    Logger.Data($"Player Speed [{SpeedUnit}]");
                    Logger.Data($"Speed Limit [{SpeedUnit}]");
                    Logger.Data($"Distance [{DistanceUnit}]");
                    Logger.Data($"Player Gravity Force [{ForceUnit}]");
                    Logger.Data("Train Brake");
                    Logger.Data("Engine Brake");
                    Logger.Data("Brakeman Brake");
                    Logger.Data($"Player Brake Cylinder [{MainPressureUnit.ToString()}]");
                    Logger.Data($"Player Brake Pipe [{MainPressureUnit.ToString()}]");
                    Logger.Data($"Player Main Res [{MainPressureUnit.ToString()}]");
                    if (Viewer.PlayerLocomotive is MSTSDieselLocomotive diesel)
                    {
                        // Logger.Data($"Available Traction Power [{PowerUnit}]");  -- for now using axle power
                        Logger.Data("Real RPM");
                        Logger.Data("Demanded RPM");
                        Logger.Data("Load [%]");
                        Logger.Data($"Fuel Flow [{VolumeConsumptionUnit}]");
                        Logger.Data($"Fuel level [{VolumeUnit}]");
                        if (diesel.DieselEngines.HasGearBox)
                        {
                            Logger.Data("Gearbox Current Gear");
                            Logger.Data("Gearbox Next Gear");
                            Logger.Data("Clutch [%]");
                        }
                    }
                    else if (Viewer.PlayerLocomotive is MSTSElectricLocomotive electric)
                    {
                        // Logger.Data($"Available Traction Power [{PowerUnit}]");  -- for now using axle power
                        Logger.Data("Panto 1");
                        Logger.Data("Panto 2");
                        Logger.Data("Panto 3");
                        Logger.Data("Panto 4");
                    }
                    else if (Viewer.PlayerLocomotive is MSTSSteamLocomotive steam)
                    {
                        // Logger.Data($"Available Traction Power [{PowerUnit}]");  -- for now using axle power
                        Logger.Data($"Blower usage [{(UseNativeUnits ? FormatStrings.lbps : MassConsumptionUnit)}]");
                        Logger.Data($"Boiler [{MainPressureUnit.ToString()}]");
                        Logger.Data("Cylinder Cocks open");
                        Logger.Data("Cylinder Compound on");
                        Logger.Data($"Evaporation [{(UseNativeUnits ? FormatStrings.lbps : MassConsumptionUnit)}]");
                        Logger.Data($"Fire Mass [{MassUnit}]");
                        Logger.Data($"Steam usage [{(UseNativeUnits ? FormatStrings.lbps : MassConsumptionUnit)}]");
                        Logger.Data("Blower");
                        Logger.Data("Damper");
                        Logger.Data("Firing Rate");
                        Logger.Data("Injector 1");
                        Logger.Data("Injector 2");
                    }
                }
            }
            Logger.End();
        }

        /// <summary>Stop logging data. Flushes the in-memory data to the dump file.</summary>
        void DataLoggerStop()
        {
            Logger.Flush();
        }

        /// <summary>Convert distance in meters to km or miles, but without appending the unit.</summary>
        string ConvertDistance(float distanceM)
        {
            if (UseNativeUnits) return distanceM.ToString("F0");
            float value = IsMetric ? Me.ToKiloM(distanceM) : Me.ToMi(distanceM);
            int precision = value > 200 ? 0 : 2;
            return string.Format(CultureInfo.CurrentCulture, $"{{0:F{precision}}}", value);
        }

        /// <summary>Convert speed in meters per second to km/h or mph, but without appending the unit.</summary>
        string ConvertSpeed(float speedMpS)
        {
            if (UseNativeUnits) return speedMpS.ToString("F1");
            float value = IsMetric ? MpS.ToKpH(speedMpS) : MpS.ToMpH(speedMpS);
            return string.Format(CultureInfo.CurrentCulture, "{0:F1}", value);
        }

        /// <summary>Convert force in Newton (or lbf) to kN or klbf, but without appending the unit.</summary>
        string ConvertForce(float force, bool isPoundForce = false)
        {
            if (UseNativeUnits) return force.ToString("F0");
            if (isPoundForce) force = N.FromLbf(force);
            float value = IsMetric ? force / 1000f : N.ToLbf(force) / 1000f;
            int precision = value > 500 ? 1 : 2;
            return string.Format(CultureInfo.CurrentCulture, $"{{0:F{precision}}}", value);
        }

        /// <summary>Convert power in Watt (or HP) to kW or HP, but without appending the unit.</summary>
        string ConvertPower(float power, bool isHorsepower = false)
        {
            if (UseNativeUnits) return power.ToString("F0");
            if (isHorsepower) power = W.FromHp(power);
            float value = IsMetric ? power / 1000f : W.ToHp(power);
            return string.Format(CultureInfo.CurrentCulture, "{0:F1}", value);
        }

        /// <summary>Convert pressure in PSI to the specified pressure unit, but without appending the unit.</summary>
        string ConvertPressure(float pressurePsi, PressureUnit outUnit)
        {
            if (UseNativeUnits) return pressurePsi.ToString("F1");
            float value = KPa.FromKPa(KPa.ToKPa(pressurePsi, PressureUnit.PSI), outUnit);
            return string.Format(CultureInfo.CurrentCulture, "{0:F2}", value);
        }

        /// <summary>Convert temperature in degree celsius (or farenheit) to celsius or farenheit, but without appending the unit.</summary>
        string ConvertTemp(float temp, bool isFarenheit = false)
        {
            if (UseNativeUnits) return temp.ToString("F0");
            if (isFarenheit) temp = C.FromF(temp);
            float value = IsMetric ? temp : C.ToF(temp);
            return string.Format(CultureInfo.CurrentCulture, "{0:F1}", value);
        }

        /// <summary>Convert mass in kg (or lb) to either kg/lb or tonnes/tons-US/tons-UK, but without appending the unit.</summary>
        string ConvertMass(float mass, bool isPounds = false, bool useLargeUnits = false)
        {
            if (UseNativeUnits) return mass.ToString("F0");
            if (isPounds) mass = Kg.FromLb(mass);
            float value;
            if (useLargeUnits) value = IsMetric ? Kg.ToTonne(mass) : IsUK ? Kg.ToTUK(mass) : Kg.ToTUS(mass);
            else value = IsMetric ? mass : Kg.ToLb(mass);
            int precision = value > 200 ? 0 : 2;
            return string.Format(CultureInfo.CurrentCulture, $"{{0:F{precision}}}", value);
        }

        /// <summary>Convert volume in liters (or mass in lb) to L/gal-us/gal-uk, but without appending the unit.</summary>
        string ConvertVolume(float volume, bool isPounds = false, float densityKgpL = WaterDensityKgpL)
        {
            if (UseNativeUnits) return volume.ToString("F0");
            if (isPounds) volume = Kg.FromLb(volume) / densityKgpL;
            float value = IsMetric ? volume : IsUK ? L.ToGUK(volume) : L.ToGUS(volume);
            return string.Format(CultureInfo.CurrentCulture, "{0:F0}", value);
        }

        /// <summary>Convert consumption in kilogram per second (or lb/s) to kg/h or lb/h, but without appending the unit.</summary>
        string ConvertMassConsumption(float consumption, bool isPounds = false)
        {
            if (UseNativeUnits) return consumption.ToString("F3");
            if (isPounds) consumption = KgpS.FromLbpS(consumption);
            float value = IsMetric ? consumption * 3600f : Kg.ToLb(consumption) * 3600f;
            return string.Format(CultureInfo.CurrentCulture, "{0:F0}", value);
        }

        /// <summary>
        /// Convert consumption in Liters per second (or lb/s) to L/h or gal-us/h or gal-uk/h, but without appending the unit.
        /// Option to convert from lb/s to volume, using specified density.
        /// </summary>
        string ConvertVolumeConsumption(float consumption, bool isPounds = false, float densityKgpL = WaterDensityKgpL)
        {
            if (UseNativeUnits) return consumption.ToString("F3");
            if (isPounds) consumption = KgpS.FromLbpS(consumption) / densityKgpL;
            float value = IsMetric ? consumption * 3600f : IsUK ? L.ToGUK(consumption) * 3600f : L.ToGUS(consumption) * 3600f;
            return string.Format(CultureInfo.CurrentCulture, "{0:F1}", value);
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
