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
using System.Globalization;
using GNU.Gettext;

namespace ORTS.Common
{
    // Classes are provided for converting into and out of these internal units.
    // OR will use metric units (m, kg, s, A, 'C) for internal properties and calculations, preferably from SI (m/s, not km/hr).
    // Use these classes rather than in-line literal factors.
    //
    // For example to convert a number from metres to inches, use "DiameterIn = M.ToIn(DiameterM);"
    //
    // Many units begin with a lowercase letter (kg, kW, in, lb) but capitalised here (Kg, KW, In, Lb) for ease of reading.
    //
    // Web research suggests that VC++ will optimize "/ 2.0" replacing it with "* 0.5f" but VC# will not and cost is around 15 cycles.
    // To prevent this, we replace "/ 2.0f" by "(1.0f / 2.0f)", which will be replaced by "*0.5f" already in CIL code (verified in CIL).
    // This enables us to use the same number for both directions of conversion, while not costing any speed.
    //
    // Also because of performance reasons, derived quantities still are hard-coded, instead of calling basic conversions and do multiplication
    //
    // Note: this class has unit tests

    /// <summary>
    /// Enumerate the various units of pressure that are used
    /// </summary>
    public enum PressureUnit
    {
        /// <summary>non-defined unit</summary>
        None,
        /// <summary>kiloPascal</summary>
        KPa,
        /// <summary>bar</summary>
        Bar,
        /// <summary>Pounds Per Square Inch</summary>
        PSI,
        /// <summary>Inches Mercury</summary>
        InHg,
        /// <summary>Mass-force per square centimetres</summary>
        KgfpCm2
    }

    /// <summary>
    /// Distance conversions from and to metres
    /// </summary>
    public static class Me {   // Not M to avoid conflict with MSTSMath.M, but note that MSTSMath.M will be gone in future.
        /// <summary>Convert (statute or land) miles to metres</summary>
        public static float FromMi(float miles)  { return miles  * 1609.344f; }
        /// <summary>Convert metres to (statute or land) miles</summary>
        public static float ToMi(float metres)   { return metres * (1.0f / 1609.344f); }
        /// <summary>Convert kilometres to metres</summary>
        public static float FromKiloM(float miles) { return miles * 1000f; }
        /// <summary>Convert metres to kilometres</summary>
        public static float ToKiloM(float metres) { return metres * (1.0f / 1000f); }
        /// <summary>Convert yards to metres</summary>
        public static float FromYd(float yards)  { return yards  * 0.9144f; }
        /// <summary>Convert metres to yards</summary>
        public static float ToYd(float metres)   { return metres * (1.0f / 0.9144f); }
        /// <summary>Convert feet to metres</summary>
        public static float FromFt(float feet)   { return feet   * 0.3048f; }
        /// <summary>Convert metres to feet</summary>
        public static float ToFt(float metres)   { return metres *(1.0f/ 0.3048f); }
        /// <summary>Convert inches to metres</summary>
        public static float FromIn(float inches) { return inches * 0.0254f; }
        /// <summary>Convert metres to inches</summary>
        public static float ToIn(float metres)   { return metres * (1.0f / 0.0254f); }

        /// <summary>
        /// Convert from metres into kilometres or miles, depending on the flag isMetric
        /// </summary>
        /// <param name="distance">distance in metres</param>
        /// <param name="isMetric">if true convert to kilometres, if false convert to miles</param>
        public static float FromM(float distance, bool isMetric)
        {
            return isMetric ? ToKiloM(distance) : ToMi(distance);
        }
        /// <summary>
        /// Convert to metres from kilometres or miles, depending on the flag isMetric
        /// </summary>
        /// <param name="distance">distance to be converted to metres</param>
        /// <param name="isMetric">if true convert from kilometres, if false convert from miles</param>
        public static float ToM(float distance, bool isMetric)
        {
            return isMetric ? FromKiloM(distance) : FromMi(distance);
        }
    }


    /// <summary>
    /// Area conversions from and to m^2
    /// </summary>
    public static class Me2
    {
        /// <summary>Convert from feet squared to metres squared</summary>
        public static float FromFt2(float feet2) { return feet2   * 0.092903f; }
        /// <summary>Convert from metres squared to feet squared</summary>
        public static float ToFt2(float metres2) { return metres2 * (1.0f / 0.092903f); }
        /// <summary>Convert from inches squared to metres squared</summary>
        public static float FromIn2(float feet2) { return feet2   * (1.0f / 1550.0031f); }
        /// <summary>Convert from metres squared to inches squared</summary>
        public static float ToIn2(float metres2) { return metres2 * 1550.0031f; }
    }

    /// <summary>
    /// Volume conversions from and to m^3
    /// </summary>
    public static class Me3
    {
        /// <summary>Convert from cubic feet to cubic metres</summary>
        public static float FromFt3(float feet3) { return feet3   * (1.0f / 35.3146665722f); }
        /// <summary>Convert from cubic metres to cubic feet</summary>
        public static float ToFt3(float metres3) { return metres3 * 35.3146665722f; }
        /// <summary>Convert from cubic inches to cubic metres</summary>
        public static float FromIn3(float inches3) { return inches3 * (1.0f / 61023.7441f); }
        /// <summary>Convert from cubic metres to cubic inches</summary>
        public static float ToIn3(float metres3)   { return metres3 * 61023.7441f; }
    }

    /// <summary>
    /// Speed conversions from and to metres/sec
    /// </summary>
	public static class MpS
    {
        /// <summary>Convert miles/hour to metres/second</summary>
        public static float FromMpH(float milesPerHour)     { return milesPerHour   * (1.0f / 2.23693629f); }
        /// <summary>Convert metres/second to miles/hour</summary>
        public static float ToMpH(float metrePerSecond)     { return metrePerSecond * 2.23693629f; }
        /// <summary>Convert kilometre/hour to metres/second</summary>
        public static float FromKpH(float kilometrePerHour) { return kilometrePerHour * (1.0f / 3.600f); }
        /// <summary>Convert metres/second to kilometres/hour</summary>
        public static float ToKpH(float metrePerSecond)     { return metrePerSecond   * 3.600f; }

        /// <summary>
        /// Convert from metres/second to kilometres/hour or miles/hour, depending on value of isMetric
        /// </summary>
        /// <param name="speed">speed in metres/second</param>
        /// <param name="isMetric">true to convert to kilometre/hour, false to convert to miles/hour</param>
        public static float FromMpS(float speed, bool isMetric)
        {
            return isMetric ? ToKpH(speed) : ToMpH(speed);
        }

        /// <summary>
        /// Convert to metres/second from kilometres/hour or miles/hour, depending on value of isMetric
        /// </summary>
        /// <param name="speed">speed to be converted to metres/second</param>
        /// <param name="isMetric">true to convert from kilometre/hour, false to convert from miles/hour</param>
        public static float ToMpS(float speed, bool isMetric)
        {
            return isMetric ? FromKpH(speed) : FromMpH(speed);
        }
    }

    /// <summary>
    /// Mass conversions from and to Kilograms
    /// </summary>
    public static class Kg
    {
        /// <summary>Convert from pounds (lb) to kilograms</summary>
        public static float FromLb(float lb)     { return lb * (1.0f / 2.20462f); }
        /// <summary>Convert from kilograms to pounds (lb)</summary>
        public static float ToLb(float kg)       { return kg * 2.20462f; }
        /// <summary>Convert from US Tons to kilograms</summary>
        public static float FromTUS(float tonsUS) { return tonsUS * 907.1847f; }
        /// <summary>Convert from kilograms to US Tons</summary>
        public static float ToTUS(float kg)       { return kg     * (1.0f / 907.1847f); }
        /// <summary>Convert from UK Tons to kilograms</summary>
        public static float FromTUK(float tonsUK) { return tonsUK * 1016.047f; }
        /// <summary>Convert from kilograms to UK Tons</summary>
        public static float ToTUK(float kg)       { return kg     * (1.0f / 1016.047f); }
        /// <summary>Convert from kilogram to metric tonnes</summary>
        public static float ToTonne(float kg)      { return kg    * (1.0f / 1000.0f); }
        /// <summary>Convert from metrix tonnes to kilogram</summary>
        public static float FromTonne(float tonne) { return tonne * 1000.0f; }
    }

    /// <summary>
    /// Force conversions from and to Newtons
    /// </summary>
    public static class N
    {
        /// <summary>Convert from pound-force to Newtons</summary>
        public static float FromLbf(float lbf)  { return lbf    * (1.0f / 0.224808943871f); }
        /// <summary>Convert from Newtons to Pound-force</summary>
        public static float ToLbf(float newton) { return newton * 0.224808943871f; }
    }

    /// <summary>
    /// Mass rate conversions from and to Kg/s
    /// </summary>
    public static class KgpS
    {
        /// <summary>Convert from pound/hour to kilograms/second</summary>
        public static float FromLbpH(float poundsPerHour)    { return poundsPerHour      * (1.0f / 7936.64144f); }
        /// <summary>Convert from kilograms/second to pounds/hour</summary>
        public static float ToLbpH(float kilogramsPerSecond) { return kilogramsPerSecond * 7936.64144f; }
    }

    /// <summary>
    /// Energy conversions from and to Joule
    /// </summary>
    public static class J
    {
        /// <summary>Convert from kiloJoules to Joules</summary>
        public static float FromKJ(float kiloJoules) { return kiloJoules * 1000f; }
        /// <summary>Convert from Joules to kileJoules</summary>
        public static float ToKJ(float joules) { return joules * (1.0f / 1000f); }
    }

    /// <summary>
    /// Power conversions from and to Watts
    /// </summary>
    public static class W
    {
        /// <summary>Convert from kiloWatts to Watts</summary>
        public static float FromKW(float kiloWatts) { return kiloWatts * 1000f; }
        /// <summary>Convert from Watts to kileWatts</summary>
        public static float ToKW(float watts)       { return watts     * (1.0f / 1000f); }
        /// <summary>Convert from HorsePower to Watts</summary>
        public static float FromHp(float horsePowers) { return horsePowers * 745.699872f; }
        /// <summary>Convert from Watts to HorsePower</summary>
        public static float ToHp(float watts)         { return watts       * (1.0f / 745.699872f); }
        /// <summary>Convert from BoilerHorsePower to Watts</summary>
        public static float FromBhp(float horsePowers) { return horsePowers * 9809.5f; }
        /// <summary>Convert from Watts to BoilerHorsePower</summary>
        public static float ToBhp(float watts) { return watts * (1.0f / 9809.5f); }
        /// <summary>Convert from British Thermal Unit (BTU) per second to watts</summary>
        public static float FromBTUpS(float btuPerSecond) { return btuPerSecond * 1055.05585f; }
        /// <summary>Convert from Watts to British Thermal Unit (BTU) per second</summary>
        public static float ToBTUpS(float watts)          { return watts        * (1.0f / 1055.05585f); }
    }

    /// <summary>
    /// Stiffness conversions from and to Newtons/metre
    /// </summary>
    public static class NpM
    {
    }

    /// <summary>
    /// Resistance conversions from and to Newtons/metre/sec
    /// </summary>
    public static class NSpM
    {
        /// <summary>Convert from pounds per mph to newtons per meter per second</summary>
        public static float FromLbfpMpH(float lbfPerMpH) { return lbfPerMpH * 9.9503884f; }
        /// <summary>Convert from newtons per meter per second to pounds per mph</summary>
        public static float ToLbfpMpH(float nPerMpS) { return nPerMpS / 9.9503884f; }
    }

    /// <summary>
    /// Resistance conversions from and to Newtons/metre^2/sec^2
    /// </summary>
    public static class NSSpMM
    {
        /// <summary>Convert from pounds per mph^2 to newtons per mps^2</summary>
        public static float FromLbfpMpH2(float lbfPerMpH2) { return lbfPerMpH2 * 22.2583849f; }
        /// <summary>Convert from newtons per mps^2 to pounds per mph^2</summary>
        public static float ToLbfpMpH2(float nPerMpS2) { return nPerMpS2 / 22.2583849f; }
    }

    /// <summary>
    /// Pressure conversions from and to kilopascals
    /// </summary>
    public static class KPa
    {
        /// <summary>Convert from Pounds per Square Inch to kiloPascal</summary>
        public static float FromPSI(float psi) { return psi * 6.89475729f; }
        /// <summary>Convert from kiloPascal to Pounds per Square Inch</summary>
        public static float ToPSI(float kiloPascal) { return kiloPascal * (1.0f / 6.89475729f); }
        /// <summary>Convert from Inches Mercury to kiloPascal</summary>
        public static float FromInHg(float inchesMercury) { return inchesMercury * 3.386389f; }
        /// <summary>Convert from kiloPascal to Inches Mercury</summary>
        public static float ToInHg(float kiloPascal) { return kiloPascal * (1.0f / 3.386389f); }
        /// <summary>Convert from Bar to kiloPascal</summary>
        public static float FromBar(float bar) { return bar * 100.0f; }
        /// <summary>Convert from kiloPascal to Bar</summary>
        public static float ToBar(float kiloPascal) { return kiloPascal * (1.0f / 100.0f); }
        /// <summary>Convert from mass-force per square metres to kiloPascal</summary>
        public static float FromKgfpCm2(float f) { return f * 98.068059f; }
        /// <summary>Convert from kiloPascal to mass-force per square centimetres</summary>
        public static float ToKgfpCm2(float kiloPascal) { return kiloPascal * (1.0f / 98.068059f); }

        /// <summary>
        /// Convert from KPa to any pressure unit
        /// </summary>
        /// <param name="pressure">pressure to convert from</param>
        /// <param name="outputUnit">Unit to convert To</param>
        public static float FromKPa(float pressure, PressureUnit outputUnit)
        {
            switch (outputUnit)
            {
                case PressureUnit.KPa:
                    return pressure;
                case PressureUnit.Bar:
                    return ToBar(pressure);
                case PressureUnit.InHg:
                    return ToInHg(pressure);
                case PressureUnit.KgfpCm2:
                    return ToKgfpCm2(pressure);
                case PressureUnit.PSI:
                    return ToPSI(pressure);
                default:
                    throw new ArgumentOutOfRangeException("Pressure unit not recognized");
            }
        }

        /// <summary>
        /// Convert from any pressure unit to KPa
        /// </summary>
        /// <param name="pressure">pressure to convert from</param>
        /// <param name="inputUnit">Unit to convert from</param>
        public static float ToKPa(float pressure, PressureUnit inputUnit)
        {
            switch (inputUnit)
            {
                case PressureUnit.KPa:
                    return pressure;
                case PressureUnit.Bar:
                    return FromBar(pressure);
                case PressureUnit.InHg:
                    return FromInHg(pressure);
                case PressureUnit.KgfpCm2:
                    return FromKgfpCm2(pressure);
                case PressureUnit.PSI:
                    return FromPSI(pressure);
                default:
                    throw new ArgumentOutOfRangeException("Pressure unit not recognized");
            }
        }
    }

    /// <summary>
    /// Pressure conversions from and to bar
    /// </summary>
    public static class Bar
    {
        /// <summary>Convert from kiloPascal to Bar</summary>
        public static float FromKPa(float kiloPascal) { return kiloPascal * (1.0f / 100.0f); }
        /// <summary>Convert from bar to kiloPascal</summary>
        public static float ToKPa(float bar) { return bar * 100.0f; }
        /// <summary>Convert from Pounds per Square Inch to Bar</summary>
        public static float FromPSI(float poundsPerSquareInch) { return poundsPerSquareInch * (1.0f / 14.5037738f); }
        /// <summary>Convert from Bar to Pounds per Square Inch</summary>
        public static float ToPSI(float bar) { return bar * 14.5037738f; }
        /// <summary>Convert from Inches Mercury to bar</summary>
        public static float FromInHg(float inchesMercury) { return inchesMercury * 0.03386389f; }
        /// <summary>Convert from bar to Inches Mercury</summary>
        public static float ToInHg(float bar) { return bar * (1.0f / 0.03386389f); }
        /// <summary>Convert from mass-force per square metres to bar</summary>
        public static float FromKgfpCm2(float f) { return f * (1.0f / 1.0197f); }
        /// <summary>Convert from bar to mass-force per square metres</summary>
        public static float ToKgfpCm2(float bar) { return bar * 1.0197f; }
    }

    /// <summary>
    /// Pressure rate conversions from and to bar/s
    /// </summary>
    public static class BarpS
    {
        /// <summary>Convert from Pounds per square Inch per second to bar per second</summary>
        public static float FromPSIpS(float psi) { return psi * (1.0f / 14.5037738f); }
        /// <summary>Convert from</summary>
        public static float ToPSIpS(float bar) { return bar * 14.5037738f; }
    }

    /// <summary>
    /// Energy density conversions from and to kJ/Kg
    /// </summary>
    public static class KJpKg
    {
        /// <summary>Convert from Britisch Thermal Units per Pound to kiloJoule per kilogram</summary>
        public static float FromBTUpLb(float btuPerPound) { return btuPerPound * 2.326f; }
        /// <summary>Convert from kiloJoule per kilogram to Britisch Thermal Units per Pound</summary>
        public static float ToBTUpLb(float kJPerkg) { return kJPerkg * (1.0f / 2.326f); }
    }

    /// <summary>
    /// Energy density conversions from and to kJ/m^3
    /// </summary>
    public static class KJpM3
    {
        /// <summary>Convert from Britisch Thermal Units per ft^3 to kiloJoule per m^3</summary>
        public static float FromBTUpFt3(float btuPerFt3) { return btuPerFt3 * (1f / 37.3f); }
        /// <summary>Convert from kiloJoule per m^3 to Britisch Thermal Units per ft^3</summary>
        public static float ToBTUpFt3(float kJPerM3) { return kJPerM3 * 37.3f; }
    }

    /// <summary>
    /// Liquid volume conversions from and to Litres
    /// </summary>
    public static class L
    {
        /// <summary>Convert from UK Gallons to litres</summary>
        public static float FromGUK(float gallonUK) { return gallonUK * 4.54609f; }
        /// <summary>Convert from litres to UK Gallons</summary>
        public static float ToGUK(float litre) { return litre * (1.0f / 4.54609f); }
        /// <summary>Convert from US Gallons to litres</summary>
        public static float FromGUS(float gallonUS) { return gallonUS * 3.78541f; }
        /// <summary>Convert from litres to US Gallons</summary>
        public static float ToGUS(float litre) { return litre * (1.0f / 3.78541f); }
    }


    /// <summary>
    /// convert vacuum values to psia for vacuum brakes
    /// </summary>
    public static class Vac
    {
        readonly static float OneAtmospherePSI = Bar.ToPSI(1);
        /// <summary>vacuum in inhg to pressure in psia</summary>
        public static float ToPress(float vac) { return OneAtmospherePSI - Bar.ToPSI(Bar.FromInHg(vac)); }
        /// <summary>convert pressure in psia to vacuum in inhg</summary>
        public static float FromPress(float press) { return Bar.ToInHg(Bar.FromPSI(OneAtmospherePSI - press)); }
    }

    /// <summary>
    /// Current conversions from and to Amps
    /// </summary>
    public static class A
    {
    }

    /// <summary>
    /// Frequency conversions from and to Hz (revolutions/sec)
    /// </summary>
    public static class pS
    {
        /// <summary>Convert from per Minute to per Second</summary>
        public static float FrompM(float revPerMinute) { return revPerMinute * (1.0f / 60f); }
        /// <summary>Convert from per Second to per Minute</summary>
        public static float TopM(float revPerSecond) { return revPerSecond * 60f; }
        /// <summary>Convert from per Hour to per Second</summary>
        public static float FrompH(float revPerHour) { return revPerHour * (1.0f / 3600f); }
        /// <summary>Convert from per Second to per Hour</summary>
        public static float TopH(float revPerSecond) { return revPerSecond * 3600f; }
    }

    /// <summary>
    /// Time conversions from and to Seconds
    /// </summary>
    public static class S
    {
        /// <summary>Convert from minutes to seconds</summary>
        public static float FromM(float minutes) { return minutes * 60f; }
        /// <summary>Convert from seconds to minutes</summary>
        public static float ToM(float seconds) { return seconds * (1.0f / 60f); }
        /// <summary>Convert from hours to seconds</summary>
        public static float FromH(float hours) { return hours * 3600f; }
        /// <summary>Convert from seconds to hours</summary>
        public static float ToH(float seconds) { return seconds * (1.0f / 3600f); }
    }

    /// <summary>
    /// Temperature conversions from and to Celsius
    /// </summary>
    public static class C
    {
        /// <summary>Convert from degrees Fahrenheit to degrees Celcius</summary>
        public static float FromF(float fahrenheit) { return (fahrenheit - 32f) * (100f / 180f); }
        /// <summary>Convert from degrees Celcius to degrees Fahrenheit</summary>
        public static float ToF(float celcius) { return celcius * (180f / 100f) + 32f; }
        /// <summary>Convert temperature difference from degrees Fahrenheit to degrees Celcius</summary>
        public static float FromDeltaF(float fahrenheit) { return fahrenheit * (100f / 180f); }
        /// <summary>Convert temperature difference from degrees Celcius to degrees Fahrenheit</summary>
        public static float ToDeltaF(float celcius) { return celcius * (180f / 100f); }
        /// <summary>Convert from Kelving to degrees Celcius</summary>
        public static float FromK(float kelvin) { return kelvin - 273.15f; }
        /// <summary>Convert from degress Celcius to Kelvin</summary>
        public static float ToK(float celcius) { return celcius + 273.15f; }
    }

    /// <summary>
    /// Class to compare times taking into account times after midnight
    /// (morning comes after night comes after evening, but morning is before afternoon, which is before evening)
    /// </summary>
    public static class CompareTimes
    {
        static int eightHundredHours = 8 * 3600;
        static int sixteenHundredHours = 16 * 3600;

        /// <summary>
        /// Return the latest time of the two input times, keeping in mind that night/morning is after evening/night
        /// </summary>
        public static int LatestTime(int time1, int time2)
        {
            if (time1 > sixteenHundredHours && time2 < eightHundredHours)
            {
                return (time2);
            }
            else if (time1 < eightHundredHours && time2 > sixteenHundredHours)
            {
                return (time1);
            }
            else if (time1 > time2)
            {
                return (time1);
            }
            return (time2);
        }

        /// <summary>
        /// Return the Earliest time of the two input times, keeping in mind that night/morning is after evening/night
        /// </summary>
        public static int EarliestTime(int time1, int time2)
        {
            if (time1 > sixteenHundredHours && time2 < eightHundredHours)
            {
                return (time1);
            }
            else if (time1 < eightHundredHours && time2 > sixteenHundredHours)
            {
                return (time2);
            }
            else if (time1 > time2)
            {
                return (time2);
            }
            return (time1);
        }
    }


    /// <summary>
    /// Class to convert various quantities (so a value with a unit) into nicely formatted strings for display
    /// </summary>
    public static class FormatStrings
    {
        public static GettextResourceManager Catalog = new GettextResourceManager("ORTS.Common");
        public static string m = Catalog.GetString("m");
        public static string km = Catalog.GetString("km");
        public static string mm = Catalog.GetString("mm");
        public static string mi = Catalog.GetString("mi");
        public static string ft = Catalog.GetString("ft");
        public static string yd = Catalog.GetString("yd");
        public static string m2 = Catalog.GetString("m²");
        public static string ft2 = Catalog.GetString("ft²");
        public static string m3 = Catalog.GetString("m³");
        public static string ft3 = Catalog.GetString("ft³");
        public static string kmph = Catalog.GetString("km/h");
        public static string mph = Catalog.GetString("mph");
        public static string kpa = Catalog.GetString("kPa");
        public static string bar = Catalog.GetString("bar");
        public static string psi = Catalog.GetString("psi");
        public static string inhg = Catalog.GetString("inHg");
        public static string kgfpcm2 = Catalog.GetString("kgf/cm²");
        public static string lps = Catalog.GetString("L/s");
        public static string lpm = Catalog.GetString("L/min");
        public static string cfm = Catalog.GetString("cfm");
        public static string kg = Catalog.GetString("kg");
        public static string t = Catalog.GetString("t");
        public static string tonUK = Catalog.GetString("t-uk");
        public static string tonUS = Catalog.GetString("t-us");
        public static string lb = Catalog.GetString("lb");
        public static string s = Catalog.GetString("s");
        public static string min = Catalog.GetString("min");
        public static string h = Catalog.GetString("h");
        public static string l = Catalog.GetString("L");
        public static string galUK = Catalog.GetString("g-uk");
        public static string galUS = Catalog.GetString("g-us");
        public static string rpm = Catalog.GetString("rpm");
        public static string kW = Catalog.GetString("kW");
        public static string hp = Catalog.GetString("hp"); // mechanical (or brake) horsepower
        public static string bhp = Catalog.GetString("bhp"); // boiler horsepower
        public static string V = Catalog.GetString("V");
        public static string kV = Catalog.GetString("kV");
        public static string kJ = Catalog.GetString("kJ");
        public static string MJ = Catalog.GetString("MJ");
        public static string btu = Catalog.GetString("BTU");
        public static string c = Catalog.GetString("°C");
        public static string f = Catalog.GetString("°F");
        public static string n = Catalog.GetString("N");
        public static string kN = Catalog.GetString("kN");
        public static string nspm = Catalog.GetString("N/m/s");
        public static string nsspmm = Catalog.GetString("N/(m/s)²");
        public static string lbf = Catalog.GetString("lbf");
        public static string klbf = Catalog.GetString("klbf");
        public static string lbfpmph = Catalog.GetString("lbf/mph");
        public static string lbfpmph2 = Catalog.GetString("lbf/mph²");
        public static string deg = Catalog.GetString("°");

        /// <summary>
        /// Formatted unlocalized speed string, used in reports and logs.
        /// </summary>
        public static string FormatSpeed(float speed, bool isMetric)
        {
            return String.Format(CultureInfo.CurrentCulture,
                "{0:F1}{1}", MpS.FromMpS(speed, isMetric), isMetric ? kmph : mph);
        }

        /// <summary>
        /// Formatted localized speed string, used to display tracking speed, with 1 decimal precision
        /// </summary>
        public static string FormatSpeedDisplay(float speed, bool isMetric)
        {
            return String.Format(CultureInfo.CurrentCulture,
                "{0:F1} {1}", MpS.FromMpS(speed, isMetric), isMetric ? kmph : mph);
        }

        /// <summary>
        /// Formatted localized speed string, used to display tracking speed, with 2 decimal precision
        /// </summary>
        public static string FormatVeryLowSpeedDisplay(float speed, bool isMetric)
        {
            return String.Format(CultureInfo.CurrentCulture,
                "{0:F2} {1}", MpS.FromMpS(speed, isMetric), isMetric ? kmph : mph);
        }

        /// <summary>
        /// Formatted localized speed string, used to display speed limits, with 0 decimal precision
        /// </summary>
        public static string FormatSpeedLimit(float speed, bool isMetric)
        {
            return String.Format(CultureInfo.CurrentCulture,
                "{0:F0} {1}", MpS.FromMpS(speed, isMetric), isMetric ? kmph : mph);
        }

        /// <summary>
        /// Formatted localized speed string, used to display speed limits, with 0 decimal precision and no unit of measure
        /// </summary>
        public static string FormatSpeedLimitNoUoM(float speed, bool isMetric)
        {
            return String.Format(CultureInfo.CurrentCulture,
                "{0:F0}", MpS.FromMpS(speed, isMetric));
        }

        /// <summary>
        /// Formatted unlocalized distance string, used in reports and logs.
        /// </summary>
        public static string FormatDistance(float distance, bool isMetric)
        {
            if (isMetric)
            {
                // <0.1 kilometres, show metres.
                if (Math.Abs(distance) < 100)
                {
                    return String.Format(CultureInfo.CurrentCulture,
                        "{0:N0}m", distance);
                }
                return String.Format(CultureInfo.CurrentCulture,
                    "{0:F1}km", Me.ToKiloM(distance));
            }
            // <0.1 miles, show yards.
            if (Math.Abs(distance) < Me.FromMi(0.1f))
            {
                return String.Format(CultureInfo.CurrentCulture, "{0:N0}yd", Me.ToYd(distance));
            }
            return String.Format(CultureInfo.CurrentCulture, "{0:F1}mi", Me.ToMi(distance));
        }

        /// <summary>
        /// Formatted localized distance string, as displayed in in-game windows
        /// </summary>
        public static string FormatDistanceDisplay(float distance, bool isMetric)
        {
            if (isMetric)
            {
                // <0.1 kilometres, show metres.
                if (Math.Abs(distance) < 100)
                {
                    return String.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", distance, m);
                }
                return String.Format(CultureInfo.CurrentCulture, "{0:F1} {1}", Me.ToKiloM(distance), km);
            }
            // <0.1 miles, show yards.
            if (Math.Abs(distance) < Me.FromMi(0.1f))
            {
                return String.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", Me.ToYd(distance), yd);
            }
            return String.Format(CultureInfo.CurrentCulture, "{0:F1} {1}", Me.ToMi(distance), mi);
        }

        public static string FormatShortDistanceDisplay(float distanceM, bool isMetric)
        {
            if (isMetric)
                return String.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", distanceM, m);
            return String.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", Me.ToFt(distanceM), ft);
        }

        public static string FormatVeryShortDistanceDisplay(float distanceM, bool isMetric)
        {
            if (isMetric)
                return String.Format(CultureInfo.CurrentCulture, "{0:N3} {1}", distanceM, m);
            return String.Format(CultureInfo.CurrentCulture, "{0:N3} {1}", Me.ToFt(distanceM), ft);
        }

        public static string FormatMillimeterDistanceDisplay(float distanceM, bool isMetric)
        {
            if (isMetric)
                return String.Format(CultureInfo.CurrentCulture, "{0:N0} {1}", distanceM * 1000.0f, mm);
            return String.Format(CultureInfo.CurrentCulture, "{0:N1} {1}", Me.ToIn(distanceM), "in");
        }

        /// <summary>
        /// format localized mass string, as displayed in in-game windows.
        /// </summary>
        /// <param name="massKg">mass in kg or in Lb</param>
        /// <param name="isMetric">use kg if true, Lb if false</param>
        public static string FormatMass(float massKg, bool isMetric)
        {
            if (isMetric)
            {
                // < 1 tons, show kilograms.
                float massInTonne = Kg.ToTonne(massKg);
                if (Math.Abs(massInTonne) > 1)
                {
                    return String.Format(CultureInfo.CurrentCulture, "{0:F1} {1}", massInTonne, t);
                }
                else
                {
                    return String.Format(CultureInfo.CurrentCulture, "{0:F0} {1}", massKg, kg);
                }
            }
            else
            {
                return String.Format(CultureInfo.CurrentCulture,"{0:F0} {1}", Kg.ToLb(massKg), lb);
            }
        }

        public static string FormatLargeMass(float massKg, bool isMetric, bool isUK)
        {
            if (isMetric)
                return FormatMass(massKg, isMetric);

            var massT = isUK ? Kg.ToTUK(massKg) : Kg.ToTUS(massKg);
            if (massT > 1)
                return String.Format(CultureInfo.CurrentCulture, "{0:F1} {1}", massT, isUK ? tonUK : tonUS);
            else
                return FormatMass(massKg, isMetric);
        }

        public static string FormatArea(float areaM2, bool isMetric)
        {
            var area = isMetric ? areaM2 : Me2.ToFt2(areaM2);
            return String.Format(CultureInfo.CurrentCulture, "{0:F0} {1}", area, isMetric ? m2 : ft2);
        }

        public static string FormatVolume(float volumeM3, bool isMetric)
        {
            var volume = isMetric ? volumeM3 : Me3.ToFt3(volumeM3);
            return String.Format(CultureInfo.CurrentCulture, "{0:F0} {1}", volume, isMetric ? m3 : ft3);
        }

        public static string FormatSmallVolume(float volumeM3, bool isMetric)
        {
            var volume = isMetric ? volumeM3 : Me3.ToFt3(volumeM3);
            return String.Format(CultureInfo.CurrentCulture, "{0:N3} {1}", volume, isMetric ? m3 : ft3);
        }

        public static string FormatFuelVolume(float volumeL, bool isMetric, bool isUK)
        {
            var volume = isMetric ? volumeL : isUK ? L.ToGUK(volumeL) : L.ToGUS(volumeL);
            return String.Format(CultureInfo.CurrentCulture, "{0:F1} {1}", volume, isMetric ? l : isUK ? galUK : galUS);
        }

        public static string FormatPower(float powerW, bool isMetric, bool isImperialBHP, bool isImperialBTUpS)
        {
            var power = isMetric ? W.ToKW(powerW) : isImperialBHP ? W.ToBhp(powerW) : isImperialBTUpS ? W.ToBTUpS(powerW) : W.ToHp(powerW);
            return String.Format(CultureInfo.CurrentCulture, "{0:F0} {1}", power, isMetric ? kW : isImperialBHP ? bhp : isImperialBTUpS ? String.Format("{0}/{1}", btu, s) : hp);
        }

        public static string FormatVoltage(float voltageV)
        {
            bool kilo = false;
            var voltage = voltageV;
            if (Math.Abs(voltage) > 1e4f)
            {
                voltage *= 1e-3f;
                kilo = true;
            }
            var unit = kilo ? kV : V;
            return String.Format(CultureInfo.CurrentCulture, kilo ? "{0:F1} {1}" : "{0:F0} {1}", voltage, unit);
        }

        public static string FormatForce(float forceN, bool isMetric)
        {
            var kilo = false;
            var force = isMetric ? forceN : N.ToLbf(forceN);
            if (kilo = Math.Abs(force) > 1e4f) force *= 1e-3f;
            var unit = isMetric ? kilo ? kN : n : kilo ? klbf : lbf;
            return String.Format(CultureInfo.CurrentCulture, kilo ? "{0:F1} {1}" : "{0:F0} {1}", force, unit);
        }

        public static string FormatLargeForce(float forceN, bool isMetric)
        {
            var force = isMetric ? forceN : N.ToLbf(forceN);
            var unit = isMetric ? kN : klbf;
            return String.Format(CultureInfo.CurrentCulture, "{0:F1} {1}", force * 1e-3f, unit);
        }

        public static string FormatLinearResistance(float resistanceNSpM, bool isMetric)
        {
            var resistance = isMetric ? resistanceNSpM : NSpM.ToLbfpMpH(resistanceNSpM);
            return String.Format(CultureInfo.CurrentCulture, isMetric ? "{0:F1} {1}" : "{0:F2} {2}", resistance, nspm, lbfpmph);
        }

        public static string FormatQuadraticResistance(float resistanceNSSpMM, bool isMetric)
        {
            var resistance = isMetric ? resistanceNSSpMM : NSSpMM.ToLbfpMpH2(resistanceNSSpMM);
            return String.Format(CultureInfo.CurrentCulture, isMetric ? "{0:F3} {1}" : "{0:F4} {2}", resistance, nsspmm, lbfpmph2);
        }

        public static string FormatTemperature(float temperatureC, bool isMetric, bool isDelta)
        {
            var temperature = isMetric ? temperatureC : isDelta ? C.ToDeltaF(temperatureC) : C.ToF(temperatureC);
            return String.Format(CultureInfo.CurrentCulture, "{0:F0}{1}", temperature, isMetric ? c : f);
        }

        public static string FormatEnergyDensityByMass(float edKJpKg, bool isMetric)
        {
            var calorie = isMetric ? edKJpKg : KJpKg.ToBTUpLb(edKJpKg);
            return String.Format(CultureInfo.CurrentCulture, "{0:F0} {1}/{2}", calorie, isMetric ? kJ : btu, isMetric ? kg : lb);
        }

        public static string FormatEnergyDensityByVolume(float edKJpM3, bool isMetric)
        {
            var calorie = isMetric ? edKJpM3 : KJpM3.ToBTUpFt3(edKJpM3);
            return String.Format(CultureInfo.CurrentCulture, "{0:F0} {1}/{2}", calorie, isMetric ? kJ : btu, String.Format("{0}³", isMetric ? m : ft));
        }

        public static string FormatEnergy(float energyJ, bool isMetric)
        {
            var energy = isMetric ? energyJ * 1e-6f : W.ToBTUpS(energyJ);
            return String.Format(CultureInfo.CurrentCulture, "{0:F0} {1}", energy, isMetric ? MJ : btu);
        }

        /// <summary>
        /// Formatted localized pressure string
        /// </summary>
        public static string FormatPressure(float pressure, PressureUnit inputUnit, PressureUnit outputUnit, bool unitDisplayed)
        {
            if (inputUnit == PressureUnit.None || outputUnit == PressureUnit.None)
                return string.Empty;

            float pressureKPa = KPa.ToKPa(pressure, inputUnit);
            float pressureOut = KPa.FromKPa(pressureKPa, outputUnit);

            string unit = "";
            string format = "";
            switch (outputUnit)
            {
                case PressureUnit.KPa:
                    unit = kpa;
                    format = "{0:F0}";
                    break;

                case PressureUnit.Bar:
                    unit = bar;
                    format = "{0:F1}";
                    break;

                case PressureUnit.PSI:
                    unit = psi;
                    format = "{0:F0}";
                    break;

                case PressureUnit.InHg:
                    unit = inhg;
                    format = "{0:F0}";
                    break;

                case PressureUnit.KgfpCm2:
                    unit = kgfpcm2;
                    format = "{0:F1}";
                    break;
            }

            if (unitDisplayed)
            {
                format += " " + unit;
            }

            return String.Format(CultureInfo.CurrentCulture, format, pressureOut);
        }

        public static string FormatAirFlow(float flowM3pS, bool isMetric)
        {
            var flow = isMetric ? flowM3pS * 1000.0f : flowM3pS * 35.3147f * 60.0f;
            return String.Format(CultureInfo.CurrentCulture, "{0:F0} {1}", flow, isMetric ? lps : cfm);
        }

        public static string FormatAngleDeg(float angleDeg)
        {
            return String.Format(CultureInfo.CurrentCulture, "{0:F0} {1}", angleDeg, deg);
        }

        /// <summary>
        /// Converts duration in floating-point seconds to whole hours, minutes and seconds (rounded down).
        /// </summary>
        /// <param name="clockTimeSeconds"></param>
        /// <returns>The time in HH:MM:SS format.</returns>
        public static string FormatTime(double clockTimeSeconds)
        {
            var hour = (int)(clockTimeSeconds / (60 * 60));
            clockTimeSeconds -= hour * 60 * 60;
            var minute = (int)(clockTimeSeconds / 60);
            clockTimeSeconds -= minute * 60;
            var seconds = (int)clockTimeSeconds;

            // Reset clock before and after midnight
            if (hour >= 24)
                hour %= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;
            if (seconds < 0)
                seconds += 60;

            return string.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, seconds);
        }

        /// <summary>
        /// Converts duration in floating-point seconds to whole hours, minutes and seconds and 2 decimal places of seconds.
        /// </summary>
        /// <param name="clockTimeSeconds"></param>
        /// <returns>The time in HH:MM:SS.SS format.</returns>
        public static string FormatPreciseTime(double clockTimeSeconds)
        {
            var hour = (int)(clockTimeSeconds / (60 * 60));
            clockTimeSeconds -= hour * 60 * 60;
            var minute = (int)(clockTimeSeconds / 60);
            clockTimeSeconds -= minute * 60;
            var seconds = clockTimeSeconds;

            // Reset clock before and after midnight
            if (hour >= 24)
                hour %= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;
            if (seconds < 0)
                seconds += 60;

            return string.Format("{0:D2}:{1:D2}:{2:00.00}", hour, minute, seconds);
        }


        /// <summary>
        /// Converts duration in floating-point seconds to whole hours and minutes (rounded to nearest).
        /// </summary>
        /// <param name="clockTimeSeconds"></param>
        /// <returns>The time in HH:MM format.</returns>
        public static string FormatApproximateTime(double clockTimeSeconds)
        {
            var hour = (int)(clockTimeSeconds / (60 * 60));
            clockTimeSeconds -= hour * 60 * 60;
            var minute = (int)Math.Round(clockTimeSeconds / 60);
            clockTimeSeconds -= minute * 60;

            // Reset clock before and after midnight
            if (hour >= 24)
                hour %= 24;
            if (hour < 0)
                hour += 24;
            if (minute < 0)
                minute += 60;

            return string.Format("{0:D2}:{1:D2}", hour, minute);
        }
    }
}
