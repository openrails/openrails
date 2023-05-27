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

using System;
using System.IO;
using Orts.Parsers.Msts;

// <Comment> This file parses only the shape names for temporary speed restrictions; the other shape names are not needed
// </Comment>
namespace Orts.Formats.Msts
{

    public class SpeedpostDatFile
    {
        public string[] TempSpeedShapeNames = new string[3];

        public SpeedpostDatFile(string filePath, string shapePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("speed_warning_sign_shape", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNames[0] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("restricted_shape", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNames[1] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                    new STFReader.TokenProcessor("end_restricted_shape", ()=>
                         {
                            var dataItem = stf.ReadStringBlock(null);
                             if (dataItem != null)
                             {
                                dataItem = shapePath + dataItem;
                                if (File.Exists(dataItem))
                                    TempSpeedShapeNames[2] = dataItem;
                                else
                                    STFException.TraceWarning(stf, String.Format("Non-existent shape file {0} referenced", dataItem));
                             }
                         }
                         ),
                });
            }
        }

    } // class SpeedpostDatFile
}

