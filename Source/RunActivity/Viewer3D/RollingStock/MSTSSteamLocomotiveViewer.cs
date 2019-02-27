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
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using ORTS.Common;
using ORTS.Settings;
using System;
using System.Collections.Generic;

namespace Orts.Viewer3D.RollingStock
{
    public class MSTSSteamLocomotiveViewer : MSTSLocomotiveViewer
    {
        float Throttlepercent;
        float Color_Value;

        MSTSSteamLocomotive SteamLocomotive { get { return (MSTSSteamLocomotive)Car; } }
        List<ParticleEmitterViewer> Cylinders = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Cylinders2 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Drainpipe = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Injectors1 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Injectors2 = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Compressor = new List<ParticleEmitterViewer>();
        List<ParticleEmitterViewer> Generator = new List<ParticleEmitterViewer>();
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
                else if (emitter.Key.ToLowerInvariant() == "cylinders2fx")
                {
                    Cylinders2.AddRange(emitter.Value);
                    car.Cylinder2SteamEffects = true;
                }
//          Not used in either MSTS or OR
                else if (emitter.Key.ToLowerInvariant() == "drainpipefx")        // Drainpipe was not used in MSTS, and has no control
                    Drainpipe.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "injectors1fx")
                    Injectors1.AddRange(emitter.Value);
                else if (emitter.Key.ToLowerInvariant() == "injectors2fx")
                    Injectors2.AddRange(emitter.Value);
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
            UserInputCommands.Add(UserCommands.ControlTroughRefill, new Action[] { Noop, () => ToggleTroughRefill() });
            UserInputCommands.Add(UserCommands.ControlSmallEjectorIncrease, new Action[] { () => SteamLocomotive.StopSmallEjectorIncrease(), () => SteamLocomotive.StartSmallEjectorIncrease(null) });
            UserInputCommands.Add(UserCommands.ControlSmallEjectorDecrease, new Action[] { () => SteamLocomotive.StopSmallEjectorDecrease(), () => SteamLocomotive.StartSmallEjectorDecrease(null) });
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

            // Keeping separated, since it is not a real engine control. (Probably wrong classification?)
            if (UserInput.IsPressed(UserCommands.ControlAIFireOn)) new AIFireOnCommand(Viewer.Log);

            // Keeping separated, since it is not a real engine control. (Probably wrong classification?)
            if (UserInput.IsPressed(UserCommands.ControlAIFireOff)) new AIFireOffCommand(Viewer.Log);

            // Keeping separated, since it is not a real engine control. (Probably wrong classification?)
            if (UserInput.IsPressed(UserCommands.ControlAIFireReset)) new AIFireResetCommand(Viewer.Log);

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
        /// Switches between refill start and refill end
        /// </summary>
        protected void ToggleTroughRefill()
        {
            if (SteamLocomotive.RefillingFromTrough)
            {
               StopRefillingFromTrough(Viewer.Log);
            }
            else
            {
               AttemptToRefillFromTrough();
            }
        }

        /// <summary>
        /// Checks if on trough. If not, tell that the scoop is destroyed; else, starts refilling
        /// </summary>
        public void AttemptToRefillFromTrough()
        {
            if (!SteamLocomotive.HasWaterScoop)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.Warning, Viewer.Catalog.GetString("No scoop in this loco"));
                return;
            }
            if (SteamLocomotive.ScoopIsBroken)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.Error, Viewer.Catalog.GetString("Scoop is broken, can't refill"));
                return;
            }
            if (!SteamLocomotive.IsOverTrough())
            {
                // Bad thing, scoop gets broken!
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.Error, Viewer.Catalog.GetString("Scoop broken because activated outside trough"));
                return;
            }
            if (SteamLocomotive.Direction == Direction.Reverse)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Loco must be moving forward."));
                return;
            }
            if (SteamLocomotive.SpeedMpS < SteamLocomotive.WaterScoopMinSpeedMpS)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Loco speed must exceed {0}.",
                    FormatStrings.FormatSpeedLimit(SteamLocomotive.WaterScoopMinSpeedMpS, Viewer.MilepostUnitsMetric)));
                return;
            }
            if (SteamLocomotive.SpeedMpS > SteamLocomotive.ScoopMaxPickupSpeedMpS)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Loco speed must not exceed {0}.",
                    FormatStrings.FormatSpeedLimit(SteamLocomotive.ScoopMaxPickupSpeedMpS, Viewer.MilepostUnitsMetric)));
                return;
            }

            //            if (SteamLocomotive.SpeedMpS > SteamLocomotive.ScoopMaxPickupSpeedMpS)
            //            {
            //                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Loco speed must not exceed {0}.",
            //                    FormatStrings.FormatSpeedLimit(SteamLocomotive.ScoopMaxPickupSpeedMpS, Viewer.MilepostUnitsMetric)));
            //                return;
            //            }

            //TODO - causes damage to locomotive if over full????

            var fraction = SteamLocomotive.GetFilledFraction((uint)MSTSWagon.PickupType.FuelWater);
            if (fraction > 0.99)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: {0} supply now replenished.",
                    PickupTypeDictionary[(uint)MSTSWagon.PickupType.FuelWater]));
                return;
            }
            else
            {
                MSTSWagon.RefillProcess.OkToRefill = true;
                MSTSWagon.RefillProcess.ActivePickupObjectUID = -1;
                SteamLocomotive.RefillingFromTrough = true;
                SteamLocomotive.IsWaterScoopDown = true;
                SteamLocomotive.SignalEvent(Event.WaterScoopDown);
//                StartRefilling((uint)MSTSWagon.PickupType.FuelWater, fraction);
            }

        }

        /// <summary>
        /// Ends a continuous increase in controlled value.
        /// </summary>
        public void StopRefillingFromTrough(CommandLog log)
        {
            MSTSWagon.RefillProcess.OkToRefill = false;
            MSTSWagon.RefillProcess.ActivePickupObjectUID = 0;
            SteamLocomotive.RefillingFromTrough = false;
//            var controller = new MSTSNotchController();
//            controller = SteamLocomotive.GetRefillController((uint)MSTSWagon.PickupType.FuelWater);

//            new RefillCommand(log, controller.CurrentValue, controller.CommandStartTime);  // for Replay to use
//            controller.StopIncrease();
            SteamLocomotive.IsWaterScoopDown = false;
            SteamLocomotive.SignalEvent(Event.WaterScoopUp);
        }



        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var car = Car as MSTSSteamLocomotive;

            foreach (var drawer in Cylinders)
                drawer.SetOutput(car.Cylinders1SteamVelocityMpS, car.Cylinders1SteamVolumeM3pS, car.Cylinder1ParticleDurationS);

             foreach (var drawer in Cylinders2)
                drawer.SetOutput(car.Cylinders2SteamVelocityMpS, car.Cylinders2SteamVolumeM3pS, car.Cylinder2ParticleDurationS);

            // TODO: Not used in either MSTS or OR - currently disabled by zero values set in SteamLocomotive file
             foreach (var drawer in Drainpipe)
                drawer.SetOutput(car.DrainpipeSteamVelocityMpS, car.DrainpipeSteamVolumeM3pS, car.DrainpipeParticleDurationS);

             foreach (var drawer in Injectors1)
                drawer.SetOutput(car.Injector1SteamVelocityMpS, car.Injector1SteamVolumeM3pS, car.Injector1ParticleDurationS);

             foreach (var drawer in Injectors2)
                 drawer.SetOutput(car.Injector2SteamVelocityMpS, car.Injector2SteamVolumeM3pS, car.Injector2ParticleDurationS);

             foreach (var drawer in Compressor)
                drawer.SetOutput(car.CompressorSteamVelocityMpS, car.CompressorSteamVolumeM3pS, car.CompressorParticleDurationS );

            foreach (var drawer in Generator)
                drawer.SetOutput(car.GeneratorSteamVelocityMpS, car.GeneratorSteamVolumeM3pS, car.GeneratorParticleDurationS);
            
            foreach (var drawer in SafetyValves)
                drawer.SetOutput(car.SafetyValvesSteamVelocityMpS, car.SafetyValvesSteamVolumeM3pS, car.SafetyValvesParticleDurationS);

            Throttlepercent = car.ThrottlePercent > 0 ? car.ThrottlePercent / 10f : 0f;

            foreach (var drawer in Stack)
            {
                Color_Value = car.SmokeColor.SmoothedValue;
                drawer.SetOutput(car.StackSteamVelocityMpS.SmoothedValue, car.StackSteamVolumeM3pS / Stack.Count + car.FireRatio, Throttlepercent + car.FireRatio, new Color(Color_Value, Color_Value, Color_Value));
            }

            foreach (var drawer in Whistle)
                drawer.SetOutput(car.WhistleSteamVelocityMpS, car.WhistleSteamVolumeM3pS, car.WhistleParticleDurationS);

            base.PrepareFrame(frame, elapsedTime);
        }
    }
}
