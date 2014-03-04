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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms.Integration;

using MSTS.Formats;
using ORTS.Common;
using ORTS.TrackViewer.Properties;
using ORTS.TrackViewer.Editing;


namespace ORTS.TrackViewer.UserInterface
{
    /// <summary>
    /// Interaction logic for StatusBarControl.xaml
    /// The statusbar at the bottom of the screen shows informational things like location, track items and possibly
    /// details on the track section, the path, ....
    /// Most of the items have a dedicated place in the statusbar. For flexibility the last item is simply a string
    /// that can contain various items (depending on the users setting/choice as well as for debug during development
    /// </summary>
    public partial class StatusBarControl : UserControl
    {
        public int statusbarHeight;
        private TrackViewer trackViewer;
        private ElementHost elementHost;

        public StatusBarControl(TrackViewer trackViewer)
        {
            this.trackViewer = trackViewer;
            InitializeComponent();

            statusbarHeight = (int) tvStatusbar.Height;

            //ElementHost object helps us to connect a WPF User Control.
            elementHost = new ElementHost();
            elementHost.Location = new System.Drawing.Point(0, 0);
            elementHost.Name = "elementHost";
            elementHost.TabIndex = 1;
            elementHost.Text = "elementHost";
            elementHost.Child = this;
            System.Windows.Forms.Control.FromHandle(trackViewer.Window.Handle).Controls.Add(elementHost);

        }

        /// <summary>
        /// set the size of the menu control (also after rescaling)
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void setScreenSize(int width, int height, int yBottom)
        {
            elementHost.Location = new System.Drawing.Point(0, yBottom-height);
            elementHost.Size = new System.Drawing.Size(width, height);
        }

        /// <summary>
        /// Update the various elements in the statusbar
        /// </summary>
        /// <param name="trackViewer">trackViewer object that contains all relevant data</param>
        /// <param name="mouseLocation">The Worldlocation of the mouse pointer</param>
        public void Update(TrackViewer trackViewer, WorldLocation mouseLocation)
        {
            ResetAdditionalText();

            SetMouseLocationStatus(mouseLocation);

            SetTrackIndexStatus(trackViewer);

            SetTrackItemStatus(trackViewer);

            AddFPS(trackViewer);
            AddVectorSectionStatus(trackViewer);
            AddPATfileStatus(trackViewer);
            AddTrainpathStatus(trackViewer);
        }

        /// <summary>
        /// Update the status of the track index
        /// </summary>
        /// <param name="trackViewer"></param>
        private void SetTrackIndexStatus(TrackViewer trackViewer)
        {
            statusTrIndex.Text = string.Format("{0} ", trackViewer.drawTrackDB.closestTrack.TrackNode.Index);
            //debug: statusAdditional.Text += Math.Sqrt((double)trackViewer.drawTrackDB.closestTrack.ClosestMouseDistanceSquared);
        }

        /// <summary>
        /// Reset (is clear) the additionalText line
        /// </summary>
        private void ResetAdditionalText()
        {
            statusAdditional.Text = String.Empty;
        }

        /// <summary>
        /// Set the status of the closest trackItem (junction or other item)
        /// </summary>
        /// <param name="trackViewer"></param>
        private void SetTrackItemStatus(TrackViewer trackViewer)
        {
            // Track items: clear first
            statusTrItemType.Text = statusTrItemIndex.Text =
                statusTrItemLocationX.Text = statusTrItemLocationZ.Text = string.Empty;

            ORTS.TrackViewer.Drawing.CloseToMouseItem closestItem = trackViewer.drawTrackDB.closestTrItem;
            ORTS.TrackViewer.Drawing.CloseToMouseJunctionOrEnd closestJunction = trackViewer.drawTrackDB.closestJunctionOrEnd;
            if (closestItem != null && closestItem.IsCloserThan(closestJunction))
            {
                statusTrItemType.Text = closestItem.type;
                statusTrItemIndex.Text = string.Format("{0} ", closestItem.trItem.TrItemId);
                statusTrItemLocationX.Text = string.Format("{0,3:F3} ", closestItem.trItem.X);
                statusTrItemLocationZ.Text = string.Format("{0,3:F3} ", closestItem.trItem.Z);
            }
            else if (closestJunction.junctionOrEndNode != null)
            {
                statusTrItemType.Text = closestJunction.type;
                TrackNode node = closestJunction.junctionOrEndNode;
                statusTrItemIndex.Text = string.Format("{0} ", node.Index);
                statusTrItemLocationX.Text = string.Format("{0,3:F3} ", node.UiD.X);
                statusTrItemLocationZ.Text = string.Format("{0,3:F3} ", node.UiD.Z);
            }
        }

        /// <summary>
        /// Put the mouse location in the statusbar
        /// </summary>
        /// <param name="mouseLocation"></param>
        private void SetMouseLocationStatus(WorldLocation mouseLocation)
        {
            tileXZ.Text = string.Format("{0,-7} {1,-7}", mouseLocation.TileX, mouseLocation.TileZ);
            LocationX.Text = string.Format("{0,3:F3} ", mouseLocation.Location.X);
            LocationZ.Text = string.Format("{0,3:F3} ", mouseLocation.Location.Z);
        }

        /// <summary>
        /// Add information about the closest vector section
        /// </summary>
        /// <param name="trackViewer"></param>
        private void AddVectorSectionStatus(TrackViewer trackViewer)
        {
            if (Properties.Settings.Default.statusShowVectorSections)
            {
                TrVectorSection tvs = trackViewer.drawTrackDB.closestTrack.VectorSection;
                if (tvs == null) return;
                uint shapeIndex = tvs.ShapeIndex;
                string shapeName = "Unknown:" + shapeIndex.ToString();
                try
                {
                    // Try to find a fixed track
                    TrackShape shape = trackViewer.drawTrackDB.tsectionDat.TrackShapes.Get(shapeIndex);
                    shapeName = shape.FileName;
                }
                catch
                {
                    // try to find a dynamic track
                    try
                    {
                        TrackPath trackPath = trackViewer.drawTrackDB.tsectionDat.TSectionIdx.TrackPaths[tvs.ShapeIndex];
                        shapeName = "<dynamic ?>";
                        foreach (uint trackSection in trackPath.TrackSections)
                        {
                            if (trackSection == tvs.SectionIndex)
                            {
                                shapeName = "<dynamic>";
                            }
                            // For some reason I do not undestand the (route) section.tdb. trackpaths are not consistent tracksections
                            // so this foreach loop will not always find a combination
                        }
                    }
                    catch
                    {
                    }
                }
                statusAdditional.Text += string.Format(" VectorSection ({3}/{4}) filename={2} Index={0} shapeIndex={1}",
                    tvs.SectionIndex, shapeIndex, shapeName,
                        trackViewer.drawTrackDB.closestTrack.TrackVectorSectionIndex + 1,
                        trackViewer.drawTrackDB.closestTrack.TrackNode.TrVectorNode.TrVectorSections.Count());
            }
        }

        /// <summary>
        /// Add information from Trainpaths
        /// </summary>
        /// <param name="trackViewer"></param>
        private void AddTrainpathStatus(TrackViewer trackViewer)
        {
            if (Properties.Settings.Default.statusShowTrainpath && (trackViewer.pathEditor != null))
            {
                if (trackViewer.pathEditor.HasValidPath)
                {
                    //statusAdditional.Text += string.Format("|{0}->{1}|", trackViewer.pathEditor.numberToDraw, trackViewer.pathEditor.numberDrawn);
                    ORTS.TrackViewer.Editing.TrainpathNode curNode = trackViewer.pathEditor.CurrentNode;
                    statusAdditional.Text += string.Format(" {0}: TVNs=[{1} {2}] ({3}, {4})",
                        trackViewer.pathEditor.fileName, curNode.NextMainTVNIndex, curNode.NextSidingTVNIndex,
                        curNode.Type, curNode.HasSidingPath);
                    TrainpathVectorNode curVectorNode = curNode as TrainpathVectorNode;
                    if (curVectorNode != null)
                    {
                        statusAdditional.Text += string.Format(" (waitT={0}, waitUntil={1}, Ncars={2}",
                            curVectorNode.WaitTimeS, curVectorNode.WaitUntil, curVectorNode.NCars);
                    }
            
                }
                else
                {
                    statusAdditional.Text += "Invalid path";
                }
            }
        }

        /// <summary>
        /// Add information of the basic MSTS PATfile
        /// </summary>
        /// <param name="trackViewer"></param>
        private void AddPATfileStatus(TrackViewer trackViewer)
        {
            if (Properties.Settings.Default.statusShowPATfile && (trackViewer.drawPATfile != null))
            {
                TrPathNode curNode = trackViewer.drawPATfile.CurrentNode;
                TrackPDP curPDP = trackViewer.drawPATfile.CurrentPDP;
                statusAdditional.Text += string.Format(" {7}: {3}, {4} [{1} {2}] [{5} {6}] <{0}>",
                    curNode.pathFlags, (int)curNode.nextMainNode, (int)curNode.nextSidingNode,
                    curPDP.X, curPDP.Z, curPDP.junctionFlag, curPDP.invalidFlag, trackViewer.drawPATfile.fileName);
            }
        }

        /// <summary>
        /// Add the FPS to the statusbar (Frames Per Second)
        /// </summary>
        private void AddFPS(TrackViewer trackViewer)
        {
            if (Properties.Settings.Default.statusShowFPS)
            {
                statusAdditional.Text += string.Format(" FPS={0:F1} ", trackViewer.FrameRate.SmoothedValue);
            }
        }

    }
}
