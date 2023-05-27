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

using Orts.Simulation;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// This class contains a number of routines to modify the path: Adding nodes, removing nodes,
    /// making sure that also 'disambiguity' nodes are created/removed as needed.
    /// These tools work on the path itself and therefore have no state.
    /// Most routines take a reference to an int that is updated with the net amount of nodes added.
    /// </summary>
    public class ModificationTools
    {
        #region node counting
        /// <summary>Keeps cound of the net amount of nodes that have been added</summary>
        public int NetNodesAdded { get; private set; }

        /// <summary>
        /// Reset the amount of nodes added
        /// </summary>
        public void Reset()
        {
            NetNodesAdded = 0;
        }
        #endregion

        /// <summary>Constructor</summary>
        public ModificationTools()
        {
            Reset(); // to give an initial value
        }

        /// <summary>
        /// Add an additional node, where the next track node is not yet given.
        /// </summary>
        /// <param name="lastNode">currently last node</param>
        /// <param name="isMainPath">Do we add the node to the main path or not</param>
        /// <returns>The newly created (unlinked) path node</returns>
        public TrainpathNode AddAdditionalNode(TrainpathNode lastNode, bool isMainPath)
        {
            TrainpathVectorNode lastNodeAsVector = lastNode as TrainpathVectorNode;
            if (lastNodeAsVector != null)
            {
                return AddAdditionalNode(lastNode, lastNodeAsVector.TvnIndex, isMainPath);
            }

            TrainpathJunctionNode junctionNode = lastNode as TrainpathJunctionNode;
            if (junctionNode.IsEndNode) return null;  // if it happens to be the end of a path, forget about it.

            if (junctionNode.IsFacingPoint)
            {
                return AddAdditionalNode(lastNode, junctionNode.MainTvn, isMainPath);
            }
            else
            {
                return AddAdditionalNode(lastNode, junctionNode.TrailingTvn, isMainPath);
            }
        }

        /// <summary>
        /// Add an additional node starting at the given node, following the TvnIndex,
        /// but take care of a possible need for disambiguity. 
        /// </summary>
        /// <param name="lastNode">Node after which a new node needs to be added</param>
        /// <param name="tvnIndex">TrackVectorNode index of the track the path needs to be on</param>
        /// <param name="isMainPath">Do we add the node to the main path or not</param>
        /// <returns>The newly created path node</returns>
        public TrainpathNode AddAdditionalNode(TrainpathNode lastNode, int tvnIndex, bool isMainPath)
        {
            TrainpathVectorNode lastNodeAsVector = lastNode as TrainpathVectorNode;
            if (lastNodeAsVector != null)
            {
                return AddAdditionalJunctionNode(lastNode, lastNodeAsVector.TvnIndex, isMainPath);
            }

            TrainpathJunctionNode junctionNode = lastNode as TrainpathJunctionNode;
            if (junctionNode.IsSimpleSidingStart())
            {   // start of a simple siding. So the next node should be a node to remove disambiguity.
                TrainpathVectorNode halfwayNode = CreateHalfWayNode(junctionNode, tvnIndex);
                return AddAdditionalVectorNode(junctionNode, halfwayNode, isMainPath);
            }
            else
            {
                return AddAdditionalJunctionNode(junctionNode, tvnIndex, isMainPath);
            }
        }

        /// <summary>
        /// Add an additional node, from the current last node along the next TrackNodeVector (given by index)
        /// The added node will always be a junction node.
        /// </summary>
        /// <param name="lastNode">Currently last node of path</param>
        /// <param name="nextTvnIndex">TrackNodeVector index along which to place the track</param>
        /// <param name="isMainPath">Are we adding a node on the main path (alternative is passing path)</param>
        /// <returns>The newly created junction path node</returns>
        TrainpathJunctionNode AddAdditionalJunctionNode(TrainpathNode lastNode, int nextTvnIndex, bool isMainPath)
        {
            // we add a new activeNodeAsJunction
            TrainpathJunctionNode newNode = new TrainpathJunctionNode(lastNode);

            if (TrackExtensions.TrackNode(nextTvnIndex) == null)
            {
                return null; // apparently there is some issue in the track.
            }

            newNode.JunctionIndex = lastNode.GetNextJunctionIndex(nextTvnIndex);
            newNode.SetLocationFromTrackNode();

            // simple linking
            newNode.PrevNode = lastNode;
            if (isMainPath)
            {
                lastNode.NextMainTvnIndex = nextTvnIndex;
                lastNode.NextMainNode = newNode;
            }
            else
            {
                lastNode.NextSidingTvnIndex = nextTvnIndex;
                lastNode.NextSidingNode = newNode;
            }

            newNode.SetFacingPoint();
            newNode.DetermineOrientation(lastNode, nextTvnIndex);

            NetNodesAdded++;
            return newNode;
        }

        /// <summary>
        /// Add a new vector path node at the location of nodeCandidate.
        /// </summary>
        /// <param name="lastNode">node that will be predecessor of the new nodeCandidate</param>
        /// <param name="nodeCandidate">partial trainpath vector node describing the current mouse location</param>
        /// <param name="isMainPath">Do we add the node to the main path or not</param>
        /// <returns>The newly created vector node</returns>
        public TrainpathVectorNode AddAdditionalVectorNode(TrainpathNode lastNode, TrainpathVectorNode nodeCandidate, bool isMainPath)
        {
            // we add a new activeNodeAsJunction
            TrainpathVectorNode newNode = new TrainpathVectorNode(nodeCandidate);

            newNode.DetermineOrientation(lastNode, newNode.TvnIndex);

            // simple linking
            if (isMainPath)
            {
                lastNode.NextMainTvnIndex = newNode.NextMainTvnIndex;
                lastNode.NextMainNode = newNode;
                newNode.PrevNode = lastNode;
            }
            else
            {
                lastNode.NextSidingTvnIndex = newNode.NextMainTvnIndex;
                newNode.NextSidingTvnIndex = newNode.NextMainTvnIndex;
                lastNode.NextSidingNode = newNode;
            }

            NetNodesAdded++;
            return newNode;
        }

        /// <summary>
        /// Add zero or more additional main nodes
        /// </summary>
        /// <param name="lastNode">currently last node</param>
        /// <param name="numberOfNodesToAdd">The number of nodes to add</param>
        public void AddAdditionalMainNodes(TrainpathNode lastNode, int numberOfNodesToAdd)
        {
            int wantedNetNodesAdded = NetNodesAdded + numberOfNodesToAdd;
            while (NetNodesAdded < wantedNetNodesAdded && lastNode != null)
            {
                lastNode = AddAdditionalNode(lastNode, true);
            }
        }

        /// <summary>
        /// Create a (still unlinked) node halfway through the next section (so halfway between this
        /// and the next junction. Needed specially for disambiguity.
        /// </summary>
        /// <param name="junctionNode">The junction node where we start</param>
        /// <param name="tvnIndex">The TrackVectorNode index for the path</param>
        /// <returns>An unlinked vectorNode at the midpoint.</returns>
        private TrainpathVectorNode CreateHalfWayNode(TrainpathJunctionNode junctionNode, int tvnIndex)
        {   // The idea here is to use all the code in traveller to make life easier.

            // move the traveller halfway through the next vector section
            Traveller traveller = junctionNode.PlaceTravellerAfterJunction(tvnIndex);
            float distanceToTravel = traveller.TrackNodeLength / 2;
            traveller.Move(distanceToTravel);

            TrainpathVectorNode halfwayNode = new TrainpathVectorNode(junctionNode, traveller);
            halfwayNode.DetermineOrientation(junctionNode, tvnIndex);

            return halfwayNode;
        }

        /// <summary>
        /// Remove a node and all of the path that follows. Add a new next node along the path.
        /// </summary>
        /// <param name="firstNodeToRemove">The first node that is removed</param>
        public void ReplaceNodeAndFollowingByNewNode(TrainpathNode firstNodeToRemove)
        {
            if (firstNodeToRemove == null)
            {
                return;
            }
            // assumption is that there is no siding next to this point.
            TrainpathNode prevNode = firstNodeToRemove.PrevNode;
            prevNode.NextMainNode = null;
            prevNode.NextSidingNode = null; // should not be needed
            prevNode.NextSidingTvnIndex = 0;
            if (prevNode.NextMainTvnIndex == -1)
            {
                AddAdditionalNode(prevNode, true);
            }
            else
            {
                // Since we already know the TVN, simply add a node (meaning removing the end will extend the path. 
                AddAdditionalNode(prevNode, prevNode.NextMainTvnIndex, true);

            }
        }

        /// <summary>
        /// Add a new vector node at the given location in the middle of a path
        /// </summary>
        /// <param name="nodeCandidate"></param>
        /// <returns>The just created node</returns>
        public TrainpathVectorNode AddIntermediateMainNode(TrainpathVectorNode nodeCandidate)
        {
            TrainpathNode prevNode = nodeCandidate.PrevNode;
            TrainpathNode nextNode = prevNode.NextMainNode;

            TrainpathVectorNode newNode = AddAdditionalVectorNode(prevNode, nodeCandidate, true);

            newNode.NextMainNode = nextNode;
            newNode.NextSidingNode = null; // should not be needed
            nextNode.PrevNode = newNode;

            CleanAmbiguityNodes(newNode);

            return newNode;
        }

        /// <summary>
        /// Check the next and the previous nodes on whether they are disambiguity node, and if yes, remove them.
        /// </summary>
        /// <param name="keepNode">The (vector) node to keep</param>
        public void CleanAmbiguityNodes(TrainpathNode keepNode)
        {
            TrainpathNode[] nodesToCheck = { keepNode.PrevNode, keepNode.NextMainNode };
            foreach (TrainpathNode node in nodesToCheck)
            {
                if (node != null
                    && node.NodeType == TrainpathNodeType.Other
                    && node is TrainpathVectorNode)
                {
                    RemoveIntermediatePoint(node);
                }
            }
        }

        /// <summary>
        /// Remove a intermediate vector node. Possibly add a disambiguity node if needed.
        /// </summary>
        /// <param name="currentNode">Node to be removed</param>
        public void RemoveIntermediatePoint(TrainpathNode currentNode)
        {
            TrainpathNode prevNode = currentNode.PrevNode;
            prevNode.NextMainNode = currentNode.NextMainNode;
            prevNode.NextSidingNode = null; // should not be needed
            //lastNodeSidingPath.NextMainTvnIndex should be the same still
            if (prevNode.NextMainNode != null)
            {   // there might not be a next node.
                prevNode.NextMainNode.PrevNode = prevNode;
            }
            NetNodesAdded--;

            AddDisambiguityNodeIfNeeded(prevNode);
        }

        /// <summary>
        /// Check if after this node a disambiguity node needs to be added
        /// </summary>
        /// <param name="currentNode"></param>
        public void AddDisambiguityNodeIfNeeded(TrainpathNode currentNode)
        {
            //Check if we need to add an disambiguity node
            TrainpathJunctionNode currentNodeAsJunction = currentNode as TrainpathJunctionNode;
            if ((currentNodeAsJunction != null)
                && (currentNode.NextMainNode != null)
                && (currentNode.NextMainNode is TrainpathJunctionNode)
                && (currentNodeAsJunction.IsSimpleSidingStart())
                )
            {
                TrainpathVectorNode halfwayNode = CreateHalfWayNode(currentNodeAsJunction, currentNodeAsJunction.NextMainTvnIndex);
                halfwayNode.PrevNode = currentNode;
                AddIntermediateMainNode(halfwayNode);
            }
        }

        /// <summary>
        /// Create a partial path from the current node, along the new track vector index, until we can reconnect again
        /// </summary>
        /// <param name="currentNode">Starting place of the new partial path</param>
        /// <param name="newTvnIndex">Index of the new track vector node along which the path starts</param>
        /// <param name="reconnectNode">Node at we will reconnect to current path again</param>
        /// <param name="isMainPath">Do we add the node to the main path or not</param>
        /// <returns>The last node on the partial path, just before the reconnect node.</returns>
        public TrainpathNode CreatePartialPath(TrainpathNode currentNode, int newTvnIndex, TrainpathJunctionNode reconnectNode, bool isMainPath)
        {
            bool newHasSidingPath = isMainPath && currentNode.HasSidingPath;
            currentNode.HasSidingPath = newHasSidingPath;

            TrainpathNode newNode = AddAdditionalNode(currentNode, newTvnIndex, isMainPath);
            do
            {
                TrainpathJunctionNode newNodeAsJunction = newNode as TrainpathJunctionNode;
                if (newNodeAsJunction != null && (newNodeAsJunction.JunctionIndex == reconnectNode.JunctionIndex))
                {
                    //we have reached the reconnection point
                    break;
                }
                currentNode = newNode;
                currentNode.HasSidingPath = newHasSidingPath;
                newNode = AddAdditionalNode(currentNode, isMainPath);

            } while (newNode != null); // if we get here, something is wrong, because we checked we could reconnect

            // The returned node will not be the last node created, because that one is at the same location as the reconnect node
            return currentNode;
        }

        /// <summary>
        /// Stitch two paths together.
        /// In case both nodes are a junction (and supposedly the same junction), then patch the two on top of each other.
        /// Otherwise, simply connect the two.
        /// No checking here on whether the connection is good.
        /// </summary>
        /// <param name="lastNodeFirstPath">Node to connect from (last node of the first partial path)</param>
        /// <param name="firstNodeSecondPath">Node to connect to (first node of the second partial path)</param>
        /// <param name="isMainPath">Do we add the node to the main path or not</param>
        public void StitchTwoPaths(TrainpathNode lastNodeFirstPath, TrainpathNode firstNodeSecondPath, bool isMainPath)
        {
            TrainpathVectorNode lastNodeFirstPathAsVector = lastNodeFirstPath as TrainpathVectorNode;
            TrainpathVectorNode firstNodeSecondPathAsVector = firstNodeSecondPath as TrainpathVectorNode;

            if (lastNodeFirstPathAsVector == null && firstNodeSecondPathAsVector == null)
            {
                //both are junctions. Remove the last node of the first path
                lastNodeFirstPath = lastNodeFirstPath.PrevNode;
                NetNodesAdded--;
            }

            // make the connection
            if (isMainPath)
            {
                lastNodeFirstPath.NextMainNode = firstNodeSecondPath;
                // For the moment, if it is a siding path, we always reconnect to the main path. And then prevnode does not need to be relinked
                firstNodeSecondPath.PrevNode = lastNodeFirstPath;
            }
            else
            {
                lastNodeFirstPath.NextSidingNode = firstNodeSecondPath;
            }

            //in case the first node of the second path is a vector, make sure that its next tvn index is copied
            if (firstNodeSecondPathAsVector != null)
            {
                if (isMainPath)
                {
                    lastNodeFirstPath.NextMainTvnIndex = firstNodeSecondPathAsVector.TvnIndex;
                }
                else
                {
                    lastNodeFirstPath.NextSidingTvnIndex = firstNodeSecondPathAsVector.TvnIndex;
                }
            }
        }
    }
}
