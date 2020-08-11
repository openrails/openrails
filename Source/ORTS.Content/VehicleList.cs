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

namespace ORTS.Content
{
    /// <summary>
    /// An engine or wagon reference for loading by the simulator.
    /// </summary>
    public class WagonReference : IEquatable<WagonReference>
    {
        public string FilePath { get; }
        public bool Flipped { get; }
        public int UiD { get; }

        public WagonReference(string filePath, bool flipped, int uid)
        {
            FilePath = Path.GetFullPath(filePath);
            Flipped = flipped;
            UiD = uid;
        }

        public override bool Equals(object other) => other is WagonReference cast && Equals(cast);

        public bool Equals(WagonReference other) => other != null
            && FilePath.Equals(other.FilePath, StringComparison.InvariantCultureIgnoreCase)
            && Flipped == other.Flipped
            && UiD == other.UiD;

        public override int GetHashCode() => Tuple.Create(FilePath.ToLowerInvariant(), Flipped, UiD).GetHashCode();
    }

    /// <summary>
    /// A generic formation of wagons and engines. Its composition may be nondeterministc.
    /// </summary>
    public interface IVehicleList
    {
        string DisplayName { get; }
        float? MaxVelocityMpS { get; }
        float Durability { get; }
        bool PlayerDrivable { get; }
        bool IsTilting { get; }

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
        public static ISet<PreferredLocomotive> NoLocomotiveSet => new HashSet<PreferredLocomotive>() { NoLocomotive };

        public PreferredLocomotive(string filePath)
        {
            FilePath = Path.GetFullPath(filePath);
        }

        private PreferredLocomotive()
        {
            FilePath = "";
        }

        public override bool Equals(object other) => other is PreferredLocomotive cast && Equals(cast);

        public bool Equals(PreferredLocomotive other) => other != null
            && FilePath.Equals(other.FilePath, StringComparison.InvariantCultureIgnoreCase);

        public override int GetHashCode() => FilePath.ToLowerInvariant().GetHashCode();
    }

    public static class VehicleListUtilities
    {
        /// <summary>
        /// Locate a vehicle list by filename. Prioritize the native (.train-or) train format if available.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="filename">The filename of the vehicle list.</param>
        /// <returns>The vehicle list path with the preferred extension, or null if no matching file is found.</returns>
        public static string ResolveVehicleList(string basePath, string filename)
        {
            string ortsTrain = ResolveOrtsTrainFile(basePath, filename);
            string mstsConsist = ResolveMstsConsist(basePath, filename);
            if (File.Exists(ortsTrain))
                return ortsTrain;
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
            Path.GetFullPath(AddExtensionIfNotPresent(Path.Combine(basePath, "trains", "consists", filename), ".train-or"));

        /// <summary>
        /// Locate a consist by filename.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="filename">The filename, minus its extension, of the file to locate.</param>
        /// <returns>The full path to the consist file.</returns>
        public static string ResolveMstsConsist(string basePath, string filename) =>
            Path.GetFullPath(AddExtensionIfNotPresent(Path.Combine(basePath, "trains", "consists", filename), ".con"));

        /// <summary>
        /// Locate a wagon.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="subFolders">The wagon's subfolder(s) relative to the TRAINSET folder.</param>
        /// <param name="filename">The wagon's filename, minus its extension.</param>
        /// <returns>The full path to the wagon file.</returns>
        public static string ResolveWagonFile(string basePath, IEnumerable<string> subFolders, string filename)
        {
            var path = new List<string>()
            {
                basePath,
                "trains",
                "trainset"
            };
            path.AddRange(subFolders);
            path.Add(filename);
            return Path.GetFullPath(AddExtensionIfNotPresent(Path.Combine(path.ToArray()), ".wag"));
        }

        /// <summary>
        /// Locate an engine.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="subFolders">The engine's subfolder(s) relative to the TRAINSET folder.</param>
        /// <param name="filename">The engine's filename, minus its extension.</param>
        /// <returns>The full path to the engine file.</returns>
        public static string ResolveEngineFile(string basePath, IEnumerable<string> subFolders, string filename)
        {
            var path = new List<string>()
            {
                basePath,
                "trains",
                "trainset"
            };
            path.AddRange(subFolders);
            path.Add(filename);
            return Path.GetFullPath(AddExtensionIfNotPresent(Path.Combine(path.ToArray()), ".eng"));
        }

        /// <summary>
        /// Enumerate all vehicle lists in a content directory. Native (.train-or) files will shadow legacy (.con) ones.
        /// </summary>
        /// <param name="basePath">The current content directory.</param>
        /// <returns>All files with known train or consist file extensions.</returns>
        public static IEnumerable<string> AllVehicleLists(string basePath)
        {
            ISet<string> BaseNames(string directory, string pattern)
            {
                if (!Directory.Exists(directory))
                    return new HashSet<string>();
                return new HashSet<string>(
                    Directory.GetFileSystemEntries(directory, pattern)
                        .Select((string path) => Path.GetFileNameWithoutExtension(path)),
                    StringComparer.InvariantCultureIgnoreCase);
            }

            var ortsBaseNames = BaseNames(Path.Combine(basePath, "trains", "consists"), "*.train-or");
            var mstsBaseNames = BaseNames(Path.Combine(basePath, "trains", "consists"), "*.con");

            IEnumerable<string> CombinedIterator()
            {
                foreach (string baseName in ortsBaseNames.Union(mstsBaseNames))
                {
                    string path;
                    // Prioritize native .train-or files.
                    if (ortsBaseNames.Contains(baseName))
                        path = AddExtensionIfNotPresent(Path.Combine(basePath, "trains", "consists", baseName), ".train-or");
                    else
                        path = AddExtensionIfNotPresent(Path.Combine(basePath, "trains", "consists", baseName), ".con");
                    yield return Path.GetFullPath(path);
                }
            }

            var trains = CombinedIterator().ToArray();
            Array.Sort(trains, BaseNameComprarer);
            return trains;
        }

        /// <summary>
        /// Check that a file path has a requested file extension and, if not, add it.
        /// </summary>
        /// <param name="path">The file path to check.</param>
        /// <param name="extension">The extension, which must include the leading period; e.g. ".con".</param>
        /// <returns>The file path with the requested extension.</returns>
        private static string AddExtensionIfNotPresent(string path, string extension) =>
            Path.GetExtension(path) == extension ? path : path + extension;

        private static readonly BaseNameComparerClass BaseNameComprarer = new BaseNameComparerClass();

        private class BaseNameComparerClass : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                string BaseName(string path) => Path.GetFileNameWithoutExtension(path);
                return string.Compare(BaseName(x), BaseName(y));
            }
        }
    }
}
