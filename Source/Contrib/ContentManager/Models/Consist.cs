// COPYRIGHT 2015 by the Open Rails project.
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

using Orts.Formats.Msts;
using System;
using Orts.Formats.OR;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ORTS.ContentManager.Models
{
    public class Consist
    {
        public readonly string Name;

        public readonly IEnumerable<Item> Items;

        public Consist(Content content)
        {
            Debug.Assert(content.Type == ContentType.Consist);
            switch (System.IO.Path.GetExtension(content.PathName).ToLowerInvariant())
            {
                case ".consist-or":
                    var ortsConsist = Orts.Formats.OR.ConsistFile.LoadFrom(content.PathName);
                    Name = ortsConsist.Name;
                    Items = GetOrtsItems(ortsConsist);
                    break;
                case ".con":
                    var mstsConsist = new Orts.Formats.Msts.ConsistFile(content.PathName);
                    Name = mstsConsist.Name;
                    Items = GetMstsItems(mstsConsist);
                    break;
                default:
                    throw new InvalidDataException("Unknown consist format");
            }
        }

        /// <summary>
        /// Enumerate <see cref="Item"/>s for a JSON Open Rails consist.
        /// </summary>
        /// <param name="file">The consist structure to query.</param>
        /// <returns>An iterator of items.</returns>
        private static IEnumerable<Item> GetOrtsItems(Orts.Formats.OR.ConsistFile file)
        {
            var items = new List<Item>();
            int n = 0;
            if (file is ListConsistFile listFile)
            {
                foreach (ListConsistItem item in listFile.List)
                    items.Add(CreateOrtsItem(item, n++));
            }
            return items.Where((Item item) => item != null);
        }

        /// <summary>
        /// Enumerate <see cref="Item"/>s for a legacy MSTS consist.
        /// </summary>
        /// <param name="file">The consist structure to query.</param>
        /// <returns>An iterator of items.</returns>
        private static IEnumerable<Item> GetMstsItems(Orts.Formats.Msts.ConsistFile file) => file.Train.TrainCfg.WagonList
            .Select((Wagon wagon) => new MstsCar(wagon));

        public enum Direction{
            Forwards,
            Backwards,
        }

        public enum ItemType
        {
            Wagon,
            Engine,
            Consist,
        }

        /// <summary>
        /// Generic consist item that can represent a wagon, engine, or consist reference.
        /// </summary>
        public class Item
        {
            public string ID { get; }
            public string Name { get; }
            public Direction Direction { get; }
            public ItemType Type { get; }

            internal Item(string id, string name, Direction direction, ItemType type)
            {
                ID = id;
                Name = name;
                Direction = direction;
                Type = type;
            }
        }

        /// <summary>
        /// An ORTS wagon or engine.
        /// </summary>
        internal class OrtsCar : Item
        {
            internal OrtsCar(ListConsistWagon wagon, int n) : base(
                id: n.ToString(),
                name: wagon.Wagon,
                direction: wagon.Flipped ? Direction.Backwards : Direction.Forwards,
                type: ItemType.Wagon) { }

            internal OrtsCar(ListConsistEngine engine, int n) : base(
                id: n.ToString(),
                name: engine.Engine,
                direction: engine.Flipped ? Direction.Backwards : Direction.Forwards,
                type: ItemType.Engine) { }
        }

        /// <summary>
        /// Factory method to create ORTS <see cref="Item"/>s.
        /// </summary>
        /// <param name="item">The <see cref="ListConsistItem"/> or <see cref="RandomConsistItem"/>.</param>
        /// <param name="n">The ID of the new item.</param>
        /// <returns>The newly created item.</returns>
        internal static Item CreateOrtsItem(IConsistItem item, int n)
        {
            if (item is ListConsistWagon listWagonItem)
                return new OrtsCar(listWagonItem, n);
            else if (item is ListConsistEngine listEngineItem)
                return new OrtsCar(listEngineItem, n);
            else
                return null;
        }

        /// <summary>
        /// An MSTS wagon or engine.
        /// </summary>
        internal class MstsCar : Item
        {
            internal MstsCar(Wagon car) : base(
                id: car.UiD.ToString(),
                name: $"{car.Folder}/{car.Name}",
                direction: car.Flip ? Direction.Backwards : Direction.Forwards,
                type: car.IsEngine ? ItemType.Engine : ItemType.Wagon) { }
        }
    }
}
