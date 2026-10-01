// Licensed under the GNU General Public License, version 3 or later.

using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;

namespace Tests.Orts.Formats.Msts
{
    public class TerrainMaterialTests
    {
        sealed class TemporaryFile : IDisposable
        {
            public readonly string FileName = Path.GetTempFileName();

            public TemporaryFile(byte[] content)
            {
                File.WriteAllBytes(FileName, content);
            }

            public TemporaryFile(string content)
            {
                File.WriteAllText(FileName, content, Encoding.Unicode);
            }

            public void Dispose()
            {
                File.Delete(FileName);
            }
        }

        static byte[] Block(uint id, Action<BinaryWriter> payload = null)
        {
            using (var body = new MemoryStream())
            using (var bodyWriter = new BinaryWriter(body, Encoding.Unicode, true))
            using (var result = new MemoryStream())
            using (var writer = new BinaryWriter(result, Encoding.Unicode, true))
            {
                bodyWriter.Write((byte)0);
                payload?.Invoke(bodyWriter);
                bodyWriter.Flush();
                writer.Write(id);
                writer.Write(checked((uint)body.Length));
                writer.Write(body.ToArray());
                writer.Flush();
                return result.ToArray();
            }
        }

        static byte[] TerrainFixture(byte[] children)
        {
            return Encoding.ASCII.GetBytes("SIMISA@@@@@@@@@@JINX0t0b________")
                .Concat(Block((uint)TokenID.terrain, writer => writer.Write(children))).ToArray();
        }

        static byte[] MaterialBuffer(string value)
        {
            return Block((uint)TokenID.TSRETerrainMaterialBuffer, writer =>
            {
                writer.Write(checked((ushort)value.Length));
                writer.Write(Encoding.Unicode.GetBytes(value));
            });
        }

        static byte[] MaterialMap(params uint[] idUidPairs)
        {
            return Block((uint)TokenID.TSRETerrainMaterialMap, writer =>
            {
                writer.Write(checked((uint)idUidPairs.Length / 2));
                foreach (uint value in idUidPairs)
                    writer.Write(value);
            });
        }

        static byte[] Samples(int count)
        {
            return Block((uint)TokenID.terrain_samples, writer =>
                writer.Write(Block((uint)TokenID.terrain_nsamples, value => value.Write(count))));
        }

        static byte[] MaterialContainer(params byte[][] children)
        {
            return Block((uint)TokenID.TSRETerrainMaterials, writer =>
            {
                foreach (byte[] child in children)
                    writer.Write(child);
            });
        }

        [Fact]
        public void TerrainMaterialContainerLoadsWithoutChangingNormalTerrainData()
        {
            byte[] extension = MaterialContainer(MaterialBuffer("tile_materials.pmap"),
                MaterialMap(0, 1, 7, 42));
            using (var fixture = new TemporaryFile(TerrainFixture(extension.Concat(Samples(512)).ToArray())))
            {
                var file = new TerrainFile(fixture.FileName);
                Assert.Equal(512, file.terrain.terrain_samples.terrain_nsamples);
                Assert.True(file.terrain.terrain_materials.IsValid);
                Assert.Equal("tile_materials.pmap", file.terrain.terrain_materials.MaterialBuffer);
                Assert.Equal(1u, file.terrain.terrain_materials.MaterialUids[0]);
                Assert.Equal(42u, file.terrain.terrain_materials.MaterialUids[7]);
            }
        }

        [Fact]
        public void DuplicateMaterialMapDisablesOnlyProceduralTerrain()
        {
            byte[] extension = MaterialContainer(MaterialBuffer("tile_materials.pmap"),
                MaterialMap(0, 1), MaterialMap(1, 2));
            using (var fixture = new TemporaryFile(TerrainFixture(extension.Concat(Samples(256)).ToArray())))
            {
                var file = new TerrainFile(fixture.FileName);
                Assert.Equal(256, file.terrain.terrain_samples.terrain_nsamples);
                Assert.False(file.terrain.terrain_materials.IsValid);
                Assert.Contains("Duplicate", file.terrain.terrain_materials.Error);
            }
        }

        [Fact]
        public void TerrainMaterialCatalogueLoadsDefinitions()
        {
            string text = "SIMISA@@@@@@@@@@JINX0t1t______\n\n" +
                "TSRE_Terrain_Materials ( Version ( 1 ) NextUiD ( 3 ) " +
                "Material ( UiD ( 1 ) Name ( \"Grass\" ) Texture ( \"grass.ace\" ) ) " +
                "Material ( UiD ( 2 ) Name ( \"Rock\" ) Texture ( \"ground/rock.dds\" ) ) )";
            using (var fixture = new TemporaryFile(text))
            {
                var file = new TerrainMaterialFile(fixture.FileName);
                Assert.Equal(3u, file.NextUid);
                Assert.Equal("Grass", file.Materials[1].Name);
                Assert.Equal("ground/rock.dds", file.Materials[2].Texture);
            }
        }

        [Theory]
        [InlineData("Material ( UiD ( 1 ) Name ( \"A\" ) Texture ( \"a.ace\" ) ) Material ( UiD ( 1 ) Name ( \"B\" ) Texture ( \"b.ace\" ) )")]
        [InlineData("Material ( UiD ( 1 ) Name ( \"A\" ) Texture ( \"../a.ace\" ) )")]
        public void TerrainMaterialCatalogueRejectsAmbiguousOrUnsafeDefinitions(string materials)
        {
            string text = "SIMISA@@@@@@@@@@JINX0t1t______\n\n" +
                "TSRE_Terrain_Materials ( Version ( 1 ) NextUiD ( 2 ) " + materials + " )";
            using (var fixture = new TemporaryFile(text))
                Assert.Throws<STFException>(() => new TerrainMaterialFile(fixture.FileName));
        }

        [Fact]
        public void PmapDecoderRequiresExactBoundedOutput()
        {
            byte[] ids = new byte[TerrainMaterialMapFile.DecodedSize];
            ids[0] = 3;
            ids[ids.Length - 1] = 9;
            byte[] encoded = EncodePmap(ids);
            Assert.Equal(ids, TerrainMaterialMapFile.Decode(encoded));

            byte[] shortMap = EncodePmap(new byte[1024]);
            Assert.Throws<InvalidDataException>(() => TerrainMaterialMapFile.Decode(shortMap));
        }

        static byte[] EncodePmap(byte[] ids)
        {
            using (var result = new MemoryStream())
            using (var writer = new BinaryWriter(result, Encoding.ASCII, true))
            {
                writer.Write(Encoding.ASCII.GetBytes("TSREPMAP"));
                writer.Write(1u);
                writer.Write((uint)TerrainMaterialMapFile.Side);
                writer.Write((uint)TerrainMaterialMapFile.Side);
                writer.Flush();
                using (var compressed = new ZLibStream(result, CompressionLevel.Fastest, true))
                    compressed.Write(ids, 0, ids.Length);
                return result.ToArray();
            }
        }
    }
}
