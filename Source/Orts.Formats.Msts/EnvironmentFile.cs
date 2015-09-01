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
using Orts.Parsers.Msts;

/*
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("", ()=>{  }),
            });
*/

namespace Orts.Formats.Msts
{
    public class EnvironmentFile
    {
        public float WaterWaveHeight;
        public float WaterWaveSpeed;
        public float WorldSkynLayers;
        public List<ENVFileWaterLayer> WaterLayers;
        public List<ENVFileSkyLayer> SkyLayers;
        public List<ENVFileSkySatellite> SkySatellite;

        public EnvironmentFile(string filePath)
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

            using (STFReader stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("world", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("world_sky", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                               new STFReader.TokenProcessor("worldskynlayers_behind_satellites", ()=>{ WorldSkynLayers = stf.ReadFloatBlock( STFReader.UNITS.Any, null ); }),
                               new STFReader.TokenProcessor("world_sky_layers", ()=>{ ParseSkyLayers(stf); }),
                               new STFReader.TokenProcessor("world_sky_satellites", ()=>{ ParseWorldSkySatellites(stf); }),
                               });}),
                        });}),
                    });
        }
        private void ParseWaterLayers(STFReader stf)
        {
            stf.MustMatch("(");
            int texturelayers = stf.ReadInt(null);
            WaterLayers = new List<ENVFileWaterLayer>(texturelayers);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("world_water_layer", ()=>{ if(texturelayers-- > 0) WaterLayers.Add(new ENVFileWaterLayer(stf)); })
            });
        }
        private void ParseSkyLayers(STFReader stf)
        {
            stf.MustMatch("(");
            int skylayers = stf.ReadInt(null);
            SkyLayers = new List<ENVFileSkyLayer>(skylayers);

            stf.ParseBlock(new STFReader.TokenProcessor[] 
            {
                new STFReader.TokenProcessor("world_sky_layer", ()=>{ if(skylayers-- > 0) SkyLayers.Add(new ENVFileSkyLayer(stf)); })});

        }
        private void ParseWorldSkySatellites(STFReader stf)
        {
            stf.MustMatch("(");
            int skysatellite = stf.ReadInt(null);
            SkySatellite = new List<ENVFileSkySatellite>(skysatellite);

            stf.ParseBlock(new STFReader.TokenProcessor[] 
        {                
            new STFReader.TokenProcessor("world_sky_satellite", () => { if (skysatellite-- > 0) SkySatellite.Add(new ENVFileSkySatellite(stf)); })});
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
                        new STFReader.TokenProcessor("terrain_texslots", ()=>{ stf.MustMatch("("); stf.ReadInt(null)/*Count*/; stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("terrain_texslot", ()=>{ stf.MustMatch("("); TextureName = stf.ReadString(); stf.SkipRestOfBlock(); }),
                            });}),
                        });}),
                    });}),
                });
            }
        }

    }

    public class ENVFileSkyLayer
    {
        public string Fadein_Begin_Time;
        public string Fadein_End_Time; 
        public string TextureName;
        public string TextureMode;
        public float TileX;
        public float TileY;


        public ENVFileSkyLayer(STFReader stf)
        {
            stf.MustMatch("(");
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("world_sky_layer_fadein", ()=>{ stf.MustMatch("("); Fadein_Begin_Time = stf.ReadString(); Fadein_End_Time = stf.ReadString(); stf.SkipRestOfBlock();}),
                        new STFReader.TokenProcessor("world_anim_shader", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("world_anim_shader_frames", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                                new STFReader.TokenProcessor("world_anim_shader_frame", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                                    new STFReader.TokenProcessor("world_anim_shader_frame_uvtiles", ()=>{ stf.MustMatch("("); TileX = stf.ReadFloat(STFReader.UNITS.Any, 1.0f); TileY = stf.ReadFloat(STFReader.UNITS.Any, 1.0f); stf.ParseBlock(new STFReader.TokenProcessor[] {
                                    });}),  
                                });}),
                            });}),
                            new STFReader.TokenProcessor("world_shader", ()=>{ stf.MustMatch("("); TextureMode = stf.ReadString(); stf.ParseBlock(new STFReader.TokenProcessor[] {
                                new STFReader.TokenProcessor("terrain_texslots", ()=>{ stf.MustMatch("("); stf.ReadInt(null)/*Count*/; stf.ParseBlock(new STFReader.TokenProcessor[] {
                                    new STFReader.TokenProcessor("terrain_texslot", ()=>{ stf.MustMatch("("); TextureName = stf.ReadString(); stf.SkipRestOfBlock(); }),
                                    });}),
                                });}),
                            });}),
            });

        }
    }
    public class ENVFileSkySatellite
    {

        public string TextureName;
        public string TextureMode;

        public ENVFileSkySatellite(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("world_anim_shader", ()=>{ stf.MustMatch("("); stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("world_shader", ()=>{ stf.MustMatch("("); TextureMode = stf.ReadString(); stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("terrain_texslots", ()=>{ stf.MustMatch("("); stf.ReadInt(null)/*Count*/; stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("terrain_texslot", ()=>{ stf.MustMatch("("); TextureName = stf.ReadString(); stf.SkipRestOfBlock(); }),
                            });}),
                        });}),
                    });}),
                });
        }
    }
}
