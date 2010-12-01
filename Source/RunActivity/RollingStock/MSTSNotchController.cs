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
        public float IntermediateValue = 0;
        public float MinimumValue = 0;
        public float MaximumValue = 1;
        public float StepSize = 0;
        private List<MSTSNotch> Notches = new List<MSTSNotch>();
        public int CurrentNotch = 0;

        //Does not need to persist
        //this indicates if the controller is increasing or decreasing, 0 no changes
        public float UpdateValue = 0;        

        #region CONSTRUCTORS

        public MSTSNotchController()
        {
        }

        public MSTSNotchController(float min, float max, float stepSize)
        {
            MinimumValue = min;
            MaximumValue = max;
            StepSize = stepSize;
        }

        public MSTSNotchController(MSTSNotchController other)
        {
            CurrentValue = other.CurrentValue;
            IntermediateValue = other.IntermediateValue;
            MinimumValue = other.MinimumValue;
            MaximumValue = other.MaximumValue;
            StepSize = other.StepSize;
            CurrentNotch = other.CurrentNotch;

            foreach (MSTSNotch notch in Notches)
            {
                Notches.Add(notch.Clone());
            }
        }

        public MSTSNotchController(STFReader stf)
        {
            Parse(stf);
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

        public virtual bool IsValid()
        {
            return StepSize != 0;
        }

        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            MinimumValue = stf.ReadFloat(STFReader.UNITS.Any, null);
            MaximumValue = stf.ReadFloat(STFReader.UNITS.Any, null);
            StepSize = stf.ReadFloat(STFReader.UNITS.Any, null);
            IntermediateValue = CurrentValue = stf.ReadFloat(STFReader.UNITS.Any, null);
            //Console.WriteLine("controller {0} {1} {2} {3}", MinimumValue, MaximumValue, StepSize, CurrentValue);
            string token = stf.ReadItem(); // s/b numnotches
            if (string.Compare(token, "NumNotches", true) != 0) // handle error in gp38.eng where extra parameter provided before NumNotches statement 
                stf.ReadItem();
            stf.MustMatch("(");
            stf.ReadInt(STFReader.UNITS.None, null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("notch", ()=>{
                    stf.MustMatch("(");
                    float value = stf.ReadFloat(STFReader.UNITS.Any, null);
                    int smooth = stf.ReadInt(STFReader.UNITS.Any, null);
                    string type = stf.ReadString();
                    //Console.WriteLine("Notch {0} {1} {2}", value, smooth, type);
                    Notches.Add(new MSTSNotch(value, smooth, type, stf));
                    if (type != ")") stf.SkipRestOfBlock();
                }),
            });
            SetValue(CurrentValue);
        }

        private float GetNotchBoost()
        {            
            return 5;
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

            IntermediateValue = CurrentValue;
        }

        /// <summary>
        /// Sets the controller value based on a RailDriver control
        /// </summary>
        /// <param name="percent"></param>
        public float SetRDPercent(float percent)
        {
            float v = (MinimumValue < 0 && percent < 0 ? -MinimumValue : MaximumValue) * percent / 100;
            if (v < MinimumValue)
                v = MinimumValue;
            CurrentValue = v;
            if (CurrentNotch >= 0)
            {
                if (Notches[Notches.Count - 1].Type == MSTSNotchType.Emergency)
                    v = Notches[Notches.Count - 1].Value * percent / 100;
                for (; ; )
                {
                    MSTSNotch notch = Notches[CurrentNotch];
                    if (CurrentNotch > 0 && v < notch.Value)
                    {
                        MSTSNotch prev= Notches[CurrentNotch-1];
                        if (!notch.Smooth && !prev.Smooth && v - prev.Value > .45 * (notch.Value - prev.Value))
                            break;
                        CurrentNotch--;
                        continue;
                    }
                    if (CurrentNotch < Notches.Count - 1)
                    {
                        MSTSNotch next = Notches[CurrentNotch + 1];
                        if (next.Type != MSTSNotchType.Emergency)
                        {
                            if ((notch.Smooth || next.Smooth) && v < next.Value)
                                break;
                            if (!notch.Smooth && !next.Smooth && v - notch.Value < .55 * (next.Value - notch.Value))
                                break;
                            CurrentNotch++;
                            continue;
                        }
                    }
                    break;
                }
                if (Notches[CurrentNotch].Smooth)
                    CurrentValue = v;
                else
                    CurrentValue = Notches[CurrentNotch].Value;
            }
            IntermediateValue = CurrentValue;
            return 100 * CurrentValue;
        }

        public void StartIncrease()
        {
            UpdateValue = 1;

            //If we have notches and the current Notch does not require smooth, we go directly to the next notch
            if ((Notches.Count > 0) && (CurrentNotch < Notches.Count - 1) && (!Notches[CurrentNotch].Smooth))
            {
                ++CurrentNotch;
                IntermediateValue = CurrentValue = Notches[CurrentNotch].Value;
            }
        }

        public void StopIncrease()
        {
            UpdateValue = 0;
        }

        public void StartDecrease()
        {
            UpdateValue = -1;

            //If we have notches and the current Notch does not require smooth, we go directly to the next notch
            if ((Notches.Count > 0) && (CurrentNotch > 0) && (!Notches[CurrentNotch].Smooth))
            {
                //Keep intermediate value with the "previous" notch, so it will take a while to change notches
                //again if the user keep holding the key
                IntermediateValue = Notches[CurrentNotch].Value;
                CurrentNotch--;                
                CurrentValue = Notches[CurrentNotch].Value;
            }
        }

        public void StopDecrease()
        {
            UpdateValue = 0;
        }

        public float Update(float elapsedSeconds)
        {
            if (UpdateValue > 0)
                return this.UpdateValues(elapsedSeconds, 1);
            else if (UpdateValue < 0)
                return this.UpdateValues(elapsedSeconds, -1);
            else 
                return this.CurrentValue;
        }

        private float UpdateValues(float elapsedSeconds, float direction)
        {
            //We increment the intermediate value first
            IntermediateValue += StepSize * elapsedSeconds * GetNotchBoost() * direction;
            IntermediateValue = MathHelper.Clamp(IntermediateValue, MinimumValue, MaximumValue);

            //Do we have nothces
            if (Notches.Count > 0)
            {
                //Increasing, check if the notche has changed
                if ((direction > 0) && (CurrentNotch < Notches.Count - 1) && (IntermediateValue >= Notches[CurrentNotch + 1].Value))
                {
                    //update notch
                    CurrentNotch++;
                }
                //decreasing, again check if the current notch has changed
                else if((direction < 0) && (CurrentNotch > 0) && (IntermediateValue < Notches[CurrentNotch].Value))
                {
                    CurrentNotch--;
                }

                //If the notch is smooth, we use intermediate value that is being update smooth thought the frames
                if (Notches[CurrentNotch].Smooth)
                    CurrentValue = IntermediateValue;
                else
                    CurrentValue = Notches[CurrentNotch].Value;
            }
            else
            {
                //if no notches, we just keep updating the current value directly
                CurrentValue = IntermediateValue;
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
            IntermediateValue = CurrentValue = inf.ReadSingle();            
            MinimumValue = inf.ReadSingle();
            MaximumValue = inf.ReadSingle();
            StepSize = inf.ReadSingle();
            CurrentNotch = inf.ReadInt32();

            UpdateValue = 0;

            int count = inf.ReadInt32();

            for (int i = 0; i < count; ++i)
            {
                Notches.Add(new MSTSNotch(inf));
            }           
        }

        protected MSTSNotch GetCurrentNotch()
        {
            return Notches.Count == 0 ? null : Notches[CurrentNotch];
        }

        protected void SetCurrentNotch(MSTSNotchType type)
        {
            for (int i = 0; i < Notches.Count; i++)
            {
                if (Notches[i].Type == type)
                {
                    CurrentNotch = i;
                    CurrentValue = Notches[i].Value;

                    break;
                }
            }
        }

    }
}
