// COPYRIGHT 2022 by the Open Rails project.
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

using System.Linq;
using Xunit;

namespace Tests.Orts.Common
{
    public static class SmoothedData
    {
        [Theory]
        // FPS-like tests
        [InlineData(5, 3, 0.353)]
        [InlineData(10, 3, 0.353)]
        [InlineData(30, 3, 0.353)]
        [InlineData(60, 3, 0.353)]
        [InlineData(120, 3, 0.353)]
        // Physics-like tests
        [InlineData(60, 1, 0.000)] // Exhaust particles
        [InlineData(60, 2, 0.066)] // Smoke colour
        [InlineData(60, 45, 8.007)] // Field rate
        [InlineData(60, 150, 9.355)] // Burn rate
        [InlineData(60, 240, 9.592)] // Boiler heat
        public static void SmoothedFPS(int fps, float smoothPeriodS, float expected)
        {
            var period = (float)(1d / fps);
            var smoothed = new ORTS.Common.SmoothedData(smoothPeriodS);
            smoothed.Update(0, 10);
            foreach (var i in Enumerable.Range(0, 10 * fps)) smoothed.Update(period, 0);
            Assert.Equal(expected, smoothed.SmoothedValue, 3);
        }
    }
}
