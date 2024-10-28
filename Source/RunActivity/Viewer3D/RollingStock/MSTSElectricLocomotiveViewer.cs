// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using System;
using Orts.Common;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.RollingStock
{
    public class MSTSElectricLocomotiveViewer : MSTSLocomotiveViewer
    {

        MSTSElectricLocomotive ElectricLocomotive;

        public MSTSElectricLocomotiveViewer(Viewer viewer, MSTSElectricLocomotive car)
            : base(viewer, car)
        {
            ElectricLocomotive = car;
            if (ElectricLocomotive.Train != null && (car.Train.TrainType == Train.TRAINTYPE.AI ||
                ((car.Train.TrainType == Train.TRAINTYPE.PLAYER || car.Train.TrainType == Train.TRAINTYPE.AI_PLAYERDRIVEN || car.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING) &&
                (car.Train.MUDirection != Direction.N && ElectricLocomotive.LocomotivePowerSupply.MainPowerSupplyOn))))
                // following reactivates the sound triggers related to certain states
                // for pantos the sound trigger related to the raised panto must be reactivated, else SignalEvent() would raise also another panto
            {
                var iPanto = 0;
                Event evt;
                foreach (var panto in ElectricLocomotive.Pantographs.List)
                {
                    if (panto.State == ORTS.Scripting.Api.PantographState.Up)
                    {
                        switch (iPanto)
                        {
                            case 0: evt = Event.Pantograph1Up; break;
                            case 1: evt = Event.Pantograph2Up; break;
                            case 2: evt = Event.Pantograph3Up; break;
                            case 3: evt = Event.Pantograph4Up; break;
                            default: evt = Event.Pantograph1Up; break;
                        }
                        ElectricLocomotive.SignalEvent(evt);
                    }
                    iPanto++;
                }
                ElectricLocomotive.SignalEvent(Event.EnginePowerOn);
                ElectricLocomotive.SignalEvent(Event.ReverserToForwardBackward);
                ElectricLocomotive.SignalEvent(Event.ReverserChange);
            }
        }


        /// <summary>
        /// A keyboard or mouse click has occurred. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            base.HandleUserInput(elapsedTime);
        }

        public override void InitializeUserInputCommands()
        {
            UserInputCommands.Add(UserCommand.ControlBatterySwitchClose, new Action[] {
                () => new BatterySwitchCloseButtonCommand(Viewer.Log, false),
                () => {
                    new BatterySwitchCloseButtonCommand(Viewer.Log, true);
                    new BatterySwitchCommand(Viewer.Log, !ElectricLocomotive.LocomotivePowerSupply.BatterySwitch.CommandSwitch);
                }
            });
            UserInputCommands.Add(UserCommand.ControlBatterySwitchOpen, new Action[] {
                () => new BatterySwitchOpenButtonCommand(Viewer.Log, false),
                () => new BatterySwitchOpenButtonCommand(Viewer.Log, true)
            });
            UserInputCommands.Add(UserCommand.ControlMasterKey, new Action[] { Noop, () => new ToggleMasterKeyCommand(Viewer.Log, !ElectricLocomotive.LocomotivePowerSupply.MasterKey.CommandSwitch) });
            UserInputCommands.Add(UserCommand.ControlServiceRetention, new Action[] { () => new ServiceRetentionButtonCommand(Viewer.Log, false), () => new ServiceRetentionButtonCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlServiceRetentionCancellation, new Action[] { () => new ServiceRetentionCancellationButtonCommand(Viewer.Log, false), () => new ServiceRetentionCancellationButtonCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlElectricTrainSupply, new Action[] { Noop, () => new ElectricTrainSupplyCommand(Viewer.Log, !ElectricLocomotive.LocomotivePowerSupply.ElectricTrainSupplySwitch.CommandSwitch) });
            UserInputCommands.Add(UserCommand.ControlCircuitBreakerClosingOrder, new Action[] {
                () => new CircuitBreakerClosingOrderButtonCommand(Viewer.Log, false),
                () => {
                    new CircuitBreakerClosingOrderCommand(Viewer.Log, !ElectricLocomotive.ElectricPowerSupply.CircuitBreaker.DriverClosingOrder);
                    new CircuitBreakerClosingOrderButtonCommand(Viewer.Log, true);
                }
            });
            UserInputCommands.Add(UserCommand.ControlCircuitBreakerOpeningOrder, new Action[] { () => new CircuitBreakerOpeningOrderButtonCommand(Viewer.Log, false), () => new CircuitBreakerOpeningOrderButtonCommand(Viewer.Log, true)});
            UserInputCommands.Add(UserCommand.ControlCircuitBreakerClosingAuthorization, new Action[] { Noop, () => new CircuitBreakerClosingAuthorizationCommand(Viewer.Log, !ElectricLocomotive.ElectricPowerSupply.CircuitBreaker.DriverClosingAuthorization) });
            base.InitializeUserInputCommands();
        }

        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {

            base.PrepareFrame(frame, elapsedTime);
        }
    }
}
