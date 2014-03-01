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
// TODOs
// === Not finished, so do before release
//
// ==== new/improved functionality
//
//  Dealing with broken nodes
//      being able to recognize them in the first place, and not abort on reading it.
//      How to draw tracks?
//      make it easy to reposition
//  Add mouse-click actions for
//      take other exit
//      edit wait time for uncouple?
//      move nodes like reverse
//      probably it makes sense to make 'rings' red when close enough, signifying mouse capture
//          and then changing, moving=dragging etc are all possible. Would be really sweet.
//
//  Improved handling
//      Take other exit for passing paths
//      Option to cut and reconnect a path manually (in case take other exit is not able to handle it)
//      Possibly 'Main TVN' is not the right one? I now use TrPins[1].Link
//
//  Issues
//      getting y correct might be an issue 

///
/// This is the main class that contains the editor actions and hence also the path modifications
/// Here we define all the possible actions that a user can take (most of which are available via a context menu coded elsewhere)
/// All the path modification logic is here, including adding and removing nodes, editing metadata, etc.
/// 
/// An important method is Draw. This will first of all find the active node and active trackLocation (if any). So this is
/// more an update-kind of action. But it depends on what is closest to the mouse and that is only known after drawing all
/// the tracks.
/// It will then draw the active node and locattion
/// 
/// Two other important methods are related to the context menu: PopupContextMenu and ExecuteAction
/// The latter basically selects the correct Action to take and exucutes that.
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS.TrackViewer.Drawing;

using MSTS.Formats;
using ORTS.Common;


namespace ORTS.TrackViewer.Editing
{
    public enum contextMenuAction
    {
        takeOtherExit,
        addReverse,
        addEnd,
        addWait,
        addUncouple,
        addPassingPath,
        separator1,
        removeReverse,
        removeWait,
        removeUncouple,
        removeSiding,
        removeEnd,
        separator2,
        editPoint,
        separator3,
        addStart,
        removeStart,
        otherStartDirection,        
    }

    public class PathEditor
    {
        bool oneTimeDisallowAdd = false; // in case we just removed a node, prevent adding an extra node during first redraw
        
        DrawTrackDB drawTrackDB; // We need to know what has been drawn, especially to get track closest to mouse
        TrackDB trackDB;
        TSectionDatFile tsectionDat;
        
        public TrainpathWithHistory trainpath;  // the path we will be editing  //todo remove public
        DrawPath drawPath;      // drawing of the path itself
        TrackNode[] trackNodes; // tracknodes from trackDB database.

        TrainpathNode activeNode;           // active Node (if present) for which actions can be performed
        TrainpathVectorNode activeTrackLocation;  // dynamic node that follows the mouse and is on track, but is not part of path

        Dictionary<contextMenuAction, string> contextMenuHeaders; //names shown in the contextMenu
        Dictionary<contextMenuAction, bool> contextMenuEnabled;  //is the specific contextMenu item enabled or not.

        EditorContextMenu editorContextMenu; // the context menu

        int numberToDraw = 4; // number of nodes to draw, start with a bit
        bool allowAddingNodes;  // if at end of path, do we allow adding a node to the path.

        int mouseX, mouseY;

        public bool EditingIsActive { get; set; } // Editing is active or not
        public bool EnableMouseUpdate { get; set; } // if context menu is open, updating active node and track is disabled

        public string fileName { get; set; } // name of the file with the .pat definition 
        public bool HasValidPath { get { return (trainpath.FirstNode != null); } }

        // some redirections to the drawPath
        public TrainpathNode CurrentNode { get { return drawPath.CurrentNode; } }
        public WorldLocation CurrentLocation { get { return CurrentNode.location; } }
       
        public PathEditor(DrawTrackDB drawTrackDB, ORTS.Menu.Path path)
        {
            this.drawTrackDB = drawTrackDB;
            this.trackDB = drawTrackDB.tdbFile.TrackDB;
            this.tsectionDat = drawTrackDB.tsectionDat;

            this.drawTrackDB = drawTrackDB;
            trackNodes = drawTrackDB.tdbFile.TrackDB.TrackNodes;

            fileName = path.FilePath.Split('\\').Last();
            trainpath = new TrainpathWithHistory(drawTrackDB.tdbFile.TrackDB, drawTrackDB.tsectionDat, path.FilePath);
            drawPath = new DrawPath(drawTrackDB, path);

            EditingIsActive = false;
            EnableMouseUpdate = true;

            activeTrackLocation = new TrainpathVectorNode(drawTrackDB.tdbFile.TrackDB, drawTrackDB.tsectionDat);
            CreateContextMenuEntries();
        }

        /// <summary>
        /// Fill the various contect menu items with appropriate headers and a default state.
        /// </summary>
        private void CreateContextMenuEntries()
        {
            if (! (contextMenuHeaders == null)) return;
            contextMenuHeaders = new Dictionary<contextMenuAction, string>();
            contextMenuHeaders[contextMenuAction.removeEnd]      = "Remove end point";
            contextMenuHeaders[contextMenuAction.removeReverse]  = "Remove reversal point";
            contextMenuHeaders[contextMenuAction.removeWait]     = "Remove wait point";
            contextMenuHeaders[contextMenuAction.removeUncouple] = "Remove (un)couple point";
            contextMenuHeaders[contextMenuAction.removeStart]    = "Remove start point";
            contextMenuHeaders[contextMenuAction.removeSiding]   = "Remove passing path";
            
            contextMenuHeaders[contextMenuAction.addEnd]      = "Place end point";
            contextMenuHeaders[contextMenuAction.addReverse]  = "Place reversal point";
            contextMenuHeaders[contextMenuAction.addWait]     = "Place wait point";
            contextMenuHeaders[contextMenuAction.addUncouple] = "Place (un)couple point";
            contextMenuHeaders[contextMenuAction.addStart]    = "Place start point";
            contextMenuHeaders[contextMenuAction.addPassingPath] = "Add passing path";

            contextMenuHeaders[contextMenuAction.editPoint] = "Edit point data";
            contextMenuHeaders[contextMenuAction.takeOtherExit] = "Take other exit";
            contextMenuHeaders[contextMenuAction.otherStartDirection] = "Change start direction";


            contextMenuEnabled = new Dictionary<contextMenuAction, bool>();
            foreach (contextMenuAction item in Enum.GetValues(typeof(contextMenuAction)))
            {
                contextMenuEnabled[item] = true; // Only default value;
            }

            editorContextMenu = new EditorContextMenu(this, contextMenuHeaders);
        }

        /// <summary>
        /// Set the state of the various items in the context Menu (is the item enabled or disabled).
        /// </summary>
        public void SetContextMenuState()
        {
            if (trainpath.FirstNode == null)
            {   // path is empty
                foreach (contextMenuAction action in Enum.GetValues(typeof(contextMenuAction)))
                {
                    contextMenuEnabled[action] = false;
                }
                contextMenuEnabled[contextMenuAction.addStart] = true;
                return;
            }

            contextMenuEnabled[contextMenuAction.addStart] = false;
            if (trainpath.HasEnd)
            {   // if there is an end, some things are simply not allowed
                contextMenuEnabled[contextMenuAction.removeEnd] = (activeNode.Type == TrainpathNodeType.End);
                contextMenuEnabled[contextMenuAction.addEnd] = false;
                contextMenuEnabled[contextMenuAction.addReverse] = false;
                contextMenuEnabled[contextMenuAction.removeReverse] = false; // removing a reverse point would not allow to have same end-point
                contextMenuEnabled[contextMenuAction.addStart] = false;
                contextMenuEnabled[contextMenuAction.otherStartDirection] = false;
                contextMenuEnabled[contextMenuAction.removeStart] = false;
            }
            else
            {
                contextMenuEnabled[contextMenuAction.removeEnd] = false;
                contextMenuEnabled[contextMenuAction.addEnd] = (activeTrackLocation.location != null
                                                            && !activeTrackLocation.PrevNode.HasSidingPath);
                contextMenuEnabled[contextMenuAction.addReverse] = (activeTrackLocation.location != null
                                                            && !activeTrackLocation.PrevNode.HasSidingPath);
                contextMenuEnabled[contextMenuAction.removeReverse] = (activeNode.Type == TrainpathNodeType.Reverse);
                contextMenuEnabled[contextMenuAction.takeOtherExit] = (activeNode.Type != TrainpathNodeType.SidingStart)
                                                            && (activeNode is TrainpathJunctionNode)
                                                            && (activeNode as TrainpathJunctionNode).IsFacingPoint
                                                            && (activeNode.NextMainNode != null);
                contextMenuEnabled[contextMenuAction.removeStart] = (activeNode == trainpath.FirstNode);
                contextMenuEnabled[contextMenuAction.otherStartDirection] = (activeNode == trainpath.FirstNode);
            }

            //not related to end-of-path
            contextMenuEnabled[contextMenuAction.removeSiding] = (activeNode.Type == TrainpathNodeType.SidingStart);
            contextMenuEnabled[contextMenuAction.removeWait] = (activeNode.Type == TrainpathNodeType.Stop);
            contextMenuEnabled[contextMenuAction.removeUncouple] = (activeNode.Type == TrainpathNodeType.Uncouple);
            contextMenuEnabled[contextMenuAction.addWait] = (activeTrackLocation.location != null
                                                            && !activeTrackLocation.PrevNode.HasSidingPath);
            contextMenuEnabled[contextMenuAction.addUncouple] = (activeTrackLocation.location != null
                                                            && !activeTrackLocation.PrevNode.HasSidingPath);
            contextMenuEnabled[contextMenuAction.editPoint] = (activeNode.Type == TrainpathNodeType.Stop
                                                            || activeNode.Type == TrainpathNodeType.Uncouple);

            //items for which the calculations are more complex.
            contextMenuEnabled[contextMenuAction.takeOtherExit]  = canTakeOtherExit(false);
            contextMenuEnabled[contextMenuAction.addPassingPath] = canTakeOtherExit(true);

        }

        /// <summary>
        /// Figure out whether it is possible to take other exit from active node
        /// </summary>
        /// <param name="needToReconnect">boolean describing whether a reconnect is needed (instead of just really going somewhere else.</param>
        bool canTakeOtherExit(bool needToReconnect)
        {
            TrainpathJunctionNode activeNodeAsJunction = activeNode as TrainpathJunctionNode;

            //basic requirements
            if (   activeNodeAsJunction == null 
                || activeNode.NextMainNode == null
                || !activeNodeAsJunction.IsFacingPoint
                || (activeNode.Type == TrainpathNodeType.SidingStart)
                )
            {
                return false;
            }

            if (trainpath.HasEnd || needToReconnect)
            {   // see if we can reconnect without affecting the end-point
                int newTVN = GetOtherExitTVNindex(activeNodeAsJunction);
                TrainpathJunctionNode reconnectNode = FindReconnectNode(activeNodeAsJunction, newTVN);
                return (reconnectNode != null);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Callback to execute one of the actions clicked by the user.
        /// </summary>
        /// <param name="e">object received from the contextMenu, should be a contextMenuAction</param>
        public void ExecuteAction(object e)
        {
            trainpath.StoreCurrentPath();
            contextMenuAction action = (contextMenuAction)e;
            switch (action)
            {
                case contextMenuAction.removeEnd:
                    ActionRemoveEndPoint();
                    break;
                case contextMenuAction.removeWait:
                case contextMenuAction.removeUncouple:
                    ActionRemoveWaitUncouplePoint();
                    break;
                case contextMenuAction.removeReverse:
                    ActionRemoveReversePoint();
                    break;
                case contextMenuAction.removeSiding:
                    ActionRemoveSidingPath();
                    break;
                case contextMenuAction.addEnd:
                    ActionAddEndPoint();
                    break;
                case contextMenuAction.addReverse:
                    ActionAddReversePoint();
                    break;
                case contextMenuAction.addWait:
                    ActionAddWaitPoint();
                    break;
                case contextMenuAction.addUncouple:
                    ActionAddUncouplePoint();
                    break;
                case contextMenuAction.takeOtherExit:
                    ActionTakeOtherExit();
                    break;
                case contextMenuAction.addPassingPath:
                    ActionAddPassingPath();
                    break;
                case contextMenuAction.addStart:
                    ActionAddStartPoint();
                    break;
                case contextMenuAction.removeStart:
                    ActionRemoveStartPoint();
                    break;
                case contextMenuAction.otherStartDirection:
                    ActionOtherStartDirection();
                    break;
                case contextMenuAction.editPoint:
                    ActionEditPointData(activeNode as TrainpathVectorNode);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Find the active node and active node candidate and draw them
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        public void Draw (DrawArea drawArea)
        {
            //List of main-track nodes that were actually drawn and can therefore be selected for editing
            List<TrainpathNode> drawnNodes = new List<TrainpathNode>();
            
            //Keys are tracknode indices, value is a list of (main) track node (in pairs) that are both
            // on the tracknode and the path between them has been drawn
            Dictionary<int,List<TrainpathNode>> drawnTrackIndices = new Dictionary<int,List<TrainpathNode>>();

            int numberDrawn = drawPath.Draw(drawArea, trainpath.FirstNode, numberToDraw, drawnNodes, drawnTrackIndices);

            if (!EditingIsActive)
            {
                return;
            }

            if (numberDrawn < numberToDraw)
            {
                // Apparently we were not able to draw all nodes. Reset maximum number to draw, and possibly add a node
                numberToDraw = numberDrawn;
                if (EditingIsActive && allowAddingNodes && !(CurrentNode.Type == TrainpathNodeType.End) )
                {
                    if (AddAdditionalNode(CurrentNode, true) != null)
                    {
                        numberToDraw = numberDrawn + 1;
                    }
                }
            }


            if (EnableMouseUpdate)
            {
                FindActiveNode(drawArea, drawnNodes);
                FindActiveTrackLocation(drawnTrackIndices);
            }

            if (activeNode != null)
            {
                drawArea.DrawSimpleTexture(activeNode.location, "ring", 4f, 3, DrawColors.colorsNormal["activeNode"]);
                
            }
            if (activeTrackLocation != null && activeTrackLocation.location !=null)
            {
                drawArea.DrawSimpleTexture(activeTrackLocation.location, "ring", 4f, 3, DrawColors.colorsNormal["nodeCandidate"]);   
            }

        }

        /// <summary>
        /// Find the node in the path that is closest to the mouse,
        /// </summary>
        /// <param name="drawArea">Area that is being drawn upon and where we have a mouse location</param>
        void FindActiveNode(DrawArea drawArea, List<TrainpathNode> drawnMainNodes)
        {
            // Initial simplest implementation: find simply the closest and first.
            float closestMouseDistanceSquared = float.MaxValue;
            TrainpathNode closestNode = null;
            foreach (TrainpathNode node in drawnMainNodes)
            {
                float distanceSquared = CloseToMouse.GetGroundDistanceSquared(node.location, drawArea.mouseLocation);
                // by using '<=' instead of '<' we should get the latest one, which overrides earlier ones
                if (distanceSquared <= closestMouseDistanceSquared)
                {
                    closestMouseDistanceSquared = distanceSquared;
                    closestNode = node;
                }
            }

            activeNode = closestNode;
        }

        /// <summary>
        /// Find the location on the track that can be used as a new end or wait node (or possibly start)
        /// </summary>
        /// <param name="drawnTrackIndices"></param>
        void FindActiveTrackLocation(Dictionary<int, List<TrainpathNode>> drawnTrackIndices)
        {

            uint tni = drawTrackDB.closestTrack.TrackNode.Index;
            int tni_int = (int)tni;

            if (!drawnTrackIndices.ContainsKey(tni_int) && trainpath.FirstNode!=null)
            {
                activeTrackLocation.location = null;
                return;
            }

            int tvsi = drawTrackDB.closestTrack.TrackVectorSectionIndex;
            float distance = drawTrackDB.closestTrack.DistanceAlongTrack;

            // find location
            WorldLocation location = drawTrackDB.FindLocation(tni, tvsi, distance);

            // fill the properties of the activeTrackLocation 
            activeTrackLocation.TVNIndex = tni_int;
            activeTrackLocation.trackVectorSectionIndex = tvsi;
            activeTrackLocation.trackSectionOffset = distance;
            activeTrackLocation.location = location;

            if (trainpath.FirstNode != null)
            {   //Only in case this is not the first path.
                TrainpathNode prevNode = FindPrevNodeOfActiveTrack(drawnTrackIndices[tni_int]);
                if (prevNode == null)
                {
                    activeTrackLocation.location = null;
                }
                else
                {
                    activeTrackLocation.PrevNode = prevNode;
                }
            }
        }

        /// <summary>
        /// The activeTrackLocation needs to be between two nodes on the track, if it is to be valid.
        /// Here we find whether indeed it is. 
        /// </summary>
        /// <param name="nodesOnTrack">Nodes on the current track, from which the paths continues along the current track.</param>
        /// <returns>The node before</returns>
        TrainpathNode FindPrevNodeOfActiveTrack(List<TrainpathNode> nodesOnTrack)
        {
            // We are given a set of nodes. We know that a part of the path was drawn following those nodes
            // All of these parts are on the same track.
            // The nodes are ordered in pairs.
            for (int i = nodesOnTrack.Count - 1; i > 0; i -= 2)
            {
                if (activeTrackLocation.IsBetween(nodesOnTrack[i-1], nodesOnTrack[i])) {
                    return nodesOnTrack[i-1];
                }
            }
            return null;
        }

        /// <summary>
        /// Popup the context menu at the given location. Also disable updates related to mouse movement while menu is open.
        /// </summary>
        /// <param name="mouseX"></param>
        /// <param name="mouseY"></param>
        public void PopupContextMenu(int mouseX, int mouseY)
        {
            SetContextMenuState();
            this.mouseX = mouseX;
            this.mouseY = mouseY;
            editorContextMenu.PopupContextMenu(mouseX, mouseY, contextMenuHeaders, contextMenuEnabled);
        }

        /// <summary>
        /// Remove the current active point and all of the path that follows
        /// </summary>
        void RemoveNodeAndAllFollowing(TrainpathNode firstNodeToRemove)
        {
            if (firstNodeToRemove == null)
            {
                return;
            }
            // assumption is that there is no siding next to this point.
            TrainpathNode prevNode = firstNodeToRemove.PrevNode;
            prevNode.NextMainNode = null;
            prevNode.NextSidingNode = null; // should not be needed
            prevNode.NextSidingTVNIndex = 0;
            // Since we already know the TVN, simply add a node (meaning removing the end will extend the path. 
            AddAdditionalNode(prevNode, prevNode.NextMainTVNIndex, true);
        }

        /// <summary>
        /// Remove the 'end' node. Note that this is not the last node during editing, but only the node
        /// that really has been set as node.
        /// </summary>
        void ActionRemoveEndPoint()
        {
            RemoveNodeAndAllFollowing(activeNode);
            trainpath.HasEnd = false;
            oneTimeDisallowAdd = true;
        }

        /// <summary>
        /// Remove a reverse node. This will remove also all of the rest of the path.
        /// </summary>
        void ActionRemoveReversePoint()
        {
            RemoveNodeAndAllFollowing(activeNode);
            oneTimeDisallowAdd = true;
        }

        /// <summary>
        /// Remove start node and subsequently the complete path. (only path metadata will be unchanged!)
        /// </summary>
        void ActionRemoveStartPoint()
        {
            RemoveNodeAndAllFollowing(activeNode.NextMainNode);
            activeNode.NextMainNode = null;
            activeNode.NextSidingNode = null;
            trainpath.FirstNode = null;
        }

        /// <summary>
        /// Remove a wait or uncouple node. 
        /// </summary>
        void ActionRemoveWaitUncouplePoint()
        {
            //Assumption, there is no siding next to it. but this might not be true. Not clear if the assumption is needed
            if (activeNode.NextMainNode != null)
            {
                RemoveIntermediatePoint(activeNode as TrainpathVectorNode);
            }
            else
            {
                RemoveNodeAndAllFollowing(activeNode);
            }
        }

        /// <summary>
        /// Add an endpoint at the current location
        /// </summary>
        void ActionAddEndPoint()
        {
            //activeTrackLocation is the place where we want to place the node
            //Its field lastNodeSidingPath is also given.
            TrainpathNode lastNode = activeTrackLocation.PrevNode;

            //remove basically rest of path.
            lastNode.NextSidingNode = null;
            lastNode.NextMainNode   = null;

            TrainpathVectorNode newNode = AddAdditionalVectorNode(lastNode, activeTrackLocation, true);
            newNode.Type = TrainpathNodeType.End;
            trainpath.HasEnd = true;
        }

        /// <summary>
        /// Add the startpoint at the current location, add one additional node
        /// </summary>
        void ActionAddStartPoint()
        {
            trainpath.FirstNode = new TrainpathVectorNode(activeTrackLocation);
            trainpath.FirstNode.Type = TrainpathNodeType.Start;
            AddAdditionalNode(trainpath.FirstNode, true);
            oneTimeDisallowAdd = false; // we do want to add an extra node if possible
        }

        /// <summary>
        /// Change the direction going from the 
        /// </summary>
        void ActionOtherStartDirection()
        {
            RemoveNodeAndAllFollowing(activeNode.NextMainNode);
            TrainpathVectorNode firstNode = trainpath.FirstNode as TrainpathVectorNode;
            firstNode.ForwardOriented = !firstNode.ForwardOriented;
            AddAdditionalNode(trainpath.FirstNode, true);
        }

        /// <summary>
        /// Add an reverse point at the current location
        /// </summary>
        void ActionAddReversePoint()
        {
            TrainpathNode lastNode = activeTrackLocation.PrevNode;
            RemoveNodeAndAllFollowing(lastNode.NextMainNode);

            TrainpathVectorNode newNode = AddAdditionalVectorNode(lastNode, activeTrackLocation, true);
            newNode.Type = TrainpathNodeType.Reverse;
            lastNode.NextMainNode = newNode;
            lastNode.NextSidingNode = null; // should not be needed
            lastNode.NextMainTVNIndex = newNode.TVNIndex;
            newNode.PrevNode = lastNode;

            newNode.ForwardOriented = !newNode.ForwardOriented; // reverse because, well, this is a reverse point.
        }
        
        /// <summary>
        /// Add a waitpoint at the current location
        /// </summary>
        void ActionAddWaitPoint()
        {
            TrainpathVectorNode newNode = AddIntermediateNode(activeTrackLocation);
            newNode.Type = TrainpathNodeType.Stop;
            ActionEditPointData(newNode);
        }

        /// <summary>
        /// Add an uncouple node at the current location
        /// </summary>
        void ActionAddUncouplePoint()
        {
            TrainpathVectorNode newNode = AddIntermediateNode(activeTrackLocation);
            newNode.Type = TrainpathNodeType.Uncouple;
            ActionEditPointData(newNode);
        }

        /// <summary>
        /// Popup a dialog to edit the metadata of a node (waitpoint, uncouple point)
        /// </summary>
        /// <param name="nodeToEdit">The node for which the metadata needs to be edited</param>
        private void ActionEditPointData(TrainpathVectorNode nodeToEdit)
        {
            switch (nodeToEdit.Type)
            {
                case TrainpathNodeType.Stop:
                    if (nodeToEdit.WaitTimeS == 0 && nodeToEdit.WaitUntil == 0)
                    {
                        nodeToEdit.WaitUntil = 12 * 3600; // some initial value: midday
                    }
                    WaitPointDialog waitDialog = new WaitPointDialog(mouseX, mouseY, nodeToEdit.WaitTimeS, nodeToEdit.WaitUntil);
                    if (waitDialog.ShowDialog() == true)
                    {
                        if (waitDialog.UntilSelected())
                        {
                            nodeToEdit.WaitUntil = waitDialog.GetWaitTime();
                        }
                        else
                        {
                            nodeToEdit.WaitTimeS = waitDialog.GetWaitTime();
                        }
                    }
                    break;
                case TrainpathNodeType.Uncouple:
                    if (nodeToEdit.NCars == 0)
                    {
                        nodeToEdit.NCars = 1;
                        nodeToEdit.WaitTimeS = 60;
                    }
                    UncouplePointDialog uncoupleDialog = new UncouplePointDialog(mouseX, mouseY, nodeToEdit.NCars, nodeToEdit.WaitTimeS);
                    if (uncoupleDialog.ShowDialog() == true)
                    {
                        nodeToEdit.NCars = uncoupleDialog.GetNCars();
                        nodeToEdit.WaitTimeS = uncoupleDialog.GetWaitTime();
                    }
                    break;
            }


        }

        /// <summary>
        /// Remove a siding path starting here
        /// </summary>
        void ActionRemoveSidingPath()
        {
            TrainpathNode mainNode = activeNode;
            mainNode.Type = TrainpathNodeType.Other; // this was SidingStart
            mainNode.NextSidingNode = null;

            //make sure the main path does no longer show it has a siding path.
            while (mainNode.Type != TrainpathNodeType.SidingEnd)
            {
                mainNode.HasSidingPath = false;
                mainNode = mainNode.NextMainNode;
            }

            mainNode.Type = TrainpathNodeType.Other; // this was the SidingEnd
        }

        /// <summary>
        /// Take the other exit at the current junction
        /// </summary>
        void ActionTakeOtherExit()
        {
            TrainpathJunctionNode junctionNode = activeNode as TrainpathJunctionNode;
            int newTVNindex = GetOtherExitTVNindex(junctionNode);
            TrainpathJunctionNode reconnectNode = FindReconnectNode(junctionNode, newTVNindex);

            if (reconnectNode == null)
            {   //really take other exit and discard rest of path
                junctionNode.NextMainTVNIndex = newTVNindex;
                RemoveNodeAndAllFollowing(activeNode.NextMainNode);
                return;
            }

            //we can reconnect, so create a path to reconnection point
            TrainpathNode lastNodeNewPath = CreatePartialPath(activeNode, newTVNindex, reconnectNode, true);

            //Reconnect.
            lastNodeNewPath.NextMainNode = reconnectNode;
            reconnectNode.PrevNode = lastNodeNewPath;
        }

        /// <summary>
        /// Add a passing path starting at active junction
        /// </summary>
        void ActionAddPassingPath()
        {
            TrainpathJunctionNode junctionNode = activeNode as TrainpathJunctionNode;
            int newTVNindex = GetOtherExitTVNindex(junctionNode);
            TrainpathJunctionNode reconnectNode = FindReconnectNode(junctionNode, newTVNindex);
            //we have checked that we can reconnect before, so reconnectNode cannot be zero.

            activeNode.Type = TrainpathNodeType.SidingStart;
            TrainpathNode lastNodeSidingPath = CreatePartialPath(activeNode, newTVNindex, reconnectNode, false);

            //reconnect. At this point, newNode is already the same node as reconnectNode. We will discard it
            lastNodeSidingPath.NextSidingNode = reconnectNode;
            reconnectNode.Type = TrainpathNodeType.SidingEnd;
        }

        /// <summary>
        /// Create a partial path from the current node, along the new track vector index, until we can reconnect again
        /// </summary>
        /// <param name="currentNode">Starting place of the new partial path</param>
        /// <param name="newTVNindex">Index of the new track vector node along which the path starts</param>
        /// <param name="reconnectNode">Node at we will reconnect to current path again</param>
        /// <param name="IsMainPath">Do we add the node to the main path or not</param>
        /// <returns>The last node on the partial path, just before the reconnect node.</returns>
        TrainpathNode CreatePartialPath(TrainpathNode currentNode, int newTVNindex, TrainpathJunctionNode reconnectNode, bool IsMainPath)
        {
            TrainpathNode newNode = AddAdditionalNode(currentNode, newTVNindex, IsMainPath);
            do
            {
                TrainpathJunctionNode newNodeAsJunction = newNode as TrainpathJunctionNode;
                if (newNodeAsJunction != null && (newNodeAsJunction.junctionIndex == reconnectNode.junctionIndex))
                {
                    //we have reached the reconnection point
                    break;
                }
                currentNode = newNode;
                newNode = AddAdditionalNode(currentNode, IsMainPath);

            } while (newNode != null); // if we get here, something is wrong, because we checked we could reconnect

            // The returned node will not be the last node created, because that one is at the same location as the reconnect node
            return currentNode; 
        }
 
        /// <summary>
        /// Add a new vector node at the given location in the middle of a path
        /// </summary>
        /// <param name="nodeCandidate"></param>
        /// <returns></returns>
        TrainpathVectorNode AddIntermediateNode (TrainpathVectorNode nodeCandidate)
        {
            TrainpathNode prevNode = nodeCandidate.PrevNode;
            TrainpathNode nextNode = prevNode.NextMainNode;
            
            TrainpathVectorNode newNode = AddAdditionalVectorNode(prevNode, nodeCandidate, true);
            
            prevNode.NextMainNode = newNode;
            prevNode.NextSidingNode = null; // should not be needed
            prevNode.NextMainTVNIndex = newNode.TVNIndex;
            newNode.PrevNode = prevNode;

            newNode.NextMainNode = nextNode;
            newNode.NextSidingNode = null; // should not be needed
            nextNode.PrevNode = newNode;


            //remove possible ambiguity node 
            if (prevNode.Type == TrainpathNodeType.Other && prevNode is TrainpathVectorNode)
            {
                RemoveIntermediatePoint(prevNode as TrainpathVectorNode);
            }
            if (nextNode.Type == TrainpathNodeType.Other && nextNode is TrainpathVectorNode)
            {
                RemoveIntermediatePoint(nextNode as TrainpathVectorNode);
            }

            return newNode;
        }

        /// <summary>
        /// Remove a intermediate vector node. Possibly add a ambiguity node if needed
        /// </summary>
        /// <param name="curNode">Node to be removed</param>
        void RemoveIntermediatePoint(TrainpathVectorNode curNode)
        {
            TrainpathNode prevNode = curNode.PrevNode;
            prevNode.NextMainNode = curNode.NextMainNode;
            prevNode.NextSidingNode = null; // should not be needed
            //lastNodeSidingPath.NextMainTVNIndex should be the same still
            if (prevNode.NextMainNode != null)
            {   // there might not be a next node.
                prevNode.NextMainNode.PrevNode = prevNode;
            }

            //Check if we need to add an ambiguity node
            if ((prevNode is TrainpathJunctionNode) && (prevNode.NextMainNode is TrainpathJunctionNode))
            {
                TrainpathJunctionNode prevJunctionNode = prevNode as TrainpathJunctionNode;
                if (IsSimpleSidingStart(prevJunctionNode))
                {
                    TrainpathVectorNode halfwayNode = CreateHalfWayNode(prevJunctionNode, prevJunctionNode.NextMainTVNIndex);
                    halfwayNode.PrevNode = prevNode;
                    AddIntermediateNode(halfwayNode);
                }
            }
        }

        /// <summary>
        /// Add an additional node, where the next track node is not yet given
        /// </summary>
        /// <param name="lastNode">currently last node</param>
        /// <param name="IsMainPath">Do we add the node to the main path or not</param>
        /// <returns>The newly created (unlinked) path node</returns>
        public TrainpathNode AddAdditionalNode(TrainpathNode lastNode, bool IsMainPath)
        {
            if (oneTimeDisallowAdd == true)
            {
                oneTimeDisallowAdd = false;
                return null;
            }

            if (lastNode is TrainpathVectorNode)
            {
                return AddAdditionalNode(lastNode, (lastNode as TrainpathVectorNode).TVNIndex, IsMainPath);
            }

            TrainpathJunctionNode junctionNode = lastNode as TrainpathJunctionNode;
            TrackNode trackNode = trackNodes[junctionNode.junctionIndex];
            if (trackNode.TrEndNode) return null;  // if it happens to be the end of a path, forget about it.

            if (junctionNode.IsFacingPoint)
            {
                return AddAdditionalNode(lastNode, trackNode.MainTVN(), IsMainPath);
            }
            else
            {
                return AddAdditionalNode(lastNode, trackNode.TrailingTVN(), IsMainPath);
            }
        }

        /// <summary>
        /// Add an additional node starting at the given node, following the TVNIndex,
        /// but take care of a possible need for disambiguity
        /// </summary>
        /// <param name="activeNodeAsJunction">activeNodeAsJunction to start</param>
        /// <param name="TVNIndex">TrackVectorNode index of the track the path needs to be on</param>
        /// <param name="IsMainPath">Do we add the node to the main path or not</param>
        /// <returns>The newly created path node</returns>
        TrainpathNode AddAdditionalNode(TrainpathNode lastNode, int TVNIndex, bool IsMainPath)
        {
            if (lastNode is TrainpathVectorNode)
            {
                return AddAdditionalJunctionNode(lastNode, (lastNode as TrainpathVectorNode).TVNIndex, IsMainPath);
            }

            TrainpathJunctionNode junctionNode = lastNode as TrainpathJunctionNode;
            if (IsSimpleSidingStart(junctionNode))
            {   // start of a simple siding. So the next node should be a node to remove ambiguity.
                TrainpathVectorNode halfwayNode = CreateHalfWayNode(junctionNode, TVNIndex);
                return AddAdditionalVectorNode(junctionNode, halfwayNode, IsMainPath);
            }
            else
            {
                return AddAdditionalJunctionNode(junctionNode, TVNIndex, IsMainPath);
            }
        }
        
        /// <summary>
        /// Add an additional node, from the current last node along the next TrackNodeVector (given by index)
        /// AdditionalNode will always be a activeNodeAsJunction.
        /// </summary>
        /// <param name="lastNode">Currently last node of path</param>
        /// <param name="nextTVNIndex">TrackNodeVector index along which to place the track</param>
        /// <returns>The newly created junction path node</returns>
        TrainpathJunctionNode AddAdditionalJunctionNode(TrainpathNode lastNode, int nextTVNIndex, bool IsMainPath)
        {
            // we add a new activeNodeAsJunction
            TrainpathJunctionNode newNode = new TrainpathJunctionNode(lastNode);

            TrackNode linkingTrackNode = trackNodes[nextTVNIndex];
            if (linkingTrackNode == null)
            {
                return null; // apparently there is some issue in the track.
            }

            newNode.junctionIndex = GetNextJunctionIndex(lastNode, nextTVNIndex);
            TrackNode newJunctionTrackNode = trackNodes[newNode.junctionIndex];
            newNode.location = DrawTrackDB.UiDLocation(newJunctionTrackNode.UiD);

            // simple linking
            if (IsMainPath)
            {
                lastNode.NextMainTVNIndex = nextTVNIndex;
                lastNode.NextMainNode = newNode;
                newNode.PrevNode = lastNode;
            }
            else
            {
                lastNode.NextSidingTVNIndex = nextTVNIndex;
                lastNode.NextSidingNode = newNode;
            }

             newNode.SetFacingPoint(GetLeavingTVNIndex(newNode.junctionIndex,nextTVNIndex)); //setfacing point works on nextmainTVNindex
             newNode.determineOrientation(lastNode, nextTVNIndex);

            return newNode;
        }

        /// <summary>
        /// Add a new vector path node at the location of nodeCandidate.
        /// </summary>
        /// <param name="lastNode">node that will be predecessor of the new nodeCandidate</param>
        /// <param name="nodeCandidate">partial trainpath vector node describing the current mouse location</param>
        /// <param name="IsMainPath">Do we add the node to the main path or not</param>
        /// <returns>The newly created vector node</returns>
        TrainpathVectorNode AddAdditionalVectorNode(TrainpathNode lastNode, TrainpathVectorNode nodeCandidate, bool IsMainPath)
        {
            // we add a new activeNodeAsJunction
            TrainpathVectorNode newNode = new TrainpathVectorNode(nodeCandidate);

            newNode.determineOrientation(lastNode, newNode.TVNIndex);   

            // simple linking
            if (IsMainPath)
            {
                lastNode.NextMainTVNIndex = newNode.NextMainTVNIndex;
                lastNode.NextMainNode = newNode;
                newNode.PrevNode = lastNode;
            }
            else
            {
                lastNode.NextSidingTVNIndex = newNode.NextMainTVNIndex;
                newNode .NextSidingTVNIndex = newNode.NextMainTVNIndex;
                lastNode.NextSidingNode = newNode;
            }

            return newNode;
        }

        /// <summary>
        /// Create a (still unlinked) node halfway through the next section (so halfway between this
        /// and the next junction. Needed specially for disambiguity.
        /// </summary>
        /// <param name="activeNodeAsJunction">The junction node where we start</param>
        /// <param name="TVNIndex">The TrackVectorNode index for the path</param>
        /// <returns>An unlinked vectorNode at the midpoint.</returns>
        TrainpathVectorNode CreateHalfWayNode(TrainpathJunctionNode junctionNode, int TVNIndex)
        {   // The idea here is to use all the code in traveller to make life easier.

            Traveller traveller = junctionNode.placeTravellerAfterJunction(TVNIndex);
            
            TrackNode nextJunctionTrackNode = trackNodes[GetNextJunctionIndex(junctionNode, TVNIndex)];
            WorldLocation nextJunctionLocation = DrawTrackDB.UiDLocation(nextJunctionTrackNode.UiD);
                        
            //move the traveller halfway through the next vector section
            float distanceToTravel = traveller.DistanceTo(nextJunctionLocation) / 2;
            traveller.Move(distanceToTravel);

            TrainpathVectorNode halfwayNode = new TrainpathVectorNode(junctionNode, traveller);
            halfwayNode.determineOrientation(junctionNode, TVNIndex);

            return halfwayNode;
        }

        /// <summary>
        /// Determine whether we can reconnect from the current junction node to the main track, if we take 
        /// the new TVNindex
        /// </summary>
        /// <param name="activeNodeAsJunction">activeNodeAsJunction which is a facing point</param>
        /// <param name="newTVNindex">index of the track vector node that is intended to be used for reconnection</param>
        /// <returns></returns>
        TrainpathJunctionNode FindReconnectNode(TrainpathJunctionNode startJunctionNode, int newTVNindex)
        {
            //First find the list of nodes we might be able to reconnect to. They should all be junction nodes
            List<TrainpathJunctionNode> reconnectNodes = FindReconnectNodeCandidates(startJunctionNode);

            //next we need to follow the new path from the newTVNindex, and see whether we can reconnect
            int junctionIndex = startJunctionNode.junctionIndex;
            int TVNindex = newTVNindex;
            int junctionsToCheck = 10;
            while (junctionsToCheck > 0)
            {
                int nextJunctionIndex = GetNextJunctionIndex(junctionIndex, TVNindex);
                foreach (TrainpathJunctionNode reconnectNode in reconnectNodes)
                {
                    if (reconnectNode.junctionIndex == nextJunctionIndex)
                    {   // we found a link, but we still need to check if we don't connect from the trailing side
                        TrackNode junctionTrackNode = trackNodes[nextJunctionIndex];
                        if (TVNindex == junctionTrackNode.TrailingTVN())
                        {   // too bad, we reconnect from the wrong side. It also does not make sense to continue searching
                            return null;
                        }
                        else
                        {   // ok, we found the reconnecting node
                            return reconnectNode;
                        }
                    }
                }
                junctionIndex = nextJunctionIndex;
                TVNindex = GetLeavingTVNIndex(junctionIndex, TVNindex);
                if (TVNindex < 0) {
                    return null;
                }
                junctionsToCheck--;
            }
            return null;
        }

        /// <summary>
        /// Find the nodes that can be used to relink either a siding path, or a 'take-other-exit' path.
        /// The reconnecing nodes all have to be before the first special node (wait, uncouple, reverse, end).
        /// They also have to be before the end of the (current path), even if it does not have a formal end,
        /// and they have to be before a possible next siding start.
        /// At last, it needs to be a non-facing junction.
        /// </summary>
        /// <param name="startJunctionNode">Junction node on train path to start searching</param>
        /// <returns>List of possible reconnect nodes. Might be empty</returns>
        List<TrainpathJunctionNode> FindReconnectNodeCandidates(TrainpathJunctionNode startJunctionNode)
        {
            List<TrainpathJunctionNode> reconnectNodeCandidates = new List<TrainpathJunctionNode>();
            TrainpathNode mainNode = startJunctionNode.NextMainNode;
            //follow the train path and see what we find
            while (mainNode != null)
            {
                if (mainNode is TrainpathJunctionNode)
                {
                    if (mainNode.Type == TrainpathNodeType.SidingStart)
                    {   // if a new siding path is started, stop searching
                        break;
                    }
                    TrainpathJunctionNode mainNodeAsJunction = mainNode as TrainpathJunctionNode;
                    if (!mainNodeAsJunction.IsFacingPoint)
                    {   // add the trailing junction.
                        reconnectNodeCandidates.Add(mainNodeAsJunction);
                    }
                }
                else
                {
                    if (mainNode.Type != TrainpathNodeType.Other)
                    {   // if it is not an other-node (hence disambiguity node), stop searching
                        break;
                    }
                }
                mainNode = mainNode.NextMainNode;
            }
            return reconnectNodeCandidates;
        }
        
        /// <summary>
        /// From the current pathnode and the linking tracknode, fin the junctionIndex of the next junction (or possibly end-point)
        /// </summary>
        /// <param name="node">The current node</param>
        /// <param name="linkingTrackNodeIndex">The index of the tracknode leaving the node</param>
        /// <returns>The index of the junction index at the end of the track (as seen from the node)</returns>
        private int GetNextJunctionIndex(TrainpathNode node, int linkingTrackNodeIndex)
        {
            if (node is TrainpathJunctionNode)
            {
                return GetNextJunctionIndex((node as TrainpathJunctionNode).junctionIndex, linkingTrackNodeIndex);
            }

            TrackNode linkingTrackNode = trackNodes[linkingTrackNodeIndex];
            return (node as TrainpathVectorNode).ForwardOriented
                ? linkingTrackNode.JunctionIndexAtEnd()
                : linkingTrackNode.JunctionIndexAtStart();
        }

        /// <summary>
        /// Get the index of the junction node at the other side of the linking track vector node.
        /// This uses only the track database, no trainpath nodes.
        /// </summary>
        /// <param name="junctionIndex">Index of this junction node</param>
        /// <param name="linkingTrackNodeIndex">index of the vector node linking the two junction nodes</param>
        /// <returns>The index of the junctin node at the other end</returns>
        int GetNextJunctionIndex(int junctionIndex, int linkingTrackNodeIndex)
        {
            TrackNode linkingTrackNode = trackNodes[linkingTrackNodeIndex];
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
        /// <param name="incomingTVNindex">index of incoming vector node</param>
        /// <returns>index of the leaving vector node or -1 if none found.</returns>
        int GetLeavingTVNIndex(int junctionIndex, int incomingTVNindex)
        {
            TrackNode junctionTrackNode = trackNodes[junctionIndex];
            if (junctionTrackNode.TrEndNode)
            {
                return -1;
            }
            if (incomingTVNindex == junctionTrackNode.TrailingTVN())
            {
                return junctionTrackNode.MainTVN();
            }
            return junctionTrackNode.TrailingTVN();
        }

        /// <summary>
        /// For a junction with two exits, get the index of the vector node not being used at the moment
        /// </summary>
        /// <param name="junctionNode">Node for which to find the other exit</param>
        /// <returns>The index of the other leaving vector node.</returns>
        int GetOtherExitTVNindex(TrainpathJunctionNode junctionNode)
        {
            TrackNode trackNode = trackNodes[junctionNode.junctionIndex];
            if (junctionNode.NextMainTVNIndex == trackNode.MainTVN())
            {
                return trackNode.SidingTVN();
            }
            else
            {
                return trackNode.MainTVN();
            }
        }

        /// <summary>
        /// Determine whether this junction node is the start of a simple siding (meaning a siding start, 
        /// where the two tracks meet at the next junction already
        /// </summary>
        bool IsSimpleSidingStart(TrainpathJunctionNode junctionNode)
        {
            if (junctionNode.IsFacingPoint)
            {
                TrackNode trackNode = trackNodes[junctionNode.junctionIndex];
                return (GetNextJunctionIndex(junctionNode, trackNode.MainTVN()) ==
                        GetNextJunctionIndex(junctionNode, trackNode.SidingTVN()));
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Popup a dialog to enable to user to edit the path meta data
        /// </summary>
        public void EditMetaData()
        {
            string[] metadata = { trainpath.PathID, trainpath.PathName, trainpath.PathStart, trainpath.pathEnd };
            PathMetadataDialog metadataDialog = new PathMetadataDialog(metadata);
            if (metadataDialog.ShowDialog() == true)
            {
                metadata = metadataDialog.GetMetadata();
                trainpath.PathID    = metadata[0];
                trainpath.PathName  = metadata[1];
                trainpath.PathStart = metadata[2];
                trainpath.pathEnd   = metadata[3];
            }
        }

        /// <summary>
        /// Save the path to file, converting the internal representation to .pat file format
        /// </summary>
        public void SavePath()
        {
           SavePatFile.WritePatFile(trainpath);
        }

        /// <summary>
        /// Create a new empty path. Old path will be deleted.
        /// </summary>
        /// <param name="drawTrackDB"></param>
        public void NewPath()
        {
            trainpath = new TrainpathWithHistory(trackDB, tsectionDat);
        }

        /// <summary>
        /// Draw more sections of the path
        /// </summary>
        public void ExtendPath()
        {
            ++numberToDraw;
            if (EditingIsActive)
            {
                allowAddingNodes = !trainpath.HasEnd;
            }
            else
            {
                allowAddingNodes = false;
            }
        }

        /// <summary>
        /// Draw the full (complete) path
        /// </summary>
        public void ExtendPathFull()
        {
            allowAddingNodes = false;
            numberToDraw = int.MaxValue;
        }

        /// <summary>
        /// Draw less sections of the path
        /// </summary>
        public void ReducePath()
        {
            if (--numberToDraw < 1) numberToDraw = 1;
        }

        /// <summary>
        /// Go to initial node and draw no path sections
        /// </summary>
        public void ReducePathFull()
        {
            numberToDraw = 1;
        }

        public void Undo()
        {
            trainpath.Undo();
        }

        public void Redo()
        {
            trainpath.Redo();
        } 
   }

    public static class ExtensionMethods
    {
        /// <summary>
        /// Extension methods to give TVN index (trackVectorNode index) for a junction/switch
        /// </summary>
        public static int TrailingTVN(this TrackNode trackNode) { return trackNode.TrPins[0].Link; }
        public static int MainTVN    (this TrackNode trackNode) { return trackNode.TrPins[1].Link; }
        public static int SidingTVN  (this TrackNode trackNode) { return trackNode.TrPins[2].Link; }

        /// <summary>
        /// Extension methods to give TVN index (trackVectorNode index) for a vector Node
        /// </summary>
        public static int JunctionIndexAtStart(this TrackNode trackNode) { return trackNode.TrPins[0].Link; }
        public static int JunctionIndexAtEnd  (this TrackNode trackNode) { return trackNode.TrPins[1].Link; }
    }
}
