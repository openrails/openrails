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
using ORTS.Common;
using ORTS.Settings;

namespace ORTS.Menu
{
    public class Consist
    {
        public readonly string Name;
        public readonly Locomotive Locomotive = new Locomotive("unknown");
        public readonly string FilePath;

        GettextResourceManager catalog = new GettextResourceManager("ORTS.Menu");

        internal Consist(string filePath, Folder folder) : this(filePath, folder, false) { }

        internal Consist(string filePath, Folder folder, bool reverseConsist)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    IConsist conFile = LoadConsist(filePath);
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

        private static IConsist LoadConsist(string filePath)
        {
            switch (System.IO.Path.GetExtension(filePath).ToLowerInvariant())
            {
                case ".consist-or":
                    return Orts.Formats.OR.ConsistFile.LoadFrom(filePath);
                case ".con":
                    return new Orts.Formats.Msts.ConsistFile(filePath);
                default:
                    throw new InvalidDataException("Unknown consist format");
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public static List<Consist> GetConsists(Folder folder)
        {
            var consists = new List<Consist>();
            string directory = System.IO.Path.Combine(folder.Path, "trains", "consists");
            if (Directory.Exists(directory))
            {
                foreach (string consist in ConsistUtilities.AllConsistFiles(directory))
                {
                    Consist loaded;
                    try
                    {
                        loaded = new Consist(consist, folder);
                    }
                    catch
                    {
                        continue;
                    }
                    consists.Add(loaded);
                }
            }
            return consists;
        }

        public static Consist GetConsist(Folder folder, string name) => GetConsist(folder, name, false);

        public static Consist GetConsist(Folder folder, string name, bool reverseConsist)
        {
            Consist consist = null;
            string file = ConsistUtilities.ResolveConsist(folder.Path, name);

            try
            {
                consist = new Consist(file, folder, reverseConsist);
            }
            catch { }

            return consist;
        }

        static Locomotive GetLocomotive(IConsist conFile, Folder folder)
        {
            ICollection<string> choices = conFile.GetLeadLocomotiveChoices(folder.Path, new Dictionary<string, string>());
            string one = choices.FirstOrDefault();
            if (one == null)
                return null;
            try
            {
                return new Locomotive(one);
            }
            catch
            {
                return null;
            }
        }

        static Locomotive GetLocomotiveReverse(IConsist conFile, Folder folder)
        {
            ICollection<string> choices = conFile.GetReverseLocomotiveChoices(folder.Path, new Dictionary<string, string>());
            string one = choices.FirstOrDefault();
            if (one == null)
                return null;
            try
            {
                return new Locomotive(one);
            }
            catch
            {
                return null;
            }
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
                var showInList = true;
                try
                {
                    var engFile = new EngineFile(filePath);
                    showInList = !string.IsNullOrEmpty(engFile.CabViewFile);
                    Name = engFile.Name.Trim();
                    Description = engFile.Description.Trim();
                }
                catch
                {
                    Name = "<" + catalog.GetString("load error:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                }
                if (!showInList) throw new InvalidDataException(catalog.GetStringFmt("Locomotive '{0}' is excluded.", filePath));
                if (string.IsNullOrEmpty(Name)) Name = "<" + catalog.GetString("unnamed:") + " " + System.IO.Path.GetFileNameWithoutExtension(filePath) + ">";
                if (string.IsNullOrEmpty(Description)) Description = null;
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
