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

using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Orts.Viewer3D
{
    /// <summary>
    /// Provides a MRU cache of tile data for a given resolution.
    /// </summary>
    [DebuggerDisplay("Count = {Tiles.List.Count}, Zoom = {Zoom}")]
    public class TileManager
    {
        const int MaximumCachedTiles = 8 * 8;

        public readonly string FilePath;
        public readonly TileName.Zoom Zoom;

        // THREAD SAFETY:
        //   All accesses must be done in local variables. No modifications to the objects are allowed except by
        //   assignment of a new instance (possibly cloned and then modified).
        TileList Tiles = new TileList(new List<Tile>());

        /// <summary>
        /// Constructs a new TileManager for loading tiles from a specific path, either at high-resolution or low-resolution.
        /// </summary>
        /// <param name="filePath">Path of the directory containing the MSTS tiles</param>
        /// <param name="loTiles">Flag indicating whether the tiles loaded should be high-resolution (2KM and 4KM square) or low-resolution (16KM and 32KM square, for distant mountains)</param>
        public TileManager(string filePath, bool loTiles)
        {
            FilePath = filePath;
            Zoom = loTiles ? TileName.Zoom.DMSmall : TileName.Zoom.Small;
        }

        /// <summary>
        /// Loads a specific tile, if it exists and is not already loaded.
        /// </summary>
        /// <param name="tileX">MSTS TileX coordinate of the tile, or of a logical tile inside a larger physical tile</param>
        /// <param name="tileZ">MSTS TileZ coordinate of the tile, or of a logical tile inside a larger physical tile</param>
        /// <param name="visible">Flag indicating whether the tile being loaded should be considered "key" to the user experience, and thus whether issues loading it should be shown.</param>
        [CallOnThread("Loader")]
        public void Load(int tileX, int tileZ, bool visible)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("Tiles.Load incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            var tiles = Tiles;

            // Take the current list of tiles, evict any necessary so the new tile fits, load and add the new
            // tile to the list, and store it all atomically in Tiles.
            var tileList = new List<Tile>(tiles.List);
            while (tileList.Count >= MaximumCachedTiles)
                tileList.RemoveAt(0);

            // Check for 1x1 (or 8x8) tiles.
            TileName.Snap(ref tileX, ref tileZ, Zoom);
            if (tiles.ByXZ.ContainsKey(((uint)tileX << 16) + (uint)tileZ))
                if (!tiles.ByXZ[((uint)tileX << 16) + (uint)tileZ].StaleData)
                    return;
                else // Remove stale tile from the list so it gets reloaded
                    tileList.Remove(tiles.ByXZ[((uint)tileX << 16) + (uint)tileZ]);

            var newTile = new Tile(FilePath, tileX, tileZ, Zoom, visible);
            if (newTile.Loaded)
            {
                tileList.Add(newTile);
                Tiles = new TileList(tileList);
                return;
            }

            // Check for 2x2 (or 16x16) tiles.
            TileName.Snap(ref tileX, ref tileZ, Zoom - 1);
            if (tiles.ByXZ.ContainsKey(((uint)tileX << 16) + (uint)tileZ))
                if (!tiles.ByXZ[((uint)tileX << 16) + (uint)tileZ].StaleData)
                    return;
                else // Remove stale tile from the list so it gets reloaded
                    tileList.Remove(tiles.ByXZ[((uint)tileX << 16) + (uint)tileZ]);

            newTile = new Tile(FilePath, tileX, tileZ, Zoom - 1, visible);
            if (newTile.Loaded)
            {
                tileList.Add(newTile);
                Tiles = new TileList(tileList);
                return;
            }
        }

        /// <summary>
        /// Loads, if it is not already loaded, and gets the tile for the specified coordinates.
        /// </summary>
        /// <param name="tileX">MSTS TileX coordinate of the tile, or of a logical tile inside a larger physical tile</param>
        /// <param name="tileZ">MSTS TileZ coordinate of the tile, or of a logical tile inside a larger physical tile</param>
        /// <param name="visible">Flag indicating whether the tile being loaded should be considered "key" to the user experience, and thus whether issues loading it should be shown.</param>
        /// <returns>The <c>Tile</c> covering the specified coordinates, if one exists and is loaded. It may be a single tile or quad tile.</returns>
        [CallOnThread("Loader")]
        public Tile LoadAndGetTile(int tileX, int tileZ, bool visible)
        {
            Load(tileX, tileZ, visible);
            return GetTile(tileX, tileZ);
        }

        /// <summary>
        /// Loads a specific tile, if it is not already loaded, and gets the elevation of the terrain at a specific location, interpolating between sample points.
        /// </summary>
        /// <param name="tileX">MSTS TileX coordinate</param>
        /// <param name="tileZ">MSTS TileZ coordinate</param>
        /// <param name="x">MSTS X coordinate within tile</param>
        /// <param name="z">MSTS Z coordinate within tile</param>
        /// <param name="visible">Flag indicating whether the tile being loaded should be considered "key" to the user experience, and thus whether issues loading it should be shown.</param>
        /// <returns>Elevation at the given coordinates</returns>
        [CallOnThread("Loader")]
        public float LoadAndGetElevation(int tileX, int tileZ, float x, float z, bool visible)
        {
            // Normalize the coordinates to the right tile.
            while (x >= 1024) { x -= 2048; tileX++; }
            while (x < -1024) { x += 2048; tileX--; }
            while (z >= 1024) { z -= 2048; tileZ++; }
            while (z < -1024) { z += 2048; tileZ--; }

            Load(tileX, tileZ, visible);
            return GetElevation(tileX, tileZ, x, z);
        }

        /// <summary>
        /// Gets, if it is loaded, the tile for the specified coordinates.
        /// </summary>
        /// <param name="tileX">MSTS TileX coordinate</param>
        /// <param name="tileZ">MSTS TileZ coordinate</param>
        /// <returns>The <c>Tile</c> covering the specified coordinates, if one exists and is loaded. It may be a single tile or quad tile.</returns>
        public Tile GetTile(int tileX, int tileZ)
        {
            var tiles = Tiles;
            Tile tile;

            // Check for 1x1 (or 8x8) tiles.
            TileName.Snap(ref tileX, ref tileZ, Zoom);
            if (tiles.ByXZ.TryGetValue(((uint)tileX << 16) + (uint)tileZ, out tile) && tile.Size == (1 << (15 - (int)Zoom)))
                return tile;

            // Check for 2x2 (or 16x16) tiles.
            TileName.Snap(ref tileX, ref tileZ, Zoom - 1);
            if (tiles.ByXZ.TryGetValue(((uint)tileX << 16) + (uint)tileZ, out tile) && tile.Size == (1 << (15 - (int)Zoom + 1)))
                return tile;

            return null;
        }

        /// <summary>
        /// Gets the elevation of the terrain at a specific location, interpolating between sample points.
        /// </summary>
        /// <param name="location">MSTS coordinates</param>
        /// <returns>Elevation at the given coordinates</returns>
        public float GetElevation(WorldLocation location)
        {
            return GetElevation(location.TileX, location.TileZ, location.Location.X, location.Location.Z);
        }

        /// <summary>
        /// Gets the elevation of the terrain at a specific location, interpolating between sample points.
        /// </summary>
        /// <param name="tileX">MSTS TileX coordinate</param>
        /// <param name="tileZ">MSTS TileZ coordinate</param>
        /// <param name="x">MSTS X coordinate within tile</param>
        /// <param name="z">MSTS Z coordinate within tile</param>
        /// <returns>Elevation at the given coordinates</returns>
        public float GetElevation(int tileX, int tileZ, float x, float z)
        {
            // Normalize the coordinates to the right tile.
            while (x >= 1024) { x -= 2048; tileX++; }
            while (x < -1024) { x += 2048; tileX--; }
            while (z >= 1024) { z -= 2048; tileZ++; }
            while (z < -1024) { z += 2048; tileZ--; }

            // Fetch the tile we're looking up elevation for; if it isn't loaded, no elevation.
            var tile = GetTile(tileX, tileZ);
            if (tile == null)
                return 0;

            // Adjust x/z based on the tile we found - this may not be in the same TileX/Z as we requested due to large (e.g. 2x2) tiles.
            x += 1024 + 2048 * (tileX - tile.TileX);
            z += 1024 + 2048 * (tileZ - tile.TileZ - tile.Size);
            z *= -1;

            // Convert x/z in meters to terrain tile samples and get the coordinates of the NW corner.
            x /= tile.SampleSize;
            z /= tile.SampleSize;
            var ux = (int)Math.Floor(x);
            var uz = (int)Math.Floor(z);

            // Start with the north west corner.
            var nw = GetElevation(tile, ux, uz);
            var ne = GetElevation(tile, ux + 1, uz);
            var sw = GetElevation(tile, ux, uz + 1);
            var se = GetElevation(tile, ux + 1, uz + 1);

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

        /// <summary>
        /// Gets the elevation of the terrain at a specific sample point within a specific tile. Wraps to the edges of the next tile in each direction.
        /// </summary>
        /// <param name="tile">Tile for the sample coordinates</param>
        /// <param name="ux">X sample coordinate</param>
        /// <param name="uz">Z sample coordinate</param>
        /// <returns>Elevation at the given sample coordinates</returns>
        public float GetElevation(Tile tile, int ux, int uz)
        {
            if (ux >= 0 && ux < tile.SampleCount && uz >= 0 && uz < tile.SampleCount)
                return tile.GetElevation(ux, uz);

            // We're outside the sample range for the given tile, so we need to convert the ux/uz in to physical
            // position (in meters) so that we can correctly look up the tile and not have to worry if it is the same
            // sample resolution or not.
            var x = ux * tile.SampleSize;
            var z = 2048 * tile.Size - uz * tile.SampleSize;
            var otherTile = GetTile(tile.TileX + (int)Math.Floor(x / 2048), tile.TileZ + (int)Math.Floor((z - 1) / 2048));
            if (otherTile != null)
            {
                var ux2 = (int)((x + 2048 * (tile.TileX - otherTile.TileX)) / otherTile.SampleSize);
                var uz2 = -(int)((z + 2048 * (tile.TileZ - otherTile.TileZ - otherTile.Size)) / otherTile.SampleSize);
                ux2 = Math.Min(ux2, otherTile.SampleCount - 1);
                uz2 = Math.Min(uz2, otherTile.SampleCount - 1);
                return otherTile.GetElevation(ux2, uz2);
            }

            // No suitable tile was found, so just use the nearest sample from the tile we started with. This means
            // that when we run out of terrain, we just repeat the last value instead of getting a vertical cliff.
            return tile.GetElevation((int)MathHelper.Clamp(ux, 0, tile.SampleCount - 1), (int)MathHelper.Clamp(uz, 0, tile.SampleCount - 1));
        }

        /// <summary>
        /// Gets the vertex-hidden flag of the terrain at a specific sample point within a specific tile. Wraps to the edges of the next tile in each direction.
        /// </summary>
        /// <param name="tile">Tile for the sample coordinates</param>
        /// <param name="ux">X sample coordinate</param>
        /// <param name="uz">Z sample coordinate</param>
        /// <returns>Vertex-hidden flag at the given sample coordinates</returns>
        public bool IsVertexHidden(Tile tile, int ux, int uz)
        {
            if (ux >= 0 && ux < tile.SampleCount && uz >= 0 && uz < tile.SampleCount)
                return tile.IsVertexHidden(ux, uz);

            // We're outside the sample range for the given tile, so we need to convert the ux/uz in to physical
            // position (in meters) so that we can correctly look up the tile and not have to worry if it is the same
            // sample resolution or not.
            var x = ux * tile.SampleSize;
            var z = 2048 * tile.Size - uz * tile.SampleSize;
            var otherTile = GetTile(tile.TileX + (int)Math.Floor(x / 2048), tile.TileZ + (int)Math.Floor((z - 1) / 2048));
            if (otherTile != null)
            {
                var ux2 = (int)((x + 2048 * (tile.TileX - otherTile.TileX)) / otherTile.SampleSize);
                var uz2 = -(int)((z + 2048 * (tile.TileZ - otherTile.TileZ - otherTile.Size)) / otherTile.SampleSize);
                return otherTile.IsVertexHidden(ux2, uz2);
            }

            // No suitable tile was found, so just return that the vertex is normal - i.e. visible.
            return false;
        }

        [DebuggerDisplay("Count = {List.Count}")]
        class TileList
        {
            /// <summary>
            /// Stores tiles in load order, so eviction is predictable and reasonable.
            /// </summary>
            public readonly List<Tile> List;

            /// <summary>
            /// Stores tiles by their TileX, TileZ location, so lookup is fast.
            /// </summary>
            public readonly Dictionary<uint, Tile> ByXZ;

            public TileList(List<Tile> list)
            {
                List = list;
                ByXZ = list.ToDictionary(t => ((uint)t.TileX << 16) + (uint)t.TileZ);
            }
        }
    }

    /// <summary>
    /// Represents a single MSTS tile stored on disk, of whatever size (2KM, 4KM, 16KM or 32KM sqaure).
    /// </summary>
    [DebuggerDisplay("TileX = {TileX}, TileZ = {TileZ}, Size = {Size}")]
    public class Tile
    {
        public readonly int TileX, TileZ, Size;

        public bool Loaded { get { return TFile != null && YFile != null; } }
        public bool StaleData = false;
        public float Floor { get { return TFile.terrain.terrain_samples.terrain_sample_floor; } }  // in meters
        public float Resolution { get { return TFile.terrain.terrain_samples.terrain_sample_scale; } }  // in meters per( number in Y-file )
        public int SampleCount { get { return TFile.terrain.terrain_samples.terrain_nsamples; } }
        public float SampleSize { get { return TFile.terrain.terrain_samples.terrain_sample_size; } }
        public int PatchCount { get { return TFile.terrain.terrain_patchsets[0].terrain_patchset_npatches; } }
        public terrain_shader[] Shaders { get { return TFile.terrain.terrain_shaders; } }
        public float WaterNE { get { return TFile.terrain.terrain_water_height_offset.NE != 0 ? TFile.terrain.terrain_water_height_offset.NE : TFile.terrain.terrain_water_height_offset.SW; } } // in meters
        public float WaterNW { get { return TFile.terrain.terrain_water_height_offset.NW != 0 ? TFile.terrain.terrain_water_height_offset.NW : TFile.terrain.terrain_water_height_offset.SW; } }
        public float WaterSE { get { return TFile.terrain.terrain_water_height_offset.SE != 0 ? TFile.terrain.terrain_water_height_offset.SE : TFile.terrain.terrain_water_height_offset.SW; } }
        public float WaterSW { get { return TFile.terrain.terrain_water_height_offset.SW != 0 ? TFile.terrain.terrain_water_height_offset.SW : TFile.terrain.terrain_water_height_offset.SW; } }

        public bool ContainsWater
        {
            get
            {
                if (TFile.terrain.terrain_water_height_offset != null)
                    foreach (var patchset in TFile.terrain.terrain_patchsets)
                        foreach (var patch in patchset.terrain_patchset_patches)
                            if (patch.WaterEnabled)
                                return true;
                return false;
            }
        }

        public terrain_patchset_patch GetPatch(int x, int z)
        {
            return TFile.terrain.terrain_patchsets[0].terrain_patchset_patches[z * PatchCount + x];
        }

        readonly TerrainFile TFile;
        readonly TerrainAltitudeFile YFile;
        readonly TerrainFlagsFile FFile;

        public Tile(string filePath, int tileX, int tileZ, TileName.Zoom zoom, bool visible)
        {
            TileX = tileX;
            TileZ = tileZ;
            Size = 1 << (15 - (int)zoom);

            var fileName = filePath + TileName.FromTileXZ(tileX, tileZ, zoom);
            if (!File.Exists(fileName + ".t"))
            {
                // Many tiles adjacent to the visible tile may not be modelled, so a warning is not helpful;
                // ignore a missing .t file unless it is the currently visible tile.
                if (visible)
                    Trace.TraceWarning("Ignoring missing tile {0}.t", fileName);
                return;
            }

            // T and Y files are expected to exist; F files are optional.
            try
            {
                TFile = new TerrainFile(fileName + ".t");
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(fileName + ".t", error));
            }
            try
            {
                YFile = new TerrainAltitudeFile(fileName + "_y.raw", SampleCount);
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(fileName + "_y.raw", error));
            }
            try
            {
                if (File.Exists(fileName + "_f.raw"))
                    FFile = new TerrainFlagsFile(fileName + "_f.raw", SampleCount);
            }
            catch (Exception error)
            {
                Trace.WriteLine(new FileLoadException(fileName + "_f.raw", error));
            }
        }

        internal float GetElevation(int ux, int uz)
        {
            return (float)YFile.GetElevation(ux, uz) * Resolution + Floor;
        }

        internal bool IsVertexHidden(int ux, int uz)
        {
            return FFile == null ? false : FFile.IsVertexHidden(ux, uz);
        }
    }
}
