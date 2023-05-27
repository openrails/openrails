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
using System.Collections.Generic;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// This class describes a trainpath node and some extra information needed for routines that want to autoconnect two Nodes.
    /// In particular, it will contain the information on the Junction to connect from (either the node itself or the nearest
    /// junction in the right direction
    /// </summary>
    class ConnectableNode
    {
        /// <summary>The corresponding trainpath node</summary>
        public TrainpathNode OriginalNode { get; private set; }
        /// <summary>The correspinding trainpath node if it is a vector node (null otherwise)</summary>
        public TrainpathVectorNode OriginalNodeAsVector { get; private set; }

        /// <summary>The index of the junction used for connecting</summary>
        public int ConnectingJunctionIndex { get; private set; }
        /// <summary>Is the junction used for connecting a facing junction. 
        /// Note for searching backward along the path facing is defined for moving backward!</summary>
        public bool IsConnectingJunctionFacing { get; private set; }
        /// <summary>In a reconnect path, is this the junction from which to connect, or the junction to which to connect</summary>
        private bool IsFrom { get; set; }
        /// <summary>If this node on a reverse looking path</summary>
        public bool IsConnectingForward { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node">The original trainpath node</param>
        /// <param name="isAFromNode">In a reconnect path, is this the junction from which to connect, or the junction to which to connect</param>
        /// <param name="isConnectingForward">Is this node connecting forwards (along the path) or not</param>
        public ConnectableNode(TrainpathNode node, bool isAFromNode, bool isConnectingForward)
        {
            OriginalNode = node;
            IsFrom = isAFromNode;
            IsConnectingForward = isConnectingForward;
            DetermineJunction();
        }

        /// <summary>
        /// Determine the details of junction from which we connect
        /// </summary>
        private void DetermineJunction()
        {
            OriginalNodeAsVector = OriginalNode as TrainpathVectorNode;
            if (OriginalNodeAsVector != null)
            {
                DetermineJunctionForVectorNode();
            }
            else
            {
                TrainpathJunctionNode nodeAsJunction = OriginalNode as TrainpathJunctionNode; // cannot be null
                this.ConnectingJunctionIndex = nodeAsJunction.JunctionIndex;
                this.IsConnectingJunctionFacing = nodeAsJunction.IsFacingPoint;
            }

            if (!IsConnectingForward)
            {
                this.IsConnectingJunctionFacing = !this.IsConnectingJunctionFacing;
            }
        }

        /// <summary>
        /// Determine the details of junction from which we connect in case this is a vector node.
        /// In this case it is important to know whether we are in a From node or in a To node.
        /// </summary>
        private void DetermineJunctionForVectorNode()
        {

            int tvnIndex = OriginalNodeAsVector.TvnIndex;
            if ((IsFrom && IsConnectingForward) || (!IsFrom && !IsConnectingForward))
            {
                // the first junction node of the reconnect path is after the vector node.
                this.ConnectingJunctionIndex = OriginalNodeAsVector.GetNextJunctionIndex(tvnIndex);
                int junctionTrailingTvn = TrackExtensions.TrackNode(this.ConnectingJunctionIndex).TrailingTvn();
                this.IsConnectingJunctionFacing = (tvnIndex == junctionTrailingTvn);
            }
            else
            {
                // the last junction node of the reconnect path is before this vector node.
                this.ConnectingJunctionIndex = OriginalNodeAsVector.GetPrevJunctionIndex(tvnIndex);
                int junctionTrailingTvn = TrackExtensions.TrackNode(this.ConnectingJunctionIndex).TrailingTvn();
                this.IsConnectingJunctionFacing = (tvnIndex != junctionTrailingTvn);
            }

        }

        /// <summary>
        /// Reverse the orientation of the original node,
        /// and then recalculate the connection properties.
        /// </summary>
        public void ReverseOrientation()
        {
            OriginalNode.ReverseOrientation();
            DetermineJunction();
        }

        public override string ToString()
        {
            return "Connectable " + OriginalNode.ToString();
        }
    }

    /// <summary>
    /// Contains a list of connectable nodes that are available for reconnection during an auto-connect action on an editable path.
    /// </summary>
    class ReconnectNodeOptions
    {
        /// <summary>There is a good connection</summary>
        public bool ConnectionIsGood { get { return (ActualReconnectNode != null); } }
        /// <summary>The actual reconnect point in case there is a good connection. Null otherwise</summary>
        public ConnectableNode ActualReconnectNode { get; private set; }

        List<ConnectableNode> connectableNodeOptions;
        bool isForwardConnecting;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="isConnectingForward">Are the options needed for connecting forwards (along the path) or not</param>
        public ReconnectNodeOptions(bool isConnectingForward)
        {
            connectableNodeOptions = new List<ConnectableNode>();
            ActualReconnectNode = null;
            this.isForwardConnecting = isConnectingForward;
        }

        /// <summary>
        /// Add a node that now is an option for reconnecting
        /// </summary>
        /// <param name="node">Node for the path to add</param>
        /// <param name="isAFromNode">Is the node a from or a to-node?</param>
        public void AddNode(TrainpathNode node, bool isAFromNode)
        {
            connectableNodeOptions.Add(new ConnectableNode(node, isAFromNode, isForwardConnecting));
        }

        /// <summary>
        /// Does the junction with given index correspond to the connecting junction of any of the options.
        /// If so, determine whether the connection is good (based on the direction of the junctions).
        /// If the connection is good, store the actual Connectable node for later use.
        /// </summary>
        /// <param name="junctionIndex">The index of the junction that we want to check for a connection</param>
        /// <param name="isFacing">Whether the junction is a facing junction</param>
        /// <returns>true if a connection can be made, even if it is not a good connection</returns>
        public bool FoundConnection(int junctionIndex, bool isFacing)
        {
            ActualReconnectNode = null; // give a good default
            if (connectableNodeOptions.Count == 0) return false;

            foreach (ConnectableNode candidate in connectableNodeOptions)
            {
                if (candidate.ConnectingJunctionIndex == junctionIndex)
                {   // we found a connection. We will not search for other connections.
                    // Now we just need to check if it is in the right direction
                    // If it is not in the right direction, we did not succeed.
                    bool goodConnection = (candidate.IsConnectingJunctionFacing == isFacing);
                    if (goodConnection)
                    {
                        ActualReconnectNode = candidate;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Find whether it is possible to connect directly two points on the same track.
        /// Store the reconnectpoint for later use during actual reconnecting
        /// </summary>
        /// <param name="fromNode">The point to connect from</param>
        /// <param name="firstTvnIndex">In case defined, the index of the first TVN the path has to follow</param>
        /// <returns>true if such a direct path has been found</returns>
        public bool FoundConnectionSameTrack(ConnectableNode fromNode, int? firstTvnIndex)
        {
            if (connectableNodeOptions.Count == 0)
            {
                return false;
            }

            ConnectableNode candidate = connectableNodeOptions[0];

            if (ExistsConnectionSameTrack(fromNode, candidate, firstTvnIndex))
            {
                ActualReconnectNode = candidate;
                return true;
            }

            return false;

        }

        /// <summary>
        /// Is there a connection on the same track between two nodes?
        /// </summary>
        /// <param name="fromNode">The node to connect from</param>
        /// <param name="toNode">The node to connect to</param>
        /// <param name="tvnIndex">Possibly a requirement on the index of the trackvectornode</param>
        private bool ExistsConnectionSameTrack(ConnectableNode fromNode, ConnectableNode toNode, int? tvnIndex)
        {
            if (connectableNodeOptions.Count == 0) return false;

            // we can only connect on the same track if the both start and reconnectnodes are on the same track
            // This means that both need to be vector nodes.
            // It also means, this is only possible for the first node
            TrainpathVectorNode fromAsVector = fromNode.OriginalNodeAsVector;
            TrainpathVectorNode toAsVector = toNode.OriginalNodeAsVector;

            if (fromAsVector == null) return false;
            if (toAsVector == null) return false;
            if (fromAsVector.TvnIndex != toAsVector.TvnIndex) return false;
            if (tvnIndex.HasValue && (tvnIndex.Value != fromAsVector.TvnIndex)) return false;

            //for a reverse point the orientation is defined as being after the reversal.
            bool reconnectIsForward = (toAsVector.NodeType == TrainpathNodeType.Reverse)
                ? !toAsVector.ForwardOriented
                : toAsVector.ForwardOriented;
            if (fromAsVector.ForwardOriented != reconnectIsForward) return false;

            if (isForwardConnecting)
            {
                if (fromAsVector.ForwardOriented != fromAsVector.IsEarlierOnTrackThan(toAsVector)) return false;
            }
            else
            {
                if (fromAsVector.ForwardOriented == fromAsVector.IsEarlierOnTrackThan(toAsVector)) return false;
            }

            return true;
        }

        /// <summary>
        /// In case there is exactly one option for reconnection, return the corresponding connectable node.
        /// Otherwise, return null
        /// </summary>
        public ConnectableNode SingleNode()
        {
            if (connectableNodeOptions.Count == 1)
            {
                return connectableNodeOptions[0];
            }
            return null;
        }
    }
}
