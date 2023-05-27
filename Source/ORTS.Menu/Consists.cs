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
using GNU.Gettext;
using Orts.Formats.Msts;

namespace ORTS.Menu
{
    public class Consist
    {
        public readonly string Name;
        public readonly Locomotive Locomotive = new Locomotive("unknown");
        public readonly string FilePath;

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

        internal Consist(string filePath, Folder folder)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var conFile = new ConsistFile(filePath);
                    Name = conFile.Name.Trim();
                    Locomotive = GetLocomotive(conFile, folder);
                }
                catch
                {
                    Name = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
                if (Locomotive == null) throw new InvalidDataException("Consist '" + filePath + "' is excluded.");
                if (string.IsNullOrEmpty(Name)) Name = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            else
            {
                Name = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            FilePath = filePath;
        }

        internal Consist(string filePath, Folder folder, bool reverseConsist)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var conFile = new ConsistFile(filePath);
                    Name = conFile.Name.Trim();
                    Locomotive = reverseConsist ? GetLocomotiveReverse(conFile, folder) : GetLocomotive(conFile, folder);
                }
                catch
                {
                    Name = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
                if (Locomotive == null) throw new InvalidDataException("Consist '" + filePath + "' is excluded.");
                if (string.IsNullOrEmpty(Name)) Name = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            else
            {
                Name = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
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

        public static Consist GetConsist(Folder folder, string name)
        {
            Consist consist = null;
            var directory = System.IO.Path.Combine(System.IO.Path.Combine(folder.Path, "TRAINS"), "CONSISTS");
            var file = System.IO.Path.Combine(directory, System.IO.Path.ChangeExtension(name, "con"));

            try
            {
                consist = new Consist(file, folder);
            }
            catch { }

            return consist;
        }

        public static Consist GetConsist(Folder folder, string name, bool reverseConsist)
        {
            Consist consist = null;
            var directory = System.IO.Path.Combine(System.IO.Path.Combine(folder.Path, "TRAINS"), "CONSISTS");
            var file = System.IO.Path.Combine(directory, System.IO.Path.ChangeExtension(name, "con"));

            try
            {
                consist = new Consist(file, folder, reverseConsist);
            }
            catch { }

            return consist;
        }

        static Locomotive GetLocomotive(ConsistFile conFile, Folder folder)
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

        static Locomotive GetLocomotiveReverse(ConsistFile conFile, Folder folder)
        {
            Locomotive newLocomotive = null;

            foreach (var wagon in conFile.Train.TrainCfg.WagonList.Where(w => w.IsEngine))
            {
                var filePath = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(folder.Path, "TRAINS"), "TRAINSET"), wagon.Folder), wagon.Name + ".eng");
                try
                {
                    newLocomotive = new Locomotive(filePath);
                }
                catch { }
            }
            return (newLocomotive);
        }

    }

    public class Locomotive
    {
        public readonly string Name;
        public readonly string Description;
        public readonly string FilePath;

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

        public Locomotive()
            : this(null)
        {
        }

        internal Locomotive(string filePath)
        {
            if (filePath == null)
            {
                Name = catalog.GetString("- Any Locomotive -");
            }
            else if (File.Exists(filePath))
            {
                EngineFile engFile;
                try
                {
                    engFile = new EngineFile(filePath);
                }
                catch
                {
                    Name = $"<{catalog.GetString("load error:")} {System.IO.Path.GetFileNameWithoutExtension(filePath)}>";
                    engFile = null;
                }
                if (engFile != null)
                {
                    bool showInList = !string.IsNullOrEmpty(engFile.CabViewFile);
                    if (!showInList)
                        throw new InvalidDataException(catalog.GetStringFmt("Locomotive '{0}' is excluded.", filePath));

                    string name = (engFile.Name ?? "").Trim();
                    Name = name != "" ? name : $"<{catalog.GetString("unnamed:")} {System.IO.Path.GetFileNameWithoutExtension(filePath)}>";

                    string description = (engFile.Description ?? "").Trim();
                    if (description != "")
                        Description = description;
                }
            }
            else
            {
                Name = $"<{catalog.GetString("missing:")} {System.IO.Path.GetFileNameWithoutExtension(filePath)}>";
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
