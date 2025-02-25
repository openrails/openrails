// COPYRIGHT 2009, 2010, 2011, 2013 by the Open Rails project.
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
    /// Work with consist files
    /// </summary>
    public class ConsistFile
    {
        public string Name; // from the Name field or label field of the consist file
        public Train_Config Train;

        public ConsistFile(string filePath)
        {
            using (var stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("train", ()=>{ Train = new Train_Config(stf); }),
                });
            Name = Train.TrainCfg.Name;
        }

        /// <summary>
        /// Allows to use a procedurally generated consist file.
        /// </summary>
        public ConsistFile(Stream inputStream, string filePath)
        {
            using (var stf = new STFReader(inputStream, filePath, System.Text.Encoding.UTF8, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("train", ()=>{ Train = new Train_Config(stf); }),
                });
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
