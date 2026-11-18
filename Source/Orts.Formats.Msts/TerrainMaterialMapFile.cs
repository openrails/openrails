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
        public const int MinimumSide = 2048;
        public const int DefaultSide = 4096;
        public const int MaximumSide = 8192;
        public const int HeaderSize = 20;
        public const int MaximumFileSize = MaximumSide * MaximumSide + 65536;

        public readonly int Side;
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
            int side;
            MaterialIds = Decode(File.ReadAllBytes(filename), out side);
            Side = side;
        }

        public TerrainMaterialMapFile(byte[] file)
        {
            int side;
            MaterialIds = Decode(file, out side);
            Side = side;
        }

        public static byte[] Decode(byte[] file)
        {
            int side;
            return Decode(file, out side);
        }

        public static byte[] Decode(byte[] file, out int side)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            side = 0;
            if (file.Length <= HeaderSize || file.Length > MaximumFileSize ||
                Encoding.ASCII.GetString(file, 0, 8) != "TSREPMAP" ||
                ReadUInt32(file, 8) != 1)
                throw new InvalidDataException("Unsupported procedural terrain material map header or size");

            uint width = ReadUInt32(file, 12);
            uint height = ReadUInt32(file, 16);
            if (width != height || width < MinimumSide || width > MaximumSide ||
                (width & (width - 1)) != 0)
                throw new InvalidDataException("Unsupported procedural terrain material map dimensions");
            side = checked((int)width);
            byte[] decoded = new byte[checked(side * side)];
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

        TerrainMaterialMapFile(int side, byte[] materialIds)
        {
            Side = side;
            MaterialIds = materialIds;
        }

        public TerrainMaterialMapFile WithMaximumSide(int maximumSide)
        {
            if (maximumSide < MinimumSide)
                throw new ArgumentOutOfRangeException(nameof(maximumSide));
            int targetSide = Side;
            while (targetSide > maximumSide)
                targetSide /= 2;
            if (targetSide == Side)
                return this;
            return new TerrainMaterialMapFile(targetSide,
                ReduceNearest(MaterialIds, Side, targetSide));
        }

        public static byte[] ReduceNearest(byte[] source, int sourceSide, int targetSide)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (sourceSide <= 0 || targetSide <= 0 || sourceSide % targetSide != 0 ||
                source.Length != checked(sourceSide * sourceSide))
                throw new ArgumentOutOfRangeException(nameof(targetSide));

            int factor = sourceSide / targetSide;
            var result = new byte[checked(targetSide * targetSide)];
            int sampleOffset = factor / 2;
            for (int targetZ = 0; targetZ < targetSide; ++targetZ)
            {
                int sourceOffset = (targetZ * factor + sampleOffset) * sourceSide +
                    sampleOffset;
                int targetOffset = targetZ * targetSide;
                for (int targetX = 0; targetX < targetSide; ++targetX)
                    result[targetOffset + targetX] = source[sourceOffset + targetX * factor];
            }
            return result;
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
