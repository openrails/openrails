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
using System.Text;
using Microsoft.Xna.Framework;
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
            using (var reader = new JsonTextReader(File.OpenText(fileName)))
            {
                new JsonReader(fileName, reader).ReadFile(tryParse);
            }
        }

        /// <summary>
        /// Read the JSON from a string using a method TryParse() which is specific for the expected objects.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="fileName"></param>
        /// <param name="tryParse"></param>
        public static (int Warning, int Information) ReadTest(string content, string fileName, Func<JsonReader, bool> tryParse)
        {
            using (var reader = new JsonTextReader(new StringReader(content)))
            {
                var json = new JsonReader(fileName, reader);
                json.ReadFile(tryParse);
                return (json._countWarnings, json._countInformations);
            }
        }

        string _fileName;
        JsonTextReader _reader;
        StringBuilder _path;
        Stack<int> _pathPositions;
        Stack<string> _paths;
        int _countWarnings;
        int _countInformations;

        string FullPath { get => _path.Length > 0 ? _path.ToString() : "(root)"; }

        /// <summary>
        /// Contains a condensed account of the position of the current item in the JSON, such as when parsing "Clear" from a WeatherFile:
        /// JsonReader item;
        ///   item.Path = "Changes[].Type"
        /// </summary>
        public string Path { get => _paths.Peek(); }

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
            _paths = new Stack<string>();
        }

        void ReadFile(Func<JsonReader, bool> tryParse)
        {
            try
            {
                ReadBlock(tryParse);
                // Read the rest of the file so that we catch any extra data, which might be in error
                while (_reader.Read()) ;
            }
            catch (JsonReaderException error)
            {
                // Newtonsoft.Json unfortunately includes extra information in the message we already provide
                var jsonMessage = error.Message.Split(new[] { ". Path '" }, StringSplitOptions.None);
                TraceWarning($"{jsonMessage[0]} in {FullPath}");
            }
        }

        /// <summary>
        /// Reads next token and stores in _reader.TokenType, _reader.ValueType, _reader.Value
        /// Throws exception if value not as expected.
        /// PropertyNames are case-sensitive.
        /// </summary>
        /// <param name="tryParse"></param>
        public void ReadBlock(Func<JsonReader, bool> tryParse)
        {
            var basePosition = _pathPositions.Count > 0 ? _pathPositions.Peek() : 0;

#if DEBUG_JSON_READER
            Console.WriteLine($"JsonReader({basePosition} / {_path} / {String.Join(" ", _pathPositions)}).ReadBlock()");
#endif

            while (_reader.Read()) // Reads the next JSON token. Returns false if at end
            {
#if DEBUG_JSON_READER
                Console.Write($"JsonReader({basePosition} / {_path} / {String.Join(" ", _pathPositions)}) --> ");
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
                        _pathPositions.Pop();
                        _path.Length = _pathPositions.Pop();
                        break;
                }
#if DEBUG_JSON_READER
                Console.WriteLine($"({basePosition} / {_path} / {string.Join(" ", _pathPositions)}) token={_reader.TokenType} value={_reader.Value} type={_reader.ValueType}");
#endif
                if (_path.Length <= basePosition && (_reader.TokenType == JsonToken.EndArray || _reader.TokenType == JsonToken.EndObject)) return;

                switch (_reader.TokenType)
                {
                    case JsonToken.StartObject:
                    case JsonToken.StartArray:
                    case JsonToken.Boolean:
                    case JsonToken.Bytes:
                    case JsonToken.Date:
                    case JsonToken.Float:
                    case JsonToken.Integer:
                    case JsonToken.Null:
                    case JsonToken.String:
                        _paths.Push(_path.ToString().Substring(basePosition));
                        if (!tryParse(this)) TraceInformation($"Skipped unknown {_reader.TokenType} \"{_reader.Value}\" in {FullPath}");
                        _paths.Pop();
                        break;
                }
            }

            TraceWarning($"Unexpected end of file in {FullPath}");
        }

        public bool TryRead<T>(Func<JsonReader, T> read, out T output)
        {
            var warnings = _countWarnings;
            output = read(this);
            return warnings == _countWarnings;
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
                    TraceWarning($"Expected string (enum) value in {FullPath}; got {_reader.TokenType}");
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
                    TraceWarning($"Expected floating point value in {FullPath}; got {_reader.TokenType}");
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
                    TraceWarning($"Expected integer value in {FullPath}; got {_reader.TokenType}");
                    return defaultValue;
            }
        }

        public bool AsBoolean(bool defaultValue)
        {
            switch (_reader.TokenType)
            {
                case JsonToken.Boolean:
                    return (bool)_reader.Value;
                default:
                    TraceWarning($"Expected Boolean value in {FullPath}; got {_reader.TokenType}");
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
                    TraceWarning($"Expected string value in {FullPath}; got {_reader.TokenType}");
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
                    TraceWarning($"Expected string (time) value in {FullPath}; got {_reader.TokenType}");
                    return defaultValue;
            }
        }

        public Vector3 AsVector3(Vector3 defaultValue)
        {
            switch (_reader.TokenType)
            {
                case JsonToken.StartArray:
                    if (TryRead(json =>
                    {
                        var floats = new List<float>(3);
                        ReadBlock(item =>
                        {
                            floats.Add(item.AsFloat(0));
                            return true;
                        });
                        return floats;
                    }, out var vector))
                    {
                        if (vector.Count == 3) return new Vector3(vector[0], vector[1], vector[2]);
                        TraceWarning($"Expected 3 float array (Vector3) value in {FullPath}; got {vector.Count} float array");
                    }
                    return defaultValue;
                default:
                    TraceWarning($"Expected array (Vector3) value in {FullPath}; got {_reader.TokenType}");
                    return defaultValue;
            }
        }

        public void TraceWarning(string message)
        {
            Trace.TraceWarning("{2} in {0}:line {1}", _fileName, _reader.LineNumber, message);
            _countWarnings++;
        }

        public void TraceInformation(string message)
        {
            Trace.TraceInformation("{2} in {0}:line {1}", _fileName, _reader.LineNumber, message);
            _countInformations++;
        }
    }
}
