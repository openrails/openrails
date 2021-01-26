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

using GNU.Gettext;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using System;
using System.Collections.Generic;

namespace ORTS.Scripting.Api
{
    public abstract class BrakeController : AbstractScriptClass
    {
        /// <summary>
        /// True if the driver has asked for an emergency braking (push button)
        /// </summary>
        public Func<bool> EmergencyBrakingPushButton;
        /// <summary>
        /// True if the TCS has asked for an emergency braking
        /// </summary>
        public Func<bool> TCSEmergencyBraking;
        /// <summary>
        /// True if the TCS has asked for a full service braking
        /// </summary>
        public Func<bool> TCSFullServiceBraking;
        /// <summary>
        /// True if the driver has pressed the Quick Release button
        /// </summary>
        public Func<bool> QuickReleaseButtonPressed;
        /// <summary>
        /// True if the driver has pressed the Overcharge button
        /// </summary>
        public Func<bool> OverchargeButtonPressed;
        /// <summary>
        /// Main reservoir pressure
        /// </summary>
        public Func<float> MainReservoirPressureBar;
        /// <summary>
        /// Maximum pressure in the brake pipes and the equalizing reservoir
        /// </summary>
        public Func<float> MaxPressureBar;
        /// <summary>
        /// Maximum pressure in the brake pipes when they are overcharged
        /// </summary>
        public Func<float> MaxOverchargePressureBar;
        /// <summary>
        /// Release rate of the equalizing reservoir
        /// </summary>
        public Func<float> ReleaseRateBarpS;
        /// <summary>
        /// Quick release rate of the equalizing reservoir
        /// </summary>
        public Func<float> QuickReleaseRateBarpS;
        /// <summary>
        /// Pressure decrease rate of equalizing reservoir when eliminating overcharge
        /// </summary>
        public Func<float> OverchargeEliminationRateBarpS;
        /// <summary>
        /// Slow application rate of the equalizing reservoir
        /// </summary>
        public Func<float> SlowApplicationRateBarpS;
        /// <summary>
        /// Apply rate of the equalizing reservoir
        /// </summary>
        public Func<float> ApplyRateBarpS;
        /// <summary>
        /// Emergency rate of the equalizing reservoir
        /// </summary>
        public Func<float> EmergencyRateBarpS;
        /// <summary>
        /// Depressure needed in order to obtain the full service braking
        /// </summary>
        public Func<float> FullServReductionBar;
        /// <summary>
        /// Release rate of the equalizing reservoir
        /// </summary>
        public Func<float> MinReductionBar;
        /// <summary>
        /// Current value of the brake controller
        /// </summary>
        public Func<float> CurrentValue;
        /// <summary>
        /// Minimum value of the brake controller
        /// </summary>
        public Func<float> MinimumValue;
        /// <summary>
        /// Maximum value of the brake controller
        /// </summary>
        public Func<float> MaximumValue;
        /// <summary>
        /// Step size of the brake controller
        /// </summary>
        public Func<float> StepSize;
        /// <summary>
        /// State of the brake pressure (1 = increasing, -1 = decreasing)
        /// </summary>
        public Func<float> UpdateValue;
        /// <summary>
        /// Gives the list of notches
        /// </summary>
        public Func<List<MSTSNotch>> Notches;

        /// <summary>
        /// Sets the current value of the brake controller lever
        /// </summary>
        public Action<float> SetCurrentValue;
        /// <summary>
        /// Sets the state of the brake pressure (1 = increasing, -1 = decreasing)
        /// </summary>
        public Action<float> SetUpdateValue;
        /// <summary>
        /// Sets the dynamic brake intervention value
        /// </summary>
        public Action<float> SetDynamicBrakeIntervention;

        /// <summary>
        /// Called once at initialization time.
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        ///
        public abstract void InitializeMoving();
        /// <summary>
        /// Called when starting speed > 0
        /// </summary>
        /// 
        public abstract float Update(float elapsedClockSeconds);
        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        public abstract void UpdatePressure(ref float pressureBar, float elapsedClockSeconds, ref float epPressureBar);
        /// <summary>
        /// Called regularly at every simulator update cycle.
        /// </summary>
        public abstract void UpdateEngineBrakePressure(ref float pressureBar, float elapsedClockSeconds);
        /// <summary>
        /// Called when an event happens (like the alerter button pressed)
        /// </summary>
        /// <param name="evt">The event happened</param>
        public abstract void HandleEvent(BrakeControllerEvent evt);
        /// <summary>
        /// Called when an event happens (like the alerter button pressed)
        /// </summary>
        /// <param name="evt">The event happened</param>
        /// <param name="value">The value assigned to the event (a target for example). May be null.</param>
        public abstract void HandleEvent(BrakeControllerEvent evt, float? value);
        /// <summary>
        /// Called in order to check if the controller is valid
        /// </summary>
        public abstract bool IsValid();
        /// <summary>
        /// Called in order to get a state for the debug overlay
        /// </summary>
        public abstract ControllerState GetState();
        /// <summary>
        /// Called in order to get a state fraction for the debug overlay
        /// </summary>
        /// <returns>The nullable state fraction</returns>
        public abstract float? GetStateFraction();
    }

    public enum BrakeControllerEvent
    {
        /// <summary>
        /// Starts the pressure increase (may have a target value)
        /// </summary>
        StartIncrease,
        /// <summary>
        /// Stops the pressure increase
        /// </summary>
        StopIncrease,
        /// <summary>
        /// Starts the pressure decrease (may have a target value)
        /// </summary>
        StartDecrease,
        /// <summary>
        /// Stops the pressure decrease
        /// </summary>
        StopDecrease,
        /// <summary>
        /// Sets the value of the brake controller using a RailDriver peripheral (must have a value)
        /// </summary>
        SetCurrentPercent,
        /// <summary>
        /// Sets the current value of the brake controller (must have a value)
        /// </summary>
        SetCurrentValue,
        /// <summary>
        /// Starts a full quick brake release.
        /// </summary>
        FullQuickRelease,
        /// <summary>
        /// Starts a pressure decrease to zero (may have a target value)
        /// </summary>
        StartDecreaseToZero
    }

    public enum ControllerState
    {
        // MSTS values (DO NOT CHANGE THE ORDER !)
        Dummy,              // Dummy
        Release,            // TrainBrakesControllerReleaseStart 
        FullQuickRelease,   // TrainBrakesControllerFullQuickReleaseStart
        Running,            // TrainBrakesControllerRunningStart 
        Neutral,            // TrainBrakesControllerNeutralhandleOffStart
        SelfLap,            // TrainBrakesControllerSelfLapStart 
        Lap,                // TrainBrakesControllerHoldLapStart 
        Apply,              // TrainBrakesControllerApplyStart 
        EPApply,            // TrainBrakesControllerEPApplyStart 
        GSelfLap,           // TrainBrakesControllerGraduatedSelfLapLimitedStart
        GSelfLapH,          // TrainBrakesControllerGraduatedSelfLapLimitedHoldStart
        Suppression,        // TrainBrakesControllerSuppressionStart 
        ContServ,           // TrainBrakesControllerContinuousServiceStart 
        FullServ,           // TrainBrakesControllerFullServiceStart 
        Emergency,          // TrainBrakesControllerEmergencyStart

        // Extra MSTS values
        MinimalReduction,  // TrainBrakesControllerMinimalReductionStart,
        Hold,                   // TrainBrakesControllerHoldStart

        // OR values
        StrBrkReleaseOn,      // TrainBrakesControllerStraightBrakingReleaseOnStart
        StrBrkReleaseOff,     // TrainBrakesControllerStraightBrakingReleaseOffStart
        StrBrkRelease,      // TrainBrakesControllerStraightBrakingReleaseStart
        StrBrkLap,          // TrainBrakesControllerStraightBrakingLapStart
        StrBrkApply,        // TrainBrakesControllerStraightBrakingApplyStart
        StrBrkApplyAll,     // TrainBrakesControllerStraightBrakingApplyAllStart
        StrBrkEmergency,    // TrainBrakesControllerStraightBrakingEmergencyStart
        Overcharge,         // Overcharge
        EBPB,               // Emergency Braking Push Button
        TCSEmergency,       // TCS Emergency Braking
        TCSFullServ,        // TCS Full Service Braking
        VacContServ,        // TrainBrakesControllerVacuumContinuousServiceStart
        VacApplyContServ,   // TrainBrakesControllerVacuumApplyContinuousServiceStart
        ManualBraking,      // BrakemanBrakesControllerManualBraking
        BrakeNotch,         // EngineBrakesControllerBrakeNotchStart
        EPOnly,             // TrainBrakesControllerEPOnlyStart
        EPFullServ,         // TrainBrakesControllerEPFullServiceStart
        SlowService,        // TrainBrakesControllerSlowServiceStart
    };

    public static class ControllerStateDictionary
    {
        private static readonly GettextResourceManager Catalog = new GettextResourceManager("Orts.Simulation");

        public static readonly Dictionary<ControllerState, string> Dict = new Dictionary<ControllerState, string>
        {
            {ControllerState.Dummy, ""},
            {ControllerState.Release, Catalog.GetString("Release")},
            {ControllerState.FullQuickRelease, Catalog.GetString("Quick Release")},
            {ControllerState.Running, Catalog.GetParticularString("Brake Controller", "Running")},
            {ControllerState.Neutral, Catalog.GetString("Neutral")},
            {ControllerState.Apply, Catalog.GetString("Apply")},
            {ControllerState.EPApply, Catalog.GetString("EPApply")},
            {ControllerState.Emergency, Catalog.GetString("Emergency")},
            {ControllerState.SelfLap, Catalog.GetString("Self Lap")},
            {ControllerState.GSelfLap, Catalog.GetString("Service")},
            {ControllerState.GSelfLapH, Catalog.GetString("Service")},
            {ControllerState.Lap, Catalog.GetString("Lap")},
            {ControllerState.Suppression, Catalog.GetString("Suppression")},
            {ControllerState.ContServ, Catalog.GetString("Cont. Service")},
            {ControllerState.FullServ, Catalog.GetString("Full Service")},
            {ControllerState.MinimalReduction, Catalog.GetString("Minimum Reduction")},
            {ControllerState.Hold, Catalog.GetString("Hold")},
            {ControllerState.StrBrkReleaseOn, Catalog.GetString("Str. Brk. Release On:")},
            {ControllerState.StrBrkReleaseOff, Catalog.GetString("Str. Brk. Release Off:")},
            {ControllerState.StrBrkRelease, Catalog.GetString("Str. Brk. Release:")},
            {ControllerState.StrBrkLap, Catalog.GetString("Str. Brk. Lap:")},
            {ControllerState.StrBrkApply, Catalog.GetString("Str. Brk. Apply:")},
            {ControllerState.StrBrkApplyAll, Catalog.GetString("Str. Brk. Apply All:")},
            {ControllerState.StrBrkEmergency, Catalog.GetString("Str. Brk. Emerg:")},
            {ControllerState.Overcharge, Catalog.GetString("Overcharge")},
            {ControllerState.EBPB, Catalog.GetString("Emergency Braking Push Button")},
            {ControllerState.TCSEmergency, Catalog.GetString("TCS Emergency Braking")},
            {ControllerState.TCSFullServ, Catalog.GetString("TCS Full Service Braking")},
            {ControllerState.VacContServ, Catalog.GetString("Vac. Cont. Service")},
            {ControllerState.VacApplyContServ, Catalog.GetString("Vac. Apply Cont. Service")},
            {ControllerState.ManualBraking, Catalog.GetString("Manual Braking")},
            {ControllerState.BrakeNotch, Catalog.GetString("Notch")},
            {ControllerState.EPOnly, Catalog.GetString("EP Service")},
            {ControllerState.EPFullServ, Catalog.GetString("EP Full Service")},
            {ControllerState.SlowService, Catalog.GetString("Slow service")}
        };
    }
}
