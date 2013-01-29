/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

using System.Collections.Generic;
using System.Diagnostics;

/*
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("", ()=>{  }),
            });
*/

namespace MSTS
{
    public class ENVFile
    {
		public float WaterWaveHeight;
		public float WaterWaveSpeed;
		public List<ENVFileWaterLayer> WaterLayers;

        public ENVFile(string filePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("world", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("world_water", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("world_water_wave_height", ()=>{ WaterWaveHeight = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                            new STFReader.TokenProcessor("world_water_wave_speed", ()=>{ WaterWaveSpeed = stf.ReadFloatBlock(STFReader.UNITS.Speed, null); }),
                            new STFReader.TokenProcessor("world_water_layers", ()=>{ ParseWaterLayers(stf); }),
                        });}),
                    });}),
                });
        }
        private void ParseWaterLayers(STFReader stf)
        {
            stf.MustMatch("(");
            int texturelayers = stf.ReadInt(STFReader.UNITS.None, null);
            WaterLayers = new List<ENVFileWaterLayer>(texturelayers);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("world_water_layer", ()=>{ if(texturelayers-- > 0) WaterLayers.Add(new ENVFileWaterLayer(stf)); })
            });
        }
    }

	public class ENVFileWaterLayer
	{
		public float Height;
		public string TextureName;

		public ENVFileWaterLayer(STFReader stf)
		{
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("world_water_layer_height", ()=>{ Height = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("world_anim_shader", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("world_shader", ()=>{ stf.MustMatch("("); stf.ReadString()/*TextureMode*/; stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("terrain_texslots", ()=>{ stf.MustMatch("("); stf.ReadInt(STFReader.UNITS.None, null)/*Count*/; stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("terrain_texslot", ()=>{ stf.MustMatch("("); TextureName = stf.ReadString(); stf.SkipRestOfBlock(); }),
                        });}),
                    });}),
                });}),
            });
        }
	}
}