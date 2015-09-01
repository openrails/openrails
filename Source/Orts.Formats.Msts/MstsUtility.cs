// COPYRIGHT 2009, 2010, 2014, 2015 by the Open Rails project.
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

namespace Orts.Formats.Msts
{
    public class MstsUtility
    {
        /// <summary>
        /// Consider a line starting a pX,pZ and heading away at deg from North
        /// returns lat =  distance of x,z off of the line
        /// returns lon =  distance of x,z along the line
        /// </summary>
        public static void Survey(float pX, float pZ, float rad, float x, float z, out float lon, out float lat)
        {
            // translate the coordinates relative to a track section that starts at 0,0 
            x -= pX;
            z -= pZ;

            // rotate the coordinates relative to a track section that is pointing due north ( +z in MSTS coordinate system )
            Rotate2D(rad, ref x, ref z);

            lat = x;
            lon = z;
        }

        //            2D Rotation
        //
        //              A point <x,y> can be rotated around the origin <0,0> by running it through the following equations to get the new point <x',y'> :
        //
        //              x' = cos(theta)*x - sin(theta)*y 
        //              y' = sin(theta)*x + cos(theta)*y
        //
        //          where theta is the angle by which to rotate the point.
        public static void Rotate2D(float radians, ref float x, ref float z)
        {
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            float xp = cos * x - sin * z;
            float zp = sin * x + cos * z;

            x = (float)xp;
            z = (float)zp;
        }

        /// <summary>
        /// Compute the angle in radians resulting from these delta's
        /// 0 degrees is straight ahead - Dz = 0, Dx = 1;
        /// </summary>
        /// <param name="Dx"></param>
        /// <param name="Dz"></param>
        /// <returns></returns>
        public static float AngleDxDz(float Dx, float Dz)
        // Compute the angle from Dx and Dz
        {
            float a;


            // Find the angle in the first quadrant
            if (Dz == 0.0)
                a = (float)(Math.PI / 2.0);
            else
                a = (float)Math.Atan(Math.Abs(Dx) / Math.Abs(Dz));


            // Find the quadrant
            if (Dz < 0.0)
            {
                a = (float)Math.PI - a;
            }

            if (Dx < 0.0)
            {
                a = -a;
            }

            // Normalize +/- 180
            if (a < -Math.PI)
            {
                a += 2.0f * (float)Math.PI;
            }
            if (a > Math.PI)
            {
                a -= 2.0f * (float)Math.PI;
            }

            return a;
        }
    }
}
