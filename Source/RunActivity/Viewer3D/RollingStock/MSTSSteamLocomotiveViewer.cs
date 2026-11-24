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
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.RollingStock
{
    public class MSTSSteamLocomotiveViewer : MSTSLocomotiveViewer
    {

        MSTSSteamLocomotive SteamLocomotive { get { return (MSTSSteamLocomotive)Car; } }

        public MSTSSteamLocomotiveViewer(Viewer viewer, MSTSSteamLocomotive car)
            : base(viewer, car)
        {

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
            UserInputCommands.Add(UserCommand.ControlForwards, new Action[] { () => SteamLocomotive.StopReverseIncrease(), () => ReverserControlForwards() });
            UserInputCommands.Add(UserCommand.ControlBackwards, new Action[] { () => SteamLocomotive.StopReverseDecrease(), () => ReverserControlBackwards() });
            UserInputCommands.Add(UserCommand.ControlInjector1Increase, new Action[] { () => SteamLocomotive.StopInjector1Increase(), () => SteamLocomotive.StartInjector1Increase(null) });
            UserInputCommands.Add(UserCommand.ControlInjector1Decrease, new Action[] { () => SteamLocomotive.StopInjector1Decrease(), () => SteamLocomotive.StartInjector1Decrease(null) });
            UserInputCommands.Add(UserCommand.ControlInjector2Increase, new Action[] { () => SteamLocomotive.StopInjector2Increase(), () => SteamLocomotive.StartInjector2Increase(null) });
            UserInputCommands.Add(UserCommand.ControlInjector2Decrease, new Action[] { () => SteamLocomotive.StopInjector2Decrease(), () => SteamLocomotive.StartInjector2Decrease(null) });
            UserInputCommands.Add(UserCommand.ControlInjector1, new Action[] { Noop, () => new ToggleInjectorCommand(Viewer.Log, 1) });
            UserInputCommands.Add(UserCommand.ControlInjector2, new Action[] { Noop, () => new ToggleInjectorCommand(Viewer.Log, 2) });
            UserInputCommands.Add(UserCommand.ControlBlowerIncrease, new Action[] { () => SteamLocomotive.StopBlowerIncrease(), () => SteamLocomotive.StartBlowerIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlBlowerDecrease, new Action[] { () => SteamLocomotive.StopBlowerDecrease(), () => SteamLocomotive.StartBlowerDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlDamperIncrease, new Action[] { () => SteamLocomotive.StopDamperIncrease(), () => SteamLocomotive.StartDamperIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlDamperDecrease, new Action[] { () => SteamLocomotive.StopDamperDecrease(), () => SteamLocomotive.StartDamperDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlFireboxOpen, new Action[] { () => SteamLocomotive.StopFireboxDoorIncrease(), () => SteamLocomotive.StartFireboxDoorIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlFireboxClose, new Action[] { () => SteamLocomotive.StopFireboxDoorDecrease(), () => SteamLocomotive.StartFireboxDoorDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlFiringRateIncrease, new Action[] { () => SteamLocomotive.StopFiringRateIncrease(), () => SteamLocomotive.StartFiringRateIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlFiringRateDecrease, new Action[] { () => SteamLocomotive.StopFiringRateDecrease(), () => SteamLocomotive.StartFiringRateDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlFireShovelFull, new Action[] { Noop, () => new FireShovelfullCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlBlowdownValve, new Action[] { Noop, () => new ToggleBlowdownValveCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlCylinderCocks, new Action[] { Noop, () => new ToggleCylinderCocksCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlCylinderCompound, new Action[] { Noop, () => new ToggleCylinderCompoundCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlSmallEjectorIncrease, new Action[] { () => SteamLocomotive.StopSmallEjectorIncrease(), () => SteamLocomotive.StartSmallEjectorIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlSmallEjectorDecrease, new Action[] { () => SteamLocomotive.StopSmallEjectorDecrease(), () => SteamLocomotive.StartSmallEjectorDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlLargeEjectorIncrease, new Action[] { () => SteamLocomotive.StopLargeEjectorIncrease(), () => SteamLocomotive.StartLargeEjectorIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlLargeEjectorDecrease, new Action[] { () => SteamLocomotive.StopLargeEjectorDecrease(), () => SteamLocomotive.StartLargeEjectorDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlSteamBoosterAirValve, new Action[] { Noop, () => new ToggleSteamBoosterAirCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlSteamBoosterIdleValve, new Action[] { Noop, () => new ToggleSteamBoosterIdleCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlSteamBoosterLatch, new Action[] { Noop, () => new ToggleSteamBoosterLatchCommand(Viewer.Log) });
            base.InitializeUserInputCommands();
        }

        /// <summary>
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            // Keeping separated, since it is not a real engine control. (Probably wrong classification?)
            if (UserInput.IsPressed(UserCommand.ControlFiring)) new ToggleManualFiringCommand(Viewer.Log);

            // Keeping separated, since it is not a real engine control. (Probably wrong classification?)
            if (UserInput.IsPressed(UserCommand.ControlAIFireOn)) new AIFireOnCommand(Viewer.Log);

            // Keeping separated, since it is not a real engine control. (Probably wrong classification?)
            if (UserInput.IsPressed(UserCommand.ControlAIFireOff)) new AIFireOffCommand(Viewer.Log);

            // Keeping separated, since it is not a real engine control. (Probably wrong classification?)
            if (UserInput.IsPressed(UserCommand.ControlAIFireReset)) new AIFireResetCommand(Viewer.Log);

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
            base.PrepareFrame(frame, elapsedTime);
        }

        /// <summary>
        /// Iterate through the list of particle emitters to update output and color
        /// </summary>
        public override void UpdateParticleEmitters()
        {
            foreach (ParticleEmitterViewer drawer in ParticleDrawers)
                drawer.UpdateOutput(this);
        }
    }
}
