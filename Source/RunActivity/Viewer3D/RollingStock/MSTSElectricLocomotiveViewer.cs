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
            if (ElectricLocomotive.Train != null && car.Train.TrainType == Train.TRAINTYPE.AI)
            {
                ElectricLocomotive.SignalEvent(Event.Pantograph1Up);
                ElectricLocomotive.SignalEvent(Event.EnginePowerOn);
                ElectricLocomotive.SignalEvent(Event.ReverserToForwardBackward);
                ElectricLocomotive.SignalEvent(Event.ReverserChange);
            }
        }

        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            base.HandleUserInput(elapsedTime);
        }

        public override void InitializeUserInputCommands()
        {
            UserInputCommands.Add(UserCommand.ControlCircuitBreakerClosingOrder, new Action[] {
                () => new CircuitBreakerClosingOrderButtonCommand(Viewer.Log, false),
                () => {
                    new CircuitBreakerClosingOrderCommand(Viewer.Log, !ElectricLocomotive.PowerSupply.CircuitBreaker.DriverClosingOrder);
                    new CircuitBreakerClosingOrderButtonCommand(Viewer.Log, true);
                }
            });
            UserInputCommands.Add(UserCommand.ControlCircuitBreakerOpeningOrder, new Action[] { () => new CircuitBreakerOpeningOrderButtonCommand(Viewer.Log, false), () => new CircuitBreakerOpeningOrderButtonCommand(Viewer.Log, true)});
            UserInputCommands.Add(UserCommand.ControlCircuitBreakerClosingAuthorization, new Action[] { Noop, () => new CircuitBreakerClosingAuthorizationCommand(Viewer.Log, !ElectricLocomotive.PowerSupply.CircuitBreaker.DriverClosingAuthorization) });
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
