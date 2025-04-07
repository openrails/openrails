// COPYRIGHT 2013 by the Open Rails project.
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
using System.IO;
using System.IO.Compression;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ORTS.IO;
using ORTS.Common;

namespace Orts.Formats.Msts
{
    public class AceInfo
    {
        public byte AlphaBits;
    }
    public class AceFile
    {
        public static Texture2D Texture2DFromFile(GraphicsDevice graphicsDevice, string fileName)
        {
            using (var stream = Vfs.OpenRead(fileName))
                return Texture2DFromStream(graphicsDevice, stream);
        }

        static Texture2D Texture2DFromStream(GraphicsDevice graphicsDevice, Stream stream)
        {
            using (var reader = new BinaryReader(stream, ByteEncoding.Encoding))
            {
                var signature = new String(reader.ReadChars(8));
                if (signature == "SIMISA@F")
                {
                    // Compressed header has the uncompressed size embedded in the @-padding.
                    reader.ReadUInt32();
                    signature = new String(reader.ReadChars(4));
                    if (signature != "@@@@") throw new InvalidDataException(String.Format("Incorrect signature; expected '@@@@', got '{0}'", signature));

                    // The stream is technically ZLIB, but we assume the selected ZLIB compression is DEFLATE (though we verify that here just in case). The ZLIB
                    // header for DEFLATE is 0x78 0x9C.
                    var zlib = reader.ReadUInt16();
                    if ((zlib & 0x20FF) != 0x0078) throw new InvalidDataException(String.Format("Incorrect signature; expected 'xx78', got '{0:X4}'", zlib));

                    // The BufferedInMemoryStream is needed because DeflateStream only supports reading forwards - no seeking.
                    return Texture2DFromReader(graphicsDevice, new BinaryReader(new BufferedInMemoryStream(new DeflateStream(stream, CompressionMode.Decompress))));
                }
                if (signature == "SIMISA@@")
                {
                    // Uncompressed header is all @-padding.
                    signature = new String(reader.ReadChars(8));
                    if (signature != "@@@@@@@@") throw new InvalidDataException(String.Format("Incorrect signature; expected '@@@@@@@@', got '{0}'", signature));

                    // Start reading the texture from the same reader.
                    return Texture2DFromReader(graphicsDevice, reader);
                }
                throw new InvalidDataException(String.Format("Incorrect signature; expected 'SIMISA@F' or 'SIMISA@@', got '{0}'", signature));
            }
        }

        static Texture2D Texture2DFromReader(GraphicsDevice graphicsDevice, BinaryReader reader)
        {
            var signature = new String(reader.ReadChars(4));
            if (signature != "\x01\x00\x00\x00") throw new InvalidDataException(String.Format("Incorrect signature; expected '01 00 00 00', got '{0}'", StringToHex(signature)));
            var options = (SimisAceFormatOptions)reader.ReadInt32();
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var surfaceFormat = reader.ReadInt32();
            var channelCount = reader.ReadInt32();
            reader.ReadBytes(128); // Miscellaneous other data we don't care about.

            // If there are mipmaps, we must validate that the image is square and dimensions are an integral power-of-two.
            if ((options & SimisAceFormatOptions.MipMaps) != 0)
            {
                if (width != height) throw new InvalidDataException(String.Format("Dimensions must match when mipmaps are used; got {0}x{1}", width, height));
                if (width == 0 || (width & (width - 1)) != 0) throw new InvalidDataException(String.Format("Width must be an integral power of 2 when mipmaps are used; got {0}", width));
                if (height == 0 || (height & (height - 1)) != 0) throw new InvalidDataException(String.Format("Height must be an integral power of 2 when mipmaps are used; got {0}", height));
            }

            // If the data is raw data, we must be able to convert the Ace format in to an XNA format or we'll blow up.
            var textureFormat = SurfaceFormat.Color;
            if ((options & SimisAceFormatOptions.RawData) != 0)
            {
                if (!SimisAceSurfaceFormats.ContainsKey(surfaceFormat)) throw new InvalidDataException(String.Format("Unsupported surface format {0:X8}", surfaceFormat));
                textureFormat = SimisAceSurfaceFormats[surfaceFormat];
            }

            // Calculate how many images we're going to load; 1 for non-mipmapped, 1+log(width)/log(2) for mipmapped.
            var imageCount = 1 + (int)((options & SimisAceFormatOptions.MipMaps) != 0 ? Math.Log(width) / Math.Log(2) : 0);
            Texture2D texture;
            if ((options & SimisAceFormatOptions.MipMaps) == 0)
                texture = new Texture2D(graphicsDevice, width, height, false, textureFormat);
            else
                texture = new Texture2D(graphicsDevice, width, height, true, textureFormat);

            // Read in the color channels; each one defines a size (in bits) and type (reg, green, blue, mask, alpha).
            var channels = new List<SimisAceChannel>();
            for (var channel = 0; channel < channelCount; channel++)
            {
                var size = (byte)reader.ReadUInt64();
                if ((size != 1) && (size != 8)) throw new InvalidDataException(String.Format("Unsupported color channel size {0}", size));
                var type = reader.ReadUInt64();
                if ((type < 2) || (type > 6)) throw new InvalidDataException(String.Format("Unknown color channel type {0}", type));
                channels.Add(new SimisAceChannel(size, (SimisAceChannelId)type));
            }

            // Construct some info about this texture for the game to use in optimisations.
            var aceInfo = new AceInfo();
            texture.Tag = aceInfo;
            if (channels.Any(c => c.Type == SimisAceChannelId.Alpha))
                aceInfo.AlphaBits = 8;
            else if (channels.Any(c => c.Type == SimisAceChannelId.Mask))
                aceInfo.AlphaBits = 1;

            if ((options & SimisAceFormatOptions.RawData) != 0)
            {
                // Raw data is stored as a table of 32bit int offsets to each mipmap level.
                reader.ReadBytes(imageCount * 4);

                var buffer = new byte[0];
                for (var imageIndex = 0; imageIndex < imageCount; imageIndex++)
                {
                    var imageWidth = width / (int)Math.Pow(2, imageIndex);
                    var imageHeight = height / (int)Math.Pow(2, imageIndex);

                    // If the mipmap level is width>=4, it is stored as raw data with a 32bit int length header.
                    // Otherwise, it is stored as a 32bit ARGB block.
                    if (imageWidth >= 4 && imageHeight >= 4)
                        buffer = reader.ReadBytes(reader.ReadInt32());

                    // For <4 pixels the images are in RGB format. There's no point reading them though, as the
                    // API accepts the 4x4 image's data for the 2x2 and 1x1 case. They do need to be set though!

                    texture.SetData(imageIndex, null, buffer, 0, buffer.Length);
                }
            }
            else
            {
                // Structured data is stored as a table of 32bit offsets to each scanline of each image.
                for (var imageIndex = 0; imageIndex < imageCount; imageIndex++)
                    reader.ReadBytes(4 * height / (int)Math.Pow(2, imageIndex));

                var buffer = new int[width * height];
                var channelBuffers = new byte[8][];
                for (var imageIndex = 0; imageIndex < imageCount; imageIndex++)
                {
                    var imageWidth = width / (int)Math.Pow(2, imageIndex);
                    var imageHeight = height / (int)Math.Pow(2, imageIndex);
                    for (var y = 0; y < imageHeight; y++)
                    {
                        foreach (var channel in channels)
                        {
                            if (channel.Size == 1)
                            {
                                // 1bpp channels start with the MSB and work down to LSB and then the next byte.
                                var bytes = reader.ReadBytes((int)Math.Ceiling((double)channel.Size * imageWidth / 8));
                                channelBuffers[(int)channel.Type] = new byte[imageWidth];
                                for (var x = 0; x < imageWidth; x++)
                                    channelBuffers[(int)channel.Type][x] = (byte)(((bytes[x / 8] >> (7 - (x % 8))) & 1) * 0xFF);
                            }
                            else
                            {
                                // 8bpp are simple.
                                channelBuffers[(int)channel.Type] = reader.ReadBytes(imageWidth);
                            }
                        }
                        for (var x = 0; x < imageWidth; x++)
                        {
                            buffer[imageWidth * y + x] = channelBuffers[(int)SimisAceChannelId.Red][x] + (channelBuffers[(int)SimisAceChannelId.Green][x] << 8) + (channelBuffers[(int)SimisAceChannelId.Blue][x] << 16);
                            if (channelBuffers[(int)SimisAceChannelId.Alpha] != null)
                                buffer[imageWidth * y + x] += channelBuffers[(int)SimisAceChannelId.Alpha][x] << 24;
                            else if (channelBuffers[(int)SimisAceChannelId.Mask] != null)
                                buffer[imageWidth * y + x] += channelBuffers[(int)SimisAceChannelId.Mask][x] << 24;
                            else
                                buffer[imageWidth * y + x] += (0xFF << 24);
                        }
                    }
                    texture.SetData(imageIndex, null, buffer, 0, imageWidth * imageHeight);
                }
            }

            return texture;
        }

        static string StringToHex(string signature)
        {
            return String.Join(" ", signature.ToCharArray().Select(c => ((byte)c).ToString("X2")).ToArray());
        }

        // This is a mapping between the 'surface format' found in ACE files and XNA's enum.
        static readonly Dictionary<int, SurfaceFormat> SimisAceSurfaceFormats = new Dictionary<int, SurfaceFormat>()
        {
            { 0x0E, SurfaceFormat.Bgr565 },
            { 0x10, SurfaceFormat.Bgra5551 },
            { 0x11, SurfaceFormat.Bgra4444 },
            { 0x12, SurfaceFormat.Dxt1 },
            { 0x14, SurfaceFormat.Dxt3 },
            { 0x16, SurfaceFormat.Dxt5 },
        };
    }

    [Flags]
    public enum SimisAceFormatOptions
    {
        Default = 0,
        MipMaps = 0x01,
        RawData = 0x10,
    }

    public enum SimisAceChannelId
    {
        Mask = 2,
        Red = 3,
        Green = 4,
        Blue = 5,
        Alpha = 6,
    }

    public class SimisAceChannel
    {
        public readonly int Size;
        public readonly SimisAceChannelId Type;

        public SimisAceChannel(int size, SimisAceChannelId type)
        {
            Size = size;
            Type = type;
        }
    }

    public class SimisAceImage
    {
        public readonly int[] Color;
        public readonly int[] Mask;

        public SimisAceImage(int[] color, int[] mask)
        {
            Color = color;
            Mask = mask;
        }
    }
}
