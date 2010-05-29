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
        public float BrakeLine1PressurePSI = 90;     // main trainline pressure at this car
        public float BrakeLine2PressurePSI = 0;     // extra line for dual line systems
        public float BrakeLine3PressurePSI = 0;     // extra line just in case

        public abstract void AISetPercent(float percent);

        public abstract string GetStatus();

        public abstract void Save(BinaryWriter outf);

        public abstract void Restore( BinaryReader inf );

        public abstract void Initialize(bool handbrakeOn);
        public abstract void SetHandbrakePercent(float percent);
    }

    public abstract class MSTSBrakeSystem: BrakeSystem
    {

        public abstract void Parse(string lowercasetoken, STFReader f);

        public abstract void Update(float elapsedClockSeconds);

        public abstract void Increase();

        public abstract void Decrease();

        public abstract void InitializeFromCopy(BrakeSystem copy);

    }

    public enum RetainerSetting { Exhaust, HighPressure, LowPressure, SlowDirect };

    public class AirSinglePipe : MSTSBrakeSystem
    {
        float MaxHandbrakeForceN = 0;
        float MaxBrakeForceN = 89e3f;
        float BrakePercent = 0;  // simplistic system
        TrainCar Car;
        float HandbrakePercent = 0;
        float CylPressurePSI = 64;
        float AutoCylPressurePSI = 64;
        float AuxResPressurePSI = 64;
        float MaxCylPressurePSI = 64;
        float AuxCylVolumeRatio = 2.5f;
        float AuxBrakeLineVolumeRatio = 3.1f;
        //float ChargeTimeFactor = 72f;
        //float ApplyTimeFactor = 85.9f;
        //float InitApplyTimeFactor = 85.9f;
        //float InitApplyThresholdPSI = 0;// 9;
        float RetainerPressureThresholdPSI = 0;
        //float RetainerTimeFactor = 9.99f;
        float MaxReleaseRate = 1.86f;
        float MaxApplicationRate = .9f;
        float MaxAuxilaryChargingRate = 1.684f;
        public enum ValveState { Lap, Apply, Release };
        ValveState TripleValveState = ValveState.Lap;

        public AirSinglePipe( TrainCar car )
        {
            Car = car;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            AirSinglePipe thiscopy = (AirSinglePipe)copy;
            MaxHandbrakeForceN = thiscopy.MaxHandbrakeForceN;
            MaxBrakeForceN = thiscopy.MaxBrakeForceN;
            MaxCylPressurePSI = thiscopy.MaxCylPressurePSI;
        }

        public override string GetStatus()
        {
            return string.Format("{0:F0} {1:F0}", CylPressurePSI, BrakeLine1PressurePSI) + (HandbrakePercent>0 ? string.Format(" handbrake {0:F0}%",HandbrakePercent):"");
        }

        public override void Parse(string lowercasetoken, STFReader f)
        {
            switch (lowercasetoken)
            {
                case "wagon(maxhandbrakeforce": MaxHandbrakeForceN = f.ReadFloatBlock(); break;
                case "wagon(maxbrakeforce": MaxBrakeForceN = f.ReadFloatBlock(); break;
                case "wagon(brakecylinderpressureformaxbrakebrakeforce": MaxCylPressurePSI = AutoCylPressurePSI = f.ReadFloatBlock(); break;
                case "wagon(triplevalveratio": AuxCylVolumeRatio = f.ReadFloatBlock(); break;
                case "wagon(maxreleaserate": MaxReleaseRate = f.ReadFloatBlock(); break;
                case "wagon(maxapplicationrate": MaxApplicationRate = f.ReadFloatBlock(); break;
                case "wagon(maxauxilarychargingrate": MaxAuxilaryChargingRate = f.ReadFloatBlock(); break;
                case "wagon(emergencyreschargingrate": f.ReadFloatBlock(); break;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write(BrakeLine1PressurePSI);
            outf.Write(BrakeLine2PressurePSI);
            outf.Write(BrakeLine3PressurePSI);
            outf.Write(BrakePercent);
            outf.Write(HandbrakePercent);
            outf.Write(MaxHandbrakeForceN);
            outf.Write(MaxBrakeForceN);
            outf.Write(MaxCylPressurePSI);
            outf.Write(AutoCylPressurePSI);
            outf.Write(AuxResPressurePSI);
            outf.Write((int)TripleValveState);
        }

        public override void Restore(BinaryReader inf)
        {
            BrakeLine1PressurePSI = inf.ReadSingle();
            BrakeLine2PressurePSI = inf.ReadSingle();
            BrakeLine3PressurePSI = inf.ReadSingle();
            BrakePercent = inf.ReadSingle();
            HandbrakePercent = inf.ReadSingle();
            MaxHandbrakeForceN = inf.ReadSingle();
            MaxBrakeForceN = inf.ReadSingle();
            MaxCylPressurePSI = inf.ReadSingle();
            AutoCylPressurePSI = inf.ReadSingle();
            AuxResPressurePSI = inf.ReadSingle();
            TripleValveState = (ValveState)inf.ReadInt32();
        }

        public override void Initialize(bool handbrakeOn)
        {
            AuxResPressurePSI = BrakeLine1PressurePSI;
            AutoCylPressurePSI = (BrakeLine2PressurePSI - BrakeLine1PressurePSI) * AuxCylVolumeRatio;
            if (AutoCylPressurePSI > MaxCylPressurePSI)
                AutoCylPressurePSI = MaxCylPressurePSI;
            TripleValveState = ValveState.Lap;
            HandbrakePercent = handbrakeOn ? 100 : 0;
            //Console.WriteLine("initb {0} {1}", AuxResPressurePSI, AutoCylPressurePSI);
        }
        public override void Update(float elapsedClockSeconds)
        {
            if (BrakeLine1PressurePSI < AuxResPressurePSI - 1)
                TripleValveState = ValveState.Apply;
            else if (BrakeLine1PressurePSI > AuxResPressurePSI + 1)
                TripleValveState = ValveState.Release;
            else if (TripleValveState == ValveState.Apply && BrakeLine1PressurePSI >= AuxResPressurePSI)
                TripleValveState = ValveState.Lap;
            else if (TripleValveState == ValveState.Release && BrakeLine1PressurePSI <= AuxResPressurePSI)
                TripleValveState = ValveState.Lap;
            if (TripleValveState == ValveState.Apply)
            {
#if false
                float dp = elapsedClockSeconds * (AuxResPressurePSI - AutoCylPressurePSI) / ApplyTimeFactor;
                if (BrakeLine1PressurePSI > AuxResPressurePSI - dp)
                    dp = AuxResPressurePSI - BrakeLine1PressurePSI;
                AuxResPressurePSI -= dp;
                AutoCylPressurePSI += dp * AuxCylVolumeRatio;
                if (AutoCylPressurePSI < InitApplyThresholdPSI)
                {
                    dp = elapsedClockSeconds * (BrakeLine1PressurePSI - AutoCylPressurePSI) / InitApplyTimeFactor;
                    AutoCylPressurePSI += dp;
                    if (AutoCylPressurePSI > InitApplyThresholdPSI)
                    {
                        dp -= AutoCylPressurePSI - InitApplyThresholdPSI;
                        AutoCylPressurePSI = InitApplyThresholdPSI;
                    }
                    BrakeLine1PressurePSI -= dp * AuxBrakeLineVolumeRatio / AuxCylVolumeRatio;
                }
#else
                float dp = elapsedClockSeconds * MaxApplicationRate;
                if (BrakeLine1PressurePSI > AuxResPressurePSI - dp)
                    dp = AuxResPressurePSI - BrakeLine1PressurePSI;
                if (AuxResPressurePSI - dp < 0)
                    dp = -AuxResPressurePSI;
                AuxResPressurePSI -= dp;
                AutoCylPressurePSI += dp * AuxCylVolumeRatio;
#endif
            }
            if (TripleValveState == ValveState.Release)
            {
#if false
                if (AutoCylPressurePSI > RetainerPressureThresholdPSI)
                {
                    //AutoCylPressurePSI -= elapsedClockSeconds * AutoCylPressurePSI / RetainerTimeFactor;
                    float pa = AutoCylPressurePSI + 15;
                    float d = .061474f * pa * 9.99f / RetainerTimeFactor;
                    float machsq = 1.38348f * (1 - 15 / pa);
                    if (machsq < 1)
                        d *= (float)Math.Sqrt(machsq);
                    AutoCylPressurePSI -= elapsedClockSeconds * d;
                }
                if (AutoCylPressurePSI < RetainerPressureThresholdPSI)
                    AutoCylPressurePSI = RetainerPressureThresholdPSI;
                float dp = elapsedClockSeconds * (BrakeLine1PressurePSI - AuxResPressurePSI) / ChargeTimeFactor;
                if (AuxResPressurePSI + dp > BrakeLine1PressurePSI - dp * AuxBrakeLineVolumeRatio)
                    dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                AuxResPressurePSI += dp;
                BrakeLine1PressurePSI -= dp * AuxBrakeLineVolumeRatio;
#else
                if (AutoCylPressurePSI > RetainerPressureThresholdPSI)
                    AutoCylPressurePSI -= elapsedClockSeconds * MaxReleaseRate;
                if (AutoCylPressurePSI < RetainerPressureThresholdPSI)
                    AutoCylPressurePSI = RetainerPressureThresholdPSI;
                float dp = elapsedClockSeconds * MaxAuxilaryChargingRate;
                if (AuxResPressurePSI + dp > BrakeLine1PressurePSI - dp * AuxBrakeLineVolumeRatio)
                    dp = (BrakeLine1PressurePSI - AuxResPressurePSI) / (1 + AuxBrakeLineVolumeRatio);
                AuxResPressurePSI += dp;
                BrakeLine1PressurePSI -= dp * AuxBrakeLineVolumeRatio;
#endif
            }
            if (BrakeLine3PressurePSI >= 1000)
            {
                BrakeLine3PressurePSI -= 1000;
                AutoCylPressurePSI -= 4 * elapsedClockSeconds;
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

        public void SetRetainer(RetainerSetting setting)
        {
            switch (setting)
            {
                case RetainerSetting.Exhaust:
                    RetainerPressureThresholdPSI = 0;
                    //RetainerTimeFactor = 9.99f;         // 50 to 5 in 23 seconds
                    break;
                case RetainerSetting.HighPressure:
                    RetainerPressureThresholdPSI = 20;
                    //RetainerTimeFactor = 98.2f;         // 50 to 20 in 90 seconds
                    break;
                case RetainerSetting.LowPressure:
                    RetainerPressureThresholdPSI = 10;
                    //RetainerTimeFactor = 37.3f;         // 50 to 10 in 60 seconds
                    break;
                case RetainerSetting.SlowDirect:
                    RetainerPressureThresholdPSI = 0;
                    //RetainerTimeFactor = 53.4f;         // 50 to 10 in 86 seconds
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

        public override string GetStatus()
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

        public override void Initialize(bool handbrakeOn)
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

