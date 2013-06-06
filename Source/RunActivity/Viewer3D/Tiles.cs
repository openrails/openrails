// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MSTS;

namespace ORTS
{
    public class TileManager
    {
        const int MaximumCachedTiles = 8 * 8;

        public int maxDim = 256;
        public int tilesCovered = 1;
        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        TileList Tiles = new TileList(new List<Tile>());

        readonly string FilePath;

        public TileManager(string filePath)
        {
            FilePath = filePath;
        }

        [CallOnThread("Loader")]
        public void Load(int tileX, int tileZ, bool visible)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("Tiles.Load incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            var tiles = Tiles;
            if (!tiles.ByXZ.ContainsKey(tileX + "," + tileZ))
            {
                // Take the current list of tiles, evict any necessary so the new tile fits, load and add the new
                // tile to the list, and store it all atomically in Tiles.
                var tileList = new List<Tile>(tiles.List);
                while (tileList.Count >= MaximumCachedTiles)
                    tileList.RemoveAt(0);
                Tile newTile = new Tile(FilePath, tileX, tileZ, visible);
                // Ignore if newTile is not complete
                if (newTile.TFile != null && newTile.YFile != null && newTile.FFile != null)
                    tileList.Add(newTile);
                Tiles = new TileList(tileList);
            }
        }

        [CallOnThread("Loader")]
        public void Load(int tileX, int tileZ, bool visible, bool isLoTile)
        {
            tilesCovered = 8;
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("Tiles.Load incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            var tiles = Tiles;
            if (!tiles.ByXZ.ContainsKey(tileX + "," + tileZ))
            {
                var name = TileNameConversion.GetTileNameFromTileXZ(tileX, tileZ);

                var fileName = name.Substring(0, name.Length - 2).Replace('-', '_');

                if (name.Replace('-', '_') != fileName + "00") return;

                // Take the current list of tiles, evict any necessary so the new tile fits, load and add the new
                // tile to the list, and store it all atomically in Tiles.
                var tileList = new List<Tile>(tiles.List);
                while (tileList.Count >= MaximumCachedTiles)
                    tileList.RemoveAt(0);
                Tile newTile = new Tile(FilePath, tileX, tileZ, visible, true);//want lo tiles
                // Ignore if newTile is not complete
                if (newTile.TFile != null && newTile.YFile != null)
                {
                    maxDim = newTile.TFile.terrain.terrain_patchsets[0].xdim * 16;
                    tileList.Add(newTile);
                }
                Tiles = new TileList(tileList);
            }
        }

        public Tile GetTile(int tileX, int tileZ)
        {
            var tiles = Tiles;
            if (!tiles.ByXZ.ContainsKey(tileX + "," + tileZ))
                return null;
            return tiles.ByXZ[tileX + "," + tileZ];
        }

        public float GetElevation(WorldLocation location)
        {
            return GetElevation(location.TileX, location.TileZ, (1024 + location.Location.X) / 8, (1024 - location.Location.Z) / 8);
        }

        public float GetElevation(int tileX, int tileZ, int x, int z)
        {
            var step = tilesCovered;
            //if (maxDim == 64) step = 8;
            // normalize x,y coordinates
            while (x > maxDim -1) { x -= maxDim; tileX+=step; }
            while (x < 0) { x += maxDim; tileX-=step; }
            while (z > maxDim - 1) { z -= maxDim; tileZ-=step; }
            while (z < 0) { z += maxDim; tileZ+=step; }

            var tile = GetTile(tileX, tileZ);
            if (tile != null)
                return tile.GetElevation(x, z);
            return 0;
        }

        public float GetElevation(int tileX, int tileZ, float x, float z)
        {
            // Start with the north west corner.
            var ux = (int)Math.Floor(x);
            var uz = (int)Math.Floor(z);
            var nw = GetElevation(tileX, tileZ, ux, uz);
            var ne = GetElevation(tileX, tileZ, ux + 1, uz);
            var sw = GetElevation(tileX, tileZ, ux, uz + 1);
            var se = GetElevation(tileX, tileZ, ux + 1, uz + 1);

            // Condition must match TerrainPatch.SetupPatchIndexBuffer's condition.
            if (((ux & 1) == (uz & 1)))
            {
                // Split NW-SE
                if ((x - ux) > (z - uz))
                    // NE side
                    return nw + (ne - nw) * (x - ux) + (se - ne) * (z - uz);
                // SW side
                return nw + (se - sw) * (x - ux) + (sw - nw) * (z - uz);
            }
            // Split NE-SW
            if ((x - ux) + (z - uz) < 1)
                // NW side
                return nw + (ne - nw) * (x - ux) + (sw - nw) * (z - uz);
            // SE side
            return se + (sw - se) * (1 - x + ux) + (ne - se) * (1 - z + uz);
        }

        public bool IsVertexHidden(int tileX, int tileZ, int x, int z)
        {
            var step = tilesCovered;
            // normalize x,y coordinates
            while (x > maxDim - 1) { x -= maxDim; tileX+=step; }
            while (x < 0) { x += maxDim; tileX-=step; }
            while (z > maxDim - 1) { z -= maxDim; tileZ-=step; }
            while (z < 0) { z += maxDim; tileZ+=step; }

            var tile = GetTile(tileX, tileZ);
            if (tile != null)
                return tile.IsVertexHidden(x, z);
            return false;
        }

        class TileList
        {
            /// <summary>
            /// Stores tiles in load order, so eviction is predictable and reasonable.
            /// </summary>
            public readonly List<Tile> List;
            /// <summary>
            /// Stores tiles by their TileX, TileZ location, so lookup is fast.
            /// </summary>
            public readonly Dictionary<string, Tile> ByXZ;
            public TileList(List<Tile> list)
            {
                List = list;
                ByXZ = list.ToDictionary(t => t.TileX + "," + t.TileZ);
            }
        }
    }

    public class Tile
    {
        public readonly int TileX;
        public readonly int TileZ;
        public readonly TFile TFile;
        public readonly YFile YFile;
        public readonly FFile FFile;

        public bool IsEmpty { get { return TFile == null; } }

        public int TilesCovered = 1;
        /// <param name="visible">Tiles adjacent to the current visible tile may not be modelled.
        /// This flag decides whether a missing file leads to a warning message.</param>
        public Tile(string filePath, int tileX, int tileZ, bool visible)
        {
            TileX = tileX;
            TileZ = tileZ;
            var tileName = TileNameConversion.GetTileNameFromTileXZ(tileX, tileZ);
            var fileName = filePath + tileName;
            var name = fileName + ".t";
            if (File.Exists(name))
            {
                try
                {
                    TFile = new TFile(name);
                    name = fileName + "_y.raw";
                    YFile = new YFile(name);
                    name = fileName + "_f.raw";
                    FFile = new FFile(name);
                }
                catch (Exception error) // errors thrown by SBR
                {
                    Trace.WriteLine(error);
                }
            }
            else
            {
                var tmpfileName = tileName.Substring(0, tileName.Length - 1);
                if (tileName != tmpfileName + "0") return;
                fileName = filePath + tmpfileName.Replace('-', '_');
                name = fileName + ".t";
                if (File.Exists(name))
                {
                    try
                    {
                        TFile = new TFile(name);
                        TilesCovered = (int)(TFile.terrain.terrain_samples.terrain_nsamples * TFile.terrain.terrain_samples.terrain_sample_size / 2048);
                        name = fileName + "_y.raw";
                        YFile = new YFile(name);
                        name = fileName + "_f.raw";
                        FFile = new FFile(name);
                    }
                    catch (Exception error) // errors thrown by SBR
                    {
                        Trace.WriteLine(error);
                    }
                }

                // Many tiles adjacent to the visible tile may not be modelled, so a warning is not helpful,
                // so ignore a missing .t file unless it is the currently visible tile.
                else if (visible)
                    Trace.TraceWarning("Tile file missing - {0}", name);
            }
        }

        /// <param name="visible">Tiles adjacent to the current visible tile may not be modelled.
        /// This flag decides whether a missing file leads to a warning message.</param>
        public Tile(string filePath, int tileX, int tileZ, bool visible, bool isLoTiles)
        {
            TileX = tileX;
            TileZ = tileZ;
            var name = TileNameConversion.GetTileNameFromTileXZ(tileX, tileZ);

            var fileName = name.Substring(0, name.Length - 2).Replace('-', '_');

            if (name.Replace('-', '_') != fileName + "00") return;
            name = filePath + fileName + ".t";
            if (File.Exists(name))
            {
                try
                {
                    TFile = new TFile(name);
                    TilesCovered = (int)(TFile.terrain.terrain_samples.terrain_nsamples * TFile.terrain.terrain_samples.terrain_sample_size / 2048);
                    name = filePath + fileName + "_y.raw";
                    YFile = new YFile(name, TFile.terrain.terrain_patchsets[0].xdim * 16);
                    name = filePath + fileName + "_f.raw";
                    if (File.Exists(name))
                        FFile = new FFile(name, TFile.terrain.terrain_patchsets[0].xdim * 16);
                    else FFile = null;
                }
                catch (Exception error) // errors thrown by SBR
                {
                    Trace.WriteLine(error);
                }
            }
            else
            {
                fileName = fileName.Replace('_', '-');
                name = filePath + fileName + ".t";
                if (File.Exists(name))
                {
                    try
                    {
                        TFile = new TFile(name);
                        TilesCovered = (int)(TFile.terrain.terrain_samples.terrain_nsamples * TFile.terrain.terrain_samples.terrain_sample_size / 2048);
                        name = filePath + fileName + "_y.raw";
                        YFile = new YFile(name, TFile.terrain.terrain_patchsets[0].xdim * 16);
                        name = filePath + fileName + "_f.raw";
                        if (File.Exists(name))
                            FFile = new FFile(name, TFile.terrain.terrain_patchsets[0].xdim * 16);
                        else FFile = null;
                    }
                    catch (Exception error) // errors thrown by SBR
                    {
                        Trace.WriteLine(error);
                    }
                }
            }
        }

        public float GetElevation(int x, int z)
        {
            if (TFile == null)
                return 0;
            var e = YFile.GetElevationIndex(x, z);
            return (float)e * TFile.Resolution + TFile.Floor;
        }

        public bool IsVertexHidden(int x, int z)
        {
            if (FFile == null)
                return false;
            return FFile.IsVertexHidden(x, z);
        }
    }
}
