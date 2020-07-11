// COPYRIGHT 2009, 2010, 2011, 2013 by the Open Rails project.
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

using System.IO;
using System.Text;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    public class terrain_water_height_offset
    {
        public readonly float SW;
        public readonly float SE;
        public readonly float NE;
        public readonly float NW;

        public terrain_water_height_offset(SBR block)
        {
            block.VerifyID(TokenID.terrain_water_height_offset);
            if (!block.EndOfBlock()) SW = block.ReadFloat();
            if (!block.EndOfBlock()) SE = block.ReadFloat();
            if (!block.EndOfBlock()) NE = block.ReadFloat();
            if (!block.EndOfBlock()) NW = block.ReadFloat();
        }
    }

    public class terrain
    {
        public readonly float terrain_errthreshold_scale = 1;
        public readonly terrain_water_height_offset terrain_water_height_offset;
        public readonly float terrain_alwaysselect_maxdist;
        public readonly terrain_samples terrain_samples;
        public readonly terrain_shader[] terrain_shaders;
        public readonly terrain_patchset[] terrain_patchsets;

        public terrain(SBR block)
        {
            block.VerifyID(TokenID.terrain);
            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.terrain_errthreshold_scale:
                            terrain_errthreshold_scale = subBlock.ReadFloat();
                            break;
                        case TokenID.terrain_water_height_offset:
                            terrain_water_height_offset = new terrain_water_height_offset(subBlock);
                            break;
                        case TokenID.terrain_alwaysselect_maxdist:
                            terrain_alwaysselect_maxdist = subBlock.ReadFloat();
                            break;
                        case TokenID.terrain_samples:
                            terrain_samples = new terrain_samples(subBlock);
                            break;
                        case TokenID.terrain_shaders:
                            terrain_shaders = new terrain_shader[subBlock.ReadInt()];
                            for (var i = 0; i < terrain_shaders.Length; ++i)
                                using (var terrain_shadersBlock = subBlock.ReadSubBlock())
                                    terrain_shaders[i] = new terrain_shader(terrain_shadersBlock);
                            if (!subBlock.EndOfBlock())
                                subBlock.Skip();
                            break;
                        case TokenID.terrain_patches:
                            using (var patch_sets_Block = subBlock.ReadSubBlock())
                            {
                                terrain_patchsets = new terrain_patchset[patch_sets_Block.ReadInt()];
                                for (var i = 0; i < terrain_patchsets.Length; ++i)
                                    using (var terrain_patchsetBlock = patch_sets_Block.ReadSubBlock())
                                        terrain_patchsets[i] = new terrain_patchset(terrain_patchsetBlock);
                                if (!subBlock.EndOfBlock())
                                    subBlock.Skip();
                            }
                            break;
                    }
                }
            }
        }
    }

    // TODO fails on = "c:\\program files\\microsoft games\\train simulator\\ROUTES\\EUROPE1\\Tiles\\-11cc0604.t") Line 330 + 0x18 bytes	C#

    public class terrain_samples
    {
        public readonly int terrain_nsamples;
        public readonly float terrain_sample_rotation;
        public readonly float terrain_sample_floor;
        public readonly float terrain_sample_scale;
        public readonly float terrain_sample_size;
        public readonly string terrain_sample_ybuffer;
        public readonly string terrain_sample_ebuffer;
        public readonly string terrain_sample_nbuffer;

        public terrain_samples(SBR block)
        {
            block.VerifyID(TokenID.terrain_samples);
            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.terrain_nsamples:
                            terrain_nsamples = subBlock.ReadInt();
                            break;
                        case TokenID.terrain_sample_rotation:
                            terrain_sample_rotation = subBlock.ReadFloat();
                            break;
                        case TokenID.terrain_sample_floor:
                            terrain_sample_floor = subBlock.ReadFloat();
                            break;
                        case TokenID.terrain_sample_scale:
                            terrain_sample_scale = subBlock.ReadFloat();
                            break;
                        case TokenID.terrain_sample_size:
                            terrain_sample_size = subBlock.ReadFloat();
                            break;
                        case TokenID.terrain_sample_ybuffer:
                            terrain_sample_ybuffer = subBlock.ReadString();
                            break;
                        case TokenID.terrain_sample_ebuffer:
                            terrain_sample_ebuffer = subBlock.ReadString();
                            break;
                        case TokenID.terrain_sample_nbuffer:
                            terrain_sample_nbuffer = subBlock.ReadString();
                            break;
                        case TokenID.terrain_sample_asbuffer:
                            subBlock.Skip(); // TODO parse this
                            break;
                        case TokenID.terrain_sample_fbuffer:
                            subBlock.Skip(); // TODO parse this
                            break;
                        case (TokenID)282:  // TODO figure out what this is and handle it
                            subBlock.Skip();
                            break;
                        default:
                            throw new InvalidDataException("Unknown token " + subBlock.ID.ToString());
                    }
                }
            }
        }
    }

    public class terrain_texslot
    {
        public readonly string Filename;
        public readonly int A;
        public readonly int B;

        public terrain_texslot(SBR block)
        {
            block.VerifyID(TokenID.terrain_texslot);
            Filename = block.ReadString();
            A = block.ReadInt();
            B = block.ReadInt();
            block.Skip();
        }
    }

    public class terrain_uvcalc
    {
        public readonly int A;
        public readonly int B;
        public readonly int C;
        public readonly int D;

        public terrain_uvcalc(SBR block)
        {
            block.VerifyID(TokenID.terrain_uvcalc);
            A = block.ReadInt();
            B = block.ReadInt();
            C = block.ReadInt();
            D = (int)block.ReadFloat();
        }
    }

    public class terrain_patchset_patch
    {
        public readonly uint Flags;  // 1 = don't draw, C0 = draw water
        public readonly float CenterX, CenterZ;
        public readonly float AverageY, RangeY, FactorY;  // don't really know the purpose of these
        public readonly int ShaderIndex;
        public readonly float ErrorBias;
        public readonly float X, Y, W, H, B, C;  // texture coordinates
        public readonly float RadiusM;

        public bool WaterEnabled { get { return (Flags & 0xC0) != 0; } }
        public bool DrawingEnabled { get { return (Flags & 1) == 0; } }

        public terrain_patchset_patch(SBR block)
        {
            block.VerifyID(TokenID.terrain_patchset_patch);
            Flags = block.ReadUInt();
            CenterX = block.ReadFloat();    // 64
            AverageY = block.ReadFloat();   // 299.9991
            CenterZ = block.ReadFloat();    // -64
            FactorY = block.ReadFloat();    // 99.48125
            RangeY = block.ReadFloat();     // 0
            RadiusM = block.ReadFloat();    // 64
            ShaderIndex = block.ReadInt();  // 0 , 14, 6 etc  TODO, I think there is something wrong here
            X = block.ReadFloat();   // 0.001953 or 0.998 or 0.001  (1/512, 511/512, 1/1024) typically, but not always
            Y = block.ReadFloat();   // 0.001953 or 0.998 or 0.001
            W = block.ReadFloat();	 // 0.06225586 0 -0.06225586  (255/256)/16
            B = block.ReadFloat();	 // 0.06225586 0 -0.06225586  
            C = block.ReadFloat();   // 0.06225586 0 -0.06225586  
            H = block.ReadFloat();   // 0.06225586 0 -0.06225586  
            ErrorBias = block.ReadFloat();  // 0 - 1
        }
    }

    public class terrain_patchset
    {
        public readonly int terrain_patchset_distance;
        public readonly int terrain_patchset_npatches;
        public readonly terrain_patchset_patch[] terrain_patchset_patches;

        public terrain_patchset(SBR block)
        {
            block.VerifyID(TokenID.terrain_patchset);
            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.terrain_patchset_distance:
                            terrain_patchset_distance = subBlock.ReadInt();
                            break;
                        case TokenID.terrain_patchset_npatches:
                            terrain_patchset_npatches = subBlock.ReadInt();
                            break;
                        case TokenID.terrain_patchset_patches:
                            terrain_patchset_patches = new terrain_patchset_patch[terrain_patchset_npatches * terrain_patchset_npatches];
                            for (var i = 0; i < terrain_patchset_patches.Length; ++i)
                                terrain_patchset_patches[i] = new terrain_patchset_patch(subBlock.ReadSubBlock());
                            break;
                    }
                }
            }
        }

        public terrain_patchset_patch GetPatch(int x, int z)
        {
            return terrain_patchset_patches[z * terrain_patchset_npatches + x];
        }
    }

    public class terrain_shader
    {
        public readonly string ShaderName;
        public readonly terrain_texslot[] terrain_texslots;
        public readonly terrain_uvcalc[] terrain_uvcalcs;

        public terrain_shader(SBR block)
        {
            block.VerifyID(TokenID.terrain_shader);
            ShaderName = block.ReadString();
            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.terrain_texslots:
                            terrain_texslots = new terrain_texslot[subBlock.ReadUInt()];
                            for (var i = 0; i < terrain_texslots.Length; ++i)
                                terrain_texslots[i] = new terrain_texslot(subBlock.ReadSubBlock());
                            break;
                        case TokenID.terrain_uvcalcs:
                            terrain_uvcalcs = new terrain_uvcalc[subBlock.ReadUInt()];
                            for (var i = 0; i < terrain_uvcalcs.Length; ++i)
                                terrain_uvcalcs[i] = new terrain_uvcalc(subBlock.ReadSubBlock());
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }

    public class TerrainFile
    {
        public readonly terrain terrain;

        public TerrainFile(string filename)
        {
            using (var sbr = SBR.Open(filename))
            using (var block = sbr.ReadSubBlock())
                terrain = new terrain(block);
        }
    }

    public static class TileName
    {
        public enum Zoom
        {
            DMLarge = 11, // 32KM^2
            DMSmall = 12, // 16KM^2
            // 13 = 8KM^2
            Large = 14, // 4KM^2
            Small = 15, // 2KM^2
        }

        const string Hex = "0123456789ABCDEF";

        public static string FromTileXZ(int tileX, int tileZ, Zoom zoom)
        {
            var rectX = -16384;
            var rectZ = -16384;
            var rectW = 16384;
            var rectH = 16384;
            var name = new StringBuilder((int)zoom % 2 == 1 ? "-" : "_");
            var partial = 0;
            for (var z = 0; z < (int)zoom; z++)
            {
                var east = tileX >= rectX + rectW;
                var north = tileZ >= rectZ + rectH;
                partial <<= 2;
                partial += (north ? 0 : 2) + (east ^ north ? 0 : 1);
                if (z % 2 == 1)
                {
                    name.Append(Hex[partial]);
                    partial = 0;
                }
                if (east) rectX += rectW;
                if (north) rectZ += rectH;
                rectW /= 2;
                rectH /= 2;
            }
            if ((int)zoom % 2 == 1)
                name.Append(Hex[partial << 2]);
            return name.ToString();
        }

        public static void Snap(ref int tileX, ref int tileZ, Zoom zoom)
        {
            var step = 15 - (int)zoom;
            tileX >>= step;
            tileX <<= step;
            tileZ >>= step;
            tileZ <<= step;
        }
    }
}
