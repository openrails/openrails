// Licensed under the GNU General Public License, version 3 or later.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Orts.Formats.Msts
{
    public sealed class TerrainMaterialMapFile
    {
        public const int Side = 4096;
        public const int HeaderSize = 20;
        public const int DecodedSize = Side * Side;
        public const int MaximumFileSize = DecodedSize + 65536;

        public readonly byte[] MaterialIds;

        public sealed class PatchCoverage
        {
            public readonly IReadOnlyList<KeyValuePair<byte, int>> Materials;

            internal PatchCoverage(IEnumerable<KeyValuePair<byte, int>> materials)
            {
                Materials = materials.OrderByDescending(item => item.Value)
                    .ThenBy(item => item.Key).ToArray();
            }
        }

        public TerrainMaterialMapFile(string filename)
        {
            var info = new FileInfo(filename);
            if (!info.Exists || info.Length <= HeaderSize || info.Length > MaximumFileSize)
                throw new InvalidDataException("Unsupported procedural terrain material map size");
            MaterialIds = Decode(File.ReadAllBytes(filename));
        }

        public TerrainMaterialMapFile(byte[] file)
        {
            MaterialIds = Decode(file);
        }

        public static byte[] Decode(byte[] file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (file.Length <= HeaderSize || file.Length > MaximumFileSize ||
                Encoding.ASCII.GetString(file, 0, 8) != "TSREPMAP" ||
                ReadUInt32(file, 8) != 1 || ReadUInt32(file, 12) != Side ||
                ReadUInt32(file, 16) != Side)
                throw new InvalidDataException("Unsupported procedural terrain material map header or size");

            byte[] decoded = new byte[DecodedSize];
            using (var input = new MemoryStream(file, HeaderSize, file.Length - HeaderSize, false))
            using (var compressed = new ZLibStream(input, CompressionMode.Decompress))
            {
                int total = 0;
                while (total < decoded.Length)
                {
                    int count = compressed.Read(decoded, total, decoded.Length - total);
                    if (count == 0)
                        throw new InvalidDataException("Procedural terrain material map has incomplete data");
                    total += count;
                }
                if (compressed.ReadByte() != -1)
                    throw new InvalidDataException("Procedural terrain material map exceeds its declared size");
            }
            return decoded;
        }

        public PatchCoverage[] AnalyzePatches(int patchCount)
        {
            if (patchCount <= 0 || patchCount > 32 || Side % patchCount != 0)
                throw new ArgumentOutOfRangeException(nameof(patchCount));

            int patchSide = Side / patchCount;
            var result = new PatchCoverage[patchCount * patchCount];
            var counts = new int[256];
            var candidates = new bool[256];

            for (int patchZ = 0; patchZ < patchCount; ++patchZ)
            {
                for (int patchX = 0; patchX < patchCount; ++patchX)
                {
                    Array.Clear(counts, 0, counts.Length);
                    Array.Clear(candidates, 0, candidates.Length);
                    int startX = patchX * patchSide;
                    int startZ = patchZ * patchSide;

                    for (int z = startZ; z < startZ + patchSide; ++z)
                    {
                        int offset = z * Side + startX;
                        for (int x = 0; x < patchSide; ++x)
                        {
                            byte id = MaterialIds[offset + x];
                            ++counts[id];
                            candidates[id] = true;
                        }
                    }

                    // Categorical interpolation can select a sample immediately outside
                    // the patch. Retain a one-texel halo as a zero-coverage candidate so
                    // the renderer never leaves a seam at a patch boundary.
                    int haloStartX = Math.Max(0, startX - 1);
                    int haloEndX = Math.Min(Side - 1, startX + patchSide);
                    int haloStartZ = Math.Max(0, startZ - 1);
                    int haloEndZ = Math.Min(Side - 1, startZ + patchSide);
                    for (int x = haloStartX; x <= haloEndX; ++x)
                    {
                        candidates[MaterialIds[haloStartZ * Side + x]] = true;
                        candidates[MaterialIds[haloEndZ * Side + x]] = true;
                    }
                    for (int z = haloStartZ + 1; z < haloEndZ; ++z)
                    {
                        candidates[MaterialIds[z * Side + haloStartX]] = true;
                        candidates[MaterialIds[z * Side + haloEndX]] = true;
                    }

                    var materials = new List<KeyValuePair<byte, int>>();
                    for (int id = 0; id < candidates.Length; ++id)
                        if (candidates[id])
                            materials.Add(new KeyValuePair<byte, int>((byte)id, counts[id]));
                    result[patchZ * patchCount + patchX] = new PatchCoverage(materials);
                }
            }
            return result;
        }

        static uint ReadUInt32(byte[] bytes, int offset)
        {
            return (uint)(bytes[offset] | bytes[offset + 1] << 8 |
                bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
        }
    }
}
