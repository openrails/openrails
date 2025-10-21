// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;
using ORTS.Scripting.Api;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
{
    public class MSTSNotch {
        public float Value;
        public bool Smooth;
        public ControllerState Type;
        public string Name;
        public MSTSNotch(float v, int s, string type, string name, STFReader stf)
        {
            Value = v;
            Smooth = s == 0 ? false : true;
            Type = ControllerState.Dummy;  // Default to a dummy controller state if no valid alternative state used
            string lower = type.ToLower();
            if (lower.StartsWith("trainbrakescontroller"))
                lower = lower.Substring(21);
            if (lower.StartsWith("enginebrakescontroller"))
                lower = lower.Substring(22);
            if (lower.StartsWith("brakemanbrakescontroller"))
                lower = lower.Substring(24);
            switch (lower)
            {
                case "dummy": break;
                case ")": break;
                case "releasestart": Type = ControllerState.Release; break;
                case "fullquickreleasestart": Type = ControllerState.FullQuickRelease; break;
                case "runningstart": Type = ControllerState.Running; break;
                case "selflapstart": Type = ControllerState.SelfLap; break;
                case "holdstart": Type = ControllerState.Hold; break;
                case "straightbrakingreleaseonstart": Type = ControllerState.StrBrkReleaseOn; break;
                case "straightbrakingreleaseoffstart": Type = ControllerState.StrBrkReleaseOff; break;
                case "straightbrakingreleasestart": Type = ControllerState.StrBrkRelease; break;
                case "straightbrakinglapstart": Type = ControllerState.StrBrkLap; break;
                case "straightbrakingapplystart": Type = ControllerState.StrBrkApply; break;
                case "straightbrakingapplyallstart": Type = ControllerState.StrBrkApplyAll; break;
                case "straightbrakingemergencystart": Type = ControllerState.StrBrkEmergency; break;
                case "holdlappedstart": Type = ControllerState.Lap; break;
                case "neutralhandleoffstart": Type = ControllerState.Neutral; break;
                case "graduatedselflaplimitedstart": Type = ControllerState.GSelfLap; break;
                case "graduatedselflaplimitedholdingstart": Type = ControllerState.GSelfLapH; break;
                case "applystart": Type = ControllerState.Apply; break;
                case "continuousservicestart": Type = ControllerState.ContServ; break;
                case "suppressionstart": Type = ControllerState.Suppression; break;
                case "fullservicestart": Type = ControllerState.FullServ; break;
                case "emergencystart": Type = ControllerState.Emergency; break;
                case "minimalreductionstart": Type = ControllerState.MinimalReduction; break;
                case "epapplystart": Type = ControllerState.EPApply; break;
                case "eponlystart": Type = ControllerState.EPOnly; break;
                case "epfullservicestart": Type = ControllerState.EPFullServ; break;
                case "epholdstart": Type = ControllerState.SelfLap; break;
                case "smeholdstart": Type = ControllerState.SMESelfLap; break;
                case "smeonlystart": Type = ControllerState.SMEOnly; break;
                case "smefullservicestart": Type = ControllerState.SMEFullServ; break;
                case "smereleasestart": Type = ControllerState.SMEReleaseStart; break;
                case "vacuumcontinuousservicestart": Type = ControllerState.VacContServ; break;
                case "vacuumapplycontinuousservicestart": Type = ControllerState.VacApplyContServ; break;
                case "manualbrakingstart": Type = ControllerState.ManualBraking; break;
                case "brakenotchstart": Type = ControllerState.BrakeNotch; break;
                case "overchargestart": Type = ControllerState.Overcharge; break;
                case "slowservicestart": Type = ControllerState.SlowService; break;
                case "holdenginestart": Type = ControllerState.HoldEngine; break;
                case "bailoffstart": Type = ControllerState.BailOff; break;
                default:
                    STFException.TraceInformation(stf, "Skipped unknown notch type " + type);
                    break;
            }
            Name = name;
        }
        public MSTSNotch(float v, bool s, int t)
        {
            Value = v;
            Smooth = s;
            Type = (ControllerState)t;
        }

        public MSTSNotch(MSTSNotch other)
        {
            Value = other.Value;
            Smooth = other.Smooth;
            Type = other.Type;
            Name = other.Name;
        }

        public MSTSNotch Clone()
        {
            return new MSTSNotch(this);
        }

        public string GetName()
        {
            if (!string.IsNullOrEmpty(Name)) return Name;
            return ControllerStateDictionary.Dict[Type];
        }
    }

    /**
     * This is the most used controller. The main use is for diesel locomotives' Throttle control.
     * 
     * It is used with single keypress, this means that when the user press a key, only the keydown event is handled.
     * The user need to press the key multiple times to update this controller.
     * 
     */
    public class MSTSNotchController: IController
    {
        private float currentValue;
        public float CurrentValue
        {
            get
            {
                return currentValue;
            }
            set
            {
                if (currentValue == value) return;
                currentValue = value;
                TimeSinceLastChange = 0;
            }
        }
        private float savedValue;
        public float SavedValue
        {
            get
            {
                if (TimeSinceLastChange >= DelayTimeBeforeUpdating)
                    savedValue = currentValue;
                return savedValue;
            }
        }
        public float IntermediateValue;
        public float MinimumValue;
        public float MaximumValue = 1;
        public const float StandardBoost = 5.0f; // standard step size multiplier
        public const float FastBoost = 20.0f;
        public float StepSize;
        private List<MSTSNotch> Notches = new List<MSTSNotch>();
        public int CurrentNotch { get; set; }
        public bool ToZero = false; // true if controller zero command;

        private float OldValue;

        //Does not need to persist
        //this indicates if the controller is increasing or decreasing, 0 no changes
        public float UpdateValue { get; set; }
        private float? controllerTarget;
        public double CommandStartTime { get; set; }
        private float prevValue;
        public float TimeSinceLastChange { get; private set; }
        public float DelayTimeBeforeUpdating;

        #region CONSTRUCTORS

        public MSTSNotchController()
        {
        }

        public MSTSNotchController(int numOfNotches)
        {
            MinimumValue = 0;
            MaximumValue = numOfNotches - 1;
            StepSize = 1;
            for (int i = 0; i < numOfNotches; i++)
                Notches.Add(new MSTSNotch(i, false, 0));
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
            DelayTimeBeforeUpdating = other.DelayTimeBeforeUpdating;

            foreach (MSTSNotch notch in other.Notches)
            {
                Notches.Add(notch.Clone());
            }
        }

        public MSTSNotchController(STFReader stf)
        {
            Parse(stf);
        }

        public MSTSNotchController(List<MSTSNotch> notches)
        {
            Notches = notches;
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
            MinimumValue = stf.ReadFloat(STFReader.UNITS.None, null);
            MaximumValue = stf.ReadFloat(STFReader.UNITS.None, null);
            StepSize = stf.ReadFloat(STFReader.UNITS.None, null);
            IntermediateValue = CurrentValue = stf.ReadFloat(STFReader.UNITS.None, null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("numnotches", () =>{
                    stf.MustMatch("(");
                    stf.ReadInt(null);
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("notch", ()=>{
                            stf.MustMatch("(");
                            float value = stf.ReadFloat(STFReader.UNITS.None, null);
                            int smooth = stf.ReadInt(null);
                            string type = stf.ReadString();
                            string name = null;
                            while(type != ")" && !stf.EndOfBlock())
                            {
                                switch (stf.ReadItem().ToLower())
                                {
                                    case "(":
                                        stf.SkipRestOfBlock();
                                        break;
                                    case "ortslabel":
                                        name = stf.ReadStringBlock(null);
                                        break;
                                }
                            }
                            Notches.Add(new MSTSNotch(value, smooth, type, name, stf));
                        }),
                    });
                }),
                new STFReader.TokenProcessor("ortsdelaytimebeforeupdating", () =>
                {
                    DelayTimeBeforeUpdating = stf.ReadFloatBlock(STFReader.UNITS.Time, null);
                }),
            });
            SetValue(CurrentValue);
        }

        public int NotchCount()
        {
            return Notches.Count;
        }

        private float GetNotchBoost(float boost)
        {
            return (ToZero && ((CurrentNotch >= 0 && Notches[CurrentNotch].Smooth) || Notches.Count == 0 || 
                IntermediateValue - CurrentValue > StepSize) ? FastBoost : boost);
        }

        public void AddNotch(float value)
        {
            Notches.Add(new MSTSNotch(value, false, (int)ControllerState.Dummy));
        }

        /// <summary>
        /// Sets the actual value of the controller, and adjusts the actual notch to match.
        /// </summary>
        /// <param name="value">Normalized value the controller to be set to. Normally is within range [-1..1]</param>
        /// <returns>1 or -1 if there was a significant change in controller position, otherwise 0.
        /// Needed for hinting whether a serializable command is to be issued for repeatability.
        /// Sign is indicating the direction of change, being displayed by confirmer text.</returns>
        public int SetValue(float value)
        {
            CurrentValue = IntermediateValue = MathHelper.Clamp(value, MinimumValue, MaximumValue);
            var oldNotch = CurrentNotch;

            for (CurrentNotch = Notches.Count - 1; CurrentNotch > 0; CurrentNotch--)
            {
                if (Notches[CurrentNotch].Value <= CurrentValue)
                    break;
            }

            if (CurrentNotch >= 0 && !Notches[CurrentNotch].Smooth)
                CurrentValue = Notches[CurrentNotch].Value;

            var change = CurrentNotch > oldNotch || CurrentValue > OldValue + 0.1f || CurrentValue == 1 && OldValue < 1 
                ? 1 : CurrentNotch < oldNotch || CurrentValue < OldValue - 0.1f || CurrentValue == 0 && OldValue > 0 ? -1 : 0;
            if (change != 0)
                OldValue = CurrentValue;

            return change;
        }

        public float SetPercent(float percent)
        {
            if (percent > 100) SetValue(1);
            float v = (MinimumValue < 0 && percent < 0 ? -MinimumValue : MaximumValue) * percent / 100;
            CurrentValue = MathHelper.Clamp(v, MinimumValue, MaximumValue);

            if (CurrentNotch >= 0)
            {
                if (Notches[Notches.Count - 1].Type == ControllerState.Emergency)
                    v = Notches[Notches.Count - 1].Value * percent / 100;
                for (; ; )
                {
                    MSTSNotch notch = Notches[CurrentNotch];
                    if (CurrentNotch > 0 && v < notch.Value)
                    {
                        MSTSNotch prev = Notches[CurrentNotch-1];
                        if (!notch.Smooth && !prev.Smooth && v - prev.Value > .45 * (notch.Value - prev.Value))
                            break;
                        CurrentNotch--;
                        continue;
                    }
                    if (CurrentNotch < Notches.Count - 1)
                    {
                        MSTSNotch next = Notches[CurrentNotch + 1];
                        if (next.Type != ControllerState.Emergency)
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

        public void StartIncrease( float? target ) {
            controllerTarget = target;
            ToZero = false;
            StartIncrease();
        }

        public void StartIncrease()
        {
            UpdateValue = 1;

            // When we have notches and the current Notch does not require smooth, we go directly to the next notch
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

        public void StartDecrease( float? target, bool toZero = false)
        {
            controllerTarget = target;
            ToZero = toZero;
            StartDecrease();
        }
        
        public void StartDecrease()
        {
            UpdateValue = -1;

            //If we have notches and the previous Notch does not require smooth, we go directly to the previous notch
            if ((Notches.Count > 0) && (CurrentNotch > 0) && SmoothMin() == null)
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
            if (UpdateValue == 1 || UpdateValue == -1)
            {
                CheckControllerTargetAchieved();
                UpdateValues(elapsedSeconds, UpdateValue, StandardBoost);
            }
            if (prevValue == CurrentValue) TimeSinceLastChange += elapsedSeconds;
            prevValue = CurrentValue;
            return CurrentValue;
        }

        public float UpdateAndSetBoost(float elapsedSeconds, float boost)
        {
            if (UpdateValue == 1 || UpdateValue == -1)
            {
                CheckControllerTargetAchieved();
                UpdateValues(elapsedSeconds, UpdateValue, boost);
            }
            if (prevValue == CurrentValue) TimeSinceLastChange += elapsedSeconds;
            prevValue = CurrentValue;
            return CurrentValue;
        }

        /// <summary>
        /// If a target has been set, then stop once it's reached and also cancel the target.
        /// </summary>
        public void CheckControllerTargetAchieved() {
            if( controllerTarget != null )
            {
                if( UpdateValue > 0.0 )
                {
                    if( CurrentValue >= controllerTarget )
                    {
                        StopIncrease();
                        controllerTarget = null;
                    }
                }
                else
                {
                    if( CurrentValue <= controllerTarget )
                    {
                        StopDecrease();
                        controllerTarget = null;
                    }
                }
            }
        }

        private float UpdateValues(float elapsedSeconds, float direction, float boost)
        {
            //We increment the intermediate value first
            IntermediateValue += StepSize * elapsedSeconds * GetNotchBoost(boost) * direction;
            IntermediateValue = MathHelper.Clamp(IntermediateValue, MinimumValue, MaximumValue);

            //Do we have notches
            if (Notches.Count > 0)
            {
                //Increasing, check if the notch has changed
                if ((direction > 0) && (CurrentNotch < Notches.Count - 1) && (IntermediateValue >= Notches[CurrentNotch + 1].Value))
                {
                    // steamer_ctn - The following code was added in relation to reported bug  #1200226. However it seems to prevent the brake controller from ever being moved to EMERGENCY position.
                    // Bug conditions indicated in the bug report have not been able to be duplicated, ie there doesn't appear to be a "safety stop" when brake key(s) held down continuously
                    // Code has been reverted pending further investigation or reports of other issues
                    // Prevent TrainBrake to continuously switch to emergency
                    //      if (Notches[CurrentNotch + 1].Type == ControllerState.Emergency)
                    //         IntermediateValue = Notches[CurrentNotch + 1].Value - StepSize;
                    //      else
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
                // Respect British 3-wire EP brake configurations
                return (notch.Type == ControllerState.EPApply || notch.Type == ControllerState.EPOnly) ? CurrentValue : 1;
            float x = 1;
            if (CurrentNotch + 1 < Notches.Count)
                x = Notches[CurrentNotch + 1].Value;
            x = (CurrentValue - notch.Value) / (x - notch.Value);
            if (notch.Type == ControllerState.Release)
                x = 1 - x;
            return x;
        }

        public float? SmoothMin()
        {
            float? target = null;
            if (Notches.Count > 0)
            {
                if (CurrentNotch > 0 && Notches[CurrentNotch - 1].Smooth)
                    target = Notches[CurrentNotch - 1].Value;
                else if (Notches[CurrentNotch].Smooth && CurrentValue > Notches[CurrentNotch].Value)
                    target = Notches[CurrentNotch].Value;
            }
            else
                target = MinimumValue;
            return target;
        }

        public float? SmoothMax()
        {
            float? target = null;
            if (Notches.Count > 0 && CurrentNotch < Notches.Count - 1 && Notches[CurrentNotch].Smooth)
                target = Notches[CurrentNotch + 1].Value;
            else if (Notches.Count == 0
                || (Notches.Count == 1 && Notches[CurrentNotch].Smooth))
                target = MaximumValue;
            return target;
        }

        public float? DPSmoothMax()
        {
            float? target = null;
            if (Notches.Count > 0 && CurrentNotch < Notches.Count - 1 && Notches[CurrentNotch].Smooth)
                target = Notches[CurrentNotch + 1].Value;
            else if (Notches.Count == 0 || CurrentNotch == Notches.Count - 1 && Notches[CurrentNotch].Smooth)
                target = MaximumValue;
            return target;
        }

        public virtual string GetStatus()
        {
            if (Notches.Count == 0)
                return string.Format("{0:F0}%", 100 * CurrentValue);
            MSTSNotch notch = Notches[CurrentNotch];
            if (!notch.Smooth && notch.Type == ControllerState.Dummy)
                return string.Format("{0:F0}%", 100 * CurrentValue);
            if (!notch.Smooth)
                return notch.GetName();
            if (notch.GetName().Length > 0)
                return string.Format("{0} {1:F0}%", notch.GetName(), 100 * GetNotchFraction());
            return string.Format("{0:F0}%", 100 * GetNotchFraction());
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
        }

        public virtual void Restore(BinaryReader inf)
        {
            IntermediateValue = CurrentValue = inf.ReadSingle();            
            MinimumValue = inf.ReadSingle();
            MaximumValue = inf.ReadSingle();
            StepSize = inf.ReadSingle();
            CurrentNotch = inf.ReadInt32();

            UpdateValue = 0;         
        }

        public MSTSNotch GetCurrentNotch()
        {
            return Notches.Count == 0 ? null : Notches[CurrentNotch];
        }

        protected void SetCurrentNotch(ControllerState type)
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

        public void SetStepSize ( float stepSize)
        {
            StepSize = stepSize;
        }

        public void Normalize (float ratio)
        {
            for (int i = 0; i < Notches.Count; i++)
                Notches[i].Value /= ratio;
        }

        /// <summary>
        /// Get the nearest discrete notch position for a normalized input value.
        /// This function is not dependent on notch controller actual (current) value, so can be queried for computer-intervened value as well.
        /// </summary>
        public int GetNearestNotch(float value)
        {
            var notch = 0;
            for (notch = Notches.Count - 1; notch > 0; notch--)
            {
                if (Notches[notch].Value <= value)
                {
                    if (notch < Notches.Count - 1 && Notches[notch + 1].Value - value < value - Notches[notch].Value)
                        notch++;
                    break;
                }
            }
            return notch;
        }

        /// <summary>
        /// Get the discrete notch position for a normalized input value.
        /// This function is not dependent on notch controller actual (current) value, so can be queried for computer-intervened value as well.
        /// </summary>
        public int GetNotch(float value)
        {
            var notch = 0;
            for (notch = Notches.Count - 1; notch > 0; notch--)
            {
                if (Notches[notch].Value <= value)
                {
                     break;
                }
            }
            return notch;
        }

    }
}
