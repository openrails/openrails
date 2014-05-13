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
// This class started as a copy of AIPath.cs with additional methods.  But it became more extensive and cleaned up in places
// Hence a different class

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MSTS.Formats;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// Defines the internal representation of a MSTS path. 
    /// The path itself is actually defined as a linked-list. That leaves the following things for this class
    ///     A link to the start point of the path
    ///     metadata like name of path, start end end point. These are public fields
    ///     A boolean describing whether the path has a well-defined end (meaning a node that has been designated as an end-node
    ///             instead of simply being the last node).
    ///     Routines to create the linked-list path from the definitions in a .pat file.
    ///     
    /// The class contains history functions like Undo and redo.
    /// </summary>
    public class Trainpath
    {
        #region public members
        /// <summary>Link to the first node of the path (starting point)</summary>
        public TrainpathNode FirstNode
        {
            get { return trainPaths[currentIndex].firstNode; }
            set { trainPaths[currentIndex].firstNode = value; }
        }

        /// <summary>Link to the first node of a stored tail</summary>
        public TrainpathNode FirstNodeOfTail
        {
            get { return trainPaths[currentIndex].firstNodeOfTail; }
            set { trainPaths[currentIndex].firstNodeOfTail = value; }
        }
        
        /// <summary>Does the path have a real end node?</summary>
        public bool HasEnd
        {
            get { return trainPaths[currentIndex].hasEnd; }
            set { trainPaths[currentIndex].hasEnd = value; }
        }

        /// <summary>Does the path have a real end node?</summary>
        public bool TailHasEnd
        {
            get { return trainPaths[currentIndex].tailHasEnd; }
            set { trainPaths[currentIndex].tailHasEnd = value; }
        }

        /// <summary>A path is broken when it has at least one broken node or a broken link</summary>
        public bool IsBroken
        {
            get { return trainPaths[currentIndex].isBroken; }
            set { trainPaths[currentIndex].isBroken = value; }
        }

        /// <summary>Is the path modified from the one as loaded from disc.</summary>
        public bool IsModified
        {
            get { return (currentIndex != currentIndexUnmodified); }
            set
            {
                if (value)
                {   // this path is modified. If it is the unmodified path, then no paths are unmodified
                    if (currentIndex == currentIndexUnmodified)
                    {
                        currentIndexUnmodified = -1;
                    }
                }
                else
                {   // this path is no longer modified, e.g. due to a save
                    currentIndexUnmodified = currentIndex;
                }
            }
        }


        // adminstration of various path properties
        /// <summary>Full file path of the .pat file that contained the train path</summary>
        public string FilePath
        {
            get { return trainPaths[currentIndex].filePath; }
            set { trainPaths[currentIndex].filePath = value; }
        }
        /// <summary>Name of the path as stored in the .pat file</summary>
        public string PathName
        {
            get { return trainPaths[currentIndex].pathName; }
            set { trainPaths[currentIndex].pathName = value; }
        }

        /// <summary>Name of the start point as stored in the .pat file</summary>
        public string PathStart
        {
            get { return trainPaths[currentIndex].pathStart; }
            set { trainPaths[currentIndex].pathStart = value; }
        }

        /// <summary>Name of the end point as stored in the .pat file</summary>
        public string PathEnd
        {
            get { return trainPaths[currentIndex].pathEnd; }
            set { trainPaths[currentIndex].pathEnd = value; }
        }

        /// <summary>identification name as stored in the .pat file</summary>
        public string PathId
        {
            get { return trainPaths[currentIndex].pathId; }
            set { trainPaths[currentIndex].pathId = value; }
        }

        /// <summary>Flags associated with the path (not the nodes)</summary>
        public PathFlags PathFlags
        {
            get { return trainPaths[currentIndex].pathFlags; }
            set { trainPaths[currentIndex].pathFlags = value; }
        }

        #endregion

        #region private members

        MSTS.Formats.TrackDB trackDB;
        TSectionDatFile tsectionDat;


        List<TrainPathData> trainPaths;
        int currentIndex; // trainPaths are indexed
        int currentIndexUnmodified; // The index of the last saved path.

        #endregion

        /// <summary>
        /// Basic constructor creating an empty path, but storing track database and track section data
        /// </summary>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        public Trainpath(TrackDB trackDB, TSectionDatFile tsectionDat)
        {
            this.trackDB = trackDB;
            this.tsectionDat = tsectionDat;
            trainPaths = new List<TrainPathData>();
            trainPaths.Add(new TrainPathData());
        }

        /// <summary>
        /// Creates an trainpath from PAT file information.
        /// First creates all the nodes and then links them together into a main list
        /// with optional parallel siding list.
        /// </summary>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        /// <param name="filePath">file name including path of the .pat file</param>
        public Trainpath(TrackDB trackDB, TSectionDatFile tsectionDat, string filePath)
            :this(trackDB, tsectionDat)
        {
            this.FilePath = filePath;

            PATFile patFile = new PATFile(filePath);
            PathId = patFile.PathID;
            PathName = patFile.Name;
            PathStart = patFile.Start;
            PathEnd = patFile.End;
            PathFlags = patFile.Flags;

            List<TrainpathNode> Nodes = new List<TrainpathNode>();
            createNodes(patFile, Nodes);

            LinkNodes(patFile, Nodes);
            SetFacingPoints(Nodes);

            FindSidingEnds();
            FindNodeOrientations();
            FindWronglyOrientedLinks();
            DetermineIfBroken();
        }

        #region Methods to parse MSTS paths
        /// <summary>
        /// Create the initial list of nodes from the patFile. No linking or preoccessing
        /// </summary>
        /// <param name="patFile">Patfile object containing the various unprocessed Track Path Nodes</param>
        /// <param name="Nodes">The list that is going to be filled with as-of-yet unlinked and almost unprocessed path nodes</param>
        private void createNodes(PATFile patFile, List<TrainpathNode> Nodes)
        {
            foreach (TrPathNode tpn in patFile.TrPathNodes)
                Nodes.Add(TrainpathNode.CreatePathNode(tpn, patFile.TrackPDPs[(int)tpn.fromPDP], trackDB, tsectionDat));
            FirstNode = Nodes[0];
            FirstNode.NodeType = TrainpathNodeType.Start;
        }
 
        /// <summary>
        /// Link the various nodes to each other. Do some initial processing on the path, like finding linking TVNs
        /// and determining whether junctions are facing or not.
        /// </summary>
        /// <param name="patFile">Patfile object containing the various unprocessed Track Path Nodes</param>
        /// <param name="Nodes">The list of as-of-yet unlinked processed path nodes</param>
        static private void LinkNodes(PATFile patFile, List<TrainpathNode> Nodes)
        {
            // Connect the various nodes to each other
            for (int i = 0; i < Nodes.Count; i++)
            {
                TrainpathNode node = Nodes[i];
                TrPathNode tpn = patFile.TrPathNodes[i];

                // find TvnIndex to next main node.
                if (tpn.HasNextMainNode)
                {
                    node.NextMainNode = Nodes[(int)tpn.nextMainNode];
                    node.NextMainNode.PrevNode = node;
                    node.NextMainTvnIndex = node.FindTvnIndex(node.NextMainNode);
                }

                // find TvnIndex to next siding node
                if (tpn.HasNextSidingNode)
                {
                    node.NextSidingNode = Nodes[(int)tpn.nextSidingNode];
                    if (node.NextSidingNode.PrevNode == null)
                    {
                        node.NextSidingNode.PrevNode = node;
                    }
                    node.NextSidingTvnIndex = node.FindTvnIndex(node.NextSidingNode);
                }

                if (node.NextMainNode != null && node.NextSidingNode != null)
                    node.NodeType = TrainpathNodeType.SidingStart;
            }
        }

        /// <summary>
        /// For all the junction nodes, set whether it is a facing point or not
        /// </summary>
        /// <param name="Nodes">The list of path nodes that now need to be linked</param>
        static private void SetFacingPoints(List<TrainpathNode> Nodes)
        {
            // It is just a convenience to use the list of Nodes. 
            // In principle this can be done without the list by following the path
            for (int i = 0; i < Nodes.Count; i++)
            {
                TrainpathJunctionNode node = Nodes[i] as TrainpathJunctionNode;
                if (node == null) continue;
                node.SetFacingPoint();
            }
        }
  
        /// <summary>
        /// Find all nodes that are the end of a siding (so where main path and siding path come together again)
        /// </summary>
        private void FindSidingEnds()
        {
            TrainpathNode curSidingEnd = null; // if we are still looking for a sidingEnd
            for (TrainpathNode curMainNode = FirstNode; curMainNode != null; curMainNode = curMainNode.NextMainNode)
            {
                if (curSidingEnd != null)
                {
                    if (curMainNode == curSidingEnd)
                    { // end of siding
                        curSidingEnd = null;
                    }
                    else
                    {
                        curMainNode.HasSidingPath = true;
                    }
                }

                TrainpathNode curSidingNode = curMainNode.NextSidingNode;
                while (curSidingNode != null && curSidingNode.NextSidingNode != null)
                {
                    curSidingNode = curSidingNode.NextSidingNode;
                }
                if (curSidingNode != null)
                {
                    curSidingEnd = curSidingNode;
                    curSidingEnd.NodeType = TrainpathNodeType.SidingEnd;
                    curMainNode.HasSidingPath = true;
                }
            }
        }

        /// <summary>
        /// Run through the track and for each node find the direction on the node
        /// </summary>
        void FindNodeOrientations()
        {
            // Node orientations are determined based on the track leading to the node
            // This makes it easier to extend the path (in case the last node is not known)
            // Only for the start node this is not possible. But hey, that is anyway up to the user.

            // start of path
            TrainpathNode currentMainNode = FirstNode;
            while (currentMainNode.NextMainNode != null)
            {

                // If there is a siding starting here, go through it until it ends.
                TrainpathNode currentSidingNode = currentMainNode;
                while (currentSidingNode.NextSidingNode != null)
                {
                    TrainpathNode nextSidingNode = currentSidingNode.NextSidingNode;
                    nextSidingNode.DetermineOrientation(currentSidingNode, currentSidingNode.NextSidingTvnIndex);
                    currentSidingNode = nextSidingNode;
                }

                // In case a main node has two nodes leading to it (siding and main), then
                // the main track will override.
                TrainpathNode nextMainNode = currentMainNode.NextMainNode;
                nextMainNode.DetermineOrientation(currentMainNode, currentMainNode.NextMainTvnIndex);
                currentMainNode = nextMainNode;
            }

            if (currentMainNode is TrainpathVectorNode)
            {   // Only vector nodes can be a real end!
                HasEnd = true;
                currentMainNode.NodeType = TrainpathNodeType.End;
            }
            else
            {
                HasEnd = false;
            }

            //Determine direction of first point. By using second point, the direction will be reversed
            FirstNode.DetermineOrientation(FirstNode.NextMainNode, FirstNode.NextMainTvnIndex);
            FirstNode.ReverseOrientation();
        }

        /// <summary>
        /// Find situations where vectors nodes do not point correctly towards the next node
        /// </summary>
        void FindWronglyOrientedLinks()
        {
            // start of path
            TrainpathNode currentMainNode = FirstNode;
            while (currentMainNode.NextMainNode != null)
            {
                TrainpathVectorNode currentNodeAsVector = currentMainNode as TrainpathVectorNode;
                if (currentNodeAsVector != null)
                {
                    // if it is forward oriented it should also be earlier on track as next node
                    // if it is not forward oriented it should also not be earlier on track as next node
                    if (   currentNodeAsVector.ForwardOriented
                        != currentNodeAsVector.IsEarlierOnTrackThan(currentMainNode.NextMainNode))
                    {   // the link is broken (although the nodes themselves might be fine)
                        currentMainNode.NextMainTvnIndex = -1;
                    }              
                }

                TrainpathNode currentSidingNode = currentMainNode;
                while (currentSidingNode.NextSidingNode != null)
                {
                    currentNodeAsVector = currentSidingNode as TrainpathVectorNode;
                    if (currentNodeAsVector != null)
                    {
                        // if it is forward oriented it should also be earlier on track as next node
                        // if it is not forward oriented it should also not be earlier on track as next node
                        if (currentNodeAsVector.ForwardOriented
                            != currentNodeAsVector.IsEarlierOnTrackThan(currentSidingNode.NextSidingNode))
                        {   // the link is broken (although the nodes themselves might be fine)
                            currentMainNode.NextSidingTvnIndex = -1;
                        }
                    }
                    currentSidingNode = currentSidingNode.NextSidingNode;
                }

                currentMainNode = currentMainNode.NextMainNode;
            }
        }

        #endregion

        #region Methods to deal with history
        /// <summary>
        /// Undo operation. Pretty simple. Just use the previous path in the list if available.
        /// Note that all paths are retained.
        /// </summary>
        public void Undo()
        {
            currentIndex--;
            if (currentIndex < 0) { currentIndex = 0; }
        }

        /// <summary>
        /// Redo operation. Pretty simple. Just use the next path in the list if available.
        /// Note that all paths are retained.
        /// </summary>
        public void Redo()
        {
            currentIndex++;
            if (currentIndex >= trainPaths.Count) { currentIndex = trainPaths.Count - 1; }
        }

        /// <summary>
        /// Use the last of the available stored paths in the history list
        /// </summary>
        public void UseLast()
        {
            currentIndex = trainPaths.Count-1;
        }

        /// <summary>
        /// Use the last but one of the available stored paths in the history list
        /// </summary>
        public void UseLastButOne()
        {
            currentIndex = trainPaths.Count-2;
            if (currentIndex <= 0) currentIndex = 0; // should not happen
        }

        /// <summary>
        /// Store the current path (without metadata), so we can undo later. 
        /// This is done by making a deep copy of path (as defined by nodes), and putting the copy in the list.
        /// This makes sure that the currently active references are still pointing to nodes in the current active path.
        /// </summary>
        public void StoreCurrentPath()
        {
            int newIndex = currentIndex + 1;
            if (trainPaths.Count > newIndex) {
                trainPaths.RemoveRange(newIndex, trainPaths.Count - newIndex);
            }

            // insert copy just before current active one
            TrainPathData newTrainData = trainPaths[currentIndex].DeepCopy();
            trainPaths.Insert(currentIndex, newTrainData);
            currentIndex = newIndex;

            //Since we store the path, we must assume something has been modified (or will in a jiffy)
            IsModified = true;
        }

   
        #endregion

        #region Methods giving info on path
        /// <summary>
        /// Determine if the path is broken or not
        /// </summary>
        /// <returns>A collection containing integers that tell how far to draw the path to go to the broken node</returns>
        public Collection<int> DetermineIfBroken()
        {
            if (FirstNode == null) {
                IsBroken = false;
                return new Collection<int>();
            }

            List<TrainpathNode> brokenNodes = new List<TrainpathNode>();
                        
            TrainpathNode currentMainNode = FirstNode;
            while (currentMainNode.NextMainNode != null)
            {
                if (currentMainNode.IsBroken)
                {
                    brokenNodes.Add(currentMainNode);
                }
                else if (currentMainNode.NextMainTvnIndex == -1)
                {
                    brokenNodes.Add(currentMainNode.NextMainNode);
                }
                else
                {
                    // For siding paths, it is difficult to get the right main node to draw until
                    // Most important however is that at least IsBroken is set correctly
                    TrainpathNode currentSidingNode = currentMainNode;
                    while (currentSidingNode.NextSidingNode != null)
                    {
                        if (currentSidingNode.NextSidingNode.IsBroken)
                        {
                            brokenNodes.Add(currentMainNode.NextMainNode); // we cannot draw until a sidingNode
                        }
                        if (currentSidingNode.NextSidingTvnIndex == -1)
                        {
                            brokenNodes.Add(currentMainNode.NextMainNode);
                        }
                        currentSidingNode = currentSidingNode.NextSidingNode;

                        if (   currentSidingNode.NextSidingNode == null 
                            && currentSidingNode.NodeType != TrainpathNodeType.SidingEnd)
                        {   // The end of a siding track while still not on siding end
                            brokenNodes.Add(currentMainNode.NextMainNode);
                        }
                    }
                }

                currentMainNode = currentMainNode.NextMainNode;
            }

            if (currentMainNode.IsBroken)
            {   //for last node
                brokenNodes.Add(currentMainNode);
            }

            IsBroken = (brokenNodes.Count > 0);

            Collection<int> brokenNodeIndexes = new Collection<int>();
            foreach (TrainpathNode node in brokenNodes)
            {
                brokenNodeIndexes.Add(GetNodeNumber(node));
            }
            return brokenNodeIndexes;
        }

        /// <summary>
        /// Calculate the number of this node in the total path. FirstNode is 1. The node needs to be on mainpath,
        /// otherwise -1 will be returned.
        /// </summary>
        /// <param name="node">Node for which to calculate the number</param>
        /// <returns>The sequential number of the node in the main path. -1 in case node is not on main path.</returns>
        public int GetNodeNumber(TrainpathNode node)
        {
            int numberFound = 1;
            TrainpathNode mainNode = FirstNode;
            while (mainNode != null && mainNode != node)
            {
                numberFound++;
                mainNode = mainNode.NextMainNode;
            }
            if (mainNode == null)
            {
                return -1;
            }
            return numberFound;
        }
        #endregion
        class TrainPathData
        {
            public TrainpathNode firstNode;
            public TrainpathNode firstNodeOfTail;

            public string filePath;
            public string pathName;
            public string pathStart;
            public string pathEnd;
            public string pathId;

            public PathFlags pathFlags;

            public bool hasEnd;
            public bool tailHasEnd;
            public bool isBroken;
            
            public TrainPathData()
            {
                //firstNode = null; //this is already the default
                pathName = "<unknown>";
                pathEnd = "<unknown>";
                pathStart = "<unknown>";
                pathId = "new";
                //hasEnd = false; //this is already the default
            }

            /// <summary>
            /// Perform a deep copy on the current instance
            /// </summary>
            /// <returns>The copied instance.</returns>
            public TrainPathData DeepCopy()
            {
                TrainPathData newData = (TrainPathData)this.MemberwiseClone();
                newData.firstNode = DeepCopyOfLinkedNodes(firstNode);
                newData.firstNodeOfTail = DeepCopyOfLinkedNodes(firstNodeOfTail);
            
                return newData;
            }

            /// <summary>
            /// Perform a deep copy of a path consisting of linked nodes
            /// </summary>
            /// <param name="curFirstNode">First node of the current path</param>
            /// <returns>First node of the copied path</returns>
            static TrainpathNode DeepCopyOfLinkedNodes(TrainpathNode curFirstNode)
            {
                if (curFirstNode == null) return null;

                TrainpathNode newFirstNode = curFirstNode.ShallowCopyNoLinks();

                TrainpathNode curMainNode = curFirstNode;
                TrainpathNode newMainNode = newFirstNode;
                TrainpathNode curSidingNode = null;
                TrainpathNode newSidingNode = null;
                TrainpathNode newNextMainNode;
                while (curMainNode.NextMainNode != null)
                {
                    // in case there is a passing path, follow that first.
                    // At the end of the path, curSidingNode will be the main Node to link again to
                    if (curMainNode.NextSidingNode != null)
                    {
                        curSidingNode = curMainNode;
                        newSidingNode = newMainNode;
                        while (curSidingNode.NextSidingNode != null)
                        {
                            newSidingNode.NextSidingNode = curSidingNode.NextSidingNode.ShallowCopyNoLinks();
                            newSidingNode.NextSidingNode.PrevNode = newSidingNode;

                            curSidingNode = curSidingNode.NextSidingNode;
                            newSidingNode = newSidingNode.NextSidingNode;
                        }
                    }

                    if (curSidingNode == curMainNode.NextMainNode)
                    {
                        // We need to relink to the end of a siding path. The corresponding node has already been created
                        newNextMainNode = newSidingNode;
                        curSidingNode = null; // no linking needed anymore
                    }
                    else
                    {
                        newNextMainNode = curMainNode.NextMainNode.ShallowCopyNoLinks();
                    }
                    newNextMainNode.PrevNode = newMainNode;
                    newMainNode.NextMainNode = newNextMainNode;

                    curMainNode = curMainNode.NextMainNode;
                    newMainNode = newMainNode.NextMainNode;
                }

                return newFirstNode;
            }

        }
    }


}
