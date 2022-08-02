//// COPYRIGHT 2014 by the Open Rails project.
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

using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using System;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Power supply for dual mode locomotives
    /// </summary>
    public abstract class DualModePowerSupply : LocomotivePowerSupply
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedDualModePowerSupply DmpsHost => LpsHost as ScriptedDualModePowerSupply;

        /// <summary>
        /// Current position of the voltage selector
        /// </summary>
        protected VoltageSelectorPosition VoltageSelectorPosition => DmpsHost.VoltageSelector.Position;

        /// <summary>
        /// Current position of the pantograph selector
        /// </summary>
        protected PantographSelectorPosition PantographSelectorPosition => DmpsHost.PantographSelector.Position;
        /// <summary>
        /// Current position of the power limitation selector
        /// </summary>
        protected PowerLimitationSelectorPosition PowerLimitationSelectorPosition => DmpsHost.PowerLimitationSelector.Position;

        /// <summary>
        /// Current state of the pantograph
        /// </summary>
        public PantographState CurrentPantographState() => DmpsHost.Pantographs.State;

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public CircuitBreakerState CurrentCircuitBreakerState() => DmpsHost.CircuitBreaker.State;

        /// <summary>
        /// Driver's closing order of the circuit breaker
        /// </summary>
        public bool CircuitBreakerDriverClosingOrder() => DmpsHost.CircuitBreaker.DriverClosingOrder;

        /// <summary>
        /// Driver's opening order of the circuit breaker
        /// </summary>
        public bool CircuitBreakerDriverOpeningOrder() => DmpsHost.CircuitBreaker.DriverOpeningOrder;

        /// <summary>
        /// Driver's closing authorization of the circuit breaker
        /// </summary>
        public bool CircuitBreakerDriverClosingAuthorization() => DmpsHost.CircuitBreaker.DriverClosingAuthorization;

        /// <summary>
        /// Voltage of the pantograph
        /// </summary>
        public float PantographVoltageV
        {
            get => DmpsHost.PantographVoltageV;
            set => DmpsHost.PantographVoltageV = value;
        }

        /// <summary>
        /// Voltage of the filter
        /// </summary>
        public float FilterVoltageV
        {
            get => DmpsHost.FilterVoltageV;
            set => DmpsHost.FilterVoltageV = value;
        }

        /// <summary>
        /// Line voltage
        /// </summary>
        public float LineVoltageV => DmpsHost.LineVoltageV;

        /// <summary>
        /// Current state of the diesel engine
        /// </summary>
        public DieselEngineState DieselEngineState => DmpsHost.DieselEngines.State;

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public TractionCutOffRelayState TractionCutOffRelayState => DmpsHost.TractionCutOffRelay.State;

        /// <summary>
        /// Driver's closing order of the traction cut-off relay
        /// </summary>
        public bool TractionCutOffRelayDriverClosingOrder => DmpsHost.TractionCutOffRelay.DriverClosingOrder;

        /// <summary>
        /// Driver's opening order of the traction cut-off relay
        /// </summary>
        public bool TractionCutOffRelayDriverOpeningOrder => DmpsHost.TractionCutOffRelay.DriverOpeningOrder;

        /// <summary>
        /// Driver's closing authorization of the traction cut-off relay
        /// </summary>
        public bool TractionCutOffRelayDriverClosingAuthorization => DmpsHost.TractionCutOffRelay.DriverClosingAuthorization;

        /// <summary>
        /// Current mode of the power supply
        /// </summary>
        public PowerSupplyMode PowerSupplyMode
        {
            get => DmpsHost.Mode;
            set => DmpsHost.Mode = value;
        }

        /// <summary>
        /// Sends an event to the circuit breaker
        /// </summary>
        public void SignalEventToCircuitBreaker(PowerSupplyEvent evt) => DmpsHost.CircuitBreaker.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the traction cut-off relay
        /// </summary>
        public void SignalEventToTractionCutOffRelay(PowerSupplyEvent evt) => DmpsHost.TractionCutOffRelay.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the voltage selector
        /// </summary>
        public void SignalEventToVoltageSelector(PowerSupplyEvent evt) => DmpsHost.VoltageSelector.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the voltage selector
        /// </summary>
        public void SignalEventToVoltageSelector(PowerSupplyEvent evt, int id) => DmpsHost.VoltageSelector.HandleEvent(evt, id);

        /// <summary>
        /// Sends an event to the pantograph selector
        /// </summary>
        public void SignalEventToPantographSelector(PowerSupplyEvent evt) => DmpsHost.PantographSelector.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the pantograph selector
        /// </summary>
        public void SignalEventToPantographSelector(PowerSupplyEvent evt, int id) => DmpsHost.PantographSelector.HandleEvent(evt, id);

        /// <summary>
        /// Sends an event to the power limitation selector
        /// </summary>
        public void SignalEventToPowerLimitationSelector(PowerSupplyEvent evt) => DmpsHost.PowerLimitationSelector.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the power limitation selector
        /// </summary>
        public void SignalEventToPowerLimitationSelector(PowerSupplyEvent evt, int id) => DmpsHost.PowerLimitationSelector.HandleEvent(evt, id);

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            base.HandleEvent(evt);

            // By default, send the event to every component
            SignalEventToTractionCutOffRelay(evt);
            SignalEventToVoltageSelector(evt);
            SignalEventToPantographSelector(evt);
            SignalEventToPowerLimitationSelector(evt);
        }

        public override void HandleEvent(PowerSupplyEvent evt, int id)
        {
            base.HandleEvent(evt, id);

            // By default, send the event to every component
            SignalEventToVoltageSelector(evt, id);
            SignalEventToPantographSelector(evt, id);
            SignalEventToPowerLimitationSelector(evt, id);
        }
    }

    public enum PowerSupplyMode
    {
        None,
        Diesel,
        Pantograph,
    }
}
