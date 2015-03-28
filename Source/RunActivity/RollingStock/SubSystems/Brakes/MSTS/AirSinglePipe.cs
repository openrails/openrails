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

using System;
using System.IO;
using Orts.Parsers.Msts;
using ORTS.Common;

namespace ORTS
{
    public class AirSinglePipe : MSTSBrakeSystem
    {
        protected float MaxHandbrakeForceN;
        protected float MaxBrakeForceN = 89e3f;
        protected TrainCar Car;
        protected float HandbrakePercent;
        protected float CylPressurePSI = 64;
        protected float AutoCylPressurePSI = 64;
        protected float AuxResPressurePSI = 64;
        protected float EmergResPressurePSI = 64;
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
        protected float EmergResChargingRatePSIpS = 1.684f;
        protected float EmergAuxVolumeRatio = 1.4f;
        protected string DebugType = string.Empty;
        protected string RetainerDebugState = string.Empty;
        protected bool NoMRPAuxResCharging;

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
            BrakePipeVolumeM3 = (0.032f * 0.032f * (float)Math.PI / 4f) * (1 + car.CarLengthM); // Using DN32 (1-1/4") pipe
            DebugType = "1P";

            // Force graduated releasable brakes. Workaround for MSTS with bugs preventing to set eng/wag files correctly for this.
            (Car as MSTSWagon).DistributorPresent = Car.Simulator.Settings.GraduatedRelease;

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
            MaxHandbrakeForceN = thiscopy.MaxHandbrakeForceN;
            MaxBrakeForceN = thiscopy.MaxBrakeForceN;
            MaxCylPressurePSI = thiscopy.MaxCylPressurePSI;
            AuxCylVolumeRatio = thiscopy.AuxCylVolumeRatio;
            AuxBrakeLineVolumeRatio = thiscopy.AuxBrakeLineVolumeRatio;
            EmergResVolumeM3 = thiscopy.EmergResVolumeM3;
            RetainerPressureThresholdPSI = thiscopy.RetainerPressureThresholdPSI;
            ReleaseRatePSIpS = thiscopy.ReleaseRatePSIpS;
            MaxReleaseRatePSIpS = thiscopy.MaxReleaseRatePSIpS;
            MaxApplicationRatePSIpS = thiscopy.MaxApplicationRatePSIpS;
            MaxAuxilaryChargingRatePSIpS = thiscopy.MaxAuxilaryChargingRatePSIpS;
            EmergResChargingRatePSIpS = thiscopy.EmergResChargingRatePSIpS;
            EmergAuxVolumeRatio = thiscopy.EmergAuxVolumeRatio;
            TwoPipes = thiscopy.TwoPipes;
            NoMRPAuxResCharging = thiscopy.NoMRPAuxResCharging;
        }

        public override string GetStatus(PressureUnit unit)
        {
            return string.Format("BP {0}", FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, unit, true));
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, PressureUnit unit)
        {
            string s = string.Format(" EQ {0}", FormatStrings.FormatPressure(Car.Train.BrakeLine1PressurePSIorInHg, PressureUnit.PSI, unit, true));
            s += string.Format(" BC {0} BP {1}", FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, unit, false), FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, unit, false));
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += " EOT " + lastCarBrakeSystem.GetStatus(unit);
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override string[] GetDebugStatus(PressureUnit unit)
        {
            return new string[] {
                DebugType,
                this is SingleTransferPipe ? string.Empty : FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, unit, true),
                FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, unit, true),
                this is SingleTransferPipe ? string.Empty : FormatStrings.FormatPressure(AuxResPressurePSI, PressureUnit.PSI, unit, true),
                (Car as MSTSWagon).EmergencyReservoirPresent ? FormatStrings.FormatPressure(EmergResPressurePSI, PressureUnit.PSI, unit, true) : string.Empty,
                TwoPipes ? FormatStrings.FormatPressure(BrakeLine2PressurePSI, PressureUnit.PSI, unit, true) : string.Empty,
                (Car as MSTSWagon).RetainerPositions == 0 ? string.Empty : RetainerDebugState,
                this is SingleTransferPipe ? string.Empty : Viewer3D.Viewer.Catalog.GetString(GetStringAttribute.GetPrettyName(TripleValveState)),
                string.Empty, // Spacer because the state above needs 2 columns.
                (Car as MSTSWagon).HandBrakePresent ? string.Format("{0:F0}%", HandbrakePercent) : string.Empty,
                FrontBrakeHoseConnected ? "I" : "T",
                string.Format("A{0} B{1}", AngleCockAOpen ? "+" : "-", AngleCockBOpen ? "+" : "-"),
                BleedOffValveOpen ? Viewer3D.Viewer.Catalog.GetString("Open") : string.Empty,
            };
        }

        public override float GetCylPressurePSI()
        {
            return CylPressurePSI;
        }

        public override float GetVacResPressurePSI()
        {
            return 0;
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(maxhandbrakeforce": MaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(maxbrakeforce": MaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
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
            outf.Write(FullServPressurePSI);
            outf.Write((int)TripleValveState);
            outf.Write(FrontBrakeHoseConnected);
            outf.Write(AngleCockAOpen);
            outf.Write(AngleCockBOpen);
            outf.Write(BleedOffValveOpen);
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
            FullServPressurePSI = inf.ReadSingle();
            TripleValveState = (ValveState)inf.ReadInt32();
            FrontBrakeHoseConnected = inf.ReadBoolean();
            AngleCockAOpen = inf.ReadBoolean();
            AngleCockBOpen = inf.ReadBoolean();
            BleedOffValveOpen = inf.ReadBoolean();
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            BrakeLine1PressurePSI = Car.Train.BrakeLine1PressurePSIorInHg;
            BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
            BrakeLine3PressurePSI = 0;
            AuxResPressurePSI = maxPressurePSI > BrakeLine1PressurePSI ? maxPressurePSI : BrakeLine1PressurePSI;
            if ((Car as MSTSWagon).EmergencyReservoirPresent || maxPressurePSI > 0)
                EmergResPressurePSI = maxPressurePSI;
            FullServPressurePSI = fullServPressurePSI;
            AutoCylPressurePSI = immediateRelease ? 0 : Math.Min((maxPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio, MaxCylPressurePSI);
            TripleValveState = ValveState.Lap;
            HandbrakePercent = handbrakeOn & (Car as MSTSWagon).HandBrakePresent ? 100 : 0;
            SetRetainer(RetainerSetting.Exhaust);
            if (Car is MSTSLocomotive)
                (Car as MSTSLocomotive).MainResPressurePSI = (Car as MSTSLocomotive).MaxMainResPressurePSI;

            if (EmergResVolumeM3 > 0 && EmergAuxVolumeRatio > 0 && BrakePipeVolumeM3 > 0)
                AuxBrakeLineVolumeRatio = EmergResVolumeM3 / EmergAuxVolumeRatio / BrakePipeVolumeM3;
            else
                AuxBrakeLineVolumeRatio = 3.1f;
        }

        /// <summary>
        /// Used when initial speed > 0
        /// </summary>
        public override void InitializeMoving ()
        {
            var emergResPressurePSI = EmergResPressurePSI;
            Initialize(false, 0, FullServPressurePSI, true);
            EmergResPressurePSI = emergResPressurePSI;
        }

        public override void LocoInitializeMoving() // starting conditions when starting speed > 0
        {
        }

        public virtual void UpdateTripleValveState(float controlPressurePSI)
        {
            if (BrakeLine1PressurePSI < FullServPressurePSI - 1)
                TripleValveState = ValveState.Emergency;
            else if (BrakeLine1PressurePSI > AuxResPressurePSI + 1)
                TripleValveState = ValveState.Release;
            else if (TripleValveState == ValveState.Emergency && BrakeLine1PressurePSI > AuxResPressurePSI)
                TripleValveState = ValveState.Release;
            else if (EmergResPressurePSI > 70 && BrakeLine1PressurePSI > EmergResPressurePSI * 0.97f) // UIC regulation: for 5 bar systems, release if > 4.85 bar
                TripleValveState = ValveState.Release;
            else if (TripleValveState != ValveState.Emergency && BrakeLine1PressurePSI < AuxResPressurePSI - 1)
                TripleValveState = ValveState.Apply;
            else if (TripleValveState == ValveState.Apply && BrakeLine1PressurePSI >= AuxResPressurePSI)
                TripleValveState = ValveState.Lap;
        }

        public override void Update(float elapsedClockSeconds)
        {
            ValveState prevTripleValueState = TripleValveState;

            // Emergency reservoir's second role (in OpenRails) is to act as a control reservoir,
            // maintaining a reference control pressure for graduated release brake actions.
            // Thus this pressure must be set even in brake systems ER not present otherwise. It just stays static in this case.
            float threshold = Math.Max(RetainerPressureThresholdPSI,
                (Car as MSTSWagon).DistributorPresent ? (EmergResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio : 0);

            if (BleedOffValveOpen)
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
            else
                UpdateTripleValveState(threshold);

            if (TripleValveState == ValveState.Apply || TripleValveState == ValveState.Emergency)
            {
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                if (AuxResPressurePSI - dp / AuxCylVolumeRatio < AutoCylPressurePSI + dp)
                    dp = (AuxResPressurePSI - AutoCylPressurePSI) * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
                if (TwoPipes && dp > threshold - AutoCylPressurePSI)
                    dp = threshold - AutoCylPressurePSI;
                if (AutoCylPressurePSI + dp > MaxCylPressurePSI)
                    dp = MaxCylPressurePSI - AutoCylPressurePSI;
                if (BrakeLine1PressurePSI > AuxResPressurePSI - dp / AuxCylVolumeRatio && !BleedOffValveOpen)
                    dp = (AuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;

                AuxResPressurePSI -= dp / AuxCylVolumeRatio;
                AutoCylPressurePSI += dp;

                if (TripleValveState == ValveState.Emergency && (Car as MSTSWagon).EmergencyReservoirPresent)
                {
                    dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                    if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                        dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                    EmergResPressurePSI -= dp;
                    AuxResPressurePSI += dp * EmergAuxVolumeRatio;
                }
            }
            if (TripleValveState == ValveState.Release)
            {
                if (AutoCylPressurePSI > threshold)
                {
                    AutoCylPressurePSI -= elapsedClockSeconds * ReleaseRatePSIpS;
                    if (AutoCylPressurePSI < threshold)
                        AutoCylPressurePSI = threshold;
                }
                if ((Car as MSTSWagon).EmergencyReservoirPresent)
				{
                    if (!(Car as MSTSWagon).DistributorPresent && AuxResPressurePSI < EmergResPressurePSI && AuxResPressurePSI < BrakeLine1PressurePSI)
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
                if (AuxResPressurePSI < BrakeLine1PressurePSI && (!TwoPipes || NoMRPAuxResCharging || BrakeLine2PressurePSI < BrakeLine1PressurePSI))
                {
                    float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
                    if (AuxResPressurePSI + dp > BrakeLine1PressurePSI - dp * AuxBrakeLineVolumeRatio)
                        dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                    AuxResPressurePSI += dp;
                    BrakeLine1PressurePSI -= dp * AuxBrakeLineVolumeRatio;
                }
            }
            if (TwoPipes
                && !NoMRPAuxResCharging
                && AuxResPressurePSI < BrakeLine2PressurePSI
                && AuxResPressurePSI < EmergResPressurePSI
                && (BrakeLine2PressurePSI > BrakeLine1PressurePSI || TripleValveState != ValveState.Release))
            {
                float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
                if (AuxResPressurePSI + dp > BrakeLine2PressurePSI - dp * AuxBrakeLineVolumeRatio)
                    dp = (BrakeLine2PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                AuxResPressurePSI += dp;
                BrakeLine2PressurePSI -= dp * AuxBrakeLineVolumeRatio;
            }
            if (TripleValveState != prevTripleValueState)
            {
                switch (TripleValveState)
                {
                    case ValveState.Release: Car.SignalEvent(Event.TrainBrakePressureDecrease); break;
                    case ValveState.Apply:
                    case ValveState.Emergency: Car.SignalEvent(Event.TrainBrakePressureIncrease); break;
                }
            }
            if (BrakeLine3PressurePSI >= 1000)
            {
                BrakeLine3PressurePSI -= 1000;
                AutoCylPressurePSI -= MaxReleaseRatePSIpS * elapsedClockSeconds;
            }
            if (AutoCylPressurePSI < 0)
                AutoCylPressurePSI = 0;
            if (AutoCylPressurePSI < BrakeLine3PressurePSI)
                CylPressurePSI = BrakeLine3PressurePSI;
            else
                CylPressurePSI = AutoCylPressurePSI;
            float f = MaxBrakeForceN * Math.Min(CylPressurePSI / MaxCylPressurePSI, 1);
            if (f < MaxHandbrakeForceN * HandbrakePercent / 100)
                f = MaxHandbrakeForceN * HandbrakePercent / 100;
            Car.BrakeForceN = f;
        }

        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            PropagateBrakeLinePressures(elapsedClockSeconds, Car, TwoPipes);
        }

        protected static void PropagateBrakeLinePressures(float elapsedClockSeconds, TrainCar trainCar, bool twoPipes)
        {
            var train = trainCar.Train;
            var lead = trainCar as MSTSLocomotive;
            var brakePipeTimeFactorS = lead == null ? 0.003f : lead.BrakePipeTimeFactorS;
            int nSteps = (int)(elapsedClockSeconds * 2 / brakePipeTimeFactorS + 1);
            float dt = elapsedClockSeconds / nSteps;

            // Propagate brake line (1) data
            if (lead != null && lead.BrakePipeChargingRatePSIpS > 1000)
            {   // pressure gradiant disabled
                foreach (TrainCar car in train.Cars)
                    if (car.BrakeSystem.BrakeLine1PressurePSI >= 0)
                        car.BrakeSystem.BrakeLine1PressurePSI = train.BrakeLine1PressurePSIorInHg;
            }
            else
            {   // approximate pressure gradiant in line1
                float serviceTimeFactor = lead != null ? lead.TrainBrakeController != null && lead.TrainBrakeController.EmergencyBraking ? lead.BrakeEmergencyTimeFactorS : lead.BrakeServiceTimeFactorS : 0;
                for (int i = 0; i < nSteps; i++)
                {
                    if (lead != null)
                    {
                        if (lead.BrakeSystem.BrakeLine1PressurePSI < train.BrakeLine1PressurePSIorInHg)
                        {
                            float dp = dt * lead.BrakePipeChargingRatePSIpS;
                            if (lead.BrakeSystem.BrakeLine1PressurePSI + dp > train.BrakeLine1PressurePSIorInHg)
                                dp = train.BrakeLine1PressurePSIorInHg - lead.BrakeSystem.BrakeLine1PressurePSI;
                            if (lead.BrakeSystem.BrakeLine1PressurePSI + dp > lead.MainResPressurePSI)
                                dp = lead.MainResPressurePSI - lead.BrakeSystem.BrakeLine1PressurePSI;
                            if (dp < 0)
                                dp = 0;
                            lead.BrakeSystem.BrakeLine1PressurePSI += dp;
                            lead.MainResPressurePSI -= dp * lead.BrakeSystem.BrakePipeVolumeM3 / lead.MainResVolumeM3;
                        }
                        else if (lead.BrakeSystem.BrakeLine1PressurePSI > train.BrakeLine1PressurePSIorInHg)
                            lead.BrakeSystem.BrakeLine1PressurePSI *= (1 - dt / serviceTimeFactor);
                    }
                    TrainCar car0 = train.Cars[0];
                    float p0 = car0.BrakeSystem.BrakeLine1PressurePSI;
                    foreach (TrainCar car in train.Cars)
                    {
                        float p1 = car.BrakeSystem.BrakeLine1PressurePSI;
                        if (car == train.Cars[0] || car.BrakeSystem.FrontBrakeHoseConnected && car.BrakeSystem.AngleCockAOpen && car0.BrakeSystem.AngleCockBOpen)
                        {
                            float dp = dt * (p1 - p0) / brakePipeTimeFactorS;
                            car.BrakeSystem.BrakeLine1PressurePSI -= dp;
                            car0.BrakeSystem.BrakeLine1PressurePSI += dp;
                        }
                        if (!car.BrakeSystem.FrontBrakeHoseConnected)
                        {
                            if (car.BrakeSystem.AngleCockAOpen)
                                car.BrakeSystem.BrakeLine1PressurePSI -= dt * p1 / brakePipeTimeFactorS;
                            if (car0.BrakeSystem.AngleCockBOpen && car != car0)
                                car0.BrakeSystem.BrakeLine1PressurePSI -= dt * p0 / brakePipeTimeFactorS;
                        }
                        if (car == train.Cars[train.Cars.Count - 1] && car.BrakeSystem.AngleCockBOpen)
                        {
                            car.BrakeSystem.BrakeLine1PressurePSI -= dt * p1 / brakePipeTimeFactorS;
                        }
                        p0 = p1;
                        car0 = car;
                    }
                }
            }

            // Propagate main reservoir pipe (2) and engine brake pipe (3) data
            int first = -1;
            int last = -1;
            train.FindLeadLocomotives(ref first, ref last);
            float sumpv = 0;
            float sumv = 0;
            int continuousFromInclusive = 0;
            int continuousToExclusive = train.Cars.Count;
            for (int i = 0; i < train.Cars.Count; i++)
            {
                BrakeSystem brakeSystem = train.Cars[i].BrakeSystem;
                if (i < first && (!train.Cars[i + 1].BrakeSystem.FrontBrakeHoseConnected || !brakeSystem.AngleCockBOpen || !train.Cars[i + 1].BrakeSystem.AngleCockAOpen || !train.Cars[i].BrakeSystem.TwoPipes))
                {
                    if (continuousFromInclusive < i + 1)
                    {
                        sumv = sumpv = 0;
                        continuousFromInclusive = i + 1;
                    }
                    continue;
                }
                if (i > last && i > 0 && (!brakeSystem.FrontBrakeHoseConnected || !brakeSystem.AngleCockAOpen || !train.Cars[i - 1].BrakeSystem.AngleCockBOpen || !train.Cars[i].BrakeSystem.TwoPipes))
                {
                    if (continuousToExclusive > i)
                        continuousToExclusive = i;
                    continue;
                }

                // Collect main reservoir pipe (2) data
                if (first <= i && i <= last || twoPipes && continuousFromInclusive <= i && i < continuousToExclusive)
                {
                    sumv += brakeSystem.BrakePipeVolumeM3;
                    sumpv += brakeSystem.BrakePipeVolumeM3 * brakeSystem.BrakeLine2PressurePSI;
                    var eng = train.Cars[i] as MSTSLocomotive;
                    if (eng != null)
                    {
                        sumv += eng.MainResVolumeM3;
                        sumpv += eng.MainResVolumeM3 * eng.MainResPressurePSI;
                    }
                }

                // Collect and propagate engine brake pipe (3) data
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
                        if (p < train.BrakeLine3PressurePSI)
                        {
                            float dp = elapsedClockSeconds * lead.EngineBrakeApplyRatePSIpS / (last - first + 1);
                            if (p + dp > train.BrakeLine3PressurePSI)
                                dp = train.BrakeLine3PressurePSI - p;
                            p += dp;
                            lead.EngineBrakeState = ValveState.Apply;
                        }
                        else if (p > train.BrakeLine3PressurePSI)
                        {
                            float dp = elapsedClockSeconds * lead.EngineBrakeReleaseRatePSIpS / (last - first + 1);
                            if (p - dp < train.BrakeLine3PressurePSI)
                                dp = p - train.BrakeLine3PressurePSI;
                            p -= dp;
                            lead.EngineBrakeState = ValveState.Release;
                        }
                        else
                            lead.EngineBrakeState = ValveState.Lap;
                        if (lead.EngineBrakeState != prevState)
                            switch (lead.EngineBrakeState)
                            {
                                case ValveState.Release: lead.SignalEvent(Event.EngineBrakePressureIncrease); break;
                                case ValveState.Apply: lead.SignalEvent(Event.EngineBrakePressureDecrease); break;
                            }
                        if (lead.BailOff || (lead.DynamicBrakeAutoBailOff && train.MUDynamicBrakePercent > 0))
                            p += 1000;
                        brakeSystem.BrakeLine3PressurePSI = p;
                    }
                }
            }
            if (sumv > 0)
                sumpv /= sumv;

            if (!train.Cars[continuousFromInclusive].BrakeSystem.FrontBrakeHoseConnected && train.Cars[continuousFromInclusive].BrakeSystem.AngleCockAOpen
                || (continuousToExclusive == train.Cars.Count || !train.Cars[continuousToExclusive].BrakeSystem.FrontBrakeHoseConnected) && train.Cars[continuousToExclusive - 1].BrakeSystem.AngleCockBOpen)
                sumpv = 0;

            // Propagate main reservoir pipe (2) data
            train.BrakeLine2PressurePSI = sumpv;
            for (int i = 0; i < train.Cars.Count; i++)
            {
                if (first <= i && i <= last || twoPipes && continuousFromInclusive <= i && i < continuousToExclusive)
                {
                    train.Cars[i].BrakeSystem.BrakeLine2PressurePSI = sumpv;
                    if (sumpv != 0 && train.Cars[i] is MSTSLocomotive)
                        (train.Cars[i] as MSTSLocomotive).MainResPressurePSI = sumpv;
                }
                else
                    train.Cars[i].BrakeSystem.BrakeLine2PressurePSI = train.Cars[i] is MSTSLocomotive ? (train.Cars[i] as MSTSLocomotive).MainResPressurePSI : 0;
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
            Car.Train.BrakeLine1PressurePSIorInHg = Math.Max(EmergResPressurePSI, 90) - FullServPressurePSI * percent / 100;
        }
    }
}
