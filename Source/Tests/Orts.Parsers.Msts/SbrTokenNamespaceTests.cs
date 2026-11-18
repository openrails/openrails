// Licensed under the GNU General Public License, version 3 or later.

using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;

namespace Tests.Orts.Parsers.Msts
{
    public class SbrTokenNamespaceTests
    {
        // Synthetic fixtures only: no route installation or proprietary files required.
        static byte[] Block(uint id, Action<BinaryWriter> payload = null, string label = "")
        {
            using (var body = new MemoryStream())
            using (var bodyWriter = new BinaryWriter(body, Encoding.Unicode, true))
            using (var result = new MemoryStream())
            using (var writer = new BinaryWriter(result, Encoding.Unicode, true))
            {
                bodyWriter.Write(checked((byte)label.Length));
                bodyWriter.Write(Encoding.Unicode.GetBytes(label));
                payload?.Invoke(bodyWriter);
                bodyWriter.Flush();
                writer.Write(id);
                writer.Write(checked((uint)body.Length));
                writer.Write(body.ToArray());
                writer.Flush();
                return result.ToArray();
            }
        }

        sealed class Fixture : IDisposable
        {
            public readonly string FileName = Path.GetTempFileName();

            public Fixture(char fileType, byte[] blocks, bool compressed)
            {
                byte[] content = Encoding.ASCII.GetBytes("JINX0" + fileType + "0b________")
                    .Concat(blocks).ToArray();
                using (var file = File.Create(FileName))
                {
                    byte[] header = Encoding.ASCII.GetBytes(compressed ? "SIMISA@F@@@@@@@@" : "SIMISA@@@@@@@@@@");
                    file.Write(header, 0, header.Length);
                    if (compressed)
                    {
                        // SBR skips the zlib header and uses a raw DEFLATE reader.
                        file.WriteByte(0x78);
                        file.WriteByte(0x9C);
                        using (var deflate = new DeflateStream(file, CompressionMode.Compress, true))
                            deflate.Write(content, 0, content.Length);
                        uint a = 1, b = 0;
                        foreach (byte value in content)
                        {
                            a = (a + value) % 65521;
                            b = (b + a) % 65521;
                        }
                        uint adler = (b << 16) | a;
                        for (int shift = 24; shift >= 0; shift -= 8)
                            file.WriteByte((byte)(adler >> shift));
                    }
                    else
                        file.Write(content, 0, content.Length);
                }
            }

            public Fixture(char fileType, string text, bool utf16)
            {
                string content = "SIMISA@@@@@@@@@@JINX0" + fileType + "0t________" + text;
                using (var writer = new StreamWriter(FileName, false, utf16 ? Encoding.Unicode : Encoding.ASCII))
                    writer.Write(content);
            }

            public void Dispose() { File.Delete(FileName); }
        }

        [Theory]
        [InlineData('t', false)]
        [InlineData('t', true)]
        [InlineData('w', false)]
        [InlineData('w', true)]
        [InlineData('s', false)]
        [InlineData('s', true)]
        public void BinaryIdsDoNotDependOnFileType(char fileType, bool compressed)
        {
            uint[] ids = { 0x00000003, 0x00040003, 0x00050003, 0x00060003,
                0x00050800, 0x00060800, 0x0006FFFF, 0x80000800, 0xFFFF0800 };
            using (var fixture = new Fixture(fileType, ids.SelectMany(id => Block(id)).ToArray(), compressed))
            using (var reader = SBR.Open(fixture.FileName))
            {
                foreach (uint id in ids)
                    using (var block = reader.ReadSubBlock())
                    {
                        Assert.Equal(id, (uint)block.ID);
                        Assert.True(block.EndOfBlock());
                    }
                Assert.True(reader.EndOfBlock());
            }
        }

        [Fact]
        public void AssignedValuesAreUniqueAndExtensionFileIdsAvoidReservedRange()
        {
            uint[] ids = Enum.GetValues(typeof(TokenID)).Cast<TokenID>().Select(id => (uint)id).ToArray();
            Assert.Equal(ids.Length, ids.Distinct().Count());
            foreach (uint id in ids.Where(id => (id >> 16) == 5 || (id >> 16) == 6))
                Assert.InRange(id & 0xFFFF, 2048u, 65535u);
            Assert.Equal(0x00050800u, (uint)TokenID.ORTSListName);
            Assert.Equal(0x0005080Eu, (uint)TokenID.Flipped);
            Assert.Equal(0x00060800u, (uint)TokenID.Ruler);
            Assert.Equal(0x0004000Du, (uint)TokenID.Wagon);
            Assert.Equal(0x0004000Eu, (uint)TokenID.Engine);
            Assert.Equal(0x000404D7u, (uint)TokenID.DEMPath);
            Assert.Equal(0x000401EDu, (uint)TokenID.TrainBrakesControllerGraduatedSelfLapLimitedKeepPsiStart);
            Assert.Equal(0x0004020Du, (uint)TokenID.EngineBrakesControllerGraduatedSelfLapLimitedStart);
            Assert.Equal(0x0004020Eu, (uint)TokenID.EngineBrakesControllerGraduatedSelfLapLimitedHoldingStart);
            Assert.Equal(0x0004020Fu, (uint)TokenID.EngineBrakesControllerGraduatedSelfLapLimitedKeepPsiStart);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EveryUnicodeNameMatchesItsBinaryId(bool utf16)
        {
            TokenID[] ids = Enum.GetValues(typeof(TokenID)).Cast<TokenID>().ToArray();
            // "comment" is intentionally skipped by the Unicode parser.
            ids = ids.Where(id => id != TokenID.comment).ToArray();
            string text = string.Join("\n", ids.Select(id => id + " ( )"));
            using (var unicode = new Fixture('w', text, utf16))
            using (var binary = new Fixture('w', ids.SelectMany(id => Block((uint)id)).ToArray(), false))
            using (var textReader = SBR.Open(unicode.FileName))
            using (var binaryReader = SBR.Open(binary.FileName))
            {
                foreach (TokenID id in ids)
                {
                    using (var textBlock = textReader.ReadSubBlock())
                    using (var binaryBlock = binaryReader.ReadSubBlock())
                    {
                        Assert.Equal(id, textBlock.ID);
                        Assert.Equal(id, binaryBlock.ID);
                        textBlock.Skip();
                        binaryBlock.Skip();
                    }
                }
                Assert.True(textReader.EndOfBlock());
                Assert.True(binaryReader.EndOfBlock());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LabelsPayloadFlagsAndUnknownNestedBlocksRemainAligned(bool compressed)
        {
            byte[] root = Block((uint)TokenID.terrain, writer =>
            {
                writer.Write(Block(0x00060800, unknown => unknown.Write(new byte[] { 1, 2, 3, 4, 5 }), "extension"));
                writer.Write(Block((uint)TokenID.terrain_nsamples, known => known.Write(512)));
                writer.Write(Block((uint)TokenID.flags, flags => flags.Write(0xDEADBEEFu)));
            }, "tile");
            using (var fixture = new Fixture('t', root, compressed))
            using (var reader = SBR.Open(fixture.FileName))
            using (var parent = reader.ReadSubBlock())
            {
                Assert.Equal("tile", parent.Label);
                using (var unknown = parent.ReadSubBlock())
                {
                    Assert.Equal(0x00060800u, (uint)unknown.ID);
                    Assert.Equal("extension", unknown.Label);
                    unknown.Skip();
                }
                using (var samples = parent.ReadSubBlock())
                {
                    Assert.Equal(TokenID.terrain_nsamples, samples.ID);
                    Assert.Equal(512, samples.ReadInt());
                }
                using (var flags = parent.ReadSubBlock())
                    Assert.Equal(0xDEADBEEFu, flags.ReadFlags());
                Assert.True(parent.EndOfBlock());
                Assert.True(reader.EndOfBlock());
            }
        }

        public static IEnumerable<object[]> WorldForms()
        {
            yield return new object[] { TokenID.Static, 0x00040003u, typeof(StaticObj) };
            yield return new object[] { TokenID.TrackObj, 0x00040005u, typeof(TrackObj) };
            yield return new object[] { TokenID.CarSpawner, 0x00040039u, typeof(CarSpawnerObj) };
            yield return new object[] { TokenID.Siding, 0x0004003Du, typeof(SidingObj) };
            yield return new object[] { TokenID.Dyntrack, 0x00040006u, typeof(DyntrackObj) };
            yield return new object[] { TokenID.Transfer, 0x0004003Fu, typeof(TransferObj) };
            yield return new object[] { TokenID.Gantry, 0x00040038u, typeof(BaseObj) };
            yield return new object[] { TokenID.Pickup, 0x0004003Bu, typeof(PickupObj) };
        }

        [Theory]
        [MemberData(nameof(WorldForms))]
        public void CanonicalWorldFormsLoadInBinaryAndUnicode(TokenID id, uint packedId, Type objectType)
        {
            Assert.Equal(packedId, (uint)id);
            byte[] bytes = Block(0x0004004B, writer => writer.Write(Block(packedId,
                obj => obj.Write(Block((uint)TokenID.UiD, uid => uid.Write(123u))))));
            string text = "Tr_Worldfile ( " + id + " ( UiD ( 123 ) ) )";
            using (var binary = new Fixture('w', bytes, false))
            using (var unicode = new Fixture('w', text, true))
            {
                foreach (var fixture in new[] { binary, unicode })
                    using (var reader = SBR.Open(fixture.FileName))
                    using (var root = reader.ReadSubBlock())
                    {
                        var world = new Tr_Worldfile(root, fixture.FileName, null);
                        var obj = Assert.Single(world);
                        Assert.Equal(objectType, obj.GetType());
                        Assert.Equal(123u, obj.UID);
                    }
            }
        }

        [Fact]
        public void UnknownNamespaceWorldObjectIsNotStaticAndDoesNotHideNextObject()
        {
            AssertWarnings.Expected();
            byte[] bytes = Block(0x0004004B, writer =>
            {
                writer.Write(Block(0x00060003, unknown => unknown.Write(1234)));
                writer.Write(Block(0x00040003, obj => obj.Write(Block((uint)TokenID.UiD, uid => uid.Write(456u)))));
            });
            using (var fixture = new Fixture('w', bytes, true))
            using (var reader = SBR.Open(fixture.FileName))
            using (var root = reader.ReadSubBlock())
            {
                var obj = Assert.Single(new Tr_Worldfile(root, fixture.FileName, null));
                Assert.IsType<StaticObj>(obj);
                Assert.Equal(456u, obj.UID);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OrtsExtensionInsideMstsObjectLoadsInBinaryAndUnicode(bool compressed)
        {
            byte[] bytes = Block(0x0004004B, writer => writer.Write(Block(0x00040039,
                obj => obj.Write(Block(0x00050800, list =>
                {
                    list.Write((ushort)5);
                    list.Write(Encoding.Unicode.GetBytes("urban"));
                })))));
            using (var binary = new Fixture('w', bytes, compressed))
            using (var unicode = new Fixture('w', "Tr_Worldfile ( CarSpawner ( ORTSListName ( urban ) ) )", true))
            {
                foreach (var fixture in new[] { binary, unicode })
                    using (var reader = SBR.Open(fixture.FileName))
                    using (var root = reader.ReadSubBlock())
                    {
                        var obj = Assert.Single(new Tr_Worldfile(root, fixture.FileName, null));
                        Assert.Equal("urban", Assert.IsType<CarSpawnerObj>(obj).ListName);
                    }
            }
        }

        [Fact]
        public void UnknownTerrainRootChildIsSkippedAndSamplesStillLoad()
        {
            AssertWarnings.Expected();
            byte[] bytes = Block((uint)TokenID.terrain, writer =>
            {
                writer.Write(Block(0x0006008B, unknown => unknown.Write(1234)));
                writer.Write(Block((uint)TokenID.terrain_samples,
                    samples => samples.Write(Block((uint)TokenID.terrain_nsamples, n => n.Write(1024)))));
            });
            using (var fixture = new Fixture('t', bytes, false))
                Assert.Equal(1024, new TerrainFile(fixture.FileName).terrain.terrain_samples.terrain_nsamples);
        }

        [Fact]
        public void UnknownTerrainSamplesChildStillUsesTheExistingStrictPolicy()
        {
            AssertWarnings.Expected();
            byte[] bytes = Block((uint)TokenID.terrain, writer => writer.Write(
                Block((uint)TokenID.terrain_samples, samples => samples.Write(
                    Block(0x00060800)))));
            using (var fixture = new Fixture('t', bytes, false))
                Assert.Throws<InvalidDataException>(() => new TerrainFile(fixture.FileName));
        }
    }
}
