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
    public static class ORFileHelper
    {
        /// <summary>
        /// Given any file path, returns a path to the same file type and name but contained inside an
        /// "OpenRails" subfolder inside the same folder as the original file. Usually used to determine
        /// the location of an "Open Rails specific" version of a file to load alongside the original
        /// "MSTS specific" file. <br />
        /// (See <see cref="FindORTSFile(string)"/> if the ORTS-specific file should not be returned
        /// unless it exists.)
        /// 
        /// <para>
        /// eg: Given <c>"MSTS\TRAINS\TRAINSET\DASH9\dash9.eng"</c>, returns <c>"MSTS\TRAINS\TRAINSET\DASH9\OpenRails\dash9.eng"</c>
        /// </para>
        /// 
        /// See pull request #1215 for additional context.
        /// </summary>
        /// <param name="path">File path to a file that an OR-specific equivalent is desired for.</param>
        /// <returns>File path string pointing to the same file given in the <paramref name="path"/> parameter,
        /// but contained inside an "OpenRails" subfolder.</returns>
        public static string GetORTSFilePath(string path)
        {
            if (String.IsNullOrEmpty(path))
                return "";

            return Path.GetDirectoryName(path) + @"\OpenRails\" + Path.GetFileName(path);
        }

        /// <summary>
        /// <para>
        /// Given any file path, returns a path to the same file type and name but contained inside an
        /// "OpenRails" subfolder if such a file exists. Otherwise, returns the original file path. Usually
        /// used to determine which file should be loaded in any case where an "Open Rails specific" file
        /// should outright replace an "MSTS specific" file. <br />
        /// (See <see cref="GetORTSFilePath(string)"/> if the OR-specific file path should be returned
        /// even if it does not exist.)
        /// </para>
        /// 
        /// <para>
        /// eg: Given <c>"MSTS\TRAINS\TRAINSET\DASH9\dash9.eng"</c>, returns <c>"MSTS\TRAINS\TRAINSET\DASH9\OpenRails\dash9.eng"</c>
        /// if that file exists, else returns <c>"MSTS\TRAINS\TRAINSET\DASH9\dash9.eng"</c>
        /// </para>
        /// 
        /// See pull request #1215 for additional context.
        /// </summary>
        /// <param name="path">File path to a file to be replaced with an OR-specific file, if available.</param>
        /// <returns>If it exists, file path string pointing to the same file given in the <paramref name="path"/> parameter,
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
