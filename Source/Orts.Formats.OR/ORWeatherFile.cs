// COPYRIGHT 2014 by the Open Rails project.
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

// IMPORTANT : method contains class definitions only until JSON file processing can be added
//             class definitions are used in weather functions as these are prepared to process variable weather
//

namespace Orts.Formats.OR
{
    /// <summary>
    ///
    /// class ORWeatherFile
    /// </summary>

    public class ORWeatherFile
    {
        public List<AutoWeatherDetails> weatherDetails;
        public float AWTimeVariance;             // allowed max variation using random time setting
        public bool AWRandomSequence = false;    // set random sequence

        // TODO : to be created when JSON processing is available
        public ORWeatherFile()
        {
        }
    }

    public class AutoWeatherDetails
    {
        public float AWTime;                                                 // time of change
        public float AWGenOvercast;                                          // cloud cover, copied from actual type when terminating
        public float AWGenVisibility;                                        // visibility, copied from actual type when terminating
    }

    public class AutoWeatherOvercast : AutoWeatherDetails
    {
        public float AWOvercast;                                         // required overcast - range : 0 - 100 (percentage)
        public float AWOvercastVariation;                                // variation in overcast - range : 0 - 100 (percentage change)
        public float AWOvercastRateOfChange;                             // overcast rate of change - range : 0 - 1 (scaling factor)
        public float AWOvercastVisibility;                               // required visibility - range 1000 - 60000 (for lower values use fog)

        // TO BE DEFINED when JSON processing is available
        public AutoWeatherOvercast()
        {
        }

        // restore
        public AutoWeatherOvercast(BinaryReader inf)
        {
            AWTime = inf.ReadSingle();
            AWOvercast = inf.ReadSingle();
            AWOvercastVariation = inf.ReadSingle();
            AWOvercastRateOfChange = inf.ReadSingle();
            AWOvercastVisibility = inf.ReadSingle();
        }

        // save
        public void Save(BinaryWriter outf)
        {
            outf.Write("overcast");
            outf.Write(AWTime);
            outf.Write(AWOvercast);
            outf.Write(AWOvercastVariation);
            outf.Write(AWOvercastRateOfChange);
            outf.Write(AWOvercastVisibility);
        }
    }

    // precipitation

    public class AutoWeatherPrecipitation : AutoWeatherDetails
    {
        // precipitation spell
        public Orts.Formats.Msts.WeatherType AWPrecipitationType;        // required precipitation : rain or snow
        public float AWPrecipitationDensity;                             // precipitation density - range 0 - 1
        public float AWPrecipitationVariation;                           // precipitation density variation - range 0 - 1
        public float AWPrecipitationRateOfChange;                        // precipitation rate of change - range 0 - 1
        public float AWPrecipitationProbability;                         // precipitation probability - range : 0 - 100
        public float AWPrecipitationSpread;                              // precipitation average continuity - range : 1 - ...
        public float AWPrecipitationVisibilityAtMinDensity;              // visibility during precipitation at min density
        public float AWPrecipitationVisibilityAtMaxDensity;              // visibility during precipitation at max density

        // build up to precipitation
        public float AWOvercastPrecipitationStart;                       // required overcast to start precipitation, also overcast during precipitation - range 0 - 100
        public float AWOvercastBuildUp;                                  // overcast rate of change ahead of precipitation spell - range : 0 - 1
        public float AWPrecipitationStartPhase;                          // measure for duration of start phase (from dry to full density) - range : 0 - 1

        // dispersion after precipitation
        public float AWOvercastDispersion;                               // overcast rate of change after precipitation spell - range : 0 - 1
        public float AWPrecipitationEndPhase;                            // measure for duration of end phase (from full density to dry) - range : 0 - 1

        // clear spell
        public float AWOvercast;                                         // required overcast in clear spells - range : 0 - 100
        public float AWOvercastVariation;                                // variation in overcast - range : 0 - 100
        public float AWOvercastRateOfChange;                             // overcast rate of change - range : 0 - 1
        public float AWOvercastVisibility;                               // visibility during clear spells

        // TO BE DEFINED when JSON processing is available
        public AutoWeatherPrecipitation()
        {
        }

        // restore
        public AutoWeatherPrecipitation(BinaryReader inf)
        {
            AWTime = inf.ReadSingle();
            AWPrecipitationType = (Orts.Formats.Msts.WeatherType)inf.ReadInt32();
            AWPrecipitationDensity = inf.ReadSingle();
            AWPrecipitationVariation = inf.ReadSingle();
            AWPrecipitationRateOfChange = inf.ReadSingle();
            AWPrecipitationProbability = inf.ReadSingle();
            AWPrecipitationSpread = inf.ReadSingle();
            AWPrecipitationVisibilityAtMinDensity = inf.ReadSingle();
            AWPrecipitationVisibilityAtMaxDensity = inf.ReadSingle();

            AWOvercastPrecipitationStart = inf.ReadSingle();
            AWOvercastBuildUp = inf.ReadSingle();
            AWPrecipitationStartPhase = inf.ReadSingle();

            AWOvercastDispersion = inf.ReadSingle();
            AWPrecipitationEndPhase = inf.ReadSingle();

            AWOvercast = inf.ReadSingle();
            AWOvercastVariation = inf.ReadSingle();
            AWOvercastRateOfChange = inf.ReadSingle();
            AWOvercastVisibility = inf.ReadSingle();
        }

        // save
        public void Save(BinaryWriter outf)
        {
            outf.Write("precipitation");
            outf.Write(AWTime);
            outf.Write((int)AWPrecipitationType);
            outf.Write(AWPrecipitationDensity);
            outf.Write(AWPrecipitationVariation);
            outf.Write(AWPrecipitationRateOfChange);
            outf.Write(AWPrecipitationProbability);
            outf.Write(AWPrecipitationSpread);
            outf.Write(AWPrecipitationVisibilityAtMinDensity);
            outf.Write(AWPrecipitationVisibilityAtMaxDensity);

            outf.Write(AWOvercastPrecipitationStart);
            outf.Write(AWOvercastBuildUp);
            outf.Write(AWPrecipitationStartPhase);

            outf.Write(AWOvercastDispersion);
            outf.Write(AWPrecipitationEndPhase);

            outf.Write(AWOvercast);
            outf.Write(AWOvercastVariation);
            outf.Write(AWOvercastRateOfChange);
            outf.Write(AWOvercastVisibility);
        }
    }

    // fog
    public class AutoWeatherFog : AutoWeatherDetails
    {
        public float AWFogVisibilityM;                                   // required fog density - range 0 - 20000
        public float AWFogSetTimeS;                                      // required rate for fog setting - range 300 - 3600
        public float AWFogLiftTimeS;                                     // required rate for fog lifting - range 300 - 3600 - required visibility is taken from next weather
        public float AWFogOvercast;                                      // required overcast after fog lifted - range 0 - 100

        // TO BE DEFINED when JSON processing is available
        public AutoWeatherFog()
        {
        }

        public AutoWeatherFog(BinaryReader inf)
        {
            AWTime = inf.ReadSingle();
            AWFogVisibilityM = inf.ReadSingle();
            AWFogSetTimeS = inf.ReadSingle();
            AWFogLiftTimeS = inf.ReadSingle();
            AWFogOvercast = inf.ReadSingle();
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write("fog");
            outf.Write(AWTime);
            outf.Write(AWFogVisibilityM);
            outf.Write(AWFogSetTimeS);
            outf.Write(AWFogLiftTimeS);
            outf.Write(AWFogOvercast);
        }
    }
}




