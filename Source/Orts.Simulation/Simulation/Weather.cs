// COPYRIGHT 2009 - 2023 by the Open Rails project.
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
using Microsoft.Xna.Framework;

namespace Orts.Simulation
{
    public static class WeatherConstants
    {
        // Source: http://www.icscc.org.cn/upload/file/20190102/Doc.9837-EN%20Manual%20on%20Automatic%20Meteorological%20Observing%20Systems%20at%20Aerodromes.pdf
        // Manual on Automatic Meteorological Observing Systems at Aerodromes, Second Edition - 2011, International Civil Aviation Organization
        //   Type       Intensity    Rate (mm/h)
        //   Drizzle    Light        <0.1
        //              Moderate      0.1-0.5
        //              Heavy            >0.5
        //   Rain       Light        <2.5
        //              Moderate      2.5-10.0
        //              Heavy            >10.0
        //   Snow       Light        <1.0
        //              Moderate      1.0-5.0
        //              Heavy            >5.0
        // Other interesting items:
        //    - Page 79, Table A-1. MOR limit above which visibility is equal to MOR
        //      Night/day background luminance values
        //    - Page 81, Figure A-2. Example of diagram (T_air, RH) used for determining the present weather phenomenon
        //      Chart of precipitation type vs air temperature and humidity

        // Final values are twice the heavy value, as a reasonable maximum possible value for interpolation
        public static readonly float[] DrizzleRateMMpH = new[] { 0.0f, 0.1f, 0.5f, 1.0f };
        public static readonly float[] RainRateMMpH = new[] { 0.0f, 2.5f, 10.0f, 20.0f };
        public static readonly float[] SnowRateMMpH = new[] { 0.0f, 1.0f, 5.0f, 10.0f };

        // Wind speed (Beaufort scale)
        //   Number    Description        Speed (m/s)
        //   0         Calm               >0.0
        //   1         Light air          >0.5
        //   2         Light breeze       >1.6
        //   3         Gentle breeze      >3.4
        //   4         Moderate breeze    >5.5
        //   5         Fresh breeze       >8.0
        //   6         Strong breeze      >10.8
        //   7         High wind          >13.9
        //   8         Gale               >17.2
        //   9         Strong gale        >20.8
        //   10        Storm              >24.5
        //   11        Violent storm      >28.5
        //   12        Hurricane-force    >32.7
        public static readonly float[] WindSpeedBeaufortMpS = new[] { 0.0f, 0.5f, 1.6f, 3.4f, 5.5f, 8.0f, 10.8f, 13.9f, 17.2f, 20.8f, 24.5f, 28.5f, 32.7f };

        // Wind gusts ("Rafale". Glossaire météorologique (in French). Météo-France. Retrieved 2018-11-15.):
        //   Type        Excess speed (m/s)
        //   Light       >5.1 (10-15 knots)
        //   Moderate    >7.7 (15-25 knots)
        //   Heavy       >12.9 (>25 knots)
        public static readonly float[] WindSpeedGustMpS = new[] { 5.1f, 7.7f, 12.9f, 25.8f };

        public enum Condition
        {
            Light,
            Moderate,
            Heavy,
        }
    }

    public class Weather
    {
        // Fog/visibility distance. Ranges from 10m (can't see anything), 5km (medium), 20km (clear) to 100km (clear arctic).
        public float VisibilityM;

        // Cloud cover factor: 0.0 = almost no clouds; 0.1 = wispy clouds; 1.0 = total overcast.
        public float CloudCoverFactor;

        // Precipitation intensity in particles per second per meter^2 (PPSPM2).
        public float PrecipitationIntensityPPSPM2;

        // Precipitation liquidity; 1 = rain, 0 = snow; intermediate values possible with dynamic weather.
        public float PrecipitationLiquidity;

        // Wind has an average direction (normalized vector pointing WITH wind movement) and speed, and instantaneous direction and speed (e.g. gusts).
        public Vector2 WindAverageDirection;
        public Vector2 WindInstantaneousDirection;
        public float WindAverageSpeedMpS;
        public float WindInstantaneousSpeedMpS;

        public float WindAverageDirectionRad
        {
            get => (float)Math.Atan2(WindAverageDirection.X, -WindAverageDirection.Y);
            set => WindAverageDirection = new Vector2((float)Math.Sin(value), -(float)Math.Cos(value));
        }

        public float WindInstantaneousDirectionRad
        {
            get => (float)Math.Atan2(WindInstantaneousDirection.X, -WindInstantaneousDirection.Y);
            set => WindInstantaneousDirection = new Vector2((float)Math.Sin(value), -(float)Math.Cos(value));
        }

        // FIXME: This is not a weather simulation field
        // Daylight offset (-12h to +12h)
        public int DaylightOffset = 0;
    }
}
