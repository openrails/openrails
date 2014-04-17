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

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using MSTS.Formats;
using ORTS.Common;
using ORTS.TrackViewer.Drawing;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>Delegate to enable giving information on the edited path back to the editor</summary>
    public delegate void AfterEditDelegate(int nodesAdded);
        
    /// <summary>
    /// Base class for all kinds of editor actions. This contains all the common glue to support the action (like menuItem).
    /// It also contains all the common methods that edit/modify the path.
    /// </summary>
    public abstract class EditorAction
    {
        #region public fields
        /// <summary>The MenuItem that can be added to a context menu</summary>
        public MenuItem ActionMenuItem { get; private set; }
        #endregion

        #region private/protected fields
        /// <summary>The trainpath that will be edited</summary>
        protected Trainpath Trainpath { get; private set; }
        /// <summary>The node that is currently active and on which the action of (some) subclasses will act</summary>
        protected TrainpathNode ActiveNode { get; private set; }
        /// <summary>The currently active location on a track node on which the action of (some) subclasses will act</summary>
        protected TrainpathVectorNode ActiveTrackLocation { get; private set; }
        /// <summary>The currently active location on a track node on which the action of (some) subclasses will act</summary>
        protected int NetNodesAdded { get; set; }
        /// <summary>x-location of the mouse</summary>
        protected int MouseX { get; set; }
        /// <summary>y-location of the mouse</summary>
        protected int MouseY { get; set; }

        private AfterEditDelegate afterEditCallback;
        #endregion

        /// <summary>
        /// Constructor. Creates the menuitem to be used in the context menu.
        /// </summary>
        /// <param name="menuHeader">The 'header' to be used in the contect menu</param>
        /// <param name="pngFileName">The name of the png icon to use</param>
        protected EditorAction(string menuHeader, string pngFileName)
        {
            ActionMenuItem = new MenuItem();
            ActionMenuItem.Header = menuHeader;
            ActionMenuItem.IsCheckable = false;
            ActionMenuItem.Click += new RoutedEventHandler(contextExecuteAction_Click);

            string contentPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "Content");
           
            string fullFileName = System.IO.Path.Combine(contentPath, pngFileName + ".png");
            ActionMenuItem.Icon = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri(fullFileName, UriKind.Relative)),
                Width=14,
                Height=14,
            };
            
        }
        
        /// <summary>
        /// Set the state of the item in the menu (depending on whether the action can be executed or not.
        /// </summary>
        /// <returns>true when the action can be executed</returns>
        public bool MenuState(Trainpath trainpath, TrainpathNode activeNode, TrainpathVectorNode activeTrackLocation,
            AfterEditDelegate callback, int mouseX, int mouseY)
        {
            this.Trainpath = trainpath;
            this.ActiveNode = activeNode;
            this.ActiveTrackLocation = activeTrackLocation;
            this.afterEditCallback = callback;
            this.MouseX = mouseX;
            this.MouseY = mouseY;

            bool canExecute =
                ORTS.TrackViewer.Properties.Settings.Default.showTrainpath
                && CanExecuteAction();
            ActionMenuItem.Visibility = canExecute ? Visibility.Visible : Visibility.Collapsed;

            return canExecute;
        }

        /// <summary>
        /// Callback that is called when the user clicks on the menuitem connected to this action
        /// </summary>
        /// <param name="sender">Sender that generates the event, not used</param>
        /// <param name="e">(routed) event arguments, not used</param>
        private void contextExecuteAction_Click(object sender, RoutedEventArgs e)
        {
            DoAction();
        }

        /// <summary>
        /// Perform the action, taking care of saving the path for undo and calling the callback at the end
        /// </summary>
        public void DoAction()
        {
            Trainpath.StoreCurrentPath();
            NetNodesAdded = 0;
            ExecuteAction();
            if (afterEditCallback != null)
            {
                afterEditCallback(NetNodesAdded);
            }
        }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected abstract void ExecuteAction();
        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected abstract bool CanExecuteAction();

        #region Common path modification methods
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
        protected TrainpathNode AddAdditionalNode(TrainpathNode lastNode, int tvnIndex, bool isMainPath)
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

            TrackNode linkingTrackNode = TrackExtensions.TrackNode(nextTvnIndex);
            if (linkingTrackNode == null)
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
        protected TrainpathVectorNode AddAdditionalVectorNode(TrainpathNode lastNode, TrainpathVectorNode nodeCandidate, bool isMainPath)
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

            NetNodesAdded++; // We want to keep the path drawn to the same node as before adding a node
            return newNode;
        }

        /// <summary>
        /// Create a (still unlinked) node halfway through the next section (so halfway between this
        /// and the next junction. Needed specially for disambiguity.
        /// </summary>
        /// <param name="junctionNode">The junction node where we start</param>
        /// <param name="tvnIndex">The TrackVectorNode index for the path</param>
        /// <returns>An unlinked vectorNode at the midpoint.</returns>
        protected static TrainpathVectorNode CreateHalfWayNode(TrainpathJunctionNode junctionNode, int tvnIndex)
        {   // The idea here is to use all the code in traveller to make life easier.

            Traveller traveller = junctionNode.PlaceTravellerAfterJunction(tvnIndex);

            TrackNode nextJunctionTrackNode = TrackExtensions.TrackNode(junctionNode.GetNextJunctionIndex(tvnIndex));
            WorldLocation nextJunctionLocation = DrawTrackDB.UidLocation(nextJunctionTrackNode.UiD);

            //move the traveller halfway through the next vector section
            float distanceToTravel = traveller.DistanceTo(nextJunctionLocation) / 2;
            traveller.Move(distanceToTravel);

            TrainpathVectorNode halfwayNode = new TrainpathVectorNode(junctionNode, traveller);
            halfwayNode.DetermineOrientation(junctionNode, tvnIndex);

            return halfwayNode;
        }
        
        /// <summary>
        /// Remove the current active point and all of the path that follows. Add a new next node along the path.
        /// </summary>
        protected void ReplaceNodeAndFollowingByNewNode(TrainpathNode firstNodeToRemove)
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
        /// Add a new vector node at the given location in the middle of a path
        /// </summary>
        /// <param name="nodeCandidate"></param>
        /// <returns>The just created node</returns>
        protected TrainpathVectorNode AddIntermediateNode(TrainpathVectorNode nodeCandidate)
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

            //remove possible disambiguity node 
            if (prevNode.NodeType == TrainpathNodeType.Other && prevNode is TrainpathVectorNode)
            {
                RemoveIntermediatePoint(prevNode);
            }
            if (nextNode.NodeType == TrainpathNodeType.Other && nextNode is TrainpathVectorNode)
            {
                RemoveIntermediatePoint(nextNode);
            }

            NetNodesAdded++;
            return newNode;
        }

        /// <summary>
        /// Remove a intermediate vector node. Possibly add a disambiguity node if needed.
        /// </summary>
        /// <param name="curNode">Node to be removed</param>
        protected void RemoveIntermediatePoint(TrainpathNode curNode)
        {
            TrainpathNode prevNode = curNode.PrevNode;
            prevNode.NextMainNode = curNode.NextMainNode;
            prevNode.NextSidingNode = null; // should not be needed
            //lastNodeSidingPath.NextMainTvnIndex should be the same still
            if (prevNode.NextMainNode != null)
            {   // there might not be a next node.
                prevNode.NextMainNode.PrevNode = prevNode;
            }
            NetNodesAdded--;

            //Check if we need to add an disambiguity node
            TrainpathJunctionNode prevJunctionNode = prevNode as TrainpathJunctionNode;
            if (prevJunctionNode!= null && (prevNode.NextMainNode is TrainpathJunctionNode))
            {
                if (prevJunctionNode.IsSimpleSidingStart())
                {
                    TrainpathVectorNode halfwayNode = CreateHalfWayNode(prevJunctionNode, prevJunctionNode.NextMainTvnIndex);
                    halfwayNode.PrevNode = prevNode;
                    AddIntermediateNode(halfwayNode);
                }
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
        protected TrainpathNode CreatePartialPath(TrainpathNode currentNode, int newTvnIndex, TrainpathJunctionNode reconnectNode, bool isMainPath)
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


        #endregion
    }

    #region Template
    /// <summary>
    /// Subclass to implement the action:
    /// </summary>
    public class EditorActionTemplate : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionTemplate() : base(TrackViewer.catalog.GetString("Not to be used, template only"), "") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return true;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
        }
    }
    #endregion
    
    #region AddStart
    /// <summary>
    /// Subclass to implement the action: Add Start Node 
    /// </summary>
    public class EditorActionAddStart : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionAddStart() : base(TrackViewer.catalog.GetString("Place start point"), "activeTrack") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            return (Trainpath.FirstNode == null);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            Trainpath.FirstNode = new TrainpathVectorNode(ActiveTrackLocation);
            NetNodesAdded++; // first node is not counted automatically
            Trainpath.FirstNode.NodeType = TrainpathNodeType.Start;
            AddAdditionalNode(Trainpath.FirstNode, true); // make sure also the second node is available and drawn.
        }
    }
    #endregion

    #region RemoveStart
    /// <summary>
    /// Subclass to implement the action: Remove Start point.
    /// This action removes start node and subsequently the complete path. (only path metadata will be unchanged!)
    /// </summary>
    public class EditorActionRemoveStart : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionRemoveStart() : base(TrackViewer.catalog.GetString("Remove start point"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (Trainpath.HasEnd) return false;
            return (ActiveNode == Trainpath.FirstNode);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            Trainpath.FirstNode = null;
        }
    }
    #endregion

    #region OtherStartDirection
    /// <summary>
    /// Subclass to implement the action: Take the other direction  from the start point
    /// </summary>
    public class EditorActionOtherStartDirection : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionOtherStartDirection() : base(TrackViewer.catalog.GetString("Change start direction"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.HasEnd) return false;
            if (Trainpath.FirstNode == null) return false;
            return (ActiveNode == Trainpath.FirstNode);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            Trainpath.FirstNode.NextMainNode = null;
            Trainpath.FirstNode.NextSidingNode = null;
            Trainpath.FirstNode.ReverseOrientation();
            AddAdditionalNode(Trainpath.FirstNode, true);
        }
    }
    #endregion

    #region AddEnd
    /// <summary>
    /// Subclass to implement the action: Add End Node 
    /// </summary>
    public class EditorActionAddEnd : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionAddEnd() : base(TrackViewer.catalog.GetString("Place end point"), "activeTrack") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (Trainpath.HasEnd) return false;
            return (ActiveTrackLocation.Location != null
                 && !ActiveTrackLocation.PrevNode.HasSidingPath
                 && Trainpath.FirstNodeOfTail == null);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            //activeTrackLocation is the place where we want to place the node
            //Its field lastNodeSidingPath is also given.
            TrainpathNode lastNode = ActiveTrackLocation.PrevNode;

            //remove basically rest of path.
            lastNode.NextSidingNode = null;
            lastNode.NextMainNode = null;

            TrainpathVectorNode newNode = AddAdditionalVectorNode(lastNode, ActiveTrackLocation, true);
            newNode.NodeType = TrainpathNodeType.End;
            Trainpath.HasEnd = true;
        }
    }
    #endregion

    #region RemoveEnd
    /// <summary>
    /// Subclass to implement the action: Remove end node
    /// </summary>
    public class EditorActionRemoveEnd : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionRemoveEnd() : base(TrackViewer.catalog.GetString("Remove end point"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (!Trainpath.HasEnd) return false;
            return (ActiveNode.NodeType == TrainpathNodeType.End);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            ReplaceNodeAndFollowingByNewNode(ActiveNode);
            Trainpath.HasEnd = false;
        }
    }
    #endregion

    #region AddReverse
    /// <summary>
    /// Subclass to implement the action: Add Reversal node
    /// </summary>
    public class EditorActionAddReverse : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionAddReverse() : base(TrackViewer.catalog.GetString("Place reversal point"), "activeTrack") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (Trainpath.HasEnd) return false;
            return (ActiveTrackLocation.Location != null
                && !ActiveTrackLocation.PrevNode.HasSidingPath);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            TrainpathNode lastNode = ActiveTrackLocation.PrevNode;

            TrainpathVectorNode newNode = AddAdditionalVectorNode(lastNode, ActiveTrackLocation, true);
            newNode.NodeType = TrainpathNodeType.Reverse;
            lastNode.NextMainNode = newNode;
            lastNode.NextSidingNode = null; // should not be needed
            lastNode.NextMainTvnIndex = newNode.TvnIndex;
            newNode.PrevNode = lastNode;

            newNode.ReverseOrientation(); // reverse because, well, this is a reverse point.
        }
    }
    #endregion

    #region RemoveReverse
    /// <summary>
    /// Subclass to implement the action: Remove reversal node
    /// </summary>
    public class EditorActionRemoveReverse : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionRemoveReverse() : base(TrackViewer.catalog.GetString("Remove reversal point"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (Trainpath.HasEnd) return false;
            return (ActiveNode.NodeType == TrainpathNodeType.Reverse);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            ReplaceNodeAndFollowingByNewNode(ActiveNode);
        }
    }
    #endregion

    #region Wait (Common)
    /// <summary>
    /// Subclass to implement common methods for wait points 
    /// </summary>
    public abstract class EditorActionWait : EditorAction
    {
        /// <summary>Constructor</summary>
        protected EditorActionWait(string header, string pngFileName) : base(header, pngFileName) { }

        /// <summary>
        /// Popup the dialog that allows you to edit the metadata of a wait point
        /// </summary>
        /// <param name="nodeToEdit">(Wait) node for which you want to edit the metadata.</param>
        protected void EditWaitMetaData(TrainpathVectorNode nodeToEdit)
        {
            if (nodeToEdit.WaitTimeS == 0 && nodeToEdit.WaitUntil == 0)
            {
                nodeToEdit.WaitTimeS = 600; // some initial value: 10 minutes
            }
            WaitPointDialog waitDialog = new WaitPointDialog(MouseX, MouseY, nodeToEdit.WaitTimeS//, nodeToEdit.WaitUntil
                );
            if (waitDialog.ShowDialog() == true)
            {
                //if (waitDialog.UntilSelected())
                //{
                //    nodeToEdit.WaitUntil = waitDialog.GetWaitTime;
                //}
                //else
                //{
                    nodeToEdit.WaitTimeS = waitDialog.GetWaitTime;
                //}
            }
        }
    }
    #endregion

    #region AddWait
    /// <summary>
    /// Subclass to implement the action: Add Wait point
    /// </summary>
    public class EditorActionAddWait : EditorActionWait
    {
        /// <summary>Constructor</summary>
        public EditorActionAddWait() : base(TrackViewer.catalog.GetString("Place wait point"), "activeTrack") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return (ActiveTrackLocation.Location != null
                && !ActiveTrackLocation.PrevNode.HasSidingPath);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            TrainpathVectorNode newNode = AddIntermediateNode(ActiveTrackLocation);
            newNode.NodeType = TrainpathNodeType.Stop;
            EditWaitMetaData(newNode);
        }
    }
    #endregion

    #region EditWait
    /// <summary>
    /// Subclass to implement the action: Edit wait point
    /// </summary>
    public class EditorActionEditWait : EditorActionWait
    {
        /// <summary>Constructor</summary>
        public EditorActionEditWait() : base(TrackViewer.catalog.GetString("Edit wait point"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return (ActiveNode.NodeType == TrainpathNodeType.Stop);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            EditWaitMetaData(ActiveNode as TrainpathVectorNode);
        }
    }
    #endregion

    #region RemoveWait
    /// <summary>
    /// Subclass to implement the action: Add Remove wait
    /// </summary>
    public class EditorActionRemoveWait : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionRemoveWait() : base(TrackViewer.catalog.GetString("Remove wait point"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return (ActiveNode.NodeType == TrainpathNodeType.Stop);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            //Assumption, there is no siding next to it. but this might not be true. Not clear if the assumption is needed
            if (ActiveNode.NextMainNode != null)
            {
                RemoveIntermediatePoint(ActiveNode);
            }
            else
            {
                ReplaceNodeAndFollowingByNewNode(ActiveNode);
            }
        }
    }
    #endregion

    #region OtherExit (Common)
    /// <summary>
    /// Subclass to define common methods related to other exit
    /// </summary>
    public abstract class EditorActionOtherExit : EditorAction
    {
        const int maxNumberNodesToCheckForReconnect = 10; // maximum number of nodes we will try before we conclude not reconnection is possible.
        /// <summary>Store the node where we might be able to reconnect during a take-other-exit</summary>
        protected TrainpathJunctionNode ReconnectNode { get; private set; }
        /// <summary>The index of the new TrackVectorNode</summary>
        protected int NewTvnIndex { get; private set; }
        /// <summary>List of junction indices on the new path we want to create</summary>
        private List<int> newMainJunctionIndexes;

        /// <summary>Constructor</summary>
        protected EditorActionOtherExit(string header, string pngFileName) : base(header, pngFileName) { }

        /// <summary>
        /// Calculate whether it is possible to take the other exit (possibly needing to reconnect automatically using main tracks at sidings)
        /// </summary>
        /// <param name="addPassingPath">For a passing path it is always needed to reconnect</param>
        /// <returns>Whether the other exit can be taken</returns>
        protected bool CanTakeOtherExit(bool addPassingPath)
        {
            TrainpathJunctionNode activeNodeAsJunction = ActiveNode as TrainpathJunctionNode;
            
            return
                CanTakeOtherExitBasic(activeNodeAsJunction) &&
                CanReconnectOtherExit(activeNodeAsJunction, addPassingPath);
        }

        /// <summary>
        /// Determine whether some basic requirements to take the other exit are fullfilled.
        /// </summary>
        /// <param name="activeNodeAsJunction">Active Node as a junction node</param>
        /// <returns>true if basic requirements are met</returns>
        protected bool CanTakeOtherExitBasic(TrainpathJunctionNode activeNodeAsJunction)
        {
            if (activeNodeAsJunction == null
                || ActiveNode.NextMainNode == null
                || !activeNodeAsJunction.IsFacingPoint
                || (ActiveNode.NodeType == TrainpathNodeType.SidingStart)
                )
            {
                return false;
            }
            NewTvnIndex = activeNodeAsJunction.OtherExitTvnIndex();
            return true;
        }

        /// <summary>
        /// Calculate whether it is possible to take the other exit (possibly needint to reconnect automatically using main tracks at sidings)
        /// </summary>
        /// <param name="activeNodeAsJunction">Active Node as a junction node</param>
        /// <param name="addPassingPath">For a passing path it is always needed to reconnect</param>
        /// <returns>Whether the other exit can be taken</returns>
        protected bool CanReconnectOtherExit(TrainpathJunctionNode activeNodeAsJunction, bool addPassingPath)
        {
            ReconnectNode = FindReconnectNode(activeNodeAsJunction, NewTvnIndex);

            if (ReconnectNode == null)
            {   // we can not reconnect. So this is a destructive take-other-exit
                if (Trainpath.HasEnd) return false;
                if (addPassingPath) return false;
                if (ActiveNode.HasSidingPath) return false;
                return true;
            }
            else
            {
                // if there is no siding path, we can just reconnect either a new main or a siding path:
                if (!ActiveNode.HasSidingPath) return true; 
                // We already have a siding path, so we cannot add another
                if (addPassingPath) return false;

                //Main path has siding path. Now there is the risk of putting the track over the passing/siding path
                List<int> sidingJunctionIndexes = IntermediateSidingJunctionIndexes(ActiveNode);
                // we can only allow intersection if there are no common indexes:
                return (newMainJunctionIndexes.Intersect(sidingJunctionIndexes).Count() == 0);
            }
        }

        /// <summary>
        /// Determine whether we can reconnect from the current junction node to the main track, if we take 
        /// the new TvnIndex
        /// </summary>
        /// <param name="startJunctionNode">junction node (which is a facing point) that is the start of an alternative path</param>
        /// <param name="newTvnIndex">index of the track vector node that is intended to be used for reconnection</param>
        /// <returns></returns>
        private TrainpathJunctionNode FindReconnectNode(TrainpathJunctionNode startJunctionNode, int newTvnIndex)
        {
            //First find the list of nodes we might be able to reconnect to. They should all be junction nodes
            List<TrainpathJunctionNode> reconnectNodes = FindReconnectNodeCandidates(startJunctionNode);
            newMainJunctionIndexes = new List<int>();

            //next we need to follow the new path from the newTvnIndex, and see whether we can reconnect
            int junctionIndex = startJunctionNode.JunctionIndex;
            int TvnIndex = newTvnIndex;
            int junctionsToCheck = maxNumberNodesToCheckForReconnect;
            while (junctionsToCheck > 0)
            {
                int nextJunctionIndex = TrackExtensions.GetNextJunctionIndex(junctionIndex, TvnIndex); 
                foreach (TrainpathJunctionNode reconnectNode in reconnectNodes)
                {
                    if (reconnectNode.JunctionIndex == nextJunctionIndex)
                    {   // we found a link, but we still need to check if we don't connect from the trailing side
                        TrackNode junctionTrackNode = TrackExtensions.TrackNode(nextJunctionIndex);
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
                newMainJunctionIndexes.Add(junctionIndex);
                TvnIndex = TrackExtensions.GetLeavingTvnIndex(junctionIndex, TvnIndex);
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
        static List<TrainpathJunctionNode> FindReconnectNodeCandidates(TrainpathJunctionNode startJunctionNode)
        {
            List<TrainpathJunctionNode> reconnectNodeCandidates = new List<TrainpathJunctionNode>();
            TrainpathNode mainNode = startJunctionNode.NextMainNode;
            //follow the train path and see what we find
            while (mainNode != null)
            {
                TrainpathJunctionNode mainNodeAsJunction = mainNode as TrainpathJunctionNode;
                    
                if (mainNodeAsJunction != null)
                {
                    if (mainNode.NodeType == TrainpathNodeType.SidingStart)
                    {   // if a new siding path is started, stop searching
                        break;
                    }
                    if (!mainNodeAsJunction.IsFacingPoint)
                    {   // add the trailing junction.
                        reconnectNodeCandidates.Add(mainNodeAsJunction);
                    }
                    if (mainNode.NodeType == TrainpathNodeType.SidingEnd)
                    {   // for a main path we cannot reconnect past the end of the current siding path, but the siding end itself is still allowed
                        // for adding a passing path this should never happen
                        break;
                    }
                }
                else
                {
                    if (mainNode.NodeType != TrainpathNodeType.Other)
                    {   // if it is not an other-node (so not a disambiguity node), stop searching
                        break;
                    }
                }
                mainNode = mainNode.NextMainNode;
            }
            return reconnectNodeCandidates;
        }

        /// <summary>
        /// Find the list of junction node indexes of all junction nodes on the siding path but not being either siding
        /// start or siding end.
        /// </summary>
        /// <param name="mainNode">Node on main track for which we want to find the siding junction node indices</param>
        /// <returns>The list with the junction indexes</returns>
        static List<int> IntermediateSidingJunctionIndexes(TrainpathNode mainNode)
        {
            List<int> sidingJunctionIndexes = new List<int>();
            if (!mainNode.HasSidingPath) return sidingJunctionIndexes;

            //first find siding start
            while (mainNode.NodeType != TrainpathNodeType.SidingStart)
            {
                mainNode = mainNode.PrevNode;
            }

            //now follow along siding path.
            TrainpathNode sidingNode = mainNode.NextSidingNode;
            while (sidingNode.NextSidingNode != null)
            {
                TrainpathJunctionNode sidingJunctionNode = sidingNode as TrainpathJunctionNode;
                if (sidingJunctionNode != null)
                {
                    sidingJunctionIndexes.Add(sidingJunctionNode.JunctionIndex);
                }
                sidingNode = sidingNode.NextSidingNode;
            }

            return sidingJunctionIndexes;
        }
    }
    #endregion

    #region TakeOtherExit
    /// <summary>
    /// Subclass to implement the action: Take the other exit at a junction
    /// </summary>
    public class EditorActionTakeOtherExit : EditorActionOtherExit
    {
        /// <summary>Constructor</summary>
        public EditorActionTakeOtherExit() : base(TrackViewer.catalog.GetString("Take other exit"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            return CanTakeOtherExit(false);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            TrainpathJunctionNode activeNodeAsJunction = ActiveNode as TrainpathJunctionNode;
            
            if (ReconnectNode == null)
            {   //really take other exit and discard rest of path
                activeNodeAsJunction.NextMainTvnIndex = NewTvnIndex;
                ReplaceNodeAndFollowingByNewNode(ActiveNode.NextMainNode);
                Trainpath.DetermineIfBroken();
                return;
            }

            //correct NumberToDraw with nodes we will remove
            int numberOfNodesInOldTrack = Trainpath.GetNodeNumber(ReconnectNode) - Trainpath.GetNodeNumber(ActiveNode);
            NetNodesAdded -= numberOfNodesInOldTrack;

            //we can reconnect, so create a path to reconnection point
            TrainpathNode lastNodeNewPath = CreatePartialPath(ActiveNode, NewTvnIndex, ReconnectNode, true);

            //Reconnect.
            lastNodeNewPath.NextMainNode = ReconnectNode;
            ReconnectNode.PrevNode = lastNodeNewPath;
            Trainpath.DetermineIfBroken();
        }
    }
    #endregion

    #region AddPassingPath
    /// <summary>
    /// Subclass to implement the action: Add a passing/siding path
    /// </summary>
    public class EditorActionAddPassingPath : EditorActionOtherExit
    {
        /// <summary>Constructor</summary>
        public EditorActionAddPassingPath() : base(TrackViewer.catalog.GetString("Add passing path"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            return CanTakeOtherExit(true);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            ActiveNode.NodeType = TrainpathNodeType.SidingStart;
            TrainpathNode lastNodeSidingPath = CreatePartialPath(ActiveNode, NewTvnIndex, ReconnectNode, false);

            //reconnect. At this point, newNode is already the same node as reconnectNode. We will discard it
            lastNodeSidingPath.NextSidingNode = ReconnectNode;
            ReconnectNode.NodeType = TrainpathNodeType.SidingEnd;

            //Set HasSidingPath for the main nodes until the reconnectNode
            for (TrainpathNode curMainNode = ActiveNode; curMainNode != ReconnectNode; curMainNode = curMainNode.NextMainNode)
            {
                curMainNode.HasSidingPath = true;
            }

            NetNodesAdded = 0; // for passing paths we do not add net nodes
        }
    }
    #endregion

    #region StartPassingPath
    /// <summary>
    /// Subclass to implement the action: Start a passing path here (but only the first part, end in broken node, reconnect later)
    /// </summary>
    public class EditorActionStartPassingPath : EditorActionOtherExit
    {
        /// <summary>Constructor</summary>
        public EditorActionStartPassingPath() : base(TrackViewer.catalog.GetString("Start passing path"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            TrainpathJunctionNode activeNodeAsJunction = ActiveNode as TrainpathJunctionNode;
            if (!CanTakeOtherExitBasic(activeNodeAsJunction)) return false;
            if (ActiveNode.HasSidingPath) return false;
            
            //The idea was that if a normal passing path is possible, then there is no need for a complex passing path
            //However, if you want to connect not to the default recnnect node, we do need this
            //if (CanReconnectOtherExit(activeNodeAsJunction, true)) return false;
            if (NewNodeWouldBeOnMainTrack()) return false;
            return true;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            // we do add an extra siding path node. We make it broken.
            // Main track is not set to hasSidingNode.
            AddAdditionalNode(ActiveNode, NewTvnIndex, false);
            ActiveNode.NodeType = TrainpathNodeType.SidingStart;
            Trainpath.DetermineIfBroken();
            NetNodesAdded = 0; // no main nodes added
        }

        /// <summary>
        /// determine if the new siding node would itself be immediately on the main track.
        /// </summary>
        /// <returns>true if the new siding node is on the main track.</returns>
        bool NewNodeWouldBeOnMainTrack()
        {
            //first find the junction index off the would-be new node
            int junctionIndexNewNode = ActiveNode.GetNextJunctionIndex(NewTvnIndex);

            TrainpathNode mainNode = ActiveNode.NextMainNode;
            while (mainNode != null)
            {
                TrainpathJunctionNode mainNodeAsJunction = mainNode as TrainpathJunctionNode;
                if ((mainNodeAsJunction != null) && (mainNodeAsJunction.JunctionIndex == junctionIndexNewNode))
                {
                    return true;
                }
                mainNode = mainNode.NextMainNode;
            }
            return false;
        }
    }
    #endregion
    
    #region RemovePassingPath
    /// <summary>
    /// Subclass to implement the action: Remove as passing path
    /// </summary>
    public class EditorActionRemovePassingPath : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionRemovePassingPath() : base(TrackViewer.catalog.GetString("Remove passing path"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return (ActiveNode.NodeType == TrainpathNodeType.SidingStart);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            TrainpathNode mainNode = ActiveNode;
            mainNode.NodeType = TrainpathNodeType.Other; // this was SidingStart
            mainNode.NextSidingNode = null;
            Trainpath.DetermineIfBroken();

            if (!mainNode.HasSidingPath)
            {   // only a broken stub created by StartPassingPath
                return;
            }
            
            //make sure the main path does no longer show it has a siding path.
            while (mainNode.NodeType != TrainpathNodeType.SidingEnd)
            {
                mainNode.HasSidingPath = false;
                mainNode = mainNode.NextMainNode;
            }

            mainNode.NodeType = TrainpathNodeType.Other; // this was the SidingEnd
        }
    }
    #endregion

    #region RemoveBrokenPoint
    /// <summary>
    /// Subclass to implement the action: Remove a broken point
    /// </summary>
    public class EditorActionRemoveBrokenPoint : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionRemoveBrokenPoint() : base(TrackViewer.catalog.GetString("Remove broken point"), "activeBroken") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return (  ActiveNode.IsBroken
                && (ActiveNode.NodeType != TrainpathNodeType.Start)     // start can be removed separately
                && (ActiveNode.NodeType != TrainpathNodeType.End)       // end can be removed separately
                );
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            //Assumption, there is no siding next to it. but this might not be true. Not clear if the assumption is needed
            if (ActiveNode.NextMainNode != null)
            {
                RemoveIntermediatePoint(ActiveNode);
            }
            else
            {
                ReplaceNodeAndFollowingByNewNode(ActiveNode);
            }
        }
    }
    #endregion

    #region RemoveRestOfPath
    /// <summary>
    /// Subclass to implement the action: Remove the rest of the path
    /// </summary>
    public class EditorActionRemoveRestOfPath : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionRemoveRestOfPath() : base(TrackViewer.catalog.GetString("Remove everything after this point"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return (Trainpath.FirstNodeOfTail != null);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            ActiveNode.NextMainNode = null;
            ActiveNode.NextSidingNode = null;
        }
    }
    #endregion

    #region CutAndStoreTail
    /// <summary>
    /// Subclass to implement the action: Cut the path here and store the tail for later use
    /// </summary>
    public class EditorActionCutAndStoreTail : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionCutAndStoreTail() : base(TrackViewer.catalog.GetString("Cut path here and store its tail"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (Trainpath.FirstNodeOfTail != null) return false;
            if (ActiveNode.NodeType == TrainpathNodeType.Start) return false;
            if (ActiveNode.NextMainNode == null) return false;
            if (ActiveNode.IsBroken) return false;
            if (ActiveNode.NodeType == TrainpathNodeType.SidingEnd) return false; 
            
            if (ActiveNode.HasSidingPath)
            {
                // a siding start itself should still be fine.
                return (ActiveNode.NodeType == TrainpathNodeType.SidingStart);
            }
            return true;

          
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            Trainpath.FirstNodeOfTail = ActiveNode;
            Trainpath.TailHasEnd = Trainpath.HasEnd;
            Trainpath.HasEnd = false; // no end on the initial part of the path

            TrainpathNode lastEditableNode = ActiveNode.PrevNode;
            ActiveNode.PrevNode = null;
            
lastEditableNode.NextMainNode = null;
        }
    }
    #endregion

    #region AutoFix (Common)
    /// <summary>
    /// Subclass to define common methods related to autofixing a path. This contains the routines that actually search for the path.
    /// </summary>
    public abstract class EditorActionAutoFix : EditorAction
    {
        /// <summary>The list/collection of junction indexes that are not allowed during path auto-fixing</summary>
        protected Collection<int> DisAllowedJunctionIndexes { get; private set; }
        private const int maxNumberNodesToCheckForAutoFix = 20; // maximum number of nodes we will try before we conclude not reconnection is possible.

        #region private members to store the path
        List<int> linkingTvns;
        TrainpathNode autoFixStartNode;
        TrainpathNode autoFixReconnectNode;
        bool startNodeNeedsReverse;
        bool reconnectNodeNeedsReverse;
        #endregion

        /// <summary>Constructor</summary>
        protected EditorActionAutoFix(string header, string pngFileName) : base(header, pngFileName) { 
            DisAllowedJunctionIndexes = new Collection<int>();
            linkingTvns = new List<int>();
        }

        /// <summary>
        /// Try to find a connection between the current junction and a reconnect junction.
        /// We do a depth-first search, using the main tracks first.
        /// The result (the path) is stored in a list of linking tvns.
        /// In case there are DisAllowedJunctionIndexes we will not allow the connection to go over these junctions
        /// </summary>
        /// <param name="currentJunctionIndex">Index of the current junction</param>
        /// <param name="currentJunctionIsFacing">true if the current junction is a facing junction</param>
        /// <param name="reconnectJunctionIndex">Index of the junction we need to link to</param>
        /// <param name="reconnectJunctionIsFacing">Is the junction we need to link to a facing junction</param>
        /// <param name="linkingTvns">Current list of linking tvns that we have found</param>
        /// <returns>true if a path was found</returns>
        private bool TryToFindConnection(int currentJunctionIndex, bool currentJunctionIsFacing,
                                 int reconnectJunctionIndex, bool reconnectJunctionIsFacing,
                                 List<int> linkingTvns)
        {
            // Did we succeed?
            if (currentJunctionIndex == reconnectJunctionIndex)
            {   // we found a connection. Now we just need to check if it is in the right direction
                // If it is not in the right direction, we did not succeed.
                return (currentJunctionIsFacing == reconnectJunctionIsFacing);
            }

            // Did we go as deep as we want wanted to go?
            if (linkingTvns.Count == maxNumberNodesToCheckForAutoFix)
            {
                return false;
            }

            // Search further along the next Tvns that we can try.
            TrackNode tn = TrackExtensions.TrackNode(currentJunctionIndex);
            if (tn.TrEndNode)
            {
                return false;
            }

            if (currentJunctionIsFacing)
            {
                return TryToFindConnectionVia(tn.MainTvn(),
                    currentJunctionIndex, reconnectJunctionIndex, reconnectJunctionIsFacing, linkingTvns)
                  ||   TryToFindConnectionVia(tn.SidingTvn(),
                    currentJunctionIndex, reconnectJunctionIndex, reconnectJunctionIsFacing, linkingTvns);
            }
            else
            { 
                return TryToFindConnectionVia(tn.TrailingTvn(),
                    currentJunctionIndex, reconnectJunctionIndex, reconnectJunctionIsFacing, linkingTvns);
            }
        }

        /// <summary>
        /// Try to find a connection between the current junction and a reconnect junction, along the given TVN
        /// We do a depth-first search, using the main tracks first.
        /// The result (the path) is stored in a list of linking tvns. 
        /// </summary>
        /// <param name="nextTvn">The TVN (Track Vector Node index) that we will take.</param>
        /// <param name="currentJunctionIndex">Index of the current junction</param>
        /// <param name="reconnectJunctionIndex">Index of the junction we need to link to</param>
        /// <param name="reconnectJunctionIsFacing">Is the junction we need to link to a facing junction</param>
        /// <param name="linkingTvns">Current list of linking tvns that we have found</param>
        /// <returns>true if a path was found</returns>
        private bool TryToFindConnectionVia(int nextTvn,
            int currentJunctionIndex, int reconnectJunctionIndex, bool reconnectJunctionIsFacing, List<int> linkingTvns)
        {
            if (nextTvn <= 0) return false; // something wrong in train database.

            int nextJunctionIndex = TrackExtensions.GetNextJunctionIndex(currentJunctionIndex, nextTvn);
            if (DisAllowedJunctionIndexes.Contains(nextJunctionIndex)) {
                return false;
            }
            bool nextJunctionIsFacing = (nextTvn == TrackExtensions.TrackNode(nextJunctionIndex).TrailingTvn());

            linkingTvns.Add(nextTvn);
            bool succeeded = TryToFindConnection(nextJunctionIndex, nextJunctionIsFacing, 
                                                 reconnectJunctionIndex, reconnectJunctionIsFacing,
                                                 linkingTvns);
            if (!succeeded)
            {   //Pop the index that did not work
                linkingTvns.RemoveAt(linkingTvns.Count - 1); 
            }

            return succeeded;
        }

        /// <summary>
        /// Try to find a connection. Depth-first search via main track at junctions.
        /// Also reversing the start or reconnectNode is tried, in case one of these nodes has a non-defined orientation 
        /// because both before and after the node the path is broken.
        /// </summary>
        /// <param name="startNode">Node at which the reconnection should start</param>
        /// <param name="reconnectNode">Node at which the reconnection should end</param>
        /// <returns>True if a connection has been found</returns>
        protected bool FindConnection(TrainpathNode startNode, TrainpathNode reconnectNode)
        {
            // We try to find a connection between two non-broken nodes.
            // We store the connection as a stack of linking tvns (track-node-vector-indexes)
            // The connection will only contain junctions (apart from maybe start and end nodes)
            // We will not consider connections without a single junction in between

            // This will store the path, so we can actually create it later on.
            autoFixStartNode = startNode;
            autoFixReconnectNode = reconnectNode;
            startNodeNeedsReverse = false;
            reconnectNodeNeedsReverse = false;
            
            if (FindConnectionSameTrack()) {
                return true;
            }

            //first try to see if we succeed without re-orienting the startNode or reconnectNode
            if (FindConnectionThisOrientation())
            {
                return true;
            }

            //perhaps there is a path with a reversed start node.
            if (CanReverse(startNode))
            {
                startNode.ReverseOrientation();
                startNodeNeedsReverse = FindConnectionThisOrientation();
                startNode.ReverseOrientation(); // we only do the actual reverse if the user chooses to fix
                if (startNodeNeedsReverse) {
                    return true;
                }
            }

            //perhaps there is a path with a reversed reconnect node.
            if (CanReverse(reconnectNode))
            {
                reconnectNode.ReverseOrientation();
                reconnectNodeNeedsReverse = FindConnectionThisOrientation();
                reconnectNode.ReverseOrientation(); // we only do the actual reverse if the user chooses to fix
                if (reconnectNodeNeedsReverse) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to find a connection. Depth-first search via main track at junctions.
        /// No reversing of the nodes will be allowed. 
        /// AutoFixStartNode and AutoFixReconnectNode are assumed to be defined already
        /// </summary>
        /// <returns>True if a connection has been found</returns>
        private bool FindConnectionThisOrientation()
        {
            linkingTvns.Clear();

            int firstJunctionIndex;
            int lastJunctionIndex;
            bool firstJunctionIsFacing;
            bool lastJunctionIsFacing;
            
            // determine the index of the first junction where we start our search
            TrainpathVectorNode firstNodeAsVector = autoFixStartNode as TrainpathVectorNode;
            TrainpathJunctionNode firstNodeAsJunction = autoFixStartNode as TrainpathJunctionNode;
            if (firstNodeAsVector != null)
            {
                firstJunctionIndex = firstNodeAsVector.GetNextJunctionIndex(firstNodeAsVector.TvnIndex);
                linkingTvns.Add(firstNodeAsVector.TvnIndex);
                firstJunctionIsFacing = (firstNodeAsVector.TvnIndex == TrackExtensions.TrackNode(firstJunctionIndex).TrailingTvn());
            }
            else
            {
                firstJunctionIndex = firstNodeAsJunction.JunctionIndex;
                firstJunctionIsFacing = firstNodeAsJunction.IsFacingPoint;
            }

            //now determine the index of the last junction node where we should end our search
            TrainpathVectorNode lastNodeAsVector = autoFixReconnectNode as TrainpathVectorNode;
            TrainpathJunctionNode lastNodeAsJunction = autoFixReconnectNode as TrainpathJunctionNode;
            if (lastNodeAsVector != null)
            {
                lastJunctionIndex = lastNodeAsVector.GetPrevJunctionIndex(lastNodeAsVector.TvnIndex);
                lastJunctionIsFacing = (lastNodeAsVector.TvnIndex != TrackExtensions.TrackNode(lastJunctionIndex).TrailingTvn());  // this node is after the junction!  
            }
            else
            {
                lastJunctionIndex = lastNodeAsJunction.JunctionIndex;
                lastJunctionIsFacing = lastNodeAsJunction.IsFacingPoint;
            }

            //search for a path
            bool foundConnection = TryToFindConnection(firstJunctionIndex, firstJunctionIsFacing,
                                                       lastJunctionIndex, lastJunctionIsFacing,
                                                       linkingTvns);
            return foundConnection;
        }

        /// <summary>
        /// Find whether it is possible to connect directly two points on the same track
        /// </summary>
        /// <returns>true if such a direct path has been found</returns>
        private bool FindConnectionSameTrack()
        {
            TrainpathVectorNode startAsVector     = autoFixStartNode as TrainpathVectorNode;
            TrainpathVectorNode reconnectAsVector = autoFixReconnectNode as TrainpathVectorNode;

            if (startAsVector == null) return false;
            if (reconnectAsVector == null) return false;
            if (startAsVector.TvnIndex != reconnectAsVector.TvnIndex) return false;

            //for a reverse point the orientation is defined as being after the reversal.
            bool reconnectIsForward = (reconnectAsVector.NodeType == TrainpathNodeType.Reverse)
                ? !reconnectAsVector.ForwardOriented
                :  reconnectAsVector.ForwardOriented;
            if (startAsVector.ForwardOriented != reconnectIsForward) return false;
            if (startAsVector.ForwardOriented != startAsVector.IsEarlierOnTrackThan(reconnectAsVector)) return false;

            linkingTvns.Clear();
            return true;
        }

        /// <summary>
        /// A node can be reversed it both the leading and the trailing path are broken.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static bool CanReverse(TrainpathNode node)
        {
            bool incomingAllowsReversal = true;
            bool outgoingAllowsReversal;

            if (node.NextSidingNode != null)
            {   // if there is a siding node, this is a siding start (probably) and things become too complex
                outgoingAllowsReversal = false;
            }
            else
            {
                if (node.NextMainNode == null)
                {   // no next main node, so we are fine with reversing
                    outgoingAllowsReversal = true; 
                }
                else
                {
                    outgoingAllowsReversal = node.NextMainNode.IsBroken || (node.NextMainTvnIndex == -1);
                }
            }

            if (node.PrevNode == null)
            {
                incomingAllowsReversal = true;
            }
            else
            {
                incomingAllowsReversal = node.PrevNode.IsBroken || (node.PrevNode.NextMainTvnIndex == -1);
            }

            return incomingAllowsReversal && outgoingAllowsReversal;
        }

        /// <summary>
        /// Actually create the path by linking nodes following the stored linking tvns.
        /// </summary>
        /// <param name="isMainPath">Do we add the node to the main path or not</param>
        protected void CreateFoundConnection(bool isMainPath)
        {
            if (    startNodeNeedsReverse) autoFixStartNode.ReverseOrientation();
            if (reconnectNodeNeedsReverse) autoFixReconnectNode.ReverseOrientation();

            //create the new path using the stored Tvns
            TrainpathNode currentNode = autoFixStartNode;
            foreach (int tvn in linkingTvns)
            {
                currentNode = AddAdditionalNode(currentNode, tvn, isMainPath);
                while (currentNode is TrainpathVectorNode)
                {   // apparently a disambiguity node has been added.
                    currentNode = AddAdditionalNode(currentNode, tvn, isMainPath);
                }
            }

            //make the final connections
            TrainpathVectorNode reconnectNodeAsVector = autoFixReconnectNode as TrainpathVectorNode;
            if (reconnectNodeAsVector != null)
            {   // we only need to make the connection between the final junction node and the vectornode
                if (isMainPath)
                {
                    currentNode.NextMainNode = reconnectNodeAsVector;
                    currentNode.NextMainTvnIndex = reconnectNodeAsVector.TvnIndex;
                    reconnectNodeAsVector.PrevNode = currentNode;
                }
                else
                {
                    currentNode.NextSidingNode = reconnectNodeAsVector;
                    currentNode.NextSidingTvnIndex = reconnectNodeAsVector.TvnIndex;
                }
            }
            else
            {   // the last node we added is the same junction as the last node we want to keep
                // so make the needed corrections, and forget about the currentNode afterwards.
                if (isMainPath)
                {
                    currentNode.PrevNode.NextMainNode = autoFixReconnectNode;
                    autoFixReconnectNode.PrevNode = currentNode.PrevNode;
                }
                else
                {
                    currentNode.PrevNode.NextSidingNode = autoFixReconnectNode;
                }
                NetNodesAdded--;
            }
        }   
    }
    #endregion

    #region AutoFixBrokenNodes
    /// <summary>
    /// Subclass to implement the action: Auto-fix broken nodes
    /// </summary>
    public class EditorActionAutoFixBrokenNodes : EditorActionAutoFix
    {
        /// <summary>The node from which we will start the autofix procedure</summary>
        TrainpathNode autoFixStartNode;
        /// <summary>The node to which we need to reconnect during autofix</summary>
        TrainpathNode autoFixReconnectNode;
        /// <summary>The amount of nodes that will be removed if we succeed</summary>
        int numberOfNodeThatWillBeRemoved;

        /// <summary>Constructor</summary>
        public EditorActionAutoFixBrokenNodes()
            : base(TrackViewer.catalog.GetString("Auto-fix broken nodes"), "activeBroken") {}

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            numberOfNodeThatWillBeRemoved = 0; // starting value

            if (ActiveNode.IsBroken)
            {   //this node is broken. But we must make sure there are non-broken nodes either before or after.
                numberOfNodeThatWillBeRemoved++; // this node will also be removed
                FindNonBrokenSuccessor();
                FindNonBrokenPredecessor();
                if ((autoFixStartNode == null) || (autoFixReconnectNode == null)) return false;
                return FindConnection(autoFixStartNode, autoFixReconnectNode);
            }

            if (ActiveNode.NextMainNode != null && ActiveNode.NextMainTvnIndex == -1)
            {   // the next link exists but is broken
                autoFixStartNode = ActiveNode;
                FindNonBrokenSuccessor();
                if (autoFixReconnectNode == null) return false;
                return FindConnection(autoFixStartNode, autoFixReconnectNode);
            }

            TrainpathNode prevNode = ActiveNode.PrevNode;
            if (prevNode == null) return false;
            if (prevNode.NextSidingNode != null) return false;
            if (prevNode.NextMainTvnIndex == -1)
            {   // the previous link exists but is broken
                autoFixReconnectNode = ActiveNode;
                FindNonBrokenPredecessor();
                if (autoFixStartNode == null) return false;
                return FindConnection(autoFixStartNode, autoFixReconnectNode);
            }

            // Nothing to fix at the activeNode
            return false;
        }

        /// <summary>
        /// Find (and store) a non-broken node that predecesses the activeNode.
        /// Also count the number of intermediate broken nodes that will be removed if we succeed in autofixing the path.
        /// </summary>
        void FindNonBrokenPredecessor()
        {
            autoFixStartNode = null;
            TrainpathNode predecessor = ActiveNode.PrevNode;
            while (predecessor != null)
            {
                if (!predecessor.IsBroken)
                {
                    autoFixStartNode = predecessor;
                    return;
                }
                predecessor = predecessor.PrevNode;
                numberOfNodeThatWillBeRemoved++;
            }
        }

        /// <summary>
        /// Find (and store) a non-broken node that comes after the activeNode.
        /// Also count the number of intermediate broken nodes that will be removed if we succeed in autofixing the path.
        /// </summary>
        void FindNonBrokenSuccessor()
        {
            autoFixReconnectNode = null;
            TrainpathNode successor = ActiveNode.NextMainNode;
            while (successor != null)
            {
                if (!successor.IsBroken)
                {
                    autoFixReconnectNode = successor;
                    return;
                }
                successor = successor.NextMainNode;
                numberOfNodeThatWillBeRemoved++;
            }
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            //bool foundConnection = FindConnection(autoFixStartNode, autoFixReconnectNode);
            //if (!foundConnection)
            //{
            //    MessageBox.Show(TrackViewer.catalog.GetString(
            //        "Auto fixing broken nodes did not succeed. Sorry.\n" +
            //        "Perhaps you can try with cutting, storing the tail and then reconnecting."
            //        ));
            //    return;
            //}
            CreateFoundConnection(true);

            //some nodes are discarded by making the new links:
            NetNodesAdded -= numberOfNodeThatWillBeRemoved;
            Trainpath.DetermineIfBroken(); // Possibly we removed the last broken nodes.
        }       
    }
    #endregion

    #region AutoConnectToTail
    /// <summary>
    /// Subclass to implement the action: Auto-connect to the stored tail
    /// </summary>
    public class EditorActionAutoConnectTail : EditorActionAutoFix
    {
        /// <summary>Constructor</summary>
        public EditorActionAutoConnectTail() : base(TrackViewer.catalog.GetString("Reconnect to tail"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (Trainpath.FirstNodeOfTail == null) return false;
            if (ActiveNode.HasSidingPath) return false;
            if (ActiveNode.IsBroken) return false;
            return FindConnection(ActiveNode, Trainpath.FirstNodeOfTail);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            //bool foundConnection = FindConnection(ActiveNode, Trainpath.FirstNodeOfTail);
            //if (!foundConnection)
            //{
            //    MessageBox.Show(TrackViewer.catalog.GetString("Connecting to the tail did not succeed. Sorry"));
            //    return;
            //}

            CreateFoundConnection(true);

            Trainpath.FirstNodeOfTail = null;
            Trainpath.HasEnd = Trainpath.TailHasEnd;
            Trainpath.DetermineIfBroken(); // Possibly we removed the last broken nodes.
            NetNodesAdded = CountSuccessors(ActiveNode); // just draw everything
        }

        /// <summary>
        /// Count the amount of nodes in the path after the node.
        /// </summary>
        /// <param name="node">Node to start with (will not be counted)</param>
        /// <returns>Amount of (main) nodes counted</returns>
        static int CountSuccessors(TrainpathNode node)
        {
            int result = 0;
            TrainpathNode currentNode = node.NextMainNode;
            while (currentNode != null)
            {
                result++;
                currentNode = currentNode.NextMainNode;
            }
            return result;
        }
    }
    #endregion

    #region ReconnectPassingPath
    /// <summary>
    /// Subclass to implement the action: Auto-connect the started passing path (currently only a stub) to here.
    /// </summary>
    public class EditorActionReconnectPassingPath : EditorActionAutoFix
    {
        TrainpathNode sidingStartNode;
 
        /// <summary>Constructor</summary>
        public EditorActionReconnectPassingPath() : base(TrackViewer.catalog.GetString("Reconnect passing path"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (ActiveNode.HasSidingPath) return false;
            if (ActiveNode.IsBroken) return false;

            findPassingPathStub();
            if (sidingStartNode == null) return false;
            if (sidingStartNode.NextSidingNode == ActiveNode) return false;  // this happens when we are at the single siding node we just started
            return FindConnection(sidingStartNode.NextSidingNode, ActiveNode);
        }

        /// <summary>
        /// This action can only be done if there is a passing path stub, that is, a start of a passing path
        /// that contains only a single siding node that is broken and not reconnected.
        /// </summary>
        void findPassingPathStub() {
            sidingStartNode = null;
            DisAllowedJunctionIndexes.Clear(); // to prevent the reconnection to go over the main path

            TrainpathNode mainNode = ActiveNode.PrevNode;
            //follow the train path backward and see what we find
            while (mainNode != null)
            {
                TrainpathJunctionNode mainNodeAsJunction = mainNode as TrainpathJunctionNode;
                    
                if (mainNodeAsJunction != null)
                {
                    if (mainNode.NodeType == TrainpathNodeType.SidingStart)
                    {   // we found the sidingstart.
                        break;
                    }
                    DisAllowedJunctionIndexes.Add(mainNodeAsJunction.JunctionIndex);
                    if (mainNode.NodeType == TrainpathNodeType.SidingEnd)
                    {   // if a new siding path is started (going back), stop searching
                        return;
                    }
                }
                else
                {
                    if (mainNode.NodeType != TrainpathNodeType.Other)
                    {   // if it is not an other-node (so not a disambiguity node), stop searching
                        return;
                    }
                }
                mainNode = mainNode.PrevNode;
            }

            if (mainNode == null) return;

            // we are at a siding start. It should be the correct one.
            if (mainNode.HasSidingPath) return; // just an extra check. Our stub does not set HasSidingpath
            sidingStartNode = mainNode;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            CreateFoundConnection(false);

            TrainpathNode sidingEnd = ActiveNode;

            sidingStartNode.NextSidingNode.SetNonBroken();
            
            sidingStartNode.NodeType = TrainpathNodeType.SidingStart;
            sidingEnd.NodeType = TrainpathNodeType.SidingEnd;

            TrainpathNode mainNode = sidingStartNode;
            while (mainNode.NodeType != TrainpathNodeType.SidingEnd)
            {
                mainNode.HasSidingPath = true;
                mainNode = mainNode.NextMainNode;
            }

            Trainpath.DetermineIfBroken();
            NetNodesAdded = 0; // no main nodes added.
        }

    }
    #endregion

    #region TakeOtherExitPassingPath
    /// <summary>
    /// Subclass to implement the action: In a passing path, take the other exit reconnecting to the same siding-end
    /// </summary>
    public class EditorActionTakeOtherExitPassingPath : EditorActionAutoFix
    {
        TrainpathNode sidingEndNode;

        /// <summary>Constructor</summary>
        public EditorActionTakeOtherExitPassingPath() : base(TrackViewer.catalog.GetString("Take other exit"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (ActiveNode.IsBroken) return false; 
            if (ActiveNode.NextMainNode != null) return false;
            if (ActiveNode.NextSidingNode == null) return false;
            if (ActiveNode.NextSidingNode.NodeType == TrainpathNodeType.SidingEnd) return false; // not enough room to take other exit.

            FindSidingEnd();
            if (sidingEndNode == null) return false;

            if (!FindDisAllowedJunctionIndexes()) return false;
            return FindConnection(ActiveNode,sidingEndNode);
        }

        /// <summary>
        /// This action can only be done if there is a passing path stub, that is, a start of a passing path
        /// that contains only a single siding node that is broken and not reconnected.
        /// </summary>
        void FindSidingEnd()
        {
            TrainpathNode sidingNode = ActiveNode;
            while (sidingNode != null)
            {
                if (sidingNode.NodeType == TrainpathNodeType.SidingEnd)
                {
                    sidingEndNode = sidingNode;
                    return;
                }
                sidingNode = sidingNode.NextSidingNode;
            }
            sidingEndNode = null;
        }

        /// <summary>
        /// Find the junction indexes that cannot be used. This is both the junction indexes of the junctions on the main path
        /// as well as the single junction index on the current siding path that we cannot take because we want the other exit.
        /// </summary>
        /// <returns>false if there was something really wrong</returns>
        bool FindDisAllowedJunctionIndexes()
        {
            DisAllowedJunctionIndexes.Clear();  // to prevent the reconnection to go over the main path

            DisAllowedJunctionIndexes.Add(TrackExtensions.GetNextJunctionIndex(
                (ActiveNode as TrainpathJunctionNode).JunctionIndex, ActiveNode.NextSidingTvnIndex));

            TrainpathNode mainNode = sidingEndNode.PrevNode;
            //follow the train path backward and see what we find
            while (mainNode != null)
            {
                TrainpathJunctionNode mainNodeAsJunction = mainNode as TrainpathJunctionNode;

                if (mainNodeAsJunction != null)
                {
                    if (mainNode.NodeType == TrainpathNodeType.SidingStart)
                    {   // we found the sidingstart. So we are done
                        return true;
                    }
                    DisAllowedJunctionIndexes.Add(mainNodeAsJunction.JunctionIndex);
                    if (mainNode.NodeType == TrainpathNodeType.SidingEnd)
                    {   // if a new siding path is started (going back), stop searching
                        // in this case the path is really really broken (because we started searching on a siding path)
                        return false;
                    }
                }
                else
                {
                    if (mainNode.NodeType != TrainpathNodeType.Other)
                    {   // if it is not an other-node (so not a disambiguity node), stop searching
                        // in this case the path is really really broken (because we started searching on a siding path)
                        return false;
                    }
                }
                mainNode = mainNode.PrevNode;
            }

            // in this case the path is really really broken (because we started searching on a siding path)
            return false;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            CreateFoundConnection(false);
            Trainpath.DetermineIfBroken();

            NetNodesAdded = 0; // no main nodes added.
        }

    }
    #endregion

    #region MouseDrag
    /// <summary>
    /// Subclass to implement the actions related to mouse dragging.
    /// </summary>
    public class EditorActionMouseDrag : EditorAction
    {
        TrainpathNode dragLimitNode1, dragLimitNode2; // the dragging node must be between these two limits
        TrainpathVectorNode nodeBeingDragged;               // link to the node (new) that is being dragged.

        /// <summary>Constructor</summary>
        public EditorActionMouseDrag() : base("", "") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (ActiveNode.IsBroken) return false;
            nodeBeingDragged = ActiveNode as TrainpathVectorNode;
            if (nodeBeingDragged == null) return false;
            
            switch (ActiveNode.NodeType)
            {
                case TrainpathNodeType.Start:
                case TrainpathNodeType.End:
                case TrainpathNodeType.Stop:
                case TrainpathNodeType.Reverse:
                    return true;
                default: 
                    return false;
            }
            
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {   //not implemented because mouse dragging has its own methods
        }

        /// <summary>
        /// Dragging has commenced. Perform initialization actions
        /// </summary>
        public void StartDragging()
        {
            Trainpath.StoreCurrentPath();
            
            switch (ActiveNode.NodeType)
            {
                case TrainpathNodeType.Start:   FindDraggingLimitsStart(); break;
                case TrainpathNodeType.End:     FindDraggingLimitsEnd(); break;
                case TrainpathNodeType.Stop:    FindDraggingLimitsStop(); break;
                case TrainpathNodeType.Reverse: FindDraggingLimitsReverse(); break;
                default: break;
            }

            Dragging();
        }

        /// <summary>
        /// Update the location of the node that is being dragged.
        /// </summary>
        public void Dragging()
        {

            if ( (nodeBeingDragged.TvnIndex == ActiveTrackLocation.TvnIndex)
              && (ActiveTrackLocation.Location != null)
              && (ActiveTrackLocation.IsBetween(dragLimitNode1, dragLimitNode2)))
            {
                nodeBeingDragged.Location                = ActiveTrackLocation.Location;
                nodeBeingDragged.TrackVectorSectionIndex = ActiveTrackLocation.TrackVectorSectionIndex;
                nodeBeingDragged.TrackSectionOffset      = ActiveTrackLocation.TrackSectionOffset;
            
                Trainpath.UseLast();
            }
            else
            {
                Trainpath.UseLastButOne();
            }
        }

        /// <summary>
        /// End the dragging and make sure the modified path will be used subsequently, as long as mouse is still in a good position. Otherwise cancel.
        /// </summary>
        public void EndDragging()
        {
            Dragging(); //make sure we do the latest update!
        }

        /// <summary>
        /// End the dragging and but do not use the modified path (but use the previouse version instead)
        /// </summary>
        public void CancelDragging()
        {
            Trainpath.UseLastButOne();
        }

        void FindDraggingLimitsStart()
        {
            dragLimitNode1 = LimitingJunctionNode(false);
            if (nodeBeingDragged.NextMainNode == null)
            {
                dragLimitNode2 = LimitingJunctionNode(true);
            }
            else
            {
                dragLimitNode2 = nodeBeingDragged.NextMainNode;
            }
        }
        
        void FindDraggingLimitsEnd()
        {
            dragLimitNode1 = nodeBeingDragged.PrevNode;
            dragLimitNode2 = LimitingJunctionNode(true);
        }

        void FindDraggingLimitsReverse()
        {
            if (nodeBeingDragged.NextMainNode == null)
            {
                dragLimitNode1 = nodeBeingDragged.PrevNode;
            }
            else
            {   // we now need to find which of the next and previous nodes is closest to the reverse point
                TrainpathVectorNode prevVectorNode = nodeBeingDragged.PrevNode as TrainpathVectorNode;
                TrainpathVectorNode nextVectorNode = nodeBeingDragged.NextMainNode as TrainpathVectorNode;
                    
                if (prevVectorNode == null)
                {   // prev is a junction node. Then either the next node is a vector node, or it is the same junction. Just take it
                    dragLimitNode1 = nodeBeingDragged.NextMainNode;
                }
                else if (nextVectorNode == null)
                {   // next is a junction node. prev is a vector node. Take that one
                    dragLimitNode1 = nodeBeingDragged.PrevNode;
                }
                else
                {   // both are vector nodes.
                    if (prevVectorNode.IsBetween(nextVectorNode,nodeBeingDragged)) {
                        dragLimitNode1 = nodeBeingDragged.PrevNode;
                    }
                    else
                    {
                        dragLimitNode1 = nodeBeingDragged.NextMainNode;
                    }
                }
            }
            dragLimitNode2 = LimitingJunctionNode(false); // direction of reverse is after the reverse. 
        }

        void FindDraggingLimitsStop()
        {
            dragLimitNode1 = nodeBeingDragged.PrevNode;
            if (nodeBeingDragged.NextMainNode == null)
            {
                dragLimitNode2 = LimitingJunctionNode(true);
            }
            else
            {
                dragLimitNode2 = nodeBeingDragged.NextMainNode;
            }
        }

        TrainpathJunctionNode LimitingJunctionNode(bool afterNode)
        {
            TrainpathJunctionNode newJunctionNode = new TrainpathJunctionNode(nodeBeingDragged);

            if (afterNode)
            {
                newJunctionNode.JunctionIndex = nodeBeingDragged.GetNextJunctionIndex(nodeBeingDragged.TvnIndex);
            }
            else
            {
                newJunctionNode.JunctionIndex = nodeBeingDragged.GetPrevJunctionIndex(nodeBeingDragged.TvnIndex);
            }
            return newJunctionNode;
        }
    }
    #endregion
  
    #region NonInteractiveActions
    /// <summary>
    /// Subclass to implement the 'action': add Missing ambiguity nodes. This is not an interactive action, but still and edit to the path.
    /// </summary>
    public class EditorActionNonInteractive : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionNonInteractive() : base(TrackViewer.catalog.GetString(""), "") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            return true;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction() {}

        /// <summary>
        /// Add an additional main node (to the end of the current path)
        /// </summary>
        /// <param name="currentNode">Node after which to add a node</param>
        /// <param name="callback">Callback to call when node has been added</param>
        public void AddMainNode(TrainpathNode currentNode, AfterEditDelegate callback)
        {
            if (currentNode.IsBroken) return;
            NetNodesAdded = 0;
            AddAdditionalNode(currentNode, true);
            callback(NetNodesAdded);
        }

        /// <summary>
        /// Upon starting editing a path, make sure everywhere where needed disambibuity nodes are added (which is
        /// not always the case when loaded from file)
        /// </summary>
        /// <param name="trainpath">The trainpath for which the disambiguity nodes need to be added</param>
        /// <param name="callback">Callback to call when node has been added</param>
        public void AddMissingDisambiguityNodes(Trainpath trainpath, AfterEditDelegate callback)
        {
            NetNodesAdded = 0;

            TrainpathNode mainNode = trainpath.FirstNode;
            if (mainNode == null) return;

            while (mainNode.NextMainNode != null)
            {
                TrainpathJunctionNode mainNodeAsJunction = mainNode as TrainpathJunctionNode;
                if ((mainNodeAsJunction != null) && mainNodeAsJunction.IsSimpleSidingStart())
                {   // this is a simple siding start and therefore needs a disambiguitynode.

                    if (mainNode.NextMainNode is TrainpathJunctionNode)
                    {   // the next node is a junction, which is not good enough
                        TrainpathVectorNode halfwayNode = CreateHalfWayNode(mainNodeAsJunction, mainNode.NextMainTvnIndex);
                        halfwayNode.PrevNode = mainNode;
                        AddIntermediateNode(halfwayNode);
                        trainpath.IsModified = true;
                    }

                }
                mainNode = mainNode.NextMainNode;
            }
             
            callback(NetNodesAdded);
        }

    }
    #endregion

    #region DrawUntilHere
    /// <summary>
    /// Subclass to implement the action: Draw until the currently selected node
    /// </summary>
    public class EditorActionDrawUntilHere : EditorAction
    {
        AfterEditDelegate callback;

        /// <summary>Constructor</summary>
        public EditorActionDrawUntilHere(AfterEditDelegate callback)
            : base(TrackViewer.catalog.GetString("Draw path until here"), "activeNormal")
        {
            this.callback = callback;
        }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return true;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            // first backup till we are on main track.
            int nodesOnSidingPath = 0;
            TrainpathNode node = ActiveNode;
            while (node.NextMainNode == null)
            {
                node = node.PrevNode;
                nodesOnSidingPath++;
            }

            int newNumberToDraw = nodesOnSidingPath + Trainpath.GetNodeNumber(node);
            callback(newNumberToDraw);
        }
    }
    #endregion

    #region DrawToNextBrokenPoint
    /// <summary>
    /// Subclass to implement the action: Find the next broken point and draw till it.
    /// </summary>
    public class EditorActionDrawToNextBrokenPoint : EditorAction
    {
        AfterEditDelegate callback;

        /// <summary>Constructor</summary>
        public EditorActionDrawToNextBrokenPoint(AfterEditDelegate callback)
            : base(TrackViewer.catalog.GetString("Draw path until next broken point"), "activeBroken")
        {
            this.callback = callback;
        }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return Trainpath.IsBroken;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            int NumberOfActiveNode = Trainpath.GetNodeNumber(ActiveNode);
            Collection<int> brokenNodeNumbers = Trainpath.DetermineIfBroken();
            var brokenNodeNumbersAfterActive = (from i in brokenNodeNumbers where i > NumberOfActiveNode select i);
            int newNumberToDraw;
            if (brokenNodeNumbersAfterActive.Count() > 0)
            {   // take the first node after this node
                newNumberToDraw = brokenNodeNumbersAfterActive.Min();
            }
            else
            {   // No nodes after this, take the first broken node
                newNumberToDraw = brokenNodeNumbers.Min();
            }

            callback(newNumberToDraw);
        }
    }
    #endregion
}
