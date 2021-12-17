// COPYRIGHT 2015 by the Open Rails project.
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

using Orts.Parsers.OR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Tests.Orts.Parsers.OR
{
    public class TimetableReaderTests
    {
        [Fact]
        public static void DetectSeparator()
        {
            using (var file = new TestFile(";"))
            {
                var tr = new TimetableReader(file.FileName);
                Assert.Single(tr.Strings);
                Assert.Equal(2, tr.Strings[0].Length);
            }
            using (var file = new TestFile(","))
            {
                var tr = new TimetableReader(file.FileName);
                Assert.Single(tr.Strings);
                Assert.Equal(2, tr.Strings[0].Length);
            }
            using (var file = new TestFile("\t"))
            {
                var tr = new TimetableReader(file.FileName);
                Assert.Single(tr.Strings);
                Assert.Equal(2, tr.Strings[0].Length);
            }
            using (var file = new TestFile(":"))
            {
                Assert.Throws<InvalidDataException>(() => {
                    var tr = new TimetableReader(file.FileName);
                });
            }
        }

        [Fact]
        public static void ParseStructure()
        {
            using (var file = new TestFile(";b;c;d\n1;2;3\nA;B;C;D;E"))
            {
                var tr = new TimetableReader(file.FileName);
                Assert.Equal(3, tr.Strings.Count);
                Assert.Equal(4, tr.Strings[0].Length);
                Assert.Equal(3, tr.Strings[1].Length);
                Assert.Equal(5, tr.Strings[2].Length);
                Assert.Equal(new[] { "", "b", "c", "d" }, tr.Strings[0]);
                Assert.Equal(new[] { "1", "2", "3" }, tr.Strings[1]);
                Assert.Equal(new[] { "A", "B", "C", "D", "E" }, tr.Strings[2]);
            }
        }
    }
}
