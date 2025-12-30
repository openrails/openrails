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

using Orts.Parsers.Msts;
using Orts.Simulation.AIs;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
{
    public class ScriptedBrakeController : IController
    {
        public readonly MSTSLocomotive Locomotive;
        public readonly Simulator Simulator;

        public bool Activated;
        string ScriptName = "MSTS";
        BrakeController Script;
        public List<MSTSNotch> Notches = new List<MSTSNotch>();

        private bool emergencyBrakingPushButton = false;
        private bool tcsEmergencyBraking = false;
        private bool tcsFullServiceBraking = false;
        private bool overchargeButtonPressed = false;
        private bool quickReleaseButtonPressed = false;
        private bool neutralModeCommandSwitchOn = false;

        public bool EmergencyBraking
        {
            get
            {
                return emergencyBrakingPushButton || tcsEmergencyBraking || (Script.GetState() == ControllerState.Emergency);
            }
        }
        public bool EmergencyBrakingPushButton
        {
            get
            {
                return emergencyBrakingPushButton;
            }
            set
            {
                if (Simulator.Confirmer != null)
                {
                    if (value && !emergencyBrakingPushButton && !tcsEmergencyBraking)
                        Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.On);
                    else if (!value && emergencyBrakingPushButton && !tcsEmergencyBraking)
                        Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.Off);
                }

                emergencyBrakingPushButton = value;
            }
        }
        public bool TCSEmergencyBraking
        {
            get
            {
                return tcsEmergencyBraking;
            }
            set
            {
                if (Simulator.Confirmer != null)
                {
                    if (value && !emergencyBrakingPushButton && !tcsEmergencyBraking)
                        Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.On);
                    else if (!value && !emergencyBrakingPushButton && tcsEmergencyBraking)
                        Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.Off);
                }

                tcsEmergencyBraking = value;
            }
        }
        public bool TCSFullServiceBraking
        {
            get
            {
                return tcsFullServiceBraking;
            }
            set
            {
                if (Simulator.Confirmer != null)
                {
                    if (value && !tcsFullServiceBraking)
                        Simulator.Confirmer.Confirm(CabControl.TrainBrake, CabSetting.On);
                }

                tcsFullServiceBraking = value;
            }
        }
        public bool QuickReleaseButtonPressed
        {
            get
            {
                return quickReleaseButtonPressed;
            }
            set
            {
                if (Simulator.Confirmer != null)
                {
                    if (value && !quickReleaseButtonPressed)
                        Simulator.Confirmer.Confirm(CabControl.QuickRelease, CabSetting.On);
                    else if (!value && quickReleaseButtonPressed)
                        Simulator.Confirmer.Confirm(CabControl.QuickRelease, CabSetting.Off);
                }

                quickReleaseButtonPressed = value;
            }
        }
        public bool OverchargeButtonPressed
        {
            get
            {
                return overchargeButtonPressed;
            }
            set
            {
                if (Simulator.Confirmer != null)
                {
                    if (value && !overchargeButtonPressed)
                        Simulator.Confirmer.Confirm(CabControl.Overcharge, CabSetting.On);
                    else if (!value && overchargeButtonPressed)
                        Simulator.Confirmer.Confirm(CabControl.Overcharge, CabSetting.Off);
                }

                overchargeButtonPressed = value;
            }
        }

        public bool NeutralModeCommandSwitchOn
        {
            get
            {
                return neutralModeCommandSwitchOn;
            }
            set
            {
                if (Simulator.Confirmer != null)
                {
                    if (value && !neutralModeCommandSwitchOn)
                        Simulator.Confirmer.Confirm(CabControl.NeutralMode, CabSetting.On);
                    else if (!value && neutralModeCommandSwitchOn)
                        Simulator.Confirmer.Confirm(CabControl.NeutralMode, CabSetting.Off);
                }

                neutralModeCommandSwitchOn = value;
            }
        }

        public bool NeutralModeOn { get; set; }

        public float MaxPressurePSI { get; set; }
        public float MaxOverchargePressurePSI { get; private set; }
        public float ReleaseRatePSIpS { get; private set; }
        public float QuickReleaseRatePSIpS { get; private set; }
        public float OverchargeEliminationRatePSIpS { get; private set; }
        public float SlowApplicationRatePSIpS { get; private set; }
        public float ApplyRatePSIpS { get; private set; }
        public float EmergencyRatePSIpS { get; private set; }
        public float FullServReductionPSI { get; private set; }
        public float MinReductionPSI { get; private set; }
        public float TrainDynamicBrakeIntervention { get; set; } = -1;
        public float CruiseControlBrakeDemand
        { 
            get
            {
                if (Locomotive.CruiseControl == null) return -1;
                if (this == Locomotive.EngineBrakeController) return Locomotive.CruiseControl.EngineBrakePercent / 100 ?? -1;
                else return Locomotive.CruiseControl.TrainBrakePercent / 100 ?? -1;
            }
        }
        InterpolatorDiesel2D DynamicBrakeBlendingTable;

        /// <summary>
        /// Needed for proper mouse operation in the cabview
        /// </summary>
        public float IntermediateValue { get; set; }

        /// <summary>
        /// Knowing actual notch and its change is needed for proper repeatability of mouse and RailDriver operation
        /// </summary>
        public int CurrentNotch { get; set; }

        public ControllerState TrainBrakeControllerState
        {
            get
            {
                if (Script is MSTSBrakeController)
                    return Notches.Count > 0 ? Notches[CurrentNotch].Type : ControllerState.Dummy;
                else
                    return Script.GetState();
            }
        }

        float OldValue;

        public float InitialValue { get; set; }
        public float CurrentValue { get; set; }
        public float MinimumValue { get; set; }
        public float MaximumValue { get; set; }
        public float StepSize { get; set; }
        public float UpdateValue { get; set; }
        public double CommandStartTime { get; set; }

        public ScriptedBrakeController(MSTSLocomotive locomotive)
        {
            Simulator = locomotive.Simulator;
            Locomotive = locomotive;

            MaxPressurePSI = 90;
            MaxOverchargePressurePSI = 95;
            ReleaseRatePSIpS = 5;
            QuickReleaseRatePSIpS = 10;
            OverchargeEliminationRatePSIpS = 0.036f;
            ApplyRatePSIpS = 2;
            SlowApplicationRatePSIpS = 1;
            EmergencyRatePSIpS = 10;
            FullServReductionPSI = 26;
            MinReductionPSI = 6;
        }

        public ScriptedBrakeController(ScriptedBrakeController controller, MSTSLocomotive locomotive)
        {
            Simulator = locomotive.Simulator;
            Locomotive = locomotive;

            ScriptName = controller.ScriptName;
            MaxPressurePSI = controller.MaxPressurePSI;
            MaxOverchargePressurePSI = controller.MaxOverchargePressurePSI;
            ReleaseRatePSIpS = controller.ReleaseRatePSIpS;
            QuickReleaseRatePSIpS = controller.QuickReleaseRatePSIpS;
            OverchargeEliminationRatePSIpS = controller.OverchargeEliminationRatePSIpS;
            ApplyRatePSIpS = controller.ApplyRatePSIpS;
            SlowApplicationRatePSIpS = controller.SlowApplicationRatePSIpS;
            EmergencyRatePSIpS = controller.EmergencyRatePSIpS;
            FullServReductionPSI = controller.FullServReductionPSI;
            MinReductionPSI = controller.MinReductionPSI;

            InitialValue = controller.InitialValue;
            CurrentValue = controller.CurrentValue;
            MinimumValue = controller.MinimumValue;
            MaximumValue = controller.MaximumValue;
            StepSize = controller.StepSize;

            controller.Notches.ForEach(
                (item) => { Notches.Add(new MSTSNotch(item)); }
            );

            DynamicBrakeBlendingTable = controller.DynamicBrakeBlendingTable;
        }

        public ScriptedBrakeController Clone(MSTSLocomotive locomotive)
        {
            return new ScriptedBrakeController(this, locomotive);
        }

        public void Parse(STFReader stf)
        {
            Parse(stf.Tree.ToLower(), stf);
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(trainbrakescontrollermaxsystempressure":
                case "engine(enginebrakescontrollermaxsystempressure":
                    MaxPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null);
                    break;

                case "engine(ortstrainbrakescontrollermaxoverchargepressure":
                    MaxOverchargePressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null);
                    break;

                case "engine(trainbrakescontrollermaxreleaserate":
                case "engine(enginebrakescontrollermaxreleaserate":    
                    ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(trainbrakescontrollermaxquickreleaserate":
                case "engine(enginebrakescontrollermaxquickreleaserate":
                    QuickReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(ortstrainbrakescontrolleroverchargeeliminationrate":
                    OverchargeEliminationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(trainbrakescontrollermaxapplicationrate":
                case "engine(enginebrakescontrollermaxapplicationrate":
                    ApplyRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(trainbrakescontrolleremergencyapplicationrate":
                case "engine(enginebrakescontrolleremergencyapplicationrate":
                    EmergencyRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(trainbrakescontrollerfullservicepressuredrop":
                case "engine(enginebrakescontrollerfullservicepressuredrop":
                    FullServReductionPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null);
                    break;

                case "engine(trainbrakescontrollerminpressurereduction":
                case "engine(enginebrakescontrollerminpressurereduction":
                    MinReductionPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null);
                    break;

                case "engine(ortstrainbrakescontrollerslowapplicationrate":
                case "engine(ortsenginebrakescontrollerslowapplicationrate":
                    SlowApplicationRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(enginecontrollers(brake_train":
                case "engine(enginecontrollers(brake_engine":
                case "engine(enginecontrollers(brake_brakeman":
                    stf.MustMatch("(");
                    MinimumValue = stf.ReadFloat(STFReader.UNITS.None, null);
                    MaximumValue = stf.ReadFloat(STFReader.UNITS.None, null);
                    StepSize = stf.ReadFloat(STFReader.UNITS.None, null);
                    InitialValue = stf.ReadFloat(STFReader.UNITS.None, null);
                    string token = stf.ReadItem(); // s/b numnotches
                    if (string.Compare(token, "NumNotches", true) != 0) // handle error in gp38.eng where extra parameter provided before NumNotches statement 
                        stf.ReadItem();
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
                    break;

                case "engine(ortstrainbrakecontroller":
                case "engine(ortsenginebrakecontroller":
                    ScriptName = stf.ReadStringBlock(null);
                    break;
                case "engine(ortstraindynamicblendingtable":
                    DynamicBrakeBlendingTable = new InterpolatorDiesel2D(stf, false);
                    break;
            }
        }

        public void Initialize(bool reinitialize = false)
        {
            if (!Activated)
            {
                if (ScriptName == "PBL2") Script = new PBL2BrakeController();
                else if (ScriptName != null && ScriptName != "MSTS")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as BrakeController;
                }
                if (Script == null)
                {
                    var mstsController = new MSTSBrakeController();
                    mstsController.ForceControllerReleaseGraduated = Simulator.Settings.GraduatedRelease;
                    mstsController.DynamicBrakeBlendingTable = DynamicBrakeBlendingTable;
                    Script = mstsController as BrakeController;
                }

                // Only set controller to initial value on first initialization, not reinitialization
                if (!reinitialize)
                    CurrentValue = IntermediateValue = InitialValue;

                Script.AttachToHost(this);

                // AbstractScriptClass
                Script.Car = Locomotive;
                Script.ClockTime = () => (float)Simulator.ClockTime;
                Script.GameTime = () => (float)Simulator.GameTime;
                Script.PreUpdate = () => Simulator.PreUpdate;
                Script.DistanceM = () => Locomotive.DistanceM;
                Script.SpeedMpS = () => Math.Abs(Locomotive.SpeedMpS);
                Script.Confirm = Locomotive.Simulator.Confirmer.Confirm;
                Script.Message = Locomotive.Simulator.Confirmer.Message;
                Script.SignalEvent = Locomotive.SignalEvent;
                Script.SignalEventToTrain = (evt) =>
                {
                    if (Locomotive.Train != null)
                    {
                        Locomotive.Train.SignalEvent(evt);
                    }
                };
                Script.Initialize();
            }
        }

        public void InitializeMoving()
        {
            Script.InitializeMoving();
        }

        public float Update(float elapsedClockSeconds)
        {
            if (Script != null)
                return Script.Update(elapsedClockSeconds);
            else
                return 0;
        }

        public void UpdatePressure(ref float pressurePSI, float elapsedClockSeconds, ref float epControllerState)
        {
            if (Script != null)
            {
                // Conversion is needed until the pressures of the brake system are converted to bar.
                float pressureBar = Bar.FromPSI(pressurePSI);
                Script.UpdatePressure(ref pressureBar, elapsedClockSeconds, ref epControllerState);
                pressurePSI = Bar.ToPSI(pressureBar);
            }
        }

        public void UpdateEngineBrakePressure(ref float pressurePSI, float elapsedClockSeconds)
        {
            if (Script != null)
            {
                // Conversion is needed until the pressures of the brake system are converted to bar.
                float pressureBar = Bar.FromPSI(pressurePSI);
                Script.UpdateEngineBrakePressure(ref pressureBar, elapsedClockSeconds);
                pressurePSI = Bar.ToPSI(pressureBar);
            }
        }

        public void SignalEvent(BrakeControllerEvent evt)
        {
            if (Script != null)
                Script.HandleEvent(evt);
        }

        public void SignalEvent(BrakeControllerEvent evt, float? value)
        {
            if (Script != null)
                Script.HandleEvent(evt, value);
            else
            {
                if (evt == BrakeControllerEvent.SetCurrentValue && value != null)
                {
                    float newValue = value ?? 0F;
                    CurrentValue = newValue;
                }
            }
        }

        public void StartIncrease()
        {
            SignalEvent(BrakeControllerEvent.StartIncrease);
        }

        public void StopIncrease()
        {
            SignalEvent(BrakeControllerEvent.StopIncrease);
        }

        public void StartDecrease()
        {
            SignalEvent(BrakeControllerEvent.StartDecrease);
        }

        public void StopDecrease()
        {
            SignalEvent(BrakeControllerEvent.StopDecrease);
        }

        public void StartIncrease(float? target)
        {
            SignalEvent(BrakeControllerEvent.StartIncrease, target);
        }

        public void StartDecrease(float? target , bool toZero = false)
        {
            if (toZero) SignalEvent(BrakeControllerEvent.StartDecreaseToZero, target);
            else SignalEvent(BrakeControllerEvent.StartDecrease, target);
        }

        public float SetPercent(float percent)
        {
            SignalEvent(BrakeControllerEvent.SetCurrentPercent, percent);
            return CurrentValue;
        }

        public int SetValue(float value)
        {
            var oldNotch = CurrentNotch;
            SignalEvent(BrakeControllerEvent.SetCurrentValue, value);

            var change = CurrentNotch > oldNotch || CurrentValue > OldValue + 0.1f || CurrentValue == 1 && OldValue < 1
                ? 1 : CurrentNotch < oldNotch || CurrentValue < OldValue - 0.1f || CurrentValue == 0 && OldValue > 0 ? -1 : 0;
            if (change != 0)
                OldValue = CurrentValue;
            return change;
        }

        public bool IsValid()
        {
            if (Script != null)
                return Script.IsValid();
            else
                return true;
        }

        public string GetStatus()
        {
            if (Script != null)
            {
                string state = Script.GetStateName();
                string fraction = GetStateFractionScripted();

                if (String.IsNullOrEmpty(state) && String.IsNullOrEmpty(fraction))
                    return String.Empty;
                else if (!String.IsNullOrEmpty(state) && String.IsNullOrEmpty(fraction))
                    return state;
                else if (String.IsNullOrEmpty(state) && !String.IsNullOrEmpty(fraction))
                    return fraction;
                else
                    return String.Format("{0} {1}", state, fraction);
            }
            else
                return String.Empty;
        }

        public string GetStateFractionScripted()
        {
            if (Script != null)
            {
                float? fraction = Script.GetStateFraction();

                if (fraction != null)
                    return String.Format("{0:F0}%", 100 * (fraction ?? 0));
                else
                    return String.Empty;
            }
            else
            {
                return String.Empty;
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write((int)ControllerTypes.BrakeController);

            outf.Write(CurrentValue);

            outf.Write(EmergencyBrakingPushButton);
            outf.Write(TCSEmergencyBraking);
            outf.Write(TCSFullServiceBraking);
        }

        public void Restore(BinaryReader inf)
        {
            SignalEvent(BrakeControllerEvent.SetCurrentValue, inf.ReadSingle());

            EmergencyBrakingPushButton = inf.ReadBoolean();
            TCSEmergencyBraking = inf.ReadBoolean();
            TCSFullServiceBraking = inf.ReadBoolean();
        }
    }
}
