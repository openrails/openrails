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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MSTS.Formats;
using ORTS.Common;
using ORTS.Settings;
using ORTS.Viewer3D.Popups;

namespace ORTS.Viewer3D.RollingStock
{
    public class MSTSLocomotiveViewer : MSTSWagonViewer
    {
        MSTSLocomotive Locomotive;

        protected Dictionary<string, List<ParticleEmitterViewer>> ParticleDrawers = new Dictionary<string, List<ParticleEmitterViewer>>();

        protected MSTSLocomotive MSTSLocomotive { get { return (MSTSLocomotive)Car; } }

        public bool _hasCabRenderer;
        public CabRenderer _CabRenderer;
		private ThreeDimentionCabViewer ThreeDimentionCabViewer = null;

        public MSTSLocomotiveViewer(Viewer viewer, MSTSLocomotive car)
            : base(viewer, car)
        {
            Locomotive = car;
            ParticleDrawers = (from effect in Locomotive.EffectData
                               select new KeyValuePair<string, List<ParticleEmitterViewer>>(effect.Key, new List<ParticleEmitterViewer>(from data in effect.Value
                                                                                                                                        select new ParticleEmitterViewer(viewer, data, car.WorldPosition)))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            //if (car.CVFFile != null && car.CVFFile.TwoDViews.Count > 0)
            //    _CabRenderer = new CabRenderer(viewer, Locomotive);

            string wagonFolderSlash = Path.GetDirectoryName(Locomotive.WagFilePath) + "\\";
            if (Locomotive.CabSoundFileName != null) LoadCarSound(wagonFolderSlash, Locomotive.CabSoundFileName);

            SoundSources.Add(new TrackSoundSource(MSTSWagon, Viewer));

            if (Locomotive.TrainControlSystem != null && Locomotive.TrainControlSystem.Sounds.Count > 0)
                foreach (var script in Locomotive.TrainControlSystem.Sounds.Keys)
                    Viewer.SoundProcess.AddSoundSource(script, new List<SoundSourceBase>() {
                        new SoundSource(Viewer, Locomotive, Locomotive.TrainControlSystem.Sounds[script])});
        }

        bool SwapControl()
        {
            if (Locomotive.HasCombThrottleTrainBrake)
                return true;
            else
                return false;
        }

        void StartThrottleIncrease()
        {
            if (!SwapControl()) // tests for CombThrottleTrainBreak
            {
                if (!Locomotive.HasCombCtrl && Locomotive.DynamicBrakePercent >= 0)
                {
                    Viewer.Simulator.Confirmer.Warning(CabControl.Throttle, CabSetting.Warn1);
                    return;
                }
                else
                {
                    if (Locomotive.StartThrottleIncrease())
                        new NotchedThrottleCommand(Viewer.Log, true);
                }
            }
            else
            {
                float trainBreakPercent = Locomotive.TrainBrakeController.CurrentValue * 100.0f;
                float throttlePercent = Locomotive.ThrottlePercent;

                //if (trainBreakPercent > 0)
                if (throttlePercent == 0 && trainBreakPercent > 0)
                {
                    Locomotive.StopThrottleIncrease();
                    Locomotive.StartTrainBrakeDecrease(null);
                }
            }
        }

        void StopThrottleIncrease()
        {
            if (!SwapControl()) // tests for CombThrottleTrainBreak
            {
                if (!Locomotive.HasCombCtrl && Locomotive.DynamicBrakePercent >= 0)
                {
                    Viewer.Simulator.Confirmer.Warning(CabControl.Throttle, CabSetting.Warn1);
                    return;
                }
                else
                    if (Locomotive.StopThrottleIncrease())
                        new ContinuousThrottleCommand(Viewer.Log, true, Locomotive.ThrottleController.CurrentValue, Locomotive.CommandStartTime);
            }
            else
            {
                float trainBreakPercent = Locomotive.TrainBrakeController.CurrentValue * 100.0f;
                float throttlePercent = Locomotive.ThrottlePercent;

                if (throttlePercent == 0 && trainBreakPercent > 0)
                {
                    Locomotive.StopTrainBrakeDecrease();
                }
                else
                {
                    Locomotive.StartThrottleIncrease();
                    Locomotive.StopThrottleIncrease();
                }
            }
        }

        void StartThrottleDecrease()
        {
            if (!SwapControl())
            {
                if (!Locomotive.HasCombCtrl && Locomotive.DynamicBrakePercent >= 0)
                {
                    Viewer.Simulator.Confirmer.Warning(CabControl.Throttle, CabSetting.Warn1);
                    return;
                }
                else
                {
                    if (Locomotive.StartThrottleDecrease())
                        new NotchedThrottleCommand(Viewer.Log, false);
                }
            }
            else
            {
                float trainBreakPercent = Locomotive.TrainBrakeController.CurrentValue * 100.0f;
                float throttlePercent = Locomotive.ThrottlePercent;

                if (throttlePercent == 0 && trainBreakPercent >= 0)
                {
                    Locomotive.StopThrottleDecrease();
                    Locomotive.StartTrainBrakeIncrease(null);
                }
                else
                {
                    Locomotive.StartThrottleDecrease();
                }
            }
        }

        void StopThrottleDecrease()
        {
            if (!SwapControl()) // tests for CombThrottleTrainBrea
            {
                if (!Locomotive.HasCombCtrl && Locomotive.DynamicBrakePercent >= 0)
                {
                    Viewer.Simulator.Confirmer.Warning(CabControl.Throttle, CabSetting.Warn1);
                    return;
                }
                else
                    if (Locomotive.StopThrottleDecrease())
                        new ContinuousThrottleCommand(Viewer.Log, false, Locomotive.ThrottleController.CurrentValue, Locomotive.CommandStartTime);
            }
            else
            {
                float trainBreakPercent = Locomotive.TrainBrakeController.CurrentValue * 100.0f;
                float throttlePercent = Locomotive.ThrottlePercent;

                if (throttlePercent == 0 && trainBreakPercent >= 0)
                    Locomotive.StopTrainBrakeIncrease();
            }
        }

        protected virtual void StartGearBoxIncrease()
        {
            if (Locomotive.GearBoxController != null)
                Locomotive.StartGearBoxIncrease();
        }

        protected virtual void StopGearBoxIncrease()
        {
            if (Locomotive.GearBoxController != null)
                Locomotive.StopGearBoxIncrease();
        }

        protected virtual void StartGearBoxDecrease()
        {
            if (Locomotive.GearBoxController != null)
                Locomotive.StartGearBoxDecrease();
        }

        protected virtual void StopGearBoxDecrease()
        {
            if (Locomotive.GearBoxController != null)
                Locomotive.StopGearBoxDecrease();
        }

        protected virtual void ReverserControlForwards()
        {
            if (Locomotive.Direction != Direction.Forward
            && (Locomotive.ThrottlePercent >= 1
            || Math.Abs(Locomotive.SpeedMpS) > 1))
            {
                Viewer.Simulator.Confirmer.Warning(CabControl.Reverser, CabSetting.Warn1);
                return;
            }
            new ReverserCommand(Viewer.Log, true);    // No harm in trying to engage Forward when already engaged.
        }

        protected virtual void ReverserControlBackwards()
        {
            if (Locomotive.Direction != Direction.Reverse
            && (Locomotive.ThrottlePercent >= 1
            || Math.Abs(Locomotive.SpeedMpS) > 1))
            {
                Viewer.Simulator.Confirmer.Warning(CabControl.Reverser, CabSetting.Warn1);
                return;
            }
            new ReverserCommand(Viewer.Log, false);    // No harm in trying to engage Reverse when already engaged.
        }

        /// <summary>
        /// A keyboard or mouse click has occurred. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(UserCommands.ControlForwards)) ReverserControlForwards();
            if (UserInput.IsPressed(UserCommands.ControlBackwards)) ReverserControlBackwards();

            if (UserInput.IsPressed(UserCommands.ControlThrottleIncrease)) StartThrottleIncrease();
            if (UserInput.IsReleased(UserCommands.ControlThrottleIncrease)) StopThrottleIncrease();
            if (UserInput.IsPressed(UserCommands.ControlThrottleDecrease)) StartThrottleDecrease();
            if (UserInput.IsReleased(UserCommands.ControlThrottleDecrease)) StopThrottleDecrease();

            if (UserInput.IsPressed(UserCommands.ControlGearUp)) StartGearBoxIncrease();
            if (UserInput.IsReleased(UserCommands.ControlGearUp)) StopGearBoxIncrease();
            if (UserInput.IsPressed(UserCommands.ControlGearDown)) StartGearBoxDecrease();
            if (UserInput.IsReleased(UserCommands.ControlGearDown)) StopGearBoxDecrease();

            if (UserInput.IsPressed(UserCommands.ControlTrainBrakeIncrease))
            {
                Locomotive.StartTrainBrakeIncrease(null);
                Locomotive.TrainBrakeController.CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.ControlTrainBrakeIncrease))
            {
                Locomotive.StopTrainBrakeIncrease();
                new TrainBrakeCommand(Viewer.Log, true, Locomotive.TrainBrakeController.CurrentValue, Locomotive.TrainBrakeController.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlTrainBrakeDecrease))
            {
                Locomotive.StartTrainBrakeDecrease(null);
                Locomotive.TrainBrakeController.CommandStartTime = Viewer.Simulator.ClockTime;
            }
            if (UserInput.IsReleased(UserCommands.ControlTrainBrakeDecrease))
            {
                Locomotive.StopTrainBrakeDecrease();
                new TrainBrakeCommand(Viewer.Log, false, Locomotive.TrainBrakeController.CurrentValue, Locomotive.TrainBrakeController.CommandStartTime);
            }
            if (UserInput.IsPressed(UserCommands.ControlEngineBrakeIncrease)) Locomotive.StartEngineBrakeIncrease(null);
            if (UserInput.IsReleased(UserCommands.ControlEngineBrakeIncrease))
            {
                if (Locomotive.StopEngineBrakeIncrease())
                {
                    new EngineBrakeCommand(Viewer.Log, true, Locomotive.EngineBrakeController.CurrentValue, Locomotive.EngineBrakeController.CommandStartTime);
                }
            }
            if (UserInput.IsPressed(UserCommands.ControlEngineBrakeDecrease)) Locomotive.StartEngineBrakeDecrease(null);
            if (UserInput.IsReleased(UserCommands.ControlEngineBrakeDecrease))
            {
                if (Locomotive.StopEngineBrakeDecrease())
                {
                    new EngineBrakeCommand(Viewer.Log, false, Locomotive.EngineBrakeController.CurrentValue, Locomotive.EngineBrakeController.CommandStartTime);
                }
            }
            if (UserInput.IsPressed(UserCommands.ControlDynamicBrakeIncrease)) Locomotive.StartDynamicBrakeIncrease(null);
            if (UserInput.IsReleased(UserCommands.ControlDynamicBrakeIncrease))
                if (Locomotive.StopDynamicBrakeIncrease())
                    new DynamicBrakeCommand(Viewer.Log, true, Locomotive.DynamicBrakeController.CurrentValue, Locomotive.DynamicBrakeController.CommandStartTime);
            if (UserInput.IsPressed(UserCommands.ControlDynamicBrakeDecrease)) Locomotive.StartDynamicBrakeDecrease(null);
            if (UserInput.IsReleased(UserCommands.ControlDynamicBrakeDecrease))
                if (Locomotive.StopDynamicBrakeDecrease())
                    new DynamicBrakeCommand(Viewer.Log, false, Locomotive.DynamicBrakeController.CurrentValue, Locomotive.DynamicBrakeController.CommandStartTime);

            if (UserInput.IsPressed(UserCommands.ControlBailOff)) new BailOffCommand(Viewer.Log, true);
            if (UserInput.IsReleased(UserCommands.ControlBailOff)) new BailOffCommand(Viewer.Log, false);

            if (UserInput.IsPressed(UserCommands.ControlInitializeBrakes)) new InitializeBrakesCommand(Viewer.Log);
            if (UserInput.IsPressed(UserCommands.ControlHandbrakeNone)) new HandbrakeCommand(Viewer.Log, false);
            if (UserInput.IsPressed(UserCommands.ControlHandbrakeFull)) new HandbrakeCommand(Viewer.Log, true);
            if (UserInput.IsPressed(UserCommands.ControlRetainersOff)) new RetainersCommand(Viewer.Log, false);
            if (UserInput.IsPressed(UserCommands.ControlRetainersOn)) new RetainersCommand(Viewer.Log, true);
            if (UserInput.IsPressed(UserCommands.ControlBrakeHoseConnect)) new BrakeHoseConnectCommand(Viewer.Log, true);
            if (UserInput.IsPressed(UserCommands.ControlBrakeHoseDisconnect)) new BrakeHoseConnectCommand(Viewer.Log, false);
            if (UserInput.IsPressed(UserCommands.ControlEmergency)) new EmergencyBrakesCommand(Viewer.Log, true);
            if (UserInput.IsReleased(UserCommands.ControlEmergency)) new EmergencyBrakesCommand(Viewer.Log, false);

            // <CJComment> Some inputs calls their method directly, other via a SignalEvent. 
            // Probably because a signal can then be handled more than once, 
            // e.g. by every locomotive on the train or every car in the consist.
            // The signals are distributed through the parent class MSTSWagon:SignalEvent </CJComment>
            if (UserInput.IsPressed(UserCommands.ControlSander)) new SanderCommand(Viewer.Log, Locomotive.Sander);
            if (UserInput.IsPressed(UserCommands.ControlWiper)) new ToggleWipersCommand(Viewer.Log);
            if (UserInput.IsPressed(UserCommands.ControlHorn)) { new HornCommand(Viewer.Log, true); this.Car.Simulator.HazzardManager.Horn(); }
            if (UserInput.IsReleased(UserCommands.ControlHorn)) new HornCommand(Viewer.Log, false);
            if (UserInput.IsPressed(UserCommands.ControlBell)) new BellCommand(Viewer.Log, true);
            if (UserInput.IsReleased(UserCommands.ControlBell)) new BellCommand(Viewer.Log, false);
            if (UserInput.IsPressed(UserCommands.ControlBellToggle)) new BellCommand(Viewer.Log, !Locomotive.Bell);
            if (UserInput.IsPressed(UserCommands.ControlAlerter)) new AlerterCommand(Viewer.Log, true);  // z
            if (UserInput.IsReleased(UserCommands.ControlAlerter)) new AlerterCommand(Viewer.Log, false);  // z

            if (UserInput.IsPressed(UserCommands.ControlHeadlightDecrease)) new HeadlightCommand(Viewer.Log, false);
            if (UserInput.IsPressed(UserCommands.ControlHeadlightIncrease)) new HeadlightCommand(Viewer.Log, true);
#if !NEW_SIGNALLING
            if (UserInput.IsPressed(UserCommands.DebugForcePlayerAuthorization))
                Program.Simulator.AI.Dispatcher.ExtendPlayerAuthorization(true);
#endif

            // By GeorgeS
            if (UserInput.IsPressed(UserCommands.ControlLight))
            {
                if (Locomotive is MSTSSteamLocomotive)
                {       // By default, the "L" key is used for injector2 on steam locos
                    // do nothing
                }
                else
                {
                    new ToggleCabLightCommand(Viewer.Log);    // and cab lights on other locos.
                }
            }
            if (UserInput.IsPressed(UserCommands.CameraToggleShowCab))
                Locomotive.ShowCab = !Locomotive.ShowCab;

            // By Matej Pacha
            if (UserInput.IsPressed(UserCommands.DebugResetWheelSlip)) { Locomotive.Train.SignalEvent(Event._ResetWheelSlip); }
            if (UserInput.IsPressed(UserCommands.DebugToggleAdvancedAdhesion)) { Locomotive.Train.SignalEvent(Event._ResetWheelSlip); Locomotive.Simulator.UseAdvancedAdhesion = !Locomotive.Simulator.UseAdvancedAdhesion; }

            if (UserInput.RDState != null)
            {
                if (UserInput.RDState.BailOff)
                {
                    Locomotive.SetBailOff(true);
                }
                if (UserInput.RDState.Changed)
                {
                    Locomotive.AlerterReset();

                    Locomotive.SetThrottlePercent(UserInput.RDState.ThrottlePercent);
                    Locomotive.SetTrainBrakePercent(UserInput.RDState.TrainBrakePercent);
                    Locomotive.SetEngineBrakePercent(UserInput.RDState.EngineBrakePercent);
                    // Locomotive.SetDynamicBrakePercent(UserInput.RDState.DynamicBrakePercent);
                    if (!Locomotive.RDHasCombThrottleTrainBrake)
                        Locomotive.SetDynamicBrakePercent(UserInput.RDState.DynamicBrakePercent);
                    if (UserInput.RDState.DirectionPercent > 50)
                        Locomotive.SetDirection(Direction.Forward);
                    else if (UserInput.RDState.DirectionPercent < -50)
                        Locomotive.SetDirection(Direction.Reverse);
                    else
                        Locomotive.SetDirection(Direction.N);
                    if (UserInput.RDState.Emergency)
                    {
                        Locomotive.SetEmergency();
                    }
                    if (UserInput.RDState.Wipers == 1 && Locomotive.Wiper)
                        Locomotive.SignalEvent(Event.WiperOff);
                    if (UserInput.RDState.Wipers != 1 && !Locomotive.Wiper)
                        Locomotive.SignalEvent(Event.WiperOn);
                    // changing Headlight more than one step at a time doesn't work for some reason
                    if (Locomotive.Headlight < UserInput.RDState.Lights - 1)
                        Locomotive.Headlight++;
                    if (Locomotive.Headlight > UserInput.RDState.Lights - 1)
                        Locomotive.Headlight--;
                }
            }

            if (UserInput.IsPressed(UserCommands.ControlRefill)) AttemptToRefill();
            if (UserInput.IsReleased(UserCommands.ControlRefill))
                if (MatchedWagonAndPickup != null)
                    Locomotive.StopRefilling((uint)MatchedWagonAndPickup.Pickup.PickupType, Viewer.Log);
            base.HandleUserInput(elapsedTime);
        }

        /// <summary>
        /// We are about to display a video frame.  Calculate positions for 
        /// animated objects, and add their primitives to the RenderFrame list.
        /// </summary>
        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
			if (Viewer.Camera.AttachedCar == this.MSTSWagon && Viewer.Camera.Style == Camera.Styles.ThreeDimCab)
			{
				if (ThreeDimentionCabViewer != null)
					ThreeDimentionCabViewer.PrepareFrame(frame, elapsedTime);
				return;
			}

            foreach (List<ParticleEmitterViewer> drawers in ParticleDrawers.Values)
                foreach (ParticleEmitterViewer drawer in drawers)
                    drawer.PrepareFrame(frame, elapsedTime);

            // Wiper animation
            Wipers.UpdateLoop(Locomotive.Wiper, elapsedTime);

            // Draw 2D CAB View - by GeorgeS
            if (Viewer.Camera.AttachedCar == this.MSTSWagon &&
                Viewer.Camera.Style == ORTS.Viewer3D.Camera.Styles.Cab)
            {

                if (_CabRenderer != null)
                    _CabRenderer.PrepareFrame(frame);
            }

            base.PrepareFrame(frame, elapsedTime);
        }

        internal override void LoadForPlayer()
        {
            if (!_hasCabRenderer)
            {
                if (Locomotive.CabViewList.Count > 0)
                {
                    _hasCabRenderer = true;
                    if (Locomotive.CabViewList[(int)CabViewType.Front].CVFFile != null && Locomotive.CabViewList[(int)CabViewType.Front].CVFFile.TwoDViews.Count > 0)
                        _CabRenderer = new CabRenderer(Viewer, Locomotive);
                }
            }
			if (Locomotive.CabViewpoints != null)
			{
				ThreeDimentionCabViewer = new ThreeDimentionCabViewer(Viewer, this.Locomotive, this);
			}

        }

        internal override void Mark()
        {
            foreach (var pdl in ParticleDrawers.Values)
                foreach (var pd in pdl)
                    pd.Mark();
            if (_CabRenderer != null)
                _CabRenderer.Mark();
            base.Mark();
        }

        /// <summary>
        /// This doesn't function yet.
        /// </summary>
        public override void Unload()
        {
            base.Unload();
        }

        /// <summary>
        /// Finds the pickup point which is closest to the loco or tender that uses coal, water or diesel oil.
        /// Uses that pickup to refill the loco or tender.
        /// Not implemented yet:
        /// 1. allowing for the position of the intake on the wagon/loco.
        /// 2. allowing for the rate at with the pickup can supply.
        /// 3. refilling any but the first loco in the player's train.
        /// 4. refilling AI trains.
        /// 5. animation, e.g. of water columns.
        /// 6. currently ignores locos and tenders without intake points.
        /// Note that the activity class currently parses the initial level of diesel oil, coal and water
        /// but does not use it yet.
        /// </summary>
        #region Refill loco or tender from pickup points

        WagonAndMatchingPickup MatchedWagonAndPickup;

        /// <summary>
        /// Converts from enum to words for user messages.  
        /// </summary>
        public Dictionary<uint, string> PickupTypeDictionary = new Dictionary<uint, string>()
        {
            {(uint)MSTSLocomotive.PickupType.FuelWater, Viewer.Catalog.GetString("water")},
            {(uint)MSTSLocomotive.PickupType.FuelCoal, Viewer.Catalog.GetString("coal")},
            {(uint)MSTSLocomotive.PickupType.FuelDiesel, Viewer.Catalog.GetString("diesel oil")},
            {(uint)MSTSLocomotive.PickupType.FuelWood, Viewer.Catalog.GetString("wood")}
        };

        /// <summary>
        /// Holds data for an intake point on a wagon (e.g. tender) or loco and a pickup point which can supply that intake. 
        /// </summary>
        public class WagonAndMatchingPickup
        {
            public PickupObj Pickup;
            public MSTSWagon Wagon;
            public IntakePoint IntakePoint;
        }

        /// <summary>
        /// Scans the train's cars for intake points and the world files for pickup refilling points of the same type.
        /// (e.g. "fuelwater").
        /// TODO: Allow for position of intake point within the car. Currently all intake points are assumed to be at the back of the car.
        /// </summary>
        /// <param name="train"></param>
        /// <returns>a combination of intake point and pickup that are closest</returns>
        // <CJComment> Might be better in the MSTSLocomotive class, but can't see the World objects from there. </CJComment>
        WagonAndMatchingPickup GetMatchingPickup(Train train)
        {
            var worldFiles = Viewer.World.Scenery.WorldFiles;
            var shortestD2 = float.MaxValue;
            WagonAndMatchingPickup nearestPickup = null;
            float distanceFromFrontOfTrainM = 0f;
            foreach (var car in train.Cars)
            {
                if (car is MSTSWagon)
                {
                    MSTSWagon wagon = (MSTSWagon)car;
                    foreach (var intake in wagon.IntakePointList)
                    {
                        // TODO Use the value calculated below
                        //if (intake.DistanceFromFrontOfTrainM == null)
                        //{
                        //    intake.DistanceFromFrontOfTrainM = distanceFromFrontOfTrainM + (wagon.LengthM / 2) - intake.OffsetM;
                        //}
                        foreach (var worldFile in worldFiles)
                        {
                            foreach (var pickup in worldFile.PickupList)
                            {
                                if (pickup.Location == null)
                                    pickup.Location = new WorldLocation(
                                        worldFile.TileX, worldFile.TileZ,
                                        pickup.Position.X, pickup.Position.Y, pickup.Position.Z);
                                if (intake.Type == "fuelcoal" && pickup.PickupType == (uint)MSTSLocomotive.PickupType.FuelCoal
                                 || intake.Type == "fuelwater" && pickup.PickupType == (uint)MSTSLocomotive.PickupType.FuelWater
                                 || intake.Type == "fueldiesel" && pickup.PickupType == (uint)MSTSLocomotive.PickupType.FuelDiesel
                                 || intake.Type == "fuelwood" && pickup.PickupType == (uint)MSTSLocomotive.PickupType.FuelWood)
                                {
                                    var intakePosition = car.WorldPosition; //TODO Convert this into the position of the intake.

                                    var intakeLocation = new WorldLocation(
                                        car.WorldPosition.TileX, car.WorldPosition.TileZ,
                                        car.WorldPosition.Location.X, car.WorldPosition.Location.Y, car.WorldPosition.Location.Z);

                                    var d2 = WorldLocation.GetDistanceSquared(intakeLocation, pickup.Location);
                                    if (d2 < shortestD2)
                                    {
                                        shortestD2 = d2;
                                        nearestPickup = new WagonAndMatchingPickup();
                                        nearestPickup.Pickup = pickup;
                                        nearestPickup.Wagon = wagon;
                                        nearestPickup.IntakePoint = intake;
                                    }
                                }
                            }
                        }
                    }
                    distanceFromFrontOfTrainM += wagon.LengthM;
                }
            }
            return nearestPickup;
        }

        /// <summary>
        /// Returns 
        /// TODO Allow for position of intake point within the car. Currently all intake points are assumed to be at the back of the car.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        float GetDistanceToM(WagonAndMatchingPickup match)
        {
            var intakePosition = match.Wagon.WorldPosition; //TODO Convert this into the position of the intake.

            var intakeLocation = new WorldLocation(
                match.Wagon.WorldPosition.TileX, match.Wagon.WorldPosition.TileZ,
                match.Wagon.WorldPosition.Location.X, match.Wagon.WorldPosition.Location.Y, match.Wagon.WorldPosition.Location.Z);

            return (float)Math.Sqrt(WorldLocation.GetDistanceSquared(intakeLocation, match.Pickup.Location));
        }

        /// <summary>
        /// Prompts if cannot refill yet, else starts continuous refilling.
        /// Tries to find the nearest supply (pickup point) which can refill the locos and tenders in the train.  
        /// </summary>
        public void AttemptToRefill()
        {
            MatchedWagonAndPickup = null;   // Ensures that releasing the T key doesn't do anything unless there is something to do.

            var loco = this.Locomotive;

            // Electric locos do nothing.
            if (loco is MSTSElectricLocomotive) return;

            var match = GetMatchingPickup(loco.Train);
            if (match == null)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetString("Refill: No suitable pick-up point anywhere, so refilling immediately."));
                loco.RefillImmediately();
                return;
            }

            float distanceToPickupM = GetDistanceToM(match) - 1f; // Deduct an extra 1 meter as pickups are never on the centre line of the track.
            if (distanceToPickupM > match.IntakePoint.WidthM / 2)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Distance to {0} supply is {1}.",
                    PickupTypeDictionary[(uint)match.Pickup.PickupType], Viewer.Catalog.GetPluralStringFmt("{0} meter", "{0} meters", (long)distanceToPickupM)));
                return;
            }
            if (loco.SpeedMpS != 0 && match.Pickup.SpeedRange.MinMpS == 0f)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Loco must be stationary to refill {0}.",
                    PickupTypeDictionary[(uint)match.Pickup.PickupType]));
                return;
            }
            if (loco.SpeedMpS < match.Pickup.SpeedRange.MinMpS)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Loco speed must exceed {0}.", 
                    FormatStrings.FormatSpeedLimit(match.Pickup.SpeedRange.MinMpS, Viewer.MilepostUnitsMetric)));
                return;
            }
            if (loco.SpeedMpS > match.Pickup.SpeedRange.MinMpS)
            {
                var speedLimitMpH = MpS.ToMpH(match.Pickup.SpeedRange.MaxMpS);
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Loco speed must not exceed {0}.", 
                    FormatStrings.FormatSpeedLimit(match.Pickup.SpeedRange.MaxMpS, Viewer.MilepostUnitsMetric)));
                return;
            }
            float fraction = loco.GetFilledFraction(match.Pickup.PickupType);
            if (fraction > 0.99)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: {0} supply now replenished.",
                    PickupTypeDictionary[(uint)match.Pickup.PickupType]));
                return;
            }
            Locomotive.StartRefilling((uint)match.Pickup.PickupType);
            MatchedWagonAndPickup = match;  // Save away for HandleUserInput() to use when key is released.
        }

        /// <summary>
        /// Called by RefillCommand during replay.
        /// </summary>
        public void RefillChangeTo(float? target)
        {
            var matchedWagonAndPickup = GetMatchingPickup(Locomotive.Train);   // Save away for RefillCommand to use.
            if (matchedWagonAndPickup != null)
            {
                var controller = Locomotive.GetRefillController((uint)matchedWagonAndPickup.Pickup.PickupType);
                controller.StartIncrease(target);
            }
        }
        #endregion
    } // Class LocomotiveViewer

    // By GeorgeS
    /// <summary>
    /// Manages all CAB View textures - light conditions and texture parts
    /// </summary>
    public static class CABTextureManager
    {
        private static Dictionary<string, Texture2D> DayTextures = new Dictionary<string, Texture2D>();
        private static Dictionary<string, Texture2D> NightTextures = new Dictionary<string, Texture2D>();
        private static Dictionary<string, Texture2D> LightTextures = new Dictionary<string, Texture2D>();
        private static Dictionary<string, Texture2D[]> PDayTextures = new Dictionary<string, Texture2D[]>();
        private static Dictionary<string, Texture2D[]> PNightTextures = new Dictionary<string, Texture2D[]>();
        private static Dictionary<string, Texture2D[]> PLightTextures = new Dictionary<string, Texture2D[]>();

        /// <summary>
        /// Loads a texture, day night and cablight
        /// </summary>
        /// <param name="viewer">Viver3D</param>
        /// <param name="FileName">Name of the Texture</param>
        public static void LoadTextures(Viewer viewer, string FileName)
        {
            if (string.IsNullOrEmpty(FileName))
                return;

            if (DayTextures.Keys.Contains(FileName))
                return;

            DayTextures.Add(FileName, viewer.TextureManager.Get(FileName));

            var nightpath = Path.Combine(Path.Combine(Path.GetDirectoryName(FileName), "night"), Path.GetFileName(FileName));
            NightTextures.Add(FileName, viewer.TextureManager.Get(nightpath));

            var lightpath = Path.Combine(Path.Combine(Path.GetDirectoryName(FileName), "cablight"), Path.GetFileName(FileName));
            LightTextures.Add(FileName, viewer.TextureManager.Get(lightpath));
        }

        static Texture2D[] Disassemble(GraphicsDevice graphicsDevice, Texture2D texture, Point controlSize, int frameCount, Point frameGrid, string fileName)
        {
            if (frameGrid.X < 1 || frameGrid.Y < 1 || frameCount < 1)
            {
                Trace.TraceWarning("Cab control has invalid frame data {1}*{2}={3} (no frames will be shown) for {0}", fileName, frameGrid.X, frameGrid.Y, frameCount);
                return new Texture2D[0];
            }

            var frameSize = new Point(texture.Width / frameGrid.X, texture.Height / frameGrid.Y);
            var frames = new Texture2D[frameCount];
            var frameIndex = 0;

            if (controlSize.X < frameSize.X || controlSize.Y < frameSize.Y)
            {
                //some may mess up the dimension, will try to reverse
                if (frameGrid.X != 1 && frameGrid.Y != 1)
                {
                    Trace.TraceWarning("Cab control size {1}x{2} is smaller than frame size {3}x{4} (frames may be cut-off) for {0}\nOR will try to reverse the dimension (may not work), better to change the CVF file accordingly.", fileName, controlSize.X, controlSize.Y, frameSize.X, frameSize.Y);
                    var tmp = frameGrid.X; frameGrid.X = frameGrid.Y; frameGrid.Y = tmp;
                    frameSize = new Point(texture.Width / frameGrid.X, texture.Height / frameGrid.Y);
                }
                else Trace.TraceWarning("Cab control size {1}x{2} is smaller than frame size {3}x{4} (frames may be cut-off) for {0}.", fileName, controlSize.X, controlSize.Y, frameSize.X, frameSize.Y);

            }
            if (frameCount > frameGrid.X * frameGrid.Y)
                Trace.TraceWarning("Cab control frame count {1} is larger than the number of frames {2}*{3}={4} (some frames will be blank) for {0}", fileName, frameCount, frameGrid.X, frameGrid.Y, frameGrid.X * frameGrid.Y);

            if (texture.Format != SurfaceFormat.Color && texture.Format != SurfaceFormat.Dxt1)
            {
                Trace.TraceWarning("Cab control texture {0} has unsupported format {1}; only Color and Dxt1 are supported.", fileName, texture.Format);
            }
            else
            {
                var copySize = new Point(Math.Min(controlSize.X, frameSize.X), Math.Min(controlSize.Y, frameSize.Y));
                if (texture.Format == SurfaceFormat.Dxt1)
                {
                    controlSize.X = (int)Math.Ceiling((float)controlSize.X / 4) * 4;
                    controlSize.Y = (int)Math.Ceiling((float)controlSize.Y / 4) * 4;
                    var buffer = new byte[(int)Math.Ceiling((float)copySize.X / 4) * 4 * (int)Math.Ceiling((float)copySize.Y / 4) * 4 / 2];
                    frameIndex = DisassembleFrames(graphicsDevice, texture, frameCount, frameGrid, frames, frameSize, copySize, controlSize, buffer);
                }
                else
                {
                    var buffer = new Color[copySize.X * copySize.Y];
                    frameIndex = DisassembleFrames(graphicsDevice, texture, frameCount, frameGrid, frames, frameSize, copySize, controlSize, buffer);
                }
            }

            while (frameIndex < frameCount)
                frames[frameIndex++] = SharedMaterialManager.MissingTexture;

            return frames;
        }

        static int DisassembleFrames<T>(GraphicsDevice graphicsDevice, Texture2D texture, int frameCount, Point frameGrid, Texture2D[] frames, Point frameSize, Point copySize, Point controlSize, T[] buffer) where T : struct
        {
            //Trace.TraceInformation("Disassembling {0} {1} frames in {2}x{3}; control {4}x{5}, frame {6}x{7}, copy {8}x{9}.", texture.Format, frameCount, frameGrid.X, frameGrid.Y, controlSize.X, controlSize.Y, frameSize.X, frameSize.Y, copySize.X, copySize.Y);
            var frameIndex = 0;
            for (var y = 0; y < frameGrid.Y; y++)
            {
                for (var x = 0; x < frameGrid.X; x++)
                {
                    if (frameIndex < frameCount)
                    {
                        texture.GetData(0, new Rectangle(x * frameSize.X, y * frameSize.Y, copySize.X, copySize.Y), buffer, 0, buffer.Length);
                        var frame = frames[frameIndex++] = new Texture2D(graphicsDevice, controlSize.X, controlSize.Y, 1, TextureUsage.None, texture.Format);
                        frame.SetData(0, new Rectangle(0, 0, copySize.X, copySize.Y), buffer, 0, buffer.Length, SetDataOptions.None);
                    }
                }
            }
            return frameIndex;
        }

        /// <summary>
        /// Disassembles all compound textures into parts
        /// </summary>
        /// <param name="graphicsDevice">The GraphicsDevice</param>
        /// <param name="fileName">Name of the Texture to be disassembled</param>
        /// <param name="width">Width of the Cab View Control</param>
        /// <param name="height">Height of the Cab View Control</param>
        /// <param name="frameCount">Number of frames</param>
        /// <param name="framesX">Number of frames in the X dimension</param>
        /// <param name="framesY">Number of frames in the Y direction</param>
        public static void DisassembleTexture(GraphicsDevice graphicsDevice, string fileName, int width, int height, int frameCount, int framesX, int framesY)
        {
            var controlSize = new Point(width, height);
            var frameGrid = new Point(framesX, framesY);

            PDayTextures[fileName] = null;
            if (DayTextures.ContainsKey(fileName))
            {
                var texture = DayTextures[fileName];
                if (texture != SharedMaterialManager.MissingTexture)
                {
                    PDayTextures[fileName] = Disassemble(graphicsDevice, texture, controlSize, frameCount, frameGrid, fileName + ":day");
                }
            }

            PNightTextures[fileName] = null;
            if (NightTextures.ContainsKey(fileName))
            {
                var texture = NightTextures[fileName];
                if (texture != SharedMaterialManager.MissingTexture)
                {
                    PNightTextures[fileName] = Disassemble(graphicsDevice, texture, controlSize, frameCount, frameGrid, fileName + ":night");
                }
            }

            PLightTextures[fileName] = null;
            if (LightTextures.ContainsKey(fileName))
            {
                var texture = LightTextures[fileName];
                if (texture != SharedMaterialManager.MissingTexture)
                {
                    PLightTextures[fileName] = Disassemble(graphicsDevice, texture, controlSize, frameCount, frameGrid, fileName + ":light");
                }
            }
        }

        /// <summary>
        /// Gets a Texture from the given array
        /// </summary>
        /// <param name="arr">Texture array</param>
        /// <param name="indx">Index</param>
        /// <param name="FileName">Name of the file to report</param>
        /// <returns>The given Texture</returns>
        private static Texture2D SafeGetAt(Texture2D[] arr, int indx, string FileName)
        {
            if (arr == null)
            {
                Trace.TraceWarning("Passed null Texture[] for accessing {0}", FileName);
                return SharedMaterialManager.MissingTexture;
            }

            if (arr.Length < 1)
            {
                Trace.TraceWarning("Disassembled texture invalid for {0}", FileName);
                return SharedMaterialManager.MissingTexture;
            }

            indx = (int)MathHelper.Clamp(indx, 0, arr.Length - 1);

            try
            {
                return arr[indx];
            }
            catch (IndexOutOfRangeException)
            {
                Trace.TraceWarning("Index {1} out of range for array length {2} while accessing texture for {0}", FileName, indx, arr.Length);
                return SharedMaterialManager.MissingTexture;
            }
        }

        /// <summary>
        /// Returns the compound part of a Texture previously disassembled
        /// </summary>
        /// <param name="FileName">Name of the disassembled Texture</param>
        /// <param name="indx">Index of the part</param>
        /// <param name="isDark">Is dark out there?</param>
        /// <param name="isLight">Is Cab Light on?</param>
        /// <param name="isNightTexture"></param>
        /// <returns>The Texture represented by its index</returns>
        public static Texture2D GetTextureByIndexes(string FileName, int indx, bool isDark, bool isLight, out bool isNightTexture)
        {
            Texture2D retval = SharedMaterialManager.MissingTexture;
            Texture2D[] tmp = null;

            isNightTexture = false;

            if (string.IsNullOrEmpty(FileName) || !PDayTextures.Keys.Contains(FileName))
                return SharedMaterialManager.MissingTexture;

            if (isDark)
            {
                if (isLight)
                {
                    //tmp = PLightTextures[FileName];
                    tmp = PDayTextures[FileName];
                    if (tmp != null)
                    {
                        retval = SafeGetAt(tmp, indx, FileName);
                        isNightTexture = false;
                    }
                }

                if (retval == SharedMaterialManager.MissingTexture)
                {
                    tmp = PNightTextures[FileName];
                    if (tmp != null)
                    {
                        retval = SafeGetAt(tmp, indx, FileName);
                        isNightTexture = true;
                    }
                }
            }

            if (retval == SharedMaterialManager.MissingTexture)
            {
                tmp = PDayTextures[FileName];
                if (tmp != null)
                {
                    retval = SafeGetAt(tmp, indx, FileName);
                    isNightTexture = false;
                }
            }
            return retval;
        }

        /// <summary>
        /// Returns a Texture by its name
        /// </summary>
        /// <param name="FileName">Name of the Texture</param>
        /// <param name="isDark">Is dark out there?</param>
        /// <param name="isLight">Is Cab Light on?</param>
        /// <param name="isNightTexture"></param>
        /// <returns>The Texture</returns>
        public static Texture2D GetTexture(string FileName, bool isDark, bool isLight, out bool isNightTexture)
        {
            Texture2D retval = SharedMaterialManager.MissingTexture;
            isNightTexture = false;

            if (string.IsNullOrEmpty(FileName) || !DayTextures.Keys.Contains(FileName))
                return retval;

            if (isDark)
            {
                if (isLight)
                {
                    //retval = LightTextures[FileName];
                    retval = DayTextures[FileName];
                    isNightTexture = false;
                }

                if (retval == SharedMaterialManager.MissingTexture)
                {
                    retval = NightTextures[FileName];
                    isNightTexture = true;
                }
            }

            if (retval == SharedMaterialManager.MissingTexture)
            {
                retval = DayTextures[FileName];
                isNightTexture = false;
            }

            return retval;
        }

        [CallOnThread("Loader")]
        public static void Mark(Viewer viewer)
        {
            foreach (var texture in DayTextures.Values)
                viewer.TextureManager.Mark(texture);
            foreach (var texture in NightTextures.Values)
                viewer.TextureManager.Mark(texture);
            foreach (var texture in LightTextures.Values)
                viewer.TextureManager.Mark(texture);
            foreach (var textureList in PDayTextures.Values)
                if (textureList != null)
                    foreach (var texture in textureList)
                        viewer.TextureManager.Mark(texture);
            foreach (var textureList in PNightTextures.Values)
                if (textureList != null)
                    foreach (var texture in textureList)
                        viewer.TextureManager.Mark(texture);
            foreach (var textureList in PLightTextures.Values)
                if (textureList != null)
                    foreach (var texture in textureList)
                        viewer.TextureManager.Mark(texture);
        }
    }

    public class CabRenderer : RenderPrimitive
    {
        private SpriteBatchMaterial _Sprite2DCabView;
        private Rectangle _CabRect = new Rectangle();
        private Matrix _Scale = Matrix.Identity;
        private Texture2D _CabTexture;
        private CabShader _Shader;

        private Point _PrevScreenSize;

        //private List<CabViewControls> CabViewControlsList = new List<CabViewControls>();
        private List<List<CabViewControlRenderer>> CabViewControlRenderersList = new List<List<CabViewControlRenderer>>();
        private Viewer _Viewer;
        private MSTSLocomotive _Locomotive;
        private int _Location;
        private bool _isNightTexture;
		public Dictionary<int, CabViewControlRenderer> ControlMap;

        [CallOnThread("Loader")]
        public CabRenderer(Viewer viewer, MSTSLocomotive car)
        {
            //Sequence = RenderPrimitiveSequence.CabView;
            _Sprite2DCabView = (SpriteBatchMaterial)viewer.MaterialManager.Load("SpriteBatch");
            _Viewer = viewer;
            _Locomotive = car;

            // _Viewer.DisplaySize intercepted to adjust cab view height
            Point DisplaySize = _Viewer.DisplaySize;
            DisplaySize.Y = _Viewer.CabHeightPixels;

            // Use same shader for both front-facing and rear-facing cabs.
            if (_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF != null)
            {
                _Shader = new CabShader(viewer.GraphicsDevice,
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF.Light1Position, DisplaySize),
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF.Light2Position, DisplaySize),
                    ExtendedCVF.TranslatedColor(_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF.Light1Color),
                    ExtendedCVF.TranslatedColor(_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF.Light2Color));
            }

            _PrevScreenSize = DisplaySize;

			#region Create Control renderers
			ControlMap = new Dictionary<int, CabViewControlRenderer>();
			int[] count = new int[256];//enough to hold all types, count the occurence of each type
			var i = 0;
			foreach (var cabView in car.CabViewList)
			{
				if (cabView.CVFFile != null)
				{
					// Loading ACE files, skip displaying ERROR messages
					foreach (var cabfile in cabView.CVFFile.TwoDViews)
					{
						CABTextureManager.LoadTextures(viewer, cabfile);
					}

					CabViewControlRenderersList.Add(new List<CabViewControlRenderer>());
					var controlSortIndex = 1;  // Controls are drawn atop the cabview and in order they appear in the CVF file.
					// This allows the segments of moving-scale meters to be hidden by covers (e.g. TGV-A)
					CabViewControlRenderersList.Add(new List<CabViewControlRenderer>());
					foreach (CabViewControl cvc in cabView.CVFFile.CabViewControls)
					{
						controlSortIndex++;
						int key = 1000 * (int)cvc.ControlType + count[(int)cvc.ControlType];
						CVCDial dial = cvc as CVCDial;
						if (dial != null)
						{
							CabViewDialRenderer cvcr = new CabViewDialRenderer(viewer, car, dial, _Shader);
							cvcr.SortIndex = controlSortIndex;
							CabViewControlRenderersList[i].Add(cvcr);
							if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvcr);
							count[(int)cvc.ControlType]++;
							continue;
						}
						CVCGauge gauge = cvc as CVCGauge;
						if (gauge != null)
						{
							CabViewGaugeRenderer cvgr = new CabViewGaugeRenderer(viewer, car, gauge, _Shader);
							cvgr.SortIndex = controlSortIndex;
							CabViewControlRenderersList[i].Add(cvgr);
							if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvgr);
							count[(int)cvc.ControlType]++;
							continue;
						}
						CVCSignal asp = cvc as CVCSignal;
						if (asp != null)
						{
							CabViewDiscreteRenderer aspr = new CabViewDiscreteRenderer(viewer, car, asp, _Shader);
							aspr.SortIndex = controlSortIndex;
							CabViewControlRenderersList[i].Add(aspr);
							if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, aspr);
							count[(int)cvc.ControlType]++;
							continue;
						}
						CVCMultiStateDisplay multi = cvc as CVCMultiStateDisplay;
						if (multi != null)
						{
							CabViewDiscreteRenderer mspr = new CabViewDiscreteRenderer(viewer, car, multi, _Shader);
							mspr.SortIndex = controlSortIndex;
							CabViewControlRenderersList[i].Add(mspr);
							if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, mspr);
							count[(int)cvc.ControlType]++;
							continue;
						}
						CVCDiscrete disc = cvc as CVCDiscrete;
						if (disc != null)
						{
							CabViewDiscreteRenderer cvdr = new CabViewDiscreteRenderer(viewer, car, disc, _Shader);
							cvdr.SortIndex = controlSortIndex;
							CabViewControlRenderersList[i].Add(cvdr);
							if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvdr);
							count[(int)cvc.ControlType]++;
							continue;
						}
						CVCDigital digital = cvc as CVCDigital;
						if (digital != null)
						{
							CabViewDigitalRenderer cvdr = new CabViewDigitalRenderer(viewer, car, digital, _Shader);
							cvdr.SortIndex = controlSortIndex;
							CabViewControlRenderersList[i].Add(cvdr);
							if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvdr);
							count[(int)cvc.ControlType]++;
							continue;
						}
					}
				}
				i++;
			}
			#endregion
		}

        public void PrepareFrame(RenderFrame frame)
        {
            if (!_Locomotive.ShowCab)
                return;

            bool Dark = _Viewer.MaterialManager.sunDirection.Y <= 0f || _Viewer.Camera.IsUnderground;
            bool CabLight = _Locomotive.CabLightOn;

            CabCamera cbc = _Viewer.Camera as CabCamera;
            if (cbc != null)
            {
                _Location = cbc.SideLocation;
            }
            else
            {
                _Location = 0;
            }

            var i = (_Locomotive.UsingRearCab) ? 1 : 0;
            _CabTexture = CABTextureManager.GetTexture(_Locomotive.CabViewList[i].CVFFile.TwoDViews[_Location], Dark, CabLight, out _isNightTexture);
            if (_CabTexture == SharedMaterialManager.MissingTexture)
                return;

            // Cab view height adjusted to allow for clip or stretch
            _CabRect.Width = _Viewer.DisplaySize.X;
            _CabRect.Height = _Viewer.CabHeightPixels;

            if (_PrevScreenSize != _Viewer.DisplaySize && _Shader != null)
            {
                _PrevScreenSize = _Viewer.DisplaySize;
                _Shader.SetLightPositions(
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[i].ExtendedCVF.Light1Position, _Viewer.DisplaySize),
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[i].ExtendedCVF.Light2Position, _Viewer.DisplaySize));
            }

            frame.AddPrimitive(_Sprite2DCabView, this, RenderPrimitiveGroup.Cab, ref _Scale);
            //frame.AddPrimitive(Materials.SpriteBatchMaterial, this, RenderPrimitiveGroup.Cab, ref _Scale);

            if (_Location == 0)
                foreach (var cvcr in CabViewControlRenderersList[i])
                    cvcr.PrepareFrame(frame);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // Cab view vertical position adjusted to allow for clip or stretch.
            Rectangle stretchedCab;
            if (_Viewer.Simulator.CarVibrating > 0 || _Viewer.Simulator.UseSuperElevation > 0 || _Locomotive.Train.tilted)
            {
                if (_CabTexture != null)
                    stretchedCab = new Rectangle(-50, -40, _CabTexture.Width + 100, _CabTexture.Height + 80);
                else stretchedCab = new Rectangle(_CabRect.Left, _CabRect.Top + _Viewer.CabYOffsetPixels, _CabRect.Width, _CabRect.Height);
            }
            else
                stretchedCab = new Rectangle(_CabRect.Left, _CabRect.Top + _Viewer.CabYOffsetPixels, _CabRect.Width, _CabRect.Height);

            if (_Location == 0 && _Shader != null)
            {
                if (_Viewer.Settings.UseMSTSEnv == false)
                    _Shader.SetData(_Viewer.MaterialManager.sunDirection,
                        _isNightTexture, _Locomotive.CabLightOn, _Viewer.World.Sky.overcastFactor);
                else
                    _Shader.SetData(_Viewer.MaterialManager.sunDirection,
                _isNightTexture, _Locomotive.CabLightOn, _Viewer.World.MSTSSky.mstsskyovercastFactor);

                _Shader.SetTextureData(stretchedCab.Left, stretchedCab.Top, stretchedCab.Width, stretchedCab.Height);
                _Shader.Begin();
                _Shader.CurrentTechnique.Passes[0].Begin();
            }

            if (_CabTexture != null)
            {
                if (this._Viewer.Simulator.UseSuperElevation > 0 || _Viewer.Simulator.CarVibrating > 0 || _Locomotive.Train.tilted)
                {
                    var scale = new Vector2((float)_CabRect.Width / _CabTexture.Width, (float)_CabRect.Height / _CabTexture.Height);
                    var place = new Vector2(_CabRect.Width / 2 - 50 * scale.X, _CabRect.Height / 2 + _Viewer.CabYOffsetPixels - 40 * scale.Y);
                    var place2 = new Vector2(_CabTexture.Width / 2, _CabTexture.Height / 2);
                    _Sprite2DCabView.SpriteBatch.Draw(_CabTexture, place, stretchedCab, Color.White, _Locomotive.CabRotationZ, place2, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    _Sprite2DCabView.SpriteBatch.Draw(_CabTexture, stretchedCab, Color.White);
                }
            }
            //Materials.SpriteBatchMaterial.SpriteBatch.Draw(_CabTexture, _CabRect, Color.White);

            if (_Location == 0 && _Shader != null)
            {
                _Shader.CurrentTechnique.Passes[0].End();
                _Shader.End();
            }
        }

        internal void Mark()
        {
            _Viewer.TextureManager.Mark(_CabTexture);

            var i = (_Locomotive.UsingRearCab) ? 1 : 0;
            foreach (var cvcr in CabViewControlRenderersList[i])
                cvcr.Mark();
        }
    }

    /// <summary>
    /// Base class for rendering Cab Controls
    /// </summary>
    public abstract class CabViewControlRenderer : RenderPrimitive
    {
        protected readonly Viewer Viewer;
        protected readonly MSTSLocomotive Locomotive;
        protected readonly CabViewControl Control;
        protected readonly CabShader Shader;
        protected readonly SpriteBatchMaterial ControlView;

        protected Vector2 Position;
        protected Texture2D Texture;
        protected bool IsNightTexture;

        Matrix Matrix = Matrix.Identity;

        public CabViewControlRenderer(Viewer viewer, MSTSLocomotive locomotive, CabViewControl control, CabShader shader)
        {
            Viewer = viewer;
            Locomotive = locomotive;
            Control = control;
            Shader = shader;

            ControlView = (SpriteBatchMaterial)viewer.MaterialManager.Load("SpriteBatch");

            CABTextureManager.LoadTextures(Viewer, Control.ACEFile);
        }

        /// <summary>
        /// Gets the requested Locomotive data and returns it as a fraction (from 0 to 1) of the range between Min and Max values.
        /// </summary>
        /// <returns>Data value as fraction (from 0 to 1) of the range between Min and Max values</returns>
        public float GetRangeFraction()
        {
            var data = Locomotive.GetDataOf(Control);
            if (data < Control.MinValue)
                return 0;
            if (data > Control.MaxValue)
                return 1;

            if (Control.MaxValue == Control.MinValue)
                return 0;

            return (float)((data - Control.MinValue) / (Control.MaxValue - Control.MinValue));
        }

        [CallOnThread("Updater")]
        public virtual void PrepareFrame(RenderFrame frame)
        {
            frame.AddPrimitive(ControlView, this, RenderPrimitiveGroup.Cab, ref Matrix);
        }

        internal void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
        }
    }

    /// <summary>
    /// Dial Cab Control Renderer
    /// Problems with aspect ratio
    /// </summary>
    public class CabViewDialRenderer : CabViewControlRenderer
    {
        readonly CVCDial ControlDial;
        /// <summary>
        /// Rotation center point, in unscaled texture coordinates
        /// </summary>
        readonly Vector2 Origin;
        /// <summary>
        /// Scale factor. Only downscaling is allowed by MSTS, so the value is in 0-1 range
        /// </summary>
        readonly float Scale = 1;
        /// <summary>
        /// 0° is 12 o'clock, 90° is 3 o'clock
        /// </summary>
        float Rotation;
        float ScaleToScreen = 1;

        public CabViewDialRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCDial control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            ControlDial = control;

            Texture = CABTextureManager.GetTexture(Control.ACEFile, false, false, out IsNightTexture);
            if (ControlDial.Height < Texture.Height)
                Scale = (float)(ControlDial.Height / Texture.Height);
            Origin = new Vector2((float)Texture.Width / 2, ControlDial.Center / Scale);
        }

        public override void PrepareFrame(RenderFrame frame)
        {
            var dark = Viewer.MaterialManager.sunDirection.Y <= 0f || Viewer.Camera.IsUnderground;

            Texture = CABTextureManager.GetTexture(Control.ACEFile, dark, Locomotive.CabLightOn, out IsNightTexture);
            if (Texture == SharedMaterialManager.MissingTexture)
                return;

            base.PrepareFrame(frame);

            // Cab view height and vertical position adjusted to allow for clip or stretch.
            Position.X = (float)Viewer.DisplaySize.X / 640 * ((float)Control.PositionX + Origin.X * Scale);
            Position.Y = (float)Viewer.CabHeightPixels / 480 * ((float)Control.PositionY + Origin.Y * Scale) + Viewer.CabYOffsetPixels;
            ScaleToScreen = (float)Viewer.DisplaySize.X / 640 * Scale;

            var rangeFraction = GetRangeFraction();
            var direction = ControlDial.Direction == 0 ? 1 : -1;
            var rangeDegrees = direction * (ControlDial.ToDegree - ControlDial.FromDegree);
            while (rangeDegrees < 0)
                rangeDegrees += 360;
            Rotation = MathHelper.WrapAngle(MathHelper.ToRadians(ControlDial.FromDegree + direction * rangeDegrees * rangeFraction));
            if (Viewer.Simulator.UseSuperElevation > 0 || Viewer.Simulator.CarVibrating > 0 || Locomotive.Train.tilted)
            {
                Position.X -= Viewer.DisplaySize.X / 2; Position.Y -= (Viewer.CabHeightPixels / 2 + Viewer.CabYOffsetPixels);
                Position = Vector2.Transform(Position, Matrix.CreateRotationZ(Locomotive.CabRotationZ));
                Position.X += Viewer.DisplaySize.X / 2; Position.Y += (Viewer.CabHeightPixels / 2 + Viewer.CabYOffsetPixels);
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (Shader != null)
            {
                Shader.SetTextureData(Position.X, Position.Y, Texture.Width * ScaleToScreen, Texture.Height * ScaleToScreen);
                Shader.Begin();
                Shader.CurrentTechnique.Passes[0].Begin();
            }
            if (Viewer.Simulator.UseSuperElevation > 0 || Viewer.Simulator.CarVibrating > 0 || Locomotive.Train.tilted)
                ControlView.SpriteBatch.Draw(Texture, Position, null, Color.White, Rotation + Locomotive.CabRotationZ, Origin, ScaleToScreen, SpriteEffects.None, 0);
            else
                ControlView.SpriteBatch.Draw(Texture, Position, null, Color.White, Rotation, Origin, ScaleToScreen, SpriteEffects.None, 0);
            if (Shader != null)
            {
                Shader.CurrentTechnique.Passes[0].End();
                Shader.End();
            }
        }
    }

    /// <summary>
    /// Gauge type renderer
    /// Supports pointer, liquid, solid
    /// Supports Orientation and Direction
    /// </summary>
    public class CabViewGaugeRenderer : CabViewControlRenderer
    {
        readonly CVCGauge Gauge;
        readonly Rectangle SourceRectangle;

        Rectangle DestinationRectangle = new Rectangle();
        bool LoadMeterPositive = true;
        Color DrawColor;
        bool IsFire;

        public CabViewGaugeRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCGauge control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            Gauge = control;
            if ((Control.ControlType == CABViewControlTypes.REVERSER_PLATE) || (Gauge.ControlStyle == CABViewControlStyles.POINTER))
            {
                DrawColor = Color.White;
                Texture = CABTextureManager.GetTexture(Control.ACEFile, false, Locomotive.CabLightOn, out IsNightTexture);
                SourceRectangle.Width = (int)Texture.Width;
                SourceRectangle.Height = (int)Texture.Height;
            }
            else
            {
                DrawColor = new Color(Gauge.PositiveColor.R, Gauge.PositiveColor.G, Gauge.PositiveColor.B);
                SourceRectangle = Gauge.Area;
            }
        }

        public CabViewGaugeRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCFirebox control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            Gauge = control;
            CABTextureManager.LoadTextures(Viewer, control.FireACEFile);
            Texture = CABTextureManager.GetTexture(control.FireACEFile, false, Locomotive.CabLightOn, out IsNightTexture);
            DrawColor = Color.White;
            SourceRectangle.Width = (int)Texture.Width;
            SourceRectangle.Height = (int)Texture.Height;
            IsFire = true;
        }

        public override void PrepareFrame(RenderFrame frame)
        {
            if (!(Gauge is CVCFirebox))
            {
                var dark = Viewer.MaterialManager.sunDirection.Y <= 0f || Viewer.Camera.IsUnderground;
                Texture = CABTextureManager.GetTexture(Control.ACEFile, dark, Locomotive.CabLightOn, out IsNightTexture);
            }
            if (Texture == SharedMaterialManager.MissingTexture)
                return;

            base.PrepareFrame(frame);

            // Cab view height adjusted to allow for clip or stretch.
            var xratio = (float)Viewer.DisplaySize.X / 640;
            var yratio = (float)Viewer.CabHeightPixels / 480;

            float percent, xpos, ypos, zeropos;

            percent = IsFire ? 1f : GetRangeFraction();
            LoadMeterPositive = percent >= 0;

            if (Gauge.Orientation == 0)  // gauge horizontal
            {
                ypos = (float)Gauge.Height;
                zeropos = (float)(Gauge.Width * -Control.MinValue / (Control.MaxValue - Control.MinValue));
                xpos = (float)Gauge.Width * percent;
            }
            else  // gauge vertical
            {
                xpos = (float)Gauge.Width;
                zeropos = (float)(Gauge.Height * -Control.MinValue / (Control.MaxValue - Control.MinValue));
                ypos = (float)Gauge.Height * percent;
            }

            if (Gauge.ControlStyle == CABViewControlStyles.SOLID || Gauge.ControlStyle == CABViewControlStyles.LIQUID)
            {
                if (Control.MinValue < 0)
                {
                    DestinationRectangle.X = (int)(xratio * (Control.PositionX + (zeropos < xpos ? zeropos : xpos)));
                    DestinationRectangle.Y = (int)(yratio * Control.PositionY) + Viewer.CabYOffsetPixels;
                    DestinationRectangle.Width = (int)(xratio * (xpos > zeropos ? xpos - zeropos : zeropos - xpos));
                    DestinationRectangle.Height = (int)(yratio * ypos);
                }
                else
                {
                    DestinationRectangle.X = (int)(xratio * Control.PositionX);
                    var topY = Control.PositionY;  // top of visible column. +ve Y is downwards
                    if (Gauge.Direction != 0)  // column grows from bottom or from right
                    {
                        DestinationRectangle.X = (int)(xratio * (Control.PositionX + Gauge.Width - xpos));
                        topY += Gauge.Height * (1 - percent);
                    }
                    // Cab view vertical position adjusted to allow for clip or stretch.
                    DestinationRectangle.Y = (int)(yratio * topY) + Viewer.CabYOffsetPixels;
                    DestinationRectangle.Width = (int)(xratio * xpos);
                    DestinationRectangle.Height = (int)(yratio * ypos);
                }
            }
            else // pointer gauge using texture
            {
                var topY = Control.PositionY;  // top of visible column. +ve Y is downwards
                if (Gauge.Orientation == 0) // gauge horizontal
                {
                    DestinationRectangle.X = (int)(xratio * (Control.PositionX - 0.5 * Gauge.Area.Width + xpos));
                    if (Gauge.Direction != 0)  // column grows from right
                        DestinationRectangle.X = (int)(xratio * (Control.PositionX + Gauge.Width - 0.5 * Gauge.Area.Width - xpos));
                }
                else // gauge vertical
                {
                    topY += ypos - 0.5 * Gauge.Area.Height;
                    DestinationRectangle.X = (int)(xratio * Control.PositionX);
                    if (Gauge.Direction != 0)  // column grows from bottom
                        topY += Gauge.Height - 2 * ypos;
                }
                // Cab view vertical position adjusted to allow for clip or stretch.
                DestinationRectangle.Y = (int)(yratio * topY) + Viewer.CabYOffsetPixels;
                DestinationRectangle.Width = (int)(xratio * Gauge.Area.Width);
                DestinationRectangle.Height = (int)(yratio * Gauge.Area.Height);

                // Adjust coal texture height, because it mustn't show up at the bottom of door (see Scotsman)
                // TODO: cut the texture at the bottom instead of stretching
                if (Gauge is CVCFirebox)
                    DestinationRectangle.Height = Math.Min(DestinationRectangle.Height, (int)(yratio * (Control.PositionY + 0.5 * Gauge.Area.Height)) - DestinationRectangle.Y);
            }
            if (Viewer.Simulator.UseSuperElevation > 0 || Viewer.Simulator.CarVibrating > 0 || Locomotive.Train.tilted)
            {
                var Position = new Vector2(DestinationRectangle.X - Viewer.DisplaySize.X / 2, DestinationRectangle.Y - Viewer.CabYOffsetPixels - Viewer.CabHeightPixels / 2);
                Position = Vector2.Transform(Position, Matrix.CreateRotationZ(Locomotive.CabRotationZ));
                Position.X += Viewer.DisplaySize.X / 2; Position.Y += (Viewer.CabHeightPixels / 2 + Viewer.CabYOffsetPixels);
                DestinationRectangle.X = (int)(Position.X + 0.5f); DestinationRectangle.Y = (int)(Position.Y + 0.5f);
            }
            if (Control.MinValue < 0 && Control.ControlType != CABViewControlTypes.REVERSER_PLATE && Gauge.ControlStyle != CABViewControlStyles.POINTER)
                DrawColor = LoadMeterPositive ? new Color(Gauge.PositiveColor.R, Gauge.PositiveColor.G, Gauge.PositiveColor.B) : new Color(Gauge.NegativeColor.R, Gauge.NegativeColor.G, Gauge.NegativeColor.B);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (Shader != null)
            {
                Shader.SetTextureData(DestinationRectangle.Left, DestinationRectangle.Top, DestinationRectangle.Width, DestinationRectangle.Height);
                Shader.Begin();
                Shader.CurrentTechnique.Passes[0].Begin();
            }
            if (Viewer.Simulator.UseSuperElevation > 0 || Viewer.Simulator.CarVibrating > 0 || Locomotive.Train.tilted)
            {
                ControlView.SpriteBatch.Draw(Texture, DestinationRectangle, SourceRectangle, DrawColor, Locomotive.CabRotationZ, Vector2.Zero, SpriteEffects.None, 0f);
            }
            else
                ControlView.SpriteBatch.Draw(Texture, DestinationRectangle, SourceRectangle, DrawColor);
            if (Shader != null)
            {
                Shader.CurrentTechnique.Passes[0].End();
                Shader.End();
            }
        }
    }

    /// <summary>
    /// Discrete renderer for Lever, Twostate, Tristate, Multistate, Signal
    /// </summary>
    public class CabViewDiscreteRenderer : CabViewControlRenderer
    {
        readonly CVCWithFrames ControlDiscrete;
        readonly Rectangle SourceRectangle;
        Vector2 DrawPosition = new Vector2();
        Rectangle DestinationRectangle = new Rectangle();

        public CabViewDiscreteRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCWithFrames control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            ControlDiscrete = control;
            CABTextureManager.DisassembleTexture(viewer.GraphicsDevice, Control.ACEFile, (int)Control.Width, (int)Control.Height, ControlDiscrete.FramesCount, ControlDiscrete.FramesX, ControlDiscrete.FramesY);
            SourceRectangle = new Rectangle(0, 0, (int)ControlDiscrete.Width, (int)ControlDiscrete.Height);
        }

        public override void PrepareFrame(RenderFrame frame)
        {
            var dark = Viewer.MaterialManager.sunDirection.Y <= 0f || Viewer.Camera.IsUnderground;

            Texture = CABTextureManager.GetTextureByIndexes(Control.ACEFile, GetDrawIndex(), dark, Locomotive.CabLightOn, out IsNightTexture);
            if (Texture == SharedMaterialManager.MissingTexture)
                return;

            base.PrepareFrame(frame);

            // Cab view height and vertical position adjusted to allow for clip or stretch.
            var xratio = (float)Viewer.DisplaySize.X / 640;
            var yratio = (float)Viewer.CabHeightPixels / 480;

            if (Viewer.Simulator.UseSuperElevation > 0 || Viewer.Simulator.CarVibrating > 0 || Locomotive.Train.tilted)
            {
                DestinationRectangle.X = (int)(xratio * Control.PositionX * 1.0001);
                DestinationRectangle.Y = (int)(yratio * Control.PositionY * 1.0001) + Viewer.CabYOffsetPixels;
                DestinationRectangle.Width = (int)(xratio * Control.Width);
                DestinationRectangle.Height = (int)(yratio * Control.Height);
                var Position = new Vector2(DestinationRectangle.X - Viewer.DisplaySize.X / 2, DestinationRectangle.Y - Viewer.CabHeightPixels / 2 - Viewer.CabYOffsetPixels);

                Position = Vector2.Transform(Position, Matrix.CreateRotationZ(Locomotive.CabRotationZ));
                Position.X += Viewer.DisplaySize.X / 2 + 0.5f; Position.Y += Viewer.CabHeightPixels / 2 + Viewer.CabYOffsetPixels + 0.5f;
                DestinationRectangle.X = (int)Position.X; DestinationRectangle.Y = (int)Position.Y;
                DrawPosition.X = Position.X; DrawPosition.Y = Position.Y;
            }
            else
            {
                DestinationRectangle.X = (int)(xratio * Control.PositionX * 1.0001);
                DestinationRectangle.Y = (int)(yratio * Control.PositionY * 1.0001) + Viewer.CabYOffsetPixels;
                DestinationRectangle.Width = (int)(xratio * Control.Width);
                DestinationRectangle.Height = (int)(yratio * Control.Height);
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (Shader != null)
            {
                Shader.SetTextureData(DestinationRectangle.Left, DestinationRectangle.Top, DestinationRectangle.Width, DestinationRectangle.Height);
                Shader.Begin();
                Shader.CurrentTechnique.Passes[0].Begin();
            }
            if (Viewer.Simulator.UseSuperElevation > 0 || Viewer.Simulator.CarVibrating > 0 || Locomotive.Train.tilted)
                ControlView.SpriteBatch.Draw(Texture, DrawPosition, SourceRectangle, Color.White, Locomotive.CabRotationZ, Vector2.Zero, new Vector2((float)Viewer.DisplaySize.X / 640,
                    (float)Viewer.CabHeightPixels / 480), SpriteEffects.None, 0f);
            else
                ControlView.SpriteBatch.Draw(Texture, DestinationRectangle, SourceRectangle, Color.White);
            if (Shader != null)
            {
                Shader.CurrentTechnique.Passes[0].End();
                Shader.End();
            }
        }

        /// <summary>
        /// Determines the index of the Texture to be drawn
        /// </summary>
        /// <returns>index of the Texture</returns>
        public int GetDrawIndex()
        {
            var data = Locomotive.GetDataOf(Control);
            var currentDynamicNotch = Locomotive.DynamicBrakeController != null ? Locomotive.DynamicBrakeController.CurrentNotch : 0;
            var dynamicNotchCount = Locomotive.DynamicBrakeController != null ? Locomotive.DynamicBrakeController.NotchCount() : 0;
            var dynamicBrakePercent = Locomotive.Train.MUDynamicBrakePercent;

            var index = 0;
            switch (ControlDiscrete.ControlType)
            {
                case CABViewControlTypes.ENGINE_BRAKE:
                case CABViewControlTypes.TRAIN_BRAKE:
                case CABViewControlTypes.REGULATOR:
                case CABViewControlTypes.CUTOFF:
                case CABViewControlTypes.BLOWER:
                case CABViewControlTypes.DAMPERS_FRONT:
                case CABViewControlTypes.WATER_INJECTOR1:
                case CABViewControlTypes.WATER_INJECTOR2:
                case CABViewControlTypes.FIREHOLE:
                    index = PercentToIndex(data);
                    break;
                case CABViewControlTypes.THROTTLE:
                case CABViewControlTypes.THROTTLE_DISPLAY:
                    if (Locomotive.ThrottleController.SmoothMax() == null)
                        index = Locomotive.ThrottleController.CurrentNotch;
                    else
                        index = PercentToIndex(data);
                    break;
                case CABViewControlTypes.FRICTION_BRAKING:
                    index = data > 0 ? 1 : 0;
                    break;
                case CABViewControlTypes.DYNAMIC_BRAKE:
                case CABViewControlTypes.DYNAMIC_BRAKE_DISPLAY:
                    if (Locomotive.DynamicBrakeController != null)
                    {
                        if (dynamicBrakePercent == -1)
                            break;
                        if (!Locomotive.HasSmoothStruc)
                            index = currentDynamicNotch;
                        else
                            index = PercentToIndex(dynamicBrakePercent);
                    }
                    else
                    {
                        index = PercentToIndex(dynamicBrakePercent);
                    }
                    break;
                case CABViewControlTypes.CPH_DISPLAY:
                case CABViewControlTypes.CP_HANDLE:
                    var currentThrottleNotch = Locomotive.ThrottleController.CurrentNotch;
                    var throttleNotchCount = Locomotive.ThrottleController.NotchCount();
                    if (dynamicBrakePercent < 0)
                    {
                        if (currentThrottleNotch == 0)
                            index = throttleNotchCount - 1;
                        else
                            index = (throttleNotchCount - 1) - currentThrottleNotch;
                    }
                    else // dynamic break enabled
                    {
                        if (!Locomotive.HasSmoothStruc)
                        {
                            index = (dynamicNotchCount - 1) + currentDynamicNotch;
                        }
                        else
                        {
                            // This section for dispaly is based on 3DTS smooth controls
                            // The # of discreet positons for the display is based on how
                            // MSTS displayed them, so a dummy emulation is supplied here.
                            index = DummyDynamicToIndex(dynamicBrakePercent) + 9;
                        }
                    } // End Dynamic != null
                    break;
                case CABViewControlTypes.ALERTER_DISPLAY:
                case CABViewControlTypes.RESET:
                case CABViewControlTypes.WIPERS:
                case CABViewControlTypes.HORN:
                case CABViewControlTypes.WHISTLE:
                case CABViewControlTypes.BELL:
                case CABViewControlTypes.SANDERS:
                case CABViewControlTypes.SANDING:
                case CABViewControlTypes.WHEELSLIP:
                case CABViewControlTypes.FRONT_HLIGHT:
                case CABViewControlTypes.PANTOGRAPH:
                case CABViewControlTypes.PANTOGRAPH2:
                case CABViewControlTypes.PANTOGRAPHS_4:
                case CABViewControlTypes.PANTOGRAPHS_4C:
                case CABViewControlTypes.PANTOGRAPHS_5:
                case CABViewControlTypes.PANTO_DISPLAY:
                case CABViewControlTypes.DIRECTION:
                case CABViewControlTypes.DIRECTION_DISPLAY:
                case CABViewControlTypes.ASPECT_DISPLAY:
                case CABViewControlTypes.GEARS:
                case CABViewControlTypes.OVERSPEED:
                case CABViewControlTypes.PENALTY_APP:
                case CABViewControlTypes.EMERGENCY_BRAKE:
                case CABViewControlTypes.DOORS_DISPLAY:
                case CABViewControlTypes.CYL_COCKS:
                case CABViewControlTypes.STEAM_INJ1:
                case CABViewControlTypes.STEAM_INJ2:
                case CABViewControlTypes.SMALL_EJECTOR:
                    index = (int)data;
                    break;
            }

            if (index >= ControlDiscrete.FramesCount) index = ControlDiscrete.FramesCount - 1;
            if (index < 0) index = 0;
            return index;
        }

        /// <summary>
        /// Translates a percent value to a display index
        /// </summary>
        /// <param name="percent">Percent to be translated</param>
        /// <returns>The calculated display index by the Control's Values</returns>
        int PercentToIndex(float percent)
        {
            var index = 0;

            if (percent > 1)
                percent /= 100f;

            percent = MathHelper.Clamp(percent, (float)ControlDiscrete.MinValue, (float)ControlDiscrete.MaxValue);

            if (ControlDiscrete.Values.Count > 1)
            {
                var val = ControlDiscrete.Values.Where(v => (float)v <= percent).Last();
                index = ControlDiscrete.Values.IndexOf(val);
            }
            else if (ControlDiscrete.MaxValue != ControlDiscrete.MinValue)
            {
                index = (int)(percent / (ControlDiscrete.MaxValue - ControlDiscrete.MinValue) * ControlDiscrete.FramesCount);
            }

            return index;
        }

        static readonly float[] IndexToPercent = new float[]
        {
            0.0f,
            .1111f,
            .2222f,
            .3333f,
            .4444f,
            .5555f,
            .6666f,
            .7777f,
            .8888f,
            1.0f
        };

        static int DummyDynamicToIndex(float percent)
        {
            var index = 0;

            if (percent > 1)
                percent /= 100f;

            for (var i = 0; i < 10; i++)
            {
                var value = IndexToPercent[i];
                if (percent >= value)
                {
                    index = i;
                    continue;
                }
                if (percent <= value)
                    break;
            }

            return index;
        }
    }

    /// <summary>
    /// Digital Cab Control renderer
    /// Uses fonts instead of graphic
    /// </summary>
	public class CabViewDigitalRenderer : CabViewControlRenderer
	{
		const float FontScale = 10f / 480;
		readonly LabelAlignment Alignment;
		string Format = "{0}";
		readonly string Format1 = "{0}";
		readonly string Format2 = "{0}";

		float Num;
		WindowTextFont DrawFont;
		Rectangle DrawPosition;
		string DrawText;
		Color DrawColor;

		[CallOnThread("Loader")]
		public CabViewDigitalRenderer(Viewer viewer, MSTSLocomotive car, CVCDigital digital, CabShader shader)
			: base(viewer, car, digital, shader)
		{
			Position.X = (float)Control.PositionX;
			Position.Y = (float)Control.PositionY;

			// Clock defaults to centered.
			if (Control.ControlType == CABViewControlTypes.CLOCK)
				Alignment = LabelAlignment.Center;
			Alignment = digital.Justification == 1 ? LabelAlignment.Center : digital.Justification == 2 ? LabelAlignment.Left : digital.Justification == 3 ? LabelAlignment.Right : Alignment;

			Format1 = "{0:0" + new String('0', digital.LeadingZeros) + (digital.Accuracy > 0 ? "." + new String('0', (int)digital.Accuracy) : "") + "}";
			Format2 = "{0:0" + new String('0', digital.LeadingZeros) + (digital.AccuracySwitch > 0 ? "." + new String('0', (int)(digital.Accuracy + 1)) : "") + "}";
		}

		public override void PrepareFrame(RenderFrame frame)
		{
			var digital = Control as CVCDigital;

			Num = Locomotive.GetDataOf(Control);
			if (Math.Abs(Num) < digital.AccuracySwitch)
				Format = Format2;
			else
				Format = Format1;
			DrawFont = Viewer.WindowManager.TextManager.Get("Courier New", Viewer.CabHeightPixels * FontScale, System.Drawing.FontStyle.Regular);
			DrawPosition.X = (int)(Position.X * Viewer.DisplaySize.X / 640);
			DrawPosition.Y = (int)((Position.Y + Control.Height / 2) * Viewer.CabHeightPixels / 480) - DrawFont.Height / 2 + Viewer.CabYOffsetPixels;
			DrawPosition.Width = (int)(Control.Width * Viewer.DisplaySize.X / 640);
			DrawPosition.Height = (int)(Control.Height * Viewer.DisplaySize.Y / 480);

			if (Viewer.Simulator.CarVibrating > 0 || Viewer.Simulator.UseSuperElevation > 0 || Locomotive.Train.tilted)
			{
				var position = new Vector2(DrawPosition.X - Viewer.DisplaySize.X / 2, DrawPosition.Y - Viewer.CabHeightPixels / 2 - Viewer.CabYOffsetPixels);
				position = Vector2.Transform(position, Matrix.CreateRotationZ(Locomotive.CabRotationZ));
				DrawPosition.X = (int)Math.Round(position.X + Viewer.DisplaySize.X / 2);
				DrawPosition.Y = (int)Math.Round(position.Y + Viewer.CabHeightPixels / 2 + Viewer.CabYOffsetPixels);
			}

			if (Control.ControlType == CABViewControlTypes.CLOCK)
			{
				// Clock is drawn specially.
				var clockSeconds = Locomotive.Simulator.ClockTime;
				var hour = (int)(clockSeconds / 3600) % 24;
				var minute = (int)(clockSeconds / 60) % 60;
				var seconds = (int)clockSeconds % 60;

				if (hour < 0)
					hour += 24;
				if (minute < 0)
					minute += 60;
				if (seconds < 0)
					seconds += 60;

				if (digital.ControlStyle == CABViewControlStyles._12HOUR)
				{
					hour %= 12;
					if (hour == 0)
						hour = 12;
				}
				DrawText = String.Format(digital.Accuracy > 0 ? "{0:D2}:{1:D2}:{2:D2}" : "{0:D2}:{1:D2}", hour, minute, seconds);
				DrawColor = new Color(digital.PositiveColor.R, digital.PositiveColor.G, digital.PositiveColor.B);
			}
			else if (Num < -2)
			{
				DrawText = String.Empty;
				DrawColor = Color.White;
			}
			else if (digital.OldValue != 0 && digital.OldValue > Num && digital.DecreaseColor.A != 0)
			{
				DrawText = String.Format(Format, Math.Abs(Num));
				DrawColor = new Color(digital.DecreaseColor.R, digital.DecreaseColor.G, digital.DecreaseColor.B, digital.DecreaseColor.A);
			}
			else if (Num < 0 && digital.NegativeColor.A != 0)
			{
				DrawText = String.Format(Format, Math.Abs(Num));
				DrawColor = new Color(digital.NegativeColor.R, digital.NegativeColor.G, digital.NegativeColor.B, digital.NegativeColor.A);
			}
			else if (digital.PositiveColor.A != 0)
			{
				DrawText = String.Format(Format, Num);
				DrawColor = new Color(digital.PositiveColor.R, digital.PositiveColor.G, digital.PositiveColor.B, digital.PositiveColor.A);
			}
			else
			{
				DrawText = String.Format(Format, Num);
				DrawColor = Color.White;
			}

			if (Control.ControlType == CABViewControlTypes.SPEEDOMETER)
			{
				// Speedometer is colored specially.
				DrawColor = new Color(digital.PositiveColor.R, digital.PositiveColor.G, digital.PositiveColor.B);
				if (Locomotive.Train != null && Locomotive.GetDataOf(Control) > MpS.FromMpS(Locomotive.Train.AllowedMaxSpeedMpS, Control.Units == CABViewControlUnits.KM_PER_HOUR))
					DrawColor = Color.Yellow;
			}

			base.PrepareFrame(frame);
		}

		public override void Draw(GraphicsDevice graphicsDevice)
		{
			if (Viewer.Simulator.CarVibrating > 0 || Viewer.Simulator.UseSuperElevation > 0 || Locomotive.Train.tilted)
				DrawFont.Draw(ControlView.SpriteBatch, DrawPosition, Point.Zero, DrawText, Alignment, DrawColor, Locomotive.CabRotationZ);
			else
				DrawFont.Draw(ControlView.SpriteBatch, DrawPosition, Point.Zero, DrawText, Alignment, DrawColor);
		}

		public string GetDigits(out Color DrawColor)
		{
			try
			{
				var digital = Control as CVCDigital;
				string displayedText = "";
				Num = Locomotive.GetDataOf(Control);
				if (Math.Abs(Num) < digital.AccuracySwitch)
					Format = Format2;
				else
					Format = Format1;

				if (Control.ControlType == CABViewControlTypes.CLOCK)
				{
					// Clock is drawn specially.
					var clockSeconds = Locomotive.Simulator.ClockTime;
					var hour = (int)(clockSeconds / 3600) % 24;
					var minute = (int)(clockSeconds / 60) % 60;
					var seconds = (int)clockSeconds % 60;

					if (hour < 0)
						hour += 24;
					if (minute < 0)
						minute += 60;
					if (seconds < 0)
						seconds += 60;

					if (digital.ControlStyle == CABViewControlStyles._12HOUR)
					{
						hour %= 12;
						if (hour == 0)
							hour = 12;
					}
					displayedText = String.Format(digital.Accuracy > 0 ? "{0:D2}:{1:D2}:{2:D2}" : "{0:D2}:{1:D2}", hour, minute, seconds);
					DrawColor = new Color(digital.PositiveColor.R, digital.PositiveColor.G, digital.PositiveColor.B);
				}
				else if (Num < -2)
				{
					displayedText = String.Empty;
					DrawColor = Color.White;
				}
				else if (digital.OldValue != 0 && digital.OldValue > Num && digital.DecreaseColor.A != 0)
				{
					displayedText = String.Format(Format, Math.Abs(Num));
					DrawColor = new Color(digital.DecreaseColor.R, digital.DecreaseColor.G, digital.DecreaseColor.B, digital.DecreaseColor.A);
				}
				else if (Num < 0 && digital.NegativeColor.A != 0)
				{
					displayedText = String.Format(Format, Math.Abs(Num));
					DrawColor = new Color(digital.NegativeColor.R, digital.NegativeColor.G, digital.NegativeColor.B, digital.NegativeColor.A);
				}
				else if (digital.PositiveColor.A != 0)
				{
					displayedText = String.Format(Format, Num);
					DrawColor = new Color(digital.PositiveColor.R, digital.PositiveColor.G, digital.PositiveColor.B, digital.PositiveColor.A);
				}
				else
				{
					displayedText = String.Format(Format, Num);
					DrawColor = Color.White;
				}

				if (Control.ControlType == CABViewControlTypes.SPEEDOMETER)
				{
					// Speedometer is colored specially.
					DrawColor = new Color(digital.PositiveColor.R, digital.PositiveColor.G, digital.PositiveColor.B);
					if (Locomotive.Train != null && Locomotive.GetDataOf(Control) > MpS.FromMpS(Locomotive.Train.AllowedMaxSpeedMpS, Control.Units == CABViewControlUnits.KM_PER_HOUR))
						DrawColor = Color.Yellow;
				}
				return displayedText;
			}
			catch (Exception error)
			{
				DrawColor = Color.Blue;
			}

			return "";
		}

	}

	/// <summary>
	/// ThreeDimentionCabViewer
	/// </summary>
	public class ThreeDimentionCabViewer : TrainCarViewer
	{
		MSTSLocomotive Locomotive;

		protected PoseableShape TrainCarShape = null;
		Dictionary<int, AnimatedPartMultiState> AnimateParts = null;
		Dictionary<int, DigitalDisplay> DigitParts = null;
		protected MSTSLocomotive MSTSLocomotive { get { return (MSTSLocomotive)Car; } }
		MSTSLocomotiveViewer LocoViewer;
		private SpriteBatchMaterial _Sprite2DCabView;
		WindowTextFont _Font;
		public ThreeDimentionCabViewer(Viewer viewer, MSTSLocomotive car, MSTSLocomotiveViewer locoViewer)
			: base(viewer, car)
		{
			Locomotive = car;
			_Sprite2DCabView = (SpriteBatchMaterial)viewer.MaterialManager.Load("SpriteBatch");
			_Font = viewer.WindowManager.TextManager.Get("Arial", 14, System.Drawing.FontStyle.Regular);
			LocoViewer = locoViewer;
			string wagonFolderSlash = Path.GetDirectoryName(car.WagFilePath) + @"\";
			string shapePath = wagonFolderSlash + car.InteriorShapeFileName;

			TrainCarShape = new PoseableShape(viewer, shapePath + '\0' + wagonFolderSlash, car.WorldPosition, ShapeFlags.ShadowCaster);

			//TrainCarShape = new PoseableShape(viewer, shapePath, car.WorldPosition, ShapeFlags.ShadowCaster);

			AnimateParts = new Dictionary<int, AnimatedPartMultiState>();
			DigitParts = new Dictionary<int, DigitalDisplay>();

			CABViewControlTypes type;
			// Find the animated parts
			if (TrainCarShape.SharedShape.Animations != null)
			{
				string matrixName = ""; string typeName = ""; AnimatedPartMultiState tmpPart = null;
				for (int iMatrix = 0; iMatrix < TrainCarShape.SharedShape.MatrixNames.Count; ++iMatrix)
				{
					matrixName = TrainCarShape.SharedShape.MatrixNames[iMatrix].ToUpper();
					//Name convention
					//TYPE:Order:Parameter-PartN
					//e.g. ASPECT_SIGNAL:0:0-1: first ASPECT_SIGNAL, parameter is 0, this component is part 1 of this cab control
					//     ASPECT_SIGNAL:0:0-2: first ASPECT_SIGNAL, parameter is 0, this component is part 2 of this cab control
					//     ASPECT_SIGNAL:1:0  second ASPECT_SIGNAL, parameter is 0, this component is the only one for this cab control
					typeName = matrixName.Split('-')[0]; //a part may have several sub-parts, like ASPECT_SIGNAL:0:0-1, ASPECT_SIGNAL:0:0-2
					type = CABViewControlTypes.NONE;
					tmpPart = null;
					try
					{
						int order; string parameter;
						//ASPECT_SIGNAL:0:0
						var tmp = typeName.Split(':');
						order = int.Parse(tmp[1]); parameter = tmp[2].Trim();

						type = (CABViewControlTypes)Enum.Parse(typeof(CABViewControlTypes), tmp[0].Trim(), true); //convert from string to enum

						int key = 1000 * (int)type + order;
						var style = locoViewer._CabRenderer.ControlMap[key];
						if (style is CabViewDigitalRenderer)//digits?
						{
							DigitParts.Add(key, new DigitalDisplay(viewer, TrainCarShape, iMatrix, int.Parse(parameter), locoViewer._CabRenderer.ControlMap[key]));
						}
						else//others
						{
							//if there is a part already, will insert this into it, otherwise, create a new
							if (!AnimateParts.ContainsKey(key))
							{
								tmpPart = new AnimatedPartMultiState(TrainCarShape, type, key);
								AnimateParts.Add(key, tmpPart);
							}
							else tmpPart = AnimateParts[key];
							tmpPart.AddMatrix(iMatrix); //tmpPart.SetPosition(false);
						}
					}
					catch { type = CABViewControlTypes.NONE; }
				}
			}
		}


		/// <summary>
		/// A keyboard or mouse click has occurred. Read the UserInput
		/// structure to determine what was pressed.
		/// </summary>
		public override void HandleUserInput(ElapsedTime elapsedTime)
		{
			bool KeyPressed = false;
			if (UserInput.IsDown(UserCommands.CameraPanDown)) KeyPressed = true;
			if (UserInput.IsDown(UserCommands.CameraPanUp)) KeyPressed = true;
			if (UserInput.IsDown(UserCommands.CameraPanLeft)) KeyPressed = true;
			if (UserInput.IsDown(UserCommands.CameraPanRight)) KeyPressed = true;
			if (KeyPressed == true)
			{
				//	foreach (var p in DigitParts) p.Value.RecomputeLocation();
			}
		}

		/// <summary>
		/// We are about to display a video frame.  Calculate positions for 
		/// animated objects, and add their primitives to the RenderFrame list.
		/// </summary>
		public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
		{
			float elapsedClockSeconds = elapsedTime.ClockSeconds;

			foreach (var p in AnimateParts)
			{
				p.Value.Update(this.LocoViewer, elapsedTime);
			}
			foreach (var p in DigitParts)
			{
				p.Value.PrepareFrame(frame, elapsedTime);
			}
			TrainCarShape.PrepareFrame(frame, elapsedTime);
		}

		internal override void LoadForPlayer()
		{
		}

		internal override void Mark()
		{
		}

		/// <summary>
		/// This doesn't function yet.
		/// </summary>
		public override void Unload()
		{
		}


	} // Class ThreeDimentionCabViewer

	public class DigitalDisplay
	{
		int X, Y;
		Viewer Viewer;
		private SpriteBatchMaterial _Sprite2DCabView;
		WindowTextFont _Font;
		PoseableShape TrainCarShape = null;
		TextPrimitive text = null;
		int digitPart;
		int height;
		Matrix xnaMatrix;
		Point coor = new Point(0, 0);
		CabViewDigitalRenderer CVFR;
		Color color;
		public DigitalDisplay(Viewer viewer, PoseableShape t, int d, int h, CabViewControlRenderer c)
		{
			TrainCarShape = t;
			Viewer = viewer;
			digitPart = d;
			height = h;
			CVFR = (CabViewDigitalRenderer)c;
			_Sprite2DCabView = (SpriteBatchMaterial)viewer.MaterialManager.Load("SpriteBatch");
			_Font = viewer.WindowManager.TextManager.Get("Arial", height, System.Drawing.FontStyle.Regular);
			X = Y = -1000;//indicating the digit is not in range
		}

		public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
		{
			RecomputeLocation();//not init, or out of range before, recompute
			if (X == -1000) return; //indicating not able to draw it (out of range or behind camera)
			text.Text = CVFR.GetDigits(out text.Color);
			frame.AddPrimitive(_Sprite2DCabView, text, RenderPrimitiveGroup.World, ref xnaMatrix);
		}

		public void RecomputeLocation()
		{
			Matrix mx = TrainCarShape.Location.XNAMatrix;
			mx.M41 += (TrainCarShape.Location.TileX - Viewer.Camera.TileX) * 2048;
			mx.M43 += (-TrainCarShape.Location.TileZ + Viewer.Camera.TileZ) * 2048;
			Matrix m = TrainCarShape.XNAMatrices[digitPart] * mx;

			//project 3D space to 2D (for the top of the line)
			Vector3 cameraVector = Viewer.GraphicsDevice.Viewport.Project(
				m.Translation,
				Viewer.Camera.XnaProjection, Viewer.Camera.XnaView, Matrix.Identity);

			if (cameraVector.Z > 1 || cameraVector.Z < 0)
			{
				X = Y = -1000;
				return; //out of range or behind the camera
			}
			X = (int)cameraVector.X; Y = (int)cameraVector.Y;
			coor.X = X; coor.Y = Y;
			if (text == null) text = new TextPrimitive(_Sprite2DCabView, coor, Color.Red, _Font);
			text.Position = coor;
		}
	}

	public class TextPrimitive : RenderPrimitive
	{
		public readonly SpriteBatchMaterial Material;
		public Point Position;
		public Color Color;
		public readonly WindowTextFont Font;
		public string Text;

		public TextPrimitive(SpriteBatchMaterial material, Point position, Color color, WindowTextFont font)
		{
			Material = material;
			Position = position;
			Color = color;
			Font = font;
		}

		public override void Draw(GraphicsDevice graphicsDevice)
		{
			Font.Draw(Material.SpriteBatch, Position, Text, Color);
		}
	}


	// This supports animation of Pantographs, Mirrors and Doors - any up/down on/off 2 state types
	// It is initialized with a list of indexes for the matrices related to this part
	// On Update( position ) it slowly moves the parts towards the specified position
	public class AnimatedPartMultiState : AnimatedPart
	{
		CABViewControlTypes Type;
		int Key;
		/// <summary>
		/// Construct with a link to the shape that contains the animated parts 
		/// </summary>
		public AnimatedPartMultiState(PoseableShape poseableShape, CABViewControlTypes t, int k)
			: base(poseableShape)
		{
			Type = t;
			Key = k;
		}

		/// <summary>
		/// Transition the part toward the specified state. 
		/// </summary>
		public void Update(MSTSLocomotiveViewer locoViewer, ElapsedTime elapsedTime)
		{
			if (MatrixIndexes.Count == 0 || !locoViewer._hasCabRenderer) return;

			CabViewControlRenderer cvfr;
			float index;
			try
			{
				cvfr = locoViewer._CabRenderer.ControlMap[Key];
				if (cvfr is CabViewDiscreteRenderer)
				{
					index = (cvfr as CabViewDiscreteRenderer).GetDrawIndex();
				}
				else index = cvfr.GetRangeFraction() * this.FrameCount;
			}
			catch { cvfr = null; index = 0; }
			if (cvfr == null) return;
			this.SetFrameClamp(index);
		}
	}
}
