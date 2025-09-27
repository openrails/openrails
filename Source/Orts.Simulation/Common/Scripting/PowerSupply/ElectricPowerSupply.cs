//// COPYRIGHT 2021 by the Open Rails project.
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
    /// Power supply for electric locomotives
    /// </summary>
    public abstract class ElectricPowerSupply : LocomotivePowerSupply
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedElectricPowerSupply EpsHost => LpsHost as ScriptedElectricPowerSupply;
        internal MSTSElectricLocomotive ElectricLocomotive => Locomotive as MSTSElectricLocomotive;
        internal Pantographs Pantographs => ElectricLocomotive.Pantographs;
        internal ScriptedCircuitBreaker CircuitBreaker => EpsHost.CircuitBreaker;
        internal ScriptedVoltageSelector VoltageSelector => EpsHost.VoltageSelector;
        internal ScriptedPantographSelector PantographSelector => EpsHost.PantographSelector;
        internal ScriptedPowerLimitationSelector PowerLimitationSelector => EpsHost.PowerLimitationSelector;

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
        protected PantographState CurrentPantographState() => Pantographs.State;

        /// <summary>
        /// Current state of the pantograph
        /// </summary>
        protected PantographState CurrentPantographState(int id) => Pantographs[id]?.State ?? PantographState.Unavailable;

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        protected CircuitBreakerState CurrentCircuitBreakerState() => CircuitBreaker.State;

        /// <summary>
        /// Driver's closing order of the circuit breaker
        /// </summary>
        protected bool CircuitBreakerDriverClosingOrder() => CircuitBreaker.DriverClosingOrder;

        /// <summary>
        /// Driver's opening order of the circuit breaker
        /// </summary>
        protected bool CircuitBreakerDriverOpeningOrder() => CircuitBreaker.DriverOpeningOrder;

        /// <summary>
        /// Driver's closing authorization of the circuit breaker
        /// </summary>
        protected bool CircuitBreakerDriverClosingAuthorization() => CircuitBreaker.DriverClosingAuthorization;

        /// <summary>
        /// Closing authorization of the circuit breaker
        /// </summary>
        protected bool CircuitBreakerClosingAuthorization() => CircuitBreaker.ClosingAuthorization;

        /// <summary>
        /// Voltage of the pantograph
        /// </summary>
        protected float PantographVoltageV() => EpsHost.PantographVoltageV;

        /// <summary>
        /// AC voltage of the pantograph
        /// </summary>
        protected float PantographVoltageVAC
        {
            get => EpsHost.PantographVoltageVAC;
            set => EpsHost.PantographVoltageVAC = value;
        }

        /// <summary>
        /// DC voltage of the pantograph
        /// </summary>
        protected float PantographVoltageVDC
        {
            get => EpsHost.PantographVoltageVDC;
            set => EpsHost.PantographVoltageVDC = value;
        }

        /// <summary>
        /// Voltage of the filter
        /// </summary>
        protected float FilterVoltageV() => EpsHost.FilterVoltageV;

        /// <summary>
        /// Line voltage
        /// </summary>
        protected float LineVoltageV() => EpsHost.LineVoltageV;

        /// <summary>
        /// Sets the voltage of the pantograph
        /// </summary>
        protected void SetPantographVoltageV(float voltage) => EpsHost.PantographVoltageV = voltage;

        /// <summary>
        /// Sets the voltage of the filter
        /// </summary>
        protected void SetFilterVoltageV(float voltage) => EpsHost.FilterVoltageV = voltage;

        public override PowerSupplyState GetPowerStatus()
        {
            var status = base.GetPowerStatus();
            PowerSupplyState electricStatus;
            switch (CurrentPantographState())
            {
                case PantographState.Up:
                    switch (CurrentCircuitBreakerState())
                    {
                        case CircuitBreakerState.Closed:
                            electricStatus = PowerSupplyState.PowerOn;
                            break;
                        default:
                            electricStatus = PowerSupplyState.PowerOnOngoing;
                            break;
                    }
                    break;
                case PantographState.Raising:
                    electricStatus = PowerSupplyState.PowerOnOngoing;
                    break;
                default:
                    electricStatus = PowerSupplyState.PowerOff;
                    break;
            }
            if (status == electricStatus) return status;
            return PowerSupplyState.PowerOnOngoing;
        }

        /// <summary>
        /// Sends an event to the circuit breaker
        /// </summary>
        public void SignalEventToCircuitBreaker(PowerSupplyEvent evt) => CircuitBreaker.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the circuit breaker
        /// </summary>
        public void SignalEventToCircuitBreaker(PowerSupplyEvent evt, int id) => CircuitBreaker.HandleEvent(evt, id);

        /// <summary>
        /// Sends an event to the voltage selector
        /// </summary>
        public void SignalEventToVoltageSelector(PowerSupplyEvent evt) => VoltageSelector.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the voltage selector
        /// </summary>
        public void SignalEventToVoltageSelector(PowerSupplyEvent evt, int id) => VoltageSelector.HandleEvent(evt, id);

        /// <summary>
        /// Sends an event to the pantograph selector
        /// </summary>
        public void SignalEventToPantographSelector(PowerSupplyEvent evt) => PantographSelector.HandleEvent(evt);
        
        /// <summary>
        /// Sends an event to the pantograph selector
        /// </summary>
        public void SignalEventToPantographSelector(PowerSupplyEvent evt, int id) => PantographSelector.HandleEvent(evt, id);

        /// <summary>
        /// Sends an event to the power limitation selector
        /// </summary>
        public void SignalEventToPowerLimitationSelector(PowerSupplyEvent evt) => PowerLimitationSelector.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the power limitation selector
        /// </summary>
        public void SignalEventToPowerLimitationSelector(PowerSupplyEvent evt, int id) => PowerLimitationSelector.HandleEvent(evt, id);

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            base.HandleEvent(evt);

            // By default, send the event to every component
            SignalEventToCircuitBreaker(evt);
            SignalEventToVoltageSelector(evt);
            SignalEventToPantographSelector(evt);
            SignalEventToPowerLimitationSelector(evt);
        }

        public override void HandleEvent(PowerSupplyEvent evt, int id)
        {
            base.HandleEvent(evt, id);

            // By default, send the event to every component
            SignalEventToCircuitBreaker(evt, id);
            SignalEventToVoltageSelector(evt, id);
            SignalEventToPantographSelector(evt, id);
            SignalEventToPowerLimitationSelector(evt, id);
        }

        public override void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
            base.HandleEventFromLeadLocomotive(evt);

            // By default, send the event to every component
            SignalEventToCircuitBreaker(evt);
        }
    }
}
