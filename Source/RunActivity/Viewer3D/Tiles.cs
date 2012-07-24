// COPYRIGHT 2009, 2010, 2011, 2012 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
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
        public void Load(int tileX, int tileZ)
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
                tileList.Add(new Tile(FilePath, tileX, tileZ));
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
            // normalize x,y coordinates
            while (x > 255) { x -= 256; ++tileX; }
            while (x < 0) { x += 256; --tileX; }
            while (z > 255) { z -= 256; --tileZ; }
            while (z < 0) { z += 256; ++tileZ; }

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
            // normalize x,y coordinates
            while (x > 255) { x -= 256; ++tileX; }
            while (x < 0) { x += 256; --tileX; }
            while (z > 255) { z -= 256; --tileZ; }
            while (z < 0) { z += 256; ++tileZ; }

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

        public Tile(string filePath, int tileX, int tileZ)
        {
            TileX = tileX;
            TileZ = tileZ;
            var fileName = filePath + TileNameConversion.GetTileNameFromTileXZ(tileX, tileZ);
            if (File.Exists(fileName + ".t"))
            {
                TFile = new TFile(fileName + ".t");
                YFile = new YFile(fileName + "_y.raw");
                FFile = new FFile(fileName + "_f.raw");
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
