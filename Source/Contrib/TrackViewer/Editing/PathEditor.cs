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
// TO-DO list for path editor
// === Not finished, so do before release
//  Crash on anti-alias
//
// ==== new/improved functionality
//  hasSidingPath not set when doing editing!
//  I want a reason for a broken path to be shown.
//      paths where nodes are find but links is broken are not correctly denoted as broken
//  add ambiguitynodes
//  end node on trendnode
//  new field:      PathHasBrokenNodes
//  add popup asking for save when path is broken. Also when there is a still a tail.
//
//  icons in context menu
//  saving path still has code parts I do not know how to do
//  struct for history. Or perhaps trainpathwithhistory should contain a list of trainpaths, so no longer inherit? Or perhaps inherit but still contain a list?
//
//  Add ambiguitiy nodes when starting to edit
//  Make a 'visit_all_nodes'??
//  snap to junction for broken node
//  Statusbar info on editing status ('broken', 'modified', ...)
//  
//  Cutting and reshaping a path
//      field: StartOfTail
//      allow to cut
//      allow to reconnect
//
//  Add mouse-click actions for
//      take other exit
//      edit wait time for uncouple?
//      move nodes like reverse
//      probably it makes sense to make 'rings' red when close enough, signifying mouse capture
//          and then changing, moving=dragging etc are all possible. Would be really sweet.
//


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS.TrackViewer.Drawing;

using MSTS.Formats;
using ORTS.Common;


namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// Enumeration of the various actions that can be taken via the editor context menu. 
    /// The enumeration order determines the order in the context menu as well
    /// </summary>
    public enum ContextMenuAction
    {
        /// <summary>Action to take the other exit</summary>
        TakeOtherExit,
        /// <summary>Action to add a passing path</summary>
        AddPassingPath,
        /// <summary>Action to remove a reversal point</summary>
        RemoveReverse,
        /// <summary>Action to remove a wait point</summary>
        RemoveWait,
        /// <summary>Action to remove an (un)couple point</summary>
        RemoveUncouple,
        /// <summary>Action to remove a passing path</summary>
        RemovePassingPath,
        /// <summary>Edit the current wait/(un)couple point</summary>
        EditPoint,
        /// <summary>Remove the end node</summary>
        RemoveEnd,
        /// <summary>Remove the start node</summary>
        RemoveStart,
        /// <summary>Change the direction at the start node</summary>
        OtherStartDirection,      
        /// <summary>Separator, so no real action</summary>
        Separator1,
        /// <summary>Action to add a reversal node</summary>
        AddReverse,
        /// <summary>Action to add an end node</summary>
        AddEnd,
        /// <summary>Action to add a wait point</summary>
        AddWait,
        /// <summary>Action to add an (un)couple point</summary>
        AddUncouple,
        /// <summary>Add the start node</summary>
        AddStart,
        /// <summary>Separator, so no real action</summary>
        Separator2,
        /// <summary>Draw the path only to here</summary>
        DrawUntilHere,
    }

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
    public class PathEditor
    {
        #region Public members
        /// <summary>Editing is active or not</summary>
        public bool EditingIsActive { 
            get {return _editingIsActive;}
            set { _editingIsActive = value; OnActiveOrPathChanged(); }
        }
        /// <summary>If context menu is open, updating active node and track is disabled</summary>
        public bool EnableMouseUpdate { get; set; }

        /// <summary>Name of the file with the .pat definition</summary>
        public string FileName { get; private set; }
        /// <summary>Does the path have a path</summary>
        public bool HasValidPath { get { return (trainpath.FirstNode != null); } }

        // some redirections to the drawPath
        /// <summary>Return current node (last drawn) node</summary>
        public TrainpathNode CurrentNode { get { return drawPath.CurrentMainNode; } }
        /// <summary>Return the location of the current (last drawn) node</summary>
        public WorldLocation CurrentLocation { get { return CurrentNode.Location; } }
        #endregion

        #region Private members
        bool oneTimeDisallowAdd; // in case we just removed a node, prevent adding an extra node during first redraw

        DrawTrackDB drawTrackDB; // We need to know what has been drawn, especially to get track closest to mouse
        TrackDB trackDB;
        TSectionDatFile tsectionDat;

        TrainpathWithHistory trainpath;  // the path we will be editing
        DrawPath drawPath;      // drawing of the path itself
        TrackNode[] trackNodes; // tracknodes from trackDB database.

        TrainpathNode activeNode;           // active Node (if present) for which actions can be performed
        TrainpathVectorNode activeTrackLocation;  // dynamic node that follows the mouse and is on track, but is not part of path

        Dictionary<ContextMenuAction, string> contextMenuHeaders; //names shown in the contextMenu
        Dictionary<ContextMenuAction, bool> contextMenuEnabled;  //is the specific contextMenu item enabled or not.

        EditorContextMenu editorContextMenu; // the context menu

        int numberToDraw = 4; // number of nodes to draw, start with a bit
        readonly int maxNumberNodesToCheckForReconnect = 10; // maximum number of nodes we will try before we conclude not reconnection is possible.
        bool allowAddingNodes;  // if at end of path, do we allow adding a node to the path.

        int mouseX, mouseY;

        bool _editingIsActive;
        #endregion
        
        /// <summary>
        /// base constructor that only stores the information of the tracks.
        /// </summary>
        /// <param name="drawTrackDB"></param>
        private PathEditor(DrawTrackDB drawTrackDB)
        {
            this.drawTrackDB = drawTrackDB;
            this.trackDB = drawTrackDB.TrackDB;
            this.tsectionDat = drawTrackDB.TsectionDat;

            trackNodes = drawTrackDB.TrackDB.TrackNodes;
            TrackExtensionMethods.StoreMainRoutes(trackDB.TrackNodes, tsectionDat); // we might be calling this more than once, but so be it.

            EnableMouseUpdate = true;

            drawPath = new DrawPath(trackDB, tsectionDat);

            activeTrackLocation = new TrainpathVectorNode(trackDB, tsectionDat);
            CreateContextMenuEntries();
        }

        /// <summary>
        /// Constructor. This will create a new empty path.
        /// </summary>
        /// <param name="drawTrackDBIn">The (draw)trackDB that also is the reference to the track data base and track section data</param>
        /// <param name="pathsDirectory">The directory where paths will be stored</param>
        public PathEditor(DrawTrackDB drawTrackDBIn, string pathsDirectory)
            :this(drawTrackDBIn)
        {
            trainpath = new TrainpathWithHistory(trackDB, tsectionDat);
            FileName = trainpath.PathId + ".pat";
            trainpath.FilePath = System.IO.Path.Combine(pathsDirectory, FileName);
            EditingIsActive = true;
        }

        /// <summary>
        /// Constructor. This will actually load the .pat from file and create menus as needed
        /// </summary>
        /// <param name="drawTrackDBIn">The (draw)trackDB that also is the reference to the track data base and track section data</param>
        /// <param name="path">Path to the .pat file</param>
        public PathEditor(DrawTrackDB drawTrackDBIn, ORTS.Menu.Path path)
            :this(drawTrackDBIn)
        {
            FileName = path.FilePath.Split('\\').Last();
            trainpath = new TrainpathWithHistory(trackDB, tsectionDat, path.FilePath);
            EditingIsActive = false;
        }

        /// <summary>
        /// Fill the various contect menu items with appropriate headers and a default state.
        /// </summary>
        private void CreateContextMenuEntries()
        {
            if (!(contextMenuHeaders == null)) return;
            contextMenuHeaders = new Dictionary<ContextMenuAction, string>();
            contextMenuHeaders[ContextMenuAction.RemoveEnd] = "Remove end point";
            contextMenuHeaders[ContextMenuAction.RemoveReverse] = "Remove reversal point";
            contextMenuHeaders[ContextMenuAction.RemoveWait] = "Remove wait point";
            contextMenuHeaders[ContextMenuAction.RemoveUncouple] = "Remove (un)couple point";
            contextMenuHeaders[ContextMenuAction.RemoveStart] = "Remove start point";
            contextMenuHeaders[ContextMenuAction.RemovePassingPath] = "Remove passing path";

            contextMenuHeaders[ContextMenuAction.AddEnd] = "Place end point";
            contextMenuHeaders[ContextMenuAction.AddReverse] = "Place reversal point";
            contextMenuHeaders[ContextMenuAction.AddWait] = "Place wait point";
            contextMenuHeaders[ContextMenuAction.AddUncouple] = "Place (un)couple point";
            contextMenuHeaders[ContextMenuAction.AddStart] = "Place start point";
            contextMenuHeaders[ContextMenuAction.AddPassingPath] = "Add passing path";

            contextMenuHeaders[ContextMenuAction.EditPoint] = "Edit point data";
            contextMenuHeaders[ContextMenuAction.TakeOtherExit] = "Take other exit";
            contextMenuHeaders[ContextMenuAction.OtherStartDirection] = "Change start direction";

            contextMenuHeaders[ContextMenuAction.DrawUntilHere] = "Draw path until here";

            contextMenuEnabled = new Dictionary<ContextMenuAction, bool>();
            foreach (ContextMenuAction item in Enum.GetValues(typeof(ContextMenuAction)))
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
            //lets start with everything off.
            foreach (ContextMenuAction action in Enum.GetValues(typeof(ContextMenuAction)))
            {
                contextMenuEnabled[action] = false;
            }

            if (!ORTS.TrackViewer.Properties.Settings.Default.showTrainpath)
            {
                return;
            }

            if (trainpath.FirstNode == null)
            {   // path is empty    
                contextMenuEnabled[ContextMenuAction.AddStart] = true;
                return;
            }

            contextMenuEnabled[ContextMenuAction.AddStart] = false;
            if (trainpath.HasEnd)
            {   // if there is an end, some things are simply not allowed
                contextMenuEnabled[ContextMenuAction.RemoveEnd] = (activeNode.Type == TrainpathNodeType.End);
                contextMenuEnabled[ContextMenuAction.AddEnd] = false;
                contextMenuEnabled[ContextMenuAction.AddReverse] = false;
                contextMenuEnabled[ContextMenuAction.RemoveReverse] = false; // removing a reverse point would not allow to have same end-point
                contextMenuEnabled[ContextMenuAction.AddStart] = false;
                contextMenuEnabled[ContextMenuAction.OtherStartDirection] = false;
                contextMenuEnabled[ContextMenuAction.RemoveStart] = false;
            }
            else
            {
                contextMenuEnabled[ContextMenuAction.RemoveEnd] = false;
                contextMenuEnabled[ContextMenuAction.AddEnd] = (activeTrackLocation.Location != null
                                                            && !activeTrackLocation.PrevNode.HasSidingPath);
                contextMenuEnabled[ContextMenuAction.AddReverse] = (activeTrackLocation.Location != null
                                                            && !activeTrackLocation.PrevNode.HasSidingPath);
                contextMenuEnabled[ContextMenuAction.RemoveReverse] = (activeNode.Type == TrainpathNodeType.Reverse);
                contextMenuEnabled[ContextMenuAction.TakeOtherExit] = (activeNode.Type != TrainpathNodeType.SidingStart)
                                                            && (activeNode is TrainpathJunctionNode)
                                                            && (activeNode as TrainpathJunctionNode).IsFacingPoint
                                                            && (activeNode.NextMainNode != null);
                contextMenuEnabled[ContextMenuAction.RemoveStart] = (activeNode == trainpath.FirstNode);
                contextMenuEnabled[ContextMenuAction.OtherStartDirection] = (activeNode == trainpath.FirstNode);
            }

            //not related to end-of-path
            contextMenuEnabled[ContextMenuAction.RemovePassingPath] = (activeNode.Type == TrainpathNodeType.SidingStart);
            contextMenuEnabled[ContextMenuAction.RemoveWait] = (activeNode.Type == TrainpathNodeType.Stop);
            contextMenuEnabled[ContextMenuAction.RemoveUncouple] = (activeNode.Type == TrainpathNodeType.Uncouple);
            contextMenuEnabled[ContextMenuAction.AddWait] = (activeTrackLocation.Location != null
                                                            && !activeTrackLocation.PrevNode.HasSidingPath);
            contextMenuEnabled[ContextMenuAction.AddUncouple] = (activeTrackLocation.Location != null
                                                            && !activeTrackLocation.PrevNode.HasSidingPath);
            contextMenuEnabled[ContextMenuAction.EditPoint] = (activeNode.Type == TrainpathNodeType.Stop
                                                            || activeNode.Type == TrainpathNodeType.Uncouple);

            contextMenuEnabled[ContextMenuAction.DrawUntilHere] = true; // can always be done?

            //items for which the calculations are more complex.
            contextMenuEnabled[ContextMenuAction.TakeOtherExit] = CanTakeOtherExit(false);
            contextMenuEnabled[ContextMenuAction.AddPassingPath] = CanTakeOtherExit(true);

        }

        /// <summary>
        /// Figure out whether it is possible to take other exit from active node
        /// </summary>
        /// <param name="needToReconnect">boolean describing whether a reconnect is needed (instead of just really going somewhere else.</param>
        bool CanTakeOtherExit(bool needToReconnect)
        {
            TrainpathJunctionNode activeNodeAsJunction = activeNode as TrainpathJunctionNode;

            //basic requirements
            if (activeNodeAsJunction == null
                || activeNode.NextMainNode == null
                || !activeNodeAsJunction.IsFacingPoint
                || (activeNode.Type == TrainpathNodeType.SidingStart)
                )
            {
                return false;
            }

            if (trainpath.HasEnd || needToReconnect)
            {   // see if we can reconnect without affecting the end-point
                int newTVN = GetOtherExitTvnIndex(activeNodeAsJunction);
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
            ContextMenuAction action = (ContextMenuAction)e;
            switch (action)
            {
                case ContextMenuAction.RemoveEnd:
                    ActionRemoveEndPoint();
                    break;
                case ContextMenuAction.RemoveWait:
                case ContextMenuAction.RemoveUncouple:
                    ActionRemoveWaitUncouplePoint();
                    break;
                case ContextMenuAction.RemoveReverse:
                    ActionRemoveReversePoint();
                    break;
                case ContextMenuAction.RemovePassingPath:
                    ActionRemoveSidingPath();
                    break;
                case ContextMenuAction.AddEnd:
                    ActionAddEndPoint();
                    break;
                case ContextMenuAction.AddReverse:
                    ActionAddReversePoint();
                    break;
                case ContextMenuAction.AddWait:
                    ActionAddWaitPoint();
                    break;
                case ContextMenuAction.AddUncouple:
                    ActionAddUncouplePoint();
                    break;
                case ContextMenuAction.TakeOtherExit:
                    ActionTakeOtherExit();
                    break;
                case ContextMenuAction.AddPassingPath:
                    ActionAddPassingPath();
                    break;
                case ContextMenuAction.AddStart:
                    ActionAddStartPoint();
                    break;
                case ContextMenuAction.RemoveStart:
                    ActionRemoveStartPoint();
                    break;
                case ContextMenuAction.OtherStartDirection:
                    ActionOtherStartDirection();
                    break;
                case ContextMenuAction.EditPoint:
                    ActionEditPointData(activeNode as TrainpathVectorNode);
                    break;
                case ContextMenuAction.DrawUntilHere:
                    ActionDrawUntilHere(activeNode);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Find the active node and active node candidate and draw them
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        public void Draw(DrawArea drawArea)
        {
            //List of main-track nodes that were actually drawn and can therefore be selected for editing
            List<TrainpathNode> drawnNodes = new List<TrainpathNode>();

            //Keys are tracknode indexes, value is a list of (main) track node (in pairs) that are both
            // on the tracknode and the path between them has been drawn
            Dictionary<int, List<TrainpathNode>> drawnTrackIndexes = new Dictionary<int, List<TrainpathNode>>();

            int numberDrawn = drawPath.Draw(drawArea, trainpath.FirstNode, numberToDraw, drawnNodes, drawnTrackIndexes);

            if (numberDrawn < numberToDraw)
            {
                // Apparently we were not able to draw all nodes. Reset maximum number to draw, and possibly add a node
                numberToDraw = numberDrawn;
                if (EditingIsActive && allowAddingNodes && !(CurrentNode.Type == TrainpathNodeType.End))
                {
                    AddAdditionalNode(CurrentNode, true);
                }
            }

            if (!EditingIsActive)
            {
                return;
            }

            if (EnableMouseUpdate)
            {
                FindActiveNode(drawArea, drawnNodes);
                FindActiveTrackLocation(drawnTrackIndexes);
            }

            if (activeNode != null)
            {
                drawArea.DrawSimpleTexture(activeNode.Location, "ring", 8f, 7, DrawColors.colorsNormal["activeNode"]);

            }
            if (activeTrackLocation != null && activeTrackLocation.Location != null)
            {
                drawArea.DrawSimpleTexture(activeTrackLocation.Location, "ring", 8f, 7, DrawColors.colorsNormal["nodeCandidate"]);
            }

        }

        /// <summary>
        /// Find the node in the path that is closest to the mouse,
        /// </summary>
        /// <param name="drawArea">Area that is being drawn upon and where we have a mouse location</param>
        /// <param name="drawnMainNodes">List of nodes that have been drawn</param>
        void FindActiveNode(DrawArea drawArea, List<TrainpathNode> drawnMainNodes)
        {
            // Initial simplest implementation: find simply the closest and first.
            float closestMouseDistanceSquared = float.MaxValue;
            TrainpathNode closestNode = null;
            foreach (TrainpathNode node in drawnMainNodes)
            {
                float distanceSquared = CloseToMouse.GetGroundDistanceSquared(node.Location, drawArea.MouseLocation);
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
        /// <param name="drawnTrackIndexes"></param>
        void FindActiveTrackLocation(Dictionary<int, List<TrainpathNode>> drawnTrackIndexes)
        {

            uint tni = drawTrackDB.ClosestTrack.TrackNode.Index;
            int tni_int = (int)tni;

            if (!drawnTrackIndexes.ContainsKey(tni_int) && trainpath.FirstNode != null)
            {
                activeTrackLocation.Location = null;
                return;
            }

            int tvsi = drawTrackDB.ClosestTrack.TrackVectorSectionIndex;
            float distance = drawTrackDB.ClosestTrack.DistanceAlongTrack;

            // find location
            WorldLocation location = drawTrackDB.FindLocation(tni, tvsi, distance, true);

            // fill the properties of the activeTrackLocation 
            activeTrackLocation.TvnIndex = tni_int;
            activeTrackLocation.TrackVectorSectionIndex = tvsi;
            activeTrackLocation.TrackSectionOffset = distance;
            activeTrackLocation.Location = location;

            if (trainpath.FirstNode != null)
            {   //Only in case this is not the first path.
                TrainpathNode prevNode = FindPrevNodeOfActiveTrack(drawnTrackIndexes[tni_int]);
                if (prevNode == null)
                {
                    activeTrackLocation.Location = null;
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
                if (activeTrackLocation.IsBetween(nodesOnTrack[i - 1], nodesOnTrack[i]))
                {
                    return nodesOnTrack[i - 1];
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
            prevNode.NextSidingTvnIndex = 0;
            // Since we already know the TVN, simply add a node (meaning removing the end will extend the path. 
            AddAdditionalNode(prevNode, prevNode.NextMainTvnIndex, true);
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
            lastNode.NextMainNode = null;

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
            numberToDraw++; //make sure the second point is drawn.
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
            lastNode.NextMainTvnIndex = newNode.TvnIndex;
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
            int newTvnIndex = GetOtherExitTvnIndex(junctionNode);
            TrainpathJunctionNode reconnectNode = FindReconnectNode(junctionNode, newTvnIndex);

            
            if (reconnectNode == null)
            {   //really take other exit and discard rest of path
                junctionNode.NextMainTvnIndex = newTvnIndex;
                RemoveNodeAndAllFollowing(activeNode.NextMainNode);
                return;
            }

            //correct NumberToDraw with nodes we will remove
            int numberOfNodesInOldTrack = GetNodeNumber(reconnectNode) - GetNodeNumber(activeNode);
            numberToDraw -= numberOfNodesInOldTrack;

            //we can reconnect, so create a path to reconnection point
            TrainpathNode lastNodeNewPath = CreatePartialPath(activeNode, newTvnIndex, reconnectNode, true);

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
            int newTvnIndex = GetOtherExitTvnIndex(junctionNode);
            TrainpathJunctionNode reconnectNode = FindReconnectNode(junctionNode, newTvnIndex);
            //we have checked that we can reconnect before, so reconnectNode cannot be zero.
            
            activeNode.Type = TrainpathNodeType.SidingStart;
            int oldNumberToDraw = numberToDraw;
            TrainpathNode lastNodeSidingPath = CreatePartialPath(activeNode, newTvnIndex, reconnectNode, false);
            numberToDraw = oldNumberToDraw;

            //reconnect. At this point, newNode is already the same node as reconnectNode. We will discard it
            lastNodeSidingPath.NextSidingNode = reconnectNode;
            reconnectNode.Type = TrainpathNodeType.SidingEnd;
        }

        /// <summary>
        /// Draw the path only until the given node, and not further.
        /// </summary>
        /// <param name="node">Node to draw until</param>
        void ActionDrawUntilHere(TrainpathNode node)
        {
            int newNumberToDraw = GetNodeNumber(node);
            if (newNumberToDraw >= 0)
            {
                numberToDraw = newNumberToDraw;
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
        TrainpathNode CreatePartialPath(TrainpathNode currentNode, int newTvnIndex, TrainpathJunctionNode reconnectNode, bool isMainPath)
        {
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
                newNode = AddAdditionalNode(currentNode, isMainPath);

            } while (newNode != null); // if we get here, something is wrong, because we checked we could reconnect

            // The returned node will not be the last node created, because that one is at the same location as the reconnect node
            return currentNode;
        }

        /// <summary>
        /// Add a new vector node at the given location in the middle of a path
        /// </summary>
        /// <param name="nodeCandidate"></param>
        /// <returns></returns>
        TrainpathVectorNode AddIntermediateNode(TrainpathVectorNode nodeCandidate)
        {
            TrainpathNode prevNode = nodeCandidate.PrevNode;
            TrainpathNode nextNode = prevNode.NextMainNode;

            TrainpathVectorNode newNode = AddAdditionalVectorNode(prevNode, nodeCandidate, true);

            prevNode.NextMainNode = newNode;
            prevNode.NextSidingNode = null; // should not be needed
            prevNode.NextMainTvnIndex = newNode.TvnIndex;
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
        /// Remove a intermediate vector node. Possibly add a ambiguity node if needed.
        /// Also the 'NumberToDraw' will be updated.
        /// </summary>
        /// <param name="curNode">Node to be removed</param>
        void RemoveIntermediatePoint(TrainpathVectorNode curNode)
        {
            TrainpathNode prevNode = curNode.PrevNode;
            prevNode.NextMainNode = curNode.NextMainNode;
            prevNode.NextSidingNode = null; // should not be needed
            //lastNodeSidingPath.NextMainTvnIndex should be the same still
            if (prevNode.NextMainNode != null)
            {   // there might not be a next node.
                prevNode.NextMainNode.PrevNode = prevNode;
            }
            numberToDraw--;

            //Check if we need to add an ambiguity node
            if ((prevNode is TrainpathJunctionNode) && (prevNode.NextMainNode is TrainpathJunctionNode))
            {
                TrainpathJunctionNode prevJunctionNode = prevNode as TrainpathJunctionNode;
                if (IsSimpleSidingStart(prevJunctionNode))
                {
                    TrainpathVectorNode halfwayNode = CreateHalfWayNode(prevJunctionNode, prevJunctionNode.NextMainTvnIndex);
                    halfwayNode.PrevNode = prevNode;
                    AddIntermediateNode(halfwayNode);
                }
            }
        }

        /// <summary>
        /// Add an additional node, where the next track node is not yet given. Also the 'NumberToDraw' will be updated.
        /// </summary>
        /// <param name="lastNode">currently last node</param>
        /// <param name="isMainPath">Do we add the node to the main path or not</param>
        /// <returns>The newly created (unlinked) path node</returns>
        public TrainpathNode AddAdditionalNode(TrainpathNode lastNode, bool isMainPath)
        {
            if (oneTimeDisallowAdd == true)
            {
                oneTimeDisallowAdd = false;
                return null;
            }

            if (lastNode is TrainpathVectorNode)
            {
                return AddAdditionalNode(lastNode, (lastNode as TrainpathVectorNode).TvnIndex, isMainPath);
            }

            TrainpathJunctionNode junctionNode = lastNode as TrainpathJunctionNode;
            TrackNode trackNode = trackNodes[junctionNode.JunctionIndex];
            if (trackNode.TrEndNode) return null;  // if it happens to be the end of a path, forget about it.

            if (junctionNode.IsFacingPoint)
            {
                return AddAdditionalNode(lastNode, trackNode.MainTvn(), isMainPath);
            }
            else
            {
                return AddAdditionalNode(lastNode, trackNode.TrailingTvn(), isMainPath);
            }
        }

        /// <summary>
        /// Add an additional node starting at the given node, following the TvnIndex,
        /// but take care of a possible need for disambiguity. Also the 'NumberToDraw' will be updated.
        /// </summary>
        /// <param name="lastNode">Node after which a new node needs to be added</param>
        /// <param name="TvnIndex">TrackVectorNode index of the track the path needs to be on</param>
        /// <param name="isMainPath">Do we add the node to the main path or not</param>
        /// <returns>The newly created path node</returns>
        TrainpathNode AddAdditionalNode(TrainpathNode lastNode, int TvnIndex, bool isMainPath)
        {
            if (lastNode is TrainpathVectorNode)
            {
                return AddAdditionalJunctionNode(lastNode, (lastNode as TrainpathVectorNode).TvnIndex, isMainPath);
            }

            TrainpathJunctionNode junctionNode = lastNode as TrainpathJunctionNode;
            if (IsSimpleSidingStart(junctionNode))
            {   // start of a simple siding. So the next node should be a node to remove ambiguity.
                TrainpathVectorNode halfwayNode = CreateHalfWayNode(junctionNode, TvnIndex);
                return AddAdditionalVectorNode(junctionNode, halfwayNode, isMainPath);
            }
            else
            {
                return AddAdditionalJunctionNode(junctionNode, TvnIndex, isMainPath);
            }
        }

        /// <summary>
        /// Add an additional node, from the current last node along the next TrackNodeVector (given by index)
        /// The added node will always be a junction node. Also the 'NumberToDraw' will be updated.
        /// </summary>
        /// <param name="lastNode">Currently last node of path</param>
        /// <param name="nextTvnIndex">TrackNodeVector index along which to place the track</param>
        /// <param name="isMainPath">Are we adding a node on the main path (alternative is passing path)</param>
        /// <returns>The newly created junction path node</returns>
        TrainpathJunctionNode AddAdditionalJunctionNode(TrainpathNode lastNode, int nextTvnIndex, bool isMainPath)
        {
            // we add a new activeNodeAsJunction
            TrainpathJunctionNode newNode = new TrainpathJunctionNode(lastNode);

            TrackNode linkingTrackNode = trackNodes[nextTvnIndex];
            if (linkingTrackNode == null)
            {
                return null; // apparently there is some issue in the track.
            }

            newNode.JunctionIndex = GetNextJunctionIndex(lastNode, nextTvnIndex);
            TrackNode newJunctionTrackNode = trackNodes[newNode.JunctionIndex];
            newNode.Location = DrawTrackDB.UidLocation(newJunctionTrackNode.UiD);

            // simple linking
            if (isMainPath)
            {
                lastNode.NextMainTvnIndex = nextTvnIndex;
                lastNode.NextMainNode = newNode;
                newNode.PrevNode = lastNode;
            }
            else
            {
                lastNode.NextSidingTvnIndex = nextTvnIndex;
                lastNode.NextSidingNode = newNode;
            }

            newNode.SetFacingPoint(GetLeavingTvnIndex(newNode.JunctionIndex, nextTvnIndex)); //setfacing point works on nextmainTvnIndex
            newNode.DetermineOrientation(lastNode, nextTvnIndex);

            numberToDraw++; // We want to keep the path drawn to the same node as before adding a node
            return newNode;
        }

        /// <summary>
        /// Add a new vector path node at the location of nodeCandidate. Also the 'NumberToDraw' will be updated.
        /// </summary>
        /// <param name="lastNode">node that will be predecessor of the new nodeCandidate</param>
        /// <param name="nodeCandidate">partial trainpath vector node describing the current mouse location</param>
        /// <param name="isMainPath">Do we add the node to the main path or not</param>
        /// <returns>The newly created vector node</returns>
        TrainpathVectorNode AddAdditionalVectorNode(TrainpathNode lastNode, TrainpathVectorNode nodeCandidate, bool isMainPath)
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

            numberToDraw++; // We want to keep the path drawn to the same node as before adding a node
            return newNode;
        }

        /// <summary>
        /// Create a (still unlinked) node halfway through the next section (so halfway between this
        /// and the next junction. Needed specially for disambiguity.
        /// </summary>
        /// <param name="junctionNode">The junction node where we start</param>
        /// <param name="TvnIndex">The TrackVectorNode index for the path</param>
        /// <returns>An unlinked vectorNode at the midpoint.</returns>
        TrainpathVectorNode CreateHalfWayNode(TrainpathJunctionNode junctionNode, int TvnIndex)
        {   // The idea here is to use all the code in traveller to make life easier.

            Traveller traveller = junctionNode.PlaceTravellerAfterJunction(TvnIndex);

            TrackNode nextJunctionTrackNode = trackNodes[GetNextJunctionIndex(junctionNode, TvnIndex)];
            WorldLocation nextJunctionLocation = DrawTrackDB.UidLocation(nextJunctionTrackNode.UiD);

            //move the traveller halfway through the next vector section
            float distanceToTravel = traveller.DistanceTo(nextJunctionLocation) / 2;
            traveller.Move(distanceToTravel);

            TrainpathVectorNode halfwayNode = new TrainpathVectorNode(junctionNode, traveller);
            halfwayNode.DetermineOrientation(junctionNode, TvnIndex);

            return halfwayNode;
        }

        /// <summary>
        /// Determine whether we can reconnect from the current junction node to the main track, if we take 
        /// the new TvnIndex
        /// </summary>
        /// <param name="startJunctionNode">junction node (which is a facing point) that is the start of an alternative path</param>
        /// <param name="newTvnIndex">index of the track vector node that is intended to be used for reconnection</param>
        /// <returns></returns>
        TrainpathJunctionNode FindReconnectNode(TrainpathJunctionNode startJunctionNode, int newTvnIndex)
        {
            //First find the list of nodes we might be able to reconnect to. They should all be junction nodes
            List<TrainpathJunctionNode> reconnectNodes = FindReconnectNodeCandidates(startJunctionNode);

            //next we need to follow the new path from the newTvnIndex, and see whether we can reconnect
            int junctionIndex = startJunctionNode.JunctionIndex;
            int TvnIndex = newTvnIndex;
            int junctionsToCheck = maxNumberNodesToCheckForReconnect;
            while (junctionsToCheck > 0)
            {
                int nextJunctionIndex = GetNextJunctionIndex(junctionIndex, TvnIndex);
                foreach (TrainpathJunctionNode reconnectNode in reconnectNodes)
                {
                    if (reconnectNode.JunctionIndex == nextJunctionIndex)
                    {   // we found a link, but we still need to check if we don't connect from the trailing side
                        TrackNode junctionTrackNode = trackNodes[nextJunctionIndex];
                        if (TvnIndex == junctionTrackNode.TrailingTvn())
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
                TvnIndex = GetLeavingTvnIndex(junctionIndex, TvnIndex);
                if (TvnIndex < 0)
                {
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
        /// Once the editing becomes active for this path, we make sure the path is 'clean' according to our standards
        /// </summary>
        void OnActiveOrPathChanged()
        {
            if (!EditingIsActive) { return; }
            SnapAllJunctionNodes();
            AddMissingAmbiguityNodes();
        }

        /// <summary>
        /// Make sure the junction nodes of have the exact location of the junctions in the track database.
        /// This is to make sure changes in the track database are taken over in the path
        /// </summary>
        void SnapAllJunctionNodes()
        {
            TrainpathNode mainNode = trainpath.FirstNode;
            while (mainNode != null)
            {
                //siding path. For this routine we do not care if junctions are done twice
                TrainpathNode sidingNode = mainNode.NextSidingNode;
                while (sidingNode != null)
                {
                    if (sidingNode is TrainpathJunctionNode && !sidingNode.IsBroken)
                    {
                        sidingNode.Location = DrawTrackDB.UidLocation(trackDB.TrackNodes[(sidingNode as TrainpathJunctionNode).JunctionIndex].UiD);
                    }
                    sidingNode = sidingNode.NextSidingNode;
                }

                
                if (mainNode is TrainpathJunctionNode && !mainNode.IsBroken)
                {
                    mainNode.Location = DrawTrackDB.UidLocation(trackDB.TrackNodes[(mainNode as TrainpathJunctionNode).JunctionIndex].UiD);
                }
                mainNode = mainNode.NextMainNode;
            }
        }


        /// <summary>
        /// Not all paths have enough ambiguity nodes to distinghuish between two possible paths. Here we add them
        /// </summary>
        void AddMissingAmbiguityNodes()
        {
            //todo
        }

        /// <summary>
        /// From the current pathnode and the linking tracknode, fin the junctionIndex of the next junction (or possibly end-point)
        /// </summary>
        /// <param name="node">The current node</param>
        /// <param name="linkingTrackNodeIndex">The index of the tracknode leaving the node</param>
        /// <returns>The index of the junction index at the end of the track (as seen from the node)</returns>
        int GetNextJunctionIndex(TrainpathNode node, int linkingTrackNodeIndex)
        {
            if (node is TrainpathJunctionNode)
            {
                return GetNextJunctionIndex((node as TrainpathJunctionNode).JunctionIndex, linkingTrackNodeIndex);
            }

            TrackNode linkingTrackNode = trackNodes[linkingTrackNodeIndex];
            return (node as TrainpathVectorNode).ForwardOriented
                ? linkingTrackNode.JunctionIndexAtEnd()
                : linkingTrackNode.JunctionIndexAtStart();
        }

        /// <summary>
        /// Calculate the number of this node in the total path. FirstNode is 1. The node needs to be on mainpath,
        /// otherwise -1 will be returned.
        /// </summary>
        /// <param name="node">Node for which to calculate the number</param>
        /// <returns>The sequential number of the node in the main path. -1 in case node is not on main path.</returns>
        int GetNodeNumber(TrainpathNode node)
        {
            int numberFound = 1;
            TrainpathNode mainNode = trainpath.FirstNode;
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
        /// <param name="incomingTvnIndex">index of incoming vector node</param>
        /// <returns>index of the leaving vector node or -1 if none found.</returns>
        int GetLeavingTvnIndex(int junctionIndex, int incomingTvnIndex)
        {
            TrackNode junctionTrackNode = trackNodes[junctionIndex];
            if (junctionTrackNode.TrEndNode)
            {
                return -1;
            }
            if (incomingTvnIndex == junctionTrackNode.TrailingTvn())
            {
                return junctionTrackNode.MainTvn();
            }
            return junctionTrackNode.TrailingTvn();
        }

        /// <summary>
        /// For a junction with two exits, get the index of the vector node not being used at the moment
        /// </summary>
        /// <param name="junctionNode">Node for which to find the other exit</param>
        /// <returns>The index of the other leaving vector node.</returns>
        int GetOtherExitTvnIndex(TrainpathJunctionNode junctionNode)
        {
            TrackNode trackNode = trackNodes[junctionNode.JunctionIndex];
            if (junctionNode.NextMainTvnIndex == trackNode.MainTvn())
            {
                return trackNode.SidingTvn();
            }
            else
            {
                return trackNode.MainTvn();
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
                TrackNode trackNode = trackNodes[junctionNode.JunctionIndex];
                return (GetNextJunctionIndex(junctionNode, trackNode.MainTvn()) ==
                        GetNextJunctionIndex(junctionNode, trackNode.SidingTvn()));
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
            string[] metadata = { trainpath.PathId, trainpath.PathName, trainpath.PathStart, trainpath.PathEnd };
            PathMetadataDialog metadataDialog = new PathMetadataDialog(metadata);
            if (metadataDialog.ShowDialog() == true)
            {
                metadata = metadataDialog.GetMetadata();
                trainpath.PathId = metadata[0];
                trainpath.PathName = metadata[1];
                trainpath.PathStart = metadata[2];
                trainpath.PathEnd = metadata[3];
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
            editorContextMenu.CloseContextMenu();
        }

        /// <summary>
        /// Draw the full (complete) path
        /// </summary>
        public void ExtendPathFull()
        {
            allowAddingNodes = false;
            numberToDraw = int.MaxValue;
            editorContextMenu.CloseContextMenu();
        }

        /// <summary>
        /// Draw less sections of the path
        /// </summary>
        public void ReducePath()
        {
            if (--numberToDraw < 1) numberToDraw = 1;
            editorContextMenu.CloseContextMenu();
        }

        /// <summary>
        /// Go to initial node and draw no path sections
        /// </summary>
        public void ReducePathFull()
        {
            numberToDraw = 1;
            editorContextMenu.CloseContextMenu();
        }

        /// <summary>
        /// Undo the last editor action
        /// </summary>
        public void Undo()
        {
            trainpath.Undo();
            editorContextMenu.CloseContextMenu();
        }

        /// <summary>
        /// Redo the last Undo (if available)
        /// </summary>
        public void Redo()
        {
            trainpath.Redo();
            editorContextMenu.CloseContextMenu();
        }
    }

    /// <summary>
    /// Extension methods to give TVN index (trackVectorNode index) for a junction/switch or for a vector node
    /// </summary>
    public static class TrackExtensionMethods
    {
        /// <summary>The TrPin index of the main route of a junction node</summary>
        private static uint[] mainRouteIndex;
        /// <summary>The TrPin index of the siding route of a junction node</summary>
        private static uint[] sidingRouteIndex;

        /// <summary>
        /// Find the indices we need to use for TrPins in the various junction nodes in case we want to use either main
        /// or siding path. That information is available in the trackshapes in the tsectionDat.
        /// </summary>
        /// <param name="trackNodes">The tracknodes</param>
        /// <param name="tsectionDat">Track section Data</param>
        public static void StoreMainRoutes(TrackNode[] trackNodes, TSectionDatFile tsectionDat)
        {
            mainRouteIndex = new uint[trackNodes.Length];
            sidingRouteIndex = new uint[trackNodes.Length];
            for (int tni = 0; tni < trackNodes.Length; tni++)
            {
                TrackNode tn = trackNodes[tni];
                if (tn == null) continue;
                if (tn.TrJunctionNode == null) continue;
                uint mainRoute = 0;

                uint trackShapeIndex = tn.TrJunctionNode.ShapeIndex;
                try
                {
                    TrackShape trackShape = tsectionDat.TrackShapes[trackShapeIndex];
                    mainRoute = trackShape.MainRoute;
                }
                catch { }

                mainRouteIndex[tni] = tn.Inpins + mainRoute;
                if (mainRoute == 0)
                {   // sidingRouteIndex is simply the next
                    sidingRouteIndex[tni] = tn.Inpins + 1;
                }
                else
                {   // sidingRouteIndex is the first
                    sidingRouteIndex[tni] = tn.Inpins;
                }
            }
        }

        /// <summary>Return the vector node index of the trailing path leaving this junction</summary>
        public static int TrailingTvn(this TrackNode trackNode) { return trackNode.TrPins[0].Link; }
        /// <summary>Return the vector node index of the main path leaving this junction (main being defined as the first one defined)</summary>
        public static int MainTvn(this TrackNode trackNode) { return trackNode.TrPins[mainRouteIndex[trackNode.Index]].Link; }
        /// <summary>Return the vector node index of the siding path leaving this junction (siding being defined as the second one defined)</summary>
        public static int SidingTvn(this TrackNode trackNode) { return trackNode.TrPins[sidingRouteIndex[trackNode.Index]].Link; }

        /// <summary>Return the vector node index at the begin of this vector node</summary>
        public static int JunctionIndexAtStart(this TrackNode trackNode) { return trackNode.TrPins[0].Link; }
        /// <summary>Return the vector node index at the end of this vector node</summary>
        public static int JunctionIndexAtEnd(this TrackNode trackNode) { return trackNode.TrPins[1].Link; }
    }

}
