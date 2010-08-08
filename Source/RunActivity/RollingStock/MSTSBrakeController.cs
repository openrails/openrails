using MSTS;
using System;
using System.IO;

namespace ORTS
{
    /**
     * This is the a Controller used to control brakes.
     * 
     * This is mainly a Notch controller, but it allows continuos input and also 
     * has specific methods update brake status.
     * 
     */ 
    public class MSTSBrakeController: MSTSNotchController, IBrakeController
    {
        // brake controller values
        private float MaxPressurePSI = 90;
        private float ReleaseRatePSIpS = 5;
        private float ApplyRatePSIpS = 2;
        private float EmergencyRatePSIpS = 10;
        private float FullServReductionPSI = 26;
        private float MinReductionPSI = 6;

        public MSTSBrakeController()
        {
        }

        public MSTSBrakeController(MSTSBrakeController controller):
            base(controller)  
        {            
        }

        public MSTSBrakeController(BinaryReader inf):
            base(inf)               
        {
            this.RestoreData(inf);
        }

        public new IController Clone()
        {
            return new MSTSBrakeController(this);
        }

        public float GetFullServReductionPSI()
        {
            return FullServReductionPSI;
        }

        public float GetMaxPressurePSI()
        {
            return MaxPressurePSI;
        }

        public void UpdatePressure(ref float pressurePSI, float elapsedClockSeconds, ref float epPressurePSI)
        {
            MSTSNotch notch = this.GetCurrentNotch();
            if (notch == null)
            {
                pressurePSI = MaxPressurePSI - FullServReductionPSI * CurrentValue;
            }
            else
            {                
                float x = GetNotchFraction();
                switch (notch.Type)
                {
                    case MSTSNotchType.Release:
                        pressurePSI += x * ReleaseRatePSIpS * elapsedClockSeconds;
                        epPressurePSI -= x * ReleaseRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.Running:
                        if (notch.Smooth)
                            x = .1f * (1 - x);
                        pressurePSI += x * ReleaseRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.Apply:
                    case MSTSNotchType.FullServ:
                        pressurePSI -= x * ApplyRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.EPApply:
                        pressurePSI += x * ReleaseRatePSIpS * elapsedClockSeconds;
                        if (notch.Smooth)
                            IncreasePressure(ref epPressurePSI, x * FullServReductionPSI, ApplyRatePSIpS, elapsedClockSeconds);
                        else
                            epPressurePSI += x * ApplyRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.GSelfLapH:
                    case MSTSNotchType.Suppression:
                    case MSTSNotchType.ContServ:
                    case MSTSNotchType.GSelfLap:
                        x = MaxPressurePSI - MinReductionPSI * (1 - x) - FullServReductionPSI * x;
                        DecreasePressure(ref pressurePSI, x, ApplyRatePSIpS, elapsedClockSeconds);
                        if (Program.GraduatedRelease)
                            IncreasePressure(ref pressurePSI, x, ReleaseRatePSIpS, elapsedClockSeconds);
                        break;
                    case MSTSNotchType.Emergency:
                        pressurePSI -= EmergencyRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.Dummy:
                        x *= MaxPressurePSI - FullServReductionPSI;
                        IncreasePressure(ref pressurePSI, x, ReleaseRatePSIpS, elapsedClockSeconds);
                        DecreasePressure(ref pressurePSI, x, ApplyRatePSIpS, elapsedClockSeconds);
                        break;
                }
                if (pressurePSI > MaxPressurePSI)
                    pressurePSI = MaxPressurePSI;
                if (pressurePSI < 0)
                    pressurePSI = 0;
                if (epPressurePSI > MaxPressurePSI)
                    epPressurePSI = MaxPressurePSI;
                if (epPressurePSI < 0)
                    epPressurePSI = 0;
            }
        }

        public void UpdateEngineBrakePressure(ref float pressurePSI, float elapsedClockSeconds)
        {
            MSTSNotch notch = this.GetCurrentNotch();
            if (notch == null)
            {
                pressurePSI = (MaxPressurePSI - FullServReductionPSI) * CurrentValue;
            }
            else
            {                
                float x = GetNotchFraction();
                switch (notch.Type)
                {
                    case MSTSNotchType.Release:
                        pressurePSI -= x * ReleaseRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.Running:
                        pressurePSI -= ReleaseRatePSIpS * elapsedClockSeconds;
                        break;
#if false
                    case MSTSNotchType.Apply:
                    case MSTSNotchType.FullServ:
                        pressurePSI += x * ApplyRatePSIpS * elapsedClockSeconds;
                        break;
#endif
                    case MSTSNotchType.Emergency:
                        pressurePSI += EmergencyRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.Dummy:
                        pressurePSI = (MaxPressurePSI - FullServReductionPSI) * CurrentValue;
                        break;
                    default:
                        x *= MaxPressurePSI - FullServReductionPSI;
                        IncreasePressure(ref pressurePSI, x, ApplyRatePSIpS, elapsedClockSeconds);
                        DecreasePressure(ref pressurePSI, x, ReleaseRatePSIpS, elapsedClockSeconds);
                        break;
                }
                if (pressurePSI > MaxPressurePSI)
                    pressurePSI = MaxPressurePSI;
                if (pressurePSI < 0)
                    pressurePSI = 0;
            }
        }      

        public void ParseBrakeValue(string lowercasetoken, STFReader f)
        {
            switch (lowercasetoken)
            {
                case "maxsystempressure": MaxPressurePSI = f.ReadFloatBlock(); break;
                case "maxreleaserate": ReleaseRatePSIpS = f.ReadFloatBlock(); break;
                case "maxapplicationrate": ApplyRatePSIpS = f.ReadFloatBlock(); break;
                case "emergencyapplicationrate": EmergencyRatePSIpS = f.ReadFloatBlock(); break;
                case "fullservicepressuredrop": FullServReductionPSI = f.ReadFloatBlock(); break;
                case "minpressurereduction": MinReductionPSI = f.ReadFloatBlock(); break;
                //default: Console.WriteLine("{0}", lowercasetoken); break;
            }
        }

        public bool GetIsEmergency()
        {
            MSTSNotch notch = this.GetCurrentNotch();

            return notch != null && notch.Type == MSTSNotchType.Emergency;            
        }

        public void SetEmergency()
        {
            SetCurrentNotch(MSTSNotchType.Emergency);            
        }        

        private void IncreasePressure(ref float pressurePSI, float targetPSI, float ratePSIpS, float elapsedSeconds)
        {
            if (pressurePSI < targetPSI)
            {
                pressurePSI += ratePSIpS * elapsedSeconds;
                if (pressurePSI > targetPSI)
                    pressurePSI = targetPSI;
            }
        }

        private void DecreasePressure(ref float pressurePSI, float targetPSI, float ratePSIpS, float elapsedSeconds)
        {
            if (pressurePSI > targetPSI)
            {
                pressurePSI -= ratePSIpS * elapsedSeconds;
                if (pressurePSI < targetPSI)
                    pressurePSI = targetPSI;
            }
        }

        public override void Save(BinaryWriter outf)
        {
            outf.Write((int)ControllerTypes.MSTSBrakeController);

            this.SaveData(outf);
        }

        protected override void SaveData(BinaryWriter outf)
        {
            base.SaveData(outf);
            
            outf.Write(MaxPressurePSI);
            outf.Write(ReleaseRatePSIpS);
            outf.Write(ApplyRatePSIpS);
            outf.Write(EmergencyRatePSIpS);
            outf.Write(FullServReductionPSI);
            outf.Write(MinReductionPSI);
        }

        private void RestoreData(BinaryReader inf)
        {
            MaxPressurePSI = inf.ReadSingle();
            ReleaseRatePSIpS = inf.ReadSingle();
            ApplyRatePSIpS = inf.ReadSingle();
            EmergencyRatePSIpS = inf.ReadSingle();
            FullServReductionPSI = inf.ReadSingle();
            MinReductionPSI = inf.ReadSingle();
        }
    }
}
