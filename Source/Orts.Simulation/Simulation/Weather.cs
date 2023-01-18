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
    public class Weather
    {
        // Rainy conditions (Glossary of Meteorology (June 2000). "Rain". American Meteorological Society. Retrieved 2010-01-15.):
        //   Type        Rate
        //   Light       <2.5mm/h
        //   Moderate     2.5-7.3mm/h
        //   Heavy           >7.3mm/h
        //   Violent         >50.0mm/h
        //
        // Snowy conditions (Glossary of Meteorology (2009). "Snow". American Meteorological Society. Retrieved 2009-06-28.):
        //   Type        Visibility
        //   Light           >1.0km
        //   Moderate     0.5-1.0km
        //   Heavy       <0.5km

        // Fog/visibility distance. Ranges from 10m (can't see anything), 5km (medium), 20km (clear) to 100km (clear arctic).
        public float VisibilityM;

        // Cloud cover factor: 0.0 = almost no clouds; 0.1 = wispy clouds; 1.0 = total overcast.
        public float CloudCoverFactor;

        // Precipitation intensity in particles per second per meter^2 (PPSPM2).
        public float PrecipitationIntensityPPSPM2;

        // Precipitation liquidity; 1 = rain, 0 = snow; intermediate values possible with dynamic weather.
        public float PrecipitationLiquidity;

        public Vector2 WindSpeedMpS;

        public float WindDirectionRad => (float)Math.Atan2(WindSpeedMpS.X, WindSpeedMpS.Y);
    }
}
