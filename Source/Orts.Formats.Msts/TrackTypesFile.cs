// COPYRIGHT 2009, 2010, 2011, 2012 by the Open Rails project.
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

    // TODO - this is an incomplete parse of the cvf file.
    public class TrackTypesFile : List<TrackTypesFile.TrackType>
    {

        public TrackTypesFile(string filePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                var count = stf.ReadInt(null);
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tracktype", ()=>{
                        if (--count < 0)
                            STFException.TraceWarning(stf, "Skipped extra TrackType");
                        else
                            Add(new TrackType(stf));
                    }),
                });
                if (count > 0)
                    STFException.TraceWarning(stf, count + " missing TrackType(s)");
            }
        }

        public class TrackType
        {
            public string Label;
            public string InsideSound;
            public string OutsideSound;

            public TrackType(STFReader stf)
            {
                stf.MustMatch("(");
                Label = stf.ReadString();
                InsideSound = stf.ReadString();
                OutsideSound = stf.ReadString();
                stf.SkipRestOfBlock();
            }
        } // TrackType

    } // class CVFFile
}

