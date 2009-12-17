using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace ORTS
{
    class ORTSMath
    {
        //
        // from http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToEuler/index.htm
        //
        public static void MatrixToAngles( Matrix m, out float heading, out float attitude, out float bank) 
        {    // Assuming the angles are in radians.
	        if (m.M21 > 0.998) { // singularity at north pole
		        heading = (float)Math.Atan2(m.M13,m.M33);		
                attitude = (float)Math.PI/2;
		        bank = 0;
		        return;
	        }	if (m.M21 < -0.998) { // singularity at south pole
		        heading = (float)Math.Atan2(m.M13,m.M33);		
                attitude = -(float)Math.PI/2;
		        bank = 0;
		        return;
	        }	
            heading = (float)Math.Atan2(-m.M31,m.M11);
	        bank = (float)Math.Atan2(-m.M23,m.M22);
	        attitude = (float)Math.Asin(m.M21);
        }

        public struct Matrix2x2
        {
            public float M00, M01, M10, M11;

            public Matrix2x2(float m00, float m01, float m10, float m11)
            {
                M00 = m00; M01 = m01; M10 = m10; M11 = m11;
            }
        }

        public static float Interpolate2D(float x, float z, Matrix2x2 y)
        {
            float result = 0;

            result += (1-x) * (1-z) * y.M00;
            result += (x) * (1-z) * y.M01;
            result += (1 - x) * (z) * y.M10;
            result += (x) * (z) * y.M11;

            return result;
        }
    }
}
