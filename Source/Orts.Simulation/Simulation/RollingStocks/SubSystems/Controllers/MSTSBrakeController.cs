// COPYRIGHT 2010, 2012 by the Open Rails project.
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

using ORTS.Scripting.Api;
using System;
using System.Diagnostics;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
{
    /**
     * This is the a Controller used to control brakes.
     * 
     * This is mainly a Notch controller, but it allows continuous input and also 
     * has specific methods to update brake status.
     * 
     */
    public class MSTSBrakeController: BrakeController
    {
        public MSTSNotchController NotchController;

        /// <summary>
        /// Setting to workaround MSTS bug of not abling to set this function correctly in .eng file
        /// </summary>
        public bool ForceControllerReleaseGraduated;
        bool BrakeControllerInitialised; // flag to allow PreviousNotchPosition to be initially set.
        MSTSNotch PreviousNotchPosition;

        bool EnforceMinimalReduction = false;

        public MSTSBrakeController()
        {
        }

        public override void Initialize()
        {
            NotchController = new MSTSNotchController(Notches());
            NotchController.SetValue(CurrentValue());
            NotchController.IntermediateValue = CurrentValue();
            NotchController.MinimumValue = MinimumValue();
            NotchController.MaximumValue = MaximumValue();
            NotchController.StepSize = StepSize();
            BrakeControllerInitialised = false;       // set to false so that the PreviousNotchPosition value can be initialised around the first update loop     
        }

        public override void InitializeMoving()
        {
            NotchController.SetValue(0);
            NotchController.CurrentNotch = 0;
        }

        public override float Update(float elapsedSeconds)
        {
            float value = NotchController.Update(elapsedSeconds);
            SetCurrentValue(value);
            SetUpdateValue(NotchController.UpdateValue);
            return value;
        }

        // Train Brake Controllers
        public override void UpdatePressure(ref float pressureBar, float elapsedClockSeconds, ref float epControllerState)
        {
            var epState = -1f;

            if (EmergencyBrakingPushButton() || TCSEmergencyBraking())
            {
                pressureBar -= EmergencyRateBarpS() * elapsedClockSeconds;
            }
            else if (TCSFullServiceBraking())
            {
                if (pressureBar > MaxPressureBar() - FullServReductionBar())
                    pressureBar -= ApplyRateBarpS() * elapsedClockSeconds;
                else if (pressureBar < MaxPressureBar() - FullServReductionBar())
                    pressureBar = MaxPressureBar() - FullServReductionBar();
            }
            else
            {
                MSTSNotch notch = NotchController.GetCurrentNotch();

                if (!BrakeControllerInitialised) // The first time around loop, PreviousNotchPosition will be set up front with current value, this will stop crashes due to vsalue not being initialised. 
                {
                    PreviousNotchPosition = NotchController.GetCurrentNotch();
                    BrakeControllerInitialised = true;
                }
                if (notch == null)
                {
                    pressureBar = MaxPressureBar() - FullServReductionBar() * CurrentValue();
                }
                else
                {
                    epState = 0;
                    float x = NotchController.GetNotchFraction();
                    var type = notch.Type;
                    if (OverchargeButtonPressed()) type = ControllerState.Overcharge;
                    else if (QuickReleaseButtonPressed()) type = ControllerState.FullQuickRelease;

                    switch (type)
                    {
                        case ControllerState.Hold:
                        case ControllerState.Lap:
                        case ControllerState.MinimalReduction:
                            if (EnforceMinimalReduction)
                                DecreasePressure(ref pressureBar, MaxPressureBar() - MinReductionBar(), ApplyRateBarpS(), elapsedClockSeconds);
                            break;
                        default:
                            EnforceMinimalReduction = false;
                            break;
                    }

                    switch (type)
                    {
                        case ControllerState.Release:
                            IncreasePressure(ref pressureBar, MaxPressureBar(), ReleaseRateBarpS(), elapsedClockSeconds);
                            DecreasePressure(ref pressureBar, MaxPressureBar(), OverchargeEliminationRateBarpS(), elapsedClockSeconds);
                            epState = -1;
                            break;
                        case ControllerState.FullQuickRelease:
                            IncreasePressure(ref pressureBar, MaxPressureBar(), QuickReleaseRateBarpS(), elapsedClockSeconds);
                            DecreasePressure(ref pressureBar, MaxPressureBar(), OverchargeEliminationRateBarpS(), elapsedClockSeconds);
                            epState = -1;
                            break;
                        case ControllerState.Overcharge:
                            IncreasePressure(ref pressureBar, Math.Min(MaxOverchargePressureBar(), MainReservoirPressureBar()), QuickReleaseRateBarpS(), elapsedClockSeconds);
                            epState = -1;
                            break;
                        case ControllerState.SlowService:
                            DecreasePressure(ref pressureBar, MaxPressureBar() - FullServReductionBar(), SlowApplicationRateBarpS(), elapsedClockSeconds);
                            break;
                        case ControllerState.StrBrkLap:
                        case ControllerState.StrBrkApply:
                        case ControllerState.StrBrkApplyAll:
                        case ControllerState.StrBrkEmergency:
                            // Nothing is done in these positions, instead they are controlled by the steam ejector in straight brake module
                            break;
                        case ControllerState.StrBrkRelease:
                        case ControllerState.StrBrkReleaseOn:
                            // This position is an on position so pressure will be zero (reversed due to vacuum brake operation)
                            pressureBar = 0;
                            break;
                        case ControllerState.StrBrkReleaseOff: //(reversed due to vacuum brake operation)
                        case ControllerState.Apply:
                            pressureBar -= x * ApplyRateBarpS() * elapsedClockSeconds;
                            break;
                        case ControllerState.FullServ:
                            epState = x;
                            EnforceMinimalReduction = true;
                            DecreasePressure(ref pressureBar, MaxPressureBar()-FullServReductionBar(), ApplyRateBarpS(), elapsedClockSeconds);
                            break;
                        case ControllerState.Lap:
                            // Lap position applies min service reduction when first selected, and previous contoller position was Running, then no change in pressure occurs 
                            if (PreviousNotchPosition.Type == ControllerState.Running)
                            {
                                EnforceMinimalReduction = true;
                                epState = -1;
                            }
                            break;
                        case ControllerState.MinimalReduction:
                            // Lap position applies min service reduction when first selected, and previous contoller position was Running or Release, then no change in pressure occurs                             
                            if (PreviousNotchPosition.Type == ControllerState.Running || PreviousNotchPosition.Type == ControllerState.Release || PreviousNotchPosition.Type == ControllerState.FullQuickRelease)
                            {
                                EnforceMinimalReduction = true;
                                epState = -1;
                            }
                            break;
                        case ControllerState.ManualBraking:
                        case ControllerState.VacContServ:
                        case ControllerState.VacApplyContServ:
                            // Continuous service position for vacuum brakes - allows brake to be adjusted up and down continuously between the ON and OFF position
                            pressureBar = (1 - x) * MaxPressureBar();
                            epState = -1;
                            break;
                        case ControllerState.EPApply:
                        case ControllerState.EPOnly:
                        case ControllerState.ContServ:
                        case ControllerState.EPFullServ:
                            epState = x;
                            if (notch.Type == ControllerState.EPApply || notch.Type == ControllerState.ContServ)
                            {
                                EnforceMinimalReduction = true;
                                x = MaxPressureBar() - MinReductionBar() * (1 - x) - FullServReductionBar() * x;
                                DecreasePressure(ref pressureBar, x, ApplyRateBarpS(), elapsedClockSeconds);
                                if (ForceControllerReleaseGraduated || notch.Type == ControllerState.EPApply)
                                    IncreasePressure(ref pressureBar, x, ReleaseRateBarpS(), elapsedClockSeconds);
                            }
                            break;
                        case ControllerState.GSelfLapH:
                        case ControllerState.Suppression:
                        case ControllerState.GSelfLap:
                            EnforceMinimalReduction = true;
                            x = MaxPressureBar() - MinReductionBar() * (1 - x) - FullServReductionBar() * x;
                            DecreasePressure(ref pressureBar, x, ApplyRateBarpS(), elapsedClockSeconds);
                            if (ForceControllerReleaseGraduated || notch.Type == ControllerState.GSelfLap)
                                IncreasePressure(ref pressureBar, x, ReleaseRateBarpS(), elapsedClockSeconds);
                            break;
                        case ControllerState.Emergency:
                            pressureBar -= EmergencyRateBarpS() * elapsedClockSeconds;
                            epState = 1;
                            break;
                        case ControllerState.Dummy:
                            x = MaxPressureBar() - FullServReductionBar() * (notch.Smooth ? x : CurrentValue());
                            IncreasePressure(ref pressureBar, x, ReleaseRateBarpS(), elapsedClockSeconds);
                            DecreasePressure(ref pressureBar, x, ApplyRateBarpS(), elapsedClockSeconds);
                            epState = -1;
                            break;
                    }

                    PreviousNotchPosition = NotchController.GetCurrentNotch();
                }
            }

            if (pressureBar < 0)
                pressureBar = 0;
            epControllerState = epState;
        }

        // Engine Brake Controllers
        public override void UpdateEngineBrakePressure(ref float pressureBar, float elapsedClockSeconds)
        {
            MSTSNotch notch = NotchController.GetCurrentNotch();
            if (notch == null)
            {
                pressureBar = (MaxPressureBar() - FullServReductionBar()) * CurrentValue();
            }
            else
            {                
                float x = NotchController.GetNotchFraction();
                switch (notch.Type)
                {
                    case ControllerState.Neutral:
                    case ControllerState.Running:
                    case ControllerState.Lap:
                        break;
                    case ControllerState.FullQuickRelease:
                        pressureBar -= x * QuickReleaseRateBarpS() * elapsedClockSeconds;
                        break;
                    case ControllerState.Release:
                        pressureBar -= x * ReleaseRateBarpS() * elapsedClockSeconds;
                        break;
                    case ControllerState.Apply:
                    case ControllerState.FullServ:
                        IncreasePressure(ref pressureBar, x * (MaxPressureBar() - FullServReductionBar()), ApplyRateBarpS(), elapsedClockSeconds);
                        break;
                    case ControllerState.ManualBraking:
                    case ControllerState.VacContServ:
                    // Continuous service positions for vacuum brakes - allows brake to be adjusted up and down continuously between the ON and OFF position
                        pressureBar = (1 - x) * MaxPressureBar();
                        break;
                    case ControllerState.BrakeNotch:
                        // Notch position for brakes - allows brake to be adjusted up and down continuously between specified notches
                        pressureBar = (1 - x) * MaxPressureBar();
                        break;
                    case ControllerState.Emergency:
                        pressureBar += EmergencyRateBarpS() * elapsedClockSeconds;
                        break;
                    case ControllerState.Dummy:
                        pressureBar = (MaxPressureBar() - FullServReductionBar()) * CurrentValue();
                        break;
                    default:
                        x *= MaxPressureBar() - FullServReductionBar();
                        IncreasePressure(ref pressureBar, x, ApplyRateBarpS(), elapsedClockSeconds);
                        DecreasePressure(ref pressureBar, x, ReleaseRateBarpS(), elapsedClockSeconds);
                        break;
                }
                if (pressureBar > MaxPressureBar())
                    pressureBar = MaxPressureBar();
                if (pressureBar < 0)
                    pressureBar = 0;
            }
        }

        public override void HandleEvent(BrakeControllerEvent evt)
        {
            switch (evt)
            {
                case BrakeControllerEvent.StartIncrease:
                    NotchController.StartIncrease();
                    break;

                case BrakeControllerEvent.StopIncrease:
                    NotchController.StopIncrease();
                    break;

                case BrakeControllerEvent.StartDecrease:
                    NotchController.StartDecrease();
                    break;

                case BrakeControllerEvent.StopDecrease:
                    NotchController.StopDecrease();
                    break;
            }
        }

        public override void HandleEvent(BrakeControllerEvent evt, float? value)
        {
            switch (evt)
            {
                case BrakeControllerEvent.StartIncrease:
                    NotchController.StartIncrease(value);
                    break;

                case BrakeControllerEvent.StartDecrease:
                    NotchController.StartDecrease(value);
                    break;

                case BrakeControllerEvent.SetCurrentPercent:
                    if (value != null)
                    {
                        float newValue = value ?? 0F;
                        NotchController.SetPercent(newValue);
                    }
                    break;

                case BrakeControllerEvent.SetCurrentValue:
                    if (value != null)
                    {
                        float newValue = value ?? 0F;
                        NotchController.SetValue(newValue);
                    }
                    break;

                case BrakeControllerEvent.StartDecreaseToZero:
                    NotchController.StartDecrease(value, true);
                    break;
            }
        }

        public override bool IsValid()
        {
            return NotchController.IsValid();
        }

        public override ControllerState GetState()
        {
            if (EmergencyBrakingPushButton())
                return ControllerState.EBPB;
            else if (TCSEmergencyBraking())
                return ControllerState.TCSEmergency;
            else if (TCSFullServiceBraking())
                return ControllerState.TCSFullServ;
            else if (OverchargeButtonPressed())
                return ControllerState.Overcharge;
            else if (QuickReleaseButtonPressed())
                return ControllerState.FullQuickRelease;
            else if (NotchController != null && NotchController.NotchCount() > 0)
                return NotchController.GetCurrentNotch().Type;
            else
                return ControllerState.Dummy;
        }

        public override float? GetStateFraction()
        {
            if (EmergencyBrakingPushButton() || TCSEmergencyBraking() || TCSFullServiceBraking() || QuickReleaseButtonPressed() || OverchargeButtonPressed())
            {
                return null;
            }
            else if (NotchController != null)
            {
                if (NotchController.NotchCount() == 0)
                    return NotchController.CurrentValue;
                else
                {
                    MSTSNotch notch = NotchController.GetCurrentNotch();

                    if (!notch.Smooth)
                    {
                        if (notch.Type == ControllerState.Dummy)
                            return NotchController.CurrentValue;
                        else
                            return null;
                    }
                    else
                    {
                        return NotchController.GetNotchFraction();
                    }
                }
            }
            else
            {
                return null;
            }
        }

        static void IncreasePressure(ref float pressurePSI, float targetPSI, float ratePSIpS, float elapsedSeconds)
        {
            if (pressurePSI < targetPSI)
            {
                pressurePSI += ratePSIpS * elapsedSeconds;
                if (pressurePSI > targetPSI)
                    pressurePSI = targetPSI;
            }
        }

        static void DecreasePressure(ref float pressurePSI, float targetPSI, float ratePSIpS, float elapsedSeconds)
        {
            if (pressurePSI > targetPSI)
            {
                pressurePSI -= ratePSIpS * elapsedSeconds;
                if (pressurePSI < targetPSI)
                    pressurePSI = targetPSI;
            }
        }
    }
}
