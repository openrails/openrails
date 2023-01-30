// COPYRIGHT 2023 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ORTS.Common
{
    /// <summary>
    /// Utility functions to procedurally generate con and eng files to e.g. display the Khronos glTF-Sample-Models for testing purposes.
    /// </summary>
    public class ConsistGenerator
    {
        public static bool GeneratedRun;

        const string ConsistTemplateStart = @"Train (
            TrainCfg ( ""trainname""
                Name ( ""trainname"" )
                MaxVelocity ( 50 1 )
            ";
        const string ConsistTemplateRecord = @"Engine ( UID ( xx ) EngineData ( ""engfile"" ""trainsetdir"" ) )" + "\r\n";
        const string ConsistTemplateEnd = ")\r\n)";
        const string EngineTemplate = @"Wagon ( ""enginname""
                Type ( Engine )
                WagonShape ( ""shapefilename"" )
                Size (100m 100m 100m )
            )
            Engine ( ""enginname""
                Wagon ( ""enginname"" )
                Type ( Electric )
                CabView ( dummy.cvf )
                Name ( ""enginname"" )
            )";

        static readonly Dictionary<string, string> Wagons = new Dictionary<string, string>();
        static readonly Dictionary<string, string> SubDirectories = new Dictionary<string, string>
        {
            { "glTF-Sample-Models", "glTF"},
            { "glTF-Sample-Models-Binary", "glTF-Binary"},
            { "glTF-Sample-Models-Embedded", "glTF-Embedded"},
            { "glTF-Sample-Models-Draco", "glTF-Draco"},
            { "glTF-Sample-Models-Quantized", "glTF-Quantized"},
        };

        static string GltfExtension(string keyword) => keyword == "glTF-Sample-Models-Binary" ? ".glb" : ".gltf";
        static string RequestedType(string requestedPath) => SubDirectories.FirstOrDefault(ext => ext.Key == Path.GetFileNameWithoutExtension(requestedPath).Split('#').FirstOrDefault()).Key;
        
        public static bool IsConsistRecognized(string requestedPath) => RequestedType(requestedPath) != null;
        public static bool IsWagonRecognized(string requestedPath) => Wagons.ContainsKey(Path.GetFileName(requestedPath));

        /// <summary>
        /// Provides a procedurally generated consist of the requested type of models
        /// </summary>
        /// <param name="requestedPath"></param>
        /// <returns></returns>
        public static Stream GetConsist(string requestedPath)
        {
            var trainsetDir = "glTF-Sample-Models";
            var baseDir = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(requestedPath)), "TRAINSET", trainsetDir);
            if (!Directory.Exists(baseDir))
            {
                Console.WriteLine($"Cannot find the trainset directory {trainsetDir}.");
                return Stream.Null;
            }

            var keyword = RequestedType(requestedPath);
            Debug.Assert(keyword != null);

            GeneratedRun = true;

            var consist = ConsistTemplateStart.Replace("trainname", keyword);

            if (keyword.StartsWith("glTF"))
            {
                var requestedModel = Path.GetFileNameWithoutExtension(requestedPath).Split('#').ElementAtOrDefault(1);
                var models = requestedModel == null ? Directory.EnumerateDirectories(Path.Combine(baseDir, "2.0")) : new List<string>() { Path.Combine(baseDir, "2.0", requestedModel) };

                var i = 0;
                foreach (var model in models)
                {
                    var dir = Path.Combine(model, SubDirectories[keyword]);
                    if (Directory.Exists(dir))
                    {
                        var file = Directory.EnumerateFiles(dir).FirstOrDefault(f => f.EndsWith(GltfExtension(keyword)));
                        if (file != null)
                        {
                            var f = file.Substring(baseDir.Length + 1);

                            var eng = $"{keyword}_{Path.GetFileNameWithoutExtension(f)}.eng";
                            Wagons.Add(eng, EngineTemplate
                                .Replace("shapefilename", f.Replace(@"\", "/"))
                                .Replace("enginname", Path.GetFileNameWithoutExtension(f)));

                            consist += ConsistTemplateRecord
                                .Replace("xx", i.ToString())
                                .Replace("engfile", Path.GetFileNameWithoutExtension(eng))
                                .Replace("trainsetdir", trainsetDir);
                            i++;
                        }
                    }
                }
            }
            consist += ConsistTemplateEnd;

            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, Encoding.UTF8.GetByteCount(consist), true))
            {
                writer.Write(consist);
                writer.Flush();
            }
            stream.Position = 0;
            return stream;
        }

        public static Stream GetWagon(string requestedEngine)
        {
            if (!Wagons.ContainsKey(Path.GetFileName(requestedEngine)))
            {
                Console.WriteLine($"Cannot find the requested procedurally generated engine {requestedEngine}.");
                return Stream.Null;
            }

            var wagon = Wagons[Path.GetFileName(requestedEngine)];
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, Encoding.UTF8, Encoding.UTF8.GetByteCount(wagon), true))
            {
                writer.Write(wagon);
                writer.Flush();
            }
            stream.Position = 0;
            return stream;
        }
    }
}
