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

namespace Orts.Formats.OR
{
    /// <summary>
    ///
    /// class ORWeatherFile
    /// </summary>

    public class WeatherFile
    {
        public List<WeatherSetting> Settings = new List<WeatherSetting>();
        public float TimeVariance;             // allowed max variation using random time setting
        public bool RandomSequence;            // set random sequence

        // TODO : to be created when JSON processing is available
        public WeatherFile(string fileName)
        {
        }
    }

    public class WeatherSetting
    {
        public float Time;                                                 // time of change
        public float GenOvercast;                                          // cloud cover, copied from actual type when terminating
        public float GenVisibility;                                        // visibility, copied from actual type when terminating
    }

    public class WeatherSettingOvercast : WeatherSetting
    {
        public float Overcast;                                         // required overcast - range : 0 - 100 (percentage)
        public float OvercastVariation;                                // variation in overcast - range : 0 - 100 (percentage change)
        public float OvercastRateOfChange;                             // overcast rate of change - range : 0 - 1 (scaling factor)
        public float OvercastVisibility;                               // required visibility - range 1000 - 60000 (for lower values use fog)

        // TO BE DEFINED when JSON processing is available
        public WeatherSettingOvercast()
        {
        }

        // restore
        public WeatherSettingOvercast(BinaryReader inf)
        {
            Time = inf.ReadSingle();
            Overcast = inf.ReadSingle();
            OvercastVariation = inf.ReadSingle();
            OvercastRateOfChange = inf.ReadSingle();
            OvercastVisibility = inf.ReadSingle();
        }

        // save
        public void Save(BinaryWriter outf)
        {
            outf.Write("overcast");
            outf.Write(Time);
            outf.Write(Overcast);
            outf.Write(OvercastVariation);
            outf.Write(OvercastRateOfChange);
            outf.Write(OvercastVisibility);
        }
    }

    // precipitation

    public class WeatherSettingPrecipitation : WeatherSetting
    {
        // precipitation spell
        public Orts.Formats.Msts.WeatherType PrecipitationType;        // required precipitation : rain or snow
        public float PrecipitationDensity;                             // precipitation density - range 0 - 1
        public float PrecipitationVariation;                           // precipitation density variation - range 0 - 1
        public float PrecipitationRateOfChange;                        // precipitation rate of change - range 0 - 1
        public float PrecipitationProbability;                         // precipitation probability - range : 0 - 100
        public float PrecipitationSpread;                              // precipitation average continuity - range : 1 - ...
        public float PrecipitationVisibilityAtMinDensity;              // visibility during precipitation at min density
        public float PrecipitationVisibilityAtMaxDensity;              // visibility during precipitation at max density

        // build up to precipitation
        public float OvercastPrecipitationStart;                       // required overcast to start precipitation, also overcast during precipitation - range 0 - 100
        public float OvercastBuildUp;                                  // overcast rate of change ahead of precipitation spell - range : 0 - 1
        public float PrecipitationStartPhase;                          // measure for duration of start phase (from dry to full density) - range : 0 - 1

        // dispersion after precipitation
        public float OvercastDispersion;                               // overcast rate of change after precipitation spell - range : 0 - 1
        public float PrecipitationEndPhase;                            // measure for duration of end phase (from full density to dry) - range : 0 - 1

        // clear spell
        public float Overcast;                                         // required overcast in clear spells - range : 0 - 100
        public float OvercastVariation;                                // variation in overcast - range : 0 - 100
        public float OvercastRateOfChange;                             // overcast rate of change - range : 0 - 1
        public float OvercastVisibility;                               // visibility during clear spells

        // TO BE DEFINED when JSON processing is available
        public WeatherSettingPrecipitation()
        {
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
            PrecipitationVisibilityAtMinDensity = inf.ReadSingle();
            PrecipitationVisibilityAtMaxDensity = inf.ReadSingle();

            OvercastPrecipitationStart = inf.ReadSingle();
            OvercastBuildUp = inf.ReadSingle();
            PrecipitationStartPhase = inf.ReadSingle();

            OvercastDispersion = inf.ReadSingle();
            PrecipitationEndPhase = inf.ReadSingle();

            Overcast = inf.ReadSingle();
            OvercastVariation = inf.ReadSingle();
            OvercastRateOfChange = inf.ReadSingle();
            OvercastVisibility = inf.ReadSingle();
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
            outf.Write(PrecipitationVisibilityAtMinDensity);
            outf.Write(PrecipitationVisibilityAtMaxDensity);

            outf.Write(OvercastPrecipitationStart);
            outf.Write(OvercastBuildUp);
            outf.Write(PrecipitationStartPhase);

            outf.Write(OvercastDispersion);
            outf.Write(PrecipitationEndPhase);

            outf.Write(Overcast);
            outf.Write(OvercastVariation);
            outf.Write(OvercastRateOfChange);
            outf.Write(OvercastVisibility);
        }
    }

    // fog
    public class WeatherSettingFog : WeatherSetting
    {
        public float FogVisibilityM;                                   // required fog density - range 0 - 20000
        public float FogSetTimeS;                                      // required rate for fog setting - range 300 - 3600
        public float FogLiftTimeS;                                     // required rate for fog lifting - range 300 - 3600 - required visibility is taken from next weather
        public float FogOvercast;                                      // required overcast after fog lifted - range 0 - 100

        // TO BE DEFINED when JSON processing is available
        public WeatherSettingFog()
        {
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
