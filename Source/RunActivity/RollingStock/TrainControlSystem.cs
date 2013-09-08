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
        public bool VigilanceAlarm = false;
        public bool VigilanceEmergency = false;
        public bool AlerterButtonPressed = false;
        public bool OverspeedWarning = false;
        public bool PenaltyApplication = false;
        
        public bool OverspeedAlarm = false;
        
        protected MSTSLocomotive MSTSLocomotive;
        protected Simulator Simulator;

        protected MonitoringDevice VigilanceMonitor;
        protected MonitoringDevice OverspeedMonitor;
        protected MonitoringDevice EmergencyStopMonitor;
        protected MonitoringDevice AWSMonitor;

        protected bool TrainControlSystemIsActive = false;

        public TrainControlSystem() { }

        public TrainControlSystem(TrainControlSystem other)
        {
            if (other.MSTSLocomotive != null)
                MSTSLocomotive = other.MSTSLocomotive;
            if (other.Simulator != null)
                Simulator = other.Simulator;
            VigilanceMonitor = other.VigilanceMonitor;
            OverspeedMonitor = other.OverspeedMonitor;
            EmergencyStopMonitor = other.EmergencyStopMonitor;
            AWSMonitor = other.AWSMonitor;
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
            public bool Started = false;

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
            public float MonitorTimeS = 66;
            public float AlarmTimeS = 60;
            public float PenaltyTimeS = 0;
            public bool EmergencyCutsPower = false;
            public bool EmergencyShutsDownEngine = false;
            public bool ResetOnZeroSpeed = true;
            public float CriticalLevelMpS = 0;
            public float ResetLevelMpS = 0;
            public bool AppliesFullBrake = true;
            public bool AppliesEmergencyBrake = false;

            // Following are for OverspeedMonitor only
            public bool ResetOnResetButton = false;
            public float TriggerOnOverspeedMpS = 0;
            public bool TriggerOnTrackOverspeed = false;
            public float TriggerOnTrackOverspeedMarginMpS = 0;
            public float AlarmTimeBeforeOverspeedS = 0;

            public MonitoringDevice(STFReader stf)
            {
                stf.MustMatch("(");
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("monitoringdevicemonitortimelimit", ()=>{ MonitorTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, 66); }),
                    new STFReader.TokenProcessor("monitoringdevicealarmtimelimit", ()=>{ AlarmTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, 60); }),
                    new STFReader.TokenProcessor("monitoringdevicepenaltytimelimit", ()=>{ PenaltyTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, 0); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliescutspower", ()=>{ EmergencyCutsPower = stf.ReadBoolBlock(false); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliesshutsdownengine", ()=>{ EmergencyShutsDownEngine = stf.ReadBoolBlock(false); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetonzerospeed", ()=>{ ResetOnZeroSpeed = stf.ReadBoolBlock(true); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetonresetbutton", ()=>{ ResetOnResetButton = stf.ReadBoolBlock(true); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggeronoverspeed", ()=>{ TriggerOnOverspeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggerontrackoverspeed", ()=>{ TriggerOnTrackOverspeed = stf.ReadBoolBlock(false); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggerontrackoverspeedmargin", ()=>{ TriggerOnTrackOverspeedMarginMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, 4); }),
                    new STFReader.TokenProcessor("monitoringdevicecriticallevel", ()=>{ CriticalLevelMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetlevel", ()=>{ ResetLevelMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
                    new STFReader.TokenProcessor("monitoringdevicealarmtimebeforeoverspeed", ()=>{ AlarmTimeBeforeOverspeedS = stf.ReadFloatBlock(STFReader.UNITS.Time, 5); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliesfullbrake", ()=>{ AppliesFullBrake = stf.ReadBoolBlock(true); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliesemergencybrake", ()=>{ AppliesEmergencyBrake = stf.ReadBoolBlock(true); }),
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

        float VigilanceAlarmTimeoutS = 0;

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
            if (VigilanceMonitor == null)
                VigilanceMonitor = new MonitoringDevice();
            if (OverspeedMonitor == null)
                OverspeedMonitor = new MonitoringDevice();
            if (EmergencyStopMonitor == null)
                EmergencyStopMonitor = new MonitoringDevice();
            if (AWSMonitor == null)
                AWSMonitor = new MonitoringDevice();

            if (VigilanceMonitor.MonitorTimeS > VigilanceMonitor.AlarmTimeS)
                VigilanceAlarmTimeoutS = VigilanceMonitor.MonitorTimeS - VigilanceMonitor.AlarmTimeS;

            VigilanceAlarmTimer.Setup(MSTSLocomotive, VigilanceMonitor.AlarmTimeS);
            VigilanceEmergencyTimer.Setup(MSTSLocomotive, VigilanceAlarmTimeoutS);
            VigilancePenaltyTimer.Setup(MSTSLocomotive, VigilanceMonitor.PenaltyTimeS);
            OverspeedAlarmTimer.Setup(MSTSLocomotive, Math.Max(OverspeedMonitor.AlarmTimeS, OverspeedMonitor.AlarmTimeBeforeOverspeedS));
            OverspeedPenaltyTimer.Setup(MSTSLocomotive, OverspeedMonitor.PenaltyTimeS);

            VigilanceAlarmTimer.Start();
            
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

            VigilanceAlarmTimer.Start();
            VigilanceAlarm = VigilanceAlarmTimer.Triggered;

            if (MSTSLocomotive.AlerterSnd)
                MSTSLocomotive.SignalEvent(Event.VigilanceAlarmOff);

            if (OverspeedWarning && OverspeedMonitor.ResetOnResetButton)
                OverspeedAlarmTimer.Start();
        }

        public new MSTSTrainControlSystem Clone()
        {
            return new MSTSTrainControlSystem(this);
        }

        public override void Update()
        {
            UpdateVigilance();
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
