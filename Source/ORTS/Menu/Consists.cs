// COPYRIGHT 2012, 2013 by the Open Rails project.
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
using System.Linq;
using MSTS;

namespace ORTS.Menu
{
    public class Consist
    {
        public readonly string Name;
        public readonly Locomotive Locomotive = new Locomotive("unknown");
        public readonly string FilePath;

        internal Consist(string filePath, Folder folder)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var conFile = new CONFile(filePath);
                    Name = conFile.Name.Trim();
                    Locomotive = GetLocomotive(conFile, folder);
                }
                catch
                {
                    Name = "<load error: " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
            }
            else
            {
                Name = "<missing: " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            FilePath = filePath;
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
                        consists.Add(new Consist(consist, folder));
                    }
                    catch { }
                }
            }
            return consists;
        }

        Locomotive GetLocomotive(CONFile conFile, Folder folder)
        {
            foreach (var wagon in conFile.Train.TrainCfg.WagonList.Where(w => w.IsEngine))
            {
                var filePath = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(folder.Path, "TRAINS"), "TRAINSET"), wagon.Folder), wagon.Name + ".eng");
                try
                {
                    return new Locomotive(filePath);
                }
                catch { }
            }
            return null;
        }
    }

    public class Locomotive
    {
        public readonly string Name;
        public readonly string Description;
        public readonly string FilePath;

        internal Locomotive(string filePath)
        {
            if (filePath == null)
            {
                Name = "- Any Locomotive -";
            }
            else if (File.Exists(filePath))
            {
                try
                {
                    var engFile = new ENGFile(filePath);
                    Name = engFile.Name.Trim();
                    Description = engFile.Description.Trim();
                }
                catch
                {
                    Name = "<load error: " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
            }
            else
            {
                Name = "<missing: " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            FilePath = filePath;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return obj is Locomotive && ((obj as Locomotive).Name == Name || (obj as Locomotive).FilePath == null || FilePath == null);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
