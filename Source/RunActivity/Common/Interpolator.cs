/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using MSTS;

namespace ORTS
{
    /// <summary>
    /// Interpolated table lookup
    /// Supports linear or cubic spline interpolation
    /// </summary>
    public class Interpolator
    {
        float[] X;  // must be in increasing order
        float[] Y;
        float[] Y2 = null;
        int Size = 0;       // number of values populated
        int PrevIndex = 0;  // used to speed up repeated evaluations with similar x values
        public Interpolator(int n)
        {
            X = new float[n];
            Y = new float[n];
        }
        public Interpolator(float[] x, float[] y)
        {
            X = x;
            Y = y;
            Size = X.Length;
        }
        public Interpolator(Interpolator other)
        {
            X = other.X;
            Y = other.Y;
            Y2= other.Y2;
            Size = other.Size;
        }
        public Interpolator(STFReader stf)
        {
            List<float> list = new List<float>();
            stf.MustMatch("(");
            while (!stf.EndOfBlock())
                list.Add(stf.ReadFloat(STFReader.UNITS.Any, null));
            if (list.Count % 2 == 1)
                STFException.TraceWarning(stf, "Ignoring extra odd value in Interpolator list.");
            int n = list.Count/2;
            if (n < 2)
                STFException.TraceWarning(stf, "Interpolator must have at least two value pairs.");
            X = new float[n];
            Y = new float[n];
            Size = n;
            for (int i = 0; i < n; i++)
            {
                X[i] = list[2*i];
                Y[i] = list[2*i+1];
                if (i > 0 && X[i - 1] >= X[i])
                    STFException.TraceWarning(stf, "Interpolator x values must be increasing.");
            }
        }
        public float this[float x]
        {
            get
            {
                if (x < X[PrevIndex] || x > X[PrevIndex + 1])
                {
                    if (x < X[1])
                        PrevIndex= 0;
                    else if (x > X[Size-2])
                        PrevIndex= Size-2;
                    else
                    {
                        int i= 0;
                        int j= Size-1;
                        while (j-i > 1)
                        {
                            int k= (i+j)/2;
                            if (X[k] > x)
                                j= k;
                            else
                                i= k;
                        }
                        PrevIndex= i;
                    }
                }
                float d= X[PrevIndex+1] - X[PrevIndex];
                float a= (X[PrevIndex+1]-x)/d;
                float b= (x-X[PrevIndex])/d;
                float y= a*Y[PrevIndex] + b*Y[PrevIndex+1];
                if (Y2 != null && a>=0 && b>=0)
                    y+= ((a*a*a-a)*Y2[PrevIndex] + (b*b*b-b)*Y2[PrevIndex+1])*d*d/6;
                return y;
            }
            set
            {
                X[Size] = x;
                Y[Size] = value;
                Size++;
            }
        }
        public float MinX() { return X[0]; }
        public float MaxX() { return X[Size-1]; }
        public float MaxY()
        {
            float x;
            return MaxY(out x);
        }
        public float MaxY(out float x)
        {
            int maxi= 0;
            for (int i=1; i<Size; i++)
                if (Y[maxi] < Y[i])
                    maxi= i;
            x = X[maxi];
            return Y[maxi];
        }
        public void ScaleX(float factor)
        {
            for (int i = 0; i < Size; i++)
                X[i] *= factor;
        }
        public void ScaleY(float factor)
        {
            for (int i = 0; i < Size; i++)
                Y[i] *= factor;
            if (Y2 != null)
            {
                for (int i = 0; i < Size; i++)
                    Y2[i]*= factor;
            }
        }
        public void ComputeSpline()
        {
            ComputeSpline(null,null);
        }
        public void ComputeSpline(float? yp1, float? yp2)
        {
            Y2= new float[Size];
            float[] u= new float[Size];
            if (yp1 == null)
            {
                Y2[0]= 0;
                u[0]= 0;
            }
            else
            {
                Y2[0]= -.5f;
                float d= X[1]-X[0];
                u[0]= 3/d * ((Y[1]-Y[0])/d-yp1.Value);
            }
            for (int i=1; i<Size-1; i++)
            {
                float sig= (X[i]-X[i-1]) / (X[i+1]-X[i-1]);
                float p= sig*Y2[i-1] + 2;
                Y2[i]= (sig-1)/p;
                u[i]= (6*((Y[i+1]-Y[i])/(X[i+1]-X[i]) -
                    (Y[i]-Y[i-1])/(X[i]-X[i-1])) / (X[i+1]-X[i-1]) -
                    sig*u[i-1]) / p;
            }
            if (yp2 == null)
            {
                Y2[Size-1]= 0;
            }
            else
            {
                float d= X[Size-1]-X[Size-2];
                Y2[Size-1]= (3/d *(yp2.Value-(Y[Size-1]-Y[Size-2])/d)- .5f*u[Size-2])/(.5f*Y2[Size-2]+1);
            }
            for (int i=Size-2; i>=0; i--)
                Y2[i]= Y2[i]*Y2[i+1] + u[i];
        }
        
        // restore game state
        public Interpolator(BinaryReader inf)
        {
            Size = inf.ReadInt32();
            X = new float[Size];
            Y = new float[Size];
            for (int i = 0; i < Size; i++)
            {
                X[i] = inf.ReadSingle();
                Y[i] = inf.ReadSingle();
            }
            if (inf.ReadBoolean())
            {
                Y2 = new float[Size];
                for (int i = 0; i < Size; i++)
                    Y2[i] = inf.ReadSingle();
            }
        }

        // save game state
        public void Save(BinaryWriter outf)
        {
            outf.Write(Size);
            for (int i = 0; i < Size; i++)
            {
                outf.Write(X[i]);
                outf.Write(Y[i]);
            }
            outf.Write(Y2 != null);
            if (Y2 != null)
                for (int i = 0; i < Size; i++)
                    outf.Write(Y2[i]);
        }

        public void test(string label, int n)
        {
            float dx = (MaxX() - MinX()) / (n-1);
            for (int i = 0; i < n; i++)
            {
                float x = MinX() + i * dx;
                float y = this[x];
                Console.WriteLine("{0} {1} {2}", label, x, y);
            }
        }
    }
    /// <summary>
    /// two dimensional Interpolated table lookup
    /// </summary>
    public class Interpolator2D
    {
        float[] X;  // must be in increasing order
        Interpolator[] Y;
        int Size = 0;       // number of values populated
        int PrevIndex = 0;  // used to speed up repeated evaluations with similar x values
        public Interpolator2D(int n)
        {
            X = new float[n];
            Y = new Interpolator[n];
        }
        public Interpolator2D(Interpolator2D other)
        {
            X = other.X;
            Size = other.Size;
            Y = new Interpolator[Size];
            for (int i = 0; i < Size; i++)
                Y[i] = new Interpolator(other.Y[i]);
        }
        public Interpolator2D(STFReader stf)
        {
            List<float> xlist = new List<float>();
            List<Interpolator> ilist = new List<Interpolator>();
            stf.MustMatch("(");
            while(!stf.EndOfBlock())
            {
                xlist.Add(stf.ReadFloat(STFReader.UNITS.Any, null));
                ilist.Add(new Interpolator(stf));
            }
            stf.SkipRestOfBlock();
            int n = xlist.Count;
            if (n < 2)
                STFException.TraceWarning(stf, "Interpolator must have at least two x values.");
            X = new float[n];
            Y = new Interpolator[n];
            Size = n;
            for (int i = 0; i < n; i++)
            {
                X[i] = xlist[i];
                Y[i] = ilist[i];
                if (i > 0 && X[i - 1] >= X[i])
                    STFException.TraceWarning(stf, "Interpolator x values must be increasing.");
            }
        }
        public float Get(float x, float y)
        {
            if (x < X[PrevIndex] || x > X[PrevIndex + 1])
            {
                if (x < X[1])
                    PrevIndex = 0;
                else if (x > X[Size - 2])
                    PrevIndex = Size - 2;
                else
                {
                    int i = 0;
                    int j = Size - 1;
                    while (j - i > 1)
                    {
                        int k = (i + j) / 2;
                        if (X[k] > x)
                            j = k;
                        else
                            i = k;
                    }
                    PrevIndex = i;
                }
            }
            float d = X[PrevIndex + 1] - X[PrevIndex];
            float a = (X[PrevIndex + 1] - x) / d;
            float b = (x - X[PrevIndex]) / d;
            float z = 0;
            if (a != 0)
                z += a * Y[PrevIndex][y];
            if (b != 0)
                z += b * Y[PrevIndex + 1][y];
            return z;
        }
        public Interpolator this[float x]
        {
            set
            {
                X[Size] = x;
                Y[Size] = value;
                Size++;
            }
        }
        public float MinX() { return X[0]; }
        public float MaxX() { return X[Size - 1]; }
        public void ScaleX(float factor)
        {
            for (int i = 0; i < Size; i++)
                X[i] *= factor;
        }
    }
}
