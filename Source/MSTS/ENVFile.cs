/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MSTS
{
    public class ENVFile
    {
        public List<string> WaterTextureNames = new List<string>();  // the last one listed in the file
        int WaterLayerCount = 0;

        
        public ENVFile(string filePath)
        {
            STFReader f = new STFReader(filePath);

            // TODO complete this parse
            while (!f.EOF())
            {
                f.ReadToken();
                if (0 == string.Compare(f.Tree, "world(world_water(world_water_layers("))
                {
                    WaterLayerCount = f.ReadInt();
                }
                else if (0 == string.Compare(f.Tree, "world(world_water(world_water_layers(world_water_layer(world_anim_shader(world_shader(terrain_texslots(terrain_texslot("))
                {
                    // ie terrain_texslot ( waterbot.ace 1 0 )
                    string waterTextureName = f.ReadToken();
                    f.ReadToken();
                    f.ReadToken();
                    f.MustMatch(")");
                    if (WaterTextureNames.Count < WaterLayerCount)
                        WaterTextureNames.Add(waterTextureName);
                }
            }

            f.Close();
        }
    }
}