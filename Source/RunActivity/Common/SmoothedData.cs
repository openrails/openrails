using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
	public class SmoothedData
	{
		public readonly float SmoothPeriodS = 3;
		float value = float.NaN;
		float smoothedValue = float.NaN;

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
				return;

			var rate = SmoothPeriodS / periodS;
			value = newValue;
            if (float.IsNaN(smoothedValue) || float.IsInfinity(smoothedValue))
                smoothedValue = value;
            else if (rate < 1)
                smoothedValue = value;
            else
				smoothedValue = (smoothedValue * (rate - 1) + value) / rate;
		}

		public float Value { get { return value; } }
		public float SmoothedValue { get { return smoothedValue; } }
	}
}
