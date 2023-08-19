// COPYRIGHT 2009, 2010, 2012 by the Open Rails project.
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

using System;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    public class ShapeDescriptorFile
    {
        public SDShape shape;

        public ShapeDescriptorFile()  // use for files with no SD file
        {
            shape = new SDShape();
        }

        public ShapeDescriptorFile(string filename)
        {
            using (STFReader stf = new STFReader(filename, false))
            {
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("shape", ()=>{ shape = new SDShape(stf); }),
                });
                //TODO This should be changed to STFException.TraceError() with defaults values created
                if (shape == null)
                    throw new STFException(stf, "Missing shape statement");
            }
        }

        public class SDShape
        {
            public SDShape()
            {
                ESD_Bounding_Box = new ESD_Bounding_Box();
            }

            public SDShape(STFReader stf)
            {
                stf.ReadString(); // Ignore the filename string. TODO: Check if it agrees with the SD file name? Is this important?
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("esd_detail_level", ()=>{ ESD_Detail_Level = stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("esd_alternative_texture", ()=>{ ESD_Alternative_Texture = stf.ReadIntBlock(null); }),
                    new STFReader.TokenProcessor("esd_no_visual_obstruction", ()=>{ ESD_No_Visual_Obstruction = stf.ReadBoolBlock(true); }),
                    new STFReader.TokenProcessor("esd_snapable", ()=>{ ESD_Snapable = stf.ReadBoolBlock(true); }),
                    new STFReader.TokenProcessor("esd_subobj", ()=>{ ESD_SubObj = true; stf.SkipBlock(); }),
                    new STFReader.TokenProcessor("esd_bounding_box", ()=>{
                        ESD_Bounding_Box = new ESD_Bounding_Box(stf);
                        if (ESD_Bounding_Box.Min == null || ESD_Bounding_Box.Max == null)  // ie quietly handle ESD_Bounding_Box()
                            ESD_Bounding_Box = null;
                    }),
                    new STFReader.TokenProcessor("esd_ortssoundfilename", ()=>{ ESD_SoundFileName = stf.ReadStringBlock(null); }),
                    new STFReader.TokenProcessor("esd_ortsbellanimationfps", ()=>{ ESD_CustomAnimationFPS = stf.ReadFloatBlock(STFReader.UNITS.Frequency, null); }),
                    new STFReader.TokenProcessor("esd_ortscustomanimationfps", ()=>{ ESD_CustomAnimationFPS = stf.ReadFloatBlock(STFReader.UNITS.Frequency, null); }),
                });
                // TODO - some objects have no bounding box - ie JP2BillboardTree1.sd
                //if (ESD_Bounding_Box == null) throw new STFException(stf, "Missing ESD_Bound_Box statement");
            }
            public int ESD_Detail_Level;
            public int ESD_Alternative_Texture;
            public ESD_Bounding_Box ESD_Bounding_Box;
            public bool ESD_No_Visual_Obstruction;
            public bool ESD_Snapable;
            public bool ESD_SubObj;
            public string ESD_SoundFileName = "";
            public float ESD_CustomAnimationFPS = 8;
        }

        public class ESD_Bounding_Box
        {
            public ESD_Bounding_Box() // default used for files with no SD file
            {
                Min = new TWorldPosition(0, 0, 0);
                Max = new TWorldPosition(0, 0, 0);
            }

            public ESD_Bounding_Box(STFReader stf)
            {
                stf.MustMatch("(");
                string item = stf.ReadString();
                if (item == ")") return;    // quietly return on ESD_Bounding_Box()
                stf.StepBackOneItem();
                float X = stf.ReadFloat(STFReader.UNITS.None, null);
                float Y = stf.ReadFloat(STFReader.UNITS.None, null);
                float Z = stf.ReadFloat(STFReader.UNITS.None, null);
                Min = new TWorldPosition(X, Y, Z);
                X = stf.ReadFloat(STFReader.UNITS.None, null);
                Y = stf.ReadFloat(STFReader.UNITS.None, null);
                Z = stf.ReadFloat(STFReader.UNITS.None, null);
                Max = new TWorldPosition(X, Y, Z);
                // JP2indirt.sd has extra parameters
                stf.SkipRestOfBlock();
            }
            public TWorldPosition Min;
            public TWorldPosition Max;
        }
    }
}
