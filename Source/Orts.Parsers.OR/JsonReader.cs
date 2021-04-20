// COPYRIGHT 2018 by the Open Rails project.
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

// Use this define to diagnose issues in the JSON reader below.
//#define DEBUG_JSON_READER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Orts.Parsers.OR
{
    public class JsonReader
    {
        /// <summary>
        /// Read the JSON file (check first that it exists) using a method TryParse() which is specific for the expected objects.  
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="tryParse"></param>
        public static void ReadFile(string fileName, Func<JsonReader, bool> tryParse)
        {
            using (var reader = new JsonTextReader(File.OpenText(fileName))
            {
                CloseInput = true,
            })
            {
                new JsonReader(fileName, reader).ReadBlock(tryParse);
            }
        }

        string _fileName;
        JsonTextReader _reader;
        StringBuilder _path;
        Stack<int> _pathPositions;

        /// <summary>
        /// Contains a condensed account of the position of the current item in the JSO, such as when parsing "Clear" from a WeatherFile:
        /// JsonReader item;
        ///   item.Path = "Changes[].Type"
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Note the values needed for parsing and helpful error messages
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="reader"></param>
        JsonReader(string fileName, JsonTextReader reader)
        {
            _fileName = fileName;
            _reader = reader;
            _path = new StringBuilder();
            _pathPositions = new Stack<int>();
        }

        public void ReadBlock(Func<JsonReader, bool> tryParse)
        {
            var basePosition = _pathPositions.Count > 0 ? _pathPositions.Peek() : 0;

#if DEBUG_JSON_READER
            Console.WriteLine();
            Console.WriteLine($"JsonReader({_path.ToString()} ({string.Join(",", _pathPositions.Select(p => p.ToString()).ToArray())})).ReadBlock(): base={basePosition}");
#endif

            while (_reader.Read()) // Reads the next JSON token. Returns false if at end
            {
#if DEBUG_JSON_READER
                Console.WriteLine($"JsonReader.ReadBlock({_path.ToString()} ({string.Join(",", _pathPositions.Select(p => p.ToString()).ToArray())})): token={_reader.TokenType} value={_reader.Value} type={_reader.ValueType}");
#endif
                switch (_reader.TokenType)
                {
                    case JsonToken.StartArray:
                        _pathPositions.Push(_path.Length);
                        _path.Append("[]");
                        break;
                    case JsonToken.EndArray:
                        _path.Length = _pathPositions.Pop();
                        break;
                    case JsonToken.StartObject:
                        _pathPositions.Push(_path.Length);
                        if (_pathPositions.Count > 1) _path.Append(".");
                        _pathPositions.Push(_path.Length);
                        break;
                    case JsonToken.PropertyName:
                        Debug.Assert(_reader.ValueType == typeof(string));
                        _path.Length = _pathPositions.Peek();
                        _path.Append((string)_reader.Value);
                        break;
                    case JsonToken.EndObject:
                        var end = _pathPositions.Pop();
                        _path.Length = _pathPositions.Pop();
                        if (end == basePosition) return;
                        break;
                }

                switch (_reader.TokenType)
                {
                    case JsonToken.StartObject:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:
                    case JsonToken.Date:
                    case JsonToken.Float:
                    case JsonToken.Integer:
                    case JsonToken.Null:
                    case JsonToken.String:
                        Path = _path.ToString().Substring(basePosition);
                        if (!tryParse(this)) TraceInformation($"Skipped unknown {_reader.TokenType} \"{_reader.Value}\" in {Path}");
                        break;
                }
            }
        }

        public T AsEnum<T>(T defaultValue)
        {
            Debug.Assert(typeof(T).IsEnum, "Must use type inheriting from Enum for AsEnum()");
            switch (_reader.TokenType)
            {
                case JsonToken.String:
                    var value = (string)_reader.Value;
                    return (T)Enum.Parse(typeof(T), value, true);
                default:
                    TraceWarning($"Expected string (enum) value in {Path}; got {_reader.TokenType}");
                    return defaultValue;
            }
        }

        public float AsFloat(float defaultValue)
        {
            switch (_reader.TokenType)
            {
                case JsonToken.Float:
                    return (float)(double)_reader.Value;
                case JsonToken.Integer:
                    return (long)_reader.Value;
                default:
                    TraceWarning($"Expected floating point value in {Path}; got {_reader.TokenType}");
                    return defaultValue;
            }
        }

        public int AsInteger(int defaultValue)
        {
            switch (_reader.TokenType)
            {
                case JsonToken.Integer:
                    return (int)(long)_reader.Value;
                default:
                    TraceWarning($"Expected integer value in {Path}; got {_reader.TokenType}");
                    return defaultValue;
            }
        }

        public string AsString(string defaultValue)
        {
            switch (_reader.TokenType)
            {
                case JsonToken.String:
                    return (string)_reader.Value;
                default:
                    TraceWarning($"Expected string value in {Path}; got {_reader.TokenType}");
                    return defaultValue;
            }
        }

        public float AsTime(float defaultValue)
        {
            switch (_reader.TokenType)
            {
                case JsonToken.String:
                    var time = ((string)_reader.Value).Split(':');
                    var StartTime = new TimeSpan(int.Parse(time[0]), time.Length > 1 ? int.Parse(time[1]) : 0, time.Length > 2 ? int.Parse(time[2]) : 0);
                    return (float)StartTime.TotalSeconds;
                default:
                    TraceWarning($"Expected string (time) value in {Path}; got {_reader.TokenType}");
                    return defaultValue;
            }
        }

        public void TraceWarning(string message)
        {
            Trace.TraceWarning("{2} in {0}:line {1}", _fileName, _reader.LineNumber, message);
        }

        public void TraceInformation(string message)
        {
            Trace.TraceInformation("{2} in {0}:line {1}", _fileName, _reader.LineNumber, message);
        }
    }
}
