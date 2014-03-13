// COPYRIGHT 2013 by the Open Rails project.
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
using Microsoft.Xna.Framework;
using MSTS.Parsers;
using Xunit;

namespace Tests
{
    public class STF
    {
        const double Metre3ToLitres = 1000;
        const double PascalToPSI = 0.0001450377438972831;

        [Fact]
        public void EmptyFile()
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
        public void EncodingASCII()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.ASCII.GetBytes("TheBlock()")), "", Encoding.ASCII, false))
            {
                reader.MustMatch("TheBlock");
            }
        }

        [Fact]
        public void EncodingUTF16()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("TheBlock()")), "", Encoding.Unicode, false))
            {
                reader.MustMatch("TheBlock");
            }
        }

        [Fact]
        public void EmptyBlock()
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
        public void NumericFormats()
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
        public void StringFormats()
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
        public void BlockNumericFormats()
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
        public void BlockStringFormats()
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
        public void BlockVectorFormats()
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
        public void Units()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes("1.1 1.2 1.3km 1.4 1.5km")), "", Encoding.Unicode, false))
            {
                Assert.Equal(1.10000f, reader.ReadFloat(STFReader.UNITS.None, null), 5);
                Assert.Equal(1.20000f, reader.ReadFloat(STFReader.UNITS.Distance, null), 5);
                Assert.Equal(1300.00000f, reader.ReadFloat(STFReader.UNITS.Distance, null), 5);
                Assert.Equal(1.40000f, reader.ReadFloat(STFReader.UNITS.Distance | STFReader.UNITS.Compulsory, null), 5); // TODO: Um, shouldn't this fail or something?
                Assert.Equal(1500.00000f, reader.ReadFloat(STFReader.UNITS.Distance | STFReader.UNITS.Compulsory, null), 5);
                Assert.True(reader.Eof, "STFReader.Eof");
            }
        }

        void UnitConversionTest(string input, double output, STFReader.UNITS unit)
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes(input)), "", Encoding.Unicode, false))
            {
                Assert.Equal(output, reader.ReadFloat(unit, null), 6);
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
        public void UnitConversionBase_MSTS_Current()
        {
            UnitConversionTest("1.2a", 1.2, STFReader.UNITS.Current);
            // TODO: UnitConversionTest("1.2ka", 1200, STFReader.UNITS.Current);
            // TODO: UnitConversionTest("1.2ma", 1200000, STFReader.UNITS.Current);
        }

        /* Base units for energy available in MSTS:
         *     j         Joule
         *     nm        Newton meter
         */
        [Fact]
        public void UnitConversionBase_MSTS_Energy()
        {
            // TODO: UnitConversionTest("1.2j", 1.2, STFReader.UNITS.Energy);
            // TODO: UnitConversionTest("1.2nm", 1.2, STFReader.UNITS.Energy);
        }

        /* Base units for force available in MSTS:
         *     n         Newton
         *     kn        Kilo-newton
         *     lbf       Pound-force
         */
        [Fact]
        public void UnitConversionBase_MSTS_Force()
        {
            UnitConversionTest("1.2n", 1.2, STFReader.UNITS.Force);
            UnitConversionTest("1.2kn", 1200, STFReader.UNITS.Force);
            UnitConversionTest("1.2lbf", 5.3378659383126, STFReader.UNITS.Force); // http://en.wikipedia.org/wiki/Pound-force#Definitions
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
        public void UnitConversionBase_MSTS_Length()
        {
            UnitConversionTest("1.2mm", 0.0012, STFReader.UNITS.Distance);
            UnitConversionTest("1.2cm", 0.012, STFReader.UNITS.Distance);
            UnitConversionTest("1.2m", 1.2, STFReader.UNITS.Distance);
            UnitConversionTest("1.2km", 1200, STFReader.UNITS.Distance);
            // TODO: UnitConversionTest("1.2\"", 0.03048, STFReader.UNITS.Distance);
            UnitConversionTest("1.2in", 0.03048, STFReader.UNITS.Distance);
            // TODO: UnitConversionTest("1.2\'", 0.36576, STFReader.UNITS.Distance);
            UnitConversionTest("1.2ft", 0.36576, STFReader.UNITS.Distance);
            // TODO: UnitConversionTest("1.2mil", 1931.2128, STFReader.UNITS.Distance);
        }

        /* Base units for mass available in MSTS:
         *     g         Gram
         *     kg        Kilogram
         *     t         Tonne
         *     tn        Short/US ton?
         *     ton       Long/UK ton?
         *     lb        Pound
         *     lbs       Pound
         */
        [Fact]
        public void UnitConversionBase_MSTS_Mass()
        {
            // Note: This unit returns values in kg, not g.
            // TODO: UnitConversionTest("1.2g", 0.0012, STFReader.UNITS.Mass);
            UnitConversionTest("1.2kg", 1.2, STFReader.UNITS.Mass);
            UnitConversionTest("1.2t", 1200, STFReader.UNITS.Mass);
            // TODO: UnitConversionTest("1.2tn", 1088.621688, STFReader.UNITS.Mass);
            // TODO: UnitConversionTest("1.2ton", 1219.25629056, STFReader.UNITS.Mass);
            UnitConversionTest("1.2lb", 0.544310844, STFReader.UNITS.Mass);
            // TODO: UnitConversionTest("1.2lbs", 0.544310844, STFReader.UNITS.Mass);
        }

        /* Base units for power available in MSTS:
         *     kw        Kilowatt
         *     hp        Horsepower
         */
        [Fact]
        public void UnitConversionBase_MSTS_Power()
        {
            UnitConversionTest("1.2kw", 1200, STFReader.UNITS.Power);
            UnitConversionTest("1.2hp", 894.8398458987242, STFReader.UNITS.Power);
        }

        /* Base units for pressure available in MSTS:
         *     pascal    Pascal
         *     mbar      Millibar
         *     bar       Bar
         *     psi       psi
         */
        [Fact]
        public void UnitConversionBase_MSTS_Pressure()
        {
            // Note: This unit returns values in PSI, not pascal.
            // TODO: UnitConversionTest("1.2pascal", 1.2 * PascalToPSI, STFReader.UNITS.PressureDefaultInHg);
            // TODO: UnitConversionTest("1.2pascal", 1.2 * PascalToPSI, STFReader.UNITS.PressureDefaultPSI);
            // TODO: UnitConversionTest("1.2mbar", 120 * PascalToPSI, STFReader.UNITS.PressureDefaultInHg);
            // TODO: UnitConversionTest("1.2mbar", 120 * PascalToPSI, STFReader.UNITS.PressureDefaultPSI);
            UnitConversionTest("1.2bar", 120000 * PascalToPSI, STFReader.UNITS.PressureDefaultInHg);
            UnitConversionTest("1.2bar", 120000 * PascalToPSI, STFReader.UNITS.PressureDefaultPSI);
            UnitConversionTest("1.2psi", 8273.7084 * PascalToPSI, STFReader.UNITS.PressureDefaultInHg);
            UnitConversionTest("1.2psi", 8273.7084 * PascalToPSI, STFReader.UNITS.PressureDefaultPSI);
        }

        /* Base units for rotation available in MSTS:
         *     deg       Degree
         *     rad       Radian
         */
        [Fact]
        public void UnitConversionBase_MSTS_Rotation()
        {
            // TODO: UnitConversionTest("1.2deg", 1.2, STFReader.UNITS.Rotation);
            // TODO: UnitConversionTest("1.2rad", 1.2 * Math.PI / 180, STFReader.UNITS.Rotation);
        }

        /* Base units for temperature available in MSTS:
         *     k         Kelvin
         *     c         Celsius
         *     f         Fahrenheit
         */
        [Fact]
        public void UnitConversionBase_MSTS_Temperature()
        {
            // TODO: UnitConversionTest("1.2k", -271.95, STFReader.UNITS.Temperature);
            // TODO: UnitConversionTest("1.2c", 1.2, STFReader.UNITS.Temperature);
            // TODO: UnitConversionTest("1.2f", -17.11111111111111, STFReader.UNITS.Temperature);
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
        public void UnitConversionBase_MSTS_Time()
        {
            // TODO: UnitConversionTest("1.2us", 0.0000012, STFReader.UNITS.Time);
            // TODO: UnitConversionTest("1.2us", 0.0000012, STFReader.UNITS.TimeDefaultM);
            // TODO: UnitConversionTest("1.2us", 0.0000012, STFReader.UNITS.TimeDefaultH);
            // TODO: UnitConversionTest("1.2ms", 0.0012, STFReader.UNITS.Time);
            // TODO: UnitConversionTest("1.2ms", 0.0012, STFReader.UNITS.TimeDefaultM);
            // TODO: UnitConversionTest("1.2ms", 0.0012, STFReader.UNITS.TimeDefaultH);
            UnitConversionTest("1.2s", 1.2, STFReader.UNITS.Time);
            UnitConversionTest("1.2s", 1.2, STFReader.UNITS.TimeDefaultM);
            UnitConversionTest("1.2s", 1.2, STFReader.UNITS.TimeDefaultH);
            // TODO: UnitConversionTest("1.2min", 72, STFReader.UNITS.Time);
            // TODO: UnitConversionTest("1.2min", 72, STFReader.UNITS.TimeDefaultM);
            // TODO: UnitConversionTest("1.2min", 72, STFReader.UNITS.TimeDefaultH);
            UnitConversionTest("1.2h", 4320, STFReader.UNITS.Time);
            UnitConversionTest("1.2h", 4320, STFReader.UNITS.TimeDefaultM);
            UnitConversionTest("1.2h", 4320, STFReader.UNITS.TimeDefaultH);
            // TODO: UnitConversionTest("1.2d", 103680, STFReader.UNITS.Time);
            // TODO: UnitConversionTest("1.2d", 103680, STFReader.UNITS.TimeDefaultM);
            // TODO: UnitConversionTest("1.2d", 103680, STFReader.UNITS.TimeDefaultH);
        }

        /* Base units for velocity available in MSTS:
         *     kmh       Kilometers/hour
         *     mph       Miles/hour
         */
        [Fact]
        public void UnitConversionBase_MSTS_Velocity()
        {
            UnitConversionTest("1.2kmh", 0.3333333333333333, STFReader.UNITS.Speed);
            UnitConversionTest("1.2kmh", 0.3333333333333333, STFReader.UNITS.SpeedDefaultMPH);
            UnitConversionTest("1.2mph", 0.536448, STFReader.UNITS.Speed);
            UnitConversionTest("1.2mph", 0.536448, STFReader.UNITS.SpeedDefaultMPH);
        }

        /* Base units for voltage available in MSTS:
         *     v         Volt
         *     kv        Kilovolt
         *     mv        Megavolt
         */
        [Fact]
        public void UnitConversionBase_MSTS_Voltage()
        {
            UnitConversionTest("1.2V", 1.2, STFReader.UNITS.Voltage);
            // TODO: UnitConversionTest("1.2kV", 1200, STFReader.UNITS.Voltage);
            // TODO: UnitConversionTest("1.2mV", 1200000, STFReader.UNITS.Voltage);
        }

        /* Base units for volume available in MSTS:
         *     l         Liter
         *     gal       Gallon (US)
         */
        [Fact]
        public void UnitConversionBase_MSTS_Volume()
        {
            UnitConversionTest("1.2l", 1.2, STFReader.UNITS.Volume);
            UnitConversionTest("1.2l", 1.2, STFReader.UNITS.VolumeDefaultFT3);
            UnitConversionTest("1.2gal", 4.5424941408, STFReader.UNITS.Volume); // Note: US gallons.
            UnitConversionTest("1.2gal", 4.5424941408, STFReader.UNITS.VolumeDefaultFT3); // Note: US gallons.
        }

        [Fact]
        public void UnitConversionDerived_MSTS_Area()
        {
            // TODO: UnitConversionTest("1.2mm^2", 0.0000012, STFReader.UNITS.AreaDefaultFT2);
            // TODO: UnitConversionTest("1.2cm^2", 0.00012, STFReader.UNITS.AreaDefaultFT2);
            UnitConversionTest("1.2m^2", 1.2, STFReader.UNITS.AreaDefaultFT2);
            // TODO: UnitConversionTest("1.2km^2", 1200000, STFReader.UNITS.AreaDefaultFT2);
            // TODO: UnitConversionTest("1.2\"^2", 0.000774192, STFReader.UNITS.AreaDefaultFT2);
            // TODO: UnitConversionTest("1.2in^2", 0.000774192, STFReader.UNITS.AreaDefaultFT2);
            // TODO: UnitConversionTest("1.2\'^2", 0.111483648, STFReader.UNITS.AreaDefaultFT2);
            UnitConversionTest("1.2ft^2", 0.111483648, STFReader.UNITS.AreaDefaultFT2);
            // TODO: UnitConversionTest("1.2mil^2", 3107985.7324032, STFReader.UNITS.AreaDefaultFT2);
        }

        [Fact]
        public void UnitConversionDerived_MSTS_Damping()
        {
            UnitConversionTest("1.2n/m/s", 1.2, STFReader.UNITS.Resistance);
        }

        [Fact]
        public void UnitConversionDerived_MSTS_MassRate()
        {
            UnitConversionTest("1.2lb/h", 1.2, STFReader.UNITS.MassRateDefaultLBpH);
        }

        [Fact]
        public void UnitConversionDerived_MSTS_Stiffness()
        {
            UnitConversionTest("1.2n/m", 1.2, STFReader.UNITS.Stiffness);
        }

        [Fact]
        public void UnitConversionDerived_MSTS_Velocity()
        {
            UnitConversionTest("1.2m/s", 1.2, STFReader.UNITS.Speed);
            UnitConversionTest("1.2m/s", 1.2, STFReader.UNITS.SpeedDefaultMPH);
            UnitConversionTest("1.2km/h", 0.333333333333333, STFReader.UNITS.Speed);
            UnitConversionTest("1.2km/h", 0.333333333333333, STFReader.UNITS.SpeedDefaultMPH);
        }

        [Fact]
        public void UnitConversionDerived_MSTS_Volume()
        {
            // Note: This unit returns values in litres, not cubic metres.
            // TODO: UnitConversionTest("1.2mm^3", 0.0000000012 * Metre3ToLitres, STFReader.UNITS.Volume);
            // TODO: UnitConversionTest("1.2cm^3", 0.0000012 * Metre3ToLitres, STFReader.UNITS.Volume);
            UnitConversionTest("1.2m^3", 1.2 * Metre3ToLitres, STFReader.UNITS.Volume);
            // TODO: UnitConversionTest("1.2km^3", 1200000000 * Metre3ToLitres, STFReader.UNITS.Volume);
            // TODO: UnitConversionTest("1.2\"^3", 0.0000196644768 * Metre3ToLitres, STFReader.UNITS.Volume);
            UnitConversionTest("1.2in^3", 0.0000196644768 * Metre3ToLitres, STFReader.UNITS.Volume);
            // TODO: UnitConversionTest("1.2\'^3", 0.0339802159104 * Metre3ToLitres, STFReader.UNITS.Volume);
            UnitConversionTest("1.2ft^3", 0.0339802159104 * Metre3ToLitres, STFReader.UNITS.Volume);
            // TODO: UnitConversionTest("1.2mil^3", 5001818190.5286955008 * Metre3ToLitres, STFReader.UNITS.Volume);
        }

        /* Default unit for pressure in MSTS is seconds.
         */
        [Fact]
        public void UnitConversionDefault_MSTS_Pressure()
        {
            UnitConversionTest("1.2", 4062.984 * PascalToPSI, STFReader.UNITS.PressureDefaultInHg);
            UnitConversionTest("1.2", 120000 * PascalToPSI, STFReader.UNITS.PressureDefaultPSI);
        }

        /* Default unit for time in MSTS is seconds.
         */
        [Fact]
        public void UnitConversionDefault_MSTS_Time()
        {
            UnitConversionTest("1.2", 1.2, STFReader.UNITS.Time);
            UnitConversionTest("1.2", 72, STFReader.UNITS.TimeDefaultM);
            UnitConversionTest("1.2", 4320, STFReader.UNITS.TimeDefaultH);
        }

        /* Default unit for velocity in MSTS is UNKNOWN.
         */
        [Fact]
        public void UnitConversionDefault_MSTS_Velocity()
        {
            //UnitConversionTest("1.2", UNKNOWN, STFReader.UNITS.Speed);
            UnitConversionTest("1.2", 0.536448, STFReader.UNITS.SpeedDefaultMPH);
        }

        /* Default unit for volume in MSTS is UNKNOWN.
         */
        [Fact]
        public void UnitConversionDefault_MSTS_Volume()
        {
            // Note: This unit returns values in litres, not cubic metres.
            //UnitConversionTest("1.2", UNKNOWN, STFReader.UNITS.Volume);
            // Note: This unit is broken and returns values in ft^3, not cubic metres.
            UnitConversionTest("1.2", 0.0339802159104 * Metre3ToLitres, STFReader.UNITS.VolumeDefaultFT3);
        }

        /* The following units are currently accepted by Open Rails but have no bearing in MSTS:
         *     amps
         *     btu/lb
         *     degc
         *     degf
         *     g/h
         *     gals
         *     g-uk
         *     g-us
         *     hz
         *     inhg
         *     inhg/s
         *     kj/kg
         *     kpa
         *     kpa/s
         *     kph
         *     ns/m
         *     rpm
         *     rps
         *     t-uk
         *     t-us
         */

        [Fact]
        public void Bug1274713_ParentheticalComment()
        {
            using (var reader = new STFReader(new MemoryStream(Encoding.Unicode.GetBytes(
                "Wagon(\n" +
                "    Lights(\n" +
                "        Light( 1 )\n" +
                "        Light( 2 )\n" +
                "        #(Comment)\n" +
                "    )\n" +
                "    Sound( test.sms )\n" +
                ")")), "", Encoding.Unicode, true))
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

                reader.ReadItem();
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
