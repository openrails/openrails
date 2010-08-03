using MSTS;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace ORTS
{
    /**
     * This is the most used contorller. The main use if for diesel locomotives Throttle control.
     * 
     * It is used with single keypress, this means that when the user press a key, only the keydown event is handled.
     * The user need to press the key multiple times to update this controller.
     * 
     */
    public class MSTSNotchController: IController
    {
        public float CurrentValue = 0;
        public float MinimumValue = 0;
        public float MaximumValue = 1;
        private float StepSize = 0;
        public List<MSTSNotch> Notches = new List<MSTSNotch>();
        public int CurrentNotch = 0;

        #region CONSTRUCTORS

        public MSTSNotchController()
        {
        }

        public MSTSNotchController(MSTSNotchController other)
        {
            CurrentValue = other.CurrentValue;
            MinimumValue = other.MinimumValue;
            MaximumValue = other.MaximumValue;
            StepSize = other.StepSize;
            CurrentNotch = other.CurrentNotch;

            foreach (MSTSNotch notch in Notches)
            {
                Notches.Add(notch.Clone());
            }
        }

        public MSTSNotchController(STFReader f)
        {
            Parse(f);
        }

        public MSTSNotchController(BinaryReader inf)
        {
            this.Restore(inf);
        }
        #endregion

        public virtual IController Clone()
        {
            return new MSTSNotchController(this);
        }

        public virtual bool IsNotched()
        {
            return Notches.Count > 0 && !Notches[CurrentNotch].Smooth;
        }

        public virtual bool IsValid()
        {
            return StepSize != 0;
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
            f.ReadInt();
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
                    Notches.Add(new MSTSNotch(value, smooth, type, f));
                    if (type != ")")
                        f.VerifyEndOfBlock();
                }
            }
            SetValue(CurrentValue);
        }

        private float GetNotchBoost()
        {
            //Allow the full range to change in 10 seconds
            return (Math.Abs(MaximumValue - MinimumValue) / StepSize) / 10;
        }

        public void SetValue(float v)
        {
            CurrentValue = MathHelper.Clamp(v, MinimumValue, MaximumValue);

            for (CurrentNotch = Notches.Count - 1; CurrentNotch > 0; CurrentNotch--)
            {
                if (Notches[CurrentNotch].Value <= CurrentValue)
                    break;
            }

            if (CurrentNotch >= 0 && !Notches[CurrentNotch].Smooth)
                CurrentValue = Notches[CurrentNotch].Value;
        }

        public float Increase(float elapsedSeconds)
        {
            CurrentValue += StepSize * elapsedSeconds * GetNotchBoost();
            CurrentValue = Math.Min(CurrentValue, MaximumValue);

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

        public float Decrease(float elapsedSeconds)
        {
            CurrentValue -= StepSize * elapsedSeconds * GetNotchBoost();
            CurrentValue = Math.Max(CurrentValue, MinimumValue);

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
            x = (CurrentValue - notch.Value) / (x - notch.Value);
            if (notch.Type == MSTSNotchType.Release)
                x = 1 - x;
            return x;
        }

        public virtual string GetStatus()
        {
            if (Notches.Count == 0)
                return string.Format("{0:F0}%", 100 * CurrentValue);
            MSTSNotch notch = Notches[CurrentNotch];
            if (!notch.Smooth && notch.Type == MSTSNotchType.Dummy)
                return string.Format("{0:F0}%", 100 * CurrentValue);
            if (!notch.Smooth)
                return notch.GetName();
            return string.Format("{0} {1:F0}%", notch.GetName(), 100 * GetNotchFraction());
        }

        public virtual void Save(BinaryWriter outf)
        {
            outf.Write((int)ControllerTypes.MSTSNotchController);

            this.SaveData(outf);
        }

        protected virtual void SaveData(BinaryWriter outf)
        {            
            outf.Write(CurrentValue);
            outf.Write(MinimumValue);
            outf.Write(MaximumValue);
            outf.Write(StepSize);
            outf.Write(CurrentNotch);            
            outf.Write(Notches.Count);
            
            foreach(MSTSNotch notch in Notches)
            {
                notch.Save(outf);                
            }            
        }

        protected virtual void Restore(BinaryReader inf)
        {
            CurrentValue = inf.ReadSingle();
            MinimumValue = inf.ReadSingle();
            MaximumValue = inf.ReadSingle();
            StepSize = inf.ReadSingle();
            CurrentNotch = inf.ReadInt32();

            int count = inf.ReadInt32();

            for (int i = 0; i < count; ++i)
            {
                Notches.Add(new MSTSNotch(inf));
            }           
        }

    }
}
