// COPYRIGHT 2012, 2013 by the Open Rails project.
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

using ORTS.Common;
using System;
using System.Diagnostics;

namespace Orts.Simulation
{
    public enum ConfirmLevel
    {
        [GetString("None")] None,
        [GetString("Information")] Information,
        [GetString("Warning")] Warning,
        [GetString("Error")] Error,
		[GetString("MSG")] MSG,
    };

    // <CJComment> Some of these are not cab controls or even controls. However they all make good use of structured text. </CJComment>
    public enum CabControl {
        None
        // Power
      , Reverser
      , Throttle
      , Wheelslip
        // Electric Power
      , Power
      , Pantograph1
      , Pantograph2
      , Pantograph3
      , Pantograph4
      , CircuitBreakerClosingOrder
      , CircuitBreakerOpeningOrder
      , CircuitBreakerClosingAuthorization
        // Diesel Power
      , PlayerDiesel
      , HelperDiesel
      , DieselFuel
      , SteamHeatBoilerWater
      , TractionCutOffRelayClosingOrder
      // Steam power
      , SteamLocomotiveReverser
      , Regulator
      , Injector1
      , Injector2
      , BlowdownValve
      , Blower
      , SteamHeat
      , Damper
      , FireboxDoor
      , FiringRate
      , FiringIsManual
      , FireShovelfull
      , CylinderCocks
      , SteamBoosterAir
      , SteamBoosterIdle
      , SteamBoosterLatch
      , CylinderCompound
      , LargeEjector
      , SmallEjector
      , VacuumExhauster
      , TenderCoal
      , TenderWater
      // General
      , WaterScoop
      // Braking
      , TrainBrake
      , EngineBrake
      , BrakemanBrake
      , DynamicBrake
      , EmergencyBrake
      , BailOff
      , InitializeBrakes
      , Handbrake
      , Retainers
      , BrakeHose
      , QuickRelease
      , Overcharge
      // Cab Devices
      , Sander
      , Alerter
      , Horn
      , Whistle
      , Bell
      , Headlight
      , CabLight
      , Wipers
      , ChangeCab
      , Odometer
      , Battery
      , MasterKey
      // Cruise control
      , MaxAcceleration
      , RestrictedSpeedZone
      // Train Devices
      , DoorsLeft
      , DoorsRight
      , Mirror
      , WindowLeft
      , WindowRight
      // Track Devices
      , SwitchAhead
      , SwitchBehind
      // Simulation
      , SimulationSpeed
      , Uncouple
      , Activity
      , Replay
      , GearBox
      , SignalMode
      // Freight Load
      , FreightLoad
      , CabRadio
    }

    public enum CabSetting {
        Name        // name of control
        , Off       // 2 or 3 state control/reset/initialise
        , Neutral   // 2 or 3 state control
        , On        // 2 or 3 state control/apply/change
        , Decrease  // continuous control
        , Increase  // continuous control
        , Warn1
        , Warn2
        , Range1    // sub-range
        , Range2
        , Range3
        , Range4
    }

    public class DisplayMessageEventArgs : EventArgs
    {
        public readonly string Key;
        public readonly string Text;
        public readonly double Duration;

        public DisplayMessageEventArgs(string key, string text, double duration)
        {
            Key = key;
            Text = text;
            Duration = duration;
        }
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
        readonly string[][] ConfirmText; 

        readonly Simulator Simulator;
        readonly double DefaultDurationS;

        public event System.EventHandler PlayErrorSound;
        public event EventHandler<DisplayMessageEventArgs> DisplayMessage;

        public Confirmer(Simulator simulator, double defaultDurationS)
        {
            Simulator = simulator;
            DefaultDurationS = defaultDurationS;

            Func<string, string> GetString = (value) => Simulator.Catalog.GetString(value);
            Func<string, string, string> GetParticularString = (context, value) => Simulator.Catalog.GetParticularString(context, value);

            // The following list needs to be in the same order as the list above under CabControl
            ConfirmText = new string[][] {
                new string [] { GetString("<none>") } 
                // Power
                , new string [] { GetParticularString("NonSteam", "Reverser"), GetString("reverse"), GetString("neutral"), GetString("forward"), null, null, GetString("locked. Close throttle, release dynamic brake, stop train then re-try.") } 
                , new string [] { GetString("Throttle"), null, null, null, GetString("close"), GetString("open"), GetString("locked. Release dynamic brake then re-try.") } 
                , new string [] { GetString("Wheel-slip"), GetString("over"), null, GetString("occurring. Tractive power greatly reduced."), null, null, GetString("warning") } 
                // Electric power
                , new string [] { GetString("Power"), GetString("off"), null, GetString("on") }
                , new string [] { GetString("Pantograph 1"), GetString("lower"), null, GetString("raise") } 
                , new string [] { GetString("Pantograph 2"), GetString("lower"), null, GetString("raise") }
                , new string [] { GetString("Pantograph 3"), GetString("lower"), null, GetString("raise") }
                , new string [] { GetString("Pantograph 4"), GetString("lower"), null, GetString("raise") }
                , new string [] { GetString("Circuit breaker"), GetString("open"), null, GetString("close") }
                , new string [] { GetString("Circuit breaker"), GetString("close"), null, GetString("open") }
                , new string [] { GetString("Circuit breaker closing authorization"), GetString("remove"), null, GetString("give") }
                // Diesel power
                , new string [] { GetString("Player Diesel Power"), GetString("off"), null, GetString("on"), null, null, GetString("locked. Close throttle then re-try.") }
                , new string [] { GetString("Helper Diesel Power"), GetString("off"), null, GetString("on") }
                , new string [] { GetString("Diesel Tank"), null, null, GetString("re-fueled"), null, GetString("level") }
                , new string [] { GetString("Boiler Water Tank"), null, null, GetString("re-fueled"), null, GetString("level") }
                , new string [] { GetString("Traction cut-off relay"), GetString("open"), null, GetString("close") }
                // Steam power
                , new string [] { GetParticularString("Steam", "Reverser"), GetString("reverse"), GetString("neutral"), GetString("forward"), null, null, GetString("locked. Close throttle, stop train then re-try.") } 
                , new string [] { GetString("Regulator"), null, null, null, GetString("close"), GetString("open") }    // Throttle for steam locomotives
                , new string [] { GetString("Injector 1"), GetString("off"), null, GetString("on"), GetString("close"), GetString("open") } 
                , new string [] { GetString("Injector 2"), GetString("off"), null, GetString("on"), GetString("close"), GetString("open") }
                , new string [] { GetString("Blowdown Valve"), GetString("close"), null, GetString("open") }
                , new string [] { GetString("Blower"), null, null, null, GetString("decrease"), GetString("increase") } 
                , new string [] { GetString("SteamHeat"), null, null, null, GetString("decrease"), GetString("increase") } 
                , new string [] { GetString("Damper"), null, null, null, GetString("close"), GetString("open") } 
                , new string [] { GetString("Firebox Door"), null, null, null, GetString("close"), GetString("open") }
                , new string [] { GetString("Firing Rate"), null, null, null, GetString("decrease"), GetString("increase") } 
                , new string [] { GetString("Manual Firing"), GetString("off"), null, GetString("on") } 
                , new string [] { GetString("Fire"), null, null, GetString("add shovel-full") }
                , new string [] { GetString("Cylinder Cocks"), GetString("close"), null, GetString("open") }
                , new string [] { GetString("Steam Booster Air Valve"), GetString("close"), null, GetString("open") }
                , new string [] { GetString("Steam Booster Idle Valve"), GetString("idle"), null, GetString("run") }
                , new string [] { GetString("Steam Booster Latch"), GetString("opened"), null, GetString("locked") }
                , new string [] { GetString("Cylinder Compound"), GetString("close"), null, GetString("open") }
                , new string [] { GetString("LargeEjector"), null, null, null, GetString("decrease"), GetString("increase") }
                , new string [] { GetString("SmallEjector"), null, null, null, GetString("decrease"), GetString("increase") }
                , new string [] { GetString("VacuumExhauster"), GetString("normal"), null, GetString("fast") }
                , new string [] { GetString("Tender"), null, null, GetString("Coal re-filled"), null, GetString("Coal level") } 
                , new string [] { GetString("Tender"), null, null, GetString("Water re-filled"), null, GetString("Water level") }
                // General
                , new string [] { GetString("Water Scoop"), GetString("up"), null, GetString("down") }
                // Braking
                , new string [] { GetString("Train Brake"), null, null, null, GetString("release"), GetString("apply") } 
                , new string [] { GetString("Engine Brake"), null, null, null, GetString("release"), GetString("apply") }
                , new string [] { GetString("Brakeman Brake"), null, null, null, GetString("release"), GetString("apply") }
                , new string [] { GetString("Dynamic Brake"), GetString("off"), null, GetString("setup"), GetString("decrease"), GetString("increase"), GetString("locked. Move reverser then retry.") }
                , new string [] { GetString("Emergency Brake"), GetString("release"), null, GetString("apply") } 
                , new string [] { GetString("Bail Off"), GetString("disengage"), null, GetString("engage") } 
                , new string [] { GetString("Brakes"), GetString("initialize"), null, null, null, null, GetString("cannot initialize. Stop train then re-try.") } 
                , new string [] { GetString("Handbrake"), GetString("none"), null, GetString("full") } 
                , new string [] { GetString("Retainers"), GetString("off"), null, GetString("on"), null, null, null, null, GetString("Exhaust"), GetString("High Pressure"), GetString("Low Pressure"), GetString("Slow Direct") } 
                , new string [] { GetString("Brake Hose"), GetString("disconnect"), null, GetString("connect") }
                , new string [] { GetString("Quick Release"), GetString("off"), null, GetString("on") }
                , new string [] { GetString("Overcharge"), GetString("off"), null, GetString("on") }
                // Cab Devices
                , new string [] { GetString("Sander"), GetString("off"), null, GetString("on") } 
                , new string [] { GetString("Alerter"), GetString("acknowledge"), null, GetParticularString("Alerter", "sound") } 
                , new string [] { GetString("Horn"), GetString("off"), null, GetParticularString("Horn", "sound") } 
                , new string [] { GetString("Whistle"), GetString("off"), null, GetString("blow") }        // Horn for steam locomotives
                , new string [] { GetString("Bell"), GetString("off"), null, GetString("ring") } 
                , new string [] { GetString("Headlight"), GetString("off"), GetString("dim"), GetString("bright") } 
                , new string [] { GetString("Cab Light"), GetString("off"), null, GetString("on") } 
                , new string [] { GetString("Wipers"), GetString("off"), null, GetString("on") }
                , new string [] { GetString("Cab"), null, null, GetParticularString("Cab", "change"), null, null, GetString("changing is not available"), GetString("changing disabled. Close throttle, set reverser to neutral, stop train then re-try.") } 
                , new string [] { GetString("Odometer"), null, null, GetParticularString("Odometer", "reset"), GetParticularString("Odometer", "counting down"), GetParticularString("Odometer", "counting up") }
                , new string [] { GetString("Battery"), GetString("off"), null, GetString("on") }
                , new string [] { GetString("Master key"), GetString("off"), null, GetString("on") }
                // Cruise control
                , new string [] { GetString("Max acceleration"), null, null, null }
                , new string [] { GetString("Restricted speed zone"), GetString("off"), null, GetString("on") }
                // Train Devices
                , new string [] { GetString("Doors Left"), GetString("close"), null, GetString("open") } 
                , new string [] { GetString("Doors Right"), GetString("close"), null, GetString("open") } 
                , new string [] { GetString("Mirror"), GetString("retract"), null, GetString("extend") }
                , new string [] { GetString("Window Left"), GetString("closing"), null, GetString("opening") }
                , new string [] { GetString("Window Right"), GetString("closing"), null, GetString("opening") }
                // Track Devices
                , new string [] { GetString("Switch Ahead"), null, null, GetParticularString("Switch", "change"), null, null, GetString("locked. Use Control+M to change signals to manual mode then re-try.") } 
                , new string [] { GetString("Switch Behind"), null, null, GetParticularString("Switch", "change"), null, null, GetString("locked. Use Control+M to change signals to manual mode then re-try.") } 
                // Simulation
                , new string [] { GetString("Simulation Speed"), GetString("reset"), null, null, GetString("decrease"), GetString("increase") } 
                , new string [] { GetString("Uncouple After") } 
                , new string [] { GetString("Activity"), GetString("quit"), null, GetString("resume") } 
                , new string [] { GetString("Replay"), null, null, null, null, null, GetString("Overriding camera replay. Press Escape to resume camera replay.") } 
                , new string [] { GetString("Gearbox"), null, null, null, GetString("down"), GetString("up"), GetString("locked. Use shaft before changing gear.") } 
                , new string [] { GetString("Signal mode"), GetString("manual"), null, GetString("auto"), null, null, GetString("locked. Stop train, then re-try.") } 
                // Freight Load
                , new string [] { GetString("Wagon"), GetString("Wagon fully unloaded"), null, GetString("Wagon fully loaded"), null, GetString("Freight load") }

                , new string [] { GetString("Cab Radio"), GetString("off"), null, GetString("on") }

            };
            Debug.Assert(ConfirmText.Length == Enum.GetNames(typeof(CabControl)).Length, "Number of entries indexer ConfirmText must match values in CabControl enum.");
        }

        #region Control confirmation

        public void Confirm(CabControl control, string text)
        {
            Message(control, Simulator.Catalog.GetString("{0} {1}"), ConfirmText[(int)control][0], text);
        }

        public void Confirm( CabControl control, CabSetting setting ) {
            Message(control, Simulator.Catalog.GetString("{0}"), ConfirmText[(int)control][(int)setting]);
        }

        public void Confirm(CabControl control, CabSetting setting, string text)
        {
            Message(control, Simulator.Catalog.GetString("{0} {1}"), ConfirmText[(int)control][(int)setting], text);
        }

        public void ConfirmWithPerCent(CabControl control, CabSetting setting, float perCent)
        {
            Message(control, Simulator.Catalog.GetString("{0} to {1:0}%"), ConfirmText[(int)control][(int)setting], perCent);
        }

        public void ConfirmWithPerCent(CabControl control, CabSetting setting1, float perCent, int setting2)
        {
            Message(control, Simulator.Catalog.GetString("{0} {1:0}% {2}"), ConfirmText[(int)control][(int)setting1], perCent, ConfirmText[(int)control][setting2]);
        }

        public void ConfirmWithPerCent(CabControl control, float perCent, CabSetting setting)
        {
            Message(control, Simulator.Catalog.GetString("{0:0}% {1}"), perCent, ConfirmText[(int)control][(int)setting]);
        }

        public void ConfirmWithPerCent(CabControl control, float perCent)
        {
            Message(control, Simulator.Catalog.GetString("{0:0}%"), perCent);
        }

        #endregion
        #region Control updates

        public void UpdateWithPerCent(CabControl control, int action, float perCent)
        {
            Message(control, Simulator.Catalog.GetString("{0} {1:0}%"), ConfirmText[(int)control][action], perCent);
        }

        public void UpdateWithPerCent(CabControl control, CabSetting setting, float perCent)
        {
            Message(control, Simulator.Catalog.GetString("{0} {1:0}%"), ConfirmText[(int)control][(int)setting], perCent);
        }

        public void Update(CabControl control, CabSetting setting, string text)
        {
            Message(control, Simulator.Catalog.GetString("{0} {1}"), ConfirmText[(int)control][(int)setting], text);
        }

        #endregion
        #region Control messages

        public void Message(CabControl control, string format, params object[] args)
        {
            Message(control, ConfirmLevel.None, String.Format(format, args));
        }

        public void Warning(CabControl control, CabSetting setting)
        {
            if (PlayErrorSound != null) PlayErrorSound(this, EventArgs.Empty);
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
            // Suppress control messages
            //
            // Cab control confirmations have level None. They are suppressed 
            //      both with Information messages when suppress level is Information
            // Control messages with level MSG are never suppressed
            switch (Simulator.Settings.SuppressConfirmations)
            {
                case (int)ConfirmLevel.None:
                    break;
                case (int)ConfirmLevel.Information:
                    if (level <= ConfirmLevel.Information)
                        return;
                    break;
                case (int)ConfirmLevel.Warning:
                    if (level <= ConfirmLevel.Warning)
                        return;
                    break;
                case (int)ConfirmLevel.Error:
                    if (level <= ConfirmLevel.Error)
                        return;
                    break;
                default:
                    break;
            }

            var format = "{2}";

            // Skip control name if not a control
            if (control != CabControl.None)
                format = "{0}: " + format;

            if (level >= ConfirmLevel.Information)
                format = "{1} - " + format;

            // message displays longer if more severe
			var duration = DefaultDurationS;
			if (level >= ConfirmLevel.Warning) duration *= 2;
			if (level >= ConfirmLevel.MSG) duration *= 5;

            if (DisplayMessage != null)
                DisplayMessage(
                    this, 
                    new DisplayMessageEventArgs(
                        key: String.Format("{0}/{1}", control, level), 
                        text: String.Format(
                            format, 
                            ConfirmText[(int)control][0], 
                            Simulator.Catalog.GetString(GetStringAttribute.GetPrettyName(level)), message), 
                        duration));
        }
    }
}
