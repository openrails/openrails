// COPYRIGHT 2024 by the Open Rails project.
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
using Orts.Common;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.RollingStock
{
    public class MSTSControlTrailerCarViewer : MSTSLocomotiveViewer
    {

        MSTSControlTrailerCar ControlCar;
        MSTSLocomotive ControlActiveLocomotive => ControlCar.ControlActiveLocomotive;

        public MSTSControlTrailerCarViewer(Viewer viewer, MSTSControlTrailerCar car)
            : base(viewer, car)
        {
            ControlCar = car;
        }

        public override void InitializeUserInputCommands()
        {
            UserInputCommands.Add(UserCommand.ControlBatterySwitchClose, new Action[] {
                () => new BatterySwitchCloseButtonCommand(Viewer.Log, false),
                () => {
                    new BatterySwitchCloseButtonCommand(Viewer.Log, true);
                    new BatterySwitchCommand(Viewer.Log, !ControlCar.LocomotivePowerSupply.BatterySwitch.CommandSwitch);
                }
            });
            UserInputCommands.Add(UserCommand.ControlBatterySwitchOpen, new Action[] {
                () => new BatterySwitchOpenButtonCommand(Viewer.Log, false),
                () => new BatterySwitchOpenButtonCommand(Viewer.Log, true)
            });
            UserInputCommands.Add(UserCommand.ControlMasterKey, new Action[] { Noop, () => new ToggleMasterKeyCommand(Viewer.Log, !ControlCar.LocomotivePowerSupply.MasterKey.CommandSwitch) });
            UserInputCommands.Add(UserCommand.ControlServiceRetention, new Action[] { () => new ServiceRetentionButtonCommand(Viewer.Log, false), () => new ServiceRetentionButtonCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlServiceRetentionCancellation, new Action[] { () => new ServiceRetentionCancellationButtonCommand(Viewer.Log, false), () => new ServiceRetentionCancellationButtonCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlElectricTrainSupply, new Action[] { Noop, () => new ElectricTrainSupplyCommand(Viewer.Log, !ControlActiveLocomotive.LocomotivePowerSupply.ElectricTrainSupplySwitch.CommandSwitch) });
            UserInputCommands.Add(UserCommand.ControlCircuitBreakerClosingOrder, new Action[] {
                () => new CircuitBreakerClosingOrderButtonCommand(Viewer.Log, false),
                () => {
                    new CircuitBreakerClosingOrderCommand(Viewer.Log, !((ControlActiveLocomotive as MSTSElectricLocomotive)?.ElectricPowerSupply.CircuitBreaker.DriverClosingOrder ?? false));
                    new CircuitBreakerClosingOrderButtonCommand(Viewer.Log, true);
                }
            });
            UserInputCommands.Add(UserCommand.ControlCircuitBreakerOpeningOrder, new Action[] { () => new CircuitBreakerOpeningOrderButtonCommand(Viewer.Log, false), () => new CircuitBreakerOpeningOrderButtonCommand(Viewer.Log, true)});
            UserInputCommands.Add(UserCommand.ControlCircuitBreakerClosingAuthorization, new Action[] { Noop, () => new CircuitBreakerClosingAuthorizationCommand(Viewer.Log, !((ControlActiveLocomotive as MSTSElectricLocomotive)?.ElectricPowerSupply.CircuitBreaker.DriverClosingAuthorization ?? false)) });
            UserInputCommands.Add(UserCommand.ControlTractionCutOffRelayClosingOrder, new Action[] {
                () => new TractionCutOffRelayClosingOrderButtonCommand(Viewer.Log, false),
                () => {
                    new TractionCutOffRelayClosingOrderCommand(Viewer.Log, !((ControlActiveLocomotive as MSTSDieselLocomotive)?.DieselPowerSupply.TractionCutOffRelay.DriverClosingOrder ?? false));
                    new TractionCutOffRelayClosingOrderButtonCommand(Viewer.Log, true);
                }
            });
            UserInputCommands.Add(UserCommand.ControlTractionCutOffRelayOpeningOrder, new Action[] { () => new TractionCutOffRelayOpeningOrderButtonCommand(Viewer.Log, false), () => new TractionCutOffRelayOpeningOrderButtonCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlTractionCutOffRelayClosingAuthorization, new Action[] { Noop, () => new TractionCutOffRelayClosingAuthorizationCommand(Viewer.Log, !((ControlActiveLocomotive as MSTSDieselLocomotive)?.DieselPowerSupply.TractionCutOffRelay.DriverClosingAuthorization ?? false)) });
            UserInputCommands.Add(UserCommand.ControlDieselPlayer, new Action[] { Noop, () => new TogglePlayerEngineCommand(Viewer.Log) });
            base.InitializeUserInputCommands();
        }
    }
}
