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
using System.Collections.Generic;

namespace Tests
{
    public class DynamicPrecisionEqualityComparer : IEqualityComparer<double>
    {
        public static readonly IEqualityComparer<double> Float = new DynamicPrecisionEqualityComparer(6);
        public static readonly IEqualityComparer<double> Double = new DynamicPrecisionEqualityComparer(14);

        readonly int DynamicPrecision;

        DynamicPrecisionEqualityComparer(int dynamicPrecision)
        {
            DynamicPrecision = dynamicPrecision;
        }

        static double DynamicRound(double value, int dynamicPrecision)
        {
            // Note: This doesn't do the right thing for values which have more digits left of the decimal point than dynamicPrecision.
            var precision = (int)(dynamicPrecision - Math.Log10(value));
            return Math.Round(value, precision < 0 ? 0 : precision > 15 ? 15 : precision);
        }

        public bool Equals(double x, double y)
        {
            x = DynamicRound(x, DynamicPrecision);
            y = DynamicRound(y, DynamicPrecision);
            return x.Equals(y);
        }

        public int GetHashCode(double obj)
        {
            obj = DynamicRound(obj, DynamicPrecision);
            return obj.GetHashCode();
        }
    }
}
