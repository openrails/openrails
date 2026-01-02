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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Popups;
using Orts.Viewer3D.RollingStock.SubSystems;
using Orts.Viewer3D.RollingStock.Subsystems.ETCS;
using ORTS.Common;
using ORTS.Common.Input;
using ORTS.Scripting.Api;
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
        CruiseControlViewer CruiseControlViewer;

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
            if (Locomotive.CruiseControl != null)
            {
                CruiseControlViewer = new CruiseControlViewer(this, Locomotive, Locomotive.CruiseControl);
                CruiseControlViewer.InitializeUserInputCommands();
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
            || Math.Abs(Locomotive.SpeedMpS) > 1 || Locomotive.DynamicBrakeController?.CurrentValue > 0))
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
            || Math.Abs(Locomotive.SpeedMpS) > 1 || Locomotive.DynamicBrakeController?.CurrentValue > 0))
            {
                Viewer.Simulator.Confirmer.Warning(CabControl.Reverser, CabSetting.Warn1);
                return;
            }
            new ReverserCommand(Viewer.Log, false);    // No harm in trying to engage Reverse when already engaged.
        }

        public override void InitializeUserInputCommands()
        {
            // Steam locomotives handle these differently, and might have set them already
            if (!UserInputCommands.ContainsKey(UserCommand.ControlForwards))
                UserInputCommands.Add(UserCommand.ControlForwards, new Action[] { Noop, () => ReverserControlForwards() });
            if (!UserInputCommands.ContainsKey(UserCommand.ControlBackwards))
                UserInputCommands.Add(UserCommand.ControlBackwards, new Action[] { Noop, () => ReverserControlBackwards() });

            UserInputCommands.Add(UserCommand.ControlThrottleIncrease, new Action[] { () => Locomotive.StopThrottleIncrease(), () => Locomotive.StartThrottleIncrease() });
            UserInputCommands.Add(UserCommand.ControlThrottleDecrease, new Action[] { () => Locomotive.StopThrottleDecrease(), () => Locomotive.StartThrottleDecrease() });
            UserInputCommands.Add(UserCommand.ControlThrottleZero, new Action[] { Noop, () => Locomotive.ThrottleToZero() });
            UserInputCommands.Add(UserCommand.ControlGearUp, new Action[] { () => StopGearBoxIncrease(), () => StartGearBoxIncrease() });
            UserInputCommands.Add(UserCommand.ControlGearDown, new Action[] { () => StopGearBoxDecrease(), () => StartGearBoxDecrease() });
            UserInputCommands.Add(UserCommand.ControlTrainBrakeIncrease, new Action[] { () => Locomotive.StopTrainBrakeIncrease(), () => Locomotive.StartTrainBrakeIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlTrainBrakeDecrease, new Action[] { () => Locomotive.StopTrainBrakeDecrease(), () => Locomotive.StartTrainBrakeDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlTrainBrakeZero, new Action[] { Noop, () => Locomotive.StartTrainBrakeDecrease(0, true) });
            UserInputCommands.Add(UserCommand.ControlEngineBrakeIncrease, new Action[] { () => Locomotive.StopEngineBrakeIncrease(), () => Locomotive.StartEngineBrakeIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlEngineBrakeDecrease, new Action[] { () => Locomotive.StopEngineBrakeDecrease(), () => Locomotive.StartEngineBrakeDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlBrakemanBrakeIncrease, new Action[] { () => Locomotive.StopBrakemanBrakeIncrease(), () => Locomotive.StartBrakemanBrakeIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlBrakemanBrakeDecrease, new Action[] { () => Locomotive.StopBrakemanBrakeDecrease(), () => Locomotive.StartBrakemanBrakeDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlDynamicBrakeIncrease, new Action[] { () => Locomotive.StopDynamicBrakeIncrease(), () => Locomotive.StartDynamicBrakeIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlDynamicBrakeDecrease, new Action[] { () => Locomotive.StopDynamicBrakeDecrease(), () => Locomotive.StartDynamicBrakeDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlSteamHeatIncrease, new Action[] { () => Locomotive.StopSteamHeatIncrease(), () => Locomotive.StartSteamHeatIncrease(null) });
            UserInputCommands.Add(UserCommand.ControlSteamHeatDecrease, new Action[] { () => Locomotive.StopSteamHeatDecrease(), () => Locomotive.StartSteamHeatDecrease(null) });
            UserInputCommands.Add(UserCommand.ControlBailOff, new Action[] { () => new BailOffCommand(Viewer.Log, false), () => new BailOffCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlBrakeQuickRelease, new Action[] { () => new QuickReleaseCommand(Viewer.Log, false), () => new QuickReleaseCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlBrakeOvercharge, new Action[] { () => new BrakeOverchargeCommand(Viewer.Log, false), () => new BrakeOverchargeCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlInitializeBrakes, new Action[] { Noop, () => new InitializeBrakesCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlHandbrakeNone, new Action[] { Noop, () => new HandbrakeCommand(Viewer.Log, false) });
            UserInputCommands.Add(UserCommand.ControlHandbrakeFull, new Action[] { Noop, () => new HandbrakeCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlRetainersOff, new Action[] { Noop, () => new RetainersCommand(Viewer.Log, false) });
            UserInputCommands.Add(UserCommand.ControlRetainersOn, new Action[] { Noop, () => new RetainersCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlBrakeHoseConnect, new Action[] { Noop, () => new BrakeHoseConnectCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlBrakeHoseDisconnect, new Action[] { Noop, () => new BrakeHoseConnectCommand(Viewer.Log, false) });
            UserInputCommands.Add(UserCommand.ControlEmergencyPushButton, new Action[] { Noop, () => new EmergencyPushButtonCommand(Viewer.Log, !Locomotive.EmergencyButtonPressed) });
            UserInputCommands.Add(UserCommand.ControlEOTEmergencyBrake, new Action[] { Noop, () => new ToggleEOTEmergencyBrakeCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlSander, new Action[] { () => new SanderCommand(Viewer.Log, false), () => new SanderCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlSanderToggle, new Action[] { Noop, () => new SanderCommand(Viewer.Log, !Locomotive.Sander) });
            UserInputCommands.Add(UserCommand.ControlWiper, new Action[] { Noop, () => new WipersCommand(Viewer.Log, !Locomotive.Wiper) });
            UserInputCommands.Add(UserCommand.ControlHorn, new Action[] { () => new HornCommand(Viewer.Log, false), () => new HornCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlBell, new Action[] { () => new BellCommand(Viewer.Log, false), () => new BellCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlBellToggle, new Action[] { Noop, () => new BellCommand(Viewer.Log, !Locomotive.ManualBell) });
            UserInputCommands.Add(UserCommand.ControlAlerter, new Action[] { () => new AlerterCommand(Viewer.Log, false), () => new AlerterCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlHeadlightIncrease, new Action[] { Noop, () => new HeadlightCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlHeadlightDecrease, new Action[] { Noop, () => new HeadlightCommand(Viewer.Log, false) });
            UserInputCommands.Add(UserCommand.ControlLight, new Action[] { Noop, () => new ToggleCabLightCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlRefill, new Action[] { () => StopRefillingOrUnloading(Viewer.Log), () => AttemptToRefillOrUnload() });
            UserInputCommands.Add(UserCommand.ControlDiscreteUnload, new Action[] { () => StopRefillingOrUnloading(Viewer.Log, true), () => AttemptToRefillOrUnload(true) });
            UserInputCommands.Add(UserCommand.ControlImmediateRefill, new Action[] { () => StopImmediateRefilling(Viewer.Log), () => ImmediateRefill() });
            UserInputCommands.Add(UserCommand.ControlWaterScoop, new Action[] { Noop, () => new ToggleWaterScoopCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlOdoMeterShowHide, new Action[] { Noop, () => new ToggleOdometerCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlOdoMeterReset, new Action[] { () => new ResetOdometerCommand(Viewer.Log, false), () => new ResetOdometerCommand(Viewer.Log, true) });
            UserInputCommands.Add(UserCommand.ControlOdoMeterDirection, new Action[] { Noop, () => new ToggleOdometerDirectionCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlCabRadio, new Action[] { Noop, () => new CabRadioCommand(Viewer.Log, !Locomotive.CabRadioOn) });
            UserInputCommands.Add(UserCommand.ControlDieselHelper, new Action[] { Noop, () => new ToggleHelpersEngineCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlGenericItem1, new Action[] { Noop, () => new ToggleGenericItem1Command(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlGenericItem2, new Action[] { Noop, () => new ToggleGenericItem2Command(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlTCSGeneric1, new Action[] {
                () => new TCSButtonCommand(Viewer.Log, false, 0),
                () => {
                    new TCSButtonCommand(Viewer.Log, true, 0);
                    Locomotive.TrainControlSystem.TCSCommandSwitchOn.TryGetValue(0, out bool wasPressed);
                    new TCSSwitchCommand(Viewer.Log, !wasPressed, 0);
                }
            });
            UserInputCommands.Add(UserCommand.ControlTCSGeneric2, new Action[] {
                () => new TCSButtonCommand(Viewer.Log, false, 1),
                () => {
                    new TCSButtonCommand(Viewer.Log, true, 1);
                    Locomotive.TrainControlSystem.TCSCommandSwitchOn.TryGetValue(1, out bool wasPressed);
                    new TCSSwitchCommand(Viewer.Log, !wasPressed, 1);
                }
            });

            //Distributed power
            UserInputCommands.Add(UserCommand.ControlDPMoveToFront, new Action[] { Noop, () => new DPMoveToFrontCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlDPMoveToBack, new Action[] { Noop, () => new DPMoveToBackCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlDPTraction, new Action[] { Noop, () => new DPTractionCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlDPIdle, new Action[] { Noop, () => new DPIdleCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlDPBrake, new Action[] { Noop, () => new DPDynamicBrakeCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlDPMore, new Action[] { Noop, () => new DPMoreCommand(Viewer.Log) });
            UserInputCommands.Add(UserCommand.ControlDPLess, new Action[] { Noop, () => new DPLessCommand(Viewer.Log) });
            
            base.InitializeUserInputCommands();
        }

        /// <summary>
        /// A keyboard or mouse click has occurred. Read the UserInput
        /// structure to determine what was pressed.
        /// </summary>
        public override void HandleUserInput(ElapsedTime elapsedTime)
        {
            if (UserInput.IsPressed(UserCommand.CameraToggleShowCab))
                Locomotive.ShowCab = !Locomotive.ShowCab;

            // By Matej Pacha
            if (UserInput.IsPressed(UserCommand.DebugResetWheelSlip)) { Locomotive.Train.SignalEvent(Event._ResetWheelSlip); }
            if (UserInput.IsPressed(UserCommand.DebugToggleAdvancedAdhesion)) { Locomotive.Train.SignalEvent(Event._ResetWheelSlip); Locomotive.Simulator.UseAdvancedAdhesion = !Locomotive.Simulator.UseAdvancedAdhesion; }

            ExternalDeviceState[] externalDevices = {UserInput.RDState, UserInput.WebDeviceState};
            foreach (var external in externalDevices)
            {
                if (external == null) continue;
                // Handle other cabcontrols
                foreach (var kvp in external.CabControls)
                {
                    if (!kvp.Value.Changed) continue;
                    float val = kvp.Value.Value;
                    switch (kvp.Key.Item1.Type)
                    {
                        // Some cab controls need specific handling for better results
                        case CABViewControlTypes.THROTTLE:
                            Locomotive.SetThrottlePercentWithSound(val * 100);
                            break;
                        case CABViewControlTypes.DIRECTION:
                            if (Locomotive is MSTSSteamLocomotive steam)
                            {
                                steam.SetCutoffPercent(val * 100);
                            }
                            else if (val > 0.5f)
                                Locomotive.SetDirection(Direction.Forward);
                            else if (val < -0.5f)
                                Locomotive.SetDirection(Direction.Reverse);
                            else
                                Locomotive.SetDirection(Direction.N);
                            break;
                        case CABViewControlTypes.TRAIN_BRAKE:
                            Locomotive.SetTrainBrakePercent(val * 100);
                            break;
                        case CABViewControlTypes.DYNAMIC_BRAKE:
                            if (Locomotive.CombinedControlType != MSTSLocomotive.CombinedControl.ThrottleAir)
                                Locomotive.SetDynamicBrakePercentWithSound(val * 100);
                            break;
                        case CABViewControlTypes.ENGINE_BRAKE:
                            Locomotive.SetEngineBrakePercent(val * 100);
                            break;
                        case CABViewControlTypes.FRONT_HLIGHT:
                            // changing Headlight more than one step at a time doesn't work for some reason
                            if (Locomotive.Headlight < val - 1)
                            {
                                Locomotive.Headlight++;
                                Locomotive.SignalEvent(Event.LightSwitchToggle);
                            }
                            if (Locomotive.Headlight > val - 1)
                            {
                                Locomotive.Headlight--;
                                Locomotive.SignalEvent(Event.LightSwitchToggle);
                            }
                            break;
                        case CABViewControlTypes.ORTS_SELECTED_SPEED_SELECTOR:
                            Locomotive.CruiseControl.SelectedSpeedMpS = val;
                            break;
                        case CABViewControlTypes.WIPERS:
                            if (val == 0 && Locomotive.Wiper)
                                Locomotive.SignalEvent(Event.WiperOff);
                            if (val != 0 && !Locomotive.Wiper)
                                Locomotive.SignalEvent(Event.WiperOn);
                            break;
                        // Other controls can hopefully be controlled faking mouse input
                        // TODO: refactor HandleUserInput() 
                        default:
                            var cabRenderer = ThreeDimentionCabRenderer ?? _CabRenderer;
                            if (cabRenderer != null && cabRenderer.ControlMap.TryGetValue(kvp.Key, out var renderer) && renderer is CabViewDiscreteRenderer discrete)
                            {
                                var oldChanged = discrete.ChangedValue;
                                discrete.ChangedValue = (oldval) => val;
                                discrete.HandleUserInput();
                                discrete.ChangedValue = oldChanged;
                            }
                            break;
                    }
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
            }

            // Wipers and bell animation
            Wipers.UpdateLoop(Locomotive.Wiper, elapsedTime);
            Bell.UpdateLoop(Locomotive.Bell, elapsedTime, TrainCarShape.SharedShape.CustomAnimationFPS);
            Item1Continuous.UpdateLoop(Locomotive.GenericItem1, elapsedTime, TrainCarShape.SharedShape.CustomAnimationFPS);
            Item2Continuous.UpdateLoop(Locomotive.GenericItem2, elapsedTime, TrainCarShape.SharedShape.CustomAnimationFPS);

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

        /// <summary>
        /// Checks this locomotive viewer for stale shapes and sets the stale data flag if any shapes are stale
        /// </summary>
        /// <returns>bool indicating if this viewer changed from fresh to stale</returns>
        public override bool CheckStaleShapes()
        {
            if (!Car.StaleViewer)
            {
                // Locomotive may have a 3D cab, which could itself have a stale shape
                if (!base.CheckStaleShapes() && ThreeDimentionCabViewer != null && ThreeDimentionCabViewer.CheckStale())
                    Car.StaleViewer = true;

                return Car.StaleViewer;
            }
            else
                return false;
        }

        /// <summary>
        /// Checks this locomotive viewer for stale directly-referenced textures and sets the stale data flag if any textures are stale
        /// </summary>
        /// <returns>bool indicating if this viewer changed from fresh to stale</returns>
        public override bool CheckStaleTextures()
        {
            if (!Car.StaleViewer)
            {
                // Locomotive viewer also handles cabviews,
                // check if any cab view textures are stale now
                if (!base.CheckStaleTextures() && _CabRenderer != null && _CabRenderer.CheckStale())
                    Car.StaleViewer = true;

                return Car.StaleViewer;
            }
            else
                return false;
        }

        internal override void Mark()
        {
            _CabRenderer?.Mark();
            ThreeDimentionCabViewer?.Mark();

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
            {(uint)MSTSWagon.PickupType.SpecialMail, Viewer.Catalog.GetString("mail")},
            {(uint)MSTSWagon.PickupType.Container, Viewer.Catalog.GetString("container")}
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
        WagonAndMatchingPickup GetMatchingPickup(Train train, bool onlyUnload = false)
        {
            var worldFiles = Viewer.World.Scenery.WorldFiles;
            var shortestD2 = float.MaxValue;
            WagonAndMatchingPickup nearestPickup = null;
            float distanceFromFrontOfTrainM = 0f;
            int index = 0;
            ContainerHandlingItem containerStation = null;
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
                                    if (intake.Type == MSTSWagon.PickupType.Container)
                                    {
                                        if (!intake.Validity(onlyUnload, pickup, Viewer.Simulator.ContainerManager, wagon.FreightAnimations, out containerStation))
                                            continue;
                                    }
                                    var intakePosition = new Vector3(0, 0, -intake.OffsetM);
                                    Vector3.Transform(ref intakePosition, ref car.WorldPosition.XNAMatrix, out intakePosition);

                                    var intakeLocation = new WorldLocation(
                                        car.WorldPosition.TileX, car.WorldPosition.TileZ,
                                        intakePosition.X, intakePosition.Y, -intakePosition.Z);

                                    var d2 = WorldLocation.GetDistanceSquared(intakeLocation, pickup.Location);
                                    if (intake.Type == MSTSWagon.PickupType.Container && containerStation != null && 
                                        (wagon.Train.FrontTDBTraveller.TN.Index == containerStation.TrackNode.Index ||
                                        wagon.Train.RearTDBTraveller.TN.Index == containerStation.TrackNode.Index) &&
                                        d2 < containerStation.MinZSpan * containerStation.MinZSpan)
                                    // for container it's enough if the intake is within the reachable range of the container crane
                                    {
                                        nearestPickup = new WagonAndMatchingPickup();
                                        nearestPickup.Pickup = pickup;
                                        nearestPickup.Wagon = wagon;
                                        nearestPickup.IntakePoint = intake;
                                        return nearestPickup;
                                    }

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
                // Electric locomotives may have steam heat boilers fitted, and they can refill these
                if (car is MSTSDieselLocomotive || car is MSTSSteamLocomotive || (car is MSTSElectricLocomotive && loco.IsSteamHeatFitted))
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
        public void AttemptToRefillOrUnload(bool onlyUnload = false)
        {
            MatchedWagonAndPickup = null;   // Ensures that releasing the T key doesn't do anything unless there is something to do.

            var loco = this.Locomotive;

            var match = GetMatchingPickup(loco.Train, onlyUnload);
            if (match == null && !(loco is MSTSElectricLocomotive && loco.IsSteamHeatFitted))
                return;
            if (match == null)
            {
                Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetString("Refill: Electric loco and no pickup. Command rejected"));
                return;
            }
            float distanceToPickupM = GetDistanceToM(match);
            if (match.IntakePoint.LinkedFreightAnim != null && match.IntakePoint.LinkedFreightAnim is FreightAnimationDiscrete)
                // for container cranes handle distance management using Z span of crane
            {
                var containerStation = Viewer.Simulator.ContainerManager.ContainerHandlingItems.Where(item => item.Key == match.Pickup.TrItemIDList[0].dbID).Select(item => item.Value).First();
                if (distanceToPickupM > containerStation.MinZSpan)
                {
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Container crane: Distance to {0} supply is {1}.",
                        PickupTypeDictionary[(uint)match.Pickup.PickupType], Viewer.Catalog.GetPluralStringFmt("{0} meter", "{0} meters", (long)(distanceToPickupM + 0.5f))));
                    return;
                }
                MSTSWagon.RefillProcess.ActivePickupObjectUID = (int)match.Pickup.UID;
            }
            else
            {
                distanceToPickupM -= 2.5f; // Deduct an extra 2.5 so that the tedious placement is less of an issue.
                if (distanceToPickupM > match.IntakePoint.WidthM / 2)
                {
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("Refill: Distance to {0} supply is {1}.",
                        PickupTypeDictionary[(uint)match.Pickup.PickupType], Viewer.Catalog.GetPluralStringFmt("{0} meter", "{0} meters", (long)(distanceToPickupM + 1f))));
                    return;
                }
                if (distanceToPickupM <= match.IntakePoint.WidthM / 2)
                    MSTSWagon.RefillProcess.ActivePickupObjectUID = (int)match.Pickup.UID;
            }

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
            if (match.Wagon is MSTSDieselLocomotive || match.Wagon is MSTSSteamLocomotive || match.Wagon is MSTSElectricLocomotive || (match.Wagon.WagonType == TrainCar.WagonTypes.Tender && match.SteamLocomotiveWithTender != null))
            {
                // Note: The tender contains the intake information, but the steam locomotive includes the controller information that is needed for the refueling process.

                float fraction = 0;

                // classical MSTS Freightanim, handled as usual
                if (match.SteamLocomotiveWithTender != null)
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
            else if (match.Wagon.FreightAnimations?.Animations?.Count > 0 && match.Wagon.FreightAnimations.Animations[0] is FreightAnimationContinuous)
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
            else if (match.IntakePoint.LinkedFreightAnim is FreightAnimationDiscrete)
            {
                var load = match.IntakePoint.LinkedFreightAnim as FreightAnimationDiscrete;
                // discrete freight wagon animation
                if (load == null)
                {
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("wag file not equipped for containers"));
                    return;
                }
                else if (load.Loaded && !onlyUnload)
                {
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("{0} now loaded.",
                        PickupTypeDictionary[match.Pickup.PickupType]));
                    return;
                }
                else if (!load.Loaded && onlyUnload)
                {
                    Viewer.Simulator.Confirmer.Message(ConfirmLevel.None, Viewer.Catalog.GetStringFmt("{0} now unloaded.",
                        PickupTypeDictionary[match.Pickup.PickupType]));
                    return;
                }

                MSTSWagon.RefillProcess.OkToRefill = true;
                MSTSWagon.RefillProcess.Unload = onlyUnload;
                match.Wagon.StartLoadingOrUnloading(match.Pickup, match.IntakePoint, MSTSWagon.RefillProcess.Unload);
                MatchedWagonAndPickup = match;  // Save away for HandleUserInput() to use when key is released.

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
        public void StopRefillingOrUnloading(CommandLog log, bool onlyUnload = true)
        {
            if (MatchedWagonAndPickup == null)
                return;
            if (MatchedWagonAndPickup.Pickup.PickupType == (uint)MSTSWagon.PickupType.Container)
                return;
            MSTSWagon.RefillProcess.OkToRefill = false;
            MSTSWagon.RefillProcess.ActivePickupObjectUID = 0;
            var match = MatchedWagonAndPickup;
            if (MatchedWagonAndPickup.Pickup.PickupType == (uint)MSTSWagon.PickupType.Container)
            {
                match.Wagon.UnloadingPartsOpen = false;
                return;
            }
            var controller = new MSTSNotchController();
            if (match.Wagon is MSTSElectricLocomotive || match.Wagon is MSTSDieselLocomotive || match.Wagon is MSTSSteamLocomotive || (match.Wagon.WagonType == TrainCar.WagonTypes.Tender && match.SteamLocomotiveWithTender != null))
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
        private static Dictionary<string, SharedTexture> DayTextures = new Dictionary<string, SharedTexture>();
        private static Dictionary<string, SharedTexture> NightTextures = new Dictionary<string, SharedTexture>();
        private static Dictionary<string, SharedTexture> LightTextures = new Dictionary<string, SharedTexture>();
        private static Dictionary<string, Texture2D[]> PDayTextures = new Dictionary<string, Texture2D[]>();
        private static Dictionary<string, Texture2D[]> PNightTextures = new Dictionary<string, Texture2D[]>();
        private static Dictionary<string, Texture2D[]> PLightTextures = new Dictionary<string, Texture2D[]>();

        /// <summary>
        /// Loads a texture, day night and cablight
        /// </summary>
        /// <param name="viewer">Viewer3D</param>
        /// <param name="fileName">Name of the Texture</param>
        /// <returns>bool indicating if this cabview has a cablight directory</returns>
        public static bool LoadTextures(Viewer viewer, string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            // Use resolved path, without any 'up one level' ("..\\") calls
            fileName = Path.GetFullPath(fileName).ToLowerInvariant();

            bool stale = GetStale(fileName);

            string nightpath = Path.Combine(Path.Combine(Path.GetDirectoryName(fileName), "night"), Path.GetFileName(fileName));

            string lightdirectory = Path.Combine(Path.GetDirectoryName(fileName), "cablight");
            string lightpath = Path.Combine(lightdirectory, Path.GetFileName(fileName));

            // Get the texture if we don't have it yet, or if it's stale
            if (!DayTextures.ContainsKey(fileName) || stale)
            {
                DayTextures[fileName] = viewer.TextureManager.Get(fileName, true);

                NightTextures[fileName] = viewer.TextureManager.Get(nightpath);

                LightTextures[fileName] = viewer.TextureManager.Get(lightpath);
            }
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
                    if (retval == SharedMaterialManager.MissingTexture.Texture)
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
                isNightTexture = retval != SharedMaterialManager.MissingTexture.Texture;
            }

            // No light or dark texture selected/available? Use day texture instead.
            if (retval == SharedMaterialManager.MissingTexture.Texture)
                retval = DayTextures[FileName];

            return retval;
        }

        /// <summary>
        /// Returns the StaleData flag for the given texture file, including night and cablight variants
        /// (or false if this texture is not loaded)
        /// </summary>
        /// <returns>bool indicating if the given texture is marked as stale</returns>
        public static bool GetStale(string texPath)
        {
            texPath = texPath;

            bool stale = (DayTextures.ContainsKey(texPath) && DayTextures[texPath].StaleData) ||
                (NightTextures.ContainsKey(texPath) && NightTextures[texPath].StaleData) ||
                (LightTextures.ContainsKey(texPath) && LightTextures[texPath].StaleData);

            return stale;
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
        private Matrix _Scale = Matrix.Identity;
        private Texture2D _CabTexture;
        private readonly Texture2D _LetterboxTexture;
        private CabShader _Shader;

        private Point _PrevScreenSize;

        //private List<CabViewControls> CabViewControlsList = new List<CabViewControls>();
        private List<List<CabViewControlRenderer>> CabViewControlRenderersList = new List<List<CabViewControlRenderer>>();
        private Viewer _Viewer;
        private MSTSLocomotive _Locomotive;
        private int _Location;
        private bool _isNightTexture;
        private bool HasCabLightDirectory = false;
        public Dictionary<(CabViewControlType, int), CabViewControlRenderer> ControlMap;
        public string[] ActiveScreen = { "default", "default", "default", "default", "default", "default", "default", "default" };

        [CallOnThread("Loader")]
        public CabRenderer(Viewer viewer, MSTSLocomotive car)
        {
            //Sequence = RenderPrimitiveSequence.CabView;
            _Viewer = viewer;
            _Locomotive = car;

            // _Viewer.DisplaySize intercepted to adjust cab view height
            Point DisplaySize = _Viewer.DisplaySize;
            DisplaySize.Y = _Viewer.CabHeightPixels;

            _PrevScreenSize = DisplaySize;

            _LetterboxTexture = new Texture2D(viewer.GraphicsDevice, 1, 1);
            _LetterboxTexture.SetData(new Color[] { Color.Black });

            // Use same shader for both front-facing and rear-facing cabs.
            if (_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF != null)
            {
                _Shader = new CabShader(viewer.GraphicsDevice,
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF.Light1Position, DisplaySize),
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF.Light2Position, DisplaySize),
                    ExtendedCVF.TranslatedColor(_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF.Light1Color),
                    ExtendedCVF.TranslatedColor(_Locomotive.CabViewList[(int)CabViewType.Front].ExtendedCVF.Light2Color));
            }

            _Sprite2DCabView = (SpriteBatchMaterial)viewer.MaterialManager.Load("SpriteBatch", effect: _Shader);

            #region Create Control renderers
            ControlMap = new Dictionary<(CabViewControlType, int), CabViewControlRenderer>();
            var count = new Dictionary<CabViewControlType, int>();
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
                        if (!count.ContainsKey(cvc.ControlType)) count[cvc.ControlType] = 0;
                        var key = (cvc.ControlType, count[cvc.ControlType]);
                        CVCDial dial = cvc as CVCDial;
                        if (dial != null)
                        {
                            CabViewDialRenderer cvcr = new CabViewDialRenderer(viewer, car, dial, _Shader);
                            cvcr.SortIndex = controlSortIndex;
                            CabViewControlRenderersList[i].Add(cvcr);
                            if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvcr);
                            count[cvc.ControlType]++;
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
                            count[cvc.ControlType]++;
                            continue;
                        }
                        CVCSignal asp = cvc as CVCSignal;
                        if (asp != null)
                        {
                            CabViewDiscreteRenderer aspr = new CabViewDiscreteRenderer(viewer, car, asp, _Shader);
                            aspr.SortIndex = controlSortIndex;
                            CabViewControlRenderersList[i].Add(aspr);
                            if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, aspr);
                            count[cvc.ControlType]++;
                            continue;
                        }
                        CVCAnimatedDisplay anim = cvc as CVCAnimatedDisplay;
                        if (anim != null)
                        {
                            CabViewAnimationsRenderer animr = new CabViewAnimationsRenderer(viewer, car, anim, _Shader);
                            animr.SortIndex = controlSortIndex;
                            CabViewControlRenderersList[i].Add(animr);
                            if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, animr);
                            count[cvc.ControlType]++;
                            continue;
                        }
                        CVCMultiStateDisplay multi = cvc as CVCMultiStateDisplay;
                        if (multi != null)
                        {
                            CabViewDiscreteRenderer mspr = new CabViewDiscreteRenderer(viewer, car, multi, _Shader);
                            mspr.SortIndex = controlSortIndex;
                            CabViewControlRenderersList[i].Add(mspr);
                            if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, mspr);
                            count[cvc.ControlType]++;
                            continue;
                        }
                        CVCDiscrete disc = cvc as CVCDiscrete;
                        if (disc != null)
                        {
                            CabViewDiscreteRenderer cvdr = new CabViewDiscreteRenderer(viewer, car, disc, _Shader);
                            cvdr.SortIndex = controlSortIndex;
                            CabViewControlRenderersList[i].Add(cvdr);
                            if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvdr);
                            count[cvc.ControlType]++;
                            continue;
                        }
                        CVCDigital digital = cvc as CVCDigital;
                        if (digital != null)
                        {
                            CabViewDigitalRenderer cvdr;
                            if (digital.ControlStyle == CABViewControlStyles.NEEDLE)
                                cvdr = new CircularSpeedGaugeRenderer(viewer, car, digital, _Shader);
                            else
                                cvdr = new CabViewDigitalRenderer(viewer, car, digital, _Shader);
                            cvdr.SortIndex = controlSortIndex;
                            CabViewControlRenderersList[i].Add(cvdr);
                            if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvdr);
                            count[cvc.ControlType]++;
                            continue;
                        }
                        CVCScreen screen = cvc as CVCScreen;
                        if (screen != null)
                        {
                            if (screen.ControlType.Type == CABViewControlTypes.ORTS_ETCS)
                            {
                                var cvr = new DriverMachineInterfaceRenderer(viewer, car, screen, _Shader);
                                cvr.SortIndex = controlSortIndex;
                                CabViewControlRenderersList[i].Add(cvr);
                                if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvr);
                                count[cvc.ControlType]++;
                                continue;
                            }
                            else if (screen.ControlType.Type == CABViewControlTypes.ORTS_DISTRIBUTED_POWER)
                            {
                                var cvr = new DistributedPowerInterfaceRenderer(viewer, car, screen, _Shader);
                                cvr.SortIndex = controlSortIndex;
                                CabViewControlRenderersList[i].Add(cvr);
                                if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvr);
                                count[cvc.ControlType]++;
                                continue;
                            }
                        }
                    }
                }
                i++;
            }
            #endregion

            _Viewer.AdjustCabHeight(_Viewer.DisplaySize.X, _Viewer.DisplaySize.Y);
        }

        public CabRenderer(Viewer viewer, MSTSLocomotive car, CabViewFile CVFFile) //used by 3D cab as a refrence, thus many can be eliminated
        {
            _Viewer = viewer;
            _Locomotive = car;


            #region Create Control renderers
            ControlMap = new Dictionary<(CabViewControlType, int), CabViewControlRenderer>();
            var count = new Dictionary<CabViewControlType, int>();
            var i = 0;

            var controlSortIndex = 1;  // Controls are drawn atop the cabview and in order they appear in the CVF file.
            // This allows the segments of moving-scale meters to be hidden by covers (e.g. TGV-A)
            CabViewControlRenderersList.Add(new List<CabViewControlRenderer>());
            foreach (CabViewControl cvc in CVFFile.CabViewControls)
            {
                controlSortIndex++;
                if (!count.ContainsKey(cvc.ControlType)) count[cvc.ControlType] = 0;
                var key = (cvc.ControlType, count[cvc.ControlType]);
                CVCDial dial = cvc as CVCDial;
                if (dial != null)
                {
                    CabViewDialRenderer cvcr = new CabViewDialRenderer(viewer, car, dial, _Shader);
                    cvcr.SortIndex = controlSortIndex;
                    CabViewControlRenderersList[i].Add(cvcr);
                    if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvcr);
                    count[cvc.ControlType]++;
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
                    count[cvc.ControlType]++;
                    continue;
                }
                CVCSignal asp = cvc as CVCSignal;
                if (asp != null)
                {
                    CabViewDiscreteRenderer aspr = new CabViewDiscreteRenderer(viewer, car, asp, _Shader);
                    aspr.SortIndex = controlSortIndex;
                    CabViewControlRenderersList[i].Add(aspr);
                    if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, aspr);
                    count[cvc.ControlType]++;
                    continue;
                }
                CVCMultiStateDisplay multi = cvc as CVCMultiStateDisplay;
                if (multi != null)
                {
                    CabViewDiscreteRenderer mspr = new CabViewDiscreteRenderer(viewer, car, multi, _Shader);
                    mspr.SortIndex = controlSortIndex;
                    CabViewControlRenderersList[i].Add(mspr);
                    if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, mspr);
                    count[cvc.ControlType]++;
                    continue;
                }
                CVCDiscrete disc = cvc as CVCDiscrete;
                if (disc != null)
                {
                    CabViewDiscreteRenderer cvdr = new CabViewDiscreteRenderer(viewer, car, disc, _Shader);
                    cvdr.SortIndex = controlSortIndex;
                    CabViewControlRenderersList[i].Add(cvdr);
                    if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvdr);
                    count[cvc.ControlType]++;
                    continue;
                }
                CVCDigital digital = cvc as CVCDigital;
                if (digital != null)
                {
                    CabViewDigitalRenderer cvdr;
                    if (digital.ControlStyle == CABViewControlStyles.NEEDLE)
                        cvdr = new CircularSpeedGaugeRenderer(viewer, car, digital, _Shader);
                    else
                        cvdr = new CabViewDigitalRenderer(viewer, car, digital, _Shader);
                    cvdr.SortIndex = controlSortIndex;
                    CabViewControlRenderersList[i].Add(cvdr);
                    if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvdr);
                    count[cvc.ControlType]++;
                    continue;
                }
                CVCScreen screen = cvc as CVCScreen;
                if (screen != null)
                {
                    if (screen.ControlType.Type == CABViewControlTypes.ORTS_ETCS)
                    {
                        var cvr = new DriverMachineInterfaceRenderer(viewer, car, screen, _Shader);
                        cvr.SortIndex = controlSortIndex;
                        CabViewControlRenderersList[i].Add(cvr);
                        if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvr);
                        count[cvc.ControlType]++;
                        continue;
                    }
                    else if (screen.ControlType.Type == CABViewControlTypes.ORTS_DISTRIBUTED_POWER)
                    {
                        var cvr = new DistributedPowerInterfaceRenderer(viewer, car, screen, _Shader);
                        cvr.SortIndex = controlSortIndex;
                        CabViewControlRenderersList[i].Add(cvr);
                        if (!ControlMap.ContainsKey(key)) ControlMap.Add(key, cvr);
                        count[cvc.ControlType]++;
                        continue;
                    }
                }
            }
            #endregion
        }
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!_Locomotive.ShowCab || _Locomotive.StaleCab || _Locomotive.StaleViewer)
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
            if (_CabTexture == SharedMaterialManager.MissingTexture.Texture)
                return;

            if (_PrevScreenSize != _Viewer.DisplaySize && _Shader != null)
            {
                _PrevScreenSize = _Viewer.DisplaySize;
                _Shader.SetLightPositions(
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[i].ExtendedCVF.Light1Position, _Viewer.DisplaySize),
                    ExtendedCVF.TranslatedPosition(_Locomotive.CabViewList[i].ExtendedCVF.Light2Position, _Viewer.DisplaySize));
            }

            frame.AddPrimitive(_Sprite2DCabView, this, RenderPrimitiveGroup.Cab, ref _Scale);
            //frame.AddPrimitive(Materials.SpriteBatchMaterial, this, RenderPrimitiveGroup.Cab, ref _Scale);

            foreach (var cvcr in CabViewControlRenderersList[i])
            {
                if (cvcr.Control.CabViewpoint == _Location)
                {
                    if (cvcr.Control.Screens != null && cvcr.Control.Screens[0] != "all")
                    {
                        foreach (var screen in cvcr.Control.Screens)
                        {
                            if (ActiveScreen[cvcr.Control.Display] == screen)
                            {
                                cvcr.PrepareFrame(frame, elapsedTime);
                                break;
                            }
                        }
                        continue;
                    }
                    cvcr.PrepareFrame(frame, elapsedTime);
                }
            }
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            var cabScale = new Vector2((float)_Viewer.CabWidthPixels / _CabTexture.Width, (float)_Viewer.CabHeightPixels / _CabTexture.Height);
            // Cab view vertical position adjusted to allow for clip or stretch.
            var cabPos = new Vector2(_Viewer.CabXOffsetPixels / cabScale.X, -_Viewer.CabYOffsetPixels / cabScale.Y);
            var cabSize = new Vector2((_Viewer.CabWidthPixels - _Viewer.CabExceedsDisplayHorizontally) / cabScale.X, (_Viewer.CabHeightPixels - _Viewer.CabExceedsDisplay) / cabScale.Y);
            int round(float x)
            {
                return (int)Math.Round(x);
            }
            var cabRect = new Rectangle(round(cabPos.X), round(cabPos.Y), round(cabSize.X), round(cabSize.Y));

            if (_Shader != null)
            {
                // TODO: Readd ability to control night time lighting.
                float overcast = _Viewer.Settings.UseMSTSEnv ? _Viewer.World.MSTSSky.mstsskyovercastFactor : _Viewer.Simulator.Weather.CloudCoverFactor;
                _Shader.SetData(_Viewer.MaterialManager.sunDirection, _isNightTexture, false, overcast);
                _Shader.SetTextureData(cabRect.Left, cabRect.Top, cabRect.Width, cabRect.Height);
                _Shader.CurrentTechnique.Passes[0].Apply();
            }

            if (_CabTexture == null)
                return;

            var drawOrigin = new Vector2(_CabTexture.Width / 2, _CabTexture.Height / 2);
            var drawPos = new Vector2(_Viewer.CabWidthPixels / 2, _Viewer.CabHeightPixels / 2);
            // Cab view position adjusted to allow for letterboxing.
            drawPos.X += _Viewer.CabXLetterboxPixels;
            drawPos.Y += _Viewer.CabYLetterboxPixels;

            _Sprite2DCabView.SpriteBatch.Draw(_CabTexture, drawPos, cabRect, Color.White, 0f, drawOrigin, cabScale, SpriteEffects.None, 0f);

            // Draw letterboxing.
            void drawLetterbox(int x, int y, int w, int h)
            {
                _Sprite2DCabView.SpriteBatch.Draw(_LetterboxTexture, new Rectangle(x, y, w, h), Color.White);
            }
            if (_Viewer.CabXLetterboxPixels > 0)
            {
                drawLetterbox(0, 0, _Viewer.CabXLetterboxPixels, _Viewer.DisplaySize.Y);
                drawLetterbox(_Viewer.CabXLetterboxPixels + _Viewer.CabWidthPixels, 0, _Viewer.DisplaySize.X - _Viewer.CabWidthPixels - _Viewer.CabXLetterboxPixels, _Viewer.DisplaySize.Y);
            }
            if (_Viewer.CabYLetterboxPixels > 0)
            {
                drawLetterbox(0, 0, _Viewer.DisplaySize.X, _Viewer.CabYLetterboxPixels);
                drawLetterbox(0, _Viewer.CabYLetterboxPixels + _Viewer.CabHeightPixels, _Viewer.DisplaySize.X, _Viewer.DisplaySize.Y - _Viewer.CabHeightPixels - _Viewer.CabYLetterboxPixels);
            }
            //Materials.SpriteBatchMaterial.SpriteBatch.Draw(_CabTexture, _CabRect, Color.White);
        }

        /// <summary>
        /// Checks this cabview for stale textures and sets the stale data flag if any textures are stale
        /// </summary>
        /// <returns>bool indicating if this cab changed from fresh to stale</returns>
        public bool CheckStale()
        {
            if (!_Locomotive.StaleViewer)
            {
                bool stale = false;

                foreach (CabView cabView in _Locomotive.CabViewList)
                {
                    if (cabView.CVFFile != null)
                    {
                        foreach (string cabImage in cabView.CVFFile.TwoDViews)
                        {
                            if (CABTextureManager.GetStale(cabImage))
                            {
                                stale = true;
                                return stale;
                            }
                        }
                    }
                }

                foreach (List<CabViewControlRenderer> controlRenderers in CabViewControlRenderersList)
                {
                    foreach (CabViewControlRenderer controlRenderer in controlRenderers)
                    {
                        if (!String.IsNullOrEmpty(controlRenderer.Control.ACEFile) && CABTextureManager.GetStale(controlRenderer.Control.ACEFile))
                        {
                            stale = true;
                            return stale;
                        }
                        else if (controlRenderer.Control is CVCFirebox fireboxControl)
                        {
                            if (!String.IsNullOrEmpty(fireboxControl.FireACEFile) && CABTextureManager.GetStale(fireboxControl.FireACEFile))
                            {
                                stale = true;
                                return stale;
                            }
                        }
                    }
                }

                return stale;
            }
            else
                return false;
        }

        internal void Mark()
        {
            _Viewer.TextureManager.Mark(_CabTexture);

            var i = (_Locomotive.UsingRearCab) ? 1 : 0;
            foreach (var cvcr in CabViewControlRenderersList[i])
                cvcr.Mark();
        }

        public void Save(BinaryWriter outf)
        {
            foreach (var activeScreen in ActiveScreen)
                if (activeScreen != null)
                    outf.Write(activeScreen);
                else
                    outf.Write("---");

        }

        public void Restore(BinaryReader inf)
        {
            for (int i = 0; i < ActiveScreen.Length; i++)
                ActiveScreen[i] = inf.ReadString();
        }

    }

    /// <summary>
    /// Base class for rendering Cab Controls
    /// </summary>
    public abstract class CabViewControlRenderer : RenderPrimitive
    {
        protected readonly Viewer Viewer;
        protected readonly MSTSLocomotive Locomotive;
        public readonly CabViewControl Control;
        protected readonly CabShader Shader;
        public readonly SpriteBatchMaterial ControlView;

        protected Vector2 Position;
        protected Texture2D Texture;
        protected bool IsNightTexture;
        protected bool HasCabLightDirectory;

        Matrix Matrix = Matrix.Identity;

        /// <summary>
        /// Determines whether or not the control has power given the state of the cab power supply.
        /// </summary>
        /// <remarks>
        /// For controls that do not depend on the power supply, this will always return true.
        /// </remarks>
        public bool IsPowered
        {
            get
            {
                if (Control.DisabledIfLowVoltagePowerSupplyOff)
                    return Locomotive.LocomotivePowerSupply.LowVoltagePowerSupplyOn;
                else if (Control.DisabledIfCabPowerSupplyOff)
                    return Locomotive.LocomotivePowerSupply.CabPowerSupplyOn;
                else
                    return true;
            }
        }

        public CabViewControlRenderer(Viewer viewer, MSTSLocomotive locomotive, CabViewControl control, CabShader shader)
        {
            Viewer = viewer;
            Locomotive = locomotive;
            Control = control;
            Shader = shader;

            ControlView = (SpriteBatchMaterial)viewer.MaterialManager.Load("SpriteBatch", effect: Shader);

            HasCabLightDirectory = CABTextureManager.LoadTextures(Viewer, Control.ACEFile);
        }

        public CabViewControlType GetControlType()
        {
            return Control.ControlType;
        }
        /// <summary>
        /// Gets the requested Locomotive data and returns it as a fraction (from 0 to 1) of the range between Min and Max values.
        /// </summary>
        /// <returns>Data value as fraction (from 0 to 1) of the range between Min and Max values</returns>
        public float GetRangeFraction(bool offsetFromZero = false)
        {
            float data;

            if (!IsPowered && Control.ValueIfDisabled != null)
                data = (float)Control.ValueIfDisabled;
            else
                data = Locomotive.GetDataOf(Control);

            if (data < Control.MinValue)
                return 0;
            if (data > Control.MaxValue)
                return 1;

            if (Control.MaxValue == Control.MinValue)
                return 0;

            return (float)((data - (offsetFromZero && Control.MinValue < 0 ? 0 : Control.MinValue)) / (Control.MaxValue - Control.MinValue));
        }

        public CABViewControlStyles GetStyle()
        {
            return Control.ControlStyle;
        }

        [CallOnThread("Updater")]
        public virtual void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!IsPowered && Control.HideIfDisabled)
                return;

            frame.AddPrimitive(ControlView, this, RenderPrimitiveGroup.Cab, ref Matrix);
        }

        internal void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
        }
    }

    /// <summary>
    /// Interface for mouse controllable CabViewControls
    /// </summary>
    public interface ICabViewMouseControlRenderer
    {
        bool IsMouseWithin();
        void HandleUserInput();
        string GetControlName();
        string ControlLabel { get; }
        Rectangle DestinationRectangleGet();
        bool isMouseControl();
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
            if (Texture == SharedMaterialManager.MissingTexture.Texture)
                return;

            base.PrepareFrame(frame, elapsedTime);

            // Cab view height and vertical position adjusted to allow for clip or stretch.
            // Cab view position adjusted to allow for letterboxing.
            Position.X = (float)Viewer.CabWidthPixels / 640 * ((float)Control.PositionX + Origin.X * Scale) - Viewer.CabXOffsetPixels + Viewer.CabXLetterboxPixels;
            Position.Y = (float)Viewer.CabHeightPixels / 480 * ((float)Control.PositionY + Origin.Y * Scale) + Viewer.CabYOffsetPixels + Viewer.CabYLetterboxPixels;
            ScaleToScreen = (float)Viewer.CabWidthPixels / 640 * Scale;

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
            ControlView.SpriteBatch.Draw(Texture, Position, null, Color.White, Rotation, Origin, ScaleToScreen, SpriteEffects.None, 0);
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
        float DrawRotation;
        Double Num;
        bool IsFire;

        public CabViewGaugeRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCGauge control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            Gauge = control;
            if ((Control.ControlType.Type == CABViewControlTypes.REVERSER_PLATE) || (Gauge.ControlStyle == CABViewControlStyles.POINTER))
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
            if (Locomotive.GetDataOf(Control) < 0)
            { positive = false; return Gauge.NegativeColor; }
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
            if (Texture == SharedMaterialManager.MissingTexture.Texture)
                return;

            base.PrepareFrame(frame, elapsedTime);

            // Cab view height adjusted to allow for clip or stretch.
            var xratio = (float)Viewer.CabWidthPixels / 640;
            var yratio = (float)Viewer.CabHeightPixels / 480;

            float percent, xpos, ypos, zeropos;

            percent = IsFire ? 1f : GetRangeFraction();

            if (!IsPowered && Control.ValueIfDisabled != null)
                Num = (float)Control.ValueIfDisabled;
            else
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

            int destX, destY, destW, destH;
            if (Gauge.ControlStyle == CABViewControlStyles.SOLID || Gauge.ControlStyle == CABViewControlStyles.LIQUID)
            {
                if (Control.MinValue < 0)
                {
                    if (Gauge.Orientation == 0)
                    {
                        destX = (int)(xratio * (Control.PositionX)) + (int)(xratio * (zeropos < xpos ? zeropos : xpos));
//                        destY = (int)(yratio * Control.PositionY);
                        destY = (int)(yratio * (Control.PositionY) - (int)(yratio * (Gauge.Direction == 0 && zeropos > xpos ? (zeropos - xpos) * Math.Sin(DrawRotation) : 0)));
                        destW = ((int)(xratio * xpos) - (int)(xratio * zeropos)) * (xpos >= zeropos ? 1 : -1);
                        destH = (int)(yratio * ypos);
                    }
                    else
                    {
                        destX = (int)(xratio * Control.PositionX) +(int)(xratio * (Gauge.Direction == 0 && ypos > zeropos ? (ypos - zeropos) * Math.Sin(DrawRotation) : 0));
                        if (Gauge.Direction != 1 && !IsFire)
                            destY = (int)(yratio * (Control.PositionY + zeropos)) + (ypos > zeropos ? (int)(yratio * (zeropos - ypos)) : 0);
                        else
                            destY = (int)(yratio * (Control.PositionY + (zeropos < ypos ? zeropos : ypos)));
                        destW = (int)(xratio * xpos);
                        destH = ((int)(yratio * (ypos - zeropos))) * (ypos > zeropos ? 1 : -1);
                    }
                }
                else
                {
                    var topY = Control.PositionY;  // top of visible column. +ve Y is downwards
                    if (Gauge.Direction != 0)  // column grows from bottom or from right
                    {
                        if (Gauge.Orientation != 0)
                        {
                            topY += Gauge.Height * (1 - percent);
                            destX = (int)(xratio * (Control.PositionX + Gauge.Width - xpos + ypos * Math.Sin(DrawRotation)));
                        }
                        else
                        {
                            topY -= xpos * Math.Sin(DrawRotation);
                            destX = (int)(xratio * (Control.PositionX + Gauge.Width - xpos));
                        }
                    }
                    else
                    {
                        destX = (int)(xratio * Control.PositionX);
                    }
                    destY = (int)(yratio * topY);
                    destW = (int)(xratio * xpos);
                    destH = (int)(yratio * ypos);
                }
            }
            else // pointer gauge using texture
            {
                // even if there is a rotation, we leave the X position unaltered (for small angles Cos(alpha) = 1)
                var topY = Control.PositionY;  // top of visible column. +ve Y is downwards
                if (Gauge.Orientation == 0) // gauge horizontal
                {

                    if (Gauge.Direction != 0)  // column grows from right
                    {
                        destX = (int)(xratio * (Control.PositionX + Gauge.Width - 0.5 * Gauge.Area.Width - xpos));
                        topY -= xpos * Math.Sin(DrawRotation);
                    }
                    else
                    {
                        destX = (int)(xratio * (Control.PositionX - 0.5 * Gauge.Area.Width + xpos));
                        topY += xpos * Math.Sin(DrawRotation);
                    }
                }
                else // gauge vertical
                {
                    // even if there is a rotation, we leave the Y position unaltered (for small angles Cos(alpha) = 1)
                    topY += ypos - 0.5 * Gauge.Area.Height;
                    if (Gauge.Direction == 0)
                        destX = (int)(xratio * (Control.PositionX - ypos * Math.Sin(DrawRotation)));
                    else  // column grows from bottom
                    {
                        topY += Gauge.Height - 2.0f * ypos;
                        destX = (int)(xratio * (Control.PositionX + ypos * Math.Sin(DrawRotation)));
                    }
                }
                destY = (int)(yratio * topY);
                destW = (int)(xratio * Gauge.Area.Width);
                destH = (int)(yratio * Gauge.Area.Height);

                // Adjust coal texture height, because it mustn't show up at the bottom of door (see Scotsman)
                // TODO: cut the texture at the bottom instead of stretching
                if (Gauge is CVCFirebox)
                    destH = Math.Min(destH, (int)(yratio * (Control.PositionY + 0.5 * Gauge.Area.Height)) - destY);
            }
            if (Control.ControlType.Type != CABViewControlTypes.REVERSER_PLATE && Gauge.ControlStyle != CABViewControlStyles.POINTER)
            {
                if (Num < 0 && Control.MinValue < 0 && Gauge.NegativeColor.A != 0)
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

            // Cab view vertical position adjusted to allow for clip or stretch.
            destX -= Viewer.CabXOffsetPixels;
            destY += Viewer.CabYOffsetPixels;

            // Cab view position adjusted to allow for letterboxing.
            destX += Viewer.CabXLetterboxPixels;
            destY += Viewer.CabYLetterboxPixels;

            DestinationRectangle.X = destX;
            DestinationRectangle.Y = destY;
            DestinationRectangle.Width = destW;
            DestinationRectangle.Height = destH;
            DrawRotation = Gauge.Rotation;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (Shader != null)
            {
                Shader.SetTextureData(DestinationRectangle.Left, DestinationRectangle.Top, DestinationRectangle.Width, DestinationRectangle.Height);
            }
            ControlView.SpriteBatch.Draw(Texture, DestinationRectangle, SourceRectangle, DrawColor, DrawRotation, origin: Vector2.Zero, SpriteEffects.None, layerDepth: 0);
        }
    }

    /// <summary>
    /// Discrete renderer for Lever, Twostate, Tristate, Multistate, Signal
    /// </summary>
    public class CabViewDiscreteRenderer : CabViewControlRenderer, ICabViewMouseControlRenderer
    {
        protected readonly CVCWithFrames ControlDiscrete;
        readonly Rectangle SourceRectangle;
        Rectangle DestinationRectangle = new Rectangle();
        public readonly float CVCFlashTimeOn = 0.75f;
        public readonly float CVCFlashTimeTotal = 1.5f;
        float CumulativeTime;
        float Scale = 1;
        int OldFrameIndex = 0;
        public bool ButtonState = false;
        int SplitIndex = -1;

        /// <summary>
        /// Accumulated mouse movement. Used for controls with no assigned notch controllers, e.g. headlight and reverser.
        /// </summary>
        float IntermediateValue;

        /// <summary>
        /// Function calculating response value for mouse events (movement, left-click), determined by configured style.
        /// </summary>
        public Func<float, float> ChangedValue;

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
            // Determine the cab view control index shown when combined control is at the split position
            // Find the index of the next value LARGER than the split value
            int splitIndex = ControlDiscrete.Values.BinarySearch(Locomotive.CombinedControlSplitPosition);
            // Account for any edge cases
            if (splitIndex < 0)
                splitIndex = ~splitIndex;
            if (splitIndex > ControlDiscrete.Values.Count - 1)
                splitIndex = ControlDiscrete.Values.Count - 1;
            if (ControlDiscrete.Reversed)
                splitIndex = (ControlDiscrete.Values.Count - 1) - splitIndex;

            SplitIndex = splitIndex;
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

            PrepareFrameForIndex(frame, elapsedTime, index);
        }

        protected void PrepareFrameForIndex(RenderFrame frame, ElapsedTime elapsedTime, int index)
        {
            var dark = Viewer.MaterialManager.sunDirection.Y <= -0.085f || Viewer.Camera.IsUnderground;

            Texture = CABTextureManager.GetTextureByIndexes(Control.ACEFile, index, dark, Locomotive.CabLightOn, out IsNightTexture, HasCabLightDirectory);
            if (Texture == SharedMaterialManager.MissingTexture.Texture)
                return;

            base.PrepareFrame(frame, elapsedTime);

            // Cab view height and vertical position adjusted to allow for clip or stretch.
            var xratio = (float)Viewer.CabWidthPixels / 640;
            var yratio = (float)Viewer.CabHeightPixels / 480;
            // Cab view position adjusted to allow for letterboxing.
            DestinationRectangle.X = (int)(xratio * Control.PositionX * 1.0001) - Viewer.CabXOffsetPixels + Viewer.CabXLetterboxPixels;
            DestinationRectangle.Y = (int)(yratio * Control.PositionY * 1.0001) + Viewer.CabYOffsetPixels + Viewer.CabYLetterboxPixels;
            DestinationRectangle.Width = (int)(xratio * Math.Min(Control.Width, Texture.Width));  // Allow only downscaling of the texture, and not upscaling
            DestinationRectangle.Height = (int)(yratio * Math.Min(Control.Height, Texture.Height));  // Allow only downscaling of the texture, and not upscaling
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (Shader != null)
            {
                Shader.SetTextureData(DestinationRectangle.Left, DestinationRectangle.Top, DestinationRectangle.Width, DestinationRectangle.Height);
            }
            ControlView.SpriteBatch.Draw(Texture, DestinationRectangle, SourceRectangle, Color.White);
        }

        /// <summary>
        /// Determines the index of the Texture to be drawn
        /// </summary>
        /// <returns>index of the Texture</returns>
        public virtual int GetDrawIndex()
        {
            float data;

            if (!IsPowered && Control.ValueIfDisabled != null)
                data = (float)Control.ValueIfDisabled;
            else
                data = Locomotive.GetDataOf(Control);

            var index = OldFrameIndex;
            switch (ControlDiscrete.ControlType.Type)
            {
                case CABViewControlTypes.ENGINE_BRAKE:
                case CABViewControlTypes.BRAKEMAN_BRAKE:
                case CABViewControlTypes.TRAIN_BRAKE:
                case CABViewControlTypes.REGULATOR:
                case CABViewControlTypes.CUTOFF:
                case CABViewControlTypes.BLOWER:
                case CABViewControlTypes.DAMPERS_FRONT:
                case CABViewControlTypes.STEAM_HEAT:
                case CABViewControlTypes.STEAM_BOOSTER_AIR:
                case CABViewControlTypes.STEAM_BOOSTER_IDLE:
                case CABViewControlTypes.STEAM_BOOSTER_LATCH:
                case CABViewControlTypes.ORTS_WATER_SCOOP:
                case CABViewControlTypes.WATER_INJECTOR1:
                case CABViewControlTypes.WATER_INJECTOR2:
                case CABViewControlTypes.SMALL_EJECTOR:
                case CABViewControlTypes.ORTS_LARGE_EJECTOR:
                case CABViewControlTypes.FIREHOLE:
                case CABViewControlTypes.THROTTLE:
                case CABViewControlTypes.THROTTLE_DISPLAY:
                case CABViewControlTypes.DYNAMIC_BRAKE_DISPLAY:
                    index = PercentToIndex(data);
                    break;
                case CABViewControlTypes.FRICTION_BRAKING:
                    index = data > 0.001 ? 1 : 0;
                    break;
                case CABViewControlTypes.DYNAMIC_BRAKE:
                    if (Locomotive.DynamicBrakeController != null && !Locomotive.HasSmoothStruc)
                        index = Locomotive.DynamicBrakeController.CurrentNotch;
                    else
                        index = PercentToIndex(data);
                    break;
                case CABViewControlTypes.CPH_DISPLAY:
                case CABViewControlTypes.CP_HANDLE:
                    var combinedHandlePosition = Locomotive.GetCombinedHandleValue(false);
                    // Make sure any deviation from the split position gives a different index
                    int handleRelativePos = combinedHandlePosition.CompareTo(Locomotive.CombinedControlSplitPosition);
                    if (handleRelativePos != 0)
                    {
                        if (handleRelativePos == (ControlDiscrete.Reversed ? - 1 : 1))
                            index = Math.Max(PercentToIndex(combinedHandlePosition), SplitIndex + 1);
                        else
                            index = Math.Min(PercentToIndex(combinedHandlePosition), SplitIndex - 1);
                    }
                    else
                        index = SplitIndex;
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_DISPLAY:
                    if (Locomotive.CruiseControl == null)
                    {
                        index = 0;
                        break;
                    }
                    index = (int)MpS.ToKpH(Locomotive.CruiseControl.SelectedSpeedMpS) / 10;
                    break;
                case CABViewControlTypes.ALERTER_DISPLAY:
                case CABViewControlTypes.RESET:
                case CABViewControlTypes.WIPERS:
                case CABViewControlTypes.EXTERNALWIPERS:
                case CABViewControlTypes.LEFTDOOR:
                case CABViewControlTypes.RIGHTDOOR:
                case CABViewControlTypes.MIRRORS:
                case CABViewControlTypes.ORTS_LEFTWINDOW:
                case CABViewControlTypes.ORTS_RIGHTWINDOW:
                case CABViewControlTypes.HORN:
                case CABViewControlTypes.VACUUM_EXHAUSTER:
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
                case CABViewControlTypes.ORTS_VOLTAGE_SELECTOR:
                case CABViewControlTypes.ORTS_PANTOGRAPH_SELECTOR:
                case CABViewControlTypes.ORTS_POWER_LIMITATION_SELECTOR:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_STATE:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_CLOSED:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_AUTHORIZED:
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_OPEN_AND_AUTHORIZED:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_ORDER:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_OPENING_ORDER:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_AUTHORIZATION:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_STATE:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_CLOSED:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_OPEN:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_AUTHORIZED:
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_OPEN_AND_AUTHORIZED:
                case CABViewControlTypes.DIRECTION:
                case CABViewControlTypes.DIRECTION_DISPLAY:
                case CABViewControlTypes.ASPECT_DISPLAY:
                case CABViewControlTypes.GEARS:
                case CABViewControlTypes.OVERSPEED:
                case CABViewControlTypes.PENALTY_APP:
                case CABViewControlTypes.EMERGENCY_BRAKE:
                case CABViewControlTypes.ORTS_BAILOFF:
                case CABViewControlTypes.ORTS_QUICKRELEASE:
                case CABViewControlTypes.ORTS_OVERCHARGE:
                case CABViewControlTypes.ORTS_NEUTRAL_MODE_COMMAND_SWITCH:
                case CABViewControlTypes.ORTS_NEUTRAL_MODE_ON:
                case CABViewControlTypes.DOORS_DISPLAY:
                case CABViewControlTypes.CYL_COCKS:
                case CABViewControlTypes.ORTS_BLOWDOWN_VALVE:
                case CABViewControlTypes.ORTS_CYL_COMP:
                case CABViewControlTypes.STEAM_INJ1:
                case CABViewControlTypes.STEAM_INJ2:
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
                case CABViewControlTypes.ORTS_BATTERY_SWITCH_COMMAND_SWITCH:
                case CABViewControlTypes.ORTS_BATTERY_SWITCH_COMMAND_BUTTON_CLOSE:
                case CABViewControlTypes.ORTS_BATTERY_SWITCH_COMMAND_BUTTON_OPEN:
                case CABViewControlTypes.ORTS_BATTERY_SWITCH_ON:
                case CABViewControlTypes.ORTS_MASTER_KEY:
                case CABViewControlTypes.ORTS_CURRENT_CAB_IN_USE:
                case CABViewControlTypes.ORTS_OTHER_CAB_IN_USE:
                case CABViewControlTypes.ORTS_SERVICE_RETENTION_BUTTON:
                case CABViewControlTypes.ORTS_SERVICE_RETENTION_CANCELLATION_BUTTON:
                case CABViewControlTypes.ORTS_ELECTRIC_TRAIN_SUPPLY_COMMAND_SWITCH:
                case CABViewControlTypes.ORTS_ELECTRIC_TRAIN_SUPPLY_ON:
                case CABViewControlTypes.ORTS_ODOMETER_DIRECTION:
                case CABViewControlTypes.ORTS_ODOMETER_RESET:
                case CABViewControlTypes.ORTS_GENERIC_ITEM1:
                case CABViewControlTypes.ORTS_GENERIC_ITEM2:
                case CABViewControlTypes.ORTS_EOT_EMERGENCY_BRAKE:
                    index = (int)data;
                    break;
                case CABViewControlTypes.ORTS_SCREEN_SELECT:
                case CABViewControlTypes.ORTS_DP_MOVE_TO_BACK:
                case CABViewControlTypes.ORTS_DP_MOVE_TO_FRONT:
                case CABViewControlTypes.ORTS_DP_TRACTION:
                case CABViewControlTypes.ORTS_DP_IDLE:
                case CABViewControlTypes.ORTS_DP_BRAKE:
                case CABViewControlTypes.ORTS_DP_MORE:
                case CABViewControlTypes.ORTS_DP_LESS:
                case CABViewControlTypes.ORTS_EOT_COMM_TEST:
                case CABViewControlTypes.ORTS_EOT_DISARM:
                case CABViewControlTypes.ORTS_EOT_ARM_TWO_WAY:
                    index = ButtonState ? 1 : 0;
                    break;
                case CABViewControlTypes.ORTS_STATIC_DISPLAY:
                    index = 0;
                    break;
                case CABViewControlTypes.ORTS_EOT_STATE_DISPLAY:
                    index = ControlDiscrete.Values.FindIndex(ind => ind > (int)data) - 1;
                    if (index == -2) index = ControlDiscrete.Values.Count - 1;
                    break;
                case CABViewControlTypes.ORTS_TCS:
                case CABViewControlTypes.ORTS_POWER_SUPPLY:
                // Jindrich
                case CABViewControlTypes.ORTS_RESTRICTED_SPEED_ZONE_ACTIVE:
                case CABViewControlTypes.ORTS_SELECTED_SPEED_MODE:
                case CABViewControlTypes.ORTS_SELECTED_SPEED_REGULATOR_MODE:
                case CABViewControlTypes.ORTS_SELECTED_SPEED_MAXIMUM_ACCELERATION:
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DISPLAY_UNITS:
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DISPLAY_TENS:
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DISPLAY_HUNDREDS:
                case CABViewControlTypes.ORTS_TRAIN_LENGTH_METERS:
                case CABViewControlTypes.ORTS_REMAINING_TRAIN_LENGTH_SPEED_RESTRICTED:
                case CABViewControlTypes.ORTS_REMAINING_TRAIN_LENGTH_PERCENT:
                case CABViewControlTypes.ORTS_MOTIVE_FORCE:
                case CABViewControlTypes.ORTS_MOTIVE_FORCE_KILONEWTON:
                case CABViewControlTypes.ORTS_MAXIMUM_FORCE:
                case CABViewControlTypes.ORTS_SELECTED_SPEED:
                case CABViewControlTypes.ORTS_FORCE_IN_PERCENT_THROTTLE_AND_DYNAMIC_BRAKE:
                case CABViewControlTypes.ORTS_TRAIN_TYPE_PAX_OR_CARGO:
                case CABViewControlTypes.ORTS_CONTROLLER_VOLTAGE:
                case CABViewControlTypes.ORTS_AMPERS_BY_CONTROLLER_VOLTAGE:
                case CABViewControlTypes.ORTS_MULTI_POSITION_CONTROLLER:
                case CABViewControlTypes.ORTS_ACCELERATION_IN_TIME:
                    index = (int)data;
                    break;
                case CABViewControlTypes.ORTS_CC_SPEED_DELTA:
                case CABViewControlTypes.ORTS_CC_SPEED_0:
                case CABViewControlTypes.ORTS_CC_SELECTED_SPEED:
                    index = ButtonState ? 1 : 0;
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_SELECTOR:
                    var fraction = data / (float)(ControlDiscrete.MaxValue);
                    index = (int)MathHelper.Clamp(0, (int)(fraction * ((ControlDiscrete as CVCDiscrete).FramesCount - 1)), (ControlDiscrete as CVCDiscrete).FramesCount - 1);
                    break;
            }
            // If it is a control with NumPositions and NumValues, the index becomes the reference to the Positions entry, which in turn is the frame index within the .ace file
            if (ControlDiscrete is CVCDiscrete && !(ControlDiscrete is CVCSignal) && (ControlDiscrete as CVCDiscrete).Positions.Count > index &&
                (ControlDiscrete as CVCDiscrete).Positions.Count == ControlDiscrete.Values.Count && index >= 0)
                index = (ControlDiscrete as CVCDiscrete).Positions[index];

            if (index >= ControlDiscrete.FramesCount) index = ControlDiscrete.FramesCount - 1;
            if (index < 0) index = 0;
            OldFrameIndex = index;
            return index;
        }

        /// <summary>
        /// Converts absolute mouse movement to control value change, respecting the configured orientation and increase direction
        /// </summary>
        float NormalizedMouseMovement()
        {
            var mouseWheelChange = (UserInput.IsDown(UserCommand.GameSwitchWithMouse) ? UserInput.MouseWheelChange : 0) / 10;
            return (ControlDiscrete.Orientation > 0
                ? (float)(UserInput.MouseMoveY + mouseWheelChange) / (float)Control.Height
                : (float)(UserInput.MouseMoveX + mouseWheelChange) / (float)Control.Width)
                * (ControlDiscrete.Direction > 0 ? -1 : 1);
        }

        public bool IsMouseWithin()
        {
            return ControlDiscrete.MouseControl & DestinationRectangle.Contains(UserInput.MouseX, UserInput.MouseY);
        }

        public string GetControlName()
        {
            if (ControlDiscrete.ControlType.Type == CABViewControlTypes.ORTS_TCS) return Locomotive.TrainControlSystem.GetDisplayString(ControlDiscrete.ControlType.Id);
            if (ControlDiscrete.ControlType.Type == CABViewControlTypes.ORTS_POWER_SUPPLY && Locomotive.LocomotivePowerSupply is ScriptedLocomotivePowerSupply supply) return supply.GetDisplayString(ControlDiscrete.ControlType.Id);
            return GetControlType().ToString();
        }

        public string ControlLabel => Control.Label;

        /// <summary>
        /// Handles cabview mouse events, and changes the corresponding locomotive control values.
        /// </summary>

        public void HandleUserInput()
        {
            var Locomotive = this.Locomotive;
            if (Locomotive is MSTSControlTrailerCar controlCar)
            {
                switch (Control.ControlType.Type)
                {
                    // Diesel locomotive controls
                    case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE:
                    case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_AUTHORIZATION:
                    case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_ORDER:
                    case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_OPENING_ORDER:
                        Locomotive = controlCar.ControlActiveLocomotive as MSTSDieselLocomotive;
                        break;
                    // Electric locomotive controls
                    case CABViewControlTypes.PANTOGRAPH:
                    case CABViewControlTypes.PANTOGRAPH2:
                    case CABViewControlTypes.ORTS_PANTOGRAPH3:
                    case CABViewControlTypes.ORTS_PANTOGRAPH4:
                    case CABViewControlTypes.PANTOGRAPHS_4:
                    case CABViewControlTypes.PANTOGRAPHS_4C:
                    case CABViewControlTypes.PANTOGRAPHS_5:
                    case CABViewControlTypes.ORTS_VOLTAGE_SELECTOR:
                    case CABViewControlTypes.ORTS_PANTOGRAPH_SELECTOR:
                    case CABViewControlTypes.ORTS_POWER_LIMITATION_SELECTOR:
                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER:
                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER:
                    case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION:
                        Locomotive = controlCar.ControlActiveLocomotive as MSTSElectricLocomotive;
                        break;
                }
                if (Locomotive == null) return;
            }
            switch (Control.ControlType.Type)
            {
                case CABViewControlTypes.REGULATOR:
                case CABViewControlTypes.THROTTLE:
                    Locomotive.SetThrottleValue(ChangedValue(Locomotive.ThrottleController.IntermediateValue));
                    break;
                case CABViewControlTypes.ENGINE_BRAKE: Locomotive.SetEngineBrakeValue(ChangedValue(Locomotive.EngineBrakeController.IntermediateValue)); break;
                case CABViewControlTypes.BRAKEMAN_BRAKE: Locomotive.SetBrakemanBrakeValue(ChangedValue(Locomotive.BrakemanBrakeController.IntermediateValue)); break;
                case CABViewControlTypes.TRAIN_BRAKE: Locomotive.SetTrainBrakeValue(ChangedValue(Locomotive.TrainBrakeController.IntermediateValue)); break;
                case CABViewControlTypes.DYNAMIC_BRAKE: Locomotive.SetDynamicBrakeValue(ChangedValue(Locomotive.DynamicBrakeController.IntermediateValue)); break;
                case CABViewControlTypes.GEARS: Locomotive.SetGearBoxValue(ChangedValue(Locomotive.GearBoxController.IntermediateValue)); break;
                case CABViewControlTypes.DIRECTION: var dir = ChangedValue(0); if (dir != 0) new ReverserCommand(Viewer.Log, dir > 0); break;
                case CABViewControlTypes.FRONT_HLIGHT: var hl = ChangedValue(0); if (hl != 0) new HeadlightCommand(Viewer.Log, hl > 0); break;
                case CABViewControlTypes.WHISTLE:
                case CABViewControlTypes.HORN: new HornCommand(Viewer.Log, ChangedValue(Locomotive.Horn ? 1 : 0) > 0); break;
                case CABViewControlTypes.VACUUM_EXHAUSTER: new VacuumExhausterCommand(Viewer.Log, ChangedValue(Locomotive.VacuumExhausterPressed ? 1 : 0) > 0); break;
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
                            else if (Control.ControlType.Type == CABViewControlTypes.PANTOGRAPHS_4C) new PantographCommand(Viewer.Log, 2, true);
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
                            else if (Control.ControlType.Type == CABViewControlTypes.PANTOGRAPHS_4C) new PantographCommand(Viewer.Log, 2, false);
                        }
                    }
                    break;
                case CABViewControlTypes.STEAM_HEAT: Locomotive.SetSteamHeatValue(ChangedValue(Locomotive.SteamHeatController.IntermediateValue)); break;
                case CABViewControlTypes.ORTS_WATER_SCOOP: if (((Locomotive as MSTSSteamLocomotive).WaterScoopDown ? 1 : 0) != ChangedValue(Locomotive.WaterScoopDown ? 1 : 0)) new ToggleWaterScoopCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_VOLTAGE_SELECTOR:
                {
                    if (Locomotive is MSTSElectricLocomotive electricLocomotive)
                    {
                        if (ChangedValue(electricLocomotive.ElectricPowerSupply.VoltageSelector.PositionId) > 0)
                        {
                            new VoltageSelectorCommand(Viewer.Log, true);
                        }
                        else if (ChangedValue(electricLocomotive.ElectricPowerSupply.VoltageSelector.PositionId) < 0)
                        {
                            new VoltageSelectorCommand(Viewer.Log, false);
                        }
                    }
                    break;
                }
                case CABViewControlTypes.ORTS_PANTOGRAPH_SELECTOR:
                {
                    if (Locomotive is MSTSElectricLocomotive electricLocomotive)
                    {
                        if (ChangedValue(electricLocomotive.ElectricPowerSupply.PantographSelector.PositionId) > 0)
                        {
                            new PantographSelectorCommand(Viewer.Log, true);
                        }
                        else if (ChangedValue(electricLocomotive.ElectricPowerSupply.PantographSelector.PositionId) < 0)
                        {
                            new PantographSelectorCommand(Viewer.Log, false);
                        }
                    }
                    break;
                }
                case CABViewControlTypes.ORTS_POWER_LIMITATION_SELECTOR:
                {
                    if (Locomotive is MSTSElectricLocomotive electricLocomotive)
                    {
                        if (ChangedValue(electricLocomotive.ElectricPowerSupply.PowerLimitationSelector.PositionId) > 0)
                        {
                            new PowerLimitationSelectorCommand(Viewer.Log, true);
                        }
                        else if (ChangedValue(electricLocomotive.ElectricPowerSupply.PowerLimitationSelector.PositionId) < 0)
                        {
                            new PowerLimitationSelectorCommand(Viewer.Log, false);
                        }
                    }
                    break;
                }
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_ORDER:
                    new CircuitBreakerClosingOrderCommand(Viewer.Log, ChangedValue((Locomotive as MSTSElectricLocomotive).ElectricPowerSupply.CircuitBreaker.DriverClosingOrder ? 1 : 0) > 0);
                    new CircuitBreakerClosingOrderButtonCommand(Viewer.Log, ChangedValue(UserInput.IsMouseLeftButtonPressed ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_OPENING_ORDER: new CircuitBreakerOpeningOrderButtonCommand(Viewer.Log, ChangedValue(UserInput.IsMouseLeftButtonPressed ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_CIRCUIT_BREAKER_DRIVER_CLOSING_AUTHORIZATION: new CircuitBreakerClosingAuthorizationCommand(Viewer.Log, ChangedValue((Locomotive as MSTSElectricLocomotive).ElectricPowerSupply.CircuitBreaker.DriverClosingAuthorization ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_ORDER:
                    new TractionCutOffRelayClosingOrderCommand(Viewer.Log, ChangedValue((Locomotive as MSTSDieselLocomotive).DieselPowerSupply.TractionCutOffRelay.DriverClosingOrder ? 1 : 0) > 0);
                    new TractionCutOffRelayClosingOrderButtonCommand(Viewer.Log, ChangedValue(UserInput.IsMouseLeftButtonPressed ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_OPENING_ORDER: new TractionCutOffRelayOpeningOrderButtonCommand(Viewer.Log, ChangedValue(UserInput.IsMouseLeftButtonPressed ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_TRACTION_CUT_OFF_RELAY_DRIVER_CLOSING_AUTHORIZATION: new TractionCutOffRelayClosingAuthorizationCommand(Viewer.Log, ChangedValue((Locomotive as MSTSDieselLocomotive).DieselPowerSupply.TractionCutOffRelay.DriverClosingAuthorization ? 1 : 0) > 0); break;
                case CABViewControlTypes.EMERGENCY_BRAKE: if ((Locomotive.EmergencyButtonPressed ? 1 : 0) != ChangedValue(Locomotive.EmergencyButtonPressed ? 1 : 0)) new EmergencyPushButtonCommand(Viewer.Log, !Locomotive.EmergencyButtonPressed); break;
                case CABViewControlTypes.ORTS_BAILOFF: new BailOffCommand(Viewer.Log, ChangedValue(Locomotive.BailOff ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_QUICKRELEASE: new QuickReleaseCommand(Viewer.Log, ChangedValue(Locomotive.TrainBrakeController.QuickReleaseButtonPressed ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_OVERCHARGE: new BrakeOverchargeCommand(Viewer.Log, ChangedValue(Locomotive.TrainBrakeController.OverchargeButtonPressed ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_NEUTRAL_MODE_COMMAND_SWITCH: new BrakeNeutralModeCommand(Viewer.Log, ChangedValue(Locomotive.TrainBrakeController.NeutralModeCommandSwitchOn ? 1 : 0) > 0); break;
                case CABViewControlTypes.RESET: new AlerterCommand(Viewer.Log, ChangedValue(Locomotive.TrainControlSystem.AlerterButtonPressed ? 1 : 0) > 0); break;
                case CABViewControlTypes.CP_HANDLE:
                    Locomotive.SetCombinedHandleValue(ChangedValue(Locomotive.GetCombinedHandleValue(true)));
                    break;

                // Steam locomotives only:
                case CABViewControlTypes.CUTOFF: (Locomotive as MSTSSteamLocomotive).SetCutoffValue(ChangedValue((Locomotive as MSTSSteamLocomotive).CutoffController.IntermediateValue)); break;
                case CABViewControlTypes.BLOWER: (Locomotive as MSTSSteamLocomotive).SetBlowerValue(ChangedValue((Locomotive as MSTSSteamLocomotive).BlowerController.IntermediateValue)); break;
                case CABViewControlTypes.DAMPERS_FRONT: (Locomotive as MSTSSteamLocomotive).SetDamperValue(ChangedValue((Locomotive as MSTSSteamLocomotive).DamperController.IntermediateValue)); break;
                case CABViewControlTypes.FIREHOLE: (Locomotive as MSTSSteamLocomotive).SetFireboxDoorValue(ChangedValue((Locomotive as MSTSSteamLocomotive).FireboxDoorController.IntermediateValue)); break;
                case CABViewControlTypes.WATER_INJECTOR1: (Locomotive as MSTSSteamLocomotive).SetInjector1Value(ChangedValue((Locomotive as MSTSSteamLocomotive).Injector1Controller.IntermediateValue)); break;
                case CABViewControlTypes.WATER_INJECTOR2: (Locomotive as MSTSSteamLocomotive).SetInjector2Value(ChangedValue((Locomotive as MSTSSteamLocomotive).Injector2Controller.IntermediateValue)); break;
                case CABViewControlTypes.CYL_COCKS: if (((Locomotive as MSTSSteamLocomotive).CylinderCocksAreOpen ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).CylinderCocksAreOpen ? 1 : 0)) new ToggleCylinderCocksCommand(Viewer.Log); break;
                case CABViewControlTypes.STEAM_BOOSTER_AIR: if (((Locomotive as MSTSSteamLocomotive).SteamBoosterAirOpen ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).SteamBoosterAirOpen ? 1 : 0)) new ToggleSteamBoosterAirCommand(Viewer.Log); break;
                case CABViewControlTypes.STEAM_BOOSTER_IDLE: if (((Locomotive as MSTSSteamLocomotive).SteamBoosterIdle ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).SteamBoosterIdle ? 1 : 0)) new ToggleSteamBoosterIdleCommand(Viewer.Log); break;
                case CABViewControlTypes.STEAM_BOOSTER_LATCH: if (((Locomotive as MSTSSteamLocomotive).SteamBoosterLatchOn ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).SteamBoosterLatchOn ? 1 : 0)) new ToggleSteamBoosterLatchCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_CYL_COMP: if (((Locomotive as MSTSSteamLocomotive).CylinderCompoundOn ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).CylinderCompoundOn ? 1 : 0)) new ToggleCylinderCompoundCommand(Viewer.Log); break;
                case CABViewControlTypes.STEAM_INJ1: if (((Locomotive as MSTSSteamLocomotive).Injector1IsOn ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).Injector1IsOn ? 1 : 0)) new ToggleInjectorCommand(Viewer.Log, 1); break;
                case CABViewControlTypes.STEAM_INJ2: if (((Locomotive as MSTSSteamLocomotive).Injector2IsOn ? 1 : 0) != ChangedValue((Locomotive as MSTSSteamLocomotive).Injector2IsOn ? 1 : 0)) new ToggleInjectorCommand(Viewer.Log, 2); break;
                case CABViewControlTypes.SMALL_EJECTOR: (Locomotive as MSTSSteamLocomotive).SetSmallEjectorValue(ChangedValue((Locomotive as MSTSSteamLocomotive).SmallEjectorController.IntermediateValue)); break;
                case CABViewControlTypes.ORTS_LARGE_EJECTOR: (Locomotive as MSTSSteamLocomotive).SetLargeEjectorValue(ChangedValue((Locomotive as MSTSSteamLocomotive).LargeEjectorController.IntermediateValue)); break;
                //
                case CABViewControlTypes.CAB_RADIO: new CabRadioCommand(Viewer.Log, ChangedValue(Locomotive.CabRadioOn ? 1 : 0) > 0); break;
                case CABViewControlTypes.WIPERS: new WipersCommand(Viewer.Log, ChangedValue(Locomotive.Wiper ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE:
                    var dieselLoco = Locomotive as MSTSDieselLocomotive;
                    if ((dieselLoco.DieselEngines[0].State == DieselEngineState.Running ||
                                dieselLoco.DieselEngines[0].State == DieselEngineState.Stopped) &&
                                ChangedValue(1) == 0) new TogglePlayerEngineCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_HELPERS_DIESEL_ENGINES:
                    foreach (var car in Locomotive.Train.Cars)
                    {
                        dieselLoco = car as MSTSDieselLocomotive;
                        if (dieselLoco != null && dieselLoco.RemoteControlGroup != -1)
                        {
                            if (car == Viewer.Simulator.PlayerLocomotive && dieselLoco.DieselEngines.Count > 1)
                            {
                                if ((dieselLoco.DieselEngines[1].State == DieselEngineState.Running ||
                                            dieselLoco.DieselEngines[1].State == DieselEngineState.Stopped) &&
                                            ChangedValue(1) == 0) new ToggleHelpersEngineCommand(Viewer.Log);
                                break;
                            }
                            else if (car != Viewer.Simulator.PlayerLocomotive && dieselLoco.RemoteControlGroup >= 0)
                            {
                                if ((dieselLoco.DieselEngines[0].State == DieselEngineState.Running ||
                                            dieselLoco.DieselEngines[0].State == DieselEngineState.Stopped) &&
                                            ChangedValue(1) == 0) new ToggleHelpersEngineCommand(Viewer.Log);
                                break;
                            }
                        }
                    }
                    break;
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE_STARTER:
                    dieselLoco = Locomotive as MSTSDieselLocomotive;
                    if (dieselLoco.DieselEngines[0].State == DieselEngineState.Stopped &&
                                ChangedValue(1) == 0) new TogglePlayerEngineCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_PLAYER_DIESEL_ENGINE_STOPPER:
                    dieselLoco = Locomotive as MSTSDieselLocomotive;
                    if (dieselLoco.DieselEngines[0].State == DieselEngineState.Running &&
                                ChangedValue(1) == 0) new TogglePlayerEngineCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_CABLIGHT:
                    if ((Locomotive.CabLightOn ? 1 : 0) != ChangedValue(Locomotive.CabLightOn ? 1 : 0)) new ToggleCabLightCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_LEFTDOOR:
                case CABViewControlTypes.ORTS_RIGHTDOOR:
                    {
                        bool right = (Control.ControlType.Type == CABViewControlTypes.ORTS_RIGHTDOOR) ^ Locomotive.Flipped ^ Locomotive.GetCabFlipped();
                        var state = Locomotive.Train.DoorState(right ? DoorSide.Right : DoorSide.Left);
                        int open = state >= DoorState.Opening ? 1 : 0;
                        if (open != ChangedValue(open))
                        {
                            if (right) new ToggleDoorsRightCommand(Viewer.Log);
                            else new ToggleDoorsLeftCommand(Viewer.Log);
                        }
                    }
                    break;
                case CABViewControlTypes.ORTS_MIRRORS:
                    if ((Locomotive.MirrorOpen ? 1 : 0) != ChangedValue(Locomotive.MirrorOpen ? 1 : 0)) new ToggleMirrorsCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_BATTERY_SWITCH_COMMAND_SWITCH:
                    new BatterySwitchCommand(Viewer.Log, ChangedValue(Locomotive.LocomotivePowerSupply.BatterySwitch.CommandSwitch ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_BATTERY_SWITCH_COMMAND_BUTTON_CLOSE:
                    new BatterySwitchCloseButtonCommand(Viewer.Log, ChangedValue(UserInput.IsMouseLeftButtonPressed ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_BATTERY_SWITCH_COMMAND_BUTTON_OPEN:
                    new BatterySwitchOpenButtonCommand(Viewer.Log, ChangedValue(UserInput.IsMouseLeftButtonPressed ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_MASTER_KEY:
                    new ToggleMasterKeyCommand(Viewer.Log, ChangedValue(Locomotive.LocomotivePowerSupply.MasterKey.CommandSwitch ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_SERVICE_RETENTION_BUTTON:
                    new ServiceRetentionButtonCommand(Viewer.Log, ChangedValue(Locomotive.LocomotivePowerSupply.ServiceRetentionButton ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_SERVICE_RETENTION_CANCELLATION_BUTTON:
                    new ServiceRetentionCancellationButtonCommand(Viewer.Log, ChangedValue(Locomotive.LocomotivePowerSupply.ServiceRetentionCancellationButton ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_ELECTRIC_TRAIN_SUPPLY_COMMAND_SWITCH:
                    new ElectricTrainSupplyCommand(Viewer.Log, ChangedValue(Locomotive.LocomotivePowerSupply.ElectricTrainSupplySwitch.CommandSwitch ? 1 : 0) > 0);
                    break;
                case CABViewControlTypes.ORTS_ODOMETER_DIRECTION: if (ChangedValue(1) == 0) new ToggleOdometerDirectionCommand(Viewer.Log); break;
                case CABViewControlTypes.ORTS_ODOMETER_RESET:
                    new ResetOdometerCommand(Viewer.Log, ChangedValue(Locomotive.OdometerResetButtonPressed ? 1 : 0) > 0); break;
                case CABViewControlTypes.ORTS_GENERIC_ITEM1:
                    if ((Locomotive.GenericItem1 ? 1 : 0) != ChangedValue(Locomotive.GenericItem1 ? 1 : 0)) new ToggleGenericItem1Command(Viewer.Log); break;
                case CABViewControlTypes.ORTS_GENERIC_ITEM2:
                    if ((Locomotive.GenericItem2 ? 1 : 0) != ChangedValue(Locomotive.GenericItem2 ? 1 : 0)) new ToggleGenericItem2Command(Viewer.Log); break;
                case CABViewControlTypes.ORTS_SCREEN_SELECT:
                    bool buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (((CVCDiscrete)Control).NewScreens != null)
                        foreach (var newScreen in ((CVCDiscrete)Control).NewScreens)
                        {
                            var newScreenDisplay = newScreen.NewScreenDisplay;
                            if (newScreen.NewScreenDisplay == -1)
                                newScreenDisplay = ((CVCDiscrete)Control).Display;
                            new SelectScreenCommand(Viewer.Log, buttonState, newScreen.NewScreen, newScreenDisplay);
                        }
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_LEFTWINDOW:
                case CABViewControlTypes.ORTS_RIGHTWINDOW:
                    {
                        bool left = (Control.ControlType.Type == CABViewControlTypes.ORTS_LEFTWINDOW);
                        var windowIndex = (left ? 0 : 1) + 2 * (Locomotive.UsingRearCab ? 1 : 0);
                        var state = Locomotive.WindowStates[windowIndex];
                        int open = state >= MSTSWagon.WindowState.Opening ? 1 : 0;
                        if (open != ChangedValue(open))
                        {
                            if (left) new ToggleWindowLeftCommand(Viewer.Log);
                            else new ToggleWindowRightCommand(Viewer.Log);
                        }
                    }
                    break;

                // Train Control System controls
                case CABViewControlTypes.ORTS_TCS:
                    {
                        int commandIndex = Control.ControlType.Id - 1;
                        Locomotive.TrainControlSystem.TCSCommandButtonDown.TryGetValue(commandIndex, out bool currentValue);
                        if (ChangedValue(1) > 0 ^ currentValue)
                            new TCSButtonCommand(Viewer.Log, !currentValue, commandIndex);
                        Locomotive.TrainControlSystem.TCSCommandSwitchOn.TryGetValue(commandIndex, out bool currentSwitchValue);
                        new TCSSwitchCommand(Viewer.Log, ChangedValue(currentSwitchValue ? 1 : 0) > 0, commandIndex);
                    }
                    break;

                // Power Supply controls
                case CABViewControlTypes.ORTS_POWER_SUPPLY:
                    if (Locomotive.LocomotivePowerSupply is ScriptedLocomotivePowerSupply supply)
                    {
                        int commandIndex = Control.ControlType.Id - 1;
                        supply.PowerSupplyCommandButtonDown.TryGetValue(commandIndex, out bool currentValue);
                        if (ChangedValue(1) > 0 ^ currentValue)
                            new PowerSupplyButtonCommand(Viewer.Log, !currentValue, commandIndex);
                        supply.PowerSupplyCommandSwitchOn.TryGetValue(commandIndex, out bool currentSwitchValue);
                        new PowerSupplySwitchCommand(Viewer.Log, ChangedValue(currentSwitchValue ? 1 : 0) > 0, commandIndex);
                    }
                    break;

                // Jindrich
                case CABViewControlTypes.ORTS_CC_SELECTED_SPEED:
                    if (Locomotive.CruiseControl == null)
                        break;
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        Locomotive.CruiseControl.SetSpeed(Control.Parameter1);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_REGULATOR_MODE:
                    var p = ChangedValue(0);
                    if (Control.ControlStyle == CABViewControlStyles.ONOFF)
                    {
                        if (ChangedValue(0) == 1)
                        {
                            if (Locomotive.CruiseControl.SpeedRegMode == Simulation.RollingStocks.SubSystems.CruiseControl.SpeedRegulatorMode.Manual)
                            {
                                Locomotive.CruiseControl.SpeedRegulatorModeIncrease();
                            }
                            else if (Locomotive.CruiseControl.SpeedRegMode == Simulation.RollingStocks.SubSystems.CruiseControl.SpeedRegulatorMode.Auto)
                            {
                                Locomotive.CruiseControl.SpeedRegulatorModeDecrease();
                            }
                        }
                    }
                    else
                    {
                        if (p == 1)
                        {
                            Locomotive.CruiseControl.SpeedRegulatorModeIncrease();
                        }
                        else if (p == -1)
                        {
                            Locomotive.CruiseControl.SpeedRegulatorModeDecrease();
                        }
                    }
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_MODE:
                    p = ChangedValue(0);
                    if (p == 1)
                    {
                        Locomotive.CruiseControl.SpeedSelectorModeStartIncrease();
                    }
                    else if (Locomotive.CruiseControl.SpeedSelMode == Simulation.RollingStocks.SubSystems.CruiseControl.SpeedSelectorMode.Start)
                    {
                        if (UserInput.IsMouseLeftButtonReleased)
                        {
                            Locomotive.CruiseControl.SpeedSelectorModeStopIncrease();
                        }
                    }
                    else if (p == -1)
                    {
                        Locomotive.CruiseControl.SpeedSelectorModeDecrease();
                    }
                    break;
                case CABViewControlTypes.ORTS_RESTRICTED_SPEED_ZONE_ACTIVE:
                    if (ChangedValue(0) == 1)
                    {
                        Locomotive.CruiseControl.ActivateRestrictedSpeedZone();
                    }
                    break;
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_INCREASE:
                    if (ChangedValue(0) == 1)
                    {
                        Locomotive.CruiseControl.NumerOfAxlesIncrease();
                    }
                    break;
                case CABViewControlTypes.ORTS_NUMBER_OF_AXES_DECREASE:
                    if (ChangedValue(0) == 1)
                    {
                        Locomotive.CruiseControl.NumberOfAxlesDecrease();
                    }
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_SELECTOR:
                    Locomotive.CruiseControl.SpeedRegulatorSelectedSpeedChangeByMouse(ChangedValue(Locomotive.CruiseControl.SpeedSelectorController.IntermediateValue));
                    break;
                case CABViewControlTypes.ORTS_SELECTED_SPEED_MAXIMUM_ACCELERATION:
                    Locomotive.CruiseControl.SpeedRegulatorMaxForceChangeByMouse(ChangedValue(Locomotive.CruiseControl.MaxForceSelectorController.IntermediateValue));
                    break;
                case CABViewControlTypes.ORTS_MULTI_POSITION_CONTROLLER:
                    {
                        foreach (MultiPositionController mpc in Locomotive.MultiPositionControllers)
                        {
                            if (mpc.ControllerId == Control.ControlId)
                            {
                                p = ChangedValue(0);
                                if (!mpc.StateChanged)
                                    mpc.StateChanged = true;
                                if (p != 0 && Locomotive.CruiseControl.SelectedMaxAccelerationPercent == 0 && Locomotive.CruiseControl.DisableCruiseControlOnThrottleAndZeroForce && Locomotive.CruiseControl.ForceRegulatorAutoWhenNonZeroSpeedSelectedAndThrottleAtZero &&
 Locomotive.ThrottleController.CurrentValue == 0 && Locomotive.DynamicBrakeController.CurrentValue == 0 && Locomotive.CruiseControl.SpeedRegMode == Simulation.RollingStocks.SubSystems.CruiseControl.SpeedRegulatorMode.Manual)
                                    Locomotive.CruiseControl.SpeedRegMode = Simulation.RollingStocks.SubSystems.CruiseControl.SpeedRegulatorMode.Auto;
                                if (p == 1)
                                {
                                    if (mpc.controllerBinding == ControllerBinding.SelectedSpeed && Locomotive.CruiseControl.ForceRegulatorAutoWhenNonZeroSpeedSelected)
                                    {
                                        Locomotive.CruiseControl.SpeedRegMode = Simulation.RollingStocks.SubSystems.CruiseControl.SpeedRegulatorMode.Auto;
                                        Locomotive.CruiseControl.SpeedSelMode = Simulation.RollingStocks.SubSystems.CruiseControl.SpeedSelectorMode.On;
                                    }
                                    mpc.DoMovement(MultiPositionController.Movement.Forward);
                                }
                                if (p == -1) mpc.DoMovement(MultiPositionController.Movement.Aft);
                                if (p == 0 && !UserInput.IsMouseLeftButtonDown)
                                {
                                    mpc.DoMovement(MultiPositionController.Movement.Neutral);
                                    mpc.StateChanged = false;
                                }
                            }
                        }
                        break;
                    }
                case CABViewControlTypes.ORTS_CC_SPEED_0:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        Locomotive.CruiseControl.SetSpeed(0);
                    ButtonState = buttonState;
                    break;
				case CABViewControlTypes.ORTS_CC_SPEED_DELTA:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        Locomotive.CruiseControl.SetSpeed(Locomotive.CruiseControl.SetSpeedKpHOrMpH + Control.Parameter1);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_DP_MOVE_TO_FRONT:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new DPMoveToFrontCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_DP_MOVE_TO_BACK:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new DPMoveToBackCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_DP_IDLE:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new DPIdleCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_DP_TRACTION:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new DPTractionCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_DP_BRAKE:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new DPDynamicBrakeCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_DP_MORE:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new DPMoreCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_DP_LESS:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new DPLessCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_EOT_COMM_TEST:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new EOTCommTestCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_EOT_DISARM:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new EOTDisarmCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_EOT_ARM_TWO_WAY:
                    buttonState = ChangedValue(ButtonState ? 1 : 0) > 0;
                    if (!ButtonState && buttonState)
                        new EOTArmTwoWayCommand(Viewer.Log);
                    ButtonState = buttonState;
                    break;
                case CABViewControlTypes.ORTS_EOT_EMERGENCY_BRAKE:
                    if (ChangedValue(0) == 1)
                    {
                        if (Locomotive.Train?.EOT != null)
                        {
                            new ToggleEOTEmergencyBrakeCommand(Viewer.Log);
                        }
                    }
                    break;
            }

        }

        /// <summary>
        /// Translates a percent value to a display index
        /// </summary>
        /// <param name="percent">Percent to be translated</param>
        /// <returns>The calculated display index by the Control's Values</returns>
        protected int PercentToIndex(float percent)
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
                    // Binary search process to find the control value closest to percent
                    // Returns index of first val LARGER than percent, or bitwise compliment of this index if percent isn't in the list
                    int checkIndex = ControlDiscrete.Values.BinarySearch(percent);

                    if (checkIndex < 0)
                        checkIndex = ~checkIndex;
                    if (checkIndex > ControlDiscrete.Values.Count - 1)
                        checkIndex = ControlDiscrete.Values.Count - 1;
                    // Choose lower index if it is closer to percent
                    if (checkIndex > 0 && Math.Abs(ControlDiscrete.Values[checkIndex - 1] - percent) < Math.Abs(ControlDiscrete.Values[checkIndex] - percent))
                        checkIndex--;
                    // If values were originally defined in reverse, correct index to account for the reversing
                    if (ControlDiscrete.Reversed)
                        checkIndex = (ControlDiscrete.Values.Count - 1) - checkIndex;

                    index = checkIndex;
                }
                catch
                {
                    index = ControlDiscrete.Reversed ? ControlDiscrete.Values.Count - 1 : 0;
                }
            }
            else if (ControlDiscrete.MaxValue != ControlDiscrete.MinValue)
            {
                index = (int)(percent / (ControlDiscrete.MaxValue - ControlDiscrete.MinValue) * ControlDiscrete.FramesCount);
            }

            return index;
        }

        public Rectangle DestinationRectangleGet()
        {
            return DestinationRectangle;
        }

        public bool isMouseControl()
        {
            return ControlDiscrete.MouseControl;
        }
    }

    /// <summary>
    /// Discrete renderer for animated controls, like external 2D wiper
    /// </summary>
    public class CabViewAnimationsRenderer : CabViewDiscreteRenderer
    {
        private float CumulativeTime;
        private readonly float CycleTimeS;
        private bool AnimationOn = false;

        public CabViewAnimationsRenderer(Viewer viewer, MSTSLocomotive locomotive, CVCAnimatedDisplay control, CabShader shader)
            : base(viewer, locomotive, control, shader)
        {
            CycleTimeS = control.CycleTimeS;
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            float data;

            if (!IsPowered && Control.ValueIfDisabled != null)
                data = (float)Control.ValueIfDisabled;
            else
                data = Locomotive.GetDataOf(Control);

            var animate = data != 0;
            if (animate)
                AnimationOn = true;

            int index = 0;
            switch (ControlDiscrete.ControlType.Type)
            {
                case CABViewControlTypes.ORTS_2DEXTERNALWIPERS:
                    var halfCycleS = CycleTimeS / 2f;
                    if (AnimationOn)
                    {
                        CumulativeTime += elapsedTime.ClockSeconds;
                        if (CumulativeTime > CycleTimeS && !animate)
                            AnimationOn = false;
                        CumulativeTime %= CycleTimeS;

                        if (CumulativeTime < halfCycleS)
                            index = PercentToIndex(CumulativeTime / halfCycleS);
                        else
                            index = PercentToIndex((CycleTimeS - CumulativeTime) / halfCycleS);
                    }
                    break;
                
                case CABViewControlTypes.ORTS_2DEXTERNALLEFTWINDOW:
                case CABViewControlTypes.ORTS_2DEXTERNALRIGHTWINDOW:
                    var windowIndex = Locomotive.UsingRearCab ? MSTSWagon.RightWindowRearIndex : MSTSWagon.LeftWindowFrontIndex;
                    var soundCorrectionIndex = windowIndex;
                    if (ControlDiscrete.ControlType.Type == CABViewControlTypes.ORTS_2DEXTERNALRIGHTWINDOW)
                    {
                        windowIndex = Locomotive.UsingRearCab ? MSTSWagon.LeftWindowRearIndex : MSTSWagon.RightWindowFrontIndex;
                    }
                    Locomotive.SoundHeardInternallyCorrection[soundCorrectionIndex] = 0;
                    if (AnimationOn)
                    {
                        CumulativeTime += elapsedTime.ClockSeconds;
                        if (CumulativeTime >= CycleTimeS)
                        {
                            AnimationOn = false;
                            CumulativeTime = CycleTimeS;
                        }
                        if (Locomotive.WindowStates[windowIndex] == MSTSWagon.WindowState.Opening)
                        {
                            index = PercentToIndex(CumulativeTime / CycleTimeS);
                            Locomotive.SoundHeardInternallyCorrection[soundCorrectionIndex] = CumulativeTime / CycleTimeS;
                            if (!AnimationOn)
                            {
                                Locomotive.WindowStates[windowIndex] = MSTSWagon.WindowState.Open;
                                CumulativeTime = 0;
                            }
                        }
                        else
                        {
                            index = PercentToIndex((CycleTimeS - CumulativeTime) / CycleTimeS);
                            Locomotive.SoundHeardInternallyCorrection[soundCorrectionIndex] = (CycleTimeS - CumulativeTime) / CycleTimeS;
                            if (!AnimationOn)
                            {
                                Locomotive.WindowStates[windowIndex] = MSTSWagon.WindowState.Closed;
                                CumulativeTime = 0;
                            }
                        }
                    }
                    else
                    {
                        CumulativeTime = 0;
                        if (Locomotive.WindowStates[windowIndex] == MSTSWagon.WindowState.Open)
                        {
                            index = PercentToIndex(1);
                            Locomotive.SoundHeardInternallyCorrection[soundCorrectionIndex] = 1;
                        }
                        else
                        {
                            index = PercentToIndex(0);
                            Locomotive.SoundHeardInternallyCorrection[soundCorrectionIndex] = 0;
                        }
                    }
                    break;
            }

            PrepareFrameForIndex(frame, elapsedTime, index);
        }
    }


    /// <summary>
    /// Digital Cab Control renderer
    /// Uses fonts instead of graphic
    /// </summary>
    public class CabViewDigitalRenderer : CabViewControlRenderer
    {
        public enum CVDigitalAlignment
        {
            Left,
            Center,
            Right,
            // Next ones are used for 3D cabs; digitals of old 3D cab will continue to be displayed left aligned for compatibility
            Cab3DLeft,
            Cab3DCenter,
            Cab3DRight
        }

        public readonly CVDigitalAlignment Alignment;
        string Format = "{0}";
        readonly string Format1 = "{0}";
        readonly string Format2 = "{0}";

        float Num;
        WindowTextFont DrawFont;
        protected Rectangle DrawPosition;
        string DrawText;
        Color DrawColor;
        float DrawRotation;

        [CallOnThread("Loader")]
        public CabViewDigitalRenderer(Viewer viewer, MSTSLocomotive car, CVCDigital digital, CabShader shader)
            : base(viewer, car, digital, shader)
        {
            Position.X = (float)Control.PositionX;
            Position.Y = (float)Control.PositionY;

            // Clock defaults to centered.
            if (Control.ControlType.Type == CABViewControlTypes.CLOCK)
                Alignment = CVDigitalAlignment.Center;
            Alignment = digital.Justification == 1 ? CVDigitalAlignment.Center : digital.Justification == 2 ? CVDigitalAlignment.Left : digital.Justification == 3 ? CVDigitalAlignment.Right : Alignment;
            // Used for 3D cabs
            Alignment = digital.Justification == 4 ? CVDigitalAlignment.Cab3DCenter : digital.Justification == 5 ? CVDigitalAlignment.Cab3DLeft : digital.Justification == 6 ? CVDigitalAlignment.Cab3DRight : Alignment;
            Format1 = "{0:0" + new String('0', digital.LeadingZeros) + (digital.Accuracy > 0 ? "." + new String('0', (int)digital.Accuracy) : "") + "}";
            Format2 = "{0:0" + new String('0', digital.LeadingZeros) + (digital.AccuracySwitch > 0 ? "." + new String('0', (int)(digital.Accuracy + 1)) : "") + "}";
        }

        public override void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var digital = Control as CVCDigital;

            if (!IsPowered && Control.ValueIfDisabled != null)
                Num = (float)Control.ValueIfDisabled;
            else
                Num = Locomotive.GetDataOf(Control);

            if (digital.MinValue < digital.MaxValue) Num = MathHelper.Clamp(Num, (float)digital.MinValue, (float)digital.MaxValue);
            if (Math.Abs(Num) < digital.AccuracySwitch)
                Format = Format2;
            else
                Format = Format1;
            DrawFont = Viewer.WindowManager.TextManager.GetExact(digital.FontFamily, Viewer.CabHeightPixels * digital.FontSize / 480, digital.FontStyle == 0 ? System.Drawing.FontStyle.Regular : System.Drawing.FontStyle.Bold);
            var xScale = Viewer.CabWidthPixels / 640f;
            var yScale = Viewer.CabHeightPixels / 480f;
            // Cab view position adjusted to allow for letterboxing.
            DrawPosition.X = (int)(Position.X * xScale) + (Viewer.CabExceedsDisplayHorizontally > 0 ? DrawFont.Height / 4 : 0) - Viewer.CabXOffsetPixels + Viewer.CabXLetterboxPixels;
            DrawPosition.Y = (int)((Position.Y + Control.Height / 2) * yScale) - DrawFont.Height / 2 + Viewer.CabYOffsetPixels + Viewer.CabYLetterboxPixels;
            DrawPosition.Width = (int)(Control.Width * xScale);
            DrawPosition.Height = (int)(Control.Height * yScale);
            DrawRotation = digital.Rotation;

            if (Control.ControlType.Type == CABViewControlTypes.CLOCK)
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
            digital.OldValue = Num;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            var alignment = (LabelAlignment)Alignment;
            DrawFont.Draw(ControlView.SpriteBatch, DrawPosition, Point.Zero, DrawRotation, DrawText, alignment, DrawColor, Color.Black);
        }

        public string GetDigits(out Color DrawColor)
        {
            try
            {
                var digital = Control as CVCDigital;
                string displayedText = "";

                if (!IsPowered && Control.ValueIfDisabled != null)
                    Num = (float)Control.ValueIfDisabled;
                else
                    Num = Locomotive.GetDataOf(Control);

                if (Math.Abs(Num) < digital.AccuracySwitch)
                    Format = Format2;
                else
                    Format = Format1;

                if (Control.ControlType.Type == CABViewControlTypes.CLOCK)
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
                digital.OldValue = Num;

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

                if (!IsPowered && Control.ValueIfDisabled != null)
                    Num = (float)Control.ValueIfDisabled;
                else
                    Num = Locomotive.GetDataOf(Control);

                if (digital.MinValue < digital.MaxValue) Num = MathHelper.Clamp(Num, (float)digital.MinValue, (float)digital.MaxValue);
                if (Math.Abs(Num) < digital.AccuracySwitch)
                    Format = Format2;
                else
                    Format = Format1;

                if (Control.ControlType.Type == CABViewControlTypes.CLOCK)
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
                digital.OldValue = Num;

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
        public Dictionary<(CabViewControlType, int), AnimatedPartMultiState> AnimateParts = null;
        Dictionary<(CabViewControlType, int), ThreeDimCabGaugeNative> Gauges = null;
        Dictionary<(CabViewControlType, int), AnimatedPart> OnDemandAnimateParts = null; //like external wipers, and other parts that will be switched on by mouse in the future
        //Dictionary<int, DigitalDisplay> DigitParts = null;
        Dictionary<(CabViewControlType, int), ThreeDimCabDigit> DigitParts3D = null;
        Dictionary<(CabViewControlType, int), ThreeDimCabDPI> DPIDisplays3D = null;
        Dictionary<(CabViewControlType, int), ThreeDimCabScreen> ScreenDisplays3D = null;
        AnimatedPart ExternalWipers = null; // setting to zero to prevent a warning. Probably this will be used later. TODO
        protected MSTSLocomotive MSTSLocomotive { get { return (MSTSLocomotive)Car; } }
        MSTSLocomotiveViewer LocoViewer;
        private SpriteBatchMaterial _Sprite2DCabView;
        public bool[] MatrixVisible;
        public ThreeDimentionCabViewer(Viewer viewer, MSTSLocomotive car, MSTSLocomotiveViewer locoViewer)
            : base(viewer, car)
        {
            Locomotive = car;
            _Sprite2DCabView = (SpriteBatchMaterial)viewer.MaterialManager.Load("SpriteBatch");
            LocoViewer = locoViewer;
            if (car.CabView3D != null)
            {
                var shapePath = car.CabView3D.ShapeFilePath;
                TrainCarShape = new PoseableShape(viewer, shapePath + '\0' + Path.GetDirectoryName(shapePath), car.WorldPosition, ShapeFlags.ShadowCaster | ShapeFlags.Interior);
                locoViewer.ThreeDimentionCabRenderer = new CabRenderer(viewer, car, car.CabView3D.CVFFile);
            }
            else locoViewer.ThreeDimentionCabRenderer = locoViewer._CabRenderer;

            AnimateParts = new Dictionary<(CabViewControlType, int), AnimatedPartMultiState>();
            DigitParts3D = new Dictionary<(CabViewControlType, int), ThreeDimCabDigit>();
            Gauges = new Dictionary<(CabViewControlType, int), ThreeDimCabGaugeNative>();
            DPIDisplays3D = new Dictionary<(CabViewControlType, int), ThreeDimCabDPI>();
            ScreenDisplays3D = new Dictionary<(CabViewControlType, int), ThreeDimCabScreen>();
            OnDemandAnimateParts = new Dictionary<(CabViewControlType, int), AnimatedPart>();

            // Find the animated parts
            if (TrainCarShape != null && TrainCarShape.SharedShape.Animations != null)
            {
                MatrixVisible = new bool[TrainCarShape.SharedShape.MatrixNames.Count + 1];
                for (int i = 0; i < MatrixVisible.Length; i++)
                    MatrixVisible[i] = true;
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
                    tmpPart = null;
                    int order;
                    string parameter1 = "0", parameter2 = "";
                    CabViewControlRenderer style = null;
                    //ASPECT_SIGNAL:0:0
                    var tmp = typeName.Split(':');
                    if (tmp.Length < 2 || !int.TryParse(tmp[1].Trim(), out order))
                    {
                        continue;
                    }
                    else if (tmp.Length >= 3)
                    {
                        parameter1 = tmp[2].Trim();
                        if (tmp.Length == 4) //we can get max two parameters per part
                            parameter2 = tmp[3].Trim();
                    }
                    var cvcName = tmp[0].Trim();
                    var cvcType = new CabViewControlType(cvcName);
                    var key = (cvcType, order);
                    switch (cvcType.Type)
                    {
                        case CABViewControlTypes.NONE:
                            continue;
                        case CABViewControlTypes.EXTERNALWIPERS:
                        case CABViewControlTypes.MIRRORS:
                        case CABViewControlTypes.LEFTDOOR:
                        case CABViewControlTypes.RIGHTDOOR:
                        case CABViewControlTypes.ORTS_ITEM1CONTINUOUS:
                        case CABViewControlTypes.ORTS_ITEM2CONTINUOUS:
                        case CABViewControlTypes.ORTS_ITEM1TWOSTATE:
                        case CABViewControlTypes.ORTS_ITEM2TWOSTATE:
                        case CABViewControlTypes.ORTS_EXTERNALLEFTWINDOWFRONT:
                        case CABViewControlTypes.ORTS_EXTERNALRIGHTWINDOWFRONT:
                        case CABViewControlTypes.ORTS_EXTERNALLEFTWINDOWREAR:
                        case CABViewControlTypes.ORTS_EXTERNALRIGHTWINDOWREAR:
                            //cvf file has no external wipers, left door, right door and mirrors key word
                            break;
                        default:
                            if (!locoViewer.ThreeDimentionCabRenderer.ControlMap.TryGetValue(key, out style))
                            {
                                var cvfBasePath = Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "CABVIEW");
                                var cvfFilePath = Path.Combine(cvfBasePath, Locomotive.CVFFileName);
                                Trace.TraceWarning($"Cabview control {cvcName} has not been defined in CVF file {cvfFilePath}");
                            }
                            break;
                    }

                    // This is the case for .s files, for glTF-s it will not be true
                    var targetNode = iMatrix;

                    if (style != null && style is CabViewDigitalRenderer)//digits?
                    {
                        //DigitParts.Add(key, new DigitalDisplay(viewer, TrainCarShape, iMatrix, parameter, locoViewer.ThreeDimentionCabRenderer.ControlMap[key]));
                        DigitParts3D.Add(key, new ThreeDimCabDigit(viewer, iMatrix, parameter1, parameter2, this.TrainCarShape, locoViewer.ThreeDimentionCabRenderer.ControlMap[key], Locomotive));
                        if (!TrainCarShape.SharedShape.StoredResultMatrixes.ContainsKey(targetNode))
                            TrainCarShape.SharedShape.StoredResultMatrixes.Add(targetNode, Matrix.Identity);
                    }
                    else if (style != null && style is CabViewGaugeRenderer)
                    {
                        var CVFR = (CabViewGaugeRenderer)style;

                        if (CVFR.GetGauge().ControlStyle != CABViewControlStyles.POINTER) //pointer will be animated, others will be drawn dynamicaly
                        {
                            Gauges.Add(key, new ThreeDimCabGaugeNative(viewer, iMatrix, parameter1, parameter2, this.TrainCarShape, locoViewer.ThreeDimentionCabRenderer.ControlMap[key]));
                            if (!TrainCarShape.SharedShape.StoredResultMatrixes.ContainsKey(targetNode))
                                TrainCarShape.SharedShape.StoredResultMatrixes.Add(targetNode, Matrix.Identity);
                        }
                        else
                        {//for pointer animation
                            //if there is a part already, will insert this into it, otherwise, create a new
                            if (!AnimateParts.ContainsKey(key))
                            {
                                tmpPart = new AnimatedPartMultiState(TrainCarShape, key);
                                AnimateParts.Add(key, tmpPart);
                            }
                            else tmpPart = AnimateParts[key];
                            tmpPart.AddMatrix(iMatrix); //tmpPart.SetPosition(false);
                            if (!TrainCarShape.SharedShape.StoredResultMatrixes.ContainsKey(targetNode))
                                TrainCarShape.SharedShape.StoredResultMatrixes.Add(targetNode, Matrix.Identity);
                        }
                    }
                    else if (style != null && style is DistributedPowerInterfaceRenderer)
                    {
                        DPIDisplays3D.Add(key, new ThreeDimCabDPI(viewer, iMatrix, parameter1, parameter2, this.TrainCarShape, locoViewer.ThreeDimentionCabRenderer.ControlMap[key]));
                        if (!TrainCarShape.SharedShape.StoredResultMatrixes.ContainsKey(targetNode))
                            TrainCarShape.SharedShape.StoredResultMatrixes.Add(targetNode, Matrix.Identity);
                    }
                    else
                    {
                        //if there is a part already, will insert this into it, otherwise, create a new
                        if (!AnimateParts.ContainsKey(key))
                        {
                            tmpPart = new AnimatedPartMultiState(TrainCarShape, key);
                            AnimateParts.Add(key, tmpPart);
                        }
                        else tmpPart = AnimateParts[key];
                        tmpPart.AddMatrix(iMatrix); //tmpPart.SetPosition(false);
                        if (!TrainCarShape.SharedShape.StoredResultMatrixes.ContainsKey(targetNode))
                            TrainCarShape.SharedShape.StoredResultMatrixes.Add(targetNode, Matrix.Identity);
                    }
                }
            }

            // Find the animated textures, like screens
            if (locoViewer.ThreeDimentionCabRenderer.ControlMap.Values.FirstOrDefault(c => c is DriverMachineInterfaceRenderer) is DriverMachineInterfaceRenderer cvcr
                && cvcr.Control?.ACEFile is var textureName && textureName != null)
            {
                textureName = Path.GetFileName(textureName).ToLower();
                if (TrainCarShape?.SharedShape?.LodControls?.FirstOrDefault()?.DistanceLevels?.FirstOrDefault()?
                .SubObjects?.SelectMany(s => s.ShapePrimitives).Where(p => p.Material.Key.Contains(textureName)).FirstOrDefault() is var primitive && primitive != null)
                {
                    cvcr.SetTexture3D();
                    var material = Viewer.MaterialManager.Load("Screen", TrainCarShape.SharedShape.ReferencePath + cvcr.Control.ACEFile, primitive.HierarchyIndex) as ScreenMaterial;
                    material.Set2DRenderer(cvcr);
                    primitive.SetMaterial(material);
                    ScreenDisplays3D.Add((new CabViewControlType(CABViewControlTypes.ORTS_ETCS), 0),
                        new ThreeDimCabScreen(Viewer, material.HierarchyIndex, TrainCarShape, cvcr));
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
            if (UserInput.IsDown(UserCommand.CameraPanDown)) KeyPressed = true;
            if (UserInput.IsDown(UserCommand.CameraPanUp)) KeyPressed = true;
            if (UserInput.IsDown(UserCommand.CameraPanLeft)) KeyPressed = true;
            if (UserInput.IsDown(UserCommand.CameraPanRight)) KeyPressed = true;
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

            Locomotive.SoundHeardInternallyCorrection[0] = Locomotive.SoundHeardInternallyCorrection[1] = 0;
            foreach (var p in AnimateParts)
            {
                if (p.Value.Type.Type >= CABViewControlTypes.EXTERNALWIPERS) //for wipers, doors and mirrors
                {
                    switch (p.Value.Type.Type)
                    {
                        case CABViewControlTypes.EXTERNALWIPERS:
                            p.Value.UpdateLoop(Locomotive.Wiper, elapsedTime);
                            break;
                        case CABViewControlTypes.LEFTDOOR:
                        case CABViewControlTypes.RIGHTDOOR:
                            {
                                bool right = (p.Value.Type.Type == CABViewControlTypes.RIGHTDOOR) ^ Locomotive.Flipped ^ Locomotive.GetCabFlipped();
                                var state = (right ? Locomotive.RightDoor : Locomotive.LeftDoor).State;
                                p.Value.UpdateState(state >= DoorState.Opening, elapsedTime);
                            }
                            break;
                        case CABViewControlTypes.MIRRORS:
                            p.Value.UpdateState(Locomotive.MirrorOpen, elapsedTime);
                            break;
                        case CABViewControlTypes.ORTS_EXTERNALLEFTWINDOWFRONT:
                            PrepareFrameForWindow(MSTSWagon.LeftWindowFrontIndex, p.Value, elapsedTime);
                            break;
                        case CABViewControlTypes.ORTS_EXTERNALRIGHTWINDOWFRONT:
                            PrepareFrameForWindow(MSTSWagon.RightWindowFrontIndex, p.Value, elapsedTime);
                            break;
                        case CABViewControlTypes.ORTS_EXTERNALLEFTWINDOWREAR:
                            PrepareFrameForWindow(MSTSWagon.LeftWindowRearIndex, p.Value, elapsedTime);
                            break;
                        case CABViewControlTypes.ORTS_EXTERNALRIGHTWINDOWREAR:
                            PrepareFrameForWindow(MSTSWagon.RightWindowRearIndex, p.Value, elapsedTime);
                            break;
                        case CABViewControlTypes.ORTS_ITEM1CONTINUOUS:
                            p.Value.UpdateLoop(Locomotive.GenericItem1, elapsedTime);
                            break;
                        case CABViewControlTypes.ORTS_ITEM2CONTINUOUS:
                            p.Value.UpdateLoop(Locomotive.GenericItem2, elapsedTime);
                            break;
                        case CABViewControlTypes.ORTS_ITEM1TWOSTATE:
                            p.Value.UpdateState(Locomotive.GenericItem1, elapsedTime);
                            break;
                        case CABViewControlTypes.ORTS_ITEM2TWOSTATE:
                            p.Value.UpdateState(Locomotive.GenericItem2, elapsedTime);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    var doShow = true;
                    if (LocoViewer.ThreeDimentionCabRenderer.ControlMap.TryGetValue(p.Key, out var cabRenderer))
                    {
                        if (!cabRenderer.IsPowered && cabRenderer.Control.HideIfDisabled)
                        {
                            doShow = false;
                        }
                        else if (cabRenderer is CabViewDiscreteRenderer)
                        {
                            var control = cabRenderer.Control;
                            if (control.Screens != null && control.Screens[0] != "all")
                            {
                                doShow = control.Screens.Any(screen =>
                                    LocoViewer.ThreeDimentionCabRenderer.ActiveScreen[control.Display] == screen);
                            }
                        }
                    }

                    foreach (var matrixIndex in p.Value.MatrixIndexes)
                        MatrixVisible[matrixIndex] = doShow;

                    p.Value.Update(LocoViewer, elapsedTime); //for all other instruments with animations
                }
            }

            foreach (var p in DigitParts3D)
            {
                var digital = p.Value.CVFR.Control;
                if (digital.Screens != null && digital.Screens[0] != "all")
                {
                    foreach (var screen in digital.Screens)
                    {
                        if (LocoViewer.ThreeDimentionCabRenderer.ActiveScreen[digital.Display] == screen)
                        {
                            p.Value.PrepareFrame(frame, elapsedTime);
                            break;
                        }
                    }
                    continue;
                }
                p.Value.PrepareFrame(frame, elapsedTime);
            }

            foreach (var p in DPIDisplays3D)
            {
                var dpdisplay = p.Value.CVFR.Control;
                if (dpdisplay.Screens != null && dpdisplay.Screens[0] != "all")
                {
                    foreach (var screen in dpdisplay.Screens)
                    {
                        if (LocoViewer.ThreeDimentionCabRenderer.ActiveScreen[dpdisplay.Display] == screen)
                        {
                            p.Value.PrepareFrame(frame, elapsedTime);
                            break;
                        }
                    }
                    continue;
                }
                p.Value.PrepareFrame(frame, elapsedTime);
            }

            foreach (var p in ScreenDisplays3D)
            {
                var screen = p.Value.CVFR.Control;
                if (screen.Screens != null && screen.Screens[0] != "all")
                {
                    foreach (var scr in screen.Screens)
                    {
                        if (LocoViewer.ThreeDimentionCabRenderer.ActiveScreen[screen.Display] == scr)
                        {
                            p.Value.PrepareFrame(frame, elapsedTime);
                            break;
                        }
                    }
                    continue;
                }
                p.Value.PrepareFrame(frame, elapsedTime);
            }

            foreach (var p in Gauges)
            {
                var gauge = p.Value.CVFR.Control;
                if (gauge.Screens != null && gauge.Screens[0] != "all")
                {
                    foreach (var screen in gauge.Screens)
                    {
                        if (LocoViewer.ThreeDimentionCabRenderer.ActiveScreen[gauge.Display] == screen)
                        {
                            p.Value.PrepareFrame(frame, elapsedTime);
                            break;
                        }
                    }
                    continue;
                }
                p.Value.PrepareFrame(frame, elapsedTime);
            }

            if (ExternalWipers != null) ExternalWipers.UpdateLoop(Locomotive.Wiper, elapsedTime);
            /*
            foreach (var p in DigitParts)
            {
                p.Value.PrepareFrame(frame, elapsedTime);
            }*/ //removed with 3D digits

            if (TrainCarShape != null)
                TrainCarShape.ConditionallyPrepareFrame(frame, elapsedTime, MatrixVisible);
        }

        internal void PrepareFrameForWindow(int windowIndex, AnimatedPartMultiState anim, ElapsedTime elapsedTime)
        {
            if (Locomotive.WindowStates[windowIndex] == MSTSWagon.WindowState.Closed) anim.SetState(false);
            else if (Locomotive.WindowStates[windowIndex] == MSTSWagon.WindowState.Open) anim.SetState(true);
            var animationFraction =  anim.UpdateAndReturnState(Locomotive.WindowStates[windowIndex] >= MSTSWagon.WindowState.Opening, elapsedTime);
            if (animationFraction == 0 && Locomotive.WindowStates[windowIndex] < MSTSWagon.WindowState.Opening)
                Locomotive.WindowStates[windowIndex] = MSTSWagon.WindowState.Closed;
            else if (animationFraction == 1 && Locomotive.WindowStates[windowIndex] >= MSTSWagon.WindowState.Opening)
                Locomotive.WindowStates[windowIndex] = MSTSWagon.WindowState.Open;
            if (Locomotive.UsingRearCab ^ windowIndex < 2)
                Locomotive.SoundHeardInternallyCorrection[windowIndex > 1 ? windowIndex - 2 : windowIndex] = animationFraction;
        }

        /// <summary>
        /// Checks this 3D cab viewer for a stale shape and reports if the shape is stale
        /// </summary>
        /// <returns>bool indicating if this 3D cab changed from fresh to stale</returns>
        public bool CheckStale()
        {
            bool found = false;

            if (!Locomotive.StaleViewer)
                found |= TrainCarShape.SharedShape.StaleData;

            return found;
        }

        internal override void Mark()
        {
            TrainCarShape?.Mark();
            foreach (ThreeDimCabDigit threeDimCabDigit in DigitParts3D.Values)
            {
                threeDimCabDigit.Mark();
            }
            foreach (ThreeDimCabDPI threeDimCabDPI in DPIDisplays3D.Values)
            {
                threeDimCabDPI.Mark();
            }
        }
    }

    public class ThreeDimCabDigit
    {
        int MaxDigits = 6;
        PoseableShape TrainCarShape;
        VertexPositionNormalTexture[] VertexList;
        int NumVertices;
        int NumIndices;
        public short[] TriangleListIndices;// Array of indices to vertices for triangles
        Matrix XNAMatrix;
        Viewer Viewer;
        MutableShapePrimitive shapePrimitive;
        public CabViewDigitalRenderer CVFR;
        Material Material;
        Material AlertMaterial;
        float Size;
        string AceFile;
        public ThreeDimCabDigit(Viewer viewer, int iMatrix, string size, string aceFile, PoseableShape trainCarShape, CabViewControlRenderer c, MSTSLocomotive locomotive)
        {

            Size = int.Parse(size) * 0.001f;//input size is in mm
            if (aceFile != "")
            {
                AceFile = aceFile.ToUpper();
                if (!AceFile.EndsWith(".ACE")) AceFile = AceFile + ".ACE"; //need to add ace into it
            }
            else { AceFile = ""; }

            CVFR = (CabViewDigitalRenderer)c;
            var digital = CVFR.Control as CVCDigital;
            if (digital.ControlType.Type == CABViewControlTypes.CLOCK && digital.Accuracy > 0) MaxDigits = 8;
            Viewer = viewer;
            TrainCarShape = trainCarShape;
            XNAMatrix = TrainCarShape.SharedShape.Matrices[iMatrix];
            var maxVertex = (MaxDigits + 2) * 4;// every face has max 8 digits, each has 2 triangles
            //Material = viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(viewer.Simulator, Helpers.TextureFlags.None, texture), (int)(SceneryMaterialOptions.None | SceneryMaterialOptions.AlphaBlendingBlend), 0);
            Material = FindMaterial(false);//determine normal material
            // Create and populate a new ShapePrimitive
            NumVertices = NumIndices = 0;

            VertexList = new VertexPositionNormalTexture[maxVertex];
            TriangleListIndices = new short[maxVertex / 2 * 3]; // as is NumIndices

            //start position is the center of the text
            var start = new Vector3(0, 0, 0);
            var rotation = locomotive.UsingRearCab ? (float)Math.PI : 0;

            //find the left-most of text
            Vector3 offset;

            offset.X = 0;

            offset.Y = -Size;

            var speed = new string('0', MaxDigits);
            foreach (char ch in speed)
            {
                var tX = GetTextureCoordX(ch);
                var tY = GetTextureCoordY(ch);
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

            //create the shape primitive
            shapePrimitive = new MutableShapePrimitive(Material, NumVertices, NumIndices, new[] { -1 }, 0);
            UpdateShapePrimitive(Material);

        }

        Material FindMaterial(bool Alert)
        {
            string imageName = "";
            string globalText = Viewer.Simulator.BasePath + @"\GLOBAL\TEXTURES\";
            CABViewControlTypes controltype = CVFR.GetControlType().Type;
            Material material = null;

            if (Alert) { imageName = "alert.ace"; }
            else if (AceFile != "")
            {
                imageName = AceFile;
            }
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

            Material UsedMaterial = Material; //use default material

            //update text string
            bool Alert;
            string speed = CVFR.Get3DDigits(out Alert);

            NumVertices = NumIndices = 0;

            // add leading blanks to consider alignment
            // for backwards compatibiliy with preceding OR releases all Justification values defined by MSTS are considered as left justified
            var leadingBlankCount = 0;
            switch (CVFR.Alignment)
            {
                case CabViewDigitalRenderer.CVDigitalAlignment.Cab3DRight:
                    leadingBlankCount = MaxDigits - speed.Length;
                    break;
                case CabViewDigitalRenderer.CVDigitalAlignment.Cab3DCenter:
                    leadingBlankCount = (MaxDigits - speed.Length + 1) / 2;
                    break;
                default:
                    break;
            }
            for (int i = leadingBlankCount; i > 0; i--)
                speed = speed.Insert(0, " ");

            if (Alert)//alert use alert meterial
            {
                if (AlertMaterial == null) AlertMaterial = FindMaterial(true);
                UsedMaterial = AlertMaterial;
            }
            //update vertex texture coordinate
            foreach (char ch in speed.Substring(0, Math.Min(speed.Length, MaxDigits)))
            {
                var tX = GetTextureCoordX(ch);
                var tY = GetTextureCoordY(ch);
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

            //update the shape primitive
            UpdateShapePrimitive(UsedMaterial);

        }

        private void UpdateShapePrimitive(Material material)
        {
            var indexData = new short[NumIndices];
            Array.Copy(TriangleListIndices, indexData, NumIndices);
            shapePrimitive.SetIndexData(indexData);

            var vertexData = new VertexPositionNormalTexture[NumVertices];
            Array.Copy(VertexList, vertexData, NumVertices);
            shapePrimitive.SetVertexData(vertexData, 0, NumVertices, NumIndices / 3);

            shapePrimitive.SetMaterial(material);
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
            if (!CVFR.IsPowered && CVFR.Control.HideIfDisabled)
                return;

            UpdateDigit();
            Matrix mx = TrainCarShape.Location.XNAMatrix;
            mx.M41 += (TrainCarShape.Location.TileX - Viewer.Camera.TileX) * 2048;
            mx.M43 += (-TrainCarShape.Location.TileZ + Viewer.Camera.TileZ) * 2048;
            Matrix m = XNAMatrix * mx;

            // TODO: Make this use AddAutoPrimitive instead.
            frame.AddPrimitive(this.shapePrimitive.Material, this.shapePrimitive, RenderPrimitiveGroup.Interior, ref m, ShapeFlags.None);
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
        MutableShapePrimitive shapePrimitive;
        public CabViewGaugeRenderer CVFR;
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


            //create the shape primitive
            var material = FindMaterial();
            shapePrimitive = new MutableShapePrimitive(material, NumVertices, NumIndices, new[] { -1 }, 0);
            UpdateShapePrimitive(material);

        }

        Material FindMaterial()
        {
            bool Positive;
            color c = this.CVFR.GetColor(out Positive);
            if (Positive)
            {
                if (PositiveMaterial == null)
                {
                    PositiveMaterial = new SolidColorMaterial(this.Viewer, c.A, c.R, c.G, c.B);
                }
                return PositiveMaterial;
            }
            else
            {
                if (NegativeMaterial == null)
                    NegativeMaterial = new SolidColorMaterial(this.Viewer, c.A, c.R, c.G, c.B);
                return NegativeMaterial;
            }
        }

        //update the digits with current speed or time
        public void UpdateDigit()
        {
            NumVertices = 0;

            Material UsedMaterial = FindMaterial();

            float length = CVFR.GetRangeFraction(true);

            CVCGauge gauge = CVFR.GetGauge();

            var len = maxLen * length;
            var absLen = Math.Abs(len);
            Vertex v1, v2, v3, v4;

            //the left-bottom vertex if ori=0;dir=0, right-bottom if ori=0,dir=1; left-top if ori=1,dir=0; left-bottom if ori=1,dir=1;
            v1 = new Vertex(0f, 0f, 0.002f, 0, 0, -1, 0f, 0f);

            if (Orientation == 0)
            {
                if (Direction == 0 ^ len < 0)//moving right
                {
                    //other vertices
                    v2 = new Vertex(0f, width, 0.002f, 0, 0, 1, 0f, 0f);
                    v3 = new Vertex(absLen, width, 0.002f, 0, 0, 1, 0f, 0f);
                    v4 = new Vertex(absLen, 0f, 0.002f, 0, 0, 1, 0f, 0f);
                }
                else //moving left
                {
                    v4 = new Vertex(0f, width, 0.002f, 0, 0, 1, 0f, 0f);
                    v3 = new Vertex(-absLen, width, 0.002f, 0, 0, 1, 0f, 0f);
                    v2 = new Vertex(-absLen, 0f, 0.002f, 0, 0, 1, 0f, 0f);
                }
            }
            else
            {
                if (Direction == 1 ^ len < 0)//up
                {
                    //other vertices
                    v2 = new Vertex(0f, absLen, 0.002f, 0, 0, 1, 0f, 0f);
                    v3 = new Vertex(width, absLen, 0.002f, 0, 0, 1, 0f, 0f);
                    v4 = new Vertex(width, 0f, 0.002f, 0, 0, 1, 0f, 0f);
                }
                else //moving down
                {
                    v4 = new Vertex(0f, -absLen, 0.002f, 0, 0, 1, 0f, 0f);
                    v3 = new Vertex(width, -absLen, 0.002f, 0, 0, 1, 0f, 0f);
                    v2 = new Vertex(width, 0, 0.002f, 0, 0, 1, 0f, 0f);
                }
            }

            //create vertex list
            VertexList[NumVertices].Position = v1.Position; VertexList[NumVertices].Normal = v1.Normal; VertexList[NumVertices].TextureCoordinate = v1.TexCoord;
            VertexList[NumVertices + 1].Position = v2.Position; VertexList[NumVertices + 1].Normal = v2.Normal; VertexList[NumVertices + 1].TextureCoordinate = v2.TexCoord;
            VertexList[NumVertices + 2].Position = v3.Position; VertexList[NumVertices + 2].Normal = v3.Normal; VertexList[NumVertices + 2].TextureCoordinate = v3.TexCoord;
            VertexList[NumVertices + 3].Position = v4.Position; VertexList[NumVertices + 3].Normal = v4.Normal; VertexList[NumVertices + 3].TextureCoordinate = v4.TexCoord;
            NumVertices += 4;

            //update the shape primitive
            UpdateShapePrimitive(UsedMaterial);

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

        private void UpdateShapePrimitive(Material material)
        {
            var indexData = new short[NumIndices];
            Array.Copy(TriangleListIndices, indexData, NumIndices);
            shapePrimitive.SetIndexData(indexData);

            var vertexData = new VertexPositionNormalTexture[NumVertices];
            Array.Copy(VertexList, vertexData, NumVertices);
            shapePrimitive.SetVertexData(vertexData, 0, NumVertices, NumIndices / 3);

            shapePrimitive.SetMaterial(material);
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            if (!CVFR.IsPowered && CVFR.Control.HideIfDisabled)
                return;

            UpdateDigit();
            Matrix mx = TrainCarShape.Location.XNAMatrix;
            mx.M41 += (TrainCarShape.Location.TileX - Viewer.Camera.TileX) * 2048;
            mx.M43 += (-TrainCarShape.Location.TileZ + Viewer.Camera.TileZ) * 2048;
            Matrix m = XNAMatrix * mx;

            // TODO: Make this use AddAutoPrimitive instead.
            frame.AddPrimitive(this.shapePrimitive.Material, this.shapePrimitive, RenderPrimitiveGroup.Interior, ref m, ShapeFlags.None);
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

            var scale = CVFR.IsPowered || CVFR.Control.ValueIfDisabled == null ? CVFR.GetRangeFraction() : (float)CVFR.Control.ValueIfDisabled;

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
        public CabViewControlType Type;
        public (CabViewControlType, int) Key;
        /// <summary>
        /// Construct with a link to the shape that contains the animated parts 
        /// </summary>
        public AnimatedPartMultiState(PoseableShape poseableShape, (CabViewControlType, int) k)
            : base(poseableShape)
        {
            Type = k.Item1;
            Key = k;
        }

        /// <summary>
        /// Transition the part toward the specified state. 
        /// </summary>
        public void Update(MSTSLocomotiveViewer locoViewer, ElapsedTime elapsedTime)
        {
            if (MatrixIndexes.Count == 0 || !locoViewer._has3DCabRenderer)
                return;

            if (locoViewer.ThreeDimentionCabRenderer.ControlMap.TryGetValue(Key, out CabViewControlRenderer cvfr))
            {
                float index = cvfr is CabViewDiscreteRenderer renderer ? renderer.GetDrawIndex() : cvfr.GetRangeFraction() * MaxFrame;
                SetFrameClamp(index);
            }
        }
    }
}
