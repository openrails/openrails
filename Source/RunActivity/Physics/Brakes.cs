using System;
using System.Collections.Generic;
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

        public abstract string GetStatus(int detailLevel);

        public abstract void Save(BinaryWriter outf);

        public abstract void Restore( BinaryReader inf );

        public abstract void Initialize(bool handbrakeOn, float maxPressurePSI);
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

        public abstract void Parse(string lowercasetoken, STFReader f);

        public abstract void Update(float elapsedClockSeconds);

        public abstract void Increase();

        public abstract void Decrease();

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

        public AirSinglePipe( TrainCar car )
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

        public override string GetStatus(int detailLevel)
        {
            if (BrakeLine1PressurePSI < 0)
                return "";
            string s = "";
            if (detailLevel > 0)
                s = s + string.Format("BC {0:F0} ",CylPressurePSI);
            s = s + string.Format("BP {0:F0}", BrakeLine1PressurePSI);
            if (detailLevel > 1)
                s = s + string.Format(" AR {0:F0} ER {1:F0} State {2}",AuxResPressurePSI, EmergResPressurePSI, TripleValveState);
            if (detailLevel > 0 && HandbrakePercent > 0)
                s = s + string.Format(" handbrake {0:F0}%", HandbrakePercent);
            return s;
        }

        public override void Parse(string lowercasetoken, STFReader f)
        {
            switch (lowercasetoken)
            {
                case "wagon(maxhandbrakeforce": MaxHandbrakeForceN = f.ReadFloatBlock(); break;
                case "wagon(maxbrakeforce": MaxBrakeForceN = f.ReadFloatBlock(); break;
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxCylPressurePSI = AutoCylPressurePSI = f.ReadFloatBlock(); break;
                case "wagon(triplevalveratio": AuxCylVolumeRatio = f.ReadFloatBlock(); break;
                case "wagon(maxreleaserate": MaxReleaseRate = ReleaseRate = f.ReadFloatBlock(); break;
                case "wagon(maxapplicationrate": MaxApplicationRate = f.ReadFloatBlock(); break;
                case "wagon(maxauxilarychargingrate": MaxAuxilaryChargingRate = f.ReadFloatBlock(); break;
                case "wagon(emergencyreschargingrate": EmergResChargingRate = f.ReadFloatBlock(); break;
                case "wagon(emergencyresvolumemultiplier": EmergAuxVolumeRatio = f.ReadFloatBlock(); break;
                case "wagon(brakepipevolume": BrakePipeVolumeFT3 = f.ReadFloatBlock(); break;
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

        public override void Initialize(bool handbrakeOn, float maxPressurePSI)
        {
            AuxResPressurePSI = BrakeLine1PressurePSI;
            EmergResPressurePSI = maxPressurePSI;
            AutoCylPressurePSI = (maxPressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
            if (AutoCylPressurePSI > MaxCylPressurePSI)
                AutoCylPressurePSI = MaxCylPressurePSI;
            TripleValveState = ValveState.Lap;
            HandbrakePercent = handbrakeOn ? 100 : 0;
            //Console.WriteLine("initb {0} {1}", AuxResPressurePSI, AutoCylPressurePSI);
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
                if (Program.GraduatedRelease)
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
                if (!Program.GraduatedRelease && AuxResPressurePSI < EmergResPressurePSI && AuxResPressurePSI < BrakeLine1PressurePSI)
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
                    case ValveState.Release: Car.SignalEvent(EventID.TrainBrakeRelease); break;
                    case ValveState.Apply: Car.SignalEvent(EventID.TrainBrakeApply); break;
                    case ValveState.Emergency: Car.SignalEvent(EventID.TrainBrakeEmergency); break;
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
            Car.FrictionForceN += f;
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

        public override void Increase()
        {
            AISetPercent(BrakePercent + 10);
        }

        public override void Decrease()
        {
            AISetPercent(BrakePercent - 10);
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
                    case ValveState.Release: Car.SignalEvent(EventID.TrainBrakeRelease); break;
                    case ValveState.Apply: Car.SignalEvent(EventID.TrainBrakeApply); break;
                    case ValveState.Emergency: Car.SignalEvent(EventID.TrainBrakeEmergency); break;
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
            Car.FrictionForceN += f;
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
                    case ValveState.Release: Car.SignalEvent(EventID.TrainBrakeRelease); break;
                    case ValveState.Apply: Car.SignalEvent(EventID.TrainBrakeApply); break;
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
            Car.FrictionForceN += f;
        }
    }

    public class VacuumSinglePipe : MSTSBrakeSystem
    {
        float MaxHandbrakeForceN = 0;
        float MaxBrakeForceN = 89e3f;
        float MaxPressurePSI = 21;
        float BrakePercent = 0;  // simplistic system
        TrainCar Car;

        public VacuumSinglePipe( TrainCar car )
        {
            Car = car;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            VacuumSinglePipe thiscopy = (VacuumSinglePipe)copy;
            MaxBrakeForceN = thiscopy.MaxBrakeForceN;
            MaxHandbrakeForceN = thiscopy.MaxHandbrakeForceN;
            MaxPressurePSI = thiscopy.MaxPressurePSI;
        }

        public override string GetStatus(int detailLevel)
        {
            return string.Format( "{0:F0}", BrakeLine1PressurePSI);
        }

        public override void Parse(string lowercasetoken, STFReader f)
        {
            switch (lowercasetoken)
            {
                case "wagon(maxhandbrakeforce": MaxHandbrakeForceN = f.ReadFloatBlock(); break;
                case "wagon(maxbrakeforce": MaxBrakeForceN = f.ReadFloatBlock(); break;
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxPressurePSI = f.ReadFloatBlock(); break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(BrakePercent);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            BrakePercent = inf.ReadSingle();
        }

        public override void Initialize(bool handbrakeOn, float maxPressurePSI)
        {
        }
        public override void Update(float elapsedClockSeconds)
        {
            if (BrakeLine1PressurePSI < 0)
                return; // pipes not connected
            // Unrealistic temporary code
            float brakePercent = 100 * (1 - BrakeLine1PressurePSI / MaxPressurePSI);
            if (brakePercent > 100) brakePercent = 100;
            if (brakePercent < 0) brakePercent = 0;
            Car.FrictionForceN += MaxBrakeForceN * brakePercent/100f; 
        }

        public override void SetHandbrakePercent(float percent)
        {
            // TODO
        }
        public override void SetRetainer(RetainerSetting setting)
        {
        }

        public override void Increase()
        {
            AISetPercent(BrakePercent + 10);
        }

        public override void Decrease()
        {
            AISetPercent(BrakePercent - 10);
        }

        public override void AISetPercent(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            BrakePercent = percent;
            Car.Train.BrakeLine1PressurePSI = MaxPressurePSI * (1 - BrakePercent / 100);
        }
    }
}

