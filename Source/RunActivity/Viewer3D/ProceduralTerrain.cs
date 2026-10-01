// Licensed under the GNU General Public License, version 3 or later.

using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Viewer3D.Common;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Viewer3D
{
    [CallOnThread("Loader")]
    sealed class ProceduralTerrainTile
    {
        // Keep these limits explicit until representative routes have been benchmarked.
        internal const int MaximumDetailedMaterialsPerPatch = 5;
        internal const int MaximumDetailedSourceTexturesPerTile = 32;
        // Direct rendering exposes the categorical scatter grid without the
        // filtering of TSRE's generated 512-pixel patch texture. Use a finer
        // grid so transition cells remain unobtrusive at close range.
        const int NoiseSide = 2048;

        internal sealed class Layer
        {
            public readonly byte Id;
            public readonly Texture2D Texture;

            public Layer(byte id, Texture2D texture)
            {
                Id = id;
                Texture = texture;
            }
        }

        internal sealed class Patch
        {
            public readonly Layer[] Layers;
            public readonly bool UseBakedBase;

            public Patch(Layer[] layers, bool useBakedBase)
            {
                Layers = layers;
                UseBakedBase = useBakedBase;
            }
        }

        public readonly Texture2D MaterialMapTexture;
        public readonly Texture2D NoiseTexture;
        public readonly Patch[] Patches;
        public readonly int PatchCount;

        ProceduralTerrainTile(Texture2D materialMapTexture, Texture2D noiseTexture,
            Patch[] patches, int patchCount)
        {
            MaterialMapTexture = materialMapTexture;
            NoiseTexture = noiseTexture;
            Patches = patches;
            PatchCount = patchCount;
        }

        public static ProceduralTerrainTile TryCreate(Viewer viewer, Tile tile,
            TerrainMaterialFile library)
        {
            terrain_materials extension = tile.TerrainMaterials;
            if (library == null || extension == null || !extension.IsValid ||
                !TerrainMaterialFile.IsSafeRelativePath(extension.MaterialBuffer))
                return null;

            try
            {
                string directory = Path.GetDirectoryName(tile.TerrainFilePath);
                string mapPath = Path.GetFullPath(Path.Combine(directory,
                    extension.MaterialBuffer.Replace('/', Path.DirectorySeparatorChar)));
                string directoryPrefix = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
                if (!mapPath.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Procedural terrain map is outside the tile directory");

                var timer = Stopwatch.StartNew();
                var map = new TerrainMaterialMapFile(mapPath);
                TerrainMaterialMapFile.PatchCoverage[] coverage = map.AnalyzePatches(tile.PatchCount);

                var globalCounts = new long[256];
                foreach (TerrainMaterialMapFile.PatchCoverage patch in coverage)
                    foreach (KeyValuePair<byte, int> material in patch.Materials)
                        globalCounts[material.Key] += material.Value;

                var sources = new Dictionary<byte, Texture2D>();
                foreach (int id in Enumerable.Range(0, globalCounts.Length)
                    .Where(id => globalCounts[id] > 0)
                    .OrderByDescending(id => globalCounts[id]).ThenBy(id => id))
                {
                    uint uid;
                    TerrainMaterialDefinition definition;
                    if (!extension.MaterialUids.TryGetValue((byte)id, out uid) ||
                        !library.Materials.TryGetValue(uid, out definition))
                        continue;

                    string texturePath = Helpers.GetProceduralTerrainTextureFile(
                        viewer.Simulator, definition.Texture);
                    Texture2D texture = viewer.TextureManager.Get(texturePath,
                        SharedMaterialManager.MissingTexture, true);
                    if (texture == SharedMaterialManager.MissingTexture)
                        continue;
                    sources.Add((byte)id, texture);
                    if (sources.Count == MaximumDetailedSourceTexturesPerTile)
                        break;
                }

                if (sources.Count == 0)
                    return null;

                var patches = new Patch[coverage.Length];
                int passTotal = 0;
                int passMaximum = 0;
                bool anyDetailedPatch = false;
                for (int i = 0; i < coverage.Length; ++i)
                {
                    Layer[] layers = coverage[i].Materials
                        .Where(item => sources.ContainsKey(item.Key))
                        .Take(MaximumDetailedMaterialsPerPatch)
                        .Select(item => new Layer(item.Key, sources[item.Key])).ToArray();
                    bool useBakedBase = layers.Length != coverage[i].Materials.Count;
                    patches[i] = new Patch(layers, useBakedBase);
                    anyDetailedPatch |= layers.Length > 0;
                    passTotal += layers.Length + (useBakedBase ? 1 : 0);
                    passMaximum = Math.Max(passMaximum, layers.Length + (useBakedBase ? 1 : 0));
                }
                if (!anyDetailedPatch)
                    return null;

                var info = new FileInfo(mapPath);
                string mapKey = String.Format("generated:terrain-map:{0}:{1}:{2}",
                    info.FullName, info.Length, info.LastWriteTimeUtc.Ticks);
                Texture2D mapTexture = viewer.TextureManager.GetGenerated(mapKey, device =>
                {
                    var texture = new Texture2D(device, TerrainMaterialMapFile.Side,
                        TerrainMaterialMapFile.Side, false, SurfaceFormat.Alpha8);
                    texture.SetData(map.MaterialIds);
                    return texture;
                });
                Texture2D noiseTexture = viewer.TextureManager.GetGenerated(
                    "generated:terrain-material-noise:v3", CreateNoiseTexture);

                timer.Stop();
                Trace.TraceInformation(
                    "Procedural terrain {0}: {1} source textures, {2:F2} average passes, {3} maximum passes, {4:F0} ms load/analysis",
                    Path.GetFileName(tile.TerrainFilePath), sources.Count,
                    (double)passTotal / patches.Length, passMaximum, timer.Elapsed.TotalMilliseconds);
                return new ProceduralTerrainTile(mapTexture, noiseTexture, patches, tile.PatchCount);
            }
            catch (Exception error)
            {
                Trace.TraceWarning("Ignoring procedural terrain for {0}: {1}",
                    tile.TerrainFilePath, error.Message);
                return null;
            }
        }

        internal Patch GetPatch(int x, int z)
        {
            return Patches[z * PatchCount + x];
        }

        static Texture2D CreateNoiseTexture(GraphicsDevice device)
        {
            var noise = new byte[NoiseSide * NoiseSide];
            for (int row = 0; row < NoiseSide; ++row)
            {
                for (int column = 0; column < NoiseSide; ++column)
                {
                    uint seed = unchecked((uint)column * 0x9e3779b9u ^
                        (uint)row * 0x85ebca6bu ^ 0x73518u);
                    noise[row * NoiseSide + column] = (byte)(ScatterHash(seed) >> 24);
                }
            }
            var texture = new Texture2D(device, NoiseSide, NoiseSide, false, SurfaceFormat.Alpha8);
            texture.SetData(noise);
            return texture;
        }

        static uint ScatterHash(uint value)
        {
            unchecked
            {
                value ^= value >> 16;
                value *= 0x7feb352du;
                value ^= value >> 15;
                value *= 0x846ca68bu;
                return value ^ (value >> 16);
            }
        }
    }
}
