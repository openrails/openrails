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
                    overcast = 0.6f;
                    intensity = 3500;
                    fogCoeff = 0.2f;
                    break;
                case (int)MSTS.WeatherType.Snow:
                    overcast = 0.6f;
                    intensity = 3500;
                    fogCoeff = 0.1f;
                    break;
                case (int)MSTS.WeatherType.Clear:
                    overcast = 0.1f;
                    fogCoeff = 1.0f;
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
