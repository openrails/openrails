// COPYRIGHT 2013, 2014 by the Open Rails project.
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
using MSTS.Parsers;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace Tests.Msts.Parsers
{
    public static class StfReader
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
            using (var reader = new STFReader(new MemoryStream(Encoding.ASCII.GetBytes("")), "EmptyFile.stf", Encoding.ASCII, false))
            {
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal("EmptyFile.stf", reader.FileName);
                Assert.Equal(1, reader.LineNumber);
                Assert.Equal(null, reader.SimisSignature);
                Assert.Throws<Xunit.Sdk.TraceAssertException>(() => reader.Tree);

                // All of the following will execute successfully at EOF...
                reader.MustMatch("ANYTHING GOES");
                reader.ParseBlock(new STFReader.TokenProcessor[0]);
                reader.ParseFile(new STFReader.TokenProcessor[0]);
                Assert.Equal(-1, reader.PeekPastWhitespace());
                Assert.Equal(false, reader.ReadBoolBlock(false));
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
                Assert.Equal(null, reader.ReadStringBlock(null));
                Assert.Equal(0U, reader.ReadUInt(null));
                Assert.Equal(0U, reader.ReadUIntBlock(null));
                Assert.Equal(Vector3.Zero, reader.ReadVector3Block(STFReader.UNITS.None, Vector3.Zero));
                Assert.Equal(Vector4.Zero, reader.ReadVector4Block(STFReader.UNITS.None, Vector4.Zero));
            }
        }

        [Fact]
        public static void EncodingAscii()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.ASCII.GetBytes("TheBlock()")), "", Encoding.ASCII, false))
            {
                reader.MustMatch("TheBlock");
            }
        }

        [Fact]
        public static void EncodingUtf16()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("TheBlock()")), "", Encoding.Unicode, false))
            {
                reader.MustMatch("TheBlock");
            }
        }

        [Fact]
        public static void EmptyBlock()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("TheBlock()")), "EmptyBlock.stf", Encoding.Unicode, false))
            {
                Assert.False(reader.Eof, "STFReader.Eof");
                Assert.False(reader.EOF(), "STFReader.EOF()");
                Assert.False(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal("EmptyBlock.stf", reader.FileName);
                Assert.Equal(1, reader.LineNumber);
                Assert.Equal(null, reader.SimisSignature);
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
                Assert.Equal(true, reader.ReadBoolBlock(false));
                Assert.Equal(false, reader.ReadBoolBlock(true));
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
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("1.1 1.2 1.3km 1.4 1.5km")), "", Encoding.Unicode, false))
            {
                Assert.Equal(1.10000f, reader.ReadFloat(STFReader.UNITS.None, null), DynamicPrecisionEqualityComparer.Float);
                Assert.Equal(1.20000f, reader.ReadFloat(STFReader.UNITS.Distance, null), DynamicPrecisionEqualityComparer.Float);
                Assert.Equal(1300.00000f, reader.ReadFloat(STFReader.UNITS.Distance, null), DynamicPrecisionEqualityComparer.Float);
                Assert.Equal(1.40000f, reader.ReadFloat(STFReader.UNITS.Distance | STFReader.UNITS.Compulsory, null), DynamicPrecisionEqualityComparer.Float); // TODO: Um, shouldn't this fail or something?
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
        public static void Bug1274713ParentheticalComment()
        {
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
                Bug1274713ParenthicalCommentSingle(part1 + middle + part2 + part3);
            foreach (var middle in middles)
                Bug1274713ParenthicalCommentSingle(part1 + part2 + middle + part3);
        }

        static void Bug1274713ParenthicalCommentSingle(string inputString)
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

                //Possibly it is not nice to have a 'while' loop in a test'
                //But it is not a requirement that STFreader returns "#\u00b6", it only is a possibility.
                //If STFreader is ever rewritten, the current test should still pass, but preferably #\u00b6 is not needed anymore
                string lastItem = reader.ReadItem();
                while (lastItem == "#\u00b6")
                {
                    lastItem = reader.ReadItem();
                }
                Assert.Equal("wagon()", reader.Tree.ToLower());

                reader.ReadItem();
                Assert.Equal("wagon(sound", reader.Tree.ToLower());
                Assert.Equal("test.sms", reader.ReadStringBlock(""));
                reader.SkipRestOfBlock();

                Assert.True(reader.Eof, "STFReader.Eof");
            }
        }
    }
}
