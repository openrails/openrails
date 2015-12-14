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

using Microsoft.Xna.Framework;
using Orts.Common;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Simulation;
using ORTS.Common;
using ORTS.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using Events = Orts.Common.Events;

namespace Orts.Viewer3D
{
    public class WeatherControl
    {
        readonly Viewer Viewer;
        readonly Weather Weather;

        public readonly List<SoundSourceBase> ClearSound;
        public readonly List<SoundSourceBase> RainSound;
        public readonly List<SoundSourceBase> SnowSound;
        public readonly List<SoundSourceBase> WeatherSounds = new List<SoundSourceBase>();

        readonly float[] WindChangeMpSS = { 40, 5 }; // Flurry, steady
        const float WindSpeedMaxMpS = 30;

        public bool weatherChangeOn = false;
        public DynamicWeather dynamicWeather;

        // Variables used for wind calculations
        Vector2 WindSpeedInternalMpS;
        Vector2[] windSpeedMpS = new Vector2[2];
        float Time;

        public WeatherControl(Viewer viewer)
        {
            Viewer = viewer;
            Weather = Viewer.Simulator.Weather;

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

            Viewer.Simulator.WeatherChanged += (object sender, EventArgs e) =>
            {
                SetInitialWeatherParameters();
                UpdateWeatherParameters();
            };
        }

        public void SaveWeatherParameters(BinaryWriter outf)
        {
            outf.Write(Weather.FogDistance);
            outf.Write(Weather.OvercastFactor);
            outf.Write(Weather.PricipitationIntensityPPSPM2);
            outf.Write(Weather.PrecipitationLiquidity);
            outf.Write(weatherChangeOn);
            if (weatherChangeOn)
            {
                dynamicWeather.Save(outf);
            }
        }

        public void RestoreWeatherParameters(BinaryReader inf)
        {
            Weather.FogDistance = inf.ReadSingle();
            Weather.OvercastFactor = inf.ReadSingle();
            Weather.PricipitationIntensityPPSPM2 = inf.ReadSingle();
            Weather.PrecipitationLiquidity = inf.ReadSingle();
            weatherChangeOn = inf.ReadBoolean();
            if (weatherChangeOn)
            {
                dynamicWeather = new DynamicWeather();
                dynamicWeather.Restore(inf);
              }
            UpdateVolume();
        }

        public void SetInitialWeatherParameters()
        {
            // These values are defaults only; subsequent changes to the weather via debugging only change the components (weather, overcastFactor and fogDistance) individually.
            switch (Viewer.Simulator.WeatherType)
            {
                case Orts.Formats.Msts.WeatherType.Clear: Weather.OvercastFactor = 0.05f; Weather.FogDistance = 20000; Weather.PrecipitationLiquidity = 1;  break;
                case Orts.Formats.Msts.WeatherType.Rain: Weather.OvercastFactor = 0.7f; Weather.FogDistance = 1000; Weather.PrecipitationLiquidity = 1; break;
                case Orts.Formats.Msts.WeatherType.Snow: Weather.OvercastFactor = 0.6f; Weather.FogDistance = 500; Weather.PrecipitationLiquidity = 0; break;
            }
        }

        public void UpdateWeatherParameters()
        {
            Viewer.SoundProcess.RemoveSoundSources(this);
            switch (Viewer.Simulator.WeatherType)
            {
                case Orts.Formats.Msts.WeatherType.Clear: Weather.PricipitationIntensityPPSPM2 = 0; Viewer.SoundProcess.AddSoundSources(this, ClearSound); break;
                case Orts.Formats.Msts.WeatherType.Rain: Weather.PricipitationIntensityPPSPM2 = 0.010f; Viewer.SoundProcess.AddSoundSources(this, RainSound); break;
                case Orts.Formats.Msts.WeatherType.Snow: Weather.PricipitationIntensityPPSPM2 = 0.0050f; Viewer.SoundProcess.AddSoundSources(this, SnowSound); break;
            }

            // WeatherControl is created during World consturction so this needs to be skipped.
            if (Viewer.World != null) Viewer.World.Precipitation.Reset();
        }

        void UpdateSoundSources()
        {
            Viewer.SoundProcess.RemoveSoundSources(this);
            switch (Viewer.Simulator.WeatherType)
            {
                case Orts.Formats.Msts.WeatherType.Clear: Viewer.SoundProcess.AddSoundSources(this, ClearSound); break;
                case Orts.Formats.Msts.WeatherType.Rain: Viewer.SoundProcess.AddSoundSources(this, RainSound); break;
                case Orts.Formats.Msts.WeatherType.Snow: Viewer.SoundProcess.AddSoundSources(this, SnowSound); break;
            }
        }

        void UpdateVolume()
        {
            if (Viewer.GraphicsDevice.GraphicsDeviceCapabilities.MaxVertexIndex > 0xFFFF) // 0xFFFF represents 65535 which is the max for 16bit devices.
            {
                foreach (var soundSource in RainSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
                foreach (var soundSource in SnowSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
            }
            else
            {
                foreach (var soundSource in RainSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2_16;
                foreach (var soundSource in SnowSound) soundSource.Volume = Weather.PricipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2_16;
            }
        }

        private void UpdateWind(ElapsedTime elapsedTime)
        {
            Time += elapsedTime.ClockSeconds;
            WindSpeedInternalMpS = Vector2.Zero;
            for (var i = 0; i < windSpeedMpS.Length; i++)
            {
                windSpeedMpS[i].X += ((float)Viewer.Random.NextDouble() * 2 - 1) * WindChangeMpSS[i] * elapsedTime.ClockSeconds;
                windSpeedMpS[i].Y += ((float)Viewer.Random.NextDouble() * 2 - 1) * WindChangeMpSS[i] * elapsedTime.ClockSeconds;

                var windMagnitude = windSpeedMpS[i].Length() / (i == 0 ? Weather.WindSpeedMpS.Length() * 0.4f : WindSpeedMaxMpS);
                if (windMagnitude > 1)
                    windSpeedMpS[i] /= windMagnitude;

                WindSpeedInternalMpS += windSpeedMpS[i];
            }

            Weather.WindSpeedMpS = WindSpeedInternalMpS;
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
                if (MPManager.Instance().weather >= 0 && MPManager.Instance().weather != (int)Viewer.Simulator.WeatherType) { Viewer.Simulator.WeatherType = (Orts.Formats.Msts.WeatherType)MPManager.Instance().weather; UpdateWeatherParameters(); }
                if (MPManager.Instance().overcastFactor >= 0) Weather.OvercastFactor = MPManager.Instance().overcastFactor;
                if (MPManager.Instance().pricipitationIntensity >= 0) { Weather.PricipitationIntensityPPSPM2 = MPManager.Instance().pricipitationIntensity; UpdateVolume(); }
                if (MPManager.Instance().fogDistance >= 0) Weather.FogDistance = MPManager.Instance().fogDistance;

                // Reset the message now that we've applied all the changes.
                try
                {
                    if ((MPManager.Instance().weather >= 0 && MPManager.Instance().weather != (int)Viewer.Simulator.WeatherType) || MPManager.Instance().overcastFactor >= 0 || MPManager.Instance().pricipitationIntensity >= 0 || MPManager.Instance().fogDistance >= 0)
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
                    switch (Viewer.Simulator.WeatherType)
                    {
                        case Orts.Formats.Msts.WeatherType.Clear:
                            Viewer.Simulator.WeatherType = Orts.Formats.Msts.WeatherType.Rain;
                            break;
                        case Orts.Formats.Msts.WeatherType.Rain:
                            Viewer.Simulator.WeatherType = Orts.Formats.Msts.WeatherType.Snow;
                            break;
                        case Orts.Formats.Msts.WeatherType.Snow:
                            Viewer.Simulator.WeatherType = Orts.Formats.Msts.WeatherType.Clear;
                            break;
                    }
                    // block dynamic weather change after a manual weather change operation
                    weatherChangeOn = false;
                    if (dynamicWeather != null) dynamicWeather.ResetWeatherTargets();
                    UpdateWeatherParameters();

                    // If we're a multiplayer server, send out the new weather to all clients.
                    if (MPManager.IsServer())
                        MPManager.Notify((new MSGWeather((int)Viewer.Simulator.WeatherType, -1, -1, -1)).ToString());
                }

                // Overcast ranges from 0 (completely clear) to 1 (completely overcast).
                if (UserInput.IsDown(UserCommands.DebugOvercastIncrease))
                {
                    Weather.OvercastFactor = MathHelper.Clamp(Weather.OvercastFactor + elapsedTime.RealSeconds / 10, 0, 1);
                    weatherChangeOn = false;
                    if (dynamicWeather != null) dynamicWeather.ORTSOvercast = -1;
                }
                if (UserInput.IsDown(UserCommands.DebugOvercastDecrease))
                {
                    Weather.OvercastFactor = MathHelper.Clamp(Weather.OvercastFactor - elapsedTime.RealSeconds / 10, 0, 1);
                    weatherChangeOn = false;
                    if (dynamicWeather != null) dynamicWeather.ORTSOvercast = -1;
                }

                // Pricipitation ranges from 0 to max PrecipitationViewer.MaxIntensityPPSPM2 if 32bit.
                // 16bit uses PrecipitationViewer.MaxIntensityPPSPM2_16
                if (Viewer.GraphicsDevice.GraphicsDeviceCapabilities.MaxVertexIndex > 0xFFFF) // 0xFFFF represents 65535 which is the max for 16bit devices.
                {
                    if (UserInput.IsDown(UserCommands.DebugPrecipitationIncrease))
                    {
                        Weather.PricipitationIntensityPPSPM2 = MathHelper.Clamp(Weather.PricipitationIntensityPPSPM2 * 1.05f, PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
                        weatherChangeOn = false;
                        if (dynamicWeather != null) dynamicWeather.ORTSPrecipitationIntensity = -1;
                    }
                    if (UserInput.IsDown(UserCommands.DebugPrecipitationDecrease))
                    {
                        Weather.PricipitationIntensityPPSPM2 = MathHelper.Clamp(Weather.PricipitationIntensityPPSPM2 / 1.05f, PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
                        weatherChangeOn = false;
                        if (dynamicWeather != null) dynamicWeather.ORTSPrecipitationIntensity = -1;
                    }
                    if (UserInput.IsDown(UserCommands.DebugPrecipitationIncrease) || UserInput.IsDown(UserCommands.DebugPrecipitationDecrease)) UpdateVolume();
                }
                else
                {
                    if (UserInput.IsDown(UserCommands.DebugPrecipitationIncrease))
                    {
                        Weather.PricipitationIntensityPPSPM2 = MathHelper.Clamp(Weather.PricipitationIntensityPPSPM2 * 1.05f, PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2_16);
                        weatherChangeOn = false;
                        if (dynamicWeather != null) dynamicWeather.ORTSPrecipitationIntensity = -1;
                    }
                    if (UserInput.IsDown(UserCommands.DebugPrecipitationDecrease))
                    {
                        Weather.PricipitationIntensityPPSPM2 = MathHelper.Clamp(Weather.PricipitationIntensityPPSPM2 / 1.05f, PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2_16);
                        weatherChangeOn = false;
                        if (dynamicWeather != null) dynamicWeather.ORTSPrecipitationIntensity = -1;
                    }
                    if (UserInput.IsDown(UserCommands.DebugPrecipitationIncrease) || UserInput.IsDown(UserCommands.DebugPrecipitationDecrease)) UpdateVolume();
                }
                // Fog ranges from 10m (can't see anything) to 100km (clear arctic conditions).
                if (UserInput.IsDown(UserCommands.DebugFogIncrease))
                {
                    Weather.FogDistance = MathHelper.Clamp(Weather.FogDistance - elapsedTime.RealSeconds * Weather.FogDistance, 10, 100000);
                    weatherChangeOn = false;
                    if (dynamicWeather != null) dynamicWeather.ORTSFog = -1;
                }
                if (UserInput.IsDown(UserCommands.DebugFogDecrease))
                {
                    Weather.FogDistance = MathHelper.Clamp(Weather.FogDistance + elapsedTime.RealSeconds * Weather.FogDistance, 10, 100000);
                    if (dynamicWeather != null) dynamicWeather.ORTSFog = -1;
                    weatherChangeOn = false;
                }

                UpdateWind(elapsedTime);
            }

            if (!Orts.MultiPlayer.MPManager.IsMultiPlayer())
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
                    MPManager.Instance().SetEnvInfo(Weather.OvercastFactor, Weather.FogDistance);
                    MPManager.Notify((new MSGWeather(-1, Weather.OvercastFactor, Weather.PricipitationIntensityPPSPM2, Weather.FogDistance)).ToString());
                }
            }
            if (Program.Simulator != null && Program.Simulator.ActivityRun != null && Program.Simulator.ActivityRun.triggeredEvent != null &&
               Program.Simulator.ActivityRun.triggeredEvent.ORTSWeatherChange != null)
                // Start a weather change sequence in activity mode
            {
                // if not yet weather changes, create the instance
                if (dynamicWeather == null)
                {
                    dynamicWeather = new DynamicWeather();
                }
                dynamicWeather.WeatherChange_Init(Program.Simulator.ActivityRun.triggeredEvent.ORTSWeatherChange, this);
                Program.Simulator.ActivityRun.triggeredEvent = null;               
            }
            if (weatherChangeOn)
                // manage the weather change sequence
            {
                dynamicWeather.WeatherChange_Update(elapsedTime, this);
            }
        }
        public class DynamicWeather
        {
            public const float RainSnowLiquidityThreshold = 0.3f;
            public float overcastChangeRate = 0;
            public float overcastTimer = 0;
            public float fogChangeRate = 0;
            public float fogTimer = 0;
            public float precipitationIntensityChangeRate = 0;
            public float precipitationIntensityTimer = 0;
            public float precipitationLiquidityChangeRate = 0;
            public float precipitationLiquidityTimer = 0;
            public float ORTSOvercast = -1;
            public int ORTSOvercastTransitionTimeS = -1;
            public float ORTSFog = -1;
            public int ORTSFogTransitionTimeS = -1;
            public float ORTSPrecipitationIntensity = -1;
            public int ORTSPrecipitationIntensityTransitionTimeS = -1;
            public float ORTSPrecipitationLiquidity = -1;
            public int ORTSPrecipitationLiquidityTransitionTimeS = -1;
            public bool fogDistanceIncreasing = false;
            public DynamicWeather()
            {
            }

            public void Save(BinaryWriter outf)
            {
                outf.Write(overcastTimer);
                outf.Write(overcastChangeRate);
                outf.Write(fogTimer);
                outf.Write(fogChangeRate);
                outf.Write(precipitationIntensityTimer);
                outf.Write(precipitationIntensityChangeRate);
                outf.Write(precipitationLiquidityTimer);
                outf.Write(precipitationLiquidityChangeRate);
                outf.Write(ORTSOvercast);
                outf.Write(ORTSFog);
                outf.Write(ORTSPrecipitationIntensity);
                outf.Write(ORTSPrecipitationLiquidity);
                outf.Write(fogDistanceIncreasing);
                outf.Write(ORTSFogTransitionTimeS);
            }

            public void Restore(BinaryReader inf)
            {
                overcastTimer = inf.ReadSingle();
                overcastChangeRate = inf.ReadSingle();
                fogTimer = inf.ReadSingle();
                fogChangeRate = inf.ReadSingle();
                precipitationIntensityTimer = inf.ReadSingle();
                precipitationIntensityChangeRate = inf.ReadSingle();
                precipitationLiquidityTimer = inf.ReadSingle();
                precipitationLiquidityChangeRate = inf.ReadSingle();
                ORTSOvercast = inf.ReadSingle();
                ORTSFog = inf.ReadSingle();
                ORTSPrecipitationIntensity = inf.ReadSingle();
                ORTSPrecipitationLiquidity = inf.ReadSingle();
                fogDistanceIncreasing = inf.ReadBoolean();
                ORTSFogTransitionTimeS = inf.ReadInt32();
            }

            public void ResetWeatherTargets()
            {
                ORTSOvercast = -1;
                ORTSFog = -1;
                ORTSPrecipitationIntensity = -1;
                ORTSPrecipitationLiquidity = -1;
            }

            // Check for correctness of parameters and initialize rates of change

            public void WeatherChange_Init(ORTSWeatherChange eventWeatherChange, WeatherControl weatherControl)
            {
                var wChangeOn = false;
                if (eventWeatherChange.ORTSOvercast >= 0 && eventWeatherChange.ORTSOvercastTransitionTimeS >= 0)
                {
                    ORTSOvercast = eventWeatherChange.ORTSOvercast;
                    ORTSOvercastTransitionTimeS = eventWeatherChange.ORTSOvercastTransitionTimeS;
                    overcastTimer = (float)ORTSOvercastTransitionTimeS;
                    overcastChangeRate = overcastTimer > 0 ? (MathHelper.Clamp(ORTSOvercast, 0, 1.0f) - weatherControl.Weather.OvercastFactor) / ORTSOvercastTransitionTimeS : 0;
                    wChangeOn = true;
                }
                if (eventWeatherChange.ORTSFog >= 0 && eventWeatherChange.ORTSFogTransitionTimeS >= 0)
                {
                    ORTSFog = eventWeatherChange.ORTSFog;
                    ORTSFogTransitionTimeS = eventWeatherChange.ORTSFogTransitionTimeS;
                    fogTimer = (float)ORTSFogTransitionTimeS;
                    var fogFinalValue = MathHelper.Clamp(ORTSFog, 10, 100000);
                    fogDistanceIncreasing = false;
                    fogChangeRate = fogTimer > 0 ? (fogFinalValue - weatherControl.Weather.FogDistance) / (ORTSFogTransitionTimeS * ORTSFogTransitionTimeS) : 0;
                    if (fogFinalValue > weatherControl.Weather.FogDistance)
                    {
                        fogDistanceIncreasing = true;
                        fogChangeRate = -fogChangeRate;
                        ORTSFog = weatherControl.Weather.FogDistance;
                    }
                    wChangeOn = true;
                }
                if (eventWeatherChange.ORTSPrecipitationIntensity >= 0 && eventWeatherChange.ORTSPrecipitationIntensityTransitionTimeS >= 0)
                {
                    ORTSPrecipitationIntensity = eventWeatherChange.ORTSPrecipitationIntensity;
                    ORTSPrecipitationIntensityTransitionTimeS = eventWeatherChange.ORTSPrecipitationIntensityTransitionTimeS;
                    precipitationIntensityTimer = (float)ORTSPrecipitationIntensityTransitionTimeS;
                    // Pricipitation ranges from 0 to max PrecipitationViewer.MaxIntensityPPSPM2 if 32bit.
                    // 16bit uses PrecipitationViewer.MaxIntensityPPSPM2_16
                    if (weatherControl.Viewer.GraphicsDevice.GraphicsDeviceCapabilities.MaxVertexIndex > 0xFFFF)
                        precipitationIntensityChangeRate = precipitationIntensityTimer > 0 ? (MathHelper.Clamp(ORTSPrecipitationIntensity, 0, PrecipitationViewer.MaxIntensityPPSPM2)
                            - weatherControl.Weather.PricipitationIntensityPPSPM2) / ORTSPrecipitationIntensityTransitionTimeS : 0;
                    else
                        precipitationIntensityChangeRate = precipitationIntensityTimer > 0 ? (MathHelper.Clamp(ORTSPrecipitationIntensity, 0, PrecipitationViewer.MaxIntensityPPSPM2_16)
                            - weatherControl.Weather.PricipitationIntensityPPSPM2) / ORTSPrecipitationIntensityTransitionTimeS : 0;
                    wChangeOn = true;
                }
                if (eventWeatherChange.ORTSPrecipitationLiquidity >= 0 && eventWeatherChange.ORTSPrecipitationLiquidityTransitionTimeS >= 0)
                {
                    ORTSPrecipitationLiquidity = eventWeatherChange.ORTSPrecipitationLiquidity;
                    ORTSPrecipitationLiquidityTransitionTimeS = eventWeatherChange.ORTSPrecipitationLiquidityTransitionTimeS;
                    precipitationLiquidityTimer = (float)ORTSPrecipitationLiquidityTransitionTimeS;
                    precipitationLiquidityChangeRate = precipitationLiquidityTimer > 0 ? (MathHelper.Clamp(ORTSPrecipitationLiquidity, 0, 1.0f)
                        - weatherControl.Weather.PrecipitationLiquidity) / ORTSPrecipitationLiquidityTransitionTimeS : 0;
                    wChangeOn = true;
                }
                weatherControl.weatherChangeOn = wChangeOn;
            }

            public void WeatherChange_Update(ElapsedTime elapsedTime, WeatherControl weatherControl)
            {
                var wChangeOn = false;
                if (ORTSOvercast >= 0)
                {
                    overcastTimer -= elapsedTime.ClockSeconds;
                    if (overcastTimer <= 0) overcastTimer = 0;
                    else wChangeOn = true;
                    weatherControl.Weather.OvercastFactor = ORTSOvercast - overcastTimer * overcastChangeRate;
                    if (overcastTimer == 0) ORTSOvercast = -1;
                }
                if (ORTSFog >= 0)
                {
                    fogTimer -= elapsedTime.ClockSeconds;
                    if (fogTimer <= 0) fogTimer = 0;
                    else wChangeOn = true;
                    if (!fogDistanceIncreasing)
                        weatherControl.Weather.FogDistance = ORTSFog - fogTimer * fogTimer * fogChangeRate;
                    else
                    {
                        var fogTimerDifference = ORTSFogTransitionTimeS - fogTimer;
                        weatherControl.Weather.FogDistance = ORTSFog - fogTimerDifference * fogTimerDifference * fogChangeRate;
                    }
                    if (fogTimer == 0) ORTSFog = -1;
                }
                if (ORTSPrecipitationIntensity >= 0)
                {
                    precipitationIntensityTimer -= elapsedTime.ClockSeconds;
                    if (precipitationIntensityTimer <= 0) precipitationIntensityTimer = 0;
                    else wChangeOn = true;
                    var oldPricipitationIntensityPPSPM2 = weatherControl.Weather.PricipitationIntensityPPSPM2;
                    weatherControl.Weather.PricipitationIntensityPPSPM2 = ORTSPrecipitationIntensity - precipitationIntensityTimer * precipitationIntensityChangeRate;
                    if (weatherControl.Weather.PricipitationIntensityPPSPM2 > 0)
                    {
                        if (oldPricipitationIntensityPPSPM2 == 0)
                        {
                            if (weatherControl.Weather.PrecipitationLiquidity > RainSnowLiquidityThreshold) weatherControl.Viewer.Simulator.WeatherType = WeatherType.Rain;
                            else weatherControl.Viewer.Simulator.WeatherType = WeatherType.Snow;
                            weatherControl.UpdateSoundSources();
                        }
                        weatherControl.UpdateVolume();
                    }
                    if (weatherControl.Weather.PricipitationIntensityPPSPM2 == 0)
                    {
                        if (oldPricipitationIntensityPPSPM2 > 0)
                        {
                            weatherControl.Viewer.Simulator.WeatherType = WeatherType.Clear;
                            weatherControl.UpdateSoundSources();
                        }
                    }
                    if (precipitationIntensityTimer == 0) ORTSPrecipitationIntensity = -1;
                }
                if (ORTSPrecipitationLiquidity >= 0)
                {
                    precipitationLiquidityTimer -= elapsedTime.ClockSeconds;
                    if (precipitationLiquidityTimer <= 0) precipitationLiquidityTimer = 0;
                    else wChangeOn = true;
                    var oldPrecipitationLiquidity = weatherControl.Weather.PrecipitationLiquidity;
                    weatherControl.Weather.PrecipitationLiquidity = ORTSPrecipitationLiquidity - precipitationLiquidityTimer * precipitationLiquidityChangeRate;
                    if (weatherControl.Weather.PrecipitationLiquidity > RainSnowLiquidityThreshold)
                    {
                        if (oldPrecipitationLiquidity <= RainSnowLiquidityThreshold)
                        {
                            weatherControl.Viewer.Simulator.WeatherType = WeatherType.Rain;
                            weatherControl.UpdateSoundSources();
                            weatherControl.UpdateVolume();
                        }
                    }
                    if (weatherControl.Weather.PrecipitationLiquidity <= RainSnowLiquidityThreshold)
                    {
                        if (oldPrecipitationLiquidity > RainSnowLiquidityThreshold)
                        {
                            weatherControl.Viewer.Simulator.WeatherType = WeatherType.Snow;
                            weatherControl.UpdateSoundSources();
                            weatherControl.UpdateVolume();
                        }
                    }
                    if (precipitationLiquidityTimer == 0) ORTSPrecipitationLiquidity = -1;
                }
                weatherControl.weatherChangeOn = wChangeOn;
            }
        }
    }
}
