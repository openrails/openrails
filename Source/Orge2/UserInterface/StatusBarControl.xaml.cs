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

using Orts.Formats.Msts;
using ORTS.Common;
using ORTS.Orge.Properties;
using ORTS.Orge.Editing;


namespace ORTS.Orge.UserInterface
{
    /// <summary>
    /// Interaction logic for StatusBarControl.xaml
    /// The statusbar at the bottom of the screen shows informational things like location, track items and possibly
    /// details on the track section, the path, ....
    /// Most of the items have a dedicated place in the statusbar. For flexibility the last item is simply a string
    /// that can contain various items (depending on the users setting/choice as well as for debug during development
    /// </summary>
    public sealed partial class StatusBarControl : UserControl, IDisposable
    {
        /// <summary>Height of the statusbar in pixels</summary>
        public int StatusbarHeight { get; private set; }
        private ElementHost elementHost;

        /// <summary>
        /// Constructor for the statusbar
        /// </summary>
        /// <param name="trackViewer">Track viewer object that contains all the information we want to show the status for</param>
        public StatusBarControl(RouteGenExtract trackViewer)
        {
            InitializeComponent();

            StatusbarHeight = (int) tvStatusbar.Height;

            //ElementHost object helps us to connect a WPF User Control.
            elementHost = new ElementHost
            {
                Location = new System.Drawing.Point(0, 0),
                TabIndex = 1,
                Child = this
            };
            System.Windows.Forms.Control.FromHandle(trackViewer.Window.Handle).Controls.Add(elementHost);

        }

        /// <summary>
        /// set the size of the statusbar control (also after rescaling)
        /// </summary>
        /// <param name="width">Width of the statusbar</param>
        /// <param name="height">Height of the statusbar</param>
        /// <param name="yBottom">Y-value in screen pixels at the bottom of the statusbar</param>
        public void SetScreenSize(int width, int height, int yBottom)
        {
            elementHost.Location = new System.Drawing.Point(0, yBottom-height);
            elementHost.Size = new System.Drawing.Size(width, height);
        }

        /// <summary>
        /// Update the various elements in the statusbar
        /// </summary>
        /// <param name="trackViewer">trackViewer object that contains all relevant data</param>
        /// <param name="mouseLocation">The Worldlocation of the mouse pointer</param>
        public void Update(RouteGenExtract trackViewer, WorldLocation mouseLocation)
        {
            ResetAdditionalText();

            SetMouseLocationStatus(mouseLocation);

            SetTrackIndexStatus(trackViewer);

            SetTrackItemStatus(trackViewer);

            AddFPS(trackViewer);
            AddVectorSectionStatus(trackViewer);
            AddPATfileStatus(trackViewer);
            AddTrainpathStatus(trackViewer);
            AddTerrainStatus(trackViewer);
        }

        /// <summary>
        /// Update the status of the track index
        /// </summary>
        /// <param name="trackViewer"></param>
        private void SetTrackIndexStatus(RouteGenExtract trackViewer)
        {
            Drawing.CloseToMouseTrack closestTrack = trackViewer.DrawTrackDB.ClosestTrack;
            if (closestTrack == null) return;
            TrackNode tn = closestTrack.TrackNode;
            if (tn == null) return;
            statusTrIndex.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0} ", tn.Index);
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
        private void SetTrackItemStatus(RouteGenExtract trackViewer)
        {
            // Track items: clear first
            statusTrItemType.Text = statusTrItemIndex.Text =
                statusTrItemLocationX.Text = statusTrItemLocationZ.Text = string.Empty;

            ORTS.Orge.Drawing.CloseToMouseItem closestItem = trackViewer.DrawTrackDB.ClosestTrackItem;
            ORTS.Orge.Drawing.CloseToMouseJunctionOrEnd closestJunction = trackViewer.DrawTrackDB.ClosestJunctionOrEnd;
            ORTS.Orge.Drawing.CloseToMousePoint closestPoint;
            if (closestItem != null && closestItem.IsCloserThan(closestJunction))
            {
                closestPoint = closestItem;
            }
            else if (closestJunction.JunctionOrEndNode != null)
            {
                closestPoint = closestJunction;
            }
            else
            {
                closestPoint = null;
            }

            if (closestPoint != null)
            {
                statusTrItemType.Text = closestPoint.Description;
                statusTrItemIndex.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0} ", closestPoint.Index);
                statusTrItemLocationX.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0,3:F3} ", closestPoint.X);
                statusTrItemLocationZ.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    "{0,3:F3} ", closestPoint.Z);
                AddSignalStatus(trackViewer, closestPoint.Description, closestPoint.Index);
                AddNamesStatus(trackViewer, closestPoint.Description, closestPoint.Index);
            }
        }

        /// <summary>
        /// Put the mouse location in the statusbar
        /// </summary>
        /// <param name="mouseLocation"></param>
        private void SetMouseLocationStatus(WorldLocation mouseLocation)
        {
            tileXZ.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0,-7} {1,-7}", mouseLocation.TileX, mouseLocation.TileZ);
            LocationX.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0,3:F3} ", mouseLocation.Location.X);
            LocationZ.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0,3:F3} ", mouseLocation.Location.Z);
        }

        /// <summary>
        /// Add information about the closest vector section
        /// </summary>
        /// <param name="trackViewer"></param>
        private void AddVectorSectionStatus(RouteGenExtract trackViewer)
        {
            if (Properties.Settings.Default.statusShowVectorSections)
            {
                TrVectorSection tvs = trackViewer.DrawTrackDB.ClosestTrack.VectorSection;
                if (tvs == null) return;
                uint shapeIndex = tvs.ShapeIndex;
                string shapeName = "Unknown:" + shapeIndex.ToString(System.Globalization.CultureInfo.CurrentCulture);
                try
                {
                    // Try to find a fixed track
                    TrackShape shape = trackViewer.RouteData.TsectionDat.TrackShapes.Get(shapeIndex);
                    shapeName = shape.FileName;
                }
                catch
                {
                    // try to find a dynamic track
                    try
                    {
                        TrackPath trackPath = trackViewer.RouteData.TsectionDat.TSectionIdx.TrackPaths[tvs.ShapeIndex];
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
                statusAdditional.Text += string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    " VectorSection ({3}/{4}) filename={2} Index={0} shapeIndex={1}",
                    tvs.SectionIndex, shapeIndex, shapeName,
                        trackViewer.DrawTrackDB.ClosestTrack.TrackVectorSectionIndex + 1,
                        trackViewer.DrawTrackDB.ClosestTrack.TrackNode.TrVectorNode.TrVectorSections.Count());
            }
        }

        /// <summary>
        /// Add information from Trainpaths
        /// </summary>
        /// <param name="trackViewer"></param>
        private void AddTrainpathStatus(RouteGenExtract trackViewer)
        {
            if (Properties.Settings.Default.statusShowTrainpath && (trackViewer.PathEditor != null))
            {
                if (trackViewer.PathEditor.HasValidPath)
                {
                    //gather some info on path status
                    List<string> statusItems = new List<string>();
                    
                    if (trackViewer.PathEditor.HasEndingPath) statusItems.Add("good end");
                    if (trackViewer.PathEditor.HasBrokenPath) statusItems.Add("broken");
                    if (trackViewer.PathEditor.HasModifiedPath) statusItems.Add("modified");
                    if (trackViewer.PathEditor.HasStoredTail) statusItems.Add("stored tail");
                    
                    string pathStatus = String.Join(", ", statusItems.ToArray());
                    
                    ORTS.Orge.Editing.TrainpathNode curNode = trackViewer.PathEditor.CurrentNode;
                    
                    statusAdditional.Text += string.Format(System.Globalization.CultureInfo.CurrentCulture,
                        " {0} ({4}): TVNs=[{1} {2}] (type={3})",
                        trackViewer.PathEditor.FileName, curNode.NextMainTvnIndex, curNode.NextSidingTvnIndex,
                        curNode.NodeType, pathStatus);

                    if (curNode.IsBroken)
                    {
                        statusAdditional.Text += string.Format(System.Globalization.CultureInfo.CurrentCulture,
                            " Broken: {0} ", curNode.BrokenStatusString());
                    }
                    TrainpathVectorNode curVectorNode = curNode as TrainpathVectorNode;
                    if (curVectorNode != null && curNode.NodeType == TrainpathNodeType.Stop)
                    {
                        statusAdditional.Text += string.Format(System.Globalization.CultureInfo.CurrentCulture,
                            " (wait-time={0}s)",
                            curVectorNode.WaitTimeS);
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
        private void AddPATfileStatus(RouteGenExtract trackViewer)
        {
            if (Properties.Settings.Default.statusShowPATfile && (trackViewer.DrawPATfile != null))
            {
                TrPathNode curNode = trackViewer.DrawPATfile.CurrentNode;
                TrackPDP curPDP = trackViewer.DrawPATfile.CurrentPdp;
                statusAdditional.Text += string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    " {7}: {3}, {4} [{1} {2}] [{5} {6}] <{0}>",
                    curNode.pathFlags, (int)curNode.nextMainNode, (int)curNode.nextSidingNode,
                    curPDP.X, curPDP.Z, curPDP.junctionFlag, curPDP.invalidFlag, trackViewer.DrawPATfile.FileName);
            }
        }

        /// <summary>
        /// Add the FPS to the statusbar (Frames Per Second)
        /// </summary>
        private void AddFPS(RouteGenExtract trackViewer)
        {
            if (Properties.Settings.Default.statusShowFPS)
            {
                statusAdditional.Text += string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    " FPS={0:F1} ", trackViewer.FrameRate.SmoothedValue);
            }
        }

        /// <summary>
        /// Add information from terrain
        /// </summary>
        /// <param name="trackViewer"></param>
        private void AddTerrainStatus(RouteGenExtract trackViewer)
        {
            if (Properties.Settings.Default.statusShowTerrain && (trackViewer.drawTerrain != null))
            {
                statusAdditional.Text += trackViewer.drawTerrain.StatusInformation;
            }
        }

        /// <summary>
        /// Add information from signal
        /// </summary>
        /// <param name="trackViewer">The trackviewer we need to find the trackDB</param>
        /// <param name="description">The description of the item we might want to show, needed to make sure it is a proper item</param>
        /// <param name="index">The index of the item to show</param>
        private void AddSignalStatus(RouteGenExtract trackViewer, string description, uint index)
        {
            if (!Properties.Settings.Default.statusShowSignal) return;
            if (!String.Equals(description, "signal")) return;
            statusAdditional.Text += "signal shape = ";
            statusAdditional.Text += trackViewer.RouteData.GetSignalFilename(index);
        }

        /// <summary>
        /// Add information from platform and station name
        /// </summary>
        /// <param name="trackViewer">The trackviewer we need to find the trackDB</param>
        /// <param name="description">The description of the item we might want to show, needed to make sure it is a proper item</param>
        /// <param name="index">The index of the item to show</param>
        private void AddNamesStatus(RouteGenExtract trackViewer, string description, uint index)
        {
            if (!Properties.Settings.Default.statusShowNames) return;
            if (!String.Equals(description, "platform")) return;

            TrItem item = trackViewer.RouteData.TrackDB.TrItemTable[index];
            PlatformItem platform = item as PlatformItem;
            if (platform == null) return;
            statusAdditional.Text += string.Format(System.Globalization.CultureInfo.CurrentCulture,
                "{0} ({1})", platform.Station, platform.ItemName);
        }

        #region IDisposable
        private bool disposed;
        /// <summary>
        /// Implementing IDisposable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true; // to prevent infinite loop. Probably elementHost should not be part of this class
                if (disposing)
                {
                    // Dispose managed resources.
                    elementHost.Dispose();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;
        }
        #endregion
    }
}
