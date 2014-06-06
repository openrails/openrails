using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSTS.Formats;
using LibAE.Common;
using LibAE.Formats;

namespace LibAE.Formats
{
    /// <summary>
    /// ORRouteConfigExt is an extension class for ORRouteConfig and its use is reserved to RunActivity.
    /// </summary>
    public static class ORRouteConfigExt
    {
        /// <summary>
        /// DistributeItems is used to distribute the OR Specific Items along the route.
        /// These Items are referenced in a table with the same size as TrackNode with the same Node Index.
        /// </summary>
        /// <param name="TDB">The current TDB config to get the TrackNodes.</param>
        public static void DistributeItems(this ORRouteConfig orRouteCfg, TDBFile TDB)
        {
            int idx;
            List<TrItem> trItemToAdd = new List<TrItem>();
            TrItem[] trItemTable = TDB.TrackDB.TrItemTable;
            foreach (var trItem in trItemTable)
            {
                trItemToAdd.Add(trItem);
            }
            idx = trItemTable.Count();
            foreach (var item in orRouteCfg.AllItems)
            {
                if (item.typeItem == (int)TypeItem.STATION_CONNECTOR)
                {
                    TrackNode node = TDB.TrackDB.TrackNodes[item.associateNodeIdx];
                    //AeConnector newTrItem = new AeConnector((StationAreaItem)item, idx);
                    //trItemToAdd.Add (newTrItem);
                    node.TrVectorNode.AddTrItemRef(idx);
                    idx++;
                }
            }
            TDB.TrackDB.TrItemTable = trItemToAdd.ToArray();
        }
    }
}
