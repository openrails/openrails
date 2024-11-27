// COPYRIGHT 2013 - 2021 by the Open Rails project.
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
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using ORTS.Common;
using ORTS.Scripting.Api;
namespace Orts.Simulation.RollingStocks.SubSystems
{
    public class CruiseControl
    {
        MSTSLocomotive Locomotive;
        readonly Simulator Simulator;
        public float AbsWheelSpeedMpS => Locomotive.AbsWheelSpeedMpS;
        public bool SpeedIsMph = false;
        public bool SpeedRegulatorMaxForcePercentUnits = false;
        protected int SpeedRegulatorMaxForceSteps = 0;
        public int SelectedMaxAccelerationStep
        {
            get
            {
                return (int)Math.Round(MaxForceSelectorController.SavedValue * SpeedRegulatorMaxForceSteps);
            }
        }
        public float SelectedMaxAccelerationPercent
        {
            get
            {
                return MaxForceSelectorController.SavedValue * 100;
            }
        }
        public bool MaxForceKeepSelectedStepWhenManualModeSet = false;
        public bool KeepSelectedSpeedWhenManualModeSet = false;
        public bool ForceRegulatorAutoWhenNonZeroSpeedSelected = false;
        public bool ForceRegulatorAutoWhenNonZeroForceSelected = false;
        public bool ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero = false;
        public bool MaxForceSelectorIsDiscrete = false;
        public List<string> SpeedRegulatorOptions = new List<string>();
        public SpeedRegulatorMode SpeedRegMode = SpeedRegulatorMode.Manual;
        public SpeedSelectorMode SpeedSelMode = SpeedSelectorMode.Neutral;
        public ControllerCruiseControlLogic CruiseControlLogic = new ControllerCruiseControlLogic();
        public float? ATOSetSpeedMpS;
        private float PrevATOSpeedMpS;
        private float ATOAccelerationMpSS;
        protected IIRFilter AccelerationFilter = new IIRFilter(IIRFilter.FilterTypes.Butterworth, 1, 1.0f, 0.1f);
        public float SelectedSpeedMpS
        {
            get
            {
                if (UseThrottleAsSpeedSelector && SpeedRegMode != SpeedRegulatorMode.Auto && ZeroSelectedSpeedWhenPassingToThrottleMode) return 0;
                return ControllerValueToSelectedSpeedMpS(SpeedSelectorController.SavedValue);
            }
            set
            {
                if (UseThrottleAsSpeedSelector && SpeedRegMode != SpeedRegulatorMode.Auto) return;
                float val = 0;
                if (value >= MinimumSpeedForCCEffectMpS)
                {
                    float min = MinimumSpeedForCCEffectMpS;
                    float max = Locomotive.MaxSpeedMpS;
                    val = Math.Max((value - min) / (max - min), float.Epsilon);
                }
                SpeedSelectorController.SetPercent(val * 100);
            }
        }
        public float SetSpeedMpS
        {
            get
            {
                if (ATOSpeedTakesPriorityOverSpeedSelector && ATOSetSpeedMpS.HasValue) return ATOSetSpeedMpS.Value;
                if (!RestrictedRegionOdometer.Started)
                {
                    CurrentSelectedSpeedMpS = SelectedSpeedMpS;
                }
                float setSpeedMpS = CurrentSelectedSpeedMpS;
                if (ATOSetSpeedMpS < setSpeedMpS) setSpeedMpS = ATOSetSpeedMpS.Value;
                return setSpeedMpS;
            }
        }
		public float SetSpeedKpHOrMpH
        {
            get
            {
                return SpeedIsMph ? MpS.ToMpH(SetSpeedMpS) : MpS.ToKpH(SetSpeedMpS);
            }
        } 
        public int SelectedNumberOfAxles = 0;
        public float SpeedRegulatorNominalSpeedStepMpS = 0;
        public float MaxAccelerationMpSS = 2;
        public float MaxDecelerationMpSS = 2;
        public float? ThrottlePercent { get; private set;}
        public float? DynamicBrakePercent { get; private set;}
        public float TrainBrakePercent { get; private set; }
        public float EngineBrakePercent { get; private set; }
        protected float trainLength = 0;
        public int TrainLengthMeters = 0;
        public float CurrentSelectedSpeedMpS = 0;
        OdoMeter RestrictedRegionOdometer;
        public float StartReducingSpeedDelta = 0.5f;
        public float StartReducingSpeedDeltaDownwards = 0f;
        public float SpeedDeltaToStartBrakingMpS;
        public float SpeedDeltaToStopBrakingMpS;
        public float SpeedDeltaToStartAcceleratingMpS;
        public float SpeedDeltaToStopAcceleratingMpS;
        public float SpeedDeltaAcceleratingOffsetMpS;
        public float SpeedDeltaBrakingOffsetMpS = -0.5f;
        public float ATOAccelerationFactor = 1f;
        public float ATODecelerationFactor = 1f;
        public bool DynamicBrakePriority = false;
        public List<int> ForceStepsThrottleTable = new List<int>();
        public List<float> AccelerationTable = new List<float>();
        public enum SpeedRegulatorMode { Manual, Auto, Testing }
        public enum SpeedSelectorMode { Parking, Neutral, On, Start }
        public float ThrottleFullRangeIncreaseTimeSeconds = 6;
        public float ThrottleFullRangeDecreaseTimeSeconds = 6;
        public float DynamicBrakeFullRangeIncreaseTimeSeconds;
        public float DynamicBrakeFullRangeDecreaseTimeSeconds;
        public float TrainBrakeFullRangeIncreaseTimeSeconds = 10;
        public float TrainBrakeFullRangeDecreaseTimeSeconds = 5;
        public float ParkingBrakeEngageSpeedMpS = 0;
        public float ParkingBrakePercent = 0;
        public bool DisableZeroForceStep = false;
        public bool DynamicBrakeIsSelectedForceDependant = false;
        public bool UseThrottleAsSpeedSelector = false;
        public bool UseThrottleAsForceSelector = false;
        public bool ContinuousSpeedIncreasing = false;
        public bool ContinuousSpeedDecreasing = false;
        public float PowerReductionDelayPaxTrain = 0;
        public float PowerReductionDelayCargoTrain = 0;
        public float PowerReductionValue = 0;
        public float MaxPowerThreshold = 0;
        public float SafeSpeedForAutomaticOperationMpS = 0;
        public float SpeedSelectorStepTimeSeconds = 0;
        public bool DisableCruiseControlOnThrottleAndZeroSpeed = false;
        public bool DisableCruiseControlOnThrottleAndZeroForce = false;
        public bool DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed = false;
        public bool ForceResetRequiredAfterBraking = false;
        public bool ZeroSelectedSpeedWhenPassingToThrottleMode = false;
        public bool DynamicBrakeCommandHasPriorityOverCruiseControl = true;
        public bool TrainBrakeCommandHasPriorityOverCruiseControl = true;
        public bool TrainBrakeCommandHasPriorityOverAcceleratingCruiseControl = true;
        public bool HasIndependentThrottleDynamicBrakeLever = false;
        public bool HasProportionalSpeedSelector = false;
        public bool SpeedSelectorIsDiscrete = false;
        public bool DoComputeNumberOfAxles = false;
        public bool DisableManualSwitchToAutoWhenSetSpeedNotAtTop = false;
        public bool EnableSelectedSpeedSelectionWhenManualModeSet = false;
        public bool ModeSwitchAllowedWithThrottleNotAtZero = false;
        public bool UseTrainBrakeAndDynBrake = false;
        public bool UseDynBrake = true;
        public bool ATOSpeedTakesPriorityOverSpeedSelector = false;
        protected float SpeedDeltaToEnableTrainBrake = 5;
        protected float MinDynamicBrakePercentToEnableTrainBrake = 80;
        protected float MinDynamicBrakePercentWhileUsingTrainBrake = 20;
        public float MinimumSpeedForCCEffectMpS = 0;
        protected float RelativeAccelerationMpSS => Locomotive.Direction == Direction.Reverse ? -Locomotive.AccelerationMpSS : Locomotive.AccelerationMpSS; // Acceleration relative to state of reverser
        public bool CCIsUsingTrainBrake = false; // Cruise control is using (also) train brake to brake
        protected float TrainBrakeMinPercentValue = 10f; // Minimum train brake settable percent Value
        protected float TrainBrakeMaxPercentValue = 85f; // Maximum train brake settable percent Value
        public bool StartInAutoMode = false; // at startup cruise control is in auto mode
        public bool ThrottleNeutralPosition = false; // when UseThrottleAsSpeedSelector is true and this is true
                                                     // and we are in auto mode, the throttle zero position is a neutral position
        protected bool firstIteration = true;
        // CCThrottleOrDynBrakePercent may vary from -100 to 100 and is the percentage value which the Cruise Control
        // sets to throttle (if CCThrottleOrDynBrakePercent >=0) or to dynamic brake (if CCThrottleOrDynBrakePercent <0)
        public float CCThrottleOrDynBrakePercent = 0;
        protected float timeFromEngineMoved = 0;
        protected bool reducingForce = false;
        protected float skidSpeedDegratation = 0;
        public bool TrainBrakePriority = false;
        public bool TrainBrakePriorityIfCCAccelerating = false;
        public bool WasBraking = false;
        public bool WasForceReset = true;

        protected float DeltaAccelerationExponent = 0.5f;

        AccelerationController ThrottlePID;
        AccelerationController DynamicBrakePID;
        AccelerationController TrainBrakePID;
        protected float MaxThrottleAccelerationMpSS;
        protected float MaxDynamicBrakeDecelerationMpSS;
        protected float MaxTrainBrakeDecelerationMpSS;

        private float defaultMaxAccelerationStep;
        public MSTSNotchController MaxForceSelectorController;
        public MSTSNotchController SpeedSelectorController;

        public bool SelectedSpeedPressed = false;
        public bool EngineBrakePriority = false;
        public int AccelerationBits = 0;
        public bool Speed0Pressed, SpeedDeltaPressed;

        public CruiseControl(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
            Simulator = Locomotive.Simulator;

            RestrictedRegionOdometer = new OdoMeter(locomotive);
        }

        public CruiseControl(CruiseControl other, MSTSLocomotive locomotive)
        {
            Simulator = locomotive.Simulator;
            Locomotive = locomotive;
            
            RestrictedRegionOdometer = new OdoMeter(locomotive);

            if (other.MaxForceSelectorController != null) MaxForceSelectorController = new MSTSNotchController(other.MaxForceSelectorController);
            if (other.SpeedSelectorController != null) SpeedSelectorController = new MSTSNotchController(other.SpeedSelectorController);

            SpeedIsMph = other.SpeedIsMph;
            SpeedRegulatorMaxForcePercentUnits = other.SpeedRegulatorMaxForcePercentUnits;
            SpeedRegulatorMaxForceSteps = other.SpeedRegulatorMaxForceSteps;
            MaxForceKeepSelectedStepWhenManualModeSet = other.MaxForceKeepSelectedStepWhenManualModeSet;
            KeepSelectedSpeedWhenManualModeSet = other.KeepSelectedSpeedWhenManualModeSet;
            ForceRegulatorAutoWhenNonZeroSpeedSelected = other.ForceRegulatorAutoWhenNonZeroSpeedSelected;
            ForceRegulatorAutoWhenNonZeroForceSelected = other.ForceRegulatorAutoWhenNonZeroForceSelected;
            ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero = other.ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero;
            MaxForceSelectorIsDiscrete = other.MaxForceSelectorIsDiscrete;
            SpeedRegulatorOptions = other.SpeedRegulatorOptions;
            CruiseControlLogic = other.CruiseControlLogic;
            DeltaAccelerationExponent = other.DeltaAccelerationExponent;
            SpeedRegulatorNominalSpeedStepMpS = other.SpeedRegulatorNominalSpeedStepMpS;
            MaxAccelerationMpSS = other.MaxAccelerationMpSS;
            MaxDecelerationMpSS = other.MaxDecelerationMpSS;
            StartReducingSpeedDelta = other.StartReducingSpeedDelta;
            StartReducingSpeedDeltaDownwards = other.StartReducingSpeedDeltaDownwards;
            SpeedDeltaToStartAcceleratingMpS = other.SpeedDeltaToStartAcceleratingMpS;
            SpeedDeltaToStopAcceleratingMpS = other.SpeedDeltaToStopAcceleratingMpS;
            SpeedDeltaToStartBrakingMpS = other.SpeedDeltaToStartBrakingMpS;
            SpeedDeltaToStopBrakingMpS = other.SpeedDeltaToStopBrakingMpS;
            SpeedDeltaAcceleratingOffsetMpS = other.SpeedDeltaAcceleratingOffsetMpS;
            SpeedDeltaBrakingOffsetMpS = other.SpeedDeltaBrakingOffsetMpS;
            ATOAccelerationFactor = other.ATOAccelerationFactor;
            ATODecelerationFactor = other.ATODecelerationFactor;
            ForceStepsThrottleTable = other.ForceStepsThrottleTable;
            AccelerationTable = other.AccelerationTable;
            ThrottleFullRangeIncreaseTimeSeconds = other.ThrottleFullRangeIncreaseTimeSeconds;
            ThrottleFullRangeDecreaseTimeSeconds = other.ThrottleFullRangeDecreaseTimeSeconds;
            DynamicBrakeFullRangeIncreaseTimeSeconds = other.DynamicBrakeFullRangeIncreaseTimeSeconds;
            DynamicBrakeFullRangeDecreaseTimeSeconds = other.DynamicBrakeFullRangeDecreaseTimeSeconds;
            TrainBrakeFullRangeIncreaseTimeSeconds = other.TrainBrakeFullRangeIncreaseTimeSeconds;
            TrainBrakeFullRangeDecreaseTimeSeconds = other.TrainBrakeFullRangeDecreaseTimeSeconds;
            ParkingBrakeEngageSpeedMpS = other.ParkingBrakeEngageSpeedMpS;
            ParkingBrakePercent = other.ParkingBrakePercent;
            DisableZeroForceStep = other.DisableZeroForceStep;
            DynamicBrakeIsSelectedForceDependant = other.DynamicBrakeIsSelectedForceDependant;
            UseThrottleAsSpeedSelector = other.UseThrottleAsSpeedSelector;
            UseThrottleAsForceSelector = other.UseThrottleAsForceSelector;
            ContinuousSpeedIncreasing = other.ContinuousSpeedIncreasing;
            ContinuousSpeedDecreasing = other.ContinuousSpeedDecreasing;
            PowerReductionDelayPaxTrain = other.PowerReductionDelayPaxTrain;
            PowerReductionDelayCargoTrain = other.PowerReductionDelayCargoTrain;
            PowerReductionValue = other.PowerReductionValue;
            MaxPowerThreshold = other.MaxPowerThreshold;
            SafeSpeedForAutomaticOperationMpS = other.SafeSpeedForAutomaticOperationMpS;
            SpeedSelectorStepTimeSeconds = other.SpeedSelectorStepTimeSeconds;
            DisableCruiseControlOnThrottleAndZeroSpeed = other.DisableCruiseControlOnThrottleAndZeroSpeed;
            DisableCruiseControlOnThrottleAndZeroForce = other.DisableCruiseControlOnThrottleAndZeroForce;
            DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed = other.DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed;
            ForceResetRequiredAfterBraking = other.ForceResetRequiredAfterBraking;
            ZeroSelectedSpeedWhenPassingToThrottleMode = other.ZeroSelectedSpeedWhenPassingToThrottleMode;
            DynamicBrakeCommandHasPriorityOverCruiseControl = other.DynamicBrakeCommandHasPriorityOverCruiseControl;
            TrainBrakeCommandHasPriorityOverCruiseControl = other.TrainBrakeCommandHasPriorityOverCruiseControl;
            TrainBrakeCommandHasPriorityOverAcceleratingCruiseControl = other.TrainBrakeCommandHasPriorityOverAcceleratingCruiseControl;
            HasIndependentThrottleDynamicBrakeLever = other.HasIndependentThrottleDynamicBrakeLever;
            HasProportionalSpeedSelector = other.HasProportionalSpeedSelector;
            DisableManualSwitchToAutoWhenSetSpeedNotAtTop = other.DisableManualSwitchToAutoWhenSetSpeedNotAtTop;
            EnableSelectedSpeedSelectionWhenManualModeSet = other.EnableSelectedSpeedSelectionWhenManualModeSet;
            SpeedSelectorIsDiscrete = other.SpeedSelectorIsDiscrete;
            DoComputeNumberOfAxles = other.DoComputeNumberOfAxles;
            UseTrainBrakeAndDynBrake = other.UseTrainBrakeAndDynBrake;
            UseDynBrake = other.UseDynBrake;
            SpeedDeltaToEnableTrainBrake = other.SpeedDeltaToEnableTrainBrake;
            MinimumSpeedForCCEffectMpS = other.MinimumSpeedForCCEffectMpS;
            TrainBrakeMinPercentValue = other.TrainBrakeMinPercentValue;
            TrainBrakeMaxPercentValue = other.TrainBrakeMaxPercentValue;
            StartInAutoMode = other.StartInAutoMode;
            ThrottleNeutralPosition = other.ThrottleNeutralPosition;
            ModeSwitchAllowedWithThrottleNotAtZero = other.ModeSwitchAllowedWithThrottleNotAtZero;

            ThrottlePID = other.ThrottlePID?.Clone();
            DynamicBrakePID = other.DynamicBrakePID?.Clone();
            TrainBrakePID = other.TrainBrakePID?.Clone();
        }

        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            while (!stf.EndOfBlock())
            {
                switch (stf.ReadItem().ToLower())
                {
                    case "speedismph": SpeedIsMph = stf.ReadBoolBlock(false); break;
                    case "speedselectorsteptimeseconds": SpeedSelectorStepTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.1f); break;
                    case "throttlefullrangeincreasetimeseconds": ThrottleFullRangeIncreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 5); break;
                    case "throttlefullrangedecreasetimeseconds": ThrottleFullRangeDecreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 5); break;
                    case "dynamicbrakefullrangeincreasetimeseconds": DynamicBrakeFullRangeIncreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 5); break;
                    case "dynamicbrakefullrangedecreasetimeseconds": DynamicBrakeFullRangeDecreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 5); break;
                    case "trainbrakefullrangeincreasetimeseconds": TrainBrakeFullRangeIncreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 10); break;
                    case "trainbrakefullrangedecreasetimeseconds": TrainBrakeFullRangeDecreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 10); break;
                    case "parkingbrakeengagespeed": ParkingBrakeEngageSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, 0); break;
                    case "parkingbrakepercent": ParkingBrakePercent = stf.ReadFloatBlock(STFReader.UNITS.Any, 0); break;
                    case "maxpowerthreshold": MaxPowerThreshold = stf.ReadFloatBlock(STFReader.UNITS.Any, 0); break;
                    case "safespeedforautomaticoperationmps": SafeSpeedForAutomaticOperationMpS = stf.ReadFloatBlock(STFReader.UNITS.Any, 0); break;
                    case "maxforcepercentunits": SpeedRegulatorMaxForcePercentUnits = stf.ReadBoolBlock(false); break;
                    case "maxforcesteps": SpeedRegulatorMaxForceSteps = stf.ReadIntBlock(0); break;
                    case "maxforcekeepselectedstepwhenmanualmodeset": MaxForceKeepSelectedStepWhenManualModeSet = stf.ReadBoolBlock(false); break;
                    case "keepselectedspeedwhenmanualmodeset": KeepSelectedSpeedWhenManualModeSet = stf.ReadBoolBlock(false); break;
                    case "forceregulatorautowhennonzerospeedselected": ForceRegulatorAutoWhenNonZeroSpeedSelected = stf.ReadBoolBlock(false); break;
                    case "forceregulatorautowhennonzeroforceselected": ForceRegulatorAutoWhenNonZeroForceSelected = stf.ReadBoolBlock(false); break;
                    case "forceregulatorautowhennonzerospeedselectedandthrottleatzero": ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero = stf.ReadBoolBlock(false); break;
                    case "maxforceselectorisdiscrete": MaxForceSelectorIsDiscrete = stf.ReadBoolBlock(false); break;
                    case "continuousspeedincreasing": ContinuousSpeedIncreasing = stf.ReadBoolBlock(false); break;
                    case "disablecruisecontrolonthrottleandzerospeed": DisableCruiseControlOnThrottleAndZeroSpeed = stf.ReadBoolBlock(false); break;
                    case "disablecruisecontrolonthrottleandzeroforce": DisableCruiseControlOnThrottleAndZeroForce = stf.ReadBoolBlock(false); break;
                    case "disablecruisecontrolonthrottleandzeroforceandzerospeed": DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed = stf.ReadBoolBlock(false); break;
                    case "disablemanualswitchtoautowhensetspeednotattop": DisableManualSwitchToAutoWhenSetSpeedNotAtTop = stf.ReadBoolBlock(false); break;
                    case "enableselectedspeedselectionwhenmanualmodeset": EnableSelectedSpeedSelectionWhenManualModeSet = stf.ReadBoolBlock(false); break;
                    case "forcestepsthrottletable":
                        foreach (var forceStepThrottleValue in stf.ReadStringBlock("").Replace(" ", "").Split(','))
                        {
                            ForceStepsThrottleTable.Add(int.Parse(forceStepThrottleValue));
                        }
                        break;
                    case "accelerationtable":
                        foreach (var accelerationValue in stf.ReadStringBlock("").Replace(" ", "").Split(','))
                        {
                            AccelerationTable.Add(float.Parse(accelerationValue));
                        }
                        break;
                    case "powerreductiondelaypaxtrain": PowerReductionDelayPaxTrain = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.0f); break;
                    case "powerreductiondelaycargotrain": PowerReductionDelayCargoTrain = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.0f); break;
                    case "powerreductionvalue": PowerReductionValue = stf.ReadFloatBlock(STFReader.UNITS.Any, 100.0f); break;
                    case "disablezeroforcestep": DisableZeroForceStep = stf.ReadBoolBlock(false); break;
                    case "dynamicbrakeisselectedforcedependant": DynamicBrakeIsSelectedForceDependant = stf.ReadBoolBlock(false); break;
                    case "defaultforcestep": defaultMaxAccelerationStep = stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f); break;
                    case "startreducingspeeddelta": StartReducingSpeedDelta = (stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f) / 10); break;
                    case "startreducingspeeddeltadownwards": StartReducingSpeedDeltaDownwards = (stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f) / 10); break;
                    case "speeddeltatostartaccelerating": SpeedDeltaToStartAcceleratingMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, 0f); break;
                    case "speeddeltatostopaccelerating": SpeedDeltaToStopAcceleratingMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, 0f); break;
                    case "speeddeltatostartbraking": SpeedDeltaToStartBrakingMpS = -stf.ReadFloatBlock(STFReader.UNITS.Speed, 0f); break;
                    case "speeddeltatostopbraking": SpeedDeltaToStopBrakingMpS = -stf.ReadFloatBlock(STFReader.UNITS.Speed, 0f); break;
                    case "speeddeltaacceleratingoffset": SpeedDeltaAcceleratingOffsetMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, 0f); break;
                    case "speeddeltabrakingoffset": SpeedDeltaBrakingOffsetMpS = -stf.ReadFloatBlock(STFReader.UNITS.Speed, 0f); break;
                    case "atoaccelerationfactor": ATOAccelerationFactor = stf.ReadFloatBlock(STFReader.UNITS.None, 1f); break;
                    case "atodecelerationfactor": ATODecelerationFactor = stf.ReadFloatBlock(STFReader.UNITS.None, 1f); break;
                    case "maxacceleration": MaxAccelerationMpSS = stf.ReadFloatBlock(STFReader.UNITS.Any, 1); break;
                    case "maxdeceleration": MaxDecelerationMpSS = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.5f); break;
                    case "nominalspeedstep":  SpeedRegulatorNominalSpeedStepMpS = MpS.ToMpS(stf.ReadFloatBlock(STFReader.UNITS.Speed, 0), !SpeedIsMph); break;
                    case "usethrottleasspeedselector": UseThrottleAsSpeedSelector = stf.ReadBoolBlock(false); break;
                    case "usethrottleasforceselector": UseThrottleAsForceSelector = stf.ReadBoolBlock(false); break;
                    case "forceresetrequiredafterbraking": ForceResetRequiredAfterBraking = stf.ReadBoolBlock(false); break;
                    case "zeroselectedspeedwhenpassingtothrottlemode": ZeroSelectedSpeedWhenPassingToThrottleMode = stf.ReadBoolBlock(false); break;
                    case "dynamicbrakecommandhaspriorityovercruisecontrol": DynamicBrakeCommandHasPriorityOverCruiseControl = stf.ReadBoolBlock(true); break;
                    case "trainbrakecommandhaspriorityovercruisecontrol": TrainBrakeCommandHasPriorityOverCruiseControl = stf.ReadBoolBlock(true); break;
                    case "trainbrakecommandhaspriorityoveracceleratingcruisecontrol": TrainBrakeCommandHasPriorityOverAcceleratingCruiseControl = stf.ReadBoolBlock(true); break;
                    case "hasindependentthrottledynamicbrakelever": HasIndependentThrottleDynamicBrakeLever = stf.ReadBoolBlock(false); break;
                    case "hasproportionalspeedselector": HasProportionalSpeedSelector = stf.ReadBoolBlock(false); break;
                    case "speedselectorisdiscrete": SpeedSelectorIsDiscrete = stf.ReadBoolBlock(false); break;
                    case "usetrainbrakeanddynbrake": UseTrainBrakeAndDynBrake = stf.ReadBoolBlock(false); break;
                    case "usedynbrake": UseDynBrake = stf.ReadBoolBlock(false); break;
                    case "speeddeltatoenabletrainbrake": SpeedDeltaToEnableTrainBrake = stf.ReadFloatBlock(STFReader.UNITS.Speed, 5f); break;
                    case "minimumspeedforcceffect": MinimumSpeedForCCEffectMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, 0f); break;
                    case "trainbrakeminpercentvalue": TrainBrakeMinPercentValue = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.3f); break;
                    case "trainbrakemaxpercentvalue": TrainBrakeMaxPercentValue = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.85f); break;
                    case "startinautomode": StartInAutoMode = stf.ReadBoolBlock(false); break;
                    case "throttleneutralposition": ThrottleNeutralPosition = stf.ReadBoolBlock(false); break;
                    case "modeswitchallowedwiththrottlenotatzero": ModeSwitchAllowedWithThrottleNotAtZero = stf.ReadBoolBlock(false); break;
                    case "docomputenumberofaxles": DoComputeNumberOfAxles = stf.ReadBoolBlock(false); break;
                    case "deltaaccelerationexponent": DeltaAccelerationExponent = stf.ReadFloatBlock(STFReader.UNITS.Any, 1f); break;
                    case "options":
                        foreach (var speedRegulatorOption in stf.ReadStringBlock("").ToLower().Replace(" ", "").Split(','))
                        {
                            SpeedRegulatorOptions.Add(speedRegulatorOption.ToLower());
                        }
                        break;
                    case "controllercruisecontrollogic":
                    {
                        String speedControlLogic = stf.ReadStringBlock("none").ToLower();
                        switch (speedControlLogic)
                        {
                            case "full":
                                {
                                    CruiseControlLogic = ControllerCruiseControlLogic.Full;
                                    break;
                                }
                            case "speedonly":
                                {
                                    CruiseControlLogic = ControllerCruiseControlLogic.SpeedOnly;
                                    break;
                                }
                        }
                        break;
                    }
                    case "throttlepid":
                    {
                        stf.MustMatch("(");
                        float p = stf.ReadFloat(STFReader.UNITS.None, 1f);
                        float i = stf.ReadFloat(STFReader.UNITS.None, 0.5f);
                        float d = stf.ReadFloat(STFReader.UNITS.None, 0f);
                        ThrottlePID = new AccelerationController(p, i, d);
                        stf.SkipRestOfBlock();
                        break;
                    }
                    case "dynamicbrakepid":
                    {
                        stf.MustMatch("(");
                        float p = stf.ReadFloat(STFReader.UNITS.None, 1f);
                        float i = stf.ReadFloat(STFReader.UNITS.None, 0.5f);
                        float d = stf.ReadFloat(STFReader.UNITS.None, 0f);
                        DynamicBrakePID = new AccelerationController(p, i, d);
                        stf.SkipRestOfBlock();
                        break;
                    }
                    case "trainbrakepid":
                    {
                        stf.MustMatch("(");
                        float p = stf.ReadFloat(STFReader.UNITS.None, 1f);
                        float i = stf.ReadFloat(STFReader.UNITS.None, 0.5f);
                        float d = stf.ReadFloat(STFReader.UNITS.None, 0f);
                        TrainBrakePID = new AccelerationController(p, i, d);
                        stf.SkipRestOfBlock();
                        break;
                    }
                    case "(": stf.SkipRestOfBlock(); break;
                    default: break;
                }
            }
        }

        public CruiseControl Clone(MSTSLocomotive locomotive)
        {
            return new CruiseControl(this, locomotive);
        }

        public void Initialize()
        {
            if (DynamicBrakeFullRangeIncreaseTimeSeconds == 0)
                DynamicBrakeFullRangeIncreaseTimeSeconds = 4;
            if (DynamicBrakeFullRangeDecreaseTimeSeconds == 0)
                DynamicBrakeFullRangeDecreaseTimeSeconds = 6;
            
            ComputeNumberOfAxles();
            if (StartReducingSpeedDelta == 0)
            {
                StartReducingSpeedDelta = 1 / ThrottleFullRangeDecreaseTimeSeconds;
                if (StartReducingSpeedDeltaDownwards == 0) StartReducingSpeedDeltaDownwards = 1 / DynamicBrakeFullRangeDecreaseTimeSeconds;
            }
            if (StartReducingSpeedDeltaDownwards == 0) StartReducingSpeedDeltaDownwards = StartReducingSpeedDelta;

            if (SpeedDeltaToStartBrakingMpS > SpeedDeltaBrakingOffsetMpS) SpeedDeltaToStartBrakingMpS = SpeedDeltaBrakingOffsetMpS;
            if (SpeedDeltaToStopBrakingMpS > SpeedDeltaBrakingOffsetMpS) SpeedDeltaToStopBrakingMpS = SpeedDeltaBrakingOffsetMpS;
            if (SpeedDeltaToStartAcceleratingMpS < SpeedDeltaAcceleratingOffsetMpS) SpeedDeltaToStartAcceleratingMpS = SpeedDeltaAcceleratingOffsetMpS;
            if (SpeedDeltaToStopAcceleratingMpS < SpeedDeltaAcceleratingOffsetMpS) SpeedDeltaToStopAcceleratingMpS = SpeedDeltaAcceleratingOffsetMpS;
            if (SpeedDeltaToStartAcceleratingMpS < SpeedDeltaToStopAcceleratingMpS) SpeedDeltaToStopAcceleratingMpS = SpeedDeltaToStartAcceleratingMpS;
            if (SpeedDeltaToStartBrakingMpS > SpeedDeltaToStopBrakingMpS) SpeedDeltaToStopBrakingMpS = SpeedDeltaToStartBrakingMpS;

            if (StartInAutoMode) SpeedRegMode = SpeedRegulatorMode.Auto;

            if (UseThrottleAsForceSelector)
            {
                MaxForceSelectorController = Locomotive.ThrottleController;
            }
            else if (MaxForceSelectorController == null)
            {
                var notches = new List<MSTSNotch>();
                if (MaxForceSelectorIsDiscrete)
                {
                    float numNotches = SpeedRegulatorMaxForceSteps;
                    for (int i=DisableZeroForceStep ? 1 : 0; i<=numNotches; i++)
                    {
                        notches.Add(new MSTSNotch(i / numNotches, false, 0));
                    }
                }
                else
                {
                    notches.Add(new MSTSNotch(0, true, 0));
                }
                MaxForceSelectorController = new MSTSNotchController(notches)
                {
                    MinimumValue = 0,
                    MaximumValue = 1,
                    StepSize = 0.2f
                };
                if (SpeedRegulatorMaxForceSteps > 0) MaxForceSelectorController.SetPercent(defaultMaxAccelerationStep / SpeedRegulatorMaxForceSteps * 100);
            }
            if (UseThrottleAsSpeedSelector)
            {
                SpeedSelectorController = Locomotive.ThrottleController;
            }
            else if (SpeedSelectorController == null)
            {
                var notches = new List<MSTSNotch>();
                if (SpeedSelectorIsDiscrete)
                {
                    float numNotches = (float)Math.Round(Locomotive.MaxSpeedMpS / SpeedRegulatorNominalSpeedStepMpS);
                    for (int i=0; i<=numNotches; i++)
                    {
                        notches.Add(new MSTSNotch(i / numNotches, false, 0));
                    }
                }
                else
                {
                    notches.Add(new MSTSNotch(0, true, 0));
                }
                SpeedSelectorController = new MSTSNotchController(notches)
                {
                    MinimumValue = 0,
                    MaximumValue = 1,
                    StepSize = SpeedRegulatorNominalSpeedStepMpS / Locomotive.MaxSpeedMpS / SpeedSelectorStepTimeSeconds,
                };
                SpeedSelectorController.SetValue(0);
            }
            if (ThrottlePID == null) ThrottlePID = new AccelerationController(1, 1 / ThrottleFullRangeIncreaseTimeSeconds);
            if (DynamicBrakePID == null) DynamicBrakePID = new AccelerationController(1, 1 / DynamicBrakeFullRangeIncreaseTimeSeconds);
            if (TrainBrakePID == null) TrainBrakePID = new AccelerationController(1, 1 / TrainBrakeFullRangeIncreaseTimeSeconds);
        }

        private void ComputeNumberOfAxles()
        {
            if (DoComputeNumberOfAxles && (TrainCar)Locomotive == Simulator.PlayerLocomotive)
            {
                SelectedNumberOfAxles = 0;
                foreach (TrainCar tc in Locomotive.Train.Cars)
                {
                    SelectedNumberOfAxles += tc.WheelAxles.Count;
                }
            }
        }

        bool IsActive = false;
        public void Update(float elapsedClockSeconds)
        {
            if (!Locomotive.IsPlayerTrain || Locomotive != Locomotive.Train.LeadLocomotive)
            {
                WasForceReset = false;
                CCThrottleOrDynBrakePercent = 0;
                ThrottlePercent = null;
                DynamicBrakePercent = null;
                TrainBrakePercent = 0;
                EngineBrakePercent = 0;
                return;
            }
            if (firstIteration) // if this is executed the first time, let's check all other than player engines in the consist, and record them for further throttle manipulation
            {
                if (!DoComputeNumberOfAxles) SelectedNumberOfAxles = (int)(Locomotive.Train.Length / 6.6f); // also set the axles, for better delta computing, if user omits to set it

                firstIteration = false;
            }

            if (RestrictedRegionOdometer.Triggered)
            {
                RestrictedRegionOdometer.Stop();
                Simulator.Confirmer.Confirm(CabControl.RestrictedSpeedZone, CabSetting.Off);
                Locomotive.SignalEvent(Common.Event.CruiseControlAlert);
            }
            var prevForceSelectorValue = MaxForceSelectorController.CurrentValue;
            var prevSpeedSelectorValue = SpeedSelectorController.CurrentValue;
            MaxForceSelectorController.Update(elapsedClockSeconds);
            SpeedSelectorController.Update(elapsedClockSeconds);
            if (prevSpeedSelectorValue > 0 && SpeedSelectorController.CurrentValue == 0) Locomotive.SignalEvent(Common.Event.LeverToZero);
            if ((!UseThrottleAsForceSelector || SpeedRegMode == SpeedRegulatorMode.Auto) && MaxForceSelectorController.UpdateValue != 0.0)
            {
                Simulator.Confirmer.UpdateWithPerCent(
                    CabControl.MaxAcceleration,
                    MaxForceSelectorController.UpdateValue > 0 ? CabSetting.Increase : CabSetting.Decrease,
                    MaxForceSelectorController.CurrentValue * 100);
            }
            if ((!UseThrottleAsSpeedSelector || SpeedRegMode == SpeedRegulatorMode.Auto) && SpeedSelectorController.UpdateValue != 0.0)
            {
                ConfirmSelectedSpeed();
            }

            UpdateSpeedRegulatorModeChanges();

            float throttle = Locomotive.ThrottleController.CurrentValue;
            float dynamic = (Locomotive.DynamicBrakeController?.CurrentValue ?? 0);

            DynamicBrakePriority = DynamicBrakeCommandHasPriorityOverCruiseControl && dynamic > 0 && SpeedRegMode == SpeedRegulatorMode.Auto;
            var brakeController = Locomotive.TrainBrakeController.TrainBrakeControllerState;
            if (brakeController == ControllerState.Release ||
                brakeController == ControllerState.Neutral ||
                brakeController == ControllerState.FullQuickRelease ||
                brakeController == ControllerState.Overcharge)
            {
                TrainBrakePriority = false;
            }
            if (SpeedRegMode == SpeedRegulatorMode.Manual)
            {
                if (SelectedMaxAccelerationPercent == 0 && throttle == 0 && dynamic == 0) WasForceReset = true;
                else WasForceReset = false;
                CCThrottleOrDynBrakePercent = 0;
                ThrottlePercent = null;
                DynamicBrakePercent = null;
                TrainBrakePercent = 0;
                EngineBrakePercent = 0;
            }
            else if (SpeedRegMode == SpeedRegulatorMode.Auto)
            {
                if ((SpeedSelMode == SpeedSelectorMode.On || SpeedSelMode == SpeedSelectorMode.Start) && !TrainBrakePriority)
                {
                    if (Locomotive.AbsSpeedMpS == 0)
                    {
                        timeFromEngineMoved = 0;
                        reducingForce = true;
                    }
                    else if (reducingForce)
                    {
                        timeFromEngineMoved += elapsedClockSeconds;
                        float timeToReduce = Locomotive.SelectedTrainType == MSTSLocomotive.TrainType.Pax ? PowerReductionDelayPaxTrain : PowerReductionDelayCargoTrain;
                        if (timeFromEngineMoved > timeToReduce)
                            reducingForce = false;
                    }
                }
                else
                {
                    timeFromEngineMoved = 0;
                    reducingForce = true;
                }
                if (SpeedRegulatorOptions.Contains("engageforceonnonzerospeed") && SetSpeedMpS > 0 && (SpeedSelMode != SpeedSelectorMode.Parking || !ForceResetRequiredAfterBraking || (WasForceReset && SelectedMaxAccelerationPercent > 0)))
                {
                    SpeedSelMode = SpeedSelectorMode.On;
                    reducingForce = false;
                }
                else if (SpeedRegulatorOptions.Contains("engageparkingonzerospeed") && Math.Max(AbsWheelSpeedMpS, SetSpeedMpS) <= ParkingBrakeEngageSpeedMpS && SpeedSelMode != SpeedSelectorMode.Parking)
                {
                    SpeedSelMode = SpeedSelectorMode.Parking;
                    WasForceReset = false;
                }
                if (TrainBrakePriority)
                {
                    WasForceReset = false;
                    WasBraking = true;
                }
                else if (DynamicBrakePriority)
                {
                    WasForceReset = false;
                }
                else if (Locomotive.TrainBrakeController.TCSEmergencyBraking || Locomotive.TrainBrakeController.TCSFullServiceBraking)
                {
                    WasBraking = true;
                }
                else if (SpeedSelMode == SpeedSelectorMode.Start)
                {
                    WasForceReset = true;
                }
                else if (SelectedMaxAccelerationPercent == 0 || ModeSwitchAllowedWithThrottleNotAtZero && UseThrottleAsForceSelector)
                {
                    WasBraking = false;
                    WasForceReset = true;
                }
                if (Locomotive.TrainBrakeController.MaxPressurePSI - Locomotive.BrakeSystem.BrakeLine1PressurePSI < 1 && Locomotive.Train.BrakeLine4 <= 0)
                {
                    if (TrainBrakePercent == 0) CCIsUsingTrainBrake = false;
                }
                else if (!CCIsUsingTrainBrake && TrainBrakePriority && (TrainBrakeCommandHasPriorityOverAcceleratingCruiseControl && (CCThrottleOrDynBrakePercent > 0 || TrainBrakeCommandHasPriorityOverCruiseControl)))
                {
                    reducingForce = true;
                    timeFromEngineMoved = 0;
                }
                bool tractionAllowed = (AbsWheelSpeedMpS > SafeSpeedForAutomaticOperationMpS || SpeedSelMode == SpeedSelectorMode.Start || SpeedRegulatorOptions.Contains("startfromzero")) && SpeedSelMode != SpeedSelectorMode.Neutral;
                tractionAllowed &= Locomotive.Direction != Direction.N;
                bool brakingAllowed = true;
                bool atoBrakingAllowed = ATOSetSpeedMpS == SetSpeedMpS;
                if (ForceResetRequiredAfterBraking && (!WasForceReset || WasBraking && SelectedMaxAccelerationPercent > 0))
                {
                    tractionAllowed = false;
                    if (TrainBrakeCommandHasPriorityOverCruiseControl) brakingAllowed = atoBrakingAllowed;
                }
                if (TrainBrakePriority)
                {
                    tractionAllowed = false;
                    if (TrainBrakeCommandHasPriorityOverCruiseControl) brakingAllowed = atoBrakingAllowed;
                }
                if (DynamicBrakePriority)
                {
                    tractionAllowed = false;
                    brakingAllowed = atoBrakingAllowed;
                }
                if (SelectedMaxAccelerationPercent == 0)
                {
                    tractionAllowed = false;
                    brakingAllowed = atoBrakingAllowed;
                }
                if (ThrottleNeutralPosition && SelectedSpeedMpS == 0)
                {
                    tractionAllowed = false;
                    brakingAllowed = false;
                }
                if (Locomotive.TrainBrakeController.TCSEmergencyBraking || Locomotive.TrainBrakeController.TCSFullServiceBraking)
                {
                    tractionAllowed = false;
                    brakingAllowed = false;
                }
                if (SpeedSelMode == SpeedSelectorMode.Parking && !EngineBrakePriority)
                {
                    tractionAllowed = false;
                    brakingAllowed = false;
                }
                if (brakingAllowed)
                {
                    UpdateRequiredForce(elapsedClockSeconds, tractionAllowed);

                    if (CCThrottleOrDynBrakePercent > 0 && !tractionAllowed) CCThrottleOrDynBrakePercent = 0;
                    if (AbsWheelSpeedMpS == 0 && CCThrottleOrDynBrakePercent < 0) CCThrottleOrDynBrakePercent = 0;
                    CCThrottleOrDynBrakePercent = MathHelper.Clamp(CCThrottleOrDynBrakePercent, -100, 100);
                    TrainBrakePercent = MathHelper.Clamp(TrainBrakePercent, 0, 100);

                    if (TrainBrakePercent > 0) CCIsUsingTrainBrake = true;
                }
                else
                {
                    CCThrottleOrDynBrakePercent = 0;
                    TrainBrakePercent = 0;
                    ThrottlePID.Active = false;
                    DynamicBrakePID.Active = false;
                    TrainBrakePID.Active = false;
                    CCIsUsingTrainBrake = false;
                }
                ThrottlePercent = Math.Max(CCThrottleOrDynBrakePercent, 0);
                if (DynamicBrakePriority) DynamicBrakePercent = null;
                else if (CCThrottleOrDynBrakePercent < 0) DynamicBrakePercent = -CCThrottleOrDynBrakePercent;
                else DynamicBrakePercent = -1;
                if (SpeedSelMode == SpeedSelectorMode.Parking && !EngineBrakePriority)
                {
                    if (Locomotive.AbsWheelSpeedMpS <= ParkingBrakeEngageSpeedMpS)
                        EngineBrakePercent = ParkingBrakePercent;
                }
                else
                {
                    EngineBrakePercent = 0;
                }
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(CurrentSelectedSpeedMpS);
            outf.Write(RestrictedRegionOdometer.Started);
            outf.Write(RestrictedRegionOdometer.RemainingValue);
            MaxForceSelectorController.Save(outf);
            outf.Write(SelectedNumberOfAxles);
            outf.Write(SelectedSpeedMpS);
            outf.Write(DynamicBrakePriority);
            outf.Write((int)SpeedRegMode);
            outf.Write((int)SpeedSelMode);
            outf.Write(TrainBrakePercent);
            outf.Write(TrainLengthMeters);
            outf.Write(CCIsUsingTrainBrake);
        }

        public void Restore(BinaryReader inf)
        {
            CurrentSelectedSpeedMpS = inf.ReadSingle();
            bool started = inf.ReadBoolean();
            RestrictedRegionOdometer.Setup(inf.ReadSingle());
            if (started) RestrictedRegionOdometer.Start();
            MaxForceSelectorController.Restore(inf);
            SelectedNumberOfAxles = inf.ReadInt32();
            SelectedSpeedMpS = inf.ReadSingle();
            DynamicBrakePriority = inf.ReadBoolean();
            SpeedRegMode = (SpeedRegulatorMode)inf.ReadInt32();
            SpeedSelMode = (SpeedSelectorMode)inf.ReadInt32();
            TrainBrakePercent = inf.ReadSingle();
            TrainLengthMeters = inf.ReadInt32();
            CCIsUsingTrainBrake = inf.ReadBoolean();
        }

        public void UpdateSpeedRegulatorModeChanges()
        {
            var prevMode = SpeedRegMode;
            float throttle = Locomotive.ThrottleController.CurrentValue;
            float dynamic = (Locomotive.DynamicBrakeController?.CurrentValue ?? 0);
            if (DisableCruiseControlOnThrottleAndZeroSpeed)
            {
                if (throttle > 0 && Locomotive.AbsSpeedMpS == 0)
                {
                    SpeedRegMode = SpeedRegulatorMode.Manual;
                }
            }
            if (DisableCruiseControlOnThrottleAndZeroForce)
            {
                if ((throttle > 0 || UseThrottleAsForceSelector) && SelectedMaxAccelerationPercent == 0)
                {
                    SpeedRegMode = SpeedRegulatorMode.Manual;
                }
            }
            if (DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed)
            {
                if ((throttle > 0 || UseThrottleAsForceSelector) && SelectedMaxAccelerationPercent == 0 && SelectedSpeedMpS == 0)
                {
                    SpeedRegMode = SpeedRegulatorMode.Manual;
                }
            }
            if (ForceRegulatorAutoWhenNonZeroSpeedSelected)
            {
                if (SelectedSpeedMpS == 0 && (!ATOSpeedTakesPriorityOverSpeedSelector || !ATOSetSpeedMpS.HasValue))
                {
                    SpeedRegMode = SpeedRegulatorMode.Manual;
                }
                else
                {
                    SpeedRegMode = SpeedRegulatorMode.Auto;
                }
            }
            if (ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero)
            {
                if (SelectedSpeedMpS > 0 && throttle == 0 && dynamic == 0 &&
                    SelectedMaxAccelerationPercent == 0 && DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed)
                {
                    SpeedRegMode = SpeedRegulatorMode.Auto;
                }
            }
            if (ForceRegulatorAutoWhenNonZeroForceSelected)
            {
                if (SelectedMaxAccelerationPercent > 0 && DisableCruiseControlOnThrottleAndZeroForce && (throttle == 0 || UseThrottleAsForceSelector) && dynamic == 0)
                {
                    SpeedRegMode = SpeedRegulatorMode.Auto;
                }
            }
            if (prevMode != SpeedRegMode)
            {
                if (ZeroSelectedSpeedWhenPassingToThrottleMode && SpeedRegMode == SpeedRegulatorMode.Manual) SelectedSpeedMpS = 0;
            }
        }

        public void SpeedRegulatorModeIncrease()
        {
            if (!Locomotive.IsPlayerTrain) return;
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedRegulator);
            SpeedRegulatorMode previousMode = SpeedRegMode;
            if (SpeedRegMode == SpeedRegulatorMode.Testing) return;
            if (SpeedRegMode == SpeedRegulatorMode.Manual && (
               (!ModeSwitchAllowedWithThrottleNotAtZero && (Locomotive.ThrottleController.CurrentValue > 0 || Locomotive.DynamicBrakeController.CurrentValue > 0)) ||
               (DisableManualSwitchToAutoWhenSetSpeedNotAtTop && SelectedSpeedMpS < Locomotive.MaxSpeedMpS && Locomotive.AbsSpeedMpS > Simulator.MaxStoppedMpS)))
                return;
            bool test = false;
            while (!test)
            {
                SpeedRegMode++;
                switch (SpeedRegMode)
                {
                    case SpeedRegulatorMode.Auto:
                        {
                            if (SpeedRegulatorOptions.Contains("regulatorauto")) test = true;
                            if (!DisableManualSwitchToAutoWhenSetSpeedNotAtTop && !KeepSelectedSpeedWhenManualModeSet && !UseThrottleAsSpeedSelector) SelectedSpeedMpS = Locomotive.AbsSpeedMpS;
                            break;
                        }
                    case SpeedRegulatorMode.Testing: if (SpeedRegulatorOptions.Contains("regulatortest")) test = true; break;
                }
                if (!test && SpeedRegMode == SpeedRegulatorMode.Testing) // if we're here, then it means no higher option, return to previous state and get out
                {
                    SpeedRegMode = previousMode;
                    return;
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Speed regulator mode changed to {0}", Simulator.Catalog.GetString(SpeedRegMode.ToString())));
        }
        public void SpeedRegulatorModeDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedRegulator);
            if (SpeedRegMode == SpeedRegulatorMode.Manual) return;
            if (SpeedRegMode == SpeedRegulatorMode.Auto &&
                !ModeSwitchAllowedWithThrottleNotAtZero &&
                (SelectedMaxAccelerationPercent != 0 && !UseThrottleAsSpeedSelector || SelectedSpeedMpS > 0 && UseThrottleAsSpeedSelector))
                return;
            bool test = false;
            while (!test)
            {
                SpeedRegMode--;
                switch (SpeedRegMode)
                {
                    case SpeedRegulatorMode.Auto: if (SpeedRegulatorOptions.Contains("regulatorauto")) test = true; break;
                    case SpeedRegulatorMode.Manual:
                        {
                            if (!ModeSwitchAllowedWithThrottleNotAtZero)
                              Locomotive.ThrottleController.SetPercent(0);
                            if (SpeedRegulatorOptions.Contains("regulatormanual")) test = true;
                            if (ZeroSelectedSpeedWhenPassingToThrottleMode) SelectedSpeedMpS = 0;
                            break;
                        }
                }
                if (!test && SpeedRegMode == SpeedRegulatorMode.Manual)
                    return;
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Speed regulator mode changed to {0}", Simulator.Catalog.GetString(SpeedRegMode.ToString())));
        }
        public void SpeedSelectorModeStartIncrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedSelector);
            if (SpeedSelMode == SpeedSelectorMode.Start) return;
            bool test = false;
            while (!test)
            {
                SpeedSelMode++;
                switch (SpeedSelMode)
                {
                    case SpeedSelectorMode.Neutral: if (SpeedRegulatorOptions.Contains("selectorneutral")) test = true; break;
                    case SpeedSelectorMode.On: if (SpeedRegulatorOptions.Contains("selectoron")) test = true; break;
                    case SpeedSelectorMode.Start: if (SpeedRegulatorOptions.Contains("selectorstart")) test = true; break;
                }
                if (!test && SpeedSelMode == SpeedSelectorMode.Start)
                    return;
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed selector mode changed to") + " " + Simulator.Catalog.GetString(SpeedSelMode.ToString()));
        }
        public void SpeedSelectorModeStopIncrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedSelector);
            if (SpeedSelMode == SpeedSelectorMode.Start)
            {
                bool test = false;
                while (!test)
                {
                    SpeedSelMode--;
                    switch (SpeedSelMode)
                    {
                        case SpeedSelectorMode.On: if (SpeedRegulatorOptions.Contains("selectoron")) test = true; break;
                        case SpeedSelectorMode.Neutral: if (SpeedRegulatorOptions.Contains("selectorneutral")) test = true; break;
                        case SpeedSelectorMode.Parking: if (SpeedRegulatorOptions.Contains("selectorparking")) test = true; break;
                    }
                    if (!test && SpeedSelMode == SpeedSelectorMode.Parking && !EngineBrakePriority)
                        return;
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Speed selector mode changed to {0}", Simulator.Catalog.GetString(SpeedSelMode.ToString())));
        }
        public void SpeedSelectorModeDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedSelector);
            SpeedSelectorMode previousMode = SpeedSelMode;
            if (SpeedSelMode == SpeedSelectorMode.Parking && !EngineBrakePriority) return;
            bool test = false;
            while (!test)
            {
                SpeedSelMode--;
                switch (SpeedSelMode)
                {
                    case SpeedSelectorMode.On: if (SpeedRegulatorOptions.Contains("selectoron")) test = true; break;
                    case SpeedSelectorMode.Neutral: if (SpeedRegulatorOptions.Contains("selectorneutral")) test = true; break;
                    case SpeedSelectorMode.Parking: if (SpeedRegulatorOptions.Contains("selectorparking")) test = true; break;
                }
                if (!test && SpeedSelMode == SpeedSelectorMode.Parking && !EngineBrakePriority)
                {
                    SpeedSelMode = previousMode;
                    return;
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Speed selector mode changed to {0}", Simulator.Catalog.GetString(SpeedSelMode.ToString())));
        }

        public void SetMaxForcePercent(float percent)
        {
            percent = MathHelper.Clamp(percent, 0, 100);
            if (SelectedMaxAccelerationPercent == percent) return;
            MaxForceSelectorController.SetPercent(percent);
            Simulator.Confirmer.ConfirmWithPerCent(CabControl.MaxAcceleration, percent);
        }

        public void SpeedRegulatorMaxForceStartIncrease()
        {
            if (MaxForceSelectorController.CurrentValue == 0)
            {
                Locomotive.SignalEvent(Common.Event.LeverFromZero);
            }
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            MaxForceSelectorController.StartIncrease();
        }
        public void SpeedRegulatorMaxForceStopIncrease()
        {
            MaxForceSelectorController.StopDecrease();
        }

        public void SpeedRegulatorMaxForceStartDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            MaxForceSelectorController.StartDecrease();
        }
        public void SpeedRegulatorMaxForceStopDecrease()
        {
            MaxForceSelectorController.StopDecrease();
        }
        public void SpeedRegulatorMaxForceStartToZero(float? target)
        {
            if (MaxForceSelectorController.CurrentValue <= MaxForceSelectorController.MinimumValue)
                return;

            MaxForceSelectorController.StartDecrease(target, true);
            if (MaxForceSelectorController.NotchCount() <= 0) Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
        }
        public void SpeedRegulatorMaxForceChangeByMouse(float value)
        {
            var controller = MaxForceSelectorController;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (change != 0)
            {
                Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            }
            if (oldValue != controller.IntermediateValue)
                Simulator.Confirmer.UpdateWithPerCent(
                    CabControl.MaxAcceleration,
                    oldValue < controller.IntermediateValue ? CabSetting.Increase : CabSetting.Decrease,
                    controller.CurrentValue * 100);
        }
        public void SpeedRegulatorSelectedSpeedStartIncrease()
        {
            var mpc = Locomotive.MultiPositionControllers.Where(x => 
                x.controllerBinding == ControllerBinding.SelectedSpeed && !x.StateChanged).FirstOrDefault();
            if (mpc != null)
            {
                mpc.StateChanged = true;
                if (SpeedRegMode != SpeedRegulatorMode.Auto && (ForceRegulatorAutoWhenNonZeroSpeedSelected ||
                    SelectedMaxAccelerationPercent == 0 && DisableCruiseControlOnThrottleAndZeroForce && ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero &&
                    Locomotive.ThrottleController.CurrentValue == 0 && (Locomotive.DynamicBrakeController?.CurrentValue ?? 0) == 0))
                {
                    SpeedRegMode = SpeedRegulatorMode.Auto;
                }

                mpc.DoMovement(MultiPositionController.Movement.Forward);
                return;
            }
            if (SpeedSelectorController.CurrentValue == 0)
            {
                Locomotive.SignalEvent(Common.Event.LeverFromZero);
            }
            if (UseThrottleAsSpeedSelector || HasProportionalSpeedSelector || (UseThrottleAsForceSelector && mpc == null ))
                SpeedSelectorController.StartIncrease();
            else
                SpeedSelectorModeStartIncrease();
        }
        public void SpeedRegulatorSelectedSpeedStopIncrease()
        {
            var mpc = Locomotive.MultiPositionControllers.Where(x => x.controllerBinding == ControllerBinding.SelectedSpeed).FirstOrDefault();
            if (mpc != null)
            {
                mpc.StateChanged = false;
                mpc.DoMovement(Controllers.MultiPositionController.Movement.Neutral);
                return;
            }
            if (UseThrottleAsSpeedSelector || HasProportionalSpeedSelector || (UseThrottleAsForceSelector && mpc == null))
                SpeedSelectorController.StopIncrease();
            else
                SpeedSelectorModeStopIncrease();
        }
        public void SpeedRegulatorSelectedSpeedStartDecrease()
        {
            var mpc = Locomotive.MultiPositionControllers.Where(x => x.controllerBinding == ControllerBinding.SelectedSpeed && !x.StateChanged).FirstOrDefault();
            if (mpc != null)
            {
                mpc.StateChanged = true;
                mpc.DoMovement(Controllers.MultiPositionController.Movement.Aft);
                return;
            }
            if (UseThrottleAsSpeedSelector || HasProportionalSpeedSelector || (UseThrottleAsForceSelector && mpc == null))
                SpeedSelectorController.StartDecrease();
            else
                SpeedSelectorModeDecrease();
        }
        public void SpeedRegulatorSelectedSpeedStopDecrease()
        {
            var mpc = Locomotive.MultiPositionControllers.Where(x => x.controllerBinding == ControllerBinding.SelectedSpeed).FirstOrDefault();
            if (mpc != null)
            {
                mpc.StateChanged = false;
                mpc.DoMovement(Controllers.MultiPositionController.Movement.Neutral);
                return;
            }
            SpeedSelectorController.StopDecrease();
        }

        public void SpeedRegulatorSelectedSpeedChangeByMouse(float value)
        {
            var controller = SpeedSelectorController;
            var oldValue = controller.IntermediateValue;
            var change = controller.SetValue(value);
            if (oldValue != controller.IntermediateValue)
            {
                ConfirmSelectedSpeed();
            }
        }

        public void NumerOfAxlesIncrease()
        {
            NumerOfAxlesIncrease(1);
        }
        public void NumerOfAxlesIncrease(int ByAmount)
        {
            SelectedNumberOfAxles += ByAmount;
            trainLength = SelectedNumberOfAxles * 6.6f;
            TrainLengthMeters = (int)Math.Round(trainLength + 0.5, 0);
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Number of axles increased to ") + SelectedNumberOfAxles.ToString());
        }
        public void NumberOfAxlesDecrease()
        {
            NumberOfAxlesDecrease(1);
        }
        public void NumberOfAxlesDecrease(int ByAmount)
        {
            if ((SelectedNumberOfAxles - ByAmount) < 1) return;
            SelectedNumberOfAxles -= ByAmount;
            trainLength = SelectedNumberOfAxles * 6.6f;
            TrainLengthMeters = (int)Math.Round(trainLength + 0.5, 0);
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Number of axles decreased to ") + SelectedNumberOfAxles.ToString());
        }
        public void ActivateRestrictedSpeedZone()
        {
            RestrictedRegionOdometer.Setup(TrainLengthMeters);
            if (!RestrictedRegionOdometer.Started) RestrictedRegionOdometer.Start();
            Simulator.Confirmer.Confirm(CabControl.RestrictedSpeedZone, CabSetting.On);
        }

        public void SetSpeed(float Speed)
        {
            if (SpeedRegMode == SpeedRegulatorMode.Manual && (ForceRegulatorAutoWhenNonZeroSpeedSelected || ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero))
                SpeedRegMode = SpeedRegulatorMode.Auto;
            if (SpeedRegMode == SpeedRegulatorMode.Manual)
                return;
            Locomotive.SignalEvent(Common.Event.CruiseControlAlert1);
            var requiredSpeedMpS = SpeedIsMph ? MpS.FromMpH(Speed) : MpS.FromKpH(Speed);
            if (MinimumSpeedForCCEffectMpS == 0)
                SelectedSpeedMpS = requiredSpeedMpS;
            else if (requiredSpeedMpS > SelectedSpeedMpS)
                SelectedSpeedMpS = Math.Max(requiredSpeedMpS, MinimumSpeedForCCEffectMpS);
            else if (requiredSpeedMpS < MinimumSpeedForCCEffectMpS)
                SelectedSpeedMpS = 0;
            else
                SelectedSpeedMpS = requiredSpeedMpS;
            if (SelectedSpeedMpS > Locomotive.MaxSpeedMpS)
                SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
            ConfirmSelectedSpeed();
        }
        private double selectedSpeedLeverHoldTime = 0;
        public void SpeedSelectorIncreaseStep()
        {
            if (SpeedSelectorController.CurrentValue >= 1) return;
            var time = Locomotive.Simulator.ClockTime;
            if (time >= selectedSpeedLeverHoldTime && time < selectedSpeedLeverHoldTime + SpeedSelectorStepTimeSeconds) return;
            selectedSpeedLeverHoldTime = time;

            if (SpeedSelectorController.CurrentValue > 0 || MinimumSpeedForCCEffectMpS == 0)
            {
                float speed = (SpeedSelectorController.CurrentValue * (Locomotive.MaxSpeedMpS - MinimumSpeedForCCEffectMpS) + MinimumSpeedForCCEffectMpS) + SpeedRegulatorNominalSpeedStepMpS;
                Console.WriteLine(speed);
                SelectedSpeedMpS = Math.Min((float)Math.Round(speed / SpeedRegulatorNominalSpeedStepMpS) * SpeedRegulatorNominalSpeedStepMpS, Locomotive.MaxSpeedMpS);
            }
            else
            {
                SelectedSpeedMpS = MinimumSpeedForCCEffectMpS;
            }
            ConfirmSelectedSpeed();
        }
        public void SpeedSelectorDecreaseStep()
        {
            if (SpeedSelectorController.CurrentValue <= 0) return;
            var time = Locomotive.Simulator.ClockTime;
            if (time >= selectedSpeedLeverHoldTime && time < selectedSpeedLeverHoldTime + SpeedSelectorStepTimeSeconds) return;
            selectedSpeedLeverHoldTime = time;

            float speed = SpeedSelectorController.CurrentValue * (Locomotive.MaxSpeedMpS - MinimumSpeedForCCEffectMpS) + MinimumSpeedForCCEffectMpS - SpeedRegulatorNominalSpeedStepMpS;
            if (speed < MinimumSpeedForCCEffectMpS)
            {
                SelectedSpeedMpS = 0;
            }
            else
            {
                SelectedSpeedMpS = Math.Max((float)Math.Round(speed / SpeedRegulatorNominalSpeedStepMpS) * SpeedRegulatorNominalSpeedStepMpS, MinimumSpeedForCCEffectMpS);
            }
            ConfirmSelectedSpeed();
        }
        public void ConfirmSelectedSpeed()
        {
            float val = SpeedSelectorController.CurrentValue;
            if (val > 0)
            {
                float min = MinimumSpeedForCCEffectMpS;
                float max = Locomotive.MaxSpeedMpS;
                val = val * (max - min) + min;
            }
            if (SpeedIsMph)
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Selected speed changed to {0} mph", Math.Round(MpS.FromMpS(val, false), 0, MidpointRounding.AwayFromZero).ToString()));
            else
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Selected speed changed to {0} km/h", Math.Round(MpS.FromMpS(val, true), 0, MidpointRounding.AwayFromZero).ToString()));
        }
        private float prevDemandedAccelerationMpSS;
        public void UpdateRequiredForce(float elapsedClockSeconds, bool tractionAllowed)
        {
            float deltaSpeedMpS = SetSpeedMpS - AbsWheelSpeedMpS;
            float totalTractionN = 0;
            float totalDynamicBrakeN = 0;
            float totalTrainBrakeN = 0;
            float totalMassKg = 0;
            foreach (var car in Locomotive.Train.Cars)
            {
                if (car is MSTSLocomotive locomotive)
                {
                    totalTractionN += locomotive.GetAvailableTractionForceN(1);
                    totalDynamicBrakeN += locomotive.GetAvailableDynamicBrakeForceN(1);
                }
                totalTrainBrakeN += car.FrictionBrakeBlendingMaxForceN; // TODO: consider changes with speed
                totalMassKg += car.MassKG;
            }
            MaxThrottleAccelerationMpSS = totalTractionN / totalMassKg;
            MaxDynamicBrakeDecelerationMpSS = totalDynamicBrakeN / totalMassKg;
            MaxTrainBrakeDecelerationMpSS = totalTrainBrakeN / totalMassKg;

            float demandedAccelerationMpSS = 0;
            if (deltaSpeedMpS < SpeedDeltaToStartBrakingMpS || (deltaSpeedMpS < SpeedDeltaToStopBrakingMpS && prevDemandedAccelerationMpSS < 0))
            {
                demandedAccelerationMpSS = (deltaSpeedMpS - SpeedDeltaBrakingOffsetMpS) * StartReducingSpeedDeltaDownwards;
                // Old algorithm: acceleration demand is proportional to the square root of delta speed
                // This achieves a linear-time reduction in acceleration/deceleration
                // a(v) = sqrt((vset-v)*srsd)
                // => v(t) = vset - (sqrt(v0-vi)-srsd*t/2)^2
                // => a(t) = - srsd * t
                // However, this means that near the set speed the algorithm oscillates, since small changes in speed
                // correspond to higher throttle demands:
                // da/dv = -srsd / 2 / sqrt((vset-v) * srsd) which diverges when v = vset
                if (DeltaAccelerationExponent == 0.5f)
                    demandedAccelerationMpSS = (float)-Math.Sqrt(-demandedAccelerationMpSS / 3);
            }
            else if (deltaSpeedMpS > SpeedDeltaToStartAcceleratingMpS || (deltaSpeedMpS > SpeedDeltaToStopAcceleratingMpS && prevDemandedAccelerationMpSS > 0))
            {
                demandedAccelerationMpSS = (deltaSpeedMpS - SpeedDeltaAcceleratingOffsetMpS) * StartReducingSpeedDelta;
                if (DeltaAccelerationExponent == 0.5f)
                    demandedAccelerationMpSS = (float)Math.Sqrt(demandedAccelerationMpSS);
            }
            prevDemandedAccelerationMpSS = demandedAccelerationMpSS;
            if (ATOSetSpeedMpS == SetSpeedMpS)
            {
                if (elapsedClockSeconds > 0)
                {
                    float rawAccel = MathHelper.Clamp((ATOSetSpeedMpS.Value - PrevATOSpeedMpS) / elapsedClockSeconds, -2, 2);
                    float filteredAccel = AccelerationFilter.Filter(rawAccel, elapsedClockSeconds);
                    if (Math.Abs(filteredAccel) < 0.05f) filteredAccel = 0;
                    if (filteredAccel > ATOAccelerationMpSS)
                    {
                        ATOAccelerationMpSS = Math.Min(filteredAccel, ATOAccelerationMpSS + 0.5f * elapsedClockSeconds);
                    }
                    else
                    {
                        ATOAccelerationMpSS = Math.Max(filteredAccel, ATOAccelerationMpSS - 0.5f * elapsedClockSeconds);
                    }
                }
            }
            else
            {
                ATOAccelerationMpSS = 0;
            }
            if (ATOAccelerationMpSS > 0)
            {
                demandedAccelerationMpSS = MathHelper.Clamp(demandedAccelerationMpSS + ATOAccelerationMpSS * ATOAccelerationFactor, -MaxDecelerationMpSS, MaxAccelerationMpSS + ATOAccelerationMpSS);
            }
            else
            {
                demandedAccelerationMpSS = MathHelper.Clamp(demandedAccelerationMpSS + ATOAccelerationMpSS * ATODecelerationFactor, -MaxDecelerationMpSS + ATOAccelerationMpSS, MaxAccelerationMpSS);
            }
            //PrevATOSpeedMpS = ATOSetSpeedMpS ?? 0;

            float maxThrottleAccelerationMpSS = MaxThrottleAccelerationMpSS;
            if (maxThrottleAccelerationMpSS < 0.01f)
            {
                float coeff = Math.Max(MpS.FromMpS(AbsWheelSpeedMpS, !SpeedIsMph) / 100 * 1.2f, 1);
                maxThrottleAccelerationMpSS = 1.5f / coeff;
            }
            float maxDynamicBrakeDecelerationMpSS = MaxDynamicBrakeDecelerationMpSS;
            if (maxDynamicBrakeDecelerationMpSS < 0.01f)
            {
                float coeff = Math.Max(MpS.FromMpS(AbsWheelSpeedMpS, !SpeedIsMph) / 100 * 1.2f, 1);
                maxDynamicBrakeDecelerationMpSS = 1.5f / coeff;
            }
            float maxTrainBrakeDecelerationMpSS = MaxTrainBrakeDecelerationMpSS;
            if (MaxTrainBrakeDecelerationMpSS < 0.01f)
            {
                maxTrainBrakeDecelerationMpSS = 1.5f;
            }
            ThrottlePID.Adjust(maxThrottleAccelerationMpSS);
            DynamicBrakePID.Adjust(maxDynamicBrakeDecelerationMpSS);
            TrainBrakePID.Adjust(maxTrainBrakeDecelerationMpSS);

            float targetThrottleAccelerationMpSS = demandedAccelerationMpSS;
            if (SelectedMaxAccelerationStep > 0 && AccelerationTable.Count >= SelectedMaxAccelerationStep && MpS.FromMpS(AbsWheelSpeedMpS, !SpeedIsMph) > 5)
            {
                float a = AccelerationTable[SelectedMaxAccelerationStep - 1];
                if (a > 0 && a < targetThrottleAccelerationMpSS) targetThrottleAccelerationMpSS = a;
            }
            float maxThrottlePercent = 0;
            if (tractionAllowed && DynamicBrakePID.Percent <= 0 && TrainBrakePID.Percent <= 0 && (demandedAccelerationMpSS > 0 || ThrottlePID.Percent > 0))
            {
                // Max throttle percent is determined by the force selector
                maxThrottlePercent = SelectedMaxAccelerationStep > 0 && ForceStepsThrottleTable.Count >= SelectedMaxAccelerationStep && !SpeedRegulatorMaxForcePercentUnits ? ForceStepsThrottleTable[SelectedMaxAccelerationStep - 1] : SelectedMaxAccelerationPercent;
                if (MaxPowerThreshold > 0)
                {
                    // Linearly increase max throttle percent until max force is available when speed reaches MaxPowerThreshold
                    float currentSpeed = MpS.FromMpS(AbsWheelSpeedMpS, !SpeedIsMph);
                    float overridePercent = (100 * currentSpeed) / MaxPowerThreshold;
                    maxThrottlePercent = Math.Max(Math.Min(overridePercent, 100), maxThrottlePercent);
                }
                if (reducingForce) maxThrottlePercent = Math.Min(maxThrottlePercent, PowerReductionValue);
            }
            ThrottlePID.Update(elapsedClockSeconds, targetThrottleAccelerationMpSS, RelativeAccelerationMpSS, 0, maxThrottlePercent);

            float minDynamicBrakePercent = 0;
            float maxDynamicBrakePercent = 0;
            if (Locomotive.DynamicBrakeAvailable && UseDynBrake && ThrottlePID.Percent <= 0 && (demandedAccelerationMpSS < 0 || DynamicBrakePID.Percent > 0))
            {
                // If train brake is active, we force dynamic brakes to stay active to give preference to brake release
                minDynamicBrakePercent = CCIsUsingTrainBrake ? MinDynamicBrakePercentWhileUsingTrainBrake : 0;
                maxDynamicBrakePercent = DynamicBrakeIsSelectedForceDependant ? SelectedMaxAccelerationPercent : 100;
            }
            DynamicBrakePID.Update(elapsedClockSeconds, -demandedAccelerationMpSS, -RelativeAccelerationMpSS, minDynamicBrakePercent, maxDynamicBrakePercent);
            float target = ThrottlePID.Percent - DynamicBrakePID.Percent;
            if (target > CCThrottleOrDynBrakePercent)
                IncreaseForce(ref CCThrottleOrDynBrakePercent, elapsedClockSeconds, target);
            else if (target < CCThrottleOrDynBrakePercent)
                DecreaseForce(ref CCThrottleOrDynBrakePercent, elapsedClockSeconds, target);
            UpdateTrainBrakePercent(elapsedClockSeconds, deltaSpeedMpS, demandedAccelerationMpSS);
        }
        void UpdateTrainBrakePercent(float elapsedClockSeconds, float deltaSpeedMpS, float demandedAccelerationMpSS)
        {
            float dynamicBrakeDecelerationMpSS = MaxDynamicBrakeDecelerationMpSS;
            bool enabled = false;
            if (demandedAccelerationMpSS < 0 && (UseTrainBrakeAndDynBrake || !Locomotive.DynamicBrakeAvailable))
            {
                bool dynamicBrakeAvailable = Locomotive.DynamicBrakeAvailable && Locomotive.LocomotivePowerSupply.DynamicBrakeAvailable && UseDynBrake;
                if (!dynamicBrakeAvailable || MaxDynamicBrakeDecelerationMpSS < MaxDecelerationMpSS * 0.1f)
                {
                    enabled = true;
                    dynamicBrakeDecelerationMpSS = 0;
                }
                else if (deltaSpeedMpS <= -SpeedDeltaToEnableTrainBrake || SpeedDeltaToEnableTrainBrake <= 0)
                {
                    // Otherwise, only enabled if dynamic brake cannot provide enough force
                    enabled = CCThrottleOrDynBrakePercent < -MinDynamicBrakePercentToEnableTrainBrake || TrainBrakePercent > 0;
                }
            }

            float decelerationDemandMpSS = -demandedAccelerationMpSS - dynamicBrakeDecelerationMpSS;
            // Since part of the demand is achieved with the dynamic brake, we substract from the real deceleration
            // the contribution which comes from dynamic brakes
            float correctedCurrentDecelerationMpSS = -RelativeAccelerationMpSS - dynamicBrakeDecelerationMpSS;
            TrainBrakePID.Update(elapsedClockSeconds, decelerationDemandMpSS, correctedCurrentDecelerationMpSS, 0, enabled ? TrainBrakeMaxPercentValue : 0);

            float target = TrainBrakePID.Percent;
            if (target <= TrainBrakeMinPercentValue) TrainBrakePercent = 0;
            else if (Math.Abs(TrainBrakePercent - target) > 5)
            {
                TrainBrakePercent = MathHelper.Clamp(target, TrainBrakeMinPercentValue, TrainBrakeMaxPercentValue);
            }
        }
        void IncreaseForce(ref float throttleOrDynPercent, float elapsedClockSeconds, float maxPercent)
        {
            if (throttleOrDynPercent < 0)
            {
                float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds * elapsedClockSeconds;
                throttleOrDynPercent = Math.Min(throttleOrDynPercent + step, Math.Min(maxPercent, 0));
            }
            else
            {
                float step = 100 / ThrottleFullRangeIncreaseTimeSeconds * elapsedClockSeconds;
                throttleOrDynPercent = Math.Min(throttleOrDynPercent + step, maxPercent);
            }
        }

        void DecreaseForce(ref float throttleOrDynPercent, float elapsedClockSeconds, float minPercent)
        {
            if (throttleOrDynPercent > 0)
            {
                float step = 100 / ThrottleFullRangeDecreaseTimeSeconds * elapsedClockSeconds;
                throttleOrDynPercent = Math.Max(throttleOrDynPercent - step, Math.Max(minPercent, 0));
            }
            else
            {
                float step = 100 / DynamicBrakeFullRangeIncreaseTimeSeconds * elapsedClockSeconds;
                throttleOrDynPercent = Math.Max(throttleOrDynPercent - step, minPercent);
            }
        }

        private float previousSelectedSpeed = 0;
        public float GetDataOf(CabViewControl cvc)
        {
            float data = 0;
            switch (cvc.ControlType.Type)
            {
                case CABViewControlTypes.ORTS_SELECTED_SPEED:
                case CABViewControlTypes.ORTS_SELECTED_SPEED_DISPLAY:
                    bool metric = cvc.Units == CABViewControlUnits.KM_PER_HOUR;
                    float temp = (float)Math.Round(MpS.FromMpS(SetSpeedMpS, metric));
                    if (previousSelectedSpeed < temp) previousSelectedSpeed += 1f;
                    if (previousSelectedSpeed > temp) previousSelectedSpeed -= 1f;
                    data = previousSelectedSpeed;
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_MODE:
                    data = (float)SpeedSelMode;
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_REGULATOR_MODE:
                    data = (float)SpeedRegMode;
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_SELECTOR:
                    metric = cvc.Units == CABViewControlUnits.KM_PER_HOUR;
                    data = (float)Math.Round(MpS.FromMpS(ControllerValueToSelectedSpeedMpS(SpeedSelectorController.CurrentValue), metric));
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_MAXIMUM_ACCELERATION:
                    if (SpeedRegMode == SpeedRegulatorMode.Auto || MaxForceKeepSelectedStepWhenManualModeSet)
                    {
                        data = SelectedMaxAccelerationPercent * (float)cvc.MaxValue / 100;
                    }
                    else
                        data = 0;
                    break;
                case CABViewControlTypes.ORTS_RESTRICTED_SPEED_ZONE_ACTIVE:
                    data = RestrictedRegionOdometer.Started ? 1 : 0;
                    break;
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DISPLAY_UNITS:
                    data = SelectedNumberOfAxles % 10;
                    break;
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DISPLAY_TENS:
                    data = (SelectedNumberOfAxles / 10) % 10;
                    break;
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DISPLAY_HUNDREDS:
                    data = (SelectedNumberOfAxles / 100) % 10;
                    break;
                case CABViewControlTypes.ORTS_TRAIN_LENGTH_METERS:
                    data = TrainLengthMeters;
                    break;
                case CABViewControlTypes.ORTS_REMAINING_TRAIN_LENGTH_SPEED_RESTRICTED:
                    data = RestrictedRegionOdometer.Started ? RestrictedRegionOdometer.RemainingValue : 0;
                    break;
                case CABViewControlTypes.ORTS_REMAINING_TRAIN_LENGTH_PERCENT:
                    if (SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto && TrainLengthMeters == 0)
                    {
                        data = RestrictedRegionOdometer.Started ? RestrictedRegionOdometer.RemainingValue / TrainLengthMeters : 0;
                    }
                    break;
                case CABViewControlTypes.ORTS_MOTIVE_FORCE:
                    data = Locomotive.TractiveForceN;
                    break;
                case CABViewControlTypes.ORTS_MOTIVE_FORCE_KILONEWTON:
                    if (Locomotive.TractionForceN >0)
                        data = (float)Math.Round(Locomotive.TractionForceN / 1000, 0);
                    else if (Locomotive.DynamicBrakeForceN > 0)
                        data = -(float)Math.Round(Locomotive.DynamicBrakeForceN / 1000, 0);
                    break;
                case CABViewControlTypes.ORTS_MAXIMUM_FORCE:
                    data = Locomotive.MaxForceN;
                    break;
                case CABViewControlTypes.ORTS_FORCE_IN_PERCENT_THROTTLE_AND_DYNAMIC_BRAKE:
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        data = Locomotive.ThrottlePercent;
                    }
                    else if (Locomotive.DynamicBrakePercent > 0 && Locomotive.AbsSpeedMpS > 0)
                    {
                        data = -Locomotive.DynamicBrakePercent;
                    }
                    else data = 0;
                    break;
                case CABViewControlTypes.ORTS_TRAIN_TYPE_PAX_OR_CARGO:
                    data = (int)Locomotive.SelectedTrainType;
                    break;
                case CABViewControlTypes.ORTS_CONTROLLER_VOLTAGE:
                    data = CCThrottleOrDynBrakePercent;
                    break;
                case CABViewControlTypes.ORTS_AMPERS_BY_CONTROLLER_VOLTAGE:
                    if (SpeedRegMode == SpeedRegulatorMode.Auto)
                    {
                        if (CCThrottleOrDynBrakePercent < 0) data = -CCThrottleOrDynBrakePercent / 100 * (Locomotive.MaxCurrentA * 0.8f);
                        else data = CCThrottleOrDynBrakePercent / 100 * (Locomotive.MaxCurrentA * 0.8f);
                        if (data == 0 && Locomotive.DynamicBrakePercent > 0 && Locomotive.AbsSpeedMpS > 0) data = Locomotive.DynamicBrakePercent / 100 * (Locomotive.MaxCurrentA * 0.8f);
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakePercent > 0 && Locomotive.AbsSpeedMpS > 0) data = Locomotive.DynamicBrakePercent / 200 * (Locomotive.MaxCurrentA * 0.8f);
                        else data = Locomotive.ThrottlePercent / 100 * (Locomotive.MaxCurrentA * 0.8f);
                    }
                    break;
                case CABViewControlTypes.ORTS_ACCELERATION_IN_TIME:
                    {
                        data = AccelerationBits;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SELECTED_SPEED:
                    data = SelectedSpeedPressed ? 1 : 0;
                    break;
                case CABViewControlTypes.ORTS_CC_SPEED_0:
                    {
                        data = Speed0Pressed ? 1 : 0;
                        break;
                    }
 				case CABViewControlTypes.ORTS_CC_SPEED_DELTA:
                    {
                        data = SpeedDeltaPressed ? 1 : 0;
                        break;
                    }
                default:
                    data = 0;
                    break;
            }
            return data;
        }
        protected float ControllerValueToSelectedSpeedMpS(float val)
        {
            if (val == 0)
                return 0;
            float min = MinimumSpeedForCCEffectMpS;
            float max = Locomotive.MaxSpeedMpS;
            return val * (max - min) + min;
        }

        public string GetCruiseControlStatus()
        {
            var cruiseControlStatus = SpeedRegMode.ToString();
            return cruiseControlStatus;
        }

        public enum ControllerCruiseControlLogic
        {
            None,
            Full,
            SpeedOnly
        }
    }
    public class AccelerationController
    {
        public float Percent { get; private set; }
        private float TotalError;
        private float LastTarget;
        private float LastError;
        private bool active;
        private readonly float[] Coefficients;
        public readonly float Granularity = 5;
        private float ProportionalFactor;
        private float IntegralFactor;
        private float DerivativeFactor;
        public float Tolerance;
        public bool Active
        {
            set
            {
                if (active != value)
                {
                    LastTarget = 0;
                    LastError = 0;
                    TotalError = 0;
                    Percent = 0;
                    active = value;
                }
            }
            get
            {
                return active;
            }
        }
        public AccelerationController(float p, float i, float d=0)
        {
            Coefficients = new float[] {100*p, 100*i, 100*d};
        }
        protected AccelerationController(AccelerationController o)
        {
            Coefficients = o.Coefficients;
        }
        public AccelerationController Clone()
        {
            return new AccelerationController(this);
        }
        public void Adjust(float maxAccelerationMpSS)
        {
            ProportionalFactor = Coefficients[0] / maxAccelerationMpSS;
            IntegralFactor = Coefficients[1] / maxAccelerationMpSS;
            DerivativeFactor = Coefficients[2] / maxAccelerationMpSS;
        }
        public void Update(float elapsedClockSeconds, float targetAccelerationMpSS, float currentAccelerationMpSS, float minPercent = 0, float maxPercent = 100)
        {
            if (!Active) Active = true;
            float error = targetAccelerationMpSS - currentAccelerationMpSS;
            TotalError += (error + LastError) * elapsedClockSeconds / 2;
            float pPercent = (float)Math.Round(ProportionalFactor * targetAccelerationMpSS * Granularity) / Granularity;
            float iPercent = IntegralFactor * TotalError;
            float dPercent = elapsedClockSeconds > 0 && DerivativeFactor > 0 ? DerivativeFactor * (error - LastError) / elapsedClockSeconds : 0;
            Percent = pPercent + iPercent + dPercent;
            if (Percent <= minPercent)
            {
                if (pPercent > minPercent && IntegralFactor > 0) TotalError = (minPercent - pPercent) / IntegralFactor;
                else if (TotalError < 0) TotalError = 0;
                Percent = minPercent;
            }
            if (Percent >= maxPercent)
            {
                if (pPercent < maxPercent && IntegralFactor > 0) TotalError = (maxPercent - pPercent) / IntegralFactor;
                else if (TotalError > 0) TotalError = 0;
                Percent = maxPercent;
            }
            LastTarget = targetAccelerationMpSS;
            LastError = error;
        }
    }
}
