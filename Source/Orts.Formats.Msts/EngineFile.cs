// COPYRIGHT 2013 by the Open Rails project.
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
    /// Work with engine files
    /// </summary>
    public class EngineFile
    {
        public string Name;
        public string EngineType;
        public float MaxPowerW;
        public float MaxForceN;
        public float MaxDynamicBrakeForceN;
        public float MaxSpeedMps;
        public int NumDriveAxles;  // ORTS
        public float NumEngWheels;  // MSTS
        public string Description;
        public string CabViewFile;

        public EngineFile(string filePath)
        {
            Name = Path.GetFileNameWithoutExtension(filePath);
            using (var stf = new STFReader(filePath, false))
            {
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("engine", ()=>{
                        stf.ReadString();
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                            new STFReader.TokenProcessor("type", ()=>{ EngineType = stf.ReadStringBlock(null); }),
                            new STFReader.TokenProcessor("maxpower", ()=>{ MaxPowerW = stf.ReadFloatBlock( STFReader.UNITS.Power, null); }),
                            new STFReader.TokenProcessor("maxforce", ()=>{ MaxForceN = stf.ReadFloatBlock( STFReader.UNITS.Force, null); }),
                            new STFReader.TokenProcessor("dynamicbrakesmaximumforce", ()=>{ MaxDynamicBrakeForceN = stf.ReadFloatBlock( STFReader.UNITS.Force, null); }),
                            new STFReader.TokenProcessor("maxvelocity", ()=>{ MaxSpeedMps = stf.ReadFloatBlock( STFReader.UNITS.Speed, null); }),
                            new STFReader.TokenProcessor("ortsnumberdriveaxles", ()=>{ NumDriveAxles = stf.ReadIntBlock(null); }),
                            new STFReader.TokenProcessor("numwheels", ()=>{ NumEngWheels = stf.ReadFloatBlock( STFReader.UNITS.None, null); }),
                            new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(null); }),
                            new STFReader.TokenProcessor("cabview", ()=>{ CabViewFile = stf.ReadStringBlock(null); }),
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
