// COPYRIGHT 2013, 2014 by the Open Rails project.
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

using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using ORTS.Common;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Orts.Simulation.RollingStocks.SubSystems
{
    public class ScriptedTrainControlSystem
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

        public bool VigilanceAlarm { get; set; }
        public bool VigilanceEmergency { get; set; }
        public bool OverspeedWarning { get; set; }
        public bool PenaltyApplication { get; set; }
        public float CurrentSpeedLimitMpS { get; set; }
        public float NextSpeedLimitMpS { get; set; }
        public float InterventionSpeedLimitMpS { get; set; }
        public TrackMonitorSignalAspect CabSignalAspect { get; set; }
        public MonitoringStatus MonitoringStatus { get; set; }

        Train.TrainInfo TrainInfo = new Train.TrainInfo();

        readonly MSTSLocomotive Locomotive;
        readonly Simulator Simulator;

        List<float> SignalSpeedLimits = new List<float>();
        List<Aspect> SignalAspects = new List<Aspect>();
        List<float> SignalDistances = new List<float>();
        List<float> PostSpeedLimits = new List<float>();
        List<float> PostDistances = new List<float>();

        MonitoringDevice VigilanceMonitor;
        MonitoringDevice OverspeedMonitor;
        MonitoringDevice EmergencyStopMonitor;
        MonitoringDevice AWSMonitor;

        public bool AlerterButtonPressed { get; private set; }
        public bool PowerAuthorization { get; private set; }
        public bool CircuitBreakerClosingOrder { get; private set;  }
        public bool CircuitBreakerOpeningOrder { get; private set; }
        public bool TractionAuthorization { get; private set; }

        string ScriptName;
        string SoundFileName;
        string ParametersFileName;
        TrainControlSystem Script;

        public Dictionary<TrainControlSystem, string> Sounds = new Dictionary<TrainControlSystem, string>();

        const float GravityMpS2 = 9.80665f;

        public ScriptedTrainControlSystem() { }

        public ScriptedTrainControlSystem(MSTSLocomotive locomotive)
        {
            Locomotive = locomotive;
            Simulator = Locomotive.Simulator;

            PowerAuthorization = true;
            CircuitBreakerClosingOrder = false;
            CircuitBreakerOpeningOrder = false;
            TractionAuthorization = true;
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
            if (!Simulator.Settings.DisableTCSScripts && ScriptName != null && ScriptName != "MSTS")
            {
                var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as TrainControlSystem;
            }

            if (ParametersFileName != null)
            {
                ParametersFileName = Path.Combine(Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script"), ParametersFileName);
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
            Script.ClockTime = () => (float)Simulator.ClockTime;
            Script.GameTime = () => (float)Simulator.GameTime;
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
            Script.IsTrainControlEnabled = () => Locomotive == Locomotive.Train.LeadLocomotive && Locomotive.Train.TrainType != Train.TRAINTYPE.AI_PLAYERHOSTING;
            Script.IsAlerterEnabled = () =>
            {
                return Simulator.Settings.Alerter
                    && !(Simulator.Settings.AlerterDisableExternal
                        && !Simulator.PlayerIsInCab
                    );
            };
            Script.IsSpeedControlEnabled = () => Simulator.Settings.SpeedControl;
            Script.AlerterSound = () => Locomotive.AlerterSnd;
            Script.TrainSpeedLimitMpS = () => TrainInfo.allowedSpeedMpS;
            Script.CurrentSignalSpeedLimitMpS = () => Locomotive.Train.allowedMaxSpeedSignalMpS;
            Script.NextSignalSpeedLimitMpS = (value) => NextSignalItem<float>(value, ref SignalSpeedLimits, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL);
            Script.NextSignalAspect = (value) => NextSignalItem<Aspect>(value, ref SignalAspects, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL);
            Script.NextSignalDistanceM = (value) => NextSignalItem<float>(value, ref SignalDistances, Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL);
            Script.CurrentPostSpeedLimitMpS = () => Locomotive.Train.allowedMaxSpeedLimitMpS;
            Script.NextPostSpeedLimitMpS = (value) => NextSignalItem<float>(value, ref PostSpeedLimits, Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST);
            Script.NextPostDistanceM = (value) => NextSignalItem<float>(value, ref PostDistances, Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST);
            Script.TrainLengthM = () => Locomotive.Train != null ? Locomotive.Train.Length : 0f;
            Script.SpeedMpS = () => Math.Abs(Locomotive.SpeedMpS);
            Script.CurrentDirection = () => Locomotive.Direction;
            Script.IsDirectionForward = () => Locomotive.Direction == Direction.Forward;
            Script.IsDirectionNeutral = () => Locomotive.Direction == Direction.N;
            Script.IsDirectionReverse = () => Locomotive.Direction == Direction.Reverse;
            Script.IsBrakeEmergency = () => Locomotive.TrainBrakeController.EmergencyBraking;
            Script.IsBrakeFullService = () => Locomotive.TrainBrakeController.TCSFullServiceBraking;
            Script.PowerAuthorization = () => PowerAuthorization;
            Script.CircuitBreakerClosingOrder = () => CircuitBreakerClosingOrder;
            Script.CircuitBreakerOpeningOrder = () => CircuitBreakerOpeningOrder;
            Script.TractionAuthorization = () => TractionAuthorization;
            Script.BrakePipePressureBar = () => Locomotive.BrakeSystem != null ? Bar.FromPSI(Locomotive.BrakeSystem.BrakeLine1PressurePSI) : float.MaxValue;
            Script.LocomotiveBrakeCylinderPressureBar = () => Locomotive.BrakeSystem != null ? Bar.FromPSI(Locomotive.BrakeSystem.GetCylPressurePSI()) : float.MaxValue;
            Script.DoesBrakeCutPower = () => Locomotive.DoesBrakeCutPower;
            Script.BrakeCutsPowerAtBrakeCylinderPressureBar = () => Bar.FromPSI(Locomotive.BrakeCutsPowerAtBrakeCylinderPressurePSI);

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
            Script.SetThrottleController = (value) => Locomotive.ThrottleController.SetValue(value);
            Script.SetDynamicBrakeController = (value) => Locomotive.DynamicBrakeController.SetValue(value);
            Script.SetPantographsDown = () =>
            {
                if (Locomotive.Pantographs.State == PantographState.Up)
                {
                    Locomotive.Train.SignalEvent(PowerSupplyEvent.LowerPantograph);
                }
            };
            Script.SetPowerAuthorization = (value) => PowerAuthorization = value;
            Script.SetCircuitBreakerClosingOrder = (value) => CircuitBreakerClosingOrder = value;
            Script.SetCircuitBreakerOpeningOrder = (value) => CircuitBreakerOpeningOrder = value;
            Script.SetTractionAuthorization = (value) => TractionAuthorization = value;
            Script.SetVigilanceAlarm = (value) => Locomotive.SignalEvent(value ? Event.VigilanceAlarmOn : Event.VigilanceAlarmOff);
            Script.SetHorn = (value) => Locomotive.TCSHorn = value;
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
            Script.SetVigilanceAlarmDisplay = (value) => this.VigilanceAlarm = value;
            Script.SetVigilanceEmergencyDisplay = (value) => this.VigilanceEmergency = value;
            Script.SetOverspeedWarningDisplay = (value) => this.OverspeedWarning = value;
            Script.SetPenaltyApplicationDisplay = (value) => this.PenaltyApplication = value;
            Script.SetMonitoringStatus = (value) => this.MonitoringStatus = value;
            Script.SetCurrentSpeedLimitMpS = (value) => this.CurrentSpeedLimitMpS = value;
            Script.SetNextSpeedLimitMpS = (value) => this.NextSpeedLimitMpS = value;
            Script.SetInterventionSpeedLimitMpS = (value) => this.InterventionSpeedLimitMpS = value;
            Script.SetNextSignalAspect = (value) => this.CabSignalAspect = (TrackMonitorSignalAspect)value;

            // TrainControlSystem INI configuration file
            Script.GetBoolParameter = (arg1, arg2, arg3) => LoadParameter<bool>(arg1, arg2, arg3);
            Script.GetIntParameter = (arg1, arg2, arg3) => LoadParameter<int>(arg1, arg2, arg3);
            Script.GetFloatParameter = (arg1, arg2, arg3) => LoadParameter<float>(arg1, arg2, arg3);
            Script.GetStringParameter = (arg1, arg2, arg3) => LoadParameter<string>(arg1, arg2, arg3);

            Script.Initialize();
        }

        T NextSignalItem<T>(int forsight, ref List<T> list, Train.TrainObjectItem.TRAINOBJECTTYPE type)
        {
            if (forsight < 0) forsight = 0;
            if (forsight >= list.Count) SearchTrainInfo(forsight, type);
            return list[forsight < list.Count ? forsight : list.Count - 1];
        }

        void SearchTrainInfo(float forsight, Train.TrainObjectItem.TRAINOBJECTTYPE searchFor)
        {
            if (SignalSpeedLimits.Count == 0)
                TrainInfo = Locomotive.Train.GetTrainInfo();

            var signalsFound = 0;
            var postsFound = 0;

            foreach (var foundItem in Locomotive.Train.MUDirection == Direction.Reverse ? TrainInfo.ObjectInfoBackward : TrainInfo.ObjectInfoForward)
            {
                switch (foundItem.ItemType)
                {
                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL:
                        signalsFound++;
                        if (signalsFound > SignalSpeedLimits.Count)
                        {
                            SignalSpeedLimits.Add(foundItem.AllowedSpeedMpS);
                            SignalAspects.Add((Aspect)foundItem.SignalState);
                            SignalDistances.Add(foundItem.DistanceToTrainM);
                        }
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST:
                        postsFound++;
                        if (postsFound > PostSpeedLimits.Count)
                        {
                            PostSpeedLimits.Add(foundItem.AllowedSpeedMpS);
                            PostDistances.Add(foundItem.DistanceToTrainM);
                        }
                        break;

                    case Train.TrainObjectItem.TRAINOBJECTTYPE.AUTHORITY:
                        signalsFound++;
                        if (signalsFound > SignalSpeedLimits.Count)
                        {
                            SignalSpeedLimits.Add(0f);
                            SignalAspects.Add(Aspect.Stop);
                            SignalDistances.Add(foundItem.DistanceToTrainM);
                        }
                        break;
                }

                if (searchFor == Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL && signalsFound > forsight ||
                    searchFor == Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST && postsFound > forsight)
                {
                    break;
                }
            }

            if (searchFor == Train.TrainObjectItem.TRAINOBJECTTYPE.SIGNAL && signalsFound == 0)
            {
                SignalSpeedLimits.Add(-1);
                SignalAspects.Add(Aspect.None);
                SignalDistances.Add(float.MaxValue);
            }
            if (searchFor == Train.TrainObjectItem.TRAINOBJECTTYPE.SPEEDPOST && postsFound == 0)
            {
                PostSpeedLimits.Add(-1);
                PostDistances.Add(float.MaxValue);
            }
        }

        private void SignalEvent(Event evt, TrainControlSystem script)
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

        public void Update()
        {
            switch (Locomotive.Train.TrainType)
            {
                case Train.TRAINTYPE.STATIC:
                case Train.TRAINTYPE.AI:
                case Train.TRAINTYPE.AI_NOTSTARTED:
                case Train.TRAINTYPE.AI_AUTOGENERATE:
                case Train.TRAINTYPE.REMOTE:
                case Train.TRAINTYPE.AI_INCORPORATED:
                    DisableRestrictions();
                    break;

                default:
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
            SignalSpeedLimits.Clear();
            SignalAspects.Clear();
            SignalDistances.Clear();
            PostSpeedLimits.Clear();
            PostDistances.Clear();
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

        public void SetEmergency(bool emergency)
        {
            if (Script != null)
                Script.SetEmergency(emergency);
            else
                Locomotive.TrainBrakeController.TCSEmergencyBraking = emergency;
        }

        public void HandleEvent(TCSEvent evt)
        {
            HandleEvent(evt, String.Empty);
        }

        public void HandleEvent(TCSEvent evt, string message)
        {
            if (Script != null)
                Script.HandleEvent(evt, message);
        }

        private T LoadParameter<T>(string sectionName, string keyName, T defaultValue)
        {
            var buffer = new String('\0', 256);

            var length = ORTS.Common.NativeMethods.GetPrivateProfileString(sectionName, keyName, null, buffer, buffer.Length, ParametersFileName);
            if (length > 0)
            {
                buffer.Trim();
                return (T)Convert.ChangeType(buffer, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            }
            else
                return defaultValue;
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

                SetEmergencyBrake(EmergencyBrake);
                SetFullBrake(FullBrake);
                SetPowerAuthorization(!PowerCut);

                if (EmergencyCausesThrottleDown && (IsBrakeEmergency() || IsBrakeFullService()))
                    SetThrottleController(0f);

                if (EmergencyEngagesHorn)
                    SetHorn(IsBrakeEmergency() || IsBrakeFullService());

                SetPenaltyApplicationDisplay(IsBrakeEmergency() || IsBrakeFullService());

                // Update monitoring status
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
            }
        }

        public override void SetEmergency(bool emergency)
        {
            ExternalEmergency = emergency;
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
