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
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Power supply for diesel locomotives
    /// </summary>
    public abstract class DieselPowerSupply : LocomotivePowerSupply
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedDieselPowerSupply DpsHost => LpsHost as ScriptedDieselPowerSupply;
        internal MSTSDieselLocomotive DieselLocomotive => Locomotive as MSTSDieselLocomotive;
        internal DieselEngines DieselEngines => DieselLocomotive.DieselEngines;
        internal ScriptedTractionCutOffRelay TractionCutOffRelay => DpsHost.TractionCutOffRelay;

        /// <summary>
        /// Current state of the diesel engines
        /// </summary>
        public DieselEngineState CurrentDieselEnginesState() => DieselEngines.State;

        /// <summary>
        /// Current state of the diesel engine
        /// </summary>
        public DieselEngineState CurrentDieselEngineState(int id)
        {
            if (id >= 0 && id < DieselEngines.Count)
            {
                return DieselEngines[id].State;
            }
            else
            {
                return DieselEngineState.Unavailable;
            }
        }
        protected float DieselEngineOutputPowerW => DieselEngines.MaxOutputPowerW;

        public float DieselEngineMinRpmForElectricTrainSupply => DpsHost.DieselEngineMinRpmForElectricTrainSupply;

        public float DieselEngineMinRpm
        {
            get
            {
                return DpsHost.DieselEngineMinRpm;
            }
            set
            {
                DpsHost.DieselEngineMinRpm = value;
            }
        }

        /// <summary>
        /// Current state of the circuit breaker
        /// </summary>
        public TractionCutOffRelayState CurrentTractionCutOffRelayState() => TractionCutOffRelay.State;

        /// <summary>
        /// Driver's closing order of the traction cut-off relay
        /// </summary>
        public bool TractionCutOffRelayDriverClosingOrder() => TractionCutOffRelay.DriverClosingOrder;

        /// <summary>
        /// Driver's opening order of the traction cut-off relay
        /// </summary>
        public bool TractionCutOffRelayDriverOpeningOrder() => TractionCutOffRelay.DriverOpeningOrder;

        /// <summary>
        /// Driver's closing authorization of the traction cut-off relay
        /// </summary>
        public bool TractionCutOffRelayDriverClosingAuthorization() => TractionCutOffRelay.DriverClosingAuthorization;

        /// <summary>
        /// Closing authorization of the traction cut-off relay
        /// </summary>
        public bool TractionCutOffRelayClosingAuthorization() => TractionCutOffRelay.ClosingAuthorization;

        public override PowerSupplyState GetPowerStatus()
        {
            var status = base.GetPowerStatus();
            PowerSupplyState engineStatus;
            switch (CurrentDieselEnginesState())
            {
                case DieselEngineState.Running:
                    engineStatus = PowerSupplyState.PowerOn;
                    break;
                case DieselEngineState.Starting:
                    engineStatus = PowerSupplyState.PowerOnOngoing;
                    break;
                default:
                    engineStatus = PowerSupplyState.PowerOff;
                    break;
            }
            if (status == engineStatus) return status;
            return PowerSupplyState.PowerOnOngoing;
        }

        /// <summary>
        /// Sends an event to all diesel engines
        /// </summary>
        public void SignalEventToDieselEngines(PowerSupplyEvent evt) => DieselEngines.HandleEvent(evt);

        /// <summary>
        /// Sends an event to one diesel engine
        /// </summary>
        public void SignalEventToDieselEngine(PowerSupplyEvent evt, int id) => DieselEngines.HandleEvent(evt, id);

        /// <summary>
        /// Sends an event to the traction cut-off relay
        /// </summary>
        public void SignalEventToTractionCutOffRelay(PowerSupplyEvent evt) => TractionCutOffRelay.HandleEvent(evt);
        public void SignalEventToTractionCutOffRelay(PowerSupplyEvent evt, int id) => TractionCutOffRelay.HandleEvent(evt, id);

        public override void HandleEvent(PowerSupplyEvent evt)
        {
            base.HandleEvent(evt);

            // By default, send the event to every component
            SignalEventToTractionCutOffRelay(evt);
            SignalEventToDieselEngines(evt);
        }

        public override void HandleEvent(PowerSupplyEvent evt, int id)
        {
            base.HandleEvent(evt, id);

            // By default, send the event to every component
            SignalEventToTractionCutOffRelay(evt, id);
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
