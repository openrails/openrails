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

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace Orts.Simulation.RollingStocks.SubSystems
{
    public class CruiseControl

    {
        MSTSLocomotive Locomotive;
        Simulator Simulator;

        public bool Equipped = false;
        public bool SpeedIsMph = false;
        public bool SpeedRegulatorMaxForcePercentUnits = false;
        public float SpeedRegulatorMaxForceSteps = 0;
        public bool MaxForceSetSingleStep = false;
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
        public float SelectedMaxAccelerationPercent = 0;
        public float SelectedMaxAccelerationStep = 0;
        public float SelectedSpeedMpS = 0;
        public int SelectedNumberOfAxles = 0;
        public float SpeedRegulatorNominalSpeedStepMpS = 0;
        public float SpeedRegulatorNominalSpeedStepKpHOrMpH = 0;
        public float MaxAccelerationMpSS = 1;
        public float MaxDecelerationMpSS = 0.5f;
        public bool UseThrottle = false;
        public bool UseThrottleInCombinedControl = false;
        public bool AntiWheelSpinEquipped = false;
        public float AntiWheelSpinSpeedDiffThreshold = 0.5f;
        public float DynamicBrakeMaxForceAtSelectorStep = 0;
        public float ForceThrottleAndDynamicBrake = 0;
        protected float maxForceN = 0;
        protected float trainBrakePercent = 0;
        protected float trainLength = 0;
        public int TrainLengthMeters = 0;
        public int RemainingTrainLengthToPassRestrictedZone = 0;
        public bool RestrictedSpeedActive = false;
        public float CurrentSelectedSpeedMpS = 0;
        protected float nextSelectedSpeedMps = 0;
        protected float restrictedRegionTravelledDistance = 0;
        protected float currentThrottlePercent = 0;
        protected double clockTime = 0;
        protected bool dynamicBrakeSetToZero = false;
        public float StartReducingSpeedDelta = 0.5f;
        public float StartReducingSpeedDeltaDownwards = 0f;
        public bool Battery = false;
        public bool DynamicBrakePriority = false;
        protected bool ThrottleNeutralPriority = false;
        public List<int> ForceStepsThrottleTable = new List<int>();
        public List<float> AccelerationTable = new List<float>();
        public enum SpeedRegulatorMode { Manual, Auto, Testing, AVV }
        public enum SpeedSelectorMode { Parking, Neutral, On, Start }
        protected float absMaxForceN = 0;
        protected float brakePercent = 0;
        public float DynamicBrakeIncreaseSpeed = 0;
        public float DynamicBrakeDecreaseSpeed = 0;
        public uint MinimumMetersToPass = 19;
        protected float relativeAcceleration;
        public float AccelerationRampMaxMpSSS = 0.7f;
        public float AccelerationDemandMpSS;
        public float AccelerationRampMinMpSSS = 0.01f;
        public bool ResetForceAfterAnyBraking = false;
        public float ThrottleFullRangeIncreaseTimeSeconds = 6;
        public float ThrottleFullRangeDecreaseTimeSeconds = 6;
        public float DynamicBrakeFullRangeIncreaseTimeSeconds;
        public float DynamicBrakeFullRangeDecreaseTimeSeconds;
        public float ParkingBrakeEngageSpeed = 0;
        public float ParkingBrakePercent = 0;
        public bool SkipThrottleDisplay = false;
        public bool DisableZeroForceStep = false;
        public bool DynamicBrakeIsSelectedForceDependant = false;
        public bool UseThrottleAsSpeedSelector = false;
        public bool UseThrottleAsForceSelector = false;
        public float Ampers = 0;
        public bool ContinuousSpeedIncreasing = false;
        public bool ContinuousSpeedDecreasing = false;
        public float PowerBreakoutAmpers = 0;
        public float PowerBreakoutSpeedDelta = 0;
        public float PowerResumeSpeedDelta = 0;
        public float PowerReductionDelayPaxTrain = 0;
        public float PowerReductionDelayCargoTrain = 0;
        public float PowerReductionValue = 0;
        public float MaxPowerThreshold = 0;
        public float SafeSpeedForAutomaticOperationMpS = 0;
        protected float SpeedSelectorStepTimeSeconds = 0;
        protected float TotalTime = 0;
        public bool DisableCruiseControlOnThrottleAndZeroSpeed = false;
        public bool DisableCruiseControlOnThrottleAndZeroForce = false;
        public bool DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed = false;
        public bool ForceResetRequiredAfterBraking = false;
        public bool ForceResetIncludeDynamicBrake = false;
        public bool ZeroSelectedSpeedWhenPassingToThrottleMode = false;
        public bool DynamicBrakeCommandHasPriorityOverCruiseControl = true;
        public bool HasIndependentThrottleDynamicBrakeLever = false;
        public bool HasProportionalSpeedSelector = false;
        public bool SpeedSelectorIsDiscrete = false;
        public bool DoComputeNumberOfAxles = false;
        public bool DisableManualSwitchToManualWhenSetForceNotAtZero = false;
        public bool DisableManualSwitchToAutoWhenThrottleNotAtZero = false;
        public bool DisableManualSwitchToAutoWhenSetSpeedNotAtTop = false;
        public bool EnableSelectedSpeedSelectionWhenManualModeSet = false;
        public bool UseTrainBrakeAndDynBrake = false;
        protected float SpeedDeltaToEnableTrainBrake = 5;
        protected float SpeedDeltaToEnableFullTrainBrake = 10;
        public float MinimumSpeedForCCEffectMpS = 0;
        protected float speedRegulatorIntermediateValue = 0;
        protected float StepSize = 20;
        protected float RelativeAccelerationMpSS = 0; // Acceleration relative to state of reverser
        public bool CCIsUsingTrainBrake = false; // Cruise control is using (also) train brake to brake
        protected float TrainBrakeMinPercentValue = 30f; // Minimum train brake settable percent Value
        protected float TrainBrakeMaxPercentValue = 85f; // Maximum train brake settable percent Value
        public bool StartInAutoMode = false; // at startup cruise control is in auto mode
        public bool ThrottleNeutralPosition = false; // when UseThrottleAsSpeedSelector is true and this is true
                                                     // and we are in auto mode, the throttle zero position is a neutral position
        public bool ThrottleLowSpeedPosition = false; // when UseThrottleAsSpeedSelector is true and this is true
                                                     // and we are in auto mode, the first throttle above zero position is used to run at low speed
        public float LowSpeed = 2f; // default parking speed
        public bool HasTwoForceValues = false; // when UseThrottleAsSpeedSelector is true, two max force values (50% and 100%) are available

        public bool OverrideForceCalculation = false;

        public CruiseControl(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
        }
 

        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            while (!stf.EndOfBlock())
            {
                stf.ReadItem();
                switch (stf.Tree.ToLower())
                {
                    case "engine(ortscruisecontrol(speedismph": SpeedIsMph = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(usethrottle": UseThrottle = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(usethrottleincombinedcontrol": UseThrottleInCombinedControl = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(speedselectorsteptimeseconds": SpeedSelectorStepTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.1f); break;
                    case "engine(ortscruisecontrol(throttlefullrangeincreasetimeseconds": ThrottleFullRangeIncreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 5); break;
                    case "engine(ortscruisecontrol(throttlefullrangedecreasetimeseconds": ThrottleFullRangeDecreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 5); break;
                    case "engine(ortscruisecontrol(resetforceafteranybraking": ResetForceAfterAnyBraking = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(dynamicbrakefullrangeincreasetimeseconds": DynamicBrakeFullRangeIncreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 5); break;
                    case "engine(ortscruisecontrol(dynamicbrakefullrangedecreasetimeseconds": DynamicBrakeFullRangeDecreaseTimeSeconds = stf.ReadFloatBlock(STFReader.UNITS.Any, 5); break;
                    case "engine(ortscruisecontrol(parkingbrakeengagespeed": ParkingBrakeEngageSpeed = stf.ReadFloatBlock(STFReader.UNITS.Speed, 0); break;
                    case "engine(ortscruisecontrol(parkingbrakepercent": ParkingBrakePercent = stf.ReadFloatBlock(STFReader.UNITS.Any, 0); break;
                    case "engine(ortscruisecontrol(maxpowerthreshold": MaxPowerThreshold = stf.ReadFloatBlock(STFReader.UNITS.Any, 0); break;
                    case "engine(ortscruisecontrol(safespeedforautomaticoperationmps": SafeSpeedForAutomaticOperationMpS = stf.ReadFloatBlock(STFReader.UNITS.Any, 0); break;
                    case "engine(ortscruisecontrol(maxforcepercentunits": SpeedRegulatorMaxForcePercentUnits = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(maxforcesteps": SpeedRegulatorMaxForceSteps = stf.ReadIntBlock(0); break;
                    case "engine(ortscruisecontrol(maxforcesetsinglestep": MaxForceSetSingleStep = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(maxforcekeepselectedstepwhenmanualmodeset": MaxForceKeepSelectedStepWhenManualModeSet = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(keepselectedspeedwhenmanualmodeset": KeepSelectedSpeedWhenManualModeSet = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(forceregulatorautowhennonzerospeedselected": ForceRegulatorAutoWhenNonZeroSpeedSelected = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(forceregulatorautowhennonzeroforceselected": ForceRegulatorAutoWhenNonZeroForceSelected = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(forceregulatorautowhennonzerospeedselectedandthrottleatzero": ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(maxforceselectorisdiscrete": MaxForceSelectorIsDiscrete = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(continuousspeedincreasing": ContinuousSpeedIncreasing = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(disablecruisecontrolonthrottleandzerospeed": DisableCruiseControlOnThrottleAndZeroSpeed = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(disablecruisecontrolonthrottleandzeroforce": DisableCruiseControlOnThrottleAndZeroForce = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(disablecruisecontrolonthrottleandzeroforceandzerospeed": DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(disablemanualswitchtomanualwhensetforcenotatzero": DisableManualSwitchToManualWhenSetForceNotAtZero = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(disablemanualswitchtoautowhenthrottlenotatzero": DisableManualSwitchToAutoWhenThrottleNotAtZero = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(disablemanualswitchtoautowhensetspeednotattop": DisableManualSwitchToAutoWhenSetSpeedNotAtTop = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(enableselectedspeedselectionwhenmanualmodeset": EnableSelectedSpeedSelectionWhenManualModeSet = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(forcestepsthrottletable":
                        foreach (var forceStepThrottleValue in stf.ReadStringBlock("").Replace(" ", "").Split(','))
                        {
                            ForceStepsThrottleTable.Add(int.Parse(forceStepThrottleValue));
                        }
                        break;
                    case "engine(ortscruisecontrol(accelerationtable":
                        foreach (var accelerationValue in stf.ReadStringBlock("").Replace(" ", "").Split(','))
                        {
                            AccelerationTable.Add(float.Parse(accelerationValue));
                        }
                        break;
                    case "engine(ortscruisecontrol(powerbreakoutampers": PowerBreakoutAmpers = stf.ReadFloatBlock(STFReader.UNITS.Any, 100.0f); break;
                    case "engine(ortscruisecontrol(powerbreakoutspeeddelta": PowerBreakoutSpeedDelta = stf.ReadFloatBlock(STFReader.UNITS.Any, 100.0f); break;
                    case "engine(ortscruisecontrol(powerresumespeeddelta": PowerResumeSpeedDelta = stf.ReadFloatBlock(STFReader.UNITS.Any, 100.0f); break;
                    case "engine(ortscruisecontrol(powerreductiondelaypaxtrain": PowerReductionDelayPaxTrain = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.0f); break;
                    case "engine(ortscruisecontrol(powerreductiondelaycargotrain": PowerReductionDelayCargoTrain = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.0f); break;
                    case "engine(ortscruisecontrol(powerreductionvalue": PowerReductionValue = stf.ReadFloatBlock(STFReader.UNITS.Any, 100.0f); break;
                    case "engine(ortscruisecontrol(disablezeroforcestep": DisableZeroForceStep = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(dynamicbrakeisselectedforcedependant": DynamicBrakeIsSelectedForceDependant = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(defaultforcestep": SelectedMaxAccelerationStep = stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f); break;
                    case "engine(ortscruisecontrol(dynamicbrakemaxforceatselectorstep": DynamicBrakeMaxForceAtSelectorStep = stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f); break;
                    case "engine(ortscruisecontrol(startreducingspeeddelta": StartReducingSpeedDelta = (stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f) / 10); break;
                    case "engine(ortscruisecontrol(startreducingspeeddeltadownwards": StartReducingSpeedDeltaDownwards = (stf.ReadFloatBlock(STFReader.UNITS.Any, 1.0f) / 10); break;
                    case "engine(ortscruisecontrol(maxacceleration": MaxAccelerationMpSS = stf.ReadFloatBlock(STFReader.UNITS.Any, 1); break;
                    case "engine(ortscruisecontrol(maxdeceleration": MaxDecelerationMpSS = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.5f); break;
                    case "engine(ortscruisecontrol(antiwheelspinequipped": AntiWheelSpinEquipped = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(antiwheelspinspeeddiffthreshold": AntiWheelSpinSpeedDiffThreshold = stf.ReadFloatBlock(STFReader.UNITS.None, 0.5f); break;
                    case "engine(ortscruisecontrol(nominalspeedstep":
                        {
                            SpeedRegulatorNominalSpeedStepKpHOrMpH = stf.ReadFloatBlock(STFReader.UNITS.Speed, 0);
                            SpeedRegulatorNominalSpeedStepMpS = SpeedIsMph ? MpS.FromMpH(SpeedRegulatorNominalSpeedStepKpHOrMpH) : MpS.FromKpH(SpeedRegulatorNominalSpeedStepKpHOrMpH);
                            break;
                        }
                    case "engine(ortscruisecontrol(usethrottleasspeedselector": UseThrottleAsSpeedSelector = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(usethrottleasforceselector": UseThrottleAsForceSelector = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(dynamicbrakeincreasespeed": DynamicBrakeIncreaseSpeed = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.5f); break;
                    case "engine(ortscruisecontrol(dynamicbrakedecreasespeed": DynamicBrakeDecreaseSpeed = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.5f); break;
                    case "engine(ortscruisecontrol(forceresetrequiredafterbraking": ForceResetRequiredAfterBraking = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(forceresetincludedynamicbrake": ForceResetIncludeDynamicBrake = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(zeroselectedspeedwhenpassingtothrottlemode": ZeroSelectedSpeedWhenPassingToThrottleMode = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(dynamicbrakecommandhaspriorityovercruisecontrol": DynamicBrakeCommandHasPriorityOverCruiseControl = stf.ReadBoolBlock(true); break;
                    case "engine(ortscruisecontrol(hasindependentthrottledynamicbrakelever": HasIndependentThrottleDynamicBrakeLever = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(hasproportionalspeedselector": HasProportionalSpeedSelector = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(speedselectorisdiscrete": SpeedSelectorIsDiscrete = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(usetrainbrakeanddynbrake": UseTrainBrakeAndDynBrake = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(speeddeltatoenabletrainbrake": SpeedDeltaToEnableTrainBrake = stf.ReadFloatBlock(STFReader.UNITS.Speed, 5f); break;
                    case "engine(ortscruisecontrol(speeddeltatoenablefulltrainbrake": SpeedDeltaToEnableFullTrainBrake = stf.ReadFloatBlock(STFReader.UNITS.Speed, 10f); break;
                    case "engine(ortscruisecontrol(minimumspeedforcceffect": MinimumSpeedForCCEffectMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, 0f); break;
                    case "engine(ortscruisecontrol(trainbrakeminpercentvalue": TrainBrakeMinPercentValue = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.3f); break;
                    case "engine(ortscruisecontrol(trainbrakemaxpercentvalue": TrainBrakeMaxPercentValue = stf.ReadFloatBlock(STFReader.UNITS.Any, 0.85f); break;
                    case "engine(ortscruisecontrol(startinautomode": StartInAutoMode = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(throttleneutralposition": ThrottleNeutralPosition = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(throttlelowspeedposition": ThrottleLowSpeedPosition = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(lowspeed": LowSpeed = stf.ReadFloatBlock(STFReader.UNITS.Speed, 2f); break;
                    case "engine(ortscruisecontrol(hasttwoforcevalues": HasTwoForceValues = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(docomputenumberofaxles": DoComputeNumberOfAxles = stf.ReadBoolBlock(false); break;
                    case "engine(ortscruisecontrol(options":
                        foreach (var speedRegulatorOption in stf.ReadStringBlock("").ToLower().Replace(" ", "").Split(','))
                        {
                            SpeedRegulatorOptions.Add(speedRegulatorOption.ToLower());
                        }
                        break;
                    case "engine(ortscruisecontrol(controllercruisecontrollogic":
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
                    default: break;
                }
            }
        }

        public CruiseControl Clone(MSTSLocomotive locomotive)
        {
            return new CruiseControl(this, locomotive);
        }

        public CruiseControl(CruiseControl other, MSTSLocomotive locomotive)
        {
            Simulator = locomotive.Simulator;
            Locomotive = locomotive;

            Equipped = other.Equipped;
            SpeedIsMph = other.SpeedIsMph;
            SpeedRegulatorMaxForcePercentUnits = other.SpeedRegulatorMaxForcePercentUnits;
            SpeedRegulatorMaxForceSteps = other.SpeedRegulatorMaxForceSteps;
            MaxForceSetSingleStep = other.MaxForceSetSingleStep;
            MaxForceKeepSelectedStepWhenManualModeSet = other.MaxForceKeepSelectedStepWhenManualModeSet;
            KeepSelectedSpeedWhenManualModeSet = other.KeepSelectedSpeedWhenManualModeSet;
            ForceRegulatorAutoWhenNonZeroSpeedSelected = other.ForceRegulatorAutoWhenNonZeroSpeedSelected;
            ForceRegulatorAutoWhenNonZeroForceSelected = other.ForceRegulatorAutoWhenNonZeroForceSelected;
            ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero = other.ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero;
            MaxForceSelectorIsDiscrete = other.MaxForceSelectorIsDiscrete;
            SpeedRegulatorOptions = other.SpeedRegulatorOptions;
            CruiseControlLogic = other.CruiseControlLogic;
            SpeedRegulatorNominalSpeedStepMpS = other.SpeedRegulatorNominalSpeedStepMpS;
            SpeedRegulatorNominalSpeedStepKpHOrMpH = other.SpeedRegulatorNominalSpeedStepKpHOrMpH;
            MaxAccelerationMpSS = other.MaxAccelerationMpSS;
            MaxDecelerationMpSS = other.MaxDecelerationMpSS;
            UseThrottle = other.UseThrottle;
            UseThrottleInCombinedControl = other.UseThrottleInCombinedControl;
            AntiWheelSpinEquipped = other.AntiWheelSpinEquipped;
            AntiWheelSpinSpeedDiffThreshold = other.AntiWheelSpinSpeedDiffThreshold;
            DynamicBrakeMaxForceAtSelectorStep = other.DynamicBrakeMaxForceAtSelectorStep;
            StartReducingSpeedDelta = other.StartReducingSpeedDelta;
            StartReducingSpeedDeltaDownwards = other.StartReducingSpeedDeltaDownwards;
            ForceStepsThrottleTable = other.ForceStepsThrottleTable;
            AccelerationTable = other.AccelerationTable;
            DynamicBrakeIncreaseSpeed = other.DynamicBrakeIncreaseSpeed;
            DynamicBrakeDecreaseSpeed = other.DynamicBrakeDecreaseSpeed;
            AccelerationRampMaxMpSSS = other.AccelerationRampMaxMpSSS;
            AccelerationRampMinMpSSS = other.AccelerationRampMinMpSSS;
            ResetForceAfterAnyBraking = other.ResetForceAfterAnyBraking;
            ThrottleFullRangeIncreaseTimeSeconds = other.ThrottleFullRangeIncreaseTimeSeconds;
            ThrottleFullRangeDecreaseTimeSeconds = other.ThrottleFullRangeDecreaseTimeSeconds;
            DynamicBrakeFullRangeIncreaseTimeSeconds = other.DynamicBrakeFullRangeIncreaseTimeSeconds;
            DynamicBrakeFullRangeDecreaseTimeSeconds = other.DynamicBrakeFullRangeDecreaseTimeSeconds;
            ParkingBrakeEngageSpeed = other.ParkingBrakeEngageSpeed;
            ParkingBrakePercent = other.ParkingBrakePercent;
            DisableZeroForceStep = other.DisableZeroForceStep;
            DynamicBrakeIsSelectedForceDependant = other.DynamicBrakeIsSelectedForceDependant;
            UseThrottleAsSpeedSelector = other.UseThrottleAsSpeedSelector;
            UseThrottleAsForceSelector = other.UseThrottleAsForceSelector;
            ContinuousSpeedIncreasing = other.ContinuousSpeedIncreasing;
            ContinuousSpeedDecreasing = other.ContinuousSpeedDecreasing;
            PowerBreakoutAmpers = other.PowerBreakoutAmpers;
            PowerBreakoutSpeedDelta = other.PowerBreakoutSpeedDelta;
            PowerResumeSpeedDelta = other.PowerResumeSpeedDelta;
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
            ForceResetIncludeDynamicBrake = other.ForceResetIncludeDynamicBrake;
            ZeroSelectedSpeedWhenPassingToThrottleMode = other.ZeroSelectedSpeedWhenPassingToThrottleMode;
            DynamicBrakeCommandHasPriorityOverCruiseControl = other.DynamicBrakeCommandHasPriorityOverCruiseControl;
            HasIndependentThrottleDynamicBrakeLever = other.HasIndependentThrottleDynamicBrakeLever;
            HasProportionalSpeedSelector = other.HasProportionalSpeedSelector;
            DisableManualSwitchToManualWhenSetForceNotAtZero = other.DisableManualSwitchToManualWhenSetForceNotAtZero;
            DisableManualSwitchToAutoWhenThrottleNotAtZero = other.DisableManualSwitchToAutoWhenThrottleNotAtZero;
            DisableManualSwitchToAutoWhenSetSpeedNotAtTop = other.DisableManualSwitchToAutoWhenSetSpeedNotAtTop;
            EnableSelectedSpeedSelectionWhenManualModeSet = other.EnableSelectedSpeedSelectionWhenManualModeSet;
            SpeedSelectorIsDiscrete = other.SpeedSelectorIsDiscrete;
            DoComputeNumberOfAxles = other.DoComputeNumberOfAxles;
            UseTrainBrakeAndDynBrake = other.UseTrainBrakeAndDynBrake;
            SpeedDeltaToEnableTrainBrake = other.SpeedDeltaToEnableTrainBrake;
            SpeedDeltaToEnableFullTrainBrake = other.SpeedDeltaToEnableFullTrainBrake;
            MinimumSpeedForCCEffectMpS = other.MinimumSpeedForCCEffectMpS;
            TrainBrakeMinPercentValue = other.TrainBrakeMinPercentValue;
            TrainBrakeMaxPercentValue = other.TrainBrakeMaxPercentValue;
            StartInAutoMode = other.StartInAutoMode;
            ThrottleNeutralPosition = other.ThrottleNeutralPosition;
            ThrottleLowSpeedPosition = other.ThrottleLowSpeedPosition;
            LowSpeed = other.LowSpeed;
            HasTwoForceValues = other.HasTwoForceValues;

        }

        public void Initialize()
        {
            Simulator = Locomotive.Simulator;
            clockTime = Simulator.ClockTime * 100;
            ComputeNumberOfAxles();
            if (StartReducingSpeedDeltaDownwards == 0) StartReducingSpeedDeltaDownwards = StartReducingSpeedDelta;
            if (StartInAutoMode) SpeedRegMode = SpeedRegulatorMode.Auto;
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

        public void Update(float elapsedClockSeconds)
        {
            OverrideForceCalculation = false;
            if (!Locomotive.IsPlayerTrain)
            {
                WasForceReset = false;
                CCThrottleOrDynBrakePercent = 0;
                return;
            }

            UpdateSelectedSpeed(elapsedClockSeconds);

            if (!ThrottleNeutralPosition || SelectedSpeedMpS > 0) ThrottleNeutralPriority = false;

            if (Locomotive.TrainBrakeController.TCSEmergencyBraking || Locomotive.TrainBrakeController.TCSFullServiceBraking)
            {
                WasBraking = true;
            }
            else if (SpeedRegMode == SpeedRegulatorMode.Manual || (SpeedRegMode == SpeedRegulatorMode.Auto && (DynamicBrakePriority || ThrottleNeutralPriority)))
            {
                WasForceReset = false;
                CCThrottleOrDynBrakePercent = 0;
            }
            else if (SpeedRegMode == SpeedRegulatorMode.Auto)
            {
                if (ThrottleNeutralPosition && SelectedSpeedMpS == 0)
                {
                    // we are in the neutral position
                    ThrottleNeutralPriority = true;
                    Locomotive.ThrottleController.SetPercent(0);
                    if (Locomotive.DynamicBrakePercent != -1)
                    {
                        Locomotive.SetDynamicBrakePercent(0);
                        Locomotive.DynamicBrakeChangeActiveState(false);
                    }
                    CCThrottleOrDynBrakePercent = 0;
                    WasForceReset = false;
                    Locomotive.DynamicBrakeIntervention = -1;
                }
                else
                {
                    OverrideForceCalculation = true;
                }
            }

            if (SpeedRegMode == SpeedRegulatorMode.Manual)
                SkipThrottleDisplay = false;

            RelativeAccelerationMpSS = Locomotive.AccelerationMpSS;
            if (Locomotive.Direction == Direction.Reverse) RelativeAccelerationMpSS *= -1;
            if (maxForceIncreasing) SpeedRegulatorMaxForceIncrease(elapsedClockSeconds);
            if (maxForceIncreasing) SpeedRegulatorMaxForceIncrease(elapsedClockSeconds);
            if (maxForceDecreasing)
            {
                if (SelectedMaxAccelerationStep <= 0) maxForceDecreasing = false;
                else SpeedRegulatorMaxForceDecrease(elapsedClockSeconds);
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(this.applyingPneumaticBrake);
            outf.Write(this.Battery);
            outf.Write(this.brakeIncreasing);
            outf.Write(this.clockTime);
            outf.Write(this.controllerTime);
            outf.Write(this.CurrentSelectedSpeedMpS);
            outf.Write(this.currentThrottlePercent);
            outf.Write(this.dynamicBrakeSetToZero);
            outf.Write(this.fromAcceleration);
            outf.Write(this.maxForceDecreasing);
            outf.Write(this.maxForceIncreasing);
            outf.Write(this.maxForceN);
            outf.Write(this.nextSelectedSpeedMps);
            outf.Write(this.restrictedRegionTravelledDistance);
            outf.Write(this.RestrictedSpeedActive);
            outf.Write(this.SelectedMaxAccelerationPercent);
            outf.Write(this.SelectedMaxAccelerationStep);
            outf.Write(this.SelectedNumberOfAxles);
            outf.Write(this.SelectedSpeedMpS);
            outf.Write((int)this.SpeedRegMode);
            outf.Write((int)this.SpeedSelMode);
            outf.Write(this.throttleIsZero);
            outf.Write(this.trainBrakePercent);
            outf.Write(this.TrainLengthMeters);
            outf.Write(speedRegulatorIntermediateValue);
            outf.Write(CCIsUsingTrainBrake);
        }

        public void Restore(BinaryReader inf)
        {
            applyingPneumaticBrake = inf.ReadBoolean();
            Battery = inf.ReadBoolean();
            brakeIncreasing = inf.ReadBoolean();
            clockTime = inf.ReadDouble();
            controllerTime = inf.ReadSingle();
            CurrentSelectedSpeedMpS = inf.ReadSingle();
            currentThrottlePercent = inf.ReadSingle();
            dynamicBrakeSetToZero = inf.ReadBoolean();
            fromAcceleration = inf.ReadSingle();
            maxForceDecreasing = inf.ReadBoolean();
            maxForceIncreasing = inf.ReadBoolean();
            maxForceN = inf.ReadSingle();
            nextSelectedSpeedMps = inf.ReadSingle();
            restrictedRegionTravelledDistance = inf.ReadSingle();
            RestrictedSpeedActive = inf.ReadBoolean();
            SelectedMaxAccelerationPercent = inf.ReadSingle();
            SelectedMaxAccelerationStep = inf.ReadSingle();
            SelectedNumberOfAxles = inf.ReadInt32();
            SelectedSpeedMpS = inf.ReadSingle();
            int fSpeedRegMode = inf.ReadInt32();
            SpeedRegMode = (SpeedRegulatorMode)fSpeedRegMode;
            int fSpeedSelMode = inf.ReadInt32();
            SpeedSelMode = (SpeedSelectorMode)fSpeedSelMode;
            throttleIsZero = inf.ReadBoolean();
            trainBrakePercent = inf.ReadSingle();
            TrainLengthMeters = inf.ReadInt32();
            speedRegulatorIntermediateValue = inf.ReadSingle();
            CCIsUsingTrainBrake = inf.ReadBoolean();
        }

        public void UpdateSelectedSpeed(float elapsedClockSeconds)
        {
            TotalTime += elapsedClockSeconds;
            if (SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto && !DynamicBrakePriority ||
             EnableSelectedSpeedSelectionWhenManualModeSet)
            {
                if (selectedSpeedIncreasing) SpeedRegulatorSelectedSpeedIncrease();
                if (SelectedSpeedDecreasing) SpeedRegulatorSelectedSpeedDecrease();
            }
        }


        public void SpeedRegulatorModeIncrease()
        {
            if (!Locomotive.IsPlayerTrain) return;
            Locomotive.SignalEvent(Common.Event.CruiseControlSpeedRegulator);
            SpeedRegulatorMode previousMode = SpeedRegMode;
            if (!Equipped) return;
            if (SpeedRegMode == SpeedRegulatorMode.Testing) return;
            if (SpeedRegMode == SpeedRegulatorMode.Manual &&
               ((DisableManualSwitchToAutoWhenThrottleNotAtZero && (Locomotive.ThrottlePercent != 0 || 
               (Locomotive.DynamicBrakePercent != -1 && Locomotive.DynamicBrakePercent != 0))) ||
               (DisableManualSwitchToAutoWhenSetSpeedNotAtTop && SelectedSpeedMpS != Locomotive.MaxSpeedMpS && Locomotive.AbsSpeedMpS > Simulator.MaxStoppedMpS)))
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
                            if (!DisableManualSwitchToAutoWhenSetSpeedNotAtTop && !KeepSelectedSpeedWhenManualModeSet) SelectedSpeedMpS = Locomotive.AbsSpeedMpS;
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
            if (!Equipped) return;
            if (SpeedRegMode == SpeedRegulatorMode.Manual) return;
            if (SpeedRegMode == SpeedRegulatorMode.Auto &&
                (DisableManualSwitchToManualWhenSetForceNotAtZero && SelectedMaxAccelerationStep != 0))
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
                            Locomotive.ThrottleController.SetPercent(0);
                            currentThrottlePercent = 0;
                            if (SpeedRegulatorOptions.Contains("regulatormanual")) test = true;
                            if (ZeroSelectedSpeedWhenPassingToThrottleMode) SelectedSpeedMpS = 0;
                            foreach (MSTSLocomotive lc in playerNotDriveableTrainLocomotives)
                            {
                                ThrottleOverriden = 0;
                                LocoIsAPartOfPlayerTrain = false; // in case we uncouple the loco later
                            }
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
            if (!Equipped) return;
            if (SpeedSelMode == SpeedSelectorMode.Start) return;
            bool test = false;
            while (!test)
            {
                SpeedSelMode++;
                if (SpeedSelMode != SpeedSelectorMode.Parking && !EngineBrakePriority) Locomotive.SetEngineBrakePercent(0);
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
            if (!Equipped) return;
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
            if (!Equipped) return;
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
            if (SelectedMaxAccelerationPercent == percent) return;
            SelectedMaxAccelerationPercent = percent;
            SelectedMaxAccelerationStep = (float)Math.Round(SelectedMaxAccelerationPercent * SpeedRegulatorMaxForceSteps / 100, 0);
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Speed regulator max acceleration percent changed to {0}", Simulator.Catalog.GetString(SelectedMaxAccelerationPercent.ToString()) + "%"));
        }

        bool maxForceIncreasing = false;
        public void SpeedRegulatorMaxForceStartIncrease()
        {
            if (SelectedMaxAccelerationStep == 0)
            {
                Locomotive.SignalEvent(Common.Event.LeverFromZero);
            }
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            if (SelectedMaxAccelerationStep == 0 && DisableCruiseControlOnThrottleAndZeroForce && ForceRegulatorAutoWhenNonZeroForceSelected &&
                Locomotive.ThrottleController.CurrentValue == 0 && Locomotive.DynamicBrakeController.CurrentValue == 0 && Locomotive.CruiseControl.SpeedRegMode == SpeedRegulatorMode.Manual)
            {
                SpeedRegMode = Simulation.RollingStocks.SubSystems.CruiseControl.SpeedRegulatorMode.Auto;
                WasForceReset = true;
            }
            maxForceIncreasing = true;
            speedRegulatorIntermediateValue = SpeedRegulatorMaxForcePercentUnits ? SelectedMaxAccelerationPercent : SelectedMaxAccelerationStep;
        }
        public void SpeedRegulatorMaxForceStopIncrease()
        {
            maxForceIncreasing = false;
        }
        protected void SpeedRegulatorMaxForceIncrease(float elapsedClockSeconds)
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            if (MaxForceSetSingleStep) maxForceIncreasing = false;
            if (SelectedMaxAccelerationStep == 0.5f) SelectedMaxAccelerationStep = 0;
            if (!Equipped) return;
            if (SpeedRegulatorMaxForcePercentUnits)
            {
                if (SelectedMaxAccelerationPercent == 100)
                    return;
                speedRegulatorIntermediateValue += StepSize * elapsedClockSeconds;
                SelectedMaxAccelerationPercent = (float)Math.Truncate(speedRegulatorIntermediateValue + 1);
//                SelectedMaxAccelerationPercent = (float)Math.Round(SelectedMaxAccelerationStep * 100 / SpeedRegulatorMaxForceSteps, 0);
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Speed regulator max acceleration percent changed to {0}", Simulator.Catalog.GetString(SelectedMaxAccelerationPercent.ToString()) + "%"));
            }
            else
            {
                if (SelectedMaxAccelerationStep == SpeedRegulatorMaxForceSteps)
                    return;
                speedRegulatorIntermediateValue += MaxForceSelectorIsDiscrete ? elapsedClockSeconds : StepSize * elapsedClockSeconds * SpeedRegulatorMaxForceSteps / 100.0f;
                SelectedMaxAccelerationStep = (float)Math.Truncate(speedRegulatorIntermediateValue + 1);
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed regulator max acceleration changed to") + " " + Simulator.Catalog.GetString(Math.Round(SelectedMaxAccelerationStep * 100 / SpeedRegulatorMaxForceSteps, 0).ToString()));
            }
        }

        protected bool maxForceDecreasing = false;
        public void SpeedRegulatorMaxForceStartDecrease()
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            maxForceDecreasing = true;
            speedRegulatorIntermediateValue = SpeedRegulatorMaxForcePercentUnits ? SelectedMaxAccelerationPercent : SelectedMaxAccelerationStep;
        }
        public void SpeedRegulatorMaxForceStopDecrease()
        {
            maxForceDecreasing = false;
        }
        protected void SpeedRegulatorMaxForceDecrease(float elapsedClockSeconds)
        {
            Locomotive.SignalEvent(Common.Event.CruiseControlMaxForce);
            if (MaxForceSetSingleStep) maxForceDecreasing = false;
            if (!Equipped) return;
            if (DisableZeroForceStep)
            {
                if (SelectedMaxAccelerationStep <= 1)
                    return;
            }
            else
            {
                if (SelectedMaxAccelerationStep <= 0)
                    return;
            }
            speedRegulatorIntermediateValue -= MaxForceSelectorIsDiscrete ? elapsedClockSeconds : StepSize * elapsedClockSeconds * SpeedRegulatorMaxForceSteps / 100.0f;
            SelectedMaxAccelerationStep = (float)Math.Truncate(speedRegulatorIntermediateValue);
            if (DisableZeroForceStep)
            {
                if (SelectedMaxAccelerationStep <= 1)
                {
                    Locomotive.SignalEvent(Common.Event.LeverToZero);
                }
            }
            else
            {
                if (SelectedMaxAccelerationStep <= 0)
                {
                    Locomotive.SignalEvent(Common.Event.LeverToZero);
                }
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Speed regulator max acceleration changed to {0}", Simulator.Catalog.GetString(Math.Round(SelectedMaxAccelerationStep * 100 / SpeedRegulatorMaxForceSteps, 0).ToString())));
        }

        public void SpeedRegulatorMaxForceChangeByMouse(float movExtension, float maxValue)
        {
            if (movExtension != 0 && SelectedMaxAccelerationStep == 0 && DisableCruiseControlOnThrottleAndZeroForce && ForceRegulatorAutoWhenNonZeroForceSelected &&
                Locomotive.ThrottleController.CurrentValue == 0 && Locomotive.DynamicBrakeController.CurrentValue == 0 && SpeedRegMode == SpeedRegulatorMode.Manual)
            {
                SpeedRegMode = SpeedRegulatorMode.Auto;
                WasForceReset = true;
            }
            if (SelectedMaxAccelerationStep == 0)
            {
                if (movExtension > 0)
                {
                    Locomotive.SignalEvent(Common.Event.LeverFromZero);
                }
                else if (movExtension < 0)
                    return;
            }
            if (movExtension == 1)
            {
                SelectedMaxAccelerationStep += 1;
            }
            if (movExtension == -1)
            {
                SelectedMaxAccelerationStep -= 1;
            }
            if (movExtension != 0)
            {
                SelectedMaxAccelerationStep += movExtension * maxValue;
                if (SelectedMaxAccelerationStep > SpeedRegulatorMaxForceSteps)
                    SelectedMaxAccelerationStep = SpeedRegulatorMaxForceSteps;
                if (SelectedMaxAccelerationStep < 0)
                    SelectedMaxAccelerationStep = 0;
                if (SelectedMaxAccelerationStep == 0)
                {
                    Locomotive.SignalEvent(Common.Event.LeverToZero);
                }
                Locomotive.Simulator.Confirmer.Information(Simulator.Catalog.GetStringFmt("Selected maximum acceleration was changed to {0}", Math.Round((MaxForceSelectorIsDiscrete ?
                    (int)SelectedMaxAccelerationStep : SelectedMaxAccelerationStep) * 100 / SpeedRegulatorMaxForceSteps, 0).ToString() + "%"));
            }
        }

        public bool selectedSpeedIncreasing = false;
        public void SpeedRegulatorSelectedSpeedStartIncrease()
        {
            if (Locomotive.MultiPositionControllers != null)
            {
                foreach (Controllers.MultiPositionController mpc in Locomotive.MultiPositionControllers)
                {
                    if (mpc.controllerBinding != Controllers.MultiPositionController.ControllerBinding.SelectedSpeed)
                        return;
                    if (!mpc.StateChanged)
                    {
                        mpc.StateChanged = true;
                        if (SpeedRegMode != SpeedRegulatorMode.Auto && (ForceRegulatorAutoWhenNonZeroSpeedSelected ||
                            SelectedMaxAccelerationStep == 0 && DisableCruiseControlOnThrottleAndZeroForce && ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero &&
                            Locomotive.ThrottleController.CurrentValue == 0 && Locomotive.DynamicBrakeController.CurrentValue == 0))
                        {
                            SpeedRegMode = SpeedRegulatorMode.Auto;
                        }

                            mpc.DoMovement(Controllers.MultiPositionController.Movement.Forward);
                        return;
                    }
                }
            }
            if (SpeedRegMode != SpeedRegulatorMode.Auto && ( ForceRegulatorAutoWhenNonZeroSpeedSelected || HasProportionalSpeedSelector &&
                SelectedMaxAccelerationStep == 0 && DisableCruiseControlOnThrottleAndZeroForce && ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero &&
                            Locomotive.ThrottleController.CurrentValue == 0 && Locomotive.DynamicBrakeController.CurrentValue == 0))
            {
                SpeedRegMode = SpeedRegulatorMode.Auto;
            }
            if (SpeedRegMode != SpeedRegulatorMode.Auto && (ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero &&
                SelectedMaxAccelerationStep == 0 && DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed &&
                            Locomotive.ThrottleController.CurrentValue == 0 && Locomotive.DynamicBrakeController.CurrentValue == 0))
            {
                SpeedRegMode = SpeedRegulatorMode.Auto;
            }
            if (UseThrottleAsSpeedSelector || HasProportionalSpeedSelector)
            {
                selectedSpeedIncreasing = true;
                if (SelectedSpeedMpS == 0)
                {
                    Locomotive.SignalEvent(Common.Event.LeverFromZero);
                }
            }
            else
                SpeedSelectorModeStartIncrease();
        }
        public void SpeedRegulatorSelectedSpeedStopIncrease()
        {
            if (Locomotive.MultiPositionControllers != null)
            {
                foreach (Controllers.MultiPositionController mpc in Locomotive.MultiPositionControllers)
                {
                    if (mpc.controllerBinding != Controllers.MultiPositionController.ControllerBinding.SelectedSpeed)
                        return;
                    mpc.StateChanged = false;
                    mpc.DoMovement(Controllers.MultiPositionController.Movement.Neutral);
                    return;
                }
            }
            if (UseThrottleAsSpeedSelector || HasProportionalSpeedSelector)
                selectedSpeedIncreasing = false;
            else
                SpeedSelectorModeStopIncrease();
        }

        protected double selectedSpeedLeverHoldTime = 0;
        public void SpeedRegulatorSelectedSpeedIncrease()
        {
            if (!Equipped) return;

            if (selectedSpeedLeverHoldTime + SpeedSelectorStepTimeSeconds > TotalTime)
                return;
            selectedSpeedLeverHoldTime = TotalTime;

            SelectedSpeedMpS = Math.Max(MinimumSpeedForCCEffectMpS, SelectedSpeedMpS + SpeedRegulatorNominalSpeedStepMpS);
            if (SelectedSpeedMpS > Locomotive.MaxSpeedMpS)
                SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
            if (SpeedIsMph)
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Selected speed changed to {0} mph", Math.Round(MpS.FromMpS(SelectedSpeedMpS, false), 0, MidpointRounding.AwayFromZero).ToString()));
            else
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Selected speed changed to {0} km/h", Math.Round(MpS.FromMpS(SelectedSpeedMpS, true), 0, MidpointRounding.AwayFromZero).ToString()));
        }

        public bool SelectedSpeedDecreasing = false;
        public void SpeedRegulatorSelectedSpeedStartDecrease()
        {
            if (Locomotive.MultiPositionControllers != null)
            {
                foreach (Controllers.MultiPositionController mpc in Locomotive.MultiPositionControllers)
                {
                    if (mpc.controllerBinding != Controllers.MultiPositionController.ControllerBinding.SelectedSpeed)
                        return;
                    if (!mpc.StateChanged)
                    {
                        mpc.StateChanged = true;
                        mpc.DoMovement(Controllers.MultiPositionController.Movement.Aft);
                        return;
                    }
                }
            }
            if (UseThrottleAsSpeedSelector || HasProportionalSpeedSelector)
                SelectedSpeedDecreasing = true;
            else
                SpeedSelectorModeDecrease();
        }
        public void SpeedRegulatorSelectedSpeedStopDecrease()
        {
            if (Locomotive.MultiPositionControllers != null)
            {
                foreach (Controllers.MultiPositionController mpc in Locomotive.MultiPositionControllers)
                {
                    if (mpc.controllerBinding != Controllers.MultiPositionController.ControllerBinding.SelectedSpeed)
                        return;
                    mpc.StateChanged = false;
                    mpc.DoMovement(Controllers.MultiPositionController.Movement.Neutral);
                    return;
                }
            }
            SelectedSpeedDecreasing = false;
        }
        public void SpeedRegulatorSelectedSpeedDecrease()
        {
            if (!Equipped) return;

            if (selectedSpeedLeverHoldTime + SpeedSelectorStepTimeSeconds > TotalTime)
                return;
            selectedSpeedLeverHoldTime = TotalTime;
            if (SelectedSpeedMpS == 0)
                return;
            SelectedSpeedMpS -= SpeedRegulatorNominalSpeedStepMpS;
            if (SelectedSpeedMpS < 0)
                SelectedSpeedMpS = 0f;
            if (MinimumSpeedForCCEffectMpS > 0 &&SelectedSpeedMpS < MinimumSpeedForCCEffectMpS)
                SelectedSpeedMpS = 0;
            if (SpeedRegMode == SpeedRegulatorMode.Auto && ForceRegulatorAutoWhenNonZeroSpeedSelected && SelectedSpeedMpS == 0)
            {
                // return back to manual, clear all we have controlled before and let the driver to set up new stuff
                SpeedRegMode = SpeedRegulatorMode.Manual;
                DynamicBrakePriority = false;
//                Locomotive.ThrottleController.SetPercent(0);
//                Locomotive.SetDynamicBrakePercent(0);
                Locomotive.DynamicBrakeChangeActiveState(false);
            }
            if (SelectedSpeedMpS == 0)
            {
                if (HasProportionalSpeedSelector)
                {
                    Locomotive.SignalEvent(Common.Event.LeverToZero);
                }
                SelectedSpeedDecreasing = false;
            }
            if (SpeedIsMph)
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Selected speed changed to {0} mph", Math.Round(MpS.FromMpS(SelectedSpeedMpS, false), 0, MidpointRounding.AwayFromZero).ToString()));
            else
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Selected speed changed to {0} km/h", Math.Round(MpS.FromMpS(SelectedSpeedMpS, true), 0, MidpointRounding.AwayFromZero).ToString()));
        }

        public void SpeedRegulatorSelectedSpeedChangeByMouse(float movExtension, bool metric, float maxValue)
        {
            if (movExtension != 0 && SelectedMaxAccelerationStep == 0 && DisableCruiseControlOnThrottleAndZeroForce && ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero &&
            Locomotive.ThrottleController.CurrentValue == 0 && Locomotive.DynamicBrakeController.CurrentValue == 0 && SpeedRegMode == SpeedRegulatorMode.Manual)
                SpeedRegMode = SpeedRegulatorMode.Auto;
            if (movExtension != 0)
            {
                if (SelectedSpeedMpS == 0)
                {
                    if (movExtension > 0)
                    {
                        Locomotive.SignalEvent(Common.Event.LeverFromZero);
                    }
                    else if (movExtension < 0)
                        return;
                }
                var deltaSpeed = SpeedSelectorIsDiscrete ? (metric ? MpS.FromKpH((float)Math.Round(movExtension * maxValue / SpeedRegulatorNominalSpeedStepKpHOrMpH) * SpeedRegulatorNominalSpeedStepKpHOrMpH) :
                    MpS.FromMpH((float)Math.Round(movExtension * maxValue / SpeedRegulatorNominalSpeedStepKpHOrMpH) * SpeedRegulatorNominalSpeedStepKpHOrMpH)) :
                    (metric ? MpS.FromKpH((float)Math.Round(movExtension * maxValue)) :
                    MpS.FromMpH((float)Math.Round(movExtension * maxValue)));
                if (deltaSpeed > 0)
                    SelectedSpeedMpS = Math.Max(SelectedSpeedMpS + deltaSpeed, MinimumSpeedForCCEffectMpS);
                else
                {
                    SelectedSpeedMpS += deltaSpeed;
                    if (MinimumSpeedForCCEffectMpS > 0 && SelectedSpeedMpS < MinimumSpeedForCCEffectMpS)
                        SelectedSpeedMpS = 0;
                }

                if (SelectedSpeedMpS > Locomotive.MaxSpeedMpS)
                    SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
                if (SelectedSpeedMpS < 0)
                    SelectedSpeedMpS = 0;
                if (SelectedSpeedMpS == 0 && movExtension < 0)
                {
                    Locomotive.SignalEvent(Common.Event.LeverToZero);
                }
                if (SpeedIsMph)
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Selected speed changed to {0}", Math.Round(MpS.FromMpS(SelectedSpeedMpS, false), 0, MidpointRounding.AwayFromZero).ToString() + " mph"));
                else
                    Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetStringFmt("Selected speed changed to {0}", Math.Round(MpS.FromMpS(SelectedSpeedMpS, true), 0, MidpointRounding.AwayFromZero).ToString() + " km/h"));
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
            RemainingTrainLengthToPassRestrictedZone = TrainLengthMeters;
            if (!RestrictedSpeedActive)
            {
                restrictedRegionTravelledDistance = Simulator.PlayerLocomotive.Train.DistanceTravelledM;
                CurrentSelectedSpeedMpS = SelectedSpeedMpS;
                RestrictedSpeedActive = true;
            }
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed restricted zone active."));
        }

        public virtual void CheckRestrictedSpeedZone()
        {
            RemainingTrainLengthToPassRestrictedZone = (int)Math.Round((Simulator.PlayerLocomotive.Train.DistanceTravelledM - restrictedRegionTravelledDistance));
            if (RemainingTrainLengthToPassRestrictedZone < 0) RemainingTrainLengthToPassRestrictedZone = 0;
            if ((Simulator.PlayerLocomotive.Train.DistanceTravelledM - restrictedRegionTravelledDistance) >= trainLength)
            {
                RestrictedSpeedActive = false;
                Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Speed restricted zone off."));
                Locomotive.SignalEvent(Common.Event.CruiseControlAlert);
            }
        }

        public void SetSpeed(float Speed)
        {
            if (!Equipped) return;
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
            Simulator.Confirmer.Message(ConfirmLevel.Information, Simulator.Catalog.GetString("Selected speed set to ") + Speed.ToString() + (SpeedIsMph? "mph" : "kmh"));
        }

        protected List<MSTSLocomotive> playerNotDriveableTrainLocomotives = new List<MSTSLocomotive>();
        protected float _AccelerationMpSS = 0;
        protected bool throttleIsZero = false;
        protected bool brakeIncreasing = false;
        protected float controllerTime = 0;
        protected float fromAcceleration = 0;
        protected bool applyingPneumaticBrake = false;
        protected bool firstIteration = true;
        protected float previousMotiveForce = 0;
        protected float addPowerTimeCount = 0;
        // CCThrottleOrDynBrakePercent may vary from -100 to 100 and is the percentage value which the Cruise Control
        // sets to throttle (if CCThrottleOrDynBrakePercent >=0) or to dynamic brake (if CCThrottleOrDynBrakePercent <0)
        public float CCThrottleOrDynBrakePercent = 0;
        protected float throttleChangeTime = 0;
        protected bool breakout = false;
        protected  float timeFromEngineMoved = 0;
        protected bool reducingForce = false;
        protected bool canAddForce = true;
        protected List<float> concurrentAccelerationList = new List<float>();
        public float TrainElevation = 0;
        protected float skidSpeedDegratation = 0;
        protected float previousAccelerationDemand = 0;
        public bool TrainBrakePriority = false;
        public bool WasBraking = false;
        public bool WasForceReset = true;


        public bool SelectingSpeedPressed = false;
        public bool EngineBrakePriority = false;
        public bool LocoIsAPartOfPlayerTrain = false;
        public float ThrottleOverriden = 0;
        public int AccelerationBits = 0;
        public bool Speed0Pressed, Speed10Pressed, Speed20Pressed, Speed30Pressed, Speed40Pressed, Speed50Pressed
            , Speed60Pressed, Speed70Pressed, Speed80Pressed, Speed90Pressed, Speed100Pressed
            , Speed110Pressed, Speed120Pressed, Speed130Pressed, Speed140Pressed, Speed150Pressed
            , Speed160Pressed, Speed170Pressed, Speed180Pressed, Speed190Pressed, Speed200Pressed;

        public virtual void UpdateMotiveForce(float elapsedClockSeconds, float AbsWheelSpeedMpS)
        {
            if (absMaxForceN == 0) absMaxForceN = Locomotive.MaxForceN;

            if (Locomotive.DynamicBrakePercent > 0)
                if (Locomotive.DynamicBrakePercent > 100)
                    Locomotive.DynamicBrakePercent = 100;
            ForceThrottleAndDynamicBrake = Locomotive.DynamicBrakePercent;

            if (DynamicBrakeFullRangeIncreaseTimeSeconds == 0)
                DynamicBrakeFullRangeIncreaseTimeSeconds = 4;
            if (DynamicBrakeFullRangeDecreaseTimeSeconds == 0)
                DynamicBrakeFullRangeDecreaseTimeSeconds = 6;
            float speedDiff = AbsWheelSpeedMpS - Locomotive.AbsSpeedMpS;
            foreach (MSTSLocomotive loco in playerNotDriveableTrainLocomotives)
            {
                if ((loco.AbsWheelSpeedMpS - loco.AbsSpeedMpS) > speedDiff)
                    speedDiff = loco.AbsWheelSpeedMpS - loco.AbsSpeedMpS;
            }
            float newThrotte = 0;
            // calculate new max force if MaxPowerThreshold is set
            if (MaxPowerThreshold > 0)
            {
                float currentSpeed = SpeedIsMph ? MpS.ToMpH(AbsWheelSpeedMpS) : MpS.ToKpH(AbsWheelSpeedMpS);
                float percentComplete = (int)Math.Round((double)(100 * currentSpeed) / MaxPowerThreshold);
                if (percentComplete > 100)
                    percentComplete = 100;
                newThrotte = percentComplete;
            }

            int count = 0;
            TrainElevation = 0;
            foreach (TrainCar tc in Locomotive.Train.Cars)
            {
                count++;
                TrainElevation += tc.Flipped ? tc.CurrentElevationPercent : -tc.CurrentElevationPercent;
            }
            TrainElevation = TrainElevation / count;

            if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Release ||
                Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Neutral)
            {
                if (TrainBrakePriority && SelectedMaxAccelerationStep > 0 && ForceResetRequiredAfterBraking)
                {
                    if (Locomotive.DynamicBrakePercent > 0 && SelectedSpeedMpS > 0)
                        Locomotive.SetDynamicBrakePercent(0);
                    CCThrottleOrDynBrakePercent = 0;
                    Locomotive.ThrottlePercent = 0;
                    return;
                }
                TrainBrakePriority = false;
            }
            if (DynamicBrakePriority) CCThrottleOrDynBrakePercent = 0;
            {

                if (TrainBrakePriority || DynamicBrakePriority)
                {
                    WasForceReset = false;
                    WasBraking = true;
                }

                if ((SpeedSelMode == SpeedSelectorMode.On || SpeedSelMode == SpeedSelectorMode.Start) && !TrainBrakePriority)
                {
                    canAddForce = true;
                }
                else
                {
                    canAddForce = false;
                    timeFromEngineMoved = 0;
                    reducingForce = true;
                    Locomotive.TractiveForceN = 0;
                    if (TrainBrakePriority)
                    {
                        if (SpeedSelMode == SpeedSelectorMode.Parking)
                            if (AbsWheelSpeedMpS < (SpeedIsMph ? MpS.FromMpH(ParkingBrakeEngageSpeed) : MpS.FromKpH(ParkingBrakeEngageSpeed)))
                                Locomotive.SetEngineBrakePercent(ParkingBrakePercent);
                        if (Locomotive.DynamicBrakePercent > 0 && SelectedSpeedMpS > 0)
                            Locomotive.SetDynamicBrakePercent(0);
                        CCThrottleOrDynBrakePercent = 0;
                        Locomotive.ThrottlePercent = 0;
                        return;
                    }
                }


                if ((SelectedMaxAccelerationStep == 0 && SelectedMaxAccelerationPercent == 0) || SpeedSelMode == SpeedSelectorMode.Start)
                    WasForceReset = true;

                if (SelectedMaxAccelerationPercent == 0 && SelectedMaxAccelerationStep == 0)
                {
                    WasBraking = false;
                    if (SpeedRegMode == SpeedRegulatorMode.Auto && UseThrottleAsForceSelector) Locomotive.ThrottleController.SetPercent(0);
                    Locomotive.SetThrottlePercent(0);
                }
                if (ForceResetRequiredAfterBraking && WasBraking && (SelectedMaxAccelerationStep > 0 || SelectedMaxAccelerationPercent > 0))
                {
                    Locomotive.SetThrottlePercent(0);
                    CCThrottleOrDynBrakePercent = 0;
                    maxForceN = 0;
                    if (SpeedSelMode == SpeedSelectorMode.Parking)
                        if (AbsWheelSpeedMpS < (SpeedIsMph ? MpS.FromMpH(ParkingBrakeEngageSpeed) : MpS.FromKpH(ParkingBrakeEngageSpeed)))
                            Locomotive.SetEngineBrakePercent(ParkingBrakePercent);
                    return;
                }

                if (ForceResetRequiredAfterBraking && !WasForceReset)
                {
                    Locomotive.SetThrottlePercent(0);
                    CCThrottleOrDynBrakePercent = 0;
                    maxForceN = 0;
                    if (SpeedSelMode == SpeedSelectorMode.Parking)
                        if (AbsWheelSpeedMpS < (SpeedIsMph ? MpS.FromMpH(ParkingBrakeEngageSpeed) : MpS.FromKpH(ParkingBrakeEngageSpeed)))
                            Locomotive.SetEngineBrakePercent(ParkingBrakePercent);
                    return;
                }


                if (canAddForce)
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

                if (!(UseTrainBrakeAndDynBrake && CCIsUsingTrainBrake) && Locomotive.TrainBrakeController.MaxPressurePSI - Locomotive.BrakeSystem.BrakeLine1PressurePSI > 1)
                {
                    canAddForce = false;
                    reducingForce = true;
                    timeFromEngineMoved = 0;
                    maxForceN = 0;
                    if (CCThrottleOrDynBrakePercent > 0)
                        CCThrottleOrDynBrakePercent = 0;
                    Ampers = 0;
                    Locomotive.ThrottleController.SetPercent(0);
                    return;
                }
                else
                {
                    canAddForce = true;
                }

                if (SpeedRegulatorOptions.Contains("engageforceonnonzerospeed") && SelectedSpeedMpS > 0)
                {
                    SpeedSelMode = SpeedSelectorMode.On;
                    SpeedRegMode = SpeedRegulatorMode.Auto;
                    SkipThrottleDisplay = true;
                    reducingForce = false;
                }
                /*           if (SpeedRegulatorOptions.Contains("engageforceonnonzerospeed") && SelectedSpeedMpS == 0)
                           {
                               if (playerNotDriveableTrainLocomotives.Count > 0) // update any other than the player's locomotive in the consist throttles to percentage of the current force and the max force
                               {
                                   foreach (MSTSLocomotive lc in playerNotDriveableTrainLocomotives)
                                   {
                                       if (UseThrottle)
                                       {
                                           lc.SetThrottlePercent(0);
                                       }
                                       else
                                       {
                                           lc.IsAPartOfPlayerTrain = true;
                                           lc.ThrottleOverriden = 0;
                                       }
                                   }
                               }
                               Locomotive.TractiveForceN = Locomotive.MotiveForceN = 0;
                               Locomotive.SetThrottlePercent(0);
                               return;
                           }*/

                float t = 0;
                if (SpeedRegMode == SpeedRegulatorMode.Manual)
                    DynamicBrakePriority = false;

                if (RestrictedSpeedActive)
                    CheckRestrictedSpeedZone();
                if (DynamicBrakePriority)
                {
                    Locomotive.ThrottleController.SetPercent(0);
                    ForceThrottleAndDynamicBrake = -Locomotive.DynamicBrakePercent;
                    return;
                }

                if (firstIteration) // if this is exetuted the first time, let's check all other than player engines in the consist, and record them for further throttle manipulation
                {
                    if (!DoComputeNumberOfAxles) SelectedNumberOfAxles = (int)(Locomotive.Train.Length / 6.6f); // also set the axles, for better delta computing, if user omits to set it
                    foreach (TrainCar tc in Locomotive.Train.Cars)
                    {
                        if (tc.GetType() == typeof(MSTSLocomotive) || tc.GetType() == typeof(MSTSDieselLocomotive) || tc.GetType() == typeof(MSTSElectricLocomotive))
                        {
                            if (tc != Locomotive)
                            {
                                try
                                {
                                    playerNotDriveableTrainLocomotives.Add((MSTSLocomotive)tc);
                                }
                                catch { }
                            }
                        }
                    }
                    firstIteration = false;
                }

                if (SelectedMaxAccelerationStep == 0) // no effort, no throttle (i.e. for reverser change, etc) and return
                {
                    Locomotive.SetThrottlePercent(0);
                    if (Locomotive.DynamicBrakePercent > 0)
                        Locomotive.SetDynamicBrakePercent(0);
                }

                if (SpeedRegMode == SpeedRegulatorMode.Auto)
                {
                    if (SpeedSelMode == SpeedSelectorMode.Parking && !EngineBrakePriority)
                    {
                        if (Locomotive.DynamicBrakePercent > 0)
                        {
                            if (AbsWheelSpeedMpS == 0)
                            {
                                Locomotive.SetDynamicBrakePercent(0);
                                Locomotive.DynamicBrakeChangeActiveState(false);
                            }
                        }
                        if (!UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                        throttleIsZero = true;

                        if (AbsWheelSpeedMpS < (SpeedIsMph ? MpS.FromMpH(ParkingBrakeEngageSpeed) : MpS.FromKpH(ParkingBrakeEngageSpeed)))
                            Locomotive.SetEngineBrakePercent(ParkingBrakePercent);
                    }
                    else if (SpeedSelMode == SpeedSelectorMode.Neutral || SpeedSelMode < SpeedSelectorMode.Start && !SpeedRegulatorOptions.Contains("startfromzero") && AbsWheelSpeedMpS < SafeSpeedForAutomaticOperationMpS)
                    {
                        if (CCThrottleOrDynBrakePercent > 0)
                        {
                            float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                            step *= elapsedClockSeconds;
                            CCThrottleOrDynBrakePercent -= step;
                            if (CCThrottleOrDynBrakePercent < 0) CCThrottleOrDynBrakePercent = 0;
                            if (CCThrottleOrDynBrakePercent > 0 && CCThrottleOrDynBrakePercent < 0.1) CCThrottleOrDynBrakePercent = 0;
                        }

                        float deltaSpeedMpS = 0;
                        if (!RestrictedSpeedActive)
                            deltaSpeedMpS = SelectedSpeedMpS - AbsWheelSpeedMpS;
                        else
                            deltaSpeedMpS = CurrentSelectedSpeedMpS - AbsWheelSpeedMpS;

                        if (deltaSpeedMpS > 0)
                        {
                            if (CCThrottleOrDynBrakePercent < -0.1)
                            {
                                float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                CCThrottleOrDynBrakePercent += step;
                            }
                            else if (CCThrottleOrDynBrakePercent > 0.1)
                            {

                                float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                CCThrottleOrDynBrakePercent -= step;
                            }
                            else
                            {
                                CCThrottleOrDynBrakePercent = 0;
                            }
                        }

                        if (deltaSpeedMpS < 0) // start braking
                        {
                            if (maxForceN > 0)
                            {
                                if (CCThrottleOrDynBrakePercent > 0)
                                {
                                    float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    CCThrottleOrDynBrakePercent -= step;
                                }
                            }
                            else
                            {
                                if (Locomotive.DynamicBrakeAvailable)
                                {
                                    deltaSpeedMpS = 0;
                                    if (!RestrictedSpeedActive)
                                        deltaSpeedMpS = (SelectedSpeedMpS + (TrainElevation < -0.01 ? TrainElevation * (SelectedNumberOfAxles / 12) : 0)) - AbsWheelSpeedMpS;
                                    else
                                        deltaSpeedMpS = (CurrentSelectedSpeedMpS + (TrainElevation < -0.01 ? TrainElevation * (SelectedNumberOfAxles / 12) : 0)) - AbsWheelSpeedMpS;

                                    relativeAcceleration = (float)-Math.Sqrt(-StartReducingSpeedDeltaDownwards * deltaSpeedMpS);
                                    AccelerationDemandMpSS = (float)-Math.Sqrt(-StartReducingSpeedDeltaDownwards * deltaSpeedMpS);
                                    if (maxForceN > 0)
                                    {
                                        if (CCThrottleOrDynBrakePercent > 0)
                                        {
                                            float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                            step *= elapsedClockSeconds;
                                            CCThrottleOrDynBrakePercent -= step;
                                        }
                                    }
                                    if (maxForceN == 0)
                                    {
                                        if (!UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                                        if (relativeAcceleration < -1) relativeAcceleration = -1;
                                        if (Locomotive.DynamicBrakePercent < -(AccelerationDemandMpSS * 100) && AccelerationDemandMpSS < -0.05f)
                                        {
                                            if (DynamicBrakeIsSelectedForceDependant)
                                            {
                                                if (CCThrottleOrDynBrakePercent >
                                                    -(MaxForceSelectorIsDiscrete ? (int)SelectedMaxAccelerationStep : SelectedMaxAccelerationStep) * 100 / SpeedRegulatorMaxForceSteps)
                                                {
                                                    float step = 100 / DynamicBrakeFullRangeIncreaseTimeSeconds;
                                                    step *= elapsedClockSeconds;
                                                    CCThrottleOrDynBrakePercent -= step;
                                                }
                                            }
                                            else
                                            {
                                                if (CCThrottleOrDynBrakePercent > -100)
                                                {
                                                    float step = 100 / DynamicBrakeFullRangeIncreaseTimeSeconds;
                                                    step *= elapsedClockSeconds;
                                                    CCThrottleOrDynBrakePercent -= step;
                                                }
                                            }
                                        }
                                        if (Locomotive.DynamicBrakePercent > -((AccelerationDemandMpSS - 0.05f) * 100))
                                        {
                                            if (CCThrottleOrDynBrakePercent < 0)
                                            {
                                                float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                                step *= elapsedClockSeconds;
                                                CCThrottleOrDynBrakePercent += step;
                                            }
                                        }
                                    }
                                }
                                else // use TrainBrake
                                {
                                    if (deltaSpeedMpS > -0.1)
                                    {
                                        if (!UseThrottle)
                                            Locomotive.ThrottleController.SetPercent(100);
                                        throttleIsZero = false;
                                        maxForceN = 0;
                                    }
                                    else if (deltaSpeedMpS > -1)
                                    {
                                        if (!UseThrottle)
                                            Locomotive.ThrottleController.SetPercent(0);
                                        throttleIsZero = true;

                                        brakePercent = TrainBrakeMinPercentValue - 3.0f + (-deltaSpeedMpS * 10);
                                    }
                                    else
                                    {
                                        Locomotive.TractiveForceN = 0;
                                        if (!UseThrottle)
                                            Locomotive.ThrottleController.SetPercent(0);
                                        throttleIsZero = true;

                                        if (RelativeAccelerationMpSS > -MaxDecelerationMpSS + 0.01f)
                                            brakePercent += 0.5f;
                                        else if (RelativeAccelerationMpSS < -MaxDecelerationMpSS - 0.01f)
                                            brakePercent -= 1;
                                        brakePercent = MathHelper.Clamp(brakePercent, TrainBrakeMinPercentValue - 3.0f, TrainBrakeMaxPercentValue);
                                    }
                                    Locomotive.SetTrainBrakePercent(brakePercent);
                                }
                                if (UseTrainBrakeAndDynBrake)
                                {
                                    if (-deltaSpeedMpS > SpeedDeltaToEnableTrainBrake)
                                    {
                                        CCIsUsingTrainBrake = true;
                                        /*                               brakePercent = Math.Max(TrainBrakeMinPercentValue + 3.0f, -deltaSpeedMpS * 2);
                                                                        if (brakePercent > TrainBrakeMaxPercentValue)
                                                                        brakePercent = TrainBrakeMaxPercentValue;*/
                                        brakePercent = (TrainBrakeMaxPercentValue - TrainBrakeMinPercentValue - 3.0f) * SelectedMaxAccelerationStep / SpeedRegulatorMaxForceSteps + TrainBrakeMinPercentValue + 3.0f;
                                        if (-deltaSpeedMpS < SpeedDeltaToEnableFullTrainBrake)
                                            brakePercent = Math.Min(brakePercent, TrainBrakeMinPercentValue + 13.0f);
                                        Locomotive.SetTrainBrakePercent(brakePercent);
                                    }
                                    else if (-deltaSpeedMpS < SpeedDeltaToEnableTrainBrake)
                                    {
                                        brakePercent = 0;
                                        Locomotive.SetTrainBrakePercent(brakePercent);
                                        if (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) >= 4.98)
                                            CCIsUsingTrainBrake = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Locomotive.DynamicBrakeAvailable)
                            {
                                if (Locomotive.DynamicBrakePercent > 0)
                                {
                                    if (CCThrottleOrDynBrakePercent < 0)
                                    {
                                        float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        CCThrottleOrDynBrakePercent += step;
                                    }
                                }
                            }
                        }
                    }

                    if ((AbsWheelSpeedMpS > SafeSpeedForAutomaticOperationMpS || SpeedSelMode == SpeedSelectorMode.Start || SpeedRegulatorOptions.Contains("startfromzero")) && (SpeedSelMode != SpeedSelectorMode.Neutral && SpeedSelMode != SpeedSelectorMode.Parking))
                    {
                        float deltaSpeedMpS = 0;
                        if (!RestrictedSpeedActive)
                            deltaSpeedMpS = SelectedSpeedMpS - AbsWheelSpeedMpS;
                        else
                            deltaSpeedMpS = CurrentSelectedSpeedMpS - AbsWheelSpeedMpS;
                        float coeff = 1;
                        float speed = SpeedIsMph ? MpS.ToMpH(Locomotive.WheelSpeedMpS) : MpS.ToKpH(Locomotive.WheelSpeedMpS);
                        if (speed > 100)
                        {
                            coeff = (speed / 100) * 1.2f;
                        }
                        else
                        {
                            coeff = 1;
                        }
                        float tempAccDemand = AccelerationDemandMpSS;
                        AccelerationDemandMpSS = (float)Math.Sqrt((StartReducingSpeedDelta) * coeff * (deltaSpeedMpS));
                        if (float.IsNaN(AccelerationDemandMpSS))
                        {
                            AccelerationDemandMpSS = tempAccDemand;
                        }
                        if (deltaSpeedMpS > 0.0f && Locomotive.DynamicBrakePercent < 1)
                        {
                            if (Locomotive.DynamicBrakePercent > 0)
                            {
                                float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                CCThrottleOrDynBrakePercent += step;
                            }
                            if (Locomotive.DynamicBrakePercent < 1 && Locomotive.DynamicBrake)
                            {
                                Locomotive.SetDynamicBrakePercent(0);
                                Locomotive.DynamicBrakeChangeActiveState(false);
                            }
                            relativeAcceleration = (float)Math.Sqrt(AccelerationRampMaxMpSSS * deltaSpeedMpS);
                        }
                        else // start braking
                        {
                            if (CCThrottleOrDynBrakePercent > 0)
                            {
                                float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                step *= elapsedClockSeconds;
                                CCThrottleOrDynBrakePercent -= step;
                                if (CCThrottleOrDynBrakePercent < 0) CCThrottleOrDynBrakePercent = 0;
                                if (CCThrottleOrDynBrakePercent > 0 && CCThrottleOrDynBrakePercent < 0.1) CCThrottleOrDynBrakePercent = 0;
                            }

                            if (deltaSpeedMpS < 0) // start braking
                            {
                                if (maxForceN > 0)
                                {
                                    if (CCThrottleOrDynBrakePercent > 0)
                                    {
                                        float step = 100 / ThrottleFullRangeDecreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        CCThrottleOrDynBrakePercent -= step;
                                    }
                                }
                                else
                                {
                                    if (Locomotive.DynamicBrakeAvailable)
                                    {
                                        relativeAcceleration = (float)-Math.Sqrt(-StartReducingSpeedDeltaDownwards * deltaSpeedMpS);

                                        float val = (StartReducingSpeedDeltaDownwards) * coeff * ((deltaSpeedMpS + 0.5f) / 3);
                                        if (val < 0)
                                            val = -val;
                                        AccelerationDemandMpSS = -(float)Math.Sqrt(val);
                                        if (maxForceN == 0)
                                        {
                                            if (!UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                                            if (RelativeAccelerationMpSS > AccelerationDemandMpSS)
                                            {
                                                if (DynamicBrakeIsSelectedForceDependant)
                                                {
                                                    if (CCThrottleOrDynBrakePercent >
                                                    -(MaxForceSelectorIsDiscrete ? (int)SelectedMaxAccelerationStep : SelectedMaxAccelerationStep) * 100 / SpeedRegulatorMaxForceSteps)
                                                    {
                                                        float step = 100 / DynamicBrakeFullRangeIncreaseTimeSeconds;
                                                        step *= elapsedClockSeconds;
                                                        if (step > (RelativeAccelerationMpSS - AccelerationDemandMpSS) * 2)
                                                            step = (RelativeAccelerationMpSS - AccelerationDemandMpSS) * 2;
                                                        CCThrottleOrDynBrakePercent -= step;
                                                        if (CCThrottleOrDynBrakePercent < -100)
                                                            CCThrottleOrDynBrakePercent = -100;
                                                    }
                                                    if (SelectedMaxAccelerationStep == 0 && CCThrottleOrDynBrakePercent < 0)
                                                    {
                                                        float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                                        step *= elapsedClockSeconds;
                                                        if (step > (AccelerationDemandMpSS - RelativeAccelerationMpSS) * 2)
                                                            step = (AccelerationDemandMpSS - RelativeAccelerationMpSS) * 2;
                                                        CCThrottleOrDynBrakePercent -= step;
                                                    }
                                                }
                                                else
                                                {
                                                    if (CCThrottleOrDynBrakePercent > -100)
                                                    {
                                                        float step = 100 / DynamicBrakeFullRangeIncreaseTimeSeconds;
                                                        step *= elapsedClockSeconds;
                                                        if (step > (RelativeAccelerationMpSS - AccelerationDemandMpSS) * 2)
                                                            step = (RelativeAccelerationMpSS - AccelerationDemandMpSS) * 2;
                                                        CCThrottleOrDynBrakePercent -= step;
                                                        if (CCThrottleOrDynBrakePercent < -100)
                                                            CCThrottleOrDynBrakePercent = -100;
                                                    }
                                                }
                                            }
                                            if (RelativeAccelerationMpSS + 0.01f < AccelerationDemandMpSS)
                                            {
                                                if (CCThrottleOrDynBrakePercent < 0)
                                                {
                                                    float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                                    step *= elapsedClockSeconds;
                                                    if (step > (AccelerationDemandMpSS - RelativeAccelerationMpSS) * 2)
                                                        step = (AccelerationDemandMpSS - RelativeAccelerationMpSS) * 2;
                                                    CCThrottleOrDynBrakePercent += step;
                                                    if (DynamicBrakeIsSelectedForceDependant)
                                                    {
                                                        if (CCThrottleOrDynBrakePercent < Math.Round(-(MaxForceSelectorIsDiscrete ? (int)SelectedMaxAccelerationStep : SelectedMaxAccelerationStep) * 100 / SpeedRegulatorMaxForceSteps, 0))
                                                        {
                                                            CCThrottleOrDynBrakePercent = (float)Math.Round(-(MaxForceSelectorIsDiscrete ? (int)SelectedMaxAccelerationStep : SelectedMaxAccelerationStep) * 100 / SpeedRegulatorMaxForceSteps, 0);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else // use TrainBrake
                                    {
                                        if (deltaSpeedMpS > -0.1)
                                        {
                                            if (!UseThrottle)
                                                Locomotive.ThrottleController.SetPercent(CCThrottleOrDynBrakePercent);
                                            throttleIsZero = false;
                                            maxForceN = 0;
                                        }
                                        else if (deltaSpeedMpS > -1)
                                        {
                                            if (!UseThrottle)
                                                Locomotive.ThrottleController.SetPercent(0);
                                            throttleIsZero = true;

                                            brakePercent = TrainBrakeMinPercentValue -3.0f + (-deltaSpeedMpS * 10);
                                        }
                                        else
                                        {
                                            Locomotive.TractiveForceN = 0;
                                            if (!UseThrottle)
                                                Locomotive.ThrottleController.SetPercent(0);
                                            throttleIsZero = true;

                                            if (RelativeAccelerationMpSS > -MaxDecelerationMpSS + 0.01f)
                                                brakePercent += 0.5f;
                                            else if (RelativeAccelerationMpSS < -MaxDecelerationMpSS - 0.01f)
                                                brakePercent -= 1;
                                            brakePercent = MathHelper.Clamp(brakePercent, TrainBrakeMinPercentValue - 3.0f, TrainBrakeMaxPercentValue);
                                        }
                                        Locomotive.SetTrainBrakePercent(brakePercent);
                                    }
                                    if (UseTrainBrakeAndDynBrake)
                                    {
                                        if (-deltaSpeedMpS > SpeedDeltaToEnableTrainBrake)
                                        {
                                            CCIsUsingTrainBrake = true;
                                            /*                               brakePercent = Math.Max(TrainBrakeMinPercentValue + 3.0f, -deltaSpeedMpS * 2);
                                                                            if (brakePercent > TrainBrakeMaxPercentValue)
                                                                            brakePercent = TrainBrakeMaxPercentValue;*/
                                            brakePercent = (TrainBrakeMaxPercentValue - TrainBrakeMinPercentValue - 3.0f) * SelectedMaxAccelerationStep / SpeedRegulatorMaxForceSteps + TrainBrakeMinPercentValue + 3.0f;
                                            if (-deltaSpeedMpS < SpeedDeltaToEnableFullTrainBrake)
                                                brakePercent = Math.Min(brakePercent, TrainBrakeMinPercentValue + 13.0f);
                                            Locomotive.SetTrainBrakePercent(brakePercent);
                                        }
                                        else if (-deltaSpeedMpS < SpeedDeltaToEnableTrainBrake)
                                        {
                                            brakePercent = 0;
                                            Locomotive.SetTrainBrakePercent(brakePercent);
                                            if (Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) >= 4.98)
                                                CCIsUsingTrainBrake = false;
                                        }
                                    }
                                }
                            }
                        }
                        if (relativeAcceleration > 1.0f)
                            relativeAcceleration = 1.0f;

                        if ((SpeedSelMode == SpeedSelectorMode.On || SpeedSelMode == SpeedSelectorMode.Start) && deltaSpeedMpS > 0)
                        {
                            if (Locomotive.DynamicBrakePercent > 0)
                            {
                                if (CCThrottleOrDynBrakePercent <= 0)
                                {
                                    float step = 100 / DynamicBrakeFullRangeDecreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    if (step > (AccelerationDemandMpSS - RelativeAccelerationMpSS) * 2)
                                        step = (AccelerationDemandMpSS - RelativeAccelerationMpSS) * 2;
                                    CCThrottleOrDynBrakePercent += step;
                                }
                            }
                            else
                            {
                                if (!UseThrottle)
                                {
                                    if (SelectedMaxAccelerationPercent == 0 && SelectedMaxAccelerationStep == 0)
                                        Locomotive.ThrottleController.SetPercent(0);
                                    else
                                        Locomotive.ThrottleController.SetPercent(CCThrottleOrDynBrakePercent);
                                }
                                throttleIsZero = false;
                            }
                        }
                        float a = 0;
                        if (Locomotive.LocomotivePowerSupply.MainPowerSupplyOn && Locomotive.Direction != Direction.N)
                        {
                            if (Locomotive.DynamicBrakePercent < 0)
                            {
                                if (RelativeAccelerationMpSS < AccelerationDemandMpSS)
                                {
                                    if (ForceStepsThrottleTable.Count > 0)
                                    {
                                        t = ForceStepsThrottleTable[(int)SelectedMaxAccelerationStep - 1];
                                        if (AccelerationTable.Count > 0)
                                            a = AccelerationTable[(int)SelectedMaxAccelerationStep - 1];
                                    }
                                    else
                                        t = (MaxForceSelectorIsDiscrete ? (int)SelectedMaxAccelerationStep : SelectedMaxAccelerationStep) * 100 / SpeedRegulatorMaxForceSteps;
                                    if (t < newThrotte)
                                        t = newThrotte;
                                    t /= 100;
                                }
                            }
                            if (reducingForce)
                            {
                                if (t > PowerReductionValue / 100)
                                    t = PowerReductionValue / 100;
                            }
                            float DemandedThrottleOrDynBrakePercent = t * 100;
                            float current = maxForceN / Locomotive.MaxForceN * Locomotive.MaxCurrentA;
                            if (current < PowerBreakoutAmpers)
                                breakout = true;
                            if (breakout && deltaSpeedMpS > 0.2f)
                                breakout = false;
                            if (UseThrottle) // not valid for diesel engines.
                                breakout = false;
                            if ((CCThrottleOrDynBrakePercent != DemandedThrottleOrDynBrakePercent) && deltaSpeedMpS > 0)
                            {
                                if (a > 0 && (SpeedIsMph ? MpS.ToMpH(Locomotive.WheelSpeedMpS) : MpS.ToKpH(Locomotive.WheelSpeedMpS)) > 5)
                                {
                                    if (CCThrottleOrDynBrakePercent < DemandedThrottleOrDynBrakePercent && Locomotive.AccelerationMpSS < a - 0.02)
                                    {
                                        float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        CCThrottleOrDynBrakePercent += step;
                                    }
                                }
                                else
                                {
                                    if (CCThrottleOrDynBrakePercent < DemandedThrottleOrDynBrakePercent && DemandedThrottleOrDynBrakePercent >= 0)
                                    {
                                        float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        float accelDiff = AccelerationDemandMpSS - Locomotive.AccelerationMpSS;
                                        if (step / 10 > accelDiff)
                                            step = accelDiff * 10;
                                        CCThrottleOrDynBrakePercent += step;
                                    }
                                }
                                if (a > 0 && (SpeedIsMph ? MpS.ToMpH(Locomotive.WheelSpeedMpS) : MpS.ToKpH(Locomotive.WheelSpeedMpS)) > 5)
                                {
                                    if (CCThrottleOrDynBrakePercent > DemandedThrottleOrDynBrakePercent && Locomotive.AccelerationMpSS > a + 0.02)
                                    {
                                        float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        CCThrottleOrDynBrakePercent -= step;
                                    }
                                }
                                else
                                {
                                    if (CCThrottleOrDynBrakePercent - 0.2f > DemandedThrottleOrDynBrakePercent)
                                    {
                                        float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        CCThrottleOrDynBrakePercent -= step;
                                    }
                                }
                                if (CCThrottleOrDynBrakePercent > DemandedThrottleOrDynBrakePercent && deltaSpeedMpS < 0.8)
                                {
                                    float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                    step *= elapsedClockSeconds;
                                    CCThrottleOrDynBrakePercent -= step;
                                }
                            }
                            if (a > 0 && (SpeedIsMph ? MpS.ToMpH(Locomotive.WheelSpeedMpS) : MpS.ToKpH(Locomotive.WheelSpeedMpS)) > 5)
                            {
                                if ((a != Locomotive.AccelerationMpSS) && deltaSpeedMpS > 0.8)
                                {
                                    if (Locomotive.AccelerationMpSS < a + 0.02)
                                    {
                                        float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        CCThrottleOrDynBrakePercent += step;
                                    }
                                    if (Locomotive.AccelerationMpSS > a - 0.02)
                                    {
                                        float step = 100 / ThrottleFullRangeIncreaseTimeSeconds;
                                        step *= elapsedClockSeconds;
                                        CCThrottleOrDynBrakePercent -= step;
                                    }
                                }
                            }

                            if (UseThrottle)
                            {
                                if (CCThrottleOrDynBrakePercent > 0)
                                    Locomotive.ThrottleController.SetPercent(CCThrottleOrDynBrakePercent);
                            }
                        }
                    }
                    else if (UseThrottle)
                    {
                        if (Locomotive.ThrottlePercent > 0)
                        {
                            float newValue = (Locomotive.ThrottlePercent - 1) / 100;
                            if (newValue < 0)
                                newValue = 0;
                            Locomotive.StartThrottleDecrease(newValue);
                        }
                    }

                    if (Locomotive.WheelSpeedMpS == 0 && CCThrottleOrDynBrakePercent < 0)
                        CCThrottleOrDynBrakePercent = 0;
                    ForceThrottleAndDynamicBrake = CCThrottleOrDynBrakePercent;

                    if (CCThrottleOrDynBrakePercent > 0)
                    {
                        if (speedDiff > AntiWheelSpinSpeedDiffThreshold)
                        {
                            skidSpeedDegratation += 0.05f;
                        }
                        else if (skidSpeedDegratation > 0)
                        {
                            skidSpeedDegratation -= 0.1f;
                        }
                        if (speedDiff < AntiWheelSpinSpeedDiffThreshold - 0.05f)
                            skidSpeedDegratation = 0;
                        if (AntiWheelSpinEquipped)
                            CCThrottleOrDynBrakePercent -= skidSpeedDegratation;
                        if (breakout || Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) < 4.98)
                        {
                            maxForceN = 0;
                            CCThrottleOrDynBrakePercent = 0;
                            Ampers = 0;
                            if (!UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                        }
                        else
                        {
                            if (Locomotive.ThrottlePercent < 100 && SpeedSelMode != SpeedSelectorMode.Parking && !UseThrottle)
                            {
                                if (SelectedMaxAccelerationPercent == 0 && SelectedMaxAccelerationStep == 0)
                                {
                                    Locomotive.ThrottleController.SetPercent(0);
                                    throttleIsZero = true;
                                }
                                else
                                {
                                    Locomotive.ThrottleController.SetPercent(CCThrottleOrDynBrakePercent);
                                    throttleIsZero = false;
                                }
                            }
                            if (Locomotive.DynamicBrakePercent > -1)
                            {
                                Locomotive.SetDynamicBrakePercent(0);
                                Locomotive.DynamicBrakeChangeActiveState(false);
                            }

                            if (Locomotive.TractiveForceCurves != null && !UseThrottle)
                            {
                                maxForceN = Locomotive.TractiveForceCurves.Get(CCThrottleOrDynBrakePercent / 100, AbsWheelSpeedMpS) * (1 - Locomotive.PowerReduction);
                            }
                            else
                            {
                                if (Locomotive.TractiveForceCurves == null)
                                {
                                    maxForceN = Locomotive.MaxForceN * (CCThrottleOrDynBrakePercent / 100);
                                    //                               if (maxForceN * AbsWheelSpeedMpS > Locomotive.MaxPowerW * (CCThrottleOrDynBrakePercent / 100))
                                    //                                   maxForceN = Locomotive.MaxPowerW / AbsWheelSpeedMpS * (CCThrottleOrDynBrakePercent / 100);
                                    if (Locomotive.MaxForceN * AbsWheelSpeedMpS > Locomotive.MaxPowerW * (CCThrottleOrDynBrakePercent / 100))
                                        maxForceN = Locomotive.MaxPowerW / AbsWheelSpeedMpS * (CCThrottleOrDynBrakePercent / 100) * (CCThrottleOrDynBrakePercent / 100);
                                    maxForceN *= 1 - Locomotive.PowerReduction;
                                }
                                else
                                    maxForceN = Locomotive.TractiveForceCurves.Get(CCThrottleOrDynBrakePercent / 100, AbsWheelSpeedMpS) * (1 - Locomotive.PowerReduction);
                            }
                        }
                    }
                    else if (CCThrottleOrDynBrakePercent < 0)
                    {
                        if (maxForceN > 0) maxForceN = 0;
                        if (Locomotive.ThrottlePercent > 0) Locomotive.ThrottleController.SetPercent(0);
                        if (Locomotive.DynamicBrakeAvailable)
                        {
                            if (Locomotive.DynamicBrakePercent <= 0)
                            {
                                string status = Locomotive.GetDynamicBrakeStatus();
                                Locomotive.DynamicBrakeChangeActiveState(true);
                            }
                            if (SelectedMaxAccelerationPercent == 0 && SelectedMaxAccelerationStep == 0)
                            {
                                Locomotive.SetDynamicBrakePercent(0);
                                Locomotive.DynamicBrakePercent = 0;
                                CCThrottleOrDynBrakePercent = 0;
                            }
                            else
                            {
                                Locomotive.SetDynamicBrakePercent(-CCThrottleOrDynBrakePercent);
                                Locomotive.DynamicBrakePercent = -CCThrottleOrDynBrakePercent;
                            }
                        }
                        else if (SelectedMaxAccelerationPercent == 0 && SelectedMaxAccelerationStep == 0)
                            CCThrottleOrDynBrakePercent = 0;
                    }
                    else if (CCThrottleOrDynBrakePercent == 0)
                    {
                        if (!breakout)
                        {

                            /*if (Locomotive.MultiPositionController.controllerPosition == Controllers.MultiPositionController.ControllerPosition.DynamicBrakeIncrease || Locomotive.MultiPositionController.controllerPosition == Controllers.MultiPositionController.ControllerPosition.DynamicBrakeIncreaseFast)
                            {
                                CCThrottleOrDynBrakePercent = -Locomotive.DynamicBrakePercent;
                            }
                            else
                            {*/
                            if (maxForceN > 0) maxForceN = 0;
                            if (Locomotive.ThrottlePercent > 0 && !UseThrottle) Locomotive.ThrottleController.SetPercent(0);
                            if (Locomotive.DynamicBrakeAvailable && Locomotive.DynamicBrakePercent > -1)
                            {
                                Locomotive.SetDynamicBrakePercent(0);
                                Locomotive.DynamicBrakeChangeActiveState(false);
                            }
                        }
                    }

                    if (!Locomotive.LocomotivePowerSupply.MainPowerSupplyOn)
                    {
                        CCThrottleOrDynBrakePercent = 0;
                        Locomotive.ThrottleController.SetPercent(0);
                        if (Locomotive.DynamicBrakeAvailable && Locomotive.DynamicBrakePercent > 0)
                            Locomotive.SetDynamicBrakePercent(0);
                        Locomotive.DynamicBrakeIntervention = -1;
                        maxForceN = 0;
                        ForceThrottleAndDynamicBrake = 0;
                        Ampers = 0;
                    }
                    else
                        ForceThrottleAndDynamicBrake = CCThrottleOrDynBrakePercent;

                    Locomotive.MotiveForceN = maxForceN;
                    Locomotive.TractiveForceN = maxForceN;
                }
            }

            if (playerNotDriveableTrainLocomotives.Count > 0) // update any other than the player's locomotive in the consist throttles to percentage of the current force and the max force
            {
                float locoPercent = Locomotive.MaxForceN - (Locomotive.MaxForceN - Locomotive.MotiveForceN);
                locoPercent = (locoPercent / Locomotive.MaxForceN) * 100;
                //Simulator.Confirmer.MSG(locoPercent.ToString());
                foreach (MSTSLocomotive lc in playerNotDriveableTrainLocomotives)
                {
                    if (Locomotive.LocomotivePowerSupply.MainPowerSupplyOn)
                    {
                        if (UseThrottle)
                        {
                            lc.SetThrottlePercent(Locomotive.ThrottlePercent);
                        }
                        else
                        {
                            LocoIsAPartOfPlayerTrain = true;
                            ThrottleOverriden = locoPercent / 100;
                        }
                    }
                    else
                    {
                        if (UseThrottle)
                        {
                            lc.SetThrottlePercent(0);
                        }
                        else
                        {
                            LocoIsAPartOfPlayerTrain = true;
                            ThrottleOverriden = 0;
                        }
                    }
                }
            }
        }

        private float previousSelectedSpeed = 0;
        public float GetDataOf(CabViewControl cvc)
        {
            float data = 0;
            switch (cvc.ControlType)
            { 
                case CABViewControlTypes.ORTS_SELECTED_SPEED:
                case CABViewControlTypes.ORTS_SELECTED_SPEED_DISPLAY:
                    bool metric = cvc.Units == CABViewControlUnits.KM_PER_HOUR;
                    float temp = (float)Math.Round(RestrictedSpeedActive ? MpS.FromMpS(CurrentSelectedSpeedMpS, metric) : MpS.FromMpS(SelectedSpeedMpS, metric));
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
                    data = (float)Math.Round(RestrictedSpeedActive ? MpS.FromMpS(CurrentSelectedSpeedMpS, metric) : MpS.FromMpS(SelectedSpeedMpS, metric));
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_MAXIMUM_ACCELERATION:
                    if (SpeedRegMode == SpeedRegulatorMode.Auto || MaxForceKeepSelectedStepWhenManualModeSet)
                    {
                        data = SelectedMaxAccelerationStep * (float)cvc.MaxValue / SpeedRegulatorMaxForceSteps;
                    }
                    else
                        data = 0;
                    break;
                case CABViewControlTypes.ORTS_RESTRICTED_SPEED_ZONE_ACTIVE:
                    data = RestrictedSpeedActive ? 1 : 0;
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
                case CABViewControlTypes.ORTS_REMAINING_TRAIN_LENGHT_SPEED_RESTRICTED:
                    if (RemainingTrainLengthToPassRestrictedZone == 0)
                        data = 0;
                    else
                        data = TrainLengthMeters - RemainingTrainLengthToPassRestrictedZone;
                    break;
                case CABViewControlTypes.ORTS_REMAINING_TRAIN_LENGTH_PERCENT:
                    if (SpeedRegMode != CruiseControl.SpeedRegulatorMode.Auto)
                    {
                        data = 0;
                        break;
                    }
                    if (TrainLengthMeters > 0 && RemainingTrainLengthToPassRestrictedZone > 0)
                    {
                        data = (((float)TrainLengthMeters - (float)RemainingTrainLengthToPassRestrictedZone) / (float)TrainLengthMeters) * 100;
                    }
                    break;
                case CABViewControlTypes.ORTS_MOTIVE_FORCE:
                    data = Locomotive.FilteredMotiveForceN;
                    break;
                case CABViewControlTypes.ORTS_MOTIVE_FORCE_KILONEWTON:
                    if (Locomotive.FilteredMotiveForceN > Locomotive.DynamicBrakeForceN)
                        data = (float)Math.Round(Locomotive.FilteredMotiveForceN / 1000, 0);
                    else if (Locomotive.DynamicBrakeForceN > 0)
                        data = -(float)Math.Round(Locomotive.DynamicBrakeForceN / 1000, 0);
                    break;
                case CABViewControlTypes.ORTS_MAXIMUM_FORCE:
                    data = Locomotive.MaxForceN;
                    break;
                case CABViewControlTypes.ORTS_FORCE_IN_PERCENT_THROTTLE_AND_DYNAMIC_BRAKE:
                    if (SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto)
                    {
                        data = ForceThrottleAndDynamicBrake;
                        if (Locomotive.DynamicBrakePercent > 0 && data > -Locomotive.DynamicBrakePercent) data = -Locomotive.DynamicBrakePercent;
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent > 0)
                        {
                            data = Locomotive.ThrottlePercent;
                        }
                        else if (Locomotive.DynamicBrakePercent > 0 && Locomotive.AbsSpeedMpS > 0)
                        {
                            data = -Locomotive.DynamicBrakePercent;
                        }
                        else data = 0;
                    }
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
                case CABViewControlTypes.ORTS_CC_SELECT_SPEED:
                    data = SelectingSpeedPressed ? 1 : 0;
                    break;
                case CABViewControlTypes.ORTS_CC_SPEED_0:
                    {
                        data = Speed0Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_10:
                    {
                        data = Speed10Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_20:
                    {
                        data = Speed20Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_30:
                    {
                        data = Speed30Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_40:
                    {
                        data = Speed40Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_50:
                    {
                        data = Speed50Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_60:
                    {
                        data = Speed60Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_70:
                    {
                        data = Speed70Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_80:
                    {
                        data = Speed80Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_90:
                    {
                        data = Speed90Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_100:
                    {
                        data = Speed100Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_110:
                    {
                        data = Speed110Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_120:
                    {
                        data = Speed120Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_130:
                    {
                        data = Speed130Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_140:
                    {
                        data = Speed140Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_150:
                    {
                        data = Speed150Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_160:
                    {
                        data = Speed160Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_170:
                    {
                        data = Speed170Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_180:
                    {
                        data = Speed180Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_190:
                    {
                        data = Speed190Pressed ? 1 : 0;
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_200:
                    {
                        data = Speed200Pressed ? 1 : 0;
                        break;
                    }
                default:
                    data = 0;
                    break;
            }
            return data;
        }

        public string GetCruiseControlStatus ()
        {
            var cruiseControlStatus = SpeedRegMode.ToString();
            return cruiseControlStatus;
        }

        public enum AvvSignal {
            Stop,
            Restricted,
            Restricting40,
            Clear,
            Restricting60,
            Restricting80,
            Restricting100
        };

        public enum ControllerCruiseControlLogic
        {
            None,
            Full,
            SpeedOnly
        }

        public AvvSignal avvSignal = AvvSignal.Stop;
        public void DrawAvvSignal(AvvSignal ToState)
        {
            avvSignal = ToState;
        }
    }
}
