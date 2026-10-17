// COPYRIGHT 2021 by the Open Rails project.
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
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;

namespace ORTS.Scripting.Api
{
    public abstract class PowerVanPowerSupply : PowerSupply
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedPowerVanPowerSupply PvpsHost => Host as ScriptedPowerVanPowerSupply;
        internal MSTSWagon Wagon => PvpsHost.Wagon;
        internal Pantographs Pantographs => PvpsHost.Pantographs;
        internal DieselEngines DieselEngines => PvpsHost.DieselEngines;
        internal ScriptedCircuitBreaker CircuitBreaker => Pantographs.CircuitBreaker;

        /// <summary>
        /// Current power consumed on the electric power supply line
        /// </summary>
        public float CurrentElectricTrainSupplyPowerW() => PvpsHost.ElectricTrainSupplyPowerW;

        /// <summary>
        /// Systems power on delay when electric train supply has been switched on
        /// </summary>
        public float PowerOnDelayS() => PvpsHost.PowerOnDelayS;
        
        /// <summary>
        /// Sets the current state of the electric train supply
        /// ETS is used by the systems of the cars (such as air conditionning)
        /// </summary>
        protected void SetCurrentElectricTrainSupplyState(PowerSupplyState state)
        {
            PvpsHost.ElectricTrainSupplyState = state;
        }

        /// <summary>
        /// Current state of the pantograph
        /// </summary>
        protected PantographState CurrentPantographState() => Pantographs.CircuitBreaker != null ? Pantographs.State : PantographState.Unavailable;

        /// <summary>
        /// Current state of the pantograph
        /// </summary>
        protected PantographState CurrentPantographState(int id) => (Pantographs.CircuitBreaker != null && Pantographs[id] != null) ? Pantographs[id].State : PantographState.Unavailable;

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        protected CircuitBreakerState CurrentCircuitBreakerState() => CircuitBreaker?.State ?? CircuitBreakerState.Unavailable;

        /// <summary>
        /// Current state of the diesel engines
        /// </summary>
        public DieselEngineState CurrentDieselEnginesState() => DieselEngines?.State ?? DieselEngineState.Unavailable;

        /// <summary>
        /// Current state of the diesel engine
        /// </summary>
        public DieselEngineState CurrentDieselEngineState(int id)
        {
            if (DieselEngines != null && id >= 0 && id < DieselEngines.Count)
            {
                return DieselEngines[id].State;
            }
            else
            {
                return DieselEngineState.Unavailable;
            }
        }

        /// <summary>
        /// Sends an event to all diesel engines
        /// </summary>
        public void SignalEventToDieselEngines(PowerSupplyEvent evt) => DieselEngines?.HandleEvent(evt);

        /// <summary>
        /// Sends an event to one diesel engine
        /// </summary>
        public void SignalEventToDieselEngine(PowerSupplyEvent evt, int id) => DieselEngines?.HandleEvent(evt, id);


        /// <summary>
        /// Sends an event to the circuit breaker
        /// </summary>
        public void SignalEventToCircuitBreaker(PowerSupplyEvent evt) => CircuitBreaker?.HandleEvent(evt);

        /// <summary>
        /// Sends an event to the circuit breaker
        /// </summary>
        public void SignalEventToCircuitBreaker(PowerSupplyEvent evt, int id) => CircuitBreaker?.HandleEvent(evt, id);


        public override void HandleEvent(PowerSupplyEvent evt)
        {
            base.HandleEvent(evt);

            // By default, send the event to every component
            SignalEventToCircuitBreaker(evt);
            SignalEventToDieselEngines(evt);
        }

        public override void HandleEvent(PowerSupplyEvent evt, int id)
        {
            base.HandleEvent(evt, id);

            // By default, send the event to every component
            SignalEventToCircuitBreaker(evt, id);
        }

        public override void HandleEventFromLeadLocomotive(PowerSupplyEvent evt)
        {
            base.HandleEventFromLeadLocomotive(evt);

            // By default, send the event to every component
            SignalEventToCircuitBreaker(evt);
            SignalEventToDieselEngines(evt);
        }

    }
}
