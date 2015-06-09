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
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using ORTS.Settings;

namespace ORTS.Viewer3D.RollingStock
{
    public class MSTSSteamLocomotiveViewer : MSTSLocomotiveViewer
    {
        float Throttlepercent;
        float Color_Value;

        MSTSSteamLocomotive SteamLocomotive { get { return (MSTSSteamLocomotive)Car; } }
        List<ParticleEmitterViewer> Cylinders = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Drainpipe = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> SafetyValves = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Stack = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Whistle = new List<ParticleEmitterViewer>();

        public MSTSSteamLocomotiveViewer(Viewer viewer, MSTSSteamLocomotive car)
            : base(viewer, car)
        {
            // Now all the particle drawers have been setup, assign them textures based
            // on what emitters we know about.
            string steamTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\smokemain.ace";

            foreach (var emitter in ParticleDrawers)
            {
                if (emitter.Key.ToLowerInvariant() == "cylindersfx")
                    Cylinders.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "drainpipefx")
                    Drainpipe.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "safetyvalvesfx")
                    SafetyValves.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "stackfx")
                    Stack.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "whistlefx")
                    Whistle.AddRange(emitter.Value);
                foreach (var drawer in emitter.Value)
                    drawer.Initialize(steamTexture);
            }
        }

        /// <summary>
        /// Overrides the base method as steam locomotives have continuous reverser controls and so
        /// lacks the throttle interlock and warning in other locomotives. 
        /// </summary>
        protected override void ReverserControlForwards()
        {
            SteamLocomotive.StartReverseIncrease(null);
        }

        /// <summary>
        /// Overrides the base method as steam locomotives have continuous reverser controls and so
        /// lacks the throttle interlock and warning in other locomotives. 
        /// </summary>
        protected override void ReverserControlBackwards()
        {
            SteamLocomotive.StartReverseDecrease(null);
        }
        
        /// <summary>
        /// Overrides the base method as steam locomotives have only rudimentary gear boxes. 
        /// </summary>
        protected override void StartGearBoxIncrease()
        {
            SteamLocomotive.SteamStartGearBoxIncrease();
        }        
                
        protected override void StopGearBoxIncrease()
        {
           SteamLocomotive.SteamStopGearBoxIncrease();
        }

        protected override void StartGearBoxDecrease()
        {
            SteamLocomotive.SteamStartGearBoxDecrease();
        }

        protected override void StopGearBoxDecrease()
        {
            SteamLocomotive.SteamStopGearBoxDecrease();
        }
                

        public override void InitializeUserInputCommands()
        {
            UserInputCommands.Add(UserCommands.ControlForwards, new Action[] { () => SteamLocomotive.StopReverseIncrease(), () => ReverserControlForwards() });
            UserInputCommands.Add(UserCommands.ControlBackwards, new Action[] { () => SteamLocomotive.StopReverseDecrease(), () => ReverserControlBackwards() });
            UserInputCommands.Add(UserCommands.ControlInjector1Increase, new Action[] { () => SteamLocomotive.StopInjector1Increase(), () => SteamLocomotive.StartInjector1Increase(null) });
            UserInputCommands.Add(UserCommands.ControlInjector1Decrease, new Action[] { () => SteamLocomotive.StopInjector1Decrease(), () => SteamLocomotive.StartInjector1Decrease(null) });
            UserInputCommands.Add(UserCommands.ControlInjector2Increase, new Action[] { () => SteamLocomotive.StopInjector2Increase(), () => SteamLocomotive.StartInjector2Increase(null) });
            UserInputCommands.Add(UserCommands.ControlInjector2Decrease, new Action[] { () => SteamLocomotive.StopInjector2Decrease(), () => SteamLocomotive.StartInjector2Decrease(null) });
            UserInputCommands.Add(UserCommands.ControlInjector1, new Action[] { Noop, () => new ToggleInjectorCommand(Viewer.Log, 1) });
            UserInputCommands.Add(UserCommands.ControlInjector2, new Action[] { Noop, () => new ToggleInjectorCommand(Viewer.Log, 2) });
            UserInputCommands.Add(UserCommands.ControlBlowerIncrease, new Action[] { () => SteamLocomotive.StopBlowerIncrease(), () => SteamLocomotive.StartBlowerIncrease(null) });
            UserInputCommands.Add(UserCommands.ControlBlowerDecrease, new Action[] { () => SteamLocomotive.StopBlowerDecrease(), () => SteamLocomotive.StartBlowerDecrease(null) });
            UserInputCommands.Add(UserCommands.ControlDamperIncrease, new Action[] { () => SteamLocomotive.StopDamperIncrease(), () => SteamLocomotive.StartDamperIncrease(null) });
            UserInputCommands.Add(UserCommands.ControlDamperDecrease, new Action[] { () => SteamLocomotive.StopDamperDecrease(), () => SteamLocomotive.StartDamperDecrease(null) });
            UserInputCommands.Add(UserCommands.ControlFireboxOpen, new Action[] { () => SteamLocomotive.StopFireboxDoorIncrease(), () => SteamLocomotive.StartFireboxDoorIncrease(null) });
            UserInputCommands.Add(UserCommands.ControlFireboxClose, new Action[] { () => SteamLocomotive.StopFireboxDoorDecrease(), () => SteamLocomotive.StartFireboxDoorDecrease(null) });
            UserInputCommands.Add(UserCommands.ControlFiringRateIncrease, new Action[] { () => SteamLocomotive.StopFiringRateIncrease(), () => SteamLocomotive.StartFiringRateIncrease(null) });
            UserInputCommands.Add(UserCommands.ControlFiringRateDecrease, new Action[] { () => SteamLocomotive.StopFiringRateDecrease(), () => SteamLocomotive.StartFiringRateDecrease(null) });
            UserInputCommands.Add(UserCommands.ControlFireShovelFull, new Action[] { Noop, () => new FireShovelfullCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlCylinderCocks, new Action[] { Noop, () => new ToggleCylinderCocksCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlCylinderCompound, new Action[] { Noop, () => new ToggleCylinderCompoundCommand(Viewer.Log) });
            base.InitializeUserInputCommands();
        }

        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            // Keeping separated, since it is not a real engine control. (Probably wrong classification?)
            if (UserInput.IsPressed(UserCommands.ControlFiring)) new ToggleManualFiringCommand(Viewer.Log);

            if (UserInput.RDState != null && UserInput.RDState.Changed)
                SteamLocomotive.SetCutoffPercent(UserInput.RDState.DirectionPercent);

            base.HandleUserInput(elapsedTime);

#if DEBUG_DUMP_STEAM_POWER_CURVE
            // For power curve tests
            if (Viewer.Settings.DataLogger
                && !Viewer.Settings.DataLogPerformanceeous
                && !Viewer.Settings.DataLogPhysics
                && !Viewer.Settings.DataLogMisc)
            {
                var loco = SteamLocomotive;
                // If we're using more steam than the boiler can make ...
                if (loco.PreviousTotalSteamUsageLBpS > loco.EvaporationLBpS)
                {
                    // Reduce the cut-off gradually as far as 15%
                    if (loco.CutoffController.CurrentValue > 0.15)
                    {
                        float? target = MathHelper.Clamp(loco.CutoffController.CurrentValue - 0.01f, 0.15f, 0.75f);
                        loco.StartReverseDecrease(target);
                    }
                    else
                    {
                        // Reduce the throttle also
                        float? target = SteamLocomotive.ThrottleController.CurrentValue - 0.01f;
                        loco.StartThrottleDecrease(target);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var car = Car as MSTSSteamLocomotive;

            foreach (var drawer in Cylinders)
                drawer.SetOutput(5, car.CylindersSteamVolumeM3pS * 10);

            foreach (var drawer in Drainpipe)
                drawer.SetOutput(0, 0);

            foreach (var drawer in SafetyValves)
                drawer.SetOutput(car.CylindersSteamVelocityMpS, car.SafetyValvesSteamVolumeM3pS);

            Throttlepercent = car.ThrottlePercent > 0 ? car.ThrottlePercent / 10f : 0f;

            foreach (var drawer in Stack)
            {
                Color_Value = car.SmokeColor.SmoothedValue;
                drawer.SetOutput(car.StackSteamVelocityMpS.SmoothedValue, car.StackSteamVolumeM3pS / Stack.Count + car.FireRatio, Throttlepercent + car.FireRatio, new Color(Color_Value, Color_Value, Color_Value));
            }

            foreach (var drawer in Whistle)
                drawer.SetOutput(5, (car.Horn ? 5 : 0));

            base.PrepareFrame(frame, elapsedTime);
        }
    }
}
