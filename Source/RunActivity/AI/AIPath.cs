// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

/* AIPath
 * 
 * Contains a processed version of the MSTS PAT file.
 * The processing saves information needed for AI train dispatching and to align switches.
 * Could this be used for player trains also?
 * 
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MSTS.Formats;
using ORTS.Common;

namespace ORTS
{
    public enum AIPathNodeType { Other, Stop, SidingStart, SidingEnd, Uncouple, Reverse, Invalid };

    public class AIPath
    {
        public TrackDB TrackDB;
        public TSectionDatFile TSectionDat;
        public AIPathNode FirstNode;    // path starting node
        //public AIPathNode LastVisitedNode; not used anymore
        public List<AIPathNode> Nodes = new List<AIPathNode>();
        public string pathName; //name of the path to be able to print it.

        /// <summary>
        /// Creates an AIPath from PAT file information.
        /// First creates all the nodes and then links them together into a main list
        /// with optional parallel siding list.
        /// </summary>
        public AIPath(TDBFile TDB, TSectionDatFile tsectiondat, string filePath)
        {
            PATFile patFile = new PATFile(filePath);
            pathName = patFile.Name;
            TrackDB = TDB.TrackDB;
            TSectionDat = tsectiondat;

            foreach (TrPathNode tpn in patFile.TrPathNodes)
                Nodes.Add(new AIPathNode(tpn, patFile.TrackPDPs[(int)tpn.fromPDP], TrackDB));
            FirstNode = Nodes[0];
            //LastVisitedNode = FirstNode;            
            
            // Connect the various nodes to each other
           bool fatalerror = false;
            for (int i = 0; i < Nodes.Count; i++)
            {
                AIPathNode node = Nodes[i];
                node.Index = i;
                TrPathNode tpn = patFile.TrPathNodes[i];

                // find TVNindex to next main node.
                if (tpn.HasNextMainNode)
                {
                    node.NextMainNode = Nodes[(int)tpn.nextMainNode];
                    node.NextMainTVNIndex = node.FindTVNIndex(node.NextMainNode, TDB, tsectiondat);
                    if (node.JunctionIndex >= 0)
                        node.IsFacingPoint = TestFacingPoint(node.JunctionIndex, node.NextMainTVNIndex);
                    if (node.NextMainTVNIndex < 0)
                    {
                        node.NextMainNode = null;
                        Trace.TraceWarning("Cannot find main track for node {1} in path {0}", filePath, i);
                        fatalerror = true;
                    }
                }

                // find TVNindex to next siding node
                if (tpn.HasNextSidingNode)
                {
                    node.NextSidingNode = Nodes[(int)tpn.nextSidingNode];
                    node.NextSidingTVNIndex = node.FindTVNIndex(node.NextSidingNode, TDB, tsectiondat);
                    if (node.JunctionIndex >= 0)
                        node.IsFacingPoint = TestFacingPoint(node.JunctionIndex, node.NextSidingTVNIndex);
                    if (node.NextSidingTVNIndex < 0)
                    {
                        node.NextSidingNode = null;
                        Trace.TraceWarning("Cannot find siding track for node {1} in path {0}", filePath, i);
                        fatalerror = true;
                    }
                }

                if (node.NextMainNode != null && node.NextSidingNode != null)
                    node.Type = AIPathNodeType.SidingStart;
            }

            FindSidingEnds();

            if (fatalerror) Nodes = null; // invalid path - do not return any nodes
        }

        /// <summary>
        /// Find all nodes that are the end of a siding (so where main path and siding path come together again)
        /// </summary>
        private void FindSidingEnds()
        {
            Dictionary<int, AIPathNode> lastUse = new Dictionary<int, AIPathNode>();
            for (AIPathNode node1 = FirstNode; node1 != null; node1 = node1.NextMainNode)
            {
                if (node1.JunctionIndex >= 0)
                    lastUse[node1.JunctionIndex] = node1;
                AIPathNode node2 = node1.NextSidingNode;
                while (node2 != null && node2.NextSidingNode != null)
                {
                    if (node2.JunctionIndex >= 0)
                        lastUse[node2.JunctionIndex] = node2;
                    node2 = node2.NextSidingNode;
                }
                if (node2 != null)
                    node2.Type = AIPathNodeType.SidingEnd;
            }
            //foreach (KeyValuePair<int, AIPathNode> kvp in lastUse)
            //    kvp.Value.IsLastSwitchUse = true;
        }

        /* Not used. But not removed from file in case it is needed later. Note pathName is currently not saved!
        // restore game state
        public AIPath(BinaryReader inf)
        {
            int n = inf.ReadInt32();
            for (int i = 0; i < n; i++)
                Nodes.Add(new AIPathNode(inf));
            for (int i = 0; i < n; i++)
            {
                Nodes[i].NextMainNode = ReadNode(inf);
                Nodes[i].NextSidingNode = ReadNode(inf);
            }
            FirstNode = Nodes[0];
            //LastVisitedNode = ReadNode(inf);
        }
        public AIPathNode ReadNode(BinaryReader inf)
        {
            int index = inf.ReadInt32();
            if (index < 0 || index > Nodes.Count)
                return null;
            else
                return Nodes[index];
        }

        // save game state
        public void Save(BinaryWriter outf)
        {
            outf.Write(Nodes.Count);
            for (int i = 0; i < Nodes.Count; i++)
                Nodes[i].Save(outf);
            for (int i = 0; i < Nodes.Count; i++)
            {
                WriteNode(outf, Nodes[i].NextMainNode);
                WriteNode(outf, Nodes[i].NextSidingNode);
            }
            //WriteNode(outf, LastVisitedNode);
        }
        public static void WriteNode(BinaryWriter outf, AIPathNode node)
        {
            if (node == null)
                outf.Write((int)-1);
            else
                outf.Write(node.Index);
        }
        */

        /// <summary>
        /// returns true if the specified vector node is at the facing point end of
        /// the specified juction node, else false.
        /// </summary>
        private bool TestFacingPoint(int junctionIndex, int vectorIndex)
        {
            if (junctionIndex < 0 || vectorIndex < 0)
                return false;
            TrackNode tn = TrackDB.TrackNodes[junctionIndex];
            if (tn.TrJunctionNode == null || tn.TrPins[0].Link == vectorIndex)
                return false;
            return true;
        }

#if !NEW_SIGNALLING
        /// <summary>
        /// finds the first path node after start that refers to the specified track node.
        /// </summary>
        public static AIPathNode FindTrackNode(AIPathNode start, int trackNodeIndex)
        {
            for (AIPathNode node = start; node != null; node = node.NextMainNode)
            {
                if (node.NextMainTVNIndex == trackNodeIndex || node.NextSidingTVNIndex == trackNodeIndex)
                    return node;
                for (AIPathNode node1 = node.NextSidingNode; node1 != null; node1 = node1.NextSidingNode)
                    if (node1.NextMainTVNIndex == trackNodeIndex || node1.NextSidingTVNIndex == trackNodeIndex)
                        return node1;
            }
            return null;
        }
#endif

        /* not used.
        // TODO. This routine is also present in Sound.cs. That routine should perhaps be replaced by this one
        public AIPathNode PrevNode(AIPathNode node)
        {
            AIPathNode previousNode = null;
            AIPathNode currentNode = FirstNode;
            while (currentNode != null && currentNode != node)
            {
                previousNode = currentNode;
                currentNode = currentNode.NextSidingNode == null ? currentNode.NextMainNode : currentNode.NextSidingNode;
            }
            if (currentNode == node)
                return previousNode;
            else
                return null;
        }
        */

        /* not used
        public void SetVisitedNode(AIPathNode node, int curNodeIndex)
        {
            if (LastVisitedNode == node)
                LastVisitedNode.IsVisited = true;

            LastVisitedNode = FindTrackNode(LastVisitedNode, curNodeIndex);
            
            if (LastVisitedNode != null)
            {
                if (LastVisitedNode.NextMainNode != null &&
                    LastVisitedNode.NextMainTVNIndex == LastVisitedNode.NextMainNode.NextMainTVNIndex)
                    LastVisitedNode = LastVisitedNode.NextMainNode;
                else if (LastVisitedNode.NextSidingNode != null &&
                    LastVisitedNode.NextSidingTVNIndex == LastVisitedNode.NextSidingNode.NextSidingTVNIndex)
                    LastVisitedNode = LastVisitedNode.NextSidingNode;
            }
        }
        */
    }

    public class AIPathNode
    {
        public int ID;
        public int Index;
        public AIPathNodeType Type = AIPathNodeType.Other;
        public int WaitTimeS;               // number of seconds to wait after stopping at this node
        public int WaitUntil;               // clock time to wait until if not zero
        public int NCars;                   // number of cars to uncouple, negative means keep rear
        public AIPathNode NextMainNode;     // next path node on main path
        public AIPathNode NextSidingNode;   // next path node on siding path
        public int NextMainTVNIndex = -1;   // index of main vector node leaving this path node
        public int NextSidingTVNIndex = -1; // index of siding vector node leaving this path node
        public WorldLocation Location;      // coordinates for this path node
        public int JunctionIndex = -1;      // index of junction node, -1 if none
        public bool IsFacingPoint;          // true if this node entered from the facing point end
        //public bool IsLastSwitchUse;        //true if this node is last to touch a switch
        public bool IsVisited;              // true if the train has visited this node

        /// <summary>
        /// Creates a single AIPathNode and initializes everything that do not depend on other nodes.
        /// The AIPath constructor will initialize the rest.
        /// </summary>
        public AIPathNode(TrPathNode tpn, TrackPDP pdp, TrackDB trackDB)
        {
            ID = (int)tpn.fromPDP;
            InterpretPathNodeFlags(tpn, pdp);

            Location = new WorldLocation(pdp.TileX, pdp.TileZ, pdp.X, pdp.Y, pdp.Z);
            if (pdp.IsJunction)
            {
                JunctionIndex = FindJunctionOrEndIndex(Location, trackDB, true);
            }
        }

        // Possible interpretation (as found on internet, by krausyao)
        // TrPathNode ( AAAABBBB mainIdx passingIdx pdpIdx )
        // AAAA wait time seconds in hexidecimal
        // BBBB (Also hexidecimal, so 16 bits)
        // Bit 0 - connected pdp-entry references a reversal-point (1/x1)
        // Bit 1 - waiting point (2/x2)
        // Bit 2 - intermediate point between switches (4/x4)
        // Bit 3 - 'other exit' is used (8/x8)
        // Bit 4 - 'optional Route' active (16/x10)
        //
        // But the interpretation below is a bit more complicated.
        // TODO. Since this interpretation belongs to the PATfile itself, 
        // in principle it would be more logical to have it in PATfile.cs. But this leads to too much code duplication
        private void InterpretPathNodeFlags(TrPathNode tpn, TrackPDP pdp)
        {
            if ((tpn.pathFlags & 03) == 0) return;
            // bit 0 and/or bit 1 is set.

            if ((tpn.pathFlags & 01) != 0)
            {
                // if bit 0 is set: reversal
                Type = AIPathNodeType.Reverse;
            }
            else
            {
                // bit 0 is not set, but bit 1 is set:waiting point
                Type = AIPathNodeType.Stop;
                if (pdp.IsInvalid) // not a valid point
                {
                    Type = AIPathNodeType.Invalid;
                }
            }

            WaitTimeS = (int)((tpn.pathFlags >> 16) & 0xffff); // get the AAAA part.
            if (WaitTimeS >= 30000 && WaitTimeS < 40000)
            {
                // real wait time. 
                // waitTimeS (in decimal notation) = 3HHMM  (hours and minuts)
                int hour = (WaitTimeS / 100) % 100;
                int minute = WaitTimeS % 100;
                WaitUntil = 60 * (minute + 60 * hour);
                WaitTimeS = 0;
            }
            else if (WaitTimeS >= 40000 && WaitTimeS < 60000)
            {
                // Uncouple if a wait=stop point
                // waitTimeS (in decimal notation) = 4NNSS (uncouple NN cars, wait SS seconds)
                //                                or 5NNSS (uncouple NN cars, keep rear, wait SS seconds)
                NCars = (WaitTimeS / 100) % 100;
                if (WaitTimeS >= 50000)
                    NCars = -NCars;
                WaitTimeS %= 100;
                if (Type == AIPathNodeType.Stop)
                    Type = AIPathNodeType.Uncouple;
            }
            else if (WaitTimeS >= 60000)  // this is old and should be removed/reused
            {
                // waitTimes = 6xSSS  with waitTime SSS seconds.
                WaitTimeS %= 1000;
            }

        }


        /* not used, but not removed in case it is needed later
        // restore game state
        public AIPathNode(BinaryReader inf)
        {
            ID = inf.ReadInt32();
            Index = inf.ReadInt32();
            Type = (AIPathNodeType)inf.ReadInt32();
            WaitTimeS = inf.ReadInt32();
            WaitUntil = inf.ReadInt32();
            NCars = inf.ReadInt32();
            NextMainTVNIndex = inf.ReadInt32();
            NextSidingTVNIndex = inf.ReadInt32();
            JunctionIndex = inf.ReadInt32();
            IsFacingPoint = inf.ReadBoolean();
            //IsLastSwitchUse = inf.ReadBoolean();
            Location = new WorldLocation();
            Location.TileX = inf.ReadInt32();
            Location.TileZ = inf.ReadInt32();
            Location.Location.X = inf.ReadSingle();
            Location.Location.Y = inf.ReadSingle();
            Location.Location.Z = inf.ReadSingle();
        }

        // save game state
        public void Save(BinaryWriter outf)
        {
            outf.Write(ID);
            outf.Write(Index);
            outf.Write((int)Type);
            outf.Write(WaitTimeS);
            outf.Write(WaitUntil);
            outf.Write(NCars);
            outf.Write(NextMainTVNIndex);
            outf.Write(NextSidingTVNIndex);
            outf.Write(JunctionIndex);
            outf.Write(IsFacingPoint);
            //outf.Write(IsLastSwitchUse);
            outf.Write(Location.TileX);
            outf.Write(Location.TileZ);
            outf.Write(Location.Location.X);
            outf.Write(Location.Location.Y);
            outf.Write(Location.Location.Z);
        }
        */

        /// <summary>
        /// Returns the index of the vector node connection this path node to the (given) nextNode.
        /// </summary>
        public int FindTVNIndex(AIPathNode nextNode, TDBFile TDB, TSectionDatFile tsectiondat)
        {
            int junctionIndexThis = JunctionIndex;
            int junctionIndexNext = nextNode.JunctionIndex;

            // if this is no junction, try to find the TVN index 
            if (junctionIndexThis < 0)
            {
                try
                {
                    return findTrackNodeIndex(TDB, tsectiondat, this);
                }
                catch
                {
                    junctionIndexThis = FindJunctionOrEndIndex(this.Location, TDB.TrackDB, false);
                }
            }

            // this is a junction; if the next node is no junction, try that one.
            if (junctionIndexNext < 0)
            {
                try
                {
                    return findTrackNodeIndex(TDB, tsectiondat, nextNode);
                }
                catch
                {
                    junctionIndexNext = FindJunctionOrEndIndex(nextNode.Location, TDB.TrackDB, false);
                }
            }

            //both this node and the next node are junctions: find the vector node connecting them.
            for (int i = 0; i < TDB.TrackDB.TrackNodes.Count(); i++)
            {
                TrackNode tn = TDB.TrackDB.TrackNodes[i];
                if (tn == null || tn.TrVectorNode == null)
                    continue;
                if (tn.TrPins[0].Link == junctionIndexThis && tn.TrPins[1].Link == junctionIndexNext)
                    return i;
                if (tn.TrPins[1].Link == junctionIndexThis && tn.TrPins[0].Link == junctionIndexNext)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Try to find the tracknode corresponding to the given node's location.
        /// This will raise an exception if it cannot be found
        /// </summary>
        /// <param name="TDB"></param>
        /// <param name="tsectiondat"></param>
        /// <param name="node"></param>
        /// <returns>The track node index that has been found (or an exception)</returns>
        private static int findTrackNodeIndex(TDBFile TDB, TSectionDatFile tsectiondat, AIPathNode node)
        {
            Traveller traveller = new Traveller(tsectiondat, TDB.TrackDB.TrackNodes, node.Location);
            return traveller.TrackNodeIndex;
        }

        /// <summary>
        /// Find the junctionNode or endNode closest to the given location
        /// </summary>
        /// <param name="location">Location for which we want to find the node</param>
        /// <param name="trackDB">track database containing the trackNodes</param>
        /// <param name="wantJunctionNode">true if a junctionNode is wanted, false for a endNode</param>
        /// <returns>tracknode index of the closes node</returns>
        public static int FindJunctionOrEndIndex(WorldLocation location, TrackDB trackDB, bool wantJunctionNode)
        {
            int bestIndex = -1;
            float bestDistance2 = 1e10f;
            for (int j = 0; j < trackDB.TrackNodes.Count(); j++)
            {
                TrackNode tn = trackDB.TrackNodes[j];
                if (tn == null) continue;
                if ( wantJunctionNode && (tn.TrJunctionNode==null)) continue;
                if (!wantJunctionNode && !tn.TrEndNode) continue;
                if (tn.UiD.WorldTileX != location.TileX || tn.UiD.WorldTileZ != location.TileZ) continue;

                float dx = tn.UiD.X - location.Location.X;
                dx += (tn.UiD.TileX - location.TileX) * 2048;
                float dz = tn.UiD.Z - location.Location.Z;
                dz += (tn.UiD.TileZ - location.TileZ) * 2048;
                float dy = tn.UiD.Y - location.Location.Y;
                float d = dx * dx + dy * dy + dz * dz;
                if (bestDistance2 > d)
                {
                    bestIndex = j;
                    bestDistance2 = d;
                }

            }
            return bestIndex;
        }
    }
}
