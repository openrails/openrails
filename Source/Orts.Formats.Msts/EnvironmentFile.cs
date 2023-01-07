// COPYRIGHT 2009 - 2023 by the Open Rails project.
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
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    public class EnvironmentFile
    {
        public List<WaterLayer> WaterLayers;
        public List<SkyLayer> SkyLayers;
        public List<SkySatellite> SkySatellites;

        public EnvironmentFile(string filePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                stf.ParseFile(new[]
                {
                    new STFReader.TokenProcessor("world", () => stf.ParseWholeBlock(new[]
                    {
                        new STFReader.TokenProcessor("world_water", () => stf.ParseWholeBlock(new[]
                        {
                            new STFReader.TokenProcessor("world_water_layers", () => stf.ParseBlockList(ref WaterLayers, "world_water_layer", s => new WaterLayer(s))),
                        })),
                        new STFReader.TokenProcessor("world_sky", () => stf.ParseWholeBlock(new[]
                        {
                            new STFReader.TokenProcessor("world_sky_layers", () => stf.ParseBlockList(ref SkyLayers, "world_sky_layer", s => new SkyLayer(s))),
                            new STFReader.TokenProcessor("world_sky_satellites", () => stf.ParseBlockList(ref SkySatellites, "world_sky_satellite", s => new SkySatellite(s))),
                        })),
                    })),
                });
            }
        }

        public class WaterLayer
        {
            public float Height;
            public string TextureName;

            public WaterLayer(STFReader stf)
            {
                stf.ParseWholeBlock(new[]
                {
                    new STFReader.TokenProcessor("world_water_layer_height", () => { Height = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                    new STFReader.TokenProcessor("world_anim_shader", () => stf.ParseWholeBlock(new[]
                    {
                        new STFReader.TokenProcessor("world_shader", () =>
                        {
                            stf.MustMatch("(");
                            stf.ReadString() /*TextureMode*/;
                            stf.ParseBlock(new[]
                            {
                                new STFReader.TokenProcessor("terrain_texslots", () =>
                                {
                                    stf.MustMatch("(");
                                    stf.ReadInt(null) /*Count*/;
                                    stf.ParseBlock(new[]
                                    {
                                        new STFReader.TokenProcessor("terrain_texslot", () =>
                                        {
                                            stf.MustMatch("(");
                                            TextureName = stf.ReadString();
                                            stf.SkipRestOfBlock();
                                        }),
                                    });
                                }),
                            });
                        }),
                    })),
                });
            }
        }

        public class SkyLayer
        {
            public string FadeInBeginTime;
            public string FadeInEndTime;
            public string TextureName;
            public string TextureMode;
            public float TileX;
            public float TileY;

            public SkyLayer(STFReader stf)
            {
                stf.ParseWholeBlock(new[]
                {
                    new STFReader.TokenProcessor("world_sky_layer_fadein", () =>
                    {
                        stf.MustMatch("(");
                        FadeInBeginTime = stf.ReadString();
                        FadeInEndTime = stf.ReadString();
                        stf.SkipRestOfBlock();
                    }),
                    new STFReader.TokenProcessor("world_anim_shader", () => stf.ParseWholeBlock(new[]
                    {
                        new STFReader.TokenProcessor("world_anim_shader_frames", () => stf.ParseWholeBlock(new[]
                        {
                            new STFReader.TokenProcessor("world_anim_shader_frame", () => stf.ParseWholeBlock(new[]
                            {
                                new STFReader.TokenProcessor("world_anim_shader_frame_uvtiles", () =>
                                {
                                    stf.MustMatch("(");
                                    TileX = stf.ReadFloat(STFReader.UNITS.Any, 1.0f);
                                    TileY = stf.ReadFloat(STFReader.UNITS.Any, 1.0f);
                                    stf.ParseBlock(new STFReader.TokenProcessor[0]);
                                }),
                            })),
                        })),
                    })),
                    new STFReader.TokenProcessor("world_shader", () =>
                    {
                        stf.MustMatch("(");
                        TextureMode = stf.ReadString();
                        stf.ParseBlock(new[]
                        {
                            new STFReader.TokenProcessor("terrain_texslots", () =>
                            {
                                stf.MustMatch("(");
                                stf.ReadInt(null) /*Count*/;
                                stf.ParseBlock(new[]
                                {
                                    new STFReader.TokenProcessor("terrain_texslot", () =>
                                    {
                                        stf.MustMatch("(");
                                        TextureName = stf.ReadString();
                                        stf.SkipRestOfBlock();
                                    }),
                                });
                            }),
                        });
                    }),
                });
            }
        }

        public class SkySatellite
        {
            public string TextureName;
            public string TextureMode;

            public SkySatellite(STFReader stf)
            {
                stf.ParseWholeBlock(new[]
                {
                    new STFReader.TokenProcessor("world_anim_shader", () => stf.ParseWholeBlock(new[]
                    {
                        new STFReader.TokenProcessor("world_shader", () =>
                        {
                            stf.MustMatch("(");
                            TextureMode = stf.ReadString();
                            stf.ParseBlock(new[]
                            {
                                new STFReader.TokenProcessor("terrain_texslots", () =>
                                {
                                    stf.MustMatch("(");
                                    stf.ReadInt(null) /*Count*/;
                                    stf.ParseBlock(new[]
                                    {
                                        new STFReader.TokenProcessor("terrain_texslot", () =>
                                        {
                                            stf.MustMatch("(");
                                            TextureName = stf.ReadString();
                                            stf.SkipRestOfBlock();
                                        }),
                                    });
                                }),
                            });
                        }),
                    })),
                });
            }
        }
    }
}
