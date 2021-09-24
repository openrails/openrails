//// COPYRIGHT 2020 by the Open Rails project.
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
    /// <summary>
    /// Traction cut-off relay for diesel locomotives
    /// </summary>
    public abstract class TractionCutOffRelay : TractionCutOffSubsystem
    {
        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public Func<TractionCutOffRelayState> CurrentState;

        /// <summary>
        /// Sets the current state of the circuit breaker
        /// </summary>
        public Action<TractionCutOffRelayState> SetCurrentState;

        /// <summary>
        /// Current state of the circuit breaker
        /// Only available on dual mode locomotives
        /// </summary>
        public Func<CircuitBreakerState> CurrentCircuitBreakerState;
    }

    public enum TractionCutOffRelayState
    {
        [GetString("Unavailable")] Unavailable = -1,
        [GetParticularString("TractionCutOffRelay", "Open")] Open,
        [GetParticularString("TractionCutOffRelay", "Closing")] Closing,
        [GetParticularString("TractionCutOffRelay", "Closed")] Closed
    }
}
