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
using System.Globalization;
using System.Linq;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;
using Orts.Simulation.RollingStocks.SubSystems.Brakes;
using GNU.Gettext;

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
        readonly PressureUnit PressureUnitEnum = PressureUnit.PSI;

        // conversion factors not defined in Conversions.cs
        const float DieselDensityKgpL = 0.85f;
        const float BunkerCDensityKgpl = 0.95f;
        const float WaterDensityKgpL = 1.0f;

        int FrameNumber;
        double LastUpdateRealTime;   // update text message only 10 times per second
        double NextLogTime;

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

                // being paranoid, as player locomotive should always be initialized at this time
                try
                {
                    // the manual settings have already been applied to the brake system
                    if (!(Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.MainReservoir].Equals(PressureUnit.None))
                        PressureUnitEnum = (Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.MainReservoir];
                    else if (!(Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.BrakeCylinder].Equals(PressureUnit.None))
                        PressureUnitEnum = (Viewer.PlayerLocomotive as MSTSLocomotive).BrakeSystemPressureUnits[BrakeSystemComponent.BrakeCylinder];
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Player Locomotive not initialized; cannot determine pressure unit.");
                    // continue with default pressure unit
                }
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
                        Logger.Data(ConvertPressure(steam.BoilerPressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogSteamChestPressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogInitialPressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogCutoffPressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogReleasePressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogBackPressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.MeanEffectivePressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertTemp(steam.CurrentSuperheatTempF, true));
                        Logger.Data(ConvertMassConsumption(steam.CylinderSteamUsageLBpS, true));
                        Logger.Data(ConvertVolumeConsumption(steam.WaterConsumptionLbpS, true, WaterDensityKgpL));
                        Logger.Data(ConvertMassConsumption(steam.FuelBurnRateSmoothedKGpS));
                        Logger.Data(steam.SuperheaterSteamUsageFactor.ToString("F2"));
                        Logger.Data(ConvertMass(steam.CumulativeCylinderSteamConsumptionLbs, true));
                        Logger.Data(ConvertVolume(steam.CumulativeWaterConsumptionLbs, true, WaterDensityKgpL));
                        Logger.Data(steam.CutoffPressureDropRatio.ToString("F2"));
                        Logger.Data(ConvertPressure(firstSteamEngine.HPCylinderMEPPSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogLPInitialPressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogLPCutoffPressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogLPReleasePressurePSI, PressureUnitEnum));
                        Logger.Data(ConvertPressure(firstSteamEngine.LogLPBackPressurePSI, PressureUnitEnum));
                        Logger.Data(steam.LPCutoffPressureDropRatio.ToString("F2"));
                        Logger.Data(ConvertPressure(firstSteamEngine.LPCylinderMEPPSI, PressureUnitEnum));

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
                if (Viewer.Settings.DataLogPhysics)
                {
                    Logger.Data(Viewer.PlayerLocomotive.Direction.ToString());
                    Logger.Data(Viewer.PlayerTrain.MUReverserPercent.ToString("F0"));
                    Logger.Data(Viewer.PlayerLocomotive.ThrottlePercent.ToString("F0"));
                    Logger.Data(ConvertForce(Viewer.PlayerLocomotive.MotiveForceN));
                    Logger.Data(ConvertForce(Viewer.PlayerLocomotive.BrakeForceN));
                    Logger.Data(ConvertForce((Viewer.PlayerLocomotive as MSTSLocomotive).LocomotiveAxles.AxleMotiveForceN));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).LocomotiveAxles.SlipSpeedPercent.ToString("F1"));
                    Logger.Data(ConvertSpeed(Viewer.PlayerLocomotive.SpeedMpS));
                    Logger.Data(ConvertSpeed(Viewer.PlayerTrain.AllowedMaxSpeedMpS));  // loco does not have a max speed
                    Logger.Data(ConvertDistance(Viewer.PlayerLocomotive.DistanceM));
                    Logger.Data(ConvertForce(Viewer.PlayerLocomotive.GravityForceN));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).TrainBrakeController?.CurrentValue.ToString("F2"));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).EngineBrakeController?.CurrentValue.ToString("F2"));
                    Logger.Data((Viewer.PlayerLocomotive as MSTSLocomotive).BrakemanBrakeController?.CurrentValue.ToString("F2"));
                    Logger.Data(ConvertPressure(Viewer.PlayerLocomotive.BrakeSystem.GetCylPressurePSI(), PressureUnitEnum));
                    Logger.Data(ConvertPressure((Viewer.PlayerLocomotive as MSTSLocomotive).MainResPressurePSI, PressureUnitEnum));
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
                        Logger.Data(ConvertVolumeConsumption(diesel.DieselFlowLps));
                        Logger.Data(ConvertVolume(diesel.DieselLevelL));
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
                        Logger.Data(ConvertMassConsumption(steam.BlowerSteamUsageLBpS, true));
                        Logger.Data(ConvertPressure(steam.BoilerPressurePSI, PressureUnitEnum));
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

        void DataLoggerStart()
        {
            // NOTE: Conditions and data here MUST match similar code in DataLoggerLog
            // Failure to update both places will result in mismatched columns or worse
            if (Viewer.Settings.DataLogExclusiveSteamPerformance)
            {
                Logger.Data($"Speed [{SpeedUnit}]");
                Logger.Data(UseNativeUnits ? "Time [S]" : "Time [M]");
                Logger.Data("Throttle [%]");
                Logger.Data("Cut-off [%]");
                Logger.Data($"ITE (MotiveForce) [{ForceUnit}]");
                Logger.Data($"IHP [{PowerUnit}]");
                Logger.Data($"Drawbar TE [{(UseNativeUnits ? FormatStrings.lbf : ForceUnit)}]");
                Logger.Data($"Drawbar HP [{(UseNativeUnits ? FormatStrings.hp : PowerUnit)}]");
                Logger.Data($"Coupler Force [{ForceUnit}]");
                Logger.Data($"Loco & Tender Resistance [{ForceUnit}]");
                Logger.Data($"Train Resistance [{ForceUnit}]");
                Logger.Data($"Train Load [{LargeMassUnit}]");
                Logger.Data($"Boiler Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data($"Steam Chest Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data($"Initial Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data($"Cutoff Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data($"Release Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data($"Back Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data($"MEP [{PressureUnitEnum.ToString()}]");
                Logger.Data($"Superheat Temp [{(UseNativeUnits ? FormatStrings.f : TemperatureUnit)}]");
                Logger.Data($"Steam consumption [{(UseNativeUnits ? FormatStrings.lbps : MassConsumptionUnit)}]");
                Logger.Data($"Water consumption [{(UseNativeUnits ? FormatStrings.lbps : VolumeConsumptionUnit)}]");
                Logger.Data($"Coal consumption [{MassConsumptionUnit}]");
                Logger.Data("Cylinder Thermal Efficiency");
                Logger.Data($"Cumulative Steam [{(UseNativeUnits ? FormatStrings.lb : MassUnit)}]");
                Logger.Data($"Cumulative Water [{(UseNativeUnits ? FormatStrings.lb : VolumeUnit)}]");
                Logger.Data("Cutoff pressure Ratio");
                Logger.Data($"HP MEP [{PressureUnitEnum.ToString()}]");
                Logger.Data($"LPInitial Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data($"LPCutoff Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data($"LPRelease Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data($"LPBack Pressure [{PressureUnitEnum.ToString()}]");
                Logger.Data("LPCutoff pressure Ratio");
                Logger.Data($"LP MEP [{PressureUnitEnum.ToString()}]");
            }
            else if (Viewer.Settings.DataLogExclusiveSteamPowerCurve)
            {
                Logger.Data($"Speed [{SpeedUnit}]");
                Logger.Data($"Power [{PowerUnit}]");
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
                    Logger.Data("Player Wheelslip");
                    Logger.Data($"Player Speed [{SpeedUnit}]");
                    Logger.Data($"Speed Limit [{SpeedUnit}]");
                    Logger.Data($"Distance [{DistanceUnit}]");
                    Logger.Data($"Player Gravity Force [{ForceUnit}]");
                    Logger.Data("Train Brake");
                    Logger.Data("Engine Brake");
                    Logger.Data("Brakeman Brake");
                    Logger.Data($"Player Cylinder [{PressureUnitEnum.ToString()}]");
                    Logger.Data($"Player Main Res [{PressureUnitEnum.ToString()}]");
                    Logger.Data($"D:Real RPM / E:panto 1 / S:Blower usage [{(UseNativeUnits ? FormatStrings.lbps : MassConsumptionUnit)}]");
                    Logger.Data($"D:Demanded RPM / E:panto 2 / S:Boiler [{PressureUnitEnum.ToString()}]");
                    Logger.Data("D:Load [%] / E:panto 3 / S:Cylinder Cocks open");
                    Logger.Data("D:Gearbox Current Gear / E:panto 4 / S:Cylinder Compound on");
                    Logger.Data($"D:Gearbox Next Gear / E:null / S:Evaporation [{(UseNativeUnits ? FormatStrings.lbps : MassConsumptionUnit)}]");
                    Logger.Data($"D:Clutch [%] / E:null / S:Fire Mass [{MassUnit}]");
                    Logger.Data($"D:Fuel Flow [{VolumeConsumptionUnit}] / E:null / S:Steam usage [{(UseNativeUnits ? FormatStrings.lbps : MassConsumptionUnit)}]");
                    Logger.Data($"D:Fuel level [{VolumeUnit}] / E:null / S:Blower");
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
