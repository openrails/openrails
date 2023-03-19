// COPYRIGHT 2014 by the Open Rails project.
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
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DataCollector
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("/system", StringComparer.OrdinalIgnoreCase))
                SystemInfo.WriteSystemDetails(Console.Out);
            else if (args.Contains("/tile-terrtex", StringComparer.OrdinalIgnoreCase))
                CollectTileTerrtex(args);
            else
                ShowHelp();
        }

        static void ShowHelp()
        {
            var version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location);
            Console.WriteLine("{0} {1}", version.FileDescription, VersionInfo.VersionOrBuild);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  {0} [options] [<PATH> [...]]", Path.GetFileNameWithoutExtension(version.FileName));
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <PATH>         Directories to scan for specific options");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  /system        Collects and reports on various system information");
            Console.WriteLine("  /tile-terrtex  Scans the provided PATHs for MSTS tile files (.t) and");
            Console.WriteLine("                 produces a statistical summary of the terrtex used");
            Console.WriteLine("  /help          Show help and usage information");
        }

        struct TileTerrtexDirectory
        {
            public string Path;
            public int TileCount;
            public float Tile1Count;
            public float Tile4Count;
            public float Tile16Count;
            public float Tile64Count;
            public TileTerrtexDirectory(string path)
            {
                Path = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
                TileCount = 0;
                Tile1Count = 0;
                Tile4Count = 0;
                Tile16Count = 0;
                Tile64Count = 0;
            }
        }

        static void CollectTileTerrtex(string[] args)
        {
            var summary = new List<TileTerrtexDirectory>();
            foreach (var arg in args)
            {
                if (Directory.Exists(arg))
                {
                    Console.WriteLine("Scanning {0}...", arg);
                    foreach (var path in Directory.GetDirectories(arg, "Tiles", SearchOption.AllDirectories))
                    {
                        Console.WriteLine("Scanning {0}...", path);
                        var data = new TileTerrtexDirectory(path);
                        foreach (var file in Directory.GetFiles(path, "*.t"))
                        {
                            try
                            {
                                var t = new TerrainFile(file);
                                if (t.terrain.terrain_patchsets.Length != 1)
                                    throw new InvalidDataException(String.Format("Tile has {0} patch sets; expected 1.", t.terrain.terrain_patchsets.Length));
                                if (t.terrain.terrain_patchsets[0].terrain_patchset_npatches != 16)
                                    throw new InvalidDataException(String.Format("Tile has {0} patches; expected 16.", t.terrain.terrain_patchsets[0].terrain_patchset_npatches));
                                if (t.terrain.terrain_patchsets[0].terrain_patchset_patches.Length != 256)
                                    throw new InvalidDataException(String.Format("Tile has {0} patches; expected 256.", t.terrain.terrain_patchsets[0].terrain_patchset_patches.Length));

                                data.TileCount++;
                                var patchset = t.terrain.terrain_patchsets[0];
                                var textures = new List<string>(patchset.terrain_patchset_npatches * patchset.terrain_patchset_npatches);
                                foreach (var patch in patchset.terrain_patchset_patches)
                                {
                                    textures.Add(String.Join("|", (from ts in t.terrain.terrain_shaders[patch.ShaderIndex].terrain_texslots
                                                                   select ts.Filename).ToArray()));
                                }

                                // 1th
                                if (textures.Distinct().Count() == 1)
                                    data.Tile1Count++;

                                // 4th
                                var textures4 = new List<string>[4];
                                for (var i = 0; i < textures4.Length; i++)
                                    textures4[i] = new List<string>();
                                for (var x = 0; x < 16; x++)
                                {
                                    for (var y = 0; y < 16; y++)
                                    {
                                        var tx = (int)(x / 8);
                                        var ty = (int)(y / 8);
                                        textures4[tx + ty * 2].Add(textures[x + y * 16]);
                                    }
                                }
                                for (var i = 0; i < textures4.Length; i++)
                                    if (textures4[i].Distinct().Count() == 1)
                                        data.Tile4Count++;

                                // 16th
                                var textures16 = new List<string>[16];
                                for (var i = 0; i < textures16.Length; i++)
                                    textures16[i] = new List<string>();
                                for (var x = 0; x < 16; x++)
                                {
                                    for (var y = 0; y < 16; y++)
                                    {
                                        var tx = (int)(x / 4);
                                        var ty = (int)(y / 4);
                                        textures16[tx + ty * 4].Add(textures[x + y * 16]);
                                    }
                                }
                                for (var i = 0; i < textures16.Length; i++)
                                    if (textures16[i].Distinct().Count() == 1)
                                        data.Tile16Count++;

                                // 64th
                                var textures64 = new List<string>[64];
                                for (var i = 0; i < textures64.Length; i++)
                                    textures64[i] = new List<string>();
                                for (var x = 0; x < 16; x++)
                                {
                                    for (var y = 0; y < 16; y++)
                                    {
                                        var tx = (int)(x / 2);
                                        var ty = (int)(y / 2);
                                        textures64[tx + ty * 8].Add(textures[x + y * 16]);
                                    }
                                }
                                for (var i = 0; i < textures64.Length; i++)
                                    if (textures64[i].Distinct().Count() == 1)
                                        data.Tile64Count++;
                            }
                            catch (Exception error)
                            {
                                Console.WriteLine("Error reading tile {0}: {1}", file, error);
                            }
                        }
                        if (data.TileCount > 0)
                            summary.Add(data);
                    }
                }
            }
            Console.WriteLine();
            foreach (var data in from data in summary
                                 orderby data.Path
                                 select data)
            {
                Console.WriteLine("{0,30} / {1,-4} / 1th {2,5:P0} / 4th {3,5:P0} / 16th {4,5:P0} / 64th {5,5:P0}", data.Path, data.TileCount, data.Tile1Count / data.TileCount, data.Tile4Count / 4 / data.TileCount, data.Tile16Count / 16 / data.TileCount, data.Tile64Count / 64 / data.TileCount);
            }
        }
    }
}
