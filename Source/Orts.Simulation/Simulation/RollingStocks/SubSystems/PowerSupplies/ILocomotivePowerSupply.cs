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

using ORTS.Scripting.Api;

namespace Orts.Simulation.RollingStocks.SubSystems.PowerSupplies
{
    /// <summary>
    /// Base class for a controllable power supply for an electric or dual-mode locmotive.
    /// </summary>
    public interface ILocomotivePowerSupply : IPowerSupply
    {
        PowerSupplyType Type { get; }

        MasterKey MasterKey { get; }
        ElectricTrainSupplySwitch ElectricTrainSupplySwitch { get; }

        PowerSupplyState MainPowerSupplyState { get; set; }
        bool MainPowerSupplyOn { get; }
        bool DynamicBrakeAvailable { get; set; }
        float PowerSupplyDynamicBrakePercent { get; set; }
        float MaximumDynamicBrakePowerW { get; set; }
        float MaxThrottlePercent { get; set; }
        float ThrottleReductionPercent { get; set; }

        PowerSupplyState AuxiliaryPowerSupplyState { get; set; }
        bool AuxiliaryPowerSupplyOn { get; }

        PowerSupplyState CabPowerSupplyState { get; set; }
        bool CabPowerSupplyOn { get; }

        bool ServiceRetentionButton { get; set; }
        bool ServiceRetentionCancellationButton { get; set; }
        bool ServiceRetentionActive { get; set; }

        PowerSupplyState GetPowerStatus();

        void HandleEventFromTcs(PowerSupplyEvent evt);
        void HandleEventFromTcs(PowerSupplyEvent evt, int id);
        void HandleEventFromTcs(PowerSupplyEvent evt, string message);
        void HandleEventFromOtherLocomotive(int locoIndex, PowerSupplyEvent evt);
        void HandleEventFromOtherLocomotive(int locoIndex, PowerSupplyEvent evt, int id);
    }
}
