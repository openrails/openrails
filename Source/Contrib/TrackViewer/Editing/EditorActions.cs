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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Orts.Formats.Msts;
using ORTS.Common;
using ORTS.TrackViewer.Drawing;

namespace ORTS.TrackViewer.Editing
{
    /// <summary>Delegate to enable giving information on the edited path back to the editor</summary>
    public delegate void AfterEditDelegate(int nodesAdded);

    #region EditorAction (base)
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

        /// <summary>x-location of the mouse</summary>
        protected int MouseX { get; set; }
        /// <summary>y-location of the mouse</summary>
        protected int MouseY { get; set; }

        /// <summary>The tools (strategy) to do modifications to the path</summary>
        protected ModificationTools ModificationTools { get; set; }

        private AfterEditDelegate afterEditCallback;
        #endregion

        /// <summary>
        /// Function that gives the amoung of nodes that will be added when extending the path significantly
        /// </summary>
        /// <returns>Depending on a preference, either 0 or a lot</returns>
        public static int NodesToAddForLongExtend() { return Properties.Settings.Default.pgupExtendsPath ? 100 : 0; }

        /// <summary>
        /// Constructor. Creates the menuitem to be used in the context menu.
        /// </summary>
        /// <param name="menuHeader">The 'header' to be used in the contect menu</param>
        /// <param name="pngFileName">The name of the png icon to use</param>
        protected EditorAction(string menuHeader, string pngFileName)
        {
            ActionMenuItem = new MenuItem
            {
                Header = menuHeader,
                IsCheckable = false
            };
            ActionMenuItem.Click += new RoutedEventHandler(ContextExecuteAction_Click);

            if (!string.IsNullOrEmpty(pngFileName))
            {
                ActionMenuItem.Icon = new System.Windows.Controls.Image
                {
                    Source = BitmapImageManager.Instance.GetImage(pngFileName),
                    Width = 14,
                    Height = 14,
                };
            }

            ModificationTools = new ModificationTools();
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
        private void ContextExecuteAction_Click(object sender, RoutedEventArgs e)
        {
            DoAction();
        }

        /// <summary>
        /// Perform the action, taking care of saving the path for undo and calling the callback at the end
        /// </summary>
        public void DoAction()
        {
            Trainpath.StoreCurrentPath();
            ModificationTools.Reset();
            ExecuteAction();
            Trainpath.DetermineIfBroken(); // instead of keeping track of when to do this or not, just do it always.
            UpdateNodeCount();
        }

        /// <summary>
        /// Update the caller with the (net) amount of nodes added
        /// </summary>
        protected void UpdateNodeCount()
        {
            afterEditCallback?.Invoke(NetMainNodesAdded());
            ModificationTools.Reset();

        }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected abstract void ExecuteAction();
        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected abstract bool CanExecuteAction();
        /// <summary>Returns the net amount of main nodes added.</summary>
        /// <remarks> Has a default implementation but can be overriden</remarks>
        protected virtual int NetMainNodesAdded()
        {
            return ModificationTools.NetNodesAdded;
        }

        /// <summary>
        /// The string representation is the same as the header which will end up in the action menu.
        /// </summary>
        public override string ToString()
        {
            return this.ActionMenuItem.Header.ToString();
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
            Trainpath.FirstNode = new TrainpathVectorNode(ActiveTrackLocation)
            {
                NodeType = TrainpathNodeType.Start
            };
            int maxNodesToAdd = 1 + EditorAction.NodesToAddForLongExtend();
            ModificationTools.AddAdditionalMainNodes(Trainpath.FirstNode, maxNodesToAdd); // make sure also the second and possible additional nodes are available and drawn.
        }

        /// <summary>Returns the net amount of main nodes added.</summary>
        protected override int NetMainNodesAdded()
        {
            return ModificationTools.NetNodesAdded + 1; // first node is not counted automatically
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
            if (Trainpath.FirstNodeOfTail != null) return false; // because then remove start (keep tail) is possible
            return (ActiveNode == Trainpath.FirstNode);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            Trainpath.FirstNode = null;
        }
    }
    #endregion

    #region RemoveStartKeepTail
    /// <summary>
    /// Subclass to implement the action: Remove Start point while keeping the tail
    /// This action removes start node and subsequently the complete path (apart from the tail). (only path metadata will be unchanged!)
    /// </summary>
    public class EditorActionRemoveStartKeepTail : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionRemoveStartKeepTail() : base(TrackViewer.catalog.GetString("Clear path (keep tail)"), null) { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return (Trainpath.FirstNodeOfTail != null);
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
            int maxNodesToAdd = 1 + EditorAction.NodesToAddForLongExtend();
            ModificationTools.AddAdditionalMainNodes(Trainpath.FirstNode, maxNodesToAdd); // make sure also the second and possible additional nodes are available and drawn.
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
            return (ActiveTrackLocation.Location != WorldLocation.None
                 && ActiveTrackLocation.PrevNode != null
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

            TrainpathVectorNode newNode = ModificationTools.AddAdditionalVectorNode(lastNode, ActiveTrackLocation, true);
            newNode.NodeType = TrainpathNodeType.End;
            ModificationTools.CleanAmbiguityNodes(newNode);
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
            ModificationTools.ReplaceNodeAndFollowingByNewNode(ActiveNode);
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
            return (ActiveTrackLocation.Location != WorldLocation.None
                && ActiveTrackLocation.PrevNode != null
                && !ActiveTrackLocation.PrevNode.HasSidingPath);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            TrainpathNode lastNode = ActiveTrackLocation.PrevNode;

            TrainpathVectorNode newNode = ModificationTools.AddAdditionalVectorNode(lastNode, ActiveTrackLocation, true);
            newNode.NodeType = TrainpathNodeType.Reverse;
            lastNode.NextMainNode = newNode;
            lastNode.NextSidingNode = null; // should not be needed
            lastNode.NextMainTvnIndex = newNode.TvnIndex;
            newNode.PrevNode = lastNode;

            newNode.ReverseOrientation(); // reverse because, well, this is a reverse point.
            ModificationTools.CleanAmbiguityNodes(newNode);
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
            ModificationTools.ReplaceNodeAndFollowingByNewNode(ActiveNode);
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
            if (nodeToEdit.WaitTimeS == 0)
            {
                nodeToEdit.WaitTimeS = 602; // some initial value: 10 minutes, 2 seconds
            }
            WaitPointDialog waitDialog = new WaitPointDialog(MouseX, MouseY, nodeToEdit.WaitTimeS);
            TrackViewer.Localize(waitDialog);
            if (waitDialog.ShowDialog() == true)
            {
                nodeToEdit.WaitTimeS = waitDialog.GetWaitTime();
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
            return (ActiveTrackLocation.Location != WorldLocation.None
                && ActiveTrackLocation.PrevNode != null
                && !ActiveTrackLocation.PrevNode.HasSidingPath);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            TrainpathVectorNode newNode = ModificationTools.AddIntermediateMainNode(ActiveTrackLocation);
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
                ModificationTools.RemoveIntermediatePoint(ActiveNode);
            }
            else
            {
                ModificationTools.ReplaceNodeAndFollowingByNewNode(ActiveNode);
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
        /// <summary>Tooling to do auto connect</summary>
        private AutoConnectTools autoConnectTools;

        /// <summary>Constructor</summary>
        protected EditorActionOtherExit(string header, string pngFileName)
            : base(header, pngFileName)
        {
            autoConnectTools = new AutoConnectTools();
        }

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
        /// Find the list of junction node indexes of all junction nodes on the siding path but not being either siding
        /// start or siding end.
        /// </summary>
        /// <param name="mainNode">Node on main track for which we want to find the siding junction node indices</param>
        /// <returns>The list with the junction indexes</returns>
        private List<int> IntermediateSidingJunctionIndexes(TrainpathNode mainNode)
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
            List<TrainpathNode> reconnectNodes = autoConnectTools.FindReconnectNodeCandidates(startJunctionNode, true, false);
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
                if (TvnIndex <= 0)
                {
                    return null;
                }
                junctionsToCheck--;
            }
            return null;
        }

    }
    #endregion

    #region TakeOtherExit
    /// <summary>
    /// Subclass to implement the action: Take the other exit at a junction
    /// </summary>
    public class EditorActionTakeOtherExit : EditorActionOtherExit
    {
        private int nodesRemoved;

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
                ModificationTools.ReplaceNodeAndFollowingByNewNode(ActiveNode.NextMainNode);
                nodesRemoved = 0; // we do not care.
                return;
            }

            //correct NumberToDraw with nodes we will remove
            nodesRemoved = Trainpath.GetNodeNumber(ReconnectNode) - Trainpath.GetNodeNumber(ActiveNode);

            //we can reconnect, so create a path to reconnection point
            TrainpathNode lastNodeNewPath = ModificationTools.CreatePartialPath(ActiveNode, NewTvnIndex, ReconnectNode, true);

            //Reconnect.
            lastNodeNewPath.NextMainNode = ReconnectNode;
            ReconnectNode.PrevNode = lastNodeNewPath;
        }

        /// <summary>Returns the net amount of main nodes added.</summary>
        protected override int NetMainNodesAdded()
        {
            return ModificationTools.NetNodesAdded - nodesRemoved;
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
            TrainpathNode lastNodeSidingPath = ModificationTools.CreatePartialPath(ActiveNode, NewTvnIndex, ReconnectNode, false);

            //reconnect. At this point, newNode is already the same node as reconnectNode. We will discard it
            lastNodeSidingPath.NextSidingNode = ReconnectNode;
            ReconnectNode.NodeType = TrainpathNodeType.SidingEnd;

            //Set HasSidingPath for the main nodes until the reconnectNode
            for (TrainpathNode curMainNode = ActiveNode; curMainNode != ReconnectNode; curMainNode = curMainNode.NextMainNode)
            {
                curMainNode.HasSidingPath = true;
            }
        }

        /// <summary>Returns the net amount of main nodes added.</summary>
        protected override int NetMainNodesAdded()
        {
            return 0; // for passing paths we do not add main nodes;
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
            if (activeNodeAsJunction.IsSimpleSidingStart()) return false;

            //The idea was that if a normal passing path is possible, then there is no need for a complex passing path
            //However, if you want to connect not to the default recnnect node, we do need this
            //if (CanReconnectOtherExit(activeNodeAsJunction, true)) return false;
            if (NewNodeWouldBeOnMainTrack()) return false;
            return true;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            // we do add an extra siding path node, which is not well-connected.
            // We will draw it as broken. It will be recognized as broken as well.
            // Main track is not set to hasSidingNode.
            TrainpathNode danglingNode = ModificationTools.AddAdditionalNode(ActiveNode, NewTvnIndex, false);
            danglingNode.SetBroken(NodeStatus.Dangling);
            ActiveNode.NodeType = TrainpathNodeType.SidingStart;
        }

        /// <summary>Returns the net amount of main nodes added.</summary>
        protected override int NetMainNodesAdded()
        {
            return 0; // for passing paths we do not add main nodes;
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

    #region FixInvalidNode
    /// <summary>
    /// Subclass to implement the action: Remove the notion of being broken if it is not really broken
    /// </summary>
    public class EditorActionFixInvalidNode : EditorAction
    {
        /// <summary>Constructor</summary>
        public EditorActionFixInvalidNode() : base(TrackViewer.catalog.GetString("Fix invalid point"), "activeBroken") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            return ActiveNode.CanSetUnbroken();
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            ActiveNode.SetNonBroken();
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
        public EditorActionRemoveRestOfPath() : base(TrackViewer.catalog.GetString("Delete rest of path"), "activeNode") { }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (ActiveNode.HasSidingPath) return false;
            if (ActiveNode.NextSidingNode != null) return false; // Do not allow if it is on a siding.
            if (ActiveNode.NodeType == TrainpathNodeType.End) return false; // Nothing to remove
            return true;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            ActiveNode.NextMainNode = null;
            ActiveNode.NextSidingNode = null;
            Trainpath.HasEnd = false;
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

    #region AutoFixBrokenNodes
    /// <summary>
    /// Subclass to implement the action: Auto-fix broken nodes
    /// </summary>
    public class EditorActionAutoFixBrokenNodes : EditorAction
    {
        /// <summary>The node from which we will start the autofix procedure</summary>
        TrainpathNode autoFixStartNode;
        /// <summary>The node to which we need to reconnect during autofix</summary>
        TrainpathNode autoFixReconnectNode;
        /// <summary>The amount of nodes that will be removed if we succeed</summary>
        int numberOfNodesThatWillBeRemoved;
        /// <summary>Tooling to do auto connect</summary>
        AutoConnectTools autoConnectTools;

        /// <summary>Constructor</summary>
        public EditorActionAutoFixBrokenNodes()
            : base(TrackViewer.catalog.GetString("Auto-fix broken points"), "activeBroken")
        {
            autoConnectTools = new AutoConnectTools();
        }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            numberOfNodesThatWillBeRemoved = 0; // starting value

            if (ActiveNode.IsBroken)
            {   //this node is broken. But we must make sure there are non-broken nodes either before or after.
                numberOfNodesThatWillBeRemoved++; // this node will also be removed
                FindNonBrokenSuccessor();
                FindNonBrokenPredecessor();
                if ((autoFixStartNode == null) || (autoFixReconnectNode == null)) return false;
                return autoConnectTools.FindConnection(autoFixStartNode, autoFixReconnectNode);
            }

            if (ActiveNode.NextMainNode != null && ActiveNode.NextMainTvnIndex == -1)
            {   // the next link exists but is broken
                autoFixStartNode = ActiveNode;
                FindNonBrokenSuccessor();
                if (autoFixReconnectNode == null) return false;
                return autoConnectTools.FindConnection(autoFixStartNode, autoFixReconnectNode);
            }

            TrainpathNode prevNode = ActiveNode.PrevNode;
            if (prevNode == null) return false;
            if (prevNode.NextSidingNode != null) return false;
            if (prevNode.NextMainTvnIndex == -1)
            {   // the previous link exists but is broken
                autoFixReconnectNode = ActiveNode;
                FindNonBrokenPredecessor();
                if (autoFixStartNode == null) return false;
                return autoConnectTools.FindConnection(autoFixStartNode, autoFixReconnectNode);
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
                numberOfNodesThatWillBeRemoved++;
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
                numberOfNodesThatWillBeRemoved++;
            }
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            autoConnectTools.CreateFoundConnection(ModificationTools, true);
        }

        /// <summary>Returns the net amount of main nodes added.</summary>
        protected override int NetMainNodesAdded()
        {
            return ModificationTools.NetNodesAdded - numberOfNodesThatWillBeRemoved;
        }
    }
    #endregion

    #region AutoConnectToTail
    /// <summary>
    /// Subclass to implement the action: Auto-connect to the stored tail
    /// </summary>
    public class EditorActionAutoConnectTail : EditorAction
    {
        /// <summary>Tooling to do auto connect</summary>
        AutoConnectTools autoConnectTools;

        /// <summary>Constructor</summary>
        public EditorActionAutoConnectTail()
            : base(TrackViewer.catalog.GetString("Reconnect to tail"), "activeNode")
        {
            autoConnectTools = new AutoConnectTools();
        }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (Trainpath.FirstNodeOfTail == null) return false;
            if (ActiveNode.HasSidingPath) return false;
            if (ActiveNode.IsBroken) return false;
            return autoConnectTools.FindConnection(ActiveNode, Trainpath.FirstNodeOfTail);
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        {
            autoConnectTools.CreateFoundConnection(ModificationTools, true);

            Trainpath.FirstNodeOfTail = null;
            Trainpath.HasEnd = Trainpath.TailHasEnd;
        }

        /// <summary>Returns the net amount of main nodes added.</summary>
        protected override int NetMainNodesAdded()
        {
            return CountSuccessors(ActiveNode); // just draw everything, doesn't matter if we add too much.
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
    public class EditorActionReconnectPassingPath : EditorAction
    {
        TrainpathNode sidingStartNode;
        // <summary>Tooling to do auto connect</summary>
        AutoConnectTools autoConnectTools;

        /// <summary>Constructor</summary>
        public EditorActionReconnectPassingPath()
            : base(TrackViewer.catalog.GetString("Reconnect passing path"), "activeNode")
        {
            autoConnectTools = new AutoConnectTools();
        }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (ActiveNode.HasSidingPath) return false;
            if (ActiveNode.IsBroken) return false;

            FindPassingPathStub();
            if (sidingStartNode == null) return false;
            if (sidingStartNode.NextSidingNode == ActiveNode) return false;  // this happens when we are at the single siding node we just started
            return autoConnectTools.FindConnection(sidingStartNode.NextSidingNode, ActiveNode);
        }

        /// <summary>
        /// This action can only be done if there is a passing path stub, that is, a start of a passing path
        /// that contains only a single siding node that is broken and not reconnected.
        /// </summary>
        void FindPassingPathStub()
        {
            sidingStartNode = null;
            autoConnectTools.ResetDisallowedJunctions();

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
                    autoConnectTools.AddDisallowedJunction(mainNodeAsJunction.JunctionIndex);
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
            autoConnectTools.CreateFoundConnection(ModificationTools, false);

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
        }

        /// <summary>Returns the net amount of main nodes added.</summary>
        protected override int NetMainNodesAdded()
        {
            return 0; // for passing paths we do not add main nodes;
        }
    }
    #endregion

    #region TakeOtherExitPassingPath
    /// <summary>
    /// Subclass to implement the action: In a passing path, take the other exit reconnecting to the same siding-end
    /// </summary>
    public class EditorActionTakeOtherExitPassingPath : EditorAction
    {
        TrainpathNode sidingEndNode;
        // <summary>Tooling to do auto connect</summary>
        AutoConnectTools autoConnectTools;


        /// <summary>Constructor</summary>
        public EditorActionTakeOtherExitPassingPath()
            : base(TrackViewer.catalog.GetString("Take other exit"), "activeNode")
        {
            autoConnectTools = new AutoConnectTools();
        }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (ActiveNode.IsBroken) return false;
            if (ActiveNode.NextMainNode != null) return false;
            if (ActiveNode.NextSidingNode == null) return false;
            if (ActiveNode.NextSidingNode.NodeType == TrainpathNodeType.SidingEnd) return false; // not enough room to take other exit.

            TrainpathJunctionNode activeNodeAsJunction = (ActiveNode as TrainpathJunctionNode);
            if ((activeNodeAsJunction == null) || (!activeNodeAsJunction.IsFacingPoint))
            {
                return false;
            }

            FindSidingEnd();
            if (sidingEndNode == null) return false;

            if (!FindDisAllowedJunctionIndexes()) return false;
            int newNextTvnIndex = activeNodeAsJunction.OtherExitTvnIndex();
            return autoConnectTools.FindConnection(ActiveNode, sidingEndNode, newNextTvnIndex);
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
            autoConnectTools.ResetDisallowedJunctions();

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
                    autoConnectTools.AddDisallowedJunction(mainNodeAsJunction.JunctionIndex);
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
            autoConnectTools.CreateFoundConnection(ModificationTools, false);
        }

        /// <summary>Returns the net amount of main nodes added.</summary>
        protected override int NetMainNodesAdded()
        {
            return 0; // for passing paths we do not add main nodes;
        }
    }
    #endregion

    #region MouseDrag (template)
    /// <summary>
    /// abstract class for all mouse dragging sub classes.
    /// Contains implementations for handling history of path (undo/redo functionality during dragging) (template pattern)
    /// The two methods that need to be overriden are InitDragging and SucceededDragging.
    /// The hook Cleanup can be overridden if needed.
    /// </summary>
    public abstract class EditorActionMouseDrag : EditorAction
    {
        /// <summary>Constructor</summary>
        protected EditorActionMouseDrag(string header, string pngFileName) : base(header, pngFileName) { }

        /// <summary>
        /// Dragging has commenced. Perform initialization actions
        /// </summary>
        public void StartDragging()
        {
            Trainpath.StoreCurrentPath();
            InitDragging();
            Dragging(); // Immediate start dragging (the mouse is already somewhere else)
        }

        /// <summary>
        /// Update the location of the node that is being dragged.
        /// This is where most of the logic of mouse dragging takes place.
        /// Do note that this will be called for every update (so in interactive loop)
        /// </summary>
        public void Dragging()
        {
            if (SucceededDragging())
            {
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
            CleanUp();
        }

        /// <summary>
        /// End the dragging and but do not use the modified path (but use the previouse version instead)
        /// </summary>
        public void CancelDragging()
        {
            CleanUp();
            Trainpath.UseLastButOne();
        }

        /// <summary>
        /// Everything you want to initialize before real dragging starts.
        /// Put here as much as possible of pre-processing, as this is only called once.
        /// Note that undo/redo is already taken care of.
        /// </summary>
        protected abstract void InitDragging();

        /// <summary>
        /// Determine whether the dragging to the new location can be done, make sure the new path is good and well-connected
        /// </summary>
        /// <returns>true if the new path is good and can be shown</returns>
        protected abstract bool SucceededDragging();

        /// <summary>
        /// Overridable hook to perform some cleanup actions at the end of dragging (EndDragging or CancelDragging)
        /// </summary>
        protected virtual void CleanUp()
        {// empty default implementation
        }

    }
    #endregion

    #region MouseDragVectorNode
    /// <summary>
    /// Subclass to implement the actions related to mouse dragging.
    /// This class is about dragging only start, end, wait and reverse points: those points can only be
    /// dragged along the track they are on, so not beyond a junction or so.
    /// </summary>
    public class EditorActionMouseDragVectorNode : EditorActionMouseDrag
    {
        TrainpathNode dragLimitNode1, dragLimitNode2; // the dragging node must be between these two limits
        TrainpathVectorNode nodeBeingDragged;               // link to the node (new) that is being dragged.

        /// <summary>Constructor</summary>
        public EditorActionMouseDragVectorNode() : base("Drag a special node", "") { }

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
        /// Everything you want to initialize before real dragging starts.
        /// Put here as much as possible of pre-processing, as this is only called once.
        /// Note that undo/redo is already taken care of.
        /// </summary>
        protected override void InitDragging()
        {
            switch (ActiveNode.NodeType)
            {
                case TrainpathNodeType.Start: FindDraggingLimitsStart(); break;
                case TrainpathNodeType.End: FindDraggingLimitsEnd(); break;
                case TrainpathNodeType.Stop: FindDraggingLimitsStop(); break;
                case TrainpathNodeType.Reverse: FindDraggingLimitsReverse(); break;
                default: break;
            }
        }

        /// <summary>
        /// Update the location of the node that is being dragged.
        /// </summary>
        protected override bool SucceededDragging()
        {

            if ((nodeBeingDragged.TvnIndex == ActiveTrackLocation.TvnIndex)
              && (ActiveTrackLocation.Location != WorldLocation.None)
              && (ActiveTrackLocation.IsBetween(dragLimitNode1, dragLimitNode2)))
            {
                nodeBeingDragged.Location = ActiveTrackLocation.Location;
                nodeBeingDragged.TrackVectorSectionIndex = ActiveTrackLocation.TrackVectorSectionIndex;
                nodeBeingDragged.TrackSectionOffset = ActiveTrackLocation.TrackSectionOffset;

                return true;
            }
            else
            {
                return false;
            }
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
                    if (prevVectorNode.IsBetween(nextVectorNode, nodeBeingDragged))
                    {
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

    #region MouseDragAutoConnect
    /// <summary>
    /// Subclass to implement the actions related to mouse dragging.
    /// This class is about dragging 
    /// </summary>
    public class EditorActionMouseDragAutoConnect : EditorActionMouseDrag
    {
        /// <summary>Tooling to do auto connect</summary>
        private ContinuousAutoConnecting autoConnectForward;
        private ContinuousAutoConnecting autoConnectReverse;
        private TrainpathVectorNode nodeBeingDragged;               // link to a possibly possibly temporary node that is being dragged.
        private int netNodesDeleted;

        //private DebugWindow debugWindow;
        /// <summary>Constructor</summary>
        public EditorActionMouseDragAutoConnect()
            : base("Drag any node", "")
        {
            //debugWindow = new DebugWindow(10, 20);
        }

        /// <summary>Can the action be executed given the current path and active nodes?</summary>
        protected override bool CanExecuteAction()
        {
            if (Trainpath.FirstNode == null) return false;
            if (ActiveNode.IsBroken) return false;
            if (ActiveTrackLocation.Location == null) return false; // we need at least something to start dragging with
            return true;
        }

        /// <summary>Execute the action. This assumes that the action can be executed</summary>
        protected override void ExecuteAction()
        { //not implemented because mouse dragging has its own methods
        }

        /// <summary>
        /// Everything you want to initialize before real dragging starts.
        /// Put here as much as possible of pre-processing, as this is only called once.
        /// Note that undo/redo is already taken care of.
        /// </summary>
        protected override void InitDragging()
        {
            ModificationTools.Reset();
            netNodesDeleted = 0;

            if (ActiveNode is TrainpathJunctionNode || (ActiveNode.NodeType == TrainpathNodeType.Other))
            {
                // we need a new vector node here.
                nodeBeingDragged = ModificationTools.AddIntermediateMainNode(ActiveTrackLocation);
                nodeBeingDragged.NodeType = TrainpathNodeType.Temporary;
            }
            else
            {
                nodeBeingDragged = (ActiveNode as TrainpathVectorNode);
            }
            UpdateNodeCount();
            InitAutoConnects();
        }

        private void InitAutoConnects()
        {
            autoConnectForward = new ContinuousAutoConnecting(nodeBeingDragged, true);
            autoConnectReverse = new ContinuousAutoConnecting(nodeBeingDragged, false);

            int danglingNodeJunctionIndex = FindPossibleDanglingNodeJunctionIndex(nodeBeingDragged);
            if (danglingNodeJunctionIndex >= 0)
            {
                autoConnectForward.AddDisallowedJunction(danglingNodeJunctionIndex);
                autoConnectReverse.AddDisallowedJunction(danglingNodeJunctionIndex);
            }
        }

        /// <summary>
        /// Possibly a single dangling node is present over which we cannot drag.
        /// This can only be a node connected from the first sidingstart when looking backwards,
        /// but it can still affect the forward-connecting path 
        /// </summary>
        /// <param name="node">The node to start searching from</param>
        /// <returns>The index of the dangling node, or -1 if not found</returns>
        private static int FindPossibleDanglingNodeJunctionIndex(TrainpathNode node)
        {
            while (node != null)
            {
                if (node.NodeType == TrainpathNodeType.SidingEnd)
                {
                    break;
                }
                if (node.NodeType == TrainpathNodeType.SidingStart)
                {
                    if (node.NextSidingNode.IsBroken)
                    {   // it is a dangling node
                        return (node.NextSidingNode as TrainpathJunctionNode).JunctionIndex;
                    }
                    break;
                }
                node = node.PrevNode;
            }
            return -1;
        }

        /// <summary>
        /// Update the location of the node that is being dragged.
        /// </summary>
        protected override bool SucceededDragging()
        {
            //update location of the node
            if (ActiveTrackLocation.Location == null) return false;

            bool tvnIndexChanged = (nodeBeingDragged.TvnIndex != ActiveTrackLocation.TvnIndex);

            nodeBeingDragged.Location = ActiveTrackLocation.Location;
            nodeBeingDragged.TvnIndex = ActiveTrackLocation.TvnIndex;
            nodeBeingDragged.TrackVectorSectionIndex = ActiveTrackLocation.TrackVectorSectionIndex;
            nodeBeingDragged.TrackSectionOffset = ActiveTrackLocation.TrackSectionOffset;

            bool succeededToDrag;
            switch (nodeBeingDragged.NodeType)
            {
                case TrainpathNodeType.Start:
                    succeededToDrag = CouldAndMadeConnectionStart();
                    break;
                case TrainpathNodeType.End:
                    succeededToDrag = CouldAndMadeConnectionEnd();
                    break;
                default:
                    succeededToDrag = CouldAndMadeConnectionNormal();
                    break;
            }
            if (tvnIndexChanged && succeededToDrag)
            {
                // The path was changed more than just moving along same track, so we have to re-initialize the possible connections
                InitAutoConnects();
                UpdateNodeCount();
            }

            return succeededToDrag;
        }

        /// <summary>
        /// Check if a connection can be made, and if it can, make it.
        /// For normal situations where both after and before the node a connection needs to be made
        /// </summary>
        /// <returns>Connection was found and made</returns>
        private bool CouldAndMadeConnectionNormal()
        {
            bool canConnectForward = autoConnectForward.CanConnect(nodeBeingDragged);
            bool canConnectReverse = autoConnectReverse.CanConnect(nodeBeingDragged);
            bool connectionIsSameDirection = (autoConnectForward.NeedsReverse == autoConnectReverse.NeedsReverse);
            bool connectionIsGood = canConnectForward && canConnectReverse && connectionIsSameDirection;

            //in some cases (e.g. due to loops) there is a good connection but it has not been found yet.
            //This happens if, due to the loop, for either forward or reverse connect both with and without a reverse
            //a solution exist. So a second solution might exist. We test this by starting reversed
            if (!connectionIsGood && canConnectForward && canConnectReverse)
            {
                nodeBeingDragged.ReverseOrientation();
                canConnectForward = autoConnectForward.CanConnect(nodeBeingDragged);
                canConnectReverse = autoConnectReverse.CanConnect(nodeBeingDragged);
                connectionIsSameDirection = (autoConnectForward.NeedsReverse == autoConnectReverse.NeedsReverse);
                connectionIsGood = canConnectForward && canConnectReverse && connectionIsSameDirection;

            }

            //debugWindow.DrawString = String.Format("tvn={0} ({1}), F={2} R={3}: {4} {5} {6}",
            //    nodeBeingDragged.TvnIndex, nodeBeingDragged.NodeType, canConnectForward, canConnectReverse,
            //    nodeBeingDragged.PrevNode.ToStringConnection(),
            //    nodeBeingDragged.ToStringConnection(),
            //    nodeBeingDragged.NextMainNode.ToStringConnection()
            //    );

            //debugWindow.DrawString = String.Format("tvn={0} ({1}), F={2} R={3}: A={4}",
            //    nodeBeingDragged.TvnIndex, nodeBeingDragged.NodeType, canConnectForward, canConnectReverse,
            //    ActiveNode.ToString()
            //    );
            if (connectionIsGood)
            {
                PrepareNodeCountUpdate(autoConnectReverse.ReconnectTrainpathNode, autoConnectForward.ReconnectTrainpathNode, -1);//Node being dragged is added again.   
                autoConnectForward.CreateFoundConnection(ModificationTools, true);
                autoConnectReverse.CreateFoundConnection(ModificationTools, true, true);
            }

            return connectionIsGood;
        }

        /// <summary>
        /// Check if a connection can be made, and if it can, make it.
        /// For a start node where only the connection from the node needs to be made.
        /// </summary>
        /// <returns>Connection was found and made</returns>
        private bool CouldAndMadeConnectionStart()
        {
            bool connectionIsGood = autoConnectForward.CanConnect(nodeBeingDragged);

            if (connectionIsGood)
            {
                PrepareNodeCountUpdate(nodeBeingDragged, autoConnectForward.ReconnectTrainpathNode, 0);
                autoConnectForward.CreateFoundConnection(ModificationTools, true);
            }

            return connectionIsGood;
        }

        /// <summary>
        /// Check if a connection can be made, and if it can, make it.
        /// For a end node where only the connection to the node needs to be made.
        /// </summary>
        /// <returns>Connection was found and made</returns>
        private bool CouldAndMadeConnectionEnd()
        {
            bool connectionIsGood = autoConnectReverse.CanConnect(nodeBeingDragged);

            if (connectionIsGood)
            {
                PrepareNodeCountUpdate(autoConnectReverse.ReconnectTrainpathNode, nodeBeingDragged, 0);
                autoConnectReverse.CreateFoundConnection(ModificationTools, true); // note: reverse is not yet done, compare 'normal connection'
            }

            return connectionIsGood;
        }

        /// <summary>
        /// Prepare to keep node count up to date. Reset modification tool and calculate the
        /// amount of nodes deleted (given the reconnectpoint backwards and forwards.
        /// Number of deleted nodes is the amount of nodes between the two unchanged nodes, plus a possible correction
        /// </summary>
        private void PrepareNodeCountUpdate(TrainpathNode lastUnchangedNodeReverse, TrainpathNode firstUnchangedNodeForward, int deletedNodesCorrection)
        {
            ModificationTools.Reset();
            int originalNodeNumberReconnectNodeForward = Trainpath.GetNodeNumber(firstUnchangedNodeForward);
            int originalNodeNumberReconnectNodeReverse = Trainpath.GetNodeNumber(lastUnchangedNodeReverse);
            netNodesDeleted = originalNodeNumberReconnectNodeForward - originalNodeNumberReconnectNodeReverse - 1;
            netNodesDeleted += deletedNodesCorrection;

            //debugWindow.DrawString = String.Format("deleted {0} - {1} - 2 = {2}, added {3}", 
            //    originalNodeNumberReconnectNodeForward, originalNodeNumberReconnectNodeReverse,
            //    netNodesDeleted, modificationTools.NetNodesAdded);

        }

        /// <summary>
        /// Overridable hook to perform some cleanup actions at the end of dragging (EndDragging or CancelDragging)
        /// </summary>
        protected override void CleanUp()
        {
            if (nodeBeingDragged.NodeType == TrainpathNodeType.Temporary || nodeBeingDragged.NodeType == TrainpathNodeType.Other)
            {
                ModificationTools.RemoveIntermediatePoint(nodeBeingDragged);
            }
            UpdateNodeCount();
        }

        /// <summary>Returns the net amount of main nodes added.</summary>
        protected override int NetMainNodesAdded()
        {
            return ModificationTools.NetNodesAdded - netNodesDeleted;
        }
        //todo for dragging:
        // * Passing paths.
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
        protected override void ExecuteAction() { }

        /// <summary>
        /// Add additional main nodes (to the end of the current path)
        /// </summary>
        /// <param name="currentNode">Node after which to add a node</param>
        /// <param name="numberOfNodes">The number of nodes to add</param>
        /// <param name="callback">Callback to call when node has been added</param>
        public void AddMainNodes(TrainpathNode currentNode, int numberOfNodes, AfterEditDelegate callback)
        {
            if (currentNode.IsBroken) return;
            ModificationTools.Reset();
            ModificationTools.AddAdditionalMainNodes(currentNode, numberOfNodes);
            callback(ModificationTools.NetNodesAdded);
        }

        /// <summary>
        /// Upon starting editing a path, make sure everywhere where needed disambibuity nodes are added (which is
        /// not always the case when loaded from file)
        /// </summary>
        /// <param name="trainpath">The trainpath for which the disambiguity nodes need to be added</param>
        /// <param name="callback">Callback to call when node has been added</param>
        public void AddMissingDisambiguityNodes(Trainpath trainpath, AfterEditDelegate callback)
        {
            ModificationTools.Reset();

            TrainpathNode mainNode = trainpath.FirstNode;
            while (mainNode != null)
            {
                ModificationTools.AddDisambiguityNodeIfNeeded(mainNode);
                mainNode = mainNode.NextMainNode;
            }

            trainpath.IsModified = (ModificationTools.NetNodesAdded != 0);
            callback(ModificationTools.NetNodesAdded);
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
            int newNumberToDraw = Trainpath.GetNodeNumber(this.ActiveNode);
            this.callback(newNumberToDraw);
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
            var brokenNodeNumbers = (from node in Trainpath.GetBrokenNodes() select Trainpath.GetNodeNumber(node));
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
