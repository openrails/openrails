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
