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

// This file is the responsibility of the 3D & Environment Team.

// debug compiler flag for test output for automatic weather
// #define DEBUG_AUTOWEATHER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Orts.Common;
using ORTS.Common;
using ORTS.Common.Input;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.MultiPlayer;
using Orts.Simulation;
using Events = Orts.Common.Events;

namespace Orts.Viewer3D
{
    // TODO: Probably everything in here, except sounds, should be in the simulator
    public class WeatherControl
    {
        public readonly Viewer Viewer;
        public readonly Weather Weather;

        public readonly List<SoundSourceBase> ClearSound;
        public readonly List<SoundSourceBase> RainSound;
        public readonly List<SoundSourceBase> SnowSound;
        public readonly List<SoundSourceBase> WeatherSounds = new List<SoundSourceBase>();

        WorldLocation CameraWorldLocation;

        public bool weatherChangeOn = false;
        public DynamicWeather dynamicWeather;
        public bool RandomizedWeather;
        public bool DesertZone; // we are in a desert zone, so no randomized weather change...
        private float[,] DesertZones = { { 30, 45, -120, -105 } }; // minlat, maxlat, minlong, maxlong
        public float Time;

        // Variables used for wind calculations
        const int WindSpeedBeaufort = 6;
        const float WindInstantaneousDirectionLimitRad = (float)(45 * Math.PI / 180);
        static readonly float WindInstantaneousSpeedLimit = WeatherConstants.WindSpeedGustMpS[(int)WeatherConstants.Condition.Light];
        const float WindNoiseScale = 10;
        readonly float WindInstantaneousDirectionNoiseStart = (float)Viewer.Random.NextDouble() * WindNoiseScale;
        readonly float WindInstantaneousSpeedNoiseStart = (float)Viewer.Random.NextDouble() * WindNoiseScale;
        const float WindGustUpdateTimeS = 0.25f;
        float WindUpdateTimer = 0.0f;

        public WeatherControl(Viewer viewer)
        {
            Viewer = viewer;
            Weather = Viewer.Simulator.Weather;

            var pathArray = new[]
            {
                Program.Simulator.RoutePath + @"\SOUND",
                Program.Simulator.BasePath + @"\SOUND",
            };

            ClearSound = new List<SoundSourceBase>()
            {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "clear_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "clear_ex.sms"), false),
            };
            RainSound = new List<SoundSourceBase>()
            {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "rain_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "rain_ex.sms"), false),
            };
            SnowSound = new List<SoundSourceBase>()
            {
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "snow_in.sms"), false),
                new SoundSource(viewer, Events.Source.MSTSInGame, ORTSPaths.GetFileFromFolders(pathArray, "snow_ex.sms"), false),
            };

            WeatherSounds.AddRange(ClearSound);
            WeatherSounds.AddRange(RainSound);
            WeatherSounds.AddRange(SnowSound);

            SetInitialWeatherParameters();
            UpdateWeatherParameters();

            // add here randomized weather
            if (Viewer.Settings.ActWeatherRandomizationLevel > 0 && Viewer.Simulator.ActivityRun != null && !Viewer.Simulator.ActivityRun.WeatherChangesPresent)
            {
                RandomizedWeather = RandomizeInitialWeather();
                dynamicWeather = new DynamicWeather();
                if (RandomizedWeather)
                {
                    UpdateSoundSources();
                    UpdateVolume();

                    // We have a pause in weather change, depending from randomization level
                    dynamicWeather.stableWeatherTimer = ((4.0f - Viewer.Settings.ActWeatherRandomizationLevel) * 600) + Viewer.Random.Next(300) - 150;
                    weatherChangeOn = true;
                }
            }

            Viewer.Simulator.WeatherChanged += (object sender, EventArgs e) =>
            {
                SetInitialWeatherParameters();
                UpdateWeatherParameters();
            };
        }

        public virtual void SaveWeatherParameters(BinaryWriter outf)
        {
            outf.Write(0); // set fixed weather
            outf.Write(Weather.VisibilityM);
            outf.Write(Weather.CloudCoverFactor);
            outf.Write(Weather.PrecipitationIntensityPPSPM2);
            outf.Write(Weather.PrecipitationLiquidity);
            outf.Write(RandomizedWeather);
            outf.Write(weatherChangeOn);
            if (weatherChangeOn)
            {
                dynamicWeather.Save(outf);
            }
        }

        public virtual void RestoreWeatherParameters(BinaryReader inf)
        {
            int weathercontroltype = inf.ReadInt32();

            // restoring wrong type of weather - abort
            if (weathercontroltype != 0)
            {
                Trace.TraceError(Simulator.Catalog.GetString("Restoring wrong weather type : trying to restore dynamic weather but save contains user controlled weather"));
            }

            Weather.VisibilityM = inf.ReadSingle();
            Weather.CloudCoverFactor = inf.ReadSingle();
            Weather.PrecipitationIntensityPPSPM2 = inf.ReadSingle();
            Weather.PrecipitationLiquidity = inf.ReadSingle();
            RandomizedWeather = inf.ReadBoolean();
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
                case Orts.Formats.Msts.WeatherType.Clear: Weather.CloudCoverFactor = 0.05f; Weather.VisibilityM = 20000; break;
                case Orts.Formats.Msts.WeatherType.Rain: Weather.CloudCoverFactor = 0.7f; Weather.VisibilityM = 1000; break;
                case Orts.Formats.Msts.WeatherType.Snow: Weather.CloudCoverFactor = 0.6f; Weather.VisibilityM = 500; break;
            }
        }

        public void UpdateWeatherParameters()
        {
            Viewer.SoundProcess.RemoveSoundSources(this);
            switch (Viewer.Simulator.WeatherType)
            {
                case Orts.Formats.Msts.WeatherType.Clear: Weather.PrecipitationLiquidity = 1; Weather.PrecipitationIntensityPPSPM2 = 0; Viewer.SoundProcess.AddSoundSources(this, ClearSound); break;
                case Orts.Formats.Msts.WeatherType.Rain: Weather.PrecipitationLiquidity = 1; Weather.PrecipitationIntensityPPSPM2 = 0.010f; Viewer.SoundProcess.AddSoundSources(this, RainSound); break;
                case Orts.Formats.Msts.WeatherType.Snow: Weather.PrecipitationLiquidity = 0; Weather.PrecipitationIntensityPPSPM2 = 0.0050f; Viewer.SoundProcess.AddSoundSources(this, SnowSound); break;
            }

            Weather.WindAverageDirectionRad = (float)Viewer.Random.NextDouble() * MathHelper.TwoPi;
            Weather.WindAverageSpeedMpS = (float)Viewer.Random.NextDouble() * WeatherConstants.WindSpeedBeaufortMpS[WindSpeedBeaufort];
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
            if (PrecipitationViewer.IndexesAre32bit)
            {
                foreach (var soundSource in RainSound)
                {
                    soundSource.Volume = Weather.PrecipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
                }

                foreach (var soundSource in SnowSound)
                {
                    soundSource.Volume = Weather.PrecipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
                }
            }
            else
            {
                foreach (var soundSource in RainSound)
                {
                    soundSource.Volume = Weather.PrecipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2_16;
                }

                foreach (var soundSource in SnowSound)
                {
                    soundSource.Volume = Weather.PrecipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2_16;
                }
            }
        }

        private void UpdateWind(ElapsedTime elapsedTime)
        {
            WindUpdateTimer += elapsedTime.ClockSeconds;

            if (WindUpdateTimer > WindGustUpdateTimeS)
            {
                // Adjust instantaneous wind speed and direction
                var noisePos = (float)Viewer.Simulator.ClockTime / WindNoiseScale;
                Weather.WindInstantaneousDirectionRad = Weather.WindAverageDirectionRad + WindInstantaneousDirectionLimitRad * Noise.Generate(WindInstantaneousDirectionNoiseStart + noisePos);
                Weather.WindInstantaneousSpeedMpS = Math.Max(0, Weather.WindAverageSpeedMpS + WindInstantaneousSpeedLimit * Noise.Generate(WindInstantaneousSpeedNoiseStart + noisePos));

                // Reset wind gust timer
                WindUpdateTimer -= WindGustUpdateTimeS;
            }

            CameraWorldLocation = Viewer.Camera.CameraWorldLocation;
        }

        private bool RandomizeInitialWeather()
        {
            CheckDesertZone();
            if (DesertZone)
            {
                return false;
            }

            // First define overcast
            var randValue = Viewer.Random.Next(170);
            var intermValue = randValue >= 50 ? (float)(randValue - 50f) : (float)randValue;
            Weather.CloudCoverFactor = intermValue >= 20 ? (float)(intermValue - 20f) / 100f : (float)intermValue / 100f; // give more probability to less overcast
            Viewer.Simulator.WeatherType = Orts.Formats.Msts.WeatherType.Clear;

            // Then check if we are in precipitation zone
            if (Weather.CloudCoverFactor > 0.5)
            {
                randValue = Viewer.Random.Next(75);
                if (randValue > 40)
                {
                    Weather.PrecipitationIntensityPPSPM2 = (float)(randValue - 40f) / 1000f;
                    if (Viewer.Simulator.Season == SeasonType.Winter)
                    {
                        Viewer.Simulator.WeatherType = Orts.Formats.Msts.WeatherType.Snow;
                        Weather.PrecipitationLiquidity = 0;
                    }
                    else
                    {
                        Viewer.Simulator.WeatherType = Orts.Formats.Msts.WeatherType.Rain;
                        Weather.PrecipitationLiquidity = 1;
                    }
                }
                else
                {
                    Weather.PrecipitationIntensityPPSPM2 = 0;
                }
            }
            else
            {
                Weather.PrecipitationIntensityPPSPM2 = 0;
            }

            // and now define visibility
            randValue = Viewer.Random.Next(2000);
            if (Weather.PrecipitationIntensityPPSPM2 > 0 || Weather.CloudCoverFactor > 0.7f)
            {
                // use first digit to define power of ten and the other three to define the multiplying number
                Weather.VisibilityM = Math.Max(100, (float)Math.Pow(10, (int)(randValue / 1000) + 2) * (float)(((randValue % 1000) + 1) / 100f));
            }
            else
            {
                Weather.VisibilityM = Math.Max(500, (float)Math.Pow(10, (int)((randValue / 1000) + 3)) * (float)(((randValue % 1000) + 1) / 100f));
            }

            return true;
        }

        // TODO: Add several other weather conditions, such as PartlyCloudy, LightRain,
        // HeavySnow, etc. to the Options dialog as dropdown list boxes. Transfer user's
        // selection to RunActivity and make appropriate adjustments to the weather here.
        // This class will eventually be expanded to interpret dynamic weather scripts and
        // make game-time weather transitions.
        private void CheckDesertZone()
        {
            // Compute player train lat/lon in degrees
            double latitude = 0;
            double longitude = 0;
            var location = Viewer.PlayerLocomotive.Train.FrontTDBTraveller;
            new Orts.Common.WorldLatLon().ConvertWTC(location.TileX, location.TileZ, location.Location, ref latitude, ref longitude);
            float LatitudeDeg = MathHelper.ToDegrees((float)latitude);
            float LongitudeDeg = MathHelper.ToDegrees((float)longitude);

            // Compare player train lat/lon with array of desert zones
            for (int i = 0; i < DesertZones.Length / 4; i++)
            {
                if ((LatitudeDeg > DesertZones[i, 0] && LatitudeDeg < DesertZones[i, 1] && LongitudeDeg > DesertZones[i, 2] && LongitudeDeg < DesertZones[i, 3]
                     && Viewer.PlayerLocomotive.Train.FrontTDBTraveller.Location.Y < 1000) ||
                     (LatitudeDeg > DesertZones[i, 0] + 1 && LatitudeDeg < DesertZones[i, 1] - 1 && LongitudeDeg > DesertZones[i, 2] + 1 && LongitudeDeg < DesertZones[i, 3] - 1))
                {
                    DesertZone = true;
                    return;
                }
            }
        }

        [CallOnThread("Updater")]
        public virtual void Update(ElapsedTime elapsedTime)
        {
            Time += elapsedTime.ClockSeconds;
            var manager = MPManager.Instance();

            if (MPManager.IsClient() && manager.weatherChanged)
            {
                // Multiplayer weather has changed so we need to update our state to match weather, overcastFactor, pricipitationIntensity and fogDistance.
                if (manager.weather >= 0 && manager.weather != (int)Viewer.Simulator.WeatherType)
                {
                    Viewer.Simulator.WeatherType = (Orts.Formats.Msts.WeatherType)manager.weather;
                    UpdateWeatherParameters();
                }

                if (manager.overcastFactor >= 0)
                {
                    Weather.CloudCoverFactor = manager.overcastFactor;
                }

                if (manager.pricipitationIntensity >= 0)
                {
                    Weather.PrecipitationIntensityPPSPM2 = manager.pricipitationIntensity;
                    UpdateVolume();
                }

                if (manager.fogDistance >= 0)
                {
                    Weather.VisibilityM = manager.fogDistance;
                }

                // Reset the message now that we've applied all the changes.
                if ((manager.weather >= 0 && manager.weather != (int)Viewer.Simulator.WeatherType) || manager.overcastFactor >= 0 || manager.pricipitationIntensity >= 0 || manager.fogDistance >= 0)
                {
                    manager.weatherChanged = false;
                    manager.weather = -1;
                    manager.overcastFactor = -1;
                    manager.pricipitationIntensity = -1;
                    manager.fogDistance = -1;
                }
            }
            else if (!MPManager.IsClient())
            {
                // The user is able to change the weather for debugging. This will cycle through clear, rain and snow.
                if (UserInput.IsPressed(UserCommand.DebugWeatherChange))
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
                    if (dynamicWeather != null)
                    {
                        dynamicWeather.ResetWeatherTargets();
                    }

                    UpdateWeatherParameters();

                    // If we're a multiplayer server, send out the new weather to all clients.
                    if (MPManager.IsServer())
                    {
                        MPManager.Notify(new MSGWeather((int)Viewer.Simulator.WeatherType, -1, -1, -1).ToString());
                    }
                }

                // Overcast ranges from 0 (completely clear) to 1 (completely overcast).
                if (UserInput.IsDown(UserCommand.DebugOvercastIncrease))
                {
                    Weather.CloudCoverFactor = MathHelper.Clamp(Weather.CloudCoverFactor + (elapsedTime.RealSeconds / 10), 0, 1);
                    weatherChangeOn = false;
                    if (dynamicWeather != null)
                    {
                        dynamicWeather.ORTSOvercast = -1;
                    }
                }

                if (UserInput.IsDown(UserCommand.DebugOvercastDecrease))
                {
                    Weather.CloudCoverFactor = MathHelper.Clamp(Weather.CloudCoverFactor - (elapsedTime.RealSeconds / 10), 0, 1);
                    weatherChangeOn = false;
                    if (dynamicWeather != null)
                    {
                        dynamicWeather.ORTSOvercast = -1;
                    }
                }

                // Pricipitation ranges from 0 to max PrecipitationViewer.MaxIntensityPPSPM2 if 32bit.
                // 16bit uses PrecipitationViewer.MaxIntensityPPSPM2_16
                // 0xFFFF represents 65535 which is the max for 16bit devices.
                if (UserInput.IsDown(UserCommand.DebugPrecipitationIncrease))
                {
                    if (Viewer.Simulator.WeatherType == WeatherType.Clear)
                    {
                        Viewer.SoundProcess.RemoveSoundSources(this);
                        if (Weather.PrecipitationLiquidity > DynamicWeather.RainSnowLiquidityThreshold)
                        {
                            Viewer.Simulator.WeatherType = WeatherType.Rain;
                            Viewer.SoundProcess.AddSoundSources(this, RainSound);
                        }
                        else
                        {
                            Viewer.Simulator.WeatherType = WeatherType.Snow;
                            Viewer.SoundProcess.AddSoundSources(this, SnowSound);
                        }
                    }

                    Weather.PrecipitationIntensityPPSPM2 = MathHelper.Clamp(Weather.PrecipitationIntensityPPSPM2 * 1.05f, PrecipitationViewer.MinIntensityPPSPM2 + 0.0000001f, PrecipitationViewer.MaxIntensityPPSPM2);
                    weatherChangeOn = false;
                    if (dynamicWeather != null)
                    {
                        dynamicWeather.ORTSPrecipitationIntensity = -1;
                    }
                }

                if (UserInput.IsDown(UserCommand.DebugPrecipitationDecrease))
                {
                    Weather.PrecipitationIntensityPPSPM2 = MathHelper.Clamp(Weather.PrecipitationIntensityPPSPM2 / 1.05f, PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
                    if (Weather.PrecipitationIntensityPPSPM2 < PrecipitationViewer.MinIntensityPPSPM2 + 0.00001f)
                    {
                        Weather.PrecipitationIntensityPPSPM2 = 0;
                        if (Viewer.Simulator.WeatherType != WeatherType.Clear)
                        {
                            Viewer.SoundProcess.RemoveSoundSources(this);
                            Viewer.Simulator.WeatherType = WeatherType.Clear;
                            Viewer.SoundProcess.AddSoundSources(this, ClearSound);
                        }
                    }

                    weatherChangeOn = false;
                    if (dynamicWeather != null)
                    {
                        dynamicWeather.ORTSPrecipitationIntensity = -1;
                    }
                }

                if (UserInput.IsDown(UserCommand.DebugPrecipitationIncrease) || UserInput.IsDown(UserCommand.DebugPrecipitationDecrease))
                {
                    UpdateVolume();
                }

                // Change in precipitation liquidity, passing from rain to snow and vice-versa
                if (UserInput.IsDown(UserCommand.DebugPrecipitationLiquidityIncrease))
                {
                    Weather.PrecipitationLiquidity = MathHelper.Clamp(Weather.PrecipitationLiquidity + 0.01f, 0, 1);
                    weatherChangeOn = false;
                    if (dynamicWeather != null)
                    {
                        dynamicWeather.ORTSPrecipitationLiquidity = -1;
                    }

                    if (Weather.PrecipitationLiquidity > DynamicWeather.RainSnowLiquidityThreshold && Viewer.Simulator.WeatherType != WeatherType.Rain
                        && Weather.PrecipitationIntensityPPSPM2 > 0)
                    {
                        Viewer.Simulator.WeatherType = WeatherType.Rain;
                        Viewer.SoundProcess.RemoveSoundSources(this);
                        Viewer.SoundProcess.AddSoundSources(this, RainSound);
                    }
                }

                if (UserInput.IsDown(UserCommand.DebugPrecipitationLiquidityDecrease))
                {
                    Weather.PrecipitationLiquidity = MathHelper.Clamp(Weather.PrecipitationLiquidity - 0.01f, 0, 1);
                    weatherChangeOn = false;
                    if (dynamicWeather != null)
                    {
                        dynamicWeather.ORTSPrecipitationLiquidity = -1;
                    }

                    if (Weather.PrecipitationLiquidity <= DynamicWeather.RainSnowLiquidityThreshold && Viewer.Simulator.WeatherType != WeatherType.Snow
                        && Weather.PrecipitationIntensityPPSPM2 > 0)
                    {
                        Viewer.Simulator.WeatherType = WeatherType.Snow;
                        Viewer.SoundProcess.RemoveSoundSources(this);
                        Viewer.SoundProcess.AddSoundSources(this, SnowSound);
                    }
                }

                if (UserInput.IsDown(UserCommand.DebugPrecipitationLiquidityIncrease) || UserInput.IsDown(UserCommand.DebugPrecipitationLiquidityDecrease))
                {
                    UpdateVolume();
                }

                // Fog ranges from 10m (can't see anything) to 100km (clear arctic conditions).
                if (UserInput.IsDown(UserCommand.DebugFogIncrease))
                {
                    Weather.VisibilityM = MathHelper.Clamp(Weather.VisibilityM - (elapsedTime.RealSeconds * Weather.VisibilityM), 10, 100000);
                    weatherChangeOn = false;
                    if (dynamicWeather != null)
                    {
                        dynamicWeather.ORTSFog = -1;
                    }
                }

                if (UserInput.IsDown(UserCommand.DebugFogDecrease))
                {
                    Weather.VisibilityM = MathHelper.Clamp(Weather.VisibilityM + (elapsedTime.RealSeconds * Weather.VisibilityM), 10, 100000);
                    if (dynamicWeather != null)
                    {
                        dynamicWeather.ORTSFog = -1;
                    }

                    weatherChangeOn = false;
                }

                UpdateWind(elapsedTime);
            }

            if (!Orts.MultiPlayer.MPManager.IsMultiPlayer())
            {
                // Shift the clock forwards or backwards at 1h-per-second.
                if (UserInput.IsDown(UserCommand.DebugClockForwards))
                {
                    Viewer.Simulator.ClockTime += elapsedTime.RealSeconds * 3600;
                }

                if (UserInput.IsDown(UserCommand.DebugClockBackwards))
                {
                    Viewer.Simulator.ClockTime -= elapsedTime.RealSeconds * 3600;
                }
            }

            // If we're a multiplayer server, send out the new overcastFactor, pricipitationIntensity and fogDistance to all clients.
            if (MPManager.IsServer())
            {
                if (UserInput.IsReleased(UserCommand.DebugOvercastIncrease) || UserInput.IsReleased(UserCommand.DebugOvercastDecrease)
                    || UserInput.IsReleased(UserCommand.DebugPrecipitationIncrease) || UserInput.IsReleased(UserCommand.DebugPrecipitationDecrease)
                    || UserInput.IsReleased(UserCommand.DebugFogIncrease) || UserInput.IsReleased(UserCommand.DebugFogDecrease))
                {
                    manager.SetEnvInfo(Weather.CloudCoverFactor, Weather.VisibilityM);
                    MPManager.Notify(new MSGWeather(-1, Weather.CloudCoverFactor, Weather.PrecipitationIntensityPPSPM2, Weather.VisibilityM).ToString());
                }
            }

            if (Program.Simulator != null && Program.Simulator.ActivityRun != null && Program.Simulator.ActivityRun.triggeredEventWrapper != null &&
               (Program.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.ORTSWeatherChange != null || Program.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.Outcomes.ORTSWeatherChange != null))
            {
                // Start a weather change sequence in activity mode
                // if not yet weather changes, create the instance
                if (dynamicWeather == null)
                {
                    dynamicWeather = new DynamicWeather();
                }

                var weatherChange = Program.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.ORTSWeatherChange ?? Program.Simulator.ActivityRun.triggeredEventWrapper.ParsedObject.Outcomes.ORTSWeatherChange;
                dynamicWeather.WeatherChange_Init(weatherChange, this);
                Program.Simulator.ActivityRun.triggeredEventWrapper = null;
            }

            if (weatherChangeOn)
            {
                // manage the weather change sequence
                dynamicWeather.WeatherChange_Update(elapsedTime, this);
            }

            if (RandomizedWeather && !weatherChangeOn) // time to prepare a new weather change
            {
                dynamicWeather.WeatherChange_NextRandomization(elapsedTime, this);
            }

            if (Weather.PrecipitationIntensityPPSPM2 == 0 && Viewer.Simulator.WeatherType != WeatherType.Clear)
            {
                Viewer.Simulator.WeatherType = Orts.Formats.Msts.WeatherType.Clear;
                UpdateWeatherParameters();
            }
            else if (Weather.PrecipitationIntensityPPSPM2 > 0 && Viewer.Simulator.WeatherType == WeatherType.Clear)
            {
                Viewer.Simulator.WeatherType = Weather.PrecipitationLiquidity > DynamicWeather.RainSnowLiquidityThreshold ? WeatherType.Rain : WeatherType.Snow;
                UpdateWeatherParameters();
            }
        }

        public class DynamicWeather
        {
            public const float RainSnowLiquidityThreshold = 0.3f;
            public float overcastChangeRate = 0;
            public float overcastTimer = 0;
            public float fogChangeRate = 0;
            public float fogTimer = 0;
            public float stableWeatherTimer = 0;
            public float precipitationIntensityChangeRate = 0;
            public float precipitationIntensityTimer = 0;
            public float precipitationIntensityDelayTimer = -1;
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
                outf.Write(stableWeatherTimer);
                outf.Write(precipitationIntensityDelayTimer);
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
                stableWeatherTimer = inf.ReadSingle();
                precipitationIntensityDelayTimer = inf.ReadSingle();
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
                    overcastChangeRate = overcastTimer > 0 ? (MathHelper.Clamp(ORTSOvercast, 0, 1.0f) - weatherControl.Weather.CloudCoverFactor) / ORTSOvercastTransitionTimeS : 0;
                    wChangeOn = true;
                }

                if (eventWeatherChange.ORTSFog >= 0 && eventWeatherChange.ORTSFogTransitionTimeS >= 0)
                {
                    ORTSFog = eventWeatherChange.ORTSFog;
                    ORTSFogTransitionTimeS = eventWeatherChange.ORTSFogTransitionTimeS;
                    fogTimer = (float)ORTSFogTransitionTimeS;
                    var fogFinalValue = MathHelper.Clamp(ORTSFog, 10, 100000);
                    fogDistanceIncreasing = false;
                    fogChangeRate = fogTimer > 0 ? (fogFinalValue - weatherControl.Weather.VisibilityM) / (ORTSFogTransitionTimeS * ORTSFogTransitionTimeS) : 0;
                    if (fogFinalValue > weatherControl.Weather.VisibilityM)
                    {
                        fogDistanceIncreasing = true;
                        fogChangeRate = -fogChangeRate;
                        if (fogTimer > 0)
                        {
                            ORTSFog = weatherControl.Weather.VisibilityM;
                        }
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
                    if (PrecipitationViewer.IndexesAre32bit)
                    {
                        precipitationIntensityChangeRate = precipitationIntensityTimer > 0 ? (MathHelper.Clamp(ORTSPrecipitationIntensity, 0, PrecipitationViewer.MaxIntensityPPSPM2)
                            - weatherControl.Weather.PrecipitationIntensityPPSPM2) / ORTSPrecipitationIntensityTransitionTimeS : 0;
                    }
                    else
                    {
                        precipitationIntensityChangeRate = precipitationIntensityTimer > 0 ? (MathHelper.Clamp(ORTSPrecipitationIntensity, 0, PrecipitationViewer.MaxIntensityPPSPM2_16)
                            - weatherControl.Weather.PrecipitationIntensityPPSPM2) / ORTSPrecipitationIntensityTransitionTimeS : 0;
                    }

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
                    if (overcastTimer <= 0)
                    {
                        overcastTimer = 0;
                    }
                    else
                    {
                        wChangeOn = true;
                    }

                    weatherControl.Weather.CloudCoverFactor = ORTSOvercast - (overcastTimer * overcastChangeRate);
                    if (overcastTimer == 0)
                    {
                        ORTSOvercast = -1;
                    }
                }

                if (ORTSFog >= 0)
                {
                    fogTimer -= elapsedTime.ClockSeconds;
                    if (fogTimer <= 0)
                    {
                        fogTimer = 0;
                    }
                    else
                    {
                        wChangeOn = true;
                    }

                    if (!fogDistanceIncreasing)
                    {
                        weatherControl.Weather.VisibilityM = ORTSFog - (fogTimer * fogTimer * fogChangeRate);
                    }
                    else
                    {
                        var fogTimerDifference = ORTSFogTransitionTimeS - fogTimer;
                        weatherControl.Weather.VisibilityM = ORTSFog - (fogTimerDifference * fogTimerDifference * fogChangeRate);
                    }

                    if (fogTimer == 0)
                    {
                        ORTSFog = -1;
                    }
                }

                if (ORTSPrecipitationIntensity >= 0 && precipitationIntensityDelayTimer == -1)
                {
                    precipitationIntensityTimer -= elapsedTime.ClockSeconds;
                    if (precipitationIntensityTimer <= 0)
                    {
                        precipitationIntensityTimer = 0;
                    }
                    else if (weatherControl.RandomizedWeather == false)
                    {
                        wChangeOn = true;
                    }

                    var oldPricipitationIntensityPPSPM2 = weatherControl.Weather.PrecipitationIntensityPPSPM2;
                    weatherControl.Weather.PrecipitationIntensityPPSPM2 = ORTSPrecipitationIntensity - (precipitationIntensityTimer * precipitationIntensityChangeRate);
                    if (weatherControl.Weather.PrecipitationIntensityPPSPM2 > 0)
                    {
                        if (oldPricipitationIntensityPPSPM2 == 0)
                        {
                            if (weatherControl.Weather.PrecipitationLiquidity > RainSnowLiquidityThreshold)
                            {
                                weatherControl.Viewer.Simulator.WeatherType = WeatherType.Rain;
                            }
                            else
                            {
                                weatherControl.Viewer.Simulator.WeatherType = WeatherType.Snow;
                            }

                            weatherControl.UpdateSoundSources();
                        }

                        weatherControl.UpdateVolume();
                    }

                    if (weatherControl.Weather.PrecipitationIntensityPPSPM2 == 0)
                    {
                        if (oldPricipitationIntensityPPSPM2 > 0)
                        {
                            weatherControl.Viewer.Simulator.WeatherType = WeatherType.Clear;
                            weatherControl.UpdateSoundSources();
                        }
                    }

                    if (precipitationIntensityTimer == 0)
                    {
                        ORTSPrecipitationIntensity = -1;
                    }
                }
                else if (ORTSPrecipitationIntensity >= 0 && precipitationIntensityDelayTimer > 0)
                {
                    precipitationIntensityDelayTimer -= elapsedTime.ClockSeconds;
                    if (precipitationIntensityDelayTimer <= 0)
                    {
                        precipitationIntensityDelayTimer = -1; // OK, now rain/snow can start
                        precipitationIntensityTimer = overcastTimer; // going in parallel now
                    }
                }

                if (ORTSPrecipitationLiquidity >= 0)
                {
                    precipitationLiquidityTimer -= elapsedTime.ClockSeconds;
                    if (precipitationLiquidityTimer <= 0)
                    {
                        precipitationLiquidityTimer = 0;
                    }
                    else
                    {
                        wChangeOn = true;
                    }

                    var oldPrecipitationLiquidity = weatherControl.Weather.PrecipitationLiquidity;
                    weatherControl.Weather.PrecipitationLiquidity = ORTSPrecipitationLiquidity - (precipitationLiquidityTimer * precipitationLiquidityChangeRate);
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

                    if (precipitationLiquidityTimer == 0)
                    {
                        ORTSPrecipitationLiquidity = -1;
                    }
                }

                if (stableWeatherTimer > 0)
                {
                    stableWeatherTimer -= elapsedTime.ClockSeconds;
                    if (stableWeatherTimer <= 0)
                    {
                        stableWeatherTimer = 0;
                    }
                    else
                    {
                        wChangeOn = true;
                    }
                }

                weatherControl.weatherChangeOn = wChangeOn;
            }

            public void WeatherChange_NextRandomization(ElapsedTime elapsedTime, WeatherControl weatherControl) // start next randomization
            {
                // define how much time transition will last
                var weatherChangeTimer = ((4 - weatherControl.Viewer.Settings.ActWeatherRandomizationLevel) * 600) +
                    Viewer.Random.Next((4 - weatherControl.Viewer.Settings.ActWeatherRandomizationLevel) * 600);

                // begin with overcast
                var randValue = Viewer.Random.Next(170);
                var intermValue = randValue >= 50 ? (float)(randValue - 50f) : (float)randValue;
                ORTSOvercast = intermValue >= 20 ? (float)(intermValue - 20f) / 100f : (float)intermValue / 100f; // give more probability to less overcast
                ORTSOvercastTransitionTimeS = weatherChangeTimer;
                overcastTimer = (float)ORTSOvercastTransitionTimeS;
                overcastChangeRate = overcastTimer > 0 ? (MathHelper.Clamp(ORTSOvercast, 0, 1.0f) - weatherControl.Weather.CloudCoverFactor) / ORTSOvercastTransitionTimeS : 0;

                // Then check if we are in precipitation zone
                if (ORTSOvercast > 0.5)
                {
                    randValue = Viewer.Random.Next(75);
                    if (randValue > 40)
                    {
                        ORTSPrecipitationIntensity = (float)(randValue - 40f) / 1000f;
                        if (weatherControl.Viewer.Simulator.Season == SeasonType.Winter)
                        {
                            weatherControl.Weather.PrecipitationLiquidity = 0;
                        }
                        else
                        {
                            weatherControl.Weather.PrecipitationLiquidity = 1;
                        }
                    }
                }

                if (weatherControl.Weather.PrecipitationIntensityPPSPM2 > 0 && ORTSPrecipitationIntensity == -1)
                {
                    ORTSPrecipitationIntensity = 0;

                    // must return to zero before overcast < 0.5
                    ORTSPrecipitationIntensityTransitionTimeS = (int)((0.5 - weatherControl.Weather.CloudCoverFactor) / overcastChangeRate);
                }

                if (weatherControl.Weather.PrecipitationIntensityPPSPM2 == 0 && ORTSPrecipitationIntensity > 0 && weatherControl.Weather.CloudCoverFactor < 0.5)
                {
                    // we will have precipitation now, but it must start after overcast is over 0.5
                    precipitationIntensityDelayTimer = (0.5f - weatherControl.Weather.CloudCoverFactor) / overcastChangeRate;
                }

                if (ORTSPrecipitationIntensity > 0)
                {
                    ORTSPrecipitationIntensityTransitionTimeS = weatherChangeTimer;
                }

                if (ORTSPrecipitationIntensity >= 0)
                {
                    precipitationIntensityTimer = (float)ORTSPrecipitationIntensityTransitionTimeS;
                    precipitationIntensityChangeRate = precipitationIntensityTimer > 0 ? (MathHelper.Clamp(ORTSPrecipitationIntensity, 0, PrecipitationViewer.MaxIntensityPPSPM2)
                        - weatherControl.Weather.PrecipitationIntensityPPSPM2) / ORTSPrecipitationIntensityTransitionTimeS : 0;
                }

                // and now define visibility
                randValue = Viewer.Random.Next(2000);
                if (ORTSPrecipitationIntensity > 0 || ORTSOvercast > 0.7f)
                {
                    // use first digit to define power of ten and the other three to define the multiplying number
                    ORTSFog = Math.Max(100, (float)Math.Pow(10, (int)(randValue / 1000) + 2) * (float)(((randValue % 1000) + 1) / 100f));
                }
                else
                {
                    ORTSFog = Math.Max(500, (float)Math.Pow(10, (int)((randValue / 1000) + 3)) * (float)(((randValue % 1000) + 1) / 100f));
                }

                ORTSFogTransitionTimeS = weatherChangeTimer;
                fogTimer = (float)ORTSFogTransitionTimeS;
                var fogFinalValue = MathHelper.Clamp(ORTSFog, 10, 100000);
                fogDistanceIncreasing = false;
                fogChangeRate = fogTimer > 0 ? (fogFinalValue - weatherControl.Weather.VisibilityM) / (ORTSFogTransitionTimeS * ORTSFogTransitionTimeS) : 0;
                if (fogFinalValue > weatherControl.Weather.VisibilityM)
                {
                    fogDistanceIncreasing = true;
                    fogChangeRate = -fogChangeRate;
                    ORTSFog = weatherControl.Weather.VisibilityM;
                }

                weatherControl.weatherChangeOn = true;
            }
        }
    }

    public class AutomaticWeather : WeatherControl
    {
        // Variables used for auto weather control
        // settings
        List<WeatherSetting> weatherDetails = new List<WeatherSetting>();

        // running values
        // general
        public int AWActiveIndex;                                        // active active index in waether list
        public float AWNextChangeTime;                                   // time for next change
        public float AWLastVisibility;                                   // visibility at end of previous weather
        public Orts.Formats.Msts.WeatherType AWPrecipitationActiveType;  // actual active precipitation

        // cloud
        public float AWOvercastCloudcover;                               // actual cloudcover
        public float AWOvercastCloudRateOfChangepS;                      // rate of change of cloudcover

        // precipitation
        public Orts.Formats.Msts.WeatherType AWPrecipitationRequiredType;// actual active precipitation
        public float AWPrecipitationTotalDuration;                       // actual total duration (seconds)
        public int AWPrecipitationTotalSpread;                           // actual number of periods with precipitation
        public float AWPrecipitationActualPPSPM2;                        // actual rate of precipitation (particals per second per square meter)
        public float AWPrecipitationRequiredPPSPM2;                      // required rate of precipitation
        public float AWPrecipitationRateOfChangePPSPM2PS;                // rate of change for rate of precipitation (particals per second per square meter per second)
        public float AWPrecipitationEndSpell;                            // end of present spell of precipitation (time in seconds)
        public float AWPrecipitationNextSpell;                           // start of next spell (time in seconds) (-1 if no further spells)
        public float AWPrecipitationStartRate;                           // rate of change at start of spell
        public float AWPrecipitationEndRate;                             // rate of change at end of spell

        // fog
        public float AWActualVisibility;                                    // actual fog visibility
        public float AWFogChangeRateMpS;                                 // required rate of change for fog
        public float AWFogLiftTime;                                      // start time of fog lifting to be clear at required time

        // wind
        public float AWPreviousWindSpeed;                                // windspeed at end of previous weather
        public float AWRequiredWindSpeed;                                // required wind speed at end of weather
        public float AWAverageWindSpeed;                                 // required average windspeed
        public float AWAverageWindGust;                                  // required average additional wind gust
        public float AWWindGustTime;                                     // time of next wind gust
        public float AWActualWindSpeed;                                  // actual wind speed
        public float AWWindSpeedChange;                                  // required change of wind speed
        public float AWRequiredWindDirection;                            // required wind direction at end of weather
        public float AWAverageWindDirection;                             // required average wind direction
        public float AWActualWindDirection;                              // actual wind direction
        public float AWWindDirectionChange;                              // required wind direction change

        public AutomaticWeather(Viewer viewer, string weatherFile, double realTime)
            : base(viewer)
        {
            // read weather details from file
            var WeatherFile = new WeatherFile(weatherFile);
            weatherDetails = WeatherFile.Changes;

            if (weatherDetails.Count == 0)
            {
                Trace.TraceWarning("Weather file contains no settings {0}", weatherFile);
            }
            else
            {
                CheckWeatherDetails();
            }

            // set initial weather parameters
            SetInitialWeatherParameters(realTime);
        }

        // dummy constructor for restore
        public AutomaticWeather(Viewer viewer)
            : base(viewer)
        {
        }

        // check weather details, set auto variables
        void CheckWeatherDetails()
        {
            float prevTime = 0;

            foreach (WeatherSetting weatherSet in weatherDetails)
            {
                TimeSpan acttime = new TimeSpan((long)(weatherSet.Time * 10000000));

                // check if time is in sequence
                if (weatherSet.Time < prevTime)
                {
                    Trace.TraceInformation("Invalid time value : time out of sequence : {0}", acttime.ToString());
                    weatherSet.Time = prevTime + 1;
                }

                prevTime = weatherSet.Time;

                // check settings
                if (weatherSet is WeatherSettingOvercast)
                {
                    WeatherSettingOvercast thisOvercast = weatherSet as WeatherSettingOvercast;
                    CheckValue(ref thisOvercast.Overcast, true, 0, 100, acttime, "Overcast");
                    CheckValue(ref thisOvercast.OvercastVariation, true, 0, 100, acttime, "Overcast Variation");
                    CheckValue(ref thisOvercast.OvercastRateOfChange, true, 0, 1, acttime, "Overcast Rate of Change");
                    CheckValue(ref thisOvercast.OvercastVisibilityM, false, 1000, 60000, acttime, "Overcast Visibility");
                }
                else if (weatherSet is WeatherSettingPrecipitation)
                {
                    WeatherSettingPrecipitation thisPrecipitation = weatherSet as WeatherSettingPrecipitation;

                    // clear spell
                    CheckValue(ref thisPrecipitation.Overcast, true, 0, 100, acttime, "Overcast");
                    CheckValue(ref thisPrecipitation.OvercastVariation, true, 0, 100, acttime, "Overcast Variation");
                    CheckValue(ref thisPrecipitation.OvercastRateOfChange, true, 0, 1, acttime, "Overcast Rate of Change");
                    CheckValue(ref thisPrecipitation.OvercastVisibilityM, false, 1000, 60000, acttime, "Overcast Visibility");

                    // precipitation
                    CheckValue(ref thisPrecipitation.PrecipitationDensity, true, 0, 1, acttime, "Precipitation Density");
                    CheckValue(ref thisPrecipitation.PrecipitationVariation, true, 0, 1, acttime, "Precipitation Variation");
                    CheckValue(ref thisPrecipitation.PrecipitationRateOfChange, true, 0, 1, acttime, "Precipitation Rate Of Change");
                    CheckValue(ref thisPrecipitation.PrecipitationProbability, true, 0, 100, acttime, "Precipitation Probability");
                    CheckValue(ref thisPrecipitation.PrecipitationSpread, false, 1, 1000, acttime, "Precipitation Spread");
                    CheckValue(ref thisPrecipitation.PrecipitationVisibilityAtMinDensityM, false, 100, thisPrecipitation.OvercastVisibilityM, acttime, "Precipitation Visibility At Min Density");
                    CheckValue(ref thisPrecipitation.PrecipitationVisibilityAtMaxDensityM, false, 100, thisPrecipitation.PrecipitationVisibilityAtMinDensityM, acttime, "Precipitation Visibility At Max Density");

                    // build up
                    CheckValue(ref thisPrecipitation.OvercastPrecipitationStart, true, thisPrecipitation.Overcast, 100, acttime, "Overcast Precipitation Start");
                    CheckValue(ref thisPrecipitation.OvercastBuildUp, true, 0, 1, acttime, "Overcast Build Up");
                    CheckValue(ref thisPrecipitation.PrecipitationStartPhaseS, false, 30, 240, acttime, "Precipitation Start Phase");

                    // dispersion
                    CheckValue(ref thisPrecipitation.OvercastDispersion, true, 0, 1, acttime, "Overcast Dispersion");
                    CheckValue(ref thisPrecipitation.PrecipitationEndPhaseS, false, 30, 360, acttime, "Precipitation End Phase");
                }
                else if (weatherSet is WeatherSettingFog)
                {
                    WeatherSettingFog thisFog = weatherSet as WeatherSettingFog;
                    CheckValue(ref thisFog.FogOvercast, true, 0, 100, acttime, "Fog Overcast");
                    CheckValue(ref thisFog.FogSetTimeS, false, 300, 3600, acttime, "Fog Set Time");
                    CheckValue(ref thisFog.FogLiftTimeS, false, 300, 3600, acttime, "Fog Lift Time");
                    CheckValue(ref thisFog.FogVisibilityM, false, 10, 20000, acttime, "Fog Visibility");
                }
            }
        }

        // check value, set random value if allowed and value not set
        void CheckValue(ref float setValue, bool randomize, float minValue, float maxValue, TimeSpan acttime, string description)
        {
            // overcast
            if (setValue < 0 && randomize)
            {
                setValue = (float)(Viewer.Random.Next((int)maxValue * 100) / 100);  // ensure there is a value if range is 0 - 1
            }
            else
            {
                float correctedValue = MathHelper.Clamp(setValue, minValue, maxValue);
                if (correctedValue != setValue)
                {
                    Trace.TraceInformation(
                        "Invalid value for {0} for weather at {1} : {2}; value must be between {3} and {4}, clamped to {5}",
                        description, acttime.ToString(), setValue, minValue, maxValue, correctedValue);
                    setValue = correctedValue;
                }
            }
        }

        // set initial weather parameters
        void SetInitialWeatherParameters(double realTime)
        {
            Time = (float)realTime;

            // find last valid weather change
            AWActiveIndex = 0;
            var passedTime = false;

            if (weatherDetails.Count == 0)
            {
                return;
            }

            for (var iIndex = 1; iIndex < weatherDetails.Count && !passedTime; iIndex++)
            {
                if (weatherDetails[iIndex].Time > Time)
                {
                    passedTime = true;
                    AWActiveIndex = iIndex - 1;
                }
            }

            // get last weather
#if DEBUG_AUTOWEATHER
            Trace.TraceInformation("Initial active weather : {0}", AWActiveIndex);
#endif
            WeatherSetting lastWeather = weatherDetails[AWActiveIndex];

            AWNextChangeTime = AWActiveIndex < (weatherDetails.Count - 1) ? weatherDetails[AWActiveIndex + 1].Time : (24 * 3600);
            int nextIndex = AWActiveIndex < (weatherDetails.Count - 1) ? AWActiveIndex + 1 : -1;

            // fog
            if (lastWeather is WeatherSettingFog)
            {
                WeatherSettingFog lastWeatherFog = lastWeather as WeatherSettingFog;
                float actualLiftingTime = (0.9f * lastWeatherFog.FogLiftTimeS) + (((float)Viewer.Random.Next(10) / 100) * lastWeatherFog.FogLiftTimeS); // defined time +- 10%
                AWFogLiftTime = AWNextChangeTime - actualLiftingTime;

                // check if fog is allready lifting
                if ((float)realTime > AWFogLiftTime && nextIndex > 1)
                {
                    float reqVisibility = GetWeatherVisibility(weatherDetails[nextIndex]);
                    float remainingFactor = ((float)realTime - AWNextChangeTime + actualLiftingTime) / actualLiftingTime;
                    AWActualVisibility = lastWeatherFog.FogVisibilityM + (remainingFactor * remainingFactor * (reqVisibility - lastWeatherFog.FogVisibilityM));
                    AWOvercastCloudcover = lastWeatherFog.FogOvercast / 100;
                }
                else
                {
                    StartFog(lastWeatherFog, (float)realTime, AWActiveIndex);
                }
            }

            // precipitation
            else if (lastWeather is WeatherSettingPrecipitation)
            {
                WeatherSettingPrecipitation lastWeatherPrecipitation = lastWeather as WeatherSettingPrecipitation;
                StartPrecipitation(lastWeatherPrecipitation, (float)realTime, true);
            }

            // cloudcover
            else if (lastWeather is WeatherSettingOvercast)
            {
                WeatherSettingOvercast lastWeatherOvercast = lastWeather as WeatherSettingOvercast;
                AWOvercastCloudcover = Math.Max(0, Math.Min(1, (lastWeatherOvercast.Overcast / 100) +
                    ((float)Viewer.Random.Next((int)(-0.5f * lastWeatherOvercast.OvercastVariation), (int)(0.5f * lastWeatherOvercast.OvercastVariation)) / 100)));
                AWActualVisibility = Weather.VisibilityM = lastWeatherOvercast.OvercastVisibilityM;

#if DEBUG_AUTOWEATHER
                Trace.TraceInformation("Visibility : {0}", Weather.FogDistance);
#endif

            }

            // set system weather parameters
            Viewer.SoundProcess.RemoveSoundSources(this);
            Viewer.Simulator.WeatherType = AWPrecipitationActiveType;

            switch (AWPrecipitationActiveType)
            {
                case WeatherType.Rain:
                    Weather.PrecipitationIntensityPPSPM2 = AWPrecipitationActualPPSPM2;
                    Weather.CloudCoverFactor = AWOvercastCloudcover;
                    Weather.VisibilityM = AWActualVisibility;
                    Viewer.SoundProcess.AddSoundSources(this, RainSound);
                    foreach (var soundSource in RainSound)
                    {
                        soundSource.Volume = Weather.PrecipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
                    }
#if DEBUG_AUTOWEATHER
                    Trace.TraceInformation("Weather type RAIN");
#endif
                    break;

                case WeatherType.Snow:
                    Weather.PrecipitationIntensityPPSPM2 = AWPrecipitationActualPPSPM2;
                    Weather.CloudCoverFactor = AWOvercastCloudcover;
                    Weather.VisibilityM = AWActualVisibility;
                    Viewer.SoundProcess.AddSoundSources(this, SnowSound);
                    foreach (var soundSource in SnowSound)
                    {
                        soundSource.Volume = Weather.PrecipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
                    }
#if DEBUG_AUTOWEATHER
                    Trace.TraceInformation("Weather type SNOW");
#endif
                    break;

                default:
                    Weather.PrecipitationIntensityPPSPM2 = 0;
                    Viewer.SoundProcess.AddSoundSources(this, ClearSound);
                    Weather.CloudCoverFactor = AWOvercastCloudcover;
                    Weather.VisibilityM = AWActualVisibility;
#if DEBUG_AUTOWEATHER
                    Trace.TraceInformation("Weather type CLEAR");
#endif
                    break;
            }

#if DEBUG_AUTOWEATHER
            Trace.TraceInformation("Overcast : {0}\nPrecipitation : {1}\n Visibility : {2}",
                Weather.OvercastFactor, Weather.PricipitationIntensityPPSPM2, Weather.FogDistance);
#endif
        }

        [CallOnThread("Updater")]
        public override void Update(ElapsedTime elapsedTime)
        {
            // not client and weather auto mode
            Time += elapsedTime.ClockSeconds;
            var fogActive = false;

            if (weatherDetails.Count == 0)
            {
                return;
            }

            WeatherSetting lastWeather = weatherDetails[AWActiveIndex];
            int nextIndex = AWActiveIndex < (weatherDetails.Count - 1) ? AWActiveIndex + 1 : -1;
            fogActive = false;

            // check for fog
            if (lastWeather is WeatherSettingFog)
            {
                WeatherSettingFog lastWeatherFog = lastWeather as WeatherSettingFog;
                CalculateFog(lastWeatherFog, nextIndex);
                fogActive = true;

                // if fog has lifted, change to next sequence
                if (Time > (AWNextChangeTime - lastWeatherFog.FogLiftTimeS) && AWActualVisibility >= 19999 && AWActiveIndex < (weatherDetails.Count - 1))
                {
                    fogActive = false;
                    AWNextChangeTime = Time - 1;  // force change to next weather
                }
            }

            // check for precipitation
            else if (lastWeather is WeatherSettingPrecipitation)
            {
                WeatherSettingPrecipitation lastWeatherPrecipitation = lastWeather as WeatherSettingPrecipitation;

                // precipitation not active
                if (AWPrecipitationActiveType == WeatherType.Clear)
                {
                    // if beyond start of next spell start precipitation
                    if (Time > AWPrecipitationNextSpell)
                    {
                        // if cloud has build up
                        if (AWOvercastCloudcover >= (lastWeatherPrecipitation.OvercastPrecipitationStart / 100))
                        {
                            StartPrecipitationSpell(lastWeatherPrecipitation, AWNextChangeTime);
                            CalculatePrecipitation(lastWeatherPrecipitation, elapsedTime);
                        }

                        // build up cloud
                        else
                        {
                            AWOvercastCloudcover = CalculateOvercast(lastWeatherPrecipitation.OvercastPrecipitationStart, 0, lastWeatherPrecipitation.OvercastBuildUp, elapsedTime);
                        }
                    }

                    // set overcast and visibility
                    else
                    {
                        AWOvercastCloudcover = CalculateOvercast(lastWeatherPrecipitation.Overcast, lastWeatherPrecipitation.OvercastVariation, lastWeatherPrecipitation.OvercastRateOfChange, elapsedTime);
                        if (Weather.VisibilityM > lastWeatherPrecipitation.OvercastVisibilityM)
                        {
                            AWActualVisibility = Weather.VisibilityM - (40 * elapsedTime.RealSeconds); // reduce visibility by 40 m/s
                        }
                        else if (Weather.VisibilityM < lastWeatherPrecipitation.OvercastVisibilityM)
                        {
                            AWActualVisibility = Weather.VisibilityM + (40 * elapsedTime.RealSeconds); // increase visibility by 40 m/s
                        }
                    }
                }

                // active precipitation
                // if beyond end of spell : decrease densitity, if density below minimum threshold stop precipitation
                else if (Time > AWPrecipitationEndSpell)
                {
                    StopPrecipitationSpell(lastWeatherPrecipitation, elapsedTime);

                    // if density dropped under min threshold precipitation has ended
                    if (AWPrecipitationActualPPSPM2 <= PrecipitationViewer.MinIntensityPPSPM2)
                    {
                        AWPrecipitationActiveType = WeatherType.Clear;
#if DEBUG_AUTOWEATHER
                        Trace.TraceInformation("Start of clear spell, duration : {0}", (AWPrecipitationNextSpell - Time));
                        TimeSpan wt = new TimeSpan((long)(AWPrecipitationNextSpell * 10000000));
                        Trace.TraceInformation("Next spell : {0}", wt.ToString());
#endif
                    }
                }

                // active precipitation : set density and related visibility
                else
                {
                    CalculatePrecipitation(lastWeatherPrecipitation, elapsedTime);
                }
            }

            // clear
            else if (lastWeather is WeatherSettingOvercast)
            {
                WeatherSettingOvercast lastWeatherOvercast = lastWeather as WeatherSettingOvercast;
                AWOvercastCloudcover = CalculateOvercast(lastWeatherOvercast.Overcast, lastWeatherOvercast.OvercastVariation, lastWeatherOvercast.OvercastRateOfChange, elapsedTime);
                if (AWActualVisibility > lastWeatherOvercast.OvercastVisibilityM)
                {
                    AWActualVisibility = Math.Max(lastWeatherOvercast.OvercastVisibilityM, AWActualVisibility - (40 * elapsedTime.RealSeconds)); // reduce visibility by 40 m/s
                }
                else if (AWActualVisibility < lastWeatherOvercast.OvercastVisibilityM)
                {
                    AWActualVisibility = Math.Min(lastWeatherOvercast.OvercastVisibilityM, AWActualVisibility + (40 * elapsedTime.RealSeconds)); // increase visibility by 40 m/s
                }
            }

            // set weather parameters
            Viewer.SoundProcess.RemoveSoundSources(this);
            Viewer.Simulator.WeatherType = AWPrecipitationActiveType;

            switch (AWPrecipitationActiveType)
            {
                case WeatherType.Rain:
                    Weather.PrecipitationIntensityPPSPM2 = AWPrecipitationActualPPSPM2;
                    Weather.CloudCoverFactor = AWOvercastCloudcover;
                    Weather.VisibilityM = AWActualVisibility;
                    Viewer.SoundProcess.AddSoundSources(this, RainSound);
                    foreach (var soundSource in RainSound)
                    {
                        soundSource.Volume = Weather.PrecipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
                    }

                    break;

                case WeatherType.Snow:
                    Weather.PrecipitationIntensityPPSPM2 = AWPrecipitationActualPPSPM2;
                    Weather.CloudCoverFactor = AWOvercastCloudcover;
                    Weather.VisibilityM = AWActualVisibility;
                    Viewer.SoundProcess.AddSoundSources(this, SnowSound);
                    foreach (var soundSource in SnowSound)
                    {
                        soundSource.Volume = Weather.PrecipitationIntensityPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2;
                    }

                    break;

                default:
                    Weather.PrecipitationIntensityPPSPM2 = 0;
                    Viewer.SoundProcess.AddSoundSources(this, ClearSound);
                    Weather.CloudCoverFactor = AWOvercastCloudcover;
                    Weather.VisibilityM = AWActualVisibility;
                    break;
            }

            // check for change in required weather
            // time to change but no change after midnight and further weather available
            if (Time < 24 * 3600 && Time > AWNextChangeTime && AWActiveIndex < (weatherDetails.Count - 1))
            {
                // if precipitation still active or fog not lifted, postpone change by one minute
                if (AWPrecipitationActiveType != WeatherType.Clear || fogActive)
                {
                    AWNextChangeTime += 60;
                }
                else
                {
                    // set final values of last weather
                    lastWeather.GenOvercast = Weather.CloudCoverFactor;
                    lastWeather.GenVisibility = AWLastVisibility = Weather.VisibilityM;
                    //lastWeater.AWGenWind = ??

                    AWActiveIndex++;
                    AWNextChangeTime = AWActiveIndex < (weatherDetails.Count - 2) ? weatherDetails[AWActiveIndex + 1].Time : 24 * 3600;

#if DEBUG_AUTOWEATHER
                    Trace.TraceInformation("Weather change : index {0}, type {1}", AWActiveIndex, weatherDetails[AWActiveIndex].GetType().ToString());
#endif

                    WeatherSetting nextWeather = weatherDetails[AWActiveIndex];
                    if (nextWeather is WeatherSettingFog)
                    {
                        StartFog(nextWeather as WeatherSettingFog, Time, AWActiveIndex);
                    }
                    else if (nextWeather is WeatherSettingPrecipitation)
                    {
                        StartPrecipitation(nextWeather as WeatherSettingPrecipitation, Time, false);
                    }
                }
            }
        }

        float GetWeatherVisibility(WeatherSetting weatherDetail)
        {
            float nextVisibility = Weather.VisibilityM; // present visibility
            if (weatherDetail is WeatherSettingFog)
            {
                WeatherSettingFog weatherFog = weatherDetail as WeatherSettingFog;
                nextVisibility = weatherFog.FogVisibilityM;
            }
            else if (weatherDetail is WeatherSettingOvercast)
            {
                WeatherSettingOvercast weatherOvercast = weatherDetail as WeatherSettingOvercast;
                nextVisibility = weatherOvercast.OvercastVisibilityM;
            }
            else if (weatherDetail is WeatherSettingPrecipitation)
            {
                WeatherSettingPrecipitation weatherPrecipitation = weatherDetail as WeatherSettingPrecipitation;
                nextVisibility = weatherPrecipitation.OvercastVisibilityM;
            }

            return nextVisibility;
        }

        void StartFog(WeatherSettingFog lastWeatherFog, float startTime, int activeIndex)
        {
            // fog fully set or fog at start of day
            if (startTime > (lastWeatherFog.Time + lastWeatherFog.FogSetTimeS) || activeIndex == 0)
            {
                AWActualVisibility = lastWeatherFog.FogVisibilityM;
            }

            // fog still setting
            else
            {
                float remainingFactor = (startTime - lastWeatherFog.Time + lastWeatherFog.FogSetTimeS) / lastWeatherFog.FogSetTimeS;
                AWActualVisibility = MathHelper.Clamp(AWActualVisibility - (remainingFactor * remainingFactor * (AWActualVisibility - lastWeatherFog.FogVisibilityM)), lastWeatherFog.FogVisibilityM, AWActualVisibility);
            }
        }

        void CalculateFog(WeatherSettingFog lastWeatherFog, int nextIndex)
        {
            if (AWFogLiftTime > 0 && Time > AWFogLiftTime && nextIndex > 0) // fog is lifting
            {
                float reqVisibility = GetWeatherVisibility(weatherDetails[nextIndex]);
                float remainingFactor = (Time - weatherDetails[nextIndex].Time + lastWeatherFog.FogLiftTimeS) / lastWeatherFog.FogLiftTimeS;
                AWActualVisibility = lastWeatherFog.FogVisibilityM + (remainingFactor * remainingFactor * (reqVisibility - lastWeatherFog.FogVisibilityM));
                AWOvercastCloudcover = lastWeatherFog.FogOvercast / 100;
            }
            else if (AWActualVisibility > lastWeatherFog.FogVisibilityM)
            {
                float remainingFactor = (Time - lastWeatherFog.Time + lastWeatherFog.FogSetTimeS) / lastWeatherFog.FogSetTimeS;
                AWActualVisibility = MathHelper.Clamp(AWLastVisibility - (remainingFactor * remainingFactor * (AWLastVisibility - lastWeatherFog.FogVisibilityM)), lastWeatherFog.FogVisibilityM, AWLastVisibility);
            }
        }

        void StartPrecipitation(WeatherSettingPrecipitation lastWeatherPrecipitation, float startTime, bool allowImmediateStart)
        {
            AWPrecipitationRequiredType = lastWeatherPrecipitation.PrecipitationType;

            // determine actual duration of precipitation
            float maxDuration = AWNextChangeTime - weatherDetails[AWActiveIndex].Time;
            AWPrecipitationTotalDuration = (float)maxDuration * (lastWeatherPrecipitation.PrecipitationProbability / 100f);  // nominal value
            AWPrecipitationTotalDuration = (0.9f + ((float)Viewer.Random.Next(20) / 20)) * AWPrecipitationTotalDuration; // randomized value, +- 10%
            AWPrecipitationTotalDuration = Math.Min(AWPrecipitationTotalDuration, maxDuration); // but never exceeding maximum duration
            AWPrecipitationNextSpell = lastWeatherPrecipitation.Time; // set start of spell to start of weather change

            // determine spread : no. of periods with precipitation (no. of showers)
            if (lastWeatherPrecipitation.PrecipitationSpread == 1)
            {
                AWPrecipitationTotalSpread = 1;
            }
            else
            {
                AWPrecipitationTotalSpread = Math.Max(1, (int)((0.9f + ((float)Viewer.Random.Next(20) / 20)) * lastWeatherPrecipitation.PrecipitationSpread));
                if ((AWPrecipitationTotalDuration / AWPrecipitationTotalSpread) < 900) // length of spell at least 15 mins
                {
                    AWPrecipitationTotalSpread = (int)(AWPrecipitationTotalDuration / 900);
                }
            }

            // determine actual precipitation state - only if immediate start allowed
            bool precipitationActive = allowImmediateStart && Viewer.Random.Next(100) >= lastWeatherPrecipitation.PrecipitationProbability;

#if DEBUG_AUTOWEATHER
            Trace.TraceInformation("Precipitation active on start : {0}", precipitationActive.ToString());
#endif

            // determine total remaining time as well as remaining periods, based on start/end time and present time
            // this is independent from actual precipitation state
            if (AWPrecipitationTotalSpread > 1)
            {
                AWPrecipitationTotalDuration = ((float)((AWNextChangeTime - startTime) / (AWNextChangeTime - weatherDetails[AWActiveIndex].Time))) * AWPrecipitationTotalDuration;
                AWPrecipitationTotalSpread = (int)(((float)((AWNextChangeTime - startTime) / (AWNextChangeTime - weatherDetails[AWActiveIndex].Time))) * AWPrecipitationTotalSpread);
            }

            // set actual details
            if (precipitationActive)
            {
                // precipitation active : set actual details, calculate end of present spell
                int precvariation = (int)(lastWeatherPrecipitation.PrecipitationVariation * 100);
                float baseDensitiy = PrecipitationViewer.MaxIntensityPPSPM2 * lastWeatherPrecipitation.PrecipitationDensity;
                AWPrecipitationActualPPSPM2 = MathHelper.Clamp(
                    (1.0f + ((float)Viewer.Random.Next(-precvariation, precvariation) / 100)) * baseDensitiy,
                    PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
                AWPrecipitationRequiredPPSPM2 = MathHelper.Clamp(
                    (1.0f + ((float)Viewer.Random.Next(-precvariation, precvariation) / 100)) * baseDensitiy,
                    PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);

                // rate of change is max. difference over random timespan between 1 and 10 mins.
                // startphase
                float startrate = (1.75f * lastWeatherPrecipitation.PrecipitationRateOfChange) +
                                           (0.5F * (float)Viewer.Random.Next((int)(lastWeatherPrecipitation.PrecipitationRateOfChange * 100)) / 100f);
                float spellStartPhase = Math.Min(60f + (300f * startrate), 600);
                AWPrecipitationStartRate = (AWPrecipitationRequiredPPSPM2 - AWPrecipitationActualPPSPM2) / spellStartPhase;

                // endphase
                float endrate = (1.75f * lastWeatherPrecipitation.PrecipitationRateOfChange) +
                               (0.5F * (float)Viewer.Random.Next((int)(lastWeatherPrecipitation.PrecipitationRateOfChange * 100)) / 100f);
                float spellEndPhase = Math.Min(60f + (300f * endrate), 600);

                float avduration = AWPrecipitationTotalDuration / AWPrecipitationTotalSpread;
                float actduration = (0.5f + ((float)Viewer.Random.Next(100) / 100)) * avduration;
                float spellEndTime = Math.Min(startTime + actduration, AWNextChangeTime);
                AWPrecipitationEndSpell = Math.Max(startTime, spellEndTime - spellEndPhase);

                // for end rate, use minimum precipitation
                AWPrecipitationEndRate = (AWPrecipitationActualPPSPM2 - PrecipitationViewer.MinIntensityPPSPM2) / spellEndPhase;
                AWPrecipitationTotalDuration -= actduration;
                AWPrecipitationTotalSpread -= 1;

                // calculate length of clear period and start of next spell
                if (AWPrecipitationTotalDuration > 0 && AWPrecipitationTotalSpread > 0)
                {
                    float avclearspell = (AWNextChangeTime - startTime - AWPrecipitationTotalDuration) / AWPrecipitationTotalSpread;
                    AWPrecipitationNextSpell = spellEndTime + ((0.9f + ((float)Viewer.Random.Next(200) / 1000f)) * avclearspell);
                }
                else
                {
                    AWPrecipitationNextSpell = AWNextChangeTime + 1; // set beyond next weather such that it never occurs
                }

                // set active values
                AWPrecipitationActiveType = lastWeatherPrecipitation.PrecipitationType;
                AWOvercastCloudcover = lastWeatherPrecipitation.OvercastPrecipitationStart / 100;  // fixed cloudcover during precipitation
                AWActualVisibility = lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM + (float)(Math.Sqrt(AWPrecipitationActualPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2) *
                    (lastWeatherPrecipitation.PrecipitationVisibilityAtMaxDensityM - lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM));
                AWLastVisibility = lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM; // fix last visibility to visibility at minimum density
            }
            else
            {
                // if presently not active, set start of next spell
                if (AWPrecipitationTotalSpread < 1)
                {
                    AWPrecipitationNextSpell = -1;
                }
                else
                {
                    int clearSpell = (int)((AWNextChangeTime - startTime - AWPrecipitationTotalDuration) / AWPrecipitationTotalSpread);
                    AWPrecipitationNextSpell = clearSpell > 0 ? startTime + Viewer.Random.Next(clearSpell) : startTime;

                    if (allowImmediateStart)
                    {
                        AWOvercastCloudcover = lastWeatherPrecipitation.Overcast / 100;
                        AWActualVisibility = lastWeatherPrecipitation.OvercastVisibilityM;
                    }

#if DEBUG_AUTOWEATHER
                    TimeSpan wt = new TimeSpan((long)(AWPrecipitationNextSpell * 10000000));
                    Trace.TraceInformation("Next spell : {0}", wt.ToString());
#endif
                }

                AWPrecipitationActiveType = WeatherType.Clear;
            }
        }

        void StartPrecipitationSpell(WeatherSettingPrecipitation lastWeatherPrecipitation, float nextWeatherTime)
        {
            int precvariation = (int)(lastWeatherPrecipitation.PrecipitationVariation * 100);
            float baseDensitiy = PrecipitationViewer.MaxIntensityPPSPM2 * lastWeatherPrecipitation.PrecipitationDensity;
            AWPrecipitationActiveType = AWPrecipitationRequiredType;
            AWPrecipitationActualPPSPM2 = PrecipitationViewer.MinIntensityPPSPM2;
            AWPrecipitationRequiredPPSPM2 = MathHelper.Clamp(
                (1.0f + ((float)Viewer.Random.Next(-precvariation, precvariation) / 100)) * baseDensitiy,
                PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
            AWLastVisibility = Weather.VisibilityM;

            // rate of change at start is max. difference over defined time span +- 10%, scaled between 1/2 and 4 mins
            float startphase = MathHelper.Clamp(lastWeatherPrecipitation.PrecipitationStartPhaseS * (0.9f + (Viewer.Random.Next(100) / 1000)), 30, 240);
            AWPrecipitationStartRate = (AWPrecipitationRequiredPPSPM2 - AWPrecipitationActualPPSPM2) / startphase;
            AWPrecipitationRateOfChangePPSPM2PS = AWPrecipitationStartRate;

            // rate of change at end is max. difference over defined time span +- 10%, scaled between 1/2 and 6 mins
            float endphase = MathHelper.Clamp(lastWeatherPrecipitation.PrecipitationEndPhaseS * (0.9f + (Viewer.Random.Next(100) / 1000)), 30, 360);
            AWPrecipitationEndRate = (AWPrecipitationRequiredPPSPM2 - AWPrecipitationActualPPSPM2) / endphase;

            // calculate end of spell and start of next spell
            if (AWPrecipitationTotalSpread > 1)
            {
                float avduration = AWPrecipitationTotalDuration / AWPrecipitationTotalSpread;
                float actduration = (0.5f + ((float)Viewer.Random.Next(100) / 100)) * avduration;
                float spellEndTime = Math.Min(Time + actduration, AWNextChangeTime);
                AWPrecipitationEndSpell = Math.Max(Time, spellEndTime - endphase);

                AWPrecipitationTotalDuration -= actduration;
                AWPrecipitationTotalSpread -= 1;

                int clearSpell = (int)((nextWeatherTime - spellEndTime - AWPrecipitationTotalDuration) / AWPrecipitationTotalSpread);
                AWPrecipitationNextSpell = spellEndTime + 60f; // always a minute between spells
                AWPrecipitationNextSpell = clearSpell > 0 ? AWPrecipitationNextSpell + Viewer.Random.Next(clearSpell) : AWPrecipitationNextSpell;
            }
            else
            {
                AWPrecipitationEndSpell = Math.Max(Time, nextWeatherTime - endphase);
            }

#if DEBUG_AUTOWEATHER
            Trace.TraceInformation("Start next spell, duration : {0} , start phase : {1} , end phase {2}, density {3} (of max. {4}) , rate of change : {5} - {6} - {7}",
                                    (AWPrecipitationEndSpell - Time), startphase, endphase, AWPrecipitationRequiredPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2, 
                                    AWPrecipitationRateOfChangePPSPM2PS, AWPrecipitationStartRate, AWPrecipitationEndRate);
#endif

        }

        void CalculatePrecipitation(WeatherSettingPrecipitation lastWeatherPrecipitation, ElapsedTime elapsedTime)
        {
            if (AWPrecipitationActualPPSPM2 < AWPrecipitationRequiredPPSPM2)
            {
                AWPrecipitationActualPPSPM2 = Math.Min(AWPrecipitationRequiredPPSPM2, AWPrecipitationActualPPSPM2 + (AWPrecipitationRateOfChangePPSPM2PS * elapsedTime.RealSeconds));
            }
            else if (AWPrecipitationActualPPSPM2 > AWPrecipitationRequiredPPSPM2)
            {
                AWPrecipitationActualPPSPM2 = Math.Max(AWPrecipitationRequiredPPSPM2, AWPrecipitationActualPPSPM2 - (AWPrecipitationRateOfChangePPSPM2PS * elapsedTime.RealSeconds));
            }
            else
            {
                AWPrecipitationRateOfChangePPSPM2PS = (lastWeatherPrecipitation.PrecipitationRateOfChange / 120) * (PrecipitationViewer.MaxIntensityPPSPM2 - PrecipitationViewer.MinIntensityPPSPM2);
                int precvariation = (int)(lastWeatherPrecipitation.PrecipitationVariation * 100);
                float baseDensitiy = PrecipitationViewer.MaxIntensityPPSPM2 * lastWeatherPrecipitation.PrecipitationDensity;
                AWPrecipitationRequiredPPSPM2 = MathHelper.Clamp(
                    (1.0f + ((float)Viewer.Random.Next(-precvariation, precvariation) / 100)) * baseDensitiy,
                    PrecipitationViewer.MinIntensityPPSPM2, PrecipitationViewer.MaxIntensityPPSPM2);
#if DEBUG_AUTOWEATHER
                Trace.TraceInformation("New density : {0}", AWPrecipitationRequiredPPSPM2);
#endif

                AWLastVisibility = lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM; // reach required density, so from now on visibility is determined by density
            }

            // calculate visibility - use last visibility which is either visibility at start of precipitation (at start of spell) or visibility at minimum density (after reaching required density)
            float reqVisibility = lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM + ((float)Math.Sqrt(AWPrecipitationRequiredPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2) *
                (lastWeatherPrecipitation.PrecipitationVisibilityAtMaxDensityM - lastWeatherPrecipitation.PrecipitationVisibilityAtMinDensityM));
            AWActualVisibility = AWLastVisibility + (float)(Math.Sqrt(AWPrecipitationActualPPSPM2 / AWPrecipitationRequiredPPSPM2) *
                (reqVisibility - AWLastVisibility));
        }

        void StopPrecipitationSpell(WeatherSettingPrecipitation lastWeatherPrecipitation, ElapsedTime elapsedTime)
        {
            AWPrecipitationActualPPSPM2 = Math.Max(PrecipitationViewer.MinIntensityPPSPM2, AWPrecipitationActualPPSPM2 - (AWPrecipitationEndRate * elapsedTime.RealSeconds));
            AWActualVisibility = AWLastVisibility +
                (float)(Math.Sqrt(AWPrecipitationActualPPSPM2 / PrecipitationViewer.MaxIntensityPPSPM2) * (lastWeatherPrecipitation.PrecipitationVisibilityAtMaxDensityM - AWLastVisibility));
            AWOvercastCloudcover = CalculateOvercast(lastWeatherPrecipitation.Overcast, 0, lastWeatherPrecipitation.OvercastDispersion, elapsedTime);
        }

        float CalculateOvercast(float requiredOvercast, float overcastVariation, float overcastRateOfChange, ElapsedTime elapsedTime)
        {
            float requiredOvercastFactor = requiredOvercast / 100f;
            if (overcastRateOfChange == 0)
            {
                AWOvercastCloudRateOfChangepS = ((float)Viewer.Random.Next(50) / (100 * 300)) * (0.8f + ((float)Viewer.Random.Next(100) / 250));
            }
            else
            {
                AWOvercastCloudRateOfChangepS = (overcastRateOfChange / 300) * (0.8f + ((float)Viewer.Random.Next(100) / 250));
            }

            if (AWOvercastCloudcover < requiredOvercastFactor)
            {
                float newOvercast = Math.Min(requiredOvercastFactor, Weather.CloudCoverFactor + (AWOvercastCloudRateOfChangepS * elapsedTime.RealSeconds));
                return newOvercast;
            }
            else if (Weather.CloudCoverFactor > requiredOvercastFactor)
            {
                float newOvercast = Math.Max(requiredOvercastFactor, Weather.CloudCoverFactor - (AWOvercastCloudRateOfChangepS * elapsedTime.RealSeconds));
                return newOvercast;
            }
            else
            {
                float newOvercast = Math.Max(0, Math.Min(1, requiredOvercastFactor + ((float)Viewer.Random.Next((int)(-0.5f * overcastVariation), (int)(0.5f * overcastVariation)) / 100)));
                return newOvercast;
            }
        }

        public override void SaveWeatherParameters(BinaryWriter outf)
        {
            // set indication to automatic weather
            outf.Write(1);

            // save input details
            foreach (WeatherSetting autoweather in weatherDetails)
            {
                if (autoweather is WeatherSettingFog)
                {
                    WeatherSettingFog autofog = autoweather as WeatherSettingFog;
                    autofog.Save(outf);
                }
                else if (autoweather is WeatherSettingPrecipitation)
                {
                    WeatherSettingPrecipitation autoprec = autoweather as WeatherSettingPrecipitation;
                    autoprec.Save(outf);
                }
                else if (autoweather is WeatherSettingOvercast)
                {
                    WeatherSettingOvercast autoovercast = autoweather as WeatherSettingOvercast;
                    autoovercast.Save(outf);
                }
            }

            outf.Write("end");

            outf.Write(AWActiveIndex);
            outf.Write(AWNextChangeTime);

            outf.Write(AWActualVisibility);
            outf.Write(AWLastVisibility);
            outf.Write(AWFogLiftTime);
            outf.Write(AWFogChangeRateMpS);

            outf.Write((int)AWPrecipitationActiveType);
            outf.Write(AWPrecipitationActualPPSPM2);
            outf.Write(AWPrecipitationRequiredPPSPM2);
            outf.Write(AWPrecipitationRateOfChangePPSPM2PS);
            outf.Write(AWPrecipitationTotalDuration);
            outf.Write(AWPrecipitationTotalSpread);
            outf.Write(AWPrecipitationEndSpell);
            outf.Write(AWPrecipitationNextSpell);
            outf.Write(AWPrecipitationStartRate);
            outf.Write(AWPrecipitationEndRate);

            outf.Write(AWOvercastCloudcover);
            outf.Write(AWOvercastCloudRateOfChangepS);

            outf.Write(Weather.CloudCoverFactor);
            outf.Write(Weather.VisibilityM);
            outf.Write(Weather.PrecipitationIntensityPPSPM2);
        }

        public override void RestoreWeatherParameters(BinaryReader inf)
        {
            int weathercontroltype = inf.ReadInt32();

            // restoring wrong type of weather - abort
            if (weathercontroltype != 1)
            {
                Trace.TraceError(Simulator.Catalog.GetString("Restoring wrong weather type : trying to restore user controlled weather but save contains dynamic weather"));
            }

            weatherDetails.Clear();

            string readtype = inf.ReadString();
            bool endread = false;

            while (!endread)
            {
                if (string.Equals(readtype, "fog"))
                {
                    WeatherSettingFog autofog = new WeatherSettingFog(inf);
                    weatherDetails.Add(autofog);
                }
                else if (string.Equals(readtype, "precipitation"))
                {
                    WeatherSettingPrecipitation autoprec = new WeatherSettingPrecipitation(inf);
                    weatherDetails.Add(autoprec);
                }
                else if (string.Equals(readtype, "overcast"))
                {
                    WeatherSettingOvercast autoovercast = new WeatherSettingOvercast(inf);
                    weatherDetails.Add(autoovercast);
                }
                else if (string.Equals(readtype, "end"))
                {
                    endread = true;
                }
                else
                {
                    // error - nothing to report it here
                    endread = true;
                }

                if (!endread)
                {
                    readtype = inf.ReadString();
                }
            }

            AWActiveIndex = inf.ReadInt32();
            AWNextChangeTime = inf.ReadSingle();

            AWActualVisibility = inf.ReadSingle();
            AWLastVisibility = inf.ReadSingle();
            AWFogLiftTime = inf.ReadSingle();
            AWFogChangeRateMpS = inf.ReadSingle();

            AWPrecipitationActiveType = (WeatherType)inf.ReadInt32();
            AWPrecipitationActualPPSPM2 = inf.ReadSingle();
            AWPrecipitationRequiredPPSPM2 = inf.ReadSingle();
            AWPrecipitationRateOfChangePPSPM2PS = inf.ReadSingle();
            AWPrecipitationTotalDuration = inf.ReadSingle();
            AWPrecipitationTotalSpread = inf.ReadInt32();
            AWPrecipitationEndSpell = inf.ReadSingle();
            AWPrecipitationNextSpell = inf.ReadSingle();
            AWPrecipitationStartRate = inf.ReadSingle();
            AWPrecipitationEndRate = inf.ReadSingle();

            AWOvercastCloudcover = inf.ReadSingle();
            AWOvercastCloudRateOfChangepS = inf.ReadSingle();

            Weather.CloudCoverFactor = inf.ReadSingle();
            Weather.VisibilityM = inf.ReadSingle();
            Weather.PrecipitationIntensityPPSPM2 = inf.ReadSingle();

            Time = (float)Viewer.Simulator.ClockTime;
        }
    }
}
