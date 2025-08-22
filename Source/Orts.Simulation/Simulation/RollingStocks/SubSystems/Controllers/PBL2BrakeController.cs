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
using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
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
        private float BrakeReleasedDepressureBar = 0.2f;
        private float EpActivationThresholdBar = 0.15f;

        private State CurrentState;
        private ControllerPosition CurrentPosition = ControllerPosition.Hold;

        private bool FirstDepression = false;
        private bool Overcharge = false;
        private bool OverchargeElimination = false;
        private bool QuickRelease = false;

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
                    case ControllerState.Overcharge:
                        OverchargeValue = notch.Value;
                        break;
                    case ControllerState.FullQuickRelease:
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

            NeutralModeOn = NeutralModeCommandSwitchOn || EmergencyBrakingPushButton() || TCSEmergencyBraking() || !IsAuxiliaryPowerSupplyOn();

            return CurrentValue();
        }

        public override void UpdatePressure(ref float pressureBar, float elapsedClockSeconds, ref float epPressureBar)
        {
            if (AiBrakeTargetPercent != null)
            {
                pressureBar = MaxPressureBar() - (float)AiBrakeTargetPercent / 100 * FullServReductionBar();
                epPressureBar = (float)AiBrakeTargetPercent / 100;
                return;
            }

            if (!FirstDepression && CurrentPosition == ControllerPosition.Apply && pressureBar > Math.Max(MaxPressureBar() - MinReductionBar(), 0))
                FirstDepression = true;
            else if (FirstDepression && pressureBar <= Math.Max(MaxPressureBar() - MinReductionBar(), 0))
                FirstDepression = false;

            if (QuickReleaseButtonPressed())
                QuickRelease = true;
            if (OverchargeButtonPressed())
            {
                Overcharge = true;
                OverchargeElimination = false;
            }
            else if (Overcharge)
            {
                Overcharge = false;
                OverchargeElimination = true;
            }
            if (CurrentPosition == ControllerPosition.Apply && Overcharge)
                Overcharge = false;
            if (CurrentPosition == ControllerPosition.Apply && QuickRelease)
                QuickRelease = false;

            if (EmergencyBrakingPushButton() || TCSEmergencyBraking())
                CurrentState = State.Emergency;
            else if (TCSFullServiceBraking() && pressureBar > MaxPressureBar() - FullServReductionBar())
                CurrentState = State.Apply;
            else if (
                CurrentPosition == ControllerPosition.Apply && pressureBar > MaxPressureBar() - FullServReductionBar()
                || FirstDepression && CurrentPosition != ControllerPosition.Release && !QuickRelease && pressureBar > MaxPressureBar() - MinReductionBar()
                )
                CurrentState = State.Apply;
            else if (OverchargeElimination && pressureBar > MaxPressureBar())
                CurrentState = State.OverchargeElimination;
            else if (Overcharge && pressureBar <= Math.Min(MaxOverchargePressureBar(), MainReservoirPressureBar()))
                CurrentState = State.Overcharge;
            else if (QuickRelease && !NeutralModeOn && pressureBar < Math.Min(MaxPressureBar(), MainReservoirPressureBar()))
                CurrentState = State.QuickRelease;
            else if (
                !NeutralModeOn && (
                    CurrentPosition == ControllerPosition.Release && pressureBar < Math.Min(MaxPressureBar(), MainReservoirPressureBar())
                    || !FirstDepression && pressureBar > MaxPressureBar() - BrakeReleasedDepressureBar && pressureBar < Math.Min(MaxPressureBar(), MainReservoirPressureBar())
                    || pressureBar < MaxPressureBar() - FullServReductionBar()
                    )
                )
                CurrentState = State.Release;
            else
                CurrentState = State.Hold;

            switch (CurrentState)
            {
                case State.Overcharge:
                    {
                        SetUpdateValue(-1);

                        float dp = QuickReleaseRateBarpS() * elapsedClockSeconds;
                        if (pressureBar + dp > MaxOverchargePressureBar())
                            dp = MaxOverchargePressureBar() - pressureBar;
                        if (pressureBar + dp > MainReservoirPressureBar())
                            dp = Math.Max(MainReservoirPressureBar() - pressureBar, 0);
                        pressureBar += dp;

                        break;
                    }
                case State.OverchargeElimination:
                    {
                        SetUpdateValue(-1);

                        float dp = OverchargeEliminationRateBarpS() * elapsedClockSeconds;
                        if (pressureBar - dp < MaxPressureBar())
                            dp = Math.Max(pressureBar - MaxPressureBar(), 0);
                        pressureBar -= dp;

                        break;
                    }
                case State.QuickRelease:
                    {
                        SetUpdateValue(-1);

                        float dp = QuickReleaseRateBarpS() * elapsedClockSeconds;
                        if (pressureBar + dp > MaxPressureBar())
                            dp = MaxPressureBar() - pressureBar;
                        if (pressureBar + dp > MainReservoirPressureBar())
                            dp = Math.Max(MainReservoirPressureBar() - pressureBar, 0);
                        pressureBar += dp;

                        break;
                    }
                case State.Release:
                    {
                        SetUpdateValue(-1);

                        float dp = ReleaseRateBarpS() * elapsedClockSeconds;
                        if (pressureBar + dp > MaxPressureBar())
                            dp = MaxPressureBar() - pressureBar;
                        if (pressureBar + dp > MainReservoirPressureBar())
                            dp = Math.Max(MainReservoirPressureBar() - pressureBar, 0);
                        pressureBar += dp;

                        break;
                    }
                case State.Hold:
                    SetUpdateValue(0);
                    break;

                case State.Apply:
                    {
                        SetUpdateValue(1);

                        float dp = ApplyRateBarpS() * elapsedClockSeconds;
                        if (pressureBar - dp < MaxPressureBar() - FullServReductionBar())
                            dp = Math.Max(pressureBar - (MaxPressureBar() - FullServReductionBar()), 0);
                        pressureBar -= dp;

                        break;
                    }
                case State.Emergency:
                    {
                        SetUpdateValue(1);

                        float dp =  EmergencyRateBarpS() * elapsedClockSeconds;
                        if (pressureBar - dp < MaxPressureBar() - FullServReductionBar())
                            dp = Math.Max(pressureBar - (MaxPressureBar() - FullServReductionBar()), 0);
                        pressureBar -= dp;
                        break;
                    }
            }

            if (BrakePipePressureBar() > Math.Max(MaxPressureBar() - FullServReductionBar(), pressureBar) + EpActivationThresholdBar)
                epPressureBar = 1; // EP application wire
            else if (!NeutralModeOn && BrakePipePressureBar() >= MaxPressureBar() - FullServReductionBar() && BrakePipePressureBar() < Math.Min(MaxPressureBar(), pressureBar) - EpActivationThresholdBar)
                epPressureBar = 0; // EP release wire
            else
                epPressureBar = -1;

            if (QuickRelease && pressureBar >= Math.Min(MaxPressureBar(), MainReservoirPressureBar()))
                QuickRelease = false;

            if (OverchargeElimination && pressureBar <= MaxPressureBar())
                OverchargeElimination = false;
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
            if (NeutralModeOn)
            {
                switch (CurrentState)
                {
                    case State.Emergency:
                        return ControllerState.Emergency;
                    default:
                        return ControllerState.Lap;
                }
            }
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
                    if (TCSFullServiceBraking())
                        return ControllerState.TCSFullServ;
                    else
                        return ControllerState.Apply;

                case State.Emergency:
                    if (EmergencyBrakingPushButton())
                        return ControllerState.EBPB;
                    else if (TCSEmergencyBraking())
                        return ControllerState.TCSEmergency;
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
