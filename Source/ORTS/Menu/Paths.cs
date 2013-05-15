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

using System;
using System.Collections.Generic;
using System.IO;
using MSTS;

namespace ORTS.Menu
{
    public class Path
    {
        public readonly string Name;
        public readonly string Start;
        public readonly string End;
        public readonly string FilePath;
        public readonly PATFile PATFile;

        public Path(string filePath, PATFile patFile)
        {
            Name = patFile.Name;
            Start = patFile.Start;
            End = patFile.End;
            FilePath = filePath;
            PATFile = patFile;
        }

        public override string ToString()
        {
            return String.Format("{0} - {1}", Start, End);
        }

        public static List<Path> GetPaths(Route route)
        {
            var paths = new List<Path>();
            var directory = System.IO.Path.Combine(route.Path, "PATHS");
            if (Directory.Exists(directory))
            {
                foreach (var path in Directory.GetFiles(directory, "*.pat"))
                {
                    try
                    {
                        var patFile = new PATFile(path);
                        paths.Add(new Path(path, patFile));
                    }
                    catch { }
                }
            }
            return paths;
        }
    }
}
