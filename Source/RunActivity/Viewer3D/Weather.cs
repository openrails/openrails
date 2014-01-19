// COPYRIGHT 2010, 2011, 2014 by the Open Rails project.
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

using System.Collections.Generic;

namespace ORTS
{
    public class WeatherControl
    {
        readonly Viewer3D Viewer;

        // Overcast factor: 0.0 = almost no clouds; 0.1 = wispy clouds; 1.0 = total overcast.
        public float overcastFactor = 0.1f;
        // ???
        public float pricipitationIntensity = 3500;
        // Fog/visibility distance. Ranges from 10m (can't see anything), 5km (medium), 20km (clear) to 100km (clear arctic).
        public float fogDistance = 5000;

        public readonly List<SoundSourceBase> ClearSound;
        public readonly List<SoundSourceBase> RainSound;
        public readonly List<SoundSourceBase> SnowSound;
        public readonly List<SoundSourceBase> WeatherSounds = new List<SoundSourceBase>();

        public WeatherControl(Viewer3D viewer)
        {
            Viewer = viewer;

            var pathArray = new[] {
                Program.Simulator.RoutePath + @"\SOUND",
                Program.Simulator.BasePath + @"\SOUND",
            };

            ClearSound = new List<SoundSourceBase>() {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "clear_in.sms")),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "clear_ex.sms")),
            };
            RainSound = new List<SoundSourceBase>() {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "rain_in.sms")),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "rain_ex.sms")),
            };
            SnowSound = new List<SoundSourceBase>() {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "snow_in.sms")),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "snow_ex.sms")),
            };

            WeatherSounds.AddRange(ClearSound);
            WeatherSounds.AddRange(RainSound);
            WeatherSounds.AddRange(SnowSound);

            SetWeather(Viewer.Simulator.Weather);
        }

        public void SetWeather(MSTS.WeatherType weather)
        {
            if (Viewer.SoundProcess != null) Viewer.SoundProcess.RemoveSoundSource(this);
            switch (weather)
            {
                case MSTS.WeatherType.Clear:
                    overcastFactor = 0.05f;
                    fogDistance = 20000;
                    if (Viewer.SoundProcess != null) Viewer.SoundProcess.AddSoundSource(this, ClearSound);
                    break;
                case MSTS.WeatherType.Rain:
                    overcastFactor = 0.7f;
                    pricipitationIntensity = 4500;
                    fogDistance = 1000;
                    if (Viewer.SoundProcess != null) Viewer.SoundProcess.AddSoundSource(this, RainSound);
                    break;
                case MSTS.WeatherType.Snow:
                    overcastFactor = 0.6f;
                    pricipitationIntensity = 6500;
                    fogDistance = 500;
                    if (Viewer.SoundProcess != null) Viewer.SoundProcess.AddSoundSource(this, SnowSound);
                    break;
            }
        }

        public void SetPricipitationVolume(float volume)
        {
            foreach (var soundSource in RainSound)
            {
                soundSource.Volume = volume;
            }
            foreach (var soundSource in SnowSound)
            {
                soundSource.Volume = volume;
            }
        }

        // TODO: Add several other weather conditions, such as PartlyCloudy, LightRain, 
        // HeavySnow, etc. to the Options dialog as dropdown list boxes. Transfer user's
        // selection to RunActivity and make appropriate adjustments to the weather here.
        // This class will eventually be expanded to interpret dynamic weather scripts and
        // make game-time weather transitions.
    }
}
