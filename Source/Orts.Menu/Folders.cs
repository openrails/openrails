// COPYRIGHT 2011, 2012, 2013, 2014 by the Open Rails project.
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

using ORTS.Settings;
using System.Collections.Generic;
using System.IO;

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

        public static List<Folder> GetFolders(UserSettings settings)
        {
            var folderDataFile = UserSettings.UserDataFolder + @"\folder.dat";
            var folders = new List<Folder>();

            if (settings.Folders.Folders.Count == 0 && File.Exists(folderDataFile))
            {
                try
                {
                    using (var inf = new BinaryReader(File.Open(folderDataFile, FileMode.Open)))
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
                catch { }

                // Migrate from folder.dat to FolderSettings.
                foreach (var folder in folders)
                    settings.Folders.Folders[folder.Name] = folder.Path;
                settings.Folders.Save();
            }
            else
            {
                foreach (var folder in settings.Folders.Folders)
                    folders.Add(new Folder(folder.Key, folder.Value));
            }

            return folders;
        }

        public static void SetFolders(UserSettings settings, List<Folder> folders)
        {
            settings.Folders.Folders.Clear();
            foreach (var folder in folders)
                settings.Folders.Folders[folder.Name] = folder.Path;
            settings.Folders.Save();
        }
    }
}
