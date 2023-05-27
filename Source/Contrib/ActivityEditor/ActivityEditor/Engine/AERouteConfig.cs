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

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 


using System;
using System.Linq;
using System.Windows.Forms;
using Orts.Formats.Msts;
using Orts.Formats.OR;

namespace ActivityEditor.Engine
{
    public class AERouteConfig
    {
        public System.Windows.Forms.TableLayoutPanel MainPanel;
        public System.Windows.Forms.Label label;

        public ORRouteConfig orRouteConfig { get { return Viewer.Simulator.orRouteConfig; } protected set { } }
        public System.Windows.Forms.TableLayoutPanel RoutePanel;

        public System.Windows.Forms.FlowLayoutPanel TagPanel;
        public System.Windows.Forms.FlowLayoutPanel StationPanel;
        //  MSTS data
        public TrackNode[] nodes { get { return simulator.nodes; } set { } }
        AEConfig Parent;

        public TrackSectionsFile TSectionDat { get { return Viewer.Simulator.TSectionDat; } protected set { } }
        public Viewer2D Viewer { get { return Parent.Viewer; } protected set { } }
        public PseudoSim simulator { get { return Viewer.Simulator; } protected set { } }

        public AERouteConfig(AEConfig parent)
        {
            Parent = parent;
        }

        public string getRouteName()
        {
            return orRouteConfig.RouteName;
        }

        public void LoadPanels(string ident, GroupBox routeData)
        {
            // 
            // groupBox2
            // 
            string name = getRouteName();
            MainPanel = new System.Windows.Forms.TableLayoutPanel();
            routeData.Text = ident;
            // 
            // ActivityPanel
            // 
            MainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right
                        | System.Windows.Forms.AnchorStyles.Bottom)));
            MainPanel.AutoScroll = true;
            //MainPanel.AutoSize = true;
            MainPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;
            MainPanel.Location = new System.Drawing.Point(0, 15);
            MainPanel.Name = "MainPanel";
            MainPanel.Size = new System.Drawing.Size(190, routeData.Height - 10);
            MainPanel.TabIndex = 0;
            MainPanel.ColumnCount = 1;
            MainPanel.RowCount = 3;
            MainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            //MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            //MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            MainPanel.Visible = true;
            //
            //  Label
            //
            //label = new System.Windows.Forms.Label();
            //label.Location = new System.Drawing.Point(18, 113);
            //label.Name = "label";
            //label.Size = new System.Drawing.Size(150, 13);
            //label.AutoSize = false;
            //label.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left
            //    | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top));
            //label.TabIndex = 10;
            //label.Text = ident;
            //label.TextAlign = System.Drawing.ContentAlignment.TopCenter;


            TagPanel = new System.Windows.Forms.FlowLayoutPanel();
            TagPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Top));
            TagPanel.AutoScroll = true;
            TagPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            TagPanel.Location = new System.Drawing.Point(0, 4);
            TagPanel.Name = "Tag Panel";
            //TagPanel.Size = new System.Drawing.Size(MainPanel.Width - 20, (groupBox.Height/2) - 20);
            TagPanel.Height = (routeData.Height / 2) - 20;
            TagPanel.Width = MainPanel.Width - 15;
            //TagPanel.TabIndex = 2;
            TagPanel.FlowDirection = FlowDirection.TopDown;
            TagPanel.WrapContents = false;
            TagPanel.Visible = true;


            StationPanel = new System.Windows.Forms.FlowLayoutPanel();
            StationPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Top));
            StationPanel.AutoScroll = true;
            StationPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            //StationPanel.Location = new System.Drawing.Point(3, 170);
            StationPanel.Name = "Station Panel";
            //StationPanel.Size = new System.Drawing.Size(MainPanel.Width - 20, (groupBox.Height / 2) - 20);
            StationPanel.Height = (routeData.Height / 2) - 20;
            StationPanel.Width = MainPanel.Width - 15;
            //StationPanel.TabIndex = 2;
            StationPanel.FlowDirection = FlowDirection.TopDown;
            StationPanel.WrapContents = false;
            StationPanel.Visible = true;

            MainPanel.Controls.Add(TagPanel);
            MainPanel.Controls.Add(StationPanel);
            //groupBox.Controls.Add(label);
            routeData.Controls.Add(MainPanel);
            routeData.ResumeLayout(true);
        }


        public void AddTagPanel(System.Windows.Forms.GroupBox[] groupBoxList)
        {
            if (this.TagPanel.Controls != null)
                this.TagPanel.Controls.Clear();
            this.TagPanel.Controls.AddRange(groupBoxList);
        }

        public void AddStationPanel(System.Windows.Forms.GroupBox[] groupBoxList)
        {
            this.StationPanel.Controls.Clear();
            this.StationPanel.Controls.AddRange(groupBoxList);
        }

        public System.Drawing.Point GetTagPanelPosition()
        {
            System.Drawing.Point current = new System.Drawing.Point(TagPanel.AutoScrollPosition.X, TagPanel.AutoScrollPosition.Y);
            return current;
        }

        public System.Drawing.Point GetStationPanelPosition()
        {
            System.Drawing.Point current = new System.Drawing.Point(StationPanel.AutoScrollPosition.X, StationPanel.AutoScrollPosition.Y);
            return current;
        }

        public void SaveRoute()
        {
            orRouteConfig.SaveConfig();
        }

        public void CloseRoute()
        {
            SaveRoute();
        }

        private GlobalItem findItemFromMouse(int x, int y, int range)
        {
            if (range < 5) range = 5;
            double closest = float.NaN;
            if (orRouteConfig == null)
                return null;
            GlobalItem closestItem = null;
            foreach (var item in orRouteConfig.AllItems)
            {
                if (item.Location2D.X < x - range || item.Location2D.X > x + range
                   || item.Location2D.Y < y - range || item.Location2D.Y > y + range) continue;
                if (closestItem != null)
                {
                    var dist = Math.Pow(item.Location2D.X - closestItem.Location2D.X, 2) + Math.Pow(item.Location2D.Y - closestItem.Location2D.Y, 2);
                    if (dist < closest)
                    {
                        closest = dist; closestItem = item;
                    }
                }
                else closestItem = item;
            }
            if (closestItem != null)
            {
                return closestItem;
            }
#if ZORRO
            foreach (var item in switchItemsDrawn)
            {
                //if out of range, continue
                if (item.Location2D.X < x - range || item.Location2D.X > x + range
                   || item.Location2D.Y < y - range || item.Location2D.Y > y + range) continue;

                if (closestItem != null)
                {
                    var dist = Math.Pow(item.Location2D.X - closestItem.Location2D.X, 2) + Math.Pow(item.Location2D.Y - closestItem.Location2D.Y, 2);
                    if (dist < closest)
                    {
                        closest = dist; closestItem = item;
                    }
                }
                else closestItem = item;
            }
            foreach (var item in signalItemsDrawn)
            {
                //if out of range, continue
                if (item.Location2D.X < x - range || item.Location2D.X > x + range
                   || item.Location2D.Y < y - range || item.Location2D.Y > y + range) continue;

                if (closestItem != null)
                {
                    var dist = Math.Pow(item.Location2D.X - closestItem.Location2D.X, 2) + Math.Pow(item.Location2D.Y - closestItem.Location2D.Y, 2);
                    if (dist < closest)
                    {
                        closest = dist; closestItem = item;
                    }
                }
                else closestItem = item;
            }
                if (closestItem != null) { switchPickedTime = simulator.GameTime; return closestItem; }
            }

            //now check for trains (first car only)
            TrainCar firstCar;
            PickedTrain = null; float tX, tY;
            closest = 100f;

            foreach (var t in Program.Simulator.Trains)
            {
                firstCar = null;
                if (t.LeadLocomotive != null)
                {
                    worldPos = t.LeadLocomotive.WorldPosition;
                    firstCar = t.LeadLocomotive;
                }
                else if (t.Cars != null && t.Cars.Count > 0)
                {
                    worldPos = t.Cars[0].WorldPosition;
                    firstCar = t.Cars[0];

                }
                else continue;

                worldPos = firstCar.WorldPosition;
                tX = (worldPos.TileX * 2048 + worldPos.Location.X - subX) * xScale; tY = pictureBox1.Height - (worldPos.TileZ * 2048 + worldPos.Location.Z - subY) * yScale;

                if (tX < x - range || tX > x + range || tY < y - range || tY > y + range) continue;
                if (PickedTrain == null) PickedTrain = t;
            }
            //if a train is picked, will clear the avatar list selection
            if (PickedTrain != null)
            {
                AvatarView.SelectedItems.Clear();
                return new TrainWidget(PickedTrain);
            }
#endif
            return null;
        }

        public dVector getVectorNextNode(int nodeIdx, TrPin fromPin)
        {
            TrackNode toNext = nodes[fromPin.Link];
            if (nodeIdx > fromPin.Link && toNext.TrJunctionNode == null)
                return null;
            dVector nextVector;
            int direction = fromPin.Direction;
            TrVectorSection item;
            if (toNext.UiD == null)
            {
                if (direction == 1)
                {
                    item = toNext.TrVectorNode.TrVectorSections.First();
                }
                else
                {
                    item = toNext.TrVectorNode.TrVectorSections.Last();
                }
                nextVector = new dVector((item.TileX * 2048f) + item.X, (item.TileZ * 2048f) + item.Z);
            }
            else
            {
                nextVector = new dVector((toNext.UiD.TileX * 2048f) + toNext.UiD.X, (toNext.UiD.TileZ * 2048f) + toNext.UiD.Z);
            }

            return nextVector;
        }

    }
}