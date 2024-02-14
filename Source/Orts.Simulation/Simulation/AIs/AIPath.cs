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
using Orts.Formats.Msts;
using Orts.Formats.OR;
using ORTS.Common;

namespace Orts.Simulation.AIs
{
    public enum AIPathNodeType { Other, Stop, SidingStart, SidingEnd, Uncouple, Reverse, Invalid };

    public class AIPath
    {
        public TrackDB TrackDB;
        public TrackSectionsFile TSectionDat;
#if ACTIVITY_EDITOR
        public ORRouteConfig orRouteConfig { get; protected set; }
#endif
        public AIPathNode FirstNode; // Path starting node
        //public AIPathNode LastVisitedNode; not used anymore --- TODO: Remove?
        public List<AIPathNode> Nodes = new List<AIPathNode>();
        public string pathName; // Name of the path to be able to print it.

        /// <summary>
        /// Creates an AIPath from PAT file information.
        /// First creates all the nodes and then links them together into a main list
        /// with optional parallel siding list.
        /// </summary>
#if ACTIVITY_EDITOR
        public AIPath(TrackDatabaseFile TDB, TrackSectionsFile tsectiondat, string filePath, bool isTimetableMode, ORRouteConfig orRouteConf)
#else
        public AIPath(TDBFile TDB, TSectionDatFile tsectiondat, string filePath)
#endif
        {
            PathFile patFile = new PathFile(filePath);
            pathName = patFile.Name;
            TrackDB = TDB.TrackDB;
            TSectionDat = tsectiondat;
#if ACTIVITY_EDITOR
            orRouteConfig = orRouteConf;
#endif
            bool fatalerror = false;
            if (patFile.TrPathNodes.Count <= 0)
            {
                fatalerror = true;
                Nodes = null;
                return;
            }
            foreach (TrPathNode tpn in patFile.TrPathNodes)
                Nodes.Add(new AIPathNode(tpn, patFile.TrackPDPs[(int)tpn.fromPDP], TrackDB, isTimetableMode));
            FirstNode = Nodes[0];
            //LastVisitedNode = FirstNode;            

            // Connect the various nodes to each other
            for (int i = 0; i < Nodes.Count; i++)
            {
                AIPathNode node = Nodes[i];
                node.Index = i;
                TrPathNode tpn = patFile.TrPathNodes[i];

                // Find TVNindex to next main node.
                if (tpn.HasNextMainNode)
                {
                    node.NextMainNode = Nodes[(int)tpn.nextMainNode];
                    node.NextMainTVNIndex = node.FindTVNIndex(node.NextMainNode, TDB, tsectiondat, i == 0 ? -1 : Nodes[i - 1].NextMainTVNIndex);
                    if (node.JunctionIndex >= 0)
                        node.IsFacingPoint = TestFacingPoint(node.JunctionIndex, node.NextMainTVNIndex);
                    if (node.NextMainTVNIndex < 0)
                    {
                        node.NextMainNode = null;
                        Trace.TraceWarning("Cannot find main track for node {1} in path {0}", filePath, i);
                        fatalerror = true;
                    }
                }

                // Find TVNindex to next siding node
                if (tpn.HasNextSidingNode)
                {
                    node.NextSidingNode = Nodes[(int)tpn.nextSidingNode];
                    node.NextSidingTVNIndex = node.FindTVNIndex(node.NextSidingNode, TDB, tsectiondat, i == 0 ? -1 : Nodes[i - 1].NextMainTVNIndex);
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

            if (fatalerror) Nodes = null; // Invalid path - do not return any nodes
        }

        /// <summary>
        /// constructor out of other path
        /// </summary>
        /// <param name="otherPath"></param>
        public AIPath(AIPath otherPath)
        {
            TrackDB = otherPath.TrackDB; ;
            TSectionDat = otherPath.TSectionDat;
            FirstNode = new AIPathNode(otherPath.FirstNode);
            foreach (AIPathNode otherNode in otherPath.Nodes)
            {
                Nodes.Add(new AIPathNode(otherNode));
            }

            // Set correct node references
            for (int iNode = 0; iNode <= otherPath.Nodes.Count - 1; iNode++)
            {
                AIPathNode otherNode = otherPath.Nodes[iNode];
                if (otherNode.NextMainNode != null)
                {
                    Nodes[iNode].NextMainNode = Nodes[otherNode.NextMainNode.Index];
                }

                if (otherNode.NextSidingNode != null)
                {
                    Nodes[iNode].NextSidingNode = Nodes[otherNode.NextSidingNode.Index];
                }
            }

            if (otherPath.FirstNode.NextMainNode != null)
            {
                FirstNode.NextMainNode = Nodes[otherPath.FirstNode.NextMainNode.Index];
            }
            if (otherPath.FirstNode.NextSidingNode != null)
            {
                FirstNode.NextSidingNode = Nodes[otherPath.FirstNode.NextSidingNode.Index];
            }

            pathName = String.Copy(otherPath.pathName);
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

        // Restore game state
        public AIPath(TrackDatabaseFile TDB, TrackSectionsFile tsectiondat, BinaryReader inf)
        {
            pathName = inf.ReadString();
            TrackDB = TDB.TrackDB;
            TSectionDat = tsectiondat;

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
            return index < 0 || index > Nodes.Count ? null : Nodes[index];
        }

        // Save game state
        public void Save(BinaryWriter outf)
        {
            outf.Write(pathName);
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
                outf.Write(-1);
            else
                outf.Write(node.Index);
        }

        /// <summary>
        /// returns true if the specified vector node is at the facing point end of
        /// the specified juction node, else false.
        /// </summary>
        private bool TestFacingPoint(int junctionIndex, int vectorIndex)
        {
            if (junctionIndex < 0 || vectorIndex < 0)
                return false;
            TrackNode tn = TrackDB.TrackNodes[junctionIndex];
            return tn.TrJunctionNode != null && tn.TrPins[0].Link != vectorIndex;
        }
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
        public AIPathNode(TrPathNode tpn, TrackPDP pdp, TrackDB trackDB, bool isTimetableMode)
        {
            ID = (int)tpn.fromPDP;
            InterpretPathNodeFlags(tpn, pdp, isTimetableMode);

            Location = new WorldLocation(pdp.TileX, pdp.TileZ, pdp.X, pdp.Y, pdp.Z);
            if (pdp.IsJunction)
            {
                JunctionIndex = FindJunctionOrEndIndex(Location, trackDB, true);
            }
        }

        /// <summary>
        /// Constructor from other AIPathNode
        /// </summary>
        /// <param name="otherNode"></param>
        public AIPathNode(AIPathNode otherNode)
        {
            ID = otherNode.ID;
            Index = otherNode.Index;
            Type = otherNode.Type;
            WaitTimeS = otherNode.WaitTimeS;
            WaitUntil = otherNode.WaitUntil;
            NCars = otherNode.NCars;
            NextMainNode = null; // Set after completion of copying to get correct reference
            NextSidingNode = null; // Set after completion of copying to get correct reference
            NextMainTVNIndex = otherNode.NextMainTVNIndex;
            NextSidingTVNIndex = otherNode.NextSidingTVNIndex;
            Location = otherNode.Location;
            JunctionIndex = otherNode.JunctionIndex;
            IsFacingPoint = otherNode.IsFacingPoint;
            IsVisited = otherNode.IsVisited;
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
        private void InterpretPathNodeFlags(TrPathNode tpn, TrackPDP pdp, bool isTimetableMode)
        {
            if ((tpn.pathFlags & 03) == 0) return;
            // Bit 0 and/or bit 1 is set.

            if ((tpn.pathFlags & 01) != 0)
            {
                // If bit 0 is set: reversal
                Type = AIPathNodeType.Reverse;
            }
            else
            {
                // Bit 0 is not set, but bit 1 is set: waiting point
                Type = AIPathNodeType.Stop;
                // <CSComment> Tests showed me that value 9 in pdp is generated  when the waiting point (or also 
                // a path start or end point) are dragged within the path editor of the MSTS activity editor; the points are still valid;
                // however, as a contradictory case of the past has been reported, the check is skipped only when the enhanced compatibility flag is on;
                if (pdp.IsInvalid && isTimetableMode) // Not a valid point
                {
                    Type = AIPathNodeType.Invalid;
                }
            }

            WaitTimeS = (int)((tpn.pathFlags >> 16) & 0xffff); // get the AAAA part.
                                                               // computations for absolute wait times are made within AITrain.cs
                                                               // TODO: Remove?
            /*            if (WaitTimeS >= 30000 && WaitTimeS < 40000)
                        {
                            // real wait time. 
                            // waitTimeS (in decimal notation) = 3HHMM  (hours and minuts)
                            int hour = (WaitTimeS / 100) % 100;
                            int minute = WaitTimeS % 100;
                            WaitUntil = 60 * (minute + 60 * hour);
                            WaitTimeS = 0;
                        }*/
            // computations are made within AITrain.cs
            /*            else if (WaitTimeS >= 40000 && WaitTimeS < 60000)
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
                        } */
        }

        // Restore game state
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
            Location = new WorldLocation();
            Location.TileX = inf.ReadInt32();
            Location.TileZ = inf.ReadInt32();
            Location.Location.X = inf.ReadSingle();
            Location.Location.Y = inf.ReadSingle();
            Location.Location.Z = inf.ReadSingle();
        }

        // Save game state
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
            outf.Write(Location.TileX);
            outf.Write(Location.TileZ);
            outf.Write(Location.Location.X);
            outf.Write(Location.Location.Y);
            outf.Write(Location.Location.Z);
        }

        /// <summary>
        /// Returns the index of the vector node connection this path node to the (given) nextNode.
        /// </summary>
        public int FindTVNIndex(AIPathNode nextNode, TrackDatabaseFile TDB, TrackSectionsFile tsectiondat, int previousNextMainTVNIndex)
        {
            int junctionIndexThis = JunctionIndex;
            int junctionIndexNext = nextNode.JunctionIndex;

            // If this is no junction, try to find the TVN index 
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

            // Both this node and the next node are junctions: find the vector node connecting them.
            var iCand = -1;
            for (int i = 0; i < TDB.TrackDB.TrackNodes.Count(); i++)
            {
                TrackNode tn = TDB.TrackDB.TrackNodes[i];
                if (tn == null || tn.TrVectorNode == null)
                    continue;
                if (tn.TrPins[0].Link == junctionIndexThis && tn.TrPins[1].Link == junctionIndexNext)
                {
                    iCand = i;
                    if (i != previousNextMainTVNIndex) break;
                    Trace.TraceInformation("Managing rocket loop at trackNode {0}", iCand);
                }
                else if (tn.TrPins[1].Link == junctionIndexThis && tn.TrPins[0].Link == junctionIndexNext)
                {
                    iCand = i;
                    if (i != previousNextMainTVNIndex) break;
                    Trace.TraceInformation("Managing rocket loop at trackNode {0}", iCand);
                }
            }
            return iCand;
        }

        /// <summary>
        /// Try to find the tracknode corresponding to the given node's location.
        /// This will raise an exception if it cannot be found
        /// </summary>
        /// <param name="TDB"></param>
        /// <param name="tsectiondat"></param>
        /// <param name="node"></param>
        /// <returns>The track node index that has been found (or an exception)</returns>
        private static int findTrackNodeIndex(TrackDatabaseFile TDB, TrackSectionsFile tsectiondat, AIPathNode node)
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
                if (wantJunctionNode && (tn.TrJunctionNode == null)) continue;
                if (!wantJunctionNode && !tn.TrEndNode) continue;
                if (tn.UiD.TileX != location.TileX || tn.UiD.TileZ != location.TileZ) continue;

                float dx = tn.UiD.X - location.Location.X;
                dx += (tn.UiD.TileX - location.TileX) * 2048;
                float dz = tn.UiD.Z - location.Location.Z;
                dz += (tn.UiD.TileZ - location.TileZ) * 2048;
                float dy = tn.UiD.Y - location.Location.Y;
                float d = (dx * dx) + (dy * dy) + (dz * dz);
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
