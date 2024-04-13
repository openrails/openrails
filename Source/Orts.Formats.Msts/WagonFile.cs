// COPYRIGHT 2013, 2015 by the Open Rails project.
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
using System.Collections;
using System.IO;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// Work with wagon files
    /// </summary>
    public class WagonFile
    {
        public class CarSize
        {
            public float CarWidth;
            public float CarHeight;
            public float CarLength;

            public CarSize(STFReader stf)
            {
                stf.MustMatch("(");
                CarWidth = stf.ReadFloat(STFReader.UNITS.Distance, null);
                CarHeight = stf.ReadFloat(STFReader.UNITS.Distance, null);
                CarLength = stf.ReadFloat(STFReader.UNITS.Distance, null);
                stf.SkipRestOfBlock(); // or should this be stf.MustMatch(")")
            }

            public override string ToString()
            {
                return CarWidth.ToString() + "x" + CarHeight.ToString() + "x" + CarLength.ToString();
            }
        }

        public string Name;
        public float MassKG;
        public float LengthM;
        public float MaxBrakeForceN;

        public WagonFile(string filePath)
        {
            Name = Path.GetFileNameWithoutExtension(filePath);
            using (var stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("wagon", ()=>{
                        stf.ReadString();
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                            new STFReader.TokenProcessor("mass", ()=>{ MassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); }),
                            new STFReader.TokenProcessor("size", ()=>{ LengthM = new CarSize( stf).CarLength; }),
                            new STFReader.TokenProcessor("maxbrakeforce", ()=>{ MaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); }),
                        });
                    }),
                });
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
