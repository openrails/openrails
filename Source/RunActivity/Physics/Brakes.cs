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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MSTS;
using System.IO;

namespace ORTS
{
    public abstract class BrakeSystem
    {
        public float BrakeLine1PressurePSI = 90;    // main trainline pressure at this car
        public float BrakeLine2PressurePSI = 0;     // main reservoir equalization pipe pressure
        public float BrakeLine3PressurePSI = 0;     // engine brake cylinder equalization pipe pressure
        public float BrakePipeVolumeFT3 = .5f;      // volume of a single brake line

        public abstract void AISetPercent(float percent);

        public abstract string GetStatus();
        public abstract string GetFullStatus(BrakeSystem lastCarBrakeSystem);
        public abstract string[] GetDebugStatus();
        public abstract float GetCylPressurePSI();

        public abstract void Save(BinaryWriter outf);

        public abstract void Restore( BinaryReader inf );

        public abstract void PropagateBrakePressure(float elapsedClockSeconds);

        public abstract void Initialize(bool handbrakeOn, float maxPressurePSI, bool immediateRelease);
        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void SetHandbrakePercent(float percent);
        public abstract void SetRetainer(RetainerSetting setting);
    }

    public enum RetainerSetting { Exhaust, HighPressure, LowPressure, SlowDirect };

    public abstract class MSTSBrakeSystem: BrakeSystem
    {
        public static BrakeSystem Create(string type, TrainCar car)
        {
            if (type != null && type.StartsWith("vacuum"))
                return new VacuumSinglePipe(car);
            else if (type != null && type == "ep")
                return new EPBrakeSystem(car);
            else if (type != null && type == "air_twin_pipe")
                return new AirTwinPipe(car);
            else
                return new AirSinglePipe(car);
        }

        public abstract void Parse(string lowercasetoken, STFReader stf);

        public abstract void Update(float elapsedClockSeconds);

        public abstract void InitializeFromCopy(BrakeSystem copy);

    }

    public class AirSinglePipe : MSTSBrakeSystem
    {
        protected float MaxHandbrakeForceN = 0;
        protected float MaxBrakeForceN = 89e3f;
        float BrakePercent = 0;  // simplistic system
        protected TrainCar Car;
        protected float HandbrakePercent = 0;
        protected float CylPressurePSI = 64;
        protected float AutoCylPressurePSI = 64;
        protected float AuxResPressurePSI = 64;
        protected float EmergResPressurePSI = 64;
        protected float MaxCylPressurePSI = 64;
        protected float AuxCylVolumeRatio = 2.5f;
        protected float AuxBrakeLineVolumeRatio = 3.1f;
        protected float RetainerPressureThresholdPSI = 0;
        protected float ReleaseRate = 1.86f;
        protected float MaxReleaseRate = 1.86f;
        protected float MaxApplicationRate = .9f;
        protected float MaxAuxilaryChargingRate = 1.684f;
        protected float EmergResChargingRate = 1.684f;
        protected float EmergAuxVolumeRatio = 1.4f;
        public enum ValveState { Lap, Apply, Release, Emergency };
        protected ValveState TripleValveState = ValveState.Lap;

        public AirSinglePipe(TrainCar car)
        {
            Car = car;
            BrakePipeVolumeFT3 = .028f * (1 + car.Length);
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
            ReleaseRate = thiscopy.ReleaseRate;
            MaxReleaseRate = thiscopy.MaxReleaseRate;
            MaxApplicationRate = thiscopy.MaxApplicationRate;
            MaxAuxilaryChargingRate = thiscopy.MaxAuxilaryChargingRate;
            EmergResChargingRate = thiscopy.EmergResChargingRate;
            EmergAuxVolumeRatio = thiscopy.EmergAuxVolumeRatio;
        }

        public override string GetStatus()
        {
            if (BrakeLine1PressurePSI < 0)
                return "";
            return string.Format("BP {0:F0}", BrakeLine1PressurePSI);
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem)
        {
            string s= string.Format(" EQ {0:F0} psi", Car.Train.BrakeLine1PressurePSI);
            if (BrakeLine1PressurePSI >= 0)
                s+= string.Format(" BC {0:F0} BP {1:F0}", CylPressurePSI, BrakeLine1PressurePSI);
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += " EOT " + lastCarBrakeSystem.GetStatus();
            if (HandbrakePercent > 0)
                s+= string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override string[] GetDebugStatus()
        {
            if (BrakeLine1PressurePSI < 0)
                return new string[0];
            var rv = new string[8];
            rv[0] = "1P";
            rv[1] = string.Format("BC {0:F0}", CylPressurePSI);
            rv[2] = string.Format("BP {0:F0}", BrakeLine1PressurePSI);
            rv[3] = string.Format("AR {0:F0}", AuxResPressurePSI);
            rv[4] = string.Format("ER {0:F0}", EmergResPressurePSI);
            rv[5] = string.Format("State {0}", TripleValveState);
            rv[6] = string.Empty; // Spacer because the state above needs 2 columns.
            rv[7] = HandbrakePercent > 0 ? string.Format("Handbrake {0:F0}%", HandbrakePercent) : string.Empty;
            return rv;
        }

        public override float GetCylPressurePSI()
        {
                return CylPressurePSI;
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(maxhandbrakeforce": MaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(maxbrakeforce": MaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxCylPressurePSI = AutoCylPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(triplevalveratio": AuxCylVolumeRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(maxreleaserate": MaxReleaseRate = ReleaseRate = stf.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                case "wagon(maxapplicationrate": MaxApplicationRate = stf.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                case "wagon(maxauxilarychargingrate": MaxAuxilaryChargingRate = stf.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                case "wagon(emergencyreschargingrate": EmergResChargingRate = stf.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                case "wagon(emergencyresvolumemultiplier": EmergAuxVolumeRatio = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(brakepipevolume": BrakePipeVolumeFT3 = stf.ReadFloatBlock(STFReader.UNITS.Any, null); break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(BrakePercent);
            outf.Write(HandbrakePercent);
            outf.Write(ReleaseRate);
            outf.Write(RetainerPressureThresholdPSI);
            outf.Write(AutoCylPressurePSI);
            outf.Write(AuxResPressurePSI);
            outf.Write(EmergResPressurePSI);
            outf.Write((int)TripleValveState);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            BrakePercent = inf.ReadSingle();
            HandbrakePercent = inf.ReadSingle();
            ReleaseRate = inf.ReadSingle();
            RetainerPressureThresholdPSI = inf.ReadSingle();
            AutoCylPressurePSI = inf.ReadSingle();
            AuxResPressurePSI = inf.ReadSingle();
            EmergResPressurePSI = inf.ReadSingle();
            TripleValveState = (ValveState)inf.ReadInt32();
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI, bool immediateRelease)
        {
            BrakeLine1PressurePSI = Car.Train.BrakeLine1PressurePSI;
            BrakeLine2PressurePSI = Car.Train.BrakeLine2PressurePSI;
            BrakeLine3PressurePSI = 0;
            AuxResPressurePSI = BrakeLine1PressurePSI;
            EmergResPressurePSI = maxPressurePSI;
            AutoCylPressurePSI = (maxPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
            if (AutoCylPressurePSI > MaxCylPressurePSI)
                AutoCylPressurePSI = MaxCylPressurePSI;
            // release brakes immediately (for AI trains)
            if (immediateRelease)
                AutoCylPressurePSI = 0;
            TripleValveState = ValveState.Lap;
            HandbrakePercent = handbrakeOn ? 100 : 0;
        }
        public override void Connect()
        {
            if (BrakeLine1PressurePSI < 0)
                BrakeLine1PressurePSI = 0;// Car.Train.BrakeLine1PressurePSI;
        }
        public override void Disconnect()
        {
            Initialize(false, 0, false);
            BrakeLine1PressurePSI = -1;
            BrakeLine2PressurePSI = 0;
        }
        public override void Update(float elapsedClockSeconds)
        {
            ValveState prevTripleValueState = TripleValveState;
            if (BrakeLine1PressurePSI < AuxResPressurePSI - 10)
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
                float dp = elapsedClockSeconds * MaxApplicationRate;
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
                    dp = elapsedClockSeconds * MaxApplicationRate;
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
                    AutoCylPressurePSI -= elapsedClockSeconds * ReleaseRate;
                    if (AutoCylPressurePSI < threshold)
                        AutoCylPressurePSI = threshold;
                }
				if (!Car.Simulator.Settings.GraduatedRelease && AuxResPressurePSI < EmergResPressurePSI && AuxResPressurePSI < BrakeLine1PressurePSI)
                {
                    float dp = elapsedClockSeconds * EmergResChargingRate;
                    if (EmergResPressurePSI - dp < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                        dp = (EmergResPressurePSI - AuxResPressurePSI) / (1 + EmergAuxVolumeRatio);
                    if (BrakeLine1PressurePSI < AuxResPressurePSI + dp * EmergAuxVolumeRatio)
                        dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / EmergAuxVolumeRatio;
                    EmergResPressurePSI -= dp;
                    AuxResPressurePSI += dp * EmergAuxVolumeRatio;
                }
                if (AuxResPressurePSI > EmergResPressurePSI)
                {
                    float dp = elapsedClockSeconds * EmergResChargingRate;
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
                    float dp = elapsedClockSeconds * MaxAuxilaryChargingRate;
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
                    case ValveState.Apply: case ValveState.Emergency: Car.SignalEvent(Event.TrainBrakePressureIncrease); break;
                }
            }
            if (BrakeLine3PressurePSI >= 1000)
            {
                BrakeLine3PressurePSI -= 1000;
                AutoCylPressurePSI -= MaxReleaseRate * elapsedClockSeconds;
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
                car.BrakeSystem.BrakeLine1PressurePSI = train.BrakeLine1PressurePSI;
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
                        car.BrakeSystem.BrakeLine1PressurePSI = train.BrakeLine1PressurePSI;
            }
            else
            {   // approximate pressure gradiant in line1
                float serviceTimeFactor = lead.BrakeServiceTimeFactorS;
                if (lead.TrainBrakeController != null && lead.TrainBrakeController.GetIsEmergency())
                    serviceTimeFactor = lead.BrakeEmergencyTimeFactorS;
                int nSteps = (int)(elapsedClockSeconds * 2 / lead.BrakePipeTimeFactorS + 1);
                float dt = elapsedClockSeconds / nSteps;
                for (int i = 0; i < nSteps; i++)
                {
                    if (lead.BrakeSystem.BrakeLine1PressurePSI < train.BrakeLine1PressurePSI)
                    {
                        float dp = dt * lead.BrakePipeChargingRatePSIpS;
                        if (lead.BrakeSystem.BrakeLine1PressurePSI + dp > train.BrakeLine1PressurePSI)
                            dp = train.BrakeLine1PressurePSI - lead.BrakeSystem.BrakeLine1PressurePSI;
                        if (lead.BrakeSystem.BrakeLine1PressurePSI + dp > lead.MainResPressurePSI)
                            dp = lead.MainResPressurePSI - lead.BrakeSystem.BrakeLine1PressurePSI;
                        if (dp < 0)
                            dp = 0;
                        lead.BrakeSystem.BrakeLine1PressurePSI += dp;
                        lead.MainResPressurePSI -= dp * lead.BrakeSystem.BrakePipeVolumeFT3 / lead.MainResVolumeFT3;
                    }
                    else if (lead.BrakeSystem.BrakeLine1PressurePSI > train.BrakeLine1PressurePSI)
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
                    ReleaseRate = MaxReleaseRate;
                    break;
                case RetainerSetting.HighPressure:
                    RetainerPressureThresholdPSI = 20;
                    ReleaseRate = (50 - 20) / 90f;
                    break;
                case RetainerSetting.LowPressure:
                    RetainerPressureThresholdPSI = 10;
                    ReleaseRate = (50 - 10) / 60f;
                    break;
                case RetainerSetting.SlowDirect:
                    RetainerPressureThresholdPSI = 0;
                    ReleaseRate = (50 - 10) / 86f;
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
            Car.Train.BrakeLine1PressurePSI = 90 - 26 * BrakePercent / 100;
        }
    }
    public class AirTwinPipe : AirSinglePipe
    {
        public AirTwinPipe(TrainCar car)
            : base(car)
        {
        }

        public override void Update(float elapsedClockSeconds)
        {
            ValveState prevTripleValueState = TripleValveState;
            float threshold = RetainerPressureThresholdPSI;
            float t = (EmergResPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
            if (threshold < t)
                threshold = t;
            if (AutoCylPressurePSI > threshold)
            {
                TripleValveState = ValveState.Release;
                AutoCylPressurePSI -= elapsedClockSeconds * ReleaseRate;
                if (AutoCylPressurePSI < threshold)
                    AutoCylPressurePSI = threshold;
            }
            else if (AutoCylPressurePSI < threshold)
            {
                TripleValveState = ValveState.Apply;
                float dp = elapsedClockSeconds * MaxApplicationRate;
                if (AuxResPressurePSI - dp / AuxCylVolumeRatio < AutoCylPressurePSI + dp)
                    dp = (AuxResPressurePSI - AutoCylPressurePSI) * AuxCylVolumeRatio / (1 + AuxCylVolumeRatio);
                if (threshold < AutoCylPressurePSI + dp)
                    dp = threshold - AutoCylPressurePSI;
                AuxResPressurePSI -= dp / AuxCylVolumeRatio;
                AutoCylPressurePSI += dp;
            }
            else
                TripleValveState = ValveState.Lap;
            if (BrakeLine1PressurePSI > EmergResPressurePSI)
            {
                float dp = elapsedClockSeconds * EmergResChargingRate;
                if (EmergResPressurePSI + dp > BrakeLine1PressurePSI - dp * EmergAuxVolumeRatio * AuxBrakeLineVolumeRatio)
                    dp = (BrakeLine1PressurePSI - EmergResPressurePSI) / (1 + EmergAuxVolumeRatio * AuxBrakeLineVolumeRatio);
                EmergResPressurePSI += dp;
                BrakeLine1PressurePSI -= dp * EmergAuxVolumeRatio * AuxBrakeLineVolumeRatio;
                TripleValveState = ValveState.Release;
            }
            if (AuxResPressurePSI < BrakeLine2PressurePSI)
            {
                float dp = elapsedClockSeconds * MaxAuxilaryChargingRate;
                if (AuxResPressurePSI + dp > BrakeLine2PressurePSI - dp * AuxBrakeLineVolumeRatio)
                    dp = (BrakeLine2PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                AuxResPressurePSI += dp;
                BrakeLine2PressurePSI -= dp * AuxBrakeLineVolumeRatio;
            }
            if (TripleValveState != prevTripleValueState)
            {
                switch (TripleValveState)
                {
                    case ValveState.Release: Car.SignalEvent(Event.TrainBrakePressureIncrease); break;
                    case ValveState.Apply: case ValveState.Emergency: Car.SignalEvent(Event.TrainBrakePressureDecrease); break;
                }
            }
            if (BrakeLine3PressurePSI >= 1000)
            {
                BrakeLine3PressurePSI -= 1000;
                AutoCylPressurePSI -= MaxReleaseRate * elapsedClockSeconds;
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

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem)
        {
            string s = string.Format(" EQ {0:F0} psi", Car.Train.BrakeLine1PressurePSI);
            if (BrakeLine1PressurePSI >= 0)
                s += string.Format(" BC {0:F0} BP {1:F0}", CylPressurePSI, BrakeLine1PressurePSI);
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += " EOT " + lastCarBrakeSystem.GetStatus();
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override string[] GetDebugStatus()
        {
            if (BrakeLine1PressurePSI < 0)
                return new string[0];
            var rv = new string[9];
            rv[0] = "2P";
            rv[1] = string.Format("BC {0:F0}", CylPressurePSI);
            rv[2] = string.Format("BP {0:F0}", BrakeLine1PressurePSI);
            rv[3] = string.Format("AR {0:F0}", AuxResPressurePSI);
            rv[4] = string.Format("ER {0:F0}", EmergResPressurePSI);
            rv[5] = string.Format("MRP {0:F0}", BrakeLine2PressurePSI);
            rv[6] = string.Format("State {0}", TripleValveState);
            rv[7] = string.Empty; // Spacer because the state above needs 2 columns.
            rv[8] = HandbrakePercent > 0 ? string.Format("Handbrake {0:F0}%", HandbrakePercent) : string.Empty;
            return rv;
        }
        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
            if (lead == null)
                SetUniformBrakePressures();
            else
                PropagateBrakeLinePressures(elapsedClockSeconds, lead, true);
        }
    }
    public class EPBrakeSystem : AirSinglePipe
    {
        ValveState epState = ValveState.Lap;

        public EPBrakeSystem(TrainCar car) : base(car)
        {
        }

        public override void Update(float elapsedClockSeconds)
        {
            ValveState prevState = epState;
            RetainerPressureThresholdPSI = Car.Train.BrakeLine4PressurePSI;
            if (AutoCylPressurePSI > RetainerPressureThresholdPSI)
            {
                epState = ValveState.Release;
                if (TripleValveState==ValveState.Lap)
                    TripleValveState = ValveState.Release;
            }
            base.Update(elapsedClockSeconds);
            if (AutoCylPressurePSI < RetainerPressureThresholdPSI)
            {
                epState = ValveState.Apply;
                float dp = elapsedClockSeconds * MaxApplicationRate;
                if (BrakeLine2PressurePSI - dp < AutoCylPressurePSI + dp)
                    dp = (BrakeLine2PressurePSI - AutoCylPressurePSI) * .5f;
                if (RetainerPressureThresholdPSI < AutoCylPressurePSI + dp)
                    dp = RetainerPressureThresholdPSI - AutoCylPressurePSI;
                BrakeLine2PressurePSI -= dp;
                AutoCylPressurePSI += dp;
            }
            if (epState != prevState)
            {
                switch (epState)
                {
                    case ValveState.Release: Car.SignalEvent(Event.TrainBrakePressureDecrease); break;
                    case ValveState.Apply: Car.SignalEvent(Event.TrainBrakePressureIncrease); break;
                }
            }
            if (BrakeLine3PressurePSI >= 1000)
            {
                BrakeLine3PressurePSI -= 1000;
                AutoCylPressurePSI -= MaxReleaseRate * elapsedClockSeconds;
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

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem)
        {
            string s = string.Format(" BC {0:F0} psi", CylPressurePSI);
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override string[] GetDebugStatus()
        {
            if (BrakeLine1PressurePSI < 0)
                return new string[0];
            var rv = new string[9];
            rv[0] = "EP";
            rv[1] = string.Format("BC {0:F0}", CylPressurePSI);
            rv[2] = string.Format("MRP {0:F0}", BrakeLine2PressurePSI);
            rv[3] = string.Format("AR {0:F0}", AuxResPressurePSI);
            rv[4] = string.Format("ER {0:F0}", EmergResPressurePSI);
            rv[5] = string.Format("BP {0:F0}", BrakeLine1PressurePSI);
            rv[6] = string.Format("State {0}", TripleValveState);
            rv[7] = string.Empty; // Spacer because the state above needs 2 columns.
            rv[8] = HandbrakePercent > 0 ? string.Format("Handbrake {0:F0}%", HandbrakePercent) : string.Empty;
            return rv;
        }
        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            MSTSLocomotive lead = (MSTSLocomotive)Car.Train.LeadLocomotive;
            if (lead == null)
                SetUniformBrakePressures();
            else
                PropagateBrakeLinePressures(elapsedClockSeconds, lead, true);
        }
    }

    public class VacuumSinglePipe : MSTSBrakeSystem
    {
        const float OneAtmospherePSIA = 15;
        const float OneAtmosphereInHg = 30;
        float MaxHandbrakeForceN = 0;
        float MaxBrakeForceN = 89e3f;
        float MaxForcePressurePSI = 21 * OneAtmospherePSIA / OneAtmosphereInHg;// relative pressure difference for max brake force
        TrainCar Car;
        float HandbrakePercent = 0;
        float CylPressurePSIA = 0;
        float VacResPressurePSIA = 0; // vacuum reservior pressure with piston in released position
        // defaults based on information in http://www.lmsca.org.uk/lms-coaches/LMSRAVB.pdf
        int NumCylinders = 2;
        // brake cylinder volume with piston in applied position
        float CylVol = (float)((18 / 2) * (18 / 2) * 4.5 * Math.PI);
        // vacuum reservior volume with piston in released position
        float VacResVol = (float)((24 / 2) * (24 / 2) * 16 * Math.PI);
        float PipeVol = (float)((2 / 2) * (2 / 2) * 70 * 12 * Math.PI);
        // volume units need to be consistent but otherwise don't matter, defaults are cubic inches
        bool HasDirectAdmissionValue = false;
        float MaxReleaseRatePSIpS = 2.5f;
        float MaxApplicationRatePSIpS = 2.5f;
        float PipeTimeFactorS = .003f; // copied from air single pipe, probably not accurate
        float ReleaseTimeFactorS = 1.009f; // copied from air single pipe, but close to modern ejector data
        float ApplyChargingRatePSIpS = 4;

        public VacuumSinglePipe( TrainCar car )
        {
            Car = car;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            VacuumSinglePipe thiscopy = (VacuumSinglePipe)copy;
            MaxBrakeForceN = thiscopy.MaxBrakeForceN;
            MaxHandbrakeForceN = thiscopy.MaxHandbrakeForceN;
            MaxForcePressurePSI = thiscopy.MaxForcePressurePSI;
            MaxReleaseRatePSIpS = thiscopy.MaxReleaseRatePSIpS;
            MaxApplicationRatePSIpS = thiscopy.MaxApplicationRatePSIpS;
            ApplyChargingRatePSIpS = thiscopy.ApplyChargingRatePSIpS;
            PipeTimeFactorS = thiscopy.PipeTimeFactorS;
            ReleaseTimeFactorS = thiscopy.ReleaseTimeFactorS;
            NumCylinders = thiscopy.NumCylinders;
            CylVol = thiscopy.CylVol;
            PipeVol = thiscopy.PipeVol;
            VacResVol = thiscopy.VacResVol;
            HasDirectAdmissionValue = thiscopy.HasDirectAdmissionValue;
        }

        // convert vacuum in inhg to pressure in psia
        float V2P(float v)
        {
            return OneAtmospherePSIA * (1 - v / OneAtmosphereInHg);
        }
        // convert pressure in psia to vacuum in inhg
        float P2V(float p)
        {
            return OneAtmosphereInHg * (1 - p / OneAtmospherePSIA);
        }
        // return vacuum reservior pressure adjusted for piston movement
        float VacResPressureAdjPSIA()
        {
            if (VacResPressurePSIA >= CylPressurePSIA)
                return VacResPressurePSIA;
            float p = VacResPressurePSIA / (1 - CylVol / VacResVol);
            return p < CylPressurePSIA ? p : CylPressurePSIA;
        }

        public override string GetStatus()
        {
            if (BrakeLine1PressurePSI < 0)
                return "";
            return string.Format(" BP {0:F0}", P2V(BrakeLine1PressurePSI));
        }

        public override string GetFullStatus(BrakeSystem lastCarBrakeSystem)
        {
            string s = string.Format(" V {0:F0} inHg", Car.Train.BrakeLine1PressurePSI);
            if (lastCarBrakeSystem != null && lastCarBrakeSystem != this)
                s += " EOT " + lastCarBrakeSystem.GetStatus();
            if (HandbrakePercent > 0)
                s += string.Format(" Handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override string[] GetDebugStatus()
        {
            if (BrakeLine1PressurePSI < 0)
                return new string[0];
            var rv = new string[6];
            rv[0] = "V";
            rv[1] = string.Format("BC {0:F0}", P2V(CylPressurePSIA));
            rv[2] = string.Format("VR {0:F0}", P2V(VacResPressureAdjPSIA()));
            rv[3] = string.Format("BP {0:F0}", P2V(BrakeLine1PressurePSI));
            rv[4] = string.Empty; // Spacer because the state above needs 2 columns.
            rv[5] = HandbrakePercent > 0 ? string.Format("Handbrake {0:F0}%", HandbrakePercent) : string.Empty;
            return rv;
        }

        public override float GetCylPressurePSI()
        {
            return 0;
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(maxhandbrakeforce": MaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(maxbrakeforce": MaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxForcePressurePSI = stf.ReadFloatBlock(STFReader.UNITS.Any, null) * OneAtmospherePSIA / OneAtmosphereInHg; break;
                case "wagon(maxreleaserate": MaxReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.Any, null) * OneAtmospherePSIA / OneAtmosphereInHg; break;
                case "wagon(maxapplicationrate": ApplyChargingRatePSIpS= MaxApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.Any, null) * OneAtmospherePSIA/ OneAtmosphereInHg; break;
                case "engine(pipetimefactor": PipeTimeFactorS = stf.ReadFloatBlock(STFReader.UNITS.Any, null); break;
                case "engine(releasetimefactor": ReleaseTimeFactorS = stf.ReadFloatBlock(STFReader.UNITS.Any, null); break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(CylPressurePSIA);
            outf.Write(VacResPressurePSIA);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            CylPressurePSIA = inf.ReadSingle();
            VacResPressurePSIA = inf.ReadSingle();
        }

        public override void Initialize(bool handbrakeOn, float maxVacuumInHg, bool immediateRelease)
        {
            CylPressurePSIA = BrakeLine1PressurePSI = V2P(Car.Train.BrakeLine1PressurePSI);
            VacResPressurePSIA = V2P(maxVacuumInHg);
        }
        public override void Connect()
        {
            if (BrakeLine1PressurePSI < 0)
                BrakeLine1PressurePSI = OneAtmospherePSIA;
        }
        public override void Disconnect()
        {
            BrakeLine1PressurePSI = -1;
            CylPressurePSIA = OneAtmospherePSIA;
            VacResPressurePSIA = OneAtmospherePSIA;
        }
        public override void Update(float elapsedClockSeconds)
        {
            if (BrakeLine1PressurePSI < 0)
                return; // pipes not connected
            if (BrakeLine1PressurePSI < VacResPressurePSIA)
            {
                float dp = elapsedClockSeconds * MaxReleaseRatePSIpS * CylVol / VacResVol;
                float vr = NumCylinders * VacResVol / PipeVol;
                if (VacResPressurePSIA - dp < BrakeLine1PressurePSI + dp * vr)
                    dp = (VacResPressurePSIA - BrakeLine1PressurePSI) / (1 + vr);
                VacResPressurePSIA -= dp;
                BrakeLine1PressurePSI += dp * vr;
                CylPressurePSIA = VacResPressurePSIA;
            }
            else if (BrakeLine1PressurePSI < CylPressurePSIA)
            {
                float dp = elapsedClockSeconds * MaxReleaseRatePSIpS;
                float vr = NumCylinders * CylVol / PipeVol;
                if (CylPressurePSIA - dp < BrakeLine1PressurePSI + dp * vr)
                    dp = (CylPressurePSIA - BrakeLine1PressurePSI) / (1 + vr);
                CylPressurePSIA -= dp;
                BrakeLine1PressurePSI += dp * vr;
            }
            else if (BrakeLine1PressurePSI > CylPressurePSIA)
            {
                float dp = elapsedClockSeconds * MaxApplicationRatePSIpS;
                float vr = 2 * CylVol / PipeVol;
                if (CylPressurePSIA + dp > BrakeLine1PressurePSI - dp * vr)
                    dp = (BrakeLine1PressurePSI - CylPressurePSIA) / (1 + vr);
                CylPressurePSIA += dp;
                if (!HasDirectAdmissionValue)
                    BrakeLine1PressurePSI -= dp * vr;
            }
            float vrp = VacResPressureAdjPSIA();
            float f = CylPressurePSIA <= vrp ? 0 : MaxBrakeForceN * (CylPressurePSIA - vrp) / MaxForcePressurePSI;
            if (f < MaxHandbrakeForceN * HandbrakePercent / 100)
                f = MaxHandbrakeForceN * HandbrakePercent / 100;
            Car.BrakeForceN = f;

// Temporary patch until problem with vacuum brakes is solved
// This will immediately fully release the brakes
	    if (Car.Train.AITrainBrakePercent == 0)
	    {
		    CylPressurePSIA = 0;
		    Car.BrakeForceN = 0;
	    }
// End of patch

        }

        public override void PropagateBrakePressure(float elapsedClockSeconds)
        {
            Train train = Car.Train;
            // train.BrakeLine1PressurePSI is really vacuum in inHg
            float psia = V2P(train.BrakeLine1PressurePSI);
            int nSteps = (int)(elapsedClockSeconds * 2 / PipeTimeFactorS + 1);
            float dt = elapsedClockSeconds / nSteps;
            for (int i = 0; i < nSteps; i++)
            {
                if (BrakeLine1PressurePSI < psia)
                {
                    float dp = dt * ApplyChargingRatePSIpS;
                    if (BrakeLine1PressurePSI + dp > psia)
                        dp = psia - BrakeLine1PressurePSI;
                    BrakeLine1PressurePSI += dp;
                }
                else if (BrakeLine1PressurePSI > psia)
                {
                    BrakeLine1PressurePSI *= (1 - dt / ReleaseTimeFactorS);
                    if (BrakeLine1PressurePSI < psia)
                        BrakeLine1PressurePSI = psia;
                }
                TrainCar car0 = Car.Train.Cars[0];
                float p0 = car0.BrakeSystem.BrakeLine1PressurePSI;
                foreach (TrainCar car in train.Cars)
                {
                    float p1 = car.BrakeSystem.BrakeLine1PressurePSI;
                    if (p0 >= 0 && p1 >= 0)
                    {
                        float dp = dt * (p1 - p0) / PipeTimeFactorS;
                        car.BrakeSystem.BrakeLine1PressurePSI -= dp;
                        car0.BrakeSystem.BrakeLine1PressurePSI += dp;
                    }
                    p0 = p1;
                    car0 = car;
                }
            }
        }

        public override void SetHandbrakePercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            HandbrakePercent = percent;
        }
        public override void SetRetainer(RetainerSetting setting)
        {
        }

        public override void AISetPercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            Car.Train.BrakeLine1PressurePSI = P2V(OneAtmospherePSIA - MaxForcePressurePSI * (1 - percent / 100));
        }
    }
}

