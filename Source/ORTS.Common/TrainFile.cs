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
    /// An engine or wagon reference for loading by the simulator.
    /// </summary>
    public class WagonReference
    {
        public string FilePath { get; }
        public bool Flipped { get; }
        public int UiD { get; }

        public WagonReference(string filePath, bool flipped, int uid)
        {
            FilePath = filePath;
            Flipped = flipped;
            UiD = uid;
        }
    }

    /// <summary>
    /// A generic formation of wagons and engines. Its composition may be nondeterministc.
    /// </summary>
    public interface ITrainFile
    {
        string DisplayName { get; }
        float? MaxVelocityMpS { get; }
        float Durability { get; }
        bool PlayerDrivable { get; }

        /// <summary>
        /// Obtain a list of <see cref="WagonReference"/>s to be loaded by the simulator.
        /// </summary>
        /// <remarks>
        /// If a preferred locomotive is specified but the constraint cannot be satisifed, this method should return an empty iterator.
        /// </remarks>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">A dictionary of other available content directories.</param>
        /// <param name="preference">Request a formation with a particular lead locomotive, identified by a filesystem path.</param>
        /// <returns>The wagons.</returns>
        IEnumerable<WagonReference> GetForwardWagonList(string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null);

        /// <summary>
        /// Obtain a list of <see cref="WagonReference"/>s to be loaded by the simulator in reverse order.
        /// </summary>
        /// <remarks>
        /// If a preferred locomotive is specified but the constraint cannot be satisifed, this method should return an empty iterator.
        /// </remarks>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">A dictionary of other available content directories.</param>
        /// <param name="preference">Request a formation with a particular lead locomotive, identified by a filesystem path.</param>
        /// <returns>The wagons, flipped and in reverse order.</returns>
        IEnumerable<WagonReference> GetReverseWagonList(string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null);

        /// <summary>
        /// Get the head-end locomotives that this train can spawn with.
        /// </summary>
        /// <remarks>
        /// trains without locomotives should return <see cref="PreferredLocomotive.NoLocomotiveSet"/>.
        /// </remarks>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">A dictionary of other available content directories.</param>
        /// <returns>The locomotives, identified by their filesystem paths.</returns>
        ISet<PreferredLocomotive> GetLeadLocomotiveChoices(string basePath, IDictionary<string, string> folders);

        /// <summary>
        /// Get the head-end locomotives that this train can spawn with if reversed.
        /// </summary>
        /// <remarks>
        /// trains without locomotives should return <see cref="PreferredLocomotive.NoLocomotiveSet"/>.
        /// </remarks>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">A dictionary of other available content directories.</param>
        /// <returns>The locomotives, identified by their filesystem paths.</returns>
        ISet<PreferredLocomotive> GetReverseLocomotiveChoices(string basePath, IDictionary<string, string> folders);
    }

    /// <summary>
    /// Identifies a preferred locomotive for the creation of a player train.
    /// </summary>
    public sealed class PreferredLocomotive : IEquatable<PreferredLocomotive>
    {
        public string FilePath { get; }

        /// <summary>
        /// Special value to explicitly request a train with no locomotive.
        /// </summary>
        public static readonly PreferredLocomotive NoLocomotive = new PreferredLocomotive();

        /// <summary>
        /// Singleton set for trains without a locomotive.
        /// </summary>
        public static readonly ISet<PreferredLocomotive> NoLocomotiveSet = new HashSet<PreferredLocomotive>() { NoLocomotive };

        public PreferredLocomotive(string filePath)
        {
            FilePath = Path.GetFullPath(filePath);
        }

        private PreferredLocomotive()
        {
            FilePath = "";
        }

        public override bool Equals(object other) => other is PreferredLocomotive cast && Equals(cast);

        public bool Equals(PreferredLocomotive other) => other != null && FilePath == other.FilePath;

        public override int GetHashCode() => FilePath.GetHashCode();
    }

    public static class TrainFileUtilities
    {
        /// <summary>
        /// Locate a train or consist by filename. Prioritize the native (.train-or) train format if available.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="filename">The filename of the train.</param>
        /// <returns>The train path with the preferred extension, or null if no matching file is found.</returns>
        public static string ResolveTrainFile(string basePath, string filename)
        {
            string ortsList = ResolveOrtsTrainFile(basePath, filename);
            string mstsConsist = ResolveMstsConsist(basePath, filename);
            if (File.Exists(ortsList))
                return ortsList;
            else if (File.Exists(mstsConsist))
                return mstsConsist;
            else
                return null;
        }

        /// <summary>
        /// Locate a train by filename.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="filename">The filename, minus its extension, of the file to locate.</param>
        /// <returns>The full path to the train file.</returns>
        public static string ResolveOrtsTrainFile(string basePath, string filename) =>
            Path.GetFullPath(Path.ChangeExtension(Path.Combine(basePath, "trains", "lists", filename), ".train-or"));

        /// <summary>
        /// Locate a consist by filename.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="filename">The filename, minus its extension, of the file to locate.</param>
        /// <returns>The full path to the consist file.</returns>
        public static string ResolveMstsConsist(string basePath, string filename) =>
            Path.GetFullPath(Path.ChangeExtension(Path.Combine(basePath, "trains", "consists", filename), ".con"));

        /// <summary>
        /// Locate a wagon.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="subFolders">The wagon's subfolder(s) relative to the TRAINSET folder.</param>
        /// <param name="filename">The wagon's filename, minus its extension.</param>
        /// <returns>The full path to the wagon file.</returns>
        public static string ResolveWagonFile(string basePath, string[] subFolders, string filename)
        {
            var path = new List<string>()
            {
                basePath,
                "trains",
                "trainset"
            };
            path.AddRange(subFolders);
            path.Add(filename);
            return Path.GetFullPath(Path.ChangeExtension(Path.Combine(path.ToArray()), ".wag"));
        }

        /// <summary>
        /// Locate an engine.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="subFolders">The engine's subfolder(s) relative to the TRAINSET folder.</param>
        /// <param name="filename">The engine's filename, minus its extension.</param>
        /// <returns>The full path to the engine file.</returns>
        public static string ResolveEngineFile(string basePath, string[] subFolders, string filename)
        {
            var path = new List<string>()
            {
                basePath,
                "trains",
                "trainset"
            };
            path.AddRange(subFolders);
            path.Add(filename);
            return Path.GetFullPath(Path.ChangeExtension(Path.Combine(path.ToArray()), ".eng"));
        }

        /// <summary>
        /// Enumerate all train files in a content directory. Native (.train-or) files will shadow legacy (.con) ones.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <returns>All files with known train or consist file extensions.</returns>
        public static IEnumerable<string> AllTrainFiles(string basePath)
        {
            ISet<string> BaseNames(string directory, string pattern) => new HashSet<string>(
                Directory.GetFileSystemEntries(directory, pattern)
                    .Select((string path) => Path.GetFileNameWithoutExtension(path)),
                StringComparer.InvariantCultureIgnoreCase);

            ISet<string> ortsBaseNames = BaseNames(Path.Combine(basePath, "trains", "lists"), "*.train-or");
            ISet<string> mstsBaseNames = BaseNames(Path.Combine(basePath, "trains", "consists"), "*.con");

            IEnumerable<string> CombinedIterator()
            {
                foreach (string baseName in ortsBaseNames.Union(mstsBaseNames))
                {
                    string path;
                    // Prioritize native .train-or files.
                    if (ortsBaseNames.Contains(baseName))
                        path = Path.ChangeExtension(Path.Combine(basePath, "trains", "lists", baseName), ".train-or");
                    else
                        path = Path.ChangeExtension(Path.Combine(basePath, "trains", "consists", baseName), ".con");
                    yield return Path.GetFullPath(path);
                }
            }

            string[] trains = CombinedIterator().ToArray();
            Array.Sort(trains, new BaseNameComparer());
            return trains;
        }

        private class BaseNameComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                string BaseName(string path) => Path.GetFileNameWithoutExtension(path);
                return string.Compare(BaseName(x), BaseName(y));
            }
        }
    }
}
