// COPYRIGHT 2022 by the Open Rails project.
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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Orts.Parsers.OR;
using Xunit;

namespace Tests.Orts.Parsers.OR
{
    // TODO: This class is a temporary fix until `AssertWarnings` is removed
    public class JsonReaderTestsSetup
    {
        public JsonReaderTestsSetup()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.AutoFlush = true;
        }
    }

    public class JsonReaderTests : IClassFixture<JsonReaderTestsSetup>
    {
        static void ReadJsonText(string text, Func<JsonReader, bool> tryParse, int expectedWarning = 0, int expectedInformation = 0, [CallerMemberName] string memberName = "")
        {
            var result = JsonReader.ReadTest(text, $"{memberName}.json", tryParse);
            Assert.True(expectedWarning == result.Warning, $"Expected {expectedWarning} warning log messages; got {result.Warning}");
            Assert.True(expectedInformation == result.Information, $"Expected {expectedInformation} information log messages; got {result.Information}");
        }

        [Fact]
        public static void FileTooShort()
        {
            ReadJsonText("{ ", item => true, expectedWarning: 1);
            ReadJsonText("[ ", item => true, expectedWarning: 1);
        }

        [Fact]
        public static void FileTooLong()
        {
            ReadJsonText("{ } }", item => true, expectedWarning: 1);
            ReadJsonText("[ ] ]", item => true, expectedWarning: 1);
        }

        [Fact]
        public static void FileJustRight()
        {
            ReadJsonText("{ }", item => true);
            ReadJsonText("[ ]", item => true);
        }

        [Fact]
        public static void FileTruncated()
        {
            ReadJsonText(@"{ ""foo ", item => true, expectedWarning: 1);
            ReadJsonText(@"{ ""foo"": ", item => true, expectedWarning: 1);
            ReadJsonText(@"{ ""foo"": tr ", item => true, expectedWarning: 1);
            ReadJsonText(@"{ ""foo"": true, ", item => true, expectedWarning: 1);
            ReadJsonText(@"{ ""foo"": { ", item => true, expectedWarning: 1);
        }

        [Fact]
        public static void ParseObjectProperties()
        {
            ReadJsonText(@"{
                ""bool"": true,
                ""enum"": ""oRdInAl"",
                ""float"": 1.111,
                ""integer"": 1,
                ""string"": ""1"",
                ""time"": ""12:34:56"",
                ""vector3"": [1.1, 2.2, 3.3],
                ""object1"": {
                    ""a"": 1.111,
                    ""b"": 1,
                    ""c"": ""1"",
                },
                ""object2"": {
                    ""a"": 1.111,
                    ""b"": 1,
                    ""c"": ""1"",
                },
            }", item =>
            {
                switch (item.Path)
                {
                    case "":
                        // Root object, AOK
                        break;
                    case "bool":
                        Assert.True(item.AsBoolean(false));
                        break;
                    case "enum":
                        Assert.Equal(StringComparison.Ordinal, item.AsEnum<StringComparison>(StringComparison.CurrentCulture));
                        break;
                    case "float":
                        Assert.Equal(1.111f, item.AsFloat(0));
                        break;
                    case "integer":
                        Assert.Equal(1, item.AsInteger(0));
                        break;
                    case "string":
                        Assert.Equal("1", item.AsString(""));
                        break;
                    case "time":
                        Assert.Equal(new TimeSpan(12, 34, 56).TotalSeconds, item.AsTime(0));
                        break;
                    case "vector3[]":
                        Assert.Equal(new Vector3(1.1f, 2.2f, 3.3f), item.AsVector3(Vector3.Zero));
                        break;
                    case "object1.":
                    case "object2.":
                        item.ReadBlock(item2 =>
                        {
                            switch (item2.Path)
                            {
                                case "a":
                                    Assert.Equal(1.111f, item.AsFloat(0));
                                    break;
                                case "b":
                                    Assert.Equal(1, item.AsInteger(0));
                                    break;
                                case "c":
                                    Assert.Equal("1", item.AsString(""));
                                    break;
                                default: return false;
                            }
                            return true;
                        });
                        break;
                    default: return false;
                }
                return true;
            });
        }

        [Fact]
        public static void ParseArrayItems()
        {
            ReadJsonText(@"[
                {
                    ""a"": 1.111,
                    ""b"": 1,
                    ""c"": ""1"",
                },
                {
                    ""a"": 1.111,
                    ""b"": 1,
                    ""c"": ""1"",
                },
            ]", item =>
            {
                switch (item.Path)
                {
                    case "[]":
                        // Root array, AOK
                        break;
                    case "[].":
                        item.ReadBlock(item2 =>
                        {
                            switch (item2.Path)
                            {
                                case "":
                                    break;
                                case "a":
                                    Assert.Equal(1.111f, item.AsFloat(0));
                                    break;
                                case "b":
                                    Assert.Equal(1, item.AsInteger(0));
                                    break;
                                case "c":
                                    Assert.Equal("1", item.AsString(""));
                                    break;
                                default: return false;
                            }
                            return true;
                        });
                        break;
                    default: return false;
                }
                return true;
            });
        }

        [Fact]
        public static void ParseInvalidVector3()
        {
            ReadJsonText(@"{
                ""a"": [1.1, 2.2, 3.3],
                ""b"": [1, 2, 3],
                ""1"": [1.1, 2.2, 3.3, 4.4],
                ""2"": [1.1, 2.2],
                ""3"": [1.1],
                ""4"": [],
                ""5"": [1, 2, {}],
                ""6"": [1, 2, []],
                ""7"": [1, 2, true],
                ""8"": {},
                ""9"": 0,
                ""z"": [1.1, 2.2, 3.3],
            }", item =>
            {
                switch (item.Path)
                {
                    case "":
                        // Root object, AOK
                        break;
                    case "a[]":
                    case "z[]":
                        Assert.Equal(new Vector3(1.1f, 2.2f, 3.3f), item.AsVector3(Vector3.Zero));
                        break;
                    case "b[]":
                        Assert.Equal(new Vector3(1f, 2f, 3f), item.AsVector3(Vector3.Zero));
                        break;
                    case "1[]":
                    case "2[]":
                    case "3[]":
                    case "4[]":
                    case "5[]":
                    case "6[]":
                    case "7[]":
                        Assert.Equal(Vector3.Zero, item.AsVector3(Vector3.Zero));
                        break;
                    default: return false;
                }
                return true;
            }, expectedWarning: 7, expectedInformation: 2);
        }

        [Fact]
        public static void TryRead()
        {
            ReadJsonText(@"{
                ""a"": {
                    ""field"": 1,
                },
                ""b"": {
                    ""field"": false,
                },
            }", item =>
            {
                switch (item.Path)
                {
                    case "a.":
                        Assert.True(item.TryRead(TryReadObject, out var _));
                        break;
                    case "b.":
                        Assert.False(item.TryRead(TryReadObject, out var _));
                        break;
                }
                return true;
            }, expectedWarning: 1);
        }

        static int TryReadObject(JsonReader json)
        {
            json.ReadBlock(item =>
            {
                switch (item.Path)
                {
                    case "field":
                        item.AsInteger(0);
                        break;
                    default: return false;
                }
                return true;
            });
            return 42;
        }
    }
}
