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
        public bool VigilanceWarning = false;
        public bool VigilanceAlarm = false;
        public bool AlerterButtonPressed = false;

        public bool OverspeedWarning = false;
        
        protected MSTSLocomotive MSTSLocomotive;
        protected Simulator Simulator;

        protected MonitoringDevice VigilanceMonitor;
        protected MonitoringDevice OverspeedMonitor;
        protected MonitoringDevice EmergencyStopMonitor;
        protected MonitoringDevice AWSMonitor;

        protected bool AlerterIsActive = false;

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
            if (MSTSLocomotive.TrainBrakeController.GetIsEmergency())
                return;
            if (MSTSLocomotive.EmergencyCausesThrottleDown) MSTSLocomotive.ThrottleController.SetValue(0.0f);
            if (EmergencyStopMonitor.EmergencyCutsPower) { MSTSLocomotive.SignalEvent(Event.Pantograph1Down); MSTSLocomotive.SignalEvent(Event.Pantograph2Down); }
            if (MSTSLocomotive.EmergencyEngagesHorn) MSTSLocomotive.SignalEvent(Event.HornOn);
            MSTSLocomotive.TrainBrakeController.SetEmergency();
            MSTSLocomotive.SignalEvent(Event.TrainBrakePressureDecrease);
            Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.On);
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
            public float MonitorTimeS = 60;
            public float AlarmTimeS = 6;
            public float PenaltyTimeS = 0;
            public bool EmergencyCutsPower = true;
            public bool ResetOnZeroSpeed = true;
            public float TriggerOnOverspeedMpS = 149;
            public float CriticalSpeedMpS = 150;
            public float AlarmTimeBeforeOverspeedS = 5;

            public MonitoringDevice(STFReader stf)
            {
                stf.MustMatch("(");
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("monitoringdevicemonitortimelimit", ()=>{ MonitorTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, 60); }),
                    new STFReader.TokenProcessor("monitoringdevicealarmtimelimit", ()=>{ AlarmTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, 6); }),
                    new STFReader.TokenProcessor("monitoringdevicepenaltytimelimit", ()=>{ PenaltyTimeS = stf.ReadFloatBlock(STFReader.UNITS.Time, 0); }),
                    new STFReader.TokenProcessor("monitoringdeviceappliescutspower", ()=>{ EmergencyCutsPower = stf.ReadBoolBlock(false); }),
                    new STFReader.TokenProcessor("monitoringdeviceresetonzerospeed", ()=>{ ResetOnZeroSpeed = stf.ReadBoolBlock(true); }),
                    new STFReader.TokenProcessor("monitoringdevicetriggeronoverspeed", ()=>{ TriggerOnOverspeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
                    new STFReader.TokenProcessor("monitoringdevicecriticallevel", ()=>{ CriticalSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
                    new STFReader.TokenProcessor("monitoringdevicealarmtimebeforeoverspeed", ()=>{ AlarmTimeBeforeOverspeedS = stf.ReadFloatBlock(STFReader.UNITS.Time, 5); }),
                });
            }

            public MonitoringDevice() { }
        }
    }

    public class MSTSTrainControlSystem : TrainControlSystem
    {
        Timer MonitorTimer = new Timer();
        Timer AlarmTimer = new Timer();
        Timer PenaltyTimer = new Timer();
        Timer OverspeedTimer = new Timer();

        public MSTSTrainControlSystem(MSTSLocomotive mstsLocomotive)
        {
            MSTSLocomotive = mstsLocomotive;
            Simulator = MSTSLocomotive.Simulator;
        }

        public MSTSTrainControlSystem(MSTSTrainControlSystem other) :
            base(other)
        {
            MonitorTimer = other.MonitorTimer;
            AlarmTimer = other.AlarmTimer;
            PenaltyTimer = other.PenaltyTimer;
            OverspeedTimer = other.OverspeedTimer;
        }

        public override void Startup()
        {
            if (VigilanceMonitor == null)
                VigilanceMonitor = new MonitoringDevice();
            if (OverspeedMonitor == null)
                OverspeedMonitor = new MonitoringDevice();
            if (EmergencyStopMonitor == null)
                EmergencyStopMonitor = new MonitoringDevice();

            MonitorTimer.Setup(MSTSLocomotive, VigilanceMonitor.MonitorTimeS);
            AlarmTimer.Setup(MSTSLocomotive, VigilanceMonitor.AlarmTimeS);
            PenaltyTimer.Setup(MSTSLocomotive, VigilanceMonitor.PenaltyTimeS);
            OverspeedTimer.Setup(MSTSLocomotive, OverspeedMonitor.AlarmTimeBeforeOverspeedS);

            MonitorTimer.Start();
            
            AlerterIsActive = true;
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
            if (!AlerterIsActive)
                return;
            if (VigilanceAlarm)
                return;
            
            MonitorTimer.Start();
        }

        public new MSTSTrainControlSystem Clone()
        {
            return new MSTSTrainControlSystem(this);
        }

        public override void Update()
        {
            if (!MSTSLocomotive.Simulator.Settings.Alerter)
                return;

            if (VigilanceMonitor.ResetOnZeroSpeed && Math.Abs(MSTSLocomotive.SpeedMpS) < 0.1f)
                TryReset();

            OverspeedWarning = Math.Abs(MSTSLocomotive.SpeedMpS) > OverspeedMonitor.TriggerOnOverspeedMpS;
            VigilanceWarning = MonitorTimer.Triggered;
            VigilanceAlarm = AlarmTimer.Triggered;
            VigilanceAlarm |= Math.Abs(MSTSLocomotive.SpeedMpS) > OverspeedMonitor.CriticalSpeedMpS;
            VigilanceAlarm |= OverspeedTimer.Triggered;

            if (VigilanceAlarm)
            {
                SetEmergency();
                
                if (!PenaltyTimer.Started)
                    PenaltyTimer.Start();
                if (Math.Abs(MSTSLocomotive.SpeedMpS) < 0.1f && PenaltyTimer.Triggered)
                {
                    AlarmTimer.Stop();
                    PenaltyTimer.Stop();
                    OverspeedTimer.Stop();
                }
                if (MSTSLocomotive.AlerterSnd)
                    MSTSLocomotive.SignalEvent(Event.VigilanceAlarmOff);
                return;
            }

            if (VigilanceWarning)
            {
                if (Simulator.Confirmer.Viewer.Camera.Style != Camera.Styles.Cab) // Auto-clear alerter when not in cabview
                {
                    TryReset();
                    return;
                }
                if (!AlarmTimer.Started)
                    AlarmTimer.Start();
                if (!MSTSLocomotive.AlerterSnd)
                    MSTSLocomotive.SignalEvent(Event.VigilanceAlarmOn);
            }
            else
            {
                AlarmTimer.Stop();
                if (MSTSLocomotive.AlerterSnd)
                    MSTSLocomotive.SignalEvent(Event.VigilanceAlarmOff);
                if (PenaltyTimer.Started && PenaltyTimer.Triggered)
                    PenaltyTimer.Stop();
            }

            if (OverspeedWarning)
            {
                if (!OverspeedTimer.Started)
                    OverspeedTimer.Start();
            }
            else
                OverspeedTimer.Stop();
        }

        public override void Parse(string lowercasetoken, STFReader stf)
        {
            base.Parse(lowercasetoken, stf);
        }
    }

}
