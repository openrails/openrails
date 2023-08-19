// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using System;
using System.IO;

namespace Orts.Common
{
    public static class ORTSMath
   {
      //
      // from http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToEuler/index.htm
      //
      public static void MatrixToAngles(Matrix m, out float heading, out float attitude, out float bank)
      {    // Assuming the angles are in radians.
         if (m.M21 > 0.998)
         { // singularity at north pole
            heading = (float)Math.Atan2(m.M13, m.M33);
            attitude = (float)Math.PI / 2;
            bank = 0;
            return;
         }
         if (m.M21 < -0.998)
         { // singularity at south pole
            heading = (float)Math.Atan2(m.M13, m.M33);
            attitude = -(float)Math.PI / 2;
            bank = 0;
            return;
         }
         heading = (float)Math.Atan2(-m.M31, m.M11);
         bank = (float)Math.Atan2(-m.M23, m.M22);
         attitude = (float)Math.Asin(m.M21);
      }

     public static float MatrixToYAngle(Matrix m)
     {    // Assuming the angles are in radians.
        if (m.M21 > 0.998 || m.M21 < -0.998)
            // singularity at poles
            return (float)Math.Atan2(m.M13, m.M33);

        else return (float)Math.Atan2(-m.M31, m.M11);
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

         result += (1 - x) * (1 - z) * y.M00;
         result += (x) * (1 - z) * y.M01;
         result += (1 - x) * (z) * y.M10;
         result += (x) * (z) * y.M11;

         return result;
      }

      public static float LineSegmentDistanceSq(Vector3 pt, Vector3 end1, Vector3 end2)
      {
         float dx = end2.X - end1.X;
         float dy = end2.Y - end1.Y;
         float dz = end2.Z - end1.Z;
         float d = dx * dx + dy * dy + dz * dz;
         float n = dx * (pt.X - end1.X) + dy * (pt.Y - end1.Y) + dz * (pt.Z - end1.Z);
         if (d == 0 || n < 0)
         {
            dx = end1.X - pt.X;
            dy = end1.Y - pt.Y;
            dz = end1.Z - pt.Z;
         }
         else if (n > d)
         {
            dx = end2.X - pt.X;
            dy = end2.Y - pt.Y;
            dz = end2.Z - pt.Z;
         }
         else
         {
            dx = end1.X + dx * n / d - pt.X;
            dy = end1.Y + dy * n / d - pt.Y;
            dz = end1.Z + dz * n / d - pt.Z;
         }
         return dx * dx + dy * dy + dz * dz;
      }

        public static void SaveMatrix(BinaryWriter outf, Matrix matrix)
        {
            outf.Write(matrix.M11);
            outf.Write(matrix.M12);
            outf.Write(matrix.M13);
            outf.Write(matrix.M14);
            outf.Write(matrix.M21);
            outf.Write(matrix.M22);
            outf.Write(matrix.M23);
            outf.Write(matrix.M24);
            outf.Write(matrix.M31);
            outf.Write(matrix.M32);
            outf.Write(matrix.M33);
            outf.Write(matrix.M34);
            outf.Write(matrix.M41);
            outf.Write(matrix.M42);
            outf.Write(matrix.M43);
            outf.Write(matrix.M44);
        }

        public static Matrix RestoreMatrix(BinaryReader inf)
        {
            var matrix = Matrix.Identity;
            matrix.M11 = inf.ReadSingle();
            matrix.M12 = inf.ReadSingle();
            matrix.M13 = inf.ReadSingle();
            matrix.M14 = inf.ReadSingle();
            matrix.M21 = inf.ReadSingle();
            matrix.M22 = inf.ReadSingle();
            matrix.M23 = inf.ReadSingle();
            matrix.M24 = inf.ReadSingle();
            matrix.M31 = inf.ReadSingle();
            matrix.M32 = inf.ReadSingle();
            matrix.M33 = inf.ReadSingle();
            matrix.M34 = inf.ReadSingle();
            matrix.M41 = inf.ReadSingle();
            matrix.M42 = inf.ReadSingle();
            matrix.M43 = inf.ReadSingle();
            matrix.M44 = inf.ReadSingle();
            return matrix;
        }
    }
}
