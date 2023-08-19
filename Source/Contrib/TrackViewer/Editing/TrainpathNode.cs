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
//      or they are special nodes like start, end, wait, reverse nodes.
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

using Orts.Formats.Msts;
using ORTS.Common;
using ORTS.TrackViewer.Drawing;
using Orts.Simulation;

namespace ORTS.TrackViewer.Editing
{
    #region TrainpathNodeType
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
        /// <summary>Node is a reversal node</summary>
        Reverse,
        /// <summary>Temporary node for editing purposes</summary>
        Temporary,
    };

    /// <summary>
    /// Store the reason why a path is broken. This makes it easier to make it unbroken.
    /// </summary>
    internal enum NodeStatus
    {
        /// <summary>Default value is not broken, which all nodes get by default</summary>
        Unbroken,
        /// <summary>Node was set to invalid in .pat file</summary>
        SetAsInvalid,
        /// <summary>The closest junction is too far away</summary>
        NotOnJunction,
        /// <summary>The closest track is too far away</summary>
        NotOnTrack,
        /// <summary>The node is dangling siding node</summary>
        Dangling
    }
    #endregion

    #region TrainpathNode
    /// <summary>
    /// base class for all nodes in a trainpath (as defined by MSTS .pat file).
    /// The class is abstract because we only allow either junction of vector nodes
    /// </summary>
    public abstract class TrainpathNode
    {


        /// <summary> World location of the node, coming directly from .pat file </summary>
        public WorldLocation Location { get; set; }

        /// <summary> Stores the type of node (see TrainPathNodeType)</summary>
        public TrainpathNodeType NodeType { get; set; }
        /// <summary> True if the node is broken, meaning that its location can not be found in the track data base, is set as invalid or otherwise indicates a broken path.
        /// By having it independent of the NodeType, we can keep the kind of node that was intended, even if it is currently not in the right place.</summary>
        public bool IsBroken { get { return brokenStatus != NodeStatus.Unbroken; } }
        /// <summary> True if the node is broken because off-track and therefore not drawable</summary>
        public bool IsBrokenOffTrack { get { return (brokenStatus == NodeStatus.NotOnJunction) || (brokenStatus == NodeStatus.NotOnTrack); } }
        private NodeStatus brokenStatus;
        
        // From simple linking:
        /// <summary>Next path node on main path</summary>
        public TrainpathNode NextMainNode { get; set; }
        /// <summary>Next path node on siding path</summary>
        public TrainpathNode NextSidingNode { get; set; }
        /// <summary>Previous path node on main path (unless it is on a siding path</summary>
        public TrainpathNode PrevNode { get; set; }
        /// <summary>
        /// Is there, next to the track to the NextMainNode, also a parallel Siding path?
        /// This does include siding start, but Siding end is the first node that has no siding path anymore.
        /// </summary>
        public bool HasSidingPath { get; set; }
        
        //To find these, both the current node and the next node need to be known.
        /// <summary>Index of main vector node leaving this path node</summary>
        public int NextMainTvnIndex { get; set; }
        /// <summary>Index of siding vector node leaving this path node</summary>
        public int NextSidingTvnIndex { get; set; }

        /// <summary>Angle that denotes the 2D direction of the path in radians</summary>
        public float TrackAngle { get; protected set; }

        /// <summary>Reference to the track database to be able to search it</summary>
        protected TrackDB TrackDB { get; private set; }
        /// <summary>Reference to the track section data to be able to search it</summary>
        protected TrackSectionsFile TsectionDat { get; private set; }

        /// <summary>
        /// Sort of constructor. But it creates the right sub-class
        /// </summary>
        /// <returns>A sub-class object properly initialized</returns>
        public static TrainpathNode CreatePathNode(TrPathNode tpn, TrackPDP pdp, TrackDB trackDB, TrackSectionsFile tsectionDat)
        {
            if (pdp.IsJunction) {
                // we do not use tpn: this means we do not interpret the flags
                return new TrainpathJunctionNode(pdp, trackDB, tsectionDat);
            }
            else {
                return new TrainpathVectorNode(tpn, pdp, trackDB, tsectionDat);
            }
            
        }

        /// <summary>
        /// basic constructor, in case node is not created from PAT file, and only some parts are needed
        /// </summary>
        protected TrainpathNode(TrackDB trackDB, TrackSectionsFile tsectionDat)
        {
            this.TrackDB = trackDB;
            this.TsectionDat = tsectionDat;
            HasSidingPath = false;
            NextMainTvnIndex = 0;
            NextSidingTvnIndex = 0;
            NodeType = TrainpathNodeType.Other;
        }

        /// <summary>
        /// constructor, in case node is not created from PAT file.
        /// </summary>
        protected TrainpathNode(TrainpathNode otherNode)
            :this(otherNode.TrackDB, otherNode.TsectionDat)
        {
        }


        /// <summary>
        /// Creates a single trainpathNode and initializes everything that do not depend on other nodes.
        /// The trainpath constructor will initialize the rest.
        /// </summary>
        protected TrainpathNode(TrackPDP pdp, TrackDB trackDB, TrackSectionsFile tsectionDat)
            :this(trackDB, tsectionDat)
        {
            Location = new WorldLocation(pdp.TileX, pdp.TileZ, pdp.X, pdp.Y, pdp.Z);
            if (pdp.IsInvalid) // not a valid point
            {
                this.SetBroken(NodeStatus.SetAsInvalid);
            }
        }

        /// <summary>
        /// Make a shallow copy with all links to other nodes set to null;
        /// </summary>
        /// <returns>a copy of this node</returns>
        public abstract TrainpathNode ShallowCopyNoLinks();

        /// <summary>
        /// Try to find the index of the vector node connecting this path node to the (given) nextNode.
        /// </summary>
        /// <returns>The index of the vector node connection, or -1</returns>
        public abstract int FindTvnIndex(TrainpathNode nextNode);

        /// <summary>
        /// Determine the orientation of the current node, by using the previousNode as well as the TVN that links the
        /// previous node with this node.
        /// </summary>
        /// <param name="previousNode">previouse node</param>
        /// <param name="linkingTvnIndex">the index of the Track Vector Node linking the previous node to this node</param>
        public abstract void DetermineOrientation(TrainpathNode previousNode, int linkingTvnIndex);

        /// <summary>
        /// Reverse the orientation of the node
        /// </summary>
        public abstract void ReverseOrientation();

        /// <summary>
        /// Get the 'flags' of the current node, describing to MSTS what kind of node it is, 
        /// as well as some details for specific nodes like wait
        /// </summary>
        /// <returns>string containing 8-digit hexedecimal coded flags</returns>
        public virtual string FlagsToString()
        {
            return "00000000";
        }

        /// <summary>
        /// From the current pathnode and the linking tracknode, fin the junctionIndex of the next junction (or possibly end-point)
        /// </summary>
        /// <param name="linkingTrackNodeIndex">The index of the tracknode leaving the node</param>
        /// <returns>The index of the junction index at the end of the track (as seen from the node)</returns>
        public abstract int GetNextJunctionIndex(int linkingTrackNodeIndex);

        /// <summary>
        /// Set that the node is broken
        /// </summary>
        /// <param name="reason">description of why it is broken</param>
        internal void SetBroken(NodeStatus reason)
        {
            brokenStatus = reason;
        }

        /// <summary>
        /// Set the node to be no longer broken.
        /// </summary>
        public void SetNonBroken()
        {
            this.brokenStatus = NodeStatus.Unbroken; //not really needed now but nice to keep clean
        }

        /// <summary>
        /// In case a node is broken only because defined as such in the .pat file, 
        /// it can be made unbroken easily by removing the statement that it is unbroken.
        /// </summary>
        /// <returns></returns>
        public bool CanSetUnbroken()
        {
            return this.IsBroken && (this.brokenStatus == NodeStatus.SetAsInvalid);
        }

        /// <summary>
        /// Returns a string description with the status (related to being broken) of the node
        /// </summary>
        public string BrokenStatusString()
        {
            switch (brokenStatus)
            {
                case NodeStatus.SetAsInvalid:
                    return TrackViewer.catalog.GetString("Set as invalid in .pat file");
                case NodeStatus.NotOnJunction:
                    return TrackViewer.catalog.GetString("Closest junction is too far away");
                case NodeStatus.NotOnTrack:
                    return TrackViewer.catalog.GetString("Not able to place node on track");
                case NodeStatus.Dangling:
                    return TrackViewer.catalog.GetString("Dangling siding node");
                default:
                    return TrackViewer.catalog.GetString("Unknown");
            }

        }
        

        /// <summary>
        /// String output for shorter debug information
        /// </summary>
        public abstract string ToStringShort();

        /// <summary>
        /// String output for debug information on connections
        /// </summary>
        public string ToStringConnection()
        {
            return String.Format(System.Globalization.CultureInfo.CurrentCulture, "({0}-{1}-{2}){3}",
                (this.PrevNode == null) ? "-" : this.PrevNode.ToString(),
                this.ToString(), 
                (this.NextMainNode == null) ? "-" : this.NextMainNode.ToString(),
                (this.NextSidingNode == null) ? String.Empty : this.NextSidingNode.ToString());
        }
    }
    #endregion

    #region TrainpathJunctionNode
    /// <summary>
    /// Class to describe junction nodes that are part of a train path.
    /// </summary>
    public class TrainpathJunctionNode : TrainpathNode
    {
        /// <summary>index of junction node (in the track data base</summary>
        public int JunctionIndex { get; set; }
        /// <summary>true if this node entered from the facing point end</summary>
        public bool IsFacingPoint { get; set; }
        /// <summary>Does the current junction node happen to be an end-node (so not a real junction)</summary>
        public bool IsEndNode { get { return (TrackDB.TrackNodes[JunctionIndex].TrEndNode); } }
        /// <summary>Return the vector node index of the main path leaving this junction (main being defined as the first one defined)</summary>
        public int MainTvn { get { return TrackDB.TrackNodes[JunctionIndex].MainTvn(); } }
        /// <summary>Return the vector node index of the siding path leaving this junction (siding being defined as the second one defined)</summary>
        public int SidingTvn { get { return TrackDB.TrackNodes[JunctionIndex].SidingTvn(); } }
        /// <summary>Return the vector node index of the trailing path leaving this junction</summary>
        public int TrailingTvn { get { return TrackDB.TrackNodes[JunctionIndex].TrailingTvn(); } }
       

        /// <summary>The maximum distance a junction node is allowed from its closest junction before it is said to be broken</summary>
        const float maxDistanceAway = 2.5f;

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
        /// <param name="pdp">Corresponding PDP in the .patfile</param>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        public TrainpathJunctionNode(TrackPDP pdp, TrackDB trackDB, TrackSectionsFile tsectionDat) 
            : base(pdp, trackDB, tsectionDat)
        {
            JunctionIndex = FindJunctionOrEndIndex(true);
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
            double bestDistance2 = 1e10f;
            for (int j = 0; j < TrackDB.TrackNodes.Count(); j++)
            {
                TrackNode tn = TrackDB.TrackNodes[j];
                if (tn == null) continue;
                if (wantJunctionNode && (tn.TrJunctionNode == null)) continue;
                if (!wantJunctionNode && !tn.TrEndNode) continue;
                if (tn.UiD.TileX != Location.TileX || tn.UiD.TileZ != Location.TileZ) continue;

                double dx = tn.UiD.X - Location.Location.X;
                dx += (tn.UiD.TileX - Location.TileX) * 2048;
                double dz = tn.UiD.Z - Location.Location.Z;
                dz += (tn.UiD.TileZ - Location.TileZ) * 2048;
                double dy = tn.UiD.Y - Location.Location.Y;
                double d = dx * dx + dy * dy + dz * dz;
                if (bestDistance2 > d)
                {
                    bestIndex = j;
                    bestDistance2 = d;
                }

            }
            bool broken = (bestDistance2 > maxDistanceAway*maxDistanceAway);
            if (broken)
            {
                SetBroken(NodeStatus.NotOnJunction);
                bestIndex = 0;
            }
            return bestIndex;
        }

        /// <summary>
        /// Set the value of Facing point. This assumes the path is correctly linked already
        /// </summary>
        public void SetFacingPoint()
        {
            TrackNode tn = TrackDB.TrackNodes[JunctionIndex];
            if (tn == null) return;  // Leave IsFacingPoint to what it is.
            if (tn.TrJunctionNode == null) return;  // Leave IsFacingPoint to what it is.
                
            //First try using the next main index
            if (NextMainNode != null && NextMainTvnIndex >= 0)
            {
                IsFacingPoint = (tn.TrailingTvn() != NextMainTvnIndex);
                return;
            }

            //Otherwise, try link from previous node
            if (PrevNode == null) return;
            int prevTvnIndex = -1;
            if (PrevNode.NextMainNode == this) prevTvnIndex = PrevNode.NextMainTvnIndex;
            if (PrevNode.NextSidingNode == this) prevTvnIndex = PrevNode.NextSidingTvnIndex;
            IsFacingPoint = (tn.TrailingTvn() == prevTvnIndex);
        }

        /// <summary>
        /// Try to find the index of the vector node connecting this path node to the (given) nextNode.
        /// </summary>
        /// <returns>The index of the vector node connection, or -1</returns>
        public override int FindTvnIndex(TrainpathNode nextNode)
        {
            TrainpathVectorNode nextAsVectorNode = nextNode as TrainpathVectorNode;
            if (nextAsVectorNode != null)
            {   // from junction to vector node.
                if (this.ConnectsToTrack(nextAsVectorNode.TvnIndex))
                {
                    return nextAsVectorNode.TvnIndex;
                }
                else
                {   //node is perhaps not broken, but connecting track is
                    return -1;
                }
            }

            //both this node and the next node are junctions: find the vector node connecting them.
            //Probably this can be faster, by just finding the TrPins from this and next junction and find the common one.
            int nextJunctionIndex = (nextNode as TrainpathJunctionNode).JunctionIndex;

            for (int i = 0; i < TrackDB.TrackNodes.Count(); i++)
            {
                TrackNode tn = TrackDB.TrackNodes[i];
                if (tn == null || tn.TrVectorNode == null)
                    continue;
                if ((tn.JunctionIndexAtStart() == this.JunctionIndex && tn.JunctionIndexAtEnd() == nextJunctionIndex)
                   || (tn.JunctionIndexAtEnd() == this.JunctionIndex && tn.JunctionIndexAtStart() == nextJunctionIndex))
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
        /// <param name="linkingTvnIndex">the index of the Track Vector Node linking the previous node to this node</param>
        public override void DetermineOrientation(TrainpathNode previousNode, int linkingTvnIndex)
        {
            // the TVN is from the previous node, so backwards. Therefore:reverse
            if (DetermineOrientationSucceeded(linkingTvnIndex, true))
            {
                return;
            }

            // if it did not succeed, most likely previous node is broken.
            // Retry with next main or siding TVN. This will fail for the last node, so be it.)
            if (NextSidingNode != null) { linkingTvnIndex = NextSidingTvnIndex; }
            if (NextMainNode != null) { linkingTvnIndex = NextMainTvnIndex; } // might override result from previous line

            if (DetermineOrientationSucceeded(linkingTvnIndex, false)) // no reverse needed
            {
                return;
            }

            // nothing seems to work. Get default value, unless it is broken
            if (IsBroken) return;
            DetermineOrientationSucceeded(TrailingTvn, IsFacingPoint);
        }

        private bool DetermineOrientationSucceeded(int linkingTvnIndex, bool needsReversal)
        {
            Traveller traveller = PlaceTravellerAfterJunction(linkingTvnIndex);
            if (traveller == null) return false;

            if (needsReversal)
            {
                traveller.ReverseDirection();
            }
            TrackAngle = traveller.RotY;
            return true;
        }

        /// <summary>
        /// Reverse the orientation of the node. For a junction node this also includes reversing IsFacing
        /// </summary>
        public override void ReverseOrientation()
        {
            IsFacingPoint = !IsFacingPoint;
            TrackAngle += (float)Math.PI;
        }

        /// <summary>
        /// Place a traveller at the junction node location, but on a track leaving it.
        /// </summary>
        /// <param name="linkingTvnIndex">The index of the track leaving it</param>
        /// <returns>The traveller, with direction leaving this node.</returns>
        public Traveller PlaceTravellerAfterJunction(int linkingTvnIndex)
        {
            // it is a junction. Place a traveller onto the tracknode and find the orientation from it.
            try
            {   //for broken paths the tracknode doesn't exit or the traveller cannot be placed.
                TrackNode linkingTN = TrackDB.TrackNodes[linkingTvnIndex];
                Traveller traveller = new Traveller(TsectionDat, TrackDB.TrackNodes, linkingTN,
                                            Location.TileX, Location.TileZ, Location.Location.X, Location.Location.Z, Traveller.TravellerDirection.Forward);
                if (linkingTN.JunctionIndexAtStart() != this.JunctionIndex)
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

        /// <summary>
        /// Determine whether the current junction node connects to a track with given trackIndex.
        /// </summary>
        /// <param name="trackIndex"></param>
        /// <returns></returns>
        public bool ConnectsToTrack(int trackIndex)
        {
            if (IsBroken) return false;

            TrackNode tn = TrackDB.TrackNodes[JunctionIndex];
            if (tn == null) return false;

            foreach (TrPin pin in tn.TrPins)
            {
                if (pin.Link == trackIndex)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// From the current pathnode and the linking tracknode, fin the junctionIndex of the next junction (or possibly end-point)
        /// </summary>
        /// <param name="linkingTrackNodeIndex">The index of the tracknode leaving the node</param>
        /// <returns>The index of the junction index at the end of the track (as seen from the node)</returns>
        public override int GetNextJunctionIndex(int linkingTrackNodeIndex)
        {
            return TrackExtensions.GetNextJunctionIndex(this.JunctionIndex, linkingTrackNodeIndex);
        }

        /// <summary>
        /// Determine whether this junction node is the start of a simple siding (meaning a siding start, 
        /// where the two tracks meet at the next junction already
        /// </summary>
        public bool IsSimpleSidingStart()
        {
            if (IsFacingPoint)
            {
                return (GetNextJunctionIndex(MainTvn) ==
                        GetNextJunctionIndex(SidingTvn));
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// For a junction with two exits, get the index of the vector node not being used at the moment
        /// </summary>
        /// <returns>The index of the other leaving vector node.</returns>
        public int OtherExitTvnIndex()
        {
            // selecting whether to do this on main or siding is simple: if there is no next main, has to be siding
            int CurrentNextTvnIndex = (NextMainTvnIndex > 0 ) ? NextMainTvnIndex : NextSidingTvnIndex;
            if (CurrentNextTvnIndex == MainTvn)
            {
                return SidingTvn;
            }
            else
            {
                return MainTvn;
            }
        }

        /// <summary>
        /// Set the location from the tracknode database.
        /// </summary>
        public void SetLocationFromTrackNode()
        {
            Location = DrawTrackDB.UidLocation(TrackDB.TrackNodes[JunctionIndex].UiD);
        }

        /// <summary>
        /// Override the ToString for easier debugging
        /// </summary>
        public override string ToString()
        {
            return String.Format(System.Globalization.CultureInfo.CurrentCulture, 
                "Junction {0}={1} ({2})", this.JunctionIndex, this.NodeType.ToString(), 
                this.IsFacingPoint ? "Facing" : "Trailing"
                );
        }

        /// <summary>
        /// String output for shorter debug information
        /// </summary>
        public override string ToStringShort()
        {
            return String.Format(System.Globalization.CultureInfo.CurrentCulture, "J{0} ", this.JunctionIndex);
        }
    }
    #endregion

    #region TrainpathVectorNode
    /// <summary>
    /// Node as part of a train path that is not on a junction but on a vector node. It contains all the relevant extra data
    /// like where exactly on the vector node it is. It also contains all relevant extra data related to the type it is 
    /// (e.g. for wait points)
    /// </summary>
    public class TrainpathVectorNode : TrainpathNode
    {
        /// <summary>Angle that denotes the forward direction of track where this node is</summary>
        float trackAngleForward;

        //For non-junction nodes:
        /// <summary>track Vector Node index of the non-junction node. 0 means no TVN found</summary>
        public int TvnIndex { get; set; }
        /// <summary>The index of the vector section within the vector node</summary>
        public int TrackVectorSectionIndex { get; set; }
        /// <summary>the offset into the track vector section.</summary>
        public float TrackSectionOffset { get; set; }
        /// <summary>number of seconds to wait after stopping at this node.
        /// For openrails advanced shunting it can mean something else (3HHMM, 4NNSS, 5???? formats)</summary>
        public int WaitTimeS { get; set; } 

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
        public TrainpathVectorNode(TrackDB trackDB, TrackSectionsFile tsectionDat)
            :base(trackDB, tsectionDat)
        {
            TvnIndex = 0;
        }

        /// <summary>
        /// Basic constructor using another node for the trackDB and tsectionDB
        /// </summary>
        /// <param name="otherNode">Just another node that already has trackDB and tsectionDB set</param>
        public TrainpathVectorNode(TrainpathNode otherNode)
            :base(otherNode)
        {
            TvnIndex = 0;
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
            Location = traveller.WorldLocation; // Not part of CopyDataFromTraveller
            ForwardOriented = true; // only initial setting
        }

        /// <summary>
        /// constructor based on a nodeCandidate: a TrainpathVectorNode based on mouse location, does not contain all information
        /// </summary>
        /// <param name="nodeCandidate"></param>
        public TrainpathVectorNode(TrainpathVectorNode nodeCandidate)
            :base(nodeCandidate)
        {
            TvnIndex = nodeCandidate.TvnIndex;
            TrackVectorSectionIndex = nodeCandidate.TrackVectorSectionIndex;
            TrackSectionOffset = nodeCandidate.TrackSectionOffset;
            NextMainTvnIndex = nodeCandidate.TvnIndex;
            Location = nodeCandidate.Location;

            ForwardOriented = true; // only initial setting

            TrackNode tn = TrackDB.TrackNodes[TvnIndex];
            Traveller traveller = new Traveller(TsectionDat, TrackDB.TrackNodes, tn,
                                        Location.TileX, Location.TileZ, Location.Location.X, Location.Location.Z, Traveller.TravellerDirection.Forward);
            CopyDataFromTraveller(traveller);
            trackAngleForward = traveller.RotY; // traveller also has TvnIndex, tvs, offset, etc, but we are not using that (should be consistent though)
        }

        /// <summary>
        /// Constructor based on PAT file information.
        /// </summary>
        /// <param name="tpn">TrPathNode from .pat file</param>
        /// <param name="pdp">TrackPDP from .pat file</param>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        public TrainpathVectorNode(TrPathNode tpn, TrackPDP pdp, TrackDB trackDB, TrackSectionsFile tsectionDat)
            : base(pdp, trackDB, tsectionDat)
        {
            try
            {
                Traveller traveller = new Traveller(tsectionDat, trackDB.TrackNodes, this.Location);
                CopyDataFromTraveller(traveller);
            }
            catch
            {
                SetBroken(NodeStatus.NotOnTrack);
            }

            ForwardOriented = true; // only initial setting

            InterpretPathNodeFlags(tpn);
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
            TvnIndex = traveller.TrackNodeIndex;
            TrackVectorSectionIndex = traveller.TrackVectorSectionIndex;
            TrackSectionOffset = traveller.TrackNodeOffset - GetSectionStartDistance();
            trackAngleForward = traveller.RotY;
        }

        /// <summary>
        /// Find the exact distance of the start of the current tracksection (from the beginning of the vector node)
        /// </summary>
        /// <returns></returns>
        private float GetSectionStartDistance()
        {
            float distanceFromStart = 0;
            TrackNode tn = TrackDB.TrackNodes[TvnIndex];
            for (int tvsi = 0; tvsi < TrackVectorSectionIndex; tvsi++)
            {
                TrVectorSection tvs = tn.TrVectorNode.TrVectorSections[tvsi];
                TrackSection trackSection = TsectionDat.TrackSections.Get(tvs.SectionIndex);
                if (trackSection != null)  // if trackSection is missing somehow, well, do without.
                {
                    distanceFromStart += ORTS.TrackViewer.Drawing.DrawTrackDB.GetLength(trackSection);
                }
            }
            return distanceFromStart;
        }

        /// <summary>
        /// Try to find the index of the vector node connecting this path node to the (given) nextNode.
        /// </summary>
        /// <returns>The index of the vector node connection, or -1</returns>
        public override int FindTvnIndex(TrainpathNode nextNode)
        {
            TrainpathVectorNode nextAsVectorNode = nextNode as TrainpathVectorNode;
            if ((nextAsVectorNode != null) && (this.TvnIndex == nextAsVectorNode.TvnIndex))
            {   // two vector nodes, tvn indices must be the same
                return this.TvnIndex;
            }

            TrainpathJunctionNode nextAsJunctionNode = nextNode as TrainpathJunctionNode;          
            if ((nextAsJunctionNode != null) && nextAsJunctionNode.ConnectsToTrack(TvnIndex))
            {   // vector node to junction node, junction must connect to tvnIndex
                return this.TvnIndex;
            }

            //The nodes themselves might not be broken, but the link between them is.
            return -1;
        }

        /// <summary>
        /// Determine the orientation of the current node, by using the previousNode as well as the TVN that links the
        /// previous node with this node.
        /// </summary>
        /// <param name="previousNode">previouse node</param>
        /// <param name="linkingTvnIndex">the index of the Track Vector Node linking the previous node to this node</param>
        public override void DetermineOrientation(TrainpathNode previousNode, int linkingTvnIndex)
        {
            if (IsBroken)
            {   // do not update the orientation. Just use default
                return;
            }

            // this is a non-junction node. linkingTvnIndex should be the same as TvnIndex.
            ForwardOriented = !this.IsEarlierOnTrackThan(previousNode);

            if (NodeType == TrainpathNodeType.Reverse)
            {   // since direction is determined from previous node, after a reversal the direction is changed
                // needs to be done after checking with previous node
                ReverseOrientation();
            }
        }
        
        /// <summary>
        /// Reverse the orientation of the node
        /// </summary>
        public override void ReverseOrientation()
        {
            ForwardOriented = !ForwardOriented;
        }

        /// <summary>
        /// Determine whether this node is earlier on a track than the given otherNode. Earlier here is defined
        /// in terms of the track orientation itself (so not in terms of the direction of a path).
        /// </summary>
        /// <param name="otherNode">Other node to compare against</param>
        /// <returns>true if this node is earlier on the track.</returns>
        public bool IsEarlierOnTrackThan(TrainpathNode otherNode)
        {
            TrainpathJunctionNode otherJunctionNode = otherNode as TrainpathJunctionNode;
            if (otherJunctionNode != null)
            {
                return otherJunctionNode.JunctionIndex == TrackDB.TrackNodes[TvnIndex].JunctionIndexAtEnd();
            }

            TrainpathVectorNode otherVectorNode = otherNode as TrainpathVectorNode;
            return (TrackVectorSectionIndex < otherVectorNode.TrackVectorSectionIndex)
                  || ((TrackVectorSectionIndex == otherVectorNode.TrackVectorSectionIndex)
                            && (TrackSectionOffset < otherVectorNode.TrackSectionOffset));

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

        // Flag Intepretation
        // (No flag interpretation for junction nodes)

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
        private void InterpretPathNodeFlags(TrPathNode tpn)
        {
            if ((tpn.pathFlags & 03) == 0) return;
            // bit 0 and/or bit 1 is set.

            if ((tpn.pathFlags & 01) != 0)
            {
                // if bit 0 is set: reversal
                NodeType = TrainpathNodeType.Reverse;
            }
            else
            {
                // bit 0 is not set, but bit 1 is set:waiting point
                NodeType = TrainpathNodeType.Stop;
            }

            WaitTimeS = (int)((tpn.pathFlags >> 16) & 0xffff); // get the AAAA part.
        }

        /// <summary>
        /// This is the reverse operation of InterpretPathNodeFlags: going from internal notation back to MSTS flags
        /// </summary>
        /// <returns>8-digit hexadecimal number (as string) describing the flags</returns>
        public override string FlagsToString()
        {
            int AAAA = 0;
            int BBBB = 0;
            
            switch (NodeType)
            {
                case TrainpathNodeType.Start:
                case TrainpathNodeType.End:
                    BBBB = 0;
                    break;
                case TrainpathNodeType.Reverse:
                    BBBB = 1;
                    break;
                case TrainpathNodeType.Stop:
                    BBBB = 2;
                    AAAA = WaitTimeS;
                    break;
                default:
                    BBBB = 4; //intermediate point;
                    break;
                    
            }
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:x4}{1:x4}", AAAA, BBBB);
        }

        /// <summary>
        /// From the current pathnode and the linking tracknode, fin the junctionIndex of the next junction (or possibly end-point)
        /// </summary>
        /// <param name="linkingTrackNodeIndex">The index of the tracknode leaving the node</param>
        /// <returns>The index of the junction index at the end of the track (as seen from the node)</returns>
        public override int GetNextJunctionIndex(int linkingTrackNodeIndex)
        {
            TrackNode linkingTrackNode = TrackDB.TrackNodes[linkingTrackNodeIndex];
            return ForwardOriented
                ? linkingTrackNode.JunctionIndexAtEnd()
                : linkingTrackNode.JunctionIndexAtStart();
        }

        /// <summary>
        /// From the current pathnode and the linking tracknode, find the junctionIndex of the previous junction (or possibly end-point)
        /// </summary>
        /// <param name="linkingTrackNodeIndex">The index of the tracknode leaving the node</param>
        /// <returns>The index of the junction index at the beginning of the track (as seen from the node)</returns>
        public int GetPrevJunctionIndex(int linkingTrackNodeIndex)
        {
            TrackNode linkingTrackNode = TrackDB.TrackNodes[linkingTrackNodeIndex];
            bool towardsNodeIsForwardOriented =
                (this.NodeType == TrainpathNodeType.Reverse)
                ? !ForwardOriented 
                : ForwardOriented;
            return towardsNodeIsForwardOriented
                ? linkingTrackNode.JunctionIndexAtStart()
                : linkingTrackNode.JunctionIndexAtEnd();
        }

        /// <summary>
        /// Override the ToString for easier debugging
        /// </summary>
        public override string ToString()
        {
            return String.Format(System.Globalization.CultureInfo.CurrentCulture,
                "Vector {0}={1}", this.TvnIndex, this.NodeType.ToString());
        }

        /// <summary>
        /// String output for shorter debug information
        /// </summary>
        public override string ToStringShort()
        {
            return String.Format(System.Globalization.CultureInfo.CurrentCulture, "V{0} ", this.TvnIndex);
        }
    }
    #endregion
}
