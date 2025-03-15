// COPYRIGHT 2022 by the Open Rails project.
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

using Orts.Common;
using Orts.Simulation;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using System.Collections.Generic;

namespace ORTS.Scripting.Api
{
    public abstract class PantographSelector
    {
        // Internal members and methods (inaccessible from script)
        internal ScriptedPantographSelector Host;
        internal ScriptedLocomotivePowerSupply PowerSupply => Host.PowerSupply;

        internal void AttachToHost(ScriptedPantographSelector host)
        {
            Host = host;
        }

        // Protected and public members and methods (accessible from script)
        public float GameTime => (float) Host.Simulator.GameTime;
        public bool PreUpdate => Host.Simulator.PreUpdate;
        public List<PantographSelectorPosition> Positions => Host.Positions;
        public PantographSelectorPosition Position
        {
            get => Host.Position;
            set
            {
                if (Positions.Contains(value))
                {
                    Host.Position = value;
                }
            }
        }
        public VoltageSelectorPosition VoltageSelectorPosition
        {
            get
            {
                switch (PowerSupply)
                {
                    case ScriptedElectricPowerSupply electricPowerSupply:
                        return electricPowerSupply.VoltageSelector.Position;
                    case ScriptedDualModePowerSupply dualModePowerSupply:
                        return dualModePowerSupply.VoltageSelector.Position;
                    default:
                        return null;
                }
            }
        }
        public PowerLimitationSelectorPosition PowerLimitationSelectorPosition
        {
            get
            {
                switch (PowerSupply)
                {
                    case ScriptedElectricPowerSupply electricPowerSupply:
                        return electricPowerSupply.PowerLimitationSelector.Position;
                    case ScriptedDualModePowerSupply dualModePowerSupply:
                        return dualModePowerSupply.PowerLimitationSelector.Position;
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Called once at initialization time.
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// Called once at initialization time if the train speed is greater than 0.
        /// Set as virtual to keep compatibility with scripts not providing this method.
        /// </summary>
        public virtual void InitializeMoving() { }
        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        public virtual void Update(float elapsedClockSeconds) { }
        /// <summary>
        /// Called when the driver (or the train's systems) want something to happen on the power supply system
        /// </summary>
        /// <param name="evt">The event</param>
        public virtual void HandleEvent(PowerSupplyEvent evt) {}

        public virtual void HandleEvent(PowerSupplyEvent evt, int id) {}

        /// <summary>
        /// Confirms a command done by the player with a pre-set message on the screen.
        /// </summary>
        protected void Confirm(CabControl control, CabSetting setting) => Host.Simulator.Confirmer.Confirm(control, setting);

        /// <summary>
        /// Displays a message on the screen.
        /// </summary>
        protected void Message(CabControl control, string message) => Host.Simulator.Confirmer.Message(control, message);

        protected void SignalEvent(Event evt) => Host.Locomotive.SignalEvent(evt);

        protected void SignalEventToTrain(Event evt) => Host.Locomotive.Train?.SignalEvent(evt);

        protected void SignalEventToPowerSupply(PowerSupplyEvent evt) => PowerSupply.HandleEvent(evt);

        protected void SignalEventToPowerSupply(PowerSupplyEvent evt, int id) => PowerSupply.HandleEvent(evt, id);

        /// <summary>
        /// Sets the value for a cabview control.
        /// </summary>
        protected void SetCabDisplayControl(int index, float value)
        {
            PowerSupply.CabDisplayControls[index] = value;
        }

        /// <summary>
        /// Sets the name which is to be shown which putting the cursor above a cabview control.
        /// </summary>
        protected void SetCustomizedCabviewControlName(int index, string name)
        {
            PowerSupply.CustomizedCabviewControlNames[index] = name;
        }
    }
}
