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

using System;
using ORTS.Common;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Circuit breaker for electric locomotives
    /// </summary>
    public abstract class CircuitBreaker : AbstractScriptClass
    {
        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public Func<CircuitBreakerState> CurrentState;
        /// <summary>
        /// Current state of the pantograph
        /// </summary>
        public Func<PantographState> CurrentPantographState;
        /// <summary>
        /// Current state of the power supply
        /// </summary>
        public Func<PowerSupplyState> CurrentPowerSupplyState;
        /// <summary>
        /// TCS authorization
        /// </summary>
        public Func<bool> TCSCloseAuthorization;
        /// <summary>
        /// Driver authorization
        /// </summary>
        public Func<bool> DriverCloseAuthorization;
        /// <summary>
        /// Delay before circuit breaker closing
        /// </summary>
        public Func<float> ClosingDelayS;

        /// <summary>
        /// Sets the current state of the circuit breaker
        /// </summary>
        public Action<CircuitBreakerState> SetCurrentState;
        /// <summary>
        /// Sets the driver authorization
        /// </summary>
        public Action<bool> SetDriverCloseAuthorization;

        /// <summary>
        /// Called once at initialization time.
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        public abstract void Update(float elapsedClockSeconds);
        /// <summary>
        /// Called when an event happens (a closing order from the driver for example)
        /// </summary>
        /// <param name="evt">The event happened</param>
        public abstract void HandleEvent(PowerSupplyEvent evt);
    }

    public enum CircuitBreakerState
    {
        [GetParticularString("CircuitBraker", "Open")] Open,
        [GetParticularString("CircuitBraker", "Closing")] Closing,
        [GetParticularString("CircuitBraker", "Closed")] Closed
    }
}
