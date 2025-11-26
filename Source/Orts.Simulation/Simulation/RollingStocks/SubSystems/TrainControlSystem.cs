// COPYRIGHT 2021 by the Open Rails project.
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

// Define this to log the wheel configurations on cars as they are loaded.
//#define DEBUG_WHEELS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.Signalling;
using ORTS.Common;
using ORTS.Scripting.Api;
using ORTS.Scripting.Api.ETCS;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks.SubSystems
{
    public class ScriptedTrainControlSystem : ISubSystem<ScriptedTrainControlSystem>
    {
        public class MonitoringDevice
        {
            public float MonitorTimeS = 66; // Time from alerter reset to applying emergency brake
            public float AlarmTimeS = 60; // Time from alerter reset to audible and visible alarm
            public float PenaltyTimeS;
            public float CriticalLevelMpS;
            public float ResetLevelMpS;
            public bool AppliesFullBrake = true;
            public bool AppliesEmergencyBrake;
            public bool EmergencyCutsPower;
            public bool EmergencyShutsDownEngine;
            public float AlarmTimeBeforeOverspeedS = 5;         // OverspeedMonitor only
            public float TriggerOnOverspeedMpS;                 // OverspeedMonitor only
            public bool TriggerOnTrackOverspeed;                // OverspeedMonitor only
            public float TriggerOnTrackOverspeedMarginMpS = 4;  // OverspeedMonitor only
            public bool ResetOnDirectionNeutral = false;
            public bool ResetOnZeroSpeed = true;
            public bool ResetOnResetButton;                     // OverspeedMonitor only

            public MonitoringDevice(STFReader stf)
            {
                stf.MustMatch("(");
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("monitoringdevicemonitortimelimit", () => { MonitorTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, MonitorTimeS); }),
                    new STFReader.TokenProcessor("monitoringdevicealarmtimelimit", () => { AlarmTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, AlarmTimeS); }),
                    new STFReader.TokenProcessor("monitoringdevicepenaltytimelimit", () => { PenaltyTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, PenaltyTimeS); }),
                    new STFReader.TokenProcessor("monitoringdevicecriticallevel", () => { CriticalLevelMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, CriticalLevelMpS); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetlevel", () => { ResetLevelMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, ResetLevelMpS); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliesfullbrake", () => { AppliesFullBrake = stf.ReadBoolBlock(AppliesFullBrake); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliesemergencybrake", () => { AppliesEmergencyBrake = stf.ReadBoolBlock(AppliesEmergencyBrake); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliescutspower", () => { EmergencyCutsPower = stf.ReadBoolBlock(EmergencyCutsPower); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliesshutsdownengine", () => { EmergencyShutsDownEngine = stf.ReadBoolBlock(EmergencyShutsDownEngine); }),
                    new STFReader.TokenProcessor("monitoringdevicealarmtimebeforeoverspeed", () => { AlarmTimeBeforeOverspeedS = stf.ReadFloatBlock(STFReader.UNITS.Time, AlarmTimeBeforeOverspeedS); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggeronoverspeed", () => { TriggerOnOverspeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, TriggerOnOverspeedMpS); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggerontrackoverspeed", () => { TriggerOnTrackOverspeed = stf.ReadBoolBlock(TriggerOnTrackOverspeed); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggerontrackoverspeedmargin", () => { TriggerOnTrackOverspeedMarginMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, TriggerOnTrackOverspeedMarginMpS); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetondirectionneutral", () => { ResetOnDirectionNeutral = stf.ReadBoolBlock(ResetOnDirectionNeutral); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetonresetbutton", () => { ResetOnResetButton = stf.ReadBoolBlock(ResetOnResetButton); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetonzerospeed", () => { ResetOnZeroSpeed = stf.ReadBoolBlock(ResetOnZeroSpeed); }),
                });
            }

            public MonitoringDevice() { }

            public MonitoringDevice(MonitoringDevice other)
            {
                MonitorTimeS = other.MonitorTimeS;
                AlarmTimeS = other.AlarmTimeS;
                PenaltyTimeS = other.PenaltyTimeS;
                CriticalLevelMpS = other.CriticalLevelMpS;
                ResetLevelMpS = other.ResetLevelMpS;
                AppliesFullBrake = other.AppliesFullBrake;
                AppliesEmergencyBrake = other.AppliesEmergencyBrake;
                EmergencyCutsPower = other.EmergencyCutsPower;
                EmergencyShutsDownEngine = other.EmergencyShutsDownEngine;
                AlarmTimeBeforeOverspeedS = other.AlarmTimeBeforeOverspeedS;
                TriggerOnOverspeedMpS = other.TriggerOnOverspeedMpS;
                TriggerOnTrackOverspeed = other.TriggerOnTrackOverspeed;
                TriggerOnTrackOverspeedMarginMpS = other.TriggerOnTrackOverspeedMarginMpS;
                ResetOnDirectionNeutral = other.ResetOnDirectionNeutral;
                ResetOnZeroSpeed = other.ResetOnZeroSpeed;
                ResetOnResetButton = other.ResetOnResetButton;
            }
        }

        // Properties
        public bool VigilanceAlarm { get; set; }
        public bool VigilanceEmergency { get; set; }
        public bool OverspeedWarning { get; set; }
        public bool PenaltyApplication { get; set; }
        public float CurrentSpeedLimitMpS { get; set; }
        public float NextSpeedLimitMpS { get; set; }
        public TrackMonitorSignalAspect CabSignalAspect { get; set; }

        public bool Activated = false;
        public bool CustomTCSScript = false;

        public readonly MSTSLocomotive Locomotive;
        public readonly Simulator Simulator;

        float ItemSpeedLimit;
        Aspect ItemAspect;
        float ItemDistance;
        string MainHeadSignalTypeName;

        MonitoringDevice VigilanceMonitor;
        MonitoringDevice OverspeedMonitor;
        MonitoringDevice EmergencyStopMonitor;
        MonitoringDevice AWSMonitor;

        private bool simulatorEmergencyBraking = false;
        public bool SimulatorEmergencyBraking {
            get
            {
                return simulatorEmergencyBraking;
            }
            protected set
            {
                simulatorEmergencyBraking = value;

                if (Script != null)
#pragma warning disable CS0618 // SetEmergency is obsolete
                    Script.SetEmergency(value);
#pragma warning restore CS0618 // SetEmergency is obsolete
                else
                    Locomotive.TrainBrakeController.TCSEmergencyBraking = value;
            }
        }
        public bool AlerterButtonPressed { get; private set; }
        public bool PowerAuthorization { get; private set; }
        public bool CircuitBreakerClosingOrder { get; private set; }
        public bool CircuitBreakerOpeningOrder { get; private set; }
        public bool TractionAuthorization { get; private set; }
        public bool DynamicBrakingAuthorization { get; private set; }
        public float MaxThrottlePercent { get; private set; } = 100f;
        public bool FullDynamicBrakingOrder { get; private set; }

        public Dictionary<int, float> CabDisplayControls = new Dictionary<int, float>();

        // generic TCS commands
        public Dictionary<int, bool> TCSCommandButtonDown = new Dictionary<int, bool>();
        public Dictionary<int, bool> TCSCommandSwitchOn = new Dictionary<int, bool>();
        // List of customized control strings;
        public Dictionary<int, string> CustomizedCabviewControlNames = new Dictionary<int, string>();
        // TODO : Delete this when SetCustomizedTCSControlString is deleted
        protected int NextCabviewControlNameToEdit = 0;

        string ScriptName;
        string SoundFileName;
        string ParametersFileName;
        string TrainParametersFileName;
        TrainControlSystem Script;

        public ETCSStatus ETCSStatus { get { return Script?.ETCSStatus; } }

        public Dictionary<TrainControlSystem, string> Sounds = new Dictionary<TrainControlSystem, string>();

        const float GravityMpS2 = 9.80665f;
        const float GenericItemDistance = 400.0f;

        public ScriptedTrainControlSystem() { }

        public ScriptedTrainControlSystem(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
            Simulator = Locomotive.Simulator;

            PowerAuthorization = true;
            CircuitBreakerClosingOrder = false;
            CircuitBreakerOpeningOrder = false;
            TractionAuthorization = true;
            DynamicBrakingAuthorization = true;
            FullDynamicBrakingOrder = false;
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(vigilancemonitor": VigilanceMonitor = new MonitoringDevice(stf); break;
                case "engine(overspeedmonitor": OverspeedMonitor = new MonitoringDevice(stf); break;
                case "engine(emergencystopmonitor": EmergencyStopMonitor = new MonitoringDevice(stf); break;
                case "engine(awsmonitor": AWSMonitor = new MonitoringDevice(stf); break;
                case "engine(ortstraincontrolsystem": ScriptName = stf.ReadStringBlock(null); break;
                case "engine(ortstraincontrolsystemsound": SoundFileName = stf.ReadStringBlock(null); break;
                case "engine(ortstraincontrolsystemparameters": ParametersFileName = stf.ReadStringBlock(null); break;
            }
        }

        public void Copy(ScriptedTrainControlSystem other)
        {
            ScriptName = other.ScriptName;
            SoundFileName = other.SoundFileName;
            ParametersFileName = other.ParametersFileName;
            TrainParametersFileName = other.TrainParametersFileName;
            if (other.VigilanceMonitor != null) VigilanceMonitor = new MonitoringDevice(other.VigilanceMonitor);
            if (other.OverspeedMonitor != null) OverspeedMonitor = new MonitoringDevice(other.OverspeedMonitor);
            if (other.EmergencyStopMonitor != null) EmergencyStopMonitor = new MonitoringDevice(other.EmergencyStopMonitor);
            if (other.AWSMonitor != null) AWSMonitor = new MonitoringDevice(other.AWSMonitor);
        }

        //Debrief Eval
        public static int DbfevalFullBrakeAbove16kmh = 0;
        public bool ldbfevalfullbrakeabove16kmh = false;

        public void Initialize()
        {
            if (!Activated)
            {
                if (!Simulator.Settings.DisableTCSScripts && ScriptName != null && ScriptName != "MSTS" && ScriptName != "")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as TrainControlSystem;
                    CustomTCSScript = true;
                }

                if (ParametersFileName != null)
                {
                    ParametersFileName = Path.Combine(Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script"), ParametersFileName);
                }

                if (Locomotive.Train.TcsParametersFileName != null)
                {
                    TrainParametersFileName = Path.Combine(Simulator.BasePath, @"TRAINS\CONSISTS\SCRIPT\", Locomotive.Train.TcsParametersFileName);
                }

                if (Script == null)
                {
                    Script = new MSTSTrainControlSystem();
                    ((MSTSTrainControlSystem)Script).VigilanceMonitor = VigilanceMonitor;
                    ((MSTSTrainControlSystem)Script).OverspeedMonitor = OverspeedMonitor;
                    ((MSTSTrainControlSystem)Script).EmergencyStopMonitor = EmergencyStopMonitor;
                    ((MSTSTrainControlSystem)Script).AWSMonitor = AWSMonitor;
                    ((MSTSTrainControlSystem)Script).EmergencyCausesThrottleDown = Locomotive.EmergencyCausesThrottleDown;
                    ((MSTSTrainControlSystem)Script).EmergencyEngagesHorn = Locomotive.EmergencyEngagesHorn;
                }

                if (SoundFileName != null)
                {
                    var soundPathArray = new[] {
                    Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "SOUND"),
                    Path.Combine(Simulator.BasePath, "SOUND"),
                };
                    var soundPath = ORTSPaths.GetFileFromFolders(soundPathArray, SoundFileName);
                    if (File.Exists(soundPath))
                        Sounds.Add(Script, soundPath);
                }

                // AbstractScriptClass
                Script.Car = Locomotive;
                Script.ClockTime = () => (float)Simulator.ClockTime;
                Script.GameTime = () => (float)Simulator.GameTime;
                Script.PreUpdate = () => Simulator.PreUpdate;
                Script.DistanceM = () => Locomotive.DistanceM;
                Script.Confirm = Locomotive.Simulator.Confirmer.Confirm;
                Script.Message = Locomotive.Simulator.Confirmer.Message;
                Script.SignalEvent = Locomotive.SignalEvent;
                Script.SignalEventToTrain = (evt) =>
                {
                    if (Locomotive.Train != null)
                    {
                        Locomotive.Train.SignalEvent(evt);
                    }
                };

                // TrainControlSystem getters
                Script.IsTrainControlEnabled = () => Locomotive == Locomotive.Train.LeadLocomotive && Locomotive.Train.TrainType != Train.TRAINTYPE.AI_PLAYERHOSTING && !Locomotive.Train.Autopilot;
                Script.IsAutopiloted = () => Locomotive == Simulator.PlayerLocomotive && (Locomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING || Locomotive.Train.Autopilot);
                Script.IsAlerterEnabled = () =>
                {
                    return Simulator.Settings.Alerter
                        && !(Simulator.Settings.AlerterDisableExternal
                            && !Simulator.PlayerIsInCab
                        );
                };
                Script.IsSpeedControlEnabled = () => Simulator.Settings.SpeedControl;
                Script.IsLowVoltagePowerSupplyOn = () => Locomotive.LocomotivePowerSupply.LowVoltagePowerSupplyOn;
                Script.IsCabPowerSupplyOn = () => Locomotive.LocomotivePowerSupply.CabPowerSupplyOn;
                Script.AlerterSound = () => Locomotive.AlerterSnd;
                Script.TrainSpeedLimitMpS = () => Math.Min(Locomotive.Train.AllowedMaxSpeedMpS, Locomotive.Train.TrainMaxSpeedMpS);
                Script.TrainMaxSpeedMpS = () => Locomotive.Train.TrainMaxSpeedMpS; // max speed for train in a specific section, independently from speedpost and signal limits
                Script.CurrentSignalSpeedLimitMpS = () => Locomotive.Train.allowedMaxSpeedSignalMpS;
                Script.NextSignalSpeedLimitMpS = (value) => NextGenericSignalItem<float>(value, ref ItemSpeedLimit, float.MaxValue,Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL, "NORMAL");
                Script.NextSignalAspect = (value) => NextGenericSignalItem<Aspect>(value, ref ItemAspect, float.MaxValue, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL, "NORMAL");
                Script.NextSignalDistanceM = (value) => NextGenericSignalItem<float>(value, ref ItemDistance, float.MaxValue, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL, "NORMAL");
                Script.NextNormalSignalDistanceHeadsAspect = () => NextNormalSignalDistanceHeadsAspect();
                Script.DoesNextNormalSignalHaveTwoAspects = () => DoesNextNormalSignalHaveTwoAspects();
                Script.NextDistanceSignalAspect = () =>
                    NextGenericSignalItem<Aspect>(0, ref ItemAspect, GenericItemDistance, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL, "DISTANCE");
                Script.NextDistanceSignalDistanceM = () =>
                    NextGenericSignalItem<float>(0, ref ItemDistance, GenericItemDistance, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL, "DISTANCE");
                Script.NextGenericSignalMainHeadSignalType = (string type) =>
                    NextGenericSignalItem<string>(0, ref MainHeadSignalTypeName, GenericItemDistance, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL, type);
                Script.NextGenericSignalAspect = (string type) =>
                    NextGenericSignalItem<Aspect>(0, ref ItemAspect, GenericItemDistance, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL, type);
                Script.NextGenericSignalDistanceM = (string type) =>
                    NextGenericSignalItem<float>(0, ref ItemDistance, GenericItemDistance, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL, type);
                Script.NextGenericSignalFeatures = (arg1, arg2, arg3) => NextGenericSignalFeatures(arg1, arg2, arg3, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL);
                Script.NextSpeedPostFeatures = (arg1, arg2) => NextSpeedPostFeatures(arg1, arg2);
                Script.DoesNextNormalSignalHaveRepeaterHead = () => DoesNextNormalSignalHaveRepeaterHead();
                Script.CurrentPostSpeedLimitMpS = () => Locomotive.Train.allowedMaxSpeedLimitMpS;
                Script.NextPostSpeedLimitMpS = (value) => NextGenericSignalItem<float>(value, ref ItemSpeedLimit, float.MaxValue, Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST);
                Script.NextPostDistanceM = (value) => NextGenericSignalItem<float>(value, ref ItemDistance, float.MaxValue, Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST);
                Script.NextTunnel = (value) =>
                {
                    var list = Locomotive.Train.PlayerTrainTunnels[Locomotive.Train.MUDirection == Direction.Reverse ? 1 : 0];
                    if (list == null || value >= list.Count) return new TunnelInfo(float.MaxValue, -1);
                    return new TunnelInfo(list[value].DistanceToTrainM, list[value].StationPlatformLength);
                };
                Script.NextMilepost = (value) =>
                {
                    var list = Locomotive.Train.PlayerTrainMileposts[Locomotive.Train.MUDirection == Direction.Reverse ? 1 : 0];
                    if (list == null || value >= list.Count) return new MilepostInfo(float.MaxValue, -1);
                    return new MilepostInfo(list[value].DistanceToTrainM, float.Parse(list[value].ThisMile));
                };
                Script.EOADistanceM = (value) => Locomotive.Train.DistanceToEndNodeAuthorityM[value];
                Script.SpeedMpS = () => Math.Abs(Locomotive.SpeedMpS);
                Script.CurrentDirection = () => Locomotive.Direction; // Direction of locomotive, may be different from direction of train
                Script.IsDirectionForward = () => Locomotive.Direction == Direction.Forward;
                Script.IsDirectionNeutral = () => Locomotive.Direction == Direction.N;
                Script.IsDirectionReverse = () => Locomotive.Direction == Direction.Reverse;
                Script.CurrentTrainMUDirection = () => Locomotive.Train.MUDirection; // Direction of train
                Script.IsFlipped = () => Locomotive.Flipped;
                Script.IsRearCab = () => Locomotive.UsingRearCab;
                Script.IsBrakeEmergency = () => Locomotive.TrainBrakeController.EmergencyBraking;
                Script.IsBrakeFullService = () => Locomotive.TrainBrakeController.TCSFullServiceBraking;
                Script.PowerAuthorization = () => PowerAuthorization;
                Script.CircuitBreakerClosingOrder = () => CircuitBreakerClosingOrder;
                Script.CircuitBreakerOpeningOrder = () => CircuitBreakerOpeningOrder;
                Script.PantographCount = () => Locomotive.Pantographs.Count;
                Script.GetPantographState = (pantoID) =>
                {
                    if (pantoID >= Pantographs.MinPantoID && pantoID <= Pantographs.MaxPantoID)
                    {
                        return Locomotive.Pantographs[pantoID].State;
                    }
                    else
                    {
                        Trace.TraceError($"TCS script used bad pantograph ID {pantoID}");
                        return PantographState.Down;
                    }
                };
                Script.ArePantographsDown = () => Locomotive.Pantographs.State == PantographState.Down;
                Script.CurrentDoorState = (side) => Locomotive.Train.DoorState(Locomotive.Flipped ^ Locomotive.GetCabFlipped() ? Doors.FlippedDoorSide(side) : side);
                Script.ThrottlePercent = () => Locomotive.ThrottleController.CurrentValue * 100;
                Script.MaxThrottlePercent = () => MaxThrottlePercent;
                Script.DynamicBrakePercent = () => Locomotive.DynamicBrakeController == null ? 0 : Locomotive.DynamicBrakeController.CurrentValue * 100;
                Script.TractionAuthorization = () => TractionAuthorization;
                Script.DynamicBrakingAuthorization = () => DynamicBrakingAuthorization;
                Script.BrakePipePressureBar = () => Locomotive.BrakeSystem != null ? Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) : float.MaxValue;
                Script.LocomotiveBrakeCylinderPressureBar = () => Locomotive.BrakeSystem != null ? Bar.FromPSI(Locomotive.BrakeSystem.GetCylPressurePSI()) : float.MaxValue;
                Script.DoesBrakeCutPower = () => Locomotive.DoesBrakeCutPower;
                Script.BrakeCutsPowerAtBrakeCylinderPressureBar = () => Bar.FromPSI(Locomotive.BrakeCutsPowerAtBrakeCylinderPressurePSI);
                Script.TrainBrakeControllerState = () => Locomotive.TrainBrakeController.TrainBrakeControllerState;
                Script.AccelerationMpSS = () => Locomotive.AccelerationMpSS;
                Script.AltitudeM = () => Locomotive.WorldPosition.Location.Y;
                Script.CurrentGradientPercent = () => -Locomotive.CurrentElevationPercent;
                Script.LineSpeedMpS = () => (float)Simulator.TRK.Tr_RouteFile.SpeedLimit;
                Script.SignedDistanceM = () => Locomotive.Train.DistanceTravelledM;
                Script.DoesStartFromTerminalStation = () => DoesStartFromTerminalStation();
                Script.IsColdStart = () => Locomotive.Train.ColdStart;
                Script.GetTrackNodeOffset = () => Locomotive.Train.FrontTDBTraveller.TrackNodeLength - Locomotive.Train.FrontTDBTraveller.TrackNodeOffset;
                Script.NextDivergingSwitchDistanceM = (value) =>
                {
                    var list = Locomotive.Train.PlayerTrainDivergingSwitches[Locomotive.Train.MUDirection == Direction.Reverse ? 1 : 0, 0];
                    if (list == null || list.Count == 0 || list[0].DistanceToTrainM > value) return float.MaxValue;
                    return list[0].DistanceToTrainM;
                };
                Script.NextTrailingDivergingSwitchDistanceM = (value) =>
                {
                    var list = Locomotive.Train.PlayerTrainDivergingSwitches[Locomotive.Train.MUDirection == Direction.Reverse ? 1 : 0, 1];
                    if (list == null || list.Count == 0 || list[0].DistanceToTrainM > value) return float.MaxValue;
                    return list[0].DistanceToTrainM;
                };
                Script.GetControlMode = () => (TRAIN_CONTROL)(int)Locomotive.Train.ControlMode;
                Script.NextStationName = () => Locomotive.Train.StationStops != null && Locomotive.Train.StationStops.Count > 0 ? Locomotive.Train.StationStops[0].PlatformItem.Name : "";
                Script.NextStationDistanceM = () => Locomotive.Train.StationStops != null && Locomotive.Train.StationStops.Count > 0 ? Locomotive.Train.StationStops[0].DistanceToTrainM : float.MaxValue;

                // TrainControlSystem functions
                Script.SpeedCurve = (arg1, arg2, arg3, arg4, arg5) => SpeedCurve(arg1, arg2, arg3, arg4, arg5);
                Script.DistanceCurve = (arg1, arg2, arg3, arg4, arg5) => DistanceCurve(arg1, arg2, arg3, arg4, arg5);
                Script.Deceleration = (arg1, arg2, arg3) => Deceleration(arg1, arg2, arg3);

                // TrainControlSystem setters
                Script.SetFullBrake = (value) =>
                {
                    if (Locomotive.TrainBrakeController.TCSFullServiceBraking != value)
                    {
                        Locomotive.TrainBrakeController.TCSFullServiceBraking = value;

                        //Debrief Eval
                        if (value && Locomotive.IsPlayerTrain && !ldbfevalfullbrakeabove16kmh && Math.Abs(Locomotive.SpeedMpS) > 4.44444)
                        {
                            var train = Simulator.PlayerLocomotive.Train;//Debrief Eval
                            DbfevalFullBrakeAbove16kmh++;
                            ldbfevalfullbrakeabove16kmh = true;
                            train.DbfEvalValueChanged = true;//Debrief eval
                        }
                        if (!value)
                            ldbfevalfullbrakeabove16kmh = false;
                    }
                };
                Script.SetEmergencyBrake = (value) =>
                {
                    if (Locomotive.TrainBrakeController.TCSEmergencyBraking != value)
                        Locomotive.TrainBrakeController.TCSEmergencyBraking = value;
                };
                Script.SetFullDynamicBrake = (value) => FullDynamicBrakingOrder = value;
                Script.SetThrottleController = (value) => Locomotive.ThrottleController.SetValue(value);
                Script.SetDynamicBrakeController = (value) =>
                {
                if (Locomotive.DynamicBrakeController == null) return;
                Locomotive.DynamicBrakeController.SetValue(value);
                };
                Script.SetPantographsDown = () =>
                {
                    if (Locomotive.Pantographs.State == PantographState.Up)
                    {
                        Locomotive.LocomotivePowerSupply.HandleEventFromTcs(PowerSupplyEvent.LowerPantograph);
                    }
                };
                Script.SetPantographUp = (pantoID) =>
                {
                    if (pantoID < Pantographs.MinPantoID || pantoID > Pantographs.MaxPantoID)
                    {
                        Trace.TraceError($"TCS script used bad pantograph ID {pantoID}");
                        return;
                    }
                    Locomotive.LocomotivePowerSupply.HandleEventFromTcs(PowerSupplyEvent.RaisePantograph, pantoID);
                };               
                Script.SetPantographDown = (pantoID) =>
                {
                    if (pantoID < Pantographs.MinPantoID || pantoID > Pantographs.MaxPantoID)
                    {
                        Trace.TraceError($"TCS script used bad pantograph ID {pantoID}");
                        return;
                    }
                    Locomotive.LocomotivePowerSupply.HandleEventFromTcs(PowerSupplyEvent.LowerPantograph, pantoID);
                };
                Script.SetPowerAuthorization = (value) => PowerAuthorization = value;
                Script.SetCircuitBreakerClosingOrder = (value) => CircuitBreakerClosingOrder = value;
                Script.SetCircuitBreakerOpeningOrder = (value) => CircuitBreakerOpeningOrder = value;
                Script.SetTractionAuthorization = (value) => TractionAuthorization = value;
                Script.SetDynamicBrakingAuthorization = (value) => DynamicBrakingAuthorization = value;
                Script.SetMaxThrottlePercent = (value) =>
                {
                    if (value >= 0 && value <= 100f)
                    {
                        MaxThrottlePercent = value;
                    }
                };
                Script.SetVigilanceAlarm = (value) => Locomotive.SignalEvent(value ? Event.VigilanceAlarmOn : Event.VigilanceAlarmOff);
                Script.SetHorn = (value) => Locomotive.TCSHorn = value;
                Script.SetDoors = (side, open) => Locomotive.Train.SetDoors(Locomotive.Flipped ^ Locomotive.GetCabFlipped() ? Doors.FlippedDoorSide(side) : side, open);
                Script.LockDoors = (side, lck) => Locomotive.Train.LockDoors(Locomotive.Flipped ^ Locomotive.GetCabFlipped() ? Doors.FlippedDoorSide(side) : side, lck);

                Script.TriggerSoundAlert1 = () => this.SignalEvent(Event.TrainControlSystemAlert1, Script);
                Script.TriggerSoundAlert2 = () => this.SignalEvent(Event.TrainControlSystemAlert2, Script);
                Script.TriggerSoundInfo1 = () => this.SignalEvent(Event.TrainControlSystemInfo1, Script);
                Script.TriggerSoundInfo2 = () => this.SignalEvent(Event.TrainControlSystemInfo2, Script);
                Script.TriggerSoundPenalty1 = () => this.SignalEvent(Event.TrainControlSystemPenalty1, Script);
                Script.TriggerSoundPenalty2 = () => this.SignalEvent(Event.TrainControlSystemPenalty2, Script);
                Script.TriggerSoundWarning1 = () => this.SignalEvent(Event.TrainControlSystemWarning1, Script);
                Script.TriggerSoundWarning2 = () => this.SignalEvent(Event.TrainControlSystemWarning2, Script);
                Script.TriggerSoundSystemActivate = () => this.SignalEvent(Event.TrainControlSystemActivate, Script);
                Script.TriggerSoundSystemDeactivate = () => this.SignalEvent(Event.TrainControlSystemDeactivate, Script);
                Script.TriggerGenericSound = (value) => this.SignalEvent(value, Script);
                Script.SetVigilanceAlarmDisplay = (value) => this.VigilanceAlarm = value;
                Script.SetVigilanceEmergencyDisplay = (value) => this.VigilanceEmergency = value;
                Script.SetOverspeedWarningDisplay = (value) => this.OverspeedWarning = value;
                Script.SetPenaltyApplicationDisplay = (value) => this.PenaltyApplication = value;
                Script.SetMonitoringStatus = (value) =>
                {
                    switch (value)
                    {
                        case MonitoringStatus.Normal:
                        case MonitoringStatus.Indication:
                            ETCSStatus.CurrentMonitor = Monitor.CeilingSpeed;
                            ETCSStatus.CurrentSupervisionStatus = SupervisionStatus.Normal;
                            break;
                        case MonitoringStatus.Overspeed:
                            ETCSStatus.CurrentMonitor = Monitor.TargetSpeed;
                            ETCSStatus.CurrentSupervisionStatus = SupervisionStatus.Indication;
                            break;
                        case MonitoringStatus.Warning:
                            ETCSStatus.CurrentSupervisionStatus = SupervisionStatus.Overspeed;
                            break;
                        case MonitoringStatus.Intervention:
                            ETCSStatus.CurrentSupervisionStatus = SupervisionStatus.Intervention;
                            break;
                    }
                };
                Script.SetCurrentSpeedLimitMpS = (value) =>
                {
                    this.CurrentSpeedLimitMpS = value;
                    ETCSStatus.AllowedSpeedMpS = value;
                };
                Script.SetNextSpeedLimitMpS = (value) => {
                    this.NextSpeedLimitMpS = value;
                    ETCSStatus.TargetSpeedMpS = value;
                };
                Script.SetInterventionSpeedLimitMpS = (value) => Script.ETCSStatus.InterventionSpeedMpS = value;
                Script.SetNextSignalAspect = (value) => this.CabSignalAspect = (TrackMonitorSignalAspect)value;
                Script.SetCabDisplayControl = (arg1, arg2) => CabDisplayControls[arg1] = arg2;
                Script.SetCustomizedTCSControlString = (value) =>
                {
                    if (NextCabviewControlNameToEdit == 0)
                    {
                        Trace.TraceWarning("SetCustomizedTCSControlString is deprecated. Please use SetCustomizedCabviewControlName.");
                    }

                    CustomizedCabviewControlNames[NextCabviewControlNameToEdit] = value;

                    NextCabviewControlNameToEdit++;
                };
                Script.SetCustomizedCabviewControlName = (id, value) =>
                {
                    if (id >= 0)
                    {
                        CustomizedCabviewControlNames[id] = value;
                    }
                };
                Script.RequestToggleManualMode = () => 
                {
                    if (Locomotive.Train.ControlMode == Train.TRAIN_CONTROL.OUT_OF_CONTROL && Locomotive.Train.ControlModeBeforeOutOfControl == Train.TRAIN_CONTROL.EXPLORER)
                    {
                        Trace.TraceWarning("RequestToggleManualMode() is deprecated for explorer mode. Please use ResetOutOfControlMode() instead");
                        Locomotive.Train.ManualResetOutOfControlMode();
                    }
                    else Locomotive.Train.RequestToggleManualMode();
                };
                Script.ResetOutOfControlMode = () => Locomotive.Train.ManualResetOutOfControlMode();

                // TrainControlSystem INI configuration file
                Script.GetBoolParameter = (arg1, arg2, arg3) => LoadParameter<bool>(arg1, arg2, arg3);
                Script.GetIntParameter = (arg1, arg2, arg3) => LoadParameter<int>(arg1, arg2, arg3);
                Script.GetFloatParameter = (arg1, arg2, arg3) => LoadParameter<float>(arg1, arg2, arg3);
                Script.GetStringParameter = (arg1, arg2, arg3) => LoadParameter<string>(arg1, arg2, arg3);

                Script.AttachToHost(this);
                Script.Initialize();
                Activated = true;
            }
        }

        public void InitializeMoving()
        {
            Script?.InitializeMoving();
        }

        private Aspect NextNormalSignalDistanceHeadsAspect()
        {
            var signal = Locomotive.Train.NextSignalObject[Locomotive.Train.MUDirection == Direction.Reverse ? 1 : 0];
            Aspect distanceSignalAspect = Aspect.None;
            if (signal != null)
            {
                foreach (var signalHead in signal.SignalHeads)
                {
                    if (signalHead.signalType.Function.MstsFunction == MstsSignalFunction.DISTANCE)
                    {
                        return distanceSignalAspect = (Aspect)Locomotive.Train.signalRef.TranslateToTCSAspect(signal.this_sig_lr(MstsSignalFunction.DISTANCE));
                    }
                }
            }
            return distanceSignalAspect;
        }

        private bool DoesNextNormalSignalHaveTwoAspects()
            // ...and the two aspects of each head are STOP and ( CLEAR_2 or CLEAR_1 or RESTRICTING)
        {
            var signal = Locomotive.Train.NextSignalObject[Locomotive.Train.MUDirection == Direction.Reverse ? 1 : 0];
            if (signal != null)
            {
                if (signal.SignalHeads[0].signalType.Aspects.Count > 2) return false;
                else
                {
                    foreach (var signalHead in signal.SignalHeads)
                    {
                        if (signalHead.signalType.Function.MstsFunction != MstsSignalFunction.DISTANCE &&
                            signalHead.signalType.Aspects.Count == 2 &&
                            (int)(signalHead.signalType.Aspects[0].Aspect) == 0 &&
                                ((int)(signalHead.signalType.Aspects[1].Aspect) == 7 ||
                                (int)(signalHead.signalType.Aspects[1].Aspect) == 6 ||
                                (int)(signalHead.signalType.Aspects[1].Aspect) == 2)) continue;
                        else return false;
                    }
                    return true;
                }
            }
            return true;
        }

        T NextGenericSignalItem<T>(int itemSequenceIndex, ref T retval, float maxDistanceM, Train.TrainObjectItem.TRAINOBJECTTYPE type, string signalTypeName = "UNKNOWN")
        {
            var item = NextGenericSignalFeatures(signalTypeName, itemSequenceIndex, maxDistanceM, type);
            MainHeadSignalTypeName = item.MainHeadSignalTypeName;
            ItemAspect = item.Aspect;
            ItemDistance = item.DistanceM;
            ItemSpeedLimit = item.SpeedLimitMpS;
            return retval;
        }

        SignalFeatures NextGenericSignalFeatures(string signalFunctionTypeName, int itemSequenceIndex, float maxDistanceM, Train.TrainObjectItem.TRAINOBJECTTYPE type)
        {
            var mainHeadSignalTypeName = "";
            var signalTypeName = "";
            var aspect = Aspect.None;
            var drawStateName = "";
            var distanceM = float.MaxValue;
            var speedLimitMpS = -1f;
            var altitudeM = float.MinValue;
            var textAspect = "";

            int dir = Locomotive.Train.MUDirection == Direction.Reverse ? 1 : 0;

            if (Locomotive.Train.ValidRoute[dir] == null || dir == 1 && Locomotive.Train.PresentPosition[dir].TCSectionIndex < 0)
                goto Exit;

            int index = dir == 0 ? Locomotive.Train.PresentPosition[dir].RouteListIndex :
                Locomotive.Train.ValidRoute[dir].GetRouteIndex(Locomotive.Train.PresentPosition[dir].TCSectionIndex, 0);

            if (!Locomotive.Train.signalRef.SignalFunctions.ContainsKey(signalFunctionTypeName))
            {
                distanceM = -1;
                goto Exit;
            }
            SignalFunction function = Locomotive.Train.signalRef.SignalFunctions[signalFunctionTypeName];

            if (index < 0)
                goto Exit;

            switch (type)
            {
                case Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL:
                case Train.TrainObjectItem.TRAINOBJECTTYPE.SPEED_SIGNAL:
                    var playerTrainSignalList = Locomotive.Train.PlayerTrainSignals[dir][function];
                    if (itemSequenceIndex > playerTrainSignalList.Count - 1)
                        goto Exit; // no n-th signal available
                    var trainSignal = playerTrainSignalList[itemSequenceIndex];
                    if (trainSignal.DistanceToTrainM > maxDistanceM)
                        goto Exit; // the requested signal is too distant

                    // All OK, we can retrieve the data for the required signal;
                    distanceM = trainSignal.DistanceToTrainM;
                    mainHeadSignalTypeName = trainSignal.SignalObject.SignalHeads[0].SignalTypeName;
                    if (signalFunctionTypeName == "NORMAL")
                    {
                        aspect = (Aspect)trainSignal.SignalState;
                        speedLimitMpS = trainSignal.AllowedSpeedMpS;
                        altitudeM = trainSignal.SignalObject.tdbtraveller.Y;
                    }
                    else
                    {
                        aspect = (Aspect)Locomotive.Train.signalRef.TranslateToTCSAspect(trainSignal.SignalObject.this_sig_lr(function));
                    }

                    var functionHead = default(SignalHead);
                    foreach (var head in trainSignal.SignalObject.SignalHeads)
                        if (head.Function == function)
                            functionHead = head;
                    if (functionHead == null)
                        goto Exit;
                    signalTypeName = functionHead.SignalTypeName;
                    if (functionHead?.signalType?.DrawStates != null)
                    {
                        foreach (var key in functionHead.signalType.DrawStates.Keys)
                        {
                            if (functionHead.signalType.DrawStates[key].Index == functionHead.draw_state)
                            {
                                drawStateName = functionHead.signalType.DrawStates[key].Name;
                                break;
                            }
                        }
                    }
                    textAspect = functionHead?.TextSignalAspect ?? "";
                    break;
                case Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST:
                    var j = 0;
                    for (var i = 0; i < Locomotive.Train.PlayerTrainSpeedposts[dir].Count; i++)
                    {
                        if (Locomotive.Train.PlayerTrainSpeedposts[dir][i].IsWarning)
                            continue;
                        if (itemSequenceIndex == j++)
                        {
                            var trainSpeedpost = Locomotive.Train.PlayerTrainSpeedposts[dir][i];
                            if (trainSpeedpost.DistanceToTrainM <= maxDistanceM)
                            {
                                distanceM = trainSpeedpost.DistanceToTrainM;
                                speedLimitMpS = trainSpeedpost.AllowedSpeedMpS;
                            }
                            break;
                        }
                    }
                    break;
            }

        Exit:
            return new SignalFeatures(mainHeadSignalTypeName: mainHeadSignalTypeName, signalTypeName: signalTypeName, aspect: aspect, drawStateName: drawStateName, distanceM: distanceM, speedLimitMpS: speedLimitMpS,
                altitudeM: altitudeM, textAspect: textAspect);
        }

        SpeedPostFeatures NextSpeedPostFeatures(int itemSequenceIndex, float maxDistanceM)
        {
            var speedPostTypeName = "";
            var isWarning = false;
            var distanceM = float.MaxValue;
            var speedLimitMpS = -1f;
            var altitudeM = float.MinValue;

            int dir = Locomotive.Train.MUDirection == Direction.Reverse ? 1 : 0;

            if (Locomotive.Train.ValidRoute[dir] == null || dir == 1 && Locomotive.Train.PresentPosition[dir].TCSectionIndex < 0)
                goto Exit;

            int index = dir == 0 ? Locomotive.Train.PresentPosition[dir].RouteListIndex :
                Locomotive.Train.ValidRoute[dir].GetRouteIndex(Locomotive.Train.PresentPosition[dir].TCSectionIndex, 0);
            if (index < 0)
                goto Exit;

            var playerTrainSpeedpostList = Locomotive.Train.PlayerTrainSpeedposts[dir];
            if (itemSequenceIndex > playerTrainSpeedpostList.Count - 1)
                goto Exit; // no n-th speedpost available
            var trainSpeedpost = playerTrainSpeedpostList[itemSequenceIndex];
            if (trainSpeedpost.DistanceToTrainM > maxDistanceM)
                goto Exit; // the requested speedpost is too distant

            // All OK, we can retrieve the data for the required speedpost;
            speedPostTypeName = Path.GetFileNameWithoutExtension(trainSpeedpost.SignalObject.SpeedPostWorldObject?.SFileName);
            isWarning = trainSpeedpost.IsWarning;
            distanceM = trainSpeedpost.DistanceToTrainM;
            speedLimitMpS = trainSpeedpost.AllowedSpeedMpS;
            altitudeM = trainSpeedpost.SignalObject.tdbtraveller.Y;

        Exit:
            return new SpeedPostFeatures(speedPostTypeName: speedPostTypeName, isWarning: isWarning, distanceM: distanceM, speedLimitMpS: speedLimitMpS,
                altitudeM: altitudeM);
        }

        private bool DoesNextNormalSignalHaveRepeaterHead()
        {
            var signal = Locomotive.Train.NextSignalObject[Locomotive.Train.MUDirection == Direction.Reverse ? 1 : 0];
            if (signal != null)
            {
                foreach (var signalHead in signal.SignalHeads)
                {
                    if (signalHead.signalType.Function.MstsFunction == MstsSignalFunction.REPEATER) return true;
                }
                return false;
            }
            return false;
        }

        private bool DoesStartFromTerminalStation()
        {
            var tempTraveller = new Traveller(Locomotive.Train.RearTDBTraveller);
            tempTraveller.ReverseDirection();
            return tempTraveller.NextTrackNode() && tempTraveller.IsEnd;
        }

        public void SignalEvent(Event evt, TrainControlSystem script)
        {
            try
            { 
                foreach (var eventHandler in Locomotive.EventHandlers)
                    eventHandler.HandleEvent(evt, script);
            }
            catch (Exception error)
            {
                Trace.TraceInformation("Sound event skipped due to thread safety problem " + error.Message);
            }
        }

        private static float SpeedCurve(float targetDistanceM, float targetSpeedMpS, float slope, float delayS, float decelerationMpS2)
        {
            if (targetSpeedMpS < 0)
                targetSpeedMpS = 0;

            decelerationMpS2 -= GravityMpS2 * slope;

            float squareSpeedComponent = targetSpeedMpS * targetSpeedMpS
                + (delayS * delayS) * decelerationMpS2 * decelerationMpS2
                + 2f * targetDistanceM * decelerationMpS2;

            float speedComponent = delayS * decelerationMpS2;

            return (float)Math.Sqrt(squareSpeedComponent) - speedComponent;
        }

        private static float DistanceCurve(float currentSpeedMpS, float targetSpeedMpS, float slope, float delayS, float decelerationMpS2)
        {
            if (targetSpeedMpS < 0)
                targetSpeedMpS = 0;

            float brakingDistanceM = (currentSpeedMpS * currentSpeedMpS - targetSpeedMpS * targetSpeedMpS)
                / (2 * (decelerationMpS2 - GravityMpS2 * slope));

            float delayDistanceM = delayS * currentSpeedMpS;

            return brakingDistanceM + delayDistanceM;
        }

        private static float Deceleration(float currentSpeedMpS, float targetSpeedMpS, float distanceM)
        {
            return (currentSpeedMpS - targetSpeedMpS) * (currentSpeedMpS + targetSpeedMpS) / (2 * distanceM);
        }

        public void Update(float elapsedClockSeconds)
        {
            switch (Locomotive.Train.TrainType)
            {
                case Train.TRAINTYPE.STATIC:
                case Train.TRAINTYPE.AI:
                case Train.TRAINTYPE.AI_NOTSTARTED:
                case Train.TRAINTYPE.AI_AUTOGENERATE:
                case Train.TRAINTYPE.REMOTE:
                case Train.TRAINTYPE.AI_INCORPORATED:
                    if (Locomotive.Train.Autopilot && Locomotive == Simulator.PlayerLocomotive)
                        Locomotive.Train.UpdatePlayerTrainData();
                    DisableRestrictions();
                    break;

                default:
                    if (Locomotive == Simulator.PlayerLocomotive || Locomotive.Train.PlayerTrainSignals == null)
                        Locomotive.Train.UpdatePlayerTrainData();
                    if (Script == null)
                    {
                        DisableRestrictions();
                    }
                    else
                    {
                        ClearParams();
                        Script.Update();
                    }
                    break;
            }
        }

        public void DisableRestrictions()
        {
            PowerAuthorization = true;
            if (Locomotive.TrainBrakeController != null)
            {
                Locomotive.TrainBrakeController.TCSFullServiceBraking = false;
                Locomotive.TrainBrakeController.TCSEmergencyBraking = false;
            }
        }

        public void ClearParams()
        {

        }

        public void AlerterPressed(bool pressed)
        {
            AlerterButtonPressed = pressed;
            HandleEvent(pressed ? TCSEvent.AlerterPressed : TCSEvent.AlerterReleased);
        }

        public void AlerterReset()
        {
            HandleEvent(TCSEvent.AlerterReset);
        }

        public void HandleEvent(TCSEvent evt)
        {
            HandleEvent(evt, String.Empty);
        }

        public void HandleEvent(TCSEvent evt, string message)
        {
            Script?.HandleEvent(evt, message);

            switch (evt)
            {
                case TCSEvent.EmergencyBrakingRequestedBySimulator:
                    SimulatorEmergencyBraking = true;
                    break;

                case TCSEvent.EmergencyBrakingReleasedBySimulator:
                    SimulatorEmergencyBraking = false;
                    break;
            }
        }

        public void HandleEvent(TCSEvent evt, int eventIndex)
        {
            var message = eventIndex.ToString();
            HandleEvent(evt, message);
        }

        public void HandleEvent(PowerSupplyEvent evt, string message="")
        {
            Script?.HandleEvent(evt, message);
        }

        private T LoadParameter<T>(string sectionName, string keyName, T defaultValue)
        {
            string buffer;
            int length;

            if (File.Exists(TrainParametersFileName))
            {
                buffer = new string('\0', 256);
                length = NativeMethods.GetPrivateProfileString(sectionName, keyName, null, buffer, buffer.Length, TrainParametersFileName);

                if (length > 0)
                {
                    buffer = buffer.Trim('\0').Trim();
                    return (T)Convert.ChangeType(buffer, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            if (File.Exists(ParametersFileName))
            {
                buffer = new string('\0', 256);
                length = NativeMethods.GetPrivateProfileString(sectionName, keyName, null, buffer, buffer.Length, ParametersFileName);

                if (length > 0)
                {
                    buffer = buffer.Trim('\0').Trim();
                    return (T)Convert.ChangeType(buffer, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            return defaultValue;
        }

        // Converts the generic string (e.g. ORTS_TCS5) shown when browsing with the mouse on a TCS control
        // to a customized string defined in the script
        public string GetDisplayString(int commandIndex)
        {
            if (CustomizedCabviewControlNames.TryGetValue(commandIndex - 1, out string name)) return name;
            return "ORTS_TCS"+commandIndex;
        }

        public void Save(BinaryWriter outf)
        {
            if (!string.IsNullOrEmpty(ScriptName))
                Script.Save(outf);
        }

        public void Restore(BinaryReader inf)
        {
            if (!string.IsNullOrEmpty(ScriptName))
                Script.Restore(inf);
        }
    }

    public class MSTSTrainControlSystem : TrainControlSystem
    {
        public enum MonitorState
        {
            Disabled,
            StandBy,
            Alarm,
            Emergency
        };

        public bool ResetButtonPressed { get; private set; }

        public bool VigilanceSystemEnabled
        {
            get
            {
                bool enabled = true;

                enabled &= IsAlerterEnabled();

                if (VigilanceMonitor != null)
                {
                    if (VigilanceMonitor.ResetOnDirectionNeutral)
                    {
                        enabled &= CurrentDirection() != Direction.N;
                    }

                    if (VigilanceMonitor.ResetOnZeroSpeed)
                    {
                        enabled &= SpeedMpS() >= 0.1f;
                    }
                }

                return enabled;
            }
        }

        public bool VigilanceReset
        {
            get
            {
                bool vigilanceReset = true;

                if (VigilanceMonitor != null)
                {
                    if (VigilanceMonitor.ResetOnDirectionNeutral)
                    {
                        vigilanceReset &= CurrentDirection() == Direction.N;
                    }

                    if (VigilanceMonitor.ResetOnZeroSpeed)
                    {
                        vigilanceReset &= SpeedMpS() < 0.1f;
                    }

                    if (VigilanceMonitor.ResetOnResetButton)
                    {
                        vigilanceReset &= ResetButtonPressed;
                    }
                }

                return vigilanceReset;
            }
        }

        public bool SpeedControlSystemEnabled
        {
            get
            {
                bool enabled = true;

                enabled &= IsSpeedControlEnabled();

                return enabled;
            }
        }

        public bool Overspeed
        {
            get
            {
                bool overspeed = false;

                if (OverspeedMonitor != null)
                {
                    if (OverspeedMonitor.TriggerOnOverspeedMpS > 0)
                    {
                        overspeed |= SpeedMpS() > OverspeedMonitor.TriggerOnOverspeedMpS;
                    }

                    if (OverspeedMonitor.CriticalLevelMpS > 0)
                    {
                        overspeed |= SpeedMpS() > OverspeedMonitor.CriticalLevelMpS;
                    }

                    if (OverspeedMonitor.TriggerOnTrackOverspeed)
                    {
                        overspeed |= SpeedMpS() > CurrentSpeedLimitMpS + OverspeedMonitor.TriggerOnTrackOverspeedMarginMpS;
                    }
                }

                return overspeed;
            }
        }

        public bool OverspeedReset
        {
            get
            {
                bool overspeedReset = true;

                if (OverspeedMonitor != null)
                {
                    if (OverspeedMonitor.ResetOnDirectionNeutral)
                    {
                        overspeedReset &= CurrentDirection() == Direction.N;
                    }

                    if (OverspeedMonitor.ResetOnZeroSpeed)
                    {
                        overspeedReset &= SpeedMpS() < 0.1f;
                    }

                    if (OverspeedMonitor.ResetOnResetButton)
                    {
                        overspeedReset &= ResetButtonPressed;
                    }
                }

                return overspeedReset;
            }
        }

        Timer VigilanceAlarmTimer;
        Timer VigilanceEmergencyTimer;
        Timer VigilancePenaltyTimer;
        Timer OverspeedEmergencyTimer;
        Timer OverspeedPenaltyTimer;

        MonitorState VigilanceMonitorState;
        MonitorState OverspeedMonitorState;
        bool ExternalEmergency;

        float VigilanceAlarmTimeoutS;
        float CurrentSpeedLimitMpS;
        float NextSpeedLimitMpS;
       
        MonitoringStatus Status;

        public ScriptedTrainControlSystem.MonitoringDevice VigilanceMonitor;
        public ScriptedTrainControlSystem.MonitoringDevice OverspeedMonitor;
        public ScriptedTrainControlSystem.MonitoringDevice EmergencyStopMonitor;
        public ScriptedTrainControlSystem.MonitoringDevice AWSMonitor;
        public bool EmergencyCausesThrottleDown;
        public bool EmergencyEngagesHorn;

        public MSTSTrainControlSystem() { }

        public override void Initialize()
        {
            VigilanceAlarmTimer = new Timer(this);
            VigilanceEmergencyTimer = new Timer(this);
            VigilancePenaltyTimer = new Timer(this);
            OverspeedEmergencyTimer = new Timer(this);
            OverspeedPenaltyTimer = new Timer(this);

            if (VigilanceMonitor != null)
            {
                if (VigilanceMonitor.MonitorTimeS > VigilanceMonitor.AlarmTimeS)
                    VigilanceAlarmTimeoutS = VigilanceMonitor.MonitorTimeS - VigilanceMonitor.AlarmTimeS;
                VigilanceAlarmTimer.Setup(VigilanceMonitor.AlarmTimeS);
                VigilanceEmergencyTimer.Setup(VigilanceAlarmTimeoutS);
                VigilancePenaltyTimer.Setup(VigilanceMonitor.PenaltyTimeS);
                VigilanceAlarmTimer.Start();
            }
            if (OverspeedMonitor != null)
            {
                OverspeedEmergencyTimer.Setup(Math.Max(OverspeedMonitor.AlarmTimeS, OverspeedMonitor.AlarmTimeBeforeOverspeedS));
                OverspeedPenaltyTimer.Setup(OverspeedMonitor.PenaltyTimeS);
            }

            ETCSStatus.DMIActive = ETCSStatus.PlanningAreaShown = true;

            Activated = true;
        }
        public override void Update()
        {
            UpdateInputs();

            if (IsTrainControlEnabled())
            {
                if (VigilanceMonitor != null)
                    UpdateVigilance();
                if (OverspeedMonitor != null)
                    UpdateSpeedControl();

                bool EmergencyBrake = false;
                bool FullBrake = false;
                bool PowerCut = false;

                if (VigilanceMonitor != null)
                {
                    if (VigilanceMonitor.AppliesEmergencyBrake)
                        EmergencyBrake |= (VigilanceMonitorState == MonitorState.Emergency);
                    else if (VigilanceMonitor.AppliesFullBrake)
                        FullBrake |= (VigilanceMonitorState == MonitorState.Emergency);

                    if (VigilanceMonitor.EmergencyCutsPower)
                        PowerCut |= (VigilanceMonitorState == MonitorState.Emergency);
                }

                if (OverspeedMonitor != null)
                {
                    if (OverspeedMonitor.AppliesEmergencyBrake)
                        EmergencyBrake |= (OverspeedMonitorState == MonitorState.Emergency);
                    else if (OverspeedMonitor.AppliesFullBrake)
                        FullBrake |= (OverspeedMonitorState == MonitorState.Emergency);

                    if (OverspeedMonitor.EmergencyCutsPower)
                        PowerCut |= (OverspeedMonitorState == MonitorState.Emergency);
                }

                if (EmergencyStopMonitor != null)
                {
                    if (EmergencyStopMonitor.AppliesEmergencyBrake)
                        EmergencyBrake |= ExternalEmergency;
                    else if (EmergencyStopMonitor.AppliesFullBrake)
                        FullBrake |= ExternalEmergency;

                    if (EmergencyStopMonitor.EmergencyCutsPower)
                        PowerCut |= ExternalEmergency;
                }

                SetTractionAuthorization(!DoesBrakeCutPower() || LocomotiveBrakeCylinderPressureBar() < BrakeCutsPowerAtBrakeCylinderPressureBar());
                SetDynamicBrakingAuthorization(!EmergencyBrakeCutsDynamicBrake || !IsBrakeEmergency());

                SetEmergencyBrake(EmergencyBrake);
                SetFullBrake(FullBrake);
                SetPowerAuthorization(!PowerCut);

                if (EmergencyCausesThrottleDown && (IsBrakeEmergency() || IsBrakeFullService()))
                    SetThrottleController(0f);

                if (EmergencyEngagesHorn)
                    SetHorn(IsBrakeEmergency() || IsBrakeFullService());

                SetPenaltyApplicationDisplay(IsBrakeEmergency() || IsBrakeFullService());

                UpdateMonitoringStatus();
                UpdateETCSPlanning();
            }
        }

        public void UpdateInputs()
        {
            SetNextSignalAspect(NextSignalAspect(0));

            CurrentSpeedLimitMpS = CurrentSignalSpeedLimitMpS();
            if (CurrentSpeedLimitMpS < 0 || CurrentSpeedLimitMpS > TrainSpeedLimitMpS())
                CurrentSpeedLimitMpS = TrainSpeedLimitMpS();

            // TODO: NextSignalSpeedLimitMpS(0) should return 0 if the signal is at stop; cause seems to be updateSpeedInfo() within Train.cs
            NextSpeedLimitMpS = NextSignalAspect(0) != Aspect.Stop ? (NextSignalSpeedLimitMpS(0) > 0 && NextSignalSpeedLimitMpS(0) < TrainSpeedLimitMpS() ? NextSignalSpeedLimitMpS(0) : TrainSpeedLimitMpS() ) : 0;

            SetCurrentSpeedLimitMpS(CurrentSpeedLimitMpS);
            SetNextSpeedLimitMpS(NextSpeedLimitMpS);
        }

        private void UpdateMonitoringStatus()
        {
            if (SpeedMpS() > CurrentSpeedLimitMpS)
            {
                if (OverspeedMonitor != null && (OverspeedMonitor.AppliesEmergencyBrake || OverspeedMonitor.AppliesFullBrake))
                    Status = MonitoringStatus.Intervention;
                else
                    Status = MonitoringStatus.Warning;
            }
            else if (NextSpeedLimitMpS < CurrentSpeedLimitMpS && SpeedMpS() > NextSpeedLimitMpS)
            {
                if (Deceleration(SpeedMpS(), NextSpeedLimitMpS, NextSignalDistanceM(0)) > 0.7f)
                    Status = MonitoringStatus.Overspeed;
                else
                    Status = MonitoringStatus.Indication;
            }
            else
                Status = MonitoringStatus.Normal;
            SetMonitoringStatus(Status);
        }

        // Provide basic functionality for ETCS DMI planning area
        private void UpdateETCSPlanning()
        {
            float maxDistanceAheadM = 0;
            ETCSStatus.SpeedTargets.Clear();
            ETCSStatus.SpeedTargets.Add(new PlanningTarget(0, CurrentSpeedLimitMpS));
            for (int i = 0; i < 5; i++)
            {
                maxDistanceAheadM = NextSignalDistanceM(i);
                if (NextSignalAspect(i) == Aspect.Stop || NextSignalAspect(i) == Aspect.None) break;
                float speedLimMpS = NextSignalSpeedLimitMpS(i); 
                if (speedLimMpS >= 0) ETCSStatus.SpeedTargets.Add(new PlanningTarget(maxDistanceAheadM, speedLimMpS));
            }
            float prevDist = 0;
            float prevSpeed = 0;
            for (int i = 0; i < 10; i++)
            {
                float distanceM = NextPostDistanceM(i);
                if (distanceM >= maxDistanceAheadM) break;
                float speed = NextPostSpeedLimitMpS(i);
                if (speed == prevSpeed || distanceM - prevDist < 10) continue;
                ETCSStatus.SpeedTargets.Add(new PlanningTarget(distanceM, speed));
                prevDist = distanceM;
                prevSpeed = speed;
            }
            ETCSStatus.SpeedTargets.Sort((x, y) => x.DistanceToTrainM.CompareTo(y.DistanceToTrainM));
            ETCSStatus.SpeedTargets.Add(new PlanningTarget(maxDistanceAheadM, 0)); 
            ETCSStatus.GradientProfile.Clear();
            ETCSStatus.GradientProfile.Add(new GradientProfileElement(0, (int)(CurrentGradientPercent() * 10)));
            ETCSStatus.GradientProfile.Add(new GradientProfileElement(maxDistanceAheadM, 0)); // End of profile
        }

        public override void HandleEvent(TCSEvent evt, string message)
        {
            switch(evt)
            {
                case TCSEvent.AlerterPressed:
                case TCSEvent.AlerterReleased:
                case TCSEvent.AlerterReset:
                    if (Activated)
                    {
                        switch (VigilanceMonitorState)
                        {
                            // case VigilanceState.Disabled: do nothing

                            case MonitorState.StandBy:
                                VigilanceAlarmTimer.Stop();
                                break;

                            case MonitorState.Alarm:
                                VigilanceEmergencyTimer.Stop();
                                VigilanceMonitorState = MonitorState.StandBy;
                                break;

                            // case VigilanceState.Emergency: do nothing
                        }
                    }
                    break;
            }

            switch (evt)
            {
                case TCSEvent.AlerterPressed:
                    ResetButtonPressed = true;
                    break;

                case TCSEvent.AlerterReleased:
                    ResetButtonPressed = false;
                    break;

                case TCSEvent.EmergencyBrakingRequestedBySimulator:
                    ExternalEmergency = true;
                    break;

                case TCSEvent.EmergencyBrakingReleasedBySimulator:
                    ExternalEmergency = false;
                    break;
            }
        }

        void UpdateVigilance()
        {
            switch (VigilanceMonitorState)
            {
                case MonitorState.Disabled:
                    if (VigilanceSystemEnabled)
                    {
                        VigilanceMonitorState = MonitorState.StandBy;
                    }
                    break;

                case MonitorState.StandBy:
                    if (!VigilanceSystemEnabled)
                    {
                        VigilanceAlarmTimer.Stop();
                        VigilanceMonitorState = MonitorState.Disabled;
                    }
                    else
                    {
                        if (!VigilanceAlarmTimer.Started)
                        {
                            VigilanceAlarmTimer.Start();
                        }

                        if (VigilanceAlarmTimer.Triggered)
                        {
                            VigilanceAlarmTimer.Stop();
                            VigilanceMonitorState = MonitorState.Alarm;
                        }
                    }
                    break;

                case MonitorState.Alarm:
                    if (!VigilanceSystemEnabled)
                    {
                        VigilanceEmergencyTimer.Stop();
                        VigilanceMonitorState = MonitorState.Disabled;
                    }
                    else
                    {
                        if (!VigilanceEmergencyTimer.Started)
                        {
                            VigilanceEmergencyTimer.Start();
                        }

                        if (VigilanceEmergencyTimer.Triggered)
                        {
                            VigilanceEmergencyTimer.Stop();
                            VigilanceMonitorState = MonitorState.Emergency;
                        }
                    }
                    break;

                case MonitorState.Emergency:
                    if (!VigilancePenaltyTimer.Started)
                    {
                        VigilancePenaltyTimer.Start();
                    }

                    if (VigilancePenaltyTimer.Triggered && VigilanceReset)
                    {
                        VigilanceEmergencyTimer.Stop();
                        VigilanceMonitorState = (VigilanceSystemEnabled ? MonitorState.StandBy : MonitorState.Disabled);
                    }
                    break;
            }

            if (VigilanceMonitorState >= MonitorState.Alarm)
            {
                if (!AlerterSound())
                {
                    SetVigilanceAlarm(true);
                }
            }
            else
            {
                if (AlerterSound())
                {
                    SetVigilanceAlarm(false);
                }
            }

            SetVigilanceAlarmDisplay(VigilanceMonitorState == MonitorState.Alarm);
            SetVigilanceEmergencyDisplay(VigilanceMonitorState == MonitorState.Emergency);
        }

        void UpdateSpeedControl()
        {
            var interventionSpeedMpS = CurrentSpeedLimitMpS + MpS.FromKpH(5.0f); // Default margin : 5 km/h
            
            if (OverspeedMonitor.TriggerOnTrackOverspeed)
            {
                interventionSpeedMpS = CurrentSpeedLimitMpS + OverspeedMonitor.TriggerOnTrackOverspeedMarginMpS;
            }
            
            SetInterventionSpeedLimitMpS(interventionSpeedMpS);

            switch (OverspeedMonitorState)
            {
                case MonitorState.Disabled:
                    if (SpeedControlSystemEnabled)
                    {
                        OverspeedMonitorState = MonitorState.StandBy;
                    }
                    break;

                case MonitorState.StandBy:
                    if (!SpeedControlSystemEnabled)
                    {
                        OverspeedMonitorState = MonitorState.Disabled;
                    }
                    else
                    {
                        if (Overspeed)
                        {
                            OverspeedMonitorState = MonitorState.Alarm;
                        }
                    }
                    break;

                case MonitorState.Alarm:
                    if (!SpeedControlSystemEnabled)
                    {
                        OverspeedMonitorState = MonitorState.Disabled;
                    }
                    else
                    {
                        if (!OverspeedEmergencyTimer.Started)
                        {
                            OverspeedEmergencyTimer.Start();
                        }

                        if (!Overspeed)
                        {
                            OverspeedEmergencyTimer.Stop();
                            OverspeedMonitorState = MonitorState.StandBy;
                        }
                        else if (OverspeedEmergencyTimer.Triggered)
                        {
                            OverspeedEmergencyTimer.Stop();
                            OverspeedMonitorState = MonitorState.Emergency;
                        }
                    }
                    break;

                case MonitorState.Emergency:
                    if (!OverspeedPenaltyTimer.Started)
                    {
                        OverspeedPenaltyTimer.Start();
                    }

                    if (OverspeedPenaltyTimer.Triggered && OverspeedReset)
                    {
                        OverspeedPenaltyTimer.Stop();
                        OverspeedMonitorState = MonitorState.StandBy;
                    }
                    break;
            }

            SetOverspeedWarningDisplay(OverspeedMonitorState >= MonitorState.Alarm);
        }
    }
}
