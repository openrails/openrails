// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

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