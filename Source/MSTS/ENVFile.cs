// COPYRIGHT 2009, 2010 by the Open Rails project.
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
