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
        readonly static float OneAtmospherePSI = 14.696f;
        protected float HandbrakePercent;
        protected float CylPressurePSI = 64;
        public float AutoCylPressurePSI { get; protected set; } = 64;
        protected float AuxResPressurePSI = 64;
        protected float EmergResPressurePSI = 64;
        protected float ControlResPressurePSI = 64;
        protected float FullServPressurePSI = 50;
        protected float MaxCylPressurePSI = 64;
        protected float MaxTripleValveCylPressurePSI;
        protected float AuxCylVolumeRatio = 2.5f;
        protected float AuxBrakeLineVolumeRatio;
        protected float EmergBrakeLineVolumeRatio;
        protected float CylBrakeLineVolumeRatio;
        protected float EmergResVolumeM3 = 0.07f;
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
        protected float EmergAuxVolumeRatio = 1.4f;
        protected bool RelayValveFitted = false;
        public float RelayValveRatio { get; protected set; } = 1;
        protected float EngineRelayValveRatio = 0;
        protected float RelayValveApplicationRatePSIpS = 50;
        protected float RelayValveReleaseRatePSIpS = 50;
        protected string DebugType = string.Empty;
        protected string RetainerDebugState = string.Empty;
        protected bool MRPAuxResCharging;
        protected float CylVolumeM3;
        protected bool EmergResQuickRelease;
        protected float UniformChargingThresholdPSI = 3.0f;
        protected float UniformChargingRatio;
        protected bool UniformChargingActive;
        protected float UniformReleaseThresholdPSI = 3.0f;
        protected float UniformReleaseRatio;
        protected bool UniformReleaseActive;
        protected bool QuickServiceActive;
        protected bool QuickReleaseActive;
        protected float QuickServiceLimitPSI;
        protected float QuickServiceApplicationRatePSIpS;
        protected float QuickServiceVentRatePSIpS;
        protected float AcceleratedApplicationFactor;
        protected float AcceleratedApplicationLimitPSIpS = 5.0f;
        protected float InitialApplicationThresholdPSI;
        protected float TripleValveSensitivityPSI;
        protected float BrakeCylinderSpringPressurePSI;
        protected float ServiceMaxCylPressurePSI;
        protected float ServiceApplicationRatePSIpS;
        protected float TwoStageLowPressurePSI;
        protected float TwoStageSpeedUpMpS;
        protected float TwoStageSpeedDownMpS;
        protected bool TwoStageLowPressureActive;
        protected float HighSpeedReducingPressurePSI;
        protected float AcceleratedEmergencyReleaseThresholdPSI = 20.0f;


        protected bool TrainBrakePressureChanging = false;
        protected bool BrakePipePressureChanging = false;
        protected float SoundTriggerCounter = 0;
        protected float prevCylPressurePSI = 0f;
        protected float prevBrakePipePressurePSI = 0f;
        protected float prevBrakePipePressurePSI_sound = 0f;

        protected float BrakePipeChangePSIpS;
        protected SmoothedData SmoothedBrakePipeChangePSIpS;


        /// <summary>
        /// EP brake holding valve. Needs to be closed (Lap) in case of brake application or holding.
        /// For non-EP brake types must default to and remain in Release.
        /// </summary>
        protected ValveState HoldingValve = ValveState.Release;

        public enum ValveState
        {
            [GetString("Lap")] Lap,
            [GetString("Apply")] Apply,
            [GetString("Release")] Release,
            [GetString("Emergency")] Emergency
        };
        protected ValveState TripleValveState = ValveState.Lap;

        public AirSinglePipe(TrainCar car)
        {
            Car = car;
            // taking into account very short (fake) cars to prevent NaNs in brake line pressures
            BrakePipeVolumeM3 = (0.032f * 0.032f * (float)Math.PI / 4f) * Math.Max ( 5.0f, (1 + car.CarLengthM)); // Using DN32 (1-1/4") pipe
            DebugType = "1P";

            SmoothedBrakePipeChangePSIpS = new SmoothedData(0.25f);

            // Force graduated releasable brakes. Workaround for MSTS with bugs preventing to set eng/wag files correctly for this.
            if (Car.Simulator.Settings.GraduatedRelease) (Car as MSTSWagon).BrakeValve = MSTSWagon.BrakeValveType.Distributor;

            if (Car.Simulator.Settings.RetainersOnAllCars && !(Car is MSTSLocomotive))
                (Car as MSTSWagon).RetainerPositions = 4;
        }

        public override bool GetHandbrakeStatus()
        {
            return HandbrakePercent > 0;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            AirSinglePipe thiscopy = (AirSinglePipe)copy;
            MaxCylPressurePSI = thiscopy.MaxCylPressurePSI;
            AuxCylVolumeRatio = thiscopy.AuxCylVolumeRatio;
            AuxBrakeLineVolumeRatio = thiscopy.AuxBrakeLineVolumeRatio;
            EmergBrakeLineVolumeRatio = thiscopy.EmergBrakeLineVolumeRatio;
            CylBrakeLineVolumeRatio = thiscopy.CylBrakeLineVolumeRatio;
            EmergResVolumeM3 = thiscopy.EmergResVolumeM3;
            BrakePipeVolumeM3 = thiscopy.BrakePipeVolumeM3;
            CylVolumeM3 = thiscopy.CylVolumeM3;
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
            TwoPipes = thiscopy.TwoPipes;
            MRPAuxResCharging = thiscopy.MRPAuxResCharging;
            HoldingValve = thiscopy.HoldingValve;
            RelayValveFitted = thiscopy.RelayValveFitted;
            RelayValveRatio = thiscopy.RelayValveRatio;
            EngineRelayValveRatio = thiscopy.EngineRelayValveRatio;
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
            AcceleratedApplicationFactor = thiscopy.AcceleratedApplicationFactor;
            AcceleratedApplicationLimitPSIpS = thiscopy.AcceleratedApplicationLimitPSIpS;
            InitialApplicationThresholdPSI = thiscopy.InitialApplicationThresholdPSI;
            TripleValveSensitivityPSI = thiscopy.TripleValveSensitivityPSI;
            BrakeCylinderSpringPressurePSI = thiscopy.BrakeCylinderSpringPressurePSI;
            ServiceMaxCylPressurePSI = thiscopy.ServiceMaxCylPressurePSI;
            ServiceApplicationRatePSIpS = thiscopy.ServiceApplicationRatePSIpS;
            TwoStageLowPressurePSI = thiscopy.TwoStageLowPressurePSI;
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
                + $" {Simulator.Catalog.GetString("Flow")} {FormatStrings.FormatAirFlow(loco.FilteredBrakePipeFlowM3pS, loco.IsMetric)}";
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
                (Car as MSTSWagon).EmergencyReservoirPresent ? FormatStrings.FormatPressure(EmergResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.EmergencyReservoir], true) : string.Empty,
                TwoPipes ? FormatStrings.FormatPressure(BrakeLine2PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.MainPipe], true) : string.Empty,
                (Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.Distributor ? FormatStrings.FormatPressure(ControlResPressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.AuxiliaryReservoir], true) : string.Empty,
                (Car as MSTSWagon).RetainerPositions == 0 ? string.Empty : RetainerDebugState,
                Simulator.Catalog.GetString(GetStringAttribute.GetPrettyName(TripleValveState)),
                string.Empty, // Spacer because the state above needs 2 columns.
                (Car as MSTSWagon).HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
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
                case "wagon(ortsbrakeinsensitivity": BrakeInsensitivityPSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, 0.07f); break;
                case "wagon(ortsemergencyvalveactuationrate": EmergencyValveActuationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, 15f); break;
                case "wagon(ortsemergencydumpvalverate": EmergencyDumpValveRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, 15f); break;
                case "wagon(ortsemergencydumpvalvetimer": EmergencyDumpValveTimerS = stf.ReadFloatBlock(STFReader.UNITS.Time, 120.0f); break;
                case "wagon(ortsemergencyquickaction": QuickActionFitted = stf.ReadBoolBlock(false); break;
                case "wagon(ortsmainrespipeauxrescharging": MRPAuxResCharging = this is AirTwinPipe && stf.ReadBoolBlock(true); break;
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
                case "wagon(ortsenginebrakerelayvalveratio": EngineRelayValveRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsbrakerelayvalveapplicationrate": RelayValveApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(ortsbrakerelayvalvereleaserate": RelayValveReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(ortsmaxtriplevalvecylinderpressure": MaxTripleValveCylPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
                case "wagon(ortsbrakecylindervolume": CylVolumeM3 = Me3.FromFt3(stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null)); break;
                case "wagon(ortsemergencyresquickrelease": EmergResQuickRelease = stf.ReadBoolBlock(true); break;
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
                case "wagon(ortstwostageincreasingspeed": TwoStageSpeedUpMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); break;
                case "wagon(ortstwostagedecreasingspeed": TwoStageSpeedDownMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); break;
                case "wagon(ortshighspeedreducingpressure": HighSpeedReducingPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null); break;
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
            outf.Write(AutoCylPressurePSI);
            outf.Write(CylPressurePSI);
            outf.Write(AuxResPressurePSI);
            outf.Write(EmergResPressurePSI);
            outf.Write(ControlResPressurePSI);
            outf.Write(FullServPressurePSI);
            outf.Write((int)TripleValveState);
            outf.Write(FrontBrakeHoseConnected);
            outf.Write(AngleCockAOpen);
            outf.Write(AngleCockAOpenAmount);
            outf.Write(AngleCockBOpen);
            outf.Write(AngleCockBOpenAmount);
            outf.Write(BleedOffValveOpen);
            outf.Write((int)HoldingValve);
            outf.Write(UniformChargingActive);
            outf.Write(UniformReleaseActive);
            outf.Write(QuickServiceActive);
            outf.Write(QuickReleaseActive);
            outf.Write(TwoStageLowPressureActive);
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
            AutoCylPressurePSI = inf.ReadSingle();
            CylPressurePSI = inf.ReadSingle();
            AuxResPressurePSI = inf.ReadSingle();
            EmergResPressurePSI = inf.ReadSingle();
            ControlResPressurePSI = inf.ReadSingle();
            FullServPressurePSI = inf.ReadSingle();
            TripleValveState = (ValveState)inf.ReadInt32();
            FrontBrakeHoseConnected = inf.ReadBoolean();
            AngleCockAOpen = inf.ReadBoolean();
            AngleCockAOpenAmount = inf.ReadSingle();
            AngleCockBOpen = inf.ReadBoolean();
            AngleCockBOpenAmount = inf.ReadSingle();
            BleedOffValveOpen = inf.ReadBoolean();
            HoldingValve = (ValveState)inf.ReadInt32();
            UniformChargingActive = inf.ReadBoolean();
            UniformReleaseActive = inf.ReadBoolean();
            QuickServiceActive = inf.ReadBoolean();
            QuickReleaseActive = inf.ReadBoolean();
            TwoStageLowPressureActive = inf.ReadBoolean();
            LegacyEmergencyValve = inf.ReadBoolean();
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            BrakeLine1PressurePSI = Car.Train.EqualReservoirPressurePSIorInHg;
            BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
            BrakeLine3PressurePSI = 0;
            if (maxPressurePSI > 0)
                ControlResPressurePSI = maxPressurePSI;
            FullServPressurePSI = fullServPressurePSI;
            AutoCylPressurePSI = immediateRelease ? 0 : Math.Min((maxPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio, MaxCylPressurePSI);
            CylPressurePSI = AutoCylPressurePSI * RelayValveRatio;
            AuxResPressurePSI = Math.Max(TwoPipes ? maxPressurePSI : maxPressurePSI - AutoCylPressurePSI / AuxCylVolumeRatio, BrakeLine1PressurePSI);
            if ((Car as MSTSWagon).EmergencyReservoirPresent)
                EmergResPressurePSI = Math.Max(AuxResPressurePSI, maxPressurePSI);
            TripleValveState = AutoCylPressurePSI < 1 ? ValveState.Release : ValveState.Lap;
            HoldingValve = ValveState.Release;
            HandbrakePercent = handbrakeOn & (Car as MSTSWagon).HandBrakePresent ? 100 : 0;
            SetRetainer(RetainerSetting.Exhaust);
            if (Car is MSTSLocomotive loco) 
            {
                loco.MainResPressurePSI = loco.MaxMainResPressurePSI;
            }

            SmoothedBrakePipeChangePSIpS.ForceSmoothValue(0);
        }

        public override void Initialize()
        {
            // reducing size of Emergency Reservoir for short (fake) cars
            if (Car.Simulator.Settings.CorrectQuestionableBrakingParams && Car.CarLengthM <= 1)
                EmergResVolumeM3 = Math.Min (0.02f, EmergResVolumeM3);

            // Install a plain triple valve if no brake valve defined
            // Do not install it for tenders if not defined, to allow tenders with straight brake only
            if (Car.Simulator.Settings.CorrectQuestionableBrakingParams && (Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.None && (Car as MSTSWagon).WagonType != TrainCar.WagonTypes.Tender)
            {
                (Car as MSTSWagon).BrakeValve = MSTSWagon.BrakeValveType.TripleValve;
                Trace.TraceWarning("{0} does not define a brake valve, defaulting to a plain triple valve", (Car as MSTSWagon).WagFilePath);
            }

            // Reducing reservoir charging rates when set unrealistically high
            if (Car.Simulator.Settings.CorrectQuestionableBrakingParams && (MaxAuxilaryChargingRatePSIpS > 10 || EmergResChargingRatePSIpS > 10))
            {
                MaxAuxilaryChargingRatePSIpS = Math.Min(MaxAuxilaryChargingRatePSIpS, 10.0f);
                EmergResChargingRatePSIpS = Math.Min(EmergResChargingRatePSIpS, 10.0f);
            }

            // In simple brake mode set emergency reservoir volume, override high volume values to allow faster brake release.
            if (Car.Simulator.Settings.SimpleControlPhysics && EmergResVolumeM3 > 2.0)
                EmergResVolumeM3 = 0.7f;

            if (MaxTripleValveCylPressurePSI == 0) MaxTripleValveCylPressurePSI = MaxCylPressurePSI / RelayValveRatio;
            if (EngineRelayValveRatio == 0) EngineRelayValveRatio = RelayValveRatio;

            if (ServiceApplicationRatePSIpS == 0)
                ServiceApplicationRatePSIpS = MaxApplicationRatePSIpS;

            if ((Car as MSTSWagon).EmergencyReservoirPresent && EmergencyValveActuationRatePSIpS == 0)
            {
                EmergencyValveActuationRatePSIpS = 15;
                LegacyEmergencyValve = true;
            }

            if (InitialApplicationThresholdPSI == 0)
            {
                if ((Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.Distributor)
                    InitialApplicationThresholdPSI = 2.2f; // UIC spec: brakes should release if brake pipe is within 0.15 bar of control res
                else
                    InitialApplicationThresholdPSI = 1.0f;
            }

            if (TripleValveSensitivityPSI == 0)
            {
                if ((Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.Distributor)
                    TripleValveSensitivityPSI = 1.4f; // UIC spec: brakes should respond to 0.1 bar changes in brake pipe
                else
                    TripleValveSensitivityPSI = 1.0f;
            }

            if (EmergResVolumeM3 > 0 && EmergAuxVolumeRatio > 0 && BrakePipeVolumeM3 > 0)
            {
                AuxBrakeLineVolumeRatio = EmergResVolumeM3 / EmergAuxVolumeRatio / BrakePipeVolumeM3;
                EmergBrakeLineVolumeRatio = EmergResVolumeM3 / BrakePipeVolumeM3;
            }
            else
            {
                AuxBrakeLineVolumeRatio = 3.1f;
                EmergBrakeLineVolumeRatio = 4.34f;
            }
            CylBrakeLineVolumeRatio = AuxBrakeLineVolumeRatio / AuxCylVolumeRatio;

            if (CylVolumeM3 == 0) CylVolumeM3 = EmergResVolumeM3 / EmergAuxVolumeRatio / AuxCylVolumeRatio;
            
            RelayValveFitted |= (Car is MSTSLocomotive loco && (loco.DynamicBrakeAutoBailOff || loco.DynamicBrakePartialBailOff)) || (Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.DistributingValve;

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

        public void UpdateTripleValveState(float elapsedClockSeconds)
        {
            var prevState = TripleValveState;
            var valveType = (Car as MSTSWagon).BrakeValve;
            bool disableGradient = !(Car.Train.LeadLocomotive is MSTSLocomotive) && Car.Train.TrainType != Orts.Simulation.Physics.Train.TRAINTYPE.STATIC;
            // Legacy cars and static cars use a simpler check for emergency applications to ensure emergency applications occur despite simplified physics
            bool emergencyTripped = (Car.Train.TrainType == Orts.Simulation.Physics.Train.TRAINTYPE.STATIC || LegacyEmergencyValve) ?
                BrakeLine1PressurePSI <= 0.75f * EmergResPressurePSI * AuxCylVolumeRatio / (AuxCylVolumeRatio + 1) : Math.Max(-SmoothedBrakePipeChangePSIpS.SmoothedValue, 0) > EmergencyValveActuationRatePSIpS;

            if (valveType == MSTSWagon.BrakeValveType.Distributor)
            {
                float applicationPSI = ControlResPressurePSI - BrakeLine1PressurePSI;
                float targetPressurePSI = applicationPSI * AuxCylVolumeRatio;
                if (!disableGradient && EmergencyValveActuationRatePSIpS > 0 && emergencyTripped)
                {
                    if (prevState == ValveState.Release) // If valve transitions from release to emergency, quick service activates
                    {
                        QuickServiceActive = true;
                        UniformChargingActive = false;
                        UniformReleaseActive = false;
                        QuickReleaseActive = false;
                    }
                    TripleValveState = ValveState.Emergency;
                }
                else if (TripleValveState != ValveState.Emergency && targetPressurePSI > AutoCylPressurePSI + (TripleValveState == ValveState.Apply ? 0.0f : TripleValveSensitivityPSI * AuxCylVolumeRatio))
                {
                    if (prevState == ValveState.Release)
                    {
                        if (applicationPSI > InitialApplicationThresholdPSI) // If valve transitions from release to apply, quick service activates
                        {
                            QuickServiceActive = true;
                            UniformChargingActive = false;
                            UniformReleaseActive = false;
                            QuickReleaseActive = false;

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
                        QuickReleaseActive = true;
                        QuickServiceActive = false;
                    }
                    TripleValveState = ValveState.Release;
                }
                else if (TripleValveState != ValveState.Emergency)
                {
                    TripleValveState = ValveState.Lap;
                }    
            }
            else if (valveType == MSTSWagon.BrakeValveType.TripleValve || valveType == MSTSWagon.BrakeValveType.DistributingValve)
            {
                if (!disableGradient && EmergencyValveActuationRatePSIpS > 0 && emergencyTripped)
                {
                    if (prevState == ValveState.Release) // If valve transitions from release to emergency, quick service activates
                    {
                        QuickServiceActive = true;
                        UniformChargingActive = false;
                        UniformReleaseActive = false;
                        QuickReleaseActive = false;
                    }
                    TripleValveState = ValveState.Emergency;
                }
                else if (TripleValveState != ValveState.Emergency && BrakeLine1PressurePSI < AuxResPressurePSI - (TripleValveState == ValveState.Apply ? 0.0f : TripleValveSensitivityPSI))
                {
                    if (prevState == ValveState.Release)
                    {
                        if (BrakeLine1PressurePSI < AuxResPressurePSI - InitialApplicationThresholdPSI) // If valve transitions from release to apply, quick service activates
                        {
                            QuickServiceActive = true;
                            UniformChargingActive = false;
                            UniformReleaseActive = false;
                            QuickReleaseActive = false;

                            TripleValveState = ValveState.Apply;
                        }
                    }
                    else
                    {
                        TripleValveState = ValveState.Apply;
                    }
                }
                else if (BrakeLine1PressurePSI > AuxResPressurePSI + (TripleValveState == ValveState.Release ? 0.0f : TripleValveSensitivityPSI * 2))
                {
                    if (prevState != ValveState.Release) // If valve transitions to release, quick release activates, quick service deactivates
                    {
                        QuickReleaseActive = true;
                        QuickServiceActive = false;
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
                    AngleCockOpenAmount = (currentTime - ((float)AngleCockOpenTime + AngleCockOpeningTime)) / 5 + 0.3f;

                    if (AngleCockOpenAmount >= 1.0f)
                    {
                        AngleCockOpenAmount = 1.0f;
                        AngleCockOpenTime = null;
                    }
                }
                else
                {
                    // Gradually open anglecock toward 30% over 30 seconds
                    AngleCockOpenAmount = 0.3f * (currentTime - (float)AngleCockOpenTime) / AngleCockOpeningTime;
                }
            }
            else if (!AngleCockOpen && AngleCockOpenAmount > 0.0f)
            {
                AngleCockOpenAmount = 0.0f;
                AngleCockOpenTime = null;
            }
        }

        public override void Update(float elapsedClockSeconds)
        {
            var valveType = (Car as MSTSWagon).BrakeValve;

            // Two stage braking: higher brake force is allowed at higher speeds
            if (TripleValveState == ValveState.Emergency || (TwoStageLowPressureActive && Math.Abs(Car.SpeedMpS) > TwoStageSpeedUpMpS))
                TwoStageLowPressureActive = false;
            else if (!TwoStageLowPressureActive && Math.Abs(Car.SpeedMpS) < TwoStageSpeedDownMpS)
                TwoStageLowPressureActive = true;

            // Determine target brake cylinder feed pressure
            float threshold = 0; 
            if (TripleValveState == ValveState.Emergency)
            {
                threshold = MaxTripleValveCylPressurePSI; // Set pressure to max in emergency
            }
            else
            {
                if (valveType == MSTSWagon.BrakeValveType.Distributor)
                {
                    threshold = Math.Max((ControlResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio, 0);

                    if (threshold < InitialApplicationThresholdPSI * AuxCylVolumeRatio) // Prevent brakes getting stuck with a small amount of air on distributor systems
                        threshold = 0;
                    if (MRPAuxResCharging && HighSpeedReducingPressurePSI > 0 && threshold > HighSpeedReducingPressurePSI)
                        threshold = HighSpeedReducingPressurePSI; // Small workaround to improve compatibility between modern systems and HSRV (such systems shouldn't have an HSRV equipped)
                }
                else
                {
                    if (TripleValveState == ValveState.Release)
                        threshold = 0;
                    else
                        threshold = MaxTripleValveCylPressurePSI; // Set pressure limit to max for plain triple valves
                }
                if (TwoStageLowPressureActive && threshold > TwoStageLowPressurePSI)
                    threshold = TwoStageLowPressurePSI;
                else if (ServiceMaxCylPressurePSI > 0 && threshold > ServiceMaxCylPressurePSI)
                    threshold = ServiceMaxCylPressurePSI;
                else if (threshold > MaxTripleValveCylPressurePSI)
                    threshold = MaxTripleValveCylPressurePSI;

                // Account for retainers
                threshold = Math.Max(threshold, RetainerPressureThresholdPSI);
            }

            BrakePipeChangePSIpS = (BrakeLine1PressurePSI - prevBrakePipePressurePSI) / Math.Max(elapsedClockSeconds, 0.0001f);
            SmoothedBrakePipeChangePSIpS.Update(Math.Max(elapsedClockSeconds, 0.0001f), BrakePipeChangePSIpS);

            // Update anglecock opening. Anglecocks set to gradually open over 30 seconds, but close instantly.
            // Gradual opening prevents undesired emergency applications
            UpdateAngleCockState(AngleCockAOpen, ref AngleCockAOpenAmount, ref AngleCockAOpenTime);
            UpdateAngleCockState(AngleCockBOpen, ref AngleCockBOpenAmount, ref AngleCockBOpenTime);

            if (BleedOffValveOpen)
            {
                if (valveType == MSTSWagon.BrakeValveType.Distributor)
                {
                    ControlResPressurePSI = 0;
                    BleedOffValveOpen = false;
                }
                else
                {
                    if (AuxResPressurePSI < 0.01f && AutoCylPressurePSI < 0.01f && BrakeLine1PressurePSI < 0.01f && (EmergResPressurePSI < 0.01f || !(Car as MSTSWagon).EmergencyReservoirPresent))
                    {
                        BleedOffValveOpen = false;
                    }
                    else
                    {
                        AuxResPressurePSI -= elapsedClockSeconds * MaxApplicationRatePSIpS;
                        if (AuxResPressurePSI < 0)
                            AuxResPressurePSI = 0;
                        AutoCylPressurePSI -= elapsedClockSeconds * MaxReleaseRatePSIpS;
                        if (AutoCylPressurePSI < 0)
                            AutoCylPressurePSI = 0;
                        if ((Car as MSTSWagon).EmergencyReservoirPresent)
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
            if ((TripleValveState == ValveState.Apply || TripleValveState == ValveState.Emergency) && !Car.WheelBrakeSlideProtectionActive)
            {
                float dp = 0;
                float dpPipe = 0;
                if (QuickServiceActive) // Quick service: Brake pipe pressure is locally reduced to speed up initial reduction
                {
                    if (QuickServiceVentRatePSIpS > 0)
                    {
                        dpPipe = Math.Abs(elapsedClockSeconds * QuickServiceVentRatePSIpS);
                        if (AutoCylPressurePSI > QuickServiceLimitPSI * 0.75f) // Vent rate is reduced when quick service is nearly complete
                        {
                            dpPipe /= 3;
                        }
                    }
                    dp = elapsedClockSeconds * Math.Max(QuickServiceApplicationRatePSIpS, MaxApplicationRatePSIpS);
                }
                else
                {
                    if (AcceleratedApplicationFactor > 0) // Accelerated application: Air is vented from the brake pipe to speed up service applications
                    {
                        // Amount of air vented is proportional to pressure reduction from external sources
                        dpPipe = MathHelper.Clamp(-SmoothedBrakePipeChangePSIpS.SmoothedValue * AcceleratedApplicationFactor, 0, AcceleratedApplicationLimitPSIpS) * elapsedClockSeconds;
                    }
                    if (TripleValveState == ValveState.Emergency)
                        dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                    else
                        dp = elapsedClockSeconds * ServiceApplicationRatePSIpS;
                }
                if (BrakeLine1PressurePSI - dpPipe < 0)
                {
                    // Prevent pipe pressure from going negative, also reset quick service to prevent runaway condition
                    dpPipe = BrakeLine1PressurePSI;
                    QuickServiceActive = false;
                }

                if (TripleValveState != ValveState.Emergency && BrakeLine1PressurePSI < AuxResPressurePSI + 1)
                    dp *= MathHelper.Clamp(AuxResPressurePSI - BrakeLine1PressurePSI, 0.1f, 1.0f); // Reduce application rate if nearing equalization to prevent rapid toggling between apply and lap
                else if ((valveType == MSTSWagon.BrakeValveType.Distributor) && AutoCylPressurePSI > threshold - 1)
                    dp *= MathHelper.Clamp(threshold - AutoCylPressurePSI, 0.1f, 1.0f); // Reduce application rate if nearing target pressure
                if (AuxResPressurePSI - dp / AuxCylVolumeRatio < AutoCylPressurePSI + dp)
                    dp = (AuxResPressurePSI - AutoCylPressurePSI) * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
                if (AutoCylPressurePSI + dp > threshold)
                    dp = threshold - AutoCylPressurePSI;
                if (BrakeLine1PressurePSI > AuxResPressurePSI - dp / AuxCylVolumeRatio && !BleedOffValveOpen)
                    dp = (AuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                if (dp < 0)
                    dp = 0;

                AuxResPressurePSI -= dp / AuxCylVolumeRatio;
                AutoCylPressurePSI += dp;
                BrakeLine1PressurePSI -= dpPipe;

                // Reset quick service if brake cylinder is above limiting valve setting
                // Also reset quick service if cylinders manage to equalize to prevent runaway condition
                if (QuickServiceActive && (AutoCylPressurePSI > QuickServiceLimitPSI || AutoCylPressurePSI >= AuxResPressurePSI)) 
                    QuickServiceActive = false;

                if (TripleValveState == ValveState.Emergency)
                {
                    if ((Car as MSTSWagon).EmergencyReservoirPresent)
                    {
                        if (EmergencyDumpValveTimerS != 0 && EmergencyDumpStartTime == null && BrakeLine1PressurePSI > AcceleratedEmergencyReleaseThresholdPSI)
                        {
                            // Accelerated emergency release: Aux res and BC air are routed into the brake pipe once the emergency application is complete, speeds up emergency release
                            // Triggers at 20 psi brake pipe pressure

                            dp = elapsedClockSeconds * MaxReleaseRatePSIpS;
                            if (AutoCylPressurePSI - dp < AuxResPressurePSI + dp / AuxCylVolumeRatio)
                                dp = Math.Max((AutoCylPressurePSI - AuxResPressurePSI) * (AuxCylVolumeRatio / (1 + AuxCylVolumeRatio)), 0);
                            AutoCylPressurePSI -= dp;
                            AuxResPressurePSI += dp / AuxCylVolumeRatio;

                            dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
                            if (AuxResPressurePSI - dp < BrakeLine1PressurePSI + dp * AuxBrakeLineVolumeRatio)
                                dp = Math.Max((AuxResPressurePSI - BrakeLine1PressurePSI) / (1 + AuxBrakeLineVolumeRatio), 0);
                            AuxResPressurePSI -= dp;
                            BrakeLine1PressurePSI += dp * AuxBrakeLineVolumeRatio;
                        }
                        else
                        {
                            dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                            if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                                dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                            EmergResPressurePSI -= dp;
                            AuxResPressurePSI += dp * EmergAuxVolumeRatio;
                        }
                    }

                    if (EmergencyDumpValveTimerS == 0)
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
                        if (BrakeLine1PressurePSI - dp * CylBrakeLineVolumeRatio < AutoCylPressurePSI + dp)
                            dp = (BrakeLine1PressurePSI - AutoCylPressurePSI) / (1 + CylBrakeLineVolumeRatio);
                        if (dp < 0)
                            dp = 0;
                        AutoCylPressurePSI += dp;
                        BrakeLine1PressurePSI -= dp * CylBrakeLineVolumeRatio;
                    }
                }
            }

            // triple valve set to release pressure in brake cylinder and EP valve set
            if (TripleValveState == ValveState.Release && valveType != MSTSWagon.BrakeValveType.None)
            {
                if (valveType == MSTSWagon.BrakeValveType.Distributor)
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

            // triple valve set to hold current pressure in brake cylinder
            if (TripleValveState == ValveState.Lap && valveType != MSTSWagon.BrakeValveType.None)
            {
                // Mitigation for brake cylinder leaks
                // TODO: Actually implement air system leaks
                if (AutoCylPressurePSI < QuickServiceLimitPSI) // Basic cylinder leak prevention, let air enter cylinder from brake pipe if pressure drops below the quick service limiting valve
                {
                    float dp = elapsedClockSeconds * ServiceApplicationRatePSIpS;

                    if (AutoCylPressurePSI > BrakeLine1PressurePSI - 1)
                        dp *= MathHelper.Clamp(BrakeLine1PressurePSI - AutoCylPressurePSI, 0.1f, 1.0f);
                    if (AutoCylPressurePSI + dp > QuickServiceLimitPSI)
                        dp = QuickServiceLimitPSI - AutoCylPressurePSI;
                    if (BrakeLine1PressurePSI - dp * CylBrakeLineVolumeRatio < AutoCylPressurePSI + dp)
                        dp = (BrakeLine1PressurePSI - AutoCylPressurePSI) / (1 + CylBrakeLineVolumeRatio);
                    AutoCylPressurePSI += dp;
                    BrakeLine1PressurePSI -= dp * CylBrakeLineVolumeRatio;
                }
            }

            // Handle brake release: reduce cylinder pressure if all triple valve, EP holding valve and retainers allow so
            if (TripleValveState == ValveState.Release && HoldingValve == ValveState.Release && AutoCylPressurePSI > threshold)
            {
                float dp = elapsedClockSeconds * ReleaseRatePSIpS;
                if (UniformReleaseRatio > 0) // Uniform release: Brake release is slowed down when the brake pipe is substantially higher than the aux res
                {
                    if (!UniformReleaseActive && AuxResPressurePSI < BrakeLine1PressurePSI - UniformReleaseThresholdPSI)
                        UniformReleaseActive = true;
                    else if (UniformReleaseActive && AuxResPressurePSI > BrakeLine1PressurePSI - UniformReleaseThresholdPSI / 2)
                        UniformReleaseActive = false;
                    if (UniformReleaseActive)
                        dp /= UniformReleaseRatio;
                }
                if (threshold > 0 && AutoCylPressurePSI < threshold + 1)
                    dp *= MathHelper.Clamp(AutoCylPressurePSI - threshold, 0.1f, 1.0f); // Reduce release rate if nearing target pressure to prevent toggling between release and lap
                if (AutoCylPressurePSI - dp < threshold)
                    dp = AutoCylPressurePSI - threshold;
                if (dp < 0)
                    dp = 0;
                AutoCylPressurePSI -= dp;
            }
            // Special cases for equipment which bypasses triple valve
            else if (TwoStageLowPressureActive && AutoCylPressurePSI > TwoStageLowPressurePSI) // Two stage braking
            {
                float dp = elapsedClockSeconds * ReleaseRatePSIpS;
                if (AutoCylPressurePSI - dp < TwoStageLowPressurePSI)
                    dp = AutoCylPressurePSI - TwoStageLowPressurePSI;
                AutoCylPressurePSI -= dp;
            }
            if (HighSpeedReducingPressurePSI > 0 && AutoCylPressurePSI > HighSpeedReducingPressurePSI) // High speed reducing valve
            {
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS / 2.0f; // This release rate should allow only emergency applications to overcome HSRV
                dp *= MathHelper.Clamp(1.0f - (AutoCylPressurePSI - HighSpeedReducingPressurePSI) / 5.0f, 0.1f, 1.0f); // Rate of release reduces as pressure difference increases
                if (AutoCylPressurePSI - dp < HighSpeedReducingPressurePSI)
                    dp = AutoCylPressurePSI - HighSpeedReducingPressurePSI;
                AutoCylPressurePSI -= dp;
            }

            // Manage emergency res charging
            if ((Car as MSTSWagon).EmergencyReservoirPresent)
            {
                if (TripleValveState == ValveState.Release && EmergResPressurePSI > BrakeLine1PressurePSI)
                {
                    if (EmergResQuickRelease && QuickReleaseActive) // Quick release: Emergency res charges brake pipe during release
                    {
                        float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
                        if (EmergResPressurePSI - dp < BrakeLine1PressurePSI + dp * EmergBrakeLineVolumeRatio)
                            dp = (EmergResPressurePSI - BrakeLine1PressurePSI) / (1 + EmergBrakeLineVolumeRatio);
                        EmergResPressurePSI -= dp;
                        BrakeLine1PressurePSI += dp * EmergBrakeLineVolumeRatio;
                    }
                    else if (!EmergResQuickRelease) // Quick recharge: Emergency res air used to recharge aux res on older control valves
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
                    if (AuxResPressurePSI >= EmergResPressurePSI)
                    {
                        QuickReleaseActive = false;
                    }
                }
                if (AuxResPressurePSI > EmergResPressurePSI && (valveType == MSTSWagon.BrakeValveType.Distributor || TripleValveState == ValveState.Release))
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

            if (TwoPipes && MRPAuxResCharging && valveType == MSTSWagon.BrakeValveType.Distributor && BrakeLine2PressurePSI > BrakeLine1PressurePSI) // Charge from main res pipe
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
                if (AuxResPressurePSI < BrakeLine1PressurePSI && (valveType == MSTSWagon.BrakeValveType.Distributor || TripleValveState == ValveState.Release) && !BleedOffValveOpen)
                {
                    if (AuxResPressurePSI > BrakeLine1PressurePSI - 1)
                        dpAux *= MathHelper.Clamp(BrakeLine1PressurePSI - AuxResPressurePSI, 0.1f, 1.0f); // Reduce recharge rate if nearing target pressure to smooth out changes in brake pipe
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
            }

            if (AutoCylPressurePSI < 0)
                AutoCylPressurePSI = 0;
            
            float demandedPressurePSI = 0;
            var loco = Car as MSTSLocomotive;
            if (loco != null && (Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.DistributingValve)
            {
                // For distributing valves, we use AutoCylPressurePSI as "Application Chamber/Pipe" pressure
                // CylPressurePSI is the actual pressure applied to cylinders
                var engineBrakeStatus = loco.EngineBrakeController.Notches[loco.EngineBrakeController.CurrentNotch].Type;
                var trainBrakeStatus = loco.TrainBrakeController.Notches[loco.TrainBrakeController.CurrentNotch].Type;
                 // BailOff
                if (engineBrakeStatus == ControllerState.BailOff)
                {
                    AutoCylPressurePSI -= Math.Max(MaxReleaseRatePSIpS, loco.EngineBrakeReleaseRatePSIpS) * elapsedClockSeconds;
                    if (AutoCylPressurePSI < 0) AutoCylPressurePSI = 0;
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
                            var lead = loco.Train.Cars[loco.Train.LeadLocomotiveIndex] as MSTSLocomotive;
                            if (lead != null && (lead.BailOff || 
                                (lead.EngineBrakeController != null && lead.EngineBrakeController.CurrentNotch >= 0 && lead.EngineBrakeController.Notches[lead.EngineBrakeController.CurrentNotch].Type == ControllerState.BailOff)))
                            {
                                if (loco.BrakeValve == MSTSWagon.BrakeValveType.Distributor)
                                {
                                    ControlResPressurePSI = 0;
                                }
                                else
                                {
                                    AutoCylPressurePSI -= Math.Max(MaxReleaseRatePSIpS, loco.EngineBrakeReleaseRatePSIpS) * elapsedClockSeconds;
                                    if (AutoCylPressurePSI < 0)
                                        AutoCylPressurePSI = 0;
                                }
                            }
                        }
                        if (loco.DynamicBrakePercent > 0 && Car.FrictionBrakeBlendingMaxForceN > 0)
                        {
                            if (loco.DynamicBrakePartialBailOff)
                            {
                                var requiredBrakeForceN = Math.Min(AutoCylPressurePSI / MaxCylPressurePSI, 1) * Car.FrictionBrakeBlendingMaxForceN;
                                var localBrakeForceN = loco.DynamicBrakeForceN + Math.Min(CylPressurePSI / MaxCylPressurePSI, 1) * Car.FrictionBrakeBlendingMaxForceN;
                                if (localBrakeForceN > requiredBrakeForceN - 0.15f * Car.FrictionBrakeBlendingMaxForceN)
                                {
                                    demandedPressurePSI = Math.Min(Math.Max((requiredBrakeForceN - loco.DynamicBrakeForceN)/Car.FrictionBrakeBlendingMaxForceN * MaxCylPressurePSI, 0), MaxCylPressurePSI);
                                    if (demandedPressurePSI > CylPressurePSI && demandedPressurePSI < CylPressurePSI + 4) // Allow some margin for unnecessary air brake application
                                    {
                                        demandedPressurePSI = CylPressurePSI;
                                    }
                                    demandedPressurePSI /= RelayValveRatio;
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
                                    var dynforce = loco.DynamicBrakeForceCurves.Get(1.0f, loco.AbsSpeedMpS);
                                    if ((loco.MaxDynamicBrakeForceN == 0 && dynforce > 0) || dynforce > loco.MaxDynamicBrakeForceN * 0.6)
                                    {
                                        demandedPressurePSI = 0;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (RelayValveFitted)
            {
                demandedPressurePSI = Math.Max(RelayValveRatio * demandedPressurePSI, EngineRelayValveRatio * BrakeLine3PressurePSI);
                if (demandedPressurePSI > CylPressurePSI)
                {
                    float dp = elapsedClockSeconds * RelayValveApplicationRatePSIpS;
                    if (dp > demandedPressurePSI - CylPressurePSI)
                        dp = demandedPressurePSI - CylPressurePSI;
                    if (MaxCylPressurePSI < CylPressurePSI + dp)
                        dp = MaxCylPressurePSI - CylPressurePSI;
                    
                    // TODO: Implement a brake reservoir which keeps some air available in case of main reservoir leakage
                    // Currently we drain from the main reservoir directly
                    if (loco != null)
                    {
                        float volumeRatio = CylVolumeM3 / loco.MainResVolumeM3;
                        if (loco.MainResPressurePSI - dp * volumeRatio < CylPressurePSI + dp)
                            dp = (loco.MainResPressurePSI - CylPressurePSI) / (1 + volumeRatio);
                        loco.MainResPressurePSI -= dp * volumeRatio;
                    }
                    else if (TwoPipes)
                    {
                        if (BrakeLine2PressurePSI - dp * CylVolumeM3 / BrakePipeVolumeM3 < CylPressurePSI + dp)
                            dp = (BrakeLine2PressurePSI - CylPressurePSI) / (1 + CylVolumeM3 / BrakePipeVolumeM3);
                        BrakeLine2PressurePSI -= dp * CylVolumeM3 / BrakePipeVolumeM3;
                    }
                    CylPressurePSI += dp;
                }
                else if (demandedPressurePSI < CylPressurePSI)
                {
                    CylPressurePSI = Math.Max(Math.Max(demandedPressurePSI, CylPressurePSI - elapsedClockSeconds * RelayValveReleaseRatePSIpS), 0);
                }
            }
            else
            {
                CylPressurePSI = Math.Max(demandedPressurePSI, BrakeLine3PressurePSI);
            }

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
                    AutoCylPressurePSI -= elapsedClockSeconds * MaxReleaseRatePSIpS;
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
                Car.BrakeShoeForceN = Car.MaxBrakeForceN * MathHelper.Clamp((CylPressurePSI - BrakeCylinderSpringPressurePSI) / (MaxCylPressurePSI - BrakeCylinderSpringPressurePSI), 0, 1);
                if (Car.BrakeShoeForceN < Car.MaxHandbrakeForceN * HandbrakePercent / 100)
                    Car.BrakeShoeForceN = Car.MaxHandbrakeForceN * HandbrakePercent / 100;
            }
            else Car.BrakeShoeForceN = Math.Max(Car.MaxBrakeForceN, Car.MaxHandbrakeForceN / 2);

            float brakeShoeFriction = Car.GetBrakeShoeFrictionFactor();
            Car.HuDBrakeShoeFriction = Car.GetBrakeShoeFrictionCoefficientHuD();

            Car.BrakeRetardForceN = Car.BrakeShoeForceN * brakeShoeFriction; // calculates value of force applied to wheel, independent of wheel skid
            if (Car.BrakeSkid) // Test to see if wheels are skiding to excessive brake force
            {
                Car.BrakeForceN = Car.BrakeShoeForceN * Car.SkidFriction;   // if excessive brakeforce, wheel skids, and loses adhesion
            }
            else
            {
                Car.BrakeForceN = Car.BrakeShoeForceN * brakeShoeFriction; // In advanced adhesion model brake shoe coefficient varies with speed, in simple model constant force applied as per value in WAG file, will vary with wheel skid.
            }

            // sound trigger checking runs every half second, to avoid the problems caused by the jumping BrakeLine1PressurePSI value, and also saves cpu time :)
            if (SoundTriggerCounter >= 0.5f)
            {
                SoundTriggerCounter = 0f;
                if ( Math.Abs(AutoCylPressurePSI - prevCylPressurePSI) > 0.1f) //(AutoCylPressurePSI != prevCylPressurePSI)
                {
                    if (!TrainBrakePressureChanging)
                    {
                        if (AutoCylPressurePSI > prevCylPressurePSI)
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

                if ( Math.Abs(BrakeLine1PressurePSI - prevBrakePipePressurePSI_sound) > 0.1f /*BrakeLine1PressurePSI > prevBrakePipePressurePSI*/)
                {
                    if (!BrakePipePressureChanging)
                    {
                        if (BrakeLine1PressurePSI > prevBrakePipePressurePSI_sound)
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
                prevCylPressurePSI = AutoCylPressurePSI;
                prevBrakePipePressurePSI_sound = BrakeLine1PressurePSI;

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
            prevBrakePipePressurePSI = BrakeLine1PressurePSI;
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
            train.FindLeadLocomotives(out int first, out int last);

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
                var brakePipeTimeFactorS = lead == null ? 0.0015f : lead.BrakePipeTimeFactorS;
                int nSteps = (int)(elapsedClockSeconds / brakePipeTimeFactorS + 1);
                float trainPipeTimeVariationS = elapsedClockSeconds / nSteps;
                float trainPipeLeakLossPSI = lead == null ? 0.0f : (trainPipeTimeVariationS * lead.TrainBrakePipeLeakPSIorInHgpS);
                float serviceTimeFactor = lead != null && lead.TrainBrakeController != null ? lead.BrakeServiceTimeFactorPSIpS : 0.001f;
                float emergencyTimeFactor = lead != null && lead.TrainBrakeController != null ? lead.BrakeEmergencyTimeFactorPSIpS : 0.001f;
                for (int i = 0; i < nSteps; i++)
                {
                    if (lead != null)
                    {
                        tempBrakePipeFlow = 0.0f;

                        // Allow for leaking train air brakepipe
                        if (lead.BrakeSystem.BrakeLine1PressurePSI - trainPipeLeakLossPSI > 0 && lead.TrainBrakePipeLeakPSIorInHgpS != 0) // if train brake pipe has pressure in it, ensure result will not be negative if loss is subtracted
                        {
                            lead.BrakeSystem.BrakeLine1PressurePSI -= trainPipeLeakLossPSI;
                        }

                        // Emergency brake - vent brake pipe to 0 psi regardless of equalizing res pressure
                        if (lead.TrainBrakeController.EmergencyBraking)
                        {
                            float emergencyVariationFactor = Math.Min(trainPipeTimeVariationS / emergencyTimeFactor, 0.95f);
                            float pressureDiffPSI = emergencyVariationFactor * lead.BrakeSystem.BrakeLine1PressurePSI;

                            if (lead.BrakeSystem.BrakeLine1PressurePSI - pressureDiffPSI < 0)
                                pressureDiffPSI = lead.BrakeSystem.BrakeLine1PressurePSI;
                            lead.BrakeSystem.BrakeLine1PressurePSI -= pressureDiffPSI;
                        }
                        else if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Neutral)
                        {
                            // Charge train brake pipe - adjust main reservoir pressure, and lead brake pressure line to maintain brake pipe equal to equalising resevoir pressure - release brakes
                            if (lead.BrakeSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg)
                            {
                                // Calculate change in brake pipe pressure between equalising reservoir and lead brake pipe
                                float chargingRatePSIpS = lead.BrakePipeChargingRatePSIorInHgpS;
                                if (lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.FullQuickRelease || lead.TrainBrakeController.TrainBrakeControllerState == ControllerState.Overcharge)
                                {
                                    chargingRatePSIpS = lead.BrakePipeQuickChargingRatePSIpS;
                                }
                                float PressureDiffEqualToPipePSI = trainPipeTimeVariationS * chargingRatePSIpS; // default condition - if EQ Res is higher then Brake Pipe Pressure

                                if (train.EqualReservoirPressurePSIorInHg - lead.BrakeSystem.BrakeLine1PressurePSI < 5.0f) // Reduce recharge rate if near EQ to simulate feed valve behavior
                                    PressureDiffEqualToPipePSI *= Math.Min((float)Math.Sqrt((train.EqualReservoirPressurePSIorInHg - lead.BrakeSystem.BrakeLine1PressurePSI) / 5.0f), 1.0f);
                                if (lead.MainResPressurePSI - train.EqualReservoirPressurePSIorInHg < 15.0f) // Reduce recharge rate if near MR pressure as per reality
                                    PressureDiffEqualToPipePSI *= Math.Min((lead.MainResPressurePSI - train.EqualReservoirPressurePSIorInHg) / 15.0f, 1.0f);

                                if (lead.BrakeSystem.BrakeLine1PressurePSI + PressureDiffEqualToPipePSI > train.EqualReservoirPressurePSIorInHg)
                                    PressureDiffEqualToPipePSI = train.EqualReservoirPressurePSIorInHg - lead.BrakeSystem.BrakeLine1PressurePSI;

                                if (lead.BrakeSystem.BrakeLine1PressurePSI + PressureDiffEqualToPipePSI > lead.MainResPressurePSI)
                                    PressureDiffEqualToPipePSI = lead.MainResPressurePSI - lead.BrakeSystem.BrakeLine1PressurePSI;

                                if (PressureDiffEqualToPipePSI < 0)
                                    PressureDiffEqualToPipePSI = 0;

                                // Adjust brake pipe pressure based upon pressure differential
                                if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Lap) // in LAP psoition brake pipe is isolated, and thus brake pipe pressure decreases, but reservoir remains at same pressure
                                {
                                    lead.BrakeSystem.BrakeLine1PressurePSI += PressureDiffEqualToPipePSI;
                                    lead.MainResPressurePSI -= PressureDiffEqualToPipePSI * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3;

                                    tempBrakePipeFlow = (PressureDiffEqualToPipePSI * lead.BrakeSystem.BrakePipeVolumeM3) / (OneAtmospherePSI * trainPipeTimeVariationS); // Instantaneous flow rate from MR to BP
                                }
                            }
                            // reduce pressure in lead brake line if brake pipe pressure is above equalising pressure - apply brakes
                            else if (lead.BrakeSystem.BrakeLine1PressurePSI > train.EqualReservoirPressurePSIorInHg)
                            {
                                float serviceVariationFactor = Math.Min(trainPipeTimeVariationS / serviceTimeFactor, 0.95f);
                                float pressureDiffPSI = serviceVariationFactor * lead.BrakeSystem.BrakeLine1PressurePSI;

                                if (train.EqualReservoirPressurePSIorInHg > lead.BrakeSystem.BrakeLine1PressurePSI - 5.0f) // Reduce exhausting rate if near EQ pressure to simulate feed valve
                                    pressureDiffPSI *= Math.Min((float)Math.Sqrt((lead.BrakeSystem.BrakeLine1PressurePSI - train.EqualReservoirPressurePSIorInHg) / 5.0f), 1.0f);
                                if (lead.BrakeSystem.BrakeLine1PressurePSI - pressureDiffPSI < train.EqualReservoirPressurePSIorInHg)
                                    pressureDiffPSI = lead.BrakeSystem.BrakeLine1PressurePSI - train.EqualReservoirPressurePSIorInHg;
                                lead.BrakeSystem.BrakeLine1PressurePSI -= pressureDiffPSI;
                            }
                        }

                        // Finish updating air flow meter
                        lead.BrakePipeFlowM3pS = tempBrakePipeFlow;
                        lead.FilteredBrakePipeFlowM3pS = lead.AFMFilter.Filter(lead.BrakePipeFlowM3pS, trainPipeTimeVariationS); // Actual flow rate displayed by air flow meter

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
                                {
                                    trainPipePressureDiffPropagationPSI *= MathHelper.Min((float)Math.Pow(car.BrakeSystem.AngleCockAOpenAmount * prevCar.BrakeSystem.AngleCockBOpenAmount, 2), 1.0f);

                                }

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
            if (first != -1 && last != -1)
            {
                float sumv = 0;
                float sumpv = 0;
                for (int i = first; i <= last; i++)
                {
                    if (train.Cars[i] is MSTSLocomotive loco)
                    {
                        sumv += loco.MainResVolumeM3;
                        sumpv += loco.MainResVolumeM3 * loco.MainResPressurePSI;
                    }
                }
                float totalReservoirPressurePSI = sumpv / sumv;
                for (int i = first; i <= last; i++)
                {
                    if (train.Cars[i] is MSTSLocomotive loco)
                    {
                        loco.MainResPressurePSI = totalReservoirPressurePSI;
                    }
                }
            }
            foreach (TrainCar car in train.Cars)
            {
                if (car is MSTSLocomotive loco)
                {
                    // Continue updating flowmeter on non-lead locomotives so it zeroes out eventually
                    if (car != lead)
                    {
                        (car as MSTSLocomotive).BrakePipeFlowM3pS = 0;
                        (car as MSTSLocomotive).FilteredBrakePipeFlowM3pS = (car as MSTSLocomotive).AFMFilter.Filter(0, elapsedClockSeconds);
                    }    

                    // Equalize main reservoir with MR pipe for every locomotive
                    if (car.BrakeSystem.TwoPipes)
                    {
                        float volumeRatio = loco.BrakeSystem.BrakePipeVolumeM3 / loco.MainResVolumeM3;
                        float dp = Math.Min((loco.MainResPressurePSI - loco.BrakeSystem.BrakeLine2PressurePSI) / (1 + volumeRatio), loco.MaximumMainReservoirPipePressurePSI - loco.BrakeSystem.BrakeLine2PressurePSI);
                        loco.MainResPressurePSI -= dp * volumeRatio;
                        loco.BrakeSystem.BrakeLine2PressurePSI += dp;
                        if (loco.MainResPressurePSI < 0) loco.MainResPressurePSI = 0;
                        if (loco.BrakeSystem.BrakeLine2PressurePSI < 0) loco.BrakeSystem.BrakeLine2PressurePSI = 0;
                    }
                }
            }

            // Propagate engine brake pipe (3) data
            for (int i = 0; i < train.Cars.Count; i++)
            {
                BrakeSystem brakeSystem = train.Cars[i].BrakeSystem;
                // Collect and propagate engine brake pipe (3) data
                // This appears to be calculating the engine brake cylinder pressure???
                if (i < first || i > last)
                {
                    brakeSystem.BrakeLine3PressurePSI = 0;
                }
                else
                {
                    if (lead != null)
                    {
                        float p = brakeSystem.BrakeLine3PressurePSI;
                        if (p > 1000)
                            p -= 1000;
                        var prevState = lead.EngineBrakeState;
                        if (p < train.BrakeLine3PressurePSI && p < lead.MainResPressurePSI )  // Apply the engine brake as the pressure decreases
                        {
                            float dp = elapsedClockSeconds * lead.EngineBrakeApplyRatePSIpS / (last - first + 1);
                            if (p + dp > train.BrakeLine3PressurePSI)
                                dp = train.BrakeLine3PressurePSI - p;
                            if (train.Cars[i] is MSTSLocomotive loco) // If this is a locomotive, drain air from main reservoir
                            {
                                float volumeRatio = brakeSystem.GetCylVolumeM3() / loco.MainResVolumeM3;
                                if (loco.MainResPressurePSI - dp * volumeRatio < p + dp)
                                {
                                    dp = (loco.MainResPressurePSI - p) / (1 + volumeRatio);
                                }
                                if (dp < 0) dp = 0;
                                loco.MainResPressurePSI -= dp * volumeRatio;
                            }
                            else // Otherwise, drain from locomotive engine brake pipe
                            {
                                if (lead.BrakeSystem.BrakeLine3PressurePSI - dp < p + dp)
                                {
                                    dp = (lead.BrakeSystem.BrakeLine3PressurePSI - p) / 2;
                                }
                                if (dp < 0) dp = 0;
                                lead.BrakeSystem.BrakeLine3PressurePSI -= dp;
                            }
                            p += dp;
                            lead.EngineBrakeState = ValveState.Apply;
                        }
                        else if (p > train.BrakeLine3PressurePSI)  // Release the engine brake as the pressure increases in the brake cylinder
                        {
                            float dp = elapsedClockSeconds * lead.EngineBrakeReleaseRatePSIpS / (last - first + 1);
                            if (p - dp < train.BrakeLine3PressurePSI)
                                dp = p - train.BrakeLine3PressurePSI;
                            p -= dp;
                            lead.EngineBrakeState = ValveState.Release;
                        }
                        else  // Engine brake does not change
                            lead.EngineBrakeState = ValveState.Lap;
                        if (lead.EngineBrakeState != prevState)
                            switch (lead.EngineBrakeState)
                            {
                                case ValveState.Release: lead.SignalEvent(Event.EngineBrakePressureIncrease); break;
                                case ValveState.Apply: lead.SignalEvent(Event.EngineBrakePressureDecrease); break;
                                case ValveState.Lap: lead.SignalEvent(Event.EngineBrakePressureStoppedChanging); break;
                            }
                        brakeSystem.BrakeLine3PressurePSI = p;
                    }
                }
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
                    if ((Car as MSTSWagon).RetainerPositions > 0)
                    {
                        RetainerPressureThresholdPSI = 20;
                        ReleaseRatePSIpS = (50 - 20) / 90f;
                        RetainerDebugState = "HP";
                    }
                    break;
                case RetainerSetting.LowPressure:
                    if ((Car as MSTSWagon).RetainerPositions > 3)
                    {
                        RetainerPressureThresholdPSI = 10;
                        ReleaseRatePSIpS = (50 - 10) / 60f;
                        RetainerDebugState = "LP";
                    }
                    else if ((Car as MSTSWagon).RetainerPositions > 0)
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
            if (!(Car as MSTSWagon).HandBrakePresent)
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
            if (AutoCylPressurePSI > MaxCylPressurePSI * 0.3)
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
