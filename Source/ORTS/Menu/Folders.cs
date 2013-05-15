// COPYRIGHT 2011, 2012 by the Open Rails project.
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

using System.Collections.Generic;
using System.IO;
using MSTS;

namespace ORTS.Menu
{
    public class Folder
    {
        public readonly string Name;
        public readonly string Path;

        public Folder(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public override string ToString()
        {
            return Name;
        }

        public static string FolderDataFile
        {
            get
            {
                return Program.UserDataFolder + @"\folder.dat";
            }
        }

        public static List<Folder> GetFolders()
        {
            var folders = new List<Folder>();

            if (File.Exists(FolderDataFile))
            {
                using (var inf = new BinaryReader(File.Open(FolderDataFile, FileMode.Open)))
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

        public static void SetFolders(List<Folder> folders)
        {
            using (var outf = new BinaryWriter(File.Open(FolderDataFile, FileMode.Create)))
            {
                outf.Write(folders.Count);
                foreach (var folder in folders)
                {
                    outf.Write(folder.Path);
                    outf.Write(folder.Name);
                }
            }
        }
    }
}
