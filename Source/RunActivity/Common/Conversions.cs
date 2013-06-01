// COPYRIGHT 2009, 2010, 2011, 2013 by the Open Rails project.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ORTS
{
    //CJ
    // OR will use metric units (m, kg, s, A, 'C) for internal properties and calculations, preferably from SI (m/s, not km/hr).
    // Currently (v1618), some internal units are Imperial and will be changed.
    // Classes are provided for converting into and out of these internal units.
    // Use these classes rather than in-line literal factors.
    //
    // For example to convert a number from metres to inches, use "DiameterIn = M.ToIn(DiameterM);"
    // 
    // Web research suggests that VC++ will optimize "/ 2.0" replacing it with "* 0.5f" but VC# will not and cost is around 15 cycles.

    /// <summary>
    /// Distance conversions from and to metres
    /// </summary>
    public class Me {   // Not M to avoid conflict with MSTSMath.M
        public static float FromMi(float m) { return m * 1609.344f; }   // miles => metres
        public static float   ToMi(float m) { return m / 1609.344f; }   // metres => miles
        public static float FromYd(float y) { return y * 0.9144f; }     // yards => metres
        public static float   ToYd(float m) { return m / 0.9144f; }     // metres => yards
        public static float FromFt(float f) { return f * 0.3048f; }     // feet => metres
        public static float   ToFt(float m) { return m / 0.3048f; }     // metres => feet
        public static float FromIn(float i) { return i * 0.0254f; }     // inches => metres
        public static float   ToIn(float m) { return m / 0.0254f; }     // metres => inches
    }

    /// <summary>
    /// Speed conversions from and to metres/sec
    /// </summary>
	public class MpS
    {
        public static float FromMpH(float m)    { return m / 2.23693629f; }    // miles per hour => metres per sec
        public static float   ToMpH(float m)    { return m * 2.23693629f; }    // metres per sec => miles per hour
        public static float FromKpH(float k)    { return k / 3.600f; }    // kilometres per hour => metres per sec
        public static float   ToKpH(float m)    { return m * 3.600f; }    // metres per sec => kilometres per hour
        
        public static float FromMpS(float speed, bool isMetric)
        {
            return isMetric ? ToKpH(speed) : ToMpH(speed);
        }

        public static float ToMpS(float speed, bool isMetric)
		{
            return isMetric ? FromKpH(speed) : FromMpH(speed);
		}
	}

#if NEW_SIGNALLING
	public class Miles
	{
        public static float FromM(float distance, bool isMetric)
		{
            //CJ
            //return isMetric ? distance : (0.000621371192f * distance);
            return isMetric ? distance : Me.FromMi(distance);
        }
        public static float ToM(float distance, bool isMetric)
		{
            return isMetric ? distance : Me.ToMi(distance);
		}
	}

	public class FormatStrings
	{
        public static string FormatSpeed(float speed, bool isMetric)
        {
            return String.Format(isMetric ? "{0:F1}kph" : "{0:F1}mph", MpS.FromMpS(speed, isMetric));
        }

        public static string FormatDistance(float distance, bool isMetric)
        {
            if (isMetric)
            {
                // <0.1 kilometers, show meters.
                if (Math.Abs(distance) < 100)
                    return String.Format("{0:N0}m", distance);
                return String.Format("{0:F1}km", distance / 1000.000);
            }
            // <0.1 miles, show yards.
            if (Math.Abs(distance) < Me.FromMi(0.1f))
                return String.Format("{0:N0}yd", Me.ToYd(distance));
            return String.Format("{0:F1}mi", Me.ToMi(distance));
        }
	}
#endif		

    /// <summary>
    /// Mass conversions from and to Kilograms
    /// </summary>
    public class Kg {
        public static float FromLb(float l)     { return l / 2.20462f; }    // lb => Kg
        public static float   ToLb(float k)     { return k * 2.20462f; }    // Kg => lb
        public static float FromTUS(float t)    { return t * 907.1847f; }   // Tons (US) => Kg
        public static float   ToTUS(float k)    { return k / 907.1847f; }   // Kg => Tons (US)
        public static float FromTUK(float t)    { return t * 1016.047f; }   // Tons (UK) => Kg 
        public static float   ToTuk(float k)    { return k / 1016.047f; }   // kg => Tons (UK)
    }

    /// <summary>
    /// Force conversions from and to Newtons
    /// </summary>
    public class N {
        public static float FromLbf(float l)    { return l / 4.44822162f; }    // lbf => Newtons
        public static float   ToLbf(float n)    { return n * 4.44822162f; }    // Newtons => lbf
    }

    /// <summary>
    /// Power conversions from and to Watts
    /// </summary>
    public class W {
        public static float FromHp(float h) { return h * 745.699872f; } // Hp => Watts
        public static float   ToHp(float w) { return w / 745.699872f; } // Watts => Hp
    }

    /// <summary>
    /// Stiffness conversions from and to Newtons/metre
    /// </summary>
    public class NpM {
    }

    /// <summary>
    /// Resistance conversions from and to Newtons/metre/sec
    /// </summary>
    public class NpMpS {
    }

    /// <summary>
    /// Mass rate conversions from and to Kg/s
    /// </summary>
    public class KgpS {
        public static float FromLbpH(float l)   { return l / 7936.64144f; }  // lb/h => Kg/s
        public static float ToLbpH(float k)     { return k * 7936.64144f; }  // Kg/s => lb/h
    }

    /// <summary>
    /// Volume conversions from and to m^3
    /// </summary>
    public class Me3 {
        public static float FromFt3(float f) { return f / 35.3146665722f; }    // ft^3 => m^3
        public static float   ToFt3(float m) { return m * 35.3146665722f; }    // m^3 => ft^3
    }

    /// <summary>
    /// Pressure conversions from and to kilopascals
    /// </summary>
    public class KPa {
        public static float FromPSI(float p)    { return p * 6.89475729f; } // PSI => kPa
        public static float   ToPSI(float k)    { return k / 6.89475729f; } // kPa => PSI
    }

    /// <summary>
    /// Area conversions from and to m^2
    /// </summary>
    public class Me2 {
        public static float FromFt2(float f)    { return f / 10.764f; } // ft^2 => m^2
        public static float   ToFt2(float m)    { return m * 10.764f; } // m^2 => ft^2
    }

    /// <summary>
    /// Energy density conversions from and to kJ/Kg
    /// </summary>
    public class KJpKg {
        public static float FromBTUpLb(float b) { return b * 2.326f; }  // btu/lb => kj/kg
        public static float   ToBTUpLb(float k) { return k / 2.326f; }  // kj/kg => btu/lb
    }

    /// <summary>
    /// Liquid volume conversions from and to Litres
    /// </summary>
    public class L {
        public static float FromGUK(float g)    { return g * 4.54609f; }    // UK gallon => litre
        public static float   ToGUK(float l)    { return l / 4.54609f; }    // litre => UK gallon
        public static float FromGUS(float g)    { return g * 3.78541f; }    // US gallon => litre
        public static float   ToGUS(float l)    { return l / 3.78541f; }    // litre => US gallon
    }

    /// <summary>
    /// Pressure rate conversions from and to kilopascals/sec
    /// </summary>
    public class KPapS {
    }

    /// <summary>
    /// Current conversions from and to Amps
    /// </summary>
    public class A {
    }

    /// <summary>
    /// Frequency conversions from and to Hz (revolutions/sec)
    /// </summary>
    public class Hz {
        public static float FromRpM(float r)    { return r / 60f; }     // rev/min => Hz
        public static float   ToRpM(float r)    { return r * 60f; }     // Hz => rev/min
    }

    /// <summary>
    /// Time conversions from and to Seconds
    /// </summary>
    public class S {
        public static float FromM(float m)  { return m / 60f; }     // mins => secs
        public static float   ToM(float s)  { return s * 60f; }     // secs => mins
        public static float FromH(float h)  { return h / 3600f; }   // hours => secs
        public static float   ToH(float s)  { return s * 3600f; }   // secs => hours
    }

    /// <summary>
    /// Temperature conversions from and to Celsius
    /// </summary>
    public class C {
        public static float FromF(float f) { return (f - 32f) * 100f / 180f; }    // Fahrenheit => Celsius
        public static float   ToF(float c) { return (c * 180f / 100f) + 32f; }    // Celsius => Fahrenheit
    }
}
