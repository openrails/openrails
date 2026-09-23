// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// Debug for Airbrake operation - Train Pipe Leak
//#define DEBUG_TRAIN_PIPE_LEAK

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Parsers.Msts;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Orts.Simulation.RollingStocks.SubSystems.Brakes.MSTS
{
    public class AirSinglePipe : MSTSBrakeSystem
    {
        protected TrainCar Car;

        /// <summary>
        /// True if vehicle is equipped with an additional emergency brake reservoir
        /// </summary>
        public bool EmergencyReservoirPresent;
        public enum BrakeValveType
        {
            None,
            TripleValve, // Plain triple valve
            Distributor, // Triple valve with graduated release
            DistributingValve, // Triple valve + driver brake valve control. Only for locomotives
        }
        /// <summary>
        /// Type of brake valve in the car
        /// </summary>
        public BrakeValveType BrakeValve;
        /// <summary>
        /// Number of available retainer positions. (Used on freight cars, mostly.) Might be 0, 3 or 4.
        /// </summary>
        public int RetainerPositions;

        /// <summary>
        /// Indicates whether an auxiliary reservoir is present on the wagon or not.
        /// </summary>
        public bool AuxiliaryReservoirPresent;

        /// <summary>
        /// Indicates whether an additional supply reservoir is present on the wagon or not.
        /// </summary>
        public bool SupplyReservoirPresent;

        /// <summary>
        /// Indicates whether emergency braking is enforced by a electrically operated valve.
        /// </summary>
        protected bool EmergencySolenoidValve;

        readonly static float OneAtmospherePSI = 14.696f;
        protected float HandbrakePercent;
        protected float CylPressurePSI = 64;
        protected float CylAirPSIM3;
        public float AutoCylPressurePSI { get; protected set; } = 64;
        protected float AutoCylAirPSIM3;
        protected float AuxResPressurePSI = 64;
        protected float EmergResPressurePSI = 64;
        public float SupplyResPressurePSI = 64;
        protected float ControlResPressurePSI = 64;
        protected float FullServPressurePSI = 50;
        protected float MaxCylPressurePSI;
        public float ReferencePressurePSI { get; protected set; }
        protected float MaxTripleValveCylPressurePSI;
        protected float AuxResVolumeM3;
        protected float AuxCylVolumeRatio;
        protected float AuxBrakeLineVolumeRatio;
        protected float EmergBrakeLineVolumeRatio;
        protected float SupplyBrakeLineVolumeRatio;
        protected float CylBrakeLineVolumeRatio;
        protected float EmergResVolumeM3 = 0.07f;
        public float SupplyResVolumeM3 { get; protected set; }
        protected float RetainerPressureThresholdPSI;
        protected float ReleaseRatePSIpS = 1.86f;
        protected float MaxReleaseRatePSIpS = 1.86f;
        protected float MaxApplicationRatePSIpS = .9f;
        protected float MaxAuxilaryChargingRatePSIpS = 1.684f;
        protected float BrakeInsensitivityPSIpS = 0.07f;
        protected float EmergencyValveActuationRatePSIpS = 0;
        protected bool LegacyEmergencyValve = false;
        protected float EmergencyDumpValveRatePSIpS = 0;
        protected float EmergencyDumpValveTimerS = 120;
        protected float? EmergencyDumpStartTime;
        protected bool QuickActionFitted;
        protected float EmergResChargingRatePSIpS = 1.684f;
        protected float SupplyResChargingRatePSIpS;
        protected float EmergAuxVolumeRatio = 1.4f;
        protected bool RelayValveFitted = false;
        // Public to allow manipulation by freight animations
        public float RelayValveRatio = 1;
        public float RelayValveInshotPSI;
        public float EngineRelayValveRatio { get; protected set; } = 0;
        protected float EngineRelayValveInshotPSI;
        protected float RelayValveApplicationRatePSIpS = 50;
        protected float RelayValveReleaseRatePSIpS = 50;
        protected string DebugType = string.Empty;
        protected string RetainerDebugState = string.Empty;
        protected bool MRPAuxResCharging;
        protected float CylVolumeM3;
        protected float TotalCylVolumeM3;
        protected float CylPipeVolumeM3;
        protected float CylDiameterM;
        protected float CylAreaM2;
        protected float CylStrokeM = Me.FromIn(7.5f);
        protected float CurrentCylTravelM;
        protected Interpolator CylTravelTab;
        protected int CylCount = 1;
        protected bool EmergResQuickReleaseActive;
        protected float UniformChargingThresholdPSI = 3.0f;
        protected float UniformChargingRatio;
        protected bool UniformChargingActive;
        protected float UniformReleaseThresholdPSI = 3.0f;
        protected float UniformReleaseRatio;
        protected bool UniformReleaseActive;
        protected float QuickServiceLimitPSI;
        protected float QuickServiceApplicationRatePSIpS;
        protected float QuickServiceVentRatePSIpS;
        protected float QuickServiceBulbVolumeM3;
        protected float QuickServiceBulbPressurePSI;
        protected float BulbBrakeLineVolumeRatio;
        protected float AcceleratedApplicationFactor;
        protected float AcceleratedApplicationLimitPSIpS = 5.0f;
        protected float InitialApplicationThresholdPSI;
        protected float TripleValveSensitivityPSI;
        public float BrakeCylinderSpringPressurePSI { get; protected set; }
        protected float ServiceMaxCylPressurePSI;
        protected float ServiceApplicationRatePSIpS;
        protected float TwoStageLowPressurePSI;
        protected float TwoStageRelayValveRatio;
        protected float TwoStageSpeedUpMpS;
        protected float TwoStageSpeedDownMpS;
        protected bool TwoStageLowSpeedActive;
        protected float HighSpeedReducingPressurePSI;
        protected float AcceleratedEmergencyReleaseThresholdPSI = 20.0f;


        protected bool TrainBrakePressureChanging = false;
        protected bool BrakePipePressureChanging = false;
        protected float SoundTriggerCounter = 0;
        protected float PrevCylPressurePSI = 0f;
        protected float PrevBrakePipePressurePSI = 0f;
        protected float PrevBrakePipePressurePSI_sound = 0f;

        protected float BrakePipeChangePSIpS;
        protected SmoothedData SmoothedBrakePipeChangePSIpS;


        /// <summary>
        /// EP brake holding valve. Needs to be closed (Lap) in case of brake application or holding.
        /// For non-EP brake types must default to and remain in Release.
        /// </summary>
        protected ValveState HoldingValve = ValveState.Release;
        /// <summary>
        /// Valve to inhibit triple valve braking.
        /// Only closed (Lap) if EP brakes are active and they inhibit brake pipe braking.
        /// For non-EP brake types must default to and remain in Release.
        /// </summary>
        protected ValveState IsolationValve = ValveState.Release;

        public enum ValveState
        {
            [GetString("Lap")] Lap,
            [GetString("Apply")] Apply,
            [GetString("Release")] Release,
            [GetString("Emergency")] Emergency
        };
        protected ValveState TripleValveState = ValveState.Lap;

        // The reservoir from which brake cylinder air is drawn
        protected enum CylinderSource
        {
            None,
            AuxRes,
            SupplyRes,
            MainRes,
            MainResPipe,
        }
        protected CylinderSource CylSource = CylinderSource.None;

        // Current mode of operation for the quick service system
        protected enum QuickServiceMode
        {
            Release,
            PrelimQuickService,
            Service,
        }
        protected QuickServiceMode QuickServiceActive = QuickServiceMode.Release;

        // Style of quick release equipped
        protected enum QuickReleaseType
        {
            None,
            EmergencyRes, // Use emergency res for quick release
            AcceleratedReleaseRes, // Replace emergency res with a release-only reservoir
        }
        protected QuickReleaseType EmergResQuickRelease = QuickReleaseType.None;

        public AirSinglePipe(TrainCar car)
        {
            Car = car;
            // taking into account very short (fake) cars to prevent NaNs in brake line pressures
            BrakePipeVolumeM3 = (0.032f * 0.032f * (float)Math.PI / 4f) * Math.Max ( 5.0f, (1 + car.CarLengthM)); // Using DN32 (1-1/4") pipe
            DebugType = "1P";

            SmoothedBrakePipeChangePSIpS = new SmoothedData(0.25f);

            // Force graduated releasable brakes. Workaround for MSTS with bugs preventing to set eng/wag files correctly for this.
            if (Car.Simulator.Settings.GraduatedRelease) BrakeValve = BrakeValveType.Distributor;

            if (Car.Simulator.Settings.RetainersOnAllCars && !(Car is MSTSLocomotive))
                RetainerPositions = 4;
        }

        public override bool GetHandbrakeStatus()
        {
            return HandbrakePercent > 0;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            base.InitializeFromCopy(copy);
            AirSinglePipe thiscopy = (AirSinglePipe)copy;
            EmergencyReservoirPresent = thiscopy.EmergencyReservoirPresent;
            BrakeValve = thiscopy.BrakeValve;
            AuxiliaryReservoirPresent = thiscopy.AuxiliaryReservoirPresent;
            SupplyReservoirPresent = thiscopy.SupplyReservoirPresent;
            EmergencySolenoidValve = thiscopy.EmergencySolenoidValve;
            RetainerPositions = thiscopy.RetainerPositions;
            MaxCylPressurePSI = thiscopy.MaxCylPressurePSI;
            ReferencePressurePSI = thiscopy.ReferencePressurePSI;
            AuxResVolumeM3 = thiscopy.AuxResVolumeM3;
            AuxCylVolumeRatio = thiscopy.AuxCylVolumeRatio;
            AuxBrakeLineVolumeRatio = thiscopy.AuxBrakeLineVolumeRatio;
            EmergBrakeLineVolumeRatio = thiscopy.EmergBrakeLineVolumeRatio;
            SupplyBrakeLineVolumeRatio = thiscopy.SupplyBrakeLineVolumeRatio;
            CylBrakeLineVolumeRatio = thiscopy.CylBrakeLineVolumeRatio;
            EmergResVolumeM3 = thiscopy.EmergResVolumeM3;
            SupplyResVolumeM3 = thiscopy.SupplyResVolumeM3;
            BrakePipeVolumeM3 = thiscopy.BrakePipeVolumeM3;
            CylVolumeM3 = thiscopy.CylVolumeM3;
            TotalCylVolumeM3 = thiscopy.TotalCylVolumeM3;
            CylPipeVolumeM3 = thiscopy.CylPipeVolumeM3;
            CylDiameterM = thiscopy.CylDiameterM;
            CylAreaM2 = thiscopy.CylAreaM2;
            CylStrokeM = thiscopy.CylStrokeM;
            CylCount = thiscopy.CylCount;
            CylTravelTab = thiscopy.CylTravelTab == null ? null : new Interpolator(thiscopy.CylTravelTab);
            CylSource = thiscopy.CylSource;
            RetainerPressureThresholdPSI = thiscopy.RetainerPressureThresholdPSI;
            ReleaseRatePSIpS = thiscopy.ReleaseRatePSIpS;
            MaxReleaseRatePSIpS = thiscopy.MaxReleaseRatePSIpS;
            MaxApplicationRatePSIpS = thiscopy.MaxApplicationRatePSIpS;
            MaxAuxilaryChargingRatePSIpS = thiscopy.MaxAuxilaryChargingRatePSIpS;
            BrakeInsensitivityPSIpS = thiscopy.BrakeInsensitivityPSIpS;
            EmergencyValveActuationRatePSIpS = thiscopy.EmergencyValveActuationRatePSIpS;
            EmergencyDumpValveRatePSIpS = thiscopy.EmergencyDumpValveRatePSIpS;
            EmergencyDumpValveTimerS = thiscopy.EmergencyDumpValveTimerS;
            QuickActionFitted = thiscopy.QuickActionFitted;
            EmergResChargingRatePSIpS = thiscopy.EmergResChargingRatePSIpS;
            EmergAuxVolumeRatio = thiscopy.EmergAuxVolumeRatio;
            SupplyResChargingRatePSIpS = thiscopy.SupplyResChargingRatePSIpS;
            TwoPipes = thiscopy.TwoPipes;
            MRPAuxResCharging = thiscopy.MRPAuxResCharging;
            RelayValveFitted = thiscopy.RelayValveFitted;
            RelayValveRatio = thiscopy.RelayValveRatio;
            RelayValveInshotPSI = thiscopy.RelayValveInshotPSI;
            EngineRelayValveRatio = thiscopy.EngineRelayValveRatio;
            EngineRelayValveInshotPSI = thiscopy.EngineRelayValveInshotPSI;
            RelayValveApplicationRatePSIpS = thiscopy.RelayValveApplicationRatePSIpS;
            RelayValveReleaseRatePSIpS = thiscopy.RelayValveReleaseRatePSIpS;
            MaxTripleValveCylPressurePSI = thiscopy.MaxTripleValveCylPressurePSI;
            EmergResQuickRelease = thiscopy.EmergResQuickRelease;
            UniformChargingThresholdPSI = thiscopy.UniformChargingThresholdPSI;
            UniformChargingRatio = thiscopy.UniformChargingRatio;
            UniformReleaseThresholdPSI = thiscopy.UniformReleaseThresholdPSI;
            UniformReleaseRatio = thiscopy.UniformReleaseRatio;
            QuickServiceLimitPSI = thiscopy.QuickServiceLimitPSI;
            QuickServiceApplicationRatePSIpS = thiscopy.QuickServiceApplicationRatePSIpS;
            QuickServiceVentRatePSIpS = thiscopy.QuickServiceVentRatePSIpS;
            QuickServiceBulbVolumeM3 = thiscopy.QuickServiceBulbVolumeM3;
            BulbBrakeLineVolumeRatio = thiscopy.BulbBrakeLineVolumeRatio;
            AcceleratedApplicationFactor = thiscopy.AcceleratedApplicationFactor;
            AcceleratedApplicationLimitPSIpS = thiscopy.AcceleratedApplicationLimitPSIpS;
            InitialApplicationThresholdPSI = thiscopy.InitialApplicationThresholdPSI;
            TripleValveSensitivityPSI = thiscopy.TripleValveSensitivityPSI;
            BrakeCylinderSpringPressurePSI = thiscopy.BrakeCylinderSpringPressurePSI;
            ServiceMaxCylPressurePSI = thiscopy.ServiceMaxCylPressurePSI;
            ServiceApplicationRatePSIpS = thiscopy.ServiceApplicationRatePSIpS;
            TwoStageLowPressurePSI = thiscopy.TwoStageLowPressurePSI;
            TwoStageRelayValveRatio = thiscopy.TwoStageRelayValveRatio;
            TwoStageSpeedUpMpS = thiscopy.TwoStageSpeedUpMpS;
            TwoStageSpeedDownMpS = thiscopy.TwoStageSpeedDownMpS;
            HighSpeedReducingPressurePSI = thiscopy.HighSpeedReducingPressurePSI;
            LegacyEmergencyValve = thiscopy.LegacyEmergencyValve;
        }

        // Get the brake BC & BP for EOT conditions
        public override string GetStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            return $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}"
                + $" {Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true)}";
        }

        // Get Brake information for train
        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            var loco = Car as MSTSLocomotive;
            var s = $" {Simulator.Catalog.GetString("EQ")} {FormatStrings.FormatPressure(Car.Train.EqualReservoirPressurePSIorInHg, PressureUnit.PSI, units[BrakeSystemComponent.EqualizingReservoir], true)}"
                + $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(Car.Train.HUDWagonBrakeCylinderPSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}"
                + $" {Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true)}"
                + $" {Simulator.Catalog.GetString("Flow")} {FormatStrings.FormatAirFlow(Car.Train.TotalBrakePipeFlowM3pS, loco.IsMetric)}";
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += $" {Simulator.Catalog.GetString("EOT")} {lastCarBrakeSystem.GetStatus(units)}";
            if (HandbrakePercent > 0)
                s += $" {Simulator.Catalog.GetString("Handbrake")} {HandbrakePercent:F0}%";
            return s;
        }

        public override string[] GetDebugStatus(Dictionary<BrakeSystemComponent, PressureUnit> units)
        {
            return new string[] {
                DebugType,
                string.Format("{0}{1}",FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true), (Car as MSTSWagon).WheelBrakeSlideProtectionActive ? "???" : ""),
                FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true),
                FormatStrings.FormatPressure(AuxResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.AuxiliaryReservoir], true),
                EmergencyReservoirPresent ? FormatStrings.FormatPressure(EmergResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.EmergencyReservoir], true) : string.Empty,
                TwoPipes ? FormatStrings.FormatPressure(BrakeLine2PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.MainPipe], true) : string.Empty,
                BrakeValve == BrakeValveType.Distributor ? FormatStrings.FormatPressure(ControlResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.AuxiliaryReservoir], true) : string.Empty,
                SupplyReservoirPresent ? FormatStrings.FormatPressure(SupplyResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.SupplyReservoir], true) : string.Empty,
                RetainerPositions == 0 ? string.Empty : RetainerDebugState,
                Simulator.Catalog.GetString(GetStringAttribute.GetPrettyName(TripleValveState)),
                string.Empty, // Spacer because the state above needs 2 columns.
                HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                string.Format("A{0} B{1}", AngleCockAOpenAmount >= 1 ? "+" : AngleCockAOpenAmount <= 0 ? "-" : "/", AngleCockBOpenAmount >= 1 ? "+" : AngleCockBOpenAmount <= 0 ? "-" : "/"),
                BleedOffValveOpen ? Simulator.Catalog.GetString("Open") : string.Empty,
            };

        }

        public override float GetCylPressurePSI()
        {
            return CylPressurePSI;
        }

        public override float GetCylVolumeM3()
        {
            return CylVolumeM3;
        }
        public override float GetTotalCylVolumeM3()
        {
            return TotalCylVolumeM3;
        }

        public override float GetNormalizedCylTravel()
        {
            return CurrentCylTravelM / CylStrokeM;
        }

        public float GetFullServPressurePSI()
        {
            return FullServPressurePSI;
        }

        public float GetMaxCylPressurePSI()
        {
            return MaxCylPressurePSI;
        }

        public float GetAuxCylVolumeRatio()
        {
            return AuxCylVolumeRatio;
        }

        public float GetMaxReleaseRatePSIpS()
        {
            return MaxReleaseRatePSIpS;
        }

        public float GetMaxApplicationRatePSIpS()
        {
            return MaxApplicationRatePSIpS;
        }

        public override float GetVacResPressurePSI()
        {
            return 0;
        }

        public override float GetVacResVolume()
        {
            return 0;
        }
        public override float GetVacBrakeCylNumber()
        {
            return 0;
        }
        public override void SetBrakeEquipment(List<string> equipment)
        {
            base.SetBrakeEquipment(equipment);
            if (equipment.Contains("distributor") || equipment.Contains("graduated_release_triple_valve")) BrakeValve = BrakeValveType.Distributor;
            else if (equipment.Contains("triple_valve")) BrakeValve = BrakeValveType.TripleValve;
            else if (equipment.Contains("distributing_valve")) BrakeValve = BrakeValveType.DistributingValve;
            else BrakeValve = BrakeValveType.None;
            EmergencyReservoirPresent = equipment.Contains("emergency_brake_reservoir");
            AuxiliaryReservoirPresent = equipment.Contains("auxiliary_reservoir");
            AuxiliaryReservoirPresent |= equipment.Contains("auxilary_reservoir"); // MSTS legacy parameter - use is discouraged
            if (equipment.Contains("retainer_4_position")) RetainerPositions = 4;
            else if (equipment.Contains("retainer_3_position")) RetainerPositions = 3;
            else RetainerPositions = 0;
            SupplyReservoirPresent = equipment.Contains("supply_reservoir");
            EmergencySolenoidValve = equipment.Contains("emergency_solenoid_valve");
        }
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxCylPressurePSI = AutoCylPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(triplevalveratio": AuxCylVolumeRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(brakedistributorreleaserate":
                case "wagon(maxreleaserate": MaxReleaseRatePSIpS = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(brakedistributorapplicationrate":
                case "wagon(maxapplicationrate": MaxApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxauxilarychargingrate": MaxAuxilaryChargingRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(emergencyreschargingrate": EmergResChargingRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(emergencyresvolumemultiplier": EmergAuxVolumeRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(emergencyrescapacity": EmergResVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                
                // OpenRails specific parameters
                case "wagon(brakepipevolume": BrakePipeVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                case "wagon(ortsbrakeforcereferencepressure": ReferencePressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortsauxilaryrescapacity":
                case "wagon(ortsauxiliaryrescapacity": AuxResVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                case "wagon(ortsbrakeinsensitivity": BrakeInsensitivityPSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, 0.07f); break;
                case "wagon(ortsemergencyvalveactuationrate": EmergencyValveActuationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, 15f); break;
                case "wagon(ortsemergencydumpvalverate": EmergencyDumpValveRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, 15f); break;
                case "wagon(ortsemergencydumpvalvetimer": EmergencyDumpValveTimerS = stf.ReadFloatBlock(STFReader.UNITS.Time, 120.0f); break;
                case "wagon(ortsemergencyquickaction": QuickActionFitted = stf.ReadBoolBlock(false); break;
                case "wagon(ortsmainrespipeauxrescharging": MRPAuxResCharging = TwoPipes && stf.ReadBoolBlock(true); break;
                case "wagon(ortsbrakerelayvalveratio":
                    RelayValveRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null);
                    if (RelayValveRatio != 0)
                    {
                        RelayValveFitted = true;
                    }
                    else
                    {
                        RelayValveRatio = 1;
                        RelayValveFitted = false;
                    }
                    break;
                case "wagon(ortsbrakerelayvalveinshot": RelayValveInshotPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortsenginebrakerelayvalveratio": EngineRelayValveRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsenginebrakerelayvalveinshot": EngineRelayValveInshotPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortsbrakerelayvalveapplicationrate": RelayValveApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(ortsbrakerelayvalvereleaserate": RelayValveReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(ortsmaxtriplevalvecylinderpressure": MaxTripleValveCylPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortsbrakecylindervolume": CylVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                case "wagon(ortsbrakecylinderpipingvolume": CylPipeVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                case "wagon(ortsbrakecylindersize":
                case "wagon(ortsbrakecylinderdiameter": CylDiameterM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsbrakecylinderpistontravel": CylStrokeM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsnumberbrakecylinders": CylCount = stf.ReadIntBlock(null); break;
                case "wagon(ortsemergencyresquickrelease":
                    EmergResQuickRelease = (QuickReleaseType)stf.ReadIntBlock(0);
                    if (EmergResQuickRelease == QuickReleaseType.AcceleratedReleaseRes)
                        EmergencyReservoirPresent = true; // Emergency res emulated accelerated release res
                    break;
                case "wagon(ortsuniformchargingthreshold": UniformChargingThresholdPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 3.0f); break;
                case "wagon(ortsuniformchargingratio": UniformChargingRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsuniformreleasethreshold": UniformReleaseThresholdPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 3.0f); break;
                case "wagon(ortsuniformreleaseratio": UniformReleaseRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsquickservicelimit": QuickServiceLimitPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortsquickserviceapplicationrate": QuickServiceApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(ortsquickserviceventrate": QuickServiceVentRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(ortsacceleratedapplicationfactor": AcceleratedApplicationFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsacceleratedapplicationmaxventrate": AcceleratedApplicationLimitPSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, 5.0f); break;
                case "wagon(ortsinitialapplicationthreshold": InitialApplicationThresholdPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortscylinderspringpressure": BrakeCylinderSpringPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortsmaxservicecylinderpressure": ServiceMaxCylPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortsmaxserviceapplicationrate": ServiceApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(ortstwostagelowpressure": TwoStageLowPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortstwostagerelayvalveratio": TwoStageRelayValveRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortstwostageincreasingspeed": TwoStageSpeedUpMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); break;
                case "wagon(ortstwostagedecreasingspeed": TwoStageSpeedDownMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); break;
                case "wagon(ortshighspeedreducingpressure": HighSpeedReducingPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "engine(ortssupplyrescapacity":
                case "wagon(ortssupplyrescapacity": SupplyResVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                case "engine(ortssupplyreschargingrate":
                case "wagon(ortssupplyreschargingrate": SupplyResChargingRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(HandbrakePercent);
            outf.Write(ReleaseRatePSIpS);
            outf.Write(RetainerPressureThresholdPSI);
            outf.Write(RetainerDebugState);
            outf.Write(CylAirPSIM3);
            outf.Write(CylPressurePSI);
            outf.Write(AutoCylAirPSIM3);
            outf.Write(AutoCylPressurePSI);
            outf.Write(CurrentCylTravelM);
            outf.Write(AuxResPressurePSI);
            outf.Write(EmergResPressurePSI);
            outf.Write(SupplyResPressurePSI);
            outf.Write(ControlResPressurePSI);
            outf.Write(FullServPressurePSI);
            outf.Write((int)TripleValveState);
            outf.Write(FrontBrakeHoseConnected);
            outf.Write(RearBrakeHoseConnected);
            outf.Write(AngleCockAOpen);
            outf.Write(AngleCockAOpenAmount);
            outf.Write(AngleCockBOpen);
            outf.Write(AngleCockBOpenAmount);
            outf.Write(BleedOffValveOpen);
            outf.Write((int)HoldingValve);
            outf.Write((int)IsolationValve);
            outf.Write(RelayValveRatio);
            outf.Write(RelayValveInshotPSI);
            outf.Write(UniformChargingActive);
            outf.Write(UniformReleaseActive);
            outf.Write((int)QuickServiceActive);
            outf.Write(QuickServiceBulbPressurePSI);
            outf.Write(EmergResQuickReleaseActive);
            outf.Write(TwoStageLowSpeedActive);
            outf.Write(LegacyEmergencyValve);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            HandbrakePercent = inf.ReadSingle();
            ReleaseRatePSIpS = inf.ReadSingle();
            RetainerPressureThresholdPSI = inf.ReadSingle();
            RetainerDebugState = inf.ReadString();
            CylAirPSIM3 = inf.ReadSingle();
            CylPressurePSI = inf.ReadSingle();
            AutoCylAirPSIM3 = inf.ReadSingle();
            AutoCylPressurePSI = inf.ReadSingle();
            CurrentCylTravelM = inf.ReadSingle();
            AuxResPressurePSI = inf.ReadSingle();
            EmergResPressurePSI = inf.ReadSingle();
            SupplyResPressurePSI = inf.ReadSingle();
            ControlResPressurePSI = inf.ReadSingle();
            FullServPressurePSI = inf.ReadSingle();
            TripleValveState = (ValveState)inf.ReadInt32();
            FrontBrakeHoseConnected = inf.ReadBoolean();
            RearBrakeHoseConnected = inf.ReadBoolean();
            AngleCockAOpen = inf.ReadBoolean();
            AngleCockAOpenAmount = inf.ReadSingle();
            AngleCockBOpen = inf.ReadBoolean();
            AngleCockBOpenAmount = inf.ReadSingle();
            BleedOffValveOpen = inf.ReadBoolean();
            HoldingValve = (ValveState)inf.ReadInt32();
            IsolationValve = (ValveState)inf.ReadInt32();
            RelayValveRatio = inf.ReadSingle();
            RelayValveInshotPSI = inf.ReadSingle();
            UniformChargingActive = inf.ReadBoolean();
            UniformReleaseActive = inf.ReadBoolean();
            QuickServiceActive = (QuickServiceMode)inf.ReadInt32();
            QuickServiceBulbPressurePSI = inf.ReadSingle();
            EmergResQuickReleaseActive = inf.ReadBoolean();
            TwoStageLowSpeedActive = inf.ReadBoolean();
            LegacyEmergencyValve = inf.ReadBoolean();
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            MSTSLocomotive loco = Car as MSTSLocomotive;

            BrakeLine1PressurePSI = Car.Train.EqualReservoirPressurePSIorInHg;
            BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
            // Initialize locomotive brakes
            if (loco != null && Car.Train.LeadLocomotive is MSTSLocomotive lead)
            {
                bool brakeLine3Init = false;

                if (loco == lead) // Always initialize loco brakes on lead loco
                    brakeLine3Init = true;
                else
                {
                    foreach (List<TrainCar> group in Car.Train.LocoGroups)
                    {
                        if (group.Contains(loco))
                        {
                            if (group.Contains(lead))
                                brakeLine3Init = true; // Always initialize loco brakes on locos in same group as lead loco
                            else if (loco.DPSyncIndependent && lead.DPSyncIndependent)
                                brakeLine3Init = true; // Otherwise, only initialize loco brakes if synchronized by DP system

                            break;
                        }
                    }
                }

                if (brakeLine3Init) // Sync loco brakes with lead brake system
                    lead.EngineBrakeController?.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, 1000);
                else // Release loco brakes
                    BrakeLine3PressurePSI = 0.0f;
            }
            if (maxPressurePSI > 0)
                ControlResPressurePSI = maxPressurePSI;

            FullServPressurePSI = fullServPressurePSI;
            AutoCylAirPSIM3 = immediateRelease ? 0 : (maxPressurePSI - BrakeLine1PressurePSI) * AuxResVolumeM3;
            QuickServiceBulbPressurePSI = BrakeLine1PressurePSI < maxPressurePSI ? BrakeLine1PressurePSI : 0;
            if (CylSource == CylinderSource.AuxRes)
                AutoCylPressurePSI = ForceBrakeCylinderPressure(ref AutoCylAirPSIM3, AdvancedBrakeCylinderPressure(AutoCylAirPSIM3));
            else
                AutoCylPressurePSI = immediateRelease ? 0 : Math.Min((maxPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio, MaxTripleValveCylPressurePSI);
            CylPressurePSI = ForceBrakeCylinderPressure(ref CylAirPSIM3, Math.Max(AutoCylPressurePSI * RelayValveRatio, BrakeLine3PressurePSI * EngineRelayValveRatio));

            AuxResPressurePSI = Math.Max(BrakeValve == BrakeValveType.Distributor
                && TwoPipes && MRPAuxResCharging && !SupplyReservoirPresent ?
                maxPressurePSI : maxPressurePSI - (AutoCylAirPSIM3 / AuxResVolumeM3), BrakeLine1PressurePSI);
            if (EmergencyReservoirPresent)
                EmergResPressurePSI = Math.Max(AuxResPressurePSI, maxPressurePSI);
            if (SupplyReservoirPresent)
                SupplyResPressurePSI = Math.Max(maxPressurePSI, MRPAuxResCharging && TwoPipes ? BrakeLine2PressurePSI : 0);

            TripleValveState = AutoCylPressurePSI < 1 ? ValveState.Release : ValveState.Lap;
            HoldingValve = ValveState.Release;
            IsolationValve = ValveState.Release;
            HandbrakePercent = handbrakeOn & HandBrakePresent ? 100 : 0;
            SetRetainer(RetainerSetting.Exhaust);
            if (loco != null) 
                loco.MainResPressurePSI = loco.MaxMainResPressurePSI;

            // Prevent initialization triggering emergency vent valves
            PrevBrakePipePressurePSI = BrakeLine1PressurePSI;
            BrakePipeChangePSIpS = 0;
            SmoothedBrakePipeChangePSIpS.ForceSmoothValue(0);
        }

        public override void Initialize()
        {
            // reducing size of Emergency Reservoir for short (fake) cars
            if (Car.Simulator.Settings.CorrectQuestionableBrakingParams && Car.CarLengthM <= 1)
                EmergResVolumeM3 = Math.Min (0.02f, EmergResVolumeM3);

            // Install a plain triple valve if no brake valve defined
            // Do not install it for tenders if not defined, to allow tenders with straight brake only
            if (Car.Simulator.Settings.CorrectQuestionableBrakingParams && BrakeValve == BrakeValveType.None && (Car as MSTSWagon).WagonType != TrainCar.WagonTypes.Tender)
            {
                BrakeValve = BrakeValveType.TripleValve;
                Trace.TraceWarning("{0} does not define a brake valve, defaulting to a plain triple valve", (Car as MSTSWagon).WagFilePath);
            }

            // Reducing reservoir charging rates when set unrealistically high
            if (Car.Simulator.Settings.CorrectQuestionableBrakingParams && (MaxAuxilaryChargingRatePSIpS > 2.5f || EmergResChargingRatePSIpS > 2.5f))
            {
                MaxAuxilaryChargingRatePSIpS = Math.Min(MaxAuxilaryChargingRatePSIpS, 2.5f);
                EmergResChargingRatePSIpS = Math.Min(EmergResChargingRatePSIpS, 2.5f);
            }

            // In simple brake mode set emergency reservoir volume, override high volume values to allow faster brake release.
            if (Car.Simulator.Settings.SimpleControlPhysics && EmergResVolumeM3 > 2.0)
                EmergResVolumeM3 = 0.7f;

            // Determine pressure at which the value of MaxBrakeForce applies
            if (ReferencePressurePSI <= 0)
                ReferencePressurePSI = MaxCylPressurePSI;
            // If reference pressure cannot be determined still, assume 64 psi
            if (ReferencePressurePSI <= 0)
                ReferencePressurePSI = 64.0f;
            // If max cylinder pressure has not been set, assume the system has no limit on pressure
            if (MaxCylPressurePSI <= 0)
                MaxCylPressurePSI = float.PositiveInfinity;

            // Remove brake cylinder pressure limit if set questionably low
            // In MSTS, there was no limit on brake cylinder pressure
            if (Car.Simulator.Settings.CorrectQuestionableBrakingParams && MaxCylPressurePSI <= 50.0f)
                MaxCylPressurePSI = float.PositiveInfinity;

            // Set default values for any optional tokens that are unset
            if (MaxTripleValveCylPressurePSI <= 0)
                MaxTripleValveCylPressurePSI = MaxCylPressurePSI / RelayValveRatio;
            if (ServiceMaxCylPressurePSI <= 0)
                ServiceMaxCylPressurePSI = MaxTripleValveCylPressurePSI;
            if (QuickServiceLimitPSI > MaxTripleValveCylPressurePSI)
                QuickServiceLimitPSI = MaxTripleValveCylPressurePSI;
            if (EngineRelayValveRatio <= 0)
                EngineRelayValveRatio = RelayValveRatio;
            if (ServiceApplicationRatePSIpS <= 0)
                ServiceApplicationRatePSIpS = MaxApplicationRatePSIpS;

            if (EmergencyReservoirPresent && EmergencyValveActuationRatePSIpS == 0)
            {
                EmergencyValveActuationRatePSIpS = 15;
                LegacyEmergencyValve = true;
            }

            if (InitialApplicationThresholdPSI == 0)
            {
                if (BrakeValve == BrakeValveType.Distributor)
                    InitialApplicationThresholdPSI = 2.2f; // UIC spec: brakes should release if brake pipe is within 0.15 bar of control res
                else
                    InitialApplicationThresholdPSI = 1.0f;
            }

            if (TripleValveSensitivityPSI == 0)
            {
                if (BrakeValve == BrakeValveType.Distributor)
                    TripleValveSensitivityPSI = 1.4f; // UIC spec: brakes should respond to 0.1 bar changes in brake pipe
                else
                    TripleValveSensitivityPSI = 1.0f;
            }

            if (AuxResVolumeM3 != 0)
                EmergAuxVolumeRatio = EmergResVolumeM3 / AuxResVolumeM3;
            else if (AuxResVolumeM3 == 0 && EmergAuxVolumeRatio > 0)
                AuxResVolumeM3 = EmergResVolumeM3 / EmergAuxVolumeRatio;

            // Initialize brake cylinder volume from given quantities
            // If diameter is defined, we assume this is an 'advanced' brake cylinder, which requires extra steps
            if (CylDiameterM > 0)
            {
                CylAreaM2 = (float)((Math.PI * (CylDiameterM * CylDiameterM) / 4.0f));
                CylVolumeM3 = CylAreaM2 * CylStrokeM;

                // Estimate the piping volume if it has not been defined yet
                // Piping volume cannot be 0 as this can cause division by 0
                if (CylPipeVolumeM3 <= 0)
                {
                    if (AuxCylVolumeRatio > 0 && !(SupplyReservoirPresent || Car is MSTSLocomotive))
                    {   // User has defined a triple valve ratio and the cylinder will be drawing air from the aux res
                        // Set piping volume to produce expected pressure for the triple valve ratio
                        // Assuming 70 psi as brake pipe pressure
                        float nomPipePressurePSI = 70.0f;
                        float nomCylPressurePSI = nomPipePressurePSI * (AuxCylVolumeRatio / (AuxCylVolumeRatio + 1.0f));

                        float tempPipeVolumeM3 = (-(CylVolumeM3 * OneAtmospherePSI) - nomCylPressurePSI * (CylVolumeM3 + AuxResVolumeM3) + (nomPipePressurePSI * AuxResVolumeM3))
                            / (CylCount * nomCylPressurePSI);

                        // Piping volume must be at least 5% of full cylinder volume
                        CylPipeVolumeM3 = Math.Max(tempPipeVolumeM3, 0.05f * CylVolumeM3);

                        // Produce a warning if the brake configuration will be unable to achieve desired pressure due to aux res too small
                        // If car has a supply res or a main res, aux res size doesn't matter
                        if (tempPipeVolumeM3 < 0.05f * CylVolumeM3)
                            Trace.TraceWarning($"Auxiliary reservoir on car {Car.WagFilePath} appears to be too small for the brake cylinder(s). " +
                                $"Consider increasing the auxiliary reservoir size, reducing cylinder size, or using a supply reservoir to provide sufficient cylinder pressures.");
                    }
                    else // Assume piping volume is 20% of full cylinder volume
                        CylPipeVolumeM3 = 0.20f * CylVolumeM3; 
                }

                // Advanced brake cylinder sim needs a spring pressure defined, otherwise it can remain 0
                if (BrakeCylinderSpringPressurePSI <= 0)
                    BrakeCylinderSpringPressurePSI = 5.0f;

                // Precalculate the relationship between air in cylinder lines and cylinder travel
                if (CylTravelTab == null) // May have been initialized already
                {
                    // Assumptions from PRR characteristic curve:
                    // pg 40: https://ia600906.us.archive.org/24/items/braketestsreport00penn/braketestsreport00penn.pdf
                    // Brake cylinder reaches rated travel at 50 psi
                    // Brake cylinder travel is 80% of nominal when pressure reaches the spring counter-pressure
                    // Other assumptions:
                    // Brake cylinder travel remains at 0 until reaching half the spring counter-pressure
                    // Brake cylinder travel is linear w.r.t. total air in the cylinder line, not entirely accurate but greatly simplifies code
                    float[] airPSIM3 = new float[7];
                    float[] cylTravelM = new float[7];

                    float nomPSI = 50.0f;

                    // Prevent interpolator error. Max cyl pressure and nominal pressure need to be slightly offset
                    if (ReferencePressurePSI == nomPSI)
                        nomPSI -= 5.0f;
                    // Prevent interpolator error. Spring pressure should never be this large
                    if (BrakeCylinderSpringPressurePSI >= nomPSI)
                        BrakeCylinderSpringPressurePSI = nomPSI - 5.0f;

                    // 0 cylinder travel with 0 air in cylinder
                    cylTravelM[0] = 0;
                    airPSIM3[0] = AdvancedBrakeCylinderAir(0, true);
                    // 0 cylinder travel when pressure is insufficient to budge spring
                    cylTravelM[1] = 0;
                    airPSIM3[1] = AdvancedBrakeCylinderAir(BrakeCylinderSpringPressurePSI / 2.0f, true);
                    // 80% cylinder travel when spring pressure is fully overcome
                    cylTravelM[2] = CylStrokeM * 0.8f;
                    airPSIM3[2] = AdvancedBrakeCylinderAir(BrakeCylinderSpringPressurePSI, true);
                    // 100% cylinder travel at 50 psi, with linear change in travel between 50 psi and max pressure
                    // This will work whether the max cylinder pressure is above or below 50 psi
                    float strokePerPsi = (CylStrokeM - cylTravelM[2]) / (50.0f - BrakeCylinderSpringPressurePSI);

                    cylTravelM[3] = (strokePerPsi * (Math.Min(nomPSI, ReferencePressurePSI) - 50.0f)) + CylStrokeM;
                    airPSIM3[3] = AdvancedBrakeCylinderAir(Math.Min(nomPSI, ReferencePressurePSI), true);

                    cylTravelM[4] = (strokePerPsi * (Math.Max(nomPSI, ReferencePressurePSI) - 50.0f)) + CylStrokeM;
                    airPSIM3[4] = AdvancedBrakeCylinderAir(Math.Max(nomPSI, ReferencePressurePSI), true);
                    // Absolute maximum cylinder travel limited to 160% of nominal
                    cylTravelM[5] = CylStrokeM * 1.6f;
                    airPSIM3[5] = AdvancedBrakeCylinderAir((cylTravelM[5] - cylTravelM[2]) / strokePerPsi , true);

                    cylTravelM[6] = cylTravelM[5];
                    airPSIM3[6] = airPSIM3[5] * 2.0f;

                    CylTravelTab = new Interpolator(airPSIM3, cylTravelM);

                }
            }
            if (CylVolumeM3 <= 0)
            {
                if (AuxCylVolumeRatio > 0)
                    CylVolumeM3 = AuxResVolumeM3 / AuxCylVolumeRatio / CylCount;
                else
                    CylVolumeM3 = AuxResVolumeM3 / 2.5f / CylCount;
            }

            TotalCylVolumeM3 = (CylVolumeM3 + CylPipeVolumeM3) * CylCount;

            // Assume supply res matches aux res specs if supply res has been poorly defined
            if (SupplyReservoirPresent && SupplyResChargingRatePSIpS == 0)
                SupplyResChargingRatePSIpS = MaxAuxilaryChargingRatePSIpS;
            if (SupplyReservoirPresent && SupplyResVolumeM3 == 0)
                SupplyResVolumeM3 = AuxResVolumeM3;

            // Determine ratios relative to the brake pipe volume
            if (BrakePipeVolumeM3 > 0)
            {
                AuxBrakeLineVolumeRatio = AuxResVolumeM3 / BrakePipeVolumeM3;
                EmergBrakeLineVolumeRatio = EmergResVolumeM3 / BrakePipeVolumeM3;
                SupplyBrakeLineVolumeRatio = SupplyResVolumeM3 / BrakePipeVolumeM3;
                CylBrakeLineVolumeRatio = TotalCylVolumeM3 / BrakePipeVolumeM3;
            }
            else
            {
                AuxBrakeLineVolumeRatio = 3.1f;
                EmergBrakeLineVolumeRatio = 4.34f;
                SupplyBrakeLineVolumeRatio = 3.1f;
                CylBrakeLineVolumeRatio = 1.24f;
            }

            // If user specified only one two stage speed, set the other to be equal
            if (TwoStageSpeedDownMpS == 0 && TwoStageSpeedUpMpS > 0)
                TwoStageSpeedDownMpS = TwoStageSpeedUpMpS;
            else if (TwoStageSpeedUpMpS == 0 && TwoStageSpeedDownMpS > 0)
                TwoStageSpeedUpMpS = TwoStageSpeedDownMpS;
            // If speeds are set nonsensically, swap them
            else if (TwoStageSpeedUpMpS < TwoStageSpeedDownMpS)
                (TwoStageSpeedUpMpS, TwoStageSpeedDownMpS) = (TwoStageSpeedDownMpS, TwoStageSpeedUpMpS);
            if (TwoStageLowPressurePSI == 0)
                TwoStageLowPressurePSI = MaxCylPressurePSI;
            // If relay valve ratio isn't used, assume it doesn't change
            if (TwoStageRelayValveRatio == 0)
                TwoStageRelayValveRatio = RelayValveRatio;
            RelayValveFitted |= (Car is MSTSLocomotive loco && (loco.DynamicBrakeAutoBailOff || loco.DynamicBrakePartialBailOff || loco.DynamicBrakeEngineBrakeReplacement)) ||
                BrakeValve == BrakeValveType.DistributingValve || SupplyReservoirPresent ||
                TwoStageRelayValveRatio != RelayValveRatio || RelayValveInshotPSI != 0 || EngineRelayValveInshotPSI != 0;

            if (AuxCylVolumeRatio <= 0 && RelayValveFitted)
                AuxCylVolumeRatio = 2.5f;
            else if (AuxCylVolumeRatio <= 0)
                AuxCylVolumeRatio = AuxResVolumeM3 / TotalCylVolumeM3;

            // Determine size of quick service bulb required to achieve expected quick service "gulp"
            // Reduction in brake pipe pressure due to transfer of air to quick service bulb should be just enough to apply brakes, no more
            if (QuickServiceVentRatePSIpS > 0.0f && QuickServiceLimitPSI > 0.0f && QuickServiceBulbVolumeM3 <= 0.0f)
            {
                // Need to assume a brake pipe pressure at start of application
                float pipePSI = 90.0f - InitialApplicationThresholdPSI;

                // Estimate that 2 psi reduction in pressure required of quick service gulp to propagate an initial application
                float quickServiceDropPSI = 2.0f;

                QuickServiceBulbVolumeM3 = BrakePipeVolumeM3 * (quickServiceDropPSI / (pipePSI - quickServiceDropPSI));

                if (QuickServiceBulbVolumeM3 < 0.0f)
                    QuickServiceBulbVolumeM3 = 0.0f;

                BulbBrakeLineVolumeRatio = QuickServiceBulbVolumeM3 / BrakePipeVolumeM3;
            }

            // Determine the air source for the brake cylinders
            if (SupplyReservoirPresent)
                CylSource = CylinderSource.SupplyRes;
            else if (Car is MSTSLocomotive)
                CylSource = CylinderSource.MainRes;
            else if (RelayValveRatio > 1.0f && TwoPipes)
                CylSource = CylinderSource.MainResPipe;
            else if ((RelayValveRatio > 1.0f && !TwoPipes) || Car.WagonType == TrainCar.WagonTypes.Tender)
                CylSource = CylinderSource.None;
            else
                CylSource = CylinderSource.AuxRes;
        }

        /// <summary>
        /// Used when initial speed > 0
        /// </summary>
        public override void InitializeMoving ()
        {
            Initialize(false, 0, FullServPressurePSI, true);
        }

        public override void LocoInitializeMoving() // starting conditions when starting speed > 0
        {
        }

        /// <summary>
        /// Returns a brake cylinder pressure when given the amount of air in the
        /// brake cylinder (as pressure * volume) as a reference variable, a change
        /// in pressure, and target pressure.
        /// </summary>
        public float CalculateBrakeCylinderPressure(ref float airPSIM3, float dp, float target)
        {
            float currentAir = airPSIM3 + (dp * TotalCylVolumeM3);
            float pressurePSI = AdvancedBrakeCylinderPressure(currentAir);
            bool recalculate = false;

            if (dp > 0)
            {
                if (pressurePSI > MaxCylPressurePSI + 0.1f)
                {
                    currentAir = AdvancedBrakeCylinderAir(MaxCylPressurePSI);
                    recalculate = true;
                }
                else if (pressurePSI > target + 0.1f)
                {
                    currentAir = AdvancedBrakeCylinderAir(target);
                    recalculate = true;
                }
            }
            else if (dp < 0)
            {
                if (pressurePSI < 0)
                {
                    currentAir = 0;
                    recalculate = true;
                }
                else if (pressurePSI < target - 0.1f)
                {
                    currentAir = AdvancedBrakeCylinderAir(target);
                    recalculate = true;
                }
            }
            // Recalculate pressure if any corrections were required
            if (recalculate)
                pressurePSI = AdvancedBrakeCylinderPressure(currentAir);

            airPSIM3 = currentAir;
            return pressurePSI;
        }

        /// <summary>
        /// Returns a brake cylinder pressure when given a reference to the amount of air in the
        /// brake cylinder (as pressure * volume) and a pressure to attempt to set the brake cylinder to.
        /// </summary>
        public float ForceBrakeCylinderPressure(ref float airPSIM3, float pressure)
        {
            float currentAir;

            if (pressure <= 0)
            {
                airPSIM3 = 0;
                return 0;
            }
            else if (pressure > MaxCylPressurePSI)
                pressure = MaxCylPressurePSI;

            currentAir = AdvancedBrakeCylinderAir(pressure);
            pressure = AdvancedBrakeCylinderPressure(currentAir);

            airPSIM3 = currentAir;
            return pressure;
        }

        /// <summary>
        /// Returns the brake cylinder pressure that would result when the given amount of air,
        /// in terms of pressure * volume, is stored in the brake cylinder. Most useful for the
        /// advanced brake cylinder simulation.
        /// </summary>
        public float AdvancedBrakeCylinderPressure(float airPSIM3)
        {
            // Advanced brake cylinder simulation: Only use if user has entered a diameter, and train isn't AI
            // This is an estimate and may not perfectly reverse the AdvancedBrakeCylinderAir function
            if (CylDiameterM > 0 && Car.Train.IsPlayerDriven)
            {
                // Accounts for dynamic changes in cylinder volume due to changing cylinder travel
                float cylinderDisplacementM3 = CylTravelTab[airPSIM3] * CylAreaM2;

                // Need to consider extra air required to displace brake cylinder, hence subtracting by (1 atm * displacement)
                return ((airPSIM3 / CylCount) - (cylinderDisplacementM3 * OneAtmospherePSI)) / (CylPipeVolumeM3 + cylinderDisplacementM3);
            }
            else
                return airPSIM3 / TotalCylVolumeM3;
        }

        /// <summary>
        /// Returns the brake cylinder air, in terms of pressure * volume, that would result when
        /// the given pressure is stored in the brake cylinder, plus an optional bool (default false)
        /// to force the advanced brake cylinder calculation no matter what. Most useful for the
        /// advanced brake cylinder simulation.
        /// </summary>
        public float AdvancedBrakeCylinderAir(float pressurePSI, bool alwaysAdvanced = false)
        {
            // Advanced brake cylinder simulation: Only use if user has entered a diameter, and train isn't AI
            if (alwaysAdvanced || (CylDiameterM > 0 && Car.Train.IsPlayerDriven))
            {
                // Assumed that cylinder travel is piecewise linear versus pressure
                float cylinderTravelM = CylStrokeM * (0.8f * MathHelper.Clamp(((2 * pressurePSI) / BrakeCylinderSpringPressurePSI) - 1.0f, 0.0f, 1.0f)
                    + (0.2f * Math.Max((pressurePSI - BrakeCylinderSpringPressurePSI) / (50.0f - BrakeCylinderSpringPressurePSI), 0.0f)));

                // Need to consider extra air required to displace brake cylinder, hence adding (1 atm * displacement)
                return ((CylPipeVolumeM3 * pressurePSI) + (cylinderTravelM * CylAreaM2) * (pressurePSI + OneAtmospherePSI)) * CylCount;
            }
            else
                return pressurePSI * TotalCylVolumeM3;
        }

        public void UpdateTripleValveState(float elapsedClockSeconds)
        {
            var prevState = TripleValveState;
            var valveType = BrakeValve;
            bool disableGradient = !(Car.Train.LeadLocomotive is MSTSLocomotive) && Car.Train.TrainType != Orts.Simulation.Physics.Train.TRAINTYPE.STATIC;
            // Legacy cars and static cars use a simpler check for emergency applications to ensure emergency applications occur despite simplified physics
            bool emergencyTripped = (Car.Train.TrainType == Orts.Simulation.Physics.Train.TRAINTYPE.STATIC || LegacyEmergencyValve) ?
                BrakeLine1PressurePSI <= 0.75f * EmergResPressurePSI * AuxCylVolumeRatio / (AuxCylVolumeRatio + 1) : Math.Max(-SmoothedBrakePipeChangePSIpS.SmoothedValue, 0) > EmergencyValveActuationRatePSIpS;

            if (valveType == BrakeValveType.Distributor)
            {
                float applicationPSI = ControlResPressurePSI - BrakeLine1PressurePSI;
                float targetPressurePSI = applicationPSI * AuxCylVolumeRatio;
                if (!disableGradient && EmergencyValveActuationRatePSIpS > 0 && emergencyTripped)
                {
                    if (prevState == ValveState.Release) // If valve transitions from release to emergency, quick service activates
                    {
                        QuickServiceActive = QuickServiceMode.PrelimQuickService;
                        UniformChargingActive = false;
                        UniformReleaseActive = false;
                        EmergResQuickReleaseActive = false;
                    }
                    TripleValveState = ValveState.Emergency;
                }
                else if (TripleValveState != ValveState.Emergency && targetPressurePSI > AutoCylPressurePSI + (TripleValveState == ValveState.Apply ? 0.0f : TripleValveSensitivityPSI * AuxCylVolumeRatio))
                {
                    if (prevState == ValveState.Release)
                    {
                        if (applicationPSI > InitialApplicationThresholdPSI) // If valve transitions from release to apply, quick service activates
                        {
                            QuickServiceActive = QuickServiceMode.PrelimQuickService;
                            UniformChargingActive = false;
                            UniformReleaseActive = false;
                            EmergResQuickReleaseActive = false;

                            TripleValveState = ValveState.Apply;
                        }
                    }
                    else
                    {
                        TripleValveState = ValveState.Apply;
                    }
                }
                else if (targetPressurePSI < AutoCylPressurePSI - (TripleValveState == ValveState.Release ? 0.0f : TripleValveSensitivityPSI * AuxCylVolumeRatio) || applicationPSI < InitialApplicationThresholdPSI)
                {
                    if (prevState != ValveState.Release) // If valve transitions to release, quick release activates, quick service deactivates
                    {
                        EmergResQuickReleaseActive = true;
                        QuickServiceActive = QuickServiceMode.Release;
                    }
                    TripleValveState = ValveState.Release;
                }
                else if (TripleValveState != ValveState.Emergency || (TripleValveState == ValveState.Emergency && BrakeLine1PressurePSI > AuxResPressurePSI))
                {
                    TripleValveState = ValveState.Lap;
                }    
            }
            else if (valveType == BrakeValveType.TripleValve || valveType == BrakeValveType.DistributingValve)
            {
                if (!disableGradient && EmergencyValveActuationRatePSIpS > 0 && emergencyTripped)
                {
                    if (prevState == ValveState.Release) // If valve transitions from release to emergency, quick service activates
                    {
                        QuickServiceActive = QuickServiceMode.PrelimQuickService;
                        UniformChargingActive = false;
                        UniformReleaseActive = false;
                        EmergResQuickReleaseActive = false;
                    }
                    TripleValveState = ValveState.Emergency;
                }
                else if (TripleValveState != ValveState.Emergency && BrakeLine1PressurePSI < AuxResPressurePSI - (TripleValveState == ValveState.Apply ? 0.0f : TripleValveSensitivityPSI))
                {
                    if (prevState == ValveState.Release)
                    {
                        if (BrakeLine1PressurePSI < AuxResPressurePSI - InitialApplicationThresholdPSI) // If valve transitions from release to apply, quick service activates
                        {
                            QuickServiceActive = QuickServiceMode.PrelimQuickService;
                            UniformChargingActive = false;
                            UniformReleaseActive = false;
                            EmergResQuickReleaseActive = false;

                            TripleValveState = ValveState.Apply;
                        }
                    }
                    else
                    {
                        TripleValveState = ValveState.Apply;
                    }
                }
                else if (BrakeLine1PressurePSI > AuxResPressurePSI + (TripleValveState == ValveState.Release ? 0.0f : TripleValveSensitivityPSI * 1.5f))
                {
                    if (prevState != ValveState.Release) // If valve transitions to release, quick release activates, quick service deactivates
                    {
                        EmergResQuickReleaseActive = true;
                        QuickServiceActive = QuickServiceMode.Release;
                    }
                    TripleValveState = ValveState.Release;
                }
                else if (TripleValveState == ValveState.Apply)
                {
                    TripleValveState = ValveState.Lap;
                }
            }
            else
            {
                TripleValveState = ValveState.Release;
            }
            if (TripleValveState == ValveState.Emergency)
            {
                if (prevState != ValveState.Emergency)
                {
                    EmergencyDumpStartTime = (float)Car.Simulator.GameTime;
                    Car.SignalEvent(Event.EmergencyVentValveOn);
                }
            }
            else EmergencyDumpStartTime = null;
        }

        public void UpdateAngleCockState(bool AngleCockOpen, ref float AngleCockOpenAmount, ref float? AngleCockOpenTime)
        {
            float currentTime = (float)this.Car.Simulator.GameTime;

            if (AngleCockOpen && AngleCockOpenAmount < 1.0f)
            {
                if (AngleCockOpenTime == null)
                {
                    AngleCockOpenTime = currentTime;
                }
                else if (currentTime - AngleCockOpenTime > AngleCockOpeningTime)
                {
                    // Finish opening anglecock at a faster rate once time has elapsed
                    AngleCockOpenAmount = MathHelper.Lerp(0.3f, 1.0f, (currentTime - ((float)AngleCockOpenTime + AngleCockOpeningTime)) / 5);

                    if (AngleCockOpenAmount >= 1.0f)
                    {
                        AngleCockOpenAmount = 1.0f;
                        AngleCockOpenTime = null;
                    }
                }
                else
                {
                    // Gradually open anglecock toward 30% over 30 seconds
                    AngleCockOpenAmount = MathHelper.Lerp(0.0f, 0.3f, (currentTime - (float)AngleCockOpenTime) / AngleCockOpeningTime);
                }
            }
            else if (!AngleCockOpen && AngleCockOpenAmount > 0.0f)
            {
                AngleCockOpenAmount = 0.0f;
                AngleCockOpenTime = null;
            }
        }

        public virtual void UpdateTractionCutoff()
        {
            var locomotive = Car as MSTSLocomotive;
            if (!locomotive.DoesBrakeCutPower) return;
            if (locomotive.BrakeCutsPowerAtBrakePipePressurePSI > 0)
            {
                if (BrakeLine1PressurePSI < locomotive.BrakeCutsPowerAtBrakePipePressurePSI) locomotive.TrainControlSystem.BrakeSystemTractionAuthorization = false;
                else if (BrakeLine1PressurePSI >= locomotive.BrakeRestoresPowerAtBrakePipePressurePSI) locomotive.TrainControlSystem.BrakeSystemTractionAuthorization = true;
            }
            else locomotive.TrainControlSystem.BrakeSystemTractionAuthorization = CylPressurePSI <= locomotive.BrakeCutsPowerAtBrakeCylinderPressurePSI;
        }

        public override void Update(float elapsedClockSeconds)
        {
            var valveType = BrakeValve;

            // Two stage braking: higher brake force is allowed at higher speeds
            if (TripleValveState == ValveState.Emergency || (TwoStageLowSpeedActive && Math.Abs(Car.SpeedMpS) > TwoStageSpeedUpMpS))
                TwoStageLowSpeedActive = false;
            else if (!TwoStageLowSpeedActive && Math.Abs(Car.SpeedMpS) < TwoStageSpeedDownMpS)
                TwoStageLowSpeedActive = true;

            // Determine target brake cylinder feed pressure
            float threshold = 0; 
            if (TripleValveState == ValveState.Emergency)
            {
                threshold = MaxTripleValveCylPressurePSI; // Set pressure to max in emergency
            }
            else
            {
                if (valveType == BrakeValveType.Distributor)
                {
                    threshold = Math.Max((ControlResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio, 0);

                    if (threshold < InitialApplicationThresholdPSI * AuxCylVolumeRatio) // Prevent brakes getting stuck with a small amount of air on distributor systems
                        threshold = 0;
                    // Prevent air from being perpetually vented by the HSRV in graduated release systems
                    if (HighSpeedReducingPressurePSI > 0 && threshold > HighSpeedReducingPressurePSI
                        && ((MRPAuxResCharging && !SupplyReservoirPresent) || BrakeLine1PressurePSI > AuxResPressurePSI))
                        threshold = HighSpeedReducingPressurePSI;
                }
                else
                {
                    if (TripleValveState == ValveState.Release)
                        threshold = 0;
                    else
                        threshold = MaxTripleValveCylPressurePSI; // Set pressure limit to max for plain triple valves
                }
                if (TwoStageLowSpeedActive && threshold > TwoStageLowPressurePSI)
                    threshold = TwoStageLowPressurePSI;
                else if (threshold > ServiceMaxCylPressurePSI)
                    threshold = ServiceMaxCylPressurePSI;
                else if (threshold > MaxTripleValveCylPressurePSI)
                    threshold = MaxTripleValveCylPressurePSI;

                // Account for retainers
                threshold = Math.Max(threshold, RetainerPressureThresholdPSI);
            }

            BrakePipeChangePSIpS = (BrakeLine1PressurePSI - PrevBrakePipePressurePSI) / Math.Max(elapsedClockSeconds, 0.0001f);
            SmoothedBrakePipeChangePSIpS.Update(Math.Max(elapsedClockSeconds, 0.0001f), BrakePipeChangePSIpS);

            // Update anglecock opening. Anglecocks set to gradually open over 30 seconds, but close instantly.
            // Gradual opening prevents undesired emergency applications
            UpdateAngleCockState(AngleCockAOpen, ref AngleCockAOpenAmount, ref AngleCockAOpenTime);
            UpdateAngleCockState(AngleCockBOpen, ref AngleCockBOpenAmount, ref AngleCockBOpenTime);

            if (BleedOffValveOpen)
            {
                if (valveType == BrakeValveType.Distributor)
                {
                    ControlResPressurePSI = 0;
                    BleedOffValveOpen = false;
                }
                else
                {
                    if (AuxResPressurePSI < 0.01f && AutoCylPressurePSI < 0.01f && BrakeLine1PressurePSI < 0.01f && (EmergResPressurePSI < 0.01f || !EmergencyReservoirPresent))
                    {
                        BleedOffValveOpen = false;
                    }
                    else
                    {
                        AuxResPressurePSI -= elapsedClockSeconds * MaxApplicationRatePSIpS;
                        if (AuxResPressurePSI < 0)
                            AuxResPressurePSI = 0;
                        if (CylSource == CylinderSource.AuxRes)
                            AutoCylPressurePSI = CalculateBrakeCylinderPressure(ref AutoCylAirPSIM3, -elapsedClockSeconds * MaxReleaseRatePSIpS, 0);
                        else
                            AutoCylPressurePSI -= elapsedClockSeconds * MaxReleaseRatePSIpS;
                        if (AutoCylPressurePSI < 0)
                        {
                            AutoCylPressurePSI = 0;
                            AutoCylAirPSIM3 = 0;
                        }
                        if (EmergencyReservoirPresent)
                        {
                            EmergResPressurePSI -= elapsedClockSeconds * EmergResChargingRatePSIpS;
                            if (EmergResPressurePSI < 0)
                                EmergResPressurePSI = 0;
                        }
                        TripleValveState = ValveState.Release;
                    }
                }
            }
            else
                UpdateTripleValveState(elapsedClockSeconds);

            // triple valve is set to charge the brake cylinder
            if ((TripleValveState == ValveState.Apply || TripleValveState == ValveState.Emergency) && !Car.WheelBrakeSlideProtectionActive && IsolationValve == ValveState.Release)
            {
                float dp;
                float dpPipe = 0;
                // Preliminary quick service: No brake cylinder pressure yet
                if (QuickServiceActive == QuickServiceMode.PrelimQuickService && QuickServiceVentRatePSIpS > 0.0f) 
                {
                    dp = 0;
                    // Transition to second phase of quick service after brake pipe pressure has dropped further
                    if (AuxResPressurePSI - BrakeLine1PressurePSI > InitialApplicationThresholdPSI + 2.0f)
                        QuickServiceActive = QuickServiceMode.Service;
                }
                else
                {
                    // Accelerated application: Air is vented from the brake pipe to speed up service applications
                    // Amount of air vented is proportional to pressure reduction from external sources
                    if (AcceleratedApplicationFactor > 0)
                        dpPipe = MathHelper.Clamp(-SmoothedBrakePipeChangePSIpS.SmoothedValue * AcceleratedApplicationFactor, 0, AcceleratedApplicationLimitPSIpS) * elapsedClockSeconds;
                    if (BrakeLine1PressurePSI - dpPipe < 0)
                        dpPipe = BrakeLine1PressurePSI;

                    if (AutoCylPressurePSI < QuickServiceLimitPSI)
                        dp = elapsedClockSeconds * Math.Max(QuickServiceApplicationRatePSIpS, MaxApplicationRatePSIpS);
                    else if (TripleValveState == ValveState.Emergency && (!QuickActionFitted || BrakeLine1PressurePSI < AutoCylPressurePSI))
                        dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                    else
                        dp = elapsedClockSeconds * ServiceApplicationRatePSIpS;
                }

                if (AuxResPressurePSI - AutoCylPressurePSI < 20.0f)
                    dp = Math.Min(dp, Math.Max(MaxApplicationRatePSIpS * (AuxResPressurePSI - AutoCylPressurePSI) / 20.0f, 0)); // Reduce application rate as pressure difference diminishes
                if (TripleValveState != ValveState.Emergency && BrakeLine1PressurePSI > AuxResPressurePSI - 1)
                    dp *= MathHelper.Lerp(0.1f, 1.0f, AuxResPressurePSI - BrakeLine1PressurePSI); // Reduce application rate if nearing equalization to prevent rapid toggling between apply and lap
                else if ((valveType == BrakeValveType.Distributor) && AutoCylPressurePSI > threshold - 1)
                    dp *= MathHelper.Lerp(0.1f, 1.0f, threshold - AutoCylPressurePSI); // Reduce application rate if nearing target pressure
                if (AuxResPressurePSI - dp / AuxCylVolumeRatio < AutoCylPressurePSI + dp)
                    dp = (AuxResPressurePSI - AutoCylPressurePSI) * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
                if (AutoCylPressurePSI + dp > threshold)
                    dp = threshold - AutoCylPressurePSI;
                if (BrakeLine1PressurePSI > AuxResPressurePSI - dp / AuxCylVolumeRatio && !BleedOffValveOpen)
                    dp = (AuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                if (dp < 0)
                    dp = 0;

                if (CylSource == CylinderSource.AuxRes) // Aux res is directly connected to brake cylinder, no relay valve
                {
                    float prevAutoCylAirPSIM3 = AutoCylAirPSIM3;
                    AutoCylPressurePSI = CalculateBrakeCylinderPressure(ref AutoCylAirPSIM3, dp, threshold);
                    AuxResPressurePSI -= (AutoCylAirPSIM3 - prevAutoCylAirPSIM3) / AuxResVolumeM3; // Improve accuracy of aux res exhausting
                }
                else
                {
                    AutoCylPressurePSI += dp;
                    AuxResPressurePSI -= dp / AuxCylVolumeRatio;
                }
                BrakeLine1PressurePSI -= dpPipe;

                // Disable preliminary quick service in some situations to prevent runaway condition
                if (QuickServiceActive == QuickServiceMode.PrelimQuickService
                    && (AutoCylPressurePSI > QuickServiceLimitPSI || AutoCylPressurePSI >= AuxResPressurePSI)
                    || QuickServiceVentRatePSIpS <= 0.0f) 
                    QuickServiceActive = QuickServiceMode.Service;

                if (TripleValveState == ValveState.Emergency)
                {
                    if (EmergencyReservoirPresent)
                    {
                        if (EmergencyDumpValveTimerS != 0 && EmergencyDumpStartTime == null && BrakeLine1PressurePSI > AcceleratedEmergencyReleaseThresholdPSI)
                        {
                            // Accelerated emergency release: Aux res and BC air are routed into the brake pipe once the emergency application is complete, speeds up emergency release
                            // Triggers at 20 psi brake pipe pressure

                            dp = elapsedClockSeconds * MaxReleaseRatePSIpS;
                            if (AutoCylPressurePSI - dp < AuxResPressurePSI + dp / AuxCylVolumeRatio)
                                dp = Math.Max((AutoCylPressurePSI - AuxResPressurePSI) * (AuxCylVolumeRatio / (1 + AuxCylVolumeRatio)), 0);
                            if (CylSource == CylinderSource.AuxRes)
                                AutoCylPressurePSI = CalculateBrakeCylinderPressure(ref AutoCylAirPSIM3, -dp, AuxResPressurePSI);
                            else
                                AutoCylPressurePSI -= dp;
                            AuxResPressurePSI += dp / AuxCylVolumeRatio;

                            dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
                            if (AuxResPressurePSI - dp < BrakeLine1PressurePSI + dp * AuxBrakeLineVolumeRatio)
                                dp = Math.Max((AuxResPressurePSI - BrakeLine1PressurePSI) / (1 + AuxBrakeLineVolumeRatio), 0);
                            AuxResPressurePSI -= dp;
                            BrakeLine1PressurePSI += dp * AuxBrakeLineVolumeRatio;
                        }
                        else if (EmergResQuickRelease != QuickReleaseType.AcceleratedReleaseRes)
                        {
                            dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                            if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                                dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                            EmergResPressurePSI -= dp;
                            AuxResPressurePSI += dp * EmergAuxVolumeRatio;
                        }
                    }
                    else if (SupplyReservoirPresent && (MaxTripleValveCylPressurePSI > ServiceMaxCylPressurePSI) && BrakeLine1PressurePSI < 15.0f)
                    {
                        // Supply res air directed to BC feed line to ensure full emergency force on cars with no emergency res
                        // Only activated with brake pipe pressure lower than 10-18 psi (assuming 15 is the design point)

                        // Ratio of displacement (dummy brake cylinder) volume to supply res volume
                        // We are supplying pressure to the brake cylinder line, not the brake cylinder itself
                        float displacementSupplyVolumeRatio = AuxResVolumeM3 / AuxCylVolumeRatio / SupplyResVolumeM3;

                        dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                        if (AutoCylPressurePSI + dp > SupplyResPressurePSI - (dp * displacementSupplyVolumeRatio))
                            dp = (SupplyResPressurePSI - AutoCylPressurePSI) / (1 + displacementSupplyVolumeRatio);
                        if (AutoCylPressurePSI + dp > threshold)
                            dp = threshold - AutoCylPressurePSI;
                        if (dp < 0)
                            dp = 0;

                        SupplyResPressurePSI -= dp * displacementSupplyVolumeRatio;
                        AutoCylPressurePSI += dp;
                    }

                    if (EmergencyDumpValveTimerS == 0 && EmergencyDumpStartTime != null)
                    {
                        if (BrakeLine1PressurePSI < 1) EmergencyDumpStartTime = null;
                    }
                    else if (Car.Simulator.GameTime - EmergencyDumpStartTime > EmergencyDumpValveTimerS)
                    {
                        EmergencyDumpStartTime = null;
                    }
                    if (EmergencyDumpValveRatePSIpS > 0 && EmergencyDumpStartTime != null)
                    {
                        BrakeLine1PressurePSI -= elapsedClockSeconds * EmergencyDumpValveRatePSIpS;
                        if (BrakeLine1PressurePSI < 0)
                            BrakeLine1PressurePSI = 0;
                    }
                    if (QuickActionFitted && BrakeLine1PressurePSI > AutoCylPressurePSI) // Quick action: Uses air from the brake pipe to fill brake cylinder during emergency, usually used without an emrg. res
                    {
                        dp = elapsedClockSeconds * MaxApplicationRatePSIpS;

                        if (AutoCylPressurePSI + dp > MaxCylPressurePSI)
                            dp = MaxCylPressurePSI - AutoCylPressurePSI;
                        if (BrakeLine1PressurePSI - (dp * CylBrakeLineVolumeRatio) < AutoCylPressurePSI + dp)
                            dp = (BrakeLine1PressurePSI - AutoCylPressurePSI) / (1 + CylBrakeLineVolumeRatio);
                        if (dp < 0)
                            dp = 0;
                        if (CylSource == CylinderSource.AuxRes)
                            AutoCylPressurePSI = CalculateBrakeCylinderPressure(ref AutoCylAirPSIM3, dp, BrakeLine1PressurePSI);
                        else
                            AutoCylPressurePSI += dp;
                        BrakeLine1PressurePSI -= dp * CylBrakeLineVolumeRatio;
                    }
                }
            }

            // triple valve set to release pressure in brake cylinder and EP valve set
            if (TripleValveState == ValveState.Release && valveType != BrakeValveType.None)
            {
                if (valveType == BrakeValveType.Distributor)
                {
                    if (ControlResPressurePSI < BrakeLine1PressurePSI)
                    {
                        ControlResPressurePSI = BrakeLine1PressurePSI;
                    }
                    else if (BrakeInsensitivityPSIpS > 0 && ControlResPressurePSI > BrakeLine1PressurePSI && ControlResPressurePSI < BrakeLine1PressurePSI + 1) // Overcharge elimination
                    {
                        float dp = elapsedClockSeconds * BrakeInsensitivityPSIpS;
                        ControlResPressurePSI = Math.Max(ControlResPressurePSI - dp, BrakeLine1PressurePSI);
                    }
                }
                if (BrakeInsensitivityPSIpS > 0 && AuxResPressurePSI > BrakeLine1PressurePSI) // Allow small flow from auxiliary reservoir to brake pipe so the triple valve is not sensible to small pressure variations when in release position
                {
                    float dp = elapsedClockSeconds * BrakeInsensitivityPSIpS;
                    if (AuxResPressurePSI - dp < BrakeLine1PressurePSI + dp * AuxBrakeLineVolumeRatio)
                        dp = (AuxResPressurePSI - BrakeLine1PressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                    AuxResPressurePSI -= dp;
                    BrakeLine1PressurePSI += dp * AuxBrakeLineVolumeRatio;
                }
            }

            // Handle brake release: reduce cylinder pressure if all triple valve, EP holding valve and retainers allow so
            if (TripleValveState == ValveState.Release && HoldingValve == ValveState.Release && IsolationValve == ValveState.Release && AutoCylPressurePSI > threshold)
            {
                float dp = elapsedClockSeconds * ReleaseRatePSIpS;
                // Advanced brake cylinder simulation: rate of release is nonlinear, increasing as pressure increases
                if (CylDiameterM > 0)
                    dp *= (AutoCylPressurePSI / ReferencePressurePSI) * 2.5f; // Multiply by 2.5 (91.8% time constant) so average release rate matches MaxReleaseRate better
                if (UniformReleaseRatio > 0) // Uniform release: Brake release is slowed down when the brake pipe is substantially higher than the aux res
                {
                    if (!UniformReleaseActive && AuxResPressurePSI < BrakeLine1PressurePSI - UniformReleaseThresholdPSI)
                        UniformReleaseActive = true;
                    else if (UniformReleaseActive && AuxResPressurePSI > BrakeLine1PressurePSI - (UniformReleaseThresholdPSI / 2))
                        UniformReleaseActive = false;
                    if (UniformReleaseActive)
                        dp /= UniformReleaseRatio;
                }
                if (threshold > 0 && AutoCylPressurePSI < threshold + 1)
                    dp *= MathHelper.Lerp(0.1f, 1.0f, AutoCylPressurePSI - threshold); // Reduce release rate if nearing target pressure to prevent toggling between release and lap
                if (AutoCylPressurePSI - dp < threshold)
                    dp = AutoCylPressurePSI - threshold;
                if (dp < 0)
                    dp = 0;

                if (CylSource == CylinderSource.AuxRes) // Aux res is directly connected to brake cylinder, no relay valve
                    AutoCylPressurePSI = CalculateBrakeCylinderPressure(ref AutoCylAirPSIM3, -dp, threshold);
                else
                    AutoCylPressurePSI -= dp;
            }
            // Special cases for equipment which bypasses triple valve
            else if (TwoStageSpeedDownMpS > 0 && (TwoStageLowSpeedActive && AutoCylPressurePSI > TwoStageLowPressurePSI) ||
                (TripleValveState != ValveState.Emergency && !TwoStageLowSpeedActive && AutoCylPressurePSI > ServiceMaxCylPressurePSI)) // Two stage braking
            {
                float target = TwoStageLowSpeedActive ? TwoStageLowPressurePSI : ServiceMaxCylPressurePSI;
                float dp = elapsedClockSeconds * ReleaseRatePSIpS;
                if (AutoCylPressurePSI - dp < target)
                    dp = AutoCylPressurePSI - target;
                if (CylSource == CylinderSource.AuxRes)
                    AutoCylPressurePSI = CalculateBrakeCylinderPressure(ref AutoCylAirPSIM3, -dp, target);
                else
                    AutoCylPressurePSI -= dp;
            }
            if (HighSpeedReducingPressurePSI > 0 && AutoCylPressurePSI > HighSpeedReducingPressurePSI) // High speed reducing valve
            {
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS / 2.0f; // This release rate should allow only emergency applications to overcome HSRV
                dp *= MathHelper.Clamp(MathHelper.Lerp(1.0f, 0.1f, (AutoCylPressurePSI - HighSpeedReducingPressurePSI) / 5.0f), 0.1f, 1.0f); // Rate of release reduces as pressure difference increases
                if (AutoCylPressurePSI - dp < HighSpeedReducingPressurePSI)
                    dp = AutoCylPressurePSI - HighSpeedReducingPressurePSI;
                if (CylSource == CylinderSource.AuxRes)
                    AutoCylPressurePSI = CalculateBrakeCylinderPressure(ref AutoCylAirPSIM3, -dp, HighSpeedReducingPressurePSI);
                else
                    AutoCylPressurePSI -= dp;
            }

            // Manage quick service bulb charging
            if (QuickServiceBulbVolumeM3 > 0.0f)
            {
                // Quick service bulb rapidly equalizes with brake pipe during normal operation
                if (threshold > 0.0f)
                {
                    float dpPipe = elapsedClockSeconds * QuickServiceVentRatePSIpS * 2.0f;

                    if (QuickServiceBulbPressurePSI + dpPipe / BulbBrakeLineVolumeRatio > BrakeLine1PressurePSI - dpPipe)
                        dpPipe = (BrakeLine1PressurePSI - QuickServiceBulbPressurePSI) * BulbBrakeLineVolumeRatio / (1 + BulbBrakeLineVolumeRatio);

                    QuickServiceBulbPressurePSI += dpPipe / BulbBrakeLineVolumeRatio;
                    BrakeLine1PressurePSI -= dpPipe;
                }
                // Quick service bulb is vented to atmosphere during preliminary quick service and release
                if (QuickServiceActive == QuickServiceMode.PrelimQuickService || threshold <= 0.0f || AutoCylPressurePSI <= 0.0f)
                {
                    float dp = elapsedClockSeconds * QuickServiceVentRatePSIpS / BulbBrakeLineVolumeRatio;
                    if (QuickServiceBulbPressurePSI - dp < 0.0f)
                        dp = QuickServiceBulbPressurePSI;
                    QuickServiceBulbPressurePSI -= dp;

                    // Failsafe to prevent runaway quick service behavior
                    if (BrakeLine1PressurePSI < 20.0f)
                        QuickServiceActive = QuickServiceMode.Release;
                }
                // Quick service bulb air will also maintain brake cylinder pressure at the quick service limit, except during preliminary quick service
                else if (QuickServiceActive != QuickServiceMode.PrelimQuickService && threshold > QuickServiceLimitPSI && AutoCylPressurePSI < QuickServiceLimitPSI)
                {
                    // Basic cylinder leak prevention, let air enter cylinder from brake pipe if pressure drops below the quick service limiting valve
                    float dp = elapsedClockSeconds * ServiceApplicationRatePSIpS / 4.0f;
                    float volumeRatio = AuxResVolumeM3 / AuxCylVolumeRatio / QuickServiceBulbVolumeM3;

                    if (AutoCylPressurePSI + dp > QuickServiceLimitPSI)
                        dp = QuickServiceLimitPSI - AutoCylPressurePSI;
                    if (QuickServiceBulbPressurePSI - (dp * volumeRatio) < AutoCylPressurePSI + dp)
                        dp = (QuickServiceBulbPressurePSI - AutoCylPressurePSI) / (1 + volumeRatio);
                    if (dp < 0)
                        dp = 0;
                    if (CylSource == CylinderSource.AuxRes)
                        AutoCylPressurePSI = CalculateBrakeCylinderPressure(ref AutoCylAirPSIM3, dp, QuickServiceLimitPSI);
                    else
                        AutoCylPressurePSI += dp;
                    QuickServiceBulbPressurePSI -= dp * volumeRatio;
                }
            }

            // Manage emergency res charging
            if (EmergencyReservoirPresent)
            {
                if (TripleValveState == ValveState.Release && EmergResPressurePSI > BrakeLine1PressurePSI)
                {
                    // Quick release is disabled when aux res pressure is close to brake pipe pressure
                    // This also reduces unintended brake releases
                    if (AuxResPressurePSI >= BrakeLine1PressurePSI - 1.0f)
                        EmergResQuickReleaseActive = false;
                    // Quick release: Emergency res charges brake pipe during release
                    if (EmergResQuickRelease != QuickReleaseType.None && EmergResQuickReleaseActive) 
                    {
                        float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
                        if (EmergResPressurePSI - dp < BrakeLine1PressurePSI + dp * EmergBrakeLineVolumeRatio)
                            dp = (EmergResPressurePSI - BrakeLine1PressurePSI) / (1 + EmergBrakeLineVolumeRatio);
                        EmergResPressurePSI -= dp;
                        BrakeLine1PressurePSI += dp * EmergBrakeLineVolumeRatio;
                    }
                    else if (EmergResQuickRelease == QuickReleaseType.None) // Quick recharge: Emergency res air used to recharge aux res on older control valves
                    {
                        float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
                        if (AuxResPressurePSI + dp > EmergResPressurePSI - dp / EmergAuxVolumeRatio)
                            dp = (EmergResPressurePSI - AuxResPressurePSI) * EmergAuxVolumeRatio / (1 + EmergAuxVolumeRatio);
                        if (BrakeLine1PressurePSI < AuxResPressurePSI + dp)
                            dp = (BrakeLine1PressurePSI - AuxResPressurePSI);
                        if (dp < 0)
                            dp = 0;
                        AuxResPressurePSI += dp;
                        EmergResPressurePSI -= dp / EmergAuxVolumeRatio;
                    }
                }
                if (AuxResPressurePSI > EmergResPressurePSI && (valveType == BrakeValveType.Distributor || TripleValveState == ValveState.Release))
                {
                    float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
                    if (EmergResPressurePSI + dp > AuxResPressurePSI - dp * EmergAuxVolumeRatio)
                        dp = (AuxResPressurePSI - EmergResPressurePSI) / (1 + EmergAuxVolumeRatio);
                    EmergResPressurePSI += dp;
                    AuxResPressurePSI -= dp * EmergAuxVolumeRatio;
                }
            }

            // Manage aux res charging
            float dpAux = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;

            if (TwoPipes && MRPAuxResCharging && !SupplyReservoirPresent &&
                valveType == BrakeValveType.Distributor && BrakeLine2PressurePSI > BrakeLine1PressurePSI) // Charge from main res pipe
            {
                if (AuxResPressurePSI < BrakeLine2PressurePSI && AuxResPressurePSI < ControlResPressurePSI && !BleedOffValveOpen)
                {
                    if (AuxResPressurePSI + dpAux > BrakeLine2PressurePSI - dpAux * AuxBrakeLineVolumeRatio)
                        dpAux = (BrakeLine2PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                    AuxResPressurePSI += dpAux;
                    BrakeLine2PressurePSI -= dpAux * AuxBrakeLineVolumeRatio;
                }
                
            }
            else // Charge from brake pipe
            {
                if (AuxResPressurePSI < BrakeLine1PressurePSI && (valveType == BrakeValveType.Distributor || TripleValveState == ValveState.Release) && !BleedOffValveOpen)
                {
                    if (AuxResPressurePSI > BrakeLine1PressurePSI - 1)
                        dpAux *= MathHelper.Lerp(0.1f, 1.0f, BrakeLine1PressurePSI - AuxResPressurePSI); // Reduce recharge rate if nearing target pressure to smooth out changes in brake pipe
                    if (UniformChargingRatio > 0) // Uniform charging: Aux res charging is slowed down when the brake pipe is substantially higher than the aux res
                    {
                        if (!UniformChargingActive && AuxResPressurePSI < BrakeLine1PressurePSI - UniformChargingThresholdPSI)
                            UniformChargingActive = true;
                        else if (UniformChargingActive && AuxResPressurePSI > BrakeLine1PressurePSI - UniformChargingThresholdPSI / 2)
                            UniformChargingActive = false;
                        if (UniformChargingActive)
                            dpAux /= UniformChargingRatio;
                    }
                    if (AuxResPressurePSI + dpAux > BrakeLine1PressurePSI - dpAux * AuxBrakeLineVolumeRatio)
                        dpAux = (BrakeLine1PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                    AuxResPressurePSI += dpAux;
                    BrakeLine1PressurePSI -= dpAux * AuxBrakeLineVolumeRatio;
                }
                // Charge via weeping port; incredibly slow pressure transfer to improve stability during applications
                else if (BrakeInsensitivityPSIpS > 0 && valveType == BrakeValveType.TripleValve && !BleedOffValveOpen)
                {
                    // Estimate for weeping rate, based on data suggesting a weeping rate of ~0.5 psi/min on brake equipment with an insensitivity of 3 psi/min
                    dpAux = elapsedClockSeconds * (BrakeInsensitivityPSIpS / 6.0f) * Math.Sign(BrakeLine1PressurePSI - AuxResPressurePSI);

                    if (Math.Abs(AuxResPressurePSI - BrakeLine1PressurePSI) < 0.1f)
                        dpAux *= Math.Abs(AuxResPressurePSI - BrakeLine1PressurePSI) / 0.1f;
                    AuxResPressurePSI += dpAux;
                    BrakeLine1PressurePSI -= dpAux * AuxBrakeLineVolumeRatio;
                }
            }

            // Manage supply res charging
            if (SupplyReservoirPresent)
            {
                float dp = elapsedClockSeconds * SupplyResChargingRatePSIpS;

                if (Car is MSTSLocomotive locomotive && SupplyResPressurePSI < locomotive.MainResPressurePSI) // Charge from MR if this is a locomotive
                {
                    float supplyMRVolumeRatio = (SupplyResVolumeM3 / locomotive.MainResVolumeM3);

                    if (SupplyResPressurePSI > locomotive.MainResPressurePSI - 1)
                        dp *= MathHelper.Lerp(0.1f, 1.0f, locomotive.MainResPressurePSI - SupplyResPressurePSI);
                    if (SupplyResPressurePSI + dp > locomotive.MainResPressurePSI - (dp * supplyMRVolumeRatio))
                        dp = (locomotive.MainResPressurePSI - SupplyResPressurePSI) / (1 + supplyMRVolumeRatio);

                    locomotive.MainResPressurePSI -= dp * supplyMRVolumeRatio;
                    SupplyResPressurePSI += dp;
                }
                else if (TwoPipes && MRPAuxResCharging && BrakeLine2PressurePSI > BrakeLine1PressurePSI && SupplyResPressurePSI < BrakeLine2PressurePSI) // Charge from MR pipe if possible
                {
                    if (SupplyResPressurePSI > BrakeLine2PressurePSI - 1)
                        dp *= MathHelper.Lerp(0.1f, 1.0f, BrakeLine2PressurePSI - SupplyResPressurePSI); // Reduce recharge rate if nearing target pressure to smooth out changes in brake pipe
                    if (SupplyResPressurePSI + dp > BrakeLine2PressurePSI - (dp * SupplyBrakeLineVolumeRatio))
                        dp = (BrakeLine2PressurePSI - SupplyResPressurePSI) / (1 + SupplyBrakeLineVolumeRatio);

                    BrakeLine2PressurePSI -= dp * SupplyBrakeLineVolumeRatio;
                    SupplyResPressurePSI += dp;
                }
                else if (SupplyResPressurePSI < BrakeLine1PressurePSI) // Otherwise charge from BP
                {
                    if (SupplyResPressurePSI > BrakeLine1PressurePSI - 1)
                        dp *= MathHelper.Lerp(0.1f, 1.0f, BrakeLine1PressurePSI - SupplyResPressurePSI); // Reduce recharge rate if nearing target pressure to smooth out changes in brake pipe
                    if (SupplyResPressurePSI + dp > BrakeLine1PressurePSI - (dp * SupplyBrakeLineVolumeRatio))
                        dp = (BrakeLine1PressurePSI - SupplyResPressurePSI) / (1 + SupplyBrakeLineVolumeRatio);

                    BrakeLine1PressurePSI -= dp * SupplyBrakeLineVolumeRatio;
                    SupplyResPressurePSI += dp;
                }
                
            }

            if (AutoCylPressurePSI < 0)
            {
                AutoCylPressurePSI = 0;
                AutoCylAirPSIM3 = 0;
            }
            
            float demandedPressurePSI = 0;
            var loco = Car as MSTSLocomotive;
            if (loco != null && BrakeValve == BrakeValveType.DistributingValve)
            {
                // For distributing valves, we use AutoCylPressurePSI as "Application Chamber/Pipe" pressure
                // CylPressurePSI is the actual pressure applied to cylinders
                var engineBrakeStatus = loco.EngineBrakeController?.TrainBrakeControllerState ?? ControllerState.Release;
                var trainBrakeStatus = loco.TrainBrakeController.TrainBrakeControllerState;
                 // BailOff
                if (engineBrakeStatus == ControllerState.BailOff)
                {
                    float dp = Math.Max(MaxReleaseRatePSIpS, loco.EngineBrakeReleaseRatePSIpS) * elapsedClockSeconds;
                    AutoCylPressurePSI -= dp;
                    if (AutoCylPressurePSI - dp < 0)
                        dp = AutoCylPressurePSI;

                    if (loco.AttachedTender?.BrakeSystem is AirSinglePipe tenderBrakes)
                    {
                        tenderBrakes.AutoCylPressurePSI -= dp;
                        if (tenderBrakes.AutoCylPressurePSI < 0)
                            tenderBrakes.AutoCylPressurePSI = 0;
                    }
                }
                // Emergency application
                if (trainBrakeStatus == ControllerState.Emergency)
                {
                    float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                    if (dp > MaxCylPressurePSI / RelayValveRatio - AutoCylPressurePSI)
                        dp = MaxCylPressurePSI / RelayValveRatio - AutoCylPressurePSI;
                    AutoCylPressurePSI += dp;
                }
                // Release pipe open
                HoldingValve = engineBrakeStatus == ControllerState.Release && trainBrakeStatus == ControllerState.Release ? ValveState.Release : ValveState.Lap;
                
                // Independent air brake equalization
                if (AutoCylPressurePSI < loco.Train.BrakeLine3PressurePSI)
                    AutoCylPressurePSI = loco.Train.BrakeLine3PressurePSI;
                else
                    loco.Train.BrakeLine3PressurePSI = AutoCylPressurePSI;

                // Equalization between application chamber and brake cylinders
                demandedPressurePSI = AutoCylPressurePSI;
            }
            else
            {
                demandedPressurePSI = AutoCylPressurePSI;
                if (loco != null && loco.EngineType != TrainCar.EngineTypes.Control)  // TODO - Control cars ned to be linked to power suppy requirements.
                {
                    if (loco.LocomotivePowerSupply.MainPowerSupplyOn)
                    {
                        if (loco.Train.LeadLocomotiveIndex >= 0)
                        { 
                            if (loco.Train.DetermineDPLeadLocomotive(loco) is MSTSLocomotive lead && (lead.BailOff || (lead.EngineBrakeController != null && lead.EngineBrakeController.TrainBrakeControllerState == ControllerState.BailOff)))
                            {
                                if (BrakeValve == BrakeValveType.Distributor)
                                    ControlResPressurePSI = 0;

                                float dp = Math.Max(MaxReleaseRatePSIpS, loco.EngineBrakeReleaseRatePSIpS) * elapsedClockSeconds;
                                AutoCylPressurePSI -= dp;
                                if (AutoCylPressurePSI < 0)
                                    AutoCylPressurePSI = 0;

                                if (loco.AttachedTender?.BrakeSystem is AirSinglePipe tenderBrakes)
                                {
                                    if (tenderBrakes.BrakeValve == BrakeValveType.Distributor)
                                        tenderBrakes.ControlResPressurePSI = 0;

                                    tenderBrakes.AutoCylPressurePSI -= dp;
                                    if (tenderBrakes.AutoCylPressurePSI < 0)
                                        tenderBrakes.AutoCylPressurePSI = 0;
                                }
                            }
                        }
                    }
                    {
                        if (loco.LocomotivePowerSupply.DynamicBrakeAvailable && loco.MaxDynamicBrakePercent > 0 && loco.DynamicBrakePercent > 0 && Car.FrictionBrakeBlendingMaxForceN > 0)
                        {
                            if (loco.DynamicBrakePartialBailOff)
                            {
                                var requiredBrakeForceN = (AutoCylPressurePSI * RelayValveRatio - loco.DynamicBrakeBlendingRetainedPressurePSI)
                                    / (ReferencePressurePSI - loco.DynamicBrakeBlendingRetainedPressurePSI) * Car.FrictionBrakeBlendingMaxForceN;
                                var localBrakeForceN = loco.DynamicBrakeForceN + (CylPressurePSI - loco.DynamicBrakeBlendingRetainedPressurePSI)
                                    / (ReferencePressurePSI - loco.DynamicBrakeBlendingRetainedPressurePSI) * Car.FrictionBrakeBlendingMaxForceN;
                                if (localBrakeForceN > requiredBrakeForceN - 0.15f * Car.FrictionBrakeBlendingMaxForceN)
                                {
                                    demandedPressurePSI = MathHelper.Clamp((requiredBrakeForceN - loco.DynamicBrakeForceN) / Car.FrictionBrakeBlendingMaxForceN *
                                        (ReferencePressurePSI - loco.DynamicBrakeBlendingRetainedPressurePSI) + loco.DynamicBrakeBlendingRetainedPressurePSI,
                                        loco.DynamicBrakeBlendingRetainedPressurePSI, MaxCylPressurePSI);
                                    if (demandedPressurePSI > CylPressurePSI && demandedPressurePSI < CylPressurePSI + 4) // Allow some margin for unnecessary air brake application
                                    {
                                        demandedPressurePSI = CylPressurePSI;
                                    }
                                    demandedPressurePSI /= RelayValveRatio;

                                    if (demandedPressurePSI > AutoCylPressurePSI)
                                        demandedPressurePSI = AutoCylPressurePSI;
                                }
                            }
                            else if (loco.DynamicBrakeAutoBailOff)
                            {
                                if (loco.DynamicBrakeForceCurves == null)
                                {
                                    demandedPressurePSI = 0;
                                }
                                else
                                {
                                    var dynforce = loco.GetAvailableDynamicBrakeForceN(1.0f);
                                    if ((loco.MaxDynamicBrakeForceN == 0 && dynforce > 0) || dynforce > loco.MaxDynamicBrakeForceN * 0.6)
                                    {
                                        demandedPressurePSI = 0;
                                    }
                                }
                            }
                        }
                        if (loco.DynamicBrakeEngineBrakeReplacement && loco.RemoteControlGroup == 0 && loco.AbsTractionSpeedMpS < loco.DynamicBrakeEngineBrakeReplacementSpeed && loco.Train.LeadLocomotive is MSTSLocomotive lead && lead.TrainBrakeController.TrainDynamicBrakeIntervention > 0)
                        {
                            var requiredBrakeForceN = loco.MaxDynamicBrakeForceN * lead.TrainBrakeController.TrainDynamicBrakeIntervention;
                            var reverseBlendingPressurePSI = Math.Min(Math.Max((requiredBrakeForceN - loco.DynamicBrakeForceN) / Car.FrictionBrakeBlendingMaxForceN * ReferencePressurePSI
                            + BrakeCylinderSpringPressurePSI, 0), MaxCylPressurePSI);
                            reverseBlendingPressurePSI /= RelayValveRatio;
                            if (demandedPressurePSI < reverseBlendingPressurePSI) demandedPressurePSI = reverseBlendingPressurePSI;
                        }
                    }
                }
            }
            if (RelayValveFitted)
            {
                if (EmergencySolenoidValve)
                {
                    var lead = Car.Train.LeadLocomotive as MSTSLocomotive;
                    if (lead != null && lead.TrainBrakeController.EmergencyBraking)
                    {
                        demandedPressurePSI = MaxCylPressurePSI / Math.Max(RelayValveRatio, TwoStageRelayValveRatio);
                    }
                }
                float automaticDemandedPressurePSI = demandedPressurePSI * (TwoStageLowSpeedActive ? TwoStageRelayValveRatio : RelayValveRatio);
                float engineDemandedPressurePSI = BrakeLine3PressurePSI * EngineRelayValveRatio;

                // Determine how in-shot will affect the demanded pressure.
                // In-shot introduces a small additional application pressure at a 1:1 ratio
                // In-shot can add on to the demanded pressure, or override the demanded pressure
                // For ORTS: in-shot setting > 0 adds, setting < 0 overrides. 'Negative inshot' is not a real thing, only a simulation compromise
                automaticDemandedPressurePSI = Math.Min(demandedPressurePSI, Math.Max(RelayValveInshotPSI, 0))
                    + Math.Max(Math.Min(demandedPressurePSI, -RelayValveInshotPSI), automaticDemandedPressurePSI);
                engineDemandedPressurePSI = Math.Min(BrakeLine3PressurePSI, Math.Max(EngineRelayValveInshotPSI, 0))
                    + Math.Max(Math.Min(BrakeLine3PressurePSI, -EngineRelayValveInshotPSI), engineDemandedPressurePSI);

                demandedPressurePSI = Math.Max(automaticDemandedPressurePSI, engineDemandedPressurePSI);

                if (demandedPressurePSI > CylPressurePSI)
                {
                    float dp = elapsedClockSeconds * RelayValveApplicationRatePSIpS;
                    // Reduce glitchyness caused by extremely low demanded pressures
                    if (CylSource != CylinderSource.AuxRes && demandedPressurePSI < BrakeCylinderSpringPressurePSI / 2.0f)
                        dp *= 0.2f;
                    if (dp > demandedPressurePSI - CylPressurePSI)
                        dp = demandedPressurePSI - CylPressurePSI;
                    if (MaxCylPressurePSI < CylPressurePSI + dp)
                        dp = MaxCylPressurePSI - CylPressurePSI;

                    float volumeRatio;
                    switch (CylSource)
                    {
                        case CylinderSource.SupplyRes:
                            volumeRatio = TotalCylVolumeM3 / SupplyResVolumeM3;
                            if (SupplyResPressurePSI - (dp * volumeRatio) < CylPressurePSI + dp)
                                dp = (SupplyResPressurePSI - CylPressurePSI) / (1 + volumeRatio);
                            SupplyResPressurePSI -= dp * volumeRatio;
                            break;
                        case CylinderSource.MainRes:
                            if (loco != null)
                            {
                                volumeRatio = TotalCylVolumeM3 / loco.MainResVolumeM3;
                                if (loco.MainResPressurePSI - (dp * volumeRatio) < CylPressurePSI + dp)
                                    dp = (loco.MainResPressurePSI - CylPressurePSI) / (1 + volumeRatio);
                                loco.MainResPressurePSI -= dp * volumeRatio;
                            }
                            break;
                        case CylinderSource.MainResPipe:
                            volumeRatio = TotalCylVolumeM3 / BrakePipeVolumeM3;
                            if (BrakeLine2PressurePSI - (dp * volumeRatio) < CylPressurePSI + dp)
                                dp = (BrakeLine2PressurePSI - CylPressurePSI) / (1 + volumeRatio);
                            BrakeLine2PressurePSI -= dp * volumeRatio;
                            break;
                    }

                    CylPressurePSI = CalculateBrakeCylinderPressure(ref CylAirPSIM3, dp, demandedPressurePSI);
                }
                else if (demandedPressurePSI < CylPressurePSI)
                {
                    float dp = elapsedClockSeconds * RelayValveReleaseRatePSIpS;
                    // Reduce glitchyness caused by extremely low demanded pressures
                    if (CylSource != CylinderSource.AuxRes && demandedPressurePSI < BrakeCylinderSpringPressurePSI / 2.0f)
                        dp *= 0.2f;
                    if (dp > CylPressurePSI - demandedPressurePSI)
                        dp = CylPressurePSI - demandedPressurePSI;
                    if (CylPressurePSI - dp < 0)
                        dp = CylPressurePSI;

                    CylPressurePSI = CalculateBrakeCylinderPressure(ref CylAirPSIM3, -dp, demandedPressurePSI);
                }
            }
            else
            {
                float dp = Math.Max(demandedPressurePSI, BrakeLine3PressurePSI) - CylPressurePSI;
                CylPressurePSI = CalculateBrakeCylinderPressure(ref CylAirPSIM3, dp, Math.Max(demandedPressurePSI, BrakeLine3PressurePSI));
            }

            // Update brake cylinder travel for advanced brake cylinder simulation
            if (CylDiameterM > 0 && Car.Train.IsPlayerDriven)
                CurrentCylTravelM = CylTravelTab[CylAirPSIM3];
            else
                CurrentCylTravelM = CylPressurePSI > BrakeCylinderSpringPressurePSI ? CylStrokeM : 0.0f;

            // During braking wheelslide control is effected throughout the train by additional equipment on each vehicle. In the piping to each pair of brake cylinders are fitted electrically operated 
            // dump valves. When axle rotations which are sensed electrically, differ by a predetermined speed the dump valves are operated releasing brake cylinder pressure to both axles of the affected 
            // bogie.

            // Dump valve operation will cease when differences in axle rotations arewithin specified limits or the axle accelerates faster than a specified rate. The dump valve resets whenever the wheel
            // creep speed drops to normal. The dump valve will only operate continuously for a maximum period of seven seconds after which time it will be de-energised and the dump valve will not 
            // re-operate until the train has stopped or the throttle operated. 

            // Dump valve operation is prevented under the following conditions:-
            // (i) When the Power Controller is open.

            // (ii) When Brake Pipe Pressure has been reduced below 250 kPa (36.25psi). 

            if (Car.WheelBrakeSlideProtectionFitted && Car.Train.IsPlayerDriven)
            {
                // WSP dump valve active
                if ((Car.BrakeSkidWarning || Car.BrakeSkid) && CylPressurePSI > 0 && !Car.WheelBrakeSlideProtectionDumpValveLockout && ( (!Car.WheelBrakeSlideProtectionLimitDisabled && BrakeLine1PressurePSI > 36.25) || Car.WheelBrakeSlideProtectionLimitDisabled) )
                {
                    Car.WheelBrakeSlideProtectionActive = true;

                    float dp = elapsedClockSeconds * MaxReleaseRatePSIpS;

                    if (AuxResPressurePSI - dp < 0)
                        dp = AuxResPressurePSI;
                    if (CylSource == CylinderSource.AuxRes)
                        AutoCylPressurePSI = CalculateBrakeCylinderPressure(ref AutoCylAirPSIM3, -dp, 0);
                    else
                        AutoCylPressurePSI -= dp;
                    Car.WheelBrakeSlideProtectionTimerS -= elapsedClockSeconds;

                    // Lockout WSP dump valve if it is open for greater then 7 seconds continuously
                    if (Car.WheelBrakeSlideProtectionTimerS <= 0)
                    {
                        Car.WheelBrakeSlideProtectionDumpValveLockout = true;
                    }

                }
                else if (!Car.WheelBrakeSlideProtectionDumpValveLockout)
                {
                    // WSP dump valve stops
                    Car.WheelBrakeSlideProtectionActive = false;
                    Car.WheelBrakeSlideProtectionTimerS = Car.wheelBrakeSlideTimerResetValueS; // Reset WSP timer if 
                }

            }

            if (Car is MSTSLocomotive) UpdateTractionCutoff();
                       
            // Record HUD display values for brake cylinders depending upon whether they are wagons or locomotives/tenders (which are subject to their own engine brakes)   
            if (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender)
            {
                Car.Train.HUDLocomotiveBrakeCylinderPSI = CylPressurePSI;
                Car.Train.HUDWagonBrakeCylinderPSI = Car.Train.HUDLocomotiveBrakeCylinderPSI;  // Initially set Wagon value same as locomotive, will be overwritten if a wagon is attached
            }
            else
            {
                // Record the Brake Cylinder pressure in first wagon, as EOT is also captured elsewhere, and this will provide the two extremeties of the train
                // Identifies the first wagon based upon the previously identified UiD 
                if (Car.UiD == Car.Train.FirstCarUiD)
                {
                    Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSI;
                }

            }

            // If wagons are not attached to the locomotive, then set wagon BC pressure to same as locomotive in the Train brake line
            if (!Car.Train.WagonsAttached &&  (Car.WagonType == MSTSWagon.WagonTypes.Engine || Car.WagonType == MSTSWagon.WagonTypes.Tender) ) 
            {
                Car.Train.HUDWagonBrakeCylinderPSI = CylPressurePSI;
            }
            
            if (!Car.BrakesStuck)
            {
                Car.BrakeShoeForceN = Car.MaxBrakeForceN * MathHelper.Max((CylPressurePSI - BrakeCylinderSpringPressurePSI) / (ReferencePressurePSI - BrakeCylinderSpringPressurePSI), 0);
                if (Car.BrakeShoeForceN < Car.MaxHandbrakeForceN * HandbrakePercent / 100)
                    Car.BrakeShoeForceN = Car.MaxHandbrakeForceN * HandbrakePercent / 100;
            }
            else Car.BrakeShoeForceN = Math.Max(Car.MaxBrakeForceN, Car.MaxHandbrakeForceN / 2);

            float brakeShoeFriction = Car.GetBrakeShoeFrictionFactor();
            Car.HuDBrakeShoeFriction = Car.GetBrakeShoeFrictionCoefficientHuD();

            Car.BrakeRetardForceN = Car.BrakeShoeForceN * brakeShoeFriction; // calculates value of force applied to wheel, independent of wheel skid

            // sound trigger checking runs every half second, to avoid the problems caused by the jumping BrakeLine1PressurePSI value, and also saves cpu time :)
            if (SoundTriggerCounter >= 0.5f)
            {
                SoundTriggerCounter = 0f;
                if (Math.Abs(AutoCylPressurePSI - PrevCylPressurePSI) > 0.1f) //(AutoCylPressurePSI != prevCylPressurePSI)
                {
                    if (!TrainBrakePressureChanging)
                    {
                        if (AutoCylPressurePSI > PrevCylPressurePSI)
                            Car.SignalEvent(Event.TrainBrakePressureIncrease);
                        else
                            Car.SignalEvent(Event.TrainBrakePressureDecrease);
                        TrainBrakePressureChanging = !TrainBrakePressureChanging;
                    }

                }
                else if (TrainBrakePressureChanging)
                {
                    TrainBrakePressureChanging = !TrainBrakePressureChanging;
                    Car.SignalEvent(Event.TrainBrakePressureStoppedChanging);
                }

                bool brakePipePressureChanged = false;
                bool brakePipePressureIncreased = false;
                if (loco != null) // Use locomotive brake application/release rate to trigger sounds
                {
                    if (loco.BrakePipeFlowM3pS >  0.015f)
                    {
                        brakePipePressureChanged = true;
                        brakePipePressureIncreased = true;
                    }
                    else if (loco.BrakePipeFlowM3pS < -0.005f)
                    {
                        brakePipePressureChanged = true;
                        brakePipePressureIncreased = false;
                    }
                }    
                else
                {
                    if (Math.Abs(BrakeLine1PressurePSI - PrevBrakePipePressurePSI_sound) > 0.1f)
                    {
                        brakePipePressureChanged = true;
                        brakePipePressureIncreased = BrakeLine1PressurePSI > PrevBrakePipePressurePSI;
                    }
                }

                if (brakePipePressureChanged)
                {
                    if (!BrakePipePressureChanging)
                    {
                        if (brakePipePressureIncreased)
                            Car.SignalEvent(Event.BrakePipePressureIncrease);
                        else
                            Car.SignalEvent(Event.BrakePipePressureDecrease);
                        BrakePipePressureChanging = !BrakePipePressureChanging;
                    }

                }
                else if (BrakePipePressureChanging)
                {
                    BrakePipePressureChanging = !BrakePipePressureChanging;
                    Car.SignalEvent(Event.BrakePipePressureStoppedChanging);
                }
                PrevCylPressurePSI = AutoCylPressurePSI;
                PrevBrakePipePressurePSI_sound = BrakeLine1PressurePSI;

                var lead = Car as MSTSLocomotive;

                if (lead != null && Car.WagonType == MSTSWagon.WagonTypes.Engine)
                {
                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Overcharge && !lead.BrakeOverchargeSoundOn)
                    {
                        Car.SignalEvent(Event.OverchargeBrakingOn);
                        lead.BrakeOverchargeSoundOn = true;
                    }
                    else if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Overcharge && lead.BrakeOverchargeSoundOn)
                    {
                        Car.SignalEvent(Event.OverchargeBrakingOff);
                        lead.BrakeOverchargeSoundOn = false;
                    }
                }
            }
            PrevBrakePipePressurePSI = BrakeLine1PressurePSI;
            SoundTriggerCounter += elapsedClockSeconds;
        }

        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            PropagateBrakeLinePressures(elapsedClockSeconds, Car, TwoPipes);
        }

        protected static void PropagateBrakeLinePressures(float elapsedClockSeconds, TrainCar trainCar, bool twoPipes)
        {
            var train = trainCar.Train;
            var lead = trainCar as MSTSLocomotive;
            var leadLocos = train.DPLeadUnits;

            float tempBrakePipeFlow = 0.0f; // Flow calculation will assume 0 flow unless calculated otherwise

            // Propagate brake line (1) data if pressure gradient disabled
            if (lead != null && lead.BrakePipeChargingRatePSIorInHgpS >= 1000)
            {   // pressure gradient disabled
                if (lead.BrakeSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg)
                {
                    var dp1 = train.EqualReservoirPressurePSIorInHg - lead.BrakeSystem.BrakeLine1PressurePSI;
                    lead.MainResPressurePSI -= dp1 * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3;

                    tempBrakePipeFlow = (dp1 * lead.BrakeSystem.BrakePipeVolumeM3) / (OneAtmospherePSI * elapsedClockSeconds); // Instantaneous flow rate from MR to BP
                }
                foreach (TrainCar car in train.Cars)
                {
                    if (car.BrakeSystem.BrakeLine1PressurePSI >= 0)
                        car.BrakeSystem.BrakeLine1PressurePSI = train.EqualReservoirPressurePSIorInHg;
                    if (car.BrakeSystem.TwoPipes)
                        car.BrakeSystem.BrakeLine2PressurePSI = Math.Min(lead.MainResPressurePSI, lead.MaximumMainReservoirPipePressurePSI);
                }

                lead.BrakePipeFlowM3pS = tempBrakePipeFlow;
                lead.FilteredBrakePipeFlowM3pS = lead.AFMFilter.Filter(lead.BrakePipeFlowM3pS, elapsedClockSeconds); // Actual flow rate displayed by air flow meter
            }
            else
            {   // approximate pressure gradient in train pipe line1
                var brakePipeTimeFactorS = lead?.BrakePipeTimeFactorS ?? 0.0015f;
                int nSteps = (int)(elapsedClockSeconds / brakePipeTimeFactorS + 1);
                float trainPipeTimeVariationS = elapsedClockSeconds / nSteps;
                float trainPipeLeakLossPSI = lead?.TrainBrakePipeLeakPSIorInHgpS * trainPipeTimeVariationS ?? 0.0f;
                float serviceTimeFactor = lead?.BrakeServiceTimeFactorPSIpS ?? 0.001f;
                float emergencyTimeFactor = lead?.BrakeEmergencyTimeFactorPSIpS ?? 0.001f;
                for (int i = 0; i < nSteps; i++)
                {
                    // Apply and release brakes from every DP lead unit
                    foreach (TrainCar locoCar in leadLocos)
                    {
                        if (locoCar is MSTSLocomotive loco && lead != null && loco.BrakeSystem is AirSinglePipe locoAirSystem)
                        {
                            // Only sync application/release on DP units if both the lead unit AND the DP lead unit are set to synchronize
                            // Lead locomotive will always be allowed to apply/release
                            bool syncApplication = loco == lead || (loco.DPSyncTrainApplication && lead.DPSyncTrainApplication);
                            bool syncRelease = loco == lead || (loco.DPSyncTrainRelease && lead.DPSyncTrainRelease);
                            bool syncEmergency = loco == lead || (loco.DPSyncEmergency && lead.DPSyncEmergency);

                            tempBrakePipeFlow = 0.0f;

                            // Emergency brake - vent brake pipe to 0 psi regardless of equalizing res pressure
                            if (syncEmergency && lead.TrainBrakeController.EmergencyBraking)
                            {
                                float emergencyVariationFactor = Math.Min(trainPipeTimeVariationS / emergencyTimeFactor, 0.95f);
                                float pressureDiffPSI = emergencyVariationFactor * locoAirSystem.BrakeLine1PressurePSI;

                                if (locoAirSystem.BrakeLine1PressurePSI - pressureDiffPSI < 0)
                                    pressureDiffPSI = locoAirSystem.BrakeLine1PressurePSI;
                                locoAirSystem.BrakeLine1PressurePSI -= pressureDiffPSI;

                                // Instantaneous flow rate out of BP to atmosphere
                                // Flow meters don't display this, still keeping track of it for reference
                                tempBrakePipeFlow = -(pressureDiffPSI * locoAirSystem.BrakePipeVolumeM3) / (OneAtmospherePSI * trainPipeTimeVariationS);
                            }
                            else if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Neutral)
                            {
                                // Charge train brake pipe - adjust main reservoir pressure, and loco brake pressure line to maintain brake pipe equal to equalising resevoir pressure - release brakes
                                if (syncRelease && locoAirSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg && lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Lap)
                                {
                                    // Calculate change in brake pipe pressure between equalising reservoir and loco brake pipe
                                    float chargingRatePSIpS = loco.BrakePipeChargingRatePSIorInHgpS;
                                    if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullQuickRelease || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Overcharge)
                                        chargingRatePSIpS = loco.BrakePipeQuickChargingRatePSIpS;
                                    float PressureDiffEqualToPipePSI = trainPipeTimeVariationS * chargingRatePSIpS; // default condition - if EQ Res is higher then Brake Pipe Pressure

                                    float chargeSlowdown = chargingRatePSIpS / 4.0f; // Estimate of when charging starts to be choked by feed valve
                                    float supplyPressure = locoAirSystem.SupplyReservoirPresent ? locoAirSystem.SupplyResPressurePSI : loco.MainResPressurePSI; // Pressure of reservoir used for brake pipe charging

                                    if (supplyPressure - locoAirSystem.BrakeLine1PressurePSI < 15.0f) // Reduce recharge rate if near MR pressure as per reality
                                        PressureDiffEqualToPipePSI *= MathHelper.Lerp(0, 1.0f, (supplyPressure - locoAirSystem.BrakeLine1PressurePSI) / 15.0f);
                                    if (train.EqualReservoirPressurePSIorInHg - locoAirSystem.BrakeLine1PressurePSI < chargeSlowdown) // Reduce recharge rate if near EQ to simulate feed valve behavior
                                        PressureDiffEqualToPipePSI *= (float)Math.Pow((train.EqualReservoirPressurePSIorInHg - locoAirSystem.BrakeLine1PressurePSI) / chargeSlowdown,
                                            1.0f/3.0f);

                                    if (locoAirSystem.BrakeLine1PressurePSI + PressureDiffEqualToPipePSI > train.EqualReservoirPressurePSIorInHg)
                                        PressureDiffEqualToPipePSI = train.EqualReservoirPressurePSIorInHg - locoAirSystem.BrakeLine1PressurePSI;
                                    if (locoAirSystem.BrakeLine1PressurePSI + PressureDiffEqualToPipePSI > supplyPressure)
                                        PressureDiffEqualToPipePSI = supplyPressure - locoAirSystem.BrakeLine1PressurePSI;
                                    if (PressureDiffEqualToPipePSI < 0)
                                        PressureDiffEqualToPipePSI = 0;

                                    // Adjust brake pipe pressure based upon pressure differential
                                    locoAirSystem.BrakeLine1PressurePSI += PressureDiffEqualToPipePSI;

                                    if (locoAirSystem.SupplyReservoirPresent)
                                        locoAirSystem.SupplyResPressurePSI -= PressureDiffEqualToPipePSI * locoAirSystem.BrakePipeVolumeM3 / locoAirSystem.SupplyResVolumeM3;
                                    else
                                        loco.MainResPressurePSI -= PressureDiffEqualToPipePSI * locoAirSystem.BrakePipeVolumeM3 / loco.MainResVolumeM3;

                                    // Instantaneous flow rate from MR to BP
                                    tempBrakePipeFlow = (PressureDiffEqualToPipePSI * locoAirSystem.BrakePipeVolumeM3) / (OneAtmospherePSI * trainPipeTimeVariationS);
                                }
                                // reduce pressure in loco brake line if brake pipe pressure is above equalising pressure - apply brakes
                                else if (syncApplication && locoAirSystem.BrakeLine1PressurePSI > train.EqualReservoirPressurePSIorInHg)
                                {
                                    float serviceVariationFactor = Math.Min(trainPipeTimeVariationS / serviceTimeFactor, 0.95f);
                                    float pressureDiffPSI = serviceVariationFactor * locoAirSystem.BrakeLine1PressurePSI;

                                    if (train.EqualReservoirPressurePSIorInHg > locoAirSystem.BrakeLine1PressurePSI - 5.0f) // Reduce exhausting rate if near EQ pressure to simulate feed valve
                                        pressureDiffPSI *= Math.Min((float)Math.Sqrt((locoAirSystem.BrakeLine1PressurePSI - train.EqualReservoirPressurePSIorInHg) / 5.0f), 1.0f);
                                    if (locoAirSystem.BrakeLine1PressurePSI - pressureDiffPSI < train.EqualReservoirPressurePSIorInHg)
                                        pressureDiffPSI = locoAirSystem.BrakeLine1PressurePSI - train.EqualReservoirPressurePSIorInHg;
                                    locoAirSystem.BrakeLine1PressurePSI -= pressureDiffPSI;

                                    // Instantaneous flow rate out of BP to atmosphere
                                    // Flow meters don't display this, still keeping track of it for reference
                                    tempBrakePipeFlow = -(pressureDiffPSI * locoAirSystem.BrakePipeVolumeM3) / (OneAtmospherePSI * trainPipeTimeVariationS);
                                }
                            }

                            // Finish updating air flow meter
                            loco.BrakePipeFlowM3pS = tempBrakePipeFlow;
                            loco.FilteredBrakePipeFlowM3pS = loco.AFMFilter.Filter(Math.Max(loco.BrakePipeFlowM3pS, 0.0f), trainPipeTimeVariationS); // Actual flow rate displayed by air flow meter
                        }

                        if (lead != null)
                            train.LeadPipePressurePSI = lead.BrakeSystem.BrakeLine1PressurePSI;  // Keep a record of current train pipe pressure in lead locomotive
                    }

                    // Propagate air pipe pressure along the train (brake pipe and main reservoir pipe)
#if DEBUG_TRAIN_PIPE_LEAK

                    Trace.TraceInformation("======================================= Train Pipe Leak (AirSinglePipe) ===============================================");
                    Trace.TraceInformation("Before:  CarID {0}  TrainPipeLeak {1} Lead BrakePipe Pressure {2}", trainCar.CarID, lead.TrainBrakePipeLeakPSIpS, lead.BrakeSystem.BrakeLine1PressurePSI);
                    Trace.TraceInformation("Brake State {0}", lead.TrainBrakeController.TrainBrakeControllerState);
                    Trace.TraceInformation("Main Resevoir {0} Compressor running {1}", lead.MainResPressurePSI, lead.CompressorIsOn);

#endif
                    train.TotalTrainBrakePipeVolumeM3 = 0.0f; // initialise train brake pipe volume
                    for (int carIndex=0; carIndex < train.Cars.Count; carIndex++)              
                    {
                        TrainCar car = train.Cars[carIndex];
                        TrainCar nextCar = carIndex < train.Cars.Count - 1 ? train.Cars[carIndex + 1] : null;
                        TrainCar prevCar = carIndex > 0 ? train.Cars[carIndex - 1] : null;
                        train.TotalTrainBrakePipeVolumeM3 += car.BrakeSystem.BrakePipeVolumeM3; // Calculate total brake pipe volume of train

                        // Allow for leaking brake pipe, using leak rate defined in lead locomotive .eng file
                        car.BrakeSystem.BrakeLine1PressurePSI -= trainPipeLeakLossPSI;
                        if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                            car.BrakeSystem.BrakeLine1PressurePSI = 0;

                        if (prevCar != null && car.BrakeSystem.FrontBrakeHoseConnected && car.BrakeSystem.AngleCockAOpen && prevCar.BrakeSystem.AngleCockBOpen)
                        {
                            // Brake pipe
                            {
                                float pressureDiffPSI = car.BrakeSystem.BrakeLine1PressurePSI - prevCar.BrakeSystem.BrakeLine1PressurePSI;
                                // Based on the principle of pressure equalization between adjacent cars
                                // First, we define a variable storing the pressure diff between cars, but limited to a maximum flow rate depending on pipe characteristics
                                // The sign in the equation determines the direction of air flow.
                                float trainPipePressureDiffPropagationPSI = pressureDiffPSI * Math.Min(trainPipeTimeVariationS / brakePipeTimeFactorS, 1);

                                // Flow is restricted when either anglecock is not opened fully
                                if (car.BrakeSystem.AngleCockAOpenAmount < 1 || prevCar.BrakeSystem.AngleCockBOpenAmount < 1)
                                    trainPipePressureDiffPropagationPSI *= MathHelper.Min((float)Math.Pow(car.BrakeSystem.AngleCockAOpenAmount * prevCar.BrakeSystem.AngleCockBOpenAmount, 2), 1.0f);

                                // Air flows from high pressure to low pressure, until pressure is equal in both cars.
                                // Brake pipe volumes of both cars are taken into account, so pressure increase/decrease is proportional to relative volumes.
                                // If TrainPipePressureDiffPropagationPSI equals to p1-p0 the equalization is achieved in one step.
                                car.BrakeSystem.BrakeLine1PressurePSI -= trainPipePressureDiffPropagationPSI * prevCar.BrakeSystem.BrakePipeVolumeM3 / (prevCar.BrakeSystem.BrakePipeVolumeM3 + car.BrakeSystem.BrakePipeVolumeM3);
                                prevCar.BrakeSystem.BrakeLine1PressurePSI += trainPipePressureDiffPropagationPSI * car.BrakeSystem.BrakePipeVolumeM3 / (prevCar.BrakeSystem.BrakePipeVolumeM3 + car.BrakeSystem.BrakePipeVolumeM3);
                            }
                            // Main reservoir pipe
                            if (prevCar.BrakeSystem.TwoPipes && car.BrakeSystem.TwoPipes)
                            {
                                float pressureDiffPSI = car.BrakeSystem.BrakeLine2PressurePSI - prevCar.BrakeSystem.BrakeLine2PressurePSI;
                                float trainPipePressureDiffPropagationPSI = pressureDiffPSI * Math.Min(trainPipeTimeVariationS / brakePipeTimeFactorS, 1);
                                car.BrakeSystem.BrakeLine2PressurePSI -= trainPipePressureDiffPropagationPSI * prevCar.BrakeSystem.BrakePipeVolumeM3 / (prevCar.BrakeSystem.BrakePipeVolumeM3 + car.BrakeSystem.BrakePipeVolumeM3);
                                prevCar.BrakeSystem.BrakeLine2PressurePSI += trainPipePressureDiffPropagationPSI * car.BrakeSystem.BrakePipeVolumeM3 / (prevCar.BrakeSystem.BrakePipeVolumeM3 + car.BrakeSystem.BrakePipeVolumeM3);
                            }
                        }
                        // Empty the brake pipe if the brake hose is not connected and angle cocks are open
                        if (!car.BrakeSystem.FrontBrakeHoseConnected && car.BrakeSystem.AngleCockAOpen)
                        {
                            float dp = car.BrakeSystem.BrakeLine1PressurePSI * trainPipeTimeVariationS / brakePipeTimeFactorS;

                            if (car.BrakeSystem.AngleCockAOpenAmount < 1)
                                dp *= MathHelper.Clamp((float)Math.Pow(car.BrakeSystem.AngleCockAOpenAmount, 2), 0.0f, 1.0f);

                            if (car.BrakeSystem.BrakeLine1PressurePSI - dp < 0)
                                dp = car.BrakeSystem.BrakeLine1PressurePSI;
                            car.BrakeSystem.BrakeLine1PressurePSI -= dp;
                        }
                        if ((nextCar == null || !nextCar.BrakeSystem.FrontBrakeHoseConnected) && car.BrakeSystem.AngleCockBOpen)
                        {
                            float dp = car.BrakeSystem.BrakeLine1PressurePSI * trainPipeTimeVariationS / brakePipeTimeFactorS;

                            if (car.BrakeSystem.AngleCockBOpenAmount < 1)
                                dp *= MathHelper.Clamp((float)Math.Pow(car.BrakeSystem.AngleCockBOpenAmount, 2), 0.0f, 1.0f);

                            if (car.BrakeSystem.BrakeLine1PressurePSI - dp < 0)
                                dp = car.BrakeSystem.BrakeLine1PressurePSI;
                            car.BrakeSystem.BrakeLine1PressurePSI -= dp;
                        }
                    }
#if DEBUG_TRAIN_PIPE_LEAK
                    Trace.TraceInformation("After: Lead Brake Pressure {0}", lead.BrakeSystem.BrakeLine1PressurePSI);
#endif
                }
            }
            // Join main reservoirs of adjacent locomotives
            foreach (List<TrainCar> locoGroup in train.LocoGroups)
            {
                float sumM3 = 0;
                float sumPSIM3 = 0;

                foreach (TrainCar locoCar in locoGroup)
                {
                    if (!locoCar.BrakeSystem.TwoPipes && locoCar is MSTSLocomotive loco)
                    {
                        sumM3 += loco.MainResVolumeM3;
                        sumPSIM3 += loco.MainResVolumeM3 * loco.MainResPressurePSI;
                    }
                }

                float totalReservoirPressurePSI = 0;
                if (sumM3 > 0)
                    totalReservoirPressurePSI = sumPSIM3 / sumM3;

                foreach (TrainCar locoCar in locoGroup)
                {
                    if (locoCar is MSTSLocomotive loco)
                    {
                        if (loco.BrakeSystem.TwoPipes)
                        {
                            // Equalize air in MR and MR pipe
                            float volumeRatio = loco.BrakeSystem.BrakePipeVolumeM3 / loco.MainResVolumeM3;
                            float dp = Math.Min((loco.MainResPressurePSI - loco.BrakeSystem.BrakeLine2PressurePSI) / (1 + volumeRatio),
                                loco.MaximumMainReservoirPipePressurePSI - loco.BrakeSystem.BrakeLine2PressurePSI);

                            loco.MainResPressurePSI -= dp * volumeRatio;
                            loco.BrakeSystem.BrakeLine2PressurePSI += dp;

                            if (loco.MainResPressurePSI < 0)
                                loco.MainResPressurePSI = 0;
                            if (loco.BrakeSystem.BrakeLine2PressurePSI < 0)
                                loco.BrakeSystem.BrakeLine2PressurePSI = 0;
                        }
                        else // Workaround to emulate MR pipe for single pipe locomotives
                            loco.MainResPressurePSI = totalReservoirPressurePSI;

                        // Continue updating flowmeter on non-lead locomotives so it zeroes out eventually
                        if (!leadLocos.Contains(locoCar) && loco.FilteredBrakePipeFlowM3pS != 0)
                        {
                            loco.BrakePipeFlowM3pS = 0;
                            loco.FilteredBrakePipeFlowM3pS = loco.AFMFilter.Filter(0, elapsedClockSeconds);
                        }
                    }
                }
            }
            // Update train total flowmeter
            float tempTrainFlow = 0;

            foreach (TrainCar locoCar in leadLocos)
            {
                if (locoCar is MSTSLocomotive loco)
                    tempTrainFlow += loco.FilteredBrakePipeFlowM3pS;
            }

            train.TotalBrakePipeFlowM3pS = tempTrainFlow;

            // Propagate engine brake pipe (3) data
            // Locomotive will try to match BC pressure with pressure in engine brake pipe
            for (int i = 0; i < train.LocoGroups.Count; i++)
            {
                MSTSLocomotive leadLoco = train.DPLeadUnits[i] as MSTSLocomotive;
                BrakeSystem locoBrakeSystem = train.DPLeadUnits[i].BrakeSystem;

                bool syncIndependent = lead != null && (leadLoco == lead || (leadLoco.DPSyncIndependent && lead.DPSyncIndependent));

                // Set loco brake pressure on all units with brakes cut in
                // Only set loco brake pressure on DP units if lead loco AND DP loco are equipped to synchronize braking
                if (leadLoco != null && syncIndependent)
                {
                    float locoBrakePressure = locoBrakeSystem.BrakeLine3PressurePSI;
                    float demandedBrakePressure;
                    float dp = 0.0f;
                    var prevState = leadLoco.EngineBrakeState;
                    // Volume ratio between MR and engine brake pipe, assuming the pipe is equal size as the train pipe
                    float volumeRatio = locoBrakeSystem.BrakePipeVolumeM3 / leadLoco.MainResVolumeM3;

                    // Lead locomotive sets pressure depending on pressure demanded by loco brake handle
                    if (leadLoco == lead)
                    {
                        demandedBrakePressure = train.BrakeLine3PressurePSI;
                    }
                    else // Distributed power lead unit
                    {
                        // DP units work by trying to match the BC pressure of the master locomotive
                        if (locoBrakeSystem is AirSinglePipe airSystem)
                        {
                            demandedBrakePressure = lead.BrakeSystem.GetCylPressurePSI() / airSystem.EngineRelayValveRatio;

                            // Auto brake application will be bailed off if it's too great
                            if (lead.BrakeSystem.GetCylPressurePSI() < airSystem.AutoCylPressurePSI * airSystem.RelayValveRatio)
                                leadLoco.BailOff = true;
                            if (airSystem.AutoCylPressurePSI == 0.0f)
                                leadLoco.BailOff = false;
                        }
                        else // Backup if brake system fails to cast to AirSinglePipe
                            demandedBrakePressure = train.BrakeLine3PressurePSI;
                    }

                    // Current pressure is less than demanded pressure, apply brakes
                    if (locoBrakePressure < demandedBrakePressure && locoBrakePressure < leadLoco.MainResPressurePSI)
                    {
                        dp = elapsedClockSeconds * leadLoco.EngineBrakeApplyRatePSIpS;

                        if (locoBrakePressure + dp > demandedBrakePressure)
                            dp = demandedBrakePressure - locoBrakePressure;
                        if (locoBrakePressure + dp > leadLoco.MainResPressurePSI - dp * volumeRatio)
                            dp = (leadLoco.MainResPressurePSI - demandedBrakePressure) / (1 + volumeRatio);
                        if (dp < 0)
                            dp = 0;
                        leadLoco.MainResPressurePSI -= dp * volumeRatio;
                        locoBrakeSystem.BrakeLine3PressurePSI += dp;

                        leadLoco.EngineBrakeState = ValveState.Apply;
                    }
                    else if (locoBrakePressure > demandedBrakePressure) // Release brakes
                    {
                        dp = elapsedClockSeconds * leadLoco.EngineBrakeReleaseRatePSIpS;

                        if (locoBrakePressure - dp < demandedBrakePressure)
                            dp = locoBrakePressure - demandedBrakePressure;
                        if (dp < 0)
                            dp = 0;
                        locoBrakeSystem.BrakeLine3PressurePSI -= dp;

                        leadLoco.EngineBrakeState = ValveState.Release;
                    }
                    else // No change needed
                        leadLoco.EngineBrakeState = ValveState.Lap;

                    // Prevent small changes in pressure from triggering audio
                    if (dp / elapsedClockSeconds < 1.0f)
                        leadLoco.EngineBrakeState = ValveState.Lap;

                    // Send messages for loco brake audio to lead locomotive only
                    if (leadLoco == lead && leadLoco.EngineBrakeState != prevState)
                        switch (leadLoco.EngineBrakeState)
                        {
                            case ValveState.Release: leadLoco.SignalEvent(Event.EngineBrakePressureIncrease); break;
                            case ValveState.Apply: leadLoco.SignalEvent(Event.EngineBrakePressureDecrease); break;
                            case ValveState.Lap: leadLoco.SignalEvent(Event.EngineBrakePressureStoppedChanging); break;
                        }
                }

                // Propagate engine brake pipe pressure to MU'd vehicles in a simplified manner
                // Instantly equalizes pressure between adjacent vehicles, may do something more realistic later
                float totalVolume = 0.0f;
                float totalAir = 0.0f;

                foreach (TrainCar loco in train.LocoGroups[i])
                {
                    totalVolume += loco.BrakeSystem.BrakePipeVolumeM3;
                    totalAir += loco.BrakeSystem.BrakePipeVolumeM3 * loco.BrakeSystem.BrakeLine3PressurePSI;
                }
                foreach (TrainCar loco in train.LocoGroups[i])
                    loco.BrakeSystem.BrakeLine3PressurePSI = totalAir / totalVolume;
            }
        }

        public override float InternalPressure(float realPressure)
        {
            return realPressure;
        }

        public override void SetRetainer(RetainerSetting setting)
        {
            switch (setting)
            {
                case RetainerSetting.Exhaust:
                    RetainerPressureThresholdPSI = 0;
                    ReleaseRatePSIpS = MaxReleaseRatePSIpS;
                    RetainerDebugState = "EX";
                    break;
                case RetainerSetting.HighPressure:
                    if (RetainerPositions > 0)
                    {
                        RetainerPressureThresholdPSI = 20;
                        ReleaseRatePSIpS = (50 - 20) / 90f;
                        RetainerDebugState = "HP";
                    }
                    break;
                case RetainerSetting.LowPressure:
                    if (RetainerPositions > 3)
                    {
                        RetainerPressureThresholdPSI = 10;
                        ReleaseRatePSIpS = (50 - 10) / 60f;
                        RetainerDebugState = "LP";
                    }
                    else if (RetainerPositions > 0)
                    {
                        RetainerPressureThresholdPSI = 20;
                        ReleaseRatePSIpS = (50 - 20) / 90f;
                        RetainerDebugState = "HP";
                    }
                    break;
                case RetainerSetting.SlowDirect:
                    RetainerPressureThresholdPSI = 0;
                    ReleaseRatePSIpS = (50 - 10) / 86f;
                    RetainerDebugState = "SD";
                    break;
            }
        }

        public override void SetHandbrakePercent(float percent)
        {
            if (!HandBrakePresent)
            {
                HandbrakePercent = 0;
                return;
            }
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            HandbrakePercent = percent;
        }

        public override void AISetPercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            Car.Train.EqualReservoirPressurePSIorInHg = 90 - (90 - FullServPressurePSI) * percent / 100;
        }

        // used when switching from autopilot to player driven mode, to move from default values to values specific for the trainset
        public void NormalizePressures(float maxPressurePSI)
        {
            if (AuxResPressurePSI > maxPressurePSI) AuxResPressurePSI = maxPressurePSI;
            if (BrakeLine1PressurePSI > maxPressurePSI) BrakeLine1PressurePSI = maxPressurePSI;
            if (EmergResPressurePSI > maxPressurePSI) EmergResPressurePSI = maxPressurePSI;
            if (ControlResPressurePSI > maxPressurePSI) ControlResPressurePSI = maxPressurePSI;
        }

        public override bool IsBraking()
        {
            if (AutoCylPressurePSI > ReferencePressurePSI * 0.3)
            return true;
            return false;
        }

        //Corrects MaxCylPressure (e.g 380.eng) when too high
        public override void CorrectMaxCylPressurePSI(MSTSLocomotive loco)
        {
            if (MaxCylPressurePSI > loco.TrainBrakeController.MaxPressurePSI - MaxCylPressurePSI / AuxCylVolumeRatio)
            {
                MaxCylPressurePSI = loco.TrainBrakeController.MaxPressurePSI * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
            }
        }
    }
}
