// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Popups;
using ORTS.Common;
using ORTS.Scripting.Api;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Event = Orts.Common.Event;

namespace Orts.Viewer3D.RollingStock
{
    public class MSTSLocomotiveViewer : MSTSWagonViewer
    {
        MSTSLocomotive Locomotive;

        protected MSTSLocomotive MSTSLocomotive { get { return (MSTSLocomotive)Car; } }

        public bool _hasCabRenderer;
        public bool _has3DCabRenderer;
        public CabRenderer _CabRenderer;
        public ThreeDimentionCabViewer ThreeDimentionCabViewer = null;
        public CabRenderer ThreeDimentionCabRenderer = null; //allow user to have different setting of .cvf file under CABVIEW3D

        public static int DbfEvalEBPBstopped = 0;//Debrief eval
        public static int DbfEvalEBPBmoving = 0;//Debrief eval
        public bool lemergencybuttonpressed = false;

        public MSTSLocomotiveViewer(Viewer viewer, MSTSLocomotive car)
            : base(viewer, car)
        {
            Locomotive = car;

            string wagonFolderSlash = Path.GetDirectoryName(Locomotive.WagFilePath) + "\\";
            if (Locomotive.CabSoundFileName != null) LoadCarSound(wagonFolderSlash, Locomotive.CabSoundFileName);

            //Viewer.SoundProcess.AddSoundSource(this, new TrackSoundSource(MSTSWagon, Viewer));

            if (Locomotive.TrainControlSystem != null && Locomotive.TrainControlSystem.Sounds.Count > 0)
                foreach (var script in Locomotive.TrainControlSystem.Sounds.Keys)
                {
                    try
                    {
                        Viewer.SoundProcess.AddSoundSources(script, new List<SoundSourceBase>() {
                            new SoundSource(Viewer, Locomotive, Locomotive.TrainControlSystem.Sounds[script])});
                    }
                    catch (Exception error)
                    {
                        Trace.TraceInformation("File " + Locomotive.TrainControlSystem.Sounds[script] + " in script of locomotive of train " + Locomotive.Train.Name + " : " + error.Message);
                    }
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

        public override void InitializeUserInputCommands()
        {
            // Steam locomotives handle these differently, and might have set them already
            if (!UserInputCommands.ContainsKey(UserCommands.ControlForwards))
                UserInputCommands.Add(UserCommands.ControlForwards, new Action[] { Noop, () => ReverserControlForwards() });
            if (!UserInputCommands.ContainsKey(UserCommands.ControlBackwards))
                UserInputCommands.Add(UserCommands.ControlBackwards, new Action[] { Noop, () => ReverserControlBackwards() });

            UserInputCommands.Add(UserCommands.ControlThrottleIncrease, new Action[] { () => Locomotive.StopThrottleIncrease(), () => Locomotive.StartThrottleIncrease() });
            UserInputCommands.Add(UserCommands.ControlThrottleDecrease, new Action[] { () => Locomotive.StopThrottleDecrease(), () => Locomotive.StartThrottleDecrease() });
            UserInputCommands.Add(UserCommands.ControlThrottleZero, new Action[] { Noop, () => Locomotive.ThrottleToZero() });
            UserInputCommands.Add(UserCommands.ControlGearUp, new Action[] { () => StopGearBoxIncrease(), () => StartGearBoxIncrease() });
            UserInputCommands.Add(UserCommands.ControlGearDown, new Action[] { () => StopGearBoxDecrease(), () => StartGearBoxDecrease() });
            UserInputCommands.Add(UserCommands.ControlTrainBrakeIncrease, new Action[] { () => Locomotive.StopTrainBrakeIncrease(), () => Locomotive.StartTrainBrakeIncrease(null) });
            UserInputCommands.Add(UserCommands.ControlTrainBrakeDecrease, new Action[] { () => Locomotive.StopTrainBrakeDecrease(), () => Locomotive.StartTrainBrakeDecrease(null) });
            UserInputCommands.Add(UserCommands.ControlTrainBrakeZero, new Action[] { Noop, () => Locomotive.StartTrainBrakeDecrease(0, true) });
            UserInputCommands.Add(UserCommands.ControlEngineBrakeIncrease, new Action[] { () => Locomotive.StopEngineBrakeIncrease(), () => Locomotive.StartEngineBrakeIncrease(null) });
            UserInputCommands.Add(UserCommands.ControlEngineBrakeDecrease, new Action[] { () => Locomotive.StopEngineBrakeDecrease(), () => Locomotive.StartEngineBrakeDecrease(null) });
            UserInputCommands.Add(UserCommands.ControlDynamicBrakeIncrease, new Action[] { () => Locomotive.StopDynamicBrakeIncrease(), () => Locomotive.StartDynamicBrakeIncrease(null) });
            UserInputCommands.Add(UserCommands.ControlDynamicBrakeDecrease, new Action[] { () => Locomotive.StopDynamicBrakeDecrease(), () => Locomotive.StartDynamicBrakeDecrease(null) });
            UserInputCommands.Add(UserCommands.ControlSteamHeatIncrease, new Action[] { () => Locomotive.StopSteamHeatIncrease(), () => Locomotive.StartSteamHeatIncrease(null) });
            UserInputCommands.Add(UserCommands.ControlSteamHeatDecrease, new Action[] { () => Locomotive.StopSteamHeatDecrease(), () => Locomotive.StartSteamHeatDecrease(null) });
            UserInputCommands.Add(UserCommands.ControlBailOff, new Action[] { () => new BailOffCommand(Viewer.Log, false), () => new BailOffCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommands.ControlInitializeBrakes, new Action[] { Noop, () => new InitializeBrakesCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlHandbrakeNone, new Action[] { Noop, () => new HandbrakeCommand(Viewer.Log, false) });
            UserInputCommands.Add(UserCommands.ControlHandbrakeFull, new Action[] { Noop, () => new HandbrakeCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommands.ControlRetainersOff, new Action[] { Noop, () => new RetainersCommand(Viewer.Log, false) });
            UserInputCommands.Add(UserCommands.ControlRetainersOn, new Action[] { Noop, () => new RetainersCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommands.ControlBrakeHoseConnect, new Action[] { Noop, () => new BrakeHoseConnectCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommands.ControlBrakeHoseDisconnect, new Action[] { Noop, () => new BrakeHoseConnectCommand(Viewer.Log, false) });
            UserInputCommands.Add(UserCommands.ControlEmergencyPushButton, new Action[] { Noop, () => new EmergencyPushButtonCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlSander, new Action[] { () => new SanderCommand(Viewer.Log, false), () => new SanderCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommands.ControlSanderToggle, new Action[] { Noop, () => new SanderCommand(Viewer.Log, !Locomotive.Sander) });
            UserInputCommands.Add(UserCommands.ControlWiper, new Action[] { Noop, () => new WipersCommand(Viewer.Log, !Locomotive.Wiper) });
            UserInputCommands.Add(UserCommands.ControlHorn, new Action[] { () => new HornCommand(Viewer.Log, false), () => new HornCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommands.ControlBell, new Action[] { () => new BellCommand(Viewer.Log, false), () => new BellCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommands.ControlBellToggle, new Action[] { Noop, () => new BellCommand(Viewer.Log, !Locomotive.Bell) });
            UserInputCommands.Add(UserCommands.ControlAlerter, new Action[] { () => new AlerterCommand(Viewer.Log, false), () => new AlerterCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommands.ControlHeadlightIncrease, new Action[] { Noop, () => new HeadlightCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommands.ControlHeadlightDecrease, new Action[] { Noop, () => new HeadlightCommand(Viewer.Log, false) });
            UserInputCommands.Add(UserCommands.ControlLight, new Action[] { Noop, () => new ToggleCabLightCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlRefill, new Action[] { () => StopRefillingOrUnloading(Viewer.Log), () => AttemptToRefillOrUnload() });
            UserInputCommands.Add(UserCommands.ControlImmediateRefill, new Action[] { () => StopImmediateRefilling(Viewer.Log), () => ImmediateRefill() });
            UserInputCommands.Add(UserCommands.ControlOdoMeterShowHide, new Action[] { Noop, () => new ToggleOdometerCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlOdoMeterReset, new Action[] { Noop, () => new ResetOdometerCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlOdoMeterDirection, new Action[] { Noop, () => new ToggleOdometerDirectionCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommands.ControlCabRadio, new Action[] { Noop, () => new CabRadioCommand(Viewer.Log, !Locomotive.CabRadioOn) });
            UserInputCommands.Add(UserCommands.ControlDieselHelper, new Action[] { Noop, () => new ToggleHelpersEngineCommand(Viewer.Log) });
            base.InitializeUserInputCommands();
        }

        /// <summary>
        /// A keyboard or mouse click has occurred. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
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
                    if (Locomotive.CombinedControlType != MSTSLocomotive.CombinedControl.ThrottleAir)
                        Locomotive.SetDynamicBrakePercent(UserInput.RDState.DynamicBrakePercent);
                    if (UserInput.RDState.DirectionPercent > 50)
                        Locomotive.SetDirection(Direction.Forward);
                    else if (UserInput.RDState.DirectionPercent < -50)
                        Locomotive.SetDirection(Direction.Reverse);
                    else
                        Locomotive.SetDirection(Direction.N);
                    if (UserInput.RDState.Emergency)
                        Locomotive.SetEmergency(true);
                    else
                        Locomotive.SetEmergency(false);
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

            foreach (var command in UserInputCommands.Keys)
            {
                if (UserInput.IsPressed(command))
                {
                    UserInputCommands[command][1]();
                    //Debrief eval
                    if (!lemergencybuttonpressed && Locomotive.EmergencyButtonPressed && Locomotive.IsPlayerTrain)
                    {
                        var train = Program.Viewer.PlayerLocomotive.Train;
                        if (Math.Abs(Locomotive.SpeedMpS) == 0) DbfEvalEBPBstopped++;
                        if (Math.Abs(Locomotive.SpeedMpS) > 0) DbfEvalEBPBmoving++;
                        lemergencybuttonpressed = true;
                        train.DbfEvalValueChanged = true;//Debrief eval
                    }
                }
                else if (UserInput.IsReleased(command))
                {
                    UserInputCommands[command][0]();
                    //Debrief eval
                    if (lemergencybuttonpressed && !Locomotive.EmergencyButtonPressed) lemergencybuttonpressed = false;
                }
            }
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

            // Wiper animation
            Wipers.UpdateLoop(Locomotive.Wiper, elapsedTime);

            // Draw 2D CAB View - by GeorgeS
            if (Viewer.Camera.AttachedCar == this.MSTSWagon &&
                Viewer.Camera.Style == Camera.Styles.Cab)
            {

                if (_CabRenderer != null)
                    _CabRenderer.PrepareFrame(frame, elapsedTime);
            }

            base.PrepareFrame(frame, elapsedTime);
        }

        internal override void LoadForPlayer()
        {
            if (!_hasCabRenderer)
            {
                if (Locomotive.CabViewList.Count > 0)
                {
                    if (Locomotive.CabViewList[(int)CabViewType.Front].CVFFile != null && Locomotive.CabViewList[(int)CabViewType.Front].CVFFile.TwoDViews.Count > 0)
                        _CabRenderer = new CabRenderer(Viewer, Locomotive);
                    _hasCabRenderer = true;
                }
            }
            if (!_has3DCabRenderer)
            {
                if (Locomotive.CabViewpoints != null)
                {
                    ThreeDimentionCabViewer tmp3DViewer = null;
                    try
                    {
                        tmp3DViewer = new ThreeDimentionCabViewer(Viewer, this.Locomotive, this); //this constructor may throw an error
                        ThreeDimentionCabViewer = tmp3DViewer; //if not catching an error, we will assign it
                        _has3DCabRenderer = true;
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new Exception("Could not load 3D cab.", error));
                    }
                }
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
        /// Release sounds of TCS if any, but not for player locomotive
        /// </summary>
        public override void Unload()
        {
            if (Locomotive.TrainControlSystem != null && Locomotive.TrainControlSystem.Sounds.Count > 0)
                foreach (var script in Locomotive.TrainControlSystem.Sounds.Keys)
                {
                         Viewer.SoundProcess.RemoveSoundSources(script);
                }
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
        /// 5. animation is in place, but the animated object should be able to swing into place first, then the refueling process begins.
        /// 6. currently ignores locos and tenders without intake points.
        /// The note below may not be accurate since I released a fix that allows the setting of both coal and water level for the tender to be set at start of activity(EK).
        /// Note that the activity class currently parses the initial level of diesel oil, coal and water
        /// but does not use it yet.
        /// Note: With the introduction of the  animated object, I implemented the RefillProcess class as a starter to allow outside classes to use, but
        /// to solve #5 above, its probably best that the processes below be combined in a common class so that both Shapes.cs and FuelPickup.cs can properly keep up with events(EK).
        /// </summary>
        #region Refill loco or tender from pickup points

        WagonAndMatchingPickup MatchedWagonAndPickup;

        /// <summary>
        /// Converts from enum to words for user messages.  
        /// </summary>
        public Dictionary<uint, string> PickupTypeDictionary = new Dictionary<uint, string>()
        {
            {(uint)MSTSWagon.PickupType.FreightGrain, Viewer.Catalog.GetString("freight-grain")},
            {(uint)MSTSWagon.PickupType.FreightCoal, Viewer.Catalog.GetString("freight-coal")},
            {(uint)MSTSWagon.PickupType.FreightGravel, Viewer.Catalog.GetString("freight-gravel")},
            {(uint)MSTSWagon.PickupType.FreightSand, Viewer.Catalog.GetString("freight-sand")},
            {(uint)MSTSWagon.PickupType.FuelWater, Viewer.Catalog.GetString("water")},
            {(uint)MSTSWagon.PickupType.FuelCoal, Viewer.Catalog.GetString("coal")},
            {(uint)MSTSWagon.PickupType.FuelDiesel, Viewer.Catalog.GetString("diesel oil")},
            {(uint)MSTSWagon.PickupType.FuelWood, Viewer.Catalog.GetString("wood")},
            {(uint)MSTSWagon.PickupType.FuelSand, Viewer.Catalog.GetString("sand")},
            {(uint)MSTSWagon.PickupType.FreightGeneral, Viewer.Catalog.GetString("freight-general")},
            {(uint)MSTSWagon.PickupType.FreightLivestock, Viewer.Catalog.GetString("freight-livestock")},
            {(uint)MSTSWagon.PickupType.FreightFuel, Viewer.Catalog.GetString("freight-fuel")},
            {(uint)MSTSWagon.PickupType.FreightMilk, Viewer.Catalog.GetString("freight-milk")},
            {(uint)MSTSWagon.PickupType.SpecialMail, Viewer.Catalog.GetString("mail")}
        };

        /// <summary>
        /// Holds data for an intake point on a wagon (e.g. tender) or loco and a pickup point which can supply that intake. 
        /// </summary>
        public class WagonAndMatchingPickup
        {
            public PickupObj Pickup;
            public MSTSWagon Wagon;
            public MSTSLocomotive SteamLocomotiveWithTender;
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
            int index = 0;
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
                                if (pickup.Location == WorldLocation.None)
                                    pickup.Location = new WorldLocation(
                                        worldFile.TileX, worldFile.TileZ,
                                        pickup.Position.X, pickup.Position.Y, pickup.Position.Z);
                                if ((wagon.FreightAnimations != null && ((uint)wagon.FreightAnimations.FreightType == pickup.PickupType || wagon.FreightAnimations.FreightType == MSTSWagon.PickupType.None) &&
                                    (uint)intake.Type == pickup.PickupType)
                                 || ((uint)intake.Type == pickup.PickupType && (uint)intake.Type > (uint)MSTSWagon.PickupType.FreightSand && (wagon.WagonType == TrainCar.WagonTypes.Tender || wagon is MSTSLocomotive)))
                                {
                                    var intakePosition = new Vector3(0, 0, -intake.OffsetM);
                                    Vector3.Transform(ref intakePosition, ref car.WorldPosition.XNAMatrix, out intakePosition);

                                    var intakeLocation = new WorldLocation(
                                        car.WorldPosition.TileX, car.WorldPosition.TileZ,
                                        intakePosition.X, intakePosition.Y, -intakePosition.Z);

                                    var d2 = WorldLocation.GetDistanceSquared(intakeLocation, pickup.Location);
                                    if (d2 < shortestD2)
                                    {
                                        shortestD2 = d2;
                                        nearestPickup = new WagonAndMatchingPickup();
                                        nearestPickup.Pickup = pickup;
                                        nearestPickup.Wagon = wagon;
                                        if (wagon.WagonType == TrainCar.WagonTypes.Tender)
                                        {
                                            // Normal arrangement would be steam locomotive followed by the tender car.
                                            if (index > 0 && train.Cars[index - 1] is MSTSSteamLocomotive && !wagon.Flipped && !train.Cars[index - 1].Flipped)
                                                nearestPickup.SteamLocomotiveWithTender = train.Cars[index - 1] as MSTSLocomotive;
                                            // but after reversal point or turntable reversal order of cars is reversed too!
                                            else if (index < train.Cars.Count - 1 && train.Cars[index + 1] is MSTSSteamLocomotive && wagon.Flipped && train.Cars[index + 1].Flipped)
                                                nearestPickup.SteamLocomotiveWithTender = train.Cars[index + 1] as MSTSLocomotive;
                                            else if (index > 0 && train.Cars[index - 1] is MSTSSteamLocomotive)
                                                nearestPickup.SteamLocomotiveWithTender = train.Cars[index - 1] as MSTSLocomotive;
                                            else if (index < train.Cars.Count - 1 && train.Cars[index + 1] is MSTSSteamLocomotive)
                                                nearestPickup.SteamLocomotiveWithTender = train.Cars[index + 1] as MSTSLocomotive;
                                        }
                                        nearestPickup.IntakePoint = intake;
                                    }
                                }
                            }
                        }
                    }
                    distanceFromFrontOfTrainM += wagon.CarLengthM;
                }
                index++;
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
            var intakePosition = new Vector3(0, 0, -match.IntakePoint.OffsetM);
            Vector3.Transform(ref intakePosition, ref match.Wagon.WorldPosition.XNAMatrix, out intakePosition);

            var intakeLocation = new WorldLocation(
                match.Wagon.WorldPosition.TileX, match.Wagon.WorldPosition.TileZ,
                intakePosition.X, intakePosition.Y, -intakePosition.Z);

            return (float)Math.Sqrt(WorldLocation.GetDistanceSquared(intakeLocation, match.Pickup.Location));
        }

        /// <summary>
        // This process is tied to the Shift T key combination
        // The purpose of is to perform immediate refueling without having to pull up alongside the fueling station.
        /// </summary>
        public void ImmediateRefill()
        {
            var loco = this.Locomotive;
            
            if (loco == null)
                return;
            
            foreach(var car in loco.Train.Cars)
            {
                // There is no need to check for the tender.  The MSTSSteamLocomotive is the primary key in the refueling process when using immediate refueling.
                if (car is MSTSDieselLocomotive || car is MSTSSteamLocomotive)
                {
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetString("Refill: Immediate refill process selected, refilling immediately."));
                    (car as MSTSLocomotive).RefillImmediately();
                }
            }
        }

        /// <summary>
        /// Prompts if cannot refill yet, else starts continuous refilling.
        /// Tries to find the nearest supply (pickup point) which can refill the locos and tenders in the train.  
        /// </summary>
        public void AttemptToRefillOrUnload()
        {
            MatchedWagonAndPickup = null;   // Ensures that releasing the T key doesn't do anything unless there is something to do.

            var loco = this.Locomotive;

            var match = GetMatchingPickup(loco.Train);
            if (match == null && !(loco is MSTSElectricLocomotive))
                return;
            if (match == null)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetString("Refill: Electric loco and no pickup. Command rejected"));
                return;
            }

            float distanceToPickupM = GetDistanceToM(match) - 2.5f; // Deduct an extra 2.5 so that the tedious placement is less of an issue.
            if (distanceToPickupM > match.IntakePoint.WidthM / 2)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Distance to {0} supply is {1}.",
                    PickupTypeDictionary[(uint)match.Pickup.PickupType], Viewer.Catalog.GetPluralStringFmt("{0} meter", "{0} meters", (long)(distanceToPickupM+1f))));
                return;
            }
            if (distanceToPickupM <= match.IntakePoint.WidthM / 2)
                MSTSWagon.RefillProcess.ActivePickupObjectUID = (int)match.Pickup.UID;
            if (loco.SpeedMpS != 0 && match.Pickup.SpeedRange.MaxMpS == 0f)
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
            if (loco.SpeedMpS > match.Pickup.SpeedRange.MaxMpS)
            {
                var speedLimitMpH = MpS.ToMpH(match.Pickup.SpeedRange.MaxMpS);
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Loco speed must not exceed {0}.",
                    FormatStrings.FormatSpeedLimit(match.Pickup.SpeedRange.MaxMpS, Viewer.MilepostUnitsMetric)));
                return;
            }
            if (match.Wagon is MSTSDieselLocomotive || match.Wagon is MSTSSteamLocomotive || (match.Wagon.WagonType == TrainCar.WagonTypes.Tender && match.SteamLocomotiveWithTender != null))
            {
                // Note: The tender contains the intake information, but the steam locomotive includes the controller information that is needed for the refueling process.

                float fraction = 0;

                // classical MSTS Freightanim, handled as usual
                if(match.SteamLocomotiveWithTender != null)
                    fraction = match.SteamLocomotiveWithTender.GetFilledFraction(match.Pickup.PickupType);
                else
                    fraction = match.Wagon.GetFilledFraction(match.Pickup.PickupType);
                                
                if (fraction > 0.99)
                {
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: {0} supply now replenished.",
                        PickupTypeDictionary[match.Pickup.PickupType]));
                    return;
                }
                else
                {
                    MSTSWagon.RefillProcess.OkToRefill = true;
                    if (match.SteamLocomotiveWithTender != null)
                        StartRefilling(match.Pickup, fraction, match.SteamLocomotiveWithTender);
                    else
                        StartRefilling(match.Pickup, fraction, match.Wagon);

                    MatchedWagonAndPickup = match;  // Save away for HandleUserInput() to use when key is released.
                }
            }
            else if (match.Wagon.FreightAnimations != null)
            {
                // freight wagon animation
                var fraction = match.Wagon.GetFilledFraction(match.Pickup.PickupType);
                if (fraction > 0.99 && match.Pickup.PickupCapacity.FeedRateKGpS >= 0)
                {
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: {0} supply now replenished.",
                        PickupTypeDictionary[match.Pickup.PickupType]));
                    return;
                }
                else if (fraction < 0.01 && match.Pickup.PickupCapacity.FeedRateKGpS < 0)
                {
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Unload: {0} fuel or freight now unloaded.",
                        PickupTypeDictionary[match.Pickup.PickupType]));
                    return;
                }
                else
                {
                    MSTSWagon.RefillProcess.OkToRefill = true;
                    MSTSWagon.RefillProcess.Unload = match.Pickup.PickupCapacity.FeedRateKGpS < 0;
                    match.Wagon.StartRefillingOrUnloading(match.Pickup, match.IntakePoint, fraction, MSTSWagon.RefillProcess.Unload);
                    MatchedWagonAndPickup = match;  // Save away for HandleUserInput() to use when key is released.
                }
            }
        }

        /// <summary>
        /// Called by RefillCommand during replay.
        /// </summary>
        public void RefillChangeTo(float? target)
        {
            MSTSNotchController controller = new MSTSNotchController();
            var loco = this.Locomotive;

            var matchedWagonAndPickup = GetMatchingPickup(loco.Train);   // Save away for RefillCommand to use.
            if (matchedWagonAndPickup != null)
            {
                if (matchedWagonAndPickup.SteamLocomotiveWithTender != null)
                    controller = matchedWagonAndPickup.SteamLocomotiveWithTender.GetRefillController((uint)matchedWagonAndPickup.Pickup.PickupType);
                else
                    controller = (matchedWagonAndPickup.Wagon as MSTSLocomotive).GetRefillController((uint)matchedWagonAndPickup.Pickup.PickupType);
                controller.StartIncrease(target);
            }
        }

        /// <summary>
        /// Starts a continuous increase in controlled value. This method also receives TrainCar car to process individual locomotives for refueling.
        /// </summary>
        /// <param name="type">Pickup point</param>
        public void StartRefilling(PickupObj matchPickup, float fraction, TrainCar car)
        {
            var controller = (car as MSTSLocomotive).GetRefillController(matchPickup.PickupType);

            if (controller == null)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.Error, Viewer.Catalog.GetString("Incompatible pickup type"));
                return;
            }
            (car as MSTSLocomotive).SetStepSize(matchPickup);
            controller.SetValue(fraction);
            controller.CommandStartTime = Viewer.Simulator.ClockTime;  // for Replay to use 
            Viewer.Simulator.Confirmer.Message(ConfirmLevel.Information, Viewer.Catalog.GetString("Starting refill"));
            controller.StartIncrease(controller.MaximumValue);
        }

        /// <summary>
        /// Starts a continuous increase in controlled value.
        /// </summary>
        /// <param name="type">Pickup point</param>
        public void StartRefilling(uint type, float fraction)
        {
            var controller = Locomotive.GetRefillController(type);
            if (controller == null)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.Error, Viewer.Catalog.GetString("Incompatible pickup type"));
                return;
            }
            controller.SetValue(fraction);
            controller.CommandStartTime = Viewer.Simulator.ClockTime;  // for Replay to use 
            Viewer.Simulator.Confirmer.Message(ConfirmLevel.Information, Viewer.Catalog.GetString("Starting refill"));
            controller.StartIncrease(controller.MaximumValue);
        }

        /// <summary>
        // Immediate refueling process is different from the process of refueling individual locomotives.
        /// </summary> 
        public void StopImmediateRefilling(CommandLog log)
        {
            new ImmediateRefillCommand(log);  // for Replay to use
        }

        /// <summary>
        /// Ends a continuous increase in controlled value.
        /// </summary>
        public void StopRefillingOrUnloading(CommandLog log)
        {
            if (MatchedWagonAndPickup == null)
                return;
            MSTSWagon.RefillProcess.OkToRefill = false;
            MSTSWagon.RefillProcess.ActivePickupObjectUID = 0;
            var match = MatchedWagonAndPickup;
            var controller = new MSTSNotchController();
            if (match.Wagon is MSTSDieselLocomotive || match.Wagon is MSTSSteamLocomotive || (match.Wagon.WagonType == TrainCar.WagonTypes.Tender && match.SteamLocomotiveWithTender != null))
            {
                if (match.SteamLocomotiveWithTender != null)
                    controller = match.SteamLocomotiveWithTender.GetRefillController(MatchedWagonAndPickup.Pickup.PickupType);
                else
                    controller = (match.Wagon as MSTSLocomotive).GetRefillController(MatchedWagonAndPickup.Pickup.PickupType);
            }
            else
            {
                controller = match.Wagon.WeightLoadController;
                match.Wagon.UnloadingPartsOpen = false;
            }

            new RefillCommand(log, controller.CurrentValue, controller.CommandStartTime);  // for Replay to use
            if (controller.UpdateValue >= 0)
                controller.StopIncrease();
            else controller.StopDecrease();
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
        public static bool LoadTextures(Viewer viewer, string FileName)
        {
            if (string.IsNullOrEmpty(FileName))
                return false;

            if (DayTextures.Keys.Contains(FileName))
                return false;

            DayTextures.Add(FileName, viewer.TextureManager.Get(FileName, true));

            var nightpath = Path.Combine(Path.Combine(Path.GetDirectoryName(FileName), "night"), Path.GetFileName(FileName));
            NightTextures.Add(FileName, viewer.TextureManager.Get(nightpath));

            var lightdirectory = Path.Combine(Path.GetDirectoryName(FileName), "cablight");
            var lightpath = Path.Combine(lightdirectory, Path.GetFileName(FileName));
            var lightTexture = viewer.TextureManager.Get(lightpath);
            LightTextures.Add(FileName, lightTexture);
            return Directory.Exists(lightdirectory);
        }

        static Texture2D[] Disassemble(GraphicsDevice graphicsDevice, Texture2D texture, int frameCount, Point frameGrid, string fileName)
        {
            if (frameGrid.X < 1 || frameGrid.Y < 1 || frameCount < 1)
            {
                Trace.TraceWarning("Cab control has invalid frame data {1}*{2}={3} (no frames will be shown) for {0}", fileName, frameGrid.X, frameGrid.Y, frameCount);
                return new Texture2D[0];
            }

            var frameSize = new Point(texture.Width / frameGrid.X, texture.Height / frameGrid.Y);
            var frames = new Texture2D[frameCount];
            var frameIndex = 0;

            if (frameCount > frameGrid.X * frameGrid.Y)
                Trace.TraceWarning("Cab control frame count {1} is larger than the number of frames {2}*{3}={4} (some frames will be blank) for {0}", fileName, frameCount, frameGrid.X, frameGrid.Y, frameGrid.X * frameGrid.Y);

            if (texture.Format != SurfaceFormat.Color && texture.Format != SurfaceFormat.Dxt1)
            {
                Trace.TraceWarning("Cab control texture {0} has unsupported format {1}; only Color and Dxt1 are supported.", fileName, texture.Format);
            }
            else
            {
                var copySize = new Point(frameSize.X, frameSize.Y);
                Point controlSize;
                if (texture.Format == SurfaceFormat.Dxt1)
                {
                    controlSize.X = (int)Math.Ceiling((float)copySize.X / 4) * 4;
                    controlSize.Y = (int)Math.Ceiling((float)copySize.Y / 4) * 4;
                    var buffer = new byte[(int)Math.Ceiling((float)copySize.X / 4) * 4 * (int)Math.Ceiling((float)copySize.Y / 4) * 4 / 2];
                    frameIndex = DisassembleFrames(graphicsDevice, texture, frameCount, frameGrid, frames, frameSize, copySize, controlSize, buffer);
                }
                else
                {
                    var buffer = new Color[copySize.X * copySize.Y];
                    frameIndex = DisassembleFrames(graphicsDevice, texture, frameCount, frameGrid, frames, frameSize, copySize, copySize, buffer);
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
                        var frame = frames[frameIndex++] = new Texture2D(graphicsDevice, controlSize.X, controlSize.Y, false, texture.Format);
                        frame.SetData(0, new Rectangle(0, 0, copySize.X, copySize.Y), buffer, 0, buffer.Length);
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
            var frameGrid = new Point(framesX, framesY);

            PDayTextures[fileName] = null;
            if (DayTextures.ContainsKey(fileName))
            {
                var texture = DayTextures[fileName];
                if (texture != SharedMaterialManager.MissingTexture)
                {
                    PDayTextures[fileName] = Disassemble(graphicsDevice, texture, frameCount, frameGrid, fileName + ":day");
                }
            }

            PNightTextures[fileName] = null;
            if (NightTextures.ContainsKey(fileName))
            {
                var texture = NightTextures[fileName];
                if (texture != SharedMaterialManager.MissingTexture)
                {
                    PNightTextures[fileName] = Disassemble(graphicsDevice, texture, frameCount, frameGrid, fileName + ":night");
                }
            }

            PLightTextures[fileName] = null;
            if (LightTextures.ContainsKey(fileName))
            {
                var texture = LightTextures[fileName];
                if (texture != SharedMaterialManager.MissingTexture)
                {
                    PLightTextures[fileName] = Disassemble(graphicsDevice, texture, frameCount, frameGrid, fileName + ":light");
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
        public static Texture2D GetTextureByIndexes(string FileName, int indx, bool isDark, bool isLight, out bool isNightTexture, bool hasCabLightDirectory)
        {
            Texture2D retval = SharedMaterialManager.MissingTexture;
            Texture2D[] tmp = null;

            isNightTexture = false;

            if (string.IsNullOrEmpty(FileName) || !PDayTextures.Keys.Contains(FileName))
                return SharedMaterialManager.MissingTexture;

            if (isLight)
            {
                // Light on: use light texture when dark, if available; else select accordingly presence of CabLight directory.
                if (isDark)
                {
                    tmp = PLightTextures[FileName];
                    if (tmp == null)
                    {
                        if (hasCabLightDirectory)
                            tmp = PNightTextures[FileName];
                        else
                            tmp = PDayTextures[FileName];
                    }
                }
                // Both light and day textures should be used as-is in this situation.
                isNightTexture = true;
            }
            else if (isDark)
            {
                // Darkness: use night texture, if available.
                tmp = PNightTextures[FileName];
                // Only use night texture as-is in this situation.
                isNightTexture = tmp != null;
            }

            // No light or dark texture selected/available? Use day texture instead.
            if (tmp == null)
                tmp = PDayTextures[FileName];

            if (tmp != null)
                retval = SafeGetAt(tmp, indx, FileName);

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
        public static Texture2D GetTexture(string FileName, bool isDark, bool isLight, out bool isNightTexture, bool hasCabLightDirectory)
        {
            Texture2D retval = SharedMaterialManager.MissingTexture;
            isNightTexture = false;

            if (string.IsNullOrEmpty(FileName) || !DayTextures.Keys.Contains(FileName))
                return retval;

            if (isLight)
            {
                // Light on: use light texture when dark, if available; else check if CabLightDirectory available to decide.
                if (isDark)
                {
                    retval = LightTextures[FileName];
                    if (retval == SharedMaterialManager.MissingTexture)
                        retval = hasCabLightDirectory ? NightTextures[FileName] : DayTextures[FileName];
                }

                // Both light and day textures should be used as-is in this situation.
                isNightTexture = true;
            }
            else if (isDark)
            {
                // Darkness: use night texture, if available.
                retval = NightTextures[FileName];
                // Only use night texture as-is in this situation.
                isNightTexture = retval != SharedMaterialManager.MissingTexture;
            }

            // No light or dark texture selected/available? Use day texture instead.
            if (retval == SharedMaterialManager.MissingTexture)
                retval = DayTextures[FileName];

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
        //private SpriteBatchMaterial _Sprite2DCabView;
        private CabSpriteBatchMaterial _SpriteShader2DCabView;
        private Rectangle _CabRect = new Rectangle();
        private Matrix _Scale = Matrix.Identity;
        private Texture2D _CabTexture;
        private CabShader _Shader;  // Shaders must have unique Keys - below
        private int ShaderKey = 1;  // Shader Key must refer to only o

        private Point _PrevScreenSize;

        //private List<CabViewControls> CabViewControlsList = new List<CabViewControls>();
        private List<List<CabViewControlRenderer>> CabViewControlRenderersList = new List<List<CabViewControlRenderer>>();
        private Viewer _Viewer;
        private MSTSLocomotive _Locomotive;
        private int _Location;
        private bool _isNightTexture;
        private bool HasCabLightDirectory = false;
        public Dictionary<int, CabViewControlRenderer> ControlMap;

        [CallOnThread("Loader")]
        public CabRenderer(Viewer viewer, MSTSLocomotive car)
        {
            //Sequence = RenderPrimitiveSequence.CabView;
            _Viewer = viewer;
            _Locomotive = car;

            _Viewer.AdjustCabHeight(_Viewer.DisplaySize.X, _Viewer.DisplaySize.Y);
            
            _Viewer.CabCamera.ScreenChanged();
            
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
            _SpriteShader2DCabView = (CabSpriteBatchMaterial)viewer.MaterialManager.Load("CabSpriteBatch", null, 0, 0, ShaderKey, _Shader);

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
                        HasCabLightDirectory = CABTextureManager.LoadTextures(viewer, cabfile);
                    }

                    if (cabView.CVFFile.CabViewControls == null)
                        continue;

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
                        CVCFirebox firebox = cvc as CVCFirebox;
                        if (firebox != null)
                        {
                            CabViewGaugeRenderer cvgrFire = new CabViewGaugeRenderer(viewer, car, firebox, _Shader);
                            cvgrFire.SortIndex = controlSortIndex++;
                            CabViewControlRenderersList[i].Add(cvgrFire);
                            // don't "continue", because this cvc has to be also recognized as CVCGauge
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
                            CabViewDigitalRenderer cvdr;
                            if (viewer.Settings.CircularSpeedGauge && digital.ControlStyle == CABViewControlStyles.NEEDLE)
                                cvdr = new CabViewCircularSpeedGaugeRenderer(viewer, car, digital, _Shader);
                            else
                                cvdr = new CabViewDigitalRenderer(viewer, car, digital, _Shader);
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

        public CabRenderer(Viewer viewer, MSTSLocomotive car, CabViewFile CVFFile) //used by 3D cab as a refrence, thus many can be eliminated
        {
            _Viewer = viewer;
            _Locomotive = car;


            #region Create Control renderers
            ControlMap = new Dictionary<int, CabViewControlRenderer>();
            int[] count = new int[256];//enough to hold all types, count the occurence of each type
            var i = 0;

            var controlSortIndex = 1;  // Controls are drawn atop the cabview and in order they appear in the CVF file.
            // This allows the segments of moving-scale meters to be hidden by covers (e.g. TGV-A)
            CabViewControlRenderersList.Add(new List<CabViewControlRenderer>());
            foreach (CabViewControl cvc in CVFFile.CabViewControls)
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
                CVCFirebox firebox = cvc as CVCFirebox;
                if (firebox != null)
                {
                    CabViewGaugeRenderer cvgrFire = new CabViewGaugeRenderer(viewer, car, firebox, _Shader);
                    cvgrFire.SortIndex = controlSortIndex++;
                    CabViewControlRenderersList[i].Add(cvgrFire);
                    // don't "continue", because this cvc has to be also recognized as CVCGauge
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
                    CabViewDigitalRenderer cvdr;
                    if (viewer.Settings.CircularSpeedGauge && digital.ControlStyle == CABViewControlStyles.NEEDLE)
                        cvdr = new CabViewCircularSpeedGaugeRenderer(viewer, car, digital, _Shader);
                    else
                        cvdr = new CabViewDigitalRenderer(viewer, car, digital, _Shader);
                    cvdr.SortIndex = controlSortIndex;
                    CabViewControlRenderersList[i].Add(cvdr);
                    if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvdr);
                    count[(int)cvc.ControlType]++;
                    continue;
                }
            }
            #endregion
        }
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!_Locomotive.ShowCab)
                return;

            bool Dark = _Viewer.MaterialManager.sunDirection.Y <= -0.085f || _Viewer.Camera.IsUnderground;
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
            _CabTexture = CABTextureManager.GetTexture(_Locomotive.CabViewList[i].CVFFile.TwoDViews[_Location], Dark, CabLight, out _isNightTexture, HasCabLightDirectory);
            if (_CabTexture == SharedMaterialManager.MissingTexture)
                return;

            // Cab view height adjusted to allow for clip or stretch
            _CabRect.Width = _Viewer.CabWidthPixels;
            _CabRect.Height = _Viewer.CabHeightPixels;

            if (_PrevScreenSize != _Viewer.DisplaySize && _Shader != null)
            {
                _PrevScreenSize = _Viewer.DisplaySize;
                _Shader.SetLightPositions(
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[i].ExtendedCVF.Light1Position, _Viewer.DisplaySize),
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[i].ExtendedCVF.Light2Position, _Viewer.DisplaySize));
            }

            frame.AddPrimitive(_SpriteShader2DCabView, this, RenderPrimitiveGroup.Cab, ref _Scale);
            //frame.AddPrimitive(Materials.SpriteBatchMaterial, this, RenderPrimitiveGroup.Cab, ref _Scale);

            if (_Location == 0)
                foreach (var cvcr in CabViewControlRenderersList[i])
                    cvcr.PrepareFrame(frame, elapsedTime);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // Cab view vertical position adjusted to allow for clip or stretch.
            Rectangle stretchedCab;
            if (_Viewer.Simulator.CarVibrating > 0 || _Viewer.Simulator.UseSuperElevation > 0 || _Locomotive.Train.IsTilting)
            {
                if (_CabTexture != null)
                    stretchedCab = new Rectangle(-50, -40, _CabTexture.Width + 100, _CabTexture.Height + 80);
                else stretchedCab = new Rectangle(_CabRect.Left - _Viewer.CabXOffsetPixels, _CabRect.Top + _Viewer.CabYOffsetPixels, _CabRect.Width, _CabRect.Height);
            }
            else
                stretchedCab = new Rectangle(_CabRect.Left - _Viewer.CabXOffsetPixels, _CabRect.Top + _Viewer.CabYOffsetPixels, _CabRect.Width, _CabRect.Height);

            if (_Shader != null)
            {
                // TODO: Readd ability to control night time lighting.
                if (_Viewer.Settings.UseMSTSEnv == false)
                    _Shader.SetData(_Viewer.MaterialManager.sunDirection, _isNightTexture, false, _Viewer.Simulator.Weather.OvercastFactor);
                else
                    _Shader.SetData(_Viewer.MaterialManager.sunDirection, _isNightTexture, false, _Viewer.World.MSTSSky.mstsskyovercastFactor);

                _Shader.SetTextureData(stretchedCab.Left, stretchedCab.Top, stretchedCab.Width, stretchedCab.Height);
            }

            if (_CabTexture != null)
            {
                if (this._Viewer.Simulator.UseSuperElevation > 0 || _Viewer.Simulator.CarVibrating > 0 || _Locomotive.Train.IsTilting)
                {
                    var scale = new Vector2((float)_CabRect.Width / _CabTexture.Width, (float)_CabRect.Height / _CabTexture.Height);
                    var place = new Vector2(_CabRect.Width / 2 - _Viewer.CabXOffsetPixels - 50 * scale.X, _CabRect.Height / 2 + _Viewer.CabYOffsetPixels - 40 * scale.Y);
                    var place2 = new Vector2(_CabTexture.Width / 2, _CabTexture.Height / 2);
                    _SpriteShader2DCabView.SpriteBatch.Draw(_CabTexture, place, stretchedCab, Color.White, 0, place2, scale, SpriteEffects.None, 0f);
                }
                else
                {
                    _SpriteShader2DCabView.SpriteBatch.Draw(_CabTexture, stretchedCab, Color.White);
                }
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
        protected readonly int ShaderKey = 1;
        protected readonly CabSpriteBatchMaterial CabShaderControlView;

        protected Vector2 Position;
        protected Texture2D Texture;
        protected bool IsNightTexture;
        protected bool HasCabLightDirectory;

        Matrix Matrix = Matrix.Identity;

        public CabViewControlRenderer(Viewer viewer, MSTSLocomotive locomotive, CabViewControl control, CabShader shader)
        {
            Viewer = viewer;
            Locomotive = locomotive;
            Control = control;
            Shader = shader;

            CabShaderControlView = (CabSpriteBatchMaterial)viewer.MaterialManager.Load("CabSpriteBatch", null, 0, 0, ShaderKey, Shader);

            HasCabLightDirectory = CABTextureManager.LoadTextures(Viewer, Control.ACEFile);
        }

        public CABViewControlTypes GetControlType()
        {
            return Control.ControlType;
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

        public CABViewControlStyles GetStyle()
        {
            return Control.ControlStyle;
        }

        [CallOnThread("Updater")]
        public virtual void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            frame.AddPrimitive(CabShaderControlView, this, RenderPrimitiveGroup.Cab, ref Matrix);
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

            Texture = CABTextureManager.GetTexture(Control.ACEFile, false, false, out IsNightTexture, HasCabLightDirectory);
            if (ControlDial.Height < Texture.Height)
                Scale = (float)(ControlDial.Height / Texture.Height);
            Origin = new Vector2((float)Texture.Width / 2, ControlDial.Center / Scale);
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var dark = Viewer.MaterialManager.sunDirection.Y <= -0.085f || Viewer.Camera.IsUnderground;

            Texture = CABTextureManager.GetTexture(Control.ACEFile, dark, Locomotive.CabLightOn, out IsNightTexture, HasCabLightDirectory);
            if (Texture == SharedMaterialManager.MissingTexture)
                return;

            base.PrepareFrame(frame, elapsedTime);

            // Cab view height and vertical position adjusted to allow for clip or stretch.
            Position.X = (float)Viewer.CabWidthPixels / 640 * ((float)Control.PositionX + Origin.X * Scale) - Viewer.CabXOffsetPixels;
            Position.Y = (float)Viewer.CabHeightPixels / 480 * ((float)Control.PositionY + Origin.Y * Scale) + Viewer.CabYOffsetPixels;
            ScaleToScreen = (float)Viewer.DisplaySize.X / 640 * Scale;

            var rangeFraction = GetRangeFraction();
            var direction = ControlDial.Direction == 0 ? 1 : -1;
            var rangeDegrees = direction * (ControlDial.ToDegree - ControlDial.FromDegree);
            while (rangeDegrees <= 0)
                rangeDegrees += 360;
            Rotation = MathHelper.WrapAngle(MathHelper.ToRadians(ControlDial.FromDegree + direction * rangeDegrees * rangeFraction));
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (Shader != null)
            {
                Shader.SetTextureData(Position.X, Position.Y, Texture.Width * ScaleToScreen, Texture.Height * ScaleToScreen);
            }
            CabShaderControlView.SpriteBatch.Draw(Texture, Position, null, Color.White, Rotation, Origin, ScaleToScreen, SpriteEffects.None, 0);
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
        //      bool LoadMeterPositive = true;
        Color DrawColor;
        Double Num;
        bool IsFire;

        public CabViewGaugeRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCGauge control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            Gauge = control;
            if ((Control.ControlType == CABViewControlTypes.REVERSER_PLATE) || (Gauge.ControlStyle == CABViewControlStyles.POINTER))
            {
                DrawColor = Color.White;
                Texture = CABTextureManager.GetTexture(Control.ACEFile, false, Locomotive.CabLightOn, out IsNightTexture, HasCabLightDirectory);
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
            HasCabLightDirectory = CABTextureManager.LoadTextures(Viewer, control.FireACEFile);
            Texture = CABTextureManager.GetTexture(control.FireACEFile, false, Locomotive.CabLightOn, out IsNightTexture, HasCabLightDirectory);
            DrawColor = Color.White;
            SourceRectangle.Width = (int)Texture.Width;
            SourceRectangle.Height = (int)Texture.Height;
            IsFire = true;
        }

        public color GetColor(out bool positive)
        {
            if (Locomotive.GetDataOf(Control) < 0) { positive = false; return Gauge.NegativeColor; }
            else { positive = true; return Gauge.PositiveColor; }
        }

        public CVCGauge GetGauge() { return Gauge; }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!(Gauge is CVCFirebox))
            {
                var dark = Viewer.MaterialManager.sunDirection.Y <= -0.085f || Viewer.Camera.IsUnderground;
                Texture = CABTextureManager.GetTexture(Control.ACEFile, dark, Locomotive.CabLightOn, out IsNightTexture, HasCabLightDirectory);
            }
            if (Texture == SharedMaterialManager.MissingTexture)
                return;

            base.PrepareFrame(frame, elapsedTime);

            // Cab view height adjusted to allow for clip or stretch.
            var xratio = (float)Viewer.CabWidthPixels / 640;
            var yratio = (float)Viewer.CabHeightPixels / 480;

            float percent, xpos, ypos, zeropos;

            percent = IsFire ? 1f : GetRangeFraction();
            //           LoadMeterPositive = percent + Gauge.MinValue / (Gauge.MaxValue - Gauge.MinValue) >= 0;
            Num = Locomotive.GetDataOf(Control);

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
                    if (Gauge.Orientation == 0)
                    {
                        DestinationRectangle.X = (int)(xratio * (Control.PositionX + (zeropos < xpos ? zeropos : xpos))) - Viewer.CabXOffsetPixels;
                        DestinationRectangle.Y = (int)(yratio * Control.PositionY) + Viewer.CabYOffsetPixels;
                        DestinationRectangle.Width = (int)(xratio * (xpos > zeropos ? xpos - zeropos : zeropos - xpos));
                        DestinationRectangle.Height = (int)(yratio * ypos);
                    }
                    else
                    {
                        DestinationRectangle.X = (int)(xratio * Control.PositionX) - Viewer.CabXOffsetPixels;
                        if (Gauge.Direction != 1 && !IsFire)
                        DestinationRectangle.Y = (int)(yratio * (Control.PositionY + (zeropos > ypos ? zeropos : 2 * zeropos - ypos))) - Viewer.CabYOffsetPixels;
                        else
                        DestinationRectangle.Y = (int)(yratio * (Control.PositionY + (zeropos < ypos ? zeropos : ypos))) + Viewer.CabYOffsetPixels;
                        DestinationRectangle.Width = (int)(xratio * xpos);
                        DestinationRectangle.Height = (int)(yratio * (ypos > zeropos ? ypos - zeropos : zeropos - ypos));
                    }
                }
                else
                {
                    DestinationRectangle.X = (int)(xratio * Control.PositionX) - Viewer.CabXOffsetPixels;
                    var topY = Control.PositionY;  // top of visible column. +ve Y is downwards
                    if (Gauge.Direction != 0)  // column grows from bottom or from right
                    {
                        DestinationRectangle.X = (int)(xratio * (Control.PositionX + Gauge.Width - xpos)) - Viewer.CabXOffsetPixels;
                        if (Gauge.Orientation != 0) topY += Gauge.Height * (1 - percent);
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
                    DestinationRectangle.X = (int)(xratio * (Control.PositionX - 0.5 * Gauge.Area.Width + xpos)) - Viewer.CabXOffsetPixels;
                    if (Gauge.Direction != 0)  // column grows from right
                        DestinationRectangle.X = (int)(xratio * (Control.PositionX + Gauge.Width - 0.5 * Gauge.Area.Width - xpos)) - Viewer.CabXOffsetPixels;
                }
                else // gauge vertical
                {
                    topY += ypos - 0.5 * Gauge.Area.Height;
                    DestinationRectangle.X = (int)(xratio * Control.PositionX) - Viewer.CabXOffsetPixels;
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
            if (Control.MinValue < 0 && Control.ControlType != CABViewControlTypes.REVERSER_PLATE && Gauge.ControlStyle != CABViewControlStyles.POINTER)
            {
                if (Num < 0 && Gauge.NegativeColor.A != 0)
                {
                    if ((Gauge.NumNegativeColors >= 2) && (Num < Gauge.NegativeSwitchVal))
                        DrawColor = new Color(Gauge.SecondNegativeColor.R, Gauge.SecondNegativeColor.G, Gauge.SecondNegativeColor.B, Gauge.SecondNegativeColor.A);
                    else DrawColor = new Color(Gauge.NegativeColor.R, Gauge.NegativeColor.G, Gauge.NegativeColor.B, Gauge.NegativeColor.A);
                }
                else
                {
                    if ((Gauge.NumPositiveColors >= 2) && (Num > Gauge.PositiveSwitchVal))
                        DrawColor = new Color(Gauge.SecondPositiveColor.R, Gauge.SecondPositiveColor.G, Gauge.SecondPositiveColor.B, Gauge.SecondPositiveColor.A);
                    else DrawColor = new Color(Gauge.PositiveColor.R, Gauge.PositiveColor.G, Gauge.PositiveColor.B, Gauge.PositiveColor.A);
                }
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (Shader != null)
            {
                Shader.SetTextureData(DestinationRectangle.Left, DestinationRectangle.Top, DestinationRectangle.Width, DestinationRectangle.Height);
            }
            CabShaderControlView.SpriteBatch.Draw(Texture, DestinationRectangle, SourceRectangle, DrawColor);
        }
    }

    /// <summary>
    /// Discrete renderer for Lever, Twostate, Tristate, Multistate, Signal
    /// </summary>
    public class CabViewDiscreteRenderer : CabViewControlRenderer
    {
        readonly CVCWithFrames ControlDiscrete;
        readonly Rectangle SourceRectangle;
        Rectangle DestinationRectangle = new Rectangle();
        public readonly float CVCFlashTimeOn = 0.75f;
        public readonly float CVCFlashTimeTotal = 1.5f;
        float CumulativeTime;
        float Scale = 1;

        /// <summary>
        /// Accumulated mouse movement. Used for controls with no assigned notch controllers, e.g. headlight and reverser.
        /// </summary>
        float IntermediateValue;

        /// <summary>
        /// Function calculating response value for mouse events (movement, left-click), determined by configured style.
        /// </summary>
        readonly Func<float, float> ChangedValue;

        public CabViewDiscreteRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCWithFrames control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            ControlDiscrete = control;
            CABTextureManager.DisassembleTexture(viewer.GraphicsDevice, Control.ACEFile, (int)Control.Width, (int)Control.Height, ControlDiscrete.FramesCount, ControlDiscrete.FramesX, ControlDiscrete.FramesY);
            Texture = CABTextureManager.GetTextureByIndexes(Control.ACEFile, 0, false, false, out IsNightTexture, HasCabLightDirectory);
            SourceRectangle = new Rectangle(0, 0, Texture.Width, Texture.Height);
            Scale = (float)(Math.Min(Control.Height, Texture.Height) / Texture.Height); // Allow only downscaling of the texture, and not upscaling

            switch (ControlDiscrete.ControlStyle)
            {
                case CABViewControlStyles.ONOFF: ChangedValue = (value) => UserInput.IsMouseLeftButtonPressed ? 1 - value : value; break;
                case CABViewControlStyles.WHILE_PRESSED:
                case CABViewControlStyles.PRESSED: ChangedValue = (value) => UserInput.IsMouseLeftButtonDown ? 1 : 0; break;
                case CABViewControlStyles.NONE:
                    ChangedValue = (value) =>
                    {
                        IntermediateValue %= 0.5f;
                        IntermediateValue += NormalizedMouseMovement();
                        return IntermediateValue > 0.5f ? 1 : IntermediateValue < -0.5f ? -1 : 0;
                    };
                    break;
                default: ChangedValue = (value) => value + NormalizedMouseMovement(); break;
            }
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var index = GetDrawIndex();

            var mS = Control as CVCMultiStateDisplay;
            if (mS != null)
            {
                CumulativeTime += elapsedTime.ClockSeconds;
                while (CumulativeTime > CVCFlashTimeTotal)
                    CumulativeTime -= CVCFlashTimeTotal;
                if ((mS.MSStyles.Count > index) && (mS.MSStyles[index] == 1) && (CumulativeTime > CVCFlashTimeOn))
                    return;
            }
            var dark = Viewer.MaterialManager.sunDirection.Y <= -0.085f || Viewer.Camera.IsUnderground;

            Texture = CABTextureManager.GetTextureByIndexes(Control.ACEFile, index, dark, Locomotive.CabLightOn, out IsNightTexture, HasCabLightDirectory);
            if (Texture == SharedMaterialManager.MissingTexture)
                return;

            base.PrepareFrame(frame, elapsedTime);

            // Cab view height and vertical position adjusted to allow for clip or stretch.
            var xratio = (float)Viewer.CabWidthPixels / 640;
            var yratio = (float)Viewer.CabHeightPixels / 480;
            DestinationRectangle.X = (int)(xratio * Control.PositionX * 1.0001) - Viewer.CabXOffsetPixels;
            DestinationRectangle.Y = (int)(yratio * Control.PositionY * 1.0001) + Viewer.CabYOffsetPixels;
            DestinationRectangle.Width = (int)(xratio * Math.Min(Control.Width, Texture.Width));  // Allow only downscaling of the texture, and not upscaling
            DestinationRectangle.Height = (int)(yratio * Math.Min(Control.Height, Texture.Height));  // Allow only downscaling of the texture, and not upscaling
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (Shader != null)
            {
                Shader.SetTextureData(DestinationRectangle.Left, DestinationRectangle.Top, DestinationRectangle.Width, DestinationRectangle.Height);
            }
            CabShaderControlView.SpriteBatch.Draw(Texture, DestinationRectangle, SourceRectangle, Color.White);
        }

        /// <summary>
        /// Determines the index of the Texture to be drawn
        /// </summary>
        /// <returns>index of the Texture</returns>
        public int GetDrawIndex()
        {
            var data = Locomotive.GetDataOf(Control);

            var index = 0;
            switch (ControlDiscrete.ControlType)
            {
                case CABViewControlTypes.ENGINE_BRAKE:
                case CABViewControlTypes.TRAIN_BRAKE:
                case CABViewControlTypes.REGULATOR:
                case CABViewControlTypes.CUTOFF:
                case CABViewControlTypes.BLOWER:
                case CABViewControlTypes.DAMPERS_FRONT:
                case CABViewControlTypes.STEAM_HEAT:
                case CABViewControlTypes.WATER_INJECTOR1:
                case CABViewControlTypes.WATER_INJECTOR2:
                case CABViewControlTypes.FIREHOLE:
                    index = PercentToIndex(data);
                    break;
                case CABViewControlTypes.THROTTLE:
                case CABViewControlTypes.THROTTLE_DISPLAY:
                    index = PercentToIndex(data);
                    break;
                case CABViewControlTypes.FRICTION_BRAKING:
                    index = data > 0.001 ? 1 : 0;
                    break;
                case CABViewControlTypes.DYNAMIC_BRAKE:
                case CABViewControlTypes.DYNAMIC_BRAKE_DISPLAY:
                    if (Locomotive.DynamicBrakeController != null)
                    {
                        if (Locomotive.DynamicBrakePercent == -1)
                            break;
                        if (!Locomotive.HasSmoothStruc)
                            index = Locomotive.DynamicBrakeController != null ? Locomotive.DynamicBrakeController.CurrentNotch : 0;
                        else
                            index = PercentToIndex(Locomotive.DynamicBrakePercent);
                    }
                    else
                    {
                        index = PercentToIndex(Locomotive.DynamicBrakePercent);
                    }
                    break;
                case CABViewControlTypes.CPH_DISPLAY:
                    if (Locomotive.CombinedControlType == MSTSLocomotive.CombinedControl.ThrottleDynamic && Locomotive.DynamicBrakePercent >= 0)
                        // TODO <CSComment> This is a sort of hack to allow MSTS-compliant operation of Dynamic brake indications in the standard USA case with 8 steps (e.g. Dash9)
                        // This hack returns to code of previous OR versions (e.g. release 1.0).
                        // The clean solution for MSTS compliance would be not to increment the percentage of the dynamic brake at first dynamic brake key pression, so that
                        // subsequent steps become of 12.5% as in MSTS instead of 11.11% as in OR. This requires changes in the physics logic </CSComment>
                        index = (int)((ControlDiscrete.FramesCount) * Locomotive.GetCombinedHandleValue(false));
                    else
                        index = PercentToIndex(Locomotive.GetCombinedHandleValue(false));
                    break;
                case CABViewControlTypes.CP_HANDLE:
                    if (Locomotive.CombinedControlType == MSTSLocomotive.CombinedControl.ThrottleDynamic && Locomotive.DynamicBrakePercent >= 0
                        || Locomotive.CombinedControlType == MSTSLocomotive.CombinedControl.ThrottleAir && Locomotive.TrainBrakeController.CurrentValue > 0)
                        index = PercentToIndex(Locomotive.GetCombinedHandleValue(false));
                    else
                        index = PercentToIndex(Locomotive.GetCombinedHandleValue(false));
                    break;
                case CABViewControlTypes.ALERTER_DISPLAY:
                case CABViewControlTypes.RESET:
                case CABViewControlTypes.WIPERS:
                case CABViewControlTypes.EXTERNALWIPERS:
                case CABViewControlTypes.LEFTDOOR:
                case CABViewControlTypes.RIGHTDOOR:
                case CABViewControlTypes.MIRRORS:
                case CABViewControlTypes.HORN:
                case CABViewControlTypes.WHISTLE:
                case CABViewControlTypes.BELL:
                case CABViewControlTypes.SANDERS:
                case CABViewControlTypes.SANDING:
                case CABViewControlTypes.WHEELSLIP:
                case CABViewControlTypes.FRONT_HLIGHT:
                case CABViewControlTypes.PANTOGRAPH:
                case CABViewControlTypes.PANTOGRAPH2:
                case CABViewControlTypes.ORTS_PANTOGRAPH3:
                case CABViewControlTypes.ORTS_PANTOGRAPH4:
                case CABViewControlTypes.PANTOGRAPHS_4:
                case CABViewControlTypes.PANTOGRAPHS_4C:
                case CABViewControlTypes.PANTOGRAPHS_5:
                case CABViewControlTypes.PANTO_DISPLAY:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_STATE:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_AUTHORIZED:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_AND_AUTHORIZED:
                case CABViewControlTypes.DIRECTION:
                case CABViewControlTypes.DIRECTION_DISPLAY:
                case CABViewControlTypes.ASPECT_DISPLAY:
                case CABViewControlTypes.GEARS:
                case CABViewControlTypes.OVERSPEED:
                case CABViewControlTypes.PENALTY_APP:
                case CABViewControlTypes.EMERGENCY_BRAKE:
                case CABViewControlTypes.DOORS_DISPLAY:
                case CABViewControlTypes.CYL_COCKS:
                case CABViewControlTypes.ORTS_CYL_COMP:
                case CABViewControlTypes.STEAM_INJ1:
                case CABViewControlTypes.STEAM_INJ2:
                case CABViewControlTypes.SMALL_EJECTOR:
                case CABViewControlTypes.GEARS_DISPLAY:
                case CABViewControlTypes.CAB_RADIO:
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE:
                case CABViewControlTypes.ORTS_HELPERS_DIESEL_ENGINES:
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE_STATE:
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE_STARTER:
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE_STOPPER:
                case CABViewControlTypes.ORTS_CABLIGHT:
                case CABViewControlTypes.ORTS_LEFTDOOR:
                case CABViewControlTypes.ORTS_RIGHTDOOR:
                case CABViewControlTypes.ORTS_MIRRORS:
                    index = (int)data;
                    break;
            }
            // If it is a control with NumPositions and NumValues, the index becomes the reference to the Positions entry, which in turn is the frame index within the .ace file
            if (ControlDiscrete is CVCDiscrete && !(ControlDiscrete is CVCSignal) && (ControlDiscrete as CVCDiscrete).Positions.Count > index &&
                (ControlDiscrete as CVCDiscrete).Positions.Count == ControlDiscrete.Values.Count && index >= 0)
                index = (ControlDiscrete as CVCDiscrete).Positions[index];

            if (index >= ControlDiscrete.FramesCount) index = ControlDiscrete.FramesCount - 1;
            if (index < 0) index = 0;
            return index;
        }

        /// <summary>
        /// Converts absolute mouse movement to control value change, respecting the configured orientation and increase direction
        /// </summary>
        float NormalizedMouseMovement()
        {
            return (ControlDiscrete.Orientation > 0
                ? (float)UserInput.MouseMoveY / (float)Control.Height
                : (float)UserInput.MouseMoveX / (float)Control.Width)
                * (ControlDiscrete.Direction > 0 ? -1 : 1);
        }

        public bool IsMouseWithin()
        {
            return ControlDiscrete.MouseControl & DestinationRectangle.Contains(UserInput.MouseX, UserInput.MouseY);
        }

        /// <summary>
        /// Handles cabview mouse events, and changes the corresponding locomotive control values.
        /// </summary>
        public void HandleUserInput()
        {
            switch (Control.ControlType)
            {
                case CABViewControlTypes.REGULATOR:
                case CABViewControlTypes.THROTTLE: Locomotive.SetThrottleValue(ChangedValue(Locomotive.ThrottleController.IntermediateValue)); break;
                case CABViewControlTypes.ENGINE_BRAKE: Locomotive.SetEngineBrakeValue(ChangedValue(Locomotive.EngineBrakeController.IntermediateValue)); break;
                case CABViewControlTypes.TRAIN_BRAKE: Locomotive.SetTrainBrakeValue(ChangedValue(Locomotive.TrainBrakeController.IntermediateValue)); break;
                case CABViewControlTypes.DYNAMIC_BRAKE: Locomotive.SetDynamicBrakeValue(ChangedValue(Locomotive.DynamicBrakeController.IntermediateValue)); break;
                case CABViewControlTypes.GEARS: Locomotive.SetGearBoxValue(ChangedValue(Locomotive.GearBoxController.IntermediateValue)); break;
                case CABViewControlTypes.DIRECTION: var dir = ChangedValue(0); if (dir != 0) new ReverserCommand(Viewer.Log, dir > 0); break;
                case CABViewControlTypes.FRONT_HLIGHT: var hl = ChangedValue(0); if (hl != 0) new HeadlightCommand(Viewer.Log, hl > 0); break;
                case CABViewControlTypes.WHISTLE:
                case CABViewControlTypes.HORN: new HornCommand(Viewer.Log, ChangedValue(Locomotive.Horn ? 1 : 0) > 0); break;
                case CABViewControlTypes.BELL: new BellCommand(Viewer.Log, ChangedValue(Locomotive.Bell ? 1 : 0) > 0); break;
                case CABViewControlTypes.SANDERS:
                case CABViewControlTypes.SANDING: new SanderCommand(Viewer.Log, ChangedValue(Locomotive.Sander ? 1 : 0) > 0); break;
                case CABViewControlTypes.PANTOGRAPH: new PantographCommand(Viewer.Log, 1, ChangedValue(Locomotive.Pantographs[1].CommandUp ? 1 : 0) > 0); break;
                case CABViewControlTypes.PANTOGRAPH2: new PantographCommand(Viewer.Log, 2, ChangedValue(Locomotive.Pantographs[2].CommandUp ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_PANTOGRAPH3: new PantographCommand(Viewer.Log, 3, ChangedValue(Locomotive.Pantographs[3].CommandUp ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_PANTOGRAPH4: new PantographCommand(Viewer.Log, 4, ChangedValue(Locomotive.Pantographs[4].CommandUp ? 1 : 0) > 0); break;
                case CABViewControlTypes.PANTOGRAPHS_4C:
                case CABViewControlTypes.PANTOGRAPHS_4:
                    var pantos = ChangedValue(0);
                    if (pantos != 0)
                    {
                        if (Locomotive.Pantographs[1].State == PantographState.Down && Locomotive.Pantographs[2].State == PantographState.Down)
                        {
                            if (pantos > 0) new PantographCommand(Viewer.Log, 1, true);
                            else if (Control.ControlType == CABViewControlTypes.PANTOGRAPHS_4C) new PantographCommand(Viewer.Log, 2, true);
                        }
                        else if (Locomotive.Pantographs[1].State == PantographState.Up && Locomotive.Pantographs[2].State == PantographState.Down)
                        {
                            if (pantos > 0) new PantographCommand(Viewer.Log, 2, true);
                            else new PantographCommand(Viewer.Log, 1, false);
                        }
                        else if (Locomotive.Pantographs[1].State == PantographState.Up && Locomotive.Pantographs[2].State == PantographState.Up)
                        {
                            if (pantos > 0) new PantographCommand(Viewer.Log, 1, false);
                            else new PantographCommand(Viewer.Log, 2, false);
                        }
                        else if (Locomotive.Pantographs[1].State == PantographState.Down && Locomotive.Pantographs[2].State == PantographState.Up)
                        {
                            if (pantos < 0) new PantographCommand(Viewer.Log, 1, true);
                            else if (Control.ControlType == CABViewControlTypes.PANTOGRAPHS_4C) new PantographCommand(Viewer.Log, 2, false);
                        }
                    }
                    break;
                case CABViewControlTypes.STEAM_HEAT: Locomotive.SetSteamHeatValue(ChangedValue(Locomotive.SteamHeatController.IntermediateValue)); break;
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER:
                    new CircuitBreakerClosingOrderCommand(Viewer.Log, ChangedValue((Locomotive as MSTSElectricLocomotive).PowerSupply.CircuitBreaker.DriverClosingOrder ? 1 : 0) > 0);
                    new CircuitBreakerClosingOrderButtonCommand(Viewer.Log, ChangedValue(UserInput.IsMouseLeftButtonPressed ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER: new CircuitBreakerOpeningOrderButtonCommand(Viewer.Log, ChangedValue(UserInput.IsMouseLeftButtonPressed ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION: new CircuitBreakerClosingAuthorizationCommand(Viewer.Log, ChangedValue((Locomotive as MSTSElectricLocomotive).PowerSupply.CircuitBreaker.DriverClosingAuthorization ? 1 : 0) > 0); break;
                case CABViewControlTypes.EMERGENCY_BRAKE: if ((Locomotive.EmergencyButtonPressed ? 1 : 0) != ChangedValue(Locomotive.EmergencyButtonPressed ? 1 : 0)) new EmergencyPushButtonCommand(Viewer.Log); break;
                case CABViewControlTypes.RESET: new AlerterCommand(Viewer.Log, ChangedValue(Locomotive.TrainControlSystem.AlerterButtonPressed ? 1 : 0) > 0); break;
                case CABViewControlTypes.CP_HANDLE: Locomotive.SetCombinedHandleValue(ChangedValue(Locomotive.GetCombinedHandleValue(true))); break;
                // Steam locomotives only:
                case CABViewControlTypes.CUTOFF: (Locomotive as MSTSSteamLocomotive).SetCutoffValue(ChangedValue((Locomotive as MSTSSteamLocomotive).CutoffController.IntermediateValue)); break;
                case CABViewControlTypes.BLOWER: (Locomotive as MSTSSteamLocomotive).SetBlowerValue(ChangedValue((Locomotive as MSTSSteamLocomotive).BlowerController.IntermediateValue)); break;
                case CABViewControlTypes.DAMPERS_FRONT: (Locomotive as MSTSSteamLocomotive).SetDamperValue(ChangedValue((Locomotive as MSTSSteamLocomotive).DamperController.IntermediateValue)); break;
                case CABViewControlTypes.FIREHOLE: (Locomotive as MSTSSteamLocomotive).SetFireboxDoorValue(ChangedValue((Locomotive as MSTSSteamLocomotive).FireboxDoorController.IntermediateValue)); break;
                case CABViewControlTypes.WATER_INJECTOR1: (Locomotive as MSTSSteamLocomotive).SetInjector1Value(ChangedValue((Locomotive as MSTSSteamLocomotive).Injector1Controller.IntermediateValue)); break;
                case CABViewControlTypes.WATER_INJECTOR2: (Locomotive as MSTSSteamLocomotive).SetInjector2Value(ChangedValue((Locomotive as MSTSSteamLocomotive).Injector2Controller.IntermediateValue)); break;
                case CABViewControlTypes.CYL_COCKS: if (((Locomotive as MSTSSteamLocomotive).CylinderCocksAreOpen ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).CylinderCocksAreOpen ? 1 : 0)) new ToggleCylinderCocksCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_CYL_COMP: if (((Locomotive as MSTSSteamLocomotive).CylinderCompoundOn ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).CylinderCompoundOn ? 1 : 0)) new ToggleCylinderCompoundCommand(Viewer.Log); break;
                case CABViewControlTypes.STEAM_INJ1: if (((Locomotive as MSTSSteamLocomotive).Injector1IsOn ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).Injector1IsOn ? 1 : 0)) new ToggleInjectorCommand(Viewer.Log, 1); break;
                case CABViewControlTypes.STEAM_INJ2: if (((Locomotive as MSTSSteamLocomotive).Injector2IsOn ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).Injector2IsOn ? 1 : 0)) new ToggleInjectorCommand(Viewer.Log, 2); break;
                case CABViewControlTypes.SMALL_EJECTOR: (Locomotive as MSTSSteamLocomotive).SetSmallEjectorValue(ChangedValue((Locomotive as MSTSSteamLocomotive).SmallEjectorController.IntermediateValue)); break;
                //
                case CABViewControlTypes.CAB_RADIO: new CabRadioCommand(Viewer.Log, ChangedValue(Locomotive.CabRadioOn ? 1 : 0) > 0); break;
                case CABViewControlTypes.WIPERS: new WipersCommand(Viewer.Log, ChangedValue(Locomotive.Wiper ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE:
                    var dieselLoco = Locomotive as MSTSDieselLocomotive;
                    if ((dieselLoco.DieselEngines[0].EngineStatus == Orts.Simulation.RollingStocks.SubSystems.PowerSupplies.DieselEngine.Status.Running ||
                                dieselLoco.DieselEngines[0].EngineStatus == Orts.Simulation.RollingStocks.SubSystems.PowerSupplies.DieselEngine.Status.Stopped) &&
                                ChangedValue(1) == 0) new TogglePlayerEngineCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_HELPERS_DIESEL_ENGINES:
                    foreach (var car in Locomotive.Train.Cars)
                    {
                        dieselLoco = car as MSTSDieselLocomotive;
                        if (dieselLoco != null && dieselLoco.AcceptMUSignals)
                        {
                            if (car == Viewer.Simulator.PlayerLocomotive && dieselLoco.DieselEngines.Count > 1)
                            {
                                if ((dieselLoco.DieselEngines[1].EngineStatus == Orts.Simulation.RollingStocks.SubSystems.PowerSupplies.DieselEngine.Status.Running ||
                                            dieselLoco.DieselEngines[1].EngineStatus == Orts.Simulation.RollingStocks.SubSystems.PowerSupplies.DieselEngine.Status.Stopped) &&
                                            ChangedValue(1) == 0) new ToggleHelpersEngineCommand(Viewer.Log);
                                break;
                            }
                            else if (car != Viewer.Simulator.PlayerLocomotive)
                            {
                                if ((dieselLoco.DieselEngines[0].EngineStatus == Orts.Simulation.RollingStocks.SubSystems.PowerSupplies.DieselEngine.Status.Running ||
                                            dieselLoco.DieselEngines[0].EngineStatus == Orts.Simulation.RollingStocks.SubSystems.PowerSupplies.DieselEngine.Status.Stopped) &&
                                            ChangedValue(1) == 0) new ToggleHelpersEngineCommand(Viewer.Log);
                                break;
                            }
                        }
                    }
                    break;
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE_STARTER:
                    dieselLoco = Locomotive as MSTSDieselLocomotive;
                    if (dieselLoco.DieselEngines[0].EngineStatus == Orts.Simulation.RollingStocks.SubSystems.PowerSupplies.DieselEngine.Status.Stopped &&
                                ChangedValue(1) == 0) new TogglePlayerEngineCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE_STOPPER:
                    dieselLoco = Locomotive as MSTSDieselLocomotive;
                    if (dieselLoco.DieselEngines[0].EngineStatus == Orts.Simulation.RollingStocks.SubSystems.PowerSupplies.DieselEngine.Status.Running &&
                                ChangedValue(1) == 0) new TogglePlayerEngineCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_CABLIGHT:
                    if ((Locomotive.CabLightOn ? 1 : 0) != ChangedValue(Locomotive.CabLightOn ? 1 : 0)) new ToggleCabLightCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_LEFTDOOR:
                    if ((Locomotive.GetCabFlipped() ? (Locomotive.DoorRightOpen ? 1 : 0) : Locomotive.DoorLeftOpen ? 1 : 0)
                        != ChangedValue(Locomotive.GetCabFlipped() ? (Locomotive.DoorRightOpen ? 1 : 0) : Locomotive.DoorLeftOpen ? 1 : 0)) new ToggleDoorsLeftCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_RIGHTDOOR:
                    if ((Locomotive.GetCabFlipped() ? (Locomotive.DoorLeftOpen ? 1 : 0) : Locomotive.DoorRightOpen ? 1 : 0)
                         != ChangedValue(Locomotive.GetCabFlipped() ? (Locomotive.DoorLeftOpen ? 1 : 0) : Locomotive.DoorRightOpen ? 1 : 0)) new ToggleDoorsRightCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_MIRRORS:
                    if ((Locomotive.MirrorOpen ? 1 : 0) != ChangedValue(Locomotive.MirrorOpen ? 1 : 0)) new ToggleMirrorsCommand(Viewer.Log); break;
            }

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

            if (ControlDiscrete.MinValue != ControlDiscrete.MaxValue && !(ControlDiscrete.MinValue == 0 && ControlDiscrete.MaxValue == 0))
                percent = MathHelper.Clamp(percent, (float)ControlDiscrete.MinValue, (float)ControlDiscrete.MaxValue);

            if (ControlDiscrete.Values.Count > 1)
            {
                try
                {
                    var val = ControlDiscrete.Values[0] <= ControlDiscrete.Values[ControlDiscrete.Values.Count - 1] ?
                        ControlDiscrete.Values.Where(v => (float)v <= percent + 0.00001).Last() : ControlDiscrete.Values.Where(v => (float)v <= percent + 0.00001).First();
                    index = ControlDiscrete.Values.IndexOf(val);
                }
                catch
                {
                    var val = ControlDiscrete.Values.Min();
                    index = ControlDiscrete.Values.IndexOf(val);
                }
            }
            else if (ControlDiscrete.MaxValue != ControlDiscrete.MinValue)
            {
                index = (int)(percent / (ControlDiscrete.MaxValue - ControlDiscrete.MinValue) * ControlDiscrete.FramesCount);
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
        readonly LabelAlignment Alignment;
        string Format = "{0}";
        readonly string Format1 = "{0}";
        readonly string Format2 = "{0}";

        float Num;
        WindowTextFont DrawFont;
        protected Rectangle DrawPosition;
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

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var digital = Control as CVCDigital;

            Num = Locomotive.GetDataOf(Control);
            if (Math.Abs(Num) < digital.AccuracySwitch)
                Format = Format2;
            else
                Format = Format1;
            DrawFont = Viewer.WindowManager.TextManager.GetExact(digital.FontFamily, Viewer.CabHeightPixels * digital.FontSize / 480, digital.FontStyle == 0 ? System.Drawing.FontStyle.Regular : System.Drawing.FontStyle.Bold);
            DrawPosition.X = (int)(Position.X * Viewer.CabWidthPixels / 640) + (Viewer.CabExceedsDisplayHorizontally > 0 ? DrawFont.Height / 4 : 0) - Viewer.CabXOffsetPixels;
            DrawPosition.Y = (int)((Position.Y + Control.Height / 2) * Viewer.CabHeightPixels / 480) - DrawFont.Height / 2 + Viewer.CabYOffsetPixels;
            DrawPosition.Width = (int)(Control.Width * Viewer.DisplaySize.X / 640);
            DrawPosition.Height = (int)(Control.Height * Viewer.DisplaySize.Y / 480);

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
            else if (digital.OldValue != 0 && digital.OldValue > Num && digital.DecreaseColor.A != 0)
            {
                DrawText = String.Format(Format, Math.Abs(Num));
                DrawColor = new Color(digital.DecreaseColor.R, digital.DecreaseColor.G, digital.DecreaseColor.B, digital.DecreaseColor.A);
            }
            else if (Num < 0 && digital.NegativeColor.A != 0)
            {
                DrawText = String.Format(Format, Math.Abs(Num));
                if ((digital.NumNegativeColors >= 2) && (Num < digital.NegativeSwitchVal))
                    DrawColor = new Color(digital.SecondNegativeColor.R, digital.SecondNegativeColor.G, digital.SecondNegativeColor.B, digital.SecondNegativeColor.A);
                else DrawColor = new Color(digital.NegativeColor.R, digital.NegativeColor.G, digital.NegativeColor.B, digital.NegativeColor.A);
            }
            else if (digital.PositiveColor.A != 0)
            {
                DrawText = String.Format(Format, Num);
                if ((digital.NumPositiveColors >= 2) && (Num > digital.PositiveSwitchVal))
                    DrawColor = new Color(digital.SecondPositiveColor.R, digital.SecondPositiveColor.G, digital.SecondPositiveColor.B, digital.SecondPositiveColor.A);
                else DrawColor = new Color(digital.PositiveColor.R, digital.PositiveColor.G, digital.PositiveColor.B, digital.PositiveColor.A);
            }
            else
            {
                DrawText = String.Format(Format, Num);
                DrawColor = Color.White;
            }
            //          <CSComment> Now speedometer is handled like the other digitals

            base.PrepareFrame(frame, elapsedTime);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            DrawFont.Draw(CabShaderControlView.SpriteBatch, DrawPosition, Point.Zero, DrawText, Alignment, DrawColor);
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
                else if (digital.OldValue != 0 && digital.OldValue > Num && digital.DecreaseColor.A != 0)
                {
                    displayedText = String.Format(Format, Math.Abs(Num));
                    DrawColor = new Color(digital.DecreaseColor.R, digital.DecreaseColor.G, digital.DecreaseColor.B, digital.DecreaseColor.A);
                }
                else if (Num < 0 && digital.NegativeColor.A != 0)
                {
                    displayedText = String.Format(Format, Math.Abs(Num));
                    if ((digital.NumNegativeColors >= 2) && (Num < digital.NegativeSwitchVal))
                        DrawColor = new Color(digital.SecondNegativeColor.R, digital.SecondNegativeColor.G, digital.SecondNegativeColor.B, digital.SecondNegativeColor.A);
                    else DrawColor = new Color(digital.NegativeColor.R, digital.NegativeColor.G, digital.NegativeColor.B, digital.NegativeColor.A);
                }
                else if (digital.PositiveColor.A != 0)
                {
                    displayedText = String.Format(Format, Num);
                    if ((digital.NumPositiveColors >= 2) && (Num > digital.PositiveSwitchVal))
                        DrawColor = new Color(digital.SecondPositiveColor.R, digital.SecondPositiveColor.G, digital.SecondPositiveColor.B, digital.SecondPositiveColor.A);
                    else DrawColor = new Color(digital.PositiveColor.R, digital.PositiveColor.G, digital.PositiveColor.B, digital.PositiveColor.A);
                }
                else
                {
                    displayedText = String.Format(Format, Num);
                    DrawColor = Color.White;
                }
                // <CSComment> Speedometer is now managed like the other digitals

                return displayedText;
            }
            catch (Exception)
            {
                DrawColor = Color.Blue;
            }

            return "";
        }

        public string Get3DDigits(out bool Alert) //used in 3D cab, with AM/PM added, and determine if we want to use alert color
        {
            Alert = false;
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
                        if (hour < 12) displayedText = "a";
                        else displayedText = "p";
                        hour %= 12;
                        if (hour == 0)
                            hour = 12;
                    }
                    displayedText = String.Format(digital.Accuracy > 0 ? "{0:D2}:{1:D2}:{2:D2}" : "{0:D2}:{1:D2}", hour, minute, seconds) + displayedText;
                }
                else if (digital.OldValue != 0 && digital.OldValue > Num && digital.DecreaseColor.A != 0)
                {
                    displayedText = String.Format(Format, Math.Abs(Num));
                }
                else if (Num < 0 && digital.NegativeColor.A != 0)
                {
                    displayedText = String.Format(Format, Math.Abs(Num));
                    if ((digital.NumNegativeColors >= 2) && (Num < digital.NegativeSwitchVal))
                        Alert = true;
                }
                else if (digital.PositiveColor.A != 0)
                {
                    displayedText = String.Format(Format, Num);
                    if ((digital.NumPositiveColors >= 2) && (Num > digital.PositiveSwitchVal))
                        Alert = true;
                }
                else
                {
                    displayedText = String.Format(Format, Num);
                }
                // <CSComment> Speedometer is now managed like the other digitals

                return displayedText;
            }
            catch (Exception)
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

        public PoseableShape TrainCarShape = null;
        public Dictionary<int, AnimatedPartMultiState> AnimateParts = null;
        Dictionary<int, ThreeDimCabGaugeNative> Gauges = null;
        Dictionary<int, AnimatedPart> OnDemandAnimateParts = null; //like external wipers, and other parts that will be switched on by mouse in the future
        //Dictionary<int, DigitalDisplay> DigitParts = null;
        Dictionary<int, ThreeDimCabDigit> DigitParts3D = null;
        AnimatedPart ExternalWipers = null; // setting to zero to prevent a warning. Probably this will be used later. TODO
        protected MSTSLocomotive MSTSLocomotive { get { return (MSTSLocomotive)Car; } }
        MSTSLocomotiveViewer LocoViewer;
        private SpriteBatchMaterial _Sprite2DCabView;
        public ThreeDimentionCabViewer(Viewer viewer, MSTSLocomotive car, MSTSLocomotiveViewer locoViewer)
            : base(viewer, car)
        {
            Locomotive = car;
            _Sprite2DCabView = (SpriteBatchMaterial)viewer.MaterialManager.Load("SpriteBatch");
            LocoViewer = locoViewer;
            if (car.CabView3D != null)
            {
                var shapePath = car.CabView3D.ShapeFilePath;
                TrainCarShape = new PoseableShape(viewer, shapePath + '\0' + Path.GetDirectoryName(shapePath), car.WorldPosition, ShapeFlags.ShadowCaster);
                locoViewer.ThreeDimentionCabRenderer = new CabRenderer(viewer, car, car.CabView3D.CVFFile);
            }
            else locoViewer.ThreeDimentionCabRenderer = locoViewer._CabRenderer;

            AnimateParts = new Dictionary<int, AnimatedPartMultiState>();
            //DigitParts = new Dictionary<int, DigitalDisplay>();
            DigitParts3D = new Dictionary<int, ThreeDimCabDigit>();
            Gauges = new Dictionary<int, ThreeDimCabGaugeNative>();
            OnDemandAnimateParts = new Dictionary<int, AnimatedPart>();
            CABViewControlTypes type;
            // Find the animated parts
            if (TrainCarShape != null && TrainCarShape.SharedShape.Animations != null)
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
                    int order, key;
                    string parameter1 = "0", parameter2 = "";
                    CabViewControlRenderer style = null;
                    //ASPECT_SIGNAL:0:0
                    var tmp = typeName.Split(':');
                    try
                    {
                        order = int.Parse(tmp[1].Trim());
                        if (tmp.Length >= 3) parameter1 = tmp[2].Trim();
                        if (tmp.Length == 4) parameter2 = tmp[3].Trim();//we can get max two parameters per part
                    }
                    catch { continue; }
                    try
                    {
                        type = (CABViewControlTypes)Enum.Parse(typeof(CABViewControlTypes), tmp[0].Trim(), true); //convert from string to enum
                        key = 1000 * (int)type + order;
                        if (type != CABViewControlTypes.EXTERNALWIPERS && type != CABViewControlTypes.MIRRORS && type != CABViewControlTypes.LEFTDOOR && type != CABViewControlTypes.RIGHTDOOR)
                            style = locoViewer.ThreeDimentionCabRenderer.ControlMap[key]; //cvf file has no external wipers, left door, right door and mirrors key word
                    }
                    catch
                    {
                        type = CABViewControlTypes.NONE;
                        continue;
                    }


                    key = 1000 * (int)type + order;
                    if (style != null && style is CabViewDigitalRenderer)//digits?
                    {
                        //DigitParts.Add(key, new DigitalDisplay(viewer, TrainCarShape, iMatrix, parameter, locoViewer.ThreeDimentionCabRenderer.ControlMap[key]));
                        DigitParts3D.Add(key, new ThreeDimCabDigit(viewer, iMatrix, parameter1, parameter2, this.TrainCarShape, locoViewer.ThreeDimentionCabRenderer.ControlMap[key]));
                    }
                    else if (style != null && style is CabViewGaugeRenderer)
                    {
                        var CVFR = (CabViewGaugeRenderer)style;

                        if (CVFR.GetGauge().ControlStyle != CABViewControlStyles.POINTER) //pointer will be animated, others will be drawn dynamicaly
                        {
                            Gauges.Add(key, new ThreeDimCabGaugeNative(viewer, iMatrix, parameter1, parameter2, this.TrainCarShape, locoViewer.ThreeDimentionCabRenderer.ControlMap[key]));
                        }
                        else
                        {//for pointer animation
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
                    else
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
            }
        }
        public override void InitializeUserInputCommands() { }

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
                if (p.Value.Type >= CABViewControlTypes.EXTERNALWIPERS) //for wipers, doors and mirrors
                {
                    switch (p.Value.Type)
                    {
                        case CABViewControlTypes.EXTERNALWIPERS:
                            p.Value.UpdateLoop(Locomotive.Wiper, elapsedTime);
                            break;
                        case CABViewControlTypes.LEFTDOOR:
                            p.Value.UpdateState(Locomotive.DoorLeftOpen, elapsedTime);
                            break;
                        case CABViewControlTypes.RIGHTDOOR:
                            p.Value.UpdateState(Locomotive.DoorRightOpen, elapsedTime);
                            break;
                        case CABViewControlTypes.MIRRORS:
                            p.Value.UpdateState(Locomotive.MirrorOpen, elapsedTime);
                            break;
                        default:
                            break;
                    }
                }
                else p.Value.Update(this.LocoViewer, elapsedTime); //for all other intruments with animations
            }
            foreach (var p in DigitParts3D)
            {
                p.Value.PrepareFrame(frame, elapsedTime);
            }
            foreach (var p in Gauges)
            {
                p.Value.PrepareFrame(frame, elapsedTime);
            }

            if (ExternalWipers != null) ExternalWipers.UpdateLoop(Locomotive.Wiper, elapsedTime);
            /*
            foreach (var p in DigitParts)
            {
                p.Value.PrepareFrame(frame, elapsedTime);
            }*/ //removed with 3D digits

            if (TrainCarShape != null)
                TrainCarShape.PrepareFrame(frame, elapsedTime);
        }

        internal override void Mark()
        {
            // TODO: This is likely wrong; we should mark textures, shapes and other graphical resources here.
        }
    } // Class ThreeDimentionCabViewer

    public class ThreeDimCabDigit
    {
        PoseableShape TrainCarShape;
        VertexPositionNormalTexture[] VertexList;
        int NumVertices;
        int NumIndices;
        public short[] TriangleListIndices;// Array of indices to vertices for triangles
        Matrix XNAMatrix;
        Viewer Viewer;
        ShapePrimitive shapePrimitive;
        CabViewDigitalRenderer CVFR;
        Material Material;
        Material AlertMaterial;
        float Size;
        string AceFile;
        public ThreeDimCabDigit(Viewer viewer, int iMatrix, string size, string aceFile, PoseableShape trainCarShape, CabViewControlRenderer c)
        {

            Size = int.Parse(size) * 0.001f;//input size is in mm
            if (aceFile != "")
            {
                AceFile = aceFile.ToUpper();
                if (!AceFile.EndsWith(".ACE")) AceFile = AceFile + ".ACE"; //need to add ace into it
            }
            else { AceFile = ""; }

            CVFR = (CabViewDigitalRenderer)c;
            Viewer = viewer;
            TrainCarShape = trainCarShape;
            XNAMatrix = TrainCarShape.SharedShape.Matrices[iMatrix];
            var maxVertex = 32;// every face has max 5 digits, each has 2 triangles
            //Material = viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(viewer.Simulator, Helpers.TextureFlags.None, texture), (int)(SceneryMaterialOptions.None | SceneryMaterialOptions.AlphaBlendingBlend), 0);
            Material = FindMaterial(false);//determine normal material
            // Create and populate a new ShapePrimitive
            NumVertices = NumIndices = 0;

            VertexList = new VertexPositionNormalTexture[maxVertex];
            TriangleListIndices = new short[maxVertex / 2 * 3]; // as is NumIndices

            //start position is the center of the text
            var start = new Vector3(0, 0, 0);
            var rotation = 0;

            //find the left-most of text
            Vector3 offset;

            offset.X = 0;

            offset.Y = -Size;

            string speed = "000000";
            for (var j = 0; j < speed.Length; j++)
            {
                var tX = GetTextureCoordX(speed[j]); var tY = GetTextureCoordY(speed[j]);
                var rot = Matrix.CreateRotationY(-rotation);

                //the left-bottom vertex
                Vector3 v = new Vector3(offset.X, offset.Y, 0.01f);
                v = Vector3.Transform(v, rot);
                v += start; Vertex v1 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX, tY);

                //the right-bottom vertex
                v.X = offset.X + Size; v.Y = offset.Y; v.Z = 0.01f;
                v = Vector3.Transform(v, rot);
                v += start; Vertex v2 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX + 0.25f, tY);

                //the right-top vertex
                v.X = offset.X + Size; v.Y = offset.Y + Size; v.Z = 0.01f;
                v = Vector3.Transform(v, rot);
                v += start; Vertex v3 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX + 0.25f, tY - 0.25f);

                //the left-top vertex
                v.X = offset.X; v.Y = offset.Y + Size; v.Z = 0.01f;
                v = Vector3.Transform(v, rot);
                v += start; Vertex v4 = new Vertex(v.X, v.Y, v.Z, 0, 0, -1, tX, tY - 0.25f);

                //create first triangle
                TriangleListIndices[NumIndices++] = (short)NumVertices;
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 1);
                // Second triangle:
                TriangleListIndices[NumIndices++] = (short)NumVertices;
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 3);
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);

                //create vertex
                VertexList[NumVertices].Position = v1.Position; VertexList[NumVertices].Normal = v1.Normal; VertexList[NumVertices].TextureCoordinate = v1.TexCoord;
                VertexList[NumVertices + 1].Position = v2.Position; VertexList[NumVertices + 1].Normal = v2.Normal; VertexList[NumVertices + 1].TextureCoordinate = v2.TexCoord;
                VertexList[NumVertices + 2].Position = v3.Position; VertexList[NumVertices + 2].Normal = v3.Normal; VertexList[NumVertices + 2].TextureCoordinate = v3.TexCoord;
                VertexList[NumVertices + 3].Position = v4.Position; VertexList[NumVertices + 3].Normal = v4.Normal; VertexList[NumVertices + 3].TextureCoordinate = v4.TexCoord;
                NumVertices += 4;
                offset.X += Size * 0.8f; offset.Y += 0; //move to next digit
            }

            var i = 0;
            //create the shape primitive
            short[] newTList = new short[NumIndices];
            for (i = 0; i < NumIndices; i++) newTList[i] = TriangleListIndices[i];
            VertexPositionNormalTexture[] newVList = new VertexPositionNormalTexture[NumVertices];
            for (i = 0; i < NumVertices; i++) newVList[i] = VertexList[i];
            IndexBuffer IndexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short),
                                                            NumIndices, BufferUsage.WriteOnly);
            IndexBuffer.SetData(newTList);
            shapePrimitive = new ShapePrimitive(Material, new SharedShape.VertexBufferSet(newVList, viewer.GraphicsDevice), IndexBuffer, 0, NumVertices, NumIndices / 3, new[] { -1 }, 0);

        }

        Material FindMaterial(bool Alert)
        {
            string imageName = "";
            string globalText = Viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\";
            CABViewControlTypes controltype = CVFR.GetControlType();
            Material material = null;

            if (AceFile != "")
            {
                imageName = AceFile;
            }
            else if (Alert) { imageName = "alert.ace"; }
            else
            {
                switch (controltype)
                {
                    case CABViewControlTypes.CLOCK:
                        imageName = "clock.ace";
                        break;
                    case CABViewControlTypes.SPEEDLIMIT:
                    case CABViewControlTypes.SPEEDLIM_DISPLAY:
                        imageName = "speedlim.ace";
                        break;
                    case CABViewControlTypes.SPEED_PROJECTED:
                    case CABViewControlTypes.SPEEDOMETER:
                    default:
                        imageName = "speed.ace";
                        break;
                }
            }

            SceneryMaterialOptions options = SceneryMaterialOptions.ShaderFullBright | SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.UndergroundTexture;

            if (String.IsNullOrEmpty(TrainCarShape.SharedShape.ReferencePath))
            {
                if (!File.Exists(globalText + imageName))
                {
                    Trace.TraceInformation("Ignored missing " + imageName + " using default. You can copy the " + imageName + " from OR\'s AddOns folder to " + globalText +
                        ", or place it under " + TrainCarShape.SharedShape.ReferencePath);
                }
                material = Viewer.MaterialManager.Load("Scenery", Helpers.GetTextureFile(Viewer.Simulator, Helpers.TextureFlags.None, globalText, imageName), (int)(options), 0);
            }
            else
            {
                if (!File.Exists(TrainCarShape.SharedShape.ReferencePath + @"\" + imageName))
                {
                    Trace.TraceInformation("Ignored missing " + imageName + " using default. You can copy the " + imageName + " from OR\'s AddOns folder to " + globalText +
                        ", or place it under " + TrainCarShape.SharedShape.ReferencePath);
                    material = Viewer.MaterialManager.Load("Scenery", Helpers.GetTextureFile(Viewer.Simulator, Helpers.TextureFlags.None, globalText, imageName), (int)(options), 0);
                }
                else material = Viewer.MaterialManager.Load("Scenery", Helpers.GetTextureFile(Viewer.Simulator, Helpers.TextureFlags.None, TrainCarShape.SharedShape.ReferencePath + @"\", imageName), (int)(options), 0);
            }

            return material;
            //Material = Viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(Viewer.Simulator, Helpers.TextureFlags.None, "Speed"), (int)(SceneryMaterialOptions.None | SceneryMaterialOptions.AlphaBlendingBlend), 0);
        }

        //update the digits with current speed or time
        public void UpdateDigit()
        {
            NumVertices = NumIndices = 0;

            Material UsedMaterial = Material; //use default material

            //update text string
            bool Alert;
            string speed = CVFR.Get3DDigits(out Alert);

            if (Alert)//alert use alert meterial
            {
                if (AlertMaterial == null) AlertMaterial = FindMaterial(true);
                UsedMaterial = AlertMaterial;
            }
            //update vertex texture coordinate
            for (var j = 0; j < speed.Length; j++)
            {
                var tX = GetTextureCoordX(speed[j]); var tY = GetTextureCoordY(speed[j]);
                //create first triangle
                TriangleListIndices[NumIndices++] = (short)NumVertices;
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 1);
                // Second triangle:
                TriangleListIndices[NumIndices++] = (short)NumVertices;
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 3);
                TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);

                VertexList[NumVertices].TextureCoordinate.X = tX; VertexList[NumVertices].TextureCoordinate.Y = tY;
                VertexList[NumVertices + 1].TextureCoordinate.X = tX + 0.25f; VertexList[NumVertices + 1].TextureCoordinate.Y = tY;
                VertexList[NumVertices + 2].TextureCoordinate.X = tX + 0.25f; VertexList[NumVertices + 2].TextureCoordinate.Y = tY - 0.25f;
                VertexList[NumVertices + 3].TextureCoordinate.X = tX; VertexList[NumVertices + 3].TextureCoordinate.Y = tY - 0.25f;
                NumVertices += 4;
            }

            var i = 0;
            //create the new shape primitive
            short[] newTList = new short[NumIndices];
            for (i = 0; i < NumIndices; i++) newTList[i] = TriangleListIndices[i];
            VertexPositionNormalTexture[] newVList = new VertexPositionNormalTexture[NumVertices];
            for (i = 0; i < NumVertices; i++) newVList[i] = VertexList[i];
            IndexBuffer IndexBuffer = new IndexBuffer(Viewer.GraphicsDevice, typeof(short),
                                                            NumIndices, BufferUsage.WriteOnly);
            IndexBuffer.SetData(newTList);
            shapePrimitive = null;
            shapePrimitive = new ShapePrimitive(UsedMaterial, new SharedShape.VertexBufferSet(newVList, Viewer.GraphicsDevice), IndexBuffer, 0, NumVertices, NumIndices / 3, new[] { -1 }, 0);

        }


        //ACE MAP:
        // 0 1 2 3 
        // 4 5 6 7
        // 8 9 : 
        // . - a p
        static float GetTextureCoordX(char c)
        {
            float x = (c - '0') % 4 * 0.25f;
            if (c == '.') x = 0;
            else if (c == ':') x = 0.5f;
            else if (c == ' ') x = 0.75f;
            else if (c == '-') x = 0.25f;
            else if (c == 'a') x = 0.5f; //AM
            else if (c == 'p') x = 0.75f; //PM
            if (x < 0) x = 0;
            if (x > 1) x = 1;
            return x;
        }

        static float GetTextureCoordY(char c)
        {
            if (c == '0' || c == '1' || c == '2' || c == '3') return 0.25f;
            if (c == '4' || c == '5' || c == '6' || c == '7') return 0.5f;
            if (c == '8' || c == '9' || c == ':' || c == ' ') return 0.75f;
            return 1.0f;
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            UpdateDigit();
            Matrix mx = TrainCarShape.Location.XNAMatrix;
            mx.M41 += (TrainCarShape.Location.TileX - Viewer.Camera.TileX) * 2048;
            mx.M43 += (-TrainCarShape.Location.TileZ + Viewer.Camera.TileZ) * 2048;
            Matrix m = XNAMatrix * mx;

            // TODO: Make this use AddAutoPrimitive instead.
            frame.AddPrimitive(this.shapePrimitive.Material, this.shapePrimitive, RenderPrimitiveGroup.World, ref m, ShapeFlags.None);
        }

        internal void Mark()
        {
            shapePrimitive.Mark();
        }
    } // class ThreeDimCabDigit

    public class ThreeDimCabGaugeNative
    {
        PoseableShape TrainCarShape;
        VertexPositionNormalTexture[] VertexList;
        int NumVertices;
        int NumIndices;
        public short[] TriangleListIndices;// Array of indices to vertices for triangles
        Matrix XNAMatrix;
        Viewer Viewer;
        ShapePrimitive shapePrimitive;
        CabViewGaugeRenderer CVFR;
        Material PositiveMaterial;
        Material NegativeMaterial;
        float width, maxLen; //width of the gauge, and the max length of the gauge
        int Direction, Orientation;
        public ThreeDimCabGaugeNative(Viewer viewer, int iMatrix, string size, string len, PoseableShape trainCarShape, CabViewControlRenderer c)
        {
            if (size != string.Empty) width = float.Parse(size) / 1000f; //in mm
            if (len != string.Empty) maxLen = float.Parse(len) / 1000f; //in mm

            CVFR = (CabViewGaugeRenderer)c;
            Direction = CVFR.GetGauge().Direction;
            Orientation = CVFR.GetGauge().Orientation;

            Viewer = viewer;
            TrainCarShape = trainCarShape;
            XNAMatrix = TrainCarShape.SharedShape.Matrices[iMatrix];
            CVCGauge gauge = CVFR.GetGauge();
            var maxVertex = 4;// a rectangle
            //Material = viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(viewer.Simulator, Helpers.TextureFlags.None, texture), (int)(SceneryMaterialOptions.None | SceneryMaterialOptions.AlphaBlendingBlend), 0);

            // Create and populate a new ShapePrimitive
            NumVertices = NumIndices = 0;
            var Size = (float)gauge.Width;

            VertexList = new VertexPositionNormalTexture[maxVertex];
            TriangleListIndices = new short[maxVertex / 2 * 3]; // as is NumIndices

            var tX = 1f; var tY = 1f;

            //the left-bottom vertex
            Vertex v1 = new Vertex(0f, 0f, 0.002f, 0, 0, -1, tX, tY);

            //the right-bottom vertex
            Vertex v2 = new Vertex(0f, Size, 0.002f, 0, 0, -1, tX, tY);

            Vertex v3 = new Vertex(Size, 0, 0.002f, 0, 0, -1, tX, tY);

            Vertex v4 = new Vertex(Size, Size, 0.002f, 0, 0, -1, tX, tY);

            //create first triangle
            TriangleListIndices[NumIndices++] = (short)NumVertices;
            TriangleListIndices[NumIndices++] = (short)(NumVertices + 1);
            TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);
            // Second triangle:
            TriangleListIndices[NumIndices++] = (short)NumVertices;
            TriangleListIndices[NumIndices++] = (short)(NumVertices + 2);
            TriangleListIndices[NumIndices++] = (short)(NumVertices + 3);

            //create vertex
            VertexList[NumVertices].Position = v1.Position; VertexList[NumVertices].Normal = v1.Normal; VertexList[NumVertices].TextureCoordinate = v1.TexCoord;
            VertexList[NumVertices + 1].Position = v2.Position; VertexList[NumVertices + 1].Normal = v2.Normal; VertexList[NumVertices + 1].TextureCoordinate = v2.TexCoord;
            VertexList[NumVertices + 2].Position = v3.Position; VertexList[NumVertices + 2].Normal = v3.Normal; VertexList[NumVertices + 2].TextureCoordinate = v3.TexCoord;
            VertexList[NumVertices + 3].Position = v4.Position; VertexList[NumVertices + 3].Normal = v4.Normal; VertexList[NumVertices + 3].TextureCoordinate = v4.TexCoord;
            NumVertices += 4;


            var i = 0;
            //create the shape primitive
            short[] newTList = new short[NumIndices];
            for (i = 0; i < NumIndices; i++) newTList[i] = TriangleListIndices[i];
            VertexPositionNormalTexture[] newVList = new VertexPositionNormalTexture[NumVertices];
            for (i = 0; i < NumVertices; i++) newVList[i] = VertexList[i];
            IndexBuffer IndexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short),
                                                            NumIndices, BufferUsage.WriteOnly);
            IndexBuffer.SetData(newTList);
            shapePrimitive = new ShapePrimitive(FindMaterial(), new SharedShape.VertexBufferSet(newVList, viewer.GraphicsDevice), IndexBuffer, 0, NumVertices, NumIndices / 3, new[] { -1 }, 0);

        }

        Material FindMaterial()
        {
            bool Positive;
            color c = this.CVFR.GetColor(out Positive);
            if (Positive)
            {
                if (PositiveMaterial == null)
                {
                    PositiveMaterial = new SolidColorMaterial(this.Viewer, 0f, c.R, c.G, c.B);
                }
                return PositiveMaterial;
            }
            else
            {
                if (NegativeMaterial == null) NegativeMaterial = new SolidColorMaterial(this.Viewer, c.A, c.R, c.G, c.B);
                return NegativeMaterial;
            }
        }

        //update the digits with current speed or time
        public void UpdateDigit()
        {
            NumVertices = 0;

            Material UsedMaterial = FindMaterial();

            float length = CVFR.GetRangeFraction();

            CVCGauge gauge = CVFR.GetGauge();

            var len = maxLen * length;
            Vertex v1, v2, v3, v4;

            //the left-bottom vertex if ori=0;dir=0, right-bottom if ori=0,dir=1; left-top if ori=1,dir=0; left-bottom if ori=1,dir=1;
            v1 = new Vertex(0f, 0f, 0.002f, 0, 0, -1, 0f, 0f);

            if (Orientation == 0)
            {
                if (Direction == 0)//moving right
                {
                    //other vertices
                    v2 = new Vertex(0f, width, 0.002f, 0, 0, 1, 0f, 0f);
                    v3 = new Vertex(len, width, 0.002f, 0, 0, 1, 0f, 0f);
                    v4 = new Vertex(len, 0f, 0.002f, 0, 0, 1, 0f, 0f);
                }
                else //moving left
                {
                    v4 = new Vertex(0f, width, 0.002f, 0, 0, 1, 0f, 0f);
                    v3 = new Vertex(-len, width, 0.002f, 0, 0, 1, 0f, 0f);
                    v2 = new Vertex(-len, 0f, 0.002f, 0, 0, 1, 0f, 0f);
                }
            }
            else
            {
                if (Direction == 1)//up
                {
                    //other vertices
                    v2 = new Vertex(0f, len, 0.002f, 0, 0, 1, 0f, 0f);
                    v3 = new Vertex(width, len, 0.002f, 0, 0, 1, 0f, 0f);
                    v4 = new Vertex(width, 0f, 0.002f, 0, 0, 1, 0f, 0f);
                }
                else //moving down
                {
                    v4 = new Vertex(0f, -len, 0.002f, 0, 0, 1, 0f, 0f);
                    v3 = new Vertex(width, -len, 0.002f, 0, 0, 1, 0f, 0f);
                    v2 = new Vertex(width, 0, 0.002f, 0, 0, 1, 0f, 0f);
                }
            }

            //create vertex list
            VertexList[NumVertices].Position = v1.Position; VertexList[NumVertices].Normal = v1.Normal; VertexList[NumVertices].TextureCoordinate = v1.TexCoord;
            VertexList[NumVertices + 1].Position = v2.Position; VertexList[NumVertices + 1].Normal = v2.Normal; VertexList[NumVertices + 1].TextureCoordinate = v2.TexCoord;
            VertexList[NumVertices + 2].Position = v3.Position; VertexList[NumVertices + 2].Normal = v3.Normal; VertexList[NumVertices + 2].TextureCoordinate = v3.TexCoord;
            VertexList[NumVertices + 3].Position = v4.Position; VertexList[NumVertices + 3].Normal = v4.Normal; VertexList[NumVertices + 3].TextureCoordinate = v4.TexCoord;
            NumVertices += 4;

            var i = 0;
            //create the new shape primitive
            short[] newTList = new short[NumIndices];
            for (i = 0; i < NumIndices; i++) newTList[i] = TriangleListIndices[i];
            VertexPositionNormalTexture[] newVList = new VertexPositionNormalTexture[NumVertices];
            for (i = 0; i < NumVertices; i++) newVList[i] = VertexList[i];
            IndexBuffer IndexBuffer = new IndexBuffer(Viewer.GraphicsDevice, typeof(short),
                                                            NumIndices, BufferUsage.WriteOnly);
            IndexBuffer.SetData(newTList);
            shapePrimitive = null;
            shapePrimitive = new ShapePrimitive(UsedMaterial, new SharedShape.VertexBufferSet(newVList, Viewer.GraphicsDevice), IndexBuffer, 0, NumVertices, NumIndices / 3, new[] { -1 }, 0);

        }


        //ACE MAP:
        // 0 1 2 3 
        // 4 5 6 7
        // 8 9 : 
        // . - a p
        static float GetTextureCoordX(char c)
        {
            float x = (c - '0') % 4 * 0.25f;
            if (c == '.') x = 0;
            else if (c == ':') x = 0.5f;
            else if (c == ' ') x = 0.75f;
            else if (c == '-') x = 0.25f;
            else if (c == 'a') x = 0.5f; //AM
            else if (c == 'p') x = 0.75f; //PM
            if (x < 0) x = 0;
            if (x > 1) x = 1;
            return x;
        }

        static float GetTextureCoordY(char c)
        {
            if (c == '0' || c == '1' || c == '2' || c == '3') return 0.25f;
            if (c == '4' || c == '5' || c == '6' || c == '7') return 0.5f;
            if (c == '8' || c == '9' || c == ':' || c == ' ') return 0.75f;
            return 1.0f;
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            UpdateDigit();
            Matrix mx = TrainCarShape.Location.XNAMatrix;
            mx.M41 += (TrainCarShape.Location.TileX - Viewer.Camera.TileX) * 2048;
            mx.M43 += (-TrainCarShape.Location.TileZ + Viewer.Camera.TileZ) * 2048;
            Matrix m = XNAMatrix * mx;

            // TODO: Make this use AddAutoPrimitive instead.
            frame.AddPrimitive(this.shapePrimitive.Material, this.shapePrimitive, RenderPrimitiveGroup.World, ref m, ShapeFlags.None);
        }

        internal void Mark()
        {
            shapePrimitive.Mark();
        }
    } // class ThreeDimCabDigit
    public class ThreeDimCabGauge
    {
        PoseableShape TrainCarShape;
        public short[] TriangleListIndices;// Array of indices to vertices for triangles
        Viewer Viewer;
        int matrixIndex;
        CabViewGaugeRenderer CVFR;
        Matrix XNAMatrix;
        float GaugeSize;
        public ThreeDimCabGauge(Viewer viewer, int iMatrix, float gaugeSize, PoseableShape trainCarShape, CabViewControlRenderer c)
        {
            CVFR = (CabViewGaugeRenderer)c;
            Viewer = viewer;
            TrainCarShape = trainCarShape;
            matrixIndex = iMatrix;
            XNAMatrix = TrainCarShape.SharedShape.Matrices[iMatrix];
            GaugeSize = gaugeSize / 1000f; //how long is the scale 1? since OR cannot allow fraction number in part names, have to define it as mm
        }



        /// <summary>
        /// Transition the part toward the specified state. 
        /// </summary>
        public void Update(MSTSLocomotiveViewer locoViewer, ElapsedTime elapsedTime)
        {
            if (!locoViewer._has3DCabRenderer) return;

            var scale = CVFR.GetRangeFraction();

            if (CVFR.GetStyle() == CABViewControlStyles.POINTER)
            {
                this.TrainCarShape.XNAMatrices[matrixIndex] = Matrix.CreateTranslation(scale * this.GaugeSize, 0, 0) * this.TrainCarShape.SharedShape.Matrices[matrixIndex];
            }
            else
            {
                this.TrainCarShape.XNAMatrices[matrixIndex] = Matrix.CreateScale(scale * 10, 1, 1) * this.TrainCarShape.SharedShape.Matrices[matrixIndex];
            }
            //this.TrainCarShape.SharedShape.Matrices[matrixIndex] = XNAMatrix * mx * Matrix.CreateRotationX(10);
        }

    } // class ThreeDimCabGauge

    public class DigitalDisplay
    {
        Viewer Viewer;
        private SpriteBatchMaterial _Sprite2DCabView;
        WindowTextFont _Font;
        PoseableShape TrainCarShape = null;
        int digitPart;
        int height;
        Point coor = new Point(0, 0);
        CabViewDigitalRenderer CVFR;
        //		Color color;
        public DigitalDisplay(Viewer viewer, PoseableShape t, int d, int h, CabViewControlRenderer c)
        {
            TrainCarShape = t;
            Viewer = viewer;
            digitPart = d;
            height = h;
            CVFR = (CabViewDigitalRenderer)c;
            _Sprite2DCabView = (SpriteBatchMaterial)viewer.MaterialManager.Load("SpriteBatch");
            _Font = viewer.WindowManager.TextManager.GetExact("Arial", height, System.Drawing.FontStyle.Regular);
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
        public CABViewControlTypes Type;
        public int Key;
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
            if (MatrixIndexes.Count == 0 || !locoViewer._has3DCabRenderer) return;

            CabViewControlRenderer cvfr;
            float index;
            try
            {
                cvfr = locoViewer.ThreeDimentionCabRenderer.ControlMap[Key];
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
