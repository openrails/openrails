// COPYRIGHT 2020 by the Open Rails project.
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

using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    /// <summary>
    /// Base interface for any kind of line electric power supply via catenary, third rail, etc.
    /// </summary>
    public interface IPowerSupply : ISubSystem<IPowerSupply>, IParsable
    {
        TrainCar Car { get; }
        Battery Battery { get; }
        BatterySwitch BatterySwitch { get; }
        Pantographs Pantographs { get; }

        PowerSupplyState ElectricTrainSupplyState { get; set; }
        bool ElectricTrainSupplyOn { get; }
        bool FrontElectricTrainSupplyCableConnected { get; set; }
        float ElectricTrainSupplyPowerW { get; }

        PowerSupplyState LowVoltagePowerSupplyState { get; set; }
        bool LowVoltagePowerSupplyOn { get; }

        PowerSupplyState BatteryState { get; set; }
        bool BatteryOn { get; }
        float BatteryVoltageV { get; }

        void HandleEvent(PowerSupplyEvent evt);
        void HandleEvent(PowerSupplyEvent evt, int id);
        void HandleEventFromLeadLocomotive(PowerSupplyEvent evt);
        void HandleEventFromLeadLocomotive(PowerSupplyEvent evt, int id);
    }
}
