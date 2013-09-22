// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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
using System.Linq;
using MSTS;

namespace ORTS
{
    public abstract class TrainControlSystem
    {
        // Following values are queried by CabView:
        public bool VigilanceAlarm;
        public bool VigilanceEmergency;
        public bool AlerterButtonPressed;
        public bool OverspeedWarning;
        public bool PenaltyApplication;

        public bool OverspeedAlarm;
        
        protected MSTSLocomotive MSTSLocomotive;
        protected Simulator Simulator;

        protected MonitoringDevice VigilanceMonitor;
        protected MonitoringDevice OverspeedMonitor;
        protected MonitoringDevice EmergencyStopMonitor;
        protected MonitoringDevice AWSMonitor;

        protected bool TrainControlSystemIsActive;

        public TrainControlSystem() { }

        public TrainControlSystem(TrainControlSystem other)
        {
            if (other.MSTSLocomotive != null) MSTSLocomotive = other.MSTSLocomotive;
            if (other.Simulator != null) Simulator = other.Simulator;
            if (other.VigilanceMonitor != null) VigilanceMonitor = other.VigilanceMonitor;
            if (other.OverspeedMonitor != null) OverspeedMonitor = other.OverspeedMonitor;
            if (other.EmergencyStopMonitor != null) EmergencyStopMonitor = other.EmergencyStopMonitor;
            if (other.AWSMonitor != null) AWSMonitor = other.AWSMonitor;
        }

        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(vigilancemonitor": VigilanceMonitor = new MonitoringDevice(stf); break;
                case "engine(overspeedmonitor": OverspeedMonitor = new MonitoringDevice(stf); break;
                case "engine(emergencystopmonitor": EmergencyStopMonitor = new MonitoringDevice(stf); break;
                case "engine(awsmonitor": AWSMonitor = new MonitoringDevice(stf); break;
            }
        }

        public virtual void Update() { }

        public virtual void Startup() { }

        // Internal reset request. Some systems don't accept this, thus needs to be handled separately.
        public virtual void AlerterReset() { }

        // Driver reset request
        public virtual void AlerterPressed(bool pressed) { }
        
        // Reset if allowed
        public virtual void TryReset() { }

        public virtual TrainControlSystem Clone() { return this; }

        public void SetEmergency()
        {
            PenaltyApplication = true;
            if (EmergencyStopMonitor != null && !EmergencyStopMonitor.AppliesEmergencyBrake)
            {
                if (MSTSLocomotive.TrainBrakeController.GetIsFullBrake() || MSTSLocomotive.TrainBrakeController.GetIsEmergency())
                    return;
                SetFullBrake();
            }
            else
            {
                if (MSTSLocomotive.TrainBrakeController.GetIsEmergency())
                    return;
                SetEmergencyBrake();
            }
            if (MSTSLocomotive.EmergencyCausesThrottleDown) MSTSLocomotive.ThrottleController.SetValue(0.0f);
            if (EmergencyStopMonitor != null && EmergencyStopMonitor.EmergencyCutsPower) { MSTSLocomotive.SignalEvent(Event.Pantograph1Down); MSTSLocomotive.SignalEvent(Event.Pantograph2Down); }
            if (MSTSLocomotive.EmergencyEngagesHorn) MSTSLocomotive.SignalEvent(Event.HornOn);
            MSTSLocomotive.SignalEvent(Event.TrainBrakePressureDecrease);
        }

        public void SetEmergencyBrake()
        {
            PenaltyApplication = true;
            MSTSLocomotive.TrainBrakeController.SetEmergency();
            Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.On);
        }

        public void SetFullBrake()
        {
            PenaltyApplication = true;
            MSTSLocomotive.TrainBrakeController.SetFullBrake();
            Simulator.Confirmer.Confirm(CabControl.TrainBrake, CabSetting.On);
        }

        protected class Alerter
        {
            protected float EndValue;
            protected float AlarmValue;
            protected MSTSLocomotive MSTSLocomotive;
            public bool Started;

            protected virtual float CurrentValue { get; set; }
            public void Setup(MSTSLocomotive mstsLocomotive, float alarmValue) { MSTSLocomotive = mstsLocomotive; AlarmValue = alarmValue; }
            public void Start() { EndValue = CurrentValue + AlarmValue; Started = true; }
            public void Stop() { Started = false; }
            public bool Triggered { get { return Started && CurrentValue >= EndValue; }}
        }

        protected class Timer : Alerter
        {
            protected override float CurrentValue { get { return (float)MSTSLocomotive.Simulator.ClockTime; }}
        }

        protected class OdoMeter : Alerter
        {
            protected override float CurrentValue { get { return MSTSLocomotive.DistanceM; }}
        }
        
        protected class MonitoringDevice
        {
            public float MonitorTimeS = 66; // Time from alerter reset to applying emergency brake
            public float AlarmTimeS = 60; // Time from alerter reset to audible and visible alarm
            public float PenaltyTimeS;
            public bool EmergencyCutsPower;
            public bool EmergencyShutsDownEngine;
            public bool ResetOnZeroSpeed = true;
            public float CriticalLevelMpS;
            public float ResetLevelMpS;
            public bool AppliesFullBrake = true;
            public bool AppliesEmergencyBrake;

            // Following are for OverspeedMonitor only
            public bool ResetOnResetButton;
            public float TriggerOnOverspeedMpS;
            public bool TriggerOnTrackOverspeed;
            public float TriggerOnTrackOverspeedMarginMpS = 4;
            public float AlarmTimeBeforeOverspeedS = 5;

            public MonitoringDevice(STFReader stf)
            {
                stf.MustMatch("(");
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("monitoringdevicemonitortimelimit", ()=>{ MonitorTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, MonitorTimeS); }),
                    new STFReader.TokenProcessor("monitoringdevicealarmtimelimit", ()=>{ AlarmTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, AlarmTimeS); }),
                    new STFReader.TokenProcessor("monitoringdevicepenaltytimelimit", ()=>{ PenaltyTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, PenaltyTimeS); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliescutspower", ()=>{ EmergencyCutsPower = stf.ReadBoolBlock(EmergencyCutsPower); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliesshutsdownengine", ()=>{ EmergencyShutsDownEngine = stf.ReadBoolBlock(EmergencyShutsDownEngine); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetonzerospeed", ()=>{ ResetOnZeroSpeed = stf.ReadBoolBlock(ResetOnZeroSpeed); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetonresetbutton", ()=>{ ResetOnResetButton = stf.ReadBoolBlock(ResetOnResetButton); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggeronoverspeed", ()=>{ TriggerOnOverspeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, TriggerOnOverspeedMpS); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggerontrackoverspeed", ()=>{ TriggerOnTrackOverspeed = stf.ReadBoolBlock(TriggerOnTrackOverspeed); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggerontrackoverspeedmargin", ()=>{ TriggerOnTrackOverspeedMarginMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, TriggerOnTrackOverspeedMarginMpS); }),
                    new STFReader.TokenProcessor("monitoringdevicecriticallevel", ()=>{ CriticalLevelMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, CriticalLevelMpS); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetlevel", ()=>{ ResetLevelMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, ResetLevelMpS); }),
                    new STFReader.TokenProcessor("monitoringdevicealarmtimebeforeoverspeed", ()=>{ AlarmTimeBeforeOverspeedS = stf.ReadFloatBlock(STFReader.UNITS.Time, AlarmTimeBeforeOverspeedS); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliesfullbrake", ()=>{ AppliesFullBrake = stf.ReadBoolBlock(AppliesFullBrake); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliesemergencybrake", ()=>{ AppliesEmergencyBrake = stf.ReadBoolBlock(AppliesEmergencyBrake); }),
                });
            }

            public MonitoringDevice() { }
        }
    }

    public class MSTSTrainControlSystem : TrainControlSystem
    {
        Timer VigilanceAlarmTimer = new Timer();
        Timer VigilanceEmergencyTimer = new Timer();
        Timer VigilancePenaltyTimer = new Timer();
        Timer OverspeedAlarmTimer = new Timer();
        Timer OverspeedPenaltyTimer = new Timer();

        float VigilanceAlarmTimeoutS;

        public MSTSTrainControlSystem(MSTSLocomotive mstsLocomotive)
        {
            MSTSLocomotive = mstsLocomotive;
            Simulator = MSTSLocomotive.Simulator;
        }

        public MSTSTrainControlSystem(MSTSTrainControlSystem other) :
            base(other)
        {
            VigilanceAlarmTimer = other.VigilanceAlarmTimer;
            VigilanceEmergencyTimer = other.VigilanceEmergencyTimer;
            VigilancePenaltyTimer = other.VigilancePenaltyTimer;
            OverspeedAlarmTimer = other.OverspeedAlarmTimer;
            OverspeedPenaltyTimer = other.OverspeedPenaltyTimer;
        }

        public override void Startup()
        {
            if (VigilanceMonitor != null)
            {
                if (VigilanceMonitor.MonitorTimeS > VigilanceMonitor.AlarmTimeS)
                    VigilanceAlarmTimeoutS = VigilanceMonitor.MonitorTimeS - VigilanceMonitor.AlarmTimeS;
                VigilanceAlarmTimer.Setup(MSTSLocomotive, VigilanceMonitor.AlarmTimeS);
                VigilanceEmergencyTimer.Setup(MSTSLocomotive, VigilanceAlarmTimeoutS);
                VigilancePenaltyTimer.Setup(MSTSLocomotive, VigilanceMonitor.PenaltyTimeS);
                VigilanceAlarmTimer.Start();
            }
            if (OverspeedMonitor != null)
            {
                OverspeedAlarmTimer.Setup(MSTSLocomotive, Math.Max(OverspeedMonitor.AlarmTimeS, OverspeedMonitor.AlarmTimeBeforeOverspeedS));
                OverspeedPenaltyTimer.Setup(MSTSLocomotive, OverspeedMonitor.PenaltyTimeS);
            }
            TrainControlSystemIsActive = true;
        }

        public override void AlerterReset()
        {
            TryReset();
        }

        public override void AlerterPressed(bool pressed)
        {
            TryReset();
            AlerterButtonPressed = pressed;
        }
        
        public override void TryReset()
        {
            if (!TrainControlSystemIsActive || VigilanceEmergency)
                return;

            if (VigilanceMonitor != null)
            {
                VigilanceAlarmTimer.Start();
                VigilanceAlarm = VigilanceAlarmTimer.Triggered;
                if (MSTSLocomotive.AlerterSnd)
                    MSTSLocomotive.SignalEvent(Event.VigilanceAlarmOff);
            }

            if (OverspeedMonitor != null)
                if (OverspeedWarning && OverspeedMonitor.ResetOnResetButton)
                    OverspeedAlarmTimer.Start();
        }

        public new MSTSTrainControlSystem Clone()
        {
            return new MSTSTrainControlSystem(this);
        }

        public override void Update()
        {
            if (VigilanceMonitor != null)
                UpdateVigilance();
            if (OverspeedMonitor != null)
                UpdateSpeedControl();

            if (PenaltyApplication && !MSTSLocomotive.TrainBrakeController.GetIsEmergency() && !MSTSLocomotive.TrainBrakeController.GetIsFullBrake())
                PenaltyApplication = false;
        }

        private void UpdateVigilance()
        {
            if (!Simulator.Settings.Alerter)
                return;

            VigilanceAlarm = VigilanceAlarmTimer.Triggered;
            VigilanceEmergency = VigilanceEmergencyTimer.Triggered;

            if (VigilanceEmergency)
            {
                if (VigilanceMonitor.AppliesEmergencyBrake)
                    SetEmergency();
                else if (VigilanceMonitor.AppliesFullBrake)
                    SetFullBrake();
                
                if (!VigilancePenaltyTimer.Started)
                    VigilancePenaltyTimer.Start();
                if (Math.Abs(MSTSLocomotive.SpeedMpS) < 0.1f && VigilancePenaltyTimer.Triggered)
                {
                    VigilanceEmergencyTimer.Stop();
                    VigilancePenaltyTimer.Stop();
                }
                if (MSTSLocomotive.AlerterSnd)
                    MSTSLocomotive.SignalEvent(Event.VigilanceAlarmOff);
                return;
            }

            if (VigilanceAlarm)
            {
                if (Simulator.Confirmer.Viewer.Camera.Style != Camera.Styles.Cab // Auto-clear alerter when not in cabview
                    || VigilanceMonitor.ResetOnZeroSpeed && Math.Abs(MSTSLocomotive.SpeedMpS) < 0.1f
                    || Math.Abs(MSTSLocomotive.SpeedMpS) <= VigilanceMonitor.ResetLevelMpS)
                {
                    TryReset();
                    return;
                }
                if (!VigilanceEmergencyTimer.Started)
                    VigilanceEmergencyTimer.Start();
                if (!MSTSLocomotive.AlerterSnd)
                    MSTSLocomotive.SignalEvent(Event.VigilanceAlarmOn);
            }
            else
            {
                VigilanceEmergencyTimer.Stop();
                if (VigilancePenaltyTimer.Triggered)
                    VigilancePenaltyTimer.Stop();
            }
        }

        private void UpdateSpeedControl()
        {
            OverspeedWarning = false;

            // Not sure about the difference of the following two. Seems both of them are used.
            if (OverspeedMonitor.TriggerOnOverspeedMpS > 0)
                OverspeedWarning |= Math.Abs(MSTSLocomotive.SpeedMpS) > OverspeedMonitor.TriggerOnOverspeedMpS;
            if (OverspeedMonitor.CriticalLevelMpS > 0)
                OverspeedWarning |= Math.Abs(MSTSLocomotive.SpeedMpS) > OverspeedMonitor.CriticalLevelMpS;
            if (OverspeedMonitor.TriggerOnTrackOverspeed)
                OverspeedWarning |= Math.Abs(MSTSLocomotive.SpeedMpS) > MSTSLocomotive.Train.AllowedMaxSpeedMpS + OverspeedMonitor.TriggerOnTrackOverspeedMarginMpS;

            OverspeedAlarm = OverspeedAlarmTimer.Triggered;

            if (OverspeedAlarm && Simulator.Settings.Alerter)
            {
                if (OverspeedMonitor.AppliesEmergencyBrake)
                    SetEmergency();
                else if (OverspeedMonitor.AppliesFullBrake)
                    SetFullBrake();

                if (!OverspeedPenaltyTimer.Started)
                    OverspeedPenaltyTimer.Start();
                if (Math.Abs(MSTSLocomotive.SpeedMpS) < 0.1f && OverspeedPenaltyTimer.Triggered)
                {
                    OverspeedAlarmTimer.Stop();
                    OverspeedPenaltyTimer.Stop();
                }
                return;
            }
            if (OverspeedWarning)
            {
                if (!OverspeedAlarmTimer.Started)
                    OverspeedAlarmTimer.Start();
            }
            else
            {
                OverspeedAlarmTimer.Stop();
                if (OverspeedPenaltyTimer.Triggered)
                    OverspeedPenaltyTimer.Stop();
            }
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            base.Parse(lowercasetoken, stf);
        }
    }

}
