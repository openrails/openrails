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

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    public class WeatherControl
    {
        Viewer3D Viewer;
        private int weatherType;
        public int seasonType;
        // Overcast factor: 0.0=almost no clouds; 0.1=wispy clouds; 1.0=total overcast
        public float overcast = 0.1f;
        public float intensity = 3500;
        public float fogCoeff = 0.75f;

        public WeatherControl(Viewer3D viewer)
        {
            Viewer = viewer;
            weatherType = (int)Viewer.Simulator.Weather;
            seasonType = (int)Viewer.Simulator.Season;

            SetWeatherParams();
        }

        void SetWeatherParams()
        {
            switch (weatherType)
            {
                case (int)MSTS.WeatherType.Rain:
                    overcast = 0.7f;
                    intensity = 4500;
                    fogCoeff = 0.5f;
                    break;
                case (int)MSTS.WeatherType.Snow:
                    overcast = 0.6f;
                    intensity = 6500;
                    fogCoeff = 0.1f;
                    break;
                case (int)MSTS.WeatherType.Clear:
                    overcast = 0.05f;
                    fogCoeff = 0.9f;
                    break;
            }
        }

        // TODO: Add several other weather conditions, such as PartlyCloudy, LightRain, 
        // HeavySnow, etc. to the Options dialog as dropdown list boxes. Transfer user's
        // selection to RunActivity and make appropriate adjustments to the weather here.
        // This class will eventually be expanded to interpret dynamic weather scripts and
        // make game-time weather transitions.
    }
}
