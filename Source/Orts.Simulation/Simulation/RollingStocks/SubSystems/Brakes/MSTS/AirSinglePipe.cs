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
        protected float HandbrakePercent;
        protected float CylPressurePSI = 64;
        public float AutoCylPressurePSI { get; protected set; } = 64;
        protected float AuxResPressurePSI = 64;
        protected float EmergResPressurePSI = 64;
        protected float ControlResPressurePSI = 64;
        protected float FullServPressurePSI = 50;
        protected float MaxCylPressurePSI = 64;
        protected float AuxCylVolumeRatio = 2.5f;
        protected float AuxBrakeLineVolumeRatio;
        protected float EmergResVolumeM3 = 0.07f;
        protected float RetainerPressureThresholdPSI;
        protected float ReleaseRatePSIpS = 1.86f;
        protected float MaxReleaseRatePSIpS = 1.86f;
        protected float MaxApplicationRatePSIpS = .9f;
        protected float MaxAuxilaryChargingRatePSIpS = 1.684f;
        protected float BrakeInsensitivityPSIpS = 0.07f;
        protected float EmergencyValveActuationRatePSIpS = 0;
        protected float EmergencyDumpValveRatePSIpS = 0;
        protected float EmergencyDumpValveTimerS = 120;
        protected float? EmergencyDumpStartTime;
        protected float EmergResChargingRatePSIpS = 1.684f;
        protected float EmergAuxVolumeRatio = 1.4f;
        protected string DebugType = string.Empty;
        protected string RetainerDebugState = string.Empty;
        protected bool MRPAuxResCharging;
        protected float CylVolumeM3;


        protected bool TrainBrakePressureChanging = false;
        protected bool BrakePipePressureChanging = false;
        protected float SoundTriggerCounter = 0;
        protected float prevCylPressurePSI = 0f;
        protected float prevBrakePipePressurePSI = 0f;
        protected float prevBrakePipePressurePSI_sound = 0f;


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
            EmergResVolumeM3 = thiscopy.EmergResVolumeM3;
            BrakePipeVolumeM3 = thiscopy.BrakePipeVolumeM3;
            RetainerPressureThresholdPSI = thiscopy.RetainerPressureThresholdPSI;
            ReleaseRatePSIpS = thiscopy.ReleaseRatePSIpS;
            MaxReleaseRatePSIpS = thiscopy.MaxReleaseRatePSIpS;
            MaxApplicationRatePSIpS = thiscopy.MaxApplicationRatePSIpS;
            MaxAuxilaryChargingRatePSIpS = thiscopy.MaxAuxilaryChargingRatePSIpS;
            BrakeInsensitivityPSIpS = thiscopy.BrakeInsensitivityPSIpS;
            EmergencyValveActuationRatePSIpS = thiscopy.EmergencyValveActuationRatePSIpS;
            EmergencyDumpValveRatePSIpS = thiscopy.EmergencyDumpValveRatePSIpS;
            EmergencyDumpValveTimerS = thiscopy.EmergencyDumpValveTimerS;
            EmergResChargingRatePSIpS = thiscopy.EmergResChargingRatePSIpS;
            EmergAuxVolumeRatio = thiscopy.EmergAuxVolumeRatio;
            TwoPipes = thiscopy.TwoPipes;
            MRPAuxResCharging = thiscopy.MRPAuxResCharging;
            HoldingValve = thiscopy.HoldingValve;
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
            var s = $" {Simulator.Catalog.GetString("EQ")} {FormatStrings.FormatPressure(Car.Train.EqualReservoirPressurePSIorInHg, PressureUnit.PSI, units[BrakeSystemComponent.EqualizingReservoir], true)}"
                + $" {Simulator.Catalog.GetString("BC")} {FormatStrings.FormatPressure(Car.Train.HUDWagonBrakeCylinderPSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakeCylinder], true)}"
                + $" {Simulator.Catalog.GetString("BP")} {FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, units[BrakeSystemComponent.BrakePipe], true)}";
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
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
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
                case "wagon(ortsmainrespipeauxrescharging": MRPAuxResCharging = this is AirTwinPipe && stf.ReadBoolBlock(true); break;
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
            outf.Write(AutoCylPressurePSI);
            outf.Write(AuxResPressurePSI);
            outf.Write(EmergResPressurePSI);
            outf.Write(ControlResPressurePSI);
            outf.Write(FullServPressurePSI);
            outf.Write((int)TripleValveState);
            outf.Write(FrontBrakeHoseConnected);
            outf.Write(AngleCockAOpen);
            outf.Write(AngleCockBOpen);
            outf.Write(BleedOffValveOpen);
            outf.Write((int)HoldingValve);
            outf.Write(CylVolumeM3);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            HandbrakePercent = inf.ReadSingle();
            ReleaseRatePSIpS = inf.ReadSingle();
            RetainerPressureThresholdPSI = inf.ReadSingle();
            AutoCylPressurePSI = inf.ReadSingle();
            AuxResPressurePSI = inf.ReadSingle();
            EmergResPressurePSI = inf.ReadSingle();
            ControlResPressurePSI = inf.ReadSingle();
            FullServPressurePSI = inf.ReadSingle();
            TripleValveState = (ValveState)inf.ReadInt32();
            FrontBrakeHoseConnected = inf.ReadBoolean();
            AngleCockAOpen = inf.ReadBoolean();
            AngleCockBOpen = inf.ReadBoolean();
            BleedOffValveOpen = inf.ReadBoolean();
            HoldingValve = (ValveState)inf.ReadInt32();
            CylVolumeM3 = inf.ReadSingle();
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
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

            // In simple brake mode set emergency reservoir volume, override high volume values to allow faster brake release.
            if (Car.Simulator.Settings.SimpleControlPhysics && EmergResVolumeM3 > 2.0)
                EmergResVolumeM3 = 0.7f;

            BrakeLine1PressurePSI = Car.Train.EqualReservoirPressurePSIorInHg;
            BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
            BrakeLine3PressurePSI = 0;
            if (maxPressurePSI > 0)
                ControlResPressurePSI = maxPressurePSI;
            FullServPressurePSI = fullServPressurePSI;
            CylPressurePSI = AutoCylPressurePSI = immediateRelease ? 0 : Math.Min((maxPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio, MaxCylPressurePSI);
            AuxResPressurePSI = Math.Max(TwoPipes ? maxPressurePSI : maxPressurePSI - AutoCylPressurePSI / AuxCylVolumeRatio, BrakeLine1PressurePSI);
            if ((Car as MSTSWagon).EmergencyReservoirPresent)
            {
                EmergResPressurePSI = Math.Max(AuxResPressurePSI, maxPressurePSI);
                if (EmergencyValveActuationRatePSIpS == 0) EmergencyValveActuationRatePSIpS = 15;
            }
            TripleValveState = AutoCylPressurePSI < 1 ? ValveState.Release : ValveState.Lap;
            HoldingValve = ValveState.Release;
            HandbrakePercent = handbrakeOn & (Car as MSTSWagon).HandBrakePresent ? 100 : 0;
            SetRetainer(RetainerSetting.Exhaust);
            MSTSLocomotive loco = Car as MSTSLocomotive;
            if (loco != null) 
            {
                loco.MainResPressurePSI = loco.MaxMainResPressurePSI;
            }

            if (EmergResVolumeM3 > 0 && EmergAuxVolumeRatio > 0 && BrakePipeVolumeM3 > 0)
                AuxBrakeLineVolumeRatio = EmergResVolumeM3 / EmergAuxVolumeRatio / BrakePipeVolumeM3;
            else
                AuxBrakeLineVolumeRatio = 3.1f;
                     
            CylVolumeM3 = EmergResVolumeM3 / EmergAuxVolumeRatio / AuxCylVolumeRatio;
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
            if (valveType == MSTSWagon.BrakeValveType.Distributor)
            {
                float targetPressurePSI = (ControlResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                if (!disableGradient && targetPressurePSI > AutoCylPressurePSI && EmergencyValveActuationRatePSIpS > 0 && (prevBrakePipePressurePSI - BrakeLine1PressurePSI) > Math.Max(elapsedClockSeconds, 0.0001f) * EmergencyValveActuationRatePSIpS)
                    TripleValveState = ValveState.Emergency;
                else if (targetPressurePSI < AutoCylPressurePSI - (TripleValveState != ValveState.Release ? 2.2f : 0f)
                    || targetPressurePSI < 2.2f) // The latter is a UIC regulation (0.15 bar)
                    TripleValveState = ValveState.Release;
                else if (TripleValveState != ValveState.Emergency && targetPressurePSI > AutoCylPressurePSI + (TripleValveState != ValveState.Apply ? 2.2f : 0f))
                    TripleValveState = ValveState.Apply;
                else if (TripleValveState != ValveState.Emergency)
                    TripleValveState = ValveState.Lap;
            }
            else if (valveType == MSTSWagon.BrakeValveType.TripleValve || valveType == MSTSWagon.BrakeValveType.DistributingValve)
            {
                if (!disableGradient && BrakeLine1PressurePSI < AuxResPressurePSI - 1 && EmergencyValveActuationRatePSIpS > 0 && (prevBrakePipePressurePSI - BrakeLine1PressurePSI) > Math.Max(elapsedClockSeconds, 0.0001f) * EmergencyValveActuationRatePSIpS)
                    TripleValveState = ValveState.Emergency;
                else if (BrakeLine1PressurePSI > AuxResPressurePSI + 1)
                    TripleValveState = ValveState.Release;
                else if (TripleValveState == ValveState.Emergency && BrakeLine1PressurePSI > AuxResPressurePSI)
                    TripleValveState = ValveState.Release;
                else if (TripleValveState != ValveState.Emergency && BrakeLine1PressurePSI < AuxResPressurePSI - 1)
                    TripleValveState = ValveState.Apply;
                else if (TripleValveState == ValveState.Apply && BrakeLine1PressurePSI >= AuxResPressurePSI)
                    TripleValveState = ValveState.Lap;
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
            prevBrakePipePressurePSI = BrakeLine1PressurePSI;
        }

        public override void Update(float elapsedClockSeconds)
        {
            float threshold = ((Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.Distributor) ? Math.Max((ControlResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio, 0) : 0;

            if (BleedOffValveOpen)
            {
                if ((Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.Distributor)
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
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                if (AuxResPressurePSI - dp / AuxCylVolumeRatio < AutoCylPressurePSI + dp)
                    dp = (AuxResPressurePSI - AutoCylPressurePSI) * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
                if (((Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.Distributor) && TripleValveState != ValveState.Emergency && dp > threshold - AutoCylPressurePSI)
                    dp = threshold - AutoCylPressurePSI;
                if (AutoCylPressurePSI + dp > MaxCylPressurePSI)
                    dp = MaxCylPressurePSI - AutoCylPressurePSI;
                if (BrakeLine1PressurePSI > AuxResPressurePSI - dp / AuxCylVolumeRatio && !BleedOffValveOpen)
                    dp = (AuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                if (dp < 0)
                    dp = 0;

                AuxResPressurePSI -= dp / AuxCylVolumeRatio;
                AutoCylPressurePSI += dp;

                if (TripleValveState == ValveState.Emergency)
                {
                    if ((Car as MSTSWagon).EmergencyReservoirPresent)
                    {
                        dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                        if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                            dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                        EmergResPressurePSI -= dp;
                        AuxResPressurePSI += dp * EmergAuxVolumeRatio;
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
                }
            }

            // triple valve set to release pressure in brake cylinder and EP valve set
            if (TripleValveState == ValveState.Release && (Car as MSTSWagon).BrakeValve != MSTSWagon.BrakeValveType.None)
            {
                if ((Car as MSTSWagon).EmergencyReservoirPresent)
				{
                    if (AuxResPressurePSI < EmergResPressurePSI && AuxResPressurePSI < BrakeLine1PressurePSI)
					{
						float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
						if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
							dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
						if (BrakeLine1PressurePSI < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
							dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / EmergAuxVolumeRatio;
						EmergResPressurePSI -= dp;
						AuxResPressurePSI += dp * EmergAuxVolumeRatio;
					}
					if (AuxResPressurePSI > EmergResPressurePSI)
					{
						float dp = elapsedClockSeconds * EmergResChargingRatePSIpS;
						if (EmergResPressurePSI + dp > AuxResPressurePSI - dp * EmergAuxVolumeRatio)
							dp = (AuxResPressurePSI - EmergResPressurePSI) / (1 + EmergAuxVolumeRatio);
						EmergResPressurePSI += dp;
						AuxResPressurePSI -= dp * EmergAuxVolumeRatio;
					}
				}
                if (AuxResPressurePSI < BrakeLine1PressurePSI && (!TwoPipes || !MRPAuxResCharging || ((Car as MSTSWagon).BrakeValve != MSTSWagon.BrakeValveType.Distributor) || BrakeLine2PressurePSI < BrakeLine1PressurePSI) && !BleedOffValveOpen)
                {
                    float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS; // Change in pressure for train brake pipe.
                    if (AuxResPressurePSI + dp > BrakeLine1PressurePSI - dp * AuxBrakeLineVolumeRatio)
                        dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                    AuxResPressurePSI += dp;
                    BrakeLine1PressurePSI -= dp * AuxBrakeLineVolumeRatio;  // Adjust the train brake pipe pressure
                }
                if ((Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.Distributor)
                {
                    if (ControlResPressurePSI < BrakeLine1PressurePSI)
                    {
                        ControlResPressurePSI = BrakeLine1PressurePSI;
                    }
                    else if (ControlResPressurePSI > BrakeLine1PressurePSI && ControlResPressurePSI < BrakeLine1PressurePSI + 1) // Overcharge elimination
                    {
                        float dp = elapsedClockSeconds * BrakeInsensitivityPSIpS;
                        ControlResPressurePSI = Math.Max(ControlResPressurePSI - dp, BrakeLine1PressurePSI);
                    }
                }
                if (AuxResPressurePSI > BrakeLine1PressurePSI) // Allow small flow from auxiliary reservoir to brake pipe so the triple valve is not sensible to small pressure variations when in release position
                {
                    float dp = elapsedClockSeconds * BrakeInsensitivityPSIpS;
                    if (AuxResPressurePSI - dp < BrakeLine1PressurePSI + dp * AuxBrakeLineVolumeRatio)
                        dp = (AuxResPressurePSI - BrakeLine1PressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                    AuxResPressurePSI -= dp;
                    BrakeLine1PressurePSI += dp * AuxBrakeLineVolumeRatio;
                }
            }

            // Handle brake release: reduce cylinder pressure if all triple valve, EP holding valve and retainers allow so
            float minCylPressurePSI = Math.Max(threshold, RetainerPressureThresholdPSI);
            if (TripleValveState == ValveState.Release && HoldingValve == ValveState.Release && AutoCylPressurePSI > minCylPressurePSI)
            {
                float dp = elapsedClockSeconds * ReleaseRatePSIpS;
                if (AutoCylPressurePSI - dp < minCylPressurePSI)
                    dp = AutoCylPressurePSI-minCylPressurePSI;
                if (dp < 0)
                    dp = 0;
                AutoCylPressurePSI -= dp;
            }

            // Charge Auxiliary reservoir for MRP
            if (TwoPipes
                && MRPAuxResCharging
                && (Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.Distributor
                && AuxResPressurePSI < BrakeLine2PressurePSI
                && AuxResPressurePSI < ControlResPressurePSI
                && (BrakeLine2PressurePSI > BrakeLine1PressurePSI || TripleValveState != ValveState.Release) && !BleedOffValveOpen)
            {
                float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
                if (AuxResPressurePSI + dp > BrakeLine2PressurePSI - dp * AuxBrakeLineVolumeRatio)
                    dp = (BrakeLine2PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                AuxResPressurePSI += dp;
                BrakeLine2PressurePSI -= dp * AuxBrakeLineVolumeRatio;
            }

            if (AutoCylPressurePSI < 0)
                AutoCylPressurePSI = 0;
            
            if (Car is MSTSLocomotive && (Car as MSTSWagon).BrakeValve == MSTSWagon.BrakeValveType.DistributingValve)
            {
                // For distributing valves, we use AutoCylPressurePSI as "Application Chamber/Pipe" pressure
                // CylPressurePSI is the actual pressure applied to cylinders
                var loco = Car as MSTSLocomotive;
                var engineBrakeStatus = loco.EngineBrakeController.Notches[loco.EngineBrakeController.CurrentNotch].Type;
                var trainBrakeStatus = loco.TrainBrakeController.Notches[loco.TrainBrakeController.CurrentNotch].Type;
                 // BailOff
                if (engineBrakeStatus == ControllerState.BailOff)
                {
                    AutoCylPressurePSI -= MaxReleaseRatePSIpS * elapsedClockSeconds;
                    if (AutoCylPressurePSI < 0) AutoCylPressurePSI = 0;
                }
                // Emergency application
                if (trainBrakeStatus == ControllerState.Emergency)
                {
                    float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                    if (dp > MaxCylPressurePSI - AutoCylPressurePSI)
                        dp = MaxCylPressurePSI - AutoCylPressurePSI;
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
                // TODO: Drain air from main reservoir
                CylPressurePSI = AutoCylPressurePSI;
            }
            else
            {
                if (Car is MSTSLocomotive loco && loco.EngineType != TrainCar.EngineTypes.Control)  // TODO - Control cars ned to be linked to power suppy requirements.
                {
                    float demandedPressurePSI = Math.Max(AutoCylPressurePSI, BrakeLine3PressurePSI);
                    //    if (Car is MSTSLocomotive loco && loco.LocomotivePowerSupply.MainPowerSupplyOn)
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
                                    AutoCylPressurePSI -= MaxReleaseRatePSIpS * elapsedClockSeconds;
                                    if (AutoCylPressurePSI < 0)
                                        AutoCylPressurePSI = 0;
                                }
                            }
                        }
                        if (loco.DynamicBrakePercent > 0 && Car.MaxBrakeForceN > 0)
                        {
                            if (loco.DynamicBrakePartialBailOff)
                            {
                                var requiredBrakeForceN = Math.Min(AutoCylPressurePSI / MaxCylPressurePSI, 1) * Car.MaxBrakeForceN;
                                var localBrakeForceN = loco.DynamicBrakeForceN + Math.Min(CylPressurePSI / MaxCylPressurePSI, 1) * Car.MaxBrakeForceN;
                                if (localBrakeForceN > requiredBrakeForceN - 0.15f * Car.MaxBrakeForceN)
                                {
                                    demandedPressurePSI = Math.Min(Math.Max((requiredBrakeForceN - loco.DynamicBrakeForceN)/Car.MaxBrakeForceN*MaxCylPressurePSI, 0), MaxCylPressurePSI);
                                    if (demandedPressurePSI > CylPressurePSI && demandedPressurePSI < CylPressurePSI + 4) // Allow some margin for unnecessary air brake application
                                    {
                                        demandedPressurePSI = CylPressurePSI;
                                    }
                                    if (demandedPressurePSI < BrakeLine3PressurePSI)
                                        demandedPressurePSI = BrakeLine3PressurePSI;
                                }
                            }
                            else if (loco.DynamicBrakeAutoBailOff)
                            {
                                if (loco.DynamicBrakeForceCurves == null)
                                {
                                    demandedPressurePSI = BrakeLine3PressurePSI;
                                }
                                else
                                {
                                    var dynforce = loco.DynamicBrakeForceCurves.Get(1.0f, loco.AbsSpeedMpS);
                                    if ((loco.MaxDynamicBrakeForceN == 0 && dynforce > 0) || dynforce > loco.MaxDynamicBrakeForceN * 0.6)
                                    {
                                        demandedPressurePSI = BrakeLine3PressurePSI;
                                    }
                                }
                            }
                        }
                    }
                    // TODO: this first clause is intended for locomotives fitted with some sort of proportional valve
                    // i.e. the triple valve is not directly attached to the physical brake cylinder
                    // This allows e.g. blending, variable load, or higher pressures than provided by triple valves
                    if (loco.DynamicBrakeAutoBailOff || loco.DynamicBrakeAutoBailOff)
                    {
                        if (demandedPressurePSI > CylPressurePSI)
                        {
                            float dp = elapsedClockSeconds * loco.EngineBrakeApplyRatePSIpS;
                            if (dp > demandedPressurePSI - CylPressurePSI)
                                dp = demandedPressurePSI - CylPressurePSI;
                            /* TODO: Proportional valves need air from the main reservoir
                            if (BrakeLine2PressurePSI - dp * AuxBrakeLineVolumeRatio / AuxCylVolumeRatio < CylPressurePSI + dp)
                                dp = (BrakeLine2PressurePSI - CylPressurePSI) / (1 + AuxBrakeLineVolumeRatio / AuxCylVolumeRatio);
                            BrakeLine2PressurePSI -= dp * AuxBrakeLineVolumeRatio / AuxCylVolumeRatio;*/
                            CylPressurePSI += dp;
                        }
                        else if (demandedPressurePSI < CylPressurePSI) CylPressurePSI = Math.Max(demandedPressurePSI, CylPressurePSI - elapsedClockSeconds * loco.EngineBrakeReleaseRatePSIpS);
                    }
                    else // Rest of cases
                    {
                        CylPressurePSI = Math.Max(AutoCylPressurePSI, BrakeLine3PressurePSI);
                    }
                }
                else
                {
                    CylPressurePSI = Math.Max(AutoCylPressurePSI, BrakeLine3PressurePSI);
                }
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
                    CylPressurePSI = AutoCylPressurePSI;
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
                Car.BrakeShoeForceN = Car.MaxBrakeForceN * Math.Min(CylPressurePSI / MaxCylPressurePSI, 1);
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
            SoundTriggerCounter = SoundTriggerCounter + elapsedClockSeconds;
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

            // Propagate brake line (1) data if pressure gradient disabled
            if (lead != null && lead.BrakePipeChargingRatePSIorInHgpS >= 1000)
            {   // pressure gradient disabled
                if (lead.BrakeSystem.BrakeLine1PressurePSI < train.EqualReservoirPressurePSIorInHg)
                {
                    var dp1 = train.EqualReservoirPressurePSIorInHg - lead.BrakeSystem.BrakeLine1PressurePSI;
                    lead.MainResPressurePSI -= dp1 * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3;
                }
                foreach (TrainCar car in train.Cars)
                {
                    if (car.BrakeSystem.BrakeLine1PressurePSI >= 0)
                        car.BrakeSystem.BrakeLine1PressurePSI = train.EqualReservoirPressurePSIorInHg;
                    if (car.BrakeSystem.TwoPipes)
                        car.BrakeSystem.BrakeLine2PressurePSI = Math.Min(lead.MainResPressurePSI, lead.MaximumMainReservoirPipePressurePSI);
                }
            }
            else
            {   // approximate pressure gradient in train pipe line1
                var brakePipeTimeFactorS = lead == null ? 0.0015f : lead.BrakePipeTimeFactorS;
                int nSteps = (int)(elapsedClockSeconds / brakePipeTimeFactorS + 1);
                float trainPipeTimeVariationS = elapsedClockSeconds / nSteps;
                float trainPipeLeakLossPSI = lead == null ? 0.0f : (trainPipeTimeVariationS * lead.TrainBrakePipeLeakPSIorInHgpS);
                float serviceTimeFactor = lead != null ? lead.TrainBrakeController != null && lead.TrainBrakeController.EmergencyBraking ? lead.BrakeEmergencyTimeFactorPSIpS : lead.BrakeServiceTimeFactorPSIpS : 0;
                for (int i = 0; i < nSteps; i++)
                {
                    if (lead != null)
                    {
                        // Allow for leaking train air brakepipe
                        if (lead.BrakeSystem.BrakeLine1PressurePSI - trainPipeLeakLossPSI > 0 && lead.TrainBrakePipeLeakPSIorInHgpS != 0) // if train brake pipe has pressure in it, ensure result will not be negative if loss is subtracted
                        {
                            lead.BrakeSystem.BrakeLine1PressurePSI -= trainPipeLeakLossPSI;
                        }

                        if (lead.TrainBrakeController.TrainBrakeControllerState != ControllerState.Neutral)
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
                                }
                            }
                            // reduce pressure in lead brake line if brake pipe pressure is above equalising pressure - apply brakes
                            else if (lead.BrakeSystem.BrakeLine1PressurePSI > train.EqualReservoirPressurePSIorInHg)
                            {
                                float serviceVariationFactor = Math.Min(trainPipeTimeVariationS / serviceTimeFactor, 0.95f);
                                float pressureDiffPSI = serviceVariationFactor * lead.BrakeSystem.BrakeLine1PressurePSI;
                                if (lead.BrakeSystem.BrakeLine1PressurePSI - pressureDiffPSI < train.EqualReservoirPressurePSIorInHg)
                                    pressureDiffPSI = lead.BrakeSystem.BrakeLine1PressurePSI - train.EqualReservoirPressurePSIorInHg;
                                lead.BrakeSystem.BrakeLine1PressurePSI -= pressureDiffPSI;
                            }
                        }

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
                            car.BrakeSystem.BrakeLine1PressurePSI = Math.Max(car.BrakeSystem.BrakeLine1PressurePSI * (1 - trainPipeTimeVariationS / brakePipeTimeFactorS), 0);
                        }
                        if ((nextCar == null || !nextCar.BrakeSystem.FrontBrakeHoseConnected) && car.BrakeSystem.AngleCockBOpen)
                        {
                            car.BrakeSystem.BrakeLine1PressurePSI = Math.Max(car.BrakeSystem.BrakeLine1PressurePSI * (1 - trainPipeTimeVariationS / brakePipeTimeFactorS), 0);
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
            // Equalize main reservoir with train pipe for every locomotive
            foreach (TrainCar car in train.Cars)
            {
                if (car is MSTSLocomotive loco && car.BrakeSystem.TwoPipes)
                {
                    float volumeRatio = loco.BrakeSystem.BrakePipeVolumeM3 / loco.MainResVolumeM3;
                    float dp = Math.Min((loco.MainResPressurePSI - loco.BrakeSystem.BrakeLine2PressurePSI) / (1 + volumeRatio), loco.MaximumMainReservoirPipePressurePSI - loco.BrakeSystem.BrakeLine2PressurePSI);
                    loco.MainResPressurePSI -= dp * volumeRatio;
                    loco.BrakeSystem.BrakeLine2PressurePSI += dp;
                    if (loco.MainResPressurePSI < 0) loco.MainResPressurePSI = 0;
                    if (loco.BrakeSystem.BrakeLine2PressurePSI < 0) loco.BrakeSystem.BrakeLine2PressurePSI = 0;
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
