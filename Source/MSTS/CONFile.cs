// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
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

namespace MSTS
{
    /// <summary>
    /// Work with consist files
    /// </summary>
    public class CONFile
    {
        public string FileName { get; set; }   // no extension, no path
        public string Description { get; set; } // form the Name field or label field of the consist file
        public Train_Config Train { get; set; }

        public CONFile(string filenamewithpath)
        {
            FileName = Path.GetFileNameWithoutExtension(filenamewithpath);
            Description = FileName;
            using (STFReader stf = new STFReader(filenamewithpath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("train", ()=>{ Train = new Train_Config(stf); }),
                });
        }

        public override string ToString()
        {
            return this.Train.TrainCfg.Name;
        }
    }
}

