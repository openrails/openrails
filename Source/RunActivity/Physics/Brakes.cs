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
    }

    public abstract class MSTSBrakeSystem: BrakeSystem
    {
        public abstract void SetHandbrakePercent(float percent);

        public abstract void Parse(string lowercasetoken, STFReader f);

        public abstract void Update(float elapsedClockSeconds);

        public abstract void Increase();

        public abstract void Decrease();

        public abstract void InitializeFromCopy(BrakeSystem copy);

    }

    public class AirSinglePipe : MSTSBrakeSystem
    {
        float MaxHandbrakeForceN = 0;
        float MaxBrakeForceN = 89e3f;
        float BrakePercent = 0;  // simplistic system
        TrainCar Car;

        public AirSinglePipe( TrainCar car )
        {
            Car = car;
        }

        public override void InitializeFromCopy(BrakeSystem copy)
        {
            AirSinglePipe thiscopy = (AirSinglePipe)copy;
            MaxBrakeForceN = thiscopy.MaxBrakeForceN;
            MaxBrakeForceN = thiscopy.MaxBrakeForceN;
        }

        public override string GetStatus()
        {
            return string.Format( "{0}% {1}PSI", BrakePercent, BrakeLine1PressurePSI);
        }

        public override void Parse(string lowercasetoken, STFReader f)
        {
            switch (lowercasetoken)
            {
                case "wagon(maxhandbrakeforce": MaxHandbrakeForceN = f.ReadFloatBlock(); break;
                case "wagon(maxbrakeforce": MaxBrakeForceN = f.ReadFloatBlock(); break;
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

        public override void Update(float elapsedClockSeconds)
        {
            // Unrealistic temporary code
            float brakePercent = 10 * (90 - BrakeLine1PressurePSI);
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
            Car.Train.BrakeLine1PressurePSI = 90 - BrakePercent / 10;
        }
    }
}
