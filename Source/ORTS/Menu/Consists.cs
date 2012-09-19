// COPYRIGHT 2012 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

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