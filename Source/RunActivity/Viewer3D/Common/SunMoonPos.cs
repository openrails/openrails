// COPYRIGHT 2010 by the Open Rails project.
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
    * Solar postion equations are NOAA equations adapted from "Astronomical Algorithms," by Jean Meeus,
    * Lunar equations are adapted by Keith Burnett from the US Naval Observatory Astronomical Almanac.
*/
// Principal Author:
//    Rick Grout
//

using Microsoft.Xna.Framework;
using System;

namespace Orts.Viewer3D.Common
{
    static class SunMoonPos
    {
        /// <summary>
        /// Calculates the solar direction vector.
        /// Used for locating the sun graphic and as the location of the main scenery light source.
        /// </summary>
        /// <param name="latitude">latitude</param>
        /// <param name="longitude">longitude</param>
        /// <param name="clockTime">wall clock time since start of activity, days</param>
        /// <param name="date">structure made up of day, month, year and ordinal date</param>
        public static Vector3 SolarAngle(double latitude, double longitude, float clockTime, SkyViewer.SkyDate date)
        {
            Vector3 sunDirection;

            // For these calculations, west longitude is in positive degrees,
            float NOAAlongitude = -MathHelper.ToDegrees((float)longitude);
            // Fractional year, radians
            double fYear = (MathHelper.TwoPi / 365) * (date.OrdinalDate - 1 + (clockTime - 0.5));
            // Equation of time, minutes
            double eqTime = 229.18 * (0.000075
                + 0.001868 * Math.Cos(fYear)
                - 0.032077 * Math.Sin(fYear)
                - 0.014615 * Math.Cos(2 * fYear)
                - 0.040849 * Math.Sin(2 * fYear));
            // Solar declination, radians
            double solarDeclination = 0.006918
                - 0.399912 * Math.Cos(fYear)
                + 0.070257 * Math.Sin(fYear)
                - 0.006758 * Math.Cos(2 * fYear)
                + 0.000907 * Math.Sin(2 * fYear)
                - 0.002697 * Math.Cos(3 * fYear)
                + 0.001480 * Math.Sin(3 * fYear);
            // Time offset at present longitude, minutes
            double timeOffset = eqTime - 4 * NOAAlongitude + 60 * Math.Round(NOAAlongitude / 15);
            // True solar time, minutes (since midnight)
            double trueSolar = clockTime * 24 * 60 + timeOffset;
            // Solar hour angle, radians
            double solarHourAngle = MathHelper.ToRadians((float)(trueSolar / 4) - 180);

            // Solar zenith cosine. This is the Y COORDINATE of the solar Vector.
            double solarZenithCosine = Math.Sin(latitude)
                * Math.Sin(solarDeclination)
                + Math.Cos(latitude)
                * Math.Cos(solarDeclination)
                * Math.Cos(solarHourAngle);

            // Solar elevation angle, radians. Currently not used.
            //          double solarElevationAngle = MathHelper.PiOver2 - Math.Acos(solarZenithCosine);

            // Solar azimuth cosine. This is the Z COORDINATE of the solar Vector.
            double solarAzimuthCosine = -(Math.Sin(latitude)
                * solarZenithCosine
                - Math.Sin(solarDeclination)) / (
                +Math.Cos(latitude)
                * Math.Sin(Math.Acos(solarZenithCosine)));

            // Running at 64 bit solarAzimuthCosine can be slightly below -1, generating NaN results
            if (solarAzimuthCosine > 1.0d) solarAzimuthCosine = 1.0d;
            if (solarAzimuthCosine < -1.0d) solarAzimuthCosine = -1.0d;

            // Solar azimuth angle, radians. Currently not used.
            //          double solarAzimuthAngle = Math.Acos(solarAzimuthCosine);
            //          if (clockTime > 0.5)
            //              solarAzimuthAngle = MathHelper.TwoPi - solarAzimuthAngle;

            // Solar azimuth sine. This is the X COORDINATE of the solar Vector.
            double solarAzimuthSine = Math.Sin(Math.Acos(solarAzimuthCosine)) * (clockTime > 0.5 ? 1 : -1);

            sunDirection.X = -(float)solarAzimuthSine;
            sunDirection.Y = (float)solarZenithCosine;
            sunDirection.Z = -(float)solarAzimuthCosine;
            sunDirection.Normalize();
            return sunDirection;
        }

        /// <summary>
        /// Calculates the lunar direction vector. 
        /// </summary>
        /// <param name="latitude">latitude</param>
        /// <param name="longitude">longitude</param>
        /// <param name="clockTime">wall clock time since start of activity</param>
        /// <param name="date">structure made up of day, month, year and ordinal date</param>
        public static Vector3 LunarAngle(double latitude, double longitude, float clockTime, SkyViewer.SkyDate date)
        {
            Vector3 moonDirection;

            // Julian day.
            double JEday = (float)367 * date.Year - 7 * (date.Year + (date.Month + 9) / 12) / 4 + 275 * date.Month / 9 + date.Day - 730531.5 + clockTime;
            // Fractional time within current 100-year epoch, days
            double Ftime = JEday / 36525;
            // Ecliptic longitude, radians
            double GeoLong = Normalize(Normalize(3.8104 + 8399.71 * Ftime, MathHelper.TwoPi)
                + 0.10978 * Math.Sin(Normalize(2.3544 + 8328.69 * Ftime, MathHelper.TwoPi))
                - 0.02216 * Math.Sin(Normalize(4.5239 - 7214.12 * Ftime, MathHelper.TwoPi))
                + 0.0115 * Math.Sin(Normalize(4.11374 + 15542.75 * Ftime, MathHelper.TwoPi))
                + 0.003665 * Math.Sin(Normalize(4.7106 + 16657.38 * Ftime, MathHelper.TwoPi))
                - 0.003316 * Math.Sin(Normalize(6.2396 + 628.3 * Ftime, MathHelper.TwoPi))
                - 0.00192 * Math.Sin(Normalize(3.2568 + 16866.93 * Ftime, MathHelper.TwoPi)), MathHelper.TwoPi);
            // Ecliptic latitude, radians
            double GeoLat = 0.08954 * Math.Sin(Normalize(1.6284 + 8433.47 * Ftime, MathHelper.TwoPi))
                + 0.004887 * Math.Sin(Normalize(3.9828 + 16762.16 * Ftime, MathHelper.TwoPi))
                - 0.004887 * Math.Sin(Normalize(5.5554 + 104.76 * Ftime, MathHelper.TwoPi))
                - 0.002967 * Math.Sin(Normalize(3.7978 - 7109.29 * Ftime, MathHelper.TwoPi));
            // Parallax, radians
            double Parallax = 0.0165946
                + 0.0009041 * Math.Cos(Normalize(2.3544 + 832869 * Ftime, MathHelper.TwoPi))
                + 0.000166 * Math.Cos(Normalize(4.5238 - 7214.06 * Ftime, MathHelper.TwoPi))
                + 0.000136 * Math.Cos(Normalize(4.1137 + 15542.75 * Ftime, MathHelper.TwoPi))
                + 0.000489 * Math.Cos(Normalize(4.7106 + 16657.38 * Ftime, MathHelper.TwoPi));
            // Geocentric distance, dimensionless
            double GeoDist = 1 / Math.Sin(Parallax);
            // Geocentric vector coordinates, dimensionless
            double geoX = GeoDist * Math.Cos(GeoLong) * Math.Cos(GeoLat);
            double geoY = GeoDist * Math.Sin(GeoLong) * Math.Cos(GeoLat);
            double geoZ = GeoDist * Math.Sin(GeoLat);
            // Mean obliquity of ecliptic, radians
            double Ecliptic = (0.4091 - 0.00000000623 * JEday);
            // Equator of ordinalDate vector coordinates, dimensionless
            double eclX = geoX;
            double eclY = geoY * Math.Cos(Ecliptic) - geoZ * Math.Sin(Ecliptic);
            double eclZ = geoY * Math.Sin(Ecliptic) + geoZ * Math.Cos(Ecliptic);
            // Right ascension and declination, radians
            double RightAsc = Math.Atan(eclY / eclX);
            if (eclX < 0)
                RightAsc += MathHelper.Pi;
            if ((eclY < 0) && (eclX > 0))
                RightAsc += MathHelper.TwoPi;
            double Declination = Math.Atan(eclZ / (Math.Pow((Math.Pow(eclX, 2) + Math.Pow(eclY, 2)), 0.5)));
            // Convert right ascension and declination to altitude and azimuth.
            // Equations by Stephen R. Schmitt.
            // Greenwich Mean Sidereal Time, degrees
            double GMST = 280.46061837 + 360.98564736629 * JEday;
            GMST = Normalize(GMST, 360);
            // Local Mean Sidereal Time, degrees
            double LMST = GMST + MathHelper.ToDegrees((float)longitude) / 15;
            // Hour angle, degrees
            double HA = LMST - MathHelper.ToDegrees((float)RightAsc) / 15;
            // Hour angle, radians
            double lunarHourAngle = MathHelper.ToRadians((float)HA);
            // Lunar Altitude, radians from its Sine
            double lunarAltSin = Math.Sin(latitude) * Math.Sin(Declination) + Math.Cos(latitude) * Math.Cos(lunarHourAngle);
            double lunarAltitude = Math.Asin(lunarAltSin);
            // Lunar Azimuth, radians from its Cosine
            double lunarAltCos = Math.Cos(lunarAltitude);
            double hourAngleSin = Math.Sin(lunarHourAngle);
            double lunarAzCos = (Math.Sin(Declination) - lunarAltSin * Math.Sin(latitude)) / (lunarAltCos * Math.Cos(latitude));
            lunarAzCos = lunarAzCos < -1 ? -1 : lunarAzCos;
            lunarAzCos = lunarAzCos > 1 ? 1 : lunarAzCos;
            double lunarAzimuth;
            if (hourAngleSin < 0)
                lunarAzimuth = Math.Acos(lunarAzCos);
            else
                lunarAzimuth = MathHelper.TwoPi - Math.Acos(lunarAzCos);

            moonDirection.X = (float)Math.Sin(lunarAzimuth);
            moonDirection.Y = (float)lunarAltSin;
            moonDirection.Z = (float)Math.Cos(lunarAzimuth);
            moonDirection.Normalize();
            return moonDirection;
        }

        /// <summary>
        /// Removes all multiples of "divisor" from the input number.
        /// </summary>
        /// <param name="input">the raw number</param>
        /// <param name="divisor">the number, or its multiples, we want to remove</param> 
        static double Normalize(double input, double divisor)
        {
            double output = input - divisor * Math.Floor(input / divisor);
            return output;
        }

    }
}
