// COPYRIGHT 2016 by the Open Rails project.
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

using Orts.Formats.Msts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Orts.DataConverter
{
    class TerrainConverter : IDataConverter
    {
        public TerrainConverter()
        {
        }

        public void ShowConversions()
        {
            //                "1234567890123456789012345678901234567890123456789012345678901234567890123456789"
            //                "               Input  Output              Description"
            Console.WriteLine("                 *.t  *.dae               Creates a set of COLLADA files for the tile's terrain.");
            Console.WriteLine("                 *.w  *.dae               Creates a set of COLLADA files for the tile's terrain.");
        }

        public bool DoConversion(DataConversion conversion)
        {
            // We can convert from .t or .w files.
            if (Path.GetExtension(conversion.Input) != ".t" && Path.GetExtension(conversion.Input) != ".w")
            {
                return false;
            }
            // We can convert to .dae files.
            if (conversion.Output.Any(output => Path.GetExtension(output) != ".dae"))
            {
                return false;
            }
            if (!File.Exists(conversion.Input))
            {
                throw new FileNotFoundException("", conversion.Input);
            }

            if (Path.GetExtension(conversion.Input) == ".w")
            {
                // Convert from world file to tile file, by parsing the X, Z coordinates from filename.
                var filename = Path.GetFileNameWithoutExtension(conversion.Input);
                int tileX, tileZ;
                if (filename.Length != 15 ||
                    filename[0] != 'w' ||
                    (filename[1] != '+' && filename[1] != '-') ||
                    (filename[8] != '+' && filename[8] != '-') ||
                    !int.TryParse(filename.Substring(1, 7), out tileX) ||
                    !int.TryParse(filename.Substring(8, 7), out tileZ))
                {
                    throw new InvalidCommandLineException("Unable to parse tile coordinates from world filename: " + filename);
                }
                var tilesDirectory = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(conversion.Input)), "Tiles");
                var tileName = TileName.FromTileXZ(tileX, tileZ, TileName.Zoom.Small);
                conversion.SetInput(Path.Combine(tilesDirectory, tileName + ".t"));
            }

            var baseFileName = Path.Combine(Path.GetDirectoryName(conversion.Input), Path.GetFileNameWithoutExtension(conversion.Input));

            var tFile = new TerrainFile(baseFileName + ".t");

            var sampleCount = tFile.terrain.terrain_samples.terrain_nsamples;
            var yFile = new TerrainAltitudeFile(baseFileName + "_y.raw", sampleCount);
            TerrainFlagsFile fFile;
            if (File.Exists(baseFileName + "_f.raw"))
            {
                fFile = new TerrainFlagsFile(baseFileName + "_f.raw", sampleCount);
            }

            var patchCount = tFile.terrain.terrain_patchsets[0].terrain_patchset_npatches;
            for (var x = 0; x < patchCount; x++)
            {
                for (var z = 0; z < patchCount; z++)
                {
                    var patch = new TerrainConverterPatch(tFile, yFile, x, z);

                    XNamespace cNS = "http://www.collada.org/2005/11/COLLADASchema";
                    var colladaDocument = new XDocument(
                        new XDeclaration("1.0", "UTF-8", "false"),
                        new XElement(cNS + "COLLADA",
                            new XAttribute("version", "1.4.1"),
                            new XElement(cNS + "asset",
                                new XElement(cNS + "created", DateTime.UtcNow.ToString("o")),
                                new XElement(cNS + "modified", DateTime.UtcNow.ToString("o"))
                            ),
                            new XElement(cNS + "library_effects",
                                new XElement(cNS + "effect",
                                    new XAttribute("id", "Library-Effect-GreenPhong"),
                                    new XElement(cNS + "profile_COMMON",
                                        new XElement(cNS + "technique",
                                            new XAttribute("sid", "phong"),
                                            new XElement(cNS + "phong",
                                                new XElement(cNS + "emission",
                                                    new XElement(cNS + "color", "1.0 1.0 1.0 1.0")
                                                ),
                                                new XElement(cNS + "ambient",
                                                    new XElement(cNS + "color", "1.0 1.0 1.0 1.0")
                                                ),
                                                new XElement(cNS + "diffuse",
                                                    new XElement(cNS + "color", "0.0 1.0 0.0 1.0")
                                                ),
                                                new XElement(cNS + "specular",
                                                    new XElement(cNS + "color", "1.0 1.0 1.0 1.0")
                                                ),
                                                new XElement(cNS + "shininess",
                                                    new XElement(cNS + "float", "20.0")
                                                ),
                                                new XElement(cNS + "reflective",
                                                    new XElement(cNS + "color", "1.0 1.0 1.0 1.0")
                                                ),
                                                new XElement(cNS + "reflectivity",
                                                    new XElement(cNS + "float", "0.5")
                                                ),
                                                new XElement(cNS + "transparent",
                                                    new XElement(cNS + "color", "1.0 1.0 1.0 1.0")
                                                ),
                                                new XElement(cNS + "transparency",
                                                    new XElement(cNS + "float", "1.0")
                                                )
                                            )
                                        )
                                    )
                                )
                            ),
                            new XElement(cNS + "library_materials",
                                new XElement(cNS + "material",
                                    new XAttribute("id", "Library-Material-Terrain"),
                                    new XAttribute("name", "Terrain material"),
                                    new XElement(cNS + "instance_effect",
                                        new XAttribute("url", "#Library-Effect-GreenPhong")
                                    )
                                )
                            ),
                            new XElement(cNS + "library_geometries",
                                new XElement(cNS + "geometry",
                                    new XAttribute("id", "Library-Geometry-Terrain"),
                                    new XAttribute("name", "Terrain geometry"),
                                    new XElement(cNS + "mesh",
                                        new XElement(cNS + "source",
                                            new XAttribute("id", "Library-Geometry-Terrain-Position"),
                                            new XElement(cNS + "float_array",
                                                new XAttribute("id", "Library-Geometry-Terrain-Position-Array"),
                                                new XAttribute("count", patch.GetVertexArrayLength()),
                                                patch.GetVertexArray()
                                            ),
                                            new XElement(cNS + "technique_common",
                                                new XElement(cNS + "accessor",
                                                    new XAttribute("source", "#Library-Geometry-Terrain-Position-Array"),
                                                    new XAttribute("count", patch.GetVertexLength()),
                                                    new XAttribute("stride", 3),
                                                    new XElement(cNS + "param",
                                                        new XAttribute("name", "X"),
                                                        new XAttribute("type", "float")
                                                    ),
                                                    new XElement(cNS + "param",
                                                        new XAttribute("name", "Y"),
                                                        new XAttribute("type", "float")
                                                    ),
                                                    new XElement(cNS + "param",
                                                        new XAttribute("name", "Z"),
                                                        new XAttribute("type", "float")
                                                    )
                                                )
                                            )
                                        ),
                                        new XElement(cNS + "vertices",
                                            new XAttribute("id", "Library-Geometry-Terrain-Vertex"),
                                            new XElement(cNS + "input",
                                                new XAttribute("semantic", "POSITION"),
                                                new XAttribute("source", "#Library-Geometry-Terrain-Position")
                                            )
                                        ),
                                        new XElement(cNS + "polygons",
                                            new XObject[] {
                                                new XAttribute("count", patch.GetPolygonLength()),
                                                new XAttribute("material", "MATERIAL"),
                                                new XElement(cNS + "input",
                                                    new XAttribute("semantic", "VERTEX"),
                                                    new XAttribute("source", "#Library-Geometry-Terrain-Vertex"),
                                                    new XAttribute("offset", 0)
                                                )
                                            }.Concat(patch.GetPolygonArray().Select(polygon => (XObject)new XElement(cNS + "p", polygon)))
                                        )
                                    )
                                )
                            ),
                            // Move nodes into <library_nodes/> to make them individual components in SketchUp.
                            //new XElement(cNS + "library_nodes",
                            //),
                            new XElement(cNS + "library_visual_scenes",
                                new XElement(cNS + "visual_scene",
                                    new XAttribute("id", "VisualScene-Default"),
                                    new XElement(cNS + "node",
                                        new XAttribute("id", "Node-Terrain"),
                                        new XAttribute("name", "Terrain"),
                                        new XElement(cNS + "instance_geometry",
                                            new XAttribute("url", "#Library-Geometry-Terrain"),
                                            new XElement(cNS + "bind_material",
                                                new XElement(cNS + "technique_common",
                                                    new XElement(cNS + "instance_material",
                                                        new XAttribute("symbol", "MATERIAL"),
                                                        new XAttribute("target", "#Library-Material-Terrain")
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            ),
                            new XElement(cNS + "scene",
                                new XElement(cNS + "instance_visual_scene",
                                    new XAttribute("url", "#VisualScene-Default")
                                )
                            )
                        )
                    );

                    foreach (var output in conversion.Output)
                    {
                        var fileName = Path.ChangeExtension(output, string.Format("{0:00}-{1:00}.dae", x, z));
                        colladaDocument.Save(fileName);
                    }
                }
            }

            return true;
        }
    }

    class TerrainConverterPatch
    {
        readonly TerrainFile TFile;
        readonly TerrainAltitudeFile YFile;
        readonly int PatchX;
        readonly int PatchZ;
        readonly int PatchSize;

        public TerrainConverterPatch(TerrainFile tFile, TerrainAltitudeFile yFile, int patchX, int patchZ)
        {
            TFile = tFile;
            YFile = yFile;
            PatchX = patchX;
            PatchZ = patchZ;
            PatchSize = tFile.terrain.terrain_samples.terrain_nsamples / tFile.terrain.terrain_patchsets[0].terrain_patchset_npatches;
        }

        private int GetElevation(int x, int z)
        {
            return YFile.GetElevation(
                Math.Min(PatchX * PatchSize + x, TFile.terrain.terrain_samples.terrain_nsamples - 1),
                Math.Min(PatchZ * PatchSize + z, TFile.terrain.terrain_samples.terrain_nsamples - 1)
            );
        }

        public int GetVertexLength()
        {
            return (PatchSize + 1) * (PatchSize + 1);
        }

        public int GetVertexArrayLength()
        {
            return GetVertexLength() * 3;
        }

        public string GetVertexArray()
        {
            var output = new StringBuilder();

            for (var x = 0; x <= PatchSize; x++)
            {
                for (var z = 0; z <= PatchSize; z++)
                {
                    output.AppendFormat("{0} {1} {2} ",
                        x * TFile.terrain.terrain_samples.terrain_sample_size,
                        GetElevation(x, z) * TFile.terrain.terrain_samples.terrain_sample_scale,
                        z * TFile.terrain.terrain_samples.terrain_sample_size
                    );
                }
            }

            return output.ToString();
        }

        public int GetPolygonLength()
        {
            return PatchSize * PatchSize * 2;
        }

        public List<string> GetPolygonArray()
        {
            var output = new List<string>();
            var stride = PatchSize + 1;

            for (var x = 0; x < PatchSize; x++)
            {
                for (var z = 0; z < PatchSize; z++)
                {
                    var nw = (x + 0) + stride * (z + 0);
                    var ne = (x + 1) + stride * (z + 0);
                    var sw = (x + 0) + stride * (z + 1);
                    var se = (x + 1) + stride * (z + 1);
                    if ((z & 1) == (x & 1))
                    {
                        output.Add(string.Format("{0} {1} {2}", nw, se, sw));
                        output.Add(string.Format("{0} {1} {2}", nw, ne, se));
                    }
                    else
                    {
                        output.Add(string.Format("{0} {1} {2}", ne, se, sw));
                        output.Add(string.Format("{0} {1} {2}", nw, ne, sw));
                    }
                }
            }

            return output;
        }
    }
}
