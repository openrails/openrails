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
using Orts.Common;
using Orts.Simulation;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D.RollingStock
{
    public class MSTSSteamLocomotiveViewer : MSTSLocomotiveViewer
    {
        float Color_Value;

        MSTSSteamLocomotive SteamLocomotive { get { return (MSTSSteamLocomotive)Car; } }
        List<ParticleEmitterViewer> Cylinders = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders2 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders11 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders12 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders21 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders22 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders31 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders32 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders41 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders42 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> CylinderSteamExhaust1 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> CylinderSteamExhaust2 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> CylinderSteamExhaust3 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> CylinderSteamExhaust4 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Blowdown = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Drainpipe = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Injectors1 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Injectors2 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Compressor = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Generator = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> SafetyValves = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Stack = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Whistle = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> SmallEjector = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> LargeEjector = new List<ParticleEmitterViewer>();

        public MSTSSteamLocomotiveViewer(Viewer viewer, MSTSSteamLocomotive car)
            : base(viewer, car)
        {
            // Now all the particle drawers have been setup, assign them textures based
            // on what emitters we know about.
            string steamTexture = viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\smokemain.ace";

            foreach (var emitter in ParticleDrawers)
            {
                if (emitter.Key.ToLowerInvariant() == "cylindersfx") // This parameter retained as legacy parameters only, ideally they should be removed eventually
                    Cylinders.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylinders2fx") // This parameter retained as legacy parameters only, ideally they should be removed eventually
                {
                    Cylinders2.AddRange(emitter.Value);
                    car.Cylinder2SteamEffects = true;
                }
                else if (emitter.Key.ToLowerInvariant() == "cylinders11fx")
                {
                    Cylinders11.AddRange(emitter.Value);
                    car.CylinderAdvancedSteamEffects = true;
                }
                else if (emitter.Key.ToLowerInvariant() == "cylinders12fx")
                    Cylinders12.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylinders21fx")
                    Cylinders21.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylinders22fx")
                    Cylinders22.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylinders31fx")
                    Cylinders31.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylinders32fx")
                    Cylinders32.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylinders41fx")
                    Cylinders41.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylinders42fx")
                    Cylinders42.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylindersteamexhaust1fx")
                {
                    CylinderSteamExhaust1.AddRange(emitter.Value);
                    car.CylinderAdvancedSteamExhaustEffects = true;
                }
                else if (emitter.Key.ToLowerInvariant() == "cylindersteamexhaust2fx")
                    CylinderSteamExhaust2.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylindersteamexhaust3fx")
                    CylinderSteamExhaust3.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "cylindersteamexhaust4fx")
                    CylinderSteamExhaust4.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "blowdownfx")
                    Blowdown.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "drainpipefx")        // Drainpipe was not used in MSTS, and has no control set up for it
                    Drainpipe.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "injectors1fx")
                    Injectors1.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "injectors2fx")
                    Injectors2.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "smallejectorfx")
                    SmallEjector.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "largeejectorfx")
                    LargeEjector.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "compressorfx")
                    Compressor.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "generatorfx")
                {
                    Generator.AddRange(emitter.Value);
                    car.GeneratorSteamEffects = true;
                }
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
            var car = Car as MSTSSteamLocomotive;

            car.StackCount = Stack.Count;

            foreach (var drawer in Cylinders)
                drawer.SetOutput(car.Cylinders1SteamVelocityMpS, car.Cylinders1SteamVolumeM3pS, car.Cylinder1ParticleDurationS);

             foreach (var drawer in Cylinders2)
                drawer.SetOutput(car.Cylinders2SteamVelocityMpS, car.Cylinders2SteamVolumeM3pS, car.Cylinder2ParticleDurationS);

            foreach (var drawer in Cylinders11)
                drawer.SetOutput(car.Cylinders1SteamVelocityMpS, car.Cylinders11SteamVolumeM3pS, car.Cylinder1ParticleDurationS);

            foreach (var drawer in Cylinders12)
                drawer.SetOutput(car.Cylinders2SteamVelocityMpS, car.Cylinders12SteamVolumeM3pS, car.Cylinder2ParticleDurationS);

            foreach (var drawer in Cylinders21)
                drawer.SetOutput(car.Cylinders1SteamVelocityMpS, car.Cylinders21SteamVolumeM3pS, car.Cylinder1ParticleDurationS);

            foreach (var drawer in Cylinders22)
                drawer.SetOutput(car.Cylinders2SteamVelocityMpS, car.Cylinders22SteamVolumeM3pS, car.Cylinder2ParticleDurationS);

            foreach (var drawer in Cylinders31)
                drawer.SetOutput(car.Cylinders1SteamVelocityMpS, car.Cylinders31SteamVolumeM3pS, car.Cylinder1ParticleDurationS);

            foreach (var drawer in Cylinders32)
                drawer.SetOutput(car.Cylinders2SteamVelocityMpS, car.Cylinders32SteamVolumeM3pS, car.Cylinder2ParticleDurationS);

            foreach (var drawer in Cylinders41)
                drawer.SetOutput(car.Cylinders1SteamVelocityMpS, car.Cylinders41SteamVolumeM3pS, car.Cylinder1ParticleDurationS);

            foreach (var drawer in Cylinders42)
                drawer.SetOutput(car.Cylinders2SteamVelocityMpS, car.Cylinders42SteamVolumeM3pS, car.Cylinder2ParticleDurationS);

            foreach (var drawer in CylinderSteamExhaust1)
                drawer.SetOutput(car.CylinderSteamExhaustSteamVelocityMpS, car.CylinderSteamExhaust1SteamVolumeM3pS, car.CylinderSteamExhaustParticleDurationS);

            foreach (var drawer in CylinderSteamExhaust2)
                drawer.SetOutput(car.CylinderSteamExhaustSteamVelocityMpS, car.CylinderSteamExhaust2SteamVolumeM3pS, car.CylinderSteamExhaustParticleDurationS);

            foreach (var drawer in CylinderSteamExhaust3)
                drawer.SetOutput(car.CylinderSteamExhaustSteamVelocityMpS, car.CylinderSteamExhaust3SteamVolumeM3pS, car.CylinderSteamExhaustParticleDurationS);

            foreach (var drawer in CylinderSteamExhaust4)
                drawer.SetOutput(car.CylinderSteamExhaustSteamVelocityMpS, car.CylinderSteamExhaust4SteamVolumeM3pS, car.CylinderSteamExhaustParticleDurationS);

            foreach (var drawer in Blowdown)
                drawer.SetOutput(car.BlowdownSteamVelocityMpS, car.BlowdownSteamVolumeM3pS, car.BlowdownParticleDurationS);
            
            // TODO: Drainpipe - Not used in either MSTS or OR - currently disabled by zero values set in SteamLocomotive file
             foreach (var drawer in Drainpipe)
                drawer.SetOutput(car.DrainpipeSteamVelocityMpS, car.DrainpipeSteamVolumeM3pS, car.DrainpipeParticleDurationS);

             foreach (var drawer in Injectors1)
                drawer.SetOutput(car.Injector1SteamVelocityMpS, car.Injector1SteamVolumeM3pS, car.Injector1ParticleDurationS);

             foreach (var drawer in Injectors2)
                 drawer.SetOutput(car.Injector2SteamVelocityMpS, car.Injector2SteamVolumeM3pS, car.Injector2ParticleDurationS);

            foreach (var drawer in SmallEjector)
                drawer.SetOutput(car.SmallEjectorSteamVelocityMpS, car.SmallEjectorSteamVolumeM3pS, car.SmallEjectorParticleDurationS);

            foreach (var drawer in LargeEjector)
                drawer.SetOutput(car.LargeEjectorSteamVelocityMpS, car.LargeEjectorSteamVolumeM3pS, car.LargeEjectorParticleDurationS);

            foreach (var drawer in Compressor)
                drawer.SetOutput(car.CompressorSteamVelocityMpS, car.CompressorSteamVolumeM3pS, car.CompressorParticleDurationS );

            foreach (var drawer in Generator)
                drawer.SetOutput(car.GeneratorSteamVelocityMpS, car.GeneratorSteamVolumeM3pS, car.GeneratorParticleDurationS);
            
            foreach (var drawer in SafetyValves)
                drawer.SetOutput(car.SafetyValvesSteamVelocityMpS, car.SafetyValvesSteamVolumeM3pS, car.SafetyValvesParticleDurationS);
            
            foreach (var drawer in Stack)
            {
                Color_Value = car.SmokeColor.SmoothedValue;
                drawer.SetOutput(car.StackSteamVelocityMpS.SmoothedValue, car.StackSteamVolumeM3pS, car.StackParticleDurationS, new Color(Color_Value, Color_Value, Color_Value));
            }

            foreach (var drawer in Whistle)
                drawer.SetOutput(car.WhistleSteamVelocityMpS, car.WhistleSteamVolumeM3pS, car.WhistleParticleDurationS);

            base.PrepareFrame(frame, elapsedTime);
        }
    }
}
