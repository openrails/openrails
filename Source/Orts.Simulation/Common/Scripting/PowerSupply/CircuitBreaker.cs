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

using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using Orts.Simulation.RollingStocks;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Circuit breaker for electric and dual mode locomotives
    /// </summary>
    public abstract class CircuitBreaker : TractionCutOffSubsystem
    {
        internal ScriptedCircuitBreaker CbHost => Host as ScriptedCircuitBreaker;
        internal ScriptedElectricPowerSupply ElectricPowerSupply => CbHost.LocomotivePowerSupply as ScriptedElectricPowerSupply;
        internal ScriptedDualModePowerSupply DualModePowerSupply => CbHost.LocomotivePowerSupply as ScriptedDualModePowerSupply;

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        protected CircuitBreakerState CurrentState() => CbHost.State;

        /// <summary>
        /// TCS' circuit breaker closing order
        /// </summary>
        protected bool TCSClosingOrder() => CbHost.TCSClosingOrder;

        /// <summary>
        /// TCS' circuit breaker opening order
        /// </summary>
        protected bool TCSOpeningOrder() => CbHost.TCSOpeningOrder;

        /// <summary>
        /// Sets the current state of the circuit breaker
        /// </summary>
        protected void SetCurrentState(CircuitBreakerState state)
        {
            CbHost.State = state;

            TCSEvent tcsEvent = state == CircuitBreakerState.Closed ? TCSEvent.CircuitBreakerClosed : TCSEvent.CircuitBreakerOpen;
            Locomotive.TrainControlSystem.HandleEvent(tcsEvent);
        }

        /// <summary>
        /// Current pantograph voltage
        /// </summary>
        protected float PantographVoltageV => ElectricPowerSupply?.PantographVoltageV ?? (DualModePowerSupply?.PantographVoltageV ?? 0f);
    }

    public enum CircuitBreakerState
    {
        [GetString("Unavailable")] Unavailable = -1,
        [GetParticularString("CircuitBreaker", "Open")] Open,
        [GetParticularString("CircuitBreaker", "Closing")] Closing,
        [GetParticularString("CircuitBreaker", "Closed")] Closed
    }
}
