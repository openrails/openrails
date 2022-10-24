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

using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Power supply for dual mode locomotives
    /// </summary>
    public abstract class DualModePowerSupply : LocomotivePowerSupply
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedDualModePowerSupply DmpsHost => LpsHost as ScriptedDualModePowerSupply;
        // TODO : Replace ElectricLocomotive and DieselLocomotive with DualModeLoco
        internal MSTSElectricLocomotive ElectricLocomotive => Locomotive as MSTSElectricLocomotive;
        internal MSTSDieselLocomotive DieselLocomotive => Locomotive as MSTSDieselLocomotive;
        internal Pantographs Pantographs => ElectricLocomotive.Pantographs;
        internal DieselEngines DieselEngines => DieselLocomotive.DieselEngines;
        internal ScriptedCircuitBreaker CircuitBreaker => DmpsHost.CircuitBreaker;
        internal ScriptedTractionCutOffRelay TractionCutOffRelay => DmpsHost.TractionCutOffRelay;
        internal ScriptedVoltageSelector VoltageSelector => DmpsHost.VoltageSelector;
        internal ScriptedPantographSelector PantographSelector => DmpsHost.PantographSelector;
        internal ScriptedPowerLimitationSelector PowerLimitationSelector => DmpsHost.PowerLimitationSelector;

        /// <summary>
        /// Current position of the voltage selector
        /// </summary>
        protected VoltageSelectorPosition VoltageSelectorPosition => VoltageSelector.Position;

        /// <summary>
        /// Current position of the pantograph selector
        /// </summary>
        protected PantographSelectorPosition PantographSelectorPosition => PantographSelector.Position;

        /// <summary>
        /// Current position of the power limitation selector
        /// </summary>
        protected PowerLimitationSelectorPosition PowerLimitationSelectorPosition => PowerLimitationSelector.Position;

        /// <summary>
        /// Current state of the pantograph
        /// </summary>
        protected PantographState PantographState => Pantographs.State;

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        protected CircuitBreakerState CircuitBreakerState => CircuitBreaker.State;

        /// <summary>
        /// Driver's closing order of the circuit breaker
        /// </summary>
        protected bool CircuitBreakerDriverClosingOrder => CircuitBreaker.DriverClosingOrder;

        /// <summary>
        /// Driver's opening order of the circuit breaker
        /// </summary>
        protected bool CircuitBreakerDriverOpeningOrder => CircuitBreaker.DriverOpeningOrder;

        /// <summary>
        /// Driver's closing authorization of the circuit breaker
        /// </summary>
        protected bool CircuitBreakerDriverClosingAuthorization => CircuitBreaker.DriverClosingAuthorization;

        /// <summary>
        /// Voltage of the pantograph
        /// </summary>
        protected float PantographVoltageV
        {
            get => DmpsHost.PantographVoltageV;
            set => DmpsHost.PantographVoltageV = value;
        }

        /// <summary>
        /// Voltage of the filter
        /// </summary>
        protected float FilterVoltageV
        {
            get => DmpsHost.FilterVoltageV;
            set => DmpsHost.FilterVoltageV = value;
        }

        /// <summary>
        /// Line voltage
        /// </summary>
        protected float LineVoltageV => DmpsHost.LineVoltageV;

        /// <summary>
        /// Current state of the diesel engine
        /// </summary>
        protected DieselEngineState DieselEngineState => DieselEngines.State;

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        protected TractionCutOffRelayState TractionCutOffRelayState => TractionCutOffRelay.State;

        /// <summary>
        /// Driver's closing order of the traction cut-off relay
        /// </summary>
        protected bool TractionCutOffRelayDriverClosingOrder => TractionCutOffRelay.DriverClosingOrder;

        /// <summary>
        /// Driver's opening order of the traction cut-off relay
        /// </summary>
        protected bool TractionCutOffRelayDriverOpeningOrder => TractionCutOffRelay.DriverOpeningOrder;

        /// <summary>
        /// Driver's closing authorization of the traction cut-off relay
        /// </summary>
        protected bool TractionCutOffRelayDriverClosingAuthorization => TractionCutOffRelay.DriverClosingAuthorization;

        /// <summary>
        /// Current mode of the power supply
        /// </summary>
        protected PowerSupplyMode PowerSupplyMode
        {
            get => DmpsHost.Mode;
            set => DmpsHost.Mode = value;
        }

        /// <summary>
        /// Sends an event to the circuit breaker
        /// </summary>
        protected void SignalEventToCircuitBreaker(PowerSupplyEvent evt) => DmpsHost.CircuitBreaker.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the traction cut-off relay
        /// </summary>
        protected void SignalEventToTractionCutOffRelay(PowerSupplyEvent evt) => DmpsHost.TractionCutOffRelay.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the voltage selector
        /// </summary>
        protected void SignalEventToVoltageSelector(PowerSupplyEvent evt) => DmpsHost.VoltageSelector.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the voltage selector
        /// </summary>
        protected void SignalEventToVoltageSelector(PowerSupplyEvent evt, int id) => DmpsHost.VoltageSelector.HandleEvent(evt, id);

        /// <summary>
        /// Sends an event to the pantograph selector
        /// </summary>
        protected void SignalEventToPantographSelector(PowerSupplyEvent evt) => DmpsHost.PantographSelector.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the pantograph selector
        /// </summary>
        protected void SignalEventToPantographSelector(PowerSupplyEvent evt, int id) => DmpsHost.PantographSelector.HandleEvent(evt, id);

        /// <summary>
        /// Sends an event to the power limitation selector
        /// </summary>
        protected void SignalEventToPowerLimitationSelector(PowerSupplyEvent evt) => DmpsHost.PowerLimitationSelector.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the power limitation selector
        /// </summary>
        protected void SignalEventToPowerLimitationSelector(PowerSupplyEvent evt, int id) => DmpsHost.PowerLimitationSelector.HandleEvent(evt, id);

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
