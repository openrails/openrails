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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Parsers.OR
{
    /// <summary>
    /// Reads a timetable file into a 2D collection of strings which will need further processing.
    /// </summary>
    public class TimetableReader
    {
        // The data is saved as a list of spreadsheet rows and each row is an array of strings, one string for each cell.
        public List<string[]> Strings = new List<string[]>();
        public string FilePath;

        public TimetableReader(string filePath)
        {
            FilePath = filePath;
            using (var filestream = new StreamReader(filePath, true))
            {
                var readLine = filestream.ReadLine();

                // Extract the separator character from start of the first line.
                char separator = readLine[0];

                var validSeparators = ";,\t";
                if (validSeparators.Contains(separator) == false) // Fatal error
                {
                    throw new InvalidDataException($"Expected separators are {validSeparators} and tab but found '{separator}' as first character of timetable {filePath}");
                }

                // Process the first line and then the remaining lines extracting each cell and storing it as a string in a list of arrays of strings.
                do
                {
                    Strings.Add(
                        (from s in readLine.Split(separator)
                         select s.Trim() // Remove leading and trailing whitespace which is difficult to see in a spreadsheet and leads to parse failures which are hard to find.
                        ).ToArray());
                    readLine = filestream.ReadLine();
                } while (readLine != null);
            }
        }
    }
}
