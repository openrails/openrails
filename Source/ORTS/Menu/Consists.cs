// COPYRIGHT 2012 by the Open Rails project.
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
using System.IO;
using MSTS;

namespace ORTS.Menu
{
    public class Consist
    {
        public readonly string Name;
        public readonly string FilePath;
        public readonly CONFile CONFile;

        public Consist(string filePath, CONFile conFile)
        {
            Name = conFile.Description;
            FilePath = filePath;
            CONFile = conFile;
        }

        public override string ToString()
        {
            return Name;
        }

        public static List<Consist> GetConsists(Folder folder)
        {
            var consists = new List<Consist>();
            var directory = System.IO.Path.Combine(System.IO.Path.Combine(folder.Path, "TRAINS"), "CONSISTS");
            if (Directory.Exists(directory))
            {
                foreach (var consist in Directory.GetFiles(directory, "*.con"))
                {
                    try
                    {
                        var conFile = new CONFile(consist);
                        consists.Add(new Consist(consist, conFile));
                    }
                    catch { }
                }
            }
            return consists;
        }
    }
}
