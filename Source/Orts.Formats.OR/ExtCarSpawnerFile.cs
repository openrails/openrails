// COPYRIGHT 2011, 2012 by the Open Rails project.
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
using Orts.Formats.Msts;
using Orts.Parsers.Msts;


namespace Orts.Formats.OR
{
    public class ExtCarSpawnerFile
    {
        public ExtCarSpawnerFile(string filePath, string shapePath, List<CarSpawnerList> carSpawnerLists)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                var listCount = stf.ReadInt(null);
                string listName = null;
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("carspawnerlist", ()=>{
                        if (--listCount < 0)
                            STFException.TraceWarning(stf, "Skipped extra CarSpawner List");
                        else
                        {
                            stf.MustMatch("(");
                            stf.MustMatch("ListName");
                            listName = stf.ReadStringBlock(null);
                            var carSpawnerBlock = new CarSpawnerBlock(stf, shapePath, carSpawnerLists, listName);
                        }
                    }),
                });
                if (listCount > 0)
                    STFException.TraceWarning(stf, listCount + " missing CarSpawner List(s)");
            }

        }

    } // class ExtCarSpawnerFile
}

