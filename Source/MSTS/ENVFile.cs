/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System.Collections.Generic;
using System.Diagnostics;

namespace MSTS
{
    public class ENVFile
    {
		public float WaterWaveHeight;
		public float WaterWaveSpeed;
		public List<ENVFileWaterLayer> WaterLayers;

        public ENVFile(string filePath)
        {
            using (STFReader reader = new STFReader(filePath))
            {
                while (!reader.EOF)
                {
                    switch (reader.ReadItem())
                    {
                        case "world": ParseWorld(reader); break;
                        case "(": reader.SkipRestOfBlock(); break;
                    }
                }
            }
        }
        private void ParseWorld(STFReader reader)
        {
            reader.MustMatch("(");
            while (!reader.EndOfBlock())
                switch (reader.ReadItem().ToLower())
                {
                    case "world_water": ParseWater(reader); break;
                    case "(": reader.SkipRestOfBlock(); break;
                }
        }
        private void ParseWater(STFReader reader)
        {
            reader.MustMatch("(");
            while (!reader.EndOfBlock())
                switch (reader.ReadItem().ToLower())
                {
                    case "world_water_wave_height": WaterWaveHeight = reader.ReadFloatBlock(); break;
                    case "world_water_wave_speed": WaterWaveSpeed = reader.ReadFloatBlock(); break;
                    case "world_water_layers": ParseWaterLayers(reader); break;
                    case "(": reader.SkipRestOfBlock(); break;
                }
        }
        private void ParseWaterLayers(STFReader reader)
        {
            reader.MustMatch("(");
            int texturelayers = reader.ReadInt(STFReader.UNITS.Any, null);
            WaterLayers = new List<ENVFileWaterLayer>(texturelayers);
            while (!reader.EndOfBlock())
                switch (reader.ReadItem().ToLower())
                {
                    case "world_water_layer":
                        if(texturelayers-- > 0)
                            WaterLayers.Add(new ENVFileWaterLayer(reader));
                        break;
                    case "(":
                        reader.SkipRestOfBlock();
                        break;
                }
        }
    }

	public class ENVFileWaterLayer
	{
		public float Height;
		public string TextureName;

		public ENVFileWaterLayer(STFReader reader)
		{
            reader.MustMatch("(");
            while (!reader.EndOfBlock())
                switch (reader.ReadItem().ToLower())
                {
                    case "world_water_layer_height": Height = reader.ReadFloatBlock(); break;
                    case "world_anim_shader": ParseAnimShader(reader); break;
                    case "(": reader.SkipRestOfBlock(); break;
                }
        }
        private void ParseAnimShader(STFReader reader)
        {
            reader.MustMatch("(");
            while (!reader.EndOfBlock())
                switch (reader.ReadItem().ToLower())
                {
                    case "world_shader": ParseWorldShader(reader); break;
                    case "(": reader.SkipRestOfBlock(); break;
                }
        }
        private void ParseWorldShader(STFReader reader)
        {
            reader.MustMatch("(");
            reader.ReadItem(); // TextureMode
            while (!reader.EndOfBlock())
                switch (reader.ReadItem().ToLower())
                {
                    case "terrain_texslots": ParseTerrainTexSlots(reader); break;
                    case "(": reader.SkipRestOfBlock(); break;
                }
        }
        private void ParseTerrainTexSlots(STFReader reader)
        {
            reader.MustMatch("(");
            reader.ReadInt(STFReader.UNITS.Any, null); // Count
            while (!reader.EndOfBlock())
                switch (reader.ReadItem().ToLower())
                {
                    case "terrain_texslot":
                        reader.MustMatch("(");
					    TextureName = reader.ReadItem();
					    reader.ReadItem();
					    reader.ReadItem();
					    reader.MustMatch(")");
                        break;
                    case "(": reader.SkipRestOfBlock(); break;
                }
        }
	}
}