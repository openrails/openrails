// Licensed under the GNU General Public License, version 3 or later.

using System;
using System.IO;
using System.IO.Compression;
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

        public TerrainMaterialMapFile(string filename)
        {
            var info = new FileInfo(filename);
            if (!info.Exists || info.Length <= HeaderSize || info.Length > MaximumFileSize)
                throw new InvalidDataException("Unsupported procedural terrain material map size");
            MaterialIds = Decode(File.ReadAllBytes(filename));
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

        static uint ReadUInt32(byte[] bytes, int offset)
        {
            return (uint)(bytes[offset] | bytes[offset + 1] << 8 |
                bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
        }
    }
}
