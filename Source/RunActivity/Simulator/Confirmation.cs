// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.

using System;

namespace ORTS {
    public enum ConfirmLevel
    {
        None,
        Information,
        Warning,
        Error,
		MSG,
    };

    public enum CabControl {
        None
        // Power
      , Power
      , Pantograph1
      , Pantograph2
      , PlayerDiesel
      , HelperDiesel
      , Reverser
      , Throttle
      // Steam power
      , Regulator
      , Injector1
      , Injector2
      , Blower
      , Damper
      , FiringRate
      , ManualFiring
      , FireShovelfull
      , CylinderCocks
      // Braking
      , TrainBrake
      , EngineBrake
      , DynamicBrake
      , EmergencyBrake
      , BailOff
      , InitializeBrakes
      , Handbrake
      , Retainers
      , BrakeHose
      // Cab Devices
      , Sander
      , Alerter
      , Horn
      , Whistle
      , Bell
      , Headlight
      , CabLight
      , Wipers
      , SwitchLocomotive
      // Train Devices
      , DoorsLeft
      , DoorsRight
      , Mirror
      // Track Devices
      , SwitchAhead
      , SwitchBehind
      , SimulationSpeed
    }

    public enum CabSetting {
        Off = 1     // 2 or 3 state control/reset/initialise
        , Neutral   // 2 or 3 state control
        , On        // 2 or 3 state control/apply/change
        , Decrease  // continuous control
        , Increase  // continuous control
        , Warn
        , Range1    // sub-range
        , Range2
        , Range3
        , Range4
    }

    /// <summary>
    /// Assembles confirmation messages in a list for MessageWindow to display.
    /// Also updates most recent message in list to show values as they changes.
    /// Also suppplements the buzzer with a warning message for operations that are disallowed.
    /// </summary>
    public class Confirmer
    {
        // ConfirmText provides a 2D array of strings so that all English text is confined to one place and can easily
        // be replaced with French and other languages.
        //
        //                      control, off/reset/initialize, neutral, on/apply/switch, decrease, increase, warn
        readonly string[][] ConfirmText = { 
              new string [] { "<none>" } 
            // Power
            , new string [] { "Power", "off", null, "on" } 
            , new string [] { "Pantograph 1", "lower", null, "raise" } 
            , new string [] { "Pantograph 2", "lower", null, "raise" }
            , new string [] { "Player Diesel Power", "off", null, "on", null, null, "locked. Close throttle then re-try." }
            , new string [] { "Helper Diesel Power", "off", null, "on" }
            , new string [] { "Reverser",  "reverse", "neutral", "forward", null, null, "locked. Close throttle then re-try." } 
            , new string [] { "Throttle", null, null, null, "close", "open", "locked. Release dynamic brake then re-try." } 
            // Steam power
            , new string [] { "Regulator", null, null, null, "close", "open" }    // Throttle for steam locomotives
            , new string [] { "Injector 1", "off", null, "on", "close", "open" } 
            , new string [] { "Injector 2", "off", null, "on", "close", "open" } 
            , new string [] { "Blower", null, null, null, "decrease", "increase" } 
            , new string [] { "Damper", null, null, null, "close", "open" } 
            , new string [] { "Firing Rate", null, null, null, "decrease", "increase" } 
            , new string [] { "Manual Firing", "off", null, "on" } 
            , new string [] { "Fire", null, null, "add shovelfull" } 
            , new string [] { "Cylinder Cocks", "close", null, "open" } 
            // Braking
            , new string [] { "Train Brake", null, null, null, "release", "apply" } 
            , new string [] { "Engine Brake", null, null, null, "release", "apply" } 
            , new string [] { "Dynamic Brake" } 
            , new string [] { "Emergency Brake", null, null, "apply" } 
            , new string [] { "Bail Off", "disengage", null, "engage" } 
            , new string [] { "Brakes", "initialize", null, null, null, null, "cannot initialize. Stop train then re-try." } 
            , new string [] { "Handbrake", "none", null, "full" } 
            , new string [] { "Retainers", "off", null, "on", null, null, null, "Exhaust", "High Pressure", "Low Pressure", "Slow Direct" } 
            , new string [] { "Brake Hose", "disconnect", null, "connect" } 
            // Cab Devices
            , new string [] { "Sander", "off", null, "on" } 
            , new string [] { "Alerter", "acknowledge", null, "sound" } 
            , new string [] { "Horn", "off", null, "sound" } 
            , new string [] { "Whistle", "off", null, "blow" }        // Horn for steam locomotives
            , new string [] { "Bell", "off", null, "ring" } 
            , new string [] { "Headlight", "off", "dim", "bright" } 
            , new string [] { "Cab Light", "off", null, "on" } 
            , new string [] { "Wipers", "off", null, "on" } 
            , new string [] { "Locomotive", null, null, "switch" } 
            // Train Devices
            , new string [] { "Doors Left", "close", null, "open" } 
            , new string [] { "Doors Right", "close", null, "open" } 
            , new string [] { "Mirror", "retract", null, "extend" } 
            // Track Devices
            , new string [] { "Switch Ahead", null, null, "change" } 
            , new string [] { "Switch Behind", null, null, "change" } 
            // Simulation
            , new string [] { "Simulation Speed", "reset", null, null, "decrease", "increase" } 
            };

        readonly Viewer3D Viewer;
        readonly double DefaultDurationS;

        public Confirmer(Viewer3D viewer, double defaultDurationS)
        {
            Viewer = viewer;
            DefaultDurationS = defaultDurationS;
        }

        #region Control confirmation

        public void Confirm(CabControl control, CabSetting setting)
        {
            Message(control, "{0}", ConfirmText[(int)control][(int)setting]);
        }

        public void Confirm(CabControl control, CabSetting setting, string text)
        {
            Message(control, "{0} {1}", ConfirmText[(int)control][(int)setting], text);
        }

        public void ConfirmWithPerCent(CabControl control, CabSetting setting, float perCent)
        {
            Message(control, "{0} to {1:0}%", ConfirmText[(int)control][(int)setting], perCent);
        }

        public void ConfirmWithPerCent(CabControl control, CabSetting setting1, float perCent, int setting2)
        {
            Message(control, "{0} {1:0}% {2}", ConfirmText[(int)control][(int)setting1], perCent, ConfirmText[(int)control][setting2]);
        }

        public void ConfirmWithPerCent(CabControl control, float perCent, CabSetting setting)
        {
            Message(control, "{0:0}% {1}", perCent, ConfirmText[(int)control][(int)setting]);
        }

        public void ConfirmWithPerCent(CabControl control, float perCent)
        {
            Message(control, "{0:0}%", perCent);
        }

        #endregion
        #region Control updates

        public void UpdateWithPerCent(CabControl control, int action, float perCent)
        {
            Message(control, "{0} {1:0}%", ConfirmText[(int)control][action], perCent);
        }

        public void UpdateWithPerCent(CabControl control, CabSetting setting, float perCent)
        {
            Message(control, "{0} {1:0}%", ConfirmText[(int)control][(int)setting], perCent);
        }

        public void Update(CabControl control, CabSetting setting, string text)
        {
            Message(control, "{0} {1}", ConfirmText[(int)control][(int)setting], text);
        }

        #endregion
        #region Control messages

        public void Message(CabControl control, string format, params object[] args)
        {
            Message(control, ConfirmLevel.None, String.Format(format, args));
        }

        public void Warning(CabControl control, CabSetting setting)
        {
            if (Viewer.World.GameSounds != null) Viewer.World.GameSounds.HandleEvent(10);
            Message(control, ConfirmLevel.Warning, ConfirmText[(int)control][(int)setting]);
        }

        #endregion
        #region Non-control messages

        public void Information(string message)
        {
            Message(CabControl.None, ConfirmLevel.Information, message);
        }

		public void MSG(string message)
		{
			Message(CabControl.None, ConfirmLevel.MSG, message);
		}
		
		public void Warning(string message)
        {
            Message(CabControl.None, ConfirmLevel.Warning, message);
        }

        public void Error(string message)
        {
            Message(CabControl.None, ConfirmLevel.Error, message);
        }

        public void Message(ConfirmLevel level, string message)
        {
            Message(CabControl.None, level, message);
        }

        #endregion

        void Message(CabControl control, ConfirmLevel level, string message)
        {
            if (level < ConfirmLevel.Information && Viewer.Settings.SuppressConfirmations)
                return;

            var format = "{2}";
            if (control != CabControl.None)
                format = "{0}: " + format;
            if (level >= ConfirmLevel.Information)
                format = "{1} - " + format;
			var duration = DefaultDurationS;
			if (level >= ConfirmLevel.Warning) duration *= 2;
			if (level >= ConfirmLevel.MSG) duration *= 5;
            Viewer.MessagesWindow.AddMessage(String.Format("{0}/{1}", control, level), String.Format(format, ConfirmText[(int)control][0], level, message), duration);
        }
    }
}