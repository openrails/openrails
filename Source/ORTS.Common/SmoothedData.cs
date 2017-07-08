// COPYRIGHT 2010, 2011 by the Open Rails project.
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
using System.Linq;
using System.Text;

namespace ORTS.Common
{
    public class SmoothedData
    {
        public readonly float SmoothPeriodS = 3;
        protected float value = float.NaN;
        protected float smoothedValue = float.NaN;

        public SmoothedData()
        {
        }

        public SmoothedData(float smoothPeriodS)
            : this()
        {
            SmoothPeriodS = smoothPeriodS;
        }

        public void Update(float periodS, float newValue)
        {
            if (periodS < float.Epsilon)
            {
                if (float.IsNaN(smoothedValue) || float.IsInfinity(smoothedValue))
                    smoothedValue = newValue;
                return;
            }

            value = newValue;
            SmoothValue(ref smoothedValue, periodS, newValue);
        }

        protected void SmoothValue(ref float smoothedValue, float periodS, float newValue)
        {
            var rate = SmoothPeriodS / periodS;
            if (float.IsNaN(smoothedValue) || float.IsInfinity(smoothedValue))
                smoothedValue = newValue;
            else if (rate < 1)
                smoothedValue = newValue;
            else
                smoothedValue = (smoothedValue * (rate - 1) + newValue) / rate;
        }

        public void ForceSmoothValue(float forcedValue)
        {
            smoothedValue = forcedValue;
        }

        public float Value { get { return value; } }
        public float SmoothedValue { get { return smoothedValue; } }
    }

    public class SmoothedDataWithPercentiles : SmoothedData
    {
        const int HistoryStepCount = 40; // 40 units (i.e. 10 seconds)
        const float HistoryStepSize = 0.25f; // each unit = 1/4 second

        List<float>[] history = new List<float>[HistoryStepCount];
        float position;
        float smoothedP50 = float.NaN;
        float smoothedP95 = float.NaN;
        float smoothedP99 = float.NaN;

        public float P50 { get; private set; }
        public float P95 { get; private set; }
        public float P99 { get; private set; }

        public float SmoothedP50 { get { return smoothedP50; } }
        public float SmoothedP95 { get { return smoothedP95; } }
        public float SmoothedP99 { get { return smoothedP99; } }

        public float SmoothedP95PCFromP50 { get; private set; }
        public float SmoothedP99PCFromP50 { get; private set; }

        public SmoothedDataWithPercentiles()
            : base()
        {
            for (var i = 0; i < HistoryStepCount; i++)
                history[i] = new List<float>();
        }

        public new void Update(float periodS, float newValue)
        {
            base.Update(periodS, newValue);

            history[0].Add(newValue);
            position += periodS;

            if (position >= HistoryStepSize)
            {
                var samples = new List<float>();
                foreach (var h in history)
                    samples.AddRange(h);
                samples.Sort();

                P50 = samples[(int)(samples.Count * 0.50f)];
                P95 = samples[(int)(samples.Count * 0.95f)];
                P99 = samples[(int)(samples.Count * 0.99f)];

                SmoothValue(ref smoothedP50, position, P50);
                SmoothValue(ref smoothedP95, position, P95);
                SmoothValue(ref smoothedP99, position, P99);

                SmoothedP95PCFromP50 = 100f * (smoothedP95 - smoothedP50) / smoothedP50;
                SmoothedP99PCFromP50 = 100f * (smoothedP99 - smoothedP50) / smoothedP50;

                var historyWrap = history[HistoryStepCount - 1];
                for (var i = HistoryStepCount - 1; i > 0; i--)
                    history[i] = history[i - 1];
                history[0] = historyWrap;
                history[0].Clear();
                position = 0;
            }
        }
    }
}
