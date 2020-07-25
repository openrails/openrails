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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GNU.Gettext;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using ORTS.Content;

namespace ORTS.Menu
{
    public class Consist
    {
        public readonly string Name;
        public readonly ISet<Locomotive> Locomotives = new HashSet<Locomotive>() { new Locomotive("unknown") };
        public readonly string FilePath;

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

        internal Consist(string filePath, Folder folder, IList<Folder> allFolders) : this(filePath, folder, allFolders, false) { }

        internal Consist(string filePath, Folder folder, IList<Folder> allFolders, bool reverseConsist)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var conFile = LoadConsist(filePath);
                    Name = conFile.DisplayName.Trim();
                    Locomotives = reverseConsist ? GetLocomotivesReverse(conFile, folder, allFolders) : GetLocomotives(conFile, folder, allFolders);
                }
                catch
                {
                    Name = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
                if (Locomotives.Count <= 0)
                    throw new InvalidDataException($"Consist '{filePath}' is excluded.");
                if (string.IsNullOrEmpty(Name)) Name = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            else
            {
                Name = "<" + catalog.GetString("missing:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
            }
            FilePath = filePath;
        }

        private static ITrainFile LoadConsist(string filePath)
        {
            switch (System.IO.Path.GetExtension(filePath).ToLowerInvariant())
            {
                case ".train-or":
                    return TrainFile.LoadFrom(filePath);
                case ".con":
                    return new ConsistFile(filePath);
                default:
                    throw new InvalidDataException("Unknown train format");
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public static List<Consist> GetConsists(Folder folder, IList<Folder> allFolders)
        {
            var consists = new List<Consist>();
            foreach (var consist in TrainFileUtilities.AllTrainFiles(folder.Path))
            {
                Consist loaded;
                try
                {
                    loaded = new Consist(consist, folder, allFolders);
                }
                catch
                {
                    continue;
                }
                consists.Add(loaded);
            }
            return consists;
        }

        public static Consist GetConsist(Folder folder, IList<Folder> allFolders, string name) => GetConsist(folder, allFolders, name, false);

        public static Consist GetConsist(Folder folder, IList<Folder> allFolders, string name, bool reverseConsist)
        {
            Consist consist = null;
            var file = TrainFileUtilities.ResolveTrainFile(folder.Path, name);

            try
            {
                consist = new Consist(file, folder, allFolders, reverseConsist);
            }
            catch { }

            return consist;
        }

        static ISet<Locomotive> GetLocomotives(ITrainFile conFile, Folder folder, IList<Folder> allFolders)
        {
            var foldersDict = allFolders.ToDictionary((Folder f) => f.Name, (Folder f) => f.Path);
            var choices = conFile.GetLeadLocomotiveChoices(folder.Path, foldersDict);
            return choices
                .Where((PreferredLocomotive pl) => !pl.Equals(PreferredLocomotive.NoLocomotive))
                .Select((PreferredLocomotive pl) => LoadLocomotive(pl))
                .Where((Locomotive loco) => loco != null)
                .ToHashSet();
        }

        static ISet<Locomotive> GetLocomotivesReverse(ITrainFile conFile, Folder folder, IList<Folder> allFolders)
        {
            var foldersDict = allFolders.ToDictionary((Folder f) => f.Name, (Folder f) => f.Path);
            var choices = conFile.GetReverseLocomotiveChoices(folder.Path, foldersDict);
            return choices
                .Where((PreferredLocomotive pl) => !pl.Equals(PreferredLocomotive.NoLocomotive))
                .Select((PreferredLocomotive pl) => LoadLocomotive(pl))
                .Where((Locomotive loco) => loco != null)
                .ToHashSet();
        }

        private static Locomotive LoadLocomotive(PreferredLocomotive locoSpec)
        {
            Locomotive loaded;
            try
            {
                loaded = new Locomotive(locoSpec.FilePath);
            }
            catch
            {
                loaded = null;
            }
            return loaded;
        }

    }

    public class Locomotive : IComparable<Locomotive>
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

        public int CompareTo(Locomotive other) => string.Compare(Name, other.Name);
    }
}
