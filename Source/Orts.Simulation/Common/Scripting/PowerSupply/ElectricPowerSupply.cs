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

using System;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Power supply for electric locomotives
    /// </summary>
    public abstract class ElectricPowerSupply : LocomotivePowerSupply
    {
        /// <summary>
        /// Current state of the pantograph
        /// </summary>
        public Func<PantographState> CurrentPantographState;
        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public Func<CircuitBreakerState> CurrentCircuitBreakerState;
        /// <summary>
        /// Driver's closing order of the circuit breaker
        /// </summary>
        public Func<bool> CircuitBreakerDriverClosingOrder;
        /// <summary>
        /// Driver's opening order of the circuit breaker
        /// </summary>
        public Func<bool> CircuitBreakerDriverOpeningOrder;
        /// <summary>
        /// Driver's closing authorization of the circuit breaker
        /// </summary>
        public Func<bool> CircuitBreakerDriverClosingAuthorization;
        /// <summary>
        /// Voltage of the pantograph
        /// </summary>
        public Func<float> PantographVoltageV;
        /// <summary>
        /// Voltage of the filter
        /// </summary>
        public Func<float> FilterVoltageV;
        /// <summary>
        /// Line voltage
        /// </summary>
        public Func<float> LineVoltageV;

        /// <summary>
        /// Sets the voltage of the pantograph
        /// </summary>
        public Action<float> SetPantographVoltageV;
        /// <summary>
        /// Sets the voltage of the filter
        /// </summary>
        public Action<float> SetFilterVoltageV;
        /// <summary>
        /// Sends an event to the circuit breaker
        /// </summary>
        public Action<PowerSupplyEvent> SignalEventToCircuitBreaker;

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            base.HandleEvent(evt);

            // By default, send the event to every component
            SignalEventToCircuitBreaker(evt);
        }

        public override void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
            base.HandleEventFromLeadLocomotive(evt);

            // By default, send the event to every component
            SignalEventToCircuitBreaker(evt);
        }
    }
}
