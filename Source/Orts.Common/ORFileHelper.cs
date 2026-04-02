// COPYRIGHT 2009 - 2026 by the Open Rails project.
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

namespace Orts.Common
{
    public class ORFileHelper
    {
        /// <summary>
        /// Given any file path, returns a path to the same file type and name but contained inside an
        /// "OpenRails" subfolder inside the same folder as the original file. Usually used to specify
        /// an "Open Rails only" file to be loaded instead of an original MSTS file.
        /// 
        /// eg: Given "MSTS\TRAINS\TRAINSET\DASH9\dash9.eng", returns "MSTS\TRAINS\TRAINSET\DASH9\OpenRails\dash9.eng"
        /// </summary>
        /// <param name="path">File path to the "MSTS" file to be replaced with an "OpenRails" file.</param>
        /// <returns>File path string pointing to the same file given in the 'path' parameter,
        /// but contained inside an "OpenRails" subfolder.</returns>
        public static string GetORTSFilePath(string path)
        {
            if (String.IsNullOrEmpty(path))
                return "";

            return Path.GetDirectoryName(path) + @"\OpenRails\" + Path.GetFileName(path);
        }

        /// <summary>
        /// Given any file path, returns a path to the same file type and name but contained inside an
        /// "OpenRails" subfolder if such a file exists. Otherwise, returns the original file path.
        /// </summary>
        /// <param name="path">File path to the "MSTS" file to be replaced with an "OpenRails" file.</param>
        /// <returns>If it exists, file path string pointing to the same file given in the 'path' parameter,
        /// but contained inside an "OpenRails" subfolder. Otherwise, returns original path.</returns>
        public static string FindORTSFile(string path)
        {
            string orPath = GetORTSFilePath(path);

            if (File.Exists(orPath))
                return orPath;
            else
                return path;
        }
    }
}
