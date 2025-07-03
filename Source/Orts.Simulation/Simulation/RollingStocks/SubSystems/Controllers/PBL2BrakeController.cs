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
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using ORTS.Scripting.Api;

namespace ORTS.Scripting.Script
{
    public class PBL2BrakeController : BrakeController
    {
        public enum State
        {
            Overcharge,
            OverchargeElimination,
            QuickRelease,
            Release,
            Hold,
            Apply,
            Emergency
        }

        public enum ControllerPosition
        {
            Release = -1,
            Hold = 0,
            Apply = 1,
        }

        public float OverchargeValue { get; private set; }
        public float QuickReleaseValue { get; private set; }
        public float ReleaseValue { get; private set; }
        public float HoldValue { get; private set; }
        public float ApplyValue { get; private set; }
        public float EmergencyValue { get; private set; }

        public int ReleaseNotch { get; private set; }
        public int HoldNotch { get; private set; }
        public int ApplyNotch { get; private set; }
        private Timer ResetTimer { get; set; }

        // brake controller values
        private float OverchargePressureBar = 0.4f;
        private float OverchargeEleminationPressureRateBarpS = 0.0025f;
        private float FirstDepressureBar = 0.5f;
        private float BrakeReleasedDepressureBar = 0.2f;

        private State CurrentState;
        private ControllerPosition CurrentPosition = ControllerPosition.Hold;

        private bool FirstDepression = false;
        private bool Overcharge = false;
        private bool OverchargeElimination = false;
        private bool QuickRelease = false;

        private float RegulatorPressureBar = 0.0f;

        private float? AiBrakeTargetPercent = null;

        public override void Initialize()
        {
            ResetTimer = new Timer(this);
            ResetTimer.Setup(0.1f);

            foreach (MSTSNotch notch in Notches())
            {
                switch (notch.Type)
                {
                    case ControllerState.Release:
                        ReleaseValue = notch.Value;
                        ReleaseNotch = Notches().IndexOf(notch);
                        break;
                    case ControllerState.FullQuickRelease:
                        OverchargeValue = notch.Value;
                        QuickReleaseValue = notch.Value;
                        break;
                    case ControllerState.Lap:
                    case ControllerState.Hold:
                        HoldValue = notch.Value;
                        HoldNotch = Notches().IndexOf(notch);
                        break;
                    case ControllerState.Apply:
                    case ControllerState.GSelfLap:
                    case ControllerState.GSelfLapH:
                        ApplyValue = notch.Value;
                        ApplyNotch = Notches().IndexOf(notch);
                        break;
                    case ControllerState.Emergency:
                        EmergencyValue = notch.Value;
                        break;
                }
            }
        }

        public override void InitializeMoving()
        {
        }

        public override float Update(float elapsedSeconds)
        {
            if (ResetTimer.Triggered)
            {
                CurrentPosition = ControllerPosition.Hold;
                ResetTimer.Stop();
            }

            switch (CurrentPosition)
            {
                case ControllerPosition.Apply:
                    SetCurrentValue(ApplyValue);
                    IntermediateValue = 0.1f;
                    CurrentNotch = ApplyNotch;
                    break;

                case ControllerPosition.Hold:
                    SetCurrentValue(HoldValue);
                    IntermediateValue = 0;
                    CurrentNotch = HoldNotch;
                    break;

                case ControllerPosition.Release:
                    SetCurrentValue(ReleaseValue);
                    IntermediateValue = -0.1f;
                    CurrentNotch = ReleaseNotch;
                    break;
            }

            NeutralModeOn = NeutralModeCommandSwitchOn || EmergencyBrakingPushButton() || TCSEmergencyBraking();

            return CurrentValue();
        }

        public override void UpdatePressure(ref float pressureBar, float elapsedClockSeconds, ref float epPressureBar)
        {
            RegulatorPressureBar = Math.Min(MaxPressureBar(), MainReservoirPressureBar());

            if (AiBrakeTargetPercent != null)
            {
                pressureBar = RegulatorPressureBar - (float)AiBrakeTargetPercent * FullServReductionBar();
                epPressureBar = (float)AiBrakeTargetPercent * MaxPressureBar();
                return;
            }

            if (!FirstDepression && CurrentPosition == ControllerPosition.Apply && pressureBar > Math.Max(RegulatorPressureBar - FirstDepressureBar, 0))
                FirstDepression = true;
            else if (FirstDepression && pressureBar <= Math.Max(RegulatorPressureBar - FirstDepressureBar, 0))
                FirstDepression = false;

            if (CurrentPosition == ControllerPosition.Apply && Overcharge)
                Overcharge = false;
            if (CurrentPosition == ControllerPosition.Apply && QuickRelease)
                QuickRelease = false;

            if (EmergencyBrakingPushButton() || TCSEmergencyBraking())
                CurrentState = State.Emergency;
            else if (
                CurrentPosition == ControllerPosition.Apply && pressureBar > RegulatorPressureBar - FullServReductionBar()
                || FirstDepression && CurrentPosition != ControllerPosition.Release && !QuickRelease && pressureBar > RegulatorPressureBar - FirstDepressureBar
                )
                CurrentState = State.Apply;
            else if (OverchargeElimination && pressureBar > RegulatorPressureBar)
                CurrentState = State.OverchargeElimination;
            else if (Overcharge && pressureBar <= RegulatorPressureBar + OverchargePressureBar)
                CurrentState = State.Overcharge;
            else if (QuickRelease && !NeutralModeOn && pressureBar < RegulatorPressureBar)
                CurrentState = State.QuickRelease;
            else if (
                !NeutralModeOn && (
                    CurrentPosition == ControllerPosition.Release && pressureBar < RegulatorPressureBar
                    || !FirstDepression && pressureBar > RegulatorPressureBar - BrakeReleasedDepressureBar && pressureBar < RegulatorPressureBar
                    || pressureBar < RegulatorPressureBar - FullServReductionBar()
                    )
                )
                CurrentState = State.Release;
            else
                CurrentState = State.Hold;

            switch (CurrentState)
            {
                case State.Overcharge:
                    SetUpdateValue(-1);

                    pressureBar += QuickReleaseRateBarpS() * elapsedClockSeconds;
                    epPressureBar -= QuickReleaseRateBarpS() * elapsedClockSeconds;

                    if (pressureBar > MaxPressureBar() + OverchargePressureBar)
                        pressureBar = MaxPressureBar() + OverchargePressureBar;
                    break;

                case State.OverchargeElimination:
                    SetUpdateValue(-1);

                    pressureBar -= OverchargeEleminationPressureRateBarpS * elapsedClockSeconds;

                    if (pressureBar < MaxPressureBar())
                        pressureBar = MaxPressureBar();
                    break;

                case State.QuickRelease:
                    SetUpdateValue(-1);

                    pressureBar += QuickReleaseRateBarpS() * elapsedClockSeconds;
                    epPressureBar -= QuickReleaseRateBarpS() * elapsedClockSeconds;

                    if (pressureBar > RegulatorPressureBar)
                        pressureBar = RegulatorPressureBar;
                    break;

                case State.Release:
                    SetUpdateValue(-1);

                    pressureBar += ReleaseRateBarpS() * elapsedClockSeconds;
                    epPressureBar -= ReleaseRateBarpS() * elapsedClockSeconds;

                    if (pressureBar > RegulatorPressureBar)
                        pressureBar = RegulatorPressureBar;
                    break;

                case State.Hold:
                    SetUpdateValue(0);
                    break;

                case State.Apply:
                    SetUpdateValue(1);

                    pressureBar -= ApplyRateBarpS() * elapsedClockSeconds;
                    epPressureBar += ApplyRateBarpS() * elapsedClockSeconds;

                    if (pressureBar < Math.Max(RegulatorPressureBar - FullServReductionBar(), 0.0f))
                        pressureBar = Math.Max(RegulatorPressureBar - FullServReductionBar(), 0.0f);
                    break;

                case State.Emergency:
                    SetUpdateValue(1);

                    pressureBar -= EmergencyRateBarpS() * elapsedClockSeconds;

                    if (pressureBar < 0)
                        pressureBar = 0;
                    break;
            }

            if (epPressureBar > MaxPressureBar())
                epPressureBar = MaxPressureBar();
            if (epPressureBar < 0)
                epPressureBar = 0;

            if (QuickRelease && pressureBar == RegulatorPressureBar)
                QuickRelease = false;
        }

        public override void UpdateEngineBrakePressure(ref float pressureBar, float elapsedClockSeconds)
        {
            switch (CurrentState)
            {
                case State.Release:
                    SetCurrentValue(ReleaseValue);
                    SetUpdateValue(-1);
                    pressureBar -= ReleaseRateBarpS() * elapsedClockSeconds;
                    break;
                
                case State.Apply:
                    SetCurrentValue(ApplyValue);
                    SetUpdateValue(0);
                    pressureBar += ApplyRateBarpS() * elapsedClockSeconds;
                    break;
                
                case State.Emergency:
                    SetCurrentValue(EmergencyValue);
                    SetUpdateValue(1);
                    pressureBar += EmergencyRateBarpS() * elapsedClockSeconds;
                    break;
            }

            if (pressureBar > MaxPressureBar())
                pressureBar = MaxPressureBar();
            if (pressureBar < 0)
                pressureBar = 0;
        }

        public override void HandleEvent(BrakeControllerEvent evt)
        {
            switch (evt)
            {
                case BrakeControllerEvent.StartIncrease:
                    CurrentPosition = ControllerPosition.Apply;
                    QuickRelease = false;
                    AiBrakeTargetPercent = null;
                    break;

                case BrakeControllerEvent.StopIncrease:
                    CurrentPosition = ControllerPosition.Hold;
                    AiBrakeTargetPercent = null;
                    break;

                case BrakeControllerEvent.StartDecrease:
                    CurrentPosition = ControllerPosition.Release;
                    QuickRelease = false;
                    AiBrakeTargetPercent = null;
                    break;

                case BrakeControllerEvent.StopDecrease:
                    CurrentPosition = ControllerPosition.Hold;
                    AiBrakeTargetPercent = null;
                    break;
            }
        }

        public override void HandleEvent(BrakeControllerEvent evt, float? value)
        {
            switch (evt)
            {
                case BrakeControllerEvent.StartIncrease:
                    CurrentPosition = ControllerPosition.Apply;
                    QuickRelease = false;
                    AiBrakeTargetPercent = null;
                    break;

                case BrakeControllerEvent.StartDecrease:
                    CurrentPosition = ControllerPosition.Release;
                    AiBrakeTargetPercent = null;
                    break;

                case BrakeControllerEvent.StartDecreaseToZero:
                    QuickRelease = true;
                    AiBrakeTargetPercent = null;
                    break;

                case BrakeControllerEvent.SetCurrentPercent:
                    if (value != null)
                    {
                        AiBrakeTargetPercent = value;
                    }
                    break;

                case BrakeControllerEvent.SetCurrentValue:
                    if (value != null)
                    {
                        float newValue = (float)value;
                        SetValue(newValue);
                    }
                    AiBrakeTargetPercent = null;
                    break;
            }
        }

        public override bool IsValid()
        {
            return true;
        }

        public override ControllerState GetState()
        {
            switch (CurrentState)
            {
                case State.Overcharge:
                    return ControllerState.Overcharge;

                case State.OverchargeElimination:
                    return ControllerState.Overcharge;

                case State.QuickRelease:
                    return ControllerState.FullQuickRelease;

                case State.Release:
                    return ControllerState.Release;

                case State.Hold:
                    return ControllerState.Hold;

                case State.Apply:
                    return ControllerState.Apply;

                case State.Emergency:
                    if (EmergencyBrakingPushButton())
                        return ControllerState.EBPB;
                    else if (TCSEmergencyBraking())
                        return ControllerState.TCSEmergency;
                    else if (TCSFullServiceBraking())
                        return ControllerState.TCSFullServ;
                    else
                        return ControllerState.Emergency;

                default:
                    return ControllerState.Dummy;
            }
        }

        public override float? GetStateFraction()
        {
            return null;
        }

        private void SetValue(float v)
        {
            ResetTimer.Start();

            if (v > 0)
            {
                CurrentPosition = ControllerPosition.Apply;
            }
            else if (v < 0)
            {
                CurrentPosition = ControllerPosition.Release;
            }
            else
            {
                CurrentPosition = ControllerPosition.Hold;
            }
        }
    }
}
