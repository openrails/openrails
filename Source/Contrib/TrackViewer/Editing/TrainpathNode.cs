// COPYRIGHT 2014 by the Open Rails project.
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
//
// This class is a copy of AIPathNode in AIPath.cs with additional methods.  But because of editing we cannot use subclassing
//  And changing AIPath.cs is a bit overdone, probably
//
// This class contains the definitions for nodes in a path.
// A path will be a (double) linked list of nodes, possibly with extra links for passing paths.
// The nodes are defined here. 
// They contain basic information like location and type, track index to next main/siding node.
// They contain a number of items related to drawing (like trackAngle, but some more for vectorNodes)
// They contain extra information to simplify editing (like HasSidingPath
//
// two types of nodes exist
//  junction nodes: those nodes that are on a junction. They contain junction Index as an extra field
//  vector nodes: Nodes not on a junction but somewhere on a vector node
//      either they are simple nodes needed for disambiguity
//      or they are special nodes like start, end, wait, uncouple, reverse nodes.
//      Vector nodes need more details on where exactly they are related to the track, and of course they need extra details
//          related to whatever special node they are.
//
// various constructors are available
//      related to whether the node is created from a .pat file, or created dynamically during edit operations.
// Because the path is a double linked list, to prevent issues with garbage collection, an Unlink method is provided that removes the lilnks.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MSTS.Formats;
using ORTS.Common;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// Enumerate the various types of nodes that are available
    /// </summary>
    public enum TrainpathNodeType { 
        /// <summary>Node is the start node </summary>
        Start,
        /// <summary>Node is the end node (and not just the last node) </summary>
        End,
        /// <summary>Node is a regular node </summary>
        Other,
        /// <summary>Node is a wait/stop node</summary>
        Stop,
        /// <summary>Node is a junction node at the start of a siding </summary>
        SidingStart,
        /// <summary>Node is a junction node at the end of a siding</summary>
        SidingEnd,
        /// <summary>Node is a (un)couple point</summary>
        Uncouple,
        /// <summary>Node is a reversal node</summary>
        Reverse,
        /// <summary>Node is not well-defined in .pat file</summary>
        Invalid //todo functionality should be replaced by IsBroken.
    };

    /// <summary>
    /// base class for all nodes in a trainpath (as defined by MSTS .pat file).
    /// The class is abstract because we only allow either junction of vector nodes
    /// </summary>
    public abstract class TrainpathNode
    {
        /// <summary> World location of the node, coming directly from .pat file </summary>
        public WorldLocation location { get; set; }

        /// <summary> Stores the type of node (see TrainPathNode)</summary>
        public TrainpathNodeType Type { get; set; }
        /// <summary> True if the node is broken, meaning that its location can no longer be found in the track data base
        /// By having it independent of the NodeType, we can keep the kind of node that was intended, even if it is currently not in the right place.</summary>
        public bool IsBroken { get; protected set; }

        // From simple linking:
        /// <summary>Next path node on main path</summary>
        public TrainpathNode NextMainNode { get; set; }
        /// <summary>Next path node on siding path</summary>
        public TrainpathNode NextSidingNode { get; set; }
        /// <summary>Previous path node on main path (unless it is on a siding path</summary>
        public TrainpathNode PrevNode { get; set; }
        /// <summary>Is there, next to the track to the NextMainNode, also a parallel Siding path?</summary>
        public bool HasSidingPath { get; set; }
        
        //To find these, both the current node and the next node need to be known.
        /// <summary>Index of main vector node leaving this path node</summary>
        public int NextMainTVNIndex { get; set; }
        /// <summary>Index of siding vector node leaving this path node</summary>
        public int NextSidingTVNIndex { get; set; }

        /// <summary>Angle that denotes the 2D direction of the path in radians</summary>
        public float TrackAngle { get; protected set; }

        /// <summary>Reference to the track database to be able to search it</summary>
        protected TrackDB trackDB;
        /// <summary>Reference to the track section data to be able to search it</summary>
        protected TSectionDatFile tsectionDat;

        /// <summary>
        /// Sort of constructor. But it creates the right sub-class
        /// </summary>
        /// <returns>A sub-class object properly initialized</returns>
        public static TrainpathNode createPathNode(TrPathNode tpn, TrackPDP pdp, TrackDB trackDB, TSectionDatFile tsectionDat)
        {
            if (pdp.IsJunction) {
                return new TrainpathJunctionNode(tpn, pdp, trackDB, tsectionDat);
            }
            else {
                return new TrainpathVectorNode(tpn, pdp, trackDB, tsectionDat);
            }
            
        }

        /// <summary>
        /// basic constructor, in case node is not created from PAT file, and only some parts are needed
        /// </summary>
        protected TrainpathNode(TrackDB trackDB, TSectionDatFile tsectionDat)
        {
            this.trackDB = trackDB;
            this.tsectionDat = tsectionDat;
            HasSidingPath = false;
            NextMainTVNIndex = -1;
            NextSidingTVNIndex = -1;
            Type = TrainpathNodeType.Other;
        }

        /// <summary>
        /// constructor, in case node is not created from PAT file.
        /// </summary>
        protected TrainpathNode(TrainpathNode otherNode)
            :this(otherNode.trackDB, otherNode.tsectionDat)
        {
        }


        /// <summary>
        /// Creates a single trainpathNode and initializes everything that do not depend on other nodes.
        /// The trainpath constructor will initialize the rest.
        /// </summary>
        protected TrainpathNode(TrackPDP pdp, TrackDB trackDB, TSectionDatFile tsectionDat)
            :this(trackDB, tsectionDat)
        {
            location = new WorldLocation(pdp.TileX, pdp.TileZ, pdp.X, pdp.Y, pdp.Z);
        }

        /// <summary>
        /// Make a shallow copy with all links to other nodes set to null;
        /// </summary>
        /// <returns>a copy of this node</returns>
        public abstract TrainpathNode ShallowCopyNoLinks();

        /// <summary>
        /// Returns the index of the vector node connection this path node to the (given) nextNode.
        /// </summary>
        public int FindTVNIndex(TrainpathNode nextNode)
        {
            // if this node is on  a vector tracknode:
            if (  this   is TrainpathVectorNode) return (    this as TrainpathVectorNode).TVNIndex;
            if (nextNode is TrainpathVectorNode) return (nextNode as TrainpathVectorNode).TVNIndex;

            //both this node and the next node are junctions: find the vector node connecting them.
            int thisJunctionIndex = (  this   as TrainpathJunctionNode).junctionIndex;
            int nextJunctionIndex = (nextNode as TrainpathJunctionNode).junctionIndex;
            for (int i = 0; i < trackDB.TrackNodes.Count(); i++)
            {
                TrackNode tn = trackDB.TrackNodes[i];
                if (tn == null || tn.TrVectorNode == null)
                    continue;
                if (  (tn.JunctionIndexAtStart() == thisJunctionIndex && tn.JunctionIndexAtEnd()   == nextJunctionIndex)
                   || (tn.JunctionIndexAtEnd()   == thisJunctionIndex && tn.JunctionIndexAtStart() == nextJunctionIndex))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Determine the orientation of the current node, by using the previousNode as well as the TVN that links the
        /// previous node with this node.
        /// </summary>
        /// <param name="previousNode">previouse node</param>
        /// <param name="linkingTVNIndex">the index of the Track Vector Node linking the previous node to this node</param>
        public abstract void determineOrientation(TrainpathNode previousNode, int linkingTVNIndex);

        /// <summary>
        /// Get the 'flags' of the current node, describing to MSTS what kind of node it is, 
        /// as well as some details for specific nodes like wait and (un)couple point
        /// </summary>
        /// <returns>string containing 8-digit hexedecimal coded flags</returns>
        public virtual string GetFlags()
        {
            return "00000000";
        }

    }

    /// <summary>
    /// Class to describe junction nodes that are part of a train path.
    /// </summary>
    public class TrainpathJunctionNode : TrainpathNode
    {
        /// <summary>index of junction node (in the track data base</summary>
        public int junctionIndex { get; set; }
        /// <summary>true if this node entered from the facing point end</summary>
        public bool IsFacingPoint { get; set; }

        /// <summary>
        /// Basic constructor using another node for the trackDB and tsectionDB
        /// </summary>
        /// <param name="otherNode">Just another node that already has trackDB and tsectionDB set</param>
        public TrainpathJunctionNode(TrainpathNode otherNode)
            :base(otherNode)
        {
        }

        /// <summary>
        /// Constructor based on the data given in the .pat file
        /// </summary>
        /// <param name="tpn">TrPathNode as defined in the .pat file</param>
        /// <param name="pdp">Corresponding PDP in the .patfile</param>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        public TrainpathJunctionNode(TrPathNode tpn, TrackPDP pdp, TrackDB trackDB, TSectionDatFile tsectionDat) 
            : base(pdp, trackDB, tsectionDat)
        {
            junctionIndex = FindJunctionOrEndIndex(true);
        }

        /// <summary>
        /// Make a shallow copy with all links to other nodes set to null;
        /// </summary>
        /// <returns>a copy of this node</returns>
        public override TrainpathNode ShallowCopyNoLinks()
        {
            TrainpathJunctionNode newNode = (TrainpathJunctionNode)this.MemberwiseClone();
            newNode.NextMainNode = null;
            newNode.NextSidingNode = null;
            newNode.PrevNode = null;
            return newNode;
        }

        /// <summary>
        /// Find the activeNodeAsJunction or endNode closest to the given location
        /// </summary>
        /// <param name="wantJunctionNode">true if a activeNodeAsJunction is wanted, false for a endNode</param>
        /// <returns>tracknode index of the closes node</returns>
        public int FindJunctionOrEndIndex(bool wantJunctionNode)
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
                float d = dx * dx + dy * dy + dz * dz;
                if (bestDistance2 > d)
                {
                    bestIndex = j;
                    bestDistance2 = d;
                }

            }
            return bestIndex;
        }

        /// <summary>
        /// returns true if the specified vector node is the trailing end of
        /// the specified juction node, else false.
        /// </summary>
        public void SetFacingPoint(int vectorIndex)
        {
            if (vectorIndex < 0)
            {
                IsFacingPoint = false;
            }
            else
            {
                TrackNode tn = trackDB.TrackNodes[junctionIndex];
                if (tn.TrJunctionNode == null || tn.TrailingTVN() == vectorIndex)
                {
                    IsFacingPoint = false;
                }
                else
                {
                    IsFacingPoint = true;
                }
            }
            
        }

        /// <summary>
        /// Determine the orientation of the current node, by using the previousNode as well as the TVN that links the
        /// previous node with this node.
        /// </summary>
        /// <param name="previousNode">previouse node</param>
        /// <param name="linkingTVNIndex">the index of the Track Vector Node linking the previous node to this node</param>
        public override void determineOrientation(TrainpathNode previousNode, int linkingTVNIndex)
        {
            Traveller traveller = placeTravellerAfterJunction(linkingTVNIndex);
            if (traveller != null)
            {
                traveller.ReverseDirection(); // the TVN is from the previous node, so backwards. Therefore:reverse
                TrackAngle = traveller.RotY;
            }
            else
            {
                TrackAngle = 0; // just set it to some default
                IsBroken = true;
            }
        }

        /// <summary>
        /// Place a traveller at the junction node location, but on a track leaving it.
        /// </summary>
        /// <param name="linkingTVNIndex">The index of the track leaving it</param>
        /// <returns>The traveller, with direction leaving this node.</returns>
        public Traveller placeTravellerAfterJunction(int linkingTVNIndex)
        {
            // it is a junction. Place a traveller onto the tracknode and find the orientation from it.
            try
            {   //for broken paths the tracknode doesn't exit or the traveller cannot be placed.
                TrackNode linkingTN = trackDB.TrackNodes[linkingTVNIndex];
                Traveller traveller = new Traveller(tsectionDat, trackDB.TrackNodes, linkingTN,
                                            location.TileX, location.TileZ, location.Location.X, location.Location.Z, Traveller.TravellerDirection.Forward);
                if (linkingTN.JunctionIndexAtStart() != this.junctionIndex)
                {   // the tracknode is oriented in the other direction.
                    traveller.ReverseDirection();
                }
                return traveller;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Node as part of a train path that is not on a junction but on a vector node. It contains all the relevant extra data
    /// like where exactly on the vector node it is. It also contains all relevant extra data related to the type it is 
    /// (e.g. for wait points and (un)couple points
    /// </summary>
    public class TrainpathVectorNode : TrainpathNode
    {
        /// <summary>Angle that denotes the forward direction of track where this node is</summary>
        float trackAngleForward;

        //For non-junction nodes:
        /// <summary>track Vector Node index of the non-junction node</summary>
        public int TVNIndex { get; set; }
        /// <summary>The index of the vector section within the vector node</summary>
        public int trackVectorSectionIndex { get; set; }
        /// <summary>the offset into the track vector section.</summary>
        public float trackSectionOffset { get; set; }
        /// <summary>number of seconds to wait after stopping at this node</summary>
        public int WaitTimeS { get; set; } 
        /// <summary>clock time to wait until if not zero</summary>
        public int WaitUntil { get; set; }
        /// <summary>number of cars to uncouple, negative means keep rear</summary>
        public int NCars { get; set; }

        private bool _forwardOriented = true;
        /// <summary>is the path oriented forward  or not (with respect of orientation of track itself</summary>
        public bool ForwardOriented
        {
            get { return _forwardOriented; }
            set { _forwardOriented = value; TrackAngle = trackAngleForward + (_forwardOriented ? 0 : (float)Math.PI); }
        }

        /// <summary>
        /// basic constructor setting only trackDB and tsectionDat
        /// </summary>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        public TrainpathVectorNode(TrackDB trackDB, TSectionDatFile tsectionDat)
            :base(trackDB, tsectionDat)
        {
        }

        /// <summary>
        /// Basic constructor using another node for the trackDB and tsectionDB
        /// </summary>
        /// <param name="otherNode">Just another node that already has trackDB and tsectionDB set</param>
        public TrainpathVectorNode(TrainpathNode otherNode)
            :base(otherNode)
        {
        }

        /// <summary>
        /// Constructor where location is copied from the given traveller
        /// </summary>
        /// <param name="otherNode">just another node to have access to trackDB and tsectiondat</param>
        /// <param name="traveller">The traveller that contains the exact location and distance on track to initialize the node</param>
        public TrainpathVectorNode(TrainpathNode otherNode, Traveller traveller)
            :base(otherNode)
        {
            CopyDataFromTraveller(traveller);
            location = traveller.WorldLocation; // Not part of CopyDataFromTraveller
            ForwardOriented = true; // only initial setting
        }

        /// <summary>
        /// constructor based on a nodeCandidate: a TrainpathVectorNode based on mouse location, does not contain all information
        /// </summary>
        /// <param name="nodeCandidate"></param>
        public TrainpathVectorNode(TrainpathVectorNode nodeCandidate)
            :base(nodeCandidate)
        {
            TVNIndex = nodeCandidate.TVNIndex;
            trackVectorSectionIndex = nodeCandidate.trackVectorSectionIndex;
            trackSectionOffset = nodeCandidate.trackSectionOffset;
            NextMainTVNIndex = nodeCandidate.TVNIndex;
            location = nodeCandidate.location;

            ForwardOriented = true; // only initial setting

            TrackNode tn = trackDB.TrackNodes[TVNIndex];
            Traveller traveller = new Traveller(tsectionDat, trackDB.TrackNodes, tn,
                                        location.TileX, location.TileZ, location.Location.X, location.Location.Z, Traveller.TravellerDirection.Forward);
            CopyDataFromTraveller(traveller);
            trackAngleForward = traveller.RotY; // traveller also has TVNindex, tvs, offset, etc, but we are not using that (should be consistent though)
        }

        /// <summary>
        /// Constructor based on PAT file information.
        /// </summary>
        /// <param name="tpn">TrPathNode from .pat file</param>
        /// <param name="pdp">TrackPDP from .pat file</param>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        public TrainpathVectorNode(TrPathNode tpn, TrackPDP pdp, TrackDB trackDB, TSectionDatFile tsectionDat)
            : base(pdp, trackDB, tsectionDat)
        {
            try
            {
                Traveller traveller = new Traveller(tsectionDat, trackDB.TrackNodes, this.location);
                CopyDataFromTraveller(traveller);
            }
            catch
            {
                IsBroken = true;
            }

            ForwardOriented = true; // only initial setting

            InterpretPathNodeFlags(tpn, pdp);
        }

        /// <summary>
        /// Make a shallow copy with all links to other nodes set to null;
        /// </summary>
        /// <returns>a copy of this node</returns>
        public override TrainpathNode ShallowCopyNoLinks()
        {
            TrainpathVectorNode newNode = (TrainpathVectorNode)this.MemberwiseClone();
            newNode.NextMainNode = null;
            newNode.NextSidingNode = null;
            newNode.PrevNode = null;
            return newNode;
        }

        /// <summary>
        /// Copy some relevant data from a traveller, specifically the track data 
        /// </summary>
        /// <param name="traveller"></param>
        public void CopyDataFromTraveller(Traveller traveller)
        {
            TVNIndex = traveller.TrackNodeIndex;
            trackVectorSectionIndex = traveller.TrackVectorSectionIndex;
            trackSectionOffset = traveller.TrackNodeOffset - getSectionStartDistance();
            trackAngleForward = traveller.RotY;
        }

        /// <summary>
        /// Find the exact distance of the start of the current tracksection (from the beginning of the vector node)
        /// </summary>
        /// <returns></returns>
        private float getSectionStartDistance()
        {
            float distanceFromStart = 0;
            TrackNode tn = trackDB.TrackNodes[TVNIndex];
            for (int tvsi = 0; tvsi < trackVectorSectionIndex; tvsi++)
            {
                TrVectorSection tvs = tn.TrVectorNode.TrVectorSections[tvsi];
                TrackSection trackSection = tsectionDat.TrackSections.Get(tvs.SectionIndex);
                if (trackSection != null)  // if trackSection is missing somehow, well, do without.
                {
                    distanceFromStart += ORTS.TrackViewer.Drawing.DrawTrackDB.GetLength(trackSection);
                }
            }
            return distanceFromStart;
        }

        /// <summary>
        /// Determine the orientation of the current node, by using the previousNode as well as the TVN that links the
        /// previous node with this node.
        /// </summary>
        /// <param name="previousNode">previouse node</param>
        /// <param name="linkingTVNIndex">the index of the Track Vector Node linking the previous node to this node</param>
        public override void determineOrientation(TrainpathNode previousNode, int linkingTVNIndex)
        {
            if (IsBroken)
            {   // do not update the orientation. Just use default
                return;
            }

            // this is a non-junction node. linkingTVNIndex should be the same as TVNIndex.
            ForwardOriented = !this.IsEarlierOnTrackThan(previousNode);

            if (Type == TrainpathNodeType.Reverse)
            {   // since direction is determined from previous node, after a reversal the direction is changed
                ForwardOriented = !ForwardOriented;
            }
        }

        /// <summary>
        /// Determine whether this node is earlier on a track than the given otherNode. Earlier here is defined
        /// in terms of the track orientation itself (so not in terms of the direction of a path).
        /// </summary>
        /// <param name="otherNode">Other node to compare against</param>
        /// <returns>true if this node is earlier on the track.</returns>
        public bool IsEarlierOnTrackThan(TrainpathNode otherNode)
        {
            if (otherNode is TrainpathJunctionNode)
            {
                return (otherNode as TrainpathJunctionNode).junctionIndex == trackDB.TrackNodes[TVNIndex].JunctionIndexAtEnd(); 
            }
            else
            {
                TrainpathVectorNode otherVectorNode = otherNode as TrainpathVectorNode;
                return (trackVectorSectionIndex < otherVectorNode.trackVectorSectionIndex)
                      || ((trackVectorSectionIndex == otherVectorNode.trackVectorSectionIndex)
                                && (trackSectionOffset < otherVectorNode.trackSectionOffset));
            }
        }

        /// <summary>
        /// Is the current node between the two other nodes or not.
        /// This assumes that all of the nodes are on the same track (or junctions bordering on the track). But this is not checked,
        /// </summary>
        /// <param name="node1">First node</param>
        /// <param name="node2">Second node</param>
        /// <returns>true if indeed between node1 and node2</returns>
        public bool IsBetween(TrainpathNode node1, TrainpathNode node2)
        {
            return (this.IsEarlierOnTrackThan(node1) != this.IsEarlierOnTrackThan(node2));
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
        // Since this interpretation belongs to the PATfile itself, 
        // in principle it would be more logical to have it in PATfile.cs. But this leads to too much code duplication
        private void InterpretPathNodeFlags(TrPathNode tpn, TrackPDP pdp)
        {
            if ((tpn.pathFlags & 03) == 0) return;
            // bit 0 and/or bit 1 is set.

            if ((tpn.pathFlags & 01) != 0)
            {
                // if bit 0 is set: reversal
                Type = TrainpathNodeType.Reverse;
            }
            else
            {
                // bit 0 is not set, but bit 1 is set:waiting point
                Type = TrainpathNodeType.Stop;
                if (pdp.IsInvalid) // not a valid point
                {
                    Type = TrainpathNodeType.Invalid;
                }
            }

            WaitTimeS = (int)((tpn.pathFlags >> 16) & 0xffff); // get the AAAA part.
            if (WaitTimeS >= 30000 && WaitTimeS < 40000)
            {
                // real wait time. 
                // waitTimeS (in decimal notation) = 3HHMM  (hours and minutes)
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
                if (Type == TrainpathNodeType.Stop)
                    Type = TrainpathNodeType.Uncouple;
            }
            else if (WaitTimeS >= 60000)  // this is old and should be removed/reused
            {
                // waitTimes = 6xSSS  with waitTime SSS seconds.
                WaitTimeS %= 1000;
            }

        }

        /// <summary>
        /// This is the reverse operation of InterpretPathNodeFlags: going from internal notation back to MSTS flags
        /// </summary>
        /// <returns>8-digit hexadecimal number (as string) describing the flags</returns>
        public override string GetFlags()
        {
            int AAAA = 0;
            int BBBB = 0;
            
            switch (Type)
            {
                case TrainpathNodeType.Start:
                case TrainpathNodeType.End:
                    BBBB = 0;
                    break;
                case TrainpathNodeType.Reverse:
                    BBBB = 1;
                    break;
                case TrainpathNodeType.Uncouple:
                    BBBB = 2;
                    if (NCars > 0)
                    {
                        AAAA = 40000 + NCars * 100 + WaitTimeS; 
                    }
                    if (NCars < 0)
                    {
                        AAAA = 50000 - NCars * 100 + WaitTimeS; 
                    }
                    break;
                case TrainpathNodeType.Stop:
                    BBBB = 2;
                    if (WaitUntil == 0)
                    {
                        AAAA = WaitTimeS;
                    }
                    else
                    {
                        int totalMinutes = WaitUntil / 60;
                        int hours = totalMinutes / 60;
                        int minutes = totalMinutes - 60 * hours;
                        AAAA = 30000 + 100 * hours + minutes;
                    }
                    break;
                default:
                    BBBB = 4; //intermediate point;
                    break;
                    
            }
            return string.Format("{0:x4}{1:x4}", AAAA, BBBB);
        }
    }
}
