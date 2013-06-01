// COPYRIGHT 2009, 2010 by the Open Rails project.
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
using System.Diagnostics;

namespace MSTSMath
{
	public class M // Not Math to avoid conflict with default Math
	{

		public static float Bearing( float x1, float z1, float x2, float z2 )
			// Returns bearing in radians from x to y
			// +ve z direction is 0'
			// +ve x direction is +90'   ( pi/2 )
			// -ve x direction is - 90 '
			// -ve z direction is +180 or -180'
		{
			float dx = x2-x1;
			float dz = z2-z1;


			float a;

			if( dx > 0 )
			{
				if( dz == 0.0 )
					a = (float)Math.PI/2.0f;   // 90'
				else if( dz > 0.0 )
					a = (float)Math.Atan( dx/dz );
				else // dz < 0.0
					a = (float)Math.Atan( dx/dz ) + (float)Math.PI;   //180'
			}
			else // dx < 0
			{
				if( dz == 0.0 )
					a = -(float)Math.PI/2.0f;
				else if( dz > 0.0 )
					a = (float)Math.Atan( dx/dz );
				else // dz < 0.0
					a = (float)Math.Atan( dx/dz ) - (float)Math.PI;
			}

			return a;
		}

		public static float DistanceSquared( float dx, float dy )
		{
			return dx*dx + dy*dy;
		}

		public static float ATanRad( float dx, float dz )
		// Angle will be + / - PI
		// +ve Z points north
		// returns NAN if dx and dz = 0
		{
			// Starting north and moving clockwise
			if( dx > 0 && dz > dx )
			{
				return (float)Math.Atan( dx/dz );
			}
			else if( dx > 0 && dz > 0 )
			{
                return (float)Math.PI / 2 - (float)Math.Atan(dz / dx);
			}
			else if( dx > 0 && -dz < dx )
			{
                return (float)Math.PI / 2 - (float)Math.Atan(dz / dx);
			}
			else if( dx > 0 )
			{
                return (float)Math.PI + (float)Math.Atan(dx / dz);
			}
			else if( -dz > -dx )
			{
                return -(float)Math.PI + (float)Math.Atan(dx / dz);
			}
			else if( dz < 0 )
			{
                return -(float)Math.PI / 2 - (float)Math.Atan(dz / dx);
			}
			else if( dz < -dx )
			{
                return -(float)Math.PI / 2 - (float)Math.Atan(dz / dx);
			}
			else
			{
                return (float)Math.Atan(dx / dz);
			}

	
		}

		public static void NormalizeRadians( ref float radians )
			// make angle between +PI and -PI 
		{
			while( radians > Math.PI ) radians -= (float)(2.0*Math.PI);	
			while( radians < -Math.PI ) radians += (float)(2.0*Math.PI);
		}

		public static float TurnAngleRadians( float diameter, float length )
		// draw a cord length long across between two points of a circle
		// the two points will be separated by 'TurnAngleRadians' angle
		// length must be < diameter
		{
			float d = length/diameter;
			if( d > 1 )
				return (float)Math.PI/2.0F; // 90
			else if ( d < -1 )
				return -(float)Math.PI/2.0F; // -90;
			else
				return 2.0F * (float)Math.Asin( d ) ;
		}

		public static float CordLength( float diameter, float turnAngleRadians )
		//  straight line distance between two points on a circle
		//  the points on the circle are separated by 'turnAngleRadians'
		{
			return (float)(Math.Abs( diameter*Math.Sin( turnAngleRadians/2.0 ) ));
		}

		public static float CordOffset( float diameter, float turnAngleRadians )
		// a cord is stretched between two points of a circle
		// how far is the middle of the cord from the edge of the circle
		{
			return (float)(diameter * ( 1.0 - Math.Cos( turnAngleRadians /2.0 ) )/ 2.0);
		}

		public static float CordOffsetAtDistance( float diameter, float turnAngleRadians, float distance )
		// a cord is stretched between two poins of a circle
		// at the specified distance along the cord, how far is it from the edge of the circle
		// TODO - this is probably just a bad approximation
		{
			float radius = diameter/2.0f;
			float cordLength = CordLength( diameter, turnAngleRadians );
			float cordOffsetFromCenter = (float)Math.Sqrt( radius*radius - (cordLength/2.0f) * (cordLength/2.0f) );
			float x = distance - cordLength/2.0f;
			float y;
			if( x > radius )
				return 1e30f; 
			y = (float)Math.Sqrt( radius*radius - x*x );
			float cordOffset = y - cordOffsetFromCenter;
			if( turnAngleRadians < 0 )
				cordOffset *= -1;
			return cordOffset;
		}


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
        /// Given a line described by ax + by + c = 0 where a^2 + b^2 = 1
        /// Returns the signed distance from x,y to the line.
        /// From http://softsurfer.com/Archive/algorithm_0102/algorithm_0102.htm
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static float DistanceToLine(float a, float b, float c, float x, float y)
        {
            return a * x + b * y + c;
        }


		public static void NearestPoint( float m, float  b, float x0, float y0, out float xc, out float yc )
			// A line is represented by y = mx + b 
			// This determines, xc,yc, the closest point on the line to x0,y0
		{
            xc = (y0 + x0 / m - b) / (m + 1.0f / m);
			yc = m * xc + b;
		}
		public static float Radians( float degrees )
		{
			return 2.0F*(float)Math.PI*degrees/360.0F;
		}

		public static float Degrees( float radians )
		{
			return (float)(360.0 * radians/( 2.0 * Math.PI ));
		}

		/// <summary>
		/// Compute the angle in radians resulting from these delta's
		/// 0 degrees is straight ahead - Dz = 0, Dx = 1;
		/// </summary>
		/// <param name="Dx"></param>
		/// <param name="Dz"></param>
		/// <returns></returns>
		public static float AngleDxDz( float Dx, float Dz )
			// Compute the angle from Dx and Dz
		{
			float a;


			// Find the angle in the first quadrant
			if( Dz == 0.0 )
                a = (float)(Math.PI / 2.0);
			else
                a = (float)Math.Atan(Math.Abs(Dx) / Math.Abs(Dz));


			// Find the quadrant
			if( Dz < 0.0 )
			{
                a = (float)Math.PI - a;
			}

			if( Dx < 0.0 )
			{
				a = -a;
			}

			// Normalize +/- 180
			if( a < -Math.PI )
			{
                a += 2.0f * (float)Math.PI;
			}
			if( a > Math.PI )
			{
                a -= 2.0f * (float)Math.PI;
			}

			return a;
		}

	}
}
