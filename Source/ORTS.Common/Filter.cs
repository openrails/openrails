// COPYRIGHT 2011 by the Open Rails project.
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
using System.Linq;

namespace ORTS.Common
{
    /// <summary>
    /// by Matej Pacha
    /// IIRFilter class provides discreet Infinite impulse response (IIR) filter
    /// Transfer function in general:
    ///                          -1      -2          -n
    ///         A(z)    a0 + a1*z  + a2*z  + ... an*z
    /// H(z) = ----- = ---------------------------------
    ///         B(z)             -1      -2          -m
    ///                 1  + b1*z  + b2*z  + ... bm*z
    /// IIRFilter class includes:
    /// - Exponential filter - not implemented!
    /// - Butterworth filter - only 1st order low pass with warping effect eliminated
    /// - Chebychev filter - not implemented!
    /// - Bessel filter - not implemented!
    /// 
    /// With every filter it is possible to use constant or variable sampling frequency (now only with Butterworth 1st order!!!)
    /// - Use Filter(NewSample) for constant sampling period
    /// - Use Filter(NewSample, samplingPeriod) for variable sampling period
    /// 
    /// Note: Sampling frequency MUST be always higher than cutoff frequency - if variable sampling period is used the Filter() function
    /// checks this condition and is skipped if not passed (may cause problems with result stability)
    /// 
    /// </summary>
    public class IIRFilter
    {
        int NCoef;
        List<float> ACoef;
        List<float> BCoef;
        List<float> y;
        List<float> x;

        public IIRFilter()
        {
            /**************************************************************
             * Addition of some coeficients to make filter working
             * If needed use following to get constant sampling period filter             
             * WinFilter version 0.8
            http://www.winfilter.20m.com
            akundert@hotmail.com

            Filter type: Low Pass
            Filter model: Chebyshev
            Filter order: 2
            Sampling Frequency: 10 Hz
            Cut Frequency: 1.000000 Hz
            Pass band Ripple: 1.000000 dB
            Coefficents Quantization: float

            Z domain Zeros
            z = -1.000000 + j 0.000000
            z = -1.000000 + j 0.000000

            Z domain Poles
            z = 0.599839 + j -0.394883
            z = 0.599839 + j 0.394883
            ***************************************************************/
            ACoef = new List<float>
            {
                0.00023973435363423468f,
                0.00047946870726846936f,
                0.00023973435363423468f
            };

            BCoef = new List<float>
            {
                1.00000000000000000000f,
                -1.94607498611971570000f,
                0.94703573071858904000f
            };

            NCoef = A.Count - 1;

            x = Enumerable.Repeat(0f, NCoef).ToList();
            y = Enumerable.Repeat(0f, NCoef).ToList();

            FilterType = FilterTypes.Bessel;
        }

        /// <summary>
        /// Creates an instance of IIRFilter class
        /// </summary>
        /// <param name="a">A coefficients of the filter</param>
        /// <param name="b">B coefficients of the filter</param>
        /// <param name="type">Filter type</param>
        public IIRFilter(List<float> a, List<float> b, FilterTypes type)
        {
            FilterType = type;
            NCoef = a.Count - 1;
            ACoef = a;
            BCoef = b;
            x = Enumerable.Repeat(0f, NCoef).ToList();
            y = Enumerable.Repeat(0f, NCoef).ToList();
        }

        /// <summary>
        /// Creates an instance of IIRFilter class
        /// </summary>
        /// <param name="type">Filter type</param>
        /// <param name="order">Filter order</param>
        /// <param name="cutoffFrequency">Filter cutoff frequency in radians per second</param>
        /// <param name="samplingPeriod">Filter sampling period</param>
        public IIRFilter(FilterTypes type, int order, float cutoffFrequency, float samplingPeriod)
        {
            FilterType = type;
            NCoef = order;
            A = new List<float>();
            B = new List<float>();

            switch (type)
            {
                case FilterTypes.Butterworth:
                    ComputeButterworth(
                        Order                   = order,
                        CutoffFrequencyRadpS    = cutoffFrequency,
                        SamplingPeriod_s        = samplingPeriod);
                    break;
                default:
                    throw new NotImplementedException("Other filter types are not implemented yet.");
            }

            NCoef = A.Count - 1;
            ACoef = A;
            BCoef = B;
            x = Enumerable.Repeat(0f, NCoef).ToList();
            y = Enumerable.Repeat(0f, NCoef).ToList();
        }

        /// <summary>
        /// A coefficients of the filter
        /// </summary>
        public List<float> A
        {
            set
            {
                if (NCoef <= 0)
                    NCoef = value.Count - 1;
                x = Enumerable.Repeat(0f, NCoef).ToList();
                y = Enumerable.Repeat(0f, NCoef).ToList();
                if (ACoef == null)
                    ACoef = new List<float>();
                ACoef.Clear();
                foreach (var obj in value)
                {
                    ACoef.Add(obj);
                }
            }
            get
            {
                return ACoef;
            }
        }

        /// <summary>
        /// B coefficients of the filter
        /// </summary>
        public List<float> B
        {
            set
            {
                if (NCoef <= 0)
                    NCoef = value.Count - 1;
                x = Enumerable.Repeat(0f, NCoef).ToList();
                y = Enumerable.Repeat(0f, NCoef).ToList();
                if (BCoef == null)
                    BCoef = new List<float>();
                BCoef.Clear();
                foreach (var obj in value)
                {
                    BCoef.Add(obj);
                }
            }
            get
            {
                return BCoef;
            }
        }

        private float cuttoffFreqRadpS;
        /// <summary>
        /// Filter Cut off frequency in Radians
        /// </summary>
        public float CutoffFrequencyRadpS
        {
            set
            {
                if (value >= 0.0f)
                    cuttoffFreqRadpS = value;
                else
                    throw new NotSupportedException("Filter cutoff frequency must be positive number");
            }
            get
            {
                return cuttoffFreqRadpS;
            }
        }

        private float samplingPeriod_s;
        /// <summary>
        /// Filter sampling period in seconds
        /// </summary>
        public float SamplingPeriod_s
        {
            set
            {
                if (value >= 0.0f)
                    samplingPeriod_s = value;
                else
                    throw new NotSupportedException("Sampling period must be positive number");
            }
            get
            {
                return samplingPeriod_s;
            }
        }

        public int Order { set; get; }

        public enum FilterTypes
        {
            Exponential     = 0,
            Chebychev       = 1,
            Butterworth     = 2,
            Bessel          = 3
        }

        public FilterTypes FilterType { set; get; }

        /// <summary>
        /// IIR Digital filter function
        /// Call this function with constant sample period
        /// </summary>
        /// <param name="NewSample">Sample to filter</param>
        /// <returns>Filtered value</returns>
        public float Filter(float NewSample)
        {
            //Calculate the new output
            x.Insert(0, NewSample);
            y.Insert(0, ACoef[0] * x[0]);
            for (int n = 1; n <= NCoef; n++)
                y[0] += ACoef[n] * x[n] - BCoef[n] * y[n];

            return y[0];
        }

        /// <summary>
        /// IIR Digital filter function
        /// Call this function with constant sample period
        /// </summary>
        /// <param name="NewSample">Sample to filter</param>
        /// <param name="samplingPeriod">Sampling period</param>
        /// <returns>Filtered value</returns>
        public float Filter(float NewSample, float samplingPeriod)
        {
            if (samplingPeriod <= 0.0f)
                return 0.0f;

            switch(FilterType)
            {
                case FilterTypes.Butterworth:
                    if ((1 / (samplingPeriod) < RadToHz(cuttoffFreqRadpS)))
                    {
                        //Reset();
                        return NewSample;
                    }
                    ComputeButterworth(Order, cuttoffFreqRadpS, samplingPeriod_s = samplingPeriod);
                    break;
                default:
                    throw new NotImplementedException("Other filter types are not implemented yet. Try to use constant sampling period and Filter(float NewSample) version of this method.");
            }
            x.Insert(0, NewSample);
            y.Insert(0, ACoef[0] * x[0]);
            for (int n = 1; n <= NCoef; n++)
                y[0] += ACoef[n] * x[n] - BCoef[n] * y[n];

            return y[0];
        }

        /// <summary>
        /// Resets all buffers of the filter
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < x.Count; i++)
            {
                x[i] = 0.0f;
                y[i] = 0.0f;
            }
        }
        /// <summary>
        /// Resets all buffers of the filter with given initial value
        /// </summary>
        /// <param name="initValue">Initial value</param>
        public void Reset(float initValue)
        {
            for (float t = 0; t < (10.0f*cuttoffFreqRadpS); t += 0.1f)
            {
                Filter(initValue, 0.1f);
            }
        }

        /// <summary>
        /// First-Order IIR Filter — Calculation by Freescale Semiconductor, Inc.
        /// **********************************************************************
        /// In GDFLIB User Reference Manual, 01/2009, Rev.0
        /// 
        /// Butterworth coefficients calculation
        /// The Butterworth first-order low-pass filter prototype is therefore given as:
        ///           w_c
        /// H(s) = ---------
        ///         s + w_c
        /// This is a transfer function of Butterworth low-pass filter in the s-domain with the cutoff frequency given by the w_c
        /// Transformation of an analog filter described by previous equation into a discrete form is done using the bilinear
        /// transformation, resulting in the following transfer function:
        ///         w_cd*Ts           w_cd*Ts      -1
        ///       -------------- + ------------ * z
        ///         2 + w_cd*Ts     2 + w_cd*Ts
        /// H(z)=-------------------------------------
        ///              w_cd*Ts - 2     -1
        ///         1 + ------------- * z
        ///              2 + w_cd*Ts
        /// where w_cd is the cutoff frequency of the filter in the digital domain and Ts
        /// is the sampling period. However, mapping of the analog system into a digital domain using the bilinear
        /// transformation makes the relation between w_c and w_cd non-linear. This introduces a distortion in the frequency
        /// scale of the digital filter relative to that of the analog filter. This is known as warping effect. The warping 
        /// effect can be eliminated by pre-warping the analog filter, and then transforming it into the digital domain,
        /// resulting in this transfer function:
        ///         w_cd_p*Ts_p           w_cd_p*Ts_p      -1
        ///       ------------------ + ---------------- * z
        ///         2 + w_cd_p*Ts_p     2 + w_cd_p*Ts_p
        /// H(z)=-------------------------------------
        ///              w_cd_p*Ts_p - 2     -1
        ///         1 + ----------------- * z
        ///              2 + w_cd_p*Ts_p
        /// where ωcd_p is the pre-warped cutoff frequency of the filter in the digital domain, and Ts_p is the 
        /// pre-warped sampling period. The pre-warped cutoff frequency is calculated as follows:
        ///            2             w_cd*Ts
        /// w_cd_p = ------ * tan ( --------- )
        ///           Ts_p              2
        /// and the pre-warped sampling period is:
        /// Ts_p = 0.5
        /// 
        /// Because the given filter equation is as described, the Butterworth low-pass filter 
        /// coefficients are calculated as follows:
        ///             w_cd_p*Ts_p
        /// a1 = a2 = -----------------
        ///            2 + w_cd_p*Ts_p           
        /// b1 = 1.0
        ///       w_cd_p*Ts_p - 2
        /// b2 = ------------------
        ///       2 + w_cd_p*Ts_p
        /// </summary>
        /// <param name="order">Filter order</param>
        /// <param name="cutoffFrequency">Cuttof frequency in rad/s</param>
        /// <param name="samplingPeriod">Sampling period</param>
        public void ComputeButterworth(int order, float cutoffFrequency, float samplingPeriod)
        {
            A.Clear();
            B.Clear();

            float Ts_p = 0.5f;
            float w_cd_p = 2 / Ts_p * (float)Math.Tan(cutoffFrequency * samplingPeriod / 2.0);

            switch (order)
            {
                case 1:
                    //a1
                    A.Add((w_cd_p * Ts_p) / (2.0f + w_cd_p * Ts_p));
                    //a2
                    A.Add((w_cd_p * Ts_p) / (2.0f + w_cd_p * Ts_p));
                    //b1 = always 1.0
                    B.Add(1.0f);
                    //b2
                    B.Add((w_cd_p * Ts_p - 2.0f) / (2.0f + w_cd_p * Ts_p));
                    break;
                default:
                    throw new NotImplementedException("Filter order higher than 1 is not supported yet");
                    
            }
        }

        /// <summary>
        /// Frequency conversion from rad/s to Hz
        /// </summary>
        /// <param name="rad">Frequency in radians per second</param>
        /// <returns>Frequency in Hertz</returns>
        public static float RadToHz(float rad)
        {
            return (rad / (2.0f * (float)Math.PI));
        }

        /// <summary>
        /// Frequenc conversion from Hz to rad/s
        /// </summary>
        /// <param name="hz">Frequenc in Hertz</param>
        /// <returns>Frequency in radians per second</returns>
        public static float HzToRad(float hz)
        {
            return (2.0f * (float)Math.PI * hz);
        }

    }


    public class MovingAverage
    {
        public MovingAverage()
        {
            Buffer = new Queue<float>(100);
            Size = 10;
        }
        public MovingAverage(int size)
        {
            Buffer = new Queue<float>(100);
            Size = size;
        }

        Queue<float> Buffer;

        int size;
        public int Size { get { if (Buffer != null) return Buffer.Count; else return 0; } 
                          set { if(value > 0) size = value; else size = 1; Initialize(); }
                        }

        public void Initialize(float value)
        {
            Buffer.Clear();
            for (int i = 0; i < size; i++)
            {
                Buffer.Enqueue(value);
            }
        }
        public void Initialize()
        {
            Initialize(0f);
        }

        public float Update(float value)
        {
            if ((!float.IsNaN(value)) || (!float.IsNaN(value)))
            {
                Buffer.Enqueue(value);
                Buffer.Dequeue();
            }
            return Buffer.Average();
        }
    }
}
