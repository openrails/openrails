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

using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Traction cut-off relay for diesel locomotives
    /// </summary>
    public abstract class TractionCutOffRelay : TractionCutOffSubsystem
    {
        internal ScriptedTractionCutOffRelay TcorHost => Host as ScriptedTractionCutOffRelay;

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        protected TractionCutOffRelayState CurrentState() => TcorHost.State;

        /// <summary>
        /// Sets the current state of the circuit breaker
        /// </summary>
        protected void SetCurrentState(TractionCutOffRelayState state)
        {
            TcorHost.State = state;

            TCSEvent tcsEvent = state == TractionCutOffRelayState.Closed ? TCSEvent.TractionCutOffRelayClosed : TCSEvent.TractionCutOffRelayOpen;
            Locomotive.TrainControlSystem.HandleEvent(tcsEvent);
        }

        /// <summary>
        /// Current state of the circuit breaker
        /// Only available on dual mode locomotives
        /// </summary>
        protected CircuitBreakerState CurrentCircuitBreakerState()
        {
            return PowerSupply.Type == PowerSupplyType.DualMode ? (PowerSupply as ScriptedDualModePowerSupply).CircuitBreaker.State : CircuitBreakerState.Unavailable;
        }
    }

    public enum TractionCutOffRelayState
    {
        [GetString("Unavailable")] Unavailable = -1,
        [GetParticularString("TractionCutOffRelay", "Open")] Open,
        [GetParticularString("TractionCutOffRelay", "Closing")] Closing,
        [GetParticularString("TractionCutOffRelay", "Closed")] Closed
    }
}
