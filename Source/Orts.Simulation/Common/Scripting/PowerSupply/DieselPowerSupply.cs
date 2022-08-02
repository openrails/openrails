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

using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using System;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Power supply for diesel locomotives
    /// </summary>
    public abstract class DieselPowerSupply : LocomotivePowerSupply
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedDieselPowerSupply DpsHost => LpsHost as ScriptedDieselPowerSupply;

        /// <summary>
        /// Current state of the diesel engines
        /// </summary>
        public Func<DieselEngineState> CurrentDieselEnginesState;
        /// <summary>
        /// Current state of the diesel engine
        /// </summary>
        public Func<int, DieselEngineState> CurrentDieselEngineState;
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
        /// Sends an event to all diesel engines
        /// </summary>
        public Action<PowerSupplyEvent> SignalEventToDieselEngines;
        /// <summary>
        /// Sends an event to one diesel engine
        /// </summary>
        public Action<PowerSupplyEvent, int> SignalEventToDieselEngine;
        /// <summary>
        /// Sends an event to the traction cut-off relay
        /// </summary>
        public Action<PowerSupplyEvent> SignalEventToTractionCutOffRelay;

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            base.HandleEvent(evt);

            // By default, send the event to every component
            SignalEventToTractionCutOffRelay(evt);
            SignalEventToDieselEngines(evt);
        }

        public override void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
            base.HandleEventFromLeadLocomotive(evt);

            // By default, send the event to every component
            SignalEventToTractionCutOffRelay(evt);
            SignalEventToDieselEngines(evt);
        }
    }
}
