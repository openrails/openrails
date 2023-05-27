// COPYRIGHT 2014 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using Orts.Formats.Msts;

namespace ORTS.ContentManager.Models
{
    public class Route
    {
        public readonly string Name;
        public readonly string Description;

        public Route(Content content)
        {
            Debug.Assert(content.Type == ContentType.Route);
            if (System.IO.Path.GetExtension(content.PathName).Equals("", StringComparison.OrdinalIgnoreCase))
            {
                var file = new RouteFile(GetTRKFileName(content.PathName));
                Name = file.Tr_RouteFile.Name;
                Description = file.Tr_RouteFile.Description;
            }
        }

        static string GetTRKFileName(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException(folderPath);
            var fileNames = Directory.GetFiles(folderPath, "*.trk");
            if (fileNames.Length == 0)
                throw new FileNotFoundException("TRK file not found in '" + folderPath + "'.", System.IO.Path.Combine(folderPath, "*.trk"));
            return fileNames[0];
        }
    }
}
