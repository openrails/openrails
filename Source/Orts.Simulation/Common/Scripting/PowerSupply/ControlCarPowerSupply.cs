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

using System;
using Orts.Simulation.RollingStocks;

namespace ORTS.Scripting.Api
{
    /// <summary>
    /// Power supply for control cars
    /// </summary>
    public abstract class ControlCarPowerSupply : LocomotivePowerSupply
    {
        MSTSLocomotive ControlActiveLocomotive => Locomotive.ControlActiveLocomotive;
        /// <summary>
        /// Index of the control active locomotive in the train (taking into account only locomotives)
        /// </summary>
        public int IndexOfControlActiveLocomotive
        {
            get
            {
                int count=0;
                for (int i=0; i<Train.Cars.Count; i++)
                {
                    if (Train.Cars[i] is MSTSLocomotive)
                    {
                        if (Train.Cars[i] == ControlActiveLocomotive) return count;
                        count++;
                    }
                }
                return -1;
            }
        }
        protected override void SetCurrentMainPowerSupplyState(PowerSupplyState state) {}
        protected override void SetCurrentAuxiliaryPowerSupplyState(PowerSupplyState state) {}
        protected override void SetCurrentElectricTrainSupplyState(PowerSupplyState state) {}
        protected override void SetCurrentDynamicBrakeAvailability(bool avail) {}
        public override PowerSupplyState GetPowerStatus() => PowerSupplyState.Unavailable;
        public void SignalEventToControlActiveLocomotive(PowerSupplyEvent evt)
        {
            ControlActiveLocomotive?.LocomotivePowerSupply.HandleEvent(evt);
        }
        public void SignalEventToControlActiveLocomotive(PowerSupplyEvent evt, int id)
        {
            ControlActiveLocomotive?.LocomotivePowerSupply.HandleEvent(evt, id);
        }
    }
}
