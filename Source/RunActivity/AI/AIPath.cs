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
using MSTS;

namespace ORTS
{
    public enum AIPathNodeType { Other, Stop, SidingStart, SidingEnd, Uncouple, Reverse, Invalid };

    public class AIPath
    {
        public TrackDB TrackDB;
        public TSectionDatFile TSectionDat;
        public AIPathNode FirstNode;    // path starting node
        public AIPathNode LastVisitedNode;
        public List<AIPathNode> Nodes = new List<AIPathNode>();

        /// <summary>
        /// Creates an AIPath from PAT file information.
        /// First creates all the nodes and then links them together into a main list
        /// with optional parallel siding list.
        /// </summary>
        public AIPath(PATFile patFile, TDBFile TDB, TSectionDatFile tsectiondat, string filename)
        {
            bool fatalerror = false;

            TrackDB = TDB.TrackDB;
            TSectionDat = tsectiondat;
            foreach (TrPathNode tpn in patFile.TrPathNodes)
                Nodes.Add(new AIPathNode(tpn, patFile.TrackPDPs[(int)tpn.FromPDP], TrackDB));
            FirstNode = Nodes[0];
            for (int i = 0; i < Nodes.Count; i++)
            {
                AIPathNode node = Nodes[i];
                node.Index = i;
                TrPathNode tpn = patFile.TrPathNodes[i];
                if (tpn.NextNode != 0xffffffff)
                {
                    node.NextMainNode = Nodes[(int)tpn.NextNode];
                    node.NextMainTVNIndex = node.FindTVNIndex(node.NextMainNode, TDB, tsectiondat);
                    if (node.JunctionIndex >= 0)
                        node.IsFacingPoint = TestFacingPoint(node.JunctionIndex, node.NextMainTVNIndex);
                    if (node.NextMainTVNIndex < 0)
                    {
                        node.NextMainNode = null;
                        Trace.TraceWarning("Cannot find main track for node {1} in path {0}", filename, i);
                        fatalerror = true;
                    }
                }
                if (tpn.C != 0xffffffff)
                {
                    node.NextSidingNode = Nodes[(int)tpn.C];
                    node.NextSidingTVNIndex = node.FindTVNIndex(node.NextSidingNode, TDB, tsectiondat);
                    if (node.JunctionIndex >= 0)
                        node.IsFacingPoint = TestFacingPoint(node.JunctionIndex, node.NextSidingTVNIndex);
                    if (node.NextSidingTVNIndex < 0)
                    {
                        node.NextSidingNode = null;
                        Trace.TraceWarning("Cannot find siding track for node {1} in path {0}", filename, i);
                        fatalerror = true;
                    }
                }
                if (node.NextMainNode != null && node.NextSidingNode != null)
                    node.Type = AIPathNodeType.SidingStart;
            }
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
            foreach (KeyValuePair<int, AIPathNode> kvp in lastUse)
                kvp.Value.IsLastSwitchUse = true;

            LastVisitedNode = FirstNode;

            if (fatalerror) Nodes = null; // invalid path - do not return any nodes
        }

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
            LastVisitedNode = ReadNode(inf);
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
            WriteNode(outf, LastVisitedNode);
        }
        public void WriteNode(BinaryWriter outf, AIPathNode node)
        {
            if (node == null)
                outf.Write((int)-1);
            else
                outf.Write(node.Index);
        }

        /// <summary>
        /// returns true if the specified vector node is at the facing point end of
        /// the specified juction node, else false.
        /// </summary>
        public bool TestFacingPoint(int junctionIndex, int vectorIndex)
        {
            if (junctionIndex < 0 || vectorIndex < 0)
                return false;
            TrackNode tn = TrackDB.TrackNodes[junctionIndex];
            if (tn.TrJunctionNode == null || tn.TrPins[0].Link == vectorIndex)
                return false;
            return true;
        }

        /// <summary>
        /// finds the first path node after start that refers to the specified track node.
        /// </summary>
        public AIPathNode FindTrackNode(AIPathNode start, int trackNodeIndex)
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

        public AIPathNode PrevNode(AIPathNode node)
        {
            AIPathNode prev = null;
            AIPathNode cur = FirstNode;
            while (cur != null && cur != node)
            {
                prev = cur;
                cur = cur.NextSidingNode == null ? cur.NextMainNode : cur.NextSidingNode;
            }
            if (cur == node)
                return prev;
            else
                return null;
        }

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
    }

    public class AIPathNode
    {
        public int ID;
        public int Index;
        public AIPathNodeType Type = AIPathNodeType.Other;
        public int WaitTimeS = 0;   // number of seconds to wait after stopping at this node
        public int WaitUntil = 0;   // clock time to wait until if not zero
        public int NCars = 0;       // number of cars to uncouple, negative means keep rear
        public AIPathNode NextMainNode = null;      // next path node on main path
        public AIPathNode NextSidingNode = null;    // next path node on siding path
        public int NextMainTVNIndex = -1;   // index of main vector node leaving this path node
        public int NextSidingTVNIndex = -1; // index of siding vector node leaving this path node
        public WorldLocation Location;      // coordinates for this path node
        public int JunctionIndex = -1;      // index of junction node, -1 if none
        public bool IsFacingPoint = false;// true if this node entered from the facing point end
        public bool IsLastSwitchUse = false;//true if this node is last to touch a switch
        public bool IsVisited = false;     // true if the train has visited this node

        /// <summary>
        /// Creates a single AIPathNode and initializes everything that do not depend on other nodes.
        /// The AIPath constructor will initialize the rest.
        /// </summary>
        public AIPathNode(TrPathNode tpn, TrackPDP pdp, TrackDB trackDB)
        {
            ID = (int)tpn.FromPDP;
            if ((tpn.A & 03) != 0)
            {
                if ((tpn.A & 01) != 0)
                    Type = AIPathNodeType.Reverse;

		else
                {
                    Type = AIPathNodeType.Stop;
                    if (pdp.B == 9) // not a valid point
                    {
                        Type = AIPathNodeType.Invalid;
                    }
                }

                WaitTimeS = (int)((tpn.A >> 16) & 0xffff);
                if (WaitTimeS >= 30000 && WaitTimeS < 40000)
                {
                    int hour = (WaitTimeS / 100) % 100;
                    int minute = WaitTimeS % 100;
                    WaitUntil = 60 * (minute + 60 * hour);
                    WaitTimeS = 0;
                }
                else if (WaitTimeS >= 40000 && WaitTimeS < 60000)
                {
                    NCars = (WaitTimeS / 100) % 100;
                    if (WaitTimeS >= 50000)
                        NCars = -NCars;
                    WaitTimeS %= 100;
                    if (Type == AIPathNodeType.Stop)
                        Type = AIPathNodeType.Uncouple;
                }
                else if (WaitTimeS >= 60000)  // this is old and should be removed/reused
                {
                    WaitTimeS %= 1000;
                }
            }
            Location = new WorldLocation(pdp.TileX, pdp.TileZ, pdp.X, pdp.Y, pdp.Z);
            if (pdp.A == 2)
            {
                float best = 1e10f;
                for (int j = 0; j < trackDB.TrackNodes.Count(); j++)
                {
                    TrackNode tn = trackDB.TrackNodes[j];
                    if (tn != null && tn.TrJunctionNode != null && tn.UiD.WorldTileX == pdp.TileX && tn.UiD.WorldTileZ == pdp.TileZ)
                    {
                        float dx = tn.UiD.X - pdp.X;
                        dx += (tn.UiD.TileX - pdp.TileX) * 2048;
                        float dz = tn.UiD.Z - pdp.Z;
                        dz += (tn.UiD.TileZ - pdp.TileZ) * 2048;
                        float dy = tn.UiD.Y - pdp.Y;
                        float d = dx * dx + dy * dy + dz * dz;
                        if (best > d)
                        {
                            JunctionIndex = j;
                            best = d;
                        }
                    }
                }
            }
        }

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
            IsLastSwitchUse = inf.ReadBoolean();
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
            outf.Write(IsLastSwitchUse);
            outf.Write(Location.TileX);
            outf.Write(Location.TileZ);
            outf.Write(Location.Location.X);
            outf.Write(Location.Location.Y);
            outf.Write(Location.Location.Z);
        }

        /// <summary>
        /// Returns the index of the vector node connection this path node to another.
        /// </summary>
        public int FindTVNIndex(AIPathNode nextNode, TDBFile TDB, TSectionDatFile tsectiondat)
        {
            int i1 = JunctionIndex;
            int i2 = nextNode.JunctionIndex;
            if (i1 < 0)
            {
                try
                {
                    // TODO: Static method to look up track nodes?
                    Traveller traveller = new Traveller(tsectiondat, TDB.TrackDB.TrackNodes, Location.TileX, Location.TileZ, Location.Location.X, Location.Location.Z);
                    return traveller.TrackNodeIndex;
                }
                catch
                {
                    i1 = FindEndIndex(Location, TDB, tsectiondat);
                }
            }
            if (i2 < 0)
            {
                try
                {
                    // TODO: Static method to look up track nodes?
                    Traveller traveller = new Traveller(tsectiondat, TDB.TrackDB.TrackNodes, nextNode.Location.TileX, nextNode.Location.TileZ, nextNode.Location.Location.X, nextNode.Location.Location.Z);
                    return traveller.TrackNodeIndex;
                }
                catch
                {
                    i2 = FindEndIndex(nextNode.Location, TDB, tsectiondat);
                }
            }
            for (int i = 0; i < TDB.TrackDB.TrackNodes.Count(); i++)
            {
                TrackNode tn = TDB.TrackDB.TrackNodes[i];
                if (tn == null || tn.TrVectorNode == null)
                    continue;
                if (tn.TrPins[0].Link == i1 && tn.TrPins[1].Link == i2)
                    return i;
                if (tn.TrPins[1].Link == i1 && tn.TrPins[0].Link == i2)
                    return i;
            }
            return -1;
        }
        public int FindEndIndex(WorldLocation location, TDBFile TDB, TSectionDatFile tsectiondat)
        {
            int bestIndex = -1;
            float best = 1e10f;
            for (int j = 0; j < TDB.TrackDB.TrackNodes.Count(); j++)
            {
                TrackNode tn = TDB.TrackDB.TrackNodes[j];
                if (tn != null && tn.TrEndNode && tn.UiD.WorldTileX == location.TileX && tn.UiD.WorldTileZ == location.TileZ)
                {
                    float dx = tn.UiD.X - location.Location.X;
                    dx += (tn.UiD.TileX - location.TileX) * 2048;
                    float dz = tn.UiD.Z - location.Location.Z;
                    dz += (tn.UiD.TileZ - location.TileZ) * 2048;
                    float dy = tn.UiD.Y - location.Location.Y;
                    float d = dx * dx + dy * dy + dz * dz;
                    if (best > d)
                    {
                        bestIndex = j;
                        best = d;
                    }
                }
            }
            return bestIndex;
        }
    }
}
