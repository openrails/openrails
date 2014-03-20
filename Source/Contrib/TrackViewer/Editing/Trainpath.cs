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
    /// </summary>
    public class Trainpath
    {
        MSTS.Formats.TrackDB trackDB;
        TSectionDatFile tsectionDat;
        
        /// <summary>Link to the first node of the path (starting point)</summary>
        public virtual TrainpathNode FirstNode { get; set; }

        /// <summary>Does the path have a real end node?</summary>
        public virtual bool HasEnd { get; set; }

        // adminstration of various path properties
        /// <summary>Full file path of the .pat file that contained the train path</summary>
        public string FilePath { get; set; }
        /// <summary>Name of the path as stored in the .pat file</summary>
        public string PathName { get; set; }
        /// <summary>Name of the start point as stored in the .pat file</summary>
        public string PathStart { get; set; }
        /// <summary>Name of the end point as stored in the .pat file</summary>
        public string PathEnd { get; set; }
        /// <summary>identification name as stored in the .pat file</summary>
        public string PathId { get; set; }
        /// <summary>Flags associated with the path (not the nodes)</summary>
        public PathFlags PathFlags { get; set; }

        /// <summary>
        /// Basic constructor creating an empty path, but storing track database and track section data
        /// </summary>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        public Trainpath(TrackDB trackDB, TSectionDatFile tsectionDat)
        {
            this.trackDB = trackDB;
            this.tsectionDat = tsectionDat;

            PathName = "<unknown>";
            PathEnd = "<unknown>";
            PathStart = "<unknown>";
            PathId = "new";
            HasEnd = false;
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
        {
            this.trackDB = trackDB;
            this.tsectionDat = tsectionDat;
            this.FilePath = filePath;
            
            PATFile patFile = new PATFile(filePath);
            PathId = patFile.PathID;
            PathName = patFile.Name;
            PathStart = patFile.Start;
            PathEnd = patFile.End;
            PathFlags = patFile.Flags;

            List<TrainpathNode> Nodes = new List<TrainpathNode>();
            createNodes(patFile, Nodes); 

            bool fatalerror = LinkNodes(patFile, Nodes);

            FindSidingEnds();
            FindNodeOrientations();

            if (fatalerror) Nodes = null; // invalid path - do not return any nodes
        }

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
            FirstNode.Type = TrainpathNodeType.Start;
        }

        /// <summary>
        /// Link the various nodes to each other. Do some initial processing on the path, like finding linking TVNs
        /// and determining whether junctions are facing or not.
        /// </summary>
        /// <param name="patFile">Patfile object containing the various unprocessed Track Path Nodes</param>
        /// <param name="Nodes">The list of as-of-yet unlinked processed path nodes</param>
        private bool LinkNodes(PATFile patFile, List<TrainpathNode> Nodes)
        {
            // Connect the various nodes to each other
            bool fatalerror = false;
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
                    if (node is TrainpathJunctionNode)
                    {
                        (node as TrainpathJunctionNode).SetFacingPoint(node.NextMainTvnIndex);
                    }
                    if (node.NextMainTvnIndex <= 0)
                    {
                        //node.NextMainNode = null;
                        //Trace.TraceWarning("Cannot find main track for node {1} in path {0}", FilePath, numberDrawn);
                        fatalerror = true;
                    }
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
                    if (node is TrainpathJunctionNode) { 
                        (node as TrainpathJunctionNode).SetFacingPoint(node.NextSidingTvnIndex); 
                    }
                    if (node.NextSidingTvnIndex < 0)
                    {
                        node.NextSidingNode = null;
                        //Trace.TraceWarning("Cannot find siding track for node {1} in path {0}", FilePath, numberDrawn);
                        fatalerror = true;
                    }
                }


                if (node.NextMainNode != null && node.NextSidingNode != null)
                    node.Type = TrainpathNodeType.SidingStart;
            }
            return fatalerror;
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
                    curSidingNode.Type = TrainpathNodeType.SidingEnd;
                    curSidingEnd = curSidingNode;
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
                currentMainNode.Type = TrainpathNodeType.End;
            }
            else
            {
                HasEnd = false;
            }

            //Determine direction of first point. By using second point, the direction will be reversed
            FirstNode.DetermineOrientation(FirstNode.NextMainNode, FirstNode.NextMainTvnIndex);
            TrainpathVectorNode FirstNodeAsVector = FirstNode as TrainpathVectorNode;
            FirstNodeAsVector.ForwardOriented = !FirstNodeAsVector.ForwardOriented;
        }

    }

    /// <summary>
    /// Class that extends TrainPath with History functions like Undo and redo.
    /// </summary>
    public class TrainpathWithHistory : Trainpath
    {
        List<TrainpathNode> firstNodes;  // list of firstnodes of complete paths
        List<bool> hasEnds; // list of booleans describing whether path does or does not have end
        int currentIndex; // current FirstNode and HasEnd are indexed by currentIndex

        /// <summary>Link to the first node of the train path</summary>
        public override TrainpathNode FirstNode
        {   // This needs to be extensive, because it is being used in base constructor, so before body of tonstructor of this class
            get
            {
                if (firstNodes == null) return null;
                return firstNodes[currentIndex];
            }
            set
            {
                if (firstNodes == null)
                {
                    firstNodes = new List<TrainpathNode>();
                    currentIndex = 0;
                    firstNodes.Add(value);
                }
                else
                {
                    firstNodes[currentIndex] = value;
                }
            }
        }

        /// <summary>Does the path have a well-defined end point or not</summary>
        public override bool HasEnd
        {   // This needs to be extensive, because it is being used in base constructor, so before body of tonstructor of this class
            get
            {
                if (hasEnds == null) return false;
                return hasEnds[currentIndex];
            }
            set
            {
                if (hasEnds == null)
                {
                    hasEnds = new List<bool>();
                    currentIndex = 0;
                    hasEnds.Add(value);
                }
                else
                {
                    hasEnds[currentIndex] = value;
                }
            }
        }

        /// <summary>
        /// Basic constructor creating an empty path, but storing track database and track section data
        /// </summary>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        public TrainpathWithHistory(TrackDB trackDB, TSectionDatFile tsectionDat)
            : base(trackDB, tsectionDat)
        {
        }

        /// <summary>
        /// Creates an trainpath from PAT file information.
        /// First creates all the nodes and then links them together into a main list
        /// with optional parallel siding list.
        /// </summary>
        /// <param name="trackDB"></param>
        /// <param name="tsectionDat"></param>
        /// <param name="filePath">file name including path of the .pat file</param>
        public TrainpathWithHistory(TrackDB trackDB, TSectionDatFile tsectionDat, string filePath)
            : base(trackDB, tsectionDat, filePath)
        {   // firstNodes, hasEnds and currentIndex are created in calls from base constructor to FirstNode and HasEnd
        }

        /// <summary>
        /// Undo operation. Pretty simple. Just use the previous path in the list if available.
        /// Note that all paths are retained.
        /// </summary>
        public void Undo()
        {
            if (firstNodes == null)
            {   // there is no path yet
                return;
            }

            currentIndex--;
            if (currentIndex < 0) { currentIndex = 0; }
        }

        /// <summary>
        /// Redo operation. Pretty simple. Just use the next path in the list if available.
        /// Note that all paths are retained.
        /// </summary>
        public void Redo()
        {
            if (firstNodes == null)
            {   // there is no path yet
                return;
            }

            currentIndex++;
            if (currentIndex >= firstNodes.Count) { currentIndex = firstNodes.Count - 1; }
        }

        /// <summary>
        /// Store the current path (without metadata), so we can undo later. 
        /// This is done by making a deep copy of path (as defined by nodes), and putting the copy in the list.
        /// This makes sure that the currently active references are still pointing to nodes in the current active path.
        /// </summary>
        public void StoreCurrentPath()
        {
            // first clear all possible stored-redo parts
            if (firstNodes == null)
            {   // there is no path yet
                return;
            }

            int newCount = currentIndex + 1;
            if (hasEnds.Count > newCount) {
                hasEnds.RemoveRange(newCount, hasEnds.Count - newCount);
            }
            if (firstNodes.Count > newCount)
            {
                firstNodes.RemoveRange(newCount, firstNodes.Count - newCount);
            }

            // insert copies just before current active one
            bool newHasEnd = HasEnd;
            TrainpathNode newFirstNode = DeepCopy(FirstNode);
            hasEnds.Insert(currentIndex, newHasEnd);
            firstNodes.Insert(currentIndex, newFirstNode);
            currentIndex++;
        }

        /// <summary>
        /// Perform a deep copy of a path consisting of linked nodes
        /// </summary>
        /// <param name="curFirstNode">First node of the current path</param>
        /// <returns>First node of the copied path</returns>
        static TrainpathNode DeepCopy(TrainpathNode curFirstNode)
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
