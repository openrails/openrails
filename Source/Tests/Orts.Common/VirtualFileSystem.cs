// COPYRIGHT 2021 by the Open Rails project.
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

using ORTS.Common;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Tests.Orts.Common
{
    public class VirtualFileSystem
    {
        [Fact]
        public static void SystemFiles()
        {
            var contents = "Árvíztűrő tükörfúrógép";
            using (var file = new TestFile(contents))
            {
                var fileName = Path.GetFileName(file.FileName);
                var directory = Path.GetDirectoryName(file.FileName);
                var testDir = $"{fileName}.testdir";
                Directory.CreateDirectory(Path.Combine(directory, testDir));
                
                AssertWarnings.Expected();
                Vfs.Initialize(directory, AppDomain.CurrentDomain.BaseDirectory);

                Assert.False(Vfs.DirectoryExists(null));
                Assert.False(Vfs.DirectoryExists(""));
                Assert.True(Vfs.DirectoryExists("/"));
                Assert.True(Vfs.DirectoryExists(@"\"));
                Assert.True(Vfs.DirectoryExists("/MSTS"));
                Assert.True(Vfs.DirectoryExists(@"\MSTS\"));
                Assert.True(Vfs.DirectoryExists(Vfs.MstsBasePath));
                Assert.True(Vfs.DirectoryExists(Vfs.ExecutablePath));

                Assert.False(Vfs.FileExists(null));
                Assert.False(Vfs.FileExists(""));
                Assert.False(Vfs.FileExists("/"));
                Assert.False(Vfs.FileExists(@"\"));
                Assert.True(Vfs.FileExists($"/MSTS/{fileName}"));
                Assert.True(Vfs.FileExists($@"\MSTS\{fileName}"));
                Assert.True(Vfs.FileExists($"/MSTS/qwerty uiop/../{fileName}"));
                Assert.True(Vfs.FileExists($@"\MSTS\asdfgéáűő 5tre/,#.\..\.\..\{fileName}"));

                Assert.Equal(File.GetLastWriteTime(file.FileName), Vfs.GetLastWriteTime($"/MSTS/{fileName}"));

                using (var stream = Vfs.OpenText($"/MSTS/{fileName}"))
                    Assert.Equal(contents, stream.ReadToEnd());
                using (var reader = Vfs.StreamReader($"/MSTS/{fileName}", Encoding.UTF8))
                    Assert.Equal(contents, reader.ReadToEnd());
                using (var reader = Vfs.StreamReader($"/MSTS/{fileName}", Encoding.ASCII))
                    Assert.NotEqual(contents, reader.ReadToEnd());

                var testFile = $"/MSTS/{fileName}.test";
                using (var writer = new StreamWriter(Vfs.OpenCreate(testFile)))
                    writer.Write(contents);
                Assert.True(Vfs.FileExists(testFile));

                Assert.Contains(testFile.ToUpper(), Vfs.GetFiles("/MSTS/", "*.test"));
                Assert.DoesNotContain(testFile.ToUpper(), Vfs.GetDirectories("/MSTS/"));

                Vfs.FileDelete(testFile);
                Assert.False(Vfs.FileExists(testFile));

                Assert.Contains($"/MSTS/{testDir}".ToUpper(), Vfs.GetDirectories("/MSTS/"));
                Directory.Delete(Path.Combine(directory, testDir));
            }
        }

        [Theory]
        [InlineData(@"""C:\My Routes\USA85\""", "/MSTS/ROUTES/USA85/", "#comment")]
        [InlineData(@"C:\TEMP\MSTS1.2.zip", "/MSTS/", "")]
        [InlineData(@"C:\routes.zip\USA3\", "/msts/routes/usa3/", "# this is a comment")]
        public static void ConfigFileParse(string path, string mountpoint, string comment)
        {
            using (var file = new TestFile($"{path} {mountpoint} {comment}"))
            {
                path = path.Replace(@"\", @"\\").Replace(@"""", "");
                mountpoint = mountpoint.ToUpper();
                AssertWarnings.Matching($"{path} => {mountpoint}", () => Vfs.Initialize(file.FileName, null));
            }
        }

        [Theory]
        [InlineData(@"C:\My Routes\USA85\", "/MSTS/ROUTES/USA85/", "# source path with spaces must be quoted")]
        public static void ConfigFileParseFail1(string path, string mountpoint, string comment)
        {
            using (var file = new TestFile($"{path} {mountpoint} {comment}"))
            {
                AssertWarnings.Matching($"Cannot parse", () => Vfs.Initialize(file.FileName, null));
            }
        }

        [Theory]
        [InlineData(@"C:\TEMP\MSTS1.2.zip", "/MSTS/ROUTES", "# mount point must end with slash")]
        [InlineData(@"A", "/MSTS/ROUTES", "# mount point must end with slash")]
        public static void ConfigFileParseFail2(string path, string mountpoint, string comment)
        {
            using (var file = new TestFile($"{path} {mountpoint} {comment}"))
            {
                AssertWarnings.Matching($"slash", () => Vfs.Initialize(file.FileName, null));
            }
        }
    }
}
