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
using MSTS;
using Xunit;

namespace Tests
{
    public class STF
    {
        [Fact]
        public void EmptyFile()
        {
            using (var reader = new STFReader(new MemoryStream("".ToCharArray().Cast<byte>().ToArray()), "HeaderUTF16.stf", Encoding.ASCII, false))
            {
                Assert.True(reader.Eof, "STFReader.Eof");
                Assert.True(reader.EOF(), "STFReader.EOF()");
                Assert.True(reader.EndOfBlock(), "STFReader.EndOfBlock()");
                Assert.Equal("HeaderUTF16.stf", reader.FileName);
                Assert.Equal(1, reader.LineNumber);
                Assert.Equal(null, reader.SimisSignature);
                //Assert.Throws<Xunit.Sdk.TraceAssertException>(() => reader.Tree); // There seems to be issues with this when debugging...

                // All of the following will execute successfully at EOF...
                reader.MustMatch("ANYTHING GOES");
                reader.ParseBlock(new STFReader.TokenProcessor[0]);
                reader.ParseFile(new STFReader.TokenProcessor[0]);
                Assert.Equal(-1, reader.PeekPastWhitespace());
                Assert.Equal(false, reader.ReadBoolBlock(false));
                Assert.Equal(0, reader.ReadDouble(null));
                Assert.Equal(0, reader.ReadDoubleBlock(null));
                Assert.Equal(0, reader.ReadFloat(STFReader.UNITS.Any, null));
                Assert.Equal(0, reader.ReadFloatBlock(STFReader.UNITS.Any, null));
                Assert.Equal(0U, reader.ReadHex(null));
                Assert.Equal(0U, reader.ReadHexBlock(null));
                Assert.Equal(0, reader.ReadInt(null));
                Assert.Equal(0, reader.ReadIntBlock(null));
                Assert.Equal("", reader.ReadItem());
                Assert.Equal("", reader.ReadString());
                Assert.Equal(null, reader.ReadStringBlock(null));
                Assert.Equal(0U, reader.ReadUInt(null));
                Assert.Equal(0U, reader.ReadUIntBlock(null));
                Assert.Equal(Vector3.Zero, reader.ReadVector3Block(STFReader.UNITS.Any, Vector3.Zero));
                Assert.Equal(Vector4.Zero, reader.ReadVector4Block(STFReader.UNITS.Any, Vector4.Zero));
            }
        }
    }
}
