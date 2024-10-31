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

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Power supply for dual mode locomotives
    /// </summary>
    public abstract class DualModePowerSupply : ElectricPowerSupply
    {
        /// <summary>
        /// Current state of the diesel engine
        /// </summary>
        public Func<DieselEngineState> CurrentDieselEngineState;
        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public Func<TractionCutOffRelayState> CurrentTractionCutOffRelayState;
        /// <summary>
        /// Driver's closing order of the traction cut-off relay
        /// </summary>
        public Func<bool> TractionCutOffRelayDriverClosingOrder;
        /// <summary>
        /// Driver's opening order of the traction cut-off relay
        /// </summary>
        public Func<bool> TractionCutOffRelayDriverOpeningOrder;
        /// <summary>
        /// Driver's closing authorization of the traction cut-off relay
        /// </summary>
        public Func<bool> TractionCutOffRelayDriverClosingAuthorization;
        /// <summary>
        /// Current mode of the power supply
        /// </summary>
        public Func<PowerSupplyMode> CurrentPowerSupplyMode;

        /// <summary>
        /// Sets current mode of the power supply
        /// </summary>
        public Action<PowerSupplyMode> SetCurrentPowerSupplyMode;
        /// <summary>
        /// Sends an event to the traction cut-off relay
        /// </summary>
        public Action<PowerSupplyEvent> SignalEventToTractionCutOffRelay;

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            base.HandleEvent(evt);

            // By default, send the event to every component
            SignalEventToTractionCutOffRelay(evt);
        }
    }

    public enum PowerSupplyMode
    {
        Diesel,
        Pantograph,
    }
}
