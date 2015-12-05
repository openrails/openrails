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

using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using ORTS.Common;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

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

            foreach (var drawers in from drawer in ParticleDrawers
                                    where drawer.Key.ToLowerInvariant().StartsWith("exhaust")
                                    select drawer.Value)
            {
                Exhaust.AddRange(drawers);
            }
            foreach (var drawer in Exhaust)
                drawer.Initialize(dieselTexture);
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
            UserInputCommands.Add(UserCommands.ControlDieselPlayer, new Action[] { Noop, () => StartStopPlayerEngine() });
            UserInputCommands.Add(UserCommands.ControlDieselHelper, new Action[] { Noop, () => StartStopHelpersEngine() });
            base.InitializeUserInputCommands();
        }

        public void StartStopPlayerEngine()
        {
            if (DieselLocomotive.ThrottlePercent < 1)
            {
                //                    DieselLocomotive.PowerOn = !DieselLocomotive.PowerOn;
                if (DieselLocomotive.DieselEngines[0].EngineStatus == DieselEngine.Status.Stopped)
                {
                    DieselLocomotive.DieselEngines[0].Start();
                    DieselLocomotive.SignalEvent(Event.EnginePowerOn); // power on sound hook
                }
                if (DieselLocomotive.DieselEngines[0].EngineStatus == DieselEngine.Status.Running)
                {
                    DieselLocomotive.DieselEngines[0].Stop();
                    DieselLocomotive.SignalEvent(Event.EnginePowerOff); // power off sound hook
                }
                Viewer.Simulator.Confirmer.Confirm(CabControl.PlayerDiesel, DieselLocomotive.DieselEngines.PowerOn ? CabSetting.On : CabSetting.Off);
            }
            else
            {
                Viewer.Simulator.Confirmer.Warning(CabControl.PlayerDiesel, CabSetting.Warn1);
            }
        }

        public void StartStopHelpersEngine()
        {
            var powerOn = false;
            var helperLocos = 0;

            foreach (var car in DieselLocomotive.Train.Cars)
            {
                var mstsDieselLocomotive = car as MSTSDieselLocomotive;
                if (mstsDieselLocomotive != null && mstsDieselLocomotive.AcceptMUSignals)
                {
                    if (mstsDieselLocomotive.DieselEngines.Count > 0)
                    {
                        if ((car == Program.Simulator.PlayerLocomotive))
                        {
                            if ((mstsDieselLocomotive.DieselEngines.Count > 1))
                            {
                                for (int i = 1; i < mstsDieselLocomotive.DieselEngines.Count; i++)
                                {
                                    if (mstsDieselLocomotive.DieselEngines[i].EngineStatus == DieselEngine.Status.Stopped)
                                    {
                                        mstsDieselLocomotive.DieselEngines[i].Start();
                                    }
                                    if (mstsDieselLocomotive.DieselEngines[i].EngineStatus == DieselEngine.Status.Running)
                                    {
                                        mstsDieselLocomotive.DieselEngines[i].Stop();
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (DieselEngine de in mstsDieselLocomotive.DieselEngines)
                            {
                                if (de.EngineStatus == DieselEngine.Status.Stopped)
                                {
                                    de.Start();
                                }
                                if (de.EngineStatus == DieselEngine.Status.Running)
                                {
                                    de.Stop();
                                }
                            }
                        }
                    }
                    //mstsDieselLocomotive.StartStopDiesel();
                    powerOn = mstsDieselLocomotive.DieselEngines.PowerOn;
                    if ((car != Program.Simulator.PlayerLocomotive) && (mstsDieselLocomotive.AcceptMUSignals))
                    {
                        if ((mstsDieselLocomotive.DieselEngines[0].EngineStatus == DieselEngine.Status.Stopped) ||
                            (mstsDieselLocomotive.DieselEngines[0].EngineStatus == DieselEngine.Status.Stopping))
                            mstsDieselLocomotive.SignalEvent(Event.EnginePowerOff);
                        else
                            mstsDieselLocomotive.SignalEvent(Event.EnginePowerOn);
                    }
                    helperLocos++;
                }
            }
            // One confirmation however many helper locomotives
            // <CJComment> Couldn't make one confirmation per loco work correctly :-( </CJComment>
            if (helperLocos > 0)
            {
                Viewer.Simulator.Confirmer.Confirm(CabControl.HelperDiesel, powerOn ? CabSetting.On : CabSetting.Off);
            }

        }

        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var car = this.Car as MSTSDieselLocomotive;
            var exhaustParticles = car.Train != null && car.Train.TrainType == Train.TRAINTYPE.STATIC ? 0 : car.ExhaustParticles.SmoothedValue;
            foreach (var drawer in Exhaust)
            {
                drawer.SetOutput(exhaustParticles, car.ExhaustMagnitude.SmoothedValue, new Color((byte)car.ExhaustColorR.SmoothedValue, (byte)car.ExhaustColorG.SmoothedValue, (byte)car.ExhaustColorB.SmoothedValue));
            }
            base.PrepareFrame(frame, elapsedTime);
        }
    }
}
