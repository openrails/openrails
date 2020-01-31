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
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Orts.Common;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.RollingStock
{
    public class MSTSDieselLocomotiveViewer : MSTSLocomotiveViewer
    {
        MSTSDieselLocomotive DieselLocomotive { get { return (MSTSDieselLocomotive)Car; } }
        List<ParticleEmitterViewer> Exhaust = new List<ParticleEmitterViewer>();

        public MSTSDieselLocomotiveViewer(Viewer viewer, MSTSDieselLocomotive car)
            : base(viewer, car)
        {
            // Now all the particle drawers have been setup, assign them textures based
            // on what emitters we know about.

            string dieselTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\dieselsmoke.ace";


            // Diesel Exhaust
            foreach (var drawers in from drawer in ParticleDrawers
                                    where drawer.Key.ToLowerInvariant().StartsWith("exhaust")
                                    select drawer.Value)
            {
                Exhaust.AddRange(drawers);
            }
            foreach (var drawer in Exhaust)
                drawer.Initialize(dieselTexture);

            if (car.Train != null && (car.Train.TrainType == Train.TRAINTYPE.AI ||
                ((car.Train.TrainType == Train.TRAINTYPE.PLAYER || car.Train.TrainType == Train.TRAINTYPE.AI_PLAYERDRIVEN || car.Train.TrainType == Train.TRAINTYPE.AI_PLAYERDRIVEN) &&
                (car.Train.MUDirection != Direction.N && (car as MSTSDieselLocomotive).DieselEngines[0].EngineStatus == Simulation.RollingStocks.SubSystems.PowerSupplies.DieselEngine.Status.Running))))
            {
                (car as MSTSDieselLocomotive).SignalEvent(Event.ReverserToForwardBackward);
                (car as MSTSDieselLocomotive).SignalEvent(Event.ReverserChange);
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
            UserInputCommands.Add(UserCommand.ControlVacuumExhausterPressed, new Action[] { () => new VacuumExhausterCommand(Viewer.Log, false), () => new VacuumExhausterCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlDieselPlayer, new Action[] { Noop, () => new TogglePlayerEngineCommand(Viewer.Log) });
            base.InitializeUserInputCommands();
        }


        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var car = this.Car as MSTSDieselLocomotive;
            
            // Diesel exhaust
            var exhaustParticles = car.Train != null && car.Train.TrainType == Train.TRAINTYPE.STATIC ? 0 : car.ExhaustParticles.SmoothedValue;
            foreach (var drawer in Exhaust)
            {
                var colorR = car.ExhaustColorR.SmoothedValue / 255f;
                var colorG = car.ExhaustColorG.SmoothedValue / 255f;
                var colorB = car.ExhaustColorB.SmoothedValue / 255f;
                drawer.SetOutput(exhaustParticles, car.ExhaustMagnitude.SmoothedValue, new Color((byte)car.ExhaustColorR.SmoothedValue, (byte)car.ExhaustColorG.SmoothedValue, (byte)car.ExhaustColorB.SmoothedValue));
            }
            
            base.PrepareFrame(frame, elapsedTime);
        }
    }
}
