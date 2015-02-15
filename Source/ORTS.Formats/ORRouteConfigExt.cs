using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orts.Formats.Msts;
using ORTS.Common;
using ORTS.Formats;
#if !ACTIVITY_EDITOR     //  Don't remove
using LibAE.Common;

namespace LibAE.Formats
{
    /// <summary>
    /// ORRouteConfigExt is an extension class for ORRouteConfig and its use is reserved to RunActivity.
    /// </summary>
    public static class ORRouteConfigExt
    {
        /// <summary>
        /// Scan the current orRouteConfig and search for items related to the given node
        /// </summary>
        /// <param name="iNode">The current node index</param>
        /// <param name="orRouteConfig">The Open Rail configuration coming from Editor</param>
        /// <param name="trackNodes">The list of MSTS Track Nodes</param>
        /// <param name="tsectiondat">The list of MSTS Section datas</param>
        public static List<TrackCircuitElement> GetORItemForNode(this ORRouteConfig orRouteCfg, int iNode, TrackNode[] trackNodes, TSectionDatFile tsectiondat)
        {
            List<TrackCircuitElement> trackCircuitElements = new List<TrackCircuitElement>();
            if (orRouteCfg.AllItems.Count <= 0)
                return trackCircuitElements;
            foreach (var item in orRouteCfg.AllItems)
            {
                switch (item.typeItem)
                {
                    case (int)TypeItem.STATION_CONNECTOR:
                        if (item.associateNodeIdx != iNode)
                            continue;
                        TrackNode node = trackNodes[iNode];
                        AETraveller travel = new AETraveller(orRouteCfg.traveller);
                        travel.place(node);
                        float position = travel.DistanceTo(item);
                        TrackCircuitElement element = (TrackCircuitElement)new TrackCircuitElementConnector(item, position);
                        trackCircuitElements.Add(element);
                        break;
                    default:
                        break;
                }
            }

            return trackCircuitElements;
        }
    }
}
#endif