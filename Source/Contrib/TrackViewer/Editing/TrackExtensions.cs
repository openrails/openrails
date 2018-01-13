// COPYRIGHT 2014, 2018 by the Open Rails project.
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
using System.Linq;
using System.Text;
using Orts.Formats.Msts;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// Extension methods that need information from the route database.
    /// </summary>
    public static class TrackExtensions
    {
        //By making the database information static, but updateable, we can concentrate a number of track-related 
        //methods in one class, without having to drag along the databases.

        /// <summary>The TrPin index of the main route of a junction node</summary>
        private static uint[] mainRouteIndex;
        /// <summary>The TrPin index of the siding route of a junction node</summary>
        private static uint[] sidingRouteIndex;

        private static TrackNode[] trackNodes;
        private static TrackSectionsFile tsectionDat;

        /// <summary>
        /// Find the indices we need to use for TrPins in the various junction nodes in case we want to use either main
        /// or siding path. That information is available in the trackshapes in the tsectionDat.
        /// </summary>
        /// <param name="trackNodesIn">The tracknodes</param>
        /// <param name="tsectionDatIn">Track section Data</param>
        public static void Initialize(TrackNode[] trackNodesIn, TrackSectionsFile tsectionDatIn)
        {
            trackNodes = trackNodesIn;
            tsectionDat = tsectionDatIn;
            
            mainRouteIndex = new uint[trackNodes.Length];
            sidingRouteIndex = new uint[trackNodes.Length];
            for (int tni = 0; tni < trackNodes.Length; tni++)
            {
                TrackNode tn = trackNodes[tni];
                if (tn == null) continue;
                if (tn.TrJunctionNode == null) continue;
                uint mainRoute = 0;

                uint trackShapeIndex = tn.TrJunctionNode.ShapeIndex;
                try
                {
                    TrackShape trackShape = tsectionDat.TrackShapes.Get(trackShapeIndex);
                    mainRoute = trackShape.MainRoute;
                }
                catch (System.IO.InvalidDataException exception)
                {
                    exception.ToString(); 
                }

                mainRouteIndex[tni] = tn.Inpins + mainRoute;
                if (mainRoute == 0)
                {   // sidingRouteIndex is simply the next
                    sidingRouteIndex[tni] = tn.Inpins + 1;
                }
                else
                {   // sidingRouteIndex is the first
                    sidingRouteIndex[tni] = tn.Inpins;
                }
            }
        }

        /// <summary>Return the vector node index of the trailing path leaving this junction.</summary>
        public static int TrailingTvn(this TrackNode trackNode) { return trackNode.TrPins[0].Link; }
        /// <summary>Return the vector node index of the main path leaving this junction (main being defined as the first one defined)</summary>
        public static int MainTvn(this TrackNode trackNode) { return trackNode.TrPins[mainRouteIndex[trackNode.Index]].Link; }
        /// <summary>Return the vector node index of the siding path leaving this junction (siding being defined as the second one defined)</summary>
        public static int SidingTvn(this TrackNode trackNode) { return trackNode.TrPins[sidingRouteIndex[trackNode.Index]].Link; }

        /// <summary>Return the vector node index at the begin of this vector node</summary>
        public static int JunctionIndexAtStart(this TrackNode trackNode) { return trackNode.TrPins[0].Link; }
        /// <summary>Return the vector node index at the end of this vector node</summary>
        public static int JunctionIndexAtEnd(this TrackNode trackNode) { return trackNode.TrPins[1].Link; }

        /// <summary>Return the tracknode corresponding the given index</summary>
        public static TrackNode TrackNode(int tvnIndex) { return trackNodes[tvnIndex]; }
       
        /// <summary>
        /// Get the index of the junction node at the other side of the linking track vector node.
        /// This uses only the track database, no trainpath nodes.
        /// </summary>
        /// <param name="junctionIndex">Index of this junction node</param>
        /// <param name="linkingTrackNodeIndex">index of the vector node linking the two junction nodes</param>
        /// <returns>The index of the junction node at the other end, or 0 in case of trouble</returns>
        public static int GetNextJunctionIndex(int junctionIndex, int linkingTrackNodeIndex)
        {
            if (linkingTrackNodeIndex <= 0) return 0; // link is not well-defined

            TrackNode linkingTrackNode = trackNodes[linkingTrackNodeIndex];
            if (linkingTrackNode == null) return 0;

            if (junctionIndex == linkingTrackNode.JunctionIndexAtStart())
            {
                return linkingTrackNode.JunctionIndexAtEnd();
            }
            else
            {
                return linkingTrackNode.JunctionIndexAtStart();
            }
        }

        /// <summary>
        /// Get the index of the vectornode leaving this junction again, given the incoming vector node.
        /// This uses only the track database, no trainpath nodes.
        /// When having a choice (in case of facing point), it will take the main node.
        /// </summary>
        /// <param name="junctionIndex">index of junction tracknode</param>
        /// <param name="incomingTvnIndex">index of incoming vector node</param>
        /// <returns>index of the leaving vector node or 0 if none found.</returns>
        public static int GetLeavingTvnIndex(int junctionIndex, int incomingTvnIndex)
        {
            if (junctionIndex <= 0) return 0; // something wrong in database

            TrackNode junctionTrackNode = trackNodes[junctionIndex];
            if (junctionTrackNode == null) {
                return 0;
            }

            if (junctionTrackNode.TrEndNode)
            {
                return 0;
            }
            if (incomingTvnIndex == junctionTrackNode.TrailingTvn())
            {
                return junctionTrackNode.MainTvn();
            }
            return junctionTrackNode.TrailingTvn();
        }
        
    }

}
