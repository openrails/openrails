// COPYRIGHT 2017, 2018 by the Open Rails project.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Orts.Parsers.OR;

namespace Orts.Formats.OR
{
    /// <summary>
    ///
    /// class ORWeatherFile
    /// </summary>

    public class WeatherFile
    {
        public List<WeatherSetting> Changes = new List<WeatherSetting>();
        public float TimeVariance;             // allowed max variation using random time setting
        public bool RandomSequence;            // set random sequence

        public WeatherFile(string fileName)
        {
            JsonReader.ReadFile(fileName, TryParse);
        }

        protected virtual bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "":
                case "Changes[].":
                    // Ignore these items.
                    break;
                case "Changes[].Type":
                    switch (item.AsString(""))
                    {
                        case "Clear":
                            Changes.Add(new WeatherSettingOvercast(item));
                            break;
                        case "Precipitation":
                            Changes.Add(new WeatherSettingPrecipitation(item));
                            break;
                        case "Fog":
                            Changes.Add(new WeatherSettingFog(item));
                            break;
                        default: return false;
                    }
                    break;
                default: return false;
            }
            return true;
        }
    }

    public class WeatherSetting
    {
        public float Time;                                                 // time of change
        public float GenOvercast;                                          // cloud cover, copied from actual type when terminating
        public float GenVisibility;                                        // visibility, copied from actual type when terminating

        protected virtual bool TryParse(JsonReader item)
        {
            switch (item.Path)
            {
                case "Time": Time = item.AsTime(Time); break;
                default: return false;
            }
            return true;
        }
    }

    public class WeatherSettingOvercast : WeatherSetting
    {
        public float Overcast;                                         // required overcast - range : 0 - 100 (percentage)
        public float OvercastVariation;                                // variation in overcast - range : 0 - 100 (percentage change)
        public float OvercastRateOfChange;                             // overcast rate of change - range : 0 - 1 (scaling factor)
        public float OvercastVisibilityM = 60000;                      // required visibility - range 1000 - 60000 (for lower values use fog)

        public WeatherSettingOvercast(JsonReader json)
        {
            json.ReadBlock(TryParse);
        }

        protected override bool TryParse(JsonReader item)
        {

            // get values
            if (base.TryParse(item)) return true;
            switch (item.Path)
            {
                case "Overcast": Overcast = item.AsFloat(Overcast); break;
                case "OvercastVariation": OvercastVariation = item.AsFloat(OvercastVariation); break;
                case "OvercastRateOfChange": OvercastRateOfChange = item.AsFloat(OvercastRateOfChange); break;
                case "OvercastVisibility": OvercastVisibilityM = item.AsFloat(OvercastVisibilityM); break;
                default: return false;
            }

            return true;
        }

        // restore
        public WeatherSettingOvercast(BinaryReader inf)
        {
            Time = inf.ReadSingle();
            Overcast = inf.ReadSingle();
            OvercastVariation = inf.ReadSingle();
            OvercastRateOfChange = inf.ReadSingle();
            OvercastVisibilityM = inf.ReadSingle();
        }

        // save
        public void Save(BinaryWriter outf)
        {
            outf.Write("overcast");
            outf.Write(Time);
            outf.Write(Overcast);
            outf.Write(OvercastVariation);
            outf.Write(OvercastRateOfChange);
            outf.Write(OvercastVisibilityM);
        }
    }

    // precipitation

    public class WeatherSettingPrecipitation : WeatherSetting
    {
        // precipitation spell
        public Msts.WeatherType PrecipitationType = Msts.WeatherType.Clear;    // required precipitation : rain or snow
        public float PrecipitationDensity;                             // precipitation density - range 0 - 1
        public float PrecipitationVariation;                           // precipitation density variation - range 0 - 1
        public float PrecipitationRateOfChange;                        // precipitation rate of change - range 0 - 1
        public float PrecipitationProbability;                         // precipitation probability - range : 0 - 100
        public float PrecipitationSpread = 1;                          // precipitation average continuity - range : 1 - ...
        public float PrecipitationVisibilityAtMinDensityM = 20000;     // visibility during precipitation at min density
        public float PrecipitationVisibilityAtMaxDensityM = 10000;     // visibility during precipitation at max density

        // build up to precipitation
        public float OvercastPrecipitationStart;                       // required overcast to start precipitation, also overcast during precipitation - range 0 - 100
        public float OvercastBuildUp;                                  // overcast rate of change ahead of precipitation spell - range : 0 - 1
        public float PrecipitationStartPhaseS = 60;                    // measure for duration of start phase (from dry to full density) - range : 30 to 240 (secs)

        // dispersion after precipitation
        public float OvercastDispersion;                               // overcast rate of change after precipitation spell - range : 0 - 1
        public float PrecipitationEndPhaseS = 60;                      // measure for duration of end phase (from full density to dry) - range : 30 to 360 (secs)

        // clear spell
        public float Overcast;                                         // required overcast in clear spells - range : 0 - 100
        public float OvercastVariation;                                // variation in overcast - range : 0 - 100
        public float OvercastRateOfChange;                             // overcast rate of change - range : 0 - 1
        public float OvercastVisibilityM = 60000;                      // visibility during clear spells

        public WeatherSettingPrecipitation(JsonReader json)
        {
            json.ReadBlock(TryParse);
        }

        protected override bool TryParse(JsonReader item)
        {
            // read items
            if (base.TryParse(item)) return true;
            switch (item.Path)
            {
                case "PrecipitationType": PrecipitationType = item.AsEnum(PrecipitationType); break;
                case "PrecipitationDensity": PrecipitationDensity = item.AsFloat(PrecipitationDensity); break;
                case "PrecipitationVariation": PrecipitationVariation = item.AsFloat(PrecipitationVariation); break;
                case "PrecipitationRateOfChange": PrecipitationRateOfChange = item.AsFloat(PrecipitationRateOfChange); break;
                case "PrecipitationProbability": PrecipitationProbability = item.AsFloat(PrecipitationProbability); break;
                case "PrecipitationSpread": PrecipitationSpread = item.AsFloat(PrecipitationSpread); break;
                case "PrecipitationVisibilityAtMinDensity": PrecipitationVisibilityAtMinDensityM = item.AsFloat(PrecipitationVisibilityAtMinDensityM); break;
                case "PrecipitationVisibilityAtMaxDensity": PrecipitationVisibilityAtMaxDensityM = item.AsFloat(PrecipitationVisibilityAtMaxDensityM); break;

                case "OvercastPrecipitationStart": OvercastPrecipitationStart = item.AsFloat(OvercastPrecipitationStart); break;
                case "OvercastBuildUp": OvercastBuildUp = item.AsFloat(OvercastBuildUp); break;
                case "PrecipitationStartPhase": PrecipitationStartPhaseS = item.AsFloat(PrecipitationStartPhaseS); break;

                case "OvercastDispersion": OvercastDispersion = item.AsFloat(OvercastDispersion); break;
                case "PrecipitationEndPhase": PrecipitationEndPhaseS = item.AsFloat(PrecipitationEndPhaseS); break;

                case "Overcast": Overcast = item.AsFloat(Overcast); break;
                case "OvercastVariation": OvercastVariation = item.AsFloat(OvercastVariation); break;
                case "OvercastRateOfChange": OvercastRateOfChange = item.AsFloat(OvercastRateOfChange); break;
                case "OvercastVisibility": OvercastVisibilityM = item.AsFloat(OvercastVisibilityM); break;
                default: return false;
            }

            return true;
        }

        // restore
        public WeatherSettingPrecipitation(BinaryReader inf)
        {
            Time = inf.ReadSingle();
            PrecipitationType = (Orts.Formats.Msts.WeatherType)inf.ReadInt32();
            PrecipitationDensity = inf.ReadSingle();
            PrecipitationVariation = inf.ReadSingle();
            PrecipitationRateOfChange = inf.ReadSingle();
            PrecipitationProbability = inf.ReadSingle();
            PrecipitationSpread = inf.ReadSingle();
            PrecipitationVisibilityAtMinDensityM = inf.ReadSingle();
            PrecipitationVisibilityAtMaxDensityM = inf.ReadSingle();

            OvercastPrecipitationStart = inf.ReadSingle();
            OvercastBuildUp = inf.ReadSingle();
            PrecipitationStartPhaseS = inf.ReadSingle();

            OvercastDispersion = inf.ReadSingle();
            PrecipitationEndPhaseS = inf.ReadSingle();

            Overcast = inf.ReadSingle();
            OvercastVariation = inf.ReadSingle();
            OvercastRateOfChange = inf.ReadSingle();
            OvercastVisibilityM = inf.ReadSingle();
        }

        // save
        public void Save(BinaryWriter outf)
        {
            outf.Write("precipitation");
            outf.Write(Time);
            outf.Write((int)PrecipitationType);
            outf.Write(PrecipitationDensity);
            outf.Write(PrecipitationVariation);
            outf.Write(PrecipitationRateOfChange);
            outf.Write(PrecipitationProbability);
            outf.Write(PrecipitationSpread);
            outf.Write(PrecipitationVisibilityAtMinDensityM);
            outf.Write(PrecipitationVisibilityAtMaxDensityM);

            outf.Write(OvercastPrecipitationStart);
            outf.Write(OvercastBuildUp);
            outf.Write(PrecipitationStartPhaseS);

            outf.Write(OvercastDispersion);
            outf.Write(PrecipitationEndPhaseS);

            outf.Write(Overcast);
            outf.Write(OvercastVariation);
            outf.Write(OvercastRateOfChange);
            outf.Write(OvercastVisibilityM);
        }
    }

    // fog
    public class WeatherSettingFog : WeatherSetting
    {
        public float FogVisibilityM = 1000;                            // required fog density - range 0 - 1000
        public float FogSetTimeS = 300;                                // required rate for fog setting - range 300 - 3600
        public float FogLiftTimeS = 300;                               // required rate for fog lifting - range 300 - 3600 - required visibility is taken from next weather
        public float FogOvercast;                                      // required overcast after fog lifted - range 0 - 100

        public WeatherSettingFog(JsonReader json)
        {
            json.ReadBlock(TryParse);
        }

        protected override bool TryParse(JsonReader item)
        {
        if (base.TryParse(item)) return true;
            switch (item.Path)
            {
                case "FogVisibility": FogVisibilityM = item.AsFloat(FogVisibilityM); break;
                case "FogSetTime": FogSetTimeS = item.AsFloat(FogSetTimeS); break;
                case "FogLiftTime": FogLiftTimeS = item.AsFloat(FogLiftTimeS); break;
                case "FogOvercast": FogOvercast = item.AsFloat(FogOvercast); break;
                default: return false;
            }

            return true;
        }

        public WeatherSettingFog(BinaryReader inf)
        {
            Time = inf.ReadSingle();
            FogVisibilityM = inf.ReadSingle();
            FogSetTimeS = inf.ReadSingle();
            FogLiftTimeS = inf.ReadSingle();
            FogOvercast = inf.ReadSingle();
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write("fog");
            outf.Write(Time);
            outf.Write(FogVisibilityM);
            outf.Write(FogSetTimeS);
            outf.Write(FogLiftTimeS);
            outf.Write(FogOvercast);
        }
    }
}
