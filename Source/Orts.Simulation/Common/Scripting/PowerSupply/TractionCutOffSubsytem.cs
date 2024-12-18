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
    /// Abstract class for circuit breaker (electric and dual mode locomotives) and traction cut-off relay (diesel and dual mode locomotive)
    /// </summary>
    public abstract class TractionCutOffSubsystem : AbstractTrainScriptClass
    {
        internal ITractionCutOffSubsystem Host;
        internal ScriptedLocomotivePowerSupply PowerSupply => Host.PowerSupply as ScriptedLocomotivePowerSupply;
        internal MSTSLocomotive Locomotive => PowerSupply.Car as MSTSLocomotive;
        internal MSTSDieselLocomotive DieselLocomotive => Locomotive as MSTSDieselLocomotive;

        /// <summary>
        /// Attaches the script to its host
        /// </summary>
        /// <param name="host">The hosting ITractionCutOffSubsystem object</param>
        internal void AttachToHost(ITractionCutOffSubsystem host)
        {
            Host = host;
            Car = Locomotive;
        }

        /// <summary>
        /// Locomotive's power supply type
        /// </summary>
        protected PowerSupplyType SupplyType() => PowerSupply.Type;

        /// <summary>
        /// Current state of the pantograph
        /// </summary>
        protected PantographState CurrentPantographState() => Locomotive?.Pantographs.State ?? PantographState.Unavailable;

        /// <summary>
        /// Current state of the diesel engine
        /// </summary>
        protected DieselEngineState CurrentDieselEngineState() => DieselLocomotive?.DieselEngines.State ?? DieselEngineState.Unavailable;

        /// <summary>
        /// Current state of the power supply
        /// </summary>
        protected PowerSupplyState CurrentPowerSupplyState() => PowerSupply.MainPowerSupplyState;

        /// <summary>
        /// Driver's circuit breaker closing order
        /// </summary>
        protected bool DriverClosingOrder() => Host.DriverClosingOrder;

        /// <summary>
        /// Driver's circuit breaker closing authorization
        /// </summary>
        protected bool DriverClosingAuthorization() => Host.DriverClosingAuthorization;

        /// <summary>
        /// Driver's circuit breaker opening order
        /// </summary>
        protected bool DriverOpeningOrder() => Host.DriverOpeningOrder;

        /// <summary>
        /// TCS' circuit breaker closing authorization
        /// </summary>
        protected bool TCSClosingAuthorization() => Host.TCSClosingAuthorization;

        /// <summary>
        /// Circuit breaker closing authorization
        /// </summary>
        protected bool ClosingAuthorization() => Host.ClosingAuthorization;

        /// <summary>
        /// True if low voltage power supply is switched on.
        /// </summary>
        protected bool IsLowVoltagePowerSupplyOn() => PowerSupply.LowVoltagePowerSupplyOn;

        /// <summary>
        /// True if cab power supply is switched on.
        /// </summary>
        protected bool IsCabPowerSupplyOn() => PowerSupply.CabPowerSupplyOn;

        /// <summary>
        /// Delay before circuit breaker closing
        /// </summary>
        protected float ClosingDelayS() => Host.DelayS;

        /// <summary>
        /// True if the service retention is active
        /// </summary>
        protected bool ServiceRetentionActive
        {
            get => PowerSupply.ServiceRetentionActive;
        }

        /// <summary>
        /// Sets the driver's circuit breaker closing order
        /// </summary>
        protected void SetDriverClosingOrder(bool value)
        {
            Host.DriverClosingOrder = value;
        }

        /// <summary>
        /// Sets the driver's circuit breaker closing authorization
        /// </summary>
        protected void SetDriverClosingAuthorization(bool value)
        {
            Host.DriverClosingAuthorization = value;
        }

        /// <summary>
        /// Sets the driver's circuit breaker opening order
        /// </summary>
        protected void SetDriverOpeningOrder(bool value)
        {
            Host.DriverOpeningOrder = value;
        }

        /// <summary>
        /// Sets the circuit breaker closing authorization
        /// </summary>
        protected void SetClosingAuthorization(bool value)
        {
            Host.ClosingAuthorization = value;
        }

        /// <summary>
        /// Called once at initialization time.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Called once at initialization time if the train speed is greater than 0.
        /// Set as virtual to keep compatibility with scripts not providing this method.
        /// </summary>
        public virtual void InitializeMoving() { }

        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        public abstract void Update(float elapsedClockSeconds);

        /// <summary>
        /// Called when an event happens (a closing order from the driver for example)
        /// </summary>
        /// <param name="evt">The event happened</param>
        public abstract void HandleEvent(PowerSupplyEvent evt);
        public virtual void HandleEvent(PowerSupplyEvent evt, int id) {}

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
