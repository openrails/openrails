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
    /// <summary>
    /// A native Open Rails JSON consist.
    /// </summary>
    public class ConsistFile : IConsist
    {
        public string Name { get; } = "Loose consist.";
        public float? MaxVelocityMpS { get; }
        public float Durability { get; } = 1f;
        public bool PlayerDrivable { get; } = true;

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

        public ICollection<string> GetLeadLocomotiveChoices(string basePath, IDictionary<string, string> folders)
        {
            throw new NotImplementedException();
        }

        public ICollection<string> GetReverseLocomotiveChoices(string basePath, IDictionary<string, string> folders)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<WagonReference> GetWagonList(string basePath, IDictionary<string, string> folders, string preferredLocomotivePath = null)
        {
            throw new InvalidOperationException();
        }

        public override string ToString() => Name;
    }

    #region Item interfaces
    public interface IConsistItem
    {
        int Count { get; set; }
        bool Flipped { get; set; }
    }

    public interface IConsistWagon : IConsistItem
    {
        string Wagon { get; set; }
    }

    public interface IConsistEngine : IConsistItem
    {
        string Engine { get; set; }
    }

    public interface IConsistReference : IConsistItem
    {
        string Consist { get; set; }
    }

    internal static class ConsistItemExtensions
    {
        public static IEnumerable<WagonReference> GetGenericWagonList(this IConsistItem item, string basePath, IDictionary<string, string> folders, int startUiD)
        {
            if (item is IConsistWagon wagon)
            {
                string filePath = Path.ChangeExtension(Path.Combine(basePath, "trains", "trainset", wagon.Wagon), ".wag");
                foreach (int _ in Enumerable.Range(0, wagon.Count))
                    yield return new WagonReference(filePath, wagon.Flipped, startUiD++);
            }
            else if (item is IConsistEngine engine)
            {
                string filePath = Path.ChangeExtension(Path.Combine(basePath, "trains", "trainset", engine.Engine), ".eng");
                foreach (int _ in Enumerable.Range(0, engine.Count))
                    yield return new WagonReference(filePath, engine.Flipped, startUiD++);
            }
            else if (item is IConsistReference consist)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
    #endregion

    #region List consist
    public class ListConsistFile : ConsistFile
    {
        public IList<ListConsistItem> List { get; } = new List<ListConsistItem>();

        public override IEnumerable<WagonReference> GetWagonList(string basePath, IDictionary<string, string> folders, string preferredLocomotivePath = null)
        {
            int uiD = 0;
            foreach (IConsistItem item in List)
            {
                foreach (WagonReference wagonRef in item.GetGenericWagonList(basePath, folders, uiD))
                {
                    uiD++;
                    yield return wagonRef;
                }
            }
        }
    }

    [JsonConverter(typeof(ListConsistItemConverter))]
    public class ListConsistItem : IConsistItem
    {
        public int Count { get; set; } = 1;
        public bool Flipped { get; set; } = false;
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
    public class RandomConsistFile : ConsistFile
    {
        public IList<RandomConsistItem> Random { get; } = new List<RandomConsistItem>();

        public override IEnumerable<WagonReference> GetWagonList(string basePath, IDictionary<string, string> folders, string preferredLocomotivePath = null)
        {
            throw new NotImplementedException();
        }
    }

    [JsonConverter(typeof(RandomConsistItemConverter))]
    public class RandomConsistItem : IConsistItem
    {
        public int Count { get; set; } = 1;
        public bool Flipped { get; set; } = false;
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
}
