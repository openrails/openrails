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
        const float LBToKG = 0.45359237f;
        const float SteamVaporDensityAt100DegC1BarM3pKG = 1.694f;
        float Throttlepercent;
        float Burn_Rate;
        float Steam_Rate;
        float Color_Value;
        float Pulse_Rate = 1.0f;
        float pulse = 0.25f;
        float steamcolor = 1.0f;
        float old_Distance_Travelled = 0.0f;

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
                    drawer.Initialize(viewer.TextureManager.Get(steamTexture));
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
        /// A keyboard or mouse click has occured. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            // Note: UserInput.IsReleased( UserCommands.ControlReverserForward/Backwards ) are given here but
            // UserInput.IsPressed( UserCommands.ControlReverserForward/Backwards ) are handled in base class MSTSLocomotive.
            if (UserInput.IsReleased(UserCommands.ControlForwards))
            {
                SteamLocomotive.StopReverseIncrease();
                new ContinuousReverserCommand(Viewer.Log, true, SteamLocomotive.CutoffController.CurrentValue, SteamLocomotive.CutoffController.CommandStartTime);
            }
            else if (UserInput.IsReleased(UserCommands.ControlBackwards))
            {
                SteamLocomotive.StopReverseDecrease();
                new ContinuousReverserCommand(Viewer.Log, false, SteamLocomotive.CutoffController.CurrentValue, SteamLocomotive.CutoffController.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlInjector1Increase))
            {
                SteamLocomotive.StartInjector1Increase(null);
            }
            else if (UserInput.IsReleased(UserCommands.ControlInjector1Increase))
            {
                SteamLocomotive.StopInjector1Increase();
                new ContinuousInjectorCommand(Viewer.Log, 1, true, SteamLocomotive.Injector1Controller.CurrentValue, SteamLocomotive.Injector1Controller.CommandStartTime);
            }
            else if (UserInput.IsPressed(UserCommands.ControlInjector1Decrease))
                SteamLocomotive.StartInjector1Decrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlInjector1Decrease))
            {
                SteamLocomotive.StopInjector1Decrease();
                new ContinuousInjectorCommand(Viewer.Log, 1, false, SteamLocomotive.Injector1Controller.CurrentValue, SteamLocomotive.Injector1Controller.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlInjector1))
                new ToggleInjectorCommand(Viewer.Log, 1);

            if (UserInput.IsPressed(UserCommands.ControlInjector2Increase))
                SteamLocomotive.StartInjector2Increase(null);
            else if (UserInput.IsReleased(UserCommands.ControlInjector2Increase))
            {
                SteamLocomotive.StopInjector2Increase();
                new ContinuousInjectorCommand(Viewer.Log, 2, true, SteamLocomotive.Injector2Controller.CurrentValue, SteamLocomotive.Injector2Controller.CommandStartTime);
            }
            else if (UserInput.IsPressed(UserCommands.ControlInjector2Decrease))
                SteamLocomotive.StartInjector2Decrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlInjector2Decrease))
            {
                SteamLocomotive.StopInjector2Decrease();
                new ContinuousInjectorCommand(Viewer.Log, 2, false, SteamLocomotive.Injector2Controller.CurrentValue, SteamLocomotive.Injector2Controller.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlInjector2))
                new ToggleInjectorCommand(Viewer.Log, 2);

            if (UserInput.IsPressed(UserCommands.ControlBlowerIncrease))
                SteamLocomotive.StartBlowerIncrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlBlowerIncrease))
            {
                SteamLocomotive.StopBlowerIncrease();
                new ContinuousBlowerCommand(Viewer.Log, true, SteamLocomotive.BlowerController.CurrentValue, SteamLocomotive.BlowerController.CommandStartTime);
            }
            else if (UserInput.IsPressed(UserCommands.ControlBlowerDecrease))
                SteamLocomotive.StartBlowerDecrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlBlowerDecrease))
            {
                SteamLocomotive.StopBlowerDecrease();
                new ContinuousBlowerCommand(Viewer.Log, false, SteamLocomotive.BlowerController.CurrentValue, SteamLocomotive.BlowerController.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlDamperIncrease))
                SteamLocomotive.StartDamperIncrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlDamperIncrease))
            {
                SteamLocomotive.StopDamperIncrease();
                new ContinuousDamperCommand(Viewer.Log, true, SteamLocomotive.DamperController.CurrentValue, SteamLocomotive.DamperController.CommandStartTime);
            }
            else if (UserInput.IsPressed(UserCommands.ControlDamperDecrease))
                SteamLocomotive.StartDamperDecrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlDamperDecrease))
            {
                SteamLocomotive.StopDamperDecrease();
                new ContinuousDamperCommand(Viewer.Log, false, SteamLocomotive.DamperController.CurrentValue, SteamLocomotive.DamperController.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlFireboxOpen))
                SteamLocomotive.StartFireboxDoorIncrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlFireboxOpen))
            {
                SteamLocomotive.StopFireboxDoorIncrease();
                new ContinuousFireboxDoorCommand(Viewer.Log, true, SteamLocomotive.FireboxDoorController.CurrentValue, SteamLocomotive.FireboxDoorController.CommandStartTime);
            }
            else if (UserInput.IsPressed(UserCommands.ControlFireboxClose))
                SteamLocomotive.StartFireboxDoorDecrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlFireboxClose))
            {
                SteamLocomotive.StopFireboxDoorDecrease();
                new ContinuousFireboxDoorCommand(Viewer.Log, false, SteamLocomotive.FireboxDoorController.CurrentValue, SteamLocomotive.FireboxDoorController.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlFiringRateIncrease))
                SteamLocomotive.StartFiringRateIncrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlFiringRateIncrease))
            {
                SteamLocomotive.StopFiringRateIncrease();
                new ContinuousFiringRateCommand(Viewer.Log, true, SteamLocomotive.FiringRateController.CurrentValue, SteamLocomotive.FiringRateController.CommandStartTime);
            }
            else if (UserInput.IsPressed(UserCommands.ControlFiringRateDecrease))
                SteamLocomotive.StartFiringRateDecrease(null);
            else if (UserInput.IsReleased(UserCommands.ControlFiringRateDecrease))
            {
                SteamLocomotive.StopFiringRateDecrease();
                new ContinuousFiringRateCommand(Viewer.Log, false, SteamLocomotive.FiringRateController.CurrentValue, SteamLocomotive.FiringRateController.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlFireShovelFull))
                new FireShovelfullCommand(Viewer.Log);
            if (UserInput.IsPressed(UserCommands.ControlCylinderCocks))
                new ToggleCylinderCocksCommand(Viewer.Log);
            if (UserInput.IsPressed(UserCommands.ControlFiring))
                new ToggleManualFiringCommand(Viewer.Log);

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
            var steamUsageLBpS = car.CylinderSteamUsageLBpS + car.BlowerSteamUsageLBpS + car.BasicSteamUsageLBpS + (car.SafetyIsOn ? car.SafetyValveUsageLBpS : 0);
            var cockSteamUsageLBps = car.CylCockSteamUsageLBpS;
            var safetySteamUsageLBps = car.SafetyValveUsageLBpS;
            // TODO: Expected assignment:
            //var steamVolumeM3pS = Kg.FromLb(steamUsageLBpS) * SteamVaporDensityAt100DegC1BarM3pKG;
            var steamVolumeM3pS = Kg.FromLb(steamUsageLBpS) * SteamVaporDensityAt100DegC1BarM3pKG;
            var cocksVolumeM3pS = Kg.FromLb(cockSteamUsageLBps) * SteamVaporDensityAt100DegC1BarM3pKG;
            var safetyVolumeM3pS = Kg.FromLb(safetySteamUsageLBps) * SteamVaporDensityAt100DegC1BarM3pKG;

            foreach (var drawer in Cylinders)
                drawer.SetOutput(car.CylinderCocksAreOpen ? cocksVolumeM3pS : 0);

            foreach (var drawer in Drainpipe)
                drawer.SetOutput(0);

            foreach (var drawer in SafetyValves)
                drawer.SetOutput(car.SafetyIsOn ? safetyVolumeM3pS : 0);

            foreach (var drawer in Stack)
            {

                Throttlepercent = Math.Max(car.ThrottlePercent / 10f, 0f);

                Pulse_Rate = (MathHelper.Pi * SteamLocomotive.DriverWheelRadiusM);

                if (car.Direction == Direction.Forward)
                {
                    if (pulse == 0.25f)
                        if (Viewer.PlayerTrain.DistanceTravelledM > old_Distance_Travelled + (Pulse_Rate / 4))
                        {
                            pulse = 1.0f;
                        }
                    if (pulse == 1.0f)
                        if (Viewer.PlayerTrain.DistanceTravelledM > old_Distance_Travelled + Pulse_Rate)
                        {
                            pulse = 0.25f;
                            old_Distance_Travelled = Viewer.PlayerTrain.DistanceTravelledM;
                        }
                }
                if (car.Direction == Direction.Reverse)
                {
                    if (pulse == 0.25f)
                        if (Viewer.PlayerTrain.DistanceTravelledM < old_Distance_Travelled - (Pulse_Rate / 4))
                        {
                            pulse = 1.0f;
                        }
                    if (pulse == 1.0f)
                        if (Viewer.PlayerTrain.DistanceTravelledM < old_Distance_Travelled - Pulse_Rate)
                        {
                            pulse = 0.25f;
                            old_Distance_Travelled = Viewer.PlayerTrain.DistanceTravelledM;
                        }
                }
                Color_Value = (steamVolumeM3pS * .10f) + (car.Smoke.SmoothedValue / 2) / 256 * 100f;

                drawer.SetOutput((steamVolumeM3pS * pulse) + car.FireRatio, (Throttlepercent + car.FireRatio), (new Color(Color_Value, Color_Value, Color_Value)));

            }

            foreach (var drawer in Whistle)
                drawer.SetOutput(car.Horn ? 1 : 0);

            base.PrepareFrame(frame, elapsedTime);
        }

        /// <summary>
        /// This doesn't function yet.
        /// </summary>
        public override void Unload()
        {
            base.Unload();
        }
    }
}
