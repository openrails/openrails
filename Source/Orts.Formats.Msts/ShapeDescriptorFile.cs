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
using System.Collections.Generic;
using Microsoft.Xna.Framework;
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

        public static Dictionary<string, SDShape> Cache = new Dictionary<string, SDShape>();

        public ShapeDescriptorFile(string filename)
        {
            var shapeDescriptorPath = filename.ToLowerInvariant();

            if (Cache.ContainsKey(shapeDescriptorPath))
            {
                shape = Cache[shapeDescriptorPath];
            }
            else
            {
                if (!System.IO.File.Exists(filename)) // If not found, skip
                {
                    shape = null;
                    return;
                }

                using (STFReader stf = new STFReader(filename, false))
                {
                    stf.ParseFile(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("shape", ()=>{ shape = new SDShape(stf); }),
                    });
                    //TODO This should be changed to STFException.TraceError() with defaults values created
                    if (shape == null) throw new STFException(stf, "Missing shape statement");
                }
                Cache.Add(shapeDescriptorPath, shape);
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
                stf.ReadString();
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
                    new STFReader.TokenProcessor("esd_ortstexturereplacement", ()=>{ ParseReplacementStrings(stf, ref ESD_TextureReplacement); }),
                    new STFReader.TokenProcessor("esd_ortsmatrixrename", ()=>{ ParseReplacementStrings(stf, ref ESD_MatrixRename); }),
                    new STFReader.TokenProcessor("esd_ortsmatrixtranslation", ()=>{ ParseMatrixOverride(STFReader.UNITS.Distance, stf, ref ESD_MatrixTranslation); }),
                    new STFReader.TokenProcessor("esd_ortsmatrixscale", ()=>{ ParseMatrixOverride(STFReader.UNITS.None, stf, ref ESD_MatrixScale); }),
                    new STFReader.TokenProcessor("esd_ortsmatrixrotation", ()=>{ ParseMatrixOverride(STFReader.UNITS.Angle, stf, ref ESD_MatrixRotation); }),
                });

                // Store set of all matrices that got modified
                foreach (string mat in ESD_MatrixRename.Keys)
                    ESD_ModifiedMatrices.Add(mat);
                foreach (string mat in ESD_MatrixTranslation.Keys)
                    ESD_ModifiedMatrices.Add(mat);
                foreach (string mat in ESD_MatrixScale.Keys)
                    ESD_ModifiedMatrices.Add(mat);
                foreach (string mat in ESD_MatrixRotation.Keys)
                    ESD_ModifiedMatrices.Add(mat);
            }
            public int ESD_Detail_Level;
            public int ESD_Alternative_Texture;
            public ESD_Bounding_Box ESD_Bounding_Box;
            public bool ESD_No_Visual_Obstruction;
            public bool ESD_Snapable;
            public bool ESD_SubObj;
            public string ESD_SoundFileName = "";
            public float ESD_CustomAnimationFPS = 8;
            // Dictionary of <original texture name, replacement texture name>
            public Dictionary<string, string> ESD_TextureReplacement = new Dictionary<string, string>();
            // Dictionary of <original matrix name, replacement matrix name>
            public Dictionary<string, string> ESD_MatrixRename = new Dictionary<string, string>();
            // Set of matrix names that are modified in some way or another
            public HashSet<string> ESD_ModifiedMatrices = new HashSet<string>();
            // Dictionary of <matrix name, matrix translation x/y/z vector>
            public Dictionary<string, Vector3> ESD_MatrixTranslation = new Dictionary<string, Vector3>();
            // Dictionary of <matrix name, matrix scale x/y/z vector>
            public Dictionary<string, Vector3> ESD_MatrixScale = new Dictionary<string, Vector3>();
            // Dictionary of <matrix name, matrix rotation x/y/z vector>
            public Dictionary<string, Vector3> ESD_MatrixRotation = new Dictionary<string, Vector3>();

            // Handle parameters concerning replacement of string values
            protected void ParseReplacementStrings(STFReader stf, ref Dictionary<string, string> renamePairs)
            {
                stf.MustMatch("(");
                // Allow for multiple pairs of replaced and replacement values
                while (!stf.EndOfBlock())
                {
                    string replaced = stf.ReadString();
                    string replacement = stf.ReadString();
                    // Add pair of values so long as we haven't reached the end of block
                    if (replaced != ")" && replacement != ")")
                        renamePairs.Add(replaced, replacement);
                }
            }

            // Handle matrix adjustment parameters
            protected void ParseMatrixOverride(STFReader.UNITS units, STFReader stf, ref Dictionary<string, Vector3> matrixParams)
            {
                Vector3 data = new Vector3(0);

                stf.MustMatch("(");
                string matName = stf.ReadString();
                data = stf.ReadVector3(units, Vector3.Zero);
                stf.SkipRestOfBlock();

                matrixParams.Add(matName, data);
            }
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
