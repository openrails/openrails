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
using System.IO;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    /// <summary>
    /// Work with wagon files
    /// </summary>
    public class WagonFile
    {
        public const float ImpossiblyHighForceN = 9.999e8f;

        public string Name;
        public string WagonType;
        public float MassKG;
        public CarSize WagonSize;
        public int NumWagAxles;  // ORTS
        public float NumWagWheels;  // MSTS
        public float MaxBrakeForceN;
        public float MinCouplerStrengthN = ImpossiblyHighForceN;

        public class CarSize
        {
            public float WidthM;
            public float HeightM;
            public float LengthM;

            public CarSize(STFReader stf)
            {
                stf.MustMatch("(");
                WidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                HeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                LengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                stf.MustMatch(")");
            }

            public override string ToString()
            {
                return WidthM.ToString() + "x" + HeightM.ToString() + "x" + LengthM.ToString();
            }
        }

        public class CouplingSpring
        {
            public CouplingSpring(ref float minCouplerStrength, STFReader stf)
            {
                float breakVal = ImpossiblyHighForceN;
                float ortsBreakVal = ImpossiblyHighForceN;

                stf.MustMatch("(");
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("spring", () =>
                    {
                        stf.MustMatch("(");
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("ortsbreak", ()=>
                            {
                                stf.MustMatch("(");
                                float val = stf.ReadFloat(STFReader.UNITS.Force, null);
                                if (val > 9.9) ortsBreakVal = Math.Min(val, ortsBreakVal);
                                val = stf.ReadFloat(STFReader.UNITS.Force, null);
                                if (val > 9.9) ortsBreakVal = Math.Min(val, ortsBreakVal);
                                stf.SkipRestOfBlock();
                            }),
                            new STFReader.TokenProcessor("break", () =>
                            {
                                stf.MustMatch("(");
                                float val = stf.ReadFloat(STFReader.UNITS.Force, null);
                                if (val > 9.9) breakVal = Math.Min(val, breakVal);
                                val = stf.ReadFloat(STFReader.UNITS.Force, null);
                                if (val > 9.9) breakVal = Math.Min(val, breakVal);
                                stf.SkipRestOfBlock();
                            })
                        });
                    })
                });
                minCouplerStrength = Math.Min(minCouplerStrength, ortsBreakVal < ImpossiblyHighForceN ? ortsBreakVal : breakVal);
            }
        }

        public WagonFile(string filePath)
        {
            Name = Path.GetFileNameWithoutExtension(filePath);
            using (var stf = new STFReader(filePath, false))
            {
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("wagon", ()=>{
                        stf.ReadString();
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                            new STFReader.TokenProcessor("type", ()=>{ WagonType = stf.ReadStringBlock(null); }),
                            new STFReader.TokenProcessor("mass", ()=>{ MassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); }),
                            new STFReader.TokenProcessor("size", ()=>{ WagonSize = new CarSize( stf); }),
                            new STFReader.TokenProcessor("ortsnumberaxles", ()=>{ NumWagAxles = stf.ReadIntBlock(null); }),
                            new STFReader.TokenProcessor("numwheels", ()=>{ NumWagWheels = stf.ReadFloatBlock( STFReader.UNITS.None, null); }),
                            new STFReader.TokenProcessor("maxbrakeforce", ()=>{ MaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); }),
                            new STFReader.TokenProcessor("coupling", ()=>{ new CouplingSpring( ref MinCouplerStrengthN, stf); })
                        });
                    }),
                });
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
