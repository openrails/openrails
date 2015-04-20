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

using System;
using Microsoft.Xna.Framework;
using ORTS.Common;
using ORTS.MultiPlayer;
using ORTS.Settings;
using System.Collections.Generic;
using System.IO;

namespace ORTS.Viewer3D
{
    public class WeatherControl
    {
        readonly Viewer Viewer;

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

        // Overcast factor: 0.0 = almost no clouds; 0.1 = wispy clouds; 1.0 = total overcast.
        public float overcastFactor;
        // Pricipitation intensity in particles per second per meter^2 (PPSPM2).
        public float pricipitationIntensityPPSPM2;
        // Fog/visibility distance. Ranges from 10m (can't see anything), 5km (medium), 20km (clear) to 100km (clear arctic).
        public float fogDistance;

        public readonly List<SoundSourceBase> ClearSound;
        public readonly List<SoundSourceBase> RainSound;
        public readonly List<SoundSourceBase> SnowSound;
        public readonly List<SoundSourceBase> WeatherSounds = new List<SoundSourceBase>();

        public Vector2 WindSpeedMpS = new Vector2();
        public float WindSpeed { get { return WindSpeedMpS.Length(); } }
        public float WindDirection { get { return (float)Math.Atan2(WindSpeedMpS.X, WindSpeedMpS.Y); } }

        readonly float[] WindChangeMpSS = { 40, 5 }; // Flurry, steady
        const float WindSpeedMaxMpS = 30;

        // Variables used for wind calculations
        Vector2 WindSpeedInternalMpS;
        Vector2[] windSpeedMpS = new Vector2[2];
        float Time;

        public WeatherControl(Viewer viewer)
        {
            Viewer = viewer;

            var pathArray = new[] {
                Program.Simulator.RoutePath + @"\SOUND",
                Program.Simulator.BasePath + @"\SOUND",
            };

            ClearSound = new List<SoundSourceBase>() {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "clear_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "clear_ex.sms"), false),
            };
            RainSound = new List<SoundSourceBase>() {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "rain_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "rain_ex.sms"), false),
            };
            SnowSound = new List<SoundSourceBase>() {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "snow_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "snow_ex.sms"), false),
            };

            WeatherSounds.AddRange(ClearSound);
            WeatherSounds.AddRange(RainSound);
            WeatherSounds.AddRange(SnowSound);

            SetInitialWeatherParameters();
            UpdateWeatherParameters();
        }

         public void SaveWeatherParameters(BinaryWriter outf)
        {
            outf.Write(fogDistance);
            outf.Write(overcastFactor);
            outf.Write(pricipitationIntensityPPSPM2);
        }

         public void RestoreWeatherParameters(BinaryReader inf)
         {
             fogDistance = inf.ReadSingle();
             overcastFactor = inf.ReadSingle();
             pricipitationIntensityPPSPM2 = inf.ReadSingle();
             UpdateVolume();
         }

        void SetInitialWeatherParameters()
        {
            // These values are defaults only; subsequent changes to the weather via debugging only change the components (weather, overcastFactor and fogDistance) individually.
            switch (Viewer.Simulator.Weather)
            {
                case Orts.Formats.Msts.WeatherType.Clear: overcastFactor = 0.05f; fogDistance = 20000; break;
                case Orts.Formats.Msts.WeatherType.Rain: overcastFactor = 0.7f; fogDistance = 1000; break;
                case Orts.Formats.Msts.WeatherType.Snow: overcastFactor = 0.6f; fogDistance = 500; break;
            }
        }

        void UpdateWeatherParameters()
        {
            Viewer.SoundProcess.RemoveSoundSources(this);
            switch (Viewer.Simulator.Weather)
            {
                case Orts.Formats.Msts.WeatherType.Clear: pricipitationIntensityPPSPM2 = 0; Viewer.SoundProcess.AddSoundSources(this, ClearSound); break;
                case Orts.Formats.Msts.WeatherType.Rain: pricipitationIntensityPPSPM2 = 0.010f; Viewer.SoundProcess.AddSoundSources(this, RainSound); break;
                case Orts.Formats.Msts.WeatherType.Snow: pricipitationIntensityPPSPM2 = 0.010f; Viewer.SoundProcess.AddSoundSources(this, SnowSound); break;
            }

            // WeatherControl is created during World consturction so this needs to be skipped.
            if (Viewer.World != null) Viewer.World.Precipitation.Reset();
        }

        void UpdateVolume()
        {
            foreach (var soundSource in RainSound) soundSource.Volume = pricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
            foreach (var soundSource in SnowSound) soundSource.Volume = pricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
        }

        private void UpdateWind(ElapsedTime elapsedTime)
        {
            Time += elapsedTime.ClockSeconds;
            WindSpeedInternalMpS = Vector2.Zero;
            for (var i = 0; i < windSpeedMpS.Length; i++)
            {
                windSpeedMpS[i].X += ((float)Program.Random.NextDouble() * 2 - 1) * WindChangeMpSS[i] * elapsedTime.ClockSeconds;
                windSpeedMpS[i].Y += ((float)Program.Random.NextDouble() * 2 - 1) * WindChangeMpSS[i] * elapsedTime.ClockSeconds;

                var windMagnitude = windSpeedMpS[i].Length() / (i == 0 ? WindSpeedMpS.Length() * 0.4f : WindSpeedMaxMpS);
                if (windMagnitude > 1)
                    windSpeedMpS[i] /= windMagnitude;

                WindSpeedInternalMpS += windSpeedMpS[i];
            }

            WindSpeedMpS = WindSpeedInternalMpS;
        }

        // TODO: Add several other weather conditions, such as PartlyCloudy, LightRain, 
        // HeavySnow, etc. to the Options dialog as dropdown list boxes. Transfer user's
        // selection to RunActivity and make appropriate adjustments to the weather here.
        // This class will eventually be expanded to interpret dynamic weather scripts and
        // make game-time weather transitions.

        [CallOnThread("Updater")]
        public void Update(ElapsedTime elapsedTime)
        {
            if (MPManager.IsClient() && MPManager.Instance().weatherChanged)
            {
                // Multiplayer weather has changed so we need to update our state to match weather, overcastFactor, pricipitationIntensity and fogDistance.
                if (MPManager.Instance().weather >= 0 && MPManager.Instance().weather != (int)Viewer.Simulator.Weather) { Viewer.Simulator.Weather = (Orts.Formats.Msts.WeatherType)MPManager.Instance().weather; UpdateWeatherParameters(); }
                if (MPManager.Instance().overcastFactor >= 0) overcastFactor = MPManager.Instance().overcastFactor;
                if (MPManager.Instance().pricipitationIntensity >= 0) { pricipitationIntensityPPSPM2 = MPManager.Instance().pricipitationIntensity; UpdateVolume(); }
                if (MPManager.Instance().fogDistance >= 0) fogDistance = MPManager.Instance().fogDistance;

                // Reset the message now that we've applied all the changes.
                try
                {
                    if ((MPManager.Instance().weather >= 0 && MPManager.Instance().weather != (int)Viewer.Simulator.Weather) || MPManager.Instance().overcastFactor >= 0 || MPManager.Instance().pricipitationIntensity >= 0 || MPManager.Instance().fogDistance >= 0)
                    {
                        MPManager.Instance().weatherChanged = false;
                        MPManager.Instance().weather = -1;
                        MPManager.Instance().overcastFactor = -1;
                        MPManager.Instance().pricipitationIntensity = -1;
                        MPManager.Instance().fogDistance = -1;
                    }
                }
                catch { }
            }

            if (!MPManager.IsClient())
            {
                // The user is able to change the weather for debugging. This will cycle through clear, rain and snow.
                if (UserInput.IsPressed(UserCommands.DebugWeatherChange))
                {
                    switch (Viewer.Simulator.Weather)
                    {
                        case Orts.Formats.Msts.WeatherType.Clear:
                            Viewer.Simulator.Weather = Orts.Formats.Msts.WeatherType.Rain;
                            break;
                        case Orts.Formats.Msts.WeatherType.Rain:
                            Viewer.Simulator.Weather = Orts.Formats.Msts.WeatherType.Snow;
                            break;
                        case Orts.Formats.Msts.WeatherType.Snow:
                            Viewer.Simulator.Weather = Orts.Formats.Msts.WeatherType.Clear;
                            break;
                    }
                    UpdateWeatherParameters();

                    // If we're a multiplayer server, send out the new weather to all clients.
                    if (MPManager.IsServer())
                        MPManager.Notify((new MSGWeather((int)Viewer.Simulator.Weather, -1, -1, -1)).ToString());
                }

                // Overcast ranges from 0 (completely clear) to 1 (completely overcast).
                if (UserInput.IsDown(UserCommands.DebugOvercastIncrease)) overcastFactor = MathHelper.Clamp(overcastFactor + elapsedTime.RealSeconds / 10, 0, 1);
                if (UserInput.IsDown(UserCommands.DebugOvercastDecrease)) overcastFactor = MathHelper.Clamp(overcastFactor - elapsedTime.RealSeconds / 10, 0, 1);
                
                // Pricipitation ranges from 0 to 15000.
                if (UserInput.IsDown(UserCommands.DebugPrecipitationIncrease)) pricipitationIntensityPPSPM2 = MathHelper.Clamp(pricipitationIntensityPPSPM2 * 1.05f, PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
                if (UserInput.IsDown(UserCommands.DebugPrecipitationDecrease)) pricipitationIntensityPPSPM2 = MathHelper.Clamp(pricipitationIntensityPPSPM2 / 1.05f, PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
                if (UserInput.IsDown(UserCommands.DebugPrecipitationIncrease) || UserInput.IsDown(UserCommands.DebugPrecipitationDecrease)) UpdateVolume();

                // Fog ranges from 10m (can't see anything) to 100km (clear arctic conditions).
                if (UserInput.IsDown(UserCommands.DebugFogIncrease)) fogDistance = MathHelper.Clamp(fogDistance - elapsedTime.RealSeconds * fogDistance, 10, 100000);
                if (UserInput.IsDown(UserCommands.DebugFogDecrease)) fogDistance = MathHelper.Clamp(fogDistance + elapsedTime.RealSeconds * fogDistance, 10, 100000);

                UpdateWind(elapsedTime);
            }

            if (!MultiPlayer.MPManager.IsMultiPlayer())
            {
                // Shift the clock forwards or backwards at 1h-per-second.
                if (UserInput.IsDown(UserCommands.DebugClockForwards)) Viewer.Simulator.ClockTime += elapsedTime.RealSeconds * 3600;
                if (UserInput.IsDown(UserCommands.DebugClockBackwards)) Viewer.Simulator.ClockTime -= elapsedTime.RealSeconds * 3600;
            }

            // If we're a multiplayer server, send out the new overcastFactor, pricipitationIntensity and fogDistance to all clients.
            if (MPManager.IsServer())
            {
                if (UserInput.IsReleased(UserCommands.DebugOvercastIncrease) || UserInput.IsReleased(UserCommands.DebugOvercastDecrease)
                    || UserInput.IsReleased(UserCommands.DebugPrecipitationIncrease) || UserInput.IsReleased(UserCommands.DebugPrecipitationDecrease)
                    || UserInput.IsReleased(UserCommands.DebugFogIncrease) || UserInput.IsReleased(UserCommands.DebugFogDecrease))
                {
                    MPManager.Instance().SetEnvInfo(overcastFactor, fogDistance);
                    MPManager.Notify((new MSGWeather(-1, overcastFactor, pricipitationIntensityPPSPM2, fogDistance)).ToString());
                }
            }
        }
    }
}
