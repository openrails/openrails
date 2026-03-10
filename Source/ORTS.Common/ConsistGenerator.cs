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
    /// Utility functions to procedurally generate con and eng files to display the Khronos glTF-Sample-Assets for testing purposes.
    /// For this to work 'git clone https://github.com/KhronosGroup/glTF-Sample-Assets.git' to the MSTS/TRAINS/TRAINSET folder,
    /// so that the models will be available in e.g. MSTS/TRAINS/TRAINSET/glTF-Sample-Assets/Models/... folder. Then start like:
    /// RunActivity.exe -start -explorer "C:\Devel\MSTS\ROUTES\USA2\PATHS\tut6path.pat" "glTF-Sample-Assets" 12:00 1 0
    /// RunActivity.exe -start -explorer "C:\Devel\MSTS\ROUTES\USA2\PATHS\tut6path.pat" "glTF-Sample-Assets&AnimatedTriangle" 12:00 1 0
    /// RunActivity.exe -start -explorer "C:\Devel\MSTS\ROUTES\USA2\PATHS\tut6path.pat" "glTF-Sample-Assets#2" 12:00 1 0
    /// </summary>
    public class ConsistGenerator
    {
        /// <summary>
        /// Indicates if the current run is a glTF visual test only. It is possible to generate an on-the-fly consist of all the Khronos test models.
        /// </summary>
        public static bool GltfVisualTestRun;

        const string ConsistTemplateStart =
            @"Train (
                TrainCfg ( ""trainname""
                    Name ( ""trainname"" )
                    MaxVelocity ( 50 1 )
            ";
        const string ConsistTemplateRecord = @"Engine ( UID ( xx ) EngineData ( ""engfile"" ""trainsetdir"" ) )" + "\r\n";
        const string ConsistTemplateEnd = ")\r\n)";
        const string EngineTemplate =
            @"Wagon ( ""enginname""
                Type ( Engine )
                WagonShape ( ""shapefilename"" )
                Size (100m 7m 100m )
                Lights ( 1
                    Light (
                        Type ( 1 )
                        ShapeIndex ( 0 )
                        Conditions ( 
                            Headlight ( 3 )
                            Unit ( 2 )
                        )
                        Cycle ( 0 )
                        FadeIn ( 1 )
                        FadeOut ( 1 )
                        States ( 1
                            State (
                                Duration ( 0.0 )
                                LightColour ( ffffffff )
                                Position ( 0 3.5 0 )
                                Transition ( 0 )
                                Radius ( 400 )
                                Angle ( 15 )
                            )
                        )
                    )
                )
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
            { "glTF-Sample-Assets", "glTF"},
            { "glTF-Sample-Assets-Binary", "glTF-Binary"},
            { "glTF-Sample-Assets-Embedded", "glTF-Embedded"},
            { "glTF-Sample-Assets-Draco", "glTF-Draco"},
            { "glTF-Sample-Assets-Quantized", "glTF-Quantized"},
        };

        static string GltfExtension(string keyword) => keyword.EndsWith("-Binary") ? ".glb" : ".gltf";
        static string RequestedType(string requestedPath) => SubDirectories.FirstOrDefault(ext => ext.Key == Path.GetFileNameWithoutExtension(requestedPath).Split('#', '&').FirstOrDefault()).Key;
        
        public static bool IsConsistRecognized(string requestedPath) => RequestedType(requestedPath) != null;
        public static bool IsWagonRecognized(string requestedPath) => Wagons.ContainsKey(Path.GetFileName(requestedPath));

        /// <summary>
        /// Provides a procedurally generated consist of the requested type of models
        /// </summary>
        /// <param name="requestedPath"></param>
        /// <returns></returns>
        public static Stream GetConsist(string requestedPath)
        {
            var trainsetDir = "glTF-Sample-Assets";
            var baseDir = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(requestedPath)), "TRAINSET", trainsetDir);
            if (!Directory.Exists(baseDir))
            {
                Console.WriteLine($"Cannot find the trainset directory {trainsetDir}.");
                return Stream.Null;
            }

            var keyword = RequestedType(requestedPath);
            Debug.Assert(keyword != null);

            var consist = ConsistTemplateStart.Replace("trainname", keyword);

            if (keyword.StartsWith("glTF"))
            {
                GltfVisualTestRun = true;

                var models = Directory.EnumerateFileSystemEntries(baseDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.Combine(baseDir, "Models")) && (f.EndsWith(".gltf") || f.EndsWith(".glb")))
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Concat(Directory.EnumerateDirectories(Path.Combine(baseDir, "Models")))
                    .Where((m, n) => Path.GetFileName(m) == (Path.GetFileNameWithoutExtension(requestedPath).Split('&').ElementAtOrDefault(1) ?? Path.GetFileName(m)) &&
                        n == (int.TryParse(Path.GetFileNameWithoutExtension(requestedPath).Split('#').ElementAtOrDefault(1), out var requestedModelNumber) ? requestedModelNumber : n));

                var uid = 0;
                foreach (var model in models)
                {
                    if (model.Contains("NodePerformanceTest"))
                        continue;

                    var dir = Path.Combine(model, SubDirectories[keyword]);
                    var file = (Directory.Exists(dir)
                        ? Directory.EnumerateFiles(dir).FirstOrDefault(f => f.EndsWith(GltfExtension(keyword)))
                        : Directory.EnumerateFiles(baseDir).FirstOrDefault(f => f.Contains(model)))
                        ?.Substring(baseDir.Length + 1)
                        ?.Replace(@"\", "/");

                    if (file == null)
                    {
                        // When gltf files got dropped into the sample directory, show that ones too.
                        file = Directory.GetFiles(baseDir, Path.GetFileNameWithoutExtension(model) + "*.gl*", SearchOption.AllDirectories)
                            .Where(f => !f.Contains(Path.Combine(baseDir, "Models")) && (f.EndsWith(".gltf") || f.EndsWith(".glb")))
                            .FirstOrDefault()
                            ?.Substring(baseDir.Length + 1)
                            ?.Replace(@"\", "/");
                        if (file == null)
                            continue;
                    }

                    var eng = $"{keyword}_{Path.GetFileNameWithoutExtension(file)}.eng";
                    Wagons.Add(eng, EngineTemplate
                        .Replace("shapefilename", file)
                        .Replace("enginname", Path.GetFileNameWithoutExtension(file)));

                    consist += ConsistTemplateRecord
                        .Replace("xx", uid.ToString())
                        .Replace("engfile", Path.GetFileNameWithoutExtension(eng))
                        .Replace("trainsetdir", trainsetDir);
                    uid++;
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
