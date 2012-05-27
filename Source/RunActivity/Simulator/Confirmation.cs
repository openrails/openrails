using System;
using System.Collections.Generic;
using System.Diagnostics;   // needed for Debug
using System.Linq;
using System.Text;
using System.Timers;    // needed by Timer

namespace ORTS {

    public struct Confirmation {
        public string Message;
        public double DurationS;
    }

    public enum CabControl {
        // Power
        Pantograph1
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
    public class Confirmer {
        // ConfirmText provides a 2D array of strings so that all English text is confined to one place and can easily
        // be replaced with French and other languages.
        //
        //                      control, off/reset/initialize, neutral, on/apply/switch, decrease, increase, warn
        readonly string[][] ConfirmText = { 
            // Power
              new string [] { "Pantograph 1", "lower", null, "raise" } 
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

        public List<Confirmation> ConfirmationList { get; set; }
        public Confirmation LatestConfirmation {
            get { return latestConfirmation; }
            set { latestConfirmation = value; }
        } private Confirmation latestConfirmation;
        public bool Updated { get; set; }

        double defaultDurationS;
        bool suppressConfirmations;
        World world;

        public Confirmer( bool suppressConfirmations, World world, double defaultDurationS ) {
            this.suppressConfirmations = suppressConfirmations;
            this.world = world;
            this.defaultDurationS = defaultDurationS;
            ConfirmationList = new List<Confirmation>();
        }

        public void Confirm( CabControl control, CabSetting setting ) {
            var i = (int)control;
            ConfirmWithText( control, ConfirmText[i][(int)setting] );
        }

        public void Confirm( CabControl control, CabSetting setting, string text ) {
            var i = (int)control;
            ConfirmWithText( control, ConfirmText[i][(int)setting] + " " + text);
        }

        public void ConfirmWithPerCent( CabControl control, CabSetting setting, float perCent ) {
            var i = (int)control;
            string message = String.Format( "{0} to {1:0}%", ConfirmText[i][(int)setting], perCent );
            ConfirmWithText( control, message );
        }

        public void ConfirmWithPerCent( CabControl control, CabSetting setting1, float perCent, int setting2 ) {
            var i = (int)control;
            string message = String.Format( " {0} {1:0}% {2}", ConfirmText[i][(int)setting1], perCent, ConfirmText[i][setting2] );
            ConfirmWithText( control, message );
        }

        public void ConfirmWithPerCent( CabControl control, float perCent, CabSetting setting ) {
            var i = (int)control; 
            string message = String.Format( " {0:0}% {1}", perCent, ConfirmText[i][(int)setting] );
            ConfirmWithText( control, message );
        }

        public void ConfirmWithPerCent( CabControl control, float perCent ) {
            string message = String.Format( " {0:0}%", perCent );
            ConfirmWithText( control, message );
        }

        public void ConfirmWithText( CabControl control, string text ) {
            if( !suppressConfirmations ) {
                var i = (int)control;
                Confirmation a;
                a.Message = String.Format( "{0}: {1}", ConfirmText[i][0], text );
                a.DurationS = defaultDurationS;
                // Messages are added to the confirmation list and not directly to the Messages list to avoid one
                // thread calling another.
                ConfirmationList.Add( a );
                Updated = false;
            }
        }

        public void UpdateWithPerCent( CabControl control, int action, float perCent ) {
            var i = (int)control;
            string message = String.Format( "{0} {1:0}%", ConfirmText[i][action], perCent );
            Update( control, message );
        }

        public void UpdateWithPerCent( CabControl control, CabSetting setting, float perCent ) {
            var i = (int)control;
            string message = String.Format( "{0} {1:0}%", ConfirmText[i][(int)setting], perCent );
            Update( control, message );
        }

        public void Update( CabControl control, CabSetting setting, string text ) {
            var i = (int)control;
            string message = String.Format( "{0} {1}", ConfirmText[i][(int)setting], text );
            Update( control, message );
        }

        public void Update( CabControl control, string text ) {
            if( !suppressConfirmations ) {
                var i = (int)control;
                latestConfirmation.Message = String.Format( "{0}: {1}", ConfirmText[i][0], text );
                latestConfirmation.DurationS = defaultDurationS;
                Updated = true; // Set here and cancelled by MessageWindow.PrepareFrame()
            }
        }

        public void Warn( CabControl control, CabSetting setting ) {
            if( world.GameSounds != null ) world.GameSounds.HandleEvent( 10 );
            if( !suppressConfirmations ) {
                var i = (int)control;
                Confirmation a;
                a.Message = String.Format( "WARN - {0} {1}", ConfirmText[i][0], ConfirmText[i][(int)setting] );
                a.DurationS = defaultDurationS;
                ConfirmationList.Add( a );
            }
        }
    }
}
