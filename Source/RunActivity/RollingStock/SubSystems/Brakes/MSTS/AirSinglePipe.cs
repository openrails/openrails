// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

using System.IO;
using MSTS.Parsers;
using ORTS.Common;

namespace ORTS
{
    public class AirSinglePipe : MSTSBrakeSystem
    {
        protected float MaxHandbrakeForceN;
        protected float MaxBrakeForceN = 89e3f;
        float BrakePercent;  // simplistic system
        protected TrainCar Car;
        protected float HandbrakePercent;
        protected float CylPressurePSI = 64;
        protected float AutoCylPressurePSI = 64;
        protected float AuxResPressurePSI = 64;
        protected float EmergResPressurePSI = 64;
        protected float FullServPressurePSI = 50;
        protected float MaxCylPressurePSI = 64;
        protected float AuxCylVolumeRatio = 2.5f;
        protected float AuxBrakeLineVolumeRatio = 3.1f;
        protected float RetainerPressureThresholdPSI;
        protected float ReleaseRatePSIpS = 1.86f;
        protected float MaxReleaseRatePSIpS = 1.86f;
        protected float MaxApplicationRatePSIpS = .9f;
        protected float MaxAuxilaryChargingRatePSIpS = 1.684f;
        protected float EmergResChargingRatePSIpS = 1.684f;
        protected float EmergAuxVolumeRatio = 1.4f;
        public enum ValveState { Lap, Apply, Release, Emergency };
        protected ValveState TripleValveState = ValveState.Lap;

        public AirSinglePipe(TrainCar car)
        {
            Car = car;
            BrakePipeVolumeFT3 = .028f * (1 + car.LengthM);
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
            RetainerPressureThresholdPSI = thiscopy.RetainerPressureThresholdPSI;
            ReleaseRatePSIpS = thiscopy.ReleaseRatePSIpS;
            MaxReleaseRatePSIpS = thiscopy.MaxReleaseRatePSIpS;
            MaxApplicationRatePSIpS = thiscopy.MaxApplicationRatePSIpS;
            MaxAuxilaryChargingRatePSIpS = thiscopy.MaxAuxilaryChargingRatePSIpS;
            EmergResChargingRatePSIpS = thiscopy.EmergResChargingRatePSIpS;
            EmergAuxVolumeRatio = thiscopy.EmergAuxVolumeRatio;
        }

        public override string GetStatus(PressureUnit unit)
        {
            if (BrakeLine1PressurePSI < 0)
                return "";
            return string.Format("BP {0}", FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, unit, true));
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem, PressureUnit unit)
        {
            string s = string.Format(" EQ {0}", FormatStrings.FormatPressure(Car.Train.BrakeLine1PressurePSIorInHg, PressureUnit.PSI, unit, true));
            if (BrakeLine1PressurePSI >= 0)
                s += string.Format(" BC {0} BP {1}", FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, unit, false), FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, unit, false));
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += " EOT " + lastCarBrakeSystem.GetStatus(unit);
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override string[] GetDebugStatus(PressureUnit unit)
        {
            if (BrakeLine1PressurePSI < 0)
                return new string[0];
            var rv = new string[8];
            rv[0] = "1P";
            rv[1] = string.Format("BC {0}", FormatStrings.FormatPressure(CylPressurePSI, PressureUnit.PSI, unit, false));
            rv[2] = string.Format("BP {0}", FormatStrings.FormatPressure(BrakeLine1PressurePSI, PressureUnit.PSI, unit, false));
            rv[3] = string.Format("AR {0}", FormatStrings.FormatPressure(AuxResPressurePSI, PressureUnit.PSI, unit, false));
            rv[4] = string.Format("ER {0}", FormatStrings.FormatPressure(EmergResPressurePSI, PressureUnit.PSI, unit, false));
            rv[5] = string.Format("State {0}", TripleValveState);
            rv[6] = string.Empty; // Spacer because the state above needs 2 columns.
            rv[7] = HandbrakePercent > 0 ? string.Format("Handbrake {0:F0}%", HandbrakePercent) : string.Empty;
            return rv;
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
                case "wagon(maxreleaserate": MaxReleaseRatePSIpS = ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxapplicationrate": MaxApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(maxauxilarychargingrate": MaxAuxilaryChargingRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(emergencyreschargingrate": EmergResChargingRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null); break;
                case "wagon(emergencyresvolumemultiplier": EmergAuxVolumeRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(brakepipevolume": BrakePipeVolumeFT3 = stf.ReadFloatBlock(STFReader.UNITS.VolumeDefaultFT3, null); break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(BrakePercent);
            outf.Write(HandbrakePercent);
            outf.Write(ReleaseRatePSIpS);
            outf.Write(RetainerPressureThresholdPSI);
            outf.Write(AutoCylPressurePSI);
            outf.Write(AuxResPressurePSI);
            outf.Write(EmergResPressurePSI);
            outf.Write(FullServPressurePSI);
            outf.Write((int)TripleValveState);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            BrakePercent = inf.ReadSingle();
            HandbrakePercent = inf.ReadSingle();
            ReleaseRatePSIpS = inf.ReadSingle();
            RetainerPressureThresholdPSI = inf.ReadSingle();
            AutoCylPressurePSI = inf.ReadSingle();
            AuxResPressurePSI = inf.ReadSingle();
            EmergResPressurePSI = inf.ReadSingle();
            FullServPressurePSI = inf.ReadSingle();
            TripleValveState = (ValveState)inf.ReadInt32();
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, float fullServPressurePSI, bool immediateRelease)
        {
            BrakeLine1PressurePSI = Car.Train.BrakeLine1PressurePSIorInHg;
            BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
            BrakeLine3PressurePSI = 0;
            AuxResPressurePSI = BrakeLine1PressurePSI;
            EmergResPressurePSI = maxPressurePSI;
            FullServPressurePSI = fullServPressurePSI;
            AutoCylPressurePSI = (maxPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
            if (AutoCylPressurePSI > MaxCylPressurePSI)
                AutoCylPressurePSI = MaxCylPressurePSI;
            // release brakes immediately (for AI trains)
            if (immediateRelease)
                AutoCylPressurePSI = 0;
            TripleValveState = ValveState.Lap;
            HandbrakePercent = handbrakeOn ? 100 : 0;
        }

        public override void InitializeMoving () // used when initial speed > 0
        {
            BrakeLine1PressurePSI = Car.Train.BrakeLine1PressurePSIorInHg;
            BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
            BrakeLine3PressurePSI = 0;
            AuxResPressurePSI = BrakeLine1PressurePSI;
             // release brakes immediately (for AI trains)
//            if (immediateRelease)
                AutoCylPressurePSI = 0;
            TripleValveState = ValveState.Lap;
            HandbrakePercent = 0;
        }

        public override void LocoInitializeMoving() // starting conditions when starting speed > 0
        {
            BrakePercent = 0;
        }


        public override void Connect()
        {
            if (BrakeLine1PressurePSI < 0)
                BrakeLine1PressurePSI = 0;// Car.Train.BrakeLine1PressurePSI;
        }
        public override void Disconnect()
        {
            Initialize(false, 0, 0, false);
            BrakeLine1PressurePSI = -1;
            BrakeLine2PressurePSI = 0;
        }
        public override void Update(float elapsedClockSeconds)
        {
            ValveState prevTripleValueState = TripleValveState;
            if (BrakeLine1PressurePSI < FullServPressurePSI - 1)
                TripleValveState = ValveState.Emergency;
            else if (BrakeLine1PressurePSI > AuxResPressurePSI + 1)
                TripleValveState = ValveState.Release;
            else if (TripleValveState == ValveState.Emergency && BrakeLine1PressurePSI > AuxResPressurePSI)
                TripleValveState = ValveState.Release;
            else if (TripleValveState != ValveState.Emergency && BrakeLine1PressurePSI < AuxResPressurePSI - 1)
                TripleValveState = ValveState.Apply;
            else if (TripleValveState == ValveState.Apply && BrakeLine1PressurePSI >= AuxResPressurePSI)
                TripleValveState = ValveState.Lap;
            if (TripleValveState == ValveState.Apply || TripleValveState == ValveState.Emergency)
            {
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                if (AuxResPressurePSI - dp / AuxCylVolumeRatio < AutoCylPressurePSI + dp)
                    dp = (AuxResPressurePSI - AutoCylPressurePSI) * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
                if (BrakeLine1PressurePSI > AuxResPressurePSI - dp / AuxCylVolumeRatio)
                {
                    dp = (AuxResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                    TripleValveState = ValveState.Lap;
                }
                AuxResPressurePSI -= dp / AuxCylVolumeRatio;
                AutoCylPressurePSI += dp;
                if (TripleValveState == ValveState.Emergency)
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
                float threshold = RetainerPressureThresholdPSI;
                if (Car.Simulator.Settings.GraduatedRelease)
                {
                    float t = (EmergResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
                    if (threshold < t)
                        threshold = t;
                }
                if (AutoCylPressurePSI > threshold)
                {
                    AutoCylPressurePSI -= elapsedClockSeconds * ReleaseRatePSIpS;
                    if (AutoCylPressurePSI < threshold)
                        AutoCylPressurePSI = threshold;
                }
                if (!Car.Simulator.Settings.GraduatedRelease && AuxResPressurePSI < EmergResPressurePSI && AuxResPressurePSI < BrakeLine1PressurePSI)
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
                if (AuxResPressurePSI < BrakeLine1PressurePSI)
                {
#if false
                    float dp = .1f * (BrakeLine1PressurePSI - AuxResPressurePSI);
                    if (dp > 1)
                        dp = .5f;
                    dp *= elapsedClockSeconds * MaxAuxilaryChargingRate;
#else
                    float dp = elapsedClockSeconds * MaxAuxilaryChargingRatePSIpS;
#endif
                    if (AuxResPressurePSI + dp > BrakeLine1PressurePSI - dp * AuxBrakeLineVolumeRatio)
                        dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                    AuxResPressurePSI += dp;
                    BrakeLine1PressurePSI -= dp * AuxBrakeLineVolumeRatio;
                }
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
            float f = MaxBrakeForceN * CylPressurePSI / MaxCylPressurePSI;
            if (f < MaxHandbrakeForceN * HandbrakePercent / 100)
                f = MaxHandbrakeForceN * HandbrakePercent / 100;
            Car.BrakeForceN = f;
            //Car.FrictionForceN += f;
        }
        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
            if (lead == null)
                SetUniformBrakePressures();
            else
                PropagateBrakeLinePressures(elapsedClockSeconds, lead, false);
        }
        protected void SetUniformBrakePressures()
        {
            Train train = Car.Train;
            foreach (TrainCar car in Car.Train.Cars)
            {
                if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                    continue;
                car.BrakeSystem.BrakeLine1PressurePSI = train.BrakeLine1PressurePSIorInHg;
                car.BrakeSystem.BrakeLine2PressurePSI = train.BrakeLine2PressurePSI;
                car.BrakeSystem.BrakeLine3PressurePSI = 0;
            }
        }
        protected void PropagateBrakeLinePressures(float elapsedClockSeconds, MSTSLocomotive lead, bool twoPipes)
        {
            Train train = Car.Train;
            if (lead.BrakePipeChargingRatePSIpS > 1000)
            {   // pressure gradiant disabled
                foreach (TrainCar car in train.Cars)
                    if (car.BrakeSystem.BrakeLine1PressurePSI >= 0)
                        car.BrakeSystem.BrakeLine1PressurePSI = train.BrakeLine1PressurePSIorInHg;
            }
            else
            {   // approximate pressure gradiant in line1
                float serviceTimeFactor = lead.BrakeServiceTimeFactorS;
                if (lead.TrainBrakeController != null && lead.TrainBrakeController.EmergencyBraking)
                    serviceTimeFactor = lead.BrakeEmergencyTimeFactorS;
                int nSteps = (int)(elapsedClockSeconds * 2 / lead.BrakePipeTimeFactorS + 1);
                float dt = elapsedClockSeconds / nSteps;
                for (int i = 0; i < nSteps; i++)
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
                        lead.MainResPressurePSI -= dp * lead.BrakeSystem.BrakePipeVolumeFT3 / lead.MainResVolumeFT3;
                    }
                    else if (lead.BrakeSystem.BrakeLine1PressurePSI > train.BrakeLine1PressurePSIorInHg)
                        lead.BrakeSystem.BrakeLine1PressurePSI *= (1 - dt / serviceTimeFactor);
                    TrainCar car0 = Car.Train.Cars[0];
                    float p0 = car0.BrakeSystem.BrakeLine1PressurePSI;
                    foreach (TrainCar car in train.Cars)
                    {
                        float p1 = car.BrakeSystem.BrakeLine1PressurePSI;
                        if (p0 >= 0 && p1 >= 0)
                        {
                            float dp = dt * (p1 - p0) / lead.BrakePipeTimeFactorS;
                            car.BrakeSystem.BrakeLine1PressurePSI -= dp;
                            car0.BrakeSystem.BrakeLine1PressurePSI += dp;
                        }
                        p0 = p1;
                        car0 = car;
                    }
                }
            }
            int first = -1;
            int last = -1;
            train.FindLeadLocomotives(ref first, ref last);
            float sumpv = 0;
            float sumv = 0;
            for (int i = 0; i < train.Cars.Count; i++)
            {
                BrakeSystem brakeSystem = train.Cars[i].BrakeSystem;
                if (brakeSystem.BrakeLine1PressurePSI < 0)
                    continue;
                if (i < first || i > last)
                {
                    brakeSystem.BrakeLine3PressurePSI = 0;
                    if (twoPipes)
                    {
                        sumv += brakeSystem.BrakePipeVolumeFT3;
                        sumpv += brakeSystem.BrakePipeVolumeFT3 * brakeSystem.BrakeLine2PressurePSI;
                    }
                }
                else
                {
                    float p = brakeSystem.BrakeLine3PressurePSI;
                    if (p > 1000)
                        p -= 1000;
                    AirSinglePipe.ValveState prevState = lead.EngineBrakeState;
                    if (p < train.BrakeLine3PressurePSI)
                    {
                        float dp = elapsedClockSeconds * lead.EngineBrakeApplyRatePSIpS / (last - first + 1);
                        if (p + dp > train.BrakeLine3PressurePSI)
                            dp = train.BrakeLine3PressurePSI - p;
                        p += dp;
                        lead.EngineBrakeState = AirSinglePipe.ValveState.Apply;
                    }
                    else if (p > train.BrakeLine3PressurePSI)
                    {
                        float dp = elapsedClockSeconds * lead.EngineBrakeReleaseRatePSIpS / (last - first + 1);
                        if (p - dp < train.BrakeLine3PressurePSI)
                            dp = p - train.BrakeLine3PressurePSI;
                        p -= dp;
                        lead.EngineBrakeState = AirSinglePipe.ValveState.Release;
                    }
                    else
                        lead.EngineBrakeState = AirSinglePipe.ValveState.Lap;
                    if (lead.EngineBrakeState != prevState)
                        switch (lead.EngineBrakeState)
                        {
                            case AirSinglePipe.ValveState.Release: lead.SignalEvent(Event.EngineBrakePressureIncrease); break;
                            case AirSinglePipe.ValveState.Apply: lead.SignalEvent(Event.EngineBrakePressureDecrease); break;
                        }
                    if (lead.BailOff || (lead.DynamicBrakeAutoBailOff && Car.Train.MUDynamicBrakePercent > 0))
                        p += 1000;
                    brakeSystem.BrakeLine3PressurePSI = p;
                    sumv += brakeSystem.BrakePipeVolumeFT3;
                    sumpv += brakeSystem.BrakePipeVolumeFT3 * brakeSystem.BrakeLine2PressurePSI;
                    MSTSLocomotive eng = (MSTSLocomotive)Car.Train.Cars[i];
                    if (eng != null)
                    {
                        sumv += eng.MainResVolumeFT3;
                        sumpv += eng.MainResVolumeFT3 * eng.MainResPressurePSI;
                    }
                }
            }
            if (sumv > 0)
                sumpv /= sumv;
            train.BrakeLine2PressurePSI = sumpv;
            for (int i = 0; i < train.Cars.Count; i++)
            {
                TrainCar car = train.Cars[i];
                if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
                    continue;
                if (i < first || i > last)
                {
                    car.BrakeSystem.BrakeLine2PressurePSI = twoPipes ? sumpv : 0;
                }
                else
                {
                    car.BrakeSystem.BrakeLine2PressurePSI = sumpv;
                    MSTSLocomotive eng = (MSTSLocomotive)car;
                    if (eng != null)
                        eng.MainResPressurePSI = sumpv;
                }
            }
        }

        public override void SetRetainer(RetainerSetting setting)
        {
            switch (setting)
            {
                case RetainerSetting.Exhaust:
                    RetainerPressureThresholdPSI = 0;
                    ReleaseRatePSIpS = MaxReleaseRatePSIpS;
                    break;
                case RetainerSetting.HighPressure:
                    RetainerPressureThresholdPSI = 20;
                    ReleaseRatePSIpS = (50 - 20) / 90f;
                    break;
                case RetainerSetting.LowPressure:
                    RetainerPressureThresholdPSI = 10;
                    ReleaseRatePSIpS = (50 - 10) / 60f;
                    break;
                case RetainerSetting.SlowDirect:
                    RetainerPressureThresholdPSI = 0;
                    ReleaseRatePSIpS = (50 - 10) / 86f;
                    break;
            }
        }

        public override void SetHandbrakePercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            HandbrakePercent = percent;
        }

        public override void AISetPercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            BrakePercent = percent;
            Car.Train.BrakeLine1PressurePSIorInHg = 90 - 26 * BrakePercent / 100;
        }
    }
}
