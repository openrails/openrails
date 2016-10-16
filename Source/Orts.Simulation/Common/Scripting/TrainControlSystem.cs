// COPYRIGHT 2014 by the Open Rails project.
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

using System;
using ORTS.Common;

namespace ORTS.Scripting.Api
{
    public abstract class TrainControlSystem : AbstractScriptClass
    {
        public bool Activated { get; set; }

        /// <summary>
        /// True if train control is switched on (the locomotive is the lead locomotive and the train is not autopiloted).
        /// </summary>
        public Func<bool> IsTrainControlEnabled;
        /// <summary>
        /// True if vigilance monitor was switched on in game options.
        /// </summary>
        public Func<bool> IsAlerterEnabled;
        /// <summary>
        /// True if speed control was switched on in game options.
        /// </summary>
        public Func<bool> IsSpeedControlEnabled;
        /// <summary>
        /// True if alerter sound rings, otherwise false
        /// </summary>
        public Func<bool> AlerterSound;
        /// <summary>
        /// Max allowed speed for the train determined by consist.
        /// </summary>
        public Func<float> TrainSpeedLimitMpS;
        /// <summary>
        /// Max allowed speed determined by current signal.
        /// </summary>
        public Func<float> CurrentSignalSpeedLimitMpS;
        /// <summary>
        /// Max allowed speed determined by next signal.
        /// </summary>
        public Func<int, float> NextSignalSpeedLimitMpS;
        /// <summary>
        /// Aspect of the next signal.
        /// </summary>
        public Func<int, Aspect> NextSignalAspect;
        /// <summary>
        /// Distance to next signal.
        /// </summary>
        public Func<int, float> NextSignalDistanceM;
        /// <summary>
        /// Max allowed speed determined by current speedpost.
        /// </summary>
        public Func<float> CurrentPostSpeedLimitMpS;
        /// <summary>
        /// Max allowed speed determined by next speedpost.
        /// </summary>
        public Func<int, float> NextPostSpeedLimitMpS;
        /// <summary>
        /// Distance to next speedpost.
        /// </summary>
        public Func<int, float> NextPostDistanceM;
        /// <summary>
        /// Train's length
        /// </summary>
        public Func<float> TrainLengthM;
        /// <summary>
        /// Train's actual absolute speed.
        /// </summary>
        public Func<float> SpeedMpS;
        /// <summary>
        /// Train's direction.
        /// </summary>
        public Func<Direction> CurrentDirection;
        /// <summary>
        /// True if train direction is forward.
        /// </summary>
        public Func<bool> IsDirectionForward;
        /// <summary>
        /// True if train direction is neutral.
        /// </summary>
        public Func<bool> IsDirectionNeutral;
        /// <summary>
        /// True if train direction is reverse.
        /// </summary>
        public Func<bool> IsDirectionReverse;
        /// <summary>
        /// True if train brake controller is in emergency position, otherwise false.
        /// </summary>
        public Func<bool> IsBrakeEmergency;
        /// <summary>
        /// True if train brake controller is in full service position, otherwise false.
        /// </summary>
        public Func<bool> IsBrakeFullService;
        /// <summary>
        /// True if circuit breaker or power contactor closing authorization is true.
        /// </summary>
        public Func<bool> PowerAuthorization;
        /// <summary>
        /// True if circuit breaker or power contactor closing order is true.
        /// </summary>
        public Func<bool> CircuitBreakerClosingOrder;
        /// <summary>
        /// True if circuit breaker or power contactor opening order is true.
        /// </summary>
        public Func<bool> CircuitBreakerOpeningOrder;
        /// <summary>
        /// True if traction is authorized.
        /// </summary>
        public Func<bool> TractionAuthorization;
        /// <summary>
        /// Train brake pipe pressure. Returns float.MaxValue if no data is available.
        /// </summary>
        public Func<float> BrakePipePressureBar;
        /// <summary>
        /// Locomotive brake cylinder pressure. Returns float.MaxValue if no data is available.
        /// </summary>
        public Func<float> LocomotiveBrakeCylinderPressureBar;
        /// <summary>
        /// True if power must be cut if the brake is applied.
        /// </summary>
        public Func<bool> DoesBrakeCutPower;
        /// <summary>
        /// Train brake pressure value which triggers the power cut-off.
        /// </summary>
        public Func<float> BrakeCutsPowerAtBrakeCylinderPressureBar;

        // TODO: The following will be available in .NET 4 as normal Func:
        public delegate TResult Func5<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

        /// <summary>
        /// (float targetDistanceM, float targetSpeedMpS, float slope, float delayS, float decelerationMpS2)
        /// Returns a speed curve based speed limit, unit is m/s
        /// </summary>
        public Func5<float, float, float, float, float, float> SpeedCurve;
        /// <summary>
        /// (float currentSpeedMpS, float targetSpeedMpS, float slope, float delayS, float decelerationMpS2)
        /// Returns a distance curve based safe braking distance, unit is m
        /// </summary>
        public Func5<float, float, float, float, float, float> DistanceCurve;
        /// <summary>
        /// (float currentSpeedMpS, float targetSpeedMpS, float distanceM)
        /// Returns the deceleration needed to decrease the speed to the target speed at the target distance
        /// </summary>
        public Func<float, float, float, float> Deceleration;

        /// <summary>
        /// Set train brake controller to full service position.
        /// </summary>
        public Action<bool> SetFullBrake;
        /// <summary>
        /// Set emergency braking on or off.
        /// </summary>
        public Action<bool> SetEmergencyBrake;
        /// <summary>
        /// Set throttle controller to position in range [0-1].
        /// </summary>
        public Action<float> SetThrottleController;
        /// <summary>
        /// Set dynamic brake controller to position in range [0-1].
        /// </summary>
        public Action<float> SetDynamicBrakeController;
        /// <summary>
        /// Cut power by pull all pantographs down.
        /// </summary>
        public Action SetPantographsDown;
        /// <summary>
        /// Set the circuit breaker or power contactor closing authorization.
        /// </summary>
        public Action<bool> SetPowerAuthorization;
        /// <summary>
        /// Set the circuit breaker or power contactor closing order.
        /// </summary>
        public Action<bool> SetCircuitBreakerClosingOrder;
        /// <summary>
        /// Set the circuit breaker or power contactor opening order.
        /// </summary>
        public Action<bool> SetCircuitBreakerOpeningOrder;
        /// <summary>
        /// Set the traction authorization.
        /// </summary>
        public Action<bool> SetTractionAuthorization;
        /// <summary>
        /// Switch vigilance alarm sound on (true) or off (false).
        /// </summary>
        public Action<bool> SetVigilanceAlarm;
        /// <summary>
        /// Set horn on (true) or off (false).
        /// </summary>
        public Action<bool> SetHorn;
        /// <summary>
        /// Trigger Alert1 sound event
        /// </summary>
        public Action TriggerSoundAlert1;
        /// <summary>
        /// Trigger Alert2 sound event
        /// </summary>
        public Action TriggerSoundAlert2;
        /// <summary>
        /// Trigger Info1 sound event
        /// </summary>
        public Action TriggerSoundInfo1;
        /// <summary>
        /// Trigger Info2 sound event
        /// </summary>
        public Action TriggerSoundInfo2;
        /// <summary>
        /// Trigger Penalty1 sound event
        /// </summary>
        public Action TriggerSoundPenalty1;
        /// <summary>
        /// Trigger Penalty2 sound event
        /// </summary>
        public Action TriggerSoundPenalty2;
        /// <summary>
        /// Trigger Warning1 sound event
        /// </summary>
        public Action TriggerSoundWarning1;
        /// <summary>
        /// Trigger Warning2 sound event
        /// </summary>
        public Action TriggerSoundWarning2;
        /// <summary>
        /// Trigger Activate sound event
        /// </summary>
        public Action TriggerSoundSystemActivate;
        /// <summary>
        /// Trigger Deactivate sound event
        /// </summary>
        public Action TriggerSoundSystemDeactivate;
        /// <summary>
        /// Set ALERTER_DISPLAY cabcontrol display's alarm state on or off.
        /// </summary>
        public Action<bool> SetVigilanceAlarmDisplay;
        /// <summary>
        /// Set ALERTER_DISPLAY cabcontrol display's emergency state on or off.
        /// </summary>
        public Action<bool> SetVigilanceEmergencyDisplay;
        /// <summary>
        /// Set OVERSPEED cabcontrol display on or off.
        /// </summary>
        public Action<bool> SetOverspeedWarningDisplay;
        /// <summary>
        /// Set PENALTY_APP cabcontrol display on or off.
        /// </summary>
        public Action<bool> SetPenaltyApplicationDisplay;
        /// <summary>
        /// Monitoring status determines the colors speeds displayed with. (E.g. circular speed gauge).
        /// </summary>
        public Action<MonitoringStatus> SetMonitoringStatus;
        /// <summary>
        /// Set current speed limit of the train, as to be shown on SPEEDLIMIT cabcontrol.
        /// </summary>
        public Action<float> SetCurrentSpeedLimitMpS;
        /// <summary>
        /// Set speed limit of the next signal, as to be shown on SPEEDLIM_DISPLAY cabcontrol.
        /// </summary>
        public Action<float> SetNextSpeedLimitMpS;
        /// <summary>
        /// The speed at the train control system applies brake automatically.
        /// Determines needle color (orange/red) on circular speed gauge, when the locomotive
        /// already runs above the permitted speed limit. Otherwise is unused.
        /// </summary>
        public Action<float> SetInterventionSpeedLimitMpS;
        /// <summary>
        /// Will be whown on ASPECT_DISPLAY cabcontrol.
        /// </summary>
        public Action<Aspect> SetNextSignalAspect;
        /// <summary>
        /// Get bool parameter in the INI file.
        /// </summary>
        public Func<string, string, bool, bool> GetBoolParameter;
        /// <summary>
        /// Get int parameter in the INI file.
        /// </summary>
        public Func<string, string, int, int> GetIntParameter;
        /// <summary>
        /// Get int parameter in the INI file.
        /// </summary>
        public Func<string, string, float, float> GetFloatParameter;
        /// <summary>
        /// Get string parameter in the INI file.
        /// </summary>
        public Func<string, string, string, string> GetStringParameter;

        /// <summary>
        /// Called once at initialization time.
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        public abstract void Update();
        /// <summary>
        /// Called when an event happens (like the alerter button pressed)
        /// </summary>
        /// <param name="evt">The event happened</param>
        /// <param name="message">The message the event wants to communicate. May be empty.</param>
        public abstract void HandleEvent(TCSEvent evt, string message);
        /// <summary>
        /// Called by signalling code externally to stop the train in certain circumstances.
        /// </summary>
        public abstract void SetEmergency(bool emergency);
    }

    // Represents the same enum as TrackMonitorSignalAspect
    /// <summary>
    /// A signal aspect, as shown on track monitor
    /// </summary>
    public enum Aspect
    {
        None,
        Clear_2,
        Clear_1,
        Approach_3,
        Approach_2,
        Approach_1,
        Restricted,
        StopAndProceed,
        Stop,
        Permission,
    }

    public enum TCSEvent
    {
        /// <summary>
        /// Reset request by pressing the alerter button.
        /// </summary>
        AlerterPressed,
        /// <summary>
        /// Alerter button was released.
        /// </summary>
        AlerterReleased,
        /// <summary>
        /// Internal reset request by touched systems other than the alerter button.
        /// </summary>
        AlerterReset,
        /// <summary>
        /// Internal reset request by the reverser.
        /// </summary>
        ReverserChanged,
        /// <summary>
        /// Internal reset request by the throttle controller.
        /// </summary>
        ThrottleChanged,
        /// <summary>
        /// Internal reset request by the gear box controller.
        /// </summary>
        GearBoxChanged,
        /// <summary>
        /// Internal reset request by the train brake controller.
        /// </summary>
        TrainBrakeChanged,
        /// <summary>
        /// Internal reset request by the engine brake controller.
        /// </summary>
        EngineBrakeChanged,
        /// <summary>
        /// Internal reset request by the dynamic brake controller.
        /// </summary>
        DynamicBrakeChanged,
        /// <summary>
        /// Internal reset request by the horn handle.
        /// </summary>
        HornActivated,
    }

    /// <summary>
    /// Controls what color the speed monitoring display uses.
    /// </summary>
    public enum MonitoringStatus
    {
        /// <summary>
        /// Grey color. No speed restriction is ahead.
        /// </summary>
        Normal,
        /// <summary>
        /// White color. Pre-indication, that the next signal is restricted. No manual intervention is needed yet.
        /// </summary>
        Indication,
        /// <summary>
        /// Yellow color. Next signal is restricted, driver should start decreasing speed.
        /// (Please note, it is not for indication of a "real" overspeed. In this state the locomotive still runs under the actual permitted speed.)
        /// </summary>
        Overspeed,
        /// <summary>
        /// Orange color. The locomotive is very close to next speed restriction, driver should start strong braking immediately.
        /// </summary>
        Warning,
        /// <summary>
        /// Red color. Train control system intervention speed. Computer has to apply full service or emergency brake to maintain speed restriction.
        /// </summary>
        Intervention,
    }
}
