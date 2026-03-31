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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;

namespace Orts.Simulation.RollingStocks.SubSystems.Controllers
{
    public class MultiPositionController
    {
        MSTSLocomotive Locomotive;
        Simulator Simulator;

        public List<Position> PositionsList = new List<Position>();

        public bool StateChanged = false;

        public ControllerPosition controllerPosition = new ControllerPosition();
        public ControllerBinding controllerBinding = new ControllerBinding();
        protected float elapsedSecondsFromLastChange = 0;
        protected bool checkNeutral = false;
        protected bool noKeyPressed = true;
        protected ControllerPosition currentPosition = ControllerPosition.Undefined;
        protected bool emergencyBrake = false;
        protected bool previousDriveModeWasAddPower = false;
        protected bool isBraking = false;
        protected bool needPowerUpAfterBrake = false;
        public bool CanControlTrainBrake = false;
        protected bool movedForward = false;
        protected bool movedAft = false;
        protected bool haveCruiseControl = false;
        public int ControllerId = 0;
        public bool MouseInputActive = false;

        protected const float MPCFullRangeIncreaseTimeS = 6.0f;
        protected const float DynamicBrakeIncreaseStepPerSecond = 50f;
        protected const float DynamicBrakeDecreaseStepPerSecond = 25f;
        protected const float ThrottleIncreaseStepPerSecond = 15f;
        protected const float ThrottleDecreaseStepPerSecond = 15f;

        public MultiPositionController(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
            Simulator = Locomotive.Simulator;
            controllerPosition = ControllerPosition.Neutral;
        }

        public MultiPositionController(MultiPositionController other, MSTSLocomotive locomotive)
        {
            Simulator = locomotive.Simulator;
            Locomotive = locomotive;

            PositionsList = other.PositionsList;
            controllerBinding = other.controllerBinding;
            ControllerId = other.ControllerId;
            CanControlTrainBrake = other.CanControlTrainBrake;
        }
  

        public void Save(BinaryWriter outf)
        {
            outf.Write(checkNeutral);
            outf.Write((int)controllerPosition);
            outf.Write((int)currentPosition);
            outf.Write(elapsedSecondsFromLastChange);
            outf.Write(emergencyBrake);
            outf.Write(isBraking);
            outf.Write(noKeyPressed);
            outf.Write(previousDriveModeWasAddPower);
            outf.Write(StateChanged);
        }

        public void Restore(BinaryReader inf)
        {
            checkNeutral = inf.ReadBoolean();
            controllerPosition = (ControllerPosition)inf.ReadInt32();
            currentPosition = (ControllerPosition)inf.ReadInt32();
            elapsedSecondsFromLastChange = inf.ReadSingle();
            emergencyBrake = inf.ReadBoolean();
            isBraking = inf.ReadBoolean();
            noKeyPressed = inf.ReadBoolean();
            previousDriveModeWasAddPower = inf.ReadBoolean();
            StateChanged = inf.ReadBoolean();
        }
        public void Parse(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new [] {
                new STFReader.TokenProcessor("positions", () => {
                    stf.MustMatch("(");
                    stf.ParseBlock(new [] {
                        new STFReader.TokenProcessor("position", ()=>{
                            stf.MustMatch("(");
                            string positionType = stf.ReadString();
                            string positionFlag = stf.ReadString();
                            string positionName = stf.ReadString();
                            stf.SkipRestOfBlock();
                            PositionsList.Add(new Position(positionType, positionFlag, positionName));
                        }),
                    });
                }),
                new STFReader.TokenProcessor("controllerbinding", () => Enum.TryParse(stf.ReadStringBlock(null), true, out controllerBinding)),
                new STFReader.TokenProcessor("controllerid", () => ControllerId = stf.ReadIntBlock(0)),
                new STFReader.TokenProcessor("cancontroltrainbrake", () => CanControlTrainBrake = stf.ReadBoolBlock(false)),
            });
        }
        public void Initialize()
        {
            if (Locomotive.CruiseControl != null)
                haveCruiseControl = true;
            foreach (Position pair in PositionsList)
            {
                if (pair.Flag == ControllerPositionFlag.Default)
                {
                    currentPosition = pair.Type;
                    break;
                }
            }
        }
        public void Update(float elapsedClockSeconds)
        {
            if (!Locomotive.IsPlayerTrain) return;

            if (haveCruiseControl)
                if (Locomotive.CruiseControl.DynamicBrakePriority) return;

            ReloadPositions();
            if (Locomotive.AbsSpeedMpS > 0)
            {
                if (emergencyBrake)
                {
                    Locomotive.TrainBrakeController.EmergencyBrakingPushButton = true;
                    return;
                }
            }
            else
            {
                emergencyBrake = false;
            }
            elapsedSecondsFromLastChange += elapsedClockSeconds;
            if (checkNeutral)
            {
                // Check every 200 ms if state of MPC has changed
                if (elapsedSecondsFromLastChange > 0.2f)
                {
                    CheckNeutralPosition();
                    checkNeutral = false;
                }
            }
            bool ccAutoMode = false;
            if (haveCruiseControl)
            {
                if (Locomotive.CruiseControl.SpeedRegMode == CruiseControl.SpeedRegulatorMode.Auto)
                {
                    ccAutoMode = true;
                }

            }
            if (!haveCruiseControl || !ccAutoMode)
            {
                if (controllerPosition == ControllerPosition.ThrottleIncrease)
                {
                    if (Locomotive.DynamicBrakePercent < 1)
                    {
                        if (Locomotive.ThrottlePercent < 100)
                        {
                            float step = (haveCruiseControl ? ThrottleIncreaseStepPerSecond : 100 / MPCFullRangeIncreaseTimeS);
                            step *= elapsedClockSeconds;
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + step);
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleIncreaseFast)
                {
                    if (Locomotive.DynamicBrakePercent < 1)
                    {
                        if (Locomotive.ThrottlePercent < 100)
                        {
                            float step = (haveCruiseControl ? ThrottleIncreaseStepPerSecond * 2 : 200 / MPCFullRangeIncreaseTimeS);
                            step *= elapsedClockSeconds;
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + step);
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleDecrease)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        float step = (haveCruiseControl ? 100 / ThrottleDecreaseStepPerSecond : 100 / MPCFullRangeIncreaseTimeS);
                        step *= elapsedClockSeconds;
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - step);
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleDecreaseFast)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        float step = (haveCruiseControl ? 100 / ThrottleDecreaseStepPerSecond * 2 : 200 / MPCFullRangeIncreaseTimeS);
                        step *= elapsedClockSeconds;
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - step);
                    }
                }
                if (controllerPosition == ControllerPosition.Neutral || controllerPosition == ControllerPosition.DynamicBrakeHold)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Apply)
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Neutral)
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.ThrottlePercent < 2 && controllerBinding == ControllerBinding.Throttle)
                    {
                        if (Locomotive.ThrottlePercent != 0)
                            Locomotive.SetThrottlePercent(0);
                    }
                    if (Locomotive.ThrottlePercent > 1 && controllerBinding == ControllerBinding.Throttle)
                    {
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - 1f);
                    }
                    if (Locomotive.ThrottlePercent > 100 && controllerBinding == ControllerBinding.Throttle)
                    {
                        Locomotive.ThrottlePercent = 100;
                    }

                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncrease)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Apply)
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Neutral)
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.DynamicBrakePercent == -1) Locomotive.SetDynamicBrakePercent(0);
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + DynamicBrakeIncreaseStepPerSecond * elapsedClockSeconds);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseFast)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Apply)
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Neutral)
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.DynamicBrakePercent == -1) Locomotive.SetDynamicBrakePercent(0);
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + DynamicBrakeIncreaseStepPerSecond * elapsedClockSeconds);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeDecrease)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - DynamicBrakeDecreaseStepPerSecond * elapsedClockSeconds);
                    }
                }
                if (controllerPosition == ControllerPosition.Drive || controllerPosition == ControllerPosition.ThrottleHold)
                {
                    if (Locomotive.DynamicBrakePercent < 2)
                    {
                        Locomotive.SetDynamicBrakePercent(-1);
                    }
                    if (Locomotive.DynamicBrakePercent > 1)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - DynamicBrakeDecreaseStepPerSecond * elapsedClockSeconds);
                    }
                }
                if (controllerPosition == ControllerPosition.TrainBrakeIncrease)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState != ORTS.Scripting.Api.ControllerState.Apply)
                        {
                            Locomotive.StartTrainBrakeIncrease(null);
                        }
                        else
                        {
                            Locomotive.StopTrainBrakeIncrease();
                        }
                    }
                }
                else if (controllerPosition == ControllerPosition.Drive)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState != ORTS.Scripting.Api.ControllerState.Release)
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        else
                            Locomotive.StopTrainBrakeDecrease();
                    }
                }
                if (controllerPosition == ControllerPosition.TrainBrakeDecrease)
                {
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState != ORTS.Scripting.Api.ControllerState.Release)
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        else
                            Locomotive.StopTrainBrakeDecrease();
                    }
                }
                if (controllerPosition == ControllerPosition.EmergencyBrake)
                {
                    EmergencyBrakes();
                    emergencyBrake = true;
                }
                if (controllerPosition == ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecrease)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - DynamicBrakeDecreaseStepPerSecond * elapsedClockSeconds);
                        if (Locomotive.DynamicBrakePercent < 2)
                            Locomotive.SetDynamicBrakePercent(-1);
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent < 100)
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + ThrottleIncreaseStepPerSecond * elapsedClockSeconds);
                        if (Locomotive.ThrottlePercent > 100)
                            Locomotive.SetThrottlePercent(100);
                    }
                }
                if (controllerPosition == ControllerPosition.ThrottleIncreaseOrDynamicBrakeDecreaseFast)
                {
                    if (Locomotive.DynamicBrakePercent > 0)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - DynamicBrakeDecreaseStepPerSecond * 2 * elapsedClockSeconds);
                        if (Locomotive.DynamicBrakePercent < 2)
                            Locomotive.SetDynamicBrakePercent(-1);
                    }
                    else
                    {
                        if (Locomotive.ThrottlePercent < 100)
                            Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent + ThrottleIncreaseStepPerSecond * 2 * elapsedClockSeconds);
                        if (Locomotive.ThrottlePercent > 100)
                            Locomotive.SetThrottlePercent(100);
                    }
                }

                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseOrThrottleDecrease)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - ThrottleDecreaseStepPerSecond * elapsedClockSeconds);
                        if (Locomotive.ThrottlePercent < 0)
                            Locomotive.ThrottlePercent = 0;
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakePercent < 100)
                        {
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + DynamicBrakeIncreaseStepPerSecond * elapsedClockSeconds);
                        }
                        if (Locomotive.DynamicBrakePercent > 100)
                            Locomotive.SetDynamicBrakePercent(100);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseOrThrottleDecreaseFast)
                {
                    if (Locomotive.ThrottlePercent > 0)
                    {
                        Locomotive.SetThrottlePercent(Locomotive.ThrottlePercent - ThrottleDecreaseStepPerSecond * 2 * elapsedClockSeconds);
                        if (Locomotive.ThrottlePercent < 0)
                            Locomotive.ThrottlePercent = 0;
                    }
                    else
                    {
                        if (Locomotive.DynamicBrakePercent < 100)
                        {
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + DynamicBrakeIncreaseStepPerSecond * 2 * elapsedClockSeconds);
                        }
                        if (Locomotive.DynamicBrakePercent > 100)
                            Locomotive.SetDynamicBrakePercent(100);
                    }
                }
                if (controllerPosition == ControllerPosition.SelectedSpeedIncrease)
                {
                    if (Locomotive.CruiseControl.ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero &&
                        Locomotive.CruiseControl.SelectedMaxAccelerationPercent == 0 && Locomotive.CruiseControl.DisableCruiseControlOnThrottleAndZeroForceAndZeroSpeed &&
                           Locomotive.ThrottleController.CurrentValue == 0 && Locomotive.DynamicBrakeController.CurrentValue == 0)
                    {
                        Locomotive.CruiseControl.SpeedRegMode = CruiseControl.SpeedRegulatorMode.Auto;
                        Locomotive.CruiseControl.SpeedRegulatorSelectedSpeedIncrease();
                    }
                }
            }
            else if (haveCruiseControl && ccAutoMode)
            {
                if (Locomotive.CruiseControl.CruiseControlLogic == CruiseControl.ControllerCruiseControlLogic.SpeedOnly)
                {
                    if (controllerPosition == ControllerPosition.ThrottleIncrease)
                    {
                        if (!Locomotive.CruiseControl.ContinuousSpeedIncreasing && movedForward) return;
                        movedForward = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Math.Max(Locomotive.CruiseControl.MinimumSpeedForCCEffectMpS,
                            Locomotive.CruiseControl.SelectedSpeedMpS + Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS);
                        if (Locomotive.CruiseControl.SelectedSpeedMpS > Locomotive.MaxSpeedMpS) Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
                    }
                    if (controllerPosition == ControllerPosition.ThrottleIncreaseFast)
                    {
                        if (!Locomotive.CruiseControl.ContinuousSpeedIncreasing && movedForward) return;
                        movedForward = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Math.Max(Locomotive.CruiseControl.MinimumSpeedForCCEffectMpS,
                            Locomotive.CruiseControl.SelectedSpeedMpS + Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS * 2);
                        if (Locomotive.CruiseControl.SelectedSpeedMpS > Locomotive.MaxSpeedMpS) Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.MaxSpeedMpS;
                    }
                    if (controllerPosition == ControllerPosition.ThrottleDecrease)
                    {
                        if (!Locomotive.CruiseControl.ContinuousSpeedDecreasing && movedAft) return;
                        movedAft = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.SelectedSpeedMpS - Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS;
                        if (Locomotive.CruiseControl.SelectedSpeedMpS < 0) Locomotive.CruiseControl.SelectedSpeedMpS = 0;
                        if (Locomotive.CruiseControl.MinimumSpeedForCCEffectMpS > 0 && Locomotive.CruiseControl.SelectedSpeedMpS < Locomotive.CruiseControl.MinimumSpeedForCCEffectMpS)
                            Locomotive.CruiseControl.SelectedSpeedMpS = 0;
                    }
                    if (controllerPosition == ControllerPosition.ThrottleDecreaseFast)
                    {
                        if (!Locomotive.CruiseControl.ContinuousSpeedDecreasing && movedAft) return;
                        movedAft = true;
                        Locomotive.CruiseControl.SelectedSpeedMpS = Locomotive.CruiseControl.SelectedSpeedMpS - Locomotive.CruiseControl.SpeedRegulatorNominalSpeedStepMpS * 2;
                        if (Locomotive.CruiseControl.SelectedSpeedMpS < 0) Locomotive.CruiseControl.SelectedSpeedMpS = 0;
                        if (Locomotive.CruiseControl.MinimumSpeedForCCEffectMpS > 0 && Locomotive.CruiseControl.SelectedSpeedMpS < Locomotive.CruiseControl.MinimumSpeedForCCEffectMpS)
                            Locomotive.CruiseControl.SelectedSpeedMpS = 0;
                    }
                    return;
                }
                if (controllerPosition == ControllerPosition.ThrottleIncrease)
                {
                    isBraking = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Start;
                    previousDriveModeWasAddPower = true;
                }
                if (controllerPosition == ControllerPosition.Neutral)
                {
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                }
                if (controllerPosition == ControllerPosition.Drive)
                {
                    bool applyPower = true;
                    if (isBraking && needPowerUpAfterBrake)
                    {
                        if (Locomotive.DynamicBrakePercent < 2)
                        {
                            Locomotive.SetDynamicBrakePercent(-1);
                        }
                        if (Locomotive.DynamicBrakePercent > 1)
                        {
                            Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent - 1);
                        }
                        if (CanControlTrainBrake)
                        {
                            if (Locomotive.TrainBrakeController.GetStatus().ToLower() != "release")
                            {
                                Locomotive.StartTrainBrakeDecrease(null);
                            }
                            else
                                Locomotive.StopTrainBrakeDecrease();
                        }
                        applyPower = false;
                    }
                    if (applyPower) Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.On;
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncrease)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Apply)
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Neutral)
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 1f);
                    }
                }
                if (controllerPosition == ControllerPosition.DynamicBrakeIncreaseFast)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Apply)
                        {
                            Locomotive.StartTrainBrakeDecrease(null);
                        }
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState == ORTS.Scripting.Api.ControllerState.Neutral)
                        {
                            Locomotive.StopTrainBrakeDecrease();
                        }
                    }
                    if (Locomotive.ThrottlePercent < 1 && Locomotive.DynamicBrakePercent < 100)
                    {
                        Locomotive.SetDynamicBrakePercent(Locomotive.DynamicBrakePercent + 2f);
                    }
                }
                if (controllerPosition == ControllerPosition.TrainBrakeIncrease)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    if (CanControlTrainBrake)
                    {
                        if (Locomotive.TrainBrakeController.TrainBrakeControllerState != ORTS.Scripting.Api.ControllerState.Apply)
                        {
                            String test = Locomotive.TrainBrakeController.GetStatus().ToLower();
                            Locomotive.StartTrainBrakeIncrease(null);
                        }
                        else
                        {
                            Locomotive.StopTrainBrakeIncrease();
                        }
                    }
                }
                if (controllerPosition == ControllerPosition.EmergencyBrake)
                {
                    isBraking = true;
                    previousDriveModeWasAddPower = false;
                    Locomotive.CruiseControl.SpeedSelMode = CruiseControl.SpeedSelectorMode.Neutral;
                    EmergencyBrakes();
                    emergencyBrake = true;
                }
                if (controllerPosition == ControllerPosition.SelectedSpeedIncrease)
                {
                    Locomotive.CruiseControl.SpeedRegulatorSelectedSpeedIncrease();
                }
                if (controllerPosition == ControllerPosition.SelectedSpeedDecrease)
                {
                    Locomotive.CruiseControl.SpeedRegulatorSelectedSpeedDecrease();
                }
                if (controllerPosition == ControllerPosition.SelectSpeedZero)
                {
                    Locomotive.CruiseControl.SetSpeed(0);
                }
            }
        }

        private bool messageDisplayed = false;
        public void DoMovement(Movement movement)
        {
            if (movement == Movement.Aft) movedForward = false;
            if (movement == Movement.Forward) movedAft = false;
            if (movement == Movement.Neutral) movedForward = movedAft = false;
            messageDisplayed = false;
            if (currentPosition == ControllerPosition.Undefined)
            {
                foreach (Position pair in PositionsList)
                {
                    if (pair.Flag == ControllerPositionFlag.Default)
                    {
                        currentPosition = pair.Type;
                        break;
                    }
                }
            }
            if (movement == Movement.Forward)
            {
                noKeyPressed = false;
                checkNeutral = false;
                bool isFirst = true;
                ControllerPosition previous = ControllerPosition.Undefined;
                foreach (Position pair in PositionsList)
                {
                    if (pair.Type == currentPosition)
                    {
                        if (isFirst)
                            break;
                        currentPosition = previous;
                        Locomotive.SignalEvent(Common.Event.MPCChangePosition);
                        break;
                    }
                    isFirst = false;
                    previous = pair.Type;
                }
            }
            if (movement == Movement.Aft)
            {
                noKeyPressed = false;
                checkNeutral = false;
                bool selectNext = false;
                foreach (Position pair in PositionsList)
                {
                    if (selectNext)
                    {
                        currentPosition = pair.Type;
                        Locomotive.SignalEvent(Common.Event.MPCChangePosition);
                        break;
                    }
                    if (pair.Type == currentPosition) selectNext = true;
                }
            }
            if (movement == Movement.Neutral)
            {
                noKeyPressed = true;
                foreach (Position pair in PositionsList)
                {
                    if (pair.Type == currentPosition)
                    {
                        if (pair.Flag == ControllerPositionFlag.SpringLoadedBackwards || pair.Flag == ControllerPositionFlag.SpringLoadedForwards)
                        {
                            checkNeutral = true;
                            elapsedSecondsFromLastChange = 0;
                        }
                        if (pair.Flag == ControllerPositionFlag.SpringLoadedBackwardsImmediately || pair.Flag == ControllerPositionFlag.SpringLoadedForwardsImmediately)
                        {
                            if (!MouseInputActive)
                            {
                                CheckNeutralPosition();
                                ReloadPositions();
                            }
                        }
                    }
                }
            }

        }

        protected void ReloadPositions()
        {
            if (noKeyPressed)
            {
                foreach (Position pair in PositionsList)
                {
                    if (pair.Type == currentPosition)
                    {
                        if (pair.Flag == ControllerPositionFlag.CCNeedIncreaseAfterAnyBrake)
                        {
                            needPowerUpAfterBrake = true;
                        }
                        if (pair.Flag == ControllerPositionFlag.SpringLoadedForwards || pair.Flag == ControllerPositionFlag.SpringLoadedForwards)
                        {
                            if (elapsedSecondsFromLastChange > 0.2f)
                            {
                                elapsedSecondsFromLastChange = 0;
                                checkNeutral = true;
                            }
                        }
                    }
                }
            }
            controllerPosition = currentPosition;
            if (!messageDisplayed)
            {
                string msg = GetPositionName(currentPosition);
                if (!string.IsNullOrEmpty(msg))
                    Simulator.Confirmer.Information(msg);
            }
            messageDisplayed = true;
        }

        protected void CheckNeutralPosition()
        {
            bool setNext = false;
            ControllerPosition previous = ControllerPosition.Undefined;
            foreach (Position pair in PositionsList)
            {
                if (setNext)
                {
                    currentPosition = pair.Type;
                    Locomotive.SignalEvent(Common.Event.MPCChangePosition);
                    break;
                }
                if (pair.Type == currentPosition)
                {
                    if (pair.Flag == ControllerPositionFlag.SpringLoadedBackwards || pair.Flag == ControllerPositionFlag.SpringLoadedBackwardsImmediately)
                    {
                        setNext = true;
                    }
                    if (pair.Flag == ControllerPositionFlag.SpringLoadedForwards || pair.Flag == ControllerPositionFlag.SpringLoadedForwardsImmediately)
                    {
                        currentPosition = previous;
                        Locomotive.SignalEvent(Common.Event.MPCChangePosition);
                        break;
                    }
                }
                previous = pair.Type;
            }
        }

        protected string GetPositionName(ControllerPosition type)
        {
            string ret = "";
            foreach (Position p in PositionsList)
            {
                if (p.Type == type)
                    ret = p.Name;
            }
            return ret;
        }

        protected void EmergencyBrakes()
        {
            Locomotive.SetThrottlePercent(0);
            Locomotive.SetDynamicBrakePercent(100);
            Locomotive.TrainBrakeController.EmergencyBrakingPushButton = true;
        }
        public enum Movement
        {
            Forward,
            Neutral,
            Aft
        };

        public float GetDataOf(CabViewControl cvc)
        {
            float data = 0;
            switch (controllerPosition)
            {
                case ControllerPosition.ThrottleIncrease:
                    data = 0;
                    break;
                case ControllerPosition.Drive:
                case ControllerPosition.ThrottleHold:
                    data = 1;
                    break;
                case ControllerPosition.Neutral:
                    data = 2;
                    break;
                case ControllerPosition.DynamicBrakeIncrease:
                    data = 3;
                    break;
                case ControllerPosition.TrainBrakeIncrease:
                    data = 4;
                    break;
                case ControllerPosition.EmergencyBrake:
                case ControllerPosition.DynamicBrakeIncreaseFast:
                    data = 5;
                    break;
                case ControllerPosition.ThrottleIncreaseFast:
                    data = 6;
                    break;
                case ControllerPosition.ThrottleDecrease:
                    data = 7;
                    break;
                case ControllerPosition.ThrottleDecreaseFast:
                    data = 8;
                    break;
                case ControllerPosition.SelectedSpeedIncrease:
                    data = 9;
                    break;
                case ControllerPosition.SelectedSpeedDecrease:
                    data = 10;
                    break;
                case ControllerPosition.SelectSpeedZero:
                    data = 11;
                    break;
            }
            return data;
        }

        public struct Position
        {
            public ControllerPosition Type;
            public ControllerPositionFlag Flag;
            public string Name;
            public Position(string positionType, string positionFlag, string name)
            {
                Enum.TryParse(positionType, true, out Type);
                if (!Enum.TryParse(positionFlag, true, out Flag))
                {
                    switch(positionFlag.ToLower())
                    {
                        case "cruisecontrol.needincreaseafteranybrake":
                            Flag = ControllerPositionFlag.CCNeedIncreaseAfterAnyBrake;
                            break;

                    }
                }
                Name = name;
            }
        }
    }
    public enum ControllerPosition
    {
        Undefined,
        Neutral,
        Drive,
        ThrottleIncrease,
        ThrottleDecrease,
        ThrottleIncreaseFast,
        ThrottleDecreaseFast,
        DynamicBrakeIncrease, DynamicBrakeDecrease,
        DynamicBrakeIncreaseFast,
        TrainBrakeIncrease,
        TrainBrakeDecrease,
        EmergencyBrake,
        ThrottleHold,
        DynamicBrakeHold,
        ThrottleIncreaseOrDynamicBrakeDecreaseFast,
        ThrottleIncreaseOrDynamicBrakeDecrease,
        DynamicBrakeIncreaseOrThrottleDecreaseFast,
        DynamicBrakeIncreaseOrThrottleDecrease,
        KeepCurrent,
        SelectedSpeedIncrease,
        SelectedSpeedDecrease,
        SelectSpeedZero
    };

    public enum ControllerPositionFlag
    {
        Default,
        Stable,
        SpringLoadedForwards,
        SpringLoadedForwardsImmediately,
        SpringLoadedBackwards,
        SpringLoadedBackwardsImmediately,
        CCNeedIncreaseAfterAnyBrake
    }

    public enum ControllerBinding
    {
        Throttle,
        SelectedSpeed
    }
}
