using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS;
using System.IO;

namespace ORTS
{
    public class MSTSEngineController
    {
        public float CurrentValue = 0;
        public float MinimumValue = 0;
        public float MaximumValue = 1;
        public float StepSize = 0;
        public List<MSTSNotch> Notches = new List<MSTSNotch>();
        public int CurrentNotch = 0;

        // brake controller values
        public float MaxPressurePSI = 90;
        public float ReleaseRatePSIpS = 5;
        public float ApplyRatePSIpS = 2;
        public float EmergencyRatePSIpS = 10;
        public float FullServReductionPSI = 26;
        public float MinReductionPSI = 6;

        public MSTSEngineController()
        {
        }

        public MSTSEngineController(STFReader f)
        {
            Parse(f);
        }

        public void Parse(STFReader f)
        {
            f.VerifyStartOfBlock();
            MinimumValue = f.ReadFloat();
            MaximumValue = f.ReadFloat();
            StepSize = f.ReadFloat();
            CurrentValue = f.ReadFloat();
            //Console.WriteLine("controller {0} {1} {2} {3}", MinimumValue, MaximumValue, StepSize, CurrentValue);
            f.ReadTokenNoComment(); // numnotches
            f.VerifyStartOfBlock();
            int n = f.ReadInt();
            for (; ; )
            {
                string token = f.ReadTokenNoComment().ToLower();
                if (token == ")") break;
                if (token == "notch")
                {
                    f.VerifyStartOfBlock();
                    float value = f.ReadFloat();
                    int smooth = f.ReadInt();
                    string type = f.ReadString();
                    //Console.WriteLine("Notch {0} {1} {2}", value, smooth, type);
                    Notches.Add(new MSTSNotch(value,smooth,type,f));
                    if (type != ")")
                        f.VerifyEndOfBlock();
                }
            }
            SetValue(CurrentValue);
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

        public static MSTSEngineController Copy(MSTSEngineController copy)
        {
            if (copy == null)
                return null;
            MSTSEngineController controller = new MSTSEngineController();
            controller.CurrentValue = copy.CurrentValue;
            controller.MinimumValue = copy.MinimumValue;
            controller.MaximumValue = copy.MaximumValue;
            controller.CurrentNotch = copy.CurrentNotch;
            for (int i = 0; i < controller.Notches.Count; i++)
                controller.Notches.Add(copy.Notches[i]);
            controller.MaxPressurePSI = copy.MaxPressurePSI;
            controller.ReleaseRatePSIpS = copy.ReleaseRatePSIpS;
            controller.ApplyRatePSIpS = copy.ApplyRatePSIpS;
            controller.EmergencyRatePSIpS = copy.EmergencyRatePSIpS;
            controller.FullServReductionPSI = copy.FullServReductionPSI;
            controller.MinReductionPSI = copy.MinReductionPSI;
            return controller;
        }

        public static void Save(MSTSEngineController controller, BinaryWriter outf)
        {
            outf.Write(controller != null);
            if (controller != null)
            {
                outf.Write(controller.CurrentValue);
                outf.Write(controller.MinimumValue);
                outf.Write(controller.MaximumValue);
                outf.Write(controller.StepSize);
                outf.Write(controller.CurrentNotch);
                outf.Write(controller.Notches.Count);
                for (int i = 0; i < controller.Notches.Count; i++)
                {
                    outf.Write(controller.Notches[i].Value);
                    outf.Write(controller.Notches[i].Smooth);
                    outf.Write((int)controller.Notches[i].Type);
                }
                outf.Write(controller.MaxPressurePSI);
                outf.Write(controller.ReleaseRatePSIpS);
                outf.Write(controller.ApplyRatePSIpS);
                outf.Write(controller.EmergencyRatePSIpS);
                outf.Write(controller.FullServReductionPSI);
                outf.Write(controller.MinReductionPSI);
            }
        }

        public static MSTSEngineController Restore(BinaryReader inf)
        {
            bool create = inf.ReadBoolean();
            if (!create)
                return null;
            MSTSEngineController controller = new MSTSEngineController();
            controller.CurrentValue = inf.ReadSingle();
            controller.MinimumValue = inf.ReadSingle();
            controller.MaximumValue = inf.ReadSingle();
            controller.StepSize = inf.ReadSingle();
            controller.CurrentNotch = inf.ReadInt32();
            int n = inf.ReadInt32();
            for (int i = 0; i < n; i++)
                controller.Notches.Add(new MSTSNotch(inf.ReadSingle(),inf.ReadBoolean(),inf.ReadInt32()));
            controller.MaxPressurePSI = inf.ReadSingle();
            controller.ReleaseRatePSIpS = inf.ReadSingle();
            controller.ApplyRatePSIpS = inf.ReadSingle();
            controller.EmergencyRatePSIpS= inf.ReadSingle();
            controller.FullServReductionPSI = inf.ReadSingle();
            controller.MinReductionPSI = inf.ReadSingle();
            return controller;
        }

        public float GetValue()
        {
            return CurrentValue;
        }
        public void SetValue(float v)
        {
            CurrentValue = v;
            if (CurrentValue < MinimumValue)
                CurrentValue = MinimumValue;
            if (CurrentValue > MaximumValue)
                CurrentValue = MaximumValue;
            for (CurrentNotch = Notches.Count - 1; CurrentNotch > 0; CurrentNotch--)
                if (Notches[CurrentNotch].Value <= CurrentValue)
                    break;
            if (CurrentNotch>=0 && !Notches[CurrentNotch].Smooth)
                CurrentValue = Notches[CurrentNotch].Value;
        }

        public float Increase()
        {
            CurrentValue += StepSize;
            if (CurrentValue > MaximumValue)
                CurrentValue = MaximumValue;
            if (Notches.Count > 0)
            {
                if (CurrentNotch < Notches.Count - 1 && (!Notches[CurrentNotch].Smooth || CurrentValue >= Notches[CurrentNotch + 1].Value))
                {
                    CurrentNotch++;
                    CurrentValue = Notches[CurrentNotch].Value;
                }
                else if (CurrentNotch == Notches.Count - 1 && !Notches[CurrentNotch].Smooth)
                {
                    CurrentValue = Notches[CurrentNotch].Value;
                }
            }
            return CurrentValue;
        }

        public float Decrease()
        {
            CurrentValue -= StepSize;
            if (CurrentValue < MinimumValue)
                CurrentValue = MinimumValue;
            if (Notches.Count > 0)
            {
                if (CurrentNotch > 0 && (!Notches[CurrentNotch].Smooth || CurrentValue < Notches[CurrentNotch].Value))
                {
                    CurrentNotch--;
                    if (!Notches[CurrentNotch].Smooth)
                        CurrentValue = Notches[CurrentNotch].Value;
                }
                else if (CurrentNotch == 0 && !Notches[CurrentNotch].Smooth)
                {
                    CurrentValue = Notches[CurrentNotch].Value;
                }
            }
            return CurrentValue;
        }
        public virtual string GetStatus()
        {
            if (Notches.Count == 0)
                return string.Format("{0:F0}%", 100*CurrentValue);
            MSTSNotch notch = Notches[CurrentNotch];
            if (!notch.Smooth && notch.Type==MSTSNotchType.Dummy)
                return string.Format("{0:F0}%", 100 * CurrentValue);
            if (!notch.Smooth)
                return notch.GetName();
            return string.Format("{0} {1:F0}%", notch.GetName(), 100*GetNotchFraction());
        }
        public float GetNotchFraction()
        {
            if (Notches.Count == 0)
                return 0;
            MSTSNotch notch = Notches[CurrentNotch];
            if (!notch.Smooth)
                return 1;
            float x = 1;
            if (CurrentNotch + 1 < Notches.Count)
                x = Notches[CurrentNotch + 1].Value;
            x= (CurrentValue - notch.Value) / (x - notch.Value);
            if (notch.Type == MSTSNotchType.Release)
                x = 1 - x;
            return x;
        }
        public bool GetIsEmergency()
        {
            return Notches.Count !=0 && Notches[CurrentNotch].Type == MSTSNotchType.Emergency;
        }
        public void SetEmergency()
        {
            for (int i = 0; i < Notches.Count; i++)
                if (Notches[i].Type == MSTSNotchType.Emergency)
                {
                    CurrentNotch = i;
                    CurrentValue = Notches[i].Value;
                }
        }
        public void UpdatePressure(ref float pressurePSI, float elapsedClockSeconds)
        {
            if (Notches.Count == 0)
            {
                pressurePSI = MaxPressurePSI - FullServReductionPSI * CurrentValue;
            }
            else
            {
                MSTSNotch notch = Notches[CurrentNotch];
                float x = GetNotchFraction();
                switch (notch.Type)
                {
                    case MSTSNotchType.Release:
                        pressurePSI += x * ReleaseRatePSIpS * elapsedClockSeconds;
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
                    case MSTSNotchType.GSelfLapH:
                    case MSTSNotchType.Suppression:
                    case MSTSNotchType.ContServ:
                        x = MaxPressurePSI - MinReductionPSI * (1 - x) - FullServReductionPSI * x;
                        if (pressurePSI > x)
                            pressurePSI -= ApplyRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.GSelfLap:
                        x = MaxPressurePSI - MinReductionPSI * (1 - x) - FullServReductionPSI * x;
                        if (pressurePSI > x)
                            pressurePSI -= ApplyRatePSIpS * elapsedClockSeconds;
                        // disabled until graduated release modeled on cars
                        //else if (pressurePSI < x)
                        //    pressurePSI += ReleaseRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.Emergency:
                        pressurePSI -= EmergencyRatePSIpS * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.Dummy:
                        x *= MaxPressurePSI - FullServReductionPSI;
                        if (pressurePSI > x)
                            pressurePSI -= ApplyRatePSIpS * elapsedClockSeconds;
                        else if (pressurePSI < x)
                            pressurePSI += ReleaseRatePSIpS * elapsedClockSeconds;
                        break;
                }
                if (pressurePSI > MaxPressurePSI)
                    pressurePSI = MaxPressurePSI;
                if (pressurePSI < 0)
                    pressurePSI = 0;
            }
        }
        public void UpdateEngineBrakePressure(ref float pressurePSI, float elapsedClockSeconds)
        {
            if (Notches.Count == 0)
            {
                pressurePSI = (MaxPressurePSI - FullServReductionPSI) * CurrentValue;
            }
            else
            {
                MSTSNotch notch = Notches[CurrentNotch];
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
                        if (pressurePSI < x)
                        {
                            pressurePSI += ApplyRatePSIpS * elapsedClockSeconds;
                            if (pressurePSI > x)
                                pressurePSI = x;
                        }
                        else if (pressurePSI > x)
                        {
                            pressurePSI -= ReleaseRatePSIpS * elapsedClockSeconds;
                            if (pressurePSI < x)
                                pressurePSI = x;
                        }
                        break;
                }
                if (pressurePSI > MaxPressurePSI)
                    pressurePSI = MaxPressurePSI;
                if (pressurePSI < 0)
                    pressurePSI = 0;
            }
        }
    
    }

    public enum MSTSNotchType { Dummy, Release, Running, SelfLap, Lap, Apply, GSelfLap, GSelfLapH, Suppression, ContServ, FullServ, Emergency };

    public class MSTSNotch
    {
        public float Value;
        public bool Smooth;
        public MSTSNotchType Type;
        public MSTSNotch(float v, int s, string type, STFReader f)
        {
            Value= v;
            Smooth= s==0 ? false : true;
            Type = MSTSNotchType.Dummy;
            string lower = type.ToLower();
            if (lower.StartsWith("trainbrakescontroller"))
                lower= lower.Substring(21);
            if (lower.StartsWith("enginebrakescontroller"))
                lower = lower.Substring(22);
            switch (lower)
            {
                case "dummy": break;
                case ")": break;
                case "releasestart": Type = MSTSNotchType.Release; break;
                case "fullquickreleasestart": Type = MSTSNotchType.Release; break;
                case "runningstart": Type = MSTSNotchType.Running; break;
                case "selflapstart": Type = MSTSNotchType.SelfLap; break;
                case "holdstart": Type = MSTSNotchType.Lap; break;
                case "holdlappedstart": Type = MSTSNotchType.Lap; break;
                case "graduatedselflaplimitedstart": Type = MSTSNotchType.GSelfLap; break;
                case "graduatedselflaplimitedholdingstart": Type = MSTSNotchType.GSelfLapH; break;
                case "applystart": Type = MSTSNotchType.Apply; break;
                case "continuousservicestart": Type = MSTSNotchType.ContServ; break;
                case "suppressionstart": Type = MSTSNotchType.Suppression; break;
                case "fullservicestart": Type = MSTSNotchType.FullServ; break;
                case "emergencystart": Type = MSTSNotchType.Emergency; break;
                case "epapplystart": Type = MSTSNotchType.Apply; break;
                case "epholdstart": Type = MSTSNotchType.SelfLap; break;
                case "minimalreductionstart": Type = MSTSNotchType.Lap; break;
                default:
                    STFError.Report(f, "Unknown notch type: " + type);
                    break;
            }
        }
        public MSTSNotch(float v, bool s, int t)
        {
            Value = v;
            Smooth = s;
            Type = (MSTSNotchType) t;
        }
        public string GetName()
        {
            switch (Type)
            {
                case MSTSNotchType.Dummy: return "";
                case MSTSNotchType.Release: return "Release";
                case MSTSNotchType.Running: return "Running";
                case MSTSNotchType.Apply: return "Apply";
                case MSTSNotchType.Emergency: return "Emergency";
                case MSTSNotchType.SelfLap: return "Lap";
                case MSTSNotchType.GSelfLap: return "Service";
                case MSTSNotchType.GSelfLapH: return "Service";
                case MSTSNotchType.Lap: return "Lap";
                case MSTSNotchType.Suppression: return "Suppresion";
                case MSTSNotchType.ContServ: return "Cont. Service";
                case MSTSNotchType.FullServ: return "Full Service";
                default: return "";
            }
        }
    }
}
