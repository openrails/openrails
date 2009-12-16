/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Contributors
///     2009-12-06  Rob Lane

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Text;
using System.Collections.Generic;


namespace MSTS
{


	public class terrain_water_height_offset
	{
		public float SW = 0;
		public float SE = 0;
		public float NE = 0;
		public float NW = 0;

        public terrain_water_height_offset( SBR block )
        {
            block.VerifyID(TokenID.terrain_water_height_offset);
            if (!block.EndOfBlock()) SW = block.ReadFloat();
            if (!block.EndOfBlock()) SE = block.ReadFloat();
            if (!block.EndOfBlock()) NE = block.ReadFloat();
            if (!block.EndOfBlock()) NW = block.ReadFloat();
        }
	} //class terrain_water_height_offset

	public class terrain
	{
		public float terrain_errthreshold_scale = 1.0f;
		public terrain_water_height_offset terrain_water_height_offset = null;
		public float terrain_alwaysselect_maxdist = 0.0f;
		public terrain_samples terrain_samples = null;
		public ArrayList terrain_shaders = null;
		public terrain_patchset[] terrain_patchsets = null;

        public terrain(SBR block)
        {
            block.VerifyID(TokenID.terrain);
            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
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
                            {
                                uint count = subBlock.ReadUInt();
                                terrain_shaders = new ArrayList((int)count);
                                for (int i = 0; i < count; ++i) 
                                    using( SBR terrain_shadersBlock = subBlock.ReadSubBlock() )
                                        terrain_shaders.Add(new terrain_shader(terrain_shadersBlock));
                                break;
                            }
                        case TokenID.terrain_patches:
                            {
                                using( SBR patch_sets_Block = subBlock.ReadSubBlock() )
                                {
                                    uint patch_sets_count = patch_sets_Block.ReadUInt();
                                    terrain_patchsets = new terrain_patchset[patch_sets_count];
                                    for (int i = 0; i < patch_sets_count; ++i) 
                                        using( SBR terrain_patchsetBlock = patch_sets_Block.ReadSubBlock() )
                                            terrain_patchsets[i] = new terrain_patchset(terrain_patchsetBlock);
                                }
                                break;
                            }
                    }
                }
            }
        } 

    } //class terrain

// TODO fails on = "c:\\program files\\microsoft games\\train simulator\\ROUTES\\EUROPE1\\Tiles\\-11cc0604.t") Line 330 + 0x18 bytes	C#


	public class terrain_samples
	{
		public int terrain_nsamples;
		public float terrain_sample_rotation;
		public float terrain_sample_floor;
		public float terrain_sample_scale;
		public float terrain_sample_size;
		public string terrain_sample_ybuffer = null;
		public string terrain_sample_ebuffer = null;
		public string terrain_sample_nbuffer = null;
		public byte[] terrain_sample_asbuffer = null; // don't know what these do
		public byte[] terrain_sample_fbuffer = null;  // don't know what these do

        public terrain_samples(SBR block)
        {
            block.VerifyID(TokenID.terrain_samples);
            while (!block.EndOfBlock())
            {
                using (SBR subBlock = block.ReadSubBlock())
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
                        case TokenID.terrain_sample_asbuffer:
                            subBlock.Skip(); // TODO parse this
                            break;
                        case TokenID.terrain_sample_fbuffer:
                            subBlock.Skip(); // TODO parse this
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
                        case (TokenID)282:  // TODO, figure out what this is and handle it
                            subBlock.Skip();
                            break;
                        default:
                            throw new System.Exception("Unknown token " + subBlock.ID.ToString());
                    }
                }
            }

        }

	} //class terrain_samples

	public class terrain_texslot
	{
		public string Filename;
		public int A;
		public int B;

        public terrain_texslot( SBR block)
        {
            block.VerifyID(TokenID.terrain_texslot);
            Filename = block.ReadString();
            A = block.ReadInt();
            B = block.ReadInt();
            block.Skip();
        }

  	} // class terrain_texslot
	
	public class terrain_uvcalc
	{
		public int A;
		public int B;
		public int C;
		public int D;

        public terrain_uvcalc( SBR block)
        {
            block.VerifyID(TokenID.terrain_uvcalc);
            A = block.ReadInt();
            B = block.ReadInt();
            C = block.ReadInt();
            D = block.ReadInt();
        }
	} //public class terrain_uvcalc

	public class terrain_patchset_patch
	{
		public uint Flags;               // 1 = don't draw
										 // 10000C0  = draw water
		public float CenterX, CenterZ;
		public float AverageY, RangeY, FactorY; // don't really know the purpose of tese
		public int iShader;
		public float ErrorBias;
		public float X,Y,W,H,B,C;        // texture coordinates
		public float K;

        public bool WaterEnabled { get { return (Flags & 0xc0) != 0; } }
        public bool DrawingEnabled { get { return (Flags & 1) == 0; } }

		public terrain_patchset_patch( SBR block)
		{
            iShader = 0;

            block.VerifyID(TokenID.terrain_patchset_patch);
			Flags = block.ReadUInt();   
			CenterX = block.ReadFloat();   // 64
			AverageY = block.ReadFloat();   // 299.9991
			CenterZ = block.ReadFloat();   // -64
			FactorY = block.ReadFloat();   // 99.48125
			RangeY = block.ReadFloat();   // 0
			K = block.ReadFloat();   // 64
			iShader = block.ReadInt();   // 0 , 14, 6 etc  TODO, I think there is something wrong here
			X = block.ReadFloat();   // 0.001953 or 0.998 or 0.001
			Y = block.ReadFloat();   // 0.001953 or 0.998 or 0.001
			W = block.ReadFloat();	 // 0.06225586 0 -0.06225586  
			B = block.ReadFloat();	 // 0.06225586 0 -0.06225586  
			C = block.ReadFloat();   // 0.06225586 0 -0.06225586  
			H = block.ReadFloat();   // 0.06225586 0 -0.06225586  
			ErrorBias = block.ReadFloat();   // 0 - 1
		}

	} //public class terrain_patchset_patch

	public class terrain_patchset
	{
		public int terrain_patchset_distance;
		public int terrain_patchset_npatches;
		public terrain_patchset_patch[] terrain_patchset_patches;

        public terrain_patchset_patch GetPatch(int x, int z)
        {
            terrain_patchset_patch patch = terrain_patchset_patches[z * 16 + x];
            return patch;
        }

		public terrain_patchset( SBR block )
		{
            block.VerifyID(TokenID.terrain_patchset);
			while( !block.EndOfBlock() )
			{
                using (SBR subBlock = block.ReadSubBlock())
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
                            for (int i = 0; i < terrain_patchset_patches.Length; ++i)
                                terrain_patchset_patches[i] = new terrain_patchset_patch(subBlock.ReadSubBlock());
                            break;
                    }
                }
			}

		}

	} //class terrain_patchset

	public class terrain_shader
	{
		public string Label;  // TODO, replace this with an enum
		public terrain_texslot[] terrain_texslots;
		public terrain_uvcalc[] terrain_uvcalcs;

		public terrain_shader( SBR block )
		{
            block.VerifyID(TokenID.terrain_shader);
            
            Label = block.ReadString();  // todo, this is wastefull, its either DetailTerrain or AlphaTerrain
            
			while( !block.EndOfBlock() )
			{
				using( SBR subBlock = block.ReadSubBlock() )
                {
				    switch( subBlock.ID )
				    {
					    case TokenID.terrain_texslots: 
					    {
						    uint count = subBlock.ReadUInt();
						    terrain_texslots = new terrain_texslot[ count ];
						    for( int i = 0; i < count; ++i )
							    terrain_texslots[i] = new terrain_texslot( subBlock.ReadSubBlock() );
						    break;
					    }
					    case TokenID.terrain_uvcalcs:
					    {
						    uint count = subBlock.ReadUInt();
						    terrain_uvcalcs = new terrain_uvcalc[ count ];
						    for( int i = 0; i < count; ++i )
							    terrain_uvcalcs[i] = new terrain_uvcalc( subBlock.ReadSubBlock() );
						    break;
					    }
					    default:
						    break;
				    }
                }
			}
		}
	} // terrain_shader

	public class TFile
	{
		public float Floor{ get{ return terrain.terrain_samples.terrain_sample_floor;}}		  // in meters
		public float Resolution{ get{ return terrain.terrain_samples.terrain_sample_scale; }}  // in meters per( number in Y-file )
		public float WaterNE{ get{ return terrain.terrain_water_height_offset.NE; } } // in meters
		public float WaterNW{ get{ return terrain.terrain_water_height_offset.NW; } }
		public float WaterSE{ get{ return terrain.terrain_water_height_offset.SE; } }
		public float WaterSW{ get{ return terrain.terrain_water_height_offset.SW; } }
		string FileName;
		public bool OK() { return terrain.terrain_samples != null; }
		public float MaxElevation {	get	{	return Floor + (ushort.MaxValue-1) * Resolution;}	}

        public bool ContainsWater { 
            get 
            {
                foreach (terrain_patchset patchset in terrain.terrain_patchsets)
                    foreach (terrain_patchset_patch patch in patchset.terrain_patchset_patches)
                        if (patch.WaterEnabled)
                            return true;
                return false;
            }
        }

        public terrain terrain;

        public TFile(string filename)
		{
            FileName = filename;

            using( SBR f = SBR.Open( filename ))
            {
                using (SBR block = f.ReadSubBlock() )
                {
                    terrain = new terrain(block);
                }
            }
		}

	} // class TFile

    public class TileNameConversion
    {

        /////////////////////////////////////////////
        /// The following code was derived from MSTSConverter
        /// Written by: West L. Card
        /// which was derived from code by  John Stanford 
        /// //////////////////////////////////////////////

        // TODO this method is not reentrant - rewrite.

        // world tile max and min
        static int wt_ew_min = -16384;
        static int wt_ew_max = 16384;
        static int wt_ns_min = -16384;
        static int wt_ns_max = 16384;


        static int[,] Direction = new int[2, 2];

        // for convenience
        const int NE = 1;
        const int SE = 2;
        const int SW = 3;
        const int NW = 0;

        const string Hex = "0123456789ABCDEF";


        // lets cache these names to speed up access to them
        private static Dictionary<long, string> TileNameCache = new Dictionary<long, string>();

        public static string GetTileNameFromTileXZ(int wt_ew, int wt_ns)
        {
            long key = wt_ew * 100000L + wt_ns;
            string filename;

            if (TileNameCache.TryGetValue(key, out filename))
                return filename;  // we found it in the cache

            // otherwise compute it, add it to the cache and return it
            filename = ComputeTileNameFromTileXZ(wt_ew, wt_ns);
            TileNameCache.Add(key, filename);
            return filename;  
        }

        /// <summary>
        /// ie Returns "-04e9a288"
        /// </summary>
        /// <param name="wt_ew"></param>
        /// <param name="wt_ns"></param>
        /// <returns></returns>
        public static string ComputeTileNameFromTileXZ(int wt_ew, int wt_ns)
        {

            char prefix = '-';
            byte append_char=0;
            byte add_value;

            StringBuilder TileFileName  = new StringBuilder("", 8);

            wt_ew_min = -16384;
            wt_ew_max = 16384;
            wt_ns_min = -16384;
            wt_ns_max = 16384;

            Direction[0, 0] = SW;
            Direction[0, 1] = NW;
            Direction[1, 1] = NE;
            Direction[1, 0] = SE;

            while (  TileFileName.Length < 8 )
            {
                int quad = UpdatePosition(wt_ew, wt_ns);

                if (prefix == '-')
                {
                    prefix = '_';
                    if (quad == NE)
                        append_char = 4;
                    else if (quad == SE)
                        append_char = 8;
                    else if (quad == SW)
                        append_char = 12; //C
                    else // quad = NW
                        append_char = 0;
                }
                else
                {
                    prefix = '-';
                    if (TileFileName.Length == 7)
                        add_value = 0;
                    else if (quad == NE)
                        add_value = 1;
                    else if (quad == SE)
                        add_value = 2;
                    else if (quad == SW)
                        add_value = 3;
                    else // quad = NW
                        add_value = 0;
                    TileFileName.Append( Hex[append_char + add_value] );
                }
            }

            return prefix + TileFileName.ToString().ToLower();
        }

        private static int UpdatePosition(int wt_ew_tgt, int wt_ns_tgt)
        {
            int idx1, idx2;

            int wt_ew_avg = (wt_ew_max + wt_ew_min) / 2;
            if (wt_ew_tgt >= wt_ew_avg)  // very left side
            {
                idx1 = 1;
                wt_ew_min = wt_ew_avg;
            }
            else
            {
                idx1 = 0;
                wt_ew_max = wt_ew_avg;
            }

            int wt_ns_avg = (wt_ns_max + wt_ns_min ) / 2;
            if (wt_ns_tgt >= wt_ns_avg)
            {
                idx2 = 1;
                wt_ns_min = wt_ns_avg;
            }
            else
            {
                idx2 = 0;
                wt_ns_max = wt_ns_avg;
            }

            return Direction[idx1, idx2];
        }
    } // class TileNameConversion

}// namespace 

