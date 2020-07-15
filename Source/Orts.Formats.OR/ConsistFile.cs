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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Formats.OR
{
    /* To make things easy, we use the Newtonsoft library to deserialize our classes,
     * with the help of some custom converters to handle the polymorphic cases. */

    /// <summary>
    /// A native Open Rails JSON consist.
    /// </summary>
    public class ConsistFile : IConsist
    {
        public string DisplayName { get; set; } = "Loose consist.";
        public float? MaxVelocityMpS { get; set; }
        public float Durability { get; set; } = 1f;
        public bool PlayerDrivable { get; set; } = true;

        /// <summary>
        /// Load a consist from a JSON file.
        /// </summary>
        /// <param name="filePath">The path to load the consist from.</param>
        /// <returns>The loaded consist.</returns>
        public static ConsistFile LoadFrom(string filePath)
        {
            JsonSerializer serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new ConsistFileConverter() }
            });
            using (var reader = new JsonTextReader(File.OpenText(filePath)))
                return (ConsistFile)serializer.Deserialize(reader, typeof(ConsistFile));
        }

        public ISet<PreferredLocomotive> GetLeadLocomotiveChoices(string basePath, IDictionary<string, string> folders) =>
            GetLeadLocomotiveChoices(new ConsistStore(), basePath, folders);

        internal virtual ISet<PreferredLocomotive> GetLeadLocomotiveChoices(ConsistStore store, string basePath, IDictionary<string, string> folders)
        {
            throw new InvalidOperationException();
        }

        public ISet<PreferredLocomotive> GetReverseLocomotiveChoices(string basePath, IDictionary<string, string> folders) =>
            GetReverseLocomotiveChoices(new ConsistStore(), basePath, folders);

        internal virtual ISet<PreferredLocomotive> GetReverseLocomotiveChoices(ConsistStore store, string basePath, IDictionary<string, string> folders)
        {
            throw new InvalidOperationException();
        }

        public IEnumerable<WagonReference> GetWagonList(string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null) =>
            GetWagonList(new ConsistStore(), basePath, folders, preference);

        internal virtual IEnumerable<WagonReference> GetWagonList(ConsistStore store, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            throw new InvalidOperationException();
        }

        public override string ToString() => DisplayName;
    }

    #region Item interfaces
    /// <summary>
    /// A wagon, engine, or consist reference in a consist.
    /// </summary>
    public interface IConsistItem
    {
        /// <summary>
        /// Number of times this item should be repeated at loading.
        /// </summary>
        int Count { get; set; }

        /// <summary>
        /// Reverse the orientation of the item at loading.
        /// </summary>
        bool Flip { get; set; }

        /// <summary>
        /// The installation profile (content directory) to search for this item; the current one if null.
        /// </summary>
        string Profile { get; set; }
    }

    public interface IConsistWagon : IConsistItem
    {
        /// <summary>
        /// Path to the wagon description file, relative to the TRAINSET folder and omitting the file extension.
        /// </summary>
        string Wagon { get; set; }
    }

    public interface IConsistEngine : IConsistItem
    {
        /// <summary>
        /// Path to the engine description file, relative to the TRAINSET folder and omitting the file extension.
        /// </summary>
        string Engine { get; set; }
    }

    public interface IConsistReference : IConsistItem
    {
        /// <summary>
        /// Path to the consist file, relative to the CONSISTS folder and omitting the file extension.
        /// </summary>
        string Consist { get; set; }
    }

    internal static class ConsistItemExtensions
    {
        /// <summary>
        /// Get the wagon reference(s) generated by a given consist item.
        /// </summary>
        /// <param name="item">The consist item. The subtype is automatically determined.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">A dictionary of other available content directories.</param>
        /// <param name="startUiD">The UiD of the generated wagon, or, if this item generates multiple wagons,
        /// the first UiD in the sequence. Subsequent UiD's will count up.</param>
        /// <returns></returns>
        public static IEnumerable<WagonReference> GetGenericWagonList(this IConsistItem item, ConsistStore store, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            if (item.Profile != null)
            {
                if (!folders.TryGetValue(item.Profile, out string newBasePath))
                {
                    UnknownProfile(item.Profile);
                    return new WagonReference[0] { };
                }
                basePath = newBasePath;
            }

            if (item is IConsistWagon wagon)
                return GetWagonWagonList(wagon, basePath, startUiD);
            else if (item is IConsistEngine engine)
                return GetEngineWagonList(engine, basePath, startUiD);
            else if (item is IConsistReference consist)
                return GetConsistWagonList(store, consist, basePath, folders, startUiD, preference);
            else
                throw new InvalidOperationException();
        }

        private static IEnumerable<WagonReference> GetWagonWagonList(IConsistWagon wagon, string basePath, int startUiD)
        {
            string filePath = wagon.GetPath(basePath);
            return Enumerable.Range(0, wagon.Count)
                .Select((int i) => new WagonReference(filePath, wagon.Flip, startUiD + i));
        }

        private static IEnumerable<WagonReference> GetEngineWagonList(IConsistEngine engine, string basePath, int startUiD)
        {
            string filePath = engine.GetPath(basePath);
            return Enumerable.Range(0, engine.Count)
                .Select((int i) => new WagonReference(filePath, engine.Flip, startUiD + i));
        }

        private static IEnumerable<WagonReference> GetConsistWagonList(ConsistStore store, IConsistReference consist, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            (IConsist subConsist, ConsistStore subStore) = store.CreateSubConsist(basePath, consist.Consist);
            IEnumerable<WagonReference> MakeList(int localStartUiD)
            {
                IEnumerable<WagonReference> localWagonRefs;
                if (subConsist is ConsistFile ortsConsist)
                    // ORTS consists can reference other consists, and hence need a ConsistStore to guard against recursion.
                    localWagonRefs = ortsConsist.GetWagonList(subStore, basePath, folders, preference);
                else
                    localWagonRefs = subConsist.GetWagonList(basePath, folders, preference);
                if (consist.Flip)
                {
                    return localWagonRefs
                        .Reverse()
                        .Select((WagonReference wagonRef, int i) => new WagonReference(wagonRef.FilePath, !wagonRef.Flipped, localStartUiD + i));
                }
                else
                {
                    return localWagonRefs
                        .Select((WagonReference wagonRef, int i) => new WagonReference(wagonRef.FilePath, wagonRef.Flipped, localStartUiD + i));
                }
            }

            IEnumerable<WagonReference> wagonRefs = new WagonReference[0] { };
            foreach (int _ in Enumerable.Range(0, consist.Count))
            {
                var thisList = new List<WagonReference>(MakeList(startUiD));
                startUiD += thisList.Count;
                wagonRefs = wagonRefs.Concat(thisList);
            }
            return wagonRefs;
        }

        public static ISet<PreferredLocomotive> GetGenericLeadLocomotiveChoices(this IConsistItem item, ConsistStore store, string basePath, IDictionary<string, string> folders)
        {
            if (item.Profile != null)
            {
                if (!folders.TryGetValue(item.Profile, out string newBasePath))
                {
                    UnknownProfile(item.Profile);
                    return new HashSet<PreferredLocomotive>();
                }
                basePath = newBasePath;
            }

            if (item is IConsistWagon)
            {
                return PreferredLocomotive.NoLocomotiveSet;
            }
            else if (item is IConsistEngine engine)
            {
                return new HashSet<PreferredLocomotive>() { new PreferredLocomotive(engine.GetPath(basePath)) };
            }
            else if (item is IConsistReference consist)
            {
                (IConsist subConsist, ConsistStore subStore) = store.CreateSubConsist(basePath, consist.Consist);
                if (subConsist is ConsistFile ortsConsist)
                    // ORTS consists can reference other consists, and hence need a ConsistStore to guard against recursion.
                    return ortsConsist.GetLeadLocomotiveChoices(subStore, basePath, folders);
                else
                    return subConsist.GetLeadLocomotiveChoices(basePath, folders);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static ISet<PreferredLocomotive> GetGenericReverseLocomotiveChoices(this IConsistItem item, ConsistStore store, string basePath, IDictionary<string, string> folders)
        {
            if (item.Profile != null)
            {
                if (!folders.TryGetValue(item.Profile, out string newBasePath))
                {
                    UnknownProfile(item.Profile);
                    return new HashSet<PreferredLocomotive>();
                }
                basePath = newBasePath;
            }

            if (item is IConsistWagon)
            {
                return PreferredLocomotive.NoLocomotiveSet;
            }
            else if (item is IConsistEngine engine)
            {
                return new HashSet<PreferredLocomotive>() { new PreferredLocomotive(engine.GetPath(basePath)) };
            }
            else if (item is IConsistReference consist)
            {
                (IConsist subConsist, ConsistStore subStore) = store.CreateSubConsist(basePath, consist.Consist);
                if (subConsist is ConsistFile ortsConsist)
                    // ORTS consists can reference other consists, and hence need a ConsistStore to guard against recursion.
                    return ortsConsist.GetReverseLocomotiveChoices(subStore, basePath, folders);
                else
                    return subConsist.GetReverseLocomotiveChoices(basePath, folders);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private static void UnknownProfile(string name) => Trace.WriteLine($"Unknown installation profile: {name}");

        private static string GetPath(this IConsistWagon wagon, string basePath) =>
            Path.ChangeExtension(Path.Combine(basePath, "trains", "trainset", wagon.Wagon), ".wag");

        private static string GetPath(this IConsistEngine engine, string basePath) =>
            Path.ChangeExtension(Path.Combine(basePath, "trains", "trainset", engine.Engine), ".eng");
    }
    #endregion

    #region List consist
    /// <summary>
    /// "List" consists assemble their items in linear order.
    /// </summary>
    public class ListConsistFile : ConsistFile
    {
        public IList<ListConsistItem> List { get; set; } = new List<ListConsistItem>();

        internal override IEnumerable<WagonReference> GetWagonList(ConsistStore store, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            int uiD = 0;
            foreach (IConsistItem item in List)
            {
                foreach (WagonReference wagonRef in item.GetGenericWagonList(store, basePath, folders, uiD, preference))
                {
                    uiD++;
                    yield return wagonRef;
                }
            }
        }

        internal override ISet<PreferredLocomotive> GetLeadLocomotiveChoices(ConsistStore store, string basePath, IDictionary<string, string> folders)
        {
            var engines = new HashSet<PreferredLocomotive>();
            foreach (IConsistItem item in List)
            {
                ISet<PreferredLocomotive> choices = item.GetGenericLeadLocomotiveChoices(store, basePath, folders);
                bool moreEngines = choices.Contains(PreferredLocomotive.NoLocomotive);

                choices.Remove(PreferredLocomotive.NoLocomotive);
                engines.UnionWith(choices);

                if (!moreEngines)
                    break;
            }
            return engines.Count > 0 ? engines : PreferredLocomotive.NoLocomotiveSet;
        }

        internal override ISet<PreferredLocomotive> GetReverseLocomotiveChoices(ConsistStore store, string basePath, IDictionary<string, string> folders)
        {
            var engines = new HashSet<PreferredLocomotive>();
            foreach (IConsistItem item in List.Reverse())
            {
                ISet<PreferredLocomotive> choices = item.GetGenericReverseLocomotiveChoices(store, basePath, folders);
                bool moreEngines = choices.Contains(PreferredLocomotive.NoLocomotive);

                choices.Remove(PreferredLocomotive.NoLocomotive);
                engines.UnionWith(choices);

                if (!moreEngines)
                    break;
            }
            return engines.Count > 0 ? engines : PreferredLocomotive.NoLocomotiveSet;
        }
    }

    [JsonConverter(typeof(ListConsistItemConverter))]
    public class ListConsistItem : IConsistItem
    {
        public int Count { get; set; } = 1;
        public bool Flip { get; set; } = false;
        public string Profile { get; set; } = null;
    }

    public class ListConsistWagon : ListConsistItem, IConsistWagon
    {
        public string Wagon { get; set; }
    }

    public class ListConsistEngine : ListConsistItem, IConsistEngine
    {
        public string Engine { get; set; }
    }

    public class ListConsistReference : ListConsistItem, IConsistReference
    {
        public string Consist { get; set; }
    }

    /// <summary>
    /// JSON deserializer for polymorphic <see cref="ListConsistItem"/>s.
    /// </summary>
    public class ListConsistItemConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(ListConsistItem);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            bool isWagon = jsonObject.ContainsKey("Wagon");
            bool isEngine = jsonObject.ContainsKey("Engine");
            bool isConsist = jsonObject.ContainsKey("Consist");
            ListConsistItem item;
            if (!isWagon && !isEngine && !isConsist)
                throw new JsonSerializationException("Unrecognized consist item");
            else if (isWagon && !isEngine && !isConsist)
                item = new ListConsistWagon();
            else if (!isWagon && isEngine && !isConsist)
                item = new ListConsistEngine();
            else if (!isWagon && !isEngine && isConsist)
                item = new ListConsistReference();
            else
                throw new JsonSerializationException("Ambiguous consist item");
            serializer.Populate(jsonObject.CreateReader(), item);
            return item;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Random consist
    /// <summary>
    /// "Random" consists select probabilistically from a set of alternative items.
    /// </summary>
    public class RandomConsistFile : ConsistFile
    {
        public IList<RandomConsistItem> Random { get; set; } = new List<RandomConsistItem>();

        private static readonly Random RnJesus = new Random();

        internal override IEnumerable<WagonReference> GetWagonList(ConsistStore store, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            var table = new List<(float, float, RandomConsistItem)>();
            float p = 0f;
            foreach (RandomConsistItem item in Random)
            {
                float nextP;
                checked
                {
                    nextP = p + Math.Max(item.Probability, 0f);
                }
                table.Add((p, nextP, item));
                p = nextP;
            }
            if (p == 0f)
                return new WagonReference[0] { };

            // TODO: Implement preferredLocomotivePath.
            double random = RnJesus.NextDouble() * p;
            RandomConsistItem selected = table
                .Where(((float, float, RandomConsistItem) tuple) => tuple.Item1 <= random && random < tuple.Item2)
                .Select(((float, float, RandomConsistItem) tuple) => tuple.Item3)
                .First();
            return selected.GetGenericWagonList(store, basePath, folders, startUiD: 0, preference);
        }

        internal override ISet<PreferredLocomotive> GetLeadLocomotiveChoices(ConsistStore store, string basePath, IDictionary<string, string> folders) => Random
            .Where((RandomConsistItem item) => item.Probability > 0f)
            .Select((RandomConsistItem item) => item.GetGenericLeadLocomotiveChoices(store, basePath, folders))
            .Aggregate(new HashSet<PreferredLocomotive>(), (ISet<PreferredLocomotive> accum, IEnumerable<PreferredLocomotive> choices) =>
            {
                accum.UnionWith(choices);
                return accum;
            });

        internal override ISet<PreferredLocomotive> GetReverseLocomotiveChoices(ConsistStore store, string basePath, IDictionary<string, string> folders) => Random
            .Where((RandomConsistItem item) => item.Probability > 0f)
            .Select((RandomConsistItem item) => item.GetGenericReverseLocomotiveChoices(store, basePath, folders))
            .Aggregate(new HashSet<PreferredLocomotive>(), (ISet<PreferredLocomotive> accum, IEnumerable<PreferredLocomotive> choices) =>
            {
                accum.UnionWith(choices);
                return accum;
            });
    }

    [JsonConverter(typeof(RandomConsistItemConverter))]
    public class RandomConsistItem : IConsistItem
    {
        public int Count { get; set; } = 1;
        public bool Flip { get; set; } = false;
        public string Profile { get; set; } = null;
        public float Probability { get; set; }
    }

    public class RandomConsistWagon : RandomConsistItem, IConsistWagon
    {
        public string Wagon { get; set; }
    }

    public class RandomConsistEngine : RandomConsistItem, IConsistEngine
    {
        public string Engine { get; set; }
    }

    public class RandomConsistReference : RandomConsistItem, IConsistReference
    {
        public string Consist { get; set; }
    }

    /// <summary>
    /// JSON deserializer for polymorphic <see cref="RandomConsistItem"/>s.
    /// </summary>
    public class RandomConsistItemConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(RandomConsistItem);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            bool isWagon = jsonObject.ContainsKey("Wagon");
            bool isEngine = jsonObject.ContainsKey("Engine");
            bool isConsist = jsonObject.ContainsKey("Consist");
            RandomConsistItem item;
            if (!isWagon && !isEngine && !isConsist)
                throw new JsonSerializationException("Unrecognized consist item");
            else if (isWagon && !isEngine && !isConsist)
                item = new RandomConsistWagon();
            else if (!isWagon && isEngine && !isConsist)
                item = new RandomConsistEngine();
            else if (!isWagon && !isEngine && isConsist)
                item = new RandomConsistReference();
            else
                throw new JsonSerializationException("Ambiguous consist item");
            serializer.Populate(jsonObject.CreateReader(), item);
            return item;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    /// <summary>
    /// JSON deserializer for polymorphic <see cref="ConsistFile"/>s.
    /// </summary>
    public class ConsistFileConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(ConsistFile);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            bool isList = jsonObject.ContainsKey("List");
            bool isRandom = jsonObject.ContainsKey("Random");
            ConsistFile consist;
            if (!isList && !isRandom)
                throw new JsonSerializationException("Unrecognized consist type");
            else if (isList && !isRandom)
                consist = new ListConsistFile();
            else if (!isList && isRandom)
                consist = new RandomConsistFile();
            else
                throw new JsonSerializationException("Ambiguous consist type");
            serializer.Populate(jsonObject.CreateReader(), consist);
            return consist;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Tracks the current stack of visited consists, so as to prevent recursion, and caches consist file loads.
    /// </summary>
    /// <remarks>
    /// NOT thread-safe.
    /// </remarks>
    internal class ConsistStore
    {
        /// <summary>
        /// Multiple stores share the same cache.
        /// </summary>
        private readonly Dictionary<string, IConsist> Consists;

        /// <summary>
        /// Each store has its own running stack of visited consists.
        /// </summary>
        private readonly HashSet<string> Visited;

        /// <summary>
        /// Creates a top-level consist store.
        /// </summary>
        public ConsistStore()
        {
            Consists = new Dictionary<string, IConsist>();
            Visited = new HashSet<string>();
        }

        private ConsistStore(Dictionary<string, IConsist> consists, HashSet<string> visited)
        {
            Consists = consists;
            Visited = visited;
        }

        /// <summary>
        /// Add a new layer of recursion by loading a sub-consist.
        /// </summary>
        /// <param name="baseName">The consist file to load.</param>
        /// <returns>The loaded consist and a new <see cref="ConsistStore"/> handle.</returns>
        public (IConsist, ConsistStore) CreateSubConsist(string basePath, string filename)
        {
            string filePath = ConsistUtilities.ResolveConsist(basePath, filename);
            if (Visited.Contains(filePath))
                throw new RecursiveConsistException($"Consist loads itself: {filePath}");

            IConsist consist;
            if (Consists.TryGetValue(filePath, out IConsist cached))
            {
                consist = cached;
            }
            else
            {
                switch (Path.GetExtension(filePath).ToLowerInvariant())
                {
                    case ".consist-or":
                        consist = ConsistFile.LoadFrom(filePath);
                        break;
                    case ".con":
                        consist = new Msts.ConsistFile(filePath);
                        break;
                    default:
                        throw new InvalidDataException("Unknown consist format");
                }
                Consists[filePath] = consist;
            }
            return (consist, new ConsistStore(Consists, new HashSet<string>(Visited) { filePath }));
        }
    }

    /// <summary>
    /// A consist refers (directly or indirectly) to itself, creating an infinite-length train.
    /// </summary>
    public class RecursiveConsistException : Exception
    {
        public RecursiveConsistException(string message) : base(message) { }
    }
}
