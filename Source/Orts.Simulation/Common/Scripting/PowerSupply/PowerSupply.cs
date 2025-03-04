﻿// COPYRIGHT 2021 by the Open Rails project.
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

using ORTS.Common;
using System;

namespace ORTS.Scripting.Api
{
    public abstract class PowerSupply : AbstractTrainScriptClass
    {
        /// <summary>
        /// Current state of the electric train supply
        /// ETS is used by the systems of the cars (such as air conditionning)
        /// </summary>
        public Func<PowerSupplyState> CurrentElectricTrainSupplyState;
        /// <summary>
        /// Current state of the low voltage power supply
        /// Low voltage power is used by safety systems (such as TCS) or lights
        /// </summary>
        public Func<PowerSupplyState> CurrentLowVoltagePowerSupplyState;
        /// <summary>
        /// Current state of the battery
        /// </summary>
        public Func<PowerSupplyState> CurrentBatteryState;
        /// <summary>
        /// True if the battery is switched on
        /// </summary>
        public Func<bool> BatterySwitchOn;

        /// <summary>
        /// Sets the current state of the low voltage power supply
        /// Low voltage power is used by safety systems (such as TCS) or lights
        /// </summary>
        public Action<PowerSupplyState> SetCurrentLowVoltagePowerSupplyState;
        /// <summary>
        /// Sets the current state of the battery
        /// </summary>
        public Action<PowerSupplyState> SetCurrentBatteryState;
        /// <summary>
        /// Sends an event to the battery switch
        /// </summary>
        public Action<PowerSupplyEvent> SignalEventToBatterySwitch;
        /// <summary>
        /// Sends an event to all pantographs
        /// </summary>
        public Action<PowerSupplyEvent> SignalEventToPantographs;
        /// <summary>
        /// Sends an event to one pantograph
        /// </summary>
        public Action<PowerSupplyEvent, int> SignalEventToPantograph;

        /// <summary>
        /// Called once at initialization time.
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Called once at initialization time if the train speed is greater than 0.
        /// Set as virtual to keep compatibility with scripts not providing this method.
        /// </summary>
        public virtual void InitializeMoving() { }
        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        public abstract void Update(float elapsedClockSeconds);
        /// <summary>
        /// Called when the driver (or the train's systems) want something to happen on the power supply system
        /// </summary>
        /// <param name="evt">The event</param>
        public virtual void HandleEvent(PowerSupplyEvent evt)
        {
            // By default, send the event to every component
            SignalEventToBatterySwitch(evt);
        }

        public virtual void HandleEvent(PowerSupplyEvent evt, int id)
        {
            // By default, send the event to every component
            SignalEventToPantograph(evt, id);
        }

        public virtual void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
            // By default, send the event to every component
            SignalEventToPantographs(evt);
            SignalEventToBatterySwitch(evt);
        }

        public virtual void HandleEventFromLeadLocomotive(PowerSupplyEvent evt, int id)
        {
            // By default, send the event to every component
            SignalEventToPantograph(evt, id);
        }
    }

    public enum PowerSupplyEvent
    {
        QuickPowerOn,
        QuickPowerOff,
        TogglePlayerEngine,
        ToggleHelperEngine,
        CloseBatterySwitch,
        OpenBatterySwitch,
        CloseBatterySwitchButtonPressed,
        CloseBatterySwitchButtonReleased,
        OpenBatterySwitchButtonPressed,
        OpenBatterySwitchButtonReleased,
        TurnOnMasterKey,
        TurnOffMasterKey,
        RaisePantograph,
        LowerPantograph,
        CloseCircuitBreaker,
        OpenCircuitBreaker,
        CloseCircuitBreakerButtonPressed,
        CloseCircuitBreakerButtonReleased,
        OpenCircuitBreakerButtonPressed,
        OpenCircuitBreakerButtonReleased,
        GiveCircuitBreakerClosingAuthorization,
        RemoveCircuitBreakerClosingAuthorization,
        StartEngine,
        StopEngine,
        CloseTractionCutOffRelay,
        OpenTractionCutOffRelay,
        CloseTractionCutOffRelayButtonPressed,
        CloseTractionCutOffRelayButtonReleased,
        OpenTractionCutOffRelayButtonPressed,
        OpenTractionCutOffRelayButtonReleased,
        GiveTractionCutOffRelayClosingAuthorization,
        RemoveTractionCutOffRelayClosingAuthorization,
        ServiceRetentionButtonPressed,
        ServiceRetentionButtonReleased,
        ServiceRetentionCancellationButtonPressed,
        ServiceRetentionCancellationButtonReleased,
        SwitchOnElectricTrainSupply,
        SwitchOffElectricTrainSupply,
        StallEngine,
    }

    public enum PowerSupplyType
    {
        [GetParticularString("PowerSupply", "Steam")] Steam,
        [GetParticularString("PowerSupply", "DieselMechanical")] DieselMechanical,
        [GetParticularString("PowerSupply", "DieselHydraulic")] DieselHydraulic,
        [GetParticularString("PowerSupply", "DieselElectric")] DieselElectric,
        [GetParticularString("PowerSupply", "Electric")] Electric,
        [GetParticularString("PowerSupply", "DualMode")] DualMode,
        [GetParticularString("PowerSupply", "ControlCar")] ControlCar,
    }

    public enum PowerSupplyState
    {
        [GetString("Unavailable")] Unavailable = -1,
        [GetParticularString("PowerSupply", "Off")] PowerOff,
        [GetParticularString("PowerSupply", "On ongoing")] PowerOnOngoing,
        [GetParticularString("PowerSupply", "On")] PowerOn
    }

    public enum PantographState
    {
        [GetString("Unavailable")] Unavailable = -1,
        [GetParticularString("Pantograph", "Down")] Down,
        [GetParticularString("Pantograph", "Lowering")] Lowering,
        [GetParticularString("Pantograph", "Raising")] Raising,
        [GetParticularString("Pantograph", "Up")] Up
    }

    public enum DieselEngineState
    {
        [GetString("Unavailable")] Unavailable = -1,
        [GetParticularString("Engine", "Stopped")] Stopped,
        [GetParticularString("Engine", "Stopping")] Stopping,
        [GetParticularString("Engine", "Starting")] Starting,
        [GetParticularString("Engine", "Running")] Running
    }
}
