// COPYRIGHT 2020 by the Open Rails project.
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

namespace ORTS.Common
{
    /// <summary>
    /// An engine or wagon reference that has been prepared for loading by the simulator.
    /// </summary>
    public class WagonSpecification
    {
        public string FilePath { get; }
        public bool Flipped { get; }
        public int UiD { get; }

        public WagonSpecification(string filePath, bool flipped, int uid)
        {
            FilePath = filePath;
            Flipped = flipped;
            UiD = uid;
        }
    }

    public interface IConsist
    {
        string Name { get; }
        float? MaxVelocityMpS { get; }
        float Durability { get; }
        bool PlayerDrivable { get; }
        IEnumerable<WagonSpecification> GetWagonList(string basePath, IDictionary<string, string> folders, string preferredLocomotivePath = null);
        ICollection<string> GetLeadLocomotiveChoices(string basePath, IDictionary<string, string> folders);
        ICollection<string> GetReverseLocomotiveChoices(string basePath, IDictionary<string, string> folders);
    }

    public static class ConsistUtilities
    {
        /// <summary>
        /// Enumerate all consist files in a directory. Native (.consist-or) files will shadow legacy (.con) ones.
        /// </summary>
        /// <param name="consistsDirectory">The directory to search.</param>
        /// <returns>All files with known consist file extensions.</returns>
        public static IEnumerable<string> AllConsistFiles(string consistsDirectory)
        {
            ICollection<string> BaseNames(string pattern) => new HashSet<string>(
                Directory.GetFileSystemEntries(consistsDirectory, pattern)
                    .Select((string path) => Path.GetFileNameWithoutExtension(path)),
                StringComparer.InvariantCultureIgnoreCase);

            var ortsBaseNames = BaseNames("*.consist-or");
            var mstsBaseNames = BaseNames("*.con");

            IEnumerable<string> CombinedIterator()
            {
                foreach (string baseName in ortsBaseNames.Union(mstsBaseNames))
                {
                    // Prioritize native .consist-or files.
                    string extension = ortsBaseNames.Contains(baseName) ? ".consist-or" : ".con";
                    yield return Path.GetFullPath(Path.ChangeExtension(Path.Combine(consistsDirectory, baseName), extension));
                }
            }

            string[] consists = CombinedIterator().ToArray();
            Array.Sort(consists);
            return consists;
        }
    }
}
