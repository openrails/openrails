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
using Orts.Common;
using Orts.Simulation;

namespace ORTS.Scripting.Api
{
    public abstract class AbstractScriptClass
    {
        /// <summary>
        /// Clock value (in seconds) for the simulation. Starts with a value = session start time.
        /// </summary>
        public Func<float> ClockTime;
        /// <summary>
        /// Clock value (in seconds) for the simulation. Starts with a value = 0.
        /// </summary>
        public Func<float> GameTime;
        /// <summary>
        /// Running total of distance travelled - always positive, updated by train physics.
        /// </summary>
        public Func<float> DistanceM;
        /// <summary>
        /// Confirms a command done by the player with a pre-set message on the screen.
        /// </summary>
        public Action<CabControl, CabSetting> Confirm;
        /// <summary>
        /// Displays a message on the screen.
        /// </summary>
        public Action<ConfirmLevel, string> Message;
        /// <summary>
        /// Sends an event to the locomotive.
        /// </summary>
        public Action<Event> SignalEvent;
        /// <summary>
        /// Sends an event to the train.
        /// </summary>
        public Action<Event> SignalEventToTrain;
    }

    /// <summary>
    /// Base class for Timer and OdoMeter. Not to be used directly.
    /// </summary>
    public class Counter
    {
        float EndValue;
        protected Func<float> CurrentValue;

        public float AlarmValue { get; private set; }
        public float RemainingValue { get { return EndValue - CurrentValue(); } }
        public bool Started { get; private set; }
        public void Setup(float alarmValue) { AlarmValue = alarmValue; }
        public void Start() { EndValue = CurrentValue() + AlarmValue; Started = true; }
        public void Stop() { Started = false; }
        public bool Triggered { get { return Started && CurrentValue() >= EndValue; } }
    }

    public class Timer : Counter
    {
        public Timer(AbstractScriptClass asc)
        {
            CurrentValue = asc.GameTime;
        }
    }

    public class OdoMeter : Counter
    {
        public OdoMeter(AbstractScriptClass asc)
        {
            CurrentValue = asc.DistanceM;
        }
    }
}
