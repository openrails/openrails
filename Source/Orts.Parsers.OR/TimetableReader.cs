// COPYRIGHT 2014, 2015 by the Open Rails project.
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
using System.IO;
using System.Linq;
using System.Text;

namespace Orts.Parsers.OR
{
    /// <summary>
    /// Reads a timetable file in to a 2D collection of unprocessed strings.
    /// </summary>
    public class TimetableReader
    {
        public List<string[]> Strings = new List<string[]>();
        public string FilePath;

        public TimetableReader(string filePath)
        {
            FilePath = filePath;
            using (var filestream = new StreamReader(filePath, true))
            {
                // read all lines in file
                var readLine = filestream.ReadLine();

                // extract separator from first line
                var separator = readLine.Substring(0, 1);

                // check : only ";" or "," or "\tab" are allowed as separators
                var validSeparator = separator == ";" || separator == "," || separator == "\t";
                if (!validSeparator)
                {
                    throw new InvalidDataException(String.Format("Expected separator ';' or ','; got '{1}' in timetable {0}", filePath, separator));
                }

                // extract and store all strings
                do
                {
                    Strings.Add(readLine.Split(separator[0]));
                    readLine = filestream.ReadLine();
                } while (readLine != null);
            }
        }
    }
}
