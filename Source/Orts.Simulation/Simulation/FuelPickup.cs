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

// This file is the responsibility of the 3D & Environment Team.

using Orts.Formats.Msts;
using Orts.Simulation.RollingStocks;
using ORTS.Common;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Simulation
{
    public class FuelManager
    {
        readonly Simulator Simulator;
        public readonly Dictionary<int, FuelPickupItem> FuelPickupItems;

        public FuelManager(Simulator simulator)
        {
            Simulator = simulator;
            FuelPickupItems = simulator.TDB != null && simulator.TDB.TrackDB != null ? GetFuelPickupItemsFromDB(simulator.TDB.TrackDB.TrackNodes, simulator.TDB.TrackDB.TrItemTable) : new Dictionary<int, FuelPickupItem>();
        }

        static Dictionary<int, FuelPickupItem> GetFuelPickupItemsFromDB(TrackNode[] trackNodes, TrItem[] trItemTable)
        {
            return (from trackNode in trackNodes
                    where trackNode != null && trackNode.TrVectorNode != null && trackNode.TrVectorNode.NoItemRefs > 0
                    from itemRef in trackNode.TrVectorNode.TrItemRefs.Distinct()
                    where trItemTable[itemRef] != null && trItemTable[itemRef].ItemType == TrItem.trItemType.trPICKUP
                    select new KeyValuePair<int, FuelPickupItem>(itemRef, new FuelPickupItem(trackNode, trItemTable[itemRef])))
                    .ToDictionary(_ => _.Key, _ => _.Value);
        }

        public FuelPickupItem CreateFuelStation(WorldPosition position, IEnumerable<int> trackIDs)
        {
            var trackItems = trackIDs.Select(id => FuelPickupItems[id]).ToArray();
            return new FuelPickupItem(trackItems);
        }


    } // end Class FuelManager

    public class FuelPickupItem
    {
        internal WorldLocation Location;
        public TrackNode TrackNode;

        public FuelPickupItem(TrackNode trackNode, TrItem trItem)
        {
            TrackNode = trackNode;
            Location = new WorldLocation(trItem.TileX, trItem.TileZ, trItem.X, trItem.Y, trItem.Z);
        }

        public FuelPickupItem(IEnumerable<FuelPickupItem> items) { }

        public bool ReFill()
        {
            while (MSTSWagon.RefillProcess.OkToRefill)
            {
                return true;
            }
            if (!MSTSWagon.RefillProcess.OkToRefill)
                return false;
            return false;
        }

        public FuelPickupItem() { }

    } // end Class FuelPickupItem

} // end Class FuelManager
