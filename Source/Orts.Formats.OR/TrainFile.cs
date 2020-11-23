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
using Orts.Formats.Msts;
using ORTS.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orts.Formats.OR
{
    /* To make things easy, we use the Newtonsoft library to deserialize our classes,
     * with the help of some custom converters to handle the polymorphic cases. */

    /// <summary>
    /// A native Open Rails JSON train.
    /// </summary>
    public class TrainFile : IVehicleList
    {
        public string DisplayName { get; set; } = "Loose train.";
        public float? MaxVelocityMpS { get; set; }
        public float Durability { get; set; } = 1f;
        public bool PlayerDrivable { get; set; } = true;
        public bool IsTilting { get; set; } = false;

        /// <summary>
        /// Load a train from a JSON file.
        /// </summary>
        /// <param name="filePath">The path to load the train from.</param>
        /// <returns>The loaded train.</returns>
        public static TrainFile LoadFrom(string filePath)
        {
            JsonSerializer serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
                Converters = new JsonConverter[] { new TrainFileConverter() }
            });
            using (var reader = new JsonTextReader(File.OpenText(filePath)))
                return (TrainFile)serializer.Deserialize(reader, typeof(TrainFile));
        }

        public ISet<PreferredLocomotive> GetLeadLocomotiveChoices(string basePath, IDictionary<string, string> folders) =>
            GetLeadLocomotiveChoices(new LocomotiveChoiceMemoizer(new TrainFileStore(), basePath, folders));

        internal virtual ISet<PreferredLocomotive> GetLeadLocomotiveChoices(LocomotiveChoiceMemoizer memoizer)
        {
            throw new InvalidOperationException();
        }

        public ISet<PreferredLocomotive> GetReverseLocomotiveChoices(string basePath, IDictionary<string, string> folders) =>
            GetReverseLocomotiveChoices(new LocomotiveChoiceMemoizer(new TrainFileStore(), basePath, folders));

        internal virtual ISet<PreferredLocomotive> GetReverseLocomotiveChoices(LocomotiveChoiceMemoizer memoizer)
        {
            throw new InvalidOperationException();
        }

        public IEnumerable<WagonReference> GetForwardWagonList(string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            var store = new TrainFileStore();
            return GetForwardWagonList(store, new LocomotiveChoiceMemoizer(store, basePath, folders), basePath, folders, preference);
        }

        internal virtual IEnumerable<WagonReference> GetForwardWagonList(TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            throw new InvalidOperationException();
        }

        public IEnumerable<WagonReference> GetReverseWagonList(string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            var store = new TrainFileStore();
            return GetReverseWagonList(store, new LocomotiveChoiceMemoizer(store, basePath, folders), basePath, folders, preference);
        }

        internal virtual IEnumerable<WagonReference> GetReverseWagonList(TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            throw new InvalidOperationException();
        }

        public override string ToString() => DisplayName;
    }

    #region Item interfaces
    /// <summary>
    /// A wagon, engine, train, or consist reference in a train.
    /// </summary>
    public interface ITrainListItem
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
        string X_Profile { get; set; }
    }

    public interface ITrainWagon : ITrainListItem
    {
        /// <summary>
        /// Path to the wagon description file, relative to the TRAINSET folder and omitting the file extension.
        /// </summary>
        string Wagon { get; set; }
    }

    public interface ITrainEngine : ITrainListItem
    {
        /// <summary>
        /// Path to the engine description file, relative to the TRAINSET folder and omitting the file extension.
        /// </summary>
        string Engine { get; set; }
    }

    public interface ITrainReference : ITrainListItem
    {
        /// <summary>
        /// Path to the train file, relative to the LISTS folder and omitting the file extension.
        /// </summary>
        string Train { get; set; }
    }

    public interface ITrainConsist : ITrainListItem
    {
        /// <summary>
        /// Path to the consist file, relative to the CONSISTS folder and omitting the file extension.
        /// </summary>
        string Consist { get; set; }
    }

    internal static class TrainItemExtensions
    {
        /// <summary>
        /// Get the wagon reference(s) generated by a given train item.
        /// </summary>
        /// <param name="item">The train item. The subtype is automatically determined.</param>
        /// <param name="store">The train file cache.</param>
        /// <param name="memoizer">The locomotive choice memoizer.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">A dictionary of other available content directories.</param>
        /// <param name="startUiD">The UiD of the generated wagon, or, if this item generates multiple wagons,
        /// the first UiD in the sequence. Subsequent UiD's will count up.</param>
        /// <param name="preference">The preferred player locomotive.</param>
        /// <returns>The wagon list.</returns>
        public static IEnumerable<WagonReference> GetForwardWagonList(this ITrainListItem item, TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            basePath = item.ResolveBasePath(basePath, folders);
            if (item is ITrainWagon wagon)
                return wagon.GetWagonList(basePath, folders, startUiD, flip: false);
            else if (item is ITrainEngine engine)
                return engine.GetWagonList(basePath, folders, startUiD, flip: false);
            else if (item is ITrainReference train)
                return train.GetForwardWagonList(store, memoizer, basePath, folders, startUiD, preference);
            else if (item is ITrainConsist consist)
                return consist.GetForwardWagonList(store, basePath, folders, startUiD, preference);
            else
                throw new InvalidOperationException();
        }

        /// <summary>
        /// Get the wagon reference(s) generated by a given train item, flipped and in reverse order.
        /// </summary>
        /// <param name="item">The train item. The subtype is automatically determined.</param>
        /// <param name="store">The train file cache.</param>
        /// <param name="memoizer">The locomotive choice memoizer.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">A dictionary of other available content directories.</param>
        /// <param name="startUiD">The UiD of the generated wagon, or, if this item generates multiple wagons,
        /// the first UiD in the sequence. Subsequent UiD's will count up.</param>
        /// <param name="preference">The preferred player locomotive.</param>
        /// <returns>The wagon list.</returns>
        public static IEnumerable<WagonReference> GetReverseWagonList(this ITrainListItem item, TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            basePath = item.ResolveBasePath(basePath, folders);
            if (item is ITrainWagon wagon)
                return wagon.GetWagonList(basePath, folders, startUiD, flip: true);
            else if (item is ITrainEngine engine)
                return engine.GetWagonList(basePath, folders, startUiD, flip: true);
            else if (item is ITrainReference train)
                return train.GetReverseWagonList(store, memoizer, basePath, folders, startUiD, preference);
            else if (item is ITrainConsist consist)
                return consist.GetReverseWagonList(store, basePath, folders, startUiD, preference);
            else
                throw new InvalidOperationException();
        }

        private static IEnumerable<WagonReference> GetWagonList(this ITrainWagon wagon, string basePath, IDictionary<string, string> folders, int startUiD, bool flip)
        {
            string filePath = wagon.GetPath(basePath, folders);
            return Enumerable.Range(0, wagon.Count)
                .Select((int i) => new WagonReference(filePath, flip ? !wagon.Flip : wagon.Flip, startUiD + i));
        }

        private static IEnumerable<WagonReference> GetWagonList(this ITrainEngine engine, string basePath, IDictionary<string, string> folders, int startUiD, bool flip)
        {
            string filePath = engine.GetPath(basePath, folders);
            return Enumerable.Range(0, engine.Count)
                .Select((int i) => new WagonReference(filePath, flip ? !engine.Flip : engine.Flip, startUiD + i));
        }

        private static IEnumerable<WagonReference> GetForwardWagonList(this ITrainReference train, TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            (TrainFile subTrain, TrainFileStore subStore) = store.CreateSubTrain(basePath, train.Train);
            return BuildFullWagonList(
                () => subTrain.GetForwardWagonList(subStore, memoizer, basePath, folders, preference),
                startUiD,
                train.Count,
                train.Flip);
        }

        private static IEnumerable<WagonReference> GetReverseWagonList(this ITrainReference train, TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            (TrainFile subTrain, TrainFileStore subStore) = store.CreateSubTrain(basePath, train.Train);
            return BuildFullWagonList(
                () => subTrain.GetReverseWagonList(subStore, memoizer, basePath, folders, preference),
                startUiD,
                train.Count,
                train.Flip);
        }

        private static IEnumerable<WagonReference> GetForwardWagonList(this ITrainConsist consist, TrainFileStore store, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            ConsistFile subConsist = store.CreateSubConsist(basePath, consist.Consist);
            return BuildFullWagonList(
                () => subConsist.GetForwardWagonList(basePath, folders, preference),
                startUiD,
                consist.Count,
                consist.Flip);
        }

        private static IEnumerable<WagonReference> GetReverseWagonList(this ITrainConsist consist, TrainFileStore store, string basePath, IDictionary<string, string> folders, int startUiD, PreferredLocomotive preference = null)
        {
            ConsistFile subConsist = store.CreateSubConsist(basePath, consist.Consist);
            return BuildFullWagonList(
                () => subConsist.GetReverseWagonList(basePath, folders, preference),
                startUiD,
                consist.Count,
                consist.Flip);
        }

        private static IEnumerable<WagonReference> BuildFullWagonList(Func<IEnumerable<WagonReference>> newWagonList, int startUiD, int count, bool flip)
        {
            IEnumerable<WagonReference> MakeList(int localStartUiD)
            {
                IEnumerable<WagonReference> localWagonRefs = newWagonList();
                if (flip)
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
            foreach (int _ in Enumerable.Range(0, count))
            {
                var thisList = new List<WagonReference>(MakeList(startUiD));
                startUiD += thisList.Count;
                wagonRefs = wagonRefs.Concat(thisList);
            }
            return wagonRefs;
        }

        /// <summary>
        /// Get the wagon specification file path for an <see cref="ITrainWagon"/>.
        /// </summary>
        /// <param name="wagon">The wagon.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">All other content directories.</param>
        /// <returns>The path to the .wag file.</returns>
        public static string GetPath(this ITrainWagon wagon, string basePath, IDictionary<string, string> folders)
        {
            var folderSplit = wagon.Wagon.Split(Path.DirectorySeparatorChar);
            var subFolders = new ArraySegment<string>(folderSplit, 0, folderSplit.Length - 1);
            var filename = folderSplit.Last();
            return VehicleListUtilities.ResolveWagonFile(wagon.ResolveBasePath(basePath, folders), subFolders, filename);
        }

        /// <summary>
        /// Get the engine specification file for an <see cref="ITrainEngine"/>.
        /// </summary>
        /// <param name="engine">The engine.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">All other content directories.</param>
        /// <returns>The path to the .eng file</returns>
        public static string GetPath(this ITrainEngine engine, string basePath, IDictionary<string, string> folders)
        {
            var folderSplit = engine.Engine.Split(Path.DirectorySeparatorChar);
            var subFolders = new ArraySegment<string>(folderSplit, 0, folderSplit.Length - 1);
            var filename = folderSplit.Last();
            return VehicleListUtilities.ResolveEngineFile(engine.ResolveBasePath(basePath, folders), subFolders, filename);
        }

        /// <summary>
        /// Get the content directory for an <see cref="ITrainListItem"/> that may or may not reside in the current content directory.
        /// </summary>
        /// <param name="item">The item to query.</param>
        /// <param name="basePath">The current content directory.</param>
        /// <param name="folders">All other content directories.</param>
        /// <returns>The resolved content directory, which may or may not be equivalent to basePath.</returns>
        public static string ResolveBasePath(this ITrainListItem item, string basePath, IDictionary<string, string> folders)
        {
            if (item.X_Profile != null)
            {
                if (!folders.TryGetValue(item.X_Profile, out string newBasePath))
                    throw new DirectoryNotFoundException($"Unknown installation profile: {item.X_Profile}");
                return newBasePath;
            }
            return basePath;
        }
    }
    #endregion

    #region List train type
    /// <summary>
    /// "List" trains assemble their items in linear order.
    /// </summary>
    public class ListTrainFile : TrainFile
    {
        public IList<ListTrainItem> List { get; set; } = new List<ListTrainItem>();

        internal override IEnumerable<WagonReference> GetForwardWagonList(TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            Dictionary<ListTrainItem, ISet<PreferredLocomotive>> locoSets = List
                .ToDictionary((ListTrainItem item) => item, (ListTrainItem item) => memoizer.GetLeadLocomotiveChoices(item));
            bool satisfiable = preference == null || locoSets
                .Values
                .Any((ISet<PreferredLocomotive> choices) => choices.Contains(preference));
            if (!satisfiable)
                yield break;

            int uiD = 0;
            bool satisfied = preference == null;
            foreach (ListTrainItem item in List)
            {
                PreferredLocomotive target;
                if (satisfied)
                {
                    target = null;
                }
                else if (locoSets[item].Contains(preference))
                {
                    target = preference;
                    satisfied = true;
                }
                else
                {
                    target = PreferredLocomotive.NoLocomotive;
                }
                foreach (WagonReference wagonRef in item.GetForwardWagonList(store, memoizer, basePath, folders, uiD, target))
                {
                    uiD++;
                    yield return wagonRef;
                }
            }
        }

        internal override IEnumerable<WagonReference> GetReverseWagonList(TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            Dictionary<ListTrainItem, ISet<PreferredLocomotive>> locoSets = List
                .ToDictionary((ListTrainItem item) => item, (ListTrainItem item) => memoizer.GetReverseLocomotiveChoices(item));
            bool satisfiable = preference == null || locoSets
                .Values
                .Any((ISet<PreferredLocomotive> choices) => choices.Contains(preference));
            if (!satisfiable)
                yield break;

            int uiD = 0;
            bool satisfied = preference == null;
            foreach (ListTrainItem item in List.Reverse())
            {
                PreferredLocomotive target;
                if (satisfied)
                {
                    target = null;
                }
                else if (locoSets[item].Contains(preference))
                {
                    target = preference;
                    satisfied = true;
                }
                else
                {
                    target = PreferredLocomotive.NoLocomotive;
                }
                foreach (WagonReference wagonRef in item.GetReverseWagonList(store, memoizer, basePath, folders, uiD, target))
                {
                    uiD++;
                    yield return wagonRef;
                }
            }
        }

        internal override ISet<PreferredLocomotive> GetLeadLocomotiveChoices(LocomotiveChoiceMemoizer memoizer)
        {
            if (List.Count == 0)
                return new HashSet<PreferredLocomotive>();

            var engines = new HashSet<PreferredLocomotive>();
            foreach (ITrainListItem item in List)
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
            if (List.Count == 0)
                return new HashSet<PreferredLocomotive>();

            var engines = new HashSet<PreferredLocomotive>();
            foreach (ITrainListItem item in List.Reverse())
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

    [JsonConverter(typeof(ListTrainItemConverter))]
    public class ListTrainItem : ITrainListItem
    {
        public int Count { get; set; } = 1;
        public bool Flip { get; set; } = false;
        public string X_Profile { get; set; } = null;
    }

    public class ListTrainWagon : ListTrainItem, ITrainWagon
    {
        public string Wagon { get; set; }
    }

    public class ListTrainEngine : ListTrainItem, ITrainEngine
    {
        public string Engine { get; set; }
    }

    public class ListTrainReference : ListTrainItem, ITrainReference
    {
        public string Train { get; set; }
    }

    public class ListTrainConsist : ListTrainItem, ITrainConsist
    {
        public string Consist { get; set; }
    }

    /// <summary>
    /// JSON deserializer for polymorphic <see cref="ListTrainItem"/>s.
    /// </summary>
    public class ListTrainItemConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(ListTrainItem);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            bool isWagon = jsonObject.ContainsKey("Wagon");
            bool isEngine = jsonObject.ContainsKey("Engine");
            bool isTrain = jsonObject.ContainsKey("Train");
            bool isConsist = jsonObject.ContainsKey("Consist");
            ListTrainItem item;
            if (!isWagon && !isEngine && !isTrain && !isConsist)
                throw new JsonSerializationException("Unrecognized consist item");
            else if (isWagon && !isEngine && !isTrain && !isConsist)
                item = new ListTrainWagon();
            else if (!isWagon && isEngine && !isTrain && !isConsist)
                item = new ListTrainEngine();
            else if (!isWagon && !isEngine && isTrain && !isConsist)
                item = new ListTrainReference();
            else if (!isWagon && !isEngine && !isTrain && isConsist)
                item = new ListTrainConsist();
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

    #region Random train type
    /// <summary>
    /// "Random" trains select probabilistically from a set of alternative items.
    /// </summary>
    public class RandomTrainFile : TrainFile
    {
        public IList<RandomTrainItem> Random { get; set; } = new List<RandomTrainItem>();

        private static readonly Random RnJesus = new Random();

        internal override IEnumerable<WagonReference> GetForwardWagonList(TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            var table = new List<(float, float, RandomTrainItem)>();
            float p = 0f;
            IEnumerable<RandomTrainItem> searchItems = Random;
            if (preference != null)
                searchItems = searchItems.Where((RandomTrainItem item) => memoizer.GetLeadLocomotiveChoices(item).Contains(preference));
            foreach (RandomTrainItem item in searchItems)
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
            RandomTrainItem selected = table
                .Where(((float, float, RandomTrainItem) tuple) => tuple.Item1 <= random && random < tuple.Item2)
                .Select(((float, float, RandomTrainItem) tuple) => tuple.Item3)
                .First();
            return selected.GetForwardWagonList(store, memoizer, basePath, folders, startUiD: 0, preference);
        }

        internal override IEnumerable<WagonReference> GetReverseWagonList(TrainFileStore store, LocomotiveChoiceMemoizer memoizer, string basePath, IDictionary<string, string> folders, PreferredLocomotive preference = null)
        {
            var table = new List<(float, float, RandomTrainItem)>();
            float p = 0f;
            IEnumerable<RandomTrainItem> searchItems = Random;
            if (preference != null)
                searchItems = searchItems.Where((RandomTrainItem item) => memoizer.GetReverseLocomotiveChoices(item).Contains(preference));
            foreach (RandomTrainItem item in searchItems)
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
            RandomTrainItem selected = table
                .Where(((float, float, RandomTrainItem) tuple) => tuple.Item1 <= random && random < tuple.Item2)
                .Select(((float, float, RandomTrainItem) tuple) => tuple.Item3)
                .First();
            return selected.GetReverseWagonList(store, memoizer, basePath, folders, startUiD: 0, preference);
        }

        internal override ISet<PreferredLocomotive> GetLeadLocomotiveChoices(LocomotiveChoiceMemoizer memoizer) => Random
            .Where((RandomTrainItem item) => item.Probability > 0f)
            .Select((RandomTrainItem item) => memoizer.GetLeadLocomotiveChoices(item))
            .Aggregate(new HashSet<PreferredLocomotive>(), (ISet<PreferredLocomotive> accum, IEnumerable<PreferredLocomotive> choices) =>
            {
                accum.UnionWith(choices);
                return accum;
            });

        internal override ISet<PreferredLocomotive> GetReverseLocomotiveChoices(LocomotiveChoiceMemoizer memoizer) => Random
            .Where((RandomTrainItem item) => item.Probability > 0f)
            .Select((RandomTrainItem item) => memoizer.GetReverseLocomotiveChoices(item))
            .Aggregate(new HashSet<PreferredLocomotive>(), (ISet<PreferredLocomotive> accum, IEnumerable<PreferredLocomotive> choices) =>
            {
                accum.UnionWith(choices);
                return accum;
            });
    }

    [JsonConverter(typeof(RandomTrainItemConverter))]
    public class RandomTrainItem : ITrainListItem
    {
        public int Count { get; set; } = 1;
        public bool Flip { get; set; } = false;
        public string X_Profile { get; set; } = null;
        public float Probability { get; set; }
    }

    public class RandomTrainWagon : RandomTrainItem, ITrainWagon
    {
        public string Wagon { get; set; }
    }

    public class RandomTrainEngine : RandomTrainItem, ITrainEngine
    {
        public string Engine { get; set; }
    }

    public class RandomTrainReference : RandomTrainItem, ITrainReference
    {
        public string Train { get; set; }
    }

    public class RandomTrainConsist : RandomTrainItem, ITrainConsist
    {
        public string Consist { get; set; }
    }

    /// <summary>
    /// JSON deserializer for polymorphic <see cref="RandomTrainItem"/>s.
    /// </summary>
    public class RandomTrainItemConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(RandomTrainItem);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            bool isWagon = jsonObject.ContainsKey("Wagon");
            bool isEngine = jsonObject.ContainsKey("Engine");
            bool isTrain = jsonObject.ContainsKey("Train");
            bool isConsist = jsonObject.ContainsKey("Consist");
            RandomTrainItem item;
            if (!isWagon && !isEngine && !isTrain && !isConsist)
                throw new JsonSerializationException("Unrecognized consist item");
            else if (isWagon && !isEngine && !isTrain && !isConsist)
                item = new RandomTrainWagon();
            else if (!isWagon && isEngine && !isTrain && !isConsist)
                item = new RandomTrainEngine();
            else if (!isWagon && !isEngine && isTrain && !isConsist)
                item = new RandomTrainReference();
            else if (!isWagon && !isEngine && !isTrain && isConsist)
                item = new RandomTrainConsist();
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
    /// JSON deserializer for polymorphic <see cref="TrainFile"/>s.
    /// </summary>
    public class TrainFileConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(TrainFile);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            bool isList = jsonObject.ContainsKey("List");
            bool isRandom = jsonObject.ContainsKey("Random");
            TrainFile list;
            if (!isList && !isRandom)
                throw new JsonSerializationException("Unrecognized train type");
            else if (isList && !isRandom)
                list = new ListTrainFile();
            else if (!isList && isRandom)
                list = new RandomTrainFile();
            else
                throw new JsonSerializationException("Ambiguous train type");
            serializer.Populate(jsonObject.CreateReader(), list);
            return list;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Tracks the current stack of visited trains, so as to prevent recursion, and caches train file loads.
    /// </summary>
    /// <remarks>
    /// <strong>NOT</strong> thread-safe.
    /// </remarks>
    internal class TrainFileStore
    {
        /// <summary>
        /// Multiple stores share the same train cache.
        /// </summary>
        private Dictionary<string, TrainFile> TrainFiles { get; }

        /// <summary>
        /// Multiple stores share the same consist cache.
        /// </summary>
        private Dictionary<string, ConsistFile> Consists { get; }

        /// <summary>
        /// Each store has its own running stack of visited trains.
        /// </summary>
        private HashSet<string> Visited { get; }

        /// <summary>
        /// Create a top-level train store.
        /// </summary>
        public TrainFileStore()
        {
            TrainFiles = new Dictionary<string, TrainFile>();
            Visited = new HashSet<string>();
        }

        private TrainFileStore(TrainFileStore parent, HashSet<string> visited)
        {
            TrainFiles = parent.TrainFiles;
            Visited = visited;
        }

        /// <summary>
        /// Add a new layer of recursion by loading a sub-train.
        /// </summary>
        /// <param name="baseName">The train file to load.</param>
        /// <returns>The loaded train and a new <see cref="TrainFileStore"/> handle.</returns>
        public (TrainFile, TrainFileStore) CreateSubTrain(string basePath, string filename)
        {
            string filePath = VehicleListUtilities.ResolveOrtsTrainFile(basePath, filename);
            if (Visited.Contains(filePath))
                throw new RecursiveTrainException($"train loads itself: {filePath}");

            TrainFile list;
            if (TrainFiles.TryGetValue(filePath, out TrainFile cached))
            {
                list = cached;
            }
            else
            {
                list = TrainFile.LoadFrom(filePath);
                TrainFiles[filePath] = list;
            }
            return (list, new TrainFileStore(this, new HashSet<string>(Visited) { filePath }));
        }

        /// <summary>
        /// Load a sub-consist.
        /// </summary>
        /// <param name="baseName">The train file to load.</param>
        /// <returns>The loaded train and a new <see cref="TrainFileStore"/> handle.</returns>
        public ConsistFile CreateSubConsist(string basePath, string filename)
        {
            string filePath = VehicleListUtilities.ResolveMstsConsist(basePath, filename);
            ConsistFile consist;
            if (Consists.TryGetValue(filePath, out ConsistFile cached))
            {
                consist = cached;
            }
            else
            {
                consist = new ConsistFile(filePath);
                Consists[filePath] = consist;
            }
            return consist;
        }
    }

    /// <summary>
    /// Memoizer for <see cref="TrainFile.GetLeadLocomotiveChoices(string, IDictionary{string, string})"/>
    /// and <see cref="TrainFile.GetReverseLocomotiveChoices(string, IDictionary{string, string})"/> computations.
    /// </summary>
    /// <remarks>
    /// <strong>NOT</strong> thread-safe.
    /// </remarks>
    internal class LocomotiveChoiceMemoizer
    {
        /// <summary>
        /// Each memoizer has its own <see cref="TrainFileStore"/>.
        /// </summary>
        private TrainFileStore Store { get; }

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
        private Dictionary<ITrainListItem, ISet<PreferredLocomotive>> LeadLocomotives { get; }

        /// <summary>
        /// The reverse locomotive choices cache.
        /// </summary>
        private Dictionary<ITrainListItem, ISet<PreferredLocomotive>> ReverseLocomotives { get; }

        /// <summary>
        /// Create a top-level locomotive choice memoizer.
        /// </summary>
        /// <param name="store">The top-level train cache.</param>
        /// <param name="basePath">The top-level content directory.</param>
        /// <param name="folders">All other content directories.</param>
        public LocomotiveChoiceMemoizer(TrainFileStore store, string basePath, IDictionary<string, string> folders)
        {
            Store = store;
            BasePath = basePath;
            Folders = folders;
            LeadLocomotives = new Dictionary<ITrainListItem, ISet<PreferredLocomotive>>();
            ReverseLocomotives = new Dictionary<ITrainListItem, ISet<PreferredLocomotive>>();
        }

        private LocomotiveChoiceMemoizer(LocomotiveChoiceMemoizer parent, string basePath, TrainFileStore store)
        {
            Store = store;
            BasePath = basePath;
            Folders = parent.Folders;
            LeadLocomotives = parent.LeadLocomotives;
            ReverseLocomotives = parent.ReverseLocomotives;
        }

        /// <summary>
        /// Get the head-end locomotives that this train item can spawn with.
        /// </summary>
        /// <param name="item">The train item.</param>
        /// <returns>The set of all <see cref="PreferredLocomotive"/> choices, or <see cref="PreferredLocomotive.NoLocomotiveSet"/>
        /// for wagons or references to all-wagon trains.</returns>
        public ISet<PreferredLocomotive> GetLeadLocomotiveChoices(ITrainListItem item)
        {
            if (LeadLocomotives.TryGetValue(item, out ISet<PreferredLocomotive> cached))
                return cached;

            ISet<PreferredLocomotive> choices = GetLeadLocomotiveChoicesUncached(item);
            LeadLocomotives[item] = choices;
            return choices;
        }

        private ISet<PreferredLocomotive> GetLeadLocomotiveChoicesUncached(ITrainListItem item)
        {
            string basePath = item.ResolveBasePath(BasePath, Folders);
            if (item is ITrainWagon)
            {
                return PreferredLocomotive.NoLocomotiveSet;
            }
            else if (item is ITrainEngine engine)
            {
                return new HashSet<PreferredLocomotive>() { new PreferredLocomotive(engine.GetPath(BasePath, Folders)) };
            }
            else if (item is ITrainReference list)
            {
                (IVehicleList subList, TrainFileStore subStore) = Store.CreateSubTrain(basePath, list.Train);
                if (subList is TrainFile ortsList)
                    // ORTS trains can reference other trains, and hence need a TrainFileStore to guard against recursion.
                    return ortsList.GetLeadLocomotiveChoices(new LocomotiveChoiceMemoizer(this, basePath, subStore));
                else
                    return subList.GetLeadLocomotiveChoices(basePath, Folders);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Get the reverse-direction locomotives that this train item can spawn with.
        /// </summary>
        /// <param name="item">The train item.</param>
        /// <returns>The set of all <see cref="PreferredLocomotive"/> choices, or <see cref="PreferredLocomotive.NoLocomotiveSet"/>
        /// for wagons or references to all-wagon trains.</returns>
        public ISet<PreferredLocomotive> GetReverseLocomotiveChoices(ITrainListItem item)
        {
            if (ReverseLocomotives.TryGetValue(item, out ISet<PreferredLocomotive> cached))
                return cached;

            ISet<PreferredLocomotive> choices = GetReverseLocomotiveChoicesUncached(item);
            ReverseLocomotives[item] = choices;
            return choices;
        }

        private ISet<PreferredLocomotive> GetReverseLocomotiveChoicesUncached(ITrainListItem item)
        {
            string basePath = item.ResolveBasePath(BasePath, Folders);
            if (item is ITrainWagon)
            {
                return PreferredLocomotive.NoLocomotiveSet;
            }
            else if (item is ITrainEngine engine)
            {
                return new HashSet<PreferredLocomotive>() { new PreferredLocomotive(engine.GetPath(BasePath, Folders)) };
            }
            else if (item is ITrainReference list)
            {
                (IVehicleList subList, TrainFileStore subStore) = Store.CreateSubTrain(basePath, list.Train);
                if (subList is TrainFile ortsList)
                    // ORTS trains can reference other trains, and hence need a TrainFileStore to guard against recursion.
                    return ortsList.GetReverseLocomotiveChoices(new LocomotiveChoiceMemoizer(this, basePath, subStore));
                else
                    return subList.GetReverseLocomotiveChoices(basePath, Folders);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// A train refers (directly or indirectly) to itself, creating an infinite-length train.
    /// </summary>
    public class RecursiveTrainException : Exception
    {
        public RecursiveTrainException(string message) : base(message) { }
    }
}
