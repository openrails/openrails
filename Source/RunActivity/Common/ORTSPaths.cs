// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;


namespace ORTS
{
    class ORTSPaths
    {
        //<CJ Comment> Cleaner to use GetFileFromFolders() instead, but not sure how to test this. </CJ Comment>
        public static string FindTrainCarPlugin( string initialFolder, string filename )
        {
            string dllPath = initialFolder + "\\" + filename;  // search in trainset folder
            if (File.Exists(dllPath))
                return dllPath;
            string rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(initialFolder)))+ "\\OpenRails";
            if( Directory.Exists( rootFolder ) )
            {
                dllPath = rootFolder + "\\" + filename;
                if (File.Exists(dllPath))
                    return dllPath;
            }
            return filename;   // then search in OpenRails program folder
        }

        /// <summary>
        /// Static variables to reduce occurrence of duplicate warning messages.
        /// </summary>
        static string badBranch = "";
        static string badPath = "";

        /// <summary>
        /// Search an array of paths for a file. Paths must be in search sequence.
        /// No need for trailing "\" on path or leading "\" on branch parameter.
        /// </summary>
        /// <param name="pathArray">2 or more folders, e.g. "D:\MSTS", E:\OR"</param>
        /// <param name="branch">a filename possibly prefixed by a folder, e.g. "folder\file.ext"</param>
        /// <returns>null or the full file path of the first file found</returns>
        public static string GetFileFromFolders(string[] pathArray, string branch)
        {
            if (branch == null) return null;

            foreach (var path in pathArray)
            {
                if (path != null)
                {
                    var fullPath = Path.Combine(path, branch);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }
            var firstPath = pathArray[0];
            if (branch != badBranch || firstPath != badPath)
            {
                Trace.TraceWarning("Sound file {0} missing from {1}", branch, firstPath);
                badBranch = branch;
                badPath = firstPath;
            }
            return null;
        }
    }
}
