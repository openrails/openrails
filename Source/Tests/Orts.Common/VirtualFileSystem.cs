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
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Tests.Orts.Common
{
    public class VirtualFileSystem
    {
        [Theory]
        [InlineData("/")]
        [InlineData(@"\")]
        [InlineData("/MSTS")]
        [InlineData("//MSTS//")]
        [InlineData(@"\MSTS\")]
        [InlineData(@"\\MSTS\\")]
        [InlineData(Vfs.MstsBasePath)]
        [InlineData(Vfs.ExecutablePath)]
        public static void DirectoryExists(string dir)
        {
            using (var testFile = new TestFile("abc"))
            {
                AssertWarnings.Expected();
                Assert.True(Vfs.Initialize(Path.GetDirectoryName(testFile.FileName), AppDomain.CurrentDomain.BaseDirectory));
                Assert.True(Vfs.DirectoryExists(dir));
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public static void DirectoryExistsFalse(string dir)
        {
            using (var testFile = new TestFile("abc"))
            {
                AssertWarnings.Expected();
                Assert.True(Vfs.Initialize(Path.GetDirectoryName(testFile.FileName), null));
                Assert.False(Vfs.DirectoryExists(dir));
            }
        }

        [Theory]
        [InlineData("/MSTS/")]
        [InlineData("//MSTS//")]
        [InlineData(@"\MSTS\")]
        [InlineData(@"\\MSTS\\")]
        [InlineData("/MSTS/qwerty uiop/../")]
        [InlineData(@"\\\\\\\\\MSTS\\\\\\\\qwerty uiop\\\\\\\\..\\\\\\\\")]
        [InlineData(@"\MSTS\asdfgéáűő 5tre/,#.\..\.\..\")]
        public static void FileExists(string file)
        {
            using (var testFile = new TestFile("abc"))
            {
                AssertWarnings.Expected();
                Assert.True(Vfs.Initialize(Path.GetDirectoryName(testFile.FileName), null));
                Assert.True(Vfs.FileExists($"{file}{Path.GetFileName(testFile.FileName)}"));
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(@"\")]
        [InlineData("/")]
        [InlineData("/MSTS")]
        public static void FileExistsFalse(string file)
        {
            using (var testFile = new TestFile("abc"))
            {
                AssertWarnings.Expected();
                Assert.True(Vfs.Initialize(Path.GetDirectoryName(testFile.FileName), null));
                Assert.False(Vfs.FileExists(file));
            }
        }

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
                Assert.True(Vfs.Initialize(directory, null));

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

        [Fact]
        public static void ZipFiles()
        {
            using (var file = new TestFile("", ".zip"))
            {
                var fileName = Path.GetFileName(file.FileName);
                var directory = Path.GetDirectoryName(file.FileName);
                var contents = "Árvíztűrő tükörfúrógép";
                var dir1 = @"1stdir";
                var dir2 = @"2nddir";
                string file1Name, file2Name;

                using (var file1 = new TestFile(contents))
                using (var file2 = new TestFile(contents))
                using (var archive = ZipFile.Open(file.FileName, ZipArchiveMode.Update))
                {
                    file1Name = Path.GetFileName(file1.FileName);
                    file2Name = Path.GetFileName(file2.FileName);
                    archive.CreateEntryFromFile(file1.FileName, $@"{dir1}\{dir2}\{file1Name}");
                    archive.CreateEntryFromFile(file2.FileName, $@"{dir1}\{dir2}\{file2Name}");
                }

                Vfs.AutoMount = true;

                AssertWarnings.Expected();
                Assert.True(Vfs.Initialize(directory, null));

                Assert.True(Vfs.DirectoryExists($"/MSTS/{dir1}/"));
                Assert.True(Vfs.DirectoryExists($"/MSTS/{dir1}/{dir2}"));
                Assert.True(Vfs.FileExists($"/MSTS/{dir1}/{dir2}/{file1Name}"));

                using (var archive = ZipFile.Open(file.FileName, ZipArchiveMode.Read))
                    Assert.Equal(archive.GetEntry($@"{dir1}\{dir2}\{file1Name}").LastWriteTime.DateTime, Vfs.GetLastWriteTime($"/MSTS/{dir1}/{dir2}/{file1Name}"));
                using (var stream = Vfs.OpenText($"/MSTS/{dir1}/{dir2}/{file2Name}"))
                    Assert.Equal(contents, stream.ReadToEnd());
                using (var reader = Vfs.StreamReader($"/MSTS/{dir1}/{dir2}/{file1Name}", Encoding.UTF8))
                    Assert.Equal(contents, reader.ReadToEnd());
                using (var reader = Vfs.StreamReader($"/MSTS/{dir1}/{dir2}/{file1Name}", Encoding.ASCII))
                    Assert.NotEqual(contents, reader.ReadToEnd());

                Assert.DoesNotContain($"/MSTS/{dir1}/{dir2}/{file2Name}".ToUpper(), Vfs.GetFiles("/MSTS/", "*.*"));
                Assert.Contains($"/MSTS/{dir1}/{dir2}/{file2Name}".ToUpper(), Vfs.GetFiles("/MSTS/", "*.*", SearchOption.AllDirectories));

                Assert.DoesNotContain($"/MSTS/{dir1}/{dir2}".ToUpper(), Vfs.GetDirectories("/MSTS/"));
                Assert.Contains($"/MSTS/{dir1}/{dir2}".ToUpper(), Vfs.GetDirectories(@"\MSTS/", SearchOption.AllDirectories));

                Assert.Throws<FileNotFoundException>(() => Vfs.FileDelete($"/MSTS/{dir1}/{dir2}/{file2Name}"));
                Assert.Throws<DirectoryNotFoundException>(() => Vfs.OpenCreate($"/MSTS/{dir1}/{dir2}/somefile"));
            }
        }

        readonly static Func<string, string, string> JsonEntry = (source, mountpoint) => $@"{{ ""vfsEntries"": [ {{ ""source"": ""{source}"", ""mountPoint"":  ""{mountpoint}"" }} ] }}";

        [Theory]
        [InlineData(@"C:\\My Routes\\USA85\\", "/MSTS/ROUTES/USA85/")]
        [InlineData(@"C:\\TEMP\\MSTS1.2.zip", "/MSTS/")]
        [InlineData(@"C:\\routes.zip\\USA3\\", "/msts/routes/usa3/")]
        public static void ConfigFileParse(string source, string mountpoint)
        {
            using (var file = new TestFile(JsonEntry(source, mountpoint)))
            {
                mountpoint = mountpoint.ToUpper();
                AssertWarnings.Matching($"{source} => {mountpoint}", () => Assert.False(Vfs.Initialize(file.FileName, null)));
            }
        }

        [Theory]
        [InlineData(@"C:\\TEMP\\MSTS1.2.zip", "/MSTS/ROUTES")] // The mount point must end with a slash
        [InlineData(@"A", "/MSTS/ROUTES")] // The mount point must end with a slash
        public static void ConfigFileParseReject(string source, string mountpoint)
        {
            using (var file = new TestFile(JsonEntry(source, mountpoint)))
            {
                AssertWarnings.Matching($"slash", () => Assert.False(Vfs.Initialize(file.FileName, null)));
            }
        }
    }
}
