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
		public readonly float WaterWaveHeight;
		public readonly float WaterWaveSpeed;
		public readonly IEnumerable<ENVFileWaterLayer> WaterLayers;

        public ENVFile(string filePath)
        {
            STFReader reader = new STFReader(filePath);
			var waterLayers = new List<ENVFileWaterLayer>();
			var waterLayerCount = 0;
            while (!reader.EOF())
            {
                reader.ReadToken();
				if (reader.Tree == "world(world_water(world_water_wave_height(")
				{
					WaterWaveHeight = reader.ReadInt();
				}
				else if (reader.Tree == "world(world_water(world_water_wave_speed(")
				{
					WaterWaveSpeed = reader.ReadInt();
				}
				else if (reader.Tree == "world(world_water(world_water_layers(")
				{
					waterLayers.Capacity = waterLayerCount = reader.ReadInt();
				}
				else if (reader.Tree == "world(world_water(world_water_layers(world_water_layer(")
				{
					if (waterLayers.Count < waterLayerCount)
						waterLayers.Add(new ENVFileWaterLayer(reader));
					else
						Trace.TraceWarning("Ignoring extra world_water_layer in {0}:line {1}", reader.FileName, reader.LineNumber);
				}
            }
            reader.Close();
			WaterLayers = waterLayers;
        }
    }

	public class ENVFileWaterLayer
	{
		public readonly float Height;
		public readonly string TextureName;

		internal ENVFileWaterLayer(STFReader reader)
		{
			while (!reader.EOF() && reader.Tree.StartsWith("world(world_water(world_water_layers(world_water_layer("))
			{
				reader.ReadToken();
				if (reader.Tree == "world(world_water(world_water_layers(world_water_layer(world_water_layer_height(")
				{
					Height = reader.ReadFloat();
				}
				else if (reader.Tree == "world(world_water(world_water_layers(world_water_layer(world_anim_shader(world_shader(terrain_texslots(terrain_texslot(")
				{
					// ie terrain_texslot ( waterbot.ace 1 0 )
					TextureName = reader.ReadToken();
					reader.ReadToken();
					reader.ReadToken();
					reader.MustMatch(")");
				}
			}
		}
	}
}