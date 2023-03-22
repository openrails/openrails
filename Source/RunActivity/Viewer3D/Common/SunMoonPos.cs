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

/*
    * Contains equations to calculate the sun and moon position for any latitude
    * and longitude on any date
    * Solar position equations are NOAA equations adapted from "Astronomical Algorithms," by Jean Meeus,
    * Lunar equations are adapted by Keith Burnett from the US Naval Observatory Astronomical Almanac.
*/

using System;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;

namespace Orts.Viewer3D.Common
{
    static class SunMoonPos
    {
        /// <summary>
        /// Calculates the solar direction vector.
        /// Used for locating the sun graphic and as the location of the main scenery light source.
        /// </summary>
        /// <param name="latitude">Latitude, radians.</param>
        /// <param name="longitude">Longitude, radians.</param>
        /// <param name="sun">Sun satellite information (including rise/set time).</param>
        /// <param name="clockTime">Wall clock time since start of activity, days.</param>
        /// <param name="date">Structure made up of day, month, year and ordinal date.</param>
        /// <returns>A normalize 3D vector indicating the sun's position from the viewer's location.</returns>
        public static Vector3 SolarAngle(double latitude, double longitude, EnvironmentFile.SkySatellite sun, float clockTime, SkyViewer.SkyDate date)
        {
            Vector3 sunDirection;
            double solarHourAngle;

            // Fractional year, radians
            var fYear = (MathHelper.TwoPi / 365) * (date.OrdinalDate - 1 + (clockTime - 0.5));

            // Solar declination, radians
            var solarDeclination = 0.006918
                - (0.399912 * Math.Cos(fYear))
                + (0.070257 * Math.Sin(fYear))
                - (0.006758 * Math.Cos(2 * fYear))
                + (0.000907 * Math.Sin(2 * fYear))
                - (0.002697 * Math.Cos(3 * fYear))
                + (0.001480 * Math.Sin(3 * fYear));

            // How much the latitude and solar declination changes the horizon so that we can correctly calculate
            // the solar angle based on sun rise/set times
            var horizonSolarHourAngle = Math.Acos(-(Math.Sin(latitude) * Math.Sin(solarDeclination)) / (Math.Cos(latitude) * Math.Cos(solarDeclination)));

            if (sun?.RiseTime != 0 && sun?.SetTime != 0 && sun?.RiseTime < sun?.SetTime && !double.IsNaN(horizonSolarHourAngle))
            {
                var noonTimeD = (float)(sun.RiseTime + sun.SetTime) / 2 / 86400;
                var riseSetScale = 90 / (noonTimeD - ((float)sun.RiseTime / 86400));
                solarHourAngle = MathHelper.ToRadians((clockTime - noonTimeD) * riseSetScale * (float)(horizonSolarHourAngle / MathHelper.PiOver2));
            }
            else
            {
                // For these calculations, west longitude is in positive degrees
                var noaaLongitude = -MathHelper.ToDegrees((float)longitude);

                // Equation of time, minutes
                var eqTime = 229.18 * (0.000075
                    + (0.001868 * Math.Cos(fYear))
                    - (0.032077 * Math.Sin(fYear))
                    - (0.014615 * Math.Cos(2 * fYear))
                    - (0.040849 * Math.Sin(2 * fYear)));

                // Time offset at present longitude, minutes
                var timeOffset = eqTime - (4 * noaaLongitude) + (60 * Math.Round(noaaLongitude / 15));

                // True solar time, minutes (since midnight)
                var trueSolar = (clockTime * 24 * 60) + timeOffset;

                // Solar hour angle, radians
                solarHourAngle = MathHelper.ToRadians((float)(trueSolar / 4) - 180);
            }

            var solarZenithCosine = (Math.Sin(latitude) * Math.Sin(solarDeclination)) + (Math.Cos(latitude) * Math.Cos(solarDeclination) * Math.Cos(solarHourAngle));
            var solarZenithSine = Math.Sin(Math.Acos(solarZenithCosine));
            var solarAzimuthCosine = (Math.Sin(solarDeclination) - (solarZenithCosine * Math.Sin(latitude))) / (Math.Sin(Math.Acos(solarZenithCosine)) * Math.Cos(latitude));
            var solarAzimuthSine = -Math.Sin(solarHourAngle) * Math.Cos(solarDeclination) / Math.Sin(Math.Acos(solarZenithCosine));

            sunDirection.X = (float)(solarZenithSine * solarAzimuthSine);
            sunDirection.Y = (float)solarZenithCosine;
            sunDirection.Z = -(float)(solarZenithSine * solarAzimuthCosine);
            sunDirection.Normalize();

            return sunDirection;
        }

        /// <summary>
        /// Calculates the lunar direction vector.
        /// </summary>
        /// <param name="latitude">Latitude, radians.</param>
        /// <param name="longitude">Longitude, radians.</param>
        /// <param name="clockTime">Wall clock time since start of activity, days.</param>
        /// <param name="date">Structure made up of day, month, year and ordinal date.</param>
        /// <returns>A normalize 3D vector indicating the moon's position from the viewer's location.</returns>
        public static Vector3 LunarAngle(double latitude, double longitude, float clockTime, SkyViewer.SkyDate date)
        {
            Vector3 moonDirection;

            // Julian day.
            var jEday = (367f * date.Year) - (7 * (date.Year + ((date.Month + 9) / 12)) / 4) + (275 * date.Month / 9) + date.Day - 730531.5 + clockTime;

            // Fractional time within current 100-year epoch, days
            var fTime = jEday / 36525;

            // Ecliptic longitude, radians
            var geoLong = Normalize(
                Normalize(3.8104 + (8399.71 * fTime), MathHelper.TwoPi)
                + (0.10978 * Math.Sin(Normalize(2.3544 + (8328.69 * fTime), MathHelper.TwoPi)))
                - (0.02216 * Math.Sin(Normalize(4.5239 - (7214.12 * fTime), MathHelper.TwoPi)))
                + (0.0115 * Math.Sin(Normalize(4.11374 + (15542.75 * fTime), MathHelper.TwoPi)))
                + (0.003665 * Math.Sin(Normalize(4.7106 + (16657.38 * fTime), MathHelper.TwoPi)))
                - (0.003316 * Math.Sin(Normalize(6.2396 + (628.3 * fTime), MathHelper.TwoPi)))
                - (0.00192 * Math.Sin(Normalize(3.2568 + (16866.93 * fTime), MathHelper.TwoPi))), MathHelper.TwoPi);

            // Ecliptic latitude, radians
            var geoLat = (0.08954 * Math.Sin(Normalize(1.6284 + (8433.47 * fTime), MathHelper.TwoPi)))
                + (0.004887 * Math.Sin(Normalize(3.9828 + (16762.16 * fTime), MathHelper.TwoPi)))
                - (0.004887 * Math.Sin(Normalize(5.5554 + (104.76 * fTime), MathHelper.TwoPi)))
                - (0.002967 * Math.Sin(Normalize(3.7978 - (7109.29 * fTime), MathHelper.TwoPi)));

            // Parallax, radians
            var parallax = 0.0165946
                + (0.0009041 * Math.Cos(Normalize(2.3544 + (832869 * fTime), MathHelper.TwoPi)))
                + (0.000166 * Math.Cos(Normalize(4.5238 - (7214.06 * fTime), MathHelper.TwoPi)))
                + (0.000136 * Math.Cos(Normalize(4.1137 + (15542.75 * fTime), MathHelper.TwoPi)))
                + (0.000489 * Math.Cos(Normalize(4.7106 + (16657.38 * fTime), MathHelper.TwoPi)));

            // Geocentric distance, dimensionless
            var geoDist = 1 / Math.Sin(parallax);

            // Geocentric vector coordinates, dimensionless
            var geoX = geoDist * Math.Cos(geoLong) * Math.Cos(geoLat);
            var geoY = geoDist * Math.Sin(geoLong) * Math.Cos(geoLat);
            var geoZ = geoDist * Math.Sin(geoLat);

            // Mean obliquity of ecliptic, radians
            var ecliptic = 0.4091 - (0.00000000623 * jEday);

            // Equator of ordinalDate vector coordinates, dimensionless
            var eclX = geoX;
            var eclY = (geoY * Math.Cos(ecliptic)) - (geoZ * Math.Sin(ecliptic));
            var eclZ = (geoY * Math.Sin(ecliptic)) + (geoZ * Math.Cos(ecliptic));

            // Right ascension and declination, radians
            var rightAsc = Math.Atan(eclY / eclX);
            if (eclX < 0)
            {
                rightAsc += MathHelper.Pi;
            }

            if ((eclY < 0) && (eclX > 0))
            {
                rightAsc += MathHelper.TwoPi;
            }

            var declination = Math.Atan(eclZ / Math.Pow(Math.Pow(eclX, 2) + Math.Pow(eclY, 2), 0.5));

            // Convert right ascension and declination to altitude and azimuth.
            // Equations by Stephen R. Schmitt.
            // Greenwich Mean Sidereal Time, degrees
            var gMST = 280.46061837 + (360.98564736629 * jEday);
            gMST = Normalize(gMST, 360);

            // Local Mean Sidereal Time, degrees
            var lMST = gMST + (MathHelper.ToDegrees((float)longitude) / 15);

            // Hour angle, degrees
            var hA = lMST - (MathHelper.ToDegrees((float)rightAsc) / 15);

            // Hour angle, radians
            var lunarHourAngle = MathHelper.ToRadians((float)hA);

            // Lunar Altitude, radians from its Sine
            var lunarAltSin = (Math.Sin(latitude) * Math.Sin(declination)) + (Math.Cos(latitude) * Math.Cos(lunarHourAngle));
            var lunarAltitude = Math.Asin(lunarAltSin);

            // Lunar Azimuth, radians from its Cosine
            var lunarAltCos = Math.Cos(lunarAltitude);
            var hourAngleSin = Math.Sin(lunarHourAngle);
            var lunarAzCos = (Math.Sin(declination) - (lunarAltSin * Math.Sin(latitude))) / (lunarAltCos * Math.Cos(latitude));
            lunarAzCos = lunarAzCos < -1 ? -1 : lunarAzCos;
            lunarAzCos = lunarAzCos > 1 ? 1 : lunarAzCos;
            var lunarAzimuth = hourAngleSin < 0 ? Math.Acos(lunarAzCos) : MathHelper.TwoPi - Math.Acos(lunarAzCos);

            moonDirection.X = (float)Math.Sin(lunarAzimuth);
            moonDirection.Y = (float)lunarAltSin;
            moonDirection.Z = (float)Math.Cos(lunarAzimuth);
            moonDirection.Normalize();
            return moonDirection;
        }

        /// <summary>
        /// Removes all multiples of "divisor" from the input number.
        /// </summary>
        /// <param name="input">the raw number.</param>
        /// <param name="divisor">the number, or its multiples, we want to remove.</param>
        static double Normalize(double input, double divisor)
        {
            return input - (divisor * Math.Floor(input / divisor));
        }
    }
}
