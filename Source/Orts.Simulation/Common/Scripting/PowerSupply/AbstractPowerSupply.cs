// COPYRIGHT 2014 by the Open Rails project.
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
    public abstract class AbstractPowerSupply : AbstractScriptClass
    {
        /// <summary>
        /// Current state of the power supply
        /// </summary>
        public Func<PowerSupplyState> CurrentState;
        /// <summary>
        /// Current state of the auxiliary power supply
        /// </summary>
        public Func<PowerSupplyState> CurrentAuxiliaryState;
        /// <summary>
        /// Main supply power on delay
        /// </summary>
        public Func<float> PowerOnDelayS;
        /// <summary>
        /// Auxiliary supply power on delay
        /// </summary>
        public Func<float> AuxPowerOnDelayS;

        /// <summary>
        /// Sets the current state of the power supply
        /// </summary>
        public Action<PowerSupplyState> SetCurrentState;
        /// <summary>
        /// Sets the current state of the auxiliary power supply
        /// </summary>
        public Action<PowerSupplyState> SetCurrentAuxiliaryState;

        /// <summary>
        /// Called once at initialization time.
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        public abstract void Update(float elapsedClockSeconds);
    }

    public enum PowerSupplyEvent
    {
        RaisePantograph,
        LowerPantograph,
        CloseCircuitBreaker,
        OpenCircuitBreaker,
        CloseCircuitBreakerButtonPressed,
        CloseCircuitBreakerButtonReleased,
        OpenCircuitBreakerButtonPressed,
        OpenCircuitBreakerButtonReleased,
        GiveCircuitBreakerClosingAuthorization,
        RemoveCircuitBreakerClosingAuthorization,
        StartEngine,
        StopEngine,
        ClosePowerContactor,
        OpenPowerContactor,
        GivePowerContactorClosingAuthorization,
        RemovePowerContactorClosingAuthorization
    }

    public enum PowerSupplyState
    {
        [GetParticularString("PowerSupply", "Off")] PowerOff,
        [GetParticularString("PowerSupply", "On ongoing")] PowerOnOngoing,
        [GetParticularString("PowerSupply", "On")] PowerOn
    }

    public enum PantographState
    {
        [GetParticularString("Pantograph", "Down")] Down,
        [GetParticularString("Pantograph", "Lowering")] Lowering,
        [GetParticularString("Pantograph", "Raising")] Raising,
        [GetParticularString("Pantograph", "Up")] Up
    }
}
