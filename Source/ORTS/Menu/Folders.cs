// COPYRIGHT 2011 by the Open Rails project.
// This code is provided to enable you to contribute improvements to the open rails program.  
// Use of the code for any other purpose or distribution of the code to anyone else
// is prohibited without specific written permission from admin@openrails.org.

using System.Collections.Generic;
using System.IO;
using MSTS;

namespace ORTS.Menu
{
    public class Folder
    {
        public const string FolderDataFileName = "folder.dat";

        public readonly string Name;
        public readonly string Path;

        public Folder(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public static List<Folder> GetFolders()
        {
            var dataFile = Program.UserDataFolder + @"\" + FolderDataFileName;
            var folders = new List<Folder>();

            if (File.Exists(dataFile))
            {
                using (var inf = new BinaryReader(File.Open(dataFile, FileMode.Open)))
                {
                    var count = inf.ReadInt32();
                    for (var i = 0; i < count; ++i)
                    {
                        var path = inf.ReadString();
                        var name = inf.ReadString();
                        folders.Add(new Folder(name, path));
                    }
                }
            }

            if (folders.Count == 0)
            {
                try
                {
                    folders.Add(new Folder("- Default -", MSTSPath.Base()));
                }
                catch { }
            }

            return folders;
        }
    }
}