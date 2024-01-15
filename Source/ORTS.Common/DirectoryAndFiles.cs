// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014, 2015, 2023 by the Open Rails project.
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
//

using System;
using System.Collections.Generic;
using System.IO;

namespace ORTS.Common
{
    public class DirectoryAndFiles
    {
        public static void directoryDelete(string directoryName)
        {
            if (Directory.Exists(directoryName))
            {
                // remove the read only flags,
                // otherwise the Directory.delete does not work in case read only files exists
                directoryRemoveReadOnlyFlags(directoryName);
                Directory.Delete(directoryName, true);
            }
        }

        private static void directoryRemoveReadOnlyFlags(string directoryName)
        {
            foreach (string filename in Directory.GetFiles(directoryName))
            {
                _ = new FileInfo(filename)
                {
                    IsReadOnly = false
                };
            }
            foreach (string subDirectoryName in Directory.GetDirectories(directoryName))
            {
                directoryRemoveReadOnlyFlags(subDirectoryName);
            }
        }

        //
        // example:
        //  input:  D:\OpenRailsMaster\Program
        //  output: D:\OpenRailsMaster
        //
        public static string determineTopDirectory(string directoryName)
        { 
            string tmpDirectoryName = directoryName;
            string topDirectoryName = directoryName;

            while (Directory.GetParent(tmpDirectoryName) != null)
            {
                topDirectoryName = tmpDirectoryName;
                tmpDirectoryName = Directory.GetParent(tmpDirectoryName).ToString();
            }

            return topDirectoryName;
        }

        public static List<FileInfo> getChangedAndAddedFiles(string directoryName, DateTime dateInstalled, bool checkForChanged)
        {
            List<FileInfo> changedFiles = new List<FileInfo>();

            if (Directory.Exists(directoryName))
            {
                getChangedAndAddedFilesDeeper(directoryName, dateInstalled, changedFiles, checkForChanged);
            }

            return changedFiles;
        }

        public static void getChangedAndAddedFilesDeeper(string directoryName, DateTime dateInstalled, List<FileInfo> changedFiles, bool checkForChanged)
        {
            foreach (string filename in Directory.GetFiles(directoryName))
            {
                FileInfo fi = new FileInfo(filename);
                if (checkForChanged) 
                {
                    if (fi.LastWriteTime > dateInstalled)
                    {
                        changedFiles.Add(fi);
                    }
                }
                else
                {
                    // check for new files, creation date after date installed
                    if (fi.CreationTime > dateInstalled)
                    {
                        changedFiles.Add(fi);
                    }
                }

            }
            foreach (string subDirectoryName in Directory.GetDirectories(directoryName))
            {
                getChangedAndAddedFilesDeeper(subDirectoryName, dateInstalled, changedFiles, checkForChanged);
            }
        }
    }
}
