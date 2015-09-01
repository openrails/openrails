// COPYRIGHT 2014, 2015 by the Open Rails project.
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

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORTS.TrackViewer.Drawing;

using Orts.Formats.Msts;
using ORTS.Common;
using System.Windows.Controls;
using System.Windows;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>
    /// Delegate definition to allow adding events for when a path is changed.
    /// </summary>
    public delegate void ChangedPathHandler();

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
    /// Some terminology: a disambiguity node is a node that only exist to uniquely identify which of two alternative paths
    /// needs to be taken between two junctions.
    public class PathEditor
    {
        #region Public members
        /// <summary>The current path that we are editing</summary>
        public Trainpath currentTrainPath { get; private set; }
        /// <summary>Editing is active or not</summary>
        public bool EditingIsActive { 
            get {return _editingIsActive;}
            set { _editingIsActive = value; OnActiveOrPathChanged(); }
        }
        /// <summary>If context menu is open, updating active node and track is disabled</summary>
        public bool EnableMouseUpdate { get; set; }

        /// <summary>Name of the file with the .pat definition</summary>
        public string FileName { get; private set; }
        /// <summary>Does the editor have a path</summary>
        public bool HasValidPath { get { return (currentTrainPath.FirstNode != null); } }
        /// <summary>Does the editor have a path that is broken</summary>
        public bool HasBrokenPath { get { return currentTrainPath.IsBroken; } }
        /// <summary>Does the editor have a path that has an end</summary>
        public bool HasEndingPath { get { return currentTrainPath.HasEnd; } }
        /// <summary>Does the editor have a path that has been modified</summary>
        public bool HasModifiedPath { get { return currentTrainPath.IsModified; } }
        /// <summary>Does the editor have a path that has a stored tail</summary>
        public bool HasStoredTail { get { return (currentTrainPath.FirstNodeOfTail != null); } }

        // some redirections to the drawPath
        /// <summary>Return current node (last drawn) node</summary>
        public TrainpathNode CurrentNode { get { return drawPath.CurrentMainNode; } }
        /// <summary>Return the location of the current (last drawn) node</summary>
        public WorldLocation CurrentLocation { get { return CurrentNode != null ? CurrentNode.Location : WorldLocation.None; } }
        #endregion

        #region Private members
        DrawTrackDB drawTrackDB; // We need to know what has been drawn, especially to get track closest to mouse
        TrackDB trackDB;
        TrackSectionsFile tsectionDat;

        
        DrawPath drawPath;      // drawing of the path itself

        TrainpathNode activeNode;           // active Node (if present) for which actions can be performed
        TrainpathVectorNode activeTrackLocation;  // dynamic node that follows the mouse and is on track, but is not part of path

        List<EditorAction> editorActionsMouseClicked;
        List<EditorAction> editorActionsActiveNode;
        List<EditorAction> editorActionsActiveTrack;
        List<EditorAction> editorActionsBroken;
        List<EditorAction> editorActionsOthers;
        Separator separatorActiveNode;
        Separator separatorActiveTrack;
        Separator separatorBroken;

        const int practicalInfinityInt = int.MaxValue/2; // large, but not close to overflow
        int numberToDraw = practicalInfinityInt; // number of nodes to draw, start with all
        bool allowAddingNodes;  // if at end of path, do we allow adding a node to the path.

        ContextMenu contextMenu;
        MenuItem noActionPossibleMenuItem;

        // Editor actions that are not via the menu
        EditorActionNonInteractive nonInteractiveAction;
        EditorActionMouseDragVectorNode editorActionMouseDragVector;
        EditorActionMouseDragAutoConnect editorActionMouseDragAuto;
        List<EditorActionMouseDrag> mouseDragActions;   // list of mouse drag actions in the order we will test them
        EditorActionMouseDrag activeMouseDragAction;

        bool _editingIsActive;
        #endregion

        #region Constructors
        /// <summary>
        /// base constructor that only stores the information of the tracks.
        /// </summary>
        /// <param name="routeData">The route information that contains track data base and track section data</param>
        /// <param name="drawTrackDB">The drawn tracks to know about where the mouse is</param>
        private PathEditor(RouteData routeData, DrawTrackDB drawTrackDB)
        {
            this.drawTrackDB = drawTrackDB;
            this.trackDB = routeData.TrackDB;
            this.tsectionDat = routeData.TsectionDat;

            TrackExtensions.Initialize(trackDB.TrackNodes, tsectionDat); // we might be calling this more than once, but so be it.

            EnableMouseUpdate = true;

            drawPath = new DrawPath(trackDB, tsectionDat);

            CreateNonMenuActions();
            CreateMouseClickEntries();
            CreateContextMenuEntries();
            CreateContextMenu();
        }

        void CreateNonMenuActions()
        {
            activeTrackLocation = new TrainpathVectorNode(trackDB, tsectionDat);
            nonInteractiveAction = new EditorActionNonInteractive();
            editorActionMouseDragVector = new EditorActionMouseDragVectorNode();
            editorActionMouseDragAuto = new EditorActionMouseDragAutoConnect();
            mouseDragActions = new List<EditorActionMouseDrag>() { editorActionMouseDragAuto };
        }

        /// <summary>
        /// Constructor. This will create a new empty path.
        /// </summary>
        /// <param name="routeData">The route information that contains track data base and track section data</param>
        /// <param name="drawTrackDB">The drawn tracks to know about where the mouse is</param>/// <param name="pathsDirectory">The directory where paths will be stored</param>
        public PathEditor(RouteData routeData, DrawTrackDB drawTrackDB, string pathsDirectory)
            :this(routeData, drawTrackDB)
        {
            currentTrainPath = new Trainpath(trackDB, tsectionDat);
            FileName = currentTrainPath.PathId + ".pat";
            currentTrainPath.FilePath = System.IO.Path.Combine(pathsDirectory, FileName);
            EditingIsActive = true;
            OnPathChanged();
        }

        /// <summary>
        /// Constructor. This will actually load the .pat from file and create menus as needed
        /// </summary>
        /// <param name="routeData">The route information that contains track data base and track section data</param>
        /// <param name="drawTrackDB">The drawn tracks to know about where the mouse is</param>
        /// <param name="path">Path to the .pat file</param>
        public PathEditor(RouteData routeData, DrawTrackDB drawTrackDB, ORTS.Menu.Path path)
            :this(routeData, drawTrackDB)
        {
            FileName = path.FilePath.Split('\\').Last();
            currentTrainPath = new Trainpath(trackDB, tsectionDat, path.FilePath);
            EditingIsActive = false;
            OnPathChanged();
        }

        #endregion

        #region Context menu and its callbacks
        /// <summary>
        /// Fill the various contect menu items with appropriate headers and a default state.
        /// </summary>
        private void CreateContextMenuEntries()
        {
            // active node actions
            editorActionsActiveNode = new List<EditorAction>();
            editorActionsActiveNode.Add(new EditorActionTakeOtherExit());
            editorActionsActiveNode.Add(new EditorActionTakeOtherExitPassingPath());
            editorActionsActiveNode.Add(new EditorActionAddPassingPath());
            editorActionsActiveNode.Add(new EditorActionStartPassingPath());
            editorActionsActiveNode.Add(new EditorActionReconnectPassingPath());
            editorActionsActiveNode.Add(new EditorActionRemoveEnd());
            editorActionsActiveNode.Add(new EditorActionRemoveReverse());
            editorActionsActiveNode.Add(new EditorActionRemoveWait());
            editorActionsActiveNode.Add(new EditorActionEditWait());
            editorActionsActiveNode.Add(new EditorActionRemovePassingPath());
            editorActionsActiveNode.Add(new EditorActionOtherStartDirection());
            editorActionsActiveNode.Add(new EditorActionRemoveStart());
            editorActionsActiveNode.Add(new EditorActionRemoveRestOfPath());
            editorActionsActiveNode.Add(new EditorActionCutAndStoreTail());
            editorActionsActiveNode.Add(new EditorActionAutoConnectTail());
            editorActionsActiveNode.Add(new EditorActionRemoveStartKeepTail());
            
            // active track location actions
            editorActionsActiveTrack = new List<EditorAction>();
            editorActionsActiveTrack.Add(new EditorActionAddEnd());
            editorActionsActiveTrack.Add(new EditorActionAddReverse());
            editorActionsActiveTrack.Add(new EditorActionAddWait());
            editorActionsActiveTrack.Add(new EditorActionAddStart());

            // Actions related to broken nodes/paths
            editorActionsBroken = new List<EditorAction>();
            editorActionsBroken.Add(new EditorActionFixInvalidNode());
            editorActionsBroken.Add(new EditorActionAutoFixBrokenNodes());
            editorActionsBroken.Add(new EditorActionDrawToNextBrokenPoint(DrawUntilHere));
            
            // Various other actions
            editorActionsOthers = new List<EditorAction>();
            editorActionsOthers.Add(new EditorActionDrawUntilHere(DrawUntilHere));
        }

        /// <summary>
        /// Create the list of actions that can be performed on a mouse click.
        /// </summary>
        private void CreateMouseClickEntries()
        {
            // the order in which the actions are defined determines the order in which they are tried
            editorActionsMouseClicked = new List<EditorAction>();
            editorActionsMouseClicked.Add(new EditorActionAddStart());
            editorActionsMouseClicked.Add(new EditorActionTakeOtherExit());
            editorActionsMouseClicked.Add(new EditorActionTakeOtherExitPassingPath());
            editorActionsMouseClicked.Add(new EditorActionAutoFixBrokenNodes());
            editorActionsMouseClicked.Add(new EditorActionRemovePassingPath());
            //editorActionsMouseClicked.Add(new EditorActionDrawUntilHere(DrawUntilHere));

            // no longer possible because these nodes will be dragged. Can be added when using Alt instead of control?
            //editorActionsMouseClicked.Add(new EditorActionOtherStartDirection());
            //editorActionsMouseClicked.Add(new EditorActionEditWait());
            //editorActionsMouseClicked.Add(new EditorActionRemoveEnd());
            //editorActionsMouseClicked.Add(new EditorActionRemoveReverse());
            
        }

        /// <summary>
        /// Create the context menu from previously defined items.
        /// </summary>
        private void CreateContextMenu() {

            contextMenu = new ContextMenu();
            separatorActiveNode = new Separator();
            separatorActiveTrack = new Separator();
            separatorBroken = new Separator();

            noActionPossibleMenuItem = new MenuItem();
            noActionPossibleMenuItem.Header = "No action possible\nPerhaps paths are not drawn.";
            contextMenu.Items.Add(noActionPossibleMenuItem);

            foreach (EditorAction action in editorActionsActiveNode)
            {
                contextMenu.Items.Add(action.ActionMenuItem);
            }
            contextMenu.Items.Add(separatorActiveNode);
            

            foreach (EditorAction action in editorActionsActiveTrack)
            {
                contextMenu.Items.Add(action.ActionMenuItem);
            }
            contextMenu.Items.Add(separatorActiveTrack);

            foreach (EditorAction action in editorActionsBroken)
            {
                contextMenu.Items.Add(action.ActionMenuItem);
            }
            contextMenu.Items.Add(separatorBroken);
            
            foreach (EditorAction action in editorActionsOthers)
            {
                contextMenu.Items.Add(action.ActionMenuItem);
            }

            contextMenu.Closed += new RoutedEventHandler(ContextMenu_Closed);
        }
       
        /// <summary>
        /// Popup the context menu at the given location. Also disable updates related to mouse movement while menu is open.
        /// </summary>
        /// <param name="mouseX"></param>
        /// <param name="mouseY"></param>
        public void PopupContextMenu(int mouseX, int mouseY)
        {
            bool someNodeActionIsPossible = false;
            bool someTrackActionIsPossible = false;
            bool someBrokenActionIsPossible = false;
            bool someOtherActionIsPossible = false;

            foreach (EditorAction action in editorActionsActiveNode)
            {
                bool actionCanBeExecuted = action.MenuState(currentTrainPath, activeNode, activeTrackLocation, UpdateAfterEdits,
                    mouseX, mouseY);
                someNodeActionIsPossible = someNodeActionIsPossible || actionCanBeExecuted;
            }
            separatorActiveNode.Visibility = someNodeActionIsPossible ? Visibility.Visible : Visibility.Collapsed;

            foreach (EditorAction action in editorActionsActiveTrack)
            {
                bool actionCanBeExecuted = action.MenuState(currentTrainPath, activeNode, activeTrackLocation, UpdateAfterEdits,
                    mouseX, mouseY);
                someTrackActionIsPossible = someTrackActionIsPossible || actionCanBeExecuted;
            }
            separatorActiveTrack.Visibility = someTrackActionIsPossible ? Visibility.Visible : Visibility.Collapsed;

            foreach (EditorAction action in editorActionsBroken)
            {
                bool actionCanBeExecuted = action.MenuState(currentTrainPath, activeNode, activeTrackLocation, UpdateAfterEdits,
                    mouseX, mouseY);
                someBrokenActionIsPossible = someBrokenActionIsPossible || actionCanBeExecuted;
            }
            separatorBroken.Visibility = someBrokenActionIsPossible ? Visibility.Visible : Visibility.Collapsed;

            foreach (EditorAction action in editorActionsOthers)
            {
                bool actionCanBeExecuted = action.MenuState(currentTrainPath, activeNode, activeTrackLocation, UpdateAfterEdits,
                    mouseX, mouseY);
                someOtherActionIsPossible = someOtherActionIsPossible || actionCanBeExecuted;
            }

            noActionPossibleMenuItem.Visibility = 
                (someNodeActionIsPossible || someTrackActionIsPossible || someOtherActionIsPossible)
                ? Visibility.Collapsed : Visibility.Visible;

            contextMenu.PlacementRectangle = new Rect((double)mouseX, (double)mouseY, 20, 20);
            contextMenu.IsOpen = true;
            EnableMouseUpdate = false;
        }

        /// <summary>
        /// See whether an action is possible based on mouse click and perform it.
        /// </summary>
        /// <param name="mouseX"></param>
        /// <param name="mouseY"></param>
        public void OnLeftMouseClick(int mouseX, int mouseY)
        {
            foreach (EditorActionMouseDrag mouseDragAction in mouseDragActions)
            {
                if (mouseDragAction.MenuState(currentTrainPath, activeNode, activeTrackLocation, UpdateAfterEdits,
                        mouseX, mouseY))
                {
                    activeMouseDragAction = mouseDragAction;
                    activeMouseDragAction.StartDragging();
                    return;
                }
            }

            foreach (EditorAction action in editorActionsMouseClicked)
            {
                bool actionCanBeExecuted = action.MenuState(currentTrainPath, activeNode, activeTrackLocation, UpdateAfterEdits,
                    mouseX, mouseY);
                if (actionCanBeExecuted)
                {
                    action.DoAction();
                    break;
                }
            }
        }

        /// <summary>
        /// Perform the actions needed when the left-mouse is moved, normally during dragging
        /// </summary>
        public void OnLeftMouseMoved()
        {
            if (activeMouseDragAction == null) return;
            activeMouseDragAction.Dragging();
        }

        /// <summary>
        /// Mouse has been released, so dragging is ended. Perform the corresponding closing actions
        /// </summary>
        public void OnLeftMouseRelease()
        {
            if (activeMouseDragAction == null) return;

            activeMouseDragAction.EndDragging();
            activeMouseDragAction = null;
            OnPathChanged();
        }

        /// <summary>
        /// User has decided to cancel the dragging action. Make sure his wishes are followed
        /// </summary>
        public void OnLeftMouseCancel()
        {
            if (activeMouseDragAction == null) return;

            activeMouseDragAction.CancelDragging();
            activeMouseDragAction = null;
        }



        /// <summary>
        /// Close the context menu, to be called, for instance, if the user does something else than clicking on the context menu
        /// </summary>
        void CloseContextMenu()
        {
            if (contextMenu == null) { return; }
            contextMenu.IsOpen = false;
        }

        /// <summary>
        /// When the context menu closes, enable updates based on mouse movement again.
        /// </summary>
        void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            EnableMouseUpdate = true;
        }

        /// <summary>
        /// Callback that will be called from EditorActions after edits have been performed
        /// </summary>
        /// <param name="nodesAdded">Number of nodes that have been added during edits</param>
        void UpdateAfterEdits(int nodesAdded)
        {
            numberToDraw += nodesAdded;
            OnPathChanged();
        }

        /// <summary>
        /// Callback that will be called from EditorActions to set the number to draw.
        /// </summary>
        /// <param name="newNumberToDraw">The new number of nodes to draw, -1 if not defined.</param>
        void DrawUntilHere(int newNumberToDraw)
        {
            if (newNumberToDraw >= 0)
            {
                numberToDraw = newNumberToDraw;
            }
        }
        #endregion

        #region Drawing, active node & track location
        /// <summary>
        /// Find the active node and active node candidate and draw them
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        public void Draw(DrawArea drawArea)
        {
            DrawnPathData drawnPathData = new DrawnPathData();
            
            int numberDrawn = drawPath.Draw(drawArea, currentTrainPath.FirstNode, currentTrainPath.FirstNodeOfTail, numberToDraw, 
                drawnPathData);

            if (numberDrawn < numberToDraw)
            {
                // Apparently we were not able to draw all nodes. Reset maximum number to draw, and possibly add a node
                numberToDraw = numberDrawn;
                if (EditingIsActive && allowAddingNodes 
                    && (CurrentNode != null) && (CurrentNode.NodeType != TrainpathNodeType.End))
                {
                    nonInteractiveAction.AddMainNode(CurrentNode, UpdateAfterEdits);
                }
            }

            if (!EditingIsActive)
            {
                return;
            }

            if (EnableMouseUpdate)
            {
                FindActiveNode(drawArea, drawnPathData);
                FindActiveTrackLocation(drawnPathData);
            }

            float textureSize = 8f;
            int minPixelSize = 7;
            int maxPixelSize = 24;
            if (activeNode != null && activeNode.Location != null)
            {
                drawArea.DrawTexture(activeNode.Location, "ring", textureSize, minPixelSize, maxPixelSize, DrawColors.colorsNormal.ActiveNode);

            }
            if (activeTrackLocation != null && activeTrackLocation.Location != WorldLocation.None)
            {
                drawArea.DrawTexture(activeTrackLocation.Location, "ring", textureSize, minPixelSize, maxPixelSize, DrawColors.colorsNormal.CandidateNode);
            }

        }

        /// <summary>
        /// Find the node in the path that is closest to the mouse,
        /// </summary>
        /// <param name="drawArea">Area that is being drawn upon and where we have a mouse location</param>
        /// <param name="drawnPathData">The data structure with the information on the drawn path</param>
        void FindActiveNode(DrawArea drawArea, DrawnPathData drawnPathData)
        {
            // Initial simplest implementation: find simply the closest and first.
            float closestMouseDistanceSquared = float.MaxValue;
            TrainpathNode closestNode = null;
            foreach (TrainpathNode node in drawnPathData.DrawnNodes)
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
        /// <param name="drawnPathData">The data structure with the information on the drawn path</param>
        void FindActiveTrackLocation(DrawnPathData drawnPathData)
        {
            if (drawTrackDB.ClosestTrack == null ||
                drawTrackDB.ClosestTrack.TrackNode == null)
            {
                activeTrackLocation.Location = WorldLocation.None;
                return;
            }

            uint tni = drawTrackDB.ClosestTrack.TrackNode.Index;
            int tni_int = (int)tni;

            if (!drawnPathData.TrackHasBeenDrawn(tni_int) && currentTrainPath.FirstNode != null && activeMouseDragAction == null)
            {
                activeTrackLocation.Location = WorldLocation.None;
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

            if ( (currentTrainPath.FirstNode != null) && (activeMouseDragAction == null) ) 
            {   //Only in case this is not the first path.
                TrainpathNode prevNode = FindPrevNodeOfActiveTrack(drawnPathData, tni_int);
                if (prevNode == null || prevNode.HasSidingPath)
                {
                    activeTrackLocation.Location = WorldLocation.None;
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
        /// <param name="drawnPathData">The data structure with the information on the drawn path</param>
        /// <param name="trackNodeIndex">The index of the track node</param>
        /// <returns>The node that will be before a possible new node on the track</returns>
        TrainpathNode FindPrevNodeOfActiveTrack(DrawnPathData drawnPathData, int trackNodeIndex)
        {
            Collection<TrainpathNode> nodesOnTrack = drawnPathData.NodesOnTrack(trackNodeIndex);
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

        #endregion

        #region Actions to take when editing is enabled
        /// <summary>
        /// Once the editing becomes active for this path, we make sure the path is 'clean' according to our standards
        /// </summary>
        void OnActiveOrPathChanged()
        {
            if (!EditingIsActive) { return; }
            SnapAllJunctionNodes();
            AddMissingDisambiguityNodes();
            OnPathChanged();
        }

        /// <summary>
        /// Make sure the junction nodes of have the exact location of the junctions in the track database.
        /// This is to make sure changes in the track database are taken over in the path
        /// </summary>
        void SnapAllJunctionNodes()
        {
            TrainpathNode mainNode = currentTrainPath.FirstNode;
            while (mainNode != null)
            {
                //siding path. For this routine we do not care if junctions are done twice
                TrainpathNode sidingNode = mainNode.NextSidingNode;
                while (sidingNode != null)
                {
                    TrainpathJunctionNode sidingNodeAsJunction = sidingNode as TrainpathJunctionNode;
                    if ((sidingNodeAsJunction != null) && !sidingNode.IsBroken)
                    {
                        sidingNode.Location = DrawTrackDB.UidLocation(trackDB.TrackNodes[sidingNodeAsJunction.JunctionIndex].UiD);
                    }
                    sidingNode = sidingNode.NextSidingNode;
                }

                TrainpathJunctionNode mainNodeAsJunction = mainNode as TrainpathJunctionNode;
                if ((mainNodeAsJunction != null) && !mainNode.IsBroken)
                {
                    mainNode.Location = DrawTrackDB.UidLocation(trackDB.TrackNodes[mainNodeAsJunction.JunctionIndex].UiD);
                }
                mainNode = mainNode.NextMainNode;
            }
        }

        /// <summary>
        /// Not all paths have enough disambiguity nodes to distinghuish between two possible paths. Here we add them
        /// </summary>
        void AddMissingDisambiguityNodes()
        {
            nonInteractiveAction.AddMissingDisambiguityNodes(currentTrainPath, UpdateAfterEdits);
        }

        #endregion

        #region Metadata, saving, reversing
        /// <summary>
        /// Popup a dialog to enable to user to edit the path meta data
        /// </summary>
        public void EditMetaData()
        {
            string[] metadata = { currentTrainPath.PathId, currentTrainPath.PathName, currentTrainPath.PathStart, currentTrainPath.PathEnd };
            bool isPlayerPath = (currentTrainPath.PathFlags & PathFlags.NotPlayerPath) == 0;
            PathMetadataDialog metadataDialog = new PathMetadataDialog(metadata, isPlayerPath);
            TrackViewer.Localize(metadataDialog);
            if (metadataDialog.ShowDialog() == true)
            {
                metadata = metadataDialog.GetMetadata();
                currentTrainPath.PathId = metadata[0];
                currentTrainPath.PathName = metadata[1];
                currentTrainPath.PathStart = metadata[2];
                currentTrainPath.PathEnd = metadata[3];

                isPlayerPath = (metadata[4] == true.ToString());
                if (isPlayerPath)
                {
                    currentTrainPath.PathFlags &= ~PathFlags.NotPlayerPath; // unset the nonplayerpath flag
                }
                else
                {
                    currentTrainPath.PathFlags |= PathFlags.NotPlayerPath; // set the nonplayerpath flag
                }
            }
        }

        /// <summary>
        /// Save the path to file, converting the internal representation to .pat file format
        /// </summary>
        public void SavePath()
        {
            SavePatFile.WritePatFile(currentTrainPath);
        }

        /// <summary>
        /// Save the names of the stations along the path to a file.
        /// </summary>
        public void SaveStationNames()
        {
            string[] stationNames = currentTrainPath.StationNames();
            SaveStationNames saveStationNames = new Editing.SaveStationNames();
            saveStationNames.SaveToFile(stationNames);
        }

        /// <summary>
        /// Reverse the path including metadata, but first check if the path is clean enough.
        /// Note that reversing is like any other action, in the sense that it allows an undo.
        /// </summary>
        public void ReversePath()
        {
            if (! CanReverse()) return;
            currentTrainPath.StoreCurrentPath();
            currentTrainPath.ReversePath();
            EditMetaData();
        }

        private bool CanReverse()
        {
            if (currentTrainPath.IsBroken)
            {
                MessageBox.Show(TrackViewer.catalog.GetString("Reversing broken paths is not supported"));
                return false;
            }
            if (currentTrainPath.FirstNode == null)
            {
                MessageBox.Show(TrackViewer.catalog.GetString("Reversing a path without start node is not supported"));
                return false;
            }
            if (!currentTrainPath.HasEnd)
            {
                MessageBox.Show(TrackViewer.catalog.GetString("Reversing a path without end node is not supported"));
                return false;
            }

            return true;
        }

        #endregion

        #region Extending and reducing path drawing
        /// <summary>
        /// Draw more sections of the path
        /// </summary>
        public void ExtendPath()
        {
            ++numberToDraw;
            if (EditingIsActive)
            {
                allowAddingNodes = !currentTrainPath.HasEnd;
            }
            else
            {
                allowAddingNodes = false;
            }
            CloseContextMenu();
        }

        /// <summary>
        /// Draw the full (complete) path
        /// </summary>
        public void ExtendPathFull()
        {
            allowAddingNodes = false;
            numberToDraw = practicalInfinityInt;
            CloseContextMenu();
        }

        /// <summary>
        /// Draw less sections of the path
        /// </summary>
        public void ReducePath()
        {
            if (--numberToDraw < 1) numberToDraw = 1;
            CloseContextMenu();
        }

        /// <summary>
        /// Go to initial node and draw no path sections
        /// </summary>
        public void ReducePathFull()
        {
            numberToDraw = 1;
            CloseContextMenu();
        }

        #endregion

        #region Undo / Redo
        /// <summary>
        /// Undo the last editor action
        /// </summary>
        public void Undo()
        {
            if (activeMouseDragAction != null) return; // do not support Undo while dragging
            currentTrainPath.Undo();
            CloseContextMenu();
        }

        /// <summary>
        /// Redo the last Undo (if available)
        /// </summary>
        public void Redo()
        {
            if (activeMouseDragAction != null) return; // do not support Redo while dragging
            currentTrainPath.Redo();
            CloseContextMenu();
            OnPathChanged();
        }

        #endregion

        #region Events
        /// <summary>
        /// Event to be called whenever the path has changed
        /// </summary>
        public event ChangedPathHandler ChangedPath;

        void OnPathChanged()
        {
            if (ChangedPath != null)
                ChangedPath();
        }
        #endregion
    }

}
