// COPYRIGHT 2013, 2014, 2015 by the Open Rails project.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;
using Xunit;

#region Integration tests (all tests from original reader)
namespace Tests.Orts.Parsers.Msts
{
    public static class StfReaderIntegration
    {
        /* All conversion factors have been sourced from:
         *   - https://en.wikipedia.org/wiki/Conversion_of_units
         *   - Windows 8.1 Calculator utility
         * 
         * In any case where there is disagreement or only 1 source available, the
         * chosen source is specified. In all other cases, all sources exist and agree.
         * 
         *********************************************************************
         * DO NOT CHANGE ANY OF THESE WITHOUT CONSULTING OTHER TEAM MEMBERS! *
         *********************************************************************/

        const double BarToPascal = 100000;
        const double CelsiusToKelvin = 273.15;
        const double DayToSecond = 86400;
        const double FahrenheitToKelvinA = 459.67;
        const double FahrenheitToKelvinB = 5 / 9;
        const double FeetToMetre = 0.3048;
        const double GallonUSToCubicMetre = 0.003785411784; // (fluid; Wine)
        const double HorsepowerToWatt = 745.69987158227022; // (imperial mechanical hoursepower)
        const double HourToSecond = 3600;
        const double InchOfMercuryToPascal = 3386.389; // (conventional)
        const double InchToMetre = 0.0254;
        const double KilometrePerHourToMetrePerSecond = 1 / 3.6;
        const double LitreToCubicMetre = 0.001;
        const double MilePerHourToMetrePerSecond = 0.44704;
        const double MileToMetre = 1609.344;
        const double MinuteToSecond = 60;
        const double PascalToPSI = 0.0001450377438972831; // Temporary while pressure values are returned in PSI instead of Pascal.
        const double PoundForceToNewton = 4.4482216152605; // Conversion_of_units
        const double PoundToKG = 0.45359237;
        const double PSIToPascal = 6894.757;
        const double TonLongToKG = 1016.0469088;
        const double TonneToKG = 1000;
        const double TonShortToKG = 907.18474;

        [Fact]
        public static void EmptyFile()
        {
            AssertWarnings.Expected(); // many warnings will result!
            using (var reader = new STFReader(new MemoryStream(Encoding.ASCII.GetBytes("")), "EmptyFile.stf", Encoding.ASCII, false))
            {
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal("EmptyFile.stf", reader.FileName);
                Assert.Equal(1, reader.LineNumber);
                Assert.Null(reader.SimisSignature);
                // Note, the Debug.Assert() in reader.Tree is already captured by AssertWarnings.Expected.
                // For the rest, we do not care which exception is being thrown.
                var exception = Record.Exception(() => reader.Tree);
                Assert.NotNull(exception);

                // All of the following will execute successfully at EOF..., although they might give warnings.
                reader.MustMatch("ANYTHING GOES");
                reader.ParseBlock(new STFReader.TokenProcessor[0]);
                reader.ParseFile(new STFReader.TokenProcessor[0]);
                Assert.Equal(-1, reader.PeekPastWhitespace());
                Assert.False(reader.ReadBoolBlock(false));
                Assert.Equal(0, reader.ReadDouble(null));
                Assert.Equal(0, reader.ReadDoubleBlock(null));
                Assert.Equal(0, reader.ReadFloat(STFReader.UNITS.None, null));
                Assert.Equal(0, reader.ReadFloatBlock(STFReader.UNITS.None, null));
                Assert.Equal(0U, reader.ReadHex(null));
                Assert.Equal(0U, reader.ReadHexBlock(null));
                Assert.Equal(0, reader.ReadInt(null));
                Assert.Equal(0, reader.ReadIntBlock(null));
                Assert.Equal("", reader.ReadItem());
                Assert.Equal("", reader.ReadString());
                Assert.Null(reader.ReadStringBlock(null));
                Assert.Equal(0U, reader.ReadUInt(null));
                Assert.Equal(0U, reader.ReadUIntBlock(null));
                Assert.Equal(Vector3.Zero, reader.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero));
                Assert.Equal(Vector4.Zero, reader.ReadVector4Block(STFReader.UNITS.None, Vector4.Zero));
            }
        }

        [Fact]
        public static void EncodingAscii()
        {
            var reader = new STFReader(new MemoryStream(Encoding.ASCII.GetBytes("TheBlock()")), "", Encoding.ASCII, false);
            reader.MustMatch("TheBlock");
        }

        [Fact]
        public static void EncodingUtf16()
        {
            var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("TheBlock()")), "", Encoding.Unicode, false);
            reader.MustMatch("TheBlock");
        }

        [Fact]
        public static void EmptyBlock()
        {
            AssertWarnings.Expected();
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("TheBlock()")), "EmptyBlock.stf", Encoding.Unicode, false))
            {
                Assert.False(reader.Eof, "STFReader.Eof");
                Assert.False(reader.EOF(), "STFReader.EOF()");
                Assert.False(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal("EmptyBlock.stf", reader.FileName);
                Assert.Equal(1, reader.LineNumber);
                Assert.Null(reader.SimisSignature);
                Assert.Throws<STFException>(() => reader.MustMatch("Something Else"));
                // We can't rewind the STFReader and it has advanced forward now. :(
            }
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("TheBlock()")), "", Encoding.Unicode, false))
            {
                reader.MustMatch("TheBlock");
                Assert.False(reader.Eof, "STFReader.Eof");
                Assert.False(reader.EOF(), "STFReader.EOF()");
                Assert.False(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
                reader.VerifyStartOfBlock(); // Same as reader.MustMatch("(");
                Assert.False(reader.Eof, "STFReader.Eof");
                Assert.False(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
                reader.MustMatch(")");
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
            }
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("TheBlock()")), "", Encoding.Unicode, false))
            {
                var called = 0;
                var not_called = 0;
                reader.ParseBlock(new[] {
                    new STFReader.TokenProcessor("theblock", () => { called++; }),
                    new STFReader.TokenProcessor("TheBlock", () => { not_called++; })
                });
                Assert.True(called == 1, "TokenProcessor for theblock must be called exactly once: called = " + called);
                Assert.True(not_called == 0, "TokenProcessor for TheBlock must not be called: not_called = " + not_called);
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
            }
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("TheBlock()")), "", Encoding.Unicode, false))
            {
                reader.MustMatch("TheBlock");
                reader.SkipBlock();
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
            }
        }

        [Fact]
        public static void NumericFormats()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("1.123456789 1e9 1.1e9 2.123456 2e9 2.1e9 00ABCDEF 123456 -123456 234567")), "", Encoding.Unicode, false))
            {
                Assert.Equal(1.123456789, reader.ReadDouble(null));
                Assert.Equal(1e9, reader.ReadDouble(null));
                Assert.Equal(1.1e9, reader.ReadDouble(null));
                Assert.Equal(2.123456f, reader.ReadFloat(STFReader.UNITS.None, null));
                Assert.Equal(2e9, reader.ReadFloat(STFReader.UNITS.None, null));
                Assert.Equal(2.1e9f, reader.ReadFloat(STFReader.UNITS.None, null));
                Assert.Equal((uint)0xABCDEF, reader.ReadHex(null));
                Assert.Equal(123456, reader.ReadInt(null));
                Assert.Equal(-123456, reader.ReadInt(null));
                Assert.Equal(234567U, reader.ReadUInt(null));
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
            }
        }

        [Fact]
        public static void StringFormats()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("Item1 \"Item2\" \"Item\" + \"3\" String1 \"String2\" \"String\" + \"3\"")), "", Encoding.Unicode, false))
            {
                Assert.Equal("Item1", reader.ReadItem());
                Assert.Equal("Item2", reader.ReadItem());
                Assert.Equal("Item3", reader.ReadItem());
                Assert.Equal("String1", reader.ReadString());
                Assert.Equal("String2", reader.ReadString());
                Assert.Equal("String3", reader.ReadString());
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
            }
        }

        [Fact]
        public static void BlockNumericFormats()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("(true ignored) (false ignored) (1.123456789 ignored) (1e9 ignored) (1.1e9 ignored) (2.123456 ignored) (2e9 ignored) (2.1e9 ignored) (00ABCDEF ignored) (123456 ignored) (-123456 ignored) (234567 ignored)")), "", Encoding.Unicode, false))
            {
                Assert.True(reader.ReadBoolBlock(false));
                Assert.False(reader.ReadBoolBlock(true));
                Assert.Equal(1.123456789, reader.ReadDoubleBlock(null));
                Assert.Equal(1e9, reader.ReadDoubleBlock(null));
                Assert.Equal(1.1e9, reader.ReadDoubleBlock(null));
                Assert.Equal(2.123456f, reader.ReadFloatBlock(STFReader.UNITS.None, null));
                Assert.Equal(2e9, reader.ReadFloatBlock(STFReader.UNITS.None, null));
                Assert.Equal(2.1e9f, reader.ReadFloatBlock(STFReader.UNITS.None, null));
                Assert.Equal((uint)0xABCDEF, reader.ReadHexBlock(null));
                Assert.Equal(123456, reader.ReadIntBlock(null));
                Assert.Equal(-123456, reader.ReadIntBlock(null));
                Assert.Equal(234567U, reader.ReadUIntBlock(null));
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
            }
        }

        [Fact]
        public static void BlockStringFormats()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("(String1 ignored) (\"String2\" ignored) (\"String\" + \"3\" ignored)")), "", Encoding.Unicode, false))
            {
                Assert.Equal("String1", reader.ReadStringBlock(null));
                Assert.Equal("String2", reader.ReadStringBlock(null));
                Assert.Equal("String3", reader.ReadStringBlock(null));
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
            }
        }

        [Fact]
        public static void BlockVectorFormats()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("(1.1 1.2 ignored) (1.1 1.2 1.3 ignored) (1.1 1.2 1.3 1.4 ignored)")), "", Encoding.Unicode, false))
            {
                Assert.Equal(new Vector2(1.1f, 1.2f), reader.ReadVector2Block(STFReader.UNITS.None, Vector2.Zero));
                Assert.Equal(new Vector3(1.1f, 1.2f, 1.3f), reader.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero));
                Assert.Equal(new Vector4(1.1f, 1.2f, 1.3f, 1.4f), reader.ReadVector4Block(STFReader.UNITS.None, Vector4.Zero));
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal(1, reader.LineNumber);
            }
        }

        [Fact]
        public static void Units()
        {
            AssertWarnings.NotExpected();
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("1.1 1.2 1.3km 1.4 1.5km")), "", Encoding.Unicode, false))
            {
                Assert.Equal(1.10000f, reader.ReadFloat(STFReader.UNITS.None, null), DynamicPrecisionEqualityComparer.Float);
                Assert.Equal(1.20000f, reader.ReadFloat(STFReader.UNITS.Distance, null), DynamicPrecisionEqualityComparer.Float);
                Assert.Equal(1300.00000f, reader.ReadFloat(STFReader.UNITS.Distance, null), DynamicPrecisionEqualityComparer.Float);
                float result4 = 0;
                AssertWarnings.Matching("", () =>
                {
                    result4 = reader.ReadFloat(STFReader.UNITS.Distance | STFReader.UNITS.Compulsory, null);
                });
                Assert.Equal(1.40000f, result4, DynamicPrecisionEqualityComparer.Float);
                Assert.Equal(1500.00000f, reader.ReadFloat(STFReader.UNITS.Distance | STFReader.UNITS.Compulsory, null), DynamicPrecisionEqualityComparer.Float);
                Assert.True(reader.Eof, "STFReader.Eof");
            }
        }

        static void UnitConversionTest(string input, double output, STFReader.UNITS unit)
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes(input)), "", Encoding.Unicode, false))
            {
                Assert.Equal(output, reader.ReadFloat(unit, null), DynamicPrecisionEqualityComparer.Float);
                Assert.True(reader.Eof, "STFReader.Eof");
            }
        }

        /* Base unit categories available in MSTS:
         *     (Constants)   pi
         *     Current       a ka ma
         *     Energy        j nm
         *     Force         n kn lbf
         *     Length        mm cm m km " in ' ft mil
         *     Mass          g kg t tn ton lb lbs
         *     Power         kw hp
         *     Pressure      pascal mbar bar psi
         *     Rotation      deg rad
         *     Temperature   k c f
         *     Time          us ms s min h d
         *     Velocity      kmh mph
         *     Voltage       v kv mv
         *     Volume        l gal
         */

        /* Base units for current available in MSTS:
         *     a         Amp
         *     ka        Kilo-amp
         *     ma        Mega-amp
         */
        [Fact]
        public static void UnitConversionBaseMstsCurrent()
        {
            UnitConversionTest("1.2a", 1.2, STFReader.UNITS.Current);
            // TODO not implemented yet: UnitConversionTest("1.2ka", 1.2 * 1000, STFReader.UNITS.Current);
            // TODO not implemented yet: UnitConversionTest("1.2ma", 1.2 * 1000 * 1000, STFReader.UNITS.Current);
        }

        /* Base units for energy available in MSTS:
         *     j         Joule
         *     nm        Newton meter
         */
        [Fact]
        public static void UnitConversionBaseMstsEnergy()
        {
            // TODO not implemented yet: UnitConversionTest("1.2j", 1.2, STFReader.UNITS.Energy);
            // TODO not implemented yet: UnitConversionTest("1.2nm", 1.2, STFReader.UNITS.Energy);
        }

        /* Base units for force available in MSTS:
         *     n         Newton
         *     kn        Kilo-newton
         *     lbf       Pound-force
         */
        [Fact]
        public static void UnitConversionBaseMstsForce()
        {
            UnitConversionTest("1.2n", 1.2, STFReader.UNITS.Force);
            UnitConversionTest("1.2kn", 1.2 * 1000, STFReader.UNITS.Force);
            UnitConversionTest("1.2lbf", 1.2 * PoundForceToNewton, STFReader.UNITS.Force);
        }

        /* Base units for length available in MSTS:
         *     mm        Millimeter
         *     cm        Centimeter
         *     m         Meter
         *     km        Kilometer
         *     "         Inch
         *     in        Inch
         *     '         Foot
         *     ft        Foot
         *     mil       Mile
         */
        [Fact]
        public static void UnitConversionBaseMstsLength()
        {
            UnitConversionTest("1.2mm", 1.2 / 1000, STFReader.UNITS.Distance);
            UnitConversionTest("1.2cm", 1.2 / 100, STFReader.UNITS.Distance);
            UnitConversionTest("1.2m", 1.2, STFReader.UNITS.Distance);
            UnitConversionTest("1.2km", 1.2 * 1000, STFReader.UNITS.Distance);
            // TODO not implemented yet: UnitConversionTest("1.2\"", 1.2 * InchToMetre, STFReader.UNITS.Distance);
            UnitConversionTest("1.2in", 1.2 * InchToMetre, STFReader.UNITS.Distance);
            // TODO not implemented yet: UnitConversionTest("1.2\'", 1.2 * FeetToMetre, STFReader.UNITS.Distance);
            UnitConversionTest("1.2ft", 1.2 * FeetToMetre, STFReader.UNITS.Distance);
            // TODO not implemented yet: UnitConversionTest("1.2mil", 1.2 * MileToMetre, STFReader.UNITS.Distance);
        }

        /* Base units for mass available in MSTS:
         *     g         Gram
         *     kg        Kilogram
         *     t         Tonne
         *     tn        Short/US ton
         *     ton       Long/UK ton
         *     lb        Pound
         *     lbs       Pound
         */
        [Fact]
        public static void UnitConversionBaseMstsMass()
        {
            // TODO not implemented yet: UnitConversionTest("1.2g", 1.2 / 1000, STFReader.UNITS.Mass);
            UnitConversionTest("1.2kg", 1.2, STFReader.UNITS.Mass);
            UnitConversionTest("1.2t", 1.2 * TonneToKG, STFReader.UNITS.Mass);
            // TODO not implemented yet: UnitConversionTest("1.2tn", 1.2 * TonShortToKG, STFReader.UNITS.Mass);
            // TODO not implemented yet: UnitConversionTest("1.2ton", 1.2 * TonLongToKG, STFReader.UNITS.Mass);
            UnitConversionTest("1.2lb", 1.2 * PoundToKG, STFReader.UNITS.Mass);
            // TODO not implemented yet: UnitConversionTest("1.2lbs", 1.2 * PoundToKG, STFReader.UNITS.Mass);
        }

        /* Base units for power available in MSTS:
         *     kw        Kilowatt
         *     hp        Horsepower
         */
        [Fact]
        public static void UnitConversionBaseMstsPower()
        {
            UnitConversionTest("1.2kw", 1.2 * 1000, STFReader.UNITS.Power);
            UnitConversionTest("1.2hp", 1.2 * HorsepowerToWatt, STFReader.UNITS.Power);
        }

        /* Base units for pressure available in MSTS:
         *     pascal    Pascal
         *     mbar      Millibar
         *     bar       Bar
         *     psi       psi
         */
        [Fact]
        public static void UnitConversionBaseMstsPressure()
        {
            // TODO not implemented yet: UnitConversionTest("1.2pascal", 1.2, STFReader.UNITS.PressureDefaultInHg);
            // TODO not implemented yet: UnitConversionTest("1.2pascal", 1.2, STFReader.UNITS.PressureDefaultPSI);
            // TODO not implemented yet: UnitConversionTest("1.2mbar", 1.2 / 1000 * BarToPascal, STFReader.UNITS.PressureDefaultInHg);
            // TODO not implemented yet: UnitConversionTest("1.2mbar", 1.2 / 1000 * BarToPascal, STFReader.UNITS.PressureDefaultPSI);
            // TODO not using SI units: UnitConversionTest("1.2bar", 1.2 * BarToPascal, STFReader.UNITS.PressureDefaultInHg);
            // TODO not using SI units: UnitConversionTest("1.2bar", 1.2 * BarToPascal, STFReader.UNITS.PressureDefaultPSI);
            // TODO not using SI units: UnitConversionTest("1.2psi", 1.2 * PSIToPascal, STFReader.UNITS.PressureDefaultInHg);
            // TODO not using SI units: UnitConversionTest("1.2psi", 1.2 * PSIToPascal, STFReader.UNITS.PressureDefaultPSI);
        }

        /* Base units for rotation available in MSTS:
         *     deg       Degree
         *     rad       Radian
         */
        [Fact]
        public static void UnitConversionBaseMstsRotation()
        {
            // TODO not implemented yet: UnitConversionTest("1.2deg", 1.2, STFReader.UNITS.Rotation);
            // TODO not implemented yet: UnitConversionTest("1.2rad", 1.2 * Math.PI / 180, STFReader.UNITS.Rotation);
        }

        /* Base units for temperature available in MSTS:
         *     k         Kelvin
         *     c         Celsius
         *     f         Fahrenheit
         */
        [Fact]
        public static void UnitConversionBaseMstsTemperature()
        {
            // TODO not implemented yet: UnitConversionTest("1.2k", 1.2, STFReader.UNITS.Temperature);
            // TODO not implemented yet: UnitConversionTest("1.2c", 1.2 + CelsiusToKelvin, STFReader.UNITS.Temperature);
            // TODO not implemented yet: UnitConversionTest("1.2f", (1.2 + FahrenheitToKelvinA) * FahrenheitToKelvinB, STFReader.UNITS.Temperature);
        }

        /* Base units for time available in MSTS:
         *     us        Microsecond
         *     ms        Millisecond
         *     s         Second
         *     min       Minute
         *     h         Hour
         *     d         Day
         */
        [Fact]
        public static void UnitConversionBaseMstsTime()
        {
            // TODO not implemented yet: UnitConversionTest("1.2us", 1.2 / 1000000, STFReader.UNITS.Time);
            // TODO not implemented yet: UnitConversionTest("1.2us", 1.2 / 1000000, STFReader.UNITS.TimeDefaultM);
            // TODO not implemented yet: UnitConversionTest("1.2us", 1.2 / 1000000, STFReader.UNITS.TimeDefaultH);
            // TODO not implemented yet: UnitConversionTest("1.2ms", 1.2 / 1000, STFReader.UNITS.Time);
            // TODO not implemented yet: UnitConversionTest("1.2ms", 1.2 / 1000, STFReader.UNITS.TimeDefaultM);
            // TODO not implemented yet: UnitConversionTest("1.2ms", 1.2 / 1000, STFReader.UNITS.TimeDefaultH);
            UnitConversionTest("1.2s", 1.2, STFReader.UNITS.Time);
            UnitConversionTest("1.2s", 1.2, STFReader.UNITS.TimeDefaultM);
            UnitConversionTest("1.2s", 1.2, STFReader.UNITS.TimeDefaultH);
            // TODO not implemented yet: UnitConversionTest("1.2min", 1.2 * MinuteToSecond, STFReader.UNITS.Time);
            // TODO not implemented yet: UnitConversionTest("1.2min", 1.2 * MinuteToSecond, STFReader.UNITS.TimeDefaultM);
            // TODO not implemented yet: UnitConversionTest("1.2min", 1.2 * MinuteToSecond, STFReader.UNITS.TimeDefaultH);
            UnitConversionTest("1.2h", 1.2 * HourToSecond, STFReader.UNITS.Time);
            UnitConversionTest("1.2h", 1.2 * HourToSecond, STFReader.UNITS.TimeDefaultM);
            UnitConversionTest("1.2h", 1.2 * HourToSecond, STFReader.UNITS.TimeDefaultH);
            // TODO not implemented yet: UnitConversionTest("1.2d", 1.2 * DayToSecond, STFReader.UNITS.Time);
            // TODO not implemented yet: UnitConversionTest("1.2d", 1.2 * DayToSecond, STFReader.UNITS.TimeDefaultM);
            // TODO not implemented yet: UnitConversionTest("1.2d", 1.2 * DayToSecond, STFReader.UNITS.TimeDefaultH);
        }

        /* Base units for velocity available in MSTS:
         *     kmh       Kilometers/hour
         *     mph       Miles/hour
         */
        [Fact]
        public static void UnitConversionBaseMstsVelocity()
        {
            UnitConversionTest("1.2kmh", 1.2 * KilometrePerHourToMetrePerSecond, STFReader.UNITS.Speed);
            UnitConversionTest("1.2kmh", 1.2 * KilometrePerHourToMetrePerSecond, STFReader.UNITS.SpeedDefaultMPH);
            UnitConversionTest("1.2mph", 1.2 * MilePerHourToMetrePerSecond, STFReader.UNITS.Speed);
            UnitConversionTest("1.2mph", 1.2 * MilePerHourToMetrePerSecond, STFReader.UNITS.SpeedDefaultMPH);
        }

        /* Base units for voltage available in MSTS:
         *     v         Volt
         *     kv        Kilovolt
         *     mv        Megavolt
         */
        [Fact]
        public static void UnitConversionBaseMstsVoltage()
        {
            UnitConversionTest("1.2v", 1.2, STFReader.UNITS.Voltage);
            UnitConversionTest("1.2kv", 1.2 * 1000, STFReader.UNITS.Voltage);
            // TODO not implemented yet: UnitConversionTest("1.2mv", 1.2 * 1000 * 1000, STFReader.UNITS.Voltage);
        }

        /* Base units for volume available in MSTS:
         *     l         Liter
         *     gal       Gallon (US)
         */
        [Fact]
        public static void UnitConversionBaseMstsVolume()
        {
            // TODO not using SI units: UnitConversionTest("1.2l", 1.2 * LitreToCubicMetre, STFReader.UNITS.Volume);
            // TODO not using SI units: UnitConversionTest("1.2l", 1.2 * LitreToCubicMetre, STFReader.UNITS.VolumeDefaultFT3);
            // TODO not using SI units: UnitConversionTest("1.2gal", 1.2 * GallonUSToCubicMetre, STFReader.UNITS.Volume);
            // TODO not using SI units: UnitConversionTest("1.2gal", 1.2 * GallonUSToCubicMetre, STFReader.UNITS.VolumeDefaultFT3);
        }

        [Fact]
        public static void UnitConversionDerivedMstsArea()
        {
            // TODO not implemented yet: UnitConversionTest("1.2mm^2", 1.2 / 1000 / 1000, STFReader.UNITS.AreaDefaultFT2);
            // TODO not implemented yet: UnitConversionTest("1.2cm^2", 1.2 / 100 / 100, STFReader.UNITS.AreaDefaultFT2);
            UnitConversionTest("1.2m^2", 1.2, STFReader.UNITS.AreaDefaultFT2);
            // TODO not implemented yet: UnitConversionTest("1.2km^2", 1.2 * 1000 * 1000, STFReader.UNITS.AreaDefaultFT2);
            // TODO not implemented yet: UnitConversionTest("1.2\"^2", 1.2 * InchToMetre * InchToMetre, STFReader.UNITS.AreaDefaultFT2);
            // TODO not implemented yet: UnitConversionTest("1.2in^2", 1.2 * InchToMetre * InchToMetre, STFReader.UNITS.AreaDefaultFT2);
            // TODO not implemented yet: UnitConversionTest("1.2\'^2", 1.2 * FeetToMetre * FeetToMetre, STFReader.UNITS.AreaDefaultFT2);
            UnitConversionTest("1.2ft^2", 1.2 * FeetToMetre * FeetToMetre, STFReader.UNITS.AreaDefaultFT2);
            // TODO not implemented yet: UnitConversionTest("1.2mil^2", 1.2 * MileToMetre * MileToMetre, STFReader.UNITS.AreaDefaultFT2);
        }

        [Fact]
        public static void UnitConversionDerivedMstsDamping()
        {
            UnitConversionTest("1.2n/m/s", 1.2, STFReader.UNITS.Resistance);
        }

        [Fact]
        public static void UnitConversionDerivedMstsMassRate()
        {
            // TODO not using SI units: UnitConversionTest("1.2lb/h", 1.2 * PoundToKG / HourToSecond, STFReader.UNITS.MassRateDefaultLBpH);
        }

        [Fact]
        public static void UnitConversionDerivedMstsStiffness()
        {
            UnitConversionTest("1.2n/m", 1.2, STFReader.UNITS.Stiffness);
        }

        [Fact]
        public static void UnitConversionDerivedMstsVelocity()
        {
            UnitConversionTest("1.2m/s", 1.2, STFReader.UNITS.Speed);
            UnitConversionTest("1.2m/s", 1.2, STFReader.UNITS.SpeedDefaultMPH);
            UnitConversionTest("1.2km/h", 1.2 * 1000 / HourToSecond, STFReader.UNITS.Speed);
            UnitConversionTest("1.2km/h", 1.2 * 1000 / HourToSecond, STFReader.UNITS.SpeedDefaultMPH);
        }

        [Fact]
        public static void UnitConversionDerivedMstsVolume()
        {
            // TODO not implemented yet: UnitConversionTest("1.2mm^3", 1.2 * 1000 / 1000 / 1000, STFReader.UNITS.Volume);
            // TODO not implemented yet: UnitConversionTest("1.2cm^3", 1.2 / 100 / 100 / 100, STFReader.UNITS.Volume);
            // TODO not using SI units: UnitConversionTest("1.2m^3", 1.2, STFReader.UNITS.Volume);
            // TODO not implemented yet: UnitConversionTest("1.2km^3", 1.2 * 1000 * 1000 * 1000, STFReader.UNITS.Volume);
            // TODO not implemented yet: UnitConversionTest("1.2\"^3", 1.2 * InchToMetre * InchToMetre * InchToMetre, STFReader.UNITS.Volume);
            // TODO not using SI units: UnitConversionTest("1.2in^3", 1.2 * InchToMetre * InchToMetre * InchToMetre, STFReader.UNITS.Volume);
            // TODO not implemented yet: UnitConversionTest("1.2\'^3", 1.2 * FeetToMetre * FeetToMetre * FeetToMetre, STFReader.UNITS.Volume);
            // TODO not using SI units: UnitConversionTest("1.2ft^3", 1.2 * FeetToMetre * FeetToMetre * FeetToMetre, STFReader.UNITS.Volume);
            // TODO not implemented yet: UnitConversionTest("1.2mil^3", 1.2 * MileToMetre * MileToMetre * MileToMetre, STFReader.UNITS.Volume);
        }

        /* Default unit for pressure in MSTS is UNKNOWN.
         */
        [Fact]
        public static void UnitConversionDefaultMstsPressure()
        {
            //UnitConversionTest("1.2", UNKNOWN, STFReader.UNITS.Pressure);
            UnitConversionTest("1.2", 1.2 * InchOfMercuryToPascal * PascalToPSI, STFReader.UNITS.PressureDefaultInHg);
            UnitConversionTest("1.2", 1.2, STFReader.UNITS.PressureDefaultPSI);
        }

        /* Default unit for time in MSTS is seconds.
         */
        [Fact]
        public static void UnitConversionDefaultMstsTime()
        {
            UnitConversionTest("1.2", 1.2, STFReader.UNITS.Time);
            UnitConversionTest("1.2", 1.2 * MinuteToSecond, STFReader.UNITS.TimeDefaultM);
            UnitConversionTest("1.2", 1.2 * HourToSecond, STFReader.UNITS.TimeDefaultH);
        }

        /* Default unit for velocity in MSTS is UNKNOWN.
         */
        [Fact]
        public static void UnitConversionDefaultMstsVelocity()
        {
            //UnitConversionTest("1.2", UNKNOWN, STFReader.UNITS.Speed);
            UnitConversionTest("1.2", 1.2 * MilePerHourToMetrePerSecond, STFReader.UNITS.SpeedDefaultMPH);
        }

        /* Default unit for volume in MSTS is UNKNOWN.
         */
        [Fact]
        public static void UnitConversionDefaultMstsVolume()
        {
            //UnitConversionTest("1.2", UNKNOWN, STFReader.UNITS.Volume);
            // TODO not using SI units: UnitConversionTest("1.2", 1.2 * FeetToMetre * FeetToMetre * FeetToMetre, STFReader.UNITS.VolumeDefaultFT3);
        }

        /* The following units are currently accepted by Open Rails but have no meaning in MSTS:
         *     amps
         *     btu/lb
         *     degc
         *     degf
         *     gals
         *     g-uk
         *     g-us
         *     hz
         *     inhg
         *     inhg/s
         *     kj/kg
         *     kmph
         *     kpa
         *     kpa/s
         *     kph
         *     ns/m
         *     rpm
         *     rps
         *     t-uk
         *     t-us
         *     w
         */

        [Fact]
        public static void ParentheticalCommentsCanGoAnywhere()
        {
            // Also testing for bugs 1274713, 1221696, 1377393
            var part1 =
                "Wagon(\n" +
                "    Lights(\n" +
                "        Light( 1 )\n" +
                "";
            var part2 =
                "        Light( 2 )\n" +
                "";
            var part3 =
                "    )\n" +
                "    Sound( test.sms )\n" +
                ")";
            var middles = new[] {
                "        #(Comment)\n",
                "        # (Comment)\n",
                "        Skip( ** comment ** ) \n",
                "        Skip ( ** comment ** ) \n"
            };
            foreach (var middle in middles)
                ParenthicalCommentSingle(part1 + middle + part2 + part3);
            foreach (var middle in middles)
                ParenthicalCommentSingle(part1 + part2 + middle + part3);
        }

        static void ParenthicalCommentSingle(string inputString)
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes(inputString)), "", Encoding.Unicode, true))
            {
                reader.ReadItem();
                Assert.Equal("wagon", reader.Tree.ToLower());
                reader.MustMatch("(");

                reader.ReadItem();
                Assert.Equal("wagon(lights", reader.Tree.ToLower());
                reader.MustMatch("(");

                reader.ReadItem();
                Assert.Equal("wagon(lights(light", reader.Tree.ToLower());
                reader.MustMatch("(");
                Assert.Equal(1, reader.ReadInt(null));
                reader.SkipRestOfBlock();
                Assert.Equal("wagon(lights()", reader.Tree.ToLower());

                reader.ReadItem();
                Assert.Equal("wagon(lights(light", reader.Tree.ToLower());
                reader.MustMatch("(");
                Assert.Equal(2, reader.ReadInt(null));
                reader.SkipRestOfBlock();
                Assert.Equal("wagon(lights()", reader.Tree.ToLower());

                if (reader.ReadItem() == STFReader.EndBlockCommentSentinel)
                    reader.ReadItem();
                reader.ReadItem();
                Assert.Equal("wagon(sound", reader.Tree.ToLower());
                Assert.Equal("test.sms", reader.ReadStringBlock(""));
                reader.SkipRestOfBlock();

                Assert.True(reader.Eof, "STFReader.Eof");
            }
        }
    }
}
#endregion

#region Unit tests
namespace Tests.Orts.Parsers.Msts.StfException
{
    #region StfExceptionTests
    /// <summary>
    /// Test the exceptions for Stf itself
    /// </summary>
    public class Should
    {
        [Fact]
        public static void BeConstructableFromStfReader()
        {
            var reader = Tests.Orts.Parsers.Msts.StfReader.Create.Reader("sometoken");
            new STFException(reader, "some message");
        }

#if NEW_READER
        /// <summary>
        /// Test constructors of the exception class
        /// </summary>
        [Fact]
        public static void BeConstructable()
        {
            Assert.DoesNotThrow(() => new STFException("filename", 1, "some message"));
        }

        /// <summary>
        /// Test that we can correctly assert a throw of an exception
        /// </summary>
        [Fact]
        public static void BeThrowable()
        {
            string filename = "somefile";
            int lineNumber = 2;
            string message = "some message";
            Tests.Msts.Parsers.StfReader.AssertStfException.Throws(() => { throw new STFException(filename, lineNumber, message); }, message);
        }
#endif
    }
    #endregion

}
namespace Tests.Orts.Parsers.Msts.StfReader
{
    // NEW_READER compilation flag is set for those tests that can be performed (compiled) only for the new STFReader, 
    // but not on the old reader. The new reader should also pass all tests that compile on the old reader.
    // This means in this file NEW_READER flag adds a number of tests, but it should also work if the flag is not set.

    // General note on 'using new STFreader'
    // In production code we want to use something like
    // using (var reader = new STFReader()) {
    //    ...
    // } 
    // This makes sure the reader is disposed of correctly.
    // During testing, however, this has an unwanted side-effect. If a unit tests fails, and not everything has been read
    // the rest of the code in the test is not executed. But the reader.Dispose will be called anyway at the end of the using block.
    // reader.Dispose, however, will give a warning when it is called when the reader is not yet at the end of the file. This warning will 
    // subsequently be catched, and give an failed assert. It is this failed assert that turns up in the test runner then, and not the initial failed assert
    // This makes that the cause of the fail is not clear from the output window.
    // Obviously, it is possible to refactor the tests such that the the asserts are either done after the dispose, 
    // or the asserts are done when all reading is done anyway (possibly by really having only one assert). 
    // Since most unit tests do not actually open a file, this is no big issue.

    #region General
    public class Should
    {
        /// <summary>
        /// Test constructor
        /// </summary>
        [Fact]
        public static void BeConstructableFromStream()
        {
            AssertWarnings.NotExpected();
            new STFReader(new MemoryStream(Encoding.ASCII.GetBytes("")), "emptyFile.stf", Encoding.ASCII, true);
        }

        /// <summary>
        /// Test that Dispose is implemented (but not that it is fully functional)
        /// </summary>
        [Fact]
        public static void BeDisposable()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("");
            reader.Dispose();
        }

        [Fact]
        public static void OnDisposeBeforeEOFWarn()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("lasttoken");
            AssertWarnings.Matching("Expected.*end", () => reader.Dispose());
        }

        [Fact]
        public static void ThrowInConstructorOnMissingFile()
        {
            Assert.Throws<DirectoryNotFoundException>(() => new STFReader("somenonexistingfile", false));
        }

        [Fact]
        public static void OnStreamConstructorHasNullSimisSignature()
        {
            AssertWarnings.NotExpected();
            string firstToken = "firsttoken";
            var reader = Create.Reader(firstToken);
            Assert.Null(reader.SimisSignature);
        }

#if NEW_READER
        [Fact]
        public static void BeConstructableFromFilename()
        {
            AssertWarnings.Activate();
            var store = new StreamReaderStore();
            string name = "someName";
            string inputToken = "someToken";
            store.AddReaderFromMemory(name, inputToken);

            STFReader.NextStreamReaderFactory = store;
            var reader = new STFReader(name, true);
            string actualToken = reader.ReadItem();
            Assert.Equal(inputToken, actualToken);
            Assert.Equal(actualToken, reader.Tree); // to be sure the constructor initializes everything correctly            
        }

        [Fact]
        public static void OnFileConstructorHasNullSimisSignatureByDefault()
        {
            AssertWarnings.Activate();

            var store = new StreamReaderStore();
            store.ShouldReadSimisSignature = false;
            string name = "someName";
            string firstToken = "firsttoken";
            store.AddReaderFromMemory(name, firstToken);

            STFReader.NextStreamReaderFactory = store;
            var reader = new STFReader(name, false);

            Assert.Equal(firstToken, reader.ReadItem());
            Assert.Equal(1, reader.LineNumber);
            Assert.Equal(null, reader.SimisSignature);
        }

        [Fact]
        public static void OnFileConstructorHasSimisSignature()
        {
            AssertWarnings.Activate();

            var store = new StreamReaderStore();
            store.ShouldReadSimisSignature = true;
            string name = "someName";
            string signature = "SimisSignature";
            string firstToken = "firsttoken";
            store.AddReaderFromMemory(name, signature + "\n" + firstToken);

            STFReader.NextStreamReaderFactory = store;
            var reader = new STFReader(name, false);

            Assert.Equal(firstToken, reader.ReadItem());
            Assert.Equal(2, reader.LineNumber);
            Assert.Equal(signature, reader.SimisSignature);
        }
#endif
    }
    #endregion

    #region Tokenizer
    public class OnReadingItemShould
    {
        [Fact]
        public static void ReadSingleItem()
        {
            AssertWarnings.NotExpected();
            string item = "sometoken";
            var reader = Create.Reader(item);
            Assert.Equal(item, reader.ReadItem());
        }

        [Fact]
        public static void ReadSingleItems()
        {
            AssertWarnings.NotExpected();
            var singleItems = new string[] { "a", "b", "c", "(", ")", "aa" };
            foreach (string item in singleItems)
            {
                var reader = Create.Reader(item);
                Assert.Equal(item, reader.ReadItem());
            }
        }

        [Fact]
        public static void LetWhiteSpaceSeparateTokens()
        {
            AssertWarnings.NotExpected();
            var tokenTesters = new List<TokenTester>();
            tokenTesters.Add(new TokenTester("a b", new string[] { "a", "b" }));
            tokenTesters.Add(new TokenTester("a   b", new string[] { "a", "b" }));
            tokenTesters.Add(new TokenTester("a\nb", new string[] { "a", "b" }));
            tokenTesters.Add(new TokenTester("a \n\t b", new string[] { "a", "b" }));
            tokenTesters.Add(new TokenTester("aa bb", new string[] { "aa", "bb" }));
            tokenTesters.Add(new TokenTester("aa b\nc", new string[] { "aa", "b", "c" }));

            foreach (var tokenTester in tokenTesters)
            {
                var reader = Create.Reader(tokenTester.inputString);
                foreach (string expectedToken in tokenTester.expectedTokens)
                {
                    Assert.Equal(expectedToken, reader.ReadItem());
                }
            }
        }

        [Fact]
        public static void RecognizeSpecialChars()
        {
            AssertWarnings.NotExpected();
            List<TokenTester> tokenTesters = new List<TokenTester>();
            tokenTesters.Add(new TokenTester("(a", new string[] { "(", "a" }));
            tokenTesters.Add(new TokenTester(")a", new string[] { ")", "a" }));
            tokenTesters.Add(new TokenTester("a(", new string[] { "a", "(" }));
            tokenTesters.Add(new TokenTester("aa ( (", new string[] { "aa", "(", "(" }));
            tokenTesters.Add(new TokenTester("(\ncc\n(", new string[] { "(", "cc", "(" }));

            foreach (var tokenTester in tokenTesters)
            {
                var reader = Create.Reader(tokenTester.inputString);
                foreach (string expectedToken in tokenTester.expectedTokens)
                {
                    Assert.Equal(expectedToken, reader.ReadItem());
                }
            }
        }

        [Fact]
        public static void RecognizeLiteralStrings()
        {
            AssertWarnings.NotExpected();
            List<TokenTester> tokenTesters = new List<TokenTester>();
            tokenTesters.Add(new TokenTester("\"a\"", new string[] { "a" }));
            tokenTesters.Add(new TokenTester("\"aa\"", new string[] { "aa" }));
            tokenTesters.Add(new TokenTester("\"a a\"", new string[] { "a a" }));
            tokenTesters.Add(new TokenTester("\"a a\"b", new string[] { "a a", "b" }));
            tokenTesters.Add(new TokenTester("\"a a\" b", new string[] { "a a", "b" }));
            tokenTesters.Add(new TokenTester("\"a a\"\nb", new string[] { "a a", "b" }));
            tokenTesters.Add(new TokenTester("\"a\na\"\nb", new string[] { "a\na", "b" }));
            tokenTesters.Add(new TokenTester("\"a\ta\"\nb", new string[] { "a\ta", "b" }));
            tokenTesters.Add(new TokenTester("\"\\\"\"b", new string[] { "\"", "b" }));

            foreach (var tokenTester in tokenTesters)
            {
                var reader = Create.Reader(tokenTester.inputString);
                foreach (string expectedToken in tokenTester.expectedTokens)
                {
                    Assert.Equal(expectedToken, reader.ReadItem());
                }
            }
        }

        [Fact]
        public static void RecognizeEscapeCharInLiteralStrings()
        {
            AssertWarnings.NotExpected();
            List<TokenTester> tokenTesters = new List<TokenTester>();
            tokenTesters.Add(new TokenTester(@"""a\na"" b", new string[] { "a\na", "b" }));
            tokenTesters.Add(new TokenTester(@"""c\tc"" d", new string[] { "c\tc", "d" }));

            foreach (var tokenTester in tokenTesters)
            {
                var reader = Create.Reader(tokenTester.inputString);
                foreach (string expectedToken in tokenTester.expectedTokens)
                {
                    Assert.Equal(expectedToken, reader.ReadItem());
                }
            }
        }

        [Fact]
        public static void OnIncompleteLiteralStringAtEOFWarnAndGiveResult()
        {
            AssertWarnings.NotExpected();
            string tokenToBeRead = "sometoken";
            string inputString = "\"" + tokenToBeRead;
            string returnedItem = "needadefault";
            AssertWarnings.Matching("unexpected.*EOF.*started.*double-quote", () =>
            {
                var reader = Create.Reader(inputString);
                returnedItem = reader.ReadItem();
            });
            Assert.Equal(tokenToBeRead, returnedItem);
        }

        [Fact]
        public static void AllowTrailingDoubleQuote()
        {   // This fixes bug 1197917, even though it is a workaround for bad files
            AssertWarnings.NotExpected();
            string tokenToBeRead = "sometoken";
            string followingToken = "following";
            string inputString = String.Format(" {0}\" {1}", tokenToBeRead, followingToken);
            var reader = Create.Reader(inputString);
            Assert.Equal(tokenToBeRead, reader.ReadItem());
            Assert.Equal(followingToken, reader.ReadItem());
        }

        [Fact]
        public static void AtEOFKeepBeingAtEOF()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("lasttoken");
            reader.ReadItem();
            reader.ReadItem();
            Assert.True(reader.Eof);
        }

        [Fact]
        public static void AtEOFKeepReturningEmptyString()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("lasttoken");
            reader.ReadItem();
            Assert.Equal(String.Empty, reader.ReadItem());
            Assert.Equal(String.Empty, reader.ReadItem());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Old STFReader has a different way of reading lineNumbers and will fail here</remarks>
        [Fact]
        public static void StoreSourceLineNumberOfLastReadToken()
        {
            //AssertWarnings.Activate();
            var tokenTesters = new TokenTester[]
            {
                new TokenTester("a b", new int[] { 1, 1 }),
                new TokenTester("a \nb", new int[] { 1, 2 }),
                new TokenTester("a \nb \nc", new int[] { 1, 2, 3 }),
                new TokenTester("a b \n\nc", new int[] { 1, 1, 3 }),
                new TokenTester("a(b(\nc)\nc)", new int[] { 1, 1, 1, 1, 2, 2, 3, 3 })
            };

            foreach (var tokenTester in tokenTesters)
            {
                var reader = Create.Reader(tokenTester.inputString);
                foreach (int expectedLineNumber in tokenTester.expectedLineNumbers)
                {
                    reader.ReadItem();
                    Assert.Equal(expectedLineNumber, reader.LineNumber);
                }
            }
        }

        [Fact]
        public static void StoreLastSourceLineNumber()
        {
            AssertWarnings.NotExpected();
            List<TokenTester> tokenTesters = new List<TokenTester>();
            tokenTesters.Add(new TokenTester("a b", new int[2] { 1, 1 }));
            tokenTesters.Add(new TokenTester("a\nb", new int[2] { 1, 2 }));
            tokenTesters.Add(new TokenTester("a\nb\nc", new int[3] { 1, 2, 3 }));
            tokenTesters.Add(new TokenTester("a b\n\nc", new int[3] { 1, 1, 3 }));
            tokenTesters.Add(new TokenTester("a(b(\nc)\nc)", new int[8] { 1, 1, 1, 1, 2, 2, 3, 3 }));

            foreach (var tokenTester in tokenTesters)
            {
                var reader = Create.Reader(tokenTester.inputString);
                foreach (int expectedLineNumber in tokenTester.expectedLineNumbers)
                {
                    reader.ReadItem();
                }
                int lastLineNumber = tokenTester.expectedLineNumbers[tokenTester.expectedLineNumbers.Count() - 1];
                Assert.Equal(lastLineNumber, reader.LineNumber);
                reader.ReadItem();
                Assert.Equal(lastLineNumber, reader.LineNumber);
            }
        }

        [Fact]
        public static void StoreFileName()
        {
            AssertWarnings.NotExpected();
            string[] fileNames = new string[] { "test1", "otherfile.stf" };
            string someThreeItemInput = "a b c";
            foreach (string fileName in fileNames)
            {
                var reader = Create.Reader(someThreeItemInput, fileName);
                reader.ReadItem();
                Assert.Equal(fileName, reader.FileName);
                reader.ReadItem();
                Assert.Equal(fileName, reader.FileName);
                reader.ReadItem();
                Assert.Equal(fileName, reader.FileName);
            }
        }

    }

    #region Concatenation
    public class OnReadingItemWithPlusShould
    {
        [Fact]
        public static void ConcatenateTwoLiteralTokens()
        {
            AssertWarnings.NotExpected();
            string inputString = "\"a\" + \"b\"";
            var reader = Create.Reader(inputString);
            Assert.Equal("ab", reader.ReadItem());
        }

        [Fact]
        public static void NotConcatenateAfterNormalToken()
        {
            AssertWarnings.NotExpected();
            string inputString = "a + b";
            var reader = Create.Reader(inputString);
            Assert.Equal("a", reader.ReadItem());
        }

        [Fact]
        public static void ConcatenateThreeLiteralTokens()
        {
            AssertWarnings.NotExpected();
            string inputString = "\"a\" + \"b\" + \"c\"";
            var reader = Create.Reader(inputString);
            Assert.Equal("abc", reader.ReadItem());
        }

        [Fact]
        public static void WarnOnNormalTokenAfterConcatenation()
        {
            AssertWarnings.NotExpected();
            string result = String.Empty;
            string inputString = "\"a\" + b";

            AssertWarnings.Matching("started.*double.*quote.*next.*must", () =>
            {
                var reader = Create.Reader(inputString);
                result = reader.ReadItem();
            });
            Assert.Equal("a", result);
        }
    }
    #endregion
    #endregion

    #region Tree
    public class OnNotUsingTreeShould
    {
        [Fact]
        public static void OnCallingTreeThrowSomething()
        {
            AssertWarnings.Expected(); // there might be a debug.assert
            var reader = Create.Reader("wagon(Lights");
            reader.ReadItem();
            Exception exception = Record.Exception(() => { var dummy = reader.Tree; });
            Assert.NotNull(exception);
        }
    }

    public class OnUsingTreeShould
    {
        [Fact]
        public static void BuildTreeString()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("wagon(Lights)engine", true);

            reader.ReadItem();
            Assert.Equal("wagon", reader.Tree.ToLower());
            reader.ReadItem();
            reader.ReadItem();
            Assert.Equal("wagon(lights", reader.Tree.ToLower());
            reader.ReadItem();
            reader.ReadItem();
            Assert.Equal("engine", reader.Tree.ToLower());
        }

        [Fact]
        public static void ContainClosingBracket()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("wagon(Lights())engine", true);

            reader.ReadItem(); // 'wagon'
            reader.ReadItem(); // '('
            reader.ReadItem(); // 'Lights'
            reader.ReadItem(); // '('
            reader.ReadItem(); // ')'
            Assert.Equal("wagon()", reader.Tree.ToLower());
            reader.ReadItem(); // ')'
            Assert.Equal(")", reader.Tree.ToLower());
            reader.ReadItem(); // ')'
            Assert.Equal("engine", reader.Tree.ToLower());
        }
    }
    #endregion

    #region Block handling
    public class BlockHandlingShould
    {
        [Fact]
        public static void SkipRestOfBlockAtBlockClose()
        {
            AssertWarnings.NotExpected();
            string someTokenAfterBlock = "a";
            var reader = Create.Reader(")" + someTokenAfterBlock);
            reader.SkipRestOfBlock();
            Assert.Equal(someTokenAfterBlock, reader.ReadItem());
        }

        [Fact]
        public static void SkipRestOfBlockBeforeBlockClose()
        {
            AssertWarnings.NotExpected();
            string someTokenAfterBlock = "a";
            var reader = Create.Reader("b)" + someTokenAfterBlock);
            reader.SkipRestOfBlock();
            Assert.Equal(someTokenAfterBlock, reader.ReadItem());
        }

        [Fact]
        public static void SkipRestOfBlockForNestedblocks()
        {
            AssertWarnings.NotExpected();
            string someTokenAfterBlock = "a";
            var reader = Create.Reader("b(c))" + someTokenAfterBlock);
            reader.SkipRestOfBlock();
            Assert.Equal(someTokenAfterBlock, reader.ReadItem());
        }

        [Fact]
        public static void OnInclompleteRestOfBlockJustReturn()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("(b)");
            reader.SkipRestOfBlock();
            Assert.True(reader.Eof);
        }

        [Fact]
        public static void SkipBlock()
        {
            string someTokenAfterBlock = "a";
            var reader = Create.Reader("(c)" + someTokenAfterBlock);
            reader.SkipBlock();
            Assert.Equal(someTokenAfterBlock, reader.ReadItem());
        }

        [Fact]
        public static void SkipNestedBlock()
        {
            string someTokenAfterBlock = "a";
            var reader = Create.Reader("(c(b)d)" + someTokenAfterBlock);
            reader.SkipBlock();
            Assert.Equal(someTokenAfterBlock, reader.ReadItem());
        }

        [Fact]
        public static void OnSkipBlockGettingImmediateCloseWarn()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader(")");
            AssertWarnings.Matching("Found a close.*expected block of data", () => reader.SkipBlock());
            Assert.Equal(")", reader.ReadItem());
        }

        [Fact]
        public static void OnSkipBlockNotStartingWithOpenThrow()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("a");
            AssertStfException.Throws(() => reader.SkipBlock(), "expected an open block");
        }

        [Fact]
        public static void OnIncompleteSkipBlockJustReturn()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("(a");
            reader.SkipBlock();
            Assert.True(reader.Eof);
        }

        [Fact]
        public static void NotBeAtEndOfBlockAfterNormalToken()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("sometoken sometoken2");
            Assert.False(reader.EndOfBlock());
            reader.ReadItem();
            Assert.False(reader.EndOfBlock());
        }

        [Fact]
        public static void BeAtEndOfBlockAtEOF()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader("");
            Assert.True(reader.EndOfBlock());
        }

        [Fact]
        public static void BeAtEndOfBlockAtClose()
        {
            AssertWarnings.NotExpected();
            var reader = Create.Reader(") nexttoken");
            Assert.True(reader.EndOfBlock());
        }

        [Fact]
        public static void AtEndOfBlockConsumeCloseMarker()
        {
            AssertWarnings.NotExpected();
            string followuptoken = "sometoken";
            var reader = Create.Reader(")" + followuptoken + " " + followuptoken); // should be at EOF

            Assert.True(reader.EndOfBlock());
            Assert.Equal(followuptoken, reader.ReadItem());
            Assert.False(reader.EndOfBlock());
        }
    }
    #endregion

    #region MustMatch
    public class OnMustMatchShould
    {
        [Fact]
        public static void MatchSimpleStrings()
        {
            AssertWarnings.NotExpected();
            string matchingToken = "a";
            string someTokenAfterMatch = "b";

            var reader = Create.Reader(matchingToken + " " + someTokenAfterMatch);
            reader.MustMatch(matchingToken);
            Assert.Equal(someTokenAfterMatch, reader.ReadItem());
        }

        [Fact]
        public static void MatchOpenBracket()
        {
            AssertWarnings.NotExpected();
            string matchingToken = "(";
            string someTokenAfterMatch = "b";

            var reader = Create.Reader(matchingToken + someTokenAfterMatch);
            reader.MustMatch(matchingToken);
            Assert.Equal(someTokenAfterMatch, reader.ReadItem());
        }

        [Fact]
        public static void MatchCloseBracket()
        {
            AssertWarnings.NotExpected();
            string matchingToken = ")";
            string someTokenAfterMatch = "b";

            var reader = Create.Reader(matchingToken + someTokenAfterMatch);
            reader.MustMatch(matchingToken);
            Assert.Equal(someTokenAfterMatch, reader.ReadItem());
        }

        [Fact]
        public static void MatchWhileIgnoringCase()
        {
            AssertWarnings.NotExpected();
            string matchingToken = "somecase";
            string matchingTokenOtherCase = "SomeCase";
            string someTokenAfterMatch = "b";

            var reader = Create.Reader(matchingTokenOtherCase + " " + someTokenAfterMatch);
            reader.MustMatch(matchingToken);
            Assert.Equal(someTokenAfterMatch, reader.ReadItem());
        }

        [Fact]
        public static void WarnOnSingleMissingMatch()
        {
            AssertWarnings.NotExpected();
            string tokenToMatch = "a";
            string someotherToken = "b";
            var reader = Create.Reader(someotherToken + " " + tokenToMatch);
            AssertWarnings.Matching("not found.*instead", () => reader.MustMatch(tokenToMatch));
        }

        [Fact]
        public static void ThrowOnDoubleMissingMatch()
        {
            AssertWarnings.Expected();  // we first expect a warning, only after that an error
            string tokenToMatch = "a";
            string someotherToken = "b";
            var reader = Create.Reader(someotherToken + " " + someotherToken);
            AssertStfException.Throws(() => reader.MustMatch(tokenToMatch), "not found.*instead");
        }

        [Fact]
        public static void WarnOnEofDuringMatch()
        {
            AssertWarnings.NotExpected();
            string tokenToMatch = "a";
            var reader = Create.Reader("");
            AssertWarnings.Matching("Unexpected end of file instead", () => reader.MustMatch(tokenToMatch));

        }
    }
    #endregion

    #region Preprocessing comments/skip, include
    #region Comments/skip
    public class PreprocessingShould
    {
        [Fact]
        public static void SkipBlockOnComment()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("comment(a)" + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void SkipBlockOnCommentOtherCase()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("Comment(a)" + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void DontWarnOnMissingBlockAfterComment()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            STFReader reader = Create.Reader("comment a " + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void SkipBlockOnSkip()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("skip(a)" + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void SkipBlockOnSkipOtherCase()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("Skip(a)" + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void DontWarnOnMissingBlockAfterSkip()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            STFReader reader = Create.Reader("skip a " + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void SkipBlockOnTokenStartingWithHash()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("#token(a)" + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void SkipSingleItemOnTokenStartingWithHash()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("#token a " + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void WarnOnEofAfterHashToken()
        {
            AssertWarnings.NotExpected();
            AssertWarnings.Matching("a # marker.*EOF", () =>
            {
                var reader = Create.Reader("#sometoken");
                reader.ReadItem();
            });
        }

        [Fact]
        public static void SkipBlockOnTokenStartingWithUnderscore()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("_token(a)" + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void SkipSingleItemOnTokenStartingWithUnderscore()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("_token a " + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void SkipBlockDisregardsNestedComment()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("comment(a comment( c) skip _underscore #hash)" + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }

        [Fact]
        public static void SkipBlockDisregardsNestedInclude()
        {
            AssertWarnings.NotExpected();
            string someFollowingToken = "b";
            var reader = Create.Reader("comment(a include c )" + someFollowingToken);
            Assert.Equal(someFollowingToken, reader.ReadItem());
        }
    }
    #endregion

    #region Include
#if NEW_READER
    public class OnIncludeShould
    {
        [Fact]
        public static void IncludeNamedStream()
        {
            AssertWarnings.Activate();

            string name = "somename";
            string includedSingleTokenInput = "a";
            var store = new StreamReaderStore();
            store.AddReaderFromMemory(name, includedSingleTokenInput);

            string followingToken = "b";
            string inputString = "include ( " + name + ") " + followingToken;

            var reader = Create.Reader(inputString, store);
            Assert.Equal(includedSingleTokenInput, reader.ReadItem());
            Assert.Equal(followingToken, reader.ReadItem());
        }

        [Fact]
        public static void AllowCapitalizedInclude()
        {
            AssertWarnings.Activate();

            string name = "somename";
            string includedSingleTokenInput = "a";
            var store = new StreamReaderStore();
            store.AddReaderFromMemory(name, includedSingleTokenInput);

            string followingToken = "b";
            string inputString = "Include ( " + name + ") " + followingToken;

            var reader = Create.Reader(inputString, store);
            Assert.Equal(includedSingleTokenInput, reader.ReadItem());
            Assert.Equal(followingToken, reader.ReadItem());
        }

        [Fact]
        public static void AllowNestedIncludes()
        {
            AssertWarnings.Activate();

            string name2 = "name2";
            string name3 = "name3";
            var store = new StreamReaderStore();
            store.AddReaderFromMemory(name3, "c");
            store.AddReaderFromMemory(name2, "b1 include ( " + name3 + ") b2");

            var reader = Create.Reader("a1 include ( " + name2 + ") a2", store);
            Assert.Equal("a1", reader.ReadItem());
            Assert.Equal("b1", reader.ReadItem());
            Assert.Equal("c", reader.ReadItem());
            Assert.Equal("b2", reader.ReadItem());
            Assert.Equal("a2", reader.ReadItem());
        }

        [Fact]
        public static void AllowNestedIncludesRelativePath()
        {
            AssertWarnings.Activate();

            string basedir = @"C:basedir\";
            string fullname1 = basedir + "name1";
            string relname2 = @"subdir\name2";
            string fullname2 = basedir + relname2;
            string relname3 = "name3";
            string fullname3 = basedir + @"subdir\" + relname3;
            string relname4 = "name4";
            string fullname4 = basedir + relname4;

            var store = new StreamReaderStore();
            var reader = Create.Reader(
                String.Format("a1 include ( {0} ) a2 include ( {1} ) a3", relname2, relname4)
                , fullname1, store);
            store.AddReaderFromMemory(fullname2, String.Format("b1 include ( {0} ) b2", relname3));
            store.AddReaderFromMemory(fullname3, "c1");
            store.AddReaderFromMemory(fullname4, "d1");
            
            Assert.Equal("a1", reader.ReadItem());
            Assert.Equal("b1", reader.ReadItem());
            Assert.Equal("c1", reader.ReadItem());
            Assert.Equal("b2", reader.ReadItem());
            Assert.Equal("a2", reader.ReadItem());
            Assert.Equal("d1", reader.ReadItem());
            Assert.Equal("a3", reader.ReadItem());
        }

        [Fact]
        public static void ThrowOnIncompleteIncludeBlock()
        {
            AssertWarnings.ExpectAWarning();
            AssertStfException.Throws(() => { var reader = Create.Reader("include "); }, "Unexpected end of file");
            AssertStfException.Throws(() => { var reader = Create.Reader("include ( "); }, "Unexpected end of file");
            AssertStfException.Throws(() => { var reader = Create.Reader("include ( somename"); }, "Unexpected end of file");

        }

    }
#endif
    #endregion
    #endregion

    #region Reading values, numbers, ...
    #region Common test utilities
    class StfTokenReaderCommon
    {
        #region Value itself
        public static void OnNoValueWarnAndReturnDefault<T, nullableT>
            (T niceDefault, T resultDefault, nullableT someDefault, ReadValueCode<T, nullableT> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            var inputString = ")";
            T result = default(T);

            var reader = Create.Reader(inputString);
            AssertWarnings.Matching("Cannot parse|expecting.*found.*[)]", () => { result = codeDoingReading(reader, default(nullableT)); });
            Assert.Equal(niceDefault, result);
            Assert.Equal(")", reader.ReadItem());

            reader = Create.Reader(inputString);
            AssertWarnings.Matching("expecting.*found.*[)]", () => { result = codeDoingReading(reader, someDefault); });
            Assert.Equal(resultDefault, result);
            Assert.Equal(")", reader.ReadItem());
        }

        public static void ReturnValue<T>
            (T[] testValues, ReadValueCode<T> codeDoingReading)
        {
            string[] inputValues = testValues.Select(
                value => String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", value)).ToArray();
            ReturnValue<T>(testValues, inputValues, codeDoingReading);
        }

        public static void ReturnValue<T>
            (T[] testValues, string[] inputValues, ReadValueCode<T> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            string inputString = String.Join(" ", inputValues);
            var reader = Create.Reader(inputString);
            foreach (T testValue in testValues)
            {
                T result = codeDoingReading(reader);
                Assert.Equal(testValue, result);
            }
        }

        public static void WithCommaReturnValue<T>
            (T[] testValues, ReadValueCode<T> codeDoingReading)
        {
            string[] inputValues = testValues.Select(
                value => String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", value)).ToArray();
            WithCommaReturnValue(testValues, inputValues, codeDoingReading);
        }

        public static void WithCommaReturnValue<T>
            (T[] testValues, string[] inputValues, ReadValueCode<T> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            string inputString = String.Join(" ", inputValues);
            var reader = Create.Reader(inputString);
            foreach (T testValue in testValues)
            {
                T result = codeDoingReading(reader);
                Assert.Equal(testValue, result);
            }
        }

        public static void ForEmptyStringReturnNiceDefault<T, nullableT>
            (T niceDefault, nullableT someDefault, ReadValueCode<T, nullableT> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            string[] tokenValues = new string[] { "", "" };
            string emptyQuotedString = "\"\"";
            string inputString = emptyQuotedString + " " + emptyQuotedString;
            var reader = Create.Reader(inputString);

            T result = codeDoingReading(reader, default(nullableT));
            Assert.Equal(niceDefault, result);

            result = codeDoingReading(reader, someDefault);
            Assert.Equal(niceDefault, result);
        }

        public static void ForNonNumbersReturnNiceDefaultAndWarn<T, nullableT>
            (T niceDefault, T resultDefault, nullableT someDefault, ReadValueCode<T, nullableT> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            string[] testValues = new string[] { "noint", "sometoken", "(" };
            string inputString = String.Join(" ", testValues);

            var reader = Create.Reader(inputString);
            foreach (string testValue in testValues)
            {
                T result = default(T);
                AssertWarnings.Matching("Cannot parse", () => { result = codeDoingReading(reader, default(nullableT)); });
                Assert.Equal(niceDefault, result);
            }

            reader = Create.Reader(inputString);
            foreach (string testValue in testValues)
            {
                T result = default(T);
                AssertWarnings.Matching("Cannot parse", () => { result = codeDoingReading(reader, someDefault); });
                Assert.Equal(resultDefault, result);

            }
        }
        #endregion

        #region Value in blocks
        public static void OnEofWarnAndReturnDefault<T, nullableT>
            (T resultDefault, nullableT someDefault, ReadValueCode<T, nullableT> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            var inputString = "";
            var reader = Create.Reader(inputString);
            T result = default(T);
            AssertWarnings.Matching("Unexpected end of file", () => { result = codeDoingReading(reader, default(nullableT)); });
            Assert.Equal(default(T), result);
            AssertWarnings.Matching("Unexpected end of file", () => { result = codeDoingReading(reader, someDefault); });
            Assert.Equal(resultDefault, result);
        }

        public static void ForNoOpenWarnAndReturnDefault<T, nullableT>
            (T resultDefault, nullableT someDefault, ReadValueCode<T, nullableT> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            var inputString = "noblock";
            T result = default(T);

            var reader = Create.Reader(inputString);
            AssertWarnings.Matching("Block [nN]ot [fF]ound", () => { result = codeDoingReading(reader, default(nullableT)); });
            Assert.Equal(default(T), result);

            reader = Create.Reader(inputString);
            AssertWarnings.Matching("Block [nN]ot [fF]ound", () => { result = codeDoingReading(reader, someDefault); });
            Assert.Equal(resultDefault, result);
        }

        public static void OnBlockEndReturnDefaultOrWarn<T, nullableT>
            (T resultDefault, nullableT someDefault, ReadValueCode<T, nullableT> codeDoingReading)
        {
            OnBlockEndReturnNiceDefaultAndWarn(resultDefault, someDefault, codeDoingReading);
            OnBlockEndReturnGivenDefault(resultDefault, someDefault, codeDoingReading);
        }

        public static void OnBlockEndReturnNiceDefaultAndWarn<T, nullableT>
            (T resultDefault, nullableT someDefault, ReadValueCode<T, nullableT> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            var inputString = ")";
            T result = default(T);

            var reader = Create.Reader(inputString);
            AssertWarnings.Matching("Block [nN]ot [fF]ound", () => { result = codeDoingReading(reader, default(nullableT)); });
            Assert.Equal(default(T), result);
        }

        public static void OnBlockEndReturnGivenDefault<T, nullableT>
            (T resultDefault, nullableT someDefault, ReadValueCode<T, nullableT> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            var inputString = ")";
            T result = default(T);

            var reader = Create.Reader(inputString);
            result = codeDoingReading(reader, someDefault);
            Assert.Equal(resultDefault, result);
            Assert.Equal(")", reader.ReadItem());
        }

        public static void OnEmptyBlockEndReturnGivenDefault<T, nullableT>
            (T resultDefault, nullableT someDefault, ReadValueCode<T, nullableT> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            var inputString = "()a";
            T result = default(T);

            var reader = Create.Reader(inputString);
            result = codeDoingReading(reader, someDefault);
            Assert.Equal(resultDefault, result);
            Assert.Equal("a", reader.ReadItem());
        }

        public static void ReturnValueInBlock<T>
            (T[] testValues, ReadValueCode<T> codeDoingReading)
        {
            string[] inputValues = testValues.Select(
                value => String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", value)).ToArray();
            ReturnValueInBlock<T>(testValues, inputValues, codeDoingReading);
        }

        public static void ReturnValueInBlock<T>
            (T[] testValues, string[] inputValues, ReadValueCode<T> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            string[] tokenValues = inputValues.Select(
                value => String.Format(System.Globalization.CultureInfo.InvariantCulture, "({0})", value)).ToArray();
            string inputString = String.Join(" ", tokenValues);
            var reader = Create.Reader(inputString);

            foreach (T testValue in testValues)
            {
                T result = codeDoingReading(reader);
                Assert.Equal(testValue, result);
            }
        }

        public static void ReturnValueInBlockAndSkipRestOfBlock<T>
            (T[] testValues, ReadValueCode<T> codeDoingReading)
        {
            string[] inputValues = testValues.Select(
                value => String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", value)).ToArray();
            ReturnValueInBlockAndSkipRestOfBlock<T>(testValues, inputValues, codeDoingReading);
        }

        public static void ReturnValueInBlockAndSkipRestOfBlock<T>
            (T[] testValues, string[] inputValues, ReadValueCode<T> codeDoingReading)
        {
            AssertWarnings.NotExpected();
            string[] tokenValues = inputValues.Select(
                value => String.Format(System.Globalization.CultureInfo.InvariantCulture, "({0} dummy_token(nested_token))", value)
                ).ToArray();
            string inputString = String.Join(" ", tokenValues);
            var reader = Create.Reader(inputString);

            foreach (T testValue in testValues)
            {
                T result = codeDoingReading(reader);
                Assert.Equal(testValue, result);
            }
        }

        #endregion

        public delegate T ReadValueCode<T, nullableT>(STFReader reader, nullableT defaultValue);
        public delegate T ReadValueCode<T>(STFReader reader);

    }
    #endregion

    #region string
    public class OnReadingStringShould
    {
        [Fact]
        public static void ReturnValue()
        {
            AssertWarnings.NotExpected();
            string[] testValues = new string[] { "token", "somestring", "lights" };
            string inputString = String.Join(" ", testValues);
            var reader = Create.Reader(inputString);

            foreach (string testValue in testValues)
            {
                Assert.Equal(testValue, reader.ReadString());
            }
        }

        [Fact]
        public static void DontSkipValueStartingWithUnderscore()
        {
            AssertWarnings.NotExpected();
            string underscoreToken = "_underscore";
            string toBeSkippedToken = "tobeskippedtoken";
            string followingToken = "followingtoken";
            string inputString = underscoreToken + " " + toBeSkippedToken + " " + followingToken;
            var reader = Create.Reader(inputString);
            Assert.Equal(underscoreToken, reader.ReadString());
            Assert.Equal(toBeSkippedToken, reader.ReadString());
            Assert.Equal(followingToken, reader.ReadString());

        }
    }

    public class OnReadingStringBlockShould
    {
        static readonly string SOMEDEFAULT = "a";
        static readonly string[] SOMEDEFAULTS = new string[] { "ss", "SomeThing", "da" };

        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<string, string>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadStringBlock(x));
        }

        [Fact]
        public static void ForNoOpenWarnAndReturnDefault()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<string, string>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadStringBlock(x));
        }

        [Fact]
        public static void OnBlockEndReturnDefaultOrWarn()
        {
            StfTokenReaderCommon.OnBlockEndReturnDefaultOrWarn<string, string>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadStringBlock(x));
        }

        [Fact]
        public static void ReturnValueInBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlock<string>(SOMEDEFAULTS, reader => reader.ReadStringBlock(null));
        }

        [Fact]
        public static void ReturnValueInBlockAndSkipRestOfBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlockAndSkipRestOfBlock<string>(SOMEDEFAULTS, reader => reader.ReadStringBlock(null));
        }

    }
    #endregion

    #region int
    public class OnReadingIntShould
    {
        static readonly int NICEDEFAULT = default(int);
        static readonly int SOMEDEFAULT = -2;
        static readonly int[] SOMEDEFAULTS = new int[] { 3, 5, -6 };

        [Fact]
        public static void OnNoValueWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnNoValueWarnAndReturnDefault<int, int?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadInt(x));
        }

        [Fact]
        public static void ReturnValue()
        {
            StfTokenReaderCommon.ReturnValue<int>(SOMEDEFAULTS, reader => reader.ReadInt(null));
        }

        [Fact]
        public static void ReturnValueWithSign()
        {
            string[] inputs = { "+2", "-3" };
            int[] expected = { 2, -3 };
            StfTokenReaderCommon.ReturnValue<int>(expected, inputs, reader => reader.ReadInt(null));
        }

        [Fact]
        public static void WithCommaReturnValue()
        {
            StfTokenReaderCommon.WithCommaReturnValue<int>(SOMEDEFAULTS, reader => reader.ReadInt(null));
        }

        [Fact]
        public static void ForEmptyStringReturnNiceDefault()
        {
            StfTokenReaderCommon.ForEmptyStringReturnNiceDefault<int, int?>
                (NICEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadInt(x));
        }

        [Fact]
        public static void ForNonNumbersReturnZeroAndWarn()
        {
            StfTokenReaderCommon.ForNonNumbersReturnNiceDefaultAndWarn<int, int?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadInt(x));
        }

    }

    public class OnReadingIntBlockShould
    {
        static readonly int SOMEDEFAULT = -2;
        static readonly int[] SOMEDEFAULTS = new int[] { 3, 5, -6 };

        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<int, int?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadIntBlock(x));
        }

        [Fact]
        public static void ForNoOpenReturnDefaultAndWarn()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<int, int?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadIntBlock(x));
        }

        [Fact]
        public static void OnBlockEndReturnDefaultOrWarn()
        {
            StfTokenReaderCommon.OnBlockEndReturnDefaultOrWarn<int, int?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadIntBlock(x));
        }

        [Fact]
        public static void ReturnValueInBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlock<int>(SOMEDEFAULTS, reader => reader.ReadIntBlock(null));
        }

        [Fact]
        public static void ReturnValueInBlockAndSkipRestOfBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlockAndSkipRestOfBlock<int>(SOMEDEFAULTS, reader => reader.ReadIntBlock(null));
        }

    }
    #endregion

    #region uint
    public class OnReadingUIntShould
    {
        static readonly uint NICEDEFAULT = default(uint);
        static readonly uint SOMEDEFAULT = 2;
        static readonly uint[] SOMEDEFAULTS = new uint[] { 3, 5, 200 };

        [Fact]
        public static void OnNoValueWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnNoValueWarnAndReturnDefault<uint, uint?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadUInt(x));
        }

        [Fact]
        public static void ReturnValue()
        {
            StfTokenReaderCommon.ReturnValue<uint>(SOMEDEFAULTS, reader => reader.ReadUInt(null));
        }

        [Fact]
        public static void WithCommaReturnValue()
        {
            StfTokenReaderCommon.WithCommaReturnValue<uint>(SOMEDEFAULTS, reader => reader.ReadUInt(null));
        }

        [Fact]
        public static void ForEmptyStringReturnNiceDefault()
        {
            StfTokenReaderCommon.ForEmptyStringReturnNiceDefault<uint, uint?>
                (NICEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadUInt(x));
        }

        [Fact]
        public static void ForNonNumbersReturnZeroAndWarn()
        {
            StfTokenReaderCommon.ForNonNumbersReturnNiceDefaultAndWarn<uint, uint?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadUInt(x));
        }
    }

    public class OnReadingUIntBlockShould
    {
        static readonly uint SOMEDEFAULT = 4;
        static readonly uint[] SOMEDEFAULTS = new uint[] { 3, 5, 20 };

        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<uint, uint?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadUIntBlock(x));
        }

        [Fact]
        public static void ForNoOpenReturnDefaultAndWarn()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<uint, uint?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadUIntBlock(x));
        }

        [Fact]
        public static void OnBlockEndReturnDefaultOrWarn()
        {
            StfTokenReaderCommon.OnBlockEndReturnDefaultOrWarn<uint, uint?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadUIntBlock(x));
        }

        [Fact]
        public static void ReturnValueInBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlock<uint>(SOMEDEFAULTS, reader => reader.ReadUIntBlock(null));
        }

        [Fact]
        public static void ReturnValueInBlockAndSkipRestOfBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlockAndSkipRestOfBlock<uint>(SOMEDEFAULTS, reader => reader.ReadUIntBlock(null));
        }
    }
    #endregion

    #region hex
    public class OnReadingHexShould
    {
        static readonly uint NICEDEFAULT = default(uint);
        static readonly uint SOMEDEFAULT = 2;
        static readonly uint[] SOMEDEFAULTS = new uint[] { 3, 5, 129 };
        static readonly string[] STRINGDEFAULTS = new string[] { "0000003", "00000005", "00000081" };

        [Fact]
        public static void OnNoValueWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnNoValueWarnAndReturnDefault<uint, uint?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadHex(x));
        }

        [Fact]
        public static void ReturnValue()
        {
            StfTokenReaderCommon.ReturnValue<uint>(SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadHex(null));
        }

        [Fact]
        public static void WithCommaReturnValue()
        {
            StfTokenReaderCommon.WithCommaReturnValue<uint>(SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadHex(null));
        }

        [Fact]
        public static void ForEmptyStringReturnNiceDefault()
        {
            StfTokenReaderCommon.ForEmptyStringReturnNiceDefault<uint, uint?>
                (NICEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadHex(x));
        }

        [Fact]
        public static void ForNonNumbersReturnZeroAndWarn()
        {
            StfTokenReaderCommon.ForNonNumbersReturnNiceDefaultAndWarn<uint, uint?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadHex(x));
        }
    }

    public class OnReadingHexBlockShould
    {
        static readonly uint SOMEDEFAULT = 4;
        static readonly uint[] SOMEDEFAULTS = new uint[] { 3, 5, 20 };
        static readonly string[] STRINGDEFAULTS = new string[] { "00000003", "00000005", "00000014" };

        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<uint, uint?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadHexBlock(x));
        }

        [Fact]
        public static void ForNoOpenReturnDefaultAndWarn()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<uint, uint?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadHexBlock(x));
        }

        [Fact]
        public static void OnBlockEndReturnDefaultOrWarn()
        {
            StfTokenReaderCommon.OnBlockEndReturnDefaultOrWarn<uint, uint?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadHexBlock(x));
        }

        [Fact]
        public static void ReturnValueInBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlock<uint>(SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadHexBlock(null));
        }

        [Fact]
        public static void ReturnValueInBlockAndSkipRestOfBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlockAndSkipRestOfBlock<uint>(SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadHexBlock(null));
        }
    }
    #endregion

    #region bool
    // ReadBool is not supported.
    public class OnReadingBoolBlockShould
    {
        static readonly bool SOMEDEFAULT = false;
        static readonly bool[] SOMEDEFAULTS1 = new bool[] { false, true, false, true, true, true, true };
        static readonly string[] STRINGDEFAULTS1 = new string[] { "false", "true", "0", "1", "1.1", "-2.9e3", "non" };

        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<bool, bool>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadBoolBlock(x));
        }

        [Fact]
        public static void ForNoOpenReturnDefaultAndWarn()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<bool, bool>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadBoolBlock(x));
        }

        [Fact]
        public static void OnBlockEndReturnGivenDefault()
        {
            StfTokenReaderCommon.OnBlockEndReturnGivenDefault<bool, bool>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadBoolBlock(x));
        }

        [Fact]
        public static void ReturnStringValueInBlock()
        {
            string[] inputValues = { "true", "false" };
            bool[] expectedValues = { true, false };
            StfTokenReaderCommon.ReturnValueInBlock<bool>(expectedValues, inputValues, reader => reader.ReadBoolBlock(false));
        }

        [Fact]
        public static void ReturnIntValueInBlock()
        {
            string[] inputValues = { "0", "1", "-2" };
            bool[] expectedValues = { false, true, true };
            StfTokenReaderCommon.ReturnValueInBlock<bool>(expectedValues, inputValues, reader => reader.ReadBoolBlock(false));
        }

        [Fact]
        public static void ReturnDefaultValueOtherwiseInBlock()
        {
            bool[] expectedValues;
            string[] inputValues = { "0.1", "1.1", "something", "()" };
            bool expectedValue = false;
            expectedValues = new bool[] { expectedValue, expectedValue, expectedValue, expectedValue };
            StfTokenReaderCommon.ReturnValueInBlock<bool>(expectedValues, inputValues, reader => reader.ReadBoolBlock(expectedValue));

            expectedValue = true;
            expectedValues = new bool[] { expectedValue, expectedValue, expectedValue, expectedValue };
            StfTokenReaderCommon.ReturnValueInBlock<bool>(expectedValues, inputValues, reader => reader.ReadBoolBlock(expectedValue));
        }

        [Fact]
        public static void ReturnStringValueInBlockAndSkipRestOfBlock()
        {
            string[] inputValues = { "true next", "false next" };
            bool[] expectedValues = { true, false };
            StfTokenReaderCommon.ReturnValueInBlock<bool>(expectedValues, inputValues, reader => reader.ReadBoolBlock(false));
        }

        [Fact]
        public static void ReturnIntValueInBlockAndSkipRestOfBlock()
        {
            string[] inputValues = { "0 next", "1 some", "-2 thing" };
            bool[] expectedValues = { false, true, true };
            StfTokenReaderCommon.ReturnValueInBlock<bool>(expectedValues, inputValues, reader => reader.ReadBoolBlock(false));
        }

        [Fact]
        public static void ReturnDefaultValueOtherwiseInBlockAndSkipRestOfBlock()
        {
            string[] inputValues = { "0.1 x", "1.1 y", "something z", "()" };
            bool[] expectedValues = { false, false, false, false };
            StfTokenReaderCommon.ReturnValueInBlock<bool>(expectedValues, inputValues, reader => reader.ReadBoolBlock(false));
        }
        [Fact]
        public static void OnEmptyBlockReturnGivenDefault()
        {
            StfTokenReaderCommon.OnEmptyBlockEndReturnGivenDefault<bool, bool>
               (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadBoolBlock(x));
        }

        [Fact]
        public static void OnNonBoolOrIntInBlockReturnFalseWithoutWarning()
        {
            AssertWarnings.NotExpected();
            string[] testValues = new string[] { "(bool)", "(something)" };
            string inputString = String.Join(" ", testValues);

            bool expectedResult = false;
            var reader = Create.Reader(inputString);
            foreach (string testValue in testValues)
            {
                bool result = !expectedResult;
                result = reader.ReadBoolBlock(expectedResult);
                Assert.Equal(expectedResult, result);
            }

            expectedResult = true;
            reader = Create.Reader(inputString);
            foreach (string testValue in testValues)
            {
                bool result = !expectedResult;
                result = reader.ReadBoolBlock(expectedResult);
                Assert.Equal(expectedResult, result);
            }
        }
    }
    #endregion

    #region double
    public class OnReadingDoubleShould
    {
        static readonly double NICEDEFAULT = default(double);
        static readonly double SOMEDEFAULT = -2;
        static readonly double[] SOMEDEFAULTS = new double[] { 3, 5, -6 };

        [Fact]
        public static void OnNoValueWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnNoValueWarnAndReturnDefault<double, double?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadDouble(x));
        }

        [Fact]
        public static void ReturnValue()
        {
            StfTokenReaderCommon.ReturnValue<double>(SOMEDEFAULTS, reader => reader.ReadDouble(null));
        }

        [Fact]
        public static void WithCommaReturnValue()
        {
            StfTokenReaderCommon.WithCommaReturnValue<double>(SOMEDEFAULTS, reader => reader.ReadDouble(null));
        }

        [Fact]
        public static void ForEmptyStringReturnNiceDefault()
        {
            StfTokenReaderCommon.ForEmptyStringReturnNiceDefault<double, double?>
                (NICEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadDouble(x));
        }

        [Fact]
        public static void ForNonNumbersReturnZeroAndWarn()
        {
            StfTokenReaderCommon.ForNonNumbersReturnNiceDefaultAndWarn<double, double?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadDouble(x));
        }

    }

    public class OnReadingDoubleBlockShould
    {
        static readonly double SOMEDEFAULT = -2;
        static readonly double[] SOMEDEFAULTS = new double[] { 3, 5, -6 };

        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<double, double?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadDoubleBlock(x));
        }

        [Fact]
        public static void ForNoOpenReturnDefaultAndWarn()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<double, double?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadDoubleBlock(x));
        }

        [Fact]
        public static void OnBlockEndReturnDefaultOrWarn()
        {
            StfTokenReaderCommon.OnBlockEndReturnDefaultOrWarn<double, double?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadDoubleBlock(x));
        }

        [Fact]
        public static void ReturnValueInBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlock<double>(SOMEDEFAULTS, reader => reader.ReadDoubleBlock(null));
        }

        [Fact]
        public static void ReturnValueInBlockAndSkipRestOfBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlockAndSkipRestOfBlock<double>(SOMEDEFAULTS, reader => reader.ReadDoubleBlock(null));
        }

    }
    #endregion

    #region float
    public class OnReadingFloatShould
    {
        static readonly float NICEDEFAULT = default(float);
        static readonly float SOMEDEFAULT = 2.1f;
        static readonly float[] SOMEDEFAULTS = new float[] { 1.1f, 4.5e6f, -0.01f };

        [Fact]
        public static void OnNoValueWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnNoValueWarnAndReturnDefault<float, float?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadFloat(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ReturnValue()
        {
            StfTokenReaderCommon.ReturnValue<float>(SOMEDEFAULTS, reader => reader.ReadFloat(STFReader.UNITS.None, null));
        }

        [Fact]
        public static void WithCommaReturnValue()
        {
            StfTokenReaderCommon.WithCommaReturnValue<float>(SOMEDEFAULTS, reader => reader.ReadFloat(STFReader.UNITS.None, null));
        }

        [Fact]
        public static void ForEmptyStringReturnNiceDefault()
        {
            StfTokenReaderCommon.ForEmptyStringReturnNiceDefault<float, float?>
                (NICEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadFloat(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ForNonNumbersReturnZeroAndWarn()
        {
            StfTokenReaderCommon.ForNonNumbersReturnNiceDefaultAndWarn<float, float?>
                (NICEDEFAULT, SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadFloat(STFReader.UNITS.None, x));
        }

    }

    public class OnReadingFloatBlockShould
    {
        static readonly float SOMEDEFAULT = 2.1f;
        static readonly float[] SOMEDEFAULTS = new float[] { 1.1f, 4.5e6f, -0.01f };

        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<float, float?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadFloatBlock(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ForNoOpenReturnDefaultAndWarn()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<float, float?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadFloatBlock(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void OnBlockEndReturnDefaultOrWarn()
        {
            StfTokenReaderCommon.OnBlockEndReturnDefaultOrWarn<float, float?>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadFloatBlock(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ReturnValueInBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlock<float>
                (SOMEDEFAULTS, reader => reader.ReadFloatBlock(STFReader.UNITS.None, null));
        }

        [Fact]
        public static void ReturnValueInBlockAndSkipRestOfBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlockAndSkipRestOfBlock<float>
                (SOMEDEFAULTS, reader => reader.ReadFloatBlock(STFReader.UNITS.None, null));
        }
    }
    #endregion

    #region Vector2
    public class OnReadingVector2BlockShould
    {
        static readonly Vector2 SOMEDEFAULT = new Vector2(1.1f, 1.2f);
        static readonly Vector2[] SOMEDEFAULTS = new Vector2[] { new Vector2(1.3f, 1.5f), new Vector2(-2f, 1e6f) };
        static readonly string[] STRINGDEFAULTS = new string[] { "1.3 1.5 ignore", "-2 1000000" };


        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<Vector2, Vector2>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadVector2Block(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ForNoOpenReturnDefaultAndWarn()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<Vector2, Vector2>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadVector2Block(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void OnBlockEndReturnDefaultWhenGiven()
        {
            StfTokenReaderCommon.OnBlockEndReturnGivenDefault<Vector2, Vector2>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadVector2Block(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ReturnValueInBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlock<Vector2>
                (SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadVector2Block(STFReader.UNITS.None, Vector2.Zero));
        }

        [Fact]
        public static void ReturnValueInBlockAndSkipRestOfBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlockAndSkipRestOfBlock<Vector2>
                (SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadVector2Block(STFReader.UNITS.None, Vector2.Zero));
        }
    }
    #endregion

    #region Vector3
    public class OnReadingVector3BlockShould
    {
        static readonly Vector3 SOMEDEFAULT = new Vector3(1.1f, 1.2f, 1.3f);
        static readonly Vector3[] SOMEDEFAULTS = new Vector3[] { new Vector3(1.3f, 1.5f, 1.8f), new Vector3(1e-3f, -2f, -1e3f) };
        static readonly string[] STRINGDEFAULTS = new string[] { "1.3 1.5 1.8 ignore", "0.001, -2, -1000" };


        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<Vector3, Vector3>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadVector3Block(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ForNoOpenReturnDefaultAndWarn()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<Vector3, Vector3>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadVector3Block(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void OnBlockEndReturnDefaultWhenGiven()
        {
            StfTokenReaderCommon.OnBlockEndReturnGivenDefault<Vector3, Vector3>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadVector3Block(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ReturnValueInBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlock<Vector3>
                (SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero));
        }

        [Fact]
        public static void ReturnValueInBlockAndSkipRestOfBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlockAndSkipRestOfBlock<Vector3>
                (SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero));
        }
    }
    #endregion

    #region Vector4
    public class OnReadingVector4BlockShould
    {
        static readonly Vector4 SOMEDEFAULT = new Vector4(1.1f, 1.2f, 1.3f, 1.4f);
        static readonly Vector4[] SOMEDEFAULTS = new Vector4[] { new Vector4(1.3f, 1.5f, 1.7f, 1.9f) };
        static readonly string[] STRINGDEFAULTS = new string[] { "1.3 1.5 1.7 1.9 ignore" };


        [Fact]
        public static void OnEofWarnAndReturnDefault()
        {
            StfTokenReaderCommon.OnEofWarnAndReturnDefault<Vector4, Vector4>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadVector4Block(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ForNoOpenReturnDefaultAndWarn()
        {
            StfTokenReaderCommon.ForNoOpenWarnAndReturnDefault<Vector4, Vector4>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadVector4Block(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void OnBlockEndReturnDefaultWhenGiven()
        {
            StfTokenReaderCommon.OnBlockEndReturnGivenDefault<Vector4, Vector4>
                (SOMEDEFAULT, SOMEDEFAULT, (reader, x) => reader.ReadVector4Block(STFReader.UNITS.None, x));
        }

        [Fact]
        public static void ReturnValueInBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlock<Vector4>
                (SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadVector4Block(STFReader.UNITS.None, Vector4.Zero));
        }

        [Fact]
        public static void ReturnValueInBlockAndSkipRestOfBlock()
        {
            StfTokenReaderCommon.ReturnValueInBlockAndSkipRestOfBlock<Vector4>
                (SOMEDEFAULTS, STRINGDEFAULTS, reader => reader.ReadVector4Block(STFReader.UNITS.None, Vector4.Zero));
        }
    }
    #endregion

    #endregion

    #region TokenProcessor and Parseblock/File
    public class TokenProcessingShould
    {
        [Fact]
        public static void DefineTokenProcessor()
        {
            string sometoken = "sometoken";
            int called = 0;
            var tokenProcessor = new STFReader.TokenProcessor(sometoken, () => { called++; });
            Assert.Equal(sometoken, tokenProcessor.token);
            tokenProcessor.processor.Invoke();
            Assert.Equal(1, called);
        }

        [Fact]
        public static void ParseABlock()
        {
            string source = "block1 block2()block1()";
            var reader = Create.Reader(source);
            int called1 = 0;
            int called2 = 0;
            reader.ParseBlock(new[] {
                    new STFReader.TokenProcessor("block1", () => { called1++; }),
                    new STFReader.TokenProcessor("block2", () => { called2++; })
                });
            Assert.Equal(2, called1);
            Assert.Equal(1, called2);
        }

        [Fact]
        public static void ParseABlockWithBreakOut()
        {
            string source = "block1 block2()block1()";
            var reader = Create.Reader(source);
            int called1 = 0;
            int called2 = 0;
            reader.ParseBlock(() => called2 == 1, new[] {
                    new STFReader.TokenProcessor("block1", () => { called1++; }),
                    new STFReader.TokenProcessor("block2", () => { called2++; })
                });
            Assert.Equal(1, called1);
            Assert.Equal(1, called2);
        }

        [Fact]
        public static void ParseABlockAndThenContinue()
        {
            string followingtoken = "sometoken";
            string source = "block1())" + followingtoken;
            var reader = Create.Reader(source);
            int called1 = 0;
            reader.ParseBlock(new[] {
                    new STFReader.TokenProcessor("block1", () => { called1++; }),
            });
            Assert.Equal(followingtoken, reader.ReadItem());
        }

        [Fact]
        public static void ParseAFile()
        {
            string source = "block1 block2()block1()";
            var reader = Create.Reader(source);
            int called1 = 0;
            int called2 = 0;
            reader.ParseFile(new[] {
                    new STFReader.TokenProcessor("block1", () => { called1++; }),
                    new STFReader.TokenProcessor("block2", () => { called2++; })
                });
            Assert.Equal(2, called1);
            Assert.Equal(1, called2);
        }

        [Fact]
        public static void ParseAFileWithBreakOut()
        {
            string source = "block1 block2()block1()";
            var reader = Create.Reader(source);
            int called1 = 0;
            int called2 = 0;
            reader.ParseFile(() => called2 == 1, new[] {
                    new STFReader.TokenProcessor("block1", () => { called1++; }),
                    new STFReader.TokenProcessor("block2", () => { called2++; })
                });
            Assert.Equal(1, called1);
            Assert.Equal(1, called2);
        }

        [Fact]
        public static void ParseAFileTillEOF()
        {
            string followingtoken = "block2";
            string source = "block1())" + followingtoken;
            var reader = Create.Reader(source);
            int called1 = 0;
            int called2 = 0;
            reader.ParseFile(new[] {
                    new STFReader.TokenProcessor("block1", () => { called1++; }),
                    new STFReader.TokenProcessor("block2", () => { called2++; })
            });
            Assert.Equal(1, called2);
            Assert.True(reader.Eof);
        }
    }
    #endregion

    #region Legacy
    public class UsedTo
    {
        [Fact]
        static void PeekPastWhiteSpace()
        {   //testing only what is really needed right now.
            var reader = Create.Reader("a  )  ");
            Assert.False(')' == reader.PeekPastWhitespace());
            Assert.False(-1 == reader.PeekPastWhitespace());

            reader.ReadItem();
            Assert.Equal(')', reader.PeekPastWhitespace());
            reader.ReadItem();
            Assert.Equal(-1, reader.PeekPastWhitespace());
        }

    }
    #endregion

    #region Test utilities
    class Create
    {
        public static STFReader Reader(string source)
        {
            return Create.Reader(source, "some.stf", false);
        }

        public static STFReader Reader(string source, string fileName)
        {
            return Create.Reader(source, fileName, false);
        }

        public static STFReader Reader(string source, bool useTree)
        {
            return Create.Reader(source, "some.stf", useTree);
        }

        public static STFReader Reader(string source, string fileName, bool useTree)
        {
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(source));
            return new STFReader(memoryStream, fileName, Encoding.ASCII, useTree);
        }

#if NEW_READER
        public static STFReader Reader(string source, MSTS.Parsers.IStreamReaderFactory factory)
        {
            return Reader(source, "some.stf", factory);
        }

        public static STFReader Reader(string source, string fileName, MSTS.Parsers.IStreamReaderFactory factory)
        {
            STFReader.NextStreamReaderFactory = factory;
            return Create.Reader(source, fileName, false);
        }
#endif
    }

    struct TokenTester
    {
        public string inputString;
        public string[] expectedTokens;
        public int[] expectedLineNumbers;

        public TokenTester(string input, string[] output)
        {
            inputString = input;
            expectedTokens = output;
            expectedLineNumbers = new int[0];
        }

        public TokenTester(string input, int[] lineNumbers)
        {
            inputString = input;
            expectedTokens = new string[0];
            expectedLineNumbers = lineNumbers;
        }
    }

    /// <summary>
    /// Class to help assert not only the type of exception being thrown, but also the message being generated
    /// </summary>
    static class AssertStfException
    {
        /// <summary>
        /// Run the testcode, make sure an exception is called and test the exception
        /// </summary>
        /// <param name="testCode">Code that will be executed</param>
        /// <param name="pattern">The pattern that the exception message should match</param>
        public static void Throws(Action testCode, string pattern)
        {
            var exception = Record.Exception(testCode);
            Assert.NotNull(exception);
            Assert.IsType<STFException>(exception);
            Assert.True(Regex.IsMatch(exception.Message, pattern), exception.Message + " does not match pattern: " + pattern);
        }
    }

#if NEW_READER
    class StreamReaderStore : IStreamReaderFactory
    {
        public StreamReaderStore()
        {
            storedStreamReaders = new Dictionary<string, StreamReader>();
            storedSimisSignatures = new Dictionary<string, string>();
            ShouldReadSimisSignature = false;
        }

        public void AddReaderFromMemory(string name, string source)
        {
            var memoryStream = new MemoryStream(Encoding.ASCII.GetBytes(source));
            var reader = new StreamReader(memoryStream, Encoding.ASCII);
            storedStreamReaders[name] = reader;
            storedSimisSignatures[name] = ShouldReadSimisSignature ? reader.ReadLine() : null;
        }

        public StreamReader GetStreamReader(string name, out string simisSignature)
        {
            Assert.True(storedStreamReaders.ContainsKey(name), "requested file:" + name + "is not available");
            simisSignature = storedSimisSignatures[name];
            return storedStreamReaders[name];
        }

        Dictionary<string, StreamReader> storedStreamReaders;
        Dictionary<string, string> storedSimisSignatures;

        public bool ShouldReadSimisSignature { get; set; }
    }
#endif
    #endregion
    #endregion

}

