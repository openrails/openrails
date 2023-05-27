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

namespace Orts.Formats.OR
{
    /// <summary>
    /// class TimetableGroupFileLite
    /// Creates pre-info for Multi TT file
    /// returns Description and list of pre-info per file
    /// </summary>

    public class TimetableGroupFileLite
    {
        public List<TimetableFileLite> ORTTInfo = new List<TimetableFileLite>();
        public String Description;

        public TimetableGroupFileLite(String filePath, String directory)
        {
            Description = String.Empty;
            try
            {
                using (StreamReader scrStream = new StreamReader(filePath, true))
                {
                    MultiTTFilePreliminaryRead(filePath, directory, scrStream);
                    if (String.IsNullOrEmpty(Description)) Description = String.Copy(filePath);
                }
            }
            catch (Exception)
            {
                Description = "<" + "load error:" + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="SingleTTInfo"></param>

        public TimetableGroupFileLite(TimetableFileLite SingleTTInfo)
        {
            Description = String.Copy(SingleTTInfo.Description);
            ORTTInfo.Add(SingleTTInfo);
        }

        /// <summary>
        /// ORMultiTTFilePreliminaryRead
        /// Reads MultiTTfile for preliminary info
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="directory"></param>
        /// <param name="scrStream"></param>

        void MultiTTFilePreliminaryRead(String filePath, String directory, StreamReader scrStream)
        {
            var (description, ttfiles) = TimetableGroupFileUtilities.ReadMultiTTFiles(directory, scrStream);
            Description = description;
            ORTTInfo = (from ttfile in ttfiles
                        select new TimetableFileLite(ttfile)).ToList();
        }
    }

    /// <summary>
    /// class TimetableGroupFile
    /// extracts filenames from multiTTfile, extents names to full path
    /// </summary>

    public class TimetableGroupFile
    {
        public List<string> TTFiles = new List<string>();
        public string Description;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="directory"></param>

        public TimetableGroupFile(String filePath, String directory)
        {
            Description = String.Empty;
            try
            {
                using (StreamReader scrStream = new StreamReader(filePath, true))
                {
                    MultiTTFileRead(filePath, directory, scrStream);
                    if (String.IsNullOrEmpty(Description)) Description = String.Copy(filePath);
                }
            }
            catch (Exception)
            {
                Description = "<" + "load error:" + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
        }

        /// <summary>
        /// MultiTTFileRead
        /// Reads multiTTfile and extracts filenames
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="directory"></param>
        /// <param name="scrStream"></param>

        void MultiTTFileRead(String filePath, String directory, StreamReader scrStream)
        {
            var (description, ttfiles) = TimetableGroupFileUtilities.ReadMultiTTFiles(directory, scrStream);
            Description = description;
            TTFiles = ttfiles.ToList();
        }
    }

    internal class TimetableGroupFileUtilities
    {
        /// <summary>
        /// Extract a description and list of file paths from a MultiTTfile.
        /// </summary>
        /// <param name="directory">The directory where the MultiTTfile is located, to prepend to all filenames.</param>
        /// <param name="scrStream">The MultiTTfile stream reader.</param>
        /// <returns>The description (null if not present) and the list of file paths.</returns>
        public static (string, IEnumerable<string>) ReadMultiTTFiles(string directory, StreamReader scrStream)
        {
            string description = null;
            var ttfiles = new List<string>();
            var validLines = scrStream
                .ReadToEnd()
                .Split(new[] { '\n', '\r' })
                .Where((string l) => !string.IsNullOrEmpty(l));
            foreach (string readLine in validLines)
            {
                if (readLine[0] == '#')
                {
                    if (string.IsNullOrEmpty(description))
                        description = readLine.Substring(1);
                }
                else
                {
                    ttfiles.Add(Path.Combine(directory, readLine));
                }
            }
            return (description, ttfiles);
        }
    }
}
