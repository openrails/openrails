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
            GetLeadLocomotiveChoices(new LocomotiveChoiceMemoizer(new ConsistStore(), basePath, folders));

        internal virtual ISet<PreferredLocomotive> GetLeadLocomotiveChoices(LocomotiveChoiceMemoizer memoizer)
        {
            throw new InvalidOperationException();
        }

        public ISet<PreferredLocomotive> GetReverseLocomotiveChoices(string basePath, IDictionary<string, string> folders) =>
            GetReverseLocomotiveChoices(new LocomotiveChoiceMemoizer(new ConsistStore(), basePath, folders));

        internal virtual ISet<PreferredLocomotive> GetReverseLocomotiveChoices(LocomotiveChoiceMemoizer memoizer)
        {
            throw new InvalidOperationException();
        }

        public IEnumerable<WagonReference> GetForwardWagonList(string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            var store = new ConsistStore();
            return GetForwardWagonList(store, new LocomotiveChoiceMemoizer(store, basePath, folders), basePath, folders, preference);
        }

        internal virtual IEnumerable<WagonReference> GetForwardWagonList(ConsistStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            throw new InvalidOperationException();
        }

        public IEnumerable<WagonReference> GetReverseWagonList(string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            var store = new ConsistStore();
            return GetReverseWagonList(store, new LocomotiveChoiceMemoizer(store, basePath, folders), basePath, folders, preference);
        }

        internal virtual IEnumerable<WagonReference> GetReverseWagonList(ConsistStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
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
        /// <param name="store">The consist file cache.</param>
        /// <param name="memoizer">The locomotive choice memoizer.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">A dictionary of other available content directories.</param>
        /// <param name="startUiD">The UiD of the generated wagon, or, if this item generates multiple wagons,
        /// the first UiD in the sequence. Subsequent UiD's will count up.</param>
        /// <param name="preference">The preferred player locomotive.</param>
        /// <returns>The wagon list.</returns>
        public static IEnumerable<WagonReference> GetForwardWagonList(this IConsistItem item, ConsistStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            basePath = item.ResolveBasePath(basePath, folders);
            if (item is IConsistWagon wagon)
                return GetWagonWagonList(wagon, basePath, folders, startUiD, flip: false);
            else if (item is IConsistEngine engine)
                return GetEngineWagonList(engine, basePath, folders, startUiD, flip: false);
            else if (item is IConsistReference consist)
                return GetForwardConsistWagonList(store, consist, memoizer, basePath, folders, startUiD, preference);
            else
                throw new InvalidOperationException();
        }

        /// <summary>
        /// Get the wagon reference(s) generated by a given consist item, flipped and in reverse order.
        /// </summary>
        /// <param name="item">The consist item. The subtype is automatically determined.</param>
        /// <param name="store">The consist file cache.</param>
        /// <param name="memoizer">The locomotive choice memoizer.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">A dictionary of other available content directories.</param>
        /// <param name="startUiD">The UiD of the generated wagon, or, if this item generates multiple wagons,
        /// the first UiD in the sequence. Subsequent UiD's will count up.</param>
        /// <param name="preference">The preferred player locomotive.</param>
        /// <returns>The wagon list.</returns>
        public static IEnumerable<WagonReference> GetReverseWagonList(this IConsistItem item, ConsistStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            basePath = item.ResolveBasePath(basePath, folders);
            if (item is IConsistWagon wagon)
                return GetWagonWagonList(wagon, basePath, folders, startUiD, flip: true);
            else if (item is IConsistEngine engine)
                return GetEngineWagonList(engine, basePath, folders, startUiD, flip: true);
            else if (item is IConsistReference consist)
                return GetReverseConsistWagonList(store, consist, memoizer, basePath, folders, startUiD, preference);
            else
                throw new InvalidOperationException();
        }

        private static IEnumerable<WagonReference> GetWagonWagonList(IConsistWagon wagon, string basePath, IDictionary<string, string> folders, int startUiD, bool flip)
        {
            string filePath = wagon.GetPath(basePath, folders);
            return Enumerable.Range(0, wagon.Count)
                .Select((int i) => new WagonReference(filePath, flip ? !wagon.Flip : wagon.Flip, startUiD + i));
        }

        private static IEnumerable<WagonReference> GetEngineWagonList(IConsistEngine engine, string basePath, IDictionary<string, string> folders, int startUiD, bool flip)
        {
            string filePath = engine.GetPath(basePath, folders);
            return Enumerable.Range(0, engine.Count)
                .Select((int i) => new WagonReference(filePath, flip ? !engine.Flip : engine.Flip, startUiD + i));
        }

        private static IEnumerable<WagonReference> GetForwardConsistWagonList(ConsistStore store, IConsistReference consist, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            (IConsist subConsist, ConsistStore subStore) = store.CreateSubConsist(basePath, consist.Consist);
            IEnumerable<WagonReference> MakeList(int localStartUiD)
            {
                IEnumerable<WagonReference> localWagonRefs;
                if (subConsist is ConsistFile ortsConsist)
                    // ORTS consists can reference other consists, and hence need a ConsistStore to guard against recursion.
                    localWagonRefs = ortsConsist.GetForwardWagonList(subStore, memoizer, basePath, folders, preference);
                else
                    localWagonRefs = subConsist.GetForwardWagonList(basePath, folders, preference);
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

        private static IEnumerable<WagonReference> GetReverseConsistWagonList(ConsistStore store, IConsistReference consist, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            (IConsist subConsist, ConsistStore subStore) = store.CreateSubConsist(basePath, consist.Consist);
            IEnumerable<WagonReference> MakeList(int localStartUiD)
            {
                IEnumerable<WagonReference> localWagonRefs;
                if (subConsist is ConsistFile ortsConsist)
                    // ORTS consists can reference other consists, and hence need a ConsistStore to guard against recursion.
                    localWagonRefs = ortsConsist.GetReverseWagonList(subStore, memoizer, basePath, folders, preference);
                else
                    localWagonRefs = subConsist.GetReverseWagonList(basePath, folders, preference);
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

        /// <summary>
        /// Get the wagon specification file path for an <see cref="IConsistWagon"/>.
        /// </summary>
        /// <param name="wagon">The wagon.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">All other content directories.</param>
        /// <returns>The path to the .wag file.</returns>
        public static string GetPath(this IConsistWagon wagon, string basePath, IDictionary<string, string> folders) =>
            Path.ChangeExtension(Path.Combine(wagon.ResolveBasePath(basePath, folders), "trains", "trainset", wagon.Wagon), ".wag");

        /// <summary>
        /// Get the engine specification file for an <see cref="IConsistEngine"/>.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">All other content directories.</param>
        /// <returns>The path to the .eng file</returns>
        public static string GetPath(this IConsistEngine engine, string basePath, IDictionary<string, string> folders) =>
            Path.ChangeExtension(Path.Combine(engine.ResolveBasePath(basePath, folders), "trains", "trainset", engine.Engine), ".eng");

        /// <summary>
        /// Get the content directory for an <see cref="IConsistItem"/> that may or may not reside in the current content directory.
        /// </summary>
        /// <param name="item">The item to query.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">All other content directories.</param>
        /// <returns>The resolved content directory, which may or may not be equivalent to basePath.</returns>
        public static string ResolveBasePath(this IConsistItem item, string basePath, IDictionary<string, string> folders)
        {
            if (item.Profile != null)
            {
                if (!folders.TryGetValue(item.Profile, out string newBasePath))
                    throw new DirectoryNotFoundException($"Unknown installation profile: {item.Profile}");
                return newBasePath;
            }
            return basePath;
        }
    }
    #endregion

    #region List consist
    /// <summary>
    /// "List" consists assemble their items in linear order.
    /// </summary>
    public class ListConsistFile : ConsistFile
    {
        public IList<ListConsistItem> List { get; set; } = new List<ListConsistItem>();

        internal override IEnumerable<WagonReference> GetForwardWagonList(ConsistStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            Dictionary<ListConsistItem, ISet<PreferredLocomotive>> locoSets = List
                .ToDictionary((ListConsistItem item) => item, (ListConsistItem item) => memoizer.GetLeadLocomotiveChoices(item));
            bool satisfiable = preference == null || locoSets
                .Values
                .Any((ISet<PreferredLocomotive> choices) => choices.Contains(preference));
            if (!satisfiable)
                yield break;

            int uiD = 0;
            foreach (ListConsistItem item in List)
            {
                PreferredLocomotive target;
                if (preference == null)
                    target = null;
                else
                    target = locoSets[item].Contains(preference) ? preference : PreferredLocomotive.NoLocomotive;
                foreach (WagonReference wagonRef in item.GetForwardWagonList(store, memoizer, basePath, folders, uiD, target))
                {
                    uiD++;
                    yield return wagonRef;
                }
            }
        }

        internal override IEnumerable<WagonReference> GetReverseWagonList(ConsistStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            Dictionary<ListConsistItem, ISet<PreferredLocomotive>> locoSets = List
                .ToDictionary((ListConsistItem item) => item, (ListConsistItem item) => memoizer.GetReverseLocomotiveChoices(item));
            bool satisfiable = preference == null || locoSets
                .Values
                .Any((ISet<PreferredLocomotive> choices) => choices.Contains(preference));
            if (!satisfiable)
                yield break;

            int uiD = 0;
            foreach (ListConsistItem item in List.Reverse())
            {
                PreferredLocomotive target;
                if (preference == null)
                    target = null;
                else
                    target = locoSets[item].Contains(preference) ? preference : PreferredLocomotive.NoLocomotive;
                foreach (WagonReference wagonRef in item.GetReverseWagonList(store, memoizer, basePath, folders, uiD, target))
                {
                    uiD++;
                    yield return wagonRef;
                }
            }
        }

        internal override ISet<PreferredLocomotive> GetLeadLocomotiveChoices(LocomotiveChoiceMemoizer memoizer)
        {
            var engines = new HashSet<PreferredLocomotive>();
            foreach (IConsistItem item in List)
            {
                ISet<PreferredLocomotive> choices = memoizer.GetLeadLocomotiveChoices(item);
                bool moreEngines = choices.Contains(PreferredLocomotive.NoLocomotive);

                choices.Remove(PreferredLocomotive.NoLocomotive);
                engines.UnionWith(choices);

                if (!moreEngines)
                    break;
            }
            return engines.Count > 0 ? engines : PreferredLocomotive.NoLocomotiveSet;
        }

        internal override ISet<PreferredLocomotive> GetReverseLocomotiveChoices(LocomotiveChoiceMemoizer memoizer)
        {
            var engines = new HashSet<PreferredLocomotive>();
            foreach (IConsistItem item in List.Reverse())
            {
                ISet<PreferredLocomotive> choices = memoizer.GetReverseLocomotiveChoices(item);
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

        internal override IEnumerable<WagonReference> GetForwardWagonList(ConsistStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            var table = new List<(float, float, RandomConsistItem)>();
            float p = 0f;
            IEnumerable<RandomConsistItem> searchItems = Random;
            if (preference != null)
                searchItems = searchItems.Where((RandomConsistItem item) => memoizer.GetLeadLocomotiveChoices(item).Contains(preference));
            foreach (RandomConsistItem item in searchItems)
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

            double random = RnJesus.NextDouble() * p;
            RandomConsistItem selected = table
                .Where(((float, float, RandomConsistItem) tuple) => tuple.Item1 <= random && random < tuple.Item2)
                .Select(((float, float, RandomConsistItem) tuple) => tuple.Item3)
                .First();
            return selected.GetForwardWagonList(store, memoizer, basePath, folders, startUiD: 0, preference);
        }

        internal override IEnumerable<WagonReference> GetReverseWagonList(ConsistStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            var table = new List<(float, float, RandomConsistItem)>();
            float p = 0f;
            IEnumerable<RandomConsistItem> searchItems = Random;
            if (preference != null)
                searchItems = searchItems.Where((RandomConsistItem item) => memoizer.GetReverseLocomotiveChoices(item).Contains(preference));
            foreach (RandomConsistItem item in searchItems)
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

            double random = RnJesus.NextDouble() * p;
            RandomConsistItem selected = table
                .Where(((float, float, RandomConsistItem) tuple) => tuple.Item1 <= random && random < tuple.Item2)
                .Select(((float, float, RandomConsistItem) tuple) => tuple.Item3)
                .First();
            return selected.GetReverseWagonList(store, memoizer, basePath, folders, startUiD: 0, preference);
        }

        internal override ISet<PreferredLocomotive> GetLeadLocomotiveChoices(LocomotiveChoiceMemoizer memoizer) => Random
            .Where((RandomConsistItem item) => item.Probability > 0f)
            .Select((RandomConsistItem item) => memoizer.GetLeadLocomotiveChoices(item))
            .Aggregate(new HashSet<PreferredLocomotive>(), (ISet<PreferredLocomotive> accum, IEnumerable<PreferredLocomotive> choices) =>
            {
                accum.UnionWith(choices);
                return accum;
            });

        internal override ISet<PreferredLocomotive> GetReverseLocomotiveChoices(LocomotiveChoiceMemoizer memoizer) => Random
            .Where((RandomConsistItem item) => item.Probability > 0f)
            .Select((RandomConsistItem item) => memoizer.GetReverseLocomotiveChoices(item))
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
    /// <strong>NOT</strong> thread-safe.
    /// </remarks>
    internal class ConsistStore
    {
        /// <summary>
        /// Multiple stores share the same cache.
        /// </summary>
        private Dictionary<string, IConsist> Consists { get; }

        /// <summary>
        /// Each store has its own running stack of visited consists.
        /// </summary>
        private HashSet<string> Visited { get; }

        /// <summary>
        /// Create a top-level consist store.
        /// </summary>
        public ConsistStore()
        {
            Consists = new Dictionary<string, IConsist>();
            Visited = new HashSet<string>();
        }

        private ConsistStore(ConsistStore parent, HashSet<string> visited)
        {
            Consists = parent.Consists;
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
            return (consist, new ConsistStore(this, new HashSet<string>(Visited) { filePath }));
        }
    }

    /// <summary>
    /// Memoizer for <see cref="ConsistFile.GetLeadLocomotiveChoices(string, IDictionary{string, string})"/>
    /// and <see cref="ConsistFile.GetReverseLocomotiveChoices(string, IDictionary{string, string})"/> computations.
    /// </summary>
    /// <remarks>
    /// <strong>NOT</strong> thread-safe.
    /// </remarks>
    internal class LocomotiveChoiceMemoizer
    {
        /// <summary>
        /// Each memoizer has its own <see cref="ConsistStore"/>.
        /// </summary>
        private ConsistStore Store { get; }

        /// <summary>
        /// Each memoizer has its own current content directory.
        /// </summary>
        private string BasePath { get; }

        /// <summary>
        /// Memoizers share the content folder dictionary.
        /// </summary>
        private IDictionary<string, string> Folders { get; }

        /// <summary>
        /// The lead locomotive choices cache.
        /// </summary>
        private Dictionary<IConsistItem, ISet<PreferredLocomotive>> LeadLocomotives { get; }

        /// <summary>
        /// The reverse locomotive choices cache.
        /// </summary>
        private Dictionary<IConsistItem, ISet<PreferredLocomotive>> ReverseLocomotives { get; }

        /// <summary>
        /// Create a top-level locomotive choice memoizer.
        /// </summary>
        /// <param name="store">The top-level consist cache.</param>
        /// <param name="basePath">The top-level content directory.</param>
        /// <param name="folders">All other content directories.</param>
        public LocomotiveChoiceMemoizer(ConsistStore store, string basePath, IDictionary<string, string> folders)
        {
            Store = store;
            BasePath = basePath;
            Folders = folders;
            LeadLocomotives = new Dictionary<IConsistItem, ISet<PreferredLocomotive>>();
            ReverseLocomotives = new Dictionary<IConsistItem, ISet<PreferredLocomotive>>();
        }

        private LocomotiveChoiceMemoizer(LocomotiveChoiceMemoizer parent, string basePath, ConsistStore store)
        {
            Store = store;
            BasePath = basePath;
            Folders = parent.Folders;
            LeadLocomotives = parent.LeadLocomotives;
            ReverseLocomotives = parent.ReverseLocomotives;
        }

        /// <summary>
        /// Get the head-end locomotives that this consist item can spawn with.
        /// </summary>
        /// <param name="item">The consist item.</param>
        /// <returns>The set of all <see cref="PreferredLocomotive"/> choices, or <see cref="PreferredLocomotive.NoLocomotiveSet"/>
        /// for wagons or references to all-wagon consists.</returns>
        public ISet<PreferredLocomotive> GetLeadLocomotiveChoices(IConsistItem item)
        {
            if (LeadLocomotives.TryGetValue(item, out ISet<PreferredLocomotive> cached))
                return cached;

            ISet<PreferredLocomotive> choices = GetLeadLocomotiveChoicesUncached(item);
            LeadLocomotives[item] = choices;
            return choices;
        }

        private ISet<PreferredLocomotive> GetLeadLocomotiveChoicesUncached(IConsistItem item)
        {
            string basePath = item.ResolveBasePath(BasePath, Folders);
            if (item is IConsistWagon)
            {
                return PreferredLocomotive.NoLocomotiveSet;
            }
            else if (item is IConsistEngine engine)
            {
                return new HashSet<PreferredLocomotive>() { new PreferredLocomotive(engine.GetPath(BasePath, Folders)) };
            }
            else if (item is IConsistReference consist)
            {
                (IConsist subConsist, ConsistStore subStore) = Store.CreateSubConsist(basePath, consist.Consist);
                if (subConsist is ConsistFile ortsConsist)
                    // ORTS consists can reference other consists, and hence need a ConsistStore to guard against recursion.
                    return ortsConsist.GetLeadLocomotiveChoices(new LocomotiveChoiceMemoizer(this, basePath, subStore));
                else
                    return subConsist.GetLeadLocomotiveChoices(basePath, Folders);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Get the reverse-direction locomotives that this consist item can spawn with.
        /// </summary>
        /// <param name="item">The consist item.</param>
        /// <returns>The set of all <see cref="PreferredLocomotive"/> choices, or <see cref="PreferredLocomotive.NoLocomotiveSet"/>
        /// for wagons or references to all-wagon consists.</returns>
        public ISet<PreferredLocomotive> GetReverseLocomotiveChoices(IConsistItem item)
        {
            if (ReverseLocomotives.TryGetValue(item, out ISet<PreferredLocomotive> cached))
                return cached;

            ISet<PreferredLocomotive> choices = GetReverseLocomotiveChoicesUncached(item);
            ReverseLocomotives[item] = choices;
            return choices;
        }

        private ISet<PreferredLocomotive> GetReverseLocomotiveChoicesUncached(IConsistItem item)
        {
            string basePath = item.ResolveBasePath(BasePath, Folders);
            if (item is IConsistWagon)
            {
                return PreferredLocomotive.NoLocomotiveSet;
            }
            else if (item is IConsistEngine engine)
            {
                return new HashSet<PreferredLocomotive>() { new PreferredLocomotive(engine.GetPath(BasePath, Folders)) };
            }
            else if (item is IConsistReference consist)
            {
                (IConsist subConsist, ConsistStore subStore) = Store.CreateSubConsist(basePath, consist.Consist);
                if (subConsist is ConsistFile ortsConsist)
                    // ORTS consists can reference other consists, and hence need a ConsistStore to guard against recursion.
                    return ortsConsist.GetReverseLocomotiveChoices(new LocomotiveChoiceMemoizer(this, basePath, subStore));
                else
                    return subConsist.GetReverseLocomotiveChoices(basePath, Folders);
            }
            else
            {
                throw new InvalidOperationException();
            }
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
