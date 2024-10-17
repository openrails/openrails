// COPYRIGHT 2009 - 2024 by the Open Rails project.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json.Linq;
using ORTS.Common;

namespace ORTS.Settings
{
    public enum TelemetryType
    {
        System,
    }

    public enum TelemetryState
    {
        Undecided,
        Disabled,
        Enabled,
    }

    public class TelemetryManager
    {
        // The enabled state MUST be last chronologically; all dates here MUST be before expected current dates
        internal static readonly DateTime StateUndecided = new DateTime(1899, 1, 1);
        internal static readonly DateTime StateDisabled = new DateTime(1899, 1, 2);
        internal static readonly DateTime StateEnabledMin = new DateTime(1900, 1, 1);

        static readonly Dictionary<TelemetryType, TimeSpan> Frequency = new Dictionary<TelemetryType, TimeSpan>
        {
            { TelemetryType.System, TimeSpan.FromDays(7) },
        };

        public readonly TelemetrySettings Settings;

        readonly SavingProperty<int> RandomNumber1000;
        readonly Dictionary<TelemetryType, SavingProperty<DateTime>> State = new Dictionary<TelemetryType, SavingProperty<DateTime>>();

        public TelemetryManager(TelemetrySettings settings)
        {
            Settings = settings;
            RandomNumber1000 = settings.GetSavingProperty<int>("RandomNumber1000");
            foreach (var type in Enum.GetValues(typeof(TelemetryType)))
            {
                State[(TelemetryType)type] = settings.GetSavingProperty<DateTime>($"State{Enum.GetName(typeof(TelemetryType), type)}");
            }

            // One-time set up of the random number - this determines which telemetry types are asked for
            if (RandomNumber1000.Value < 1 || RandomNumber1000.Value > 1000)
            {
                RandomNumber1000.Value = new Random().Next(1, 1001);
            }
        }

        /// <summary>
        /// Returns the state (undecided/disabled/enabled) of the specified telemetry type.
        /// </summary>
        /// <param name="type">Which telemetry type to check</param>
        /// <returns>Enumerated value indicating the state</returns>
        public TelemetryState GetState(TelemetryType type)
        {
            Debug.Assert(State.ContainsKey(type), "Telemetry type is not valid");
            if (State[type].Value >= StateEnabledMin) return TelemetryState.Enabled;
            if (State[type].Value == StateDisabled) return TelemetryState.Disabled;
            return TelemetryState.Undecided;
        }

        /// <summary>
        /// Sets the specified telemetry type to a specific state. Does not reset due date unless changing state.
        /// </summary>
        /// <param name="type">Which telemetry type to set</param>
        /// <param name="state">Which telemetry state to set</param>
        public void SetState(TelemetryType type, TelemetryState state)
        {
            Debug.Assert(State.ContainsKey(type), "Telemetry type is not valid");
            if (state != GetState(type))
            {
                State[type].Value = state == TelemetryState.Enabled ? StateEnabledMin : state == TelemetryState.Disabled ? StateDisabled : StateUndecided;
            }
        }
    }
}
