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

using System;
using System.IO;
using ORTS.Scripting.Api;

namespace ORTS
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
        MSTSNotchController NotchController;

		public MSTSBrakeController()
        {
        }

        public override void Initialize()
        {
            NotchController = new MSTSNotchController(Notches());
            NotchController.SetValue(CurrentValue());
            NotchController.IntermediateValue = IntermediateValue();
            NotchController.MinimumValue = MinimumValue();
            NotchController.MaximumValue = MaximumValue();
            NotchController.StepSize = StepSize();
        }

        public override float Update(float elapsedSeconds)
        {
            float value = NotchController.Update(elapsedSeconds);
            SetCurrentValue(value);
            SetUpdateValue(NotchController.UpdateValue);
            return value;
        }

        public override void UpdatePressure(ref float pressureBar, float elapsedClockSeconds, ref float epPressureBar)
        {
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
                if (notch == null)
                {
                    pressureBar = MaxPressureBar() - FullServReductionBar() * CurrentValue();
                }
                else
                {
                    float x = NotchController.GetNotchFraction();
                    switch (notch.Type)
                    {
                        case MSTSNotchType.Release:
                            pressureBar += x * ReleaseRateBarpS() * elapsedClockSeconds;
                            epPressureBar -= x * ReleaseRateBarpS() * elapsedClockSeconds;
                            break;
                        case MSTSNotchType.FullQuickRelease:
                            pressureBar += x * QuickReleaseRateBarpS() * elapsedClockSeconds;
                            epPressureBar -= x * QuickReleaseRateBarpS() * elapsedClockSeconds;
                            break;
                        case MSTSNotchType.Running:
                            if (notch.Smooth)
                                x = .1f * (1 - x);
                            pressureBar += x * ReleaseRateBarpS() * elapsedClockSeconds;
                            break;
                        case MSTSNotchType.Apply:
                        case MSTSNotchType.FullServ:
                            pressureBar -= x * ApplyRateBarpS() * elapsedClockSeconds;
                            break;
                        case MSTSNotchType.EPApply:
                            pressureBar += x * ReleaseRateBarpS() * elapsedClockSeconds;
                            if (notch.Smooth)
                                IncreasePressure(ref epPressureBar, x * FullServReductionBar(), ApplyRateBarpS(), elapsedClockSeconds);
                            else
                                epPressureBar += x * ApplyRateBarpS() * elapsedClockSeconds;
                            break;
                        case MSTSNotchType.GSelfLapH:
                        case MSTSNotchType.Suppression:
                        case MSTSNotchType.ContServ:
                        case MSTSNotchType.GSelfLap:
                            x = MaxPressureBar() - MinReductionBar() * (1 - x) - FullServReductionBar() * x;
                            DecreasePressure(ref pressureBar, x, ApplyRateBarpS(), elapsedClockSeconds);
                            if (GraduatedRelease())
                                IncreasePressure(ref pressureBar, x, ReleaseRateBarpS(), elapsedClockSeconds);
                            break;
                        case MSTSNotchType.Emergency:
                            pressureBar -= EmergencyRateBarpS() * elapsedClockSeconds;
                            break;
                        case MSTSNotchType.Dummy:
                            x *= MaxPressureBar() - FullServReductionBar();
                            IncreasePressure(ref pressureBar, x, ReleaseRateBarpS(), elapsedClockSeconds);
                            DecreasePressure(ref pressureBar, x, ApplyRateBarpS(), elapsedClockSeconds);
                            break;
                    }
                }
            }

            if (pressureBar > MaxPressureBar())
                pressureBar = MaxPressureBar();
            if (pressureBar < 0)
                pressureBar = 0;
            if (epPressureBar > MaxPressureBar())
                epPressureBar = MaxPressureBar();
            if (epPressureBar < 0)
                epPressureBar = 0;
        }

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
                    case MSTSNotchType.Release:
                        pressureBar -= x * ReleaseRateBarpS() * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.Running:
                        pressureBar -= ReleaseRateBarpS() * elapsedClockSeconds;
                        break;
#if false
                    case MSTSNotchType.Apply:
                    case MSTSNotchType.FullServ:
                        pressurePSI += x * ApplyRatePSIpS * elapsedClockSeconds;
                        break;
#endif
                    case MSTSNotchType.Emergency:
                        pressureBar += EmergencyRateBarpS() * elapsedClockSeconds;
                        break;
                    case MSTSNotchType.Dummy:
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

                case BrakeControllerEvent.SetRDPercent:
                    if (value != null)
                    {
                        float newValue = value ?? 0F;
                        NotchController.SetRDPercent(newValue);
                    }
                    break;

                case BrakeControllerEvent.SetCurrentValue:
                    if (value != null)
                    {
                        float newValue = value ?? 0F;
                        NotchController.SetValue(newValue);
                    }
                    break;
            }
        }

        public override bool IsValid()
        {
            return NotchController.IsValid();
        }

        public override string GetStatus()
        {
            if (EmergencyBrakingPushButton())
                return "Emergency Braking Push Button";
            else if (TCSEmergencyBraking())
                return "TCS Emergency Braking";
            else if (TCSFullServiceBraking())
                return "TCS Full Service Braking";
            else
                return NotchController.GetStatus();
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
