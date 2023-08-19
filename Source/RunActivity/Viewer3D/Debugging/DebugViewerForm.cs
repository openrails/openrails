// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

// Principal Author:
//     Author: Charlie Salts / Signalsoft Rail Consultancy Ltd.
// Contributor:
//    Richard Plokhaar / Signalsoft Rail Consultancy Ltd.
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using GNU.Gettext.WinForms;
using Microsoft.Xna.Framework;
using Orts.Formats.Msts;
using Orts.MultiPlayer;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.Signalling;
using Orts.Viewer3D.Popups;
using ORTS.Common;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;

namespace Orts.Viewer3D.Debugging
{
    /// <summary>
    /// Defines an external window for use as a debugging viewer 
    /// when using Open Rails 
    /// </summary>
    public partial class DispatchViewer : Form
    {
        /// <summary>
        /// Reference to the main simulator object.
        /// </summary>
        public readonly Simulator simulator;
        private TimetableWindow TimetableWindow;

        private int IM_Width = 720;
        private int IM_Height = 720;

        /// <summary>
        /// True when the user is dragging the route view
        /// </summary>
        public bool Dragging;
        private WorldPosition worldPos;
        public float xScale = 1; // pixels / metre
        public float yScale = 1; // pixels / metre 

        string name = "";
        public List<SwitchWidget> switchItemsDrawn;
        public List<SignalWidget> signalItemsDrawn;

        public SwitchWidget switchPickedItem;
        public SignalWidget signalPickedItem;
        public bool switchPickedItemHandled;
        public double switchPickedTime;
        public bool signalPickedItemHandled;
        public double signalPickedTime;
        public bool DrawPath = true; //draw train path
        ImageList imageList1;
        public List<Train> selectedTrainList;
        /// <summary>
        /// contains the last position of the mouse
        /// </summary>
        private System.Drawing.Point LastCursorPosition = new System.Drawing.Point();
        public Pen redPen = new Pen(Color.Red);
        public Pen greenPen = new Pen(Color.Green);
        public Pen orangePen = new Pen(Color.Orange);
        public Pen trainPen = new Pen(Color.DarkGreen);
        public Pen pathPen = new Pen(Color.DeepPink);
        public Pen grayPen = new Pen(Color.Gray);
        public Pen PlatformPen = new Pen(Color.Blue);

        //the train selected by leftclicking the mouse
        public Train PickedTrain;

        // Note +ve pixels draw down from top, but +ve metres draw up from the bottom
        //
        // |-------- subX ---------->|           ViewWindow                
        //                           +--------------------------------+             
        //                           |                                |      
        //                           |                                |           
        //                           |                                |         
        //                           |                                |       
        //                  ==========                                |                -----   
        //                  ===========\         Track Extent         |                   ^
        //                  ====================================================          |
        //                           |                       \        |                   |
        //                           |                        ==================   ----   |
        //                           |                                |               ^   |
        //                           |                                |               |
        //                           |                                |               | maxY
        //                           +--------------------------------+                
        //                                                                           minY |
        // |----- minX --->|<- VW.X->|<------------ VW.Width -------->|                   |
        //                                                                            |   |
        // |------------------------------ maxX ------------------------------->|     |   |
        //                                                                            |   |
        // + 0,0 World origin                                                     ----------

        /// <summary>
        /// Defines the area to view, in meters. The left edge is meters from the leftmost extent of the route.
        /// </summary>
        public RectangleF ViewWindow;

        /// <summary>
        /// Used to periodically check if we should shift the view when the
        /// user is holding down a "shift view" button.
        /// </summary>
        private Timer UITimer;

        bool loaded;
        TrackNode[] nodes;

        // Extents of the route in meters measured from the World origin
        public float minX = float.MaxValue;
        public float minY = float.MaxValue;
        public float maxX = float.MinValue;
        public float maxY = float.MinValue;

        public Viewer Viewer;
        /// <summary>
        /// Creates a new DebugViewerForm.
        /// </summary>
        /// <param name="simulator"></param>
        /// /// <param name="viewer"></param>
        public DispatchViewer(Simulator simulator, Viewer viewer)
        {
            InitializeComponent();

            if (simulator == null)
                throw new ArgumentNullException("simulator", "Simulator object cannot be null.");

            this.simulator = simulator;
            Viewer = viewer;
            TimetableWindow = new TimetableWindow(this);

            nodes = simulator.TDB.TrackDB.TrackNodes;
            if (MultiPlayer.MPManager.IsMultiPlayer()) { MultiPlayer.MPManager.AllowedManualSwitch = false; }

            // initialise the timer used to handle user input
            UITimer = new Timer();
            UITimer.Interval = 100;
            UITimer.Tick += new EventHandler(UITimer_Tick);
            UITimer.Start();
        }

        public int RedrawCount;
        public Font trainFont;
        public Font sidingFont;
        public Font PlatformFont;
        public Font SignalFont;
        public SolidBrush trainBrush;
        public SolidBrush sidingBrush;
        public SolidBrush PlatformBrush;
        public SolidBrush SignalBrush;
        public SolidBrush InactiveTrainBrush;

        private double lastUpdateTime;

        /// <summary>
        /// When the user holds down the  "L", "R", "U", "D" buttons,
        /// shift the view. Avoids the case when the user has to click
        /// buttons like crazy.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UITimer_Tick(object sender, EventArgs e)
        {
            if (Viewer.DebugViewerEnabled == false) // Ctrl+9 sets this true to initialise the window and make it visible
            {
                Visible = false;
                firstShow = true;
                return;
            }

            if (firstShow)
            {
                InitializeWindow();
            }
            Visible = true;

            if (Program.Simulator.GameTime - lastUpdateTime < 1)
                return;

            lastUpdateTime = Program.Simulator.GameTime;

            GenerateView();
        }

        private void InitializeWindow()
        {
            ViewWindow = new RectangleF(0, 0, 5000f, 5000f);
            windowSizeUpDown.Accelerations.Add(new NumericUpDownAcceleration(1, 100));
            boxSetSignal.Items.Add("System Controlled");
            boxSetSignal.Items.Add("Stop");
            boxSetSignal.Items.Add("Approach");
            boxSetSignal.Items.Add("Proceed");
            chkAllowUserSwitch.Checked = false;
            selectedTrainList = new List<Train>();

            InitData();
            InitImage();

            MPManager.Instance().ServerChanged += (sender, e) =>
            {
                firstShow = true;
            };

            MPManager.Instance().AvatarUpdated += (sender, e) =>
            {
                AddAvatar(e.User, e.URL);
            };

            MPManager.Instance().MessageReceived += (sender, e) =>
            {
                AddNewMessage(e.Time, e.Message);
            };

            tWindow.SelectedIndex = MPManager.IsMultiPlayer() ? 0 : 1;
            TimetableWindow.SetControls();
        }

        #region initData
        private void InitData()
        {
            if (!loaded)
            {
                // do this only once
                loaded = true;
                //trackSections.DataSource = new List<InterlockingTrack>(simulator.InterlockingSystem.Tracks.Values).ToArray();
                Localizer.Localize(this, Viewer.Catalog);
            }

            switchItemsDrawn = new List<SwitchWidget>();
            signalItemsDrawn = new List<SignalWidget>();
            switches = new List<SwitchWidget>();
            for (int i = 0; i < nodes.Length; i++)
            {
                TrackNode currNode = nodes[i];

                if (currNode != null)
                {

                    if (currNode.TrEndNode)
                    {
                        //buffers.Add(new PointF(currNode.UiD.TileX * 2048 + currNode.UiD.X, currNode.UiD.TileZ * 2048 + currNode.UiD.Z));
                    }
                    else if (currNode.TrVectorNode != null)
                    {

                        if (currNode.TrVectorNode.TrVectorSections.Length > 1)
                        {
                            AddSegments(segments, currNode, currNode.TrVectorNode.TrVectorSections, ref minX, ref minY, ref maxX, ref maxY, simulator);
                        }
                        else
                        {
                            TrVectorSection s = currNode.TrVectorNode.TrVectorSections[0];

                            foreach (TrPin pin in currNode.TrPins)
                            {

                                TrackNode connectedNode = nodes[pin.Link];

                                dVector A = new dVector(s.TileX, s.X, s.TileZ, +s.Z);
                                dVector B = new dVector(connectedNode.UiD.TileX, connectedNode.UiD.X, connectedNode.UiD.TileZ, connectedNode.UiD.Z);
                                segments.Add(new LineSegment(A, B, /*s.InterlockingTrack.IsOccupied*/ false, null));
                            }


                        }
                    }
                    else if (currNode.TrJunctionNode != null)
                    {
                        foreach (TrPin pin in currNode.TrPins)
                        {
                            var vectorSections = nodes[pin.Link]?.TrVectorNode?.TrVectorSections;
                            if (vectorSections == null || vectorSections.Length < 1)
                                continue;
                            TrVectorSection item = pin.Direction == 1 ? vectorSections.First() : vectorSections.Last();
                            dVector A = new dVector(currNode.UiD.TileX, currNode.UiD.X, currNode.UiD.TileZ, +currNode.UiD.Z);
                            dVector B = new dVector(item.TileX, +item.X, item.TileZ, +item.Z);
                            var x = dVector.DistanceSqr(A, B);
                            if (x < 0.1) continue;
                            segments.Add(new LineSegment(B, A, /*s.InterlockingTrack.IsOccupied*/ false, item));
                        }
                        switches.Add(new SwitchWidget(currNode));
                    }
                }
            }

            var maxsize = maxX - minX > maxY - minY ? maxX - minX : maxY - minY;
            // Take up to next 100
            maxsize = (int)(maxsize / 100 + 1) * 100;
            windowSizeUpDown.Maximum = (decimal)maxsize;
            Inited = true;

            if (simulator.TDB == null || simulator.TDB.TrackDB == null || simulator.TDB.TrackDB.TrItemTable == null)
                return;

            TimetableWindow.PopulateItemLists();
        }

        bool Inited;
        public List<LineSegment> segments = new List<LineSegment>();
        public List<SwitchWidget> switches;
        public List<SignalWidget> signals = new List<SignalWidget>();
        public List<SidingWidget> sidings = new List<SidingWidget>();
        public List<PlatformWidget> platforms = new List<PlatformWidget>();

        /// <summary>
        /// Initialises the picturebox and the image it contains. 
        /// </summary>
        public void InitImage()
        {
            pbCanvas.Width = IM_Width;
            pbCanvas.Height = IM_Height;

            if (pbCanvas.Image != null)
            {
                pbCanvas.Image.Dispose();
            }

            pbCanvas.Image = new Bitmap(pbCanvas.Width, pbCanvas.Height);
            imageList1 = new ImageList();
            AvatarView.View = View.LargeIcon;
            imageList1.ImageSize = new Size(64, 64);
            AvatarView.LargeImageList = imageList1;
        }
        #endregion

        #region avatar
        Dictionary<string, Image> avatarList;
        public void AddAvatar(string name, string url)
        {
            if (avatarList == null) avatarList = new Dictionary<string, Image>();
            bool FindDefault = false;
            try
            {
                if (Program.Simulator.Settings.ShowAvatar == false) throw new Exception();
                FindDefault = true;
                var request = WebRequest.Create(url);
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    Image newImage = Image.FromStream(stream);//Image.FromFile("C:\\test1.png");//
                    avatarList[name] = newImage;
                }
            }
            catch
            {
                if (FindDefault)
                {
                    byte[] imageBytes = Convert.FromBase64String(imagestring);
                    MemoryStream ms = new MemoryStream(imageBytes, 0,
                      imageBytes.Length);

                    // Convert byte[] to Image
                    ms.Write(imageBytes, 0, imageBytes.Length);
                    Image newImage = Image.FromStream(ms, true);
                    avatarList[name] = newImage;
                }
                else
                {
                    avatarList[name] = null;
                }
            }
        }

        int LostCount = 0;//how many players in the lost list (quit)
        public void CheckAvatar()
        {
            if (!MPManager.IsMultiPlayer() || MPManager.OnlineTrains == null || MPManager.OnlineTrains.Players == null) return;
            var player = MPManager.OnlineTrains.Players;
            var username = MPManager.GetUserName();
            player = player.Concat(MPManager.Instance().lostPlayer).ToDictionary(x => x.Key, x => x.Value);
            if (avatarList == null) avatarList = new Dictionary<string, Image>();
            if (avatarList.Count == player.Count + 1 && LostCount == MPManager.Instance().lostPlayer.Count) return;

            LostCount = MPManager.Instance().lostPlayer.Count;
            //add myself
            if (!avatarList.ContainsKey(username))
            {
                AddAvatar(username, Program.Simulator.Settings.AvatarURL);
            }

            foreach (var p in player)
            {
                if (avatarList.ContainsKey(p.Key)) continue;
                AddAvatar(p.Key, p.Value.url);
            }

            Dictionary<string, Image> tmplist = null;
            foreach (var a in avatarList)
            {
                if (player.ContainsKey(a.Key) || a.Key == username) continue;
                if (tmplist == null) tmplist = new Dictionary<string, Image>();
                tmplist.Add(a.Key, a.Value);
            }

            if (tmplist != null)
            {
                foreach (var t in tmplist) avatarList.Remove(t.Key);
            }
            imageList1.Images.Clear();
            AvatarView.Items.Clear();
            var i = 0;
            if (!Program.Simulator.Settings.ShowAvatar)
            {
                AvatarView.View = View.List;
                foreach (var pair in avatarList)
                {
                    if (pair.Key != username) continue;
                    AvatarView.Items.Add(pair.Key);
                }
                i = 1;
                foreach (var pair in avatarList)
                {
                    if (pair.Key == username)
                        continue;

                    if (MPManager.Instance().aiderList.Contains(pair.Key))
                    {
                        AvatarView.Items.Add(pair.Key + " (H)");
                    }
                    else if (MPManager.Instance().lostPlayer.ContainsKey(pair.Key))
                    {
                        AvatarView.Items.Add(pair.Key + " (Q)");
                    }
                    else
                    {
                        AvatarView.Items.Add(pair.Key);
                    }

                    i++;
                }
            }
            else
            {
                AvatarView.View = View.LargeIcon;
                AvatarView.LargeImageList = imageList1;
                foreach (var pair in avatarList)
                {
                    if (pair.Key != username) continue;

                    if (pair.Value == null) AvatarView.Items.Add(pair.Key).ImageIndex = -1;
                    else
                    {
                        AvatarView.Items.Add(pair.Key).ImageIndex = 0;
                        imageList1.Images.Add(pair.Value);
                    }
                }

                i = 1;
                foreach (var pair in avatarList)
                {
                    if (pair.Key == username) continue;
                    var text = pair.Key;
                    if (MPManager.Instance().aiderList.Contains(pair.Key)) text = pair.Key + " (H)";

                    if (pair.Value == null) AvatarView.Items.Add(name).ImageIndex = -1;
                    else
                    {
                        AvatarView.Items.Add(text).ImageIndex = i;
                        imageList1.Images.Add(pair.Value);
                        i++;
                    }
                }
            }
        }
        #endregion

        #region Draw
        public bool firstShow = true;
        public bool followTrain;
        public float subX, subY;
        public float oldWidth;
        public float oldHeight;

        //determine locations of buttons and boxes
        void DetermineLocations()
        {
            if (Height < 600 || Width < 800) return;
            if (oldHeight != Height || oldWidth != label1.Left)//use the label "Res" as anchor point to determine the picture size
            {
                oldWidth = label1.Left; oldHeight = Height;
                IM_Width = label1.Left - 20;
                IM_Height = Height - pbCanvas.Top;
                pbCanvas.Width = IM_Width;
                pbCanvas.Height = Height - pbCanvas.Top - 40;
                if (pbCanvas.Image != null)
                {
                    pbCanvas.Image.Dispose();
                }

                pbCanvas.Image = new Bitmap(pbCanvas.Width, pbCanvas.Height);

                if (btnAssist.Left - 10 < composeMSG.Right)
                {
                    var size = composeMSG.Width;
                    composeMSG.Left = msgAll.Left = msgSelected.Left = reply2Selected.Left = btnAssist.Left - 10 - size;
                    MSG.Width = messages.Width = composeMSG.Left - 20;
                }
                firstShow = true;
            }
        }

        /// <summary>
        /// Regenerates the 2D view. At the moment, examines the track network
        /// each time the view is drawn. Later, the traversal and drawing can be separated.
        /// </summary>
        public void GenerateView(bool dragging = false)
        {
            if (!Inited) return;

            if (tWindow.SelectedIndex == 1)
            {
                TimetableWindow.GenerateTimetableView(dragging);
                return;
            }

            if (pbCanvas.Image == null) InitImage();
            DetermineLocations();

            if (firstShow)
            {
                if (!MPManager.IsServer())
                {
                    chkAllowUserSwitch.Visible = false;
                    chkAllowUserSwitch.Checked = false;
                    rmvButton.Visible = false;
                    btnAssist.Visible = false;
                    btnNormal.Visible = false;
                    msgAll.Text = "MSG to Server";
                }
                else
                {
                    msgAll.Text = "MSG to All";
                }
                if (MPManager.IsServer())
                {
                    rmvButton.Visible = true;
                    chkAllowNew.Visible = true;
                    chkAllowUserSwitch.Visible = true;
                }
                else
                {
                    rmvButton.Visible = false;
                    chkAllowNew.Visible = false;
                    chkAllowUserSwitch.Visible = false;
                    chkBoxPenalty.Visible = false;
                    chkPreferGreen.Visible = false;
                }
            }
            if (firstShow || followTrain)
            {
                //see who should I look at:
                //if the player is selected in the avatar list, show the player, otherwise, show the one with the lowest index
                WorldPosition pos = Program.Simulator.PlayerLocomotive == null ? Program.Simulator.Trains.First().Cars.First().WorldPosition : Program.Simulator.PlayerLocomotive.WorldPosition;
                if (AvatarView.SelectedIndices.Count > 0 && !AvatarView.SelectedIndices.Contains(0))
                {
                    int i = AvatarView.SelectedIndices.Cast<int>().Min();
                    string name = (AvatarView.Items[i].Text ?? "").Split(' ').First().Trim();
                    if (MPManager.OnlineTrains.Players.TryGetValue(name, out OnlinePlayer player))
                        pos = player?.Train?.Cars?.FirstOrDefault()?.WorldPosition;
                    else if (MPManager.Instance().lostPlayer.TryGetValue(name, out OnlinePlayer lost))
                        pos = lost?.Train?.Cars?.FirstOrDefault()?.WorldPosition;
                }
                if (pos == null)
                    pos = PickedTrain?.Cars?.FirstOrDefault()?.WorldPosition;
                if (pos != null)
                {
                    var ploc = new PointF(pos.TileX * 2048 + pos.Location.X, pos.TileZ * 2048 + pos.Location.Z);
                    ViewWindow.X = ploc.X - minX - ViewWindow.Width / 2; ViewWindow.Y = ploc.Y - minY - ViewWindow.Width / 2;
                    firstShow = false;
                }
            }

            try
            {
                CheckAvatar();
            }
            catch { } //errors for avatar, just ignore
            using (Graphics g = Graphics.FromImage(pbCanvas.Image))
            {
                subX = minX + ViewWindow.X; subY = minY + ViewWindow.Y;
                g.Clear(Color.White);

                xScale = pbCanvas.Width / ViewWindow.Width;
                yScale = pbCanvas.Height / ViewWindow.Height;

                PointF[] points = new PointF[3];
                Pen p = grayPen;

                p.Width = MathHelper.Clamp(xScale, 1, 3);
                greenPen.Width = orangePen.Width = redPen.Width = p.Width; pathPen.Width = 2 * p.Width;
                trainPen.Width = p.Width * 6;
                var forwardDist = 100 / xScale; if (forwardDist < 5) forwardDist = 5;

                PointF scaledA = new PointF(0, 0);
                PointF scaledB = new PointF(0, 0);
                PointF scaledC = new PointF(0, 0);

                foreach (var line in segments)
                {

                    scaledA.X = (line.A.TileX * 2048 - subX + (float)line.A.X) * xScale; scaledA.Y = pbCanvas.Height - (line.A.TileZ * 2048 - subY + (float)line.A.Z) * yScale;
                    scaledB.X = (line.B.TileX * 2048 - subX + (float)line.B.X) * xScale; scaledB.Y = pbCanvas.Height - (line.B.TileZ * 2048 - subY + (float)line.B.Z) * yScale;

                    if ((scaledA.X < 0 && scaledB.X < 0) || (scaledA.X > IM_Width && scaledB.X > IM_Width) || (scaledA.Y > IM_Height && scaledB.Y > IM_Height) || (scaledA.Y < 0 && scaledB.Y < 0))
                        continue;

                    if (line.isCurved == true)
                    {
                        scaledC.X = ((float)line.C.X - subX) * xScale; scaledC.Y = pbCanvas.Height - ((float)line.C.Z - subY) * yScale;
                        points[0] = scaledA; points[1] = scaledC; points[2] = scaledB;
                        g.DrawCurve(p, points);
                    }
                    else
                    {
                        g.DrawLine(p, scaledA, scaledB);
                    }
                }

                switchItemsDrawn.Clear();
                signalItemsDrawn.Clear();
                float x, y;
                PointF scaledItem = new PointF(0f, 0f);
                var width = 6f * p.Width; if (width > 15) width = 15;//not to make it too large
                for (var i = 0; i < switches.Count; i++)
                {
                    SwitchWidget sw = switches[i];

                    x = (sw.Location.X - subX) * xScale; y = pbCanvas.Height - (sw.Location.Y - subY) * yScale;

                    if (x < 0 || x > IM_Width || y > IM_Height || y < 0) continue;

                    scaledItem.X = x; scaledItem.Y = y;


                    if (sw.Item.TrJunctionNode.SelectedRoute == sw.main) g.FillEllipse(Brushes.Black, GetRect(scaledItem, width));
                    else g.FillEllipse(Brushes.Gray, GetRect(scaledItem, width));

                    sw.Location2D.X = scaledItem.X; sw.Location2D.Y = scaledItem.Y;
                    switchItemsDrawn.Add(sw);
                }

                foreach (var s in signals)
                {
                    if (float.IsNaN(s.Location.X) || float.IsNaN(s.Location.Y)) continue;
                    x = (s.Location.X - subX) * xScale; y = pbCanvas.Height - (s.Location.Y - subY) * yScale;
                    if (x < 0 || x > IM_Width || y > IM_Height || y < 0) continue;
                    scaledItem.X = x; scaledItem.Y = y;
                    s.Location2D.X = scaledItem.X; s.Location2D.Y = scaledItem.Y;
                    if (s.Signal.isSignalNormal())//only show nor
                    {
                        var color = Brushes.Green;
                        var pen = greenPen;
                        if (s.IsProceed == 0)
                        {
                        }
                        else if (s.IsProceed == 1)
                        {
                            color = Brushes.Orange;
                            pen = orangePen;
                        }
                        else
                        {
                            color = Brushes.Red;
                            pen = redPen;
                        }
                        g.FillEllipse(color, GetRect(scaledItem, width));
                        signalItemsDrawn.Add(s);
                        if (s.hasDir)
                        {
                            scaledB.X = (s.Dir.X - subX) * xScale; scaledB.Y = pbCanvas.Height - (s.Dir.Y - subY) * yScale;
                            g.DrawLine(pen, scaledItem, scaledB);
                        }
                    }
                }

                if (true/*showPlayerTrain.Checked*/)
                {
                    CleanVerticalCells();//clean the drawing area for text of sidings and platforms
                    foreach (var sw in sidings)
                        scaledItem = DrawSiding(g, scaledItem, sw);
                    foreach (var pw in platforms)
                        scaledItem = DrawPlatform(g, scaledItem, pw);

                    var margin = 30 * xScale;//margins to determine if we want to draw a train
                    var margin2 = 5000 * xScale;

                    //variable for drawing train path
                    var mDist = 5000f; var pDist = 50; //segment length when draw path

                    selectedTrainList.Clear();
                    foreach (var t in simulator.Trains) selectedTrainList.Add(t);

                    var redTrain = selectedTrainList.Count;

                    //choosen trains will be drawn later using blue, so it will overlap on the red lines
                    var chosen = AvatarView.SelectedItems;
                    if (chosen.Count > 0)
                    {
                        for (var i = 0; i < chosen.Count; i++)
                        {
                            var name = chosen[i].Text.Split(' ')[0].Trim(); //filter out (H) in the text
                            var train = MPManager.OnlineTrains.findTrain(name);
                            if (train != null) { selectedTrainList.Remove(train); selectedTrainList.Add(train); redTrain--; }
                            //if selected include myself, will show it as blue
                            if (MPManager.GetUserName() == name && Program.Simulator.PlayerLocomotive != null)
                            {
                                selectedTrainList.Remove(Program.Simulator.PlayerLocomotive.Train); selectedTrainList.Add(Program.Simulator.PlayerLocomotive.Train);
                                redTrain--;
                            }

                        }
                    }

                    //trains selected in the avatar view list will be drawn in blue, others will be drawn in red
                    pathPen.Color = Color.Red;
                    var drawRed = 0;
                    int ValidTrain = selectedTrainList.Count();
                    //add trains quit into the end, will draw them in gray
                    var quitTrains = MPManager.Instance().lostPlayer.Values
                        .Select((OnlinePlayer lost) => lost?.Train)
                        .Where((Train t) => t != null)
                        .Where((Train t) => !selectedTrainList.Contains(t));
                    selectedTrainList.AddRange(quitTrains);
                    foreach (Train t in selectedTrainList)
                    {
                        drawRed++;//how many red has been drawn
                        if (drawRed > redTrain) pathPen.Color = Color.Blue; //more than the red should be drawn, thus draw in blue

                        name = "";
                        TrainCar firstCar = null;
                        if (t.LeadLocomotive != null)
                        {
                            worldPos = t.LeadLocomotive.WorldPosition;
                            name = t.GetTrainName(t.LeadLocomotive.CarID);
                            firstCar = t.LeadLocomotive;
                        }
                        else if (t.Cars != null && t.Cars.Count > 0)
                        {
                            worldPos = t.Cars[0].WorldPosition;
                            name = t.GetTrainName(t.Cars[0].CarID);
                            if (t.TrainType == Train.TRAINTYPE.AI)
                                name = t.Number.ToString() + ":" + t.Name;
                            firstCar = t.Cars[0];
                        }
                        else continue;

                        if (xScale < 0.3 || t.FrontTDBTraveller == null || t.RearTDBTraveller == null)
                        {
                            worldPos = firstCar.WorldPosition;
                            scaledItem.X = (worldPos.TileX * 2048 - subX + worldPos.Location.X) * xScale;
                            scaledItem.Y = pbCanvas.Height - (worldPos.TileZ * 2048 - subY + worldPos.Location.Z) * yScale;
                            if (scaledItem.X < -margin2 || scaledItem.X > IM_Width + margin2 || scaledItem.Y > IM_Height + margin2 || scaledItem.Y < -margin2) continue;
                            if (drawRed > ValidTrain) g.FillRectangle(Brushes.Gray, GetRect(scaledItem, 15f));
                            else
                            {
                                if (t == PickedTrain) g.FillRectangle(Brushes.Red, GetRect(scaledItem, 15f));
                                else g.FillRectangle(Brushes.DarkGreen, GetRect(scaledItem, 15f));
                                scaledItem.Y -= 25;
                                DrawTrainPath(t, subX, subY, pathPen, g, scaledA, scaledB, pDist, mDist);
                            }
                            g.DrawString(name, trainFont, trainBrush, scaledItem);
                            continue;
                        }
                        var loc = t.FrontTDBTraveller.WorldLocation;
                        x = (loc.TileX * 2048 + loc.Location.X - subX) * xScale; y = pbCanvas.Height - (loc.TileZ * 2048 + loc.Location.Z - subY) * yScale;
                        if (x < -margin2 || x > IM_Width + margin2 || y > IM_Height + margin2 || y < -margin2) continue;

                        //train quit will not draw path, others will draw it
                        if (drawRed <= ValidTrain) DrawTrainPath(t, subX, subY, pathPen, g, scaledA, scaledB, pDist, mDist);

                        trainPen.Color = Color.DarkGreen;
                        foreach (var car in t.Cars)
                        {
                            Traveller t1 = new Traveller(t.RearTDBTraveller);
                            worldPos = car.WorldPosition;
                            var dist = t1.DistanceTo(worldPos.WorldLocation.TileX, worldPos.WorldLocation.TileZ, worldPos.WorldLocation.Location.X, worldPos.WorldLocation.Location.Y, worldPos.WorldLocation.Location.Z);
                            if (dist > 0)
                            {
                                t1.Move(dist - 1 + car.CarLengthM / 2);
                                x = (t1.TileX * 2048 + t1.Location.X - subX) * xScale; y = pbCanvas.Height - (t1.TileZ * 2048 + t1.Location.Z - subY) * yScale;
                                //x = (worldPos.TileX * 2048 + worldPos.Location.X - minX - ViewWindow.X) * xScale; y = pictureBox1.Height - (worldPos.TileZ * 2048 + worldPos.Location.Z - minY - ViewWindow.Y) * yScale;
                                if (x < -margin || x > IM_Width + margin || y > IM_Height + margin || y < -margin) continue;

                                scaledItem.X = x; scaledItem.Y = y;

                                t1.Move(-car.CarLengthM);
                                x = (t1.TileX * 2048 + t1.Location.X - subX) * xScale; y = pbCanvas.Height - (t1.TileZ * 2048 + t1.Location.Z - subY) * yScale;
                                if (x < -margin || x > IM_Width + margin || y > IM_Height + margin || y < -margin) continue;

                                scaledA.X = x; scaledA.Y = y;

                                //if the train has quit, will draw in gray, if the train is selected by left click of the mouse, will draw it in red
                                if (drawRed > ValidTrain) trainPen.Color = Color.Gray;
                                else if (t == PickedTrain) trainPen.Color = Color.Red;
                                g.DrawLine(trainPen, scaledA, scaledItem);
                            }
                        }
                        worldPos = firstCar.WorldPosition;
                        scaledItem.X = (worldPos.TileX * 2048 - subX + worldPos.Location.X) * xScale;
                        scaledItem.Y = -25 + pbCanvas.Height - (worldPos.TileZ * 2048 - subY + worldPos.Location.Z) * yScale;

                        g.DrawString(name, trainFont, trainBrush, scaledItem);

                    }
                    if (switchPickedItemHandled)
                        switchPickedItem = null;
                    if (signalPickedItemHandled)
                        signalPickedItem = null;
                }
            }

            pbCanvas.Invalidate();
        }

        private PointF DrawSiding(Graphics g, PointF scaledItem, SidingWidget s)
        {
            scaledItem.X = (s.Location.X - subX) * xScale;
            scaledItem.Y = DetermineSidingLocation(scaledItem.X, pbCanvas.Height - (s.Location.Y - subY) * yScale, s.Name);
            if (scaledItem.Y >= 0f) //if we need to draw the siding names
            {
                g.DrawString(s.Name, sidingFont, sidingBrush, scaledItem);
            }
            return scaledItem;
        }

        private PointF DrawPlatform(Graphics g, PointF scaledItem, PlatformWidget s)
        {
            scaledItem.X = (s.Location.X - subX) * xScale;
            scaledItem.Y = DetermineSidingLocation(scaledItem.X, pbCanvas.Height - (s.Location.Y - subY) * yScale, s.Name);
            if (scaledItem.Y >= 0f) //if we need to draw the siding names
            {
                g.DrawString(s.Name, sidingFont, sidingBrush, scaledItem);
            }
            return scaledItem;
        }

        public Vector2[][] alignedTextY;
        public int[] alignedTextNum;
        public const int spacing = 12;
        private void CleanVerticalCells()
        {
            if (alignedTextY == null || alignedTextY.Length != IM_Height / spacing) //first time to put text, or the text height has changed
            {
                alignedTextY = new Vector2[IM_Height / spacing][];
                alignedTextNum = new int[IM_Height / spacing];
                for (var i = 0; i < IM_Height / spacing; i++)
                    alignedTextY[i] = new Vector2[4]; //each line has at most 4 sidings
            }
            for (var i = 0; i < IM_Height / spacing; i++)
            {
                alignedTextNum[i] = 0;
            }
        }

        private float DetermineSidingLocation(float startX, float wantY, string name)
        {
            //out of drawing area
            if (startX < -64 || startX > IM_Width || wantY < -spacing || wantY > IM_Height) return -1f;

            int position = (int)(wantY / spacing);//the cell of the text it wants in
            if (position > alignedTextY.Length) return wantY;//position is larger than the number of cells
            var endX = startX + name.Length * trainFont.Size;
            int desiredPosition = position;
            while (position < alignedTextY.Length && position >= 0)
            {
                //if the line contains no text yet, put it there
                if (alignedTextNum[position] == 0)
                {
                    alignedTextY[position][alignedTextNum[position]].X = startX;
                    alignedTextY[position][alignedTextNum[position]].Y = endX;//add info for the text (i.e. start and end location)
                    alignedTextNum[position]++;
                    return position * spacing;
                }

                bool conflict = false;
                //check if it is intersect any one in the cell
                foreach (Vector2 v in alignedTextY[position])
                {
                    //check conflict with a text, v.x is the start of the text, v.y is the end of the text
                    if ((startX > v.X && startX < v.Y) || (endX > v.X && endX < v.Y) || (v.X > startX && v.X < endX) || (v.Y > startX && v.Y < endX))
                    {
                        conflict = true;
                        break;
                    }
                }
                if (conflict == false) //no conflict
                {
                    if (alignedTextNum[position] >= alignedTextY[position].Length) return -1f;
                    alignedTextY[position][alignedTextNum[position]].X = startX;
                    alignedTextY[position][alignedTextNum[position]].Y = endX;//add info for the text (i.e. start and end location)
                    alignedTextNum[position]++;
                    return position * spacing;
                }
                position--;
                //cannot move up, then try to move it down
                if (position - desiredPosition < -1)
                {
                    position = desiredPosition + 2;
                }
                //could not find any position up or down, just return negative
                if (position == desiredPosition) return -1f;
            }
            return position * spacing;
        }

        const float SignalErrorDistance = 100;
        const float SignalWarningDistance = 500;
        const float DisplayDistance = 1000;
        const float DisplaySegmentLength = 10;
        const float MaximumSectionDistance = 10000;

        Dictionary<int, SignallingDebugWindow.TrackSectionCacheEntry> Cache = new Dictionary<int, SignallingDebugWindow.TrackSectionCacheEntry>();
        SignallingDebugWindow.TrackSectionCacheEntry GetCacheEntry(Traveller position)
        {
            SignallingDebugWindow.TrackSectionCacheEntry rv;
            if (Cache.TryGetValue(position.TrackNodeIndex, out rv) && (rv.Direction == position.Direction))
                return rv;
            Cache[position.TrackNodeIndex] = rv = new SignallingDebugWindow.TrackSectionCacheEntry()
            {
                Direction = position.Direction,
                Length = 0,
                Objects = new List<SignallingDebugWindow.TrackSectionObject>(),
            };
            var nodeIndex = position.TrackNodeIndex;
            var trackNode = new Traveller(position);
            while (true)
            {
                rv.Length += MaximumSectionDistance - trackNode.MoveInSection(MaximumSectionDistance);
                if (!trackNode.NextSection())
                    break;
                if (trackNode.IsEnd)
                    rv.Objects.Add(new SignallingDebugWindow.TrackSectionEndOfLine() { Distance = rv.Length });
                else if (trackNode.IsJunction)
                    rv.Objects.Add(new SignallingDebugWindow.TrackSectionSwitch() { Distance = rv.Length, TrackNode = trackNode.TN, NodeIndex = nodeIndex });
                else
                    rv.Objects.Add(new SignallingDebugWindow.TrackSectionObject() { Distance = rv.Length }); // Always have an object at the end.
                if (trackNode.TrackNodeIndex != nodeIndex)
                    break;
            }
            trackNode = new Traveller(position);

            rv.Objects = rv.Objects.OrderBy(tso => tso.Distance).ToList();
            return rv;
        }

        //draw the train path if it is within the window
        public void DrawTrainPath(Train train, float subX, float subY, Pen pathPen, Graphics g, PointF scaledA, PointF scaledB, float stepDist, float MaximumSectionDistance)
        {
            if (DrawPath != true) return;
            bool ok = false;
            if (train == Program.Simulator.PlayerLocomotive.Train) ok = true;
            if (MPManager.IsMultiPlayer())
            {
                if (MPManager.OnlineTrains.findTrain(train))
                    ok = true;
            }
            if (train.FirstCar != null & train.FirstCar.CarID.Contains("AI")) // AI train
                ok = true;
            if (Math.Abs(train.SpeedMpS) > 0.001)
                ok = true;

            if (ok == false) return;

            var DisplayDistance = MaximumSectionDistance;
            var position = train.MUDirection != Direction.Reverse ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
            var caches = new List<SignallingDebugWindow.TrackSectionCacheEntry>();
            // Work backwards until we end up on a different track section.
            var cacheNode = new Traveller(position);
            cacheNode.ReverseDirection();
            var initialNodeOffsetCount = 0;
            while (cacheNode.TrackNodeIndex == position.TrackNodeIndex && cacheNode.NextSection())
                initialNodeOffsetCount++;
            // Now do it again, but don't go the last track section (because it is from a different track node).
            cacheNode = new Traveller(position);
            cacheNode.ReverseDirection();
            for (var i = 1; i < initialNodeOffsetCount; i++)
                cacheNode.NextSection();
            // Push the location right up to the end of the section.
            cacheNode.MoveInSection(MaximumSectionDistance);
            // Now back facing the right way, calculate the distance to the train location.
            cacheNode.ReverseDirection();
            var initialNodeOffset = cacheNode.DistanceTo(position.TileX, position.TileZ, position.X, position.Y, position.Z);
            // Go and collect all the cache entries for the visible range of vector nodes (straights, curves).
            var totalDistance = 0f;
            while (!cacheNode.IsEnd && totalDistance - initialNodeOffset < DisplayDistance)
            {
                if (cacheNode.IsTrack)
                {
                    var cache = GetCacheEntry(cacheNode);
                    cache.Age = 0;
                    caches.Add(cache);
                    totalDistance += cache.Length;
                }
                var nodeIndex = cacheNode.TrackNodeIndex;
                while (cacheNode.TrackNodeIndex == nodeIndex && cacheNode.NextSection()) ;
            }

            var switchErrorDistance = initialNodeOffset + DisplayDistance + SignalWarningDistance;
            var signalErrorDistance = initialNodeOffset + DisplayDistance + SignalWarningDistance;
            var currentDistance = 0f;
            foreach (var cache in caches)
            {
                foreach (var obj in cache.Objects)
                {
                    var objDistance = currentDistance + obj.Distance;
                    if (objDistance < initialNodeOffset)
                        continue;

                    var switchObj = obj as SignallingDebugWindow.TrackSectionSwitch;
                    if (switchObj != null)
                    {
                        for (var pin = switchObj.TrackNode.Inpins; pin < switchObj.TrackNode.Inpins + switchObj.TrackNode.Outpins; pin++)
                        {
                            if (switchObj.TrackNode.TrPins[pin].Link == switchObj.NodeIndex)
                            {
                                if (pin - switchObj.TrackNode.Inpins != switchObj.TrackNode.TrJunctionNode.SelectedRoute)
                                    switchErrorDistance = objDistance;
                                break;
                            }
                        }
                        if (switchErrorDistance < DisplayDistance)
                            break;
                    }

                }
                if (switchErrorDistance < DisplayDistance || signalErrorDistance < DisplayDistance)
                    break;
                currentDistance += cache.Length;
            }

            var currentPosition = new Traveller(position);
            currentPosition.Move(-initialNodeOffset);
            currentDistance = 0;

            foreach (var cache in caches)
            {
                var lastObjDistance = 0f;
                foreach (var obj in cache.Objects)
                {
                    var objDistance = currentDistance + obj.Distance;

                    for (var step = lastObjDistance; step < obj.Distance; step += DisplaySegmentLength)
                    {
                        var stepDistance = currentDistance + step;
                        var stepLength = DisplaySegmentLength > obj.Distance - step ? obj.Distance - step : DisplaySegmentLength;
                        var previousLocation = currentPosition.WorldLocation;
                        currentPosition.Move(stepLength);
                        if (stepDistance + stepLength >= initialNodeOffset && stepDistance <= initialNodeOffset + DisplayDistance)
                        {
                            var currentLocation = currentPosition.WorldLocation;
                            scaledA.X = (float)((previousLocation.TileX * WorldLocation.TileSize + previousLocation.Location.X - subX) * xScale); 
                            scaledA.Y = (float)(pbCanvas.Height - (previousLocation.TileZ * WorldLocation.TileSize + previousLocation.Location.Z - subY) * yScale);
                            scaledB.X = (float)((currentLocation.TileX * WorldLocation.TileSize + currentLocation.Location.X - subX) * xScale); 
                            scaledB.Y = (float)(pbCanvas.Height - (currentPosition.TileZ * WorldLocation.TileSize + currentPosition.Location.Z - subY) * yScale); g.DrawLine(pathPen, scaledA, scaledB);
                        }
                    }
                    lastObjDistance = obj.Distance;

                    if (objDistance >= switchErrorDistance)
                        break;
                }
                currentDistance += cache.Length;
                if (currentDistance >= switchErrorDistance)
                    break;

            }

            currentPosition = new Traveller(position);
            currentPosition.Move(-initialNodeOffset);
            currentDistance = 0;
            foreach (var cache in caches)
            {
                var lastObjDistance = 0f;
                foreach (var obj in cache.Objects)
                {
                    currentPosition.Move(obj.Distance - lastObjDistance);
                    lastObjDistance = obj.Distance;

                    var objDistance = currentDistance + obj.Distance;
                    if (objDistance < initialNodeOffset || objDistance > initialNodeOffset + DisplayDistance)
                        continue;

                    var switchObj = obj as SignallingDebugWindow.TrackSectionSwitch;
                    if (switchObj != null)
                    {
                        for (var pin = switchObj.TrackNode.Inpins; pin < switchObj.TrackNode.Inpins + switchObj.TrackNode.Outpins; pin++)
                        {
                            if (switchObj.TrackNode.TrPins[pin].Link == switchObj.NodeIndex && pin - switchObj.TrackNode.Inpins != switchObj.TrackNode.TrJunctionNode.SelectedRoute)
                            {
                                foreach (var sw in switchItemsDrawn)
                                {
                                    if (sw.Item.TrJunctionNode == switchObj.TrackNode.TrJunctionNode)
                                    {
                                        var r = 6 * greenPen.Width;
                                        g.DrawLine(pathPen, new PointF(sw.Location2D.X - r, sw.Location2D.Y - r), new PointF(sw.Location2D.X + r, sw.Location2D.Y + r));
                                        g.DrawLine(pathPen, new PointF(sw.Location2D.X - r, sw.Location2D.Y + r), new PointF(sw.Location2D.X + r, sw.Location2D.Y - r));
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (objDistance >= switchErrorDistance)
                        break;
                }
                currentDistance += cache.Length;
                if (currentDistance >= switchErrorDistance)
                    break;
            }
            // Clean up any cache entries who haven't been using for 30 seconds.
            var oldCaches = Cache.Where(kvp => kvp.Value.Age > 30 * 4).ToArray();
            foreach (var oldCache in oldCaches)
                Cache.Remove(oldCache.Key);

        }
        #endregion

        /// <summary>
        /// Generates a rectangle representing a dot being drawn.
        /// </summary>
        /// <param name="p">Center point of the dot, in pixels.</param>
        /// <param name="size">Size of the dot's diameter, in pixels</param>
        /// <returns></returns>
        static public RectangleF GetRect(PointF p, float size)
        {
            return new RectangleF(p.X - size / 2f, p.Y - size / 2f, size, size);
        }

        /// <summary>
        /// Generates line segments from an array of TrVectorSection. Also computes 
        /// the bounds of the entire route being drawn.
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="items"></param>
        /// <param name="minX"></param>
        /// <param name="minY"></param>
        /// <param name="maxX"></param>
        /// <param name="maxY"></param>
        /// <param name="simulator"></param>
        private static void AddSegments(List<LineSegment> segments, TrackNode node, TrVectorSection[] items, ref float minX, ref float minY, ref float maxX, ref float maxY, Simulator simulator)
        {

            bool occupied = false;

            double tempX1, tempX2, tempZ1, tempZ2;

            for (int i = 0; i < items.Length - 1; i++)
            {
                dVector A = new dVector(items[i].TileX, items[i].X, items[i].TileZ, items[i].Z);
                dVector B = new dVector(items[i + 1].TileX, items[i + 1].X, items[i + 1].TileZ, items[i + 1].Z);

                tempX1 = A.TileX * 2048 + A.X; tempX2 = B.TileX * 2048 + B.X;
                tempZ1 = A.TileZ * 2048 + A.Z; tempZ2 = B.TileZ * 2048 + B.Z;
                CalcBounds(ref maxX, tempX1, true);
                CalcBounds(ref maxY, tempZ1, true);
                CalcBounds(ref maxX, tempX2, true);
                CalcBounds(ref maxY, tempZ2, true);

                CalcBounds(ref minX, tempX1, false);
                CalcBounds(ref minY, tempZ1, false);
                CalcBounds(ref minX, tempX2, false);
                CalcBounds(ref minY, tempZ2, false);

                segments.Add(new LineSegment(A, B, occupied, items[i]));
            }
        }

        /// <summary>
        /// Given a value representing a limit, evaluate if the given value exceeds the current limit.
        /// If so, expand the limit.
        /// </summary>
        /// <param name="limit">The current limit.</param>
        /// <param name="value">The value to compare the limit to.</param>
        /// <param name="gt">True when comparison is greater-than. False if less-than.</param>
        private static void CalcBounds(ref float limit, double v, bool gt)
        {
            float value = (float)v;
            if (gt)
            {
                if (value > limit)
                {
                    limit = value;
                }
            }
            else
            {
                if (value < limit)
                {
                    limit = value;
                }
            }
        }

        private float ScrollSpeedX
        {
            get
            {
                return ViewWindow.Width * 0.10f;
            }
        }

        private float ScrollSpeedY
        {
            get
            {
                return ViewWindow.Width * 0.10f;
            }
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            followTrain = false;
            firstShow = true;
            GenerateView();
        }

        private void ShiftViewUp()
        {
            ViewWindow.Offset(0, -ScrollSpeedY);

            GenerateView();
        }
        private void ShiftViewDown()
        {
            ViewWindow.Offset(0, ScrollSpeedY);

            GenerateView();
        }

        private void ShiftViewRight()
        {
            ViewWindow.Offset(ScrollSpeedX, 0);

            GenerateView();
        }


        private void ShiftViewLeft()
        {
            ViewWindow.Offset(-ScrollSpeedX, 0);

            GenerateView();
        }

        private void rmvButton_Click(object sender, EventArgs e)
        {
            if (!MPManager.IsServer()) return;
            AvatarView.SelectedIndices.Remove(0);//remove myself is not possible.
            var chosen = AvatarView.SelectedItems;
            if (chosen.Count > 0)
            {
                for (var i = 0; i < chosen.Count; i++)
                {
                    var tmp = chosen[i];
                    var name = tmp.Text.Split(' ')[0];//the name may have (H) in it, need to filter that out
                    if (MPManager.OnlineTrains.Players.ContainsKey(name))
                    {
                        MPManager.OnlineTrains.Players[name].status = OnlinePlayer.Status.Removed;
                        MPManager.BroadCast(new MSGMessage(name, "Error", "Sorry the server has removed you").ToString());
                    }
                }
            }
        }


        private void windowSizeUpDown_ValueChanged(object sender, EventArgs e)
        {
            // this is the center, before increasing the size
            PointF center = new PointF(ViewWindow.X + ViewWindow.Width / 2f, ViewWindow.Y + ViewWindow.Height / 2f);

            float newSizeH = (float)windowSizeUpDown.Value;
            float verticalByHorizontal = ViewWindow.Height / ViewWindow.Width;
            float newSizeV = newSizeH * verticalByHorizontal;

            ViewWindow = new RectangleF(center.X - newSizeH / 2f, center.Y - newSizeV / 2f, newSizeH, newSizeV);

            GenerateView();
        }


        protected override void OnMouseWheel(MouseEventArgs e)
        {
            decimal tempValue = windowSizeUpDown.Value;
            if (e.Delta < 0) tempValue /= 0.95m;
            else if (e.Delta > 0) tempValue *= 0.95m;
            else return;

            if (tempValue < windowSizeUpDown.Minimum) tempValue = windowSizeUpDown.Minimum;
            if (tempValue > windowSizeUpDown.Maximum) tempValue = windowSizeUpDown.Maximum;
            windowSizeUpDown.Value = tempValue;
        }

        private bool Zooming;
        private bool LeftClick;
        private bool RightClick;

        private void pictureBoxMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) LeftClick = true;
            if (e.Button == MouseButtons.Right) RightClick = true;

            if (LeftClick == true && RightClick == false)
            {
                if (Dragging == false)
                {
                    Dragging = true;
                }
            }
            else if (LeftClick == true && RightClick == true)
            {
                if (Zooming == false) Zooming = true;
            }
            LastCursorPosition.X = e.X;
            LastCursorPosition.Y = e.Y;
            MPManager.Instance().ComposingText = false;
            lblInstruction1.Visible = true;
            lblInstruction2.Visible = true;
            lblInstruction3.Visible = true;
            lblInstruction4.Visible = true;
        }

        private void pictureBoxMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) LeftClick = false;
            if (e.Button == MouseButtons.Right) RightClick = false;

            if (LeftClick == false)
            {
                Dragging = false;
                Zooming = false;
            }

            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                PictureMoveAndZoomInOut(e.X, e.Y, 1200);
            }
            else if ((ModifierKeys & Keys.Alt) == Keys.Alt)
            {
                PictureMoveAndZoomInOut(e.X, e.Y, 30000);
            }
            else if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                PictureMoveAndZoomInOut(e.X, e.Y, windowSizeUpDown.Maximum);
            }
            else if (LeftClick == false)
            {
                if (LastCursorPosition.X == e.X && LastCursorPosition.Y == e.Y)
                {
                    var range = 5 * (int)xScale; if (range > 10) range = 10;
                    var temp = findItemFromMouse(e.X, e.Y, range);
                    if (temp != null)
                    {
                        if (temp is SwitchWidget)
                        {
                            switchPickedItem = (SwitchWidget)temp;
                            signalPickedItem = null;
                            HandlePickedSwitch();
                        }
                        if (temp is SignalWidget)
                        {
                            signalPickedItem = (SignalWidget)temp;
                            switchPickedItem = null;
                            HandlePickedSignal();
                        }
                    }
                    else
                    {
                        switchPickedItem = null;
                        signalPickedItem = null;
                        UnHandleItemPick();
                        PickedTrain = null;
                    }
                }

            }
            lblInstruction1.Visible = false;
            lblInstruction2.Visible = false;
            lblInstruction3.Visible = false;
            lblInstruction4.Visible = false;

        }

        private void UnHandleItemPick()
        {
            boxSetSignal.Visible = false;
            boxSetSwitch.Visible = false;
        }

        private void HandlePickedSignal()
        {
            if (MPManager.IsClient() && !MPManager.Instance().AmAider) // normal client not server or aider
                return;
            boxSetSwitch.Visible = false;
            if (signalPickedItem == null) return;
            var y = LastCursorPosition.Y;
            if (LastCursorPosition.Y < 100) y = 100;
            if (LastCursorPosition.Y > pbCanvas.Size.Height - 100) y = pbCanvas.Size.Height - 100;

            if (boxSetSignal.Items.Count == 5)
                boxSetSignal.Items.RemoveAt(4);

            if (signalPickedItem.Signal.enabledTrain != null && signalPickedItem.Signal.CallOnEnabled)
            {
                if (!signalPickedItem.Signal.CallOnManuallyAllowed)
                    boxSetSignal.Items.Add("Allow call on");
            }

            boxSetSignal.Location = new System.Drawing.Point(LastCursorPosition.X + 2, y);
            boxSetSignal.Enabled = true;
            boxSetSignal.Focus();
            boxSetSignal.SelectedIndex = -1;
            boxSetSignal.Visible = true;
            return;
        }

        private void HandlePickedSwitch()
        {
            if (MPManager.IsClient() && !MPManager.Instance().AmAider)
                return;//normal client not server

            boxSetSignal.Visible = false;
            if (switchPickedItem == null) return;
            var y = LastCursorPosition.Y + 100;
            if (y < 140) y = 140;
            if (y > pbCanvas.Size.Height + 100) y = pbCanvas.Size.Height + 100;
            boxSetSwitch.Location = new System.Drawing.Point(LastCursorPosition.X + 2, y);
            boxSetSwitch.Enabled = true;
            boxSetSwitch.Focus();
            boxSetSwitch.SelectedIndex = -1;
            boxSetSwitch.Visible = true;
            return;
        }

        private ItemWidget findItemFromMouse(int x, int y, int range)
        {
            if (range < 5) range = 5;
            double closest = float.NaN;
            ItemWidget closestItem = null;
            if (chkPickSwitches.Checked == true)
            {
                foreach (var item in switchItemsDrawn)
                {
                    //if out of range, continue
                    if (item.Location2D.X < x - range || item.Location2D.X > x + range
                       || item.Location2D.Y < y - range || item.Location2D.Y > y + range)
                        continue;

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
                    switchPickedTime = simulator.GameTime;
                    return closestItem;
                }
            }
            if (chkPickSignals.Checked == true)
            {
                foreach (var item in signalItemsDrawn)
                {
                    //if out of range, continue
                    if (item.Location2D.X < x - range || item.Location2D.X > x + range
                       || item.Location2D.Y < y - range || item.Location2D.Y > y + range)
                        continue;

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
                    switchPickedTime = simulator.GameTime;
                    return closestItem;
                }
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
                else
                    continue;

                worldPos = firstCar.WorldPosition;
                tX = (worldPos.TileX * 2048 - subX + worldPos.Location.X) * xScale;
                tY = pbCanvas.Height - (worldPos.TileZ * 2048 - subY + worldPos.Location.Z) * yScale;
                float xSpeedCorr = Math.Abs(t.SpeedMpS) * xScale * 1.5f;
                float ySpeedCorr = Math.Abs(t.SpeedMpS) * yScale * 1.5f;

                if (tX < x - range - xSpeedCorr || tX > x + range + xSpeedCorr || tY < y - range - ySpeedCorr || tY > y + range + ySpeedCorr)
                    continue;
                if (PickedTrain == null)
                    PickedTrain = t;
            }
            //if a train is picked, will clear the avatar list selection
            if (PickedTrain != null)
            {
                AvatarView.SelectedItems.Clear();
                return new TrainWidget(PickedTrain);
            }
            return null;
        }

        private void pictureBoxMouseMove(object sender, MouseEventArgs e)
        {
            if (tWindow.SelectedIndex == 1)
                TimetableDrag(sender, e);
            else
            {
                if (Dragging && !Zooming)
                {
                    int diffX = LastCursorPosition.X - e.X;
                    int diffY = LastCursorPosition.Y - e.Y;

                    ViewWindow.Offset(diffX * ScrollSpeedX / 10, -diffY * ScrollSpeedX / 10);
                    GenerateView();
                }
                else if (Zooming)
                {
                    decimal tempValue = windowSizeUpDown.Value;
                    if (LastCursorPosition.Y - e.Y < 0) tempValue /= 0.95m;
                    else if (LastCursorPosition.Y - e.Y > 0) tempValue *= 0.95m;

                    if (tempValue < windowSizeUpDown.Minimum) tempValue = windowSizeUpDown.Minimum;
                    if (tempValue > windowSizeUpDown.Maximum) tempValue = windowSizeUpDown.Maximum;
                    windowSizeUpDown.Value = tempValue;
                    GenerateView();
                }
                LastCursorPosition.X = e.X;
                LastCursorPosition.Y = e.Y;
            }
        }

        public bool AddNewMessage(double _, string msg)
        {
            if (messages.Items.Count > 10)
                messages.Items.RemoveAt(0);
            messages.Items.Add(msg);
            messages.SelectedIndex = messages.Items.Count - 1;
            messages.SelectedIndex = -1;
            return true;
        }

        private void chkAllowUserSwitch_CheckedChanged(object sender, EventArgs e)
        {
            MPManager.AllowedManualSwitch = chkAllowUserSwitch.Checked;
            if (chkAllowUserSwitch.Checked == true) { MPManager.BroadCast(new MSGMessage("All", "SwitchOK", "OK to switch").ToString()); }
            else { MPManager.BroadCast(new MSGMessage("All", "SwitchWarning", "Cannot switch").ToString()); }
        }

        private void chkShowAvatars_CheckedChanged(object sender, EventArgs e)
        {
            Program.Simulator.Settings.ShowAvatar = chkShowAvatars.Checked;
            AvatarView.Items.Clear();

            if (avatarList != null)
                avatarList.Clear();

            if (chkShowAvatars.Checked)
                AvatarView.Font = new Font(FontFamily.GenericSansSerif, 12);
            else
                AvatarView.Font = new Font(FontFamily.GenericSansSerif, 16);

            try
            {
                CheckAvatar();
            }
            catch { }
        }

        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }
        string imagestring = "iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAgY0hSTQAAeiYAAICEAAD6AAAAgOgAAHUwAADqYAAAOpgAABdwnLpRPAAAACpJREFUOE9jYBjs4D/QgSBMNhg1ABKAFAUi2aFPNY0Ue4FiA6jmlUFsEABfyg/x8/L8/gAAAABJRU5ErkJggg==";

        private void composeMSG_Click(object sender, EventArgs e)
        {
            MSG.Enabled = true;
            MSG.Focus();
            MPManager.Instance().ComposingText = true;
            msgAll.Enabled = true;
            if (messages.SelectedItems.Count > 0) msgSelected.Enabled = true;
            if (AvatarView.SelectedItems.Count > 0) reply2Selected.Enabled = true;
        }

        private void msgAll_Click(object sender, EventArgs e)
        {
            msgDefault();
        }

        private void msgDefault()
        {
            msgAll.Enabled = false;
            msgSelected.Enabled = false;
            reply2Selected.Enabled = false;
            if (!MPManager.IsMultiPlayer())
                return;
            var msg = MSG.Text;
            msg = msg.Replace("\r", "");
            msg = msg.Replace("\t", "");
            MPManager.Instance().ComposingText = false;
            MSG.Enabled = false;

            if (msg != "")
            {
                if (MPManager.IsServer())
                {
                    var users = MPManager.OnlineTrains.Players.Keys
                        .Select((string u) => $"{u}\r");
                    string user = string.Join("", users) + "0END";
                    string msgText = new MSGText(MPManager.GetUserName(), user, msg).ToString();
                    try
                    {
                        MPManager.Notify(msgText);
                    }
                    catch { }
                    finally
                    {
                        MSG.Text = "";
                    }
                }
                else
                {
                    var user = "0Server\r+0END";
                    MPManager.Notify(new MSGText(MPManager.GetUserName(), user, msg).ToString());
                    MSG.Text = "";
                }
            }
        }
        private void replySelected(object sender, EventArgs e)
        {
            msgAll.Enabled = false;
            msgSelected.Enabled = false;
            reply2Selected.Enabled = false;

            if (!MPManager.IsMultiPlayer())
                return;

            var msg = MSG.Text;
            msg = msg.Replace("\r", "");
            msg = msg.Replace("\t", "");
            MPManager.Instance().ComposingText = false;
            MSG.Text = "";
            MSG.Enabled = false;
            if (msg == "") return;
            var user = "";
            if (messages.SelectedItems.Count > 0)
            {
                var chosen = messages.SelectedItems;
                for (var i = 0; i < chosen.Count; i++)
                {
                    var tmp = (string)chosen[i];
                    var index = tmp.IndexOf(':');
                    if (index < 0) continue;
                    tmp = tmp.Substring(0, index) + "\r";
                    if (user.Contains(tmp)) continue;
                    user += tmp;
                }
                user += "0END";
            }
            else
                return;

            MPManager.Notify(new MSGText(MPManager.GetUserName(), user, msg).ToString());
        }

        private void MSGLeave(object sender, EventArgs e)
        {
            //MultiPlayer.MPManager.Instance().ComposingText = false;
        }

        private void MSGEnter(object sender, EventArgs e)
        {
            //MultiPlayer.MPManager.Instance().ComposingText = true;
        }

        private void DispatcherLeave(object sender, EventArgs e)
        {
            //MultiPlayer.MPManager.Instance().ComposingText = false;
        }

        private void checkKeys(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                if (e.KeyValue == 13)
                {
                    var msg = MSG.Text;
                    msg = msg.Replace("\r", "");
                    msg = msg.Replace("\t", "");
                    msg = msg.Replace("\n", "");
                    MPManager.Instance().ComposingText = false;
                    MSG.Enabled = false;
                    MSG.Text = "";
                    if (msg == "") return;
                    var user = "";

                    if (MPManager.IsServer())
                    {
                        var users = MPManager.OnlineTrains.Players.Keys
                            .Select((string u) => $"{u}\r");
                        user += string.Join("", users) + "0END";
                        string msgText = new MSGText(MPManager.GetUserName(), user, msg).ToString();
                        try
                        {
                            MPManager.Notify(msgText);
                        }
                        catch { }
                        finally
                        {
                            MSG.Text = "";
                        }
                    }
                    else
                    {
                        user = "0Server\r+0END";
                        MPManager.Notify(new MSGText(MPManager.GetUserName(), user, msg).ToString());
                        MSG.Text = "";
                    }
                }
            }
        }

        private void msgSelected_Click(object sender, EventArgs e)
        {
            msgAll.Enabled = false;
            msgSelected.Enabled = false;
            reply2Selected.Enabled = false;
            MPManager.Instance().ComposingText = false;
            MSG.Enabled = false;

            if (!MPManager.IsMultiPlayer()) return;
            var msg = MSG.Text;
            MSG.Text = "";
            msg = msg.Replace("\r", "");
            msg = msg.Replace("\t", "");
            if (msg == "") return;
            var user = "";
            if (AvatarView.SelectedItems.Count > 0)
            {
                var chosen = AvatarView.SelectedItems;
                for (var i = 0; i < chosen.Count; i++)
                {
                    var name = chosen[i].Text.Split(' ')[0]; //text may have (H) in it, so need to filter out
                    if (name == MPManager.GetUserName())
                        continue;
                    user += name + "\r";
                }
                user += "0END";
            }
            else
                return;

            MPManager.Notify(new MSGText(MPManager.GetUserName(), user, msg).ToString());

        }

        private void msgSelectedChanged(object sender, EventArgs e)
        {
            AvatarView.SelectedItems.Clear();
            msgSelected.Enabled = false;
            if (MSG.Enabled == true) reply2Selected.Enabled = true;
        }

        private void AvatarView_SelectedIndexChanged(object sender, EventArgs e)
        {
            messages.SelectedItems.Clear();
            reply2Selected.Enabled = false;
            if (MSG.Enabled == true) msgSelected.Enabled = true;
            if (AvatarView.SelectedItems.Count <= 0) return;
            var name = AvatarView.SelectedItems[0].Text.Split(' ')[0].Trim();
            if (name == MPManager.GetUserName())
            {
                if (Program.Simulator.PlayerLocomotive != null) PickedTrain = Program.Simulator.PlayerLocomotive.Train;
                else if (Program.Simulator.Trains.Count > 0) PickedTrain = Program.Simulator.Trains[0];
            }
            else PickedTrain = MPManager.OnlineTrains.findTrain(name);

        }

        private void chkDrawPathChanged(object sender, EventArgs e)
        {
            DrawPath = chkDrawPath.Checked;
        }

        private void boxSetSignalChosen(object sender, EventArgs e)
        {
            if (signalPickedItem == null)
            {
                UnHandleItemPick();
                return;
            }

            var signal = signalPickedItem.Signal;
            var type = boxSetSignal.SelectedIndex;
            if (MPManager.Instance().AmAider)
            {
                MPManager.Notify(new MSGSignalChange(signal, type).ToString());
                UnHandleItemPick();
                return;
            }

            switch (type)
            {
                case 0:
                    signal.ClearHoldSignalDispatcher();
                    break;

                case 1:
                    signal.RequestHoldSignalDispatcher(true);
                    break;

                case 2:
                    signal.RequestApproachAspect();
                    break;

                case 3:
                    signal.RequestLeastRestrictiveAspect();
                    break;

                case 4:
                    signal.SetManualCallOn(true);
                    break;
            }

            UnHandleItemPick();
        }

        private void boxSetSwitchChosen(object sender, EventArgs e)
        {
            if (switchPickedItem == null)
            {
                UnHandleItemPick(); return;
            }
            var sw = switchPickedItem.Item.TrJunctionNode;
            var type = boxSetSwitch.SelectedIndex;

            //aider can send message to the server for a switch
            if (MPManager.IsMultiPlayer() && MPManager.Instance().AmAider)
            {
                var nextSwitchTrack = sw;
                var Selected = 0;
                switch (type)
                {
                    case 0:
                        Selected = (int)switchPickedItem.main;
                        break;
                    case 1:
                        Selected = 1 - (int)switchPickedItem.main;
                        break;
                }
                //aider selects and throws the switch, but need to confirm by the dispatcher
                MPManager.Notify(new MSGSwitch(MPManager.GetUserName(),
                    nextSwitchTrack.TN.UiD.WorldTileX, nextSwitchTrack.TN.UiD.WorldTileZ, nextSwitchTrack.TN.UiD.WorldId, Selected, true).ToString());
                Program.Simulator.Confirmer.Information(Viewer.Catalog.GetString("Switching Request Sent to the Server"));

            }
            //server throws the switch immediately
            else
            {
                switch (type)
                {
                    case 0:
                        Program.Simulator.Signals.RequestSetSwitch(sw.TN, (int)switchPickedItem.main);
                        //sw.SelectedRoute = (int)switchPickedItem.main;
                        break;
                    case 1:
                        Program.Simulator.Signals.RequestSetSwitch(sw.TN, 1 - (int)switchPickedItem.main);
                        //sw.SelectedRoute = 1 - (int)switchPickedItem.main;
                        break;
                }
            }
            UnHandleItemPick();

        }

        private void chkAllowNewCheck(object sender, EventArgs e)
        {
            MPManager.Instance().AllowNewPlayer = chkAllowNew.Checked;
        }

        private void AssistClick(object sender, EventArgs e)
        {
            AvatarView.SelectedIndices.Remove(0);
            if (AvatarView.SelectedIndices.Count > 0)
            {
                var tmp = AvatarView.SelectedItems[0].Text.Split(' ');
                var name = tmp[0].Trim();
                if (MPManager.Instance().aiderList.Contains(name)) return;
                if (MPManager.OnlineTrains.Players.ContainsKey(name))
                {
                    MPManager.BroadCast(new MSGAider(name, true).ToString());
                    MPManager.Instance().aiderList.Add(name);
                }
                AvatarView.Items.Clear();
                if (avatarList != null) avatarList.Clear();
            }
        }

        private void btnNormalClick(object sender, EventArgs e)
        {
            if (AvatarView.SelectedIndices.Count > 0)
            {
                var tmp = AvatarView.SelectedItems[0].Text.Split(' ');
                var name = tmp[0].Trim();
                if (MPManager.OnlineTrains.Players.ContainsKey(name))
                {
                    MPManager.BroadCast(new MSGAider(name, false).ToString());
                    MPManager.Instance().aiderList.Remove(name);
                }
                AvatarView.Items.Clear();
                if (avatarList != null) avatarList.Clear();
            }

        }

        private void btnFollowClick(object sender, EventArgs e)
        {
            followTrain = true;
        }

        private void chkOPenaltyHandle(object sender, EventArgs e)
        {
            MPManager.Instance().CheckSpad = chkBoxPenalty.Checked;
            if (chkBoxPenalty.Checked == false) { MPManager.BroadCast(new MSGMessage("All", "OverSpeedOK", "OK to go overspeed and pass stop light").ToString()); }
            else { MPManager.BroadCast(new MSGMessage("All", "NoOverSpeed", "Penalty for overspeed and passing stop light").ToString()); }

        }

        private void chkPreferGreenHandle(object sender, EventArgs e)
        {
            MPManager.PreferGreen = chkBoxPenalty.Checked;

        }

        public bool ClickedTrain;
        private void btnSeeInGameClick(object sender, EventArgs e)
        {
            if (PickedTrain != null) ClickedTrain = true;
            else ClickedTrain = false;
        }

        private void bBackgroundColor_Click(object sender, EventArgs e)
        {
            if (pbCanvas.BackColor == Color.White)
                pbCanvas.BackColor = Color.FromArgb(64, 128, 128);
            else if (pbCanvas.BackColor == Color.FromArgb(64, 128, 128))
                pbCanvas.BackColor = Color.FromArgb(250, 234, 209);
            else if (pbCanvas.BackColor == Color.FromArgb(250, 234, 209))
                pbCanvas.BackColor = Color.FromArgb(250, 240, 230);
            else if (pbCanvas.BackColor == Color.FromArgb(250, 240, 230)) // Windows color "linen"
                pbCanvas.BackColor = Color.White;
        }

        private void PictureMoveAndZoomInOut(int x, int y, decimal scale)
        {
            int diffX = x - pbCanvas.Width / 2;
            int diffY = y - pbCanvas.Height / 2;
            ViewWindow.Offset(diffX / xScale, -diffY / yScale);
            if (scale < windowSizeUpDown.Minimum) scale = windowSizeUpDown.Minimum;
            if (scale > windowSizeUpDown.Maximum) scale = windowSizeUpDown.Maximum;
            windowSizeUpDown.Value = scale;
            GenerateView();
        }

        #region Timetable
        public int DaylightOffsetHrs { get; set; } = 0;

        private void TimetableDrag(object sender, MouseEventArgs e)
        {
            if (Dragging && !Zooming)
            {
                int diffX = e.X - LastCursorPosition.X;
                int diffY = e.Y - LastCursorPosition.Y;

                ClipDrag(diffX, diffY);
                GenerateView(true);
            }
            else if (Zooming)
            {
                decimal tempValue = windowSizeUpDown.Value;
                if (LastCursorPosition.Y - e.Y < 0) tempValue /= 0.95m;
                else if (LastCursorPosition.Y - e.Y > 0) tempValue *= 0.95m;

                if (tempValue < windowSizeUpDown.Minimum) tempValue = windowSizeUpDown.Minimum;
                if (tempValue > windowSizeUpDown.Maximum) tempValue = windowSizeUpDown.Maximum;
                windowSizeUpDown.Value = tempValue;
                GenerateView(true);
            }
            LastCursorPosition.X = e.X;
            LastCursorPosition.Y = e.Y;
        }

        private void pbCanvas_SizeChanged(object sender, EventArgs e)
        {
            var oldSizePxX = ViewWindow.Width * xScale;
            var oldSizePxY = ViewWindow.Height * yScale;
            var newSizePxX = pbCanvas.Width;
            var newSizePxY = pbCanvas.Height;
            var sizeIncreaseX = newSizePxX / oldSizePxX;
            var sizeIncreaseY = newSizePxY / oldSizePxY;

            // Could be clever and keep all the previous view still in view and centred at the same point.
            // Instead use the simplest solution:
            ViewWindow.Width *= sizeIncreaseX;
            ViewWindow.Height *= sizeIncreaseY;
        }

        /// <summary>
        /// Add or subtract hours of daylight to more easily observe activity during the night.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nudDaylightOffsetHrs_ValueChanged(object sender, EventArgs e)
        {
            DaylightOffsetHrs = (int)nudDaylightOffsetHrs.Value;
        }

        private void tWindow_SelectedIndexChanged(object sender, EventArgs e)
        {
            TimetableWindow.SetControls();
        }

        /// <summary>
        /// Loads a relevant page from the manual maintained by James Ross's automatic build
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bTrainKey_Click(object sender, EventArgs e)
        {
            // This method is also compatible with .NET Core 3
            var psi = new ProcessStartInfo
            {
                FileName = "https://open-rails.readthedocs.io/en/latest/driving.html#extended-hud-for-dispatcher-information"
                ,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        /// <summary>
        /// Provides a clip zone to stop user from pushing track fully out of window
        /// </summary>
        /// <param name="diffX"></param>
        /// <param name="diffY"></param>
        private void ClipDrag(int diffX, int diffY)
        {
            // Moving the mouse right means moving the ViewWindow left.
            var changeXm = -(float)(diffX / xScale);
            // Moving the mouse up means moving the ViewWindow up.
            var changeYm = +(float)(diffY / yScale);


            const int clipPixels = 100;
            var viewWindowLeftM = minX + ViewWindow.X;
            var viewWindowRightM = minX + ViewWindow.X + ViewWindow.Width;
            var bufferXm = clipPixels / xScale;
            var viewWindowTopM = minY + ViewWindow.Y;
            var viewWindowBottomM = minY + ViewWindow.Y + ViewWindow.Height;
            var bufferYm = clipPixels / yScale;

            if (viewWindowRightM + changeXm < minX + bufferXm) // drag right => -ve changeX
                changeXm = +(minX + bufferXm - viewWindowRightM);
            else if (viewWindowLeftM + changeXm > maxX - bufferXm)
                changeXm = +(maxX - bufferXm - viewWindowLeftM);

            if (viewWindowBottomM + changeYm < minY + bufferYm)
                changeYm = minY + bufferYm - viewWindowBottomM;
            else if (viewWindowTopM + changeYm > maxY - bufferYm)
                changeYm = maxY - bufferYm - viewWindowTopM;

            ViewWindow.Offset(changeXm, changeYm);
        }
        #endregion
    }

    
    /* NOTE: Commented out to avoid duplication in namespace; copied to `DebugViewerBetaForm.cs` */
    /*
    #region SignalWidget
    /// <summary>
    /// Defines a signal being drawn in a 2D view.
    /// </summary>
    public class SignalWidget : ItemWidget
    {
        public TrItem Item;
        /// <summary>
        /// The underlying signal object as referenced by the TrItem.
        /// </summary>
        public SignalObject Signal;

        public PointF Dir;
        public bool hasDir;
        /// <summary>
        /// For now, returns true if any of the signal heads shows any "clear" aspect.
        /// This obviously needs some refinement.
        /// </summary>
        public int IsProceed
        {
            get
            {
                int returnValue = 2;

                foreach (var head in Signal.SignalHeads.Where(x => x.Function == SignalFunction.NORMAL))
                {
                    if (head.state == MstsSignalAspect.CLEAR_1
                        || head.state == MstsSignalAspect.CLEAR_2)
                    {
                        returnValue = 0;
                    }
                    if (head.state == MstsSignalAspect.APPROACH_1
                        || head.state == MstsSignalAspect.APPROACH_2
                        || head.state == MstsSignalAspect.APPROACH_3)
                    {
                        returnValue = 1;
                    }
                }

                return returnValue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public SignalWidget(SignalItem item, SignalObject signal)
        {
            Item = item;
            Signal = signal;
            hasDir = false;
            Location.X = item.TileX * 2048 + item.X;
            Location.Y = item.TileZ * 2048 + item.Z;
            var node = Program.Simulator.TDB.TrackDB.TrackNodes?[signal.trackNode];
            Vector2 v2;
            if (node?.TrVectorNode != null)
            {
                var ts = node.TrVectorNode.TrVectorSections?.FirstOrDefault();
                if (ts == null)
                    return;
                v2 = new Vector2(ts.TileX * 2048 + ts.X, ts.TileZ * 2048 + ts.Z);
            }
            else if (node?.TrJunctionNode != null)
            {
                var ts = node?.UiD;
                if (ts == null)
                    return;
                v2 = new Vector2(ts.TileX * 2048 + ts.X, ts.TileZ * 2048 + ts.Z);
            }
            else
            {
                return;
            }
            var v1 = new Vector2(Location.X, Location.Y);
            var v3 = v1 - v2;
            v3.Normalize();
            void copyTo(Vector2 input, ref PointF output)
            {
                output.X = input.X;
                output.Y = input.Y;
            }
            copyTo(v1 - Vector2.Multiply(v3, signal.direction == 0 ? 12f : -12f), ref Dir);
            //shift signal along the dir for 2m, so signals will not be overlapped
            copyTo(v1 - Vector2.Multiply(v3, signal.direction == 0 ? 1.5f : -1.5f), ref Location);
            hasDir = true;
        }
    }
    #endregion

    #region SwitchWidget
    /// <summary>
    /// Defines a signal being drawn in a 2D view.
    /// </summary>
    public class SwitchWidget : ItemWidget
    {
        public TrackNode Item;
        public uint main;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public SwitchWidget(TrackNode item)
        {
            Item = item;
            var TS = Program.Simulator.TSectionDat.TrackShapes.Get(item.TrJunctionNode.ShapeIndex);  // TSECTION.DAT tells us which is the main route

            if (TS != null)
            {
                main = TS.MainRoute;
            }
            else
            {
                main = 0;
            }

            Location.X = Item.UiD.TileX * 2048 + Item.UiD.X; Location.Y = Item.UiD.TileZ * 2048 + Item.UiD.Z;
        }
    }

    #endregion

    #region BufferWidget
    public class BufferWidget : ItemWidget
    {
        public TrackNode Item;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public BufferWidget(TrackNode item)
        {
            Item = item;

            Location.X = Item.UiD.TileX * 2048 + Item.UiD.X; Location.Y = Item.UiD.TileZ * 2048 + Item.UiD.Z;
        }
    }
    #endregion

    #region ItemWidget
    public class ItemWidget
    {
        public PointF Location;
        public PointF Location2D;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public ItemWidget()
        {

            Location = new PointF(float.NegativeInfinity, float.NegativeInfinity);
            Location2D = new PointF(float.NegativeInfinity, float.NegativeInfinity);
        }
    }
    #endregion

    #region TrainWidget
    public class TrainWidget : ItemWidget
    {
        public Train Train;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public TrainWidget(Train t)
        {
            Train = t;
        }
    }
    #endregion

    #region LineSegment
    /// <summary>
    /// Defines a geometric line segment.
    /// </summary>
    public class LineSegment
    {
        public dVector A;
        public dVector B;
        public dVector C;
        //public float radius;
        public bool isCurved;

        public float angle1, angle2;

        public LineSegment(dVector A, dVector B, bool Occupied, TrVectorSection Section)
        {
            this.A = A;
            this.B = B;

            isCurved = false;
            if (Section == null) return;
            //MySection = Section;
            uint k = Section.SectionIndex;
            TrackSection ts = Program.Simulator.TSectionDat.TrackSections.Get(k);
            if (ts != null)
            {
                if (ts.SectionCurve != null)
                {
                    float diff = (float)(ts.SectionCurve.Radius * (1 - Math.Cos(ts.SectionCurve.Angle * 3.14f / 360)));
                    if (diff < 3) return; //not need to worry, curve too small
                                          //curve = ts.SectionCurve;
                    Vector3 v = new Vector3((float)((B.TileX - A.TileX) * 2048 + B.X - A.X), 0, (float)((B.TileZ - A.TileZ) * 2048 + B.Z - A.Z));
                    isCurved = true;
                    Vector3 v2 = Vector3.Cross(Vector3.Up, v); v2.Normalize();
                    v = v / 2; v.X += A.TileX * 2048 + (float)A.X; v.Z += A.TileZ * 2048 + (float)A.Z;
                    if (ts.SectionCurve.Angle > 0)
                    {
                        v = v2 * -diff + v;
                    }
                    else v = v2 * diff + v;
                    C = new dVector(0, v.X, 0, v.Z);
                }
            }
        }
    }
    #endregion

    #region SidingWidget

    /// <summary>
    /// Defines a siding name being drawn in a 2D view.
    /// </summary>
    public struct SidingWidget
    {
        public uint Id;
        public PointF Location;
        public string Name;
        public uint LinkId;

        /// <summary>
        /// The underlying track item.
        /// </summary>
        public SidingItem Item;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public SidingWidget(SidingItem item)
        {
            Id = item.TrItemId;
            LinkId = item.LinkedSidingId;
            Item = item;
            Name = item.ItemName;
            Location = new PointF(item.TileX * 2048 + item.X, item.TileZ * 2048 + item.Z);
        }
    }
    #endregion

    public struct PlatformWidget
    {
        public uint Id;
        public PointF Location;
        public string Name;
        public PointF Extent1;
        public PointF Extent2;
        public uint LinkId;
        public string Station;

        /// <summary>
        /// The underlying track item.
        /// </summary>
        public PlatformItem Item;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="signal"></param>
        public PlatformWidget(PlatformItem item)
        {
            Id = item.TrItemId;
            LinkId = item.LinkedPlatformItemId;
            Item = item;
            Name = item.ItemName;
            Station = item.Station;
            Location = new PointF(item.TileX * 2048 + item.X, item.TileZ * 2048 + item.Z);
            Extent1 = default;
            Extent2 = default;
        }
    }

    public class dVector
    {
        public int TileX, TileZ;
        public double X, Z;

        public dVector(int tilex1, double x1, int tilez1, double z1)
        {
            TileX = tilex1;
            TileZ = tilez1;
            X = x1;
            Z = z1;
        }

        static public double DistanceSqr(dVector v1, dVector v2)
        {
            return Math.Pow((v1.TileX - v2.TileX) * 2048 + v1.X - v2.X, 2)
                + Math.Pow((v1.TileZ - v2.TileZ) * 2048 + v1.Z - v2.Z, 2);
        }
    }*/
}
